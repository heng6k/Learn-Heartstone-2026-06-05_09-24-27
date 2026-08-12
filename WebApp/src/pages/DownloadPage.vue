<script setup>
import { RouterLink } from 'vue-router'
import AppIcon from '../components/AppIcon.vue'
import { currentVersion, windowsRelease } from '../data/site-content.js'
</script>

<template>
  <div class="page download-page">
    <header class="page-hero shell">
      <div>
        <span class="section-kicker">WINDOWS CLIENT</span>
        <h1>Windows 下载</h1>
        <p>下载桌面客户端，在浏览器之外继续练习酒馆战棋。</p>
      </div>
      <AppIcon name="download" :size="56" />
    </header>

    <section class="shell download-panel" aria-labelledby="download-status-title">
      <div class="download-status-icon ready" aria-hidden="true">
        <AppIcon name="check" :size="34" />
      </div>
      <div class="download-copy">
        <span class="status-pill status-preview">{{ currentVersion.label }}</span>
        <h2 id="download-status-title">{{ windowsRelease.status }}</h2>
        <p>{{ windowsRelease.reason }}</p>
        <a
          class="button button-primary"
          :href="windowsRelease.downloadUrl"
          download
          aria-describedby="download-help"
        >
          <AppIcon name="download" :size="20" />
          下载 Windows 版
        </a>
        <p id="download-help" class="helper-text">压缩包约 180 MB，下载后解压即可运行。</p>
      </div>
    </section>

    <section class="closing-cta shell">
      <div>
        <span class="section-kicker">PLAY NOW</span>
        <h2>不等安装包，也可以先用浏览器训练。</h2>
        <p>浏览器版无需安装，打开即可开始训练。</p>
      </div>
      <RouterLink class="button button-primary" to="/play">
        <AppIcon name="play" :size="20" />
        前往试玩
      </RouterLink>
    </section>
  </div>
</template>
