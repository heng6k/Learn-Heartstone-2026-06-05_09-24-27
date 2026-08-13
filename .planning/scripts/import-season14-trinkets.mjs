import fs from "node:fs";
import path from "node:path";

const root = process.cwd();
const trinketsPath = path.join(root, "Assets/LearnHearthstone/Resources/Data/battlegroundsTrinkets.json");
const localizationPath = path.join(root, "Assets/LearnHearthstone/Resources/Data/battlegroundsTrinketLocalizationZhCN.json");
const versionsPath = path.join(root, "Assets/LearnHearthstone/Resources/Data/battlegroundsGameVersions.json");
const factTablePath = path.join(root, ".planning/dark-gifts-version-design-20260729/season14-content-fact-table.zh-CN.md");
const patchPath = "C:/tmp/hsbg-patch-36.2.json";
const apiPath = "C:/tmp/hsbg-s14-trinkets.json";
const imageDirectory = path.join(root, "Assets/LearnHearthstone/Resources/CardImages/Trinkets/Season14");
const imageResourcePrefix = "CardImages/Trinkets/Season14/";
const previewVersionId = "36.2-preview";
const previewContentSetId = "content-36.2-preview-v1";
const season14NewTavernSpellIds = ["132903", "132995", "133369", "133371", "133711"];
const implementedBehaviorByResearchKey = new Map(Object.entries({
  "LT-R01": { effectFamily: "combat_attack", requires: ["rally", "free_refresh"] },
  "LT-R02": { effectFamily: "combat_attack", requires: ["beast", "lurking_lionfish"] },
  "LT-R03": { effectFamily: "tavern_spell_cast", requires: ["tavern_spell", "shop_growth"] },
  "LT-R04": { effectFamily: "minion_consumed", requires: ["demon", "bonus_keywords"] },
  "LT-R05": { effectFamily: "card_bought", requires: ["battlecry", "buy_cost"] },
  "LT-R06": { effectFamily: "battlecry_triggered", requires: ["battlecry", "edge_minions"] },
  "LT-R07": { effectFamily: "shop_refresh", requires: ["refresh", "tavern_upgrade_cost"] },
  "LT-R08": { effectFamily: "turn_start", requires: ["fire_baller", "snow_baller"] },
  "LT-R09": { effectFamily: "turn_end", requires: ["tavern_spell", "magnetic"] },
  "LT-R10": { effectFamily: "turn_end", requires: ["repair_job", "mech"] },
  "LT-R11": { effectFamily: "tavern_spell_cast", requires: ["flighty_scout", "tavern_spell"] },
  "LT-R12": { effectFamily: "targeted_spell_cast", requires: ["targeted_spell", "gold"] },
  "LT-R13": { effectFamily: "spell_cast", requires: ["naga_trinket", "atomic_replacement"] },
  "LT-R14": { effectFamily: "turn_start", requires: ["lockbox", "delayed_object"] },
  "LT-R15": { effectFamily: "turn_end", requires: ["golden_minion_played", "friendly_board"] },
  "LT-R16": { effectFamily: "turn_start", requires: ["choose_one", "hand_generation"] },
  "LT-R17": { effectFamily: "recruit_destroy", requires: ["plaguerunner", "plain_copy"] },
  "LT-R18": { effectFamily: "friendly_reborn", requires: ["reborn", "friendly_board"] },
  "LT-R19": { effectFamily: "on_equip", requires: ["dark_gift", "tier_4"] },
  "LT-R20": { effectFamily: "spellcraft", requires: ["deathrattle", "recruit_phase"] },
  "LT-R21": { effectFamily: "minion_played", requires: ["minion_played", "divine_shield"] },
  "LT-R22": { effectFamily: "targeted_spell_cast", requires: ["friendly_minion", "automatic_cast"] },
  "LT-R23": { effectFamily: "turn_start", requires: ["friendly_minion_types", "gold"] },
  "GT-R01": { effectFamily: "turn_end", requires: ["last_tavern_spell", "hand_generation"] },
  "GT-R02": { effectFamily: "on_equip", requires: ["dark_gift", "tier_7"] },
  "GT-R03": { effectFamily: "targeted_spell_cast", requires: ["tavern_spell", "turn_counter"] },
  "GT-R04": { effectFamily: "tavern_spell_cast", requires: ["friendly_minion_types", "spell_bonus"] },
  "GT-R05": { effectFamily: "turn_start", requires: ["dark_gift", "plain_copy"] },
  "GT-R06": { effectFamily: "turn_end", requires: ["rally", "recruit_phase"] },
  "GT-R07": { effectFamily: "battlecry_multiplier", requires: ["dragon", "battlecry"] },
  "GT-R08": { effectFamily: "minion_played", requires: ["murloc", "tavern_spell"] },
  "GT-R09": { effectFamily: "tavern_spell_cast", requires: ["fodder", "refresh"] },
  "GT-R10": { effectFamily: "targeted_spell_cast", requires: ["tavern_minion", "consume"] },
  "GT-R11": { effectFamily: "turn_end", requires: ["deathrattle", "recruit_phase"] },
  "GT-R12": { effectFamily: "card_bought", requires: ["mech", "tavern_spell"] },
  "GT-R13": { effectFamily: "combat_start", requires: ["mech", "magnetic", "deathrattle"] },
  "GT-R14": { effectFamily: "turn_end", requires: ["tavern_spell", "magnetic"] },
  "GT-R15": { effectFamily: "turn_end", requires: ["golden_minion_played", "friendly_board"] },
  "GT-R16": { effectFamily: "on_equip", requires: ["lockbox", "golden_minion"] },
  "GT-R17": { effectFamily: "elemental_effect", requires: ["elemental", "scaling_bonus"] },
  "GT-R18": { effectFamily: "choose_one", requires: ["choose_one", "combined_effects"] },
  "GT-R19": { effectFamily: "turn_start", requires: ["choose_one", "hand_generation"] },
  "GT-R20": { effectFamily: "combat_start", requires: ["naga", "edge_minions"] },
  "GT-R21": { effectFamily: "friendly_death", requires: ["showy_cyclist", "permanent_stats"] },
  "GT-R22": { effectFamily: "friendly_reborn", requires: ["reborn", "plain_copy"] },
  "GT-R23": { effectFamily: "friendly_death", requires: ["undead", "eternal_knight"] },
  "GT-R24": { effectFamily: "combat_start", requires: ["rally", "divine_shield"] }
}));

function readJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8"));
}

function writeJson(file, value, eol = "\n") {
  fs.writeFileSync(file, `${JSON.stringify(value, null, 2).replaceAll("\n", eol)}${eol}`, "utf8");
}

function stripMarkup(value) {
  return String(value ?? "")
    .replace(/<br\s*\/?>/gi, "\n")
    .replace(/<[^>]+>/g, "")
    .replace(/&nbsp;/g, " ")
    .replace(/&amp;/g, "&")
    .replace(/&quot;/g, "\"")
    .replace(/&#39;/g, "'")
    .trim();
}

function slug(value) {
  return String(value ?? "")
    .normalize("NFKD")
    .replace(/[^a-zA-Z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "")
    .toLowerCase();
}

function section(payload, label) {
  const result = payload.data.sections.find(item => item.label === label);
  if (!result) {
    throw new Error(`Missing patch section: ${label}`);
  }
  return result.cards ?? [];
}

function loadInputs() {
  const patch = readJson(patchPath);
  const apiPayload = readJson(apiPath);
  const apiCards = new Map((apiPayload.data ?? []).map(card => [card.id, card]));
  const newCards = [
    ...section(patch, "New Lesser Trinkets"),
    ...section(patch, "New Greater Trinkets")
  ];
  if (newCards.length !== 47 || apiCards.size !== 47) {
    throw new Error(`Expected 47 Season 14 Trinkets, got patch=${newCards.length}, api=${apiCards.size}.`);
  }
  const factRows = new Map();
  for (const line of fs.readFileSync(factTablePath, "utf8").split(/\r?\n/)) {
    if (!/^\| (LT|GT)-R\d+ \|/.test(line)) {
      continue;
    }
    const columns = line.split("|").map(value => value.trim());
    const researchKey = columns[1];
    const names = columns[3].split("／");
    const slotKind = researchKey.startsWith("LT-") ? "Lesser" : "Greater";
    factRows.set(`${slotKind}|${names[0]}`, {
      researchKey,
      englishName: names[0],
      chineseName: names.slice(1).join("／"),
      chineseText: columns[5]
    });
  }
  if (factRows.size !== 47) {
    throw new Error(`Expected 47 Trinket fact rows, got ${factRows.size}.`);
  }
  return { patch, apiCards, newCards, factRows };
}

function createDefinition(change, api, fact) {
  const stableId = `preview-s14-trinket-${api.id}`;
  const effectName = `season14_${slug(api.name)}`;
  const lesser = String(api.trinketTier).toLowerCase() === "lesser";
  const implemented = implementedBehaviorByResearchKey.get(fact.researchKey);
  return {
    id: stableId,
    cardId: stableId,
    researchKey: fact.researchKey,
    dbfId: api.id,
    name: api.name,
    slotKind: lesser ? "Lesser" : "Greater",
    cost: Math.max(0, Number(api.manaCost ?? change.newCard?.manaCost ?? 0)),
    text: stripMarkup(api.text ?? change.newCard?.text),
    mechanics: [...new Set(api.keywords ?? [])],
    referencedTags: [...new Set(api.keywords ?? [])],
    associatedRaces: [...new Set(api.minionTypes ?? [])],
    relatedDbfId: (api.childIds ?? [])[0] ?? 0,
    tags: ["trinket", lesser ? "lesser_trinket" : "greater_trinket", "season14", "36.2-preview"],
    effectIds: [effectName],
    implementationStatus: implemented ? "Implemented" : "Planned",
    notes: `36.2 live-client definition; external identity ${api.externalId} remains community-crosschecked until Blizzard API publishes BG36.`,
    imagePath: `${imageResourcePrefix}${api.externalId}`,
    imageUrl: `https://hsbg.cards${api.image}`,
    offerPoolStatus: "Disabled",
    powerLevel: implemented ? "Medium" : "Pending",
    effectFamily: implemented?.effectFamily ?? "season14_pending",
    requires: implemented?.requires ?? [],
    proxyLevel: implemented ? "Exact" : "Blocked"
  };
}

function updateCatalog(newCards, apiCards, factRows) {
  const raw = fs.readFileSync(trinketsPath, "utf8");
  const eol = raw.includes("\r\n") ? "\r\n" : "\n";
  const payload = JSON.parse(raw);
  const imported = [];
  for (const change of newCards) {
    const api = apiCards.get(change.id);
    if (!api?.externalId || !api?.image || !api?.text) {
      throw new Error(`Incomplete live-client card ${change.id}.`);
    }
    const slotKind = String(api.trinketTier).toLowerCase() === "lesser" ? "Lesser" : "Greater";
    const fact = factRows.get(`${slotKind}|${api.name}`);
    if (!fact) {
      throw new Error(`Missing fact-table row for ${slotKind} ${api.name}.`);
    }
    const definition = createDefinition(change, api, fact);
    const index = payload.trinkets.findIndex(item => item.dbfId === change.id || item.cardId === definition.cardId);
    if (index >= 0) {
      const current = payload.trinkets[index];
      definition.offerPoolStatus = current.offerPoolStatus ?? definition.offerPoolStatus;
      if (!implementedBehaviorByResearchKey.has(fact.researchKey)) {
        definition.implementationStatus = current.implementationStatus ?? definition.implementationStatus;
        definition.powerLevel = current.powerLevel ?? definition.powerLevel;
        definition.effectFamily = current.effectFamily ?? definition.effectFamily;
        definition.requires = current.requires ?? definition.requires;
        definition.proxyLevel = current.proxyLevel ?? definition.proxyLevel;
      }
      definition.notes = current.notes ?? definition.notes;
      definition.effectIds = current.effectIds?.length ? current.effectIds : definition.effectIds;
      payload.trinkets[index] = definition;
    } else {
      payload.trinkets.push(definition);
    }
    imported.push(definition);
  }
  payload.count = payload.trinkets.length;
  payload.generatedAt = "2026-08-05";
  const source = "https://hsbg.cards/api-docs (live-client export)";
  payload.sourcePages = [...new Set([...(payload.sourcePages ?? []), source])];
  payload.trinkets.sort((a, b) => a.dbfId - b.dbfId || a.cardId.localeCompare(b.cardId));
  writeJson(trinketsPath, payload, eol);
  return imported;
}

function updateLocalization(imported, factRows) {
  const raw = fs.readFileSync(localizationPath, "utf8");
  const eol = raw.includes("\r\n") ? "\r\n" : "\n";
  const payload = JSON.parse(raw);
  for (const item of imported) {
    const fact = factRows.get(`${item.slotKind}|${item.name}`);
    const localized = {
      cardId: item.cardId,
      name: fact.chineseName,
      text: fact.chineseText
    };
    const index = payload.cards.findIndex(card => card.cardId === item.cardId);
    if (index >= 0) {
      payload.cards[index] = localized;
    } else {
      payload.cards.push(localized);
    }
  }
  payload.count = payload.cards.length;
  payload.generatedAt = "2026-08-05";
  const factSource = "Season 14 fact-table community cross-check";
  if (!String(payload.source ?? "").includes(factSource)) {
    payload.source = `${payload.source}; ${factSource}`;
  }
  payload.cards.sort((a, b) => a.cardId.localeCompare(b.cardId));
  writeJson(localizationPath, payload, eol);
}

function updateVersions(imported, patch) {
  const raw = fs.readFileSync(versionsPath, "utf8");
  const eol = raw.includes("\r\n") ? "\r\n" : "\n";
  const payload = JSON.parse(raw);
  const contentSet = payload.contentSets.find(item => item.id === previewContentSetId);
  if (!contentSet) {
    throw new Error(`Missing content set ${previewContentSetId}.`);
  }

  payload.entityRevisions = (payload.entityRevisions ?? []).filter(revision =>
    revision.kind !== "Trinket" || !imported.some(item => item.cardId === revision.stableEntityId));
  const revisions = imported.map(item => ({
    kind: "Trinket",
    stableEntityId: item.cardId,
    revisionId: `${item.cardId}@36.2-preview-v1`,
    effectRevision: `trinket.${slug(item.name)}@36.2-preview-v1`,
    effectiveVersionId: previewVersionId,
    stats: `cost:${item.cost}`,
    text: item.text,
    art: item.imagePath,
    tags: [...item.tags, `source-dbf:${item.dbfId}`],
    effectIds: item.effectIds,
    englishText: item.text
  }));
  payload.entityRevisions.push(...revisions);
  contentSet.trinketRevisionIds = revisions.map(item => item.revisionId).sort();

  const catalog = readJson(trinketsPath).trinkets;
  const byDbf = new Map(catalog.map(item => [item.dbfId, item]));
  const staying = section(patch, "Staying Trinkets");
  const returning = section(patch, "Returning Trinkets");
  const poolIds = [...staying, ...returning]
    .map(change => byDbf.get(change.id))
    .map(item => item?.cardId ?? item?.id);
  const missingDbfIds = [...staying, ...returning]
    .filter(change => !byDbf.has(change.id))
    .map(change => change.id)
    .sort((a, b) => a - b);
  const expectedDuosOnly = [117932, 117933, 117934, 117936, 117937, 117938, 117939, 117941, 117942];
  if (JSON.stringify(missingDbfIds) !== JSON.stringify(expectedDuosOnly) || poolIds.filter(Boolean).length !== 195) {
    throw new Error(`Expected 195 existing Solo Trinkets plus 9 known Duos-only omissions, got ${poolIds.filter(Boolean).length}.`);
  }
  const soloPoolIds = poolIds.filter(Boolean);
  soloPoolIds.push(...imported.map(item => item.cardId));
  contentSet.poolMembership = (contentSet.poolMembership ?? [])
    .filter(item => item.kind !== "Trinket")
    .concat([...new Set(soloPoolIds)].sort().map(stableEntityId => ({ kind: "Trinket", stableEntityId })));
  for (const stableEntityId of season14NewTavernSpellIds) {
    if (!contentSet.poolMembership.some(item => item.kind === "TavernSpell" && item.stableEntityId === stableEntityId)) {
      contentSet.poolMembership.push({ kind: "TavernSpell", stableEntityId });
    }
  }
  writeJson(versionsPath, payload, eol);
}

async function downloadImages(imported, apiCards) {
  fs.mkdirSync(imageDirectory, { recursive: true });
  const queue = imported.map(item => apiCards.get(item.dbfId));
  let cursor = 0;
  const workers = Array.from({ length: 6 }, async () => {
    while (cursor < queue.length) {
      const api = queue[cursor++];
      const target = path.join(imageDirectory, `${api.externalId}.png`);
      const response = await fetch(`https://hsbg.cards${api.image}`);
      if (!response.ok) {
        throw new Error(`Image download failed ${api.id}: HTTP ${response.status}`);
      }
      const bytes = Buffer.from(await response.arrayBuffer());
      if (bytes.length < 1024) {
        throw new Error(`Image download too small ${api.id}: ${bytes.length}`);
      }
      fs.writeFileSync(target, bytes);
    }
  });
  await Promise.all(workers);
}

const { patch, apiCards, newCards, factRows } = loadInputs();
const imported = updateCatalog(newCards, apiCards, factRows);
updateLocalization(imported, factRows);
updateVersions(imported, patch);
if (process.argv.includes("--download-images")) {
  await downloadImages(imported, apiCards);
}

const slots = imported.reduce((result, item) => {
  result[item.slotKind] = (result[item.slotKind] ?? 0) + 1;
  return result;
}, {});
console.log(JSON.stringify({ imported: imported.length, slots, downloadedImages: process.argv.includes("--download-images") }, null, 2));
