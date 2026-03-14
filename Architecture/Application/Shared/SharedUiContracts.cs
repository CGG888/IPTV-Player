using System.Windows;

namespace LibmpvIptvClient.Architecture.Application.Shared;

public interface ILocalizationService
{
    string GetText(string key, string fallback);
}

public interface IThemeTitleBarService
{
    void Apply(Window window, string? themeMode = null);
}
