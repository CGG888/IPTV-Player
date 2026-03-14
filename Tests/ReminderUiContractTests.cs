using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LibmpvIptvClient.Tests
{
    [TestClass]
    public class ReminderUiContractTests
    {
        [TestMethod]
        public void Success_NoPlayButton_Present()
        {
            WpfTestHost.Invoke(() =>
            {
                var win = new ReminderToastWindow("", "", "p", DateTime.Now, null, false);
                win.Show();
                var playBtn = (System.Windows.Controls.Button)win.FindName("BtnPlay");
                Assert.AreEqual(System.Windows.Visibility.Collapsed, playBtn.Visibility);
                win.Close();
            });
        }

        [TestMethod]
        public void Due_PlayButton_Visible()
        {
            WpfTestHost.Invoke(() =>
            {
                var win = new ReminderToastWindow("", "", "p", DateTime.Now, null, true);
                win.Show();
                var playBtn = (System.Windows.Controls.Button)win.FindName("BtnPlay");
                Assert.AreEqual(System.Windows.Visibility.Visible, playBtn.Visibility);
                win.Close();
            });
        }

        [TestMethod]
        public void Countdown_Label_Format()
        {
            WpfTestHost.Invoke(() =>
            {
                var win = new ReminderToastWindow("", "", "p", DateTime.Now, null, false);
                var tb = (System.Windows.Controls.TextBlock)win.FindName("TxtCountdown");
                Assert.IsTrue(tb.Text.Contains("剩余"));
                win.Close();
            });
        }
    }
}
