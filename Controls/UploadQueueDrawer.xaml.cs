using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibmpvIptvClient.Controls
{
    public partial class UploadQueueDrawer : System.Windows.Controls.UserControl
    {
        public UploadQueueDrawer()
        {
            InitializeComponent();
            Loaded += (_, __) => Refresh();
        }
        void Refresh()
        {
            try
            {
                var items = LibmpvIptvClient.Services.UploadQueueService.Instance.GetSnapshot();
                GridQueue.ItemsSource = items;
                var running = items.Count(i => i.Status == "running");
                var failed = items.Count(i => i.Status == "failed");
                TxtSummary.Text = $"Total: {items.Count}, running: {running}, failed: {failed}";
            }
            catch { }
        }
        void BtnRefresh_Click(object sender, RoutedEventArgs e) => Refresh();
        void BtnRetry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as System.Windows.Controls.Button; var id = btn?.Tag?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(id)) return;
                LibmpvIptvClient.Services.UploadQueueService.Instance.Retry(id);
                Refresh();
            }
            catch { }
        }
        void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as System.Windows.Controls.Button; var id = btn?.Tag?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(id)) return;
                LibmpvIptvClient.Services.UploadQueueService.Instance.Remove(id);
                Refresh();
            }
            catch { }
        }
    }
}
