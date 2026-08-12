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
} from '../src/data/site-content.js'

const testRoot = path.dirname(fileURLToPath(import.meta.url))
const routerSource = await readFile(path.resolve(testRoot, '../src/router.js'), 'utf8')
const guidesPageSource = await readFile(path.resolve(testRoot, '../src/pages/GuidesPage.vue'), 'utf8')
const downloadPageSource = await readFile(path.resolve(testRoot, '../src/pages/DownloadPage.vue'), 'utf8')
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
})
