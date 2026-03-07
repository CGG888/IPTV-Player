# Features

## Core Playback
- **Playback Control**: Play/Pause, Stop, Seek, Fast Forward/Rewind.
- **Volume Control**: Slider adjustment, mute support (0-100).
- **Status Indicators**: Live/Replay/Timeshift status displayed in overlay.

## IPTV Specifics

### M3U Parsing
Supports local/remote M3U playlists, compatible with UTF-8/GB18030 encoding. Supports `#EXTINF` extended attributes.

### EPG Support
XMLTV (gz) format support with automatic day switching and `tvg-id` matching.
[Usage and configuration guide](./epg)

### Catchup (Replay)
Template-based catchup URL generation (e.g., `{utc:yyyyMMddHHmmss}`).
[Usage and customization guide](./catchup-timeshift)

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/catchup.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### Timeshift
Seek back in live stream history (depends on `catchup-source`).
[Usage and customization guide](./catchup-timeshift)

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/timeshift.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### Playback Speed
- Supported for Timeshift and Replay only; Live does not support speed
- Available speeds: 0.5×, 0.75×, 1.0×, 1.25×, 1.5×, 1.75×, 2.0×, 3.0×, 5.0×
- Audio pitch correction is enabled automatically to keep voices natural
- Consistent UI in fullscreen and window; adapts to dark/light themes

### Channel Management
Grouping, Search, and Favorites (runtime-only).

### Channel List Badges (R/T)
- R: Catchup available; T: Timeshift available
- Hover tooltips localized (ZH/EN/ZH-TW/RU)
- Consistent style and layout in fullscreen and window
### Optimization (FCC)
Fast Channel Change and UDP multicast optimization.

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/fast-zapping.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

## Fullscreen & Overlay
- **Double-click Fullscreen**: Toggle fullscreen by double-clicking the video area.
- **Overlay Bar**: Auto-shows at bottom on mouse move; supports controls & EPG toggle.
- **Side Drawer**: Auto-shows channel list (right) or EPG (left) on mouse hover near edges.

## Updates since v1.1.2

- **EPG Status Chips**: Chip-style badges on the right side of each program row
  - Live = red-filled chip; Replay = green-filled chip (auto adapts to dark/light themes)
  - Clicking the Live chip plays the current channel immediately
  - The current playing row is highlighted; when replaying, a green left stripe is shown for better visibility (especially in dark mode)
- **Reminders & Notifications**
  - Single-instance reminder list; centers to the player on first open; user-dragged position is respected afterward
  - Checkbox + Select All/Invert for batch deletion
  - Accessible from Tray / Dropdown / Right-click with consistent behavior
  - Due/success toasts don’t steal focus (bottom-right vs. centered strategies are clearly separated)
- **M3U Management**
  - New “Manage M3U List” window (same style as reminders) with checkbox batch deletion and single edit
  - Tray entry; consistent entries from dropdown/right-click; visible in fullscreen above the player
- **System Tray**
  - Always-on icon; double-click to restore; menu includes Open/Reminders/Manage M3U/Settings/Exit
- **Close Confirmation**
  - Close “×/ESC” only dismisses dialog; “Yes” exits, “No” minimizes to tray (leave fullscreen first)
  - Three-line i18n text synchronized (ZH/EN/ZH-TW/RU)
- **Theme Sync**
  - Title bars for Reminders, M3U Manager, Reminder Dialog, and Exit Dialog apply theme at OnSourceInitialized for instant dark/light consistency
