import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

import {
  communityNews,
  currentVersion,
  knownDifferences,
  mechanics,
  windowsRelease,
  unityRelease,
} from '../src/data/site-content.js'

const testRoot = path.dirname(fileURLToPath(import.meta.url))
const appSource = await readFile(path.resolve(testRoot, '../src/App.vue'), 'utf8')
const routerSource = await readFile(path.resolve(testRoot, '../src/router.js'), 'utf8')
const guidesPageSource = await readFile(path.resolve(testRoot, '../src/pages/GuidesPage.vue'), 'utf8')
const downloadPageSource = await readFile(path.resolve(testRoot, '../src/pages/DownloadPage.vue'), 'utf8')
const playPageSource = await readFile(path.resolve(testRoot, '../src/pages/PlayPage.vue'), 'utf8')
const indexSource = await readFile(path.resolve(testRoot, '../index.html'), 'utf8')
const webManifest = JSON.parse(await readFile(
  path.resolve(testRoot, '../public/manifest.webmanifest'),
  'utf8',
))
const guideCatalog = JSON.parse(await readFile(
  path.resolve(testRoot, '../public/data/guides.json'),
  'utf8',
))
const windowsManifest = JSON.parse(await readFile(
  path.resolve(testRoot, '../public/releases/windows-release-manifest.json'),
  'utf8',
))

test('36.2 exposes only dark gifts and trinkets', () => {
  assert.equal(currentVersion.id, '36.2-preview')
  assert.equal(currentVersion.label, '36.2')
  assert.equal(currentVersion.officialStatus, '已上线')
  assert.equal(currentVersion.trainerStatus, '已上线')
  assert.equal(currentVersion.supportLabel, '限定训练范围')
  assert.deepEqual(mechanics.map(({ id }) => id), ['dark-gifts', 'trinkets'])
  assert.ok(mechanics.every(({ status }) => status === '已上线可试玩'))
  assert.ok(knownDifferences.every(item => !item.includes('正式 hero / power DBF')))
})

test('minimal shell exposes the lightweight guide routes alongside product routes', () => {
  for (const route of ["'/'", "'/versions'", "'/guides'", "'/guides/:guideId'", "'/play'", "'/download'"]) {
    assert.match(routerSource, new RegExp(`path: ${route.replace('/', '\\/')}`))
  }
  assert.doesNotMatch(routerSource, /path:\s*['"]\/s\//)
})

test('lightweight guide catalog is complete and does not embed Unity', () => {
  assert.equal(guideCatalog.guides.length, 8)
  assert.ok(guideCatalog.guides.every(guide => guide.profiles.length === 3))
  assert.ok(guideCatalog.guides.every(guide => guide.profiles.every(profile => Array.isArray(profile.steps))))
  assert.ok(guideCatalog.guides.every(guide => guide.profiles.some(profile => profile.steps.length > 0)))
  assert.doesNotMatch(guidesPageSource, /createUnityInstance|\.wasm|\.data|UnityLoader/)
})

test('version center links only to explicit HTTPS community sources', () => {
  assert.equal(communityNews.length, 3)
  for (const article of communityNews) {
    assert.equal(new URL(article.href).protocol, 'https:')
    assert.equal(new URL(article.href).hostname, 'www.iyingdi.com')
  }
})

test('the verified Windows release is downloadable and matches its manifest', () => {
  assert.equal(windowsRelease.available, true)
  assert.equal(windowsRelease.candidateBuilt, true)
  assert.match(windowsRelease.reason, /解压/)
  assert.equal(windowsRelease.version, currentVersion.id)
  assert.equal(windowsRelease.contentSnapshotId, currentVersion.contentSnapshotId)
  assert.equal(windowsRelease.sha256, windowsManifest.artifact.sha256)
  assert.equal(windowsRelease.artifactBytes, windowsManifest.artifact.bytes)
  assert.equal(windowsRelease.downloadUrl, windowsManifest.delivery.url)
  assert.equal(new URL(windowsRelease.downloadUrl).hostname, 'downloads.jsoncool.com')
  assert.equal(windowsManifest.validation.nativeShutdown, 'passed-d3d11-d3d12-exit0-no-dump')
  assert.equal(windowsManifest.validation.remoteReadbackSha256, 'passed-12-ranges')
  assert.equal(windowsManifest.validation.publicReleaseStatus, 'ready')
})

test('the player download page hides release engineering details', () => {
  for (const technicalDetail of [
    'buildJobId',
    'contentSnapshotId',
    'SHA-256',
    'D3D11',
    'Cloudflare R2',
    'RELEASE GATE',
    '发布前必须同时具备',
  ]) {
    assert.doesNotMatch(downloadPageSource, new RegExp(technicalDetail))
  }
  assert.match(downloadPageSource, /全部解压/)
  assert.match(downloadPageSource, /Learn Heartstone\.exe/)
  assert.match(downloadPageSource, /不要只移动或复制 EXE/)
})

test('player-facing play gate and footer omit release engineering metadata', () => {
  assert.doesNotMatch(playPageSource, /内容快照|contentSnapshotId|门外只加载轻量页面|Unity loader/)
  assert.doesNotMatch(appSource, /36\.2 Preview|Cloudflare · Unity|footer-meta/)
})

test('play gate defers Unity and offers browser and mobile fullscreen paths', () => {
  assert.equal(unityRelease.chunkCount, 12)
  assert.equal(unityRelease.sourceDataBytes, 107314429)
  assert.match(playPageSource, /state === 'idle' \|\| state === 'failed'/)
  assert.match(playPageSource, /requestFullscreen \|\| document\.documentElement\.webkitRequestFullscreen/)
  assert.match(playPageSource, /全屏进入训练场/)
  assert.match(playPageSource, /窗口模式进入/)
  assert.match(playPageSource, /添加到主屏幕/)
  assert.match(playPageSource, /screen\.orientation\?\.lock/)
  assert.match(playPageSource, /screen\.orientation\.lock\('landscape'\)/)
  assert.match(playPageSource, /allow="autoplay; fullscreen; gamepad"/)
})

test('web app manifest enables installed mobile fullscreen without changing the lightweight start path', () => {
  assert.equal(webManifest.start_url, '/play')
  assert.equal(webManifest.display, 'fullscreen')
  assert.equal(webManifest.orientation, 'landscape')
  assert.ok(webManifest.icons.some(icon => icon.purpose.includes('maskable')))
  assert.match(indexSource, /rel="manifest" href="\/manifest\.webmanifest"/)
  assert.match(indexSource, /apple-mobile-web-app-capable/)
})
