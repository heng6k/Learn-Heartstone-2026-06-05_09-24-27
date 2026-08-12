using System;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class SetupSelectionPolicyTests
    {
        [Test]
        public void TribeBoundaries_AllowFiveThroughTenAndRejectIncompleteOrOverCapSelections()
        {
            var policy = SetupSelectionPolicy.CreateLegacyCompatible(10);

            Assert.AreEqual(5, SetupSelectionPolicy.DefaultRandomTribeCount);
            Assert.AreEqual(5, SetupSelectionPolicy.MinCustomTribeCount);
            Assert.AreEqual(10, SetupSelectionPolicy.SelectionCap);
            Assert.AreEqual(10, policy.MaxCustomTribeCount);
            Assert.IsFalse(policy.IsCustomTribeCountValid(4));
            Assert.IsTrue(policy.IsCustomTribeCountValid(5));
            Assert.IsTrue(policy.IsCustomTribeCountValid(6));
            Assert.IsTrue(policy.IsCustomTribeCountValid(10));
            Assert.IsFalse(policy.IsCustomTribeCountValid(11));
            Assert.IsTrue(policy.CanSelectAllPlayableTribes);

            var futurePolicy = SetupSelectionPolicy.CreateLegacyCompatible(11);
            Assert.AreEqual(10, futurePolicy.MaxCustomTribeCount);
            Assert.IsFalse(futurePolicy.CanSelectAllPlayableTribes, "Future catalogs over the cap must not silently select an arbitrary ten.");

            var incompletePolicy = SetupSelectionPolicy.CreateLegacyCompatible(4);
            Assert.IsFalse(incompletePolicy.HasCompletePlayableTribeCatalog);
            Assert.IsFalse(incompletePolicy.IsCustomTribeCountValid(4));
        }

        [Test]
        public void Season14Ruleset_AllowsAndDefaultsOnlyDarkGiftsAndTrinkets()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("p1b-policy-test");
            var resolved = snapshot.VersionedContent.CreateResolver()
                .Resolve(GameVersionIds.Season14Preview, snapshot.AsVersionResolutionSource());
            var policy = SetupSelectionPolicy.FromRuleset(resolved.Ruleset, 10);

            CollectionAssert.AreEquivalent(
                new[] { SetupMechanicIds.DarkGifts, SetupMechanicIds.Trinkets },
                policy.AllowedMechanicIds);
            CollectionAssert.AreEquivalent(
                new[] { SetupMechanicIds.DarkGifts, SetupMechanicIds.Trinkets },
                policy.DefaultMechanicIds);
            Assert.IsFalse(policy.AllowsMechanic("activate"));
            Assert.IsFalse(policy.AllowsMechanic("lockbox"));
            Assert.IsFalse(policy.AllowsMechanic("fishbait"));
            Assert.IsFalse(policy.AllowsMechanic(SetupMechanicIds.Quests));
            Assert.IsFalse(policy.AllowsMechanic(SetupMechanicIds.Anomalies));
            Assert.IsFalse(policy.AllowsMechanic(SetupMechanicIds.TimewarpedTavern));
        }

        [Test]
        public void LegacyRuleset_RetainsItsHistoricalSetupMechanics()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("p1b-policy-test");
            var resolved = snapshot.VersionedContent.CreateResolver()
                .Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot.AsVersionResolutionSource());
            var policy = SetupSelectionPolicy.FromRuleset(resolved.Ruleset, 10);

            CollectionAssert.AreEquivalent(SetupMechanicIds.LegacyComposite, policy.AllowedMechanicIds);
            Assert.IsTrue(SetupMechanicIds.LegacyComposite.All(policy.AllowsMechanic));
            Assert.IsFalse(policy.AllowsMechanic(SetupMechanicIds.DarkGifts));
        }

        [Test]
        public void Ruleset_RejectsDefaultMechanicOutsideAllowedSet()
        {
            Assert.Throws<ArgumentException>(() => new RulesetDefinition(
                "invalid-setup-policy",
                1,
                allowedSetupMechanicIds: new[] { SetupMechanicIds.Trinkets },
                defaultSetupMechanicIds: new[] { SetupMechanicIds.Quests }));
        }

        [Test]
        public void ContentFingerprint_ChangesWhenSetupMechanicPolicyChanges()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("p1b-policy-fingerprint-test");
            var source = snapshot.AsVersionResolutionSource();
            var catalog = snapshot.VersionedContent;
            var original = catalog.CreateResolver().Resolve(GameVersionIds.Season14Preview, source);
            var alteredRulesets = catalog.Rulesets.Select(ruleset =>
                ruleset.Id == RulesetIds.Season14Preview
                    ? new RulesetDefinition(
                        ruleset.Id,
                        ruleset.SchemaVersion,
                        ruleset.RuleFlags,
                        ruleset.TurnSchedule,
                        ruleset.MechanicProfiles,
                        ruleset.CompatibilityPolicy,
                        ruleset.DarkGiftProfile,
                        ruleset.VenomousEffectRevision,
                        new[] { SetupMechanicIds.Trinkets },
                        new[] { SetupMechanicIds.Trinkets })
                    : ruleset);
            var altered = new GameVersionResolver(
                    catalog.Versions,
                    alteredRulesets,
                    catalog.ContentSets,
                    catalog.EntityRevisions)
                .Resolve(GameVersionIds.Season14Preview, source);

            Assert.AreNotEqual(original.ContentFingerprint, altered.ContentFingerprint);
        }
    }
}
