# Configuration

## `user_settings.json`

Located in the application run directory, stores user preferences.

```json
{
  "Hwdec": true,              // Hardware decoding
  "FccPrefetchCount": 2,      // FCC prefetch count
  "EnableUdpOptimization": false,
  "SourceTimeoutSec": 3,      // Source switch timeout (seconds)
  "TimeshiftHours": 2,        // Timeshift duration (hours)
  "RecordingLocalDir": "recordings/{channel}",
  "Recording": {
    "Enabled": true,
    "SaveMode": "local_then_upload",
    "UploadMaxConcurrency": 1
  },
  "ScheduledReminders": [],
  "Language": "en-US",        // Interface language
  "ThemeMode": "System",      // Theme mode (Dark/Light/System)
  "ConfirmOnClose": true
}
```

## Parameters

- **Hwdec**: Enables hardware acceleration (d3d11va by default).
- **FccPrefetchCount**: FCC prefetch parallelism balancing zap speed and resource usage.
- **EnableUdpOptimization**: UDP multicast optimization switch.
- **SourceTimeoutSec**: Time to wait before switching sources if one fails.
- **TimeshiftHours**: Maximum duration to look back in the live stream.
- **RecordingLocalDir**: Recording directory template (supports `{channel}` token).
- **Recording.SaveMode**: Recording persistence policy (local/upload priority).
- **ScheduledReminders**: Persisted schedule entries, including remind-only and auto-play actions.
- **Language**: UI language code (e.g., `en-US`, `zh-CN`).
- **ThemeMode**: Application theme preference.
- **ConfirmOnClose**: Whether to show close confirmation (supports minimize-to-tray flow).

## Scheduling and Recording Notes

- Keep `ConfirmOnClose=true` for scheduling-heavy workflows to avoid missing due reminders.
- Tune `Recording.UploadMaxConcurrency` based on available network bandwidth.
- When using remote storage, configure WebDAV in the recording settings section.
