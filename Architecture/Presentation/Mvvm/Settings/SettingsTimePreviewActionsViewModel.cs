namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed record TimePreviewResult(string PreviewUrl, string PreviewTemplate);

public sealed class SettingsTimePreviewActionsViewModel : ViewModelBase
{
    private readonly SettingsTimeOverrideViewModel _timeOverrideViewModel;

    public SettingsTimePreviewActionsViewModel(SettingsTimeOverrideViewModel timeOverrideViewModel)
    {
        _timeOverrideViewModel = timeOverrideViewModel;
    }

    public TimePreviewResult BuildPreview(string baseUrl, SettingsTimeOverrideFormState form, bool isTimeshift)
    {
        var url = _timeOverrideViewModel.GeneratePreviewResult(baseUrl, form, isTimeshift);
        var tpl = _timeOverrideViewModel.GenerateTemplatePreview(baseUrl, form);
        return new TimePreviewResult(url, tpl);
    }
}
