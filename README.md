# 源匣（SrcBox）（Windows / WPF）

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6)](https://www.microsoft.com/windows)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](http://makeapullrequest.com)

[English](./README_EN.md) | [中文](./README.md)

**源匣（SrcBox）** 是一款专为 Windows 平台打造的高性能、现代化的 IPTV 播放器。

它基于强大的 **libmpv** 播放内核构建，结合 **WPF** 的现代化界面设计，为您带来流畅、稳定的直播观看体验。不仅支持 M3U 播放列表、EPG 电子节目单、回看等核心功能，还针对 IPTV 场景进行了深度优化（如 FCC 快速切台、UDP 组播优化），是您在 PC 上观看电视直播的理想选择。

---

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

**源匣（SrcBox）** 以 `libmpv-2.dll` 为播放内核，通过 `WindowsFormsHost` 承载视频窗口句柄，提供 IPTV 频道列表、分组、EPG、回放、源切换等功能。

- 播放器入口窗口：[MainWindow.xaml](./MainWindow.xaml)
- libmpv 绑定封装：[MpvPlayer.cs](./MpvPlayer.cs)
- M3U 解析：[Services/M3UParser.cs](./Services/M3UParser.cs)
- EPG 解析与缓存：[Services/EpgService.cs](./Services/EpgService.cs)

---

## 技术架构

本项目采用 **C# / WPF** 开发，核心架构如下：

- **UI 层**：基于 WPF (ModernWpf)，提供流畅的现代化交互体验。
- **互操作层**：通过 `MpvPlayer.cs` 封装 libmpv 的 C API，实现 P/Invoke 调用。
- **渲染层**：利用 `WindowsFormsHost` 承载 Win32 窗口句柄，将 mpv 的渲染输出嵌入 WPF 界面，解决 WPF 原生媒体元素性能不足的问题。
- **服务层**：
  - `M3UParser`：高效的正则表达式解析器，支持极其复杂的 M3U 扩展标签。
  - `EpgService`：基于 `XmlSerializer` 的异步 EPG 加载与内存缓存机制。

### 项目结构

```text
📂 SrcBox
├── 📂 Models          # 数据模型 (Channel, EpgProgram, Source)
├── 📂 Services        # 核心服务 (M3U解析, EPG下载, 频道管理)
├── 📂 Resources       # 资源文件 (多语言字符串, 样式, 字体)
├── 📂 Interop         # libmpv 互操作层 (P/Invoke)
├── 📄 MainWindow.xaml # 主界面逻辑
└── 📄 MpvPlayer.cs    # 播放器核心封装
```

---

## 功能清单与变更记录

### 核心播放功能

| 功能点 | 说明 | 参数/示例 | 变更记录 |
| :--- | :--- | :--- | :--- |
| **播放控制** | 播放/暂停、停止、快进/快退、拖动进度 | 快捷键：暂无默认快捷键，支持鼠标操作 | v1.0.0 (Initial) |
| **音量调节** | 滑块调节，支持静音 | 范围 0-100 | v1.0.0 (Initial) |
| **状态标识** | 全屏条显示直播/回放/时移状态 | 自动检测 | v1.1.0 (2024-05, Update) <br>新增“时移”状态显示，优化回放绿色标识 |

### IPTV 专用功能

| 功能点 | 说明 | 参数/示例 | 变更记录 |
| :--- | :--- | :--- | :--- |
| **M3U 解析** | 支持本地/远程 M3U，兼容 UTF-8/GB18030 | 支持 `#EXTINF` 扩展属性 | v1.0.0 (Initial) |
| **EPG 节目单** | 支持 XMLTV (gz)，按日切换 | 自动匹配 `tvg-id` | v1.0.0 (Initial) |
| **回放 (Catchup)** | 支持基于模板的回放 URL 生成 | `{utc:yyyyMMddHHmmss}` 等 | v1.0.0 (Initial) |
| **时移 (Time-Shift)** | 拖动进度条回看直播历史 | 依赖 `catchup-source` | v1.1.0 (2024-05, New) <br>支持全屏/窗口同步，实时时间轴 |
| **频道管理** | 分组、搜索、收藏 | 收藏列表运行时有效 | v1.0.0 (Initial) |
| **FCC/UDP 优化** | 快速切台与组播优化开关 | 设置中开启 | v1.0.0 (Initial) |

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

### 已实现功能
- [x] **时移 (Time-Shift)**：基于 `catchup-source` 的回放式时移，支持实时拖动回看。
- [x] **EPG 电子节目单**：支持 XMLTV (gz) 解析与展示。
- [x] **频道回看**：基于模板自动生成回放 URL。
- [x] **M3U 解析**：支持本地/远程列表及 `#EXTINF` 扩展属性。
- [x] **频道管理**：分组、搜索、收藏功能。
- [x] **直播优化**：FCC 快速切台、UDP 组播优化、自动换源。
- [x] **硬件解码**：默认开启 `d3d11va`。
- [x] **UI/UX**：全屏悬浮控制、侧边抽屉、多语言支持（中/英）。

---

## 国际化说明

本项目支持多语言切换（目前支持简体中文与英语）。

> **注意**：英文翻译目前主要由 AI 辅助生成，可能存在不准确或不地道之处。如果您发现任何错误，非常欢迎提交 PR 进行修正！

### 术语对照表

| 中文 | English | 备注 |
| :--- | :--- | :--- |
| 直播 | Live | |
| 回放 | Replay | 曾用名 Playback |
| 时移 | Timeshift | |
| 节目单 | EPG / Program Guide | |
| 频道 | Channel | |
| 源 | Source | |

### 贡献翻译

1. 找到 `Resources/Strings.en-US.xaml` 文件。
2. 修改对应的 `String` 值。
3. 提交 PR，并在描述中说明修改原因（如“修正语法错误”或“优化表达”）。

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
- **依赖库**：`libmpv-2.dll` (必须手动放置在输出目录)

### 编译与运行

```powershell
# 还原依赖
dotnet restore

# 编译（Debug）
dotnet build

# 运行
dotnet run
```

**注意**：运行前请确保 `libmpv-2.dll` 已放置在 `bin\Debug\net8.0-windows\` 目录下，否则程序会闪退或报错。

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
