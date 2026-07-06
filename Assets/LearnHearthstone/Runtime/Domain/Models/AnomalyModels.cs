using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum AnomalyPoolVersion
    {
        CurrentHsReplay,
        Season5Launch,
        Season5AllBg27,
        AllKnown
    }

    public enum AnomalyImplementationStatus
    {
        Implemented,
        OfferableWithExactProxy,
        FrameworkOnly,
        Planned,
        BlockedByDependency,
        DebugOnly,
        Unsupported
    }

    public enum AnomalyAvailabilityReason
    {
        None,
        RequiresBuddyMode,
        RequiresDarkmoonPrizeBackend,
        RequiresSecondHeroPowerUi,
        RequiresTier7Pool,
        RequiresTimewarpPool,
        RequiresSharedLobbyChoice,
        RequiresYoggWheel,
        RequiresDuos,
        RequiresCombatRewrite,
        RequiresOfficialDataReview
    }

    public enum AnomalyEffectFamily
    {
        Unknown,
        Economy,
        TavernRefresh,
        MinionPool,
        Buddy,
        DarkmoonPrize,
        SecondHeroPower,
        Timewarp,
        GeneratedSpell,
        GeneratedMinion,
        DelayedReward,
        TripleRule,
        CombatRule,
        SharedLobbyChoice,
        SinglePlayerChoice,
        HeroReplacement
    }

    [Serializable]
    public sealed class AnomalyDefinition
    {
        public string Id;
        public string CardId;
        public int DbfId;
        public string Name;
        public string Text;
        public List<AnomalyPoolVersion> SourcePools = new List<AnomalyPoolVersion>();
        public AnomalyEffectFamily EffectFamily;
        public AnomalyImplementationStatus ImplementationStatus;
        public List<AnomalyAvailabilityReason> AvailabilityReasons = new List<AnomalyAvailabilityReason>();
        public List<string> Tags = new List<string>();
        public List<string> SourceUrls = new List<string>();
        public string SnapshotDate;
        public string Notes;
    }

    [Serializable]
    public sealed class AnomalyState
    {
        public bool Enabled;
        public string ActiveAnomalyId;
        public string ActiveCardId;
        public string ActiveName;
        public string ActiveText;
        public AnomalyPoolVersion PoolVersion;
        public AnomalyImplementationStatus ImplementationStatus;
        public List<AnomalyAvailabilityReason> AvailabilityReasons = new List<AnomalyAvailabilityReason>();
        public Dictionary<string, int> Counters = new Dictionary<string, int>();
        public Dictionary<string, string> Flags = new Dictionary<string, string>();
        public List<string> AppliedPoolModifiers = new List<string>();
        public List<string> BlockedHeroIds = new List<string>();
        public List<string> BlockedTribes = new List<string>();
    }
}
