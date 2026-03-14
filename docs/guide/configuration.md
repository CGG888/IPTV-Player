# 配置说明

## `user_settings.json`

位于程序运行目录，存储用户偏好设置。

```json
{
  "Hwdec": true,              // 硬件解码开关
  "FccPrefetchCount": 2,      // FCC 预取数量
  "EnableUdpOptimization": false,
  "SourceTimeoutSec": 3,      // 换源超时时间（秒）
  "TimeshiftHours": 2,        // 时移回看时长（小时）
  "RecordingLocalDir": "recordings/{channel}",
  "Recording": {
    "Enabled": true,
    "SaveMode": "local_then_upload",
    "UploadMaxConcurrency": 1
  },
  "ScheduledReminders": [],
  "Language": "zh-CN",        // 界面语言
  "ThemeMode": "System",      // 主题模式 (Dark/Light/System)
  "ConfirmOnClose": true
}
```

## 参数说明

- **Hwdec**: 开启硬件加速（默认使用 `d3d11va`）。
- **FccPrefetchCount**: FCC 预取并行数量，影响切台速度与资源占用平衡。
- **EnableUdpOptimization**: UDP 组播优化开关。
- **SourceTimeoutSec**: 换源超时时间（秒），若源无效则尝试下一个。
- **TimeshiftHours**: 时移回看的最大时长（小时）。
- **RecordingLocalDir**: 录播默认目录模板（支持 `{channel}`）。
- **Recording.SaveMode**: 录制保存模式（本地/上传优先策略）。
- **ScheduledReminders**: 预约列表持久化数据，含“仅提醒/自动播放”策略。
- **Language**: 界面语言代码（如 `zh-CN`, `en-US`）。
- **ThemeMode**: 应用程序主题模式（深色/浅色/跟随系统）。
- **ConfirmOnClose**: 关闭窗口时是否弹出确认框（可最小化到托盘）。

## 预约与录播建议

- 预约播放场景建议保持 `ConfirmOnClose=true`，避免误关导致错过提醒。
- 录播上传场景建议按网络能力调整 `Recording.UploadMaxConcurrency`。
- 使用远端存储时建议同时配置 WebDAV（见设置页“录播”分组）。
