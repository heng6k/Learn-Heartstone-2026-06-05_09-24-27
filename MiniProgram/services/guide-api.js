const config = require('../config')
const localFixture = require('../fixtures/guides')

const legacyCodes = {
  '23456789ABCDEFGHJKMN': ['GUIDE-S14-BEAST-LOBSTER-RALLY', 'showcase'],
  '3456789ABCDEFGHJKLMN': ['GUIDE-S14-MECH-SPELL-SATELLITE', 'showcase'],
  '456789ABCDEFGHJKLMNP': ['GUIDE-S14-DEMON-TAVERN-CONSUME', 'showcase']
}

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

function listGuides() {
  if (config.apiBaseUrl) {
    return request('/api/guides').then(data => data.items || data)
  }
  return Promise.resolve(localFixture.guides)
}

function getGuide(guideId, profileId) {
  if (config.apiBaseUrl) {
    const suffix = profileId ? '?profile=' + encodeURIComponent(profileId) : ''
    return request('/api/guides/' + encodeURIComponent(guideId) + suffix)
  }
  const guide = localFixture.guides.find(item => item.guideId === guideId)
  if (!guide) return Promise.reject(new Error('本地一图流中没有这个阵容'))
  const requested = profileId || guide.defaultProfileId
  const profile = guide.profiles.find(item => item.profileId === requested) || guide.profiles[0]
  return Promise.resolve({ guide, profile })
}

function resolveReference(value) {
  let decoded = ''
  try {
    decoded = decodeURIComponent(String(value || '')).trim()
  } catch (_) {
    return null
  }

  const queryGuide = decoded.match(/(?:guideId|guide)=([^&#]+)/i)
  const queryProfile = decoded.match(/(?:profileId|profile)=([^&#]+)/i)
  if (queryGuide) {
    return {
      guideId: queryGuide[1],
      profileId: queryProfile ? queryProfile[1] : ''
    }
  }

  const compact = decoded.replace(/[\s-]/g, '').toUpperCase()
  if (legacyCodes[compact]) {
    return { guideId: legacyCodes[compact][0], profileId: legacyCodes[compact][1] }
  }

  const direct = decoded.match(/^(GUIDE-S14-[A-Z0-9-]+)(?::([a-z0-9_-]+))?$/i)
  return direct ? { guideId: direct[1].toUpperCase(), profileId: direct[2] || '' } : null
}

function trackIntent(eventName, payload) {
  if (!config.apiBaseUrl) return Promise.resolve()
  return request('/api/events', 'POST', {
    eventName,
    channel: config.releaseChannel,
    payload: payload || {},
    occurredAt: new Date().toISOString()
  }).catch(() => undefined)
}

module.exports = {
  getGuide,
  listGuides,
  resolveReference,
  trackIntent
}
