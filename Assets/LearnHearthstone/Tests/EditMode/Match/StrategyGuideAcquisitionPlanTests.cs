using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideAcquisitionPlanTests
    {
        [Test]
        public void Resolver_NaturalSeededPreservesExistingCandidates()
        {
            var schedule = Schedule(StrategyGuideOfferPolicies.NaturalSeeded);

            var resolved = StrategyGuideOfferScheduleResolver.ResolveCandidateIds(
                schedule,
                new[] { "A", "B", "C", "D" },
                new[] { "A", "B", "C", "D" },
                17);

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, resolved);
        }

        [Test]
        public void Resolver_MustIncludeIsDeterministicAndKeepsTargetAmongNaturalOptions()
        {
            var schedule = Schedule(StrategyGuideOfferPolicies.MustInclude, "TARGET");

            var first = StrategyGuideOfferScheduleResolver.ResolveCandidateIds(
                schedule,
                new[] { "A", "B", "C" },
                new[] { "A", "B", "C", "TARGET" },
                29);
            var second = StrategyGuideOfferScheduleResolver.ResolveCandidateIds(
                schedule,
                new[] { "A", "B", "C" },
                new[] { "A", "B", "C", "TARGET" },
                29);

            CollectionAssert.AreEqual(first, second);
            Assert.AreEqual(3, first.Count);
            CollectionAssert.Contains(first, "TARGET");
            Assert.AreEqual(3, first.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Test]
        public void Resolver_MustIncludeAnyChoosesExactlyOneRecommendedTargetDeterministically()
        {
            var schedule = Schedule(
                StrategyGuideOfferPolicies.MustIncludeAny,
                "RECOMMENDED-A",
                "RECOMMENDED-B",
                "RECOMMENDED-C");
            var eligible = new[]
            {
                "NATURAL-A",
                "NATURAL-B",
                "NATURAL-C",
                "RECOMMENDED-A",
                "RECOMMENDED-B",
                "RECOMMENDED-C"
            };

            var first = StrategyGuideOfferScheduleResolver.ResolveCandidateIds(
                schedule,
                new[] { "NATURAL-A", "NATURAL-B", "NATURAL-C" },
                eligible,
                37);
            var second = StrategyGuideOfferScheduleResolver.ResolveCandidateIds(
                schedule,
                new[] { "NATURAL-A", "NATURAL-B", "NATURAL-C" },
                eligible,
                37);

            CollectionAssert.AreEqual(first, second);
            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(1, first.Count(cardId => cardId.StartsWith("RECOMMENDED-", StringComparison.Ordinal)));
        }

        [Test]
        public void Resolver_PinnedUsesExactDeclaredOrderAndRejectsIneligibleTarget()
        {
            var schedule = Schedule(StrategyGuideOfferPolicies.Pinned, "C", "A", "B");

            CollectionAssert.AreEqual(
                new[] { "C", "A", "B" },
                StrategyGuideOfferScheduleResolver.ResolveCandidateIds(
                    schedule,
                    new[] { "A", "B", "D" },
                    new[] { "A", "B", "C", "D" },
                    31));

            Assert.Throws<InvalidOperationException>(() =>
                StrategyGuideOfferScheduleResolver.ResolveCandidateIds(
                    schedule,
                    new[] { "A", "B", "D" },
                    new[] { "A", "B", "D" },
                    31));
        }

        [Test]
        public void Resolver_SelectsBySourceTriggerAndOccurrenceWithoutGuideIdBranch()
        {
            var plan = new StrategyGuideAcquisitionPlanDefinition
            {
                DiscloseControlledOffers = true,
                OfferSchedules = new List<StrategyGuideOfferScheduleDefinition>
                {
                    new StrategyGuideOfferScheduleDefinition
                    {
                        ScheduleId = "refresh-two",
                        Source = StrategyGuideOfferSources.ShopRefresh,
                        TriggerOccurrence = 2
                    },
                    new StrategyGuideOfferScheduleDefinition
                    {
                        ScheduleId = "spell-one",
                        Source = StrategyGuideOfferSources.TavernSpellDiscover,
                        TriggerCardId = "SPELL-1",
                        TriggerOccurrence = 1
                    }
                }
            };

            Assert.IsNull(StrategyGuideOfferScheduleResolver.FindSchedule(
                plan,
                StrategyGuideOfferSources.ShopRefresh,
                null,
                1));
            Assert.AreEqual(
                "refresh-two",
                StrategyGuideOfferScheduleResolver.FindSchedule(
                    plan,
                    StrategyGuideOfferSources.ShopRefresh,
                    null,
                    2).ScheduleId);
            Assert.IsNull(StrategyGuideOfferScheduleResolver.FindSchedule(
                plan,
                StrategyGuideOfferSources.TavernSpellDiscover,
                "OTHER",
                1));
            Assert.AreEqual(
                "spell-one",
                StrategyGuideOfferScheduleResolver.FindSchedule(
                    plan,
                    StrategyGuideOfferSources.TavernSpellDiscover,
                    "SPELL-1",
                    1).ScheduleId);

            plan.OfferSchedules[0].TriggerTavernTier = 5;
            Assert.IsNull(StrategyGuideOfferScheduleResolver.FindSchedule(
                plan,
                StrategyGuideOfferSources.ShopRefresh,
                null,
                2,
                4));
            Assert.AreEqual(
                "refresh-two",
                StrategyGuideOfferScheduleResolver.FindSchedule(
                    plan,
                    StrategyGuideOfferSources.ShopRefresh,
                    null,
                    2,
                    5).ScheduleId);
        }

        [Test]
        public void Validator_GuidedProfileRequiresNoUndoAndAValidDisclosedPlan()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var guided = AddGuidedProfile(guide);

            var valid = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());

            Assert.IsTrue(valid.IsValid, string.Join(" | ", valid.Errors));
            Assert.AreEqual(0, guided.Undo.UsesPerRun);
        }

        [Test]
        public void Validator_AllowsSeparateTierFourAndTierFiveTripleRewardRoutes()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var guided = AddGuidedProfile(guide);
            var version = ResolveSeason14();
            var tierFiveCore = version.Snapshot.Chinese.Minions.All.First(card =>
                card.InPool && card.TavernTier == 5).CardId;
            var tierSixCore = version.Snapshot.Chinese.Minions.All.First(card =>
                card.InPool && card.TavernTier == 6).CardId;
            guided.TavernTier = 4;
            guided.AcquisitionPlan.OfferSchedules = new List<StrategyGuideOfferScheduleDefinition>
            {
                TripleTierSchedule("hard-tier-five-core", 4, 5, tierFiveCore),
                TripleTierSchedule("hard-tier-six-core", 5, 6, tierSixCore)
            };

            var valid = StrategyGuideValidator.Validate(catalog, guide, version);

            Assert.IsTrue(valid.IsValid, string.Join(" | ", valid.Errors));

            guided.AcquisitionPlan.OfferSchedules[1].TargetCardIds = new List<string> { tierFiveCore };
            var invalid = StrategyGuideValidator.Validate(catalog, guide, version);

            CollectionAssert.Contains(
                invalid.Errors,
                "guide.acquisition.target.tier:hard-tier-six-core:" + tierFiveCore);
        }

        [Test]
        public void Compiler_DifficultProfileCanStartWithTwoTripleRewards()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddDifficultProfile(guide);
            profile.InitialTripleRewardCount = 2;

            var validation = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);

            Assert.IsTrue(validation.IsValid, string.Join(" | ", validation.Errors));
            Assert.AreEqual(
                2,
                session.MatchService.State.Player.Tavern.Hand.Count(card => card.CardId == "TRIPLE_REWARD"));

            profile.Difficulty = StrategyGuideDifficulties.GuidedDiscover;
            var invalid = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());
            CollectionAssert.Contains(invalid.Errors, "guide.triple-reward.difficulty");
        }

        [Test]
        public void Validator_RejectsUnknownRouteInvalidBoundsAndUndisclosedControl()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var guided = AddGuidedProfile(guide);
            var schedule = guided.AcquisitionPlan.OfferSchedules.Single();
            schedule.Source = "UnknownSource";
            schedule.TriggerOccurrence = 0;
            schedule.TriggerTavernTier = 8;
            schedule.OptionCount = 0;
            guided.AcquisitionPlan.DiscloseControlledOffers = false;

            var result = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());

            CollectionAssert.Contains(result.Errors, "guide.acquisition.source:guided-triple-core");
            CollectionAssert.Contains(result.Errors, "guide.acquisition.occurrence:guided-triple-core");
            CollectionAssert.Contains(result.Errors, "guide.acquisition.trigger-tier:guided-triple-core");
            CollectionAssert.Contains(result.Errors, "guide.acquisition.option-count:guided-triple-core");
            CollectionAssert.Contains(result.Errors, "guide.acquisition.disclosure");
        }

        [Test]
        public void Validator_RejectsDuplicateRouteBadPinnedCountAndUnknownTarget()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var guided = AddGuidedProfile(guide);
            var first = guided.AcquisitionPlan.OfferSchedules.Single();
            first.Policy = StrategyGuideOfferPolicies.Pinned;
            first.TargetCardIds = new List<string> { "UNKNOWN-GUIDED-TARGET" };
            guided.AcquisitionPlan.OfferSchedules.Add(new StrategyGuideOfferScheduleDefinition
            {
                ScheduleId = "duplicate-route",
                Source = first.Source,
                TriggerOccurrence = first.TriggerOccurrence,
                Policy = StrategyGuideOfferPolicies.NaturalSeeded,
                CardKind = StrategyGuideCardKinds.Minion,
                OptionCount = 3
            });

            var result = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());

            Assert.IsTrue(result.Errors.Any(error => error.StartsWith("guide.acquisition.route.duplicate-or-empty:", StringComparison.Ordinal)));
            CollectionAssert.Contains(result.Errors, "guide.acquisition.pinned-count:guided-triple-core");
            CollectionAssert.Contains(result.Errors, "guide.acquisition.target.missing:guided-triple-core:UNKNOWN-GUIDED-TARGET");
        }

        [Test]
        public void Validator_RejectsBadCardKindEmptyTargetAndPinnedOpenBuild()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddGuidedProfile(guide);
            profile.Difficulty = StrategyGuideDifficulties.OpenBuild;
            var schedule = profile.AcquisitionPlan.OfferSchedules.Single();
            schedule.Policy = StrategyGuideOfferPolicies.Pinned;
            schedule.CardKind = "AnyCard";
            schedule.TargetCardIds = new List<string> { string.Empty };

            var result = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());

            CollectionAssert.Contains(result.Errors, "guide.acquisition.card-kind:guided-triple-core");
            CollectionAssert.Contains(result.Errors, "guide.acquisition.target.empty:guided-triple-core");
            CollectionAssert.Contains(result.Errors, "guide.acquisition.open-build-pinned:guided-triple-core");
        }

        [Test]
        public void Validator_DifficultGreaterTrinketPlanRequiresUnequippedSlotAndValidTribeGate()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddDifficultProfile(guide);

            var valid = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());

            Assert.IsTrue(valid.IsValid, string.Join(" | ", valid.Errors));

            profile.UnequippedTrinketSlots.Clear();
            var schedule = profile.AcquisitionPlan.OfferSchedules.Single(item =>
                item.Source == StrategyGuideOfferSources.GreaterTrinketChoice);
            schedule.RequiredTribe = "Totem";
            schedule.MinimumRequiredTribeMinions = 8;

            var invalid = StrategyGuideValidator.Validate(catalog, guide, ResolveSeason14());

            CollectionAssert.Contains(invalid.Errors, "guide.acquisition.trinket-slot:hard-greater-trinket");
            CollectionAssert.Contains(invalid.Errors, "guide.acquisition.required-tribe:hard-greater-trinket");
            CollectionAssert.Contains(invalid.Errors, "guide.acquisition.required-tribe-count:hard-greater-trinket");
        }

        [Test]
        public void Session_DifficultGreaterTrinketMustIncludeIsDeterministicAndRestartRestoresPreOfferState()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddDifficultProfile(guide);
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);

            Assert.AreEqual(8, session.MatchService.State.Round);
            Assert.IsTrue(string.IsNullOrEmpty(
                session.MatchService.State.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId));
            Assert.AreEqual(0, session.UndoUsesRemaining);

            session.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));
            session.Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition));

            var active = session.MatchService.State.ChoiceQueue.ActiveChoice;
            Assert.AreEqual(ChoiceRequestKind.Trinket, active.Kind);
            Assert.AreEqual(4, active.Options.Count);
            CollectionAssert.Contains(active.Options.Select(option => option.SourceId), guide.GreaterTrinketCardId);
            Assert.AreEqual("hard-greater-trinket", session.ActiveOfferSchedule.ScheduleId);
            var first = active.Options.Select(option => option.SourceId).ToList();

            session.Restart();
            Assert.AreEqual(8, session.MatchService.State.Round);
            Assert.IsNull(session.MatchService.State.ChoiceQueue.ActiveChoice);
            Assert.IsTrue(string.IsNullOrEmpty(
                session.MatchService.State.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId));

            session.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));
            session.Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition));
            CollectionAssert.AreEqual(
                first,
                session.MatchService.State.ChoiceQueue.ActiveChoice.Options.Select(option => option.SourceId));
        }

        [Test]
        public void Session_DifficultGreaterTrinketGateFailureIsAtomicAndDoesNotConsumeOccurrence()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddDifficultProfile(guide);
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);
            session.MatchService.State.Player.Board.Clear();

            session.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));
            var error = Assert.Throws<InvalidOperationException>(() =>
                session.Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition)));

            StringAssert.Contains("required tribe gate", error.Message);
            Assert.AreEqual(8, session.MatchService.State.Round);
            Assert.IsNull(session.MatchService.State.ChoiceQueue.ActiveChoice);
            Assert.IsNull(session.ActiveOfferSchedule);
        }

        [Test]
        public void Session_AppliesTripleScheduleDeterministicallyAndRestartResetsOccurrence()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddGuidedProfile(guide);
            var targetCardId = profile.AcquisitionPlan.OfferSchedules.Single().TargetCardIds.Single();
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);

            AddTripleReward(session);
            session.Apply(new GameCommand(GameCommandType.PlayMinion, session.MatchService.State.Player.Tavern.Hand.Count - 1));
            var first = session.MatchService.State.Player.Tavern.Discover.Options.Select(item => item.CardId).ToList();

            CollectionAssert.Contains(first, targetCardId);
            Assert.AreEqual("guided-triple-core", session.ActiveOfferSchedule.ScheduleId);
            Assert.AreEqual(0, session.UndoUsesRemaining);
            Assert.IsFalse(session.CanUndo);

            session.Restart();
            AddTripleReward(session);
            session.Apply(new GameCommand(GameCommandType.PlayMinion, session.MatchService.State.Player.Tavern.Hand.Count - 1));
            CollectionAssert.AreEqual(
                first,
                session.MatchService.State.Player.Tavern.Discover.Options.Select(item => item.CardId));
        }

        [Test]
        public void Session_ShopSchedulePreservesFrozenSlotAndUsesRealPoolSupply()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddGuidedProfile(guide);
            if (!profile.AllowedCommands.Contains(GameCommandType.RerollShop.ToString()))
            {
                profile.AllowedCommands.Add(GameCommandType.RerollShop.ToString());
            }
            var schedule = profile.AcquisitionPlan.OfferSchedules.Single();
            schedule.Source = StrategyGuideOfferSources.ShopRefresh;
            schedule.TriggerTavernTier = profile.TavernTier;
            schedule.TavernTier = 0;
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);
            var tavern = session.MatchService.State.Player.Tavern;
            TavernShopSlots.SetSlotFrozen(tavern, 0, true);
            var frozenInstanceId = tavern.Shop[0].InstanceId;
            var beforeGold = tavern.Gold;
            var targetCardId = schedule.TargetCardIds.Single();

            session.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual(beforeGold - 1, tavern.Gold);
            Assert.AreEqual(frozenInstanceId, tavern.Shop[0].InstanceId);
            Assert.IsTrue(TavernShopSlots.IsSlotFrozen(tavern, 0));
            Assert.IsTrue(tavern.Shop.Any(card =>
                card != null &&
                card.CardId == targetCardId &&
                card.PoolSource == PoolSource.Pool &&
                card.PoolCopiesHeld == 1));
            Assert.AreEqual("guided-triple-core", session.ActiveOfferSchedule.ScheduleId);
        }

        [Test]
        public void Session_ShopScheduleFailureRestoresRefreshAndDoesNotConsumeOccurrence()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddGuidedProfile(guide);
            if (!profile.AllowedCommands.Contains(GameCommandType.RerollShop.ToString()))
            {
                profile.AllowedCommands.Add(GameCommandType.RerollShop.ToString());
            }
            var schedule = profile.AcquisitionPlan.OfferSchedules.Single();
            schedule.Source = StrategyGuideOfferSources.ShopRefresh;
            schedule.TriggerTavernTier = profile.TavernTier;
            schedule.TavernTier = 0;
            var version = ResolveSeason14();
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                version,
                profileId: profile.ProfileId);
            var tavern = session.MatchService.State.Player.Tavern;
            session.MatchService.Catalogs.Minions.TryGetByCardId(schedule.TargetCardIds.Single(), out var target);
            tavern.Pool[target.Id] = 0;
            var beforeGold = tavern.Gold;
            var beforeShop = tavern.Shop.Select(item => item?.CardId).ToList();

            var error = Assert.Throws<InvalidOperationException>(() =>
                session.Apply(new GameCommand(GameCommandType.RerollShop)));

            StringAssert.Contains("could not be applied", error.Message);
            Assert.AreEqual(beforeGold, tavern.Gold);
            CollectionAssert.AreEqual(beforeShop, tavern.Shop.Select(item => item?.CardId));
            Assert.IsNull(session.ActiveOfferSchedule);

            tavern.Pool[target.Id] = 1;
            session.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.IsTrue(tavern.Shop.Any(card => card?.CardId == target.CardId));
            Assert.AreEqual("guided-triple-core", session.ActiveOfferSchedule.ScheduleId);
        }

        [Test]
        public void Session_ShopOccurrenceSurvivesScenarioRoundTrip()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddGuidedProfile(guide);
            if (!profile.AllowedCommands.Contains(GameCommandType.RerollShop.ToString()))
            {
                profile.AllowedCommands.Add(GameCommandType.RerollShop.ToString());
            }
            var schedule = profile.AcquisitionPlan.OfferSchedules.Single();
            schedule.Source = StrategyGuideOfferSources.ShopRefresh;
            schedule.TriggerTavernTier = profile.TavernTier;
            schedule.TriggerOccurrence = 2;
            schedule.TavernTier = 0;
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                ResolveSeason14(),
                profileId: profile.ProfileId);

            session.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.IsNull(session.ActiveOfferSchedule);
            var resumed = TestScenarioMapper.Capture(
                session.MatchService.State,
                "guided-refresh-occurrence-roundtrip");
            TestScenarioMapper.ApplyTo(session.MatchService.State, resumed);

            session.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.IsTrue(session.MatchService.State.Player.Tavern.Shop.Any(card =>
                card?.CardId == schedule.TargetCardIds.Single()));
            Assert.AreEqual(schedule.ScheduleId, session.ActiveOfferSchedule.ScheduleId);
        }

        [Test]
        public void Session_TavernSpellSchedulePreservesDiscoverDeliveryMetadata()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = AddGuidedProfile(guide);
            var version = ResolveSeason14();
            var target = version.Snapshot.Chinese.Minions.All.First(minion =>
                minion.InPool &&
                minion.TavernTier == profile.TavernTier &&
                minion.Tribes.Contains(Tribe.Undead));
            var schedule = profile.AcquisitionPlan.OfferSchedules.Single();
            schedule.Source = StrategyGuideOfferSources.TavernSpellDiscover;
            schedule.TriggerCardId = "126957";
            schedule.TavernTier = profile.TavernTier;
            schedule.TargetCardIds = new List<string> { target.CardId };
            var session = StrategyGuideSession.Start(
                catalog,
                guide.GuideId,
                version,
                profileId: profile.ProfileId);
            if (!session.MatchService.State.ActiveTribes.Contains(Tribe.Undead))
            {
                session.MatchService.State.ActiveTribes.Add(Tribe.Undead);
            }
            session.MatchService.Apply(new GameCommand(
                GameCommandType.AddCardToHand,
                "126957",
                CardKind.TavernSpell));
            var spellIndex = session.MatchService.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "126957");

            session.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex));

            var discover = session.MatchService.State.Player.Tavern.Discover;
            var optionIndex = discover.Options.FindIndex(option => option.CardId == target.CardId);
            Assert.GreaterOrEqual(optionIndex, 0);
            Assert.IsTrue(discover.Options[optionIndex].Tags.Contains("discover_then_death"));
            Assert.AreEqual(session.MatchService.State.Round, discover.Options[optionIndex].Counters["disturbed-grave-round"]);
            Assert.AreEqual("guided-triple-core", session.ActiveOfferSchedule.ScheduleId);

            var scenario = TestScenarioMapper.Capture(
                session.MatchService.State,
                "guided-spell-metadata-roundtrip");
            TestScenarioMapper.ApplyTo(session.MatchService.State, scenario);
            discover = session.MatchService.State.Player.Tavern.Discover;
            optionIndex = discover.Options.FindIndex(option => option.CardId == target.CardId);
            CollectionAssert.Contains(discover.OptionTags, "discover_then_death");
            Assert.AreEqual("disturbed-grave-round", discover.OptionCounters.Single().Key);

            session.Apply(new GameCommand(GameCommandType.ChooseDiscover, optionIndex));
            var acquired = session.MatchService.State.Player.Tavern.Hand.Single(card => card.CardId == target.CardId);
            var acquiredInstanceId = acquired.InstanceId;
            Assert.IsTrue(acquired.Tags.Contains("discover_then_death"));
            session.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                session.MatchService.State.Player.Tavern.Hand.IndexOf(acquired)));
            Assert.IsFalse(session.MatchService.State.Player.Board.Any(card => card.InstanceId == acquiredInstanceId));
        }

        [Test]
        public void FrozenGuidedBeastSample_CompletesTwoTriplesAndGuaranteesDeathstrider()
        {
            var session = StartFrozenGuided("GUIDE-S14-BEAST-LOBSTER-RALLY");
            var tavern = session.MatchService.State.Player.Tavern;
            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardId == "BG36_210" && !card.Golden));
            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardId == "BG36_208" && !card.Golden));

            var hyenaIndex = tavern.Shop.FindIndex(card => card?.CardId == "BG36_210");
            Assert.GreaterOrEqual(hyenaIndex, 0);
            session.Apply(new GameCommand(GameCommandType.BuyMinion, hyenaIndex));

            PlayGolden(session, "BG36_210");
            PlayTripleReward(session);
            var optionIndex = tavern.Discover.Options.FindIndex(card => card.CardId == "BG36_208");
            Assert.GreaterOrEqual(optionIndex, 0);
            Assert.AreEqual("beast-guided-deathstrider", session.ActiveOfferSchedule.ScheduleId);
            session.Apply(new GameCommand(GameCommandType.ChooseDiscover, optionIndex));

            Assert.AreEqual(2, PlayerCards(session).Count(card => card.CardId == "BG36_208" && card.Golden));
            PlayGolden(session, "BG36_208", "beast-guided-deathstrider-a");
            Assert.IsTrue(tavern.Hand.Any(IsTripleReward), "The second triple reward must remain available.");
        }

        [Test]
        public void FrozenGuidedMechSample_UsesBoundlessPotentialToCompleteSecondGoldenGlambot()
        {
            var session = StartFrozenGuided("GUIDE-S14-MECH-SPELL-SATELLITE");
            var tavern = session.MatchService.State.Player.Tavern;
            var spellIndex = tavern.Hand.FindIndex(card => card.CardId == "115910");
            Assert.GreaterOrEqual(spellIndex, 0);

            session.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                spellIndex,
                -1,
                TargetZone.Unspecified,
                -1,
                TargetZone.Unspecified,
                choiceId: "minion"));

            var optionIndex = tavern.Discover.Options.FindIndex(card => card.CardId == "BG36_853");
            Assert.GreaterOrEqual(optionIndex, 0);
            Assert.AreEqual("mech-guided-glambot", session.ActiveOfferSchedule.ScheduleId);
            session.Apply(new GameCommand(GameCommandType.ChooseDiscover, optionIndex));

            Assert.AreEqual(2, PlayerCards(session).Count(card => card.CardId == "BG36_853" && card.Golden));
            PlayGolden(session, "BG36_853", "mech-guided-glambot-a");
            Assert.IsTrue(tavern.Hand.Any(IsTripleReward));
        }

        [Test]
        public void FrozenGuidedDemonSample_FirstTierSixRefreshCompletesGoldenEredar()
        {
            var session = StartFrozenGuided("GUIDE-S14-DEMON-TAVERN-CONSUME");
            var tavern = session.MatchService.State.Player.Tavern;
            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardId == "BG36_733" && !card.Golden));
            Assert.AreEqual(1, session.RemainingOfferScheduleCount);
            StringAssert.Contains("待触发 1/1", session.AcquisitionStatus(false));

            session.Apply(new GameCommand(GameCommandType.RerollShop));

            var eredarIndex = tavern.Shop.FindIndex(card => card?.CardId == "BG36_733");
            Assert.GreaterOrEqual(eredarIndex, 0);
            Assert.AreEqual("demon-guided-tier-six-eredar", session.ActiveOfferSchedule.ScheduleId);
            Assert.AreEqual(0, session.RemainingOfferScheduleCount);
            StringAssert.Contains("已触发 0/1", session.AcquisitionStatus(false));
            session.Apply(new GameCommand(GameCommandType.BuyMinion, eredarIndex));

            Assert.AreEqual(1, PlayerCards(session).Count(card => card.CardId == "BG36_733" && card.Golden));
            PlayGolden(session, "BG36_733");
            Assert.IsTrue(tavern.Hand.Any(IsTripleReward));
        }

        [Test]
        public void FrozenDifficultSamples_SecondTierFourRefreshIncludesAuxiliaryCore()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();
            foreach (var guide in catalog.Guides)
            {
                var profile = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.OpenBuild);
                var schedule = profile.AcquisitionPlan.OfferSchedules.Single(item =>
                    item.Source == StrategyGuideOfferSources.ShopRefresh);
                Assert.AreEqual(4, schedule.TriggerTavernTier, guide.GuideId);
                Assert.AreEqual(2, schedule.TriggerOccurrence, guide.GuideId);
                Assert.That(schedule.TavernTier, Is.InRange(1, 4), guide.GuideId);
                Assert.AreEqual(TavernRules.GetShopSize(4), schedule.OptionCount, guide.GuideId);
                Assert.AreEqual(1, schedule.TargetCardIds.Count, guide.GuideId);
                Assert.Contains(schedule.TargetCardIds[0], guide.CoreMinionCardIds, guide.GuideId);

                var session = StrategyGuideSession.Start(
                    catalog,
                    guide.GuideId,
                    version,
                    profileId: profile.ProfileId);
                var tavern = session.MatchService.State.Player.Tavern;
                Assert.AreEqual(4, tavern.Tier, guide.GuideId);
                Assert.IsNull(session.ActiveOfferSchedule, guide.GuideId);

                session.Apply(new GameCommand(GameCommandType.RerollShop));

                Assert.IsNull(session.ActiveOfferSchedule, guide.GuideId + " must not trigger on the first refresh.");
                Assert.AreEqual(5, tavern.Shop.Count(card => card != null && card.CardKind == CardKind.Minion), guide.GuideId);

                session.Apply(new GameCommand(GameCommandType.RerollShop));

                Assert.AreEqual(schedule.ScheduleId, session.ActiveOfferSchedule?.ScheduleId, guide.GuideId);
                Assert.IsTrue(tavern.Shop.Any(card => card?.CardId == schedule.TargetCardIds[0]), guide.GuideId);
                Assert.AreEqual(5, tavern.Shop.Count(card => card != null && card.CardKind == CardKind.Minion), guide.GuideId);
            }
        }

        [Test]
        public void FrozenDifficultSamples_EnterRoundNineWithRequiredTribeAndChooseDeclaredGreaterTrinket()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();
            foreach (var guide in catalog.Guides)
            {
                var profile = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.OpenBuild);
                var schedule = profile.AcquisitionPlan.OfferSchedules.Single(item =>
                    item.Source == StrategyGuideOfferSources.GreaterTrinketChoice);
                Assert.AreEqual(StrategyGuideOfferSources.GreaterTrinketChoice, schedule.Source);
                Assert.AreEqual(8, profile.StartRound);
                Assert.Contains(schedule.RequiredTribe, guide.ActiveTribes);
                Assert.AreEqual(0, profile.Undo.UsesPerRun);

                var session = StrategyGuideSession.Start(
                    catalog,
                    guide.GuideId,
                    version,
                    profileId: profile.ProfileId);
                Assert.IsTrue(string.IsNullOrEmpty(
                    session.MatchService.State.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId));
                Assert.IsTrue(session.MatchService.State.Player.Board.Count(card =>
                    card.Tribes.Contains((Tribe)Enum.Parse(typeof(Tribe), schedule.RequiredTribe, true)) ||
                    card.Tribes.Contains(Tribe.All)) >= schedule.MinimumRequiredTribeMinions);
                Assert.AreEqual(
                    profile.InitialTripleRewardCount,
                    session.MatchService.State.Player.Tavern.Hand.Count(IsTripleReward),
                    "Difficult entry must carry its authored Triple Rewards: " + guide.GuideId);

                session.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));
                session.Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition));
                var choice = session.MatchService.State.ChoiceQueue.ActiveChoice;
                var targetIndex = choice.Options.FindIndex(option =>
                    schedule.TargetCardIds.Contains(option.SourceId));
                Assert.GreaterOrEqual(targetIndex, 0, guide.GuideId);
                var chosenGreaterTrinketId = choice.Options[targetIndex].SourceId;
                Assert.IsTrue(profile.AcquisitionPlan.DiscloseControlledOffers, guide.GuideId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(schedule.Label), guide.GuideId);

                session.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, targetIndex));

                Assert.AreEqual(
                    chosenGreaterTrinketId,
                    session.MatchService.State.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId);
                if (session.ActionProgress.Count > 0)
                {
                    Assert.IsTrue(session.ActionProgress.First().IsComplete, guide.GuideId);
                }
                Assert.IsFalse(session.CanUndo);
            }
        }

        [Test]
        public void FrozenDifficultSamples_FollowTierFiveGreaterTrinketAndTierSixChain()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();
            foreach (var guide in catalog.Guides)
            {
                var profile = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.OpenBuild);
                var tierFiveSchedule = profile.AcquisitionPlan.OfferSchedules.Single(item =>
                    item.Source == StrategyGuideOfferSources.TripleRewardDiscover &&
                    item.TriggerTavernTier == 4);
                var tierSixSchedule = profile.AcquisitionPlan.OfferSchedules.Single(item =>
                    item.Source == StrategyGuideOfferSources.TripleRewardDiscover &&
                    item.TriggerTavernTier == 5);
                var greaterSchedule = profile.AcquisitionPlan.OfferSchedules.Single(item =>
                    item.Source == StrategyGuideOfferSources.GreaterTrinketChoice);
                var session = StrategyGuideSession.Start(
                    catalog,
                    guide.GuideId,
                    version,
                    profileId: profile.ProfileId);
                var tavern = session.MatchService.State.Player.Tavern;

                Assert.AreEqual(4, tavern.Tier, guide.GuideId);
                Assert.AreEqual(7, session.MatchService.State.Player.Board.Count, guide.GuideId);
                Assert.AreEqual(5, tavern.Shop.Count(card => card.CardKind == CardKind.Minion), guide.GuideId);
                Assert.AreEqual(2, tavern.Hand.Count(IsTripleReward), guide.GuideId);

                PlayTripleReward(session);
                var tierFiveIndex = tavern.Discover.Options.FindIndex(card =>
                    card.CardId == tierFiveSchedule.TargetCardIds[0]);
                Assert.GreaterOrEqual(tierFiveIndex, 0, guide.GuideId);
                session.Apply(new GameCommand(GameCommandType.ChooseDiscover, tierFiveIndex));

                session.Apply(new GameCommand(GameCommandType.BeginNextTurnTransition));
                session.Apply(new GameCommand(GameCommandType.ContinueNextTurnTransition));
                var greaterChoice = session.MatchService.State.ChoiceQueue.ActiveChoice;
                var greaterIndex = greaterChoice.Options.FindIndex(option =>
                    greaterSchedule.TargetCardIds.Contains(option.SourceId));
                Assert.GreaterOrEqual(greaterIndex, 0, guide.GuideId);
                session.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, greaterIndex));

                session.Apply(new GameCommand(
                    GameCommandType.SellMinion,
                    session.MatchService.State.Player.Board.Last().InstanceId));
                Assert.AreEqual(6, session.MatchService.State.Player.Board.Count, guide.GuideId);
                session.Apply(new GameCommand(GameCommandType.UpgradeTavern, 0));
                Assert.AreEqual(5, session.MatchService.State.Player.Tavern.Tier, guide.GuideId);

                PlayTripleReward(session);
                var tierSixIndex = session.MatchService.State.Player.Tavern.Discover.Options.FindIndex(card =>
                    card.CardId == tierSixSchedule.TargetCardIds[0]);
                Assert.GreaterOrEqual(tierSixIndex, 0, guide.GuideId);
                session.Apply(new GameCommand(GameCommandType.ChooseDiscover, tierSixIndex));
            }
        }

        private static StrategyGuideOfferScheduleDefinition Schedule(string policy, params string[] targets)
        {
            return new StrategyGuideOfferScheduleDefinition
            {
                ScheduleId = "resolver-probe",
                Source = StrategyGuideOfferSources.TripleRewardDiscover,
                TriggerOccurrence = 1,
                Policy = policy,
                CardKind = StrategyGuideCardKinds.Minion,
                OptionCount = 3,
                TargetCardIds = targets.ToList(),
                Label = "受控候选"
            };
        }

        private static StrategyGuideOfferScheduleDefinition TripleTierSchedule(
            string scheduleId,
            int triggerTavernTier,
            int rewardTier,
            string targetCardId)
        {
            return new StrategyGuideOfferScheduleDefinition
            {
                ScheduleId = scheduleId,
                Source = StrategyGuideOfferSources.TripleRewardDiscover,
                TriggerTavernTier = triggerTavernTier,
                TriggerOccurrence = 1,
                Policy = StrategyGuideOfferPolicies.MustInclude,
                CardKind = StrategyGuideCardKinds.Minion,
                TavernTier = rewardTier,
                OptionCount = 3,
                TargetCardIds = new List<string> { targetCardId },
                Label = "受控三连发现",
                EnglishLabel = "Controlled Triple Reward"
            };
        }

        private static StrategyGuideSession StartFrozenGuided(string guideId)
        {
            return StrategyGuideSession.Start(
                StrategyGuideCatalogLoader.LoadFromResources(),
                guideId,
                ResolveSeason14(),
                profileId: "guided");
        }

        private static void PlayTripleReward(StrategyGuideSession session)
        {
            var hand = session.MatchService.State.Player.Tavern.Hand;
            var rewardIndex = hand.FindIndex(IsTripleReward);
            Assert.GreaterOrEqual(rewardIndex, 0, "A completed triple must grant its reward.");
            session.Apply(new GameCommand(GameCommandType.PlayMinion, rewardIndex));
        }

        private static void PlayGolden(
            StrategyGuideSession session,
            string cardId,
            string excludedPlacementId = null)
        {
            var hand = session.MatchService.State.Player.Tavern.Hand;
            var goldenIndex = hand.FindIndex(card =>
                card.CardId == cardId &&
                card.Golden &&
                (string.IsNullOrEmpty(excludedPlacementId) ||
                 !card.Tags.Contains("strategy-guide-placement:" + excludedPlacementId)));
            Assert.GreaterOrEqual(goldenIndex, 0, "The completed Golden minion must be playable.");
            session.Apply(new GameCommand(GameCommandType.PlayMinion, goldenIndex));
        }

        private static IEnumerable<MinionInstance> PlayerCards(StrategyGuideSession session)
        {
            return session.MatchService.State.Player.Tavern.Hand
                .Concat(session.MatchService.State.Player.Board);
        }

        private static bool IsTripleReward(MinionInstance card)
        {
            return card?.CardId == "TRIPLE_REWARD";
        }

        private static StrategyGuideEntryProfileDefinition AddGuidedProfile(StrategyGuideDefinition guide)
        {
            var guided = JsonUtility.FromJson<StrategyGuideEntryProfileDefinition>(
                JsonUtility.ToJson(guide.EntryProfiles.Single(profile =>
                    profile.Difficulty == StrategyGuideDifficulties.Showcase)));
            guided.ProfileId = "guided-probe";
            guided.Difficulty = StrategyGuideDifficulties.GuidedDiscover;
            guided.Title = "初级模式";
            guided.EnglishTitle = "Guided";
            guided.Undo.UsesPerRun = 0;
            guided.AcquisitionPlan = new StrategyGuideAcquisitionPlanDefinition
            {
                DiscloseControlledOffers = true,
                OfferSchedules = new List<StrategyGuideOfferScheduleDefinition>
                {
                    new StrategyGuideOfferScheduleDefinition
                    {
                        ScheduleId = "guided-triple-core",
                        Source = StrategyGuideOfferSources.TripleRewardDiscover,
                        TriggerOccurrence = 1,
                        Policy = StrategyGuideOfferPolicies.MustInclude,
                        CardKind = StrategyGuideCardKinds.Minion,
                        OptionCount = 3,
                        TargetCardIds = new List<string> { guide.CoreMinionCardIds[2] },
                        Label = "引导发现：保证出现一张核心随从",
                        EnglishLabel = "Guided Discover: one core minion is guaranteed"
                    }
                }
            };
            guide.EntryProfiles.Add(guided);
            return guided;
        }

        private static StrategyGuideEntryProfileDefinition AddDifficultProfile(StrategyGuideDefinition guide)
        {
            var profile = JsonUtility.FromJson<StrategyGuideEntryProfileDefinition>(
                JsonUtility.ToJson(guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.Showcase)));
            profile.ProfileId = "hard-probe";
            profile.Difficulty = StrategyGuideDifficulties.OpenBuild;
            profile.Title = "困难模式";
            profile.EnglishTitle = "Difficult";
            profile.StartRound = 8;
            profile.TavernTier = 4;
            profile.InitialTripleRewardCount = 2;
            profile.Undo.UsesPerRun = 0;
            if (!profile.AllowedCommands.Contains(GameCommandType.RerollShop.ToString()))
            {
                profile.AllowedCommands.Add(GameCommandType.RerollShop.ToString());
            }
            if (!profile.AllowedCommands.Contains(GameCommandType.UpgradeTavern.ToString()))
            {
                profile.AllowedCommands.Add(GameCommandType.UpgradeTavern.ToString());
            }
            var fillerTemplate = profile.Placements.First(item =>
                item.CardKind == StrategyGuideCardKinds.Minion &&
                item.Zone == StrategyGuideZones.Board);
            while (profile.Placements.Count(item => item.Zone == StrategyGuideZones.Board) < 7)
            {
                var filler = JsonUtility.FromJson<StrategyGuideCardDefinition>(JsonUtility.ToJson(fillerTemplate));
                filler.PlacementId = "hard-board-filler-" + profile.Placements.Count(item =>
                    item.Zone == StrategyGuideZones.Board);
                filler.Golden = false;
                profile.Placements.Add(filler);
            }
            while (profile.Placements.Count(item =>
                item.Zone == StrategyGuideZones.Shop &&
                item.CardKind == StrategyGuideCardKinds.Minion) < 5)
            {
                var shopFiller = JsonUtility.FromJson<StrategyGuideCardDefinition>(JsonUtility.ToJson(fillerTemplate));
                shopFiller.PlacementId = "hard-shop-filler-" + profile.Placements.Count(item =>
                    item.Zone == StrategyGuideZones.Shop &&
                    item.CardKind == StrategyGuideCardKinds.Minion);
                shopFiller.Zone = StrategyGuideZones.Shop;
                shopFiller.Golden = false;
                profile.Placements.Add(shopFiller);
            }
            profile.UnequippedTrinketSlots = new List<string> { TrinketSlotKind.Greater.ToString() };
            profile.AcquisitionPlan = new StrategyGuideAcquisitionPlanDefinition
            {
                DiscloseControlledOffers = true,
                OfferSchedules = new List<StrategyGuideOfferScheduleDefinition>
                {
                    new StrategyGuideOfferScheduleDefinition
                    {
                        ScheduleId = "hard-tier-four-refresh-core",
                        Source = StrategyGuideOfferSources.ShopRefresh,
                        TriggerTavernTier = 4,
                        TriggerOccurrence = 2,
                        Policy = StrategyGuideOfferPolicies.MustInclude,
                        CardKind = StrategyGuideCardKinds.Minion,
                        OptionCount = TavernRules.GetShopSize(4),
                        TargetCardIds = new List<string> { guide.CoreMinionCardIds[0] },
                        Label = "困难教学：四本酒馆第二次刷新必含前期核心",
                        EnglishLabel = "Difficult lesson: the second Tier 4 refresh includes the early core"
                    },
                    TripleTierSchedule("hard-tier-five-core", 4, 5, guide.CoreMinionCardIds[1]),
                    TripleTierSchedule("hard-tier-six-core", 5, 6, guide.CoreMinionCardIds[2]),
                    new StrategyGuideOfferScheduleDefinition
                    {
                        ScheduleId = "hard-greater-trinket",
                        Source = StrategyGuideOfferSources.GreaterTrinketChoice,
                        TriggerOccurrence = 1,
                        Policy = StrategyGuideOfferPolicies.MustInclude,
                        CardKind = StrategyGuideCardKinds.Trinket,
                        OptionCount = 4,
                        TargetCardIds = new List<string> { guide.GreaterTrinketCardId },
                        RequiredTribe = guide.RequiredTribes[0],
                        MinimumRequiredTribeMinions = 3,
                        Label = "大饰品池教学：目标种族达标后必含核心大饰品",
                        EnglishLabel = "Greater Trinket pool lesson: include the core Trinket after meeting the tribe gate"
                    }
                }
            };
            guide.EntryProfiles.Add(profile);
            return profile;
        }

        private static void AddTripleReward(StrategyGuideSession session)
        {
            session.MatchService.State.Player.Tavern.Hand.Add(new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "guided-triple-reward",
                DefinitionId = "triple-reward",
                CardId = "TRIPLE_REWARD",
                Name = "Triple Reward",
                Cost = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Discover, Keyword.TavernSpell }
            });
        }

        private static ResolvedGameVersion ResolveSeason14()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            return snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
        }
    }
}
