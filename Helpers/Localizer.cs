using System;
using LibmpvIptvClient.Architecture.Application.Shared;
using LibmpvIptvClient.Architecture.Core;
using WpfApp = System.Windows;

namespace LibmpvIptvClient.Helpers
{
    public static class Localizer
    {
        public static string S(string key, string fallback)
        {
            try
            {
                try
                {
                    var service = SrcBoxArchitectureHost.Kernel.Resolve<ILocalizationService>();
                    return service.GetText(key, fallback);
                }
                catch { }
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
