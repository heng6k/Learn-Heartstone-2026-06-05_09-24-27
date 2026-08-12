using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum DarkGiftImplementationStatus
    {
        BlockedByOfficialFact,
        Planned,
        FrameworkOnly,
        Implemented,
        Verified
    }

    public enum DarkGiftOfficialFactStatus
    {
        BlockedByOfficialFact,
        Confirmed
    }

    [Serializable]
    public sealed class DarkGiftDefinition
    {
        public string Id;
        public string CardId;
        public int DbfId;
        public string ResearchKey;
        public string RevisionId;
        public string EffectRevision;
        public string SourceLevel;
        public string DisplayName;
        public string Text;
        public string ImagePath;
        public string ImageSource;
        public int EarliestOfferRound;
        public int LatestOfferRound;
        public List<string> AvailabilityTags = new List<string>();
        public List<string> CompatibilityTags = new List<string>();
        public List<string> RequiredMinionTags = new List<string>();
        public List<string> ExcludedMinionTags = new List<string>();
        public string TriggerSpec;
        public int TriggerDelayRounds;
        public string ChoiceSpec;
        public string StackPolicy;
        public int MaxStacks = 1;
        public string DurationPolicy;
        public int DurationRounds;
        public int InitialUses;
        public int CooldownRounds;
        public List<string> EffectIds = new List<string>();
        public DarkGiftImplementationStatus ImplementationStatus = DarkGiftImplementationStatus.BlockedByOfficialFact;

        public DarkGiftDefinition Clone()
        {
            var clone = (DarkGiftDefinition)MemberwiseClone();
            clone.AvailabilityTags = new List<string>(AvailabilityTags ?? new List<string>());
            clone.CompatibilityTags = new List<string>(CompatibilityTags ?? new List<string>());
            clone.RequiredMinionTags = new List<string>(RequiredMinionTags ?? new List<string>());
            clone.ExcludedMinionTags = new List<string>(ExcludedMinionTags ?? new List<string>());
            clone.EffectIds = new List<string>(EffectIds ?? new List<string>());
            return clone;
        }
    }

    [Serializable]
    public sealed class DarkGiftTierRangeRule
    {
        public int FromRound;
        public int MinTier;
        public int MaxTier;

        public DarkGiftTierRangeRule Clone()
        {
            return (DarkGiftTierRangeRule)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class DarkGiftCandidateFilter
    {
        public int BattlecryAllowedFromRound;
        public int ChooseOneAllowedFromRound;
        public List<string> RequiredTags = new List<string>();
        public List<string> ExcludedTags = new List<string>();
        public List<string> ExcludedMechanics = new List<string>();

        public DarkGiftCandidateFilter Clone()
        {
            var clone = (DarkGiftCandidateFilter)MemberwiseClone();
            clone.RequiredTags = new List<string>(RequiredTags ?? new List<string>());
            clone.ExcludedTags = new List<string>(ExcludedTags ?? new List<string>());
            clone.ExcludedMechanics = new List<string>(ExcludedMechanics ?? new List<string>());
            return clone;
        }
    }

    [Serializable]
    public sealed class DarkGiftCommonTribeGuarantee
    {
        public bool Enabled;
        public int StartRound;
        public int MinimumOfferCount;

        public DarkGiftCommonTribeGuarantee Clone()
        {
            return (DarkGiftCommonTribeGuarantee)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class DarkGiftProfile
    {
        public string Id;
        public bool Enabled;
        public int NormalEntryStartRound;
        public int GoldCost;
        public int UsesPerTurn;
        public int UsesPerGame;
        public int OfferCount;
        public int PickCount;
        public List<DarkGiftTierRangeRule> TierRanges = new List<DarkGiftTierRangeRule>();
        public DarkGiftCandidateFilter CandidateFilter = new DarkGiftCandidateFilter();
        public string DeduplicationPolicy;
        public DarkGiftCommonTribeGuarantee CommonTribeGuarantee = new DarkGiftCommonTribeGuarantee();
        public int ChoiceQueuePriority;
        public DarkGiftOfficialFactStatus ChoiceQueuePriorityFactStatus = DarkGiftOfficialFactStatus.BlockedByOfficialFact;
        public DarkGiftAutoChoicePolicy AutoChoicePolicy = DarkGiftAutoChoicePolicy.PlayerChoice;
        public DarkGiftImplementationStatus ImplementationStatus = DarkGiftImplementationStatus.BlockedByOfficialFact;

        public DarkGiftProfile Clone()
        {
            var clone = (DarkGiftProfile)MemberwiseClone();
            clone.TierRanges = (TierRanges ?? new List<DarkGiftTierRangeRule>())
                .ConvertAll(rule => rule?.Clone());
            clone.CandidateFilter = CandidateFilter?.Clone() ?? new DarkGiftCandidateFilter();
            clone.CommonTribeGuarantee = CommonTribeGuarantee?.Clone() ?? new DarkGiftCommonTribeGuarantee();
            return clone;
        }
    }

    public static class DarkGiftProfiles
    {
        public const string Season14PreviewId = "dark-gift-36.2-preview-v1";

        public static DarkGiftProfile CreateSeason14Preview()
        {
            return new DarkGiftProfile
            {
                Id = Season14PreviewId,
                Enabled = true,
                NormalEntryStartRound = 3,
                GoldCost = 3,
                UsesPerTurn = 1,
                UsesPerGame = 3,
                OfferCount = 3,
                PickCount = 1,
                TierRanges = new List<DarkGiftTierRangeRule>
                {
                    new DarkGiftTierRangeRule { FromRound = 3, MinTier = 2, MaxTier = 2 },
                    new DarkGiftTierRangeRule { FromRound = 4, MinTier = 2, MaxTier = 3 },
                    new DarkGiftTierRangeRule { FromRound = 5, MinTier = 3, MaxTier = 3 },
                    new DarkGiftTierRangeRule { FromRound = 6, MinTier = 3, MaxTier = 4 },
                    new DarkGiftTierRangeRule { FromRound = 7, MinTier = 4, MaxTier = 4 },
                    new DarkGiftTierRangeRule { FromRound = 8, MinTier = 4, MaxTier = 5 },
                    new DarkGiftTierRangeRule { FromRound = 9, MinTier = 4, MaxTier = 6 },
                    new DarkGiftTierRangeRule { FromRound = 10, MinTier = 5, MaxTier = 6 },
                    new DarkGiftTierRangeRule { FromRound = 11, MinTier = 5, MaxTier = 6 },
                    new DarkGiftTierRangeRule { FromRound = 12, MinTier = 6, MaxTier = 6 }
                },
                CandidateFilter = new DarkGiftCandidateFilter
                {
                    BattlecryAllowedFromRound = 5,
                    ChooseOneAllowedFromRound = 5,
                    ExcludedMechanics = new List<string> { "magnetic", "sell-trigger", "hand-only" }
                },
                DeduplicationPolicy = "distinct-gift-definitions-per-offer",
                CommonTribeGuarantee = new DarkGiftCommonTribeGuarantee
                {
                    Enabled = true,
                    StartRound = 3,
                    MinimumOfferCount = 1
                },
                ChoiceQueuePriority = 0,
                ChoiceQueuePriorityFactStatus = DarkGiftOfficialFactStatus.BlockedByOfficialFact,
                ImplementationStatus = DarkGiftImplementationStatus.Implemented
            };
        }
    }

    public enum DarkGiftAutoChoicePolicy
    {
        PlayerChoice,
        FirstOption
    }

    public static class DarkGiftStackPolicies
    {
        public const string Reject = "reject";
        public const string Stack = "stack";
        public const string Replace = "replace";
    }

    public static class DarkGiftDurationPolicies
    {
        public const string Persistent = "persistent";
        public const string Uses = "uses";
        public const string Rounds = "rounds";
    }

    public enum DarkGiftOfferSourceKind
    {
        NormalButton,
        HeroPower,
        Trinket,
        Card,
        Debug
    }

    [Serializable]
    public sealed class DarkGiftTribeCount
    {
        public Tribe Tribe;
        public int Count;

        public DarkGiftTribeCount Clone()
        {
            return (DarkGiftTribeCount)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class DarkGiftOfferRequest
    {
        public DarkGiftOfferSourceKind SourceKind;
        public string SourceId;
        public int Round;
        public int RequestedTier;
        public int MinTier;
        public int MaxTier;
        public int OfferCount;
        public int PickCount;
        public int PlayerTavernTier;
        public int BattlecriesTriggeredThisGame;
        public int DeathrattlesTriggeredThisGame;
        public int TavernSpellsCastThisGame;
        public List<Tribe> ActiveTribes = new List<Tribe>();
        public List<DarkGiftTribeCount> CurrentBoardTribeCounts = new List<DarkGiftTribeCount>();
        public string GiftPoolProfileId;
        public bool IgnoreNormalRoundRestrictions;
        public int Seed;
        public int RngCursor;
    }

    [Serializable]
    public sealed class DarkGiftOfferOption
    {
        public string OptionId;
        public string MinionDefinitionId;
        public string MinionCardId;
        public string MinionRevisionId;
        public string MinionName;
        public string MinionText;
        public string MinionImagePath;
        public int MinionTier;
        public int MinionAttack;
        public int MinionHealth;
        public List<Tribe> MinionTribes = new List<Tribe>();
        public string GiftId;
        public string GiftRevisionId;
        public string GiftName;
        public string GiftText;
        public string GiftImagePath;

        public DarkGiftOfferOption Clone()
        {
            var clone = (DarkGiftOfferOption)MemberwiseClone();
            clone.MinionTribes = new List<Tribe>(MinionTribes ?? new List<Tribe>());
            return clone;
        }
    }

    [Serializable]
    public sealed class DarkGiftOfferResult
    {
        public bool Succeeded;
        public string Code;
        public string Message;
        public DarkGiftOfferSourceKind SourceKind;
        public string SourceId;
        public string GiftPoolProfileId;
        public int PickCount;
        public int NextRngCursor;
        public List<DarkGiftOfferOption> Options = new List<DarkGiftOfferOption>();
    }

    [Serializable]
    public sealed class DarkGiftTriggerRequest
    {
        public string TargetInstanceId;
        public string DefinitionRevisionId;
        public MechanicEventType EventType;
        public string RequestId;

        public DarkGiftTriggerRequest Clone()
        {
            return (DarkGiftTriggerRequest)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class DarkGiftStateMachineResult
    {
        public bool Succeeded;
        public string Code;
        public string Message;
        public PlayerDarkGiftInstance Instance;
    }

    [Serializable]
    public sealed class PlayerDarkGiftInstance
    {
        public string InstanceId;
        public string DefinitionRevisionId;
        public int AcquiredRound;
        public string Source;
        public int StackCount;
        public int RemainingUses;
        public int Cooldown;
        public int NextTriggerRound;
        public bool Active;
        public bool Suppressed;
        public bool Expired;

        public PlayerDarkGiftInstance Clone()
        {
            return (PlayerDarkGiftInstance)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class DarkGiftTriggerHistory
    {
        public List<MechanicEventRecord> Events = new List<MechanicEventRecord>();

        public DarkGiftTriggerHistory Clone()
        {
            return new DarkGiftTriggerHistory
            {
                Events = (Events ?? new List<MechanicEventRecord>()).ConvertAll(item => item?.Clone())
            };
        }
    }

    [Serializable]
    public sealed class PlayerDarkGiftState
    {
        public List<PlayerDarkGiftInstance> AcquiredGiftInstances = new List<PlayerDarkGiftInstance>();
        public Dictionary<string, int> Counters = new Dictionary<string, int>();
        public Dictionary<string, int> Cooldowns = new Dictionary<string, int>();
        public DarkGiftTriggerHistory TriggerHistory = new DarkGiftTriggerHistory();

        public PlayerDarkGiftState Clone()
        {
            return new PlayerDarkGiftState
            {
                AcquiredGiftInstances = (AcquiredGiftInstances ?? new List<PlayerDarkGiftInstance>())
                    .ConvertAll(item => item?.Clone()),
                Counters = new Dictionary<string, int>(Counters ?? new Dictionary<string, int>()),
                Cooldowns = new Dictionary<string, int>(Cooldowns ?? new Dictionary<string, int>()),
                TriggerHistory = TriggerHistory?.Clone() ?? new DarkGiftTriggerHistory()
            };
        }
    }
}
