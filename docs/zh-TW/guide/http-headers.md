# HTTP/RTSP Header 設定

本頁說明如何在播放器中設定自訂 HTTP Headers 和 RTSP 參數，以支援特殊播放源。

## 功能概述

播放器支援為 HTTP/HTTPS 流和 RTSP 協定配置自訂 Header 參數，適用於：

- 需要特定 User-Agent 才能訪問的源
- 需要 Referer 或 Cookie 驗證的源
- RTSP 認證和傳輸模式配置

## HTTP Headers 設定（適用於 HTTP/HTTPS 流）

### 支援的欄位

| 欄位 | 說明 | 範例 |
|------|------|------|
| User-Agent | 瀏覽器識別碼 | `Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...` |
| Referer | 來源頁面 | `https://example.com/player` |
| Cookie | 工作階段 Cookie | `session=abc123; token=xyz` |

### 設定格式

在設定介面中，每行一個 Header，格式為 `Field: Value`：

```
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36
Referer: https://example.com/
Cookie: session=abc123
```

### 應用場景

- **IPTV 授權驗證**：部分電信商或平台要求特定的 User-Agent
- **防盜鏈源**：需要驗證 Referer 的直播源
- **登入態維持**：透過 Cookie 保持工作階段

## RTSP 設定

### 傳輸模式

| 模式 | 說明 | 適用場景 |
|------|------|----------|
| TCP | 預設模式，穿透性強，穩定性好 | 大多數網路環境 |
| UDP | 低延遲模式，需要網路支援 UDP | 區域網路、高品質內網源 |
| HTTP Tunnel | HTTP 隧道模式，用於 HTTP 代理環境 | 企業防火牆後的源 |

### User-Agent

RTSP 流專用的 User-Agent，部分設備/平台使用不同的識別碼：

```
VLC/3.0.18 Libmpv
```

### 認證資訊

支援 RTSP 標準認證：

- **使用者名稱**：RTSP 認證使用者名稱
- **密碼**：RTSP 認證密碼（加密儲存）

## 測試功能

### 使用方法

1. 在「測試 URL」輸入框中輸入完整的播放 URL（含認證資訊）
2. 點擊「測試 HTTP」或「測試 RTSP」按鈕
3. 播放器將使用目前設定的 Header 參數啟動測試
4. 查看播放器的偵錯日誌取得詳細資訊

### 注意事項

- 測試 URL 應包含完整的協定前綴（如 `http://` 或 `rtsp://`）
- RTSP URL 如需認證，可直接在建議中嵌入：`rtsp://user:password@host:port/path`
- 測試不會修改已儲存的設定，僅用於驗證配置是否正確

## 設定位置

1. 開啟「設定」→「播放」選項卡
2. 滾動到頁面底部的「HTTP/RTSP Header 設定」區域
3. 根據需要設定 HTTP Headers 和 RTSP 參數
4. 點擊「儲存」應用設定

## 常見問題

### 設定的 Header 不生效

- 檢查 URL 是否以 `http://` 或 `https://` 開頭（HTTP Headers 僅對 HTTP/HTTPS 流生效）
- 確認 Header 格式正確：每行一個，格式為 `Field: Value`
- 查看偵錯日誌確認 Header 是否被正確發送

### RTSP 連線失敗

- 嘗試更換傳輸模式（TCP/UDP/HTTP Tunnel）
- 確認使用者名稱密碼正確，或嘗試直接在建議中嵌入認證資訊
- 檢查防火牆是否阻斷了 RTSP 連接埠（預設 554）

### 密碼安全問題

- RTSP 密碼使用 Windows DPAPI 加密儲存在本機
- 密碼僅在需要時解密，記憶體中不安裝明文
