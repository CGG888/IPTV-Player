using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LibmpvIptvClient.Diagnostics;
using LibmpvIptvClient.Models;

namespace LibmpvIptvClient.Services
{
    public static class DnsPrefetcher
    {
        static readonly ConcurrentDictionary<string, DateTime> _seen = new(StringComparer.OrdinalIgnoreCase);
        static readonly SemaphoreSlim _gate = new(1, 1);
        static int _maxParallel = Math.Max(2, Environment.ProcessorCount / 2);

        public static void PrefetchForChannels(IEnumerable<Channel> channels, int maxHosts = 40)
        {
            if (channels == null) return;
            var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ch in channels)
            {
                if (ch?.Sources == null) continue;
                foreach (var s in ch.Sources)
                {
                    var h = ExtractHost(s?.Url);
                    if (!string.IsNullOrEmpty(h)) hosts.Add(h);
                    if (hosts.Count >= maxHosts) break;
                }
                if (hosts.Count >= maxHosts) break;
            }
            if (hosts.Count == 0) return;
            Task.Run(() => PrefetchHostsAsync(hosts));
        }

        public static void PrefetchForUrls(IEnumerable<string> urls)
        {
            if (urls == null) return;
            var hosts = new HashSet<string>(urls.Select(ExtractHost).Where(h => !string.IsNullOrEmpty(h))!, StringComparer.OrdinalIgnoreCase);
            if (hosts.Count == 0) return;
            Task.Run(() => PrefetchHostsAsync(hosts));
        }

        static async Task PrefetchHostsAsync(HashSet<string> hosts)
        {
            try
            {
                var toResolve = hosts.Where(h => !_seen.ContainsKey(h)).ToArray();
                if (toResolve.Length == 0) return;
                using var throttler = new SemaphoreSlim(_maxParallel, _maxParallel);
                var tasks = new List<Task>();
                foreach (var h in toResolve)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await throttler.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            // Skip literals (IPs)
                            if (IPAddress.TryParse(h, out _)) { _seen[h] = DateTime.UtcNow; return; }
                            var addrs = await Dns.GetHostAddressesAsync(h).ConfigureAwait(false);
                            _seen[h] = DateTime.UtcNow;
                            if (addrs != null && addrs.Length > 0)
                                Logger.Debug($"[DNS] 预解析 {h} -> {string.Join(",", addrs.Select(a => a.ToString()).Take(3))}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"[DNS] 预解析失败 {h}: {ex.Message}");
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch { }
        }

        static string? ExtractHost(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                if (url.StartsWith("udp://", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("rtp://", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("srt://", StringComparison.OrdinalIgnoreCase)) return null;
                if (Uri.TryCreate(url, UriKind.Absolute, out var u))
                {
                    return u.Host;
                }
            }
            catch { }
            return null;
        }
    }
}
