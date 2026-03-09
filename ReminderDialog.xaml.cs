using System;
using System.Globalization;
using System.Windows;

namespace LibmpvIptvClient
{
    public partial class ReminderDialog : Window
    {
        public string Action { get; private set; } = "notify";
        public int PreAlertSeconds { get; private set; } = 60;
        public string PlayMode { get; private set; } = "default";
        public ReminderDialog(string channel, string title, DateTime startLocal)
        {
            InitializeComponent();
            TxtTitle.Text = channel + " - " + title;
            TxtTime.Text = startLocal.ToString("yyyy-MM-dd HH:mm:ss");
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try { LibmpvIptvClient.Helpers.ThemeHelper.ApplyTitleBarByTheme(this); } catch { }
        }
        void BtnNotify_Click(object sender, RoutedEventArgs e)
        {
            Action = "notify";
            if (int.TryParse(TbPreAlert.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) PreAlertSeconds = Math.Max(0, v);
            DialogResult = true;
            Close();
        }
        void BtnAutoplay_Click(object sender, RoutedEventArgs e)
        {
            Action = "play";
            if (int.TryParse(TbPreAlert.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) PreAlertSeconds = Math.Max(0, v);
            try
            {
                var dlg = new AutoPlayModeDialog { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, Topmost = this.Topmost };
                if (dlg.ShowDialog() == true)
                {
                    PlayMode = dlg.Mode ?? "default";
                }
                else
                {
                    PlayMode = "default";
                }
            }
            catch { PlayMode = "default"; }
            DialogResult = true;
            Close();
        }
    }
}
