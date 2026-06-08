using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MatchServiceTests
    {
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

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_004", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            var weaver = service.State.Player.Board[0];

            Assert.AreEqual(3, weaver.Attack);
            Assert.AreEqual(5, weaver.MaxHealth);
            Assert.AreEqual(29, service.State.Player.Health);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG24_009", CardKind.Minion));
            var shopMinionsBefore = service.State.Player.Tavern.Shop.Count(card => card != null && card.CardKind == CardKind.Minion);
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            var picky = service.State.Player.Board.Last();

            Assert.Less(service.State.Player.Tavern.Shop.Count(card => card != null && card.CardKind == CardKind.Minion), shopMinionsBefore);
            Assert.Greater(picky.Attack, picky.BaseAttack);
            Assert.AreEqual(28, service.State.Player.Health);
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

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BGS_004", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "BG26_174", CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, service.State.Player.Tavern.Hand.Count - 1));
            Assert.AreEqual(29, service.State.Player.Health);
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
        public void Apply_PlayingGoldenMinionGrantsRewardCardThatDiscoversNextTier()
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
            Assert.AreEqual(7, service.State.Player.Tavern.Discover.RewardTier);
            Assert.AreEqual(3, service.State.Player.Tavern.Discover.Options.Count);
        }

        [Test]
        public void Apply_TripleRewardDiscoverCapsAtTierSeven()
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
            Assert.AreEqual(7, service.State.Player.Tavern.Discover.RewardTier);
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
    }
}
