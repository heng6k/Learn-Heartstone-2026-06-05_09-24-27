import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const projectRoot = path.resolve(here, "..");
const hyperframesRoot = path.join(projectRoot, "hyperframes");
const scriptPath = path.join(here, "VOICEBOX_TEXT.txt");
const segmentsDir = path.join(hyperframesRoot, "narration", "segments");
const manifestPath = path.join(hyperframesRoot, "narration", "voicebox-generation.json");
const voiceboxDataRoot = path.join(process.env.APPDATA ?? "", "sh.voicebox.app");

const profileId = "1e356649-5042-4bad-9204-d94fcb901e68";
const starts = [0.6, 5.7, 16.7, 25.7, 34.7, 44.7, 52.0];
const seed = 42917;
const engine = "chatterbox";
const modelName = "chatterbox-tts";
const activeStatuses = new Set(["loading_model", "queued", "generating"]);
const apiBase = "http://127.0.0.1:17493";
const args = new Set(process.argv.slice(2));
const onlyArg = [...args].find((arg) => arg.startsWith("--only="));
const only = onlyArg ? Number(onlyArg.split("=")[1]) : null;
const force = args.has("--force");

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function requestJson(url, options = {}) {
  const response = await fetch(url, {
    ...options,
    signal: AbortSignal.timeout(2 * 60 * 60 * 1000),
  });
  const body = await response.text();
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}: ${body}`);
  }
  return body ? JSON.parse(body) : null;
}

function sha256(filePath) {
  return crypto.createHash("sha256").update(fs.readFileSync(filePath)).digest("hex").toUpperCase();
}

function loadManifest() {
  if (!fs.existsSync(manifestPath)) {
    return {
      profile_id: profileId,
      profile_name: "曼波",
      engine,
      model_name: modelName,
      model_size: null,
      language: "zh",
      seed,
      segments: [],
    };
  }
  return JSON.parse(fs.readFileSync(manifestPath, "utf8"));
}

function saveManifest(manifest) {
  fs.mkdirSync(path.dirname(manifestPath), { recursive: true });
  fs.writeFileSync(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
}

async function generateLine(text, index) {
  const created = await requestJson(`${apiBase}/generate`, {
    method: "POST",
    headers: { "content-type": "application/json; charset=utf-8" },
    body: JSON.stringify({
      profile_id: profileId,
      text,
      language: "zh",
      seed,
      engine,
      personality: false,
      normalize: true,
    }),
  });

  process.stdout.write(`segment ${index}: queued ${created.id}\n`);
  const deadline = Date.now() + 2 * 60 * 60 * 1000;
  let item = created;
  while (activeStatuses.has(item.status) && Date.now() < deadline) {
    await sleep(2000);
    item = await requestJson(`${apiBase}/history/${created.id}`);
  }

  if (activeStatuses.has(item.status)) {
    throw new Error(`segment ${index} timed out while ${item.status}`);
  }

  if (item.status !== "completed") {
    throw new Error(`segment ${index} failed: ${item.status} ${item.error ?? ""}`.trim());
  }
  if (!item.audio_path) {
    throw new Error(`segment ${index} completed without audio_path`);
  }

  const sourcePath = path.resolve(voiceboxDataRoot, item.audio_path);
  if (!fs.existsSync(sourcePath)) {
    throw new Error(`segment ${index} source audio missing: ${sourcePath}`);
  }

  const destinationPath = path.join(segmentsDir, `${String(index).padStart(2, "0")}.wav`);
  fs.copyFileSync(sourcePath, destinationPath);
  return {
    index,
    text,
    scene_start_s: starts[index - 1],
    generation_id: item.id,
    source_audio_path: item.audio_path,
    file: path.relative(hyperframesRoot, destinationPath).replaceAll("\\", "/"),
    duration_s: item.duration,
    seed: item.seed,
    engine: item.engine,
    model_name: modelName,
    model_size: item.model_size,
    status: item.status,
    created_at: item.created_at,
    sha256: sha256(destinationPath),
  };
}

fs.mkdirSync(segmentsDir, { recursive: true });
const lines = fs
  .readFileSync(scriptPath, "utf8")
  .split(/\r?\n/)
  .map((line) => line.trim())
  .filter(Boolean);

if (lines.length !== starts.length) {
  throw new Error(`expected ${starts.length} narration lines, found ${lines.length}`);
}
if (only !== null && (!Number.isInteger(only) || only < 1 || only > lines.length)) {
  throw new Error(`--only must be between 1 and ${lines.length}`);
}

const manifest = {
  ...loadManifest(),
  profile_id: profileId,
  profile_name: "曼波",
  engine,
  model_name: modelName,
  model_size: null,
  language: "zh",
  seed,
};
for (let i = 0; i < lines.length; i += 1) {
  const index = i + 1;
  if (only !== null && index !== only) continue;

  const existing = manifest.segments.find((entry) => entry.index === index);
  const existingPath = existing ? path.join(hyperframesRoot, existing.file) : null;
  if (
    !force &&
    existing &&
    existing.engine === engine &&
    existing.model_name === modelName &&
    existingPath &&
    fs.existsSync(existingPath)
  ) {
    process.stdout.write(`segment ${index}: reused ${existing.generation_id}\n`);
    continue;
  }

  const generated = await generateLine(lines[i], index);
  if (generated.duration_s < 0.8) {
    throw new Error(`segment ${index} is abnormally short (${generated.duration_s}s)`);
  }
  manifest.segments = manifest.segments.filter((entry) => entry.index !== index);
  manifest.segments.push(generated);
  manifest.segments.sort((a, b) => a.index - b.index);
  saveManifest(manifest);
  process.stdout.write(`segment ${index}: completed ${generated.duration_s.toFixed(3)}s\n`);
}

saveManifest(manifest);
process.stdout.write(`${manifestPath}\n`);
