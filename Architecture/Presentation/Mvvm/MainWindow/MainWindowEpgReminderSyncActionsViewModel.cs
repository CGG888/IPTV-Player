using System;
using System.Collections.Generic;
using System.Linq;
using LibmpvIptvClient;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;
using LibmpvIptvClient.Models;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed class MainWindowEpgReminderSyncActionsViewModel : ViewModelBase
{
    public bool SyncBookedFlags(IEnumerable<EpgProgram> items, string channelKey, IReadOnlyList<ScheduledReminder>? reminders)
    {
        var changed = false;
        if (string.IsNullOrEmpty(channelKey))
        {
            foreach (var p in items)
            {
                if (p.IsBooked)
                {
                    p.IsBooked = false;
                    changed = true;
                }
            }
            return changed;
        }

        foreach (var p in items)
        {
            var booked = reminders != null && reminders.Any(r =>
                r.Enabled && !r.Completed &&
                string.Equals(r.ChannelId ?? "", channelKey ?? "", StringComparison.OrdinalIgnoreCase) &&
                Math.Abs((r.StartAtUtc - p.Start.ToUniversalTime()).TotalSeconds) <= 60);
            if (p.IsBooked != booked)
            {
                p.IsBooked = booked;
                changed = true;
            }
        }

        return changed;
    }
}
