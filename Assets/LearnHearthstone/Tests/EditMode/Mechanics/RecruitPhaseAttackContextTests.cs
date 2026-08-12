using System.Collections.Generic;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class RecruitPhaseAttackContextTests
    {
        [Test]
        public void ResolveRecruitPhaseAttack_HitsSelectedShopTargetWithoutPollutingCombatState()
        {
            var state = CreateState();
            var attacker = Minion("fishbait-attacker", 5, 5);
            attacker.AttacksThisCombat = 4;
            var untouched = Minion("untouched-shop", 2, 6);
            var target = Minion("selected-shop", 1, 1);
            var opponent = Minion("opponent-board", 7, 7);
            state.Player.Board.Add(attacker);
            state.Player.Tavern.Shop.Add(untouched);
            state.Player.Tavern.Shop.Add(target);
            state.Opponent.Board.Add(opponent);

            var result = CombatEngine.ResolveRecruitPhaseAttack(
                state,
                new RecruitPhaseAttackContext
                {
                    AttackerInstanceId = attacker.InstanceId,
                    TavernTargetInstanceId = target.InstanceId,
                    DamageContext = "fishbait-damage",
                    DeathContext = "fishbait-death",
                    RewardSource = "fishbait-test",
                    Sequence = 3
                },
                54321);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.IsTrue(result.TargetDied);
            Assert.IsFalse(result.AttackerDied);
            Assert.AreEqual(5, result.TargetDamage);
            Assert.AreEqual(1, result.AttackerDamage);
            Assert.AreEqual(1, state.Player.Tavern.Shop.Count);
            Assert.AreEqual(untouched.InstanceId, state.Player.Tavern.Shop[0].InstanceId);
            Assert.AreEqual(6, state.Player.Tavern.Shop[0].Health);
            Assert.AreEqual(4, state.Player.Board[0].AttacksThisCombat);
            Assert.AreEqual(4, state.Player.Board[0].Health);
            Assert.AreEqual(opponent.InstanceId, state.Opponent.Board[0].InstanceId);
            Assert.AreEqual(7, state.Opponent.Board[0].Health);
            Assert.IsNull(state.LastReplay);
            Assert.AreEqual("recruit-attack.resolved", state.MechanicEvents[0].Type);
            CollectionAssert.AreEqual(
                new[] { attacker.InstanceId, target.InstanceId },
                state.MechanicEvents[0].Targets);
        }

        [Test]
        public void ResolveRecruitPhaseAttack_InvalidTargetLeavesAllStateUntouched()
        {
            var state = CreateState();
            var attacker = Minion("fishbait-attacker", 5, 5);
            var shop = Minion("shop-minion", 2, 2);
            state.Player.Board.Add(attacker);
            state.Player.Tavern.Shop.Add(shop);

            var result = CombatEngine.ResolveRecruitPhaseAttack(
                state,
                new RecruitPhaseAttackContext
                {
                    AttackerInstanceId = attacker.InstanceId,
                    TavernTargetInstanceId = "missing-target",
                    RewardSource = "fishbait-test",
                    Sequence = 1
                },
                54321);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("recruit-attack.target.missing", result.Code);
            Assert.AreEqual(5, state.Player.Board[0].Health);
            Assert.AreEqual(2, state.Player.Tavern.Shop[0].Health);
            Assert.IsEmpty(state.MechanicEvents);
        }

        private static MatchState CreateState()
        {
            var state = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository()).State;
            state.Phase = MatchPhase.Tavern;
            state.Player.Board.Clear();
            state.Player.Tavern.Shop.Clear();
            state.Opponent.Board.Clear();
            state.MechanicEvents.Clear();
            state.LastReplay = null;
            return state;
        }

        private static MinionInstance Minion(string instanceId, int attack, int health)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = BoardSide.Player,
                CanAttack = true,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }
    }
}
