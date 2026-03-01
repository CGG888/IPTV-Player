using System;
using WpfApp = System.Windows;

namespace LibmpvIptvClient.Helpers
{
    public static class Localizer
    {
        public static string S(string key, string fallback)
        {
            try
            {
                var app = WpfApp.Application.Current;
                if (app != null)
                {
                    var val = app.TryFindResource(key) as string;
                    if (!string.IsNullOrEmpty(val)) return val!;
                }
            }
            catch { }
            return fallback;
        }
    }
}
