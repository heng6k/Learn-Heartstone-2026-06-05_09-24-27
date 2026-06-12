import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");
const localPath = path.join(root, "Assets", "LearnHearthstone", "Resources", "Data", "battlegroundsMinions.json");
const officialUrl = "https://hearthstone.blizzard.com/en-us/api/cards?locale=en_US&gameMode=battlegrounds&pageSize=500&bgCardType=minion";

const officialKeywordIdMap = new Map([
  [1, "Taunt"],
  [3, "DivineShield"],
  [6, "Stealth"],
  [8, "Battlecry"],
  [11, "Windfury"],
  [12, "Deathrattle"],
  [21, "Discover"],
  [66, "Magnetic"],
  [78, "Reborn"],
  [109, "BloodGem"],
  [196, "Refresh"],
  [198, "Avenge"],
  [234, "Spellcraft"],
  [259, "Reborn"],
  [261, "Venomous"],
  [300, "Pass"],
  [360, "Rally"],
  [379, "Bounty"],
  [414, "TavernSpell"]
]);

const [localPayload, officialPayload] = await Promise.all([
  fs.readFile(localPath, "utf8").then(JSON.parse),
  fetchJson(officialUrl, "Official Battlegrounds minion API")
]);

const officialByDbfId = new Map((officialPayload.cards ?? []).map((card) => [Number(card.id), card]));
let updated = 0;
let withOfficialCard = 0;
const unknownKeywordIds = new Set();

for (const minion of localPayload.minions ?? []) {
  const official = officialByDbfId.get(Number(minion.dbfId));
  if (!official) {
    continue;
  }

  withOfficialCard += 1;
  const officialKeywords = normalizeOfficialKeywords(official.keywordIds, unknownKeywordIds);
  if (!sameArray(minion.officialKeywords ?? [], officialKeywords)) {
    minion.officialKeywords = officialKeywords;
    updated += 1;
  } else if (!Array.isArray(minion.officialKeywords)) {
    minion.officialKeywords = officialKeywords;
    updated += 1;
  }
}

if (unknownKeywordIds.size > 0) {
  throw new Error("Unknown official keyword ids: " + Array.from(unknownKeywordIds).sort((a, b) => a - b).join(","));
}

await fs.writeFile(localPath, JSON.stringify(localPayload, null, 2) + "\n", "utf8");

console.log(`official_minion_keyword_sources=${withOfficialCard}`);
console.log(`official_keywords_updated=${updated}`);

async function fetchJson(url, label) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`${label} request failed: ${response.status} ${response.statusText}`);
  }

  return response.json();
}

function normalizeOfficialKeywords(keywordIds = [], unknownKeywordIds) {
  return Array.from(new Set((keywordIds ?? []).map((id) => {
    const mapped = officialKeywordIdMap.get(Number(id));
    if (!mapped) {
      unknownKeywordIds.add(Number(id));
      return null;
    }

    return mapped;
  }).filter(Boolean)));
}

function sameArray(left, right) {
  if (left.length !== right.length) {
    return false;
  }

  for (let index = 0; index < left.length; index += 1) {
    if (left[index] !== right[index]) {
      return false;
    }
  }

  return true;
}
