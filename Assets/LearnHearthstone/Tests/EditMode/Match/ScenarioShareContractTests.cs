using System;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class ScenarioShareContractTests
    {
        private const string ShareCode = "23456789ABCDEFGHJKMN";

        [Test]
        public void Create_ComposesExistingGuideShareCardAndCompiledScenario()
        {
            var context = CreateContext();

            var contract = ScenarioShareContractService.Create(
                context.Catalog,
                context.Guide.GuideId,
                context.Profile.ProfileId,
                context.Version,
                context.Snapshot.ForLanguage(false),
                "2345-6789-abcdefgh-jkmn",
                Handoff(),
                false);
            var shareCard = StrategyGuideShareCardService.Create(
                context.Catalog,
                context.Guide.GuideId,
                context.Profile.ProfileId,
                context.Version,
                context.Snapshot.ForLanguage(false),
                false);

            Assert.AreEqual(ScenarioShareContractVersions.Current, contract.SchemaVersion);
            Assert.AreEqual(context.Guide.GuideId + ":" + context.Profile.ProfileId, contract.SceneId);
            Assert.AreEqual(context.Guide.RevisionId, contract.RevisionId);
            Assert.AreEqual(ShareCode, contract.ShareCode);
            Assert.AreEqual(ScenarioSharePublicationStatuses.Published, contract.Status);
            Assert.AreEqual(shareCard.ContentHash, contract.ContentHash);
            Assert.AreEqual(context.Guide.Title, contract.Summary.Title);
            Assert.AreEqual(context.Profile.Difficulty, contract.Summary.Difficulty);
            Assert.AreEqual(context.Profile.Title, contract.Summary.DifficultyTitle);
            Assert.AreEqual(context.Guide.FinalComposition.Count, contract.Summary.FinalComposition.Count);
            CollectionAssert.AreEqual(context.Profile.AllowedCommands, contract.Content.AllowedActions);
            Assert.AreEqual(context.Profile.RequiredActions.Count, contract.Content.Steps.Count);
            Assert.AreEqual(context.Profile.Seed, contract.Content.State.Seed);
            Assert.AreEqual(context.Profile.ProfileId, contract.Content.State.Name.Split('#').Last());
            Assert.AreEqual(ScenarioShareCompatibilityStatuses.Compatible, contract.Compatibility.Status);
            Assert.IsEmpty(contract.Compatibility.Diagnostics);
            Assert.AreEqual("https://learn-hearthstone.example/play?scene=" + ShareCode, contract.Handoff.WebPlayUrl);
        }

        [Test]
        public void Create_UsesTheRequestedProfilePortableHash()
        {
            var context = CreateContext();
            var profile = context.Guide.EntryProfiles.Single(item =>
                item.Difficulty == StrategyGuideDifficulties.OpenBuild);

            var contract = ScenarioShareContractService.Create(
                context.Catalog,
                context.Guide.GuideId,
                profile.ProfileId,
                context.Version,
                context.Snapshot.ForLanguage(false),
                ShareCode,
                Handoff(),
                false);
            var expectedCode = StrategyGuidePortableCodeService.Export(
                context.Catalog,
                context.Guide.GuideId,
                profile.ProfileId,
                context.Version);
            var defaultCode = StrategyGuidePortableCodeService.Export(
                context.Catalog,
                context.Guide.GuideId,
                context.Catalog.GetDefaultProfile(context.Guide.GuideId).ProfileId,
                context.Version);

            Assert.AreEqual(expectedCode.Split('.')[2], contract.ContentHash);
            Assert.AreNotEqual(defaultCode.Split('.')[2], contract.ContentHash);
            Assert.AreEqual(context.Guide.GuideId + ":" + profile.ProfileId, contract.SceneId);
            Assert.AreEqual(profile.Difficulty, contract.Summary.Difficulty);
        }

        [Test]
        public void Serialize_UsesCamelCaseAndRoundTripsAuthoritativeScenarioState()
        {
            var context = CreateContext();
            var contract = CreateContract(context);

            var json = ScenarioShareContractService.Serialize(contract, true);
            var token = JObject.Parse(json);
            var roundTrip = ScenarioShareContractService.Deserialize(json);

            Assert.IsNotNull(token["schemaVersion"]);
            Assert.IsNull(token["SchemaVersion"]);
            Assert.AreEqual(ShareCode, token["shareCode"]?.Value<string>());
            Assert.AreEqual(3, token["content"]?["state"]?["schemaVersion"]?.Value<int>());
            Assert.AreEqual(contract.SceneId, roundTrip.SceneId);
            Assert.AreEqual(contract.Content.State.ContentFingerprint, roundTrip.Content.State.ContentFingerprint);
            Assert.AreEqual(contract.Content.State.PlayerBoard.Count, roundTrip.Content.State.PlayerBoard.Count);
        }

        [Test]
        public void GoldenFixture_MatchesTheCompilerBackedContractExactly()
        {
            var expected = JToken.Parse(ScenarioShareContractService.Serialize(CreateContract(CreateContext()), true));
            var path = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(),
                "MiniProgram",
                "fixtures",
                "scenario-share-golden.json"));
            var actual = JToken.Parse(File.ReadAllText(path));

            Assert.AreEqual(
                expected["contentHash"]?.Value<string>(),
                actual["contentHash"]?.Value<string>(),
                "Committed mini-program content hash drifted from the requested profile.");
            Assert.IsTrue(
                JToken.DeepEquals(expected, actual),
                "Committed mini-program golden fixture drifted from the Unity contract.");
        }

        [Test]
        public void EvaluateCompatibility_WarnsOnlyForSnapshotOrFingerprintDrift()
        {
            var context = CreateContext();
            var contract = CreateContract(context);
            var runtime = ScenarioShareContractService.CreateRuntimeIdentity(context.Version);
            runtime.ContentSnapshotId = "newer-snapshot";
            runtime.ContentFingerprint = "newer-fingerprint";

            var result = ScenarioShareContractService.EvaluateCompatibility(contract, runtime);

            Assert.AreEqual(ScenarioShareCompatibilityStatuses.CompatibleWithWarnings, result.Status);
            Assert.IsTrue(result.CanOpen);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    ScenarioShareDiagnosticCodes.ContentSnapshotMismatch,
                    ScenarioShareDiagnosticCodes.ContentFingerprintMismatch
                },
                result.Diagnostics.Select(item => item.Code));
        }

        [Test]
        public void EvaluateCompatibility_RejectsSchemaGameVersionAndRulesetMismatches()
        {
            var context = CreateContext();
            var contract = CreateContract(context);

            AssertRejected(
                contract,
                Identity(context, supportedContractSchemaVersion: 2),
                ScenarioShareDiagnosticCodes.ContractSchemaMismatch);
            AssertRejected(
                contract,
                Identity(context, supportedScenarioSchemaVersion: 2),
                ScenarioShareDiagnosticCodes.ScenarioSchemaMismatch);
            AssertRejected(
                contract,
                Identity(context, gameVersionId: "other-version"),
                ScenarioShareDiagnosticCodes.GameVersionMismatch);
            AssertRejected(
                contract,
                Identity(context, rulesetId: "other-ruleset"),
                ScenarioShareDiagnosticCodes.RulesetMismatch);
            AssertRejected(
                contract,
                Identity(context, rulesetRevision: context.Version.Ruleset.SchemaVersion + 1),
                ScenarioShareDiagnosticCodes.RulesetRevisionMismatch);
        }

        [Test]
        public void NormalizeShareCode_RemovesSeparatorsUppercasesAndRejectsAmbiguousCharacters()
        {
            Assert.AreEqual(ShareCode, ScenarioShareContractService.NormalizeShareCode("2345 6789-abcd-efgh-jkmn"));
            Assert.Throws<ArgumentException>(() => ScenarioShareContractService.NormalizeShareCode("23456789ABCDEFGHJKMO"));
            Assert.Throws<ArgumentException>(() => ScenarioShareContractService.NormalizeShareCode("23456789ABCDEFGHJKM"));
        }

        private static void AssertRejected(
            ScenarioShareContract contract,
            ScenarioShareRuntimeIdentity identity,
            string diagnosticCode)
        {
            var result = ScenarioShareContractService.EvaluateCompatibility(contract, identity);

            Assert.AreEqual(ScenarioShareCompatibilityStatuses.Rejected, result.Status);
            Assert.IsFalse(result.CanOpen);
            CollectionAssert.Contains(result.Diagnostics.Select(item => item.Code).ToArray(), diagnosticCode);
        }

        private static ScenarioShareRuntimeIdentity Identity(
            TestContextData context,
            int? supportedContractSchemaVersion = null,
            int? supportedScenarioSchemaVersion = null,
            string gameVersionId = null,
            string rulesetId = null,
            int? rulesetRevision = null)
        {
            var identity = ScenarioShareContractService.CreateRuntimeIdentity(context.Version);
            identity.SupportedContractSchemaVersion = supportedContractSchemaVersion ?? identity.SupportedContractSchemaVersion;
            identity.SupportedScenarioSchemaVersion = supportedScenarioSchemaVersion ?? identity.SupportedScenarioSchemaVersion;
            identity.GameVersionId = gameVersionId ?? identity.GameVersionId;
            identity.RulesetId = rulesetId ?? identity.RulesetId;
            identity.RulesetRevision = rulesetRevision ?? identity.RulesetRevision;
            return identity;
        }

        private static ScenarioShareContract CreateContract(TestContextData context)
        {
            return ScenarioShareContractService.Create(
                context.Catalog,
                context.Guide.GuideId,
                context.Profile.ProfileId,
                context.Version,
                context.Snapshot.ForLanguage(false),
                ShareCode,
                Handoff(),
                false);
        }

        private static ScenarioShareHandoff Handoff()
        {
            return new ScenarioShareHandoff
            {
                WebPlayUrl = "https://learn-hearthstone.example/play?scene=" + ShareCode,
                ShareUrl = "https://learn-hearthstone.example/scenes/" + ShareCode,
                WindowsDownloadUrl = "https://learn-hearthstone.example/download"
            };
        }

        private static TestContextData CreateContext()
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var guide = catalog.Guides[0];
            var profile = guide.EntryProfiles.Single(item =>
                item.Difficulty == StrategyGuideDifficulties.Showcase);
            return new TestContextData(catalog, snapshot, version, guide, profile);
        }

        private sealed class TestContextData
        {
            public TestContextData(
                StrategyGuideCatalog catalog,
                GameCatalogSnapshot snapshot,
                ResolvedGameVersion version,
                StrategyGuideDefinition guide,
                StrategyGuideEntryProfileDefinition profile)
            {
                Catalog = catalog;
                Snapshot = snapshot;
                Version = version;
                Guide = guide;
                Profile = profile;
            }

            public StrategyGuideCatalog Catalog { get; }
            public GameCatalogSnapshot Snapshot { get; }
            public ResolvedGameVersion Version { get; }
            public StrategyGuideDefinition Guide { get; }
            public StrategyGuideEntryProfileDefinition Profile { get; }
        }
    }
}
