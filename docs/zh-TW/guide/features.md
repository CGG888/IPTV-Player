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

## v1.1.2 之後新增與優化

- **EPG 狀態晶片**：節目單右側使用晶片式標識（與「提醒」同一外觀）
  - 直播=紅色填充、回放=綠色填充（自動適配深/淺色）
  - 點擊「直播」晶片=立即播放當前頻道直播
  - 當前播放項整行高亮；回放播放中左側另有綠色竪線提示（深色更明顯）
- **提醒與通知**
  - 提醒列表單實例管理；首次開啟跟隨播放器置中，使用者拖動後保留位置
  - 支援勾選框 + 全選/反選，刪除時優先刪除勾選項
  - 托盤／下拉／右鍵均可進入提醒列表，視窗層級在播放器之上（全螢幕／窗口一致）
  - 到點／成功通知不搶焦點：右下角（到點／成功）＋ 置中（列表）定位策略清晰
- **M3U 管理**
  - 新增「管理 M3U 清單」視窗（與提醒列表相同樣式），支援勾選批次刪除、單筆編輯
  - 托盤新增入口；下拉／右鍵與托盤入口行為一致；全螢幕可見且置於播放器上層
- **系統托盤**
  - 常駐圖示；雙擊恢復；選單包含 打開／提醒／管理 M3U／設定／退出
- **關閉確認**
  - 退出彈窗「×／ESC」僅關閉彈窗；「是」=退出，「否」=最小化到托盤（全螢幕先退出全螢幕）
  - 文案三段式國際化（已同步中／英／繁／俄）
- **深淺色適配**
  - 提醒列表、M3U 管理、提醒對話框、退出對話框在視窗建立後即時套用深／淺色標題列
