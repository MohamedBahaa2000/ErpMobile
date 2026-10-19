using Microsoft.Maui.Storage;

namespace ErpMobile.Services;

/// <summary>
/// إدارة جلسة المستخدم: "تذكّرني" يحفظ تسجيل الدخول ويدخل تلقائياً في المرة القادمة.
/// </summary>
public static class SessionManager
{
    private const string KeyRemember = "session_remember";
    private const string KeyUsername = "session_username";
    private const string KeyPassword = "session_password";

    /// <summary>المستخدم مُفعّل له "تذكّرني".</summary>
    public static bool IsRemembered => Preferences.Default.Get(KeyRemember, false);

    /// <summary>اسم المستخدم المحفوظ (يُملأ تلقائياً في شاشة الدخول).</summary>
    public static string SavedUsername => Preferences.Default.Get(KeyUsername, string.Empty);

    private static string SavedPassword => Preferences.Default.Get(KeyPassword, string.Empty);

    /// <summary>التحقق من بيانات الدخول (المنطق الموحّد للتطبيق).</summary>
    public static bool Authenticate(string username, string password) =>
        username.Trim() == "admin" && password == "123456";

    /// <summary>حفظ الجلسة بعد تسجيل دخول ناجح.</summary>
    public static void SaveSession(string username, string password, bool remember)
    {
        Preferences.Default.Set(KeyUsername, username);

        if (remember)
        {
            Preferences.Default.Set(KeyRemember, true);
            Preferences.Default.Set(KeyPassword, password);
        }
        else
        {
            Clear();
        }
    }

    /// <summary>هل ندخل تلقائياً؟ (تذكر مفعّل والبيانات المحفوظة ما زالت صحيحة)</summary>
    public static bool ShouldAutoLogin() =>
        IsRemembered && Authenticate(SavedUsername, SavedPassword);

    /// <summary>إنهاء الجلسة (تسجيل الخروج) مع الإبقاء على اسم المستخدم للملء التلقائي.</summary>
    public static void SignOut()
    {
        Preferences.Default.Remove(KeyRemember);
        Preferences.Default.Remove(KeyPassword);
    }

    /// <summary>مسح بيانات الجلسة المحفوظة.</summary>
    public static void Clear()
    {
        Preferences.Default.Remove(KeyRemember);
        Preferences.Default.Remove(KeyPassword);
    }
}
