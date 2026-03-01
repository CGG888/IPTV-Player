using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Xml;
using System.Xml.Linq;

namespace LibmpvIptvClient.Helpers
{
    public static class ResxLocalizer
    {
        static readonly object _lock = new object();
        static CultureInfo _culture = CultureInfo.CurrentUICulture;
        static Dictionary<string, string> _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void SetCulture(CultureInfo ci)
        {
            lock (_lock)
            {
                _culture = ci;
                Reload();
            }
        }

        public static void Reload()
        {
            lock (_lock)
            {
                _cache.Clear();
                // Try exact, then parent, then neutral
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var folder = Path.Combine(baseDir, "Resources", "Resx");
                var tried = new List<string>();
                if (!Directory.Exists(folder)) return;
                string[] candidates = new[]
                {
                    Path.Combine(folder, $"Strings.{_culture.Name}.resx"),
                    Path.Combine(folder, $"Strings.{_culture.TwoLetterISOLanguageName}.resx"),
                    Path.Combine(folder, "Strings.resx"),
                };
                foreach (var f in candidates)
                {
                    if (File.Exists(f))
                    {
                        tried.Add(f);
                        try { LoadResx(f, _cache); } catch { }
                    }
                }
            }
        }

        public static string Get(string key, string fallback)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)) return v;
            }
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    var val = app.TryFindResource(key) as string;
                    if (!string.IsNullOrEmpty(val)) return val!;
                }
            }
            catch { }
            return fallback;
        }

        static void LoadResx(string path, Dictionary<string, string> map)
        {
            using (var reader = new ResXResourceReader(path))
            {
                foreach (System.Collections.DictionaryEntry d in reader)
                {
                    if (d.Key is string k && d.Value is string s)
                    {
                        map[k] = s;
                    }
                }
            }
        }
    }
}
