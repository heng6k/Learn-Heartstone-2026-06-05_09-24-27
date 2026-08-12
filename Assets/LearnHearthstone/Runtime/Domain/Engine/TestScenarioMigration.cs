using System;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class TestScenarioMigration
    {
        public const string LegacyVersion = "battle-test-loop-v1";
        public const string V2Version = "battle-test-loop-v2";
        public const string CurrentVersion = "battle-test-loop-v3";
        public const int CurrentSchemaVersion = 3;
        public const int CurrentMechanicStateSchemaVersion = 2;
        public const string DerivedRngAlgorithm = "derived-seed-v1";

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

            if (string.Equals(version, V2Version, StringComparison.OrdinalIgnoreCase))
            {
                MigrateV2ToV3(scenario);
                version = scenario.Version;
            }

            if (!string.Equals(version, CurrentVersion, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unsupported test scenario version: " + version);
            }

            NormalizeV3(scenario);
            return scenario;
        }

        public static int ResolveRulesetRevision(string rulesetId)
        {
            return string.Equals(rulesetId, RulesetIds.LegacyCompositeSandbox, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rulesetId, RulesetIds.Season14Preview, StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;
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
            scenario.Version = V2Version;
        }

        private static void MigrateV2ToV3(TestScenarioDefinition scenario)
        {
            scenario.SchemaVersion = CurrentSchemaVersion;
            scenario.IsStateTemplate = false;
            scenario.MechanicStateSchemaVersion = CurrentMechanicStateSchemaVersion;
            scenario.GameVersionId = GameVersionIds.LegacyCompositeSandbox;
            scenario.RulesetId = RulesetIds.LegacyCompositeSandbox;
            scenario.RulesetRevision = ResolveRulesetRevision(scenario.RulesetId);
            scenario.ContentSnapshotId = string.Empty;
            scenario.ContentFingerprint = string.Empty;
            scenario.CardPoolPresetId = CardPoolVersionFactory.DefaultVersionId;
            scenario.CardPoolPresetName = "Default";
            scenario.IsDefaultCardPoolPreset = true;
            scenario.ResolvedCardPool = new ScenarioResolvedCardPoolState();
            scenario.Version = CurrentVersion;
        }

        private static void NormalizeV3(TestScenarioDefinition scenario)
        {
            scenario.SchemaVersion = CurrentSchemaVersion;
            scenario.Version = CurrentVersion;
            scenario.MechanicStateSchemaVersion = CurrentMechanicStateSchemaVersion;
            scenario.PendingTurnStartRound = Math.Max(0, scenario.PendingTurnStartRound);
            scenario.TurnEndTransitionSequence = Math.Max(0, scenario.TurnEndTransitionSequence);
            if (scenario.PendingTurnStartRound > 0 && !string.IsNullOrWhiteSpace(scenario.PendingTurnEndTransitionId))
            {
                scenario.PendingTurnEndOccurrenceCount = Math.Max(1, scenario.PendingTurnEndOccurrenceCount);
            }
            else
            {
                scenario.PendingTurnResolvedCombat = false;
                scenario.PendingTurnEndTransitionId = null;
                scenario.PendingTurnEndOccurrenceCount = 0;
            }

            scenario.Player = scenario.Player ?? new PlayerScenarioState();
            scenario.Opponent = scenario.Opponent ?? new OpponentScenarioState();
            scenario.Tavern = scenario.Tavern ?? new ScenarioTavernState();
            scenario.PlayerCombatModifiers = scenario.PlayerCombatModifiers ?? new SideCombatModifierState();
            scenario.OpponentCombatModifiers = scenario.OpponentCombatModifiers ?? new SideCombatModifierState();
            scenario.Shop = scenario.Shop ?? new System.Collections.Generic.List<ScenarioCardState>();
            scenario.Hand = scenario.Hand ?? new System.Collections.Generic.List<ScenarioCardState>();
            scenario.OpponentHand = scenario.OpponentHand ?? new System.Collections.Generic.List<ScenarioCardState>();
            scenario.PlayerBoard = scenario.PlayerBoard ?? new System.Collections.Generic.List<ScenarioCardState>();
            scenario.OpponentBoard = scenario.OpponentBoard ?? new System.Collections.Generic.List<ScenarioCardState>();
            scenario.ResolvedCardPool = scenario.ResolvedCardPool ?? new ScenarioResolvedCardPoolState();
            NormalizePool(scenario.ResolvedCardPool);
            scenario.PlayerAdvancedMechanics = NormalizeAdvanced(scenario.PlayerAdvancedMechanics);
            scenario.OpponentAdvancedMechanics = NormalizeAdvanced(scenario.OpponentAdvancedMechanics);
            scenario.PlayerDarkGiftState = scenario.PlayerDarkGiftState ?? new ScenarioPlayerDarkGiftState();
            scenario.PlayerDarkGiftState.AcquiredGiftInstances = scenario.PlayerDarkGiftState.AcquiredGiftInstances ?? new System.Collections.Generic.List<ScenarioDarkGiftInstanceState>();
            scenario.PlayerDarkGiftState.Counters = scenario.PlayerDarkGiftState.Counters ?? new System.Collections.Generic.List<ScenarioCounterState>();
            scenario.PlayerDarkGiftState.Cooldowns = scenario.PlayerDarkGiftState.Cooldowns ?? new System.Collections.Generic.List<ScenarioCounterState>();
            scenario.PlayerDarkGiftState.TriggerHistory = scenario.PlayerDarkGiftState.TriggerHistory ?? new System.Collections.Generic.List<ScenarioMechanicEventRecord>();
            foreach (var triggerEvent in scenario.PlayerDarkGiftState.TriggerHistory)
            {
                if (triggerEvent != null)
                {
                    triggerEvent.Targets = triggerEvent.Targets ?? new System.Collections.Generic.List<string>();
                }
            }
            scenario.ChoiceQueueState = scenario.ChoiceQueueState ?? new ScenarioChoiceQueueState();
            scenario.ChoiceQueueState.PendingChoices = scenario.ChoiceQueueState.PendingChoices ?? new System.Collections.Generic.List<ScenarioChoiceQueueItem>();
            scenario.ChoiceQueueState.CompletedRequestIds = scenario.ChoiceQueueState.CompletedRequestIds ?? new System.Collections.Generic.List<string>();
            scenario.ChoiceQueueState.NextSequence = Math.Max(1, scenario.ChoiceQueueState.NextSequence);
            if (!scenario.ChoiceQueueState.HasActiveChoice ||
                string.IsNullOrWhiteSpace(scenario.ChoiceQueueState.ActiveChoice?.RequestId))
            {
                scenario.ChoiceQueueState.HasActiveChoice = false;
                scenario.ChoiceQueueState.ActiveChoice = null;
            }
            else
            {
                NormalizeChoiceItem(scenario.ChoiceQueueState.ActiveChoice);
            }

            foreach (var choice in scenario.ChoiceQueueState.PendingChoices)
            {
                NormalizeChoiceItem(choice);
            }
            scenario.RecruitActionStates = scenario.RecruitActionStates ?? new System.Collections.Generic.List<ScenarioRecruitActionState>();
            scenario.DelayedObjectStates = scenario.DelayedObjectStates ?? new System.Collections.Generic.List<ScenarioDelayedObjectState>();
            scenario.MechanicEvents = scenario.MechanicEvents ?? new System.Collections.Generic.List<ScenarioMechanicEventRecord>();
            foreach (var mechanicEvent in scenario.MechanicEvents)
            {
                if (mechanicEvent != null)
                {
                    mechanicEvent.Targets = mechanicEvent.Targets ?? new System.Collections.Generic.List<string>();
                }
            }
            scenario.RecruitLog = scenario.RecruitLog ?? new System.Collections.Generic.List<RecruitLogEntry>();
            scenario.RngState = scenario.RngState ?? new ScenarioDeterministicRngState();
            if (string.IsNullOrWhiteSpace(scenario.RngState.Algorithm))
            {
                scenario.RngState.Algorithm = DerivedRngAlgorithm;
                scenario.RngState.Seed = scenario.Seed;
                scenario.RngState.Round = Math.Max(1, scenario.SavedAtRound);
                scenario.RngState.RecruitLogCursor = scenario.RecruitLog.Count;
                scenario.RngState.MechanicEventCursor = scenario.MechanicEvents.Count;
            }

            if (scenario.RulesetRevision <= 0)
            {
                scenario.RulesetRevision = ResolveRulesetRevision(scenario.RulesetId);
            }
        }

        private static ScenarioAdvancedMechanicState NormalizeAdvanced(ScenarioAdvancedMechanicState state)
        {
            state = state ?? new ScenarioAdvancedMechanicState();
            state.State = state.State ?? new AdvancedMechanicState();
            state.Counters = state.Counters ?? new System.Collections.Generic.List<ScenarioCounterState>();
            state.Selections = state.Selections ?? new System.Collections.Generic.List<ScenarioStringState>();
            state.QuestRewardCounters = state.QuestRewardCounters ?? new System.Collections.Generic.List<ScenarioCounterState>();
            state.QuestRewardFlags = state.QuestRewardFlags ?? new System.Collections.Generic.List<ScenarioBoolState>();
            state.AnomalyCounters = state.AnomalyCounters ?? new System.Collections.Generic.List<ScenarioCounterState>();
            state.AnomalyFlags = state.AnomalyFlags ?? new System.Collections.Generic.List<ScenarioStringState>();
            return state;
        }

        private static void NormalizeChoiceItem(ScenarioChoiceQueueItem choice)
        {
            if (choice == null)
            {
                return;
            }

            choice.Options = choice.Options ?? new System.Collections.Generic.List<MechanicChoiceOption>();
            choice.ResolutionMetadata = choice.ResolutionMetadata ?? new System.Collections.Generic.List<ScenarioStringState>();
            if (choice.Discover != null)
            {
                choice.Discover.Options = choice.Discover.Options ?? new System.Collections.Generic.List<ScenarioCardState>();
            }
        }

        private static void NormalizePool(ScenarioResolvedCardPoolState pool)
        {
            pool.ActiveTribes = pool.ActiveTribes ?? new System.Collections.Generic.List<Tribe>();
            pool.EnabledTimewarpedCardIds = pool.EnabledTimewarpedCardIds ?? new System.Collections.Generic.List<string>();
            pool.EnabledMinionCardIds = pool.EnabledMinionCardIds ?? new System.Collections.Generic.List<string>();
            pool.EnabledTavernSpellCardNumbers = pool.EnabledTavernSpellCardNumbers ?? new System.Collections.Generic.List<string>();
            pool.EnabledQuestCardIds = pool.EnabledQuestCardIds ?? new System.Collections.Generic.List<string>();
            pool.EnabledQuestRewardCardIds = pool.EnabledQuestRewardCardIds ?? new System.Collections.Generic.List<string>();
            pool.EnabledLesserTrinketCardIds = pool.EnabledLesserTrinketCardIds ?? new System.Collections.Generic.List<string>();
            pool.EnabledGreaterTrinketCardIds = pool.EnabledGreaterTrinketCardIds ?? new System.Collections.Generic.List<string>();
            pool.EnabledAnomalyCardIds = pool.EnabledAnomalyCardIds ?? new System.Collections.Generic.List<string>();
        }
    }
}
