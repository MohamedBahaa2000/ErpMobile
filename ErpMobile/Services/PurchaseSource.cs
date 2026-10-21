using ErpMobile.Models;

namespace ErpMobile.Services;

/// <summary>نتيجة جلب فواتير المشتريات (من الخادم أو من الجهاز).</summary>
public sealed class PurchaseSourceResult
{
    public List<PurchaseInvoice> Items { get; init; } = [];

    /// <summary>بنود كل الفواتير — للتحليلات.</summary>
    public List<PurchaseInvoiceItem> AllItems { get; init; } = [];

    public bool FromApi { get; init; }

    public string StatusMessage { get; init; } = string.Empty;

    public string StatusColor { get; init; } = "#8A91A6";
}

/// <summary>
/// المصدر الموحّد لفواتير المشتريات — نقطة الاتصال بين الخادم والجهاز.
///
/// • القراءة: من api/PurchaseInvoices إن كان مفعّلاً، وإلا من قاعدة الجهاز.
/// • المرآة: تُنسخ الفواتير وبنودها إلى الجهاز بمفتاح «رقم الفاتورة».
/// • الكتابة: تتم على الخادم أولاً ثم تُنسَخ للجهاز.
/// </summary>
public static class PurchaseSource
{
    // ═══════════════ القراءة ═══════════════

    public static async Task<PurchaseSourceResult> GetAsync(int count = 500)
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                List<ApiPurchaseInvoice> api = await ApiService.GetPurchaseInvoicesAsync();
                List<PurchaseInvoice> mapped = api.ConvertAll(i => i.ToInvoice());

                // بنود الفواتير (مع تصحيح InvoiceId ليطابق الجهاز)
                List<PurchaseInvoiceItem> items = [];
                for (int index = 0; index < api.Count; index++)
                {
                    items.AddRange(api[index].ToItems(mapped[index].Id));
                }

                await MirrorAllAsync(mapped, api);

                return new PurchaseSourceResult
                {
                    Items = mapped.OrderByDescending(i => i.Id).Take(count).ToList(),
                    AllItems = items,
                    FromApi = true,
                    StatusMessage = $"متصل بالخادم — {mapped.Count} فاتورة مشتريات من API ✓",
                    StatusColor = "#17A45C"
                };
            }
            catch (Exception)
            {
                return await LoadLocalAsync(count, offline: true);
            }
        }

        return await LoadLocalAsync(count, offline: false);
    }

    // ═══════════════ الكتابة ═══════════════

    /// <summary>حفظ فاتورة مشتريات — على الخادم أولاً ثم على الجهاز.</summary>
    public static async Task<PurchaseInvoice?> CreateAsync(
        PurchaseInvoice invoice, IEnumerable<PurchaseInvoiceItem> items)
    {
        List<PurchaseInvoiceItem> itemList = items.ToList();

        if (ApiConfig.UseApi)
        {
            try
            {
                ApiPurchaseInvoice? saved = await ApiService.CreatePurchaseInvoiceAsync(
                    ApiPurchaseInvoice.FromInvoice(invoice, itemList));

                if (saved is not null)
                {
                    PurchaseInvoice serverInvoice = saved.ToInvoice();
                    await UpsertLocalAsync(serverInvoice, saved.ToItems(0));
                    return serverInvoice;
                }
            }
            catch (Exception)
            {
                // الخادم غير متاح → نحفظ محلياً حتى لا يفقد المستخدم عمله
            }
        }

        await PurchaseInvoiceRepository.Instance.SaveInvoiceAsync(invoice, itemList);
        return invoice;
    }

    /// <summary>حذف فاتورة مشتريات — من الخادم ثم من الجهاز.</summary>
    public static async Task<bool> DeleteAsync(PurchaseInvoice invoice)
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                await ApiService.DeletePurchaseInvoiceAsync(invoice.Id);
            }
            catch (Exception)
            {
                // الخادم غير متاح — نحذف محلياً
            }
        }

        await PurchaseInvoiceRepository.Instance.DeleteWithItemsAsync(invoice);
        return true;
    }

    // ═══════════════ المرآة المحلية ═══════════════

    /// <summary>
    /// نسخ/تحديث فاتورة مشتريات داخل قاعدة الجهاز بمفتاح «رقم الفاتورة»
    /// (لأن Id الخادم لا يطابق Id الجهاز).
    /// </summary>
    public static async Task UpsertLocalAsync(
        PurchaseInvoice invoice, IEnumerable<PurchaseInvoiceItem>? items = null)
    {
        try
        {
            PurchaseInvoice? existing =
                await PurchaseInvoiceRepository.Instance.GetByNumberAsync(invoice.InvoiceNumber);

            int localId;

            if (existing is null)
            {
                localId = await PurchaseInvoiceRepository.Instance.InsertAsync(Clone(invoice, 0));
            }
            else
            {
                localId = existing.Id;
                await PurchaseInvoiceRepository.Instance.UpdateAsync(Clone(invoice, localId));
            }

            if (items is not null)
            {
                await PurchaseInvoiceRepository.Instance.DeleteItemsAsync(localId);

                foreach (PurchaseInvoiceItem item in items)
                {
                    PurchaseInvoiceItem copy = CloneItem(item, localId);
                    await PurchaseInvoiceRepository.Instance.InsertItemAsync(copy);
                }
            }
        }
        catch (Exception)
        {
            // فشل المرآة المحلية لا يجب أن يمنع العملية الأساسية
        }
    }

    private static async Task MirrorAllAsync(
        List<PurchaseInvoice> invoices, List<ApiPurchaseInvoice> api)
    {
        for (int index = 0; index < invoices.Count; index++)
        {
            await UpsertLocalAsync(invoices[index], api[index].ToItems(0));
        }
    }

    private static PurchaseInvoice Clone(PurchaseInvoice source, int id) => new()
    {
        Id = id,
        InvoiceNumber = source.InvoiceNumber,
        InvoiceDate = source.InvoiceDate,
        Store = source.Store,
        InvoiceType = source.InvoiceType,
        Supplier = source.Supplier,
        Rep = source.Rep,
        Driver = source.Driver,
        PriceList = source.PriceList,
        ApproveJournal = source.ApproveJournal,
        ApproveStoreMove = source.ApproveStoreMove,
        Subtotal = source.Subtotal,
        ItemDiscount = source.ItemDiscount,
        InvoiceDiscount = source.InvoiceDiscount,
        Vat = source.Vat,
        Total = source.Total,
        Paid = source.Paid,
        Remaining = source.Remaining,
        DiscountReason = source.DiscountReason
    };

    private static PurchaseInvoiceItem CloneItem(PurchaseInvoiceItem source, int invoiceId) => new()
    {
        Id = 0,
        InvoiceId = invoiceId,
        Barcode = source.Barcode,
        Name = source.Name,
        Unit = source.Unit,
        Quantity = source.Quantity,
        Price = source.Price,
        DiscountPercent = source.DiscountPercent,
        DiscountAmount = source.DiscountAmount,
        Total = source.Total,
        Note = source.Note
    };

    private static async Task<PurchaseSourceResult> LoadLocalAsync(int count, bool offline)
    {
        try
        {
            List<PurchaseInvoice> invoices =
                await PurchaseInvoiceRepository.Instance.GetRecentAsync(count);

            return new PurchaseSourceResult
            {
                Items = invoices,
                FromApi = false,
                StatusMessage = offline
                    ? "الخادم غير متاح — المعروض من بيانات الجهاز"
                    : string.Empty,
                StatusColor = offline ? "#D64545" : "#8A91A6"
            };
        }
        catch (Exception)
        {
            return new PurchaseSourceResult();
        }
    }
}
