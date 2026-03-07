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

## v1.1.2 之后新增与优化

- **EPG 状态芯片**：节目单右侧使用芯片式标识（UI 与“预约”统一）
  - 直播=红色填充、回放=绿色填充（自动适配深/浅色）
  - 点击“直播”芯片=立即播放当前频道直播
  - 当前播放项整行高亮；回放播放中左侧额外绿色竖线提示（深色更明显）
- **预约与通知**
  - 预约通知列表单实例管理，首次打开跟随播放器居中，用户拖动后不再回弹
  - 支持复选框 + 全选/反选，删除时优先删除勾选项
  - 托盘右键/下拉/右键均可进入预约列表，窗口层级在播放器之上（全屏/窗口一致）
  - 到点/成功通知不抢焦点：右下角（到点/成功）+ 居中（列表）定位策略明确
- **M3U 管理**
  - 新增“管理 M3U 列表”窗口（与预约列表相同样式），支持复选框批量删除、单条编辑
  - 托盘新增入口；下拉/右键与托盘入口行为一致；全屏可见且置于播放器上层
- **系统托盘**
  - 常驻图标；双击恢复；菜单包含 打开/预约/管理 M3U/设置/退出
- **关闭确认**
  - 退出弹窗“×/ESC”仅关闭弹窗；“是”=退出，“否”=最小化到托盘（全屏先退出全屏）
  - 文案三段式国际化（已同步中/英/繁/俄）
- **深浅色适配**
  - 预约列表、M3U 管理、预约提醒、退出对话框等在窗口创建后即时套用深/浅色标题栏
