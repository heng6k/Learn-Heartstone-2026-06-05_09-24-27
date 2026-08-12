using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14HeroPowerTests
    {
        private const string XaviusHeroCardId = "TEST_SEASON14_XAVIUS";
        private const string XaviusPowerCardId = "TEST_SEASON14_XAVIUS_POWER";
        private const string TrastathHeroCardId = "TEST_SEASON14_TRASTATH";
        private const string TrastathPowerCardId = "TEST_SEASON14_TRASTATH_POWER";
        private const string WaxLanceCardId = "TEST_SEASON14_WAX_LANCE";
        private const string LockedTurnsCounter = "locked-turns";

        [Test]
        public void Xavius_TurnFourQueuesNormalDarkGiftOfferAndChosenMinionUsesSharedStateMachine()
        {
            var service = CreateService(XaviusHeroCardId);

            Assert.IsFalse(HasDarkGiftChoice(service.State));
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsFalse(HasDarkGiftChoice(service.State));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var choice = service.State.ChoiceQueue.ActiveChoice;
            Assert.AreEqual(4, service.State.Round);
            Assert.AreEqual(ChoiceRequestKind.DarkGift, choice.Kind);
            Assert.AreEqual(Season14DarkGiftSourceService.XaviusSourceId, choice.Source);
            Assert.AreEqual(3, choice.Options.Count);
            Assert.IsTrue(choice.Options.All(option => option.DifficultyTier >= 2 && option.DifficultyTier <= 3));

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var acquired = service.State.PlayerDarkGifts.AcquiredGiftInstances.Single();
            var minion = service.State.Player.Tavern.Hand.Single(item => item.InstanceId == acquired.InstanceId);
            Assert.IsTrue(minion.Tags.Contains("test-dark-gift-applied"));
            Assert.IsTrue(service.State.ChoiceQueue.CompletedRequestIds.Contains(choice.RequestId));
        }

        [Test]
        public void Trastath_OpeningChoiceUsesExclusiveTierFivePoolPersistsAndUnlocksOnTurnSeven()
        {
            var service = CreateService(TrastathHeroCardId);
            var choice = service.State.ChoiceQueue.ActiveChoice;

            Assert.AreEqual(1, service.State.Round);
            Assert.AreEqual(ChoiceRequestKind.DarkGift, choice.Kind);
            Assert.AreEqual(Season14DarkGiftSourceService.TrastathSourceId, choice.Source);
            Assert.AreEqual(3, choice.Options.Count);
            Assert.IsTrue(choice.Options.All(option => option.DifficultyTier == 5));
            Assert.IsTrue(choice.ResolutionMetadata.Any(item =>
                item.Key == Season14DarkGiftSourceService.GiftPoolMetadataKey &&
                item.Value == Season14DarkGiftSourceService.TrastathGiftPoolId));

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            var selected = service.State.Player.Tavern.Hand.Single();
            Assert.AreEqual(6, selected.Counters[LockedTurnsCounter]);
            Assert.IsTrue(selected.Tags.Contains("locked_in_hand"));
            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(service.State, "trastath-turn-seven-lock"));
            var restored = CreateService(XaviusHeroCardId).State;
            Assert.AreEqual(TestScenarioRestoreStatus.Applied, TestScenarioMapper.TryApplyTo(restored, scenario).Status);
            Assert.AreEqual(6, restored.Player.Tavern.Hand.Single().Counters[LockedTurnsCounter]);
            Assert.AreEqual(1, restored.PlayerDarkGifts.AcquiredGiftInstances.Count);

            for (var turn = 2; turn <= 7; turn += 1)
            {
                service.Apply(new GameCommand(GameCommandType.NextTurn));
            }

            Assert.AreEqual(7, service.State.Round);
            var unlocked = service.State.Player.Tavern.Hand.Single(item => item.InstanceId == selected.InstanceId);
            Assert.IsFalse(unlocked.Counters.ContainsKey(LockedTurnsCounter));
            Assert.IsFalse(unlocked.Tags.Contains("locked_in_hand"));
        }

        [Test]
        public void WaxLance_EquipQueuesTierSevenOfferThroughSameCandidateAndChoicePipeline()
        {
            var service = CreateService(XaviusHeroCardId, enableTrinkets: true);
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, WaxLanceCardId, CardKind.Trinket, 1));

            var choice = service.State.ChoiceQueue.ActiveChoice;
            Assert.AreEqual(ChoiceRequestKind.DarkGift, choice.Kind);
            Assert.AreEqual(Season14DarkGiftSourceService.WaxLanceSourceId, choice.Source);
            Assert.AreEqual(3, choice.Options.Count);
            Assert.IsTrue(choice.Options.All(option => option.DifficultyTier == 7));

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, service.State.PlayerDarkGifts.AcquiredGiftInstances.Count);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(item => item.Tags.Contains("test-dark-gift-applied")));
        }

        [Test]
        public void NormalEntry_CostsThreeGoldAndEnforcesRoundTurnAndMatchLimits()
        {
            var service = CreateService(null);
            Assert.IsFalse(service.CanUseNormalDarkGift());

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.State.Player.Tavern.Gold = 10;
            Assert.IsTrue(service.CanUseNormalDarkGift());

            service.Apply(new GameCommand(GameCommandType.UseNormalDarkGift));

            var choice = service.State.ChoiceQueue.ActiveChoice;
            Assert.AreEqual(ChoiceRequestKind.DarkGift, choice.Kind);
            Assert.AreEqual(Season14DarkGiftSourceService.NormalEntrySourceId, choice.Source);
            Assert.AreEqual(3, choice.Options.Count);
            Assert.IsTrue(choice.Options.All(option => option.DifficultyTier == 2));
            Assert.IsTrue(choice.ResolutionMetadata.Any(item =>
                item.Key == Season14DarkGiftSourceService.GoldCostMetadataKey && item.Value == "3"));
            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
            Assert.AreEqual(2, service.NormalDarkGiftUsesRemaining());

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            Assert.IsFalse(service.CanUseNormalDarkGift(), "Normal entry is limited to once per turn.");
            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseNormalDarkGift)));

            for (var round = 4; round <= 5; round += 1)
            {
                service.Apply(new GameCommand(GameCommandType.NextTurn));
                service.State.Player.Tavern.Gold = 10;
                service.Apply(new GameCommand(GameCommandType.UseNormalDarkGift));
                Assert.IsTrue(service.State.ChoiceQueue.ActiveChoice.Options.All(option =>
                    option.DifficultyTier >= (round == 4 ? 2 : 3) &&
                    option.DifficultyTier <= 3));
                service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));
            }

            Assert.AreEqual(0, service.NormalDarkGiftUsesRemaining());
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.State.Player.Tavern.Gold = 10;
            Assert.IsFalse(service.CanUseNormalDarkGift());
            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseNormalDarkGift)));
        }

        private static MatchService CreateService(string selectedHeroCardId, bool enableTrinkets = false)
        {
            var baseline = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var minions = new List<MinionDefinition>(baseline.Catalogs.Minions.All);
            AddCandidates(minions, 2);
            AddCandidates(minions, 3);
            AddCandidates(minions, 5);
            AddCandidates(minions, 7);
            var heroes = new List<HeroDefinition>(baseline.Catalogs.Heroes.AllHeroes)
            {
                Hero(XaviusHeroCardId, XaviusPowerCardId, "Nightmare Lord Xavius", "Feel Devastation"),
                Hero(TrastathHeroCardId, TrastathPowerCardId, "Tras'tath, Soul Parasite", "Void Power")
            };
            var trinkets = new List<TrinketDefinition>(baseline.Catalogs.Trinkets.All)
            {
                new TrinketDefinition
                {
                    Id = WaxLanceCardId,
                    CardId = WaxLanceCardId,
                    Name = "蜡油长枪",
                    SlotKind = TrinketSlotKind.Greater,
                    Cost = 3,
                    Text = "Discover a Tier 7 minion with a Dark Gift.",
                    ImplementationStatus = TrinketImplementationStatus.Implemented,
                    OfferPoolStatus = TrinketOfferPoolStatus.Offerable,
                    TriggerTemplate = TrinketTriggerTemplate.OnEquip,
                    EffectTemplate = TrinketEffectTemplate.Discover,
                    EffectIds = new List<string> { "season14_wax_lance" }
                }
            };
            var catalogs = new GameCatalogSet(
                new MinionCatalog(minions),
                baseline.Catalogs.Spells,
                new HeroCatalog(heroes),
                new TrinketCatalog(trinkets),
                baseline.Catalogs.Quests,
                baseline.Catalogs.TimewarpedTavern,
                baseline.Catalogs.Anomalies,
                baseline.Catalogs.DarkmoonPrizes);
            var snapshot = new GameCatalogSnapshot(
                new ContentSnapshotInfo(
                    "season14-m6-tests",
                    string.Empty,
                    ContentSnapshotSource.Embedded,
                    string.Empty,
                    new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
                    "season14-m6-tests",
                    GameVersionIds.Season14Preview,
                    RulesetIds.Season14Preview,
                    string.Empty),
                catalogs,
                catalogs);
            var resolved = GameVersionResolver.CreateBuiltIn().Resolve(GameVersionIds.Season14Preview, snapshot);
            var gifts = Gifts();
            var resolvers = new DarkGiftResolverRegistry();
            foreach (var gift in gifts)
            {
                resolvers.Register(gift.EffectRevision, context => DarkGiftResolution.Success(
                    "test-gift-applied",
                    (state, target) => target.Tags.Add("test-dark-gift-applied")));
            }

            return MatchService.CreateWithResolvedVersion(
                resolved,
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    SelectedHeroCardId = selectedHeroCardId,
                    EnableQuests = false,
                    EnableTrinkets = enableTrinkets,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                },
                darkGiftDefinitions: gifts,
                darkGiftResolvers: resolvers);
        }

        private static HeroDefinition Hero(string heroCardId, string powerCardId, string name, string powerName)
        {
            return new HeroDefinition
            {
                HeroCardId = heroCardId,
                Name = name,
                Health = 30,
                HeroPower = new HeroPowerDefinition
                {
                    CardId = powerCardId,
                    Name = powerName,
                    Text = powerName,
                    PrimaryCategory = HeroPowerCategory.Discover,
                    ReplacementEligibility = HeroPowerReplacementEligibility.NonSelectable
                }
            };
        }

        private static void AddCandidates(List<MinionDefinition> definitions, int tier)
        {
            for (var index = 1; index <= 4; index += 1)
            {
                var cardId = "TEST_M6_T" + tier + "_" + index;
                definitions.Add(new MinionDefinition
                {
                    Id = cardId,
                    CardId = cardId,
                    RevisionId = cardId + "@36.2",
                    EffectRevision = cardId + "@36.2",
                    Name = cardId,
                    TavernTier = tier,
                    BaseAttack = tier,
                    BaseHealth = tier,
                    InPool = true,
                    Tribes = new List<Tribe> { Tribe.None }
                });
            }
        }

        private static List<DarkGiftDefinition> Gifts()
        {
            return Enumerable.Range(1, 4).Select(index => new DarkGiftDefinition
            {
                Id = "test-gift-" + index,
                RevisionId = "test-gift-" + index + "@36.2",
                EffectRevision = "test-gift-effect-" + index + "@36.2",
                DisplayName = "Test Gift " + index,
                Text = "Test gift effect.",
                EarliestOfferRound = 1,
                StackPolicy = DarkGiftStackPolicies.Reject,
                DurationPolicy = DarkGiftDurationPolicies.Persistent,
                ImplementationStatus = DarkGiftImplementationStatus.Implemented
            }).ToList();
        }

        private static bool HasDarkGiftChoice(MatchState state)
        {
            return state.ChoiceQueue.ActiveChoice?.Kind == ChoiceRequestKind.DarkGift ||
                   state.ChoiceQueue.PendingChoices.Any(item => item.Kind == ChoiceRequestKind.DarkGift);
        }
    }
}
