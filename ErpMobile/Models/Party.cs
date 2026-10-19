using SQLite;

namespace ErpMobile.Models;

/// <summary>
/// طرف تجاري: عميل أو مورد — نفس البيانات مع اختلاف النوع.
/// </summary>
[Table("Parties")]
public sealed class Party
{
    public const string Customer = "customer";
    public const string Supplier = "supplier";

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>customer / supplier</summary>
    public string Type { get; set; } = Party.Customer;

    /// <summary>كود الطرف — يُولَّد تلقائياً (C-0001 / S-0001).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string EnglishName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    /// <summary>الرقم الضريبي.</summary>
    public string TaxNumber { get; set; } = string.Empty;

    /// <summary>السجل التجاري.</summary>
    public string CommercialReg { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>نوع السداد: نقدي / آجل.</summary>
    public string PaymentKind { get; set; } = "نقدي";

    /// <summary>رصيد أول المدة (موجب للمورد = له، سالب للعميل = عليه).</summary>
    public double OpeningBalance { get; set; }

    public string Notes { get; set; } = string.Empty;
}
