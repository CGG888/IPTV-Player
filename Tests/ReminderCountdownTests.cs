using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Windows.Threading;

namespace LibmpvIptvClient.Tests
{
    [TestClass]
    public class ReminderCountdownTests
    {
        [TestMethod]
        public void Countdown_TenSeconds_Elapses()
        {
            Exception? error = null;
            bool? visibleAtEnd = null;
            var done = new ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                try
                {
                    var win = new ReminderToastWindow("", "", "p", DateTime.Now, null);
                    win.Show();
                    var endAt = DateTime.UtcNow.AddSeconds(12);
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                    timer.Tick += (_, __) =>
                    {
                        if (DateTime.UtcNow < endAt) return;
                        visibleAtEnd = win.IsVisible;
                        try { win.Close(); } catch { }
                        timer.Stop();
                        Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                    };
                    timer.Start();
                    Dispatcher.Run();
                }
                catch (Exception ex) { error = ex; }
                finally { done.Set(); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            done.Wait(TimeSpan.FromSeconds(20));
            if (error != null) throw error;
            Assert.AreEqual(false, visibleAtEnd);
        }
    }
}
