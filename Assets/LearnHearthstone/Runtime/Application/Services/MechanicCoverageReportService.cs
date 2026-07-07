using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public static class MechanicCoverageReportService
    {
        public static MechanicCoverageReport CreateDefaultReport()
        {
            var report = new MechanicCoverageReport();
            Add(report, "Opponent hand", true, true, true, true, "High", "Opponent hand can be configured, deleted, mapped into combat, and consumed by hand-summon effects.");
            Add(report, "Undead growth", true, true, true, true, "High", "Side undead attack is stored separately and consumed by opponent combat snapshots plus retained cards.");
            Add(report, "Spell power", true, true, true, true, "Medium", "Combat spell-like damage reads side spell power; coverage should expand as more spell effects are modeled.");
            Add(report, "Spells cast this game", true, false, true, false, "Medium", "Stored per side and visible, but only effects with explicit spell-count hooks consume it.");
            Add(report, "Tavern spell stat bonus", true, false, true, false, "Medium", "Stored per side; broad tavern-spell casting consumption is intentionally incremental.");
            Add(report, "Blood gem quality", true, true, true, true, "Medium", "Blood gem attack and health are side-configurable and consumed by existing blood-gem paths.");
            Add(report, "Eternal Knight history", true, true, true, true, "High", "Opponent combat snapshots and retained cards receive history stats without double-applying player service buffs.");
            Add(report, "Astral Automaton history", true, true, true, true, "High", "Opponent combat snapshots and retained cards receive history stats without double-applying player service buffs.");
            Add(report, "Friendly deaths this game", true, false, true, false, "Medium", "Stored per side; only mechanics with explicit death-count hooks consume it today.");
            Add(report, "Timewarped historical minions", true, true, true, true, "High", "Historical pool and runtime handlers are split into dedicated coverage and tests.");
            Add(report, "Quest resources", true, true, true, true, "Medium", "Quest rewards can convert resources into combat stats, hand cards, or board pressure through implemented reward paths.");
            Add(report, "Trinket resources", true, true, true, true, "Medium", "Combat and economy trinket rewards have implemented paths; edge trinkets remain a normal content-coverage backlog.");
            Add(report, "Quest/Trinket interactions", true, true, true, true, "Medium High", "Start-of-combat stacking, deathrattle repeats, shared Avenge events, summon modifiers, opponent reward isolation, repeated summon overflow, stacked repeat sources, non-Beast filtering, counter remainders, and replay non-duplication have focused tests.");
            Add(report, "Anomaly rules", true, true, true, true, "Medium", "Anomaly catalog/pool controls exist; design trust depends on per-anomaly implementation status.");
            Add(report, "Timewarped content precision", true, true, true, true, "High", "High-impact combat minions and non-minion timewarped cards are covered by runtime handlers and focused tests, including Deathswarmer combat growth, Kil'rek demon reward, and Goldrinn immediate Beast combat growth precision slices.");
            Add(report, "Darkmoon prize precision", true, true, true, true, "Medium High", "First-batch direct prizes plus persistent and ordering-sensitive prizes have dedicated system tests.");
            Add(report, "Darkmoon prizes", true, true, true, true, "Medium High", "Prize system has dedicated tests after being split from anomaly tests.");
            Add(report, "Full next-turn flow", true, true, true, true, "High", "Next turn resolves turn end, combat, and turn start unless debug-skip is used.");
            return report;
        }

        private static void Add(MechanicCoverageReport report, string system, bool configurable, bool combatConsumed, bool uiVisible, bool testCovered, string confidence, string notes)
        {
            report.Rows.Add(new MechanicCoverageRow
            {
                System = system,
                Configurable = configurable,
                CombatConsumed = combatConsumed,
                UiVisible = uiVisible,
                TestCovered = testCovered,
                DesignConfidence = confidence,
                Notes = notes
            });
        }
    }
}
