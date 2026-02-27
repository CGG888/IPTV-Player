using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace LibmpvIptvClient
{
    public partial class SettingsWindow : Window
    {
        public PlaybackSettings Result { get; private set; } = new PlaybackSettings();
        public SettingsWindow(PlaybackSettings current)
        {
            InitializeComponent();
            CbHwdec.IsChecked = current.Hwdec;
            TbCacheSecs.Text = current.CacheSecs.ToString(CultureInfo.InvariantCulture);
            TbMaxBytes.Text = current.DemuxerMaxBytesMiB.ToString(CultureInfo.InvariantCulture);
            TbMaxBackBytes.Text = current.DemuxerMaxBackBytesMiB.ToString(CultureInfo.InvariantCulture);
            TbFccPrefetch.Text = current.FccPrefetchCount.ToString(CultureInfo.InvariantCulture);
            TbSourceTimeout.Text = current.SourceTimeoutSec.ToString(CultureInfo.InvariantCulture);
            TbEpgUrl.Text = current.CustomEpgUrl;
            TbLogoUrl.Text = current.CustomLogoUrl;
        }
        void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var s = new PlaybackSettings();
            s.Hwdec = CbHwdec.IsChecked == true;
            if (double.TryParse(TbCacheSecs.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var cache)) s.CacheSecs = Math.Max(0, cache);
            if (int.TryParse(TbMaxBytes.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxb)) s.DemuxerMaxBytesMiB = Math.Max(1, maxb);
            if (int.TryParse(TbMaxBackBytes.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var backb)) s.DemuxerMaxBackBytesMiB = Math.Max(0, backb);
            if (int.TryParse(TbFccPrefetch.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var pf)) s.FccPrefetchCount = Math.Max(0, Math.Min(3, pf));
            if (int.TryParse(TbSourceTimeout.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var st)) s.SourceTimeoutSec = Math.Max(1, st);
            s.CustomEpgUrl = TbEpgUrl.Text?.Trim() ?? "";
            s.CustomLogoUrl = TbLogoUrl.Text?.Trim() ?? "";
            Result = s;
            DialogResult = true;
            Close();
        }
        void BtnDebug_Click(object sender, RoutedEventArgs e)
        {
            DebugRequested?.Invoke();
        }
        void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 1)
            {
                try { DragMove(); } catch { }
            }
        }
        void BtnCloseTitle_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        public event Action? DebugRequested;
    }
}
