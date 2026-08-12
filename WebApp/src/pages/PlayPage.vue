<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import AppIcon from '../components/AppIcon.vue'
import { currentVersion, unityRelease } from '../data/site-content.js'

const state = ref('idle')
const route = useRoute()
const frameKey = ref(0)
let loadTimer
const unityUrl = import.meta.env.VITE_UNITY_URL || unityRelease.path
const requestedGuide = computed(() => String(route.query.guide ?? ''))
const isFullscreen = ref(false)
const isStandalone = ref(false)
const fullscreenAvailable = ref(false)
const fullscreenNotice = ref('')

const stateLabel = computed(() => ({
  idle: '等待确认',
  loading: '正在加载',
  ready: '训练场页面已连接',
  failed: '加载失败',
})[state.value])

function startUnity() {
  clearTimeout(loadTimer)
  frameKey.value += 1
  state.value = 'loading'
  loadTimer = setTimeout(() => {
    state.value = 'failed'
  }, 90000)
}

function fullscreenElement() {
  return document.fullscreenElement || document.webkitFullscreenElement || null
}

function syncFullscreenState() {
  isFullscreen.value = Boolean(fullscreenElement())
}

async function requestGameFullscreen() {
  const root = document.documentElement
  const request = root.requestFullscreen || root.webkitRequestFullscreen
  if (!request) {
    fullscreenNotice.value = isStandalone.value
      ? '当前已由主屏幕全屏运行。'
      : '当前浏览器不开放网页全屏。iPhone / iPad 可用“添加到主屏幕”后全屏打开。'
    return false
  }

  try {
    await request.call(root, { navigationUI: 'hide' })
    fullscreenNotice.value = '已进入全屏；按 Esc 或再次点击全屏按钮即可退出。'
    if (screen.orientation?.lock) {
      await screen.orientation.lock('landscape').catch(() => {})
    }
    return true
  } catch {
    fullscreenNotice.value = '浏览器没有允许全屏。可继续使用窗口模式，或从浏览器菜单添加到主屏幕。'
    return false
  }
}

async function exitGameFullscreen() {
  const exit = document.exitFullscreen || document.webkitExitFullscreen
  if (fullscreenElement() && exit) {
    await exit.call(document).catch(() => {})
  }
}

async function startFullscreen() {
  await requestGameFullscreen()
  startUnity()
}

async function toggleFullscreen() {
  if (isFullscreen.value) {
    await exitGameFullscreen()
    return
  }
  await requestGameFullscreen()
}

function handleLoaded() {
  clearTimeout(loadTimer)
  state.value = 'ready'
}

function handleFailed() {
  clearTimeout(loadTimer)
  state.value = 'failed'
}

function exitUnity() {
  clearTimeout(loadTimer)
  state.value = 'idle'
  if (isFullscreen.value) {
    void exitGameFullscreen()
  }
}

onMounted(() => {
  fullscreenAvailable.value = Boolean(document.documentElement.requestFullscreen || document.documentElement.webkitRequestFullscreen)
  isStandalone.value = window.matchMedia?.('(display-mode: fullscreen), (display-mode: standalone)').matches || window.navigator.standalone === true
  document.addEventListener('fullscreenchange', syncFullscreenState)
  document.addEventListener('webkitfullscreenchange', syncFullscreenState)
  syncFullscreenState()
})

onBeforeUnmount(() => {
  clearTimeout(loadTimer)
  document.removeEventListener('fullscreenchange', syncFullscreenState)
  document.removeEventListener('webkitfullscreenchange', syncFullscreenState)
})
</script>

<template>
  <div class="page play-page" :class="{ 'play-page--running': state === 'loading' || state === 'ready' }">
    <header class="page-hero shell play-hero">
      <div>
        <span class="section-kicker">TAVERN GATE · WEBGL</span>
        <h1>推开酒馆的门</h1>
        <p>先选择显示方式，再加载训练场。手机版与电脑浏览器都不会在你确认前下载 Unity。</p>
      </div>
      <span class="load-state" :data-state="state">
        <span aria-hidden="true"></span>
        {{ stateLabel }}
      </span>
    </header>

    <section v-if="state === 'idle' || state === 'failed'" class="shell play-preflight" aria-labelledby="preflight-title">
      <div class="preflight-main tavern-gate-panel">
        <span class="card-kicker">CHOOSE YOUR TABLE</span>
        <h2 id="preflight-title">选择进入方式</h2>
        <p>
          本次会加载 {{ unityRelease.chunkCount }} 个数据分块，合计 {{ unityRelease.sourceDataLabel }}。移动设备可能出现较长加载或内存压力。
        </p>
        <p v-if="requestedGuide" class="guide-play-context">已从一图流进入。Unity 加载完成后，请在训练场选择对应阵容与档位继续操作练习。</p>

        <div v-if="state === 'failed'" class="inline-error" role="alert">
          <AppIcon name="alert" :size="22" />
          <div>
            <strong>训练场没有在 90 秒内连接。</strong>
            <span>检查网络与内存后，可以主动重试；页面不会在后台循环请求。</span>
          </div>
        </div>

        <div class="entry-mode-grid" aria-label="游戏显示方式">
          <button class="entry-mode-card entry-mode-card--primary" type="button" @click="startFullscreen">
            <span class="entry-mode-icon" aria-hidden="true"><AppIcon name="fullscreen" :size="28" /></span>
            <span>
              <strong>全屏进入训练场</strong>
              <small>{{ fullscreenAvailable || isStandalone ? '手机沉浸显示 · 电脑网页全屏' : '不支持时自动使用窗口模式' }}</small>
            </span>
            <span class="entry-mode-action">进入</span>
          </button>

          <button class="entry-mode-card" type="button" @click="startUnity">
            <span class="entry-mode-icon" aria-hidden="true"><AppIcon name="window" :size="28" /></span>
            <span>
              <strong>窗口模式进入</strong>
              <small>保留浏览器导航，适合电脑多任务</small>
            </span>
            <span class="entry-mode-action">进入</span>
          </button>
        </div>

        <p v-if="fullscreenNotice" class="fullscreen-notice" role="status">{{ fullscreenNotice }}</p>

        <dl class="preflight-facts">
          <div>
            <dt>游戏版本</dt>
            <dd>{{ currentVersion.label }}</dd>
          </div>
          <div>
            <dt>资源规模</dt>
            <dd>{{ unityRelease.sourceDataLabel }}</dd>
          </div>
          <div>
            <dt>设备建议</dt>
            <dd>{{ unityRelease.recommendedMemory }}</dd>
          </div>
        </dl>

        <RouterLink class="button button-quiet play-back-to-guides" to="/guides">暂不加载，返回轻量一图流</RouterLink>
      </div>

      <aside class="preflight-aside">
        <img
          src="/images/lobby.png"
          width="1280"
          height="720"
          loading="lazy"
          decoding="async"
          alt="Learn Heartstone 大厅界面预览"
        />
      </aside>
    </section>

    <section v-else class="unity-stage" aria-label="Unity WebGL 训练场">
      <div class="unity-toolbar shell">
        <div>
          <span>{{ currentVersion.label }}</span>
          <strong>{{ stateLabel }}</strong>
        </div>
        <div class="toolbar-actions">
          <button class="button button-quiet fullscreen-button" type="button" @click="toggleFullscreen">
            <AppIcon :name="isFullscreen ? 'fullscreen-exit' : 'fullscreen'" :size="19" />
            {{ isFullscreen ? '退出全屏' : '进入全屏' }}
          </button>
          <button class="button button-quiet" type="button" @click="startUnity">重新加载</button>
          <button class="button button-secondary" type="button" @click="exitUnity">退出试玩</button>
        </div>
      </div>

      <div class="unity-frame-wrap">
        <div v-if="state === 'loading'" class="unity-loading" role="status" aria-live="polite">
          <span class="loading-ring" aria-hidden="true"></span>
          <strong>正在连接训练场</strong>
          <span>首次加载需要下载较大的 WebGL 资源。</span>
        </div>
        <iframe
          :key="frameKey"
          class="unity-frame"
          :src="unityUrl"
          title="Learn Heartstone Unity 训练场"
          allow="autoplay; fullscreen; gamepad"
          allowfullscreen
          @load="handleLoaded"
          @error="handleFailed"
        ></iframe>
      </div>
    </section>
  </div>
</template>
