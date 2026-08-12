<script setup>
import { RouterLink } from 'vue-router'
</script>

<template>
  <section class="page shell not-found" aria-labelledby="not-found-title">
    <span class="section-kicker">404</span>
    <h1 id="not-found-title">这里没有可用页面</h1>
    <p>攻略和一图流路由不在当前精简版本中。</p>
    <RouterLink class="button button-primary" to="/">返回首页</RouterLink>
  </section>
</template>
