using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

public partial class InvoicePage : ContentPage
{
    private static readonly double[] ItemColumnWidths = [45, 110, 145, 75, 75, 85, 95, 80, 90, 140];
    private double _invoiceTotal;

    public InvoicePage()
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
                await AddProductByBarcodeAsync(barcode);
            }
        }
        catch (UnauthorizedAccessException)
        {
            await DisplayAlert("إذن الكاميرا", "اسمح للتطبيق باستخدام الكاميرا لمسح الباركود.", "موافق");
        }
    }

    private async void OnAddBarcodeClicked(object? sender, EventArgs e)
    {
        string barcode = InvoiceBarcodeEntry.Text?.Trim() ?? string.Empty;
        if (barcode.Length == 0)
        {
            await DisplayAlert("الباركود مطلوب", "امسح الباركود أو أدخله أولاً.", "موافق");
            return;
        }

        await AddProductByBarcodeAsync(barcode);
    }

    private async Task AddProductByBarcodeAsync(string barcode)
    {
        Product? product = await ProductRepository.Instance.GetByBarcodeAsync(barcode);
        if (product is null)
        {
            InvoiceBarcodeEntry.Text = barcode;
            await DisplayAlert("المنتج غير موجود", "أضف المنتج أولاً من لوحة التحكم ثم امسح الباركود مرة أخرى.", "موافق");
            return;
        }

        int lineNumber = InvoiceItemsContainer.Children.Count + 1;
        InvoiceItemsContainer.Children.Add(CreateInvoiceRow(product, lineNumber));
        EmptyItemsLabel.IsVisible = false;
        _invoiceTotal += product.Price;
        InvoiceTotalLabel.Text = FormatAmount(_invoiceTotal);
        InvoiceRemainingLabel.Text = FormatAmount(_invoiceTotal);
        InvoiceBarcodeEntry.Text = string.Empty;
    }

    private static Grid CreateInvoiceRow(Product product, int lineNumber)
    {
        var row = new Grid
        {
            Padding = new Thickness(6, 8),
            ColumnSpacing = 2,
            FlowDirection = FlowDirection.RightToLeft
        };

        foreach (double width in ItemColumnWidths)
        {
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
        }

        string price = FormatAmount(product.Price);
        string[] values =
        [
            lineNumber.ToString(CultureInfo.InvariantCulture),
            product.Barcode,
            product.Name,
            product.Unit,
            "1",
            price,
            price,
            "0.00",
            price,
            string.Empty
        ];

        for (int column = 0; column < values.Length; column++)
        {
            var cell = new Label
            {
                Text = values[column],
                FontSize = 11,
                TextColor = Color.FromArgb("#48546B"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            row.Children.Add(cell);
            Grid.SetColumn(cell, column);
        }

        return row;
    }

    private static string FormatAmount(double amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        await DisplayAlert(
            "فواتير المبيعات",
            "هذه واجهة تجريبية؛ لم يتم ربط حفظ الفاتورة بقاعدة بيانات بعد.",
            "موافق");
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
