
namespace ErpMobile.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            ErrorLabel.Text = "من فضلك أدخل اسم المستخدم وكلمة المرور.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (username == "admin" && password == "123456")
        {
            ErrorLabel.IsVisible = false;
            await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
        }
        else
        {
            ErrorLabel.Text = "اسم المستخدم أو كلمة المرور غير صحيحة.";
            ErrorLabel.IsVisible = true;
        }
    }
}
