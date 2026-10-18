using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

public partial class ProductPage : ContentPage
{
    public ProductPage()
    {
        InitializeComponent();
    }

    private async void OnScanBarcodeClicked(object? sender, EventArgs e)
    {
        try
        {
            string? barcode = await BarcodeScanCoordinator.ScanAsync();
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                BarcodeEntry.Text = barcode;
            }
        }
        catch (UnauthorizedAccessException)
        {
            await DisplayAlert("إذن الكاميرا", "اسمح للتطبيق باستخدام الكاميرا لمسح الباركود.", "موافق");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim() ?? string.Empty;
        string barcode = BarcodeEntry.Text?.Trim() ?? string.Empty;
        string unit = UnitEntry.Text?.Trim() ?? string.Empty;

        if (name.Length == 0 || barcode.Length == 0)
        {
            await DisplayAlert("بيانات ناقصة", "أدخل اسم المنتج وامسح الباركود أو اكتبه.", "موافق");
            return;
        }

        if (!TryParsePrice(PriceEntry.Text, out double price) || price < 0)
        {
            await DisplayAlert("سعر غير صحيح", "أدخل سعراً صحيحاً للمنتج.", "موافق");
            return;
        }

        var product = new Product
        {
            Name = name,
            Barcode = barcode,
            Price = price,
            Unit = unit.Length == 0 ? "قطعة" : unit
        };

        bool added = await ProductRepository.Instance.AddAsync(product);
        if (!added)
        {
            await DisplayAlert("الباركود مسجل", "يوجد منتج مسجل بهذا الباركود بالفعل.", "موافق");
            return;
        }

        await DisplayAlert("تم الحفظ", "تم حفظ المنتج على هذا الجهاز.", "موافق");
        await Shell.Current.GoToAsync("..");
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private static bool TryParsePrice(string? text, out double price)
    {
        return double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out price) ||
               double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }
}
