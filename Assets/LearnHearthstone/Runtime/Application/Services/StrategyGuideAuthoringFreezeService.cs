using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Application.Services
{
    public static class StrategyGuideAuthoringFreezeService
    {
        public const int SchemaVersion = 1;
        public const int MaximumAuthoringRound = 99;
        public const int MaximumAuthoringGold = 99;

        private const string AuthoringCatalogRevision = "strategy-guide-authoring-v1";
        private const string RevisionSentinel = "authoring-draft";

        public static StrategyGuideAuthoringFreezeResult Freeze(
            StrategyGuideAuthoringDraft draft,
            StrategyGuideCatalog source,
            ResolvedGameVersion version)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            var result = new StrategyGuideAuthoringFreezeResult();
            ValidateDraftBoundary(draft, result.Diagnostics);
            if (result.Diagnostics.Count > 0)
            {
                return result;
            }

            var candidate = Clone(draft.Guide);
            candidate.RevisionId = candidate.GuideId + "@" + RevisionSentinel;
            StrategyGuideCatalog authoringCatalog;
            try
            {
                authoringCatalog = BuildCatalog(candidate, source.Opponents);
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is InvalidOperationException)
            {
                result.Diagnostics.Add("authoring.catalog.invalid:" + exception.Message);
                return result;
            }

            var validation = StrategyGuideValidator.Validate(authoringCatalog, candidate, version);
            if (!validation.IsValid)
            {
                result.Diagnostics.AddRange(validation.Errors);
                return result;
            }

            var provisionalCode = StrategyGuidePortableCodeService.ExportGuide(
                authoringCatalog,
                candidate.GuideId,
                version);
            var hash = provisionalCode.Split('.')[2];
            candidate.RevisionId = candidate.GuideId + "@" + hash.Substring(0, 16);

            var frozenCatalog = BuildCatalog(candidate, source.Opponents);
            var finalValidation = StrategyGuideValidator.Validate(frozenCatalog, candidate, version);
            if (!finalValidation.IsValid)
            {
                result.Diagnostics.AddRange(finalValidation.Errors);
                return result;
            }

            result.Guide = candidate;
            result.ContentHash = hash;
            return result;
        }

        public static StrategyGuideCatalog CreateFrozenCatalog(
            StrategyGuideAuthoringFreezeResult frozen,
            StrategyGuideCatalog source)
        {
            if (frozen == null || !frozen.Succeeded)
            {
                throw new InvalidOperationException("A successful frozen strategy guide is required.");
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return BuildCatalog(Clone(frozen.Guide), source.Opponents);
        }

        private static void ValidateDraftBoundary(
            StrategyGuideAuthoringDraft draft,
            ICollection<string> diagnostics)
        {
            if (draft.SchemaVersion != SchemaVersion)
            {
                diagnostics.Add("authoring.schema.unsupported");
            }
            if (string.IsNullOrWhiteSpace(draft.DraftId))
            {
                diagnostics.Add("authoring.draft-id.required");
            }
            if (draft.Guide == null)
            {
                diagnostics.Add("authoring.guide.required");
                return;
            }

            foreach (var profile in draft.Guide.EntryProfiles ?? new List<StrategyGuideEntryProfileDefinition>())
            {
                if (profile == null)
                {
                    continue;
                }
                if (profile.StartRound < 1 || profile.StartRound > MaximumAuthoringRound)
                {
                    AddOnce(diagnostics, "authoring.start-round.range");
                }
                if (profile.Gold < 0 || profile.Gold > MaximumAuthoringGold)
                {
                    AddOnce(diagnostics, "authoring.gold.range");
                }
                if (profile.MaxGold < 0 || profile.MaxGold > MaximumAuthoringGold)
                {
                    AddOnce(diagnostics, "authoring.max-gold.range");
                }
            }
        }

        private static StrategyGuideCatalog BuildCatalog(
            StrategyGuideDefinition guide,
            IReadOnlyList<StrategyGuideOpponentDefinition> opponents)
        {
            return new StrategyGuideCatalog(new StrategyGuideCatalogDefinition
            {
                SchemaVersion = 2,
                CatalogRevisionId = AuthoringCatalogRevision,
                Guides = new List<StrategyGuideDefinition> { guide },
                Opponents = (opponents ?? Array.Empty<StrategyGuideOpponentDefinition>()).ToList()
            });
        }

        private static T Clone<T>(T value)
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        }

        private static void AddOnce(ICollection<string> values, string value)
        {
            if (!values.Contains(value))
            {
                values.Add(value);
            }
        }
    }
}
