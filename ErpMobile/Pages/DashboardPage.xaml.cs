using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SetupHeader();
        await LoadDataAsync();
    }

    private void SetupHeader()
    {
        string username = SessionManager.SavedUsername;
        UserNameLabel.Text = string.IsNullOrWhiteSpace(username) ? "المسؤول" : username;
        DateLabel.Text = DateTime.Now.ToString("dddd، d MMMM yyyy", new CultureInfo("ar-EG"));
    }

    // ===================== العمليات اليومية =====================

    private async void OnSalesAction(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(InvoicePage));

    private async void OnProductAction(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ProductPage));

    private async void OnCustomersAction(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync($"{nameof(PartiesPage)}?type=customer");

    private async void OnSuppliersAction(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync($"{nameof(PartiesPage)}?type=supplier");

    private async void OnAllInvoicesClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(InvoicesListPage));

    private async void OnAllProductsClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ProductsListPage));

    private async void OnLogoutClicked(object? sender, TappedEventArgs e)
    {
        bool confirm = await DisplayAlertAsync(
            "تسجيل الخروج",
            "هل تريد تسجيل الخروج من الحساب؟",
            "نعم",
            "إلغاء");

        if (!confirm)
        {
            return;
        }

        SessionManager.SignOut();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }

    private async Task LoadDataAsync()
    {
        try
        {
            List<Invoice> invoices = await InvoiceRepository.Instance.GetRecentAsync(500);
            string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            List<Invoice> todayInvoices = invoices.Where(invoice => invoice.InvoiceDate == today).ToList();

            TodaySalesLabel.Text = FormatAmount(todayInvoices.Sum(invoice => invoice.Total));
            TodayInvoicesLabel.Text = $"{todayInvoices.Count} فاتورة";
            OutstandingLabel.Text = $"إجمالي المتبقي: {FormatAmount(invoices.Sum(invoice => invoice.Remaining))}";

            BuildRecentInvoices(invoices.Take(5).ToList());

            await LoadAnalyticsAsync(invoices);
        }
        catch (Exception)
        {
            // تجاهل أخطاء القراءة مؤقتاً حتى لا تمنع عرض اللوحة
        }
    }

    // ===================== تحليلات الداشبورد =====================

    private async Task LoadAnalyticsAsync(List<Invoice> invoices)
    {
        // 1) بطاقات المؤشرات
        string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        double todaySales = invoices.Where(i => i.InvoiceDate == today).Sum(i => i.Total);

        AnalyticsSalesLabel.Text = FormatAmount(todaySales);
        AnalyticsSalesCountLabel.Text = $"{TodayInvoicesCount(invoices)} فاتورة اليوم";

        // الأصناف
        List<Product> products = await ProductRepository.Instance.GetAllAsync();
        AnalyticsProductsLabel.Text = products.Count.ToString();
        AnalyticsProductsValueLabel.Text = $"قيمة المخزون: {FormatAmount(products.Sum(p => p.Price))}";

        // العملاء
        List<Party> customers = await PartyRepository.Instance.GetAllAsync(Party.Customer);
        AnalyticsCustomersLabel.Text = customers.Count.ToString();
        AnalyticsCustomersBalanceLabel.Text = $"أرصدة: {FormatAmount(customers.Sum(c => c.OpeningBalance))}";

        // الموردين
        List<Party> suppliers = await PartyRepository.Instance.GetAllAsync(Party.Supplier);
        AnalyticsSuppliersLabel.Text = suppliers.Count.ToString();
        AnalyticsSuppliersBalanceLabel.Text = $"أرصدة: {FormatAmount(suppliers.Sum(s => s.OpeningBalance))}";

        // 2) رسم مبيعات آخر 7 أيام
        BuildSalesChart(invoices);

        // 3) الأصناف الأكثر مبيعاً
        await BuildTopProductsAsync();

        // 4) العملاء الأكثر شراءً
        BuildTopCustomers(invoices);
    }

    private static int TodayInvoicesCount(List<Invoice> invoices)
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return invoices.Count(i => i.InvoiceDate == today);
    }

    private void BuildSalesChart(List<Invoice> invoices)
    {
        SalesChartGrid.Children.Clear();

        DateTime today = DateTime.Today;
        double maxSales = 1;
        var daily = new List<(string Day, double Sales)>();

        for (int offset = 6; offset >= 0; offset--)
        {
            DateTime date = today.AddDays(-offset);
            string key = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            double sales = invoices.Where(i => i.InvoiceDate == key).Sum(i => i.Total);
            maxSales = Math.Max(maxSales, sales);
            string dayLabel = date.DayOfWeek switch
            {
                DayOfWeek.Saturday => "السبت",
                DayOfWeek.Sunday => "الأحد",
                DayOfWeek.Monday => "الاثنين",
                DayOfWeek.Tuesday => "الثلاثاء",
                DayOfWeek.Wednesday => "الأربعاء",
                DayOfWeek.Thursday => "الخميس",
                _ => "الجمعة"
            };
            daily.Add((dayLabel, sales));
        }

        double totalWeek = daily.Sum(d => d.Sales);
        SalesChartTotalLabel.Text = $"الإجمالي: {FormatAmount(totalWeek)}";

        for (int col = 0; col < 7; col++)
        {
            (string dayLabel, double sales) = daily[col];
            double ratio = sales / maxSales;
            int barHeight = Math.Max(8, (int)(80 * ratio));

            var bar = new BoxView
            {
                HeightRequest = barHeight,
                WidthRequest = 22,
                BackgroundColor = Color.FromArgb(col == 6 ? "#E1523D" : "#2458F2"),
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 0)
            };

            var dayLbl = new Label
            {
                Text = dayLabel,
                FontSize = 10,
                TextColor = Color.FromArgb("#8995A7"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            var valLbl = new Label
            {
                Text = sales > 0 ? FormatAmount(sales) : "-",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = sales > 0 ? Color.FromArgb("#192D4A") : Color.FromArgb("#C5CCD8"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(bar, col);
            Grid.SetRow(bar, 0);
            Grid.SetColumn(dayLbl, col);
            Grid.SetRow(dayLbl, 1);
            Grid.SetColumn(valLbl, col);
            Grid.SetRow(valLbl, 2);

            SalesChartGrid.Children.Add(bar);
            SalesChartGrid.Children.Add(dayLbl);
            SalesChartGrid.Children.Add(valLbl);
        }
    }

    private async Task BuildTopProductsAsync()
    {
        TopProductsContainer.Children.Clear();

        List<InvoiceItem> items = await InvoiceRepository.Instance.GetAllItemsAsync();

        var grouped = items
            .GroupBy(i => new { i.Barcode, i.Name })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Barcode,
                TotalQty = g.Sum(x => x.Quantity),
                TotalAmount = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(5)
            .ToList();

        if (grouped.Count == 0)
        {
            TopProductsContainer.Children.Add(new Label
            {
                Text = "لا توجد مبيعات بعد — أنشئ فاتورة لعرض التحليلات.",
                FontSize = 12,
                TextColor = Color.FromArgb("#8995A7"),
                HorizontalTextAlignment = TextAlignment.Center
            });
            return;
        }

        for (int rank = 0; rank < grouped.Count; rank++)
        {
            var item = grouped[rank];
            string medal = rank switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => "▫" };

            var info = new VerticalStackLayout { Spacing = 2 };
            info.Children.Add(new Label
            {
                Text = $"{medal} {item.Name}",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#192D4A")
            });
            if (!string.IsNullOrWhiteSpace(item.Barcode))
            {
                info.Children.Add(new Label
                {
                    Text = item.Barcode,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#8995A7")
                });
            }

            var amt = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.End };
            amt.Children.Add(new Label
            {
                Text = FormatAmount(item.TotalAmount),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#248A58"),
                HorizontalTextAlignment = TextAlignment.End
            });
            amt.Children.Add(new Label
            {
                Text = $"{item.TotalQty} قطعة",
                FontSize = 11,
                TextColor = Color.FromArgb("#8995A7"),
                HorizontalTextAlignment = TextAlignment.End
            });

            var grid = new Grid { ColumnSpacing = 12, Padding = new Thickness(4, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(info);
            grid.Children.Add(amt);
            Grid.SetColumn(amt, 1);

            TopProductsContainer.Children.Add(grid);
        }
    }

    private void BuildTopCustomers(List<Invoice> invoices)
    {
        TopCustomersContainer.Children.Clear();

        var grouped = invoices
            .Where(i => !string.IsNullOrWhiteSpace(i.Customer))
            .GroupBy(i => i.Customer)
            .Select(g => new
            {
                Name = g.Key,
                TotalSpent = g.Sum(x => x.Total),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(5)
            .ToList();

        if (grouped.Count == 0)
        {
            TopCustomersContainer.Children.Add(new Label
            {
                Text = "لا يوجد عملاء مع فواتير بعد.",
                FontSize = 12,
                TextColor = Color.FromArgb("#8995A7"),
                HorizontalTextAlignment = TextAlignment.Center
            });
            return;
        }

        for (int rank = 0; rank < grouped.Count; rank++)
        {
            var c = grouped[rank];
            string medal = rank switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => "▫" };

            var info = new VerticalStackLayout { Spacing = 2 };
            info.Children.Add(new Label
            {
                Text = $"{medal} {c.Name}",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#192D4A")
            });
            info.Children.Add(new Label
            {
                Text = $"{c.Count} فاتورة",
                FontSize = 11,
                TextColor = Color.FromArgb("#8995A7")
            });

            var amt = new Label
            {
                Text = FormatAmount(c.TotalSpent),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0E9BB8"),
                HorizontalTextAlignment = TextAlignment.End,
                VerticalOptions = LayoutOptions.Center
            };

            var grid = new Grid { ColumnSpacing = 12, Padding = new Thickness(4, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(info);
            grid.Children.Add(amt);
            Grid.SetColumn(amt, 1);

            TopCustomersContainer.Children.Add(grid);
        }
    }

    private void BuildRecentInvoices(List<Invoice> invoices)
    {
        RecentInvoicesContainer.Children.Clear();

        if (invoices.Count == 0)
        {
            RecentInvoicesContainer.Children.Add(new Border
            {
                Padding = new Thickness(16),
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E5EAF2"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                Content = new Label
                {
                    Text = "لا توجد فواتير محفوظة بعد — أنشئ أول فاتورة من زر «فاتورة جديدة».",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#8995A7"),
                    HorizontalTextAlignment = TextAlignment.Center
                }
            });
            return;
        }

        var rows = new VerticalStackLayout { Spacing = 0 };

        for (int index = 0; index < invoices.Count; index++)
        {
            Invoice invoice = invoices[index];
            bool paid = invoice.Remaining <= 0.005 && invoice.Total > 0;

            var info = new VerticalStackLayout { Spacing = 4 };
            info.Children.Add(new Label
            {
                Text = $"فاتورة #{invoice.InvoiceNumber}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#192D4A")
            });
            info.Children.Add(new Label
            {
                Text = $"{invoice.Customer} · {invoice.InvoiceDate}",
                FontSize = 12,
                TextColor = Color.FromArgb("#8995A7")
            });

            var amounts = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.End };
            amounts.Children.Add(new Label
            {
                Text = FormatAmount(invoice.Total),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#192D4A"),
                HorizontalTextAlignment = TextAlignment.End
            });
            amounts.Children.Add(new Label
            {
                Text = paid ? "مدفوعة" : $"متبقية {FormatAmount(invoice.Remaining)}",
                FontSize = 12,
                TextColor = paid ? Color.FromArgb("#248A58") : Color.FromArgb("#D98A16"),
                HorizontalTextAlignment = TextAlignment.End
            });

            var grid = new Grid { Padding = new Thickness(16, 12), ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(info);
            grid.Children.Add(amounts);
            Grid.SetColumn(amounts, 1);

            rows.Children.Add(grid);

            if (index < invoices.Count - 1)
            {
                rows.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#EEF1F6") });
            }
        }

        var card = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E5EAF2"),
            StrokeThickness = 1,
            Content = rows
        };
        SetRoundedShape(card, 16);
        RecentInvoicesContainer.Children.Add(card);
    }

    private static void SetRoundedShape(Border border, double radius)
    {
        try
        {
            var shape = new Microsoft.Maui.Controls.Shapes.RoundRectangle();
            shape.CornerRadius = radius;
            border.StrokeShape = shape;
        }
        catch (Exception)
        {
            // في حال عدم توفر الخاصية يتم إبقاء الحدود مستقيمة
        }
    }

    private static string FormatAmount(double amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);
}
