using ErpMobile.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace ErpMobile.Services;

/// <summary>مستودع فواتير المشتريات — ترقيم مستقل يبدأ من 2001.</summary>
public sealed class PurchaseInvoiceRepository
{
    private const int FirstInvoiceNumber = 2001;

    private static readonly Lazy<PurchaseInvoiceRepository> LazyInstance = new(() => new PurchaseInvoiceRepository());
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _initialized;

    public static PurchaseInvoiceRepository Instance => LazyInstance.Value;

    private PurchaseInvoiceRepository()
    {
        string databasePath = Path.Combine(FileSystem.AppDataDirectory, "erpmobile.db3");
        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task<int> GetNextInvoiceNumberAsync()
    {
        await InitializeAsync();

        PurchaseInvoice? last = await _database
            .Table<PurchaseInvoice>()
            .OrderByDescending(invoice => invoice.Id)
            .FirstOrDefaultAsync();

        return last is null ? FirstInvoiceNumber : Math.Max(FirstInvoiceNumber, last.InvoiceNumber + 1);
    }

    public async Task SaveInvoiceAsync(PurchaseInvoice invoice, IEnumerable<PurchaseInvoiceItem> items)
    {
        await InitializeAsync();

        await _database.InsertAsync(invoice);

        foreach (PurchaseInvoiceItem item in items)
        {
            item.InvoiceId = invoice.Id;
            await _database.InsertAsync(item);
        }
    }

    // ═══════════════ عمليات تستخدمها المرآة المحلية (PurchaseSource) ═══════════════

    /// <summary>إدراج فاتورة مشتريات وإعادة رقمها التذكاري المحلي.</summary>
    public async Task<int> InsertAsync(PurchaseInvoice invoice)
    {
        await InitializeAsync();
        await _database.InsertAsync(invoice);
        return invoice.Id;
    }

    public async Task UpdateAsync(PurchaseInvoice invoice)
    {
        await InitializeAsync();
        await _database.UpdateAsync(invoice);
    }

    public async Task InsertItemAsync(PurchaseInvoiceItem item)
    {
        await InitializeAsync();
        await _database.InsertAsync(item);
    }

    /// <summary>البحث عن فاتورة مشتريات برقمها (المفتاح الطبيعي في المرآة المحلية).</summary>
    public async Task<PurchaseInvoice?> GetByNumberAsync(int invoiceNumber)
    {
        await InitializeAsync();
        return await _database
            .Table<PurchaseInvoice>()
            .Where(invoice => invoice.InvoiceNumber == invoiceNumber)
            .FirstOrDefaultAsync();
    }

    /// <summary>حذف بنود فاتورة مشتريات محلية (قبل إعادة كتابتها من الخادم).</summary>
    public async Task DeleteItemsAsync(int invoiceId)
    {
        await InitializeAsync();
        await _database
            .Table<PurchaseInvoiceItem>()
            .Where(item => item.InvoiceId == invoiceId)
            .DeleteAsync();
    }

    /// <summary>حذف فاتورة مشتريات مع بنودها.</summary>
    public async Task DeleteWithItemsAsync(PurchaseInvoice invoice)
    {
        await InitializeAsync();
        await DeleteItemsAsync(invoice.Id);
        await _database.DeleteAsync(invoice);
    }

    public async Task<List<PurchaseInvoice>> GetRecentAsync(int count = 20)
    {
        await InitializeAsync();

        return await _database
            .Table<PurchaseInvoice>()
            .OrderByDescending(invoice => invoice.Id)
            .Take(count)
            .ToListAsync();
    }

    private async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            await _database.CreateTableAsync<PurchaseInvoice>();
            await _database.CreateTableAsync<PurchaseInvoiceItem>();
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
