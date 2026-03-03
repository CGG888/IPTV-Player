import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "SrcBox",
  description: "A modern IPTV player based on libmpv",
  base: "/",
  
  // Favicon configuration
  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/logo.svg' }]
  ],
  
  // Theme related configurations
  themeConfig: {
    logo: '/logo.svg',
    siteTitle: 'SrcBox',
    socialLinks: [
      { icon: 'github', link: 'https://github.com/CGG888/SrcBox' }
    ],
    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2024-present CGG888'
    },
    search: {
      provider: 'local'
    }
  },

  // Internationalization
  locales: {
    root: {
      label: '简体中文',
      lang: 'zh',
      link: '/',
      themeConfig: {
        nav: [
          { text: '首页', link: '/' },
          { text: '指南', link: '/guide/' },
          { text: '下载', link: 'https://github.com/CGG888/SrcBox/releases' },
          { text: '问题反馈', link: 'https://github.com/CGG888/SrcBox/issues' }
        ],
        sidebar: [
          {
            text: '指南',
            items: [
              { text: '项目介绍', link: '/guide/' },
              { text: '功能特性', link: '/guide/features' },
              { text: '技术架构', link: '/guide/architecture' },
              { text: '开发指南', link: '/guide/development' },
              { text: '配置说明', link: '/guide/configuration' },
              { text: '国际化', link: '/i18n' },
              { text: '路线图', link: '/guide/roadmap' },
              { text: '常见问题', link: '/guide/faq' }
            ]
          }
        ],
        outline: {
          label: '页面导航'
        },
        docFooter: {
          prev: '上一页',
          next: '下一页'
        },
        lastUpdated: {
          text: '最后更新于'
        },
        editLink: {
          pattern: 'https://github.com/CGG888/SrcBox/edit/main/docs/:path',
          text: '在 GitHub 上编辑此页'
        }
      }
    },
    en: {
      label: 'English',
      lang: 'en',
      link: '/en/',
      themeConfig: {
        nav: [
          { text: 'Home', link: '/en/' },
          { text: 'Guide', link: '/en/guide/' },
          { text: 'Download', link: 'https://github.com/CGG888/SrcBox/releases' },
          { text: 'Issues', link: 'https://github.com/CGG888/SrcBox/issues' }
        ],
        sidebar: [
          {
            text: 'Guide',
            items: [
              { text: 'Introduction', link: '/en/guide/' },
              { text: 'Features', link: '/en/guide/features' },
              { text: 'Architecture', link: '/en/guide/architecture' },
              { text: 'Development', link: '/en/guide/development' },
              { text: 'Configuration', link: '/en/guide/configuration' },
              { text: 'Internationalization', link: '/en/i18n' },
              { text: 'Roadmap', link: '/en/guide/roadmap' },
              { text: 'FAQ', link: '/en/guide/faq' }
            ]
          }
        ],
        editLink: {
          pattern: 'https://github.com/CGG888/SrcBox/edit/main/docs/:path',
          text: 'Edit this page on GitHub'
        }
      }
    }
  }
})
