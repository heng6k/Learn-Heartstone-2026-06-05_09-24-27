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
            Assert.AreEqual(TavernRules.GetShopSize(1), service.State.Player.Tavern.Shop.Count);
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
    }
}
