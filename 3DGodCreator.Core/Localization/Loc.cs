using System.Globalization;
using System.Resources;

namespace ThreeDGodCreator.Core.Localization;

/// <summary>
/// Runtime localization helper backed by embedded .resx resources (en neutral, de satellite).
/// Missing keys never surface raw key names to the user.
/// </summary>
public static class Loc
{
    private static readonly ResourceManager Manager = new(
        "ThreeDGodCreator.Core.Resources.Strings",
        typeof(Loc).Assembly);

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static CultureInfo _culture = English;

    public static event Action? CultureChanged;

    public static CultureInfo CurrentCulture => _culture;

    public static string CurrentLocale =>
        _culture.TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase) ? "de" : "en";

    public static void SetCulture(string locale)
    {
        var next = NormalizeLocale(locale);
        if (_culture.Name.Equals(next.Name, StringComparison.OrdinalIgnoreCase))
            return;

        _culture = next;
        Thread.CurrentThread.CurrentUICulture = next;
        CultureChanged?.Invoke();
    }

    public static string Get(string key, params object[] args) =>
        GetForLocale(CurrentLocale, key, args);

    public static string GetForLocale(string locale, string key, params object[] args)
    {
        var culture = NormalizeLocale(locale);
        var value = Manager.GetString(key, culture)
                    ?? Manager.GetString(key, English);

        if (string.IsNullOrEmpty(value))
            value = FriendlyMissing(key);

        return args.Length > 0 ? string.Format(culture, value, args) : value;
    }

    private static CultureInfo NormalizeLocale(string locale) =>
        locale.Trim().StartsWith("de", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("de")
            : English;

    private static string FriendlyMissing(string key)
    {
        var english = Manager.GetString(key, English);
        if (!string.IsNullOrEmpty(english))
            return english;

        return "…";
    }
}
