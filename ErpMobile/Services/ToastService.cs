using Microsoft.Maui.Controls.Shapes;

namespace ErpMobile.Services;

/// <summary>نوع التنبيه المنبثق.</summary>
public enum ToastType
{
    Success,   // ✓ تم الحفظ / تم الحذف
    Error,     // ✗ خطأ
    Warning,   // ⚠ تنبيه
    Info       // ℹ معلومة
}

/// <summary>
/// ═══════════════════════════════════════════════════════
///  تنبيه منبثق (Toast) مميّز: كارت مدوّر بأيقونة ولون،
///  يظهر أعلى/أسفل الشاشة ثم ينزلق ويختفي تلقائياً.
/// ═══════════════════════════════════════════════════════
/// </summary>
public static class ToastService
{
    private sealed class OverlayGrid : Grid
    {
    }

    private static readonly Dictionary<ToastType, (string Icon, string Bg, string Fg)> Styles = new()
    {
        [ToastType.Success] = ("✓", "#17A45C", "White"),
        [ToastType.Error]   = ("✗", "#D64545", "White"),
        [ToastType.Warning] = ("⚠", "#E0952B", "White"),
        [ToastType.Info]    = ("ℹ", "#2458F2", "White"),
    };

    /// <summary>عرض تنبيه ناجح (تم الحفظ / تم الحذف ...).</summary>
    public static void Success(string message) => Show(message, ToastType.Success);

    /// <summary>عرض تنبيه خطأ.</summary>
    public static void Error(string message) => Show(message, ToastType.Error);

    /// <summary>عرض تنبيه تنبيهي.</summary>
    public static void Warning(string message) => Show(message, ToastType.Warning);

    /// <summary>عرض تنبيه معلوماتي.</summary>
    public static void Info(string message) => Show(message, ToastType.Info);

    public static void Show(string message, ToastType type = ToastType.Success) =>
        MainThread.BeginInvokeOnMainThread(() => ShowOnMainThread(message, type));

    // ─────────────────── التنفيذ ───────────────────
    private static async void ShowOnMainThread(string message, ToastType type)
    {
        if (Shell.Current?.CurrentPage is not ContentPage page)
        {
            return;
        }

        try
        {
            Grid host = EnsureOverlayHost(page);
            (string icon, string bg, string fg) = Styles[type];

            // بطاقة التنبيه
            var iconBox = new Border
            {
                Padding = new Thickness(7, 3),
                BackgroundColor = Color.FromArgb("#33FFFFFF"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Content = new Label
                {
                    Text = icon,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                },
                VerticalOptions = LayoutOptions.Center
            };

            var label = new Label
            {
                Text = message,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb(fg),
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var content = new HorizontalStackLayout
            {
                Spacing = 9,
                VerticalOptions = LayoutOptions.Center
            };
            content.Children.Add(iconBox);
            content.Children.Add(label);

            var toast = new Border
            {
                Padding = new Thickness(14, 11),
                BackgroundColor = Color.FromArgb(bg),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(16, 46, 16, 0),
                MaximumWidthRequest = 420,
                Opacity = 0,
                Scale = 0.85,
                TranslationY = -24,
                Content = content
            };
            toast.Shadow = new Shadow
            {
                Brush = Color.FromArgb(bg),
                Offset = new Point(0, 6),
                Radius = 16,
                Opacity = 0.4f
            };

            host.Children.Add(toast);
            Grid.SetRow(toast, 0);
            Grid.SetColumn(toast, 0);

            // لو الشبكة مقسّمة صفوف/أعمدة، التوست يمتد على الكل
            if (host.RowDefinitions.Count > 1)
            {
                Grid.SetRowSpan(toast, host.RowDefinitions.Count);
            }

            if (host.ColumnDefinitions.Count > 1)
            {
                Grid.SetColumnSpan(toast, host.ColumnDefinitions.Count);
            }

            // ظهور متدرّج
            await Task.WhenAll(
                toast.FadeToAsync(1, 220, Easing.CubicOut),
                toast.ScaleToAsync(1, 220, Easing.CubicOut),
                toast.TranslateToAsync(0, 0, 220, Easing.CubicOut));

            // بقاء ثم اختفاء
            await Task.Delay(2000);

            await Task.WhenAll(
                toast.FadeToAsync(0, 260, Easing.CubicIn),
                toast.TranslateToAsync(0, -24, 260, Easing.CubicIn));

            host.Children.Remove(toast);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ToastService] {ex}");
            // لا ندع فشل التنبيه يمنع عمل التطبيق
        }
    }

    /// <summary>
    /// إيجاد سطح الإضافة للتنبيه داخل الصفحة.
    ///
    /// ⚠ على ويندوز: ممنوع إعادة ترتيب شجرة المحتوى (page.Content = غلاف جديد)
    ///   لأنها بترمي COMException «No installed components were detected»
    ///   وبيختفي التنبيه. فنضيف التنبيه فوق نفس الجRID الرئيسي للصفحة.
    /// </summary>
    private static Grid EnsureOverlayHost(ContentPage page)
    {
        // (Grid تشمل OverlayGrid لو الشبكة اتعملتلها غلاف قبل كده)
        if (page.Content is Grid existing)
        {
            return existing;
        }

        // محتوى غير Grid: بنلفّه في غلاف (نادر الحدوث)
        var original = page.Content;
        var host = new OverlayGrid();

        if (original is not null)
        {
            // نحتفظ بسلوك المحتوى الأصلي كما هو
            host.Children.Add(original);
            Grid.SetRow(original, 0);
        }

        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

        page.Content = host;
        return host;
    }
}
