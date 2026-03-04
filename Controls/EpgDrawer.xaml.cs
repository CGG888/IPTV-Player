using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace LibmpvIptvClient.Controls
{
    public partial class EpgDrawer : UserControl
    {
        private EpgConfig _config;

        public EpgDrawer()
        {
            InitializeComponent();
        }

        public void Load(EpgConfig config)
        {
            _config = config;
            CbEnabled.IsChecked = config.Enabled;
            TbUrl.Text = config.Url;
            TbRefresh.Text = config.RefreshIntervalHours.ToString(CultureInfo.InvariantCulture);
            CbSmartMatch.IsChecked = config.EnableSmartMatch;
        }

        public void Save(EpgConfig config)
        {
            config.Enabled = CbEnabled.IsChecked == true;
            config.Url = TbUrl.Text;
            if (double.TryParse(TbRefresh.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            {
                config.RefreshIntervalHours = Math.Max(0.1, val);
            }
            config.EnableSmartMatch = CbSmartMatch.IsChecked == true;
        }

        public bool HasChanges(EpgConfig original)
        {
            if (original.Enabled != (CbEnabled.IsChecked == true)) return true;
            if (original.Url != TbUrl.Text) return true;
            if (original.EnableSmartMatch != (CbSmartMatch.IsChecked == true)) return true;
            
            double uiVal = 24;
            double.TryParse(TbRefresh.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out uiVal);
            if (Math.Abs(original.RefreshIntervalHours - uiVal) > 0.01) return true;
            
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
