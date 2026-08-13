const api = require('../../services/guide-api')
const localState = require('../../services/local-state')

const actionLabels = {
  Buy: '购买',
  Sell: '出售',
  Play: '打出',
  PlayFinalCards: '完成阵容',
  BoardOrder: '调整站位',
  Reroll: '刷新酒馆',
  Upgrade: '升级酒馆',
  Freeze: '冻结',
  CastSpell: '施放法术',
  UseShapingSpell: '使用塑造法术',
  ChooseDiscover: '发现选择',
  ChooseTrinket: '选择饰品'
}

Page({
  data: {
    loading: true,
    error: '',
    guide: null,
    profile: null,
    completedCount: 0,
    totalSteps: 0,
    progressPercent: 0,
    isFavorite: false,
    currentInstruction: '从第一步开始'
  },

  onLoad(options) {
    let guideId = options && options.guideId
    let profileId = options && options.profileId
    if (!guideId) {
      const reference = api.resolveReference(options && (options.code || options.scene || options.guide))
      guideId = reference && reference.guideId
      profileId = reference && reference.profileId
    }
    if (!guideId) {
      this.setData({ loading: false, error: '一图流编号无效，请返回重新选择。' })
      return
    }
    this.guideId = guideId
    this.profileId = profileId || ''
    this.loadGuide()
  },

  loadGuide() {
    this.setData({ loading: true, error: '' })
    api.getGuide(this.guideId, this.profileId)
      .then(result => {
        this.guideSource = result.guide || result
        this.profileId = (result.profile && result.profile.profileId) || this.profileId || this.guideSource.defaultProfileId
        this.refreshView()
        api.trackIntent('miniapp_guide_open', { guideId: this.guideId, profileId: this.profileId })
      })
      .catch(error => this.setData({ loading: false, error: error.message || '一图流加载失败' }))
  },

  refreshView() {
    const guide = this.guideSource
    const selected = guide.profiles.find(item => item.profileId === this.profileId) || guide.profiles[0]
    this.profileId = selected.profileId
    const progress = localState.guideProgress(guide.guideId, selected.profileId)
    let foundCurrent = false
    const steps = selected.steps.map(step => {
      const done = Boolean(progress[step.actionId])
      const current = !done && !foundCurrent
      if (current) foundCurrent = true
      return Object.assign({}, step, {
        done,
        current,
        actionLabel: actionLabels[step.kind] || step.kind
      })
    })
    const completedCount = steps.filter(step => step.done).length
    const current = steps.find(step => step.current)
    const profile = Object.assign({}, selected, { steps })
    const tabs = guide.profiles.map(item => Object.assign({}, item, {
      selected: item.profileId === selected.profileId
    }))
    const viewGuide = Object.assign({}, guide, { profiles: tabs })

    localState.rememberGuide(guide.guideId, selected.profileId)
    this.setData({
      loading: false,
      guide: viewGuide,
      profile,
      completedCount,
      totalSteps: steps.length,
      progressPercent: steps.length ? Math.round(completedCount * 100 / steps.length) : 100,
      isFavorite: localState.isGuideFavorite(guide.guideId),
      currentInstruction: current ? current.instruction : '本档步骤已全部完成'
    })
  },

  selectProfile(event) {
    this.profileId = event.currentTarget.dataset.profileId
    this.refreshView()
    api.trackIntent('miniapp_guide_profile_change', { guideId: this.guideId, profileId: this.profileId })
  },

  toggleStep(event) {
    localState.toggleGuideStep(this.guideId, this.profileId, event.currentTarget.dataset.actionId)
    this.refreshView()
  },

  completeCurrentStep() {
    const current = this.data.profile && this.data.profile.steps.find(step => !step.done)
    if (!current) {
      wx.showToast({ title: '本档训练已完成', icon: 'success' })
      return
    }
    localState.toggleGuideStep(this.guideId, this.profileId, current.actionId)
    api.trackIntent('miniapp_guide_step_complete', {
      guideId: this.guideId,
      profileId: this.profileId,
      actionId: current.actionId
    })
    this.refreshView()
  },

  resetProgress() {
    wx.showModal({
      title: '重新开始本档？',
      content: '只会清除本机的步骤勾选，不会修改攻略内容。',
      confirmText: '重新开始',
      success: result => {
        if (!result.confirm) return
        localState.resetGuideProgress(this.guideId, this.profileId)
        this.refreshView()
      }
    })
  },

  toggleFavorite() {
    this.setData({ isFavorite: localState.toggleGuideFavorite(this.guideId) })
  },

  copyIdentity() {
    wx.setClipboardData({
      data: this.guideId + ':' + this.profileId,
      success: () => wx.showToast({ title: '一图流编号已复制', icon: 'success' })
    })
  },

  onShareAppMessage() {
    return {
      title: this.data.guide ? this.data.guide.title + ' · ' + this.data.profile.title : '一图流训练',
      path: '/pages/scenario/scenario?guideId=' + encodeURIComponent(this.guideId) + '&profileId=' + encodeURIComponent(this.profileId)
    }
  }
})
