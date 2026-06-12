import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");
const localPath = path.join(root, "Assets", "LearnHearthstone", "Resources", "Data", "battlegroundsSpells.json");
const apiUrl = "https://hearthstone.blizzard.com/en-us/api/cards?locale=en_US&gameMode=battlegrounds&pageSize=500&bgCardType=spell";
const documentedLegacyExtras = new Set(["119603", "122489", "123553", "127642"]);

const response = await fetch(apiUrl);
if (!response.ok) {
  throw new Error(`Official API request failed: ${response.status} ${response.statusText}`);
}

const officialPayload = await response.json();
const officialSolo = (officialPayload.cards ?? [])
  .filter((card) => card.battlegrounds && card.battlegrounds.duosOnly !== true && card.battlegrounds.isDuosOnly !== true)
  .map((card) => String(card.id))
  .sort();

const localPayload = JSON.parse(await fs.readFile(localPath, "utf8"));
const localSpells = (localPayload.spells ?? localPayload)
  .filter((spell) => spell.inPool !== 0 && spell.category === "TavernSpell")
  .map((spell) => String(spell.cardNumber))
  .sort();

const officialSet = new Set(officialSolo);
const localSet = new Set(localSpells);
const missing = officialSolo.filter((id) => !localSet.has(id));
const unexpected = localSpells.filter((id) => !officialSet.has(id) && !documentedLegacyExtras.has(id));
const legacyExtras = localSpells.filter((id) => documentedLegacyExtras.has(id));

console.log(`official_solo=${officialSolo.length}`);
console.log(`local_tavern_spells=${localSpells.length}`);
console.log(`missing_official=${missing.length}${missing.length ? ` ${missing.join(",")}` : ""}`);
console.log(`unexpected_local=${unexpected.length}${unexpected.length ? ` ${unexpected.join(",")}` : ""}`);
console.log(`documented_legacy_extras=${legacyExtras.length}${legacyExtras.length ? ` ${legacyExtras.join(",")}` : ""}`);

if (missing.length > 0 || unexpected.length > 0) {
  process.exitCode = 1;
}
