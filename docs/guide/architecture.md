# 技术架构

本项目采用 **C# / WPF** 开发，核心架构如下：

- **UI 层**：基于 WPF (ModernWpf)，提供流畅的现代化交互体验。
- **互操作层**：通过 `MpvPlayer.cs` 封装 libmpv 的 C API，实现 P/Invoke 调用。
- **渲染层**：利用 `WindowsFormsHost` 承载 Win32 窗口句柄，将 mpv 的渲染输出嵌入 WPF 界面，解决 WPF 原生媒体元素性能不足的问题。
- **服务层**：
  - `M3UParser`：高效的正则表达式解析器，支持极其复杂的 M3U 扩展标签。
  - `EpgService`：基于 `XmlSerializer` 的异步 EPG 加载与内存缓存机制。

## 项目结构

```text
📂 SrcBox
├── 📂 Models          # 数据模型 (Channel, EpgProgram, Source)
├── 📂 Services        # 核心服务 (M3U解析, EPG下载, 频道管理)
├── 📂 Resources       # 资源文件 (多语言字符串, 样式, 字体)
├── 📂 Interop         # libmpv 互操作层 (P/Invoke)
├── 📄 MainWindow.xaml # 主界面逻辑
└── 📄 MpvPlayer.cs    # 播放器核心封装
```

## libmpv 引擎说明

本项目依赖 `libmpv-2.dll`。

- **硬解**：默认开启 `d3d11va`。
- **无声问题**：部分 IPTV 源音频探测较慢，已设置 `probesize=32` 加速起播，可能导致短暂无声。
