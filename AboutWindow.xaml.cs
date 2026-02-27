using System;
using System.Windows;
using System.Diagnostics;
using System.Reflection;

namespace LibmpvIptvClient
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            // Get version from Assembly Informational Version (matches <Version> in csproj)
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            // Fallback to assembly version if informational version is missing
            if (string.IsNullOrEmpty(version))
            {
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            }

            // Clean up version string (remove commit hash if present, e.g. 1.0.1+a1b2c3)
            if (version != null && version.Contains("+"))
            {
                version = version.Substring(0, version.IndexOf("+"));
            }

            TxtVersion.Text = $"v{version}";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Direct link to releases page
                Process.Start(new ProcessStartInfo("https://github.com/CGG888/IPTV-Player/releases") { UseShellExecute = true });
            }
            catch { }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch { }
        }
    }
}