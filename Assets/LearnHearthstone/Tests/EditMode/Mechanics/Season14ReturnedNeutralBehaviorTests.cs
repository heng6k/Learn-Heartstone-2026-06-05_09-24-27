using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14ReturnedNeutralBehaviorTests
    {
        [TestCase(false, BoardSide.Player, 2)]
        [TestCase(true, BoardSide.Player, 4)]
        [TestCase(false, BoardSide.Opponent, 2)]
        [TestCase(true, BoardSide.Opponent, 4)]
        public void MotleyPhalanx_DeathrattlePermanentlyBuffsOneMinionOfEachType(
            bool golden,
            BoardSide owner,
            int bonus)
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var board = owner == BoardSide.Player ? service.State.Player.Board : service.State.Opponent.Board;
            var opposingBoard = owner == BoardSide.Player ? service.State.Opponent.Board : service.State.Player.Board;
            var source = CreateCatalogMinion(service, "motley-source", golden, owner);
            source.Health = 1;
            var firstBeast = Minion("motley-beast-a", "MOTLEY_BEAST_A", 3, 20, Tribe.Beast, owner);
            var secondBeast = Minion("motley-beast-b", "MOTLEY_BEAST_B", 5, 20, Tribe.Beast, owner);
            var dragon = Minion("motley-dragon", "MOTLEY_DRAGON", 7, 20, Tribe.Dragon, owner);
            var typeless = Minion("motley-typeless", "MOTLEY_TYPELESS", 11, 20, Tribe.None, owner);
            board.Add(source);
            board.Add(firstBeast);
            board.Add(secondBeast);
            board.Add(dragon);
            board.Add(typeless);
            opposingBoard.Add(Minion("motley-wall", "MOTLEY_WALL", 50, 100, Tribe.None, Opposite(owner), Keyword.Taunt));

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = golden ? 8402 : 8401, SafetyLimit = 1 }));

            var retainedFirstBeast = board.Single(item => item.InstanceId == firstBeast.InstanceId);
            var retainedSecondBeast = board.Single(item => item.InstanceId == secondBeast.InstanceId);
            var retainedDragon = board.Single(item => item.InstanceId == dragon.InstanceId);
            var retainedTypeless = board.Single(item => item.InstanceId == typeless.InstanceId);
            CollectionAssert.AreEquivalent(
                new[] { 0, bonus },
                new[] { retainedFirstBeast.Attack - 3, retainedSecondBeast.Attack - 5 });
            CollectionAssert.AreEquivalent(
                new[] { 0, bonus },
                new[] { retainedFirstBeast.MaxHealth - 20, retainedSecondBeast.MaxHealth - 20 });
            Assert.AreEqual(7 + bonus, retainedDragon.Attack);
            Assert.AreEqual(20 + bonus, retainedDragon.MaxHealth);
            Assert.AreEqual(11, retainedTypeless.Attack);
            Assert.AreEqual(20, retainedTypeless.MaxHealth);

            var rewards = owner == BoardSide.Player
                ? service.State.LastResult.PlayerRewards
                : service.State.LastResult.OpponentRewards;
            Assert.AreEqual(2, rewards.Count(reward =>
                reward.Type == CombatRewardType.BuffOriginalFriendlyMinion &&
                reward.SourceCardId == "BG27_080"));
        }

        [Test]
        public void LegacyComposite_MotleyPhalanxDoesNotUsePreviewDeathrattle()
        {
            var service = CreateService(GameVersionIds.LegacyCompositeSandbox);
            var source = Minion("legacy-motley", "BG27_080", 2, 1, Tribe.All, BoardSide.Player, Keyword.Deathrattle);
            var beast = Minion("legacy-motley-beast", "LEGACY_MOTLEY_BEAST", 3, 20, Tribe.Beast);
            service.State.Player.Board.Add(source);
            service.State.Player.Board.Add(beast);
            service.State.Opponent.Board.Add(Minion("legacy-motley-wall", "LEGACY_MOTLEY_WALL", 50, 100, Tribe.None, BoardSide.Opponent, Keyword.Taunt));

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 8403, SafetyLimit = 1 }));

            Assert.AreEqual(3, beast.Attack);
            Assert.AreEqual(20, beast.MaxHealth);
            Assert.IsFalse(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.BuffOriginalFriendlyMinion &&
                reward.SourceCardId == "BG27_080"));
        }

        private static MatchService CreateService(string gameVersionId)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(gameVersionId, snapshot);
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableQuests = false,
                    EnableTrinkets = false,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                });
            service.State.Phase = MatchPhase.Tavern;
            service.State.ActiveTribes = new List<Tribe> { Tribe.Beast, Tribe.Dragon };
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Opponent.Hand.Clear();
            service.State.RecruitActionStates.Clear();
            service.State.MechanicEvents.Clear();
            return service;
        }

        private static MinionInstance CreateCatalogMinion(
            MatchService service,
            string suffix,
            bool golden,
            BoardSide owner)
        {
            var definition = service.Catalogs.Minions.All.Single(item => item.ResearchKey == "POOL-D27");
            return MinionFactory.Create(definition, owner, suffix, golden, PoolSource.Copy, 0);
        }

        private static BoardSide Opposite(BoardSide owner)
        {
            return owner == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
        }

        private static MinionInstance Minion(
            string instanceId,
            string cardId,
            int attack,
            int health,
            Tribe tribe,
            BoardSide owner = BoardSide.Player,
            Keyword keyword = Keyword.Trigger)
        {
            var keywords = keyword == Keyword.Trigger ? new List<Keyword>() : new List<Keyword> { keyword };
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = instanceId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = owner,
                CanAttack = true,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords,
                OfficialKeywords = new List<Keyword>(keywords),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }
    }
}
