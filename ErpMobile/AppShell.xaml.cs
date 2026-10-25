using ErpMobile.Pages;

namespace ErpMobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(InvoicePage), typeof(InvoicePage));
            Routing.RegisterRoute(nameof(PurchaseInvoicePage), typeof(PurchaseInvoicePage));
            Routing.RegisterRoute(nameof(ProductPage), typeof(ProductPage));
            Routing.RegisterRoute(nameof(PartiesPage), typeof(PartiesPage));
            Routing.RegisterRoute(nameof(PartyFormPage), typeof(PartyFormPage));
            Routing.RegisterRoute(nameof(BarcodeScannerPage), typeof(BarcodeScannerPage));
            Routing.RegisterRoute(nameof(InvoicesListPage), typeof(InvoicesListPage));
            Routing.RegisterRoute(nameof(ProductsListPage), typeof(ProductsListPage));
            Routing.RegisterRoute(nameof(InvoiceDetailsPage), typeof(InvoiceDetailsPage));
        }
    }
}
