import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");
const minionPath = path.join(root, "Assets", "LearnHearthstone", "Resources", "Data", "battlegroundsMinions.json");
const spellPath = path.join(root, "Assets", "LearnHearthstone", "Resources", "Data", "battlegroundsSpells.json");
const sourceUrl = "https://api.hearthstonejson.com/v1/latest/enUS/cards.json";

const [minionPayload, spellPayload, cards] = await Promise.all([
  fs.readFile(minionPath, "utf8").then(JSON.parse),
  fs.readFile(spellPath, "utf8").then(JSON.parse),
  fetchJson(sourceUrl, "HearthstoneJSON enUS cards")
]);

const byCardId = new Map(cards.filter((card) => card?.id).map((card) => [card.id, card]));
const byDbfId = new Map();
for (const card of cards) {
  const dbfId = Number(card?.dbfId);
  if (!Number.isFinite(dbfId)) {
    continue;
  }

  if (!byDbfId.has(dbfId) || isBetterBattlegroundsRecord(card, byDbfId.get(dbfId))) {
    byDbfId.set(dbfId, card);
  }
}

const missing = [];
let minionsUpdated = 0;
let goldenMinionsUpdated = 0;
let spellsUpdated = 0;

for (const minion of minionPayload.minions ?? []) {
  const normal = byCardId.get(minion.cardId);
  if (!normal || !hasEnglishText(normal)) {
    missing.push(`minion:${minion.cardId}`);
    continue;
  }

  minion.englishName = normal.name.trim();
  minion.englishText = normalizeText(normal.text);
  minionsUpdated += 1;

  const goldenCardId = minion.golden?.cardId;
  const golden = goldenCardId ? byCardId.get(goldenCardId) : null;
  if (!golden || !hasEnglishText(golden)) {
    missing.push(`golden:${goldenCardId ?? minion.cardId}`);
    continue;
  }

  minion.golden.englishText = normalizeText(golden.text);
  goldenMinionsUpdated += 1;
}

for (const spell of spellPayload.spells ?? []) {
  const card = byDbfId.get(Number(spell.cardNumber));
  if (!card || !hasEnglishText(card)) {
    missing.push(`spell:${spell.cardNumber}`);
    continue;
  }

  spell.englishName = card.name.trim();
  spell.englishText = normalizeText(card.text);
  spellsUpdated += 1;
}

if (missing.length > 0) {
  throw new Error(`Missing English card records (${missing.length}): ${missing.join(", ")}`);
}

await Promise.all([
  fs.writeFile(minionPath, JSON.stringify(minionPayload, null, 2) + "\n", "utf8"),
  fs.writeFile(spellPath, JSON.stringify(spellPayload, null, 2) + "\n", "utf8")
]);

console.log(`minions_updated=${minionsUpdated}`);
console.log(`golden_minions_updated=${goldenMinionsUpdated}`);
console.log(`spells_updated=${spellsUpdated}`);

async function fetchJson(url, label) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`${label} request failed: ${response.status} ${response.statusText}`);
  }

  return response.json();
}

function hasEnglishText(card) {
  return typeof card?.name === "string" && card.name.trim().length > 0 &&
    typeof card?.text === "string" && normalizeText(card.text).length > 0;
}

function normalizeText(value) {
  return String(value ?? "")
    .replace(/\[x\]/g, "")
    .replace(/\r\n/g, "\n")
    .replace(/(<\/i>)\d+(?=[A-Z]).*$/s, "$1")
    .trim();
}

function isBetterBattlegroundsRecord(candidate, current) {
  const candidateScore = battlegroundsScore(candidate);
  const currentScore = battlegroundsScore(current);
  return candidateScore > currentScore;
}

function battlegroundsScore(card) {
  let score = 0;
  if (card?.set === "BATTLEGROUNDS") score += 2;
  if (card?.battlegrounds) score += 1;
  return score;
}
