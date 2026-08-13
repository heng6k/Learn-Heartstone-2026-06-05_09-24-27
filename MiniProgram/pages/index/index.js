const api = require('../../services/guide-api')
const localState = require('../../services/local-state')

Page({
  data: {
    loading: true,
    error: '',
    query: '',
    selectedTribe: '全部',
    tribes: ['全部'],
    favoritesOnly: false,
    showImport: false,
    importValue: '',
    importError: '',
    allGuides: [],
    guides: [],
    continued: null
  },

  onLoad(options) {
    this.loadGuides()
    if (options && (options.guide || options.guideId || options.scene)) {
      const query = options.guide || options.guideId || options.scene
      const profile = options.profile || options.profileId || ''
      this.openResolved({ guideId: query, profileId: profile })
    }
  },

  onShow() {
    if (this.data.allGuides.length) this.applyFilters()
  },

  loadGuides() {
    this.setData({ loading: true, error: '' })
    api.listGuides()
      .then(guides => {
        const tribes = ['全部'].concat(Array.from(new Set(guides.map(item => item.primaryTribe).filter(Boolean))))
        this.setData({ allGuides: guides, tribes, loading: false }, () => this.applyFilters())
      })
      .catch(error => this.setData({ loading: false, error: error.message || '一图流加载失败' }))
  },

  applyFilters() {
    const query = this.data.query.trim().toLowerCase()
    const selectedTribe = this.data.selectedTribe
    const last = localState.lastGuide()
    const guides = this.data.allGuides
      .filter(item => selectedTribe === '全部' || item.primaryTribe === selectedTribe)
      .filter(item => !this.data.favoritesOnly || localState.isGuideFavorite(item.guideId))
      .filter(item => !query || [
        item.title,
        item.summary,
        item.primaryTribe,
        item.guideId,
        item.coreCards.map(card => card.name).join(' ')
      ].join(' ').toLowerCase().indexOf(query) >= 0)
      .map(item => this.decorateGuide(item, last))

    let continued = null
    if (last) {
      const guide = this.data.allGuides.find(item => item.guideId === last.guideId)
      const profile = guide && guide.profiles.find(item => item.profileId === last.profileId)
      if (guide && profile) {
        const progress = localState.guideProgress(guide.guideId, profile.profileId)
        const completed = profile.steps.filter(step => progress[step.actionId]).length
        continued = {
          guideId: guide.guideId,
          profileId: profile.profileId,
          title: guide.title,
          profileTitle: profile.title,
          completed,
          total: profile.steps.length,
          hero: guide.hero
        }
      }
    }
    this.setData({ guides, continued })
  },

  decorateGuide(guide, last) {
    const profileId = last && last.guideId === guide.guideId ? last.profileId : guide.defaultProfileId
    const profile = guide.profiles.find(item => item.profileId === profileId) || guide.profiles[0]
    const progress = localState.guideProgress(guide.guideId, profile.profileId)
    const completed = profile.steps.filter(step => progress[step.actionId]).length
    return Object.assign({}, guide, {
      isFavorite: localState.isGuideFavorite(guide.guideId),
      resumeProfileId: profile.profileId,
      resumeProfileTitle: profile.title,
      completed,
      total: profile.steps.length
    })
  },

  onSearchInput(event) {
    this.setData({ query: event.detail.value }, () => this.applyFilters())
  },

  selectTribe(event) {
    this.setData({ selectedTribe: event.currentTarget.dataset.tribe }, () => this.applyFilters())
  },

  toggleFavoritesOnly() {
    this.setData({ favoritesOnly: !this.data.favoritesOnly }, () => this.applyFilters())
  },

  toggleFavorite(event) {
    localState.toggleGuideFavorite(event.currentTarget.dataset.guideId)
    api.trackIntent('miniapp_guide_favorite', { guideId: event.currentTarget.dataset.guideId })
    this.applyFilters()
  },

  openGuide(event) {
    this.openResolved({
      guideId: event.currentTarget.dataset.guideId,
      profileId: event.currentTarget.dataset.profileId || ''
    })
  },

  openResolved(reference) {
    if (!reference || !reference.guideId) {
      this.setData({ importError: '没有识别到可用的一图流编号' })
      return
    }
    const profile = reference.profileId ? '&profileId=' + encodeURIComponent(reference.profileId) : ''
    wx.navigateTo({
      url: '/pages/scenario/scenario?guideId=' + encodeURIComponent(reference.guideId) + profile
    })
  },

  toggleImport() {
    this.setData({ showImport: !this.data.showImport, importError: '' })
  },

  onImportInput(event) {
    this.setData({ importValue: event.detail.value, importError: '' })
  },

  submitImport() {
    const reference = api.resolveReference(this.data.importValue)
    if (!reference) {
      this.setData({ importError: '请输入阵容编号、旧分享码或分享链接' })
      return
    }
    api.trackIntent('miniapp_guide_reference_open', reference)
    this.openResolved(reference)
  },

  scanCode() {
    wx.scanCode({
      scanType: ['qrCode'],
      success: result => {
        const reference = api.resolveReference(result.result)
        if (reference) this.openResolved(reference)
        else this.setData({ importError: '二维码中没有可识别的一图流编号' })
      },
      fail: error => {
        if (String(error.errMsg || '').indexOf('cancel') < 0) {
          this.setData({ importError: '扫码失败，请改用编号打开' })
        }
      }
    })
  },

  onShareAppMessage() {
    return {
      title: '一图流训练：8 种酒馆阵容',
      path: '/pages/index/index'
    }
  }
})
