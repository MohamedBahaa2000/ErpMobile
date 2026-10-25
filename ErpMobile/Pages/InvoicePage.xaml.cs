using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

public partial class InvoicePage : ContentPage
{
    private static readonly double[] PaymentColumnWidths = [40, 110, 150, 95, 115, 170, 45];

    private readonly List<InvoiceLine> _lines = [];
    private readonly List<PaymentLine> _payments = [];

    private int? _invoiceNumber;
    private bool _ready;
    private bool _scanning;
    private int _scanSessionCount;
    private readonly ScannerInputListener? _scannerListener;

    private double _subtotal;
    private double _itemDiscount;
    private double _invoiceDiscount;
    private double _vat;
    private double _total;
    private double _paid;
    private double _remaining;

    private sealed class InvoiceLine
    {
        public Product Product { get; init; } = null!;
        public Label LineNumberLabel { get; set; } = null!;
        public Entry QuantityEntry { get; set; } = null!;
        public Entry PriceEntry { get; set; } = null!;
        public Entry DiscountPercentEntry { get; set; } = null!;
        public Label GrossLabel { get; set; } = null!;
        public Label DiscountAmountLabel { get; set; } = null!;
        public Label NetLabel { get; set; } = null!;
        public Entry NoteEntry { get; set; } = null!;
    }

    private sealed class PaymentLine
    {
        public Label NumberLabel { get; set; } = null!;
        public Picker MethodPicker { get; set; } = null!;
        public Entry AccountEntry { get; set; } = null!;
        public Entry AmountEntry { get; set; } = null!;
        public DatePicker DateEntry { get; set; } = null!;
        public Entry NoteEntry { get; set; } = null!;
    }

    public InvoicePage()
    {
        InitializeComponent();

        StorePicker.ItemsSource = new[] { "Main Store", "مخزن بضاعة أول" };
        StorePicker.SelectedIndex = 0;

        InvoiceTypePicker.ItemsSource = new[] { "نقدي", "آجل", "تقسيط" };
        InvoiceTypePicker.SelectedIndex = 0;

        PriceListPicker.ItemsSource = new[] { "-- اختر --", "سعر الجمهور", "سعر الجملة", "سعر التجزئة" };
        PriceListPicker.SelectedIndex = 0;

        ApproveJournalPicker.ItemsSource = new[] { "معتمد", "غير معتمد" };
        ApproveJournalPicker.SelectedIndex = 0;

        ApproveStorePicker.ItemsSource = new[] { "معتمد", "غير معتمد" };
        ApproveStorePicker.SelectedIndex = 0;

        InvoiceDatePicker.Date = DateTime.Now;

        // ماسح الباركود USB: يلتقط الباركود تلقائياً من غير أي نقر
        _scannerListener = new ScannerInputListener(InvoiceBarcodeEntry, AddProductByBarcodeAsync);

#if WINDOWS
        // على الويندوز يُخفى زرار «مسح» بالكاميرا (مسح الكاميرا للهاتف فقط،
        // وعلى الويندوز يفتح شاشة الكاميرا فتظهر خطأ). الإخفاء هنا لا يؤثر إطلاقاً
        // على أندرويد لأنه كود تجميع منفصل للمنصة.
        ScanBarcodeButton.IsVisible = false;

        // يعيد التركيز لحقل الباركود بعد كل التزام حتى لا تضيع المسحات التالية
        // بعد حوار «الصنف موجود مسبقاً».
        _scannerListener.RefocusAfterCommit = true;
#endif

        _ready = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_payments.Count == 0)
        {
            AddPaymentRow();
        }

        if (_invoiceNumber is null)
        {
            await LoadInvoiceNumberAsync();
        }

        Recalculate();

#if WINDOWS
        // ماسح الباركود USB: نجهّز الحقل لاستقبال الباركود مباشرةً
        ScanStatusLabel.Text = "جاهز لاستقبال ماسح الباركود (USB) — وجّه الماسح وامسح بدون نقر";
        ScanStatusLabel.TextColor = Color.FromArgb("#248A58");
        _ = FocusBarcodeAsync();
#endif
    }

#if WINDOWS
    /// <summary>ننتظر حتى تستقر الصفحة ثم نضع التركيز في حقل الباركود.</summary>
    private async Task FocusBarcodeAsync()
    {
        await Task.Delay(250);
        InvoiceBarcodeEntry.Focus();
    }
#endif

    private async Task LoadInvoiceNumberAsync()
    {
        int number = await InvoiceRepository.Instance.GetNextInvoiceNumberAsync();
        _invoiceNumber = number;
        InvoiceNumberEntry.Text = number.ToString(CultureInfo.InvariantCulture);
    }

    // ---------------------------------------------------------------------
    // الباركود وإضافة البنود
    // ---------------------------------------------------------------------

    private async void OnScanBarcodeClicked(object? sender, EventArgs e)
    {
        if (_scanning)
        {
            // المستخدم يخرج من الماسح ليعود للفاتورة
            BarcodeScanCoordinator.Complete(null);
            return;
        }

        _scanning = true;
        _scanSessionCount = 0;
        ScanBarcodeButton.Text = "إيقاف المسح";
        ScanStatusLabel.TextColor = Color.FromArgb("#7A8299");
        ScanStatusLabel.Text = "المسح المتصل يعمل... امسح الأصناف بالترتيب ثم اضغط «تم ✓ رجوع»";

        try
        {
            // يبقى الماسح مفتوحاً ويضيف كل صنف يُمسح حتى يخرج المستخدم بنفسه
            await BarcodeScanCoordinator.ScanContinuousAsync(AddScannedProductAsync);
        }
        catch (UnauthorizedAccessException)
        {
            await DisplayAlertAsync("إذن الكاميرا", "اسمح للتطبيق باستخدام الكاميرا لمسح الباركود.", "موافق");
        }
        catch (InvalidOperationException)
        {
            // جلسة مسح أخرى شغالة بالفعل
        }
        finally
        {
            _scanning = false;
            ScanBarcodeButton.Text = "مسح";

            if (_scanSessionCount > 0)
            {
                ScanStatusLabel.TextColor = Color.FromArgb("#248A58");
                ScanStatusLabel.Text = $"تمت إضافة {_scanSessionCount} وحدة للفاتورة — اضغط «مسح» للمتابعة";
            }
            else
            {
                ScanStatusLabel.TextColor = Color.FromArgb("#7A8299");
                ScanStatusLabel.Text = "لم يتم إضافة أصناف — المسح المتصل يضيف عدة أصناف حتى تضغط «تم ✓ رجوع»";
            }
        }
    }

    /// <summary>تُستدعى من الماسح مع كل بارود؛ تعيد true للاستمرار في المسح.</summary>
    private async Task<bool> AddScannedProductAsync(string barcode)
    {
        InvoiceBarcodeEntry.Text = barcode;

        Product? product = await ProductRepository.Instance.GetByBarcodeAsync(barcode);
        if (product is null)
        {
            // لا نوقف المسح بسبب باركود غير معروف — نعرضه فقط في شاشة الماسح
            BarcodeScanCoordinator.ReportProgress(_scanSessionCount, $"باركود غير معروف: {barcode} — تم تجاهله");
            BarcodeScanCoordinator.ReportItemAdded("باركود غير معروف", $"{barcode} — لم يُضف", success: false);
            return true;
        }

        int quantity = BarcodeScanCoordinator.QuantityPerScan;
        double? totalQuantity = await AddProductCheckedAsync(product, quantity);

        if (totalQuantity is null)
        {
            // المستخدم رفض إضافة الصنف الموجود مسبقاً — نكمل المسح
            BarcodeScanCoordinator.ReportProgress(_scanSessionCount, $"تم تخطي: {product.Name} — موجود مسبقاً");
            BarcodeScanCoordinator.ReportItemAdded(product.Name, "الصنف موجود مسبقاً — لم تتم الإضافة", success: false);
            return true;
        }

        _scanSessionCount += quantity;
        BarcodeScanCoordinator.ReportProgress(_scanSessionCount, $"تمت إضافة: {product.Name} × {quantity}");
        BarcodeScanCoordinator.ReportItemAdded(
            product.Name,
            $"{product.Barcode} • {FormatAmount(product.Price)} • الكمية {FormatQuantity(totalQuantity.Value)} {product.Unit}",
            success: true);
        return true;
    }

    private async void OnAddBarcodeClicked(object? sender, EventArgs e)
    {
        string barcode = InvoiceBarcodeEntry.Text?.Trim() ?? string.Empty;
        if (barcode.Length == 0)
        {
            await DisplayAlertAsync("الباركود مطلوب", "امسح الباركود أو أدخله أولاً.", "موافق");
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
            await DisplayAlertAsync("المنتج غير موجود", "أضف المنتج أولاً من لوحة التحكم ثم امسح الباركود مرة أخرى.", "موافق");
#if WINDOWS
            InvoiceBarcodeEntry.Focus();
#endif
            return;
        }

        double? totalQuantity = await AddProductCheckedAsync(product, 1);
        InvoiceBarcodeEntry.Text = string.Empty;

        if (totalQuantity is null)
        {
            ScanStatusLabel.TextColor = Color.FromArgb("#C77700");
            ScanStatusLabel.Text = $"{product.Name} موجود مسبقاً — تم إلغاء الإضافة";
            return;
        }

        ScanStatusLabel.TextColor = Color.FromArgb("#248A58");
        ScanStatusLabel.Text = $"{product.Name} × {FormatQuantity(totalQuantity.Value)} — تمت الإضافة";
    }

    // ---------------------------------------------------------------------
    // البحث عن الصنف بالاسم (تحت حقل الباركود)
    // ---------------------------------------------------------------------

    private List<Product>? _allProducts;
    private CancellationTokenSource? _searchCts;

    /// <summary>يجلب كل الأصناف مرة واحدة ويُخزّنها حتى يكون البحث فورياً.</summary>
    private async Task<List<Product>> GetAllProductsCachedAsync()
    {
        if (_allProducts is not null)
        {
            return _allProducts;
        }

        try
        {
            _allProducts = await ProductRepository.Instance.GetAllAsync();
        }
        catch (Exception)
        {
            // انقطاع الخادم — نبحث في القائمة الفارغة بدلاً من تعطيل الصفحة
            _allProducts = [];
        }

        return _allProducts;
    }

    private async void OnItemSearchChanged(object? sender, TextChangedEventArgs e)
    {
        // إلغاء البحث السابق (Debounce) حتى لا يُثقل الواجهة بأوامر متتابعة
        _searchCts?.Cancel();
        _searchCts?.Dispose();

        var cts = new CancellationTokenSource();
        _searchCts = cts;

        try
        {
            await Task.Delay(220, cts.Token);
            await RenderSearchResultsAsync(e.NewTextValue, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // المستخدم ما زال يكتب — ننتظر حتى يهدأ
        }
        catch (Exception)
        {
            // لا ندع خطأ البحث يُسقط الصفحة
        }
    }

    private async Task RenderSearchResultsAsync(string query, CancellationToken token)
    {
        SearchResultsContainer.Children.Clear();

        string term = (query ?? string.Empty).Trim();
        if (term.Length < 2)
        {
            SearchResultsBorder.IsVisible = false;
            return;
        }

        List<Product> all = await GetAllProductsCachedAsync();
        token.ThrowIfCancellationRequested();

        List<Product> matches =
        [
            .. all.Where(p =>
                (!string.IsNullOrEmpty(p.Name) && p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase)))
                  .Take(12)
        ];

        SearchResultsTitle.Text = matches.Count == 0
            ? $"لا توجد نتائج مطابقة لـ «{term}»"
            : $"نتائج البحث ({matches.Count}) — اضغط على الصنف لإضافته";

        foreach (Product product in matches)
        {
            SearchResultsContainer.Children.Add(BuildSearchResultRow(product));
        }

        SearchResultsBorder.IsVisible = true;
    }

    private View BuildSearchResultRow(Product product)
    {
        var nameLabel = new Label
        {
            Text = product.Name,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#192D4A"),
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center
        };

        var metaLabel = new Label
        {
            Text = $"{product.Barcode}  •  {product.Unit}",
            FontSize = 11,
            TextColor = Color.FromArgb("#8A91A6"),
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center
        };

        var priceLabel = new Label
        {
            Text = FormatAmount(product.Price),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#12A15B"),
            VerticalOptions = LayoutOptions.Center
        };

        var addLabel = new Label
        {
            Text = "＋ إضافة",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#25A36F"),
            Padding = new Thickness(10, 5),
            VerticalOptions = LayoutOptions.Center
        };

        var row = new Grid
        {
            Padding = new Thickness(10, 6),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star),
                new(GridLength.Auto),
                new(GridLength.Auto)
            },
            ColumnSpacing = 10,
            FlowDirection = FlowDirection.RightToLeft
        };

        Grid.SetColumn(nameLabel, 0);
        Grid.SetColumn(priceLabel, 1);
        Grid.SetColumn(addLabel, 2);

        var info = new VerticalStackLayout { Spacing = 1, Children = { nameLabel, metaLabel } };
        Grid.SetColumn(info, 0);

        row.Children.Add(info);
        row.Children.Add(priceLabel);
        row.Children.Add(addLabel);

        var border = new Border
        {
            Padding = 0,
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E6EBF4"),
            StrokeThickness = 1
        };
        border.Content = row;

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await AddSearchedProductAsync(product);
        border.GestureRecognizers.Add(tap);

        return border;
    }

    /// <summary>يضيف الصنف المختار من نتائج البحث ثم يُنظّف مربع البحث.</summary>
    private async Task AddSearchedProductAsync(Product product)
    {
        double? totalQuantity = await AddProductCheckedAsync(product, 1);
        if (totalQuantity is null)
        {
            return; // المستخدم رفض إضافة صنف موجود مسبقاً
        }

        SearchResultsBorder.IsVisible = false;
        ItemSearchEntry.Text = string.Empty;
        InvoiceBarcodeEntry.Text = string.Empty;

        ScanStatusLabel.TextColor = Color.FromArgb("#248A58");
        ScanStatusLabel.Text = $"{product.Name} × {FormatQuantity(totalQuantity.Value)} — تمت الإضافة";

#if WINDOWS
        // نُبقي التركيز على الباركود حتى تبقى جلسة المسح USB تعملية
        InvoiceBarcodeEntry.Focus();
#endif
    }

    // ---------------------------------------------------------------------
    // بنود الفاتورة
    // ---------------------------------------------------------------------

    private void AddLine(Product product, int quantity = 1)
    {
        var item = new InvoiceLine { Product = product };

        // ────────── الصف ١: الترويسة ──────────
        // رقم البند داخل «شارة» مدوّرة
        var numberBadge = MakeCardBadge((_lines.Count + 1).ToString(CultureInfo.InvariantCulture));
        item.LineNumberLabel = numberBadge;

        var nameLabel = new Label
        {
            Text = product.Name,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1B2B4B"),
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        var metaLabel = new Label
        {
            Text = $"{product.Barcode} • {product.Unit}",
            FontSize = 10,
            TextColor = Color.FromArgb("#8A91A6"),
            LineBreakMode = LineBreakMode.TailTruncation
        };
        var nameStack = new VerticalStackLayout { Spacing = 1 };
        nameStack.Children.Add(nameLabel);
        nameStack.Children.Add(metaLabel);

        item.NetLabel = MakeCardValue(FormatAmount(product.Price), 15, "#173B70");

        var header = MakeCardGrid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(numberBadge);
        header.Children.Add(nameStack);
        header.Children.Add(item.NetLabel);
        Grid.SetColumn(nameStack, 1);
        Grid.SetColumn(item.NetLabel, 2);

        // ────────── الصف ٢: الحقول القابلة للتعديل ──────────
        item.QuantityEntry = MakeCardEntry(
            quantity.ToString(CultureInfo.InvariantCulture), Keyboard.Numeric, OnInvoiceInputChanged);
        item.PriceEntry = MakeCardEntry(FormatAmount(product.Price), Keyboard.Numeric, OnInvoiceInputChanged);
        item.DiscountPercentEntry = MakeCardEntry("0", Keyboard.Numeric, OnInvoiceInputChanged);
        item.DiscountAmountLabel = MakeCardValue("0.00", 12, "#D9534F");

        View[] fieldControls =
        [
            item.QuantityEntry,
            item.PriceEntry,
            item.DiscountPercentEntry,
            item.DiscountAmountLabel
        ];
        string[] fieldCaptions = ["الكمية", "السعر", "خصم %", "مبلغ الخصم"];

        // أوزان غير متساوية: عمودا «السعر» و«مبلغ الخصم» أعرض لأن أرقامهما أطول،
        // فلا يُقتطع الرقم على الشاشات الضيقة (موبايل).
        GridLength[] fieldWidths =
        [
            new GridLength(0.8, GridUnitType.Star),
            new GridLength(1.2, GridUnitType.Star),
            new GridLength(0.9, GridUnitType.Star),
            new GridLength(1.1, GridUnitType.Star)
        ];

        var fields = MakeCardGrid();
        for (int column = 0; column < fieldControls.Length; column++)
        {
            fields.ColumnDefinitions.Add(new ColumnDefinition { Width = fieldWidths[column] });

            var cell = new VerticalStackLayout { Spacing = 3 };
            cell.Children.Add(new Label
            {
                Text = fieldCaptions[column],
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B7488"),
                HorizontalTextAlignment = TextAlignment.Center
            });
            cell.Children.Add(WrapInFieldBox(fieldControls[column]));

            Grid.SetColumn(cell, column);
            fields.Children.Add(cell);
        }

        // ────────── الصف ٣: الإجمالي + البيان + حذف ──────────
        item.GrossLabel = MakeCardValue(FormatAmount(product.Price), 13, "#1B2B4B");
        item.NoteEntry = MakeCardEntry(string.Empty, Keyboard.Default, null);
        var deleteButton = CreateDeleteButton();

        var grossStack = new HorizontalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center };
        grossStack.Children.Add(new Label
        {
            Text = "قبل الخصم:",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#6B7488"),
            VerticalTextAlignment = TextAlignment.Center
        });
        grossStack.Children.Add(item.GrossLabel);

        var noteBox = WrapInFieldBox(item.NoteEntry);

        var footer = MakeCardGrid();
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.Children.Add(grossStack);
        footer.Children.Add(noteBox);
        footer.Children.Add(deleteButton);
        Grid.SetColumn(noteBox, 1);
        Grid.SetColumn(deleteButton, 2);

        // ────────── تركيب البطاقة ──────────
        var content = new VerticalStackLayout { Spacing = 8 };
        content.Children.Add(header);
        content.Children.Add(fields);
        content.Children.Add(footer);

        var card = new Border
        {
            Padding = new Thickness(12, 10),
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#8582D5"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Content = content
        };

        deleteButton.Clicked += (_, _) =>
        {
            InvoiceItemsContainer.Children.Remove(card);
            _lines.Remove(item);
            RenumberLines();
            EmptyItemsLabel.IsVisible = _lines.Count == 0;
            Recalculate();
        };

        InvoiceItemsContainer.Children.Add(card);
        _lines.Add(item);
        EmptyItemsLabel.IsVisible = false;
        Recalculate();
    }

    // ---------------------------------------------------------------------
    // مكوّنات بطاقة الصنف
    // ---------------------------------------------------------------------

    private static Grid MakeCardGrid() => new()
    {
        ColumnSpacing = 8,
        FlowDirection = FlowDirection.RightToLeft
    };

    private static Label MakeCardBadge(string text) => new()
    {
        Text = text,
        FontSize = 11,
        FontAttributes = FontAttributes.Bold,
        TextColor = Colors.White,
        BackgroundColor = Color.FromArgb("#7B74D6"),
        WidthRequest = 30,
        HeightRequest = 24,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    private static Label MakeCardValue(string text, double fontSize, string color) => new()
    {
        Text = text,
        FontSize = fontSize,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb(color),
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    private static Entry MakeCardEntry(string text, Keyboard keyboard, EventHandler<TextChangedEventArgs>? handler)
    {
        var entry = new Entry
        {
            Text = text,
            FontSize = 12,
            Keyboard = keyboard,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#34445C"),
            HeightRequest = 34,
            HorizontalTextAlignment = TextAlignment.Center
        };

        if (handler is not null)
        {
            entry.TextChanged += handler;
        }

        return entry;
    }

    /// <summary>يضع الحقل داخل إطار مدوّر — بنفس شكل حقول «الخصومات والضرائب».</summary>
    private static Border WrapInFieldBox(View control) => new()
    {
        Padding = new Thickness(6, 0),
        BackgroundColor = Color.FromArgb("#FBFCFE"),
        Stroke = Color.FromArgb("#D5D4EC"),
        StrokeThickness = 1,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
        Content = control
    };

    /// <summary>
    /// يضيف الصنف للفاتورة؛ ولو الباركود موجود مسبقاً <b>يسأل المستخدم</b>
    /// هل يضيفه مرة أخرى (بدل الإضافة التلقائية الصامتة).
    /// يعيد الكمية الإجمالية للسطر، أو null لو المستخدم رفض الإضافة.
    /// </summary>
    private async Task<double?> AddProductCheckedAsync(Product product, int quantity)
    {
        InvoiceLine? existing = _lines.FirstOrDefault(line => line.Product.Barcode == product.Barcode);

        if (existing is not null)
        {
            double currentQuantity = ParseAmount(existing.QuantityEntry.Text);
            string unit = string.IsNullOrWhiteSpace(product.Unit) ? "وحدة" : product.Unit;

            bool addAgain = await DisplayAlertAsync(
                "الصنف موجود مسبقاً",
                $"{product.Name}\n\nالكمية الحالية في الفاتورة: {FormatQuantity(currentQuantity)} {unit}\nهل تريد إضافة {quantity} {unit} أخرى؟",
                "أضف",     
                "إلغاء");

            if (!addAgain)
            {
                return null; // المستخدم اختار «إلغاء»
            }

            double newQuantity = currentQuantity + quantity;
            existing.QuantityEntry.Text = FormatQuantity(newQuantity);
            FlashCell(existing.QuantityEntry);
            Recalculate();
            return newQuantity;
        }

        AddLine(product, quantity);
        return quantity;
    }

    /// <summary>إبراز حقل الكمية لحظياً لتأكيد أن الزيادة وقعت في هذا السطر.</summary>
    private static void FlashCell(Entry cell)
    {
        _ = FlashCellAsync(cell);
    }

    private static async Task FlashCellAsync(Entry cell)
    {
        Color original = cell.BackgroundColor;
        cell.BackgroundColor = Color.FromArgb("#7BE0B0");
        cell.TextColor = Color.FromArgb("#0B3B2A");

        await Task.Delay(450);

        cell.BackgroundColor = original;
        cell.TextColor = Color.FromArgb("#34445C");
    }

    private void RenumberLines()
    {
        for (int index = 0; index < _lines.Count; index++)
        {
            _lines[index].LineNumberLabel.Text = (index + 1).ToString(CultureInfo.InvariantCulture);
        }
    }

    // ---------------------------------------------------------------------
    // السداد
    // ---------------------------------------------------------------------

    private void AddPaymentRow()
    {
        var payment = new PaymentLine();

        var row = CreateRow(PaymentColumnWidths);

        payment.NumberLabel = MakeCell((_payments.Count + 1).ToString(CultureInfo.InvariantCulture));
        payment.MethodPicker = new Picker
        {
            ItemsSource = new[] { "نقد", "شيك", "تحويل بنكي", "آجل" },
            SelectedIndex = 0,
            FontSize = 11,
            HeightRequest = 32,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#34445C")
        };
        payment.AccountEntry = MakeCellEntry("نقدي افتراضي", Keyboard.Default, null);
        payment.AmountEntry = MakeCellEntry("0.00", Keyboard.Numeric, OnInvoiceInputChanged);
        payment.DateEntry = new DatePicker
        {
            Date = DateTime.Now,
            FontSize = 10,
            HeightRequest = 32,
            WidthRequest = 110,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#34445C")
        };
        payment.NoteEntry = MakeCellEntry(string.Empty, Keyboard.Default, null);

        var deleteButton = CreateDeleteButton();

        View[] views =
        [
            payment.NumberLabel,
            payment.MethodPicker,
            payment.AccountEntry,
            payment.AmountEntry,
            payment.DateEntry,
            payment.NoteEntry,
            deleteButton
        ];
        AddColumns(row, views);

        deleteButton.Clicked += (_, _) =>
        {
            PaymentsContainer.Children.Remove(row);
            _payments.Remove(payment);
            RenumberPayments();
            Recalculate();
        };

        PaymentsContainer.Children.Add(row);
        _payments.Add(payment);
        Recalculate();
    }

    private void RenumberPayments()
    {
        for (int index = 0; index < _payments.Count; index++)
        {
            _payments[index].NumberLabel.Text = (index + 1).ToString(CultureInfo.InvariantCulture);
        }
    }

    private async void OnAddPaymentClicked(object? sender, EventArgs e)
    {
        AddPaymentRow();
        await Task.CompletedTask;
    }

    // ---------------------------------------------------------------------
    // عناصر الجداول
    // ---------------------------------------------------------------------

    private static Grid CreateRow(double[] widths)
    {
        var row = new Grid
        {
            Padding = new Thickness(6, 6),
            ColumnSpacing = 2,
            FlowDirection = FlowDirection.RightToLeft
        };

        foreach (double width in widths)
        {
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
        }

        return row;
    }

    private static void AddColumns(Grid row, View[] views)
    {
        for (int column = 0; column < views.Length; column++)
        {
            Grid.SetColumn(views[column], column);
            row.Children.Add(views[column]);
        }
    }

    private static Label MakeCell(string text) => new()
    {
        Text = text,
        FontSize = 11,
        TextColor = Color.FromArgb("#48546B"),
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        LineBreakMode = LineBreakMode.TailTruncation
    };

    private static Entry MakeCellEntry(string text, Keyboard keyboard, EventHandler<TextChangedEventArgs>? handler)
    {
        var entry = new Entry
        {
            Text = text,
            FontSize = 11,
            Keyboard = keyboard,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#34445C"),
            HeightRequest = 34,
            HorizontalTextAlignment = TextAlignment.Center
        };

        if (handler is not null)
        {
            entry.TextChanged += handler;
        }

        return entry;
    }

    private static Button CreateDeleteButton() => new()
    {
        Text = "✕",
        FontSize = 11,
        BackgroundColor = Color.FromArgb("#D9534F"),
        TextColor = Colors.White,
        Padding = 0,
        WidthRequest = 36,
        HeightRequest = 32,
        CornerRadius = 6
    };

    // ---------------------------------------------------------------------
    // الحسابات
    // ---------------------------------------------------------------------

    private void OnInvoiceInputChanged(object? sender, TextChangedEventArgs e) => Recalculate();

    private void OnVatCheckChanged(object? sender, EventArgs e) => Recalculate();

    private void Recalculate()
    {
        if (!_ready || ItemsDiscountLabel is null)
        {
            return;
        }

        _subtotal = 0;
        _itemDiscount = 0;

        foreach (InvoiceLine line in _lines)
        {
            double quantity = ParseAmount(line.QuantityEntry.Text);
            double price = ParseAmount(line.PriceEntry.Text);
            double discountPercent = Math.Clamp(ParseAmount(line.DiscountPercentEntry.Text), 0, 100);

            double gross = quantity * price;
            double discount = gross * discountPercent / 100d;
            double net = gross - discount;

            line.GrossLabel.Text = FormatAmount(gross);
            line.DiscountAmountLabel.Text = FormatAmount(discount);
            line.NetLabel.Text = FormatAmount(net);

            _subtotal += gross;
            _itemDiscount += discount;
        }

        double invoiceDiscountPercent = Math.Clamp(ParseAmount(DiscountPercentEntry.Text), 0, 100);
        _invoiceDiscount = (_subtotal - _itemDiscount) * invoiceDiscountPercent / 100d;

        double beforeVat = _subtotal - _itemDiscount - _invoiceDiscount;
        double vatPercent = VatCheckBox.IsChecked ? Math.Max(0, ParseAmount(VatPercentEntry.Text)) : 0;
        _vat = beforeVat * vatPercent / 100d;
        _total = beforeVat + _vat;

        _paid = 0;
        foreach (PaymentLine payment in _payments)
        {
            _paid += Math.Max(0, ParseAmount(payment.AmountEntry.Text));
        }

        _remaining = _total - _paid;

        ItemsDiscountLabel.Text = FormatAmount(_itemDiscount);
        ExtraDiscountValueLabel.Text = FormatAmount(_invoiceDiscount);
        SubtotalLabel.Text = FormatAmount(_subtotal);
        VatValueLabel.Text = FormatAmount(_vat);
        GrandTotalLabel.Text = FormatAmount(_total);
        if (SummaryGrandTotalLabel is not null)
        {
            SummaryGrandTotalLabel.Text = FormatAmount(_total);
        }

        InvoiceTotalLabel.Text = FormatAmount(_total);
        PaidTotalLabel.Text = FormatAmount(_paid);
        InvoiceRemainingLabel.Text = FormatAmount(_remaining);

        if (InvoiceSummaryHint is not null)
        {
            InvoiceSummaryHint.IsVisible = _lines.Count == 0;
        }
    }

    // ---------------------------------------------------------------------
    // الحفظ والتنقل
    // ---------------------------------------------------------------------

    private async void OnAddNewClicked(object? sender, EventArgs e)
    {
        if (_lines.Count > 0)
        {
            bool proceed = await DisplayAlertAsync(
                "فاتورة جديدة",
                "سيتم إلغاء البنود المسجلة في الفاتورة الحالية. هل تريد المتابعة؟",
                "نعم",
                "لا");

            if (!proceed)
            {
                return;
            }
        }

        await ResetFormAsync();
    }

    private async void OnShowAllClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (await SaveInvoiceAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnSaveAndContinueClicked(object? sender, EventArgs e)
    {
        if (await SaveInvoiceAsync())
        {
            await ResetFormAsync();
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task<bool> SaveInvoiceAsync()
    {
        if (_lines.Count == 0)
        {
            await DisplayAlertAsync("لا توجد بنود", "أضف صنفاً واحداً على الأقل إلى الفاتورة قبل الحفظ.", "موافق");
            return false;
        }

        Recalculate();

        if (_invoiceNumber is null)
        {
            await LoadInvoiceNumberAsync();
        }

        var invoice = new Invoice
        {
            InvoiceNumber = _invoiceNumber!.Value,
            InvoiceDate = (InvoiceDatePicker.Date ?? DateTime.Now).ToString("yyyy-MM-dd"),
            Store = StorePicker.SelectedItem?.ToString() ?? "Main Store",
            InvoiceType = InvoiceTypePicker.SelectedItem?.ToString() ?? "نقدي",
            Customer = string.IsNullOrWhiteSpace(CustomerEntry.Text) ? "عميل نقدي" : CustomerEntry.Text.Trim(),
            SalesRep = string.IsNullOrWhiteSpace(SalesRepEntry.Text) ? "-- اختر --" : SalesRepEntry.Text.Trim(),
            Driver = string.IsNullOrWhiteSpace(DriverEntry.Text) ? "-- اختر --" : DriverEntry.Text.Trim(),
            PriceList = PriceListPicker.SelectedItem?.ToString() ?? "-- اختر --",
            ApproveJournal = ApproveJournalPicker.SelectedItem?.ToString() ?? "معتمد",
            ApproveStoreMove = ApproveStorePicker.SelectedItem?.ToString() ?? "معتمد",
            Subtotal = _subtotal,
            ItemDiscount = _itemDiscount,
            InvoiceDiscount = _invoiceDiscount,
            Vat = _vat,
            Total = _total,
            Paid = _paid,
            Remaining = _remaining,
            DiscountReason = DiscountReasonEntry.Text?.Trim() ?? string.Empty
        };

        List<InvoiceItem> items = _lines
            .Select(line => new InvoiceItem
            {
                Barcode = line.Product.Barcode,
                Name = line.Product.Name,
                Unit = line.Product.Unit,
                Quantity = ParseAmount(line.QuantityEntry.Text),
                Price = ParseAmount(line.PriceEntry.Text),
                DiscountPercent = Math.Clamp(ParseAmount(line.DiscountPercentEntry.Text), 0, 100),
                DiscountAmount = Math.Max(0, ParseAmount(line.QuantityEntry.Text)) *
                                 ParseAmount(line.PriceEntry.Text) *
                                 Math.Clamp(ParseAmount(line.DiscountPercentEntry.Text), 0, 100) / 100d,
                Total = ParseAmount(line.NetLabel.Text),
                Note = line.NoteEntry.Text?.Trim() ?? string.Empty
            })
            .ToList();

        try
        {
            // حفظ على الخادم أولاً ثم نسخها للجهاز (نفس مصدر «كل الفواتير» والداشبورد)
            await InvoiceSource.CreateAsync(invoice, items);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("خطأ في الحفظ", "تعذر حفظ الفاتورة على هذا الجهاز، حاول مرة أخرى.", "موافق");
            return false;
        }

        ToastService.Success(
            $"تم حفظ الفاتورة رقم {invoice.InvoiceNumber} — الإجمالي {FormatAmount(invoice.Total)}، المدفوع {FormatAmount(invoice.Paid)} ✓");

        return true;
    }

    private async Task ResetFormAsync()
    {
        InvoiceItemsContainer.Children.Clear();
        _lines.Clear();
        EmptyItemsLabel.IsVisible = true;

        PaymentsContainer.Children.Clear();
        _payments.Clear();
        AddPaymentRow();

        InvoiceBarcodeEntry.Text = string.Empty;
        if (ScanStatusLabel is not null)
        {
            ScanStatusLabel.TextColor = Color.FromArgb("#7A8299");
            ScanStatusLabel.Text = "المسح المتصل يضيف عدة أصناف متتالية حتى تضغط «تم ✓ رجوع»";
        }
        _scanSessionCount = 0;
        DiscountPercentEntry.Text = "0";
        DiscountReasonEntry.Text = string.Empty;
        VatCheckBox.IsChecked = true;
        VatPercentEntry.Text = "14";
        CustomerEntry.Text = "عميل نقدي";
        CustomerBalanceLabel.Text = "كارت العميل · رصيد سابق: 0.00";
        SalesRepEntry.Text = string.Empty;
        DriverEntry.Text = string.Empty;
        InvoiceDatePicker.Date = DateTime.Now;
        StorePicker.SelectedIndex = 0;
        InvoiceTypePicker.SelectedIndex = 0;
        PriceListPicker.SelectedIndex = 0;
        ApproveJournalPicker.SelectedIndex = 0;
        ApproveStorePicker.SelectedIndex = 0;

        _invoiceNumber = null;
        await LoadInvoiceNumberAsync();
        Recalculate();
    }

    private async void OnSearchCustomerClicked(object? sender, EventArgs e)
    {
        string choice = await DisplayActionSheetAsync(
            "اختيار العميل",
            "إلغاء",
            null,
            "عميل نقدي",
            "عميل نقدي نقدي رئيسي");

        if (string.IsNullOrEmpty(choice) || choice == "إلغاء")
        {
            return;
        }

        CustomerEntry.Text = choice;
        CustomerBalanceLabel.Text = "كارت العميل · رصيد سابق: 0.00";
    }

    // ---------------------------------------------------------------------
    // أدوات
    // ---------------------------------------------------------------------

    private static double ParseAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }

        if (double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            return value;
        }

        return 0;
    }

    private static string FormatAmount(double amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>تنسيق الكمية: عدد صحيح بلا كسور عند القيم الصحيحة، وإلا رقم عشري.</summary>
    private static string FormatQuantity(double quantity) =>
        quantity == Math.Floor(quantity)
            ? quantity.ToString("0", CultureInfo.InvariantCulture)
            : FormatAmount(quantity);
}
