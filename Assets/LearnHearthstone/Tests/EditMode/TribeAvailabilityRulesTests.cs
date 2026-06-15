using System.Collections.Generic;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TribeAvailabilityRulesTests
    {
        [Test]
        public void Normalize_DefaultsToAllPlayableTribesAndRemovesInvalidValues()
        {
            CollectionAssert.AreEqual(TribeAvailabilityRules.PlayableTribes, TribeAvailabilityRules.Normalize(null));
            CollectionAssert.AreEqual(
                new[] { Tribe.Beast, Tribe.Murloc },
                TribeAvailabilityRules.Normalize(new[] { Tribe.None, Tribe.Beast, Tribe.Beast, Tribe.All, Tribe.Murloc }));
        }

        [Test]
        public void IsMinionAvailable_HandlesNeutralAllSingleAndMultiTribeMinions()
        {
            var active = new[] { Tribe.Beast };

            Assert.IsTrue(TribeAvailabilityRules.IsMinionAvailable(Minion("neutral", Tribe.None), active));
            Assert.IsTrue(TribeAvailabilityRules.IsMinionAvailable(Minion("all", Tribe.All), active));
            Assert.IsTrue(TribeAvailabilityRules.IsMinionAvailable(Minion("beast", Tribe.Beast), active));
            Assert.IsTrue(TribeAvailabilityRules.IsMinionAvailable(Minion("multi", Tribe.Murloc, Tribe.Beast), active));
            Assert.IsFalse(TribeAvailabilityRules.IsMinionAvailable(Minion("murloc", Tribe.Murloc), active));
        }

        [Test]
        public void SpellTribes_UsesExplicitMappingAndFactionFallback()
        {
            var catalog = SpellCatalogLoader.LoadFromResources();

            CollectionAssert.AreEqual(new[] { Tribe.Pirate }, TribeAvailabilityRules.SpellTribes(catalog.GetByCardNumber("122182")));
            CollectionAssert.AreEqual(new[] { Tribe.Elemental }, TribeAvailabilityRules.SpellTribes(catalog.GetByCardNumber("130310")));
            CollectionAssert.AreEqual(new[] { Tribe.Undead }, TribeAvailabilityRules.SpellTribes(catalog.GetByCardNumber("122489")));
            CollectionAssert.AreEqual(new[] { Tribe.Pirate }, TribeAvailabilityRules.SpellTribes(catalog.GetByCardNumber("127506")));
            Assert.AreEqual(0, TribeAvailabilityRules.SpellTribes(catalog.GetByCardNumber("100596")).Count);
        }

        [Test]
        public void IsTavernSpellAvailable_FiltersMappedSpellsByActiveTribes()
        {
            var catalog = SpellCatalogLoader.LoadFromResources();

            Assert.IsFalse(TribeAvailabilityRules.IsTavernSpellAvailable(catalog.GetByCardNumber("122182"), new[] { Tribe.Beast }));
            Assert.IsTrue(TribeAvailabilityRules.IsTavernSpellAvailable(catalog.GetByCardNumber("122182"), new[] { Tribe.Pirate }));
            Assert.IsTrue(TribeAvailabilityRules.IsTavernSpellAvailable(catalog.GetByCardNumber("100596"), new[] { Tribe.Beast }));
        }

        private static MinionDefinition Minion(string id, params Tribe[] tribes)
        {
            return new MinionDefinition
            {
                Id = id,
                CardId = id,
                InPool = true,
                TavernTier = 1,
                Tribes = new List<Tribe>(tribes)
            };
        }
    }
}
