using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

public partial class InvoicePage : ContentPage
{
    private static readonly double[] ItemColumnWidths = [40, 95, 135, 70, 65, 80, 90, 65, 85, 85, 110, 45];
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

    private void AddLine(Product product, int quantity = 1)
    {
        var item = new InvoiceLine { Product = product };

        var row = CreateRow(ItemColumnWidths);

        item.LineNumberLabel = MakeCell((_lines.Count + 1).ToString(CultureInfo.InvariantCulture));
        Label codeLabel = MakeCell(product.Barcode);
        Label nameLabel = MakeCell(product.Name);
        Label unitLabel = MakeCell(product.Unit);
        item.QuantityEntry = MakeCellEntry(
            quantity.ToString(CultureInfo.InvariantCulture), Keyboard.Numeric, OnInvoiceInputChanged);
        item.PriceEntry = MakeCellEntry(FormatAmount(product.Price), Keyboard.Numeric, OnInvoiceInputChanged);
        item.GrossLabel = MakeCell(FormatAmount(product.Price));
        item.DiscountPercentEntry = MakeCellEntry("0", Keyboard.Numeric, OnInvoiceInputChanged);
        item.DiscountAmountLabel = MakeCell("0.00");
        item.NetLabel = MakeCell(FormatAmount(product.Price));
        item.NoteEntry = MakeCellEntry(string.Empty, Keyboard.Default, null);

        var deleteButton = CreateDeleteButton();

        View[] views =
        [
            item.LineNumberLabel,
            codeLabel,
            nameLabel,
            unitLabel,
            item.QuantityEntry,
            item.PriceEntry,
            item.GrossLabel,
            item.DiscountPercentEntry,
            item.DiscountAmountLabel,
            item.NetLabel,
            item.NoteEntry,
            deleteButton
        ];
        AddColumns(row, views);

        deleteButton.Clicked += (_, _) =>
        {
            InvoiceItemsContainer.Children.Remove(row);
            _lines.Remove(item);
            RenumberLines();
            EmptyItemsLabel.IsVisible = _lines.Count == 0;
            Recalculate();
        };

        InvoiceItemsContainer.Children.Add(row);
        _lines.Add(item);
        EmptyItemsLabel.IsVisible = false;
        Recalculate();
    }

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
