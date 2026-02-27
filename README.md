# IPTV Player（Windows / WPF）

**IPTV Player** 是一款专为 Windows 平台打造的高性能、现代化的 IPTV 播放器。

它基于强大的 **libmpv** 播放内核构建，结合 **WPF** 的现代化界面设计，为您带来流畅、稳定的直播观看体验。不仅支持 M3U 播放列表、EPG 电子节目单、回看等核心功能，还针对 IPTV 场景进行了深度优化（如 FCC 快速切台、UDP 组播优化），是您在 PC 上观看电视直播的理想选择。

---

## 目录

- [项目概览](#项目概览)
- [功能清单](#功能清单)
  - [核心播放功能](#核心播放功能)
  - [IPTV 专用功能](#iptv-专用功能)
  - [全屏与悬浮层](#全屏与悬浮层)
  - [调试与日志](#调试与日志)
- [界面与交互](#界面与交互)
  - [主界面区域](#主界面区域)
  - [菜单与操作入口](#菜单与操作入口)
  - [快捷键](#快捷键)
- [配置文件与参数说明](#配置文件与参数说明)
  - [`user_settings.json`](#user_settingsjson)
  - [`appsettings.json`](#appsettingsjson)
- [libmpv 引擎说明](#libmpv-引擎说明)
  - [硬解支持](#硬解支持)
  - [音视频探测与 IPTV “无声”问题](#音视频探测与-iptv-无声问题)
  - [字幕与多音轨/字幕轨](#字幕与多音轨字幕轨)
- [依赖环境](#依赖环境)
- [编译与运行](#编译与运行)
- [二次开发指南](#二次开发指南)
  - [关键类与职责](#关键类与职责)
  - [常见扩展点](#常见扩展点)
- [示例截图（占位符）](#示例截图占位符)

---

## 项目概览

**IPTV Player** 以 `libmpv-2.dll` 为播放内核，通过 `WindowsFormsHost` 承载视频窗口句柄，提供 IPTV 频道列表、分组、EPG、回放、源切换等功能。

- 播放器入口窗口：[MainWindow.xaml](./MainWindow.xaml)
- libmpv 绑定封装：[MpvPlayer.cs](./MpvPlayer.cs)
- M3U 解析：[Services/M3UParser.cs](./Services/M3UParser.cs)
- EPG 解析与缓存：[Services/EpgService.cs](./Services/EpgService.cs)

---

## 功能清单

### 核心播放功能

- 播放 / 暂停：底部按钮或悬浮控制条（全屏）触发  
  - 暂停调用 `mpv` 属性 `pause`（[MainWindow.xaml.cs](./MainWindow.xaml.cs)）
- 停止：停止播放并显示占位页（避免 WindowsFormsHost 空域遮挡）  
- 快退 / 快进：默认 `-10s / +10s`（[MainWindow.xaml.cs](./MainWindow.xaml.cs)）
- 进度条拖动：支持拖动 Seek（主窗口进度条与全屏悬浮条均支持）
- 音量调节：滑块调节音量；支持静音开关（全屏悬浮条）
- 播放状态标签：在全屏悬浮控制条中显示 **直播/回放** 标识  
  - 直播：白色“直播”
  - 回放：橙色“回放”
  - 实现窗口：[OverlayControls.xaml](./OverlayControls.xaml)、更新入口：[MainWindow.xaml.cs](./MainWindow.xaml.cs)

### IPTV 专用功能

- M3U 列表解析（本地文件 / HTTP(S) URL）
  - 支持 `#EXTM3U` 头部属性读取（如 `x-tvg-url`/`url-tvg`）
  - 支持 `#EXTINF` 属性：`tvg-id`、`tvg-logo`、`group-title`、`catchup`、`catchup-source` 等
  - URL 文本兼容：UTF-8 与 GB18030 回退
  - 远程 M3U 支持 GZIP（`.gz` 或响应体为 gzip）自动解压
  - 实现见：[Services/M3UParser.cs](./Services/M3UParser.cs)
- 频道列表与分组
  - 频道抽屉（右侧）支持“所有频道 / 分组 / 收藏”页签（[MainWindow.xaml](./MainWindow.xaml)）
  - 搜索与清空按钮（右侧抽屉顶部）
- 频道切换
  - 双击频道项播放
  - “源切换”按钮显示同名频道的多个源（单播/组播、UHD、fps 标签）（[MainWindow.xaml.cs](./MainWindow.xaml.cs)）
- 自动换源（超时保护）
  - 当设定时间内未能开始播放（基于 `time-pos` 判断），自动切换到下一个源
  - 参数：`SourceTimeoutSec`（[MainWindow.xaml.cs](./MainWindow.xaml.cs)）
- EPG 显示（XMLTV）
  - 支持 XML 或 GZIP(XML) 输入
  - 支持按日期切换（前一天/今天/后一天）
  - 显示状态：直播 / 回放 / 预约（[Models/EpgProgram.cs](./Models/EpgProgram.cs)）
  - 无 EPG 数据时：自动生成占位节目单（“精彩节目”，按小时分段）
  - 解析与缓存实现：[Services/EpgService.cs](./Services/EpgService.cs)
- 回放（Catchup）
  - 仅当频道存在 `catchup-source` 时可用
  - 在 EPG 列表中点击“回放”状态的节目，将基于模板生成回放 URL 并播放
  - 支持的时间占位符（示例）：
    - `{utc:yyyyMMddHHmmss}`、`{utcend:yyyyMMddHHmmss}`
    - `${(b)yyyyMMdd|UTC}`、`${(b)HHmmss|UTC}`、`${(e)yyyyMMdd|UTC}`、`${(e)HHmmss|UTC}`
    - `{start}`、`{end}`（本地时间 `yyyyMMddHHmmss`）
  - 相关实现：[MainWindow.xaml.cs](./MainWindow.xaml.cs)
- 收藏
  - 频道项“★/☆”按钮收藏/取消收藏
  - “收藏”页签展示收藏列表（去重保持顺序）
  - 说明：当前收藏为运行时状态，未做跨启动持久化（[MainWindow.xaml.cs](./MainWindow.xaml.cs)）
- 播放记录/历史
  - 说明：当前版本未实现播放历史记录功能（后续可扩展）

### 全屏与悬浮层

- 双击视频区域进入/退出全屏
- 全屏退出：按 `Esc`（[FullscreenWindow.xaml](./FullscreenWindow.xaml)）
- 全屏悬浮控制条（底部）：
  - 鼠标移动显示，停留后自动隐藏
  - 覆盖在视频之上（独立透明窗口）
  - 实现：[OverlayControls.xaml](./OverlayControls.xaml)
- 全屏顶部悬浮条（窗口按钮 + 菜单）：
  - 置顶、最小化、退出全屏、关闭等（[TopOverlay.xaml](./TopOverlay.xaml)）
  - 标题菜单包含：打开文件、添加 M3U、设置、FCC/UDP 切换、已保存源列表（右键可编辑源）
- 全屏 EPG / 频道抽屉悬浮窗（左右侧）
  - 鼠标贴边显示，离开区域自动隐藏
  - 处理焦点切换导致的 Z-order 问题：全屏模式下对窗口层级进行保护，避免被其他应用覆盖（[MainWindow.xaml.cs](./MainWindow.xaml.cs)）

### 调试与日志

- “调试日志”窗口：实时显示日志、支持清空  
  - 实现：[DebugWindow.xaml](./DebugWindow.xaml)、[DebugWindow.xaml.cs](./DebugWindow.xaml.cs)
- 入口：主窗口菜单/设置窗口按钮

---

## 界面与交互

### 主界面区域

- 顶部标题栏：自定义按钮（置顶/最小化/最大化/全屏/关闭）与下拉菜单（打开文件/添加 M3U/设置/FCC/UDP/源列表）
- 左侧：EPG 面板（可开关）
- 中央：视频渲染区域（WindowsFormsHost + Panel）
- 右侧：频道抽屉（可折叠）
- 底部：控制栏（播放/停止/快退快进/音量/源切换/画面比例/EPG/抽屉）

### 菜单与操作入口

- 主窗口标题按钮下拉菜单（[MainWindow.xaml.cs](./MainWindow.xaml.cs)）
  - 打开文件（本地 M3U/M3U8）
  - 添加 M3U 地址（保存到 `user_settings.json`）
  - 设置（播放设置）
  - FCC（快速切台）开关
  - UDP（组播优化）开关
  - 已保存源列表（点击加载）
- 全屏顶部悬浮条菜单（[TopOverlay.xaml.cs](./TopOverlay.xaml.cs)）
  - 功能同上，另外支持：对已保存源“右键编辑”

### 快捷键

- 全屏退出：`Esc`（[FullscreenWindow.xaml.cs](./FullscreenWindow.xaml.cs)）
- 说明：当前主窗口未实现全局快捷键（如空格暂停、方向键快进/快退等），可在后续通过 `PreviewKeyDown` 或 WPF `InputBinding` 扩展。

---

## 配置文件与参数说明

### `user_settings.json`

位置：程序运行目录（`AppDomain.CurrentDomain.BaseDirectory`）  
读写逻辑：[PlaybackSettings.cs](./PlaybackSettings.cs)

字段说明：

- `Hwdec`：是否启用硬解（默认 `true`，使用 `d3d11va`）
- `CacheSecs`：缓存秒数（对 TS/HLS 等起作用）
- `DemuxerMaxBytesMiB`：demuxer 最大缓冲大小（MiB）
- `DemuxerMaxBackBytesMiB`：demuxer 回看缓冲大小（MiB）
- `FccPrefetchCount`：快速切台预取数量（0=关闭，默认 2）
- `EnableUdpOptimization`：组播优化开关（默认 false）
- `SourceTimeoutSec`：换源超时秒数（默认 3）
- `CustomEpgUrl`：自定义 EPG 地址（XMLTV）
- `SavedSources`：保存的 M3U 源列表
  - `Name`：显示名称
  - `Url`：M3U 地址
  - `IsSelected`：是否选中（用于 UI 标记）

示例：

```json
{
  "Hwdec": true,
  "CacheSecs": 1.0,
  "DemuxerMaxBytesMiB": 16,
  "DemuxerMaxBackBytesMiB": 4,
  "FccPrefetchCount": 2,
  "EnableUdpOptimization": false,
  "SourceTimeoutSec": 3,
  "CustomEpgUrl": "http://example.com/epg.xml.gz",
  "SavedSources": [
    { "Name": "本地测试", "Url": "C:\\\\iptv\\\\live.m3u", "IsSelected": true },
    { "Name": "远程源", "Url": "https://example.com/live.m3u", "IsSelected": false }
  ]
}
```

### `appsettings.json`

文件存在于项目中并会复制到输出目录（[LibmpvIptvClient.csproj](./LibmpvIptvClient.csproj)），内容包含 IPTV Checker 后端、FCC 参数等默认值示例：

- `backend.iptvChecker.*`
- `fcc.*`

说明：当前 WPF 应用未读取该文件（没有 `ConfigurationBuilder` 相关逻辑）。如需启用后端加载（`IptvCheckerClient`），需要在启动阶段将 `baseUrl/token/endpoints` 注入到 `IptvCheckerClient` 构造参数中。

---

## libmpv 引擎说明

`libmpv-2.dll` 通过 P/Invoke 调用，核心封装见：[MpvPlayer.cs](./MpvPlayer.cs)

### 获取与部署 libmpv-2.dll

- 本仓库不包含 `libmpv-2.dll`，并已在 [.gitignore](./.gitignore) 中忽略。原因：文件体积较大（>100MB，超过 GitHub 单文件限制）且属运行时依赖，建议在发布阶段引入。
- 获取途径：请从 mpv 官方发布或可信的 Windows 构建渠道获取与本机架构匹配的 `libmpv-2.dll`（通常为 x64）。获取时请注意来源与校验。
- 开发/调试放置位置：
  - 将 `libmpv-2.dll` 置于调试输出目录，使其与可执行文件同级，例如：
    - `bin\Debug\net8.0-windows\`
    - `bin\Release\net8.0-windows\win-x64\`
- 发布与安装包：
  - 运行 `dotnet publish` 后，把 `libmpv-2.dll` 放入发布输出目录 `bin\Release\net8.0-windows\win-x64\publish\`；
  - 安装脚本 [setup.iss](./setup.iss) 会从该目录收集文件并打包（见 [Files] 段配置）。
- 许可与替换：
  - `libmpv` 采用 LGPL 2.1+ 许可。应用对其动态链接，用户可用兼容版本替换该 DLL；
  - 安装包随附第三方许可说明，见根目录 [THIRD-PARTY-NOTICES.txt](./THIRD-PARTY-NOTICES.txt)。
- 常见问题排查：
  - “找不到 DLL”：确认 DLL 与 `IPTV_Player.exe` 同目录，且架构匹配（x64）；
  - 仍无法加载：检查是否缺失其依赖组件，或在“事件查看器/调试输出”中查看更多信息。

### 硬解支持

- 默认使用 `d3d11va`，可在“播放设置”中关闭（[SettingsWindow.xaml](./SettingsWindow.xaml)）
- 关键 mpv 选项初始化位于 `MpvInterop.Initialize()`（[MpvPlayer.cs](./MpvPlayer.cs)）

### 音视频探测与 IPTV “无声”问题

为了“秒开”，TS 场景会强制设置：

- `demuxer-lavf-format=mpegts`
- `demuxer-lavf-probesize=32`
- `demuxer-lavf-analyzeduration=0`

这会让部分频道（尤其是需要更多探测数据的音频格式）出现“有画无声”。若定位到该问题，建议在二次开发时放宽探测参数，或提供开关。

实现位置：[MpvPlayer.cs](./MpvPlayer.cs)

### 字幕与多音轨/字幕轨

- mpv 本身支持外挂字幕/内封字幕渲染（依赖 FFmpeg/libass）
- 当前项目 UI 未提供“音轨/字幕轨切换”入口；如需实现，可通过：
  - 查询 `track-list` / `audio-params` / `sub-params`
  - 设置 `aid` / `sid` 等属性
  - 或调用 `mpv_command` 执行 `cycle` / `set` 命令

---

## 依赖环境

- Windows 10/11
- .NET SDK 8.0+（项目目标框架：`net8.0-windows`）
- NuGet：`ModernWpfUI`（[LibmpvIptvClient.csproj](./LibmpvIptvClient.csproj)）
- 运行时依赖：`libmpv-2.dll`（以及其依赖的 FFmpeg/渲染相关 DLL，取决于具体构建）

---

## 编译与运行

在 `clients/windows/LibmpvIptvClient` 目录下：

```powershell
dotnet restore
dotnet build
dotnet run
```

运行时请确保 `libmpv-2.dll` 能被加载（通常放在可执行文件同目录下）。项目调试输出目录默认：

```text
bin\Debug\net8.0-windows\
```

---

## 二次开发指南

### 关键类与职责

- `MainWindow`
  - UI 事件、频道加载、EPG 刷新、全屏/悬浮窗管理、自动换源策略等（[MainWindow.xaml.cs](./MainWindow.xaml.cs)）
- `MpvInterop`
  - libmpv 封装与常用操作：加载、暂停、Seek、音量、获取属性（[MpvPlayer.cs](./MpvPlayer.cs)）
- `M3UParser`
  - M3U/EXTINF 解析、URL 清洗与协议识别（[Services/M3UParser.cs](./Services/M3UParser.cs)）
- `EpgService`
  - XMLTV 解析、缓存、节目查询（[Services/EpgService.cs](./Services/EpgService.cs)）

### 常见扩展点

- 增加快捷键：在 `MainWindow` 增加 `PreviewKeyDown` / `InputBindings`
- 增加音轨/字幕轨切换：通过 mpv `track-list` + 设置 `aid/sid`
- 收藏/历史持久化：将收藏/播放历史写入 `user_settings.json`（扩展 `PlaybackSettings`）
- 后端 IPTV Checker：读取 `appsettings.json` 并注入 `IptvCheckerClient`，实现“后端频道列表/Logo/EPG”联动
- 音频兼容性：为 TS 探测参数提供可配置项，或在音轨缺失时动态提升 `probesize/analyzeduration`

---

## 示例截图（占位符）

> 维护人员可将截图放入 `docs/screenshots/` 并替换下列占位符。

- 主界面：
  - `![main](docs/screenshots/main.png)`
- 全屏悬浮控制条：
  - `![fullscreen-overlay](docs/screenshots/fullscreen-overlay.png)`
- EPG 面板（含直播/回放状态）：
  - `![epg](docs/screenshots/epg.png)`
- 设置窗口（深色标题栏与主题）：
  - `![settings](docs/screenshots/settings.png)`
