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
        // ── أيقونة الصنف (مربّع مدوّر) ──
        var iconTile = new Border
        {
            Padding = 0,
            BackgroundColor = Color.FromArgb("#EEF0FD"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            VerticalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = "🏷",
                FontSize = 17,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };

        // ── الاسم + بيانات تكميلية ──
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

        // ── السعر داخل «حبّة» خضراء ──
        var priceChildren = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.End };
        priceChildren.Children.Add(new Border
        {
            Padding = new Thickness(11, 5),
            BackgroundColor = Color.FromArgb("#E9F9F0"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            Content = new Label
            {
                Text = product.Price.ToString("0.00", CultureInfo.InvariantCulture),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0E9152"),
                HorizontalTextAlignment = TextAlignment.Center
            }
        });
        if (product.WholesalePrice > 0)
        {
            priceChildren.Children.Add(new Label
            {
                Text = $"جملة {product.WholesalePrice.ToString("0.00", CultureInfo.InvariantCulture)}",
                FontSize = 11,
                TextColor = Color.FromArgb("#8A91A6"),
                HorizontalTextAlignment = TextAlignment.End
            });
        }

        var topRow = new Grid { ColumnSpacing = 10 };
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.Children.Add(iconTile);
        topRow.Children.Add(info);
        topRow.Children.Add(priceChildren);
        Grid.SetColumn(info, 1);
        Grid.SetColumn(priceChildren, 2);

        // ── الصف السفلي: وسم + زر تعديل بشكل «حبّة» ──
        var editButton = new Button
        {
            Text = "✎ تعديل",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 32,
            Padding = new Thickness(14, 0),
            CornerRadius = 16,
            BackgroundColor = Color.FromArgb("#EFEEFB"),
            TextColor = Color.FromArgb("#6B62C7"),
            VerticalOptions = LayoutOptions.Center
        };
        editButton.Clicked += async (_, _) => await OpenProductAsync(product);

        View? tag = null;
        if (product.WholesalePrice > 0)
        {
            tag = MakeTag($"جملة {product.WholesalePrice.ToString("0.00", CultureInfo.InvariantCulture)}", "#FFF4E8", "#C77A20");
        }
        else if (!string.IsNullOrWhiteSpace(product.Category))
        {
            tag = MakeTag(product.Category, "#EEF0FD", "#5A63B8");
        }

        var bottomRow = new Grid { ColumnSpacing = 8 };
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        if (tag is not null)
        {
            bottomRow.Children.Add(tag);
        }
        bottomRow.Children.Add(editButton);
        Grid.SetColumn(editButton, 2);

        var content = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(14, 12) };
        content.Children.Add(topRow);
        content.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#EEF1F7") });
        content.Children.Add(bottomRow);

        var border = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E5EAF2"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Shadow = new Shadow
            {
                Brush = Color.FromArgb("#253B65"),
                Offset = new Point(0, 4),
                Radius = 10,
                Opacity = 0.07f
            },
            Content = content
        };

        // النقر على أي مساحة غير الأزرار يفتح صفحة الصنف
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnProductTapped;
        tap.CommandParameter = product;
        border.GestureRecognizers.Add(tap);

        ResponsiveCards.Apply(border);

        return border;
    }

    /// <summary>وسم صغير ملوّن داخل بطاقة الصنف.</summary>
    private static Border MakeTag(string text, string background, string foreground) => new()
    {
        Padding = new Thickness(10, 5),
        BackgroundColor = Color.FromArgb(background),
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 12 },
        VerticalOptions = LayoutOptions.Center,
        Content = new Label
        {
            Text = text,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb(foreground),
            HorizontalTextAlignment = TextAlignment.Center
        }
    };

    /// <summary>يفتح صفحة تعديل الصنف (من البطاقة أو من زر «تعديل»).</summary>
    private async Task OpenProductAsync(Product product)
    {
        // نمرّر المصدر (خادم/جهاز) والباركود عشان صفحة التعديل تعرف من أين تجلبه
        string source = _fromApi ? "api" : "local";
        string barcode = string.IsNullOrWhiteSpace(product.Barcode)
            ? string.Empty
            : Uri.EscapeDataString(product.Barcode);

        await Shell.Current.GoToAsync(
            $"{nameof(ProductPage)}?id={product.Id}&src={source}&bc={barcode}");
    }

    private async void OnProductTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Product product)
        {
            await OpenProductAsync(product);
        }
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => Render();

    private async void OnAddClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ProductPage));

    private async void OnCloseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    /// <summary>
    /// ريلود قائمة الأصناف من الخادم بدون الخروج من التطبيق —
    /// بيعرض «حمّلة الريلود» (الحلزون المزدوج) لحد ما تيجي البيانات.
    /// </summary>
    private async void OnReloadClicked(object? sender, EventArgs e) =>
        await ReloadOverlay.RunAsync(
            LoadAsync,
            "تحديث قائمة الأصناف",
            "جلب أحدث قائمة الأصناف من الخادم…");
}
