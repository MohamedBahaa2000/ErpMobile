namespace ErpMobile.Services;

using ErpMobile.Models;

/// <summary>نتيجة جلب الأطراف (من الخادم أو من الجهاز).</summary>
public sealed class PartySourceResult
{
    public List<Party> Items { get; init; } = [];

    /// <summary>true إذا كانت البيانات مأخوذة من الـ API.</summary>
    public bool FromApi { get; init; }

    public string StatusMessage { get; init; } = string.Empty;

    /// <summary>لون شريط الحالة (#RRGGBB).</summary>
    public string StatusColor { get; init; } = "#8A91A6";
}

/// <summary>
/// المصدر الموحّد للعملاء والموردين — نقطة الاتصال بين الخادم والجهاز.
///
/// • القراءة: من api/Parties إن كان مفعّلاً، وإلا من قاعدة الجهاز.
/// • المرآة: تُنسخ سجلات الخادم إلى الجهاز بمفتاح ثابت
///   (الكود إن وُجد، وإلا «النوع + الاسم») عشان الفواتير تلاقى نفس الأطراف.
/// • الكتابة: تتم على الخادم أولاً ثم تُنسَخ للجهاز.
/// </summary>
public static class PartySource
{
    // ═══════════════ القراءة ═══════════════

    /// <summary>
    /// جلب طرف بمعرّفه — المعرّف هنا هو معرّف الخادم (لأن القائمة تأتي من الـ API).
    /// يرجع null إن لم يُوجد.
    /// </summary>
    public static async Task<Party?> GetByIdAsync(int id)
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                List<ApiParty> parties = await ApiService.GetPartiesAsync();
                ApiParty? found = parties.FirstOrDefault(p => p.Id == id);
                if (found is not null)
                {
                    return found.ToParty();
                }
            }
            catch (Exception)
            {
                // الخادم غير متاح → نجرب الجهاز
            }
        }

        return await PartyRepository.Instance.GetByIdAsync(id);
    }

    public static async Task<PartySourceResult> GetAsync(string type, string? search = null)
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                List<ApiParty> parties = await ApiService.GetPartiesAsync();
                List<Party> mapped = parties
                    .ConvertAll(p => p.ToParty())
                    .Where(p => p.Type == type)
                    .ToList();

                await MirrorAllAsync(mapped);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    mapped = Filter(mapped, search);
                }

                return new PartySourceResult
                {
                    Items = mapped,
                    FromApi = true,
                    StatusMessage = $"متصل بالخادم — {mapped.Count} سجل من API ✓",
                    StatusColor = "#17A45C"
                };
            }
            catch (Exception)
            {
                List<Party> local = await LoadLocalAsync(type, search);

                return new PartySourceResult
                {
                    Items = local,
                    FromApi = false,
                    StatusMessage = "الخادم غير متاح — المعروض من بيانات الجهاز",
                    StatusColor = "#D64545"
                };
            }
        }

        return new PartySourceResult
        {
            Items = await LoadLocalAsync(type, search),
            FromApi = false,
            StatusMessage = string.Empty,
            StatusColor = "#8A91A6"
        };
    }

    // ═══════════════ الكتابة ═══════════════

    /// <summary>إضافة طرف جديد — على الخادم ثم على الجهاز. يعيد الطرف المحفوظ أو null عند الفشل.</summary>
    public static async Task<Party?> CreateAsync(Party party)
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                ApiParty? saved = await ApiService.CreatePartyAsync(ApiParty.FromParty(party));
                if (saved is not null)
                {
                    await UpsertLocalAsync(saved.ToParty());
                    return saved.ToParty();
                }
            }
            catch (Exception)
            {
                // الخادم غير متاح → نحفظ محلياً حتى لا يفقد المستخدم عمله
            }
        }

        bool ok = await PartyRepository.Instance.AddAsync(party);
        return ok ? party : null;
    }

    /// <summary>تعديل طرف — على الخادم ثم على الجهاز. يعيد true عند النجاح.</summary>
    public static async Task<bool> UpdateAsync(Party party)
    {
        if (ApiConfig.UseApi)
        {
            try
            {
                await ApiService.UpdatePartyAsync(party.Id, ApiParty.FromParty(party));
                await UpsertLocalAsync(party);
                return true;
            }
            catch (Exception)
            {
                // الخادم غير متاح → نعدّل محلياً
            }
        }

        await PartyRepository.Instance.UpdateAsync(party);
        return true;
    }

    /// <summary>حذف طرف — من الخادم ثم من الجهاز.</summary>
    public static async Task<bool> DeleteAsync(Party party)
    {
        bool deleted = false;

        if (ApiConfig.UseApi)
        {
            try
            {
                await ApiService.DeletePartyAsync(party.Id);
                deleted = true;
            }
            catch (Exception)
            {
                // الخادم غير متاح — نحذف محلياً
            }
        }

        await PartyRepository.Instance.DeleteAsync(party);
        return true;
    }

    // ═══════════════ المرآة المحلية ═══════════════

    /// <summary>
    /// نسخ/تحديث طرف داخل قاعدة الجهاز بمفتاح ثابت: الكود إن وُجد، وإلا «النوع + الاسم».
    /// لا يعدّل Id على الكائن الأصلي — لأنه معرّف الخادم ولازم يبقى كما هو.
    /// </summary>
    public static async Task UpsertLocalAsync(Party party)
    {
        if (string.IsNullOrWhiteSpace(party.Name))
        {
            return;
        }

        try
        {
            List<Party> all = await PartyRepository.Instance.GetAllAsync(party.Type);
            Party? existing = FindByKey(all, party);

            if (existing is null)
            {
                Party copy = Clone(party, 0);
                await PartyRepository.Instance.AddAsync(copy);
            }
            else
            {
                await PartyRepository.Instance.UpdateAsync(Clone(party, existing.Id));
            }
        }
        catch (Exception)
        {
            // فشل المرآة المحلية لا يجب أن يمنع العملية الأساسية
        }
    }

    private static async Task MirrorAllAsync(List<Party> parties)
    {
        foreach (Party party in parties)
        {
            await UpsertLocalAsync(party);
        }
    }

    /// <summary>البحث عن طرف مطابق بمفتاحه الثابت (الكود أو النوع + الاسم).</summary>
    private static Party? FindByKey(IEnumerable<Party> all, Party target)
    {
        foreach (Party party in all)
        {
            if (!string.IsNullOrWhiteSpace(target.Code) &&
                string.Equals(party.Code, target.Code, StringComparison.OrdinalIgnoreCase))
            {
                return party;
            }
        }

        if (!string.IsNullOrWhiteSpace(target.Code))
        {
            return null; // له كود لكن غير موجود → سجل جديد
        }

        return all.FirstOrDefault(p =>
            string.Equals(p.Name?.Trim(), target.Name?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>إنشاء نسخة جديدة برقم تذكاري مختلف (حماية من تلوّث Id الخادم).</summary>
    private static Party Clone(Party source, int id) => new()
    {
        Id = id,
        Type = source.Type,
        Code = source.Code,
        Name = source.Name,
        EnglishName = source.EnglishName,
        Phone = source.Phone,
        Mobile = source.Mobile,
        Address = source.Address,
        TaxNumber = source.TaxNumber,
        CommercialReg = source.CommercialReg,
        Email = source.Email,
        PaymentKind = source.PaymentKind,
        OpeningBalance = source.OpeningBalance,
        Notes = source.Notes
    };

    private static List<Party> Filter(List<Party> parties, string search)
    {
        string needle = search.Trim();
        return parties
            .Where(item =>
                item.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                item.Code.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                item.Mobile.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                item.Phone.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static async Task<List<Party>> LoadLocalAsync(string type, string? search)
    {
        try
        {
            return await PartyRepository.Instance.GetAllAsync(type, search);
        }
        catch (Exception)
        {
            return [];
        }
    }
}
