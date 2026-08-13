import fs from "node:fs";
import path from "node:path";

const minionPath = "Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json";
const heroPath = "Assets/LearnHearthstone/Resources/Data/battlegroundsHeroes.json";
const versionPath = "Assets/LearnHearthstone/Resources/Data/battlegroundsGameVersions.json";
const heroLocalizationPath = "Assets/LearnHearthstone/Resources/Data/battlegroundsHeroLocalizationZhCN.json";
const checkOnly = process.argv.includes("--check");
const isNewSeason14Minion = item => /^(ACT-R0[123]N|LOCK-R0[23]N|FISH-R0[123]N|MIN-R\d\d)$/.test(item.researchKey ?? "");
const productionMinionCardId = item => path.posix.basename(item.imagePath);

function replaceToken(raw, label, currentValue, expectedValue) {
  if (currentValue === expectedValue) {
    return raw;
  }

  const pattern = new RegExp(`(\"${label}\"\\s*:\\s*)\"${currentValue}\"`, "g");
  const matches = [...raw.matchAll(pattern)];
  if (matches.length !== 1) {
    throw new Error(`Expected exactly one ${label} token for ${currentValue}.`);
  }
  return raw.replace(pattern, `$1\"${expectedValue}\"`);
}

function replaceNumber(raw, label, currentValue, expectedValue) {
  if (currentValue === expectedValue) {
    return raw;
  }

  const pattern = new RegExp(`(\"${label}\"\\s*:\\s*)${currentValue}(?=\\s*[,}])`, "g");
  const matches = [...raw.matchAll(pattern)];
  if (matches.length !== 1) {
    throw new Error(`Missing ${label} token for ${currentValue}.`);
  }
  return raw.replace(pattern, `$1${expectedValue}`);
}

function migrateToken(raw, label, legacyValue, expectedValue) {
  if (legacyValue === expectedValue) {
    return raw;
  }
  const legacyPattern = new RegExp(`(\"${label}\"\\s*:\\s*)\"${legacyValue}\"`, "g");
  const expectedPattern = new RegExp(`(\"${label}\"\\s*:\\s*)\"${expectedValue}\"`, "g");
  const legacyCount = [...raw.matchAll(legacyPattern)].length;
  const expectedCount = [...raw.matchAll(expectedPattern)].length;
  if (legacyCount === 1) {
    return replaceToken(raw, label, legacyValue, expectedValue);
  }
  if (legacyCount === 0 && expectedCount === 1) {
    return raw;
  }
  throw new Error(`Expected one legacy or migrated ${label} token for ${legacyValue}.`);
}

function finalizeMinions() {
  const original = fs.readFileSync(minionPath, "utf8");
  const data = JSON.parse(original);
  let next = original;
  let updated = 0;

  const season14Minions = data.minions.filter(isNewSeason14Minion);
  if (season14Minions.length !== 63) {
    throw new Error(`Expected 63 Season 14 minions, got ${season14Minions.length}.`);
  }
  for (const minion of season14Minions) {
    if (!(minion.dbfId > 0) || !(minion.golden?.dbfId > 0)) {
      throw new Error(`${minion.researchKey} is missing a production DBF identity.`);
    }

    const expectedCardId = productionMinionCardId(minion);
    const expectedGoldenCardId = `${expectedCardId}_G`;
    const before = next;
    next = replaceToken(next, "cardId", minion.cardId, expectedCardId);
    next = replaceToken(next, "cardId", minion.golden.cardId, expectedGoldenCardId);
    if (next !== before) {
      updated += 1;
    }
  }

  if (!checkOnly) {
    fs.writeFileSync(minionPath, next, "utf8");
  }
  return updated;
}

function finalizeHeroes() {
  const original = fs.readFileSync(heroPath, "utf8");
  const data = JSON.parse(original);
  const identities = new Map([
    ["HERO-R01", { heroCardId: "BG36_HERO_105", heroDbfId: 132608, powerCardId: "BG36_HERO_105p", powerDbfId: 134010 }],
    ["HERO-R02", { heroCardId: "BG36_HERO_101", heroDbfId: 132578, powerCardId: "BG36_HERO_101p", powerDbfId: 132581 }]
  ]);
  let next = original;

  for (const hero of data.heroes.filter(item => identities.has(item.researchKey))) {
    const expected = identities.get(hero.researchKey);
    const valueIndex = next.indexOf(`\"${hero.heroCardId}\"`);
    const anchor = next.lastIndexOf("\"heroCardId\"", valueIndex);
    const end = next.indexOf("\"heroCardId\"", valueIndex + hero.heroCardId.length + 2);
    if (valueIndex < 0 || anchor < 0) {
      throw new Error(`Missing hero object for ${hero.researchKey}.`);
    }
    const objectEnd = end < 0 ? next.length : end;
    let segment = next.slice(anchor, objectEnd);
    segment = replaceToken(segment, "heroCardId", hero.heroCardId, expected.heroCardId);
    segment = replaceNumber(segment, "heroDbfId", hero.heroDbfId, expected.heroDbfId);
    segment = replaceToken(segment, "cardId", hero.heroPower.cardId, expected.powerCardId);
    segment = replaceNumber(segment, "dbfId", hero.heroPower.dbfId, expected.powerDbfId);
    next = next.slice(0, anchor) + segment + next.slice(objectEnd);
  }

  if (!checkOnly) {
    fs.writeFileSync(heroPath, next, "utf8");
  }
  return identities.size;
}

function finalizeReferences() {
  const minions = JSON.parse(fs.readFileSync(minionPath, "utf8")).minions
    .filter(isNewSeason14Minion);
  const heroes = JSON.parse(fs.readFileSync(heroPath, "utf8")).heroes
    .filter(item => /^HERO-R0[12]$/.test(item.researchKey ?? ""));
  let versions = fs.readFileSync(versionPath, "utf8");
  let localization = fs.readFileSync(heroLocalizationPath, "utf8");

  for (const minion of minions) {
    versions = migrateToken(
      versions,
      "stableEntityId",
      minion.revisionId.split("@")[0],
      productionMinionCardId(minion));
  }
  for (const hero of heroes) {
    const legacyId = hero.revisionId.split("@")[0];
    versions = migrateToken(versions, "stableEntityId", legacyId, hero.heroCardId);
    localization = migrateToken(localization, "cardId", legacyId, hero.heroCardId);
  }
  localization = migrateToken(
    localization,
    "cardId",
    "preview-s14-hero-power-feel-devastation",
    "BG36_HERO_105p");
  localization = migrateToken(
    localization,
    "cardId",
    "preview-s14-hero-power-void-power",
    "BG36_HERO_101p");

  if (!checkOnly) {
    fs.writeFileSync(versionPath, versions, "utf8");
    fs.writeFileSync(heroLocalizationPath, localization, "utf8");
  }
  return { versionMembers: minions.length + heroes.length, heroLocalizations: heroes.length };
}

function finalizeRuntimeReferences() {
  const minions = JSON.parse(fs.readFileSync(minionPath, "utf8")).minions
    .filter(isNewSeason14Minion);
  const runtimePaths = [
    "Assets/LearnHearthstone/Runtime/Application/Services/MatchService.cs",
    "Assets/LearnHearthstone/Runtime/Domain/Engine/CombatEngine.cs",
    "Assets/LearnHearthstone/Runtime/Domain/Engine/Season14MechanicServices.cs"
  ];
  let replacements = 0;

  for (const runtimePath of runtimePaths) {
    let source = fs.readFileSync(runtimePath, "utf8");
    for (const minion of minions) {
      const legacyId = minion.revisionId.split("@")[0];
      const productionId = productionMinionCardId(minion);
      if (legacyId === productionId || !source.includes(legacyId)) {
        continue;
      }
      const occurrences = source.split(legacyId).length - 1;
      source = source.split(legacyId).join(productionId);
      replacements += occurrences;
    }
    if (!checkOnly) {
      fs.writeFileSync(runtimePath, source, "utf8");
    }
  }
  return replacements;
}

console.log(JSON.stringify({
  minions: finalizeMinions(),
  heroes: finalizeHeroes(),
  references: finalizeReferences(),
  runtimeReferences: finalizeRuntimeReferences(),
  checkOnly
}));
