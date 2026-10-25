using System.Globalization;
using ErpMobile.Models;
using ErpMobile.Services;

namespace ErpMobile.Pages;

[QueryProperty(nameof(PartyType), "type")]
public partial class PartiesPage : ContentPage
{
    private List<Party> _parties = [];

    /// <summary>نتيجة آخر جلب — تحمل شريط حالة الاتصال بالخادم.</summary>
    private PartySourceResult _source = new();

    /// <summary>customer أو supplier — يُمرَّر من الداشبورد.</summary>
    public string PartyType { get; set; } = Party.Customer;

    public PartiesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool suppliers = PartyType == Party.Supplier;
        TitleLabel.Text = suppliers ? "الموردين" : "العملاء";
        Title = suppliers ? "الموردين" : "العملاء";

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            // مصدر موحّد: الخادم أولاً ثم المرآة المحلية (نفس مصدر الداشبورد)
            _source = await PartySource.GetAsync(PartyType, SearchEntry.Text);
            _parties = _source.Items;
        }
        catch (Exception)
        {
            _parties = [];
        }

        BuildList();
    }

    private void BuildList()
    {
        ListContainer.Children.Clear();

        // شريط حالة الاتصال بالخادم (أخضر = من الـ API، أحمر = بيانات الجهاز)
        if (!string.IsNullOrEmpty(_source.StatusMessage))
        {
            var statusBanner = new Border
            {
                Padding = new Thickness(12, 8),
                BackgroundColor = Color.FromArgb(_source.FromApi ? "#EAF7F0" : "#FDEEEE"),
                Stroke = Color.FromArgb(_source.StatusColor),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Content = new Label
                {
                    Text = _source.StatusMessage,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb(_source.StatusColor),
                    HorizontalTextAlignment = TextAlignment.Center
                }
            };

            // شريط كامل العرض في كلتا الحالتين (موبايل وويندوز)
            ResponsiveCards.Apply(statusBanner, ResponsiveCards.FullWidthBasis);
            ListContainer.Children.Add(statusBanner);
        }

        CountLabel.Text = $"{_parties.Count} سجل";

        if (_parties.Count == 0)
        {
            var emptyBanner = new Border
            {
                Padding = new Thickness(16, 22),
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E5EAF2"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                Content = new Label
                {
                    Text = "لا توجد سجلات — اضغط «＋ إضافة» لتسجيل أول سجل.",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#8995A7"),
                    HorizontalTextAlignment = TextAlignment.Center
                }
            };

            ResponsiveCards.Apply(emptyBanner, ResponsiveCards.FullWidthBasis);
            ListContainer.Children.Add(emptyBanner);
            return;
        }

        foreach (Party party in _parties)
        {
            var card = BuildCard(party);
            ResponsiveCards.Apply(card);
            ListContainer.Children.Add(card);
        }
    }

    private Border BuildCard(Party party)
    {
        var nameLabel = new Label
        {
            Text = party.Name,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#192D4A")
        };

        var codeLabel = new Label
        {
            Text = party.Code,
            FontSize = 11,
            TextColor = Color.FromArgb("#7A8299")
        };

        var info = new VerticalStackLayout { Spacing = 3 };
        info.Children.Add(nameLabel);
        info.Children.Add(codeLabel);

        string details = string.Join(" · ", new[]
        {
            party.Mobile,
            party.Phone,
            party.PaymentKind,
            party.OpeningBalance != 0
                ? $"رصيد: {party.OpeningBalance.ToString("0.00", CultureInfo.InvariantCulture)}"
                : null
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        var detailLabel = new Label
        {
            Text = details.Length == 0 ? "—" : details,
            FontSize = 12,
            TextColor = Color.FromArgb("#8995A7"),
            Margin = new Thickness(0, 4, 0, 0)
        };

        var editButton = new Button
        {
            Text = "تعديل",
            FontSize = 12,
            HeightRequest = 36,
            Padding = new Thickness(12, 4),
            CornerRadius = 8,
            BackgroundColor = Color.FromArgb("#EEF3FF"),
            TextColor = Color.FromArgb("#173B70")
        };
        editButton.Clicked += async (_, _) =>
        {
            await Shell.Current.GoToAsync($"{nameof(PartyFormPage)}?id={party.Id}&type={PartyType}");
        };

        var deleteButton = new Button
        {
            Text = "حذف",
            FontSize = 12,
            HeightRequest = 36,
            Padding = new Thickness(12, 4),
            CornerRadius = 8,
            BackgroundColor = Color.FromArgb("#FDEEEE"),
            TextColor = Color.FromArgb("#D9534F")
        };
        deleteButton.Clicked += async (_, _) => await DeleteAsync(party);

        var actions = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        actions.Children.Add(editButton);
        actions.Children.Add(deleteButton);

        var content = new Grid { ColumnSpacing = 10 };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.Children.Add(info);
        content.Children.Add(actions);
        Grid.SetColumn(actions, 1);

        var body = new VerticalStackLayout { Spacing = 0 };
        body.Children.Add(content);
        body.Children.Add(detailLabel);

        return new Border
        {
            Padding = new Thickness(14, 12),
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E5EAF2"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            Content = body
        };
    }

    private async Task DeleteAsync(Party party)
    {
        bool confirm = await DisplayAlertAsync(
            "حذف السجل",
            $"هل تريد حذف «{party.Name}»؟",
            "نعم",
            "لا");

        if (!confirm)
        {
            return;
        }

        await PartySource.DeleteAsync(party);
        ToastService.Success($"تم حذف «{party.Name}» ✓");
        await LoadAsync();
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(PartyFormPage)}?type={PartyType}");
    }

    private async void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        await LoadAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
