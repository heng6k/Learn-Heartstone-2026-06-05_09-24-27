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
            var returned = reborn.OpponentBoardSnapshot.Minions.Single(minion => minion.CardId == "O1");
            Assert.AreEqual(1, returned.Health);
            Assert.AreNotEqual("o1", returned.InstanceId);
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
        public void CombatPointer_CurrentAttackerDeathrattleSummonTakesItsVacatedAttackSlot()
        {
            var summoner = TestMinion("p-summoner", BoardSide.Player, 1, 1, Keyword.Deathrattle);
            summoner.CardId = "BG31_801";
            var follower = TestMinion("p-follower", BoardSide.Player, 1, 30);
            var opponent = TestMinion("o-wall", BoardSide.Opponent, 1, 50);
            var tavern = new TavernState { BeetleAttackBonus = 10, BeetleHealthBonus = 10 };

            var result = CombatEngine.SimulateBasicCombat(new[] { summoner, follower }, new[] { opponent }, 3110, 4, tavern);
            var nextPlayerAttack = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player)
                .Skip(1)
                .First();
            var summonedId = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == summoner.InstanceId)
                .SelectMany(frame => frame.SummonedEntityIds)
                .Single();

            Assert.AreEqual(summonedId, nextPlayerAttack.ActorId);
            Assert.IsTrue(result.Replay.Frames.Any(frame =>
                frame.EventType == CombatEventType.AttackPointerRetargeted &&
                frame.AttackPointerSide == BoardSide.Player &&
                frame.TargetId == summonedId));
        }

        [Test]
        public void CombatPointer_DustboneRebornAfterItsAttackTakesTheNextNaturalAttackBeforeRoadrunner()
        {
            var dustbone = TestMinion("p-dustbone", BoardSide.Player, 2, 6, Keyword.Rally);
            dustbone.CardId = "BG33_323";
            dustbone.Tribes = new List<Tribe> { Tribe.Undead };
            var roadrunner = TestMinion("p-roadrunner", BoardSide.Player, 10, 11, Keyword.Reborn);
            roadrunner.CardId = "BG36_208";
            roadrunner.Tribes = new List<Tribe> { Tribe.Beast };
            var mummifier = TestMinion("p-mummifier", BoardSide.Player, 10, 4, Keyword.Deathrattle, Keyword.Reborn);
            mummifier.CardId = "BG28_309";
            mummifier.Tribes = new List<Tribe> { Tribe.Undead };
            var rider = TestMinion("p-rider", BoardSide.Player, 4, 2, Keyword.Reborn, Keyword.Taunt);
            rider.CardId = "BG25_001";
            rider.Tribes = new List<Tribe> { Tribe.Undead };
            var opponentA = TestMinion("o-rider-a", BoardSide.Opponent, 10000, 10000, Keyword.Reborn, Keyword.Taunt);
            var opponentB = TestMinion("o-rider-b", BoardSide.Opponent, 10000, 10000, Keyword.Reborn, Keyword.Taunt);

            var result = CombatEngine.SimulateBasicCombat(
                new[] { dustbone, roadrunner, mummifier, rider },
                new[] { opponentA, opponentB },
                3117,
                3);
            var dustboneReborn = result.Replay.Frames.First(frame =>
                frame.EventType == CombatEventType.RebornResolved &&
                frame.ActorId == dustbone.InstanceId);
            var riderReborn = result.Replay.Frames.First(frame =>
                frame.EventType == CombatEventType.RebornResolved &&
                frame.ActorId == rider.InstanceId);
            var nextPlayerAttack = result.Replay.Frames.First(frame =>
                frame.Index > riderReborn.Index &&
                frame.EventType == CombatEventType.AttackDeclared &&
                frame.ActorSide == BoardSide.Player);
            var dustboneRebornId = dustboneReborn.SummonedEntityIds.Single();

            Assert.AreEqual(0, dustboneReborn.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == dustboneRebornId).AttacksThisCombat);
            Assert.AreEqual(dustboneRebornId, nextPlayerAttack.ActorId);
            Assert.AreNotEqual(roadrunner.InstanceId, nextPlayerAttack.ActorId);
        }

        [Test]
        public void CombatPointer_BacklineRebornDoesNotReplaceLivingNextAttacker()
        {
            var next = TestMinion("p-next", BoardSide.Player, 1, 30);
            var backline = TestMinion("p-backline", BoardSide.Player, 1, 1, Keyword.Reborn, Keyword.Taunt);
            var opponentA = TestMinion("o-attacker", BoardSide.Opponent, 5, 30);
            var opponentB = TestMinion("o-filler-a", BoardSide.Opponent, 0, 30);
            var opponentC = TestMinion("o-filler-b", BoardSide.Opponent, 0, 30);

            var result = CombatEngine.SimulateBasicCombat(new[] { next, backline }, new[] { opponentA, opponentB, opponentC }, 3114, 2);
            var reborn = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.RebornResolved);
            var rebornId = reborn.SummonedEntityIds.Single();
            var nextPlayerAttack = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player);

            Assert.AreEqual(next.InstanceId, nextPlayerAttack.ActorId);
            Assert.AreNotEqual(rebornId, nextPlayerAttack.ActorId);
        }

        [Test]
        public void CombatPointer_WindfuryInsertsExtraAttackWithoutConsumingNaturalNextAttacker()
        {
            var windfury = TestMinion("p-windfury", BoardSide.Player, 1, 30, Keyword.Windfury);
            var next = TestMinion("p-next", BoardSide.Player, 1, 30);
            var wall = TestMinion("o-wall", BoardSide.Opponent, 0, 100, Keyword.Taunt);
            var opponentFiller = TestMinion("o-filler", BoardSide.Opponent, 0, 100);

            var result = CombatEngine.SimulateBasicCombat(new[] { windfury, next }, new[] { wall, opponentFiller }, 3115, 3);
            var playerAttacks = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player)
                .Select(frame => frame.ActorId)
                .ToList();

            CollectionAssert.AreEqual(new[] { windfury.InstanceId, windfury.InstanceId, next.InstanceId }, playerAttacks);
        }

        [Test]
        public void CombatPointer_FirstNaturalAttackAfterEnemyKillStartsAtDeathrattleSummon()
        {
            var summoner = TestMinion("p-summoner", BoardSide.Player, 1, 1, Keyword.Deathrattle);
            summoner.CardId = "BG31_801";
            var opponentA = TestMinion("o-attacker", BoardSide.Opponent, 1, 50);
            var opponentB = TestMinion("o-follower", BoardSide.Opponent, 1, 50);
            var tavern = new TavernState { BeetleAttackBonus = 10, BeetleHealthBonus = 10 };

            var result = CombatEngine.SimulateBasicCombat(new[] { summoner }, new[] { opponentA, opponentB }, 3111, 3, tavern);
            var summonedId = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == summoner.InstanceId)
                .SelectMany(frame => frame.SummonedEntityIds)
                .Single();
            var nextPlayerAttack = result.Replay.Frames.First(frame =>
                frame.EventType == CombatEventType.AttackDeclared &&
                frame.ActorSide == BoardSide.Player);

            Assert.AreEqual(summonedId, nextPlayerAttack.ActorId);
            Assert.IsFalse(result.Replay.Frames.Any(frame =>
                frame.Index < nextPlayerAttack.Index &&
                frame.EventType == CombatEventType.AttackPointerRetargeted &&
                frame.AttackPointerSide == BoardSide.Player));
        }

        [Test]
        public void CombatPointer_DeathrattleBeforeRebornPlacesFirstNewUnitInFirstAttackSlot()
        {
            var summoner = TestMinion("p-summoner", BoardSide.Player, 1, 1, Keyword.Deathrattle, Keyword.Reborn);
            summoner.CardId = "BG31_801";
            var opponent = TestMinion("o-attacker", BoardSide.Opponent, 1, 50);
            var opponentFollower = TestMinion("o-follower", BoardSide.Opponent, 0, 50);
            var tavern = new TavernState { BeetleAttackBonus = 10, BeetleHealthBonus = 10 };

            var result = CombatEngine.SimulateBasicCombat(new[] { summoner }, new[] { opponent, opponentFollower }, 3112, 2, tavern);
            var deathrattleIndex = result.Replay.Frames.FindIndex(frame => frame.EventType == CombatEventType.DeathrattleResolved);
            var rebornIndex = result.Replay.Frames.FindIndex(frame => frame.EventType == CombatEventType.RebornResolved);
            var summonedId = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == summoner.InstanceId)
                .SelectMany(frame => frame.SummonedEntityIds)
                .Single();

            Assert.GreaterOrEqual(deathrattleIndex, 0);
            Assert.GreaterOrEqual(rebornIndex, 0);
            Assert.Less(deathrattleIndex, rebornIndex);
            var nextPlayerAttack = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player);
            Assert.AreEqual(summonedId, nextPlayerAttack.ActorId);
            Assert.IsFalse(result.Replay.Frames.Any(frame =>
                frame.Index < nextPlayerAttack.Index &&
                frame.EventType == CombatEventType.AttackPointerRetargeted &&
                frame.AttackPointerSide == BoardSide.Player));
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
            var summonedId = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.MinionSummoned && frame.ActorId == summoner.InstanceId)
                .SelectMany(frame => frame.SummonedEntityIds)
                .Single();
            var nextPlayerAttack = result.Replay.Frames
                .Where(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorSide == BoardSide.Player)
                .Skip(1)
                .First();

            Assert.AreEqual(1, summonOverflow.SummonOverflowCount);
            Assert.AreEqual(1, rebornOverflow.RebornOverflowCount);
            Assert.LessOrEqual(summonOverflow.PlayerBoardSnapshot.Minions.Count, 7);
            Assert.LessOrEqual(rebornOverflow.PlayerBoardSnapshot.Minions.Count, 7);
            Assert.AreEqual(summonedId, nextPlayerAttack.ActorId);
        }

        [Test]
        public void RallyTarget_UsesTheActualAttackDefenderInsteadOfTheFirstEnemy()
        {
            var attacker = TestMinion("p-ravager", BoardSide.Player, 3, 20, Keyword.Rally);
            attacker.CardId = "BG27_017";
            var playerFillerA = TestMinion("p-filler-a", BoardSide.Player, 0, 30);
            var playerFillerB = TestMinion("p-filler-b", BoardSide.Player, 0, 30);
            var decoy = TestMinion("o-decoy", BoardSide.Opponent, 0, 20);
            var target = TestMinion("o-target", BoardSide.Opponent, 0, 20, Keyword.Taunt);
            var neighbor = TestMinion("o-neighbor", BoardSide.Opponent, 0, 20);

            var result = CombatEngine.SimulateBasicCombat(
                new[] { attacker, playerFillerA, playerFillerB },
                new[] { decoy, target, neighbor },
                3116,
                1);

            Assert.AreEqual(20, result.FinalOpponentBoard.Single(minion => minion.InstanceId == decoy.InstanceId).Health);
            Assert.AreEqual(14, result.FinalOpponentBoard.Single(minion => minion.InstanceId == target.InstanceId).Health);
            Assert.AreEqual(17, result.FinalOpponentBoard.Single(minion => minion.InstanceId == neighbor.InstanceId).Health);
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
