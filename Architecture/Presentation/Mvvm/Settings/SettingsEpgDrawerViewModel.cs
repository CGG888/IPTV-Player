using LibmpvIptvClient.Controls;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed class SettingsEpgDrawerViewModel : ViewModelBase
{
    public EpgConfig BuildTempConfig(EpgConfig? source)
    {
        if (source == null)
        {
            return new EpgConfig();
        }

        return new EpgConfig
        {
            Enabled = source.Enabled,
            Url = source.Url,
            RefreshIntervalHours = source.RefreshIntervalHours,
            EnableSmartMatch = source.EnableSmartMatch,
            StrictMatchByPlaybackTime = source.StrictMatchByPlaybackTime
        };
    }

    public void LoadDrawer(EpgDrawer? drawer, EpgConfig config)
    {
        drawer?.Load(config);
    }

    public void SaveDrawer(EpgDrawer? drawer, EpgConfig config)
    {
        drawer?.Save(config);
    }
}
