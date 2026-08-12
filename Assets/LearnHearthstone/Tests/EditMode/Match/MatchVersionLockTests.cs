using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.Match
{
    public sealed class MatchVersionLockTests
    {
        [Test]
        public void CreateWithResolvedVersion_LocksIdentityAndResolvedPools()
        {
            var resolved = ResolveLegacy("version-lock-create");
            var setup = new MatchSetupOptions { UseEnglish = true };

            var service = MatchService.CreateWithResolvedVersion(resolved, 12345, setup: setup);

            Assert.AreSame(resolved.Snapshot.English, service.Catalogs);
            Assert.AreEqual(resolved.GameVersion.Id, setup.GameVersionId);
            Assert.AreEqual(resolved.Ruleset.Id, setup.RulesetId);
            Assert.AreEqual(resolved.ContentSnapshotId, setup.ContentSnapshotId);
            Assert.AreEqual(resolved.ContentFingerprint, setup.ContentFingerprint);
            Assert.AreEqual(setup.GameVersionId, service.State.GameVersionId);
            Assert.AreEqual(setup.RulesetId, service.State.RulesetId);
            Assert.AreEqual(setup.ContentSnapshotId, service.State.ContentSnapshotId);
            Assert.AreEqual(setup.ContentFingerprint, service.State.ContentFingerprint);
            CollectionAssert.IsNotEmpty(service.State.EnabledMinionCardIds);
            CollectionAssert.IsNotEmpty(service.State.EnabledTavernSpellCardNumbers);
            CollectionAssert.IsNotEmpty(service.State.EnabledQuestCardIds);
            CollectionAssert.IsNotEmpty(service.State.EnabledQuestRewardCardIds);
            CollectionAssert.IsNotEmpty(service.State.EnabledLesserTrinketCardIds);
            CollectionAssert.IsNotEmpty(service.State.EnabledGreaterTrinketCardIds);
            CollectionAssert.IsNotEmpty(service.State.EnabledAnomalyCardIds);
            CollectionAssert.IsNotEmpty(service.State.EnabledTimewarpedCardIds);
        }

        [Test]
        public void StartedMatch_IgnoresLaterSetupVersionChanges()
        {
            var resolved = ResolveLegacy("version-lock-immutable");
            var setup = new MatchSetupOptions();
            var service = MatchService.CreateWithResolvedVersion(resolved, 12345, setup: setup);
            var lockedFingerprint = service.State.ContentFingerprint;
            var lockedMinions = service.State.EnabledMinionCardIds.ToArray();

            setup.GameVersionId = GameVersionIds.Season14Preview;
            setup.RulesetId = RulesetIds.Season14Preview;
            setup.ContentSnapshotId = "changed-snapshot";
            setup.ContentFingerprint = "changed-fingerprint";
            setup.EnabledMinionCardIds.Clear();

            Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, service.State.GameVersionId);
            Assert.AreEqual(RulesetIds.LegacyCompositeSandbox, service.State.RulesetId);
            Assert.AreEqual(resolved.ContentSnapshotId, service.State.ContentSnapshotId);
            Assert.AreEqual(lockedFingerprint, service.State.ContentFingerprint);
            CollectionAssert.AreEqual(lockedMinions, service.State.EnabledMinionCardIds);
        }

        [Test]
        public void ValidateRestoredVersionLock_RejectsFingerprintMismatch()
        {
            var resolved = ResolveLegacy("version-lock-restore");
            var service = MatchService.CreateWithResolvedVersion(resolved);
            var restored = JsonUtility.FromJson<MatchState>(JsonUtility.ToJson(service.State));
            restored.ContentFingerprint = "different-fingerprint";

            Assert.Throws<InvalidDataException>(() => service.ValidateRestoredVersionLock(restored));
        }

        private static ResolvedGameVersion ResolveLegacy(string clientVersion)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load(clientVersion);
            return GameVersionResolver.CreateBuiltIn().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);
        }
    }
}
