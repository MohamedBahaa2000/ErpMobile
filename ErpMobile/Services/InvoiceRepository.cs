using ErpMobile.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace ErpMobile.Services;

public sealed class InvoiceRepository
{
    private const int FirstInvoiceNumber = 1001;

    private static readonly Lazy<InvoiceRepository> LazyInstance = new(() => new InvoiceRepository());
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _initialized;

    public static InvoiceRepository Instance => LazyInstance.Value;

    private InvoiceRepository()
    {
        string databasePath = Path.Combine(FileSystem.AppDataDirectory, "erpmobile.db3");
        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task<int> GetNextInvoiceNumberAsync()
    {
        await InitializeAsync();

        Invoice? last = await _database
            .Table<Invoice>()
            .OrderByDescending(invoice => invoice.Id)
            .FirstOrDefaultAsync();

        return last is null ? FirstInvoiceNumber : Math.Max(FirstInvoiceNumber, last.InvoiceNumber + 1);
    }

    public async Task SaveInvoiceAsync(Invoice invoice, IEnumerable<InvoiceItem> items)
    {
        await InitializeAsync();

        await _database.InsertAsync(invoice);

        foreach (InvoiceItem item in items)
        {
            item.InvoiceId = invoice.Id;
            await _database.InsertAsync(item);
        }
    }

    // ═══════════════ عمليات تستخدمها المرآة المحلية (InvoiceSource) ═══════════════

    /// <summary>إدراج فاتورة وإعادة رقمها التذكاري المحلي.</summary>
    public async Task<int> InsertAsync(Invoice invoice)
    {
        await InitializeAsync();
        await _database.InsertAsync(invoice);
        return invoice.Id;
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        await InitializeAsync();
        await _database.UpdateAsync(invoice);
    }

    public async Task InsertItemAsync(InvoiceItem item)
    {
        await InitializeAsync();
        await _database.InsertAsync(item);
    }

    /// <summary>البحث عن فاتورة برقمها (المفتاح الطبيعي في المرآة المحلية).</summary>
    public async Task<Invoice?> GetByNumberAsync(int invoiceNumber)
    {
        await InitializeAsync();
        return await _database
            .Table<Invoice>()
            .Where(invoice => invoice.InvoiceNumber == invoiceNumber)
            .FirstOrDefaultAsync();
    }

    /// <summary>حذف بنود فاتورة محلية (قبل إعادة كتابتها من الخادم).</summary>
    public async Task DeleteItemsAsync(int invoiceId)
    {
        await InitializeAsync();
        await _database
            .Table<InvoiceItem>()
            .Where(item => item.InvoiceId == invoiceId)
            .DeleteAsync();
    }

    /// <summary>حذف فاتورة مع بنودها.</summary>
    public async Task DeleteWithItemsAsync(Invoice invoice)
    {
        await InitializeAsync();
        await DeleteItemsAsync(invoice.Id);
        await _database.DeleteAsync(invoice);
    }

    public async Task<List<Invoice>> GetRecentAsync(int count = 20)
    {
        await InitializeAsync();

        return await _database
            .Table<Invoice>()
            .OrderByDescending(invoice => invoice.Id)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>كل بنود الفواتير — للتحليلات (الأصناف الأكثر مبيعاً).</summary>
    public async Task<List<InvoiceItem>> GetAllItemsAsync()
    {
        await InitializeAsync();
        return await _database.Table<InvoiceItem>().ToListAsync();
    }

    /// <summary>بنود فاتورة واحدة (لعرض تفاصيلها وتعديلها).</summary>
    public async Task<List<InvoiceItem>> GetItemsAsync(int invoiceId)
    {
        await InitializeAsync();
        return await _database
            .Table<InvoiceItem>()
            .Where(item => item.InvoiceId == invoiceId)
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

            await _database.CreateTableAsync<Invoice>();
            await _database.CreateTableAsync<InvoiceItem>();
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
