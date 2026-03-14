using System;
using System.Windows.Media;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;
using LibmpvIptvClient.Models;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public enum PlaybackMode
{
    Live = 0,
    Replay = 1,
    Timeshift = 2,
    RecordingPlayback = 3,
    LocalFile = 4,
    Stopped = 5
}

public enum PlaybackWindowForm
{
    Window = 0,
    Maximized = 1,
    Fullscreen = 2
}

public enum PlaybackTheme
{
    System = 0,
    Light = 1,
    Dark = 2
}

public sealed record PlaybackState(
    PlaybackMode Mode,
    bool IsPlaying,
    string CurrentChannelId,
    EpgProgram? CurrentProgram,
    DateTime? FocusTime,
    string SourceUrl,
    PlaybackWindowForm WindowForm,
    PlaybackTheme Theme)
{
    public static PlaybackState Default => new(
        PlaybackMode.Stopped,
        false,
        "",
        null,
        null,
        "",
        PlaybackWindowForm.Window,
        PlaybackTheme.System);
}

public abstract record PlaybackEvent;
public sealed record StartLivePlayback(string ChannelId, EpgProgram? Program, string SourceUrl) : PlaybackEvent;
public sealed record StartReplayPlayback(string ChannelId, EpgProgram? Program, string SourceUrl) : PlaybackEvent;
public sealed record StartTimeshiftPlayback(string ChannelId, DateTime CursorTime, EpgProgram? Program, string SourceUrl) : PlaybackEvent;
public sealed record StartRecordingPlayback(string SourceUrl) : PlaybackEvent;
public sealed record StartLocalFilePlayback(string SourceUrl) : PlaybackEvent;
public sealed record StopPlayback(bool IsAuto) : PlaybackEvent;
public sealed record WindowFormChanged(PlaybackWindowForm Form) : PlaybackEvent;
public sealed record ThemeChanged(PlaybackTheme Theme) : PlaybackEvent;

public static class PlaybackStateReducer
{
    public static PlaybackState Reduce(PlaybackState current, PlaybackEvent ev)
    {
        switch (ev)
        {
            case StartLivePlayback e:
                return current with
                {
                    Mode = PlaybackMode.Live,
                    IsPlaying = true,
                    CurrentChannelId = e.ChannelId ?? "",
                    CurrentProgram = e.Program,
                    FocusTime = DateTime.Now,
                    SourceUrl = e.SourceUrl ?? ""
                };
            case StartReplayPlayback e:
                return current with
                {
                    Mode = PlaybackMode.Replay,
                    IsPlaying = true,
                    CurrentChannelId = e.ChannelId ?? "",
                    CurrentProgram = e.Program,
                    FocusTime = e.Program != null ? e.Program.Start : current.FocusTime,
                    SourceUrl = e.SourceUrl ?? ""
                };
            case StartTimeshiftPlayback e:
                return current with
                {
                    Mode = PlaybackMode.Timeshift,
                    IsPlaying = true,
                    CurrentChannelId = e.ChannelId ?? "",
                    CurrentProgram = e.Program,
                    FocusTime = e.CursorTime,
                    SourceUrl = e.SourceUrl ?? ""
                };
            case StartRecordingPlayback e:
                return current with
                {
                    Mode = PlaybackMode.RecordingPlayback,
                    IsPlaying = true,
                    CurrentProgram = null,
                    FocusTime = null,
                    SourceUrl = e.SourceUrl ?? ""
                };
            case StartLocalFilePlayback e:
                return current with
                {
                    Mode = PlaybackMode.LocalFile,
                    IsPlaying = true,
                    CurrentProgram = null,
                    FocusTime = null,
                    SourceUrl = e.SourceUrl ?? ""
                };
            case StopPlayback:
                return current with
                {
                    Mode = PlaybackMode.Stopped,
                    IsPlaying = false,
                    CurrentProgram = null,
                    FocusTime = null,
                    SourceUrl = ""
                };
            case WindowFormChanged e:
                return current with { WindowForm = e.Form };
            case ThemeChanged e:
                return current with { Theme = e.Theme };
            default:
                return current;
        }
    }
}

public enum PlaybackStatusKind
{
    Live = 0,
    Playback = 1,
    Timeshift = 2,
    Record = 3
}

public enum PlaybackStatusBrushKind
{
    Live = 0,
    Replay = 1,
    Timeshift = 2,
    Orange = 3
}

public sealed record PlaybackStatusState(string Text, PlaybackStatusBrushKind BrushKind);

public sealed class MainWindowPlaybackStatusOverlaySyncActionsViewModel : ViewModelBase
{
    public PlaybackStatusKind ResolveKind(bool isRecordingPlaying, bool timeshiftActive, bool hasPlayingProgram)
    {
        if (isRecordingPlaying)
        {
            return PlaybackStatusKind.Record;
        }
        if (timeshiftActive)
        {
            return PlaybackStatusKind.Timeshift;
        }
        if (hasPlayingProgram)
        {
            return PlaybackStatusKind.Playback;
        }
        // Fix: Do NOT default to Live. Return Live only if actively playing live?
        // Actually, the user wants "Stop" to show NOTHING.
        // But "Live" status is usually implied when playing a channel without timeshift/replay.
        // The issue is: How do we know if we are "Stopped"?
        // This method only takes booleans about *special* modes.
        // It needs to know if we are playing at all!
        // But "Live" is the default fallback.
        // We should add an `isPlaying` parameter or handle `Live` logic outside.
        // However, `MainWindowChannelPlaybackActionsViewModel` calls this.
        // If we are stopped, we clear the status text explicitly in `ClearIndicatorsOnStop`.
        // BUT, if we start playing again, we need to set it back to "Live" (or whatever).
        // The user says: "After stop, play same/other channel -> indicator NOT appear".
        // This implies when we start playing, we are NOT setting the status text correctly?
        // Let's check `PlayChannel`.
        
        return PlaybackStatusKind.Live;
    }

    public string BuildText(PlaybackStatusKind kind)
    {
        return kind switch
        {
            PlaybackStatusKind.Timeshift => LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Timeshift", "时移"),
            PlaybackStatusKind.Playback => LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Playback", "回放"),
            PlaybackStatusKind.Record => LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Record", "录播"),
            _ => LibmpvIptvClient.Helpers.ResxLocalizer.Get("EPG_Status_Live", "直播")
        };
    }

    public PlaybackStatusBrushKind ResolveBrushKind(PlaybackStatusKind kind, bool preferOrangeForPlayback)
    {
        return kind switch
        {
            PlaybackStatusKind.Timeshift => PlaybackStatusBrushKind.Timeshift,
            PlaybackStatusKind.Playback => preferOrangeForPlayback ? PlaybackStatusBrushKind.Orange : PlaybackStatusBrushKind.Replay,
            PlaybackStatusKind.Record => PlaybackStatusBrushKind.Replay,
            _ => PlaybackStatusBrushKind.Live
        };
    }

    public PlaybackStatusState BuildState(
        bool isRecordingPlaying,
        bool timeshiftActive,
        bool hasPlayingProgram,
        bool preferOrangeForPlayback)
    {
        var kind = ResolveKind(isRecordingPlaying, timeshiftActive, hasPlayingProgram);
        var text = BuildText(kind);
        var brushKind = ResolveBrushKind(kind, preferOrangeForPlayback);
        return new PlaybackStatusState(text, brushKind);
    }

    public void Sync(
        PlaybackStatusState state,
        Func<PlaybackStatusBrushKind, System.Windows.Media.Brush> resolveBrush,
        Action<string, System.Windows.Media.Brush> applyTop,
        Action<string, System.Windows.Media.Brush> applyBottom,
        Action<string, System.Windows.Media.Brush> applyOverlay)
    {
        if (state == null || resolveBrush == null)
        {
            return;
        }

        System.Windows.Media.Brush brush;
        try
        {
            brush = resolveBrush(state.BrushKind);
        }
        catch
        {
            return;
        }

        try { applyTop?.Invoke(state.Text, brush); } catch { }
        try { applyBottom?.Invoke(state.Text, brush); } catch { }
        try { applyOverlay?.Invoke(state.Text, brush); } catch { }
    }
}
