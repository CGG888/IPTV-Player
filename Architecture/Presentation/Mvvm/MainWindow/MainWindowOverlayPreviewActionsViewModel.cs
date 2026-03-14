using System;
using LibmpvIptvClient.Architecture.Presentation.Mvvm;
using LibmpvIptvClient.Models;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.MainWindow;

public sealed record OverlayPreviewResult(string Title, string Message);

public sealed class MainWindowOverlayPreviewActionsViewModel : ViewModelBase
{
    public OverlayPreviewResult? BuildPreviewResult(
        Channel? channel,
        bool timeshiftActive,
        DateTime timeshiftMin,
        DateTime timeshiftMax,
        double timeshiftCursorSec,
        EpgProgram? currentPlayingProgram)
    {
        if (channel == null)
        {
            return null;
        }

        DateTime s = DateTime.Now.AddHours(-3);
        DateTime e2 = s.AddHours(1);
        if (timeshiftActive)
        {
            var total = Math.Max(1, (timeshiftMax - timeshiftMin).TotalSeconds);
            var cursor = Math.Max(0, Math.Min(total, timeshiftCursorSec));
            s = timeshiftMin.AddSeconds(cursor);
            e2 = s.AddMinutes(10);
        }
        else if (currentPlayingProgram != null)
        {
            s = currentPlayingProgram.Start;
            e2 = currentPlayingProgram.End;
        }

        var baseUrl = channel.CatchupSource;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            if (AppSettings.Current.Replay.Enabled && !string.IsNullOrEmpty(AppSettings.Current.Replay.UrlFormat))
            {
                var fmt = AppSettings.Current.Replay.UrlFormat;
                if (!string.IsNullOrEmpty(fmt) && (fmt.StartsWith("?") || fmt.StartsWith("&")))
                {
                    var live = (channel.Tag is Source s1 && !string.IsNullOrEmpty(s1.Url)) ? s1.Url
                               : (channel.Sources != null && channel.Sources.Count > 0 ? channel.Sources[0].Url : "");
                    if (!string.IsNullOrEmpty(live))
                    {
                        var sep = live.Contains("?") ? "&" : "?";
                        baseUrl = live + sep + fmt.TrimStart('?', '&');
                    }
                }
                else
                {
                    baseUrl = fmt.Replace("{id}", channel.Id ?? channel.Name);
                }
            }
        }

        baseUrl = baseUrl ?? "";
        var preview = LibmpvIptvClient.Services.UrlTimeRewriter.RewriteIfEnabled(AppSettings.Current, baseUrl, s, e2, timeshiftActive);
        var title = LibmpvIptvClient.Helpers.ResxLocalizer.Get("Overlay_Preview_Title", "预览拼接");
        return new OverlayPreviewResult(title, preview);
    }
}
