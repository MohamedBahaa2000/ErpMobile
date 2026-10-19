using ErpMobile.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace ErpMobile.Services;

/// <summary>
/// مستودع العملاء والموردين — نفس البيانات مع اختلاف النوع (Party.Type).
/// </summary>
public sealed class PartyRepository
{
    private static readonly Lazy<PartyRepository> LazyInstance = new(() => new PartyRepository());
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile bool _initialized;

    public static PartyRepository Instance => LazyInstance.Value;

    private PartyRepository()
    {
        string databasePath = Path.Combine(FileSystem.AppDataDirectory, "erpmobile.db3");
        _database = new SQLiteAsyncConnection(databasePath);
    }

    /// <summary>إضافة طرف جديد — يولّد كوداً تلقائياً حسب النوع (C-0001 للعميل / S-0001 للمورد).</summary>
    public async Task<bool> AddAsync(Party party)
    {
        await InitializeAsync();

        party.Name = party.Name.Trim();
        if (party.Name.Length == 0)
        {
            return false;
        }

        Party? existing = await _database
            .Table<Party>()
            .Where(item => item.Type == party.Type && item.Name == party.Name)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            return false;
        }

        party.Code = await NextCodeAsync(party.Type);
        await _database.InsertAsync(party);
        return true;
    }

    public async Task UpdateAsync(Party party)
    {
        await InitializeAsync();
        await _database.UpdateAsync(party);
    }

    public async Task DeleteAsync(Party party)
    {
        await InitializeAsync();
        await _database.DeleteAsync(party);
    }

    /// <summary>قائمة الأطراف حسب النوع، مع بحث اختياري بالاسم/الكود/الموبايل.</summary>
    public async Task<List<Party>> GetAllAsync(string type, string? search = null)
    {
        await InitializeAsync();

        List<Party> parties = await _database
            .Table<Party>()
            .Where(item => item.Type == type)
            .OrderBy(item => item.Name)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string needle = search.Trim();
            parties = parties
                .Where(item =>
                    item.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Code.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Mobile.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    item.Phone.Contains(needle, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return parties;
    }

    public async Task<Party?> GetByIdAsync(int id)
    {
        await InitializeAsync();
        return await _database.Table<Party>().Where(item => item.Id == id).FirstOrDefaultAsync();
    }

    private async Task<string> NextCodeAsync(string type)
    {
        string prefix = type == Party.Supplier ? "S" : "C";

        List<Party> all = await _database.Table<Party>().Where(item => item.Type == type).ToListAsync();
        int max = 0;
        foreach (Party party in all)
        {
            string code = party.Code ?? string.Empty;
            int dash = code.LastIndexOf('-');
            if (dash > 0 && int.TryParse(code[(dash + 1)..], out int number))
            {
                max = Math.Max(max, number);
            }
        }

        return $"{prefix}-{(max + 1).ToString("D4")}";
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

            await _database.CreateTableAsync<Party>();
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
