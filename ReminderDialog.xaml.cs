using System;
using System.Globalization;
using System.Windows;

namespace LibmpvIptvClient
{
    public partial class ReminderDialog : Window
    {
        public string Action { get; private set; } = "notify";
        public int PreAlertSeconds { get; private set; } = 60;
        public ReminderDialog(string channel, string title, DateTime startLocal)
        {
            InitializeComponent();
            TxtTitle.Text = channel + " - " + title;
            TxtTime.Text = startLocal.ToString("yyyy-MM-dd HH:mm:ss");
        }
        void BtnNotify_Click(object sender, RoutedEventArgs e)
        {
            Action = "notify";
            if (int.TryParse(TbPreAlert.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) PreAlertSeconds = Math.Max(0, v);
            DialogResult = true;
            Close();
        }
    }
}
