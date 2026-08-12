import { createRouter, createWebHistory } from 'vue-router'

const routeTitles = {
  home: '酒馆战棋训练器',
  versions: '版本中心',
  guides: '一图流训练',
  'guide-detail': '一图流详情',
  play: '开始试玩',
  download: 'Windows 下载',
}

const router = createRouter({
  history: createWebHistory(),
  scrollBehavior: () => ({ top: 0 }),
  routes: [
    { path: '/', name: 'home', component: () => import('./pages/HomePage.vue') },
    { path: '/versions', name: 'versions', component: () => import('./pages/VersionsPage.vue') },
    { path: '/guides', name: 'guides', component: () => import('./pages/GuidesPage.vue') },
    { path: '/guides/:guideId', name: 'guide-detail', component: () => import('./pages/GuidesPage.vue') },
    { path: '/play', name: 'play', component: () => import('./pages/PlayPage.vue') },
    { path: '/download', name: 'download', component: () => import('./pages/DownloadPage.vue') },
    { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('./pages/NotFoundPage.vue') },
  ],
})

router.afterEach((to) => {
  document.title = `${to.meta.title ?? routeTitles[to.name] ?? '页面未找到'} · Learn Heartstone`
})

export default router
