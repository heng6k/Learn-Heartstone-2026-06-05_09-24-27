using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public enum TrinketSlotKind
    {
        Lesser,
        Greater
    }

    public enum TrinketImplementationStatus
    {
        Implemented,
        FrameworkFirst,
        Planned,
        Deferred,
        Unsupported,
        Unregistered
    }

    public enum TrinketOfferPoolStatus
    {
        Offerable,
        HiddenEffectOnly,
        DebugOnly,
        Disabled
    }

    public enum TrinketPowerLevel
    {
        Pending = 0,
        Weak = 1,
        Medium = 2,
        Strong = 3,
        Premium = 4
    }

    public enum TrinketTriggerTemplate
    {
        Auto,
        Unknown,
        OnEquip,
        Passive,
        TurnStart,
        TurnEnd,
        ShopRefresh,
        CardBought,
        CardSold,
        MinionPlayed,
        SpellCast,
        SpellcraftCast,
        StartOfCombat,
        Avenge,
        Combat
    }

    public enum TrinketEffectTemplate
    {
        Auto,
        Unknown,
        Economy,
        BuffStats,
        GrantKeyword,
        GenerateCard,
        Discover,
        Summon,
        ShopModifier,
        CombatModifier,
        Deathrattle,
        TribeSynergy,
        SpellSynergy,
        PoolModifier,
        Utility
    }

    public sealed class TrinketDefinition
    {
        public string Id;
        public string CardId;
        public int DbfId;
        public string SourceName;
        public string Name;
        public TrinketSlotKind SlotKind;
        public int Cost;
        public string Text;
        public string ImagePath;
        public string ImageUrl;
        public List<string> Mechanics = new List<string>();
        public List<string> ReferencedTags = new List<string>();
        public List<string> AssociatedRaces = new List<string>();
        public int RelatedDbfId;
        public List<string> Tags = new List<string>();
        public List<string> EffectIds = new List<string>();
        public TrinketImplementationStatus ImplementationStatus;
        public TrinketOfferPoolStatus OfferPoolStatus = TrinketOfferPoolStatus.DebugOnly;
        public TrinketPowerLevel PowerLevel = TrinketPowerLevel.Pending;
        public string EffectFamily;
        public TrinketTriggerTemplate TriggerTemplate = TrinketTriggerTemplate.Auto;
        public TrinketEffectTemplate EffectTemplate = TrinketEffectTemplate.Auto;
        public List<string> Requires = new List<string>();
        public string ProxyLevel;
        public string Notes;
    }

    [Serializable]
    public sealed class EquippedTrinketState
    {
        public string TrinketId;
        public string Name;
        public TrinketSlotKind SlotKind;
        public int EquippedRound;
        public int CostPaid;
        public TrinketImplementationStatus ImplementationStatus;
    }

    [Serializable]
    public sealed class PlayerTrinketState
    {
        public string LesserTrinketId;
        public string GreaterTrinketId;
        public string LesserCrystalBallCopiedTrinketId;
        public string GreaterCrystalBallCopiedTrinketId;
        public string MysteryCubeHeroPowerTrinketId;
        public List<EquippedTrinketState> Equipped = new List<EquippedTrinketState>();
        public int ExtraMaxGold;
        public int DalaranCheeseWheelRefreshes;
        public int DalaranCheeseWheelBonusAttack;
        public int DalaranCheeseWheelBonusHealth;
        public int OrnateClockGreaterOfferRound;
        public int WornTreasureMapDueRound;
        public bool WornTreasureMapClaimed;
        public int ShamanPrayerBeadsBattlecryBuys;
        public int ReusableBatteriesLastTriggerRound;
        public bool StuffedCoinPurseClaimed;
        public bool MysteriousOrbNextTrinketIsLesser;
        public int HeartOfForestBonusAttack;
        public int HeartOfForestBonusHealth;
        public int HeartOfForestCastProgress;
        public int MarvelousMushroomBonusAttack;
        public int MarvelousMushroomBonusHealth;
        public int PeacebloomCandleRound;
        public int PeacebloomCandleBuysThisRound;
        public int SinstoneStickerRound;
        public int SinstoneStickerCopiesThisRound;
        public int LubberStickerRound;
        public int LubberStickerTavernSpellBuysThisRound;
        public int WaterWheelRound;
        public int WaterWheelTriggersThisRound;
        public int CharmingPanpipesAttack;
        public int CharmingPanpipesHealth;
        public int LuckyTabbyDeaths;
        public int BleedingHeartDeaths;
        public int StormcoilStickerDeaths;
        public int LavaLampSoldMinions;
        public int FungalmancerStickerSoldMinions;
        public int AvalancheStickerSoldMinions;
        public int GemDonationSoldRound;
        public int DarnassusPieSoldMinionsThisTurn;
        public int WildfeatherDusterBeastSummons;
        public int GoosePortraitBeastSummons;
        public int FangAnkletBonusAttack;
        public int FangAnkletBonusHealth;
        public int AllPurposeKibbleAttack;
        public int FelburnedLedgerBonusThisTurn;
    }
}
