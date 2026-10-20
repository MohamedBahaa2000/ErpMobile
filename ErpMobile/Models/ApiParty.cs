namespace ErpMobile.Models;

/// <summary>
/// طرف تجاري (عميل/مورد) كما يتعامل معه الـ API — GET/POST/PUT/DELETE /api/Parties.
/// مطابق تماماً لجدول الخادم ونموذج <see cref="Party"/> في التطبيق (14 حقل).
/// الحقول nullable لأن الـ API ممكن يرجّع null في بعض الحالات.
/// </summary>
public sealed class ApiParty
{
    public int Id { get; set; }

    /// <summary>customer / supplier</summary>
    public string? Type { get; set; }

    /// <summary>الكود المولّد: C-0001 للعميل / S-0001 للمورد.</summary>
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? EnglishName { get; set; }

    public string? Phone { get; set; }

    public string? Mobile { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public string? CommercialReg { get; set; }

    public string? Email { get; set; }

    /// <summary>نقدي / آجل.</summary>
    public string? PaymentKind { get; set; }

    public double? OpeningBalance { get; set; }

    public string? Notes { get; set; }

    // ═══════════════ تحويلات ═══════════════

    /// <summary>تحويل نموذج التطبيق إلى شكل الخادم (للإرسال).</summary>
    public static ApiParty FromParty(Party party) => new()
    {
        Id = party.Id,
        Type = party.Type,
        Code = party.Code,
        Name = party.Name,
        EnglishName = party.EnglishName,
        Phone = party.Phone,
        Mobile = party.Mobile,
        Address = party.Address,
        TaxNumber = party.TaxNumber,
        CommercialReg = party.CommercialReg,
        Email = party.Email,
        PaymentKind = party.PaymentKind,
        OpeningBalance = party.OpeningBalance,
        Notes = party.Notes
    };

    /// <summary>تحويل استجابة الخادم إلى نموذج التطبيق (للعرض والحفظ المحلي).</summary>
    public Party ToParty() => new()
    {
        Id = Id,
        Type = string.IsNullOrWhiteSpace(Type) ? Party.Customer : Type!,
        Code = Code ?? string.Empty,
        Name = Name ?? string.Empty,
        EnglishName = EnglishName ?? string.Empty,
        Phone = Phone ?? string.Empty,
        Mobile = Mobile ?? string.Empty,
        Address = Address ?? string.Empty,
        TaxNumber = TaxNumber ?? string.Empty,
        CommercialReg = CommercialReg ?? string.Empty,
        Email = Email ?? string.Empty,
        PaymentKind = string.IsNullOrWhiteSpace(PaymentKind) ? "نقدي" : PaymentKind!,
        OpeningBalance = OpeningBalance ?? 0,
        Notes = Notes ?? string.Empty
    };
}
