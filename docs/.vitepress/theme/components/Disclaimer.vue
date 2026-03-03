<script setup>
import { ref, onMounted, computed } from 'vue'
import { useData } from 'vitepress'

const show = ref(false)
const { lang } = useData()

// i18n texts
const i18n = {
  zh: {
    title: '⚠️ 郑重声明 (Disclaimer)',
    point1: '1. 本页面展示的所有视频、截图及演示画面仅作<strong>功能展示用途</strong>，并非实际可播放或可用的媒体资源。',
    point2: '2. 本项目不提供任何 m3u 播放列表文件及其中包含的频道数据，亦不对第三方数据源负责。',
    footer: 'IPTV Player 仅作为一个开源的播放器工具，用户需自行寻找合法的播放源。请遵守当地法律法规。',
    button: '我已知晓 (I Understand)'
  },
  en: {
    title: '⚠️ Disclaimer',
    point1: '1. All videos, screenshots, and demos shown on this page are for <strong>functional demonstration purposes only</strong> and are not actual playable media resources.',
    point2: '2. This project does not provide any m3u playlist files or channel data, nor is it responsible for any third-party data sources.',
    footer: 'IPTV Player is an open-source player tool only. Users must find legal playback sources themselves. Please comply with local laws and regulations.',
    button: 'I Understand'
  }
}

const currentText = computed(() => {
  return lang.value === 'zh' ? i18n.zh : i18n.en
})

// Function to close the disclaimer and save preference
const closeDisclaimer = () => {
  show.value = false
  localStorage.setItem('iptv-player-disclaimer-accepted', 'true')
}

// Check if user has already accepted the disclaimer
onMounted(() => {
  const accepted = localStorage.getItem('iptv-player-disclaimer-accepted')
  if (!accepted) {
    // Show after a slight delay to ensure user sees it
    setTimeout(() => {
      show.value = true
    }, 500)
  }
})
</script>

<template>
  <div v-if="show" class="disclaimer-overlay">
    <div class="disclaimer-modal" role="alert" aria-live="assertive">
      <div class="disclaimer-header">
        {{ currentText.title }}
      </div>
      
      <div class="disclaimer-content">
        <p class="highlight-text" v-html="currentText.point1"></p>
        
        <div class="critical-warning">
          <strong>{{ currentText.point2 }}</strong>
        </div>
        
        <p class="secondary-text">
          {{ currentText.footer }}
        </p>
      </div>
      
      <div class="disclaimer-actions">
        <button 
          @click="closeDisclaimer" 
          class="accept-btn"
          :aria-label="currentText.button"
        >
          {{ currentText.button }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Overlay background */
.disclaimer-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background-color: rgba(0, 0, 0, 0.7);
  z-index: 9999;
  display: flex;
  justify-content: center;
  align-items: center;
  backdrop-filter: blur(4px);
}

/* Modal container */
.disclaimer-modal {
  background-color: #ffffff;
  color: #1a1a1a;
  width: 90%;
  max-width: 600px;
  border-radius: 12px;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  overflow: hidden;
  border: 2px solid #ef4444; /* Red border for emphasis */
  animation: modal-pop 0.3s ease-out;
}

@keyframes modal-pop {
  0% { transform: scale(0.9); opacity: 0; }
  100% { transform: scale(1); opacity: 1; }
}

/* Header */
.disclaimer-header {
  background-color: #ef4444; /* Red-500 */
  color: #ffffff;
  padding: 16px 24px;
  font-size: 20px;
  font-weight: 700;
  text-align: center;
  letter-spacing: 0.5px;
}

/* Content */
.disclaimer-content {
  padding: 24px;
  font-size: 16px;
  line-height: 1.6;
}

.highlight-text {
  margin-bottom: 16px;
}

/* Critical warning block */
.critical-warning {
  background-color: #fef2f2; /* Red-50 */
  border-left: 4px solid #b91c1c; /* Red-700 */
  color: #b91c1c; /* Red-700 for high contrast */
  padding: 16px;
  margin: 16px 0;
  font-size: 18px; /* Larger font size */
  font-weight: 700;
}

.secondary-text {
  font-size: 14px;
  color: #525252; /* Neutral-600 */
  margin-top: 16px;
}

/* Actions */
.disclaimer-actions {
  padding: 0 24px 24px;
  display: flex;
  justify-content: center;
}

.accept-btn {
  background-color: #1a1a1a; /* Black background for high contrast */
  color: #ffffff;
  border: none;
  padding: 12px 32px;
  border-radius: 6px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s;
}

.accept-btn:hover {
  background-color: #333333;
}

.accept-btn:focus {
  outline: 3px solid #ef4444;
  outline-offset: 2px;
}

/* Dark mode adjustments (optional, but ensuring readability) */
:root.dark .disclaimer-modal {
  background-color: #1e1e1e;
  color: #e5e5e5;
  border-color: #ef4444;
}

:root.dark .critical-warning {
  background-color: #450a0a; /* Red-950 */
  color: #fecaca; /* Red-200 */
}

:root.dark .accept-btn {
  background-color: #ffffff;
  color: #000000;
}

:root.dark .accept-btn:hover {
  background-color: #e5e5e5;
}
</style>
