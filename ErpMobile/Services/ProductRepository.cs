using ErpMobile.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace ErpMobile.Services;

public sealed class ProductRepository
{
    private static readonly Lazy<ProductRepository> LazyInstance = new(() => new ProductRepository());
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _initialized;

    public static ProductRepository Instance => LazyInstance.Value;

    private ProductRepository()
    {
        string databasePath = Path.Combine(FileSystem.AppDataDirectory, "erpmobile.db3");
        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task<bool> AddAsync(Product product)
    {
        await InitializeAsync();

        product.Barcode = product.Barcode.Trim();
        Product? existing = await _database
            .Table<Product>()
            .Where(item => item.Barcode == product.Barcode)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            return false;
        }

        await _database.InsertAsync(product);
        return true;
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        await InitializeAsync();

        string normalizedBarcode = barcode.Trim();
        if (normalizedBarcode.Length == 0)
        {
            return null;
        }

        return await _database
            .Table<Product>()
            .Where(item => item.Barcode == normalizedBarcode)
            .FirstOrDefaultAsync();
    }

    /// <summary>كل الأصناف — للتحليلات في الداشبورد.</summary>
    public async Task<List<Product>> GetAllAsync()
    {
        await InitializeAsync();
        return await _database.Table<Product>().OrderBy(item => item.Name).ToListAsync();
    }

    /// <summary>جلب صنف بالمعرّف (للتعديل من صفحة العرض).</summary>
    public async Task<Product?> GetByIdAsync(int id)
    {
        await InitializeAsync();
        return await _database.Table<Product>().Where(item => item.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// تحديث صنف موجود — يسمح بإبقاء الباركود نفسه لهذا الصنف،
    /// ويرفض الباركود إذا كان مسجلاً لصنف آخر.
    /// </summary>
    public async Task<bool> UpdateAsync(Product product)
    {
        await InitializeAsync();

        product.Barcode = product.Barcode.Trim();
        Product? conflict = await _database
            .Table<Product>()
            .Where(item => item.Barcode == product.Barcode && item.Id != product.Id)
            .FirstOrDefaultAsync();

        if (conflict is not null)
        {
            return false;
        }

        await _database.UpdateAsync(product);
        return true;
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

            await _database.CreateTableAsync<Product>();
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
