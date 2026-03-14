using LibmpvIptvClient.Architecture.Application.Shared;
using System.Windows;
using System.Windows.Input;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed record AppearancePreviewResult(
    string PreviewLanguage,
    string PreviewTheme,
    bool AppliedLanguage,
    bool AppliedTheme);

public sealed record TimeOverrideModeChangePrompt(
    bool ShouldConfirm,
    string Message,
    string Title);

public sealed class SettingsWindowUiActionsViewModel : ViewModelBase
{
    private readonly SettingsAppearanceViewModel _appearanceViewModel;
    private readonly SettingsTimeOverrideViewModel _timeOverrideViewModel;
    private readonly IThemeTitleBarService _themeTitleBarService;
    private readonly ILocalizationService _localizationService;

    public SettingsWindowUiActionsViewModel(
        SettingsAppearanceViewModel appearanceViewModel,
        SettingsTimeOverrideViewModel timeOverrideViewModel,
        IThemeTitleBarService themeTitleBarService,
        ILocalizationService localizationService)
    {
        _appearanceViewModel = appearanceViewModel;
        _timeOverrideViewModel = timeOverrideViewModel;
        _themeTitleBarService = themeTitleBarService;
        _localizationService = localizationService;
    }

    public AppearancePreviewResult ApplyAppearancePreview(
        bool appearanceInitialized,
        int languageIndex,
        int themeIndex,
        string previewLanguage,
        string previewTheme,
        Window window)
    {
        if (!appearanceInitialized)
        {
            return new AppearancePreviewResult(previewLanguage, previewTheme, false, false);
        }

        var plan = _appearanceViewModel.BuildPreviewPlan(
            languageIndex,
            themeIndex,
            previewLanguage,
            previewTheme);

        if (plan.ApplyLanguage)
        {
            App.ApplyLanguage(plan.Selection.LanguageTag);
            previewLanguage = plan.Selection.LanguageTag;
        }

        if (plan.ApplyTheme)
        {
            App.ApplyTheme(plan.Selection.ThemeMode);
            previewTheme = plan.Selection.ThemeMode;
            _themeTitleBarService.Apply(window, plan.Selection.ThemeMode);
        }

        return new AppearancePreviewResult(previewLanguage, previewTheme, plan.ApplyLanguage, plan.ApplyTheme);
    }

    public TimeOverrideModeChangePrompt BuildModeChangePrompt(int newIndex)
    {
        if (!_timeOverrideViewModel.NeedsConfirmationForModeChange(newIndex))
        {
            return new TimeOverrideModeChangePrompt(false, string.Empty, string.Empty);
        }

        var msg = _localizationService.GetText(
            "UI_TimeOverride_ConfirmReplaceAll",
            "将完全替换时间参数，可能影响非时间参数的顺序或兼容性。确定继续？");
        var title = _localizationService.GetText("Common_Tips", "提示");
        return new TimeOverrideModeChangePrompt(true, msg, title);
    }

    public TimeOverrideVisibilityState BuildTimeOverrideVisibility(int layoutIndex)
    {
        return _timeOverrideViewModel.GetVisibilityState(layoutIndex);
    }

    public bool ShouldTriggerDebug(Key key)
    {
        return key == Key.F1;
    }
}
