namespace ErpMobile.Models;

/// <summary>
/// بند فاتورة مشتريات كما يتعامل معه الـ API.
/// ⚠️ لا يحتوي على خاصية إسناد للفاتورة — الربط بالعمود InvoiceId فقط
///    (لتجنّب الحلقة الدائرية التي تسبب 500).
/// </summary>
public sealed class ApiPurchaseInvoiceItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public string? Barcode { get; set; }

    public string? Name { get; set; }

    public string? Unit { get; set; }

    public double? Quantity { get; set; }

    public double? Price { get; set; }

    public double? DiscountPercent { get; set; }

    public double? DiscountAmount { get; set; }

    public double? Total { get; set; }

    public string? Note { get; set; }

    // ═══════════════ تحويلات ═══════════════

    public static ApiPurchaseInvoiceItem FromItem(PurchaseInvoiceItem item) => new()
    {
        Id = item.Id,
        InvoiceId = item.InvoiceId,
        Barcode = item.Barcode,
        Name = item.Name,
        Unit = item.Unit,
        Quantity = item.Quantity,
        Price = item.Price,
        DiscountPercent = item.DiscountPercent,
        DiscountAmount = item.DiscountAmount,
        Total = item.Total,
        Note = item.Note
    };

    public PurchaseInvoiceItem ToItem() => new()
    {
        Id = Id,
        InvoiceId = InvoiceId,
        Barcode = Barcode ?? string.Empty,
        Name = Name ?? string.Empty,
        Unit = string.IsNullOrWhiteSpace(Unit) ? "قطعة" : Unit!,
        Quantity = Quantity ?? 0,
        Price = Price ?? 0,
        DiscountPercent = DiscountPercent ?? 0,
        DiscountAmount = DiscountAmount ?? 0,
        Total = Total ?? 0,
        Note = Note ?? string.Empty
    };
}

/// <summary>
/// فاتورة مشتريات كما يتعامل معها الـ API — /api/PurchaseInvoices.
/// مطابق لجدول الخادم ونموذج <see cref="PurchaseInvoice"/> (19 حقل + بنود).
/// </summary>
public sealed class ApiPurchaseInvoice
{
    public int Id { get; set; }

    public int? InvoiceNumber { get; set; }

    /// <summary>yyyy-MM-dd</summary>
    public string? InvoiceDate { get; set; }

    public string? Store { get; set; }

    public string? InvoiceType { get; set; }

    public string? Supplier { get; set; }

    public string? Rep { get; set; }

    public string? Driver { get; set; }

    public string? PriceList { get; set; }

    public string? ApproveJournal { get; set; }

    public string? ApproveStoreMove { get; set; }

    public double? Subtotal { get; set; }

    public double? ItemDiscount { get; set; }

    public double? InvoiceDiscount { get; set; }

    public double? Vat { get; set; }

    public double? Total { get; set; }

    public double? Paid { get; set; }

    public double? Remaining { get; set; }

    public string? DiscountReason { get; set; }

    public List<ApiPurchaseInvoiceItem>? PurchaseInvoiceItems { get; set; }

    // ═══════════════ تحويلات ═══════════════

    /// <summary>تحويل فاتورة التطبيق + بنودها إلى شكل الخادم (للإرسال).</summary>
    public static ApiPurchaseInvoice FromInvoice(
        PurchaseInvoice invoice, IEnumerable<PurchaseInvoiceItem>? items = null) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        InvoiceDate = invoice.InvoiceDate,
        Store = invoice.Store,
        InvoiceType = invoice.InvoiceType,
        Supplier = invoice.Supplier,
        Rep = invoice.Rep,
        Driver = invoice.Driver,
        PriceList = invoice.PriceList,
        ApproveJournal = invoice.ApproveJournal,
        ApproveStoreMove = invoice.ApproveStoreMove,
        Subtotal = invoice.Subtotal,
        ItemDiscount = invoice.ItemDiscount,
        InvoiceDiscount = invoice.InvoiceDiscount,
        Vat = invoice.Vat,
        Total = invoice.Total,
        Paid = invoice.Paid,
        Remaining = invoice.Remaining,
        DiscountReason = invoice.DiscountReason,
        PurchaseInvoiceItems = items?.Select(ApiPurchaseInvoiceItem.FromItem).ToList() ?? []
    };

    /// <summary>تحويل استجابة الخادم إلى فاتورة التطبيق.</summary>
    public PurchaseInvoice ToInvoice() => new()
    {
        Id = Id,
        InvoiceNumber = InvoiceNumber ?? 0,
        InvoiceDate = InvoiceDate ?? string.Empty,
        Store = string.IsNullOrWhiteSpace(Store) ? "Main Store" : Store!,
        InvoiceType = string.IsNullOrWhiteSpace(InvoiceType) ? "نقدي" : InvoiceType!,
        Supplier = string.IsNullOrWhiteSpace(Supplier) ? "مورد نقدي" : Supplier!,
        Rep = string.IsNullOrWhiteSpace(Rep) ? "-- اختر --" : Rep!,
        Driver = string.IsNullOrWhiteSpace(Driver) ? "-- اختر --" : Driver!,
        PriceList = string.IsNullOrWhiteSpace(PriceList) ? "-- اختر --" : PriceList!,
        ApproveJournal = string.IsNullOrWhiteSpace(ApproveJournal) ? "معتمد" : ApproveJournal!,
        ApproveStoreMove = string.IsNullOrWhiteSpace(ApproveStoreMove) ? "معتمد" : ApproveStoreMove!,
        Subtotal = Subtotal ?? 0,
        ItemDiscount = ItemDiscount ?? 0,
        InvoiceDiscount = InvoiceDiscount ?? 0,
        Vat = Vat ?? 0,
        Total = Total ?? 0,
        Paid = Paid ?? 0,
        Remaining = Remaining ?? 0,
        DiscountReason = DiscountReason ?? string.Empty
    };

    /// <summary>تحويل بنود الاستجابة إلى بنود التطبيق (مع تصحيح InvoiceId محلياً).</summary>
    public List<PurchaseInvoiceItem> ToItems(int localInvoiceId)
    {
        List<PurchaseInvoiceItem> items = (PurchaseInvoiceItems ?? [])
            .Select(item => item.ToItem())
            .ToList();

        foreach (PurchaseInvoiceItem item in items)
        {
            item.InvoiceId = localInvoiceId;
        }

        return items;
    }
}
