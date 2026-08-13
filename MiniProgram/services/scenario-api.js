const config = require('../config')
const localFixtures = require('../fixtures/scenarios')
const { normalizeShareCode } = require('../utils/share-code')

function request(path, method, data) {
  return new Promise((resolve, reject) => {
    wx.request({
      url: config.apiBaseUrl.replace(/\/$/, '') + path,
      method: method || 'GET',
      data: data || undefined,
      timeout: 8000,
      success(response) {
        if (response.statusCode >= 200 && response.statusCode < 300) {
          resolve(response.data)
          return
        }
        reject(new Error('服务返回状态 ' + response.statusCode))
      },
      fail(error) {
        reject(new Error(error.errMsg || '网络请求失败'))
      }
    })
  })
}

function summarize(item) {
  const contract = item.contract
  return {
    shareCode: contract.shareCode,
    sceneId: contract.sceneId,
    status: contract.status,
    title: contract.summary.title,
    summary: contract.summary.summary,
    archetype: contract.summary.archetype,
    difficulty: contract.summary.difficulty,
    difficultyTitle: contract.summary.difficultyTitle,
    gameVersionName: contract.summary.gameVersionName,
    hero: contract.summary.hero,
    activeTribes: contract.summary.activeTribes,
    compatibilityStatus: contract.compatibility.status,
    popularity: item.popularity,
    updatedAt: item.updatedAt
  }
}

function listScenarios(sort) {
  if (config.apiBaseUrl) {
    return request('/api/scenes?sort=' + encodeURIComponent(sort || 'latest')).then(data => data.items || data)
  }

  const items = localFixtures.items.map(summarize)
  items.sort(sort === 'popular'
    ? (left, right) => right.popularity - left.popularity
    : (left, right) => right.updatedAt.localeCompare(left.updatedAt))
  return Promise.resolve(items)
}

function getScenario(shareCode) {
  const normalized = normalizeShareCode(shareCode)
  if (config.apiBaseUrl) {
    return request('/api/scenes/' + normalized)
  }

  const item = localFixtures.byCode[normalized]
  return item
    ? Promise.resolve(item.contract)
    : Promise.reject(new Error('本地样例中没有这个分享码'))
}

function trackIntent(eventName, payload) {
  if (!config.apiBaseUrl) {
    return Promise.resolve()
  }
  return request('/api/events', 'POST', {
    eventName,
    payload: payload || {},
    occurredAt: new Date().toISOString()
  }).catch(() => undefined)
}

module.exports = {
  getScenario,
  listScenarios,
  trackIntent
}
