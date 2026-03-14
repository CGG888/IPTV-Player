using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed class SettingsCdnViewModel : ViewModelBase
{
    public ObservableCollection<string> CdnList { get; } = new();

    public void Load(IEnumerable<string>? items)
    {
        CdnList.Clear();
        if (items != null)
        {
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    CdnList.Add(item);
                }
            }
        }
    }

    public void AddCdn(string url)
    {
        var t = (url ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(t) || !t.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
        
        // Avoid duplicates (simple check against raw string)
        if (!CdnList.Contains(t))
        {
            CdnList.Add(t);
        }
    }

    public void RemoveCdn(System.Collections.IList selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;
        
        var toRemove = new List<string>();
        foreach (var item in selectedItems)
        {
            if (item is string s) toRemove.Add(s);
        }
        
        foreach (var s in toRemove)
        {
            CdnList.Remove(s);
        }
    }

    public void MoveUp(string? item)
    {
        if (string.IsNullOrEmpty(item)) return;
        var idx = CdnList.IndexOf(item);
        if (idx > 0)
        {
            CdnList.Move(idx, idx - 1);
        }
    }

    public void MoveDown(string? item)
    {
        if (string.IsNullOrEmpty(item)) return;
        var idx = CdnList.IndexOf(item);
        if (idx >= 0 && idx < CdnList.Count - 1)
        {
            CdnList.Move(idx, idx + 1);
        }
    }

    public async Task RunSpeedTestAsync()
    {
        if (CdnList.Count == 0) return;

        var targets = new List<(string url, long ms)>();
        using var ping = new Ping();

        foreach (var c in CdnList)
        {
            // Clean old ping results (e.g. "url [100ms]")
            var cleanUrl = c.Split(' ')[0];
            long rtt = long.MaxValue;
            try
            {
                string host = cleanUrl;
                if (Uri.TryCreate(cleanUrl, UriKind.Absolute, out var uri))
                {
                    host = uri.Host;
                }

                var reply = await ping.SendPingAsync(host, 3000).ConfigureAwait(true);
                if (reply.Status == IPStatus.Success)
                {
                    rtt = reply.RoundtripTime;
                }
            }
            catch { }
            targets.Add((cleanUrl, rtt));
        }

        // Sort by latency
        targets.Sort((a, b) => a.ms.CompareTo(b.ms));

        // Update list
        CdnList.Clear();
        foreach (var t in targets)
        {
            string display;
            if (t.ms == long.MaxValue)
                display = $"{t.url} [Failed]";
            else
                display = $"{t.url} [{t.ms}ms]";
            CdnList.Add(display);
        }
    }

    public List<string> GetCleanList()
    {
        var cleanList = new List<string>();
        foreach (var item in CdnList)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            var parts = item.Split(' '); // Remove [xxxms]
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                cleanList.Add(parts[0].Trim());
            }
        }
        return cleanList;
    }
}
