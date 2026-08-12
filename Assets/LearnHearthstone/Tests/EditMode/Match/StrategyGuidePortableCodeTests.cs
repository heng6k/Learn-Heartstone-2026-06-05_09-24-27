using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuidePortableCodeTests
    {
        [Test]
        public void ExportImport_AllShowcaseGuidesRoundTripToSameCodeAndScenario()
        {
            var source = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();

            foreach (var guide in source.Guides)
            {
                var profile = source.GetDefaultProfile(guide.GuideId);
                var code = StrategyGuidePortableCodeService.Export(source, guide.GuideId, profile.ProfileId, version);
                var imported = StrategyGuidePortableCodeService.Import(code, version);

                Assert.IsTrue(imported.IsCompatible, Diagnostics(imported));
                Assert.AreEqual(guide.GuideId, imported.Guide.GuideId);
                Assert.AreEqual(guide.RevisionId, imported.Guide.RevisionId);
                Assert.AreEqual(profile.ProfileId, imported.Profile.ProfileId);
                Assert.AreEqual(
                    code,
                    StrategyGuidePortableCodeService.Export(
                        imported.Catalog,
                        imported.Guide.GuideId,
                        imported.Profile.ProfileId,
                        version));

                var builtIn = StrategyGuideScenarioCompiler.Compile(
                    source,
                    guide,
                    version,
                    profileId: profile.ProfileId);
                var portable = StrategyGuideScenarioCompiler.Compile(
                    imported.Catalog,
                    imported.Guide,
                    version,
                    profileId: imported.Profile.ProfileId);
                Assert.AreEqual(builtIn.Scenario.Name, portable.Scenario.Name);
                Assert.AreEqual(builtIn.Scenario.RngState.Algorithm, portable.Scenario.RngState.Algorithm);
                Assert.AreEqual(builtIn.Scenario.RngState.Seed, portable.Scenario.RngState.Seed);
                Assert.AreEqual(builtIn.Scenario.RngState.Round, portable.Scenario.RngState.Round);
                Assert.AreEqual(builtIn.Scenario.RngState.RecruitLogCursor, portable.Scenario.RngState.RecruitLogCursor);
                Assert.AreEqual(builtIn.Scenario.RngState.MechanicEventCursor, portable.Scenario.RngState.MechanicEventCursor);
                Assert.AreEqual(builtIn.Opponent.OpponentId, portable.Opponent.OpponentId);
                CollectionAssert.AreEqual(
                    builtIn.Scenario.PlayerBoard.Select(item => item.CardId),
                    portable.Scenario.PlayerBoard.Select(item => item.CardId));
                CollectionAssert.AreEqual(
                    builtIn.Scenario.Hand.Select(item => item.CardId),
                    portable.Scenario.Hand.Select(item => item.CardId));
                CollectionAssert.AreEqual(
                    builtIn.Scenario.Shop.Select(item => item.CardId),
                    portable.Scenario.Shop.Select(item => item.CardId));
            }
        }

        [Test]
        public void ExportGuide_UsesOneCanonicalCodeForAllEntryProfilesAndDefersProfileSelection()
        {
            var source = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();

            foreach (var guide in source.Guides)
            {
                var code = StrategyGuidePortableCodeService.ExportGuide(source, guide.GuideId, version);
                var payload = DecodePayload(code);
                var imported = StrategyGuidePortableCodeService.Import(code, version);

                Assert.AreEqual(JTokenType.Null, payload["ProfileId"]?.Type);
                Assert.IsTrue(imported.IsCompatible, Diagnostics(imported));
                Assert.IsNull(imported.Profile);
                Assert.AreEqual(guide.GuideId, imported.Guide.GuideId);
                Assert.AreEqual(guide.RevisionId, imported.Guide.RevisionId);
                CollectionAssert.AreEqual(
                    guide.EntryProfiles.Select(item => item.ProfileId),
                    imported.Guide.EntryProfiles.Select(item => item.ProfileId));
                Assert.AreEqual(
                    code,
                    StrategyGuidePortableCodeService.ExportGuide(
                        imported.Catalog,
                        imported.Guide.GuideId,
                        version));
            }
        }

        [Test]
        public void ExportImport_ExactNonShowcaseProfileRemainsAvailableAsInternalDeepLink()
        {
            var source = StrategyGuideCatalogLoader.LoadFromResources();
            var version = ResolveSeason14();
            var guide = source.Guides[0];
            var profile = guide.EntryProfiles.Single(item =>
                item.Difficulty == StrategyGuideDifficulties.OpenBuild);

            var code = StrategyGuidePortableCodeService.Export(
                source,
                guide.GuideId,
                profile.ProfileId,
                version);
            var imported = StrategyGuidePortableCodeService.Import(code, version);

            Assert.IsTrue(imported.IsCompatible, Diagnostics(imported));
            Assert.AreEqual(profile.ProfileId, imported.Profile.ProfileId);
            Assert.AreEqual(profile.Difficulty, imported.Profile.Difficulty);
            Assert.AreEqual(
                code,
                StrategyGuidePortableCodeService.Export(
                    imported.Catalog,
                    imported.Guide.GuideId,
                    imported.Profile.ProfileId,
                    version));
        }

        [TestCase(null, "portable.code.required")]
        [TestCase("", "portable.code.required")]
        [TestCase("OTHER.a.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "portable.code.prefix")]
        [TestCase("LHSG1.only-two", "portable.code.segment-count")]
        [TestCase("LHSG1.!.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "portable.code.base64")]
        public void Import_RejectsMalformedCodeWithoutCreatingCatalog(string code, string expected)
        {
            AssertRejected(StrategyGuidePortableCodeService.Import(code, ResolveSeason14()), expected);
        }

        [Test]
        public void Import_RejectsHashTampering()
        {
            var code = ExportFirst();
            var parts = code.Split('.');
            parts[2] = new string('a', 64);

            AssertRejected(
                StrategyGuidePortableCodeService.Import(string.Join(".", parts), ResolveSeason14()),
                "portable.code.hash-mismatch");
        }

        [TestCase("GameVersionId", "legacy-composite-sandbox", "portable.version.mismatch")]
        [TestCase("RulesetId", "wrong-ruleset", "portable.ruleset.mismatch")]
        [TestCase("ContentSnapshotId", "wrong-snapshot", "portable.snapshot.mismatch")]
        [TestCase("ContentFingerprint", "wrong-fingerprint", "portable.fingerprint.mismatch")]
        public void Import_RejectsMismatchedContentIdentity(string property, string value, string expected)
        {
            var payload = DecodePayload(ExportFirst());
            payload[property] = value;

            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(payload), ResolveSeason14()),
                expected);
        }

        [Test]
        public void Import_RejectsUnknownProfileInvalidCardAndMissingOpponent()
        {
            var unknownProfile = DecodePayload(ExportFirst());
            unknownProfile["ProfileId"] = "missing-profile";
            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(unknownProfile), ResolveSeason14()),
                "portable.profile.missing");

            var invalidCard = DecodePayload(ExportFirst());
            invalidCard["Guide"]["EntryProfiles"][0]["Placements"][0]["CardId"] = "UNKNOWN_PORTABLE_CARD";
            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(invalidCard), ResolveSeason14()),
                "portable.guide.invalid");

            var missingOpponent = DecodePayload(ExportFirst());
            missingOpponent["Opponents"] = new JArray();
            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(missingOpponent), ResolveSeason14()),
                "portable.guide.invalid");
        }

        [Test]
        public void Import_RejectsUnknownHeroTrinketAndDarkGiftReferences()
        {
            var invalidHero = DecodePayload(ExportFirst());
            invalidHero["Guide"]["HeroCardId"] = "UNKNOWN_PORTABLE_HERO";
            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(invalidHero), ResolveSeason14()),
                "portable.guide.invalid");

            var invalidTrinket = DecodePayload(ExportFirst());
            invalidTrinket["Guide"]["LesserTrinketCardId"] = "UNKNOWN_PORTABLE_TRINKET";
            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(invalidTrinket), ResolveSeason14()),
                "portable.guide.invalid");

            var invalidDarkGift = DecodePayload(ExportFirst());
            invalidDarkGift["Guide"]["EntryProfiles"][0]["DarkGiftAttachments"][0]["GiftResearchKey"] =
                "UNKNOWN_PORTABLE_DARK_GIFT";
            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(invalidDarkGift), ResolveSeason14()),
                "portable.guide.invalid");
        }

        [Test]
        public void ExportImport_RoundTripsOptionalAcquisitionPlanWithoutAnotherPayloadShape()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var profile = catalog.GetDefaultProfile(guide.GuideId);
            profile.AcquisitionPlan = new StrategyGuideAcquisitionPlanDefinition
            {
                DiscloseControlledOffers = true,
                OfferSchedules = new List<StrategyGuideOfferScheduleDefinition>
                {
                    new StrategyGuideOfferScheduleDefinition
                    {
                        ScheduleId = "portable-guided-probe",
                        Source = StrategyGuideOfferSources.TripleRewardDiscover,
                        TriggerOccurrence = 1,
                        Policy = StrategyGuideOfferPolicies.MustInclude,
                        CardKind = StrategyGuideCardKinds.Minion,
                        OptionCount = 3,
                        TargetCardIds = new List<string> { guide.CoreMinionCardIds[2] },
                        Label = "受控候选"
                    }
                }
            };
            var version = ResolveSeason14();

            var code = StrategyGuidePortableCodeService.Export(catalog, guide.GuideId, profile.ProfileId, version);
            var imported = StrategyGuidePortableCodeService.Import(code, version);

            Assert.IsTrue(imported.IsCompatible, Diagnostics(imported));
            var plan = imported.Profile.AcquisitionPlan;
            Assert.NotNull(plan);
            Assert.IsTrue(plan.DiscloseControlledOffers);
            Assert.AreEqual("portable-guided-probe", plan.OfferSchedules.Single().ScheduleId);
            Assert.AreEqual(guide.CoreMinionCardIds[2], plan.OfferSchedules.Single().TargetCardIds.Single());
            Assert.AreEqual(
                code,
                StrategyGuidePortableCodeService.Export(
                    imported.Catalog,
                    imported.Guide.GuideId,
                    imported.Profile.ProfileId,
                    version));
        }

        [Test]
        public void Import_RejectsUnknownAndDuplicateJsonMembers()
        {
            var unknown = DecodePayload(ExportFirst());
            unknown["UnexpectedMember"] = true;
            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(unknown), ResolveSeason14()),
                "portable.payload.schema");

            var canonical = CanonicalJson(DecodePayload(ExportFirst()));
            var duplicate = canonical.Insert(1, "\"SchemaVersion\":1,");
            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodeRawJson(duplicate), ResolveSeason14()),
                "portable.payload.invalid");
        }

        [Test]
        public void Import_RejectsPayloadThatExpandsBeyondOneMiB()
        {
            var oversized = "{\"Padding\":\"" + new string('x', StrategyGuidePortableCodeService.MaxJsonBytes) + "\"}";

            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodeRawJson(oversized), ResolveSeason14()),
                "portable.payload.too-large");
        }

        [Test]
        public void Import_RejectsEmptyGuideLevelProfileCollection()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            var payload = DecodePayload(StrategyGuidePortableCodeService.ExportGuide(
                catalog,
                guide.GuideId,
                ResolveSeason14()));
            payload["Guide"]["EntryProfiles"] = new JArray();

            AssertRejected(
                StrategyGuidePortableCodeService.Import(EncodePayload(payload), ResolveSeason14()),
                "portable.guide.invalid");
        }

        private static string ExportFirst()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.Guides[0];
            return StrategyGuidePortableCodeService.Export(
                catalog,
                guide.GuideId,
                catalog.GetDefaultProfile(guide.GuideId).ProfileId,
                ResolveSeason14());
        }

        private static JObject DecodePayload(string code)
        {
            var part = code.Split('.')[1].Replace('-', '+').Replace('_', '/');
            part = part.PadRight(part.Length + ((4 - part.Length % 4) % 4), '=');
            var compressed = Convert.FromBase64String(part);
            using var input = new MemoryStream(compressed);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, new UTF8Encoding(false, true));
            return JObject.Parse(reader.ReadToEnd());
        }

        private static string EncodePayload(JObject payload)
        {
            return EncodeRawJson(CanonicalJson(payload));
        }

        private static string EncodeRawJson(string json)
        {
            var bytes = new UTF8Encoding(false, true).GetBytes(json);
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            return StrategyGuidePortableCodeService.CodePrefix + "." +
                Convert.ToBase64String(output.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_') + "." +
                Sha256(bytes);
        }

        private static string CanonicalJson(JToken token)
        {
            return Canonicalize(token).ToString(Formatting.None);
        }

        private static JToken Canonicalize(JToken token)
        {
            if (token is JObject obj)
            {
                var result = new JObject();
                foreach (var property in obj.Properties().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    result.Add(property.Name, Canonicalize(property.Value));
                }
                return result;
            }
            if (token is JArray array)
            {
                return new JArray(array.Select(Canonicalize));
            }
            return token.DeepClone();
        }

        private static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return string.Concat(sha.ComputeHash(bytes).Select(item => item.ToString("x2")));
        }

        private static void AssertRejected(StrategyGuideImportResult result, string code)
        {
            Assert.AreEqual(StrategyGuideImportStatus.Rejected, result.Status, Diagnostics(result));
            Assert.IsNull(result.Catalog);
            CollectionAssert.Contains(result.Diagnostics.Select(item => item.Code).ToList(), code, Diagnostics(result));
        }

        private static string Diagnostics(StrategyGuideImportResult result)
        {
            return string.Join(" | ", (result?.Diagnostics ?? new System.Collections.Generic.List<StrategyGuideImportDiagnostic>())
                .Select(item => item.Code + ":" + item.Message));
        }

        private static ResolvedGameVersion ResolveSeason14()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            return snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
        }
    }
}
