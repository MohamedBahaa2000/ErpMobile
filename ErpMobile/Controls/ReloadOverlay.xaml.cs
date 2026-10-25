using System.Diagnostics;
using ErpMobile.Services;

namespace ErpMobile.Controls;

/// <summary>
/// حمّلة الريلود — طبقة تغطي الصفحة أثناء تحديث البيانات من الخادم.
///
/// الاستخدام (مشتركة بين الموبايل والويندوز):
///     await ReloadOverlay.RunAsync(LoadAsync, "تحديث الأصناف", "جلب آخر قائمة…");
///
/// الشكل: خلفية متدرّجة شفّافة + بطاقة بيضاء مدوّرة + «حلزون مزدوج متقابل الدوران»
///        + رمز ⟳ ثابت في المنتصف + ثلاث نقاط ترتد بموجة متتابعة.
/// </summary>
public partial class ReloadOverlay : ContentView
{
    private const string OuterAnim = "reload.outer";
    private const string InnerAnim = "reload.inner";
    private const string GlyphAnim = "reload.glyph";
    private const string Dot1Anim = "reload.dot1";
    private const string Dot2Anim = "reload.dot2";
    private const string Dot3Anim = "reload.dot3";

    private bool _busy;

    public ReloadOverlay()
    {
        InitializeComponent();
    }

    /// <summary>زمن أدنى لظهور الحمّلة حتى لا تومض أثناء العمليات السريعة جداً.</summary>
    public TimeSpan MinimumVisible { get; set; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// يعرض الحمّلة، ينفّذ العملية، ثم يخفيها (مع زمن أدنى للعرض).
    ///
    /// • آمن بالكامل: أي خطأ داخل العملية لا يُسقط التطبيق ولا يترك الحمّلة عالقة،
    ///   و«علامة الانشغال» تُعاد قراءتها دائماً عشان الزر يفضل شغّال.
    /// • في النهاية يظهر تنبيه «تم التحديث ✓» عشان المستخدم يتأكد إن الريلود حصل فعلاً.
    /// </summary>
    public async Task RunAsync(Func<Task> work, string? title = null, string? subtitle = null)
    {
        if (_busy || work is null)
        {
            return;
        }

        _busy = true;
        bool success = false;

        try
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                TitleLabel.Text = title;
            }

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                SubtitleLabel.Text = subtitle;
            }

            DateTime started = DateTime.UtcNow;
            Show();

            try
            {
                await work();
                success = true;
            }
            catch (Exception ex)
            {
                // الخطأ مش بنسكت عنه — بس كمان مش بنوقّع التطبيق
                Debug.WriteLine($"[ReloadOverlay] فشل التحديث: {ex}");
            }

            TimeSpan elapsed = DateTime.UtcNow - started;
            if (elapsed < MinimumVisible)
            {
                await Task.Delay(MinimumVisible - elapsed);
            }

            // علامة نجاح/فشل واضحة جوّه البطاقة نفسها
            // (بدل الاعتماد على التنبيه المنبثق بس، عشان المستخدم يتأكد إن الريلود حصل)
            PresentResult(success);
            await Task.Delay(900);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReloadOverlay] خطأ غير متوقع: {ex}");
        }
        finally
        {
            Hide();
            _busy = false;
        }

        // تنبيه إضافي (لو متاح) — فشله ما بيوقّعش حاجة
        if (success)
        {
            ToastService.Success("تم تحديث البيانات من الخادم ✓");
        }
        else
        {
            ToastService.Warning("تعذر تحديث البيانات — حاول مجدداً");
        }
    }

    /// <summary>إظهار علامة النجاح (✓) أو الخطأ (✗) بدل الحلزون داخل البطاقة.</summary>
    private void PresentResult(bool success)
    {
        StopAnimations();

        OuterRing.IsVisible = false;
        InnerRing.IsVisible = false;
        CenterGlyph.IsVisible = false;
        DotsLayout.IsVisible = false;

        SuccessIcon.IsVisible = true;
        SuccessIcon.Text = success ? "✓" : "✗";
        SuccessIcon.TextColor = success ? Color.FromArgb("#17A45C") : Color.FromArgb("#D64545");
        SuccessIcon.Opacity = 0;
        SuccessIcon.Scale = 0.4;

        TitleLabel.Text = success ? "تم التحديث بنجاح" : "تعذر التحديث";
        TitleLabel.TextColor = success ? Color.FromArgb("#173B70") : Color.FromArgb("#D64545");
        SubtitleLabel.Text = success
            ? "أحدث البيانات اتحمّلت من الخادم ✓"
            : "تأكد من اتصال الخادم وحاول مجدداً";

        _ = SuccessIcon.FadeToAsync(1, 220, Easing.CubicOut);
        _ = SuccessIcon.ScaleToAsync(1, 320, Easing.SpringOut);
    }

    /// <summary>إرجاع البطاقة لوضع الحلزون قبل عرضها في المرة القادمة.</summary>
    private void ResetCard()
    {
        OuterRing.IsVisible = true;
        InnerRing.IsVisible = true;
        CenterGlyph.IsVisible = true;
        DotsLayout.IsVisible = true;

        SuccessIcon.IsVisible = false;
        SuccessIcon.Opacity = 1;
        SuccessIcon.Scale = 1;

        TitleLabel.TextColor = Color.FromArgb("#173B70");
    }

    /// <summary>
    /// إظهار الحمّلة وتشغيل الحركات.
    ///
    /// ⚠ مهم على ويندوز: الحمّلة لازم تكون «ظاهرة» هي نفسها (مش شريطها جوّه فقط)،
    ///   وإلا تبقى الطبقة فوق الصفحة وبتبتلع ضغط الماوس على كل الأزرار.
    ///   أثناء التحميل تمنع الضغط عمد (InputTransparent=False) — ده السلوك المطلوب كطبقة تغطية.
    /// </summary>
    public void Show()
    {
        ResetCard();

        IsVisible = true;
        InputTransparent = false;

        Root.Opacity = 1;
        Root.IsVisible = true;

        Card.Opacity = 0;
        Card.Scale = 0.92;

        StartAnimations();

        _ = Card.FadeToAsync(1, 200, Easing.CubicOut);
        _ = Card.ScaleToAsync(1, 320, Easing.SpringOut);
    }

    /// <summary>
    /// إيقاف الحركات وإخفاء الحمّلة.
    ///
    /// ⚠ لازم IsVisible=False على الـ ContentView نفسه (مش على داخليه فقط)
    ///   عشان الطبقة تبطل تشارك في الـ hit-testing وترجّع كل الأزرار تشتغل.
    /// </summary>
    public void Hide()
    {
        StopAnimations();
        Root.IsVisible = false;
        IsVisible = false;
        InputTransparent = true;
        Card.Opacity = 0;
        Card.Scale = 0.92;
    }

    // ═══════════════════ الحركات ═══════════════════

    private void StartAnimations()
    {
        // حلقة خارجية: باتجاه الساعة
        if (!OuterRing.AnimationIsRunning(OuterAnim))
        {
            new Animation(rotation => OuterRing.Rotation = rotation, 0, 360)
                .Commit(OuterRing, OuterAnim, 16, 1500, Easing.Linear, null, () => true);
        }

        // حلقة داخلية: عكس اتجاه الساعة (متقابلة)
        if (!InnerRing.AnimationIsRunning(InnerAnim))
        {
            new Animation(rotation => InnerRing.Rotation = rotation, 360, 0)
                .Commit(InnerRing, InnerAnim, 16, 1000, Easing.Linear, null, () => true);
        }

        // رمز ⟳ في المنتصف: دوران بطيء عكسي
        if (!CenterGlyph.AnimationIsRunning(GlyphAnim))
        {
            new Animation(rotation => CenterGlyph.Rotation = rotation, 0, -360)
                .Commit(CenterGlyph, GlyphAnim, 16, 2400, Easing.Linear, null, () => true);
        }

        // ثلاث نقاط بموجة متتابعة
        CommitDot(Dot1, Dot1Anim, 0.00);
        CommitDot(Dot2, Dot2Anim, 0.20);
        CommitDot(Dot3, Dot3Anim, 0.40);
    }

    /// <summary>نقطة ترتد داخل دورة كاملة، مع بداية مُزاحة لإنشاء موجة.</summary>
    private static void CommitDot(Label dot, string name, double phase)
    {
        if (dot.AnimationIsRunning(name))
        {
            return;
        }

        const double up = 0.30;      // مدة الصعود
        const double amplitude = -11;

        var animation = new Animation();
        animation.Add(phase, phase + up,
            new Animation(value => dot.TranslationY = value, 0, amplitude, Easing.CubicOut));
        animation.Add(phase + up, phase + (up * 2),
            new Animation(value => dot.TranslationY = value, amplitude, 0, Easing.CubicIn));

        animation.Commit(dot, name, 16, 1500, Easing.Linear, null, () => true);
    }

    private void StopAnimations()
    {
        OuterRing.AbortAnimation(OuterAnim);
        InnerRing.AbortAnimation(InnerAnim);
        CenterGlyph.AbortAnimation(GlyphAnim);
        Dot1.AbortAnimation(Dot1Anim);
        Dot2.AbortAnimation(Dot2Anim);
        Dot3.AbortAnimation(Dot3Anim);

        OuterRing.Rotation = 0;
        InnerRing.Rotation = 0;
        CenterGlyph.Rotation = 0;
        Dot1.TranslationY = 0;
        Dot2.TranslationY = 0;
        Dot3.TranslationY = 0;
    }
}
