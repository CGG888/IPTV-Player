---
layout: home

hero:
  name: "SrcBox"
  text: "High Performance MPV-Based IPTV Client"
  tagline: "Modern, lightweight, and efficient IPTV client with advanced EPG and timeshift features."
  image:
    src: /logo.svg
    alt: SrcBox Logo
  actions:
    - theme: brand
      text: Get Started
      link: /guide/
    - theme: alt
      text: View on GitHub
      link: https://github.com/CGG888/SrcBox

features:
  - title: Modern UI
    details: Built with WPF/ModernWpf for a fluid and native Windows experience, supporting both Dark and Light themes.
    icon: 🎨
  - title: High Performance
    details: Powered by libmpv for smooth playback, hardware acceleration (d3d11va), and low resource usage.
    icon: 🚀
  - title: Fast Zapping
    details: Optimized for instant channel switching with FCC technology, delivering a seamless TV experience.
    icon: ⚡
  - title: EPG Support
    details: Full XMLTV (gz) support with automatic day switching and intelligent program matching.
    icon: 📅
  - title: Timeshift & Catchup
    details: Never miss a moment with real-time seeking in live streams and auto-generated catchup replay URLs.
    icon: ⏪
  - title: Channel Management
    details: Easily organize your channels with grouping, searching, and favorites features.
    icon: 📺
---

<script setup>
import { onMounted } from 'vue'

onMounted(() => {
  // Redirect logic if needed, or custom scripts
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

## Overview

**SrcBox** is a high-performance, modern IPTV player designed for the Windows platform.

Built on the powerful **libmpv** playback core and combined with a modern **WPF** interface, it delivers a smooth and stable live viewing experience. It supports core features like M3U playlists, EPG (Electronic Program Guide), and Catchup (Replay), while offering deep optimizations for IPTV scenarios (such as FCC fast channel switching and UDP multicast optimization), making it the ideal choice for watching live TV on your PC.

## Feature Demos

<div style="display: flex; flex-direction: column; gap: 60px; align-items: center; padding-bottom: 40px;">
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Fast Zapping</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">Instant channel switching with FCC optimization</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/fast-zapping.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
  
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Catchup / Replay</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">Watch past programs with auto-generated catchup URLs</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/catchup.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
  
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Timeshift</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">Seek back in live streams seamlessly</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/timeshift.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
</div>

## Screenshots

<div style="display: flex; gap: 20px; overflow-x: auto; padding-bottom: 20px; justify-content: start;">
  <img src="/screenshots/main.png" alt="Main Interface" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <img src="/screenshots/fullscreen-overlay.png" alt="Fullscreen Overlay" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <img src="/screenshots/settings.png" alt="Settings" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
</div>

<div style="margin-top: 40px; padding-top: 20px; border-top: 1px solid var(--vp-c-divider); font-size: 14px; color: var(--vp-c-text-2);">
  <p><strong>Disclaimer:</strong> The videos, screenshots, and demos shown on this page are for functional demonstration purposes only and are not actual playable media resources. <strong>This project does not provide any m3u playlist files or channel data, nor is it responsible for any third-party data sources.</strong></p>
</div>
