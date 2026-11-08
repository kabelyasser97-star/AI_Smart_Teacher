using System.Windows;

namespace AI_Smart_Teacher.Desktop.Services;

public enum UiLanguage
{
    Arabic,
    English
}

public static class LocalizationService
{
    public static UiLanguage Current { get; private set; } = UiLanguage.Arabic;

    public static void Apply(Window window, UiLanguage language)
    {
        Current = language;
        var isArabic = language == UiLanguage.Arabic;
        window.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        window.Language = System.Windows.Markup.XmlLanguage.GetLanguage(isArabic ? "ar-SA" : "en-US");
        if (System.Windows.Application.Current.Resources["FontArabic"] is System.Windows.Media.FontFamily ar &&
            System.Windows.Application.Current.Resources["FontEnglish"] is System.Windows.Media.FontFamily en)
        {
            window.FontFamily = isArabic ? ar : en;
        }
    }

    public static UiLanguage Toggle(Window window)
    {
        var next = Current == UiLanguage.Arabic ? UiLanguage.English : UiLanguage.Arabic;
        Apply(window, next);
        return next;
    }
}
