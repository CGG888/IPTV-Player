---
layout: home

hero:
  name: "SrcBox"
  text: "Высокопроизводительный IPTV плеер для Windows"
  tagline: "Современный IPTV клиент на базе libmpv с поддержкой EPG, Timeshift и мгновенного переключения каналов."
  image:
    src: /logo.svg
    alt: Логотип SrcBox
  actions:
    - theme: brand
      text: Начать
      link: /ru/guide/
    - theme: alt
      text: GitHub
      link: https://github.com/CGG888/SrcBox

features:
  - title: Современный интерфейс
    details: Построен на WPF/ModernWpf, идеально сочетается с темной и светлой темами Windows 10/11, обеспечивая плавный пользовательский опыт.
    icon: 🎨
  - title: Высокая производительность
    details: Использует ядро libmpv, поддерживает аппаратное декодирование (d3d11va), потребляет мало ресурсов и мгновенно переключает каналы.
    icon: 🚀
  - title: Мгновенное переключение (FCC)
    details: Технология Fast Channel Change (FCC), оптимизированная для IPTV, позволяет забыть о буферизации при переключении каналов.
    icon: ⚡
  - title: Умный телегид (EPG)
    details: Полная поддержка XMLTV (gz) с автоматическим переключением по дням и интеллектуальным сопоставлением программ.
    icon: 📅
  - title: Timeshift и Архив
    details: Поддержка перемотки прямого эфира и автоматическая генерация ссылок на архив (Catchup) на основе шаблонов.
    icon: ⏪
  - title: Управление каналами
    details: Удобная группировка, поиск и избранное для легкого управления вашим плейлистом.
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

## Обзор проекта

**SrcBox** — это высокопроизводительный, современный IPTV плеер, разработанный специально для платформы Windows.

Он построен на мощном ядре воспроизведения **libmpv** в сочетании с современным интерфейсом **WPF**, что обеспечивает плавный и стабильный просмотр прямых трансляций. Плеер поддерживает не только M3U плейлисты, EPG и архив передач, но и глубоко оптимизирован для IPTV (например, быстрое переключение каналов FCC, оптимизация UDP multicast), что делает его идеальным выбором для просмотра ТВ на ПК.

## Демонстрация функций

<div style="display: flex; flex-direction: column; gap: 60px; align-items: center; padding-bottom: 40px;">
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Мгновенное переключение (FCC)</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">Экстремально оптимизированное переключение каналов</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/fast-zapping.mp4" type="video/mp4">
        Ваш браузер не поддерживает видео тег.
      </video>
    </ClientOnly>
  </div>
  
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Catchup / Архив передач</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">Автоматическая генерация ссылок на архив, чтобы не пропустить интересные программы</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/catchup.mp4" type="video/mp4">
        Ваш браузер не поддерживает видео тег.
      </video>
    </ClientOnly>
  </div>
  
  <div style="width: 100%; max-width: 800px; text-align: center;">
    <h3>Timeshift / Перемотка эфира</h3>
    <p style="opacity: 0.6; margin-bottom: 10px;">Перемотка прямого эфира в реальном времени</p>
    <ClientOnly>
      <video controls muted preload="metadata" playsinline width="100%" style="border-radius: 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15); background-color: #000;">
        <source src="/screenshots/timeshift.mp4" type="video/mp4">
        Ваш браузер не поддерживает видео тег.
      </video>
    </ClientOnly>
  </div>
</div>

## Скриншоты

<div style="display: flex; gap: 20px; overflow-x: auto; padding-bottom: 20px; justify-content: start;">
  <img src="/screenshots/main.png" alt="Главное окно" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <img src="/screenshots/fullscreen-overlay.png" alt="Полноэкранный режим" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
  <img src="/screenshots/settings.png" alt="Настройки" style="height: 350px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
</div>

<div style="margin-top: 40px; padding-top: 20px; border-top: 1px solid var(--vp-c-divider); font-size: 14px; color: var(--vp-c-text-2);">
  <p><strong>Отказ от ответственности:</strong> Все видео и скриншоты на этой странице предназначены только для демонстрации функций. <strong>Этот проект не предоставляет никаких M3U плейлистов или содержащихся в них данных каналов, а также не несет ответственности за сторонние источники данных.</strong></p>
</div>
