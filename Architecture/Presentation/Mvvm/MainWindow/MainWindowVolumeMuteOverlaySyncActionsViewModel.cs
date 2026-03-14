using System;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record VolumeMuteState(double Volume, bool Muted);

public sealed class MainWindowVolumeMuteOverlaySyncActionsViewModel : ViewModelBase
{
    public double ClampVolume(double volume)
    {
        if (double.IsNaN(volume) || double.IsInfinity(volume))
        {
            return 0;
        }
        return Math.Max(0, Math.Min(100, volume));
    }

    public VolumeMuteState BuildState(double volume, bool muted)
    {
        return new VolumeMuteState(ClampVolume(volume), muted);
    }

    public void Sync(
        VolumeMuteState state,
        Action<double>? setMpvVolume,
        Action<bool>? setMpvMuted,
        Action<double>? setMainVolume,
        Action<bool>? setMainMuted,
        Action<double>? setOverlayVolume,
        Action<bool>? setOverlayMuted)
    {
        if (state == null)
        {
            return;
        }

        try { setMpvVolume?.Invoke(state.Volume); } catch { }
        try { setMpvMuted?.Invoke(state.Muted); } catch { }
        try { setMainVolume?.Invoke(state.Volume); } catch { }
        try { setMainMuted?.Invoke(state.Muted); } catch { }
        try { setOverlayVolume?.Invoke(state.Volume); } catch { }
        try { setOverlayMuted?.Invoke(state.Muted); } catch { }
    }
}
