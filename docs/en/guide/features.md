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

### Catchup (Replay)
Template-based catchup URL generation (e.g., `{utc:yyyyMMddHHmmss}`).

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/catchup.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### Timeshift
Seek back in live stream history (depends on `catchup-source`).

<video controls width="100%" poster="/logo.svg">
  <source src="/screenshots/timeshift.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### Channel Management
Grouping, Search, and Favorites (runtime-only).

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
