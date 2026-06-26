using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class HeroPowerBuddyEffectTests
    {
        [Test]
        public void Cenarius_ActiveHeroPowerIncreasesMaxGoldAndMalorneScales()
        {
            var service = CreateHeroService("BG32_HERO_001");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.MaxGold = 10;
            PlayBuddy(service, "BG32_HERO_001_Buddy");

            var malorne = service.State.Player.Board.Single(card => card.CardId == "BG32_HERO_001_Buddy");

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(11, service.State.Player.Tavern.MaxGold);
            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
            Assert.AreEqual(2, malorne.Attack);
            Assert.AreEqual(2, malorne.MaxHealth);
        }

        [Test]
        public void Kaelthas_ThirdBoughtMinionGrantsCoinAndBuffsBuddy()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_60");
            service.State.Player.Tavern.Gold = 10;
            PlayBuddy(service, "TB_BaconShop_HERO_60_Buddy");
            var buddy = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_60_Buddy");
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("buy-one", "BUY_ONE", 1, 2),
                TestMinion("buy-two", "BUY_TWO", 3, 4),
                TestMinion("buy-three", "BUY_THREE", 5, 6)
            };

            BuyFirstShopMinion(service);
            BuyFirstShopMinion(service);
            var third = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
            var expectedAttack = buddy.Attack + third.Attack;
            var expectedHealth = buddy.MaxHealth + third.MaxHealth;
            BuyFirstShopMinion(service);

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "104436"));
            Assert.AreEqual(expectedAttack, buddy.Attack);
            Assert.AreEqual(expectedHealth, buddy.MaxHealth);
        }

        [Test]
        public void Varden_RefreshCopiesHighestTierMinionAndBuddyBuffsBoth()
        {
            var service = CreateHeroService("BG22_HERO_004");
            service.State.Player.Tavern.Gold = 10;
            PlayBuddy(service, "BG22_HERO_004_Buddy");

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var buffed = service.State.Player.Tavern.Shop
                .Where(card => card != null && card.Enchantments.Any(enchantment => enchantment.SourceId == "Varden's Aquarrior"))
                .ToList();
            Assert.IsTrue(service.State.Player.Tavern.Frozen);
            Assert.AreEqual(2, buffed.Count);
            Assert.IsTrue(service.State.Player.Tavern.Shop.Any(card => card != null && card.Tags.Contains("frozen_by_varden")));
        }

        [Test]
        public void Othaar_StartOfTurnDiscountAndCelestialArchiveCopiesZeroCostSpell()
        {
            var service = CreateHeroService("BG31_HERO_006");
            PlayBuddy(service, "BG31_HERO_006_Buddy");
            service.State.Player.Tavern.Gold = 10;
            service.State.Round = 2;

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestTavernSpell("othaar-spell", "104436", 1)
            };
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(0, service.State.Player.Tavern.NextTavernSpellCostReduction);
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "104436"));
        }

        [Test]
        public void Nozdormu_StartOfTurnGrantsFreeRefreshAndChromieHelpfulRefresh()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_57");
            PlayBuddy(service, "TB_BaconShop_HERO_57_Buddy");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, service.State.Player.Tavern.FreeRefreshes);
            Assert.AreEqual(1, service.State.Player.Tavern.HelpfulRefreshes);
        }

        [Test]
        public void Taethelan_EveryFourthTavernSpellBoughtCostsZero()
        {
            var service = CreateHeroService("BG28_HERO_800");
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Tavern.Hand.Clear();

            for (var index = 0; index < 4; index += 1)
            {
                service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
                {
                    TestTavernSpell("taethelan-spell-" + index, "104436", 1)
                };
                service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            }

            Assert.AreEqual(17, service.State.Player.Tavern.Gold);
            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count(card => card.CardId == "104436"));
        }

        [Test]
        public void PatchwerkBuddy_EndTurnBuffsLeftMinionByMissingHealth()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_34");
            service.State.Player.Health = service.State.Player.MaxHealth - 5;
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(TestMinion("left", "LEFT", 2, 2));
            service.State.Player.Board.Add(MinionFactory.Create(service.HeroCatalog.GetBuddyByCardId("TB_BaconShop_HERO_34_Buddy"), BoardSide.Player, "buddy"));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(8, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Millhouse_MinionsAndRefreshesCostTwoAndUpgradeCostsOneMore()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_49");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.UpgradeCost = 5;
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("millhouse-buy", "MILLHOUSE_BUY", 1, 1)
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(8, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.AreEqual(6, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.UpgradeTavern));
            Assert.AreEqual(0, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void MagnusManastorm_FirstTwoRefreshesEachTurnAreFree()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_49");
            PlayBuddy(service, "TB_BaconShop_HERO_49_Buddy");
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.AreEqual(8, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void Gallywix_SellBanksGoldAndBuddyIncreasesMaximumGoldAtEndOfTurn()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_10");
            PlayBuddy(service, "TB_BaconShop_HERO_10_Buddy");
            service.State.Player.Tavern.Gold = 3;
            service.State.Player.Tavern.MaxGold = 3;
            var sold = TestMinion("gallywix-sold", "GALLYWIX_SOLD", 1, 1);
            service.State.Player.Board.Add(sold);

            service.Apply(new GameCommand(GameCommandType.SellMinion, sold.InstanceId));
            Assert.AreEqual(1, service.State.Player.Tavern.NextTurnBonusGold);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(6, service.State.Player.Tavern.Gold);
            Assert.AreEqual(5, service.State.Player.Tavern.MaxGold);
        }

        [Test]
        public void Omu_UpgradeTavernRefundsGoldAndBuddyAddsMinionAtEndOfTurn()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_74");
            PlayBuddy(service, "TB_BaconShop_HERO_74_Buddy");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.UpgradeCost = 5;

            service.Apply(new GameCommand(GameCommandType.UpgradeTavern));

            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
            Assert.AreEqual(2, service.State.Player.Tavern.Tier);
        }

        [Test]
        public void Hoggarr_BuyPirateGrantsGoldAndRefreshInjectsPirate()
        {
            var service = CreateHeroService("BG26_HERO_101");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Tier = 3;
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("pirate-buy", "PIRATE_BUY", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Pirate })
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(8, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void Ysera_RefreshGuaranteesDragonAndBuddyScalesWithDragonCount()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_53");
            PlayBuddy(service, "TB_BaconShop_HERO_53_Buddy");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Tier = 3;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            var hasDragon = service.State.Player.Tavern.Shop.Any(card => card != null && card.Tribes.Contains(Tribe.Dragon));
            Assert.IsTrue(hasDragon);
        }

        [Test]
        public void EvergreenBotani_EndTurnAddsMinionToBoard()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_74");
            PlayBuddy(service, "TB_BaconShop_HERO_74_Buddy");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Tier = 3;

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(service.State.Player.Board.Count > 0);
        }

        [Test]
        public void EnhanceOMechano_RefreshAddsBonusKeywordAndBuddyScalesFromBoughtKeywords()
        {
            var service = CreateHeroService("BG24_HERO_204");
            PlayBuddy(service, "BG24_HERO_204_Buddy");
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.RerollShop));

            Assert.IsTrue(service.State.Player.Tavern.Shop.Any(card =>
                card != null &&
                card.Enchantments.Any(enchantment => enchantment.SourceId == "Enhancification" && enchantment.AddedKeywords.Count == 1)));

            var bought = TestMinion("enhance-buy", "ENHANCE_BUY", 1, 1);
            bought.Keywords.Add(Keyword.Taunt);
            bought.Keywords.Add(Keyword.DivineShield);
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance> { bought };
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            var buddy = service.State.Player.Board.First(card => card.CardId == "BG24_HERO_204_Buddy");
            Assert.AreEqual(9, buddy.Attack);
            Assert.AreEqual(9, buddy.MaxHealth);
        }

        [Test]
        public void EnhanceOMedico_DoesNotScaleFromHand()
        {
            var service = CreateHeroService("BG24_HERO_204");
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG24_HERO_204_Buddy", CardKind.HeroBuddy));
            var buddy = service.State.Player.Tavern.Hand.First(card => card.CardId == "BG24_HERO_204_Buddy");
            var bought = TestMinion("enhance-hand-buy", "ENHANCE_HAND_BUY", 1, 1);
            bought.Keywords.Add(Keyword.Taunt);
            bought.Keywords.Add(Keyword.Reborn);
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance> { bought };
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(3, buddy.Attack);
            Assert.AreEqual(3, buddy.MaxHealth);
        }

        [Test]
        public void Kurtrus_ThirdBoughtMinionCreatesPlainCopyAndBuddyBuffsTavern()
        {
            var service = CreateHeroService("BG20_HERO_280");
            PlayBuddy(service, "BG20_HERO_280_Buddy");
            service.State.Player.Tavern.Gold = 20;
            var tavernTarget = TestMinion("kurtrus-shop-target", "KURTRUS_SHOP_TARGET", 1, 1);
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("kurtrus-buy-1", "KURTRUS_BUY_1", 2, 2),
                TestMinion("kurtrus-buy-2", "KURTRUS_BUY_2", 3, 3),
                TestMinion("kurtrus-buy-3", "KURTRUS_BUY_3", 4, 4),
                tavernTarget
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(3, tavernTarget.Attack);
            Assert.AreEqual(3, tavernTarget.MaxHealth);
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 2));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.Tags.Contains("plain_copy")));
        }

        [Test]
        public void Flurgl_SellFiveMinionsAddsMurlocAndBuddyTransformsTavern()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_55");
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("flurgl-shop-1", "FLURGL_SHOP_1", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Beast }),
                TestMinion("flurgl-shop-2", "FLURGL_SHOP_2", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Pirate })
            };
            PlayBuddy(service, "TB_BaconShop_HERO_55_Buddy");

            Assert.IsTrue(service.State.Player.Tavern.Shop.All(card => card == null || card.Tribes.Contains(Tribe.Murloc)));

            for (var index = 0; index < 5; index += 1)
            {
                service.State.Player.Board.Add(TestMinion("flurgl-sold-" + index, "FLURGL_SOLD_" + index, 1, 1));
            }

            foreach (var minion in service.State.Player.Board.Where(card => card.CardId.StartsWith("FLURGL_SOLD_")).ToList())
            {
                service.Apply(new GameCommand(GameCommandType.SellMinion, minion.InstanceId));
            }

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.Tribes.Contains(Tribe.Murloc)));
        }

        [Test]
        public void Saurfang_FourBoughtMinionsImprovesTavernBuffAndBuddyGainsHalfStats()
        {
            var service = CreateHeroService("BG20_HERO_102");
            PlayBuddy(service, "BG20_HERO_102_Buddy");
            service.State.Player.Tavern.Gold = 20;
            var tavernTarget = TestMinion("saurfang-shop-target", "SAURFANG_SHOP_TARGET", 1, 1);
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("saurfang-buy-1", "SAURFANG_BUY_1", 5, 5),
                TestMinion("saurfang-buy-2", "SAURFANG_BUY_2", 1, 1),
                TestMinion("saurfang-buy-3", "SAURFANG_BUY_3", 1, 1),
                TestMinion("saurfang-buy-4", "SAURFANG_BUY_4", 1, 1),
                tavernTarget
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 2));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 3));

            var buddy = service.State.Player.Board.First(card => card.CardId == "BG20_HERO_102_Buddy");
            Assert.AreEqual(8, buddy.Attack);
            Assert.AreEqual(8, buddy.MaxHealth);
            Assert.AreEqual(2, tavernTarget.Attack);
            Assert.AreEqual(3, tavernTarget.MaxHealth);
        }

        [Test]
        public void Edwin_TargetedHeroPowerImprovesAfterFiveBuysAndBuddyGrows()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_01");
            PlayBuddy(service, "TB_BaconShop_HERO_01_Buddy");
            var target = TestMinion("edwin-target", "EDWIN_TARGET", 1, 1);
            service.State.Player.Board.Add(target);
            service.State.Player.Tavern.Gold = 20;

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 1));

            Assert.AreEqual(2, target.Attack);
            Assert.AreEqual(2, target.MaxHealth);

            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("edwin-buy-1", "EDWIN_BUY_1", 1, 1),
                TestMinion("edwin-buy-2", "EDWIN_BUY_2", 1, 1),
                TestMinion("edwin-buy-3", "EDWIN_BUY_3", 1, 1),
                TestMinion("edwin-buy-4", "EDWIN_BUY_4", 1, 1),
                TestMinion("edwin-buy-5", "EDWIN_BUY_5", 1, 1)
            };

            for (var index = 0; index < 5; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.BuyMinion, index));
            }

            var buddy = service.State.Player.Board.First(card => card.CardId == "TB_BaconShop_HERO_01_Buddy");
            Assert.AreEqual(12, buddy.Attack);
            Assert.AreEqual(12, buddy.MaxHealth);

            service.State.Player.Tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 1));

            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(4, target.MaxHealth);
        }

        [Test]
        public void Kragg_PiggyBankGainsGrowingGoldOncePerGame()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_68");
            service.State.Round = 4;
            service.State.Player.Tavern.Gold = 1;

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(6, service.State.Player.Tavern.Gold);
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower)));
        }

        [Test]
        public void Sharkbait_SellingRefreshesKraggHeroPowerButOtherMinionsDoNot()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_68");
            service.State.Round = 3;
            service.State.Player.Tavern.MaxGold = 20;
            service.State.Player.Tavern.Gold = 0;

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            var decoy = TestMinion("kragg-decoy", "KRAGG_DECOY", 1, 1);
            service.State.Player.Board.Add(decoy);
            service.Apply(new GameCommand(GameCommandType.SellMinion, decoy.InstanceId));

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower)));

            PlayBuddy(service, "TB_BaconShop_HERO_68_Buddy");
            var sharkbait = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_68_Buddy");
            service.Apply(new GameCommand(GameCommandType.SellMinion, sharkbait.InstanceId));
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(10, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void George_BoonOfLightGivesDivineShieldAndKarlBuffsDivineShieldMinions()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_15");
            service.State.Player.Tavern.Gold = 5;
            service.State.Player.Board.Clear();
            var target = TestMinion("george-target", "GEORGE_TARGET", 1, 1);
            var alreadyShielded = TestMinion("george-shielded", "GEORGE_SHIELDED", 2, 2);
            alreadyShielded.Keywords.Add(Keyword.DivineShield);
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(alreadyShielded);
            PlayBuddy(service, "TB_BaconShop_HERO_15_Buddy");

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));

            var karl = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_15_Buddy");
            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.IsTrue(target.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(3, target.Attack);
            Assert.AreEqual(4, alreadyShielded.Attack);
            Assert.AreEqual(3, karl.Attack);
        }

        [Test]
        public void KarlTheLost_DoesNotBuffFromHand()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_15");
            service.State.Player.Tavern.Gold = 5;
            service.State.Player.Board.Clear();
            var target = TestMinion("george-hand-target", "GEORGE_HAND_TARGET", 1, 1);
            service.State.Player.Board.Add(target);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "TB_BaconShop_HERO_15_Buddy", CardKind.HeroBuddy));

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));

            Assert.IsTrue(target.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(1, target.Attack);
        }

        [Test]
        public void Nobundo_HeroPowerCopiesLastTavernSpellAndConsumesDiscount()
        {
            var service = CreateHeroService("BG31_HERO_003");
            service.State.Player.Tavern.LastTavernSpellCardId = "104436";
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == "104436"));

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.State.Player.Tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(8, service.State.Player.Tavern.Gold);
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "104436"));
        }

        [Test]
        public void DoctorHollidae_HeroPowerGainsRandomTavernSpell()
        {
            var service = CreateHeroService("BG28_HERO_801");
            service.State.Player.Tavern.Tier = 2;
            service.State.Player.Tavern.Gold = 5;
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card =>
                card.CardKind == CardKind.TavernSpell && card.TavernTier <= 2));
        }

        [Test]
        public void TheNineFrogs_BuyMinionGainsSameTierTavernSpellUntilChargesExpire()
        {
            var service = CreateHeroService("BG28_HERO_801");
            PlayBuddy(service, "BG28_HERO_801_Buddy");
            service.State.Player.Tavern.HeroEffectCounters["hero:hollidae:nine_frogs_remaining"] = 1;
            service.State.Player.Tavern.Gold = 10;
            var first = TestMinion("frogs-buy-1", "FROGS_BUY_1", 1, 1);
            var second = TestMinion("frogs-buy-2", "FROGS_BUY_2", 1, 1);
            first.TavernTier = 2;
            second.TavernTier = 2;
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                first,
                second
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card =>
                card.CardKind == CardKind.TavernSpell && card.TavernTier == 2));

            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));

            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
        }

        [Test]
        public void Blackthorn_BloodboundIsTwicePerTurnAndSageCopiesBloodGems()
        {
            var service = CreateHeroService("BG20_HERO_103");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(4, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower)));

            var withBuddy = CreateHeroService("BG20_HERO_103");
            withBuddy.State.Player.Tavern.Gold = 10;
            withBuddy.State.Player.Tavern.Hand.Clear();
            PlayBuddy(withBuddy, "BG20_HERO_103_Buddy");

            withBuddy.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(4, withBuddy.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
        }

        [Test]
        public void LichBazHial_StealsTavernCardAndUnderlingRewindsDamage()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_25");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Health = 30;
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("lich-steal", "LICH_STEAL", 2, 3)
            };

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));

            Assert.AreEqual(8, service.State.Player.Tavern.Gold);
            Assert.AreEqual(28, service.State.Player.Health);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == "LICH_STEAL"));
            Assert.IsNull(service.State.Player.Tavern.Shop[0]);

            var withBuddy = CreateHeroService("TB_BaconShop_HERO_25");
            withBuddy.State.Player.Tavern.Gold = 10;
            withBuddy.State.Player.Health = 30;
            PlayBuddy(withBuddy, "TB_BaconShop_HERO_25_Buddy");
            withBuddy.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("lich-steal-buddy", "LICH_STEAL_BUDDY", 2, 3)
            };

            withBuddy.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));

            var underling = withBuddy.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_25_Buddy");
            Assert.AreEqual(30, withBuddy.State.Player.Health);
            Assert.AreEqual(7, underling.Attack);
            Assert.AreEqual(7, underling.MaxHealth);
        }

        [Test]
        public void Rakanishu_LanternLightBuffsByTierAndTenderAddsStatSpells()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_75");
            service.State.Player.Tavern.Gold = 5;
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            var target = TestMinion("rakanishu-target", "RAKANISHU_TARGET", 1, 1);
            service.State.Player.Board.Add(target);

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count(card => card.CardId == "RAKANISHU_LANTERN_LIGHT"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.AreEqual(5, target.Attack);
            Assert.AreEqual(5, target.MaxHealth);

            PlayBuddy(service, "TB_BaconShop_HERO_75_Buddy");
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card =>
                card.CardKind == CardKind.TavernSpell && card.Tags.Contains("generated_spell")));
        }

        [Test]
        public void Reno_GoldenHeroPowerIsOncePerGameAndTombDiverGoldensRightmostOnSell()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_41");
            service.State.Player.Board.Clear();
            var target = TestMinion("reno-target", "RENO_TARGET", 3, 4);
            service.State.Player.Board.Add(target);

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));

            Assert.IsTrue(target.Golden);
            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(8, target.MaxHealth);
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0)));

            var rightmost = TestMinion("reno-rightmost", "RENO_RIGHTMOST", 2, 2);
            service.State.Player.Board.Add(rightmost);
            PlayBuddy(service, "TB_BaconShop_HERO_41_Buddy");
            var buddy = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_41_Buddy");

            service.Apply(new GameCommand(GameCommandType.SellMinion, buddy.InstanceId));

            Assert.IsTrue(rightmost.Golden);
            Assert.AreEqual(4, rightmost.Attack);
            Assert.AreEqual(4, rightmost.MaxHealth);
        }

        [Test]
        public void Patches_DiscountedHeroPowerGetsPirateAndTuskarrAddsBounties()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_18");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("patches-buy-1", "PATCHES_BUY_1", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Pirate }),
                TestMinion("patches-buy-2", "PATCHES_BUY_2", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Pirate })
            };

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            service.State.Player.Tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(3, service.State.Player.Tavern.Gold);
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.Tribes.Contains(Tribe.Pirate)));

            PlayBuddy(service, "TB_BaconShop_HERO_18_Buddy");
            var bountyCountAfterPlay = service.State.Player.Tavern.Hand.Count(card => card.Tags.Contains("bounty"));
            var tuskarr = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_18_Buddy");

            service.Apply(new GameCommand(GameCommandType.SellMinion, tuskarr.InstanceId));

            Assert.GreaterOrEqual(bountyCountAfterPlay, 1);
            Assert.Greater(service.State.Player.Tavern.Hand.Count(card => card.Tags.Contains("bounty")), bountyCountAfterPlay);
        }

        [Test]
        public void KingMukla_StartTurnGainsBananasAndCrazyMonkeyFeedsMoreAfterSpells()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_38");
            service.State.Player.Board.Clear();
            var target = TestMinion("mukla-target", "MUKLA_TARGET", 1, 1);
            service.State.Player.Board.Add(target);
            PlayBuddy(service, "TB_BaconShop_HERO_38_Buddy");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "MUKLA_BANANA"));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));
            var buddy = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_38_Buddy");

            service.Apply(new GameCommand(GameCommandType.SellMinion, buddy.InstanceId));

            Assert.AreEqual(6, target.Attack);
            Assert.AreEqual(6, target.MaxHealth);
        }

        [Test]
        public void CThun_EndTurnBuffImprovesAndTentacleGainsTemporaryStats()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_29");
            service.State.Player.Board.Clear();
            var target = TestMinion("cthun-target", "CTHUN_TARGET", 1, 1);
            service.State.Player.Board.Add(target);
            PlayBuddy(service, "TB_BaconShop_HERO_29_Buddy");
            var tentacle = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_29_Buddy");

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(4, target.MaxHealth);
            Assert.AreEqual(5, tentacle.Attack);
            Assert.AreEqual(5, tentacle.MaxHealth);
        }

        [Test]
        public void Eudora_FourDigsAddsGoldenMinionAndDagwikBuffsGoldenAtEndOfTurn()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_64");
            service.State.Player.Tavern.Tier = 3;
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Hand.Clear();

            for (var index = 0; index < 4; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            }

            var goldenReward = service.State.Player.Tavern.Hand.Single(card => card.Golden && card.Tags.Contains("golden_reward"));
            Assert.AreEqual(6, service.State.Player.Tavern.Gold);
            Assert.IsNotNull(goldenReward);

            service.State.Player.Board.Clear();
            var goldenTarget = TestMinion("dagwik-target", "DAGWIK_TARGET", 2, 2);
            goldenTarget.Golden = true;
            service.State.Player.Board.Add(goldenTarget);
            PlayBuddy(service, "TB_BaconShop_HERO_64_Buddy");

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(7, goldenTarget.Attack);
            Assert.AreEqual(7, goldenTarget.MaxHealth);
        }

        [Test]
        public void Elise_LeadExplorerDiscoversCurrentTierAndNavigatorReducesCost()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_42");
            service.State.Player.Tavern.Tier = 3;
            service.State.Player.Tavern.Gold = 10;

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(9, service.State.Player.Tavern.Gold);
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card => card.TavernTier == 3));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(7, service.State.Player.Tavern.Gold);

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            PlayBuddy(service, "TB_BaconShop_HERO_42_Buddy");
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(6, service.State.Player.Tavern.Gold);
        }

        [Test]
        public void Millificent_TinkerDiscoversMechsAndSquirrelBombUsesMechDeathProxy()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_17");
            service.State.Player.Tavern.Gold = 5;
            service.State.Player.Tavern.Tier = 3;

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower)));

            service.State.Player.Tavern.Tier = 4;
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(4, service.State.Player.Tavern.Gold);
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card => card.Tribes.Contains(Tribe.Mech)));

            service.State.Player.Tavern.Discover = null;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var soldMech = TestMinion("millificent-mech", "MILLIFICENT_MECH", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Mech });
            var enemy = TestMinion("millificent-enemy", "MILLIFICENT_ENEMY", 1, 10);
            service.State.Player.Board.Add(soldMech);
            service.State.Opponent.Board.Add(enemy);
            PlayBuddy(service, "TB_BaconShop_HERO_17_Buddy");
            var squirrel = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_17_Buddy");

            service.Apply(new GameCommand(GameCommandType.SellMinion, soldMech.InstanceId));
            service.Apply(new GameCommand(GameCommandType.SellMinion, squirrel.InstanceId));

            Assert.AreEqual(2, enemy.Health);
        }

        [Test]
        public void LichKing_RebornRitesIsTemporaryAndArfusAddsAttack()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_22");
            service.State.Player.Board.Clear();
            var target = TestMinion("lich-king-target", "LICH_KING_TARGET", 1, 1);
            service.State.Player.Board.Add(target);
            PlayBuddy(service, "TB_BaconShop_HERO_22_Buddy");

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));

            Assert.IsTrue(target.Keywords.Contains(Keyword.Reborn));
            Assert.AreEqual(9, target.Attack);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsFalse(target.Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void JandiceMutanusAndXyrella_CoreTavernActionsResolve()
        {
            var jandice = CreateHeroService("TB_BaconShop_HERO_71");
            jandice.State.Player.Board.Clear();
            jandice.State.Player.Board.Add(TestMinion("jandice-board", "JANDICE_REPEAT", 1, 1));
            jandice.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("jandice-shop", "JANDICE_SHOP", 4, 4)
            };
            PlayBuddy(jandice, "TB_BaconShop_HERO_71_Buddy");

            jandice.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.AreEqual("JANDICE_SHOP", jandice.State.Player.Board[0].CardId);
            jandice.State.Player.Tavern.Hand.Add(TestMinion("jandice-repeat-1", "JANDICE_REPEAT", 1, 1));
            jandice.State.Player.Tavern.Hand.Add(TestMinion("jandice-repeat-2", "JANDICE_REPEAT", 1, 1));
            jandice.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            jandice.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsTrue(jandice.State.Player.Board.Any(card => card.Enchantments.Any(enchantment => enchantment.SourceId == "Jandice's Apprentice")));

            var mutanus = CreateHeroService("BG20_HERO_301");
            mutanus.State.Player.Board.Clear();
            mutanus.State.Player.Board.Add(TestMinion("mutanus-food", "MUTANUS_FOOD", 3, 4));
            mutanus.State.Player.Board.Add(TestMinion("mutanus-target", "MUTANUS_TARGET", 1, 1));
            mutanus.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.AreEqual(1, mutanus.State.Player.Board.Count);
            Assert.AreEqual(4, mutanus.State.Player.Board[0].Attack);
            Assert.AreEqual(5, mutanus.State.Player.Board[0].MaxHealth);

            var xyrella = CreateHeroService("BG20_HERO_101");
            xyrella.State.Player.Tavern.Gold = 5;
            xyrella.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("xyrella-shop", "XYRELLA_SHOP", 7, 8)
            };
            xyrella.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            var handCard = xyrella.State.Player.Tavern.Hand.Single(card => card.CardId == "XYRELLA_SHOP");
            Assert.AreEqual(2, handCard.Attack);
            Assert.AreEqual(2, handCard.MaxHealth);
        }

        [Test]
        public void PyramadIngeAndMalygos_TargetedHeroPowersResolve()
        {
            var pyramad = CreateHeroService("TB_BaconShop_HERO_39");
            pyramad.State.Player.Tavern.Gold = 5;
            PlayBuddy(pyramad, "TB_BaconShop_HERO_39_Buddy");
            var guardian = pyramad.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_39_Buddy");
            pyramad.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("pyramad-shop", "PYRAMAD_SHOP", 2, 3)
            };
            pyramad.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.AreEqual(6, pyramad.State.Player.Tavern.Hand.Single(card => card.CardId == "PYRAMAD_SHOP").MaxHealth);
            Assert.AreEqual(5, guardian.MaxHealth);

            var inge = CreateHeroService("BG26_HERO_102");
            inge.State.Player.Tavern.Tier = 4;
            inge.State.Player.Board.Clear();
            var target = TestMinion("inge-target", "INGE_TARGET", 1, 1);
            inge.State.Player.Board.Add(target);
            PlayBuddy(inge, "BG26_HERO_102_Buddy");
            inge.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.AreEqual(8, target.Attack);
            inge.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.AreEqual(8, target.MaxHealth);

            var malygos = CreateHeroService("TB_BaconShop_HERO_58");
            malygos.State.Player.Board.Clear();
            malygos.State.Player.Tavern.Shop.Clear();
            var replace = TestMinion("malygos-target", "MALYGOS_TARGET", 1, 1);
            replace.TavernTier = 1;
            malygos.State.Player.Board.Add(replace);
            PlayBuddy(malygos, "TB_BaconShop_HERO_58_Buddy");
            malygos.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.AreEqual(2, malygos.State.Player.Board[0].TavernTier);
        }

        [Test]
        public void MaievZephrysAndHooktusk_RewardsAndLocksResolve()
        {
            var maiev = CreateHeroService("TB_BaconShop_HERO_62");
            maiev.State.Player.Tavern.Gold = 5;
            PlayBuddy(maiev, "TB_BaconShop_HERO_62_Buddy");
            maiev.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("maiev-shop", "MAIEV_SHOP", 2, 2)
            };
            maiev.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            var locked = maiev.State.Player.Tavern.Hand.Single(card => card.CardId == "MAIEV_SHOP");
            Assert.IsTrue(locked.Golden);
            Assert.IsTrue(locked.Tags.Contains("locked_in_hand"));

            var zephrys = CreateHeroService("TB_BaconShop_HERO_91");
            zephrys.State.Player.Tavern.Gold = 10;
            var pair = zephrys.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
            zephrys.State.Player.Board.Add(pair);
            zephrys.State.Player.Tavern.Hand.Add(pair.Clone());
            zephrys.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.AreEqual(2, zephrys.State.Player.Tavern.Hand.Count(card => card.CardId == pair.CardId));

            var hooktusk = CreateHeroService("TB_BaconShop_HERO_67");
            hooktusk.State.Player.Board.Clear();
            var removed = TestMinion("hooktusk-remove", "HOOKTUSK_REMOVE", 1, 1);
            removed.TavernTier = 3;
            hooktusk.State.Player.Board.Add(removed);
            PlayBuddy(hooktusk, "TB_BaconShop_HERO_67_Buddy");
            hooktusk.State.Player.Tavern.Gold = 0;
            hooktusk.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.IsNotNull(hooktusk.State.Player.Tavern.Discover);
            Assert.AreEqual(3, hooktusk.State.Player.Tavern.Gold);
        }

        [Test]
        public void VooneZerekAndTogwaggle_CopyAndStealEffectsResolve()
        {
            var voone = CreateHeroService("BG26_HERO_104");
            voone.State.Player.Tavern.Hand.Clear();
            voone.State.Player.Tavern.Hand.Add(TestMinion("voone-hand", "VOONE_HAND", 1, 1));
            PlayBuddy(voone, "BG26_HERO_104_Buddy");
            voone.State.Player.Tavern.Hand.Clear();
            voone.State.Player.Tavern.Hand.Add(TestMinion("voone-hand-2", "VOONE_HAND", 1, 1));
            voone.Apply(new GameCommand(GameCommandType.NextTurn));
            voone.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(2, voone.State.Player.Tavern.Hand.Count(card => card.CardId == "VOONE_HAND"));

            var zerek = CreateHeroService("BG31_HERO_005");
            zerek.State.Player.Board.Clear();
            zerek.State.Player.Board.Add(TestMinion("zerek-target", "ZEREK_TARGET", 3, 4));
            zerek.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.AreEqual(2, zerek.State.Player.Board.Count(card => card.CardId == "ZEREK_TARGET"));

            var tog = CreateHeroService("BG23_HERO_305");
            tog.State.Player.Tavern.Gold = 20;
            tog.State.Player.Tavern.Hand.Clear();
            tog.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("tog-shop-1", "TOG_SHOP_1", 1, 1),
                TestMinion("tog-shop-2", "TOG_SHOP_2", 1, 1)
            };
            tog.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.AreEqual(2, tog.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(tog.State.Player.Tavern.Shop.All(card => card == null));
        }

        [Test]
        public void ChenvaalaCuratorAndDeryl_PassiveBoardStateEffectsResolve()
        {
            var chen = CreateHeroService("TB_BaconShop_HERO_78");
            chen.State.Player.Tavern.UpgradeCost = 5;
            for (var index = 0; index < 3; index += 1)
            {
                chen.State.Player.Tavern.Hand.Add(TestMinion("chen-" + index, "CHEN_" + index, 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Elemental }));
                chen.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            }
            Assert.AreEqual(2, chen.State.Player.Tavern.UpgradeCost);

            var curator = CreateHeroService("TB_BaconShop_HERO_33");
            Assert.IsTrue(curator.State.Player.Board.Any(card => card.Tags.Contains("curator_amalgam") && card.Keywords.Contains(Keyword.Venomous)));
            PlayBuddy(curator, "TB_BaconShop_HERO_33_Buddy");
            var amalgam = curator.State.Player.Board.First(card => card.Tags.Contains("curator_amalgam"));
            amalgam.Attack += 3;
            amalgam.MaxHealth += 3;
            curator.Apply(new GameCommand(GameCommandType.NextTurn));
            var mishmash = curator.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_33_Buddy");
            Assert.AreEqual(7, mishmash.Attack);

            var deryl = CreateHeroService("TB_BaconShop_HERO_36");
            deryl.State.Player.Board.Clear();
            deryl.State.Player.Tavern.Hand.Add(TestMinion("deryl-play", "DERYL_PLAY", 1, 1));
            deryl.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var played = deryl.State.Player.Board.Single(card => card.CardId == "DERYL_PLAY");
            Assert.AreEqual(2, played.Attack);
        }

        [Test]
        public void RagnarosChromieSindragosaAndProxyHeroesResolveVisibleBehavior()
        {
            var rag = CreateHeroService("TB_BaconShop_HERO_11");
            rag.State.Player.Tavern.Gold = 100;
            rag.State.Player.Board.Clear();
            rag.State.Player.Board.Add(TestMinion("rag-left", "RAG_LEFT", 1, 1));
            rag.State.Player.Board.Add(TestMinion("rag-right", "RAG_RIGHT", 1, 1));
            PlayBuddy(rag, "TB_BaconShop_HERO_11_Buddy");
            for (var index = 0; index < 16; index += 1)
            {
                rag.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance> { TestMinion("rag-buy-" + index, "RAG_BUY_" + index, 1, 1) };
                rag.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
                rag.State.Player.Tavern.Hand.Clear();
            }
            rag.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(7, rag.State.Player.Board.First(card => card.CardId == "RAG_LEFT").Attack);

            var chromie = CreateHeroService("BG34_HERO_001");
            chromie.State.Player.Tavern.Gold = 10;
            chromie.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.IsTrue(chromie.State.Player.Tavern.Shop.All(card => card.CardKind == CardKind.TavernSpell));

            var sindragosa = CreateHeroService("TB_BaconShop_HERO_27");
            sindragosa.State.Player.Tavern.Gold = 10;
            sindragosa.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance> { TestMinion("sindra-buy", "SINDRA_BUY", 1, 1) };
            sindragosa.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            Assert.AreEqual(8, sindragosa.State.Player.Tavern.Gold);

            var shudderwock = CreateHeroService("TB_BaconShop_HERO_23");
            shudderwock.State.Round = 3;
            shudderwock.State.Player.Board.Clear();
            shudderwock.State.Player.Tavern.Hand.Clear();
            var shudderBrann = TestMinion("shudder-brann", "BG_LOE_077", 2, 4);
            var shudderTarget = TestMinion("shudder-razorfen", "BG20_100", 3, 1);
            shudderTarget.Keywords.Add(Keyword.Battlecry);
            shudderwock.State.Player.Board.Add(shudderBrann);
            shudderwock.State.Player.Board.Add(shudderTarget);
            shudderwock.Apply(new GameCommand(GameCommandType.UseHeroPower, 1, TargetZone.FriendlyBoard));
            Assert.AreEqual(4, shudderwock.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
            Assert.IsFalse(shudderTarget.Counters.ContainsKey("battlecry_retrigger_proxy"));

            var voljin = CreateHeroService("BG20_HERO_201");
            voljin.State.Player.Board.Clear();
            voljin.State.Player.Board.Add(TestMinion("voljin-a", "VOLJIN_A", 2, 1));
            voljin.State.Player.Board.Add(TestMinion("voljin-b", "VOLJIN_B", 5, 1));
            voljin.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, TargetZone.FriendlyBoard, 1, TargetZone.FriendlyBoard));
            Assert.AreEqual(7, voljin.State.Player.Board[0].Attack);
            Assert.AreEqual(7, voljin.State.Player.Board[1].Attack);
            Assert.IsTrue(voljin.State.Player.Board.Any(card => card.Tags.Contains("temporary_spirit_swap")));
        }

        [Test]
        public void Shudderwock_ReplaysBuddyBattlecryThroughPublicResolver()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_23");
            service.State.Round = 3;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();

            PlayBuddy(service, "TB_BaconShop_HERO_23_Buddy");
            var handAfterPlayingMuckslinger = service.State.Player.Tavern.Hand.Count;

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, TargetZone.FriendlyBoard));

            Assert.AreEqual(handAfterPlayingMuckslinger + 1, service.State.Player.Tavern.Hand.Count);
            Assert.IsFalse(service.State.Player.Board[0].Counters.ContainsKey("battlecry_retrigger_proxy"));
        }

        [Test]
        public void Sindragosa_UsesSmallerShopAndPreservesOnlyFrozenSlot()
        {
            var baseline = CreateHeroService("TB_BaconShop_HERO_34");
            var sindragosa = CreateHeroService("TB_BaconShop_HERO_27");
            var baselineMinions = baseline.State.Player.Tavern.Shop.Count(card => card != null && card.CardKind == CardKind.Minion);
            var sindragosaMinions = sindragosa.State.Player.Tavern.Shop.Count(card => card != null && card.CardKind == CardKind.Minion);
            Assert.AreEqual(baselineMinions - 1, sindragosaMinions);

            sindragosa.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("sindra-freeze-a", "SINDRA_FREEZE_A", 1, 1),
                TestMinion("sindra-freeze-b", "SINDRA_FREEZE_B", 2, 2),
                TestMinion("sindra-freeze-c", "SINDRA_FREEZE_C", 3, 3)
            };
            TavernShopSlots.Ensure(sindragosa.State.Player.Tavern);

            sindragosa.Apply(new GameCommand(GameCommandType.NextTurn));

            var originalIds = new[] { "sindra-freeze-a", "sindra-freeze-b", "sindra-freeze-c" };
            var preservedOriginals = sindragosa.State.Player.Tavern.Shop
                .Where(card => card != null && originalIds.Contains(card.InstanceId))
                .ToList();
            var frozenCards = TavernShopSlots.FrozenCards(sindragosa.State.Player.Tavern);
            Assert.AreEqual(1, preservedOriginals.Count);
            Assert.AreEqual(1, frozenCards.Count);
            Assert.AreEqual(preservedOriginals[0].InstanceId, frozenCards[0].InstanceId);
            Assert.IsTrue(frozenCards[0].Tags.Contains("frozen_by_sindragosa"));

            sindragosa.State.Player.Tavern.Gold = 10;
            var frozenIndex = sindragosa.State.Player.Tavern.Shop.FindIndex(card => card != null && card.InstanceId == frozenCards[0].InstanceId);
            sindragosa.Apply(new GameCommand(GameCommandType.BuyMinion, frozenIndex));

            Assert.IsFalse(TavernShopSlots.IsSlotFrozen(sindragosa.State.Player.Tavern, frozenIndex));
            Assert.IsFalse(sindragosa.State.Player.Tavern.Frozen);
        }

        [Test]
        public void ThawedChampion_MakesOnlyAFrozenTavernSlotGolden()
        {
            var sindragosa = CreateHeroService("TB_BaconShop_HERO_27");
            PlayBuddy(sindragosa, "TB_BaconShop_HERO_27_Buddy");
            sindragosa.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("thawed-freeze-a", "THAWED_FREEZE_A", 1, 1),
                TestMinion("thawed-freeze-b", "THAWED_FREEZE_B", 2, 2),
                TestMinion("thawed-freeze-c", "THAWED_FREEZE_C", 3, 3)
            };
            TavernShopSlots.Ensure(sindragosa.State.Player.Tavern);

            sindragosa.Apply(new GameCommand(GameCommandType.NextTurn));

            var golden = sindragosa.State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Single(item => item.Card != null && item.Card.Golden);
            Assert.IsTrue(TavernShopSlots.IsSlotFrozen(sindragosa.State.Player.Tavern, golden.Index));
            Assert.IsTrue(golden.Card.Tags.Contains("frozen_by_sindragosa"));
            Assert.IsTrue(TavernShopSlots.FrozenCards(sindragosa.State.Player.Tavern).Any(card => card.InstanceId == golden.Card.InstanceId));
        }

        [Test]
        public void VoljinSpiritSwap_UsesTwoExplicitTargetsIncludingTavernMinions()
        {
            var voljin = CreateHeroService("BG20_HERO_201");
            voljin.State.Player.Board.Clear();
            voljin.State.Player.Board.Add(TestMinion("voljin-board", "VOLJIN_BOARD", 2, 1));
            voljin.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("voljin-shop", "VOLJIN_SHOP", 5, 1),
                TestMinion("voljin-shop-untouched", "VOLJIN_SHOP_UNTOUCHED", 11, 1)
            };

            voljin.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, TargetZone.FriendlyBoard, 0, TargetZone.TavernShop));

            Assert.AreEqual(7, voljin.State.Player.Board[0].Attack);
            Assert.AreEqual(7, voljin.State.Player.Tavern.Shop[0].Attack);
            Assert.AreEqual(11, voljin.State.Player.Tavern.Shop[1].Attack);
            Assert.IsTrue(voljin.State.Player.Board[0].Tags.Contains("temporary_spirit_swap"));
            Assert.IsTrue(voljin.State.Player.Tavern.Shop[0].Tags.Contains("temporary_spirit_swap"));

            voljin.State.Player.Tavern.Frozen = true;
            voljin.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, voljin.State.Player.Board[0].Attack);
            Assert.AreEqual(5, voljin.State.Player.Tavern.Shop[0].Attack);
            Assert.IsFalse(voljin.State.Player.Board[0].Tags.Contains("temporary_spirit_swap"));
            Assert.IsFalse(voljin.State.Player.Tavern.Shop[0].Tags.Contains("temporary_spirit_swap"));
        }

        [Test]
        public void MiniZerek_TransformsIntoExplicitTavernTargetOnly()
        {
            var zerek = CreateHeroService("BG31_HERO_005");
            zerek.State.Player.Board.Clear();
            zerek.State.Player.Tavern.Hand.Clear();
            zerek.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG31_HERO_005_Buddy", CardKind.HeroBuddy));
            zerek.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("mini-zerek-shop-wrong", "MINI_ZEREK_WRONG", 1, 1),
                TestMinion("mini-zerek-shop-right", "MINI_ZEREK_RIGHT", 6, 7)
            };

            zerek.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                -1,
                TargetZone.Unspecified,
                1,
                TargetZone.TavernShop));

            var transformed = zerek.State.Player.Board.Single();
            Assert.AreEqual("MINI_ZEREK_RIGHT", transformed.CardId);
            Assert.AreEqual(6, transformed.Attack);
            Assert.AreEqual(7, transformed.MaxHealth);
            Assert.IsTrue(transformed.Tags.Contains("mini_zerek_copy"));
            Assert.IsFalse(zerek.State.Player.Board.Any(card => card.CardId == "MINI_ZEREK_WRONG"));
        }

        [Test]
        public void PhaseFive_CombatStartKeywordsSummonsAndPermanentAttackResolve()
        {
            var alakir = CreateHeroService("TB_BaconShop_HERO_76");
            alakir.State.Player.Board.Clear();
            alakir.State.Player.Board.Add(TestMinion("alakir-left", "ALAKIR_LEFT", 1, 1));
            alakir.Apply(new GameCommand(GameCommandType.SimulateCombat));
            var alakirSnapshot = alakir.State.LastReplay.InitialSnapshot.Player.Minions[0];
            Assert.IsTrue(alakirSnapshot.Keywords.Contains(Keyword.Windfury));
            Assert.IsTrue(alakirSnapshot.Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(alakirSnapshot.Keywords.Contains(Keyword.Taunt));

            var yshaarj = CreateHeroService("TB_BaconShop_HERO_92");
            yshaarj.State.Player.Tavern.Tier = 1;
            yshaarj.State.Player.Board.Clear();
            PlayBuddy(yshaarj, "TB_BaconShop_HERO_92_Buddy");
            var handBefore = yshaarj.State.Player.Tavern.Hand.Count;
            yshaarj.Apply(new GameCommand(GameCommandType.SimulateCombat));
            Assert.Greater(yshaarj.State.LastReplay.InitialSnapshot.Player.Minions.Count, 1);
            Assert.Greater(yshaarj.State.Player.Tavern.Hand.Count, handBefore);
            Assert.IsTrue(yshaarj.State.LastReplay.InitialSnapshot.Player.Minions.Any(card => card.EnchantmentSourceIds.Contains("Baby Y'Shaarj")));

            var deathwing = CreateHeroService("TB_BaconShop_HERO_52");
            deathwing.State.Player.Board.Clear();
            deathwing.State.Opponent.Board.Clear();
            deathwing.State.Player.Board.Add(TestMinion("deathwing-friendly", "DEATHWING_FRIENDLY", 2, 2));
            deathwing.State.Opponent.Board.Add(TestMinion("deathwing-enemy", "DEATHWING_ENEMY", 3, 3));
            PlayBuddy(deathwing, "TB_BaconShop_HERO_52_Buddy");
            deathwing.Apply(new GameCommand(GameCommandType.SimulateCombat));
            Assert.AreEqual(4, deathwing.State.Player.Board.First(card => card.CardId == "DEATHWING_FRIENDLY").Attack);
            Assert.AreEqual(3, deathwing.State.Player.Board.First(card => card.CardId == "DEATHWING_FRIENDLY").MaxHealth);
            Assert.AreEqual(5, deathwing.State.LastReplay.InitialSnapshot.Opponent.Minions[0].Attack);
        }

        [Test]
        public void PhaseFive_CombatCopiesAndEndTurnLieutenantsResolve()
        {
            var vanndar = CreateHeroService("BG22_HERO_003");
            vanndar.State.Round = 7;
            vanndar.State.Player.Board.Clear();
            vanndar.State.Player.Board.Add(TestMinion("van-low", "VAN_LOW", 1, 2));
            vanndar.State.Player.Board.Add(TestMinion("van-high", "VAN_HIGH", 1, 9));
            vanndar.Apply(new GameCommand(GameCommandType.SimulateCombat));
            Assert.AreEqual(3, vanndar.State.LastReplay.InitialSnapshot.Player.Minions.Count);
            Assert.IsTrue(vanndar.State.LastReplay.InitialSnapshot.Player.Minions.Any(card => card.CardId == "VAN_HIGH" && card.InstanceId.Contains("combat-copy")));

            PlayBuddy(vanndar, "BG22_HERO_003_Buddy");
            var rightMost = vanndar.State.Player.Board.Last(card => card.CardKind == CardKind.Minion);
            vanndar.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(19, rightMost.MaxHealth);

            var drek = CreateHeroService("BG22_HERO_002");
            drek.State.Round = 7;
            drek.State.Player.Board.Clear();
            drek.State.Player.Board.Add(TestMinion("drek-high", "DREK_HIGH", 8, 2));
            drek.State.Player.Board.Add(TestMinion("drek-low", "DREK_LOW", 1, 2));
            drek.Apply(new GameCommand(GameCommandType.SimulateCombat));
            Assert.IsTrue(drek.State.LastReplay.InitialSnapshot.Player.Minions.Any(card => card.CardId == "DREK_HIGH" && card.InstanceId.Contains("combat-copy")));

            PlayBuddy(drek, "BG22_HERO_002_Buddy");
            var leftMost = drek.State.Player.Board.First(card => card.CardKind == CardKind.Minion);
            drek.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(18, leftMost.Attack);
        }

        [Test]
        public void PhaseFive_TavernAndProxyBuddyEffectsResolve()
        {
            var jailer = CreateHeroService("TB_BaconShop_HERO_702");
            jailer.State.Player.Tavern.Gold = 5;
            jailer.State.Player.Tavern.FriendlyMinionDeathsThisGame = 10;
            jailer.State.Player.Board.Clear();
            jailer.State.Player.Board.Add(TestMinion("jailer-target", "JAILER_TARGET", 1, 1));
            jailer.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.AreEqual(2, jailer.State.Player.Board[0].Attack);
            Assert.AreEqual(4, jailer.State.Player.Board[0].MaxHealth);

            var aranna = CreateHeroService("TB_BaconShop_HERO_59");
            PlayBuddy(aranna, "TB_BaconShop_HERO_59_Buddy");
            aranna.State.Player.Tavern.Gold = 10;
            aranna.State.Player.Tavern.Tier = 2;
            aranna.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.IsTrue(aranna.State.Player.Tavern.Shop.Any(card => card.Tags.Contains("sklibb_extra_higher_tier")));

            var ini = CreateHeroService("BG22_HERO_200");
            PlayBuddy(ini, "BG22_HERO_200_Buddy");
            var scrubber = ini.State.Player.Board.Single(card => card.CardId == "BG22_HERO_200_Buddy");
            ini.State.Player.Tavern.Hand.Add(TestMinion("ini-mech", "INI_MECH", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Mech }));
            ini.Apply(new GameCommand(GameCommandType.PlayMinion, ini.State.Player.Tavern.Hand.Count - 1));
            Assert.AreEqual(12, scrubber.Attack);
            Assert.AreEqual(12, scrubber.MaxHealth);

            var jaraxxus = CreateHeroService("TB_BaconShop_HERO_37");
            PlayBuddy(jaraxxus, "TB_BaconShop_HERO_37_Buddy");
            var kilrek = jaraxxus.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_37_Buddy");
            jaraxxus.Apply(new GameCommand(GameCommandType.SellMinion, kilrek.InstanceId));
            Assert.IsTrue(jaraxxus.State.Player.Tavern.Hand.Any(card => card.Tribes.Contains(Tribe.Demon)));
        }

        [Test]
        public void CombatDeathrattleBuddies_TriggerFromActualCombatDeaths()
        {
            var reno = CreateHeroService("TB_BaconShop_HERO_41");
            reno.State.Player.Board.Clear();
            var rightmost = TestMinion("reno-combat-rightmost", "RENO_COMBAT_RIGHTMOST", 2, 2);
            reno.State.Player.Board.Add(rightmost);
            PlayBuddy(reno, "TB_BaconShop_HERO_41_Buddy");
            reno.State.Opponent.Board.Clear();
            reno.State.Opponent.Board.Add(TestMinion("reno-combat-enemy", "RENO_COMBAT_ENEMY", 20, 20));

            reno.Apply(new GameCommand(GameCommandType.SimulateCombat));

            Assert.IsTrue(rightmost.Golden);
            Assert.AreEqual(4, rightmost.Attack);
            Assert.AreEqual(4, rightmost.MaxHealth);

            var patches = CreateHeroService("TB_BaconShop_HERO_18");
            PlayBuddy(patches, "TB_BaconShop_HERO_18_Buddy");
            patches.State.Player.Tavern.Hand.Clear();
            patches.State.Opponent.Board.Clear();
            patches.State.Opponent.Board.Add(TestMinion("tuskarr-combat-enemy", "TUSKARR_COMBAT_ENEMY", 20, 20));

            patches.Apply(new GameCommand(GameCommandType.SimulateCombat));

            Assert.IsTrue(patches.State.Player.Tavern.Hand.Any(card => card.Tags.Contains("bounty")));
        }

        [Test]
        public void CombatDeathrattleBuddies_ApplyVisiblePostCombatRewards()
        {
            var spirit = CreateHeroService("TB_BaconShop_HERO_34");
            spirit.State.Player.Board.Clear();
            var keywordTarget = TestMinion("spirit-combat-target", "SPIRIT_COMBAT_TARGET", 2, 2);
            spirit.State.Player.Board.Add(keywordTarget);
            PlayBuddy(spirit, "TB_BaconShop_HERO_76_Buddy");
            spirit.State.Opponent.Board.Clear();
            spirit.State.Opponent.Board.Add(TestMinion("spirit-combat-enemy", "SPIRIT_COMBAT_ENEMY", 20, 20));

            spirit.Apply(new GameCommand(GameCommandType.SimulateCombat));

            Assert.IsTrue(keywordTarget.Keywords.Contains(Keyword.Windfury));
            Assert.IsTrue(keywordTarget.Keywords.Contains(Keyword.DivineShield));
            Assert.IsTrue(keywordTarget.Keywords.Contains(Keyword.Taunt));

            var buttons = CreateHeroService("BG32_HERO_002");
            buttons.State.Player.Tavern.Hand.Clear();
            PlayBuddy(buttons, "BG32_HERO_002_Buddy");
            buttons.State.Player.Tavern.Hand.Clear();
            buttons.State.Opponent.Board.Clear();
            buttons.State.Opponent.Board.Add(TestMinion("zippers-combat-enemy", "ZIPPERS_COMBAT_ENEMY", 20, 20));

            buttons.Apply(new GameCommand(GameCommandType.SimulateCombat));

            Assert.Greater(buttons.State.Player.Tavern.Hand.Count, 0);

            var putricide = CreateHeroService("BG25_HERO_100");
            putricide.State.Player.Tavern.Hand.Clear();
            PlayBuddy(putricide, "BG25_HERO_100_Buddy");
            putricide.State.Player.Tavern.Hand.Clear();
            putricide.State.Opponent.Board.Clear();
            putricide.State.Opponent.Board.Add(TestMinion("festergut-combat-enemy", "FESTERGUT_COMBAT_ENEMY", 20, 20));

            putricide.Apply(new GameCommand(GameCommandType.SimulateCombat));

            Assert.IsTrue(putricide.State.Player.Board.Any(card => card.Tags.Contains("undead_creation") || card.Tags.Contains("putricide_creation_proxy")));
            Assert.IsTrue(putricide.State.Player.Tavern.Hand.Any(card => card.Tags.Contains("undead_creation") || card.Tags.Contains("putricide_creation_proxy")));
        }

        [Test]
        public void Millificent_SquirrelBombUsesActualCombatMechDeathCountAsProxyDamage()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_17");
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(TestMinion("combat-mech-death", "COMBAT_MECH_DEATH", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Mech }));
            PlayBuddy(service, "TB_BaconShop_HERO_17_Buddy");
            service.State.Opponent.Board.Clear();
            var enemy = TestMinion("squirrel-combat-enemy", "SQUIRREL_COMBAT_ENEMY", 20, 20);
            service.State.Opponent.Board.Add(enemy);

            service.Apply(new GameCommand(GameCommandType.SimulateCombat));

            Assert.Less(enemy.Health, 20);
        }

        [Test]
        public void PhaseFive_OzumatTeronNzothAndNathanosResolveVisibleBehavior()
        {
            var ozumat = CreateHeroService("BG23_HERO_201");
            ozumat.State.Player.Board.Clear();
            PlayBuddy(ozumat, "BG23_HERO_201_Buddy");
            var sold = TestMinion("ozumat-sold", "OZUMAT_SOLD", 1, 1);
            ozumat.State.Player.Board.Add(sold);
            ozumat.Apply(new GameCommand(GameCommandType.SellMinion, sold.InstanceId));
            ozumat.Apply(new GameCommand(GameCommandType.SimulateCombat));
            var tentacle = ozumat.State.LastReplay.InitialSnapshot.Player.Minions.First(card => card.CardId == "OZUMAT_TENTACLE");
            Assert.IsTrue(tentacle.Keywords.Contains(Keyword.Taunt));
            Assert.AreEqual(6, tentacle.Attack);

            var teron = CreateHeroService("BG25_HERO_103");
            teron.State.Player.Board.Clear();
            teron.State.Player.Board.Add(TestMinion("teron-target", "TERON_TARGET", 4, 5));
            PlayBuddy(teron, "BG25_HERO_103_Buddy");
            teron.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            teron.Apply(new GameCommand(GameCommandType.SimulateCombat));
            Assert.IsTrue(teron.State.LastReplay.InitialSnapshot.Player.Minions.Any(card => card.InstanceId.Contains("teron-reanimated")));
            Assert.IsTrue(teron.State.LastReplay.InitialSnapshot.Player.Minions.Any(card => card.CardId == "BG25_HERO_103_Buddy" && card.Attack > 4));

            var nzoth = CreateHeroService("TB_BaconShop_HERO_93");
            Assert.IsTrue(nzoth.State.Player.Board.Any(card => card.CardId == "NZOTH_FISH"));
            var deathrattle = TestMinion("nzoth-deathrattle", "NZOTH_DEATHRATTLE", 1, 1);
            deathrattle.Text = "Deathrattle: test.";
            nzoth.State.Player.Board.Add(deathrattle);
            PlayBuddy(nzoth, "TB_BaconShop_HERO_93_Buddy");
            Assert.IsTrue(deathrattle.Golden);

            var sylvanas = CreateHeroService("BG23_HERO_306");
            sylvanas.State.Player.Board.Clear();
            sylvanas.State.Player.Board.Add(TestMinion("nath-left", "NATH_LEFT", 1, 1));
            sylvanas.State.Player.Board.Add(TestMinion("nath-sold", "NATH_SOLD", 6, 6));
            sylvanas.State.Player.Board.Add(TestMinion("nath-right", "NATH_RIGHT", 1, 1));
            sylvanas.State.Player.Tavern.Hand.Clear();
            sylvanas.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG23_HERO_306_Buddy", CardKind.HeroBuddy));
            sylvanas.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1));
            Assert.IsFalse(sylvanas.State.Player.Board.Any(card => card.CardId == "NATH_SOLD"));
            Assert.IsTrue(sylvanas.State.Player.Board.Any(card => card.Enchantments.Any(enchantment => enchantment.SourceId == "Nathanos Blightcaller")));
        }

        [Test]
        public void Silas_ThreeTicketsDiscoversCurrentTierMinionAndBurthBuffsChoice()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_90");
            service.State.Player.Tavern.Gold = 20;
            service.State.Player.Tavern.Tier = 2;
            PlayBuddy(service, "TB_BaconShop_HERO_90_Buddy");
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("silas-ticket-1", "SILAS_TICKET_1", 1, 1),
                TestMinion("silas-ticket-2", "SILAS_TICKET_2", 1, 1),
                TestMinion("silas-ticket-3", "SILAS_TICKET_3", 1, 1)
            };
            foreach (var card in service.State.Player.Tavern.Shop)
            {
                card.Tags.Add("silas_darkmoon_ticket");
            }

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 1));
            service.Apply(new GameCommand(GameCommandType.BuyMinion, 2));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(2, service.State.Player.Tavern.Discover.RewardTier);
            var picked = service.State.Player.Tavern.Discover.Options[0];
            var baseAttack = picked.Attack;
            var baseHealth = picked.MaxHealth;

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            var reward = service.State.Player.Tavern.Hand.Single(card => card.InstanceId == picked.InstanceId);
            Assert.AreEqual(baseAttack + 1, reward.Attack);
            Assert.AreEqual(baseHealth + 1, reward.MaxHealth);
        }

        [Test]
        public void Cookie_SousChefAllowsExtraUseAndThirdFeedDiscoversFromFedTypes()
        {
            var service = CreateHeroService("BG21_HERO_020");
            PlayBuddy(service, "BG21_HERO_020_Buddy");
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("cookie-murloc", "COOKIE_MURLOC", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Murloc })
            };

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            service.State.Player.Tavern.Shop[0] = TestMinion("cookie-beast", "COOKIE_BEAST", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Beast });
            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0)));

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("cookie-dragon", "COOKIE_DRAGON", 1, 1, new System.Collections.Generic.List<Tribe> { Tribe.Dragon })
            };
            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card =>
                card.Tribes.Contains(Tribe.Murloc) ||
                card.Tribes.Contains(Tribe.Beast) ||
                card.Tribes.Contains(Tribe.Dragon)));
        }

        [Test]
        public void Galakrond_ReplacesTargetWithHigherTierChoiceAndApostleUpgradesTavern()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_02");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("galakrond-target", "GALAKROND_TARGET", 1, 1)
            };
            service.State.Player.Tavern.Shop[0].TavernTier = 1;

            service.Apply(new GameCommand(GameCommandType.UseHeroPower, 0, 0));

            Assert.AreEqual(9, service.State.Player.Tavern.Gold);
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card => card.TavernTier > 1));
            var replacementId = service.State.Player.Tavern.Discover.Options[0].CardId;

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));

            Assert.AreEqual(replacementId, service.State.Player.Tavern.Shop[0].CardId);

            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop = new System.Collections.Generic.List<MinionInstance>
            {
                TestMinion("apostle-shop", "APOSTLE_SHOP", 1, 1)
            };
            service.State.Player.Tavern.Shop[0].TavernTier = 1;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "TB_BaconShop_HERO_02_Buddy", CardKind.HeroBuddy));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.Greater(service.State.Player.Tavern.Shop[0].TavernTier, 1);
        }

        [Test]
        public void Etc_DiscoversBuddiesAndTalentScoutGoldensBuddy()
        {
            var service = CreateHeroService("BG25_HERO_105");
            service.State.Player.Tavern.Gold = 10;
            service.State.Player.Tavern.Tier = 1;
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.UseHeroPower)));

            service.State.Player.Tavern.Tier = 2;
            service.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(7, service.State.Player.Tavern.Gold);
            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(card => card.CardKind == CardKind.HeroBuddy));

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.HeroBuddy));

            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "TB_BaconShop_HERO_90_Buddy", CardKind.HeroBuddy));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var target = service.State.Player.Board[0];
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG25_HERO_105_Buddy", CardKind.HeroBuddy));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));

            Assert.IsTrue(target.Golden);
        }

        [Test]
        public void Finley_StartsHeroPowerDiscoverAndMaxwellGetsCurrentPowerBuddy()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_40");
            var discover = service.State.Player.Tavern.Discover;

            Assert.IsNotNull(discover);
            Assert.IsTrue(discover.Options.All(card => card.CardKind == CardKind.HeroPower));

            var optionIndex = discover.Options.FindIndex(option =>
                service.HeroCatalog.AllHeroes.Any(hero =>
                    hero.HeroPower != null &&
                    hero.Buddy != null &&
                    hero.HeroPower.CardId == option.CardId));
            Assert.GreaterOrEqual(optionIndex, 0);
            var selectedPowerId = discover.Options[optionIndex].CardId;
            var expectedBuddyId = service.HeroCatalog.AllHeroes
                .First(hero => hero.HeroPower != null && hero.HeroPower.CardId == selectedPowerId)
                .Buddy.CardId;

            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, optionIndex));
            PlayBuddy(service, "TB_BaconShop_HERO_40_Buddy");
            var maxwell = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_40_Buddy");
            service.Apply(new GameCommand(GameCommandType.SellMinion, maxwell.InstanceId));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == expectedBuddyId));
        }

        [Test]
        public void Rokara_CombatKillBuffsKillerAndIcesnarlGainsHealth()
        {
            var service = CreateHeroService("BG20_HERO_100");
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();

            var killer = TestMinion("rokara-killer", "ROKARA_KILLER", 10, 10);
            killer.CanAttack = true;
            service.State.Player.Board.Add(killer);
            PlayBuddy(service, "BG20_HERO_100_Buddy");
            var icesnarl = service.State.Player.Board.Single(card => card.CardId == "BG20_HERO_100_Buddy");
            var icesnarlHealth = icesnarl.MaxHealth;

            var enemy = TestMinion("rokara-enemy", "ROKARA_ENEMY", 1, 1);
            enemy.CanAttack = true;
            service.State.Opponent.Board.Add(enemy);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1001, SafetyLimit = 5 }));

            Assert.AreEqual(11, killer.Attack);
            Assert.AreEqual(icesnarlHealth + 3, icesnarl.MaxHealth);
            Assert.IsTrue(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.FriendlyMinionKilledEnemy &&
                reward.SourceInstanceId == "rokara-killer" &&
                reward.TargetInstanceId == "rokara-enemy"));
        }

        [Test]
        public void Rafaam_FirstKillAndLoyalHenchmanSecondKillAddPlainCopies()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_45");
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Gold = 10;

            var killer = TestMinion("rafaam-killer", "RAFAAM_KILLER", 10, 10);
            killer.CanAttack = true;
            service.State.Player.Board.Add(killer);
            PlayBuddy(service, "TB_BaconShop_HERO_45_Buddy");
            service.State.Player.Tavern.Hand.Clear();

            var firstEnemy = TestMinion("rafaam-enemy-1", "RAFAAM_ENEMY_ONE", 1, 1);
            var secondEnemy = TestMinion("rafaam-enemy-2", "RAFAAM_ENEMY_TWO", 1, 1);
            firstEnemy.CanAttack = true;
            secondEnemy.CanAttack = true;
            service.State.Opponent.Board.Add(firstEnemy);
            service.State.Opponent.Board.Add(secondEnemy);

            service.Apply(new GameCommand(GameCommandType.UseHeroPower));
            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1002, SafetyLimit = 10 }));

            var copiedCardIds = service.State.Player.Tavern.Hand
                .Where(card => card.Tags.Contains("plain_copy"))
                .Select(card => card.CardId)
                .ToList();
            CollectionAssert.Contains(copiedCardIds, "RAFAAM_ENEMY_ONE");
            CollectionAssert.Contains(copiedCardIds, "RAFAAM_ENEMY_TWO");
            Assert.AreEqual(2, service.State.LastResult.PlayerRewards.Count(reward => reward.Type == CombatRewardType.FriendlyMinionKilledEnemy));
        }

        [Test]
        public void Illidan_WingmenAttackBeforeNormalCombatAndEclipsionGrantsOneAttackImmune()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_08");
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            PlayBuddy(service, "TB_BaconShop_HERO_08_Buddy");
            var buddy = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_08_Buddy");

            var left = TestMinion("illidan-left", "ILLIDAN_LEFT", 1, 6);
            var right = TestMinion("illidan-right", "ILLIDAN_RIGHT", 1, 6);
            left.CanAttack = true;
            right.CanAttack = true;
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(left);
            service.State.Player.Board.Add(buddy);
            service.State.Player.Board.Add(right);

            var enemy = TestMinion("illidan-enemy", "ILLIDAN_ENEMY", 4, 20);
            enemy.CanAttack = true;
            enemy.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(enemy);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1003, SafetyLimit = 3 }));

            var attacks = service.State.LastReplay.Frames
                .Where(frame => frame.EventType == CombatEventType.AttackDeclared)
                .ToList();
            Assert.GreaterOrEqual(attacks.Count, 3);
            Assert.AreEqual("illidan-left", attacks[0].ActorId);
            Assert.IsTrue(attacks[0].TriggeredAttack);
            Assert.AreEqual("illidan-right", attacks[1].ActorId);
            Assert.IsTrue(attacks[1].TriggeredAttack);
            Assert.AreEqual(BoardSide.Opponent, attacks[2].ActorSide);
            Assert.IsFalse(attacks[2].TriggeredAttack);

            var damageFrames = service.State.LastReplay.Frames
                .Where(frame => frame.EventType == CombatEventType.DamageResolved)
                .ToList();
            Assert.AreEqual(7, damageFrames[0].PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "illidan-left").Health);
            Assert.Less(damageFrames[1].PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "illidan-right").Health, 7);
        }

        [Test]
        public void Greybough_InternalCombatSummonsGainStatsTauntAndQueueRewards()
        {
            var service = CreateHeroService("TB_BaconShop_HERO_95");
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            var bonehead = TestMinion("greybough-bonehead", "BG28_300", 1, 1);
            bonehead.CanAttack = true;
            bonehead.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Add(bonehead);
            var enemy = TestMinion("greybough-enemy", "GREYBOUGH_ENEMY", 10, 20);
            enemy.CanAttack = true;
            enemy.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(enemy);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1004, SafetyLimit = 1 }));

            var summons = service.State.LastResult.FinalPlayerBoard.Where(card => card.CardId == "SKELETON").ToList();
            Assert.AreEqual(2, summons.Count);
            Assert.IsTrue(summons.All(card => card.Attack == 2 && card.MaxHealth == 3 && card.Keywords.Contains(Keyword.Taunt)));
            Assert.AreEqual(2, service.State.LastResult.PlayerRewards.Count(reward => reward.Type == CombatRewardType.FriendlyMinionSummoned));
        }

        [Test]
        public void Tamuzo_InternalCombatSummonsDoubleStats()
        {
            var service = CreateHeroService("BG23_HERO_201");
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            PlayBuddy(service, "BG23_HERO_201_Buddy");
            var tamuzo = service.State.Player.Board.Single(card => card.CardId == "BG23_HERO_201_Buddy");
            var bonehead = TestMinion("tamuzo-bonehead", "BG28_300", 1, 1);
            bonehead.CanAttack = true;
            bonehead.Keywords.Add(Keyword.Deathrattle);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(bonehead);
            service.State.Player.Board.Add(tamuzo);
            var enemy = TestMinion("tamuzo-enemy", "TAMUZO_ENEMY", 10, 20);
            enemy.CanAttack = true;
            enemy.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(enemy);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1005, SafetyLimit = 1 }));

            var summons = service.State.LastResult.FinalPlayerBoard.Where(card => card.CardId == "SKELETON").ToList();
            Assert.AreEqual(2, summons.Count);
            Assert.IsTrue(summons.All(card => card.Attack == 2 && card.MaxHealth == 2 && card.Enchantments.Any(enchantment => enchantment.SourceId == "Tamuzo")));
        }

        [Test]
        public void BabyYshaarj_InternalSameTierCombatSummonBuffsBoard()
        {
            var service = CreateHeroService("BG32_HERO_001");
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Tier = 1;
            PlayBuddy(service, "TB_BaconShop_HERO_92_Buddy");
            var baby = service.State.Player.Board.Single(card => card.CardId == "TB_BaconShop_HERO_92_Buddy");
            var sporebat = TestMinion("baby-yshaarj-sporebat", "BG31_835", 1, 1);
            sporebat.CanAttack = true;
            sporebat.Keywords.Add(Keyword.Deathrattle);
            var handMinion = TestMinion("baby-yshaarj-hand", "BABY_YSHAARJ_HAND", 2, 2);
            service.State.Player.Tavern.Hand.Add(handMinion);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(sporebat);
            service.State.Player.Board.Add(baby);
            var enemy = TestMinion("baby-yshaarj-enemy", "BABY_YSHAARJ_ENEMY", 10, 20);
            enemy.CanAttack = true;
            enemy.Owner = BoardSide.Opponent;
            service.State.Opponent.Board.Add(enemy);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1006, SafetyLimit = 1 }));

            var summoned = service.State.LastResult.FinalPlayerBoard.Single(card => card.CardId == "BABY_YSHAARJ_HAND");
            Assert.AreEqual(3, summoned.Attack);
            Assert.AreEqual(3, summoned.MaxHealth);
            Assert.AreEqual(7, service.State.LastResult.FinalPlayerBoard.Single(card => card.CardId == "TB_BaconShop_HERO_92_Buddy").Attack);
        }

        [Test]
        public void Phase7_BuddyFirstFrameworkEffects_WorkAsVisibleProxies()
        {
            var marin = CreateHeroService("BG30_HERO_304");
            marin.State.Player.Tavern.Hand.Clear();
            PlayBuddy(marin, "BG30_HERO_304_Buddy");
            marin.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.Greater(marin.State.Player.Tavern.Hand.Count, 0);

            var buttons = CreateHeroService("BG32_HERO_002");
            buttons.State.Player.Tavern.Hand.Clear();
            PlayBuddy(buttons, "BG32_HERO_002_Buddy");
            var zippers = buttons.State.Player.Board.Single(card => card.CardId == "BG32_HERO_002_Buddy");
            buttons.Apply(new GameCommand(GameCommandType.SellMinion, zippers.InstanceId));
            Assert.Greater(buttons.State.Player.Tavern.Hand.Count, 0);

            var akazamzarak = CreateHeroService("TB_BaconShop_HERO_21");
            akazamzarak.State.Player.Tavern.Hand.Clear();
            PlayBuddy(akazamzarak, "TB_BaconShop_HERO_21_Buddy");
            akazamzarak.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.IsTrue(akazamzarak.State.Player.Tavern.Hand.Any(card => card.CardId == "BETTER_SECRET_PROXY"));

            var putricide = CreateHeroService("BG25_HERO_100");
            putricide.State.Player.Tavern.Hand.Clear();
            PlayBuddy(putricide, "BG25_HERO_100_Buddy");
            var festergut = putricide.State.Player.Board.Single(card => card.CardId == "BG25_HERO_100_Buddy");
            putricide.Apply(new GameCommand(GameCommandType.SellMinion, festergut.InstanceId));
            Assert.IsTrue(putricide.State.Player.Board.Any(card => card.Tags.Contains("undead_creation") || card.Tags.Contains("putricide_creation_proxy")));
            Assert.IsTrue(putricide.State.Player.Tavern.Hand.Any(card => card.Tags.Contains("undead_creation") || card.Tags.Contains("putricide_creation_proxy")));

            var raynor = CreateHeroService("BG31_HERO_801");
            raynor.State.Player.Tavern.Hand.Clear();
            PlayBuddy(raynor, "BG31_HERO_801_Buddy");
            raynor.State.Player.Tavern.Hand.Add(TestTavernSpell("ty-spell-1", "MUKLA_BANANA", 0));
            raynor.State.Player.Tavern.Hand.Add(TestTavernSpell("ty-spell-2", "MUKLA_BANANA", 0));
            raynor.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            raynor.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.IsTrue(raynor.State.Player.Tavern.Hand.Any(card => card.CardId == "BATTLECRUISER_UPGRADE"));

            var artanis = CreateHeroService("BG31_HERO_802");
            artanis.State.Player.Board.Clear();
            artanis.State.Player.Tavern.Hand.Clear();
            artanis.State.Player.Board.Add(TestMinion("probius-target", "PROBIUS_TARGET", 2, 2, new System.Collections.Generic.List<Tribe> { Tribe.Mech }));
            artanis.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG31_HERO_802_Buddy", CardKind.HeroBuddy));
            artanis.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));
            Assert.IsTrue(artanis.State.Player.Board[0].Golden);

            var kerrigan = CreateHeroService("BG31_HERO_811");
            kerrigan.State.Player.Tavern.Hand.Clear();
            PlayBuddy(kerrigan, "BG31_HERO_811_Buddy");
            var brokenHorn = kerrigan.State.Player.Board.Single(card => card.CardId == "BG31_HERO_811_Buddy");
            kerrigan.Apply(new GameCommand(GameCommandType.SellMinion, brokenHorn.InstanceId));
            Assert.IsNotNull(kerrigan.State.Player.Tavern.Discover);
            Assert.IsTrue(kerrigan.State.Player.Tavern.Discover.Options.All(card => card.CardId == "ZERG_MINION_PROXY" && card.Attack == 6 && card.MaxHealth == 6));

            var tess = CreateHeroService("TB_BaconShop_HERO_50");
            tess.State.Player.Tavern.Hand.Clear();
            tess.State.Opponent.HeroId = "TB_BaconShop_HERO_90";
            PlayBuddy(tess, "TB_BaconShop_HERO_50_Buddy");
            tess.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.IsTrue(tess.State.Player.Tavern.Hand.Any(card => card.CardId == "TB_BaconShop_HERO_90_Buddy"));
        }

        [Test]
        public void OpponentHistorySnapshot_WaxadredAndRecentDeathsUseLastCombat()
        {
            var service = CreateHeroService("BG23_HERO_305");
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(TestMinion("history-player-wall", "HISTORY_PLAYER_WALL", 10, 20));
            var oldHigh = TestMinion("history-old-high", "HISTORY_OLD_HIGH", 5, 5);
            oldHigh.Owner = BoardSide.Opponent;
            oldHigh.TavernTier = 6;
            var oldLow = TestMinion("history-old-low", "HISTORY_OLD_LOW", 2, 2);
            oldLow.Owner = BoardSide.Opponent;
            oldLow.TavernTier = 2;
            service.State.Opponent.Board.Add(oldLow);
            service.State.Opponent.Board.Add(oldHigh);

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1701, SafetyLimit = 1 }));

            Assert.AreEqual(2, service.State.OpponentHistory.LastOpponentWarband.Count);
            Assert.IsTrue(service.State.OpponentHistory.LastOpponentWarband.Any(card => card.CardId == "HISTORY_OLD_HIGH"));
            Assert.IsTrue(service.State.OpponentHistory.RecentCombatDeaths.Any(card => card.CardId == "HISTORY_OLD_LOW" || card.CardId == "HISTORY_OLD_HIGH"));

            service.State.Opponent.Board.Clear();
            var current = TestMinion("history-current", "HISTORY_CURRENT", 9, 9);
            current.Owner = BoardSide.Opponent;
            current.TavernTier = 7;
            service.State.Opponent.Board.Add(current);
            PlayBuddy(service, "BG23_HERO_305_Buddy");

            Assert.IsTrue(service.State.Player.Tavern.Shop.Any(card => card != null && card.CardId == "HISTORY_OLD_HIGH"));
            Assert.IsFalse(service.State.Player.Tavern.Shop.Any(card => card != null && card.CardId == "HISTORY_CURRENT"));
        }

        [Test]
        public void TessAndHunterOfOld_UseLastOpponentHistory()
        {
            var tess = CreateHeroService("TB_BaconShop_HERO_50");
            tess.State.Player.Board.Clear();
            tess.State.Opponent.Board.Clear();
            tess.State.Opponent.HeroId = "TB_BaconShop_HERO_90";
            var lastOpponentMinion = TestMinion("tess-last", "TESS_LAST_WARBAND", 4, 4);
            lastOpponentMinion.Owner = BoardSide.Opponent;
            lastOpponentMinion.TavernTier = 4;
            tess.State.Opponent.Board.Add(lastOpponentMinion);
            PlayBuddy(tess, "TB_BaconShop_HERO_50_Buddy");
            tess.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1702, SafetyLimit = 1 }));

            tess.State.Opponent.Board.Clear();
            var currentOpponentMinion = TestMinion("tess-current", "TESS_CURRENT_WARBAND", 8, 8);
            currentOpponentMinion.Owner = BoardSide.Opponent;
            currentOpponentMinion.TavernTier = 6;
            tess.State.Opponent.Board.Add(currentOpponentMinion);
            tess.State.Opponent.HeroId = "TB_BaconShop_HERO_40";
            tess.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(tess.State.Player.Tavern.Hand.Any(card => card.CardId == "TB_BaconShop_HERO_90_Buddy"));
            Assert.IsFalse(tess.State.Player.Tavern.Hand.Any(card => card.CardId == "TB_BaconShop_HERO_40_Buddy"));

            tess.State.Player.Tavern.Gold = 10;
            tess.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.IsTrue(tess.State.Player.Tavern.Shop.Any(card => card != null && card.CardId == "TESS_LAST_WARBAND"));
            Assert.IsFalse(tess.State.Player.Tavern.Shop.Any(card => card != null && card.CardId == "TESS_CURRENT_WARBAND"));
        }

        [Test]
        public void ScabbsAndWardenThelwater_UseNextOpponentProxy()
        {
            var scabbs = CreateHeroService("BG21_HERO_010");
            scabbs.State.Player.Tavern.Hand.Clear();
            scabbs.State.Opponent.Board.Clear();
            scabbs.State.Opponent.HeroId = "TB_BaconShop_HERO_90";
            var first = TestMinion("scabbs-next-a", "SCABBS_NEXT_A", 3, 3);
            first.Owner = BoardSide.Opponent;
            first.TavernTier = 3;
            var second = TestMinion("scabbs-next-b", "SCABBS_NEXT_B", 5, 5);
            second.Owner = BoardSide.Opponent;
            second.TavernTier = 5;
            scabbs.State.Opponent.Board.Add(first);
            scabbs.State.Opponent.Board.Add(second);
            PlayBuddy(scabbs, "BG21_HERO_010_Buddy");
            scabbs.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.IsTrue(scabbs.State.Player.Tavern.Hand.Any(card => card.CardId == "TB_BaconShop_HERO_90_Buddy"));

            scabbs.State.Player.Tavern.Gold = 10;
            scabbs.Apply(new GameCommand(GameCommandType.UseHeroPower));
            Assert.IsNotNull(scabbs.State.Player.Tavern.Discover);
            Assert.IsTrue(scabbs.State.Player.Tavern.Discover.Options.All(card => card.Tags.Contains("plain_copy")));
            Assert.IsTrue(scabbs.State.Player.Tavern.Discover.Options.Any(card => card.CardId == "SCABBS_NEXT_A" || card.CardId == "SCABBS_NEXT_B"));
        }

        [Test]
        public void BigglesworthAndLilKt_UseEliminatedAndLowestHealthOpponentProxies()
        {
            var bigglesworth = CreateHeroService("TB_BaconShop_HERO_70");
            bigglesworth.State.Player.Board.Clear();
            bigglesworth.State.Opponent.Board.Clear();
            bigglesworth.State.Opponent.Health = 0;
            var eliminated = TestMinion("biggles-eliminated", "BIGGLES_ELIMINATED", 7, 7);
            eliminated.Owner = BoardSide.Opponent;
            eliminated.TavernTier = 5;
            bigglesworth.State.Opponent.Board.Add(eliminated);
            bigglesworth.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1703, SafetyLimit = 1 }));
            bigglesworth.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, bigglesworth.State.OpponentHistory.EliminatedPlayerWarbands.Count);
            Assert.IsNotNull(bigglesworth.State.Player.Tavern.Discover);
            Assert.IsTrue(bigglesworth.State.Player.Tavern.Discover.Options.Any(card => card.CardId == "BIGGLES_ELIMINATED"));

            var lilKt = CreateHeroService("TB_BaconShop_HERO_70");
            lilKt.State.Player.Tavern.Hand.Clear();
            lilKt.State.Opponent.Board.Clear();
            var lowest = TestMinion("lil-kt-lowest", "LIL_KT_LOWEST", 2, 6);
            lowest.Owner = BoardSide.Opponent;
            lowest.TavernTier = 3;
            lilKt.State.Opponent.Board.Add(lowest);
            PlayBuddy(lilKt, "TB_BaconShop_HERO_70_Buddy");
            lilKt.Apply(new GameCommand(GameCommandType.NextTurn));

            var gained = lilKt.State.Player.Tavern.Hand.Single(card => card.CardId == "LIL_KT_LOWEST");
            Assert.IsTrue(gained.Tags.Contains("plain_copy"));
            Assert.AreEqual(BoardSide.Player, gained.Owner);
        }

        private static MatchService CreateHeroService(string heroCardId)
        {
            return MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { SelectedHeroCardId = heroCardId });
        }

        private static void PlayBuddy(MatchService service, string buddyCardId)
        {
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, buddyCardId, CardKind.HeroBuddy));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
        }

        private static void BuyFirstShopMinion(MatchService service)
        {
            var index = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            Assert.GreaterOrEqual(index, 0);
            service.Apply(new GameCommand(GameCommandType.BuyMinion, index));
        }

        private static MinionInstance TestTavernSpell(string instanceId, string cardId, int cost)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = cardId,
                Name = "Test Tavern Spell",
                Cost = cost,
                TavernTier = 1,
                Tribes = new System.Collections.Generic.List<Tribe> { Tribe.None },
                Keywords = new System.Collections.Generic.List<Keyword> { Keyword.TavernSpell },
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0
            };
        }

        private static MinionInstance TestMinion(string instanceId, string cardId, int attack, int health, System.Collections.Generic.List<Tribe> tribes = null)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = cardId,
                Name = cardId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Tribes = tribes ?? new System.Collections.Generic.List<Tribe> { Tribe.None },
                Keywords = new System.Collections.Generic.List<Keyword>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0
            };
        }
    }
}
