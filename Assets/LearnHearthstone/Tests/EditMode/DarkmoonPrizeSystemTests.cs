using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DarkmoonPrizeSystemTests
    {
        [Test]
        public void DarkmoonPrizeCatalog_LoadsTierGroupsAndExplicitHandlers()
        {
            var catalog = DarkmoonPrizeCatalogLoader.LoadFromResources();

            Assert.AreEqual(33, catalog.All.Count);
            Assert.AreEqual(9, catalog.GetByTier(1).Count);
            Assert.AreEqual(9, catalog.GetByTier(2).Count);
            Assert.AreEqual(8, catalog.GetByTier(3).Count);
            Assert.AreEqual(7, catalog.GetByTier(4).Count);
            Assert.IsTrue(catalog.All.All(prize =>
                prize.ImplementationStatus == DarkmoonPrizeImplementationStatus.Implemented ||
                prize.ImplementationStatus == DarkmoonPrizeImplementationStatus.Proxy));
            Assert.IsTrue(catalog.GetByTier(3).All(prize => prize.ImplementationStatus == DarkmoonPrizeImplementationStatus.Implemented));
            Assert.IsTrue(catalog.All.All(prize => prize.DbfId > 0));
            Assert.AreEqual("CardImages/BGS_Treasures_034", catalog.GetByCardId("BGS_Treasures_034").ImagePath);
            Assert.IsFalse(string.IsNullOrEmpty(catalog.GetByCardId("BGS_Treasures_034").ImageUrl));
        }

        [Test]
        public void DarkmoonPrizeCatalog_LocalizesEveryEntryAndPreservesEnglishMode()
        {
            var chinese = DarkmoonPrizeCatalogLoader.LoadFromResources(false);
            var english = DarkmoonPrizeCatalogLoader.LoadFromResources(true);

            Assert.AreEqual(33, chinese.All.Count);
            Assert.IsTrue(chinese.All.All(prize => ContainsChinese(prize.Name) && ContainsChinese(prize.Text)));
            Assert.AreEqual("Pocket Change", english.GetByCardId("BGS_Treasures_001").Name);
            Assert.AreEqual("压袋零钱", chinese.GetByCardId("BGS_Treasures_001").Name);
            StringAssert.Contains("2张酒馆币", chinese.GetByCardId("BGS_Treasures_001").Text);

            var chineseService = MatchService.CreateWithDefaultCatalog(setup: new MatchSetupOptions { UseEnglish = false });
            var englishService = MatchService.CreateWithDefaultCatalog(setup: new MatchSetupOptions { UseEnglish = true });
            Assert.AreEqual("提高身价", chineseService.DarkmoonPrizeCatalog.GetByCardId("BGS_Treasures_016").Name);
            Assert.AreEqual("Raise the Stakes", englishService.DarkmoonPrizeCatalog.GetByCardId("BGS_Treasures_016").Name);
        }

        [Test]
        public void DarkmoonPrize_PocketChangeAddsTwoTavernCoins()
        {
            var catalog = DarkmoonPrizeCatalogLoader.LoadFromResources();
            var definition = catalog.GetByCardId("BGS_Treasures_001");
            var prize = DarkmoonPrizeEngine.CreatePrizeCard(definition, "test");
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            tavern.Hand.Add(prize);

            Assert.AreEqual(DarkmoonPrizeImplementationStatus.Implemented, definition.ImplementationStatus);
            Assert.IsFalse(string.IsNullOrEmpty(prize.ImagePath));
            Assert.IsTrue(prize.Tags.Contains("implemented_darkmoon_prize"));
            Assert.IsFalse(prize.Tags.Contains("darkmoon_prize_proxy"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(2, tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell && card.CardId == "104436"));
        }

        [Test]
        public void DarkmoonPrize_AddCardToHandUsesSharedCatalogForAnyTier()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_016", CardKind.Spell));

            Assert.AreEqual(1, tavern.Hand.Count);
            var prize = tavern.Hand[0];
            Assert.AreEqual(service.DarkmoonPrizeCatalog.GetByCardId("BGS_Treasures_016").Name, prize.Name);
            Assert.AreEqual(4, prize.TavernTier);
            Assert.AreEqual("CardImages/BGS_Treasures_016", prize.ImagePath);
            Assert.IsTrue(prize.Tags.Contains("darkmoon_prize"));
            Assert.IsTrue(prize.Tags.Contains("darkmoon_prize_tier_4"));
            Assert.IsFalse(prize.Tags.Contains("darkmoon_prize_proxy"));
            Assert.IsTrue(prize.Tags.Contains("implemented_darkmoon_prize"));
        }

        [Test]
        public void DarkmoonPrize_TrainingSessionDiscoversAndSwapsHeroPower()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_011", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(3, tavern.Discover.Options.Count);
            Assert.IsTrue(tavern.Discover.Options.All(option => option.CardKind == CardKind.HeroPower));
            Assert.IsTrue(tavern.Discover.Options.All(option => option.CardId != primaryHeroPower));

            var pickedCardId = tavern.Discover.Options[0].CardId;
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.AreEqual(pickedCardId, service.State.Player.HeroPowerCardId);
            Assert.AreNotEqual(primaryHeroPower, service.State.Player.HeroPowerCardId);
        }

        [Test]
        public void DarkmoonPrize_TopShelfDiscoversHigherTierMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            UpgradeToTier(service, 3);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_020", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(3, tavern.Discover.Options.Count);
            Assert.IsTrue(tavern.Discover.Options.All(option => option.CardKind == CardKind.Minion));
            Assert.IsTrue(tavern.Discover.Options.All(option => option.TavernTier == 4));

            var pickedCardId = tavern.Discover.Options[0].CardId;
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.IsNull(tavern.Discover);
            Assert.IsTrue(tavern.Hand.Any(card => card.CardId == pickedCardId && card.CardKind == CardKind.Minion));
        }

        [Test]
        public void DarkmoonPrize_RepeatCustomerReturnsFriendlyNonGoldenMinionToHandWithBuff()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            service.State.Player.Board.Add(TestMinion("repeat-customer-test", "REPEAT_CUSTOMER_TEST", 2, 3));
            var target = service.State.Player.Board[0];
            var attackBefore = target.Attack;
            var healthBefore = target.Health;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_034", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.IsEmpty(service.State.Player.Board);
            Assert.IsTrue(tavern.Hand.Any(card => card.CardId == "REPEAT_CUSTOMER_TEST" && card.CardKind == CardKind.Minion));
            var returned = tavern.Hand.First(card => card.CardId == "REPEAT_CUSTOMER_TEST" && card.CardKind == CardKind.Minion);
            Assert.AreEqual(attackBefore + 6, returned.Attack);
            Assert.AreEqual(healthBefore + 6, returned.Health);
            Assert.IsFalse(returned.Golden);
        }

        [Test]
        public void DarkmoonPrize_AllThatGlittersMakesATavernMinionGolden()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            tavern.Shop.Clear();
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.IsNotEmpty(tavern.Shop);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_037", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, tavern.Shop.Count(card => card != null && card.Golden));
        }

        [Test]
        public void DarkmoonPrize_MindflayerGogglesStealsShopAndRefreshes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            tavern.Shop.Clear();
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            var shopInstanceIds = tavern.Shop.Select(card => card.InstanceId).ToList();
            var shopCountBefore = tavern.Shop.Count;
            var handBefore = tavern.Hand.Count;

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_039", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.GreaterOrEqual(tavern.Hand.Count, handBefore + shopCountBefore);
            Assert.IsTrue(shopInstanceIds.All(instanceId => tavern.Hand.Any(card => card.InstanceId == instanceId)));
            Assert.IsNotEmpty(tavern.Shop);
        }

        [Test]
        public void DarkmoonPrize_ReservePricesDiscountsTavernSpellsForTheTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345);
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_Treasures_104", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "104436", CardKind.TavernSpell));

            var reservePrices = tavern.Hand.Single(card => card.CardId == "BGS_Treasures_104");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(reservePrices)));

            Assert.AreEqual(1, tavern.NextTavernSpellCostReduction);
        }

        [Test]
        public void DarkmoonPrize_P0DirectEffectsArePlayable()
        {
            var gacha = MatchService.CreateWithDefaultCatalog(12345);
            gacha.State.Player.Tavern.Hand.Clear();
            PlayDarkmoonPrize(gacha, "BGS_Treasures_004");
            Assert.AreEqual("darkmoon-gacha-gift", gacha.State.Player.Tavern.Discover.Source);
            Assert.IsTrue(gacha.State.Player.Tavern.Discover.Options.All(card => card.CardKind == CardKind.Minion && card.TavernTier == 1));

            var onTheHouse = MatchService.CreateWithDefaultCatalog(12345);
            onTheHouse.State.Player.Tavern.Hand.Clear();
            UpgradeToTier(onTheHouse, 3);
            PlayDarkmoonPrize(onTheHouse, "BGS_Treasures_012");
            Assert.AreEqual("darkmoon-on-the-house", onTheHouse.State.Player.Tavern.Discover.Source);
            Assert.IsTrue(onTheHouse.State.Player.Tavern.Discover.Options.All(card => card.CardKind == CardKind.Minion && card.TavernTier == 3));

            var might = MatchService.CreateWithDefaultCatalog(12345);
            might.State.Player.Tavern.Hand.Clear();
            might.State.Player.Tavern.Tier = 4;
            var mightTarget = TestMinion("might-target", "MIGHT_TARGET", 2, 3);
            might.State.Player.Board.Add(mightTarget);
            PlayDarkmoonPrize(might, "BGS_Treasures_007");
            Assert.AreEqual(6, mightTarget.Attack);
            Assert.AreEqual(3, mightTarget.MaxHealth);

            var freshTab = MatchService.CreateWithDefaultCatalog(12345);
            freshTab.State.Player.Tavern.Hand.Clear();
            freshTab.State.Player.Tavern.Gold = 0;
            PlayDarkmoonPrize(freshTab, "BGS_Treasures_025");
            Assert.AreEqual(12, freshTab.State.Player.Tavern.Gold);

            var bananaBunch = MatchService.CreateWithDefaultCatalog(12345);
            bananaBunch.State.Player.Tavern.Hand.Clear();
            PlayDarkmoonPrize(bananaBunch, "BGS_Treasures_040");
            Assert.AreEqual(2, bananaBunch.State.Player.Tavern.Hand.Count(card =>
                card.CardId == "MUKLA_BANANA" &&
                card.CardKind == CardKind.TavernSpell &&
                card.Tags.Contains("tavern_dish_banana")));

            var codex = MatchService.CreateWithDefaultCatalog(12345);
            codex.State.Player.Tavern.Hand.Clear();
            PlayDarkmoonPrize(codex, "BGS_Treasures_100");
            Assert.AreEqual(1, codex.State.Player.Tavern.Hand.Count(card =>
                card.CardKind == CardKind.TavernSpell &&
                card.Cost >= 2));

            var blossom = MatchService.CreateWithDefaultCatalog(12345);
            blossom.State.Player.Tavern.Hand.Clear();
            UpgradeToTier(blossom, 2);
            PlayDarkmoonPrize(blossom, "BGS_Treasures_101");
            Assert.AreEqual("darkmoon-mageroyal-blossom", blossom.State.Player.Tavern.Discover.Source);
            Assert.IsTrue(blossom.State.Player.Tavern.Discover.Options.All(card => card.CardKind == CardKind.TavernSpell && card.TavernTier == 2));

            var rat = MatchService.CreateWithDefaultCatalog(12345);
            rat.State.Player.Tavern.Hand.Clear();
            var ratTarget = TestMinion("rat-target", "RAT_TARGET", 3, 4);
            rat.State.Player.Board.Add(ratTarget);
            PlayDarkmoonPrize(rat, "BGS_Treasures_018", 0);
            Assert.AreEqual(10, ratTarget.Attack);

            var bouncer = MatchService.CreateWithDefaultCatalog(12345);
            bouncer.State.Player.Tavern.Hand.Clear();
            var bouncerTarget = TestMinion("bouncer-target", "BOUNCER_TARGET", 2, 5);
            bouncer.State.Player.Board.Add(bouncerTarget);
            PlayDarkmoonPrize(bouncer, "BGS_Treasures_026", 0);
            Assert.IsTrue(bouncerTarget.Keywords.Contains(Keyword.Taunt));
            Assert.AreEqual(10, bouncerTarget.MaxHealth);

            var dogBone = MatchService.CreateWithDefaultCatalog(12345);
            dogBone.State.Player.Tavern.Hand.Clear();
            var dogTarget = TestMinion("dog-target", "DOG_TARGET", 2, 3);
            dogBone.State.Player.Board.Add(dogTarget);
            PlayDarkmoonPrize(dogBone, "BGS_Treasures_028", 0);
            Assert.AreEqual(17, dogTarget.Attack);
            Assert.AreEqual(18, dogTarget.MaxHealth);
            Assert.IsTrue(dogTarget.Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(dogTarget.Keywords.Contains(Keyword.Windfury));
        }

        [Test]
        public void DarkmoonPrize_P1PersistentAndExistingStateEffectsArePlayable()
        {
            var goodStuff = MatchService.CreateWithDefaultCatalog(12345);
            goodStuff.State.Player.Tavern.Hand.Clear();
            var shopA = TestMinion("good-shop-a", "GOOD_SHOP_A", 2, 3);
            var shopB = TestMinion("good-shop-b", "GOOD_SHOP_B", 4, 5);
            goodStuff.State.Player.Tavern.Shop = new List<MinionInstance> { shopA, shopB };
            PlayDarkmoonPrize(goodStuff, "BGS_Treasures_013");
            Assert.AreEqual(3, shopA.Attack);
            Assert.AreEqual(4, shopA.MaxHealth);
            Assert.AreEqual(5, shopB.Attack);
            Assert.AreEqual(6, shopB.MaxHealth);
            Assert.IsTrue(goodStuff.State.Player.Tavern.Growth.ShopModifiers.Any(modifier => modifier.SourceId == "The Good Stuff"));

            var rocking = MatchService.CreateWithDefaultCatalog(12345);
            rocking.State.Player.Tavern.Hand.Clear();
            rocking.State.Player.Tavern.FreeRefreshes = 0;
            PlayDarkmoonPrize(rocking, "BGS_Treasures_029");
            rocking.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(1, rocking.State.Player.Tavern.FreeRefreshes);

            var newRecruit = MatchService.CreateWithDefaultCatalog(12345);
            newRecruit.State.Player.Tavern.Hand.Clear();
            newRecruit.State.Player.Tavern.Gold = 10;
            PlayDarkmoonPrize(newRecruit, "BGS_Treasures_033");
            newRecruit.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.GreaterOrEqual(newRecruit.State.Player.Tavern.Shop.Count(card => card != null), 7);
            Assert.IsTrue(newRecruit.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .All(card => card.Enchantments.Any(enchantment => enchantment.SourceId == "New Recruit")));

            var crystallization = MatchService.CreateWithDefaultCatalog(12345);
            crystallization.State.Player.Tavern.Hand.Clear();
            PlayDarkmoonPrize(crystallization, "BGS_Treasures_110");
            Assert.AreEqual(1, crystallization.State.Player.Tavern.TavernSpellBonusAttack);
            Assert.AreEqual(1, crystallization.State.Player.Tavern.TavernSpellBonusHealth);

            var evolving = MatchService.CreateWithDefaultCatalog(12345);
            evolving.State.Player.Tavern.Hand.Clear();
            evolving.State.Player.Tavern.Shop = new List<MinionInstance>
            {
                TestMinion("evolving-a", "EVOLVING_A", 1, 1, Tribe.None, 1),
                TestMinion("evolving-b", "EVOLVING_B", 2, 2, Tribe.None, 1)
            };
            PlayDarkmoonPrize(evolving, "BGS_Treasures_006");
            Assert.IsTrue(evolving.State.Player.Tavern.Shop.All(card => card == null || card.TavernTier == 2));

            var timeThief = MatchService.CreateWithDefaultCatalog(12345);
            timeThief.State.Player.Tavern.Hand.Clear();
            timeThief.State.OpponentHistory.LastOpponentWarband = new List<MinionInstance>
            {
                TestMinion("time-thief-a", "TIME_THIEF_A", 6, 7),
                TestMinion("time-thief-b", "TIME_THIEF_B", 8, 9)
            };
            PlayDarkmoonPrize(timeThief, "BGS_Treasures_010");
            Assert.AreEqual("darkmoon-time-thief", timeThief.State.Player.Tavern.Discover.Source);
            Assert.IsTrue(timeThief.State.Player.Tavern.Discover.Options.Any(card => card.CardId == "TIME_THIEF_A"));

            var raise = MatchService.CreateWithDefaultCatalog(12345);
            raise.State.Player.Tavern.Hand.Clear();
            raise.State.Player.Board.Add(TestMinion("raise-target", "RAISE_TARGET", 3, 4));
            PlayDarkmoonPrize(raise, "BGS_Treasures_016", 0);
            Assert.IsEmpty(raise.State.Player.Board);
            Assert.IsTrue(raise.State.Player.Tavern.Hand.Any(card => card.CardId == "RAISE_TARGET" && card.Golden));

            var goblet = MatchService.CreateWithDefaultCatalog(12345);
            goblet.State.Player.Tavern.Hand.Clear();
            PlayDarkmoonPrize(goblet, "BGS_Treasures_106");
            Assert.AreEqual(10, goblet.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(goblet.State.Player.Tavern.Hand.All(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void DarkmoonPrize_P2OrderingSensitiveEffectsArePlayable()
        {
            var gruul = MatchService.CreateWithDefaultCatalog(12345);
            gruul.State.Player.Tavern.Hand.Clear();
            var gruulTarget = TestMinion("gruul-target", "GRUUL_TARGET", 2, 3);
            gruul.State.Player.Board.Add(gruulTarget);
            PlayDarkmoonPrize(gruul, "BGS_Treasures_009", 0);
            gruul.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(6, gruulTarget.Attack);
            Assert.AreEqual(7, gruulTarget.MaxHealth);

            var unlimited = MatchService.CreateWithDefaultCatalog(12345);
            unlimited.State.Player.Tavern.Hand.Clear();
            unlimited.State.Player.Tavern.Gold = 0;
            PlayDarkmoonPrize(unlimited, "BGS_Treasures_014");
            Assert.AreEqual(1, unlimited.State.Player.Tavern.Gold);
            unlimited.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsTrue(unlimited.State.Player.Tavern.Hand.Any(card => card.CardId == "BGS_Treasures_014"));

            var brann = MatchService.CreateWithDefaultCatalog(12345);
            brann.State.Player.Tavern.Hand.Clear();
            PlayDarkmoonPrize(brann, "BGS_Treasures_030");
            var battlecry = TestMinion("brann-battlecry", "BGS_116", 0, 10, Tribe.None, 4, Keyword.Battlecry);
            brann.State.Player.Tavern.FreeRefreshes = 0;
            brann.State.Player.Tavern.Hand.Add(battlecry);
            brann.Apply(new GameCommand(GameCommandType.PlayMinion, brann.State.Player.Tavern.Hand.IndexOf(battlecry)));
            Assert.AreEqual(4, brann.State.Player.Tavern.FreeRefreshes);
            brann.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsFalse(brann.State.Player.Tavern.AdvancedMechanics.Counters.ContainsKey("darkmoon:big_brann_play:extra_battlecries_this_turn") &&
                brann.State.Player.Tavern.AdvancedMechanics.Counters["darkmoon:big_brann_play:extra_battlecries_this_turn"] > 0);

            var discount = MatchService.CreateWithDefaultCatalog(12345);
            discount.State.Player.Tavern.Hand.Clear();
            discount.State.Player.Tavern.Shop = new List<MinionInstance> { TestMinion("discount-shop", "DISCOUNT_SHOP", 2, 2) };
            discount.State.Player.Tavern.Gold = 2;
            PlayDarkmoonPrize(discount, "BGS_Treasures_022");
            discount.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(0, discount.State.Player.Tavern.Gold);
            Assert.IsTrue(discount.State.Player.Tavern.Hand.Any(card => card.CardId == "DISCOUNT_SHOP"));

            var openBar = MatchService.CreateWithDefaultCatalog(12345);
            openBar.State.Player.Tavern.Hand.Clear();
            openBar.State.Player.Tavern.FreeRefreshes = 0;
            PlayDarkmoonPrize(openBar, "BGS_Treasures_023");
            Assert.AreEqual(5, openBar.State.Player.Tavern.FreeRefreshes);
            openBar.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(10, openBar.State.Player.Tavern.FreeRefreshes);

            var bigWinner = MatchService.CreateWithDefaultCatalog(12345);
            bigWinner.State.Player.Tavern.Hand.Clear();
            PlayDarkmoonPrize(bigWinner, "BGS_Treasures_032");
            AssertDarkmoonPrizeDiscover(bigWinner.State.Player.Tavern, "darkmoon-big-winner-tier-1", 1, false);
            bigWinner.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            AssertDarkmoonPrizeDiscover(bigWinner.State.Player.Tavern, "darkmoon-big-winner-tier-2", 2, false);
            bigWinner.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            AssertDarkmoonPrizeDiscover(bigWinner.State.Player.Tavern, "darkmoon-big-winner-tier-3", 3, false);
        }

        private static void UpgradeToTier(MatchService service, int tier)
        {
            while (service.State.Player.Tavern.Tier < tier)
            {
                service.State.Player.Tavern.Gold = 100;
                service.Apply(new GameCommand(GameCommandType.UpgradeTavern));
            }
        }

        private static void AssertDarkmoonPrizeDiscover(TavernState tavern, string source, int tier, bool expectProxy)
        {
            Assert.IsNotNull(tavern.Discover);
            Assert.AreEqual(source, tavern.Discover.Source);
            Assert.AreEqual(tier, tavern.Discover.RewardTier);
            Assert.AreEqual(3, tavern.Discover.Options.Count);
            Assert.IsTrue(tavern.Discover.Options.All(card =>
                card.CardKind == CardKind.Spell &&
                card.TavernTier == tier &&
                card.PoolSource == PoolSource.Discover &&
                card.PoolCopiesHeld == 0 &&
                card.Tags.Contains("darkmoon_prize") &&
                card.Tags.Contains("darkmoon_prize_tier_" + tier)));
            if (!expectProxy)
            {
                Assert.IsTrue(tavern.Discover.Options.All(card => !card.Tags.Contains("darkmoon_prize_proxy")));
            }
        }

        private static void PlayDarkmoonPrize(MatchService service, string cardId, int targetIndex = -1)
        {
            var tavern = service.State.Player.Tavern;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Spell));
            var handIndex = tavern.Hand.FindIndex(card => card.CardId == cardId);
            Assert.GreaterOrEqual(handIndex, 0, "Expected Darkmoon Prize in hand: " + cardId);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, handIndex, targetIndex));
        }

        private static MinionInstance TestMinion(string instanceId, string definitionId)
        {
            return TestMinion(instanceId, definitionId, 2, 3);
        }

        private static MinionInstance TestMinion(
            string instanceId,
            string definitionId,
            int attack,
            int health,
            Tribe tribe = Tribe.None,
            int tavernTier = 1,
            params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = definitionId,
                CardId = definitionId.ToUpperInvariant(),
                Name = "Darkmoon Prize Test",
                BaseAttack = attack,
                Attack = attack,
                BaseHealth = health,
                Health = health,
                MaxHealth = health,
                TavernTier = tavernTier,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }

        private static bool ContainsChinese(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Any(character => character >= '\u3400' && character <= '\u9fff');
        }
    }
}
