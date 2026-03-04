using System;
using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace LibmpvIptvClient.Controls
{
    public partial class TimeshiftDrawer : UserControl
    {
        private TimeshiftConfig _config;

        public TimeshiftDrawer()
        {
            InitializeComponent();
        }

        public void Load(TimeshiftConfig config)
        {
            _config = config;
            CbEnabled.IsChecked = config.Enabled;
            TbUrlFormat.Text = config.UrlFormat;
            SldDuration.Value = Math.Max(1, Math.Min(168, config.DurationHours));
        }

        public void Save(TimeshiftConfig config)
        {
            config.Enabled = CbEnabled.IsChecked == true;
            config.UrlFormat = TbUrlFormat.Text;
            config.DurationHours = (int)SldDuration.Value;
        }

        public bool HasChanges(TimeshiftConfig original)
        {
            if (original.Enabled != (CbEnabled.IsChecked == true)) return true;
            if (original.UrlFormat != TbUrlFormat.Text) return true;
            if (original.DurationHours != (int)SldDuration.Value) return true;
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
