#!/usr/bin/env node

import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { cp, mkdir, readFile, readdir, stat, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { TextDecoder } from "node:util";

import { replaceBrotliDataWithChunks } from "./webgl-data-chunks.mjs";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, "..", "..");
const releaseRoot = path.join(repositoryRoot, "Builds", "ReleaseCandidate");
const cloudflareHeadersPath = path.join(repositoryRoot, "Deploy", "Cloudflare", "_headers");
const dataRoot = path.join(repositoryRoot, "Assets", "LearnHearthstone", "Resources", "Data");
const generatedRootFiles = new Set(["release-meta.txt", "release-meta.json", "_headers"]);
const strictUtf8 = new TextDecoder("utf-8", { fatal: true });
const maxContentBytes = 16 * 1024 * 1024;
const maxCloudflarePagesAssetBytes = 25 * 1024 * 1024;
const defaultGameVersionId = "legacy-composite-sandbox-v1";
const gameVersionProfiles = Object.freeze({
  "legacy-composite-sandbox-v1": {
    rulesetId: "ruleset-legacy-composite-v1",
    contentSetId: "content-legacy-composite-v1",
  },
  "36.2-preview": {
    rulesetId: "ruleset-36.2-preview-v1",
    contentSetId: "content-36.2-preview-v1",
  },
});
const contentFileDefinitions = Object.freeze([
  { kind: "versions", outputName: "versions", sourceFile: "battlegroundsGameVersions.json" },
  { kind: "rulesets", outputName: "rulesets", sourceFile: "battlegroundsRulesets.json" },
  { kind: "heroes", outputName: "heroes", sourceFile: "battlegroundsHeroes.json" },
  { kind: "minions", outputName: "minions", sourceFile: "battlegroundsMinions.json" },
  { kind: "tavern-spells", outputName: "tavernSpells", sourceFile: "battlegroundsSpells.json" },
  { kind: "trinkets", outputName: "trinkets", sourceFile: "battlegroundsTrinkets.json" },
  { kind: "quests", outputName: "quests", sourceFile: "battlegroundsQuests.json" },
  { kind: "anomalies", outputName: "anomalies", sourceFile: "battlegroundsAnomalies.json" },
  { kind: "timewarped-tavern", outputName: "timewarpedTavern", sourceFile: "timewarpedTavernCards.json" },
  { kind: "darkmoon-prizes", outputName: "darkmoonPrizes", sourceFile: "darkmoonPrizes.json" },
  { kind: "dark-gifts", outputName: "darkGifts", sourceFile: "battlegroundsDarkGifts.json" },
  { kind: "localizations", outputName: "heroLocalizationZhCN", sourceFile: "battlegroundsHeroLocalizationZhCN.json" },
  { kind: "localizations", outputName: "questLocalizationZhCN", sourceFile: "battlegroundsQuestLocalizationZhCN.json" },
  { kind: "localizations", outputName: "trinketLocalizationZhCN", sourceFile: "battlegroundsTrinketLocalizationZhCN.json" },
  { kind: "localizations", outputName: "anomalyLocalizationZhCN", sourceFile: "battlegroundsAnomalyLocalizationZhCN.json" },
  { kind: "localizations", outputName: "darkmoonPrizeLocalizationZhCN", sourceFile: "darkmoonPrizeLocalizationZhCN.json" },
  { kind: "localizations", outputName: "darkGiftLocalizationZhCN", sourceFile: "battlegroundsDarkGiftLocalizationZhCN.json" },
  { kind: "asset-map", outputName: "assetMap" },
]);

const usage = `Usage:
  node Tools/Release/assemble-release-candidate.mjs --webgl <build-directory> --content-version <version> [--game-version <id>] [--snapshot-id <id>] [--output <candidate-directory>]

The output must stay under Builds/ReleaseCandidate. The command is offline and never deploys.`;

function fail(message) {
  throw new Error(message);
}

function parseArguments(argv) {
  const result = {};
  const argumentNames = {
    "--webgl": "webgl",
    "--content-version": "contentVersion",
    "--game-version": "gameVersion",
    "--snapshot-id": "snapshotId",
    "--output": "output",
  };
  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index];
    if (argument === "--help" || argument === "-h") {
      result.help = true;
      continue;
    }
    if (!Object.hasOwn(argumentNames, argument)) {
      fail(`Unknown argument: ${argument}`);
    }
    const value = argv[index + 1];
    if (!value || value.startsWith("--")) {
      fail(`Missing value for ${argument}`);
    }
    result[argumentNames[argument]] = value;
    index += 1;
  }
  return result;
}

function runGit(args) {
  return execFileSync("git", args, { cwd: repositoryRoot, encoding: "utf8" }).trim();
}

function readRequiredMatch(text, pattern, description) {
  const match = text.match(pattern);
  if (!match) {
    fail(`Could not read ${description}`);
  }
  return match[1].trim().replace(/^"|"$/g, "");
}

function compactUtc(date) {
  return date.toISOString().replace(/[-:]/g, "").replace(/\.\d{3}Z$/, "Z");
}

function safePathSegment(value) {
  return value.replace(/[^A-Za-z0-9._-]+/g, "-");
}

function validateContentVersion(value) {
  return validateSafeToken(value, "content version");
}

function validateSafeToken(value, description) {
  if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$/.test(value) || value.includes("..")) {
    fail(`Unsafe ${description}: ${value}`);
  }
  return value;
}

function sha256Hex(bytes) {
  return createHash("sha256").update(bytes).digest("hex");
}

function compareOrdinal(left, right) {
  return left < right ? -1 : left > right ? 1 : 0;
}

function generatedPayload(kind, context) {
  switch (kind) {
    case "asset-map":
      return { schemaVersion: 1, assets: [] };
    default:
      fail(`Content file definition is missing a source: ${kind}`);
  }
}

function encodeJson(payload) {
  return Buffer.from(`${JSON.stringify(payload, null, 2)}\n`, "utf8");
}

async function sourceContentBytes(definition) {
  const sourcePath = path.join(dataRoot, definition.sourceFile);
  const sourceBytes = await readFile(sourcePath);
  if (sourceBytes.byteLength === 0 || sourceBytes.byteLength > maxContentBytes) {
    fail(`Content source size is outside the supported range: ${sourcePath}`);
  }
  if (sourceBytes[0] === 0xef && sourceBytes[1] === 0xbb && sourceBytes[2] === 0xbf) {
    fail(`Content source must not contain a UTF-8 BOM: ${sourcePath}`);
  }

  let sourceText;
  let payload;
  try {
    sourceText = strictUtf8.decode(sourceBytes);
    payload = JSON.parse(sourceText);
  } catch (error) {
    fail(`Invalid UTF-8 or JSON in ${sourcePath}: ${error.message}`);
  }
  if (!payload || Array.isArray(payload) || typeof payload !== "object") {
    fail(`Content source must contain a JSON object: ${sourcePath}`);
  }
  if (Object.hasOwn(payload, "schemaVersion")) {
    if (payload.schemaVersion !== 1) {
      fail(`Unsupported schemaVersion in ${sourcePath}: ${payload.schemaVersion}`);
    }
    return sourceBytes;
  }

  const objectStart = sourceText.indexOf("{");
  const versionedBytes = Buffer.from(
    `${sourceText.slice(0, objectStart + 1)}"schemaVersion":1,${sourceText.slice(objectStart + 1)}`,
    "utf8",
  );
  if (versionedBytes.byteLength > maxContentBytes) {
    fail(`Versioned content source is too large: ${sourcePath}`);
  }
  return versionedBytes;
}

async function createContentFiles(contentVersion, context) {
  const files = [];
  for (const definition of contentFileDefinitions) {
    const contentBytes = definition.sourceFile
      ? await sourceContentBytes(definition)
      : encodeJson(generatedPayload(definition.kind, context));
    const fileName = `${definition.outputName}.v${contentVersion}.json`;
    files.push({
      kind: definition.kind,
      fileName,
      schemaVersion: 1,
      bytes: contentBytes.byteLength,
      sha256: sha256Hex(contentBytes),
      contentBytes,
    });
  }
  return files.sort((left, right) =>
    compareOrdinal(left.kind, right.kind) || compareOrdinal(left.fileName, right.fileName));
}

function packageFingerprint(files) {
  const canonical = [...files]
    .sort((left, right) =>
      compareOrdinal(left.kind, right.kind) || compareOrdinal(left.fileName, right.fileName))
    .map((file) => `${file.kind}|${file.fileName}|${file.schemaVersion}|${file.bytes}|${file.sha256}`)
    .join("\n");
  return sha256Hex(Buffer.from(canonical, "utf8"));
}

function isInside(parent, candidate) {
  const relative = path.relative(parent, candidate);
  return relative !== "" && !relative.startsWith("..") && !path.isAbsolute(relative);
}

async function pathExists(target) {
  try {
    await stat(target);
    return true;
  } catch (error) {
    if (error.code === "ENOENT") {
      return false;
    }
    throw error;
  }
}

async function readWebGLBuild(sitePath) {
  for (const requiredPath of ["index.html", "Build", "TemplateData"]) {
    if (!(await pathExists(path.join(sitePath, requiredPath)))) {
      fail(`WebGL site is missing ${requiredPath}: ${sitePath}`);
    }
  }

  const buildFiles = await readdir(path.join(sitePath, "Build"));
  const indexHtml = await readFile(path.join(sitePath, "index.html"), "utf8");
  return { buildFiles, indexHtml };
}

async function validateCloudflareAssetSizes(sitePath, buildFiles) {
  for (const buildFile of buildFiles) {
    const buildAsset = path.join(sitePath, "Build", buildFile);
    const buildAssetStats = await stat(buildAsset);
    if (buildAssetStats.isFile() && buildAssetStats.size > maxCloudflarePagesAssetBytes) {
      fail(`Cloudflare Pages asset exceeds 25 MiB: Build/${buildFile} (${buildAssetStats.size} bytes)`);
    }
  }
}

async function validateWebGLSource(sitePath) {
  const { buildFiles, indexHtml } = await readWebGLBuild(sitePath);
  if (!indexHtml.includes("resolveChunkedDataUrl(config.dataUrl")) {
    fail("WebGL source is missing the data chunk bootstrap; create a new WebGL build before assembling a Pages candidate");
  }

  for (const suffix of [".loader.js", ".data.br", ".framework.js.br", ".wasm.br"]) {
    const matches = buildFiles.filter((file) => file.endsWith(suffix));
    if (matches.length !== 1) {
      fail(`Expected exactly one Build/*${suffix}, found ${matches.length}`);
    }
  }

  for (const buildFile of buildFiles.filter((file) => /\.(loader\.js|data\.br|framework\.js\.br|wasm\.br)$/.test(file))) {
    if (!indexHtml.includes(buildFile)) {
      fail(`index.html does not reference Build/${buildFile}`);
    }
  }
}

async function validateCandidateWebGLSite(sitePath) {
  const { buildFiles, indexHtml } = await readWebGLBuild(sitePath);
  await validateCloudflareAssetSizes(sitePath, buildFiles);
  if (!indexHtml.includes("resolveChunkedDataUrl(config.dataUrl")) {
    fail("ReleaseCandidate is missing the WebGL data chunk bootstrap");
  }

  for (const suffix of [".loader.js", ".framework.js.br", ".wasm.br"]) {
    const matches = buildFiles.filter((file) => file.endsWith(suffix));
    if (matches.length !== 1) {
      fail(`Expected exactly one Build/*${suffix}, found ${matches.length}`);
    }
    if (!indexHtml.includes(matches[0])) {
      fail(`index.html does not reference Build/${matches[0]}`);
    }
  }

  const monoliths = buildFiles.filter((file) => file.endsWith(".data.br"));
  if (monoliths.length !== 0) {
    fail(`ReleaseCandidate must not contain a monolithic Build/*.data.br, found ${monoliths.length}`);
  }

  const manifestFiles = buildFiles.filter((file) => file.endsWith(".data.br.chunks.json"));
  if (manifestFiles.length !== 1) {
    fail(`Expected exactly one Build/*.data.br.chunks.json, found ${manifestFiles.length}`);
  }
  const manifest = JSON.parse(await readFile(path.join(sitePath, "Build", manifestFiles[0]), "utf8"));
  if (manifest.schemaVersion !== 1 || typeof manifest.originalFile !== "string" || !Array.isArray(manifest.chunks) || manifest.chunks.length === 0) {
    fail("Invalid WebGL data chunk manifest");
  }
  if (!indexHtml.includes(manifest.originalFile)) {
    fail(`index.html does not reference chunk manifest source Build/${manifest.originalFile}`);
  }

  let totalUncompressedBytes = 0;
  const expectedChunkFiles = [];
  for (const chunk of manifest.chunks) {
    if (typeof chunk.file !== "string" || path.basename(chunk.file) !== chunk.file || !chunk.file.startsWith(`${manifest.originalFile}.part`) || !chunk.file.endsWith(".data-chunk.br")) {
      fail("Invalid WebGL data chunk file name");
    }
    const chunkBytes = await readFile(path.join(sitePath, "Build", chunk.file));
    if (chunkBytes.byteLength !== chunk.compressedBytes || sha256Hex(chunkBytes) !== chunk.sha256) {
      fail(`WebGL data chunk integrity mismatch: ${chunk.file}`);
    }
    totalUncompressedBytes += chunk.uncompressedBytes;
    expectedChunkFiles.push(chunk.file);
  }
  if (totalUncompressedBytes !== manifest.uncompressedBytes) {
    fail("WebGL data chunk uncompressed byte total does not match its manifest");
  }

  const actualChunkFiles = buildFiles.filter((file) => file.startsWith(`${manifest.originalFile}.part`) && file.endsWith(".data-chunk.br"));
  if (JSON.stringify(actualChunkFiles.sort(compareOrdinal)) !== JSON.stringify(expectedChunkFiles.sort(compareOrdinal))) {
    fail("ReleaseCandidate contains unexpected WebGL data chunks");
  }
}

async function verifyCandidate(candidatePath, expectedMetadata, expectedManifest) {
  await validateCandidateWebGLSite(candidatePath);

  for (const requiredPath of ["_headers", "release-meta.json"]) {
    if (!(await pathExists(path.join(candidatePath, requiredPath)))) {
      fail(`ReleaseCandidate is missing ${requiredPath}`);
    }
  }
  if (await pathExists(path.join(candidatePath, "release-meta.txt"))) {
    fail("ReleaseCandidate contains legacy release-meta.txt");
  }

  const canonicalHeaders = await readFile(cloudflareHeadersPath, "utf8");
  const candidateHeaders = await readFile(path.join(candidatePath, "_headers"), "utf8");
  if (candidateHeaders !== canonicalHeaders) {
    fail("ReleaseCandidate _headers does not match Deploy/Cloudflare/_headers");
  }

  const metadataText = await readFile(path.join(candidatePath, "release-meta.json"), "utf8");
  const metadata = JSON.parse(metadataText);
  if (JSON.stringify(metadata) !== JSON.stringify(expectedMetadata)) {
    fail("ReleaseCandidate metadata changed after generation");
  }
  if (/[A-Za-z]:[\\/]/.test(metadataText) || metadataText.includes(repositoryRoot)) {
    fail("release-meta.json contains a machine absolute path");
  }

  const contentPath = path.join(candidatePath, "content");
  const contentEntries = (await readdir(contentPath)).sort(compareOrdinal);
  const expectedEntries = [
    "content-manifest.json",
    ...expectedManifest.files.map((file) => file.fileName),
  ].sort(compareOrdinal);
  if (JSON.stringify(contentEntries) !== JSON.stringify(expectedEntries)) {
    fail("ReleaseCandidate content directory contains unexpected files");
  }

  const manifestText = await readFile(path.join(contentPath, "content-manifest.json"), "utf8");
  const manifest = JSON.parse(manifestText);
  if (JSON.stringify(manifest) !== JSON.stringify(expectedManifest)) {
    fail("ReleaseCandidate content manifest changed after generation");
  }
  if (/[A-Za-z]:[\\/]/.test(manifestText) || manifestText.includes(repositoryRoot)) {
    fail("content-manifest.json contains a machine absolute path");
  }
  if (packageFingerprint(manifest.files) !== manifest.packageFingerprint) {
    fail("ReleaseCandidate package fingerprint does not match its manifest");
  }

  for (const file of manifest.files) {
    const contentBytes = await readFile(path.join(contentPath, file.fileName));
    if (contentBytes.byteLength !== file.bytes) {
      fail(`ReleaseCandidate content byte count does not match: ${file.fileName}`);
    }
    if (sha256Hex(contentBytes) !== file.sha256) {
      fail(`ReleaseCandidate content SHA-256 does not match: ${file.fileName}`);
    }
    let payload;
    try {
      payload = JSON.parse(strictUtf8.decode(contentBytes));
    } catch (error) {
      fail(`Invalid candidate UTF-8 or JSON in ${file.fileName}: ${error.message}`);
    }
    if (payload?.schemaVersion !== file.schemaVersion) {
      fail(`ReleaseCandidate schemaVersion does not match: ${file.fileName}`);
    }
  }
}

async function main() {
  const args = parseArguments(process.argv.slice(2));
  if (args.help) {
    console.log(usage);
    return;
  }
  if (!args.webgl) {
    fail(`--webgl is required\n\n${usage}`);
  }
  if (!args.contentVersion) {
    fail(`--content-version is required\n\n${usage}`);
  }

  const sourcePath = path.resolve(repositoryRoot, args.webgl);
  await validateWebGLSource(sourcePath);
  const contentVersion = validateContentVersion(args.contentVersion);
  const gameVersionId = args.gameVersion ?? defaultGameVersionId;
  const gameVersionProfile = gameVersionProfiles[gameVersionId];
  if (!gameVersionProfile) {
    fail(`Unsupported game version: ${gameVersionId}`);
  }
  const snapshotId = validateSafeToken(args.snapshotId ?? contentVersion, "snapshot id");

  const projectSettings = await readFile(path.join(repositoryRoot, "ProjectSettings", "ProjectSettings.asset"), "utf8");
  const projectVersion = await readFile(path.join(repositoryRoot, "ProjectSettings", "ProjectVersion.txt"), "utf8");
  const clientVersion = readRequiredMatch(projectSettings, /^\s*bundleVersion:\s*(.+)$/m, "PlayerSettings.bundleVersion");
  const unityVersion = readRequiredMatch(projectVersion, /^m_EditorVersion:\s*(.+)$/m, "Unity editor version");
  const sourceCommit = runGit(["rev-parse", "HEAD"]);
  const sourceStatusBefore = runGit(["status", "--porcelain=v1", "--untracked-files=all"]);
  const sourceDirty = sourceStatusBefore.length > 0;
  const builtAt = new Date();
  const buildId = `${compactUtc(builtAt)}-${sourceCommit.slice(0, 7)}${sourceDirty ? "-dirty" : ""}`;
  const defaultCandidatePath = path.join(releaseRoot, `${safePathSegment(clientVersion)}__${buildId}`);
  const candidatePath = path.resolve(repositoryRoot, args.output ?? defaultCandidatePath);

  if (!isInside(releaseRoot, candidatePath)) {
    fail(`Output must be a child of ${releaseRoot}`);
  }
  if (candidatePath === sourcePath || isInside(sourcePath, candidatePath)) {
    fail("Output cannot be the WebGL source directory or one of its children");
  }
  if (await pathExists(candidatePath)) {
    fail(`Output already exists: ${candidatePath}`);
  }

  const contentFiles = await createContentFiles(contentVersion, {
    gameVersionId,
    rulesetId: gameVersionProfile.rulesetId,
    contentSetId: gameVersionProfile.contentSetId,
  });
  const manifestFiles = contentFiles.map((file) => ({
    kind: file.kind,
    fileName: file.fileName,
    schemaVersion: file.schemaVersion,
    bytes: file.bytes,
    sha256: file.sha256,
  }));
  const contentFingerprint = packageFingerprint(manifestFiles);
  const contentManifest = {
    protocolVersion: 2,
    contentVersion,
    snapshotId,
    gameVersionId,
    rulesetId: gameVersionProfile.rulesetId,
    minClientVersion: clientVersion,
    maxClientVersion: clientVersion,
    generatedAtUtc: builtAt.toISOString(),
    files: manifestFiles,
    packageFingerprint: contentFingerprint,
  };
  const metadata = {
    schemaVersion: 1,
    clientVersion,
    contentVersion,
    snapshotId,
    gameVersionId,
    rulesetId: gameVersionProfile.rulesetId,
    packageFingerprint: contentFingerprint,
    buildId,
    sourceCommit,
    sourceDirty,
    unityVersion,
    builtAtUtc: builtAt.toISOString(),
  };

  await mkdir(releaseRoot, { recursive: true });
  await cp(sourcePath, candidatePath, {
    recursive: true,
    filter(source) {
      const relative = path.relative(sourcePath, source).replaceAll("\\", "/");
      const sourceRootJson = !relative.includes("/") && relative.endsWith(".json");
      return relative === "" ||
        (!generatedRootFiles.has(relative) && !sourceRootJson && relative !== "content" && !relative.startsWith("content/"));
    },
  });
  const candidateBuildPath = path.join(candidatePath, "Build");
  const candidateDataFiles = (await readdir(candidateBuildPath)).filter((file) => file.endsWith(".data.br"));
  await replaceBrotliDataWithChunks(path.join(candidateBuildPath, candidateDataFiles[0]), {
    maxCompressedChunkBytes: maxCloudflarePagesAssetBytes,
  });
  await cp(cloudflareHeadersPath, path.join(candidatePath, "_headers"));
  await writeFile(path.join(candidatePath, "release-meta.json"), `${JSON.stringify(metadata, null, 2)}\n`, "utf8");
  const contentPath = path.join(candidatePath, "content");
  await mkdir(contentPath);
  await Promise.all(contentFiles.map((file) =>
    writeFile(path.join(contentPath, file.fileName), file.contentBytes)));
  await writeFile(path.join(contentPath, "content-manifest.json"), `${JSON.stringify(contentManifest, null, 2)}\n`, "utf8");

  await verifyCandidate(candidatePath, metadata, contentManifest);
  const sourceStatusAfter = runGit(["status", "--porcelain=v1", "--untracked-files=all"]);
  if (sourceStatusAfter !== sourceStatusBefore) {
    fail("Release assembly changed the source working tree");
  }

  console.log(`ReleaseCandidate: ${candidatePath}`);
  console.log(`Build ID: ${buildId}`);
  console.log(`Content version: ${contentVersion}`);
  console.log(`Snapshot ID: ${snapshotId}`);
  console.log(`Game version: ${gameVersionId}`);
  console.log(`Package fingerprint: ${contentFingerprint}`);
  console.log(`Source: ${sourceCommit}${sourceDirty ? " (dirty)" : ""}`);
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
