# 技術架構

本專案採用 **C# / WPF** 開發，核心架構如下：

- **UI 層**：基於 WPF (ModernWpf)，提供流暢的現代化互動體驗。
- **互操作層**：通過 `MpvPlayer.cs` 封裝 libmpv 的 C API，實現 P/Invoke 呼叫。
- **渲染層**：利用 `WindowsFormsHost` 承載 Win32 視窗控制代碼，將 mpv 的渲染輸出嵌入 WPF 介面，解決 WPF 原生媒體元素效能不足的問題。
- **服務層**：
  - `M3UParser`：高效的正則表示式解析器，支援極其複雜的 M3U 擴展標籤。
  - `EpgService`：基於 `XmlSerializer` 的非同步 EPG 載入與記憶體快取機制。

## 專案結構

```text
📂 SrcBox
├── 📂 Models          # 數據模型 (Channel, EpgProgram, Source)
├── 📂 Services        # 核心服務 (M3U解析, EPG下載, 頻道管理)
├── 📂 Resources       # 資源檔案 (多語言字串, 樣式, 字體)
├── 📂 Interop         # libmpv 互操作層 (P/Invoke)
├── 📄 MainWindow.xaml # 主介面邏輯
└── 📄 MpvPlayer.cs    # 播放器核心封裝
```

## libmpv 引擎說明

本專案依賴 `libmpv-2.dll`。

- **硬解**：預設開啟 `d3d11va`。
- **無聲問題**：部分 IPTV 來源音訊探測較慢，已設定 `probesize=32` 加速起播，可能導致短暫無聲。
