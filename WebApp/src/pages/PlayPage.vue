<script setup>
import { computed, onBeforeUnmount, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import AppIcon from '../components/AppIcon.vue'
import { currentVersion, unityRelease } from '../data/site-content.js'

const state = ref('idle')
const route = useRoute()
const frameKey = ref(0)
let loadTimer
const unityUrl = import.meta.env.VITE_UNITY_URL || unityRelease.path
const requestedGuide = computed(() => String(route.query.guide ?? ''))

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
}

onBeforeUnmount(() => clearTimeout(loadTimer))
</script>

<template>
  <div class="page play-page">
    <header class="page-hero shell play-hero">
      <div>
        <span class="section-kicker">WEBGL TRAINING</span>
        <h1>开始试玩</h1>
        <p>游戏不会自动加载。确认版本、下载量与设备条件后，再进入 Unity 训练场。</p>
      </div>
      <span class="load-state" :data-state="state">
        <span aria-hidden="true"></span>
        {{ stateLabel }}
      </span>
    </header>

    <section v-if="state === 'idle' || state === 'failed'" class="shell play-preflight" aria-labelledby="preflight-title">
      <div class="preflight-main">
        <span class="card-kicker">BEFORE YOU ENTER</span>
        <h2 id="preflight-title">加载前确认</h2>
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

        <dl class="preflight-facts">
          <div>
            <dt>游戏版本</dt>
            <dd>{{ currentVersion.label }}</dd>
          </div>
          <div>
            <dt>内容快照</dt>
            <dd>{{ currentVersion.contentSnapshotId }}</dd>
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

        <button class="button button-primary start-game-button" type="button" @click="startUnity">
          <AppIcon name="play" :size="22" />
          {{ state === 'failed' ? '重新加载训练场' : '确认并加载 Unity' }}
        </button>
        <RouterLink class="button button-quiet play-back-to-guides" to="/guides">返回轻量一图流</RouterLink>
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
        <div>
          <strong>静态内容已经就绪</strong>
          <p>此刻网络面板中没有 Unity loader、WASM 或数据分块请求。</p>
        </div>
      </aside>
    </section>

    <section v-else class="unity-stage" aria-label="Unity WebGL 训练场">
      <div class="unity-toolbar shell">
        <div>
          <span>{{ currentVersion.label }}</span>
          <strong>{{ stateLabel }}</strong>
        </div>
        <div class="toolbar-actions">
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
