using System.Linq;
using System.Collections.Generic;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MatchServiceTests
    {
        private const string KraggHeroPowerId = "TB_BaconShop_HP_076";
        private const string PatchesHeroPowerId = "TB_BaconShop_HP_054";
        private const string GeorgeHeroPowerId = "TB_BaconShop_HP_010";
        private const string RakanishuHeroPowerId = "TB_BaconShop_HP_085";
        private const string BlackthornHeroPowerId = "BG20_HERO_103p";
        private const string LanternLightCardId = "RAKANISHU_LANTERN_LIGHT";
        private const string BloodGemCardId = "BLOOD_GEM";

        [Test]
        public void CreateNewMatch_StartsWithTierOneShopAndThreeGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            Assert.AreEqual(1, service.State.Round);
            Assert.AreEqual(1, service.State.Player.Tavern.Tier);
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.AreEqual(TavernRules.GetShopSize(1) + 1, service.State.Player.Tavern.Shop.Count);
            Assert.AreEqual(CardKind.TavernSpell, service.State.Player.Tavern.Shop.Last().CardKind);
            Assert.AreEqual(TavernRules.GetShopSize(1), service.State.Player.Tavern.Shop.Count(card => card.CardKind == CardKind.Minion));
            Assert.LessOrEqual(service.State.Player.Tavern.Shop.Last().TavernTier, service.State.Player.Tavern.Tier);
        }

        [Test]
        public void CreateNewMatch_DefaultsToAllPlayableTribes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            CollectionAssert.AreEqual(TribeAvailabilityRules.PlayableTribes, service.State.ActiveTribes);
        }

        [Test]
        public void TimewarpedTavernCatalog_LoadsExpectedCurrentAndHistoricalPools()
        {
            var catalog = TimewarpedTavernCatalogLoader.LoadFromResources();

            Assert.Greater(catalog.All.Count, 0);
            Assert.Greater(catalog.Current.Count, 0);
            Assert.Greater(catalog.Minor.Count, 0);
            Assert.Greater(catalog.Major.Count, 0);
            Assert.Greater(catalog.HistoricalExtra.Count, 0);
            Assert.Greater(catalog.NonMinions.Count, 0);
            Assert.AreEqual(0, catalog.BlockedNonMinions.Count);
            Assert.IsTrue(catalog.Current.All(card => !string.IsNullOrWhiteSpace(card.ImagePath)));
            Assert.IsTrue(catalog.NonMinions.All(card => ContainsChinese(card.ZhName)));
            Assert.IsTrue(catalog.NonMinions.All(card => ContainsChinese(card.ZhText)));

            var exit = catalog.GetByCardId("BG34_BlackMarket_Skip");
            Assert.AreEqual(CardKind.Spell, exit.CardKind);
            Assert.AreEqual(TimewarpedPurchaseBehavior.Exit, TimewarpedCardBehavior.ResolvePurchaseBehavior(exit));

            var nonMinion = catalog.GetByCardId("BG34_Treasure_934");
            Assert.AreEqual(CardKind.TavernSpell, nonMinion.CardKind);
            CollectionAssert.Contains(nonMinion.Tags, "timewarp:implemented");
            Assert.AreEqual(TimewarpedPurchaseBehavior.CastsWhenBought, TimewarpedCardBehavior.ResolvePurchaseBehavior(nonMinion));
            Assert.IsFalse(nonMinion.Tags.Contains(TimewarpedCardBehavior.BlockedNonMinionSupportTag));

            var discoverSpell = catalog.GetByCardId("BG34_Treasure_903");
            Assert.AreEqual(CardKind.TavernSpell, discoverSpell.CardKind);
            CollectionAssert.Contains(discoverSpell.Tags, "timewarp:implemented");
            Assert.AreEqual(TimewarpedPurchaseBehavior.EntersHand, TimewarpedCardBehavior.ResolvePurchaseBehavior(discoverSpell));
            Assert.IsFalse(discoverSpell.Tags.Contains(TimewarpedCardBehavior.BlockedNonMinionSupportTag));

            var repeatSpell = catalog.GetByCardId("BG34_Treasure_608");
            Assert.AreEqual(CardKind.TavernSpell, repeatSpell.CardKind);
            CollectionAssert.Contains(repeatSpell.Tags, "timewarp:implemented");
            Assert.AreEqual(TimewarpedPurchaseBehavior.EntersHand, TimewarpedCardBehavior.ResolvePurchaseBehavior(repeatSpell));
            Assert.IsFalse(repeatSpell.Tags.Contains(TimewarpedCardBehavior.BlockedNonMinionSupportTag));
        }

        [Test]
        public void TimewarpedTavern_DefaultCandidatesIncludeImplementedNonMinionsAndExcludeHistorical()
        {
            var service = CreateTimewarpOnlyService(12345);

            var minor = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor).ToList();
            var major = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Major).ToList();

            Assert.AreEqual(TimewarpedPoolVersion.Current, service.State.TimewarpedPoolVersion);
            Assert.IsFalse(service.State.UseHistoricalTimewarpedPool);
            Assert.Greater(minor.Count, 0);
            Assert.Greater(major.Count, 0);
            Assert.IsTrue(minor.Any(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(major.Any(card => card.CardKind == CardKind.Minion));
            Assert.IsTrue(minor.Any(card => card.PoolStatus == "implemented_non_minion"));
            Assert.IsTrue(major.Any(card => card.PoolStatus == "implemented_non_minion"));
            Assert.IsFalse(minor.Concat(major).Any(card => card.PoolStatus == "historical_extra"));
            Assert.IsFalse(minor.Concat(major).Any(card => card.TimewarpKind == TimewarpKind.None));
            Assert.IsTrue(minor.Concat(major).All(TimewarpedCardBehavior.IsOfferableNonExit));
        }

        [Test]
        public void TimewarpedTavernCatalog_PurchaseBehaviorFieldOverridesDefaultInference()
        {
            var catalog = TimewarpedTavernCatalogLoader.LoadFromJson(
                "{\"count\":1,\"cards\":[{\"cardId\":\"TEST_TIMEWARPED_TEMPLATE\",\"name\":\"Template Spell\",\"cardKind\":\"TavernSpell\",\"timewarpKind\":\"Minor\",\"cost\":1,\"techLevel\":3,\"attack\":0,\"health\":0,\"poolStatus\":\"current\",\"purchaseBehavior\":\"CastsWhenBought\"}]}");

            var card = catalog.GetByCardId("TEST_TIMEWARPED_TEMPLATE");

            Assert.AreEqual(TimewarpedPurchaseBehavior.CastsWhenBought, card.PurchaseBehavior);
            Assert.AreEqual(TimewarpedPurchaseBehavior.CastsWhenBought, TimewarpedCardBehavior.ResolvePurchaseBehavior(card));
            Assert.IsFalse(TimewarpedCardBehavior.EntersHand(card));
        }

        [Test]
        public void TimewarpedTavern_HistoricalSwitchAddsHistoricalExtraCandidates()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Timewarp,
                    EnableTrinkets = false,
                    UseHistoricalTimewarpedPool = true
                });

            var minor = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor).ToList();
            var major = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Major).ToList();
            var current = CreateTimewarpOnlyService(12345);
            var currentMinor = current.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor).Count;
            var currentMajor = current.GetTimewarpedCandidateDefinitions(TimewarpKind.Major).Count;

            Assert.AreEqual(TimewarpedPoolVersion.FirestoneAll, service.State.TimewarpedPoolVersion);
            Assert.IsTrue(service.State.UseHistoricalTimewarpedPool);
            Assert.Greater(minor.Count, currentMinor);
            Assert.Greater(major.Count, currentMajor);
            Assert.IsTrue(minor.Any(card => card.PoolStatus == "historical_extra"));
            Assert.IsTrue(major.Any(card => card.PoolStatus == "historical_extra"));
            Assert.IsTrue(minor.Any(card => card.PoolStatus == "implemented_non_minion"));
            Assert.IsTrue(major.Any(card => card.PoolStatus == "implemented_non_minion"));
            Assert.IsTrue(minor.Concat(major).All(TimewarpedCardBehavior.IsOfferableNonExit));
        }

        [Test]
        public void TimewarpedTavern_ExplicitHistoricalPoolVersionIsVisibleInStateAndCandidates()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Timewarp,
                    EnableTrinkets = false,
                    TimewarpedPoolVersion = TimewarpedPoolVersion.Launch
                });

            var candidates = service
                .GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .Concat(service.GetTimewarpedCandidateDefinitions(TimewarpKind.Major))
                .ToList();

            Assert.AreEqual(TimewarpedPoolVersion.Launch, service.State.TimewarpedPoolVersion);
            Assert.IsTrue(service.State.UseHistoricalTimewarpedPool);
            Assert.IsTrue(candidates.Any(card => card.PoolStatus == "historical_extra"));
            var current = CreateTimewarpOnlyService(12345);
            Assert.IsFalse(current
                .GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .Concat(current.GetTimewarpedCandidateDefinitions(TimewarpKind.Major))
                .Any(card => card.PoolStatus == "historical_extra"));
        }

        [Test]
        public void TimewarpedTavern_ExitOfferClosesVisitWithoutAddingHandCard()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            timewarp.Chronum = 3;
            timewarp.Offers = new List<TimewarpedOfferSlot>
            {
                new TimewarpedOfferSlot { SlotId = "fixed-exit", CardId = "BG34_BlackMarket_Skip", CardKind = CardKind.Spell, Cost = 1, Source = "test" }
            };
            var handBefore = tavern.Hand.Count;

            service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0));

            Assert.AreEqual(2, timewarp.Chronum);
            Assert.IsTrue(timewarp.Offers[0].Purchased);
            Assert.IsFalse(timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Closed, timewarp.Phase);
            Assert.AreEqual(handBefore, tavern.Hand.Count);
        }

        [Test]
        public void TimewarpedTavern_NonMinionsHaveNoBlockedSupportStubs()
        {
            var catalog = TimewarpedTavernCatalogLoader.LoadFromResources();

            Assert.AreEqual(0, catalog.BlockedNonMinions.Count);
            Assert.IsFalse(catalog.NonMinions.Any(TimewarpedCardBehavior.IsBlockedNonMinionSupport));
        }

        [Test]
        public void TimewarpedCastsWhenBought_ArmorStashGainsArmorWithoutAddingHandCard()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            service.State.Player.Armor = 4;
            var handBefore = tavern.Hand.Count;
            var castsBefore = tavern.TavernSpellsCastThisTurn;

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_934", CardKind.TavernSpell, 1, 3);

            Assert.AreEqual(14, service.State.Player.Armor);
            Assert.AreEqual(2, timewarp.Chronum);
            Assert.IsTrue(timewarp.Offers[0].Purchased);
            Assert.AreEqual(handBefore, tavern.Hand.Count);
            Assert.AreEqual(castsBefore + 1, tavern.TavernSpellsCastThisTurn);
        }

        [Test]
        public void TimewarpedCastsWhenBought_InvestmentAddsAndConsumesNextTimewarpBonusChronum()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_300", CardKind.TavernSpell, 1, 3);

            Assert.AreEqual(2, timewarp.Chronum);
            Assert.AreEqual(1, timewarp.NextTimewarpBonusChronum);
            Assert.IsTrue(timewarp.Offers[0].Purchased);

            timewarp.VisitOpen = false;
            timewarp.Phase = TimewarpTavernPhase.Closed;
            timewarp.PendingKind = TimewarpKind.None;
            timewarp.PendingSource = null;
            timewarp.LastVisitRound = 0;

            AdvanceToRound(service, 6);

            Assert.AreEqual(TimewarpTavernPhase.Open, timewarp.Phase);
            Assert.AreEqual(6, timewarp.Chronum);
            Assert.AreEqual(0, timewarp.NextTimewarpBonusChronum);
        }

        [Test]
        public void TimewarpedCastsWhenBought_HeroPowerSpellAddsSecondHeroPower()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            var handBefore = tavern.Hand.Count;
            service.State.Player.HeroPowerCardId = "TB_BaconShop_HP_054";
            service.State.Player.ExtraHeroPowerCardIds.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_HeroPowerSpell_006", CardKind.TavernSpell, 2, 3);

            Assert.AreEqual(1, timewarp.Chronum);
            Assert.IsTrue(timewarp.Offers[0].Purchased);
            Assert.AreEqual(handBefore, tavern.Hand.Count);
            Assert.AreEqual("TB_BaconShop_HP_054", service.State.Player.HeroPowerCardId);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, "TB_BaconShop_HP_010");
        }

        [Test]
        public void TimewarpedChooseDiscover_OnTheHouseDiscoversThreeCurrentTierMinions()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            tavern.Tier = 4;

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_903", CardKind.TavernSpell, 2, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual("timewarped-on-the-house", tavern.Discover.Source);
            Assert.AreEqual(3, tavern.Discover.RemainingPicks);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.CardKind == CardKind.Minion && card.TavernTier == 4));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.AreEqual(2, tavern.Discover.RemainingPicks);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.AreEqual(2, tavern.Hand.Count);
            Assert.AreEqual(1, tavern.Discover.RemainingPicks);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(3, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand.All(card => card.CardKind == CardKind.Minion && card.TavernTier == 4));
        }

        [Test]
        public void TimewarpedChooseDiscover_CorpseDiscoversDeathrattleWithReborn()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_937", CardKind.TavernSpell, 1, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual("timewarped-corpse", tavern.Discover.Source);
            Assert.IsTrue(tavern.Discover.Options.Count > 0);
            Assert.IsTrue(tavern.Discover.Options.All(card =>
                card.CardKind == CardKind.Minion &&
                card.TavernTier >= 5 &&
                card.Keywords.Contains(Keyword.Deathrattle) &&
                card.Keywords.Contains(Keyword.Reborn)));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.Contains(Keyword.Reborn, tavern.Hand[0].Keywords);
        }

        [Test]
        public void TimewarpedChooseDiscover_ChefsChoiceAddsSameTypeTierFourFiveSixMinions()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(TestBoardMinion("chef-target", "Chef Target", "CHEF_TARGET", 2, 2, Tribe.Beast, 3));

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_940", CardKind.TavernSpell, 1, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(3, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand.All(card =>
                card.CardKind == CardKind.Minion &&
                card.TavernTier >= 4 &&
                card.TavernTier <= 6 &&
                (card.Tribes.Contains(Tribe.Beast) || card.Tribes.Contains(Tribe.All))));
        }

        [Test]
        public void TimewarpedChooseDiscover_RevelationDiscoversCostOneAndTwoMinorTimewarpedMinions()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_953", CardKind.TavernSpell, 1, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual("timewarped-revelation:1", tavern.Discover.Source);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.CardKind == CardKind.Minion && card.Cost == 1 && card.Tags.Contains("timewarped")));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.AreEqual(1, tavern.Hand[0].Cost);
            Assert.AreEqual("timewarped-revelation:2", tavern.Discover.Source);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.CardKind == CardKind.Minion && card.Cost == 2 && card.Tags.Contains("timewarped")));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(2, tavern.Hand.Count);
            Assert.AreEqual(2, tavern.Hand[1].Cost);
            Assert.IsTrue(tavern.Hand.All(card => card.Tags.Contains("timewarped")));
        }

        [Test]
        public void TimewarpedChooseDiscover_RitualDiscoversTwoTierSevenMinions()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_919", CardKind.TavernSpell, 2, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual("timewarped-ritual", tavern.Discover.Source);
            Assert.AreEqual(2, tavern.Discover.RemainingPicks);
            var rewardTier = tavern.Discover.RewardTier;
            Assert.GreaterOrEqual(rewardTier, 6);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.CardKind == CardKind.Minion && card.TavernTier == rewardTier));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.AreEqual(1, tavern.Discover.RemainingPicks);
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(2, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand.All(card => card.CardKind == CardKind.Minion && card.TavernTier == rewardTier));
        }

        [Test]
        public void TimewarpedChooseDiscover_EvolutionTransformsTargetIntoThirtyThirtyTierSix()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(TestBoardMinion("evolution-target", "Evolution Target", "EVOLUTION_TARGET", 2, 3, Tribe.Beast, 2));

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_933", CardKind.TavernSpell, 1, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.IsNotNull(tavern.Discover);
            StringAssert.StartsWith("timewarped-evolution:", tavern.Discover.Source);
            Assert.AreEqual("evolution-target", tavern.Discover.TargetInstanceId);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.CardKind == CardKind.Minion && card.TavernTier == 6));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Board.Count);
            var transformed = service.State.Player.Board[0];
            Assert.AreEqual("evolution-target", transformed.InstanceId);
            Assert.AreNotEqual("EVOLUTION_TARGET", transformed.CardId);
            Assert.AreEqual(6, transformed.TavernTier);
            Assert.AreEqual(30, transformed.Attack);
            Assert.AreEqual(30, transformed.Health);
            Assert.AreEqual(30, transformed.MaxHealth);
        }

        [Test]
        public void TimewarpedChooseDiscover_BeanstalkDiscoversMajorCostOneAndLocksItInHand()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_301", CardKind.TavernSpell, 2, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual("timewarped-beanstalk", tavern.Discover.Source);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.Cost == 1 && card.Tags.Contains("timewarped")));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.AreEqual(1, tavern.Hand[0].Cost);
            Assert.IsTrue(tavern.Hand[0].Tags.Contains("locked_in_hand"));
            Assert.AreEqual(1, tavern.Hand[0].Counters["locked-turns"]);
        }

        [Test]
        public void TimewarpedChooseDiscover_SecrecyDiscoversGoldenTierSevenMinion()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_625", CardKind.TavernSpell, 3, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual("timewarped-secrecy", tavern.Discover.Source);
            var rewardTier = tavern.Discover.RewardTier;
            Assert.GreaterOrEqual(rewardTier, 6);
            Assert.IsTrue(tavern.Discover.Options.All(card => card.CardKind == CardKind.Minion && card.TavernTier == rewardTier && card.Golden));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.AreEqual(rewardTier, tavern.Hand[0].TavernTier);
            Assert.IsTrue(tavern.Hand[0].Golden);
        }

        [Test]
        public void TimewarpedStateBackedDiscover_BigWinnerDiscoversTierThreeDarkmoonPrizeAndRepeatsEveryThreeTurns()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_606", CardKind.TavernSpell, 2, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual("timewarped-big-winner", tavern.Discover.Source);
            Assert.AreEqual(3, tavern.Discover.RewardTier);
            var tierThreePrizeIds = new HashSet<string>(service.DarkmoonPrizeCatalog.GetByTier(3).Select(prize => prize.CardId));
            Assert.AreEqual(8, tierThreePrizeIds.Count);
            Assert.IsTrue(tavern.Discover.Options.All(card =>
                card.CardKind == CardKind.Spell &&
                card.TavernTier == 3 &&
                tierThreePrizeIds.Contains(card.CardId) &&
                card.Tags.Contains("darkmoon_prize") &&
                card.Tags.Contains("darkmoon_prize_tier_3") &&
                !card.Tags.Contains("bounty") &&
                !card.Tags.Contains("darkmoon_prize_proxy")));
            Assert.AreEqual(4, tavern.AdvancedMechanics.Counters["timewarped_big_winner_due_round"]);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand[0].Tags.Contains("darkmoon_prize"));
            Assert.IsFalse(tavern.Hand[0].Tags.Contains("darkmoon_prize_proxy"));
            Assert.IsFalse(tavern.Hand[0].Tags.Contains("bounty"));
            tavern.Hand.Clear();

            AdvanceToRound(service, 3);
            Assert.IsNull(tavern.Discover);

            AdvanceToRound(service, 4);

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual("timewarped-big-winner", tavern.Discover.Source);
            Assert.IsTrue(tavern.Discover.Options.All(card =>
                card.Tags.Contains("darkmoon_prize") &&
                card.Tags.Contains("darkmoon_prize_tier_3") &&
                !card.Tags.Contains("darkmoon_prize_proxy")));
            Assert.AreEqual(7, tavern.AdvancedMechanics.Counters["timewarped_big_winner_due_round"]);
        }

        [Test]
        public void DarkmoonPrize_BuyTheHolyLightIsPlayableSpellWithoutTavernSpellCounters()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            var target = TestBoardMinion("holy-light-target", "Holy Light Target", "HOLY_LIGHT_TARGET", 3, 4, Tribe.Beast, 2);
            service.State.Player.Board.Add(target);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_015", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(13, target.Attack);
            Assert.IsTrue(target.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(0, tavern.TavernSpellsCastThisGame);
        }

        [Test]
        public void DarkmoonPrize_BananasFillHandWithTavernDishBananas()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_019", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(10, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand.All(card =>
                card.CardId == "MUKLA_BANANA" &&
                card.CardKind == CardKind.TavernSpell &&
                card.Tags.Contains("tavern_dish_banana")));
        }

        [Test]
        public void DarkmoonPrize_ReservePricesDiscountsTavernSpellsWithoutCountingAsTavernSpell()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_104", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, tavern.NextTavernSpellCostReduction);
            Assert.AreEqual(0, tavern.TavernSpellsCastThisGame);
        }

        [Test]
        public void DarkmoonPrize_TrainingSessionQueuesHeroPowerDiscover()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_011", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual("hero-power:unmasked-identity", tavern.Discover.Source);
            Assert.AreEqual(3, tavern.Discover.Options.Count);
            Assert.IsTrue(tavern.Discover.Options.All(option => option.CardKind == CardKind.HeroPower));
            Assert.AreEqual(0, tavern.TavernSpellsCastThisGame);
        }

        [Test]
        public void DarkmoonPrize_TopShelfDiscoversHigherTierMinion()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 4;
            tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_020", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual("darkmoon-top-shelf", tavern.Discover.Source);
            Assert.AreEqual(5, tavern.Discover.RewardTier);
            Assert.IsTrue(tavern.Discover.Options.All(option => option.CardKind == CardKind.Minion && option.TavernTier == 5));
        }

        [Test]
        public void DarkmoonPrize_RepeatCustomerReturnsNonGoldenFriendlyMinionWithBuff()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            var target = TestBoardMinion("repeat-target", "Repeat Target", "REPEAT_TARGET", 3, 4, Tribe.Beast, 2);
            service.State.Player.Board.Add(target);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_034", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(0, service.State.Player.Board.Count);
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.AreEqual("REPEAT_TARGET", tavern.Hand[0].CardId);
            Assert.AreEqual(9, tavern.Hand[0].Attack);
            Assert.AreEqual(10, tavern.Hand[0].MaxHealth);
        }

        [Test]
        public void DarkmoonPrize_AllThatGlittersMakesRandomTavernMinionGolden()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            TavernShopSlots.ReplaceShop(
                tavern,
                new List<MinionInstance>
                {
                    TestBoardMinion("glitters-a", "Glitters A", "GLITTERS_A", 2, 2, Tribe.Mech, 2),
                    TestBoardMinion("glitters-b", "Glitters B", "GLITTERS_B", 3, 3, Tribe.Beast, 3)
                });

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_037", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsTrue(tavern.Shop.Where(card => card != null).Any(card => card.Golden));
        }

        [Test]
        public void DarkmoonPrize_MindflayerGogglesStealsShopAndRefreshes()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            TavernShopSlots.ReplaceShop(
                tavern,
                new List<MinionInstance>
                {
                    TestBoardMinion("mindflayer-a", "Mindflayer A", "MINDFLAYER_A", 2, 2, Tribe.Mech, 2),
                    TestBoardMinion("mindflayer-b", "Mindflayer B", "MINDFLAYER_B", 3, 3, Tribe.Beast, 3)
                });

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_039", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardId == "MINDFLAYER_A" || card.CardId == "MINDFLAYER_B"));
            Assert.IsTrue(tavern.Shop.Any(card => card != null));
            Assert.IsFalse(tavern.Shop.Any(card => card != null && (card.CardId == "MINDFLAYER_A" || card.CardId == "MINDFLAYER_B")));
        }

        [Test]
        public void TimewarpedStateBackedDiscover_MasterThiefOffersFixedGoldenLegacyMinions()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_902", CardKind.TavernSpell, 3, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual("timewarped-master-thief", tavern.Discover.Source);
            Assert.AreEqual(3, tavern.Discover.Options.Count);
            CollectionAssert.AreEquivalent(
                new[] { "BG_LOE_077", "BG25_354", "BG26_ICC_901" },
                tavern.Discover.Options.Select(option => option.CardId).ToArray());
            Assert.IsTrue(tavern.Discover.Options.All(option => option.Golden));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand[0].Golden);
        }

        [Test]
        public void TimewarpedStateBackedDiscover_ThiefOffersFixedGoldenLegacyMinions()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_966", CardKind.TavernSpell, 1, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual("timewarped-thief", tavern.Discover.Source);
            Assert.AreEqual(3, tavern.Discover.Options.Count);
            CollectionAssert.AreEquivalent(
                new[] { "BG_LOE_077", "BG25_354", "BG26_ICC_901" },
                tavern.Discover.Options.Select(option => option.CardId).ToArray());
            Assert.IsTrue(tavern.Discover.Options.All(option => option.Golden));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand[0].Golden);
        }

        [Test]
        public void TimewarpedDirectNonMinion_TraineeAddsOneMinionFromTiersOneTwoThree()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_607", CardKind.TavernSpell, 1, 3);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(3, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand.All(card => card.CardKind == CardKind.Minion));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, tavern.Hand.Select(card => card.TavernTier).ToArray());
        }

        [Test]
        public void TimewarpedDirectNonMinion_StrikeOilIncreasesMaxGoldAndGainsFourGold()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            tavern.Gold = 2;
            tavern.MaxGold = 6;

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_932", CardKind.TavernSpell, 2, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(6, tavern.Gold);
            Assert.AreEqual(10, tavern.MaxGold);

            AdvanceToRound(service, 2);

            Assert.AreEqual(TavernRules.GetMaxGoldForRound(2) + 4, tavern.MaxGold);
            Assert.AreEqual(TavernRules.GetMaxGoldForRound(2) + 4, tavern.Gold);
        }

        [Test]
        public void TimewarpedDirectNonMinion_RatInACageBuffsThenDoublesFriendlyTarget()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("rat-target", "Rat Target", "RAT_TARGET", 3, 4, Tribe.Beast, 2));

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_905", CardKind.TavernSpell, 1, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            var target = service.State.Player.Board[0];
            Assert.AreEqual(10, target.Attack);
            Assert.AreEqual(12, target.Health);
            Assert.AreEqual(12, target.MaxHealth);
        }

        [Test]
        public void TimewarpedDirectNonMinion_GoldenizerMakesFriendlyTargetGolden()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("goldenizer-target", "Goldenizer Target", "GOLDENIZER_TARGET", 4, 5, Tribe.Mech, 3));

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_955", CardKind.TavernSpell, 2, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            var target = service.State.Player.Board[0];
            Assert.IsTrue(target.Golden);
            Assert.AreEqual(8, target.Attack);
            Assert.AreEqual(10, target.Health);
            Assert.AreEqual(10, target.MaxHealth);
            Assert.AreEqual(1, target.Counters["triple-reward-granted"]);
        }

        [Test]
        public void TimewarpedFinalNonMinion_BananasFillHandAndImproveTavernSpellBuffs()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            var target = TestBoardMinion("banana-target", "Banana Target", "BANANA_TARGET", 2, 2, Tribe.Beast, 1);
            service.State.Player.Board.Add(target);

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_912", CardKind.TavernSpell, 2, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(10, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand.All(card =>
                card.CardId == "MUKLA_BANANA" &&
                card.CardKind == CardKind.TavernSpell &&
                card.Tags.Contains("tavern_dish_banana")));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(6, target.Health);
            Assert.AreEqual(6, target.MaxHealth);
        }

        [Test]
        public void TimewarpedFinalNonMinion_NewRecruitBuffsCurrentAndFutureSevenCardTaverns()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            tavern.Gold = 10;
            tavern.MaxGold = 10;
            TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance>
            {
                TestBoardMinion("new-recruit-shop-1", "Shop One", "SHOP_ONE", 2, 3, Tribe.Beast, 1),
                TestBoardMinion("new-recruit-shop-2", "Shop Two", "SHOP_TWO", 3, 4, Tribe.Murloc, 1)
            });

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_917", CardKind.TavernSpell, 1, 3);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            var currentShopMinions = tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            Assert.GreaterOrEqual(tavern.Shop.Count(card => card != null), 7);
            Assert.IsTrue(currentShopMinions.All(card => card.Attack >= card.BaseAttack + 2));
            Assert.IsTrue(currentShopMinions.All(card => card.MaxHealth >= card.BaseHealth + 2));

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var refreshedShopMinions = tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            Assert.GreaterOrEqual(tavern.Shop.Count(card => card != null), 7);
            Assert.IsTrue(refreshedShopMinions.All(card => card.Attack >= card.BaseAttack + 2));
            Assert.IsTrue(refreshedShopMinions.All(card => card.MaxHealth >= card.BaseHealth + 2));
        }

        [Test]
        public void TimewarpedFinalNonMinion_CloningDeviceSummonsExactFriendlyCopy()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            var target = TestBoardMinion("clone-target", "Clone Target", "CLONE_TARGET", 3, 4, Tribe.Dragon, 2);
            target.Attack = 7;
            target.Health = 8;
            target.MaxHealth = 9;
            target.Golden = true;
            target.Keywords.Add(Keyword.DivineShield);
            target.Counters["exact-copy-counter"] = 2;
            target.Tags.Add("exact-copy-tag");
            target.Enchantments.Add(new Enchantment { Id = "test-enchant", SourceId = "test", AttackBonus = 4, HealthBonus = 5 });
            service.State.Player.Board.Add(target);

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_302", CardKind.TavernSpell, 2, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(2, service.State.Player.Board.Count);
            var copy = service.State.Player.Board[1];
            Assert.AreNotEqual(target.InstanceId, copy.InstanceId);
            Assert.AreEqual(target.CardId, copy.CardId);
            Assert.AreEqual(7, copy.Attack);
            Assert.AreEqual(8, copy.Health);
            Assert.AreEqual(9, copy.MaxHealth);
            Assert.IsTrue(copy.Golden);
            Assert.Contains(Keyword.DivineShield, copy.Keywords);
            Assert.AreEqual(2, copy.Counters["exact-copy-counter"]);
            Assert.Contains("exact-copy-tag", copy.Tags);
            Assert.AreEqual("test", copy.Enchantments[0].SourceId);
            Assert.AreEqual(PoolSource.Summon, copy.PoolSource);
        }

        [Test]
        public void TimewarpedFinalNonMinion_SpecialFillsHandWithTemporarySpellcraft()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_950", CardKind.TavernSpell, 1, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(10, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand.All(card => card.CardKind == CardKind.Spell));
            Assert.IsTrue(tavern.Hand.All(card => card.Keywords.Contains(Keyword.Spellcraft)));
            Assert.IsTrue(tavern.Hand.All(card => card.Tags.Contains("temporary_spellcraft_card")));
        }

        [Test]
        public void TimewarpedFinalNonMinion_ConchCopiesFriendlyAllTribeAsMurloc()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            var target = TestBoardMinion("conch-target", "Conch Target", "CONCH_TARGET", 5, 6, Tribe.All, 3);
            target.Attack = 11;
            target.MaxHealth = 12;
            target.Health = 10;
            service.State.Player.Board.Add(target);

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_951", CardKind.TavernSpell, 1, 5);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(2, service.State.Player.Board.Count);
            var copy = service.State.Player.Board[1];
            Assert.AreNotEqual(target.InstanceId, copy.InstanceId);
            Assert.AreEqual(target.CardId, copy.CardId);
            Assert.AreEqual(11, copy.Attack);
            Assert.AreEqual(10, copy.Health);
            Assert.AreEqual(12, copy.MaxHealth);
            Assert.AreEqual(PoolSource.Summon, copy.PoolSource);
        }

        [Test]
        public void TimewarpedRepeatNonMinion_EvolvingTavernAddsOfficialPrizeAndRepeatsAtTurnStart()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_900", CardKind.TavernSpell, 1, 3);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == "BGS_Treasures_006"));
            Assert.IsTrue(tavern.Hand.Single(card => card.CardId == "BGS_Treasures_006").Tags.Contains("implemented_darkmoon_prize"));
            Assert.IsFalse(tavern.Hand.Any(card => card.CardId == "TIMEWARPED_EVOLVING_TAVERN_SPELL"));

            AdvanceToRound(service, 2);

            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardId == "BGS_Treasures_006"));
            Assert.IsFalse(tavern.Hand.Any(card => card.CardId == "TIMEWARPED_EVOLVING_TAVERN_SPELL"));

            TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance>
            {
                TestBoardMinion("evolving-shop", "Evolving Shop", "EVOLVING_SHOP", 1, 1, Tribe.Beast, 1)
            });
            var prizeIndex = tavern.Hand.FindIndex(card => card.CardId == "BGS_Treasures_006");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, prizeIndex));

            Assert.AreEqual(1, tavern.Shop.Count);
            Assert.AreEqual(2, tavern.Shop[0].TavernTier);
            Assert.AreNotEqual("EVOLVING_SHOP", tavern.Shop[0].CardId);
        }

        [Test]
        public void TimewarpedRepeatNonMinion_RingAddsShinyRingAndRepeatsAtTurnStart()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_608", CardKind.TavernSpell, 1, 3);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == "109230"));

            AdvanceToRound(service, 2);

            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardId == "109230"));
        }

        [Test]
        public void TimewarpedRepeatNonMinion_LassoAddsEnchantedLassoAndCanStealShopMinion()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_609", CardKind.TavernSpell, 1, 3);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == "104502"));

            TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance>
            {
                TestBoardMinion("lasso-shop", "Lasso Shop", "LASSO_SHOP", 3, 4, Tribe.Murloc, 1)
            });
            var lassoIndex = tavern.Hand.FindIndex(card => card.CardId == "104502");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, lassoIndex));

            Assert.IsNull(tavern.Shop[0]);
            Assert.IsTrue(tavern.Hand.Any(card => card.InstanceId == "lasso-shop"));

            AdvanceToRound(service, 2);

            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == "104502"));
        }

        [Test]
        public void TimewarpedRefreshNonMinion_ApplesCastsThemApplesAfterManualRefresh()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            tavern.Gold = 10;
            tavern.MaxGold = 10;

            BuyFixedTimewarpedOffer(service, "BG34_Treasure_620", CardKind.TavernSpell, 1, 3);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var castsBefore = tavern.TavernSpellsCastThisTurn;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var shopMinions = tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            Assert.IsTrue(shopMinions.Count > 0);
            Assert.IsTrue(shopMinions.All(card => card.Attack >= card.BaseAttack + 1));
            Assert.IsTrue(shopMinions.All(card => card.MaxHealth >= card.BaseHealth + 2));
            Assert.AreEqual(castsBefore + 1, tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual("105903", tavern.LastTavernSpellCardId);
        }

        [Test]
        public void TimewarpedTavern_DisabledDoesNotScheduleVisit()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Timewarp,
                    EnableTimewarpedTavern = false,
                    EnableTrinkets = false
                });

            for (var turn = 2; turn <= 6; turn += 1)
            {
                service.Apply(new GameCommand(GameCommandType.NextTurn));
            }

            Assert.IsFalse(service.State.TimewarpedTavernEnabled);
            Assert.IsEmpty(service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor));
            Assert.IsEmpty(service.GetTimewarpedCandidateDefinitions(TimewarpKind.Major));
            Assert.IsFalse(service.State.Player.Tavern.Timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Idle, service.State.Player.Tavern.Timewarp.Phase);
        }

        [Test]
        public void TimewarpedTavern_ExplicitPoolControlsCandidateCards()
        {
            var defaultService = CreateTimewarpOnlyService(12345);
            var included = defaultService.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor).First();
            var excluded = defaultService.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .First(card => card.CardId != included.CardId);
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Timewarp,
                    EnableTrinkets = false,
                    UseExplicitTimewarpedPool = true,
                    EnabledTimewarpedCardIds = new List<string> { included.CardId }
                });

            var candidates = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor);
            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual(included.CardId, candidates[0].CardId);
            Assert.IsFalse(candidates.Any(card => card.CardId == excluded.CardId));
            CollectionAssert.AreEqual(new[] { included.CardId }, service.State.EnabledTimewarpedCardIds);
        }

        [Test]
        public void TimewarpedTavern_RoundSixOpensMinorAndPurchasesWithChronum()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Timewarp,
                    EnableTrinkets = false
                });

            for (var turn = 2; turn <= 6; turn += 1)
            {
                service.Apply(new GameCommand(GameCommandType.NextTurn));
            }

            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            var shopSnapshot = TavernShopStateFingerprint(tavern);
            var goldBefore = tavern.Gold;
            var handBefore = tavern.Hand.Count;
            var chronumBefore = timewarp.Chronum;
            var definitionsById = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .ToDictionary(card => card.CardId);
            var offerIndex = Enumerable.Range(0, timewarp.Offers.Count)
                .First(index =>
                {
                    var candidate = timewarp.Offers[index];
                    return candidate != null &&
                        definitionsById.TryGetValue(candidate.CardId, out var definition) &&
                        TimewarpedCardBehavior.EntersHand(definition);
                });
            var offer = timewarp.Offers[offerIndex];

            Assert.AreEqual(6, service.State.Round);
            Assert.AreEqual(TimewarpTavernPhase.Open, timewarp.Phase);
            Assert.AreEqual(TimewarpKind.Minor, timewarp.PendingKind);
            Assert.AreEqual(4, timewarp.Offers.Count);
            Assert.GreaterOrEqual(timewarp.Chronum, offer.Cost);

            service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, offerIndex));

            Assert.AreEqual(goldBefore, tavern.Gold);
            Assert.AreEqual(shopSnapshot, TavernShopStateFingerprint(tavern));
            Assert.AreEqual(handBefore + 1, tavern.Hand.Count);
            Assert.AreEqual(chronumBefore - offer.Cost, timewarp.Chronum);
            Assert.IsTrue(timewarp.Offers[offerIndex].Purchased);
            Assert.AreEqual(PoolSource.Timewarped, tavern.Hand.Last().PoolSource);

            service.Apply(new GameCommand(GameCommandType.ExitTimewarpedTavern));

            Assert.IsFalse(timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Closed, timewarp.Phase);
            Assert.AreEqual(chronumBefore - offer.Cost, timewarp.Chronum);
            Assert.AreEqual(shopSnapshot, TavernShopStateFingerprint(tavern));
        }

        [Test]
        public void WhiteBox_TimewarpedTavern_OpenVisitBlocksNextTurnWithoutStateChanges()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            var roundBefore = service.State.Round;
            var goldBefore = tavern.Gold;
            var chronumBefore = timewarp.Chronum;
            var shopBefore = tavern.Shop.Select(card => card?.InstanceId).ToList();
            var offersBefore = timewarp.Offers.Select(offer => offer?.CardId).ToList();

            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.NextTurn)));

            Assert.AreEqual("请先退出当前时空酒馆。", exception.Message);
            Assert.AreEqual(roundBefore, service.State.Round);
            Assert.AreEqual(goldBefore, tavern.Gold);
            Assert.AreEqual(chronumBefore, timewarp.Chronum);
            Assert.IsTrue(timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Open, timewarp.Phase);
            CollectionAssert.AreEqual(shopBefore, tavern.Shop.Select(card => card?.InstanceId).ToList());
            CollectionAssert.AreEqual(offersBefore, timewarp.Offers.Select(offer => offer?.CardId).ToList());
        }

        [Test]
        public void WhiteBox_TimewarpedPurchase_RepeatedCommandDeductsAndAddsOnlyOnce()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            var definition = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .First(card => card.CardKind == CardKind.Minion);
            timewarp.Chronum = 10;
            timewarp.Offers = new List<TimewarpedOfferSlot>
            {
                new TimewarpedOfferSlot
                {
                    SlotId = "repeat-purchase",
                    CardId = definition.CardId,
                    CardKind = definition.CardKind,
                    Cost = 1,
                    Source = "test"
                }
            };

            service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0));
            var handAfterFirst = tavern.Hand.Count;
            var chronumAfterFirst = timewarp.Chronum;
            var stateAfterFirst = TimewarpedFailureStateFingerprint(service);

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0)));

            Assert.AreEqual(stateAfterFirst, TimewarpedFailureStateFingerprint(service));
            Assert.AreEqual(9, chronumAfterFirst);
            Assert.AreEqual(chronumAfterFirst, timewarp.Chronum);
            Assert.AreEqual(handAfterFirst, tavern.Hand.Count);
            Assert.AreEqual(1, tavern.Hand.Count(card => card.CardId == definition.CardId));
            Assert.IsTrue(timewarp.Offers[0].Purchased);
        }

        [Test]
        public void TimewarpedTavern_RoundNineOpensMajorAndCarriesChronum()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var timewarp = service.State.Player.Tavern.Timewarp;
            var minorChronum = timewarp.Chronum;
            service.Apply(new GameCommand(GameCommandType.ExitTimewarpedTavern));

            AdvanceToRound(service, 9);

            Assert.AreEqual(9, service.State.Round);
            Assert.AreEqual(TimewarpTavernPhase.Open, timewarp.Phase);
            Assert.AreEqual(TimewarpKind.Major, timewarp.PendingKind);
            Assert.AreEqual(minorChronum + 3, timewarp.Chronum);
            Assert.AreEqual(4, timewarp.Offers.Count);
            var majorCandidateIds = new HashSet<string>(service.GetTimewarpedCandidateDefinitions(TimewarpKind.Major).Select(card => card.CardId));
            Assert.IsTrue(timewarp.Offers.All(offer => majorCandidateIds.Contains(offer.CardId)));
        }

        [Test]
        public void TimewarpedTavern_MorchieOpensMinorOnTurnFiveWithoutTimewarpMode()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    SelectedHeroCardId = "BG34_HERO_004",
                    EnableTrinkets = false
                });

            AdvanceToRound(service, 4);

            var timewarp = service.State.Player.Tavern.Timewarp;
            Assert.IsFalse(timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Idle, timewarp.Phase);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(5, service.State.Round);
            Assert.AreEqual("BG34_HERO_004p", service.State.Player.HeroPowerCardId);
            Assert.IsTrue(timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Open, timewarp.Phase);
            Assert.AreEqual(TimewarpKind.Minor, timewarp.PendingKind);
            Assert.AreEqual("morchie-minor-timewarp", timewarp.PendingSource);
            Assert.AreEqual(4, timewarp.Offers.Count);
            var minorCandidateIds = new HashSet<string>(service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor).Select(card => card.CardId));
            Assert.IsTrue(timewarp.Offers.All(offer => minorCandidateIds.Contains(offer.CardId)));
        }

        [Test]
        public void TimewarpedTavern_MurozondOpensMajorOnTurnEightWithoutTimewarpMode()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions
                {
                    SelectedHeroCardId = "BG34_HERO_000",
                    EnableTrinkets = false
                });

            AdvanceToRound(service, 7);

            var timewarp = service.State.Player.Tavern.Timewarp;
            Assert.IsFalse(timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Idle, timewarp.Phase);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(8, service.State.Round);
            Assert.AreEqual("BG34_HERO_000p", service.State.Player.HeroPowerCardId);
            Assert.IsTrue(timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Open, timewarp.Phase);
            Assert.AreEqual(TimewarpKind.Major, timewarp.PendingKind);
            Assert.AreEqual("murozond-major-timewarp", timewarp.PendingSource);
            Assert.AreEqual(3, timewarp.Chronum);
            Assert.AreEqual(4, timewarp.Offers.Count);
            var majorCandidateIds = new HashSet<string>(service.GetTimewarpedCandidateDefinitions(TimewarpKind.Major).Select(card => card.CardId));
            Assert.IsTrue(timewarp.Offers.All(offer => majorCandidateIds.Contains(offer.CardId)));
        }

        [Test]
        public void TimewarpedTavern_HandFullPurchaseDoesNotSpendChronumOrMarkOffer()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            while (tavern.Hand.Count < 10)
            {
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG20_100", CardKind.Minion));
            }

            var chronumBefore = timewarp.Chronum;
            var fixedMinion = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .First(card => card.CardKind == CardKind.Minion);
            timewarp.Offers = new List<TimewarpedOfferSlot>
            {
                new TimewarpedOfferSlot
                {
                    SlotId = "fixed-hand-full-minion",
                    CardId = fixedMinion.CardId,
                    CardKind = CardKind.Minion,
                    Cost = 1,
                    Source = "test"
                }
            };
            var offer = timewarp.Offers[0];
            var stateBefore = TimewarpedFailureStateFingerprint(service);

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0)));

            Assert.AreEqual(stateBefore, TimewarpedFailureStateFingerprint(service));
            Assert.AreEqual(chronumBefore, timewarp.Chronum);
            Assert.IsFalse(offer.Purchased);
            Assert.AreEqual(10, tavern.Hand.Count);
        }

        [Test]
        public void WhiteBox_TimewarpedPurchase_InsufficientChronumPreservesStateAndForbiddenCounters()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            var definition = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .First(card => card.CardKind == CardKind.Minion);
            timewarp.Chronum = 0;
            timewarp.Offers = new List<TimewarpedOfferSlot>
            {
                new TimewarpedOfferSlot
                {
                    SlotId = "insufficient-chronum",
                    CardId = definition.CardId,
                    CardKind = definition.CardKind,
                    Cost = 1,
                    Source = "test"
                }
            };
            var stateBefore = TimewarpedFailureStateFingerprint(service);

            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0)));

            Assert.AreEqual("时空资源不足。", exception.Message);
            Assert.AreEqual(stateBefore, TimewarpedFailureStateFingerprint(service));
        }

        [Test]
        public void WhiteBox_TimewarpedPurchase_InvalidOfferIndexPreservesStateAndForbiddenCounters()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var stateBefore = TimewarpedFailureStateFingerprint(service);

            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, -1)));

            Assert.AreEqual("时空酒馆选项不存在。", exception.Message);
            Assert.AreEqual(stateBefore, TimewarpedFailureStateFingerprint(service));
        }

        [Test]
        public void WhiteBox_TimewarpedPurchase_MissingDefinitionPreservesEquippedQuestAndTrinketState()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var tavern = service.State.Player.Tavern;
            var advanced = tavern.AdvancedMechanics;
            advanced.Quests.MainQuest = new ActiveQuestState
            {
                QuestId = "atomicity-quest",
                RewardId = "atomicity-reward",
                Progress = 3,
                RequiredAmount = 7,
                Completed = true,
                RewardActive = true
            };
            advanced.Quests.RewardCounters["atomicity-counter"] = 5;
            advanced.Quests.RewardFlags["atomicity-flag"] = true;
            advanced.Trinkets.LesserTrinketId = "atomicity-trinket";
            advanced.Trinkets.Equipped.Add(new EquippedTrinketState
            {
                TrinketId = "atomicity-trinket",
                Name = "Atomicity Trinket",
                SlotKind = TrinketSlotKind.Lesser,
                EquippedRound = service.State.Round,
                CostPaid = 2,
                ImplementationStatus = TrinketImplementationStatus.Implemented
            });
            tavern.Timewarp.Offers = new List<TimewarpedOfferSlot>
            {
                new TimewarpedOfferSlot
                {
                    SlotId = "missing-definition",
                    CardId = "MISSING_TIMEWARPED_DEFINITION",
                    CardKind = CardKind.Minion,
                    Cost = 1,
                    Source = "test"
                }
            };
            var stateBefore = TimewarpedFailureStateFingerprint(service);

            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0)));

            Assert.AreEqual(
                "时空酒馆卡牌数据缺失。",
                exception.Message);
            Assert.AreEqual(stateBefore, TimewarpedFailureStateFingerprint(service));
        }

        [Test]
        public void TimewarpedTavern_OffersAreDeterministicForSameSeed()
        {
            var first = CreateTimewarpOnlyService(24680);
            var second = CreateTimewarpOnlyService(24680);

            AdvanceToRound(first, 6);
            AdvanceToRound(second, 6);

            CollectionAssert.AreEqual(
                first.State.Player.Tavern.Timewarp.Offers.Select(offer => offer.CardId).ToList(),
                second.State.Player.Tavern.Timewarp.Offers.Select(offer => offer.CardId).ToList());
        }

        [Test]
        public void TimewarpedTavern_OfferCardsAndErrorsFollowSetupLanguage()
        {
            var chinese = CreateTimewarpOnlyService(24680);
            var english = MatchService.CreateWithDefaultCatalog(
                24680,
                null,
                new MatchSetupOptions
                {
                    UseEnglish = true,
                    AdvancedMechanicMode = AdvancedMechanicMode.Timewarp,
                    EnableTrinkets = false
                });
            AdvanceToRound(chinese, 6);
            AdvanceToRound(english, 6);

            var chineseCards = chinese.GetTimewarpedOfferCards();
            var offerIndex = chineseCards.FindIndex(card => card != null && card.Cost > 0);
            Assert.GreaterOrEqual(offerIndex, 0);
            var chineseCard = chineseCards[offerIndex];
            var englishCard = english.GetTimewarpedOfferCards().First(card => card != null && card.CardId == chineseCard.CardId);
            var definition = chinese.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .First(card => card.CardId == chineseCard.CardId);

            Assert.AreEqual(definition.ZhName, chineseCard.Name);
            Assert.AreEqual(definition.ZhText, chineseCard.Text);
            Assert.AreEqual(definition.Name, englishCard.Name);
            Assert.AreEqual(definition.Text, englishCard.Text);
            Assert.IsTrue(chinese.State.Player.Tavern.RecruitLog.Any(entry =>
                entry.Message.Contains("小型时空酒馆已开启") &&
                entry.Message.Contains("获得") &&
                entry.Message.Contains("当前共有")));
            Assert.IsFalse(chinese.State.Player.Tavern.RecruitLog.Any(entry =>
                entry.Message.Contains("尚待确认") ||
                entry.Message.Contains("rule-unconfirmed") ||
                entry.Message.Contains("Chronum")));
            Assert.IsTrue(english.State.Player.Tavern.RecruitLog.Any(entry =>
                entry.Message.Contains("Minor Timewarped Tavern opened. Gained") &&
                entry.Message.Contains("current Chronum")));
            Assert.IsFalse(english.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains("时空酒馆已开启")));

            chinese.State.Player.Tavern.Timewarp.Chronum = 0;
            english.State.Player.Tavern.Timewarp.Chronum = 0;
            Assert.AreEqual(
                "时空资源不足。",
                Assert.Throws<System.InvalidOperationException>(() =>
                    chinese.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, offerIndex))).Message);
            Assert.AreEqual(
                "Not enough Chronum.",
                Assert.Throws<System.InvalidOperationException>(() =>
                    english.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, offerIndex))).Message);
        }

        [Test]
        public void TimewarpedTavern_SmartOffersPrioritizeBoardAndHandDirections()
        {
            var service = CreateTimewarpOnlyService(
                13579,
                new List<Tribe> { Tribe.Beast, Tribe.Murloc, Tribe.Pirate });
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("smart-beast-a", "Smart Beast A", "SMART_BEAST_A", 1, 1, Tribe.Beast, 1));
            service.State.Player.Board.Add(TestBoardMinion("smart-beast-b", "Smart Beast B", "SMART_BEAST_B", 1, 1, Tribe.Beast, 1));
            service.State.Player.Tavern.Hand.Add(TestBoardMinion("smart-hand-murloc", "Smart Hand Murloc", "SMART_HAND_MURLOC", 1, 1, Tribe.Murloc, 1));

            AdvanceToRound(service, 6);

            var offers = CurrentTimewarpedOfferDefinitions(service);
            Assert.AreEqual(4, offers.Count);
            Assert.AreEqual(4, offers.Select(card => card.CardId).Distinct().Count());
            Assert.GreaterOrEqual(offers.Count(card => HasAnyTimewarpedConcreteTribe(card, Tribe.Beast, Tribe.Murloc)), 2);
            Assert.IsTrue(offers.Any(card => HasAnyTimewarpedConcreteTribe(card, Tribe.Pirate)));
            Assert.IsTrue(offers.Any(IsGenericTimewarpedOffer));
            Assert.IsTrue(offers.All(card => ConcreteTimewarpedOfferTribes(card)
                .All(tribe => tribe == Tribe.Beast || tribe == Tribe.Murloc || tribe == Tribe.Pirate)));
            Assert.IsFalse(offers.Any(card => card.PoolStatus == "historical_extra"));
        }

        [Test]
        public void TimewarpedTavern_SmartOffersAreDeterministicWithBoardAndHandContext()
        {
            var first = CreateTimewarpOnlyService(
                97531,
                new List<Tribe> { Tribe.Beast, Tribe.Murloc, Tribe.Pirate });
            var second = CreateTimewarpOnlyService(
                97531,
                new List<Tribe> { Tribe.Beast, Tribe.Murloc, Tribe.Pirate });
            SeedSmartTimewarpDirection(first);
            SeedSmartTimewarpDirection(second);

            AdvanceToRound(first, 6);
            AdvanceToRound(second, 6);

            CollectionAssert.AreEqual(
                first.State.Player.Tavern.Timewarp.Offers.Select(offer => offer.CardId).ToList(),
                second.State.Player.Tavern.Timewarp.Offers.Select(offer => offer.CardId).ToList());
        }

        [Test]
        public void TimewarpedTavern_WaitsForTrinketChoiceThenOpens()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                13579,
                null,
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Mixed,
                    EnableQuests = false,
                    EnableQuestRewards = false,
                    EnableTrinkets = true
                });

            AdvanceToRound(service, 6);

            var tavern = service.State.Player.Tavern;
            Assert.IsNotNull(tavern.AdvancedMechanics.PendingChoice);
            Assert.AreEqual(AdvancedMechanicKind.Trinket, tavern.AdvancedMechanics.PendingChoice.Kind);
            Assert.AreEqual(TimewarpTavernPhase.BlockedByTrinketChoice, tavern.Timewarp.Phase);
            Assert.AreEqual(TimewarpKind.Minor, tavern.Timewarp.PendingKind);

            tavern.Gold = 100;
            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.IsNull(tavern.AdvancedMechanics.PendingChoice);
            Assert.IsTrue(tavern.Timewarp.VisitOpen);
            Assert.AreEqual(TimewarpTavernPhase.Open, tavern.Timewarp.Phase);
            Assert.AreEqual(TimewarpKind.Minor, tavern.Timewarp.PendingKind);
        }

        [Test]
        public void TimewarpedTavern_ThreeCopiesCanTriple()
        {
            var service = CreateTimewarpOnlyService(12345);
            AdvanceToRound(service, 6);
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            var cardId = service.GetTimewarpedCandidateDefinitions(TimewarpKind.Minor)
                .First(card => card.CardKind == CardKind.Minion)
                .CardId;
            timewarp.Chronum = 10;
            timewarp.Offers = new List<TimewarpedOfferSlot>
            {
                new TimewarpedOfferSlot { SlotId = "fixed-0", CardId = cardId, CardKind = CardKind.Minion, Cost = 1, Source = "test" },
                new TimewarpedOfferSlot { SlotId = "fixed-1", CardId = cardId, CardKind = CardKind.Minion, Cost = 1, Source = "test" },
                new TimewarpedOfferSlot { SlotId = "fixed-2", CardId = cardId, CardKind = CardKind.Minion, Cost = 1, Source = "test" }
            };

            service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0));
            service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 1));
            service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 2));

            var definitionId = "timewarped-" + cardId;
            Assert.AreEqual(7, timewarp.Chronum);
            Assert.AreEqual(1, tavern.Hand.Count(card => card.DefinitionId == definitionId));
            Assert.IsTrue(tavern.Hand.Any(card => card.DefinitionId == definitionId && card.Golden));
        }

        [Test]
        public void TimewarpedKeywordGroup_MapsStaticKeywordBodies()
        {
            var catalog = TimewarpedTavernCatalogLoader.LoadFromResources();
            var expected = new Dictionary<string, Keyword[]>
            {
                { "BG34_Giant_007", new[] { Keyword.Taunt, Keyword.DivineShield, Keyword.Reborn } },
                { "BG34_Giant_302", new[] { Keyword.DivineShield, Keyword.Avenge } },
                { "BG34_Giant_012", new[] { Keyword.DivineShield, Keyword.Windfury, Keyword.Reborn } },
                { "BG34_Giant_332", new[] { Keyword.Trigger } },
                { "BG34_Giant_584", new[] { Keyword.Taunt, Keyword.Deathrattle } },
                { "BG34_Giant_031", new[] { Keyword.Taunt, Keyword.Reborn, Keyword.Deathrattle } },
                { "BG34_Giant_207", new[] { Keyword.DivineShield, Keyword.Trigger } },
                { "BG34_Giant_204", new[] { Keyword.Taunt, Keyword.Reborn, Keyword.Deathrattle } },
                { "BG34_Giant_589", new[] { Keyword.DivineShield, Keyword.Trigger } },
                { "BG34_Giant_304", new[] { Keyword.Taunt, Keyword.Deathrattle } },
                { "BG34_Giant_017", new[] { Keyword.Taunt, Keyword.Deathrattle } },
                { "BG34_Giant_360", new[] { Keyword.Taunt, Keyword.EndOfTurn } },
                { "BG34_Giant_582", new[] { Keyword.Taunt, Keyword.Deathrattle } },
                { "BG34_Giant_039", new[] { Keyword.Stealth, Keyword.Trigger } },
                { "BG34_Giant_777", new[] { Keyword.Taunt, Keyword.Trigger } },
                { "BG34_Giant_102", new[] { Keyword.Windfury, Keyword.Rally } },
                { "BG34_Giant_680", new[] { Keyword.Rally, Keyword.Cleave } },
                { "BG34_Giant_644", new[] { Keyword.DivineShield, Keyword.Trigger } },
                { "BG34_Giant_035", new[] { Keyword.Taunt, Keyword.Spellcraft, Keyword.Trigger } },
                { "BG34_Giant_342", new[] { Keyword.Trigger } },
                { "BG34_Giant_040", new[] { Keyword.DivineShield, Keyword.Trigger } },
                { "BG34_PreMadeChamp_004", new[] { Keyword.Stealth, Keyword.Trigger } },
                { "BG34_Giant_608", new[] { Keyword.Reborn, Keyword.Deathrattle } },
                { "BG34_Giant_314", new[] { Keyword.DivineShield, Keyword.Trigger } },
                { "BG34_Giant_110", new[] { Keyword.DivineShield, Keyword.Rally } },
                { "BG34_PreMadeChamp_032", new[] { Keyword.DivineShield, Keyword.EndOfTurn } },
                { "BG34_Giant_677", new[] { Keyword.Magnetic } },
                { "BG34_Giant_331", new[] { Keyword.Taunt, Keyword.Deathrattle } }
            };

            Assert.AreEqual(28, expected.Count);
            foreach (var pair in expected)
            {
                Assert.IsTrue(catalog.TryGetByCardId(pair.Key, out var card), pair.Key);
                foreach (var keyword in pair.Value)
                {
                    Assert.Contains(keyword, card.Keywords, pair.Key);
                }
            }

            Assert.IsFalse(catalog.All.First(card => card.CardId == "BG34_Giant_031").Keywords.Contains(Keyword.Trigger));
        }

        [Test]
        public void TimewarpedKeywordGrantBodies_ApplyToPlayedMinions()
        {
            var service = CreateTimewarpOnlyService(12345);

            BuyFixedTimewarpedCard(service, "BG34_Giant_040");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_332");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            BuyFixedTimewarpedCard(service, "BG34_Giant_009");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            var played = service.State.Player.Board.Last(card => card.CardId == "BG34_Giant_009");
            Assert.Contains(Keyword.DivineShield, played.Keywords);
            Assert.Contains(Keyword.Reborn, played.Keywords);
        }

        [Test]
        public void TimewarpedStats_BuyingElementalBuffsBehemoth()
        {
            var service = CreateTimewarpOnlyService(12345);

            BuyFixedTimewarpedCard(service, "BG34_Giant_777");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var behemoth = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_777");
            var attackBefore = behemoth.Attack;
            var healthBefore = behemoth.MaxHealth;

            BuyFixedTimewarpedCard(service, "BG34_Giant_012");

            Assert.AreEqual(attackBefore + 6, behemoth.Attack);
            Assert.AreEqual(healthBefore + 1, behemoth.MaxHealth);
        }

        [Test]
        public void TimewarpedStats_CardAddedAndTurnEndedBuffsResolve()
        {
            var service = CreateTimewarpOnlyService(12345);

            BuyFixedTimewarpedCard(service, "BG34_Giant_327");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var pirate = TestBoardMinion("tw-pirate", "Pirate", "TEST_PIRATE", 2, 2, Tribe.Pirate, 1);
            service.State.Player.Board.Add(pirate);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG20_100", CardKind.Minion));

            Assert.AreEqual(3, pirate.Attack);
            Assert.AreEqual(3, pirate.MaxHealth);

            BuyFixedTimewarpedCard(service, "BG34_Giant_209");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            var mech = TestBoardMinion("tw-mech", "Mech", "TEST_MECH", 1, 1, Tribe.Mech, 1);
            service.State.Player.Board.Add(mech);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(4, mech.Attack);
            Assert.AreEqual(4, mech.MaxHealth);
        }

        [Test]
        public void TimewarpedStats_CombatStartBuffsUseExistingCombatPath()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Tavern.Tier = 3;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_029");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Board.Insert(0, TestBoardMinion("tw-left", "Left", "TEST_LEFT", 1, 10, Tribe.Mech, 1));
            service.State.Player.Board.Add(TestBoardMinion("tw-right", "Right", "TEST_RIGHT", 1, 10, Tribe.Pirate, 1));
            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, "BG20_100"));
            service.Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, service.State.Opponent.Board[0].InstanceId, new MinionPatch { Attack = 0, Health = 20, MaxHealth = 20 }));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 7, SafetyLimit = 1 }));

            Assert.AreEqual(4, service.State.LastResult.FinalPlayerBoard.First(card => card.CardId == "TEST_LEFT").Attack);
            Assert.AreEqual(6, service.State.LastResult.FinalPlayerBoard.First(card => card.CardId == "BG34_Giant_029").Attack);
            Assert.AreEqual(4, service.State.LastResult.FinalPlayerBoard.First(card => card.CardId == "TEST_RIGHT").Attack);
        }

        [Test]
        public void TimewarpedStats_DeathrattleRewardsPersist()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_306");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var jazzer = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_306");
            jazzer.Attack = 0;
            jazzer.Health = 1;
            jazzer.MaxHealth = 1;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-a", "Enemy A", "TEST_ENEMY_A", 5, 5, Tribe.None, 1));
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-b", "Enemy B", "TEST_ENEMY_B", 1, 5, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 3, SafetyLimit = 1 }));

            Assert.AreEqual(1, service.State.Player.Tavern.BloodGemBonusHealth);
        }

        [Test]
        public void TimewarpedStats_KillRewardBuffsHand()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_207");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_207").Attack = 5;
            var target = TestBoardMinion("hand-target", "Hand Target", "HAND_TARGET", 2, 2, Tribe.None, 1);
            service.State.Player.Tavern.Hand.Add(target);
            service.State.Opponent.Board.Add(TestBoardMinion("enemy", "Enemy", "TEST_ENEMY", 1, 1, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 4, SafetyLimit = 1 }));

            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(6, target.MaxHealth);
        }

        [Test]
        public void TimewarpedStats_SpellcraftAndRecruitTriggersResolve()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_212");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var target = TestBoardMinion("spell-target", "Spell Target", "SPELL_TARGET", 2, 2, Tribe.None, 1);
            service.State.Player.Board.Add(target);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG34_Giant_212t");
            Assert.GreaterOrEqual(spellIndex, 0);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, 1));

            Assert.AreEqual(14, target.Attack);

            BuyFixedTimewarpedCard(service, "BG34_Giant_320");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.State.Player.Board.Add(TestBoardMinion("murloc-a", "Murloc A", "MURLOC_A", 1, 1, Tribe.Murloc, 1));
            service.State.Player.Board.Add(TestBoardMinion("murloc-b", "Murloc B", "MURLOC_B", 1, 1, Tribe.Murloc, 1));
            service.State.Player.Board.Add(TestBoardMinion("murloc-c", "Murloc C", "MURLOC_C", 1, 1, Tribe.Murloc, 1));

            service.Apply(new GameCommand(GameCommandType.SellMinion, "murloc-a"));
            service.Apply(new GameCommand(GameCommandType.SellMinion, "murloc-b"));
            service.Apply(new GameCommand(GameCommandType.SellMinion, "murloc-c"));

            Assert.AreEqual(2, service.State.Player.Tavern.TavernSpellBonusAttack);
            Assert.AreEqual(3, service.State.Player.Tavern.TavernSpellBonusHealth);
        }

        [Test]
        public void TimewarpedStats_StoneDrakeUsesSoldStats()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_675");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var stoneDrake = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_675");
            var beforeAttack = stoneDrake.Attack;
            var beforeHealth = stoneDrake.MaxHealth;
            var sold = TestBoardMinion("sold-minion", "Sold Minion", "SOLD_MINION", 3, 5, Tribe.None, 1);
            service.State.Player.Board.Add(sold);
            service.Apply(new GameCommand(GameCommandType.SellMinion, sold.InstanceId));
            service.State.Opponent.Board.Add(TestBoardMinion("enemy", "Enemy", "TEST_ENEMY", 0, 20, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 5, SafetyLimit = 1 }));

            var finalStoneDrake = service.State.LastResult.FinalPlayerBoard.First(card => card.CardId == "BG34_Giant_675");
            Assert.AreEqual(beforeAttack + 3, finalStoneDrake.Attack);
            Assert.AreEqual(beforeHealth + 5, finalStoneDrake.MaxHealth);
        }

        [Test]
        public void TimewarpedTriggers_BattlecryAndDeathrattleUseCommonEntrypoint()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_001");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.AreEqual(1, service.State.Player.Tavern.NextTurnBonusGold);

            var busker = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_001");
            busker.Attack = 0;
            busker.Health = 1;
            busker.MaxHealth = 1;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-busker", "Enemy", "TEST_ENEMY", 1, 20, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 11, SafetyLimit = 1 }));

            Assert.AreEqual(2, service.State.Player.Tavern.NextTurnBonusGold);
        }

        [Test]
        public void TimewarpedTriggers_DeathrattleAddsTavernCoin()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_204");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var pillager = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_204");
            pillager.Attack = 0;
            pillager.Health = 1;
            pillager.MaxHealth = 1;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-pillager", "Enemy", "TEST_ENEMY", 1, 20, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 12, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));
        }

        [Test]
        public void TimewarpedTriggers_AvengeAddsSpellcraftAndTavernSpell()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_211");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var pashmar = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_211");
            pashmar.Health = 50;
            pashmar.MaxHealth = 50;
            service.State.Player.Board.Insert(0, TestBoardMinion("avenge-a", "Avenge A", "AVENGE_A", 0, 1, Tribe.None, 1));
            service.State.Player.Board.Insert(1, TestBoardMinion("avenge-b", "Avenge B", "AVENGE_B", 0, 1, Tribe.None, 1));
            service.State.Player.Board.Insert(2, TestBoardMinion("avenge-c", "Avenge C", "AVENGE_C", 0, 1, Tribe.None, 1));
            service.State.Player.Board[0].Keywords.Add(Keyword.Taunt);
            service.State.Player.Board[1].Keywords.Add(Keyword.Taunt);
            service.State.Player.Board[2].Keywords.Add(Keyword.Taunt);
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-avenge", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 13, SafetyLimit = 8 }));

            Assert.GreaterOrEqual(service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell), 2);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.Tags.Any(tag => tag.ToLowerInvariant().Contains("spellcraft"))));
        }

        [Test]
        public void TimewarpedTriggers_RallyAddsDragon()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_585");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-rally", "Enemy", "TEST_ENEMY", 0, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 14, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion && BoardTribeAnalyzer.HasTribe(card, Tribe.Dragon)));
        }

        [Test]
        public void TimewarpedTriggers_TurnStartAndEndUseGeneratedCardEntrypoints()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_PreMadeChamp_076");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_594");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.Tags.Contains("timewarped_blood_gem_tavern_spell")));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion && card.TavernTier == service.State.Player.Tavern.Tier));
        }

        [Test]
        public void TimewarpedBloodGems_GeomancerAvengeAddsPlainBloodGemAndImprovesStats()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_305");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var geomancer = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_305");
            geomancer.Health = 50;
            geomancer.MaxHealth = 50;
            for (var index = 0; index < 5; index += 1)
            {
                var fodder = TestBoardMinion("geomancer-fodder-" + index, "Fodder", "GEOMANCER_FODDER", 0, 1, Tribe.None, 1);
                fodder.Keywords.Add(Keyword.Taunt);
                service.State.Player.Board.Insert(index, fodder);
            }

            service.State.Opponent.Board.Add(TestBoardMinion("enemy-geomancer", "Enemy", "TEST_ENEMY", 1, 120, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 21, SafetyLimit = 16 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BLOOD_GEM"));
            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BRISTLEBACK_BLOOD_GEM"));
            Assert.AreEqual(1, service.State.Player.Tavern.BloodGemBonusAttack);
            Assert.AreEqual(1, service.State.Player.Tavern.BloodGemBonusHealth);
        }

        [Test]
        public void TimewarpedBloodGems_BanditDiscardsSpellAndPlaysBloodGemsOnBoard()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_PreMadeChamp_078");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var target = TestBoardMinion("bandit-target", "Bandit Target", "BANDIT_TARGET", 2, 3, Tribe.Beast, 1);
            service.State.Player.Board.Add(target);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BLOOD_GEM", CardKind.Spell));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BLOOD_GEM"));
            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(7, target.MaxHealth);
        }

        [Test]
        public void TimewarpedBloodGems_BonkerRallyPlaysBloodGemsOnOtherMinions()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_102");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var bonker = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_102");
            bonker.Attack = 1;
            bonker.Health = 20;
            bonker.MaxHealth = 20;
            var other = TestBoardMinion("bonker-other", "Other", "BONKER_OTHER", 2, 2, Tribe.Beast, 1);
            service.State.Player.Board.Add(other);
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-bonker", "Enemy", "TEST_ENEMY", 0, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 22, SafetyLimit = 1 }));

            Assert.AreEqual(4, other.Attack);
            Assert.AreEqual(4, other.MaxHealth);
            Assert.AreEqual(1, service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_102").Attack);
        }

        [Test]
        public void TimewarpedBloodGems_LilQuilboarDeathrattlePlaysBloodGemsOnQuilboarTypes()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_608");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var lil = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_608");
            lil.Attack = 0;
            lil.Health = 1;
            lil.MaxHealth = 1;
            lil.Keywords.Add(Keyword.Taunt);
            var quilboar = TestBoardMinion("lil-quilboar-target", "Quilboar Target", "LIL_QUILBOAR_TARGET", 2, 4, Tribe.Quilboar, 1);
            var allType = TestBoardMinion("lil-all-target", "All Target", "LIL_ALL_TARGET", 3, 5, Tribe.All, 1);
            var beast = TestBoardMinion("lil-beast-target", "Beast Target", "LIL_BEAST_TARGET", 4, 6, Tribe.Beast, 1);
            service.State.Player.Board.Add(quilboar);
            service.State.Player.Board.Add(allType);
            service.State.Player.Board.Add(beast);
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-lil-quilboar", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 23, SafetyLimit = 1 }));

            var finalQuilboar = service.State.LastResult.FinalPlayerBoard.First(card => card.InstanceId == "lil-quilboar-target");
            var finalAll = service.State.LastResult.FinalPlayerBoard.First(card => card.InstanceId == "lil-all-target");
            var finalBeast = service.State.LastResult.FinalPlayerBoard.First(card => card.InstanceId == "lil-beast-target");
            Assert.AreEqual(5, finalQuilboar.Attack);
            Assert.AreEqual(7, finalQuilboar.MaxHealth);
            Assert.AreEqual(6, finalAll.Attack);
            Assert.AreEqual(8, finalAll.MaxHealth);
            Assert.AreEqual(4, finalBeast.Attack);
            Assert.AreEqual(6, finalBeast.MaxHealth);
        }

        [Test]
        public void TimewarpedBloodGems_PiperDamageImprovesBloodGemAttackUpToCombatLimit()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_069");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var piper = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_069");
            piper.Attack = 0;
            piper.Health = 10;
            piper.MaxHealth = 10;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-piper", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 24, SafetyLimit = 8 }));

            Assert.AreEqual(2, service.State.Player.Tavern.BloodGemBonusAttack);
        }

        [Test]
        public void TimewarpedBloodGems_ThorncallerBattlecryAndDeathrattleAddBloodGemBarrage()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_078");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "126676"));

            service.State.Player.Tavern.Hand.Clear();
            var thorncaller = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_078");
            thorncaller.Attack = 0;
            thorncaller.Health = 1;
            thorncaller.MaxHealth = 1;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-thorncaller", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 25, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "126676"));
        }

        [Test]
        public void TimewarpedBloodGems_GemsplitterImprovesAttackWhenFriendlyDivineShieldBreaks()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_644");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var shielded = TestBoardMinion("gemsplitter-shielded", "Shielded", "GEMSPLITTER_SHIELDED", 0, 10, Tribe.Mech, 1);
            shielded.Keywords.Add(Keyword.DivineShield);
            shielded.Keywords.Add(Keyword.Taunt);
            service.State.Player.Board.Insert(0, shielded);
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-gemsplitter", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 26, SafetyLimit = 1 }));

            Assert.AreEqual(1, service.State.Player.Tavern.BloodGemBonusAttack);
        }

        [Test]
        public void TimewarpedSummons_BassgillSummonsHighestHealthHandMinionWithCombatOnlyDivineShield()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_071");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var bassgill = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_071");
            bassgill.Attack = 0;
            bassgill.Health = 1;
            bassgill.MaxHealth = 1;
            var lowHealth = TestBoardMinion("bassgill-low", "Low", "BASSGILL_LOW", 3, 3, Tribe.Beast, 1);
            var highHealth = TestBoardMinion("bassgill-high", "High", "BASSGILL_HIGH", 4, 9, Tribe.Dragon, 1);
            service.State.Player.Tavern.Hand.Add(lowHealth);
            service.State.Player.Tavern.Hand.Add(highHealth);
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-bassgill", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 27, SafetyLimit = 1 }));

            var summoned = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId.Contains("bassgill-high"));
            Assert.IsTrue(summoned.Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.InstanceId == "bassgill-high"));
            Assert.IsFalse(highHealth.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void TimewarpedSummons_ScourfinBuffsHandMinionAndSummonsCombatOnlyCopy()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_017");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var scourfin = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_017");
            scourfin.Attack = 0;
            scourfin.Health = 1;
            scourfin.MaxHealth = 1;
            var handTarget = TestBoardMinion("scourfin-hand", "Hand Target", "SCOURFIN_HAND", 2, 2, Tribe.Murloc, 1);
            service.State.Player.Tavern.Hand.Add(handTarget);
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-scourfin", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 28, SafetyLimit = 1 }));

            var summoned = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId.Contains("scourfin-hand"));
            Assert.AreEqual(9, summoned.Attack);
            Assert.AreEqual(9, summoned.MaxHealth);
            Assert.AreEqual(9, handTarget.Attack);
            Assert.AreEqual(9, handTarget.MaxHealth);
        }

        [Test]
        public void TimewarpedSummons_FestergutSummonsAndGetsUndeadCreation()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_590");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var festergut = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_590");
            festergut.Attack = 0;
            festergut.Health = 1;
            festergut.MaxHealth = 1;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-festergut", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 29, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Undead)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Undead)));
        }

        [Test]
        public void TimewarpedSummons_NelliesShipSummonsAndGetsPirate()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_074t");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var ship = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_074t");
            ship.Attack = 0;
            ship.Health = 1;
            ship.MaxHealth = 1;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-nellie", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 30, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Pirate)));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Pirate)));
        }

        [Test]
        public void TimewarpedSummons_TideRazorSummonsAndGetsFourPirates()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_328");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var tideRazor = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_328");
            tideRazor.Attack = 0;
            tideRazor.Health = 1;
            tideRazor.MaxHealth = 1;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-tide-razor", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 31, SafetyLimit = 1 }));

            Assert.GreaterOrEqual(service.State.LastResult.FinalPlayerBoard.Count(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Pirate)), 4);
            Assert.GreaterOrEqual(service.State.Player.Tavern.Hand.Count(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Pirate)), 4);
        }

        [Test]
        public void TimewarpedDamage_RagnarosHitsHighestCurrentHealthEnemy()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_580");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var ragnaros = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_580");
            ragnaros.Attack = 4;
            ragnaros.CanAttack = false;
            var highMaxLowCurrent = TestBoardMinion("rag-low-current", "Low Current", "RAG_LOW_CURRENT", 0, 1, Tribe.None, 1);
            highMaxLowCurrent.MaxHealth = 20;
            highMaxLowCurrent.CanAttack = false;
            var highCurrent = TestBoardMinion("rag-high-current", "High Current", "RAG_HIGH_CURRENT", 0, 10, Tribe.None, 1);
            highCurrent.CanAttack = false;
            service.State.Opponent.Board.Add(highMaxLowCurrent);
            service.State.Opponent.Board.Add(highCurrent);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 32, SafetyLimit = 1 }));

            var finalLowCurrent = service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "rag-low-current");
            var finalHighCurrent = service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "rag-high-current");
            Assert.AreEqual(1, finalLowCurrent.Health);
            Assert.AreEqual(6, finalHighCurrent.Health);
        }

        [Test]
        public void TimewarpedDamage_RedWhelpDamagesTwoEnemiesAndImprovesAfterDragon()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_091");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var whelp = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_091");
            whelp.Attack = 0;
            whelp.CanAttack = false;

            BuyFixedTimewarpedCard(service, "BG34_Giant_029");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            var dragon = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_029");
            dragon.CanAttack = false;

            Assert.AreEqual(1, whelp.Counters["timewarped_red_whelp_bonus"]);
            var enemyA = TestBoardMinion("red-whelp-a", "Enemy A", "RED_WHELP_A", 0, 20, Tribe.None, 1);
            var enemyB = TestBoardMinion("red-whelp-b", "Enemy B", "RED_WHELP_B", 0, 20, Tribe.None, 1);
            enemyA.CanAttack = false;
            enemyB.CanAttack = false;
            service.State.Opponent.Board.Add(enemyA);
            service.State.Opponent.Board.Add(enemyB);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 33, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.LastResult.FinalOpponentBoard.All(card => card.Health == 16));
        }

        [Test]
        public void TimewarpedDamage_ClefthoofBuffsAndDamagesBeastsAtTurnEnd()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_PreMadeChamp_090");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var beast = TestBoardMinion("clefthoof-beast", "Beast", "CLEFTHOOF_BEAST", 2, 10, Tribe.Beast, 1);
            service.State.Player.Board.Add(beast);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(8, beast.Attack);
            Assert.AreEqual(16, beast.MaxHealth);
            Assert.AreEqual(13, beast.Health);
        }

        [Test]
        public void TimewarpedDamage_RewinderRewindsHeroDamageAndBuffsDemons()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_300");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var rewinder = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_300");
            rewinder.Health = 4;
            rewinder.MaxHealth = 4;
            var otherDemon = TestBoardMinion("rewinder-demon", "Other Demon", "REWINDER_DEMON", 2, 5, Tribe.Demon, 1);
            service.State.Player.Board.Add(otherDemon);
            service.State.Player.Health = 30;
            service.State.Player.Armor = 0;
            service.State.Player.Tavern.FreeRefreshes = 0;
            service.State.Player.Tavern.HealthCostRefreshes = 1;
            service.State.Player.Tavern.Gold = 0;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual(30, service.State.Player.Health);
            Assert.AreEqual(6, rewinder.MaxHealth);
            Assert.AreEqual(6, rewinder.Health);
            Assert.AreEqual(7, otherDemon.MaxHealth);
            Assert.AreEqual(7, otherDemon.Health);
        }

        [Test]
        public void TimewarpedDamage_ArchimondeRewindsHeroDamageAndDiscountsNextTavernSpell()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_596");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var tavern = service.State.Player.Tavern;
            service.State.Player.Health = 30;
            service.State.Player.Armor = 0;
            tavern.FreeRefreshes = 0;
            tavern.HealthCostRefreshes = 1;
            tavern.Gold = 0;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual(30, service.State.Player.Health);
            Assert.AreEqual(1, tavern.NextTavernSpellCostReduction);

            var spell = TestTavernSpell("archimonde-discount-spell");
            spell.Counters["base_buy_cost"] = 1;
            tavern.Shop.Clear();
            tavern.Shop.Add(spell);
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(0, tavern.Gold);
            Assert.AreEqual(0, tavern.NextTavernSpellCostReduction);
            Assert.IsTrue(tavern.Hand.Any(card => card.InstanceId == "archimonde-discount-spell"));
        }

        [Test]
        public void TimewarpedDamage_CollectorDamagesAdjacentEnemiesAndRalliesDivineShield()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_680");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var collector = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_680");
            collector.Attack = 5;
            collector.Health = 30;
            collector.MaxHealth = 30;
            for (var index = 0; index < 4; index += 1)
            {
                var golden = TestBoardMinion("collector-golden-" + index, "Golden " + index, "COLLECTOR_GOLDEN", 1, 1, Tribe.None, 1);
                golden.Golden = true;
                service.State.Player.Board.Add(golden);
            }

            var left = TestBoardMinion("collector-left", "Left", "COLLECTOR_LEFT", 0, 20, Tribe.None, 1);
            var middle = TestBoardMinion("collector-middle", "Middle", "COLLECTOR_MIDDLE", 0, 20, Tribe.None, 1);
            var right = TestBoardMinion("collector-right", "Right", "COLLECTOR_RIGHT", 0, 20, Tribe.None, 1);
            middle.Keywords.Add(Keyword.Taunt);
            service.State.Opponent.Board.Add(left);
            service.State.Opponent.Board.Add(middle);
            service.State.Opponent.Board.Add(right);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 34, SafetyLimit = 1 }));

            Assert.AreEqual(15, service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "collector-left").Health);
            Assert.AreEqual(15, service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "collector-middle").Health);
            Assert.AreEqual(15, service.State.LastResult.FinalOpponentBoard.Single(card => card.InstanceId == "collector-right").Health);
            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == collector.InstanceId).Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void TimewarpedTriggers_CalligrapherUsesBattlecryDeathrattleAndRally()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_PreMadeChamp_091");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var afterBattlecry = service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell);
            Assert.GreaterOrEqual(afterBattlecry, 1);

            service.State.Opponent.Board.Add(TestBoardMinion("enemy-calligrapher-rally", "Enemy", "TEST_ENEMY", 0, 80, Tribe.None, 1));
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 15, SafetyLimit = 1 }));
            Assert.Greater(service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell), afterBattlecry);
            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_PreMadeChamp_091");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var calligrapher = service.State.Player.Board.Single(card => card.CardId == "BG34_PreMadeChamp_091");
            calligrapher.Attack = 0;
            calligrapher.Health = 1;
            calligrapher.MaxHealth = 1;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-calligrapher-deathrattle", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 16, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void TimewarpedTriggers_AvengeCommonEntrypointHandlesOtherRewards()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_082");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var recycler = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_082");
            recycler.Health = 50;
            recycler.MaxHealth = 50;
            service.State.Player.Board.Insert(0, TestBoardMinion("recycler-avenge-a", "A", "A", 0, 1, Tribe.None, 1));
            service.State.Player.Board.Insert(1, TestBoardMinion("recycler-avenge-b", "B", "B", 0, 1, Tribe.None, 1));
            service.State.Player.Board.Insert(2, TestBoardMinion("recycler-avenge-c", "C", "C", 0, 1, Tribe.None, 1));
            service.State.Player.Board.Insert(3, TestBoardMinion("recycler-avenge-d", "D", "D", 0, 1, Tribe.None, 1));
            service.State.Player.Board[0].Keywords.Add(Keyword.Taunt);
            service.State.Player.Board[1].Keywords.Add(Keyword.Taunt);
            service.State.Player.Board[2].Keywords.Add(Keyword.Taunt);
            service.State.Player.Board[3].Keywords.Add(Keyword.Taunt);
            var beforeMaxGold = service.State.Player.Tavern.MaxGold;
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-recycler", "Enemy", "TEST_ENEMY", 1, 120, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 17, SafetyLimit = 12 }));

            Assert.Greater(service.State.Player.Tavern.MaxGold, beforeMaxGold);
        }

        [Test]
        public void TimewarpedTriggers_MurkEyeTriggersFriendlyBattlecriesAtEndOfTurn()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_318");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_001");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.State.Player.Tavern.NextTurnBonusGold = 0;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(service.State.Player.Tavern.MaxGold + 1, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void TimewarpedTriggers_HawkstriderTriggersFriendlyDeathrattlesAtCombatStart()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_370");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_204");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-hawkstrider", "Enemy", "TEST_ENEMY", 0, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 18, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));
        }

        [Test]
        public void TimewarpedTriggers_WarghoulTriggersAdjacentDeathrattle()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_204");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_331");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-warghoul-a", "Enemy A", "TEST_ENEMY_A", 10, 10, Tribe.None, 1));
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-warghoul-b", "Enemy B", "TEST_ENEMY_B", 0, 10, Tribe.None, 1));
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-warghoul-c", "Enemy C", "TEST_ENEMY_C", 0, 10, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 19, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));
        }

        [Test]
        public void TimewarpedTriggers_GreenskeeperTriggersRightMostBattlecryAndDeathrattleOnRally()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_041");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var battlecryTarget = TestBoardMinion("greenskeeper-battlecry", "Refreshing Anomaly", "BGS_116", 0, 20, Tribe.Elemental, 1);
            battlecryTarget.Keywords.Add(Keyword.Battlecry);
            service.State.Player.Board.Add(battlecryTarget);
            BuyFixedTimewarpedCard(service, "BG34_Giant_204");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-greenskeeper", "Enemy", "TEST_ENEMY", 0, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 20, SafetyLimit = 1 }));

            Assert.GreaterOrEqual(service.State.Player.Tavern.FreeRefreshes, 2);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));
        }

        [Test]
        public void TimewarpedSpecial_DeiosDoublesBattlecryTriggers()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.NextTurnBonusGold = 0;

            BuyFixedTimewarpedCard(service, "BG34_Giant_376");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_001");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(2, service.State.Player.Tavern.NextTurnBonusGold);
        }

        [Test]
        public void TimewarpedSpecial_DeiosDoublesDeathrattleTriggers()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_204");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_376");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            var pillager = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_204");
            pillager.Attack = 0;
            pillager.Health = 1;
            pillager.MaxHealth = 1;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-deios-deathrattle", "Enemy", "TEST_ENEMY", 1, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 21, SafetyLimit = 1 }));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));
        }

        [Test]
        public void TimewarpedSpecial_DeiosDoublesRallyTriggers()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_PreMadeChamp_091");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_376");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Board.Add(TestBoardMinion("enemy-deios-rally", "Enemy", "TEST_ENEMY", 0, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 22, SafetyLimit = 1 }));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void TimewarpedRefreshOffers_LubberAddsExtraTavernSpellSlot()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_066");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.GreaterOrEqual(service.State.Player.Tavern.Shop.Count(card => card?.CardKind == CardKind.TavernSpell), 2);
        }

        [Test]
        public void TimewarpedRefreshOffers_RaiderAddsExtraPirate()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_589");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var tavern = service.State.Player.Tavern;
            Assert.GreaterOrEqual(tavern.Shop.Count, TavernRules.GetShopSize(tavern.Tier) + 2);
            Assert.IsTrue(tavern.Shop.Any(card => card?.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Pirate)));
        }

        [Test]
        public void TimewarpedRefreshOffers_RaiderPreservesFrozenSlotAndAddsExtraOffer()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_589");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var tavern = service.State.Player.Tavern;
            TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance>
            {
                TestBoardMinion("refresh-frozen", "Frozen Shop Minion", "TEST_REFRESH_FROZEN", 1, 1, Tribe.Murloc, 1),
                TestBoardMinion("refresh-open", "Open Shop Minion", "TEST_REFRESH_OPEN", 1, 1, Tribe.Beast, 1)
            });
            TavernShopSlots.SetSlotFrozen(tavern, 0, true);
            tavern.Gold = 10;
            tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual("refresh-frozen", tavern.Shop[0].InstanceId);
            Assert.IsTrue(TavernShopSlots.IsSlotFrozen(tavern, 0));
            Assert.IsTrue(tavern.Shop.Any(card => card != null && card.InstanceId.StartsWith("timewarped-raider-", System.StringComparison.Ordinal)));
        }

        [Test]
        public void TimewarpedRefreshOffers_SnowElementalAddsFrozenElemental()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_586");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var frozenCards = TavernShopSlots.FrozenCards(service.State.Player.Tavern);
            Assert.IsTrue(frozenCards.Any(card => card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Elemental)));
        }

        [Test]
        public void TimewarpedRefreshOffers_KiljaedenPreservesFrozenSlotAndAddsExtraDemons()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_313");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var tavern = service.State.Player.Tavern;
            TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance>
            {
                TestBoardMinion("kiljaeden-frozen", "Frozen Shop Minion", "TEST_KILJAEDEN_FROZEN", 1, 1, Tribe.Pirate, 1),
                TestBoardMinion("kiljaeden-open", "Open Shop Minion", "TEST_KILJAEDEN_OPEN", 1, 1, Tribe.Beast, 1)
            });
            TavernShopSlots.SetSlotFrozen(tavern, 0, true);
            tavern.Gold = 10;
            tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var extraDemons = tavern.Shop
                .Where(card => card != null && card.InstanceId.StartsWith("timewarped-kiljaeden-", System.StringComparison.Ordinal))
                .ToList();
            Assert.AreEqual("kiljaeden-frozen", tavern.Shop[0].InstanceId);
            Assert.IsTrue(TavernShopSlots.IsSlotFrozen(tavern, 0));
            Assert.AreEqual(2, extraDemons.Count);
            Assert.IsTrue(extraDemons.All(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Demon)));
            Assert.IsTrue(extraDemons.All(card => card.Attack >= card.BaseAttack + 7));
        }

        [Test]
        public void TimewarpedRefreshSlots_UpstartDoublesRightmostShopMinion()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_361");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var rightmost = service.State.Player.Tavern.Shop.Last(card => card != null && card.CardKind == CardKind.Minion);
            Assert.GreaterOrEqual(rightmost.MaxHealth, rightmost.BaseHealth * 2);
        }

        [Test]
        public void TimewarpedRefreshSlots_UpstartTargetsFrozenExtraOfferAfterInjection()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_361");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            BuyFixedTimewarpedCard(service, "BG34_Giant_586");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var rightmost = service.State.Player.Tavern.Shop.Last(card => card != null && card.CardKind == CardKind.Minion);
            Assert.IsTrue(rightmost.InstanceId.StartsWith("timewarped-snow-elemental-", System.StringComparison.Ordinal));
            Assert.IsTrue(rightmost.Tags.Contains("frozen"));
            Assert.GreaterOrEqual(rightmost.MaxHealth, rightmost.BaseHealth * 2);
        }

        [Test]
        public void TimewarpedRefreshRewards_EliseMakesHighestTierShopMinionGoldenAfterFiveRefreshes()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_038");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            for (var index = 0; index < 5; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
            }

            Assert.IsTrue(service.State.Player.Tavern.Shop.Any(card => card?.CardKind == CardKind.Minion && card.Golden));
        }

        [Test]
        public void TimewarpedRefreshRewards_EliseCanGoldenPreservedFrozenHighestTierSlot()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_038");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var tavern = service.State.Player.Tavern;
            TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance>
            {
                TestBoardMinion("elise-frozen-high", "Frozen High Tier", "TEST_ELISE_FROZEN_HIGH", 5, 5, Tribe.Dragon, 6),
                TestBoardMinion("elise-open-low", "Open Low Tier", "TEST_ELISE_OPEN_LOW", 1, 1, Tribe.Beast, 1)
            });
            TavernShopSlots.SetSlotFrozen(tavern, 0, true);
            tavern.Gold = 10;
            tavern.MaxGold = 10;

            for (var index = 0; index < 5; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
                tavern.Gold = 10;
            }

            Assert.AreEqual("elise-frozen-high", tavern.Shop[0].InstanceId);
            Assert.IsTrue(tavern.Shop[0].Golden);
            Assert.IsTrue(TavernShopSlots.IsSlotFrozen(tavern, 0));
        }

        [Test]
        public void TimewarpedRefreshRewards_ThreeRefreshesGrantSpellcraftSpell()
        {
            var service = CreateTimewarpOnlyService(12345);
            BuyFixedTimewarpedCard(service, "BG34_Giant_322");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            for (var index = 0; index < 3; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
            }

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card =>
                card.Keywords.Contains(Keyword.Spellcraft) ||
                card.Tags.Any(tag => tag.IndexOf("spellcraft", System.StringComparison.OrdinalIgnoreCase) >= 0)));
        }

        [Test]
        public void TimewarpedSelectors_NalaaCountsAllAsATypeCandidate()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_205");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var allType = TestBoardMinion("nalaa-all", "All Type", "TEST_ALL_TYPE", 2, 2, Tribe.All, 1);
            var spellTarget = TestBoardMinion("nalaa-target", "Spell Target", "TEST_SPELL_TARGET", 2, 2, Tribe.Beast, 1);
            service.State.Player.Board.Add(allType);
            service.State.Player.Board.Add(spellTarget);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BLOOD_GEM", CardKind.Spell));
            var targetIndex = service.State.Player.Board.FindIndex(card => card.InstanceId == spellTarget.InstanceId);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, targetIndex));

            Assert.AreEqual(6, allType.Attack);
            Assert.AreEqual(5, allType.MaxHealth);
        }

        [Test]
        public void TimewarpedSelectors_CommanderCountsAllAsFriendlyNaga()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_210");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var allType = TestBoardMinion("commander-all", "All Type", "TEST_ALL_TYPE", 2, 2, Tribe.All, 1);
            var target = TestBoardMinion("commander-target", "Commander Target", "TEST_COMMANDER_TARGET", 1, 1, Tribe.Beast, 1);
            service.State.Player.Board.Add(allType);
            service.State.Player.Board.Add(target);
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            var expectedNagaCount = BoardTribeAnalyzer.CountTribe(service.State.Player.Board, Tribe.Naga);
            var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "BG34_Giant_210t");
            var targetIndex = service.State.Player.Board.FindIndex(card => card.InstanceId == target.InstanceId);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, targetIndex));

            Assert.AreEqual(1 + expectedNagaCount * 2, target.Attack);
            Assert.AreEqual(1 + expectedNagaCount * 2, target.MaxHealth);
        }

        [Test]
        public void TimewarpedShopReplace_SummonerTransformsShopMinionsAndPreservesFrozenSlot()
        {
            var service = CreateTimewarpOnlyService(12345);
            var targetIndex = PrepareTimewarpedSummonerSpell(service);
            var tavern = service.State.Player.Tavern;
            TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance>
            {
                TestBoardMinion("summoner-frozen", "Frozen Target", "TEST_FROZEN", 1, 1, Tribe.Pirate, 1),
                TestBoardMinion("summoner-open", "Open Target", "TEST_OPEN", 1, 1, Tribe.Murloc, 1)
            });
            TavernShopSlots.SetSlotFrozen(tavern, 0, true);
            var spellIndex = tavern.Hand.FindIndex(card => card.CardId == "TIMEWARPED_SUMMONER_SPELL");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, targetIndex));

            Assert.IsTrue(tavern.Shop.Where(card => card.CardKind == CardKind.Minion).All(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Beast)));
            Assert.IsTrue(TavernShopSlots.IsSlotFrozen(tavern, 0));
            Assert.AreNotEqual("summoner-frozen", tavern.Shop[0].InstanceId);
        }

        [Test]
        public void TimewarpedShopReplace_SummonerLeavesTavernSpellSlotsUnchanged()
        {
            var service = CreateTimewarpOnlyService(12345);
            var targetIndex = PrepareTimewarpedSummonerSpell(service);
            var tavern = service.State.Player.Tavern;
            var spellSlot = TestTavernSpell("summoner-spell-slot");
            TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance>
            {
                TestBoardMinion("summoner-minion", "Shop Minion", "TEST_SHOP_MINION", 1, 1, Tribe.Pirate, 1),
                spellSlot
            });
            var spellIndex = tavern.Hand.FindIndex(card => card.CardId == "TIMEWARPED_SUMMONER_SPELL");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, targetIndex));

            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(tavern.Shop[0], Tribe.Beast));
            Assert.AreEqual("summoner-spell-slot", tavern.Shop[1].InstanceId);
            Assert.AreEqual(CardKind.TavernSpell, tavern.Shop[1].CardKind);
        }

        [Test]
        public void TimewarpedShopReplace_SummonerTransformsExtraRefreshOffers()
        {
            var service = CreateTimewarpOnlyService(12345);
            var targetIndex = PrepareTimewarpedSummonerSpell(service);
            BuyFixedTimewarpedCard(service, "BG34_Giant_589");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var tavern = service.State.Player.Tavern;
            var extraOfferIndex = tavern.Shop.FindIndex(card => card != null && card.InstanceId.StartsWith("timewarped-raider-", System.StringComparison.Ordinal));
            Assert.GreaterOrEqual(extraOfferIndex, 0);
            var extraOfferTier = tavern.Shop[extraOfferIndex].TavernTier;
            var spellIndex = tavern.Hand.FindIndex(card => card.CardId == "TIMEWARPED_SUMMONER_SPELL");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, targetIndex));

            Assert.AreEqual(extraOfferTier, tavern.Shop[extraOfferIndex].TavernTier);
            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(tavern.Shop[extraOfferIndex], Tribe.Beast));
            Assert.IsFalse(tavern.Shop[extraOfferIndex].InstanceId.StartsWith("timewarped-raider-", System.StringComparison.Ordinal));
        }

        [Test]
        public void TimewarpedCopyTransform_CenturionCopiesTavernSpellOnlyThreeTimesPerTurn()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_PreMadeChamp_200");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "110412", CardKind.TavernSpell));

            for (var cast = 0; cast < 4; cast += 1)
            {
                var target = TestBoardMinion("centurion-target-" + cast, "Centurion Target", "CENTURION_TARGET_" + cast, 1, 1, Tribe.Undead, 1);
                service.State.Player.Board.Add(target);
                var spellIndex = service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "110412");
                var targetIndex = service.State.Player.Board.FindIndex(card => card.InstanceId == target.InstanceId);

                service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex, targetIndex));
            }

            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count(card => card.CardId == "110412"));
        }

        [Test]
        public void TimewarpedCopyTransform_ZerusDiscoversMinorTransformAndKeepsStats()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_671");
            var zerus = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG34_Giant_671");
            zerus.Attack = 9;
            zerus.MaxHealth = 11;
            zerus.Health = 11;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            StringAssert.StartsWith("timewarped-zerus:", service.State.Player.Tavern.Discover.Source);
            Assert.AreEqual(2, service.State.Player.Tavern.Discover.Options.Count);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            var transformed = service.State.Player.Tavern.Hand.Single();
            Assert.AreNotEqual("BG34_Giant_671", transformed.CardId);
            Assert.AreEqual(9, transformed.Attack);
            Assert.AreEqual(11, transformed.MaxHealth);
        }

        [Test]
        public void TimewarpedCopyTransform_LuckyEggHatchesIntoGoldenTierSevenChoice()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_683");
            var egg = service.State.Player.Tavern.Hand.Single(card => card.CardId == "BG34_Giant_683");

            Assert.IsTrue(egg.Tags.Contains("locked_in_hand"));
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsNull(service.State.Player.Tavern.Discover);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            StringAssert.StartsWith("timewarped-lucky-egg:", service.State.Player.Tavern.Discover.Source);
            Assert.AreEqual(3, service.State.Player.Tavern.Discover.Options.Count);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            var transformed = service.State.Player.Tavern.Hand.Single();
            Assert.AreNotEqual("BG34_Giant_683", transformed.CardId);
            Assert.IsTrue(transformed.Golden);
            Assert.GreaterOrEqual(transformed.TavernTier, 6);
        }

        [Test]
        public void TimewarpedCopyTransform_ChameleonCopiesLeftMinionAtCombatStart()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_042");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var left = TestBoardMinion("chameleon-left", "Left Target", "CHAMELEON_LEFT", 0, 20, Tribe.Mech, 3);
            left.Keywords.Add(Keyword.Taunt);
            service.State.Player.Board.Insert(0, left);
            var chameleonId = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_042").InstanceId;
            service.State.Opponent.Board.Add(TestBoardMinion("chameleon-enemy", "Enemy", "CHAMELEON_ENEMY", 0, 80, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 40, SafetyLimit = 1 }));

            var transformed = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == chameleonId);
            Assert.AreEqual("CHAMELEON_LEFT", transformed.CardId);
            Assert.AreEqual(20, transformed.MaxHealth);
            Assert.IsTrue(transformed.Keywords.Contains(Keyword.Taunt));
            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(transformed, Tribe.Mech));
        }

        [Test]
        public void TimewarpedCopyTransform_HenchmanCopiesSecondKilledEnemyAfterCombat()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_593");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var henchman = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_593");
            henchman.Attack = 20;
            henchman.Health = 20;
            henchman.MaxHealth = 20;
            henchman.Keywords.Add(Keyword.Windfury);
            var enemyA = TestBoardMinion("henchman-enemy-a", "Enemy A", "BG26_135", 0, 1, Tribe.Beast, 1);
            var enemyB = TestBoardMinion("henchman-enemy-b", "Enemy B", "BG26_135", 0, 1, Tribe.Beast, 1);
            service.State.Opponent.Board.Add(enemyA);
            service.State.Opponent.Board.Add(enemyB);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 41, SafetyLimit = 4 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BG26_135"));
        }

        [Test]
        public void TimewarpedCopyTransform_RiplashCopiesLastTavernSpellDeathrattle()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_325");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var riplash = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_325");
            riplash.Attack = 0;
            riplash.Health = 1;
            riplash.MaxHealth = 1;
            var target = TestBoardMinion("riplash-spell-target", "Spell Target", "RIPLASH_TARGET", 1, 1, Tribe.Undead, 1);
            service.State.Player.Board.Add(target);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "110412", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));
            service.State.Opponent.Board.Add(TestBoardMinion("riplash-enemy", "Enemy", "RIPLASH_ENEMY", 1, 20, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 42, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "110412"));
        }

        [Test]
        public void TimewarpedCurrentPool_BoarThirdDeathAddsGoldenBeast()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            for (var index = 0; index < 3; index += 1)
            {
                service.State.Player.Board.Add(TestBoardMinion("boar-" + index, "Timewarped Boar", "BG34_Giant_201", 0, 1, Tribe.Beast, 3));
                service.State.Opponent.Board.Add(TestOpponentMinion("boar-enemy-" + index, "Boar Enemy", "BOAR_ENEMY", 1, 10, Tribe.None, 1));
            }

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 51, SafetyLimit = 8 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.Golden && BoardTribeAnalyzer.HasTribe(card, Tribe.Beast)));
        }

        [Test]
        public void TimewarpedCurrentPool_WinnerSurvivesCombatAndGrantsTripleRewardNextTurn()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_039");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var winner = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_039");
            winner.Attack = 1;
            winner.Health = 20;
            winner.MaxHealth = 20;
            service.State.Opponent.Board.Add(TestOpponentMinion("winner-enemy", "Winner Enemy", "WINNER_ENEMY", 0, 20, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 52, SafetyLimit = 1 }));
            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.DefinitionId == "triple-reward"));
        }

        [Test]
        public void TimewarpedCurrentPool_MothershipAvengeFourAddsProtossReward()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_598");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var mothership = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_598");
            mothership.Attack = 0;
            mothership.Health = 30;
            mothership.MaxHealth = 30;
            for (var index = 0; index < 4; index += 1)
            {
                service.State.Player.Board.Add(TestBoardMinion("mothership-token-" + index, "Token", "MOTHERSHIP_TOKEN", 0, 1, Tribe.None, 1));
                service.State.Opponent.Board.Add(TestOpponentMinion("mothership-enemy-" + index, "Enemy", "MOTHERSHIP_ENEMY", 1, 10, Tribe.None, 1));
            }

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 53, SafetyLimit = 12 }));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.Tags.Contains("protoss_reward")));
        }

        [Test]
        public void TimewarpedCurrentPool_LavaLurkerCopiesTwoSpellcraftsPermanently()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_678");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            tavern.Tier = 2;
            var lava = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_678");
            service.State.Player.Board.Add(TestBoardMinion("lava-other-target", "Other Target", "LAVA_OTHER_TARGET", 1, 1, Tribe.Beast, 1));
            var beforeAttack = lava.Attack;
            var beforeHealth = lava.MaxHealth;

            for (var cast = 0; cast < 3; cast += 1)
            {
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, "REEF_RIFFER_SPELL", CardKind.Spell));
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));
            }

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(beforeAttack + 4, lava.Attack);
            Assert.AreEqual(beforeHealth + 4, lava.MaxHealth);
        }

        [Test]
        public void TimewarpedCurrentPool_NineFrogsNinthBoughtMinionAddsSameTierTavernSpell()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_309");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            tavern.Gold = 10;
            tavern.MaxGold = 10;

            for (var index = 0; index < 9; index += 1)
            {
                var bought = TestBoardMinion("nine-frogs-buy-" + index, "Bought Minion", "NINE_FROGS_BUY", 1, 1, Tribe.Beast, 3);
                bought.Cost = 0;
                tavern.Gold = 10;
                TavernShopSlots.ReplaceShop(tavern, new List<MinionInstance> { bought });
                service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
                tavern.Hand.RemoveAll(card => card.InstanceId == bought.InstanceId);
            }

            Assert.IsTrue(tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.TavernTier == 3));
        }

        [Test]
        public void TimewarpedCurrentPool_ScoutImprovesEachTurnAndSellsTierSevenMinions()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_333");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var scoutId = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_333").InstanceId;

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.SellMinion, scoutId));

            Assert.IsTrue(tavern.Hand.Any(card => card.CardKind == CardKind.Minion && card.TavernTier == 7));
        }

        [Test]
        public void TimewarpedCurrentPool_SecretarySecondSpellcraftAddsTavernSpell()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_323");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Board.Add(TestBoardMinion("secretary-target", "Secretary Target", "SECRETARY_TARGET", 1, 1, Tribe.Beast, 1));

            for (var cast = 0; cast < 2; cast += 1)
            {
                service.Apply(new GameCommand(GameCommandType.AddCardToHand, "REEF_RIFFER_SPELL", CardKind.Spell));
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));
            }

            Assert.IsTrue(tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void TimewarpedCurrentPool_TrumpeterFifthElementalSoldAddsElemental()
        {
            var service = CreateTimewarpOnlyService(12345);
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_676");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            for (var index = 0; index < 5; index += 1)
            {
                service.State.Player.Board.Add(TestBoardMinion("trumpeter-elemental-" + index, "Elemental", "TRUMPETER_ELEMENTAL", 1, 1, Tribe.Elemental, 1));
            }

            foreach (var elementalId in service.State.Player.Board
                .Where(card => card.CardId == "TRUMPETER_ELEMENTAL")
                .Select(card => card.InstanceId)
                .ToList())
            {
                service.Apply(new GameCommand(GameCommandType.SellMinion, elementalId));
            }

            Assert.IsTrue(tavern.Hand.Any(card => BoardTribeAnalyzer.HasTribe(card, Tribe.Elemental)));
        }

        [Test]
        public void TimewarpedCurrentPool_WhirlOTronCopiedDeathrattleResolvesOnDeath()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            BuyFixedTimewarpedCard(service, "BG34_Giant_599");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var whirl = service.State.Player.Board.Single(card => card.CardId == "BG34_Giant_599");
            whirl.Attack = 0;
            whirl.Health = 1;
            whirl.MaxHealth = 1;
            var cordPuller = TestBoardMinion("cord-puller", "Cord Puller", "BG29_611", 0, 20, Tribe.Mech, 4);
            cordPuller.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Add(cordPuller);
            service.State.Opponent.Board.Add(TestOpponentMinion("whirl-enemy", "Whirl Enemy", "WHIRL_ENEMY", 1, 20, Tribe.None, 1));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 54, SafetyLimit = 2 }));

            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(card => card.Name == "Microbot"));
        }

        [Test]
        public void TimewarpedSpecial_AcolyteSpinsYoggWheelAtTurnStart()
        {
            var service = CreateTimewarpOnlyService(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_591");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var logBefore = service.State.Player.Tavern.RecruitLog.Count;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(service.State.Player.Tavern.RecruitLog
                .Skip(logBefore)
                .Any(entry => entry.Message.Contains("命运之轮")));
        }

        [Test]
        public void TimewarpedSpecial_LeiGetsBuddyOfCurrentHeroPowerAtTurnStart()
        {
            var service = CreateTimewarpOnlyService(12345);
            var expectedBuddyId = service.HeroCatalog.AllHeroes
                .First(hero => hero.HeroPower != null && hero.Buddy != null && hero.HeroPower.CardId == service.State.Player.HeroPowerCardId)
                .Buddy
                .CardId;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_602");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            var buddy = service.State.Player.Tavern.Hand.FirstOrDefault(card => card.CardId == expectedBuddyId);
            Assert.IsNotNull(buddy);
            Assert.AreEqual(CardKind.HeroBuddy, buddy.CardKind);
            Assert.AreEqual(PoolSource.Copy, buddy.PoolSource);
            Assert.IsTrue(buddy.Tags.Contains("generated_copy"));
        }

        [Test]
        public void CreateWithSetup_FiltersShopAcrossInitialRerollAndNextTurn()
        {
            var active = new List<Tribe>
            {
                Tribe.Beast,
                Tribe.Murloc,
                Tribe.Mech,
                Tribe.Demon,
                Tribe.Dragon
            };
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions { ActiveTribes = active });

            CollectionAssert.AreEqual(active, service.State.ActiveTribes);
            AssertShopMatchesActiveTribes(service);

            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 10));
            for (var reroll = 0; reroll < 5; reroll += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
                AssertShopMatchesActiveTribes(service);
            }

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            AssertShopMatchesActiveTribes(service);
        }

        [Test]
        public void CreateWithSetup_CardPoolVersionFiltersShopAcrossInitialRerollAndNextTurn()
        {
            var minion = MinionCatalogLoader.LoadFromResources().All
                .First(card => card.InPool && card.TavernTier == 1 && card.PoolCount > 0 && !card.CardId.StartsWith("BGDUO"));
            var spell = SpellCatalogLoader.LoadFromResources().All
                .First(card => card.InPool && card.Category == "TavernSpell" && card.TavernTier <= 1);
            var setup = new MatchSetupOptions
            {
                ActiveTribes = TribeAvailabilityRules.AllPlayableTribes(),
                CardPoolVersionId = "test-version",
                CardPoolVersionName = "测试版本",
                IsDefaultCardPoolVersion = false,
                EnabledMinionCardIds = new List<string> { minion.CardId },
                EnabledTavernSpellCardNumbers = new List<string> { spell.CardNumber }
            };
            var service = MatchService.CreateWithDefaultCatalog(12345, null, setup);

            Assert.AreEqual("test-version", service.State.CardPoolVersionId);
            Assert.AreEqual("测试版本", service.State.CardPoolVersionName);
            Assert.IsFalse(service.State.IsDefaultCardPoolVersion);
            AssertShopMatchesCardPool(service, minion.CardId, spell.CardNumber);

            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 10));
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            AssertShopMatchesCardPool(service, minion.CardId, spell.CardNumber);

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            AssertShopMatchesCardPool(service, minion.CardId, spell.CardNumber);
        }

        [Test]
        public void Apply_TavernSpellDiscoverRespectsActiveTribes()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                null,
                new MatchSetupOptions { ActiveTribes = new List<Tribe> { Tribe.Beast } });
            service.State.Player.Tavern.Tier = TavernRules.MaxTavernTier;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG28_550", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.Greater(service.State.Player.Tavern.Discover.Options.Count, 0);
            foreach (var option in service.State.Player.Tavern.Discover.Options)
            {
                AssertTavernSpellMatchesActiveTribes(option, service.State.ActiveTribes);
            }
        }

        [Test]
        public void Apply_RerollShopKeepsRightmostSlotAsTierEligibleTavernSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 10));

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var shop = service.State.Player.Tavern.Shop;
            Assert.AreEqual(TavernRules.GetShopSize(1) + 1, shop.Count);
            Assert.AreEqual(CardKind.TavernSpell, shop.Last().CardKind);
            Assert.LessOrEqual(shop.Last().TavernTier, 1);
            Assert.AreEqual(TavernRules.GetShopSize(1), shop.Count(card => card.CardKind == CardKind.Minion));
        }

        [Test]
        public void Apply_RerollShopDrawsSpellFromCurrentTierOrLower()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Tier = 4;
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 10));

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var spell = service.State.Player.Tavern.Shop.Last();
            Assert.AreEqual(CardKind.TavernSpell, spell.CardKind);
            Assert.GreaterOrEqual(spell.TavernTier, 1);
            Assert.LessOrEqual(spell.TavernTier, 4);
        }

        [Test]
        public void Apply_RerollShopAppliesGlobalShopGrowthToMatchingMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
            {
                Scope = BuffScope.ShopGlobal,
                Tribe = Tribe.All,
                Attack = 2,
                Health = 2,
                SourceId = "test-global-shop"
            });
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 10));

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var minions = service.State.Player.Tavern.Shop.Where(card => card.CardKind == CardKind.Minion).ToList();
            Assert.IsTrue(minions.Count > 0);
            Assert.IsTrue(minions.All(card => card.Attack >= card.BaseAttack + 2));
            Assert.IsTrue(minions.All(card => card.MaxHealth >= card.BaseHealth + 2));
        }

        [Test]
        public void Apply_PlayingTavernSpellDoesNotPutItOnBoard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var spell = service.State.Player.Tavern.Shop.Last();
            service.State.Player.Tavern.Hand.Add(spell.Clone());

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.IsFalse(service.State.Player.Board.Any(card => card.CardKind == CardKind.TavernSpell));
            Assert.IsFalse(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void Apply_BuyingTavernSpellUsesSpellCost()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var spellIndex = service.State.Player.Tavern.Shop.Count - 1;
            service.State.Player.Tavern.Shop[spellIndex].Cost = 1;
            service.State.Player.Tavern.Gold = 3;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, spellIndex));

            Assert.AreEqual(2, service.State.Player.Tavern.Gold);
            Assert.AreEqual(CardKind.TavernSpell, service.State.Player.Tavern.Hand.Last().CardKind);
        }

        [Test]
        public void Apply_TierOneBattlecriesResolveSpecificEffects()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG20_100", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG31_330", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            Assert.AreEqual(1, service.State.Player.Tavern.NextTavernSpellCostReduction);

            var spellIndex = service.State.Player.Tavern.Shop.Count - 1;
            service.State.Player.Tavern.Shop[spellIndex].Cost = 1;
            service.State.Player.Tavern.Gold = 0;
            service.Apply(new GameCommand(GameCommandType.BuyMinion, spellIndex));

            Assert.AreEqual(0, service.State.Player.Tavern.Gold);
            Assert.AreEqual(0, service.State.Player.Tavern.NextTavernSpellCostReduction);
        }

        [Test]
        public void Apply_GeneratedCardEntrypointsAddExpectedCards()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG23_002", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));

            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG20_100", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));

            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG22_202", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.SellMinion, service.State.Player.Board.Single(card => card.CardId == "BG22_202").InstanceId));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion && BoardTribeAnalyzer.HasTribe(card, Tribe.Murloc)));

            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG33_894", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void Apply_TierOneSellEffectsGrantCorrectCards()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG20_301", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.SellMinion, service.State.Player.Board[0].InstanceId));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG33_140", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.Apply(new GameCommand(GameCommandType.SellMinion, service.State.Player.Board[0].InstanceId));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion && card.TavernTier == 1));
        }

        [Test]
        public void Apply_TierOneTriggeredMinionsTrackCountersAndDelayedGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.Apply(new GameCommand(GameCommandType.DebugAddGold, 20));

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_801", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var trogg = service.State.Player.Board[0];

            for (var index = 0; index < service.State.Player.Tavern.Shop.Count; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.BuyMinion, index));
            }

            Assert.AreEqual(trogg.BaseAttack + 4, trogg.Attack);
            Assert.AreEqual(trogg.BaseHealth + 4, trogg.MaxHealth);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_135", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(5, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void Apply_TierOneDemonAndDevourEffectsResolve()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var startingHealth = service.State.Player.Health;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_004", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var weaver = service.State.Player.Board[0];

            Assert.AreEqual(3, weaver.Attack);
            Assert.AreEqual(5, weaver.MaxHealth);
            Assert.AreEqual(startingHealth - 1, service.State.Player.Health);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG24_009", CardKind.Minion));
            var shopMinionsBefore = service.State.Player.Tavern.Shop.Count(card => card != null && card.CardKind == CardKind.Minion);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            var picky = service.State.Player.Board.Last();

            Assert.Less(service.State.Player.Tavern.Shop.Count(card => card != null && card.CardKind == CardKind.Minion), shopMinionsBefore);
            Assert.Greater(picky.Attack, picky.BaseAttack);
            Assert.AreEqual(startingHealth - 2, service.State.Player.Health);
        }

        [Test]
        public void Apply_TierOneCombatStartSummonsFlightyScoutFromHand()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG32_330", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 7, SafetyLimit = 20 }));

            Assert.AreEqual(1, service.State.LastResult.FinalPlayerBoard.Count);
            Assert.AreEqual("BG32_330", service.State.LastResult.FinalPlayerBoard[0].CardId);
        }

        [Test]
        public void Apply_TierTwoBattlecriesAndGeneratedSpellsResolve()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG23_002", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));

            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG27_002", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.Spell && card.CardId == "SLIMY_SHIELD"));

            var target = service.State.Player.Board.First();
            var beforeHealth = target.MaxHealth;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "SLIMY_SHIELD")));
            Assert.AreEqual(beforeHealth + 1, target.MaxHealth);
            Assert.IsTrue(target.Keywords.Contains(Keyword.Taunt));
        }

        [Test]
        public void Apply_TierTwoSellAndPlayTriggersResolve()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Gold = 0;
            service.State.Player.Tavern.MaxGold = 10;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_049", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.SellMinion, service.State.Player.Board[0].InstanceId));
            Assert.AreEqual(3, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG31_816", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            var boardTarget = service.State.Player.Board.First();
            var attackBefore = boardTarget.Attack;
            service.Apply(new GameCommand(GameCommandType.SellMinion, service.State.Player.Board.Last().InstanceId));
            Assert.AreEqual(attackBefore + 1, boardTarget.Attack);
            Assert.AreEqual(1, service.State.Player.Tavern.FutureBallerAttackBonus);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG20_203", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG20_100", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Spell && card.CardId == "BLOOD_GEM"));
        }

        [Test]
        public void Apply_TierTwoGlobalAndCombatEffectsResolve()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var startingHealth = service.State.Player.Health;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_004", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_174", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            Assert.AreEqual(startingHealth - 1, service.State.Player.Health);
            Assert.Greater(service.State.Player.Board.Last().MaxHealth, service.State.Player.Board.Last().BaseHealth);

            service.State.Player.Board.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_805", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_800", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            var beastAttack = service.State.Player.Board.Last().Attack;
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 8, SafetyLimit = 10 }));
            Assert.AreEqual(beastAttack + 1, service.State.LastResult.FinalPlayerBoard.Last(card => card.CardId == "BG26_800").Attack);
        }

        [Test]
        public void Apply_ForestRoverCombatSummonsBuffedBeetle()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG31_801", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var rover = service.State.Player.Board[0];
            service.Apply(new GameCommand(GameCommandType.UpdateMinion, rover.InstanceId, new MinionPatch { Attack = 0, Health = 1, MaxHealth = 1 }));
            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, "BG26_135"));
            service.Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, service.State.Opponent.Board[0].InstanceId, new MinionPatch { Attack = 1, Health = 1, MaxHealth = 1 }));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 9, SafetyLimit = 10 }));

            var beetle = service.State.LastResult.FinalPlayerBoard.First(card => card.DefinitionId == "beetle");
            Assert.AreEqual(4, beetle.Attack);
            Assert.AreEqual(3, beetle.MaxHealth);
        }

        [Test]
        public void Apply_TierTwoCombatDeathrattleRewardsApplyAfterCombatTest()
        {
            var alarmist = MatchService.CreateWithDefaultCatalog(12345);
            RunRewardDeathrattleCombat(alarmist, "BG35_340");
            Assert.AreEqual(1, alarmist.State.Player.Tavern.NextTavernSpellCostReduction);

            var hunter = MatchService.CreateWithDefaultCatalog(12345);
            RunRewardDeathrattleCombat(hunter, "BG32_170");
            Assert.IsTrue(hunter.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Spell && card.CardId == "100596"));

            var bully = MatchService.CreateWithDefaultCatalog(12345);
            RunRewardDeathrattleCombat(bully, "BG35_432");
            var specialGemIndex = bully.State.Player.Tavern.Hand.FindIndex(card => card.CardKind == CardKind.Spell && card.CardId == "BRISTLEBACK_BLOOD_GEM");
            Assert.GreaterOrEqual(specialGemIndex, 0);

            bully.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));
            bully.State.Player.Board.Clear();
            bully.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG20_100", CardKind.Minion));
            bully.Apply(new GameCommand(GameCommandType.PlayMinion, bully.State.Player.Tavern.Hand.Count - 1));
            var target = bully.State.Player.Board[0];
            var beforeAttack = target.Attack;
            specialGemIndex = bully.State.Player.Tavern.Hand.FindIndex(card => card.CardKind == CardKind.Spell && card.CardId == "BRISTLEBACK_BLOOD_GEM");
            bully.Apply(new GameCommand(GameCommandType.PlayMinion, specialGemIndex));

            Assert.AreEqual(beforeAttack + 1, target.Attack);
            Assert.IsTrue(target.Keywords.Contains(Keyword.Taunt));
        }

        [Test]
        public void Apply_TarecgosaPermanentlyKeepsCombatBuffs()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG33_241", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG21_015", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            if (!service.State.Player.Board[0].Keywords.Contains(Keyword.Rally))
            {
                service.State.Player.Board[0].Keywords.Add(Keyword.Rally);
            }

            var tarecgosa = service.State.Player.Board[1];
            var beforeAttack = tarecgosa.Attack;
            var beforeHealth = tarecgosa.MaxHealth;
            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, "BG26_135"));
            service.Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, service.State.Opponent.Board[0].InstanceId, new MinionPatch { Attack = 0, Health = 20, MaxHealth = 20 }));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 51, SafetyLimit = 1 }));

            Assert.AreEqual(beforeAttack + 2, tarecgosa.Attack);
            Assert.AreEqual(beforeHealth + 2, tarecgosa.MaxHealth);
            Assert.IsTrue(tarecgosa.Enchantments.Any(enchantment => enchantment.SourceId == "Tarecgosa"));
        }

        [Test]
        public void Apply_TierTwoGlobalDeathAndSummonRecordsResolve()
        {
            var eternal = MatchService.CreateWithDefaultCatalog(12345);
            RunRewardDeathrattleCombat(eternal, "BG25_008");
            Assert.AreEqual(1, eternal.State.Player.Tavern.EternalKnightDeaths);
            eternal.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG25_008", CardKind.Minion));
            var knight = eternal.State.Player.Tavern.Hand.Last(card => card.CardId == "BG25_008");
            Assert.AreEqual(knight.BaseAttack + 4, knight.Attack);
            Assert.AreEqual(knight.BaseHealth + 2, knight.MaxHealth);

            var automaton = MatchService.CreateWithDefaultCatalog(12345);
            automaton.State.Player.Board.Clear();
            automaton.State.Player.Tavern.Hand.Clear();
            automaton.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG_TTN_401", CardKind.Minion));
            automaton.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            automaton.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG_TTN_401", CardKind.Minion));
            automaton.Apply(new GameCommand(GameCommandType.PlayMinion, automaton.State.Player.Tavern.Hand.Count - 1));
            Assert.IsTrue(automaton.State.Player.Board.Where(card => card.CardId == "BG_TTN_401").All(card => card.Attack == card.BaseAttack + 3));
            Assert.IsTrue(automaton.State.Player.Board.Where(card => card.CardId == "BG_TTN_401").All(card => card.MaxHealth == card.BaseHealth + 2));
        }

        [Test]
        public void Apply_OldSoulAndWinterfinnerCombatHandEffectsResolve()
        {
            var oldSoul = MatchService.CreateWithDefaultCatalog(12345);
            oldSoul.State.Player.Board.Clear();
            oldSoul.State.Opponent.Board.Clear();
            oldSoul.State.Player.Tavern.Hand.Clear();
            oldSoul.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG34_231", CardKind.Minion));
            oldSoul.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_135", CardKind.Minion));
            oldSoul.Apply(new GameCommand(GameCommandType.PlayMinion, oldSoul.State.Player.Tavern.Hand.Count - 1));
            oldSoul.Apply(new GameCommand(GameCommandType.UpdateMinion, oldSoul.State.Player.Board[0].InstanceId, new MinionPatch { Attack = 0, Health = 1, MaxHealth = 1 }));
            oldSoul.Apply(new GameCommand(GameCommandType.AddOpponentMinion, "BG26_135"));
            oldSoul.Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, oldSoul.State.Opponent.Board[0].InstanceId, new MinionPatch { Attack = 1, Health = 10, MaxHealth = 10 }));
            for (var count = 0; count < 15; count += 1)
            {
                oldSoul.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 61 + count, SafetyLimit = 1 }));
            }

            Assert.IsTrue(oldSoul.State.Player.Tavern.Hand.First(card => card.CardId == "BG34_231").Golden);

            var winterfinner = MatchService.CreateWithDefaultCatalog(12345);
            winterfinner.State.Player.Board.Clear();
            winterfinner.State.Opponent.Board.Clear();
            winterfinner.State.Player.Tavern.Hand.Clear();
            winterfinner.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG29_300", CardKind.Minion));
            winterfinner.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            winterfinner.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_135", CardKind.Minion));
            var handTarget = winterfinner.State.Player.Tavern.Hand[0];
            var beforeAttack = handTarget.Attack;
            var beforeHealth = handTarget.MaxHealth;
            winterfinner.Apply(new GameCommand(GameCommandType.AddOpponentMinion, "BG26_135"));
            winterfinner.Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, winterfinner.State.Opponent.Board[0].InstanceId, new MinionPatch { Attack = 1, Health = 20, MaxHealth = 20 }));

            winterfinner.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 71, SafetyLimit = 1 }));

            Assert.AreEqual(beforeAttack + 2, handTarget.Attack);
            Assert.AreEqual(beforeHealth + 1, handTarget.MaxHealth);
        }

        [Test]
        public void Apply_TierTwoSpellcraftSpellsGenerateAndResolveAsNormalSpells()
        {
            var reef = MatchService.CreateWithDefaultCatalog(12345);
            reef.State.Player.Tavern.Tier = 2;
            reef.State.Player.Board.Clear();
            reef.State.Player.Tavern.Hand.Clear();
            reef.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_501", CardKind.Minion));
            reef.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var target = reef.State.Player.Board[0];
            var beforeAttack = target.Attack;
            var beforeHealth = target.MaxHealth;

            reef.Apply(new GameCommand(GameCommandType.NextTurn));
            var reefSpellIndex = reef.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "REEF_RIFFER_SPELL");
            Assert.GreaterOrEqual(reefSpellIndex, 0);
            Assert.AreEqual(CardKind.Spell, reef.State.Player.Tavern.Hand[reefSpellIndex].CardKind);
            reef.Apply(new GameCommand(GameCommandType.PlayMinion, reefSpellIndex));

            Assert.AreEqual(beforeAttack + 2, target.Attack);
            Assert.AreEqual(beforeHealth + 2, target.MaxHealth);
            reef.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(beforeAttack, target.Attack);
            Assert.AreEqual(beforeHealth, target.MaxHealth);

            var lava = MatchService.CreateWithDefaultCatalog(12345);
            lava.State.Player.Tavern.Tier = 2;
            lava.State.Player.Board.Clear();
            lava.State.Player.Tavern.Hand.Clear();
            lava.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG23_009", CardKind.Minion));
            lava.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var lavaTarget = lava.State.Player.Board[0];
            var lavaBeforeAttack = lavaTarget.Attack;
            var lavaBeforeHealth = lavaTarget.MaxHealth;
            lava.Apply(new GameCommand(GameCommandType.AddCardToHand, "REEF_RIFFER_SPELL", CardKind.Spell));
            lava.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            lava.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(lavaBeforeAttack + 2, lavaTarget.Attack);
            Assert.AreEqual(lavaBeforeHealth + 2, lavaTarget.MaxHealth);

            var surf = MatchService.CreateWithDefaultCatalog(12345);
            surf.State.Player.Board.Clear();
            surf.State.Opponent.Board.Clear();
            surf.State.Player.Tavern.Hand.Clear();
            surf.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG27_004", CardKind.Minion));
            surf.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            surf.Apply(new GameCommand(GameCommandType.NextTurn));
            var surfSpellIndex = surf.State.Player.Tavern.Hand.FindIndex(card => card.CardId == "SURF_N_SURF_SPELL");
            Assert.GreaterOrEqual(surfSpellIndex, 0);
            surf.Apply(new GameCommand(GameCommandType.PlayMinion, surfSpellIndex));
            Assert.IsTrue(surf.State.Player.Board[0].Tags.Contains("surf_n_surf_crab"));

            surf.Apply(new GameCommand(GameCommandType.UpdateMinion, surf.State.Player.Board[0].InstanceId, new MinionPatch { Attack = 0, Health = 1, MaxHealth = 1 }));
            surf.Apply(new GameCommand(GameCommandType.AddOpponentMinion, "BG26_135"));
            surf.Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, surf.State.Opponent.Board[0].InstanceId, new MinionPatch { Attack = 1, Health = 10, MaxHealth = 10 }));
            surf.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 81, SafetyLimit = 1 }));

            var crab = surf.State.LastResult.FinalPlayerBoard.First(card => card.DefinitionId == "crab");
            Assert.AreEqual(3, crab.Attack);
            Assert.AreEqual(2, crab.MaxHealth);
            surf.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));
            Assert.IsFalse(surf.State.Player.Board[0].Tags.Contains("surf_n_surf_crab"));
        }

        [Test]
        public void Apply_BuyPlaySellRoundTripChangesGoldAndBoard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var played = service.State.Player.Board[0].InstanceId;
            service.Apply(new GameCommand(GameCommandType.SellMinion, played));

            Assert.AreEqual(1, service.State.Player.Tavern.Gold);
            Assert.AreEqual(0, service.State.Player.Board.Count);
            Assert.AreEqual(3, service.State.Player.Tavern.RecruitLog.Count);
        }

        [Test]
        public void Apply_UpdateMinionPatchChangesSelectedBoardMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var target = service.State.Player.Board[0];

            service.Apply(new GameCommand(
                GameCommandType.UpdateMinion,
                target.InstanceId,
                new MinionPatch { Attack = 11, Health = 7, MaxHealth = 9, Golden = true }));

            var updated = service.State.Player.Board[0];
            Assert.AreEqual(target.InstanceId, updated.InstanceId);
            Assert.AreEqual(11, updated.Attack);
            Assert.AreEqual(7, updated.Health);
            Assert.AreEqual(9, updated.MaxHealth);
            Assert.IsTrue(updated.Golden);
        }

        [Test]
        public void Apply_PlayingGoldenMinionGrantsRewardCardThatDiscoversNextAvailableTier()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Tier = 6;
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;

            var source = service.State.Player.Tavern.Shop.First(minion => minion != null);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Add(CloneForHand(source, "triple-a"));
            service.State.Player.Tavern.Hand.Add(CloneForHand(source, "triple-b"));
            service.State.Player.Tavern.Hand.Add(CloneForHand(source, "triple-c"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNull(service.State.Player.Tavern.Discover, "Triples should not discover until the reward card is played.");
            var goldenIndex = service.State.Player.Tavern.Hand.FindIndex(minion => minion.Golden);
            Assert.GreaterOrEqual(goldenIndex, 0, "Expected triple to create a golden minion in hand.");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, goldenIndex));

            var rewardIndex = service.State.Player.Tavern.Hand.FindIndex(minion => minion.DefinitionId == "triple-reward");
            Assert.GreaterOrEqual(rewardIndex, 0, "Playing a golden minion should add a triple reward card to hand.");
            Assert.IsNull(service.State.Player.Tavern.Discover, "Reward card should be played before discover appears.");

            var boardCountBeforeReward = service.State.Player.Board.Count;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, rewardIndex));

            Assert.AreEqual(boardCountBeforeReward, service.State.Player.Board.Count, "Reward card should resolve as a spell-like card, not a board minion.");
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(6, service.State.Player.Tavern.Discover.RewardTier);
            Assert.AreEqual(3, service.State.Player.Tavern.Discover.Options.Count);
            Assert.IsFalse(service.State.Player.Tavern.Discover.Options.Any(card => card.TavernTier == 7));
        }

        [Test]
        public void Apply_TripleRewardDiscoverCapsAtCurrentMaxTier()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Tier = 7;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Add(new MinionInstance
            {
                InstanceId = "reward-card",
                DefinitionId = "triple-reward",
                CardId = "TRIPLE_REWARD",
                Name = "Triple Reward",
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy
            });

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(6, service.State.Player.Tavern.Discover.RewardTier);
        }

        [Test]
        public void Apply_MoveMinionReturnsPlayerBoardMinionToHand()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var played = service.State.Player.Board[0];

            service.Apply(new GameCommand(GameCommandType.MoveMinion, played.InstanceId));

            Assert.AreEqual(0, service.State.Player.Board.Count);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(played.DefinitionId, service.State.Player.Tavern.Hand[0].DefinitionId);
        }

        [Test]
        public void Apply_PlayMinionWithTargetIndexInsertsAtBoardPosition()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var minions = service.State.Player.Tavern.Shop.Where(card => card.CardKind == CardKind.Minion).Take(2).ToList();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            var existing = CloneForBoard(minions[0], "existing-board");
            var played = CloneForHand(minions[1], "played-hand");
            service.State.Player.Board.Add(existing);
            service.State.Player.Tavern.Hand.Add(played);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(2, service.State.Player.Board.Count);
            Assert.AreEqual(played.DefinitionId, service.State.Player.Board[0].DefinitionId);
            Assert.AreEqual(existing.DefinitionId, service.State.Player.Board[1].DefinitionId);
        }

        [Test]
        public void Apply_MoveBoardMinionWithTargetIndexReordersPlayerBoard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            var first = CloneForBoard(source, "board-a");
            var second = CloneForBoard(source, "board-b");
            var third = CloneForBoard(source, "board-c");
            service.State.Player.Board.Add(first);
            service.State.Player.Board.Add(second);
            service.State.Player.Board.Add(third);

            service.Apply(new GameCommand(GameCommandType.MoveBoardMinion, first.InstanceId, 2));

            Assert.AreEqual(second.InstanceId, service.State.Player.Board[0].InstanceId);
            Assert.AreEqual(third.InstanceId, service.State.Player.Board[1].InstanceId);
            Assert.AreEqual(first.InstanceId, service.State.Player.Board[2].InstanceId);
        }

        [Test]
        public void Apply_TargetedSpellRecordsExplicitTargetAndBuffsThatMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("alpha", "Alpha", "ALPHA", 4, 4, Tribe.None, 1));
            service.State.Player.Board.Add(TestBoardMinion("beta", "Beta", "BETA", 5, 5, Tribe.Quilboar, 1));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BLOOD_GEM", CardKind.Spell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));

            Assert.AreEqual(4, service.State.Player.Board[0].Attack);
            Assert.AreEqual(6, service.State.Player.Board[1].Attack);
            Assert.That(service.State.Player.Tavern.RecruitLog.Last().Message, Does.Contain("-> Beta"));
        }

        [Test]
        public void Apply_InvalidExplicitGraverobberTargetDoesNotFallbackOrConsumeCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("undead", "Undead", "UNDEAD", 2, 2, Tribe.Undead, 3));
            service.State.Player.Board.Add(TestBoardMinion("beast", "Beast", "BEAST", 3, 3, Tribe.Beast, 3));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG28_303", CardKind.Minion));

            Assert.Throws<System.InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1)));

            Assert.AreEqual(2, service.State.Player.Board.Count);
            Assert.IsTrue(service.State.Player.Board.Any(minion => minion.InstanceId == "undead"));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "BG28_303"));
        }

        [Test]
        public void Apply_InvalidExplicitEyesTargetDoesNotFallbackOrConsumeCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Tier = 7;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("tier-five", "Tier Five", "TIER_FIVE", 5, 5, Tribe.None, 5));
            service.State.Player.Board.Add(TestBoardMinion("tier-four", "Tier Four", "TIER_FOUR", 4, 4, Tribe.Dragon, 4));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "100601", CardKind.TavernSpell));

            Assert.Throws<System.InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0)));

            Assert.IsFalse(service.State.Player.Board[0].Golden);
            Assert.IsFalse(service.State.Player.Board[1].Golden);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "100601"));
        }

        [Test]
        public void Apply_DestroyedSpellTargetDoesNotTriggerPufferquilFallback()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.Tavern.Tier = 7;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("puffer", "Pufferquil", "BG25_039", 2, 6, Tribe.Quilboar, 3));
            service.State.Player.Board.Add(TestBoardMinion("undead", "Undead", "UNDEAD", 2, 2, Tribe.Undead, 3));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "110412", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));

            Assert.IsFalse(service.State.Player.Board.Any(minion => minion.InstanceId == "undead"));
            Assert.IsFalse(service.State.Player.Board.Single(minion => minion.InstanceId == "puffer").Keywords.Contains(Keyword.Venomous));
        }

        [Test]
        public void Apply_ReplayingReturnedGoldenDoesNotGrantDuplicateTripleReward()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var source = service.State.Player.Tavern.Shop.First(minion => minion != null);
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Add(CloneForHand(source, "triple-a"));
            service.State.Player.Tavern.Hand.Add(CloneForHand(source, "triple-b"));
            service.State.Player.Tavern.Hand.Add(CloneForHand(source, "triple-c"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var goldenIndex = service.State.Player.Tavern.Hand.FindIndex(minion => minion.Golden);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, goldenIndex));
            var goldenOnBoard = service.State.Player.Board.First(minion => minion.Golden);

            service.Apply(new GameCommand(GameCommandType.MoveMinion, goldenOnBoard.InstanceId));
            var returnedGoldenIndex = service.State.Player.Tavern.Hand.FindIndex(minion => minion.Golden);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, returnedGoldenIndex));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(minion => minion.DefinitionId == "triple-reward"));
        }

        private static MatchService CreateTimewarpOnlyService(int seed)
        {
            return CreateTimewarpOnlyService(seed, null);
        }

        private static MatchService CreateTimewarpOnlyService(int seed, List<Tribe> activeTribes)
        {
            return MatchService.CreateWithDefaultCatalog(
                seed,
                null,
                new MatchSetupOptions
                {
                    AdvancedMechanicMode = AdvancedMechanicMode.Timewarp,
                    EnableTrinkets = false,
                    ActiveTribes = activeTribes ?? new List<Tribe>()
                });
        }

        private static void SeedSmartTimewarpDirection(MatchService service)
        {
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Add(TestBoardMinion("smart-beast-a", "Smart Beast A", "SMART_BEAST_A", 1, 1, Tribe.Beast, 1));
            service.State.Player.Board.Add(TestBoardMinion("smart-beast-b", "Smart Beast B", "SMART_BEAST_B", 1, 1, Tribe.Beast, 1));
            service.State.Player.Tavern.Hand.Add(TestBoardMinion("smart-hand-murloc", "Smart Hand Murloc", "SMART_HAND_MURLOC", 1, 1, Tribe.Murloc, 1));
        }

        private static List<TimewarpedTavernCardDefinition> CurrentTimewarpedOfferDefinitions(MatchService service)
        {
            var timewarp = service.State.Player.Tavern.Timewarp;
            var definitions = service.GetTimewarpedCandidateDefinitions(timewarp.PendingKind)
                .GroupBy(card => card.CardId)
                .ToDictionary(group => group.Key, group => group.First());
            return timewarp.Offers
                .Select(offer => definitions[offer.CardId])
                .ToList();
        }

        private static bool HasAnyTimewarpedConcreteTribe(TimewarpedTavernCardDefinition definition, params Tribe[] tribes)
        {
            var wanted = new HashSet<Tribe>(tribes);
            return ConcreteTimewarpedOfferTribes(definition).Any(wanted.Contains);
        }

        private static bool IsGenericTimewarpedOffer(TimewarpedTavernCardDefinition definition)
        {
            return !ConcreteTimewarpedOfferTribes(definition).Any();
        }

        private static List<Tribe> ConcreteTimewarpedOfferTribes(TimewarpedTavernCardDefinition definition)
        {
            if (definition?.Tribes == null)
            {
                return new List<Tribe>();
            }

            return definition.Tribes
                .Where(tribe => TribeAvailabilityRules.PlayableTribes.Contains(tribe))
                .Distinct()
                .ToList();
        }

        private static MinionInstance BuyFixedTimewarpedCard(MatchService service, string cardId)
        {
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            timewarp.VisitOpen = true;
            timewarp.Phase = TimewarpTavernPhase.Open;
            timewarp.PendingKind = TimewarpKind.Minor;
            timewarp.Chronum = 10;
            timewarp.Offers = new List<TimewarpedOfferSlot>
            {
                new TimewarpedOfferSlot { SlotId = "fixed-" + cardId, CardId = cardId, CardKind = CardKind.Minion, Cost = 0, Source = "test" }
            };

            service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0));
            var bought = tavern.Hand.Last(card => card.CardId == cardId);
            if (timewarp.VisitOpen)
            {
                service.Apply(new GameCommand(GameCommandType.ExitTimewarpedTavern));
            }

            return bought;
        }

        private static void BuyFixedTimewarpedOffer(MatchService service, string cardId, CardKind cardKind, int cost, int chronum)
        {
            var tavern = service.State.Player.Tavern;
            var timewarp = tavern.Timewarp;
            timewarp.VisitOpen = true;
            timewarp.Phase = TimewarpTavernPhase.Open;
            timewarp.PendingKind = TimewarpKind.Minor;
            timewarp.Chronum = chronum;
            timewarp.Offers = new List<TimewarpedOfferSlot>
            {
                new TimewarpedOfferSlot { SlotId = "fixed-" + cardId, CardId = cardId, CardKind = cardKind, Cost = cost, Source = "test" }
            };

            service.Apply(new GameCommand(GameCommandType.BuyTimewarpedTavernCard, 0));
            if (timewarp.VisitOpen)
            {
                service.Apply(new GameCommand(GameCommandType.ExitTimewarpedTavern));
            }
        }

        private static int PrepareTimewarpedSummonerSpell(MatchService service)
        {
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            BuyFixedTimewarpedCard(service, "BG34_Giant_324");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var target = TestBoardMinion("summoner-beast-target", "Beast Target", "TEST_BEAST_TARGET", 1, 1, Tribe.Beast, 1);
            service.State.Player.Board.Add(target);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "TIMEWARPED_SUMMONER_SPELL"));
            return service.State.Player.Board.FindIndex(card => card.InstanceId == target.InstanceId);
        }

        private static void AdvanceToRound(MatchService service, int round)
        {
            while (service.State.Round < round)
            {
                service.Apply(new GameCommand(GameCommandType.NextTurn));
            }
        }

        private static string TimewarpedFailureStateFingerprint(MatchService service)
        {
            var state = service.State;
            var player = state.Player;
            var tavern = player.Tavern;
            var advanced = tavern.AdvancedMechanics;
            var quests = advanced?.Quests;
            return string.Join("\n",
                state.Round + ":" + state.Phase,
                UnityEngine.JsonUtility.ToJson(player),
                DictionaryFingerprint("pool", tavern.Pool),
                DictionaryFingerprint("pool-capacities", tavern.PoolCapacities),
                DictionaryFingerprint("buddy-pool", tavern.BuddyPool),
                DictionaryFingerprint("buddy-capacities", tavern.BuddyPoolCapacities),
                DictionaryFingerprint("hero-counters", tavern.HeroEffectCounters),
                DictionaryFingerprint("advanced-counters", advanced?.Counters),
                DictionaryFingerprint("advanced-selections", advanced?.Selections),
                DictionaryFingerprint("quest-counters", quests?.RewardCounters),
                DictionaryFingerprint("quest-flags", quests?.RewardFlags),
                DictionaryFingerprint("hero-power-unlocks", player.ExtraHeroPowerUnlockRounds));
        }

        private static string TavernShopStateFingerprint(TavernState tavern)
        {
            return tavern.Frozen + "\n" +
                string.Join("|", tavern.Shop.Select(card => UnityEngine.JsonUtility.ToJson(card))) + "\n" +
                string.Join("|", tavern.ShopSlots.Select(slot => UnityEngine.JsonUtility.ToJson(slot)));
        }

        private static string DictionaryFingerprint<T>(string label, IDictionary<string, T> values)
        {
            return label + ":" + string.Join("|", (values ?? new Dictionary<string, T>())
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Key + "=" + pair.Value));
        }

        private static bool ContainsChinese(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Any(character => character >= '\u4e00' && character <= '\u9fff');
        }

        private static void AssertShopMatchesActiveTribes(MatchService service)
        {
            foreach (var card in service.State.Player.Tavern.Shop.Where(card => card != null))
            {
                if (card.CardKind == CardKind.Minion)
                {
                    AssertMinionMatchesActiveTribes(card, service.State.ActiveTribes);
                }
                else if (card.CardKind == CardKind.TavernSpell)
                {
                    AssertTavernSpellMatchesActiveTribes(card, service.State.ActiveTribes);
                }
            }
        }

        private static void AssertShopMatchesCardPool(MatchService service, string minionCardId, string tavernSpellCardNumber)
        {
            var minions = service.State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            Assert.Greater(minions.Count, 0);
            Assert.IsTrue(minions.All(card => card.CardId == minionCardId));

            var spells = service.State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.TavernSpell).ToList();
            Assert.AreEqual(1, spells.Count);
            Assert.AreEqual(tavernSpellCardNumber, spells[0].CardId);
        }

        private static void AssertMinionMatchesActiveTribes(MinionInstance card, IReadOnlyCollection<Tribe> activeTribes)
        {
            Assert.IsTrue(
                card.Tribes == null ||
                card.Tribes.Count == 0 ||
                card.Tribes.Contains(Tribe.None) ||
                card.Tribes.Contains(Tribe.All) ||
                card.Tribes.Any(activeTribes.Contains),
                card.Name + " should match the active tribe pool.");
        }

        private static void AssertTavernSpellMatchesActiveTribes(MinionInstance card, IReadOnlyCollection<Tribe> activeTribes)
        {
            var definition = SpellCatalogLoader.LoadFromResources().All
                .FirstOrDefault(spell => spell.CardNumber == card.CardId || spell.Id == card.DefinitionId);
            var tribes = TribeAvailabilityRules.SpellTribes(definition ?? new TavernSpellDefinition
            {
                CardNumber = card.CardId,
                Id = card.DefinitionId
            });

            Assert.IsTrue(
                tribes.Count == 0 || tribes.Any(activeTribes.Contains),
                card.Name + " should match the active tavern spell tribe pool.");
        }

        private static void RunRewardDeathrattleCombat(MatchService service, string cardId)
        {
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var source = service.State.Player.Board[0];
            service.Apply(new GameCommand(GameCommandType.UpdateMinion, source.InstanceId, new MinionPatch { Attack = 0, Health = 1, MaxHealth = 1 }));
            if (!service.State.Player.Board[0].Keywords.Contains(Keyword.Deathrattle))
            {
                service.State.Player.Board[0].Keywords.Add(Keyword.Deathrattle);
            }

            service.Apply(new GameCommand(GameCommandType.AddOpponentMinion, "BG26_135"));
            service.Apply(new GameCommand(GameCommandType.UpdateOpponentMinion, service.State.Opponent.Board[0].InstanceId, new MinionPatch { Attack = 1, Health = 10, MaxHealth = 10 }));
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 31, SafetyLimit = 1 }));
        }

        [Test]
        public void GrantSecondHeroPower_StoresUnlockRoundWithoutReplacingPrimary()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.HeroPowerCardId = PatchesHeroPowerId;
            service.State.Player.ExtraHeroPowerCardIds.Clear();
            service.State.Player.ExtraHeroPowerUnlockRounds.Clear();

            service.GrantSecondHeroPower(GeorgeHeroPowerId, "test", 5);

            Assert.AreEqual(PatchesHeroPowerId, service.State.Player.HeroPowerCardId);
            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, GeorgeHeroPowerId);
            Assert.AreEqual(5, service.State.Player.ExtraHeroPowerUnlockRounds[GeorgeHeroPowerId]);
        }

        [Test]
        public void Apply_UseHeroPowerTargetsExplicitSecondHeroPower()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.HeroPowerCardId = KraggHeroPowerId;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 10;
            service.GrantSecondHeroPower(BlackthornHeroPowerId, "test");

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId));

            Assert.AreEqual(KraggHeroPowerId, service.State.Player.HeroPowerCardId);
            Assert.AreEqual(9, service.State.Player.Tavern.Gold);
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == BloodGemCardId));
        }

        [Test]
        public void Apply_SecondHeroPowerCountersAreScopedPerHeroPower()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.HeroPowerCardId = KraggHeroPowerId;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 10;
            service.GrantSecondHeroPower(BlackthornHeroPowerId, "test");

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId));
            service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId));

            var counters = service.State.Player.Tavern.HeroEffectCounters;
            Assert.IsFalse(counters.ContainsKey("hero:blackthorn:round"));
            Assert.IsFalse(counters.ContainsKey("hero:blackthorn:uses"));
            Assert.AreEqual(service.State.Round, counters["hero-power:" + BlackthornHeroPowerId + ":hero:blackthorn:round"]);
            Assert.AreEqual(2, counters["hero-power:" + BlackthornHeroPowerId + ":hero:blackthorn:uses"]);
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId)));
        }

        [Test]
        public void Apply_SecondHeroPowerUnlockRoundBlocksUseUntilUnlocked()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.HeroPowerCardId = KraggHeroPowerId;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 10;
            service.GrantSecondHeroPower(BlackthornHeroPowerId, "test", 5);

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId)));

            AdvanceToRound(service, 5);
            service.State.Player.Tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == BloodGemCardId));
        }

        [Test]
        public void Apply_PrimaryHeroPowerUseLimitResetsEachTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.HeroPowerCardId = RakanishuHeroPowerId;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 10;

            Assert.IsTrue(service.CanUseHeroPower());
            Assert.AreEqual(1, service.GetHeroPowerUsesRemainingThisTurn());

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.IsFalse(service.CanUseHeroPower());
            Assert.AreEqual(0, service.GetHeroPowerUsesRemainingThisTurn());
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == LanternLightCardId));
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower)));

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.State.Player.Tavern.Gold = 10;

            Assert.IsTrue(service.CanUseHeroPower());
            Assert.AreEqual(1, service.GetHeroPowerUsesRemainingThisTurn());
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == LanternLightCardId));
        }

        [Test]
        public void Apply_PrimaryAndSecondHeroPowerUseLimitsAreIndependent()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.HeroPowerCardId = RakanishuHeroPowerId;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 10;
            service.GrantSecondHeroPower(BlackthornHeroPowerId, "test");

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(0, service.GetHeroPowerUsesRemainingThisTurn());
            Assert.AreEqual(2, service.GetHeroPowerUsesRemainingThisTurn(BlackthornHeroPowerId));

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId));
            service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == LanternLightCardId));
            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count(card => card.CardId == BloodGemCardId));
            Assert.AreEqual(0, service.GetHeroPowerUsesRemainingThisTurn(BlackthornHeroPowerId));
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower, -1, TargetZone.Unspecified, heroPowerCardId: BlackthornHeroPowerId)));
        }

        [Test]
        public void Apply_ReplacedHeroPowerGetsItsOwnUseBudget()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            service.State.Player.HeroPowerCardId = RakanishuHeroPowerId;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            service.State.Player.HeroPowerCardId = BlackthornHeroPowerId;

            Assert.AreEqual(2, service.GetHeroPowerUsesRemainingThisTurn());
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.AreEqual(0, service.GetHeroPowerUsesRemainingThisTurn());
            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count(card => card.CardId == BloodGemCardId));
        }

        private static MinionInstance CloneForHand(MinionInstance source, string suffix)
        {
            var clone = source.Clone();
            clone.InstanceId = "player-" + source.DefinitionId + "-" + suffix;
            clone.Owner = BoardSide.Player;
            clone.Golden = false;
            return clone;
        }

        private static MinionInstance CloneForBoard(MinionInstance source, string suffix)
        {
            var clone = CloneForHand(source, suffix);
            clone.InstanceId = "player-" + source.DefinitionId + "-" + suffix;
            clone.Owner = BoardSide.Player;
            return clone;
        }

        private static MinionInstance TestBoardMinion(string id, string name, string cardId, int attack, int health, Tribe tribe, int tavernTier)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = id,
                DefinitionId = id,
                CardId = cardId,
                Name = name,
                Attack = attack,
                BaseAttack = attack,
                Health = health,
                MaxHealth = health,
                BaseHealth = health,
                TavernTier = tavernTier,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                Owner = BoardSide.Player,
                CanAttack = true
            };
        }

        private static MinionInstance TestOpponentMinion(string id, string name, string cardId, int attack, int health, Tribe tribe, int tavernTier)
        {
            var minion = TestBoardMinion(id, name, cardId, attack, health, tribe, tavernTier);
            minion.Owner = BoardSide.Opponent;
            return minion;
        }

        private static MinionInstance TestTavernSpell(string id)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = id,
                DefinitionId = id,
                CardId = "104436",
                Name = "Test Tavern Spell",
                Cost = 0,
                Attack = 0,
                BaseAttack = 0,
                Health = 0,
                MaxHealth = 0,
                BaseHealth = 0,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string> { "tavern_spell" },
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }
    }
}
