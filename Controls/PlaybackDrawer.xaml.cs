using System;
using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace LibmpvIptvClient.Controls
{
    public partial class PlaybackDrawer : UserControl
    {
        private ReplayConfig _config;

        public PlaybackDrawer()
        {
            InitializeComponent();
        }

        public void Load(ReplayConfig config)
        {
            _config = config;
            CbEnabled.IsChecked = config.Enabled;
            TbUrlFormat.Text = config.UrlFormat;
            SldDuration.Value = Math.Max(1, Math.Min(168, config.DurationHours));
            CbAppendEpgTime.IsChecked = config.AppendEpgTime;
        }

        public void Save(ReplayConfig config)
        {
            config.Enabled = CbEnabled.IsChecked == true;
            config.UrlFormat = TbUrlFormat.Text;
            config.DurationHours = (int)SldDuration.Value;
            config.AppendEpgTime = CbAppendEpgTime.IsChecked == true;
        }

        public bool HasChanges(ReplayConfig original)
        {
            if (original.Enabled != (CbEnabled.IsChecked == true)) return true;
            if (original.UrlFormat != TbUrlFormat.Text) return true;
            if (original.DurationHours != (int)SldDuration.Value) return true;
            if (original.AppendEpgTime != (CbAppendEpgTime.IsChecked == true)) return true;
            return false;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch { }
        }
    }
}
