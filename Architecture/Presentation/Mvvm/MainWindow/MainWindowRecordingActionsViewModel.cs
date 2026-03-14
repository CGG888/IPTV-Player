using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibmpvIptvClient.Models;
using LibmpvIptvClient.Services;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow
{
    public sealed class MainWindowRecordingActionsViewModel : ViewModelBase
    {
        public bool ResolveRecordingEnabled(bool? appSettingsEnabled)
        {
            return appSettingsEnabled ?? true;
        }

        public string ResolveRecordingToolTip(bool recordingNow, Func<string, string, string> localizer)
        {
            return recordingNow 
                ? localizer("Overlay_RecordStop", "停止录播") 
                : localizer("Overlay_RecordNow", "实时录播");
        }

        public RecordingState BuildRecordingState(bool recordingNow, string filePath, DateTime? startUtc)
        {
            return new RecordingState(recordingNow, filePath, startUtc);
        }

        public string ResolveProgramTitle(
            Channel? currentChannel,
            EpgProgram? currentPlayingProgram,
            DateTime? programTime,
            Func<string?, string?, List<EpgProgram>?> getPrograms)
        {
            if (currentPlayingProgram != null && !string.IsNullOrWhiteSpace(currentPlayingProgram.Title))
                return currentPlayingProgram.Title;

            try
            {
                var focusTime = programTime ?? DateTime.Now;
                if (currentChannel != null)
                {
                    var progs = getPrograms(currentChannel.TvgId, currentChannel.Name);
                    var p = progs?.FirstOrDefault(x => x.Start <= focusTime && x.End > focusTime);
                    if (p != null && !string.IsNullOrWhiteSpace(p.Title)) return p.Title;
                }
            }
            catch { }
            return "";
        }

        public async Task<List<RecordingGroup>> LoadRecordingsLocalGroupedAsync(
            List<Channel> channels,
            EpgService epgService)
        {
            try
            {
                string Resolver(string channel, DateTime? start)
                {
                    try
                    {
                        var ch = channels?.FirstOrDefault(c => string.Equals(c.Name ?? "", channel ?? "", StringComparison.OrdinalIgnoreCase));
                        var progs = epgService?.GetPrograms(ch?.TvgId, channel);
                        if (progs != null && start.HasValue)
                        {
                            var p = progs.FirstOrDefault(x => x.Start <= start.Value && x.End > start.Value);
                            if (p != null && !string.IsNullOrWhiteSpace(p.Title)) return p.Title;
                        }
                    }
                    catch { }
                    return "";
                }
                TimeSpan? DurationResolver(string channel, DateTime? start)
                {
                    try
                    {
                        var ch = channels?.FirstOrDefault(c => string.Equals(c.Name ?? "", channel ?? "", StringComparison.OrdinalIgnoreCase));
                        var progs = epgService?.GetPrograms(ch?.TvgId, channel);
                        if (progs != null && start.HasValue)
                        {
                            var p = progs.FirstOrDefault(x => x.Start <= start.Value && x.End > start.Value);
                            if (p != null) return (p.End - start.Value);
                        }
                    }
                    catch { }
                    return null;
                }
                var svc = new RecordingIndexService();
                var groups = await svc.GetAllLocalGroupedAsync((ch, st) => Resolver(ch, st), (ch, st) => DurationResolver(ch, st));
                return groups ?? new List<RecordingGroup>();
            }
            catch
            {
                return new List<RecordingGroup>();
            }
        }

        public async Task<List<RecordingEntry>> MergeRemoteRecordingsAsync(
            List<RecordingGroup> groups,
            List<Channel> channels,
            EpgService epgService)
        {
            try
            {
                string Resolver(string channel, DateTime? start)
                {
                    try
                    {
                        var ch = channels?.FirstOrDefault(c => string.Equals(c.Name ?? "", channel ?? "", StringComparison.OrdinalIgnoreCase));
                        var progs = epgService?.GetPrograms(ch?.TvgId, channel);
                        if (progs != null && start.HasValue)
                        {
                            var p = progs.FirstOrDefault(x => x.Start <= start.Value && x.End > start.Value);
                            if (p != null && !string.IsNullOrWhiteSpace(p.Title)) return p.Title;
                        }
                    }
                    catch { }
                    return "";
                }
                TimeSpan? DurationResolver(string channel, DateTime? start)
                {
                    try
                    {
                        var ch = channels?.FirstOrDefault(c => string.Equals(c.Name ?? "", channel ?? "", StringComparison.OrdinalIgnoreCase));
                        var progs = epgService?.GetPrograms(ch?.TvgId, channel);
                        if (progs != null && start.HasValue)
                        {
                            var p = progs.FirstOrDefault(x => x.Start <= start.Value && x.End > start.Value);
                            if (p != null) return (p.End - start.Value);
                        }
                    }
                    catch { }
                    return null;
                }

                var svc = new RecordingIndexService();
                var channelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var g in groups) { if (!string.IsNullOrWhiteSpace(g.Channel)) channelNames.Add(g.Channel); }
                if (channels != null) foreach (var c in channels) { if (!string.IsNullOrWhiteSpace(c.Name)) channelNames.Add(c.Name!); }

                foreach (var ch in channelNames)
                {
                    try
                    {
                        var flat = await svc.GetForChannelAsync(ch, (st) => 
                        { 
                            try 
                            { 
                                var title = Resolver(ch, st); 
                                var dur = DurationResolver(ch, st); 
                                return (string.IsNullOrWhiteSpace(title) ? null : title, dur); 
                            } 
                            catch { return (null, null); } 
                        });
                        if (flat != null && flat.Count > 0)
                        {
                            var existing = groups.FirstOrDefault(x => string.Equals(x.Channel ?? "", ch ?? "", StringComparison.OrdinalIgnoreCase));
                            if (existing == null)
                            {
                                existing = new RecordingGroup { Channel = ch };
                                groups.Add(existing);
                            }
                            existing.Items = flat.OrderByDescending(x => x.StartTime ?? DateTime.MinValue).ToList();
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            return groups.SelectMany(g => g.Items ?? new List<RecordingEntry>()).ToList();
        }

        public async Task UpdateChannelRecordingsAsync(
            List<RecordingGroup> allGroups,
            string channelKey,
            List<Channel> channels,
            EpgService epgService)
        {
            try
            {
                var svc = new RecordingIndexService();
                (string? title, TimeSpan?) Resolver(DateTime? start)
                {
                    try
                    {
                        var ch = channelKey ?? "";
                        var progs = epgService?.GetPrograms(channels?.FirstOrDefault(c => string.Equals(c.Name ?? "", ch ?? "", StringComparison.OrdinalIgnoreCase))?.TvgId, ch);
                        if (progs != null && start.HasValue)
                        {
                            var p = progs.FirstOrDefault(x => x.Start <= start.Value && x.End > start.Value);
                            if (p != null) return (p.Title, (p.End - start.Value));
                        }
                    }
                    catch { }
                    return (null, null);
                }
                var list = await svc.GetForChannelAsync(channelKey ?? "", Resolver) ?? new List<RecordingEntry>();
                
                var grp = allGroups.FirstOrDefault(g => string.Equals(g.Channel ?? "", channelKey ?? "", StringComparison.OrdinalIgnoreCase));
                if (grp == null)
                {
                    grp = new RecordingGroup { Channel = channelKey ?? "" };
                    allGroups.Add(grp);
                }
                grp.Items = list.OrderByDescending(x => x.StartTime ?? DateTime.MinValue).ThenByDescending(x => x.SizeBytes).ToList();
            }
            catch { }
        }

        public RecordingFilterResult ApplyFilter(
            List<RecordingGroup> allGroups,
            string searchText,
            Func<string, string, string> countLocalizer)
        {
            try
            {
                var q = (searchText ?? "").Trim();
                bool hasQ = !string.IsNullOrWhiteSpace(q);
                Func<string, bool> match = s => string.IsNullOrWhiteSpace(q) ? true :
                    (s?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
                
                var filteredGroups = new List<RecordingGroup>();
                int total = 0;
                foreach (var g in allGroups)
                {
                    var items = hasQ ? g.Items.Where(it => match(it.Title) || match(it.PathOrUrl)).ToList() : g.Items.ToList();
                    if (items.Count > 0 || !hasQ)
                    {
                        filteredGroups.Add(new RecordingGroup { Channel = g.Channel, Items = items });
                        total += items.Count;
                    }
                }
                
                var countText = countLocalizer("Drawer_Records_Count", "共") + $" {total} " + countLocalizer("Drawer_Records_Unit", "个录播");
                return new RecordingFilterResult(filteredGroups, countText);
            }
            catch
            {
                return new RecordingFilterResult(new List<RecordingGroup>(), "");
            }
        }
    }

    public sealed class RecordingFilterResult
    {
        public List<RecordingGroup> Groups { get; }
        public string CountText { get; }

        public RecordingFilterResult(List<RecordingGroup> groups, string countText)
        {
            Groups = groups;
            CountText = countText;
        }
    }

    public sealed class RecordingState
    {
        public bool IsRecording { get; }
        public string FilePath { get; }
        public DateTime? StartUtc { get; }

        public RecordingState(bool isRecording, string filePath, DateTime? startUtc)
        {
            IsRecording = isRecording;
            FilePath = filePath;
            StartUtc = startUtc;
        }
    }
}
