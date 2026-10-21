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
