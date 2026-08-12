using System;
using System.Collections.Generic;
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
    public sealed class StrategyGuideShapingSpellTests
    {
        private const string DeathrattleSpellId = StrategyGuideShapingSpells.Deathrattle;
        private const string BattlecrySpellId = StrategyGuideShapingSpells.Battlecry;
        private const string EndOfTurnSpellId = StrategyGuideShapingSpells.EndOfTurn;

        [Test]
        public void Catalog_ContainsOnlyNonPoolGuideTutorialShapingSpells()
        {
            var service = MatchService.CreateWithDefaultCatalog(38101);
            var definitions = new[] { DeathrattleSpellId, BattlecrySpellId, EndOfTurnSpellId }
                .Select(service.Catalogs.Spells.GetByCardNumber)
                .ToList();

            Assert.IsTrue(definitions.All(item => !item.InPool));
            Assert.IsTrue(definitions.All(item => item.Cost == 0));
            Assert.IsTrue(definitions.All(item => item.Tags.Contains("guide_tutorial")));
            Assert.AreEqual(2, definitions.Count(item => item.TargetTemplate == SpellTargetTemplate.FriendlyMinion));
            Assert.AreEqual(1, definitions.Count(item => item.TargetTemplate == SpellTargetTemplate.None));
        }

        [Test]
        public void CurrentGuideShapingSpell_QueryReturnsTutorialInstanceAndClearsWhenUnavailable()
        {
            var service = MatchService.CreateWithDefaultCatalog(381011);
            SetGuideSpellSlot(service, EndOfTurnSpellId);

            var spell = service.GetCurrentGuideShapingSpell();

            Assert.IsNotNull(spell);
            Assert.AreEqual(EndOfTurnSpellId, spell.CardId);
            CollectionAssert.Contains(spell.Tags, "strategy-guide:" + StrategyGuideProvenance.GuideTutorial);
            service.State.Player.Tavern.GuideShapingSpellConsumed = true;
            Assert.IsNull(service.GetCurrentGuideShapingSpell());
        }

        [Test]
        public void CurrentGuideShapingSpell_QueryValidatesBattlecrySecondaryTarget()
        {
            var service = MatchService.CreateWithDefaultCatalog(381012);
            service.State.Player.Board.Clear();
            var source = CreateMinion(service, "BG28_303", "guide-query-source");
            source.Keywords.Add(Keyword.Battlecry);
            service.State.Player.Board.Add(source);
            SetGuideSpellSlot(service, BattlecrySpellId);

            Assert.IsFalse(service.TryValidateGuideShapingSecondaryTarget(
                0,
                source.InstanceId,
                -1,
                TargetZone.Unspecified,
                null,
                out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void DeathrattleShapingSpell_TriggersTargetWithoutDestroyingIt()
        {
            var service = MatchService.CreateWithDefaultCatalog(38102);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var target = CreateMinion(service, "BG28_300", "guide-deathrattle-target");
            target.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Add(target);
            SetGuideSpellSlot(service, DeathrattleSpellId);

            service.Apply(new GameCommand(
                GameCommandType.UseGuideShapingSpell,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.IsTrue(service.State.Player.Board.Any(item => item.InstanceId == target.InstanceId));
            Assert.Greater(service.State.Player.Board.Count, 1);
            Assert.IsNull(service.State.Player.Tavern.GuideShapingSpellCardId);
            Assert.IsTrue(service.State.Player.Tavern.GuideShapingSpellConsumed);
        }

        [Test]
        public void DeathrattleShapingSpell_InvalidTargetIsRejectedAtomically()
        {
            var service = MatchService.CreateWithDefaultCatalog(38103);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var target = CreateMinion(service, "BG20_100", "guide-non-deathrattle-target");
            target.Keywords.Remove(Keyword.Deathrattle);
            service.State.Player.Board.Add(target);
            SetGuideSpellSlot(service, DeathrattleSpellId);

            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(
                GameCommandType.UseGuideShapingSpell,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId)));
            Assert.AreEqual(DeathrattleSpellId, service.State.Player.Tavern.GuideShapingSpellCardId);
            Assert.IsFalse(service.State.Player.Tavern.GuideShapingSpellConsumed);
        }

        [Test]
        public void BattlecryShapingSpell_ReplaysFriendlyBattlecry()
        {
            var service = MatchService.CreateWithDefaultCatalog(38104);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var target = CreateMinion(service, "BG20_100", "guide-battlecry-target");
            target.Keywords.Add(Keyword.Battlecry);
            service.State.Player.Board.Add(target);
            SetGuideSpellSlot(service, BattlecrySpellId);

            service.Apply(new GameCommand(
                GameCommandType.UseGuideShapingSpell,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(1, service.State.Player.Tavern.BattlecriesTriggeredThisGame);
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
        }

        [Test]
        public void BattlecryShapingSpell_ForwardsSecondaryBattlecryTarget()
        {
            var service = MatchService.CreateWithDefaultCatalog(38105);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var source = CreateMinion(service, "BG28_303", "guide-targeted-battlecry-source");
            source.Keywords.Add(Keyword.Battlecry);
            var victim = CreateMinion(service, "BG28_300", "guide-targeted-battlecry-victim");
            victim.Tribes.Clear();
            victim.Tribes.Add(Tribe.Undead);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(victim);
            SetGuideSpellSlot(service, BattlecrySpellId);

            service.Apply(new GameCommand(
                GameCommandType.UseGuideShapingSpell,
                0,
                TargetZone.FriendlyBoard,
                1,
                TargetZone.FriendlyBoard,
                source.InstanceId,
                victim.InstanceId));

            Assert.IsFalse(service.State.Player.Board.Any(item => item.InstanceId == victim.InstanceId));
            Assert.Greater(service.State.Player.Tavern.Hand.Count, 0);
        }

        [Test]
        public void BattlecryShapingSpell_MissingRequiredSecondaryTargetIsRejectedAtomically()
        {
            var service = MatchService.CreateWithDefaultCatalog(381051);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var source = CreateMinion(service, "BG28_303", "guide-missing-secondary-source");
            source.Keywords.Add(Keyword.Battlecry);
            service.State.Player.Board.Add(source);
            SetGuideSpellSlot(service, BattlecrySpellId);

            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(
                GameCommandType.UseGuideShapingSpell,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                source.InstanceId)));
            Assert.AreEqual(BattlecrySpellId, service.State.Player.Tavern.GuideShapingSpellCardId);
            Assert.IsFalse(service.State.Player.Tavern.GuideShapingSpellConsumed);
            Assert.AreEqual(0, service.State.Player.Tavern.BattlecriesTriggeredThisGame);
        }

        [Test]
        public void EndOfTurnShapingSpell_TriggersEffectsWithoutAdvancingRealTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(38106);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var source = CreateMinion(service, "BG20_100", "guide-end-turn-source");
            source.EffectIds.Clear();
            source.EffectIds.Add("turn_ended_self_buff_1_1");
            service.State.Player.Board.Add(source);
            SetGuideSpellSlot(service, EndOfTurnSpellId);
            service.State.DelayedObjectStates.Add(new DelayedObjectState
            {
                InstanceId = "guide-lockbox-probe",
                DefinitionRevisionId = "guide-lockbox-probe@1",
                CreatedRound = service.State.Round,
                RemainingTurns = 5,
                OpenResolverId = "guide-lockbox-probe@open"
            });
            var round = service.State.Round;
            var phase = service.State.Phase;
            var transitionSequence = service.State.TurnEndTransitionSequence;
            var attack = source.Attack;
            var health = source.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.UseGuideShapingSpell));

            Assert.AreEqual(attack + 1, source.Attack);
            Assert.AreEqual(health + 1, source.MaxHealth);
            Assert.AreEqual(round, service.State.Round);
            Assert.AreEqual(phase, service.State.Phase);
            Assert.AreEqual(0, service.State.PendingTurnStartRound);
            Assert.IsTrue(string.IsNullOrEmpty(service.State.PendingTurnEndTransitionId));
            Assert.AreEqual(transitionSequence, service.State.TurnEndTransitionSequence);
            Assert.IsNull(service.State.LastResult);
            Assert.AreEqual(5, service.State.DelayedObjectStates.Single().RemainingTurns);
            Assert.IsNull(service.State.Player.Tavern.GuideShapingSpellCardId);
            Assert.IsTrue(service.State.Player.Tavern.GuideShapingSpellConsumed);
        }

        [TestCase(DeathrattleSpellId)]
        [TestCase(BattlecrySpellId)]
        [TestCase(EndOfTurnSpellId)]
        public void ShapingSpell_OrdinaryHandPathIsRejectedAtomically(string cardId)
        {
            var service = MatchService.CreateWithDefaultCatalog(381061);
            service.State.Player.Tavern.Hand.Clear();
            AddGuideSpellToHand(service, cardId);

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, -1)));
            Assert.AreEqual(cardId, service.State.Player.Tavern.Hand.Single().CardId);
        }

        [Test]
        public void DedicatedShapingSpell_ResolvesOnceWithoutOrdinarySpellOrCardSideEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(381062);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            var target = CreateMinion(service, "BG28_300", "guide-single-cast-target");
            var extraCastSource = CreateMinion(service, "BG35_883", "guide-extra-cast-source");
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(extraCastSource);
            tavern.TavernSpellsCastThisTurn = 3;
            tavern.TavernSpellsCastThisGame = 7;
            tavern.CardsPlayedThisTurn = 5;
            tavern.LastTavernSpellCardId = "sentinel-spell";
            SetGuideSpellSlot(service, DeathrattleSpellId);

            service.Apply(new GameCommand(
                GameCommandType.UseGuideShapingSpell,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(4, service.State.Player.Board.Count, "Belinda must not add a second shaping cast.");
            Assert.AreEqual(3, tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(7, tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(5, tavern.CardsPlayedThisTurn);
            Assert.AreEqual("sentinel-spell", tavern.LastTavernSpellCardId);
            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseGuideShapingSpell)));
        }

        [Test]
        public void DedicatedShapingSpell_RemainsUsableWithTenOrdinaryHandCards()
        {
            var service = MatchService.CreateWithDefaultCatalog(381063);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            for (var index = 0; index < 10; index += 1)
            {
                tavern.Hand.Add(CreateMinion(service, "BG20_100", "guide-full-hand-" + index));
            }
            SetGuideSpellSlot(service, EndOfTurnSpellId);

            service.Apply(new GameCommand(GameCommandType.UseGuideShapingSpell));

            Assert.AreEqual(10, tavern.Hand.Count);
            Assert.IsTrue(tavern.GuideShapingSpellConsumed);
        }

        [Test]
        public void DedicatedShapingSpell_RejectsStaleRoundWithoutConsumingSlot()
        {
            var service = MatchService.CreateWithDefaultCatalog(381064);
            SetGuideSpellSlot(service, EndOfTurnSpellId);
            service.State.Player.Tavern.GuideShapingSpellRound -= 1;

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseGuideShapingSpell)));

            Assert.AreEqual(EndOfTurnSpellId, service.State.Player.Tavern.GuideShapingSpellCardId);
            Assert.IsFalse(service.State.Player.Tavern.GuideShapingSpellConsumed);
        }

        [Test]
        public void Mapper_RoundTripsDedicatedShapingSpellSlotsWithoutSharingList()
        {
            var source = MatchService.CreateWithDefaultCatalog(381065);
            source.State.Player.Tavern.GuideShapingSpellCardId = BattlecrySpellId;
            source.State.Player.Tavern.GuideShapingSpellCardIds = new List<string>
            {
                BattlecrySpellId,
                DeathrattleSpellId
            };
            source.State.Player.Tavern.GuideShapingSpellRound = 6;
            source.State.Player.Tavern.GuideShapingSpellConsumed = false;
            var scenario = TestScenarioMapper.Capture(source.State, "guide-shaping-slot");
            var target = MatchService.CreateWithDefaultCatalog(381066);

            TestScenarioMapper.ApplyTo(target.State, scenario);

            Assert.AreEqual(BattlecrySpellId, scenario.Tavern.GuideShapingSpellCardId);
            CollectionAssert.AreEqual(
                new[] { BattlecrySpellId, DeathrattleSpellId },
                scenario.Tavern.GuideShapingSpellCardIds);
            Assert.AreEqual(6, scenario.Tavern.GuideShapingSpellRound);
            Assert.IsFalse(scenario.Tavern.GuideShapingSpellConsumed);
            Assert.AreEqual(BattlecrySpellId, target.State.Player.Tavern.GuideShapingSpellCardId);
            CollectionAssert.AreEqual(
                new[] { BattlecrySpellId, DeathrattleSpellId },
                target.State.Player.Tavern.GuideShapingSpellCardIds);
            Assert.AreEqual(6, target.State.Player.Tavern.GuideShapingSpellRound);
            Assert.IsFalse(target.State.Player.Tavern.GuideShapingSpellConsumed);
            scenario.Tavern.GuideShapingSpellCardIds.Clear();
            Assert.AreEqual(2, target.State.Player.Tavern.GuideShapingSpellCardIds.Count);
        }

        [Test]
        public void Validator_RequiresCompleteSetAndRejectsDuplicatesUnknownOrOrdinaryPlacement()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = guide.EntryProfiles[0];
            profile.ShapingSpellCardIds = new List<string>
            {
                BattlecrySpellId,
                DeathrattleSpellId,
                EndOfTurnSpellId
            };

            var complete = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());
            Assert.IsFalse(
                complete.Errors.Any(error => error.StartsWith("guide.shaping-spell.", StringComparison.Ordinal)),
                string.Join(" | ", complete.Errors));

            profile.ShapingSpellCardIds[2] = DeathrattleSpellId;
            var repeated = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());
            CollectionAssert.Contains(
                repeated.Errors,
                "guide.shaping-spell.duplicate-or-empty:" + DeathrattleSpellId);
            CollectionAssert.Contains(repeated.Errors, "guide.shaping-spell.complete-set");

            profile.ShapingSpellCardIds[2] = "UNKNOWN_GUIDE_SHAPING";
            var unknown = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());
            CollectionAssert.Contains(unknown.Errors, "guide.shaping-spell.unknown:UNKNOWN_GUIDE_SHAPING");

            var placement = profile.Placements[0];
            placement.CardKind = StrategyGuideCardKinds.TavernSpell;
            placement.CardId = EndOfTurnSpellId;
            placement.Provenance = StrategyGuideProvenance.GuideTutorial;
            var ordinaryPlacement = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());
            CollectionAssert.Contains(ordinaryPlacement.Errors, "guide.card.shaping-slot-only:" + EndOfTurnSpellId);
        }

        [Test]
        public void Session_DealsThreeFirstRoundThenCyclesOneAndExpiresAtCombat()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = guide.EntryProfiles.Single(item =>
                item.Difficulty == StrategyGuideDifficulties.Showcase);
            profile.ShapingSpellCardIds = new List<string>
            {
                BattlecrySpellId,
                DeathrattleSpellId,
                EndOfTurnSpellId
            };
            profile.AllowedCommands.Remove(GameCommandType.UseGuideShapingSpell.ToString());
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);
            var state = session.MatchService.State;

            CollectionAssert.AreEqual(
                new[] { BattlecrySpellId, DeathrattleSpellId, EndOfTurnSpellId },
                state.Player.Tavern.GuideShapingSpellCardIds);
            Assert.AreEqual(3, session.MatchService.GetCurrentGuideShapingSpells().Count);
            Assert.AreEqual(BattlecrySpellId, state.Player.Tavern.GuideShapingSpellCardId);
            Assert.AreEqual(profile.StartRound, state.Player.Tavern.GuideShapingSpellRound);
            Assert.IsTrue(session.CanApply(GameCommandType.UseGuideShapingSpell));

            session.Apply(new GameCommand(
                GameCommandType.UseGuideShapingSpell,
                -1,
                TargetZone.Unspecified,
                cardId: EndOfTurnSpellId));
            CollectionAssert.AreEqual(
                new[] { BattlecrySpellId, DeathrattleSpellId },
                state.Player.Tavern.GuideShapingSpellCardIds);
            Assert.IsFalse(state.Player.Tavern.GuideShapingSpellConsumed);

            state.Round = profile.StartRound + 1;
            session.Synchronize();
            Assert.AreEqual(DeathrattleSpellId, state.Player.Tavern.GuideShapingSpellCardId);
            CollectionAssert.AreEqual(
                new[] { DeathrattleSpellId },
                state.Player.Tavern.GuideShapingSpellCardIds);

            state.Round = profile.StartRound + 8;
            session.Synchronize();
            Assert.AreEqual(EndOfTurnSpellId, state.Player.Tavern.GuideShapingSpellCardId);
            Assert.AreEqual(state.Round, state.Player.Tavern.GuideShapingSpellRound);

            state.Phase = MatchPhase.Combat;
            session.Synchronize();
            Assert.IsNull(state.Player.Tavern.GuideShapingSpellCardId);
            Assert.IsEmpty(state.Player.Tavern.GuideShapingSpellCardIds);
            state.Phase = MatchPhase.Tavern;
            session.Synchronize();
            Assert.IsNull(state.Player.Tavern.GuideShapingSpellCardId);
            Assert.IsFalse(session.CanApply(GameCommandType.UseGuideShapingSpell));

            state.Round += 1;
            session.Synchronize();
            Assert.AreEqual(BattlecrySpellId, state.Player.Tavern.GuideShapingSpellCardId);
            CollectionAssert.AreEqual(
                new[] { BattlecrySpellId },
                state.Player.Tavern.GuideShapingSpellCardIds);

            state.Player.Tavern.GuideShapingSpellCardId = null;
            state.Player.Tavern.GuideShapingSpellCardIds.Clear();
            state.Player.Tavern.GuideShapingSpellConsumed = true;
            session.Synchronize();
            Assert.IsNull(state.Player.Tavern.GuideShapingSpellCardId);
            Assert.IsFalse(session.CanApply(GameCommandType.UseGuideShapingSpell));
        }

        [Test]
        public void Session_UndoAndRestartRestoreDedicatedShapingSlot()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = guide.EntryProfiles.Single(item =>
                item.Difficulty == StrategyGuideDifficulties.Showcase);
            profile.ShapingSpellCardIds = new List<string>
            {
                EndOfTurnSpellId,
                BattlecrySpellId,
                DeathrattleSpellId
            };
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);

            session.Apply(new GameCommand(GameCommandType.UseGuideShapingSpell));
            CollectionAssert.AreEqual(
                new[] { BattlecrySpellId, DeathrattleSpellId },
                session.MatchService.State.Player.Tavern.GuideShapingSpellCardIds);
            Assert.IsFalse(session.MatchService.State.Player.Tavern.GuideShapingSpellConsumed);
            Assert.IsTrue(session.Undo().Succeeded);
            Assert.AreEqual(EndOfTurnSpellId, session.MatchService.State.Player.Tavern.GuideShapingSpellCardId);
            CollectionAssert.AreEqual(
                new[] { EndOfTurnSpellId, BattlecrySpellId, DeathrattleSpellId },
                session.MatchService.State.Player.Tavern.GuideShapingSpellCardIds);
            Assert.IsFalse(session.MatchService.State.Player.Tavern.GuideShapingSpellConsumed);

            session.Apply(new GameCommand(GameCommandType.UseGuideShapingSpell));
            Assert.IsTrue(session.Restart().Succeeded);
            Assert.AreEqual(EndOfTurnSpellId, session.MatchService.State.Player.Tavern.GuideShapingSpellCardId);
            CollectionAssert.AreEqual(
                new[] { EndOfTurnSpellId, BattlecrySpellId, DeathrattleSpellId },
                session.MatchService.State.Player.Tavern.GuideShapingSpellCardIds);
            Assert.AreEqual(profile.StartRound, session.MatchService.State.Player.Tavern.GuideShapingSpellRound);
            Assert.IsFalse(session.MatchService.State.Player.Tavern.GuideShapingSpellConsumed);
        }

        [Test]
        public void Session_GrowthProgressReadsAllFourThresholdsAndGatesCompletion()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = guide.EntryProfiles.Single(item =>
                item.Difficulty == StrategyGuideDifficulties.Showcase);
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);
            profile.RequiredActions.Clear();
            profile.Victory.RequireFinalComposition = false;
            profile.Victory.RequireCombatWin = false;
            profile.GrowthQuality = new List<StrategyGuideGrowthValue>
            {
                new StrategyGuideGrowthValue { Key = StrategyGuideGrowthKeys.BeastLobsterGrowth, Value = 2 },
                new StrategyGuideGrowthValue { Key = StrategyGuideGrowthKeys.TavernSpellsCastThisGame, Value = 3 },
                new StrategyGuideGrowthValue { Key = StrategyGuideGrowthKeys.DemonTavernBonusAttack, Value = 4 },
                new StrategyGuideGrowthValue { Key = StrategyGuideGrowthKeys.DemonTavernBonusHealth, Value = 5 }
            };

            session.Synchronize();

            Assert.AreEqual(4, session.GrowthProgress.Count);
            Assert.AreEqual(0, session.Evaluation.CompletedGrowthCount);
            Assert.AreEqual(4, session.Evaluation.RequiredGrowthCount);
            Assert.IsFalse(session.Evaluation.GrowthQualityComplete);
            Assert.IsFalse(session.Evaluation.IsComplete);

            var tavern = session.MatchService.State.Player.Tavern;
            tavern.AdvancedMechanics.Counters["season14_min_r30_lobster_growth"] = 2;
            tavern.TavernSpellsCastThisGame = 3;
            tavern.TavernSpellBonusAttack = 4;
            tavern.TavernSpellBonusHealth = 5;
            session.Synchronize();

            CollectionAssert.AreEqual(
                new[] { 2, 3, 4, 5 },
                session.GrowthProgress.Select(item => item.CurrentValue).ToArray());
            Assert.IsTrue(session.GrowthProgress.All(item => item.IsComplete));
            Assert.AreEqual(4, session.Evaluation.CompletedGrowthCount);
            Assert.IsTrue(session.Evaluation.GrowthQualityComplete);
            Assert.IsTrue(session.Evaluation.IsComplete);
        }

        [Test]
        public void Compiler_IgnoresPlayerGrowthPresetButKeepsOpponentGrowth()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.GetGuide("GUIDE-S14-BEAST-LOBSTER-RALLY");
            guide.EntryProfiles.Single(profile => profile.Difficulty == StrategyGuideDifficulties.Showcase)
                .GrowthQuality.Add(new StrategyGuideGrowthValue
                {
                    Key = StrategyGuideGrowthKeys.BeastLobsterGrowth,
                    Value = 99
                });
            var compiled = StrategyGuideScenarioCompiler.Compile(catalog, guide, ResolveSeason14());

            Assert.IsFalse(compiled.Scenario.PlayerAdvancedMechanics.State.Counters.ContainsKey(
                "season14_min_r30_lobster_growth"));
            Assert.IsNotEmpty(compiled.Opponent.GrowthQuality);
            foreach (var growth in compiled.Opponent.GrowthQuality)
            {
                switch (growth.Key)
                {
                    case StrategyGuideGrowthKeys.BeastLobsterGrowth:
                        Assert.AreEqual(
                            growth.Value,
                            compiled.Scenario.OpponentAdvancedMechanics.State.Counters[
                                "season14_min_r30_lobster_growth"]);
                        break;
                    case StrategyGuideGrowthKeys.TavernSpellsCastThisGame:
                        Assert.AreEqual(growth.Value, compiled.Scenario.OpponentCombatModifiers.SpellsCastThisGame);
                        break;
                    case StrategyGuideGrowthKeys.DemonTavernBonusAttack:
                        Assert.AreEqual(growth.Value, compiled.Scenario.OpponentCombatModifiers.TavernSpellBonusAttack);
                        break;
                    case StrategyGuideGrowthKeys.DemonTavernBonusHealth:
                        Assert.AreEqual(growth.Value, compiled.Scenario.OpponentCombatModifiers.TavernSpellBonusHealth);
                        break;
                }
            }
        }

        private static void AddGuideSpellToHand(MatchService service, string cardNumber)
        {
            var spell = MinionFactory.Create(
                service.Catalogs.Spells.GetByCardNumber(cardNumber),
                BoardSide.Player,
                "shaping-" + service.State.Player.Tavern.Hand.Count);
            spell.Tags.Add("strategy-guide:" + StrategyGuideProvenance.GuideTutorial);
            service.State.Player.Tavern.Hand.Add(spell);
        }

        private static void SetGuideSpellSlot(MatchService service, string cardNumber)
        {
            var tavern = service.State.Player.Tavern;
            tavern.GuideShapingSpellCardId = cardNumber;
            tavern.GuideShapingSpellCardIds = new List<string> { cardNumber };
            tavern.GuideShapingSpellRound = service.State.Round;
            tavern.GuideShapingSpellConsumed = false;
        }

        private static MinionInstance CreateMinion(MatchService service, string cardId, string instanceId)
        {
            var instance = MinionFactory.Create(
                service.Catalogs.Minions.GetByCardId(cardId),
                BoardSide.Player,
                instanceId,
                false,
                PoolSource.Copy,
                0);
            instance.InstanceId = instanceId;
            return instance;
        }

        private static ResolvedGameVersion ResolveSeason14()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            return snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
        }
    }
}
