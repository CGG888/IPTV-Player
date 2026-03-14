namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed record SettingsAppearanceSelection(string LanguageTag, string ThemeMode);
public sealed record SettingsAppearancePreviewPlan(SettingsAppearanceSelection Selection, bool ApplyLanguage, bool ApplyTheme);

public sealed class SettingsAppearanceViewModel : ViewModelBase
{
    public int ResolveLanguageIndex(string? language)
    {
        var lang = (language ?? string.Empty).Trim();
        if (string.Equals(lang, "zh-CN", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(lang, "zh-TW", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(lang, "ru-RU", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return 0;
    }

    public int ResolveThemeIndex(string? themeMode)
    {
        var theme = (themeMode ?? "System").Trim();
        if (string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 0;
    }

    public string ResolveLanguageTagByIndex(int selectedIndex)
    {
        return selectedIndex switch
        {
            1 => "zh-CN",
            2 => "zh-TW",
            3 => "en-US",
            4 => "ru-RU",
            _ => string.Empty
        };
    }

    public string ResolveThemeModeByIndex(int selectedIndex)
    {
        return selectedIndex switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };
    }

    public SettingsAppearanceSelection BuildSelection(int languageIndex, int themeIndex)
    {
        return new SettingsAppearanceSelection(
            ResolveLanguageTagByIndex(languageIndex),
            ResolveThemeModeByIndex(themeIndex));
    }

    public SettingsAppearancePreviewPlan BuildPreviewPlan(
        int languageIndex,
        int themeIndex,
        string? currentPreviewLanguage,
        string? currentPreviewTheme)
    {
        var selection = BuildSelection(languageIndex, themeIndex);
        var applyLanguage = !string.Equals(
            currentPreviewLanguage ?? string.Empty,
            selection.LanguageTag,
            StringComparison.OrdinalIgnoreCase);
        var applyTheme = !string.Equals(
            currentPreviewTheme ?? "System",
            selection.ThemeMode,
            StringComparison.OrdinalIgnoreCase);
        return new SettingsAppearancePreviewPlan(selection, applyLanguage, applyTheme);
    }
}
