using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Diagnostics;

namespace LibmpvIptvClient
{
    public partial class SettingsWindow : Window
    {
        public PlaybackSettings Result { get; private set; } = new PlaybackSettings();
        private ObservableCollection<string> _cdn = new ObservableCollection<string>();
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
            TbTimeshiftHours.Text = Math.Max(0, current.TimeshiftHours).ToString(CultureInfo.InvariantCulture);
            // CDN
            _cdn = new ObservableCollection<string>(current.UpdateCdnMirrors ?? new System.Collections.Generic.List<string>());
            ListCdn.ItemsSource = _cdn;
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
            if (int.TryParse(TbTimeshiftHours.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var tsh)) s.TimeshiftHours = Math.Max(0, Math.Min(168, tsh));
            // CDN
            s.UpdateCdnMirrors = new System.Collections.Generic.List<string>(_cdn);
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

        // CDN 管理交互
        void BtnCdnAdd_Click(object sender, RoutedEventArgs e)
        {
            var t = (TbNewCdn.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(t) || !t.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
            if (!_cdn.Contains(t)) _cdn.Add(t);
            TbNewCdn.Text = "";
        }
        void BtnCdnRemove_Click(object sender, RoutedEventArgs e)
        {
            var sel = ListCdn.SelectedItems;
            if (sel == null || sel.Count == 0) return;
            var toRemove = new System.Collections.Generic.List<string>();
            foreach (var it in sel) if (it is string s) toRemove.Add(s);
            foreach (var s in toRemove) _cdn.Remove(s);
        }
        void BtnCdnUp_Click(object sender, RoutedEventArgs e)
        {
            if (ListCdn.SelectedItem is string s)
            {
                var idx = _cdn.IndexOf(s);
                if (idx > 0)
                {
                    _cdn.RemoveAt(idx);
                    _cdn.Insert(idx - 1, s);
                    ListCdn.SelectedIndex = idx - 1;
                }
            }
        }
        void BtnCdnDown_Click(object sender, RoutedEventArgs e)
        {
            if (ListCdn.SelectedItem is string s)
            {
                var idx = _cdn.IndexOf(s);
                if (idx >= 0 && idx < _cdn.Count - 1)
                {
                    _cdn.RemoveAt(idx);
                    _cdn.Insert(idx + 1, s);
                    ListCdn.SelectedIndex = idx + 1;
                }
            }
        }
        async void BtnCdnSpeedTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cdn.Count == 0) return;
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                var targets = new System.Collections.Generic.List<(string url, double ms)>();
                foreach (var c in _cdn)
                {
                    var testUrl = c.TrimEnd('/') + "/https://github.com/favicon.ico";
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        using var resp = await client.GetAsync(testUrl);
                        sw.Stop();
                        targets.Add((c, resp.IsSuccessStatusCode ? sw.Elapsed.TotalMilliseconds : double.MaxValue));
                    }
                    catch { targets.Add((c, double.MaxValue)); }
                }
                // 排序（成功优先、延迟小在前）
                targets.Sort((a, b) => a.ms.CompareTo(b.ms));
                _cdn.Clear();
                foreach (var t in targets) _cdn.Add(t.url);
            }
            catch { }
        }
    }
}
