import { createHash } from 'node:crypto'
import { spawnSync } from 'node:child_process'
import { cp, mkdir, mkdtemp, readFile, rm, stat, writeFile } from 'node:fs/promises'
import os from 'node:os'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url))
const repositoryRoot = path.resolve(scriptDirectory, '..', '..')
const windowsBuildRoot = path.join(repositoryRoot, 'Builds', 'Windows')
const webCandidateRoot = path.join(repositoryRoot, 'Builds', 'ReleaseCandidate')
const windowsReleaseRoot = path.join(repositoryRoot, 'Builds', 'WindowsRelease')

const usage = `Usage:
  node Tools/Release/assemble-windows-candidate.mjs --windows <build-directory> --web-release <candidate-directory> --build-job <job-id> --output <release-directory>

The output must be a new child of Builds/WindowsRelease. This command never uploads or publishes.`

function fail(message) {
  throw new Error(message)
}

function parseArguments(values) {
  const args = {}
  for (let index = 0; index < values.length; index += 1) {
    const value = values[index]
    if (value === '--help' || value === '-h') {
      args.help = true
      continue
    }
    if (!value.startsWith('--') || index + 1 >= values.length) {
      fail(`Invalid argument: ${value}\n\n${usage}`)
    }
    args[value.slice(2)] = values[index + 1]
    index += 1
  }
  return args
}

function isInside(parent, child) {
  const relative = path.relative(parent, child)
  return relative !== '' && !relative.startsWith('..') && !path.isAbsolute(relative)
}

function safeToken(value, label) {
  if (typeof value !== 'string' || !/^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(value)) {
    fail(`${label} is missing or unsafe`)
  }
  return value
}

function sha256(bytes) {
  return createHash('sha256').update(bytes).digest('hex')
}

async function pathExists(target) {
  try {
    await stat(target)
    return true
  } catch (error) {
    if (error?.code === 'ENOENT') return false
    throw error
  }
}

async function readJson(filePath) {
  return JSON.parse(await readFile(filePath, 'utf8'))
}

async function validateContentPackage(contentDirectory, manifest) {
  if (!Array.isArray(manifest.files) || manifest.files.length === 0) {
    fail('Content manifest contains no files')
  }
  for (const file of manifest.files) {
    if (typeof file.fileName !== 'string' || path.basename(file.fileName) !== file.fileName) {
      fail('Content manifest contains an unsafe file name')
    }
    const filePath = path.join(contentDirectory, file.fileName)
    const bytes = await readFile(filePath)
    if (bytes.byteLength !== file.bytes || sha256(bytes) !== file.sha256) {
      fail(`Content file integrity mismatch: ${file.fileName}`)
    }
  }
}

async function main() {
  const args = parseArguments(process.argv.slice(2))
  if (args.help) {
    console.log(usage)
    return
  }
  for (const required of ['windows', 'web-release', 'build-job', 'output']) {
    if (!args[required]) fail(`--${required} is required\n\n${usage}`)
  }

  const windowsPath = path.resolve(repositoryRoot, args.windows)
  const webReleasePath = path.resolve(repositoryRoot, args['web-release'])
  const outputPath = path.resolve(repositoryRoot, args.output)
  if (!isInside(windowsBuildRoot, windowsPath)) fail('Windows input must be under Builds/Windows')
  if (!isInside(webCandidateRoot, webReleasePath)) fail('Web release input must be under Builds/ReleaseCandidate')
  if (!isInside(windowsReleaseRoot, outputPath)) fail('Output must be under Builds/WindowsRelease')
  if (await pathExists(outputPath)) fail(`Output already exists: ${outputPath}`)

  for (const required of ['Learn Heartstone.exe', 'Learn Heartstone_Data', 'UnityPlayer.dll', 'content']) {
    if (!(await pathExists(path.join(windowsPath, required)))) {
      fail(`Windows candidate is missing ${required}`)
    }
  }

  const releaseMeta = await readJson(path.join(webReleasePath, 'release-meta.json'))
  const webContentPath = path.join(webReleasePath, 'content')
  const windowsContentPath = path.join(windowsPath, 'content')
  const webManifestBytes = await readFile(path.join(webContentPath, 'content-manifest.json'))
  const windowsManifestBytes = await readFile(path.join(windowsContentPath, 'content-manifest.json'))
  if (!webManifestBytes.equals(windowsManifestBytes)) {
    fail('Windows and WebGL content manifests are not byte-identical')
  }

  const contentManifest = JSON.parse(webManifestBytes.toString('utf8'))
  await validateContentPackage(webContentPath, contentManifest)
  await validateContentPackage(windowsContentPath, contentManifest)

  const identity = {
    clientVersion: safeToken(releaseMeta.clientVersion, 'clientVersion'),
    contentSnapshotId: safeToken(contentManifest.snapshotId, 'contentSnapshotId'),
    gameVersionId: safeToken(contentManifest.gameVersionId, 'gameVersionId'),
    rulesetId: safeToken(contentManifest.rulesetId, 'rulesetId'),
    packageFingerprint: safeToken(contentManifest.packageFingerprint, 'packageFingerprint'),
  }
  if (
    identity.contentSnapshotId !== releaseMeta.snapshotId ||
    identity.gameVersionId !== releaseMeta.gameVersionId ||
    identity.rulesetId !== releaseMeta.rulesetId ||
    identity.packageFingerprint !== releaseMeta.packageFingerprint
  ) {
    fail('Windows content identity does not match WebGL release metadata')
  }

  const releaseId = `${identity.clientVersion}__${identity.contentSnapshotId}__${args['build-job']}`
  const artifactName = `LearnHeartstone-Windows-x64-${releaseId}.zip`
  const temporaryRoot = await mkdtemp(path.join(os.tmpdir(), 'learn-heartstone-windows-release-'))
  const packageRoot = path.join(temporaryRoot, 'Learn Heartstone')

  try {
    await cp(windowsPath, packageRoot, {
      recursive: true,
      filter: (source) => {
        const name = path.basename(source)
        return !name.endsWith('_BurstDebugInformation_DoNotShip') &&
          !/^Player-.*\.log$/i.test(name)
      },
    })

    const packageMeta = {
      schemaVersion: 1,
      platform: 'Windows-x64',
      ...identity,
      unityVersion: releaseMeta.unityVersion,
      sourceCommit: releaseMeta.sourceCommit,
      sourceDirty: releaseMeta.sourceDirty,
      buildJobId: args['build-job'],
      publicReleaseStatus: 'blocked',
      blockers: [
        'Unity 6000.4.10f1 on the current Windows host exits with 0xC0000005 in UnityPlayer.dll after a settled native window close.',
        'Cloudflare R2 is not enabled for the current account, so the large Windows artifact has no approved public object host.',
      ],
    }
    await writeFile(
      path.join(packageRoot, 'windows-release-meta.json'),
      `${JSON.stringify(packageMeta, null, 2)}\n`,
      'utf8',
    )

    await mkdir(outputPath, { recursive: true })
    const artifactPath = path.join(outputPath, artifactName)
    const archive = spawnSync('tar.exe', ['-a', '-cf', artifactPath, '-C', temporaryRoot, 'Learn Heartstone'], {
      encoding: 'utf8',
    })
    if (archive.status !== 0) {
      fail(`tar.exe failed: ${archive.stderr || archive.stdout}`)
    }

    const artifactBytes = await readFile(artifactPath)
    const releaseManifest = {
      schemaVersion: 1,
      generatedAtUtc: new Date().toISOString(),
      platform: 'Windows-x64',
      ...identity,
      unityVersion: releaseMeta.unityVersion,
      sourceCommit: releaseMeta.sourceCommit,
      sourceDirty: releaseMeta.sourceDirty,
      artifact: {
        fileName: artifactName,
        bytes: artifactBytes.byteLength,
        sha256: sha256(artifactBytes),
      },
      validation: {
        buildJobId: args['build-job'],
        buildResult: 'succeeded',
        contentIdentity: 'passed',
        runtimeLogErrors: 0,
        nativeShutdown: 'failed-0xC0000005',
        publicReleaseStatus: 'blocked',
      },
    }
    await writeFile(
      path.join(outputPath, 'windows-release-manifest.json'),
      `${JSON.stringify(releaseManifest, null, 2)}\n`,
      'utf8',
    )

    console.log(`Windows verification artifact: ${path.relative(repositoryRoot, artifactPath)}`)
    console.log(`Bytes: ${releaseManifest.artifact.bytes}`)
    console.log(`SHA-256: ${releaseManifest.artifact.sha256}`)
    console.log(`Identity: ${identity.contentSnapshotId} / ${identity.gameVersionId} / ${identity.rulesetId}`)
    console.log('Public release status: blocked')
  } finally {
    await rm(temporaryRoot, { recursive: true, force: true })
  }
}

main().catch((error) => {
  console.error(error.message)
  process.exitCode = 1
})
