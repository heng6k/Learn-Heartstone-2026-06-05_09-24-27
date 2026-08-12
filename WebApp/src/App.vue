<script setup>
import { nextTick, ref, watch } from 'vue'
import { RouterLink, RouterView, useRoute } from 'vue-router'
import AppIcon from './components/AppIcon.vue'

const route = useRoute()
const menuOpen = ref(false)
const navItems = [
  { to: '/', label: '首页', icon: 'home' },
  { to: '/guides', label: '一图流', icon: 'guides' },
  { to: '/versions', label: '版本中心', icon: 'versions' },
  { to: '/play', label: '开始试玩', icon: 'play' },
  { to: '/download', label: 'Windows 下载', icon: 'download' },
]

watch(
  () => route.fullPath,
  async () => {
    menuOpen.value = false
    await nextTick()
    document.getElementById('main-content')?.focus({ preventScroll: true })
  },
)
</script>

<template>
  <a class="skip-link" href="#main-content">跳到主要内容</a>

  <header class="site-header">
    <div class="shell header-inner">
      <RouterLink class="brand" to="/" aria-label="Learn Heartstone 首页">
        <span class="brand-mark" aria-hidden="true">LH</span>
        <span>
          <strong>Learn Heartstone</strong>
          <small>酒馆战棋规则训练器</small>
        </span>
      </RouterLink>

      <nav class="desktop-nav" aria-label="主导航">
        <RouterLink v-for="item in navItems" :key="item.to" :to="item.to">
          <AppIcon :name="item.icon" />
          {{ item.label }}
        </RouterLink>
      </nav>

      <button
        class="menu-button"
        type="button"
        :aria-expanded="menuOpen"
        aria-controls="mobile-navigation"
        :aria-label="menuOpen ? '关闭导航' : '打开导航'"
        @click="menuOpen = !menuOpen"
      >
        <AppIcon :name="menuOpen ? 'close' : 'menu'" :size="24" />
      </button>
    </div>

    <nav v-if="menuOpen" id="mobile-navigation" class="mobile-nav" aria-label="移动端主导航">
      <RouterLink v-for="item in navItems" :key="item.to" :to="item.to">
        <AppIcon :name="item.icon" :size="22" />
        {{ item.label }}
      </RouterLink>
    </nav>
  </header>

  <main id="main-content" tabindex="-1">
    <RouterView v-slot="{ Component }">
      <Transition name="page" mode="out-in">
        <component :is="Component" />
      </Transition>
    </RouterView>
  </main>

  <footer class="site-footer">
    <div class="shell footer-inner">
      <div>
        <strong>Learn Heartstone</strong>
        <p>规则训练与版本差异验证工具，不代表暴雪或旅法师营地。</p>
      </div>
    </div>
  </footer>
</template>
