using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;
using Microsoft.Maui.Controls.Shapes;

namespace ErpMobile.Pages;

/// <summary>صفحة عرض كل الأصناف مع البحث والتعديل.</summary>
public partial class ProductsListPage : ContentPage
{
    private List<Product> _all = [];
    private bool _fromApi;

    public ProductsListPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        // المصدر الموحّد: جلب من الخادم + مرآة محلية + احتياطي أوفلاين
        ProductSourceResult source = await ProductSource.GetAsync();

        _all = source.Items;
        _fromApi = source.FromApi;

        if (string.IsNullOrEmpty(source.StatusMessage))
        {
            HideBanner();
        }
        else
        {
            ShowBanner(source.StatusMessage, source.StatusColor);
        }

        Render();
    }

    private void ShowBanner(string message, string color)
    {
        ApiStatusBanner.BackgroundColor = Color.FromArgb(color);
        ApiStatusText.Text = message;
        ApiStatusBanner.IsVisible = true;
    }

    private void HideBanner() => ApiStatusBanner.IsVisible = false;

    private void Render()
    {
        TitleLabel.Text = _fromApi ? "عرض الأصناف — من الخادم" : "عرض الأصناف — محلي";

        string query = SearchEntry.Text?.Trim() ?? string.Empty;

        List<Product> items = string.IsNullOrEmpty(query)
            ? _all
            : _all.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Unit.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.EnglishName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        CountLabel.Text = items.Count.ToString();
        TotalValueLabel.Text = items.Sum(p => p.Price).ToString("0.00", CultureInfo.InvariantCulture);

        ListContainer.Children.Clear();

        if (items.Count == 0)
        {
            EmptyLabel.Text = _all.Count == 0
                ? "لا توجد أصناف مسجّلة — اضغط «＋ إضافة» لإنشاء أول صنف."
                : "لا توجد نتائج مطابقة للبحث.";
            EmptyLabel.IsVisible = true;
            return;
        }

        EmptyLabel.IsVisible = false;

        foreach (Product product in items)
        {
            ListContainer.Children.Add(BuildRow(product));
        }
    }

    private Border BuildRow(Product product)
    {
        var info = new VerticalStackLayout { Spacing = 3 };
        info.Children.Add(new Label
        {
            Text = product.Name,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#192D4A"),
            LineBreakMode = LineBreakMode.TailTruncation
        });

        string subtitle = string.IsNullOrWhiteSpace(product.Barcode)
            ? product.Unit
            : $"{product.Barcode} • {product.Unit}";
        if (!string.IsNullOrWhiteSpace(product.Category))
        {
            subtitle += $" • {product.Category}";
        }

        info.Children.Add(new Label
        {
            Text = subtitle,
            FontSize = 12,
            TextColor = Color.FromArgb("#8A91A6"),
            LineBreakMode = LineBreakMode.TailTruncation
        });

        var price = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };
        price.Children.Add(new Label
        {
            Text = product.Price.ToString("0.00", CultureInfo.InvariantCulture),
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#12A15B"),
            HorizontalTextAlignment = TextAlignment.End
        });
        if (product.WholesalePrice > 0)
        {
            price.Children.Add(new Label
            {
                Text = $"جملة {product.WholesalePrice.ToString("0.00", CultureInfo.InvariantCulture)}",
                FontSize = 11,
                TextColor = Color.FromArgb("#8A91A6"),
                HorizontalTextAlignment = TextAlignment.End
            });
        }

        var grid = new Grid { Padding = new Thickness(14, 12), ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(info);
        grid.Children.Add(price);
        Grid.SetColumn(price, 1);

        var border = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E5EAF2"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = grid
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnProductTapped;
        tap.CommandParameter = product;
        border.GestureRecognizers.Add(tap);

        return border;
    }

    private async void OnProductTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Product product)
        {
            return;
        }

        // نمرّر المصدر (خادم/جهاز) والباركود عشان صفحة التعديل تعرف من أين تجلبه
        string source = _fromApi ? "api" : "local";
        string barcode = string.IsNullOrWhiteSpace(product.Barcode)
            ? string.Empty
            : Uri.EscapeDataString(product.Barcode);

        await Shell.Current.GoToAsync(
            $"{nameof(ProductPage)}?id={product.Id}&src={source}&bc={barcode}");
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => Render();

    private async void OnAddClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ProductPage));

    private async void OnCloseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
