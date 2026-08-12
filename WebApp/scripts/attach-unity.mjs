import { cp, mkdir, readFile, rm, stat } from 'node:fs/promises'
import path from 'node:path'
import process from 'node:process'

const appRoot = path.resolve(import.meta.dirname, '..')
const distRoot = path.join(appRoot, 'dist')
const unityTarget = path.join(distRoot, 'unity')
const candidateArg = process.argv[2]

if (!candidateArg) {
  throw new Error('Usage: npm run build:with-unity -- <release-candidate-directory>')
}

const candidateRoot = path.resolve(process.cwd(), candidateArg)
const requiredEntries = [
  'index.html',
  'Build',
  'TemplateData',
  'content',
  'release-meta.json',
]

for (const entry of requiredEntries) {
  await stat(path.join(candidateRoot, entry)).catch(() => {
    throw new Error(`Release candidate is incomplete: missing ${entry}`)
  })
}

const releaseMeta = JSON.parse(await readFile(path.join(candidateRoot, 'release-meta.json'), 'utf8'))
const contentSnapshotId = releaseMeta.contentSnapshotId ?? releaseMeta.snapshotId ?? releaseMeta.contentVersion
if (!contentSnapshotId || !releaseMeta.rulesetId) {
  throw new Error('release-meta.json must include a snapshot/content version and rulesetId')
}

await mkdir(distRoot, { recursive: true })

const resolvedTarget = path.resolve(unityTarget)
if (path.dirname(resolvedTarget) !== path.resolve(distRoot) || path.basename(resolvedTarget) !== 'unity') {
  throw new Error(`Refusing to replace unexpected target: ${resolvedTarget}`)
}

await rm(resolvedTarget, { recursive: true, force: true })
await cp(candidateRoot, resolvedTarget, { recursive: true })
await rm(path.join(resolvedTarget, '_headers'), { force: true })

console.log(`Attached Unity candidate: ${candidateRoot}`)
console.log(`Content identity: ${contentSnapshotId} / ${releaseMeta.rulesetId}`)
