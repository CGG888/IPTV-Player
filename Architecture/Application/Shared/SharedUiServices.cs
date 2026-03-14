using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace LibmpvIptvClient.Architecture.Application.Shared;

public sealed class ResourceLocalizationService : ILocalizationService
{
    public string GetText(string key, string fallback)
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app is null)
            {
                return fallback;
            }

            var val = app.TryFindResource(key) as string;
            return string.IsNullOrEmpty(val) ? fallback : val;
        }
        catch
        {
            return fallback;
        }
    }
}

public sealed class WindowsTitleBarThemeService : IThemeTitleBarService
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public void Apply(Window window, string? themeMode = null)
    {
        try
        {
            var mode = themeMode ?? AppSettings.Current?.ThemeMode ?? "System";
            var dark = ResolveIsDark(mode);
            var hwnd = new WindowInteropHelper(window).Handle;
            var value = dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
            DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
        }
        catch
        {
        }
    }

    private static bool ResolveIsDark(string mode)
    {
        if (string.Equals(mode, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(mode, "Light", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }
}
