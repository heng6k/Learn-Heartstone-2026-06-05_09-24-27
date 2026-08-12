using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LearnHearthstone.Application.Services
{
    public static class ScenarioShareContractService
    {
        public const int ShareCodeLength = 20;

        private const string ShareCodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Culture = CultureInfo.InvariantCulture,
            DateParseHandling = DateParseHandling.None,
            NullValueHandling = NullValueHandling.Include
        };

        public static ScenarioShareContract Create(
            StrategyGuideCatalog catalog,
            string guideId,
            string profileId,
            ResolvedGameVersion version,
            GameCatalogSet catalogs,
            string shareCode,
            ScenarioShareHandoff handoff,
            bool useEnglish = false)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }
            if (catalogs == null)
            {
                throw new ArgumentNullException(nameof(catalogs));
            }

            var guide = catalog.GetGuide(guideId);
            var compiled = StrategyGuideScenarioCompiler.Compile(
                catalog,
                guide,
                version,
                useEnglish,
                profileId);
            var shareCard = StrategyGuideShareCardService.Create(
                catalog,
                guide.GuideId,
                compiled.Profile.ProfileId,
                version,
                catalogs,
                useEnglish);
            var normalizedShareCode = NormalizeShareCode(shareCode);

            return new ScenarioShareContract
            {
                SceneId = guide.GuideId + ":" + compiled.Profile.ProfileId,
                RevisionId = guide.RevisionId,
                ShareCode = normalizedShareCode,
                Status = ScenarioSharePublicationStatuses.Published,
                ContentHash = shareCard.ContentHash,
                Summary = BuildSummary(compiled.Profile, shareCard, version, useEnglish),
                Compatibility = BuildCompatibility(compiled.Scenario),
                Content = BuildContent(compiled.Profile, shareCard, compiled.Scenario, useEnglish),
                Handoff = CloneHandoff(handoff)
            };
        }

        public static string Serialize(ScenarioShareContract contract, bool indented = false)
        {
            if (contract == null)
            {
                throw new ArgumentNullException(nameof(contract));
            }
            return JsonConvert.SerializeObject(
                contract,
                indented ? Formatting.Indented : Formatting.None,
                JsonSettings);
        }

        public static ScenarioShareContract Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Scenario share JSON is required.", nameof(json));
            }
            return JsonConvert.DeserializeObject<ScenarioShareContract>(json, JsonSettings) ??
                throw new JsonSerializationException("Scenario share JSON is empty.");
        }

        public static ScenarioShareRuntimeIdentity CreateRuntimeIdentity(ResolvedGameVersion version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            var scenarioDefaults = new TestScenarioDefinition();
            return new ScenarioShareRuntimeIdentity
            {
                SupportedContractSchemaVersion = ScenarioShareContractVersions.Current,
                SupportedScenarioSchemaVersion = scenarioDefaults.SchemaVersion,
                SupportedMechanicStateSchemaVersion = scenarioDefaults.MechanicStateSchemaVersion,
                GameVersionId = version.GameVersion.Id,
                RulesetId = version.Ruleset.Id,
                RulesetRevision = version.Ruleset.SchemaVersion,
                ContentSnapshotId = version.ContentSnapshotId,
                ContentFingerprint = version.ContentFingerprint
            };
        }

        public static ScenarioShareCompatibilityResult EvaluateCompatibility(
            ScenarioShareContract contract,
            ScenarioShareRuntimeIdentity runtime)
        {
            if (contract == null)
            {
                throw new ArgumentNullException(nameof(contract));
            }
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }

            var result = new ScenarioShareCompatibilityResult();
            var state = contract.Content?.State;
            if (state == null)
            {
                Reject(result, ScenarioShareDiagnosticCodes.ScenarioStateMissing, "Scenario state is missing.");
            }
            if (contract.SchemaVersion != runtime.SupportedContractSchemaVersion)
            {
                Reject(result, ScenarioShareDiagnosticCodes.ContractSchemaMismatch, "Scenario share contract schema is not supported.");
            }
            if (state != null && state.SchemaVersion != runtime.SupportedScenarioSchemaVersion)
            {
                Reject(result, ScenarioShareDiagnosticCodes.ScenarioSchemaMismatch, "Scenario state schema is not supported.");
            }
            if (state != null && state.MechanicStateSchemaVersion != runtime.SupportedMechanicStateSchemaVersion)
            {
                Reject(result, ScenarioShareDiagnosticCodes.MechanicStateSchemaMismatch, "Scenario mechanic state schema is not supported.");
            }
            if (state != null && !Same(state.GameVersionId, runtime.GameVersionId))
            {
                Reject(result, ScenarioShareDiagnosticCodes.GameVersionMismatch, "Scenario game version does not match the current runtime.");
            }
            if (state != null && !Same(state.RulesetId, runtime.RulesetId))
            {
                Reject(result, ScenarioShareDiagnosticCodes.RulesetMismatch, "Scenario ruleset does not match the current runtime.");
            }
            if (state != null && state.RulesetRevision != runtime.RulesetRevision)
            {
                Reject(result, ScenarioShareDiagnosticCodes.RulesetRevisionMismatch, "Scenario ruleset revision does not match the current runtime.");
            }

            if (result.Diagnostics.Count == 0 && state != null)
            {
                if (!Same(state.ContentSnapshotId, runtime.ContentSnapshotId))
                {
                    Warn(result, ScenarioShareDiagnosticCodes.ContentSnapshotMismatch, "Content snapshot differs from the current runtime.");
                }
                if (!Same(state.ContentFingerprint, runtime.ContentFingerprint))
                {
                    Warn(result, ScenarioShareDiagnosticCodes.ContentFingerprintMismatch, "Content fingerprint differs from the current runtime.");
                }
            }

            result.Status = result.Diagnostics.Any(IsRejected)
                ? ScenarioShareCompatibilityStatuses.Rejected
                : result.Diagnostics.Count > 0
                    ? ScenarioShareCompatibilityStatuses.CompatibleWithWarnings
                    : ScenarioShareCompatibilityStatuses.Compatible;
            return result;
        }

        public static string NormalizeShareCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Share code is required.", nameof(value));
            }

            var builder = new StringBuilder(ShareCodeLength);
            foreach (var character in value)
            {
                if (character == '-' || char.IsWhiteSpace(character))
                {
                    continue;
                }

                var normalized = char.ToUpperInvariant(character);
                if (ShareCodeAlphabet.IndexOf(normalized) < 0)
                {
                    throw new ArgumentException("Share code contains an unsupported character.", nameof(value));
                }
                builder.Append(normalized);
            }

            if (builder.Length != ShareCodeLength)
            {
                throw new ArgumentException("Share code must contain exactly 20 characters.", nameof(value));
            }
            return builder.ToString();
        }

        private static ScenarioShareSummary BuildSummary(
            StrategyGuideEntryProfileDefinition profile,
            StrategyGuideShareCardModel shareCard,
            ResolvedGameVersion version,
            bool useEnglish)
        {
            return new ScenarioShareSummary
            {
                Title = shareCard.Title,
                Summary = shareCard.Summary,
                Archetype = shareCard.Archetype,
                Difficulty = profile.Difficulty,
                DifficultyTitle = Localized(profile.Title, profile.EnglishTitle, useEnglish),
                GameVersionId = version.GameVersion.Id,
                GameVersionName = version.GameVersion.DisplayName,
                Hero = MapAsset(shareCard.Hero),
                ActiveTribes = new List<string>(shareCard.ActiveTribes ?? new List<string>()),
                FinalComposition = (shareCard.FinalComposition ?? new List<StrategyGuideShareCardAsset>())
                    .Select(MapAsset)
                    .ToList()
            };
        }

        private static ScenarioShareCompatibility BuildCompatibility(TestScenarioDefinition scenario)
        {
            return new ScenarioShareCompatibility
            {
                ScenarioSchemaVersion = scenario.SchemaVersion,
                MechanicStateSchemaVersion = scenario.MechanicStateSchemaVersion,
                GameVersionId = scenario.GameVersionId,
                RulesetId = scenario.RulesetId,
                RulesetRevision = scenario.RulesetRevision,
                ContentSnapshotId = scenario.ContentSnapshotId,
                ContentFingerprint = scenario.ContentFingerprint,
                Status = ScenarioShareCompatibilityStatuses.Compatible
            };
        }

        private static ScenarioShareContent BuildContent(
            StrategyGuideEntryProfileDefinition profile,
            StrategyGuideShareCardModel shareCard,
            TestScenarioDefinition scenario,
            bool useEnglish)
        {
            var victory = profile.Victory ?? new StrategyGuideVictoryCondition();
            var undo = profile.Undo ?? new StrategyGuideUndoPolicy();
            var plan = profile.AcquisitionPlan ?? new StrategyGuideAcquisitionPlanDefinition();
            var content = new ScenarioShareContent
            {
                AllowedActions = new List<string>(profile.AllowedCommands ?? new List<string>()),
                Objectives = new ScenarioShareObjectives
                {
                    RequireFinalComposition = victory.RequireFinalComposition,
                    RequireCombatWin = victory.RequireCombatWin,
                    PostWinChoices = new List<string>(victory.PostWinChoices ?? new List<string>())
                },
                Steps = (profile.RequiredActions ?? new List<StrategyGuideRequiredAction>())
                    .Where(item => item != null)
                    .Select((item, index) => new ScenarioShareStep
                    {
                        Order = index + 1,
                        ActionId = item.ActionId,
                        Kind = item.Kind,
                        Count = item.Count,
                        Instruction = Localized(item.Instruction, item.EnglishInstruction, useEnglish),
                        SourcePlacementId = item.SourcePlacementId,
                        SourcePlacementIds = new List<string>(item.SourcePlacementIds ?? new List<string>()),
                        TargetPlacementId = item.TargetPlacementId,
                        ChoiceId = item.ChoiceId
                    })
                    .ToList(),
                DiscoveryRules = (plan.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                    .Where(item => item != null)
                    .Select(item => new ScenarioShareDiscoveryRule
                    {
                        ScheduleId = item.ScheduleId,
                        Source = item.Source,
                        Policy = item.Policy,
                        Label = Localized(item.Label, item.EnglishLabel, useEnglish),
                        CardKind = item.CardKind,
                        TavernTier = item.TavernTier,
                        OptionCount = item.OptionCount,
                        TargetCardIds = new List<string>(item.TargetCardIds ?? new List<string>()),
                        RequiredTribe = item.RequiredTribe,
                        MinimumRequiredTribeMinions = item.MinimumRequiredTribeMinions
                    })
                    .ToList(),
                Undo = new ScenarioShareUndoPolicy
                {
                    UsesPerRun = undo.UsesPerRun,
                    RestoreRng = undo.RestoreRng,
                    LockAfterTurnEnd = undo.LockAfterTurnEnd,
                    LockAfterCombat = undo.LockAfterCombat,
                    LockDuringFreeExplore = undo.LockDuringFreeExplore
                },
                State = scenario
            };
            if (!string.IsNullOrWhiteSpace(shareCard.ProbabilityNotice))
            {
                content.Hints.Add(shareCard.ProbabilityNotice);
            }
            content.Hints.AddRange(content.DiscoveryRules
                .Select(item => item.Label)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal));
            return content;
        }

        private static ScenarioShareAsset MapAsset(StrategyGuideShareCardAsset asset)
        {
            if (asset == null)
            {
                return new ScenarioShareAsset();
            }
            return new ScenarioShareAsset
            {
                StableId = asset.StableId,
                Kind = asset.CardKind.ToString(),
                Name = asset.Name,
                ImagePath = asset.ImagePath,
                Golden = asset.Golden,
                Badge = asset.Badge
            };
        }

        private static ScenarioShareHandoff CloneHandoff(ScenarioShareHandoff source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return new ScenarioShareHandoff
            {
                WebPlayUrl = Required(source.WebPlayUrl, nameof(source.WebPlayUrl)),
                ShareUrl = Required(source.ShareUrl, nameof(source.ShareUrl)),
                WindowsDownloadUrl = Required(source.WindowsDownloadUrl, nameof(source.WindowsDownloadUrl))
            };
        }

        private static void Reject(ScenarioShareCompatibilityResult result, string code, string message)
        {
            result.Diagnostics.Add(new ScenarioShareDiagnostic
            {
                Code = code,
                Level = ScenarioShareDiagnosticLevels.Error,
                Message = message
            });
        }

        private static void Warn(ScenarioShareCompatibilityResult result, string code, string message)
        {
            result.Diagnostics.Add(new ScenarioShareDiagnostic
            {
                Code = code,
                Level = ScenarioShareDiagnosticLevels.Warning,
                Message = message
            });
        }

        private static bool IsRejected(ScenarioShareDiagnostic diagnostic)
        {
            return diagnostic != null &&
                string.Equals(diagnostic.Level, ScenarioShareDiagnosticLevels.Error, StringComparison.Ordinal);
        }

        private static bool Same(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
        }

        private static string Localized(string chinese, string english, bool useEnglish)
        {
            return useEnglish ? FirstNonEmpty(english, chinese) : FirstNonEmpty(chinese, english);
        }

        private static string FirstNonEmpty(string primary, string fallback)
        {
            return !string.IsNullOrWhiteSpace(primary) ? primary : fallback;
        }

        private static string Required(string value, string parameterName)
        {
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
