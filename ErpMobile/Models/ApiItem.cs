namespace ErpMobile.Models;

/// <summary>
/// صنف كما يتعامل معه الـ API (GET/POST/PUT /api/Items).
/// مطابق لحقل قاعدة بيانات الخادم الـ 17 حقل.
/// كل الحقول nullable لأن الـ API ممكن يرجّع null في بعض الحالات.
/// </summary>
public sealed class ApiItem
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? EnglishName { get; set; }

    public string? Barcode { get; set; }

    public string? HelperCode { get; set; }

    public double? Price { get; set; }

    public double? WholesalePrice { get; set; }

    public double? CostValue { get; set; }

    public string? Unit { get; set; }

    public string? Category { get; set; }

    public string? Manufacturer { get; set; }

    public string? Origin { get; set; }

    public string? CompanyCode { get; set; }

    public string? CostMethod { get; set; }

    public bool? IsStock { get; set; }

    public bool? IsTaxable { get; set; }

    public string? Notes { get; set; }

    // ═══════════════ تحويلات ═══════════════

    /// <summary>تحويل نموذج التطبيق إلى شكل الخادم (للإرسال).</summary>
    public static ApiItem FromProduct(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        EnglishName = product.EnglishName,
        Barcode = product.Barcode,
        HelperCode = product.HelperCode,
        Price = product.Price,
        WholesalePrice = product.WholesalePrice,
        CostValue = product.CostValue,
        Unit = product.Unit,
        Category = product.Category,
        Manufacturer = product.Manufacturer,
        Origin = product.Origin,
        CompanyCode = product.CompanyCode,
        CostMethod = product.CostMethod,
        IsStock = product.IsStock,
        IsTaxable = product.IsTaxable,
        Notes = product.Notes
    };

    /// <summary>تحويل استجابة الخادم إلى نموذج التطبيق (للعرض والحفظ المحلي).</summary>
    public Product ToProduct() => new()
    {
        Id = Id,
        Name = Name ?? string.Empty,
        EnglishName = EnglishName ?? string.Empty,
        Barcode = Barcode ?? string.Empty,
        HelperCode = HelperCode ?? string.Empty,
        Price = Price ?? 0,
        WholesalePrice = WholesalePrice ?? 0,
        CostValue = CostValue ?? 0,
        Unit = string.IsNullOrWhiteSpace(Unit) ? "قطعة" : Unit!,
        Category = Category ?? string.Empty,
        Manufacturer = Manufacturer ?? string.Empty,
        Origin = Origin ?? string.Empty,
        CompanyCode = CompanyCode ?? string.Empty,
        CostMethod = string.IsNullOrWhiteSpace(CostMethod) ? "متوسط مرجح" : CostMethod!,
        IsStock = IsStock ?? true,
        IsTaxable = IsTaxable ?? false,
        Notes = Notes ?? string.Empty
    };
}
