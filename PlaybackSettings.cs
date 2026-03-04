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

    public class EpgConfig
    {
        public bool Enabled { get; set; } = true;
        public string Url { get; set; } = "";
        public double RefreshIntervalHours { get; set; } = 24;
        public bool EnableSmartMatch { get; set; } = true; // Added
    }

    public class LogoConfig
    {
        public bool Enabled { get; set; } = true;
        public string Url { get; set; } = "";
    }

    public class ReplayConfig
    {
        public bool Enabled { get; set; } = true;
        public string UrlFormat { get; set; } = "";
        public int DurationHours { get; set; } = 72;
    }

    public class TimeshiftConfig
    {
        public bool Enabled { get; set; } = true;
        public string UrlFormat { get; set; } = "";
        public int DurationHours { get; set; } = 6;
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
        
        public EpgConfig Epg { get; set; } = new EpgConfig();
        public LogoConfig Logo { get; set; } = new LogoConfig();
        public ReplayConfig Replay { get; set; } = new ReplayConfig();
        public TimeshiftConfig Timeshift { get; set; } = new TimeshiftConfig();

        // Compatibility Properties (Deprecated)
        [System.Text.Json.Serialization.JsonIgnore]
        public string CustomEpgUrl { get => Epg.Url; set => Epg.Url = value; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string CustomLogoUrl { get => Logo.Url; set => Logo.Url = value; }
        [System.Text.Json.Serialization.JsonIgnore]
        public int TimeshiftHours { get => Timeshift.DurationHours; set => Timeshift.DurationHours = value; }

        public List<M3uSource> SavedSources { get; set; } = new List<M3uSource>();
        // 更新下载 CDN 持久化列表（仅前缀，例如 https://gh-proxy.org）
        public List<string> UpdateCdnMirrors { get; set; } = new List<string>();
        // 界面语言（例如 zh-CN / en-US；为空表示跟随系统）
        public string Language { get; set; } = "";
        // 主题模式：System/Light/Dark
        public string ThemeMode { get; set; } = "System";

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
