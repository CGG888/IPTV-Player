using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LibmpvIptvClient
{
    public class M3uSource
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IsSelected { get; set; }
    }

    public class PlaybackSettings
    {
        public bool Hwdec { get; set; } = true;
        public double CacheSecs { get; set; } = 1.0;
        public int DemuxerMaxBytesMiB { get; set; } = 16;
        public int DemuxerMaxBackBytesMiB { get; set; } = 4;
        public int FccPrefetchCount { get; set; } = 2;
        public bool EnableUdpOptimization { get; set; } = false; // Added
        public int SourceTimeoutSec { get; set; } = 3;
        public string CustomEpgUrl { get; set; } = "";
        public string CustomLogoUrl { get; set; } = ""; // Custom Logo Repo URL (e.g. http://site.com/{name}.png)
        public List<M3uSource> SavedSources { get; set; } = new List<M3uSource>();

        public static PlaybackSettings Load()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_settings.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var obj = JsonSerializer.Deserialize<PlaybackSettings>(json);
                    if (obj != null) return obj;
                }
            }
            catch { }
            return new PlaybackSettings();
        }

        public void Save()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_settings.json");
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }

    public static class AppSettings
    {
        public static PlaybackSettings Current { get; set; } = PlaybackSettings.Load();
    }
}