# 配置說明

## `user_settings.json`

位於程式執行目錄，存儲使用者偏好設定。

```json
{
  "Hwdec": true,              // 硬體解碼開關
  "SourceTimeoutSec": 3,      // 換源超時時間（秒）
  "TimeshiftHours": 2,        // 時移回看時長（小時）
  "Language": "zh-CN",        // 介面語言
  "ThemeMode": "Dark"         // 主題模式 (Dark/Light/System)
}
```

## 參數說明

- **Hwdec**: 開啟硬體加速（預設使用 `d3d11va`）。
- **SourceTimeoutSec**: 換源超時時間（秒），若來源無效則嘗試下一個。
- **TimeshiftHours**: 時移回看的最大時長（小時）。
- **Language**: 介面語言代碼（如 `zh-CN`, `en-US`）。
- **ThemeMode**: 應用程式主題模式（深色/淺色/跟隨系統）。
