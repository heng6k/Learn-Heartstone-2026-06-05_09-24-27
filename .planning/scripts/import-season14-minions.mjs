import fs from "node:fs";
import path from "node:path";

const root = process.cwd();
const minionsPath = path.join(root, "Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json");
const patchPath = "C:/tmp/hsbg-patch-36.2.json";
const batchPath = index => `C:/tmp/hsbg-s14-full-${index}.json`;
const imageDirectory = path.join(root, "Assets/LearnHearthstone/Resources/CardImages/Minions/Season14");
const imageResourcePrefix = "CardImages/Minions/Season14/";

const namesByResearchKey = {
  "ACT-R01N": "Kelp Keeper",
  "ACT-R02N": "Private Investigator",
  "ACT-R03N": "Soulkeeping Jailer",
  "LOCK-R02N": "Bilgewater Breakout",
  "LOCK-R03N": "Locked-up Mutineer",
  "FISH-R01N": "Fishbait",
  "FISH-R02N": "Lurking Lionfish",
  "FISH-R03N": "Snarky Shark",
  "MIN-R01": "Dead Bellringer",
  "MIN-R02": "Barrier Banshee",
  "MIN-R03": "Snazzy Phantom",
  "MIN-R04": "Fleeing Fugitive",
  "MIN-R05": "Cagey Conjurer",
  "MIN-R06": "Torrential Ruiner",
  "MIN-R07": "Sly Infiltrator",
  "MIN-R08": "Bramble Tunneler",
  "MIN-R09": "Snare Trapper",
  "MIN-R10": "Vigilant Bristlemane",
  "MIN-R11": "Jailbird Juggernaut",
  "MIN-R12": "Veteran Brigand",
  "MIN-R13": "Living Prison",
  "MIN-R14": "Air Baller",
  "MIN-R15": "Moat Custodian",
  "MIN-R16": "Unbound Tempest",
  "MIN-R17": "Clever Castaway",
  "MIN-R18": "Treasure Parrot",
  "MIN-R19": "Enterprising Escapee",
  "MIN-R20": "Maritime Extortionist",
  "MIN-R21": "Captain Cookie",
  "MIN-R22": "Silent Deliverer",
  "MIN-R23": "Hooktusk, Master Marauder",
  "MIN-R24": "Rescue Bot",
  "MIN-R25": "Drone Duplicator",
  "MIN-R26": "Spark Snapper",
  "MIN-R27": "Gearfin",
  "MIN-R28": "Glambot",
  "MIN-R29": "Flittering Bat",
  "MIN-R30": "Tasty Lobster",
  "MIN-R31": "Wolf Pup",
  "MIN-R32": "Headhunter Gryphon",
  "MIN-R33": "Cage Gnawer",
  "MIN-R34": "Hoarding Hyena",
  "MIN-R35": "Deathstrider",
  "MIN-R36": "Ravaging Scorpid",
  "MIN-R37": "Trapped Clapper",
  "MIN-R38": "Devilish Distractor",
  "MIN-R39": "Imp-lusionist",
  "MIN-R40": "Deft Deserter",
  "MIN-R41": "Eredar Escapist",
  "MIN-R42": "Breakout Mastermind",
  "MIN-R43": "Twilight Tidehunter",
  "MIN-R44": "Shamanic Tidecaller",
  "MIN-R45": "Hired Mount",
  "MIN-R46": "Bronze Timewalker",
  "MIN-R47": "Sky-hatch Runaway",
  "MIN-R48": "Runic Arcanist",
  "MIN-R49": "Crimson Vindicator",
  "MIN-R50": "Suspicious Prisonguard",
  "MIN-R51": "Decoy Conjurer",
  "MIN-R52": "Fruit Vendor",
  "MIN-R53": "Boom-in-a-Box",
  "MIN-R54": "Gatekeeper Amalgam",
  "MIN-R55": "Tyrael"
};

const officialStableIdsByResearchKey = {
  "ACT-R01N": "SHAMAN_BG36_701",
  "ACT-R02N": "ROGUE_BG36_509",
  "ACT-R03N": "WARLOCK_BG36_503",
  "LOCK-R02N": "ROGUE_BG36_520",
  "LOCK-R03N": "ROGUE_BG36_521",
  "FISH-R01N": "HUNTER_BG36_205",
  "FISH-R02N": "HUNTER_BG36_201",
  "FISH-R03N": "HUNTER_BG36_206",
  "MIN-R14": "MAGE_BG36_181",
  "MIN-R28": "PALADIN_BG36_853",
  "MIN-R38": "WARLOCK_BG36_762"
};

const officialKeywordNames = new Map([
  ["Aura", "Aura"],
  ["Battlecry", "Battlecry"],
  ["Choose One", "ChooseOne"],
  ["Deathrattle", "Deathrattle"],
  ["Discover", "Discover"],
  ["Divine Shield", "DivineShield"],
  ["End of Turn", "EndOfTurn"],
  ["Rally", "Rally"],
  ["Refresh", "Refresh"],
  ["Start of Combat", "StartOfCombat"],
  ["Taunt", "Taunt"]
]);

function readJson(file) {
  return JSON.parse(fs.readFileSync(file, "utf8"));
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

function loadApiCards() {
  const cards = [];
  for (let index = 1; index <= 4; index += 1) {
    cards.push(...readJson(batchPath(index)).data);
  }
  return new Map(cards.map(card => [card.name, card]));
}

function loadPatchCards() {
  const sections = readJson(patchPath).data.sections;
  const cards = sections
    .flatMap(section => section.cards ?? [])
    .filter(card => card.changeType === "added" && card.cardType === "minion");
  return new Map(cards.map(card => [card.name, card.newCard]));
}

function validateDefinition(definition, api) {
  if (!definition.golden) {
    throw new Error(`${definition.researchKey} has no golden definition.`);
  }
  if (!api.externalId || !api.dbfIdGold || !api.image) {
    throw new Error(`${definition.researchKey} is missing a live identity or image.`);
  }
}

function describeLiveDifference(definition, patchCard) {
  const expected = [definition.tavernTier, definition.attack, definition.health].join("/");
  const actual = [patchCard.tier, patchCard.attack, patchCard.health].join("/");
  const expectedGolden = [definition.golden.attack, definition.golden.health].join("/");
  const actualGolden = [patchCard.attackGold, patchCard.healthGold].join("/");
  return expected === actual && expectedGolden === actualGolden
    ? null
    : `${definition.researchKey} ${definition.name}: ${expected} (${expectedGolden}) -> ${actual} (${actualGolden})`;
}

function replaceStableIds(replacements) {
  const roots = [path.join(root, "Assets"), path.join(root, ".planning")];
  const extensions = new Set([".cs", ".json", ".md"]);
  const visit = directory => {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      const file = path.join(directory, entry.name);
      if (entry.isDirectory()) {
        visit(file);
      } else if (extensions.has(path.extname(entry.name).toLowerCase()) && file !== minionsPath) {
        let text = fs.readFileSync(file, "utf8");
        const original = text;
        for (const [oldId, newId] of replacements) {
          text = text.split(oldId).join(newId);
          text = text.split(oldId.toLowerCase()).join(newId.toLowerCase());
        }
        for (const prefix of ["SHAMAN", "ROGUE", "WARLOCK", "HUNTER", "MAGE", "PALADIN"]) {
          text = text.split(`${prefix}_${prefix}_BG36`).join(`${prefix}_BG36`);
          text = text.split(`${prefix.toLowerCase()}_${prefix.toLowerCase()}_bg36`).join(`${prefix.toLowerCase()}_bg36`);
        }
        if (text !== original) {
          fs.writeFileSync(file, text, "utf8");
        }
      }
    }
  };
  roots.forEach(visit);
}

function updateCarrierTestExpectations() {
  const directory = path.join(root, "Assets/LearnHearthstone/Tests/EditMode/Mechanics");
  for (const name of fs.readdirSync(directory)) {
    if (!/^Season14.*CarrierTests\.cs$/.test(name)) {
      continue;
    }
    const file = path.join(directory, name);
    const source = fs.readFileSync(file, "utf8");
    const updated = source.replaceAll('Assert.AreEqual("Partial",', 'Assert.AreEqual("Implemented",');
    if (updated !== source) {
      fs.writeFileSync(file, updated, "utf8");
    }
  }
}

function updateData() {
  const raw = fs.readFileSync(minionsPath, "utf8");
  const eol = raw.includes("\r\n") ? "\r\n" : "\n";
  const payload = JSON.parse(raw);
  const apiCards = loadApiCards();
  const patchCards = loadPatchCards();
  const replacements = [];
  const liveDifferences = [];
  const poolCountsByTier = new Map([[1, 15], [2, 15], [3, 13], [4, 11], [5, 9], [6, 7], [7, 5]]);

  for (const [researchKey, englishName] of Object.entries(namesByResearchKey)) {
    const definition = payload.minions.find(item => item.researchKey === researchKey);
    const api = apiCards.get(englishName);
    const patchCard = patchCards.get(englishName);
    if (!definition || !api || !patchCard) {
      throw new Error(`Missing import input for ${researchKey} (${englishName}).`);
    }
    validateDefinition(definition, api);
    const difference = describeLiveDifference(definition, patchCard);
    if (difference) {
      liveDifferences.push(difference);
    }

    const oldCardId = definition.cardId;
    const oldGoldenCardId = definition.golden.cardId;
    const cardId = officialStableIdsByResearchKey[researchKey] ?? `preview-s14-minion-${researchKey.toLowerCase()}`;
    const goldenCardId = officialStableIdsByResearchKey[researchKey] ? `${cardId}_G` : `${cardId}-g`;
    if (oldCardId !== cardId) {
      replacements.push([oldCardId, cardId]);
    }
    if (oldGoldenCardId !== goldenCardId) {
      replacements.push([oldGoldenCardId, goldenCardId]);
    }

    definition.id = cardId.toLowerCase();
    definition.cardId = cardId;
    definition.revisionId = `${cardId}@36.2-preview-v1`;
    definition.implementationStatus = "Implemented";
    definition.dbfId = api.id;
    definition.englishName = api.name;
    definition.englishText = stripMarkup(api.text);
    definition.tavernTier = patchCard.tier;
    definition.attack = patchCard.attack;
    definition.health = patchCard.health;
    definition.poolCount = poolCountsByTier.get(patchCard.tier) ?? definition.poolCount;
    definition.imagePath = `${imageResourcePrefix}${api.externalId}`;
    const liveImageSource = `HSBG live-client export https://hsbg.cards${api.image}`;
    const priorImageSources = String(definition.imageSource ?? "")
      .split("; ")
      .filter(source => source && source !== liveImageSource);
    definition.imageSource = [liveImageSource, ...new Set(priorImageSources)].join("; ");
    definition.effectIds = [];
    definition.officialKeywords = (api.keywords ?? [])
      .map(keyword => officialKeywordNames.get(keyword))
      .filter(Boolean);
    definition.goldenDbfId = api.dbfIdGold;
    definition.golden.cardId = goldenCardId;
    definition.golden.dbfId = api.dbfIdGold;
    definition.golden.attack = patchCard.attackGold;
    definition.golden.health = patchCard.healthGold;
    definition.golden.englishText = stripMarkup(api.textGold);
    definition.golden.officialKeywords = [...definition.officialKeywords];
  }

  const serialized = `${JSON.stringify(payload, null, 2)}\n`.replace(/\n/g, eol);
  fs.writeFileSync(minionsPath, serialized, "utf8");
  replaceStableIds(replacements);
  updateCarrierTestExpectations();
  console.log(`Updated ${Object.keys(namesByResearchKey).length} Season 14 minions and ${replacements.length} stable IDs.`);
  if (liveDifferences.length > 0) {
    console.log(`Applied ${liveDifferences.length} live stat corrections:\n${liveDifferences.join("\n")}`);
  }
}

async function downloadImages() {
  const apiCards = loadApiCards();
  const cards = Object.values(namesByResearchKey).map(name => apiCards.get(name));
  fs.mkdirSync(imageDirectory, { recursive: true });
  let cursor = 0;
  const workers = Array.from({ length: 8 }, async () => {
    while (cursor < cards.length) {
      const card = cards[cursor];
      cursor += 1;
      const destination = path.join(imageDirectory, `${card.externalId}.png`);
      const response = await fetch(`https://hsbg.cards${card.image}`);
      if (!response.ok) {
        throw new Error(`Image download failed for ${card.name}: HTTP ${response.status}`);
      }
      fs.writeFileSync(destination, Buffer.from(await response.arrayBuffer()));
    }
  });
  await Promise.all(workers);
  console.log(`Downloaded ${cards.length} Season 14 minion images.`);
}

if (process.argv.includes("--update-data")) {
  updateData();
}
if (process.argv.includes("--download-images")) {
  await downloadImages();
}
if (!process.argv.includes("--update-data") && !process.argv.includes("--download-images")) {
  throw new Error("Use --update-data and/or --download-images.");
}
