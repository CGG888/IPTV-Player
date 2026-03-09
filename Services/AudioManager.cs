using System;
using System.Collections.Concurrent;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LibmpvIptvClient.Services
{
    public class AudioManager : IDisposable
    {
        public static AudioManager Instance { get; } = new AudioManager();
        readonly ConcurrentDictionary<string, MediaPlayer> _players = new ConcurrentDictionary<string, MediaPlayer>(StringComparer.OrdinalIgnoreCase);
        double _volume = 0.8;
        public double Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Max(0, Math.Min(1.0, value));
                foreach (var kv in _players) { try { kv.Value.Volume = _volume; } catch { } }
            }
        }
        string ResolveWindowsMedia(string name)
        {
            try
            {
                var media = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                var path = Path.Combine(media, "Media", name);
                if (File.Exists(path)) return path;
            }
            catch { }
            return "";
        }
        public void PreloadDefaults()
        {
            // Map logical keys to Windows default sounds if available
            RegisterIfMissing("appointment", ResolveWindowsMedia("Windows Notify Calendar.wav"));
            RegisterIfMissing("program_start", ResolveWindowsMedia("Windows Notify System Generic.wav"));
            RegisterIfMissing("record_start", ResolveWindowsMedia("Windows Print complete.wav"));
            RegisterIfMissing("record_stop", ResolveWindowsMedia("Windows Hardware Remove.wav"));
            RegisterIfMissing("upload_done", ResolveWindowsMedia("Windows User Account Control.wav"));
            RegisterIfMissing("download_done", ResolveWindowsMedia("Windows Notify Email.wav"));
            RegisterIfMissing("upload_queued", ResolveWindowsMedia("Windows Notify Messaging.wav"));
        }
        void RegisterIfMissing(string key, string file)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file)) return;
            if (_players.ContainsKey(key)) return;
            try
            {
                var p = new MediaPlayer();
                p.Open(new Uri(file));
                p.Volume = _volume;
                _players[key] = p;
            }
            catch { }
        }
        public void Register(string key, string file)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(file) || !File.Exists(file)) return;
            try
            {
                var p = new MediaPlayer();
                p.Open(new Uri(file));
                p.Volume = _volume;
                _players[key] = p;
            }
            catch { }
        }
        public void Play(string key)
        {
            try
            {
                if (_players.TryGetValue(key, out var p))
                {
                    p.Position = TimeSpan.Zero;
                    p.Volume = _volume;
                    p.Play();
                }
                else
                {
                    // Fallback
                    SystemSounds.Asterisk.Play();
                }
            }
            catch
            {
                try { SystemSounds.Asterisk.Play(); } catch { }
            }
        }
        public void Stop(string key)
        {
            try
            {
                if (_players.TryGetValue(key, out var p)) p.Stop();
            }
            catch { }
        }
        public void Dispose()
        {
            foreach (var kv in _players)
            {
                try { kv.Value.Close(); } catch { }
            }
            _players.Clear();
        }
    }
}
