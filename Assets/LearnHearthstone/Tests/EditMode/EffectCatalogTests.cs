using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class EffectCatalogTests
    {
        [Test]
        public void DefaultCatalog_ReturnsBattlecrySelfBuffEffect()
        {
            var catalog = MinionEffectCatalog.CreateDefault();

            var effect = catalog.Get("battlecry_self_buff_2_2");

            Assert.AreEqual("battlecry_self_buff_2_2", effect.Id);
            Assert.AreEqual(MechanicEventType.CardPlayed, effect.Triggers[0].EventType);
            Assert.AreEqual(EffectTargetType.Source, effect.Actions[0].Target.Type);
            Assert.AreEqual(MechanicActionType.BuffStats, effect.Actions[0].Action.Type);
            Assert.AreEqual(2, effect.Actions[0].Action.Attack);
            Assert.AreEqual(2, effect.Actions[0].Action.Health);
        }

        [Test]
        public void DefaultCatalog_ReturnsRepresentativeSliceEffects()
        {
            var catalog = MinionEffectCatalog.CreateDefault();
            var effectIds = new[]
            {
                "battlecry_random_friendly_buff_1_1",
                "future_shop_typed_buff_1_1",
                "battlecry_add_divine_shield_random_friendly",
                "battlecry_add_taunt_self",
                "combat_reborn_self",
                "deathrattle_summon_token_1",
                "deathrattle_buff_random_friendly_2_2",
                "avenge_2_buff_self_2_2",
                "card_bought_buff_shop_elemental_1_1",
                "summon_token_microbot"
            };

            foreach (var effectId in effectIds)
            {
                Assert.AreEqual(effectId, catalog.Get(effectId).Id);
            }
        }
    }
}
