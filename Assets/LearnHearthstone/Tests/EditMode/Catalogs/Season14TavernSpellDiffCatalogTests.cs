using System;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14TavernSpellDiffCatalogTests
    {
        private static readonly string[] DiffCardNumbers =
        {
            "130310", "130311", "116596", "116221", "100596", "105665",
            "113902", "131153", "104445", "117567", "117584", "115910"
        };

        [Test]
        public void EmbeddedCatalog_DefinesReturnedAndGeneratedOnlySpellsWithOfficialStableIds()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var spells = snapshot.English.Spells;

            AssertSpell(spells.GetByCardNumber("116596"), "Gem Day", 3, 1);
            AssertSpell(spells.GetByCardNumber("116221"), "Fandral's Fortune", 6, 3);
            AssertSpell(spells.GetByCardNumber("117567"), "Alliance Flag", 1, 1);
            AssertSpell(spells.GetByCardNumber("117584"), "Forest's Bounty", 5, 2);
            AssertSpell(spells.GetByCardNumber("115910"), "Boundless Potential", 4, 3);

            Assert.AreEqual("宝石特训", snapshot.Chinese.Spells.GetByCardNumber("116596").Name);
            Assert.AreEqual("范达尔的佑护", snapshot.Chinese.Spells.GetByCardNumber("116221").Name);
            Assert.AreEqual("联盟旗帜", snapshot.Chinese.Spells.GetByCardNumber("117567").Name);
            Assert.AreEqual("森林秘宝", snapshot.Chinese.Spells.GetByCardNumber("117584").Name);
            Assert.AreEqual("无限潜力", snapshot.Chinese.Spells.GetByCardNumber("115910").Name);
        }

        [Test]
        public void PreviewContentSet_SelectsTwelveUniqueSpellDiffRevisionsWithoutChangingLegacy()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
            var diffRevisions = preview.EntityRevisions
                .Where(revision =>
                    revision.Kind == EntityKind.TavernSpell &&
                    DiffCardNumbers.Contains(revision.StableEntityId))
                .ToList();

            Assert.AreEqual(12, diffRevisions.Count);
            Assert.AreEqual(12, diffRevisions.Select(revision => revision.StableEntityId).Distinct(StringComparer.Ordinal).Count());
            Assert.AreEqual(17, preview.ContentSet.TavernSpellRevisionIds.Count);
            Assert.IsTrue(DiffCardNumbers.All(cardNumber =>
                diffRevisions.Single(revision => revision.StableEntityId == cardNumber)
                    .Tags.Any(tag => tag.StartsWith("research-key:SPELL-D", StringComparison.Ordinal))));
            Assert.IsFalse(legacy.EntityRevisions.Any(revision =>
                revision.Kind == EntityKind.TavernSpell && DiffCardNumbers.Contains(revision.StableEntityId)));
        }

        [Test]
        public void PreviewResolver_AppliesChangedSpellCostAndLocalizedTextOnlyToPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);

            StringAssert.Contains("+7/+7", preview.Snapshot.Chinese.Spells.GetByCardNumber("104445").Text);
            StringAssert.Contains("+6/+6", legacy.Snapshot.Chinese.Spells.GetByCardNumber("104445").Text);
            Assert.AreEqual(2, preview.Snapshot.English.Spells.GetByCardNumber("117584").Cost);
            StringAssert.Contains("triggers twice", preview.Snapshot.English.Spells.GetByCardNumber("117584").Text);
            StringAssert.Contains("触发两次", preview.Snapshot.Chinese.Spells.GetByCardNumber("117584").Text);
        }

        [Test]
        public void FandralFortune_IsAvailableOnlyWhenQuilboarAreActive()
        {
            var spell = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha")
                .English.Spells.GetByCardNumber("116221");

            CollectionAssert.AreEqual(new[] { Tribe.Quilboar }, TribeAvailabilityRules.SpellTribes(spell));
            Assert.IsTrue(TribeAvailabilityRules.IsTavernSpellAvailable(spell, new[] { Tribe.Quilboar }));
            Assert.IsFalse(TribeAvailabilityRules.IsTavernSpellAvailable(spell, new[] { Tribe.Elemental }));
        }

        private static void AssertSpell(TavernSpellDefinition spell, string name, int tier, int cost)
        {
            Assert.AreEqual(name, spell.Name);
            Assert.AreEqual(tier, spell.TavernTier);
            Assert.AreEqual(cost, spell.Cost);
            Assert.IsFalse(spell.InPool);
        }
    }
}
