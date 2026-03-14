# 源匣（SrcBox）（Windows / WPF）

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6)](https://www.microsoft.com/windows)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](http://makeapullrequest.com)

[English](./README_EN.md) | [中文](./README.md)

**源匣（SrcBox）** 是一款专为 Windows 平台打造的高性能、现代化的 IPTV 播放器。

它基于强大的 **libmpv** 播放内核构建，结合 **WPF** 的现代化界面设计，为您带来流畅、稳定的直播观看体验。不仅支持 M3U 播放列表、EPG 电子节目单、回看等核心功能，还针对 IPTV 场景进行了深度优化（如 FCC 快速切台、UDP 组播优化），是您在 PC 上观看电视直播的理想选择。

---

> **郑重声明 (Disclaimer)**：
>
> 1. 本页面展示的所有视频、截图及演示画面仅作**功能展示用途**，并非实际可播放或可用的媒体资源。
> 2. 本项目**不提供任何 m3u 播放列表文件及其中包含的频道数据**，亦不对第三方数据源负责。
> 3. SrcBox 仅作为一个开源的播放器工具，用户需自行寻找合法的播放源。请遵守当地法律法规。

## 目录

- [项目概览](#项目概览)
- [功能清单与变更记录](#功能清单与变更记录)
  - [核心播放功能](#核心播放功能)
  - [IPTV 专用功能](#iptv-专用功能)
  - [全屏与悬浮层](#全屏与悬浮层)
- [路线图 (Roadmap)](#路线图-roadmap)
- [国际化说明](#国际化说明)
- [界面与交互](#界面与交互)
- [配置文件与参数说明](#配置文件与参数说明)
- [libmpv 引擎说明](#libmpv-引擎说明)
- [开发指南](#开发指南)
  - [环境依赖](#环境依赖)
  - [编译与运行](#编译与运行)
  - [故障排查](#故障排查)
  - [测试与贡献](#测试与贡献)
- [示例截图](#示例截图)

---

## 项目概览

**源匣（SrcBox）** 以 `libmpv-2.dll` 为播放内核，通过 `WindowsFormsHost` 承载视频窗口句柄，提供 IPTV 频道列表、EPG、回放、时移、录播、上传队列等完整能力。

- 播放器入口窗口：[MainWindow.xaml](./MainWindow.xaml)
- libmpv 绑定封装：[MpvPlayer.cs](./MpvPlayer.cs)
- M3U 解析：[Services/M3UParser.cs](./Services/M3UParser.cs)
- EPG 解析与缓存：[Services/EpgService.cs](./Services/EpgService.cs)
- 主状态与播放模式：[Architecture/Presentation/Mvvm/MainWindow/MainShellViewModel.cs](./Architecture/Presentation/Mvvm/MainWindow/MainShellViewModel.cs)

---

## 技术架构

本项目采用 **C# / WPF** 开发，核心架构如下：

- **UI 层**：基于 WPF (ModernWpf)，提供流畅的现代化交互体验。
- **架构层**：`Architecture/` 下按 Application / Platform / Presentation 分层，播放、设置、同步逻辑均模块化拆分。
- **互操作层**：通过 `MpvPlayer.cs` 与 `MpvPlayerEngineAdapter` 封装 libmpv 调用。
- **渲染层**：利用 `WindowsFormsHost` 承载 Win32 窗口句柄，将 mpv 的渲染输出嵌入 WPF 界面，解决 WPF 原生媒体元素性能不足的问题。
- **服务层**：
  - `M3UParser`：高效的正则表达式解析器，支持极其复杂的 M3U 扩展标签。
  - `EpgService`：基于 `XmlSerializer` 的异步 EPG 加载与内存缓存机制。

### 项目结构

```text
📂 SrcBox
├── 📂 Architecture    # 分层架构 (Application/Platform/Presentation)
├── 📂 Services        # 核心服务 (M3U/EPG/录播/WebDAV/通知等)
├── 📂 Controls        # 抽屉与弹窗控件 (EPG/录播/时移/上传队列)
├── 📂 Resources       # 国际化与主题资源
├── 📂 Tests           # MSTest 自动化测试
├── 📄 MainWindow.*.cs # 主窗口分片逻辑
└── 📄 MpvPlayer.cs    # libmpv 封装
```

---

## 功能清单与变更记录

版本说明：仓库可核对的 Git Tag 为 `1.0.1` ～ `1.1.2`，当前分支描述为 `1.1.2-6-gedf98b6`，工程版本号为 `1.1.4`（见 `LibmpvIptvClient.csproj` / `setup.iss`）。下表仅填写可从 Tag/代码符号交叉验证的版本；无法映射到已打 Tag 的功能，标注为“1.1.2 后（未打 Tag）”。

### 核心播放功能

| 功能点 | 说明 | 参数/示例 | 变更记录 |
| :--- | :--- | :--- | :--- |
| **播放控制** | 播放/暂停、停止、快进/快退、拖动进度 | 快捷键：暂无默认快捷键，支持鼠标操作 | 1.0.1 起 |
| **音量调节** | 滑块调节，支持静音 | 范围 0-100 | 1.0.7 起 |
| **状态标识** | 全屏条显示直播/回放/时移状态 | 自动检测 | 1.0.5 起 |
| **预约通知与预约播放** | 支持节目预约提醒与到点播放策略 | 支持“仅提醒/自动播放”模式 | 1.1.2 起 |
| **精简模式** | 提供精简播放器窗口形态与独立交互 | 支持窗口态/全屏态同步 | 1.1.2 后（未打 Tag） |

### IPTV 专用功能

| 功能点 | 说明 | 参数/示例 | 变更记录 |
| :--- | :--- | :--- | :--- |
| **M3U 解析** | 支持本地/远程 M3U，兼容 UTF-8/GB18030 | 支持 `#EXTINF` 扩展属性 | 1.0.1 起 |
| **EPG 节目单** | 支持 XMLTV (gz)，按日切换 | 自动匹配 `tvg-id` | 1.0.1 起 |
| **回放 (Catchup)** | 支持基于模板的回放 URL 生成 | `{utc:yyyyMMddHHmmss}` 等 | 1.0.1 起 |
| **时移 (Time-Shift)** | 拖动进度条回看直播历史 | 依赖 `catchup-source` | 1.0.2 起 |
| **频道管理** | 分组、搜索、收藏、历史 | 收藏与历史本地持久化 | 分组/搜索/收藏：1.0.1 起；历史：1.0.4 起 |
| **FCC/UDP 优化** | 快速切台与组播优化开关 | 设置中开启 | 1.0.1 起 |
| **录播与上传** | 本地录播、WebDAV 上传、上传队列 | 支持本地/远端双模式 | 1.1.2 后（未打 Tag） |

### 全屏与悬浮层

- **双击全屏**：在视频区域双击切换全屏。
- **悬浮控制条**：全屏下鼠标移动到底部自动显示，支持播放控制与 EPG 切换。
- **侧边抽屉**：全屏下鼠标贴边显示频道列表（右侧）或 EPG（左侧）。

---

## 路线图 (Roadmap)

我们致力于持续提升 IPTV 观看体验。以下是未来的开发计划及已实现功能：

### 未来计划
- [ ] **智能推荐**：基于观看习惯的 AI 节目推荐。
- [ ] **多视角同屏**：支持多源拼屏观看（画中画/四分屏）。
- [ ] **云端录制 (PVR)**：支持连接远程存储进行节目录制。
- [ ] **高级视听体验**：HDR10+ 动态元数据支持、8K 120fps 解码优化。
- [ ] **互动功能**：语音弹幕（语音转字幕）、低延迟云游戏入口。
- [ ] **版权保护**：区块链版权校验（防篡改溯源）。
- [ ] **测试体系扩展**：补齐播放状态机、录播索引、EPG 同步相关单元测试。
- [ ] **录播体验增强**：录制中信息同步、远端元数据一致性与失败恢复优化。
- [ ] **播放链路优化**：继续降低切台延迟，优化弱网和高抖动场景稳定性。
- [ ] **多源治理能力**：增强源健康检测、自动降级与可观测性日志。

### 已实现功能
- [x] **时移 (Time-Shift)**：基于 `catchup-source` 的回放式时移，支持实时拖动回看。
- [x] **EPG 电子节目单**：支持 XMLTV (gz) 解析与展示。
- [x] **频道回看**：基于模板自动生成回放 URL。
- [x] **M3U 解析**：支持本地/远程列表及 `#EXTINF` 扩展属性。
- [x] **频道管理**：分组、搜索、收藏功能。
- [x] **直播优化**：FCC 快速切台、UDP 组播优化、自动换源。
- [x] **硬件解码**：默认开启 `d3d11va`。
- [x] **录播能力**：本地录播、录播索引、上传队列、WebDAV 集成。
- [x] **预约能力**：节目预约提醒、预约列表、到点自动播放策略。
- [x] **精简模式**：支持精简窗口、顶栏交互、窗口态与全屏态状态同步。
- [x] **UI/UX**：全屏悬浮控制、侧边抽屉、多语言支持（中/英/繁中/俄语）。

---

## 国际化说明

本项目支持多语言切换（目前支持简体中文、繁体中文、英语、俄语）。

详细的国际化指南与翻译贡献方式，请参阅 **[国际化文档](https://srcbox.top/i18n)**。

### 快速贡献

1.  在 `Resources/` 目录下找到 `Strings.en-US.xaml`。
2.  复制并重命名为目标语言代码（如 `Strings.fr-FR.xaml`）。
3.  翻译内容并提交 Pull Request。

---

## 界面与交互

### 主界面区域

- **顶部标题栏**：包含菜单按钮（打开文件、设置等）与窗口控制。
- **中央区域**：视频播放窗口。
- **底部/悬浮栏**：播放进度、音量、功能开关（EPG/列表）。

### 快捷键

目前主要依赖鼠标交互。
- **Esc**：退出全屏。
- **双击**：切换全屏。

---

## 配置文件与参数说明

### `user_settings.json`

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

---

## libmpv 引擎说明

本项目依赖 `libmpv-2.dll`。

- **硬解**：默认开启 `d3d11va`。
- **无声问题**：部分 IPTV 源音频探测较慢，已设置 `probesize=32` 加速起播，可能导致短暂无声。

---

## 开发指南

### 环境依赖

- **操作系统**：Windows 10 / 11 (x64)
- **开发工具**：Visual Studio 2022 或 JetBrains Rider
- **SDK**：.NET 8.0 SDK
- **依赖库**：`libmpv-2.dll`（仓库根目录提供，构建时复制到输出目录）

### 编译与运行

```powershell
# 还原依赖
dotnet restore

# 编译（Debug）
dotnet build

# 运行
dotnet run

# 测试
dotnet test .\Tests\LibmpvIptvClient.Tests.csproj
```

**注意**：若运行环境缺少 `libmpv-2.dll`，应用会在启动阶段报错；默认仓库中的 dll 会在构建时复制到输出目录。

### 故障排查

| 现象 | 可能原因 | 解决方案 |
| :--- | :--- | :--- |
| **程序启动即崩溃** | 缺少 `libmpv-2.dll` | 下载对应架构的 dll 放入运行目录 |
| **有画面无声音** | 音频流探测超时 | 属正常优化策略，可尝试切换音轨或重启播放 |
| **EPG 显示“无数据”** | 网络问题或格式不支持 | 检查 XMLTV URL 是否可访问，是否为 GZIP 格式 |
| **设置不保存** | 权限不足 | 确保程序目录有写入权限 |

### 测试与贡献

#### 贡献流程

1. **Fork** 本仓库。
2. 创建特性分支：`git checkout -b feature/AmazingFeature`。
3. 提交代码：`git commit -m 'feat: Add some AmazingFeature'` (请遵循 [Conventional Commits](https://www.conventionalcommits.org/))。
4. 推送分支：`git push origin feature/AmazingFeature`。
5. 提交 **Pull Request**。

#### 代码规范

- 保持现有的 C# 代码风格（K&R / Allman 混合，视文件而定，建议遵循 .editorconfig）。
- UI 修改请注意深色/浅色主题适配。

#### 性能基准

- **CPU 占用**：1080p 播放时应 < 15% (i5-8250U 基准)。
- **内存占用**：稳定播放时应 < 500MB。
- **启动时间**：冷启动 < 2秒。

---

## 隐私与安全

- **本地优先**：所有播放列表、EPG 缓存及用户配置（收藏、设置）均存储于用户本地设备 (`user_settings.json`)，不会上传至任何云端服务器。
- **网络请求**：应用仅在请求您指定的 M3U/EPG 地址、检查软件更新（GitHub API）或下载 CDN 加速资源时发起网络连接。

---

## 示例截图

- 主界面  
  ![main](docs/screenshots/main.png)

- 全屏悬浮控制条  
  ![fullscreen-overlay](docs/screenshots/fullscreen-overlay.png)

- 设置窗口  
  ![settings](docs/screenshots/settings.png)
