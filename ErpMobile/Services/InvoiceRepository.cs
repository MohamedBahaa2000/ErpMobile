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
