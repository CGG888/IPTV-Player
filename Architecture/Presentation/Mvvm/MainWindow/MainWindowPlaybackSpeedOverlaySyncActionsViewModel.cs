using System;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record PlaybackSpeedState(double Speed, bool Enabled, string Label);

public sealed class MainWindowPlaybackSpeedOverlaySyncActionsViewModel : ViewModelBase
{
    public bool ResolveEnabled(bool timeshiftActive, bool hasPlayingProgram, bool isRecordingPlaying)
    {
        return timeshiftActive || hasPlayingProgram || isRecordingPlaying;
    }

    public PlaybackSpeedState BuildState(double speed, bool enabled)
    {
        if (!enabled)
        {
            return new PlaybackSpeedState(1.0, false, "1.0x");
        }

        var safe = speed;
        if (double.IsNaN(safe) || double.IsInfinity(safe) || safe <= 0)
        {
            safe = 1.0;
        }
        var label = $"{safe:0.##}x";
        return new PlaybackSpeedState(safe, true, label);
    }

    public void Sync(
        PlaybackSpeedState state,
        Action<double>? setOverlaySpeed,
        Action<bool>? setOverlayEnabled,
        Action<bool>? setButtonEnabled,
        Action<string>? setLabel,
        Action<double>? setMpvSpeed)
    {
        if (state == null)
        {
            return;
        }

        try { setOverlaySpeed?.Invoke(state.Speed); } catch { }
        try { setOverlayEnabled?.Invoke(state.Enabled); } catch { }
        try { setButtonEnabled?.Invoke(state.Enabled); } catch { }
        try { setLabel?.Invoke(state.Label); } catch { }
        try { setMpvSpeed?.Invoke(state.Speed); } catch { }
    }
}
