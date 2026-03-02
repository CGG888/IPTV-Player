# IPTV Player (Windows / WPF)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6)](https://www.microsoft.com/windows)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](http://makeapullrequest.com)

[English](./README_EN.md) | [中文](./README.md)

**IPTV Player** is a high-performance, modern IPTV player designed for the Windows platform.

Built on the powerful **libmpv** playback core and combined with a modern **WPF** interface, it delivers a smooth and stable live viewing experience. It supports core features like M3U playlists, EPG (Electronic Program Guide), and Catchup (Replay), while offering deep optimizations for IPTV scenarios (such as FCC fast channel switching and UDP multicast optimization), making it the ideal choice for watching live TV on your PC.

---

## Table of Contents

- [Overview](#overview)
- [Features & Changelog](#features--changelog)
  - [Core Playback](#core-playback)
  - [IPTV Specifics](#iptv-specifics)
  - [Fullscreen & Overlay](#fullscreen--overlay)
- [Roadmap](#roadmap)
- [Internationalization](#internationalization)
- [Interface & Interaction](#interface--interaction)
- [Configuration & Parameters](#configuration--parameters)
- [libmpv Engine](#libmpv-engine)
- [Development Guide](#development-guide)
  - [Prerequisites](#prerequisites)
  - [Build & Run](#build--run)
  - [Troubleshooting](#troubleshooting)
  - [Testing & Contribution](#testing--contribution)
- [Screenshots](#screenshots)

---

## Overview

**IPTV Player** uses `libmpv-2.dll` as its playback core, hosting the video window via `WindowsFormsHost`. It provides features such as IPTV channel lists, grouping, EPG, catchup/replay, and source switching.

- Entry Window: [MainWindow.xaml](./MainWindow.xaml)
- libmpv Wrapper: [MpvPlayer.cs](./MpvPlayer.cs)
- M3U Parser: [Services/M3UParser.cs](./Services/M3UParser.cs)
- EPG Service: [Services/EpgService.cs](./Services/EpgService.cs)

---

## Technical Architecture

This project is developed using **C# / WPF**. The core architecture is as follows:

- **UI Layer**: Built with WPF (ModernWpf), providing a smooth and modern user experience.
- **Interop Layer**: Encapsulates libmpv C API via `MpvPlayer.cs` using P/Invoke.
- **Rendering Layer**: Uses `WindowsFormsHost` to host a Win32 window handle, embedding mpv's rendering output into the WPF interface to bypass WPF's native media element limitations.
- **Service Layer**:
  - `M3UParser`: High-performance regex-based parser supporting complex M3U extended tags.
  - `EpgService`: Asynchronous EPG loading and in-memory caching based on `XmlSerializer`.

### Project Structure

```text
📂 IPTV-Player
├── 📂 Models          # Data Models (Channel, EpgProgram, Source)
├── 📂 Services        # Core Services (M3U Parsing, EPG, Channel Mgmt)
├── 📂 Resources       # Resources (Localization, Styles, Fonts)
├── 📂 Interop         # libmpv Interop Layer (P/Invoke)
├── 📄 MainWindow.xaml # Main Window Logic
└── 📄 MpvPlayer.cs    # Player Core Wrapper
```

---

## Features & Changelog

### Core Playback

| Feature | Description | Params/Example | Changelog |
| :--- | :--- | :--- | :--- |
| **Playback Control** | Play/Pause, Stop, Seek, Fast Forward/Rewind | No default hotkeys yet; mouse supported | v1.0.0 (Initial) |
| **Volume Control** | Slider adjustment, mute support | Range 0-100 | v1.0.0 (Initial) |
| **Status Indicators** | Live/Replay/Timeshift status in overlay | Auto-detection | v1.1.0 (2024-05, Update) <br>Added "Timeshift" status; optimized green replay badge |

### IPTV Specifics

| Feature | Description | Params/Example | Changelog |
| :--- | :--- | :--- | :--- |
| **M3U Parsing** | Local/Remote M3U, UTF-8/GB18030 compatible | Supports `#EXTINF` attributes | v1.0.0 (Initial) |
| **EPG** | XMLTV (gz) support, day switching | Auto-match `tvg-id` | v1.0.0 (Initial) |
| **Catchup (Replay)** | Template-based catchup URL generation | `{utc:yyyyMMddHHmmss}` etc. | v1.0.0 (Initial) |
| **Timeshift** | Seek back in live stream history | Depends on `catchup-source` | v1.1.0 (2024-05, New) <br>Syncs between fullscreen/window; real-time timeline |
| **Channel Mgmt** | Grouping, Search, Favorites | Favorites are runtime-only | v1.0.0 (Initial) |
| **FCC/UDP** | Fast Channel Change & Multicast optimization | Toggle in Settings | v1.0.0 (Initial) |

### Fullscreen & Overlay

- **Double-click Fullscreen**: Toggle fullscreen by double-clicking the video area.
- **Overlay Bar**: Auto-shows at bottom on mouse move; supports controls & EPG toggle.
- **Side Drawer**: Auto-shows channel list (right) or EPG (left) on mouse hover near edges.

---

## Roadmap

We are dedicated to continuously improving the IPTV viewing experience. Here are our future plans and completed features:

### Future Plans
- [ ] **AI Recommendation**: Personalized content suggestions based on viewing habits.
- [ ] **Multi-view**: Picture-in-Picture (PiP) and Mosaic view support.
- [ ] **Cloud PVR**: Remote recording to connected cloud storage.
- [ ] **Advanced A/V**: HDR10+ dynamic metadata support, 8K 120fps decoding optimization.
- [ ] **Interactive Features**: Voice barrage (Speech-to-Text), low-latency cloud gaming entry.
- [ ] **Copyright Protection**: Blockchain-based copyright verification.

### Completed Features
- [x] **Timeshift**: Replay-based timeshifting via `catchup-source` with real-time seeking.
- [x] **EPG**: XMLTV (gz) parsing and display.
- [x] **Catchup**: Template-based automatic replay URL generation.
- [x] **M3U Parsing**: Local/Remote playlist support with `#EXTINF` attributes.
- [x] **Channel Mgmt**: Grouping, search, and favorites.
- [x] **Live Optimization**: FCC fast switching, UDP multicast optimization, auto-source switching.
- [x] **Hardware Decoding**: Enabled `d3d11va` by default.
- [x] **UI/UX**: Fullscreen overlay, side drawer, multi-language support (CN/EN).

---

## Internationalization

This project supports multi-language switching (currently Simplified Chinese and English).

> **Note**: The English translation is primarily AI-generated and may contain inaccuracies. If you find any errors, PRs are highly welcome!

### Terminology

| Chinese | English | Notes |
| :--- | :--- | :--- |
| 直播 | Live | |
| 回放 | Replay | Formerly "Playback" |
| 时移 | Timeshift | |
| 节目单 | EPG / Program Guide | |
| 频道 | Channel | |
| 源 | Source | |

### Contributing Translations

1. Locate `Resources/Strings.en-US.xaml`.
2. Modify the corresponding `String` values.
3. Submit a PR with a description of the changes (e.g., "Fix grammar" or "Improve phrasing").

---

## Interface & Interaction

### Main Interface

- **Top Bar**: Menu buttons (Open File, Settings, etc.) and window controls.
- **Center**: Video playback area.
- **Bottom/Overlay**: Progress bar, volume, toggles (EPG/List).

### Shortcuts

Currently relies mostly on mouse interaction.
- **Esc**: Exit fullscreen.
- **Double-click**: Toggle fullscreen.

---

## Configuration & Parameters

### `user_settings.json`

Located in the application run directory, stores user preferences.

```json
{
  "Hwdec": true,              // Hardware decoding
  "SourceTimeoutSec": 3,      // Source switch timeout (seconds)
  "TimeshiftHours": 2,        // Timeshift duration (hours)
  "Language": "en-US",        // Interface language
  "ThemeMode": "Dark"         // Theme mode (Dark/Light/System)
}
```

---

## libmpv Engine

This project depends on `libmpv-2.dll`.

- **Hardware Decoding**: `d3d11va` is enabled by default.
- **No Audio**: Some IPTV sources have slow audio probing; `probesize=32` is set to speed up start, which might cause brief silence.

---

## Development Guide

### Prerequisites

- **OS**: Windows 10 / 11 (x64)
- **IDE**: Visual Studio 2022 or JetBrains Rider
- **SDK**: .NET 8.0 SDK
- **Dependency**: `libmpv-2.dll` (Must be manually placed in the output directory)

### Build & Run

```powershell
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Run
dotnet run
```

**Note**: Ensure `libmpv-2.dll` is placed in `bin\Debug\net8.0-windows\` before running, otherwise the app will crash.

### Troubleshooting

| Issue | Possible Cause | Solution |
| :--- | :--- | :--- |
| **Crash on startup** | Missing `libmpv-2.dll` | Download x64 dll and place in run dir |
| **Video but no audio** | Audio probe timeout | Normal optimization; try switching tracks or restarting |
| **EPG "No Data"** | Network or format issue | Check XMLTV URL accessibility and GZIP format |
| **Settings not saving** | Permission denied | Ensure write permission to app directory |

### Testing & Contribution

#### Workflow

1. **Fork** the repository.
2. Create a feature branch: `git checkout -b feature/AmazingFeature`.
3. Commit changes: `git commit -m 'feat: Add some AmazingFeature'` (Follow [Conventional Commits](https://www.conventionalcommits.org/)).
4. Push branch: `git push origin feature/AmazingFeature`.
5. Submit a **Pull Request**.

#### Code Style

- Follow existing C# style (K&R / Allman hybrid, see .editorconfig if available).
- Ensure UI changes adapt to both Dark and Light themes.

#### Performance Benchmarks

- **CPU Usage**: < 15% @ 1080p (i5-8250U baseline).
- **Memory**: < 500MB during stable playback.
- **Startup**: < 2 seconds (cold start).

---

## Privacy & Security

- **Local First**: All playlists, EPG caches, and user configurations (favorites, settings) are stored locally on your device (`user_settings.json`) and are never uploaded to any cloud server.
- **Network Activity**: The application only initiates network connections when requesting your specified M3U/EPG URLs, checking for updates (GitHub API), or downloading CDN resources.

---

## Screenshots

- Main Interface  
  ![main](docs/screenshots/main.png)

- Fullscreen Overlay  
  ![fullscreen-overlay](docs/screenshots/fullscreen-overlay.png)

- Settings  
  ![settings](docs/screenshots/settings.png)
