using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Data
{
    public sealed class MechanicCoverageRegistration
    {
        public string Key;
        public string System;
        public bool Configurable;
        public bool CombatConsumed;
        public bool UiVisible;
        public bool TestCovered;
        public string DesignConfidence;
        public string Notes;

        public MechanicCoverageRow ToReportRow()
        {
            return new MechanicCoverageRow
            {
                System = System,
                Configurable = Configurable,
                CombatConsumed = CombatConsumed,
                UiVisible = UiVisible,
                TestCovered = TestCovered,
                DesignConfidence = DesignConfidence,
                Notes = Notes
            };
        }
    }

    public static class MechanicCoverageRegistry
    {
        private static readonly List<MechanicCoverageRegistration> Entries = new List<MechanicCoverageRegistration>
        {
            Entry("opponent-hand", "Opponent hand", true, true, true, true, "High", "Opponent hand can be configured, deleted, mapped into combat, and consumed by hand-summon effects."),
            Entry("undead-growth", "Undead growth", true, true, true, true, "High", "Side undead attack is stored separately and consumed by opponent combat snapshots plus retained cards."),
            Entry("spell-power", "Spell power", true, true, true, true, "Medium", "Combat spell-like damage reads side spell power; coverage should expand as more spell effects are modeled."),
            Entry("spells-cast", "Spells cast this game", true, false, true, false, "Medium", "Stored per side and visible, but only effects with explicit spell-count hooks consume it."),
            Entry("tavern-spell-stat-bonus", "Tavern spell stat bonus", true, false, true, false, "Medium", "Stored and combat-reward-persisted per side; only explicit Tavern-spell casting paths consume it."),
            Entry("blood-gem-quality", "Blood gem quality", true, true, true, true, "High", "Blood gem attack and health are side-configurable, combat-reward-persisted, and consumed only when an effect actually plays a Blood Gem."),
            Entry("eternal-knight-history", "Eternal Knight history", true, true, true, true, "High", "Both sides persist Eternal Knight combat deaths and immediately recalculate retained cards."),
            Entry("astral-automaton-history", "Astral Automaton history", true, true, true, true, "High", "Opponent combat snapshots and retained cards receive history stats without double-applying player service buffs."),
            Entry("friendly-deaths", "Friendly deaths this game", true, false, true, true, "Medium", "Combat deaths persist for both sides; only mechanics with explicit death-count hooks consume the stored history."),
            Entry("timewarped-historical-minions", "Timewarped historical minions", true, true, true, true, "High", "Historical pool and runtime handlers are split into dedicated coverage and tests."),
            Entry("quest-resources", "Quest resources", true, true, true, true, "Medium", "Quest rewards can convert resources into combat stats, hand cards, or board pressure through implemented reward paths."),
            Entry("trinket-resources", "Trinket resources", true, true, true, true, "Medium", "Combat and economy trinket rewards have implemented paths; edge trinkets remain a normal content-coverage backlog."),
            Entry("quest-trinket-interactions", "Quest/Trinket interactions", true, true, true, true, "Medium High", "Start-of-combat stacking, deathrattle repeats, shared Avenge events, summon modifiers, opponent reward isolation, repeated summon overflow, stacked repeat sources, non-Beast filtering, counter remainders, and replay non-duplication have focused tests."),
            Entry("anomaly-rules", "Anomaly rules", true, true, true, true, "Medium", "Anomaly catalog/pool controls exist; design trust depends on per-anomaly implementation status."),
            Entry("timewarped-content-precision", "Timewarped content precision", true, true, true, true, "High", "High-impact combat minions and non-minion timewarped cards are covered by runtime handlers and focused tests, including Deathswarmer combat growth, Kil'rek demon reward, and Goldrinn immediate Beast combat growth precision slices."),
            Entry("darkmoon-prize-precision", "Darkmoon prize precision", true, true, true, true, "Medium High", "First-batch direct prizes plus persistent and ordering-sensitive prizes have dedicated system tests."),
            Entry("darkmoon-prizes", "Darkmoon prizes", true, true, true, true, "Medium High", "Prize system has dedicated tests after being split from anomaly tests."),
            Entry("full-next-turn-flow", "Full next-turn flow", true, true, true, true, "High", "Next turn resolves turn end, combat, and turn start unless debug-skip is used.")
        };

        public static IReadOnlyList<MechanicCoverageRegistration> All => Entries;

        public static MechanicCoverageRegistration Find(string key)
        {
            return Entries.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private static MechanicCoverageRegistration Entry(
            string key,
            string system,
            bool configurable,
            bool combatConsumed,
            bool uiVisible,
            bool testCovered,
            string confidence,
            string notes)
        {
            return new MechanicCoverageRegistration
            {
                Key = key,
                System = system,
                Configurable = configurable,
                CombatConsumed = combatConsumed,
                UiVisible = uiVisible,
                TestCovered = testCovered,
                DesignConfidence = confidence,
                Notes = notes
            };
        }
    }
}
