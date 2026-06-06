using System.Collections.Generic;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DomainEngineTests
    {
        [Test]
        public void SeededRng_RepeatsSequenceForSameSeed()
        {
            var first = new SeededRng(12345);
            var second = new SeededRng(12345);

            Assert.AreEqual(first.NextInt(1000), second.NextInt(1000));
            Assert.AreEqual(first.NextInt(1000), second.NextInt(1000));
        }

        [Test]
        public void MinionPool_DrawsAndReleasesCopies()
        {
            var definition = TestMinion("m1", 1, 2, 3, 2);
            var pool = new MinionPool(new[] { definition });

            var drawn = pool.DrawShop(1, 1, new SeededRng(1));

            Assert.AreEqual("m1", drawn[0].Id);
            Assert.AreEqual(1, pool.Remaining("m1"));

            pool.Release("m1", 1);
            Assert.AreEqual(2, pool.Remaining("m1"));
        }

        [Test]
        public void TripleEngine_CombinesThreeCopiesIntoGolden()
        {
            var items = new List<MinionInstance>
            {
                TestInstance("a", "m1", 1),
                TestInstance("b", "m1", 1),
                TestInstance("c", "m1", 1),
                TestInstance("d", "m2", 0)
            };

            Assert.AreEqual("m1", TripleEngine.FindTripleCandidate(items));

            var result = TripleEngine.ResolveTriple(items, "m1", BoardSide.Player, "unit");

            Assert.AreEqual(1, result.Remaining.Count);
            Assert.IsTrue(result.Golden.Golden);
            Assert.AreEqual(3, result.Golden.PoolCopiesHeld);
        }

        [Test]
        public void CombatEngine_PrioritizesTauntAndRemovesDivineShield()
        {
            var attacker = TestInstance("p1", "attacker", 0);
            attacker.Attack = 2;
            attacker.Health = 2;
            var support = TestInstance("p2", "support", 0);
            var taunt = TestInstance("o1", "taunt", 0);
            taunt.Attack = 1;
            taunt.Health = 3;
            taunt.Keywords.Add(Keyword.Taunt);
            taunt.Keywords.Add(Keyword.DivineShield);
            var other = TestInstance("o2", "other", 0);
            other.Attack = 1;
            other.Health = 3;

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, support }, new[] { other, taunt }, 42, 1);

            Assert.AreEqual(1, result.Steps);
            Assert.AreEqual("o1", result.Log[0].TargetId);
            Assert.IsFalse(result.FinalOpponentBoard.Find(m => m.InstanceId == "o1").Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void CombatEngine_VenomousKillsDamagedTargetAndIsConsumed()
        {
            var attacker = TestInstance("p1", "venom", 0);
            attacker.Attack = 1;
            attacker.Keywords.Add(Keyword.Venomous);
            var defender = TestInstance("o1", "defender", 0);
            defender.Attack = 0;
            defender.Health = 10;
            defender.MaxHealth = 10;

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker }, new[] { defender }, 1, 1);

            Assert.AreEqual(0, result.FinalOpponentBoard.Count);
            Assert.IsFalse(result.FinalPlayerBoard[0].Keywords.Contains(Keyword.Venomous));
        }

        [Test]
        public void CombatEngine_StealthDefendersAreSkippedWhenOtherTargetsExist()
        {
            var attacker = TestInstance("p1", "attacker", 0);
            var support = TestInstance("p2", "support", 0);
            var stealth = TestInstance("o1", "stealth", 0);
            stealth.Keywords.Add(Keyword.Stealth);
            var visible = TestInstance("o2", "visible", 0);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker, support }, new[] { stealth, visible }, 5, 1);

            Assert.AreEqual("o2", result.Log[0].TargetId);
        }

        private static MinionDefinition TestMinion(string id, int tier, int attack, int health, int poolCount)
        {
            return new MinionDefinition
            {
                Id = id,
                CardId = id.ToUpperInvariant(),
                DbfId = 1,
                Name = id,
                TavernTier = tier,
                BaseAttack = attack,
                BaseHealth = health,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                InPool = true,
                PoolCount = poolCount
            };
        }

        private static MinionInstance TestInstance(string instanceId, string definitionId, int poolCopiesHeld)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = definitionId,
                CardId = definitionId.ToUpperInvariant(),
                Name = definitionId,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Golden = false,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                PoolSource = PoolSource.Pool,
                PoolCopiesHeld = poolCopiesHeld
            };
        }
    }
}
