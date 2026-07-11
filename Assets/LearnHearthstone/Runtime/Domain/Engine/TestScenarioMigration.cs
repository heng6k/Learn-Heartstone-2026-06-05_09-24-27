using System;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class TestScenarioMigration
    {
        public const string LegacyVersion = "battle-test-loop-v1";
        public const string CurrentVersion = "battle-test-loop-v2";

        public static TestScenarioDefinition MigrateToCurrent(TestScenarioDefinition scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            var version = string.IsNullOrWhiteSpace(scenario.Version) ? LegacyVersion : scenario.Version;
            if (string.Equals(version, LegacyVersion, StringComparison.OrdinalIgnoreCase))
            {
                MigrateV1ToV2(scenario);
                version = scenario.Version;
            }

            if (!string.Equals(version, CurrentVersion, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unsupported test scenario version: " + version);
            }

            return scenario;
        }

        private static void MigrateV1ToV2(TestScenarioDefinition scenario)
        {
            scenario.PlayerCombatModifiers = scenario.PlayerCombatModifiers ?? new SideCombatModifierState();
            scenario.OpponentCombatModifiers = scenario.OpponentCombatModifiers ?? new SideCombatModifierState();
            if (!scenario.PlayerCombatModifiersAreAuthoritative && scenario.Tavern != null)
            {
                scenario.PlayerCombatModifiers.TavernSpellBonusAttack = Math.Max(0, scenario.Tavern.TavernSpellBonusAttack);
                scenario.PlayerCombatModifiers.TavernSpellBonusHealth = Math.Max(0, scenario.Tavern.TavernSpellBonusHealth);
                scenario.PlayerCombatModifiers.UndeadAttackBonus = Math.Max(0, scenario.Tavern.UndeadAttackBonus);
                scenario.PlayerCombatModifiers.EternalKnightDeaths = Math.Max(0, scenario.Tavern.EternalKnightDeaths);
                scenario.PlayerCombatModifiers.AstralAutomatonSummons = Math.Max(0, scenario.Tavern.AncestralAutomatonSummons);
                scenario.PlayerCombatModifiers.FriendlyMinionDeathsThisGame = Math.Max(0, scenario.Tavern.FriendlyMinionDeathsThisGame);
            }

            scenario.PlayerCombatModifiersAreAuthoritative = true;
            scenario.Version = CurrentVersion;
        }
    }
}
