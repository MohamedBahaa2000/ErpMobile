using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

[QueryProperty(nameof(ProductId), "id")]
public partial class ProductPage : ContentPage
{
    private const string ChooseItem = "-- اختر --";

    /// <summary>معرّف الصنف المراد تعديله — 0 لصنف جديد.</summary>
    public int ProductId { get; set; }

    private bool _isEditMode;
    private bool _loaded;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // تحميل الصنف المراد تعديله مرة واحدة
        if (!_loaded && ProductId > 0)
        {
            _loaded = true;
            await LoadProductAsync();
        }
    }

    private async Task LoadProductAsync()
    {
        Product? product = await ProductRepository.Instance.GetByIdAsync(ProductId);
        if (product is null)
        {
            return;
        }

        _isEditMode = true;

        NameEntry.Text = product.Name;
        BarcodeEntry.Text = product.Barcode;
        HelperCodeEntry.Text = product.HelperCode;
        EnglishNameEntry.Text = product.EnglishName;
        PriceEntry.Text = product.Price.ToString("0.00", CultureInfo.InvariantCulture);
        WholesalePriceEntry.Text = product.WholesalePrice.ToString("0.00", CultureInfo.InvariantCulture);
        CompanyCodeEntry.Text = product.CompanyCode;
        CostValueEntry.Text = product.CostValue.ToString("0.00", CultureInfo.InvariantCulture);
        NotesEditor.Text = product.Notes;
        IsStockCheck.IsChecked = product.IsStock;
        IsTaxableCheck.IsChecked = product.IsTaxable;

        SelectInPicker(UnitPicker, product.Unit);
        SelectInPicker(OriginPicker, product.Origin);
        SelectInPicker(CategoryPicker, product.Category);
        SelectInPicker(ManufacturerPicker, product.Manufacturer);
        SelectInPicker(CostMethodPicker, product.CostMethod);

        Title = "تعديل صنف";
        OnPriceFieldsChanged(null!, new TextChangedEventArgs(string.Empty, string.Empty));
    }

    private static void SelectInPicker(Picker picker, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        int index = picker.Items.IndexOf(value);
        if (index >= 0)
        {
            picker.SelectedIndex = index;
        }
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
            ToastService.Success("تم حفظ الصنف ✓ أدخل بيانات الصنف التالي");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (await SaveProductAsync())
        {
            await Shell.Current.GoToAsync("..");
            ToastService.Success("تم حفظ الصنف ✓");
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

        if (_isEditMode)
        {
            product.Id = ProductId;
            bool updated = await ProductRepository.Instance.UpdateAsync(product);
            if (!updated)
            {
                await DisplayAlertAsync("الكود مسجل", "يوجد صنف آخر مسجل بهذا الكود.", "موافق");
                return false;
            }

            return true;
        }

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
        // «حفظ وإضافة جديد» يعيد النموذج لوضع الصنف الجديد
        _isEditMode = false;
        ProductId = 0;

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
