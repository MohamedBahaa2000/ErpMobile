using ErpMobile.Services;

namespace ErpMobile.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();

        // ملء تلقائي لبيانات المستخدم المحفوظة مسبقاً
        if (!string.IsNullOrEmpty(SessionManager.SavedUsername))
        {
            UsernameEntry.Text = SessionManager.SavedUsername;
        }

        RememberCheck.IsChecked = SessionManager.IsRemembered;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // دخول تلقائي إذا كان "تذكّرني" مفعّلاً والبيانات ما زالت صحيحة
        if (SessionManager.ShouldAutoLogin())
        {
            await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
        }
    }

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
