using System.Collections.Generic;
using System.Linq;
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
            reborn.OfficialKeywords.Add(Keyword.Reborn);
            reborn.Attack = 7;
            reborn.MaxHealth = 12;
            reborn.Enchantments.Add(new Enchantment { Id = "permanent-buff", AttackBonus = 6, HealthBonus = 11 });
            reborn.Counters["permanent-counter"] = 2;
            reborn.AttacksThisCombat = 1;
            reborn.CanAttack = false;

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker }, new[] { reborn }, 1, 1);

            Assert.AreEqual(1, result.FinalOpponentBoard.Count);
            var returned = result.FinalOpponentBoard[0];
            Assert.AreEqual(1, returned.Health);
            Assert.AreEqual(7, returned.Attack);
            Assert.AreEqual(12, returned.MaxHealth);
            Assert.AreNotEqual("o1", returned.InstanceId);
            Assert.IsFalse(returned.Keywords.Contains(Keyword.Reborn));
            Assert.IsFalse(returned.OfficialKeywords.Contains(Keyword.Reborn));
            Assert.IsTrue(returned.Enchantments.Exists(enchantment => enchantment.Id == "permanent-buff"));
            Assert.AreEqual(2, returned.Counters["permanent-counter"]);
            Assert.AreEqual(0, returned.AttacksThisCombat);
            Assert.IsTrue(returned.CanAttack);
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

        [Test]
        public void CombatEngine_RebornAppliesNewGlobalBuffOnce()
        {
            var attacker = TestMinion("p1", 5, 5);
            var plaguerunner = TestMinion("o1", 2, 1);
            plaguerunner.CardId = "BG34_690";
            plaguerunner.Tribes = new List<Tribe> { Tribe.Undead };
            plaguerunner.Keywords.Add(Keyword.Deathrattle);
            plaguerunner.Keywords.Add(Keyword.Reborn);

            var result = CombatEngine.SimulateBasicCombat(new[] { attacker }, new[] { plaguerunner }, 2, 1);

            var returned = result.FinalOpponentBoard[0];
            Assert.AreEqual(4, returned.Attack);
            Assert.AreEqual(1, returned.Enchantments.Count(enchantment => enchantment.Id.StartsWith("combat-tribe-bonus-")));
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
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>()
            };
        }
    }
}
