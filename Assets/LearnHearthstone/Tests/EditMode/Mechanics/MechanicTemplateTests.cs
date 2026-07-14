using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MechanicTemplateTests
    {
        [Test]
        public void SpellCatalogLoader_ParsesExplicitTemplatesAndInfersLegacyTemplates()
        {
            var catalog = SpellCatalogLoader.LoadFromJson(
                "{\"count\":2,\"spells\":[" +
                "{\"id\":\"SPELL_EXPLICIT\",\"sourceId\":1,\"cardNumber\":\"S1\",\"name\":\"Explicit\",\"type\":\"Spell\",\"category\":\"Generated\",\"cost\":1,\"tavernTier\":1,\"inPool\":1,\"cardTemplate\":\"Spellcraft\",\"targetTemplate\":\"FriendlyMinion\",\"effectTemplate\":\"GrantKeyword\"}," +
                "{\"id\":\"SPELL_INFERRED\",\"sourceId\":2,\"cardNumber\":\"S2\",\"name\":\"Inferred\",\"type\":\"Spell\",\"category\":\"TavernSpell\",\"cost\":1,\"tavernTier\":1,\"inPool\":1,\"tags\":[\"tavern_spell\",\"targeted_attack_buff\",\"buff_spell\"]}" +
                "]}");

            var explicitSpell = catalog.GetById("SPELL_EXPLICIT");
            var inferredSpell = catalog.GetById("SPELL_INFERRED");

            Assert.AreEqual(SpellCardTemplate.Spellcraft, explicitSpell.CardTemplate);
            Assert.AreEqual(SpellTargetTemplate.FriendlyMinion, explicitSpell.TargetTemplate);
            Assert.AreEqual(SpellEffectTemplate.GrantKeyword, explicitSpell.EffectTemplate);
            Assert.AreEqual(SpellCardTemplate.TavernSpell, inferredSpell.CardTemplate);
            Assert.AreEqual(SpellTargetTemplate.FriendlyMinion, inferredSpell.TargetTemplate);
            Assert.AreEqual(SpellEffectTemplate.BuffStats, inferredSpell.EffectTemplate);
        }

        [Test]
        public void TrinketCatalogLoader_ParsesExplicitTemplatesAndInfersLegacyTemplates()
        {
            var catalog = TrinketCatalogLoader.LoadFromJson(
                "{\"count\":2,\"trinkets\":[" +
                "{\"id\":\"TRINKET_EXPLICIT\",\"cardId\":\"TRINKET_EXPLICIT\",\"name\":\"Explicit\",\"slotKind\":\"Lesser\",\"cost\":1,\"implementationStatus\":\"Implemented\",\"offerPoolStatus\":\"Offerable\",\"effectFamily\":\"pending\",\"effectIds\":[\"explicit\"],\"triggerTemplate\":\"SpellcraftCast\",\"effectTemplate\":\"SpellSynergy\"}," +
                "{\"id\":\"TRINKET_INFERRED\",\"cardId\":\"TRINKET_INFERRED\",\"name\":\"Inferred\",\"slotKind\":\"Greater\",\"cost\":2,\"implementationStatus\":\"Implemented\",\"offerPoolStatus\":\"Offerable\",\"effectFamily\":\"combat_start_buff\",\"effectIds\":[\"inferred\"],\"mechanics\":[\"StartOfCombat\"],\"tags\":[\"buff\"]}" +
                "]}");

            var explicitTrinket = catalog.GetById("TRINKET_EXPLICIT");
            var inferredTrinket = catalog.GetById("TRINKET_INFERRED");

            Assert.AreEqual(TrinketTriggerTemplate.SpellcraftCast, explicitTrinket.TriggerTemplate);
            Assert.AreEqual(TrinketEffectTemplate.SpellSynergy, explicitTrinket.EffectTemplate);
            Assert.AreEqual(TrinketTriggerTemplate.StartOfCombat, inferredTrinket.TriggerTemplate);
            Assert.AreEqual(TrinketEffectTemplate.BuffStats, inferredTrinket.EffectTemplate);
        }

        [Test]
        public void TimewarpedTavernCatalogLoader_ParsesExplicitMechanicTemplatesAndInfersLegacyTemplates()
        {
            var catalog = TimewarpedTavernCatalogLoader.LoadFromJson(
                "{\"count\":2,\"cards\":[" +
                "{\"cardId\":\"TIMEWARP_EXPLICIT\",\"name\":\"Explicit\",\"cardKind\":\"Minion\",\"timewarpKind\":\"Minor\",\"cost\":1,\"techLevel\":3,\"attack\":2,\"health\":2,\"mechanicTemplates\":[\"Battlecry\",\"Spellcraft\"]}," +
                "{\"cardId\":\"TIMEWARP_INFERRED\",\"name\":\"Inferred\",\"cardKind\":\"Minion\",\"timewarpKind\":\"Major\",\"cost\":1,\"techLevel\":5,\"attack\":2,\"health\":2,\"keywords\":[\"BATTLECRY\"],\"effectIds\":[\"timewarp_discover_current_tier_3\"]}" +
                "]}");

            var explicitCard = catalog.GetByCardId("TIMEWARP_EXPLICIT");
            var inferredCard = catalog.GetByCardId("TIMEWARP_INFERRED");

            CollectionAssert.AreEqual(
                new[] { TimewarpedMechanicTemplate.Battlecry, TimewarpedMechanicTemplate.Spellcraft },
                explicitCard.MechanicTemplates);
            Assert.AreEqual(TimewarpedMechanicTemplate.Battlecry, explicitCard.PrimaryMechanicTemplate);
            CollectionAssert.Contains(inferredCard.MechanicTemplates, TimewarpedMechanicTemplate.Battlecry);
            CollectionAssert.Contains(inferredCard.MechanicTemplates, TimewarpedMechanicTemplate.Discover);
        }

        [Test]
        public void ResourceCatalogs_LoadWithResolvedTemplates()
        {
            var spells = SpellCatalogLoader.LoadFromResources().All;
            var trinkets = TrinketCatalogLoader.LoadFromResources().All;
            var timewarpedCards = TimewarpedTavernCatalogLoader.LoadFromResources().All;

            Assert.IsTrue(spells.All(spell => spell.CardTemplate != SpellCardTemplate.Auto));
            Assert.IsTrue(spells.All(spell => spell.TargetTemplate != SpellTargetTemplate.Auto));
            Assert.IsTrue(spells.All(spell => spell.EffectTemplate != SpellEffectTemplate.Auto));
            Assert.IsTrue(trinkets.All(trinket => trinket.TriggerTemplate != TrinketTriggerTemplate.Auto));
            Assert.IsTrue(trinkets.All(trinket => trinket.EffectTemplate != TrinketEffectTemplate.Auto));
            Assert.IsTrue(timewarpedCards.All(card => card.PrimaryMechanicTemplate != TimewarpedMechanicTemplate.Auto));
            Assert.IsTrue(timewarpedCards.All(card => card.MechanicTemplates != null && card.MechanicTemplates.Count > 0));
        }
    }
}
