using System;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record PlaybackPauseState(bool Paused);

public sealed class MainWindowPlaybackPauseOverlaySyncActionsViewModel : ViewModelBase
{
    public PlaybackPauseState BuildState(bool paused)
    {
        return new PlaybackPauseState(paused);
    }

    public void Sync(
        PlaybackPauseState state,
        Action<bool>? setMainPaused,
        Action<bool>? setOverlayPaused)
    {
        if (state == null)
        {
            return;
        }
        try { setMainPaused?.Invoke(state.Paused); } catch { }
        try { setOverlayPaused?.Invoke(state.Paused); } catch { }
    }
}
