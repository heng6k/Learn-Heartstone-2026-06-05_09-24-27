using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class Season14ContentCatalogTests
    {
        [Test]
        public void EmbeddedSnapshot_LoadsAllPreviewDarkGiftsWithStableResearchRevisions()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var english = snapshot.English.DarkGifts.All;
            var chinese = snapshot.Chinese.DarkGifts.All;

            Assert.AreEqual(43, english.Count);
            Assert.AreEqual(43, chinese.Count);
            Assert.AreEqual(43, english.Select(item => item.Id).Distinct().Count());
            Assert.AreEqual(43, english.Select(item => item.CardId).Distinct().Count());
            Assert.IsTrue(english.All(item => item.Id == item.CardId));
            Assert.IsTrue(english.All(item => item.CardId.StartsWith("BG36_MidGameEffect_", System.StringComparison.Ordinal)));
            Assert.IsTrue(english.All(item => item.DbfId > 0));
            Assert.AreEqual(43, english.Select(item => item.RevisionId).Distinct().Count());
            Assert.AreEqual(43, english.Select(item => item.EffectRevision).Distinct().Count());
            Assert.AreEqual(21, english.Count(item => item.AvailabilityTags.Contains("tras-pool")));
            CollectionAssert.AreEquivalent(
                Enumerable.Range(1, 43).Select(index => "DG-R" + index.ToString("00")),
                english.Select(item => item.ResearchKey));
            Assert.IsTrue(english.All(item => item.SourceLevel == "LiveClientCrossChecked"));
            Assert.AreEqual(0, english.Count(item => item.ImplementationStatus == DarkGiftImplementationStatus.Planned));
            Assert.AreEqual(43, english.Count(item => item.ImplementationStatus == DarkGiftImplementationStatus.Implemented));
            CollectionAssert.AreEquivalent(
                Enumerable.Range(1, 43).Select(index => "DG-R" + index.ToString("00")),
                english.Where(item => item.ImplementationStatus == DarkGiftImplementationStatus.Implemented).Select(item => item.ResearchKey));
            Assert.IsTrue(english
                .Where(item => item.ImplementationStatus == DarkGiftImplementationStatus.Implemented)
                .All(item => item.EffectIds.Count == 1));
            Assert.AreEqual("Sunken Persistence", english.Single(item => item.ResearchKey == "DG-R01").DisplayName);
            Assert.AreEqual("沉没的传承", chinese.Single(item => item.ResearchKey == "DG-R01").DisplayName);
            Assert.AreEqual("泰坦之力", chinese.Single(item => item.ResearchKey == "DG-R42").DisplayName);
            Assert.AreEqual("BG36_MidGameEffect_000t62", english.Single(item => item.ResearchKey == "DG-R01").CardId);
            Assert.AreEqual(133310, english.Single(item => item.ResearchKey == "DG-R01").DbfId);
            Assert.AreEqual("BG36_MidGameEffect_000t28t", english.Single(item => item.ResearchKey == "DG-R14").CardId);
            Assert.AreEqual("BG36_MidGameEffect_000t28", english.Single(item => item.ResearchKey == "DG-R31").CardId);
            Assert.AreEqual("BG36_MidGameEffect_000t64", english.Single(item => item.ResearchKey == "DG-R25").CardId);
            Assert.AreEqual("BG36_MidGameEffect_000t64t", english.Single(item => item.ResearchKey == "DG-R39").CardId);
            Assert.IsTrue(english.All(item => !string.IsNullOrWhiteSpace(item.ImagePath)));
            Assert.IsTrue(english.All(item => Resources.Load<Texture2D>(item.ImagePath) != null));
            Assert.AreEqual(2, english.Single(item => item.ResearchKey == "DG-R26").TriggerDelayRounds);
        }

        [Test]
        public void EmbeddedVersionContent_ResolvesPreviewWithAllGiftRevisionsButKeepsLegacyDefault()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var content = snapshot.VersionedContent;

            Assert.NotNull(content);
            Assert.AreEqual(GameVersionIds.LegacyCompositeSandbox, content.Versions.Default.Id);
            Assert.IsFalse(content.Versions.Get(GameVersionIds.Season14Preview).IsDefaultCandidate);

            var season14 = content.Versions.Get(GameVersionIds.Season14Preview);
            Assert.AreEqual("36.2", season14.DisplayName);
            Assert.AreEqual(GameVersionOfficialStatus.Released, season14.OfficialStatus);
            Assert.AreEqual(GameVersionImplementationStatus.Partial, season14.ImplementationStatus);

            var resolved = content.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);

            Assert.AreEqual(43, resolved.ContentSet.DarkGiftRevisionIds.Count);
            Assert.AreEqual(43, resolved.EntityRevisions.Count(item => item.Kind == EntityKind.DarkGift));
            Assert.AreEqual(DarkGiftProfiles.Season14PreviewId, resolved.Ruleset.DarkGiftProfile.Id);
            Assert.AreEqual(3, resolved.Ruleset.DarkGiftProfile.NormalEntryStartRound);
            Assert.AreEqual(3, resolved.Ruleset.DarkGiftProfile.GoldCost);
            Assert.AreEqual(GameVersionImplementationStatus.Partial, resolved.GameVersion.ImplementationStatus);
        }

        [Test]
        public void EmbeddedDarkGiftDefinitions_KeepRoundWindowsAndCompatibilityFacts()
        {
            var gifts = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha").English.DarkGifts;
            var toxicity = gifts.GetByResearchKey("DG-R35");
            var invulnerability = gifts.GetByResearchKey("DG-R43");
            var torethsBlessing = gifts.GetByResearchKey("DG-R19");

            Assert.AreEqual(7, toxicity.EarliestOfferRound);
            Assert.AreEqual(0, toxicity.LatestOfferRound);
            CollectionAssert.Contains(toxicity.RequiredMinionTags, "tribe:murloc");
            CollectionAssert.Contains(toxicity.ExcludedMinionTags, "keyword:venomous");
            CollectionAssert.Contains(toxicity.ExcludedMinionTags, "keyword:poisonous");
            Assert.AreEqual(12, invulnerability.EarliestOfferRound);
            CollectionAssert.Contains(invulnerability.ExcludedMinionTags, "keyword:taunt");
            CollectionAssert.Contains(invulnerability.ExcludedMinionTags, "tribe:none");
            CollectionAssert.Contains(torethsBlessing.RequiredMinionTags, "keyword:divine-shield");
        }
    }
}
