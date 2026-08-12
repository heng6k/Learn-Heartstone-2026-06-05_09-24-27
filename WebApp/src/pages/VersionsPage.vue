<script setup>
import AppIcon from '../components/AppIcon.vue'
import {
  communityNews,
  currentVersion,
  knownDifferences,
  mechanics,
  unsupportedEffects,
} from '../data/site-content.js'
</script>

<template>
  <div class="page versions-page">
    <header class="page-hero shell">
      <div>
        <span class="section-kicker">VERSION CENTER</span>
        <h1>版本中心</h1>
        <p>把官方版本、训练器实现状态与社区资讯分开显示，避免“能看到”被误解为“已完整支持”。</p>
      </div>
      <div class="version-seal" aria-label="当前版本 36.2，已上线，限定训练范围">
        <small>CURRENT</small>
        <strong>36.2</strong>
        <span>PREVIEW</span>
      </div>
    </header>

    <section class="shell status-overview" aria-labelledby="status-title">
      <h2 id="status-title" class="sr-only">当前版本状态</h2>
      <article class="status-card status-card-primary">
        <span>官方补丁</span>
        <strong>{{ currentVersion.officialStatus }}</strong>
        <small>版本日期 2026-08-05</small>
      </article>
      <article class="status-card">
        <span>训练器实现</span>
        <strong>{{ currentVersion.supportLabel }}</strong>
        <small>{{ currentVersion.trainerStatus }}</small>
      </article>
      <article class="status-card">
        <span>内容更新时间</span>
        <strong>{{ currentVersion.updatedAt }}</strong>
        <small>snapshot {{ currentVersion.contentSnapshotId }}</small>
      </article>
    </section>

    <section class="section shell two-column-section" aria-labelledby="support-title">
      <div>
        <div class="section-heading compact-heading">
          <span class="section-kicker">SUPPORTED</span>
          <h2 id="support-title">当前支持范围</h2>
          <p>36.2 的机制入口严格限制为以下两项。</p>
        </div>
        <div class="support-list">
          <article v-for="mechanic in mechanics" :key="mechanic.id" class="support-item">
            <AppIcon name="check" :size="22" />
            <div>
              <h3>{{ mechanic.name }}</h3>
              <p>{{ mechanic.description }}</p>
            </div>
            <span>{{ mechanic.status }}</span>
          </article>
        </div>
      </div>

      <aside class="snapshot-card" aria-label="内容快照信息">
        <span class="card-kicker">CONTENT IDENTITY</span>
        <dl>
          <div>
            <dt>GameVersionId</dt>
            <dd>{{ currentVersion.id }}</dd>
          </div>
          <div>
            <dt>RulesetId</dt>
            <dd>{{ currentVersion.rulesetId }}</dd>
          </div>
          <div>
            <dt>ContentSnapshotId</dt>
            <dd>{{ currentVersion.contentSnapshotId }}</dd>
          </div>
        </dl>
      </aside>
    </section>

    <section class="section shell differences-grid" aria-label="已知差异与未支持效果">
      <article class="difference-panel">
        <div class="panel-title">
          <AppIcon name="clock" :size="22" />
          <div>
            <span class="section-kicker">KNOWN DIFFERENCES</span>
            <h2>已知差异</h2>
          </div>
        </div>
        <ul class="plain-list">
          <li v-for="item in knownDifferences" :key="item">{{ item }}</li>
        </ul>
      </article>

      <article class="difference-panel difference-panel-warning">
        <div class="panel-title">
          <AppIcon name="alert" :size="22" />
          <div>
            <span class="section-kicker">NOT FROZEN</span>
            <h2>未支持或未冻结</h2>
          </div>
        </div>
        <ul class="plain-list">
          <li v-for="item in unsupportedEffects" :key="item">{{ item }}</li>
        </ul>
      </article>
    </section>

    <section class="section shell" aria-labelledby="news-title">
      <div class="section-heading news-heading">
        <div>
          <span class="section-kicker">COMMUNITY READING</span>
          <h2 id="news-title">旅法师营地资讯</h2>
          <p>以下内容来自社区站点，用于补充版本阅读，不作为本项目的官方事实冻结依据。</p>
        </div>
        <span class="external-source-label">外部链接</span>
      </div>
      <div class="news-list">
        <a
          v-for="article in communityNews"
          :key="article.href"
          class="news-card"
          :href="article.href"
          target="_blank"
          rel="noopener noreferrer nofollow"
        >
          <div>
            <span>{{ article.source }} · {{ article.date }}</span>
            <h3>{{ article.title }}</h3>
            <p>{{ article.summary }}</p>
          </div>
          <AppIcon name="external" :size="22" />
          <span class="sr-only">在新标签页打开</span>
        </a>
      </div>
    </section>
  </div>
</template>
