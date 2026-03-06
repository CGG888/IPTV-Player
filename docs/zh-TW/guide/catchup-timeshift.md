# 回看與時移（使用與自訂）

本頁說明播放器內如何使用回看/時移，以及如何透過 M3U 的 `catchup-source` 自訂時間參數生成規則（HTTP 單播與 RTSP 單播通用）。

## 概念與關係

- **回看（Catchup/Replay）**：從 EPG 節目單選擇已播節目，依節目起迄時間生成回放位址並播放。
- **時移（Timeshift）**：在「直播」基礎上拖動進度條回退到過去某時間點；播放器會依「時移游標時間」生成回放位址並播放。
- **重點**：播放器不理解營運商私有協議語意，只負責把 URL 模板中的「時間佔位符」替換為實際時間字串，然後播放替換後的 URL。

## 前置條件

要讓某頻道支援回看/時移，至少滿足其一：

- **M3U 中提供 `catchup-source`**（建議；可每頻道獨立設定）
- **播放器設定中提供全域回放/時移位址模板**（當頻道沒有 `catchup-source` 時的備援）

回看依賴節目單時間：

- 有 EPG：能使用精準的節目開始/結束時間。
- 無 EPG：回看入口可能不可用或只能用占位節目生成時間段（取決於來源容忍度）。

## 播放器內操作（回看）

1. 選擇並播放一個頻道（進入直播）。
2. 打開 EPG 面板（左側節目單）。
3. 點選標記為「回放/回看」的節目（通常是當前時間之前的節目）。
4. 播放器生成「帶時間參數」的回放 URL 並播放，狀態顯示為「回放」。

## 播放器內操作（時移）

1. 直播播放中，開啟「時移」。
2. 拖動進度條回退到想看的時間點。
3. 放開後播放器會依該時間點生成回放 URL 並播放，狀態顯示為「時移」。
4. 關閉時移後回到直播。

## M3U 寫法（建議：頻道自帶回放模板）

在 `#EXTINF` 行加入 `catchup-source`，例如：

```m3u
#EXTINF:-1 tvg-id="CCTV1" tvg-name="CCTV1" catchup="default" catchup-source="https://example.com/live/index.m3u8?starttime=${(b)yyyyMMdd|UTC}T${(b)HHmmss|UTC}&endtime=${(e)yyyyMMdd|UTC}T${(e)HHmmss|UTC}",CCTV1-綜合
https://example.com/live/index.m3u8
```

說明：

- `catchup-source` 是「回放位址模板」，可與直播位址相同，也可為專用回放入口。
- `catchup` 常見為 `default/append/shift`，主要是播放清單生態的標註；本播放器關鍵在於 `catchup-source` 是否存在且可生成有效 URL。

## 時間佔位符（自訂的核心）

你可以在 `catchup-source`（或全域模板）使用以下佔位符：

### 1）通用 `${(b)FORMAT}` / `${(e)FORMAT}`（建議）

- `${(b)FORMAT}`：開始時間
- `${(e)FORMAT}`：結束時間
- `FORMAT` 是時間格式字串
- 預設輸出本地時間；若 `FORMAT` 以 `|UTC` 結尾則輸出 UTC

本地時間範例：

```text
?playseek=${(b)yyyyMMddHHmmss}-${(e)yyyyMMddHHmmss}
```

UTC 範例：

```text
?starttime=${(b)yyyyMMdd|UTC}T${(b)HHmmss|UTC}&endtime=${(e)yyyyMMdd|UTC}T${(e)HHmmss|UTC}
```

### 2）`{utc:FORMAT}` / `{utcend:FORMAT}`（UTC）

```text
?begin={utc:yyyyMMddHHmmss}&end={utcend:yyyyMMddHHmmss}
```

### 3）`{start}` / `{end}`（本地固定格式）

```text
?start={start}&end={end}
```

### 4）rtp2httpd 相容簡寫巨集

為了方便從 rtp2httpd 遷移，SrcBox 原生支援了以下簡寫（rtp2httpd 支援但 SrcBox 原本沒有的格式）：

- `YmdHMS` -> `yyyyMMddHHmmss`（14 位時間）
- `Ymd` -> `yyyyMMdd`
- `HMS` -> `HHmmss`
- `${timestamp}` -> Unix 時間戳（秒，10 位）
- `${duration}` -> 持續時長（秒）

rtp2httpd 其他格式的對應寫法：

- `yyyyMMddHHmmssGMT` -> `${(b)yyyyMMddHHmmss}GMT`
- ISO 8601 (UTC + Z) -> `${(b)yyyy-MM-ddTHH:mm:ss|UTC}Z`
- ISO 8601 (Local + Offset) -> `${(b)yyyy-MM-ddTHH:mm:ssK}`

範例：

```text
?playseek={utc:YmdHMS}-{utcend:YmdHMS}
?start=${timestamp}&duration=${duration}
```

### 5）Unix 時間戳（秒）— 開始/結束

支援以下 10 位 Unix 時間戳佔位符：

- 開始時間：`${timestamp}`、`{timestamp}`、`${(b)timestamp}`、`${(b)unix}`、`${(b)epoch}`
- 結束時間：`${end_timestamp}`、`{end_timestamp}`、`${(e)timestamp}`、`${(e)unix}`、`${(e)epoch}`
- 時長（秒）：`${duration}`、`{duration}`
常見介面範例：
範例：

// 1) start/end 參數介面
?start=${timestamp}&end=${end_timestamp}

// 2) playseek（開始-結束）
playseek=${(b)timestamp}-${(e)timestamp}

// 3) 開始 + 時長
?start=${timestamp}&duration=${duration}
```

M3U 整合範例：

```m3u
#EXTINF:-1 tvg-name="示例頻道" catchup="default" catchup-source="https://example.com/live/index.m3u8?start=${timestamp}&end=${end_timestamp}",示例頻道
https://example.com/live/index.m3u8

#EXTINF:-1 tvg-name="示例頻道" catchup="append" catchup-source="https://example.com/live/index.m3u8?playseek=${(b)timestamp}-${(e)timestamp}",示例頻道
https://example.com/live/index.m3u8

#EXTINF:-1 tvg-name="示例頻道" catchup="default" catchup-source="https://example.com/live/index.m3u8?start=${timestamp}&duration=${duration}",示例頻道
https://example.com/live/index.m3u8
playseek=${(b)timestamp}-${(e)timestamp}
```


### HTTP 單播（HLS m3u8，UTC + T）

```m3u
#EXTINF:-1 tvg-name="示例頻道" catchup="default" catchup-source="https://example.com/live/index.m3u8?starttime=${(b)yyyyMMdd|UTC}T${(b)HHmmss|UTC}&endtime=${(e)yyyyMMdd|UTC}T${(e)HHmmss|UTC}",示例頻道
https://example.com/live/index.m3u8
```

### RTSP 單播（PLTV 常見 playseek）

```m3u
#EXTINF:-1 tvg-name="示例頻道" catchup="append" catchup-source="rtsp://example.com/live.smil?playseek=${(b)yyyyMMddHHmmss}-${(e)yyyyMMddHHmmss}",示例頻道
rtsp://example.com/live.smil
```

### RTSP/HTTP 通用（starttime/endtime）

```m3u
#EXTINF:-1 tvg-name="示例頻道" catchup="default" catchup-source="https://example.com/live/stream?starttime=${(b)yyyyMMddHHmmss}&endtime=${(e)yyyyMMddHHmmss}",示例頻道
https://example.com/live/stream
```

## 優先級與覆蓋

- 頻道 `catchup-source` → 生成基礎位址與時間佔位（建議每頻道獨立配置）。
- 設定中的「回放/時移模板」 → 當頻道沒有 `catchup-source` 時作為備援生成。
- 時間覆蓋（設定頁「時間覆蓋」）→ 若啟用，只重寫「時間片段」（佈局/鍵名/編碼），不改網域/路徑/非時間參數；對頻道模板與備援模板皆生效。
- 調試建議：先用頻道模板或備援模板生成可播連結，再用「時間覆蓋」統一為營運商要求的時間表達（如 starttime/endtime、UTC 或 Unix 秒）。

## 私有時間佔位符回饋

- 如需本文未涵蓋的時間格式，或營運商使用私有佔位/路徑式時間片段：
  - 可在設定頁啟用「時間覆蓋」選擇最接近的佈局與編碼進行適配；
  - 或到 Issues 提交需求（附示例與說明），我們會評估加入預設或提供更通用的自訂能力。
- 提交地址：https://github.com/CGG888/SrcBox/issues
## 更多寫法示例（豐富格式）

- **RFC3339/ISO-8601（帶時區資訊）**
  - 以 UTC 並帶 Z：  
    `start=${(b)yyyy-MM-ddTHH:mm:ss|UTC}Z&end=${(e)yyyy-MM-ddTHH:mm:ss|UTC}Z`
  - 依時間類型自動輸出 Z 或偏移：  
    `start=${(b)yyyy-MM-ddTHH:mm:ssK}&end=${(e)yyyy-MM-ddTHH:mm:ssK}`
  - 指定偏移（例如 `+08:00`）：  
    `start=${(b)yyyy-MM-ddTHH:mm:ss}(${(b)zzz})&end=${(e)yyyy-MM-ddTHH:mm:ss}(${(e)zzz})`

- **僅日期 / 僅時間**
  - `begin_date=${(b)yyyyMMdd}&begin_time=${(b)HHmmss}`
  - `end_date=${(e)yyyyMMdd}&end_time=${(e)HHmmss}`

- **毫秒片段（視來源是否支援）**
  - `start=${(b)yyyyMMddHHmmssfff}&end=${(e)yyyyMMddHHmmssfff}`

- **花括號 UTC 寫法的等價形式**
  - `begin={utc:yyyy-MM-ddTHH:mm:ss}&end={utcend:yyyy-MM-ddTHH:mm:ss}`

提示：
- `FORMAT` 使用 .NET 時間格式；`|UTC` 代表以 UTC 轉換後再格式化。
- `K` 在 UTC 時輸出 `Z`，本地時間輸出偏移；`zzz` 始終輸出偏移（如 `+08:00`）。
- 是否需要毫秒、`T`、時區偏移取決於來源端協議，請依實際需求選擇。

## 與「設定」的關係（建議用法）

- **優先使用 M3U 的 `catchup-source`**：每頻道獨立，最穩定。
- **設定中的模板**：更適合作為全域備援（當頻道未提供 `catchup-source` 時統一生成）。

## 排錯建議

- 點回看後日誌仍出現 `${(b)...}`/`{utc:...}`：通常代表模板未被替換或未走到回看流程，先確認該頻道是否真的使用 `catchup-source`。
- 回放 URL 正確但無法播放：多為來源端不支援該參數名/時間格式/時區，請依來源端要求調整為本地時間或 UTC，或更換參數名。
