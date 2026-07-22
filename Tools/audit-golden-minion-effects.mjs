import fs from "node:fs";
import path from "node:path";

const root = process.cwd();
const checkOnly = process.argv.includes("--check");
const minionPath = path.join(root, "Assets/LearnHearthstone/Resources/Data/battlegroundsMinions.json");
const contractPath = path.join(root, "Docs/data/golden-minion-effect-contracts.json");
const reportPath = path.join(root, "Docs/generated/GoldenMinionEffectAuditReport.md");

const walk = (directory, extension) => fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
  const current = path.join(directory, entry.name);
  return entry.isDirectory() ? walk(current, extension) : current.endsWith(extension) ? [current] : [];
});
const normalize = (value) => String(value ?? "").replace(/<[^>]+>/g, "").replace(/\s+/g, " ").trim();
const sourceFiles = walk(path.join(root, "Assets/LearnHearthstone/Runtime"), ".cs");
const testFiles = walk(path.join(root, "Assets/LearnHearthstone/Tests"), ".cs");
const runtimeSources = sourceFiles.map((file) => ({ file, text: fs.readFileSync(file, "utf8") }));
const testSources = testFiles.map((file) => ({ file, text: fs.readFileSync(file, "utf8") }));
const data = JSON.parse(fs.readFileSync(minionPath, "utf8"));
const minions = Array.isArray(data) ? data : data.minions ?? data.cards ?? [];

const classify = (normal, golden) => {
  const combined = `${normal} ${golden}`;
  const kinds = [];
  if (JSON.stringify(normal.match(/\d+/g) ?? []) !== JSON.stringify(golden.match(/\d+/g) ?? [])) kinds.push("NumericChange");
  if (/twice|three times|extra time|repeat/i.test(combined)) kinds.push("RepeatCount");
  if (/both|adjacent|different friendly|one of each|two random|2 random/i.test(combined)) kinds.push("TargetOrQuantity");
  if (/summon|get |add |copy/i.test(combined)) kinds.push("GeneratedCardOrToken");
  if (/in combat|this combat|this turn|this game|per turn|end of your turn|start of combat/i.test(combined)) kinds.push("PhaseOrDuration");
  if (/double|triple|set its stats|maximum stats/i.test(combined)) kinds.push("FormulaChange");
  return kinds.length > 0 ? [...new Set(kinds)] : ["TextChange"];
};
const references = (sources, cardId) => sources.filter((source) => source.text.includes(cardId)).map((source) => path.relative(root, source.file).replaceAll("\\", "/"));
const hasNearbyGoldenTest = (cardId) => testSources.some((source) => {
  let index = source.text.indexOf(cardId);
  while (index >= 0) {
    const context = source.text.slice(Math.max(0, index - 1800), Math.min(source.text.length, index + 2600));
    if (/\.Golden\s*=\s*true|Golden\s*=\s*true/i.test(context)) return true;
    index = source.text.indexOf(cardId, index + cardId.length);
  }
  return false;
});
const hasStaticGoldenBranch = (cardId) => runtimeSources.some((source) => {
  const constantPattern = new RegExp(`(?:const|static readonly)\\s+string\\s+(\\w+)\\s*=\\s*\"${cardId}\"`);
  const constant = source.text.match(constantPattern)?.[1];
  const needles = constant ? [constant, cardId] : [cardId];
  return needles.some((needle) => {
    let index = source.text.indexOf(needle);
    while (index >= 0) {
      const context = source.text.slice(Math.max(0, index - 3500), Math.min(source.text.length, index + 5000));
      if (/\.Golden|Golden\s*\?|golden\s*\?|var\s+multiplier\s*=.*Golden/is.test(context)) return true;
      index = source.text.indexOf(needle, index + needle.length);
    }
    return false;
  });
});

const contracts = minions
  .filter((minion) => !String(minion.cardId).startsWith("BGDUO") && minion.golden && normalize(minion.text) !== normalize(minion.golden.text))
  .map((minion) => {
    const runtime = references(runtimeSources, minion.cardId);
    const tests = references(testSources, minion.cardId);
    const goldenTestEvidence = hasNearbyGoldenTest(minion.cardId);
    const staticGoldenBranchEvidence = hasStaticGoldenBranch(minion.cardId);
    return {
      cardId: minion.cardId,
      goldenCardId: minion.golden.cardId,
      dbfId: minion.dbfId,
      goldenDbfId: minion.golden.dbfId,
      name: minion.name,
      englishName: minion.englishName,
      tavernTier: minion.tavernTier,
      inPool: Boolean(minion.inPool),
      normalText: minion.text,
      goldenText: minion.golden.text,
      normalEnglishText: minion.englishText,
      goldenEnglishText: minion.golden.englishText,
      deltaKinds: classify(normalize(minion.englishText), normalize(minion.golden.englishText)),
      implementationStatus: runtime.length > 0 ? "RuntimeReferenced" : "NeedsImplementation",
      runtimeOwners: runtime,
      testOwners: tests,
      goldenTestEvidence,
      staticGoldenBranchEvidence,
      verificationStatus: goldenTestEvidence
        ? "BehaviorTestEvidence"
        : staticGoldenBranchEvidence
          ? "StaticGoldenBranchEvidence"
          : "NeedsSemanticReview",
      source: "local-catalog-snapshot",
      sourceUrl: "https://api.hearthstonejson.com/v1/latest/enUS/cards.json"
    };
  });

const issues = [];
for (const contract of contracts) {
  if (contract.runtimeOwners.length === 0) {
    issues.push({ severity: "error", rule: "GOLD002", cardId: contract.cardId, message: "Solo Golden-delta card has no Runtime owner." });
  }
  if (contract.verificationStatus === "NeedsSemanticReview") {
    issues.push({ severity: "warning", rule: "GOLD009", cardId: contract.cardId, message: "No behavior test or static Golden branch evidence was found." });
  }
}

const ledger = {
  schemaVersion: 1,
  generatedAt: new Date().toISOString(),
  source: path.relative(root, minionPath).replaceAll("\\", "/"),
  scope: "single-player cards with different normal and Golden rules text",
  count: contracts.length,
  contracts
};
const report = [
  "# Golden minion effect audit report",
  "",
  `- Contracts: ${contracts.length}`,
  `- Runtime referenced: ${contracts.filter((contract) => contract.runtimeOwners.length > 0).length}`,
  `- Golden test evidence: ${contracts.filter((contract) => contract.goldenTestEvidence).length}`,
  `- Static Golden branch evidence: ${contracts.filter((contract) => contract.staticGoldenBranchEvidence).length}`,
  `- Needs semantic review: ${contracts.filter((contract) => contract.verificationStatus === "NeedsSemanticReview").length}`,
  `- Errors: ${issues.filter((issue) => issue.severity === "error").length}`,
  `- Warnings: ${issues.filter((issue) => issue.severity === "warning").length}`,
  "",
  "## Issues",
  "",
  "| Severity | Rule | CardId | Message |",
  "|---|---|---|---|",
  ...issues.map((issue) => `| ${issue.severity} | ${issue.rule} | \`${issue.cardId}\` | ${issue.message} |`),
  ""
].join("\n");

fs.mkdirSync(path.dirname(contractPath), { recursive: true });
fs.mkdirSync(path.dirname(reportPath), { recursive: true });
fs.writeFileSync(contractPath, `${JSON.stringify(ledger, null, 2)}\n`);
fs.writeFileSync(reportPath, report);

console.log(`Golden contracts: ${contracts.length}`);
console.log(`Errors: ${issues.filter((issue) => issue.severity === "error").length}`);
console.log(`Warnings: ${issues.filter((issue) => issue.severity === "warning").length}`);
if (checkOnly && issues.some((issue) => issue.severity === "error")) process.exit(1);
