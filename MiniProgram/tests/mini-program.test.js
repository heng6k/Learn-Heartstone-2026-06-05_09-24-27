const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const test = require('node:test')

const root = path.resolve(__dirname, '..')
const shareCode = require('../utils/share-code')
const guideApi = require('../services/guide-api')
const guidesFixture = require('../fixtures/guides')

test('share-code parser keeps the frozen legacy normalization contract', () => {
  const expected = '23456789ABCDEFGHJKMN'
  assert.equal(shareCode.normalizeShareCode('2345 6789-abcd-efgh-jkmn'), expected)
  assert.equal(shareCode.extractShareCode('https://learn-hearthstone.example/scenes/' + expected), expected)
  assert.throws(() => shareCode.normalizeShareCode('23456789ABCDEFGHJKMO'))
  assert.equal(shareCode.extractShareCode('%E0%A4%A'), '')
})

test('one-sheet fixture exposes eight guides and every three-profile route', async () => {
  const guides = await guideApi.listGuides()
  assert.equal(guidesFixture.schemaVersion, 1)
  assert.equal(guides.length, 8)
  assert.equal(guides.reduce((total, guide) => total + guide.profiles.length, 0), 24)

  for (const guide of guides) {
    assert.equal(guide.profiles.length, 3, guide.guideId)
    assert.ok(guide.profiles.some(profile => profile.difficulty === 'Showcase'), guide.guideId)
    assert.ok(guide.profiles.some(profile => profile.difficulty === 'GuidedDiscover'), guide.guideId)
    assert.ok(guide.profiles.some(profile => profile.difficulty === 'OpenBuild'), guide.guideId)
    assert.equal(guide.finalComposition.length, 7, guide.guideId)
    assert.ok(guide.coreCards.length >= 4, guide.guideId)
    assert.ok(guide.recommendedGreaterTrinkets.length >= 1, guide.guideId)
  }
})

test('generated card references use real local thumbnails or explicit tutorial shells', () => {
  let imageCount = 0
  let tutorialCount = 0
  const visit = value => {
    if (Array.isArray(value)) {
      value.forEach(visit)
      return
    }
    if (!value || typeof value !== 'object') return
    if (Object.hasOwn(value, 'artType')) {
      assert.notEqual(value.artType, 'missing', value.stableId)
      if (value.artType === 'image') {
        imageCount += 1
        assert.match(value.image, /^\/assets\/cards\/[A-Za-z0-9._-]+\.jpg$/)
        assert.ok(fs.existsSync(path.join(root, value.image.slice(1))), value.image)
      } else {
        tutorialCount += 1
        assert.equal(value.kind, '教学法术')
        assert.ok(value.tutorialGlyph)
      }
    }
    Object.values(value).forEach(visit)
  }
  visit(guidesFixture)
  assert.ok(imageCount > 100)
  assert.equal(tutorialCount, 72)
})

test('guide API resolves direct identifiers, legacy codes and local profiles', async () => {
  assert.deepEqual(
    guideApi.resolveReference('GUIDE-S14-BEAST-LOBSTER-RALLY:difficult'),
    { guideId: 'GUIDE-S14-BEAST-LOBSTER-RALLY', profileId: 'difficult' }
  )
  assert.deepEqual(
    guideApi.resolveReference('2345-6789-ABCD-EFGH-JKMN'),
    { guideId: 'GUIDE-S14-BEAST-LOBSTER-RALLY', profileId: 'showcase' }
  )
  const result = await guideApi.getGuide('GUIDE-S14-PIRATE-BOUNTY-APM', 'difficult')
  assert.equal(result.guide.title, '海盗悬赏循环')
  assert.equal(result.profile.profileId, 'difficult')
})

test('favorites, current route and operation progress stay in native local storage', () => {
  const storage = new Map()
  global.wx = {
    getStorageSync(key) {
      return storage.has(key) ? storage.get(key) : ''
    },
    setStorageSync(key, value) {
      storage.set(key, value)
    },
    removeStorageSync(key) {
      storage.delete(key)
    }
  }
  const localState = require('../services/local-state')
  const guideId = 'GUIDE-S14-BEAST-LOBSTER-RALLY'
  const profileId = 'guided'

  assert.equal(localState.isGuideFavorite(guideId), false)
  assert.equal(localState.toggleGuideFavorite(guideId), true)
  assert.equal(localState.toggleGuideStep(guideId, profileId, 'buy-core')['buy-core'], true)
  assert.deepEqual(localState.lastGuide(), { guideId, profileId })
  localState.resetGuideProgress(guideId, profileId)
  assert.deepEqual(localState.guideProgress(guideId, profileId), {})
})

test('native mobile UI is one-sheet only, safe-area aware and operation led', () => {
  const app = JSON.parse(fs.readFileSync(path.join(root, 'app.json'), 'utf8'))
  const appStyles = fs.readFileSync(path.join(root, 'app.wxss'), 'utf8')
  const homeMarkup = fs.readFileSync(path.join(root, 'pages', 'index', 'index.wxml'), 'utf8')
  const detailMarkup = fs.readFileSync(path.join(root, 'pages', 'scenario', 'scenario.wxml'), 'utf8')
  const pageStyles = [
    fs.readFileSync(path.join(root, 'pages', 'index', 'index.wxss'), 'utf8'),
    fs.readFileSync(path.join(root, 'pages', 'scenario', 'scenario.wxss'), 'utf8')
  ].join('\n')

  assert.equal(app.window.navigationBarTitleText, '一图流训练')
  assert.deepEqual(app.pages, ['pages/index/index', 'pages/scenario/scenario'])
  assert.match(appStyles + pageStyles, /env\(safe-area-inset-bottom\)/)
  assert.match(appStyles + pageStyles, /min-height:\s*96rpx/)
  assert.match(homeMarkup, />一图流训练</)
  assert.match(homeMarkup, /class="cover-card-art"/)
  assert.match(detailMarkup, />起手战术板</)
  assert.match(detailMarkup, />操作顺序</)
  assert.match(detailMarkup, /完成本步/)
  assert.match(detailMarkup, />首回合 3 张，之后每回合 1 张</)
  assert.match(detailMarkup, /class="mini-card-art"/)
  assert.doesNotMatch(homeMarkup + detailMarkup, /<web-view\b/i)
  assert.doesNotMatch(homeMarkup + detailMarkup, /Windows 下载|复制网页试玩链接/)
})
