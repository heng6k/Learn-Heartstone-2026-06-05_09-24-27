using System.Collections.Generic;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MechanicEngineTests
    {
        [Test]
        public void MinionFactory_StoresBaseStatsAndPoolOrigin()
        {
            var definition = new MinionDefinition
            {
                Id = "m1",
                CardId = "M1",
                Name = "m1",
                TavernTier = 1,
                BaseAttack = 2,
                BaseHealth = 3,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                InPool = true,
                PoolCount = 12
            };

            var minion = MinionFactory.Create(definition, BoardSide.Player, "unit", false, PoolSource.Pool, 1);
            var clone = minion.Clone();

            Assert.AreEqual(2, minion.BaseAttack);
            Assert.AreEqual(3, minion.BaseHealth);
            Assert.AreEqual(PoolSource.Pool, minion.OriginPoolSource);
            Assert.IsTrue(minion.CanReturnToPoolAfterAttach);
            Assert.AreEqual(minion.BaseAttack, clone.BaseAttack);
            Assert.AreEqual(minion.BaseHealth, clone.BaseHealth);
            Assert.AreEqual(minion.OriginPoolSource, clone.OriginPoolSource);
        }

        [Test]
        public void MechanicEnums_RepresentAnnotatedTriggerAndGrowthScopes()
        {
            Assert.AreEqual("TurnStarted", MechanicEventType.TurnStarted.ToString());
            Assert.AreEqual("ShopCurrent", BuffScope.ShopCurrent.ToString());
            Assert.AreEqual("ShopGlobal", BuffScope.ShopGlobal.ToString());
            Assert.AreEqual("FutureShopTyped", BuffScope.FutureShopTyped.ToString());
            Assert.AreEqual("EffectAura", MechanicAuraKind.EffectAura.ToString());
        }

        [Test]
        public void MechanicEngine_BuffStatsIncreasesAttackMaxHealthAndCurrentHealth()
        {
            var minion = TestInstance("m1");

            MechanicEngine.ApplyToMinion(minion, new MechanicAction
            {
                Type = MechanicActionType.BuffStats,
                Attack = 2,
                Health = 3,
                SourceId = "test-buff"
            });

            Assert.AreEqual(4, minion.Attack);
            Assert.AreEqual(5, minion.MaxHealth);
            Assert.AreEqual(5, minion.Health);
            Assert.AreEqual(1, minion.Enchantments.Count);
        }

        [Test]
        public void MechanicEngine_AddAndRemoveKeywordMutatesKeywordList()
        {
            var minion = TestInstance("m1");

            MechanicEngine.ApplyToMinion(minion, new MechanicAction { Type = MechanicActionType.AddKeyword, Keyword = Keyword.DivineShield });
            MechanicEngine.ApplyToMinion(minion, new MechanicAction { Type = MechanicActionType.RemoveKeyword, Keyword = Keyword.DivineShield });

            Assert.IsFalse(minion.Keywords.Contains(Keyword.DivineShield));
        }

        [Test]
        public void MechanicEngine_ModifyShopGrowthAddsTypedGlobalModifier()
        {
            var tavern = new TavernState();

            MechanicEngine.ApplyToTavern(tavern, new MechanicAction
            {
                Type = MechanicActionType.ModifyShopGrowth,
                Scope = BuffScope.ShopGlobal,
                Tribe = Tribe.Elemental,
                Attack = 1,
                Health = 1,
                SourceId = "future-elementals"
            });

            Assert.AreEqual(1, tavern.Growth.ShopModifiers.Count);
            Assert.AreEqual(BuffScope.ShopGlobal, tavern.Growth.ShopModifiers[0].Scope);
            Assert.AreEqual(Tribe.Elemental, tavern.Growth.ShopModifiers[0].Tribe);
        }

        private static MinionInstance TestInstance(string id)
        {
            return new MinionInstance
            {
                InstanceId = id,
                DefinitionId = id,
                CardId = id.ToUpperInvariant(),
                Name = id,
                BaseAttack = 2,
                BaseHealth = 2,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>()
            };
        }
    }
}
