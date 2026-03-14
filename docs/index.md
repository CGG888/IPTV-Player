---
layout: home

hero:
  name: "源匣 (SrcBox)"
  text: "高性能 Windows IPTV 播放器"
  tagline: "基于 libmpv 内核，支持 EPG、时移回看、快速切台的现代化 IPTV 播放工具。"
  image:
    src: /logo.svg
    alt: 源匣 Logo
  actions:
    - theme: brand
      text: 开始使用
      link: /guide/
    - theme: alt
      text: 访问 GitHub
      link: https://github.com/CGG888/SrcBox

features:
  - title: 现代化界面
    details: 基于 WPF/ModernWpf 构建，完美适配 Windows 10/11 深色与浅色主题，提供流畅的原生体验。
    icon: 🎨
  - title: 极致性能
    details: 采用 libmpv 播放内核，支持硬件解码 (d3d11va)，低资源占用，毫秒级快速切台。
    icon: 🚀
  - title: 极速切台 (FCC)
    details: 针对 IPTV 场景深度优化的快速切台技术，告别缓冲等待，享受丝滑换台体验。
    icon: ⚡
  - title: 智能节目单
    details: 完整支持 XMLTV (gz) 格式 EPG，支持自动按日切换和智能节目匹配。
    icon: 📅
  - title: 时移与回看
    details: 支持直播流实时拖动时移，以及基于模板自动生成的 Catchup 回放，不错过任何精彩瞬间。
    icon: ⏪
  - title: 预约与提醒
    details: 支持节目预约通知与到点自动播放，提供预约列表和批量管理能力。
    icon: ⏰
  - title: 录播与上传
    details: 支持本地录播、录播索引与 WebDAV 上传队列，适配本地/远端双模式。
    icon: ⬆️
  - title: 频道管理
    details: 提供频道分组、搜索、收藏与历史记录，本地持久化管理播放列表。
    icon: 📺
---

<script setup>
import { onMounted } from 'vue'

onMounted(() => {
  // Custom logic
})
</script>

<style>
/* Hide volume controls for WebKit browsers (Chrome, Edge, Safari) */
video::-webkit-media-controls-volume-slider,
video::-webkit-media-controls-mute-button,
video::-webkit-media-controls-volume-control-hover-background,
video::-webkit-media-controls-volume-panel {
  display: none !important;
}
</style>

## 项目概览

**源匣 (SrcBox)** 是一款专为 Windows 平台打造的高性能、现代化的 IPTV 播放器。

它基于强大的 **libmpv** 播放内核构建，结合 **WPF** 的现代化界面设计，为您带来流畅、稳定的直播观看体验。不仅支持 M3U 播放列表、EPG 电子节目单、回看等核心功能，还针对 IPTV 场景进行了深度优化（如 FCC 快速切台、UDP 组播优化），是您在 PC 上观看电视直播的理想选择。

## 功能演示

<div style="display: flex; flex-direction: column; gap: 60px; align-items: center; padding-bottom: 40px;">
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>毫秒级切台 (FCC)</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">极致优化的快速换台体验</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/fast-zapping.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
  
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Catchup / 节目回放</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">基于模板自动生成回看地址，不错过精彩节目</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/catchup.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
  
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Timeshift / 直播时移</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">实时拖动进度条，随时回看直播历史</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/timeshift.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
</div>

## 界面预览

<div style="display: flex; gap: 20px; overflow-x: auto; padding-bottom: 20px; justify-content: start;">
  <img src="/screenshots/main.png" alt="主界面" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <img src="/screenshots/fullscreen-overlay.png" alt="全屏悬浮" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <img src="/screenshots/settings.png" alt="设置" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
</div>

<div style="margin-top: 40px; padding-top: 20px; border-top: 1px solid var(--vp-c-divider); font-size: 14px; color: var(--vp-c-text-2);">
  <p><strong>免责声明：</strong> 本页面展示的所有视频、截图及演示画面仅作功能展示用途，并非实际可播放或可用的媒体资源。<strong>本项目不提供任何 m3u 播放列表文件及其中包含的频道数据，亦不对第三方数据源负责。</strong></p>
</div>
