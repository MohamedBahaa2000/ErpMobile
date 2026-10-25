namespace ErpMobile.Services;

/// <summary>
/// يُكيّف بطاقات القوائم مع عرض الشاشة.
///
/// الحاويات في صفحات القوائم أصبحت <c>FlexLayout</c> بدل VerticalStackLayout،
/// فتُصفّ البطاقات هكذا:
///   • شاشة ضيقة (موبايل ~384dp) → عمود واحد، البطاقة تملأ العرض.
///   • شاشة عريضة (ويندوز 1900px) → ٣ أو ٤ بطاقات في الصف الواحد.
///
/// «الأساس» (Basis) هو عرض البطاقة قبل التمدد؛ وقيمة كبيرة جداً (FullWidthBasis)
/// تجعل العنصر يحتل الصف بالكامل — تُستخدم لشارات مثل حالة الاتصال والرسائل الفارغة.
/// </summary>
public static class ResponsiveCards
{
    /// <summary>عرض البطاقة قبل التمدد — يعطي ٣-٤ بطاقات على شاشة ويندوز عريضة.</summary>
    public const float CardBasis = 430f;

    /// <summary>أساس يتجاوز أي شاشة → العنصر يحتل الصف بالكامل.</summary>
    public const float FullWidthBasis = 100000f;

    /// <summary>يطبّق التكيّف على بطاقة داخل حاوية FlexLayout.</summary>
    /// <param name="card">البطاقة نفسها (Border أو أي VisualElement).</param>
    /// <param name="basis">العرض الأساسي؛ اتركه للبطاقة العادية، أو استخدم FullWidthBasis للشريط الكامل.</param>
    public static void Apply(VisualElement card, float basis = CardBasis)
    {
        // ملاحظة مهمة: FlexLayout بيوفّر overload واحد ثابت SetGrow(BindableObject, float)
        // و overload تاني عادي (instance) SetGrow(IView, float). لو مرّرنا المعامل بـ IView
        // الـ compiler بيختار التابع للـ instance ويطلّع خطأ CS0120 (محتاج مرجع كائن)،
        // فنمرّر الكائن بـ BindableObject عشان يقع على النسخة الثابتة بالضبط.
        BindableObject bindable = card;

        // FlexLayout.SetBasis(BindableObject, FlexBasis) — وكامل FlexBasis بيتولّد
        // من الـ float بتحويل ضمني (implicit operator).
        FlexLayout.SetGrow(bindable, 1f);
        FlexLayout.SetBasis(bindable, basis);

        // هوامش تفصل البطاقات عن بعضها داخل الـ FlexLayout (اللي مالوش Spacing).
        // نستخدم SetValue على الـ BindableProperty مباشرة، لأن الـ CLR property
        // Margin مش متاح على VisualElement في MAUI 10 (موجود بس على الـ IView للقراءة).
        card.SetValue(FlexLayout.MarginProperty, new Thickness(5, 5, 5, 5));
    }
}
