using System.Collections.Generic;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class HeroCombatDamageServiceTests
    {
        [Test]
        public void ResolveAndApply_PlayerWinCountsSurvivorTiersAndAppliesArmorFirst()
        {
            var state = State(playerTier: 4, opponentHealth: 30, opponentArmor: 5);
            var combat = Combat(
                CombatWinner.Player,
                Minion(6),
                Minion(3, golden: true),
                Minion(0),
                Minion(6, health: 0));

            var result = HeroCombatDamageService.ResolveAndApply(
                combat,
                state,
                round: 8,
                HeroDamageCapPolicy.OfficialTopFour,
                isTopFour: false);

            Assert.IsTrue(result.Applied);
            Assert.AreEqual(BoardSide.Opponent, result.DamagedSide);
            Assert.AreEqual(4, result.TavernTierDamage);
            Assert.AreEqual(10, result.SurvivingMinionTierDamage, "Golden keeps its printed tier and a tierless token counts as Tier 1.");
            Assert.AreEqual(14, result.RawDamage);
            Assert.AreEqual(15, result.DamageCap);
            Assert.AreEqual(14, result.AppliedDamage);
            Assert.AreEqual(5, result.ArmorAbsorbed);
            Assert.AreEqual(9, result.HealthDamage);
            Assert.AreEqual(0, state.Opponent.Armor);
            Assert.AreEqual(21, state.Opponent.Health);
        }

        [TestCase(3, 5)]
        [TestCase(4, 10)]
        [TestCase(8, 15)]
        public void ResolveAndApply_OfficialPolicyUsesRoundCaps(int round, int expectedDamage)
        {
            var state = State(playerTier: 6, opponentHealth: 40, opponentArmor: 0);
            var combat = Combat(CombatWinner.Player, Minion(6), Minion(6), Minion(6));

            var result = HeroCombatDamageService.ResolveAndApply(
                combat,
                state,
                round,
                HeroDamageCapPolicy.OfficialTopFour,
                isTopFour: false);

            Assert.AreEqual(expectedDamage, result.AppliedDamage);
            Assert.IsFalse(result.CapRemoved);
            Assert.IsFalse(result.UsesApproximation);
        }

        [Test]
        public void ResolveAndApply_TrainingPolicyRemovesCapFromRoundTwelveAndLabelsApproximation()
        {
            var state = State(playerTier: 6, opponentHealth: 40, opponentArmor: 0);
            var combat = Combat(CombatWinner.Player, Minion(6), Minion(6), Minion(6));

            var result = HeroCombatDamageService.ResolveAndApply(
                combat,
                state,
                round: 12,
                HeroDamageCapPolicy.TrainingRound12Approximation,
                isTopFour: false);

            Assert.AreEqual(22, result.AppliedDamage);
            Assert.IsTrue(result.CapRemoved);
            Assert.IsTrue(result.UsesApproximation);
            Assert.AreEqual(18, state.Opponent.Health);
        }

        [Test]
        public void ResolveAndApply_OfficialTopFourRemovesCapWithoutApproximation()
        {
            var state = State(playerTier: 6, opponentHealth: 40, opponentArmor: 0);
            var combat = Combat(CombatWinner.Player, Minion(6), Minion(6), Minion(6));

            var result = HeroCombatDamageService.ResolveAndApply(
                combat,
                state,
                round: 8,
                HeroDamageCapPolicy.OfficialTopFour,
                isTopFour: true);

            Assert.AreEqual(22, result.AppliedDamage);
            Assert.IsTrue(result.CapRemoved);
            Assert.IsFalse(result.UsesApproximation);
        }

        [Test]
        public void ResolveAndApply_DrawDealsZeroAndDoesNotMutateHeroes()
        {
            var state = State(playerTier: 6, opponentHealth: 40, opponentArmor: 7);
            var playerHealth = state.Player.Health;
            var combat = Combat(CombatWinner.Draw, Minion(6));

            var result = HeroCombatDamageService.ResolveAndApply(
                combat,
                state,
                round: 12,
                HeroDamageCapPolicy.TrainingRound12Approximation,
                isTopFour: false);

            Assert.IsFalse(result.Applied);
            Assert.AreEqual(0, result.AppliedDamage);
            Assert.AreEqual(playerHealth, state.Player.Health);
            Assert.AreEqual(40, state.Opponent.Health);
            Assert.AreEqual(7, state.Opponent.Armor);
        }

        private static MatchState State(int playerTier, int opponentHealth, int opponentArmor)
        {
            return new MatchState
            {
                Player = new LocalPlayerState
                {
                    Health = 30,
                    MaxHealth = 30,
                    Armor = 0,
                    Tavern = new TavernState { Tier = playerTier }
                },
                Opponent = new LocalOpponentState
                {
                    Health = opponentHealth,
                    Armor = opponentArmor,
                    TavernTier = 4
                }
            };
        }

        private static CombatOutput Combat(CombatWinner winner, params MinionInstance[] playerSurvivors)
        {
            return new CombatOutput
            {
                Winner = winner,
                FinalPlayerBoard = new List<MinionInstance>(playerSurvivors),
                FinalOpponentBoard = new List<MinionInstance>(),
                FinalPlayerTavern = new TavernState { Tier = 4 },
                FinalOpponentTavern = new TavernState { Tier = 4 }
            };
        }

        private static MinionInstance Minion(int tier, bool golden = false, int health = 1)
        {
            return new MinionInstance
            {
                TavernTier = tier,
                Golden = golden,
                Health = health,
                MaxHealth = 1
            };
        }
    }
}
