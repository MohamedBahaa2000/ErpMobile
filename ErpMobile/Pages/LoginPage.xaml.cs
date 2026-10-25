using ErpMobile.Services;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace ErpMobile.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();

#if WINDOWS
        // ── إصلاح: صورة الهيدر لا تظهر على الويندوز ──
        // التطبيق غير مُحزَّم (Unpackaged)، فمصدر ms-appx الافتراضي في MAUI لا
        // يعثر على ملفات الصور. نقرأها هنا من مجلد الملف التنفيذي مباشرةً —
        // كود خاص بالويندوز فقط، ولا يمسّ أندرويد إطلاقاً.
        HeroImage.Source = ImageSource.FromStream(OpenHeroImageFromDisk);
#endif

        // ملء تلقائي لبيانات المستخدم المحفوظة مسبقاً
        if (!string.IsNullOrEmpty(SessionManager.SavedUsername))
        {
            UsernameEntry.Text = SessionManager.SavedUsername;
        }

        RememberCheck.IsChecked = SessionManager.IsRemembered;
    }

#if WINDOWS
    /// <summary>
    /// يفتح صورة الهيدر من مجلد التشغيل — بأفضل مقاس متاح لدقة الشاشة.
    /// يُستدعى من MAUI عند الحاجة، ويعيد فتح ملف جديد في كل مرة.
    /// </summary>
    private static Stream OpenHeroImageFromDisk()
    {
        string baseDirectory = AppContext.BaseDirectory;

        // نفضّل المقاس الأقرب فوق 150% (دقة شاشة الويندوز الشائعة) لحدة أعلى
        string[] candidates =
        [
            "login_hero.scale-150.png",
            "login_hero.scale-200.png",
            "login_hero.scale-125.png",
            "login_hero.scale-400.png",
            "login_hero.scale-100.png",
            "login_hero.png"
        ];

        foreach (string fileName in candidates)
        {
            string path = Path.Combine(baseDirectory, fileName);
            if (File.Exists(path))
            {
                return File.OpenRead(path);
            }
        }

        throw new FileNotFoundException(
            "لم يتم العثور على صورة الهيدر login_hero بجانب الملف التنفيذي.");
    }
#endif

    private bool _autoLoginStarted;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // دخول تلقائي إذا كان "تذكّرني" مفعّلاً والبيانات ما زالت صحيحة
        if (SessionManager.ShouldAutoLogin() && !_autoLoginStarted)
        {
            _autoLoginStarted = true;
            _ = AutoLoginAsync();
        }
    }

    /// <summary>
    /// نفّذ الدخول التلقائي بعد أن يكتمل إنشاء Shell.
    /// ملاحظة: التنقل المباشر من OnAppearing أثناء بناء Shell الأولي
    /// يسبّب خطأً معروفاً في MAUI: "Pending Navigations still processing".
    /// لذلك ننتظر ثم نعيد المحاولة حتى يجهز التنقل.
    /// </summary>
    private static async Task AutoLoginAsync()
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            await Task.Delay(350);

            try
            {
                if (Shell.Current is not null)
                {
                    await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
                    return;
                }
            }
            catch (Exception)
            {
                // Shell ما زال يجهّز — نعيد المحاولة بعد قليل
            }
        }
    }

    /// <summary>
    /// عند الضغط على Enter في حقل اسم المستخدم → الانتقال إلى حقل كلمة المرور.
    /// </summary>
    private void OnUsernameCompleted(object sender, EventArgs e)
        => PasswordEntry.Focus();

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            ShowError("من فضلك أدخل اسم المستخدم وكلمة المرور.");
            return;
        }

        SetBusy(true);
        try
        {
            bool success = await ValidateAsync(username, password);

            if (success)
            {
                ErrorLabel.IsVisible = false;

                // حفظ الجلسة إذا كان "تذكّرني" محدداً
                SessionManager.SaveSession(username, password, RememberCheck.IsChecked);

                await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
            }
            else
            {
                ShowError("اسم المستخدم أو كلمة المرور غير صحيحة.");
            }
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception)
        {
            ShowError("تعذّر تسجيل الدخول، حاول مرة أخرى.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// التحقق من الدخول: عبر الـ API إذا كان مفعّلاً في ApiConfig،
    /// وإلا محلياً (admin / 123456).
    /// </summary>
    private static async Task<bool> ValidateAsync(string username, string password)
    {
        if (!ApiConfig.UseApi)
        {
            // وضع محلي بدون خادم
            return SessionManager.Authenticate(username, password);
        }

        // وضع الخادم الحقيقي
        LoginResponse? result = await ApiService.LoginAsync(username, password);
        return result is not null && result.Success;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        LoginButton.IsEnabled = !busy;
        LoginButton.Opacity = busy ? 0.6 : 1;
        LoginButton.Text = busy ? "جارٍ الدخول..." : "تسجيل الدخول";
    }
}
