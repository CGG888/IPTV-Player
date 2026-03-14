using Microsoft.VisualStudio.TestTools.UnitTesting;
using LibmpvIptvClient;

namespace LibmpvIptvClient.Tests
{
    [TestClass]
    public class PlaybackSettingsTests
    {
        [TestMethod]
        public void DefaultValues_ShouldBeCorrect()
        {
            var settings = new PlaybackSettings();
            
            // EPG
            Assert.IsTrue(settings.Epg.Enabled);
            Assert.AreEqual("", settings.Epg.Url);
            Assert.AreEqual(24, settings.Epg.RefreshIntervalHours);
            
            // Logo
            Assert.IsTrue(settings.Logo.Enabled);
            Assert.AreEqual("", settings.Logo.Url);
            
            // Replay
            Assert.IsTrue(settings.Replay.Enabled);
            Assert.AreEqual("", settings.Replay.UrlFormat);
            Assert.AreEqual(72, settings.Replay.DurationHours);
            
            // Timeshift
            Assert.IsTrue(settings.Timeshift.Enabled);
            Assert.AreEqual("", settings.Timeshift.UrlFormat);
            Assert.AreEqual(6, settings.Timeshift.DurationHours);
        }

        [TestMethod]
        public void CompatibilityProperties_ShouldMapToNewConfig()
        {
            var settings = new PlaybackSettings();
            
            settings.CustomEpgUrl = "http://epg.com";
            Assert.AreEqual("http://epg.com", settings.Epg.Url);
            
            settings.Epg.Url = "http://new.com";
            Assert.AreEqual("http://new.com", settings.CustomEpgUrl);
            
            settings.CustomLogoUrl = "http://logo.com";
            Assert.AreEqual("http://logo.com", settings.Logo.Url);
            
            settings.TimeshiftHours = 12;
            Assert.AreEqual(12, settings.Timeshift.DurationHours);
        }
    }
}
