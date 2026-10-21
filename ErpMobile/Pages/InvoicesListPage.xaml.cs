using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace ErpMobile.Pages;

/// <summary>صفحة عرض كل فواتير المبيعات مع البحث والفلترة.</summary>
public partial class InvoicesListPage : ContentPage
{
    private enum Filter { All, Paid, Due }

    private Filter _filter = Filter.All;
    private List<Invoice> _all = [];

    public InvoicesListPage()
    {
        InitializeComponent();
        ApplyFilterStyles();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            // جلب كل الفواتير (رقم كبير = العرض الكامل)
            _all = await InvoiceRepository.Instance.GetRecentAsync(100000);
        }
        catch (Exception)
        {
            _all = [];
        }

        Render();
    }

    private List<Invoice> GetFiltered()
    {
        string query = SearchEntry.Text?.Trim() ?? string.Empty;

        IEnumerable<Invoice> q = _all;

        q = _filter switch
        {
            Filter.Paid => q.Where(i => i.Remaining <= 0.005 && i.Total > 0),
            Filter.Due => q.Where(i => i.Remaining > 0.005),
            _ => q
        };

        if (!string.IsNullOrEmpty(query))
        {
            q = q.Where(i =>
                i.InvoiceNumber.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.Customer.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                i.InvoiceDate.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return q.ToList();
    }

    private void Render()
    {
        List<Invoice> items = GetFiltered();

        CountLabel.Text = items.Count.ToString();
        TotalLabel.Text = items.Sum(i => i.Total).ToString("0.00", CultureInfo.InvariantCulture);

        ListContainer.Children.Clear();

        if (items.Count == 0)
        {
            EmptyLabel.Text = _all.Count == 0
                ? "لا توجد فواتير محفوظة — اضغط «＋ جديدة» لإنشاء أول فاتورة."
                : "لا توجد نتائج مطابقة للفلتر أو البحث.";
            EmptyLabel.IsVisible = true;
            return;
        }

        EmptyLabel.IsVisible = false;

        foreach (Invoice invoice in items)
        {
            ListContainer.Children.Add(BuildRow(invoice));
        }
    }

    private Border BuildRow(Invoice invoice)
    {
        bool paid = invoice.Remaining <= 0.005 && invoice.Total > 0;

        var info = new VerticalStackLayout { Spacing = 3 };
        info.Children.Add(new Label
        {
            Text = $"فاتورة #{invoice.InvoiceNumber}",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#192D4A")
        });
        info.Children.Add(new Label
        {
            Text = $"{invoice.Customer}  •  {invoice.InvoiceDate}",
            FontSize = 12,
            TextColor = Color.FromArgb("#8A91A6"),
            LineBreakMode = LineBreakMode.TailTruncation
        });

        var amounts = new VerticalStackLayout
        {
            Spacing = 3,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };
        amounts.Children.Add(new Label
        {
            Text = invoice.Total.ToString("0.00", CultureInfo.InvariantCulture),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#192D4A"),
            HorizontalTextAlignment = TextAlignment.End
        });

        var status = new Border
        {
            Padding = new Thickness(8, 3),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb(paid ? "#E4F6EC" : "#FDF1DC"),
            HorizontalOptions = LayoutOptions.End,
            Content = new Label
            {
                Text = paid ? "مدفوعة" : $"متبقية {invoice.Remaining.ToString("0.00", CultureInfo.InvariantCulture)}",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb(paid ? "#17A45C" : "#C77A11")
            }
        };
        amounts.Children.Add(status);

        var grid = new Grid { Padding = new Thickness(14, 12), ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(info);
        grid.Children.Add(amounts);
        Grid.SetColumn(amounts, 1);

        var border = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E5EAF2"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = grid
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnInvoiceTapped;
        tap.CommandParameter = invoice;
        border.GestureRecognizers.Add(tap);

        return border;
    }

    private async void OnInvoiceTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Invoice invoice)
        {
            return;
        }

        string paidStatus = invoice.Remaining <= 0.005
            ? "مدفوعة بالكامل"
            : $"مدفوع: {invoice.Paid.ToString("0.00", CultureInfo.InvariantCulture)}  •  متبقي: {invoice.Remaining.ToString("0.00", CultureInfo.InvariantCulture)}";

        await DisplayAlertAsync(
            $"فاتورة #{invoice.InvoiceNumber}",
            $"العميل: {invoice.Customer}\nالتاريخ: {invoice.InvoiceDate}\n" +
            $"نوع الفاتورة: {invoice.InvoiceType}\n" +
            $"الإجمالي: {invoice.Total.ToString("0.00", CultureInfo.InvariantCulture)}\n{paidStatus}",
            "حسناً");
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => Render();

    private void OnFilterAllClicked(object? sender, EventArgs e)
    {
        _filter = Filter.All;
        ApplyFilterStyles();
        Render();
    }

    private void OnFilterPaidClicked(object? sender, EventArgs e)
    {
        _filter = Filter.Paid;
        ApplyFilterStyles();
        Render();
    }

    private void OnFilterDueClicked(object? sender, EventArgs e)
    {
        _filter = Filter.Due;
        ApplyFilterStyles();
        Render();
    }

    private void ApplyFilterStyles()
    {
        SetChip(FilterAllButton, _filter == Filter.All, "#2458F2");
        SetChip(FilterPaidButton, _filter == Filter.Paid, "#17A45C");
        SetChip(FilterDueButton, _filter == Filter.Due, "#E0952B");
    }

    private static void SetChip(Button button, bool active, string color)
    {
        button.BackgroundColor = Color.FromArgb(active ? color : "#FFFFFF");
        button.TextColor = Color.FromArgb(active ? "#FFFFFF" : "#55607A");
        button.BorderColor = Color.FromArgb(active ? color : "#DCE3ED");
        button.BorderWidth = 1;
    }

    private async void OnAddClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(InvoicePage));

    private async void OnCloseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
