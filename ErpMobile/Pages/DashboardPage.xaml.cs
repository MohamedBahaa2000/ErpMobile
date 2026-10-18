namespace ErpMobile.Pages;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private async void OnNewInvoiceClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(InvoicePage));
    }

    private async void OnAddProductClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProductPage));
    }
}
