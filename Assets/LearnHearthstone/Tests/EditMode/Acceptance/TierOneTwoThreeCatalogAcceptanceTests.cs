using System.Linq;
using LearnHearthstone.Adapters.Data;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TierOneTwoThreeCatalogAcceptanceTests
    {
        [Test]
        public void MinionCatalog_AllTierOneTwoThreeInPoolMinionsAreAcceptanceReady()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            var tierOneToThree = catalog.All
                .Where(minion => minion.InPool && minion.TavernTier >= 1 && minion.TavernTier <= 3)
                .ToList();

            Assert.AreEqual(106, tierOneToThree.Count);
            Assert.AreEqual(22, tierOneToThree.Count(minion => minion.TavernTier == 1));
            Assert.AreEqual(36, tierOneToThree.Count(minion => minion.TavernTier == 2));
            Assert.AreEqual(48, tierOneToThree.Count(minion => minion.TavernTier == 3));
            Assert.IsTrue(tierOneToThree.All(minion => !string.IsNullOrWhiteSpace(minion.CardId)));
            Assert.IsTrue(tierOneToThree.All(minion => !string.IsNullOrWhiteSpace(minion.Name)));
            Assert.IsTrue(tierOneToThree.All(minion => minion.BaseAttack >= 0 && minion.BaseHealth >= 1));
            Assert.IsTrue(tierOneToThree.All(minion => minion.Tribes != null && minion.Tribes.Count > 0));
            Assert.IsTrue(tierOneToThree.All(minion => minion.Keywords != null));
            Assert.IsTrue(tierOneToThree.All(minion => minion.Tags != null && minion.Tags.Contains("tier_" + minion.TavernTier)));
        }

        [Test]
        public void SpellCatalog_AllTierOneTwoThreeInPoolTavernSpellsAreAcceptanceReady()
        {
            var catalog = SpellCatalogLoader.LoadFromResources();
            var tierOneToThree = catalog.All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell" && spell.TavernTier >= 1 && spell.TavernTier <= 3)
                .ToList();

            Assert.AreEqual(30, tierOneToThree.Count);
            Assert.AreEqual(8, tierOneToThree.Count(spell => spell.TavernTier == 1));
            Assert.AreEqual(6, tierOneToThree.Count(spell => spell.TavernTier == 2));
            Assert.AreEqual(16, tierOneToThree.Count(spell => spell.TavernTier == 3));
            Assert.IsTrue(tierOneToThree.All(spell => spell.SourceId > 0));
            Assert.IsTrue(tierOneToThree.All(spell => !string.IsNullOrWhiteSpace(spell.Name)));
            Assert.IsTrue(tierOneToThree.All(spell => !string.IsNullOrWhiteSpace(spell.CardNumber)));
            Assert.IsTrue(tierOneToThree.All(spell => spell.Cost >= 0));
            Assert.IsTrue(tierOneToThree.All(spell => !string.IsNullOrWhiteSpace(spell.ImagePath)));
            Assert.IsTrue(tierOneToThree.All(spell => spell.Tags != null && spell.Tags.Contains("tier_" + spell.TavernTier)));
        }
    }
}
