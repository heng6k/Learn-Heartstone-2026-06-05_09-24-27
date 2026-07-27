using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum QuestObjectiveKind
    {
        BuyCards,
        BuyMinions,
        BuyTavernSpells,
        AddCardsToHand,
        SellMinions,
        SpendGold,
        RefreshShop,
        CastSpells,
        CastTavernSpells,
        PlayBattlecryMinions
    }

    public enum QuestRewardTrigger
    {
        OnComplete,
        TurnStarted,
        TurnEnded,
        CardBought,
        CardPlayed,
        MinionPlayed,
        MinionSold,
        ShopRefreshed,
        StartOfCombat,
        AfterCombat,
        CombatMinionSummoned,
        CombatFriendlyMinionDied,
        CombatAfterAttack,
        SpellcraftGenerated,
        DiscoverChosen
    }

    public enum QuestRewardEffectKind
    {
        None,
        GrantGold,
        GrantGoldAndMaxGold,
        GainGoldEachTurn,
        BuffBoughtMinionAndImprove,
        BuffRandomShopMinion,
        BuffTierThreeOrLowerMinions,
        RightmostStealthAndHealth,
        FirstBattlecryExtraTriggers,
        FriendlyMinionsAttackAura,
        BuffRandomHandMinion,
        StartCombatBoardBuff,
        AlternatingShopTierBuff,
        EndTurnMenagerieBoardBuff,
        VolatileVenomAura,
        RightmostMissingHealthAttack,
        BuffOtherSameTierMinions,
        AddEnhancedPartToHand,
        AvengeDamageHighestHealthEnemy,
        BuffNonTauntByTauntCount,
        PlayMissingTypeMenagerieBuff,
        ImproveBloodGemsAndAddGems,
        CombatSummonBuffAndImprove,
        LeftmostDivineShieldImmediateAttack,
        AvengeGainFreeRefresh,
        AddRushingWindsSpellcraft,
        SellMinionStatsToShop,
        BuyMinionStatsToFriendly,
        FirstBuyEachTurnCopy,
        GainRandomTavernSpells,
        TavernSpellCostDiscount,
        SetTavernMinionCost,
        CopyExpensiveBoughtTavernSpell,
        GoldenHighestTierShopMinion,
        TriggerBattlecriesAtEndOfTurn,
        RefreshCountShopBuffAura,
        EdgeMinionsConsumeShop,
        GoldenHighestTierShopAfterRefreshes,
        GainLastDeadFriendlyPlainCopyAfterCombat,
        MakeEdgeMinionsGoldenForCombat,
        SummonHighestHealthCopyAtCombatStart,
        PermanentBuffDeathrattleMinionAfterDeath,
        AvengeGainRandomTavernSpell,
        AvengeSummonAmalgam,
        ExtraDeathrattleTriggers,
        ExtraDiscoverCopy,
        GainGoldenBuddy,
        DiscoverOpponentWarbandMinionAfterCombat,
        DiscoverBuddyEachTurn,
        DelayedLesserTrinketChoice,
        DelayedGreaterTrinketChoice,
        DiscoverSecondHeroPower,
        ExtraEndOfTurnTriggers,
        GainRandomPlaceholder92,
        SpinYoggWheel,
        DiscoverCurrentTierMinion,
        WisdomballHelpfulRefreshes,
        TwoCopiesTripleRule,
        GainTransformingZerus,
        ChooseNewRewardsEachTurn,
        AddKidnapSackSpellcraft,
        AddGoldenHammerSpellcraft,
        GainTierUpTransformSpells,
        ExtraBattlecryTriggers,
        GuidancePlaceholder92ShopSlots,
        ExtraTavernSpellCast,
        GoldenFriendlyTierAndImprove,
        GainTierSevenCopy,
        CastRandomTavernSpells,
        UnlockTierSevenAndAutoUpgrade,
        MagicfinRelic,
        ScalingTavernSpellBonus,
        ExtraRallyTriggers
    }

    public enum QuestRewardPowerLevel
    {
        Pending = 0,
        Weak = 1,
        Medium = 2,
        Strong = 3,
        Premium = 4
    }

    public enum QuestOfferPoolStatus
    {
        Offerable,
        HiddenEffectOnly,
        DebugOnly,
        Disabled
    }

    public enum QuestImplementationStatus
    {
        Implemented,
        FrameworkFirst,
        Planned,
        Deferred,
        Unsupported,
        Unregistered
    }

    public sealed class QuestObjectiveDefinition
    {
        public QuestObjectiveKind Kind;
        public int RequiredAmount;
        public string RequiredTag;
    }

    public sealed class QuestDefinition
    {
        public string Id;
        public string CardId;
        public int DbfId;
        public string Name;
        public string Text;
        public string ImagePath;
        public string ImageUrl;
        public QuestObjectiveDefinition Objective = new QuestObjectiveDefinition();
        public string DefaultRewardId;
        public List<string> Tags = new List<string>();
        public QuestImplementationStatus ImplementationStatus;
        public string Notes;
    }

    public sealed class QuestRewardDefinition
    {
        public string Id;
        public string CardId;
        public int DbfId;
        public string Name;
        public string Text;
        public string ImagePath;
        public string ImageUrl;
        public QuestRewardTrigger Trigger;
        public QuestRewardEffectKind EffectKind;
        public int GoldAmount;
        public int MaxGoldAmount;
        public int AttackBonus;
        public int HealthBonus;
        public int TargetCount;
        public bool Improves;
        public QuestRewardPowerLevel PowerLevel = QuestRewardPowerLevel.Medium;
        public QuestOfferPoolStatus OfferPoolStatus = QuestOfferPoolStatus.Offerable;
        public List<string> Tags = new List<string>();
        public QuestImplementationStatus ImplementationStatus;
        public string Notes;
    }

    [Serializable]
    public sealed class QuestDifficultyTierMultiplier
    {
        public int Tier;
        public double Multiplier;
    }

    [Serializable]
    public sealed class QuestArmorDifficultyModifier
    {
        public int MinArmor;
        public int MaxArmor;
        public int Modifier;
    }

    [Serializable]
    public sealed class QuestHeroDifficultyOverride
    {
        public string HeroCardId;
        public int Modifier;
    }

    [Serializable]
    public sealed class QuestDifficultyProfile
    {
        public List<QuestDifficultyTierMultiplier> TierMultipliers = new List<QuestDifficultyTierMultiplier>();
        public List<QuestArmorDifficultyModifier> ArmorModifiers = new List<QuestArmorDifficultyModifier>();
        public List<QuestHeroDifficultyOverride> HeroOverrides = new List<QuestHeroDifficultyOverride>();
        public int DefaultModifier;
        public int HighHealthThreshold = 60;
        public int HighHealthModifier = -2;

        public static QuestDifficultyProfile CreateDefault(params string[] highHealthEquivalentHeroIds)
        {
            var profile = new QuestDifficultyProfile
            {
                DefaultModifier = 0,
                HighHealthThreshold = 60,
                HighHealthModifier = -2,
                TierMultipliers = new List<QuestDifficultyTierMultiplier>
                {
                    new QuestDifficultyTierMultiplier { Tier = 1, Multiplier = 0.75 },
                    new QuestDifficultyTierMultiplier { Tier = 2, Multiplier = 1.0 },
                    new QuestDifficultyTierMultiplier { Tier = 3, Multiplier = 1.25 },
                    new QuestDifficultyTierMultiplier { Tier = 4, Multiplier = 1.5 }
                },
                ArmorModifiers = new List<QuestArmorDifficultyModifier>
                {
                    new QuestArmorDifficultyModifier { MinArmor = 0, MaxArmor = 5, Modifier = 1 },
                    new QuestArmorDifficultyModifier { MinArmor = 6, MaxArmor = 10, Modifier = 0 },
                    new QuestArmorDifficultyModifier { MinArmor = 11, MaxArmor = 15, Modifier = -1 },
                    new QuestArmorDifficultyModifier { MinArmor = 16, MaxArmor = int.MaxValue, Modifier = -2 }
                }
            };

            foreach (var heroId in highHealthEquivalentHeroIds ?? new string[0])
            {
                if (!string.IsNullOrWhiteSpace(heroId))
                {
                    profile.HeroOverrides.Add(new QuestHeroDifficultyOverride
                    {
                        HeroCardId = heroId,
                        Modifier = profile.HighHealthModifier
                    });
                }
            }

            return profile;
        }

        public int ResolveTier(QuestRewardPowerLevel powerLevel, int armor, int maxHealth, string heroCardId)
        {
            var baseTier = ClampTier((int)powerLevel);
            return ClampTier(baseTier + ResolveModifier(armor, maxHealth, heroCardId));
        }

        public int ResolveModifier(int armor, int maxHealth, string heroCardId)
        {
            var overrideEntry = HeroOverrides.Find(entry =>
                entry != null &&
                !string.IsNullOrEmpty(entry.HeroCardId) &&
                string.Equals(entry.HeroCardId, heroCardId, StringComparison.OrdinalIgnoreCase));
            if (overrideEntry != null)
            {
                return overrideEntry.Modifier;
            }

            if (maxHealth >= HighHealthThreshold)
            {
                return HighHealthModifier;
            }

            var resolvedArmor = Math.Max(0, armor);
            foreach (var band in ArmorModifiers)
            {
                if (band != null && resolvedArmor >= band.MinArmor && resolvedArmor <= band.MaxArmor)
                {
                    return band.Modifier;
                }
            }

            return DefaultModifier;
        }

        public int ResolveRequiredAmount(int baseAmount, int difficultyTier)
        {
            var multiplier = ResolveMultiplier(difficultyTier);
            return Math.Max(1, (int)Math.Ceiling(Math.Max(1, baseAmount) * multiplier));
        }

        public double ResolveMultiplier(int difficultyTier)
        {
            var tier = ClampTier(difficultyTier);
            var entry = TierMultipliers.Find(item => item != null && item.Tier == tier);
            return entry == null || entry.Multiplier <= 0 ? 1.0 : entry.Multiplier;
        }

        private static int ClampTier(int tier)
        {
            return Math.Max(1, Math.Min(4, tier));
        }
    }

    [Serializable]
    public sealed class ActiveQuestState
    {
        public string QuestId;
        public string QuestCardId;
        public string QuestName;
        public string QuestText;
        public string QuestImagePath;
        public string RewardId;
        public string RewardCardId;
        public string RewardName;
        public string RewardText;
        public string RewardImagePath;
        public string Source;
        public int Progress;
        public int BaseRequiredAmount;
        public int RequiredAmount;
        public int DifficultyTier;
        public int DifficultyModifier;
        public QuestRewardPowerLevel RewardPowerLevel;
        public bool Completed;
        public bool RewardActive;
        public QuestImplementationStatus ImplementationStatus;

        public ActiveQuestState Clone()
        {
            return (ActiveQuestState)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class PlayerQuestState
    {
        public ActiveQuestState MainQuest;
        public ActiveQuestState BonusQuest;
        public List<ActiveQuestState> Completed = new List<ActiveQuestState>();
        public int HiddenTreasureVaultGold = 1;
        public int CookedBookAttack = 2;
        public int CookedBookHealth = 2;
        public Dictionary<string, int> RewardCounters = new Dictionary<string, int>();
        public Dictionary<string, bool> RewardFlags = new Dictionary<string, bool>();

        public PlayerQuestState Clone()
        {
            var clone = (PlayerQuestState)MemberwiseClone();
            clone.MainQuest = MainQuest?.Clone();
            clone.BonusQuest = BonusQuest?.Clone();
            clone.Completed = (Completed ?? new List<ActiveQuestState>())
                .ConvertAll(quest => quest?.Clone());
            clone.RewardCounters = new Dictionary<string, int>(RewardCounters ?? new Dictionary<string, int>());
            clone.RewardFlags = new Dictionary<string, bool>(RewardFlags ?? new Dictionary<string, bool>());
            return clone;
        }
    }
}
