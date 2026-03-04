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
        void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var s = new PlaybackSettings();
            s.Hwdec = CbHwdec.IsChecked == true;
            if (double.TryParse(TbCacheSecs.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var cache)) s.CacheSecs = Math.Max(0, cache);
            if (int.TryParse(TbMaxBytes.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var maxb)) s.DemuxerMaxBytesMiB = Math.Max(1, maxb);
            if (int.TryParse(TbMaxBackBytes.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var backb)) s.DemuxerMaxBackBytesMiB = Math.Max(0, backb);
            if (int.TryParse(TbFccPrefetch.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var pf)) s.FccPrefetchCount = Math.Max(0, Math.Min(3, pf));
            if (int.TryParse(TbSourceTimeout.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var st)) s.SourceTimeoutSec = Math.Max(1, st);
            
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
            ApplySettingsRequested?.Invoke(s);

            ModernMessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_SettingsSaved", "设置已保存"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK);
            // DialogResult = true;
            // Close();
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
