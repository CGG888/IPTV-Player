using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace LibmpvIptvClient.Helpers
{
    public static class ReminderWindowManager
    {
        private static ReminderListWindow? _instance;
        private static DispatcherTimer? _timer;
        private static bool _autoFollow = true;
        private static void CenterOverMain()
        {
            try
            {
                if (_instance == null) return;
                var mw = System.Windows.Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault();
                if (mw == null) return;
                var r = mw.GetVideoScreenRect();
                double w = _instance.ActualWidth > 1 ? _instance.ActualWidth : (_instance.Width > 1 ? _instance.Width : 640);
                double h = _instance.ActualHeight > 1 ? _instance.ActualHeight : (_instance.Height > 1 ? _instance.Height : 420);
                _instance.Left = r.Left + (r.Width - w) / 2;
                _instance.Top = r.Top + (r.Height - h) / 2;
            }
            catch { }
        }
        public static void OpenOrActivate()
        {
            try
            {
                if (_instance != null && _instance.IsVisible)
                {
                    CenterOverMain();
                    _instance.Topmost = true;
                    _instance.Activate();
                    return;
                }
            }
            catch { }
            try
            {
                _instance = new ReminderListWindow();
                _instance.Closed += (s, e) => { _instance = null; try { _timer?.Stop(); } catch { } };
                _instance.Loaded += (s, e) => CenterOverMain();
                _instance.SizeChanged += (s, e) => { if (_autoFollow) CenterOverMain(); };
                _instance.LocationChanged += (s, e) => { _autoFollow = false; };
                CenterOverMain();
                bool isFs = false;
                try
                {
                    var fs = System.Windows.Application.Current?.Windows.OfType<FullscreenWindow>().FirstOrDefault();
                    if (fs != null && fs.IsVisible)
                    {
                        _instance.Owner = fs; isFs = true;
                    }
                    else
                    {
                        var mw = System.Windows.Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault();
                        if (mw != null) _instance.Owner = mw;
                    }
                }
                catch { }
                _instance.Show();
                // 全屏时保证置顶，其它情况下不强制 Topmost，避免盖住设置窗口
                _instance.Topmost = isFs || _instance.Owner == null;
                try
                {
                    var settings = System.Windows.Application.Current?.Windows.OfType<SettingsWindow>().FirstOrDefault();
                    if (settings != null && settings.IsVisible) settings.Activate();
                }
                catch { }
                try
                {
                    _instance.Closed += (s2, e2) =>
                    {
                        try
                        {
                            if (isFs)
                            {
                                var fs2 = System.Windows.Application.Current?.Windows.OfType<FullscreenWindow>().FirstOrDefault();
                                fs2?.Activate();
                            }
                            else
                            {
                                var mw2 = System.Windows.Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault();
                                mw2?.Activate();
                            }
                        }
                        catch { }
                    };
                }
                catch { }
                _timer = _timer ?? new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(250) };
                _timer.Tick -= Timer_Tick;
                _timer.Tick += Timer_Tick;
                _autoFollow = true;
                _timer.Start();
            }
            catch { }
        }
        private static void Timer_Tick(object? sender, System.EventArgs e)
        {
            try
            {
                if (_instance == null || !_instance.IsVisible) return;
                if (_autoFollow) CenterOverMain();
                var fs = System.Windows.Application.Current?.Windows.OfType<FullscreenWindow>().FirstOrDefault();
                _instance.Topmost = (fs != null && fs.IsVisible) || _instance.Owner == null;
            }
            catch { }
        }
    }
}
