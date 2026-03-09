using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using LibmpvIptvClient.Models;
using LibmpvIptvClient.Services;
using LibmpvIptvClient.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Text.Json;

namespace LibmpvIptvClient
{
    public partial class MainWindow : Window
    {
        private MpvInterop? _mpv;
        private ChannelService? _channelService;
        private M3UParser? _m3uParser;
        private EpgService? _epgService;
            private UserDataStore _userDataStore = new UserDataStore();
        private HttpClient _http => HttpClientService.Instance.Client;
        private List<Channel> _channels = new List<Channel>();
        private DebugWindow? _debug;
        private System.Windows.Threading.DispatcherTimer _timer = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer _epgTimer = new System.Windows.Threading.DispatcherTimer();
        private bool _seeking = false;
        private List<DateTime> _availableDates = new List<DateTime>();
        private DateTime _currentEpgDate = DateTime.Today;
        private bool _paused = false;
        private OverlayControls? _overlayWpf;
        private System.Windows.Threading.DispatcherTimer _overlayHideTimer = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer _overlayPollTimer = new System.Windows.Threading.DispatcherTimer();
        private bool _isFullscreen = false;
        private FullscreenWindow? _fs;
        private System.Windows.Forms.Panel? _fsPanel;
        private Channel? _currentChannel;
        private List<Source> _currentSources = new List<Source>();
        private string? _selectedGroup = null;
        private bool _drawerCollapsed = true; // Default collapsed
        private double _drawerWidth = 380;
        private Window? _fsDrawer;
        private Window? _fsEpg; // 全屏 EPG 抽屉
        private TopOverlay? _topOverlay;
        private double _volume = 60;
        private bool _updatingVolume = false;
        private string _playbackStatusText = "";
        private System.Windows.Media.Brush _playbackStatusBrush = System.Windows.Media.Brushes.White;
        private double _playbackSpeed = 1.0;
        private bool _firstFrameLogged = false;
        private DateTime _lastOverlayEval = DateTime.MinValue;
        private bool _lastBottomVisible = false;
        private bool _lastTopVisible = false;
        private string _currentAspect = "default";
        private System.Windows.Forms.IMessageFilter? _fsKeyFilter;
        private bool _timeshiftActive = false;
        private DateTime _timeshiftMin;
        private DateTime _timeshiftMax;
        private DateTime _timeshiftStart;
        private double _timeshiftCursorSec = 0; // from _timeshiftMin in seconds
        private System.Windows.Threading.DispatcherTimer? _timeshiftResyncTimer;
            private string _currentUrl = "";
            private DateTime _lastHistoryUpdate = DateTime.MinValue;

        void ApplyTimeshiftUi()
        {
            try
            {
                if (!_timeshiftActive) return;
                _timeshiftMax = DateTime.Now;
                var total = Math.Max(1, (_timeshiftMax - _timeshiftMin).TotalSeconds);
                _timeshiftCursorSec = Math.Max(0, Math.Min(total, _timeshiftCursorSec));
                var t = _timeshiftMin.AddSeconds(_timeshiftCursorSec);
                // Window bar
                SliderSeek.Maximum = total;
                SliderSeek.Value = _timeshiftCursorSec;
                LblElapsed.Text = t.ToString("yyyy-MM-dd HH:mm:ss");
                LblDuration.Text = _timeshiftMax.ToString("yyyy-MM-dd HH:mm:ss");
                // Overlay bar
                try
                {
                    _overlayWpf?.SetTimeshiftRange(_timeshiftMin, _timeshiftMax);
                    _overlayWpf?.SetTime(_timeshiftCursorSec, total);
                    _overlayWpf?.SetTimeshiftLabels(t, _timeshiftMax, false);
                }
                catch { }
            }
            catch { }
        }
        private class GroupItem
        {
            public string Name { get; set; } = "";
            public List<Channel> Items { get; set; } = new List<Channel>();
            public int Count => Items?.Count ?? 0;
        }
        private System.Windows.Threading.DispatcherTimer _sourceTimeoutTimer = new System.Windows.Threading.DispatcherTimer();
        private DateTime _playStartTime;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
            PreviewKeyDown += OnPreviewKeyDown;
            try
            {
                if (ListHistory != null)
                {
                    ListHistory.CommandBindings.Add(new CommandBinding(System.Windows.Input.ApplicationCommands.Delete, HistoryDeleteOne_Executed, HistoryDeleteOne_CanExecute));
                }
            }
            catch { }
            // 主视频区域双击切换全屏
            try { VideoPanel.DoubleClick += MainPanel_DoubleClick; } catch { }
            try { VideoPanel.MouseWheel += FsPanel_MouseWheel; } catch { }
            try { VideoPanel.MouseClick += VideoPanel_MouseClick; } catch { }
            this.MouseRightButtonUp += MainWindow_MouseRightButtonUp;
            _sourceTimeoutTimer.Tick += OnSourceTimeout;
            App.LanguageChanged += OnLanguageChanged;
            try
            {
                LibmpvIptvClient.Services.UploadQueueService.OnUploaded += (remoteDir) =>
                {
                    try
                    {
                        // 从远端路径解析频道键
                        var key = "unknown";
                        try
                        {
                            var parts = (remoteDir ?? "").Trim('/').Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                            key = parts.Length > 0 ? parts.Last() : "unknown";
                        }
                        catch { }
                        Dispatcher.BeginInvoke(new Action(() => { try { ScheduleRecordingsRefresh(key); } catch { } }));
                    }
                    catch { }
                };
            }
            catch { }
        }
        void OnLanguageChanged()
        {
            try
            {
                ApplyChannelFilter();
                UpdateEpgDateUI();
                UpdateEpgDisplay(); // Force refresh EPG list to update localized status
                
                // Update Playback Status Text
                if (_timeshiftActive)
                {
                    _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Timeshift", "时移");
                }
                else if (_currentPlayingProgram != null) // Catchup/Replay
                {
                     _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放");
                }
                else
                {
                     _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播");
                }
                
                if (TxtPlaybackStatus != null) TxtPlaybackStatus.Text = _playbackStatusText;
                if (TxtBottomPlaybackStatus != null) TxtBottomPlaybackStatus.Text = _playbackStatusText;
                _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
            }
            catch { }
        }
        void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == System.Windows.Input.Key.Space)
                {
                    BtnPlayPause_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
                if (e.Key == System.Windows.Input.Key.Left)
                {
                    TryArrowSeek(-1);
                    e.Handled = true;
                    return;
                }
                if (e.Key == System.Windows.Input.Key.Right)
                {
                    TryArrowSeek(1);
                    e.Handled = true;
                    return;
                }
                if (e.Key == System.Windows.Input.Key.F1)
                {
                    BtnDebug_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
            }
            catch { }
        }
        void TryArrowSeek(int dir)
        {
            if (_mpv == null) return;
            if (_currentPlayingProgram == null) return; // 仅回看模式可用
            var step = 10 * dir;
            _mpv.SeekRelative(step);
        }
        void SetVolumeInternal(double v)
        {
            if (_updatingVolume) return;
            _updatingVolume = true;
            _volume = Math.Max(0, Math.Min(100, v));
            try { if (_mpv != null) _mpv.SetVolume(_volume); } catch { }
            try { SliderVolume.Volume = _volume; } catch { }
            try { _overlayWpf?.SetVolume(_volume); } catch { }
            _updatingVolume = false;
        }
        void OnOverlayVolumeChanged(double v)
        {
            SetVolumeInternal(v);
        }
        void AdjustVolumeByWheel(int delta)
        {
            int step = 5;
            int dir = delta > 0 ? 1 : -1;
            SetVolumeInternal(_volume + dir * step);
        }
        
        // Window Control Buttons
        void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            if (sender is System.Windows.Controls.Primitives.ToggleButton tb)
            {
                tb.IsChecked = Topmost;
            }
        }
        void BtnMin_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
        void BtnMax_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
            else SystemCommands.MaximizeWindow(this);
        }
        void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        void PromptOpenFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Playlist Files (*.m3u;*.m3u8)|*.m3u;*.m3u8|All Files (*.*)|*.*" };
            if (dlg.ShowDialog() == true)
            {
                // Deselect all saved sources when opening a local file
                if (AppSettings.Current.SavedSources != null)
                {
                    foreach (var s in AppSettings.Current.SavedSources) s.IsSelected = false;
                    AppSettings.Current.Save();
                }
                try
                {
                    AppSettings.Current.LastLocalM3uPath = dlg.FileName;
                    AppSettings.Current.Save();
                }
                catch { }
                _ = LoadChannels(dlg.FileName);
            }
        }

        void PromptAddM3u()
        {
            var dlg = new AddM3uWindow { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var src = new M3uSource { Name = dlg.SourceName, Url = dlg.SourceUrl };
                if (AppSettings.Current.SavedSources == null) AppSettings.Current.SavedSources = new List<M3uSource>();
                AppSettings.Current.SavedSources.Add(src);
                AppSettings.Current.Save(); // Save to disk
                LoadM3uSource(src);
            }
        }

        void BtnAppTitle_Click(object sender, RoutedEventArgs e)
        {
            var cm = CreateAppMenu();
            BtnAppTitle.ContextMenu = cm;
            BtnAppTitle.ContextMenu.PlacementTarget = BtnAppTitle;
            BtnAppTitle.ContextMenu.IsOpen = true;
        }

        ContextMenu CreateAppMenu()
        {
            return LibmpvIptvClient.Helpers.MenuBuilder.BuildMainMenu(
                openFile: PromptOpenFile,
                openUrl: PromptOpenUrl,
                addM3uFile: PromptAddM3uFile,
                addM3uUrl: PromptAddM3u,
                editM3u: EditM3uSource,
                loadM3u: LoadM3uSource,
                openSettings: OpenSettings,
                showAbout: ShowAbout,
                exitApp: ExitApp,
                toggleFcc: (on) => 
                {
                    AppSettings.Current.FccPrefetchCount = on ? 2 : 0;
                    AppSettings.Current.Save();
                    try { _overlayWpf?.SetFcc(on); } catch { }
                },
                toggleUdp: (on) => 
                {
                    AppSettings.Current.EnableUdpOptimization = on;
                    AppSettings.Current.Save();
                    try { _overlayWpf?.SetUdp(on); } catch { }
                },
                toggleEpg: (on) => 
                {
                    CbEpg.IsChecked = on;
                    CbEpg_Click(CbEpg, new RoutedEventArgs());
                },
                toggleDrawer: (on) => SetDrawerCollapsed(!on),
                isEpgChecked: CbEpg.IsChecked == true,
                isDrawerChecked: !_drawerCollapsed
            );
        }

        void VideoPanel_MouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                Dispatcher.Invoke(() => 
                {
                    var cm = CreateAppMenu();
                    cm.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                    cm.IsOpen = true;
                });
            }
        }

        void MainWindow_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (IsInsideRecordingsArea(e.OriginalSource as System.Windows.DependencyObject))
                {
                    e.Handled = true;
                    return;
                }
                var cm = CreateAppMenu();
                cm.IsOpen = true;
                e.Handled = true;
            }
            catch
            {
                e.Handled = true;
            }
        }
        bool IsInsideRecordingsArea(System.Windows.DependencyObject? d)
        {
            try
            {
                while (d != null)
                {
                    if (d is System.Windows.FrameworkElement fe)
                    {
                        var name = fe.Name ?? "";
                        if (string.Equals(name, "ListRecordings", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(name, "ListRecordingsGrouped", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(name, "TxtRecordingsSearch", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(name, "BtnRecordingsRefresh", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    d = System.Windows.Media.VisualTreeHelper.GetParent(d);
                }
            }
            catch { }
            return false;
        }

        async void PromptOpenUrl()
        {
            var dlg = new AddM3uWindow { Owner = this, Title = LibmpvIptvClient.Helpers.Localizer.S("Menu_OpenUrl", "打开链接") };
            dlg.HideNameField(); // Hide name input for simple URL opening
            
            if (dlg.ShowDialog() == true)
            {
                var url = dlg.SourceUrl.Trim();
                if (string.IsNullOrWhiteSpace(url)) return;

                if (AppSettings.Current.SavedSources != null)
                {
                    foreach (var s in AppSettings.Current.SavedSources) s.IsSelected = false;
                    AppSettings.Current.Save();
                }

                // Directly load as stream
                LoadSingleStream(url);
            }
        }



        void LoadSingleStream(string url)
        {
            _channels.Clear();
            var ch = new Channel 
            { 
                Name = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Stream_Network", "网络流"), 
                Tag = new Source { Url = url },
                Group = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Stream_Network", "网络流"),
                Logo = "/srcbox.png"
            };
            ch.Sources = new List<Source> { (Source)ch.Tag };
            _channels.Add(ch);
            ComputeGlobalIndex();
            ApplyChannelFilter();
            
            PlayChannel(ch);
        }

        void PromptAddM3uFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog 
            { 
                Filter = "Playlist Files (*.m3u;*.m3u8)|*.m3u;*.m3u8|All Files (*.*)|*.*",
                Multiselect = true 
            };
            
            if (dlg.ShowDialog() == true)
            {
                if (AppSettings.Current.SavedSources == null) AppSettings.Current.SavedSources = new List<M3uSource>();
                
                M3uSource? lastSrc = null;
                foreach (var file in dlg.FileNames)
                {
                    var src = new M3uSource { Name = System.IO.Path.GetFileNameWithoutExtension(file), Url = file };
                    AppSettings.Current.SavedSources.Add(src);
                    lastSrc = src;
                }
                AppSettings.Current.Save();
                
                if (lastSrc != null) LoadM3uSource(lastSrc);
            }
        }

        void ShowAbout()
        {
            var owner = (_isFullscreen && _fs != null) ? (Window)_fs : this;
            var dlg = new AboutWindow { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            try { dlg.Topmost = _isFullscreen; } catch { }
            dlg.ShowDialog();
        }

        void ExitApp() => Close();

        void LoadM3uSource(M3uSource src)
        {
            if (AppSettings.Current.SavedSources != null)
            {
                foreach (var s in AppSettings.Current.SavedSources)
                    s.IsSelected = (s == src); // Match by reference for uniqueness
                AppSettings.Current.Save();
            }
            try
            {
                AppSettings.Current.LastLocalM3uPath = "";
                AppSettings.Current.Save();
            }
            catch { }
            _ = LoadChannels(src.Url);
        }

        void MainPanel_DoubleClick(object? sender, EventArgs e) => ToggleFullscreen(true);
        void VideoHost_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            AdjustVolumeByWheel(e.Delta);
            ShowOverlayWithDelay();
            e.Handled = true;
        }
        void VideoArea_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            AdjustVolumeByWheel(e.Delta);
            ShowOverlayWithDelay();
            e.Handled = true;
        }
        void BottomBar_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            AdjustVolumeByWheel(e.Delta);
            ShowOverlayWithDelay();
            e.Handled = true;
        }
        void ListChannels_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try { ListChannels.Focus(); } catch { }
        }
        void EpgLiveChip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentChannel == null) return;
                // 仅当该行节目状态为“直播”时触发播放
                if (sender is FrameworkElement fe && fe.DataContext is EpgProgram p)
                {
                    var liveLabel = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播");
                    if (string.Equals(p.Status, liveLabel, StringComparison.OrdinalIgnoreCase) || p.Status == "直播")
                    {
                        PlayChannel(_currentChannel);
                    }
                }
            }
            catch { }
        }
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TryEnableDarkTitleBar();
                try
                {
                    LibmpvIptvClient.Services.NotificationService.Instance.SetMenuCallbacks(
                        openMain: () => { try { this.Dispatcher.Invoke(() => { this.Show(); this.WindowState = WindowState.Normal; this.Activate(); }); } catch { } },
                        openSettings: () => { try { this.Dispatcher.Invoke(OpenSettings); } catch { } },
                        exitApp: () => { try { System.Windows.Application.Current.Shutdown(); } catch { } },
                        openReminderNotify: () => { try { this.Dispatcher.Invoke(LibmpvIptvClient.Helpers.ReminderWindowManager.OpenOrActivate); } catch { } },
                        openReminderAutoplay: () => { try { this.Dispatcher.Invoke(LibmpvIptvClient.Helpers.ReminderWindowManager.OpenOrActivate); } catch { } },
                        openM3uManage: () => { try { this.Dispatcher.Invoke(LibmpvIptvClient.Helpers.M3uWindowManager.OpenOrActivate); } catch { } }
                    );
                }
                catch { }
                try { LibmpvIptvClient.Services.ReminderService.Instance.Start(); } catch { }
                try
                {
                    var oldDefault = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SrcBox", "logo-cache");
                    if (!string.IsNullOrWhiteSpace(AppSettings.Current.Logo.CacheDir) &&
                        string.Equals(AppSettings.Current.Logo.CacheDir, oldDefault, StringComparison.OrdinalIgnoreCase))
                    {
                        AppSettings.Current.Logo.CacheDir = "";
                        AppSettings.Current.Save();
                        try { LibmpvIptvClient.Diagnostics.Logger.Log("[LogoCache] migrate old default path to EXE-based default"); } catch { }
                    }
                }
                catch { }
                try
                {
                    var s1 = OverlayVisibilityPolicy.ShouldShowBottom(1080, 1070, 160, 0.26, false) == true;
                    var s2 = OverlayVisibilityPolicy.ShouldShowBottom(1080, 800, 160, 0.26, true) == true;
                    var s3 = OverlayVisibilityPolicy.ShouldShowBottom(1080, 800, 160, 0.26, false) == false;
                    var s4 = OverlayVisibilityPolicy.ShouldShowTop(10, 60, false) == true;
                    var s5 = OverlayVisibilityPolicy.ShouldShowTop(100, 60, true) == true;
                    var s6 = OverlayVisibilityPolicy.ShouldShowTop(100, 60, false) == false;
                    if (!(s1 && s2 && s3 && s4 && s5 && s6))
                    {
                        System.Diagnostics.Debug.WriteLine("OverlayVisibilityPolicy tests failed");
                    }
                }
                catch { }
                _mpv = new MpvInterop();
                _mpv.Create();
                _mpv.SetSettings(AppSettings.Current);
                var hwnd = new WindowInteropHelper(this).Handle;
                var panelHwnd = VideoPanel.Handle;
                _mpv.SetWid(panelHwnd);
                _mpv.Initialize();
                try { SliderVolume.Volume = 60; } catch { }
                _volume = 60;
                _mpv.SetVolume(_volume);
                _timer.Interval = TimeSpan.FromMilliseconds(500);
                _timer.Tick += OnTick;
                _timer.Start();
                BuildOverlayWindow();
                if (DrawerColumn != null) _drawerWidth = DrawerColumn.Width.Value;
                
                // Initially hide VideoHost to show placeholder
                try { VideoHost.Visibility = Visibility.Collapsed; } catch { }
                try { _ = LoadRecordingsLocalGrouped(); } catch { }
            }
            catch (Exception ex)
            {
                try { Logger.Error("初始化失败 " + ex.ToString()); } catch { }
                System.Windows.MessageBox.Show(this, ex.Message, LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_StartupFailed", "启动失败"), MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            _m3uParser = new M3UParser();
            _epgService = new EpgService();
            var checker = new IptvCheckerClient(_m3uParser, "", "/api/export/json", "/api/export/m3u", "");
            _channelService = new ChannelService(_m3uParser, checker);
            _epgTimer.Interval = TimeSpan.FromMinutes(1);
            _epgTimer.Tick += (s, e) => UpdateEpgDisplay();
            _epgTimer.Start();
            Logger.Log("应用启动完成");
            
            // 初始化用户数据（收藏/历史）
            try { _userDataStore.Load(); } catch { }
            try { StartRecordingsWatcher(); } catch { }
            
            // Auto-load last selected source if available
            if (AppSettings.Current.AutoLoadLastSource && !string.IsNullOrWhiteSpace(AppSettings.Current.LastLocalM3uPath) && System.IO.File.Exists(AppSettings.Current.LastLocalM3uPath))
            {
                Logger.Log("自动加载上次本地 M3U: " + AppSettings.Current.LastLocalM3uPath);
                _ = LoadChannels(AppSettings.Current.LastLocalM3uPath);
            }
            else if (AppSettings.Current.SavedSources != null)
            {
                var lastSelected = AppSettings.Current.SavedSources.FirstOrDefault(s => s.IsSelected);
                if (lastSelected != null)
                {
                    Logger.Log("自动加载上次选择的源: " + lastSelected.Name);
                    LoadM3uSource(lastSelected);
                }
            }
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (AppSettings.Current.ConfirmOnClose)
                {
                    e.Cancel = true;
                    var title = LibmpvIptvClient.Helpers.Localizer.S("CloseConfirm_Title", "关闭确认");
                    var label = LibmpvIptvClient.Helpers.Localizer.S("CloseConfirm_Label", "选择操作：");
                    var yesLine = LibmpvIptvClient.Helpers.Localizer.S("CloseConfirm_LineYes", "是：退出软件");
                    var noLine = LibmpvIptvClient.Helpers.Localizer.S("CloseConfirm_LineNo", "否：最小化到系统托盘");
                    var msg = label + Environment.NewLine + Environment.NewLine + yesLine + Environment.NewLine + noLine;
                    var owner = (_isFullscreen && _fs != null) ? (Window)_fs : this;
                    var r = ModernMessageBox.Show(owner, msg, title, MessageBoxButton.YesNo);
                    if (r.HasValue && r.Value == true)
                    {
                        try { _mpv?.Dispose(); } catch { }
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                    else if (r.HasValue && r.Value == false)
                    {
                        try { if (_isFullscreen) ToggleFullscreen(false); } catch { }
                        Hide();
                        return;
                    }
                    // r == null -> user pressed X/ESC: just close the dialog and keep app visible (no action)
                    return;
                }
            }
            catch { }
            base.OnClosing(e);
        }
        void ComputeGlobalIndex()
        {
            try
            {
                var list = DistinctByNamePreserveOrder(_channels);
                var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < list.Count; i++)
                {
                    var key = list[i].Name ?? "";
                    map[key] = i + 1;
                }
                foreach (var ch in _channels)
                {
                    if (ch?.Name == null) continue;
                    if (map.TryGetValue(ch.Name, out var idx)) ch.GlobalIndex = idx;
                }
            }
            catch { }
        }
        [DllImport("dwmapi.dll", PreserveSig = true)]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        void TryEnableDarkTitleBar()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int value = 1;
                DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
                DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
            }
            catch { }
        }
        void BuildOverlayWindow()
        {
            _overlayWpf = new OverlayControls();
            _overlayWpf.Owner = this;
            _overlayWpf.PlayPause += () => BtnPlayPause_Click(this, new RoutedEventArgs());
            _overlayWpf.Stop += () => BtnStop_Click(this, new RoutedEventArgs());
            _overlayWpf.Rew += () => BtnRew_Click(this, new RoutedEventArgs());
            _overlayWpf.Fwd += () => BtnFwd_Click(this, new RoutedEventArgs());
            _overlayWpf.AspectRatioChanged += (ratio) => _mpv?.SetAspectRatio(ratio);
            _overlayWpf.SpeedSelected += (sp) => 
            {
                try
                {
                    _playbackSpeed = sp;
                    _mpv?.SetSpeed(sp);
                }
                catch { }
            };
            _overlayWpf.PreviewRequested += () =>
            {
                try
                {
                    if (_currentChannel == null) return;
                    DateTime s = DateTime.Now.AddHours(-3);
                    DateTime e2 = s.AddHours(1);
                    if (_timeshiftActive)
                    {
                        var total = Math.Max(1, (_timeshiftMax - _timeshiftMin).TotalSeconds);
                        var cursor = Math.Max(0, Math.Min(total, _timeshiftCursorSec));
                        s = _timeshiftMin.AddSeconds(cursor);
                        e2 = s.AddMinutes(10);
                    }
                    else if (_currentPlayingProgram != null)
                    {
                        s = _currentPlayingProgram.Start;
                        e2 = _currentPlayingProgram.End;
                    }
                    var baseUrl = _currentChannel.CatchupSource;
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        if (AppSettings.Current.Replay.Enabled && !string.IsNullOrEmpty(AppSettings.Current.Replay.UrlFormat))
                        {
                            var fmt = AppSettings.Current.Replay.UrlFormat;
                            if (!string.IsNullOrEmpty(fmt) && (fmt.StartsWith("?") || fmt.StartsWith("&")))
                            {
                                var live = (_currentChannel.Tag is Source s1 && !string.IsNullOrEmpty(s1.Url)) ? s1.Url
                                           : (_currentChannel.Sources != null && _currentChannel.Sources.Count > 0 ? _currentChannel.Sources[0].Url : "");
                                if (!string.IsNullOrEmpty(live))
                                {
                                    var sep = live.Contains("?") ? "&" : "?";
                                    baseUrl = live + sep + fmt.TrimStart('?', '&');
                                }
                            }
                            else
                            {
                                baseUrl = fmt.Replace("{id}", _currentChannel.Id ?? _currentChannel.Name);
                            }
                        }
                    }
                    baseUrl = baseUrl ?? "";
                    var preview = LibmpvIptvClient.Services.UrlTimeRewriter.RewriteIfEnabled(AppSettings.Current, baseUrl, s, e2, _timeshiftActive);
                    var title = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Overlay_Preview_Title", "预览拼接");
                    var msg = string.IsNullOrEmpty(baseUrl) ? preview : preview;
                    ModernMessageBox.Show(this, msg, title, MessageBoxButton.OK);
                }
                catch { }
            };
            _overlayWpf.DrawerToggled += (visible) => SetDrawerCollapsed(!visible);
            _overlayWpf.EpgToggled += (visible) => 
            { 
                CbEpg.IsChecked = visible;
                CbEpg_Click(this, new RoutedEventArgs());
            };
            _overlayWpf.SeekStart += () => _seeking = true;
            _overlayWpf.SeekEnd += (val) => 
            { 
                _seeking = false; 
                if (_mpv == null) return;
                if (_timeshiftActive && _currentChannel != null)
                {
                    var secs = Math.Max(0, val);
                    var t = _timeshiftMin.AddSeconds(secs);
                    PlayCatchupAt(_currentChannel, t);
                    _timeshiftStart = t;
                }
                else
                {
                    _mpv.SeekAbsolute(val);
                }
            };
            _overlayWpf.VolumeChanged += (val) => OnOverlayVolumeChanged(val);
            _overlayWpf.MuteChanged += (on) => { _muted = on; if (_mpv != null) _mpv.Mute(on); SetMuteIcon(); };
            // _overlayWpf.FccChanged += (v) => { try { CbFcc.IsChecked = v; } catch { } }; // Removed
            // _overlayWpf.UdpChanged += (v) => { try { CbUdpMode.IsChecked = v; } catch { } }; // Removed
            _overlayWpf.SourceMenuRequested += () => OpenSourceMenuAtOverlay();
            _overlayWpf.TimeshiftToggled += (on) => ToggleTimeshift(on);
            _overlayWpf.Topmost = true;
            try { _overlayWpf.CurrentAspect = _currentAspect; } catch { }
            
            if (_drawerCollapsed)
            {
                DrawerColumn.Width = new GridLength(0);
            }
            try 
            { 
                _overlayWpf.SetDrawerVisible(!_drawerCollapsed); 
            } 
            catch { }
            
            try
            {
                if (!string.IsNullOrEmpty(_playbackStatusText))
                {
                    _overlayWpf.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
                }
            }
            catch { }
            _overlayWpf.SetVolume(_volume);
            try { _overlayWpf.SetSpeed(1.0); _overlayWpf.SetSpeedEnabled(false); } catch { }
            try { if (BtnSpeed != null) { BtnSpeed.IsEnabled = false; LblSpeedBar.Text = "1.0x"; } } catch { }
            _overlayWpf.Show();
            PositionOverlay();
            _overlayWpf.Hide();
            _overlayHideTimer.Interval = TimeSpan.FromSeconds(2);
            { 
                _overlayWpf?.Hide(); 
                _topOverlay?.Hide();
                _overlayHideTimer.Stop(); 
            };
            _overlayPollTimer.Interval = TimeSpan.FromMilliseconds(120);
            _overlayPollTimer.Tick += (s, e) => ShowOverlayWithDelay();
            VideoPanel.MouseMove += (s, e) => ShowOverlayWithDelay();
            this.SizeChanged += (s, e) => PositionOverlay();
            this.LocationChanged += (s, e) => PositionOverlay();
            ShowOverlayWithDelay();
            
            _timer.Interval = TimeSpan.FromSeconds(0.5);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        void Timer_Tick(object? sender, EventArgs e)
        {
            if (_mpv == null) return;
            try
            {
                try
                {
                    var w1 = _mpv.GetInt("width") ?? 0;
                    var h1 = _mpv.GetInt("height") ?? 0;
                    var fps1 = _mpv.GetDouble("estimated-vf-fps") ?? _mpv.GetDouble("fps") ?? 0;
                    if (!_firstFrameLogged && w1 > 0 && h1 > 0)
                    {
                        Logger.Info($"[Playback] 首帧出现 {w1}x{h1} @{fps1:0.##} URL={_currentUrl}");
                        _firstFrameLogged = true;
                    }
                    if (!_firstFrameLogged)
                    {
                        var waited = (DateTime.Now - _playStartTime).TotalSeconds;
                        if (waited > Math.Max(3, AppSettings.Current.SourceTimeoutSec))
                        {
                            Logger.Warn($"[Playback] 起播{waited:0.#}s未见画面 time-pos={_mpv.GetTimePos() ?? 0:0.##} URL={_currentUrl}");
                            _firstFrameLogged = true;
                        }
                    }
                }
                catch { }
                if (_timeshiftActive)
                {
                    _timeshiftMax = DateTime.Now;
                    var posMpv = _mpv.GetTimePos() ?? 0;
                    var t = _timeshiftStart.AddSeconds(posMpv);
                    var current = (t - _timeshiftMin).TotalSeconds;
                    var total = (_timeshiftMax - _timeshiftMin).TotalSeconds;
                    SliderSeek.Maximum = Math.Max(1, total);
                    if (!_seeking)
                    {
                        _timeshiftCursorSec = Math.Max(0, Math.Min(SliderSeek.Maximum, current));
                        ApplyTimeshiftUi();
                    }
                    else
                    {
                        // 拖动期间仅更新 Now（右侧），不覆盖左侧
                        try { LblDuration.Text = _timeshiftMax.ToString("yyyy-MM-dd HH:mm:ss"); _overlayWpf?.SetTimeshiftRange(_timeshiftMin, _timeshiftMax); } catch { }
                    }
                }
                else
                {
                    var pos = _mpv.GetTimePos() ?? 0;
                    var dur = _mpv.GetDuration() ?? 0;
                    LblElapsed.Text = FormatTime(pos);
                    LblDuration.Text = FormatTime(dur);
                    if (!_seeking)
                    {
                        SliderSeek.Maximum = dur <= 0 ? 1 : dur;
                        SliderSeek.Value = Math.Max(0, Math.Min(SliderSeek.Maximum, pos));
                    }
                    try { _overlayWpf?.SetTime(pos, dur); } catch { }
                }
            }
            catch { }
            UpdateHistoryProgressIfNeeded();
        }
        void ToggleTimeshift(bool on)
        {
            StopRecordReplayMonitor();
            // 防止重复调用导致的状态重置（特别是切换全屏/窗口时的事件回环）
            if (_timeshiftActive == on) return;

            try { Logger.Log(on ? "[Timeshift] 开启时移" : "[Timeshift] 关闭时移"); } catch { }
            ClearRecordingPlayingIndicator();
            bool hasSource = _currentChannel != null && !string.IsNullOrEmpty(_currentChannel.CatchupSource);
            bool hasFallback = AppSettings.Current.Timeshift.Enabled && !string.IsNullOrEmpty(AppSettings.Current.Timeshift.UrlFormat);

            if (!hasSource && !hasFallback)
            {
                try { _overlayWpf?.SetTimeshift(false); } catch { }
                try { if (CbTimeshift != null) CbTimeshift.IsChecked = false; } catch { }
                return;
            }
            _timeshiftActive = on;
            if (on)
            {
                _timeshiftMax = DateTime.Now;
                var hours = Math.Max(0, AppSettings.Current.Timeshift.DurationHours);
                var minBySettings = _timeshiftMax.AddHours(-hours);
                DateTime minByEpg = minBySettings;
                try
                {
                    var programs = _epgService?.GetPrograms(_currentChannel.TvgId, _currentChannel.Name);
                    if (programs != null && programs.Count > 0)
                    {
                        var earliest = programs.Min(p => p.Start);
                        if (earliest > minByEpg) minByEpg = earliest;
                    }
                }
                catch { }
                _timeshiftMin = minByEpg;
                _timeshiftStart = _timeshiftMax;
                // 初始游标放在最右侧（Now），便于直接拖动回退
                _timeshiftCursorSec = Math.Max(0, (_timeshiftMax - _timeshiftMin).TotalSeconds);
                _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Timeshift", "时移");
                _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusTimeshiftBrush");
                try { _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush); } catch { }
                try { _overlayWpf?.SetTimeshift(true); } catch { }
                try { if (CbTimeshift != null) CbTimeshift.IsChecked = true; } catch { }
                try { TxtPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                try { TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                ApplyTimeshiftUi();
                try { _overlayWpf?.SetSpeedEnabled(true); _mpv?.SetSpeed(_playbackSpeed); _overlayWpf?.SetSpeed(_playbackSpeed); } catch { }
                try { if (BtnSpeed != null) { BtnSpeed.IsEnabled = true; LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; } } catch { }
            }
            else
            {
                _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播");
                _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusLiveBrush");
                try { _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush); } catch { }
                try { TxtPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                try { TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                try { _overlayWpf?.SetTimeshift(false); } catch { }
                try { if (CbTimeshift != null) CbTimeshift.IsChecked = false; } catch { }
                // 返回直播：重新播放当前频道的直播源
                try
                {
                    if (_currentChannel != null)
                    {
                        PlayChannel(_currentChannel);
                    }
                }
                catch { }
                try { _playbackSpeed = 1.0; _mpv?.SetSpeed(1.0); _overlayWpf?.SetSpeed(1.0); _overlayWpf?.SetSpeedEnabled(false); } catch { }
                try { if (BtnSpeed != null) { BtnSpeed.IsEnabled = false; LblSpeedBar.Text = "1.0x"; } } catch { }
            }
        }
        void CbTimeshift_Click(object sender, RoutedEventArgs e)
        {
            bool on = CbTimeshift.IsChecked == true;
            ToggleTimeshift(on);
        }
        void PlayCatchupAt(Channel ch, DateTime start)
        {
            var url = ch.CatchupSource;
            if (string.IsNullOrEmpty(url))
            {
                if (AppSettings.Current.Timeshift.Enabled && !string.IsNullOrEmpty(AppSettings.Current.Timeshift.UrlFormat))
                {
                    var fmt = AppSettings.Current.Timeshift.UrlFormat;
                    if (!string.IsNullOrEmpty(fmt) && (fmt.StartsWith("?") || fmt.StartsWith("&")))
                    {
                        var live = (ch.Tag is Source s1 && !string.IsNullOrEmpty(s1.Url)) ? s1.Url
                                   : (ch.Sources != null && ch.Sources.Count > 0 ? ch.Sources[0].Url : "");
                        if (string.IsNullOrEmpty(live)) return;
                        var sep = live.Contains("?") ? "&" : "?";
                        url = live + sep + fmt.TrimStart('?', '&');
                    }
                    else
                    {
                        url = fmt.Replace("{id}", ch.Id ?? ch.Name);
                    }
                }
                else return;
            }
            try
            {
                DateTime end = start.AddHours(1);
                try
                {
                    var programs = _epgService?.GetPrograms(ch.TvgId, ch.Name);
                    if (programs != null && programs.Count > 0)
                    {
                        var prog = programs.FirstOrDefault(p => p.Start <= start && p.End > start);
                        if (prog != null) end = prog.End;
                    }
                }
                catch { }
                url = ProcessUrlPlaceholders(url, start, end);
                try { url = LibmpvIptvClient.Services.UrlTimeRewriter.RewriteIfEnabled(AppSettings.Current, url, start, end, _timeshiftActive); } catch { }
                
                Logger.Log($"[Timeshift] 开始时移/回看 - 频道: {ch.Name}, 时间点: {start:yyyy-MM-dd HH:mm:ss}, URL: {url}");
                _mpv?.LoadFile(url);
                _currentUrl = url;
                try { PlaceholderPanel.Visibility = Visibility.Collapsed; VideoHost.Visibility = Visibility.Visible; } catch { }
                try { _firstFrameLogged = false; } catch { }
                if (_timeshiftActive)
                {
                    _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Timeshift", "时移");
                    _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusTimeshiftBrush");
                }
                else
                {
                    _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放");
                    _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusReplayBrush");
                }
                try { _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush); } catch { }
                try { TxtPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                try { TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                try { _overlayWpf?.SetSpeedEnabled(true); _mpv?.SetSpeed(_playbackSpeed); _overlayWpf?.SetSpeed(_playbackSpeed); } catch { }
                try { if (BtnSpeed != null) { BtnSpeed.IsEnabled = true; LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; } } catch { }
                
                // 写入播放记录（时移）
                try
                {
                    var key = UserDataStore.ComputeKey(ch, url);
                    _userDataStore.AddOrUpdateHistory(new HistoryItem
                    {
                        Key = key,
                        Name = ch.Name,
                        Logo = ch.Logo,
                        Group = ch.Group,
                        SourceUrl = url,
                        PlayType = "timeshift"
                    });
                    ListHistory.ItemsSource = _userDataStore.GetHistory();
                }
                catch { }
            }
            catch { }
        }
        void ListHistory_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (ListHistory.SelectedItem is HistoryItem hi)
                {
                    // 非直播类型：优先按记录URL播放，确保状态正确
                    if ((hi.PlayTypeLabel != "直播" && hi.PlayTypeLabel != LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播")) && _mpv != null && !string.IsNullOrWhiteSpace(hi.SourceUrl))
                    {
                        _mpv.LoadFile(hi.SourceUrl);
                        _currentUrl = hi.SourceUrl;
                        if (hi.PlayTypeLabel == "回放" || hi.PlayTypeLabel == LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放"))
                        {
                            _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放");
                            _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusReplayBrush");
                            try { TxtPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                            try { _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush); } catch { }
                            _timeshiftActive = false; try { CbTimeshift.IsChecked = false; } catch { }
                            // 启用倍速（历史记录回放）
                            try { _overlayWpf?.SetSpeedEnabled(true); _mpv?.SetSpeed(_playbackSpeed); _overlayWpf?.SetSpeed(_playbackSpeed); } catch { }
                            try { if (BtnSpeed != null) { BtnSpeed.IsEnabled = true; LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; } } catch { }
                        }
                        else if (hi.PlayTypeLabel == "时移" || hi.PlayTypeLabel == LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Timeshift", "时移"))
                        {
                            _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Timeshift", "时移");
                            _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusTimeshiftBrush");
                            try { TxtPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                            try { _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush); } catch { }
                            // 不强制切到时移UI，避免依赖当前频道 catchup-source；仅同步文案
                        }
                        _paused = false;
                        UpdatePlayPauseIcon();
                        return;
                    }
                    // 直播：尝试匹配频道再播放
                    {
                        var ch = _channels.FirstOrDefault(c => string.Equals(UserDataStore.ComputeKey(c), hi.Key, StringComparison.OrdinalIgnoreCase))
                                 ?? _channels.FirstOrDefault(c => string.Equals(c.Name ?? "", hi.Name ?? "", StringComparison.OrdinalIgnoreCase));
                        if (ch != null)
                        {
                            PlayChannel(ch);
                            return;
                        }
                    }
                    // 最后兜底：直接URL播放（当频道匹配不到时）
                    if (_mpv != null && !string.IsNullOrWhiteSpace(hi.SourceUrl))
                    {
                        _mpv.LoadFile(hi.SourceUrl);
                        _currentUrl = hi.SourceUrl;
                        _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播");
                        _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusLiveBrush");
                        try { TxtPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                        try { _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush); } catch { }
                        _paused = false;
                        UpdatePlayPauseIcon();
                    }
                }
            }
            catch { }
        }
        void HistoryDeleteOne_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                if (e.Parameter is HistoryItem hi)
                {
                    _userDataStore.RemoveHistory(hi.Key);
                    ListHistory.ItemsSource = _userDataStore.GetHistory();
                    e.Handled = true;
                }
            }
            catch { }
        }
        void HistoryDeleteOne_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is HistoryItem;
            e.Handled = true;
        }
        void BtnHistoryDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sel = ListHistory.SelectedItems;
                if (sel == null || sel.Count == 0) return;
                var keys = new List<string>();
                foreach (var item in sel)
                {
                    if (item is HistoryItem hi && !string.IsNullOrWhiteSpace(hi.Key)) keys.Add(hi.Key);
                }
                foreach (var k in keys) _userDataStore.RemoveHistory(k);
                ListHistory.ItemsSource = _userDataStore.GetHistory();
            }
            catch { }
        }
        void BtnHistoryClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _userDataStore.ClearHistory();
                ListHistory.ItemsSource = _userDataStore.GetHistory();
            }
            catch { }
        }
        void UpdateHistoryProgressIfNeeded()
        {
            if (_mpv == null) return;
            var now = DateTime.UtcNow;
            if ((now - _lastHistoryUpdate).TotalSeconds < 5) return;
            _lastHistoryUpdate = now;
            try
            {
                string playType = "live";
                if (_timeshiftActive) playType = "timeshift";
                else if (_currentPlayingProgram != null) playType = "catchup";
                if (_currentChannel == null && string.IsNullOrWhiteSpace(_currentUrl)) return;
                var key = _currentChannel != null ? UserDataStore.ComputeKey(_currentChannel, _currentUrl) : (_currentUrl ?? "");
                var pos = _mpv.GetTimePos() ?? 0;
                var dur = _mpv.GetDuration() ?? 0;
                var name = _currentChannel?.Name ?? LibmpvIptvClient.Helpers.ResxLocalizer.Get("App_Untitled", "未命名");
                var logo = _currentChannel?.Logo ?? "";
                var group = _currentChannel?.Group ?? "";
                var srcUrl = _currentUrl;
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(srcUrl)) return;
                _userDataStore.AddOrUpdateHistory(new HistoryItem
                {
                    Key = key,
                    Name = name,
                    Logo = logo,
                    Group = group,
                    SourceUrl = srcUrl,
                    PlayType = playType,
                    PositionSec = pos,
                    DurationSec = dur
                });
                try { Dispatcher.BeginInvoke(new Action(() => { try { ListHistory.Items.Refresh(); } catch { ListHistory.ItemsSource = _userDataStore.GetHistory(); } }), System.Windows.Threading.DispatcherPriority.Background); } catch { }
            }
            catch { }
        }

        void ResetOverlayForOwner(Window owner, bool fullscreen)
        {
            try { _overlayWpf?.Close(); } catch { }
            _overlayWpf = new OverlayControls();
            _overlayWpf.Owner = owner;
            _overlayWpf.PlayPause += () => BtnPlayPause_Click(this, new RoutedEventArgs());
            _overlayWpf.Stop += () => BtnStop_Click(this, new RoutedEventArgs());
            _overlayWpf.Rew += () => BtnRew_Click(this, new RoutedEventArgs());
            _overlayWpf.Fwd += () => BtnFwd_Click(this, new RoutedEventArgs());
            _overlayWpf.AspectRatioChanged += (ratio) => _mpv?.SetAspectRatio(ratio);
            _overlayWpf.SpeedSelected += (sp) =>
            {
                try
                {
                    _playbackSpeed = sp;
                    _mpv?.SetSpeed(sp);
                }
                catch { }
            };
            _overlayWpf.DrawerToggled += (visible) => SetDrawerCollapsed(!visible);
            _overlayWpf.EpgToggled += (visible) => 
            { 
                CbEpg.IsChecked = visible;
                CbEpg_Click(this, new RoutedEventArgs());
            };
            _overlayWpf.SeekStart += () => _seeking = true;
            _overlayWpf.SeekEnd += (val) =>
            {
                _seeking = false;
                if (_mpv == null) return;
                if (_timeshiftActive && _currentChannel != null)
                {
                    var secs = Math.Max(0, val);
                    _timeshiftCursorSec = secs; // 保存游标，供窗口/全屏切换同步
                    var t = _timeshiftMin.AddSeconds(secs);
                    try { Logger.Log($"[Timeshift] 拖动定位到 {t:yyyy-MM-dd HH:mm:ss}"); } catch { }
                    PlayCatchupAt(_currentChannel, t);
                    _timeshiftStart = t;
                }
                else
                {
                    _mpv.SeekAbsolute(val);
                }
            };
            _overlayWpf.VolumeChanged += (val) => OnOverlayVolumeChanged(val);
            _overlayWpf.MuteChanged += (on) => { _muted = on; if (_mpv != null) _mpv.Mute(on); SetMuteIcon(); };
            // _overlayWpf.FccChanged += (v) => { try { CbFcc.IsChecked = v; } catch { } }; // Removed
            // _overlayWpf.UdpChanged += (v) => { try { CbUdpMode.IsChecked = v; } catch { } }; // Removed
            _overlayWpf.SourceMenuRequested += () => OpenSourceMenuAtOverlay();
            try { _overlayWpf.TimeshiftToggled += (on) => ToggleTimeshift(on); } catch { }
            _overlayWpf.Topmost = true;
            try { _overlayWpf.SetDrawerVisible(!_drawerCollapsed); } catch { }
            try { _overlayWpf.SetEpgVisible(CbEpg.IsChecked == true); } catch { }
            try { _overlayWpf.SetPaused(_paused); } catch { }
            try { _overlayWpf.CurrentAspect = _currentAspect; } catch { }
            _overlayWpf.SetVolume(_volume);
            try { _overlayWpf.SetSpeed(_playbackSpeed); _overlayWpf.SetSpeedEnabled(_timeshiftActive || (_currentPlayingProgram != null)); } catch { }
            try { if (BtnSpeed != null) { BtnSpeed.IsEnabled = _timeshiftActive || (_currentPlayingProgram != null); LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; } } catch { }
            // 初始化时移状态到新创建的悬浮条（窗口/全屏一致）
            try
            {
                _overlayWpf.SetTimeshift(_timeshiftActive);
                if (_timeshiftActive)
                {
                    if (_timeshiftMax == default) _timeshiftMax = DateTime.Now;
                    if (_timeshiftMin == default)
                    {
                        var hours = Math.Max(0, AppSettings.Current.TimeshiftHours);
                        _timeshiftMin = _timeshiftMax.AddHours(-hours);
                    }
                    var total = Math.Max(1, (_timeshiftMax - _timeshiftMin).TotalSeconds);
                    ApplyTimeshiftUi();
                    // 如果当前是回到窗口模式（fullscreen == false），同时把底部进度条与标签立即同步一次
                    if (!fullscreen)
                    {
                        ApplyTimeshiftUi();
                    }
                }
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(_playbackStatusText))
                {
                    _overlayWpf.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
                }
            }
            catch { }
            _overlayWpf.Show();
            PositionOverlay();
            _overlayWpf.Hide();
            // 刚创建悬浮条后，再异步同步一次时移 UI（避免创建过程中的布局延迟导致的不同步）
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_timeshiftActive) SyncTimeshiftUi();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch { }
            // 再设定一次轻微延迟的二次同步，确保 mpv.pos 在切换后稳定
            try
            {
                _timeshiftResyncTimer?.Stop();
                _timeshiftResyncTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                _timeshiftResyncTimer.Tick += (s, e2) =>
                {
                    _timeshiftResyncTimer?.Stop();
                    _timeshiftResyncTimer = null;
                    if (_timeshiftActive) SyncTimeshiftUi();
                };
                _timeshiftResyncTimer.Start();
            }
            catch { }
        }
        void BtnSpeed_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            double[] speeds = new double[] { 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 3.0, 5.0 };
            foreach (var sp in speeds)
            {
                var mi = new MenuItem();
                mi.Header = $"{sp:0.##}x";
                mi.IsCheckable = true;
                mi.IsChecked = Math.Abs(_playbackSpeed - sp) < 0.001;
                mi.Click += (s, ev) =>
                {
                    _playbackSpeed = sp;
                    try { _mpv?.SetSpeed(sp); } catch { }
                    try { _overlayWpf?.SetSpeed(sp); } catch { }
                    try { LblSpeedBar.Text = $"{sp:0.##}x"; } catch { }
                };
                menu.Items.Add(mi);
            }
            try
            {
                var cmStyle = (Style)FindResource(typeof(ContextMenu));
                if (cmStyle != null) menu.Style = cmStyle;
                var miStyle = (Style)FindResource(typeof(MenuItem));
                foreach (var obj in menu.Items)
                {
                    if (obj is MenuItem mi && miStyle != null) mi.Style = miStyle;
                }
            }
            catch { }
            BtnSpeed.ContextMenu = menu;
            BtnSpeed.ContextMenu.PlacementTarget = BtnSpeed;
            BtnSpeed.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Custom;
            BtnSpeed.ContextMenu.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
                new[] { new System.Windows.Controls.Primitives.CustomPopupPlacement(new System.Windows.Point((targetSize.Width - popupSize.Width) / 2, -popupSize.Height - 30), System.Windows.Controls.Primitives.PopupPrimaryAxis.Horizontal) };
            BtnSpeed.ContextMenu.IsOpen = true;
        }
        void SyncTimeshiftUi()
        {
            if (_mpv == null || !_timeshiftActive) return;
            ApplyTimeshiftUi();
        }
        bool _muted = false;
        void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            _muted = !_muted;
            if (_mpv != null) _mpv.Mute(_muted);
            SetMuteIcon();
        }
        void UpdatePlayPauseIcon()
        {
            try
            {
                IconPlayPause.Symbol = _paused ? ModernWpf.Controls.Symbol.Play : ModernWpf.Controls.Symbol.Pause;
            }
            catch { }
            try
            {
                _overlayWpf?.SetPaused(_paused);
            }
            catch { }
        }
        void SetMuteIcon()
        {
            try
            {
                IconMute.Symbol = _muted ? ModernWpf.Controls.Symbol.Mute : ModernWpf.Controls.Symbol.Volume;
            }
            catch { }
        }
        void EditM3uSource(M3uSource source)
        {
            var owner = (_isFullscreen && _fs != null) ? (Window)_fs : this;
            var dlg = new EditM3uWindow(source.Name, source.Url) { Owner = owner };
            try { dlg.Topmost = _isFullscreen; } catch { }
            
            if (dlg.ShowDialog() == true)
            {
                if (dlg.IsDeleteRequested)
                {
                    if (AppSettings.Current.SavedSources != null)
                    {
                        // Use reference removal if possible, or by name/url
                        var target = AppSettings.Current.SavedSources.FirstOrDefault(s => s.Name == source.Name && s.Url == source.Url);
                        if (target != null)
                        {
                            AppSettings.Current.SavedSources.Remove(target);
                            AppSettings.Current.Save();
                        }
                    }
                }
                else
                {
                    source.Name = dlg.SourceName;
                    source.Url = dlg.SourceUrl;
                    AppSettings.Current.Save();
                }
            }
        }

        void OpenSettings()
        {
            var owner = (_isFullscreen && _fs != null) ? (Window)_fs : this;
            // If already open, bring it to front
            try
            {
                foreach (Window w in System.Windows.Application.Current.Windows)
                {
                    if (w is SettingsWindow existing)
                    {
                        try { existing.Owner = owner; } catch { }
                        try { existing.Activate(); existing.Topmost = existing.Topmost; } catch { }
                        return;
                    }
                }
            }
            catch { }
            var dlg = new SettingsWindow(AppSettings.Current) { Owner = owner };
            dlg.DebugRequested += () => BtnDebug_Click(this, new RoutedEventArgs());
            try { dlg.Topmost = _isFullscreen; } catch { }
            
            dlg.ApplySettingsRequested += (settings) =>
            {
                // 记录当前播放位置与URL，用于样式/主题变更后的恢复（尤其是回放/时移）
                double? resumePos = null;
                string? resumeUrl = null;
                bool wasTimeshift = _timeshiftActive;
                bool wasReplay = _currentPlayingProgram != null;
                var old = AppSettings.Current;
                bool mpvWillChange =
                    old.Hwdec != settings.Hwdec
                    || old.CacheSecs != settings.CacheSecs
                    || old.DemuxerMaxBytesMiB != settings.DemuxerMaxBytesMiB
                    || old.DemuxerMaxBackBytesMiB != settings.DemuxerMaxBackBytesMiB
                    || old.EnableProtocolAdaptive != settings.EnableProtocolAdaptive
                    || old.HlsStartAtLiveEdge != settings.HlsStartAtLiveEdge
                    || old.HlsReadaheadSecs != settings.HlsReadaheadSecs
                    || (old.Alang ?? "") != (settings.Alang ?? "")
                    || (old.Slang ?? "") != (settings.Slang ?? "")
                    || old.MpvNetworkTimeoutSec != settings.MpvNetworkTimeoutSec;
                try
                {
                    if (_mpv != null)
                    {
                        resumePos = _mpv.GetTimePos();
                        resumeUrl = _currentUrl;
                    }
                }
                catch { }
                AppSettings.Current.Hwdec = settings.Hwdec;
                AppSettings.Current.CacheSecs = settings.CacheSecs;
                AppSettings.Current.DemuxerMaxBytesMiB = settings.DemuxerMaxBytesMiB;
                AppSettings.Current.DemuxerMaxBackBytesMiB = settings.DemuxerMaxBackBytesMiB;
                AppSettings.Current.FccPrefetchCount = settings.FccPrefetchCount;
                AppSettings.Current.SourceTimeoutSec = settings.SourceTimeoutSec;
                AppSettings.Current.CustomEpgUrl = settings.CustomEpgUrl;
                AppSettings.Current.CustomLogoUrl = settings.CustomLogoUrl;
                AppSettings.Current.TimeshiftHours = settings.TimeshiftHours;
                AppSettings.Current.UpdateCdnMirrors = settings.UpdateCdnMirrors;
                // 新增：协议自适应 / HLS 专项 / 语言偏好 / mpv 网络超时
                AppSettings.Current.EnableProtocolAdaptive = settings.EnableProtocolAdaptive;
                AppSettings.Current.HlsStartAtLiveEdge = settings.HlsStartAtLiveEdge;
                AppSettings.Current.HlsReadaheadSecs = settings.HlsReadaheadSecs;
                AppSettings.Current.Alang = settings.Alang ?? "";
                AppSettings.Current.Slang = settings.Slang ?? "";
                AppSettings.Current.MpvNetworkTimeoutSec = settings.MpvNetworkTimeoutSec;
                // 回放/时移模板持久化
                try
                {
                    if (settings.Replay != null)
                    {
                        AppSettings.Current.Replay.Enabled = settings.Replay.Enabled;
                        AppSettings.Current.Replay.UrlFormat = settings.Replay.UrlFormat ?? "";
                        AppSettings.Current.Replay.DurationHours = settings.Replay.DurationHours;
                    }
                    if (settings.Timeshift != null)
                    {
                        AppSettings.Current.Timeshift.Enabled = settings.Timeshift.Enabled;
                        AppSettings.Current.Timeshift.UrlFormat = settings.Timeshift.UrlFormat ?? "";
                        AppSettings.Current.Timeshift.DurationHours = settings.Timeshift.DurationHours;
                    }
                    if (settings.Logo != null)
                    {
                        AppSettings.Current.Logo.Enabled = settings.Logo.Enabled;
                        AppSettings.Current.Logo.Url = settings.Logo.Url ?? "";
                        AppSettings.Current.Logo.EnableCache = settings.Logo.EnableCache;
                        AppSettings.Current.Logo.CacheDir = settings.Logo.CacheDir ?? "";
                        AppSettings.Current.Logo.CacheTtlHours = settings.Logo.CacheTtlHours;
                        AppSettings.Current.Logo.CacheMaxMiB = settings.Logo.CacheMaxMiB;
                    }
                }
                catch { }
                // 界面设置
                AppSettings.Current.Language = settings.Language;
                AppSettings.Current.ThemeMode = settings.ThemeMode;
                try
                {
                    if (settings.WebDav != null)
                    {
                        AppSettings.Current.WebDav.Enabled = settings.WebDav.Enabled;
                        AppSettings.Current.WebDav.BaseUrl = settings.WebDav.BaseUrl ?? "";
                        AppSettings.Current.WebDav.Username = settings.WebDav.Username ?? "";
                        AppSettings.Current.WebDav.TokenOrPassword = settings.WebDav.TokenOrPassword ?? "";
                        AppSettings.Current.WebDav.EncryptedToken = settings.WebDav.EncryptedToken ?? "";
                        AppSettings.Current.WebDav.AllowSelfSignedCert = settings.WebDav.AllowSelfSignedCert;
                        AppSettings.Current.WebDav.RootPath = settings.WebDav.RootPath ?? "/srcbox/";
                        AppSettings.Current.WebDav.RecordingsPath = settings.WebDav.RecordingsPath ?? "/srcbox/recordings/";
                        AppSettings.Current.WebDav.UserDataPath = settings.WebDav.UserDataPath ?? "/srcbox/user-data/";
                    }
                }
                catch { }
                // 时间覆盖持久化
                try
                {
                    if (settings.TimeOverride != null)
                    {
                        if (AppSettings.Current.TimeOverride == null)
                            AppSettings.Current.TimeOverride = new TimeOverrideConfig();
                        AppSettings.Current.TimeOverride.Enabled = settings.TimeOverride.Enabled;
                        AppSettings.Current.TimeOverride.Mode = settings.TimeOverride.Mode ?? "time_only";
                        AppSettings.Current.TimeOverride.Layout = settings.TimeOverride.Layout ?? "start_end";
                        AppSettings.Current.TimeOverride.Encoding = settings.TimeOverride.Encoding ?? "local";
                        AppSettings.Current.TimeOverride.StartKey = settings.TimeOverride.StartKey ?? "start";
                        AppSettings.Current.TimeOverride.EndKey = settings.TimeOverride.EndKey ?? "end";
                        AppSettings.Current.TimeOverride.DurationKey = settings.TimeOverride.DurationKey ?? "duration";
                        AppSettings.Current.TimeOverride.PlayseekKey = settings.TimeOverride.PlayseekKey ?? "playseek";
                        AppSettings.Current.TimeOverride.UrlEncode = settings.TimeOverride.UrlEncode;
                    }
                }
                catch { }
                try
                {
                    Logger.Debug($"[Settings] apply hwdec={settings.Hwdec} cache={settings.CacheSecs} max={settings.DemuxerMaxBytesMiB} back={settings.DemuxerMaxBackBytesMiB} fcc={settings.FccPrefetchCount} src_to={settings.SourceTimeoutSec} adaptive={settings.EnableProtocolAdaptive} hls_live={settings.HlsStartAtLiveEdge} hls_ra={settings.HlsReadaheadSecs} alang={settings.Alang} slang={settings.Slang} mpv_to={settings.MpvNetworkTimeoutSec}");
                }
                catch { }
                AppSettings.Current.Save(); // Save to disk
                if (_mpv != null && mpvWillChange)
                {
                    _mpv.SetSettings(AppSettings.Current);
                    _mpv.Initialize();
                }
                try
                {
                    App.ApplyLanguage(AppSettings.Current.Language);
                    App.ApplyTheme(AppSettings.Current.ThemeMode);
                }
                catch { }
                // 样式/主题应用后，如当前为回放或时移，恢复到变更前的播放位置
                try
                {
                    if (_mpv != null && mpvWillChange && !string.IsNullOrWhiteSpace(resumeUrl) && (wasTimeshift || wasReplay))
                    {
                        _mpv.LoadFile(resumeUrl);
                        var dt = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                        dt.Tick += (s2, e2) =>
                        {
                            dt.Stop();
                            try
                            {
                                if (resumePos.HasValue) _mpv.SeekAbsolute(Math.Max(0, resumePos.Value));
                            }
                            catch { }
                        };
                        dt.Start();
                        // 同步倍速与状态文案
                        try
                        {
                            if (wasTimeshift)
                            {
                                _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Timeshift", "时移");
                                _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusTimeshiftBrush");
                            }
                            else
                            {
                                _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放");
                                _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusReplayBrush");
                            }
                            if (TxtPlaybackStatus != null) { TxtPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush; }
                            if (TxtBottomPlaybackStatus != null) { TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush; }
                            _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
                            _overlayWpf?.SetSpeedEnabled(true);
                            _mpv.SetSpeed(_playbackSpeed);
                            _overlayWpf?.SetSpeed(_playbackSpeed);
                            if (BtnSpeed != null) { BtnSpeed.IsEnabled = true; LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; }
                        }
                        catch { }
                    }
                }
                catch { }
            };

            dlg.Show();
        }
        void SetDrawerCollapsed(bool collapsed)
        {
            if (_drawerCollapsed == collapsed) return;
            _drawerCollapsed = collapsed;
            if (collapsed)
            {
                _drawerWidth = DrawerColumn.Width.Value;
                DrawerColumn.Width = new GridLength(0);
                if (_fsDrawer != null) _fsDrawer.Visibility = Visibility.Collapsed;
            }
            else
            {
                DrawerColumn.Width = new GridLength(_drawerWidth > 0 ? _drawerWidth : 380);
                if (_fsDrawer != null) _fsDrawer.Visibility = Visibility.Visible;
                if (_isFullscreen && _fsDrawer == null && !_drawerCollapsed) ShowFullscreenDrawer();
            }
            try { CbDrawer.IsChecked = !collapsed; } catch { }
            try { _overlayWpf?.SetDrawerVisible(!collapsed); } catch { }
        }
        void BtnDrawerCollapse_Click(object sender, RoutedEventArgs e)
        {
            try { SetDrawerCollapsed(true); } catch { }
        }
        void PositionOverlay()
        {
            if (_overlayWpf == null) return;
            var rect = (_isFullscreen && _fsPanel != null)
                ? _fsPanel.RectangleToScreen(_fsPanel.ClientRectangle)
                : VideoPanel.RectangleToScreen(VideoPanel.ClientRectangle);
            var source = PresentationSource.FromVisual(_isFullscreen && _fs != null ? (System.Windows.Media.Visual)_fs : (System.Windows.Media.Visual)this);
            if (source != null)
            {
                var m = source.CompositionTarget.TransformFromDevice;
                var pt1 = m.Transform(new System.Windows.Point(rect.Left, rect.Top));
                var pt2 = m.Transform(new System.Windows.Point(rect.Right, rect.Bottom));
                _overlayWpf.Left = pt1.X;
                _overlayWpf.Top = pt2.Y - _overlayWpf.Height;
                _overlayWpf.Width = Math.Max(320, pt2.X - pt1.X);
                return;
            }
            // Fallback（未取到 DPI 转换）
            _overlayWpf.Left = rect.Left;
            _overlayWpf.Top = rect.Bottom - _overlayWpf.Height;
            _overlayWpf.Width = Math.Max(320, rect.Width);
        }
        
        void PositionTopOverlay()
        {
            if (_topOverlay == null || !_isFullscreen) return;
            if (_fsPanel != null)
            {
                var rect = _fsPanel.RectangleToScreen(_fsPanel.ClientRectangle);
                var source = PresentationSource.FromVisual(_fs);
                if (source != null)
                {
                    var m = source.CompositionTarget.TransformFromDevice;
                    var pt1 = m.Transform(new System.Windows.Point(rect.Left, rect.Top));
                    var pt2 = m.Transform(new System.Windows.Point(rect.Right, rect.Bottom));
                    _topOverlay.Left = pt1.X;
                    _topOverlay.Top = pt1.Y;
                    _topOverlay.Width = Math.Max(320, pt2.X - pt1.X);
                }
                else
                {
                    _topOverlay.Left = rect.Left;
                    _topOverlay.Top = rect.Top;
                    _topOverlay.Width = rect.Width;
                }
            }
        }

        void ShowOverlayWithDelay()
        {
            if (!_isFullscreen || _fs == null || !_fs.IsLoaded)
            {
                // Windowed Mode Logic
                if (VideoHost.IsMouseOver || (BottomBar.IsMouseOver && BottomBar.Visibility == Visibility.Visible))
                {
                    _overlayWpf?.Hide();
                    _topOverlay?.Hide();
                }
                return;
            }
            
            // 监控并强制全屏窗口置顶（防止被新打开的最大化窗口覆盖）
            try
            {
                if (_fs != null && !_fs.Topmost)
                {
                    _fs.Topmost = true;
                }
            }
            catch { }

            // 确保 EPG 和 Drawer 在全屏窗口之上
            try
            {
                if (_fsEpg != null && _fsEpg.IsVisible && !_fsEpg.Topmost) _fsEpg.Topmost = true;
                if (_fsDrawer != null && _fsDrawer.IsVisible && !_fsDrawer.Topmost) _fsDrawer.Topmost = true;
            }
            catch { }
            
            // Fullscreen Logic
            PositionOverlay();
            PositionTopOverlay();

            if (GetCursorPos(out POINT p))
            {
                // Convert to logical pixels relative to _fs
                System.Windows.Point relPoint;
                try
                {
                    relPoint = _fs.PointFromScreen(new System.Windows.Point(p.X, p.Y));
                }
                catch
                {
                    relPoint = new System.Windows.Point(p.X - _fs.Left, p.Y - _fs.Top);
                }

                var relX = relPoint.X;
                var relY = relPoint.Y;
                var screenW = _fs.ActualWidth;
                var screenH = _fs.ActualHeight;

                if (screenW <= 0 || screenH <= 0) return;

                // 1. Bottom Zone (Overlay Controls) - dynamic sensitivity (min 160px or 26% height)
                double bottomZone = Math.Max(160, screenH * 0.26);
                bool inBottomZone = relY > screenH - bottomZone;
                bool keepBottomForMenu = _overlayWpf != null && _overlayWpf.IsAnyMenuOpen;
                if (inBottomZone) 
                {
                    if (_overlayWpf != null && !_overlayWpf.IsVisible)
                    {
                        _overlayWpf.Show();
                        BringOverlayToTop();
                        _overlayHideTimer.Stop();
                        _overlayHideTimer.Start();
                    }
                    else if (_overlayWpf != null && _overlayWpf.IsVisible)
                    {
                        BringOverlayToTop();
                        _overlayHideTimer.Stop();
                        _overlayHideTimer.Start();
                    }
                    _lastBottomVisible = true;
                    _lastOverlayEval = DateTime.UtcNow;
                }
                else
                {
                    var now = DateTime.UtcNow;
                    if (_overlayWpf != null && _overlayWpf.IsVisible && !keepBottomForMenu)
                    {
                        if ((now - _lastOverlayEval).TotalMilliseconds > 60 || _lastBottomVisible)
                        {
                            _overlayWpf.Hide();
                            
                            // 修复：当底部控制栏隐藏时，强制刷新主窗口的置顶状态，防止层级丢失
                            try
                            {
                                if (_fs != null)
                                {
                                    _fs.Topmost = true;
                                    var h = new System.Windows.Interop.WindowInteropHelper(_fs).Handle;
                                    SetWindowPos(h, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
                                }
                            }
                            catch { }

                            _lastBottomVisible = false;
                            _lastOverlayEval = now;
                        }
                    }
                }

                // 2. Top Zone (Top Overlay)
                bool inTopZone = relY < 60;
                bool keepTopForMenu = _topOverlay != null && _topOverlay.IsMenuOpen;
                if (inTopZone || keepTopForMenu)
                {
                    if (_topOverlay != null && !_topOverlay.IsVisible)
                    {
                        _topOverlay.Show();
                        _topOverlay.Topmost = true;
                    }
                    if (_topOverlay != null && _topOverlay.IsVisible)
                    {
                        _overlayHideTimer.Stop();
                        _overlayHideTimer.Start();
                    }
                    _lastTopVisible = true;
                }
                else
                {
                    if (_topOverlay != null && _topOverlay.IsVisible && !keepTopForMenu)
                    {
                        var now = DateTime.UtcNow;
                        if ((now - _lastOverlayEval).TotalMilliseconds > 60 || _lastTopVisible)
                        {
                            _topOverlay.Hide();
                            _lastTopVisible = false;
                            _lastOverlayEval = now;
                        }
                    }
                }

                // 3. EPG (Left)
                bool epgEnabled = CbEpg.IsChecked == true;
                if (epgEnabled)
                {
                    if (_fsEpg == null) ShowFullscreenEpg();
                    
                    if (_fsEpg != null)
                    {
                        bool isVisible = _fsEpg.Visibility == Visibility.Visible;
                        bool inZone = relX <= 320; 
                        bool onEdge = relX <= 20;
                        
                        if (!isVisible && onEdge)
                        {
                            if (_fsEpg != null && _fs != null)
                            {
                                _fsEpg.Show();
                                _fsEpg.Visibility = Visibility.Visible;
                                // Fix Z-Order: Ensure FS is topmost first, then EPG
                                _fs.Topmost = true;
                                _fsEpg.Topmost = true;
                                _fsEpg.Activate();
                                
                                _fsEpg.Left = _fs.Left;
                                _fsEpg.Top = _fs.Top;
                                _fsEpg.Height = _fs.ActualHeight;
                            }
                        }
                        else if (isVisible && !inZone)
                        {
                            if (_fsEpg != null)
                            {
                                _fsEpg.Hide();
                                _fsEpg.Visibility = Visibility.Collapsed;
                                // Restore FS topmost state based on user preference
                                if (_fs != null) _fs.Topmost = Topmost;
                            }
                        }
                    }
                }
                else if (_fsEpg != null) CloseFullscreenEpg();

                // 4. Drawer (Right)
                if (!_drawerCollapsed && _fs != null) 
                {
                    if (_fsDrawer == null) ShowFullscreenDrawer(); 
                    
                    if (_fsDrawer != null)
                    {
                        bool isVisible = _fsDrawer.Visibility == Visibility.Visible;
                        double w = _drawerWidth > 0 ? _drawerWidth : 380;
                        bool inZone = relX >= screenW - w;
                        bool onEdge = relX >= screenW - 20;

                        if (!isVisible && onEdge)
                        {
                            if (_fsDrawer != null && _fs != null)
                            {
                                _fsDrawer.Show();
                                _fsDrawer.Visibility = Visibility.Visible;
                                // Fix Z-Order: Ensure FS is topmost first, then Drawer
                                _fs.Topmost = true;
                                _fsDrawer.Topmost = true;
                                _fsDrawer.Activate();
                                
                                _fsDrawer.Left = _fs.Left + _fs.ActualWidth - w;
                                _fsDrawer.Top = _fs.Top;
                                _fsDrawer.Height = _fs.ActualHeight;
                            }
                        }
                        else if (isVisible && !inZone)
                        {
                            if (_fsDrawer != null)
                            {
                                _fsDrawer.Hide();
                                _fsDrawer.Visibility = Visibility.Collapsed;
                                // Restore FS topmost state based on user preference
                                if (_fs != null) _fs.Topmost = Topmost;
                            }
                        }
                    }
                }
                else if (_fsDrawer != null) CloseFullscreenDrawer();
            }
        }
        void OnClosed(object? sender, EventArgs e)
        {
            try
            {
                if (_overlayWpf != null) 
                {
                    _overlayWpf.Close();
                    _overlayWpf = null;
                }
                if (_fs != null) 
                {
                    _fs.Close();
                    _fs = null;
                }
                if (_mpv != null)
                {
                    _mpv.Dispose();
                    _mpv = null;
                }
                App.LanguageChanged -= OnLanguageChanged;
            }
            catch { }
        }
        void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_mpv == null) return;
            var url = "https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8";
            _mpv.LoadFile(url);
        }
        // Duplicate BtnStop_Click removed from here

        async System.Threading.Tasks.Task LoadChannels(string m3uUrl, bool silent = false)
        {
            if (_channelService == null) return;
            try
            {
                if (string.IsNullOrWhiteSpace(m3uUrl)) return;

                // Selection logic moved to LoadM3uSource / PromptOpenFile to avoid duplicates

                Logger.Log("加载频道 " + m3uUrl);
                _channels = await _channelService.LoadChannelsAsync(m3uUrl, true);
                ComputeGlobalIndex();
                ApplyChannelFilter();
                if (_channels.Count == 0 && !silent)
                {
                System.Windows.MessageBox.Show(this, LibmpvIptvClient.Helpers.ResxLocalizer.Get("LoadResult_NoChannels", "未从来源解析到任何频道：\n1) 检查 URL 或本地路径是否可访问\n2) 若为后端导出，确认参数有效\n3) 若为压缩/特殊编码，已自动尝试解码"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Common_Tips", "提示"), MessageBoxButton.OK, MessageBoxImage.Information);
                    Logger.Log("未解析到频道");
                }
                else Logger.Log("解析频道数量 " + _channels.Count);
                
                // 优先使用自定义 EPG，否则尝试从 M3U 获取
                var epgUrl = AppSettings.Current.CustomEpgUrl;
                if (string.IsNullOrWhiteSpace(epgUrl) && _m3uParser != null)
                {
                    epgUrl = _m3uParser.TvgUrl;
                }

                // DNS 预解析（后台，不阻塞 UI）：预热前若干频道的域名解析
                try
                {
                    LibmpvIptvClient.Services.DnsPrefetcher.PrefetchForChannels(_channels, maxHosts: 60);
                }
                catch { }
                try { LibmpvIptvClient.Services.ConnectionPreheater.PreheatForChannels(_channels, maxHosts: 30); }
                catch { }

                if (!string.IsNullOrEmpty(epgUrl) && _epgService != null)
                {
                    Logger.Log("正在加载 EPG: " + epgUrl);
                    // 异步加载 EPG，不阻塞 UI
                    _ = Task.Run(async () => 
                    {
                        try 
                        {
                            await _epgService.LoadEpgAsync(epgUrl);
                            Dispatcher.Invoke(() => UpdateEpgDisplay());
                            // EPG 加载完后，刷新录播分组以匹配节目标题
                            Dispatcher.Invoke(async () => { try { await LoadRecordingsLocalGrouped(); } catch { } });
                        }
                        catch (Exception ex) { Logger.Log("EPG 加载失败: " + ex.Message); }
                    });
                }

                try { UpdateGroups(); } catch { }
                
                // 应用收藏状态
                try
                {
                    var favs = _userDataStore.GetFavorites();
                    foreach (var ch in _channels)
                    {
                        if (ch == null) continue;
                        var key = UserDataStore.ComputeKey(ch);
                        ch.Favorite = favs.Any(f => string.Equals(f, key, StringComparison.OrdinalIgnoreCase));
                    }
                    UpdateFavorites();
                }
                catch { }
                
                // 加载播放记录到列表
                try
                {
                    ListHistory.ItemsSource = _userDataStore.GetHistory();
                }
                catch { }

                // 录播：填充频道下拉，默认选第一个；加载本地分组以显示录播目录
                try { _ = LoadRecordingsLocalGrouped(); } catch { }
                try { await LoadRecordingsLocalGrouped(); } catch { }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, string.Format(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_LoadChannelsFailed", "加载频道失败：\n{0}"), ex.Message), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Err_UnhandledTitle", "错误"), MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error("加载频道失败 " + ex.Message);
            }
            finally
            {
                // BtnLoad.IsEnabled = true; // Removed
            }
        }
        void TxtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyChannelFilter();
        }
        void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            try { TxtSearch.Text = ""; } catch { }
        }
        void ApplyChannelFilter()
        {
            var key = "";
            try { key = (TxtSearch.Text ?? "").Trim(); } catch { }
            IEnumerable<Channel> baseList = _channels;
            if (!string.IsNullOrEmpty(key))
            {
                var q = key.ToLowerInvariant();
                baseList = _channels.FindAll(c => c?.Name != null && c.Name.ToLowerInvariant().Contains(q));
            }
            if (!string.IsNullOrEmpty(_selectedGroup))
            {
                baseList = System.Linq.Enumerable.Where(baseList, c => string.Equals(c.Group, _selectedGroup, StringComparison.OrdinalIgnoreCase));
            }
            var distinct = DistinctByName(baseList);
            // “所有频道”保持全局序号（GlobalIndex），不重新编号
            ListChannels.ItemsSource = distinct;
            try 
            { 
                TxtCount.Text = string.IsNullOrEmpty(key) 
                    ? string.Format(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_CountAll", "共 {0} 个频道"), _channels.Count) 
                    : string.Format(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_FilteredCount", "筛选到 {0} / {1}"), distinct.Count, _channels.Count); 
            } 
            catch { }
            UpdateGroups();
        }
        List<Channel> DistinctByNamePreserveOrder(IEnumerable<Channel> list)
        {
            var map = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<Channel>();
            foreach (var c in list)
            {
                if (c == null || c.Name == null) continue;
                if (map.Add(c.Name)) result.Add(c);
            }
            return result;
        }
        List<Channel> DistinctByName(IEnumerable<Channel> list)
        {
            var map = new Dictionary<string, Channel>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in list)
            {
                if (c == null) continue;
                if (!map.TryGetValue(c.Name, out var exist))
                {
                    map[c.Name] = c;
                }
                else
                {
                    var scoreExist = ScoreChannel(exist);
                    var scoreNew = ScoreChannel(c);
                    if (scoreNew > scoreExist) map[c.Name] = c;
                    if (string.IsNullOrWhiteSpace(map[c.Name].Logo) && !string.IsNullOrWhiteSpace(c.Logo)) map[c.Name].Logo = c.Logo;
                }
            }
            return new List<Channel>(map.Values);
        }
        int ScoreChannel(Channel c)
        {
            int score = 0;
            if (!string.IsNullOrWhiteSpace(c.Logo)) score += 1;
            if (!string.IsNullOrWhiteSpace(c.Group) && c.Group.Contains("4K", StringComparison.OrdinalIgnoreCase)) score += 2;
            return score;
        }
        void UpdateGroups()
        {
            var map = new Dictionary<string, List<Channel>>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in _channels)
            {
                if (string.IsNullOrWhiteSpace(c.Group)) continue;
                if (!map.TryGetValue(c.Group, out var list)) { list = new List<Channel>(); map[c.Group] = list; }
                list.Add(c);
            }
            var groups = new List<GroupItem>();
            foreach (var kv in map)
            {
                var items = DistinctByName(kv.Value);
                for (int i = 0; i < items.Count; i++) items[i].DisplayIndex = i + 1;
                groups.Add(new GroupItem { Name = $"{kv.Key}", Items = items });
            }
            groups.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            try { ListGroups.ItemsSource = groups; } catch { }
        }
        void ListGroups_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { }
        void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                try
                {
                    if (sender is System.Windows.FrameworkElement fe && fe.DataContext is Channel ch)
                    {
                        PlayChannel(ch);
                        e.Handled = true;
                    }
                }
                catch { }
            }
        }
        void ToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement fe && fe.DataContext is Channel ch)
                {
                    ch.Favorite = !ch.Favorite;
                    try
                    {
                        var key = UserDataStore.ComputeKey(ch);
                        _userDataStore.SetFavorite(key, ch.Favorite);
                    }
                    catch { }
                    UpdateFavorites();
                }
            }
            catch { }
        }
        void UpdateFavorites()
        {
            try
            {
                var favs = new List<(string key, Channel ch)>();
                foreach (var c in _channels) if (c?.Favorite == true) favs.Add((UserDataStore.ComputeKey(c), c));
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var list = new List<Channel>();
                foreach (var pair in favs) if (set.Add(pair.key)) list.Add(pair.ch);
                ListFavorites.ItemsSource = list;
            }
            catch { }
        }
        void PlayChannel(Channel ch)
        {
            if (_mpv == null || ch == null) return;
            try
            {
                EnsureMpvReadyForLoad();
                StopRecordReplayMonitor();
                ClearRecordingPlayingIndicator();
                if (_timeshiftActive) ToggleTimeshift(false);
                // Clear EPG playing status
                if (_currentPlayingProgram != null)
                {
                    _currentPlayingProgram.IsPlaying = false;
                    _currentPlayingProgram = null;
                    if (ListEpg.ItemsSource is List<EpgProgram> || ListEpg.ItemsSource is IEnumerable<EpgProgram>) 
                    {
                        ListEpg.Items.Refresh();
                    }
                }

                // 更新播放标记用于样式高亮
                foreach (var c in _channels) if (c != null) c.Playing = false;
                ch.Playing = true;
            }
            catch { }
            if (ch.Tag is Source src)
            {
                _paused = false;
                var url = SanitizeUrl(src.Url);
                    // 针对当前播放 URL 及后续候选源做精确预解析（不阻塞）
                    try
                    {
                        var neighbors = new List<string> { url };
                        if (_currentSources != null && _currentSources.Count > 0)
                        {
                            foreach (var s in _currentSources) if (!string.IsNullOrWhiteSpace(s?.Url)) neighbors.Add(s.Url);
                        }
                        LibmpvIptvClient.Services.DnsPrefetcher.PrefetchForUrls(neighbors);
                        LibmpvIptvClient.Services.ConnectionPreheater.PreheatForUrls(neighbors);
                    }
                    catch { }
                if (AppSettings.Current.EnableUdpOptimization) // Check AppSettings instead of CbUdpMode
                {
                    // rtp2httpd 场景下，不要把 HTTP URL 还原为 UDP URL，否则无法通过代理播放
                    // 只有在原始 URL 本身就是 udp:// 或 rtp:// 时才可能涉及优化，
                    // 或者用户明确希望还原（但 rtp2httpd 用户通常不希望还原，因为内网可能不支持组播）
                    // 暂时注释掉这段逻辑，因为 rtp2httpd 用户反馈无法播放，可能是因为被错误还原成了 udp://
                    /*
                    try
                    {
                        var u = new Uri(url);
                        var m = System.Text.RegularExpressions.Regex.Match(u.AbsolutePath, @"/rtp/(?<ip>239\.\d+\.\d+\.\d+):(?<port>\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            var ip = m.Groups["ip"].Value;
                            var port = m.Groups["port"].Value;
                            url = $"udp://{ip}:{port}";
                        }
                    }
                    catch { }
                    */
                }
                // 识别源类型（组播/单播/本地/其他）
                string streamType = "Unicast";
                if (IsMulticast(url)) streamType = "Multicast";
                else if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) streamType = "LocalFile";
                else if (src.Name.Contains("组播")) streamType = "Multicast(Proxy)";
                else if (src.Name.Contains("单播")) streamType = "Unicast(Proxy)";

                Logger.Log($"[Live] 开始直播播放 - 频道: {ch.Name}, 类型: {streamType}, URL: {url}");

                // 强制开启缓存以支持录制复用
                try
                {
                    // 使用更激进的缓存策略
                    _mpv.SetPropertyString("cache", "yes");
                    // 增大前向和后向缓存
                    _mpv.SetPropertyString("demuxer-max-bytes", "512MiB"); 
                    _mpv.SetPropertyString("demuxer-max-back-bytes", "256MiB");
                    // 强制缓存甚至对实时流生效（关键参数）
                    _mpv.SetPropertyString("cache-pause", "no"); 
                    _mpv.SetPropertyString("force-seekable", "yes");
                }
                catch { }
                
                _currentChannel = ch;
                _currentSources = BuildSourcesForChannel(ch);
                _currentUrl = url;
                
                // 重置超时计时器
                _sourceTimeoutTimer.Stop();
                _playStartTime = DateTime.Now;
                _sourceTimeoutTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, AppSettings.Current.SourceTimeoutSec));
                _sourceTimeoutTimer.Start();

                if (AppSettings.Current.FccPrefetchCount > 0 && IsMulticast(url)) // Check AppSettings instead of CbFcc
                {
                    var idx = _channels.IndexOf(ch);
                    var list = new List<string>();
                    var count = Math.Max(0, AppSettings.Current.FccPrefetchCount);
                    for (int i = 1; i <= count; i++)
                    {
                        var n = idx + i;
                        if (n >= 0 && n < _channels.Count)
                        {
                            var next = _channels[n];
                            if (next?.Tag is Source nextSrc)
                            {
                                list.Add(SanitizeUrl(nextSrc.Url));
                            }
                        }
                    }
                    if (list.Count > 0) _mpv.LoadWithPrefetch(url, list);
                    else _mpv.LoadFile(url);
                }
                else
                {
                    _mpv.LoadFile(url);
                }
                try { _firstFrameLogged = false; } catch { }
                UpdatePlayPauseIcon();
                
                // 写入播放记录（直播）
                try
                {
                    var key = UserDataStore.ComputeKey(ch, url);
                    _userDataStore.AddOrUpdateHistory(new HistoryItem
                    {
                        Key = key,
                        Name = ch.Name,
                        Logo = ch.Logo,
                        Group = ch.Group,
                        SourceUrl = url,
                        PlayType = "live"
                    });
                    ListHistory.ItemsSource = _userDataStore.GetHistory();
                }
                catch { }
                
                // Hide placeholder and Show VideoHost
                try 
                { 
                    PlaceholderPanel.Visibility = Visibility.Collapsed; 
                    VideoHost.Visibility = Visibility.Visible;
                    // Update Status Indicator: Live
                    _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播");
                    _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusLiveBrush");
                    TxtPlaybackStatus.Text = _playbackStatusText;
                    TxtPlaybackStatus.Foreground = _playbackStatusBrush;
                    // PlaybackStatusIndicator.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x80, 0x00, 0x00, 0x00));
                    try { TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush; } catch { }
                    
                    // Sync with Overlay
                    _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
                } 
                catch { }

                if (EpgPanel.Visibility == Visibility.Visible) RefreshEpgList(ch);
            }
        }
        void UpdateEpgDisplay()
        {
            if (_epgService == null) return;
            
            // var now = DateTime.Now; // Unused
            foreach (var ch in _channels)
            {
                try
                {
                    // 获取当前节目
                    var prog = _epgService.GetCurrentProgram(ch.TvgId, ch.Name);
                    
                    if (prog != null)
                    {
                        if (ch.CurrentProgramTitle != prog.Title)
                        {
                            ch.CurrentProgramTitle = prog.Title;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(ch.CurrentProgramTitle))
                        {
                            ch.CurrentProgramTitle = "";
                        }
                    }
                }
                catch
                {
                    // Log error but continue loop
                    // Logger.Error($"EPG Update Error for {ch.Name}: {ex.Message}");
                    ch.CurrentProgramTitle = "";
                }
            }

            if (EpgPanel.Visibility == Visibility.Visible && _currentChannel != null)
            {
                if (AppSettings.Current.Epg.StrictMatchByPlaybackTime && (_timeshiftActive || _currentPlayingProgram != null))
                {
                    var t = GetPlaybackLocalTime();
                    if (t.HasValue) { RefreshEpgList(_currentChannel, t.Value); return; }
                }
                RefreshEpgList(_currentChannel);
            }
        }
        DateTime? GetPlaybackLocalTime()
        {
            try
            {
                if (_timeshiftActive)
                {
                    var total = Math.Max(1, (_timeshiftMax - _timeshiftMin).TotalSeconds);
                    var sec = Math.Max(0, Math.Min(total, _timeshiftCursorSec));
                    return _timeshiftMin.AddSeconds(sec);
                }
                if (_currentPlayingProgram != null && _mpv != null)
                {
                    var pos = _mpv.GetTimePos() ?? 0;
                    return _currentPlayingProgram.Start.AddSeconds(Math.Max(0, pos));
                }
            }
            catch { }
            return null;
        }
        void CbEpg_Click(object sender, RoutedEventArgs e)
        {
            bool show = CbEpg.IsChecked == true;
            if (_isFullscreen)
            {
                if (show) ShowFullscreenEpg();
                else CloseFullscreenEpg();
            }
            else
            {
                EpgColumn.Width = new GridLength(show ? 320 : 0);
                EpgPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
            
            try { _overlayWpf?.SetEpgVisible(show); } catch { }
            if (show && _currentChannel != null)
            {
                if (AppSettings.Current.Epg.StrictMatchByPlaybackTime && (_timeshiftActive || _currentPlayingProgram != null))
                {
                    var t = GetPlaybackLocalTime();
                    if (t.HasValue) RefreshEpgList(_currentChannel, t.Value);
                    else RefreshEpgList(_currentChannel);
                }
                else
                {
                    RefreshEpgList(_currentChannel);
                }
            }
        }
        void BtnEpgCollapse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CbEpg.IsChecked = false;
                CbEpg_Click(CbEpg, new RoutedEventArgs());
            }
            catch { }
        }
        private class EpgDateItem
        {
            public DateTime Date { get; set; }
            public string Label { get; set; } = "";
        }
        
        void RefreshEpgList(Channel ch)
        {
            if (ch == null) return;
            LblEpgChannel.Text = ch.Name;
            
            // Re-fetch programs for the channel
            var programs = _epgService?.GetPrograms(ch.TvgId, ch.Name);
            try { Logger.Log($"[EPG] 渲染频道 {ch.Name} EPG 列表，数据条数={(programs?.Count ?? 0)}"); } catch { }

            // 清空旧数据状态，防止残留
            _currentEpgDate = DateTime.Today;
            _availableDates.Clear();

            if (programs == null || programs.Count == 0)
            {
                // No EPG data found -> Generate placeholders
                programs = GeneratePlaceholderEpg();
            }

            // Group by date
            _availableDates = programs.Select(p => p.Start.Date).Distinct().OrderBy(d => d).ToList();
            
            // Set current date (Today or first available)
            if (_availableDates.Contains(DateTime.Today)) 
            {
                _currentEpgDate = DateTime.Today;
            }
            else if (_availableDates.Count > 0) 
            {
                _currentEpgDate = _availableDates[0];
            }
            // else: _currentEpgDate is already Today from reset above

            UpdateEpgDateUI();
            
            // Filter and display
            var filtered = programs.Where(p => p.Start.Date == _currentEpgDate).OrderBy(p => p.Start).ToList();
            try
            {
                // 标记“已预约”状态
                var key = _currentChannel.TvgId ?? _currentChannel.Id ?? "";
                foreach (var p in filtered)
                {
                    p.IsBooked = AppSettings.Current.ScheduledReminders != null &&
                                 AppSettings.Current.ScheduledReminders.Exists(r =>
                                     r.Enabled && !r.Completed &&
                                     string.Equals(r.ChannelId ?? "", key ?? "", StringComparison.OrdinalIgnoreCase) &&
                                     Math.Abs((r.StartAtUtc - p.Start.ToUniversalTime()).TotalSeconds) <= 60);
                }
            }
            catch { }
            ListEpg.ItemsSource = filtered;
            try { Logger.Log($"[EPG] 日期 {_currentEpgDate:yyyy-MM-dd} 可见节目数 {filtered.Count}"); } catch { }
            
            // Scroll logic
            if (_currentEpgDate == DateTime.Today)
            {
                var now = DateTime.Now;
                var current = filtered.FirstOrDefault(p => p.Start <= now && p.End > now);
                if (current != null) ListEpg.ScrollIntoView(current);
            }
            else
            {
                if (ListEpg.Items.Count > 0) ListEpg.ScrollIntoView(ListEpg.Items[0]);
            }
        }
        void RefreshEpgList(Channel ch, DateTime focusTime)
        {
            if (ch == null) return;
            _currentEpgDate = focusTime.Date;
            RefreshEpgList(ch);
            try
            {
                var items = ListEpg.ItemsSource as System.Collections.IEnumerable;
                if (items != null)
                {
                    foreach (var it in items)
                    {
                        if (it is EpgProgram p && p.Start <= focusTime && p.End > focusTime)
                        {
                            ListEpg.ScrollIntoView(it);
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        List<EpgProgram> GeneratePlaceholderEpg()
        {
            var list = new List<EpgProgram>();
            var today = DateTime.Today;
            // Generate for today only, 1-hour blocks
            for (int i = 0; i < 24; i++)
            {
                var start = today.AddHours(i);
                var end = today.AddHours(i + 1);
                list.Add(new EpgProgram
                {
                    Title = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Featured", "精彩节目"),
                    Start = start,
                    End = end
                });
            }
            return list;
        }

        void UpdateEpgDateUI()
        {
            if (_availableDates.Count == 0)
            {
                TxtCurrentDate.Text = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_NoData", "无数据");
                BtnPrevDay.IsEnabled = false;
                BtnNextDay.IsEnabled = false;
                return;
            }

            var idx = _availableDates.IndexOf(_currentEpgDate);
            if (idx < 0 && _availableDates.Count > 0)
            {
                _currentEpgDate = _availableDates[0];
                idx = 0;
            }

            // Label
            var d = _currentEpgDate;
            var today = DateTime.Today;
            string label = d.ToString("MM-dd");
            if (d == today) label = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Today", "今天");
            else if (d == today.AddDays(1)) label = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Tomorrow", "明天");
            else if (d == today.AddDays(-1)) label = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Yesterday", "昨天");
            TxtCurrentDate.Text = label;

            // Buttons
            BtnPrevDay.IsEnabled = idx > 0;
            BtnNextDay.IsEnabled = idx < _availableDates.Count - 1;
        }

        void BtnPrevDay_Click(object sender, RoutedEventArgs e)
        {
            var idx = _availableDates.IndexOf(_currentEpgDate);
            if (idx > 0)
            {
                _currentEpgDate = _availableDates[idx - 1];
                UpdateEpgDateUI();
                FilterEpgList();
            }
        }

        void BtnNextDay_Click(object sender, RoutedEventArgs e)
        {
            var idx = _availableDates.IndexOf(_currentEpgDate);
            if (idx >= 0 && idx < _availableDates.Count - 1)
            {
                _currentEpgDate = _availableDates[idx + 1];
                UpdateEpgDateUI();
                FilterEpgList();
            }
        }

        void FilterEpgList()
        {
            if (_currentChannel == null) return;
            
            // Re-fetch programs for the channel
            var programs = _epgService?.GetPrograms(_currentChannel.TvgId, _currentChannel.Name);

            if (programs == null || programs.Count == 0)
            {
                // No EPG data found -> Generate placeholders
                programs = GeneratePlaceholderEpg();
            }

            var filtered = programs.Where(p => p.Start.Date == _currentEpgDate).OrderBy(p => p.Start).ToList();
            try
            {
                var key = _currentChannel.TvgId ?? _currentChannel.Id ?? "";
                foreach (var p in filtered)
                {
                    p.IsBooked = AppSettings.Current.ScheduledReminders != null &&
                                 AppSettings.Current.ScheduledReminders.Exists(r =>
                                     r.Enabled && !r.Completed &&
                                     string.Equals(r.ChannelId ?? "", key ?? "", StringComparison.OrdinalIgnoreCase) &&
                                     Math.Abs((r.StartAtUtc - p.Start.ToUniversalTime()).TotalSeconds) <= 60);
                }
            }
            catch { }
            ListEpg.ItemsSource = filtered;
            
            // Scroll to current program if viewing today
            if (_currentEpgDate == DateTime.Today)
            {
                var now = DateTime.Now;
                var current = filtered.FirstOrDefault(p => p.Start <= now && p.End > now);
                if (current != null) ListEpg.ScrollIntoView(current);
            }
            else
            {
                if (ListEpg.Items.Count > 0) ListEpg.ScrollIntoView(ListEpg.Items[0]);
            }
        }
        void ListEpg_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBoxItem item && item.DataContext is EpgProgram prog)
            {
                if ((prog.Status == "回放" || prog.Status == LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放")) && _currentChannel != null)
                {
                    PlayCatchup(_currentChannel, prog);
                    e.Handled = true;
                }
            }
        }
        void EpgMenu_Remind_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentChannel == null) return;
                var mi = sender as System.Windows.Controls.MenuItem;
                if (mi == null) return;
                var ctx = mi.DataContext as EpgProgram;
                if (ctx == null) return;
                var now = DateTime.Now;
                if (ctx.Start <= now) return;
                        var ownerWin2 = (_isFullscreen && _fs != null) ? (Window)_fs : this;
                        var dlg = new ReminderDialog(_currentChannel.Name, ctx.Title, ctx.Start) { Owner = ownerWin2, WindowStartupLocation = WindowStartupLocation.CenterOwner, Topmost = _isFullscreen };
                if (dlg.ShowDialog() == true)
                {
                    var r = new ScheduledReminder
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ChannelId = _currentChannel.TvgId ?? _currentChannel.Id ?? "",
                        ChannelName = _currentChannel.Name ?? "",
                        ChannelLogo = _currentChannel.Logo ?? "",
                        StartAtUtc = ctx.Start.ToUniversalTime(),
                        PreAlertSeconds = dlg.PreAlertSeconds,
                        Action = dlg.Action,
                        Enabled = true,
                        Note = ctx.Title ?? ""
                    };
                    if (AppSettings.Current.ScheduledReminders == null) AppSettings.Current.ScheduledReminders = new List<ScheduledReminder>();
                    var dupKey = (r.ChannelId + "|" + r.StartAtUtc.ToString("o")).ToLowerInvariant();
                    AppSettings.Current.ScheduledReminders.RemoveAll(x => (x.ChannelId + "|" + x.StartAtUtc.ToString("o")).ToLowerInvariant() == dupKey);
                    AppSettings.Current.ScheduledReminders.Add(r);
                    AppSettings.Current.Save();
                    try { LibmpvIptvClient.Services.ReminderService.Instance.Start(); } catch { }
                    try 
                    { 
                        LibmpvIptvClient.Services.ToastService.ShowReminder(r.ChannelId, r.ChannelName, r.Note, r.StartAtUtc.ToLocalTime(), r.ChannelLogo, false);
                    } 
                    catch 
                    { 
                        try { LibmpvIptvClient.Services.ToastService.ShowReminder(r.ChannelName ?? "", r.ChannelName ?? "", r.Note ?? "", r.StartAtUtc.ToLocalTime(), _currentChannel?.Logo, false); } catch { } 
                    }
                }
            }
            catch { }
        }
        void EpgRemindButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentChannel == null) return;
                if (sender is System.Windows.Controls.Button btn && btn.DataContext is EpgProgram ctx)
                {
                    if (ctx.Start <= DateTime.Now) return;
                    // 乐观更新：立即标记为已预约
                    var oldBooked = ctx.IsBooked;
                    ctx.IsBooked = true;
                    try { ListEpg.Items.Refresh(); } catch { }
                    var ownerWin = (_isFullscreen && _fs != null) ? (Window)_fs : this;
                    var dlg = new ReminderDialog(_currentChannel.Name, ctx.Title, ctx.Start) { Owner = ownerWin, WindowStartupLocation = WindowStartupLocation.CenterOwner, Topmost = _isFullscreen };
                    if (dlg.ShowDialog() == true)
                    {
                        var r = new ScheduledReminder
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            ChannelId = _currentChannel.TvgId ?? _currentChannel.Id ?? "",
                            ChannelName = _currentChannel.Name ?? "",
                            ChannelLogo = _currentChannel.Logo ?? "",
                            StartAtUtc = ctx.Start.ToUniversalTime(),
                            PreAlertSeconds = dlg.PreAlertSeconds,
                            Action = dlg.Action,
                            PlayMode = dlg.PlayMode,
                            Enabled = true,
                            Note = ctx.Title ?? ""
                        };
                        if (AppSettings.Current.ScheduledReminders == null) AppSettings.Current.ScheduledReminders = new List<ScheduledReminder>();
                        var dupKey = (r.ChannelId + "|" + r.StartAtUtc.ToString("o")).ToLowerInvariant();
                        AppSettings.Current.ScheduledReminders.RemoveAll(x => (x.ChannelId + "|" + x.StartAtUtc.ToString("o")).ToLowerInvariant() == dupKey);
                        AppSettings.Current.ScheduledReminders.Add(r);
                        AppSettings.Current.Save();
                        try { LibmpvIptvClient.Services.ReminderService.Instance.Start(); } catch { }
                        try 
                        { 
                        LibmpvIptvClient.Services.ToastService.ShowReminder(r.ChannelId, r.ChannelName, r.Note, r.StartAtUtc.ToLocalTime(), r.ChannelLogo, false);
                        } 
                        catch 
                        { 
                            try { LibmpvIptvClient.Services.ToastService.ShowReminder(r.ChannelName ?? "", r.ChannelName ?? "", r.Note ?? "", r.StartAtUtc.ToLocalTime(), _currentChannel?.Logo, false); } catch { } 
                        }
                    }
                    else
                    {
                        // 回滚
                        ctx.IsBooked = oldBooked;
                        try { ListEpg.Items.Refresh(); } catch { }
                    }
                }
            }
            catch { }
        }
        public Rect GetVideoScreenRect()
        {
            try
            {
                FrameworkElement anchor = VideoHost ?? (FrameworkElement)this;
                double w = anchor.ActualWidth;
                double h = anchor.ActualHeight;
                // 若未播放或布局未就绪，使用主窗口客户区作为锚点
                if (w < 50 || h < 50)
                {
                    var winTopLeft = this.PointToScreen(new System.Windows.Point(0, 0));
                    var dpiw = VisualTreeHelper.GetDpi(this);
                    double wl = winTopLeft.X / dpiw.DpiScaleX;
                    double wt = winTopLeft.Y / dpiw.DpiScaleY;
                    double ww = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
                    double wh = this.ActualHeight > 0 ? this.ActualHeight : this.Height;
                    return new Rect(wl, wt, ww, wh);
                }
                var topLeft = anchor.PointToScreen(new System.Windows.Point(0, 0));
                var dpi = VisualTreeHelper.GetDpi(this);
                double left = topLeft.X / dpi.DpiScaleX;
                double top = topLeft.Y / dpi.DpiScaleY;
                if (_isFullscreen && _fs != null)
                {
                    return new Rect(_fs.Left, _fs.Top, _fs.Width, _fs.Height);
                }
                return new Rect(left, top, w, h);
            }
            catch
            {
                // 兜底：窗口居中
                var winTopLeft = this.PointToScreen(new System.Windows.Point(0, 0));
                var dpi = VisualTreeHelper.GetDpi(this);
                double wl = winTopLeft.X / dpi.DpiScaleX;
                double wt = winTopLeft.Y / dpi.DpiScaleY;
                double ww = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
                double wh = this.ActualHeight > 0 ? this.ActualHeight : this.Height;
                return new Rect(wl, wt, ww, wh);
            }
        }

        public void JumpToChannelByIdOrName(string id, string name)
        {
            try
            {
                if (_channels == null || _channels.Count == 0) return;
                Channel? target = null;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    target = _channels.FirstOrDefault(c => string.Equals(c?.TvgId ?? "", id, StringComparison.OrdinalIgnoreCase)
                                                        || string.Equals(c?.Id ?? "", id, StringComparison.OrdinalIgnoreCase));
                }
                if (target == null && !string.IsNullOrWhiteSpace(name))
                {
                    target = _channels.FirstOrDefault(c => string.Equals(c?.Name ?? "", name, StringComparison.OrdinalIgnoreCase));
                }
                if (target != null) PlayChannel(target);
            }
            catch { }
        }
        private EpgProgram? _currentPlayingProgram; // Track playing catchup

        void PlayCatchup(Channel ch, EpgProgram prog)
        {
            StopRecordReplayMonitor();
            EnsureMpvReadyForLoad();
            ClearRecordingPlayingIndicator();
            var url = ch.CatchupSource;
            if (string.IsNullOrEmpty(url))
            {
                if (AppSettings.Current.Replay.Enabled && !string.IsNullOrEmpty(AppSettings.Current.Replay.UrlFormat))
                {
                    var fmt = AppSettings.Current.Replay.UrlFormat;
                    if (!string.IsNullOrEmpty(fmt) && (fmt.StartsWith("?") || fmt.StartsWith("&")))
                    {
                        var live = (ch.Tag is Source s1 && !string.IsNullOrEmpty(s1.Url)) ? s1.Url
                                   : (ch.Sources != null && ch.Sources.Count > 0 ? ch.Sources[0].Url : "");
                        if (string.IsNullOrEmpty(live)) return;
                        var sep = live.Contains("?") ? "&" : "?";
                        url = live + sep + fmt.TrimStart('?', '&');
                    }
                    else
                    {
                        url = fmt.Replace("{id}", ch.Id ?? ch.Name);
                    }
                }
                else return;
            }
            try
            {
                if (_timeshiftActive) ToggleTimeshift(false);
                
                // ... (url processing) ...
                // 替换占位符 (统一处理)
                url = ProcessUrlPlaceholders(url, prog.Start, prog.End);
                try { url = LibmpvIptvClient.Services.UrlTimeRewriter.RewriteIfEnabled(AppSettings.Current, url, prog.Start, prog.End, false); } catch { }

                Logger.Log($"[Replay] 开始回放 - 节目: {prog.Title}, 频道: {ch.Name}, 时间: {prog.Start:HH:mm}-{prog.End:HH:mm}, URL: {url}");
                _mpv?.LoadFile(url);
                _currentUrl = url;
                try { PlaceholderPanel.Visibility = Visibility.Collapsed; VideoHost.Visibility = Visibility.Visible; } catch { }
                
                // Update playing status
                if (_currentPlayingProgram != null) _currentPlayingProgram.IsPlaying = false;
                _currentPlayingProgram = prog;
                _currentPlayingProgram.IsPlaying = true;
                
                UpdatePlayPauseIcon();
                try
                {
                    if (EpgPanel.Visibility == Visibility.Visible && _currentChannel != null)
                    {
                        RefreshEpgList(_currentChannel, prog.Start);
                    }
                }
                catch { }
                // Force UI refresh to update "Playing" status in EPG list
                if (ListEpg.ItemsSource is List<EpgProgram> list)
                {
                    ListEpg.Items.Refresh();
                }

                // Update Status Indicator: Catchup
                try
                {
                    TxtPlaybackStatus.Text = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放");
                    TxtPlaybackStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    PlaybackStatusIndicator.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x80, 0x00, 0x00, 0x00));
                    try { TxtBottomPlaybackStatus.Text = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放"); TxtBottomPlaybackStatus.Foreground = System.Windows.Media.Brushes.Orange; } catch { }
                    
                    // Sync with Overlay
                    _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放");
                    _playbackStatusBrush = System.Windows.Media.Brushes.Orange;
                    _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
                }
                catch { }
                
                // 启用倍速（回放模式）
                try 
                { 
                    _overlayWpf?.SetSpeedEnabled(true); 
                    _mpv?.SetSpeed(_playbackSpeed); 
                    _overlayWpf?.SetSpeed(_playbackSpeed); 
                } 
                catch { }
                try 
                { 
                    if (BtnSpeed != null) 
                    { 
                        BtnSpeed.IsEnabled = true; 
                        LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; 
                    } 
                } 
                catch { }
                
                // 写入播放记录（回放）
                try
                {
                    var key = _currentChannel != null ? UserDataStore.ComputeKey(_currentChannel, url) : (prog.Title + "|" + url);
                    _userDataStore.AddOrUpdateHistory(new HistoryItem
                    {
                        Key = key,
                        Name = _currentChannel?.Name ?? prog.Title,
                        Logo = _currentChannel?.Logo ?? "",
                        Group = _currentChannel?.Group ?? "",
                        SourceUrl = url,
                        PlayType = "catchup"
                    });
                    ListHistory.ItemsSource = _userDataStore.GetHistory();
                }
                catch { }
            }
            catch (Exception ex)
            {
                Logger.Error("回放失败: " + ex.Message);
            }
        }
        string ProcessUrlPlaceholders(string url, DateTime start, DateTime end)
        {
            // 1. Unix Timestamp & Duration (rtp2httpd macros)
            long tsStart = new DateTimeOffset(start).ToUnixTimeSeconds();
            long tsEnd = new DateTimeOffset(end).ToUnixTimeSeconds();
            url = url.Replace("${timestamp}", tsStart.ToString());
            url = url.Replace("{timestamp}", tsStart.ToString());
            url = url.Replace("${end_timestamp}", tsEnd.ToString());
            url = url.Replace("{end_timestamp}", tsEnd.ToString());
            
            long dur = (long)(end - start).TotalSeconds;
            url = url.Replace("${duration}", dur.ToString());
            url = url.Replace("{duration}", dur.ToString());

            // 2. {utc:...} and {utcend:...} with Macro Expansion
            url = ReplaceTimePlaceholder(url, "{utc:", "}", start.ToUniversalTime(), end.ToUniversalTime());
            url = ReplaceTimePlaceholder(url, "{utcend:", "}", start.ToUniversalTime(), end.ToUniversalTime());

            // 3. ${...} format with Macro Expansion
            url = System.Text.RegularExpressions.Regex.Replace(url, @"\$\{\((b|e)\)(.*?)\}", m =>
            {
                var type = m.Groups[1].Value;
                var fmt = m.Groups[2].Value;
                
                // Expand rtp2httpd Macros
                if (fmt == "YmdHMS") fmt = "yyyyMMddHHmmss";
                else if (fmt == "Ymd") fmt = "yyyyMMdd";
                else if (fmt == "HMS") fmt = "HHmmss";
                
                var dt = (type == "b" ? start : end);
                if (fmt.EndsWith("|UTC", StringComparison.OrdinalIgnoreCase))
                {
                    dt = dt.ToUniversalTime();
                    fmt = fmt.Substring(0, fmt.Length - 4);
                }
                
                // Unix seconds for start/end
                if (string.Equals(fmt, "timestamp", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fmt, "unix", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fmt, "epoch", StringComparison.OrdinalIgnoreCase))
                {
                    var unix = new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeSeconds();
                    return unix.ToString();
                }
                try { return dt.ToString(fmt); } catch { return m.Value; }
            });

            // 4. Fixed Local Time Placeholders
            url = url.Replace("{start}", start.ToString("yyyyMMddHHmmss"));
            url = url.Replace("{end}", end.ToString("yyyyMMddHHmmss"));
            // 5. Append EPG tracking parameters (minute-level) for replay/timeshift correlation
            try
            {
                var minTs = start.ToString("yyyy-MM-ddTHH:mm");
                var sep = url.Contains("?") ? "&" : "?";
                url = url + sep + "epg_time=" + Uri.EscapeDataString(minTs);
            }
            catch { }
            return url;
        }

        string ReplaceTimePlaceholder(string input, string prefix, string suffix, DateTime start, DateTime end)
        {
            // Simple regex replacement for {utc:format}
            // Need to find all occurrences
            var pattern = System.Text.RegularExpressions.Regex.Escape(prefix) + "(.*?)" + System.Text.RegularExpressions.Regex.Escape(suffix);
            return System.Text.RegularExpressions.Regex.Replace(input, pattern, m =>
            {
                var fmt = m.Groups[1].Value;
                
                // Expand rtp2httpd Macros
                if (fmt == "YmdHMS") fmt = "yyyyMMddHHmmss";
                else if (fmt == "Ymd") fmt = "yyyyMMdd";
                else if (fmt == "HMS") fmt = "HHmmss";

                if (prefix.Contains("end")) return end.ToString(fmt);
                return start.ToString(fmt);
            });
        }
        void AllChannel_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (sender is System.Windows.FrameworkElement fe && fe.DataContext is Channel ch)
                {
                    PlayChannel(ch);
                }
            }
            catch { }
        }
        List<Source> BuildSourcesForChannel(Channel ch)
        {
            var list = new List<Source>();
            if (ch.Sources != null) foreach (var s in ch.Sources) if (!list.Exists(x => string.Equals(x.Url, s.Url, StringComparison.OrdinalIgnoreCase))) list.Add(s);
            foreach (var c in _channels)
            {
                if (c == null) continue;
                if (string.Equals(c.Name, ch.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (c.Sources != null)
                        foreach (var s in c.Sources)
                            if (!list.Exists(x => string.Equals(x.Url, s.Url, StringComparison.OrdinalIgnoreCase))) list.Add(s);
                    if (c.Tag != null && !list.Exists(x => string.Equals(x.Url, c.Tag.Url, StringComparison.OrdinalIgnoreCase))) list.Add(c.Tag);
                }
            }
            return list;
        }
        string SourceLabel(Source s)
        {
            var parts = new List<string>();
            // 优先使用源名称（通常来自 M3U $后缀）来判断组播/单播
            bool isMulticast = false;
            bool isUnicast = false;

            if (!string.IsNullOrWhiteSpace(s.Name))
            {
                if (s.Name.Contains("组播")) isMulticast = true;
                else if (s.Name.Contains("单播")) isUnicast = true;
                
                // 如果源名称本身很有意义（不只是简单的组播/单播），可以考虑直接使用它
                // 但为了保持 UI 统一，我们先提取关键信息
            }
            
            if (!isMulticast && !isUnicast)
            {
                isMulticast = IsMulticast(s.Url);
            }

            parts.Add(isMulticast ? LibmpvIptvClient.Helpers.ResxLocalizer.Get("Stream_Multicast", "组播") : LibmpvIptvClient.Helpers.ResxLocalizer.Get("Stream_Unicast", "单播"));
            
            if (s.Quality != null)
            {
                if (s.Quality.Height >= 2160) parts.Add("UHD");
                else if (s.Quality.Height >= 1080) parts.Add("FHD");
                else if (s.Quality.Height >= 720) parts.Add("HD");
                else if (s.Quality.Height > 0) parts.Add("SD");
            }
            
            if (s.Quality != null && s.Quality.Fps > 0) parts.Add($"{s.Quality.Fps:0.#}fps");
            
            return string.Join("-", parts);
        }
        void BtnSources_Click(object sender, RoutedEventArgs e)
        {
            OpenSourceMenuAtButton(BtnSources);
        }
        void BtnRatio_Click(object sender, RoutedEventArgs e)
        {
            var menu = new System.Windows.Controls.ContextMenu();
            var options = new[] { 
                (LibmpvIptvClient.Helpers.ResxLocalizer.Get("Ratio_Default", "默认"), "default"),
                ("16:9", "16:9"),
                ("4:3", "4:3"),
                (LibmpvIptvClient.Helpers.ResxLocalizer.Get("Ratio_Stretch", "拉伸"), "stretch"),
                (LibmpvIptvClient.Helpers.ResxLocalizer.Get("Ratio_Fill", "填充"), "fill"),
                (LibmpvIptvClient.Helpers.ResxLocalizer.Get("Ratio_Crop", "裁剪"), "crop")
            };
            foreach (var (label, val) in options)
            {
                var mi = new System.Windows.Controls.MenuItem();
                mi.Header = label;
                mi.IsCheckable = true;
                mi.IsChecked = string.Equals(_currentAspect, val, StringComparison.OrdinalIgnoreCase);
                mi.Click += (s, ev) => 
                { 
                    _currentAspect = val;
                    if (_overlayWpf != null) _overlayWpf.CurrentAspect = _currentAspect;
                    _mpv?.SetAspectRatio(val);
                };
                menu.Items.Add(mi);
            }
            BtnRatio.ContextMenu = menu;
            BtnRatio.ContextMenu.PlacementTarget = BtnRatio;
            BtnRatio.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Custom;
            BtnRatio.ContextMenu.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
                new[] { new System.Windows.Controls.Primitives.CustomPopupPlacement(new System.Windows.Point((targetSize.Width - popupSize.Width) / 2, -popupSize.Height - 30), System.Windows.Controls.Primitives.PopupPrimaryAxis.Horizontal) };
            BtnRatio.ContextMenu.IsOpen = true;
        }
        void OpenSourceMenuAtButton(System.Windows.Controls.Button target)
        {
            if (_currentChannel == null) return;
            if (_currentSources == null || _currentSources.Count == 0) _currentSources = BuildSourcesForChannel(_currentChannel);
            
            // 获取当前播放的源 URL
            string currentPlayingUrl = "";
            if (_currentChannel.Tag is Source playingSrc)
            {
                currentPlayingUrl = SanitizeUrl(playingSrc.Url);
            }

            var menu = new System.Windows.Controls.ContextMenu();
            foreach (var s in _currentSources)
            {
                var mi = new System.Windows.Controls.MenuItem();
                mi.Header = SourceLabel(s);
                mi.Tag = s;
                
                // 如果是当前播放源，设置为选中状态
                if (SanitizeUrl(s.Url) == currentPlayingUrl)
                {
                    mi.IsChecked = true;
                }

                mi.Click += (s2, e2) =>
                {
                    if (_mpv == null) return;
                    // 更新当前频道的播放源记录
                    var newSrc = (Source)((System.Windows.Controls.MenuItem)s2).Tag;
                    _currentChannel.Tag = newSrc;
                    
                    var u = SanitizeUrl(newSrc.Url);
                    Logger.Log("切换源 " + u);
                    _mpv.LoadFile(u);
                };
                menu.Items.Add(mi);
            }
            target.ContextMenu = menu;
            target.ContextMenu.PlacementTarget = target;
            target.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Custom;
            target.ContextMenu.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
                new[] { new System.Windows.Controls.Primitives.CustomPopupPlacement(new System.Windows.Point((targetSize.Width - popupSize.Width) / 2, -popupSize.Height - 30), System.Windows.Controls.Primitives.PopupPrimaryAxis.Horizontal) };
            target.ContextMenu.IsOpen = true;
        }
        void OpenSourceMenuAtOverlay()
        {
            if (_overlayWpf == null) return;
            if (_currentChannel == null) return;
            if (_currentSources == null || _currentSources.Count == 0) _currentSources = BuildSourcesForChannel(_currentChannel);
            
            // 获取当前播放的源 URL
            string currentPlayingUrl = "";
            if (_currentChannel.Tag is Source playingSrc)
            {
                currentPlayingUrl = SanitizeUrl(playingSrc.Url);
            }

            var menu = new System.Windows.Controls.ContextMenu();
            foreach (var s in _currentSources)
            {
                var mi = new System.Windows.Controls.MenuItem();
                mi.Header = SourceLabel(s);
                mi.Tag = s;
                mi.IsCheckable = true;
                
                // 如果是当前播放源，设置为选中状态
                if (SanitizeUrl(s.Url) == currentPlayingUrl)
                {
                    mi.IsChecked = true;
                }

                mi.Click += (s2, e2) =>
                {
                    if (_mpv == null) return;
                    // 更新当前频道的播放源记录
                    var newSrc = (Source)((System.Windows.Controls.MenuItem)s2).Tag;
                    _currentChannel.Tag = newSrc;
                    
                    var u = SanitizeUrl(newSrc.Url);
                    Logger.Log("切换源 " + u);
                    _mpv.LoadFile(u);
                };
                menu.Items.Add(mi);
            }
            _overlayWpf.OpenSourceContextMenu(menu);
        }
        bool IsMulticast(string url)
        {
            try
            {
                var s = url.ToLowerInvariant();
                if (s.StartsWith("udp://239.")) return true;
                if (s.Contains("/rtp/239.")) return true;
                if (System.Text.RegularExpressions.Regex.IsMatch(url, @"239\.\d+\.\d+\.\d+")) return true;
            }
            catch { }
            return false;
        }
        void ToggleFullscreen(bool on)
        {
            if (on == _isFullscreen) return;

            // 1. 暂停所有定时器，防止在切换过程中读取不稳定的 MPV 状态
            _timer.Stop();
            _overlayPollTimer.Stop();

            // 2. 移除切换前的 MPV 状态读取，直接信任当前的 _timeshiftCursorSec
            // 因为在切换过程中 MPV 的状态可能不稳定（归零），导致进度条重置
            // 而 Timer_Tick 在切换前一直在正常更新，所以当前的 _timeshiftCursorSec 是最准确的

            if (on)
            {
                _isFullscreen = true; // 先切标志，避免早期轮询把悬浮条隐藏
                
                // 确保同步两个全屏按钮的状态
                try { if (BtnWinFullscreen != null) BtnWinFullscreen.IsChecked = on; } catch { }

                // Hide Title Bar (Row 0)
                try { ((Grid)Content).RowDefinitions[0].Height = new GridLength(0); } catch { }

                BottomBar.Visibility = Visibility.Collapsed;
                _fs = new FullscreenWindow();
                _fs.Topmost = true; // 启用置顶，确保不被最大化窗口覆盖
                _fs.Owner = this;
                _fs.Loaded += OnFullscreenLoaded;
                _fsPanel = _fs.VideoPanel;
                if (_mpv != null && _fsPanel != null)
                {
                    _mpv.SetWid(_fsPanel.Handle);
                }
                try { if (_fsPanel != null) _fsPanel.DoubleClick += FsPanel_DoubleClick; } catch { }
                try { if (_fsPanel != null) _fsPanel.MouseWheel += FsPanel_MouseWheel; } catch { }
                try { if (_fsPanel != null) _fsPanel.MouseUp += FsPanel_MouseUp; } catch { }
                _fs.ExitRequested += () => ToggleFullscreen(false);
                _fs.PlayPauseRequested += () => BtnPlayPause_Click(this, new RoutedEventArgs());
                _fs.SeekRequested += (dir) => TryArrowSeek(dir);
                try { _fs.Host.PreviewMouseWheel += FsHost_PreviewMouseWheel; } catch { }
                _fs.Activated += (s, e) => 
                {
                    // 当全屏窗口激活时，确保子窗口在最前
                    try
                    {
                        if (_fsEpg != null && _fsEpg.IsVisible) 
                        {
                            _fsEpg.Topmost = true;
                            var h = new System.Windows.Interop.WindowInteropHelper(_fsEpg).Handle;
                            SetWindowPos(h, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
                        }
                        if (_fsDrawer != null && _fsDrawer.IsVisible)
                        {
                            _fsDrawer.Topmost = true;
                            var h = new System.Windows.Interop.WindowInteropHelper(_fsDrawer).Handle;
                            SetWindowPos(h, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
                        }
                        if (_topOverlay != null && _topOverlay.IsVisible)
                        {
                            _topOverlay.Topmost = true;
                        }
                    }
                    catch { }
                };
                _fs.StateChanged += (s, e) =>
                {
                    // 监控最大化/还原，确保始终最大化
                    if (_fs.WindowState != WindowState.Maximized)
                    {
                         try { _fs.WindowState = WindowState.Maximized; } catch { }
                    }
                };
                _fs.Deactivated += (s, e) =>
                {
                    // 窗口失去焦点（如点击其他软件），尝试保持置顶
                    try 
                    { 
                        if (_fs != null && !_fs.Topmost) _fs.Topmost = true; 
                    } 
                    catch { }
                };
                _fs.SizeChanged += (s, e) => { PositionOverlay(); PositionTopOverlay(); };
                _fs.LocationChanged += (s, e) => { PositionOverlay(); PositionTopOverlay(); };
                _fs.MouseMove += (s, e) => ShowOverlayWithDelay();
                try { _fs.Host.MouseMove += (s, e) => ShowOverlayWithDelay(); } catch { }
                try { _fs.Host.PreviewMouseMove += (s, e) => ShowOverlayWithDelay(); } catch { }
                if (_fsPanel != null)
                {
                    _fsPanel.MouseMove += (s, e) => ShowOverlayWithDelay();
                    _fsPanel.MouseEnter += (s, e) => ShowOverlayWithDelay();
                }
                // 注意：这里不要立即 Start timer，等到 OnFullscreenLoaded 再恢复
                _fs.Show();
                _fs.Focus();
                ResetOverlayForOwner(_fs, true);
                ShowFsOverlayNow();
                try { _fs.Activate(); _fs.Focus(); } catch { }
                // Add WinForms key filter to capture keys even when WinForms controls hold focus
                try
                {
                    _fsKeyFilter = new WinFormsKeyFilter(
                        exit: () => ToggleFullscreen(false),
                        playPause: () => BtnPlayPause_Click(this, new RoutedEventArgs()),
                        seek: (dir) => TryArrowSeek(dir)
                    );
                    System.Windows.Forms.Application.AddMessageFilter(_fsKeyFilter);
                }
                catch { }
                
                // Create Top Overlay
                _topOverlay = new TopOverlay();
                _topOverlay.Minimize += () => BtnMin_Click(this, new RoutedEventArgs());
                _topOverlay.MaximizeRestore += () => ToggleFullscreen(false); // In FS, Maximize/Restore exits FS? Or just Restore? Usually "Restore" exits FS.
                _topOverlay.CloseWindow += () => ExitApp();
                _topOverlay.ExitApp += () => ExitApp();
                _topOverlay.FullscreenToggle += () => ToggleFullscreen(false);
                _topOverlay.OpenFile += () => { ToggleFullscreen(false); PromptOpenFile(); };
                _topOverlay.OpenUrl += () => { ToggleFullscreen(false); PromptOpenUrl(); };
                _topOverlay.AddM3uFile += () => { ToggleFullscreen(false); PromptAddM3uFile(); };
                _topOverlay.AddM3u += () => { ToggleFullscreen(false); PromptAddM3u(); };
                _topOverlay.LoadM3u += (src) => LoadM3uSource(src);
                _topOverlay.EditM3u += EditM3uSource;
                _topOverlay.FccChanged += (on) => { try { _overlayWpf?.SetFcc(on); } catch { } };
                _topOverlay.UdpChanged += (on) => { try { _overlayWpf?.SetUdp(on); } catch { } };
                _topOverlay.EpgToggled += (on) => { CbEpg.IsChecked = on; CbEpg_Click(CbEpg, new RoutedEventArgs()); };
                _topOverlay.DrawerToggled += (on) => SetDrawerCollapsed(!on);
                
                _topOverlay.IsUdpEnabled = () => AppSettings.Current.EnableUdpOptimization;
                _topOverlay.IsEpgVisible = () => CbEpg.IsChecked == true;
                _topOverlay.IsDrawerVisible = () => !_drawerCollapsed;
                
                _topOverlay.IsTopmost = () => Topmost;
                _topOverlay.TopmostChanged += (on) => 
                {
                    Topmost = on;
                    if (_fs != null) _fs.Topmost = on;
                    // Ensure overlay stays on top of FS
                    if (_topOverlay != null) _topOverlay.Topmost = true; 
                };
                _topOverlay.OpenSettings += OpenSettings;
                
                PositionTopOverlay();
                // Initially hidden, shown on hover
                _topOverlay.Hide();
                try { _fs.Activate(); _fs.Focus(); } catch { }
            }
            else
            {
                _isFullscreen = false; // 先切标志，确保后续逻辑按窗口模式处理

                // 确保同步两个全屏按钮的状态
                try { if (BtnWinFullscreen != null) BtnWinFullscreen.IsChecked = on; } catch { }
                
                // Show Title Bar
                try { ((Grid)Content).RowDefinitions[0].Height = GridLength.Auto; } catch { }

                CloseFullscreenDrawer();
                CloseFullscreenEpg();
                if (_topOverlay != null) { _topOverlay.Close(); _topOverlay = null; }

                BottomBar.Visibility = Visibility.Visible;
                
                if (_mpv != null)
                {
                    _mpv.SetWid(VideoPanel.Handle);
                }
                if (_fs != null)
                {
                    _fs.Loaded -= OnFullscreenLoaded;
                    _fs.ExitRequested -= () => ToggleFullscreen(false);
                    try { if (_fsPanel != null) _fsPanel.DoubleClick -= FsPanel_DoubleClick; } catch { }
                    try { if (_fsPanel != null) _fsPanel.MouseWheel -= FsPanel_MouseWheel; } catch { }
                    try { if (_fsPanel != null) _fsPanel.MouseUp -= FsPanel_MouseUp; } catch { }
                    try { _fs.Host.PreviewMouseWheel -= FsHost_PreviewMouseWheel; } catch { }
                    try { _fs.Host.MouseMove -= (s, e) => ShowOverlayWithDelay(); } catch { }
                    try { _fs.Host.PreviewMouseMove -= (s, e) => ShowOverlayWithDelay(); } catch { }
                    try { if (_fsPanel != null) _fsPanel.MouseEnter -= (s, e) => ShowOverlayWithDelay(); } catch { }
                    try
                    {
                        if (_fsKeyFilter != null) System.Windows.Forms.Application.RemoveMessageFilter(_fsKeyFilter);
                        _fsKeyFilter = null;
                    }
                    catch { }
                    
                    // Reset overlay BEFORE closing FS to avoid crash
                    ResetOverlayForOwner(this, false);
                    
                    // Dispose/Close FS window safely
                    try { _fs.Close(); } catch { }
                    _fs = null;
                    _fsPanel = null;
                }
                // 返回窗口模式时隐藏悬浮条（进度/控制条在视频外展示）
                _overlayWpf?.Hide();

                // 强制同步窗口模式下的控件状态
                try 
                { 
                    if (CbTimeshift != null) CbTimeshift.IsChecked = _timeshiftActive; 
                    if (_timeshiftActive) ApplyTimeshiftUi(); // 再次应用时移 UI 到窗口控件
                } 
                catch { }

                try
                {
                    Topmost = true;   // 提升到最前
                    Topmost = false;  // 恢复正常置顶标志
                    Activate();       // 激活窗口
                    Focus();          // 聚焦窗口
                }
                catch { }

                // 恢复定时器
                _timer.Start();
                _overlayPollTimer.Start();
            }
            ShowOverlayWithDelay();
        }
        void ShowFsOverlayNow()
        {
            if (!_isFullscreen || _fs == null || _overlayWpf == null) return;
            try
            {
                PositionOverlay();
                _overlayWpf.Show();
                BringOverlayToTop();
                _overlayHideTimer.Stop();
                _overlayHideTimer.Start();
            }
            catch { }
        }
        void BringOverlayToTop()
        {
            try
            {
                if (_overlayWpf == null) return;
                _overlayWpf.Topmost = true;
                _overlayWpf.Topmost = false;
                _overlayWpf.Topmost = true;
                var h = new System.Windows.Interop.WindowInteropHelper(_overlayWpf).Handle;
                SetWindowPos(h, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
            }
            catch { }
        }
        void FsHost_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            AdjustVolumeByWheel(e.Delta);
            ShowOverlayWithDelay();
            e.Handled = true;
        }
        void FsPanel_MouseWheel(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            AdjustVolumeByWheel(e.Delta);
            ShowOverlayWithDelay();
        }
        void FsPanel_MouseUp(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        var cm = CreateAppMenu();
                        cm.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                        cm.IsOpen = true;
                    });
                }
                catch { }
            }
        }
        void OnFullscreenLoaded(object? sender, RoutedEventArgs e)
        {
            if (!_isFullscreen) return;
            if (_overlayWpf == null) return;
            if (sender is not Window win) return;
            
            // 确保全屏抽屉逻辑
            if (_fsDrawer == null && !_drawerCollapsed)
            {
                ShowFullscreenDrawer();
            }

            // 使用 Loaded 优先级而不是 Idle，避免延迟过长
            win.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_isFullscreen) return;
                if (_overlayWpf == null) return;
                try
                {
                    // 强制断开旧的 Owner，避免句柄冲突
                    _overlayWpf.Owner = null;
                    _overlayWpf.Visibility = Visibility.Collapsed;
                    _overlayWpf.Hide();

                    if (!ReferenceEquals(_overlayWpf.Owner, win))
                    {
                        _overlayWpf.Owner = win;
                    }
                    _overlayWpf.Topmost = true;
                    // 使用 InvalidateVisual 强制刷新
                    _overlayWpf.InvalidateVisual();
                    _overlayWpf.Show();
                    _overlayWpf.Visibility = Visibility.Visible;
                    ShowFsOverlayNow();
                    
                    PositionOverlay();
                    _overlayHideTimer.Stop();
                    _overlayHideTimer.Start();
                    ShowOverlayWithDelay(); // 强制触发一次显示检查

                    // 恢复定时器（在窗口完全加载后）
                    _timer.Start();
                    _overlayPollTimer.Start();
                    
                    // 再次同步一次时移UI，确保 Slider 位置正确
                    if (_timeshiftActive) ApplyTimeshiftUi();
                }
                catch (Exception ex)
                {
                    Logger.Error("Overlay reparent error: " + ex.Message);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        void ShowFullscreenDrawer()
        {
            if (_fsDrawer != null || !_isFullscreen || _fs == null) return;
            try
            {
                // 从主窗口移除 DrawerPanel
                if (DrawerPanel.Parent is System.Windows.Controls.Grid g) g.Children.Remove(DrawerPanel);
                else if (DrawerPanel.Parent is System.Windows.Controls.Panel p) p.Children.Remove(DrawerPanel);

                _fsDrawer = new Window
                {
                    Owner = _fs,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent, // 确保透明背景
                    ShowInTaskbar = false,
                    Topmost = true,
                    Width = _drawerWidth > 0 ? _drawerWidth : 380,
                    Height = _fs.ActualHeight > 0 ? _fs.ActualHeight : SystemParameters.PrimaryScreenHeight,
                    Left = _fs.Left + _fs.ActualWidth - (_drawerWidth > 0 ? _drawerWidth : 380),
                    Top = _fs.Top,
                    Content = DrawerPanel
                };
                // 确保 DrawerPanel 可见
                DrawerPanel.Visibility = Visibility.Visible;
                _fsDrawer.Show();
                _fsDrawer.Hide(); // 初始隐藏，等待鼠标触发
                
                // 强制置顶
                _fsDrawer.Dispatcher.BeginInvoke(new Action(() => 
                {
                    if (_fsDrawer != null)
                    {
                        var handle = new System.Windows.Interop.WindowInteropHelper(_fsDrawer).Handle;
                        SetWindowPos(handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                Logger.Log("ShowFullscreenDrawer error: " + ex.Message);
            }
        }
        void CloseFullscreenDrawer()
        {
            if (_fsDrawer != null)
            {
                try
                {
                    _fsDrawer.Close();
                    _fsDrawer.Content = null; // Detach content
                }
                catch { }
                _fsDrawer = null;
            }
            // 归还 DrawerPanel 到主窗口
            if (DrawerPanel.Parent == null)
            {
                try
                {
                    // 确保挂载到主 Grid，并设置正确的行列
                    // Column 2, Row 1, RowSpan 2
                    var mainGrid = (System.Windows.Controls.Grid)this.Content;
                    if (!mainGrid.Children.Contains(DrawerPanel))
                    {
                        mainGrid.Children.Add(DrawerPanel);
                    }
                    System.Windows.Controls.Grid.SetColumn(DrawerPanel, 2);
                    System.Windows.Controls.Grid.SetRow(DrawerPanel, 1);
                    System.Windows.Controls.Grid.SetRowSpan(DrawerPanel, 2);
                }
                catch (Exception ex)
                {
                    Logger.Log("Drawer restore error: " + ex.Message);
                }
            }
            // 恢复主窗口中的折叠状态
            if (_drawerCollapsed)
            {
                DrawerColumn.Width = new GridLength(0);
            }
            else
            {
                DrawerColumn.Width = new GridLength(_drawerWidth > 0 ? _drawerWidth : 380);
            }
        }
        
        void ShowFullscreenEpg()
        {
            if (_fsEpg != null || !_isFullscreen || _fs == null) return;
            try
            {
                // 确保从任何父容器中移除
                if (EpgPanel.Parent is System.Windows.Controls.Panel p) p.Children.Remove(EpgPanel);
                else if (EpgPanel.Parent is System.Windows.Controls.ContentControl cc) cc.Content = null;
                else if (EpgPanel.Parent is System.Windows.Controls.Decorator d) d.Child = null;
                
                _fsEpg = new Window
                {
                    Owner = _fs, 
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    ShowInTaskbar = false,
                    Topmost = true, // Force topmost
                    Width = 320,
                    Height = _fs.ActualHeight > 0 ? _fs.ActualHeight : SystemParameters.PrimaryScreenHeight,
                    Left = _fs.Left,
                    Top = _fs.Top,
                    Content = EpgPanel
                };
                
                EpgPanel.Visibility = Visibility.Visible;
                _fsEpg.Show();
                _fsEpg.Hide(); // 初始隐藏，等待鼠标触发
                
                // 强制置顶刷新
                _fsEpg.Dispatcher.BeginInvoke(new Action(() => 
                {
                    if (_fsEpg != null)
                    {
                        _fsEpg.Topmost = false;
                        _fsEpg.Topmost = true;
                        _fsEpg.Activate();
                        _fsEpg.Focus();
                        // 确保位置正确
                        try 
                        {
                            if (_fs != null)
                            {
                                _fsEpg.Left = _fs.Left;
                                _fsEpg.Top = _fs.Top;
                            }
                            // 使用 Win32 API 强制置顶 HWND_TOPMOST
                            var handle = new System.Windows.Interop.WindowInteropHelper(_fsEpg).Handle;
                            SetWindowPos(handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040);
                        }
                        catch {}
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                Logger.Error("ShowFullscreenEpg error: " + ex.Message);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        void CloseFullscreenEpg()
        {
            if (_fsEpg != null)
            {
                try 
                { 
                    _fsEpg.Close(); 
                    _fsEpg.Content = null; 
                } 
                catch { }
                _fsEpg = null;
            }
            
            // 归还 EpgPanel
            if (EpgPanel.Parent == null)
            {
                try
                {
                    // Find the main grid. Assuming MainWindow's Content is the Grid.
                    if (this.Content is System.Windows.Controls.Grid mainGrid)
                    {
                        if (!mainGrid.Children.Contains(EpgPanel))
                        {
                            mainGrid.Children.Add(EpgPanel);
                        }
                        // Column 0, Row 1, RowSpan 2
                        System.Windows.Controls.Grid.SetColumn(EpgPanel, 0);
                        System.Windows.Controls.Grid.SetRow(EpgPanel, 1);
                        System.Windows.Controls.Grid.SetRowSpan(EpgPanel, 2);
                    }
                }
                catch { }
            }
            
            // Restore visibility based on toggle state
            bool isEpgOn = CbEpg.IsChecked == true;
            if (isEpgOn)
            {
                EpgPanel.Visibility = Visibility.Visible;
                EpgColumn.Width = new GridLength(320);
            }
            else
            {
                EpgPanel.Visibility = Visibility.Collapsed;
                EpgColumn.Width = new GridLength(0);
            }
        }
        void FsPanel_DoubleClick(object? sender, EventArgs e) => ToggleFullscreen(false);
        string SanitizeUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            var s = input.Trim();
            // 注意：M3UParser.NormalizeUrl 已经处理过 $ 后缀，理论上 Source.Url 应该是干净的。
            // 但为了双重保险（比如来自其他途径的 URL），我们这里再次清理。
            // 必须先移除 $ 后缀，再 Trim
            var idx = s.IndexOf('$');
            if (idx > 0) s = s.Substring(0, idx);
            s = s.Trim().TrimEnd(',');
            
            // 重要：rtp2httpd 的 URL 中包含中文路径（如 /央视频道/），需要确保这些中文被正确编码
            // 但不能对整个 URL 进行编码，否则协议头（如 http://）和参数分隔符（如 ? &）也会被编码导致无法识别
            // 最好的方式是保持原样传递给 MPV，因为 MPV 能够处理未编码的 Unicode URL。
            // 如果之前有其他逻辑对 URL 进行了错误的编码或解码，这里需要修正。
            
            // 目前看起来 SanitizeUrl 只是去掉了后缀，这很好。
            // 但是，我们在 PlayChannel 中调用了 SanitizeUrl，而之前的日志显示 URL 是明文中文：
            // https://hntv.mtoo.vip:8444/央视频道/CCTV8-电视剧/组播标清-25.00fps
            // 而回放的 URL 是被编码过的：
            // https://hntv.mtoo.vip:8444/%E5%A4%AE%E8%A7%86%E9%A2%91%E9%81%93/...
            // 这可能是问题的关键：MPV 可能需要（或者在某些网络环境下更喜欢）编码后的 URL。
            
            // 尝试对路径部分进行编码
            try
            {
                if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !s.Contains("%"))
                {
                    // 只有当 URL 包含非 ASCII 字符且尚未编码时才尝试处理
                    bool hasNonAscii = s.Any(c => c > 127);
                    if (hasNonAscii)
                    {
                        var uri = new Uri(s);
                        return uri.AbsoluteUri; // AbsoluteUri 会自动对路径中的中文进行编码
                    }
                }
            }
            catch { }

            return s;
        }
        void ToggleDebugWindow()
        {
            try
            {
                if (_debug != null && _debug.IsVisible)
                {
                    _debug.Close();
                    return;
                }
                _debug = new DebugWindow();
                _debug.Owner = this;
                _debug.Show();
            }
            catch { }
        }
        void BtnDebug_Click(object sender, RoutedEventArgs e) => ToggleDebugWindow();
        void OnSourceTimeout(object? sender, EventArgs e)
        {
            _sourceTimeoutTimer.Stop();
            if (_mpv == null || _currentChannel == null || _currentSources == null || _currentSources.Count <= 1) return;

            // 检查当前是否已经播放成功（根据时间位置判断）
            var t = _mpv.GetTimePos();
            if (t.HasValue && t.Value > 0) return; // 已经播放中

            // 尝试切换到下一个源
            var currentUrl = SanitizeUrl(_currentChannel.Tag is Source src ? src.Url : "");
            var idx = _currentSources.FindIndex(s => SanitizeUrl(s.Url) == currentUrl);
            
            if (idx >= 0 && idx < _currentSources.Count - 1)
            {
                var nextSource = _currentSources[idx + 1];
                Logger.Log($"源超时 ({AppSettings.Current.SourceTimeoutSec}s)，自动切换到下一源: {nextSource.Url}");
                
                // 更新当前频道的Tag为新源，以便 PlayChannel 使用
                _currentChannel.Tag = nextSource;
                PlayChannel(_currentChannel);
            }
        }
        void OnTick(object? sender, EventArgs e)
        {
            // 在时移模式下，不允许此超时监控例程改动进度条和标签，避免与时移逻辑竞态导致不同步
            if (_timeshiftActive)
            {
                // 仅做 EPG 同步（按播放时刻），不改动底部进度条
                try
                {
                    if (AppSettings.Current.Epg.StrictMatchByPlaybackTime && EpgPanel.Visibility == Visibility.Visible && _currentChannel != null)
                    {
                        var t = GetPlaybackLocalTime();
                        if (t.HasValue) RefreshEpgList(_currentChannel, t.Value);
                    }
                }
                catch { }
                return;
            }
            if (_mpv == null) return;
            var tpos = _mpv.GetTimePos();
            if (tpos.HasValue && tpos.Value > 0) _sourceTimeoutTimer.Stop(); // 有进度，说明播放成功
            var d = _mpv.GetDuration();
            if (d.HasValue && d.Value > 0)
            {
                if (!_seeking)
                {
                    SliderSeek.Maximum = d.Value;
                    SliderSeek.Value = tpos ?? 0;
                }
                LblElapsed.Text = FormatTime(tpos ?? 0);
                LblDuration.Text = FormatTime(d.Value);
                _overlayWpf?.SetTime(tpos ?? 0, d.Value);
            }
            // 回放模式：按播放时刻同步 EPG（跨节目边界时更新）
            try
            {
                if (_currentPlayingProgram != null && AppSettings.Current.Epg.StrictMatchByPlaybackTime && EpgPanel.Visibility == Visibility.Visible && _currentChannel != null)
                {
                    var tt = GetPlaybackLocalTime();
                    if (tt.HasValue) RefreshEpgList(_currentChannel, tt.Value);
                }
            }
            catch { }
            // 更新播放信息状态行（英文标签芯片）
            try
            {
                var w = _mpv.GetInt("width") ?? 0;
                var h = _mpv.GetInt("height") ?? 0;
                var fps = _mpv.GetDouble("estimated-vf-fps") ?? _mpv.GetDouble("fps");
                var hw = _mpv.GetString("hwdec-current");
                var vcodec = _mpv.GetString("video-codec");
                var acodec = _mpv.GetString("audio-codec");
                // 码率优先使用 video-params/bitrate（kbit/s），其次 demuxer-bitrate/video-bitrate（单位不稳定）
                var brKbit = _mpv.GetDouble("video-params/bitrate");
                double? brRaw = _mpv.GetDouble("demuxer-bitrate") ?? _mpv.GetDouble("video-bitrate");
                string brStr = "-";
                if (brKbit.HasValue && brKbit.Value > 0)
                {
                    var mb = brKbit.Value / 8000.0; // kbit/s -> MB/s
                    brStr = $"{mb:0.0}MB/s";
                }
                else if (brRaw.HasValue)
                {
                    double v = brRaw.Value;
                    double mbps;
                    if (v < 900)
                    {
                        // MB/s
                        mbps = v;
                    }
                    else if (v < 9000)
                    {
                        // kB/s -> MB/s
                        mbps = v / 1000.0;
                    }
                    else if (v < 2_000_000)
                    {
                        // kbit/s -> MB/s
                        mbps = v / 8000.0;
                    }
                    else
                    {
                        // bit/s -> MB/s
                        mbps = v / 8_000_000.0;
                    }
                    if (double.IsFinite(mbps) && mbps > 0)
                    {
                        if (mbps > 500) // 异常兜底
                        {
                            mbps = v / 8_000_000.0;
                        }
                        brStr = $"{mbps:0.0}MB/s";
                    }
                    else
                    {
                        brStr = "-";
                    }
                }
                var tags = new List<string>();
                tags.Add(string.IsNullOrEmpty(hw) ? "SW" : "HW");
                if (!string.IsNullOrWhiteSpace(vcodec))
                {
                    var up = vcodec.ToUpperInvariant();
                    if (up.Contains("HEVC") || up.Contains("H265")) tags.Add("HEVC");
                    else if (up.Contains("H264") || up.Contains("AVC")) tags.Add("H.264");
                    else tags.Add(up);
                }
                if (!string.IsNullOrWhiteSpace(acodec))
                {
                    var up = acodec.ToUpperInvariant();
                    if (up.Contains("E-AC-3") || up.Contains("EAC3")) tags.Add("EAC3");
                    else if (up.Contains("AC-3") || up.Contains("AC3")) tags.Add("AC3");
                    else if (up.Contains("MP3") || up.Contains("MPEG AUDIO LAYER 3")) tags.Add("MP3");
                    else if (up.Contains("MP2") || up.Contains("MPEG AUDIO LAYER 2") || up.Contains("MPEG LAYER II")) tags.Add("MP2");
                    else if (up.Contains("AAC")) tags.Add("AAC");
                    else if (up.Contains("WMA")) tags.Add("WMA");
                    else if (up.Contains("FLAC")) tags.Add("FLAC");
                    else if (up.Contains("OPUS")) tags.Add("OPUS");
                    else if (up.Contains("VORBIS")) tags.Add("VORBIS");
                    else if (up.Contains("PCM")) tags.Add("PCM");
                    else if (up.Contains("DTS")) tags.Add("DTS");
                    else if (up.Contains("TRUEHD")) tags.Add("TRUEHD");
                    else tags.Add(up);
                }
                if (h >= 2160) tags.Add("4K");
                else if (h >= 1080) tags.Add("FHD");
                else if (h >= 720) tags.Add("HD");
                else if (h > 0) tags.Add("SD");
                if (fps.HasValue && fps.Value > 0) tags.Add($"{fps.Value:0.##}fps");
                tags.Add(brStr);
                UpdateTagPanel(tags);
                var info = string.Join("  ", tags);
                if (_overlayWpf != null)
                {
                    _overlayWpf.SetInfo(info);
                    _overlayWpf.SetTags(tags);
                }
            }
            catch { }
        }
        void UpdateTagPanel(List<string> tags)
        {
            try
            {
                TagPanel.Children.Clear();
                var style = TagPanel.TryFindResource("TagChip") as System.Windows.Style;
                var brush = TagPanel.TryFindResource("TextPrimaryBrush") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Gray;
                foreach (var t in tags)
                {
                    var border = new System.Windows.Controls.Border
                    {
                        Style = style
                    };
                    var tb = new System.Windows.Controls.TextBlock
                    {
                        Text = t,
                        Foreground = brush,
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    border.Child = tb;
                    TagPanel.Children.Add(border);
                }
            }
            catch { }
        }
        string FormatTime(double sec)
        {
            if (sec < 0) sec = 0;
            var ts = TimeSpan.FromSeconds(sec);
            return ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
        }
        void SliderSeek_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _seeking = true;
            try
            {
                var p = e.GetPosition(SliderSeek);
                var ratio = Math.Max(0, Math.Min(1, SliderSeek.ActualWidth > 0 ? p.X / SliderSeek.ActualWidth : 0));
                var sec = ratio * (SliderSeek.Maximum - SliderSeek.Minimum) + SliderSeek.Minimum;
                SliderSeek.Value = sec;
            }
            catch { }
        }
        void SliderSeek_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_mpv == null) return;
            _seeking = false;
            var v = SliderSeek.Value;
            if (_timeshiftActive && _currentChannel != null)
            {
                _timeshiftCursorSec = Math.Max(0, v);
                var t = _timeshiftMin.AddSeconds(_timeshiftCursorSec);
                PlayCatchupAt(_currentChannel, t);
                _timeshiftStart = t;
            }
            else
            {
                _mpv.SeekAbsolute(v);
            }
        }
        void SliderSeek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_seeking)
            {
                if (_timeshiftActive)
                {
                    var t = _timeshiftMin.AddSeconds(Math.Max(0, SliderSeek.Value));
                    LblElapsed.Text = t.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    LblElapsed.Text = FormatTime(SliderSeek.Value);
                }
            }
        }
        void SliderSeek_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                var p = e.GetPosition(SliderSeek);
                var ratio = Math.Max(0, Math.Min(1, SliderSeek.ActualWidth > 0 ? p.X / SliderSeek.ActualWidth : 0));
                var sec = ratio * (SliderSeek.Maximum - SliderSeek.Minimum) + SliderSeek.Minimum;
                if (_timeshiftActive)
                {
                    var t = _timeshiftMin.AddSeconds(Math.Max(0, sec));
                    SliderSeek.ToolTip = t.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    // 回看或普通播放：若有时移起点，则显示起点+sec，否则显示相对时间
                    var tip = _currentPlayingProgram != null
                        ? _currentPlayingProgram.Start.AddSeconds(Math.Max(0, sec)).ToString("yyyy-MM-dd HH:mm:ss")
                        : FormatTime(sec);
                    SliderSeek.ToolTip = tip;
                }
            }
            catch { }
        }
        void SliderSeek_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try { SliderSeek.ClearValue(ToolTipProperty); } catch { }
        }
        void ClearIndicatorsOnStop()
        {
            try
            {
                ClearLiveAndEpgIndicators();
            }
            catch { }
            try
            {
                ClearRecordingPlayingIndicator();
            }
            catch { }
            try
            {
                _timeshiftActive = false;
                try { _overlayWpf?.SetTimeshift(false); } catch { }
                try { if (CbTimeshift != null) CbTimeshift.IsChecked = false; } catch { }
                try
                {
                    _playbackSpeed = 1.0;
                    _mpv?.SetSpeed(1.0);
                    _overlayWpf?.SetSpeed(1.0);
                    _overlayWpf?.SetSpeedEnabled(false);
                    if (BtnSpeed != null) { BtnSpeed.IsEnabled = false; LblSpeedBar.Text = "1.0x"; }
                }
                catch { }
            }
            catch { }
        }
        void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_mpv == null) return;
            _paused = !_paused;
            _mpv.Pause(_paused);
            UpdatePlayPauseIcon();
        }
        void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_mpv == null) return;
            _mpv.Stop();
            _paused = false;
            UpdatePlayPauseIcon();
            ClearIndicatorsOnStop();
            
            // Show placeholder and Hide VideoHost (Airspace workaround)
            try 
            { 
                PlaceholderPanel.Visibility = Visibility.Visible; 
                VideoHost.Visibility = Visibility.Collapsed;
            } 
            catch { }
        }
        void BtnRew_Click(object sender, RoutedEventArgs e)
        {
            if (_mpv == null) return;
            _mpv.SeekRelative(-10);
        }
        void BtnFwd_Click(object sender, RoutedEventArgs e)
        {
            if (_mpv == null) return;
            _mpv.SeekRelative(10);
        }
        void SliderVolume_VolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetVolumeInternal(e.NewValue);
        }
        void CbFullscreen_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = false;
            if (sender is System.Windows.Controls.Primitives.ToggleButton tb)
            {
                isChecked = tb.IsChecked == true;
            }
            ToggleFullscreen(isChecked);
        }
        void CbDrawer_Click(object sender, RoutedEventArgs e)
        {
            var visible = CbDrawer.IsChecked == true;
            SetDrawerCollapsed(!visible);
        }
        bool _recordingNow = false;
        string _recordFilePath = "";
        DateTime? _recordStartUtc = null;
        DateTime? _recordFirstWriteUtc = null;
        System.Threading.CancellationTokenSource? _recordCts;
        System.Threading.Tasks.Task? _recordTask;
        FileSystemWatcher? _recordingsWatcher;
        bool _recordingsRefreshPending = false;
        System.Windows.Threading.DispatcherTimer? _recordingsRefreshTimer;
        bool _recordViaHttp = false;
        List<LibmpvIptvClient.Services.RecordingGroup> _recordingsGroupsAll = new List<LibmpvIptvClient.Services.RecordingGroup>();
        List<LibmpvIptvClient.Services.RecordingEntry> _recordingsFlatAll = new List<LibmpvIptvClient.Services.RecordingEntry>();
        System.Windows.Threading.DispatcherTimer? _recordReplayEofTimer;
        LibmpvIptvClient.Services.RecordingEntry? _currentRecordingPlaying;
        void EnsureMpvReadyForLoad()
        {
            try
            {
                _mpv?.Pause(false);
                var eof = _mpv?.GetString("eof-reached");
                if (string.Equals(eof ?? "", "yes", StringComparison.OrdinalIgnoreCase))
                {
                    _mpv?.Stop();
                    System.Threading.Thread.Sleep(80);
                }
            }
            catch { }
        }
        void ClearRecordingPlayingIndicator()
        {
            try
            {
                if (_currentRecordingPlaying != null)
                {
                    _currentRecordingPlaying.IsPlaying = false;
                    _currentRecordingPlaying = null;
                }
            }
            catch { }
        }
        void StopRecordReplayMonitor()
        {
            try { _recordReplayEofTimer?.Stop(); _recordReplayEofTimer = null; } catch { }
        }
        void StartRecordReplayMonitor()
        {
            try
            {
                StopRecordReplayMonitor();
                _recordReplayEofTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
                _recordReplayEofTimer.Tick += (s, e) =>
                {
                    try
                    {
                        var eof = _mpv?.GetString("eof-reached");
                        if (string.Equals(eof ?? "", "yes", StringComparison.OrdinalIgnoreCase))
                        {
                            StopRecordReplayMonitor();
                            // 仅复位播放器，不切回直播，保持录播项的播放标记
                            EnsureMpvReadyForLoad();
                        }
                    }
                    catch { }
                };
                _recordReplayEofTimer.Start();
            }
            catch { }
        }
        void ClearLiveAndEpgIndicators()
        {
            try
            {
                if (_channels != null)
                {
                    foreach (var c in _channels) if (c != null) c.Playing = false;
                }
                if (_currentPlayingProgram != null)
                {
                    _currentPlayingProgram.IsPlaying = false;
                    _currentPlayingProgram = null;
                    try { if (ListEpg?.Items != null) ListEpg.Items.Refresh(); } catch { }
                }
            }
            catch { }
        }
        string ResolveProgramTitleNow()
        {
            try
            {
                var now = DateTime.Now;
                if (_currentChannel != null)
                {
                    var progs = _epgService?.GetPrograms(_currentChannel.TvgId, _currentChannel.Name);
                    var p = progs?.FirstOrDefault(x => x.Start <= now && x.End > now);
                    if (p != null && !string.IsNullOrWhiteSpace(p.Title)) return p.Title!;
                }
            }
            catch { }
            return "";
        }
        async System.Threading.Tasks.Task ResetAfterRecordingStopAsync()
        {
            try
            {
                try { _mpv.SetOptionString("stream-record", ""); } catch { }
                try { _mpv.SetPropertyString("stream-record", ""); } catch { }
                try { _mpv.SetPropertyString("record-file", ""); } catch { }
                try { _mpv.SetPropertyString("ab-loop-a", "no"); } catch { }
                try { _mpv.SetPropertyString("ab-loop-b", "no"); } catch { }
                try { _mpv.Pause(false); } catch { }
                try { await System.Threading.Tasks.Task.Delay(120); } catch { }
                try { PlaceholderPanel.Visibility = Visibility.Collapsed; VideoHost.Visibility = Visibility.Visible; } catch { }
            }
            catch { }
        }
        void TxtRecordingsSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try { ApplyRecordingsFilter(); } catch { }
        }
        void BtnRecordingsSearchClear_Click(object sender, RoutedEventArgs e)
        {
            try { TxtRecordingsSearch.Text = ""; } catch { }
        }
        void BtnChannelsRefresh_Click(object sender, RoutedEventArgs e)
        {
            try { ApplyChannelFilter(); } catch { }
        }
        void ApplyRecordingsFilter()
        {
            try
            {
                var q = (TxtRecordingsSearch?.Text ?? "").Trim();
                bool hasQ = !string.IsNullOrWhiteSpace(q);
                Func<string, bool> match = s => string.IsNullOrWhiteSpace(q) ? true :
                    (s?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
                // 过滤分组
                var groups = new List<LibmpvIptvClient.Services.RecordingGroup>();
                int total = 0;
                foreach (var g in _recordingsGroupsAll)
                {
                    var items = hasQ ? g.Items.Where(it => match(it.Title) || match(it.PathOrUrl)).ToList() : g.Items.ToList();
                    if (items.Count > 0 || !hasQ)
                    {
                        groups.Add(new LibmpvIptvClient.Services.RecordingGroup { Channel = g.Channel, Items = items });
                        total += items.Count;
                    }
                }
                ListRecordingsGrouped.ItemsSource = groups;
                // 扁平列表隐藏，不再使用
                try { ListRecordings.ItemsSource = null; } catch { }
                try { TxtRecordingsCount.Text = (LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_Records_Count", "共")) + $" {total} " + (LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_Records_Unit", "个录播")); } catch { TxtRecordingsCount.Text = $"共 {total} 个录播"; }
            }
            catch { }
        }
        readonly System.Collections.Generic.HashSet<string> _recordingsExpanded = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        void RecordingGroup_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Expander ex && ex.DataContext is LibmpvIptvClient.Services.RecordingGroup g)
                {
                    ex.IsExpanded = _recordingsExpanded.Contains(g.Channel ?? "");
                }
            }
            catch { }
        }
        void RecordingGroup_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Expander ex && ex.DataContext is LibmpvIptvClient.Services.RecordingGroup g)
                {
                    _recordingsExpanded.Add(g.Channel ?? "");
                }
            }
            catch { }
        }
        void RecordingGroup_Collapsed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Expander ex && ex.DataContext is LibmpvIptvClient.Services.RecordingGroup g)
                {
                    _recordingsExpanded.Remove(g.Channel ?? "");
                }
            }
            catch { }
        }
        async void BtnRecordNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mpv == null || _currentChannel == null) return;
                if (!_recordingNow)
                {
                var recCfgGate = AppSettings.Current?.Recording?.Enabled ?? true;
                if (!recCfgGate)
                {
                    try { BtnRecordNow.IsChecked = false; } catch { }
                    try { BtnRecordNow.ToolTip = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Overlay_RecordNow", "实时录播"); } catch { }
                    try { LibmpvIptvClient.Services.NotificationService.Instance.Show(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Overlay_RecordNow", "实时录播"), LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_Record_Disabled", "已关闭录播"), 4000); } catch { }
                    return;
                }
                var key = ResolveChannelKey();
                    var safe = SanName(string.IsNullOrWhiteSpace(key) ? "unknown" : key);
                    var dir = ResolveRecordingDir(safe);
                    var verify = AppSettings.Current?.Recording?.VerifyDirReady ?? true;
                    var dirReady = verify ? await EnsureRecordingDirReady(dir) : true;
                    if (!dirReady)
                    {
                        try { LibmpvIptvClient.Diagnostics.Logger.Warn("[Record] dir not ready " + dir); } catch { }
                        return;
                    }
                    var name = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".ts";
                    _recordFilePath = System.IO.Path.Combine(dir, name).Replace("\\", "/"); // Normalize path
                    _recordStartUtc = DateTime.UtcNow;
                    _recordFirstWriteUtc = null;
                    try { LibmpvIptvClient.Diagnostics.Logger.Info("[Record] start " + _recordFilePath); } catch { }
                    TryStartStreamRecord(_recordFilePath);
                    _recordingNow = true;
                    try { StartRealtimeUploadIfEnabled(_recordFilePath, safe, System.IO.Path.GetFileName(_recordFilePath)); } catch { }
                    try { BtnRecordNow.IsChecked = true; BtnRecordNow.ToolTip = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Overlay_RecordStop", "停止录播"); } catch { }
                    try
                    {
                        var ch = _currentChannel?.Name ?? "SrcBox";
                        var prog = _currentPlayingProgram?.Title;
                        if (string.IsNullOrWhiteSpace(prog)) prog = ResolveProgramTitleNow();
                        var logoPath = _currentChannel?.Logo;
                        var status = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_Record_Start", "开始录播");
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            try
                            {
                            LibmpvIptvClient.Services.ToastService.ShowSimple(LibmpvIptvClient.Services.ToastKind.RecordStart, ch, status, logoPath, 3000);
                            }
                            catch
                            {
                                LibmpvIptvClient.Services.NotificationService.Instance.ShowWithLogo(ch, status, DateTime.Now, logoPath, 5000);
                            }
                        });
                    }
                    catch { }
                }
                else
                {
                    // 停止录播并清理所有副作用，确保播放流程恢复
                    try { _mpv.SetOptionString("stream-record", ""); } catch { }
                    try { _mpv.SetPropertyString("stream-record", ""); } catch { }
                    try { _mpv.SetPropertyString("record-file", ""); } catch { }
                    try { _mpv.SetPropertyString("ab-loop-a", "no"); } catch { }
                    try { _mpv.SetPropertyString("ab-loop-b", "no"); } catch { }
                    try { _mpv.Pause(false); } catch { }
                    try { _recordCts?.Cancel(); } catch { }
                    try { await System.Threading.Tasks.Task.Delay(150); } catch { }
                    _recordingNow = false;
                    _recordViaHttp = false;
                    try { BtnRecordNow.IsChecked = false; BtnRecordNow.ToolTip = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Overlay_RecordNow", "实时录播"); } catch { }
                    try { LibmpvIptvClient.Diagnostics.Logger.Info("[Record] stop " + _recordFilePath); } catch { }
                    string? logoPathStop = _currentChannel?.Logo;
                    try
                    {
                            var ch = _currentChannel?.Name ?? "SrcBox";
                            var prog = _currentPlayingProgram?.Title;
                            if (string.IsNullOrWhiteSpace(prog)) prog = ResolveProgramTitleNow();
                            var status = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Msg_Record_Stop", "录播已停止");
                            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                try
                                {
                            LibmpvIptvClient.Services.ToastService.ShowSimple(LibmpvIptvClient.Services.ToastKind.RecordStop, ch, status, logoPathStop, 3000);
                                }
                                catch
                                {
                                    LibmpvIptvClient.Services.NotificationService.Instance.ShowWithLogo(ch, status, DateTime.Now, logoPathStop, 5000);
                                }
                            });
                    }
                    catch { }
                    try { WriteRecordingMeta(_recordFilePath, _recordStartUtc, DateTime.UtcNow); } catch { }
                    TryUploadRecording(_recordFilePath, logoPathStop);
                    try 
                    { 
                        var k = ResolveChannelKey() ?? ""; 
                        ScheduleRecordingsRefresh(k); 
                    } 
                    catch { }
                    // 如仍卡帧，强制刷新当前直播流
                    try
                    {
                        await ResetAfterRecordingStopAsync();
                    }
                    catch { }
                    _recordFilePath = "";
                    _recordStartUtc = null;
                }
            }
            catch { }
        }
        void StartRealtimeUploadIfEnabled(string localPath, string safeChannel, string fileName)
        {
            try
            {
                var recCfg = AppSettings.Current?.Recording ?? new RecordingConfig();
                if (!string.Equals(recCfg.SaveMode ?? "", "dual_realtime", StringComparison.OrdinalIgnoreCase)) return;
                var wd = AppSettings.Current.WebDav;
                if (wd == null || !wd.Enabled || string.IsNullOrWhiteSpace(wd.BaseUrl)) return;
                _recordCts = new System.Threading.CancellationTokenSource();
                var ct = _recordCts.Token;
                var interval = Math.Max(2, recCfg.RealtimeUploadIntervalSec);
                var tempSuffix = string.IsNullOrWhiteSpace(recCfg.RemoteTempSuffix) ? ".part" : recCfg.RemoteTempSuffix;
                var remoteDir = (wd.RecordingsPath ?? "/srcbox/recordings/").TrimEnd('/') + "/" + safeChannel + "/";
                var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                _recordTask = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await cli.EnsureCollectionAsync(remoteDir);
                        var tempUrl = cli.Combine(remoteDir + fileName + tempSuffix);
                        try { LibmpvIptvClient.Diagnostics.Logger.Info($"[RealtimeUpload] begin url={tempUrl} interval={interval}s"); } catch { }
                        while (!ct.IsCancellationRequested && _recordingNow)
                        {
                            try
                            {
                                var ok = await cli.PutFileAsync(tempUrl, localPath, "video/MP2T", Math.Max(0, recCfg.UploadMaxKBps), ct);
                                try { LibmpvIptvClient.Diagnostics.Logger.Info($"[RealtimeUpload] push {(ok ? "ok" : "fail")} -> {tempUrl}"); } catch { }
                            }
                            catch { }
                            for (int i = 0; i < interval * 10 && !ct.IsCancellationRequested && _recordingNow; i++)
                            {
                                await System.Threading.Tasks.Task.Delay(100, ct);
                            }
                        }
                        try { LibmpvIptvClient.Diagnostics.Logger.Info("[RealtimeUpload] end"); } catch { }
                    }
                    catch { }
                }, ct);
            }
            catch { }
        }
        async void TryStartStreamRecord(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                path = path.Replace("\\", "/");
                LibmpvIptvClient.Diagnostics.Logger.Info($"[Record] Attempting to start record to: {path}");
                try
                {
                    var pd = System.IO.Path.GetDirectoryName(path) ?? "";
                    await EnsureRecordingDirReady(pd);
                }
                catch { }
                
                // 方案A: 尝试 stream-record 属性
                try 
                { 
                    // 确保缓存已开启，这对 stream-record 和 dump-cache 至关重要
                    // 注意：在播放中途修改 cache 属性可能需要重新加载，这里仅作尝试
                    _mpv.SetPropertyString("cache", "yes");
                    _mpv.SetPropertyString("demuxer-max-bytes", "512MiB"); 
                    _mpv.SetPropertyString("demuxer-max-back-bytes", "256MiB");
                    _mpv.SetPropertyString("force-seekable", "yes");

                    _mpv.SetPropertyString("stream-record", path);
                    LibmpvIptvClient.Diagnostics.Logger.Info("[Record] Set stream-record property (path)");
                    
                    // 诊断：检查缓存状态
                    try 
                    {
                        var cacheSize = 0L;
                        if (long.TryParse(_mpv.GetString("demuxer-cache-state/total-bytes"), out var s)) cacheSize = s;
                        var cacheDuration = _mpv.GetString("demuxer-cache-duration");
                        var cacheTime = _mpv.GetString("demuxer-cache-time");
                        var cacheState = _mpv.GetString("demuxer-cache-state/fw-bytes");
                        
                        LibmpvIptvClient.Diagnostics.Logger.Info($"[Record] Cache Diag - Size: {cacheSize} bytes, Duration: {cacheDuration}, Time: {cacheTime}, FwBytes: {cacheState}");
                        
                        // 即使 cacheSize 为 0，如果 duration > 0，说明 back-buffer 可能有数据
                        // 或者数据在内部队列中。如果 stream-record 失败，我们需要尝试 dump-cache。
                        
                        // 验证 stream-record 是否生效
                        var currentRecordPath = _mpv.GetString("stream-record");
                        LibmpvIptvClient.Diagnostics.Logger.Info($"[Record] stream-record property check: '{currentRecordPath}'");
                        
                        // 即使 cacheSize 为 0，如果 duration > 0，说明 back-buffer 可能有数据
                        // 增加更详细的诊断
                        if (cacheSize == 0)
                        {
                            LibmpvIptvClient.Diagnostics.Logger.Warn("[Record] Cache size is 0! Checking stream details...");
                            // 尝试获取更多流信息
                            var demuxerStart = _mpv.GetString("demuxer-cache-state/seekable-start");
                            var demuxerEnd = _mpv.GetString("demuxer-cache-state/seekable-end");
                            LibmpvIptvClient.Diagnostics.Logger.Info($"[Record] Seekable Range: {demuxerStart} - {demuxerEnd}");
                        }

                        if (string.IsNullOrEmpty(currentRecordPath) || cacheSize == 0)
                        {
                            LibmpvIptvClient.Diagnostics.Logger.Warn("[Record] Cache empty or stream-record rejected. Triggering fallback chain...");
                            
                            // 1. 尝试 record-file (如果 stream-record 失败)
                            if (string.IsNullOrEmpty(currentRecordPath))
                            {
                                try 
                                {
                                    _mpv.SetPropertyString("record-file", path); 
                                    LibmpvIptvClient.Diagnostics.Logger.Info("[Record] Set record-file property");
                                } catch {}
                            }
                            
                            // 2. 如果还是没反应，且缓存为0但有时长，尝试 dump-cache
                            // 注意：dump-cache 需要具体的范围，如果 start 默认为 0 可能无效
                            if (cacheSize == 0)
                            {
                                // 尝试设置 ab-loop 来辅助 dump-cache
                                _mpv.SetPropertyString("ab-loop-a", "0");
                                _mpv.SetPropertyString("ab-loop-b", "no");
                            }
                        }
                    }
                    catch { }
                } 
                catch (Exception ex)
                {
                    LibmpvIptvClient.Diagnostics.Logger.Error($"[Record] Failed to set stream-record: {ex.Message}");
                }
                
                await System.Threading.Tasks.Task.Delay(1000);
                
                // 验证文件是否创建，若未创建则尝试方案B: dump-cache
                if (!System.IO.File.Exists(path))
                {
                    LibmpvIptvClient.Diagnostics.Logger.Warn("[Record] File not created via stream-record, trying dump-cache...");
                    try 
                    {
                        // 使用 run 命令调用 dump-cache，参数: start, end, path
                        // 注意：对于直播流，start=0 可能意味着从当前播放点开始，这可能导致之前的缓存被忽略
                        // 尝试转储所有可用缓存：start=-36000 (10小时前), end=no (直到流结束)
                        // 注意：Mpv.NET 或底层库可能需要通过 command 数组传递
                        
                        // 方法1: 尝试 SetProperty 触发 dump-cache（某些版本不支持直接 command）
                        // 这里回退到尝试 file URI 
                        var fileUri = new Uri(path, UriKind.Absolute).AbsoluteUri;
                        LibmpvIptvClient.Diagnostics.Logger.Info($"[Record] Retry with file URI: {fileUri}");
                        
                        // 尝试使用 ab-loop 标记缓存范围
                        _mpv.SetPropertyString("ab-loop-a", "0"); // 从头开始
                        _mpv.SetPropertyString("ab-loop-b", "no"); // 到尾结束
                        
                        // 再次尝试 stream-record (现在有了 ab-loop 范围)
                        // _mpv.SetPropertyString("stream-record", fileUri);
                        
                        // 尝试显式 dump-cache 命令
                        _mpv.Command("dump-cache", "0", "no", path);
                        LibmpvIptvClient.Diagnostics.Logger.Info("[Record] Invoked dump-cache command");
                    } 
                    catch (Exception ex)
                    {
                         LibmpvIptvClient.Diagnostics.Logger.Error($"[Record] Retry failed: {ex.Message}");
                    }
                }
                else
                {
                     LibmpvIptvClient.Diagnostics.Logger.Info("[Record] File created successfully via stream-record");
                }

                var grew = await MonitorRecordingGrowth();
                if (!grew)
                {
                    var retries = Math.Max(0, AppSettings.Current?.Recording?.RetryCount ?? 1);
                    for (int attempt = 1; attempt <= retries && !_recordingNow; attempt++) { }
                    for (int attempt = 1; attempt <= retries && _recordingNow; attempt++)
                    {
                        LibmpvIptvClient.Diagnostics.Logger.Warn("[Record] auto-retry begin");
                        try { _mpv.SetPropertyString("stream-record", ""); } catch { }
                        try { _mpv.SetPropertyString("record-file", ""); } catch { }
                        try { await System.Threading.Tasks.Task.Delay(200); } catch { }
                        try
                        {
                            if (System.IO.File.Exists(_recordFilePath))
                            {
                                var fi = new System.IO.FileInfo(_recordFilePath);
                                if (fi.Length == 0) System.IO.File.Delete(_recordFilePath);
                            }
                        }
                        catch { }
                        var dir2 = System.IO.Path.GetDirectoryName(path) ?? "";
                        var verify = AppSettings.Current?.Recording?.VerifyDirReady ?? true;
                        var ok = verify ? await EnsureRecordingDirReady(dir2) : true;
                        if (ok)
                        {
                            var name2 = DateTime.Now.ToString("yyyyMMdd_HHmmss") + $"_r{attempt}.ts";
                            _recordFilePath = System.IO.Path.Combine(dir2, name2).Replace("\\", "/");
                            _recordFirstWriteUtc = null;
                            try { _mpv.SetPropertyString("stream-record", _recordFilePath); } catch { }
                            var grew2 = await MonitorRecordingGrowth();
                            if (grew2) break;
                            if (attempt == retries) LibmpvIptvClient.Diagnostics.Logger.Warn("[Record] auto-retry failed");
                        }
                        else
                        {
                            LibmpvIptvClient.Diagnostics.Logger.Warn("[Record] dir not ready on retry");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 LibmpvIptvClient.Diagnostics.Logger.Error($"[Record] TryStartStreamRecord Exception: {ex}");
            }
        }
        async System.Threading.Tasks.Task<bool> MonitorRecordingGrowth()
        {
            try
            {
                LibmpvIptvClient.Diagnostics.Logger.Info("[Record] Starting growth monitor...");
                // 持续检测直到文件开始增长或录制停止
                var secs = Math.Max(1, AppSettings.Current?.Recording?.GrowthTimeoutSec ?? 20);
                var loops = Math.Max(1, (int)Math.Ceiling(secs * 1000.0 / 500.0));
                for (int i = 0; i < loops; i++)
                {
                    if (!_recordingNow) 
                    {
                        LibmpvIptvClient.Diagnostics.Logger.Info("[Record] Monitor stopped (recording stopped)");
                        return false;
                    }
                    await System.Threading.Tasks.Task.Delay(500);
                    try
                    {
                        if (System.IO.File.Exists(_recordFilePath))
                        {
                            var info = new System.IO.FileInfo(_recordFilePath);
                            if (info.Length > 0)
                            {
                                if (!_recordFirstWriteUtc.HasValue) 
                                {
                                    _recordFirstWriteUtc = DateTime.UtcNow;
                                    LibmpvIptvClient.Diagnostics.Logger.Info($"[Record] File started growing at {_recordFirstWriteUtc}, size: {info.Length}");
                                }
                                return true;
                            }
                        }
                        else
                        {
                             LibmpvIptvClient.Diagnostics.Logger.Info($"[Record] File not found yet: {_recordFilePath}");
                        }
                    }
                    catch { }
                }
                LibmpvIptvClient.Diagnostics.Logger.Warn("[Record] Monitor timeout: File did not grow after 20s");
                return false;
            }
            catch (Exception ex)
            {
                LibmpvIptvClient.Diagnostics.Logger.Error($"[Record] Monitor Exception: {ex}");
                return false;
            }
        }
        async System.Threading.Tasks.Task<bool> EnsureRecordingDirReady(string dir)
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    try { System.IO.Directory.CreateDirectory(dir); } catch { }
                    var ok = false;
                    try
                    {
                        var tf = System.IO.Path.Combine(dir, ".dircheck.tmp");
                        try { if (System.IO.File.Exists(tf)) System.IO.File.Delete(tf); } catch { }
                        System.IO.File.WriteAllText(tf, "ok");
                        ok = System.IO.File.Exists(tf);
                        try { System.IO.File.Delete(tf); } catch { }
                    }
                    catch { ok = false; }
                    if (ok) return true;
                    await System.Threading.Tasks.Task.Delay(200 * (i + 1));
                }
            }
            catch { }
            return false;
        }
        void WriteRecordingMeta(string path, DateTime? startUtc, DateTime? endUtc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return;
                var finfo = new System.IO.FileInfo(path);
                var end = endUtc ?? finfo.LastWriteTimeUtc;
                var start = _recordFirstWriteUtc ?? startUtc;
                if (!start.HasValue || end <= DateTime.MinValue) return;
                var ch = ResolveChannelKey();
                var source = GetSelectedSourceName();
                string title = "";
                try
                {
                    var progs = _epgService?.GetPrograms(_currentChannel?.TvgId, _currentChannel?.Name);
                    var startLocal = start.Value.ToLocalTime();
                    var p = progs?.FirstOrDefault(x => x.Start <= startLocal && x.End > startLocal);
                    if (p != null && !string.IsNullOrWhiteSpace(p.Title)) title = p.Title!;
                }
                catch { }
                var meta = new
                {
                    channel = ch,
                    source = source,
                    start_utc = start.Value.ToString("o"),
                    end_utc = end.ToString("o"),
                    duration_secs = (int)Math.Max(0, (end - start.Value).TotalSeconds),
                    title = title
                };
                var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = false });
                var adsPath = path + ":srcbox-meta";
                try
                {
                    using var ads = new System.IO.FileStream(adsPath, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                    using var sw = new System.IO.StreamWriter(ads);
                    sw.Write(json);
                }
                catch
                {
                }
                try { System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(path, ".json"), json); } catch { }
            }
            catch { }
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _ = LoadRecordingsLocalGrouped();
                        _ = LoadRecordingsLocalGrouped();
                    }
                    catch { }
                }));
            }
            catch { }
        }
        string SanName(string s)
        {
            s = s ?? "";
            s = s.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
            return s.Trim();
        }
        string ResolveRecordingDir(string safeChannel)
        {
            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var cfg = AppSettings.Current?.RecordingLocalDir ?? "";
            var source = GetSelectedSourceName();
            var safeSource = SanName(source);
            var path = (cfg ?? "").Replace("{source}", safeSource).Replace("{channel}", safeChannel);
            if (string.IsNullOrWhiteSpace(path)) path = System.IO.Path.Combine(baseDir, "recordings", safeChannel);
            if (!System.IO.Path.IsPathRooted(path)) path = System.IO.Path.Combine(baseDir, path);
            if (!path.EndsWith(safeChannel, StringComparison.OrdinalIgnoreCase) && !(cfg ?? "").Contains("{channel}")) path = System.IO.Path.Combine(path, safeChannel);
            return path;
        }
        string BuildRemoteUrlForRecording(LibmpvIptvClient.WebDavConfig wd, LibmpvIptvClient.Services.RecordingEntry item, bool json)
        {
            try
            {
                var recBase = (wd.RecordingsPath ?? "/srcbox/recordings/").TrimEnd('/');
                string channel = "", fileName = "";
                var p = item.PathOrUrl ?? "";
                if (!string.IsNullOrWhiteSpace(p) && p.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var u = new Uri(p, UriKind.Absolute);
                    var parts = u.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        if (string.Equals(parts[i], "recordings", StringComparison.OrdinalIgnoreCase) && i + 2 <= parts.Length - 1)
                        {
                            channel = Uri.UnescapeDataString(parts[i + 1]);
                            fileName = Uri.UnescapeDataString(parts[i + 2]);
                            break;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(fileName)) fileName = System.IO.Path.GetFileName(u.AbsolutePath);
                }
                if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(fileName))
                {
                    var lp = item.PathOrUrl ?? "";
                    if (!string.IsNullOrWhiteSpace(lp))
                    {
                        channel = ChannelFromFullPath(lp);
                        fileName = System.IO.Path.GetFileName(lp);
                    }
                }
                if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(fileName)) return "";
                if (json) fileName = System.IO.Path.ChangeExtension(fileName, ".json");
                var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                return cli.Combine(recBase + "/" + SanName(channel) + "/" + fileName);
            }
            catch { return ""; }
        }
        string GetSelectedSourceName()
        {
            try
            {
                var sel = AppSettings.Current?.SavedSources?.Find(s => s.IsSelected);
                if (sel != null && !string.IsNullOrWhiteSpace(sel.Name)) return sel.Name;
                var p = AppSettings.Current?.LastLocalM3uPath ?? "";
                if (!string.IsNullOrWhiteSpace(p)) return System.IO.Path.GetFileNameWithoutExtension(p);
            }
            catch { }
            return "default";
        }
        string ResolveChannelKey()
        {
            try
            {
                var name = _currentChannel?.Name;
                if (!string.IsNullOrWhiteSpace(name)) return name!;
                var id = _currentChannel?.TvgId ?? _currentChannel?.Id;
                if (!string.IsNullOrWhiteSpace(id)) return id!;
                var url = _currentUrl ?? "";
                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        if (Uri.TryCreate(url, UriKind.Absolute, out var u))
                        {
                            var last = System.IO.Path.GetFileName(u.AbsolutePath);
                            if (!string.IsNullOrWhiteSpace(last)) return last;
                            var host = u.Host;
                            if (!string.IsNullOrWhiteSpace(host)) return host;
                        }
                        else
                        {
                            var last = System.IO.Path.GetFileName(url);
                            if (!string.IsNullOrWhiteSpace(last)) return last;
                        }
                    }
                    catch
                    {
                        var last = System.IO.Path.GetFileName(url);
                        if (!string.IsNullOrWhiteSpace(last)) return last;
                    }
                }
            }
            catch { }
            return "unknown";
        }
        async void TryUploadRecording(string path, string? logoForToast)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                if (!System.IO.File.Exists(path)) return;
                var recCfg = AppSettings.Current?.Recording ?? new RecordingConfig();
                var mode = recCfg.SaveMode ?? "local_then_upload";
                if (string.Equals(mode, "local_only", StringComparison.OrdinalIgnoreCase)) return;
                var wd = AppSettings.Current.WebDav;
                if (wd == null || !wd.Enabled || string.IsNullOrWhiteSpace(wd.BaseUrl)) return;
                var key = ResolveChannelKey();
                var safe = SanName(string.IsNullOrWhiteSpace(key) ? "unknown" : key);
                var remoteDir = (wd.RecordingsPath ?? "/srcbox/recordings/").TrimEnd('/') + "/" + safe + "/";
                var fileName = System.IO.Path.GetFileName(path);
                if (string.Equals(mode, "dual_realtime", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                        if (wd.MoveSupported == null || wd.CopySupported == null)
                        {
                            try
                            {
                                var caps = await cli.ProbeCapabilitiesAsync(wd.BaseUrl);
                                wd.MoveSupported = caps.move;
                                wd.CopySupported = caps.copy;
                            }
                            catch { }
                        }
                        var tempSuffix = string.IsNullOrWhiteSpace(recCfg.RemoteTempSuffix) ? ".part" : recCfg.RemoteTempSuffix;
                        var tempUrl = cli.Combine(remoteDir + fileName + tempSuffix);
                        var finalUrl = cli.Combine(remoteDir + fileName);
                        // 若最终文件已存在（可能是此前已提升），直接提示并返回
                        var finalHead = await cli.HeadAsync(finalUrl);
                        if (finalHead.ok && finalHead.size > 0)
                        {
                            try
                            {
                                var titleToast = ReadSidecarTitle(path) ?? System.IO.Path.GetFileNameWithoutExtension(fileName);
                                LibmpvIptvClient.Services.ToastService.ShowSimple(LibmpvIptvClient.Services.ToastKind.UploadSuccess, safe, titleToast, logoForToast, 10000);
                            }
                            catch { }
                            // 尝试清理可能遗留的 .part
                            try { _ = cli.DeleteAsync(tempUrl); } catch { }
                            return;
                        }
                        var head = await cli.HeadAsync(tempUrl);
                        if (head.ok && head.size > 0)
                        {
                            var moved = false;
                            if (wd.MoveSupported.GetValueOrDefault(true))
                            {
                                moved = await cli.MoveAsync(tempUrl, finalUrl, overwrite: true);
                            }
                            if (!moved)
                            {
                                // MOVE 不被支持时尝试 COPY + DELETE
                                var copied = wd.CopySupported.GetValueOrDefault(true) ? await cli.CopyAsync(tempUrl, finalUrl, overwrite: true) : false;
                                if (copied) { try { _ = cli.DeleteAsync(tempUrl); } catch { } }
                                moved = copied;
                            }
                            if (moved)
                            {
                                try
                                {
                                    var titleToast = ReadSidecarTitle(path) ?? System.IO.Path.GetFileNameWithoutExtension(fileName);
                                    LibmpvIptvClient.Services.ToastService.ShowSimple(LibmpvIptvClient.Services.ToastKind.UploadSuccess, safe, titleToast, logoForToast, 10000);
                                    // 上传 sidecar.json（若存在）
                                    try
                                    {
                                        var sideLocal = System.IO.Path.ChangeExtension(path, ".json");
                                        if (System.IO.File.Exists(sideLocal))
                                        {
                                            var cliUp = new LibmpvIptvClient.Services.WebDavClient(wd);
                                            var sideRemote = cliUp.Combine(remoteDir + System.IO.Path.ChangeExtension(fileName, ".json"));
                                            await cliUp.PutFileAsync(sideRemote, sideLocal, "application/json", Math.Max(0, AppSettings.Current?.Recording?.UploadMaxKBps ?? 0));
                                        }
                                    }
                                    catch { }
                                }
                                catch { }
                                try { ScheduleRecordingsRefresh(safe); } catch { }
                                return;
                            }
                        }
                    }
                    catch { }
                    // 禁用回退全量上传：直接结束，保留 .part（避免可能的并发干扰）
                    try 
                    { 
                        ScheduleRecordingsRefresh(safe); 
                        if ((AppSettings.Current?.Recording?.RealtimeFinalizeEnabled ?? false))
                        {
                            var delaySec = Math.Max(0, AppSettings.Current.Recording.RealtimeFinalizeDelaySec);
                            var kbps = Math.Max(0, AppSettings.Current.Recording.RealtimeFinalizeMaxKBps);
                            _ = System.Threading.Tasks.Task.Run(async () =>
                            {
                                try
                                {
                                    await System.Threading.Tasks.Task.Delay(delaySec * 1000);
                                    var cli3 = new LibmpvIptvClient.Services.WebDavClient(wd);
                                    var finalUrl3 = cli3.Combine(remoteDir + fileName);
                                    var swf = System.Diagnostics.Stopwatch.StartNew();
                                    var ok = await cli3.PutFileAsync(finalUrl3, path, "video/MP2T", kbps);
                                    swf.Stop();
                                    if (ok)
                                    {
                                        try
                                        {
                                            var titleToast = ReadSidecarTitle(path) ?? System.IO.Path.GetFileNameWithoutExtension(fileName);
                                            LibmpvIptvClient.Services.ToastService.ShowSimple(LibmpvIptvClient.Services.ToastKind.UploadSuccess, safe, titleToast, logoForToast, 10000);
                                            // 上传 sidecar.json（若存在）
                                            try
                                            {
                                                var sideLocal3 = System.IO.Path.ChangeExtension(path, ".json");
                                                if (System.IO.File.Exists(sideLocal3))
                                                {
                                                    var sideRemote3 = cli3.Combine(remoteDir + System.IO.Path.ChangeExtension(fileName, ".json"));
                                                    await cli3.PutFileAsync(sideRemote3, sideLocal3, "application/json", kbps);
                                                }
                                            }
                                            catch { }
                                        } catch { }
                                        try 
                                        { 
                                            var tempSuffix3 = string.IsNullOrWhiteSpace(AppSettings.Current.Recording.RemoteTempSuffix) ? ".part" : AppSettings.Current.Recording.RemoteTempSuffix;
                                            var tempUrl3 = cli3.Combine(remoteDir + fileName + tempSuffix3);
                                            _ = cli3.DeleteAsync(tempUrl3); 
                                        } catch { }
                                        try { ScheduleRecordingsRefresh(safe); } catch { }
                                    }
                                }
                                catch { }
                            });
                        }
                    } 
                    catch { }
                    return;
                }
                LibmpvIptvClient.Services.UploadQueueService.Instance.Configure(
                    Math.Max(1, recCfg.UploadMaxConcurrency),
                    Math.Max(0, recCfg.UploadRetry),
                    Math.Max(100, recCfg.UploadRetryBackoffMs),
                    Math.Max(0, recCfg.UploadMaxKBps)
                );
                var deleteLocal = string.Equals(mode, "remote_only", StringComparison.OrdinalIgnoreCase);
                string? tempUrlForCleanup = null;
                if (string.Equals(mode, "dual_realtime", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var cli2 = new LibmpvIptvClient.Services.WebDavClient(wd);
                        var tempSuffix2 = string.IsNullOrWhiteSpace(recCfg.RemoteTempSuffix) ? ".part" : recCfg.RemoteTempSuffix;
                        tempUrlForCleanup = cli2.Combine(remoteDir + fileName + tempSuffix2);
                    }
                    catch { tempUrlForCleanup = null; }
                }
                LibmpvIptvClient.Services.UploadQueueService.Instance.Enqueue(path, remoteDir, fileName, wd, deleteLocal, tempUrlForCleanup, logoForToast);
            }
            catch { }
        }
        // 频道下拉与加载按钮已移除
        async Task LoadRecordingsLocalGrouped()
        {
            try
            {
                var t0 = DateTime.Now;
                try { LibmpvIptvClient.Diagnostics.Logger.Info("[Recordings] Refresh begin"); } catch { }
                string Resolver(string channel, DateTime? start)
                {
                    try
                    {
                        var progs = _epgService?.GetPrograms(_channels.FirstOrDefault(c => string.Equals(c.Name ?? "", channel ?? "", StringComparison.OrdinalIgnoreCase))?.TvgId, channel);
                        if (progs != null && start.HasValue)
                        {
                            var p = progs.FirstOrDefault(x => x.Start <= start.Value && x.End > start.Value);
                            if (p != null && !string.IsNullOrWhiteSpace(p.Title)) return p.Title;
                        }
                    }
                    catch { }
                    return "";
                }
                TimeSpan? DurationResolver(string channel, DateTime? start)
                {
                    try
                    {
                        var progs = _epgService?.GetPrograms(_channels.FirstOrDefault(c => string.Equals(c.Name ?? "", channel ?? "", StringComparison.OrdinalIgnoreCase))?.TvgId, channel);
                        if (progs != null && start.HasValue)
                        {
                            var p = progs.FirstOrDefault(x => x.Start <= start.Value && x.End > start.Value);
                            if (p != null) return (p.End - start.Value);
                        }
                    }
                    catch { }
                    return null;
                }
                var svc = new LibmpvIptvClient.Services.RecordingIndexService();
                var groups = await svc.GetAllLocalGroupedAsync((ch, st) => Resolver(ch, st), (ch, st) => DurationResolver(ch, st));
                _recordingsGroupsAll = groups ?? new List<LibmpvIptvClient.Services.RecordingGroup>();
                // 合并远端（WebDAV）：按频道逐一拉取远端+本地，保证“网络/本地”标识与远端文件可见
                try
                {
                    var channelNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var g in _recordingsGroupsAll) { if (!string.IsNullOrWhiteSpace(g.Channel)) channelNames.Add(g.Channel); }
                    try { foreach (var c in _channels ?? new System.Collections.Generic.List<Channel>()) { if (!string.IsNullOrWhiteSpace(c.Name)) channelNames.Add(c.Name!); } } catch { }
                    int mergedGroups = 0, mergedItems = 0;
                    foreach (var ch in channelNames)
                    {
                        try
                        {
                            var flat = await svc.GetForChannelAsync(ch, (st) => 
                            { 
                                try 
                                { 
                                    var title = Resolver(ch, st); 
                                    var dur = DurationResolver(ch, st); 
                                    return (string.IsNullOrWhiteSpace(title) ? null : title, dur); 
                                } 
                                catch { return (null, null); } 
                            });
                            if (flat != null && flat.Count > 0)
                            {
                                var existing = _recordingsGroupsAll.FirstOrDefault(x => string.Equals(x.Channel ?? "", ch ?? "", StringComparison.OrdinalIgnoreCase));
                                if (existing == null)
                                {
                                    existing = new LibmpvIptvClient.Services.RecordingGroup { Channel = ch };
                                    _recordingsGroupsAll.Add(existing);
                                }
                                existing.Items = flat.OrderByDescending(x => x.StartTime ?? DateTime.MinValue).ToList();
                                mergedGroups++;
                                mergedItems += existing.Items.Count;
                            }
                        }
                        catch { }
                    }
                    try { LibmpvIptvClient.Diagnostics.Logger.Info($"[Recordings] Refresh merged groups={mergedGroups} items={mergedItems}"); } catch { }
                }
                catch { }
                // 扁平缓存用于搜索/过滤
                _recordingsFlatAll = _recordingsGroupsAll.SelectMany(g => g.Items ?? new System.Collections.Generic.List<LibmpvIptvClient.Services.RecordingEntry>()).ToList();
                ApplyRecordingsFilter();
                try { LibmpvIptvClient.Diagnostics.Logger.Info($"[Recordings] Refresh done groups={_recordingsGroupsAll.Count} items={_recordingsFlatAll.Count} elapsed={(DateTime.Now - t0).TotalMilliseconds:0}ms"); } catch { }
            }
            catch { }
        }
        void StartRecordingsWatcher()
        {
            try
            {
                var root = System.AppDomain.CurrentDomain.BaseDirectory;
                var recRoot = System.IO.Path.Combine(root, "recordings");
                if (!System.IO.Directory.Exists(recRoot)) System.IO.Directory.CreateDirectory(recRoot);
                _recordingsWatcher = new FileSystemWatcher(recRoot, "*.ts");
                _recordingsWatcher.IncludeSubdirectories = true;
                _recordingsWatcher.EnableRaisingEvents = true;
                _recordingsWatcher.Created += (_, e) => { try { var ch = ChannelFromFullPath(e.FullPath); if (!string.IsNullOrWhiteSpace(ch)) ScheduleRecordingsRefresh(ch); else ScheduleRecordingsRefresh(); } catch { ScheduleRecordingsRefresh(); } };
                _recordingsWatcher.Renamed += (_, e) => { try { var ch = ChannelFromFullPath(e.FullPath); if (!string.IsNullOrWhiteSpace(ch)) ScheduleRecordingsRefresh(ch); else ScheduleRecordingsRefresh(); } catch { ScheduleRecordingsRefresh(); } };
                _recordingsWatcher.Changed += (_, e) => { try { var ch = ChannelFromFullPath(e.FullPath); if (!string.IsNullOrWhiteSpace(ch)) ScheduleRecordingsRefresh(ch); else ScheduleRecordingsRefresh(); } catch { ScheduleRecordingsRefresh(); } };
                _recordingsRefreshTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _recordingsRefreshTimer.Tick += (s, e) =>
                {
                    _recordingsRefreshTimer?.Stop();
                    _recordingsRefreshPending = false;
                    try
                    {
                        var key = _recordingsRefreshChannelKey;
                        _recordingsRefreshChannelKey = null;
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            _ = LoadRecordingsForChannel(key);
                        }
                        else
                        {
                            _ = LoadRecordingsLocalGrouped();
                        }
                    }
                    catch { }
                };
            }
            catch { }
        }
        string? _recordingsRefreshChannelKey = null;
        string ChannelFromFullPath(string fullPath)
        {
            try
            {
                var p = fullPath.Replace('\\', '/');
                var idx = p.LastIndexOf("/recordings/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var sub = p.Substring(idx + "/recordings/".Length);
                    var parts = sub.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0) return parts[0];
                }
            }
            catch { }
            return "";
        }
        void ScheduleRecordingsRefresh()
        {
            try
            {
                _recordingsRefreshPending = true;
                _recordingsRefreshTimer?.Stop();
                _recordingsRefreshTimer?.Start();
            }
            catch { }
        }
        void ScheduleRecordingsRefresh(string channelKey)
        {
            try
            {
                _recordingsRefreshChannelKey = channelKey;
                ScheduleRecordingsRefresh();
            }
            catch { }
        }
        async Task LoadRecordingsForChannel(string key)
        {
            try
            {
                if (_channelRefreshLast.TryGetValue(key ?? "", out var last) && (DateTime.UtcNow - last).TotalSeconds < 1.5) return;
                var svc = new LibmpvIptvClient.Services.RecordingIndexService();
                (string? title, TimeSpan?) Resolver(DateTime? start)
                {
                    try
                    {
                        var ch = key ?? "";
                        var progs = _epgService?.GetPrograms(_channels.FirstOrDefault(c => string.Equals(c.Name ?? "", ch ?? "", StringComparison.OrdinalIgnoreCase))?.TvgId, ch);
                        if (progs != null && start.HasValue)
                        {
                            var p = progs.FirstOrDefault(x => x.Start <= start.Value && x.End > start.Value);
                            if (p != null) return (p.Title, (p.End - start.Value));
                        }
                    }
                    catch { }
                    return (null, null);
                }
                var list = await svc.GetForChannelAsync(key ?? "", Resolver) ?? new List<LibmpvIptvClient.Services.RecordingEntry>();
                // 将该频道的最新数据写回到 _recordingsGroupsAll，并保持排序
                try
                {
                    var grp = _recordingsGroupsAll.FirstOrDefault(g => string.Equals(g.Channel ?? "", key ?? "", StringComparison.OrdinalIgnoreCase));
                    if (grp == null)
                    {
                        grp = new LibmpvIptvClient.Services.RecordingGroup { Channel = key ?? "" };
                        _recordingsGroupsAll.Add(grp);
                    }
                    grp.Items = list.OrderByDescending(x => x.StartTime ?? DateTime.MinValue).ThenByDescending(x => x.SizeBytes).ToList();
                }
                catch { }
                // 重新生成扁平缓存并应用过滤→更新 UI
                _recordingsFlatAll = _recordingsGroupsAll.SelectMany(g => g.Items ?? new System.Collections.Generic.List<LibmpvIptvClient.Services.RecordingEntry>()).ToList();
                ApplyRecordingsFilter();
                _channelRefreshLast[key ?? ""] = DateTime.UtcNow;
            }
            catch { }
        }
        readonly System.Collections.Generic.Dictionary<string, DateTime> _channelRefreshLast = new System.Collections.Generic.Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        // 频道下拉已移除，无需初始化
        async void BtnRecordingsRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                try { LibmpvIptvClient.Diagnostics.Logger.Info("[UI] Recordings Refresh clicked"); } catch { }
                await LoadRecordingsLocalGrouped();
            }
            catch { }
        }
        async void BtnRecordingPlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mpv == null) return;
                if (sender is System.Windows.Controls.Button btn && btn.DataContext is LibmpvIptvClient.Services.RecordingEntry item)
                {
                    string url = item.PathOrUrl ?? "";
                    if (string.IsNullOrWhiteSpace(url)) return;
                    // 播放方式选择：根据可用性控制按钮与文案
                    bool preferRemote = false;
                    string? remoteHrefFromLocal = null;
                    bool canLocal = false, canNetwork = false;
                    try
                    {
                        var wd = AppSettings.Current.WebDav;
                        if (!string.IsNullOrWhiteSpace(url) && System.IO.File.Exists(url)) canLocal = true;
                        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) canNetwork = true;
                        if (wd != null && wd.Enabled && !string.IsNullOrWhiteSpace(wd.BaseUrl) && !string.IsNullOrWhiteSpace(wd.RecordingsPath))
                        {
                            var recPath = (wd.RecordingsPath ?? "/srcbox/recordings/").TrimEnd('/');
                            var localFull = url.Replace('\\', '/');
                            var anchor = "/recordings/";
                            var idx = localFull.IndexOf(anchor, StringComparison.OrdinalIgnoreCase);
                            if (idx >= 0)
                            {
                                var rel = localFull.Substring(idx + anchor.Length); // <channel>/<file>
                                var cliTmp = new LibmpvIptvClient.Services.WebDavClient(wd);
                                remoteHrefFromLocal = cliTmp.Combine(recPath + "/" + rel);
                                try
                                {
                                    var head = await cliTmp.HeadAsync(remoteHrefFromLocal);
                                    canNetwork = head.ok;
                                }
                                catch
                                {
                                    canNetwork = false;
                                }
                            }
                        }
                        // 首先根据配置决定是否直接选择
                        var recCfg = AppSettings.Current?.Recording ?? new RecordingConfig();
                        var modePref = (recCfg.DefaultPlayChoice ?? "prompt").ToLowerInvariant();
                        var directChosen = false;
                        if (modePref == "local" || modePref == "remote" || modePref == "remember")
                        {
                            string desired = modePref == "remember" ? (recCfg.LastPlayChoice ?? "") : modePref;
                            if (desired == "local" && canLocal) { preferRemote = false; directChosen = true; }
                            else if (desired == "remote" && canNetwork) { preferRemote = true; directChosen = true; }
                            // 如果配置的目标不可用，回退到另一个可用
                            else if (!canLocal && canNetwork) { preferRemote = true; directChosen = true; }
                            else if (!canNetwork && canLocal) { preferRemote = false; directChosen = true; }
                            // 两者都不可用，继续弹窗
                        }
                        if (!directChosen)
                        {
                            var title = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_Recordings_Play_Mode_Title", "播放方式");
                            string desc;
                            if (canLocal && canNetwork)
                                desc = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_Recordings_Play_Mode_Desc_Both", "此录播同时存在网络与本地，请选择播放方式");
                            else if (canNetwork)
                                desc = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_Recordings_Play_Mode_Desc_RemoteOnly", "此录播仅有网络版本");
                            else
                                desc = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_Recordings_Play_Mode_Desc_LocalOnly", "此录播仅有本地版本");
                            var yesLabel = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Btn_Play_Remote", "网络");
                            var noLabel = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Btn_Play_Local", "本地");
                            var choice = ModernMessageBox.ShowCustom(this, desc, title, yesLabel, noLabel, canNetwork, canLocal);
                            if (choice == null) return;
                            preferRemote = choice == true;
                            if (!canNetwork && canLocal) preferRemote = false;
                            if (canNetwork && !canLocal) preferRemote = true;
                            // 记忆选择（若设置为 remember）
                            try
                            {
                                if (string.Equals(AppSettings.Current?.Recording?.DefaultPlayChoice ?? "", "remember", StringComparison.OrdinalIgnoreCase))
                                {
                                    AppSettings.Current.Recording.LastPlayChoice = preferRemote ? "remote" : "local";
                                    AppSettings.Current.Save();
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                    if (preferRemote && !string.IsNullOrWhiteSpace(remoteHrefFromLocal))
                    {
                        try
                        {
                            var wd = AppSettings.Current.WebDav;
                            if (wd == null || !wd.Enabled || string.IsNullOrWhiteSpace(wd.BaseUrl)) return;
                            var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                            var href = remoteHrefFromLocal!;
                            string authUrl = href;
                            try
                            {
                                var u = new Uri(href);
                                var user = wd.Username ?? "";
                                var pass = wd.TokenOrPassword;
                                if (string.IsNullOrWhiteSpace(pass)) pass = LibmpvIptvClient.Services.CryptoUtil.UnprotectString(wd.EncryptedToken ?? "");
                                var userInfo = $"{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(pass)}";
                                authUrl = $"{u.Scheme}://{userInfo}@{u.Host}{(u.IsDefaultPort ? "" : ":" + u.Port)}{u.AbsolutePath}{u.Query}{u.Fragment}";
                            }
                            catch { authUrl = href; }
                            if (_timeshiftActive) { try { ToggleTimeshift(false); } catch { } }
                            ClearLiveAndEpgIndicators();
                            try { LibmpvIptvClient.Diagnostics.Logger.Info("[Recordings] LoadFile (remote-direct) " + RedactUrl(authUrl)); } catch { }
                            _mpv.LoadFile(authUrl);
                            _currentUrl = authUrl;
                            try { PlaceholderPanel.Visibility = Visibility.Collapsed; VideoHost.Visibility = Visibility.Visible; } catch { }
                            try { _mpv.Pause(false); _mpv.SeekAbsolute(0); } catch { }
                        }
                        catch { }
                        _paused = false;
                        UpdatePlayPauseIcon();
                        _currentPlayingProgram = null;
                        _timeshiftActive = false;
                        try
                        {
                            _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Record", "录播");
                            _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusReplayBrush");
                            TxtPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush;
                            TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush;
                            _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
                            _overlayWpf?.SetSpeedEnabled(true); _mpv?.SetSpeed(_playbackSpeed); _overlayWpf?.SetSpeed(_playbackSpeed);
                            if (BtnSpeed != null) { BtnSpeed.IsEnabled = true; LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; }
                        }
                        catch { }
                        try { _currentRecordingPlaying = item; _currentRecordingPlaying.IsPlaying = true; } catch { }
                        StartRecordReplayMonitor();
                        return;
                    }
PLAY_LOCAL:
                    if (string.Equals(item.Source ?? "", "Local", StringComparison.OrdinalIgnoreCase) || true)
                    {
                        try
                        {
                            if (_timeshiftActive) { try { ToggleTimeshift(false); } catch { } }
                            ClearLiveAndEpgIndicators();
                            ClearRecordingPlayingIndicator();
                            var fu = new Uri(url, UriKind.Absolute);
                            var fileUri = fu.AbsoluteUri;
                            try { LibmpvIptvClient.Diagnostics.Logger.Info("[Recordings] LoadFile (remote-download) " + RedactUrl(fileUri)); } catch { }
                            _mpv.LoadFile(fileUri);
                            _currentUrl = fileUri;
                            _currentUrl = fileUri;
                        }
                        catch
                        {
                            if (_timeshiftActive) { try { ToggleTimeshift(false); } catch { } }
                            ClearLiveAndEpgIndicators();
                            ClearRecordingPlayingIndicator();
                            try { LibmpvIptvClient.Diagnostics.Logger.Info("[Recordings] LoadFile (local-raw) " + RedactUrl(url)); } catch { }
                            _mpv.LoadFile(url);
                            _currentUrl = url;
                        }
                        try { PlaceholderPanel.Visibility = Visibility.Collapsed; VideoHost.Visibility = Visibility.Visible; } catch { }
                        _paused = false;
                        UpdatePlayPauseIcon();
                try { _mpv.Pause(false); _mpv.SeekAbsolute(0); } catch { }
                        _currentPlayingProgram = null;
                        _timeshiftActive = false;
                        try
                        {
                            _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Record", "录播");
                            _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusReplayBrush");
                            TxtPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush;
                            TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush;
                            _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
                            _overlayWpf?.SetSpeedEnabled(true); _mpv?.SetSpeed(_playbackSpeed); _overlayWpf?.SetSpeed(_playbackSpeed);
                            if (BtnSpeed != null) { BtnSpeed.IsEnabled = true; LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; }
                        }
                        catch { }
                        try { _currentRecordingPlaying = item; _currentRecordingPlaying.IsPlaying = true; } catch { }
                        StartRecordReplayMonitor();
                    }
                    else
                    {
                        try
                        {
                            var wd = AppSettings.Current.WebDav;
                            if (wd == null || !wd.Enabled || string.IsNullOrWhiteSpace(wd.BaseUrl)) return;
                            var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                            var href = url;
                            if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase)) href = cli.Combine(href);
                            // 优先流式播放
                            string authUrl = href;
                            try
                            {
                                var u = new Uri(href);
                                var user = wd.Username ?? "";
                                var pass = wd.TokenOrPassword;
                                if (string.IsNullOrWhiteSpace(pass)) pass = LibmpvIptvClient.Services.CryptoUtil.UnprotectString(wd.EncryptedToken ?? "");
                                var userInfo = $"{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(pass)}";
                                authUrl = $"{u.Scheme}://{userInfo}@{u.Host}{(u.IsDefaultPort ? "" : ":" + u.Port)}{u.AbsolutePath}{u.Query}{u.Fragment}";
                            }
                            catch { authUrl = href; }
                            if (_timeshiftActive) { try { ToggleTimeshift(false); } catch { } }
                            ClearLiveAndEpgIndicators();
                            ClearRecordingPlayingIndicator();
                            _mpv.LoadFile(authUrl);
                            _currentUrl = authUrl;
                            try { PlaceholderPanel.Visibility = Visibility.Collapsed; VideoHost.Visibility = Visibility.Visible; } catch { }
                            try { _mpv.Pause(false); _mpv.SeekAbsolute(0); } catch { }
                            try { LibmpvIptvClient.Services.NotificationService.Instance.Show(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_Recordings_Title", "录播列表"), authUrl, 4000); } catch { }
                        }
                        catch 
                        { 
                            // 回退：下载到临时目录
                            try
                            {
                                var wd = AppSettings.Current.WebDav;
                                if (wd == null || !wd.Enabled || string.IsNullOrWhiteSpace(wd.BaseUrl)) return;
                                var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                                var href = url;
                                if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase)) href = cli.Combine(href);
                            string authUrl = href;
                            try
                            {
                                var u = new Uri(href);
                                var user = wd.Username ?? "";
                                var pass = wd.TokenOrPassword;
                                if (string.IsNullOrWhiteSpace(pass)) pass = LibmpvIptvClient.Services.CryptoUtil.UnprotectString(wd.EncryptedToken ?? "");
                                var userInfo = $"{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(pass)}";
                                authUrl = $"{u.Scheme}://{userInfo}@{u.Host}{(u.IsDefaultPort ? "" : ":" + u.Port)}{u.AbsolutePath}{u.Query}{u.Fragment}";
                            }
                            catch { authUrl = href; }
                                if (_timeshiftActive) { try { ToggleTimeshift(false); } catch { } }
                                ClearLiveAndEpgIndicators();
                                ClearRecordingPlayingIndicator();
                            try { LibmpvIptvClient.Diagnostics.Logger.Info("[Recordings] LoadFile (remote-direct) " + RedactUrl(authUrl)); } catch { }
                            _mpv.LoadFile(authUrl);
                            _currentUrl = authUrl;
                                try { PlaceholderPanel.Visibility = Visibility.Collapsed; VideoHost.Visibility = Visibility.Visible; } catch { }
                                try { _mpv.Pause(false); _mpv.SeekAbsolute(0); } catch { }
                            try { LibmpvIptvClient.Services.NotificationService.Instance.Show(LibmpvIptvClient.Helpers.ResxLocalizer.Get("Drawer_Recordings_Title", "录播列表"), authUrl, 4000); } catch { }
                            }
                            catch { }
                        }
                        _paused = false;
                        UpdatePlayPauseIcon();
                        _currentPlayingProgram = null;
                        _timeshiftActive = false;
                        try
                        {
                            _playbackStatusText = LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Record", "录播");
                            _playbackStatusBrush = (System.Windows.Media.Brush)FindResource("StatusReplayBrush");
                            TxtPlaybackStatus.Text = _playbackStatusText; TxtPlaybackStatus.Foreground = _playbackStatusBrush;
                            TxtBottomPlaybackStatus.Text = _playbackStatusText; TxtBottomPlaybackStatus.Foreground = _playbackStatusBrush;
                            _overlayWpf?.SetPlaybackStatus(_playbackStatusText, _playbackStatusBrush);
                            _overlayWpf?.SetSpeedEnabled(true); _mpv?.SetSpeed(_playbackSpeed); _overlayWpf?.SetSpeed(_playbackSpeed);
                            if (BtnSpeed != null) { BtnSpeed.IsEnabled = true; LblSpeedBar.Text = $"{_playbackSpeed:0.##}x"; }
                        }
                        catch { }
                        try { _currentRecordingPlaying = item; _currentRecordingPlaying.IsPlaying = true; } catch { }
                        StartRecordReplayMonitor();
                    }
                }
            }
            catch { }
        }
        void RecordingItem_ContextMenu(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var fe = sender as System.Windows.FrameworkElement;
                if (fe == null) return;
                var item = fe.DataContext as LibmpvIptvClient.Services.RecordingEntry;
                if (item == null) return;
                var cm = new System.Windows.Controls.ContextMenu();
                System.Windows.Controls.MenuItem Add(string key, string fallback, Action click, bool isEnabled = true)
                {
                    var mi = new System.Windows.Controls.MenuItem();
                    try { mi.Header = LibmpvIptvClient.Helpers.ResxLocalizer.Get(key, fallback); } catch { mi.Header = fallback; }
                    mi.IsEnabled = isEnabled;
                    mi.Click += (_, __) => { try { click(); } catch { } };
                    cm.Items.Add(mi);
                    return mi;
                }
                // 播放
                Add("Drawer_Recordings_Play", "播放", () =>
                {
                    try
                    {
                        var btn = new System.Windows.Controls.Button { DataContext = item };
                        BtnRecordingPlay_Click(btn, new RoutedEventArgs());
                    }
                    catch { }
                }, true);
                // 下载（仅远端）
                Add("Drawer_Recordings_Download", "下载", () =>
                {
                    try
                    {
                        var btn = new System.Windows.Controls.Button { DataContext = item };
                        BtnRecordingDownload_Click(btn, new RoutedEventArgs());
                    }
                    catch { }
                }, string.Equals(item.Source ?? "", "Remote", StringComparison.OrdinalIgnoreCase));
                cm.Items.Add(new System.Windows.Controls.Separator());
                // 重命名（仅本地）
                Add("Common_Rename", "重命名", () =>
                {
                    try
                    {
                        var path = item.PathOrUrl ?? "";
                        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return;
                        var dir = System.IO.Path.GetDirectoryName(path) ?? "";
                        var ext = System.IO.Path.GetExtension(path);
                        var baseName = System.IO.Path.GetFileNameWithoutExtension(path);
                        var dlg = new Microsoft.Win32.SaveFileDialog
                        {
                            InitialDirectory = dir,
                            FileName = baseName + ext,
                            Filter = "Video|*.ts;*.mp4;*.mkv;*.*"
                        };
                        var ok = dlg.ShowDialog() == true;
                        if (!ok) return;
                        var newPath = dlg.FileName;
                        if (string.Equals(newPath, path, StringComparison.OrdinalIgnoreCase)) return;
                        System.IO.File.Move(path, newPath, true);
                        // sidecar 同步
                        try
                        {
                            var sideOld = System.IO.Path.ChangeExtension(path, ".json");
                            if (System.IO.File.Exists(sideOld))
                            {
                                var sideNew = System.IO.Path.ChangeExtension(newPath, ".json");
                                System.IO.File.Move(sideOld, sideNew, true);
                            }
                        }
                        catch { }
                        item.PathOrUrl = newPath;
                        item.Title = System.IO.Path.GetFileNameWithoutExtension(newPath);
                        ScheduleRecordingsRefresh(ChannelFromFullPath(newPath));
                    }
                    catch { }
                }, string.Equals(item.Source ?? "", "Local", StringComparison.OrdinalIgnoreCase));
                Add("UI_Delete", "删除…", async () =>
                {
                    try
                    {
                        var dlg = new LibmpvIptvClient.Controls.DeleteRecordingDialog { Owner = this };
                        var ok = dlg.ShowDialog() == true;
                        if (!ok) return;
                        var choice = dlg.Choice;
                        var chKeyForToast = ResolveChannelKey() ?? "SrcBox";
                        if (choice == "local" || choice == "both")
                        {
                            try
                            {
                                var path = item.PathOrUrl ?? "";
                                if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                                {
                                    try { System.IO.File.Delete(path); } catch { }
                                    try
                                    {
                                        var side = System.IO.Path.ChangeExtension(path, ".json");
                                        if (System.IO.File.Exists(side)) System.IO.File.Delete(side);
                                    }
                                    catch { }
                                }
                            }
                            catch { }
                        }
                        if (choice == "remote" || choice == "both")
                        {
                            try
                            {
                                var wd = AppSettings.Current.WebDav;
                                if (wd != null && wd.Enabled && !string.IsNullOrWhiteSpace(wd.BaseUrl))
                                {
                                    var hrefTs = BuildRemoteUrlForRecording(wd, item, false);
                                    var hrefJson = BuildRemoteUrlForRecording(wd, item, true);
                                    if (!string.IsNullOrWhiteSpace(hrefTs))
                                    {
                                        var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                                        try { LibmpvIptvClient.Diagnostics.Logger.Info($"[Recordings.Delete] Remote TS {RedactUrl(hrefTs)}"); } catch { }
                                        try { await cli.DeleteAsync(hrefTs); } catch { }
                                        if (!string.IsNullOrWhiteSpace(hrefJson))
                                        {
                                            try { LibmpvIptvClient.Diagnostics.Logger.Info($"[Recordings.Delete] Remote JSON {RedactUrl(hrefJson)}"); } catch { }
                                            try { await cli.DeleteAsync(hrefJson); } catch { }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                        try
                        {
                            var chKey = ResolveChannelKey() ?? "";
                            if (!string.IsNullOrWhiteSpace(item.PathOrUrl)) chKey = ChannelFromFullPath(item.PathOrUrl);
                            // 删除完成后先提示，再刷新（用户无感）
                            try
                            {
                                var kind = choice == "local" ? LibmpvIptvClient.Services.ToastKind.DeleteLocal
                                          : (choice == "remote" ? LibmpvIptvClient.Services.ToastKind.DeleteRemote
                                            : LibmpvIptvClient.Services.ToastKind.DeleteBoth);
                                var logo = TryResolveChannelLogo(chKeyForToast) ?? _currentChannel?.Logo;
                                var subtitle = item.Title; // 节目名显示在频道下
                                LibmpvIptvClient.Services.ToastService.ShowSimple(kind, chKeyForToast, subtitle, logo, 3000);
                            }
                            catch { }
                            ScheduleRecordingsRefresh(chKey);
                        }
                        catch { }
                    }
                    catch { }
                }, true);
                cm.Items.Add(new System.Windows.Controls.Separator());
                // 打开文件夹/复制路径
                Add("Drawer_OpenFolder", "打开文件夹", () =>
                {
                    try
                    {
                        var p = item.PathOrUrl ?? "";
                        if (!string.IsNullOrWhiteSpace(p) && System.IO.File.Exists(p))
                        {
                            try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + p + "\""); } catch { }
                        }
                        else if (!string.IsNullOrWhiteSpace(p))
                        {
                            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = p, UseShellExecute = true }); } catch { }
                        }
                    }
                    catch { }
                }, true);
                Add("Drawer_CopyPath", "复制路径", () =>
                {
                    try
                    {
                        var p = item.PathOrUrl ?? "";
                        if (!string.IsNullOrWhiteSpace(p)) System.Windows.Clipboard.SetText(p);
                    }
                    catch { }
                }, true);
                fe.ContextMenu = cm;
                cm.IsOpen = true;
                e.Handled = true;
            }
            catch { }
        }
        async void BtnRecordingDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn && btn.DataContext is LibmpvIptvClient.Services.RecordingEntry item)
                {
                    if (!string.Equals(item.Source ?? "", "Remote", StringComparison.OrdinalIgnoreCase)) return;
                    var wd = AppSettings.Current.WebDav;
                    if (wd == null || !wd.Enabled || string.IsNullOrWhiteSpace(wd.BaseUrl)) return;
                    var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                    // 统一构造远端 ts/json URL，避免重复 /webdav
                    var tsUrl = BuildRemoteUrlForRecording(wd, item, false);
                    var jsonUrl = BuildRemoteUrlForRecording(wd, item, true);
                    if (string.IsNullOrWhiteSpace(tsUrl)) return;
                    string channel = ChannelFromFullPath(item.PathOrUrl ?? "") ?? "";
                    if (string.IsNullOrWhiteSpace(channel))
                    {
                        try
                        {
                            var uu = new Uri(tsUrl, UriKind.Absolute);
                            var parts = uu.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < parts.Length - 1; i++)
                            {
                                if (string.Equals(parts[i], "recordings", StringComparison.OrdinalIgnoreCase)) { channel = Uri.UnescapeDataString(parts[i + 1]); break; }
                            }
                        }
                        catch { }
                    }
                    var safe = SanName(string.IsNullOrWhiteSpace(channel) ? "unknown" : channel);
                    var dir = ResolveRecordingDir(safe);
                    await EnsureRecordingDirReady(dir);
                    var fileName = System.IO.Path.GetFileName(new Uri(tsUrl, UriKind.Absolute).AbsolutePath);
                    var path = System.IO.Path.Combine(dir, fileName);
                    try { LibmpvIptvClient.Diagnostics.Logger.Info($"[Recordings.Download] TS {RedactUrl(tsUrl)}"); } catch { }
                    var res = await cli.GetBytesAsync(tsUrl);
                    if (res.ok && res.bytes != null && res.bytes.Length > 0) { System.IO.File.WriteAllBytes(path, res.bytes); }
                    // 下载 json（可选）
                    if (!string.IsNullOrWhiteSpace(jsonUrl))
                    {
                        try
                        {
                            try { LibmpvIptvClient.Diagnostics.Logger.Info($"[Recordings.Download] JSON {RedactUrl(jsonUrl)}"); } catch { }
                            var sideRes = await cli.GetBytesAsync(jsonUrl);
                            if (sideRes.ok && sideRes.bytes != null && sideRes.bytes.Length > 0)
                            {
                                var sidePath = System.IO.Path.ChangeExtension(path, ".json");
                                System.IO.File.WriteAllBytes(sidePath, sideRes.bytes);
                            }
                        }
                        catch { }
                    }
                    try
                    {
                        var logo = TryResolveChannelLogo(safe) ?? _currentChannel?.Logo;
                        var subtitle = item.Title; // 节目名显示在频道名下
                        LibmpvIptvClient.Services.ToastService.ShowSimple(LibmpvIptvClient.Services.ToastKind.DownloadSuccess, safe, subtitle, logo, 10000);
                    }
                    catch { }
                    try { ScheduleRecordingsRefresh(safe); } catch { }
                }
            }
            catch { }
        }
        async void BtnRecordingUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn && btn.DataContext is LibmpvIptvClient.Services.RecordingEntry item)
                {
                    if (!string.Equals(item.Source ?? "", "Local", StringComparison.OrdinalIgnoreCase)) { /*仅本地触发上传*/ }
                    var wd = AppSettings.Current.WebDav;
                    if (wd == null || !wd.Enabled || string.IsNullOrWhiteSpace(wd.BaseUrl)) return;
                    // 远端是否已存在：HEAD 检测
                    var hrefTs = BuildRemoteUrlForRecording(wd, item, false);
                    var cli = new LibmpvIptvClient.Services.WebDavClient(wd);
                    var existsRemote = false;
                    try { var head = await cli.HeadAsync(hrefTs); existsRemote = head.ok; } catch { existsRemote = false; }
                    if (existsRemote) return; // 已有网络资源则不上传
                    // 入队上传（ts 与 json 同步在队列与转正路径中已处理）
                    var channel = ChannelFromFullPath(item.PathOrUrl ?? "") ?? "unknown";
                    var safe = SanName(channel);
                    var remoteDir = (wd.RecordingsPath ?? "/srcbox/recordings/").TrimEnd('/') + "/" + safe + "/";
                    var fileName = System.IO.Path.GetFileName(item.PathOrUrl ?? "");
                    if (string.IsNullOrWhiteSpace(fileName) || !System.IO.File.Exists(item.PathOrUrl ?? "")) return;
                    LibmpvIptvClient.Services.UploadQueueService.Instance.Enqueue(item.PathOrUrl!, remoteDir, fileName, wd, false, null, _currentChannel?.Logo);
                    // 手动上传：给 3 秒“已加入上传”气泡（频道名 + 节目名）
                    try
                    {
                        var logo = TryResolveChannelLogo(safe) ?? _currentChannel?.Logo;
                        var subtitle = item.Title;
                        LibmpvIptvClient.Services.ToastService.ShowSimple(LibmpvIptvClient.Services.ToastKind.UploadQueued, safe, subtitle, logo, 3000);
                    }
                    catch { }
                }
            }
            catch { }
        }
        void BtnRecordingUpload_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button btn && btn.DataContext is LibmpvIptvClient.Services.RecordingEntry item)
                {
                    var label = (item.SourceLabel ?? "").ToLowerInvariant();
                    var hasRemote = label.Contains("网络") || label.Contains("remote") || label.Contains("удал");
                    btn.IsEnabled = !hasRemote;
                }
            }
            catch { }
        }
        string? TryResolveChannelLogo(string channelKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channelKey)) return null;
                var keySan = SanName(channelKey);
                var ch = _channels?.FirstOrDefault(c => string.Equals(c.Name, channelKey, StringComparison.OrdinalIgnoreCase));
                if (ch == null) ch = _channels?.FirstOrDefault(c => string.Equals(SanName(c.Name ?? ""), keySan, StringComparison.OrdinalIgnoreCase));
                return ch?.Logo;
            }
            catch { return null; }
        }
        string? ReadSidecarTitle(string videoPath)
        {
            try
            {
                var jf = System.IO.Path.ChangeExtension(videoPath, ".json");
                if (!System.IO.File.Exists(jf)) return null;
                using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(jf));
                if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String)
                    return t.GetString();
            }
            catch { }
            return null;
        }
        public string? GetCurrentChannelId() => _currentChannel?.TvgId ?? _currentChannel?.Id;
        public string? GetCurrentChannelName() => _currentChannel?.Name;
        public void SetFullscreenMode(bool on) => ToggleFullscreen(on);
        string RedactUrl(string href)
        {
            try
            {
                var u = new Uri(href, UriKind.RelativeOrAbsolute);
                if (!u.IsAbsoluteUri) return href;
                var port = u.IsDefaultPort ? "" : ":" + u.Port.ToString();
                var maskedHost = "***";
                return $"{u.Scheme}://{maskedHost}{port}{u.AbsolutePath}{u.Query}";
            }
            catch { return href; }
        }
    }
}
