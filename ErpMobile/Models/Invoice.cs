using SQLite;

namespace ErpMobile.Models;

[Table("Invoices")]
public sealed class Invoice
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int InvoiceNumber { get; set; }

    /// <summary>yyyy-MM-dd</summary>
    public string InvoiceDate { get; set; } = string.Empty;

    public string Store { get; set; } = "Main Store";

    public string InvoiceType { get; set; } = "نقدي";

    public string Customer { get; set; } = "عميل نقدي";

    public string SalesRep { get; set; } = "-- اختر --";

    public string Driver { get; set; } = "-- اختر --";

    public string PriceList { get; set; } = "-- اختر --";

    public string ApproveJournal { get; set; } = "معتمد";

    public string ApproveStoreMove { get; set; } = "معتمد";

    public double Subtotal { get; set; }

    public double ItemDiscount { get; set; }

    public double InvoiceDiscount { get; set; }

    public double Vat { get; set; }

    public double Total { get; set; }

    public double Paid { get; set; }

    public double Remaining { get; set; }

    public string DiscountReason { get; set; } = string.Empty;
}

[Table("InvoiceItems")]
public sealed class InvoiceItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = "قطعة";

    public double Quantity { get; set; } = 1;

    public double Price { get; set; }

    public double DiscountPercent { get; set; }

    public double DiscountAmount { get; set; }

    public double Total { get; set; }

    public string Note { get; set; } = string.Empty;
}
