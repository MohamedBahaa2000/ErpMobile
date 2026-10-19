using SQLite;

namespace ErpMobile.Models;

[Table("Products")]
public sealed class Product
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique]
    public string Barcode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public double Price { get; set; }

    public string Unit { get; set; } = "قطعة";

    // ---- حقول إضافية بنمط نظام ERP (تُضاف تلقائياً لقاعدة البيانات القديمة) ----

    /// <summary>الاسم الأجنبي (إنجليزي).</summary>
    public string EnglishName { get; set; } = string.Empty;

    /// <summary>كود مساعد اختياري.</summary>
    public string HelperCode { get; set; } = string.Empty;

    /// <summary>منشأ الصنف (محلي / مستورد ...).</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>نوع الصنف.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>الشركة المصنعة.</summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>كود الصنف في الشركة المصنعة.</summary>
    public string CompanyCode { get; set; } = string.Empty;

    /// <summary>طريقة احتساب التكلفة.</summary>
    public string CostMethod { get; set; } = "متوسط مرجح";

    /// <summary>قيمة التكلفة النهائية.</summary>
    public double CostValue { get; set; }

    /// <summary>سعر الجملة.</summary>
    public double WholesalePrice { get; set; }

    /// <summary>هل يُدار مخزونه؟.</summary>
    public bool IsStock { get; set; } = true;

    /// <summary>هل يخضع لضريبة القيمة المضافة؟.</summary>
    public bool IsTaxable { get; set; }

    public string Notes { get; set; } = string.Empty;
}
