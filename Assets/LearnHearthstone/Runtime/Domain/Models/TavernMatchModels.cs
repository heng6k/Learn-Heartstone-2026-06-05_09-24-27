using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    [Serializable]
    public sealed class MatchSetupOptions
    {
        public List<Tribe> ActiveTribes = new List<Tribe>();
        public string SelectedHeroCardId;
        public string CardPoolVersionId;
        public string CardPoolVersionName;
        public bool IsDefaultCardPoolVersion = true;
        public AdvancedMechanicMode AdvancedMechanicMode = AdvancedMechanicMode.None;
        public List<string> EnabledMinionCardIds = new List<string>();
        public List<string> EnabledTavernSpellCardNumbers = new List<string>();
    }

    [Serializable]
    public sealed class SearchTarget
    {
        public string DefinitionId;
        public string Priority = "MEDIUM";
        public int DesiredCopies = 1;
    }

    [Serializable]
    public sealed class SearchPlanState
    {
        public List<SearchTarget> Targets = new List<SearchTarget>();
        public int GoldSpentOnRerollThisTurn;
        public List<string> HitsThisTurn = new List<string>();
    }

    [Serializable]
    public sealed class RecruitLogEntry
    {
        public int Seq;
        public int Round;
        public RecruitLogType Type;
        public string Message;
        public int GoldBefore;
        public int GoldAfter;
    }

    [Serializable]
    public sealed class DiscoverState
    {
        public string Source = "TRIPLE";
        public int RewardTier;
        public string TargetInstanceId;
        public int RemainingPicks;
        public List<MinionInstance> Options = new List<MinionInstance>();
    }

    [Serializable]
    public sealed class TavernGrowthState
    {
        public List<TavernGrowthModifier> ShopModifiers = new List<TavernGrowthModifier>();
        public List<GeneratedCardBuffState> GeneratedCardBuffs = new List<GeneratedCardBuffState>();
    }

    [Serializable]
    public sealed class TavernShopSlotState
    {
        public string SlotId;
        public bool Frozen;
        public string CardInstanceId;
    }

    public static class TavernShopSlots
    {
        public static void Ensure(TavernState tavern)
        {
            if (tavern == null)
            {
                return;
            }

            if (tavern.Shop == null)
            {
                tavern.Shop = new List<MinionInstance>();
            }

            if (tavern.ShopSlots == null)
            {
                tavern.ShopSlots = new List<TavernShopSlotState>();
            }

            var legacyWholeShopFreeze = tavern.Frozen && !HasAnyFrozenSlot(tavern);
            while (tavern.ShopSlots.Count < tavern.Shop.Count)
            {
                tavern.ShopSlots.Add(new TavernShopSlotState { SlotId = "shop-slot-" + tavern.ShopSlots.Count });
            }

            while (tavern.ShopSlots.Count > tavern.Shop.Count)
            {
                tavern.ShopSlots.RemoveAt(tavern.ShopSlots.Count - 1);
            }

            for (var index = 0; index < tavern.ShopSlots.Count; index += 1)
            {
                var slot = tavern.ShopSlots[index];
                if (slot == null)
                {
                    slot = new TavernShopSlotState();
                    tavern.ShopSlots[index] = slot;
                }

                if (string.IsNullOrEmpty(slot.SlotId))
                {
                    slot.SlotId = "shop-slot-" + index;
                }

                var card = tavern.Shop[index];
                if (card == null)
                {
                    slot.CardInstanceId = null;
                    slot.Frozen = false;
                    continue;
                }

                if (!string.Equals(slot.CardInstanceId, card.InstanceId, StringComparison.Ordinal))
                {
                    slot.CardInstanceId = card.InstanceId;
                    slot.Frozen = HasFrozenTag(card);
                }

                if (legacyWholeShopFreeze)
                {
                    slot.Frozen = true;
                }
            }

            SyncFrozenFlag(tavern);
        }

        public static void ReplaceShop(TavernState tavern, List<MinionInstance> shop)
        {
            if (tavern == null)
            {
                return;
            }

            tavern.Shop = shop ?? new List<MinionInstance>();
            tavern.ShopSlots = new List<TavernShopSlotState>();
            tavern.Frozen = false;
            Ensure(tavern);
        }

        public static bool HasAnyFrozenSlot(TavernState tavern)
        {
            if (tavern?.ShopSlots == null)
            {
                return false;
            }

            for (var index = 0; index < tavern.ShopSlots.Count; index += 1)
            {
                if (tavern.ShopSlots[index]?.Frozen == true)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSlotFrozen(TavernState tavern, int index)
        {
            Ensure(tavern);
            return tavern != null &&
                   index >= 0 &&
                   index < tavern.ShopSlots.Count &&
                   tavern.ShopSlots[index]?.Frozen == true &&
                   index < tavern.Shop.Count &&
                   tavern.Shop[index] != null;
        }

        public static void SetSlotFrozen(TavernState tavern, int index, bool frozen)
        {
            Ensure(tavern);
            if (tavern == null || index < 0 || index >= tavern.ShopSlots.Count || index >= tavern.Shop.Count)
            {
                return;
            }

            var card = tavern.Shop[index];
            var slot = tavern.ShopSlots[index];
            slot.Frozen = frozen && card != null;
            slot.CardInstanceId = card?.InstanceId;
            SyncFrozenFlag(tavern);
        }

        public static void SetAllFrozen(TavernState tavern, bool frozen)
        {
            Ensure(tavern);
            if (tavern == null)
            {
                return;
            }

            for (var index = 0; index < tavern.ShopSlots.Count; index += 1)
            {
                var card = index < tavern.Shop.Count ? tavern.Shop[index] : null;
                tavern.ShopSlots[index].Frozen = frozen && card != null;
                tavern.ShopSlots[index].CardInstanceId = card?.InstanceId;
            }

            SyncFrozenFlag(tavern);
        }

        public static void ClearSlot(TavernState tavern, int index)
        {
            Ensure(tavern);
            if (tavern == null || index < 0 || index >= tavern.ShopSlots.Count)
            {
                return;
            }

            tavern.ShopSlots[index].Frozen = false;
            tavern.ShopSlots[index].CardInstanceId = null;
            SyncFrozenFlag(tavern);
        }

        public static List<int> FrozenIndexes(TavernState tavern)
        {
            Ensure(tavern);
            var indexes = new List<int>();
            if (tavern == null)
            {
                return indexes;
            }

            for (var index = 0; index < tavern.ShopSlots.Count && index < tavern.Shop.Count; index += 1)
            {
                if (tavern.ShopSlots[index].Frozen && tavern.Shop[index] != null)
                {
                    indexes.Add(index);
                }
            }

            return indexes;
        }

        public static List<MinionInstance> FrozenCards(TavernState tavern)
        {
            Ensure(tavern);
            var cards = new List<MinionInstance>();
            if (tavern == null)
            {
                return cards;
            }

            for (var index = 0; index < tavern.ShopSlots.Count && index < tavern.Shop.Count; index += 1)
            {
                if (tavern.ShopSlots[index].Frozen && tavern.Shop[index] != null)
                {
                    cards.Add(tavern.Shop[index]);
                }
            }

            return cards;
        }

        public static void SyncFrozenFlag(TavernState tavern)
        {
            if (tavern == null)
            {
                return;
            }

            var anyFrozen = false;
            if (tavern.ShopSlots != null && tavern.Shop != null)
            {
                for (var index = 0; index < tavern.ShopSlots.Count && index < tavern.Shop.Count; index += 1)
                {
                    if (tavern.Shop[index] == null)
                    {
                        tavern.ShopSlots[index].Frozen = false;
                        tavern.ShopSlots[index].CardInstanceId = null;
                        continue;
                    }

                    anyFrozen |= tavern.ShopSlots[index].Frozen;
                }
            }

            tavern.Frozen = anyFrozen;
        }

        private static bool HasFrozenTag(MinionInstance card)
        {
            if (card?.Tags == null)
            {
                return false;
            }

            foreach (var tag in card.Tags)
            {
                if (!string.IsNullOrEmpty(tag) &&
                    tag.IndexOf("frozen", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class TavernState
    {
        public int Tier;
        public int Gold;
        public int MaxGold;
        public int UpgradeCost;
        public bool Frozen;
        public int NextTurnBonusGold;
        public int PendingCombatWinGold;
        public int PendingCombatDrawGold;
        public int NextCombatBoardAttack;
        public int NextCombatBoardHealth;
        public int NextTavernSpellCostReduction;
        public int FreeRefreshes;
        public int DemonFodderRefreshes;
        public int TavernSpellBonusAttack;
        public int TavernSpellBonusHealth;
        public int TavernSpellsCastThisTurn;
        public int TavernSpellsCastThisGame;
        public int CardsPlayedThisTurn;
        public string LastTavernSpellCardId;
        public int GoldSpentThisTurn;
        public int GoldSpentThisGame;
        public int BloodGemBonusAttack;
        public int BloodGemBonusHealth;
        public int DeepBlueBonusAttack;
        public int DeepBlueBonusHealth;
        public int HealthCostRefreshes;
        public int RefreshBuffAttack;
        public int RefreshBuffHealth;
        public int RefreshRightmostBuffAttack;
        public int RefreshRightmostBuffHealth;
        public int TemporaryAvengeBeastRewards;
        public int NextCombatBeetles;
        public int NextCombatEnemyHealthToOne;
        public bool NextCombatLeftmostCopiesNearestEnemyStats;
        public bool NextCombatLeftmostDoubleAttack;
        public bool NextCombatTriggerMixedMechanics;
        public int CombatSummonBonusAttack;
        public int CombatSummonBonusHealth;
        public bool CombatSummonTaunt;
        public bool CombatSummonDoubleStats;
        public int CombatSameTierSummonBuffTier;
        public int CombatSameTierSummonBuffAttack;
        public int CombatSameTierSummonBuffHealth;
        public int QuestFriendlyAttackAura;
        public bool QuestVolatileVenomActive;
        public bool QuestBoomSquadActive;
        public bool QuestGrimFreshenerActive;
        public bool QuestCycleOfEnergyActive;
        public bool QuestStableAmalgamationActive;
        public int QuestDeathrattleExtraTriggers;
        public int QuestTumblingAttack;
        public int QuestTumblingHealth;
        public int QuestTumblingAvengeAttack;
        public int QuestTumblingAvengeHealth;
        public int TrinketBirdFeederAvengeThreshold;
        public int TrinketBirdFeederAttack;
        public int TrinketBirdFeederHealth;
        public int TrinketBeetleBandAvengeThreshold;
        public int TrinketBeetleBandSummonCount;
        public int TrinketQuilligraphyAvengeThreshold;
        public int TrinketQuilligraphyAttack;
        public int TrinketQuilligraphyHealth;
        public int TrinketWickedTomeAvengeThreshold;
        public int TrinketWickedTomeAttack;
        public int TrinketWickedTomeHealth;
        public int TrinketStaffOfTheScourgeAvengeThreshold;
        public int TrinketCloudSerpentHornAvengeThreshold;
        public int TrinketFridgeMagnetAvengeThreshold;
        public int TrinketBattleHornAvengeThreshold;
        public bool TrinketBristlebachPortraitActive;
        public int TrinketCombatBeastSummonBonusAttack;
        public int TrinketCombatBeastSummonBonusHealth;
        public bool TrinketSlammaStickerActive;
        public bool TrinketBassgillPortraitActive;
        public int TrinketReinforcedShieldUses;
        public int TrinketTwinSkyLanternCopies;
        public int TrinketCeremonialSwordAttack;
        public int TrinketFaerieDragonScaleUses;
        public int TrinketAllianceKeychainTargets;
        public int TrinketDeathlyPhylacteryExtraDeathrattles;
        public bool TrinketHeraldStickerActive;
        public bool TrinketRylakPortraitActive;
        public int TrinketDivineSignetUses;
        public int TrinketMechagonAdapterUses;
        public int TrinketDeathtouchAppleUses;
        public bool TrinketTarecgosaStickerActive;
        public int TrinketUnholySanctumAttack;
        public int TrinketUnholySanctumHealth;
        public string TrinketUnholySanctumSourceCardId;
        public bool TrinketFishyStickerActive;
        public bool TrinketSoulFermenterActive;
        public int TrinketBelcherPortraitAttack;
        public int TrinketBelcherPortraitHealth;
        public string TrinketBelcherPortraitSourceCardId;
        public bool TrinketBoomControllerActive;
        public bool TrinketBloodGolemStickerActive;
        public bool TrinketBloodAmuletActive;
        public int TrinketAllPurposeKibbleAttack;
        public bool TrinketSTharaStickerActive;
        public int TrinketTemporaryBloodGemAttack;
        public int TrinketTemporaryBloodGemHealth;
        public int TrinketStartOfCombatExtraTriggers;
        public int TrinketPromoPortraitExtraTriggers;
        public int TrinketJarredFrostlingTargets;
        public int TrinketPowderKegTargets;
        public bool TrinketHoggyBankActive;
        public int TrinketRustyTridentTriggers;
        public int TrinketSkyGolemDeathrattleTriggers;
        public int TrinketJarOGemsAttackThreshold;
        public int TrinketJarOGemsAttackCounter;
        public int TrinketElementiumChestAttackThreshold;
        public int TrinketElementiumChestAttackCounter;
        public int TrinketGilneanRoseAvengeThreshold;
        public int TrinketGilneanRoseAttack;
        public int TrinketGilneanRoseHealth;
        public int TrinketTigerCarvingAttack;
        public int TrinketTigerCarvingHealth;
        public int TrinketThornspikePauldronAttack;
        public int TrinketThornspikePauldronHealth;
        public bool TrinketMugOfTheSireActive;
        public bool TrinketBlingtronSunglassesActive;
        public bool TrinketScrapsmithPortraitActive;
        public bool TrinketEyeOfDalaranActive;
        public int ElementalsPlayedThisTurn;
        public int BackToBackBonus;
        public int HelpfulRefreshes;
        public bool LostLastCombat;
        public int ElementalHealthBonus;
        public int BeetleAttackBonus;
        public int BeetleHealthBonus;
        public int FutureBallerAttackBonus;
        public int FutureBallerHealthBonus;
        public int UndeadAttackBonus;
        public int EternalKnightDeaths;
        public int AncestralAutomatonSummons;
        public int FriendlyMinionDeathsThisGame;
        public int DeathrattlesTriggeredThisGame;
        public int MurgleAttackBattlecries;
        public int MurgleHealthBattlecries;
        public List<MinionInstance> Shop = new List<MinionInstance>();
        public List<TavernShopSlotState> ShopSlots = new List<TavernShopSlotState>();
        public List<MinionInstance> Hand = new List<MinionInstance>();
        public Dictionary<string, int> Pool = new Dictionary<string, int>();
        public Dictionary<string, int> HeroEffectCounters = new Dictionary<string, int>();
        public DiscoverState Discover;
        public AdvancedMechanicState AdvancedMechanics = new AdvancedMechanicState();
        public SearchPlanState SearchPlan = new SearchPlanState();
        public TavernGrowthState Growth = new TavernGrowthState();
        public List<RecruitLogEntry> RecruitLog = new List<RecruitLogEntry>();
    }

    [Serializable]
    public sealed class SearchHint
    {
        public SearchHintType Type;
        public string Message;
        public SearchHintSeverity Severity;
    }

    [Serializable]
    public sealed class LocalPlayerState
    {
        public string HeroId;
        public string HeroPowerCardId;
        public List<string> ExtraHeroPowerCardIds = new List<string>();
        public int Health;
        public int MaxHealth;
        public int Armor;
        public TavernState Tavern = new TavernState();
        public List<MinionInstance> Board = new List<MinionInstance>();
        public Dictionary<Tribe, int> BoardTribeDistribution = new Dictionary<Tribe, int>();
    }

    [Serializable]
    public sealed class LocalOpponentState
    {
        public string Name;
        public string HeroId;
        public int Health;
        public int Armor;
        public int TavernTier;
        public List<MinionInstance> Board = new List<MinionInstance>();
        public bool Editable;
    }

    [Serializable]
    public sealed class OpponentWarbandSnapshot
    {
        public string HeroId;
        public int Round;
        public int TavernTier;
        public bool Eliminated;
        public List<MinionInstance> Warband = new List<MinionInstance>();
    }

    [Serializable]
    public sealed class OpponentHistoryState
    {
        public string LastOpponentHeroId;
        public int LastOpponentRound;
        public int LastOpponentTavernTier;
        public List<MinionInstance> LastOpponentWarband = new List<MinionInstance>();
        public List<MinionInstance> RecentCombatDeaths = new List<MinionInstance>();
        public List<OpponentWarbandSnapshot> EliminatedPlayerWarbands = new List<OpponentWarbandSnapshot>();
    }

    [Serializable]
    public sealed class MatchState
    {
        public MatchMode Mode;
        public MatchPhase Phase;
        public int Round;
        public int Seed;
        public List<Tribe> ActiveTribes = new List<Tribe>();
        public string CardPoolVersionId;
        public string CardPoolVersionName;
        public bool IsDefaultCardPoolVersion = true;
        public List<string> EnabledMinionCardIds = new List<string>();
        public List<string> EnabledTavernSpellCardNumbers = new List<string>();
        public LocalPlayerState Player = new LocalPlayerState();
        public LocalOpponentState Opponent = new LocalOpponentState();
        public OpponentHistoryState OpponentHistory = new OpponentHistoryState();
        public List<SearchHint> RecruitHints = new List<SearchHint>();
        public List<CombatLogEntry> CombatLog = new List<CombatLogEntry>();
        public CombatOutput LastResult;
        public CombatReplay LastReplay;
    }
}
