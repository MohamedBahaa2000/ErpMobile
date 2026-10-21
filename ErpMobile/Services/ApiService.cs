using ErpMobile.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ErpMobile.Services;

// ═══════════════ نماذج المصادقة ═══════════════
public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string? Token { get; set; }
    public string? UserName { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// الدخول ناجح لو رجع توكن، أو رجع مستخدماً/رسالة نجاح.
    /// ملاحظة: أخطاء الدخول (401) تُعالج قبل الوصول هنا، فأي استجابة 2xx هنا نجاح.
    /// </summary>
    public bool Success =>
        !string.IsNullOrEmpty(Token) ||
        !string.IsNullOrEmpty(UserName) ||
        !string.IsNullOrEmpty(Message);
}

// ═══════════════ استثناء الـ API ═══════════════
public sealed class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode = 0, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// ═══════════════════════════════════════════════════════════
///  خدمة الـ HTTP الموحّدة — كل العمليات تمرّ من هنا.
///  الروابط كلها في <see cref="ApiConfig"/>.
/// ═══════════════════════════════════════════════════════════
/// </summary>
public static class ApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,

        // الـ API متوقع camelCase (name / price / unit ...) — بنفس صيغة استجاباته
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>إنشاء عميل HTTP بالإعدادات المركزة.</summary>
    public static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler();
        if (ApiConfig.AllowSelfSigned)
        {
            handler.ServerCertificateCustomValidationCallback =
                (_, _, _, _) => true;
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl),
            Timeout = TimeSpan.FromSeconds(ApiConfig.TimeoutSeconds)
        };

        if (!string.IsNullOrWhiteSpace(ApiConfig.ApiKey))
        {
            client.DefaultRequestHeaders.Add(ApiConfig.ApiKeyHeader, ApiConfig.ApiKey);
        }

        string token = GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    // ═══════════════ التوكن ═══════════════
    public static string GetToken() =>
        Preferences.Default.Get(ApiConfig.TokenStorageKey, string.Empty);

    public static void SetToken(string token) =>
        Preferences.Default.Set(ApiConfig.TokenStorageKey, token);

    public static void ClearToken() =>
        Preferences.Default.Remove(ApiConfig.TokenStorageKey);

    // ═══════════════ تسجيل الدخول ═══════════════
    /// <summary>
    /// إرسال طلب تسجيل الدخول إلى الخادم.
    /// يعيد LoginResponse، أو null إذا كان الـ API مُعطّلاً في ApiConfig.
    /// يرمي ApiException عند فشل الاتصال أو رفض الخادم.
    /// </summary>
    public static async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        if (!ApiConfig.UseApi)
        {
            return null; // وضع محلي — بدون خادم
        }

        var payload = new LoginRequest { Username = username, Password = password };

        using HttpClient client = CreateClient();
        string json = JsonSerializer.Serialize(payload, JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(
                ApiConfig.LoginEndpoint,
                new StringContent(json, Encoding.UTF8, "application/json"));
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException("انتهت مهلة الاتصال بالخادم، تحقق من الإنترنت.", 0, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"تعذّر الاتصال بالخادم: {ex.Message}", 0, ex);
        }

        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            string msg = ExtractMessage(body);
            throw new ApiException(
                string.IsNullOrWhiteSpace(msg) ? "اسم المستخدم أو كلمة المرور غير صحيحة." : msg,
                (int)response.StatusCode);
        }

        LoginResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ApiException("استجابة غير متوقعة من الخادم.", (int)response.StatusCode, ex);
        }

        if (result is not null && result.Success)
        {
            // بعض إصدارات الـ API لا تُرجع توكن (رسالة نجاح فقط) — نحفظ فارغاً بدل null
            SetToken(result.Token ?? string.Empty);
        }

        return result;
    }

    // ═══════════════ عمليات عامة ═══════════════
    public static async Task<T?> GetAsync<T>(string endpoint)
    {
        using HttpClient client = CreateClient();
        HttpResponseMessage response = await client.GetAsync(endpoint);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(ExtractMessage(body), (int)response.StatusCode);
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    public static async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint, TRequest payload)
    {
        using HttpClient client = CreateClient();
        string json = JsonSerializer.Serialize(payload, JsonOptions);

        HttpResponseMessage response = await client.PostAsync(
            endpoint,
            new StringContent(json, Encoding.UTF8, "application/json"));

        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(ExtractMessage(body), (int)response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TResponse>(body, JsonOptions);
    }

    public static async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string endpoint, TRequest payload)
    {
        using HttpClient client = CreateClient();
        string json = JsonSerializer.Serialize(payload, JsonOptions);

        HttpResponseMessage response = await client.PutAsync(
            endpoint,
            new StringContent(json, Encoding.UTF8, "application/json"));

        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(ExtractMessage(body), (int)response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        return JsonSerializer.Deserialize<TResponse>(body, JsonOptions);
    }

    public static async Task DeleteAsync(string endpoint)
    {
        using HttpClient client = CreateClient();
        HttpResponseMessage response = await client.DeleteAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            throw new ApiException(ExtractMessage(body), (int)response.StatusCode);
        }
    }

    /// <summary>
    /// جلب كل الأصناف من الخادم (GET {ItemsEndpoint}).
    /// يعيد قائمة فارغة إذا كان الـ API مُعطّلاً.
    /// </summary>
    public static async Task<List<ApiItem>> GetItemsAsync()
    {
        if (!ApiConfig.UseApi)
        {
            return new List<ApiItem>();
        }

        List<ApiItem>? items = await GetAsync<List<ApiItem>>(ApiConfig.ItemsEndpoint);
        return items ?? new List<ApiItem>();
    }

    // ═══════════════ عمليات الأصناف (CRUD) ═══════════════

    /// <summary>جلب صنف واحد من الخادم (GET {ItemsEndpoint}/{id}).</summary>
    public static async Task<ApiItem?> GetItemAsync(int id)
    {
        if (!ApiConfig.UseApi)
        {
            return null;
        }

        return await GetAsync<ApiItem>($"{ApiConfig.ItemsEndpoint}/{id}");
    }

    /// <summary>صنف جديد على الخادم (POST {ItemsEndpoint}) — يعيد الصنف بـ Id الجديد.</summary>
    public static async Task<ApiItem?> CreateItemAsync(ApiItem item)
    {
        if (!ApiConfig.UseApi)
        {
            return null;
        }

        return await PostAsync<ApiItem, ApiItem>(ApiConfig.ItemsEndpoint, item);
    }

    /// <summary>تعديل صنف موجود على الخادم (PUT {ItemsEndpoint}/{id}).</summary>
    public static async Task<ApiItem?> UpdateItemAsync(int id, ApiItem item)
    {
        if (!ApiConfig.UseApi)
        {
            return null;
        }

        return await PutAsync<ApiItem, ApiItem>($"{ApiConfig.ItemsEndpoint}/{id}", item);
    }

    /// <summary>حذف صنف من الخادم (DELETE {ItemsEndpoint}/{id}).</summary>
    public static async Task DeleteItemAsync(int id)
    {
        if (!ApiConfig.UseApi)
        {
            return;
        }

        await DeleteAsync($"{ApiConfig.ItemsEndpoint}/{id}");
    }

    // ═══════════════ عمليات الأطراف (عملاء/موردون) ═══════════════

    /// <summary>كل الأطراف من الخادم (GET {PartiesEndpoint}).</summary>
    public static async Task<List<ApiParty>> GetPartiesAsync()
    {
        if (!ApiConfig.UseApi)
        {
            return new List<ApiParty>();
        }

        List<ApiParty>? parties = await GetAsync<List<ApiParty>>(ApiConfig.PartiesEndpoint);
        return parties ?? new List<ApiParty>();
    }

    /// <summary>طرف جديد على الخادم (POST) — يعيده بـ Id الجديد.</summary>
    public static async Task<ApiParty?> CreatePartyAsync(ApiParty party)
    {
        if (!ApiConfig.UseApi)
        {
            return null;
        }

        return await PostAsync<ApiParty, ApiParty>(ApiConfig.PartiesEndpoint, party);
    }

    /// <summary>تعديل طرف على الخادم (PUT {PartiesEndpoint}/{id}).</summary>
    public static async Task<ApiParty?> UpdatePartyAsync(int id, ApiParty party)
    {
        if (!ApiConfig.UseApi)
        {
            return null;
        }

        return await PutAsync<ApiParty, ApiParty>($"{ApiConfig.PartiesEndpoint}/{id}", party);
    }

    /// <summary>حذف طرف من الخادم (DELETE {PartiesEndpoint}/{id}).</summary>
    public static async Task DeletePartyAsync(int id)
    {
        if (!ApiConfig.UseApi)
        {
            return;
        }

        await DeleteAsync($"{ApiConfig.PartiesEndpoint}/{id}");
    }

    // ═══════════════ عمليات الفواتير ═══════════════

    /// <summary>كل فواتير المبيعات من الخادم (GET {InvoicesEndpoint}).</summary>
    public static async Task<List<ApiInvoice>> GetInvoicesAsync()
    {
        if (!ApiConfig.UseApi)
        {
            return new List<ApiInvoice>();
        }

        List<ApiInvoice>? invoices = await GetAsync<List<ApiInvoice>>(ApiConfig.InvoicesEndpoint);
        return invoices ?? new List<ApiInvoice>();
    }

    /// <summary>فاتورة جديدة على الخادم (POST) — تشمل بنودها.</summary>
    public static async Task<ApiInvoice?> CreateInvoiceAsync(ApiInvoice invoice)
    {
        if (!ApiConfig.UseApi)
        {
            return null;
        }

        return await PostAsync<ApiInvoice, ApiInvoice>(ApiConfig.InvoicesEndpoint, invoice);
    }

    /// <summary>حذف فاتورة من الخادم (DELETE {InvoicesEndpoint}/{id}).</summary>
    public static async Task DeleteInvoiceAsync(int id)
    {
        if (!ApiConfig.UseApi)
        {
            return;
        }

        await DeleteAsync($"{ApiConfig.InvoicesEndpoint}/{id}");
    }

    // ═══════════════ عمليات فواتير المشتريات ═══════════════

    /// <summary>كل فواتير المشتريات من الخادم (GET {PurchaseInvoicesEndpoint}).</summary>
    public static async Task<List<ApiPurchaseInvoice>> GetPurchaseInvoicesAsync()
    {
        if (!ApiConfig.UseApi)
        {
            return new List<ApiPurchaseInvoice>();
        }

        List<ApiPurchaseInvoice>? invoices =
            await GetAsync<List<ApiPurchaseInvoice>>(ApiConfig.PurchaseInvoicesEndpoint);

        return invoices ?? new List<ApiPurchaseInvoice>();
    }

    /// <summary>فاتورة مشتريات جديدة على الخادم (POST) — تشمل بنودها.</summary>
    public static async Task<ApiPurchaseInvoice?> CreatePurchaseInvoiceAsync(ApiPurchaseInvoice invoice)
    {
        if (!ApiConfig.UseApi)
        {
            return null;
        }

        return await PostAsync<ApiPurchaseInvoice, ApiPurchaseInvoice>(
            ApiConfig.PurchaseInvoicesEndpoint, invoice);
    }

    /// <summary>تعديل فاتورة مشتريات على الخادم (PUT {PurchaseInvoicesEndpoint}/{id}).</summary>
    public static async Task<ApiPurchaseInvoice?> UpdatePurchaseInvoiceAsync(int id, ApiPurchaseInvoice invoice)
    {
        if (!ApiConfig.UseApi)
        {
            return null;
        }

        return await PutAsync<ApiPurchaseInvoice, ApiPurchaseInvoice>(
            $"{ApiConfig.PurchaseInvoicesEndpoint}/{id}", invoice);
    }

    /// <summary>حذف فاتورة مشتريات من الخادم (DELETE {PurchaseInvoicesEndpoint}/{id}).</summary>
    public static async Task DeletePurchaseInvoiceAsync(int id)
    {
        if (!ApiConfig.UseApi)
        {
            return;
        }

        await DeleteAsync($"{ApiConfig.PurchaseInvoicesEndpoint}/{id}");
    }

    /// <summary>استخراج رسالة الخطأ من استجابة الخادم.</summary>
    private static string ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (string key in new[] { "message", "error", "title", "detail" })
                {
                    if (doc.RootElement.TryGetProperty(key, out JsonElement el) &&
                        el.ValueKind == JsonValueKind.String)
                    {
                        return el.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // ليست JSON — نتجاهل
        }

        return string.Empty;
    }
}
