using System;
using System.Collections.Generic;
using System.Linq;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;
using LibmpvIptvClient.Models;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record EpgSelectionSyncResult(EpgProgram? CurrentPlayingProgram, bool ShouldRefresh);

public sealed class MainWindowEpgSelectionSyncActionsViewModel : ViewModelBase
{
    public EpgSelectionSyncResult ClearPlaying(EpgProgram? currentPlayingProgram, IEnumerable<EpgProgram>? items)
    {
        if (currentPlayingProgram == null)
        {
            return new EpgSelectionSyncResult(null, false);
        }

        currentPlayingProgram.IsPlaying = false;
        var shouldRefresh = items != null;
        return new EpgSelectionSyncResult(null, shouldRefresh);
    }

    public EpgSelectionSyncResult SetPlaying(EpgProgram? currentPlayingProgram, EpgProgram target, IEnumerable<EpgProgram>? items)
    {
        if (currentPlayingProgram != null && !ReferenceEquals(currentPlayingProgram, target))
        {
            currentPlayingProgram.IsPlaying = false;
        }

        target.IsPlaying = true;
        var shouldRefresh = items != null;
        return new EpgSelectionSyncResult(target, shouldRefresh);
    }

    public EpgProgram? ResolveSelectionTarget(IEnumerable<EpgProgram> items, DateTime focusTime)
    {
        return items.FirstOrDefault(p => p.Start <= focusTime && p.End > focusTime);
    }
}
