using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LibmpvIptvClient.Helpers
{
    public static class ThemeHelper
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // attr 19/20: use dark mode for title bar/caption buttons (1 = dark, 0 = light)
        static void SetTitleBarDark(Window w, bool dark)
        {
            try
            {
                var hwnd = new WindowInteropHelper(w).Handle;
                int value = dark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
                DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
            }
            catch { }
        }

        static bool IsSystemDark()
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var v = key.GetValue("AppsUseLightTheme");
                    if (v is int i) return i == 0; // 0 = dark, 1 = light
                }
            }
            catch { }
            return false;
        }

        public static void ApplyTitleBarByTheme(Window w)
        {
            try
            {
                var mode = AppSettings.Current?.ThemeMode ?? "System";
                bool dark = false;
                if (string.Equals(mode, "Dark", StringComparison.OrdinalIgnoreCase)) dark = true;
                else if (string.Equals(mode, "Light", StringComparison.OrdinalIgnoreCase)) dark = false;
                else dark = IsSystemDark();
                SetTitleBarDark(w, dark);
            }
            catch { }
        }
    }
}
