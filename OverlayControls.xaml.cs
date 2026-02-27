using System;
using System.Windows;
using System.Windows.Controls;

namespace LibmpvIptvClient
{
    public partial class OverlayControls : Window
    {
        public event Action? PlayPause;
        public event Action? Stop;
        public event Action? Rew;
        public event Action? Fwd;
        public event Action<double>? SeekEnd;
        public event Action? SeekStart;
        public event Action<double>? VolumeChanged;
        // public event Action<bool>? FccChanged; // Removed
        // public event Action<bool>? UdpChanged; // Removed
        public event Action? SourceMenuRequested;
        public event Action<bool>? DrawerToggled;
        public event Action<bool>? EpgToggled;
        public event Action<string>? AspectRatioChanged;
        private bool _seeking = false;
        public OverlayControls()
        {
            InitializeComponent();
        }
        public void SetPaused(bool paused)
        {
            try
            {
                IconPlayPause.Symbol = paused ? ModernWpf.Controls.Symbol.Play : ModernWpf.Controls.Symbol.Pause;
            }
            catch { }
        }
        public void SetTime(double current, double duration)
        {
            SliderSeek.Maximum = duration <= 0 ? 1 : duration;
            if (!_seeking)
            {
                SliderSeek.Value = Math.Max(0, Math.Min(SliderSeek.Maximum, current));
            }
            LblElapsed.Text = FormatTime(current);
            LblDuration.Text = FormatTime(duration);
        }
        public void SetVolume(double val)
        {
            SliderVolume.Value = val;
        }
        string FormatTime(double sec)
        {
            if (sec < 0) sec = 0;
            var ts = TimeSpan.FromSeconds(sec);
            return ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
        }
        void BtnPlayPause_Click(object sender, RoutedEventArgs e) => PlayPause?.Invoke();
        void BtnStop_Click(object sender, RoutedEventArgs e) => Stop?.Invoke();
        void BtnRew_Click(object sender, RoutedEventArgs e) => Rew?.Invoke();
        void BtnFwd_Click(object sender, RoutedEventArgs e) => Fwd?.Invoke();
        void SliderSeek_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { _seeking = true; SeekStart?.Invoke(); }
        void SliderSeek_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _seeking = false;
            SeekEnd?.Invoke(SliderSeek.Value);
        }
        void SliderSeek_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_seeking) LblElapsed.Text = FormatTime(SliderSeek.Value);
        }
        void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VolumeChanged?.Invoke(SliderVolume.Value);
        }
        bool _muted = false;
        void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            _muted = !_muted;
            IconMute.Symbol = _muted ? ModernWpf.Controls.Symbol.Mute : ModernWpf.Controls.Symbol.Volume;
            VolumeChanged?.Invoke(SliderVolume.Value);
            MuteChanged?.Invoke(_muted);
        }
        public void SetFcc(bool v) { /* Removed */ }
        public void SetUdp(bool v) { /* Removed */ }
        
        // 重构：统一使用 Visible 逻辑
        // visible = true -> IsChecked = true (高亮)
        // visible = false -> IsChecked = false (灰色)
        public void SetDrawerVisible(bool visible) 
        { 
            CbDrawer.IsChecked = visible; 
        }
        
        public void SetEpgVisible(bool visible) { CbEpg.IsChecked = visible; }
        // void CbFcc_Checked(object sender, RoutedEventArgs e) => FccChanged?.Invoke(CbFcc.IsChecked == true); // Removed
        // void CbUdp_Checked(object sender, RoutedEventArgs e) => UdpChanged?.Invoke(CbUdp.IsChecked == true); // Removed
        
        // 点击事件：
        // IsChecked = true -> visible = true
        // IsChecked = false -> visible = false
        // DrawerToggled 参数含义改为 visible
        void CbDrawer_Checked(object sender, RoutedEventArgs e) 
        {
             DrawerToggled?.Invoke(CbDrawer.IsChecked == true);
        }
        void CbEpg_Checked(object sender, RoutedEventArgs e) => EpgToggled?.Invoke(CbEpg.IsChecked == true);
        void BtnRatio_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var options = new[] { 
                ("默认", "default"),
                ("16:9", "16:9"),
                ("4:3", "4:3"),
                ("拉伸", "stretch"),
                ("填充", "fill"),
                ("裁剪", "crop")
            };
            foreach (var (label, val) in options)
            {
                var mi = new MenuItem();
                mi.Header = label;
                mi.Click += (s, ev) => AspectRatioChanged?.Invoke(val);
                menu.Items.Add(mi);
            }
            BtnRatio.ContextMenu = menu;
            BtnRatio.ContextMenu.PlacementTarget = BtnRatio;
            BtnRatio.ContextMenu.IsOpen = true;
        }
        public void SetInfo(string text) { LblInfo.Text = text; }
        void BtnSourceMenu_Click(object sender, RoutedEventArgs e) => SourceMenuRequested?.Invoke();
        public event Action<bool>? MuteChanged;
        public void OpenSourceContextMenu(ContextMenu menu)
        {
            BtnSources.ContextMenu = menu;
            BtnSources.ContextMenu.PlacementTarget = BtnSources;
            BtnSources.ContextMenu.IsOpen = true;
        }
        public void SetTags(System.Collections.Generic.List<string> tags)
        {
            try
            {
                TagPanelOverlay.Children.Clear();
                var style = TryFindResource("OverlayTagChip") as System.Windows.Style;
                foreach (var t in tags)
                {
                    var b = new Border { Style = style };
                    var tb = new TextBlock { Text = t, Foreground = System.Windows.Media.Brushes.White, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
                    b.Child = tb;
                    TagPanelOverlay.Children.Add(b);
                }
            }
            catch { }
        }
        
        public void SetPlaybackStatus(string text, System.Windows.Media.Brush foreground)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    StatusIndicator.Visibility = Visibility.Collapsed;
                    StatusIndicatorInner.Visibility = Visibility.Collapsed;
                }
                else
                {
                    StatusIndicator.Visibility = Visibility.Visible;
                    StatusIndicatorInner.Visibility = Visibility.Visible;
                    TxtStatus.Text = text;
                    TxtStatus.Foreground = foreground;
                    TxtStatusInner.Text = text;
                    TxtStatusInner.Foreground = foreground;
                }
            }
            catch { }
        }
    }
}
