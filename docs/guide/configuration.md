# Configuration

## `user_settings.json`

Located in the application run directory, stores user preferences.

```json
{
  "Hwdec": true,              // Hardware decoding
  "SourceTimeoutSec": 3,      // Source switch timeout (seconds)
  "TimeshiftHours": 2,        // Timeshift duration (hours)
  "Language": "en-US",        // Interface language
  "ThemeMode": "Dark"         // Theme mode (Dark/Light/System)
}
```

## Parameters

- **Hwdec**: Enables hardware acceleration (d3d11va by default).
- **SourceTimeoutSec**: Time to wait before switching sources if one fails.
- **TimeshiftHours**: Maximum duration to look back in the live stream.
- **Language**: UI language code (e.g., `en-US`, `zh-CN`).
- **ThemeMode**: Application theme preference.
