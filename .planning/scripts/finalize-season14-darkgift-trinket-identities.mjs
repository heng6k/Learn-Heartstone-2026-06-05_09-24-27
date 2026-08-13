import fs from "node:fs";
import path from "node:path";

const checkOnly = process.argv.includes("--check");
const root = process.cwd();
const files = {
  gifts: "Assets/LearnHearthstone/Resources/Data/battlegroundsDarkGifts.json",
  giftZh: "Assets/LearnHearthstone/Resources/Data/battlegroundsDarkGiftLocalizationZhCN.json",
  trinkets: "Assets/LearnHearthstone/Resources/Data/battlegroundsTrinkets.json",
  trinketZh: "Assets/LearnHearthstone/Resources/Data/battlegroundsTrinketLocalizationZhCN.json",
  versions: "Assets/LearnHearthstone/Resources/Data/battlegroundsGameVersions.json"
};

const darkGiftIdentity = {
  "DG-R01": ["BG36_MidGameEffect_000t62", 133310],
  "DG-R02": ["BG36_MidGameEffect_000t13", 132279],
  "DG-R03": ["BG36_MidGameEffect_000t16", 132443],
  "DG-R04": ["BG36_MidGameEffect_000t73", 133421],
  "DG-R05": ["BG36_MidGameEffect_000t82", 133860],
  "DG-R06": ["BG36_MidGameEffect_000t74", 133423],
  "DG-R07": ["BG36_MidGameEffect_000t75", 133424],
  "DG-R08": ["BG36_MidGameEffect_000t51", 132733],
  "DG-R09": ["BG36_MidGameEffect_000t21", 132448],
  "DG-R10": ["BG36_MidGameEffect_000t79", 133472],
  "DG-R11": ["BG36_MidGameEffect_000t80", 133474],
  "DG-R12": ["BG36_MidGameEffect_000t52", 132734],
  "DG-R13": ["BG36_MidGameEffect_000t18", 132445],
  "DG-R14": ["BG36_MidGameEffect_000t28t", 133476],
  "DG-R15": ["BG36_MidGameEffect_000t29t", 133478],
  "DG-R16": ["BG36_MidGameEffect_000t30t", 133480],
  "DG-R17": ["BG36_MidGameEffect_000t14", 132441],
  "DG-R18": ["BG36_MidGameEffect_000t11", 132485],
  "DG-R19": ["BG36_MidGameEffect_000t15", 132442],
  "DG-R20": ["BG36_MidGameEffect_000t22", 132790],
  "DG-R21": ["BG36_MidGameEffect_000t66", 133351],
  "DG-R22": ["BG36_MidGameEffect_000t65", 133344],
  "DG-R23": ["BG36_MidGameEffect_000t5", 132203],
  "DG-R24": ["BG36_MidGameEffect_000t50", 132732],
  "DG-R25": ["BG36_MidGameEffect_000t64", 133361],
  "DG-R26": ["BG36_MidGameEffect_000t4", 132202],
  "DG-R27": ["BG36_MidGameEffect_000t10", 132208],
  "DG-R28": ["BG36_MidGameEffect_000t", 132192],
  "DG-R29": ["BG36_MidGameEffect_000t2", 132200],
  "DG-R30": ["BG36_MidGameEffect_000t81", 133482],
  "DG-R31": ["BG36_MidGameEffect_000t28", 132553],
  "DG-R32": ["BG36_MidGameEffect_000t29", 132554],
  "DG-R33": ["BG36_MidGameEffect_000t30", 132555],
  "DG-R34": ["BG36_MidGameEffect_000t9", 132207],
  "DG-R35": ["BG36_MidGameEffect_000t69", 133353],
  "DG-R36": ["BG36_MidGameEffect_000t3", 132201],
  "DG-R37": ["BG36_MidGameEffect_000t7", 132205],
  "DG-R38": ["BG36_MidGameEffect_000t71", 133359],
  "DG-R39": ["BG36_MidGameEffect_000t64t", 133457],
  "DG-R40": ["BG36_MidGameEffect_000t61", 132835],
  "DG-R41": ["BG36_MidGameEffect_000t12", 132276],
  "DG-R42": ["BG36_MidGameEffect_000t72", 133363],
  "DG-R43": ["BG36_MidGameEffect_000t60", 132833]
};

const returningTrinketCorrections = new Map([
  [111664, { cost: 2 }],
  [120866, { cost: 6 }],
  [115253, { cost: 2 }],
  [117416, { cost: 2 }],
  [117858, { cost: 1 }],
  [120864, {
    cost: 2,
    text: "After you play a <b>Magnetic</b> minion, cast Repair Job on a random friendly Mech."
  }],
  [131278, {
    cost: 1,
    text: "[x]Get a Woodland Defiler.\n<b>Fodders</b> in the Tavern\nhave +4/+4."
  }],
  [131277, {
    cost: 1,
    text: "[x]Get a Woodland Defiler.\n<b>Fodders</b> in the Tavern\nhave +15/+15."
  }]
]);

const returningTrinketZhText = new Map([
  ["BG32_MagicItem_170", "在你使用一张<b>磁力</b>随从牌后，随机对一个友方机械施放维修作业。"],
  ["BG35_MagicItem_151", "获取一张林地亵渎者。酒馆中的恶魔<b>饲料</b>拥有+4/+4。"],
  ["BG35_MagicItem_151t", "获取一张林地亵渎者。酒馆中的恶魔<b>饲料</b>拥有+15/+15。"]
]);

const season14GeneratedOnlyChromadrakeIds = new Set([
  "BG34_634t",
  "BG34_635t",
  "BG34_636t",
  "BG34_637t",
  "BG34_638t"
]);

const load = relative => JSON.parse(fs.readFileSync(path.join(root, relative), "utf8"));
const output = new Map();
const save = (relative, value) => output.set(relative, JSON.stringify(value, null, 2) + "\n");

const gifts = load(files.gifts);
const giftZh = load(files.giftZh);
const trinkets = load(files.trinkets);
const trinketZh = load(files.trinketZh);
const versions = load(files.versions);

const season14Version = versions.versions.find(version => version.id === "36.2-preview");
if (!season14Version) throw new Error("Missing stable Season 14 version entry.");
season14Version.displayName = "36.2";
season14Version.officialStatus = "Released";
season14Version.changeSummary = "Season 14: Dark Gifts, Activate, Lockbox, Fishbait, heroes, cards and pool changes. Trainer support remains partial.";

if (gifts.darkGifts.length !== 43 || Object.keys(darkGiftIdentity).length !== 43) {
  throw new Error("Expected exactly 43 Season 14 Dark Gifts.");
}

const oldGiftIdToCardId = new Map();
for (const gift of gifts.darkGifts) {
  const identity = darkGiftIdentity[gift.researchKey];
  if (!identity) throw new Error(`Missing frozen Dark Gift identity: ${gift.researchKey}`);
  const [cardId, dbfId] = identity;
  oldGiftIdToCardId.set(gift.id, cardId);
  gift.id = cardId;
  gift.cardId = cardId;
  gift.dbfId = dbfId;
  gift.sourceLevel = "LiveClientCrossChecked";
  gift.name = gift.name.replace(/ \((low|high)\)$/, "");
  gift.imagePath = `CardImages/DarkGifts/Season14/${cardId}`;
  gift.imageSource = `https://art.hearthstonejson.com/v1/orig/${cardId}.png`;
}

for (const localized of giftZh.gifts) {
  const cardId = oldGiftIdToCardId.get(localized.id) ?? localized.id;
  localized.id = cardId;
  localized.name = localized.name.replace(/（(?:低|高)回合）$/, "");
}

const season14Trinkets = trinkets.trinkets.filter(item =>
  item.researchKey?.startsWith("LT-R") || item.researchKey?.startsWith("GT-R"));
if (season14Trinkets.length !== 47) throw new Error(`Expected 47 Season 14 Trinkets, got ${season14Trinkets.length}.`);

const oldTrinketIdToCardId = new Map();
for (const trinket of season14Trinkets) {
  const cardId = path.posix.basename(trinket.imagePath ?? "");
  if (!/^BG36_MagicItem_[A-Za-z0-9]+$/.test(cardId)) {
    throw new Error(`Invalid Season 14 Trinket image/CardId mapping: ${trinket.researchKey}`);
  }
  oldTrinketIdToCardId.set(trinket.cardId, cardId);
  trinket.cardId = cardId;
  trinket.notes = `36.2 live-client identity ${cardId} / DBF ${trinket.dbfId}; behavior covered by Season14 trinket regression.`;
}

for (const trinket of trinkets.trinkets) {
  const correction = returningTrinketCorrections.get(Number(trinket.dbfId));
  if (!correction) continue;
  trinket.cost = correction.cost;
  if (correction.text) trinket.text = correction.text;
}

for (const localized of trinketZh.cards) {
  localized.cardId = oldTrinketIdToCardId.get(localized.cardId) ?? localized.cardId;
  localized.text = returningTrinketZhText.get(localized.cardId) ?? localized.text;
}

for (const revision of versions.entityRevisions) {
  if (revision.kind === "Trinket") {
    revision.stableEntityId = oldTrinketIdToCardId.get(revision.stableEntityId) ?? revision.stableEntityId;
  }
}
for (const contentSet of versions.contentSets) {
  if (contentSet.id === "content-36.2-preview-v1") {
    contentSet.poolMembership = (contentSet.poolMembership ?? []).filter(membership =>
      membership.kind !== "Minion" ||
      !season14GeneratedOnlyChromadrakeIds.has(membership.stableEntityId));
  }
  for (const membership of contentSet.poolMembership ?? []) {
    if (membership.kind === "Trinket") {
      membership.stableEntityId = oldTrinketIdToCardId.get(membership.stableEntityId) ?? membership.stableEntityId;
    }
  }
}

save(files.gifts, gifts);
save(files.giftZh, giftZh);
save(files.trinkets, trinkets);
save(files.trinketZh, trinketZh);
save(files.versions, versions);

const testRoot = path.join(root, "Assets/LearnHearthstone/Tests");
const walk = directory => fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
  const target = path.join(directory, entry.name);
  return entry.isDirectory() ? walk(target) : entry.isFile() && entry.name.endsWith(".cs") ? [target] : [];
});
for (const absolute of walk(testRoot)) {
  let text = fs.readFileSync(absolute, "utf8");
  const before = text;
  for (const [oldId, cardId] of oldTrinketIdToCardId) text = text.split(oldId).join(cardId);
  if (text !== before) output.set(path.relative(root, absolute).replaceAll("\\", "/"), text);
}

let pending = 0;
for (const [relative, next] of output) {
  const absolute = path.join(root, relative);
  const current = fs.readFileSync(absolute, "utf8");
  if (current === next) continue;
  pending += 1;
  if (!checkOnly) fs.writeFileSync(absolute, next, "utf8");
}

const summary = {
  darkGifts: gifts.darkGifts.length,
  trinkets: season14Trinkets.length,
  trinketAliasesPreserved: season14Trinkets.filter(item => item.id.startsWith("preview-s14-trinket-")).length,
  pendingFiles: pending,
  checkOnly
};
console.log(JSON.stringify(summary));
if (checkOnly && pending > 0) process.exitCode = 1;
