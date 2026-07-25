using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public enum HeroEffectEventType
    {
        MatchStarted,
        HeroPowerUsed,
        CardBought,
        CardPlayed,
        TavernSpellCast,
        MinionSold,
        ShopRefreshed,
        TurnStarted,
        TurnEnded,
        DiscoverChosen,
        Magnetized,
        BattlecryTriggered,
        FriendlyMinionDiedInCombat,
        FriendlyDeathrattleTriggeredInCombat,
        FriendlyMinionKilledEnemyInCombat,
        FriendlyMinionAttackedInCombat,
        FriendlyMinionSummonedInCombat,
        CombatEnded
    }

    public sealed class HeroBattlecryReplayRequest
    {
        public MinionInstance Source;
        public int RepeatCount = 1;
        public int TargetIndex = -1;
        public TargetZone TargetZone = TargetZone.Unspecified;
        public string TargetInstanceId;
    }

    public sealed class HeroBattlecryReplayResult
    {
        public int ResolvedRepeats;
        public List<string> Messages = new List<string>();
    }

    public sealed class HeroEffectContext
    {
        public HeroEffectEventType EventType;
        public MatchState State;
        public HeroCatalog Heroes;
        public MinionCatalog Minions;
        public SpellCatalog Spells;
        public SeededRng Rng;
        public MinionInstance Card;
        public MinionInstance TargetCard;
        public int GoldCost;
        public int TargetIndex = -1;
        public TargetZone TargetZone = TargetZone.Unspecified;
        public int SecondaryTargetIndex = -1;
        public TargetZone SecondaryTargetZone = TargetZone.Unspecified;
        public string TargetInstanceId;
        public string SecondaryTargetInstanceId;
        public string ChoiceId;
        public string DiscoverSource;
        public Func<HeroBattlecryReplayRequest, HeroBattlecryReplayResult> BattlecryResolver;
    }

    public sealed class HeroCombatEffectContext
    {
        public MatchState State;
        public MinionCatalog Minions;
        public SeededRng Rng;
        public List<MinionInstance> PlayerBoard;
        public List<MinionInstance> OpponentBoard;
        public List<string> ActiveHeroPowerCardIds;
    }

    public sealed class HeroEffectResult
    {
        public List<string> Messages = new List<string>();
    }

    public static class HeroEffectEngine
    {
        private const int BoardLimit = 7;
        private const int HandLimit = 10;
        private const string TavernCoinCardNumber = "104436";
        private const string ForestLordCenariusPowerId = "BG32_HERO_001p";
        private const string MalorneCardId = "BG32_HERO_001_Buddy";
        private const string NozdormuPowerId = "TB_BaconShop_HP_063";
        private const string ChromieCardId = "TB_BaconShop_HERO_57_Buddy";
        private const string MillhousePowerId = "TB_BaconShop_HP_054";
        private const string MagnusManastormCardId = "TB_BaconShop_HERO_49_Buddy";
        private const string GallywixPowerId = "TB_BaconShop_HP_008";
        private const string BilgewaterMogulCardId = "TB_BaconShop_HERO_10_Buddy";
        private const string KaelthasPowerId = "TB_BaconShop_HP_066";
        private const string CrimsonHandCenturionCardId = "TB_BaconShop_HERO_60_Buddy";
        private const string ExarchOthaarPowerId = "BG31_HERO_006p";
        private const string CelestialArchiveCardId = "BG31_HERO_006_Buddy";
        private const string TaethelanPowerId = "BG28_HERO_800p";
        private const string ReliquaryAttendantCardId = "BG28_HERO_800_Buddy";
        private const string VardenPowerId = "BG22_HERO_004p";
        private const string VardenAquarriorCardId = "BG22_HERO_004_Buddy";
        private const string WeebominationCardId = "TB_BaconShop_HERO_34_Buddy";
        private const string ForestWardenOmuPowerId = "TB_BaconShop_HP_082";
        private const string EvergreenBotaniCardId = "TB_BaconShop_HERO_74_Buddy";
        private const string CapnHoggarrPowerId = "BG26_HERO_101p";
        private const string ShiningSailorCardId = "BG26_HERO_101_Buddy";
        private const string YseraPowerId = "TB_BaconShop_HP_062";
        private const string ValithriaDreamwalkerCardId = "TB_BaconShop_HERO_53_Buddy";
        private const string EnhanceOMechanoPowerId = "BG24_HERO_204p";
        private const string EnhanceOMedicoCardId = "BG24_HERO_204_Buddy";
        private const string KurtrusPowerId = "BG20_HERO_280p5";
        private const string LivingNightmareCardId = "BG20_HERO_280_Buddy";
        private const string FlurglPowerId = "TB_BaconShop_HP_056";
        private const string SparkfinSoothsayerCardId = "TB_BaconShop_HERO_55_Buddy";
        private const string SaurfangPowerId = "BG20_HERO_102p";
        private const string DranoshSaurfangCardId = "BG20_HERO_102_Buddy";
        private const string EdwinPowerId = "TB_BaconShop_HP_001";
        private const string SI7ScoutCardId = "TB_BaconShop_HERO_01_Buddy";
        private const string KraggPowerId = "TB_BaconShop_HP_076";
        private const string SharkbaitCardId = "TB_BaconShop_HERO_68_Buddy";
        private const string GeorgePowerId = "TB_BaconShop_HP_010";
        private const string KarlTheLostCardId = "TB_BaconShop_HERO_15_Buddy";
        private const string FarseerNobundoPowerId = "BG31_HERO_003p";
        private const string DoctorHollidaePowerId = "BG28_HERO_801p";
        private const string NineFrogsCardId = "BG28_HERO_801_Buddy";
        private const string SnakeEyesPowerId = "BG28_HERO_400p";
        private const string BoxCarsCardId = "BG28_HERO_400_Buddy";
        private const string SnakeEyesLastRollCounter = "hero:snake_eyes:last_roll";
        private const string SnakeEyesReadyRoundCounter = "hero:snake_eyes:ready_round";
        private const string BoxCarsLastRollCounter = "hero:snake_eyes:box_cars_last_roll";
        private const string BlackthornPowerId = "BG20_HERO_103p";
        private const string DeathsHeadSageCardId = "BG20_HERO_103_Buddy";
        private const string LichBazHialPowerId = "TB_BaconShop_HP_049";
        private const string UnearthedUnderlingCardId = "TB_BaconShop_HERO_25_Buddy";
        private const string RakanishuPowerId = "TB_BaconShop_HP_085";
        private const string LanternTenderCardId = "TB_BaconShop_HERO_75_Buddy";
        private const string LanternLightCardId = "RAKANISHU_LANTERN_LIGHT";
        private const string RenoPowerId = "TB_BaconShop_HP_046";
        private const string SrTombDiverCardId = "TB_BaconShop_HERO_41_Buddy";
        private const string PatchesPowerId = "TB_BaconShop_HP_072";
        private const string TuskarrRaiderCardId = "TB_BaconShop_HERO_18_Buddy";
        private const string BloodGemCardId = "BLOOD_GEM";
        private const string KingMuklaPowerId = "TB_BaconShop_HP_038";
        private const string CrazyMonkeyCardId = "TB_BaconShop_HERO_38_Buddy";
        private const string MuklaBananaCardId = "MUKLA_BANANA";
        private const string CThunPowerId = "TB_BaconShop_HP_104";
        private const string TentacleOfCThunCardId = "TB_BaconShop_HERO_29_Buddy";
        private const string CaptainEudoraPowerId = "TB_BaconShop_HP_074";
        private const string DagwikStickytoeCardId = "TB_BaconShop_HERO_64_Buddy";
        private const string ElisePowerId = "TB_BaconShop_HP_047";
        private const string JrNavigatorCardId = "TB_BaconShop_HERO_42_Buddy";
        private const string MillificentPowerId = "TB_BaconShop_HP_015";
        private const string ElementiumSquirrelBombCardId = "TB_BaconShop_HERO_17_Buddy";
        private const string LichKingPowerId = "TB_BaconShop_HP_024";
        private const string ArfusCardId = "TB_BaconShop_HERO_22_Buddy";
        private const string ShudderwockPowerId = "TB_BaconShop_HP_022";
        private const string MuckslingerCardId = "TB_BaconShop_HERO_23_Buddy";
        private const string JandicePowerId = "TB_BaconShop_HP_084";
        private const string JandiceApprenticeCardId = "TB_BaconShop_HERO_71_Buddy";
        private const string MutanusPowerId = "BG20_HERO_301p";
        private const string NightmareEctoplasmCardId = "BG20_HERO_301_Buddy";
        private const string XyrellaPowerId = "BG20_HERO_101p";
        private const string BabyElekkCardId = "BG20_HERO_101_Buddy";
        private const string PyramadPowerId = "TB_BaconShop_HP_040";
        private const string TitanicGuardianCardId = "TB_BaconShop_HERO_39_Buddy";
        private const string VoljinPowerId = "BG20_HERO_201p";
        private const string MasterGadrinCardId = "BG20_HERO_201_Buddy";
        private const string IngePowerId = "BG26_HERO_102p";
        private const string SolemnSerenaderCardId = "BG26_HERO_102_Buddy";
        private const string MalygosPowerId = "TB_BaconShop_HP_052";
        private const string NexusLordCardId = "TB_BaconShop_HERO_58_Buddy";
        private const string MaievPowerId = "TB_BaconShop_HP_068";
        private const string ShadowWardenCardId = "TB_BaconShop_HERO_62_Buddy";
        private const string ZephrysPowerId = "TB_BaconShop_HP_102";
        private const string PhyreszCardId = "TB_BaconShop_HERO_91_Buddy";
        private const string HooktuskPowerId = "TB_BaconShop_HP_075";
        private const string RagingContenderCardId = "TB_BaconShop_HERO_67_Buddy";
        private const string VoonePowerId = "BG26_HERO_104p";
        private const string AkaliRockRhinoCardId = "BG26_HERO_104_Buddy";
        private const string ZerekPowerId = "BG31_HERO_005p";
        private const string MiniZerekCardId = "BG31_HERO_005_Buddy";
        private const string TogwagglePowerId = "BG23_HERO_305p";
        private const string WaxadredCardId = "BG23_HERO_305_Buddy";
        private const string ChenvaalaPowerId = "TB_BaconShop_HP_088";
        private const string SnowElementalCardId = "TB_BaconShop_HERO_78_Buddy";
        private const string CuratorPowerId = "TB_BaconShop_HP_033";
        private const string MishmashCardId = "TB_BaconShop_HERO_33_Buddy";
        private const string DerylPowerId = "TB_BaconShop_HP_042";
        private const string AsherHaberdasherCardId = "TB_BaconShop_HERO_36_Buddy";
        private const string RagnarosPowerId = "TB_BaconShop_HP_087";
        private const string LucifronCardId = "TB_BaconShop_HERO_11_Buddy";
        private const string ChromieTwisterPowerId = "BG34_HERO_001p";
        private const string SindragosaPowerId = "TB_BaconShop_HP_014";
        private const string ThawedChampionCardId = "TB_BaconShop_HERO_27_Buddy";
        private const string AlAkirPowerId = "TB_BaconShop_HP_086";
        private const string SpiritOfAirCardId = "TB_BaconShop_HERO_76_Buddy";
        private const string TavishPowerId = "BG22_HERO_000p";
        private const string TamsinPowerId = "BG20_HERO_282p";
        private const string OnyxiaPowerId = "BG22_HERO_305p";
        private const string BrukanPowerId = "BG22_HERO_001p";
        private const string TamsinPhylacteryTag = "hero_tamsin_phylactery";
        private const string OnyxiaWhelpStatsCounter = "hero:onyxia:whelp_stats";
        private const string YshaarjPowerId = "TB_BaconShop_HP_103";
        private const string BabyYshaarjCardId = "TB_BaconShop_HERO_92_Buddy";
        private const string DeathwingPowerId = "TB_BaconShop_HP_061";
        private const string SinestraCardId = "TB_BaconShop_HERO_52_Buddy";
        private const string IllidanPowerId = "TB_BaconShop_HP_069";
        private const string EclipsionIllidariCardId = "TB_BaconShop_HERO_08_Buddy";
        private const string EclipsionFirstAttackImmunePendingTag = "eclipsion_first_attack_immune_pending";
        private const string QueenWagtogglePowerId = "TB_BaconShop_HP_037a";
        private const string ElderTaggawagCardId = "TB_BaconShop_HERO_14_Buddy";
        private const string NzothPowerId = "TB_BaconShop_HP_105";
        private const string BabyNzothCardId = "TB_BaconShop_HERO_93_Buddy";
        private const string VanndarPowerId = "BG22_HERO_003p";
        private const string StormpikeLieutenantCardId = "BG22_HERO_003_Buddy";
        private const string DrektharPowerId = "BG22_HERO_002p";
        private const string FrostwolfLieutenantCardId = "BG22_HERO_002_Buddy";
        private const string TeronPowerId = "BG25_HERO_103p";
        private const string RafaamPowerId = "TB_BaconShop_HP_053";
        private const string LoyalHenchmanCardId = "TB_BaconShop_HERO_45_Buddy";
        private const string RokaraPowerId = "BG20_HERO_100p";
        private const string IcesnarlCardId = "BG20_HERO_100_Buddy";
        private const string SylvanasPowerId = "BG23_HERO_306p";
        private const string NathanosCardId = "BG23_HERO_306_Buddy";
        private const string SneedPowerId = "BG21_HERO_030p";
        private const string PilotedWhirlOTronCardId = "BG21_HERO_030_Buddy";
        private const string JailerPowerId = "TB_BaconShop_HP_702";
        private const string MawswornSoulkeeperCardId = "TB_BaconShop_HERO_702_Buddy";
        private const string GreyboughPowerId = "TB_BaconShop_HP_107";
        private const string WanderingTreantCardId = "TB_BaconShop_HERO_95_Buddy";
        private const string IniStormcoilPowerId = "BG22_HERO_200p";
        private const string SubScrubberCardId = "BG22_HERO_200_Buddy";
        private const string OzumatPowerId = "BG23_HERO_201p";
        private const string TamuzoCardId = "BG23_HERO_201_Buddy";
        private const string ArannaPowerId = "TB_BaconShop_HP_065";
        private const string SklibbCardId = "TB_BaconShop_HERO_59_Buddy";
        private const string JaraxxusPowerId = "TB_BaconShop_HP_036";
        private const string KilrekCardId = "TB_BaconShop_HERO_37_Buddy";
        private const string SilasPowerId = "TB_BaconShop_HP_101";
        private const string BurthCardId = "TB_BaconShop_HERO_90_Buddy";
        private const string CookiePowerId = "BG21_HERO_020p";
        private const string SousChefCardId = "BG21_HERO_020_Buddy";
        private const string GalakrondPowerId = "TB_BaconShop_HP_011";
        private const string GalakrondApostleCardId = "TB_BaconShop_HERO_02_Buddy";
        private const string EtcPowerId = "BG25_HERO_105p";
        private const string TalentScoutCardId = "BG25_HERO_105_Buddy";
        private const string FinleyPowerId = "TB_BaconShop_HP_057";
        private const string MaxwellCardId = "TB_BaconShop_HERO_40_Buddy";
        private const string RatKingPowerId = "TB_BaconShop_HP_041";
        private const string PigeonLordCardId = "TB_BaconShop_HERO_12_Buddy";
        private const string BarovPowerId = "TB_BaconShop_HP_081";
        private const string BarovsApprenticeCardId = "TB_BaconShop_HERO_72_Buddy";
        private const string HolmesPowerId = "BG23_HERO_303p2";
        private const string WatfinCardId = "BG23_HERO_303_Buddy";
        private const string TessPowerId = "TB_BaconShop_HP_077";
        private const string BigglesworthPowerId = "TB_BaconShop_HP_080";
        private const string LilKtCardId = "TB_BaconShop_HERO_70_Buddy";
        private const string ScabbsPowerId = "BG21_HERO_010p";
        private const string WardenThelwaterCardId = "BG21_HERO_010_Buddy";
        private const string LohPowerId = "BG33_HERO_001p_ALT";
        private const string LohAttackCounter = "hero:loh:friendly_attacks";
        private const int LohAttackThreshold = 15;
        private const string DinotamerBrannPowerId = "TB_BaconShop_HP_048";
        private const string DinotamerBrannBoughtCounter = "hero:dinotamer_brann:battlecry_bought";
        private const string DinotamerBrannGrantedCounter = "hero:dinotamer_brann:granted";
        private const string BrannBronzebeardCardId = "BG_LOE_077";
        private const string QueenAzsharaPowerId = "BG22_HERO_007p";
        private const string QueenAzsharaConquestCounter = "hero:azshara:naga_conquest_started";
        private const string QueenAzsharaConquestSource = "hero-power:naga-conquest";
        private const int QueenAzsharaAttackThreshold = 30;
        private const string AkazamzarakPowerId = "TB_BaconShop_HP_020";
        private const string FantasticBellhopCardId = "BG30_HERO_304_Buddy";
        private const string ZippersCardId = "BG32_HERO_002_Buddy";
        private const string StreetMagicianCardId = "TB_BaconShop_HERO_21_Buddy";
        private const string FestergutCardId = "BG25_HERO_100_Buddy";
        private const string TychusFindlayCardId = "BG31_HERO_801_Buddy";
        private const string ProbiusCardId = "BG31_HERO_802_Buddy";
        private const string BrokenHornCardId = "BG31_HERO_811_Buddy";
        private const string PutricidePowerId = "BG25_HERO_100p";
        private const string PutricideCreationCardId = "BG25_HERO_100pt";
        private const string RaynorHeroId = "BG31_HERO_801";
        private const string RaynorPowerId = "BG31_HERO_801p";
        private const string BattlecruiserCardId = "BG31_HERO_801pt";
        private const string RaynorStartingBattlecruiserTag = "hero_start:raynor_battlecruiser";
        private const string KerriganPowerTier2Id = "BG31_HERO_811p";
        private const string KerriganPowerTier3Id = "BG31_HERO_811p2";
        private const string KerriganPowerFinalId = "BG31_HERO_811p3";
        private const string ZergLarvaCardId = "BG31_HERO_811t";
        private const string ArtanisPowerId = "BG31_HERO_802p";
        private const string HunterOfOldCardId = "TB_BaconShop_HERO_50_Buddy";
        private const string BattlecruiserUpgradeCardId = "BATTLECRUISER_UPGRADE";
        private const string ZergProxyCardId = "ZERG_MINION_PROXY";
        private const string NzothFishCardId = "TB_BaconShop_HP_105t";
        private const string SneedShredderCardId = "BG21_HERO_030t";
        private const string CuratorAmalgamCardId = "TB_BaconShop_HP_033t";
        private const string OzumatTentacleCardId = "OZUMAT_TENTACLE";
        private static readonly string[] BountyCardIds =
        {
            "122182",
            "122183",
            "122184",
            "122185",
            "122186"
        };

        private const string KaelthasBoughtCounter = "hero:kaelthas:minions_bought";
        private const string TaethelanSpellCounter = "hero:taethelan:spells_bought";
        private const string ReliquaryCopiedRoundCounter = "hero:reliquary_attendant:copied_round";
        private const string MillhouseFreeRefreshRoundCounter = "hero:magnus:free_refresh_round";
        private const string MillhouseFreeRefreshCounter = "hero:magnus:free_refreshes";
        private const string MaxGoldBonusCounter = "hero:shared:max_gold_bonus";
        private const string KurtrusBoughtRoundCounter = "hero:kurtrus:bought_round";
        private const string KurtrusBoughtCounter = "hero:kurtrus:minions_bought_this_turn";
        private const string KurtrusTriggeredRoundCounter = "hero:kurtrus:triggered_round";
        private const string FlurglSoldCounter = "hero:flurgl:minions_sold";
        private const string SaurfangBoughtCounter = "hero:saurfang:minions_bought";
        private const string SaurfangHealthBonusCounter = "hero:saurfang:health_bonus";
        private const string EdwinBoughtCounter = "hero:edwin:cards_bought";
        private const string EdwinBuffAmountCounter = "hero:edwin:buff_amount";
        private const string KraggUsedCounter = "hero:kragg:piggy_bank_used";
        private const string NobundoHeroPowerDiscountCounter = "hero:nobundo:hero_power_discount";
        private const string NineFrogsRemainingCounter = "hero:hollidae:nine_frogs_remaining";
        private const string BlackthornRoundCounter = "hero:blackthorn:round";
        private const string BlackthornUsesCounter = "hero:blackthorn:uses";
        private const string RenoUsedCounter = "hero:reno:gonna_be_rich_used";
        private const string PatchesDiscountCounter = "hero:patches:discount";
        private const string CrazyMonkeyBananaCounter = "hero:mukla:crazy_monkey_bananas";
        private const string CrazyMonkeySpellCounter = "hero:mukla:crazy_monkey_spells";
        private const string CThunRepeatCounter = "hero:cthun:repeat_count";
        private const string EudoraDigCounter = "hero:eudora:digs";
        private const string EliseCostIncreaseCounter = "hero:elise:cost_increase";
        private const string MillificentMechDeathCounter = "hero:millificent:mech_deaths";
        private const string IngeModeCounter = "hero:inge:mode";
        private const string MalygosRoundCounter = "hero:malygos:round";
        private const string MalygosUsesCounter = "hero:malygos:uses";
        private const string MaievNextGoldenCounter = "hero:maiev:next_golden";
        private const string ZephrysWishesCounter = "hero:zephrys:wishes";
        private const string VooneHeroCounter = "hero:voone:turns";
        private const string VooneBuddyCounter = "hero:voone:buddy_turns";
        private const string ZerekUsedCounter = "hero:zerek:used";
        private const string TogwaggleDiscountCounter = "hero:togwaggle:discount";
        private const string ChenvaalaElementalCounter = "hero:chenvaala:elementals";
        private const string RagnarosBoughtCounter = "hero:ragnaros:bought";
        private const string RagnarosUnlockedCounter = "hero:ragnaros:unlocked";
        private const string OzumatTentacleStatsCounter = "hero:ozumat:tentacle_stats";
        private const string JailerHealthBonusCounter = "hero:jailer:health_bonus";
        private const string RafaamArmedCounter = "hero:rafaam:armed";
        private const string CombatKillRoundCounter = "hero:shared:combat_kill_round";
        private const string CombatKillCountCounter = "hero:shared:combat_kills_this_round";
        private const string IniCombatFriendlyDeathsCounter = "hero:ini:combat_friendly_deaths";
        private const string ArannaAttackCounter = "hero:aranna:friendly_attacks";
        private const string ArannaFirstBuyFreeCounter = "hero:aranna:first_buy_free";
        private const string BigglesworthDiscoverCountCounter = "hero:bigglesworth:discovers";
        private const string BigglesworthSnapshotIndexCounter = "hero:bigglesworth:snapshot_index";
        private const string RatKingCurrentTribeCounter = "hero:rat_king:current_tribe";
        private const string RatKingLastTribeCounter = "hero:rat_king:last_tribe";
        private const string PigeonLordRefreshRoundCounter = "hero:rat_king:pigeon_refresh_round";
        private const string BarovPredictionRoundCounter = "hero:barov:prediction_round";
        private const string BarovPredictionCounter = "hero:barov:prediction";
        public const string HolmesDiscoverSource = "hero-power:murloc-holmes";
        private const string HolmesCorrectGuessTag = "murloc_holmes_correct_guess";
        private const string SilasTicketTag = "silas_darkmoon_ticket";
        private const string SilasTicketCounter = "hero:silas:tickets";
        private const string BurthBuffCounter = "hero:silas:burth_buff";
        private const string CookieRoundCounter = "hero:cookie:round";
        private const string CookieUsesCounter = "hero:cookie:uses";
        private const string CookieFedCounter = "hero:cookie:fed";
        private const string CookieTribeCounterPrefix = "hero:cookie:tribe:";
        private const string TychusSpellCounter = "hero:tychus:tavern_spells";
        private const string PutricideCreationsLeftCounter = "hero:putricide:creations_left";
        private const string KerriganUnlockedTierCounter = "hero:kerrigan:unlocked_tier";
        private const string KerriganCostCounter = "hero:kerrigan:cost";
        private const string ArtanisBoughtCounter = "hero:artanis:cards_bought";
        private const string ArtanisRewardClaimedCounter = "hero:artanis:reward_claimed";
        public const string PutricideFirstDiscoverSource = "hero:putricide:first-component";
        public const string PutricideSecondDiscoverSource = "hero:putricide:second-component";
        public const string PutricideComponentTagPrefix = "putricide_component:";
        public const string PutricideCreationTag = "putricide_creation";
        public const string KerriganMorphDiscoverSource = "hero:kerrigan:morph";
        public const string ArtanisProtossDiscoverSource = "hero:artanis:warp-gate";
        public const string ArtanisSelectedRewardKey = "hero:artanis:selected_reward";
        private const string LichKingRebornSource = "Reborn Rites";
        private const string VoljinSwapSource = "Spirit Swap";
        private const string TeronTargetTag = "teron_reanimation_target";
        private const string LockedTurnsCounter = "locked-turns";
        private static readonly string[] BattlecruiserUpgradeFamilies =
        {
            "BG31_HERO_801pta",
            "BG31_HERO_801ptb",
            "BG31_HERO_801ptc",
            "BG31_HERO_801ptd",
            "BG31_HERO_801pte",
            "BG31_HERO_801ptf",
            "BG31_HERO_801pth",
            "BG31_HERO_801pti",
            "BG31_HERO_801ptj"
        };
        private static readonly string[] ZergTier2CardIds =
        {
            "BG31_HERO_811t2",
            "BG31_HERO_811t3",
            "BG31_HERO_811t4",
            "BG31_HERO_811t5"
        };
        private static readonly string[] ZergTier3CardIds =
        {
            "BG31_HERO_811t6",
            "BG31_HERO_811t7",
            "BG31_HERO_811t8",
            "BG31_HERO_811t9",
            "BG31_HERO_811t10"
        };
        private static readonly string[] ProtossRewardCardIds =
        {
            "BG31_HERO_802pt",
            "BG31_HERO_802pt1",
            "BG31_HERO_802pt4",
            "BG31_HERO_802pt5",
            "BG31_HERO_802pt7"
        };
        private static readonly Keyword[] BonusKeywords =
        {
            Keyword.Taunt,
            Keyword.DivineShield,
            Keyword.Reborn,
            Keyword.Windfury
        };
        private sealed class PutricideCreationComponent
        {
            public string Id;
            public string Name;
            public int Attack;
            public int Health;
            public Keyword? Keyword;
            public string Text;
            public string EffectTag;
        }

        private static readonly PutricideCreationComponent[] PutricideComponents =
        {
            new PutricideCreationComponent
            {
                Id = "hulking-frame",
                Name = "Hulking Frame",
                Attack = 4,
                Health = 5,
                Text = "+4/+5."
            },
            new PutricideCreationComponent
            {
                Id = "spiked-shell",
                Name = "Spiked Shell",
                Attack = 3,
                Health = 4,
                Keyword = Keyword.Taunt,
                Text = "+3/+4 and Taunt.",
                EffectTag = "putricide_taunt_component"
            },
            new PutricideCreationComponent
            {
                Id = "reborn-stitching",
                Name = "Reborn Stitching",
                Attack = 2,
                Health = 3,
                Keyword = Keyword.Reborn,
                Text = "+2/+3 and Reborn.",
                EffectTag = "putricide_reborn_component"
            },
            new PutricideCreationComponent
            {
                Id = "volatile-glands",
                Name = "Volatile Glands",
                Attack = 3,
                Health = 2,
                Keyword = Keyword.Deathrattle,
                Text = "+3/+2 and Deathrattle.",
                EffectTag = "putricide_deathrattle_component"
            },
            new PutricideCreationComponent
            {
                Id = "toxic-ichor",
                Name = "Toxic Ichor",
                Attack = 2,
                Health = 2,
                Keyword = Keyword.Venomous,
                Text = "+2/+2 and Venomous.",
                EffectTag = "putricide_venomous_component"
            },
            new PutricideCreationComponent
            {
                Id = "borrowed-wings",
                Name = "Borrowed Wings",
                Attack = 3,
                Health = 1,
                Keyword = Keyword.Windfury,
                Text = "+3/+1 and Windfury.",
                EffectTag = "putricide_windfury_component"
            },
            new PutricideCreationComponent
            {
                Id = "plated-heart",
                Name = "Plated Heart",
                Attack = 1,
                Health = 4,
                Keyword = Keyword.DivineShield,
                Text = "+1/+4 and Divine Shield.",
                EffectTag = "putricide_divine_shield_component"
            }
        };
        private static readonly Tribe[] RatKingTribes =
        {
            Tribe.Beast,
            Tribe.Demon,
            Tribe.Dragon,
            Tribe.Elemental,
            Tribe.Mech,
            Tribe.Murloc,
            Tribe.Naga,
            Tribe.Pirate,
            Tribe.Quilboar,
            Tribe.Undead
        };

        public static HeroEffectResult Dispatch(HeroEffectContext context)
        {
            var result = new HeroEffectResult();
            if (context?.State?.Player?.Tavern == null)
            {
                return result;
            }

            EnsureCounters(context.State.Player.Tavern);
            DispatchHeroPower(context, result);
            DispatchBuddies(context, result);
            return result;
        }

        public static HeroEffectResult ApplyCombatStartEffects(HeroCombatEffectContext context)
        {
            var result = new HeroEffectResult();
            if (context?.State?.Player?.Tavern == null || context.PlayerBoard == null)
            {
                return result;
            }

            EnsureCounters(context.State.Player.Tavern);
            var activePowerIds = GetActiveCombatHeroPowerCardIds(context);
            ConfigureCombatSummonModifiers(context, activePowerIds);
            context.State.Player.Tavern.HeroTavishDeadeyeActive = HasCombatHeroPower(activePowerIds, TavishPowerId);
            context.State.Player.Tavern.HeroOnyxiaBroodmotherActive = HasCombatHeroPower(activePowerIds, OnyxiaPowerId);
            context.State.Player.Tavern.HeroBrukanElementActive =
                HasCombatHeroPower(activePowerIds, BrukanPowerId) &&
                !string.IsNullOrEmpty(context.State.Player.Tavern.HeroBrukanElement);
            context.State.Player.Tavern.HeroVanndarStormpikeActive =
                HasCombatHeroPower(activePowerIds, VanndarPowerId) && context.State.Round >= 7;
            context.State.Player.Tavern.HeroDrektharActive =
                HasCombatHeroPower(activePowerIds, DrektharPowerId) && context.State.Round >= 7;
            context.State.Player.Tavern.HeroTeronGorefiendActive = HasCombatHeroPower(activePowerIds, TeronPowerId);
            context.State.Player.Tavern.HeroTeronTargetInstanceId = context.State.Player.Tavern.HeroTeronGorefiendActive
                ? context.PlayerBoard.FirstOrDefault(minion => minion.Tags.Contains(TeronTargetTag))?.InstanceId
                : null;
            context.State.Player.Tavern.HeroOzumatActive = HasCombatHeroPower(activePowerIds, OzumatPowerId);

            if (HasCombatHeroPower(activePowerIds, AlAkirPowerId))
            {
                var target = context.PlayerBoard.FirstOrDefault();
                AddBonusKeywordSet(target, "Swatting Insects");
                if (target != null)
                {
                    result.Messages.Add("Swatting Insects: left-most minion gained Windfury, Divine Shield, and Taunt for combat.");
                }
            }

            foreach (var gadrin in context.PlayerBoard
                         .Where(minion => string.Equals(minion.CardId, MasterGadrinCardId, StringComparison.OrdinalIgnoreCase))
                         .ToList())
            {
                var index = context.PlayerBoard.FindIndex(minion => minion.InstanceId == gadrin.InstanceId);
                if (index > 0)
                {
                    var amount = gadrin.Golden ? 4 : 2;
                    Buff(context.PlayerBoard[index - 1], amount, amount, "Master Gadrin");
                    result.Messages.Add("Master Gadrin: buffed the minion to its left for combat.");
                }
            }

            if (HasCombatHeroPower(activePowerIds, DeathwingPowerId))
            {
                ApplyDeathwingCombatBuff(context, result);
            }

            if (HasCombatHeroPower(activePowerIds, TamsinPowerId))
            {
                ApplyTamsinPhylactery(context, result);
            }

            if (HasCombatHeroPower(activePowerIds, IllidanPowerId))
            {
                ApplyIllidanEdgeBuffs(context.PlayerBoard, result);
            }

            if (HasBuddy(context.State, EclipsionIllidariCardId))
            {
                ApplyEclipsionFirstAttackImmune(context, result);
            }

            if (HasCombatHeroPower(activePowerIds, QueenWagtogglePowerId))
            {
                ApplyWagtoggleCombatBuffs(context, result);
            }

            if (HasCombatHeroPower(activePowerIds, YshaarjPowerId))
            {
                SummonRandomTierMinionForCombat(context, Math.Max(1, context.State.Player.Tavern.Tier), "Y'Shaarj", true, result);
            }

            if (context.State.Player.Tavern.HeroOnyxiaBroodmotherActive &&
                GetCounterOrDefault(context.State.Player.Tavern, OnyxiaWhelpStatsCounter, 0) <= 0)
            {
                context.State.Player.Tavern.HeroEffectCounters[OnyxiaWhelpStatsCounter] = 3;
            }

            if (context.State.Player.Tavern.HeroBrukanElementActive)
            {
                result.Messages.Add("Embrace the Elements: " + context.State.Player.Tavern.HeroBrukanElement + " will be called at combat start.");
            }

            return result;
        }

        public static int ModifyBuyCost(MatchState state, string heroPowerCardId, MinionInstance target, int currentCost)
        {
            if (state?.Player?.Tavern == null || target == null)
            {
                return currentCost;
            }

            EnsureCounters(state.Player.Tavern);
            if (string.Equals(heroPowerCardId, ArannaPowerId, StringComparison.OrdinalIgnoreCase) &&
                target.CardKind == CardKind.Minion &&
                GetCounterOrDefault(state.Player.Tavern, ArannaFirstBuyFreeCounter, 0) > 0)
            {
                return 0;
            }

            if (string.Equals(heroPowerCardId, MillhousePowerId, StringComparison.OrdinalIgnoreCase) &&
                target.CardKind == CardKind.Minion)
            {
                currentCost = 2;
            }

            if (string.Equals(heroPowerCardId, SindragosaPowerId, StringComparison.OrdinalIgnoreCase) &&
                target.CardKind == CardKind.Minion)
            {
                currentCost = 2;
            }

            if (string.Equals(heroPowerCardId, TaethelanPowerId, StringComparison.OrdinalIgnoreCase) &&
                target.CardKind == CardKind.TavernSpell)
            {
                state.Player.Tavern.HeroEffectCounters.TryGetValue(TaethelanSpellCounter, out var bought);
                if ((bought + 1) % 4 == 0)
                {
                    return 0;
                }
            }

            return currentCost;
        }

        public static int ModifyShopSize(string heroPowerCardId, int currentSize)
        {
            if (string.Equals(heroPowerCardId, SindragosaPowerId, StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(1, currentSize - 1);
            }

            return currentSize;
        }

        public static int ModifyRefreshCost(MatchState state, string heroPowerCardId, int currentCost)
        {
            if (state?.Player?.Tavern == null)
            {
                return currentCost;
            }

            EnsureCounters(state.Player.Tavern);
            if (!string.Equals(heroPowerCardId, MillhousePowerId, StringComparison.OrdinalIgnoreCase))
            {
                return currentCost;
            }

            if (HasBuddy(state, MagnusManastormCardId))
            {
                var tavern = state.Player.Tavern;
                tavern.HeroEffectCounters.TryGetValue(MillhouseFreeRefreshRoundCounter, out var round);
                tavern.HeroEffectCounters.TryGetValue(MillhouseFreeRefreshCounter, out var used);
                if (round != state.Round)
                {
                    used = 0;
                }

                if (used < 2)
                {
                    return 0;
                }
            }

            return 2;
        }

        public static void RecordRefreshCostPaid(MatchState state, string heroPowerCardId, int cost)
        {
            if (state?.Player?.Tavern == null ||
                cost != 0 ||
                !string.Equals(heroPowerCardId, MillhousePowerId, StringComparison.OrdinalIgnoreCase) ||
                !HasBuddy(state, MagnusManastormCardId))
            {
                return;
            }

            var tavern = state.Player.Tavern;
            EnsureCounters(tavern);
            tavern.HeroEffectCounters.TryGetValue(MillhouseFreeRefreshRoundCounter, out var round);
            tavern.HeroEffectCounters.TryGetValue(MillhouseFreeRefreshCounter, out var used);
            if (round != state.Round)
            {
                used = 0;
                tavern.HeroEffectCounters[MillhouseFreeRefreshRoundCounter] = state.Round;
            }

            tavern.HeroEffectCounters[MillhouseFreeRefreshCounter] = used + 1;
        }

        public static int ModifyUpgradeCost(MatchState state, string heroPowerCardId, int currentCost)
        {
            return string.Equals(heroPowerCardId, MillhousePowerId, StringComparison.OrdinalIgnoreCase)
                ? currentCost + 1
                : currentCost;
        }

        public static int ModifyTurnMaxGold(MatchState state, int currentMaxGold)
        {
            if (state?.Player?.Tavern == null)
            {
                return currentMaxGold;
            }

            EnsureCounters(state.Player.Tavern);
            state.Player.Tavern.HeroEffectCounters.TryGetValue(MaxGoldBonusCounter, out var bonus);
            return currentMaxGold + Math.Max(0, bonus);
        }

        private static void DispatchHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var powerId = context.State.Player.HeroPowerCardId;
            switch (context.EventType)
            {
                case HeroEffectEventType.MatchStarted:
                    ResolveMatchStartedHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.HeroPowerUsed:
                    UseHeroPower(context, result, powerId);
                    break;
                case HeroEffectEventType.CardBought:
                    ResolveCardBoughtHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.ShopRefreshed:
                    if (IsPower(powerId, VardenPowerId))
                    {
                        ResolveVardenRefresh(context, result);
                    }
                    if (IsPower(powerId, YseraPowerId))
                    {
                        ResolveYseraRefresh(context, result);
                    }
                    if (IsPower(powerId, CapnHoggarrPowerId))
                    {
                        ResolveHoggarrRefresh(context, result);
                    }
                    if (IsPower(powerId, EnhanceOMechanoPowerId))
                    {
                        ResolveEnhanceOMechanoRefresh(context, result);
                    }
                    if (IsPower(powerId, SaurfangPowerId))
                    {
                        ApplySaurfangShopBuff(context.State);
                    }
                    if (IsPower(powerId, ChenvaalaPowerId) && HasBuddy(context.State, SnowElementalCardId))
                    {
                        InjectFrozenElemental(context, result);
                    }
                    if (IsPower(powerId, ChromieTwisterPowerId))
                    {
                        RefreshShopWithTavernSpells(context, result);
                    }
                    if (IsPower(powerId, SilasPowerId))
                    {
                        MarkSilasTickets(context, result);
                    }
                    if (IsPower(powerId, RatKingPowerId))
                    {
                        ResolvePigeonLordRefresh(context, result);
                    }
                    if (IsPower(powerId, RaynorPowerId))
                    {
                        AddBattlecruiserUpgradeToShop(context, result);
                    }
                    if (HasBuddy(context.State, SklibbCardId))
                    {
                        AddSklibbRefreshMinion(context, result);
                    }
                    break;
                case HeroEffectEventType.CardPlayed:
                    ResolveCardPlayedHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.TavernSpellCast:
                    ResolveTavernSpellCastHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.BattlecryTriggered:
                    ResolveBattlecryHeroEffects(context, result, powerId);
                    break;
                case HeroEffectEventType.MinionSold:
                    ResolveMinionSoldHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.FriendlyMinionDiedInCombat:
                    ResolveCombatMinionDiedHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.FriendlyMinionKilledEnemyInCombat:
                    ResolveCombatKillHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.FriendlyMinionAttackedInCombat:
                    ResolveCombatAttackHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.TurnStarted:
                    ResolveTurnStartedHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.TurnEnded:
                    ResolveTurnEndedHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.DiscoverChosen:
                    ResolveDiscoverChosenHeroPowers(context, result, powerId);
                    break;
                case HeroEffectEventType.CombatEnded:
                    ResolveCombatEndedHeroPowers(context, result, powerId);
                    break;
            }
        }

        private static void ResolveMatchStartedHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (IsPower(powerId, NzothPowerId) && context.State.Player.Board.Count < 7)
            {
                var fish = CreateGeneratedMinion(NzothFishCardId, "Fish of N'Zoth", 2, 2, Tribe.Beast, "nzoth-fish");
                AddTag(fish, "hero_derivative");
                AddTag(fish, "hero_derivative:nzoth");
                AddTag(fish, "fish_of_nzoth");
                context.State.Player.Board.Add(fish);
                result.Messages.Add("Avatar of N'Zoth: started with a 2/2 Fish.");
            }

            if (IsPower(powerId, SneedPowerId) && context.State.Player.Board.Count < 7)
            {
                var shredder = CreateGeneratedMinion(SneedShredderCardId, "Sneed's New Shredder", 2, 1, Tribe.Mech, "sneed-shredder");
                AddKeyword(shredder, Keyword.Deathrattle, "Sneed's New Shredder");
                AddTag(shredder, "hero_derivative");
                AddTag(shredder, "hero_derivative:sneed");
                AddTag(shredder, "sneed_shredder");
                AddTag(shredder, "deathrattle");
                context.State.Player.Board.Add(shredder);
                result.Messages.Add("Pilot the Shredder: started with a 2/1 Sneed's New Shredder.");
            }

            if (IsPower(powerId, FinleyPowerId))
            {
                StartHeroPowerDiscover(context, "hero-power:adventure");
                result.Messages.Add("Adventure!: started a Hero Power Discover.");
            }

            if (IsPower(powerId, RaynorPowerId))
            {
                AddStartingBattlecruiser(context, result);
            }

            if (IsPower(powerId, KerriganPowerTier2Id))
            {
                AddStartingLarva(context, result);
            }

            if (IsPower(powerId, ArtanisPowerId))
            {
                StartArtanisProtossDiscover(context, result);
            }

            if (!IsPower(powerId, CuratorPowerId) || context.State.Player.Board.Count >= 7)
            {
                return;
            }

            var amalgam = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-curator-amalgam-" + context.State.Round,
                DefinitionId = "curator-amalgam",
                CardId = CuratorAmalgamCardId,
                Name = "Amalgam",
                Cost = 3,
                BaseAttack = 2,
                BaseHealth = 2,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.All },
                Keywords = new List<Keyword> { Keyword.Venomous },
                OfficialKeywords = new List<Keyword> { Keyword.Venomous },
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string>
                {
                    "hero_derivative",
                    "hero_derivative:curator",
                    "curator_amalgam",
                    "all_minion_types"
                }
            };
            context.State.Player.Board.Add(amalgam);
            result.Messages.Add("Menagerist: started with a 2/2 Venomous Amalgam.");
        }

        private static void ResolveMinionSoldHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (context.Card == null)
            {
                return;
            }

            if (IsPower(powerId, GallywixPowerId))
            {
                context.State.Player.Tavern.NextTurnBonusGold += 1;
                result.Messages.Add("Smart Savings: banked 1 Gold for next turn.");
            }

            if (IsPower(powerId, FlurglPowerId) && context.Card.CardKind == CardKind.Minion)
            {
                var sold = IncrementCounter(context.State.Player.Tavern, FlurglSoldCounter, 1);
                if (sold >= 5)
                {
                    context.State.Player.Tavern.HeroEffectCounters[FlurglSoldCounter] = 0;
                    AddRandomTribeMinionToHand(context, Tribe.Murloc, "flurgl");
                    result.Messages.Add("Gone Fishing: gained a random Murloc.");
                }
            }

            if (IsPower(powerId, MillificentPowerId) && context.Card.Tribes.Contains(Tribe.Mech))
            {
                IncrementCounter(context.State.Player.Tavern, MillificentMechDeathCounter, 1);
            }

            if (IsPower(powerId, OzumatPowerId))
            {
                IncrementCounter(context.State.Player.Tavern, OzumatTentacleStatsCounter, 1);
                result.Messages.Add("Tentacular: future Tentacles gained +1/+1 from the sell proxy.");
            }

            if (IsPower(powerId, DerylPowerId))
            {
                PassDerylHats(context.State, context.Card, context.Card.Counters.TryGetValue("deryl_hats", out var hats) ? hats : 0, result);
            }
        }

        private static void ResolveCombatMinionDiedHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (context.Card == null)
            {
                return;
            }

            if (IsPower(powerId, MillificentPowerId) &&
                context.Card.Tribes.Contains(Tribe.Mech))
            {
                IncrementCounter(context.State.Player.Tavern, MillificentMechDeathCounter, 1);
            }

            if (IsPower(powerId, OzumatPowerId))
            {
                IncrementCounter(context.State.Player.Tavern, OzumatTentacleStatsCounter, 1);
                result.Messages.Add("Tentacular: future Tentacles gained +1/+1 from a combat death.");
            }

            if (IsPower(powerId, IniStormcoilPowerId))
            {
                var deaths = IncrementCounter(context.State.Player.Tavern, IniCombatFriendlyDeathsCounter, 1);
                if (deaths >= 9)
                {
                    context.State.Player.Tavern.HeroEffectCounters[IniCombatFriendlyDeathsCounter] = deaths - 9;
                    if (AddRandomMechToHand(context, "mechgyver"))
                    {
                        result.Messages.Add("MechGyver: gained a random Mech after friendly minions died.");
                    }
                }
            }
        }

        private static void ResolveCombatKillHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            var killCount = IncrementCombatKillCounter(context.State);
            if (IsPower(powerId, RokaraPowerId) && context.Card != null)
            {
                Buff(context.Card, 1, 0, "Glory of Combat");
                result.Messages.Add("Glory of Combat: gave the friendly killer +1 Attack permanently.");
            }

            if (IsPower(powerId, RafaamPowerId) &&
                GetCounterOrDefault(context.State.Player.Tavern, RafaamArmedCounter, 0) > 0)
            {
                if (context.TargetCard != null)
                {
                    AddPlainCopyToHand(context.State, context.TargetCard, "rafaam-kill-" + killCount, context.Minions);
                    context.State.Player.Tavern.HeroEffectCounters[RafaamArmedCounter] = 0;
                    result.Messages.Add("I'll Take That!: gained a plain copy of the first enemy killed.");
                }
                else
                {
                    result.Messages.Add("I'll Take That!: kill target data was unavailable, so no copy was created.");
                }
            }
        }

        private static void ResolveCombatAttackHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (IsPower(powerId, LohPowerId))
            {
                var tavern = context.State.Player.Tavern;
                var attacks = IncrementCounter(tavern, LohAttackCounter, Math.Max(1, context.GoldCost));
                while (attacks >= LohAttackThreshold)
                {
                    attacks -= LohAttackThreshold;
                    AddTripleRewardToHand(context);
                    result.Messages.Add("Heroic Inspiration: 15 friendly attacks granted a Triple Reward.");
                }

                tavern.HeroEffectCounters[LohAttackCounter] = attacks;
            }

            if (IsPower(powerId, ArannaPowerId) &&
                GetCounterOrDefault(context.State.Player.Tavern, ArannaFirstBuyFreeCounter, 0) <= 0)
            {
                var attacks = IncrementCounter(context.State.Player.Tavern, ArannaAttackCounter, Math.Max(1, context.GoldCost));
                if (attacks >= 7)
                {
                    context.State.Player.Tavern.HeroEffectCounters[ArannaAttackCounter] = 0;
                    context.State.Player.Tavern.HeroEffectCounters[ArannaFirstBuyFreeCounter] = 1;
                    result.Messages.Add("Demon Hunter Training: unlocked a free first minion buy.");
                }
            }
        }

        private static void RecordBarovPrediction(HeroEffectContext context, HeroEffectResult result)
        {
            var prediction = ParseBarovPrediction(context.ChoiceId);
            if (prediction == 0)
            {
                throw new InvalidOperationException("Friendly Wager needs choiceId win/player, loss/opponent, or draw.");
            }

            var tavern = context.State.Player.Tavern;
            tavern.HeroEffectCounters[BarovPredictionRoundCounter] = context.State.Round;
            tavern.HeroEffectCounters[BarovPredictionCounter] = prediction;
            result.Messages.Add("Friendly Wager: predicted " + BarovPredictionLabel(prediction) + ".");
        }

        private static void ResolveCombatEndedHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (!IsPower(powerId, BarovPowerId))
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            var pendingRound = GetCounterOrDefault(tavern, BarovPredictionRoundCounter, 0);
            var prediction = GetCounterOrDefault(tavern, BarovPredictionCounter, 0);
            if (pendingRound != context.State.Round || prediction == 0)
            {
                return;
            }

            tavern.HeroEffectCounters[BarovPredictionRoundCounter] = 0;
            tavern.HeroEffectCounters[BarovPredictionCounter] = 0;
            var actual = ParseBarovPrediction(context.ChoiceId);
            if (actual == 0)
            {
                return;
            }

            if (prediction != actual)
            {
                result.Messages.Add("Friendly Wager: prediction missed.");
                return;
            }

            for (var index = 0; index < 3; index += 1)
            {
                AddTavernCoinToHand(context, "barov");
            }

            result.Messages.Add("Friendly Wager: prediction hit and added 3 Tavern Coins.");
        }

        private static int ParseBarovPrediction(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            if (string.Equals(value, "win", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "player", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, CombatWinner.Player.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(value, "loss", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "lose", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "opponent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, CombatWinner.Opponent.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return string.Equals(value, "draw", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, CombatWinner.Draw.ToString(), StringComparison.OrdinalIgnoreCase)
                ? 3
                : 0;
        }

        private static string BarovPredictionLabel(int prediction)
        {
            switch (prediction)
            {
                case 1:
                    return "win";
                case 2:
                    return "loss";
                case 3:
                    return "draw";
                default:
                    return "unknown";
            }
        }

        private static void UseHeroPower(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (IsPower(powerId, ForestLordCenariusPowerId))
            {
                SpendGold(context.State.Player.Tavern, 3);
                IncrementCounter(context.State.Player.Tavern, MaxGoldBonusCounter, 1);
                TavernRules.IncreaseMaxGold(context.State.Player.Tavern, 1);
                result.Messages.Add("Hero Power: maximum Gold increased by 1.");
                UpdateMalorneStats(context.State);
            }

            if (IsPower(powerId, EdwinPowerId))
            {
                var board = context.State.Player.Board;
                if ((context.TargetZone != TargetZone.Unspecified && context.TargetZone != TargetZone.FriendlyBoard) ||
                    context.TargetIndex < 0 || context.TargetIndex >= board.Count)
                {
                    throw new InvalidOperationException("必须选择一个友方随从作为目标。");
                }

                var tavern = context.State.Player.Tavern;
                SpendGold(tavern, 1);
                var amount = GetCounterOrDefault(tavern, EdwinBuffAmountCounter, 1);
                Buff(board[context.TargetIndex], amount, amount, "Sharpen Blades");
                result.Messages.Add("Sharpen Blades: gave a minion +" + amount + "/+" + amount + ".");
            }

            if (IsPower(powerId, GeorgePowerId))
            {
                var board = context.State.Player.Board;
                if ((context.TargetZone != TargetZone.Unspecified && context.TargetZone != TargetZone.FriendlyBoard) ||
                    context.TargetIndex < 0 || context.TargetIndex >= board.Count)
                {
                    throw new InvalidOperationException("必须选择一个友方随从作为目标。");
                }

                SpendGold(context.State.Player.Tavern, 1);
                AddKeyword(board[context.TargetIndex], Keyword.DivineShield, "Boon of Light");
                result.Messages.Add("Boon of Light: gave a minion Divine Shield.");
            }

            if (IsPower(powerId, RafaamPowerId))
            {
                SpendGold(context.State.Player.Tavern, 1);
                context.State.Player.Tavern.HeroEffectCounters[RafaamArmedCounter] = 1;
                result.Messages.Add("I'll Take That!: armed the next combat kill copy.");
            }

            if (IsPower(powerId, JaraxxusPowerId))
            {
                SpendGold(context.State.Player.Tavern, 1);
                var buffed = 0;
                foreach (var demon in context.State.Player.Board.Where(card => card.Tribes.Contains(Tribe.Demon)))
                {
                    Buff(demon, 1, 1, "Bloodfury");
                    buffed += 1;
                }

                result.Messages.Add("Bloodfury: gave " + buffed + " friendly Demon(s) +1/+1.");
            }

            if (IsPower(powerId, KraggPowerId))
            {
                var tavern = context.State.Player.Tavern;
                if (GetCounterOrDefault(tavern, KraggUsedCounter, 0) > 0)
                {
                    throw new InvalidOperationException("Piggy Bank has already been used this game.");
                }

                var goldGained = Math.Max(2, context.State.Round + 1);
                TavernRules.GainGold(tavern, goldGained);
                tavern.HeroEffectCounters[KraggUsedCounter] = 1;
                result.Messages.Add("Piggy Bank: gained " + goldGained + " Gold.");
            }

            if (IsPower(powerId, FarseerNobundoPowerId))
            {
                var tavern = context.State.Player.Tavern;
                var discount = Math.Min(3, Math.Max(0, GetCounterOrDefault(tavern, NobundoHeroPowerDiscountCounter, 0)));
                SpendGold(tavern, Math.Max(0, 3 - discount));
                tavern.HeroEffectCounters[NobundoHeroPowerDiscountCounter] = 0;
                if (AddLastTavernSpellCopyToHand(context, "nobundo"))
                {
                    result.Messages.Add("The Galaxy's Lens: copied your last Tavern spell.");
                }
                else
                {
                    result.Messages.Add("The Galaxy's Lens: no Tavern spell to copy.");
                }
            }

            if (IsPower(powerId, DoctorHollidaePowerId))
            {
                SpendGold(context.State.Player.Tavern, 1);
                if (AddRandomTavernSpellToHand(context, 1, Math.Max(1, context.State.Player.Tavern.Tier), "hollidae"))
                {
                    result.Messages.Add("Blessing of the Nine Frogs: gained a random Tavern spell.");
                }
            }

            if (IsPower(powerId, SnakeEyesPowerId))
            {
                var tavern = context.State.Player.Tavern;
                var readyRound = GetCounterOrDefault(tavern, SnakeEyesReadyRoundCounter, 0);
                if (context.State.Round < readyRound)
                {
                    throw new InvalidOperationException("Lucky Roll is cooling down until turn " + readyRound + ".");
                }

                SpendGold(tavern, 1);
                var roll = RollSixSidedDie(context);
                TavernRules.GainGold(tavern, roll);
                tavern.HeroEffectCounters[SnakeEyesLastRollCounter] = roll;
                tavern.HeroEffectCounters[SnakeEyesReadyRoundCounter] = context.State.Round + roll;
                result.Messages.Add("Lucky Roll: rolled " + roll + " and gained " + roll + " Gold.");
            }

            if (IsPower(powerId, BlackthornPowerId))
            {
                var tavern = context.State.Player.Tavern;
                tavern.HeroEffectCounters.TryGetValue(BlackthornRoundCounter, out var round);
                var uses = round == context.State.Round ? GetCounterOrDefault(tavern, BlackthornUsesCounter, 0) : 0;

                SpendGold(tavern, 1);
                tavern.HeroEffectCounters[BlackthornRoundCounter] = context.State.Round;
                tavern.HeroEffectCounters[BlackthornUsesCounter] = uses + 1;
                AddBloodGemsToHand(context, 2, "blackthorn");
                result.Messages.Add("Bloodbound: gained 2 Blood Gems.");
            }

            if (IsPower(powerId, PutricidePowerId))
            {
                UsePutricideHeroPower(context, result);
            }

            if (IsPower(powerId, KerriganPowerTier2Id) ||
                IsPower(powerId, KerriganPowerTier3Id) ||
                IsPower(powerId, KerriganPowerFinalId))
            {
                UseKerriganHeroPower(context, result, powerId);
            }

            if (IsPower(powerId, LichBazHialPowerId))
            {
                var tavern = context.State.Player.Tavern;
                if (context.TargetIndex < 0 || context.TargetIndex >= tavern.Shop.Count || tavern.Shop[context.TargetIndex] == null)
                {
                    throw new InvalidOperationException("Graveyard Shift needs a Tavern card target.");
                }

                if (tavern.Hand.Count >= HandLimit)
                {
                    throw new InvalidOperationException("Hand is full.");
                }

                SpendGold(tavern, 2);
                var stolen = tavern.Shop[context.TargetIndex];
                tavern.Shop[context.TargetIndex] = null;
                stolen.Owner = BoardSide.Player;
                stolen.PoolSource = PoolSource.Copy;
                stolen.OriginPoolSource = PoolSource.Copy;
                stolen.PoolCopiesHeld = 0;
                tavern.Hand.Add(stolen);
                DamageHeroWithUnderlingRewind(context.State, 2, result);
                result.Messages.Add("Graveyard Shift: stole a Tavern card.");
            }

            if (IsPower(powerId, RakanishuPowerId))
            {
                SpendGold(context.State.Player.Tavern, 1);
                if (AddLanternLightToHand(context))
                {
                    result.Messages.Add("Tavern Lighting: gained a Lantern Light.");
                }
            }

            if (IsPower(powerId, RenoPowerId))
            {
                var board = context.State.Player.Board;
                if (context.TargetIndex < 0 || context.TargetIndex >= board.Count)
                {
                    throw new InvalidOperationException("Gonna Be Rich! needs a friendly minion target.");
                }

                var tavern = context.State.Player.Tavern;
                if (GetCounterOrDefault(tavern, RenoUsedCounter, 0) > 0)
                {
                    throw new InvalidOperationException("Gonna Be Rich! has already been used this game.");
                }

                MakeGoldenInPlace(board[context.TargetIndex], context.Minions);
                tavern.HeroEffectCounters[RenoUsedCounter] = 1;
                result.Messages.Add("Gonna Be Rich!: made a friendly minion Golden.");
            }

            if (IsPower(powerId, PatchesPowerId))
            {
                var tavern = context.State.Player.Tavern;
                var discount = Math.Min(3, Math.Max(0, GetCounterOrDefault(tavern, PatchesDiscountCounter, 0)));
                SpendGold(tavern, Math.Max(0, 3 - discount));
                tavern.HeroEffectCounters[PatchesDiscountCounter] = 0;
                AddRandomTribeMinionToHand(context, Tribe.Pirate, "patches");
                result.Messages.Add("Pirate Parrrrty!: gained a Pirate.");
            }

            if (IsPower(powerId, RatKingPowerId))
            {
                SpendGold(context.State.Player.Tavern, 1);
                StartRatKingCurrentTribeDiscover(context, result);
            }

            if (IsPower(powerId, BarovPowerId))
            {
                SpendGold(context.State.Player.Tavern, 1);
                RecordBarovPrediction(context, result);
            }

            if (IsPower(powerId, HolmesPowerId))
            {
                SpendGold(context.State.Player.Tavern, 1);
                StartHolmesGuessDiscover(context, result);
            }

            if (IsPower(powerId, CaptainEudoraPowerId))
            {
                var tavern = context.State.Player.Tavern;
                SpendGold(tavern, 1);
                var digs = IncrementCounter(tavern, EudoraDigCounter, 1);
                if (digs >= 4)
                {
                    tavern.HeroEffectCounters[EudoraDigCounter] = 0;
                    if (AddRandomGoldenMinionToHand(context, "eudora"))
                    {
                        result.Messages.Add("Buried Treasure: found a Golden minion.");
                    }
                }
                else
                {
                    result.Messages.Add("Buried Treasure: dug " + digs + " times.");
                }
            }

            if (IsPower(powerId, ElisePowerId))
            {
                var tavern = context.State.Player.Tavern;
                var extraCost = Math.Max(0, GetCounterOrDefault(tavern, EliseCostIncreaseCounter, 0));
                SpendGold(tavern, 1 + extraCost);
                StartCurrentTierMinionDiscover(context, "hero-power:lead-explorer");
                tavern.HeroEffectCounters[EliseCostIncreaseCounter] = extraCost + 1;
                result.Messages.Add("Lead Explorer: started a current-tier minion Discover.");
            }

            if (IsPower(powerId, MillificentPowerId))
            {
                var tavern = context.State.Player.Tavern;
                if (tavern.Tier < 4)
                {
                    throw new InvalidOperationException("Tinker unlocks at Tavern Tier 4.");
                }

                SpendGold(tavern, 1);
                StartMagneticMechDiscover(context, "hero-power:tinker");
                result.Messages.Add("Tinker: started a Magnetic Mech Discover.");
            }

            if (IsPower(powerId, LichKingPowerId))
            {
                var target = GetFriendlyBoardTarget(context, "Reborn Rites needs a friendly minion target.");
                AddKeyword(target, Keyword.Reborn, LichKingRebornSource);
                AddTag(target, "temporary_reborn_rites");
                result.Messages.Add("Reborn Rites: gave a minion Reborn until next turn.");
            }

            if (IsPower(powerId, ShudderwockPowerId))
            {
                if (context.State.Round < 3)
                {
                    throw new InvalidOperationException("Snicker-snack unlocks on Turn 3.");
                }

                var target = GetFriendlyBoardTarget(context, "Snicker-snack needs a friendly minion target.");
                if (!IsBattlecryMinion(target))
                {
                    throw new InvalidOperationException("Snicker-snack needs a friendly Battlecry minion target.");
                }

                if (context.BattlecryResolver == null)
                {
                    throw new InvalidOperationException("Battlecry replay resolver is not available.");
                }

                var replay = context.BattlecryResolver(new HeroBattlecryReplayRequest
                {
                    Source = target,
                    RepeatCount = 1,
                    TargetIndex = context.SecondaryTargetIndex,
                    TargetZone = context.SecondaryTargetZone,
                    TargetInstanceId = context.SecondaryTargetInstanceId
                });
                result.Messages.Add("Snicker-snack: triggered " + target.Name + "'s Battlecry.");
                if (replay != null)
                {
                    foreach (var message in replay.Messages)
                    {
                        result.Messages.Add(message);
                    }
                }
            }

            if (IsPower(powerId, JandicePowerId))
            {
                SwapFriendlyMinionWithShop(context, result);
            }

            if (IsPower(powerId, MutanusPowerId))
            {
                DevourFriendlyMinion(context, result);
            }

            if (IsPower(powerId, XyrellaPowerId))
            {
                var tavern = context.State.Player.Tavern;
                SpendGold(tavern, 2);
                var target = GetShopTarget(context, "See the Light needs a Tavern minion target.");
                if (tavern.Hand.Count >= HandLimit)
                {
                    throw new InvalidOperationException("Hand is full.");
                }

                tavern.Shop[context.TargetIndex] = null;
                target.Owner = BoardSide.Player;
                target.PoolSource = PoolSource.Copy;
                target.OriginPoolSource = PoolSource.Copy;
                target.PoolCopiesHeld = 0;
                SetStats(target, 2, 2, "See the Light");
                tavern.Hand.Add(target);
                result.Messages.Add("See the Light: set a Tavern minion to 2/2 and added it to hand.");
            }

            if (IsPower(powerId, PyramadPowerId))
            {
                var tavern = context.State.Player.Tavern;
                SpendGold(tavern, 2);
                var target = PickShopMinion(context, "Brick by Brick needs a Tavern minion.");
                if (tavern.Hand.Count >= HandLimit)
                {
                    throw new InvalidOperationException("Hand is full.");
                }

                var healthGain = Math.Max(0, target.MaxHealth);
                tavern.Shop[tavern.Shop.FindIndex(card => card != null && card.InstanceId == target.InstanceId)] = null;
                target.Owner = BoardSide.Player;
                target.PoolSource = PoolSource.Copy;
                target.OriginPoolSource = PoolSource.Copy;
                target.PoolCopiesHeld = 0;
                Buff(target, 0, healthGain, "Brick by Brick");
                tavern.Hand.Add(target);
                NotifyTitanicGuardian(context.State, healthGain, target.InstanceId, result);
                result.Messages.Add("Brick by Brick: stole a Tavern minion and doubled its Health.");
            }

            if (IsPower(powerId, VoljinPowerId))
            {
                ResolveSpiritSwap(context, result);
            }

            if (IsPower(powerId, IngePowerId))
            {
                var target = GetFriendlyBoardTarget(context, "Major Hymn needs a friendly minion target.");
                var tavern = context.State.Player.Tavern;
                var healthMode = GetCounterOrDefault(tavern, IngeModeCounter, 0) == 1;
                var amount = Math.Max(1, tavern.Tier);
                if (healthMode)
                {
                    Buff(target, 0, amount, "Major Hymn");
                    NotifyTitanicGuardian(context.State, amount, target.InstanceId, result);
                    result.Messages.Add("Major Hymn: gave a minion Health equal to your Tier.");
                }
                else
                {
                    Buff(target, amount, 0, "Major Hymn");
                    result.Messages.Add("Major Hymn: gave a minion Attack equal to your Tier.");
                }

                tavern.HeroEffectCounters["hero:inge:last_health_mode"] = healthMode ? 1 : 0;
                tavern.HeroEffectCounters[IngeModeCounter] = healthMode ? 0 : 1;
            }

            if (IsPower(powerId, MalygosPowerId))
            {
                ResolveMalygosHeroPower(context, result);
            }

            if (IsPower(powerId, MaievPowerId))
            {
                ResolveMaievHeroPower(context, result);
            }

            if (IsPower(powerId, ZephrysPowerId))
            {
                ResolveZephrysHeroPower(context, result);
            }

            if (IsPower(powerId, HooktuskPowerId))
            {
                ResolveHooktuskHeroPower(context, result);
            }

            if (IsPower(powerId, ZerekPowerId))
            {
                ResolveZerekHeroPower(context, result);
            }

            if (IsPower(powerId, TogwagglePowerId))
            {
                ResolveTogwaggleHeroPower(context, result);
            }

            if (IsPower(powerId, TessPowerId))
            {
                ResolveTessHeroPower(context, result);
            }

            if (IsPower(powerId, ScabbsPowerId))
            {
                ResolveScabbsHeroPower(context, result);
            }

            if (IsPower(powerId, TeronPowerId))
            {
                var target = GetFriendlyBoardTarget(context, "Rapid Reanimation needs a friendly minion target.");
                foreach (var minion in context.State.Player.Board)
                {
                    minion.Tags.Remove(TeronTargetTag);
                }

                AddTag(target, TeronTargetTag);
                result.Messages.Add("Rapid Reanimation: marked a friendly minion for start-of-combat reanimation.");
            }

            if (IsPower(powerId, JailerPowerId))
            {
                var target = GetFriendlyBoardTarget(context, "Runic Empowerment needs a friendly minion target.");
                SpendGold(context.State.Player.Tavern, 1);
                var deaths = Math.Max(0, context.State.Player.Tavern.FriendlyMinionDeathsThisGame);
                var stored = Math.Max(0, GetCounterOrDefault(context.State.Player.Tavern, JailerHealthBonusCounter, 0));
                var health = 1 + Math.Max(stored, deaths / 5);
                context.State.Player.Tavern.HeroEffectCounters[JailerHealthBonusCounter] = health - 1;
                Buff(target, 1, health, "Runic Empowerment");
                result.Messages.Add("Runic Empowerment: gave a minion +1/+" + health + ".");
            }

            if (IsPower(powerId, CookiePowerId))
            {
                ResolveCookieHeroPower(context, result);
            }

            if (IsPower(powerId, GalakrondPowerId))
            {
                ResolveGalakrondHeroPower(context, result);
            }

            if (IsPower(powerId, EtcPowerId))
            {
                ResolveEtcHeroPower(context, result);
            }
        }

        private static void ResolveCardPlayedHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (context.Card == null)
            {
                return;
            }

            ResolveEarlyBattlecryHeroEffects(context, result, powerId);

            if (context.Card.Tribes.Contains(Tribe.Mech))
            {
                foreach (var buddy in MatchingBoardBuddies(context.State, SubScrubberCardId))
                {
                    Buff(buddy, 3, 3, "Sub Scrubber");
                    result.Messages.Add("Sub Scrubber: gained +3/+3 after you played a Mech.");
                }
            }

            if (IsPower(powerId, JandicePowerId) && context.Card.CardKind == CardKind.Minion)
            {
                var key = "hero:jandice:played:" + context.State.Round + ":" + context.Card.CardId;
                var played = IncrementCounter(context.State.Player.Tavern, key, 1);
                if (played > 1 && HasBuddy(context.State, JandiceApprenticeCardId))
                {
                    var amount = Math.Max(1, context.State.Player.Tavern.Tier);
                    foreach (var minion in context.State.Player.Board.Where(card => card != null && card.CardKind == CardKind.Minion))
                    {
                        Buff(minion, amount, amount, "Jandice's Apprentice");
                    }

                    result.Messages.Add("Jandice's Apprentice: your minions gained stats equal to your Tier.");
                }
            }

            if (context.Card.CardKind == CardKind.Minion && HasBuddy(context.State, BabyElekkCardId))
            {
                foreach (var buddy in MatchingBoardBuddies(context.State, BabyElekkCardId))
                {
                    if (context.Card.InstanceId == buddy.InstanceId || context.Card.Attack >= buddy.Attack)
                    {
                        continue;
                    }

                    var attack = GetCounterOrDefault(context.State.Player.Tavern, "hero:xyrella:baby_elekk_attack", 2);
                    var health = GetCounterOrDefault(context.State.Player.Tavern, "hero:xyrella:baby_elekk_health", 2);
                    Buff(context.Card, attack, health, "Baby Elekk");
                    NotifyTitanicGuardian(context.State, health, context.Card.InstanceId, result);
                    context.State.Player.Tavern.HeroEffectCounters["hero:xyrella:baby_elekk_attack"] = attack + 1;
                    context.State.Player.Tavern.HeroEffectCounters["hero:xyrella:baby_elekk_health"] = health + 1;
                    result.Messages.Add("Baby Elekk: buffed a lower-Attack played minion and improved.");
                }
            }

            if (IsPower(powerId, ChenvaalaPowerId) && context.Card.Tribes.Contains(Tribe.Elemental))
            {
                var played = IncrementCounter(context.State.Player.Tavern, ChenvaalaElementalCounter, 1);
                if (played >= 3)
                {
                    context.State.Player.Tavern.HeroEffectCounters[ChenvaalaElementalCounter] = 0;
                    context.State.Player.Tavern.UpgradeCost = Math.Max(0, context.State.Player.Tavern.UpgradeCost - 3);
                    result.Messages.Add("Avalanche: Tavern upgrade costs 3 less.");
                }
            }

            if (IsPower(powerId, DerylPowerId) && context.Card.CardKind == CardKind.Minion)
            {
                AddDerylHats(context.Card, 1, "Hat Trick");
                result.Messages.Add("Hat Trick: played minion gained a +1/+1 hat.");
            }

            ResolveKerriganCardPlayed(context, result, powerId);
            ResolveLateBattlecryHeroEffects(context, result);
            TryStartAzsharaNagaConquest(context, result, powerId);
        }

        private static void ResolveTavernSpellCastHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            TryStartAzsharaNagaConquest(context, result, powerId);
        }

        private static void ResolveBattlecryHeroEffects(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            ResolveEarlyBattlecryHeroEffects(context, result, powerId);
            ResolveLateBattlecryHeroEffects(context, result);
        }

        private static void ResolveEarlyBattlecryHeroEffects(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (context.Card == null)
            {
                return;
            }

            if (IsPower(powerId, FlurglPowerId) && context.Card.CardId == SparkfinSoothsayerCardId)
            {
                TransformShopMinionsToTribe(context, Tribe.Murloc);
                result.Messages.Add("Sparkfin Soothsayer: transformed Tavern minions into same-tier Murlocs.");
            }

            if (string.Equals(context.Card.CardId, TuskarrRaiderCardId, StringComparison.OrdinalIgnoreCase))
            {
                AddRandomBountyToHand(context, "tuskarr-battlecry");
                result.Messages.Add("Tuskarr Raider: gained a random Bounty.");
            }

            if (string.Equals(context.Card.CardId, JrNavigatorCardId, StringComparison.OrdinalIgnoreCase))
            {
                var tavern = context.State.Player.Tavern;
                var extraCost = Math.Max(0, GetCounterOrDefault(tavern, EliseCostIncreaseCounter, 0) - 2);
                tavern.HeroEffectCounters[EliseCostIncreaseCounter] = extraCost;
                result.Messages.Add("Jr. Navigator: reduced Lead Explorer's Cost by 2.");
            }

            if (string.Equals(context.Card.CardId, MuckslingerCardId, StringComparison.OrdinalIgnoreCase))
            {
                AddRandomBattlecryMinionToHand(context, "muckslinger");
                result.Messages.Add("Muckslinger: gained a random Battlecry minion.");
            }

            if (string.Equals(context.Card.CardId, BabyNzothCardId, StringComparison.OrdinalIgnoreCase))
            {
                var target = PickTargetedOrFirstOtherBoardMinion(context, card => IsDeathrattleMinion(card));
                if (target != null)
                {
                    MakeGoldenInPlace(target, context.Minions);
                    result.Messages.Add("Baby N'Zoth: made a friendly Deathrattle minion Golden.");
                }
            }

            if (string.Equals(context.Card.CardId, NathanosCardId, StringComparison.OrdinalIgnoreCase))
            {
                ResolveNathanosBattlecry(context, result);
            }
        }

        private static void ResolveLateBattlecryHeroEffects(HeroEffectContext context, HeroEffectResult result)
        {
            if (context.Card == null)
            {
                return;
            }

            if (string.Equals(context.Card.CardId, MiniZerekCardId, StringComparison.OrdinalIgnoreCase))
            {
                TransformIntoSelectedShopMinion(context.Card, context);
                result.Messages.Add("Mini-Zerek: transformed into the selected Tavern minion.");
            }

            if (string.Equals(context.Card.CardId, ShadowWardenCardId, StringComparison.OrdinalIgnoreCase))
            {
                context.State.Player.Tavern.HeroEffectCounters[MaievNextGoldenCounter] = 1;
                result.Messages.Add("Shadow Warden: your next Imprison makes the target Golden.");
            }

            if (string.Equals(context.Card.CardId, WaxadredCardId, StringComparison.OrdinalIgnoreCase))
            {
                RefreshShopFromOpponentHighestTier(context, result);
            }

            if (string.Equals(context.Card.CardId, GalakrondApostleCardId, StringComparison.OrdinalIgnoreCase))
            {
                ReplaceShopCardsOneTierHigher(context, result);
            }

            if (string.Equals(context.Card.CardId, TalentScoutCardId, StringComparison.OrdinalIgnoreCase))
            {
                MakeTargetBuddyGolden(context, result);
            }
        }

        private static void ResolveCardBoughtHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            var card = context.Card;
            if (card == null)
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            if (IsPower(powerId, ArannaPowerId) &&
                card.CardKind == CardKind.Minion &&
                GetCounterOrDefault(tavern, ArannaFirstBuyFreeCounter, 0) > 0)
            {
                tavern.HeroEffectCounters[ArannaFirstBuyFreeCounter] = 0;
                result.Messages.Add("Demon Hunter Training: consumed the free minion buy.");
            }

            if (IsPower(powerId, KaelthasPowerId) && card.CardKind == CardKind.Minion)
            {
                var bought = IncrementCounter(tavern, KaelthasBoughtCounter, 1);
                if (bought >= 3)
                {
                    tavern.HeroEffectCounters[KaelthasBoughtCounter] = 0;
                    AddTavernCoinToHand(context, "kaelthas");
                    result.Messages.Add("Verdant Spheres: gained a Tavern Coin.");
                    foreach (var buddy in MatchingBoardBuddies(context.State, CrimsonHandCenturionCardId))
                    {
                        Buff(buddy, Math.Max(0, card.Attack), Math.Max(0, card.MaxHealth), "Crimson Hand Centurion");
                        result.Messages.Add("Crimson Hand Centurion gained the bought minion's stats.");
                    }
                }
            }

            if (IsPower(powerId, ExarchOthaarPowerId) && context.State.Round >= 3 && card.CardKind == CardKind.TavernSpell)
            {
                result.Messages.Add("Arcane Knowledge: next Tavern spell discount consumed.");
            }

            if (IsPower(powerId, TaethelanPowerId) && card.CardKind == CardKind.TavernSpell)
            {
                IncrementCounter(tavern, TaethelanSpellCounter, 1);
                if (context.GoldCost == 0)
                {
                    result.Messages.Add("Reliquary Research: bought Tavern spell cost 0.");
                }
            }

            if (IsPower(powerId, CapnHoggarrPowerId) && card.Tribes.Contains(Tribe.Pirate))
            {
                TavernRules.GainGold(tavern, 1);
                result.Messages.Add("Cap'n Hoggarr: gained 1 Gold from buying a Pirate.");
            }

            if (IsPower(powerId, PatchesPowerId) && card.Tribes.Contains(Tribe.Pirate))
            {
                var discount = Math.Min(3, GetCounterOrDefault(tavern, PatchesDiscountCounter, 0) + 1);
                tavern.HeroEffectCounters[PatchesDiscountCounter] = discount;
                result.Messages.Add("Pirate Parrrrty!: next Hero Power costs " + discount + " less.");
            }

            if (IsPower(powerId, KurtrusPowerId) && card.CardKind == CardKind.Minion)
            {
                RecordKurtrusMinionBought(context, result, card);
            }

            if (IsPower(powerId, SaurfangPowerId) && card.CardKind == CardKind.Minion)
            {
                var bought = IncrementCounter(tavern, SaurfangBoughtCounter, 1);
                if (bought >= 4)
                {
                    tavern.HeroEffectCounters[SaurfangBoughtCounter] = 0;
                    var bonus = GetCounterOrDefault(tavern, SaurfangHealthBonusCounter, 1) + 1;
                    tavern.HeroEffectCounters[SaurfangHealthBonusCounter] = bonus;
                    ApplySaurfangShopBuff(context.State);
                    result.Messages.Add("For the Horde!: Tavern Health bonus improved to +" + bonus + ".");
                }
            }

            if (IsPower(powerId, EdwinPowerId))
            {
                var bought = IncrementCounter(tavern, EdwinBoughtCounter, 1);
                if (bought >= 5)
                {
                    tavern.HeroEffectCounters[EdwinBoughtCounter] = 0;
                    var amount = GetCounterOrDefault(tavern, EdwinBuffAmountCounter, 1) + 1;
                    tavern.HeroEffectCounters[EdwinBuffAmountCounter] = amount;
                    result.Messages.Add("Sharpen Blades improved to +" + amount + "/+" + amount + ".");
                }
            }

            if (IsPower(powerId, RagnarosPowerId))
            {
                var bought = IncrementCounter(tavern, RagnarosBoughtCounter, 1);
                if (bought >= 16 && GetCounterOrDefault(tavern, RagnarosUnlockedCounter, 0) == 0)
                {
                    tavern.HeroEffectCounters[RagnarosUnlockedCounter] = 1;
                    result.Messages.Add("BUY, INSECT!: Sulfuras unlocked; end of turn buffs are active.");
                }
            }

            if (IsPower(powerId, SilasPowerId) && card.Tags.Contains(SilasTicketTag))
            {
                var tickets = IncrementCounter(tavern, SilasTicketCounter, 1);
                if (tickets >= 3)
                {
                    tavern.HeroEffectCounters[SilasTicketCounter] = 0;
                    StartCurrentTierMinionDiscover(context, "hero-power:come-one-come-all");
                    result.Messages.Add("Come One, Come All!: collected 3 Tickets and started a minion Discover.");
                }
                else
                {
                    result.Messages.Add("Come One, Come All!: collected Darkmoon Ticket " + tickets + "/3.");
                }
            }

            if (IsPower(powerId, DinotamerBrannPowerId) &&
                card.CardKind == CardKind.Minion &&
                IsBattlecryMinion(card) &&
                GetCounterOrDefault(tavern, DinotamerBrannGrantedCounter, 0) == 0)
            {
                var bought = IncrementCounter(tavern, DinotamerBrannBoughtCounter, 1);
                if (bought >= 4 && AddMinionByCardIdToHand(context, BrannBronzebeardCardId, "dinotamer-brann"))
                {
                    tavern.HeroEffectCounters[DinotamerBrannGrantedCounter] = 1;
                    result.Messages.Add("Battle Brand: gained Brann Bronzebeard after buying 4 Battlecry minions.");
                }
            }

            if (IsPower(powerId, ArtanisPowerId))
            {
                AdvanceArtanisBuyReward(context, result);
            }
        }

        private static void ResolveDiscoverChosenHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (context.Card == null || context.Card.CardKind != CardKind.Minion)
            {
                return;
            }

            if (IsPower(powerId, HolmesPowerId) &&
                string.Equals(context.DiscoverSource, HolmesDiscoverSource, StringComparison.OrdinalIgnoreCase))
            {
                if (context.Card.Tags != null && context.Card.Tags.Contains(HolmesCorrectGuessTag))
                {
                    AddTavernCoinToHand(context, "murloc-holmes");
                    if (HasBuddy(context.State, WatfinCardId))
                    {
                        AddPlainCopyToHand(context.State, context.Card, "watfin", context.Minions);
                    }

                    result.Messages.Add("Detective for Hire: correct guess rewarded a Tavern Coin.");
                }
                else
                {
                    result.Messages.Add("Detective for Hire: guess missed.");
                }

                return;
            }

            foreach (var burth in MatchingBoardBuddies(context.State, BurthCardId))
            {
                var amount = Math.Max(1, GetCounterOrDefault(context.State.Player.Tavern, BurthBuffCounter, 1));
                Buff(context.Card, amount, amount, "Burth");
                context.State.Player.Tavern.HeroEffectCounters[BurthBuffCounter] = amount + 1;
                result.Messages.Add("Burth: discovered minion gained +" + amount + "/+" + amount + " and Burth improved.");
            }
        }

        private static void ResolveTurnStartedHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            ClearTemporaryRebornRites(context.State);
            ClearVoljinTemporarySwaps(context.State);

            if (IsPower(powerId, MillificentPowerId))
            {
                context.State.Player.Tavern.HeroEffectCounters[MillificentMechDeathCounter] = 0;
            }

            if (IsPower(powerId, BigglesworthPowerId))
            {
                StartBigglesworthEliminatedWarbandDiscover(context, result);
            }

            if (IsPower(powerId, RatKingPowerId))
            {
                RotateRatKingTribe(context, result);
            }

            if (IsPower(powerId, NozdormuPowerId))
            {
                context.State.Player.Tavern.FreeRefreshes += 1;
                context.State.Player.Tavern.HelpfulRefreshes += HasBuddy(context.State, ChromieCardId) ? 1 : 0;
                result.Messages.Add("Clairvoyance: gained a free Refresh.");
            }

            if (IsPower(powerId, ExarchOthaarPowerId) && context.State.Round >= 3)
            {
                context.State.Player.Tavern.NextTavernSpellCostReduction = Math.Max(context.State.Player.Tavern.NextTavernSpellCostReduction, 1);
                result.Messages.Add("Arcane Knowledge: next Tavern spell costs 1 less.");
            }

            if (IsPower(powerId, FarseerNobundoPowerId))
            {
                var discount = Math.Min(3, GetCounterOrDefault(context.State.Player.Tavern, NobundoHeroPowerDiscountCounter, 0) + 1);
                context.State.Player.Tavern.HeroEffectCounters[NobundoHeroPowerDiscountCounter] = discount;
                result.Messages.Add("The Galaxy's Lens: next Hero Power costs " + discount + " less.");
            }

            if (IsPower(powerId, KingMuklaPowerId))
            {
                var added = AddBananasToHand(context, 2, "mukla");
                if (added > 0)
                {
                    result.Messages.Add("Bananarama: gained " + added + " Bananas.");
                }
            }

            if (IsPower(powerId, TogwagglePowerId))
            {
                var discount = Math.Min(11, GetCounterOrDefault(context.State.Player.Tavern, TogwaggleDiscountCounter, 0) + 1);
                context.State.Player.Tavern.HeroEffectCounters[TogwaggleDiscountCounter] = discount;
                result.Messages.Add("The Perfect Crime: next Hero Power costs " + discount + " less.");
            }

            if (IsPower(powerId, KerriganPowerTier2Id) ||
                IsPower(powerId, KerriganPowerTier3Id) ||
                IsPower(powerId, KerriganPowerFinalId))
            {
                ReduceKerriganHeroPowerCost(context, result, powerId);
                StartKerriganMorphDiscovers(context, result);
            }

            TryStartAzsharaNagaConquest(context, result, powerId);
        }

        private static void ResolveTurnEndedHeroPowers(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (IsPower(powerId, VoonePowerId))
            {
                var turns = IncrementCounter(context.State.Player.Tavern, VooneHeroCounter, 1);
                if (turns >= 3)
                {
                    context.State.Player.Tavern.HeroEffectCounters[VooneHeroCounter] = 0;
                    CopyLeftmostHandCard(context.State, "voone", context.Minions);
                    result.Messages.Add("Upbeat Harmony: copied the left-most card in your hand.");
                }
            }

            if (IsPower(powerId, RagnarosPowerId) &&
                GetCounterOrDefault(context.State.Player.Tavern, RagnarosUnlockedCounter, 0) > 0)
            {
                var repetitions = HasBuddy(context.State, LucifronCardId) ? 2 : 1;
                for (var index = 0; index < repetitions; index += 1)
                {
                    BuffLeftAndRightMostMinions(context.State, 3, 3, "Sulfuras");
                }

                result.Messages.Add("Sulfuras: buffed your left and right-most minions.");
            }

            if (IsPower(powerId, SindragosaPowerId))
            {
                var playerTavern = context.State.Player.Tavern;
                TavernShopSlots.Ensure(playerTavern);
                var candidates = playerTavern.Shop
                    .Select((card, index) => new { Card = card, Index = index })
                    .Where(item =>
                        item.Card != null &&
                        item.Card.CardKind == CardKind.Minion &&
                        !TavernShopSlots.IsSlotFrozen(playerTavern, item.Index))
                    .ToList();
                if (candidates.Count == 0)
                {
                    candidates = playerTavern.Shop
                        .Select((card, index) => new { Card = card, Index = index })
                        .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                        .ToList();
                }

                if (candidates.Count > 0)
                {
                    var picked = candidates[context.Rng.NextInt(candidates.Count)];
                    AddTag(picked.Card, "frozen_by_sindragosa");
                    TavernShopSlots.SetSlotFrozen(playerTavern, picked.Index, true);
                }

                if (HasBuddy(context.State, ThawedChampionCardId))
                {
                    var frozen = TavernShopSlots.FrozenCards(playerTavern)
                        .Where(card => card.CardKind == CardKind.Minion)
                        .ToList();
                    if (frozen.Count > 0)
                    {
                        MakeGoldenInPlace(frozen[context.Rng.NextInt(frozen.Count)], context.Minions);
                        result.Messages.Add("Thawed Champion: made a Frozen Tavern minion Golden.");
                    }
                }

                result.Messages.Add("Stay Frosty: froze a Tavern minion slot at end of turn.");
            }

            ResolveRaynorTurnEnded(context, result, powerId);
            ResolveKerriganTurnEnded(context, result, powerId);

            if (!IsPower(powerId, CThunPowerId))
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            ResetTentacleTemporaryBuffs(context.State);
            var repeats = 1 + Math.Max(0, GetCounterOrDefault(tavern, CThunRepeatCounter, 0));
            for (var index = 0; index < repeats; index += 1)
            {
                var target = PickFriendlyMinion(context, excludeCardId: TentacleOfCThunCardId);
                if (target == null)
                {
                    break;
                }

                Buff(target, 1, 1, "Saturday C'Thuns!");
                BuffTentaclesAfterDifferentFriendlyMinionGainsStats(context.State, target);
            }

            tavern.HeroEffectCounters[CThunRepeatCounter] = repeats;
            result.Messages.Add("Saturday C'Thuns!: repeated " + Math.Max(0, repeats - 1) + " times.");
        }

        private static void ResolveVardenRefresh(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var candidates = tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.TavernTier)
                .ThenBy(card => card.InstanceId)
                .ToList();
            var source = candidates.FirstOrDefault();
            if (source == null)
            {
                return;
            }

            var copy = source.Clone();
            copy.InstanceId = source.InstanceId + "-varden-copy-" + tavern.RecruitLog.Count;
            copy.PoolSource = PoolSource.Copy;
            copy.OriginPoolSource = PoolSource.Copy;
            copy.PoolCopiesHeld = 0;
            var insertIndex = tavern.Shop.FindIndex(card => card == null);
            if (insertIndex >= 0)
            {
                tavern.Shop[insertIndex] = copy;
            }
            else
            {
                tavern.Shop.Add(copy);
            }

            AddTag(source, "frozen_by_varden");
            AddTag(copy, "frozen_by_varden");
            TavernShopSlots.SetSlotFrozen(tavern, tavern.Shop.FindIndex(card => card != null && card.InstanceId == source.InstanceId), true);
            TavernShopSlots.SetSlotFrozen(tavern, tavern.Shop.FindIndex(card => card != null && card.InstanceId == copy.InstanceId), true);
            if (HasBuddy(context.State, VardenAquarriorCardId))
            {
                var amount = Math.Max(1, tavern.Tier);
                Buff(source, amount, amount, "Varden's Aquarrior");
                Buff(copy, amount, amount, "Varden's Aquarrior");
            }

            result.Messages.Add("Twice as Nice: copied and froze the Tavern's highest-tier minion.");
        }

        private static void ResolveYseraRefresh(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var hasDragon = tavern.Shop.Any(card => card != null && card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Dragon));
            if (hasDragon)
            {
                return;
            }

            var candidates = context.Minions.All
                .Where(minion => minion.Tribes.Contains(Tribe.Dragon) && minion.TavernTier <= tavern.Tier)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var chosen = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(chosen, BoardSide.Player, "ysera-dragon-" + tavern.RecruitLog.Count);
            var insertIndex = tavern.Shop.FindIndex(c => c == null);
            if (insertIndex >= 0)
            {
                tavern.Shop[insertIndex] = card;
            }
            else
            {
                tavern.Shop.Add(card);
            }

            result.Messages.Add("Ysera's gift: a Dragon appeared in the Tavern.");
        }

        private static void ResolveHoggarrRefresh(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var hasPirate = tavern.Shop.Any(card => card != null && card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Pirate));
            if (hasPirate)
            {
                return;
            }

            var candidates = context.Minions.All
                .Where(minion => minion.Tribes.Contains(Tribe.Pirate) && minion.TavernTier <= tavern.Tier)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var chosen = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(chosen, BoardSide.Player, "hoggarr-pirate-" + tavern.RecruitLog.Count);
            var insertIndex = tavern.Shop.FindIndex(c => c == null);
            if (insertIndex >= 0)
            {
                tavern.Shop[insertIndex] = card;
            }
            else
            {
                tavern.Shop.Add(card);
            }

            result.Messages.Add("Cap'n Hoggarr: a Pirate appeared in the Tavern.");
        }

        private static void ResolveEnhanceOMechanoRefresh(HeroEffectContext context, HeroEffectResult result)
        {
            var candidates = context.State.Player.Tavern.Shop
                .Where(card => card != null &&
                               card.CardKind == CardKind.Minion &&
                               BonusKeywords.Any(keyword => !card.Keywords.Contains(keyword)))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var target = candidates[context.Rng.NextInt(candidates.Count)];
            var missingKeywords = BonusKeywords
                .Where(keyword => !target.Keywords.Contains(keyword))
                .ToList();
            var keyword = missingKeywords[context.Rng.NextInt(missingKeywords.Count)];
            AddKeyword(target, keyword, "Enhancification");
            result.Messages.Add("Enhancification: gave a Tavern minion " + keyword + ".");
        }

        private static void DispatchBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            switch (context.EventType)
            {
                case HeroEffectEventType.CardBought:
                    ResolveCardBoughtBuddies(context, result);
                    break;
                case HeroEffectEventType.TavernSpellCast:
                    ResolveTavernSpellCastBuddies(context, result);
                    break;
                case HeroEffectEventType.MinionSold:
                    ResolveMinionSoldBuddies(context, result);
                    break;
                case HeroEffectEventType.TurnEnded:
                    ResolveTurnEndedBuddies(context, result);
                    break;
                case HeroEffectEventType.TurnStarted:
                    ResolveTurnStartedBuddies(context, result);
                    break;
                case HeroEffectEventType.HeroPowerUsed:
                    ResolveHeroPowerUsedBuddies(context, result);
                    UpdateMalorneStats(context.State);
                    UpdateValithriaDreamwalkerStats(context.State);
                    break;
                case HeroEffectEventType.CardPlayed:
                    UpdateValithriaDreamwalkerStats(context.State);
                    break;
                case HeroEffectEventType.Magnetized:
                    ResolveMagnetizedBuddies(context, result);
                    break;
                case HeroEffectEventType.FriendlyDeathrattleTriggeredInCombat:
                    ResolveCombatDeathrattleBuddies(context, result);
                    break;
                case HeroEffectEventType.FriendlyMinionKilledEnemyInCombat:
                    ResolveCombatKillBuddies(context, result);
                    break;
            }
        }

        private static void ResolveCardBoughtBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            if (context.Card == null)
            {
                return;
            }

            if (context.Card.CardKind == CardKind.TavernSpell &&
                context.GoldCost == 0 &&
                HasBuddy(context.State, CelestialArchiveCardId))
            {
                AddCopyToHand(context.State, context.Card, "celestial-archive");
                result.Messages.Add("The Celestial Archive: copied the zero-cost Tavern spell.");
            }

            if (context.Card.Tribes.Contains(Tribe.Pirate) && HasBuddy(context.State, ShiningSailorCardId))
            {
                Buff(context.Card, 2, 2, "Shining Sailor");
                result.Messages.Add("Shining Sailor: gained +2/+2 from buying a Pirate.");
            }

            var bonusKeywordCount = CountBonusKeywords(context.Card);
            if (bonusKeywordCount > 0)
            {
                foreach (var buddy in MatchingBoardBuddies(context.State, EnhanceOMedicoCardId))
                {
                    var amount = bonusKeywordCount * 3;
                    Buff(buddy, amount, amount, "Enhance-o Medico");
                    result.Messages.Add("Enhance-o Medico: gained +" + amount + "/+" + amount + " from bought Bonus Keywords.");
                }
            }

            if (HasBuddy(context.State, LivingNightmareCardId))
            {
                foreach (var shopMinion in context.State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion))
                {
                    Buff(shopMinion, 2, 2, "Living Nightmare");
                }

                result.Messages.Add("Living Nightmare: Tavern minions gained +2/+2 this turn.");
            }

            if (context.Card.CardKind == CardKind.Minion)
            {
                foreach (var buddy in MatchingBoardBuddies(context.State, DranoshSaurfangCardId))
                {
                    Buff(buddy, Math.Max(0, context.Card.Attack / 2), Math.Max(0, context.Card.MaxHealth / 2), "Dranosh Saurfang");
                    result.Messages.Add("Dranosh Saurfang: gained half the bought minion's stats.");
                }
            }

            foreach (var buddy in MatchingBoardBuddies(context.State, SI7ScoutCardId))
            {
                Buff(buddy, 2, 2, "SI:7 Scout");
                result.Messages.Add("SI:7 Scout: gained +2/+2 after buying a card.");
            }

            if (context.Card.CardKind == CardKind.Minion && HasBuddy(context.State, NineFrogsCardId))
            {
                var remaining = GetCounterOrDefault(context.State.Player.Tavern, NineFrogsRemainingCounter, 9);
                if (remaining > 0 && AddRandomTavernSpellToHand(context, context.Card.TavernTier, context.Card.TavernTier, "nine-frogs"))
                {
                    context.State.Player.Tavern.HeroEffectCounters[NineFrogsRemainingCounter] = remaining - 1;
                    result.Messages.Add("The Nine Frogs: gained a same-tier Tavern spell.");
                }
            }
        }

        private static void ResolveTavernSpellCastBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            if (context.Card == null || context.Card.CardKind != CardKind.TavernSpell)
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            if (HasBuddy(context.State, ReliquaryAttendantCardId))
            {
                tavern.HeroEffectCounters.TryGetValue(ReliquaryCopiedRoundCounter, out var copiedRound);
                if (copiedRound != context.State.Round)
                {
                    AddCopyToHand(context.State, context.Card, "reliquary-attendant");
                    tavern.HeroEffectCounters[ReliquaryCopiedRoundCounter] = context.State.Round;
                    result.Messages.Add("Reliquary Attendant: copied the first Tavern spell cast this turn.");
                }
            }

            if (HasBuddy(context.State, CrazyMonkeyCardId))
            {
                var spells = IncrementCounter(tavern, CrazyMonkeySpellCounter, 1);
                if (spells >= 2)
                {
                    tavern.HeroEffectCounters[CrazyMonkeySpellCounter] = 0;
                    var bananas = GetCounterOrDefault(tavern, CrazyMonkeyBananaCounter, 2) + 1;
                    tavern.HeroEffectCounters[CrazyMonkeyBananaCounter] = bananas;
                    result.Messages.Add("Crazy Monkey: Banana feed improved to " + bananas + ".");
                }
            }

            if (HasBuddy(context.State, TychusFindlayCardId))
            {
                var spells = IncrementCounter(tavern, TychusSpellCounter, 1);
                if (spells >= 2)
                {
                    tavern.HeroEffectCounters[TychusSpellCounter] = 0;
                    var copies = MatchingBoardBuddies(context.State, TychusFindlayCardId).Any(card => card.Golden) ? 2 : 1;
                    for (var index = 0; index < copies; index += 1)
                    {
                        AddBattlecruiserUpgradeToHand(context, "tychus-" + index);
                    }

                    result.Messages.Add("Tychus Findlay: gained " + copies + " Battlecruiser Upgrade(s) after two Tavern spells.");
                }
            }

            if (HasBuddy(context.State, BarovsApprenticeCardId) &&
                context.Card.Tags != null &&
                context.Card.Tags.Contains("tavern_coin"))
            {
                TavernRules.GainGold(tavern, 1);
                result.Messages.Add("Barov's Apprentice: gained 1 Gold after you played a Tavern Coin.");
            }
        }

        private static void ResolveMagnetizedBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            if (context.Card == null ||
                !string.Equals(context.Card.CardId, ProbiusCardId, StringComparison.OrdinalIgnoreCase) ||
                context.TargetIndex < 0 ||
                context.TargetIndex >= context.State.Player.Board.Count)
            {
                return;
            }

            var target = context.State.Player.Board[context.TargetIndex];
            MakeGoldenInPlace(target, context.Minions);
            result.Messages.Add("Probius: made the Mech it Magnetized to Golden.");
        }

        private static void ResolveHeroPowerUsedBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            if (IsPower(context.State.Player.HeroPowerCardId, AkazamzarakPowerId) &&
                HasBuddy(context.State, StreetMagicianCardId))
            {
                AddBetterSecretProxyToHand(context, result);
            }

            if (IsPower(context.State.Player.HeroPowerCardId, LichKingPowerId) &&
                context.TargetIndex >= 0 &&
                context.TargetIndex < context.State.Player.Board.Count)
            {
                var target = context.State.Player.Board[context.TargetIndex];
                foreach (var arfus in MatchingBoardBuddies(context.State, ArfusCardId))
                {
                    if (target.InstanceId == arfus.InstanceId)
                    {
                        continue;
                    }

                    Buff(target, Math.Max(0, arfus.Attack), 0, "Arfus");
                    result.Messages.Add("Arfus: Reborn minion gained Arfus's Attack.");
                }
            }

            if (IsPower(context.State.Player.HeroPowerCardId, IngePowerId) &&
                context.TargetIndex >= 0 &&
                context.TargetIndex < context.State.Player.Board.Count)
            {
                var target = context.State.Player.Board[context.TargetIndex];
                var healthMode = GetCounterOrDefault(context.State.Player.Tavern, "hero:inge:last_health_mode", 0) == 1;
                foreach (var buddy in MatchingBoardBuddies(context.State, SolemnSerenaderCardId))
                {
                    var amount = Math.Max(0, buddy.Attack / 2);
                    if (healthMode)
                    {
                        Buff(target, 0, amount, "Solemn Serenader");
                        NotifyTitanicGuardian(context.State, amount, target.InstanceId, result);
                    }
                    else
                    {
                        Buff(target, amount, 0, "Solemn Serenader");
                    }

                    result.Messages.Add("Solemn Serenader: enhanced the Hero Power target.");
                }
            }

            foreach (var buddy in MatchingBoardBuddies(context.State, KarlTheLostCardId))
            {
                var buffed = 0;
                foreach (var minion in context.State.Player.Board.Where(card => card.Keywords.Contains(Keyword.DivineShield)))
                {
                    Buff(minion, 2, 0, "Karl the Lost");
                    buffed += 1;
                }

                if (buffed > 0)
                {
                    result.Messages.Add("Karl the Lost: your Divine Shield minions gained +2 Attack.");
                }
            }
        }

        private static void ResolveTurnStartedBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            foreach (var lilKt in MatchingBoardBuddies(context.State, LilKtCardId))
            {
                if (AddLowestHealthOpponentPlainMinionToHand(context, "lil-kt-" + lilKt.InstanceId))
                {
                    result.Messages.Add("Lil' K.T.: gained a plain minion from the opponent warband proxy.");
                }
                else
                {
                    result.Messages.Add("Lil' K.T.: no opponent warband minion is available yet.");
                }
            }

            foreach (var boxCars in MatchingBoardBuddies(context.State, BoxCarsCardId))
            {
                var roll = RollSixSidedDie(context);
                context.State.Player.Tavern.HeroEffectCounters[BoxCarsLastRollCounter] = roll;
                if (StartBoxCarsTavernSpellDiscover(context, roll, boxCars.InstanceId))
                {
                    result.Messages.Add("Box Cars: rolled " + roll + " and started a Tier " + roll + " Tavern spell Discover.");
                }
                else
                {
                    result.Messages.Add("Box Cars: rolled " + roll + " but no Tier " + roll + " Tavern spell Discover started.");
                }
            }

            foreach (var hunter in MatchingBoardBuddies(context.State, HunterOfOldCardId))
            {
                if (AddOpponentBuddyToHand(context, GetLastOpponentHeroId(context.State), "hunter-of-old-" + hunter.InstanceId, "last_opponent_buddy_proxy"))
                {
                    result.Messages.Add("Hunter of Old: gained the last opponent's Buddy.");
                }
                else
                {
                    result.Messages.Add("Hunter of Old: no last-opponent Buddy mapping is available yet.");
                }
            }

            foreach (var warden in MatchingBoardBuddies(context.State, WardenThelwaterCardId))
            {
                if (AddOpponentBuddyToHand(context, GetNextOpponentHeroId(context.State), "warden-thelwater-" + warden.InstanceId, "next_opponent_buddy_proxy"))
                {
                    result.Messages.Add("Warden Thelwater: gained the next opponent's Buddy proxy.");
                }
                else
                {
                    result.Messages.Add("Warden Thelwater: no next-opponent Buddy mapping is available yet.");
                }
            }
        }

        private static void ResolveMinionSoldBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            if (context.Card == null)
            {
                return;
            }

            if (IsPower(context.State.Player.HeroPowerCardId, KraggPowerId) &&
                string.Equals(context.Card.CardId, SharkbaitCardId, StringComparison.OrdinalIgnoreCase))
            {
                context.State.Player.Tavern.HeroEffectCounters[KraggUsedCounter] = 0;
                result.Messages.Add("Sharkbait: refreshed your Hero Power.");
            }

            if (string.Equals(context.Card.CardId, SrTombDiverCardId, StringComparison.OrdinalIgnoreCase))
            {
                var rightmost = context.State.Player.Board.LastOrDefault(card => card != null && card.CardKind == CardKind.Minion);
                if (rightmost != null)
                {
                    MakeGoldenInPlace(rightmost, context.Minions);
                    result.Messages.Add("Sr. Tomb Diver: made your right-most minion Golden.");
                }
            }

            if (string.Equals(context.Card.CardId, SpiritOfAirCardId, StringComparison.OrdinalIgnoreCase))
            {
                var target = PickFriendlyMinion(context);
                AddBonusKeywordSet(target, "Spirit of Air");
                if (target != null)
                {
                    result.Messages.Add("Spirit of Air: gave a random friendly minion Windfury, Divine Shield, and Taunt on the Tavern death proxy.");
                }
            }

            if (string.Equals(context.Card.CardId, MawswornSoulkeeperCardId, StringComparison.OrdinalIgnoreCase))
            {
                var added = AddRandomTribeMinionsToBoard(context, Tribe.Undead, 2, "mawsworn");
                if (added > 0)
                {
                    result.Messages.Add("Mawsworn Soulkeeper: summoned " + added + " random Undead on the Tavern death proxy.");
                }
            }

            if (string.Equals(context.Card.CardId, KilrekCardId, StringComparison.OrdinalIgnoreCase))
            {
                AddRandomTribeMinionToHand(context, Tribe.Demon, "kilrek");
                result.Messages.Add("Kil'rek: gained a random Demon on the Tavern death proxy.");
            }

            if (string.Equals(context.Card.CardId, TuskarrRaiderCardId, StringComparison.OrdinalIgnoreCase))
            {
                AddRandomBountyToHand(context, "tuskarr-deathrattle");
                result.Messages.Add("Tuskarr Raider: gained a random Bounty.");
            }

            if (string.Equals(context.Card.CardId, CrazyMonkeyCardId, StringComparison.OrdinalIgnoreCase))
            {
                var bananas = GetCounterOrDefault(context.State.Player.Tavern, CrazyMonkeyBananaCounter, 2);
                var fed = FeedBananasToBoard(context, bananas, context.Card.InstanceId);
                if (fed > 0)
                {
                    result.Messages.Add("Crazy Monkey: fed " + fed + " Bananas to your minions.");
                }
            }

            if (string.Equals(context.Card.CardId, ElementiumSquirrelBombCardId, StringComparison.OrdinalIgnoreCase))
            {
                var deaths = Math.Max(1, GetCounterOrDefault(context.State.Player.Tavern, MillificentMechDeathCounter, 1));
                var damage = deaths * 4;
                if (DamageRandomEnemyMinion(context, damage))
                {
                    result.Messages.Add("Elementium Squirrel Bomb: dealt " + damage + " damage.");
                }

                context.State.Player.Tavern.HeroEffectCounters[MillificentMechDeathCounter] = 0;
            }

            if (string.Equals(context.Card.CardId, PhyreszCardId, StringComparison.OrdinalIgnoreCase))
            {
                StartSingletonPlainCopyDiscover(context, result, context.Card.InstanceId);
            }

            if (string.Equals(context.Card.CardId, AsherHaberdasherCardId, StringComparison.OrdinalIgnoreCase))
            {
                var hats = GetCounterOrDefault(context.State.Player.Tavern, "hero:deryl:asher_hats", 0);
                PassDerylHats(context.State, context.Card, hats, result);
                context.State.Player.Tavern.HeroEffectCounters["hero:deryl:asher_hats"] = 0;
            }

            if (string.Equals(context.Card.CardId, MaxwellCardId, StringComparison.OrdinalIgnoreCase))
            {
                AddBuddyForCurrentHeroPowerToHand(context, result);
            }

            if (string.Equals(context.Card.CardId, ZippersCardId, StringComparison.OrdinalIgnoreCase))
            {
                if (AddHelpfulCardToHand(context, "zippers"))
                {
                    result.Messages.Add("Zippers: gained a helpful card on the Tavern death proxy.");
                }
            }

            if (string.Equals(context.Card.CardId, FestergutCardId, StringComparison.OrdinalIgnoreCase))
            {
                SummonAndGetUndeadCreation(context, result);
            }

            if (string.Equals(context.Card.CardId, BrokenHornCardId, StringComparison.OrdinalIgnoreCase))
            {
                StartBrokenHornDiscover(context, result);
            }

            if (IsPower(context.State.Player.HeroPowerCardId, DerylPowerId) &&
                !string.Equals(context.Card.CardId, AsherHaberdasherCardId, StringComparison.OrdinalIgnoreCase) &&
                HasBuddy(context.State, AsherHaberdasherCardId))
            {
                var hats = GetCounterOrDefault(context.State.Player.Tavern, "hero:deryl:asher_hats", 0) + 2;
                context.State.Player.Tavern.HeroEffectCounters["hero:deryl:asher_hats"] = hats;
                result.Messages.Add("Asher the Haberdasher: gained two hats.");
            }
        }

        private static void ResolveCombatDeathrattleBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            if (context.Card == null)
            {
                return;
            }

            if (string.Equals(context.Card.CardId, SrTombDiverCardId, StringComparison.OrdinalIgnoreCase))
            {
                var rightmost = context.State.Player.Board.LastOrDefault(card =>
                    card != null &&
                    card.CardKind == CardKind.Minion &&
                    !IsSameInstance(card, context.Card));
                if (rightmost != null)
                {
                    MakeGoldenInPlace(rightmost, context.Minions);
                    result.Messages.Add("Sr. Tomb Diver: made your right-most minion Golden from a combat Deathrattle.");
                }
            }

            if (string.Equals(context.Card.CardId, SpiritOfAirCardId, StringComparison.OrdinalIgnoreCase))
            {
                var target = PickFriendlyMinion(context, excludeInstanceId: context.Card.InstanceId);
                AddBonusKeywordSet(target, "Spirit of Air");
                if (target != null)
                {
                    result.Messages.Add("Spirit of Air: gave a random friendly minion Windfury, Divine Shield, and Taunt from a combat Deathrattle.");
                }
            }

            if (string.Equals(context.Card.CardId, MawswornSoulkeeperCardId, StringComparison.OrdinalIgnoreCase))
            {
                var added = AddRandomTribeMinionsToBoard(context, Tribe.Undead, 2, "mawsworn-combat");
                if (added > 0)
                {
                    result.Messages.Add("Mawsworn Soulkeeper: summoned " + added + " random Undead from a combat Deathrattle.");
                }
            }

            if (string.Equals(context.Card.CardId, KilrekCardId, StringComparison.OrdinalIgnoreCase))
            {
                AddRandomTribeMinionToHand(context, Tribe.Demon, "kilrek-combat");
                result.Messages.Add("Kil'rek: gained a random Demon from a combat Deathrattle.");
            }

            if (string.Equals(context.Card.CardId, TuskarrRaiderCardId, StringComparison.OrdinalIgnoreCase))
            {
                AddRandomBountyToHand(context, "tuskarr-combat-deathrattle");
                result.Messages.Add("Tuskarr Raider: gained a random Bounty from a combat Deathrattle.");
            }

            if (string.Equals(context.Card.CardId, ElementiumSquirrelBombCardId, StringComparison.OrdinalIgnoreCase))
            {
                var deaths = Math.Max(1, GetCounterOrDefault(context.State.Player.Tavern, MillificentMechDeathCounter, 1));
                var damage = deaths * 4;
                if (DamageRandomEnemyMinion(context, damage))
                {
                    result.Messages.Add("Elementium Squirrel Bomb: dealt " + damage + " post-combat proxy damage.");
                }

                context.State.Player.Tavern.HeroEffectCounters[MillificentMechDeathCounter] = 0;
            }

            if (string.Equals(context.Card.CardId, ZippersCardId, StringComparison.OrdinalIgnoreCase))
            {
                if (AddHelpfulCardToHand(context, "zippers-combat-deathrattle"))
                {
                    result.Messages.Add("Zippers: gained a helpful card from a combat Deathrattle.");
                }
            }

            if (string.Equals(context.Card.CardId, FestergutCardId, StringComparison.OrdinalIgnoreCase))
            {
                SummonAndGetUndeadCreation(context, result);
            }
        }

        private static void ResolveCombatKillBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            var killCount = GetCounterOrDefault(context.State.Player.Tavern, CombatKillCountCounter, 0);
            foreach (var buddy in MatchingBoardBuddies(context.State, IcesnarlCardId))
            {
                var health = buddy.Golden ? 6 : 3;
                Buff(buddy, 0, health, "Icesnarl the Mighty");
                result.Messages.Add("Icesnarl the Mighty: gained +" + health + " Health permanently from a friendly kill.");
            }

            if (killCount == 2 && HasBuddy(context.State, LoyalHenchmanCardId))
            {
                if (context.TargetCard != null)
                {
                    AddPlainCopyToHand(context.State, context.TargetCard, "loyal-henchman-kill", context.Minions);
                    result.Messages.Add("Loyal Henchman: gained a plain copy of the second enemy killed this combat.");
                }
                else
                {
                    result.Messages.Add("Loyal Henchman: second kill target data was unavailable, so no copy was created.");
                }
            }
        }

        private static void ResolveTurnEndedBuddies(HeroEffectContext context, HeroEffectResult result)
        {
            foreach (var buddy in MatchingBoardBuddies(context.State, WeebominationCardId).ToList())
            {
                var index = context.State.Player.Board.FindIndex(minion => minion.InstanceId == buddy.InstanceId);
                if (index <= 0)
                {
                    continue;
                }

                var missingHealth = Math.Max(0, context.State.Player.MaxHealth - context.State.Player.Health);
                Buff(context.State.Player.Board[index - 1], 0, 1 + missingHealth, "Weebomination");
                result.Messages.Add("Weebomination: buffed the minion to its left.");
            }

            foreach (var buddy in MatchingBoardBuddies(context.State, BilgewaterMogulCardId))
            {
                IncrementCounter(context.State.Player.Tavern, MaxGoldBonusCounter, 1);
                TavernRules.IncreaseMaxGold(context.State.Player.Tavern, 1);
                result.Messages.Add("Bilgewater Mogul: maximum Gold increased by 1.");
            }

            if (HasBuddy(context.State, StormpikeLieutenantCardId))
            {
                var target = context.State.Player.Board.LastOrDefault(card => card != null && card.CardKind == CardKind.Minion);
                if (target != null)
                {
                    Buff(target, 0, 10, "Stormpike Lieutenant");
                    result.Messages.Add("Stormpike Lieutenant: gave your right-most minion +10 Health.");
                }
            }

            if (HasBuddy(context.State, FrostwolfLieutenantCardId))
            {
                var target = context.State.Player.Board.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion);
                if (target != null)
                {
                    Buff(target, 10, 0, "Frostwolf Lieutenant");
                    result.Messages.Add("Frostwolf Lieutenant: gave your left-most minion +10 Attack.");
                }
            }

            if (HasBuddy(context.State, EvergreenBotaniCardId))
            {
                var tavern = context.State.Player.Tavern;
                var candidates = context.Minions.All
                    .Where(minion => minion.TavernTier <= tavern.Tier && minion.Tribes.Contains(Tribe.None) == false)
                    .ToList();
                if (candidates.Count > 0 && context.State.Player.Board.Count < 7)
                {
                    var chosen = candidates[context.Rng.NextInt(candidates.Count)];
                    var card = MinionFactory.Create(chosen, BoardSide.Player, "evergreen-botani-" + tavern.RecruitLog.Count);
                    context.State.Player.Board.Add(card);
                    result.Messages.Add("Evergreen Botani: a " + tavern.Tier + "-tier minion joined your board.");
                }
            }

            if (HasBuddy(context.State, LanternTenderCardId))
            {
                var added = 0;
                for (var index = 0; index < 2; index += 1)
                {
                    if (AddRandomStatTavernSpellToHand(context, "lantern-tender-" + index))
                    {
                        added += 1;
                    }
                }

                if (added > 0)
                {
                    result.Messages.Add("Lantern Tender: gained " + added + " stat Tavern spells.");
                }
            }

            if (HasBuddy(context.State, DagwikStickytoeCardId))
            {
                var golden = context.State.Player.Board
                    .Where(card => card != null && card.CardKind == CardKind.Minion && card.Golden)
                    .ToList();
                if (golden.Count > 0)
                {
                    var target = golden[context.Rng.NextInt(golden.Count)];
                    Buff(target, 5, 5, "Dagwik Stickytoe");
                    result.Messages.Add("Dagwik Stickytoe: gave a Golden minion +5/+5.");
                }
            }

            if (HasBuddy(context.State, AkaliRockRhinoCardId))
            {
                var turns = IncrementCounter(context.State.Player.Tavern, VooneBuddyCounter, 1);
                if (turns >= 2)
                {
                    context.State.Player.Tavern.HeroEffectCounters[VooneBuddyCounter] = 0;
                    CopyLeftmostHandCard(context.State, "akali", context.Minions);
                    result.Messages.Add("Akali, Rock Rhino: copied the left-most card in your hand.");
                }
            }

            foreach (var bellhop in MatchingBoardBuddies(context.State, FantasticBellhopCardId))
            {
                if (AddHelpfulCardToHand(context, "fantastic-bellhop-" + bellhop.InstanceId))
                {
                    result.Messages.Add("Fantastic Bellhop: gained a helpful card.");
                }
            }

            UpdateMalorneStats(context.State);
            UpdateValithriaDreamwalkerStats(context.State);
            UpdateMishmashStats(context.State);
        }

        private static MinionInstance GetFriendlyBoardTarget(HeroEffectContext context, string error)
        {
            var board = context.State.Player.Board;
            if (context.TargetIndex < 0 || context.TargetIndex >= board.Count || board[context.TargetIndex] == null)
            {
                throw new InvalidOperationException(error);
            }

            return board[context.TargetIndex];
        }

        private static MinionInstance GetShopTarget(HeroEffectContext context, string error)
        {
            var shop = context.State.Player.Tavern.Shop;
            if (context.TargetIndex < 0 || context.TargetIndex >= shop.Count || shop[context.TargetIndex] == null)
            {
                throw new InvalidOperationException(error);
            }

            return shop[context.TargetIndex];
        }

        private static bool TryResolveTarget(
            HeroEffectContext context,
            int targetIndex,
            TargetZone targetZone,
            string targetInstanceId,
            out MinionInstance target)
        {
            target = null;
            if (context?.State == null)
            {
                return false;
            }

            switch (targetZone)
            {
                case TargetZone.FriendlyBoard:
                    return TryResolveTargetInList(context.State.Player.Board, targetIndex, targetInstanceId, out target);
                case TargetZone.TavernShop:
                    return TryResolveTargetInList(context.State.Player.Tavern.Shop, targetIndex, targetInstanceId, out target);
                case TargetZone.OpponentBoard:
                    return TryResolveTargetInList(context.State.Opponent.Board, targetIndex, targetInstanceId, out target);
                case TargetZone.Hand:
                    return TryResolveTargetInList(context.State.Player.Tavern.Hand, targetIndex, targetInstanceId, out target);
                default:
                    return TryResolveUnspecifiedTarget(context, targetIndex, targetInstanceId, out target);
            }
        }

        private static bool TryResolveUnspecifiedTarget(HeroEffectContext context, int targetIndex, string targetInstanceId, out MinionInstance target)
        {
            if (!string.IsNullOrEmpty(targetInstanceId))
            {
                return TryResolveTargetInList(context.State.Player.Board, -1, targetInstanceId, out target) ||
                       TryResolveTargetInList(context.State.Player.Tavern.Shop, -1, targetInstanceId, out target) ||
                       TryResolveTargetInList(context.State.Player.Tavern.Hand, -1, targetInstanceId, out target) ||
                       TryResolveTargetInList(context.State.Opponent.Board, -1, targetInstanceId, out target);
            }

            return TryResolveTargetInList(context.State.Player.Board, targetIndex, null, out target);
        }

        private static bool TryResolveTargetInList(
            IList<MinionInstance> candidates,
            int targetIndex,
            string targetInstanceId,
            out MinionInstance target)
        {
            target = null;
            if (candidates == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(targetInstanceId))
            {
                target = candidates.FirstOrDefault(card =>
                    card != null &&
                    string.Equals(card.InstanceId, targetInstanceId, StringComparison.OrdinalIgnoreCase));
                if (target != null)
                {
                    return true;
                }
            }

            if (targetIndex < 0 || targetIndex >= candidates.Count)
            {
                return false;
            }

            target = candidates[targetIndex];
            return target != null;
        }

        private static MinionInstance PickShopMinion(HeroEffectContext context, string error)
        {
            var candidates = context.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(error);
            }

            return candidates[context.Rng.NextInt(candidates.Count)];
        }

        private static void SwapFriendlyMinionWithShop(HeroEffectContext context, HeroEffectResult result)
        {
            var board = context.State.Player.Board;
            var target = GetFriendlyBoardTarget(context, "Swap, Lock, & Shop It needs a friendly minion target.");
            if (target.Golden)
            {
                throw new InvalidOperationException("Swap, Lock, & Shop It needs a non-Golden friendly minion.");
            }

            var tavernMinions = context.State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .ToList();
            if (tavernMinions.Count == 0)
            {
                throw new InvalidOperationException("The Tavern has no minion to swap.");
            }

            var shopPick = tavernMinions[context.Rng.NextInt(tavernMinions.Count)];
            board[context.TargetIndex] = shopPick.Card;
            shopPick.Card.Owner = BoardSide.Player;
            context.State.Player.Tavern.Shop[shopPick.Index] = target;
            result.Messages.Add("Swap, Lock, & Shop It: swapped a friendly minion with a Tavern minion.");
        }

        private static void DevourFriendlyMinion(HeroEffectContext context, HeroEffectResult result)
        {
            var board = context.State.Player.Board;
            var target = GetFriendlyBoardTarget(context, "Devour needs a friendly minion target.");
            var attack = Math.Max(0, target.Attack);
            var health = Math.Max(0, target.MaxHealth);
            var extraTargets = string.Equals(target.CardId, NightmareEctoplasmCardId, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            board.RemoveAt(context.TargetIndex);
            TavernRules.GainGold(context.State.Player.Tavern, 1);
            SpitStatsOntoRandomMinions(context, attack, health, 1 + extraTargets, "Devour", result);
            result.Messages.Add(extraTargets > 0
                ? "Devour: sold Nightmare Ectoplasm and spat its stats onto an extra minion."
                : "Devour: sold a friendly minion and spat its stats onto another.");
        }

        private static void SpitStatsOntoRandomMinions(HeroEffectContext context, int attack, int health, int count, string source, HeroEffectResult result)
        {
            var candidates = context.State.Player.Board
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            for (var index = 0; index < count && candidates.Count > 0; index += 1)
            {
                var pickIndex = context.Rng.NextInt(candidates.Count);
                var target = candidates[pickIndex];
                candidates.RemoveAt(pickIndex);
                Buff(target, attack, health, source);
                NotifyTitanicGuardian(context.State, health, target.InstanceId, result);
            }
        }

        private static void ResolveSpiritSwap(HeroEffectContext context, HeroEffectResult result)
        {
            var first = GetSpiritSwapTarget(context, false, "Spirit Swap needs a first minion target.");
            var second = GetSpiritSwapTarget(context, true, "Spirit Swap needs a second minion target.");
            if (string.Equals(first.InstanceId, second.InstanceId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Spirit Swap needs two different minions.");
            }

            var firstAttack = Math.Max(0, first.Attack);
            var secondAttack = Math.Max(0, second.Attack);
            Buff(first, secondAttack, 0, VoljinSwapSource);
            Buff(second, firstAttack, 0, VoljinSwapSource);
            AddTag(first, "temporary_spirit_swap");
            AddTag(second, "temporary_spirit_swap");
            result.Messages.Add("Spirit Swap: two explicit targets gained each other's Attack until next turn.");
        }

        private static MinionInstance GetSpiritSwapTarget(HeroEffectContext context, bool secondary, string error)
        {
            var targetIndex = secondary ? context.SecondaryTargetIndex : context.TargetIndex;
            var targetZone = secondary ? context.SecondaryTargetZone : context.TargetZone;
            var targetInstanceId = secondary ? context.SecondaryTargetInstanceId : context.TargetInstanceId;
            if (targetZone == TargetZone.Unspecified && string.IsNullOrEmpty(targetInstanceId))
            {
                targetZone = TargetZone.FriendlyBoard;
            }

            if (targetZone != TargetZone.Unspecified &&
                targetZone != TargetZone.FriendlyBoard &&
                targetZone != TargetZone.TavernShop)
            {
                throw new InvalidOperationException(error);
            }

            if (!TryResolveTarget(context, targetIndex, targetZone, targetInstanceId, out var target) ||
                target.CardKind != CardKind.Minion ||
                (!context.State.Player.Board.Contains(target) && !context.State.Player.Tavern.Shop.Contains(target)))
            {
                throw new InvalidOperationException(error);
            }

            return target;
        }

        private static void ResolveMalygosHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            tavern.HeroEffectCounters.TryGetValue(MalygosRoundCounter, out var round);
            var uses = round == context.State.Round ? GetCounterOrDefault(tavern, MalygosUsesCounter, 0) : 0;

            tavern.HeroEffectCounters[MalygosRoundCounter] = context.State.Round;
            tavern.HeroEffectCounters[MalygosUsesCounter] = uses + 1;
            var higherTier = HasBuddy(context.State, NexusLordCardId);
            if (context.TargetIndex >= 0 && context.TargetIndex < tavern.Shop.Count && tavern.Shop[context.TargetIndex] != null)
            {
                tavern.Shop[context.TargetIndex] = CreateRandomReplacement(context, tavern.Shop[context.TargetIndex], higherTier, "malygos-shop");
                result.Messages.Add("Arcane Alteration: replaced a Tavern card.");
                return;
            }

            var target = GetFriendlyBoardTarget(context, "Arcane Alteration needs a board or Tavern target.");
            context.State.Player.Board[context.TargetIndex] = CreateRandomReplacement(context, target, higherTier, "malygos-board");
            result.Messages.Add("Arcane Alteration: replaced a friendly minion.");
        }

        private static MinionInstance CreateRandomReplacement(HeroEffectContext context, MinionInstance source, bool oneTierHigher, string suffix)
        {
            var tier = Math.Max(1, source.TavernTier + (oneTierHigher ? 1 : 0));
            var candidates = context.Minions.All
                .Where(minion => minion.InPool && minion.TavernTier == tier && minion.CardId != source.CardId)
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = context.Minions.All
                    .Where(minion => minion.InPool && minion.TavernTier == Math.Max(1, source.TavernTier) && minion.CardId != source.CardId)
                    .ToList();
            }

            if (candidates.Count == 0)
            {
                return source;
            }

            return MinionFactory.Create(candidates[context.Rng.NextInt(candidates.Count)], BoardSide.Player, suffix + "-" + context.State.Round + "-" + context.State.Player.Tavern.RecruitLog.Count, false, PoolSource.Copy, 0);
        }

        private static void ResolveMaievHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            SpendGold(tavern, 1);
            var target = GetShopTarget(context, "Imprison needs a Tavern card target.");
            if (tavern.Hand.Count >= HandLimit)
            {
                throw new InvalidOperationException("Hand is full.");
            }

            tavern.Shop[context.TargetIndex] = null;
            target.Owner = BoardSide.Player;
            target.PoolSource = PoolSource.Copy;
            target.OriginPoolSource = PoolSource.Copy;
            target.PoolCopiesHeld = 0;
            target.Counters[LockedTurnsCounter] = 2;
            AddTag(target, "locked_in_hand");
            if (GetCounterOrDefault(tavern, MaievNextGoldenCounter, 0) > 0)
            {
                MakeGoldenInPlace(target, context.Minions);
                tavern.HeroEffectCounters[MaievNextGoldenCounter] = 0;
            }

            tavern.Hand.Add(target);
            result.Messages.Add("Imprison: locked a Tavern card in your hand for 2 turns.");
        }

        private static void ResolveZephrysHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var wishes = GetCounterOrDefault(tavern, ZephrysWishesCounter, 3);
            if (wishes <= 0)
            {
                throw new InvalidOperationException("Three Wishes has no wishes left.");
            }

            SpendGold(tavern, 3);
            var pairs = context.State.Player.Board.Concat(tavern.Hand)
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .GroupBy(card => card.CardId)
                .FirstOrDefault(group => group.Count() == 2);
            if (pairs == null)
            {
                throw new InvalidOperationException("Three Wishes needs exactly two copies of a minion.");
            }

            var definition = context.Minions.All.FirstOrDefault(minion => minion.CardId == pairs.Key);
            if (definition == null || tavern.Hand.Count >= HandLimit)
            {
                throw new InvalidOperationException("Could not create the third copy.");
            }

            tavern.Hand.Add(MinionFactory.Create(definition, BoardSide.Player, "zephrys-" + context.State.Round + "-" + tavern.Hand.Count, false, PoolSource.Copy, 0));
            tavern.HeroEffectCounters[ZephrysWishesCounter] = wishes - 1;
            result.Messages.Add("Three Wishes: found the third copy.");
        }

        private static void ResolveHooktuskHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var target = GetFriendlyBoardTarget(context, "Trash for Treasure needs a friendly minion target.");
            var tier = Math.Max(1, target.TavernTier);
            context.State.Player.Board.RemoveAt(context.TargetIndex);
            StartLowerTierMinionDiscover(context, tier - 1, "hero-power:trash-for-treasure");
            if (HasBuddy(context.State, RagingContenderCardId))
            {
                TavernRules.GainGold(context.State.Player.Tavern, tier);
                result.Messages.Add("Raging Contender: gained Gold equal to the removed minion's Tier.");
            }

            result.Messages.Add("Trash for Treasure: removed a friendly minion and started a lower-tier Discover.");
        }

        private static void ResolveZerekHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            if (GetCounterOrDefault(tavern, ZerekUsedCounter, 0) > 0)
            {
                throw new InvalidOperationException("Cloning Gallery has already been used this game.");
            }

            var target = GetFriendlyBoardTarget(context, "Cloning Gallery needs a friendly minion target.");
            if (context.State.Player.Board.Count >= 7)
            {
                throw new InvalidOperationException("Board is full.");
            }

            var copy = target.Clone();
            copy.InstanceId = target.InstanceId + "-zerek-copy-" + context.State.Round;
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Copy;
            copy.OriginPoolSource = PoolSource.Copy;
            copy.PoolCopiesHeld = 0;
            AddTag(copy, "generated_copy");
            context.State.Player.Board.Insert(Math.Min(context.TargetIndex + 1, context.State.Player.Board.Count), copy);
            tavern.HeroEffectCounters[ZerekUsedCounter] = 1;
            result.Messages.Add("Cloning Gallery: summoned an exact copy of a friendly minion.");
        }

        private static void ResolveTogwaggleHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var discount = Math.Min(11, Math.Max(0, GetCounterOrDefault(tavern, TogwaggleDiscountCounter, 0)));
            SpendGold(tavern, Math.Max(0, 11 - discount));
            tavern.HeroEffectCounters[TogwaggleDiscountCounter] = 0;
            var stolen = 0;
            for (var index = 0; index < tavern.Shop.Count && tavern.Hand.Count < HandLimit; index += 1)
            {
                var card = tavern.Shop[index];
                if (card == null)
                {
                    continue;
                }

                tavern.Shop[index] = null;
                card.Owner = BoardSide.Player;
                card.PoolSource = PoolSource.Copy;
                card.OriginPoolSource = PoolSource.Copy;
                card.PoolCopiesHeld = 0;
                tavern.Hand.Add(card);
                stolen += 1;
            }

            result.Messages.Add("The Perfect Crime: stole " + stolen + " Tavern cards.");
        }

        private static void ResolveTessHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            SpendGold(tavern, 1);
            var warband = GetLastOpponentWarband(context.State).ToList();
            if (warband.Count == 0)
            {
                result.Messages.Add("Bob's Burgles: no last-opponent warband memory is available yet.");
                return;
            }

            tavern.Shop.Clear();
            foreach (var source in warband.Take(BoardLimit))
            {
                var copy = CreatePlainCopy(source, "player-tess-" + context.State.Round + "-" + tavern.Shop.Count, BoardSide.Player, PoolSource.Copy, context.Minions);
                AddTag(copy, "last_opponent_warband_copy");
                tavern.Shop.Add(copy);
            }

            result.Messages.Add("Bob's Burgles: refreshed the Tavern with " + tavern.Shop.Count + " last-opponent warband copies.");
        }

        private static void ResolveScabbsHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            SpendGold(context.State.Player.Tavern, 2);
            if (StartOpponentWarbandDiscover(context, GetNextOpponentWarband(context.State), "hero-power:i-spy", plainCopies: true))
            {
                result.Messages.Add("I Spy: started a next-opponent plain-copy Discover.");
            }
            else
            {
                result.Messages.Add("I Spy: no next-opponent warband memory/minions available.");
            }
        }

        private static void StartLowerTierMinionDiscover(HeroEffectContext context, int tier, string source)
        {
            var rewardTier = Math.Max(1, tier);
            var candidates = context.Minions.All
                .Where(minion => minion.InPool && minion.TavernTier == rewardTier)
                .ToList();
            if (candidates.Count < 3)
            {
                candidates = context.Minions.All
                    .Where(minion => minion.InPool && minion.TavernTier <= rewardTier)
                    .ToList();
            }

            StartMinionDiscover(context, candidates, rewardTier, source);
        }

        private static void AddRandomBattlecryMinionToHand(HeroEffectContext context, string source)
        {
            if (context.State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var candidates = context.Minions.All
                .Where(minion => minion.InPool &&
                                 minion.TavernTier <= Math.Max(1, context.State.Player.Tavern.Tier) &&
                                 (minion.Keywords.Contains(Keyword.Battlecry) ||
                                  minion.Tags.Any(tag => tag.IndexOf("battlecry", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                  (!string.IsNullOrWhiteSpace(minion.Text) && minion.Text.IndexOf("Battlecry", StringComparison.OrdinalIgnoreCase) >= 0)))
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = context.Minions.All.Where(minion => minion.InPool && minion.TavernTier <= Math.Max(1, context.State.Player.Tavern.Tier)).ToList();
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var card = MinionFactory.Create(candidates[context.Rng.NextInt(candidates.Count)], BoardSide.Player, source + "-" + context.State.Round + "-" + context.State.Player.Tavern.Hand.Count, false, PoolSource.Copy, 0);
            AddTag(card, "generated_copy");
            context.State.Player.Tavern.Hand.Add(card);
        }

        private static void InjectFrozenElemental(HeroEffectContext context, HeroEffectResult result)
        {
            var candidates = context.Minions.All
                .Where(minion => minion.InPool && minion.Tribes.Contains(Tribe.Elemental) && minion.TavernTier <= Math.Max(1, context.State.Player.Tavern.Tier))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var card = MinionFactory.Create(candidates[context.Rng.NextInt(candidates.Count)], BoardSide.Player, "snow-elemental-" + context.State.Round + "-" + context.State.Player.Tavern.Shop.Count, false, PoolSource.Copy, 0);
            AddTag(card, "frozen_by_snow_elemental");
            context.State.Player.Tavern.Shop.Add(card);
            TavernShopSlots.SetSlotFrozen(context.State.Player.Tavern, context.State.Player.Tavern.Shop.Count - 1, true);
            result.Messages.Add("Snow Elemental: added an extra Frozen Elemental to the Tavern.");
        }

        private static void RefreshShopWithTavernSpells(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var size = Math.Max(1, tavern.Shop.Count);
            var candidates = context.Spells.All
                .Where(spell => spell.InPool && spell.Category == "TavernSpell" && spell.TavernTier <= Math.Max(1, tavern.Tier))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            tavern.Shop.Clear();
            for (var index = 0; index < size; index += 1)
            {
                var spell = candidates[context.Rng.NextInt(candidates.Count)];
                var card = MinionFactory.Create(spell, BoardSide.Player, "chromie-spell-" + context.State.Round + "-" + index);
                card.PoolSource = PoolSource.Copy;
                card.OriginPoolSource = PoolSource.Copy;
                card.PoolCopiesHeld = 0;
                tavern.Shop.Add(card);
            }

            result.Messages.Add("Mana Per Minute: refreshed the Tavern with Tavern spells.");
        }

        private static void MarkSilasTickets(HeroEffectContext context, HeroEffectResult result)
        {
            var candidates = context.State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion && !card.Tags.Contains(SilasTicketTag))
                .ToList();
            var ticketCount = Math.Min(3, candidates.Count);
            for (var index = 0; index < ticketCount; index += 1)
            {
                var pickIndex = context.Rng.NextInt(candidates.Count);
                var target = candidates[pickIndex];
                candidates.RemoveAt(pickIndex);
                AddTag(target, SilasTicketTag);
                target.Counters[SilasTicketTag] = 1;
            }

            if (ticketCount > 0)
            {
                result.Messages.Add("Come One, Come All!: added " + ticketCount + " Darkmoon Tickets to Tavern minions.");
            }
        }

        private static void ResolveCookieHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            tavern.HeroEffectCounters.TryGetValue(CookieRoundCounter, out var round);
            var uses = round == context.State.Round ? GetCounterOrDefault(tavern, CookieUsesCounter, 0) : 0;

            var fed = TakeCookieTarget(context);
            foreach (var tribe in fed.Tribes.Where(tribe => tribe != Tribe.None && tribe != Tribe.All).Distinct())
            {
                IncrementCounter(tavern, CookieTribeCounterPrefix + tribe, 1);
            }

            tavern.HeroEffectCounters[CookieRoundCounter] = context.State.Round;
            tavern.HeroEffectCounters[CookieUsesCounter] = uses + 1;
            var totalFed = IncrementCounter(tavern, CookieFedCounter, 1);
            if (totalFed >= 3)
            {
                StartCookieDiscover(context);
                ClearCookiePot(tavern);
                result.Messages.Add("Stir the Pot: gathered 3 ingredients and started a type-based Discover.");
            }
            else
            {
                result.Messages.Add("Stir the Pot: fed " + fed.Name + " to the pot (" + (3 - totalFed) + " left).");
            }
        }

        private static MinionInstance TakeCookieTarget(HeroEffectContext context)
        {
            var tavern = context.State.Player.Tavern;
            if (context.TargetIndex >= 0 &&
                context.TargetIndex < tavern.Shop.Count &&
                tavern.Shop[context.TargetIndex] != null &&
                tavern.Shop[context.TargetIndex].CardKind == CardKind.Minion)
            {
                var target = tavern.Shop[context.TargetIndex];
                tavern.Shop[context.TargetIndex] = null;
                return target;
            }

            if (context.TargetIndex >= 0 &&
                context.TargetIndex < context.State.Player.Board.Count &&
                context.State.Player.Board[context.TargetIndex] != null &&
                context.State.Player.Board[context.TargetIndex].CardKind == CardKind.Minion)
            {
                var target = context.State.Player.Board[context.TargetIndex];
                context.State.Player.Board.RemoveAt(context.TargetIndex);
                return target;
            }

            throw new InvalidOperationException("Stir the Pot needs a Tavern or friendly minion target.");
        }

        private static void StartCookieDiscover(HeroEffectContext context)
        {
            var tavern = context.State.Player.Tavern;
            var fedTribes = Enum.GetValues(typeof(Tribe))
                .Cast<Tribe>()
                .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                .Where(tribe => GetCounterOrDefault(tavern, CookieTribeCounterPrefix + tribe, 0) > 0)
                .ToList();
            if (fedTribes.Count == 0)
            {
                StartCurrentTierMinionDiscover(context, "hero-power:stir-the-pot");
                return;
            }

            var candidates = context.Minions.All
                .Where(minion => minion.InPool &&
                                 minion.TavernTier <= Math.Max(1, tavern.Tier) &&
                                 minion.Tribes.Any(tribe => fedTribes.Contains(tribe)))
                .ToList();
            if (candidates.Count < 3)
            {
                candidates = context.Minions.All
                    .Where(minion => minion.InPool &&
                                     minion.TavernTier <= Math.Max(1, tavern.Tier) &&
                                     minion.Tribes.Any(tribe => fedTribes.Contains(tribe)))
                    .ToList();
            }

            StartMinionDiscover(context, candidates, Math.Max(1, tavern.Tier), "hero-power:stir-the-pot");
        }

        private static void ClearCookiePot(TavernState tavern)
        {
            tavern.HeroEffectCounters[CookieFedCounter] = 0;
            foreach (var tribe in Enum.GetValues(typeof(Tribe)).Cast<Tribe>())
            {
                tavern.HeroEffectCounters.Remove(CookieTribeCounterPrefix + tribe);
            }
        }

        private static void ResolveGalakrondHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var target = GetShopTarget(context, "Galakrond's Greed needs a Tavern minion target.");
            if (target.CardKind != CardKind.Minion)
            {
                throw new InvalidOperationException("Galakrond's Greed needs a Tavern minion target.");
            }

            SpendGold(tavern, 1);
            StartHigherTierReplacementDiscover(context, target, context.TargetIndex, "hero-power:galakronds-greed");
            result.Messages.Add("Galakrond's Greed: started a higher-tier replacement Discover.");
        }

        private static void ResolveEtcHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            if (tavern.Tier < 2)
            {
                throw new InvalidOperationException("Sign a New Artist unlocks at Tavern Tier 2.");
            }

            SpendGold(tavern, 3);
            StartBuddyDiscover(context, "hero-power:sign-a-new-artist");
            result.Messages.Add("Sign a New Artist: started a Buddy Discover.");
        }

        private static void StartHigherTierReplacementDiscover(HeroEffectContext context, MinionInstance target, int shopIndex, string source)
        {
            var tier = Math.Max(1, target.TavernTier + 1);
            var candidates = context.Minions.All
                .Where(minion => minion.InPool && minion.TavernTier == tier && minion.CardId != target.CardId)
                .ToList();
            if (candidates.Count < 3)
            {
                candidates = context.Minions.All
                    .Where(minion => minion.InPool && minion.TavernTier > Math.Max(0, target.TavernTier) && minion.CardId != target.CardId)
                    .ToList();
            }

            var discover = StartMinionDiscover(context, candidates, tier, source);
            if (discover != null)
            {
                discover.TargetInstanceId = target.InstanceId;
                discover.RemainingPicks = shopIndex;
            }
        }

        private static void StartBuddyDiscover(HeroEffectContext context, string source)
        {
            if (context.Heroes == null)
            {
                return;
            }

            var candidates = context.Heroes.AllBuddies
                .Where(buddy => !buddy.ExcludedFromBuddyDiscover)
                .ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = context.Rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, source + "-" + options.Count, PoolSource.Discover));
            }

            context.State.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = 0,
                Options = options
            });
        }

        private static void StartHeroPowerDiscover(HeroEffectContext context, string source)
        {
            if (context.Heroes == null)
            {
                return;
            }

            var candidates = context.Heroes.GetOfferableDiscoverableHeroPowers(context.State.Player.HeroPowerCardId);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = context.Rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(context.Heroes.CreateDiscoverableHeroPowerOption(definition, BoardSide.Player, source + "-" + options.Count));
            }

            context.State.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = 0,
                Options = options
            });
        }

        private static void ReplaceShopCardsOneTierHigher(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var replaced = 0;
            for (var index = 0; index < tavern.Shop.Count; index += 1)
            {
                var card = tavern.Shop[index];
                if (card == null || card.CardKind != CardKind.Minion)
                {
                    continue;
                }

                tavern.Shop[index] = CreateRandomReplacement(context, card, true, "galakrond-apostle");
                replaced += 1;
            }

            if (replaced > 0)
            {
                result.Messages.Add("Apostle of Galakrond: replaced " + replaced + " Tavern minions with higher-tier minions.");
            }
        }

        private static void MakeTargetBuddyGolden(HeroEffectContext context, HeroEffectResult result)
        {
            var target = PickTargetedOrFirstOtherBoardMinion(context, card => card.CardKind == CardKind.HeroBuddy);
            if (target == null)
            {
                result.Messages.Add("Talent Scout: no Buddy target available.");
                return;
            }

            MakeGoldenInPlace(target, context.Minions);
            result.Messages.Add("Talent Scout: made a Buddy Golden.");
        }

        private static void AddBuddyForCurrentHeroPowerToHand(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            if (context.Heroes == null || tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            context.Heroes.TryGetHeroByHeroPowerCardId(context.State.Player.HeroPowerCardId, out var hero);
            if (hero?.Buddy == null)
            {
                result.Messages.Add("Maxwell, Mighty Steed: current Hero Power has no Buddy mapping.");
                return;
            }

            var buddy = MinionFactory.Create(hero.Buddy, BoardSide.Player, "maxwell-" + context.State.Round + "-" + tavern.Hand.Count, PoolSource.Copy);
            AddTag(buddy, "generated_copy");
            tavern.Hand.Add(buddy);
            result.Messages.Add("Maxwell, Mighty Steed: gained the Buddy of your Hero Power.");
        }

        private static bool AddHelpfulCardToHand(HeroEffectContext context, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            if (AddRandomTavernSpellToHand(context, 1, Math.Max(1, tavern.Tier), source))
            {
                return true;
            }

            var tier = Math.Max(1, tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion => minion.InPool && minion.TavernTier <= tier)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var card = MinionFactory.Create(candidates[context.Rng.NextInt(candidates.Count)], BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count, false, PoolSource.Copy, 0);
            AddTag(card, "helpful_card");
            tavern.Hand.Add(card);
            return true;
        }

        private static void AddStartingBattlecruiser(HeroEffectContext context, HeroEffectResult result)
        {
            if (context.State.Player.Board.Count >= BoardLimit ||
                context.State.Player.Board.Any(card => card.Tags.Contains("battlecruiser")))
            {
                return;
            }

            context.State.Player.Board.Add(CreateBattlecruiser("start-" + context.State.Round));
            result.Messages.Add("Lift Off: started with a 2/2 Battlecruiser.");
        }

        private static MinionInstance CreateBattlecruiser(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-battlecruiser-" + suffix,
                DefinitionId = BattlecruiserCardId,
                CardId = BattlecruiserCardId,
                Name = "Battlecruiser",
                Cost = 0,
                BaseAttack = 2,
                BaseHealth = 2,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.Mech },
                Keywords = new List<Keyword>(),
                Text = "Terran Battlecruiser. Battlecruiser Upgrades modify this minion.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "battlecruiser", "terran_battlecruiser", RaynorStartingBattlecruiserTag }
            };
        }

        private static MinionInstance CurrentBattlecruiser(MatchState state)
        {
            var board = state?.Player?.Board;
            if (board == null)
            {
                return null;
            }

            return board.FirstOrDefault(card => card?.Tags?.Contains(RaynorStartingBattlecruiserTag) == true) ??
                   board.FirstOrDefault(card =>
                       card != null &&
                       (card.Tags.Contains("battlecruiser") ||
                        string.Equals(card.CardId, BattlecruiserCardId, StringComparison.OrdinalIgnoreCase)));
        }

        private static void AddBattlecruiserUpgradeToShop(HeroEffectContext context, HeroEffectResult result)
        {
            if (!string.Equals(context.State.Player.HeroId, RaynorHeroId, StringComparison.OrdinalIgnoreCase) ||
                !context.State.Player.Board.Any(card => card?.Tags?.Contains(RaynorStartingBattlecruiserTag) == true))
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            var card = CreateRandomBattlecruiserUpgrade(context, "shop");
            var insertIndex = tavern.Shop.FindIndex(slot => slot == null);
            if (insertIndex >= 0)
            {
                tavern.Shop[insertIndex] = card;
            }
            else
            {
                tavern.Shop.Add(card);
            }

            result.Messages.Add("Lift Off: added a Battlecruiser Upgrade to the Tavern.");
        }

        private static void AddBattlecruiserUpgradeToHand(HeroEffectContext context, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            tavern.Hand.Add(CreateRandomBattlecruiserUpgrade(context, source));
        }

        private static MinionInstance CreateRandomBattlecruiserUpgrade(HeroEffectContext context, string source)
        {
            var family = BattlecruiserUpgradeFamilies[context.Rng.NextInt(BattlecruiserUpgradeFamilies.Length)];
            var battlecruiser = CurrentBattlecruiser(context.State);
            var level = battlecruiser != null && battlecruiser.Counters.TryGetValue("battlecruiser_upgrade:" + family, out var current)
                ? current + 1
                : 1;
            return CreateBattlecruiserUpgradeCard(ResolveBattlecruiserUpgradeCardId(family, level), source, context.State.Round, context.State.Player.Tavern.Hand.Count);
        }

        private static string ResolveBattlecruiserUpgradeCardId(string family, int level)
        {
            var capped = Math.Max(1, level);
            if (family == "BG31_HERO_801pti")
            {
                return capped <= 1 ? family : family + "2";
            }

            if (family == "BG31_HERO_801ptj")
            {
                return capped <= 1 ? family : family + "2";
            }

            if (family == "BG31_HERO_801ptf" || family == "BG31_HERO_801pth")
            {
                capped = Math.Min(4, capped);
                return capped == 1 ? family : family + capped;
            }

            capped = Math.Min(7, capped);
            return capped == 1 ? family : family + capped;
        }

        private static MinionInstance CreateBattlecruiserUpgradeCard(string cardId, string source, int round, int sequence)
        {
            var name = BattlecruiserUpgradeName(cardId);
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-battlecruiser-upgrade-" + source + "-" + round + "-" + sequence,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = BattlecruiserUpgradeCost(cardId),
                TavernTier = BattlecruiserUpgradeTier(cardId),
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = BattlecruiserUpgradeText(cardId),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_spell", "battlecruiser_upgrade", "terran_battlecruiser_upgrade" }
            };
        }

        private static string BattlecruiserUpgradeName(string cardId)
        {
            if (cardId.StartsWith("BG31_HERO_801pta", StringComparison.OrdinalIgnoreCase)) return "Hyperflight Rotors";
            if (cardId.StartsWith("BG31_HERO_801ptb", StringComparison.OrdinalIgnoreCase)) return "Smart Servos";
            if (cardId.StartsWith("BG31_HERO_801ptc", StringComparison.OrdinalIgnoreCase)) return "Yamato Cannon";
            if (cardId.StartsWith("BG31_HERO_801ptd", StringComparison.OrdinalIgnoreCase)) return "Advanced Ballistics";
            if (cardId.StartsWith("BG31_HERO_801pte", StringComparison.OrdinalIgnoreCase)) return "Caduceus Reactor";
            if (cardId.StartsWith("BG31_HERO_801ptf", StringComparison.OrdinalIgnoreCase)) return "Advanced Construction";
            if (cardId.StartsWith("BG31_HERO_801pth", StringComparison.OrdinalIgnoreCase)) return "Fortified Bunker";
            if (cardId.StartsWith("BG31_HERO_801pti", StringComparison.OrdinalIgnoreCase)) return "Missile Pod";
            if (cardId.StartsWith("BG31_HERO_801ptj", StringComparison.OrdinalIgnoreCase)) return "Ultra-Capacitor";
            return "Battlecruiser Upgrade";
        }

        private static int BattlecruiserUpgradeCost(string cardId)
        {
            if (cardId.StartsWith("BG31_HERO_801ptf", StringComparison.OrdinalIgnoreCase)) return 3;
            if (cardId.StartsWith("BG31_HERO_801pth", StringComparison.OrdinalIgnoreCase)) return 4;
            if (cardId.StartsWith("BG31_HERO_801pti", StringComparison.OrdinalIgnoreCase)) return 5;
            if (cardId.StartsWith("BG31_HERO_801ptj", StringComparison.OrdinalIgnoreCase)) return 6;
            return 2;
        }

        private static int BattlecruiserUpgradeTier(string cardId)
        {
            if (cardId.StartsWith("BG31_HERO_801ptf", StringComparison.OrdinalIgnoreCase)) return 4;
            if (cardId.StartsWith("BG31_HERO_801pth", StringComparison.OrdinalIgnoreCase)) return 5;
            if (cardId.StartsWith("BG31_HERO_801pti", StringComparison.OrdinalIgnoreCase) ||
                cardId.StartsWith("BG31_HERO_801ptj", StringComparison.OrdinalIgnoreCase)) return 6;
            return 1;
        }

        private static string BattlecruiserUpgradeText(string cardId)
        {
            var name = BattlecruiserUpgradeName(cardId);
            return name + ": upgrade your Battlecruiser.";
        }

        private static void ResolveRaynorTurnEnded(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (!IsPower(powerId, RaynorPowerId))
            {
                return;
            }

            var battlecruiser = CurrentBattlecruiser(context.State);
            if (battlecruiser == null || !battlecruiser.Tags.Contains("battlecruiser_bunker_magnetic"))
            {
                return;
            }

            if (AddRandomMagneticMechToHand(context, "battlecruiser-bunker"))
            {
                result.Messages.Add("Fortified Bunker: gained a random Magnetic Mech.");
            }
        }

        private static void AddStartingLarva(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            tavern.HeroEffectCounters[KerriganUnlockedTierCounter] = 2;
            tavern.HeroEffectCounters[KerriganCostCounter] = 6;
            if (context.State.Player.Board.Count >= BoardLimit ||
                context.State.Player.Board.Any(card => string.Equals(card.CardId, ZergLarvaCardId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var larva = CreateZergMinion(ZergLarvaCardId, "start-" + context.State.Round, false);
            AddTag(larva, "hero_derivative");
            AddTag(larva, "hero_derivative:kerrigan");
            context.State.Player.Board.Add(larva);
            result.Messages.Add("Spawning Pool: started with a 2/2 Larva.");
        }

        private static void ReduceKerriganHeroPowerCost(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            var tavern = context.State.Player.Tavern;
            var fallback = IsPower(powerId, KerriganPowerTier3Id) ? 8 : IsPower(powerId, KerriganPowerFinalId) ? 0 : 6;
            var current = GetCounterOrDefault(tavern, KerriganCostCounter, fallback);
            if (current > 0)
            {
                tavern.HeroEffectCounters[KerriganCostCounter] = current - 1;
                result.Messages.Add("Spawning Pool: Hero Power cost reduced to " + (current - 1) + ".");
            }
        }

        private static void UseKerriganHeroPower(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            var tavern = context.State.Player.Tavern;
            var cost = GetCounterOrDefault(tavern, KerriganCostCounter, IsPower(powerId, KerriganPowerTier3Id) ? 8 : 6);
            SpendGold(tavern, Math.Max(0, cost));
            if (IsPower(powerId, KerriganPowerTier2Id))
            {
                tavern.HeroEffectCounters[KerriganUnlockedTierCounter] = 3;
                tavern.HeroEffectCounters[KerriganCostCounter] = 8;
                context.State.Player.HeroPowerCardId = KerriganPowerTier3Id;
                result.Messages.Add("Spawning Pool: Tier 2 Zerg unlocked; Evolution Chamber is ready.");
            }
            else if (IsPower(powerId, KerriganPowerTier3Id))
            {
                tavern.HeroEffectCounters[KerriganUnlockedTierCounter] = 4;
                tavern.HeroEffectCounters[KerriganCostCounter] = 0;
                context.State.Player.HeroPowerCardId = KerriganPowerFinalId;
                result.Messages.Add("Evolution Chamber: Tier 3 Zerg unlocked; Ultralisk Cavern is ready.");
            }
            else
            {
                tavern.HeroEffectCounters[KerriganUnlockedTierCounter] = Math.Max(4, GetCounterOrDefault(tavern, KerriganUnlockedTierCounter, 4));
                result.Messages.Add("Ultralisk Cavern: Zerg morphing is fully unlocked.");
            }
        }

        private static void StartKerriganMorphDiscovers(HeroEffectContext context, HeroEffectResult result)
        {
            var unlocked = Math.Max(2, GetCounterOrDefault(context.State.Player.Tavern, KerriganUnlockedTierCounter, 2));
            var targets = context.State.Player.Board
                .Where(card => IsMorphingZerg(card))
                .ToList();
            foreach (var target in targets)
            {
                var options = CreateZergMorphOptions(context, unlocked, target.InstanceId);
                if (options.Count == 0)
                {
                    continue;
                }

                context.State.Player.Tavern.QueueDiscover(new DiscoverState
                {
                    Source = KerriganMorphDiscoverSource,
                    TargetInstanceId = target.InstanceId,
                    RewardTier = unlocked,
                    Options = options
                });
            }

            if (targets.Count > 0)
            {
                result.Messages.Add("Kerrigan: queued Zerg morph choices for " + targets.Count + " morphing Zerg.");
            }
        }

        private static bool IsMorphingZerg(MinionInstance card)
        {
            return card != null &&
                   card.Tags != null &&
                   card.Tags.Contains("zerg_morphing") &&
                   !card.Tags.Contains("does_not_morph");
        }

        private static List<MinionInstance> CreateZergMorphOptions(HeroEffectContext context, int unlockedTier, string targetInstanceId)
        {
            var ids = new List<string>();
            if (unlockedTier >= 2)
            {
                ids.AddRange(ZergTier2CardIds);
            }

            if (unlockedTier >= 4)
            {
                ids.AddRange(ZergTier3CardIds);
            }

            var options = new List<MinionInstance>();
            var pool = ids
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            while (options.Count < 3 && pool.Count > 0)
            {
                var index = context.Rng.NextInt(pool.Count);
                var id = pool[index];
                pool.RemoveAt(index);
                var option = CreateZergMinion(id, "morph-" + targetInstanceId + "-" + options.Count, false);
                option.PoolSource = PoolSource.Discover;
                option.OriginPoolSource = PoolSource.Discover;
                options.Add(option);
            }

            return options;
        }

        private static MinionInstance CreateZergMinion(string cardId, string suffix, bool noMorph)
        {
            var name = ZergName(cardId);
            var card = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-zerg-" + cardId + "-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = 0,
                BaseAttack = 2,
                BaseHealth = 2,
                Attack = noMorph ? 6 : 2,
                Health = noMorph ? 6 : 2,
                MaxHealth = noMorph ? 6 : 2,
                TavernTier = ZergTier(cardId),
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = ZergText(cardId),
                Owner = BoardSide.Player,
                PoolSource = noMorph ? PoolSource.Discover : PoolSource.Copy,
                OriginPoolSource = noMorph ? PoolSource.Discover : PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "zerg_minion", "zerg_morphing" }
            };
            ConfigureZergKeywords(card);
            if (noMorph)
            {
                AddTag(card, "does_not_morph");
            }

            return card;
        }

        private static string ZergName(string cardId)
        {
            switch (cardId)
            {
                case "BG31_HERO_811t2": return "Zergling";
                case "BG31_HERO_811t3": return "Roach";
                case "BG31_HERO_811t4": return "Hydralisk";
                case "BG31_HERO_811t5": return "Baneling";
                case "BG31_HERO_811t6": return "Mutalisk";
                case "BG31_HERO_811t7": return "Lurker";
                case "BG31_HERO_811t8": return "Viper";
                case "BG31_HERO_811t9": return "Infestor";
                case "BG31_HERO_811t10": return "Ultralisk";
                default: return "Larva";
            }
        }

        private static int ZergTier(string cardId)
        {
            if (ZergTier3CardIds.Contains(cardId))
            {
                return 3;
            }

            if (ZergTier2CardIds.Contains(cardId))
            {
                return 2;
            }

            return 1;
        }

        private static string ZergText(string cardId)
        {
            return ZergName(cardId) + ". Morphs each turn unless created by Broken Horn.";
        }

        private static void ConfigureZergKeywords(MinionInstance card)
        {
            if (card.CardId == "BG31_HERO_811t3")
            {
                AddKeyword(card, Keyword.Taunt, "Roach");
            }
            else if (card.CardId == "BG31_HERO_811t4")
            {
                AddKeyword(card, Keyword.Windfury, "Hydralisk");
                AddKeyword(card, Keyword.Rally, "Hydralisk");
            }
            else if (card.CardId == "BG31_HERO_811t5")
            {
                AddKeyword(card, Keyword.Deathrattle, "Baneling");
            }
            else if (card.CardId == "BG31_HERO_811t7")
            {
                AddKeyword(card, Keyword.Stealth, "Lurker");
                AddKeyword(card, Keyword.Avenge, "Lurker");
                card.Counters["avenge_threshold"] = 1;
            }
            else if (card.CardId == "BG31_HERO_811t8")
            {
                AddKeyword(card, Keyword.Venomous, "Viper");
            }
            else if (card.CardId == "BG31_HERO_811t10")
            {
                AddKeyword(card, Keyword.Cleave, "Ultralisk");
            }
        }

        private static void ResolveKerriganTurnEnded(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (!IsPower(powerId, KerriganPowerTier2Id) &&
                !IsPower(powerId, KerriganPowerTier3Id) &&
                !IsPower(powerId, KerriganPowerFinalId))
            {
                return;
            }

            foreach (var roach in context.State.Player.Board.Where(card => string.Equals(card.CardId, "BG31_HERO_811t3", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                var amount = Math.Max(1, context.State.Player.Tavern.Tier) * (roach.Golden ? 2 : 1);
                Buff(roach, 0, amount, "Roach");
                result.Messages.Add("Roach: gained Health equal to your Tavern Tier.");
            }
        }

        private static void ResolveKerriganCardPlayed(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (!IsPower(powerId, KerriganPowerTier2Id) &&
                !IsPower(powerId, KerriganPowerTier3Id) &&
                !IsPower(powerId, KerriganPowerFinalId))
            {
                return;
            }

            var infest = context.State.Player.Board
                .Where(card => string.Equals(card.CardId, "BG31_HERO_811t9", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var infestor in infest)
            {
                var amount = infestor.Golden ? 2 : 1;
                foreach (var minion in context.State.Player.Board.Where(card => card.CardKind == CardKind.Minion))
                {
                    Buff(minion, amount, amount, "Infestor");
                }

                result.Messages.Add("Infestor: your minions gained +" + amount + "/+" + amount + ".");
            }
        }

        private static void StartArtanisProtossDiscover(HeroEffectContext context, HeroEffectResult result)
        {
            var options = new List<MinionInstance>();
            var pool = ProtossRewardCardIds.ToList();
            while (options.Count < 2 && pool.Count > 0)
            {
                var index = context.Rng.NextInt(pool.Count);
                var id = pool[index];
                pool.RemoveAt(index);
                options.Add(CreateProtossReward(id, "choice-" + options.Count, false));
            }

            if (options.Count == 0)
            {
                return;
            }

            context.State.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = ArtanisProtossDiscoverSource,
                RewardTier = 5,
                Options = options
            });
            context.State.Player.Tavern.HeroEffectCounters[ArtanisBoughtCounter] = 0;
            context.State.Player.Tavern.HeroEffectCounters[ArtanisRewardClaimedCounter] = 0;
            result.Messages.Add("Warp Gate: started a Protoss reward choice.");
        }

        private static void AdvanceArtanisBuyReward(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            if (GetCounterOrDefault(tavern, ArtanisRewardClaimedCounter, 0) > 0 ||
                !tavern.AdvancedMechanics.Selections.TryGetValue(ArtanisSelectedRewardKey, out var selected) ||
                string.IsNullOrEmpty(selected))
            {
                return;
            }

            var bought = IncrementCounter(tavern, ArtanisBoughtCounter, 1);
            if (bought < 14)
            {
                return;
            }

            if (tavern.Hand.Count >= HandLimit)
            {
                result.Messages.Add("Warp Gate: reward is ready, but your hand is full.");
                return;
            }

            tavern.Hand.Add(CreateProtossReward(selected, "reward-" + context.State.Round, false));
            tavern.HeroEffectCounters[ArtanisRewardClaimedCounter] = 1;
            result.Messages.Add("Warp Gate: gained the selected Protoss minion after buying 14 cards.");
        }

        private static MinionInstance CreateProtossReward(string cardId, string suffix, bool golden)
        {
            var stats = ProtossStats(cardId, golden);
            var card = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-protoss-" + cardId + "-" + suffix,
                DefinitionId = golden ? cardId + "_G" : cardId,
                CardId = cardId,
                Name = ProtossName(cardId),
                Cost = 0,
                BaseAttack = stats.Attack,
                BaseHealth = stats.Health,
                Attack = stats.Attack,
                Health = stats.Health,
                MaxHealth = stats.Health,
                TavernTier = 5,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = "Protoss reward from Warp Gate.",
                Golden = golden,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "protoss_reward", "starcraft_protoss" }
            };
            ConfigureProtossKeywords(card);
            return card;
        }

        private static (int Attack, int Health) ProtossStats(string cardId, bool golden)
        {
            switch (cardId)
            {
                case "BG31_HERO_802pt": return golden ? (12, 24) : (6, 12);
                case "BG31_HERO_802pt1": return golden ? (8, 24) : (4, 12);
                case "BG31_HERO_802pt4": return golden ? (16, 16) : (8, 8);
                case "BG31_HERO_802pt5": return golden ? (14, 2) : (7, 1);
                case "BG31_HERO_802pt7": return golden ? (12, 16) : (6, 8);
                default: return (6, 6);
            }
        }

        private static string ProtossName(string cardId)
        {
            switch (cardId)
            {
                case "BG31_HERO_802pt": return "Colossus";
                case "BG31_HERO_802pt1": return "Carrier";
                case "BG31_HERO_802pt4": return "Immortal";
                case "BG31_HERO_802pt5": return "Void Ray";
                case "BG31_HERO_802pt7": return "Mothership";
                default: return "Protoss";
            }
        }

        private static void ConfigureProtossKeywords(MinionInstance card)
        {
            if (card.CardId == "BG31_HERO_802pt")
            {
                AddKeyword(card, Keyword.Rally, "Colossus");
            }
            else if (card.CardId == "BG31_HERO_802pt1" || card.CardId == "BG31_HERO_802pt7")
            {
                AddKeyword(card, Keyword.Avenge, card.Name);
                card.Counters["avenge_threshold"] = 4;
            }
            else if (card.CardId == "BG31_HERO_802pt4")
            {
                AddKeyword(card, Keyword.StartOfCombat, "Immortal");
            }
            else if (card.CardId == "BG31_HERO_802pt5")
            {
                AddKeyword(card, Keyword.DivineShield, "Void Ray");
            }
        }

        private static void UsePutricideHeroPower(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var left = GetCounterOrDefault(tavern, PutricideCreationsLeftCounter, 3);
            if (left <= 0)
            {
                throw new InvalidOperationException("Build-An-Undead has no creations left.");
            }

            if (tavern.Hand.Count >= HandLimit)
            {
                throw new InvalidOperationException("Hand is full.");
            }

            SpendGold(tavern, 3);
            tavern.HeroEffectCounters[PutricideCreationsLeftCounter] = left - 1;
            tavern.QueueDiscover(new DiscoverState
            {
                Source = PutricideFirstDiscoverSource,
                RewardTier = 0,
                Options = CreatePutricideComponentOptions(context, null, "hero-power-first")
            });
            result.Messages.Add("Build-An-Undead: started a two-part Undead Creation. " + (left - 1) + " creation(s) left.");
        }

        private static void AddBetterSecretProxyToHand(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            tavern.Hand.Add(new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-better-secret-" + context.State.Round + "-" + tavern.Hand.Count,
                DefinitionId = "better-secret-proxy",
                CardId = "BETTER_SECRET_PROXY",
                Name = "Better Secret",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = "Secret proxy: give your left-most minion +2/+2.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_spell", "secret_proxy", "better_secret" }
            });
            result.Messages.Add("Street Magician: created a Better Secret proxy; full Secret battlefield support is still deferred.");
        }

        private static void SummonAndGetUndeadCreation(HeroEffectContext context, HeroEffectResult result)
        {
            var count = context.Card != null && context.Card.Golden ? 2 : 1;
            var resolved = 0;
            for (var index = 0; index < count; index += 1)
            {
                if (AddRandomUndeadCreationToBoard(context, "festergut-summon-" + index))
                {
                    resolved += 1;
                }

                if (AddRandomUndeadCreationToHand(context, "festergut-hand-" + index))
                {
                    resolved += 1;
                }
            }

            if (resolved > 0)
            {
                result.Messages.Add("Festergut: summoned and gained Putricide's Creation through the shared factory.");
            }
        }

        private static bool AddRandomUndeadCreationToBoard(HeroEffectContext context, string source)
        {
            if (context.State.Player.Board.Count >= BoardLimit)
            {
                return false;
            }

            var card = CreateRandomUndeadCreation(context, source);
            if (card == null)
            {
                return false;
            }

            context.State.Player.Board.Add(card);
            return true;
        }

        private static bool AddRandomUndeadCreationToHand(HeroEffectContext context, string source)
        {
            if (context.State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var card = CreateRandomUndeadCreation(context, source);
            if (card == null)
            {
                return false;
            }

            context.State.Player.Tavern.Hand.Add(card);
            return true;
        }

        private static MinionInstance CreateRandomUndeadCreation(HeroEffectContext context, string source)
        {
            if (PutricideComponents.Length == 0)
            {
                return null;
            }

            var first = PutricideComponents[context.Rng.NextInt(PutricideComponents.Length)];
            var secondCandidates = GetPutricideComponents(null)
                .Where(component => IsPutricideComponentAllowedAfterFirst(component, first))
                .ToList();
            if (secondCandidates.Count == 0)
            {
                secondCandidates = GetPutricideComponents(null).ToList();
            }

            var second = secondCandidates[context.Rng.NextInt(secondCandidates.Count)];
            return CreatePutricideCreationFromComponents(first.Id, second.Id, source + "-" + context.State.Round);
        }

        public static List<MinionInstance> CreatePutricideComponentOptions(HeroEffectContext context, string firstComponentId, string source)
        {
            var candidates = GetPutricideComponents(firstComponentId).ToList();
            var options = new List<MinionInstance>();
            var take = Math.Min(3, candidates.Count);
            for (var index = 0; index < take; index += 1)
            {
                var selectedIndex = context.Rng.NextInt(candidates.Count);
                var component = candidates[selectedIndex];
                candidates.RemoveAt(selectedIndex);
                options.Add(CreatePutricideComponentOption(component, source, context.State.Round, index));
            }

            return options;
        }

        public static string GetPutricideComponentId(MinionInstance option)
        {
            if (option == null || option.Tags == null)
            {
                return null;
            }

            var tag = option.Tags.FirstOrDefault(value => value != null && value.StartsWith(PutricideComponentTagPrefix, StringComparison.Ordinal));
            return tag == null ? null : tag.Substring(PutricideComponentTagPrefix.Length);
        }

        public static MinionInstance CreatePutricideCreationFromComponents(string firstComponentId, string secondComponentId, string source)
        {
            var first = FindPutricideComponent(firstComponentId);
            var second = FindPutricideComponent(secondComponentId);
            if (first == null || second == null)
            {
                return null;
            }

            var attack = first.Attack + second.Attack;
            var health = first.Health + second.Health;
            var card = CreateGeneratedMinion(PutricideCreationCardId, "Putricide's Creation", attack, health, Tribe.Undead, source);
            card.DefinitionId = PutricideCreationCardId;
            card.Text = "Custom Undead Creation: " + first.Name + " + " + second.Name + ". " + first.Text + " " + second.Text;
            AddTag(card, "undead_creation");
            AddTag(card, PutricideCreationTag);
            ApplyPutricideComponent(card, first);
            ApplyPutricideComponent(card, second);

            return card;
        }

        private static IEnumerable<PutricideCreationComponent> GetPutricideComponents(string firstComponentId)
        {
            var first = FindPutricideComponent(firstComponentId);
            foreach (var component in PutricideComponents)
            {
                if (component == first)
                {
                    continue;
                }

                if (IsPutricideComponentAllowedAfterFirst(component, first))
                {
                    yield return component;
                }
            }
        }

        private static bool IsPutricideComponentAllowedAfterFirst(PutricideCreationComponent component, PutricideCreationComponent first)
        {
            return component != null &&
                (first == null ||
                 !component.Keyword.HasValue ||
                 !first.Keyword.HasValue ||
                 component.Keyword.Value != first.Keyword.Value);
        }

        private static PutricideCreationComponent FindPutricideComponent(string componentId)
        {
            if (string.IsNullOrEmpty(componentId))
            {
                return null;
            }

            return PutricideComponents.FirstOrDefault(component =>
                string.Equals(component.Id, componentId, StringComparison.OrdinalIgnoreCase));
        }

        private static MinionInstance CreatePutricideComponentOption(PutricideCreationComponent component, string source, int round, int index)
        {
            var option = CreateGeneratedMinion(
                "PUTRICIDE_COMPONENT_" + component.Id.ToUpperInvariant().Replace("-", "_"),
                component.Name,
                component.Attack,
                component.Health,
                Tribe.Undead,
                source + "-" + round + "-" + index);
            option.DefinitionId = option.CardId;
            option.CardKind = CardKind.Spell;
            option.Cost = 0;
            option.TavernTier = 0;
            option.Text = component.Text;
            option.PoolSource = PoolSource.Discover;
            option.OriginPoolSource = PoolSource.Discover;
            AddTag(option, "putricide_component_option");
            AddTag(option, PutricideComponentTagPrefix + component.Id);
            if (!string.IsNullOrEmpty(component.EffectTag))
            {
                AddTag(option, component.EffectTag);
            }

            if (component.Keyword.HasValue)
            {
                AddKeyword(option, component.Keyword.Value, component.Name);
            }

            return option;
        }

        private static void ApplyPutricideComponent(MinionInstance card, PutricideCreationComponent component)
        {
            AddTag(card, PutricideComponentTagPrefix + component.Id);
            if (!string.IsNullOrEmpty(component.EffectTag))
            {
                AddTag(card, component.EffectTag);
            }

            if (component.Keyword.HasValue)
            {
                AddKeyword(card, component.Keyword.Value, component.Name);
            }
        }

        private static void StartBrokenHornDiscover(HeroEffectContext context, HeroEffectResult result)
        {
            var copies = context.Card != null && context.Card.Golden ? 2 : 1;
            for (var pick = 0; pick < copies; pick += 1)
            {
                var unlocked = Math.Max(2, GetCounterOrDefault(context.State.Player.Tavern, KerriganUnlockedTierCounter, 2));
                var options = CreateZergMorphOptions(context, unlocked >= 4 ? 4 : unlocked, "broken-horn-" + pick)
                    .Select(card =>
                    {
                        card.Attack = 6;
                        card.Health = 6;
                        card.MaxHealth = 6;
                        card.BaseAttack = 6;
                        card.BaseHealth = 6;
                        AddTag(card, "does_not_morph");
                        AddTag(card, "broken_horn_zerg");
                        return card;
                    })
                    .ToList();
                context.State.Player.Tavern.QueueDiscover(new DiscoverState
                {
                    Source = "buddy:broken-horn",
                    RewardTier = 0,
                    Options = options
                });
            }

            result.Messages.Add("Broken Horn: started " + copies + " Zerg Discover(s) set to 6/6.");
        }

        private static bool AddOpponentBuddyToHand(HeroEffectContext context, string heroId, string source, string tag)
        {
            var tavern = context.State.Player.Tavern;
            if (context.Heroes == null || tavern.Hand.Count >= HandLimit || string.IsNullOrWhiteSpace(heroId))
            {
                return false;
            }

            var hero = context.Heroes.AllHeroes.FirstOrDefault(candidate =>
                string.Equals(candidate.HeroCardId, heroId, StringComparison.OrdinalIgnoreCase));
            if (hero?.Buddy == null)
            {
                return false;
            }

            var buddy = MinionFactory.Create(hero.Buddy, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count, PoolSource.Copy);
            AddTag(buddy, string.IsNullOrEmpty(tag) ? "opponent_buddy_proxy" : tag);
            tavern.Hand.Add(buddy);
            return true;
        }

        private static string GetLastOpponentHeroId(MatchState state)
        {
            return string.IsNullOrWhiteSpace(state?.OpponentHistory?.LastOpponentHeroId)
                ? state?.Opponent?.HeroId
                : state.OpponentHistory.LastOpponentHeroId;
        }

        private static string GetNextOpponentHeroId(MatchState state)
        {
            return string.IsNullOrWhiteSpace(state?.Opponent?.HeroId)
                ? state?.OpponentHistory?.LastOpponentHeroId
                : state.Opponent.HeroId;
        }

        private static void CopyLeftmostHandCard(MatchState state, string source, MinionCatalog catalog)
        {
            if (state.Player.Tavern.Hand.Count == 0 || state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            AddPlainCopyToHand(state, state.Player.Tavern.Hand[0], source, catalog);
        }

        private static void BuffLeftAndRightMostMinions(MatchState state, int attack, int health, string source)
        {
            var minions = state.Player.Board.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            if (minions.Count == 0)
            {
                return;
            }

            Buff(minions.First(), attack, health, source);
            if (minions.Count > 1)
            {
                Buff(minions.Last(), attack, health, source);
            }
        }

        private static void StartSingletonPlainCopyDiscover(HeroEffectContext context, HeroEffectResult result, string excludedInstanceId)
        {
            var grouped = context.State.Player.Board.Concat(context.State.Player.Tavern.Hand)
                .Where(card => card != null &&
                               card.CardKind == CardKind.Minion &&
                               card.InstanceId != excludedInstanceId)
                .GroupBy(card => card.CardId)
                .Where(group => group.Count() == 1)
                .Select(group => group.First())
                .ToList();
            var options = grouped.Take(3).Select(card =>
            {
                var copy = card.Clone();
                copy.InstanceId = "player-phyresz-discover-" + context.State.Round + "-" + card.CardId;
                copy.Golden = false;
                context.Minions?.TrySyncGoldenText(copy);
                copy.Attack = card.BaseAttack;
                copy.Health = card.BaseHealth;
                copy.MaxHealth = card.BaseHealth;
                copy.Enchantments = new List<Enchantment>();
                copy.PoolSource = PoolSource.Discover;
                copy.OriginPoolSource = PoolSource.Discover;
                copy.PoolCopiesHeld = 0;
                AddTag(copy, "plain_copy");
                return copy;
            }).ToList();
            if (options.Count == 0)
            {
                return;
            }

            context.State.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = "buddy:phyresz",
                RewardTier = Math.Max(1, context.State.Player.Tavern.Tier),
                Options = options
            });
            result.Messages.Add("Phyresz: started a plain-copy Discover.");
        }

        private static void AddDerylHats(MinionInstance target, int count, string source)
        {
            if (target == null || count <= 0)
            {
                return;
            }

            target.Counters["deryl_hats"] = (target.Counters.TryGetValue("deryl_hats", out var hats) ? hats : 0) + count;
            Buff(target, count, count, source);
        }

        private static void PassDerylHats(MatchState state, MinionInstance sold, int count, HeroEffectResult result)
        {
            if (count <= 0)
            {
                return;
            }

            var candidates = state.Player.Board
                .Where(card => card != null &&
                               card.CardKind == CardKind.Minion &&
                               (sold == null || card.InstanceId != sold.InstanceId))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var target = candidates[0];
            AddDerylHats(target, count, "Deryl hats");
            result.Messages.Add("Hat Trick: passed " + count + " hats to a friendly minion.");
        }

        private static void TransformIntoSelectedShopMinion(MinionInstance target, HeroEffectContext context)
        {
            var source = GetSelectedShopTarget(context, "Mini-Zerek needs a Tavern minion target.");
            var instanceId = target.InstanceId;
            var copy = source.Clone();
            target.CardKind = copy.CardKind;
            target.DefinitionId = copy.DefinitionId;
            target.CardId = copy.CardId;
            target.Name = copy.Name;
            target.Cost = copy.Cost;
            target.BaseAttack = copy.BaseAttack;
            target.BaseHealth = copy.BaseHealth;
            target.Attack = copy.Attack;
            target.Health = copy.Health;
            target.MaxHealth = copy.MaxHealth;
            target.TavernTier = copy.TavernTier;
            target.Tribes = new List<Tribe>(copy.Tribes);
            target.Keywords = new List<Keyword>(copy.Keywords);
            target.OfficialKeywords = new List<Keyword>(copy.OfficialKeywords);
            target.Text = copy.Text;
            target.Golden = copy.Golden;
            target.Enchantments = new List<Enchantment>(copy.Enchantments);
            target.Counters = new Dictionary<string, int>(copy.Counters);
            target.ImagePath = copy.ImagePath;
            target.EffectIds = new List<string>(copy.EffectIds);
            target.Tags = new List<string>(copy.Tags) { "mini_zerek_copy" };
            target.InstanceId = instanceId;
            target.Owner = BoardSide.Player;
            target.PoolSource = PoolSource.Copy;
            target.OriginPoolSource = PoolSource.Copy;
            target.PoolCopiesHeld = 0;
        }

        private static MinionInstance GetSelectedShopTarget(HeroEffectContext context, string error)
        {
            if (TryResolveSelectedShopTarget(
                    context,
                    context.SecondaryTargetIndex,
                    context.SecondaryTargetZone,
                    context.SecondaryTargetInstanceId,
                    out var secondary))
            {
                return secondary;
            }

            if (TryResolveSelectedShopTarget(
                    context,
                    context.TargetIndex,
                    context.TargetZone,
                    context.TargetInstanceId,
                    out var primary))
            {
                return primary;
            }

            throw new InvalidOperationException(error);
        }

        private static bool TryResolveSelectedShopTarget(
            HeroEffectContext context,
            int targetIndex,
            TargetZone targetZone,
            string targetInstanceId,
            out MinionInstance target)
        {
            target = null;
            if (targetZone == TargetZone.Unspecified && string.IsNullOrEmpty(targetInstanceId))
            {
                return false;
            }

            if (targetZone != TargetZone.Unspecified && targetZone != TargetZone.TavernShop)
            {
                return false;
            }

            if (!TryResolveTarget(context, targetIndex, targetZone == TargetZone.Unspecified ? TargetZone.TavernShop : targetZone, targetInstanceId, out target))
            {
                return false;
            }

            return target.CardKind == CardKind.Minion && context.State.Player.Tavern.Shop.Contains(target);
        }

        private static void RefreshShopFromOpponentHighestTier(HeroEffectContext context, HeroEffectResult result)
        {
            var highest = GetOpponentWarbandForEffects(context.State)
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.TavernTier)
                .ThenBy(card => card.InstanceId)
                .Take(3)
                .ToList();
            if (highest.Count == 0)
            {
                result.Messages.Add("Waxadred: no opponent warband memory/minions available.");
                return;
            }

            context.State.Player.Tavern.Shop.Clear();
            foreach (var source in highest)
            {
                var copy = source.Clone();
                copy.InstanceId = "player-waxadred-" + context.State.Round + "-" + context.State.Player.Tavern.Shop.Count;
                copy.Owner = BoardSide.Player;
                copy.PoolSource = PoolSource.Copy;
                copy.OriginPoolSource = PoolSource.Copy;
                copy.PoolCopiesHeld = 0;
                context.State.Player.Tavern.Shop.Add(copy);
            }

            result.Messages.Add("Waxadred: refreshed the Tavern with highest-tier opponent minion proxies.");
        }

        private static IEnumerable<MinionInstance> GetOpponentWarbandForEffects(MatchState state)
        {
            var history = state?.OpponentHistory?.LastOpponentWarband;
            if (history != null && history.Any(card => card != null && card.CardKind == CardKind.Minion))
            {
                return history;
            }

            return state?.Opponent?.Board ?? Enumerable.Empty<MinionInstance>();
        }

        private static IEnumerable<MinionInstance> GetLastOpponentWarband(MatchState state)
        {
            return GetOpponentWarbandForEffects(state);
        }

        private static IEnumerable<MinionInstance> GetNextOpponentWarband(MatchState state)
        {
            var current = state?.Opponent?.Board;
            if (current != null && current.Any(card => card != null && card.CardKind == CardKind.Minion))
            {
                return current;
            }

            return state?.OpponentHistory?.LastOpponentWarband ?? Enumerable.Empty<MinionInstance>();
        }

        private static IEnumerable<MinionInstance> GetLowestHealthOpponentWarband(MatchState state)
        {
            return GetNextOpponentWarband(state);
        }

        private static bool AddLowestHealthOpponentPlainMinionToHand(HeroEffectContext context, string source)
        {
            var candidates = GetLowestHealthOpponentWarband(context.State)
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var target = candidates[context.Rng.NextInt(candidates.Count)];
            return AddPlainCopyToHand(context.State, target, source, context.Minions);
        }

        private static void StartBigglesworthEliminatedWarbandDiscover(HeroEffectContext context, HeroEffectResult result)
        {
            var history = context.State.OpponentHistory;
            if (history?.EliminatedPlayerWarbands == null)
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            var discoverCount = GetCounterOrDefault(tavern, BigglesworthDiscoverCountCounter, 0);
            if (discoverCount >= 7)
            {
                return;
            }

            var snapshotIndex = Math.Max(0, GetCounterOrDefault(tavern, BigglesworthSnapshotIndexCounter, 0));
            while (snapshotIndex < history.EliminatedPlayerWarbands.Count)
            {
                var snapshot = history.EliminatedPlayerWarbands[snapshotIndex];
                if (snapshot?.Warband != null && snapshot.Warband.Any(card => card != null && card.CardKind == CardKind.Minion))
                {
                    if (StartOpponentWarbandDiscover(context, snapshot.Warband, "hero-power:bigglesworth-" + snapshotIndex, plainCopies: false))
                    {
                        tavern.HeroEffectCounters[BigglesworthDiscoverCountCounter] = discoverCount + 1;
                        tavern.HeroEffectCounters[BigglesworthSnapshotIndexCounter] = snapshotIndex + 1;
                        result.Messages.Add("Kel'Thuzad's Kitty: started a Discover from an eliminated-player warband snapshot.");
                    }

                    return;
                }

                snapshotIndex += 1;
            }
        }

        private static bool StartOpponentWarbandDiscover(HeroEffectContext context, IEnumerable<MinionInstance> warband, string source, bool plainCopies)
        {
            var candidates = (warband ?? Enumerable.Empty<MinionInstance>())
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = context.Rng.NextInt(candidates.Count);
                var candidate = candidates[index];
                candidates.RemoveAt(index);
                var copy = plainCopies
                    ? CreatePlainCopy(candidate, "hero-discover-" + source + "-" + options.Count, BoardSide.Player, PoolSource.Discover, context.Minions)
                    : candidate.Clone();
                copy.InstanceId = "hero-discover-" + source + "-" + options.Count;
                copy.Owner = BoardSide.Player;
                copy.PoolSource = PoolSource.Discover;
                copy.OriginPoolSource = PoolSource.Discover;
                copy.PoolCopiesHeld = 0;
                AddTag(copy, "opponent_warband_discover");
                if (plainCopies)
                {
                    AddTag(copy, "plain_copy");
                }

                options.Add(copy);
            }

            context.State.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = source,
                RewardTier = options.Max(card => Math.Max(1, card.TavernTier)),
                Options = options
            });
            return true;
        }

        private static void ClearTemporaryRebornRites(MatchState state)
        {
            foreach (var minion in state.Player.Board.Where(card => card != null && card.Tags.Contains("temporary_reborn_rites")).ToList())
            {
                RemoveKeywordFromSource(minion, Keyword.Reborn, LichKingRebornSource);
                minion.Tags.Remove("temporary_reborn_rites");
            }
        }

        private static void ClearVoljinTemporarySwaps(MatchState state)
        {
            var targets = state.Player.Board
                .Concat(state.Player.Tavern.Shop.Where(card => card != null))
                .Where(card => card.Tags.Contains("temporary_spirit_swap"))
                .ToList();
            foreach (var minion in targets)
            {
                RemoveTrackedBuff(minion, VoljinSwapSource);
                minion.Tags.Remove("temporary_spirit_swap");
            }
        }

        private static void SetStats(MinionInstance target, int attack, int health, string sourceId)
        {
            StatMath.SetStats(target, attack, health, sourceId);
        }

        private static void NotifyTitanicGuardian(MatchState state, int healthGain, string changedInstanceId, HeroEffectResult result)
        {
            if (healthGain <= 0)
            {
                return;
            }

            foreach (var guardian in MatchingBoardBuddies(state, TitanicGuardianCardId))
            {
                if (guardian.InstanceId == changedInstanceId)
                {
                    continue;
                }

                Buff(guardian, 0, healthGain, "Titanic Guardian");
                result.Messages.Add("Titanic Guardian: gained Health with another minion.");
            }
        }

        private static void UpdateMishmashStats(MatchState state)
        {
            var amalgam = state.Player.Board.FirstOrDefault(card => card != null && card.Tags.Contains("curator_amalgam"));
            if (amalgam == null)
            {
                return;
            }

            var attack = Math.Max(0, amalgam.Attack - 2);
            var health = Math.Max(0, amalgam.MaxHealth - 2);
            foreach (var mishmash in MatchingBoardBuddies(state, MishmashCardId))
            {
                SetTrackedBuff(mishmash, "Mishmash", attack, health);
            }
        }

        private static int AddBananasToHand(HeroEffectContext context, int count, string source)
        {
            var added = 0;
            var tavern = context.State.Player.Tavern;
            for (var index = 0; index < count && tavern.Hand.Count < HandLimit; index += 1)
            {
                tavern.Hand.Add(CreateBananaCard(source + "-" + context.State.Round + "-" + tavern.Hand.Count));
                added += 1;
            }

            return added;
        }

        private static MinionInstance CreateBananaCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-mukla-banana-" + suffix,
                DefinitionId = "mukla-banana",
                CardId = MuklaBananaCardId,
                Name = "Banana",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = "Give a friendly minion +1/+1.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_spell", "banana", "targeted_spell", "buff_spell" }
            };
        }

        private static int FeedBananasToBoard(HeroEffectContext context, int count, string excludedInstanceId)
        {
            var fed = 0;
            var candidates = context.State.Player.Board
                .Where(card => card != null &&
                               card.CardKind == CardKind.Minion &&
                               !string.Equals(card.InstanceId, excludedInstanceId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            for (var index = 0; index < count && candidates.Count > 0; index += 1)
            {
                var target = candidates[context.Rng.NextInt(candidates.Count)];
                Buff(target, 1, 1, "Crazy Monkey");
                fed += 1;
            }

            return fed;
        }

        private static MinionInstance PickFriendlyMinion(HeroEffectContext context, string excludeCardId = null, string excludeInstanceId = null)
        {
            var candidates = context.State.Player.Board
                .Where(card => card != null &&
                               card.CardKind == CardKind.Minion &&
                               (string.IsNullOrEmpty(excludeCardId) || !string.Equals(card.CardId, excludeCardId, StringComparison.OrdinalIgnoreCase)) &&
                               (string.IsNullOrEmpty(excludeInstanceId) || !string.Equals(card.InstanceId, excludeInstanceId, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (candidates.Count == 0 && !string.IsNullOrEmpty(excludeCardId))
            {
                candidates = context.State.Player.Board
                    .Where(card => card != null &&
                                   card.CardKind == CardKind.Minion &&
                                   (string.IsNullOrEmpty(excludeInstanceId) || !string.Equals(card.InstanceId, excludeInstanceId, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return candidates.Count == 0 ? null : candidates[context.Rng.NextInt(candidates.Count)];
        }

        private static void BuffTentaclesAfterDifferentFriendlyMinionGainsStats(MatchState state, MinionInstance buffedMinion)
        {
            foreach (var tentacle in MatchingBoardBuddies(state, TentacleOfCThunCardId))
            {
                if (buffedMinion != null && string.Equals(tentacle.InstanceId, buffedMinion.InstanceId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var existing = tentacle.Enchantments.FirstOrDefault(enchantment => enchantment.SourceId == "Tentacle of C'Thun temporary");
                var amount = (existing?.AttackBonus ?? 0) + 1;
                SetTrackedBuff(tentacle, "Tentacle of C'Thun temporary", amount, amount);
            }
        }

        private static void ResetTentacleTemporaryBuffs(MatchState state)
        {
            foreach (var tentacle in MatchingBoardBuddies(state, TentacleOfCThunCardId))
            {
                RemoveTrackedBuff(tentacle, "Tentacle of C'Thun temporary");
            }
        }

        private static bool AddRandomGoldenMinionToHand(HeroEffectContext context, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var tier = Math.Max(1, tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion => minion.InPool && minion.TavernTier <= tier)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var definition = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count, true, PoolSource.Copy, 0);
            card.Counters["triple-reward-granted"] = 1;
            AddTag(card, "generated_copy");
            AddTag(card, "golden_reward");
            tavern.Hand.Add(card);
            return true;
        }

        private static void StartCurrentTierMinionDiscover(HeroEffectContext context, string source)
        {
            var tier = Math.Max(1, context.State.Player.Tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion => minion.InPool && minion.TavernTier == tier)
                .ToList();
            if (candidates.Count < 3)
            {
                candidates = context.Minions.All
                    .Where(minion => minion.InPool && minion.TavernTier <= tier)
                    .ToList();
            }

            StartMinionDiscover(context, candidates, tier, source);
        }

        private static void TryStartAzsharaNagaConquest(HeroEffectContext context, HeroEffectResult result, string powerId)
        {
            if (!IsPower(powerId, QueenAzsharaPowerId) ||
                GetCounterOrDefault(context.State.Player.Tavern, QueenAzsharaConquestCounter, 0) > 0)
            {
                return;
            }

            var totalAttack = context.State.Player.Board
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .Sum(card => Math.Max(0, card.Attack));
            if (totalAttack < QueenAzsharaAttackThreshold)
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            tavern.HeroEffectCounters[QueenAzsharaConquestCounter] = 1;
            var tier = Math.Max(1, tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion =>
                    IsMinionAvailableForCurrentPool(context.State, minion) &&
                    minion.TavernTier <= tier &&
                    minion.Tribes != null &&
                    minion.Tribes.Contains(Tribe.Naga))
                .ToList();
            if (candidates.Count == 0)
            {
                result.Messages.Add("Azshara's Ambition: reached 30 Attack, but no legal Naga are available.");
                return;
            }

            StartMinionDiscover(context, candidates, tier, QueenAzsharaConquestSource);
            result.Messages.Add("Azshara's Ambition: began Naga Conquest.");
        }

        private static void StartMagneticMechDiscover(HeroEffectContext context, string source)
        {
            var tier = Math.Max(1, context.State.Player.Tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion => minion.InPool &&
                                 minion.TavernTier <= tier &&
                                 minion.Tribes.Contains(Tribe.Mech) &&
                                 minion.Keywords.Contains(Keyword.Magnetic))
                .ToList();
            if (candidates.Count < 3)
            {
                candidates = context.Minions.All
                    .Where(minion => minion.InPool &&
                                     minion.TavernTier <= tier &&
                                     minion.Tribes.Contains(Tribe.Mech))
                    .ToList();
            }

            StartMinionDiscover(context, candidates, tier, source);
        }

        private static void RotateRatKingTribe(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var current = DecodeTribe(GetCounterOrDefault(tavern, RatKingCurrentTribeCounter, 0));
            var candidates = AvailableRatKingTribes(context)
                .Where(tribe => tribe != current)
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = AvailableRatKingTribes(context);
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var next = candidates[context.Rng.NextInt(candidates.Count)];
            tavern.HeroEffectCounters[RatKingCurrentTribeCounter] = EncodeTribe(next);
            tavern.HeroEffectCounters[RatKingLastTribeCounter] = EncodeTribe(current);
            result.Messages.Add("King of " + next + ": Rat King changed to " + next + ".");
            ResolvePigeonLordRefresh(context, result);
        }

        private static void StartRatKingCurrentTribeDiscover(HeroEffectContext context, HeroEffectResult result)
        {
            var tribe = DecodeTribe(GetCounterOrDefault(context.State.Player.Tavern, RatKingCurrentTribeCounter, 0));
            if (!IsRatKingTribe(tribe))
            {
                RotateRatKingTribe(context, result);
                tribe = DecodeTribe(GetCounterOrDefault(context.State.Player.Tavern, RatKingCurrentTribeCounter, 0));
            }

            var candidates = RatKingMinionCandidates(context, tribe).ToList();
            if (candidates.Count == 0)
            {
                result.Messages.Add("King of " + tribe + ": no legal minions are available.");
                return;
            }

            StartMinionDiscover(context, candidates, Math.Max(1, context.State.Player.Tavern.Tier), "hero-power:rat-king:" + tribe.ToString().ToLowerInvariant());
            result.Messages.Add("King of " + tribe + ": started a " + tribe + " Discover.");
        }

        private static void StartHolmesGuessDiscover(HeroEffectContext context, HeroEffectResult result)
        {
            var opponentCandidates = GetNextOpponentWarband(context.State)
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            if (opponentCandidates.Count == 0)
            {
                result.Messages.Add("Detective for Hire: no opponent snapshot is available.");
                return;
            }

            var correct = opponentCandidates[context.Rng.NextInt(opponentCandidates.Count)];
            var options = new List<MinionInstance>
            {
                CreateHolmesGuessOption(correct, "holmes-correct", true, context.Minions)
            };
            var tier = Math.Max(1, context.State.Player.Tavern.Tier);
            var distractors = context.Minions.All
                .Where(minion =>
                    IsMinionAvailableForCurrentPool(context.State, minion) &&
                    minion.TavernTier <= tier &&
                    !string.Equals(minion.CardId, correct.CardId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            while (options.Count < 3 && distractors.Count > 0)
            {
                var index = context.Rng.NextInt(distractors.Count);
                var definition = distractors[index];
                distractors.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "holmes-distractor-" + options.Count, false, PoolSource.Discover, 0));
            }

            Shuffle(options, context.Rng);
            context.State.Player.Tavern.QueueDiscover(new DiscoverState
            {
                Source = HolmesDiscoverSource,
                RewardTier = tier,
                RemainingPicks = 1,
                Options = options
            });
            result.Messages.Add("Detective for Hire: started an opponent snapshot guess.");
        }

        private static MinionInstance CreateHolmesGuessOption(MinionInstance source, string suffix, bool correct, MinionCatalog catalog)
        {
            var option = CreatePlainCopy(source, suffix, BoardSide.Player, PoolSource.Discover, catalog);
            if (correct)
            {
                AddTag(option, HolmesCorrectGuessTag);
            }

            return option;
        }

        private static void Shuffle<T>(IList<T> values, SeededRng rng)
        {
            for (var index = values.Count - 1; index > 0; index -= 1)
            {
                var swap = rng.NextInt(index + 1);
                var temp = values[index];
                values[index] = values[swap];
                values[swap] = temp;
            }
        }

        private static void ResolvePigeonLordRefresh(HeroEffectContext context, HeroEffectResult result)
        {
            if (!HasBuddy(context.State, PigeonLordCardId))
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            if (GetCounterOrDefault(tavern, PigeonLordRefreshRoundCounter, 0) == context.State.Round)
            {
                return;
            }

            var tribe = DecodeTribe(GetCounterOrDefault(tavern, RatKingCurrentTribeCounter, 0));
            if (!IsRatKingTribe(tribe))
            {
                return;
            }

            var hasCurrentType = tavern.Shop.Any(card =>
                card != null &&
                card.CardKind == CardKind.Minion &&
                card.Tribes != null &&
                card.Tribes.Contains(tribe));
            if (hasCurrentType)
            {
                return;
            }

            tavern.FreeRefreshes = StatMath.SaturatingAdd(tavern.FreeRefreshes, 1, 0, StatMath.MaxStat);
            tavern.HeroEffectCounters[PigeonLordRefreshRoundCounter] = context.State.Round;
            result.Messages.Add("Pigeon Lord: gained a free Refresh because the Tavern has no " + tribe + ".");
        }

        private static List<Tribe> AvailableRatKingTribes(HeroEffectContext context)
        {
            return RatKingTribes
                .Where(tribe => RatKingMinionCandidates(context, tribe).Any())
                .ToList();
        }

        private static IEnumerable<MinionDefinition> RatKingMinionCandidates(HeroEffectContext context, Tribe tribe)
        {
            if (context?.Minions?.All == null)
            {
                return Enumerable.Empty<MinionDefinition>();
            }

            var tier = Math.Max(1, context.State.Player.Tavern.Tier);
            return context.Minions.All
                .Where(minion =>
                    IsMinionAvailableForCurrentPool(context.State, minion) &&
                    minion.TavernTier <= tier &&
                    minion.Tribes != null &&
                    minion.Tribes.Contains(tribe));
        }

        private static bool IsMinionAvailableForCurrentPool(MatchState state, MinionDefinition minion)
        {
            if (minion == null || !minion.InPool || IsDuoCardId(minion.CardId))
            {
                return false;
            }

            if (!state.IsDefaultCardPoolVersion &&
                (state.EnabledMinionCardIds == null ||
                 !state.EnabledMinionCardIds.Contains(minion.CardId, StringComparer.OrdinalIgnoreCase)))
            {
                return false;
            }

            return TribeAvailabilityRules.IsMinionAvailable(minion, state.ActiveTribes);
        }

        private static bool IsDuoCardId(string cardId)
        {
            return !string.IsNullOrEmpty(cardId) && cardId.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase);
        }

        private static int EncodeTribe(Tribe tribe)
        {
            return (int)tribe;
        }

        private static Tribe DecodeTribe(int value)
        {
            return Enum.IsDefined(typeof(Tribe), value) ? (Tribe)value : Tribe.None;
        }

        private static bool IsRatKingTribe(Tribe tribe)
        {
            return RatKingTribes.Contains(tribe);
        }

        private static DiscoverState StartMinionDiscover(HeroEffectContext context, List<MinionDefinition> candidates, int rewardTier, string source)
        {
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = context.Rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "hero-discover-" + source + "-" + options.Count, false, PoolSource.Discover, 0));
            }

            var discover = new DiscoverState
            {
                Source = source,
                RewardTier = rewardTier,
                Options = options
            };
            context.State.Player.Tavern.QueueDiscover(discover);
            return discover;
        }

        private static bool DamageRandomEnemyMinion(HeroEffectContext context, int damage)
        {
            var candidates = context.State.Opponent.Board
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0 || damage <= 0)
            {
                return false;
            }

            var target = candidates[context.Rng.NextInt(candidates.Count)];
            target.Health = StatMath.DamageHealth(target.Health, damage);
            if (target.Health <= 0)
            {
                context.State.Opponent.Board.Remove(target);
            }

            return true;
        }

        private static void AddBloodGemsToHand(HeroEffectContext context, int count, string source)
        {
            var multiplier = 1 + MatchingBoardBuddies(context.State, DeathsHeadSageCardId).Count();
            var total = count * multiplier;
            for (var index = 0; index < total && context.State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                context.State.Player.Tavern.Hand.Add(CreateBloodGemCard(source + "-" + context.State.Round + "-" + index));
            }
        }

        private static MinionInstance CreateBloodGemCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-blood-gem-" + suffix,
                DefinitionId = "blood-gem",
                CardId = BloodGemCardId,
                Name = "Blood Gem",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.BloodGem },
                Text = "Give a friendly minion +1/+1.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_spell", "blood_gem", "targeted_spell", "buff_spell" }
            };
        }

        private static void DamageHeroWithUnderlingRewind(MatchState state, int amount, HeroEffectResult result)
        {
            var underlings = MatchingBoardBuddies(state, UnearthedUnderlingCardId).ToList();
            if (underlings.Count == 0)
            {
                state.Player.Health = Math.Max(0, state.Player.Health - amount);
                return;
            }

            foreach (var underling in underlings)
            {
                Buff(underling, amount, amount, "Unearthed Underling");
            }

            result.Messages.Add("Unearthed Underling: rewound the damage and gained +" + amount + "/+" + amount + ".");
        }

        private static bool AddLanternLightToHand(HeroEffectContext context)
        {
            var tavern = context.State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var amount = Math.Max(1, tavern.Tier);
            tavern.Hand.Add(new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-lantern-light-" + context.State.Round + "-" + tavern.Hand.Count,
                DefinitionId = "lantern-light",
                CardId = LanternLightCardId,
                Name = "Lantern Light",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = "Give a minion +" + amount + "/+" + amount + ".",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Counters = new Dictionary<string, int> { { "lantern_amount", amount } },
                Tags = new List<string> { "generated_spell", "lantern_light", "targeted_spell", "buff_spell" }
            });
            return true;
        }

        private static bool StartBoxCarsTavernSpellDiscover(HeroEffectContext context, int tier, string instanceId)
        {
            var tavern = context.State.Player.Tavern;
            if (context.Spells == null)
            {
                return false;
            }

            var candidates = AvailableTavernSpells(context)
                .Where(spell => spell.TavernTier == tier)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = context.Rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "box-cars-" + instanceId + "-" + options.Count));
            }

            tavern.QueueDiscover(new DiscoverState
            {
                Source = "buddy:box-cars",
                RewardTier = tier,
                RemainingPicks = 1,
                Options = options
            });
            return true;
        }

        private static List<TavernSpellDefinition> AvailableTavernSpells(HeroEffectContext context)
        {
            return context.Spells.All
                .Where(spell =>
                    spell != null &&
                    spell.InPool &&
                    spell.Category == "TavernSpell" &&
                    (context.State.IsDefaultCardPoolVersion ||
                        (context.State.EnabledTavernSpellCardNumbers != null &&
                            context.State.EnabledTavernSpellCardNumbers.Contains(spell.CardNumber, StringComparer.OrdinalIgnoreCase))) &&
                    TribeAvailabilityRules.IsTavernSpellAvailable(spell, context.State.ActiveTribes))
                .ToList();
        }

        private static int RollSixSidedDie(HeroEffectContext context)
        {
            return context.Rng.NextInt(6) + 1;
        }

        private static bool AddRandomStatTavernSpellToHand(HeroEffectContext context, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (context.Spells == null || tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var candidates = context.Spells.All
                .Where(spell =>
                    spell.InPool &&
                    spell.Category == "TavernSpell" &&
                    spell.TavernTier <= Math.Max(1, tavern.Tier) &&
                    (spell.Tags.Contains("buff_spell") ||
                        spell.Tags.Contains("targeted_spell") ||
                        (!string.IsNullOrWhiteSpace(spell.Text) && (spell.Text.Contains("+") || spell.Text.Contains("stats")))))
                .ToList();
            if (candidates.Count == 0)
            {
                return AddRandomTavernSpellToHand(context, 1, Math.Max(1, tavern.Tier), source);
            }

            var definition = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count);
            card.PoolSource = PoolSource.Copy;
            card.OriginPoolSource = PoolSource.Copy;
            card.PoolCopiesHeld = 0;
            AddTag(card, "generated_spell");
            AddTag(card, "generated_copy");
            tavern.Hand.Add(card);
            return true;
        }

        private static bool AddRandomBountyToHand(HeroEffectContext context, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var cardId = BountyCardIds[context.Rng.NextInt(BountyCardIds.Length)];
            var definition = context.Spells?.All.FirstOrDefault(spell =>
                string.Equals(spell.CardNumber, cardId, StringComparison.OrdinalIgnoreCase));
            var card = definition == null
                ? CreateFallbackBountyCard(cardId, source + "-" + context.State.Round + "-" + tavern.Hand.Count)
                : MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count);
            card.PoolSource = PoolSource.Copy;
            card.OriginPoolSource = PoolSource.Copy;
            card.PoolCopiesHeld = 0;
            AddTag(card, "generated_spell");
            AddTag(card, "bounty");
            tavern.Hand.Add(card);
            return true;
        }

        private static MinionInstance CreateFallbackBountyCard(string cardId, string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-bounty-" + suffix,
                DefinitionId = "bounty-" + cardId,
                CardId = cardId,
                Name = "Bounty",
                Cost = 2,
                TavernTier = 3,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = "Generated Bounty.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_spell", "generated_tavern_spell", "bounty" }
            };
        }

        private static void MakeGoldenInPlace(MinionInstance target, MinionCatalog catalog)
        {
            if (target == null)
            {
                return;
            }

            if (!target.Golden)
            {
                target.Golden = true;
                StatMath.DoubleCurrentStats(target, false);
                target.Counters["triple-reward-granted"] = 1;
            }

            catalog?.TrySyncGoldenText(target);
        }

        private static void AddTavernCoinToHand(HeroEffectContext context, string source)
        {
            var definition = context.Spells?.All.FirstOrDefault(spell => spell.CardNumber == TavernCoinCardNumber);
            if (definition == null || context.State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + context.State.Player.Tavern.Hand.Count);
            card.PoolSource = PoolSource.Copy;
            card.OriginPoolSource = PoolSource.Copy;
            card.PoolCopiesHeld = 0;
            AddTag(card, "generated_spell");
            AddTag(card, "tavern_coin");
            context.State.Player.Tavern.Hand.Add(card);
        }

        private static bool AddLastTavernSpellCopyToHand(HeroEffectContext context, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (string.IsNullOrWhiteSpace(tavern.LastTavernSpellCardId) ||
                context.Spells == null ||
                tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var definition = context.Spells.All.FirstOrDefault(spell =>
                string.Equals(spell.CardNumber, tavern.LastTavernSpellCardId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(spell.Id, tavern.LastTavernSpellCardId, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(spell.ImagePath) && spell.ImagePath.EndsWith("/" + tavern.LastTavernSpellCardId, StringComparison.OrdinalIgnoreCase)));
            if (definition == null)
            {
                return false;
            }

            var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count);
            card.PoolSource = PoolSource.Copy;
            card.OriginPoolSource = PoolSource.Copy;
            card.PoolCopiesHeld = 0;
            AddTag(card, "generated_spell");
            AddTag(card, "generated_copy");
            tavern.Hand.Add(card);
            return true;
        }

        private static bool AddRandomTavernSpellToHand(HeroEffectContext context, int minTier, int maxTier, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (context.Spells == null || tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var low = Math.Max(1, minTier);
            var high = Math.Max(low, maxTier);
            var candidates = context.Spells.All
                .Where(spell => spell.InPool &&
                                spell.Category == "TavernSpell" &&
                                spell.TavernTier >= low &&
                                spell.TavernTier <= high)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var definition = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count);
            card.PoolSource = PoolSource.Copy;
            card.OriginPoolSource = PoolSource.Copy;
            card.PoolCopiesHeld = 0;
            AddTag(card, "generated_spell");
            AddTag(card, "generated_copy");
            tavern.Hand.Add(card);
            return true;
        }

        private static void AddCopyToHand(MatchState state, MinionInstance source, string copySource)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var copy = source.Clone();
            copy.InstanceId = "player-" + source.DefinitionId + "-" + copySource + "-" + state.Round + "-" + state.Player.Tavern.Hand.Count;
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Copy;
            copy.OriginPoolSource = PoolSource.Copy;
            copy.PoolCopiesHeld = 0;
            AddTag(copy, "generated_copy");
            state.Player.Tavern.Hand.Add(copy);
        }

        private static bool AddPlainCopyToHand(MatchState state, MinionInstance source, string copySource, MinionCatalog catalog)
        {
            if (state.Player.Tavern.Hand.Count >= HandLimit || source == null)
            {
                return false;
            }

            var copy = CreatePlainCopy(
                source,
                "player-" + source.DefinitionId + "-" + copySource + "-" + state.Round + "-" + state.Player.Tavern.Hand.Count,
                BoardSide.Player,
                PoolSource.Copy,
                catalog);
            AddTag(copy, "generated_copy");
            AddTag(copy, "plain_copy");
            state.Player.Tavern.Hand.Add(copy);
            return true;
        }

        private static void AddTripleRewardToHand(HeroEffectContext context)
        {
            var tavern = context.State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            tavern.Hand.Add(new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-triple-reward-hero-" + context.State.Round + "-" + tavern.Hand.Count,
                DefinitionId = "triple-reward",
                CardId = "TRIPLE_REWARD",
                Name = "Triple Reward",
                Cost = 0,
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Discover, Keyword.TavernSpell },
                Text = "Play: Discover a minion from one tavern tier higher, up to tier 7.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            });
        }

        private static bool AddMinionByCardIdToHand(HeroEffectContext context, string cardId, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (context.Minions == null || tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var definition = context.Minions.All.FirstOrDefault(minion =>
                string.Equals(minion.CardId, cardId, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                return false;
            }

            tavern.Hand.Add(MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count, false, PoolSource.Copy, 0));
            return true;
        }

        private static MinionInstance CreatePlainCopy(MinionInstance source, string instanceId, BoardSide owner, PoolSource poolSource, MinionCatalog catalog)
        {
            var copy = source.Clone();
            var baseHealth = source.BaseHealth > 0 ? source.BaseHealth : Math.Max(1, source.MaxHealth);
            copy.InstanceId = instanceId;
            copy.Owner = owner;
            copy.Golden = false;
            copy.BaseAttack = Math.Max(0, source.BaseAttack);
            copy.BaseHealth = baseHealth;
            copy.Attack = copy.BaseAttack;
            copy.Health = baseHealth;
            copy.MaxHealth = baseHealth;
            copy.Keywords = source.OfficialKeywords != null && source.OfficialKeywords.Count > 0
                ? new List<Keyword>(source.OfficialKeywords)
                : new List<Keyword>(source.Keywords);
            copy.Enchantments = new List<Enchantment>();
            copy.PoolSource = poolSource;
            copy.OriginPoolSource = poolSource;
            copy.PoolCopiesHeld = 0;
            catalog?.TrySyncGoldenText(copy);
            return copy;
        }

        private static void AddRandomTribeMinionToHand(HeroEffectContext context, Tribe tribe, string source)
        {
            if (context.State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var tier = Math.Max(1, context.State.Player.Tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion => minion.Tribes.Contains(tribe) && minion.TavernTier <= tier)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var definition = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + context.State.Player.Tavern.Hand.Count, source: PoolSource.Copy);
            card.PoolCopiesHeld = 0;
            AddTag(card, "generated_copy");
            context.State.Player.Tavern.Hand.Add(card);
        }

        private static bool AddRandomMagneticMechToHand(HeroEffectContext context, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (context.Minions == null || tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var tier = Math.Max(1, tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion => minion.InPool &&
                                 minion.TavernTier <= tier &&
                                 minion.Tribes.Contains(Tribe.Mech) &&
                                 minion.Keywords.Contains(Keyword.Magnetic))
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = context.Minions.All
                    .Where(minion => minion.InPool &&
                                     minion.TavernTier <= tier &&
                                     minion.Tribes.Contains(Tribe.Mech))
                    .ToList();
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var definition = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count, false, PoolSource.Copy, 0);
            card.PoolCopiesHeld = 0;
            AddTag(card, "generated_copy");
            tavern.Hand.Add(card);
            return true;
        }

        private static bool AddRandomMechToHand(HeroEffectContext context, string source)
        {
            var tavern = context.State.Player.Tavern;
            if (context.Minions == null || tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var tier = Math.Max(1, tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion => minion.InPool &&
                                 minion.TavernTier <= tier &&
                                 minion.Tribes.Contains(Tribe.Mech))
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var definition = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + tavern.Hand.Count, false, PoolSource.Copy, 0);
            card.PoolCopiesHeld = 0;
            AddTag(card, "generated_copy");
            tavern.Hand.Add(card);
            return true;
        }

        private static int AddRandomTribeMinionsToBoard(HeroEffectContext context, Tribe tribe, int count, string source)
        {
            var added = 0;
            var tier = Math.Max(1, context.State.Player.Tavern.Tier);
            var candidates = context.Minions.All
                .Where(minion => minion.Tribes.Contains(tribe) && minion.TavernTier <= tier)
                .ToList();
            while (added < count && context.State.Player.Board.Count < BoardLimit && candidates.Count > 0)
            {
                var definition = candidates[context.Rng.NextInt(candidates.Count)];
                var card = MinionFactory.Create(definition, BoardSide.Player, source + "-" + context.State.Round + "-" + context.State.Player.Board.Count, source: PoolSource.Summon);
                card.PoolCopiesHeld = 0;
                AddTag(card, "summoned_by_death_proxy");
                context.State.Player.Board.Add(card);
                added += 1;
            }

            return added;
        }

        private static void ApplyDeathwingCombatBuff(HeroCombatEffectContext context, HeroEffectResult result)
        {
            var sinestraActive = HasBuddy(context.State, SinestraCardId);
            foreach (var minion in context.PlayerBoard.Where(card => card != null))
            {
                Buff(minion, 2, sinestraActive ? 1 : 0, "ALL Will Burn!");
                var original = context.State.Player.Board.FirstOrDefault(card => card.InstanceId == minion.InstanceId);
                Buff(original, 2, sinestraActive ? 1 : 0, sinestraActive ? "ALL Will Burn! + Sinestra" : "ALL Will Burn!");
            }

            foreach (var minion in context.OpponentBoard ?? Enumerable.Empty<MinionInstance>())
            {
                Buff(minion, 2, 0, "ALL Will Burn!");
            }

            result.Messages.Add(sinestraActive
                ? "ALL Will Burn!: all combat minions gained +2 Attack; Sinestra made friendly Attack gains add +1 Health permanently."
                : "ALL Will Burn!: all combat minions gained +2 Attack and friendly minions kept it permanently.");
        }

        private static void ApplyTamsinPhylactery(HeroCombatEffectContext context, HeroEffectResult result)
        {
            var target = context.PlayerBoard
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderBy(card => card.Attack)
                .ThenBy(card => card.InstanceId)
                .FirstOrDefault();
            if (target == null)
            {
                return;
            }

            AddKeyword(target, Keyword.Deathrattle, "Fragrant Phylactery");
            AddTag(target, TamsinPhylacteryTag);
            result.Messages.Add("Fragrant Phylactery: lowest-Attack minion gained the stat-sharing Deathrattle for combat.");
        }

        private static void ApplyIllidanEdgeBuffs(List<MinionInstance> board, HeroEffectResult result)
        {
            var targets = board
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null)
                .Where(item => item.Index == 0 || item.Index == board.Count - 1)
                .Select(item => item.Card)
                .Distinct()
                .ToList();
            foreach (var target in targets)
            {
                Buff(target, 2, 1, "Wingmen");
                AddTag(target, "wingmen_immediate_attack_pending");
            }

            if (targets.Count > 0)
            {
                result.Messages.Add("Wingmen: edge minions gained +2/+1 and will attack before the normal combat start phase.");
            }
        }

        private static void ApplyEclipsionFirstAttackImmune(HeroCombatEffectContext context, HeroEffectResult result)
        {
            var buddies = context.PlayerBoard
                .Where(card => card != null && string.Equals(card.CardId, EclipsionIllidariCardId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var buddy in buddies)
            {
                AddTag(buddy, EclipsionFirstAttackImmunePendingTag);
            }

            if (buddies.Count > 0)
            {
                result.Messages.Add("Eclipsion Illidari: the first friendly attacker will be Immune while attacking once.");
            }
        }

        private static void ApplyWagtoggleCombatBuffs(HeroCombatEffectContext context, HeroEffectResult result)
        {
            var amount = 2 + Math.Max(0, context.State.Player.Tavern.GoldSpentThisGame / 10);
            var buffed = new HashSet<string>();
            foreach (var tribe in context.PlayerBoard
                         .SelectMany(minion => minion.Tribes)
                         .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                         .Distinct()
                         .ToList())
            {
                var target = context.PlayerBoard.FirstOrDefault(minion => minion.Tribes.Contains(tribe) && !buffed.Contains(minion.InstanceId));
                if (target == null)
                {
                    continue;
                }

                Buff(target, amount, amount, "Wax Warband");
                buffed.Add(target.InstanceId);
            }

            foreach (var elder in context.PlayerBoard.Where(minion => string.Equals(minion.CardId, ElderTaggawagCardId, StringComparison.OrdinalIgnoreCase)))
            {
                var differentTypes = context.PlayerBoard
                    .SelectMany(minion => minion.Tribes)
                    .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                    .Distinct()
                    .Count();
                if (differentTypes < 4)
                {
                    continue;
                }

                var attack = context.PlayerBoard.Where(minion => minion.InstanceId != elder.InstanceId).Select(minion => minion.Attack).DefaultIfEmpty(0).Max();
                var health = context.PlayerBoard.Where(minion => minion.InstanceId != elder.InstanceId).Select(minion => minion.MaxHealth).DefaultIfEmpty(0).Max();
                Buff(elder, attack, health, "Elder Taggawag");
            }

            if (buffed.Count > 0)
            {
                result.Messages.Add("Wax Warband: buffed one friendly minion of each type for combat.");
            }
        }

        private static void SummonRandomTierMinionForCombat(HeroCombatEffectContext context, int tier, string source, bool addCopyToHand, HeroEffectResult result)
        {
            if (context.PlayerBoard.Count >= BoardLimit || context.Minions == null)
            {
                return;
            }

            var candidates = context.Minions.All
                .Where(minion => minion.TavernTier == tier && minion.InPool)
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = context.Minions.All.Where(minion => minion.TavernTier <= tier && minion.InPool).ToList();
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var definition = candidates[context.Rng.NextInt(candidates.Count)];
            var summoned = MinionFactory.Create(definition, BoardSide.Player, source.ToLowerInvariant().Replace("'", string.Empty) + "-combat-" + context.PlayerBoard.Count, source: PoolSource.Summon);
            summoned.PoolCopiesHeld = 0;
            context.PlayerBoard.Add(summoned);
            ApplyCombatSummonModifiers(context, summoned, result);

            if (addCopyToHand && context.State.Player.Tavern.Hand.Count < HandLimit)
            {
                var handCopy = MinionFactory.Create(definition, BoardSide.Player, source.ToLowerInvariant().Replace("'", string.Empty) + "-hand-" + context.State.Player.Tavern.Hand.Count, source: PoolSource.Copy);
                handCopy.PoolCopiesHeld = 0;
                AddTag(handCopy, "generated_copy");
                context.State.Player.Tavern.Hand.Add(handCopy);
            }

            result.Messages.Add(source + ": summoned a Tavern Tier " + definition.TavernTier + " minion for combat.");
        }

        private static void ApplyCombatSummonModifiers(HeroCombatEffectContext context, MinionInstance summoned, HeroEffectResult result)
        {
            if (summoned == null)
            {
                return;
            }

            var tavern = context.State.Player.Tavern;
            if (tavern.CombatSummonBonusAttack != 0 || tavern.CombatSummonBonusHealth != 0 || tavern.CombatSummonTaunt)
            {
                Buff(summoned, tavern.CombatSummonBonusAttack, tavern.CombatSummonBonusHealth, "Sprout It Out!");
                if (tavern.CombatSummonTaunt)
                {
                    AddKeyword(summoned, Keyword.Taunt, "Sprout It Out!");
                }
            }

            if (tavern.CombatSummonDoubleStats)
            {
                Buff(summoned, summoned.Attack, summoned.MaxHealth, "Tamuzo");
                result.Messages.Add("Tamuzo: doubled a combat-summoned minion's stats.");
            }

            if (tavern.CombatSameTierSummonBuffTier > 0 && summoned.TavernTier == tavern.CombatSameTierSummonBuffTier)
            {
                foreach (var minion in context.PlayerBoard.Where(card => card != null))
                {
                    Buff(minion, tavern.CombatSameTierSummonBuffAttack, tavern.CombatSameTierSummonBuffHealth, "Baby Y'Shaarj");
                }

                result.Messages.Add("Baby Y'Shaarj: your minions gained +1/+1 after a same-tier summon.");
            }
        }

        private static List<string> GetActiveCombatHeroPowerCardIds(HeroCombatEffectContext context)
        {
            var powerIds = context.ActiveHeroPowerCardIds != null
                ? context.ActiveHeroPowerCardIds.Where(powerId => !string.IsNullOrEmpty(powerId)).ToList()
                : new List<string>();
            if (powerIds.Count == 0 && !string.IsNullOrEmpty(context.State.Player.HeroPowerCardId))
            {
                powerIds.Add(context.State.Player.HeroPowerCardId);
            }

            return powerIds;
        }

        private static bool HasCombatHeroPower(IEnumerable<string> activePowerIds, string cardId)
        {
            return activePowerIds != null &&
                   activePowerIds.Any(powerId => string.Equals(powerId, cardId, StringComparison.OrdinalIgnoreCase));
        }

        private static void ConfigureCombatSummonModifiers(HeroCombatEffectContext context, IEnumerable<string> activePowerIds)
        {
            var tavern = context.State.Player.Tavern;
            tavern.CombatSummonBonusAttack = 0;
            tavern.CombatSummonBonusHealth = 0;
            tavern.CombatSummonTaunt = false;
            tavern.CombatSummonDoubleStats = false;
            tavern.CombatSameTierSummonBuffTier = 0;
            tavern.CombatSameTierSummonBuffAttack = 0;
            tavern.CombatSameTierSummonBuffHealth = 0;

            if (HasCombatHeroPower(activePowerIds, GreyboughPowerId))
            {
                tavern.CombatSummonBonusAttack = 1;
                tavern.CombatSummonBonusHealth = 2;
                tavern.CombatSummonTaunt = true;
            }

            if (HasBuddy(context.State, TamuzoCardId))
            {
                tavern.CombatSummonDoubleStats = true;
            }

            if (HasBuddy(context.State, BabyYshaarjCardId))
            {
                tavern.CombatSameTierSummonBuffTier = Math.Max(1, tavern.Tier);
                tavern.CombatSameTierSummonBuffAttack = 1;
                tavern.CombatSameTierSummonBuffHealth = 1;
            }
        }

        private static void AddSklibbRefreshMinion(HeroEffectContext context, HeroEffectResult result)
        {
            var tavern = context.State.Player.Tavern;
            var tier = Math.Min(7, Math.Max(1, tavern.Tier + 1));
            var candidates = context.Minions.All
                .Where(minion => minion.InPool && minion.TavernTier == tier)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var definition = candidates[context.Rng.NextInt(candidates.Count)];
            var card = MinionFactory.Create(definition, BoardSide.Player, "sklibb-" + context.State.Round + "-" + tavern.Shop.Count, source: PoolSource.Copy);
            card.PoolCopiesHeld = 0;
            AddTag(card, "sklibb_extra_higher_tier");
            tavern.Shop.Add(card);
            result.Messages.Add("Sklibb: added an extra higher-Tier minion to the Tavern.");
        }

        private static void ResolveNathanosBattlecry(HeroEffectContext context, HeroEffectResult result)
        {
            var target = PickTargetedOrFirstOtherBoardMinion(context, card => card.CardKind == CardKind.Minion);
            if (target == null)
            {
                return;
            }

            var board = context.State.Player.Board;
            var index = board.FindIndex(minion => minion.InstanceId == target.InstanceId);
            if (index < 0)
            {
                return;
            }

            var neighbors = new List<MinionInstance>();
            if (index > 0)
            {
                neighbors.Add(board[index - 1]);
            }

            if (index + 1 < board.Count)
            {
                neighbors.Add(board[index + 1]);
            }

            board.RemoveAt(index);
            if (neighbors.Count == 0)
            {
                return;
            }

            var attackShare = Math.Max(0, target.Attack) / neighbors.Count;
            var healthShare = Math.Max(0, target.MaxHealth) / neighbors.Count;
            foreach (var neighbor in neighbors.Where(minion => board.Any(current => current.InstanceId == minion.InstanceId)))
            {
                Buff(neighbor, attackShare, healthShare, "Nathanos Blightcaller");
            }

            result.Messages.Add("Nathanos Blightcaller: sold a friendly minion and split its stats amongst its neighbors.");
        }

        private static MinionInstance PickTargetedOrFirstOtherBoardMinion(HeroEffectContext context, Func<MinionInstance, bool> predicate)
        {
            var board = context.State.Player.Board;
            var playedIndex = context.Card == null ? -1 : board.FindIndex(minion => minion.InstanceId == context.Card.InstanceId);
            var targetIndex = context.TargetIndex;
            if (playedIndex >= 0 && targetIndex >= playedIndex)
            {
                targetIndex += 1;
            }

            if (targetIndex >= 0 && targetIndex < board.Count &&
                (context.Card == null || board[targetIndex].InstanceId != context.Card.InstanceId) &&
                predicate(board[targetIndex]))
            {
                return board[targetIndex];
            }

            return board.FirstOrDefault(card =>
                (context.Card == null || card.InstanceId != context.Card.InstanceId) &&
                predicate(card));
        }

        private static bool IsBattlecryMinion(MinionInstance card)
        {
            return card != null &&
                   (card.CardKind == CardKind.Minion || card.CardKind == CardKind.HeroBuddy) &&
                   (card.Keywords.Contains(Keyword.Battlecry) ||
                    (card.Text != null && card.Text.IndexOf("Battlecry", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    card.Tags.Any(tag => tag.IndexOf("battlecry", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static bool IsDeathrattleMinion(MinionInstance card)
        {
            return card != null &&
                   card.CardKind == CardKind.Minion &&
                   ((card.Text != null && card.Text.IndexOf("Deathrattle", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    card.Tags.Any(tag => tag.IndexOf("deathrattle", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static MinionInstance CreateGeneratedMinion(string cardId, string name, int attack, int health, Tribe tribe, string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-" + cardId.ToLowerInvariant() + "-" + suffix,
                DefinitionId = cardId.ToLowerInvariant(),
                CardId = cardId,
                Name = name,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Summon,
                OriginPoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_minion" }
            };
        }

        private static void AddBonusKeywordSet(MinionInstance target, string source)
        {
            AddKeyword(target, Keyword.Windfury, source);
            AddKeyword(target, Keyword.DivineShield, source);
            AddKeyword(target, Keyword.Taunt, source);
        }

        private static void RecordKurtrusMinionBought(HeroEffectContext context, HeroEffectResult result, MinionInstance card)
        {
            var tavern = context.State.Player.Tavern;
            tavern.HeroEffectCounters.TryGetValue(KurtrusBoughtRoundCounter, out var round);
            if (round != context.State.Round)
            {
                tavern.HeroEffectCounters[KurtrusBoughtRoundCounter] = context.State.Round;
                tavern.HeroEffectCounters[KurtrusBoughtCounter] = 0;
            }

            var bought = IncrementCounter(tavern, KurtrusBoughtCounter, 1);
            tavern.HeroEffectCounters.TryGetValue(KurtrusTriggeredRoundCounter, out var triggeredRound);
            if (bought >= 3 && triggeredRound != context.State.Round)
            {
                tavern.HeroEffectCounters[KurtrusTriggeredRoundCounter] = context.State.Round;
                AddPlainCopyToHand(context.State, card, "kurtrus", context.Minions);
                result.Messages.Add("Glaive Ricochet: gained a plain copy of a bought minion.");
            }
        }

        private static void TransformShopMinionsToTribe(HeroEffectContext context, Tribe tribe)
        {
            var shop = context.State.Player.Tavern.Shop;
            for (var i = 0; i < shop.Count; i += 1)
            {
                var original = shop[i];
                if (original == null || original.CardKind != CardKind.Minion)
                {
                    continue;
                }

                var candidates = context.Minions.All
                    .Where(minion => minion.Tribes.Contains(tribe) && minion.TavernTier == original.TavernTier)
                    .ToList();
                if (candidates.Count == 0)
                {
                    continue;
                }

                var definition = candidates[context.Rng.NextInt(candidates.Count)];
                shop[i] = MinionFactory.Create(definition, BoardSide.Player, "sparkfin-" + context.State.Round + "-" + i, source: PoolSource.Copy);
            }
        }

        private static void ApplySaurfangShopBuff(MatchState state)
        {
            var tavern = state.Player.Tavern;
            var health = GetCounterOrDefault(tavern, SaurfangHealthBonusCounter, 1);
            foreach (var minion in tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion))
            {
                SetTrackedBuff(minion, "For the Horde!", 1, health);
            }
        }

        private static void UpdateMalorneStats(MatchState state)
        {
            if (state?.Player?.Tavern == null)
            {
                return;
            }

            var bonus = Math.Max(0, state.Player.Tavern.GoldSpentThisGame / 3);
            foreach (var malorne in MatchingBoardBuddies(state, MalorneCardId))
            {
                SetTrackedBuff(malorne, "Malorne", bonus, bonus);
            }
        }

        private static void UpdateValithriaDreamwalkerStats(MatchState state)
        {
            if (state?.Player?.Tavern == null)
            {
                return;
            }

            var dragonCount = state.Player.Board.Count(minion => minion.Tribes.Contains(Tribe.Dragon));
            foreach (var buddy in MatchingBoardBuddies(state, ValithriaDreamwalkerCardId))
            {
                SetTrackedBuff(buddy, "Valithria Dreamwalker", dragonCount, dragonCount);
            }
        }

        public static int HandleOmuUpgradeRefund(MatchState state, int goldSpent)
        {
            if (state?.Player?.Tavern == null || !IsPower(state.Player.HeroPowerCardId, ForestWardenOmuPowerId))
            {
                return 0;
            }

            var refund = Math.Min(2, goldSpent);
            TavernRules.GainGold(state.Player.Tavern, refund);
            return refund;
        }

        private static IEnumerable<MinionInstance> MatchingBoardBuddies(MatchState state, string cardId)
        {
            return state?.Player?.Board == null
                ? Enumerable.Empty<MinionInstance>()
                : state.Player.Board.Where(minion => string.Equals(minion.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSameInstance(MinionInstance left, MinionInstance right)
        {
            return left != null &&
                   right != null &&
                   !string.IsNullOrEmpty(left.InstanceId) &&
                   string.Equals(left.InstanceId, right.InstanceId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasBuddy(MatchState state, string cardId)
        {
            return MatchingBoardBuddies(state, cardId).Any();
        }

        private static bool IsPower(string currentPowerId, string expectedPowerId)
        {
            return string.Equals(currentPowerId, expectedPowerId, StringComparison.OrdinalIgnoreCase);
        }

        private static int IncrementCounter(TavernState tavern, string key, int amount)
        {
            tavern.HeroEffectCounters.TryGetValue(key, out var current);
            current += amount;
            tavern.HeroEffectCounters[key] = current;
            return current;
        }

        private static int IncrementCombatKillCounter(MatchState state)
        {
            var tavern = state.Player.Tavern;
            tavern.HeroEffectCounters.TryGetValue(CombatKillRoundCounter, out var round);
            if (round != state.Round)
            {
                tavern.HeroEffectCounters[CombatKillRoundCounter] = state.Round;
                tavern.HeroEffectCounters[CombatKillCountCounter] = 0;
            }

            return IncrementCounter(tavern, CombatKillCountCounter, 1);
        }

        private static int GetCounterOrDefault(TavernState tavern, string key, int fallback)
        {
            return tavern.HeroEffectCounters.TryGetValue(key, out var current) ? current : fallback;
        }

        private static void EnsureCounters(TavernState tavern)
        {
            if (tavern.HeroEffectCounters == null)
            {
                tavern.HeroEffectCounters = new Dictionary<string, int>();
            }
        }

        private static void SpendGold(TavernState tavern, int amount)
        {
            if (tavern.Gold < amount)
            {
                throw new InvalidOperationException("Not enough Gold to use this Hero Power.");
            }

            tavern.Gold -= amount;
            tavern.GoldSpentThisTurn += amount;
            tavern.GoldSpentThisGame += amount;
        }

        private static void Buff(MinionInstance target, int attack, int health, string sourceId)
        {
            if (target == null || (attack == 0 && health == 0))
            {
                return;
            }

            StatMath.ApplyStatDelta(target, attack, health);
            target.Enchantments.Add(new Enchantment
            {
                Id = sourceId,
                SourceId = sourceId,
                AttackBonus = attack,
                HealthBonus = health
            });
        }

        private static int CountBonusKeywords(MinionInstance target)
        {
            if (target?.Keywords == null)
            {
                return 0;
            }

            return BonusKeywords.Count(keyword => target.Keywords.Contains(keyword));
        }

        private static void AddKeyword(MinionInstance target, Keyword keyword, string sourceId)
        {
            if (target == null)
            {
                return;
            }

            if (target.Keywords == null)
            {
                target.Keywords = new List<Keyword>();
            }

            if (target.Keywords.Contains(keyword))
            {
                return;
            }

            target.Keywords.Add(keyword);
            target.Enchantments.Add(new Enchantment
            {
                Id = sourceId + ":" + keyword,
                SourceId = sourceId,
                AddedKeywords = new List<Keyword> { keyword }
            });
        }

        private static void SetTrackedBuff(MinionInstance target, string sourceId, int attack, int health)
        {
            if (target == null)
            {
                return;
            }

            var existing = target.Enchantments.FirstOrDefault(enchantment => enchantment.SourceId == sourceId);
            var currentAttack = existing?.AttackBonus ?? 0;
            var currentHealth = existing?.HealthBonus ?? 0;
            StatMath.ApplyStatDelta(
                target,
                StatMath.SaturatingDelta(attack, currentAttack),
                StatMath.SaturatingDelta(health, currentHealth));

            if (existing == null)
            {
                target.Enchantments.Add(new Enchantment
                {
                    Id = sourceId,
                    SourceId = sourceId,
                    AttackBonus = attack,
                    HealthBonus = health
                });
                return;
            }

            existing.AttackBonus = attack;
            existing.HealthBonus = health;
        }

        private static void RemoveTrackedBuff(MinionInstance target, string sourceId)
        {
            if (target == null)
            {
                return;
            }

            var existing = target.Enchantments.FirstOrDefault(enchantment => enchantment.SourceId == sourceId);
            if (existing == null)
            {
                return;
            }

            StatMath.ApplyStatDeltaPreservingDamage(
                target,
                StatMath.SaturatingSubtract(0, existing.AttackBonus),
                StatMath.SaturatingSubtract(0, existing.HealthBonus));
            target.Enchantments.Remove(existing);
        }

        private static void RemoveKeywordFromSource(MinionInstance target, Keyword keyword, string sourceId)
        {
            if (target == null)
            {
                return;
            }

            var existing = target.Enchantments.FirstOrDefault(enchantment =>
                enchantment.SourceId == sourceId && enchantment.AddedKeywords.Contains(keyword));
            if (existing == null)
            {
                return;
            }

            target.Enchantments.Remove(existing);
            var stillGranted = target.Enchantments.Any(enchantment => enchantment.AddedKeywords.Contains(keyword)) ||
                               (target.OfficialKeywords != null && target.OfficialKeywords.Contains(keyword));
            if (!stillGranted)
            {
                target.Keywords.Remove(keyword);
            }
        }

        private static void AddTag(MinionInstance target, string tag)
        {
            if (target != null && !target.Tags.Contains(tag))
            {
                target.Tags.Add(tag);
            }
        }
    }
}
