using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Domain.Engine
{
    public static class TestScenarioMapper
    {
        public static TestScenarioDefinition Capture(MatchState state, string name)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var tavern = state.Player.Tavern;
            ChoiceQueueService.SynchronizeDiscoverAdapter(state.ChoiceQueue, tavern, state.Round);
            return new TestScenarioDefinition
            {
                SchemaVersion = TestScenarioMigration.CurrentSchemaVersion,
                Version = TestScenarioMigration.CurrentVersion,
                IsStateTemplate = false,
                MechanicStateSchemaVersion = TestScenarioMigration.CurrentMechanicStateSchemaVersion,
                Name = name,
                SavedAtRound = state.Round,
                Seed = state.Seed,
                Phase = state.Phase,
                PendingTurnStartRound = state.PendingTurnStartRound,
                PendingTurnResolvedCombat = state.PendingTurnResolvedCombat,
                PendingTurnEndTransitionId = state.PendingTurnEndTransitionId,
                PendingTurnEndOccurrenceCount = state.PendingTurnEndOccurrenceCount,
                TurnEndTransitionSequence = state.TurnEndTransitionSequence,
                GameVersionId = state.GameVersionId,
                RulesetId = state.RulesetId,
                RulesetRevision = TestScenarioMigration.ResolveRulesetRevision(state.RulesetId),
                ContentSnapshotId = state.ContentSnapshotId,
                ContentFingerprint = state.ContentFingerprint,
                CardPoolPresetId = state.CardPoolVersionId,
                CardPoolPresetName = state.CardPoolVersionName,
                IsDefaultCardPoolPreset = state.IsDefaultCardPoolVersion,
                ResolvedCardPool = CaptureResolvedCardPool(state),
                Player = new PlayerScenarioState
                {
                    HeroId = state.Player.HeroId,
                    Health = state.Player.Health,
                    Armor = state.Player.Armor
                },
                Opponent = new OpponentScenarioState
                {
                    Name = state.Opponent.Name,
                    HeroId = state.Opponent.HeroId,
                    Health = state.Opponent.Health,
                    Armor = state.Opponent.Armor,
                    TavernTier = state.Opponent.TavernTier,
                    Editable = state.Opponent.Editable
                },
                Tavern = new ScenarioTavernState
                {
                    Tier = tavern.Tier,
                    Gold = tavern.Gold,
                    MaxGold = tavern.MaxGold,
                    UpgradeCost = tavern.UpgradeCost,
                    Frozen = tavern.Frozen,
                    NextTurnBonusGold = tavern.NextTurnBonusGold,
                    NextTavernSpellCostReduction = tavern.NextTavernSpellCostReduction,
                    FreeRefreshes = tavern.FreeRefreshes,
                    DemonFodderRefreshes = tavern.DemonFodderRefreshes,
                    TavernSpellBonusAttack = tavern.TavernSpellBonusAttack,
                    TavernSpellBonusHealth = tavern.TavernSpellBonusHealth,
                    GuideShapingSpellCardId = tavern.GuideShapingSpellCardId,
                    GuideShapingSpellCardIds = new List<string>(tavern.GuideShapingSpellCardIds ?? new List<string>()),
                    GuideCoreSpellCardNumbers = new List<string>(tavern.GuideCoreSpellCardNumbers ?? new List<string>()),
                    GuideShapingSpellRound = tavern.GuideShapingSpellRound,
                    GuideShapingSpellConsumed = tavern.GuideShapingSpellConsumed,
                    BeetleAttackBonus = tavern.BeetleAttackBonus,
                    BeetleHealthBonus = tavern.BeetleHealthBonus,
                    FutureBallerAttackBonus = tavern.FutureBallerAttackBonus,
                    FutureBallerHealthBonus = tavern.FutureBallerHealthBonus,
                    UndeadAttackBonus = tavern.UndeadAttackBonus,
                    EternalKnightDeaths = tavern.EternalKnightDeaths,
                    AncestralAutomatonSummons = tavern.AncestralAutomatonSummons,
                    FriendlyMinionDeathsThisGame = tavern.FriendlyMinionDeathsThisGame
                },
                PlayerAdvancedMechanics = CaptureAdvancedMechanics(tavern.AdvancedMechanics),
                OpponentAdvancedMechanics = CaptureAdvancedMechanics(state.Opponent.AdvancedMechanics),
                PlayerDarkGiftState = CapturePlayerDarkGifts(state.PlayerDarkGifts),
                ChoiceQueueState = CaptureChoiceQueue(state.ChoiceQueue),
                RecruitActionStates = CaptureRecruitActionStates(state.RecruitActionStates),
                DelayedObjectStates = CaptureDelayedObjectStates(state.DelayedObjectStates),
                MechanicEvents = CaptureMechanicEvents(state.MechanicEvents),
                RecruitLog = (tavern.RecruitLog ?? new List<RecruitLogEntry>()).ConvertAll(entry => entry?.Clone()),
                RngState = new ScenarioDeterministicRngState
                {
                    Algorithm = TestScenarioMigration.DerivedRngAlgorithm,
                    Seed = state.Seed,
                    Round = Math.Max(1, state.Round),
                    RecruitLogCursor = tavern.RecruitLog?.Count ?? 0,
                    MechanicEventCursor = state.MechanicEvents?.Count(item => item != null) ?? 0
                },
                PlayerCombatModifiersAreAuthoritative = true,
                PlayerCombatModifiers = CapturePlayerCombatModifiers(tavern, state.Player.CombatModifiers),
                OpponentCombatModifiers = CloneModifiers(state.Opponent.CombatModifiers),
                Shop = CaptureCards(tavern.Shop),
                Hand = CaptureCards(tavern.Hand),
                OpponentHand = CaptureCards(state.Opponent.Hand),
                PlayerBoard = CaptureCards(state.Player.Board),
                OpponentBoard = CaptureCards(state.Opponent.Board)
            };
        }

        public static TestScenarioDefinition Clone(TestScenarioDefinition scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            return TestScenarioMigration.MigrateToCurrent(
                JsonUtility.FromJson<TestScenarioDefinition>(JsonUtility.ToJson(scenario)));
        }

        public static void ApplyTo(MatchState target, TestScenarioDefinition scenario)
        {
            var preserveStateTemplate = scenario != null && scenario.IsStateTemplate;
            if (preserveStateTemplate)
            {
                scenario = TestScenarioMigration.MigrateToCurrent(scenario);
                scenario.IsStateTemplate = true;
            }

            var result = TryApplyTo(target, scenario);
            if (!result.IsApplied)
            {
                throw new InvalidOperationException("Scenario restore blocked [" + result.Status + "]: " + result.Message);
            }
        }

        public static TestScenarioRestoreResult TryApplyTo(MatchState target, TestScenarioDefinition scenario)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            scenario = TestScenarioMigration.MigrateToCurrent(scenario);
            var validation = ValidateRestore(target, scenario);
            if (!validation.IsApplied)
            {
                return validation;
            }

            ApplyToCore(target, scenario);
            return Applied();
        }

        public static TestScenarioRestoreResult InspectRestore(MatchState target, TestScenarioDefinition scenario)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            return ValidateRestore(target, TestScenarioMigration.MigrateToCurrent(scenario));
        }

        private static void ApplyToCore(MatchState target, TestScenarioDefinition scenario)
        {
            target.Phase = scenario.Phase;
            target.Round = Math.Max(1, scenario.SavedAtRound);
            target.PendingTurnStartRound = Math.Max(0, scenario.PendingTurnStartRound);
            target.PendingTurnResolvedCombat = scenario.PendingTurnResolvedCombat;
            target.PendingTurnEndTransitionId = scenario.PendingTurnEndTransitionId;
            target.PendingTurnEndOccurrenceCount = Math.Max(0, scenario.PendingTurnEndOccurrenceCount);
            target.TurnEndTransitionSequence = Math.Max(0, scenario.TurnEndTransitionSequence);
            target.Seed = scenario.Seed;
            if (!scenario.IsStateTemplate)
            {
                ApplyVersionAndPoolSnapshot(target, scenario);
            }
            target.Player.HeroId = scenario.Player?.HeroId;
            target.Player.Health = scenario.Player?.Health ?? target.Player.Health;
            target.Player.Armor = scenario.Player?.Armor ?? target.Player.Armor;
            target.Opponent.Name = string.IsNullOrEmpty(scenario.Opponent?.Name) ? target.Opponent.Name : scenario.Opponent.Name;
            target.Opponent.HeroId = scenario.Opponent?.HeroId;
            target.Opponent.Health = scenario.Opponent?.Health ?? target.Opponent.Health;
            target.Opponent.Armor = scenario.Opponent?.Armor ?? target.Opponent.Armor;
            target.Opponent.TavernTier = Math.Max(1, scenario.Opponent?.TavernTier ?? target.Opponent.TavernTier);
            target.Opponent.Editable = scenario.Opponent?.Editable ?? target.Opponent.Editable;

            var tavern = target.Player.Tavern;
            tavern.Tier = Math.Max(1, scenario.Tavern?.Tier ?? tavern.Tier);
            tavern.Gold = Math.Max(0, scenario.Tavern?.Gold ?? tavern.Gold);
            tavern.MaxGold = TavernRules.ClampMaxGold(scenario.Tavern?.MaxGold ?? tavern.MaxGold);
            tavern.UpgradeCost = Math.Max(0, scenario.Tavern?.UpgradeCost ?? tavern.UpgradeCost);
            tavern.Frozen = scenario.Tavern?.Frozen ?? tavern.Frozen;
            tavern.NextTurnBonusGold = Math.Max(0, scenario.Tavern?.NextTurnBonusGold ?? tavern.NextTurnBonusGold);
            tavern.NextTavernSpellCostReduction = Math.Max(0, scenario.Tavern?.NextTavernSpellCostReduction ?? tavern.NextTavernSpellCostReduction);
            tavern.FreeRefreshes = Math.Max(0, scenario.Tavern?.FreeRefreshes ?? tavern.FreeRefreshes);
            tavern.DemonFodderRefreshes = Math.Max(0, scenario.Tavern?.DemonFodderRefreshes ?? tavern.DemonFodderRefreshes);
            tavern.TavernSpellBonusAttack = Math.Max(0, scenario.Tavern?.TavernSpellBonusAttack ?? tavern.TavernSpellBonusAttack);
            tavern.TavernSpellBonusHealth = Math.Max(0, scenario.Tavern?.TavernSpellBonusHealth ?? tavern.TavernSpellBonusHealth);
            tavern.GuideShapingSpellCardId = scenario.Tavern?.GuideShapingSpellCardId;
            tavern.GuideShapingSpellCardIds = new List<string>(scenario.Tavern?.GuideShapingSpellCardIds ?? new List<string>());
            tavern.GuideCoreSpellCardNumbers = new List<string>(scenario.Tavern?.GuideCoreSpellCardNumbers ?? new List<string>());
            tavern.GuideShapingSpellRound = Math.Max(0, scenario.Tavern?.GuideShapingSpellRound ?? 0);
            tavern.GuideShapingSpellConsumed = scenario.Tavern?.GuideShapingSpellConsumed ?? false;
            tavern.BeetleAttackBonus = Math.Max(2, scenario.Tavern?.BeetleAttackBonus ?? tavern.BeetleAttackBonus);
            tavern.BeetleHealthBonus = Math.Max(2, scenario.Tavern?.BeetleHealthBonus ?? tavern.BeetleHealthBonus);
            tavern.FutureBallerAttackBonus = Math.Max(0, scenario.Tavern?.FutureBallerAttackBonus ?? tavern.FutureBallerAttackBonus);
            tavern.FutureBallerHealthBonus = Math.Max(0, scenario.Tavern?.FutureBallerHealthBonus ?? tavern.FutureBallerHealthBonus);
            tavern.UndeadAttackBonus = Math.Max(0, scenario.Tavern?.UndeadAttackBonus ?? tavern.UndeadAttackBonus);
            tavern.EternalKnightDeaths = Math.Max(0, scenario.Tavern?.EternalKnightDeaths ?? tavern.EternalKnightDeaths);
            tavern.AncestralAutomatonSummons = Math.Max(0, scenario.Tavern?.AncestralAutomatonSummons ?? tavern.AncestralAutomatonSummons);
            tavern.FriendlyMinionDeathsThisGame = Math.Max(0, scenario.Tavern?.FriendlyMinionDeathsThisGame ?? tavern.FriendlyMinionDeathsThisGame);

            tavern.Shop = RestoreCards(scenario.Shop, BoardSide.Player);
            tavern.Hand = RestoreCards(scenario.Hand, BoardSide.Player);
            target.Opponent.Hand = RestoreCards(scenario.OpponentHand, BoardSide.Opponent);
            target.Player.Board = RestoreCards(scenario.PlayerBoard, BoardSide.Player);
            target.Opponent.Board = RestoreCards(scenario.OpponentBoard, BoardSide.Opponent);
            target.Player.CombatModifiers = CloneModifiers(scenario.PlayerCombatModifiers) ?? CapturePlayerCombatModifiers(tavern, target.Player.CombatModifiers);

            target.Opponent.CombatModifiers = CloneModifiers(scenario.OpponentCombatModifiers) ?? new SideCombatModifierState();
            ApplyPlayerModifiersToTavern(target.Player.CombatModifiers, tavern);
            tavern.AdvancedMechanics = RestoreAdvancedMechanics(scenario.PlayerAdvancedMechanics);
            target.Opponent.AdvancedMechanics = RestoreAdvancedMechanics(scenario.OpponentAdvancedMechanics);
            target.PlayerDarkGifts = RestorePlayerDarkGifts(scenario.PlayerDarkGiftState);
            target.ChoiceQueue = RestoreChoiceQueue(scenario.ChoiceQueueState);
            target.RecruitActionStates = RestoreRecruitActionStates(scenario.RecruitActionStates);
            target.DelayedObjectStates = RestoreDelayedObjectStates(scenario.DelayedObjectStates);
            target.MechanicEvents = RestoreMechanicEvents(scenario.MechanicEvents);
            ChoiceQueueService.RefreshDiscoverAdapter(target.ChoiceQueue, tavern);
            tavern.RecruitLog = (scenario.RecruitLog ?? new List<RecruitLogEntry>()).ConvertAll(entry => entry?.Clone());
            target.CombatLog.Clear();
            target.LastResult = null;
        }

        private static TestScenarioRestoreResult ValidateRestore(MatchState target, TestScenarioDefinition scenario)
        {
            if (!scenario.IsStateTemplate)
            {
                if (string.IsNullOrWhiteSpace(scenario.ContentSnapshotId) ||
                    string.IsNullOrWhiteSpace(scenario.ContentFingerprint))
                {
                    return Blocked(
                        TestScenarioRestoreStatus.MissingContentSnapshot,
                        "The scenario does not prove an exact content snapshot and cannot be simulated.");
                }

                if (scenario.ResolvedCardPool == null || !scenario.ResolvedCardPool.IsComplete)
                {
                    return Blocked(
                        TestScenarioRestoreStatus.MissingCardPoolSnapshot,
                        "The scenario does not contain a complete resolved card pool snapshot.");
                }

                var expectedRulesetRevision = TestScenarioMigration.ResolveRulesetRevision(target.RulesetId);
                if (!string.Equals(target.GameVersionId, scenario.GameVersionId, StringComparison.Ordinal) ||
                    !string.Equals(target.RulesetId, scenario.RulesetId, StringComparison.Ordinal) ||
                    expectedRulesetRevision <= 0 ||
                    expectedRulesetRevision != scenario.RulesetRevision ||
                    !string.Equals(target.ContentSnapshotId, scenario.ContentSnapshotId, StringComparison.Ordinal) ||
                    !string.Equals(target.ContentFingerprint, scenario.ContentFingerprint, StringComparison.Ordinal))
                {
                    return Blocked(
                        TestScenarioRestoreStatus.ContentSnapshotMismatch,
                        "The scenario version, ruleset revision, snapshot, or fingerprint does not match the loaded content.");
                }
            }

            var rng = scenario.RngState;
            if (rng == null ||
                !string.Equals(rng.Algorithm, TestScenarioMigration.DerivedRngAlgorithm, StringComparison.Ordinal) ||
                rng.Seed != scenario.Seed ||
                rng.Round != Math.Max(1, scenario.SavedAtRound) ||
                rng.RecruitLogCursor != (scenario.RecruitLog?.Count ?? 0) ||
                rng.MechanicEventCursor != (scenario.MechanicEvents?.Count ?? 0))
            {
                return Blocked(
                    TestScenarioRestoreStatus.InvalidRngState,
                    "The scenario RNG state does not match its deterministic event cursors.");
            }

            return Applied();
        }

        private static TestScenarioRestoreResult Applied()
        {
            return new TestScenarioRestoreResult
            {
                Status = TestScenarioRestoreStatus.Applied,
                Message = string.Empty
            };
        }

        private static TestScenarioRestoreResult Blocked(TestScenarioRestoreStatus status, string message)
        {
            return new TestScenarioRestoreResult
            {
                Status = status,
                Message = message
            };
        }

        private static ScenarioResolvedCardPoolState CaptureResolvedCardPool(MatchState state)
        {
            return new ScenarioResolvedCardPoolState
            {
                IsComplete = true,
                ActiveTribes = CopyList(state.ActiveTribes),
                TimewarpedTavernEnabled = state.TimewarpedTavernEnabled,
                UseHistoricalTimewarpedPool = state.UseHistoricalTimewarpedPool,
                TimewarpedPoolVersion = state.TimewarpedPoolVersion,
                UseExplicitTimewarpedPool = state.UseExplicitTimewarpedPool,
                EnabledTimewarpedCardIds = CopyList(state.EnabledTimewarpedCardIds),
                EnabledMinionCardIds = CopyList(state.EnabledMinionCardIds),
                EnabledTavernSpellCardNumbers = CopyList(state.EnabledTavernSpellCardNumbers),
                EnabledQuestCardIds = CopyList(state.EnabledQuestCardIds),
                EnabledQuestRewardCardIds = CopyList(state.EnabledQuestRewardCardIds),
                EnabledLesserTrinketCardIds = CopyList(state.EnabledLesserTrinketCardIds),
                EnabledGreaterTrinketCardIds = CopyList(state.EnabledGreaterTrinketCardIds),
                EnabledAnomalyCardIds = CopyList(state.EnabledAnomalyCardIds)
            };
        }

        private static void ApplyVersionAndPoolSnapshot(MatchState target, TestScenarioDefinition scenario)
        {
            var pool = scenario.ResolvedCardPool;
            target.GameVersionId = scenario.GameVersionId;
            target.RulesetId = scenario.RulesetId;
            target.ContentSnapshotId = scenario.ContentSnapshotId;
            target.ContentFingerprint = scenario.ContentFingerprint;
            target.CardPoolVersionId = scenario.CardPoolPresetId;
            target.CardPoolVersionName = scenario.CardPoolPresetName;
            target.IsDefaultCardPoolVersion = scenario.IsDefaultCardPoolPreset;
            target.ActiveTribes = CopyList(pool.ActiveTribes);
            target.TimewarpedTavernEnabled = pool.TimewarpedTavernEnabled;
            target.UseHistoricalTimewarpedPool = pool.UseHistoricalTimewarpedPool;
            target.TimewarpedPoolVersion = pool.TimewarpedPoolVersion;
            target.UseExplicitTimewarpedPool = pool.UseExplicitTimewarpedPool;
            target.EnabledTimewarpedCardIds = CopyList(pool.EnabledTimewarpedCardIds);
            target.EnabledMinionCardIds = CopyList(pool.EnabledMinionCardIds);
            target.EnabledTavernSpellCardNumbers = CopyList(pool.EnabledTavernSpellCardNumbers);
            target.EnabledQuestCardIds = CopyList(pool.EnabledQuestCardIds);
            target.EnabledQuestRewardCardIds = CopyList(pool.EnabledQuestRewardCardIds);
            target.EnabledLesserTrinketCardIds = CopyList(pool.EnabledLesserTrinketCardIds);
            target.EnabledGreaterTrinketCardIds = CopyList(pool.EnabledGreaterTrinketCardIds);
            target.EnabledAnomalyCardIds = CopyList(pool.EnabledAnomalyCardIds);
        }

        private static ScenarioAdvancedMechanicState CaptureAdvancedMechanics(AdvancedMechanicState advanced)
        {
            var state = advanced?.Clone() ?? new AdvancedMechanicState();
            return new ScenarioAdvancedMechanicState
            {
                State = state,
                Counters = CaptureIntEntries(state.Counters),
                Selections = CaptureStringEntries(state.Selections),
                QuestRewardCounters = CaptureIntEntries(state.Quests?.RewardCounters),
                QuestRewardFlags = CaptureBoolEntries(state.Quests?.RewardFlags),
                AnomalyCounters = CaptureIntEntries(state.Anomalies?.Counters),
                AnomalyFlags = CaptureStringEntries(state.Anomalies?.Flags)
            };
        }

        private static AdvancedMechanicState RestoreAdvancedMechanics(ScenarioAdvancedMechanicState snapshot)
        {
            var result = snapshot?.State?.Clone() ?? new AdvancedMechanicState();
            if (string.IsNullOrWhiteSpace(result.PendingChoice?.RequestId))
            {
                result.PendingChoice = null;
            }
            result.Trinkets = result.Trinkets ?? new PlayerTrinketState();
            result.Quests = result.Quests ?? new PlayerQuestState();
            result.Anomalies = result.Anomalies ?? new AnomalyState();
            result.Counters = RestoreIntEntries(snapshot?.Counters);
            result.Selections = RestoreStringEntries(snapshot?.Selections);
            result.Quests.RewardCounters = RestoreIntEntries(snapshot?.QuestRewardCounters);
            result.Quests.RewardFlags = RestoreBoolEntries(snapshot?.QuestRewardFlags);
            result.Anomalies.Counters = RestoreIntEntries(snapshot?.AnomalyCounters);
            result.Anomalies.Flags = RestoreStringEntries(snapshot?.AnomalyFlags);
            return result;
        }

        private static ScenarioPlayerDarkGiftState CapturePlayerDarkGifts(PlayerDarkGiftState state)
        {
            var snapshot = state?.Clone() ?? new PlayerDarkGiftState();
            return new ScenarioPlayerDarkGiftState
            {
                AcquiredGiftInstances = (snapshot.AcquiredGiftInstances ?? new List<PlayerDarkGiftInstance>())
                    .Where(item => item != null)
                    .Select(item => new ScenarioDarkGiftInstanceState
                    {
                        InstanceId = item.InstanceId,
                        DefinitionRevisionId = item.DefinitionRevisionId,
                        AcquiredRound = Math.Max(0, item.AcquiredRound),
                        Source = item.Source,
                        StackCount = Math.Max(0, item.StackCount),
                        RemainingUses = Math.Max(0, item.RemainingUses),
                        Cooldown = Math.Max(0, item.Cooldown),
                        Active = item.Active,
                        Suppressed = item.Suppressed,
                        Expired = item.Expired
                    })
                    .ToList(),
                Counters = CaptureIntEntries(snapshot.Counters),
                Cooldowns = CaptureIntEntries(snapshot.Cooldowns),
                TriggerHistory = CaptureMechanicEvents(snapshot.TriggerHistory?.Events)
            };
        }

        private static PlayerDarkGiftState RestorePlayerDarkGifts(ScenarioPlayerDarkGiftState snapshot)
        {
            return new PlayerDarkGiftState
            {
                AcquiredGiftInstances = (snapshot?.AcquiredGiftInstances ?? new List<ScenarioDarkGiftInstanceState>())
                    .Where(item => item != null)
                    .Select(item => new PlayerDarkGiftInstance
                    {
                        InstanceId = item.InstanceId,
                        DefinitionRevisionId = item.DefinitionRevisionId,
                        AcquiredRound = Math.Max(0, item.AcquiredRound),
                        Source = item.Source,
                        StackCount = Math.Max(0, item.StackCount),
                        RemainingUses = Math.Max(0, item.RemainingUses),
                        Cooldown = Math.Max(0, item.Cooldown),
                        Active = item.Active,
                        Suppressed = item.Suppressed,
                        Expired = item.Expired
                    })
                    .ToList(),
                Counters = RestoreIntEntries(snapshot?.Counters),
                Cooldowns = RestoreIntEntries(snapshot?.Cooldowns),
                TriggerHistory = new DarkGiftTriggerHistory
                {
                    Events = RestoreMechanicEvents(snapshot?.TriggerHistory)
                }
            };
        }

        private static ScenarioChoiceQueueState CaptureChoiceQueue(ChoiceQueueState queue)
        {
            queue = ChoiceQueueService.Normalize(queue?.Clone() ?? new ChoiceQueueState());
            return new ScenarioChoiceQueueState
            {
                HasActiveChoice = queue.ActiveChoice != null,
                ActiveChoice = CaptureChoiceQueueItem(queue.ActiveChoice),
                PendingChoices = queue.PendingChoices.ConvertAll(CaptureChoiceQueueItem),
                CompletedRequestIds = CopyList(queue.CompletedRequestIds),
                NextSequence = queue.NextSequence
            };
        }

        private static List<ScenarioRecruitActionState> CaptureRecruitActionStates(IEnumerable<RecruitActionState> states)
        {
            return (states ?? Enumerable.Empty<RecruitActionState>())
                .Where(state => state != null)
                .Select(state => new ScenarioRecruitActionState
                {
                    SourceInstanceId = state.SourceInstanceId,
                    UsesThisTurn = Math.Max(0, state.UsesThisTurn),
                    LastUsedRound = Math.Max(0, state.LastUsedRound),
                    Cooldown = Math.Max(0, state.Cooldown),
                    LockedReason = state.LockedReason
                })
                .ToList();
        }

        private static List<RecruitActionState> RestoreRecruitActionStates(IEnumerable<ScenarioRecruitActionState> states)
        {
            return (states ?? Enumerable.Empty<ScenarioRecruitActionState>())
                .Where(state => state != null)
                .Select(state => new RecruitActionState
                {
                    SourceInstanceId = state.SourceInstanceId,
                    UsesThisTurn = Math.Max(0, state.UsesThisTurn),
                    LastUsedRound = Math.Max(0, state.LastUsedRound),
                    Cooldown = Math.Max(0, state.Cooldown),
                    LockedReason = state.LockedReason
                })
                .ToList();
        }

        private static List<ScenarioDelayedObjectState> CaptureDelayedObjectStates(IEnumerable<DelayedObjectState> states)
        {
            return (states ?? Enumerable.Empty<DelayedObjectState>())
                .Where(state => state != null)
                .Select(state => new ScenarioDelayedObjectState
                {
                    InstanceId = state.InstanceId,
                    DefinitionRevisionId = state.DefinitionRevisionId,
                    CreatedRound = Math.Max(0, state.CreatedRound),
                    RemainingTurns = Math.Max(0, state.RemainingTurns),
                    OpenResolverId = state.OpenResolverId,
                    Source = state.Source,
                    Opened = state.Opened
                })
                .ToList();
        }

        private static List<DelayedObjectState> RestoreDelayedObjectStates(IEnumerable<ScenarioDelayedObjectState> states)
        {
            return (states ?? Enumerable.Empty<ScenarioDelayedObjectState>())
                .Where(state => state != null)
                .Select(state => new DelayedObjectState
                {
                    InstanceId = state.InstanceId,
                    DefinitionRevisionId = state.DefinitionRevisionId,
                    CreatedRound = Math.Max(0, state.CreatedRound),
                    RemainingTurns = Math.Max(0, state.RemainingTurns),
                    OpenResolverId = state.OpenResolverId,
                    Source = state.Source,
                    Opened = state.Opened
                })
                .ToList();
        }

        private static List<ScenarioMechanicEventRecord> CaptureMechanicEvents(IEnumerable<MechanicEventRecord> events)
        {
            return (events ?? Enumerable.Empty<MechanicEventRecord>())
                .Where(item => item != null)
                .OrderBy(item => item.Sequence)
                .Select(item => new ScenarioMechanicEventRecord
                {
                    Sequence = Math.Max(1, item.Sequence),
                    Round = Math.Max(1, item.Round),
                    Phase = item.Phase,
                    Type = item.Type,
                    Source = item.Source,
                    Targets = CopyList(item.Targets),
                    Result = item.Result,
                    RequestId = item.RequestId
                })
                .ToList();
        }

        private static List<MechanicEventRecord> RestoreMechanicEvents(IEnumerable<ScenarioMechanicEventRecord> events)
        {
            return (events ?? Enumerable.Empty<ScenarioMechanicEventRecord>())
                .Where(item => item != null)
                .OrderBy(item => item.Sequence)
                .Select(item => new MechanicEventRecord
                {
                    Sequence = Math.Max(1, item.Sequence),
                    Round = Math.Max(1, item.Round),
                    Phase = item.Phase,
                    Type = item.Type,
                    Source = item.Source,
                    Targets = CopyList(item.Targets),
                    Result = item.Result,
                    RequestId = item.RequestId
                })
                .ToList();
        }

        private static ChoiceQueueState RestoreChoiceQueue(ScenarioChoiceQueueState snapshot)
        {
            snapshot = snapshot ?? new ScenarioChoiceQueueState();
            var result = new ChoiceQueueState
            {
                ActiveChoice = snapshot.HasActiveChoice ? RestoreChoiceQueueItem(snapshot.ActiveChoice) : null,
                PendingChoices = (snapshot.PendingChoices ?? new List<ScenarioChoiceQueueItem>())
                    .Where(item => item != null)
                    .Select(RestoreChoiceQueueItem)
                    .ToList(),
                CompletedRequestIds = CopyList(snapshot.CompletedRequestIds),
                NextSequence = Math.Max(1, snapshot.NextSequence)
            };
            return ChoiceQueueService.Normalize(result);
        }

        private static ScenarioChoiceQueueItem CaptureChoiceQueueItem(ChoiceQueueItem item)
        {
            if (item == null)
            {
                return null;
            }

            return new ScenarioChoiceQueueItem
            {
                RequestId = item.RequestId,
                Kind = item.Kind.ToString(),
                Source = item.Source,
                CreatedRound = item.CreatedRound,
                Sequence = item.Sequence,
                Priority = item.Priority,
                Blocking = item.Blocking,
                RemainingPicks = item.RemainingPicks,
                Options = (item.Options ?? new List<MechanicChoiceOption>()).ConvertAll(option => option?.Clone()),
                ResolutionMetadata = (item.ResolutionMetadata ?? new List<ChoiceResolutionMetadataEntry>())
                    .ConvertAll(entry => entry == null ? null : new ScenarioStringState { Key = entry.Key, Value = entry.Value }),
                Discover = CaptureDiscover(item.Discover)
            };
        }

        private static ChoiceQueueItem RestoreChoiceQueueItem(ScenarioChoiceQueueItem item)
        {
            if (item == null || !Enum.TryParse(item.Kind, true, out ChoiceRequestKind kind))
            {
                throw new InvalidOperationException("Scenario choice queue contains an invalid choice kind.");
            }

            return new ChoiceQueueItem
            {
                RequestId = item.RequestId,
                Kind = kind,
                Source = item.Source,
                CreatedRound = item.CreatedRound,
                Sequence = item.Sequence,
                Priority = item.Priority,
                Blocking = item.Blocking,
                RemainingPicks = item.RemainingPicks,
                Options = (item.Options ?? new List<MechanicChoiceOption>()).ConvertAll(option => option?.Clone()),
                ResolutionMetadata = (item.ResolutionMetadata ?? new List<ScenarioStringState>())
                    .ConvertAll(entry => entry == null ? null : new ChoiceResolutionMetadataEntry { Key = entry.Key, Value = entry.Value }),
                Discover = RestoreDiscover(item.Discover)
            };
        }

        private static ScenarioDiscoverState CaptureDiscover(DiscoverState discover)
        {
            if (discover == null)
            {
                return null;
            }

            return new ScenarioDiscoverState
            {
                Source = discover.Source,
                RewardTier = discover.RewardTier,
                TargetInstanceId = discover.TargetInstanceId,
                RemainingPicks = discover.RemainingPicks,
                AutoResolveRandomly = discover.AutoResolveRandomly,
                AutoResolveSeed = discover.AutoResolveSeed,
                ResolveAllOptions = discover.ResolveAllOptions,
                OptionTags = new List<string>(discover.OptionTags ?? new List<string>()),
                OptionCounters = (discover.OptionCounters ?? new List<DiscoverOptionCounterState>())
                    .Where(counter => counter != null)
                    .Select(counter => new ScenarioCounterState { Key = counter.Key, Value = counter.Value })
                    .ToList(),
                Options = CaptureCards(discover.Options)
            };
        }

        private static DiscoverState RestoreDiscover(ScenarioDiscoverState discover)
        {
            if (discover == null)
            {
                return null;
            }

            return new DiscoverState
            {
                Source = discover.Source,
                RewardTier = discover.RewardTier,
                TargetInstanceId = discover.TargetInstanceId,
                RemainingPicks = discover.RemainingPicks,
                AutoResolveRandomly = discover.AutoResolveRandomly,
                AutoResolveSeed = discover.AutoResolveSeed,
                ResolveAllOptions = discover.ResolveAllOptions,
                OptionTags = new List<string>(discover.OptionTags ?? new List<string>()),
                OptionCounters = (discover.OptionCounters ?? new List<ScenarioCounterState>())
                    .Where(counter => counter != null)
                    .Select(counter => new DiscoverOptionCounterState { Key = counter.Key, Value = counter.Value })
                    .ToList(),
                Options = (discover.Options ?? new List<ScenarioCardState>())
                    .Where(card => card != null)
                    .Select(card => RestoreCard(card, card.Owner))
                    .ToList()
            };
        }

        private static List<ScenarioCounterState> CaptureIntEntries(IEnumerable<KeyValuePair<string, int>> entries)
        {
            return (entries ?? Enumerable.Empty<KeyValuePair<string, int>>())
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new ScenarioCounterState { Key = entry.Key, Value = entry.Value })
                .ToList();
        }

        private static List<ScenarioStringState> CaptureStringEntries(IEnumerable<KeyValuePair<string, string>> entries)
        {
            return (entries ?? Enumerable.Empty<KeyValuePair<string, string>>())
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new ScenarioStringState { Key = entry.Key, Value = entry.Value })
                .ToList();
        }

        private static List<ScenarioBoolState> CaptureBoolEntries(IEnumerable<KeyValuePair<string, bool>> entries)
        {
            return (entries ?? Enumerable.Empty<KeyValuePair<string, bool>>())
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new ScenarioBoolState { Key = entry.Key, Value = entry.Value })
                .ToList();
        }

        private static Dictionary<string, int> RestoreIntEntries(IEnumerable<ScenarioCounterState> entries)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in entries ?? Enumerable.Empty<ScenarioCounterState>())
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                {
                    result[entry.Key] = entry.Value;
                }
            }
            return result;
        }

        private static Dictionary<string, string> RestoreStringEntries(IEnumerable<ScenarioStringState> entries)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in entries ?? Enumerable.Empty<ScenarioStringState>())
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                {
                    result[entry.Key] = entry.Value;
                }
            }
            return result;
        }

        private static Dictionary<string, bool> RestoreBoolEntries(IEnumerable<ScenarioBoolState> entries)
        {
            var result = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var entry in entries ?? Enumerable.Empty<ScenarioBoolState>())
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                {
                    result[entry.Key] = entry.Value;
                }
            }
            return result;
        }

        private static List<T> CopyList<T>(IEnumerable<T> values)
        {
            return values == null ? new List<T>() : new List<T>(values);
        }

        private static SideCombatModifierState CapturePlayerCombatModifiers(TavernState tavern, SideCombatModifierState existing)
        {
            var snapshot = CloneModifiers(existing) ?? new SideCombatModifierState();
            if (tavern == null)
            {
                return snapshot;
            }

            snapshot.SpellsCastThisGame = Math.Max(0, tavern.TavernSpellsCastThisGame);
            snapshot.SpellPower = Math.Max(0, tavern.SpellPower);
            snapshot.TavernSpellBonusAttack = Math.Max(0, tavern.TavernSpellBonusAttack);
            snapshot.TavernSpellBonusHealth = Math.Max(0, tavern.TavernSpellBonusHealth);
            snapshot.BloodGemAttackBonus = Math.Max(0, tavern.BloodGemBonusAttack);
            snapshot.BloodGemHealthBonus = Math.Max(0, tavern.BloodGemBonusHealth);
            snapshot.BeetleAttackBonus = Math.Max(2, tavern.BeetleAttackBonus);
            snapshot.BeetleHealthBonus = Math.Max(2, tavern.BeetleHealthBonus);
            snapshot.UndeadAttackBonus = Math.Max(0, tavern.UndeadAttackBonus);
            snapshot.EternalKnightDeaths = Math.Max(0, tavern.EternalKnightDeaths);
            snapshot.AstralAutomatonSummons = Math.Max(0, tavern.AncestralAutomatonSummons);
            snapshot.FriendlyMinionDeathsThisGame = Math.Max(0, tavern.FriendlyMinionDeathsThisGame);
            return snapshot;
        }

        private static SideCombatModifierState CloneModifiers(SideCombatModifierState modifiers)
        {
            if (modifiers == null)
            {
                return null;
            }

            return new SideCombatModifierState
            {
                SpellsCastThisGame = Math.Max(0, modifiers.SpellsCastThisGame),
                SpellPower = Math.Max(0, modifiers.SpellPower),
                TavernSpellBonusAttack = Math.Max(0, modifiers.TavernSpellBonusAttack),
                TavernSpellBonusHealth = Math.Max(0, modifiers.TavernSpellBonusHealth),
                BloodGemAttackBonus = Math.Max(0, modifiers.BloodGemAttackBonus),
                BloodGemHealthBonus = Math.Max(0, modifiers.BloodGemHealthBonus),
                BeetleAttackBonus = Math.Max(2, modifiers.BeetleAttackBonus),
                BeetleHealthBonus = Math.Max(2, modifiers.BeetleHealthBonus),
                UndeadAttackBonus = Math.Max(0, modifiers.UndeadAttackBonus),
                EternalKnightDeaths = Math.Max(0, modifiers.EternalKnightDeaths),
                AstralAutomatonSummons = Math.Max(0, modifiers.AstralAutomatonSummons),
                FriendlyMinionDeathsThisGame = Math.Max(0, modifiers.FriendlyMinionDeathsThisGame)
            };
        }

        private static void ApplyPlayerModifiersToTavern(SideCombatModifierState modifiers, TavernState tavern)
        {
            if (modifiers == null || tavern == null)
            {
                return;
            }

            tavern.TavernSpellsCastThisGame = Math.Max(0, modifiers.SpellsCastThisGame);
            tavern.SpellPower = Math.Max(0, modifiers.SpellPower);
            tavern.TavernSpellBonusAttack = Math.Max(0, modifiers.TavernSpellBonusAttack);
            tavern.TavernSpellBonusHealth = Math.Max(0, modifiers.TavernSpellBonusHealth);
            tavern.BloodGemBonusAttack = Math.Max(0, modifiers.BloodGemAttackBonus);
            tavern.BloodGemBonusHealth = Math.Max(0, modifiers.BloodGemHealthBonus);
            tavern.BeetleAttackBonus = Math.Max(2, modifiers.BeetleAttackBonus);
            tavern.BeetleHealthBonus = Math.Max(2, modifiers.BeetleHealthBonus);
            tavern.UndeadAttackBonus = Math.Max(0, modifiers.UndeadAttackBonus);
            tavern.EternalKnightDeaths = Math.Max(0, modifiers.EternalKnightDeaths);
            tavern.AncestralAutomatonSummons = Math.Max(0, modifiers.AstralAutomatonSummons);
            tavern.FriendlyMinionDeathsThisGame = Math.Max(0, modifiers.FriendlyMinionDeathsThisGame);
        }

        private static List<ScenarioCardState> CaptureCards(IEnumerable<MinionInstance> cards)
        {
            return cards == null
                ? new List<ScenarioCardState>()
                : cards.Where(card => card != null).Select(CaptureCard).ToList();
        }

        private static ScenarioCardState CaptureCard(MinionInstance card)
        {
            return new ScenarioCardState
            {
                CardKind = card.CardKind,
                InstanceId = card.InstanceId,
                DefinitionId = card.DefinitionId,
                CardId = card.CardId,
                Name = card.Name,
                Cost = card.Cost,
                BaseAttack = card.BaseAttack,
                BaseHealth = card.BaseHealth,
                Attack = card.Attack,
                Health = card.Health,
                MaxHealth = card.MaxHealth,
                TavernTier = card.TavernTier,
                Tribes = new List<Tribe>(card.Tribes),
                Keywords = new List<Keyword>(card.Keywords),
                OfficialKeywords = card.OfficialKeywords == null ? new List<Keyword>() : new List<Keyword>(card.OfficialKeywords),
                Text = card.Text,
                Golden = card.Golden,
                Owner = card.Owner,
                Enchantments = card.Enchantments.Select(enchantment => new ScenarioEnchantmentState
                {
                    Id = enchantment.Id,
                    SourceId = enchantment.SourceId,
                    AttackBonus = enchantment.AttackBonus,
                    HealthBonus = enchantment.HealthBonus,
                    AddedKeywords = new List<Keyword>(enchantment.AddedKeywords),
                    Duration = enchantment.Duration
                }).ToList(),
                Counters = card.Counters.Select(counter => new ScenarioCounterState { Key = counter.Key, Value = counter.Value }).ToList(),
                CanAttack = card.CanAttack,
                AttacksThisCombat = card.AttacksThisCombat,
                OriginPoolSource = card.OriginPoolSource,
                CanReturnToPoolAfterAttach = card.CanReturnToPoolAfterAttach,
                PoolSource = card.PoolSource,
                PoolCopiesHeld = card.PoolCopiesHeld,
                ImagePath = card.ImagePath,
                EffectIds = new List<string>(card.EffectIds),
                Tags = card.Tags == null ? new List<string>() : new List<string>(card.Tags)
            };
        }

        private static List<MinionInstance> RestoreCards(IEnumerable<ScenarioCardState> cards, BoardSide owner)
        {
            return cards == null
                ? new List<MinionInstance>()
                : cards.Where(card => card != null).Select(card => RestoreCard(card, owner)).ToList();
        }

        private static MinionInstance RestoreCard(ScenarioCardState card, BoardSide owner)
        {
            var maxHealth = Math.Max(1, card.MaxHealth);
            var health = Math.Max(1, Math.Min(card.Health, maxHealth));
            if (card.CardKind == CardKind.TavernSpell || card.CardKind == CardKind.Spell)
            {
                maxHealth = 0;
                health = 0;
            }

            return new MinionInstance
            {
                CardKind = card.CardKind,
                InstanceId = string.IsNullOrEmpty(card.InstanceId) ? owner.ToString().ToLowerInvariant() + "-" + card.DefinitionId + "-scenario" : card.InstanceId,
                DefinitionId = card.DefinitionId,
                CardId = card.CardId,
                Name = card.Name,
                Cost = card.Cost,
                BaseAttack = Math.Max(0, card.BaseAttack),
                BaseHealth = Math.Max(0, card.BaseHealth),
                Attack = Math.Max(0, card.Attack),
                Health = health,
                MaxHealth = maxHealth,
                TavernTier = Math.Max(0, card.TavernTier),
                Tribes = card.Tribes == null ? new List<Tribe> { Tribe.None } : new List<Tribe>(card.Tribes),
                Keywords = card.Keywords == null ? new List<Keyword>() : new List<Keyword>(card.Keywords),
                OfficialKeywords = card.OfficialKeywords == null ? new List<Keyword>() : new List<Keyword>(card.OfficialKeywords),
                Text = card.Text,
                Golden = card.Golden,
                Owner = owner,
                Enchantments = card.Enchantments == null
                    ? new List<Enchantment>()
                    : card.Enchantments.Select(enchantment => new Enchantment
                    {
                        Id = enchantment.Id,
                        SourceId = enchantment.SourceId,
                        AttackBonus = enchantment.AttackBonus,
                        HealthBonus = enchantment.HealthBonus,
                        AddedKeywords = enchantment.AddedKeywords == null ? new List<Keyword>() : new List<Keyword>(enchantment.AddedKeywords),
                        Duration = enchantment.Duration
                    }).ToList(),
                Counters = card.Counters == null ? new Dictionary<string, int>() : card.Counters.ToDictionary(counter => counter.Key, counter => counter.Value),
                CanAttack = card.CanAttack,
                AttacksThisCombat = card.AttacksThisCombat,
                OriginPoolSource = card.OriginPoolSource,
                CanReturnToPoolAfterAttach = card.CanReturnToPoolAfterAttach,
                PoolSource = card.PoolSource,
                PoolCopiesHeld = card.PoolCopiesHeld,
                ImagePath = card.ImagePath,
                EffectIds = card.EffectIds == null ? new List<string>() : new List<string>(card.EffectIds),
                Tags = card.Tags == null ? new List<string>() : new List<string>(card.Tags)
            };
        }
    }
}
