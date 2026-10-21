using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

[QueryProperty(nameof(ProductId), "id")]
[QueryProperty(nameof(ItemSource), "src")]
[QueryProperty(nameof(ItemBarcode), "bc")]
public partial class ProductPage : ContentPage
{
    private const string ChooseItem = "-- اختر --";

    /// <summary>معرّف الصنف المراد تعديله — 0 لصنف جديد.</summary>
    public int ProductId { get; set; }

    /// <summary>من أين جاء الصنف: "api" (معرّف الخادم) أو "local" (معرّف الجهاز).</summary>
    public string ItemSource { get; set; } = string.Empty;

    /// <summary>باركود الصنف — يُستخدم للبحث المحلي الدقيق لو فشل الاتصال بالخادم.</summary>
    public string ItemBarcode { get; set; } = string.Empty;

    private bool _isEditMode;

    /// <summary>true إذا كان الصنف مسجلاً على الخادم (نستخدم PUT بدل التحديث المحلي).</summary>
    private bool _fromApi;

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

        // توضيح طريقة الحفظ الحالية للمستخدم
        StorageHintLabel.Text = ApiConfig.UseApi
            ? "سيتم حفظ الصنف على الخادم ومزامنته على الجهاز. عند انقطاع الاتصال يُحفظ محلياً وتتم المزامنة عند العودة."
            : "سيتم حفظ الأصناف محلياً على هذا الجهاز، ويمكن استخدامها دون إنترنت.";

        // تحميل الصنف المراد تعديله مرة واحدة
        if (!_loaded && ProductId > 0)
        {
            _loaded = true;
            await LoadProductAsync();
        }
    }

    private async Task LoadProductAsync()
    {
        Product? product = null;
        _fromApi = false;

        // 1) إن جاء الصنف من قائمة الخادم → نجلبه من api/Items/{id}
        if (ApiConfig.UseApi && ItemSource == "api")
        {
            try
            {
                ApiItem? item = await ApiService.GetItemAsync(ProductId);
                if (item is not null)
                {
                    product = item.ToProduct();
                    _fromApi = true;
                }
            }
            catch (Exception)
            {
                // الخادم غير متاح الآن → ننتقل للخطوة 2 (بحث محلي دقيق بالباركود)
            }
        }

        // 2) مسار محلي: بحث بالباركود (دقيق) ثم بالرقم
        if (product is null)
        {
            _fromApi = false;

            if (!string.IsNullOrWhiteSpace(ItemBarcode))
            {
                product = await ProductRepository.Instance.GetByBarcodeAsync(ItemBarcode);
            }

            product ??= await ProductRepository.Instance.GetByIdAsync(ProductId);
        }

        if (product is null)
        {
            return;
        }

        _isEditMode = true;
        DeleteButton.IsVisible = true;

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

    // ---------------------------------------------------------------------
    // الحذف (خادم + جهاز)
    // ---------------------------------------------------------------------

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (!_isEditMode)
        {
            return;
        }

        string name = NameEntry.Text?.Trim() ?? string.Empty;
        bool confirm = await DisplayAlertAsync(
            "حذف الصنف",
            $"هل تريد حذف «{name}» نهائياً؟",
            "نعم، احذف",
            "إلغاء");

        if (!confirm)
        {
            return;
        }

        try
        {
            // 1) الحذف من الخادم إن كان الصنف مسجّلاً هناك
            if (ApiConfig.UseApi && _fromApi)
            {
                await ApiService.DeleteItemAsync(ProductId);
            }

            // 2) حذف النسخة المحلية إن وُجدت (بالباركود)
            if (!string.IsNullOrWhiteSpace(ItemBarcode))
            {
                Product? local = await ProductRepository.Instance.GetByBarcodeAsync(ItemBarcode);
                if (local is not null)
                {
                    await ProductRepository.Instance.DeleteAsync(local.Id);
                }
            }

            ToastService.Success("تم حذف الصنف ✓");
            await Shell.Current.GoToAsync("..");
        }
        catch (ApiException ex)
        {
            await DisplayAlertAsync("تعذّر الحذف", ex.Message, "موافق");
        }
        catch (Exception)
        {
            await DisplayAlertAsync("تعذّر الحذف", "حدث خطأ أثناء الحذف من الخادم.", "موافق");
        }
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

        // ── 1) وضع الخادم: الكتابة على api/Items ثم نسخها للجهاز (مرآة) ──
        if (ApiConfig.UseApi)
        {
            try
            {
                if (_isEditMode && _fromApi)
                {
                    // تعديل صنف مسجّل على الخادم
                    product.Id = ProductId;
                    await ApiService.UpdateItemAsync(ProductId, ApiItem.FromProduct(product));
                }
                else
                {
                    // صنف جديد — أو صنف محلي غير موجود على الخادم → يُنشأ هناك
                    product.Id = 0;
                    ApiItem? created = await ApiService.CreateItemAsync(ApiItem.FromProduct(product));

                    if (created is not null && created.Id > 0)
                    {
                        ProductId = created.Id;
                        ItemSource = "api";
                        ItemBarcode = product.Barcode;
                        _fromApi = true;
                        _isEditMode = true;
                    }
                }

                // مرآة محلية بالباركود (عشان الفواتير والسكانر يلاقوه)
                await ProductSource.UpsertLocalAsync(product);
                return true;
            }
            catch (ApiException ex) when (ex.StatusCode == 0)
            {
                // الخادم غير متاح فعلاً → حفظ محلي حتى لا يضيع عمل المستخدم
                await ProductSource.UpsertLocalAsync(product);
                ToastService.Warning("الخادم غير متاح — تم الحفظ محلياً وسيُزامَن عند عودته");
                return true;
            }
            catch (ApiException ex)
            {
                // رفض من الخادم (مثلاً: باركود مكرر) → نعرض رسالته كما هي
                string msg = string.IsNullOrWhiteSpace(ex.Message)
                    ? "تعذّر حفظ الصنف على الخادم."
                    : ex.Message;
                await DisplayAlertAsync("رفض الخادم", msg, "موافق");
                return false;
            }
        }

        // ── 2) وضع محلي (ApiConfig.UseApi = false) ──
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
        _fromApi = false;
        ProductId = 0;
        ItemSource = string.Empty;
        ItemBarcode = string.Empty;
        DeleteButton.IsVisible = false;

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
