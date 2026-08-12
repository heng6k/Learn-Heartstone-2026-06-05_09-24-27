using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class StrategyGuidePortablePayload
    {
        public int SchemaVersion = 1;
        public string PackageType = "StrategyGuide";
        public string GameVersionId;
        public string RulesetId;
        public string ContentSnapshotId;
        public string ContentFingerprint;
        public string CatalogRevisionId;
        public string ProfileId;
        public StrategyGuideDefinition Guide;
        public List<StrategyGuideOpponentDefinition> Opponents = new List<StrategyGuideOpponentDefinition>();
    }

    public enum StrategyGuideImportStatus
    {
        Compatible,
        CompatibleWithWarnings,
        Rejected
    }

    public sealed class StrategyGuideImportDiagnostic
    {
        public string Code;
        public string Message;
        public bool IsWarning;
    }

    public sealed class StrategyGuideImportResult
    {
        public StrategyGuideImportStatus Status;
        public StrategyGuidePortablePayload Payload;
        public StrategyGuideCatalog Catalog;
        public StrategyGuideDefinition Guide;
        public StrategyGuideEntryProfileDefinition Profile;
        public List<StrategyGuideImportDiagnostic> Diagnostics = new List<StrategyGuideImportDiagnostic>();

        public bool IsCompatible =>
            Status == StrategyGuideImportStatus.Compatible ||
            Status == StrategyGuideImportStatus.CompatibleWithWarnings;
    }
}
