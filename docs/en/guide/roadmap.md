# Roadmap

We are dedicated to continuously improving the IPTV viewing experience.

## Future Plans

- [ ] **AI Recommendation**: Personalized content suggestions based on viewing habits.
- [ ] **Multi-view**: Picture-in-Picture (PiP) and Mosaic view support.
- [ ] **Cloud PVR**: Remote recording to connected cloud storage.
- [ ] **Advanced A/V**: HDR10+ dynamic metadata support, 8K 120fps decoding optimization.
- [ ] **Interactive Features**: Voice barrage (Speech-to-Text), low-latency cloud gaming entry.
- [ ] **Copyright Protection**: Blockchain-based copyright verification.

## Completed Features

- [x] **Timeshift**: Replay-based timeshifting via `catchup-source` with real-time seeking.
- [x] **EPG**: XMLTV (gz) parsing and display.
- [x] **Catchup**: Template-based automatic replay URL generation.
- [x] **M3U Parsing**: Local/Remote playlist support with `#EXTINF` attributes.
- [x] **Channel Mgmt**: Grouping, search, and favorites.
- [x] **Live Optimization**: FCC fast switching, UDP multicast optimization, auto-source switching.
- [x] **Hardware Decoding**: Enabled `d3d11va` by default.
- [x] **UI/UX**: Fullscreen overlay, side drawer, multi-language support (CN/EN).

- [x] **v1.1.2+ Interaction Updates**:
  - EPG status chips: Live = red-filled; Replay = green-filled; clicking the Live chip plays the current channel immediately
  - Current playing indicator: row highlight; green left stripe when replaying (more visible in dark theme)
  - Reminders: single-instance list; centers on first open; checkbox + Select All/Invert for batch deletion; accessible from Tray/Dropdown/Right-click with consistent behavior
  - M3U Management: batch delete and single edit; tray entry; visible above the player in fullscreen
  - Persistent System Tray: Open/Reminders/Manage M3U/Settings/Exit
  - Close Confirmation: ×/ESC only dismisses dialog; “No” minimizes to tray (exit fullscreen first)
  - Dark/Light title bars: Reminders, M3U Manager, Reminder Dialog, and Exit Dialog apply theme immediately after window creation

## Preview / Experimental Features

- [~] Reminder/notification animations: styles and motion are designed; to be enabled in later versions
- [~] Replay chip clickable: under UX evaluation; planned to coexist with “click program title to replay”
