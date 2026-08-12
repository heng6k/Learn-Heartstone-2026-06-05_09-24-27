using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Content
{
    public sealed class VersionedContentCatalog
    {
        public VersionedContentCatalog(
            GameVersionCatalog versions,
            IEnumerable<RulesetDefinition> rulesets,
            IEnumerable<ContentSetDefinition> contentSets,
            IEnumerable<EntityRevisionDefinition> entityRevisions)
        {
            Versions = versions ?? throw new ArgumentNullException(nameof(versions));
            Rulesets = ReadOnly(rulesets);
            ContentSets = ReadOnly(contentSets);
            EntityRevisions = ReadOnly(entityRevisions);
        }

        public GameVersionCatalog Versions { get; }
        public ReadOnlyCollection<RulesetDefinition> Rulesets { get; }
        public ReadOnlyCollection<ContentSetDefinition> ContentSets { get; }
        public ReadOnlyCollection<EntityRevisionDefinition> EntityRevisions { get; }

        public GameVersionResolver CreateResolver()
        {
            return new GameVersionResolver(Versions, Rulesets, ContentSets, EntityRevisions);
        }

        private static ReadOnlyCollection<T> ReadOnly<T>(IEnumerable<T> values)
        {
            return Array.AsReadOnly((values ?? Enumerable.Empty<T>()).ToArray());
        }
    }
}
