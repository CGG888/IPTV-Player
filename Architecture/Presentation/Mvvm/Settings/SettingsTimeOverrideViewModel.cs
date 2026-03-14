using LibmpvIptvClient.Services;
using System;

namespace LibmpvIptvClient.Architecture.Presentation.Mvvm.Settings;

public sealed record SettingsTimeOverrideFormState(
    bool Enabled,
    int ModeIndex,
    int LayoutIndex,
    int EncodingIndex,
    string StartKey,
    string EndKey,
    string DurationKey,
    string PlayseekKey,
    bool UrlEncode);

public sealed record TimeOverrideVisibilityState(
    bool ShowStart,
    bool ShowEnd,
    bool ShowDuration,
    bool ShowPlayseek);

public sealed class SettingsTimeOverrideViewModel : ViewModelBase
{
    private const string MODE_APPEND = "append";
    private const string MODE_REPLACE = "replace_all";

    private const string LAYOUT_START_END = "start_end";
    private const string LAYOUT_PLAYSEEK = "playseek";
    private const string LAYOUT_START_DURATION = "start_duration";

    private const string ENC_LOCAL = "local";
    private const string ENC_UTC = "utc";
    private const string ENC_UNIX = "unix";
    private const string ENC_UNIX_MS = "unix_ms";

    public SettingsTimeOverrideFormState BuildFormState(TimeOverrideConfig? config)
    {
        if (config is null)
        {
            return new SettingsTimeOverrideFormState(false, 0, 0, 0, "start", "end", "duration", "playseek", false);
        }

        var modeIdx = string.Equals(config.Mode, MODE_REPLACE, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        
        var layoutIdx = 0;
        if (string.Equals(config.Layout, LAYOUT_PLAYSEEK, StringComparison.OrdinalIgnoreCase)) layoutIdx = 1;
        else if (string.Equals(config.Layout, LAYOUT_START_DURATION, StringComparison.OrdinalIgnoreCase)) layoutIdx = 2;

        var encIdx = 0;
        if (string.Equals(config.Encoding, ENC_UTC, StringComparison.OrdinalIgnoreCase)) encIdx = 1;
        else if (string.Equals(config.Encoding, ENC_UNIX, StringComparison.OrdinalIgnoreCase)) encIdx = 2;
        else if (string.Equals(config.Encoding, ENC_UNIX_MS, StringComparison.OrdinalIgnoreCase)) encIdx = 3;

        return new SettingsTimeOverrideFormState(
            config.Enabled,
            modeIdx,
            layoutIdx,
            encIdx,
            config.StartKey ?? "start",
            config.EndKey ?? "end",
            config.DurationKey ?? "duration",
            config.PlayseekKey ?? "playseek",
            config.UrlEncode
        );
    }

    public TimeOverrideConfig BuildSaveConfig(SettingsTimeOverrideFormState form)
    {
        return new TimeOverrideConfig
        {
            Enabled = form.Enabled,
            Mode = form.ModeIndex == 1 ? MODE_REPLACE : MODE_APPEND,
            Layout = form.LayoutIndex switch
            {
                1 => LAYOUT_PLAYSEEK,
                2 => LAYOUT_START_DURATION,
                _ => LAYOUT_START_END
            },
            Encoding = form.EncodingIndex switch
            {
                1 => ENC_UTC,
                2 => ENC_UNIX,
                3 => ENC_UNIX_MS,
                _ => ENC_LOCAL
            },
            StartKey = (form.StartKey ?? "start").Trim(),
            EndKey = (form.EndKey ?? "end").Trim(),
            DurationKey = (form.DurationKey ?? "duration").Trim(),
            PlayseekKey = (form.PlayseekKey ?? "playseek").Trim(),
            UrlEncode = form.UrlEncode
        };
    }

    public TimeOverrideVisibilityState GetVisibilityState(int layoutIndex)
    {
        var layout = layoutIndex switch
        {
            1 => LAYOUT_PLAYSEEK,
            2 => LAYOUT_START_DURATION,
            _ => LAYOUT_START_END
        };

        return new TimeOverrideVisibilityState(
            ShowStart: layout == LAYOUT_START_END || layout == LAYOUT_START_DURATION,
            ShowEnd: layout == LAYOUT_START_END,
            ShowDuration: layout == LAYOUT_START_DURATION,
            ShowPlayseek: layout == LAYOUT_PLAYSEEK
        );
    }

    public string GeneratePreviewResult(string baseUrl, SettingsTimeOverrideFormState form, bool isTimeshift)
    {
        var config = BuildSaveConfig(form);
        var settings = new PlaybackSettings { TimeOverride = config };
        var now = DateTime.Now;
        var start = now.AddHours(-3);
        var end = start.AddHours(1);
        
        return UrlTimeRewriter.RewriteIfEnabled(settings, baseUrl, start, end, isTimeshift);
    }

    public string GenerateTemplatePreview(string baseUrl, SettingsTimeOverrideFormState form)
    {
        var config = BuildSaveConfig(form);
        
        // Choose placeholder tokens by encoding
        string pStart, pEnd, pDur = "${duration}";
        if (string.Equals(config.Encoding, ENC_UTC, StringComparison.OrdinalIgnoreCase))
        {
            pStart = "${(b)yyyyMMddHHmmss|UTC}";
            pEnd = "${(e)yyyyMMddHHmmss|UTC}";
        }
        else if (string.Equals(config.Encoding, ENC_UNIX, StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(config.Encoding, ENC_UNIX_MS, StringComparison.OrdinalIgnoreCase))
        {
            pStart = "${timestamp}";
            pEnd = "${end_timestamp}";
        }
        else
        {
            pStart = "${(b)yyyyMMddHHmmss}";
            pEnd = "${(e)yyyyMMddHHmmss}";
        }

        string query;
        if (string.Equals(config.Layout, LAYOUT_PLAYSEEK, StringComparison.OrdinalIgnoreCase))
        {
            query = $"{config.PlayseekKey}={pStart}-{pEnd}";
        }
        else if (string.Equals(config.Layout, LAYOUT_START_DURATION, StringComparison.OrdinalIgnoreCase))
        {
            query = $"{config.StartKey}={pStart}&{config.DurationKey}={pDur}";
        }
        else
        {
            query = $"{config.StartKey}={pStart}&{config.EndKey}={pEnd}";
        }

        if (string.IsNullOrWhiteSpace(baseUrl)) return query;
        var sep = baseUrl.Contains("?") ? "&" : "?";
        return baseUrl + sep + query;
    }

    public bool NeedsConfirmationForModeChange(int newIndex)
    {
        return newIndex == 1; // 1 is replace_all
    }
}
