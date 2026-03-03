# 配置说明

## `user_settings.json`

位于程序运行目录，存储用户偏好设置。

```json
{
  "Hwdec": true,              // 硬件解码开关
  "SourceTimeoutSec": 3,      // 换源超时时间（秒）
  "TimeshiftHours": 2,        // 时移回看时长（小时）
  "Language": "zh-CN",        // 界面语言
  "ThemeMode": "Dark"         // 主题模式 (Dark/Light/System)
}
```

## 参数说明

- **Hwdec**: 开启硬件加速（默认使用 `d3d11va`）。
- **SourceTimeoutSec**: 换源超时时间（秒），若源无效则尝试下一个。
- **TimeshiftHours**: 时移回看的最大时长（小时）。
- **Language**: 界面语言代码（如 `zh-CN`, `en-US`）。
- **ThemeMode**: 应用程序主题模式（深色/浅色/跟随系统）。
