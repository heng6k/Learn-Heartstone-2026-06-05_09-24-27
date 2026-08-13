const FAVORITES_KEY = 'learn-heartstone:favorites:v1'
const STEP_KEY_PREFIX = 'learn-heartstone:steps:v1:'
const GUIDE_FAVORITES_KEY = 'learn-heartstone:guide-favorites:v1'
const GUIDE_STEP_KEY_PREFIX = 'learn-heartstone:guide-steps:v1:'
const LAST_GUIDE_KEY = 'learn-heartstone:last-guide:v1'

function read(key, fallback) {
  const value = wx.getStorageSync(key)
  return value === '' || value === undefined || value === null ? fallback : value
}

function favorites() {
  const value = read(FAVORITES_KEY, [])
  return Array.isArray(value) ? value : []
}

function isFavorite(shareCode) {
  return favorites().indexOf(shareCode) >= 0
}

function toggleFavorite(shareCode) {
  const next = favorites()
  const index = next.indexOf(shareCode)
  if (index >= 0) {
    next.splice(index, 1)
  } else {
    next.push(shareCode)
  }
  wx.setStorageSync(FAVORITES_KEY, next)
  return next.indexOf(shareCode) >= 0
}

function stepProgress(shareCode) {
  const value = read(STEP_KEY_PREFIX + shareCode, {})
  return value && typeof value === 'object' && !Array.isArray(value) ? value : {}
}

function toggleStep(shareCode, actionId) {
  const next = stepProgress(shareCode)
  next[actionId] = !next[actionId]
  wx.setStorageSync(STEP_KEY_PREFIX + shareCode, next)
  return next
}

function guideIdentity(guideId, profileId) {
  return String(guideId || '') + ':' + String(profileId || '')
}

function guideFavorites() {
  const value = read(GUIDE_FAVORITES_KEY, [])
  return Array.isArray(value) ? value : []
}

function isGuideFavorite(guideId) {
  return guideFavorites().indexOf(guideId) >= 0
}

function toggleGuideFavorite(guideId) {
  const next = guideFavorites()
  const index = next.indexOf(guideId)
  if (index >= 0) next.splice(index, 1)
  else next.push(guideId)
  wx.setStorageSync(GUIDE_FAVORITES_KEY, next)
  return next.indexOf(guideId) >= 0
}

function guideProgress(guideId, profileId) {
  const value = read(GUIDE_STEP_KEY_PREFIX + guideIdentity(guideId, profileId), {})
  return value && typeof value === 'object' && !Array.isArray(value) ? value : {}
}

function toggleGuideStep(guideId, profileId, actionId) {
  const key = GUIDE_STEP_KEY_PREFIX + guideIdentity(guideId, profileId)
  const next = guideProgress(guideId, profileId)
  next[actionId] = !next[actionId]
  wx.setStorageSync(key, next)
  rememberGuide(guideId, profileId)
  return next
}

function resetGuideProgress(guideId, profileId) {
  wx.removeStorageSync(GUIDE_STEP_KEY_PREFIX + guideIdentity(guideId, profileId))
}

function rememberGuide(guideId, profileId) {
  wx.setStorageSync(LAST_GUIDE_KEY, { guideId, profileId })
}

function lastGuide() {
  const value = read(LAST_GUIDE_KEY, null)
  return value && value.guideId && value.profileId ? value : null
}

module.exports = {
  favorites,
  guideFavorites,
  guideProgress,
  isGuideFavorite,
  isFavorite,
  lastGuide,
  rememberGuide,
  resetGuideProgress,
  stepProgress,
  toggleGuideFavorite,
  toggleGuideStep,
  toggleFavorite,
  toggleStep
}
