namespace ErpMobile.Pages;

/// <summary>لوجو Select ERP — يتحكّم في مقاسه عبر خاصية FontSize.</summary>
public partial class SelectLogoView : ContentView
{
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize),
        typeof(double),
        typeof(SelectLogoView),
        30d,
        propertyChanged: OnFontSizeChanged);

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public SelectLogoView()
    {
        InitializeComponent();
        ApplyFontSize(FontSize);
    }

    private static void OnFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SelectLogoView view && newValue is double size)
        {
            view.ApplyFontSize(size);
        }
    }

    private void ApplyFontSize(double size)
    {
        SelectLabel.FontSize = size;
        ErpLabel.FontSize = Math.Max(9, size * 0.55);
        ErpBadge.Padding = new Thickness(Math.Max(6, size * 0.4), Math.Max(2, size * 0.1));
    }
}
