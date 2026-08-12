using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class ScenarioShareContract
    {
        public int SchemaVersion = ScenarioShareContractVersions.Current;
        public string SceneId;
        public string RevisionId;
        public string ShareCode;
        public string Status = ScenarioSharePublicationStatuses.Published;
        public string ContentHash;
        public ScenarioShareSummary Summary = new ScenarioShareSummary();
        public ScenarioShareCompatibility Compatibility = new ScenarioShareCompatibility();
        public ScenarioShareContent Content = new ScenarioShareContent();
        public ScenarioShareHandoff Handoff = new ScenarioShareHandoff();
    }

    [Serializable]
    public sealed class ScenarioShareSummary
    {
        public string Title;
        public string Summary;
        public string Archetype;
        public string Difficulty;
        public string DifficultyTitle;
        public string GameVersionId;
        public string GameVersionName;
        public ScenarioShareAsset Hero = new ScenarioShareAsset();
        public List<string> ActiveTribes = new List<string>();
        public List<ScenarioShareAsset> FinalComposition = new List<ScenarioShareAsset>();
    }

    [Serializable]
    public sealed class ScenarioShareAsset
    {
        public string StableId;
        public string Kind;
        public string Name;
        public string ImagePath;
        public bool Golden;
        public string Badge;
    }

    [Serializable]
    public sealed class ScenarioShareCompatibility
    {
        public int ScenarioSchemaVersion;
        public int MechanicStateSchemaVersion;
        public string GameVersionId;
        public string RulesetId;
        public int RulesetRevision;
        public string ContentSnapshotId;
        public string ContentFingerprint;
        public string Status = ScenarioShareCompatibilityStatuses.Compatible;
        public List<ScenarioShareDiagnostic> Diagnostics = new List<ScenarioShareDiagnostic>();
    }

    [Serializable]
    public sealed class ScenarioShareContent
    {
        public List<string> AllowedActions = new List<string>();
        public ScenarioShareObjectives Objectives = new ScenarioShareObjectives();
        public List<ScenarioShareStep> Steps = new List<ScenarioShareStep>();
        public List<string> Hints = new List<string>();
        public List<ScenarioShareDiscoveryRule> DiscoveryRules = new List<ScenarioShareDiscoveryRule>();
        public ScenarioShareUndoPolicy Undo = new ScenarioShareUndoPolicy();
        public TestScenarioDefinition State = new TestScenarioDefinition();
    }

    [Serializable]
    public sealed class ScenarioShareObjectives
    {
        public bool RequireFinalComposition;
        public bool RequireCombatWin;
        public List<string> PostWinChoices = new List<string>();
    }

    [Serializable]
    public sealed class ScenarioShareStep
    {
        public int Order;
        public string ActionId;
        public string Kind;
        public int Count;
        public string Instruction;
        public string SourcePlacementId;
        public List<string> SourcePlacementIds = new List<string>();
        public string TargetPlacementId;
        public string ChoiceId;
    }

    [Serializable]
    public sealed class ScenarioShareDiscoveryRule
    {
        public string ScheduleId;
        public string Source;
        public string Policy;
        public string Label;
        public string CardKind;
        public int TavernTier;
        public int OptionCount;
        public List<string> TargetCardIds = new List<string>();
        public string RequiredTribe;
        public int MinimumRequiredTribeMinions;
    }

    [Serializable]
    public sealed class ScenarioShareUndoPolicy
    {
        public int UsesPerRun;
        public bool RestoreRng;
        public bool LockAfterTurnEnd;
        public bool LockAfterCombat;
        public bool LockDuringFreeExplore;
    }

    [Serializable]
    public sealed class ScenarioShareHandoff
    {
        public string WebPlayUrl;
        public string ShareUrl;
        public string WindowsDownloadUrl;
    }

    [Serializable]
    public sealed class ScenarioShareDiagnostic
    {
        public string Code;
        public string Level;
        public string Message;
    }

    public sealed class ScenarioShareRuntimeIdentity
    {
        public int SupportedContractSchemaVersion;
        public int SupportedScenarioSchemaVersion;
        public int SupportedMechanicStateSchemaVersion;
        public string GameVersionId;
        public string RulesetId;
        public int RulesetRevision;
        public string ContentSnapshotId;
        public string ContentFingerprint;
    }

    public sealed class ScenarioShareCompatibilityResult
    {
        public string Status;
        public List<ScenarioShareDiagnostic> Diagnostics = new List<ScenarioShareDiagnostic>();

        public bool CanOpen => Status != ScenarioShareCompatibilityStatuses.Rejected;
    }

    public static class ScenarioShareContractVersions
    {
        public const int Current = 1;
    }

    public static class ScenarioSharePublicationStatuses
    {
        public const string Published = "Published";
    }

    public static class ScenarioShareCompatibilityStatuses
    {
        public const string Compatible = "Compatible";
        public const string CompatibleWithWarnings = "CompatibleWithWarnings";
        public const string Rejected = "Rejected";
    }

    public static class ScenarioShareDiagnosticCodes
    {
        public const string ScenarioStateMissing = "scenario.state.missing";
        public const string ContractSchemaMismatch = "contract.schema.mismatch";
        public const string ScenarioSchemaMismatch = "scenario.schema.mismatch";
        public const string MechanicStateSchemaMismatch = "scenario.mechanic-schema.mismatch";
        public const string GameVersionMismatch = "game-version.mismatch";
        public const string RulesetMismatch = "ruleset.mismatch";
        public const string RulesetRevisionMismatch = "ruleset.revision-mismatch";
        public const string ContentSnapshotMismatch = "content.snapshot-mismatch";
        public const string ContentFingerprintMismatch = "content.fingerprint-mismatch";
    }

    public static class ScenarioShareDiagnosticLevels
    {
        public const string Warning = "Warning";
        public const string Error = "Error";
    }
}
