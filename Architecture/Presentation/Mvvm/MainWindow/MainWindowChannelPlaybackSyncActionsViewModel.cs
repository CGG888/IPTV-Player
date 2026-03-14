using System;
using System.Collections.Generic;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;
using LibmpvIptvClient.Models;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record ChannelPlaybackSyncResult(EpgProgram? CurrentPlayingProgram, bool ShouldRefreshEpg);

public sealed class MainWindowChannelPlaybackSyncActionsViewModel : ViewModelBase
{
    public ChannelPlaybackSyncResult SyncChannelPlaybackAndEpg(
        IReadOnlyList<Channel> channels,
        Channel activeChannel,
        EpgProgram? currentPlayingProgram,
        IEnumerable<EpgProgram>? epgItems,
        Func<EpgProgram?, IEnumerable<EpgProgram>?, EpgSelectionSyncResult> clearEpg)
    {
        if (channels != null)
        {
            foreach (var c in channels) if (c != null) c.Playing = false;
        }
        if (activeChannel != null)
        {
            activeChannel.Playing = true;
        }
        if (clearEpg == null)
        {
            return new ChannelPlaybackSyncResult(currentPlayingProgram, false);
        }
        var result = clearEpg(currentPlayingProgram, epgItems);
        return new ChannelPlaybackSyncResult(result.CurrentPlayingProgram, result.ShouldRefresh);
    }

    public ChannelPlaybackSyncResult ClearLiveAndEpgIndicators(
        IReadOnlyList<Channel> channels,
        EpgProgram? currentPlayingProgram,
        IEnumerable<EpgProgram>? epgItems,
        Func<EpgProgram?, IEnumerable<EpgProgram>?, EpgSelectionSyncResult> clearEpg)
    {
        if (channels != null)
        {
            foreach (var c in channels) if (c != null) c.Playing = false;
        }
        if (clearEpg == null)
        {
            return new ChannelPlaybackSyncResult(currentPlayingProgram, false);
        }
        var result = clearEpg(currentPlayingProgram, epgItems);
        return new ChannelPlaybackSyncResult(result.CurrentPlayingProgram, result.ShouldRefresh);
    }
}
