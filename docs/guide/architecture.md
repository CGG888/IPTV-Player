# Technical Architecture

This project is developed using **C# / WPF**. The core architecture is as follows:

- **UI Layer**: Built with WPF (ModernWpf), providing a smooth and modern user experience.
- **Interop Layer**: Encapsulates libmpv C API via `MpvPlayer.cs` using P/Invoke.
- **Rendering Layer**: Uses `WindowsFormsHost` to host a Win32 window handle, embedding mpv's rendering output into the WPF interface to bypass WPF's native media element limitations.
- **Service Layer**:
  - `M3UParser`: High-performance regex-based parser supporting complex M3U extended tags.
  - `EpgService`: Asynchronous EPG loading and in-memory caching based on `XmlSerializer`.

## Project Structure

```text
📂 SrcBox
├── 📂 Models          # Data Models (Channel, EpgProgram, Source)
├── 📂 Services        # Core Services (M3U Parsing, EPG, Channel Mgmt)
├── 📂 Resources       # Resources (Localization, Styles, Fonts)
├── 📂 Interop         # libmpv Interop Layer (P/Invoke)
├── 📄 MainWindow.xaml # Main Window Logic
└── 📄 MpvPlayer.cs    # Player Core Wrapper
```

## libmpv Engine

This project depends on `libmpv-2.dll`.

- **Hardware Decoding**: `d3d11va` is enabled by default.
- **No Audio Issue**: Some IPTV sources have slow audio probing; `probesize=32` is set to speed up start, which might cause brief silence initially. This is an expected optimization strategy.
