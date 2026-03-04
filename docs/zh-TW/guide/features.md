# 功能特性

## 核心播放功能
- **播放控制**: 播放/暫停、停止、快進/快退、拖動進度。
- **音量調節**: 滑塊調節，支援靜音（0-100）。
- **狀態標識**: 全螢幕條顯示直播/回放/時移狀態。

## IPTV 專用功能

### M3U 解析
支援本地/遠端 M3U 清單，相容 UTF-8/GB18030 編碼。支援 `#EXTINF` 擴展屬性。

### EPG 節目單
支援 XMLTV (gz) 格式，按日切換，自動匹配 `tvg-id`。
[查看使用與設定說明](./epg)

### 回放 (Catchup)
基於模板自動生成回放 URL（如 `{utc:yyyyMMddHHmmss}`）。
[查看使用與自訂說明](./catchup-timeshift)

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/catchup.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### 時移 (Time-Shift)
拖動進度條回看直播歷史（依賴 `catchup-source`）。
[查看使用與自訂說明](./catchup-timeshift)

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/timeshift.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### 頻道管理
分組、搜尋、收藏（執行時有效）。

### 直播最佳化 (FCC)
FCC 快速切台與 UDP 組播最佳化。

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/fast-zapping.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

## 全螢幕與懸浮層
- **雙擊全螢幕**: 在影片區域雙擊切換全螢幕。
- **懸浮控制條**: 全螢幕下滑鼠移動到底部自動顯示，支援播放控制與 EPG 切換。
- **側邊抽屜**: 全螢幕下滑鼠貼邊顯示頻道列表（右側）或 EPG（左側）。
