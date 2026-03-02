using System.Windows;
using System.Text;
using System;
using System.Runtime.InteropServices;
using LibmpvIptvClient.Diagnostics;
using ModernWpf;
using System.Globalization;
using System.Threading;

namespace LibmpvIptvClient
{
    public partial class App : System.Windows.Application
    {
        public static event Action? LanguageChanged;
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                try { Logger.Log("未处理异常 " + e.Exception?.ToString()); } catch { }
                System.Windows.MessageBox.Show(e.Exception?.Message ?? LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_Unknown", "未知错误"),
                    LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_UnhandledTitle", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { Logger.Log("未处理异常 " + e.ExceptionObject?.ToString()); } catch { }
                System.Windows.MessageBox.Show(e.ExceptionObject?.ToString() ?? LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_Unknown", "未知错误"),
                    LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_UnhandledTitle", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch { }
            try
            {
                ApplyTheme(AppSettings.Current.ThemeMode);
                ApplyLanguage(AppSettings.Current.Language);
            }
            catch { }
        }
        public static void ApplyTheme(string mode)
        {
            try
            {
                if (string.Equals(mode, "Dark", StringComparison.OrdinalIgnoreCase))
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                else if (string.Equals(mode, "Light", StringComparison.OrdinalIgnoreCase))
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                else
                    ThemeManager.Current.ApplicationTheme = null;
                var app = System.Windows.Application.Current;
                if (app == null) return;
                var remove = new System.Collections.Generic.List<ResourceDictionary>();
                foreach (var rd in app.Resources.MergedDictionaries)
                {
                    if (rd.Source != null && rd.Source.OriginalString.StartsWith("Resources/Colors.", StringComparison.OrdinalIgnoreCase))
                        remove.Add(rd);
                }
                foreach (var r in remove) app.Resources.MergedDictionaries.Remove(r);
                var actual = ThemeManager.Current.ActualApplicationTheme;
                var src = actual == ApplicationTheme.Light ? "Resources/Colors.Light.xaml" : "Resources/Colors.Dark.xaml";
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(src, UriKind.Relative) });
            }
            catch { }
        }
        public static void ApplyLanguage(string lang)
        {
            try
            {
                CultureInfo ci = string.IsNullOrWhiteSpace(lang) ? CultureInfo.InstalledUICulture : new CultureInfo(lang);
                Thread.CurrentThread.CurrentUICulture = ci;
                Thread.CurrentThread.CurrentCulture = ci;
                LibmpvIptvClient.Helpers.ResxLocalizer.SetCulture(ci);
                var app = System.Windows.Application.Current;
                if (app == null) return;
                var toRemove = new System.Collections.Generic.List<ResourceDictionary>();
                foreach (var rd in app.Resources.MergedDictionaries)
                {
                    try
                    {
                        if (rd.Source != null && rd.Source.OriginalString.Contains("Resources/Strings.", StringComparison.OrdinalIgnoreCase))
                            toRemove.Add(rd);
                    }
                    catch { }
                }
                foreach (var r in toRemove) app.Resources.MergedDictionaries.Remove(r);
                string code;
                if (ci.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    code = ci.Name.IndexOf("TW", StringComparison.OrdinalIgnoreCase) >= 0 ? "zh-TW" : "zh-CN";
                else if (ci.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
                    code = "ru-RU";
                else
                    code = "en-US";
                var dict = new ResourceDictionary { Source = new Uri($"Resources/Strings.{code}.xaml", UriKind.Relative) };
                app.Resources.MergedDictionaries.Add(dict);
                LanguageChanged?.Invoke();
            }
            catch { }
        }
    }
}
