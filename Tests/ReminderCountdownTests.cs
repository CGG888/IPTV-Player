using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace LibmpvIptvClient.Tests
{
    [TestClass]
    public class ReminderCountdownTests
    {
        [TestMethod]
        public void Countdown_TenSeconds_Elapses()
        {
            var win = new ReminderToastWindow("", "", "p", System.DateTime.Now, null);
            win.Show();
            Thread.Sleep(11000);
            Assert.IsFalse(win.IsVisible);
        }
    }
}
