# HTTP/RTSP Header Settings

This page explains how to configure custom HTTP Headers and RTSP parameters in the player to support special streaming sources.

## Feature Overview

The player supports custom Header parameters for HTTP/HTTPS streams and RTSP protocol:

- Custom User-Agent for sources requiring specific browser identification
- Referer or Cookie verification for protected sources
- RTSP authentication and transport mode configuration

## HTTP Headers Settings (for HTTP/HTTPS Streams)

### Supported Fields

| Field | Description | Example |
|-------|-------------|---------|
| User-Agent | Browser identifier | `Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...` |
| Referer | Referrer page | `https://example.com/player` |
| Cookie | Session cookie | `session=abc123; token=xyz` |

### Configuration Format

In the settings interface, one Header per line with format `Field: Value`:

```
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36
Referer: https://example.com/
Cookie: session=abc123
```

### Use Cases

- **IPTV Authorization**: Some carriers or platforms require specific User-Agent
- **Anti-hotlink Sources**: Streaming sources requiring Referer verification
- **Session Maintenance**: Keeping sessions alive with Cookies

## RTSP Settings

### Transport Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| TCP | Default mode, strong compatibility, good stability | Most network environments |
| UDP | Low latency mode, requires UDP network support | LAN, high-quality internal sources |
| HTTP Tunnel | HTTP tunnel mode for HTTP proxy environments | Sources behind corporate firewalls |

### User-Agent

RTSP-specific User-Agent, some devices/platforms use different identifiers:

```
VLC/3.0.18 Libmpv
```

### Authentication

Supports RTSP standard authentication:

- **Username**: RTSP authentication username
- **Password**: RTSP authentication password (encrypted storage)

## Testing Function

### How to Use

1. Enter a complete playback URL (including authentication info) in the "Test URL" field
2. Click "Test HTTP" or "Test RTSP" button
3. The player will start testing using the currently configured Header parameters
4. Check the player's debug log for detailed information

### Notes

- Test URL should include complete protocol prefix (`http://` or `rtsp://`)
- For RTSP URLs requiring authentication, you can embed credentials directly in the URL: `rtsp://user:password@host:port/path`
- Testing does not modify saved settings, only verifies if configuration is correct

## Configuration Location

1. Open "Settings" → "Playback" tab
2. Scroll to the bottom to find "HTTP/RTSP Header Settings" section
3. Configure HTTP Headers and RTSP parameters as needed
4. Click "Save" to apply settings

## FAQ

### Configured Headers Not Working

- Check if URL starts with `http://` or `https://` (HTTP Headers only apply to HTTP/HTTPS streams)
- Confirm Header format is correct: one per line, format `Field: Value`
- Check debug log to confirm Headers are being sent correctly

### RTSP Connection Failed

- Try switching transport modes (TCP/UDP/HTTP Tunnel)
- Confirm username and password are correct, or try embedding authentication directly in the URL
- Check if firewall is blocking RTSP port (default 554)

### Password Security

- RTSP passwords are encrypted using Windows DPAPI and stored locally
- Passwords are only decrypted when needed, plaintext is never stored in memory
