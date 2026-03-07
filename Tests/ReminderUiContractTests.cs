using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LibmpvIptvClient.Tests
{
    [TestClass]
    public class ReminderUiContractTests
    {
        [TestMethod]
        public void Success_NoPlayButton_Present()
        {
            var win = new ReminderToastWindow("", "", "p", System.DateTime.Now, null, false);
            win.Show();
            var playBtn = win.FindName("BtnPlay");
            Assert.IsNull(playBtn);
            win.Close();
        }

        [TestMethod]
        public void Due_PlayButton_Visible()
        {
            var win = new ReminderToastWindow("", "", "p", System.DateTime.Now, null, true);
            win.Show();
            var playBtn = (System.Windows.Controls.Button)win.FindName("BtnPlay");
            Assert.AreEqual(System.Windows.Visibility.Visible, playBtn.Visibility);
            win.Close();
        }

        [TestMethod]
        public void Countdown_Label_Format()
        {
            var win = new ReminderToastWindow("", "", "p", System.DateTime.Now, null, false);
            var tb = (System.Windows.Controls.TextBlock)win.FindName("TxtCountdown");
            Assert.IsTrue(tb.Text.Contains("剩余"));
            win.Close();
        }
    }
}
