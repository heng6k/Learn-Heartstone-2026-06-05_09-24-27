import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");
const localPath = path.join(root, "Assets", "LearnHearthstone", "Resources", "Data", "battlegroundsMinions.json");
const officialUrl = "https://hearthstone.blizzard.com/en-us/api/cards?locale=en_US&gameMode=battlegrounds&pageSize=500&bgCardType=minion";
const hsJsonUrl = "https://api.hearthstonejson.com/v1/latest/enUS/cards.json";

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

const localKeywordMap = {
  Aura: "Aura",
  Avenge: "Avenge",
  Battlecry: "Battlecry",
  BloodGem: "BloodGem",
  Bounty: "Bounty",
  ChooseOne: "ChooseOne",
  Cleave: "Cleave",
  Deathrattle: "Deathrattle",
  Devour: "Devour",
  Discover: "Discover",
  DivineShield: "DivineShield",
  EndOfTurn: "EndOfTurn",
  HiddenDeathrattle: "HiddenDeathrattle",
  Magnetic: "Magnetic",
  Pass: "Pass",
  Poisonous: "Poisonous",
  Rally: "Rally",
  Reborn: "Reborn",
  Refresh: "Refresh",
  Spellcraft: "Spellcraft",
  StartOfCombat: "StartOfCombat",
  Stealth: "Stealth",
  Taunt: "Taunt",
  TavernSpell: "TavernSpell",
  Trigger: null,
  Venomous: "Venomous",
  Windfury: "Windfury",
  "光环": "Aura",
  "复仇": "Avenge",
  "战吼": "Battlecry",
  "鲜血宝石": "BloodGem",
  "悬赏": "Bounty",
  "抉择": "ChooseOne",
  "顺劈": "Cleave",
  "亡语": "Deathrattle",
  "吞食": "Devour",
  "发现": "Discover",
  "圣盾": "DivineShield",
  "回合结束时": "EndOfTurn",
  "隐藏亡语": "HiddenDeathrattle",
  "磁力": "Magnetic",
  "传递": "Pass",
  "剧毒": "Poisonous",
  "进击": "Rally",
  "复生": "Reborn",
  "刷新": "Refresh",
  "塑造法术": "Spellcraft",
  "战斗开始时": "StartOfCombat",
  "潜行": "Stealth",
  "嘲讽": "Taunt",
  "酒馆法术": "TavernSpell",
  "触发效果": null,
  "烈毒": "Venomous",
  "风怒": "Windfury"
};

const localPayload = JSON.parse(await fs.readFile(localPath, "utf8"));
const localMinions = (localPayload.minions ?? [])
  .filter((minion) => minion.inPool !== 0 && minion.tavernTier >= 1 && minion.tavernTier <= 7);
const localSolo = localMinions.filter((minion) => !String(minion.cardId).startsWith("BGDUO"));
const localDuos = localMinions.filter((minion) => String(minion.cardId).startsWith("BGDUO"));

const [officialPayload, hsJsonPayload] = await Promise.all([
  fetchJson(officialUrl, "Official Battlegrounds minion API"),
  fetchJson(hsJsonUrl, "HearthstoneJSON card id mirror")
]);

const hsJsonByDbfId = new Map(hsJsonPayload.map((card) => [Number(card.dbfId), card]));
const officialSolo = (officialPayload.cards ?? [])
  .filter((card) =>
    card.battlegrounds &&
    card.battlegrounds.tier >= 1 &&
    card.battlegrounds.tier <= 7 &&
    card.battlegrounds.hero !== true &&
    card.battlegrounds.quest !== true &&
    card.battlegrounds.reward !== true &&
    card.battlegrounds.duosOnly !== true &&
    card.battlegrounds.isDuosOnly !== true)
  .sort((a, b) => Number(a.id) - Number(b.id));

const localByDbfId = new Map(localSolo.map((minion) => [Number(minion.dbfId), minion]));
const officialIds = new Set(officialSolo.map((card) => Number(card.id)));
const unknownKeywordIds = new Set();
const missing = officialSolo.filter((card) => !localByDbfId.has(Number(card.id)));
const unexpected = localSolo.filter((minion) => !officialIds.has(Number(minion.dbfId)));
const statMismatches = officialSolo.flatMap((card) => {
  const local = localByDbfId.get(Number(card.id));
  if (!local) {
    return [];
  }

  const diffs = [];
  if (Number(local.tavernTier) !== Number(card.battlegrounds.tier)) {
    diffs.push(`tier local=${local.tavernTier} official=${card.battlegrounds.tier}`);
  }

  if (Number(local.attack) !== Number(card.attack)) {
    diffs.push(`attack local=${local.attack} official=${card.attack}`);
  }

  if (Number(local.health) !== Number(card.health)) {
    diffs.push(`health local=${local.health} official=${card.health}`);
  }

  return diffs.length === 0 ? [] : [formatCard(card, local) + " " + diffs.join("; ")];
});

const keywordMismatches = officialSolo.flatMap((card) => {
  const local = localByDbfId.get(Number(card.id));
  if (!local) {
    return [];
  }

  const officialKeywords = normalizeOfficialKeywordIds(card.keywordIds, unknownKeywordIds);
  const localKeywords = normalizeLocalKeywords(local.officialKeywords);
  const missingKeywords = officialKeywords.filter((keyword) => !localKeywords.includes(keyword));
  const extraKeywords = localKeywords.filter((keyword) => !officialKeywords.includes(keyword));
  if (missingKeywords.length === 0 && extraKeywords.length === 0) {
    return [];
  }

  return [
    `${formatCard(card, local)} officialKeywords local=[${localKeywords.join(",")}] official=[${officialKeywords.join(",")}]`
  ];
});

console.log(`official_solo_minions=${officialSolo.length}`);
console.log(`local_solo_minions=${localSolo.length}`);
console.log(`local_duos_out_of_scope=${localDuos.length}`);
console.log(`missing_official=${missing.length}${missing.length ? ` ${missing.map((card) => formatCard(card)).join(" | ")}` : ""}`);
console.log(`unexpected_local=${unexpected.length}${unexpected.length ? ` ${unexpected.map((minion) => formatLocal(minion)).join(" | ")}` : ""}`);
console.log(`stat_mismatches=${statMismatches.length}${statMismatches.length ? ` ${statMismatches.join(" | ")}` : ""}`);
console.log(`keyword_mismatches=${keywordMismatches.length}${keywordMismatches.length ? ` ${keywordMismatches.slice(0, 20).join(" | ")}` : ""}`);
if (keywordMismatches.length > 20) {
  console.log(`keyword_mismatches_truncated=${keywordMismatches.length - 20}`);
}

if (unknownKeywordIds.size > 0) {
  console.log(`unknown_keyword_ids=${Array.from(unknownKeywordIds).sort((a, b) => a - b).join(",")}`);
}

if (missing.length > 0 || unexpected.length > 0 || statMismatches.length > 0 || keywordMismatches.length > 0 || unknownKeywordIds.size > 0) {
  process.exitCode = 1;
}

async function fetchJson(url, label) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`${label} request failed: ${response.status} ${response.statusText}`);
  }

  return response.json();
}

function formatCard(card, local = null) {
  const hsJson = hsJsonByDbfId.get(Number(card.id));
  const cardId = local?.cardId ?? hsJson?.id ?? card.slug ?? "unknown";
  return `${card.id}:${cardId}:${card.name}:T${card.battlegrounds?.tier ?? "?"}`;
}

function formatLocal(minion) {
  return `${minion.dbfId}:${minion.cardId}:${minion.name}:T${minion.tavernTier}`;
}

function normalizeOfficialKeywordIds(keywordIds = [], unknownKeywordIds) {
  return Array.from(new Set((keywordIds ?? []).map((id) => {
    const mapped = officialKeywordIdMap.get(Number(id));
    if (!mapped) {
      unknownKeywordIds.add(Number(id));
      return null;
    }

    return mapped;
  }).filter(Boolean)))
    .sort();
}

function normalizeLocalKeywords(keywords = []) {
  return Array.from(new Set((keywords ?? [])
    .map((keyword) => localKeywordMap[keyword] ?? keyword)
    .filter(Boolean)))
    .sort();
}
