using System.Collections.Generic;
using LibmpvIptvClient.Architecture.Application.Player;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record PlaybackSpeedOption(double Speed, string Label);

public sealed class MainWindowPlaybackActionsViewModel : ViewModelBase
{
    private static readonly double[] SpeedValues = { 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 3.0, 5.0 };

    public IReadOnlyList<PlaybackSpeedOption> GetSpeedOptions()
    {
        var options = new List<PlaybackSpeedOption>();
        foreach (var sp in SpeedValues)
        {
            options.Add(new PlaybackSpeedOption(sp, $"{sp:0.##}x"));
        }
        return options;
    }

    public bool TryTogglePlayPause(IPlayerEngine? engine, bool currentPaused, out bool nextPaused)
    {
        nextPaused = currentPaused;
        if (engine == null) return false;
        nextPaused = !currentPaused;
        engine.Pause(nextPaused);
        return true;
    }

    public bool TryStop(IPlayerEngine? engine)
    {
        if (engine == null) return false;
        engine.Stop();
        return true;
    }

    public bool TrySeekRelative(IPlayerEngine? engine, int seconds)
    {
        if (engine == null) return false;
        engine.SeekRelative(seconds);
        return true;
    }

    public bool TrySetSpeed(IPlayerEngine? engine, double speed)
    {
        if (engine == null) return false;
        engine.SetSpeed(speed);
        return true;
    }

    public bool TryToggleMute(IPlayerEngine? engine, bool currentMuted, out bool nextMuted)
    {
        nextMuted = currentMuted;
        if (engine == null) return false;
        nextMuted = !currentMuted;
        engine.SetMute(nextMuted);
        return true;
    }
}
