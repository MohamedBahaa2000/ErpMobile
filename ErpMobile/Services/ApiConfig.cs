namespace ErpMobile.Services;

/// <summary>
/// ══════════════════════════════════════════════════════════════
///  إعدادات الـ API — الملف الوحيد الذي تُعدّل فيه كل الروابط.
///  كود مشترك ⇒ يعمل على Android و Windows و iOS من مكان واحد.
/// ══════════════════════════════════════════════════════════════
/// </summary>
public static class ApiConfig
{
    /// <summary>
    /// تفعيل الاتصال بالخادم.
    /// false  = وضع محلي (تسجيل دخول admin/123456 بدون خادم) — الوضع الحالي.
    /// true   = الاتصال بالخادم الحقيقي عبر الـ API بالأسفل.
    /// </summary>
    public const bool UseApi = false;

    // ═══════════════════ الرابط الأساسي ═══════════════════
    /// <summary>الرابط الأساسي للخادم — غيّره هنا فقط.</summary>
    public const string BaseUrl = "https://api.example.com";

    // ═══════════════════ المصادقة (Login) ═══════════════════
    /// <summary>تسجيل الدخول: POST {BaseUrl}{LoginEndpoint}</summary>
    public const string LoginEndpoint = "/api/auth/login";

    /// <summary>تجديد التوكن: POST</summary>
    public const string RefreshTokenEndpoint = "/api/auth/refresh";

    /// <summary>تسجيل الخروج: POST</summary>
    public const string LogoutEndpoint = "/api/auth/logout";

    /// <summary>بيانات المستخدم الحالي: GET</summary>
    public const string ProfileEndpoint = "/api/auth/me";

    // ═══════════════════ الأصناف ═══════════════════
    public const string ProductsEndpoint = "/api/products";

    /// <summary>بحث بالباركود: GET {ProductByBarcodeEndpoint}/{barcode}</summary>
    public const string ProductByBarcodeEndpoint = "/api/products/barcode";

    public const string ProductCategoriesEndpoint = "/api/products/categories";

    // ═══════════════════ العملاء والموردون ═══════════════════
    public const string PartiesEndpoint = "/api/parties";
    public const string CustomersEndpoint = "/api/customers";
    public const string SuppliersEndpoint = "/api/suppliers";

    // ═══════════════════ الفواتير ═══════════════════
    /// <summary>فواتير المبيعات.</summary>
    public const string InvoicesEndpoint = "/api/invoices";

    /// <summary>فواتير المشتريات.</summary>
    public const string PurchaseInvoicesEndpoint = "/api/purchase-invoices";

    /// <summary>تقرير المبيعات (تحليلات): GET ?from=&amp;to=</summary>
    public const string SalesReportEndpoint = "/api/reports/sales";

    // ═══════════════════ إعدادات الاتصال ═══════════════════
    /// <summary>مهلة الانتظار بالثواني.</summary>
    public const int TimeoutSeconds = 20;

    /// <summary>رأس الترويسة الخاصة بمفتاح الـ API (إن وجد).</summary>
    public const string ApiKeyHeader = "X-Api-Key";

    /// <summary>قيمة مفتاح الـ API (اتركه فارغاً إن لم يكن مطلوباً).</summary>
    public const string ApiKey = "";

    /// <summary>المفتاح الذي يُحفظ به التوكن بعد تسجيل الدخول.</summary>
    public const string TokenStorageKey = "api_auth_token";

    /// <summary>هل الخادم يستخدم HTTPS؟ (للتحقق من الشهادات ذاتية التوقيع)</summary>
    public const bool AllowSelfSigned = false;
}
