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
        }

        public void Save(LogoConfig config)
        {
            config.Enabled = CbEnabled.IsChecked == true;
            config.Url = TbUrl.Text;
        }

        public bool HasChanges(LogoConfig original)
        {
            if (original.Enabled != (CbEnabled.IsChecked == true)) return true;
            if (original.Url != TbUrl.Text) return true;
            return false;
        }
    }
}
