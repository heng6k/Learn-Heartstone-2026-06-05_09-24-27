using System.Collections.Generic;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class CombatMechanicTests
    {
        [Test]
        public void CombatEngine_RebornResummonsWithOneHealthAfterDeath()
        {
            var attacker = TestMinion("p1", 5, 5);
            var reborn = TestMinion("o1", 1, 1);
            reborn.Keywords.Add(Keyword.Reborn);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker }, new[] { reborn }, 1, 1);

            Assert.AreEqual(1, result.FinalOpponentBoard.Count);
            Assert.AreEqual(1, result.FinalOpponentBoard[0].Health);
            Assert.IsFalse(result.FinalOpponentBoard[0].Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void CombatEngine_DeathrattleResolvesBeforeReborn()
        {
            var attacker = TestMinion("p1", 5, 5);
            var target = TestMinion("o1", 1, 1);
            target.Keywords.Add(Keyword.Deathrattle);
            target.Keywords.Add(Keyword.Reborn);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker }, new[] { target }, 1, 1);
            var deathrattleIndex = result.Log.FindIndex(entry => entry.Title == "DeathrattleResolved");
            var rebornIndex = result.Log.FindIndex(entry => entry.Title == "RebornResolved");

            Assert.GreaterOrEqual(deathrattleIndex, 0);
            Assert.GreaterOrEqual(rebornIndex, 0);
            Assert.Less(deathrattleIndex, rebornIndex);
        }

        private static MinionInstance TestMinion(string id, int attack, int health)
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
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>()
            };
        }
    }
}
