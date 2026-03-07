using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace LibmpvIptvClient.Helpers
{
    public static class ReminderWindowManager
    {
        private static ReminderListWindow? _instance;
        private static DispatcherTimer? _timer;
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
                _instance.SizeChanged += (s, e) => CenterOverMain();
                CenterOverMain();
                _instance.Show();
                _instance.Topmost = true;
                _timer = _timer ?? new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(250) };
                _timer.Tick -= Timer_Tick;
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }
            catch { }
        }
        private static void Timer_Tick(object? sender, System.EventArgs e)
        {
            try
            {
                if (_instance == null || !_instance.IsVisible) return;
                CenterOverMain();
                _instance.Topmost = true;
            }
            catch { }
        }
    }
}
