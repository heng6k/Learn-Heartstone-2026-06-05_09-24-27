using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class CombatReplayAndOpponentEditorTests
    {
        [Test]
        public void CombatReplay_RecordsStartAttackDamageAndEndFrames()
        {
            var player = TestMinion("p1", BoardSide.Player, 3, 5);
            var opponent = TestMinion("o1", BoardSide.Opponent, 2, 4);

            var result = CombatEngine.SimulateBasicCombat(new[] { player }, new[] { opponent }, 77, 1);

            Assert.AreEqual(77, result.Replay.Seed);
            Assert.AreEqual(1, result.Replay.InitialSnapshot.Player.Minions.Count);
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.CombatStarted));
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.AttackDeclared));
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.DamageResolved));
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.CombatEnded));
        }

        [Test]
        public void CombatReplay_DamageFrameContainsUpdatedStats()
        {
            var player = TestMinion("p1", BoardSide.Player, 3, 5);
            var opponent = TestMinion("o1", BoardSide.Opponent, 2, 4);

            var result = CombatEngine.SimulateBasicCombat(new[] { player }, new[] { opponent }, 78, 1);
            var damage = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.DamageResolved);

            Assert.AreEqual(3, damage.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "p1").Health);
            Assert.AreEqual(1, damage.OpponentBoardSnapshot.Minions.Single(minion => minion.InstanceId == "o1").Health);
            Assert.That(damage.DamagedEntityIds, Does.Contain("p1"));
            Assert.That(damage.DamagedEntityIds, Does.Contain("o1"));
        }

        [Test]
        public void CombatReplay_RecordsDivineShieldBreak()
        {
            var player = TestMinion("p1", BoardSide.Player, 3, 5);
            var opponent = TestMinion("o1", BoardSide.Opponent, 2, 4, Keyword.DivineShield);

            var result = CombatEngine.SimulateBasicCombat(new[] { player }, new[] { opponent }, 79, 1);
            var shield = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.DivineShieldBroken);

            Assert.IsFalse(shield.OpponentBoardSnapshot.Minions.Single(minion => minion.InstanceId == "o1").Keywords.Contains(Keyword.DivineShield));
            Assert.That(shield.RelatedEntityIds, Does.Contain("o1"));
        }

        [Test]
        public void CombatReplay_RecordsVenomousResolution()
        {
            var player = TestMinion("p1", BoardSide.Player, 1, 5, Keyword.Venomous);
            var opponent = TestMinion("o1", BoardSide.Opponent, 2, 10);

            var result = CombatEngine.SimulateBasicCombat(new[] { player }, new[] { opponent }, 80, 1);
            var venomous = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.VenomousResolved);

            Assert.IsFalse(venomous.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "p1").Keywords.Contains(Keyword.Venomous));
            Assert.That(venomous.DamagedEntityIds, Does.Contain("o1"));
        }

        [Test]
        public void CombatReplay_RecordsDeathQueueAndDeathrattleSummon()
        {
            var player = TestMinion("p1", BoardSide.Player, 5, 5);
            var opponent = TestMinion("o1", BoardSide.Opponent, 1, 1, Keyword.Deathrattle);
            opponent.CardId = "BG28_300";

            var result = CombatEngine.SimulateBasicCombat(new[] { player }, new[] { opponent }, 81, 1);

            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.DeathQueued));
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.DeathrattleResolved));
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.MinionSummoned));
            Assert.Greater(result.Replay.Frames.First(frame => frame.EventType == CombatEventType.MinionSummoned).SummonedEntityIds.Count, 0);
        }

        [Test]
        public void CombatReplay_RecordsRebornAtOriginalSide()
        {
            var player = TestMinion("p1", BoardSide.Player, 5, 5);
            var opponent = TestMinion("o1", BoardSide.Opponent, 1, 1, Keyword.Reborn);

            var result = CombatEngine.SimulateBasicCombat(new[] { player }, new[] { opponent }, 82, 1);
            var reborn = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.RebornResolved);

            Assert.AreEqual(BoardSide.Opponent, reborn.ActorSide);
            Assert.AreEqual(1, reborn.OpponentBoardSnapshot.Minions.Single(minion => minion.InstanceId == "o1").Health);
        }

        [Test]
        public void CombatReplay_WindfuryProducesTwoAttackDeclarations()
        {
            var player = TestMinion("p1", BoardSide.Player, 1, 20, Keyword.Windfury);
            var opponentA = TestMinion("o1", BoardSide.Opponent, 1, 20);
            var opponentB = TestMinion("o2", BoardSide.Opponent, 1, 20);

            var result = CombatEngine.SimulateBasicCombat(new[] { player }, new[] { opponentA, opponentB }, 83, 3);

            Assert.GreaterOrEqual(result.Replay.Frames.Count(frame => frame.EventType == CombatEventType.AttackDeclared), 2);
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.WindfuryResolved));
        }

        [Test]
        public void CombatReplay_ImmediateAttackQueueIsVisible()
        {
            var player = TestMinion("p1", BoardSide.Player, 10, 10);
            var opponent = TestMinion("o1", BoardSide.Opponent, 1, 1, Keyword.Deathrattle);
            opponent.CardId = "BG34_630";

            var result = CombatEngine.SimulateBasicCombat(new[] { player }, new[] { opponent }, 84, 2);

            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.ImmediateAttackQueued));
        }

        [Test]
        public void CombatPointer_SelfDeathrattleSummonRetargetsNextFriendlyAttack()
        {
            var summoner = TestMinion("p-summoner", BoardSide.Player, 1, 1, Keyword.Deathrattle);
            summoner.CardId = "BG31_801";
            var follower = TestMinion("p-follower", BoardSide.Player, 1, 30);
            var opponent = TestMinion("o-wall", BoardSide.Opponent, 1, 50);
            var tavern = new TavernState { BeetleAttackBonus = 10, BeetleHealthBonus = 10 };

            var result = CombatEngine.SimulateBasicCombat(new[] { summoner, follower }, new[] { opponent }, 3110, 4, tavern);
            var retarget = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackPointerRetargeted);
            var nextPlayerAttack = result.Replay.Frames
                .Where(frame => frame.Index > retarget.Index && frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player)
                .First();

            Assert.AreEqual(BoardSide.Player, retarget.AttackPointerSide);
            Assert.AreEqual(0, retarget.AttackPointerIndex);
            Assert.That(retarget.TargetId, Does.StartWith("token-p-summoner-beetle"));
            Assert.That(nextPlayerAttack.ActorId, Does.StartWith("token-p-summoner-beetle"));
        }

        [Test]
        public void CombatPointer_DeathOnEnemyAttackRetargetsDefenderNextAttack()
        {
            var summoner = TestMinion("p-summoner", BoardSide.Player, 1, 1, Keyword.Deathrattle);
            summoner.CardId = "BG31_801";
            var opponentA = TestMinion("o-attacker", BoardSide.Opponent, 1, 50);
            var opponentB = TestMinion("o-follower", BoardSide.Opponent, 1, 50);
            var tavern = new TavernState { BeetleAttackBonus = 10, BeetleHealthBonus = 10 };

            var result = CombatEngine.SimulateBasicCombat(new[] { summoner }, new[] { opponentA, opponentB }, 3111, 3, tavern);
            var retarget = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackPointerRetargeted && frame.AttackPointerSide == BoardSide.Player);
            var nextPlayerAttack = result.Replay.Frames
                .Where(frame => frame.Index > retarget.Index && frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player)
                .First();

            Assert.That(retarget.TargetId, Does.StartWith("token-p-summoner-beetle"));
            Assert.That(nextPlayerAttack.ActorId, Does.StartWith("token-p-summoner-beetle"));
        }

        [Test]
        public void CombatPointer_DeathrattleBeforeRebornRetargetsToFirstNewUnit()
        {
            var summoner = TestMinion("p-summoner", BoardSide.Player, 1, 1, Keyword.Deathrattle, Keyword.Reborn);
            summoner.CardId = "BG31_801";
            var follower = TestMinion("p-follower", BoardSide.Player, 1, 30);
            var opponent = TestMinion("o-wall", BoardSide.Opponent, 1, 50);
            var tavern = new TavernState { BeetleAttackBonus = 10, BeetleHealthBonus = 10 };

            var result = CombatEngine.SimulateBasicCombat(new[] { summoner, follower }, new[] { opponent }, 3112, 4, tavern);
            var deathrattleIndex = result.Replay.Frames.FindIndex(frame => frame.EventType == CombatEventType.DeathrattleResolved);
            var rebornIndex = result.Replay.Frames.FindIndex(frame => frame.EventType == CombatEventType.RebornResolved);
            var retarget = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackPointerRetargeted);

            Assert.GreaterOrEqual(deathrattleIndex, 0);
            Assert.GreaterOrEqual(rebornIndex, 0);
            Assert.Less(deathrattleIndex, rebornIndex);
            Assert.That(retarget.TargetId, Does.StartWith("token-p-summoner-beetle"));
            Assert.That(retarget.RelatedEntityIds, Does.Contain("p-summoner"));
        }

        [Test]
        public void CombatPointer_FullBoardRecordsSummonAndRebornOverflow()
        {
            var summoner = TestMinion("p-summoner", BoardSide.Player, 1, 1, Keyword.Deathrattle, Keyword.Reborn);
            summoner.CardId = "BG28_300";
            var playerBoard = new List<MinionInstance> { summoner };
            playerBoard.AddRange(Enumerable.Range(0, 6).Select(index => TestMinion("p-filler-" + index, BoardSide.Player, 1, 30)));
            var opponent = TestMinion("o-wall", BoardSide.Opponent, 1, 50);

            var result = CombatEngine.SimulateBasicCombat(playerBoard, new[] { opponent }, 3113, 3);
            var summonOverflow = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.SummonOverflowed);
            var rebornOverflow = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.RebornOverflowed);
            var retarget = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackPointerRetargeted);

            Assert.AreEqual(1, summonOverflow.SummonOverflowCount);
            Assert.AreEqual(1, rebornOverflow.RebornOverflowCount);
            Assert.LessOrEqual(summonOverflow.PlayerBoardSnapshot.Minions.Count, 7);
            Assert.LessOrEqual(rebornOverflow.PlayerBoardSnapshot.Minions.Count, 7);
            Assert.That(retarget.TargetId, Does.StartWith("token-p-summoner-skeleton"));
        }

        [Test]
        public void OpponentEditor_ClearCopyAndMirrorCommandsUpdateBoard()
        {
            var service = MatchService.CreateWithDefaultCatalog(100);
            service.State.Player.Board.Add(TestMinion("p1", BoardSide.Player, 2, 2));
            service.State.Player.Board.Add(TestMinion("p2", BoardSide.Player, 3, 3));

            service.Apply(new GameCommand(GameCommandType.CopyPlayerBoardToOpponent));
            Assert.AreEqual(2, service.State.Opponent.Board.Count);
            Assert.AreEqual(BoardSide.Opponent, service.State.Opponent.Board[0].Owner);
            Assert.AreEqual("P1", service.State.Opponent.Board[0].CardId);

            service.Apply(new GameCommand(GameCommandType.MirrorPlayerBoardToOpponent));
            Assert.AreEqual("P2", service.State.Opponent.Board[0].CardId);

            service.Apply(new GameCommand(GameCommandType.ClearOpponentBoard));
            Assert.AreEqual(0, service.State.Opponent.Board.Count);
        }

        [Test]
        public void CombatReplay_SevenBySevenHighStatPressureRunStaysBounded()
        {
            var players = Enumerable.Range(0, 7)
                .Select(index => TestMinion("p" + index, BoardSide.Player, 30 + index, 40 + index, index % 2 == 0 ? Keyword.DivineShield : Keyword.Taunt))
                .ToList();
            var opponents = Enumerable.Range(0, 7)
                .Select(index => TestMinion("o" + index, BoardSide.Opponent, 28 + index, 38 + index, index % 2 == 0 ? Keyword.Venomous : Keyword.Reborn))
                .ToList();

            var result = CombatEngine.SimulateBasicCombat(players, opponents, 9001, 80);

            Assert.IsFalse(result.SafetyStopped);
            Assert.LessOrEqual(result.Steps, 80);
            Assert.Greater(result.Replay.Frames.Count, 10);
            foreach (var frame in result.Replay.Frames)
            {
                Assert.LessOrEqual(frame.PlayerBoardSnapshot.Minions.Count, 7);
                Assert.LessOrEqual(frame.OpponentBoardSnapshot.Minions.Count, 7);
                Assert.IsTrue(frame.PlayerBoardSnapshot.Minions.Concat(frame.OpponentBoardSnapshot.Minions).All(minion => minion.MaxHealth >= 1));
            }
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
