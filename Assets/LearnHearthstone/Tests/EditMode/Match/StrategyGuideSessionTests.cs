using System;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideSessionTests
    {
        [Test]
        public void Start_UnknownGuideFailsWithoutCreatingAnotherMatchPath()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                StrategyGuideSession.Start(
                    StrategyGuideCatalogLoader.LoadFromResources(),
                    "UNKNOWN-GUIDE",
                    ResolveSeason14()));

            StringAssert.Contains("does not exist", exception.Message);
        }

        [Test]
        public void Start_UnknownProfileFailsBeforeCreatingMatchState()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                StrategyGuideSession.Start(
                    StrategyGuideCatalogLoader.LoadFromResources(),
                    "GUIDE-S14-BEAST-LOBSTER-RALLY",
                    ResolveSeason14(),
                    profileId: "missing-profile"));

            StringAssert.Contains("entry profile does not exist", exception.Message);
        }

        [Test]
        public void Catalog_FreezesAllowedCommandsInstructionsAndExplicitBatchSources()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();

            Assert.IsTrue(catalog.Guides.SelectMany(guide => guide.EntryProfiles)
                .All(profile => profile.AllowedCommands.Count > 0));
            Assert.IsTrue(catalog.Guides.SelectMany(guide => Showcase(guide).RequiredActions)
                .All(action => !string.IsNullOrWhiteSpace(action.Instruction)));
            Assert.AreEqual(
                3,
                Showcase(catalog.GetGuide("GUIDE-S14-BEAST-LOBSTER-RALLY")).RequiredActions
                    .Single(action => action.Kind == StrategyGuideActionKinds.PlayFinalCards)
                    .SourcePlacementIds.Count);
            Assert.AreEqual(
                4,
                Showcase(catalog.GetGuide("GUIDE-S14-DEMON-TAVERN-CONSUME")).RequiredActions
                    .Single(action => action.Kind == StrategyGuideActionKinds.PlayFinalCards)
                    .SourcePlacementIds.Count);
        }

        [Test]
        public void Validator_RejectsUnknownAllowedCommandAndActionKind()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            Showcase(guide).AllowedCommands.Add("DoAnything");
            Showcase(guide).RequiredActions[0].Kind = "MagicAction";

            var result = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());

            CollectionAssert.Contains(result.Errors, "guide.allowed-command.unknown:DoAnything");
            CollectionAssert.Contains(result.Errors, "guide.action.kind:buy-scarab");
        }

        [Test]
        public void Start_RestoresCompiledV3AndInitialStateObjectives()
        {
            var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY");

            Assert.AreEqual(GameVersionIds.Season14Preview, session.MatchService.State.GameVersionId);
            Assert.AreEqual(session.Profile.StartRound, session.MatchService.State.Round);
            Assert.AreEqual("showcase", session.Profile.ProfileId);
            Assert.AreEqual(5, session.MatchService.State.ActiveTribes.Count);
            Assert.AreEqual(StrategyGuideRunState.Playing, session.RunState);
            Assert.AreEqual(1, session.UndoUsesRemaining);
            Assert.IsTrue(session.ActionProgress.Single(item => item.ActionId == "keep-lobster-left").IsComplete);
            Assert.IsFalse(session.Evaluation.FinalCompositionComplete);
        }

        [Test]
        public void StartAndRestartPreserveGuideCoreSpellWeightingMetadata()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides.First(item => item.CoreSpellCardNumbers.Count > 0);
            var session = StrategyGuideSession.Start(catalog, guide.GuideId, ResolveSeason14());

            CollectionAssert.AreEquivalent(
                guide.CoreSpellCardNumbers,
                session.MatchService.State.Player.Tavern.GuideCoreSpellCardNumbers);

            session.MatchService.State.Player.Tavern.GuideCoreSpellCardNumbers.Clear();
            Assert.IsTrue(session.Restart().Succeeded);
            CollectionAssert.AreEquivalent(
                guide.CoreSpellCardNumbers,
                session.MatchService.State.Player.Tavern.GuideCoreSpellCardNumbers);
        }

        [Test]
        public void Apply_RejectsCommandsOutsideGuideAllowlistWithoutMutation()
        {
            var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY");
            var gold = session.MatchService.State.Player.Tavern.Gold;

            var exception = Assert.Throws<InvalidOperationException>(() =>
                session.Apply(new GameCommand(GameCommandType.DebugAddGold, 20)));

            StringAssert.Contains("not allowed", exception.Message);
            Assert.AreEqual(gold, session.MatchService.State.Player.Tavern.Gold);
            Assert.IsFalse(session.CanUndo);
        }

        [Test]
        public void Undo_RestoresStateRngAndActionProgressExactlyOnce()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.GetGuide("GUIDE-S14-BEAST-LOBSTER-RALLY");
            Showcase(guide).AllowedCommands.Add(GameCommandType.RerollShop.ToString());
            var session = StrategyGuideSession.Start(catalog, guide.GuideId, ResolveSeason14());

            session.Apply(new GameCommand(GameCommandType.RerollShop));
            var firstRoll = session.MatchService.State.Player.Tavern.Shop.Select(card => card.CardId).ToList();
            Assert.IsTrue(session.CanUndo);

            var undo = session.Undo();
            Assert.IsTrue(undo.Succeeded, undo.Message);
            Assert.AreEqual(0, session.UndoUsesRemaining);
            Assert.IsFalse(session.CanUndo);

            session.Apply(new GameCommand(GameCommandType.RerollShop));
            CollectionAssert.AreEqual(firstRoll, session.MatchService.State.Player.Tavern.Shop.Select(card => card.CardId));
            Assert.AreEqual("guide.undo.used", session.Undo().Code);
        }

        [Test]
        public void Undo_RestoresTrackedActionWhenLastCommandIsReverted()
        {
            var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY");
            var shop = session.MatchService.State.Player.Tavern.Shop;
            var scarabIndex = shop.FindIndex(card => card.InstanceId == "player-guide-beast-scarab");

            session.Apply(new GameCommand(GameCommandType.BuyMinion, scarabIndex));
            Assert.IsTrue(session.ActionProgress.Single(item => item.ActionId == "buy-scarab").IsComplete);

            Assert.IsTrue(session.Undo().Succeeded);
            Assert.IsFalse(session.ActionProgress.Single(item => item.ActionId == "buy-scarab").IsComplete);
            Assert.IsTrue(session.MatchService.State.Player.Tavern.Shop.Any(card => card.InstanceId == "player-guide-beast-scarab"));
        }

        [Test]
        public void FailedRecruitAction_DoesNotAdvanceObjectiveOrCreateUndoPoint()
        {
            var session = Start("GUIDE-S14-MECH-SPELL-SATELLITE");

            session.Apply(new GameCommand(GameCommandType.UseRecruitAction, new RecruitActionRequest
            {
                ActionId = "unknown-action",
                SourceInstanceId = "missing-source",
                TargetZone = TargetZone.FriendlyBoard
            }));

            Assert.IsFalse(session.MatchService.LastRecruitActionResult.Succeeded);
            Assert.IsFalse(session.ActionProgress.Single(item => item.ActionId == "activate-drone").IsComplete);
            Assert.IsFalse(session.CanUndo);
        }

        [Test]
        public void TurnEndLocksUndoAndObservesCombatOutcome()
        {
            var session = Start("GUIDE-S14-BEAST-LOBSTER-RALLY");
            var lobster = session.MatchService.State.Player.Board[0];
            session.Apply(new GameCommand(GameCommandType.MoveBoardMinion, lobster.InstanceId, 0));
            Assert.IsTrue(session.CanUndo);

            session.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));

            Assert.AreEqual(MatchPhase.Result, session.MatchService.State.Phase);
            Assert.IsFalse(session.CanUndo);
            Assert.AreEqual(
                session.MatchService.State.LastResult.Winner == CombatWinner.Player,
                session.Evaluation.CombatWon);
        }

        [Test]
        public void ObjectiveEvaluator_RequiresExactOrderedGoldenCompositionAndMinimumStats()
        {
            var session = Start("GUIDE-S14-MECH-SPELL-SATELLITE");
            var state = session.MatchService.State;
            state.Player.Board.Clear();
            foreach (var target in session.Guide.FinalComposition)
            {
                var card = MinionFactory.Create(
                    session.MatchService.Catalogs.Minions.GetByCardId(target.CardId),
                    BoardSide.Player,
                    "objective-" + target.PlacementId,
                    target.Golden,
                    PoolSource.Copy,
                    0);
                card.Attack = Math.Max(card.Attack, target.MinimumAttack);
                card.Health = Math.Max(card.Health, target.MinimumHealth);
                card.MaxHealth = Math.Max(card.MaxHealth, card.Health);
                state.Player.Board.Add(card);
            }

            session.Synchronize();
            Assert.AreEqual(7, session.FinalSlotProgress.Count);
            Assert.IsTrue(session.FinalSlotProgress.All(item =>
                item.Status == StrategyGuideFinalSlotStatus.Complete));
            Assert.AreEqual(7, StrategyGuideObjectiveEvaluator.CountMatchedFinalSlots(session.Guide, state));
            Assert.IsTrue(StrategyGuideObjectiveEvaluator.IsFinalCompositionComplete(session.Guide, state));

            var first = state.Player.Board[0];
            state.Player.Board[0] = state.Player.Board[1];
            state.Player.Board[1] = first;
            Assert.IsFalse(StrategyGuideObjectiveEvaluator.IsFinalCompositionComplete(session.Guide, state));
        }

        [Test]
        public void ObjectiveEvaluator_ClassifiesEveryFinalSlotStatus()
        {
            var state = Start("GUIDE-S14-MECH-SPELL-SATELLITE").MatchService.State;
            var guide = new StrategyGuideDefinition();
            guide.FinalComposition.Add(Target("A", false, 5, 5));
            guide.FinalComposition.Add(Target("B", true, 5, 5));
            guide.FinalComposition.Add(Target("C", false, 5, 5));
            guide.FinalComposition.Add(Target("D", false, 5, 5));
            guide.FinalComposition.Add(Target("E", false, 5, 5));
            state.Player.Board.Clear();
            state.Player.Board.Add(Card("A", false, 5, 5));
            state.Player.Board.Add(Card("B", false, 5, 5));
            state.Player.Board.Add(Card("X", false, 5, 5));
            state.Player.Board.Add(Card("C", false, 5, 5));
            state.Player.Board.Add(Card("D", false, 4, 5));
            state.Player.Board.Add(Card("B", true, 5, 5));

            var progress = StrategyGuideObjectiveEvaluator.EvaluateFinalSlots(guide, state);

            Assert.AreEqual(StrategyGuideFinalSlotStatus.Complete, progress[0].Status);
            Assert.AreEqual(0, progress[0].MatchedBoardIndex);
            Assert.AreEqual(StrategyGuideFinalSlotStatus.StateMismatch, progress[1].Status);
            Assert.AreEqual(1, progress[1].MatchedBoardIndex);
            Assert.AreEqual(StrategyGuideFinalSlotStatus.PositionWrong, progress[2].Status);
            Assert.AreEqual(3, progress[2].MatchedBoardIndex);
            Assert.AreEqual(StrategyGuideFinalSlotStatus.StateMismatch, progress[3].Status);
            Assert.AreEqual(4, progress[3].MatchedBoardIndex);
            Assert.AreEqual(StrategyGuideFinalSlotStatus.Missing, progress[4].Status);
            Assert.AreEqual(-1, progress[4].MatchedBoardIndex);
            Assert.IsNull(progress[4].Actual);
            Assert.AreEqual(1, StrategyGuideObjectiveEvaluator.CountMatchedFinalSlots(guide, state));
        }

        [Test]
        public void ObjectiveEvaluator_DoesNotAssignOneCardToDuplicateTargets()
        {
            var state = Start("GUIDE-S14-MECH-SPELL-SATELLITE").MatchService.State;
            var guide = new StrategyGuideDefinition();
            guide.FinalComposition.Add(Target("A", false, 1, 1));
            guide.FinalComposition.Add(Target("A", false, 1, 1));
            state.Player.Board.Clear();
            state.Player.Board.Add(Card("X", false, 1, 1));
            state.Player.Board.Add(Card("A", false, 1, 1));

            var progress = StrategyGuideObjectiveEvaluator.EvaluateFinalSlots(guide, state);

            Assert.AreEqual(StrategyGuideFinalSlotStatus.Missing, progress[0].Status);
            Assert.AreEqual(StrategyGuideFinalSlotStatus.Complete, progress[1].Status);
            Assert.AreEqual(1, progress.Count(item => item.Actual != null));
        }

        [Test]
        public void CompletedRunCanEnterFreeExploreAndRestartResetsTheGenericSession()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.GetGuide("GUIDE-S14-MECH-SPELL-SATELLITE");
            Showcase(guide).RequiredActions.Clear();
            var session = StrategyGuideSession.Start(catalog, guide.GuideId, ResolveSeason14());
            FillFinalBoard(session);
            session.MatchService.State.LastResult = new CombatOutput { Winner = CombatWinner.Player };
            session.MatchService.State.Phase = MatchPhase.Result;

            session.Synchronize();
            Assert.AreEqual(StrategyGuideRunState.Completed, session.RunState);
            Assert.IsTrue(session.EnterFreeExplore().Succeeded);
            Assert.AreEqual(StrategyGuideRunState.FreeExplore, session.RunState);
            Assert.IsFalse(session.CanUndo);

            Assert.IsTrue(session.Restart().Succeeded);
            Assert.AreEqual(StrategyGuideRunState.Playing, session.RunState);
            Assert.AreEqual(session.Profile.StartRound, session.MatchService.State.Round);
            Assert.AreEqual(1, session.UndoUsesRemaining);
        }

        private static StrategyGuideSession Start(string guideId)
        {
            return StrategyGuideSession.Start(StrategyGuideCatalogLoader.LoadFromResources(), guideId, ResolveSeason14());
        }

        private static void FillFinalBoard(StrategyGuideSession session)
        {
            var state = session.MatchService.State;
            state.Player.Board.Clear();
            foreach (var target in session.Guide.FinalComposition)
            {
                var card = MinionFactory.Create(
                    session.MatchService.Catalogs.Minions.GetByCardId(target.CardId),
                    BoardSide.Player,
                    "complete-" + target.PlacementId,
                    target.Golden,
                    PoolSource.Copy,
                    0);
                card.Attack = Math.Max(card.Attack, target.MinimumAttack);
                card.Health = Math.Max(card.Health, target.MinimumHealth);
                card.MaxHealth = Math.Max(card.MaxHealth, card.Health);
                state.Player.Board.Add(card);
            }
        }

        private static StrategyGuideCardDefinition Target(string cardId, bool golden, int attack, int health)
        {
            return new StrategyGuideCardDefinition
            {
                CardId = cardId,
                Golden = golden,
                MinimumAttack = attack,
                MinimumHealth = health
            };
        }

        private static MinionInstance Card(string cardId, bool golden, int attack, int health)
        {
            return new MinionInstance
            {
                CardId = cardId,
                Golden = golden,
                Attack = attack,
                Health = health,
                MaxHealth = health
            };
        }

        private static ResolvedGameVersion ResolveSeason14()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            return snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
        }

        private static StrategyGuideEntryProfileDefinition Showcase(StrategyGuideDefinition guide)
        {
            return guide.EntryProfiles.Single(profile =>
                profile.Difficulty == StrategyGuideDifficulties.Showcase);
        }
    }
}
