using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Threading;

namespace LibmpvIptvClient
{
    public partial class ReminderToastWindow : Window
    {
        private readonly string _channelId;
        private readonly string _channelName;
        private readonly DispatcherTimer _autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private readonly DispatcherTimer _reposition = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        private int _remainSec = 10;
        private readonly bool _isDue;
        private readonly Func<System.Windows.Rect>? _anchorProvider;
        public ReminderToastWindow(string channelId, string channelName, string program, DateTime startLocal, string? logoPath, bool isDue = false, Func<System.Windows.Rect>? anchorProvider = null)
        {
            InitializeComponent();
            _channelId = channelId ?? "";
            _channelName = channelName ?? "";
            _isDue = isDue;
            _anchorProvider = anchorProvider;
            TxtChannel.Text = channelName ?? "";
            TxtProgram.Text = program ?? "";
            TxtTime.Text = startLocal.ToString("yyyy-MM-dd HH:mm");
            TxtCountdown.Text = $"剩余 {_remainSec:00} 秒";
            TxtStatus.Text = isDue ? "节目开播" : "预约成功";
            try { BtnPlay.Visibility = isDue ? Visibility.Visible : Visibility.Collapsed; } catch { }
            try
            {
                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(logoPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    ImgLogo.Source = bmp;
                }
            }
            catch { }
            Loaded += (s, e) =>
            {
                try
                {
                    CenterToAnchor();
                    _autoClose.Tick += (s2, e2) =>
                    {
                        try
                        {
                            _remainSec = Math.Max(0, _remainSec - 1);
                            TxtCountdown.Text = $"剩余 {_remainSec:00} 秒";
                            if (_remainSec <= 0) Close();
                        }
                        catch { }
                    };
                    _autoClose.Start();
                    _reposition.Tick += (s3, e3) => { try { CenterToAnchor(); } catch { } };
                    _reposition.Start();
                }
                catch { }
            };
        }
        void CenterToAnchor()
        {
            if (_anchorProvider != null)
            {
                var r = _anchorProvider();
                if (r.Width > 0 && r.Height > 0)
                {
                    Left = r.Left + (r.Width - Width) / 2;
                    Top = r.Top + (r.Height - Height) / 2;
                    return;
                }
            }
            // Default: bottom-right of workarea
            Left = SystemParameters.WorkArea.Right - Width - 16;
            Top = SystemParameters.WorkArea.Bottom - Height - 16;
        }
        void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mw = System.Windows.Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault();
                mw?.JumpToChannelByIdOrName(_channelId, _channelName);
            }
            catch { }
            Close();
        }
        void BtnDismiss_Click(object sender, RoutedEventArgs e) => Close();
        protected override void OnClosed(EventArgs e)
        {
            try { _autoClose.Stop(); } catch { }
            try { _reposition.Stop(); } catch { }
            base.OnClosed(e);
        }
    }
}
