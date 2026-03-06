using System;
using System.Reflection;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Animation;

using System.Text.Json;
namespace LibmpvIptvClient
{
    public partial class SettingsWindow : Window
    {
        public event Action<PlaybackSettings>? ApplySettingsRequested;
        public PlaybackSettings Result { get; private set; } = new PlaybackSettings();
        private ObservableCollection<string> _cdn = new ObservableCollection<string>();
        private HttpClient _httpInline => LibmpvIptvClient.Services.HttpClientService.Instance.Client;
        private AboutWindow.ReleaseInfo? _latestInline;
        private string _currentVersionInline = "0.0.0";
        
        private Controls.PlaybackDrawer? _playbackDrawer;
        private Controls.TimeshiftDrawer? _timeshiftDrawer;
        private Controls.EpgDrawer? _epgDrawer;
        private Controls.LogoDrawer? _logoDrawer;
        
        private ReplayConfig _tempReplay;
        private TimeshiftConfig _tempTimeshift;
        private EpgConfig _tempEpg;
        private LogoConfig _tempLogo;
        
        // private FrameworkElement? _currentDrawer;

        public SettingsWindow(PlaybackSettings current)
        {
            InitializeComponent();
            PreviewKeyDown += Settings_PreviewKeyDown;
            
            // Deep copy for temp configs
            _tempReplay = new ReplayConfig 
            { 
                Enabled = current.Replay.Enabled, 
                UrlFormat = current.Replay.UrlFormat, 
                DurationHours = current.Replay.DurationHours 
            };
            _tempTimeshift = new TimeshiftConfig 
            { 
                Enabled = current.Timeshift.Enabled, 
                UrlFormat = current.Timeshift.UrlFormat, 
                DurationHours = current.Timeshift.DurationHours 
            };
            _tempEpg = new EpgConfig
            {
                Enabled = current.Epg.Enabled,
                Url = current.Epg.Url,
                RefreshIntervalHours = current.Epg.RefreshIntervalHours
            };
            _tempLogo = new LogoConfig
            {
                Enabled = current.Logo.Enabled,
                Url = current.Logo.Url
            };

            CbHwdec.IsChecked = current.Hwdec;
            TbCacheSecs.Text = current.CacheSecs.ToString(CultureInfo.InvariantCulture);
            TbMaxBytes.Text = current.DemuxerMaxBytesMiB.ToString(CultureInfo.InvariantCulture);
            TbMaxBackBytes.Text = current.DemuxerMaxBackBytesMiB.ToString(CultureInfo.InvariantCulture);
            TbFccPrefetch.Text = current.FccPrefetchCount.ToString(CultureInfo.InvariantCulture);
            TbSourceTimeout.Text = current.SourceTimeoutSec.ToString(CultureInfo.InvariantCulture);
            // New playback options
            try
            {
                CbAdaptive.IsChecked = current.EnableProtocolAdaptive;
                CbHlsStart.IsChecked = current.HlsStartAtLiveEdge;
                TbHlsReadahead.Text = current.HlsReadaheadSecs.ToString(CultureInfo.InvariantCulture);
                TbAlang.Text = current.Alang ?? "";
                TbSlang.Text = current.Slang ?? "";
                TbMpvTimeout.Text = current.MpvNetworkTimeoutSec.ToString(CultureInfo.InvariantCulture);
            }
            catch { }
            
            try
            {
                // Init Controls (Lazy load not strictly needed for local controls, but we populate them now)
                // We reference the controls by x:Name defined in XAML
                EpgSettingsControl?.Load(_tempEpg);
                LogoSettingsControl?.Load(_tempLogo);
                ReplaySettingsControl?.Load(_tempReplay);
                TimeshiftSettingsControl?.Load(_tempTimeshift);

                // Wire up pointers for HasChanges check if needed, or just check controls directly
                _epgDrawer = EpgSettingsControl;
                _logoDrawer = LogoSettingsControl;
                _playbackDrawer = ReplaySettingsControl;
                _timeshiftDrawer = TimeshiftSettingsControl;
                
                // Time Override UI init
                try
                {
                    var to = current.TimeOverride ?? new TimeOverrideConfig();
                    if (CbTimeOverrideEnabled != null) CbTimeOverrideEnabled.IsChecked = to.Enabled;
                    if (CbTimeOverrideMode != null)
                    {
                        CbTimeOverrideMode.SelectedIndex = string.Equals(to.Mode, "replace_all", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                        _prevModeIndex = CbTimeOverrideMode.SelectedIndex;
                        CbTimeOverrideMode.SelectionChanged += (_, __) => OnTimeOverrideModeChanged();
                    }
                    if (CbTimeLayout != null)
                    {
                        if (string.Equals(to.Layout, "playseek", StringComparison.OrdinalIgnoreCase)) CbTimeLayout.SelectedIndex = 1;
                        else if (string.Equals(to.Layout, "start_duration", StringComparison.OrdinalIgnoreCase)) CbTimeLayout.SelectedIndex = 2;
                        else CbTimeLayout.SelectedIndex = 0;
                        CbTimeLayout.SelectionChanged += (_, __) => UpdateTimeOverrideKeyFieldsVisibility();
                    }
                    if (CbTimeEncoding != null)
                    {
                        if (string.Equals(to.Encoding, "utc", StringComparison.OrdinalIgnoreCase)) CbTimeEncoding.SelectedIndex = 1;
                        else if (string.Equals(to.Encoding, "unix", StringComparison.OrdinalIgnoreCase)) CbTimeEncoding.SelectedIndex = 2;
                        else if (string.Equals(to.Encoding, "unix_ms", StringComparison.OrdinalIgnoreCase)) CbTimeEncoding.SelectedIndex = 3;
                        else CbTimeEncoding.SelectedIndex = 0;
                    }
                    if (CbStartKey != null) CbStartKey.Text = to.StartKey ?? "start";
                    if (CbEndKey != null) CbEndKey.Text = to.EndKey ?? "end";
                    if (CbDurationKey != null) CbDurationKey.Text = to.DurationKey ?? "duration";
                    if (CbPlayseekKey != null) CbPlayseekKey.Text = to.PlayseekKey ?? "playseek";
                    if (CbUrlEncode != null) CbUrlEncode.IsChecked = to.UrlEncode;
                    UpdateTimeOverrideKeyFieldsVisibility();
                }
                catch { }
            }
            catch { }

            try
            {
                // 初始化界面：语言 & 主题
                if (CbLanguage != null)
                {
                    var lang = (current.Language ?? "").Trim();
                    int idx = 0;
                    if (string.Equals(lang, "zh-CN", StringComparison.OrdinalIgnoreCase)) idx = 1;
                    else if (string.Equals(lang, "zh-TW", StringComparison.OrdinalIgnoreCase)) idx = 2;
                    else if (string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase)) idx = 3;
                    else if (string.Equals(lang, "ru-RU", StringComparison.OrdinalIgnoreCase)) idx = 4;
                    CbLanguage.SelectedIndex = idx;
                    CbLanguage.SelectionChanged += CbLanguage_SelectionChanged;
                }
                if (CbTheme != null)
                {
                    var theme = (current.ThemeMode ?? "System").Trim();
                    int idx = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase) ? 1
                            : string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                    CbTheme.SelectedIndex = idx;
                    CbTheme.SelectionChanged += CbTheme_SelectionChanged;
                }
            }
            catch { }
            // CDN
            _cdn = new ObservableCollection<string>(current.UpdateCdnMirrors ?? new System.Collections.Generic.List<string>());
            ListCdn.ItemsSource = _cdn;
            try
            {
                var v = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;
                if (string.IsNullOrEmpty(v))
                    v = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
                if (v != null && v.Contains("+")) v = v.Substring(0, v.IndexOf("+"));
                _currentVersionInline = v ?? "0.0.0";
                if (TxtVersionInline != null) TxtVersionInline.Text = $"v{_currentVersionInline}";
            }
            catch { }
            _ = CheckUpdateInlineAsync(false);
        }
        void Settings_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == System.Windows.Input.Key.F1)
                {
                    DebugRequested?.Invoke();
                    e.Handled = true;
                }
            }
            catch { }
        }
        
        int _prevModeIndex = 0;
        void OnTimeOverrideModeChanged()
        {
            try
            {
                if (CbTimeOverrideMode == null) return;
                var idx = CbTimeOverrideMode.SelectedIndex;
                if (idx == 1)
                {
                    var msg = LibmpvIptvClient.Helpers.ResxLocalizer.Get("UI_TimeOverride_ConfirmReplaceAll",
                        "将完全替换时间参数，可能影响非时间参数的顺序或兼容性。确定继续？");
                    var title = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示");
                    var ok = ModernMessageBox.Show(this, msg, title, MessageBoxButton.YesNo) == true;
                    if (!ok)
                    {
                        CbTimeOverrideMode.SelectedIndex = _prevModeIndex;
                        return;
                    }
                }
                _prevModeIndex = idx;
            }
            catch { }
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
            // New
            s.EnableProtocolAdaptive = CbAdaptive.IsChecked == true;
            s.HlsStartAtLiveEdge = CbHlsStart.IsChecked == true;
            if (double.TryParse(TbHlsReadahead.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var ra)) s.HlsReadaheadSecs = Math.Max(0, ra);
            s.Alang = (TbAlang.Text ?? "").Trim();
            s.Slang = (TbSlang.Text ?? "").Trim();
            if (int.TryParse(TbMpvTimeout.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var nt)) s.MpvNetworkTimeoutSec = Math.Max(0, nt);
            
            // Save from Controls to Temp Configs
            _playbackDrawer?.Save(_tempReplay);
            _timeshiftDrawer?.Save(_tempTimeshift);
            _epgDrawer?.Save(_tempEpg);
            _logoDrawer?.Save(_tempLogo);

            // Assign Temp Configs to Result
            s.Replay = _tempReplay;
            s.Timeshift = _tempTimeshift;
            s.Epg = _tempEpg;
            s.Logo = _tempLogo;

            // CDN
            CleanCdnListForSave(s);
            // 界面
            try
            {
                s.Language = GetSelectedTag(CbLanguage);
                var themeTag = GetSelectedTag(CbTheme);
                s.ThemeMode = string.IsNullOrWhiteSpace(themeTag) ? "System" : themeTag;
            }
            catch { }
            Result = s;
            
            // TimeOverride save
            try
            {
                var to = new TimeOverrideConfig();
                to.Enabled = CbTimeOverrideEnabled?.IsChecked == true;
                to.Mode = GetSelectedTag(CbTimeOverrideMode);
                to.Layout = GetSelectedTag(CbTimeLayout);
                to.Encoding = GetSelectedTag(CbTimeEncoding);
                to.StartKey = (CbStartKey?.Text ?? "start").Trim();
                to.EndKey = (CbEndKey?.Text ?? "end").Trim();
                to.DurationKey = (CbDurationKey?.Text ?? "duration").Trim();
                to.PlayseekKey = (CbPlayseekKey?.Text ?? "playseek").Trim();
                to.UrlEncode = CbUrlEncode?.IsChecked == true;
                s.TimeOverride = to;
            }
            catch { }
            ApplySettingsRequested?.Invoke(s);

            ModernMessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_SettingsSaved", "设置已保存"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK);
            // DialogResult = true;
            // Close();
        }
        
        void BtnPreviewReplay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                var start = now.AddHours(-3);
                var end = start.AddHours(1);
                var settings = BuildTempSettings();
                settings.TimeOverride.Enabled = true;
                var baseUrl = TbBaseUrl?.Text?.Trim() ?? "";
                var url = LibmpvIptvClient.Services.UrlTimeRewriter.RewriteIfEnabled(settings, baseUrl, start, end, false);
                if (TbPreviewResult != null) TbPreviewResult.Text = url;
                if (TbPreviewTemplate != null) TbPreviewTemplate.Text = BuildPlaceholderPreview(baseUrl, false);
            }
            catch { }
        }
        void BtnPreviewTimeshift_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                var start = now.AddHours(-3);
                var end = start.AddHours(1);
                var settings = BuildTempSettings();
                settings.TimeOverride.Enabled = true;
                var baseUrl = TbBaseUrl?.Text?.Trim() ?? "";
                var url = LibmpvIptvClient.Services.UrlTimeRewriter.RewriteIfEnabled(settings, baseUrl, start, end, true);
                if (TbPreviewResult != null) TbPreviewResult.Text = url;
                if (TbPreviewTemplate != null) TbPreviewTemplate.Text = BuildPlaceholderPreview(baseUrl, true);
            }
            catch { }
        }
        PlaybackSettings BuildTempSettings()
        {
            var s = new PlaybackSettings();
            try
            {
                var to = new TimeOverrideConfig();
                to.Enabled = CbTimeOverrideEnabled?.IsChecked == true;
                to.Mode = GetSelectedTag(CbTimeOverrideMode);
                to.Layout = GetSelectedTag(CbTimeLayout);
                to.Encoding = GetSelectedTag(CbTimeEncoding);
                to.StartKey = (CbStartKey?.Text ?? "start").Trim();
                to.EndKey = (CbEndKey?.Text ?? "end").Trim();
                to.DurationKey = (CbDurationKey?.Text ?? "duration").Trim();
                to.PlayseekKey = (CbPlayseekKey?.Text ?? "playseek").Trim();
                to.UrlEncode = CbUrlEncode?.IsChecked == true;
                s.TimeOverride = to;
            }
            catch { }
            return s;
        }
        void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            try { Close(); } catch { }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                if (Owner != null)
                {
                    Owner.Activate();
                }
            }
            catch { }
        }
        void BtnOpenDocs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://srcbox.top/guide/catchup-timeshift.html",
                    UseShellExecute = true
                });
            }
            catch { }
        }
        void BtnOpenIssues_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/CGG888/SrcBox/issues",
                    UseShellExecute = true
                });
            }
            catch { }
        }
        
        void UpdateTimeOverrideKeyFieldsVisibility()
        {
            try
            {
                var layout = GetSelectedTag(CbTimeLayout);
                if (string.IsNullOrEmpty(layout)) layout = "start_end";
                bool showStart = layout == "start_end" || layout == "start_duration";
                bool showEnd = layout == "start_end";
                bool showDuration = layout == "start_duration";
                bool showPlayseek = layout == "playseek";
                
                if (CbStartKey != null) CbStartKey.Visibility = showStart ? Visibility.Visible : Visibility.Collapsed;
                if (CbEndKey != null) CbEndKey.Visibility = showEnd ? Visibility.Visible : Visibility.Collapsed;
                if (CbDurationKey != null) CbDurationKey.Visibility = showDuration ? Visibility.Visible : Visibility.Collapsed;
                if (SpPlayseekRow != null) SpPlayseekRow.Visibility = showPlayseek ? Visibility.Visible : Visibility.Collapsed;
                if (CbPlayseekKey != null) CbPlayseekKey.Visibility = showPlayseek ? Visibility.Visible : Visibility.Collapsed;
                
                // 对应标签同步隐藏/显示
                if (LblStartKey != null) LblStartKey.Visibility = showStart ? Visibility.Visible : Visibility.Collapsed;
                if (LblEndKey != null) LblEndKey.Visibility = showEnd ? Visibility.Visible : Visibility.Collapsed;
                if (LblDurationKey != null) LblDurationKey.Visibility = showDuration ? Visibility.Visible : Visibility.Collapsed;
                if (LblPlayseekKey != null) LblPlayseekKey.Visibility = showPlayseek ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }
        
        string BuildPlaceholderPreview(string baseUrl, bool isTimeshift)
        {
            try
            {
                var layout = GetSelectedTag(CbTimeLayout);
                var encoding = GetSelectedTag(CbTimeEncoding);
                if (string.IsNullOrEmpty(layout)) layout = "start_end";
                if (string.IsNullOrEmpty(encoding)) encoding = "local";
                var startKey = (CbStartKey?.Text ?? "start").Trim();
                var endKey = (CbEndKey?.Text ?? "end").Trim();
                var durationKey = (CbDurationKey?.Text ?? "duration").Trim();
                var playseekKey = (CbPlayseekKey?.Text ?? "playseek").Trim();

                // Choose placeholder tokens by encoding
                string pStart, pEnd, pDur = "${duration}";
                if (encoding.Equals("utc", StringComparison.OrdinalIgnoreCase))
                {
                    pStart = "${(b)yyyyMMddHHmmss|UTC}";
                    pEnd = "${(e)yyyyMMddHHmmss|UTC}";
                }
                else if (encoding.Equals("unix", StringComparison.OrdinalIgnoreCase) || encoding.Equals("unix_ms", StringComparison.OrdinalIgnoreCase))
                {
                    pStart = "${timestamp}";
                    pEnd = "${end_timestamp}";
                }
                else
                {
                    pStart = "${(b)yyyyMMddHHmmss}";
                    pEnd = "${(e)yyyyMMddHHmmss}";
                }

                string query;
                if (layout == "playseek")
                {
                    query = $"{playseekKey}={pStart}-{pEnd}";
                }
                else if (layout == "start_duration" || (layout == "auto" && isTimeshift))
                {
                    query = $"{startKey}={pStart}&{durationKey}={pDur}";
                }
                else
                {
                    query = $"{startKey}={pStart}&{endKey}={pEnd}";
                }

                if (string.IsNullOrWhiteSpace(baseUrl)) return query;
                var sep = baseUrl.Contains("?") ? "&" : "?";
                return baseUrl + sep + query;
            }
            catch { }
            return "";
        }
        string GetSelectedTag(System.Windows.Controls.ComboBox cb)
        {
            try
            {
                if (cb?.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    return (item.Tag ?? "").ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        void CbLanguage_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Do nothing immediately. Apply on Save.
        }

        void CbTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Do nothing immediately. Apply on Save.
        }
        void BtnDebug_Click(object sender, RoutedEventArgs e)
        {
            DebugRequested?.Invoke();
        }
        void BtnOpenAbout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = this.Owner ?? this;
                var dlg = new AboutWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                try { dlg.Topmost = this.Topmost; } catch { }
                dlg.ShowDialog();
            }
            catch { }
        }
        async void BtnCheckUpdateInline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BtnCheckUpdateInline != null)
                {
                    BtnCheckUpdateInline.IsEnabled = false;
                    var old = BtnCheckUpdateInline.Content;
                    BtnCheckUpdateInline.Content = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_Checking", "检查中…");
                    try { await CheckUpdateInlineAsync(true); }
                    finally
                    {
                        BtnCheckUpdateInline.Content = old;
                        BtnCheckUpdateInline.IsEnabled = true;
                    }
                }
                else
                {
                    await CheckUpdateInlineAsync(true);
                }
            }
            catch { }
        }

        private void OpenDrawer()
        {
            // Drawer logic removed
        }

        private void CloseDrawer()
        {
            // Drawer logic removed
        }

        private void DrawerOverlay_Click(object sender, MouseButtonEventArgs e)
        {
            // Drawer logic removed
        }
/*
        void BtnViewUpdateInline_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BtnViewUpdate != null)
                {
                    BtnViewUpdate.IsEnabled = false;
                    var old = BtnViewUpdate.Content;
                    BtnViewUpdate.Content = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_Opening", "打开中…");
                    try
                    {
                        if (_latestInline == null)
                        {
                            _ = CheckUpdateInlineAsync(true);
                            return;
                        }
                        var dlg = new UpdateDialog(_latestInline);
                        dlg.Owner = this;
                        dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        dlg.Topmost = this.Topmost;
                        dlg.ShowDialog();
                    }
                    finally
                    {
                        BtnViewUpdate.Content = old;
                        BtnViewUpdate.IsEnabled = true;
                    }
                    return;
                }
                if (_latestInline == null)
                {
                    // 若尚未获取，则执行一次交互式检查
                    _ = CheckUpdateInlineAsync(true);
                    return;
                }
                var dlg2 = new UpdateDialog(_latestInline);
                dlg2.Owner = this;
                dlg2.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dlg2.Topmost = this.Topmost;
                dlg2.ShowDialog();
            }
            catch { }
        }
*/
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
                var targets = new System.Collections.Generic.List<(string url, long ms)>();
                using var ping = new System.Net.NetworkInformation.Ping();
                
                // 为了显示结果，我们需要更新 ObservableCollection 中的字符串
                // 但简单的 string 列表无法直接显示 ping 值
                // 方案：将 ObservableCollection 改为 ItemViewModel 或者直接修改字符串内容 (e.g. "url [100ms]")
                // 但考虑到后续保存需要纯 URL，我们这里仅做排序和临时显示，或者采用 "url|ms" 格式？
                // 鉴于用户要求“显示不通”，我们修改列表内容格式
                
                // 1. 先进行测速
                foreach (var c in _cdn)
                {
                    // 清理旧的 Ping 结果标记 (假设格式为 "url [xxxms]" 或 "url [Failed]")
                    var cleanUrl = c.Split(' ')[0];
                    long rtt = long.MaxValue;
                    try
                    {
                        // Extract host from URL
                        string host = cleanUrl;
                        if (Uri.TryCreate(cleanUrl, UriKind.Absolute, out var uri))
                        {
                            host = uri.Host;
                        }
                        
                        // Send Ping
                        var reply = await ping.SendPingAsync(host, 3000);
                        if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                        {
                            rtt = reply.RoundtripTime;
                        }
                    }
                    catch { }
                    targets.Add((cleanUrl, rtt));
                }

                // 2. 排序（延迟小在前）
                targets.Sort((a, b) => a.ms.CompareTo(b.ms));

                // 3. 更新 UI 列表，附带 Ping 值
                _cdn.Clear();
                foreach (var t in targets)
                {
                    string display;
                    if (t.ms == long.MaxValue)
                        display = $"{t.url} [Failed]";
                    else
                        display = $"{t.url} [{t.ms}ms]";
                    _cdn.Add(display);
                }
            }
            catch { }
        }

        // 保存前需要清理 Ping 结果标记
        void CleanCdnListForSave(PlaybackSettings s)
        {
            var cleanList = new System.Collections.Generic.List<string>();
            foreach (var item in _cdn)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var parts = item.Split(' '); // Split by space to remove [xxxms]
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    cleanList.Add(parts[0].Trim());
                }
            }
            s.UpdateCdnMirrors = cleanList;
        }
        void Inline_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); } catch { }
        }
        async void BtnLicense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BtnLicense != null)
                {
                    BtnLicense.IsEnabled = false;
                    var old = BtnLicense.Content;
                    BtnLicense.Content = "加载中…";
                    try
                    {
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        var lic = System.IO.Path.Combine(baseDir, "LICENSE.txt");
                        if (!System.IO.File.Exists(lic)) lic = System.IO.Path.Combine(baseDir, "license.txt");
                        string content = "";
                        if (System.IO.File.Exists(lic)) content = System.IO.File.ReadAllText(lic);
                        if (string.IsNullOrEmpty(content))
                        {
                            try
                            {
                                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                                content = await _httpInline.GetStringAsync("https://raw.githubusercontent.com/CGG888/SrcBox/main/LICENSE.txt", cts.Token);
                            }
                            catch { }
                        }
                        if (string.IsNullOrEmpty(content))
                        {
                            ModernMessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_FetchLicenseFailed", "无法获取许可证内容。"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK);
                            return;
                        }
                        var dlg = new TextViewerDialog(LibmpvIptvClient.Helpers.Localizer.S("UI_License", "开源许可证"), content) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, Topmost = this.Topmost };
                        dlg.ShowDialog();
                    }
                    finally
                    {
                        BtnLicense.Content = old;
                        BtnLicense.IsEnabled = true;
                    }
                    return;
                }
                var baseDir2 = AppDomain.CurrentDomain.BaseDirectory;
                var lic2 = System.IO.Path.Combine(baseDir2, "LICENSE.txt");
                if (!System.IO.File.Exists(lic2)) lic2 = System.IO.Path.Combine(baseDir2, "license.txt");
                string content2 = "";
                if (System.IO.File.Exists(lic2)) content2 = System.IO.File.ReadAllText(lic2);
                if (string.IsNullOrEmpty(content2))
                {
                    try
                    {
                        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                        content2 = await _httpInline.GetStringAsync("https://raw.githubusercontent.com/CGG888/SrcBox/main/LICENSE.txt", cts.Token);
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(content2))
                {
                    ModernMessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_FetchLicenseFailed", "无法获取许可证内容。"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK);
                    return;
                }
                var dlg2 = new TextViewerDialog(LibmpvIptvClient.Helpers.Localizer.S("UI_License", "开源许可证"), content2) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, Topmost = this.Topmost };
                dlg2.ShowDialog();
            }
            catch { }
        }
        async void BtnThirdParty_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BtnThirdParty != null)
                {
                    BtnThirdParty.IsEnabled = false;
                    var old = BtnThirdParty.Content;
                    BtnThirdParty.Content = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_Loading", "加载中…");
                    try
                    {
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        var third = System.IO.Path.Combine(baseDir, "THIRD-PARTY-NOTICES.txt");
                        if (!System.IO.File.Exists(third)) third = System.IO.Path.Combine(baseDir, "Third-Party-Notices.txt");
                        string content = "";
                        if (System.IO.File.Exists(third)) content = System.IO.File.ReadAllText(third);
                        if (string.IsNullOrEmpty(content))
                        {
                            try
                            {
                                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                                content = await _httpInline.GetStringAsync("https://raw.githubusercontent.com/CGG888/SrcBox/main/THIRD-PARTY-NOTICES.txt", cts.Token);
                            }
                            catch { }
                        }
                        if (string.IsNullOrEmpty(content))
                        {
                            ModernMessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_FetchThirdPartyFailed", "无法获取第三方声明内容。"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK);
                            return;
                        }
                        var dlg = new TextViewerDialog(LibmpvIptvClient.Helpers.Localizer.S("UI_ThirdParty", "第三方声明"), content) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, Topmost = this.Topmost };
                        dlg.ShowDialog();
                    }
                    finally
                    {
                        BtnThirdParty.Content = old;
                        BtnThirdParty.IsEnabled = true;
                    }
                    return;
                }
                var baseDir3 = AppDomain.CurrentDomain.BaseDirectory;
                var third2 = System.IO.Path.Combine(baseDir3, "THIRD-PARTY-NOTICES.txt");
                if (!System.IO.File.Exists(third2)) third2 = System.IO.Path.Combine(baseDir3, "Third-Party-Notices.txt");
                string content3 = "";
                if (System.IO.File.Exists(third2)) content3 = System.IO.File.ReadAllText(third2);
                if (string.IsNullOrEmpty(content3))
                {
                    try
                    {
                        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                        content3 = await _httpInline.GetStringAsync("https://raw.githubusercontent.com/CGG888/SrcBox/main/THIRD-PARTY-NOTICES.txt", cts.Token);
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(content3))
                {
                    ModernMessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_FetchThirdPartyFailed", "无法获取第三方声明内容。"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK);
                    return;
                }
                var dlg3 = new TextViewerDialog(LibmpvIptvClient.Helpers.Localizer.S("UI_ThirdParty", "第三方声明"), content3) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, Topmost = this.Topmost };
                dlg3.ShowDialog();
            }
            catch { }
        }
        async System.Threading.Tasks.Task CheckUpdateInlineAsync(bool interactive)
        {
            try
            {
                _httpInline.DefaultRequestHeaders.UserAgent.ParseAdd("SrcBox/UpdateCheck");
                _httpInline.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                var latest = await FetchLatestReleaseInlineAsync("https://api.github.com/repos/CGG888/SrcBox/releases/latest");
                if (latest == null) return;
                _latestInline = latest;
                if (IsNewerInline(_latestInline.Version, _currentVersionInline))
                {
                    if (BadgeNewInline != null) BadgeNewInline.Visibility = Visibility.Visible;
                    if (interactive)
                    {
                var r = ModernMessageBox.Show(this, string.Format(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_NewVersionFound", "发现新版本 v{0}，是否查看更新？"), _latestInline.Version), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.YesNo);
                        if (r == true)
                        {
                            try
                            {
                                var dlg = new UpdateDialog(_latestInline);
                                dlg.Owner = this;
                                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                dlg.Topmost = this.Topmost;
                                dlg.ShowDialog();
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    if (interactive) ModernMessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_AlreadyLatest", "当前已是最新版本。"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK);
                }
            }
            catch { }
        }
        static bool IsNewerInline(string remote, string local)
        {
            try
            {
                Version vr = new Version(remote.Split('-')[0].TrimStart('v', 'V'));
                Version vl = new Version(local.Split('-')[0].TrimStart('v', 'V'));
                return vr > vl;
            }
            catch { return false; }
        }
        private async System.Threading.Tasks.Task<AboutWindow.ReleaseInfo?> FetchLatestReleaseInlineAsync(string apiUrl)
        {
            var urls = new System.Collections.Generic.List<string> { apiUrl };
            var cdnList = LibmpvIptvClient.AppSettings.Current.UpdateCdnMirrors ?? new System.Collections.Generic.List<string>();
            try
            {
                if (cdnList.Count == 0)
                {
                    var cdnFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "docs", "screenshots", "cdn.md");
                    if (System.IO.File.Exists(cdnFile))
                    {
                        foreach (var line in System.IO.File.ReadAllLines(cdnFile))
                        {
                            var p = line.Trim();
                            if (p.StartsWith("http")) cdnList.Add(p);
                        }
                    }
                    LibmpvIptvClient.AppSettings.Current.UpdateCdnMirrors = cdnList;
                    LibmpvIptvClient.AppSettings.Current.Save();
                }
                foreach (var p in cdnList)
                    urls.Add(p.TrimEnd('/') + "/" + apiUrl);
            }
            catch { }
            foreach (var url in urls)
            {
                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                    using var resp = await _httpInline.GetAsync(url, cts.Token);
                    if (!resp.IsSuccessStatusCode) continue;
                    var json = await resp.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var tag = root.TryGetProperty("tag_name", out var tagEl) ? (tagEl.GetString() ?? "") : "";
                    var body = root.TryGetProperty("body", out var bEl) ? (bEl.GetString() ?? "") : "";
                    string assetUrl = "";
                    string assetName = "";
                    if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array && assets.GetArrayLength() > 0)
                    {
                        System.Text.Json.JsonElement? chosen = null;
                        foreach (var a in assets.EnumerateArray())
                        {
                            if (a.TryGetProperty("name", out var n) && (n.GetString() ?? "").EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                chosen = a; break;
                            }
                        }
                        if (chosen == null) chosen = assets[0];
                        assetUrl = chosen.Value.GetProperty("browser_download_url").GetString() ?? "";
                        assetName = chosen.Value.GetProperty("name").GetString() ?? "";
                    }
                    var ver = (tag ?? root.GetProperty("name").GetString() ?? "").Trim().TrimStart('v', 'V');
                    return new AboutWindow.ReleaseInfo { Version = ver, Notes = body ?? "", DownloadUrl = assetUrl, FileName = assetName };
                }
                catch { }
            }
            return null;
        }
    }
}
