<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AppIcon from '../components/AppIcon.vue'
import GuideAssetCard from '../components/GuideAssetCard.vue'
import { loadGuideCatalog } from '../data/guide-catalog.js'

const route = useRoute()
const router = useRouter()
const catalog = ref(null)
const error = ref('')
const search = ref('')
const tribe = ref('全部')
const profileId = ref('')
const copyState = ref('')

const guides = computed(() => catalog.value?.guides ?? [])
const tribes = computed(() => ['全部', ...new Set(guides.value.map((guide) => guide.primaryTribe))])
const selectedGuide = computed(() => guides.value.find((guide) => guide.guideId === route.params.guideId))
const activeProfile = computed(() => {
  const guide = selectedGuide.value
  return guide?.profiles.find((profile) => profile.profileId === profileId.value) ?? guide?.profiles[0]
})
const filteredGuides = computed(() => {
  const value = search.value.trim().toLowerCase()
  return guides.value.filter((guide) => {
    const matchesTribe = tribe.value === '全部' || guide.primaryTribe === tribe.value
    const matchesSearch = !value || [guide.title, guide.summary, guide.primaryTribe, guide.guideId]
      .some((field) => String(field).toLowerCase().includes(value))
    return matchesTribe && matchesSearch
  })
})
const recommendedTrinkets = computed(() => selectedGuide.value
  ? [...selectedGuide.value.recommendedLesserTrinkets, ...selectedGuide.value.recommendedGreaterTrinkets]
  : [])

onMounted(async () => {
  try {
    catalog.value = await loadGuideCatalog()
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '一图流加载失败'
  }
})

watch(selectedGuide, (guide) => {
  if (!guide) return
  const requested = String(route.query.profile ?? '')
  profileId.value = guide.profiles.some((profile) => profile.profileId === requested)
    ? requested
    : guide.defaultProfileId
}, { immediate: true })

function openGuide(guide) {
  router.push({ name: 'guide-detail', params: { guideId: guide.guideId }, query: { profile: guide.defaultProfileId } })
}

function selectProfile(id) {
  profileId.value = id
  router.replace({ query: { ...route.query, profile: id } })
}

function submitSearch() {
  if (filteredGuides.value.length === 1) openGuide(filteredGuides.value[0])
}

async function copyGuideLink() {
  try {
    await navigator.clipboard.writeText(window.location.href)
    copyState.value = '链接已复制'
  } catch (_) {
    copyState.value = '请复制浏览器地址'
  }
  window.setTimeout(() => { copyState.value = '' }, 2200)
}
</script>

<template>
  <div class="page guides-page">
    <template v-if="!route.params.guideId">
      <header class="guide-index-hero shell">
        <div>
          <span class="section-kicker">MOBILE ONE-SHEET</span>
          <h1>一图流阵容训练</h1>
          <p>先看清核心牌、站位和回合操作。这个页面不会加载 Unity。</p>
        </div>
        <span class="guide-light-badge"><AppIcon name="check" :size="18" /> 轻量浏览</span>
      </header>

      <main class="guide-index shell" aria-live="polite">
        <form class="guide-search" role="search" @submit.prevent="submitSearch">
          <label for="guide-search-input">搜索阵容、种族或阵容 ID</label>
          <div>
            <input id="guide-search-input" v-model="search" type="search" placeholder="例如：野兽、龙虾、GUIDE-S14…" />
            <button class="button button-secondary" type="submit">打开唯一结果</button>
          </div>
        </form>

        <div class="guide-tribe-tabs" role="group" aria-label="按种族筛选">
          <button
            v-for="item in tribes"
            :key="item"
            type="button"
            :aria-pressed="tribe === item"
            @click="tribe = item"
          >{{ item }}</button>
        </div>

        <div v-if="error" class="guide-message guide-message-error" role="alert">{{ error }}</div>
        <div v-else-if="!catalog" class="guide-message" role="status">正在读取一图流…</div>
        <div v-else-if="!filteredGuides.length" class="guide-message">没有匹配的阵容，换一个关键词试试。</div>

        <section v-else class="guide-grid" aria-label="一图流阵容列表">
          <button
            v-for="guide in filteredGuides"
            :key="guide.guideId"
            class="guide-cover"
            type="button"
            @click="openGuide(guide)"
          >
            <div class="guide-cover-cards" aria-hidden="true">
              <img v-for="card in guide.coverCards.slice(0, 3)" :key="card.stableId + card.name" :src="card.image" width="176" height="232" loading="lazy" alt="" />
            </div>
            <div class="guide-cover-copy">
              <span>{{ guide.primaryTribe }}</span>
              <h2>{{ guide.title }}</h2>
              <p>{{ guide.summary }}</p>
              <strong>查看 3 个训练档位 <span aria-hidden="true">→</span></strong>
            </div>
          </button>
        </section>
      </main>
    </template>

    <template v-else-if="!catalog && !error">
      <div class="guide-message shell" role="status">正在读取一图流…</div>
    </template>

    <template v-else-if="error || !selectedGuide">
      <div class="guide-message guide-message-error shell" role="alert">
        {{ error || '没有找到这套一图流。' }}
        <RouterLink class="button button-secondary" to="/guides">返回阵容列表</RouterLink>
      </div>
    </template>

    <template v-else>
      <header class="guide-detail-hero shell">
        <RouterLink class="guide-back" to="/guides">← 全部阵容</RouterLink>
        <div class="guide-detail-heading">
          <div>
            <span class="section-kicker">{{ selectedGuide.primaryTribe }} · {{ selectedGuide.gameVersionId }}</span>
            <h1>{{ selectedGuide.title }}</h1>
            <p>{{ selectedGuide.summary }}</p>
          </div>
          <GuideAssetCard :item="selectedGuide.hero" compact />
        </div>
      </header>

      <div class="guide-detail shell">
        <section class="guide-profile-panel" aria-labelledby="guide-profile-title">
          <div class="guide-section-heading">
            <div>
              <span class="section-kicker">训练档位</span>
              <h2 id="guide-profile-title">先选学习强度</h2>
            </div>
            <span>第 {{ activeProfile.startRound }} 回合 · {{ activeProfile.tavernTier }} 本 · {{ activeProfile.gold }} 金币</span>
          </div>
          <div class="guide-profile-tabs" role="tablist" aria-label="训练档位">
            <button
              v-for="profile in selectedGuide.profiles"
              :key="profile.profileId"
              type="button"
              role="tab"
              :aria-selected="profile.profileId === activeProfile.profileId"
              @click="selectProfile(profile.profileId)"
            >
              <strong>{{ profile.title }}</strong>
              <span>{{ profile.learningGoal }}</span>
            </button>
          </div>
        </section>

        <section class="guide-content-section" aria-labelledby="core-cards-title">
          <div class="guide-section-heading">
            <div><span class="section-kicker">核心路线</span><h2 id="core-cards-title">先认清这些牌</h2></div>
            <span>左右滑动查看完整卡牌</span>
          </div>
          <div class="guide-card-track">
            <GuideAssetCard v-for="card in selectedGuide.coreCards" :key="card.stableId + card.name" :item="card" />
          </div>
        </section>

        <section v-if="recommendedTrinkets.length" class="guide-content-section" aria-labelledby="trinkets-title">
          <div class="guide-section-heading">
            <div><span class="section-kicker">推荐饰品</span><h2 id="trinkets-title">优先选择其一</h2></div>
          </div>
          <div class="guide-card-track">
            <GuideAssetCard v-for="card in recommendedTrinkets" :key="card.stableId" :item="card" />
          </div>
        </section>

        <section v-if="activeProfile.shapingSpells.length" class="guide-content-section guide-shaping" aria-labelledby="shaping-title">
          <div class="guide-section-heading">
            <div><span class="section-kicker">教学专用</span><h2 id="shaping-title">塑造法术</h2></div>
            <span>未使用的法术会在回合结束时清除</span>
          </div>
          <div class="guide-card-track">
            <GuideAssetCard v-for="card in activeProfile.shapingSpells" :key="card.stableId" :item="card" compact />
          </div>
        </section>

        <section class="guide-content-section guide-steps" aria-labelledby="steps-title">
          <div class="guide-section-heading">
            <div><span class="section-kicker">操作顺序</span><h2 id="steps-title">照着这条回合线行动</h2></div>
            <span>{{ activeProfile.steps.length }} 步</span>
          </div>
          <ol v-if="activeProfile.steps.length">
            <li v-for="step in activeProfile.steps" :key="step.actionId">
              <span>{{ step.order }}</span>
              <div><strong>{{ step.instruction }}</strong><small v-if="step.count > 1">执行 {{ step.count }} 次</small></div>
            </li>
          </ol>
          <p v-else class="guide-empty-steps">该档位沿用当前阵容目标，没有额外的分步操作。</p>
          <div v-if="activeProfile.keyDecisions.length" class="guide-decisions">
            <strong>关键判断</strong>
            <ul><li v-for="decision in activeProfile.keyDecisions" :key="decision">{{ decision }}</li></ul>
          </div>
        </section>

        <section class="guide-content-section" aria-labelledby="opening-title">
          <div class="guide-section-heading">
            <div><span class="section-kicker">开局位置</span><h2 id="opening-title">酒馆、场上与手牌</h2></div>
          </div>
          <div class="guide-zones">
            <div v-for="zone in [
              { title: '酒馆', cards: activeProfile.startingShop },
              { title: '场上', cards: activeProfile.startingBoard },
              { title: '手牌', cards: activeProfile.startingHand },
            ]" :key="zone.title" class="guide-zone">
              <h3>{{ zone.title }} <span>{{ zone.cards.length }}</span></h3>
              <div v-if="zone.cards.length" class="guide-card-track guide-card-track-compact">
                <GuideAssetCard v-for="card in zone.cards" :key="card.placementId" :item="card" compact />
              </div>
              <p v-else>本区域没有预置卡牌。</p>
            </div>
          </div>
        </section>

        <section class="guide-content-section" aria-labelledby="finish-title">
          <div class="guide-section-heading">
            <div><span class="section-kicker">最终站位</span><h2 id="finish-title">目标阵容</h2></div>
          </div>
          <div class="guide-card-track">
            <GuideAssetCard v-for="(card, index) in selectedGuide.finalComposition" :key="`${card.stableId}-${index}`" :item="card" />
          </div>
          <p class="guide-completion">完成条件：{{ activeProfile.completionCondition }}</p>
        </section>
      </div>

      <div class="guide-bottom-bar">
        <div class="shell">
          <button class="button button-secondary" type="button" @click="copyGuideLink">{{ copyState || '复制本页链接' }}</button>
          <RouterLink class="button button-primary" :to="{ name: 'play', query: { guide: selectedGuide.guideId, profile: activeProfile.profileId } }">
            <AppIcon name="play" :size="20" />进入操作训练
          </RouterLink>
        </div>
      </div>
    </template>
  </div>
</template>
