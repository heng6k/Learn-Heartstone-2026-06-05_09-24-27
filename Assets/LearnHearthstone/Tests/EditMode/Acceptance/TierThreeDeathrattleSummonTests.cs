using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TierThreeDeathrattleSummonTests
    {
        private const string HandlessForsakenCardId = "BG25_010";
        private const string BoneWatcherCardId = "BG30_125";
        private const string SlyRaptorCardId = "BG25_806";

        [Test]
        public void MinionCatalog_TierThreeDeathrattleSummonRepresentativesExist()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var handless = catalog.GetByCardId(HandlessForsakenCardId);
            var watcher = catalog.GetByCardId(BoneWatcherCardId);
            var raptor = catalog.GetByCardId(SlyRaptorCardId);

            Assert.AreEqual(3, handless.TavernTier);
            Assert.AreEqual(3, watcher.TavernTier);
            Assert.AreEqual(3, raptor.TavernTier);
            Assert.IsTrue(handless.InPool);
            Assert.IsTrue(watcher.InPool);
            Assert.IsTrue(raptor.InPool);
            Assert.That(handless.Keywords, Does.Contain(Keyword.Deathrattle));
            Assert.That(handless.Keywords, Does.Contain(Keyword.Reborn));
            Assert.That(watcher.Keywords, Does.Contain(Keyword.Deathrattle));
            Assert.That(raptor.Keywords, Does.Contain(Keyword.Deathrattle));
            Assert.That(raptor.Tribes, Does.Contain(Tribe.Beast));
        }

        [Test]
        public void SlyRaptor_BacklineDeathSummonDoesNotReplaceLivingNextAttacker()
        {
            var attacker = TestMinion("p-attacker", BoardSide.Player, 10, 30);
            var playerFillerA = TestMinion("p-filler-a", BoardSide.Player, 0, 30);
            var playerFillerB = TestMinion("p-filler-b", BoardSide.Player, 0, 30);
            var left = TestMinion("o-left", BoardSide.Opponent, 1, 30);
            var raptor = CardMinion("o-raptor", BoardSide.Opponent, SlyRaptorCardId, false, 1, 3, Keyword.Deathrattle, Keyword.Taunt);
            var right = TestMinion("o-right", BoardSide.Opponent, 1, 30);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, playerFillerA, playerFillerB }, new[] { left, raptor, right }, 3101, 2);
            var summon = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == "o-raptor");
            var tokenId = summon.SummonedEntityIds.Single();
            var token = summon.OpponentBoardSnapshot.Minions.Single(minion => minion.InstanceId == tokenId);
            var nextOpponentAttack = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Opponent);

            Assert.AreEqual(1, summon.OpponentBoardSnapshot.Minions.FindIndex(minion => minion.InstanceId == tokenId));
            Assert.AreEqual(6, token.Attack);
            Assert.AreEqual(6, token.Health);
            Assert.AreEqual(6, token.MaxHealth);
            Assert.That(token.Tribes, Does.Contain(Tribe.Beast));
            Assert.AreEqual(left.InstanceId, nextOpponentAttack.ActorId);
            Assert.IsFalse(result.Replay.Frames.Any(frame =>
                frame.EventType == CombatEventType.AttackPointerRetargeted &&
                frame.AttackPointerSide == BoardSide.Opponent &&
                frame.TargetId == tokenId));
        }

        [Test]
        public void GoldenSlyRaptor_SummonsTwelveTwelveBeast()
        {
            var attacker = TestMinion("p-attacker", BoardSide.Player, 20, 40);
            var raptor = CardMinion("o-golden-raptor", BoardSide.Opponent, SlyRaptorCardId, true, 2, 6, Keyword.Deathrattle, Keyword.Taunt);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker }, new[] { raptor }, 3106, 3);
            var summon = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == "o-golden-raptor");
            var token = summon.OpponentBoardSnapshot.Minions.Single(minion => summon.SummonedEntityIds.Contains(minion.InstanceId));

            Assert.AreEqual(12, token.Attack);
            Assert.AreEqual(12, token.Health);
            Assert.AreEqual(12, token.MaxHealth);
            Assert.That(token.Tribes, Does.Contain(Tribe.Beast));
        }

        [Test]
        public void HandlessForsaken_DeathrattleResolvesBeforeSourceRebornAndSummonedHandAttacksFirst()
        {
            var handless = CardMinion("p-handless", BoardSide.Player, HandlessForsakenCardId, false, 2, 1, Keyword.Deathrattle, Keyword.Reborn);
            var opponentA = TestMinion("o-attacker", BoardSide.Opponent, 1, 50);
            var opponentB = TestMinion("o-follower", BoardSide.Opponent, 1, 50);

            var result = CombatEngine.SimulateBasicCombat(new[] { handless }, new[] { opponentA, opponentB }, 3102, 4);
            var deathrattleIndex = result.Replay.Frames.FindIndex(frame => frame.EventType == CombatEventType.DeathrattleResolved);
            var summon = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == "p-handless");
            var rebornIndex = result.Replay.Frames.FindIndex(frame => frame.EventType == CombatEventType.RebornResolved);
            var handId = summon.SummonedEntityIds.Single();
            var hand = summon.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == handId);
            var nextPlayerAttack = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player);

            Assert.Less(deathrattleIndex, summon.Index);
            Assert.Less(summon.Index, rebornIndex);
            Assert.AreEqual(0, summon.PlayerBoardSnapshot.Minions.FindIndex(minion => minion.InstanceId == handId));
            Assert.AreEqual(2, hand.Attack);
            Assert.AreEqual(1, hand.Health);
            Assert.That(hand.Tribes, Does.Contain(Tribe.Undead));
            Assert.That(hand.Keywords, Does.Contain(Keyword.Reborn));
            Assert.AreEqual(handId, nextPlayerAttack.ActorId);
            Assert.IsFalse(result.Replay.Frames.Any(frame =>
                frame.Index < nextPlayerAttack.Index &&
                frame.EventType == CombatEventType.AttackPointerRetargeted &&
                frame.AttackPointerSide == BoardSide.Player));
        }

        [Test]
        public void BoneWatcher_BacklineDeathSummonsInOrderWithoutReplacingLivingNextAttacker()
        {
            var attacker = TestMinion("p-attacker", BoardSide.Player, 10, 30);
            var playerFillerA = TestMinion("p-filler-a", BoardSide.Player, 0, 30);
            var playerFillerB = TestMinion("p-filler-b", BoardSide.Player, 0, 30);
            var left = TestMinion("o-left", BoardSide.Opponent, 1, 30);
            var watcher = CardMinion("o-watcher", BoardSide.Opponent, BoneWatcherCardId, false, 3, 3, Keyword.Deathrattle, Keyword.Taunt);
            var right = TestMinion("o-right", BoardSide.Opponent, 1, 30);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, playerFillerA, playerFillerB }, new[] { left, watcher, right }, 3103, 2);
            var summonFrames = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == "o-watcher")
                .ToList();
            var tokenIds = summonFrames.SelectMany(frame => frame.SummonedEntityIds).ToList();
            var finalSummonBoard = summonFrames.Last().OpponentBoardSnapshot.Minions;
            var nextOpponentAttack = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Opponent);

            Assert.AreEqual(3, tokenIds.Count);
            Assert.AreEqual(tokenIds[0], finalSummonBoard[1].InstanceId);
            Assert.AreEqual(tokenIds[1], finalSummonBoard[2].InstanceId);
            Assert.AreEqual(tokenIds[2], finalSummonBoard[3].InstanceId);
            Assert.IsTrue(tokenIds.All(id => finalSummonBoard.Single(minion => minion.InstanceId == id).Attack == 1));
            Assert.IsTrue(tokenIds.All(id => finalSummonBoard.Single(minion => minion.InstanceId == id).Health == 1));
            Assert.AreEqual(left.InstanceId, nextOpponentAttack.ActorId);
            Assert.IsFalse(result.Replay.Frames.Any(frame =>
                frame.EventType == CombatEventType.AttackPointerRetargeted &&
                frame.AttackPointerSide == BoardSide.Opponent &&
                tokenIds.Contains(frame.TargetId)));
        }

        [Test]
        public void BoneWatcher_FullBoardRecordsTwoSummonOverflowEvents()
        {
            var watcher = CardMinion("p-watcher", BoardSide.Player, BoneWatcherCardId, false, 3, 3, Keyword.Deathrattle);
            var playerBoard = new List<MinionInstance> { watcher };
            playerBoard.AddRange(Enumerable.Range(0, 6).Select(index => TestMinion("p-filler-" + index, BoardSide.Player, 1, 30)));
            var opponent = TestMinion("o-wall", BoardSide.Opponent, 3, 50);

            var result = CombatEngine.SimulateBasicCombat(playerBoard, new[] { opponent }, 3104, 4);
            var summonFrames = result.Replay.Frames.Where(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == "p-watcher").ToList();
            var overflowFrames = result.Replay.Frames.Where(frame => frame.EventType == CombatEventType.SummonOverflowed && frame.ActorId == "p-watcher").ToList();
            var nextPlayerAttack = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player)
                .Skip(1)
                .First();

            Assert.AreEqual(1, summonFrames.Count);
            Assert.AreEqual(2, overflowFrames.Sum(frame => frame.SummonOverflowCount));
            Assert.That(overflowFrames.SelectMany(frame => frame.OverflowedEntityIds).Count(), Is.EqualTo(2));
            Assert.LessOrEqual(overflowFrames.Last().PlayerBoardSnapshot.Minions.Count, 7);
            Assert.AreEqual(summonFrames.Single().SummonedEntityIds.Single(), nextPlayerAttack.ActorId);
        }

        [Test]
        public void TierThreeMixedDeathrattlePressureRunStaysBounded()
        {
            var players = new List<MinionInstance>
            {
                CardMinion("p-handless", BoardSide.Player, HandlessForsakenCardId, false, 2, 1, Keyword.Deathrattle, Keyword.Reborn),
                CardMinion("p-raptor", BoardSide.Player, SlyRaptorCardId, false, 1, 3, Keyword.Deathrattle),
                CardMinion("p-watcher", BoardSide.Player, BoneWatcherCardId, false, 3, 3, Keyword.Deathrattle),
                TestMinion("p-shield", BoardSide.Player, 25, 25, Keyword.DivineShield),
                TestMinion("p-taunt", BoardSide.Player, 12, 18, Keyword.Taunt),
                TestMinion("p-reborn", BoardSide.Player, 8, 1, Keyword.Reborn),
                TestMinion("p-venom", BoardSide.Player, 1, 8, Keyword.Venomous)
            };
            var opponents = new List<MinionInstance>
            {
                TestMinion("o-cleaver", BoardSide.Opponent, 9, 9),
                CardMinion("o-watcher", BoardSide.Opponent, BoneWatcherCardId, false, 3, 3, Keyword.Deathrattle),
                TestMinion("o-wall", BoardSide.Opponent, 18, 20, Keyword.Taunt),
                CardMinion("o-raptor", BoardSide.Opponent, SlyRaptorCardId, false, 1, 3, Keyword.Deathrattle),
                TestMinion("o-shield", BoardSide.Opponent, 20, 20, Keyword.DivineShield),
                TestMinion("o-reborn", BoardSide.Opponent, 7, 1, Keyword.Reborn),
                TestMinion("o-venom", BoardSide.Opponent, 1, 8, Keyword.Venomous)
            };

            var result = CombatEngine.SimulateBasicCombat(players, opponents, 3105, 120);

            Assert.IsFalse(result.SafetyStopped);
            Assert.LessOrEqual(result.Steps, 120);
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.AttackPointerRetargeted));
            foreach (var frame in result.Replay.Frames)
            {
                Assert.LessOrEqual(frame.PlayerBoardSnapshot.Minions.Count, 7);
                Assert.LessOrEqual(frame.OpponentBoardSnapshot.Minions.Count, 7);
                Assert.IsTrue(frame.PlayerBoardSnapshot.Minions.Concat(frame.OpponentBoardSnapshot.Minions).All(minion => minion.MaxHealth >= 1));
            }
        }

        private static MinionInstance CardMinion(string id, BoardSide owner, string cardId, bool golden, int attack, int health, params Keyword[] keywords)
        {
            var minion = TestMinion(id, owner, attack, health, keywords);
            minion.CardId = cardId;
            minion.Golden = golden;
            return minion;
        }

        private static MinionInstance TestMinion(string id, BoardSide owner, int attack, int health, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id.ToUpperInvariant(),
                Name = id,
                Attack = attack,
                BaseAttack = attack,
                Health = health,
                MaxHealth = health,
                BaseHealth = health,
                Owner = owner,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true
            };
        }
    }
}
