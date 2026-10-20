using ErpMobile.Models;

namespace ErpMobile.Services;

/// <summary>نتيجة جلب الفواتير (من الخادم أو من الجهاز).</summary>
public sealed class InvoiceSourceResult
{
    public List<Invoice> Items { get; init; } = [];

    /// <summary>بنود كل الفواتير — للتحليلات (الأصناف الأكثر مبيعاً).</summary>
    public List<InvoiceItem> AllItems { get; init; } = [];

    public bool FromApi { get; init; }

    public string StatusMessage { get; init; } = string.Empty;

    public string StatusColor { get; init; } = "#8A91A6";
}

/// <summary>
/// المصدر الموحّد لفواتير المبيعات — نقطة الاتصال بين الخادم والجهاز.
///
/// • القراءة: من api/Invoices إن كان مفعّلاً، وإلا من قاعدة الجهاز.
/// • المرآة: تُنسخ الفواتير وبنودها إلى الجهاز بمفتاح «رقم الفاتورة».
/// • الكتابة: تتم على الخادم أولاً ثم تُنسَخ للجهاز.
/// </summary>
public static class InvoiceSource
{
    // ═══════════════ القراءة ═══════════════

    public static async Task<InvoiceSourceResult> GetAsync(int count = 500)
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                List<ApiInvoice> api = await ApiService.GetInvoicesAsync();
                List<Invoice> mapped = api.ConvertAll(i => i.ToInvoice());

                // بنود الفواتير (مع تصحيح InvoiceId ليطابق الجهاز)
                List<InvoiceItem> items = [];
                for (int index = 0; index < api.Count; index++)
                {
                    items.AddRange(api[index].ToItems(mapped[index].Id));
                }

                await MirrorAllAsync(mapped, api);

                return new InvoiceSourceResult
                {
                    Items = mapped.OrderByDescending(i => i.Id).Take(count).ToList(),
                    AllItems = items,
                    FromApi = true,
                    StatusMessage = $"متصل بالخادم — {mapped.Count} فاتورة من API ✓",
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

    /// <summary>حفظ فاتورة جديدة — على الخادم أولاً ثم على الجهاز.</summary>
    public static async Task<Invoice?> CreateAsync(Invoice invoice, IEnumerable<InvoiceItem> items)
    {
        List<InvoiceItem> itemList = items.ToList();

        if (ApiConfig.UseApi)
        {
            try
            {
                ApiInvoice? saved = await ApiService.CreateInvoiceAsync(
                    ApiInvoice.FromInvoice(invoice, itemList));

                if (saved is not null)
                {
                    Invoice serverInvoice = saved.ToInvoice();
                    await UpsertLocalAsync(serverInvoice, saved.ToItems(0));
                    return serverInvoice;
                }
            }
            catch (Exception)
            {
                // الخادم غير متاح → نحفظ محلياً حتى لا يفقد المستخدم عمله
            }
        }

        await InvoiceRepository.Instance.SaveInvoiceAsync(invoice, itemList);
        return invoice;
    }

    /// <summary>حذف فاتورة — من الخادم ثم من الجهاز.</summary>
    public static async Task<bool> DeleteAsync(Invoice invoice)
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                await ApiService.DeleteInvoiceAsync(invoice.Id);
            }
            catch (Exception)
            {
                // الخادم غير متاح — نحذف محلياً
            }
        }

        await InvoiceRepository.Instance.DeleteWithItemsAsync(invoice);
        return true;
    }

    // ═══════════════ المرآة المحلية ═══════════════

    /// <summary>
    /// نسخ/تحديث فاتورة داخل قاعدة الجهاز بمفتاح «رقم الفاتورة»
    /// (لأن Id الخادم لا يطابق Id الجهاز).
    /// </summary>
    public static async Task UpsertLocalAsync(Invoice invoice, IEnumerable<InvoiceItem>? items = null)
    {
        try
        {
            Invoice? existing = await InvoiceRepository.Instance.GetByNumberAsync(invoice.InvoiceNumber);
            int localId;

            if (existing is null)
            {
                localId = await InvoiceRepository.Instance.InsertAsync(Clone(invoice, 0));
            }
            else
            {
                localId = existing.Id;
                await InvoiceRepository.Instance.UpdateAsync(Clone(invoice, localId));
            }

            if (items is not null)
            {
                await InvoiceRepository.Instance.DeleteItemsAsync(localId);

                foreach (InvoiceItem item in items)
                {
                    InvoiceItem copy = CloneItem(item, localId);
                    await InvoiceRepository.Instance.InsertItemAsync(copy);
                }
            }
        }
        catch (Exception)
        {
            // فشل المرآة المحلية لا يجب أن يمنع العملية الأساسية
        }
    }

    private static async Task MirrorAllAsync(List<Invoice> invoices, List<ApiInvoice> api)
    {
        for (int index = 0; index < invoices.Count; index++)
        {
            await UpsertLocalAsync(invoices[index], api[index].ToItems(0));
        }
    }

    private static Invoice Clone(Invoice source, int id) => new()
    {
        Id = id,
        InvoiceNumber = source.InvoiceNumber,
        InvoiceDate = source.InvoiceDate,
        Store = source.Store,
        InvoiceType = source.InvoiceType,
        Customer = source.Customer,
        SalesRep = source.SalesRep,
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

    private static InvoiceItem CloneItem(InvoiceItem source, int invoiceId) => new()
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

    private static async Task<InvoiceSourceResult> LoadLocalAsync(int count, bool offline)
    {
        try
        {
            List<Invoice> invoices = await InvoiceRepository.Instance.GetRecentAsync(count);
            List<InvoiceItem> items = await InvoiceRepository.Instance.GetAllItemsAsync();

            return new InvoiceSourceResult
            {
                Items = invoices,
                AllItems = items,
                FromApi = false,
                StatusMessage = offline
                    ? "الخادم غير متاح — المعروض من بيانات الجهاز"
                    : string.Empty,
                StatusColor = offline ? "#D64545" : "#8A91A6"
            };
        }
        catch (Exception)
        {
            return new InvoiceSourceResult();
        }
    }
}
