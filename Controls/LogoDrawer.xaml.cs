using System;
using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace LibmpvIptvClient.Controls
{
    public partial class LogoDrawer : UserControl
    {
        private LogoConfig _config;

        public LogoDrawer()
        {
            InitializeComponent();
        }

        public void Load(LogoConfig config)
        {
            _config = config;
            CbEnabled.IsChecked = config.Enabled;
            TbUrl.Text = config.Url;
            try
            {
                CbCache.IsChecked = config.EnableCache;
                var oldDefault = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SrcBox", "logo-cache");
                if (string.IsNullOrWhiteSpace(config.CacheDir) || string.Equals(config.CacheDir, oldDefault, StringComparison.OrdinalIgnoreCase))
                {
                    string exeDir = "";
                    try { exeDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "") ?? ""; } catch { }
                    if (string.IsNullOrWhiteSpace(exeDir))
                    {
                        try { exeDir = AppContext.BaseDirectory; } catch { }
                    }
                    TbCacheDir.Text = System.IO.Path.Combine(exeDir, "logo-cache");
                }
                else
                {
                    TbCacheDir.Text = config.CacheDir;
                }
                TbCacheTtl.Text = config.CacheTtlHours.ToString(System.Globalization.CultureInfo.InvariantCulture);
                TbCacheMax.Text = config.CacheMaxMiB.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            catch { }
        }

        public void Save(LogoConfig config)
        {
            config.Enabled = CbEnabled.IsChecked == true;
            config.Url = TbUrl.Text;
            config.EnableCache = CbCache.IsChecked == true;
            config.CacheDir = (TbCacheDir.Text ?? "").Trim();
            if (double.TryParse(TbCacheTtl.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ttl))
                config.CacheTtlHours = Math.Max(1, ttl);
            if (int.TryParse(TbCacheMax.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var max))
                config.CacheMaxMiB = Math.Max(50, max);
        }

        public bool HasChanges(LogoConfig original)
        {
            if (original.Enabled != (CbEnabled.IsChecked == true)) return true;
            if (original.Url != TbUrl.Text) return true;
            if (original.EnableCache != (CbCache.IsChecked == true)) return true;
            if ((original.CacheDir ?? "") != (TbCacheDir.Text ?? "")) return true;
            if (original.CacheTtlHours.ToString() != (TbCacheTtl.Text ?? "")) return true;
            if (original.CacheMaxMiB.ToString() != (TbCacheMax.Text ?? "")) return true;
            return false;
        }
        void BtnClearLogoCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dir = (TbCacheDir.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(dir)) return;
                if (System.IO.Directory.Exists(dir))
                {
                    foreach (var f in System.IO.Directory.EnumerateFiles(dir, "*", System.IO.SearchOption.AllDirectories))
                    {
                        try { System.IO.File.Delete(f); } catch { }
                    }
                }
            }
            catch { }
        }
    }
}
