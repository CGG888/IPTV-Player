namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed class SettingsSaveCoordinatorViewModel : ViewModelBase
{
    private readonly SettingsPlaybackViewModel _playbackViewModel;
    private readonly SettingsTimeOverrideViewModel _timeOverrideViewModel;
    private readonly SettingsWebDavViewModel _webDavViewModel;
    private readonly SettingsCdnViewModel _cdnViewModel;
    private readonly SettingsAppearanceViewModel _appearanceViewModel;

    public SettingsSaveCoordinatorViewModel(
        SettingsPlaybackViewModel playbackViewModel,
        SettingsTimeOverrideViewModel timeOverrideViewModel,
        SettingsWebDavViewModel webDavViewModel,
        SettingsCdnViewModel cdnViewModel,
        SettingsAppearanceViewModel appearanceViewModel)
    {
        _playbackViewModel = playbackViewModel;
        _timeOverrideViewModel = timeOverrideViewModel;
        _webDavViewModel = webDavViewModel;
        _cdnViewModel = cdnViewModel;
        _appearanceViewModel = appearanceViewModel;
    }

    public PlaybackSettings BuildSettings(
        SettingsPlaybackFormState playbackForm,
        SettingsTimeOverrideFormState timeOverrideForm,
        SettingsWebDavFormState webDavForm,
        int languageIndex,
        int themeIndex,
        ReplayConfig replay,
        TimeshiftConfig timeshift,
        EpgConfig epg,
        LogoConfig logo,
        RecordingConfig recording)
    {
        var settings = new PlaybackSettings();
        try { _playbackViewModel.UpdateSettings(settings, playbackForm); } catch { }
        try { settings.Replay = replay; } catch { }
        try { settings.Timeshift = timeshift; } catch { }
        try { settings.Epg = epg; } catch { }
        try { settings.Logo = logo; } catch { }
        try { settings.Recording = recording; } catch { }
        try { settings.RecordingLocalDir = settings.Recording?.DirTemplate ?? settings.RecordingLocalDir; } catch { }
        try { settings.UpdateCdnMirrors = _cdnViewModel.GetCleanList(); } catch { }
        try { settings.Language = _appearanceViewModel.ResolveLanguageTagByIndex(languageIndex); } catch { }
        try { settings.ThemeMode = _appearanceViewModel.ResolveThemeModeByIndex(themeIndex); } catch { }
        try { settings.TimeOverride = _timeOverrideViewModel.BuildSaveConfig(timeOverrideForm); } catch { }
        try { settings.WebDav = _webDavViewModel.BuildSaveConfig(webDavForm); } catch { }
        return settings;
    }
}
