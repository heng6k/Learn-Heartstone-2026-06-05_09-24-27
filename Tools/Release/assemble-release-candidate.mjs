#!/usr/bin/env node

import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { cp, mkdir, readFile, readdir, stat, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { TextDecoder } from "node:util";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, "..", "..");
const releaseRoot = path.join(repositoryRoot, "Builds", "ReleaseCandidate");
const vercelConfigPath = path.join(repositoryRoot, "Deploy", "Vercel", "vercel.json");
const minionSourcePath = path.join(repositoryRoot, "Assets", "LearnHearthstone", "Resources", "Data", "battlegroundsMinions.json");
const legacyRootFiles = new Set(["release-meta.txt", "release-meta.json", "vercel.json"]);
const strictUtf8 = new TextDecoder("utf-8", { fatal: true });
const maxContentBytes = 16 * 1024 * 1024;

const usage = `Usage:
  node Tools/Release/assemble-release-candidate.mjs --webgl <build-directory> --content-version <version> [--output <candidate-directory>]

The output must stay under Builds/ReleaseCandidate. The command is offline and never deploys.`;

function fail(message) {
  throw new Error(message);
}

function parseArguments(argv) {
  const result = {};
  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index];
    if (argument === "--help" || argument === "-h") {
      result.help = true;
      continue;
    }
    if (argument !== "--webgl" && argument !== "--content-version" && argument !== "--output") {
      fail(`Unknown argument: ${argument}`);
    }
    const value = argv[index + 1];
    if (!value || value.startsWith("--")) {
      fail(`Missing value for ${argument}`);
    }
    result[argument === "--content-version" ? "contentVersion" : argument.slice(2)] = value;
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
  if (!/^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$/.test(value) || value.includes("..")) {
    fail(`Unsafe content version: ${value}`);
  }
  return value;
}

function sha256Hex(bytes) {
  return createHash("sha256").update(bytes).digest("hex");
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

async function validateWebGLSite(sitePath) {
  for (const requiredPath of ["index.html", "Build", "TemplateData"]) {
    if (!(await pathExists(path.join(sitePath, requiredPath)))) {
      fail(`WebGL site is missing ${requiredPath}: ${sitePath}`);
    }
  }

  const buildFiles = await readdir(path.join(sitePath, "Build"));
  for (const suffix of [".loader.js", ".data.br", ".framework.js.br", ".wasm.br"]) {
    const matches = buildFiles.filter((file) => file.endsWith(suffix));
    if (matches.length !== 1) {
      fail(`Expected exactly one Build/*${suffix}, found ${matches.length}`);
    }
  }

  const indexHtml = await readFile(path.join(sitePath, "index.html"), "utf8");
  for (const buildFile of buildFiles.filter((file) => /\.(loader\.js|data\.br|framework\.js\.br|wasm\.br)$/.test(file))) {
    if (!indexHtml.includes(buildFile)) {
      fail(`index.html does not reference Build/${buildFile}`);
    }
  }
}

async function verifyCandidate(candidatePath, expectedMetadata, expectedManifest) {
  await validateWebGLSite(candidatePath);

  for (const requiredPath of ["vercel.json", "release-meta.json"]) {
    if (!(await pathExists(path.join(candidatePath, requiredPath)))) {
      fail(`ReleaseCandidate is missing ${requiredPath}`);
    }
  }
  if (await pathExists(path.join(candidatePath, "release-meta.txt"))) {
    fail("ReleaseCandidate contains legacy release-meta.txt");
  }

  const canonicalConfig = await readFile(vercelConfigPath, "utf8");
  const candidateConfig = await readFile(path.join(candidatePath, "vercel.json"), "utf8");
  JSON.parse(candidateConfig);
  if (candidateConfig !== canonicalConfig) {
    fail("ReleaseCandidate vercel.json does not match Deploy/Vercel/vercel.json");
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
  const contentEntries = (await readdir(contentPath)).sort();
  const expectedEntries = ["content-manifest.json", expectedManifest.minions.fileName].sort();
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

  const contentBytes = await readFile(path.join(contentPath, manifest.minions.fileName));
  if (contentBytes.byteLength !== manifest.minions.bytes) {
    fail("ReleaseCandidate content byte count does not match its manifest");
  }
  if (sha256Hex(contentBytes) !== manifest.minions.sha256) {
    fail("ReleaseCandidate content SHA-256 does not match its manifest");
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
  await validateWebGLSite(sourcePath);
  const contentVersion = validateContentVersion(args.contentVersion);

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

  const metadata = {
    schemaVersion: 1,
    clientVersion,
    contentVersion,
    buildId,
    sourceCommit,
    sourceDirty,
    unityVersion,
    builtAtUtc: builtAt.toISOString(),
  };
  const minionBytes = await readFile(minionSourcePath);
  if (minionBytes.byteLength === 0 || minionBytes.byteLength > maxContentBytes) {
    fail("Minion content source size is outside the supported range");
  }
  if (minionBytes[0] === 0xef && minionBytes[1] === 0xbb && minionBytes[2] === 0xbf) {
    fail("Minion content source must not contain a UTF-8 BOM");
  }
  let minionPayload;
  try {
    minionPayload = JSON.parse(strictUtf8.decode(minionBytes));
  } catch (error) {
    fail(`Invalid UTF-8 or JSON in ${minionSourcePath}: ${error.message}`);
  }
  if (!Array.isArray(minionPayload.minions)) {
    fail("Minion content source is missing its minions array");
  }

  const minionFileName = `battlegroundsMinions.v${contentVersion}.json`;
  const contentManifest = {
    protocolVersion: 1,
    contentVersion,
    requiredClientVersion: clientVersion,
    generatedAtUtc: builtAt.toISOString(),
    minions: {
      fileName: minionFileName,
      bytes: minionBytes.byteLength,
      sha256: sha256Hex(minionBytes),
    },
  };

  await mkdir(releaseRoot, { recursive: true });
  await cp(sourcePath, candidatePath, {
    recursive: true,
    filter(source) {
      const relative = path.relative(sourcePath, source).replaceAll("\\", "/");
      return relative === "" || !legacyRootFiles.has(relative);
    },
  });
  await cp(vercelConfigPath, path.join(candidatePath, "vercel.json"));
  await writeFile(path.join(candidatePath, "release-meta.json"), `${JSON.stringify(metadata, null, 2)}\n`, "utf8");
  const contentPath = path.join(candidatePath, "content");
  await mkdir(contentPath);
  await writeFile(path.join(contentPath, minionFileName), minionBytes);
  await writeFile(path.join(contentPath, "content-manifest.json"), `${JSON.stringify(contentManifest, null, 2)}\n`, "utf8");

  await verifyCandidate(candidatePath, metadata, contentManifest);
  const sourceStatusAfter = runGit(["status", "--porcelain=v1", "--untracked-files=all"]);
  if (sourceStatusAfter !== sourceStatusBefore) {
    fail("Release assembly changed the source working tree");
  }

  console.log(`ReleaseCandidate: ${candidatePath}`);
  console.log(`Build ID: ${buildId}`);
  console.log(`Content version: ${contentVersion}`);
  console.log(`Source: ${sourceCommit}${sourceDirty ? " (dirty)" : ""}`);
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
