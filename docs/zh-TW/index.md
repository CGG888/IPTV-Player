---
layout: home

hero:
  name: "源匣 (SrcBox)"
  text: "高效能 Windows IPTV 播放器"
  tagline: "基於 libmpv 核心，支援 EPG、時移回看、快速切台的現代化 IPTV 播放工具。"
  image:
    src: /logo.svg
    alt: 源匣 Logo
  actions:
    - theme: brand
      text: 開始使用
      link: /zh-TW/guide/
    - theme: alt
      text: 訪問 GitHub
      link: https://github.com/CGG888/SrcBox

features:
  - title: 現代化介面
    details: 基於 WPF/ModernWpf 建構，完美適配 Windows 10/11 深色與淺色主題，提供流暢的原生體驗。
    icon: 🎨
  - title: 極致效能
    details: 採用 libmpv 播放核心，支援硬體解碼 (d3d11va)，低資源佔用，毫秒級快速切台。
    icon: 🚀
  - title: 極速切台 (FCC)
    details: 針對 IPTV 場景深度最佳化的快速切台技術，告別緩衝等待，享受絲滑換台體驗。
    icon: ⚡
  - title: 智慧節目單
    details: 完整支援 XMLTV (gz) 格式 EPG，支援自動按日切換和智慧節目匹配。
    icon: 📅
  - title: 時移與回看
    details: 支援直播流即時拖動時移，以及基於模板自動生成的 Catchup 回放，不錯過任何精彩瞬間。
    icon: ⏪
  - title: 頻道管理
    details: 提供便捷的頻道分組、搜尋和收藏功能，輕鬆管理您的播放清單。
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

## 專案概覽

**源匣 (SrcBox)** 是一款專為 Windows 平台打造的高效能、現代化的 IPTV 播放器。

它基於強大的 **libmpv** 播放核心建構，結合 **WPF** 的現代化介面設計，為您帶來流暢、穩定的直播觀看體驗。不僅支援 M3U 播放清單、EPG 電子節目單、回看等核心功能，還針對 IPTV 場景進行了深度最佳化（如 FCC 快速切台、UDP 組播最佳化），是您在 PC 上觀看電視直播的理想選擇。

## 功能演示

<div style="display: flex; flex-direction: column; gap: 60px; align-items: center; padding-bottom: 40px;">
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>毫秒級切台 (FCC)</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">極致最佳化的快速換台體驗</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/fast-zapping.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
  
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Catchup / 節目回放</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">基於模板自動生成回看地址，不錯過精彩節目</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/catchup.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
  
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Timeshift / 直播時移</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">即時拖動進度條，隨時回看直播歷史</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/timeshift.mp4" type="video/mp4">
        Your browser does not support the video tag.
      </video>
    </ClientOnly>
  </div>
</div>

## 介面預覽

<div style="display: flex; gap: 20px; overflow-x: auto; padding-bottom: 20px; justify-content: start;">
  <img src="/screenshots/main.png" alt="主介面" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <img src="/screenshots/fullscreen-overlay.png" alt="全屏懸浮" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <img src="/screenshots/settings.png" alt="設定" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
</div>

<div style="margin-top: 40px; padding-top: 20px; border-top: 1px solid var(--vp-c-divider); font-size: 14px; color: var(--vp-c-text-2);">
  <p><strong>免責聲明：</strong> 本頁面展示的所有影片、截圖及演示畫面僅作功能展示用途，並非實際可播放或可用的媒體資源。<strong>本專案不提供任何 m3u 播放清單檔案及其中包含的頻道數據，亦不對第三方數據源負責。</strong></p>
</div>
