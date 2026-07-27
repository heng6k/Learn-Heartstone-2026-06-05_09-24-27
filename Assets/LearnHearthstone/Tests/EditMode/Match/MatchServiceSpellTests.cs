using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MatchServiceSpellTests
    {
        [Test]
        public void Apply_PlayPointyArrowBuffsFirstAvailableMinionAttack()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            var beforeAttack = target.Attack;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(beforeAttack + 4, service.State.Player.Board[0].Attack);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Last().Message.Contains("尖利箭矢"));
        }

        [Test]
        public void Apply_TargetedSpellRejectsMissingStaleAndWrongZoneTargetsWithoutConsumingSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            var beforeAttack = target.Attack;
            AddSpellToHand(service, "100596");
            var spell = service.State.Player.Tavern.Hand.Single();
            var castsThisTurn = service.State.Player.Tavern.TavernSpellsCastThisTurn;
            var castsThisGame = service.State.Player.Tavern.TavernSpellsCastThisGame;

            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, 0)));
            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                "stale-target")));
            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.OpponentBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId)));

            Assert.AreSame(spell, service.State.Player.Tavern.Hand.Single());
            Assert.AreEqual(beforeAttack, target.Attack);
            Assert.AreEqual(castsThisTurn, service.State.Player.Tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(castsThisGame, service.State.Player.Tavern.TavernSpellsCastThisGame);
        }

        [Test]
        public void Apply_PlayTavernCoinGainsGoldWithoutAddingBoardCard()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 0;
            service.State.Player.Tavern.MaxGold = 3;
            AddSpellToHand(service, "104436");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.Gold);
            Assert.AreEqual(0, service.State.Player.Board.Count);
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
        }

        [Test]
        public void Apply_PlayPerfectImageSetsTargetStatsToTwentyTwenty()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            AddSpellToHand(service, "104601");

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                service.State.Player.Tavern.Hand.Count - 1,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(20, service.State.Player.Board[0].Attack);
            Assert.AreEqual(20, service.State.Player.Board[0].Health);
            Assert.AreEqual(20, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_PlayUnexpectedFruitBuffsShopMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var shopTarget = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion);
            var beforeAttack = shopTarget.Attack;
            var beforeHealth = shopTarget.Health;
            AddSpellToHand(service, "105903");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            Assert.AreEqual(beforeAttack + 1, shopTarget.Attack);
            Assert.AreEqual(beforeHealth + 2, shopTarget.Health);
        }

        [Test]
        public void Apply_PlayNewSaplingStartsTierOneDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            AddSpellToHand(service, "122864");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.IsNotNull(service.State.Player.Tavern.Discover);
            Assert.AreEqual(1, service.State.Player.Tavern.Discover.RewardTier);
            Assert.AreEqual(3, service.State.Player.Tavern.Discover.Options.Count);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.TavernTier == 1));
        }

        [Test]
        public void Apply_PlaySpitescaleSpecialAddsThreeTemporarySpellcraftCards()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 4;
            service.State.Player.Tavern.Hand.Clear();
            AddSpellToHand(service, "110406");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(3, service.State.Player.Tavern.Hand.Select(card => card.InstanceId).Distinct().Count());
            foreach (var card in service.State.Player.Tavern.Hand)
            {
                AssertSpitescaleSpellcraftCard(card);
            }
        }

        [Test]
        public void Apply_SpitescaleSpecialUsesExactSevenCardPoolAndCreationMetadata()
        {
            var expected = new HashSet<string>(StringComparer.Ordinal)
            {
                "DEEP_SEA_ANGLER_SPELL",
                "DEEP_BLUE_SPELL",
                "REEF_RIFFER_SPELL",
                "SURF_N_SURF_SPELL",
                "VOLCANIC_VISITOR_ATTACK_SPELL",
                "VOLCANIC_VISITOR_HEALTH_SPELL",
                "FROSTLING_PRIESTESS_SPELL"
            };
            var observed = new HashSet<string>(StringComparer.Ordinal);
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;

            for (var seed = 1; seed <= 32 && observed.Count < expected.Count; seed += 1)
            {
                service.State.Seed = seed;
                AddSpellToHand(service, "110406");

                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

                Assert.AreEqual(3, tavern.Hand.Count);
                Assert.AreEqual(3, tavern.Hand.Select(card => card.InstanceId).Distinct().Count());
                foreach (var card in tavern.Hand)
                {
                    Assert.IsTrue(expected.Contains(card.CardId), "Unexpected Spitescale Spellcraft card: " + card.CardId);
                    AssertSpitescaleSpellcraftCard(card);
                    observed.Add(card.CardId);
                }
            }

            CollectionAssert.AreEquivalent(expected, observed);
        }

        [Test]
        public void Apply_SpitescaleSpecialStopsAtTheHandLimit()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var template = tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
            tavern.Hand.Clear();
            for (var index = 0; index < 9; index += 1)
            {
                var filler = template.Clone();
                filler.InstanceId = "spitescale-filler-" + index;
                filler.Owner = BoardSide.Player;
                filler.PoolSource = PoolSource.Debug;
                filler.PoolCopiesHeld = 0;
                tavern.Hand.Add(filler);
            }

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "110406", CardKind.TavernSpell));
            var spellIndex = tavern.Hand.FindIndex(card => card.CardId == "110406");
            var castsBefore = tavern.TavernSpellsCastThisTurn;

            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex));

            Assert.AreEqual(10, tavern.Hand.Count);
            Assert.AreEqual(9, tavern.Hand.Count(card => card.InstanceId.StartsWith("spitescale-filler-", StringComparison.Ordinal)));
            var generated = tavern.Hand.Single(card => card.Tags.Contains("temporary_spellcraft_card"));
            AssertSpitescaleSpellcraftCard(generated);
            Assert.AreEqual(castsBefore + 1, tavern.TavernSpellsCastThisTurn);
        }

        [Test]
        public void Apply_CorruptedCupcakesRejectsNonDemonWithoutChangingState()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var target = AddCorruptedCupcakesTarget(service, Tribe.Beast);
            var shopBefore = tavern.Shop.Select(card => card?.InstanceId).ToList();
            var poolBefore = new Dictionary<string, int>(tavern.Pool);
            var castsThisTurn = tavern.TavernSpellsCastThisTurn;
            var castsThisGame = tavern.TavernSpellsCastThisGame;
            AddSpellToHand(service, "110407");
            var spell = tavern.Hand.Single();

            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId)));

            Assert.AreSame(spell, tavern.Hand.Single());
            CollectionAssert.AreEqual(shopBefore, tavern.Shop.Select(card => card?.InstanceId).ToList());
            CollectionAssert.AreEquivalent(poolBefore, tavern.Pool);
            Assert.AreEqual(castsThisTurn, tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(castsThisGame, tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(2, target.Attack);
            Assert.AreEqual(3, target.MaxHealth);
        }

        [Test]
        public void Apply_CorruptedCupcakesConsumesThreeDistinctMinionsAndClearsPoolSlotsWithoutBuySellTriggers()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var target = AddCorruptedCupcakesTarget(service, Tribe.Demon);
            var buySentinel = target.Clone();
            buySentinel.DefinitionId = "cupcakes-buy-sentinel";
            buySentinel.CardId = "CUPCAKES_BUY_SENTINEL";
            buySentinel.InstanceId = "cupcakes-buy-sentinel";
            buySentinel.Name = "Cupcakes Buy Sentinel";
            buySentinel.Tribes.Clear();
            buySentinel.Tribes.Add(Tribe.None);
            buySentinel.EffectIds.Add("card_bought_buff_self_1_1");
            service.State.Player.Board.Add(buySentinel);

            var candidates = tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .Take(3)
                .ToList();
            Assert.AreEqual(3, candidates.Count, "Cupcakes regression needs three Tavern minions.");
            var retainedIndexes = new HashSet<int>(candidates.Select(item => item.Index));
            for (var index = 0; index < tavern.Shop.Count; index += 1)
            {
                if (tavern.Shop[index]?.CardKind == CardKind.Minion && !retainedIndexes.Contains(index))
                {
                    tavern.Shop[index] = null;
                }
            }

            TavernShopSlots.Ensure(tavern);
            Assert.AreEqual(3, tavern.Shop.Count(card => card?.CardKind == CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.FreezeShop, null, true));
            var consumed = candidates
                .Select(item => new
                {
                    item.Index,
                    Card = tavern.Shop[item.Index],
                    Attack = tavern.Shop[item.Index].Attack,
                    Health = tavern.Shop[item.Index].MaxHealth,
                    PoolCopies = tavern.Shop[item.Index].PoolCopiesHeld
                })
                .ToList();
            Assert.AreEqual(3, consumed.Select(item => item.Card.InstanceId).Distinct().Count());
            Assert.IsTrue(consumed.All(item => item.PoolCopies > 0));

            var targetAttack = target.Attack;
            var targetHealth = target.MaxHealth;
            var sentinelAttack = buySentinel.Attack;
            var sentinelHealth = buySentinel.MaxHealth;
            var gold = tavern.Gold;
            var soldAttack = tavern.SoldThisTurnAttack;
            var soldHealth = tavern.SoldThisTurnHealth;
            var poolBefore = new Dictionary<string, int>(tavern.Pool);
            AddSpellToHand(service, "110407");
            var buyLogs = tavern.RecruitLog.Count(entry => entry.Type == RecruitLogType.Buy);
            var sellLogs = tavern.RecruitLog.Count(entry => entry.Type == RecruitLogType.Sell);
            var castsThisTurn = tavern.TavernSpellsCastThisTurn;
            var castsThisGame = tavern.TavernSpellsCastThisGame;

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(targetAttack + consumed.Sum(item => item.Attack), target.Attack);
            Assert.AreEqual(targetHealth + consumed.Sum(item => item.Health), target.MaxHealth);
            Assert.AreEqual(target.MaxHealth, target.Health);
            Assert.AreEqual(0, tavern.Hand.Count);
            Assert.AreEqual(gold, tavern.Gold);
            Assert.AreEqual(soldAttack, tavern.SoldThisTurnAttack);
            Assert.AreEqual(soldHealth, tavern.SoldThisTurnHealth);
            Assert.AreEqual(buyLogs, tavern.RecruitLog.Count(entry => entry.Type == RecruitLogType.Buy));
            Assert.AreEqual(sellLogs, tavern.RecruitLog.Count(entry => entry.Type == RecruitLogType.Sell));
            Assert.AreEqual(sentinelAttack, buySentinel.Attack);
            Assert.AreEqual(sentinelHealth, buySentinel.MaxHealth);
            Assert.AreEqual(castsThisTurn + 1, tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(castsThisGame + 1, tavern.TavernSpellsCastThisGame);
            foreach (var item in consumed)
            {
                Assert.IsNull(tavern.Shop[item.Index]);
                Assert.IsFalse(tavern.ShopSlots[item.Index].Frozen);
                Assert.IsNull(tavern.ShopSlots[item.Index].CardInstanceId);
            }

            foreach (var group in consumed.GroupBy(item => item.Card.DefinitionId))
            {
                Assert.IsTrue(poolBefore.TryGetValue(group.Key, out var beforeRemaining));
                Assert.IsTrue(tavern.Pool.TryGetValue(group.Key, out var afterRemaining));
                Assert.AreEqual(beforeRemaining + group.Sum(item => item.PoolCopies), afterRemaining);
            }
        }

        [Test]
        public void Apply_CorruptedCupcakesConsumesEveryTavernMinionWhenFewerThanThreeExist()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var target = AddCorruptedCupcakesTarget(service, Tribe.Demon);
            var candidates = tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .Take(2)
                .ToList();
            Assert.AreEqual(2, candidates.Count, "Cupcakes regression needs two Tavern minions.");
            var retainedIndexes = new HashSet<int>(candidates.Select(item => item.Index));
            for (var index = 0; index < tavern.Shop.Count; index += 1)
            {
                if (tavern.Shop[index]?.CardKind == CardKind.Minion && !retainedIndexes.Contains(index))
                {
                    tavern.Shop[index] = null;
                }
            }

            TavernShopSlots.Ensure(tavern);
            Assert.AreEqual(2, tavern.Shop.Count(card => card?.CardKind == CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.FreezeShop, null, true));
            var consumed = candidates
                .Select(item => new
                {
                    item.Index,
                    Card = tavern.Shop[item.Index],
                    Attack = tavern.Shop[item.Index].Attack,
                    Health = tavern.Shop[item.Index].MaxHealth,
                    PoolCopies = tavern.Shop[item.Index].PoolCopiesHeld
                })
                .ToList();
            var targetAttack = target.Attack;
            var targetHealth = target.MaxHealth;
            var poolBefore = new Dictionary<string, int>(tavern.Pool);
            AddSpellToHand(service, "110407");

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                target.InstanceId));

            Assert.AreEqual(targetAttack + consumed.Sum(item => item.Attack), target.Attack);
            Assert.AreEqual(targetHealth + consumed.Sum(item => item.Health), target.MaxHealth);
            foreach (var item in consumed)
            {
                Assert.IsNull(tavern.Shop[item.Index]);
                Assert.IsFalse(tavern.ShopSlots[item.Index].Frozen);
                Assert.IsNull(tavern.ShopSlots[item.Index].CardInstanceId);
            }

            foreach (var group in consumed.GroupBy(item => item.Card.DefinitionId))
            {
                Assert.IsTrue(poolBefore.TryGetValue(group.Key, out var beforeRemaining));
                Assert.IsTrue(tavern.Pool.TryGetValue(group.Key, out var afterRemaining));
                Assert.AreEqual(beforeRemaining + group.Sum(item => item.PoolCopies), afterRemaining);
            }
        }

        [Test]
        public void Apply_MountingAvalancheRejectsMissingAndTavernTargetsWithoutConsumingSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            AddSpellToHand(service, "122862");
            var spell = tavern.Hand.Single();
            var castsThisTurn = tavern.TavernSpellsCastThisTurn;
            var castsThisGame = tavern.TavernSpellsCastThisGame;

            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, 0)));
            Assert.AreSame(spell, tavern.Hand.Single());

            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var shopTarget = tavern.Shop[shopIndex];
            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                shopIndex,
                TargetZone.TavernShop,
                -1,
                TargetZone.Unspecified,
                shopTarget.InstanceId)));

            Assert.AreSame(spell, tavern.Hand.Single());
            Assert.AreSame(shopTarget, tavern.Shop[shopIndex]);
            Assert.AreEqual(castsThisTurn, tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(castsThisGame, tavern.TavernSpellsCastThisGame);
        }

        [Test]
        public void Apply_MountingAvalancheUsesCanonicalSellTransactionAndBuffsLeftmostRemainingElemental()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var poolTemplate = tavern.Shop.First(card =>
                card != null &&
                card.CardKind == CardKind.Minion &&
                card.PoolCopiesHeld > 0 &&
                tavern.Pool.ContainsKey(card.DefinitionId));
            var sold = CreateSellLifecycleMinion(poolTemplate, "avalanche-sold", 7, 11, Tribe.None, true);
            sold.Health = 4;
            sold.EffectIds.Add("minion_sold_gain_gold_1");
            var nonElemental = CreateSellLifecycleMinion(poolTemplate, "avalanche-non-elemental", 2, 3, Tribe.None);
            var leftElemental = CreateSellLifecycleMinion(poolTemplate, "avalanche-left-elemental", 5, 6, Tribe.Elemental);
            var rightElemental = CreateSellLifecycleMinion(poolTemplate, "avalanche-right-elemental", 8, 9, Tribe.Elemental);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(sold);
            service.State.Player.Board.Add(nonElemental);
            service.State.Player.Board.Add(leftElemental);
            service.State.Player.Board.Add(rightElemental);
            service.State.Player.HeroPowerCardId = "TB_BaconShop_HP_008";
            tavern.AdvancedMechanics.Quests.MainQuest = new ActiveQuestState
            {
                RewardId = "BG24_Reward_305",
                Completed = true,
                RewardActive = true
            };
            tavern.AdvancedMechanics.Trinkets.GreaterTrinketId = "BG35_MagicItem_863";
            tavern.Gold = 0;
            tavern.MaxGold = 10;
            AddSpellToHand(service, "122862");

            var poolBefore = tavern.Pool[sold.DefinitionId];
            var shopAttackBefore = tavern.Shop.Where(card => card?.CardKind == CardKind.Minion).Sum(card => card.Attack);
            var shopHealthBefore = tavern.Shop.Where(card => card?.CardKind == CardKind.Minion).Sum(card => card.MaxHealth);
            var soldAttackBefore = tavern.SoldThisTurnAttack;
            var soldHealthBefore = tavern.SoldThisTurnHealth;
            var sellLogsBefore = tavern.RecruitLog.Count(entry => entry.Type == RecruitLogType.Sell);
            var castsThisTurn = tavern.TavernSpellsCastThisTurn;
            var castsThisGame = tavern.TavernSpellsCastThisGame;
            var cardsPlayed = tavern.CardsPlayedThisTurn;
            var nextTurnGold = tavern.NextTurnBonusGold;
            var trinketSells = tavern.AdvancedMechanics.Trinkets.AvalancheStickerSoldMinions;
            var leftAttack = leftElemental.Attack;
            var leftHealth = leftElemental.MaxHealth;
            var rightAttack = rightElemental.Attack;
            var rightHealth = rightElemental.MaxHealth;
            var nonElementalAttack = nonElemental.Attack;
            var nonElementalHealth = nonElemental.MaxHealth;

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                sold.InstanceId));

            Assert.IsFalse(service.State.Player.Board.Contains(sold));
            Assert.AreEqual(3, service.State.Player.Board.Count);
            Assert.AreSame(nonElemental, service.State.Player.Board[0]);
            Assert.AreSame(leftElemental, service.State.Player.Board[1]);
            Assert.AreSame(rightElemental, service.State.Player.Board[2]);
            Assert.AreEqual(leftAttack + 7, leftElemental.Attack);
            Assert.AreEqual(leftHealth + 11, leftElemental.MaxHealth);
            Assert.AreEqual(leftElemental.MaxHealth, leftElemental.Health);
            Assert.AreEqual(rightAttack, rightElemental.Attack);
            Assert.AreEqual(rightHealth, rightElemental.MaxHealth);
            Assert.AreEqual(nonElementalAttack, nonElemental.Attack);
            Assert.AreEqual(nonElementalHealth, nonElemental.MaxHealth);
            Assert.AreEqual(2, tavern.Gold);
            Assert.AreEqual(soldAttackBefore + 7, tavern.SoldThisTurnAttack);
            Assert.AreEqual(soldHealthBefore + 11, tavern.SoldThisTurnHealth);
            Assert.AreEqual(poolBefore + sold.PoolCopiesHeld, tavern.Pool[sold.DefinitionId]);
            Assert.AreEqual(sellLogsBefore + 1, tavern.RecruitLog.Count(entry => entry.Type == RecruitLogType.Sell));
            Assert.AreEqual(shopAttackBefore + 7, tavern.Shop.Where(card => card?.CardKind == CardKind.Minion).Sum(card => card.Attack));
            Assert.AreEqual(shopHealthBefore + 11, tavern.Shop.Where(card => card?.CardKind == CardKind.Minion).Sum(card => card.MaxHealth));
            Assert.AreEqual(nextTurnGold + 1, tavern.NextTurnBonusGold);
            Assert.AreEqual(trinketSells + 1, tavern.AdvancedMechanics.Trinkets.AvalancheStickerSoldMinions);
            Assert.AreEqual(castsThisTurn + 1, tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(castsThisGame + 1, tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(cardsPlayed + 1, tavern.CardsPlayedThisTurn);
            Assert.AreEqual(0, tavern.Hand.Count);
        }

        [Test]
        public void Apply_ChannelTheDevourerRejectsMissingAndTavernTargetsWithoutConsumingSpell()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            AddSpellToHand(service, "100899");
            var spell = tavern.Hand.Single();
            var castsThisTurn = tavern.TavernSpellsCastThisTurn;
            var castsThisGame = tavern.TavernSpellsCastThisGame;

            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, 0)));
            Assert.AreSame(spell, tavern.Hand.Single());

            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var shopTarget = tavern.Shop[shopIndex];
            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                shopIndex,
                TargetZone.TavernShop,
                -1,
                TargetZone.Unspecified,
                shopTarget.InstanceId)));

            Assert.AreSame(spell, tavern.Hand.Single());
            Assert.AreSame(shopTarget, tavern.Shop[shopIndex]);
            Assert.AreEqual(castsThisTurn, tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(castsThisGame, tavern.TavernSpellsCastThisGame);
        }

        [Test]
        public void Apply_ChannelTheDevourerUsesCanonicalSellTransactionAndTransfersStatsToExactlyOneRemainingMinion()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var poolTemplate = tavern.Shop.First(card =>
                card != null &&
                card.CardKind == CardKind.Minion &&
                card.PoolCopiesHeld > 0 &&
                tavern.Pool.ContainsKey(card.DefinitionId));
            var sold = CreateSellLifecycleMinion(poolTemplate, "devourer-sold", 7, 11, Tribe.None, true);
            sold.Health = 4;
            sold.EffectIds.Add("minion_sold_gain_gold_1");
            var receiver = CreateSellLifecycleMinion(poolTemplate, "devourer-receiver", 2, 3, Tribe.Demon);
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(sold);
            service.State.Player.Board.Add(receiver);
            service.State.Player.HeroPowerCardId = "TB_BaconShop_HP_008";
            tavern.AdvancedMechanics.Quests.MainQuest = new ActiveQuestState
            {
                RewardId = "BG24_Reward_305",
                Completed = true,
                RewardActive = true
            };
            tavern.AdvancedMechanics.Trinkets.GreaterTrinketId = "BG35_MagicItem_863";
            tavern.Gold = 0;
            tavern.MaxGold = 10;
            AddSpellToHand(service, "100899");

            var poolBefore = tavern.Pool[sold.DefinitionId];
            var shopAttackBefore = tavern.Shop.Where(card => card?.CardKind == CardKind.Minion).Sum(card => card.Attack);
            var shopHealthBefore = tavern.Shop.Where(card => card?.CardKind == CardKind.Minion).Sum(card => card.MaxHealth);
            var soldAttackBefore = tavern.SoldThisTurnAttack;
            var soldHealthBefore = tavern.SoldThisTurnHealth;
            var sellLogsBefore = tavern.RecruitLog.Count(entry => entry.Type == RecruitLogType.Sell);
            var castsThisTurn = tavern.TavernSpellsCastThisTurn;
            var castsThisGame = tavern.TavernSpellsCastThisGame;
            var cardsPlayed = tavern.CardsPlayedThisTurn;
            var nextTurnGold = tavern.NextTurnBonusGold;
            var trinketSells = tavern.AdvancedMechanics.Trinkets.AvalancheStickerSoldMinions;
            var receiverAttack = receiver.Attack;
            var receiverHealth = receiver.MaxHealth;

            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                0,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                sold.InstanceId));

            Assert.AreEqual(1, service.State.Player.Board.Count);
            Assert.AreSame(receiver, service.State.Player.Board[0]);
            Assert.AreEqual(receiverAttack + 7, receiver.Attack);
            Assert.AreEqual(receiverHealth + 11, receiver.MaxHealth);
            Assert.AreEqual(receiver.MaxHealth, receiver.Health);
            Assert.AreEqual(2, tavern.Gold);
            Assert.AreEqual(soldAttackBefore + 7, tavern.SoldThisTurnAttack);
            Assert.AreEqual(soldHealthBefore + 11, tavern.SoldThisTurnHealth);
            Assert.AreEqual(poolBefore + sold.PoolCopiesHeld, tavern.Pool[sold.DefinitionId]);
            Assert.AreEqual(sellLogsBefore + 1, tavern.RecruitLog.Count(entry => entry.Type == RecruitLogType.Sell));
            Assert.AreEqual(shopAttackBefore + 7, tavern.Shop.Where(card => card?.CardKind == CardKind.Minion).Sum(card => card.Attack));
            Assert.AreEqual(shopHealthBefore + 11, tavern.Shop.Where(card => card?.CardKind == CardKind.Minion).Sum(card => card.MaxHealth));
            Assert.AreEqual(nextTurnGold + 1, tavern.NextTurnBonusGold);
            Assert.AreEqual(trinketSells + 1, tavern.AdvancedMechanics.Trinkets.AvalancheStickerSoldMinions);
            Assert.AreEqual(castsThisTurn + 1, tavern.TavernSpellsCastThisTurn);
            Assert.AreEqual(castsThisGame + 1, tavern.TavernSpellsCastThisGame);
            Assert.AreEqual(cardsPlayed + 1, tavern.CardsPlayedThisTurn);
            Assert.AreEqual(0, tavern.Hand.Count);
        }

        [Test]
        public void Apply_GeneratedSpellsAreNormalSpellsAndIgnoreTavernSpellPower()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            service.State.Player.Tavern.TavernSpellBonusAttack = 3;
            AddGeneratedSpellToHand(service, "BLOOD_GEM");

            Assert.AreEqual(CardKind.Spell, service.State.Player.Tavern.Hand[0].CardKind);
            Assert.IsFalse(service.State.Player.Tavern.Hand[0].Keywords.Contains(Keyword.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(target.BaseAttack + 1, service.State.Player.Board[0].Attack);
            Assert.AreEqual(target.BaseHealth + 1, service.State.Player.Board[0].MaxHealth);
        }

        [Test]
        public void Apply_TavernSpellsUseTavernSpellPower()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            service.State.Player.Tavern.TavernSpellBonusAttack = 3;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(target.BaseAttack + 7, service.State.Player.Board[0].Attack);
        }

        [Test]
        public void Apply_TierTwoTavernSpellsResolveRefreshHealthCostAndLockedDiscover()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Tier = 2;
            service.State.Player.Tavern.Gold = 0;
            AddSpellToHand(service, "104446");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));
            Assert.AreEqual(2, service.State.Player.Tavern.FreeRefreshes);

            service.Apply(new GameCommand(GameCommandType.RerollShop));
            Assert.AreEqual(0, service.State.Player.Tavern.Gold);
            Assert.AreEqual(1, service.State.Player.Tavern.FreeRefreshes);

            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Gold = 0;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "104559", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.AreEqual(1, service.State.Player.Tavern.Gold);

            service.State.Player.Tavern.Hand.Clear();
            AddSpellToHand(service, "127288");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            Assert.AreEqual(2, service.State.Player.Tavern.Discover.RewardTier);
            Assert.IsTrue(service.State.Player.Tavern.Discover.Options.All(option => option.TavernTier == 2));
            service.Apply(new GameCommand(GameCommandType.ChooseDiscover, 0));
            Assert.Throws<InvalidOperationException>(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1)));
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.DoesNotThrow(() => service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1)));
        }

        [Test]
        public void Apply_BuyHastyExcavationCostsHealthInsteadOfGold()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "104559", CardKind.TavernSpell));
            var hasty = service.State.Player.Tavern.Hand[0];
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop[0] = hasty;
            service.State.Player.Tavern.Gold = 0;
            service.State.Player.Health = 30;

            service.Apply(new GameCommand(GameCommandType.BuyMinion, 0));

            Assert.AreEqual(0, service.State.Player.Tavern.Gold);
            Assert.AreEqual(27, service.State.Player.Health);
            Assert.AreEqual("104559", service.State.Player.Tavern.Hand.Last().CardId);
        }

        [Test]
        public void Apply_ChefsChoiceGetsAnotherMinionOfTargetsType()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_800", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.State.Player.Tavern.Hand.Clear();
            AddSpellToHand(service, "105664");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Beast)));
        }

        [Test]
        public void Apply_PointyArrowAndSlimyShieldCanTargetTavernMinions()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var target = service.State.Player.Tavern.Shop[shopIndex];
            var beforeAttack = target.Attack;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));

            Assert.AreEqual(beforeAttack + 4, target.Attack);

            AddGeneratedSpellToHand(service, "SLIMY_SHIELD");
            var beforeHealth = target.MaxHealth;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));
            Assert.AreEqual(beforeAttack + 5, target.Attack);
            Assert.AreEqual(beforeHealth + 1, target.MaxHealth);
            Assert.IsTrue(target.Keywords.Contains(Keyword.Taunt));
        }

        [Test]
        public void Apply_GoldenArrowTargetsTavernOnly()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            AddBoardTarget(service);
            AddSpellToHand(service, "100596");
            var arrow = service.State.Player.Tavern.Hand[0];
            arrow.Golden = true;
            arrow.Tags.Add("anomaly_golden_arrow");

            Assert.Throws<InvalidOperationException>(() =>
                service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified)));

            var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var beforeAttack = service.State.Player.Tavern.Shop[shopIndex].Attack;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));
            Assert.AreEqual(beforeAttack + 8, service.State.Player.Tavern.Shop[shopIndex].Attack);
        }

        [Test]
        public void Apply_KidnapSackMovesSelectedBoardOrTavernMinionToHand()
        {
            var boardService = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var boardTarget = AddBoardTarget(boardService);
            AddGeneratedSpellToHand(boardService, "BG24_Reward_718t");
            boardService.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));
            Assert.IsFalse(boardService.State.Player.Board.Contains(boardTarget));
            Assert.IsTrue(boardService.State.Player.Tavern.Hand.Contains(boardTarget));

            var shopService = MatchService.CreateWithDefaultCatalog(54321, new InMemoryTestScenarioRepository());
            var shopIndex = shopService.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var shopTarget = shopService.State.Player.Tavern.Shop[shopIndex];
            AddGeneratedSpellToHand(shopService, "BG24_Reward_718t");
            shopService.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));
            Assert.IsTrue(shopService.State.Player.Tavern.Hand.Contains(shopTarget));
        }

        [Test]
        public void Apply_TimewarpedSummonerUsesAllTargetTribesForTavernPool()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            target.Tribes = new System.Collections.Generic.List<Tribe> { Tribe.Dragon, Tribe.Murloc };
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Hand.Add(new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "timewarped-summoner-test",
                DefinitionId = "TIMEWARPED_SUMMONER_SPELL",
                CardId = "TIMEWARPED_SUMMONER_SPELL",
                Name = "Timewarped Summoner",
                Owner = BoardSide.Player,
                Tags = new System.Collections.Generic.List<string> { "generated_spell", "spellcraft", "targeted_spell" }
            });

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.IsTrue(service.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .All(card => card.Tribes.Contains(Tribe.All) || card.Tribes.Contains(Tribe.Dragon) || card.Tribes.Contains(Tribe.Murloc)));
        }

        [Test]
        public void Apply_NaturalBlessingUsesAllTargetTribesWithoutDoubleBuffing()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var target = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion).Clone();
            target.InstanceId = "natural-target";
            target.Tribes = new System.Collections.Generic.List<Tribe> { Tribe.Dragon, Tribe.Murloc };
            var dualMatch = target.Clone();
            dualMatch.InstanceId = "natural-dual-match";
            var beast = target.Clone();
            beast.InstanceId = "natural-beast";
            beast.Tribes = new System.Collections.Generic.List<Tribe> { Tribe.Beast };
            service.State.Player.Board.Add(target);
            service.State.Player.Board.Add(dualMatch);
            service.State.Player.Board.Add(beast);
            var beforeAttack = dualMatch.Attack;
            AddSpellToHand(service, "104472");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(beforeAttack + 3, dualMatch.Attack);
            Assert.AreEqual(beast.BaseAttack, beast.Attack);
        }

        [Test]
        public void Apply_SelfishBountyIsNonTargetedAndBuffsLeftmost()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var left = AddBoardTarget(service);
            var right = left.Clone();
            right.InstanceId = "selfish-right";
            service.State.Player.Board.Add(right);
            var leftAttack = left.Attack;
            var rightAttack = right.Attack;
            AddSpellToHand(service, "122184");

            Assert.IsFalse(service.State.Player.Tavern.Hand[0].Tags.Contains("targeted_spell"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(leftAttack + 6, left.Attack);
            Assert.AreEqual(rightAttack, right.Attack);
        }

        [Test]
        public void Apply_TimeManagementPreservesEachDelayedCastUntilNextTurn()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var target = AddBoardTarget(service);
            target.DefinitionId = "time-management-target";
            target.CardId = "TIME_MANAGEMENT_TARGET";
            target.Name = "Time Management Target";
            target.Tribes.Clear();
            target.Keywords.Clear();
            target.OfficialKeywords.Clear();
            target.Tags.Clear();
            target.EffectIds.Clear();
            target.Counters.Clear();
            var baseAttack = target.Attack;
            var baseHealth = target.MaxHealth;

            service.State.Player.Tavern.TavernSpellBonusAttack = 1;
            service.State.Player.Tavern.TavernSpellBonusHealth = 2;
            AddSpellToHand(service, "117573");
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                -1,
                TargetZone.Unspecified,
                -1,
                TargetZone.Unspecified,
                choiceId: "immediate"));

            Assert.AreEqual(baseAttack + 3, target.Attack);
            Assert.AreEqual(baseHealth + 4, target.MaxHealth);

            service.State.Player.Tavern.TavernSpellBonusAttack = 1;
            service.State.Player.Tavern.TavernSpellBonusHealth = 0;
            AddSpellToHand(service, "117573");
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                -1,
                TargetZone.Unspecified,
                -1,
                TargetZone.Unspecified,
                choiceId: "delayed"));

            service.State.Player.Tavern.TavernSpellBonusAttack = 4;
            service.State.Player.Tavern.TavernSpellBonusHealth = 3;
            AddSpellToHand(service, "117573");
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                -1,
                TargetZone.Unspecified,
                -1,
                TargetZone.Unspecified,
                choiceId: "next_turn"));

            Assert.AreEqual(baseAttack + 3, target.Attack);
            Assert.AreEqual(baseHealth + 4, target.MaxHealth);
            Assert.AreEqual(4, service.State.Player.Tavern.PendingTimeManagementEnchantments.Count);
            var combatClone = service.State.Player.Tavern.CloneForCombat();
            combatClone.PendingTimeManagementEnchantments.Clear();
            Assert.AreEqual(4, service.State.Player.Tavern.PendingTimeManagementEnchantments.Count);

            service.State.Player.Tavern.TavernSpellBonusAttack = 20;
            service.State.Player.Tavern.TavernSpellBonusHealth = 20;
            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(baseAttack + 21, target.Attack);
            Assert.AreEqual(baseHealth + 18, target.MaxHealth);
            CollectionAssert.AreEqual(
                new[] { 3, 3, 3, 6, 6 },
                target.Enchantments.Where(enchantment => enchantment.SourceId == "Time Management").Select(enchantment => enchantment.AttackBonus));
            CollectionAssert.AreEqual(
                new[] { 4, 2, 2, 5, 5 },
                target.Enchantments.Where(enchantment => enchantment.SourceId == "Time Management").Select(enchantment => enchantment.HealthBonus));
            Assert.IsEmpty(service.State.Player.Tavern.PendingTimeManagementEnchantments);

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(baseAttack + 21, target.Attack);
            Assert.AreEqual(baseHealth + 18, target.MaxHealth);
        }

        [Test]
        public void Apply_HauntedCarapacePropagatesAcrossOwnedZonesExactlyOnce()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var boardTarget = AddBoardTarget(service);
            boardTarget.DefinitionId = "carapace-board";
            boardTarget.CardId = "CARAPACE_BOARD";
            boardTarget.Name = "Carapace Board";
            boardTarget.BaseAttack = 4;
            boardTarget.BaseHealth = 8;
            boardTarget.Attack = 4;
            boardTarget.MaxHealth = 8;
            boardTarget.Health = 8;
            boardTarget.Tribes.Clear();
            boardTarget.Keywords.Clear();
            boardTarget.OfficialKeywords.Clear();
            boardTarget.Tags.Clear();
            boardTarget.EffectIds.Clear();
            boardTarget.Counters.Clear();
            boardTarget.Enchantments.Clear();

            tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_883", CardKind.Minion));
            var handTarget = tavern.Hand.Single(card => card.CardKind == CardKind.Minion);
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            Assert.GreaterOrEqual(shopIndex, 0);
            var shopTarget = tavern.Shop[shopIndex];
            var boardAttack = boardTarget.Attack;
            var boardHealth = boardTarget.MaxHealth;
            var handAttack = handTarget.Attack;
            var handHealth = handTarget.MaxHealth;
            var shopAttack = shopTarget.Attack;
            var shopHealth = shopTarget.MaxHealth;

            tavern.TavernSpellBonusAttack = 2;
            tavern.TavernSpellBonusHealth = 3;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "122489", CardKind.TavernSpell));
            var spellIndex = tavern.Hand.FindIndex(card => card.CardKind == CardKind.TavernSpell);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, spellIndex));

            Assert.AreEqual(5, tavern.TemporaryCarapaceAttack);
            Assert.AreEqual(4, tavern.TemporaryCarapaceHealth);
            Assert.AreEqual(boardAttack + 5, boardTarget.Attack);
            Assert.AreEqual(boardHealth + 4, boardTarget.MaxHealth);
            Assert.AreEqual(handAttack + 5, handTarget.Attack);
            Assert.AreEqual(handHealth + 4, handTarget.MaxHealth);
            Assert.AreEqual(shopAttack + 5, shopTarget.Attack);
            Assert.AreEqual(shopHealth + 4, shopTarget.MaxHealth);
            AssertHauntedCarapaceBonus(boardTarget, 5, 4, 1);
            AssertHauntedCarapaceBonus(handTarget, 5, 4, 1);
            AssertHauntedCarapaceBonus(shopTarget, 5, 4, 1);

            tavern.MaxGold = 10;
            tavern.Gold = 10;
            var shopTargetId = shopTarget.InstanceId;
            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));
            var boughtTarget = tavern.Hand.Single(card => card.InstanceId == shopTargetId);
            Assert.AreEqual(shopAttack + 5, boughtTarget.Attack);
            Assert.AreEqual(shopHealth + 4, boughtTarget.MaxHealth);
            AssertHauntedCarapaceBonus(boughtTarget, 5, 4, 1);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_883", CardKind.Minion));
            var addedTarget = tavern.Hand.Last();
            Assert.AreEqual(addedTarget.BaseAttack + 5, addedTarget.Attack);
            Assert.AreEqual(addedTarget.BaseHealth + 4, addedTarget.MaxHealth);
            AssertHauntedCarapaceBonus(addedTarget, 5, 4, 1);

            tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            var refreshedMinions = tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            Assert.IsNotEmpty(refreshedMinions);
            foreach (var refreshed in refreshedMinions)
            {
                AssertHauntedCarapaceBonus(refreshed, 5, 4, 1);
            }
        }

        [Test]
        public void NextTurn_HauntedCarapaceExpiresAcrossOwnedZonesAndPreservesDamage()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var tavern = service.State.Player.Tavern;
            var boardTarget = AddBoardTarget(service);
            boardTarget.DefinitionId = "carapace-expiry-board";
            boardTarget.CardId = "CARAPACE_EXPIRY_BOARD";
            boardTarget.Name = "Carapace Expiry Board";
            boardTarget.BaseAttack = 4;
            boardTarget.BaseHealth = 8;
            boardTarget.Attack = 4;
            boardTarget.MaxHealth = 8;
            boardTarget.Health = 6;
            boardTarget.Tribes.Clear();
            boardTarget.Keywords.Clear();
            boardTarget.OfficialKeywords.Clear();
            boardTarget.Tags.Clear();
            boardTarget.EffectIds.Clear();
            boardTarget.Counters.Clear();
            boardTarget.Enchantments.Clear();

            tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_883", CardKind.Minion));
            var handTarget = tavern.Hand.Single(card => card.CardKind == CardKind.Minion);
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            Assert.GreaterOrEqual(shopIndex, 0);
            var shopTarget = tavern.Shop[shopIndex];
            var shopTargetId = shopTarget.InstanceId;
            var handAttack = handTarget.Attack;
            var handHealth = handTarget.MaxHealth;
            var shopAttack = shopTarget.Attack;
            var shopHealth = shopTarget.MaxHealth;

            tavern.TavernSpellBonusAttack = 1;
            tavern.TavernSpellBonusHealth = 2;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "122489", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.FindIndex(card => card.CardKind == CardKind.TavernSpell)));
            tavern.TavernSpellBonusAttack = 3;
            tavern.TavernSpellBonusHealth = 0;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "122489", CardKind.TavernSpell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.FindIndex(card => card.CardKind == CardKind.TavernSpell)));

            Assert.AreEqual(10, tavern.TemporaryCarapaceAttack);
            Assert.AreEqual(4, tavern.TemporaryCarapaceHealth);
            AssertHauntedCarapaceBonus(boardTarget, 10, 4, 2);
            AssertHauntedCarapaceBonus(handTarget, 10, 4, 2);
            AssertHauntedCarapaceBonus(shopTarget, 10, 4, 2);

            service.Apply(new GameCommand(GameCommandType.FreezeShop, null, true));
            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            var retainedShopTarget = tavern.Shop.Single(card => card != null && card.InstanceId == shopTargetId);
            Assert.AreEqual(0, tavern.TemporaryCarapaceAttack);
            Assert.AreEqual(0, tavern.TemporaryCarapaceHealth);
            AssertHauntedCarapaceBonus(boardTarget, 0, 0, 0);
            AssertHauntedCarapaceBonus(handTarget, 0, 0, 0);
            AssertHauntedCarapaceBonus(retainedShopTarget, 0, 0, 0);
            Assert.AreEqual(4, boardTarget.Attack);
            Assert.AreEqual(8, boardTarget.MaxHealth);
            Assert.AreEqual(6, boardTarget.Health);
            Assert.AreEqual(handAttack, handTarget.Attack);
            Assert.AreEqual(handHealth, handTarget.MaxHealth);
            Assert.AreEqual(shopAttack, retainedShopTarget.Attack);
            Assert.AreEqual(shopHealth, retainedShopTarget.MaxHealth);
        }

        [Test]
        public void Apply_DeepBlueAndBackToBackUseIndependentGlobalCountersAndPerCastHistory()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var firstTarget = AddBoardTarget(service);
            var secondTarget = firstTarget.Clone();
            secondTarget.InstanceId = "second-spell-target";
            service.State.Player.Board.Add(secondTarget);
            AddGeneratedSpellToHand(service, "DEEP_BLUE_SPELL");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.Greater(service.State.Player.Tavern.DeepBlueBonusAttack, 0);
            Assert.AreEqual(0, service.State.Player.Tavern.BackToBackBonus);

            var firstAttackBeforeBackToBack = firstTarget.Attack;
            var firstHealthBeforeBackToBack = firstTarget.MaxHealth;
            service.State.Player.Tavern.TavernSpellBonusAttack = 1;
            service.State.Player.Tavern.TavernSpellBonusHealth = 3;
            AddSpellToHand(service, "131153");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(firstAttackBeforeBackToBack + 5, firstTarget.Attack);
            Assert.AreEqual(firstHealthBeforeBackToBack + 7, firstTarget.MaxHealth);
            Assert.AreEqual(5, service.State.Player.Tavern.BackToBackAttackBonus);
            Assert.AreEqual(7, service.State.Player.Tavern.BackToBackHealthBonus);

            var secondAttackBeforeBackToBack = secondTarget.Attack;
            var secondHealthBeforeBackToBack = secondTarget.MaxHealth;
            service.State.Player.Tavern.TavernSpellBonusAttack = 2;
            service.State.Player.Tavern.TavernSpellBonusHealth = 0;
            AddSpellToHand(service, "131153");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 1, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.Greater(service.State.Player.Tavern.DeepBlueBonusAttack, 0);
            Assert.AreEqual(secondAttackBeforeBackToBack + 11, secondTarget.Attack);
            Assert.AreEqual(secondHealthBeforeBackToBack + 11, secondTarget.MaxHealth);
            Assert.AreEqual(11, service.State.Player.Tavern.BackToBackAttackBonus);
            Assert.AreEqual(11, service.State.Player.Tavern.BackToBackHealthBonus);
            Assert.AreEqual(11, service.State.Player.Tavern.BackToBackBonus);
        }

        [Test]
        public void Apply_BelindaRepeatsFriendlyTargetedSpellsIncludingTavernTargets()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var boardTarget = AddBoardTarget(service);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_883", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            var boardAttack = boardTarget.Attack;
            AddSpellToHand(service, "100596");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));
            Assert.AreEqual(boardAttack + 8, boardTarget.Attack);

            AddSpellToHand(service, "100596");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));
            Assert.AreEqual(boardAttack + 16, boardTarget.Attack);

            var shopIndex = service.State.Player.Tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            var shopTarget = service.State.Player.Tavern.Shop[shopIndex];
            var shopAttack = shopTarget.Attack;
            AddSpellToHand(service, "100596");
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, shopIndex, TargetZone.TavernShop, -1, TargetZone.Unspecified));
            Assert.AreEqual(shopAttack + 8, shopTarget.Attack);
        }

        [Test]
        public void Apply_GoldenBelindaMakesFriendlyTargetedSpellCastThreeTimes()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            var boardTarget = AddBoardTarget(service);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_883", CardKind.Minion));
            service.State.Player.Tavern.Hand[0].Golden = true;
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var beforeAttack = boardTarget.Attack;
            AddSpellToHand(service, "100596");

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0, TargetZone.FriendlyBoard, -1, TargetZone.Unspecified));

            Assert.AreEqual(beforeAttack + 12, boardTarget.Attack);
        }

        [Test]
        public void Apply_BelindaRepeatsDestructiveButcheringByResolvingNextUndeadTarget()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            var first = AddBoardTarget(service);
            first.InstanceId = "butchering-first";
            first.Tribes.Clear();
            first.Tribes.Add(Tribe.Undead);
            var second = first.Clone();
            second.InstanceId = "butchering-second";
            service.State.Player.Board.Add(second);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG35_883", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));

            AddSpellToHand(service, "110412");
            var targetIndex = service.State.Player.Board.FindIndex(card => card.InstanceId == first.InstanceId);
            Assert.GreaterOrEqual(targetIndex, 0);
            Assert.DoesNotThrow(() => service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                targetIndex,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                first.InstanceId)));

            Assert.IsFalse(service.State.Player.Board.Any(card => card.InstanceId == first.InstanceId));
            Assert.IsFalse(service.State.Player.Board.Any(card => card.InstanceId == second.InstanceId));
            Assert.AreEqual(10, service.State.Player.Tavern.ButcheringAttackBonus);
        }

        [Test]
        public void Apply_ButcheringHistoryPropagatesAcrossFutureUndeadExactlyOnce()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    ActiveTribes = new List<Tribe> { Tribe.Undead },
                    CardPoolVersionId = "butchering-regression",
                    IsDefaultCardPoolVersion = false,
                    EnabledMinionCardIds = new List<string> { "BG28_300", "BG25_013" },
                    EnabledTavernSpellCardNumbers = new List<string> { "110412" }
                });
            var tavern = service.State.Player.Tavern;
            service.State.Player.Board.Clear();
            tavern.Hand.Clear();

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG28_300", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var first = service.State.Player.Board.Single();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG28_300", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            tavern.TavernSpellBonusAttack = 2;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "110412", CardKind.TavernSpell));
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                0,
                service.State.Player.Board.FindIndex(card => card.InstanceId == first.InstanceId),
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                first.InstanceId));

            Assert.AreEqual(7, tavern.ButcheringAttackBonus);
            Assert.AreEqual(2, service.State.Player.Board.Count(card => card.Name == "Skeleton"));
            foreach (var undead in service.State.Player.Board.Where(card => card.Tribes.Contains(Tribe.Undead)))
            {
                AssertButcheringBonus(undead, 7);
            }

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG25_013", CardKind.Minion));
            var futureHand = tavern.Hand.FirstOrDefault(card => card.CardKind == CardKind.Minion);
            Assert.IsNotNull(futureHand, "Butchering future-hand phase did not create an Undead minion in hand.");
            AssertButcheringBonus(futureHand, 7);

            tavern.MaxGold = 10;
            tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            var shopIndex = tavern.Shop.FindIndex(card => card != null && card.CardKind == CardKind.Minion);
            Assert.GreaterOrEqual(shopIndex, 0);
            var futureShop = tavern.Shop[shopIndex];
            AssertButcheringBonus(futureShop, 7);
            var futureShopId = futureShop.InstanceId;
            service.Apply(new GameCommand(GameCommandType.BuyMinion, shopIndex));
            var bought = tavern.Hand.FirstOrDefault(card => card.InstanceId == futureShopId);
            Assert.IsNotNull(bought, "Butchering buy phase did not move the refreshed shop minion into hand.");
            AssertButcheringBonus(bought, 7);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, tavern.Hand.IndexOf(futureHand)));
            AssertButcheringBonus(futureHand, 7);

            tavern.TavernSpellBonusAttack = 0;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "110412", CardKind.TavernSpell));
            var secondTargetIndex = service.State.Player.Board.FindIndex(card => card.InstanceId == futureHand.InstanceId);
            service.Apply(new GameCommand(
                GameCommandType.PlayMinion,
                tavern.Hand.FindIndex(card => card.CardId == "110412"),
                secondTargetIndex,
                TargetZone.FriendlyBoard,
                -1,
                TargetZone.Unspecified,
                futureHand.InstanceId));

            Assert.AreEqual(12, tavern.ButcheringAttackBonus);
            foreach (var undead in service.State.Player.Board
                         .Concat(tavern.Hand)
                         .Concat(tavern.Shop)
                         .Where(card => card != null && card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Undead)))
            {
                AssertButcheringBonus(undead, 12);
            }

            tavern.Gold = 10;
            service.Apply(new GameCommand(GameCommandType.RerollShop));
            foreach (var undead in tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion))
            {
                AssertButcheringBonus(undead, 12);
            }
        }

        private static void AssertSpitescaleSpellcraftCard(MinionInstance card)
        {
            Assert.AreEqual(CardKind.Spell, card.CardKind);
            Assert.AreEqual(0, card.Cost);
            Assert.AreEqual(BoardSide.Player, card.Owner);
            Assert.AreEqual(PoolSource.Copy, card.PoolSource);
            CollectionAssert.Contains(card.Keywords, Keyword.Spellcraft);
            CollectionAssert.Contains(card.Tags, "generated_spell");
            CollectionAssert.Contains(card.Tags, "spellcraft");
            Assert.AreEqual(1, card.Tags.Count(tag => tag == "temporary_spellcraft_card"));

            switch (card.CardId)
            {
                case "REEF_RIFFER_SPELL":
                    Assert.AreEqual(1, card.Counters["spellcraft_multiplier"]);
                    break;
                case "SURF_N_SURF_SPELL":
                    Assert.AreEqual(3, card.Counters["crab_attack"]);
                    Assert.AreEqual(2, card.Counters["crab_health"]);
                    CollectionAssert.Contains(card.Tags, "deathrattle_grant");
                    break;
                case "DEEP_SEA_ANGLER_SPELL":
                    Assert.AreEqual(2, card.Counters["angler_attack"]);
                    Assert.AreEqual(6, card.Counters["angler_health"]);
                    CollectionAssert.Contains(card.Tags, "taunt_grant");
                    break;
                case "DEEP_BLUE_SPELL":
                    Assert.AreEqual(2, card.Counters["deep_blue_attack"]);
                    Assert.AreEqual(2, card.Counters["deep_blue_health"]);
                    Assert.AreEqual(1, card.Counters["deep_blue_growth"]);
                    break;
                case "VOLCANIC_VISITOR_ATTACK_SPELL":
                    Assert.AreEqual(4, card.Counters["spellcraft_amount"]);
                    CollectionAssert.Contains(card.Tags, "attack_buff_spell");
                    break;
                case "VOLCANIC_VISITOR_HEALTH_SPELL":
                    Assert.AreEqual(4, card.Counters["spellcraft_amount"]);
                    CollectionAssert.Contains(card.Tags, "health_buff_spell");
                    break;
                case "FROSTLING_PRIESTESS_SPELL":
                    Assert.AreEqual(1, card.Counters["spellcraft_multiplier"]);
                    CollectionAssert.Contains(card.Tags, "generated_tavern_spell");
                    CollectionAssert.Contains(card.Tags, "stat_tavern_spell");
                    break;
                default:
                    Assert.Fail("Unexpected Spitescale Spellcraft card: " + card.CardId);
                    break;
            }
        }

        private static MinionInstance AddBoardTarget(MatchService service)
        {
            service.State.Player.Board.Clear();
            var target = service.State.Player.Tavern.Shop.First(card => card.CardKind == CardKind.Minion).Clone();
            target.InstanceId = "spell-target";
            target.Owner = BoardSide.Player;
            service.State.Player.Board.Add(target);
            return target;
        }

        private static MinionInstance AddCorruptedCupcakesTarget(MatchService service, Tribe tribe)
        {
            var target = AddBoardTarget(service);
            target.DefinitionId = "cupcakes-target";
            target.CardId = "CUPCAKES_TARGET";
            target.InstanceId = "cupcakes-target";
            target.Name = "Cupcakes Target";
            target.BaseAttack = 2;
            target.BaseHealth = 3;
            target.Attack = 2;
            target.Health = 3;
            target.MaxHealth = 3;
            target.Golden = false;
            target.PoolSource = PoolSource.Debug;
            target.PoolCopiesHeld = 0;
            target.Tribes.Clear();
            target.Tribes.Add(tribe);
            target.Keywords.Clear();
            target.OfficialKeywords.Clear();
            target.Tags.Clear();
            target.EffectIds.Clear();
            target.Counters.Clear();
            target.Enchantments.Clear();
            return target;
        }

        private static MinionInstance CreateSellLifecycleMinion(
            MinionInstance template,
            string id,
            int attack,
            int health,
            Tribe tribe,
            bool preservePoolIdentity = false)
        {
            var minion = template.Clone();
            if (!preservePoolIdentity)
            {
                minion.DefinitionId = id;
                minion.PoolSource = PoolSource.Debug;
                minion.PoolCopiesHeld = 0;
            }

            minion.CardId = id;
            minion.InstanceId = id;
            minion.Name = id;
            minion.BaseAttack = attack;
            minion.BaseHealth = health;
            minion.Attack = attack;
            minion.Health = health;
            minion.MaxHealth = health;
            minion.Golden = false;
            minion.Owner = BoardSide.Player;
            minion.Tribes.Clear();
            minion.Tribes.Add(tribe);
            minion.Keywords.Clear();
            minion.OfficialKeywords.Clear();
            minion.Tags.Clear();
            minion.EffectIds.Clear();
            minion.Counters.Clear();
            minion.Enchantments.Clear();
            return minion;
        }

        private static void AddSpellToHand(MatchService service, string cardNumber)
        {
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardNumber, CardKind.TavernSpell));
        }

        private static void AddGeneratedSpellToHand(MatchService service, string cardNumber)
        {
            service.State.Player.Tavern.Hand.Clear();
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardNumber, CardKind.Spell));
        }

        private static void AssertHauntedCarapaceBonus(MinionInstance card, int attack, int health, int enchantmentCount)
        {
            var enchantments = card.Enchantments
                .Where(enchantment => enchantment != null && enchantment.SourceId == TavernSpellEngine.HauntedCarapaceSourceId)
                .ToList();
            Assert.AreEqual(enchantmentCount, enchantments.Count);
            Assert.AreEqual(attack, enchantments.Sum(enchantment => enchantment.AttackBonus));
            Assert.AreEqual(health, enchantments.Sum(enchantment => enchantment.HealthBonus));
        }

        private static void AssertButcheringBonus(MinionInstance card, int attack)
        {
            var actualAttackBonus = card.Enchantments
                .Where(enchantment => enchantment != null && enchantment.SourceId == "Butchering")
                .Sum(enchantment => enchantment.AttackBonus);
            Assert.AreEqual(
                attack,
                actualAttackBonus,
                $"Butchering bonus mismatch for {card.Name} ({card.CardId}/{card.InstanceId}); " +
                $"expected {attack}, actual {actualAttackBonus}, attack {card.Attack}, base {card.BaseAttack}");
            Assert.AreEqual(
                card.BaseAttack + attack,
                card.Attack,
                $"Butchering stat mismatch for {card.Name} ({card.CardId}/{card.InstanceId})");
        }
    }
}
