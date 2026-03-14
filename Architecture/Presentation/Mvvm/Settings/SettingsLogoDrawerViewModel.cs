using LibmpvIptvClient.Controls;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed class SettingsLogoDrawerViewModel : ViewModelBase
{
    public LogoConfig BuildTempConfig(LogoConfig? source)
    {
        if (source == null)
        {
            return new LogoConfig();
        }

        return new LogoConfig
        {
            Enabled = source.Enabled,
            Url = source.Url,
            EnableCache = source.EnableCache,
            CacheDir = source.CacheDir,
            CacheTtlHours = source.CacheTtlHours,
            CacheMaxMiB = source.CacheMaxMiB
        };
    }

    public void LoadDrawer(LogoDrawer? drawer, LogoConfig config)
    {
        drawer?.Load(config);
    }

    public void SaveDrawer(LogoDrawer? drawer, LogoConfig config)
    {
        drawer?.Save(config);
    }
}
