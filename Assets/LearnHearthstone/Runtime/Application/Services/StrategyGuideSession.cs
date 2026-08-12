using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public enum StrategyGuideRunState
    {
        Playing,
        CombatFailed,
        Completed,
        FreeExplore
    }

    public sealed class StrategyGuideActionProgress
    {
        public string ActionId;
        public int CompletedCount;
        public int RequiredCount;
        public string Instruction;
        public string EnglishInstruction;

        public bool IsComplete => CompletedCount >= RequiredCount;
    }

    public sealed class StrategyGuideGrowthProgress
    {
        public string Key;
        public int CurrentValue;
        public int RequiredValue;

        public bool IsComplete => CurrentValue >= RequiredValue;
    }

    public enum StrategyGuideFinalSlotStatus
    {
        Complete,
        PositionWrong,
        StateMismatch,
        Missing
    }

    public sealed class StrategyGuideFinalSlotProgress
    {
        public int SlotIndex;
        public StrategyGuideCardDefinition Target;
        public int MatchedBoardIndex = -1;
        public MinionInstance Actual;
        public StrategyGuideFinalSlotStatus Status = StrategyGuideFinalSlotStatus.Missing;
    }

    public sealed class StrategyGuideEvaluation
    {
        public int MatchedFinalSlots;
        public int FinalSlotCount;
        public bool FinalCompositionComplete;
        public int CompletedActionCount;
        public int RequiredActionCount;
        public bool RequiredActionsComplete;
        public int CompletedGrowthCount;
        public int RequiredGrowthCount;
        public bool GrowthQualityComplete;
        public bool CombatWon;
        public bool IsComplete;
    }

    public sealed class StrategyGuideSessionResult
    {
        public bool Succeeded;
        public string Code;
        public string Message;

        public static StrategyGuideSessionResult Success(string code)
        {
            return new StrategyGuideSessionResult { Succeeded = true, Code = code };
        }

        public static StrategyGuideSessionResult Failure(string code, string message)
        {
            return new StrategyGuideSessionResult { Succeeded = false, Code = code, Message = message };
        }
    }

    public static class StrategyGuideObjectiveEvaluator
    {
        public static IReadOnlyList<StrategyGuideFinalSlotProgress> EvaluateFinalSlots(
            StrategyGuideDefinition guide,
            MatchState state)
        {
            var targets = guide?.FinalComposition ?? new List<StrategyGuideCardDefinition>();
            var board = state?.Player?.Board ?? new List<MinionInstance>();
            var progress = targets.Select((target, index) => new StrategyGuideFinalSlotProgress
            {
                SlotIndex = index,
                Target = target
            }).ToList();
            var usedBoardCards = new bool[board.Count];

            for (var index = 0; index < progress.Count && index < board.Count; index += 1)
            {
                if (MatchesRequiredState(progress[index].Target, board[index]))
                {
                    Assign(progress[index], board[index], index, StrategyGuideFinalSlotStatus.Complete, usedBoardCards);
                }
            }

            for (var index = 0; index < progress.Count && index < board.Count; index += 1)
            {
                if (progress[index].Actual == null && MatchesCardId(progress[index].Target, board[index]))
                {
                    Assign(progress[index], board[index], index, StrategyGuideFinalSlotStatus.StateMismatch, usedBoardCards);
                }
            }

            AssignCardsFromOtherPositions(
                progress,
                board,
                usedBoardCards,
                MatchesRequiredState,
                StrategyGuideFinalSlotStatus.PositionWrong);
            AssignCardsFromOtherPositions(
                progress,
                board,
                usedBoardCards,
                MatchesCardId,
                StrategyGuideFinalSlotStatus.StateMismatch);
            return progress;
        }

        public static int CountMatchedFinalSlots(StrategyGuideDefinition guide, MatchState state)
        {
            return CountMatchedFinalSlots(EvaluateFinalSlots(guide, state));
        }

        public static bool IsFinalCompositionComplete(StrategyGuideDefinition guide, MatchState state)
        {
            var progress = EvaluateFinalSlots(guide, state);
            return IsFinalCompositionComplete(progress, state?.Player?.Board?.Count ?? -1);
        }

        internal static int CountMatchedFinalSlots(IReadOnlyList<StrategyGuideFinalSlotProgress> progress)
        {
            return progress?.Count(item => item.Status == StrategyGuideFinalSlotStatus.Complete) ?? 0;
        }

        internal static bool IsFinalCompositionComplete(
            IReadOnlyList<StrategyGuideFinalSlotProgress> progress,
            int boardCount)
        {
            return progress != null &&
                progress.Count > 0 &&
                boardCount == progress.Count &&
                progress.All(item => item.Status == StrategyGuideFinalSlotStatus.Complete);
        }

        private static void AssignCardsFromOtherPositions(
            IReadOnlyList<StrategyGuideFinalSlotProgress> progress,
            IReadOnlyList<MinionInstance> board,
            bool[] usedBoardCards,
            Func<StrategyGuideCardDefinition, MinionInstance, bool> matches,
            StrategyGuideFinalSlotStatus status)
        {
            foreach (var slot in progress.Where(item => item.Actual == null))
            {
                for (var boardIndex = 0; boardIndex < board.Count; boardIndex += 1)
                {
                    if (usedBoardCards[boardIndex] || boardIndex == slot.SlotIndex || !matches(slot.Target, board[boardIndex]))
                    {
                        continue;
                    }

                    Assign(slot, board[boardIndex], boardIndex, status, usedBoardCards);
                    break;
                }
            }
        }

        private static void Assign(
            StrategyGuideFinalSlotProgress slot,
            MinionInstance card,
            int boardIndex,
            StrategyGuideFinalSlotStatus status,
            bool[] usedBoardCards)
        {
            slot.Actual = card;
            slot.MatchedBoardIndex = boardIndex;
            slot.Status = status;
            usedBoardCards[boardIndex] = true;
        }

        private static bool MatchesRequiredState(StrategyGuideCardDefinition target, MinionInstance card)
        {
            return MatchesCardId(target, card) &&
                card.Golden == target.Golden &&
                card.Attack >= target.MinimumAttack &&
                card.Health >= target.MinimumHealth;
        }

        private static bool MatchesCardId(StrategyGuideCardDefinition target, MinionInstance card)
        {
            return target != null &&
                card != null &&
                string.Equals(card.CardId, target.CardId, StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class StrategyGuideSession
    {
        private const string PlacementTagPrefix = "strategy-guide-placement:";
        private const string TripleRewardDefinitionId = "triple-reward";
        private const string OfferOccurrencePrefix = "strategy-guide:offer-occurrence:";
        private const string ActiveOfferScheduleKey = "strategy-guide:active-offer-schedule";
        private const string LobsterGrowthCounter = "season14_min_r30_lobster_growth";

        private readonly TestScenarioDefinition initialScenario;
        private readonly Dictionary<string, int> actionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private UndoSnapshot undoSnapshot;
        private bool combatWon;

        private StrategyGuideSession(
            MatchService matchService,
            StrategyGuideDefinition guide,
            StrategyGuideEntryProfileDefinition profile,
            TestScenarioDefinition scenario)
        {
            MatchService = matchService;
            Guide = guide;
            Profile = profile;
            initialScenario = TestScenarioMapper.Clone(scenario);
            UndoUsesRemaining = Math.Max(0, profile.Undo.UsesPerRun);
            ResetActionCounts();
            Synchronize();
        }

        public MatchService MatchService { get; }
        public StrategyGuideDefinition Guide { get; }
        public StrategyGuideEntryProfileDefinition Profile { get; }
        public StrategyGuideRunState RunState { get; private set; }
        public int UndoUsesRemaining { get; private set; }
        public StrategyGuideEvaluation Evaluation { get; private set; } = new StrategyGuideEvaluation();
        public IReadOnlyList<StrategyGuideFinalSlotProgress> FinalSlotProgress { get; private set; } =
            Array.Empty<StrategyGuideFinalSlotProgress>();

        public StrategyGuideOfferScheduleDefinition ActiveOfferSchedule
        {
            get
            {
                var selections = MatchService.State.Player.Tavern.AdvancedMechanics?.Selections;
                if (selections == null || !selections.TryGetValue(ActiveOfferScheduleKey, out var scheduleId))
                {
                    return null;
                }

                return (Profile.AcquisitionPlan?.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                    .FirstOrDefault(item => item != null && string.Equals(item.ScheduleId, scheduleId, StringComparison.Ordinal));
            }
        }

        public int RemainingOfferScheduleCount => OfferSchedules.Count(schedule => !IsOfferScheduleTriggered(schedule));

        public int TotalOfferScheduleCount => OfferSchedules.Count;

        public StrategyGuideOfferScheduleDefinition CurrentOfferSchedule =>
            ActiveOfferSchedule ?? OfferSchedules.FirstOrDefault(schedule => !IsOfferScheduleTriggered(schedule));

        public string AcquisitionStatus(bool useEnglish)
        {
            if (Profile.AcquisitionPlan?.DiscloseControlledOffers != true || TotalOfferScheduleCount == 0)
            {
                return string.Empty;
            }

            var schedule = CurrentOfferSchedule;
            if (schedule == null)
            {
                return useEnglish ? "Controlled offers complete" : "受控发牌已完成";
            }

            var policy = OfferPolicyLabel(schedule.Policy, useEnglish);
            var state = ActiveOfferSchedule == schedule
                ? (useEnglish ? "triggered" : "已触发")
                : (useEnglish ? "pending" : "待触发");
            var label = useEnglish && !string.IsNullOrWhiteSpace(schedule.EnglishLabel)
                ? schedule.EnglishLabel
                : schedule.Label;
            return (useEnglish ? "Controlled offer" : "受控发牌") +
                   " · " + policy +
                   " · " + state + " " + RemainingOfferScheduleCount + "/" + TotalOfferScheduleCount +
                   (string.IsNullOrWhiteSpace(label) ? string.Empty : " · " + label);
        }

        public IReadOnlyList<StrategyGuideActionProgress> ActionProgress =>
            (Profile.RequiredActions ?? new List<StrategyGuideRequiredAction>())
                .Where(item => item != null)
                .Select(item => new StrategyGuideActionProgress
                {
                    ActionId = item.ActionId,
                    CompletedCount = actionCounts.TryGetValue(item.ActionId, out var count) ? count : 0,
                    RequiredCount = Math.Max(1, item.Count),
                    Instruction = item.Instruction,
                    EnglishInstruction = item.EnglishInstruction
                })
                .ToList();

        public IReadOnlyList<StrategyGuideGrowthProgress> GrowthProgress =>
            (Profile.GrowthQuality ?? new List<StrategyGuideGrowthValue>())
                .Where(item => item != null)
                .Select(item => new StrategyGuideGrowthProgress
                {
                    Key = item.Key,
                    CurrentValue = ReadGrowthValue(item.Key),
                    RequiredValue = item.Value
                })
                .ToList();

        private List<StrategyGuideOfferScheduleDefinition> OfferSchedules =>
            (Profile.AcquisitionPlan?.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                .Where(schedule => schedule != null)
                .ToList();

        private bool IsOfferScheduleTriggered(StrategyGuideOfferScheduleDefinition schedule)
        {
            var counters = MatchService.State.Player.Tavern.AdvancedMechanics?.Counters;
            if (schedule == null || counters == null)
            {
                return false;
            }

            var tierScope = schedule.TriggerTavernTier > 0 ? schedule.TriggerTavernTier : 0;
            var occurrenceKey = OfferOccurrencePrefix + Profile.ProfileId + ":" + schedule.Source + ":" +
                                (string.IsNullOrWhiteSpace(schedule.TriggerCardId) ? "*" : schedule.TriggerCardId) +
                                ":" + tierScope;
            return counters.TryGetValue(occurrenceKey, out var completedOccurrences) &&
                   completedOccurrences >= Math.Max(1, schedule.TriggerOccurrence);
        }

        private static string OfferPolicyLabel(string policy, bool useEnglish)
        {
            if (string.Equals(policy, StrategyGuideOfferPolicies.Pinned, StringComparison.Ordinal))
            {
                return useEnglish ? "Pinned" : "固定候选";
            }

            if (string.Equals(policy, StrategyGuideOfferPolicies.MustIncludeAny, StringComparison.Ordinal))
            {
                return useEnglish ? "Include one recommendation" : "推荐中至少出现一个";
            }

            if (string.Equals(policy, StrategyGuideOfferPolicies.MustInclude, StringComparison.Ordinal))
            {
                return useEnglish ? "Must include" : "必含目标";
            }

            return useEnglish ? "Seeded odds" : "固定种子概率";
        }

        public bool CanUndo =>
            UndoUsesRemaining > 0 &&
            undoSnapshot != null &&
            RunState != StrategyGuideRunState.FreeExplore &&
            MatchService.State.Phase == MatchPhase.Tavern;

        public static StrategyGuideSession Start(
            StrategyGuideCatalog catalog,
            string guideId,
            ResolvedGameVersion version,
            bool useEnglish = false,
            string profileId = null)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var guide = catalog.GetGuide(guideId);
            var profile = string.IsNullOrWhiteSpace(profileId)
                ? catalog.GetDefaultProfile(guideId)
                : catalog.GetProfile(guideId, profileId);
            var compiled = StrategyGuideScenarioCompiler.Compile(catalog, guide, version, useEnglish, profile.ProfileId);
            var service = StrategyGuideScenarioCompiler.CreateRuntimeService(version, guide, useEnglish, profile.ProfileId);
            compiled.Scenario.Tavern.GuideCoreSpellCardNumbers = (guide.CoreSpellCardNumbers ?? new List<string>())
                .Where(cardNumber => !string.IsNullOrWhiteSpace(cardNumber))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            TestScenarioMapper.ApplyTo(service.State, compiled.Scenario);
            return new StrategyGuideSession(service, guide, profile, compiled.Scenario);
        }

        public bool CanApply(GameCommandType commandType)
        {
            if (commandType == GameCommandType.MoveShopCard)
            {
                return MatchService.CanApply(commandType);
            }
            if (commandType == GameCommandType.UseGuideShapingSpell)
            {
                return MatchService.CanApply(commandType) && HasCurrentShapingSpellSlot();
            }
            if (RunState == StrategyGuideRunState.FreeExplore)
            {
                return MatchService.CanApply(commandType);
            }

            return MatchService.CanApply(commandType) &&
                (Profile.AllowedCommands ?? new List<string>())
                    .Contains(commandType.ToString(), StringComparer.Ordinal);
        }

        public MatchState Apply(GameCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            if (!CanApply(command.Type))
            {
                throw new InvalidOperationException("Command is not allowed by this strategy guide: " + command.Type + ".");
            }
            if (command.Type == GameCommandType.MoveShopCard)
            {
                return MatchService.Apply(command);
            }

            var context = CaptureCommandContext(command, MatchService.State);
            var locksUndo = LocksUndo(command.Type);
            var before = !locksUndo && UndoUsesRemaining > 0
                ? new UndoSnapshot
                {
                    Scenario = TestScenarioMapper.Capture(
                        MatchService.State,
                        Guide.RevisionId + "#" + Profile.ProfileId + "-undo"),
                    ActionCounts = new Dictionary<string, int>(actionCounts, StringComparer.Ordinal),
                    CombatWon = combatWon,
                    RunState = RunState
                }
                : null;
            var acquisitionBefore = CanTriggerAcquisition(context)
                ? TestScenarioMapper.Capture(
                    MatchService.State,
                    Guide.RevisionId + "#" + Profile.ProfileId + "-acquisition-atomic")
                : null;

            MatchState result;
            try
            {
                result = MatchService.Apply(command);
                ApplyAcquisitionPlan(context);
            }
            catch
            {
                if (acquisitionBefore != null)
                {
                    TestScenarioMapper.ApplyTo(MatchService.State, acquisitionBefore);
                }
                throw;
            }
            var commandSucceeded = command.Type != GameCommandType.UseRecruitAction ||
                MatchService.LastRecruitActionResult?.Succeeded == true;
            if (commandSucceeded && before != null)
            {
                undoSnapshot = before;
            }
            if (locksUndo)
            {
                undoSnapshot = null;
            }

            if (commandSucceeded)
            {
                RecordCommand(context);
            }
            Synchronize();
            return result;
        }

        public StrategyGuideSessionResult Undo()
        {
            if (UndoUsesRemaining <= 0)
            {
                return StrategyGuideSessionResult.Failure("guide.undo.used", "Undo has already been used.");
            }
            if (RunState == StrategyGuideRunState.FreeExplore || MatchService.State.Phase != MatchPhase.Tavern)
            {
                return StrategyGuideSessionResult.Failure("guide.undo.locked", "Undo is locked after turn end or combat.");
            }
            if (undoSnapshot == null)
            {
                return StrategyGuideSessionResult.Failure("guide.undo.unavailable", "There is no completed action to undo.");
            }

            TestScenarioMapper.ApplyTo(MatchService.State, undoSnapshot.Scenario);
            actionCounts.Clear();
            foreach (var item in undoSnapshot.ActionCounts)
            {
                actionCounts[item.Key] = item.Value;
            }
            combatWon = undoSnapshot.CombatWon;
            RunState = undoSnapshot.RunState;
            UndoUsesRemaining -= 1;
            undoSnapshot = null;
            Synchronize();
            return StrategyGuideSessionResult.Success("guide.undo.applied");
        }

        public StrategyGuideSessionResult Restart()
        {
            TestScenarioMapper.ApplyTo(MatchService.State, initialScenario);
            UndoUsesRemaining = Math.Max(0, Profile.Undo.UsesPerRun);
            undoSnapshot = null;
            combatWon = false;
            RunState = StrategyGuideRunState.Playing;
            ResetActionCounts();
            Synchronize();
            return StrategyGuideSessionResult.Success("guide.restart.applied");
        }

        public StrategyGuideSessionResult EnterFreeExplore()
        {
            if (RunState != StrategyGuideRunState.Completed)
            {
                return StrategyGuideSessionResult.Failure("guide.free-explore.locked", "Complete the guide and win its combat first.");
            }

            RunState = StrategyGuideRunState.FreeExplore;
            undoSnapshot = null;
            SynchronizeShapingSpellSlot();
            return StrategyGuideSessionResult.Success("guide.free-explore.entered");
        }

        public void Synchronize()
        {
            SynchronizeShapingSpellSlot();
            var activeSchedule = ActiveOfferSchedule;
            var keepsActiveTrinketSchedule = string.Equals(
                activeSchedule?.Source,
                StrategyGuideOfferSources.GreaterTrinketChoice,
                StringComparison.Ordinal) && HasActiveScheduledGreaterTrinketChoice();
            if (MatchService.State.Player.Tavern.Discover == null &&
                !string.Equals(activeSchedule?.Source, StrategyGuideOfferSources.ShopRefresh, StringComparison.Ordinal) &&
                !keepsActiveTrinketSchedule)
            {
                MatchService.State.Player.Tavern.AdvancedMechanics?.Selections?.Remove(ActiveOfferScheduleKey);
            }
            SynchronizeStateActions();
            if (MatchService.State.Phase == MatchPhase.Result &&
                MatchService.State.LastResult?.Winner == CombatWinner.Player)
            {
                combatWon = true;
            }

            var progress = ActionProgress;
            var growthProgress = GrowthProgress;
            FinalSlotProgress = StrategyGuideObjectiveEvaluator.EvaluateFinalSlots(Guide, MatchService.State);
            var finalSlots = FinalSlotProgress.Count;
            var matchedSlots = StrategyGuideObjectiveEvaluator.CountMatchedFinalSlots(FinalSlotProgress);
            var actionsComplete = progress.All(item => item.IsComplete);
            var growthComplete = growthProgress.All(item => item.IsComplete);
            var finalComplete = StrategyGuideObjectiveEvaluator.IsFinalCompositionComplete(
                FinalSlotProgress,
                MatchService.State.Player.Board.Count);
            Evaluation = new StrategyGuideEvaluation
            {
                MatchedFinalSlots = matchedSlots,
                FinalSlotCount = finalSlots,
                FinalCompositionComplete = finalComplete,
                CompletedActionCount = progress.Count(item => item.IsComplete),
                RequiredActionCount = progress.Count,
                RequiredActionsComplete = actionsComplete,
                CompletedGrowthCount = growthProgress.Count(item => item.IsComplete),
                RequiredGrowthCount = growthProgress.Count,
                GrowthQualityComplete = growthComplete,
                CombatWon = combatWon,
                IsComplete = (!Profile.Victory.RequireFinalComposition || finalComplete) &&
                    actionsComplete &&
                    growthComplete &&
                    (!Profile.Victory.RequireCombatWin || combatWon)
            };

            if (RunState == StrategyGuideRunState.FreeExplore)
            {
                return;
            }
            if (Evaluation.IsComplete)
            {
                RunState = StrategyGuideRunState.Completed;
            }
            else if (MatchService.State.Phase == MatchPhase.Result && MatchService.State.LastResult != null)
            {
                RunState = StrategyGuideRunState.CombatFailed;
            }
            else
            {
                RunState = StrategyGuideRunState.Playing;
            }
        }

        public string NextInstruction(bool useEnglish)
        {
            var next = ActionProgress.FirstOrDefault(item => !item.IsComplete);
            if (next != null)
            {
                return useEnglish && !string.IsNullOrWhiteSpace(next.EnglishInstruction)
                    ? next.EnglishInstruction
                    : next.Instruction;
            }
            if (!Evaluation.FinalCompositionComplete)
            {
                return useEnglish
                    ? "Complete the final warband in the shown order."
                    : "按目标顺序补齐最终战队。";
            }
            if (!Evaluation.CombatWon)
            {
                return RunState == StrategyGuideRunState.CombatFailed
                    ? useEnglish ? "Combat failed. Restart and adjust before trying again." : "验收战斗未通过，重开后调整再试。"
                    : useEnglish ? "End the turn and win the validation combat." : "结束回合并赢下验收战斗。";
            }
            return useEnglish ? "Guide complete." : "攻略已完成。";
        }

        private void ResetActionCounts()
        {
            actionCounts.Clear();
            foreach (var action in Profile.RequiredActions ?? new List<StrategyGuideRequiredAction>())
            {
                if (action != null && !string.IsNullOrWhiteSpace(action.ActionId))
                {
                    actionCounts[action.ActionId] = 0;
                }
            }
        }

        private int ReadGrowthValue(string key)
        {
            var tavern = MatchService.State?.Player?.Tavern;
            if (tavern == null)
            {
                return 0;
            }

            switch (key)
            {
                case StrategyGuideGrowthKeys.BeastLobsterGrowth:
                    var counters = tavern.AdvancedMechanics?.Counters;
                    return counters != null && counters.TryGetValue(LobsterGrowthCounter, out var growth)
                        ? growth
                        : 0;
                case StrategyGuideGrowthKeys.TavernSpellsCastThisGame:
                    return tavern.TavernSpellsCastThisGame;
                case StrategyGuideGrowthKeys.DemonTavernBonusAttack:
                    return tavern.TavernSpellBonusAttack;
                case StrategyGuideGrowthKeys.DemonTavernBonusHealth:
                    return tavern.TavernSpellBonusHealth;
                default:
                    return 0;
            }
        }

        private bool HasCurrentShapingSpellSlot()
        {
            var state = MatchService.State;
            return state?.Phase == MatchPhase.Tavern &&
                RunState != StrategyGuideRunState.FreeExplore &&
                MatchService.GetCurrentGuideShapingSpells().Count > 0;
        }

        private void SynchronizeShapingSpellSlot()
        {
            var state = MatchService.State;
            var tavern = state?.Player?.Tavern;
            if (tavern == null)
            {
                return;
            }
            if (tavern.GuideShapingSpellCardIds == null)
            {
                tavern.GuideShapingSpellCardIds = new List<string>();
            }

            var expectedCardIds = ScheduledShapingSpellCardIds(state.Round);
            if (expectedCardIds.Count == 0)
            {
                tavern.GuideShapingSpellCardId = null;
                tavern.GuideShapingSpellCardIds.Clear();
                tavern.GuideShapingSpellRound = 0;
                tavern.GuideShapingSpellConsumed = false;
                return;
            }
            if (state.Phase != MatchPhase.Tavern || RunState == StrategyGuideRunState.FreeExplore)
            {
                tavern.GuideShapingSpellCardId = null;
                tavern.GuideShapingSpellCardIds.Clear();
                tavern.GuideShapingSpellRound = state.Round;
                tavern.GuideShapingSpellConsumed = true;
                return;
            }

            if (tavern.GuideShapingSpellRound != state.Round)
            {
                tavern.GuideShapingSpellCardIds = new List<string>(expectedCardIds);
                tavern.GuideShapingSpellRound = state.Round;
                tavern.GuideShapingSpellConsumed = tavern.GuideShapingSpellCardIds.Count == 0;
            }
            else if ((tavern.GuideShapingSpellCardIds == null || tavern.GuideShapingSpellCardIds.Count == 0) &&
                     !tavern.GuideShapingSpellConsumed &&
                     StrategyGuideShapingSpells.Contains(tavern.GuideShapingSpellCardId))
            {
                tavern.GuideShapingSpellCardIds = new List<string> { tavern.GuideShapingSpellCardId };
            }

            if (tavern.GuideShapingSpellConsumed || tavern.GuideShapingSpellCardIds.Count == 0)
            {
                tavern.GuideShapingSpellCardId = null;
                return;
            }

            tavern.GuideShapingSpellCardId = tavern.GuideShapingSpellCardIds[0];
        }

        private List<string> ScheduledShapingSpellCardIds(int round)
        {
            var schedule = Profile.ShapingSpellCardIds;
            if (schedule == null || schedule.Count == 0 || round < Profile.StartRound)
            {
                return new List<string>();
            }

            var validSchedule = schedule
                .Where(StrategyGuideShapingSpells.Contains)
                .ToList();
            if (validSchedule.Count == 0)
            {
                return validSchedule;
            }

            var offset = Math.Max(0, round - Profile.StartRound);
            if (offset == 0)
            {
                return validSchedule.Distinct(StringComparer.Ordinal).ToList();
            }

            return new List<string> { validSchedule[offset % validSchedule.Count] };
        }

        private bool CanTriggerAcquisition(CommandContext context)
        {
            if (Profile.AcquisitionPlan == null || context == null)
            {
                return false;
            }

            return context.Type == GameCommandType.RerollShop ||
                ((context.Type == GameCommandType.NextTurn ||
                  context.Type == GameCommandType.ContinueNextTurnTransition) &&
                 (Profile.AcquisitionPlan?.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                    .Any(schedule => schedule != null &&
                        string.Equals(schedule.Source, StrategyGuideOfferSources.GreaterTrinketChoice, StringComparison.Ordinal))) ||
                (context.Type == GameCommandType.PlayMinion &&
                 (string.Equals(context.SourceDefinitionId, TripleRewardDefinitionId, StringComparison.Ordinal) ||
                  context.SourceCardKind == CardKind.TavernSpell ||
                  context.SourceCardKind == CardKind.Spell));
        }

        private void ApplyAcquisitionPlan(CommandContext context)
        {
            if (!CanTriggerAcquisition(context))
            {
                return;
            }

            string source;
            string triggerCardId;
            if (context.Type == GameCommandType.RerollShop)
            {
                source = StrategyGuideOfferSources.ShopRefresh;
                triggerCardId = null;
            }
            else if (context.Type == GameCommandType.NextTurn ||
                     context.Type == GameCommandType.ContinueNextTurnTransition)
            {
                if (!HasActiveScheduledGreaterTrinketChoice())
                {
                    return;
                }
                source = StrategyGuideOfferSources.GreaterTrinketChoice;
                triggerCardId = null;
            }
            else if (string.Equals(context.SourceDefinitionId, TripleRewardDefinitionId, StringComparison.Ordinal))
            {
                source = StrategyGuideOfferSources.TripleRewardDiscover;
                triggerCardId = null;
            }
            else
            {
                source = StrategyGuideOfferSources.TavernSpellDiscover;
                triggerCardId = context.SourceCardId;
            }

            var advanced = MatchService.State.Player.Tavern.AdvancedMechanics ??
                (MatchService.State.Player.Tavern.AdvancedMechanics = new AdvancedMechanicState());
            advanced.Counters = advanced.Counters ?? new Dictionary<string, int>();
            advanced.Selections = advanced.Selections ?? new Dictionary<string, string>();
            var tierScope = HasTierSpecificRoute(Profile.AcquisitionPlan, source, triggerCardId)
                ? MatchService.State.Player.Tavern.Tier
                : 0;
            var occurrenceKey = OfferOccurrencePrefix + Profile.ProfileId + ":" + source + ":" + (triggerCardId ?? "*") + ":" + tierScope;
            advanced.Counters.TryGetValue(occurrenceKey, out var completedOccurrences);
            var occurrence = completedOccurrences + 1;
            var schedule = StrategyGuideOfferScheduleResolver.FindSchedule(
                Profile.AcquisitionPlan,
                source,
                triggerCardId,
                occurrence,
                MatchService.State.Player.Tavern.Tier);
            if (schedule == null)
            {
                advanced.Counters[occurrenceKey] = occurrence;
                advanced.Selections.Remove(ActiveOfferScheduleKey);
                return;
            }

            if (string.Equals(source, StrategyGuideOfferSources.ShopRefresh, StringComparison.Ordinal))
            {
                if (!StrategyGuideOfferScheduleResolver.ApplyToShop(schedule, MatchService))
                {
                    throw new InvalidOperationException("Strategy guide shop offer schedule could not be applied: " + schedule.ScheduleId + ".");
                }

                advanced.Counters[occurrenceKey] = occurrence;
                advanced.Selections[ActiveOfferScheduleKey] = schedule.ScheduleId;
                return;
            }

            var seed = MatchService.State.Seed ^ Profile.Seed ^ occurrence * 7919 ^ StableHash(schedule.ScheduleId);
            if (string.Equals(source, StrategyGuideOfferSources.GreaterTrinketChoice, StringComparison.Ordinal))
            {
                if (!StrategyGuideOfferScheduleResolver.ApplyToActiveTrinketChoice(schedule, MatchService, seed))
                {
                    throw new InvalidOperationException(
                        "Strategy guide Trinket offer schedule could not be applied: " + schedule.ScheduleId + ".");
                }

                advanced.Counters[occurrenceKey] = occurrence;
                advanced.Selections[ActiveOfferScheduleKey] = schedule.ScheduleId;
                return;
            }

            if (!StrategyGuideOfferScheduleResolver.ApplyToActiveDiscover(
                    schedule,
                    MatchService.State,
                    MatchService.Catalogs,
                    seed))
            {
                throw new InvalidOperationException("Strategy guide offer schedule could not be applied: " + schedule.ScheduleId + ".");
            }

            advanced.Counters[occurrenceKey] = occurrence;
            advanced.Selections[ActiveOfferScheduleKey] = schedule.ScheduleId;
        }

        private bool HasActiveScheduledGreaterTrinketChoice()
        {
            var active = MatchService.State?.ChoiceQueue?.ActiveChoice;
            if (active?.Kind != ChoiceRequestKind.Trinket ||
                !string.Equals(active.Source, "turn-schedule", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var slot = active.ResolutionMetadata?
                .FirstOrDefault(entry => string.Equals(entry?.Key, "slot", StringComparison.Ordinal))?
                .Value;
            if (string.IsNullOrWhiteSpace(slot))
            {
                slot = active.Options?.FirstOrDefault()?.Slot;
            }
            return string.Equals(slot, TrinketSlotKind.Greater.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasTierSpecificRoute(
            StrategyGuideAcquisitionPlanDefinition plan,
            string source,
            string triggerCardId)
        {
            return (plan?.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                .Any(item => item != null &&
                    item.TriggerTavernTier > 0 &&
                    string.Equals(item.Source, source, StringComparison.Ordinal) &&
                    (string.IsNullOrWhiteSpace(item.TriggerCardId) ||
                     string.Equals(item.TriggerCardId, triggerCardId, StringComparison.OrdinalIgnoreCase)));
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var character in value ?? string.Empty)
                {
                    hash = (hash ^ character) * 16777619;
                }
                return hash;
            }
        }

        private void RecordCommand(CommandContext context)
        {
            foreach (var action in Profile.RequiredActions ?? new List<StrategyGuideRequiredAction>())
            {
                if (action == null || action.Kind == StrategyGuideActionKinds.BoardOrder || !Matches(action, context))
                {
                    continue;
                }

                var current = actionCounts.TryGetValue(action.ActionId, out var count) ? count : 0;
                actionCounts[action.ActionId] = Math.Min(Math.Max(1, action.Count), current + 1);
            }
        }

        private void SynchronizeStateActions()
        {
            foreach (var action in (Profile.RequiredActions ?? new List<StrategyGuideRequiredAction>())
                         .Where(item => item != null && item.Kind == StrategyGuideActionKinds.BoardOrder))
            {
                var satisfied = string.Equals(action.ChoiceId, "LeftMost", StringComparison.OrdinalIgnoreCase) &&
                    PlacementMatches(MatchService.State.Player.Board.FirstOrDefault(), action.TargetPlacementId);
                actionCounts[action.ActionId] = satisfied ? Math.Max(1, action.Count) : 0;
            }
        }

        private static bool Matches(StrategyGuideRequiredAction action, CommandContext context)
        {
            if (!MatchesCommandKind(action.Kind, context))
            {
                return false;
            }

            var sources = (action.SourcePlacementIds ?? new List<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            if (!string.IsNullOrWhiteSpace(action.SourcePlacementId))
            {
                sources.Add(action.SourcePlacementId);
            }
            if (sources.Count > 0 && !sources.Contains(context.SourcePlacementId, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!string.IsNullOrWhiteSpace(action.TargetPlacementId) &&
                !string.Equals(action.TargetPlacementId, context.TargetPlacementId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return string.IsNullOrWhiteSpace(action.ChoiceId) ||
                string.Equals(action.ChoiceId, context.ChoiceId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesCommandKind(string kind, CommandContext context)
        {
            switch (kind)
            {
                case StrategyGuideActionKinds.Buy:
                    return context.Type == GameCommandType.BuyMinion;
                case StrategyGuideActionKinds.Sell:
                    return context.Type == GameCommandType.SellMinion;
                case StrategyGuideActionKinds.Play:
                    return context.Type == GameCommandType.PlayMinion && context.SourceCardKind == CardKind.Minion;
                case StrategyGuideActionKinds.Cast:
                    return (context.Type == GameCommandType.PlayMinion ||
                            context.Type == GameCommandType.UseGuideShapingSpell) &&
                        context.SourceCardKind == CardKind.TavernSpell;
                case StrategyGuideActionKinds.Activate:
                    return context.Type == GameCommandType.UseRecruitAction;
                case StrategyGuideActionKinds.ChooseTrinket:
                    return context.Type == GameCommandType.ChooseMechanicOption &&
                        context.SourceCardKind == CardKind.Trinket;
                case StrategyGuideActionKinds.PlayFinalCards:
                    return context.Type == GameCommandType.PlayMinion && context.SourceCardKind == CardKind.Minion;
                default:
                    return false;
            }
        }

        private static CommandContext CaptureCommandContext(GameCommand command, MatchState state)
        {
            MinionInstance source = null;
            MinionInstance target = null;
            var choiceId = command.ChoiceId;
            CardKind? sourceCardKindOverride = null;
            string sourceCardIdOverride = null;
            switch (command.Type)
            {
                case GameCommandType.BuyMinion:
                    source = At(state.Player.Tavern.Shop, command.Index);
                    break;
                case GameCommandType.PlayMinion:
                    source = At(state.Player.Tavern.Hand, command.Index);
                    target = FindTarget(state, command.TargetInstanceId, command.TargetZone, command.TargetIndex);
                    break;
                case GameCommandType.UseGuideShapingSpell:
                    target = FindTarget(state, command.TargetInstanceId, command.TargetZone, command.TargetIndex);
                    sourceCardKindOverride = CardKind.TavernSpell;
                    sourceCardIdOverride = string.IsNullOrWhiteSpace(command.CardId)
                        ? state.Player.Tavern.GuideShapingSpellCardId
                        : command.CardId;
                    break;
                case GameCommandType.SellMinion:
                case GameCommandType.MoveBoardMinion:
                case GameCommandType.MoveMinion:
                    source = FindByInstanceId(state, command.InstanceId);
                    break;
                case GameCommandType.UseRecruitAction:
                    source = FindByInstanceId(state, command.RecruitActionRequest?.SourceInstanceId);
                    target = FindTarget(
                        state,
                        command.RecruitActionRequest?.TargetInstanceId,
                        command.RecruitActionRequest?.TargetZone ?? TargetZone.Unspecified,
                        command.RecruitActionRequest?.TargetIndex ?? -1);
                    choiceId = command.RecruitActionRequest?.ChoiceId;
                    break;
                case GameCommandType.ChooseMechanicOption:
                    var option = state.ChoiceQueue?.ActiveChoice?.Options != null &&
                        command.Index >= 0 && command.Index < state.ChoiceQueue.ActiveChoice.Options.Count
                        ? state.ChoiceQueue.ActiveChoice.Options[command.Index]
                        : null;
                    if (option?.Kind == AdvancedMechanicKind.Trinket)
                    {
                        sourceCardKindOverride = CardKind.Trinket;
                        sourceCardIdOverride = option.SourceId;
                        choiceId = option.SourceId;
                    }
                    break;
            }

            return new CommandContext
            {
                Type = command.Type,
                SourcePlacementId = PlacementId(source),
                TargetPlacementId = PlacementId(target),
                SourceCardKind = sourceCardKindOverride ?? source?.CardKind ?? CardKind.Minion,
                SourceCardId = sourceCardIdOverride ?? source?.CardId,
                SourceDefinitionId = source?.DefinitionId,
                ChoiceId = choiceId
            };
        }

        private bool LocksUndo(GameCommandType type)
        {
            if (!Profile.Undo.LockAfterTurnEnd && !Profile.Undo.LockAfterCombat)
            {
                return false;
            }

            return type == GameCommandType.NextTurn ||
                type == GameCommandType.BeginNextTurnTransition ||
                type == GameCommandType.ContinueNextTurnTransition ||
                type == GameCommandType.DebugSkipToNextTurn ||
                type == GameCommandType.SimulateCombat ||
                type == GameCommandType.RunCombatTest;
        }

        private static MinionInstance FindTarget(MatchState state, string instanceId, TargetZone zone, int index)
        {
            var byId = FindByInstanceId(state, instanceId);
            if (byId != null)
            {
                return byId;
            }

            switch (zone)
            {
                case TargetZone.FriendlyBoard:
                    return At(state.Player.Board, index);
                case TargetZone.TavernShop:
                    return At(state.Player.Tavern.Shop, index);
                case TargetZone.OpponentBoard:
                    return At(state.Opponent.Board, index);
                case TargetZone.Hand:
                    return At(state.Player.Tavern.Hand, index);
                default:
                    return null;
            }
        }

        private static MinionInstance FindByInstanceId(MatchState state, string instanceId)
        {
            if (state == null || string.IsNullOrWhiteSpace(instanceId))
            {
                return null;
            }

            return state.Player.Board
                .Concat(state.Player.Tavern.Hand)
                .Concat(state.Player.Tavern.Shop)
                .Concat(state.Opponent.Board)
                .Concat(state.Opponent.Hand)
                .FirstOrDefault(card => card != null && string.Equals(card.InstanceId, instanceId, StringComparison.Ordinal));
        }

        private static MinionInstance At(IReadOnlyList<MinionInstance> cards, int index)
        {
            return cards != null && index >= 0 && index < cards.Count ? cards[index] : null;
        }

        private static string PlacementId(MinionInstance card)
        {
            var tag = card?.Tags?.FirstOrDefault(value => value != null && value.StartsWith(PlacementTagPrefix, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(tag))
            {
                return tag.Substring(PlacementTagPrefix.Length);
            }

            const string instancePrefix = "player-guide-";
            return card?.InstanceId != null && card.InstanceId.StartsWith(instancePrefix, StringComparison.Ordinal)
                ? card.InstanceId.Substring(instancePrefix.Length)
                : null;
        }

        private static bool PlacementMatches(MinionInstance card, string placementId)
        {
            return !string.IsNullOrWhiteSpace(placementId) &&
                string.Equals(PlacementId(card), placementId, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class UndoSnapshot
        {
            public TestScenarioDefinition Scenario;
            public Dictionary<string, int> ActionCounts;
            public bool CombatWon;
            public StrategyGuideRunState RunState;
        }

        private sealed class CommandContext
        {
            public GameCommandType Type;
            public string SourcePlacementId;
            public string TargetPlacementId;
            public CardKind SourceCardKind;
            public string SourceCardId;
            public string SourceDefinitionId;
            public string ChoiceId;
        }
    }
}
