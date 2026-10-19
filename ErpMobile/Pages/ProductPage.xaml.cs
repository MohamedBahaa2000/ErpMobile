using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

public partial class ProductPage : ContentPage
{
    private const string ChooseItem = "-- اختر --";

    public ProductPage()
    {
        InitializeComponent();

        UnitPicker.ItemsSource = new[] { "قطعة", "كرتونة", "كيلوجرام", "لتر" };
        UnitPicker.SelectedIndex = 0;
        UnitPicker.SelectedIndexChanged += (_, _) => UnitCellLabel.Text = Selected(UnitPicker);

        OriginPicker.ItemsSource = new[] { ChooseItem, "محلي", "مستورد" };
        OriginPicker.SelectedIndex = 0;

        CategoryPicker.ItemsSource = new[] { ChooseItem, "مخزني", "خدمي", "مصروف" };
        CategoryPicker.SelectedIndex = 0;

        ManufacturerPicker.ItemsSource = new[] { ChooseItem, "شركة عامة", "شركة خاصة" };
        ManufacturerPicker.SelectedIndex = 0;

        CostMethodPicker.ItemsSource = new[]
        {
            "متوسط مرجح",
            "الوارد أولاً صادر أولاً (FIFO)",
            "الوارد أولاً صادر أخيراً (LIFO)",
            "يدوي"
        };
        CostMethodPicker.SelectedIndex = 0;
    }

    // ---------------------------------------------------------------------
    // المسح والحقول الحية
    // ---------------------------------------------------------------------

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
            await DisplayAlertAsync("إذن الكاميرا", "اسمح للتطبيق باستخدام الكاميرا لمسح الباركود.", "موافق");
        }
    }

    private void OnPriceFieldsChanged(object? sender, TextChangedEventArgs e)
    {
        BarcodeCellLabel.Text = string.IsNullOrWhiteSpace(BarcodeEntry.Text) ? "—" : BarcodeEntry.Text.Trim();
        PriceCellLabel.Text = string.IsNullOrWhiteSpace(PriceEntry.Text) ? "0.00" : PriceEntry.Text.Trim();
        WholesaleCellLabel.Text = string.IsNullOrWhiteSpace(WholesalePriceEntry.Text) ? "0.00" : WholesalePriceEntry.Text.Trim();
    }

    // ---------------------------------------------------------------------
    // إضافة قيم جديدة للقوائم المنسدلة
    // ---------------------------------------------------------------------

    private async void OnAddOriginClicked(object? sender, EventArgs e) => await AddCustomValueAsync(OriginPicker);

    private async void OnAddCategoryClicked(object? sender, EventArgs e) => await AddCustomValueAsync(CategoryPicker);

    private async void OnAddManufacturerClicked(object? sender, EventArgs e) => await AddCustomValueAsync(ManufacturerPicker);

    private async Task AddCustomValueAsync(Picker picker)
    {
        string? value = await DisplayPromptAsync("إضافة جديد", "أدخل القيمة:", "حفظ", "إلغاء");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        value = value.Trim();
        var items = picker.Items;
        int existing = items.IndexOf(value);
        if (existing < 0)
        {
            items.Add(value);
            existing = items.IndexOf(value);
        }

        picker.SelectedIndex = existing;
    }

    private static string Selected(Picker picker) =>
        picker.SelectedIndex < 0 ? string.Empty : picker.Items[picker.SelectedIndex];

    // ---------------------------------------------------------------------
    // الحفظ
    // ---------------------------------------------------------------------

    private async void OnSaveAndNewClicked(object? sender, EventArgs e)
    {
        if (await SaveProductAsync())
        {
            ClearForm();
            await DisplayAlertAsync("تم الحفظ", "تم حفظ الصنف، أدخل بيانات الصنف التالي.", "موافق");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (await SaveProductAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task<bool> SaveProductAsync()
    {
        string name = NameEntry.Text?.Trim() ?? string.Empty;
        string barcode = BarcodeEntry.Text?.Trim() ?? string.Empty;

        if (name.Length == 0 || barcode.Length == 0)
        {
            await DisplayAlertAsync("بيانات ناقصة", "أدخل اسم الصنف وكود الصنف (الباركود).", "موافق");
            return false;
        }

        if (!TryParseNumber(PriceEntry.Text, out double price) || price < 0)
        {
            await DisplayAlertAsync("سعر غير صحيح", "أدخل سعر تجزئة صحيحاً للصنف.", "موافق");
            return false;
        }

        _ = TryParseNumber(WholesalePriceEntry.Text, out double wholesalePrice);
        _ = TryParseNumber(CostValueEntry.Text, out double costValue);

        var product = new Product
        {
            Name = name,
            Barcode = barcode,
            Price = price,
            Unit = Selected(UnitPicker).Length == 0 ? "قطعة" : Selected(UnitPicker),
            EnglishName = EnglishNameEntry.Text?.Trim() ?? string.Empty,
            HelperCode = HelperCodeEntry.Text?.Trim() ?? string.Empty,
            Origin = Selected(OriginPicker),
            Category = Selected(CategoryPicker),
            Manufacturer = Selected(ManufacturerPicker),
            CompanyCode = CompanyCodeEntry.Text?.Trim() ?? string.Empty,
            CostMethod = Selected(CostMethodPicker).Length == 0 ? "متوسط مرجح" : Selected(CostMethodPicker),
            CostValue = costValue,
            WholesalePrice = wholesalePrice,
            IsStock = IsStockCheck.IsChecked,
            IsTaxable = IsTaxableCheck.IsChecked,
            Notes = NotesEditor.Text?.Trim() ?? string.Empty
        };

        bool added = await ProductRepository.Instance.AddAsync(product);
        if (!added)
        {
            await DisplayAlertAsync("الكود مسجل", "يوجد صنف مسجل بهذا الكود بالفعل.", "موافق");
            return false;
        }

        return true;
    }

    private void ClearForm()
    {
        NameEntry.Text = string.Empty;
        BarcodeEntry.Text = string.Empty;
        HelperCodeEntry.Text = string.Empty;
        EnglishNameEntry.Text = string.Empty;
        PriceEntry.Text = string.Empty;
        WholesalePriceEntry.Text = string.Empty;
        CompanyCodeEntry.Text = string.Empty;
        CostValueEntry.Text = string.Empty;
        NotesEditor.Text = string.Empty;
        DiscountValueEntry.Text = "0.00";
        DiscountPercentEntry.Text = "0";
        AllCustomersDiscountEntry.Text = "0";
        IsTaxableCheck.IsChecked = false;
        BarcodeCellLabel.Text = "—";
        PriceCellLabel.Text = "0.00";
        WholesaleCellLabel.Text = "0.00";
        NameEntry.Focus();
    }

    private static bool TryParseNumber(string? text, out double value)
    {
        return double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
               double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
