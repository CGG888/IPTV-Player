using LibmpvIptvClient.Controls;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed class SettingsTimeshiftDrawerViewModel : ViewModelBase
{
    public TimeshiftConfig BuildTempConfig(TimeshiftConfig? source)
    {
        if (source == null)
        {
            return new TimeshiftConfig();
        }

        return new TimeshiftConfig
        {
            Enabled = source.Enabled,
            UrlFormat = source.UrlFormat,
            DurationHours = source.DurationHours
        };
    }

    public void LoadDrawer(TimeshiftDrawer? drawer, TimeshiftConfig config)
    {
        drawer?.Load(config);
    }

    public void SaveDrawer(TimeshiftDrawer? drawer, TimeshiftConfig config)
    {
        drawer?.Save(config);
    }
}
