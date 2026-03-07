using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using LibmpvIptvClient.Diagnostics;

namespace LibmpvIptvClient.Services
{
    public class ReminderService : IDisposable
    {
        private static readonly Lazy<ReminderService> _lazy = new Lazy<ReminderService>(() => new ReminderService());
        public static ReminderService Instance => _lazy.Value;
        private readonly System.Timers.Timer _timer = new System.Timers.Timer(1000) { AutoReset = false };
        private const int GraceSeconds = 120;
        private List<ScheduledReminder> _list = new List<ScheduledReminder>();

        private ReminderService()
        {
            _timer.Elapsed += (_, __) => Tick();
        }

        public void Start()
        {
            try
            {
                _list = AppSettings.Current.ScheduledReminders ?? new List<ScheduledReminder>();
            }
            catch { _list = new List<ScheduledReminder>(); }
            // 先处理已经到点或刚过点（宽限内）的预约，然后再安排下一次
            ProcessDue(includeGrace: true);
            ScheduleNext();
        }

        public void Import(IEnumerable<ScheduledReminder> items)
        {
            if (items == null) return;
            var map = _list.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var it in items)
            {
                if (it == null || string.IsNullOrWhiteSpace(it.Id)) continue;
                map[it.Id] = it;
            }
            _list = map.Values.ToList();
            AppSettings.Current.ScheduledReminders = _list;
            AppSettings.Current.Save();
            ScheduleNext();
        }

        void ScheduleNext()
        {
            try
            {
                var now = DateTime.UtcNow;
                var next = _list.Where(x => x.Enabled && !x.Completed && x.StartAtUtc.AddSeconds(-(x.PreAlertSeconds)) > now)
                                .OrderBy(x => x.StartAtUtc.AddSeconds(-x.PreAlertSeconds))
                                .FirstOrDefault();
                if (next == null) { _timer.Stop(); return; }
                var due = next.StartAtUtc.AddSeconds(-next.PreAlertSeconds);
                var ms = Math.Max(500, (int)(due - now).TotalMilliseconds);
                _timer.Interval = ms;
                _timer.Start();
                try
                {
                    LibmpvIptvClient.Diagnostics.Logger.Info($"[Reminder] next={due.ToLocalTime():yyyy-MM-dd HH:mm:ss} count={_list.Count(r=>r.Enabled && !r.Completed)}");
                }
                catch { }
            }
            catch { }
        }

        void Tick()
        {
            try
            {
                ProcessDue(includeGrace: false);
            }
            catch { }
            ScheduleNext();
        }
        void ProcessDue(bool includeGrace)
        {
            var now = DateTime.UtcNow;
            int ok = 0, miss = 0;
            foreach (var r in _list.Where(x => x.Enabled && !x.Completed).OrderBy(x => x.StartAtUtc))
            {
                var triggerAt = r.StartAtUtc.AddSeconds(-r.PreAlertSeconds);
                if (triggerAt <= now)
                {
                    var delta = (now - triggerAt).TotalSeconds;
                    if (!includeGrace && delta > 1) continue; // 非宽限模式，仅处理“到点”/极近的
                    if (includeGrace && delta > GraceSeconds) { r.Completed = true; miss++; continue; }
                    try
                    {
                        var local = r.StartAtUtc.ToLocalTime();
                        string? logoLocal = null;
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(r.ChannelLogo))
                            {
                                if (System.IO.File.Exists(r.ChannelLogo)) logoLocal = r.ChannelLogo;
                                else logoLocal = LogoCacheService.Instance.GetOrDownloadAsync(r.ChannelLogo).GetAwaiter().GetResult();
                            }
                        }
                        catch { }
                        try
                        {
                            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                try
                                {
                                    var toast = new LibmpvIptvClient.ReminderToastWindow(r.ChannelId ?? "", r.ChannelName ?? "", r.Note ?? "", local, logoLocal, true, null);
                                    toast.Show();
                                }
                                catch
                                {
                                    NotificationService.Instance.ShowWithLogo(r.ChannelName ?? "", r.Note ?? "", local, logoLocal, 8000);
                                }
                            });
                        }
                        catch
                        {
                            NotificationService.Instance.ShowWithLogo(r.ChannelName ?? "", r.Note ?? "", local, logoLocal, 8000);
                        }
                        r.Completed = true; ok++;
                    }
                    catch { }
                }
            }
            try
            {
                if (ok + miss > 0)
                {
                    LibmpvIptvClient.Diagnostics.Logger.Info($"[Reminder] summary ok={ok} missed={miss}");
                }
            }
            catch { }
            AppSettings.Current.ScheduledReminders = _list;
            AppSettings.Current.Save();
        }

        public void Dispose()
        {
            try { _timer.Stop(); _timer.Dispose(); } catch { }
        }
    }
}
