using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;
using Microsoft.Maui.ApplicationModel;
using ZXing.Net.Maui;

namespace ErpMobile.Pages;

public partial class BarcodeScannerPage : ContentPage
{
    private int _isFinishing;
    private CancellationTokenSource? _cardCts;

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
        BarcodeScanCoordinator.ItemAdded += OnItemAdded;
        ScanCountLabel.Text = "الأصناف المضافة: 0";
        QtyValueLabel.Text = BarcodeScanCoordinator.QuantityPerScan.ToString(CultureInfo.InvariantCulture);
        ScanMessageLabel.Text = BarcodeScanCoordinator.IsContinuous
            ? "وضع المسح المتصل: مسح متتالي لحد ما تضغط «تم»"
            : "امسح الباركود المطلوب";
        CameraReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        BarcodeScanCoordinator.ProgressChanged -= OnScanProgress;
        BarcodeScanCoordinator.ItemAdded -= OnItemAdded;
        _cardCts?.Cancel();
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

    /// <summary>عرض معاينة الصنف المضاف فوق الكاميرا (أخضر) أو الباركود المرفوض (أحمر).</summary>
    private void OnItemAdded(string name, string detail, bool success) =>
        MainThread.BeginInvokeOnMainThread(() => _ = ShowAddedCardAsync(name, detail, success));

    private async Task ShowAddedCardAsync(string name, string detail, bool success)
    {
        // نلغي أي معاينة سابقة معلّقة
        _cardCts?.Cancel();
        _cardCts?.Dispose();
        var cts = new CancellationTokenSource();
        _cardCts = cts;
        CancellationToken token = cts.Token;

        try
        {
            AddedCard.BackgroundColor = Color.FromArgb(success ? "#17A45C" : "#D64545");
            AddedIcon.Text = success ? "✓" : "✗";
            AddedName.Text = success ? $"تمت إضافة {name}" : name;
            AddedDetail.Text = detail;
            AddedDetail.TextColor = success
                ? Color.FromArgb("#E9F8F0")
                : Color.FromArgb("#FFE7E7");

            AddedCard.IsVisible = true;
            AddedCard.Opacity = 0;
            AddedCard.TranslationY = -18;
            AddedCard.Scale = 0.9;

            await Task.WhenAll(
                AddedCard.FadeTo(1, 180, Easing.CubicOut),
                AddedCard.TranslateTo(0, 0, 180, Easing.CubicOut),
                AddedCard.ScaleTo(1, 180, Easing.CubicOut));

            await Task.Delay(1400, token);

            await AddedCard.FadeTo(0, 240, Easing.CubicIn);

            if (!token.IsCancellationRequested)
            {
                AddedCard.IsVisible = false;
            }
        }
        catch (TaskCanceledException)
        {
            // وصلت معاينة جديدة قبل إخفاء السابقة — نتجاهل
        }
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

            // صوت عند قراءة الباركود
            ScanFeedback.Beep();

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
                // وضع المسح لمرة واحدة: نعرض اسم الصنف ثم نعود
                Product? product = await ProductRepository.Instance.GetByBarcodeAsync(barcode);
                if (product is not null)
                {
                    BarcodeScanCoordinator.ReportItemAdded(
                        product.Name,
                        $"{product.Barcode} • {product.Price:0.00}",
                        success: true);
                }
                else
                {
                    BarcodeScanCoordinator.ReportItemAdded(
                        "باركود غير معروف", barcode, success: false);
                }

                await Task.Delay(500);

                BarcodeScanCoordinator.Complete(barcode);
            }

            await Shell.Current.GoToAsync("..");
        });
    }

    // ---------------------------------------------------------------------
    // عدّاد كمية الإضافة لكل مسح
    // ---------------------------------------------------------------------

    private void OnQtyPlusClicked(object? sender, EventArgs e) =>
        SetQuantity(BarcodeScanCoordinator.QuantityPerScan + 1);

    private void OnQtyMinusClicked(object? sender, EventArgs e) =>
        SetQuantity(BarcodeScanCoordinator.QuantityPerScan - 1);

    private void SetQuantity(int value)
    {
        BarcodeScanCoordinator.QuantityPerScan = value; // الكوّرديناتور يعمل clamp بين 1 و99
        QtyValueLabel.Text = BarcodeScanCoordinator.QuantityPerScan.ToString(CultureInfo.InvariantCulture);

        int qty = BarcodeScanCoordinator.QuantityPerScan;
        ScanMessageLabel.Text = qty == 1
            ? "كل مسح هيضيف وحدة واحدة"
            : $"كل مسح هيضيف {qty} وحدة — لو نفس الباركود اتمسح تاني هتزود في نفس السطر";
        ScanMessageLabel.TextColor = Color.FromArgb("#9DE7C4");
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
