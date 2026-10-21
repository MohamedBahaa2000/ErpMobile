using ErpMobile.Models;

namespace ErpMobile.Services;

/// <summary>نتيجة جلب الأصناف (من الخادم أو من الجهاز).</summary>
public sealed class ProductSourceResult
{
    public List<Product> Items { get; init; } = [];

    /// <summary>true إذا كانت البيانات مأخوذة من الـ API.</summary>
    public bool FromApi { get; init; }

    /// <summary>رسالة حالة الاتصال (فارغة = لا حاجة لعرض شريط).</summary>
    public string StatusMessage { get; init; } = string.Empty;

    /// <summary>لون شريط الحالة (#RRGGBB).</summary>
    public string StatusColor { get; init; } = "#8A91A6";
}

/// <summary>
/// المصدر الموحّد للأصناف — نقطة الاتصال بين الخادم والجهاز.
///
/// • القراءة: من api/Items إن كان الاستخدام مفعّلاً، وإلا من قاعدة الجهاز.
/// • المرآة: بعد جلب الأصناف من الخادم تُنسَخ إلى قاعدة الجهاز (بواسطة الباركود)،
///   عشان الفواتير والسكانر (اللي بيقرو محلياً) يلاقوا نفس الأصناف.
/// • الكتابة: تتم على الخادم ثم تُنسَخ للجهاز (انظر UpsertLocalAsync).
/// </summary>
public static class ProductSource
{
    public static async Task<ProductSourceResult> GetAsync()
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                List<ApiItem> items = await ApiService.GetItemsAsync();
                List<Product> mapped = items.ConvertAll(i => i.ToProduct());

                // مرآة محلية: أي صنف على الخادم يبقى موجوداً محلياً أيضاً
                await MirrorAllAsync(mapped);

                return new ProductSourceResult
                {
                    Items = mapped,
                    FromApi = true,
                    StatusMessage = $"متصل بالخادم — تم جلب {mapped.Count} صنف من API ✓",
                    StatusColor = "#17A45C"
                };
            }
            catch (Exception)
            {
                // الخادم غير متاح → نعرض بيانات الجهاز مع تنبيه واضح بدل إيقاف التطبيق
                List<Product> local = await LoadLocalAsync();

                return new ProductSourceResult
                {
                    Items = local,
                    FromApi = false,
                    StatusMessage = "الخادم غير متاح — المعروض من بيانات الجهاز",
                    StatusColor = "#D64545"
                };
            }
        }

        return new ProductSourceResult
        {
            Items = await LoadLocalAsync(),
            FromApi = false,
            StatusMessage = string.Empty,
            StatusColor = "#8A91A6"
        };
    }

    // ═══════════════ المرآة المحلية ═══════════════

    /// <summary>
    /// نسخ/تحديث صنف داخل قاعدة الجهاز **بواسطة الباركود** (المفتاح الطبيعي).
    /// لا تعدّل Id على الكائن الأصلي — لأنه معرّف الخادم ولازم يفضل كما هو.
    /// </summary>
    public static async Task UpsertLocalAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Barcode))
        {
            return;
        }

        try
        {
            Product? existing = await ProductRepository.Instance.GetByBarcodeAsync(product.Barcode);

            if (existing is null)
            {
                await ProductRepository.Instance.AddAsync(Clone(product, 0));
            }
            else
            {
                await ProductRepository.Instance.UpdateAsync(Clone(product, existing.Id));
            }
        }
        catch (Exception)
        {
            // فشل المرآة المحلية لا يجب أن يمنع العملية الأساسية
        }
    }

    /// <summary>نسخ كل أصناف الخادم إلى قاعدة الجهاز (بمفتاح الباركود).</summary>
    private static async Task MirrorAllAsync(List<Product> items)
    {
        foreach (Product item in items)
        {
            await UpsertLocalAsync(item);
        }
    }

    /// <summary>إنشاء نسخة جديدة من الصنف برقم تذكاري مختلف (حماية من تلوّث Id الخادم).</summary>
    private static Product Clone(Product source, int id) => new()
    {
        Id = id,
        Barcode = source.Barcode,
        Name = source.Name,
        Price = source.Price,
        Unit = source.Unit,
        EnglishName = source.EnglishName,
        HelperCode = source.HelperCode,
        Origin = source.Origin,
        Category = source.Category,
        Manufacturer = source.Manufacturer,
        CompanyCode = source.CompanyCode,
        CostMethod = source.CostMethod,
        CostValue = source.CostValue,
        WholesalePrice = source.WholesalePrice,
        IsStock = source.IsStock,
        IsTaxable = source.IsTaxable,
        Notes = source.Notes
    };

    private static async Task<List<Product>> LoadLocalAsync()
    {
        try
        {
            return await ProductRepository.Instance.GetAllAsync();
        }
        catch (Exception)
        {
            return [];
        }
    }
}
