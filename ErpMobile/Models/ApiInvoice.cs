namespace ErpMobile.Models;

/// <summary>
/// بنود الفاتورة كما يتعامل معها الـ API.
/// ملاحظة: استجابة الخادم قد تحوي خاصية «invoice» دائرية — لذا لا نمررها في الإرسال.
/// </summary>
public sealed class ApiInvoiceItem
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

    public static ApiInvoiceItem FromItem(InvoiceItem item) => new()
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

    public InvoiceItem ToItem() => new()
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
/// فاتورة مبيعات كما يتعامل معها الـ API — GET/POST/PUT/DELETE /api/Invoices.
/// مطابق لجدول الخادم ونموذج <see cref="Invoice"/> (19 حقل + بنود).
/// </summary>
public sealed class ApiInvoice
{
    public int Id { get; set; }

    public int? InvoiceNumber { get; set; }

    /// <summary>yyyy-MM-dd</summary>
    public string? InvoiceDate { get; set; }

    public string? Store { get; set; }

    public string? InvoiceType { get; set; }

    public string? Customer { get; set; }

    public string? SalesRep { get; set; }

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

    public List<ApiInvoiceItem>? InvoiceItems { get; set; }

    // ═══════════════ تحويلات ═══════════════

    /// <summary>تحويل فاتورة التطبيق + بنودها إلى شكل الخادم (للإرسال).</summary>
    public static ApiInvoice FromInvoice(Invoice invoice, IEnumerable<InvoiceItem>? items = null) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        InvoiceDate = invoice.InvoiceDate,
        Store = invoice.Store,
        InvoiceType = invoice.InvoiceType,
        Customer = invoice.Customer,
        SalesRep = invoice.SalesRep,
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
        InvoiceItems = items?.Select(ApiInvoiceItem.FromItem).ToList() ?? []
    };

    /// <summary>تحويل استجابة الخادم إلى فاتورة التطبيق.</summary>
    public Invoice ToInvoice() => new()
    {
        Id = Id,
        InvoiceNumber = InvoiceNumber ?? 0,
        InvoiceDate = InvoiceDate ?? string.Empty,
        Store = string.IsNullOrWhiteSpace(Store) ? "Main Store" : Store!,
        InvoiceType = string.IsNullOrWhiteSpace(InvoiceType) ? "نقدي" : InvoiceType!,
        Customer = Customer ?? string.Empty,
        SalesRep = string.IsNullOrWhiteSpace(SalesRep) ? "-- اختر --" : SalesRep!,
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
    public List<InvoiceItem> ToItems(int localInvoiceId)
    {
        List<InvoiceItem> items = (InvoiceItems ?? [])
            .Select(item => item.ToItem())
            .ToList();

        foreach (InvoiceItem item in items)
        {
            item.InvoiceId = localInvoiceId;
        }

        return items;
    }
}
