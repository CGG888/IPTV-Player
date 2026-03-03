# FAQ

## General
**Q: How do I change the language?**
A: Go to Settings > Interface > Language and select your preferred language.

**Q: Does it support remote M3U files?**
A: Yes, you can load playlists from a URL or a local file.

**Q: Why is there no sound?**
A: Some IPTV streams have slow audio probing. We set `probesize=32` to speed up playback start, which might cause brief silence initially. Try switching tracks or restarting the stream.

## Troubleshooting
**Q: The app crashes on startup.**
A: Ensure `libmpv-2.dll` is placed in the output directory (`bin\Debug\net8.0-windows\`).

**Q: EPG shows "No Data".**
A: Check if the XMLTV URL is accessible and in GZIP format. Ensure `tvg-id` matches.
