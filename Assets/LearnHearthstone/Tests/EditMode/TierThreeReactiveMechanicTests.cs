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
    public sealed class TierThreeReactiveMechanicTests
    {
        private const string DustboneDestroyerCardId = "BG33_323";
        private const string ToughOrcaCardId = "BG34_312";
        private const string RoaringRecruiterCardId = "BG29_816";
        private const string TwilightHatchlingCardId = "BG34_630";

        [Test]
        public void MinionCatalog_TierThreeReactiveRepresentativesExist()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var dustbone = catalog.GetByCardId(DustboneDestroyerCardId);
            var orca = catalog.GetByCardId(ToughOrcaCardId);
            var recruiter = catalog.GetByCardId(RoaringRecruiterCardId);

            Assert.AreEqual(3, dustbone.TavernTier);
            Assert.AreEqual(3, orca.TavernTier);
            Assert.AreEqual(3, recruiter.TavernTier);
            Assert.IsTrue(dustbone.InPool);
            Assert.IsTrue(orca.InPool);
            Assert.IsTrue(recruiter.InPool);
            Assert.That(dustbone.Keywords, Does.Contain(Keyword.Rally));
            Assert.That(orca.Keywords, Does.Contain(Keyword.Taunt));
            Assert.That(recruiter.Tribes, Does.Contain(Tribe.Dragon));
        }

        [Test]
        public void AvengeCounter_CountsFriendlyDeathsOneAtATimeAndBuffsAtThreshold()
        {
            var fodderA = TestMinion("p-fodder-a", BoardSide.Player, 1, 1, Tribe.None, Keyword.Taunt);
            var source = TestMinion("p-avenge", BoardSide.Player, 1, 10, Tribe.None, Keyword.Avenge);
            source.EffectIds.Add("avenge_2_buff_self_2_2");
            var fodderB = TestMinion("p-fodder-b", BoardSide.Player, 1, 1, Tribe.None, Keyword.Taunt);
            var wall = TestMinion("o-wall", BoardSide.Opponent, 5, 100, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(new[] { fodderA, source, fodderB }, new[] { wall }, 3201, 6);
            var avengeFrames = result.Replay.Frames.Where(frame => frame.EventType == CombatEventType.AvengeCounterUpdated).ToList();
            var trigger = avengeFrames.Single(frame => frame.MechanicCounter == 2);
            var buffed = trigger.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "p-avenge");

            Assert.AreEqual(2, avengeFrames.Count);
            Assert.AreEqual(1, avengeFrames[0].MechanicCounter);
            Assert.AreEqual(2, avengeFrames[0].MechanicThreshold);
            Assert.AreEqual(2, trigger.MechanicThreshold);
            Assert.AreEqual(3, buffed.Attack);
            Assert.AreEqual(12, buffed.MaxHealth);
            Assert.That(trigger.DeadEntityIds, Does.Contain("p-fodder-b"));
        }

        [Test]
        public void AvengeCounter_DoesNotCountEnemyDeaths()
        {
            var attacker = TestMinion("p-attacker", BoardSide.Player, 10, 10, Tribe.None);
            var source = TestMinion("p-avenge", BoardSide.Player, 1, 10, Tribe.None, Keyword.Avenge);
            source.EffectIds.Add("avenge_2_buff_self_2_2");
            var enemy = TestMinion("o-enemy", BoardSide.Opponent, 1, 1, Tribe.None, Keyword.Taunt);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, source }, new[] { enemy }, 3202, 3);

            Assert.IsFalse(result.Replay.Frames.Any(frame => frame.EventType == CombatEventType.AvengeCounterUpdated));
        }

        [Test]
        public void ToughOrca_ActualDamageBuffsOtherFriendlyMinions()
        {
            var orca = CardMinion("p-orca", BoardSide.Player, ToughOrcaCardId, false, 1, 6, Tribe.Beast, Keyword.Taunt);
            var ally = TestMinion("p-ally", BoardSide.Player, 2, 2, Tribe.Pirate);
            var enemy = TestMinion("o-pinger", BoardSide.Opponent, 1, 10, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(new[] { orca, ally }, new[] { enemy }, 3203, 3);
            var trigger = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.DamageTriggered);
            var buffed = trigger.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "p-ally");

            Assert.AreEqual("p-orca", trigger.ActorId);
            Assert.AreEqual(1, trigger.ActualDamageCount);
            Assert.AreEqual(0, trigger.DivineShieldBreakCount);
            Assert.That(trigger.DamagedEntityIds, Does.Contain("p-orca"));
            Assert.AreEqual(3, buffed.Attack);
            Assert.AreEqual(3, buffed.Health);
            Assert.AreEqual(3, buffed.MaxHealth);
        }

        [Test]
        public void ToughOrca_DivineShieldBreakDoesNotTriggerDamageBenefit()
        {
            var orca = CardMinion("p-orca", BoardSide.Player, ToughOrcaCardId, false, 1, 6, Tribe.Beast, Keyword.Taunt, Keyword.DivineShield);
            var ally = TestMinion("p-ally", BoardSide.Player, 2, 2, Tribe.Pirate);
            var enemy = TestMinion("o-pinger", BoardSide.Opponent, 1, 10, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(new[] { orca, ally }, new[] { enemy }, 3204, 1);
            var damage = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.DamageResolved);

            Assert.AreEqual(1, damage.ActualDamageCount);
            Assert.AreEqual(1, damage.DivineShieldBreakCount);
            Assert.IsFalse(result.Replay.Frames.Any(frame => frame.EventType == CombatEventType.DamageTriggered));
        }

        [Test]
        public void RoaringRecruiter_BuffsOtherFriendlyDragonOnNaturalAttack()
        {
            var recruiter = CardMinion("p-recruiter", BoardSide.Player, RoaringRecruiterCardId, false, 0, 20, Tribe.Dragon);
            var dragon = TestMinion("p-dragon", BoardSide.Player, 2, 10, Tribe.Dragon);
            var wall = TestMinion("o-wall", BoardSide.Opponent, 0, 50, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(new[] { recruiter, dragon }, new[] { wall }, 3205, 4);
            var trigger = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackTriggered && frame.ActorId == "p-recruiter");
            var buffed = trigger.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "p-dragon");

            Assert.IsFalse(trigger.TriggeredAttack);
            Assert.AreEqual("p-dragon", trigger.TargetId);
            Assert.AreEqual(5, buffed.Attack);
            Assert.AreEqual(11, buffed.Health);
            Assert.That(trigger.TriggerSourceIds, Does.Contain("p-recruiter"));
        }

        [Test]
        public void RoaringRecruiter_DoesNotCountItsOwnAttackAsAnotherFriendlyDragon()
        {
            var recruiter = CardMinion("p-recruiter", BoardSide.Player, RoaringRecruiterCardId, false, 2, 20, Tribe.Dragon);
            var wall = TestMinion("o-wall", BoardSide.Opponent, 0, 50, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(new[] { recruiter }, new[] { wall }, 3210, 2);

            Assert.IsFalse(result.Replay.Frames.Any(frame => frame.EventType == CombatEventType.AttackTriggered && frame.ActorId == "p-recruiter"));
        }

        [Test]
        public void RoaringRecruiter_MarksImmediateAttackTriggerSeparately()
        {
            var attacker = TestMinion("p-attacker", BoardSide.Player, 10, 30, Tribe.None);
            var follower = TestMinion("p-follower", BoardSide.Player, 1, 30, Tribe.None);
            var recruiter = CardMinion("o-recruiter", BoardSide.Opponent, RoaringRecruiterCardId, false, 0, 20, Tribe.Dragon);
            var hatchling = CardMinion("o-hatchling-source", BoardSide.Opponent, TwilightHatchlingCardId, false, 1, 1, Tribe.Dragon, Keyword.Deathrattle, Keyword.Taunt);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, follower }, new[] { recruiter, hatchling }, 3206, 5);
            var trigger = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackTriggered && frame.ActorId == "o-recruiter");
            var buffedHatchling = trigger.OpponentBoardSnapshot.Minions.Single(minion => minion.InstanceId == trigger.TargetId);

            Assert.IsTrue(trigger.TriggeredAttack);
            Assert.That(trigger.TargetId, Does.StartWith("token-o-hatchling-source-hatchling"));
            Assert.AreEqual(6, buffedHatchling.Attack);
            Assert.AreEqual(4, buffedHatchling.Health);
            Assert.AreEqual(4, buffedHatchling.MaxHealth);
            Assert.That(trigger.TriggerSourceIds, Does.Contain("o-recruiter"));
        }

        [Test]
        public void DustboneDestroyer_RallyBuffsFriendlyUndeadAttackAndRecordsReplay()
        {
            var dustbone = CardMinion("p-dustbone", BoardSide.Player, DustboneDestroyerCardId, false, 2, 6, Tribe.Undead, Keyword.Rally);
            var undead = TestMinion("p-undead", BoardSide.Player, 3, 3, Tribe.Undead);
            var wall = TestMinion("o-wall", BoardSide.Opponent, 0, 50, Tribe.None);

            var result = CombatEngine.SimulateBasicCombat(new[] { dustbone, undead }, new[] { wall }, 3207, 3);
            var trigger = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.AttackTriggered && frame.ActorId == "p-dustbone");
            var buffedSource = trigger.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "p-dustbone");
            var buffed = trigger.PlayerBoardSnapshot.Minions.Single(minion => minion.InstanceId == "p-undead");

            Assert.AreEqual(3, buffedSource.Attack);
            Assert.AreEqual(4, buffed.Attack);
            Assert.AreEqual(3, buffed.Health);
            Assert.That(trigger.TriggerSourceIds, Does.Contain("p-dustbone"));
            Assert.IsTrue(result.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.ImproveUndeadAttack &&
                reward.SourceCardId == DustboneDestroyerCardId &&
                reward.Amount == 1));
        }

        [Test]
        public void DustboneDestroyer_RallyPaysUndeadAttackIntoTavernState()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Board.Add(CardMinion("p-dustbone-service", BoardSide.Player, DustboneDestroyerCardId, false, 2, 6, Tribe.Undead, Keyword.Rally));
            service.State.Player.Board.Add(TestMinion("p-undead-service", BoardSide.Player, 3, 3, Tribe.Undead));
            service.State.Opponent.Board.Add(TestMinion("o-wall-service", BoardSide.Opponent, 0, 50, Tribe.None));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 3207, SafetyLimit = 3 }));

            Assert.AreEqual(1, service.State.Player.Tavern.UndeadAttackBonus);
            Assert.IsTrue(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.ImproveUndeadAttack &&
                reward.SourceCardId == DustboneDestroyerCardId &&
                reward.Amount == 1));
        }

        [Test]
        public void TierThreeReactiveMixedPressureRunStaysBounded()
        {
            var players = new List<MinionInstance>
            {
                CardMinion("p-dustbone", BoardSide.Player, DustboneDestroyerCardId, false, 2, 6, Tribe.Undead, Keyword.Rally),
                TestMinion("p-undead", BoardSide.Player, 6, 4, Tribe.Undead),
                CardMinion("p-orca", BoardSide.Player, ToughOrcaCardId, false, 1, 6, Tribe.Beast, Keyword.Taunt),
                TestMinion("p-ally", BoardSide.Player, 4, 8, Tribe.Pirate),
                CardMinion("p-recruiter", BoardSide.Player, RoaringRecruiterCardId, false, 2, 8, Tribe.Dragon),
                TestMinion("p-dragon", BoardSide.Player, 5, 7, Tribe.Dragon),
                TestMinion("p-avenge", BoardSide.Player, 2, 10, Tribe.None, Keyword.Avenge)
            };
            players.Last().EffectIds.Add("avenge_2_buff_self_2_2");

            var opponents = new List<MinionInstance>
            {
                TestMinion("o-attacker-a", BoardSide.Opponent, 8, 8, Tribe.None),
                TestMinion("o-attacker-b", BoardSide.Opponent, 7, 7, Tribe.None),
                CardMinion("o-orca", BoardSide.Opponent, ToughOrcaCardId, false, 1, 6, Tribe.Beast, Keyword.Taunt),
                TestMinion("o-ally", BoardSide.Opponent, 4, 8, Tribe.Pirate),
                CardMinion("o-recruiter", BoardSide.Opponent, RoaringRecruiterCardId, false, 2, 8, Tribe.Dragon),
                TestMinion("o-dragon", BoardSide.Opponent, 5, 7, Tribe.Dragon),
                TestMinion("o-avenge", BoardSide.Opponent, 2, 10, Tribe.None, Keyword.Avenge)
            };
            opponents.Last().EffectIds.Add("avenge_2_buff_self_2_2");

            var result = CombatEngine.SimulateBasicCombat(players, opponents, 3208, 120);

            Assert.IsFalse(result.SafetyStopped);
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.AttackTriggered));
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.DamageTriggered));
            Assert.That(result.Replay.Frames.Select(frame => frame.EventType), Does.Contain(CombatEventType.AvengeCounterUpdated));
            foreach (var frame in result.Replay.Frames)
            {
                Assert.LessOrEqual(frame.PlayerBoardSnapshot.Minions.Count, 7);
                Assert.LessOrEqual(frame.OpponentBoardSnapshot.Minions.Count, 7);
            }
        }

        private static MinionInstance CardMinion(string id, BoardSide owner, string cardId, bool golden, int attack, int health, Tribe tribe, params Keyword[] keywords)
        {
            var minion = TestMinion(id, owner, attack, health, tribe, keywords);
            minion.CardId = cardId;
            minion.Golden = golden;
            return minion;
        }

        private static MinionInstance TestMinion(string id, BoardSide owner, int attack, int health, Tribe tribe, params Keyword[] keywords)
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
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                CanAttack = true
            };
        }
    }
}
