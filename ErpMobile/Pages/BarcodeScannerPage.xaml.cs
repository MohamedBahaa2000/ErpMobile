using ErpMobile.Services;
using Microsoft.Maui.ApplicationModel;
using ZXing.Net.Maui;

namespace ErpMobile.Pages;

public partial class BarcodeScannerPage : ContentPage
{
    private int _isFinishing;

    public BarcodeScannerPage()
    {
        InitializeComponent();
        CameraReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.OneDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BarcodeScanCoordinator.ProgressChanged += OnScanProgress;
        ScanCountLabel.Text = "الأصناف المضافة: 0";
        ScanMessageLabel.Text = BarcodeScanCoordinator.IsContinuous
            ? "وضع المسح المتصل: مسح متتالي لحد ما تضغط «تم»"
            : "امسح الباركود المطلوب";
        CameraReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        BarcodeScanCoordinator.ProgressChanged -= OnScanProgress;
        CameraReader.IsDetecting = false;
        BarcodeScanCoordinator.Complete(null);
        base.OnDisappearing();
    }

    private void OnScanProgress(int count, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ScanCountLabel.Text = $"الأصناف المضافة: {count}";
            ScanMessageLabel.Text = message;
            ScanMessageLabel.TextColor = message.Contains("غير معروف", StringComparison.Ordinal)
                ? Color.FromArgb("#FFC7CE")
                : Color.FromArgb("#BFD4F2");
        });
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        string? barcode = e.Results.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(barcode) || Interlocked.Exchange(ref _isFinishing, 1) != 0)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            CameraReader.IsDetecting = false;

            if (BarcodeScanCoordinator.IsContinuous)
            {
                bool keepGoing = await BarcodeScanCoordinator.HandleDetectedAsync(barcode);
                if (keepGoing)
                {
                    // نكمل المسح لتلقي صنف آخر
                    Interlocked.Exchange(ref _isFinishing, 0);
                    CameraReader.IsDetecting = true;
                    return;
                }

                BarcodeScanCoordinator.Complete(null);
            }
            else
            {
                BarcodeScanCoordinator.Complete(barcode);
            }

            await Shell.Current.GoToAsync("..");
        });
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _isFinishing, 1) != 0)
        {
            return;
        }

        CameraReader.IsDetecting = false;
        BarcodeScanCoordinator.Complete(null);
        await Shell.Current.GoToAsync("..");
    }
}
