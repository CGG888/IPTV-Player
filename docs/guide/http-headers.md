# HTTP/RTSP Header 设置

本页说明如何在播放器中配置自定义 HTTP Headers 和 RTSP 参数，以支持特殊播放源。

## 功能概述

播放器支持为 HTTP/HTTPS 流和 RTSP 协议配置自定义 Header 参数，适用于：

- 需要特定 User-Agent 才能访问的源
- 需要 Referer 或 Cookie 验证的源
- RTSP 认证和传输模式配置

## HTTP Headers 设置（适用于 HTTP/HTTPS 流）

### 支持的字段

| 字段 | 说明 | 示例 |
|------|------|------|
| User-Agent | 浏览器标识 | `Mozilla/5.0 (Windows NT 10.0; Win64; x64)...` |
| Referer | 来源页面 | `https://example.com/player` |
| Cookie | 会话 Cookie | `session=abc123; token=xyz` |

### 配置格式

在设置界面中，每行一个 Header，格式为 `Field: Value`：

```
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36
Referer: https://example.com/
Cookie: session=abc123
```

### 应用场景

- **IPTV 授权验证**：部分运营商或平台要求特定的 User-Agent
- **防盗链源**：需要验证 Referer 的直播源
- **登录态维持**：通过 Cookie 保持会话

## RTSP 设置

### 传输模式

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| TCP | 默认模式，穿透性强，稳定性好 | 大多数网络环境 |
| UDP | 低延迟模式，需要网络支持 UDP | 局域网、高质量内网源 |
| HTTP Tunnel | HTTP 隧道模式，用于 HTTP 代理环境 | 企业防火墙后的源 |

### User-Agent

RTSP 流专用的 User-Agent，部分设备/平台使用不同的标识：

```
VLC/3.0.18 Libmpv
```

### 认证信息

支持 RTSP 标准认证：

- **用户名**：RTSP 认证用户名
- **密码**：RTSP 认证密码（加密存储）

## 测试功能

### 使用方法

1. 在「测试 URL」输入框中输入完整的播放 URL（含认证信息）
2. 点击「测试 HTTP」或「测试 RTSP」按钮
3. 播放器将使用当前配置的 Header 参数启动测试
4. 查看播放器的调试日志获取详细信息

### 注意事项

- 测试 URL 应包含完整的协议前缀（如 `http://` 或 `rtsp://`）
- RTSP URL 如需认证，可直接在 URL 中嵌入：`rtsp://user:password@host:port/path`
- 测试不会修改已保存的设置，仅用于验证配置是否正确

## 配置位置

1. 打开「设置」→「播放」选项卡
2. 滚动到页面底部的「HTTP/RTSP Header 设置」区域
3. 根据需要配置 HTTP Headers 和 RTSP 参数
4. 点击「保存」应用设置

## 常见问题

### 配置的 Header 不生效

- 检查 URL 是否以 `http://` 或 `https://` 开头（HTTP Headers 仅对 HTTP/HTTPS 流生效）
- 确认 Header 格式正确：每行一个，格式为 `Field: Value`
- 查看调试日志确认 Header 是否被正确发送

### RTSP 连接失败

- 尝试更换传输模式（TCP/UDP/HTTP Tunnel）
- 确认用户名密码正确，或尝试直接在 URL 中嵌入认证信息
- 检查防火墙是否阻断了 RTSP 端口（默认 554）

### 密码安全问题

- RTSP 密码使用 Windows DPAPI 加密存储在本地
- 密码仅在需要时解密，内存中不保存明文
