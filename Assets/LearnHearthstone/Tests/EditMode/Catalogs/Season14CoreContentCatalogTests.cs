using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14CoreContentCatalogTests
    {
        private const string XaviusId = "BG36_HERO_105";
        private const string TrastathId = "BG36_HERO_101";

        [Test]
        public void EmbeddedCatalog_ContainsTwoLocalizedPreviewHeroesWithoutLeakingIntoLegacyPool()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var english = snapshot.English.Heroes;
            var chinese = snapshot.Chinese.Heroes;

            var xavius = english.GetHeroByCardId(XaviusId);
            var trastath = chinese.GetHeroByCardId(TrastathId);

            Assert.AreEqual("HERO-R01", xavius.ResearchKey);
            Assert.AreEqual("Nightmare Lord Xavius", xavius.Name);
            Assert.AreEqual("梦魇之王萨维斯", chinese.GetHeroByCardId(XaviusId).ZhName);
            Assert.AreEqual("Feel Devastation", xavius.HeroPower.Name);
            Assert.AreEqual("虚空能量", trastath.HeroPower.ZhName);
            Assert.AreEqual("OfficialPreview", xavius.SourceLevel);
            Assert.AreEqual(132608, xavius.HeroDbfId);
            Assert.AreEqual("BG36_HERO_105p", xavius.HeroPower.CardId);
            Assert.AreEqual(134010, xavius.HeroPower.DbfId);
            Assert.AreEqual(132578, trastath.HeroDbfId);
            Assert.AreEqual("BG36_HERO_101p", trastath.HeroPower.CardId);
            Assert.AreEqual(132581, trastath.HeroPower.DbfId);
            Assert.IsFalse(xavius.InPool);
            Assert.IsFalse(english.GetInitialSelectableHeroes().Any(hero => hero.HeroCardId == XaviusId));
        }

        [Test]
        public void PreviewVersion_SelectsBothHeroesAndTheirImmutableRevisions()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);

            Assert.IsTrue(resolved.Snapshot.English.Heroes.GetHeroByCardId(XaviusId).InPool);
            Assert.IsTrue(resolved.Snapshot.English.Heroes.GetHeroByCardId(TrastathId).InPool);
            Assert.IsTrue(resolved.Snapshot.English.Heroes.GetInitialSelectableHeroes().Any(hero => hero.HeroCardId == XaviusId));
            Assert.AreEqual(8, resolved.EntityRevisions.Count(item => item.Kind == EntityKind.Hero));
            Assert.AreEqual(8, resolved.ContentSet.HeroRevisionIds.Count);
            CollectionAssert.Contains(
                resolved.ContentSet.HeroRevisionIds.ToArray(),
                "preview-s14-hero-nightmare-lord-xavius@36.2-preview-v1");
            CollectionAssert.Contains(
                resolved.ContentSet.HeroRevisionIds.ToArray(),
                "preview-s14-hero-trastath-soul-parasite@36.2-preview-v1");
        }

        [Test]
        public void PreviewVersion_LoadsFiveNewSpellsWithOfficialIdsAndKeepsThemOutOfLegacy()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var officialIds = new[] { "132903", "132995", "133369", "133711", "133371" };

            Assert.IsTrue(officialIds.All(id => !snapshot.English.Spells.GetByCardNumber(id).InPool));
            Assert.AreEqual("Methodical Madness", snapshot.English.Spells.GetByCardNumber("132903").Name);
            Assert.AreEqual("理性癫狂", snapshot.Chinese.Spells.GetByCardNumber("132903").Name);
            Assert.AreEqual(4, snapshot.English.Spells.GetByCardNumber("132903").TavernTier);
            Assert.AreEqual(3, snapshot.English.Spells.GetByCardNumber("132903").Cost);
            var legacyPointyArrow = snapshot.English.Spells.GetByCardNumber("100596");
            Assert.AreEqual(1, legacyPointyArrow.TavernTier);
            Assert.AreEqual(1, legacyPointyArrow.Cost);
            Assert.IsTrue(legacyPointyArrow.InPool);

            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);

            Assert.IsTrue(officialIds.All(id => resolved.Snapshot.English.Spells.GetByCardNumber(id).InPool));
            Assert.AreEqual(17, resolved.EntityRevisions.Count(item => item.Kind == EntityKind.TavernSpell));
            Assert.AreEqual(17, resolved.ContentSet.TavernSpellRevisionIds.Count);
            Assert.IsFalse(resolved.Snapshot.English.Spells.GetByCardNumber("100596").InPool);
        }
    }
}
