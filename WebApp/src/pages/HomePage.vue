<script setup>
import { RouterLink } from 'vue-router'
import AppIcon from '../components/AppIcon.vue'
import { currentVersion, mechanics, productCapabilities } from '../data/site-content.js'
</script>

<template>
  <div class="page home-page">
    <section class="hero shell" aria-labelledby="home-title">
      <div class="hero-copy reveal">
        <div class="eyebrow-row">
          <span class="status-pill status-preview">{{ currentVersion.label }}</span>
          <span>{{ currentVersion.season }}</span>
        </div>
        <h1 id="home-title">把版本差异，带进一局真正能复现的酒馆训练。</h1>
        <p class="hero-lead">
          先看清当前支持范围，再进入 Unity 训练场。静态页面不会在后台下载百兆游戏资源。
        </p>
        <div class="action-row">
          <RouterLink class="button button-primary" to="/guides">
            <AppIcon name="guides" :size="21" />
            查看一图流
          </RouterLink>
          <RouterLink class="button button-secondary" to="/play">
            <AppIcon name="play" :size="20" />
            进入酒馆训练
          </RouterLink>
        </div>
        <p class="hero-note">手机查看一图流不会加载 Unity；需要操作时再进入训练场。</p>
      </div>

      <figure class="hero-preview reveal" aria-label="四步开局界面预览">
        <div class="preview-topbar">
          <span></span><span></span><span></span>
          <small>真实 WebGL 画面</small>
        </div>
        <img
          src="/images/four-step-setup.png"
          width="1280"
          height="720"
          alt="Learn Heartstone 四步开局界面，依次选择游戏版本、英雄与种族、版本机制和高级卡池"
        />
        <figcaption>
          <span>四步开局</span>
          <span>版本锁定 · 可重复验证</span>
        </figcaption>
      </figure>
    </section>

    <section class="version-rail-section shell" aria-labelledby="current-version-title">
      <div class="section-heading compact-heading">
        <span class="section-kicker">CURRENT SNAPSHOT</span>
        <h2 id="current-version-title">当前版本牌轨</h2>
      </div>
      <div class="version-rail">
        <div class="rail-stop rail-version">
          <small>规则版本</small>
          <strong>{{ currentVersion.label }}</strong>
          <span>{{ currentVersion.trainerStatus }}</span>
        </div>
        <div v-for="mechanic in mechanics" :key="mechanic.id" class="rail-stop">
          <small>已启用机制</small>
          <strong>{{ mechanic.name }}</strong>
          <span>{{ mechanic.status }}</span>
        </div>
        <RouterLink class="rail-action" to="/guides" aria-label="查看当前版本一图流">
          <AppIcon name="guides" :size="24" />
          <span>查看一图流</span>
        </RouterLink>
      </div>
      <p class="rail-summary">{{ currentVersion.summary }}</p>
    </section>

    <section class="section shell" aria-labelledby="mechanics-title">
      <div class="section-heading">
        <span class="section-kicker">SEASON 14</span>
        <h2 id="mechanics-title">这套版本只保留两个机制</h2>
        <p>黑暗之赐与饰品构成 36.2 训练范围；历史机制只在对应旧版本中选择。</p>
      </div>
      <div class="mechanic-grid">
        <article v-for="(mechanic, index) in mechanics" :key="mechanic.id" class="mechanic-card">
          <div class="mechanic-number" aria-hidden="true">0{{ index + 1 }}</div>
          <div>
            <span class="card-kicker">{{ mechanic.kicker }}</span>
            <h3>{{ mechanic.name }}</h3>
            <p>{{ mechanic.description }}</p>
          </div>
          <span class="status-pill status-ready">
            <AppIcon name="check" :size="16" />
            {{ mechanic.status }}
          </span>
        </article>
      </div>
    </section>

    <section class="section shell" aria-labelledby="capabilities-title">
      <div class="section-heading">
        <span class="section-kicker">TRAINING LOOP</span>
        <h2 id="capabilities-title">为验证规则而设计</h2>
      </div>
      <div class="capability-grid">
        <article v-for="(item, index) in productCapabilities" :key="item.title" class="capability-card">
          <span class="capability-index">{{ String(index + 1).padStart(2, '0') }}</span>
          <h3>{{ item.title }}</h3>
          <p>{{ item.detail }}</p>
        </article>
      </div>
    </section>

    <section class="closing-cta shell">
      <div>
        <span class="section-kicker">READY WHEN YOU ARE</span>
        <h2>手机先看路线，需要时再操作。</h2>
        <p>一图流页面只加载当前阵容卡图；进入试玩页并确认后才会加载 Unity。</p>
      </div>
      <RouterLink class="button button-primary" to="/guides">
        打开一图流
        <AppIcon name="guides" :size="20" />
      </RouterLink>
    </section>
  </div>
</template>
