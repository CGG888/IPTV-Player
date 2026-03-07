using System;
using System.Runtime.InteropServices;
using System.Text;
using LibmpvIptvClient.Diagnostics;
using System.Globalization;

namespace LibmpvIptvClient
{
    public class MpvInterop : IDisposable
    {
        IntPtr _handle = IntPtr.Zero;
        PlaybackSettings _settings = AppSettings.Current;
        public void Create()
        {
            _handle = mpv_create();
        }
        public void SetSettings(PlaybackSettings s) { _settings = s; }
        public void Initialize()
        {
            mpv_initialize(_handle);
            SetFlag("keep-open", true);
            SetFlag("idle", true);
            SetString("ytdl", "no");
            SetString("hwdec", _settings.Hwdec ? "d3d11va" : "no");
            SetString("gpu-api", "d3d11");
            var threads = Math.Max(2, Environment.ProcessorCount / 2);
            SetString("vd-lavc-threads", threads.ToString(System.Globalization.CultureInfo.InvariantCulture));
            // 确保音频输出正常
            SetString("mute", "no");
            SetString("audio", "yes");
            SetString("audio-device", "auto");
            SetString("ad-lavc-threads", "2");
            SetString("audio-channels", "stereo");
            SetString("ad-lavc-downmix", "yes");
            SetString("audio-pitch-correction", "yes");
            
            // 设置全局通用 User-Agent，解决部分源因空 UA 拒绝访问的问题
            SetString("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            // 忽略 HTTPS 证书错误，解决部分自签名源无法播放的问题
            SetString("tls-verify", "no");
            // 用户语言偏好与网络超时
            if (!string.IsNullOrWhiteSpace(_settings.Alang)) SetString("alang", _settings.Alang);
            if (!string.IsNullOrWhiteSpace(_settings.Slang)) SetString("slang", _settings.Slang);
            if (_settings.MpvNetworkTimeoutSec > 0) SetString("network-timeout", _settings.MpvNetworkTimeoutSec.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Logger.Debug($"[mpv] init alang={_settings.Alang} slang={_settings.Slang} net_to={_settings.MpvNetworkTimeoutSec}");
        }
        public void SetSpeed(double speed)
        {
            SetDouble("speed", speed);
        }
        public void SetWid(IntPtr hwnd)
        {
            var prop = "wid";
            var val = BitConverter.GetBytes(hwnd.ToInt64());
            mpv_set_property(_handle, prop, mpv_format.MPV_FORMAT_INT64, val);
        }
        void SetFlag(string name, bool value)
        {
            var data = BitConverter.GetBytes(value ? 1 : 0);
            mpv_set_property(_handle, name, mpv_format.MPV_FORMAT_FLAG, data);
        }
        public void Mute(bool mute)
        {
            SetFlag("mute", mute);
        }
        void SetString(string name, string value)
        {
            mpv_set_property_string(_handle, name, value);
        }
        void SetupProtocolOptions(string url)
        {
            var u = url.ToLowerInvariant();
            
            // 1. RTSP Special Handling
            if (u.StartsWith("rtsp://"))
            {
                SetString("rtsp-transport", "tcp"); // Force TCP for stability
                SetString("user-agent", "VLC/3.0.18Libmpv"); // Fake UA
                // Enable cache for RTSP to allow smoother playback
                SetString("cache", _settings.CacheSecs > 0 ? "yes" : "no");
                if (_settings.CacheSecs > 0)
                    SetString("cache-secs", _settings.CacheSecs.ToString(System.Globalization.CultureInfo.InvariantCulture));
                // RTSP doesn't need forced demuxer, let ffmpeg handle it
                SetString("demuxer-lavf-format", ""); 
                SetString("demuxer-lavf-probesize", "32"); // Fast probe
                SetString("demuxer-lavf-analyzeduration", "0");
                Logger.Debug($"[mpv] rtsp cache={(_settings.CacheSecs>0?"yes":"no")} cache-secs={_settings.CacheSecs} probesize=32 adur=0 url={url}");
                return;
            }

            // 2. TS / MPEG-TS / UDP Handling
            var looksTs = u.Contains("/rtp/") || u.EndsWith(".ts") || u.Contains("proto=http") || u.StartsWith("udp://");
            if (looksTs)
            {
                SetString("demuxer", "lavf");
                SetString("demuxer-lavf-format", "mpegts");
                SetString("demuxer-lavf-probesize", "32"); // 极小 probesize，加速首帧
                SetString("demuxer-lavf-analyzeduration", "0"); // 禁用分析时长，加速首帧
                SetString("demuxer-lavf-buffersize", "128000"); // 减小缓冲区
                if (u.StartsWith("udp://"))
                {
                    SetString("cache", "no");
                    SetString("demuxer-max-back-bytes", "0");
                    Logger.Debug($"[mpv] udp-ts demux=mpegts cache=no max-back=0 probesize=32 adur=0 url={url}");
                }
                else
                {
                    SetString("cache", _settings.CacheSecs > 0 ? "yes" : "no");
                    if (_settings.CacheSecs > 0)
                        SetString("cache-secs", _settings.CacheSecs.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    SetString("demuxer-max-bytes", $"{_settings.DemuxerMaxBytesMiB}MiB");
                    SetString("demuxer-max-back-bytes", $"{_settings.DemuxerMaxBackBytesMiB}MiB");
                    Logger.Debug($"[mpv] http-ts demux=mpegts cache={( _settings.CacheSecs>0?"yes":"no")} cache-secs={_settings.CacheSecs} max={_settings.DemuxerMaxBytesMiB}MiB back={_settings.DemuxerMaxBackBytesMiB}MiB probesize=32 adur=0 url={url}");
                }
            }
            else
            {
                // 清空强制格式，恢复自动探测
                SetString("demuxer-lavf-format", "");
                // Ensure default cache settings for other protocols (like http/hls)
                SetString("cache", "yes");
                Logger.Debug($"[mpv] generic http cache=yes url={url}");
            }
            // 3. HLS 自适应（仅在启用时）
            if (_settings.EnableProtocolAdaptive && (u.Contains(".m3u8") || u.Contains("format=hls")))
            {
                if (_settings.HlsStartAtLiveEdge) SetString("hls-playlist-start", "no");
                if (_settings.HlsReadaheadSecs > 0)
                    SetString("demuxer-readahead-secs", _settings.HlsReadaheadSecs.ToString(CultureInfo.InvariantCulture));
                Logger.Debug($"[mpv] hls opts start_live={_settings.HlsStartAtLiveEdge} readahead={_settings.HlsReadaheadSecs} url={url}");
            }
        }
        public string? GetString(string name)
        {
            var p = mpv_get_property_string(_handle, name);
            if (p == IntPtr.Zero) return null;
            try { return Marshal.PtrToStringAnsi(p); }
            finally { mpv_free(p); }
        }
        public int? GetInt(string name)
        {
            var s = GetString(name);
            if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            return null;
        }
        public double? GetDouble(string name)
        {
            var s = GetString(name);
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            return null;
        }
        public void LoadFile(string url)
        {
            SetupProtocolOptions(url);
            var args = new string[] { "loadfile", url, null! };
            mpv_command(_handle, args);
            Logger.Log("mpv loadfile 调用完成");
        }
        public void LoadWithPrefetch(string url, string? nextUrl)
        {
            SetFlag("prefetch-playlist", true);
            var clear = new string[] { "playlist-clear", null! };
            mpv_command(_handle, clear);
            SetupProtocolOptions(url);
            var loadCurrent = new string[] { "loadfile", url, "replace", null! };
            mpv_command(_handle, loadCurrent);
            if (!string.IsNullOrWhiteSpace(nextUrl))
            {
                SetupProtocolOptions(nextUrl!);
                var loadNext = new string[] { "loadfile", nextUrl!, "append-play", null! };
                mpv_command(_handle, loadNext);
                Logger.Log("已预取下一频道");
            }
            Logger.Log("mpv loadfile + prefetch 调用完成");
        }
        public void LoadWithPrefetch(string url, System.Collections.Generic.IEnumerable<string> nextUrls)
        {
            SetFlag("prefetch-playlist", true);
            var clear = new string[] { "playlist-clear", null! };
            mpv_command(_handle, clear);
            SetupProtocolOptions(url);
            var loadCurrent = new string[] { "loadfile", url, "replace", null! };
            mpv_command(_handle, loadCurrent);
            foreach (var n in nextUrls)
            {
                if (string.IsNullOrWhiteSpace(n)) continue;
                SetupProtocolOptions(n);
                var loadNext = new string[] { "loadfile", n, "append-play", null! };
                mpv_command(_handle, loadNext);
            }
            Logger.Log("mpv loadfile + multi-prefetch 调用完成");
        }
        public void Pause(bool pause)
        {
            SetFlag("pause", pause);
        }
        public void SeekRelative(double seconds)
        {
            var args = new string[] { "seek", seconds.ToString(CultureInfo.InvariantCulture), "relative", null! };
            mpv_command(_handle, args);
        }
        public void SeekAbsolute(double seconds)
        {
            var args = new string[] { "seek", seconds.ToString(CultureInfo.InvariantCulture), "absolute", null! };
            mpv_command(_handle, args);
        }
        public void SetVolume(double volume)
        {
            SetString("volume", volume.ToString(CultureInfo.InvariantCulture));
        }
        public void SetAspectRatio(string ratio)
        {
            // ratio: "default", "16:9", "4:3", "stretch", "fill", "crop"
            switch (ratio.ToLowerInvariant())
            {
                case "16:9":
                    SetString("video-aspect-override", "16:9");
                    SetString("keepaspect", "yes");
                    SetDouble("panscan", 0.0);
                    break;
                case "4:3":
                    SetString("video-aspect-override", "4:3");
                    SetString("keepaspect", "yes");
                    SetDouble("panscan", 0.0);
                    break;
                case "stretch":
                    SetString("video-aspect-override", "-1");
                    SetString("keepaspect", "no");
                    SetDouble("panscan", 0.0);
                    break;
                case "fill":
                    SetString("video-aspect-override", "-1");
                    SetString("keepaspect", "yes");
                    SetDouble("panscan", 1.0);
                    break;
                case "crop":
                    SetString("video-aspect-override", "-1");
                    SetString("keepaspect", "yes");
                    SetDouble("panscan", 1.0); // panscan=1.0 is crop/fill
                    break;
                default: // default
                    SetString("video-aspect-override", "-1");
                    SetString("keepaspect", "yes");
                    SetDouble("panscan", 0.0);
                    break;
            }
        }
        void SetDouble(string name, double value)
        {
            mpv_set_property(_handle, name, mpv_format.MPV_FORMAT_DOUBLE, BitConverter.GetBytes(value));
        }
        public void SetFullscreen(bool on)
        {
            SetFlag("fullscreen", on);
        }
        public double? GetTimePos()
        {
            return GetPropertyDouble("time-pos");
        }
        public double? GetDuration()
        {
            return GetPropertyDouble("duration");
        }
        double? GetPropertyDouble(string name)
        {
            var p = mpv_get_property_string(_handle, name);
            if (p == IntPtr.Zero) return null;
            try
            {
                var s = Marshal.PtrToStringAnsi(p);
                if (string.IsNullOrEmpty(s)) return null;
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
                return null;
            }
            finally
            {
                mpv_free(p);
            }
        }
        public void Stop()
        {
            var args = new string[] { "stop", null! };
            mpv_command(_handle, args);
        }
        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                mpv_terminate_destroy(_handle);
                _handle = IntPtr.Zero;
            }
        }
        enum mpv_format
        {
            MPV_FORMAT_NONE = 0,
            MPV_FORMAT_STRING = 1,
            MPV_FORMAT_OSD_STRING = 2,
            MPV_FORMAT_FLAG = 3,
            MPV_FORMAT_INT64 = 4,
            MPV_FORMAT_DOUBLE = 5,
            MPV_FORMAT_NODE = 6,
            MPV_FORMAT_NODE_ARRAY = 7,
            MPV_FORMAT_NODE_MAP = 8,
            MPV_FORMAT_BYTE_ARRAY = 9
        }
        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr mpv_create();
        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int mpv_initialize(IntPtr ctx);
        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int mpv_set_property(IntPtr ctx, string name, mpv_format format, byte[] data);
        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int mpv_set_property_string(IntPtr ctx, string name, string data);
        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int mpv_command(IntPtr ctx, string[] args);
        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr mpv_get_property_string(IntPtr ctx, string name);
        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void mpv_free(IntPtr data);
        [DllImport("libmpv-2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void mpv_terminate_destroy(IntPtr ctx);
    }
}
