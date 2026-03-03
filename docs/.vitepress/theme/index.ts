// .vitepress/theme/index.ts
import DefaultTheme from 'vitepress/theme'
import Disclaimer from './components/Disclaimer.vue'
import { h } from 'vue'
import './custom.css'

export default {
  extends: DefaultTheme,
  Layout() {
    return h(DefaultTheme.Layout, null, {
      'layout-bottom': () => h(Disclaimer)
    })
  }
}
