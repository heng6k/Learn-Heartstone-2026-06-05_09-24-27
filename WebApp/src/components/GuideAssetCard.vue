<script setup>
defineProps({
  item: { type: Object, required: true },
  compact: { type: Boolean, default: false },
})
</script>

<template>
  <article class="guide-asset" :class="{ 'guide-asset-compact': compact }">
    <div class="guide-asset-art">
      <img
        v-if="item.image"
        :src="item.image"
        width="176"
        height="232"
        loading="lazy"
        decoding="async"
        :alt="`${item.name}卡牌`"
      />
      <span v-else class="guide-tutorial-glyph" aria-hidden="true">{{ item.tutorialGlyph || '术' }}</span>
      <span v-if="item.tavernTier" class="guide-tier">{{ item.tavernTier }}</span>
      <span v-if="item.badge" class="guide-card-badge">{{ item.badge }}</span>
    </div>
    <div class="guide-asset-copy">
      <strong>{{ item.name }}</strong>
      <span>
        {{ item.kind }}
        <template v-if="item.attack || item.health"> · {{ item.attack }}/{{ item.health }}</template>
        <template v-else-if="item.cost"> · {{ item.cost }} 费</template>
      </span>
    </div>
  </article>
</template>

