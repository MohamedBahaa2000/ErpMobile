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
            BarcodeScanCoordinator.Complete(barcode);
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

    protected override void OnDisappearing()
    {
        CameraReader.IsDetecting = false;
        BarcodeScanCoordinator.Complete(null);
        base.OnDisappearing();
    }
}
