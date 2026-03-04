using Xunit;
using LibmpvIptvClient;

namespace LibmpvIptvClient.Tests
{
    public class PlaybackSettingsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            var settings = new PlaybackSettings();
            
            // EPG
            Assert.True(settings.Epg.Enabled);
            Assert.Equal("", settings.Epg.Url);
            Assert.Equal(24, settings.Epg.RefreshIntervalHours);
            
            // Logo
            Assert.True(settings.Logo.Enabled);
            Assert.Equal("", settings.Logo.Url);
            
            // Replay
            Assert.True(settings.Replay.Enabled);
            Assert.Equal("", settings.Replay.UrlFormat);
            Assert.Equal(72, settings.Replay.DurationHours);
            
            // Timeshift
            Assert.True(settings.Timeshift.Enabled);
            Assert.Equal("", settings.Timeshift.UrlFormat);
            Assert.Equal(6, settings.Timeshift.DurationHours);
        }

        [Fact]
        public void CompatibilityProperties_ShouldMapToNewConfig()
        {
            var settings = new PlaybackSettings();
            
            settings.CustomEpgUrl = "http://epg.com";
            Assert.Equal("http://epg.com", settings.Epg.Url);
            
            settings.Epg.Url = "http://new.com";
            Assert.Equal("http://new.com", settings.CustomEpgUrl);
            
            settings.CustomLogoUrl = "http://logo.com";
            Assert.Equal("http://logo.com", settings.Logo.Url);
            
            settings.TimeshiftHours = 12;
            Assert.Equal(12, settings.Timeshift.DurationHours);
        }
    }
}
