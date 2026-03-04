# 功能特性

## 核心播放功能
- **播放控制**: 播放/暂停、停止、快进/快退、拖动进度。
- **音量调节**: 滑块调节，支持静音（0-100）。
- **状态标识**: 全屏条显示直播/回放/时移状态。

## IPTV 专用功能

### M3U 解析
支持本地/远程 M3U 列表，兼容 UTF-8/GB18030 编码。支持 `#EXTINF` 扩展属性。

### EPG 节目单
支持 XMLTV (gz) 格式，按日切换，自动匹配 `tvg-id`。
[查看使用与配置说明](./epg)

### 回放 (Catchup)
基于模板自动生成回放 URL（如 `{utc:yyyyMMddHHmmss}`）。
[查看使用与自定义说明](./catchup-timeshift)

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/catchup.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### 时移 (Time-Shift)
拖动进度条回看直播历史（依赖 `catchup-source`）。
[查看使用与自定义说明](./catchup-timeshift)

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/timeshift.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### 频道管理
分组、搜索、收藏（运行时有效）。

### 直播优化 (FCC)
FCC 快速切台与 UDP 组播优化。

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/fast-zapping.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

## 全屏与悬浮层
- **双击全屏**: 在视频区域双击切换全屏。
- **悬浮控制条**: 全屏下鼠标移动到底部自动显示，支持播放控制与 EPG 切换。
- **侧边抽屉**: 全屏下鼠标贴边显示频道列表（右侧）或 EPG（左侧）。
