using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public sealed class MatchService
    {
        private const int BoardLimit = 7;
        private const int HandLimit = 10;
        private const int BuyCost = 3;
        private const int RerollCost = 1;
        private const int SellValue = 1;
        private const string TripleRewardDefinitionId = "triple-reward";
        private const string TripleRewardCardId = "TRIPLE_REWARD";
        private const string TripleRewardGrantedCounter = "triple-reward-granted";
        private const string PatchwerkHeroCardId = "TB_BaconShop_HERO_34";
        private const string SireDenathriusHeroCardId = "BG24_HERO_100";
        private const string ShadyAristocratCardId = "BG24_HERO_100_Buddy";
        private const string GoldenShadyAristocratCardId = "BG24_HERO_100_Buddy_G";
        private const string ShadyCoinPouchRewardId = "LH_Reward_CoinPouch8";
        private const string GoldenShadyCoinPouchRewardId = "LH_Reward_CoinPouch16";
        private const string AnimaBribeRewardId = "BG24_Reward_305";
        private const string InvigoratingConchRewardId = "BG27_Reward_503";
        private const string DoubleHeadedRewardId = "BG28_Reward_506";
        private const string StashOfTheScribeRewardId = "BG28_Reward_515";
        private const string BeyondTheMirageRewardId = "BG28_Reward_500";
        private const string BloodsoakedTomeRewardId = "BG27_Reward_811";
        private const string SplittingScrollRewardId = "BG28_Reward_502";
        private const string GoldenForgeRewardId = "BG33_Reward_013";
        private const string SnickerSnacksRewardId = "BG24_Reward_107";
        private const string StolenGoldRewardId = "BG24_Reward_109";
        private const string EvilTwinRewardId = "BG24_Reward_111";
        private const string RitualDaggerRewardId = "BG24_Reward_113";
        private const string SecretSinstoneRewardId = "BG24_Reward_129";
        private const string PartnerInCrimeRewardId = "BG24_Reward_310";
        private const string DoppelgangersLocketRewardId = "BG27_Reward_806";
        private const string OpenAuditionsRewardId = "BG28_Reward_513";
        private const string VictimsSpecterRewardId = "BG24_Reward_138";
        private const string TealTigerSapphireRewardId = "BG24_Reward_308";
        private const string DevilsInTheDetailsRewardId = "BG24_Reward_309";
        private const string GiftOfTheGoldenKoboldRewardId = "BG28_Reward_508";
        private const string QuaintBoutiqueRewardId = "BG33_Reward_014";
        private const string JumboWarehouseRewardId = "BG33_Reward_015";
        private const string CosmicRewardId = "BG33_Reward_017";
        private const string TheotarsParasolRewardId = "BG24_Reward_115";
        private const string ExquisiteConchRewardId = "BG24_Reward_123";
        private const string TheSmokingGunRewardId = "BG24_Reward_125";
        private const string MirrorShieldRewardId = "BG24_Reward_128";
        private const string RedHandRewardId = "BG24_Reward_131";
        private const string StaffOfOriginationRewardId = "BG24_Reward_312";
        private const string AlterEgoRewardId = "BG24_Reward_321";
        private const string MenagerieMayhemRewardId = "BG24_Reward_331";
        private const string VolatileVenomRewardId = "BG24_Reward_364";
        private const string BloodGobletRewardId = "BG24_Reward_708";
        private const string SinfallMedallionRewardId = "BG24_Reward_712";
        private const string EnhanceAMaticRewardId = "BG24_Reward_715";
        private const string BoomSquadRewardId = "BG27_Reward_502";
        private const string SturdyShardRewardId = "BG27_Reward_804";
        private const string MapOfTheUnknownRewardId = "BG27_Reward_810";
        private const string EndlessBloodMoonRewardId = "BG27_Reward_815";
        private const string TumblingDisasterRewardId = "BG28_Reward_505";
        private const string RighteousChargeRewardId = "BG33_Reward_003";
        private const string GrimFreshenerRewardId = "BG33_Reward_004";
        private const string RushingWindsRewardId = "BG33_Reward_006";
        private const string CycleOfEnergyRewardId = "BG28_Reward_504";
        private const string StableAmalgamationRewardId = "BG28_Reward_518";
        private const string TurbulentTombsRewardId = "BG27_Reward_803";
        private const string GhastlyMaskRewardId = "BG24_Reward_130";
        private const string FriendsAlongTheWayRewardId = "BG24_Reward_134";
        private const string YoggTasticTastiesRewardId = "BG24_Reward_135";
        private const string AnotherHiddenBodyRewardId = "BG24_Reward_311";
        private const string WondrousWisdomballRewardId = "BG24_Reward_313";
        private const string PilferedLampsRewardId = "BG24_Reward_350";
        private const string EssenceOfZerusRewardId = "BG24_Reward_362";
        private const string EtherealEvidenceRewardId = "BG24_Reward_363";
        private const string KidnapSackRewardId = "BG24_Reward_718";
        private const string GoldenHammerRewardId = "BG24_Reward_719";
        private const string TimelineAccelerationRewardId = "BG27_Reward_504";
        private const string GilneanWarHornRewardId = "BG27_Reward_802";
        private const string ScepterOfGuidanceRewardId = "BG27_Reward_812";
        private const string TemporalTamperingRewardId = "BG28_Reward_501";
        private const string SmeltingChamberRewardId = "BG28_Reward_509";
        private const string SecretCulpritRewardId = "BG28_Reward_510";
        private const string UntamedSorceryRewardId = "BG28_Reward_514";
        private const string NorgannonsRewardId = "BG33_Reward_010";
        private const string MagicfinRelicRewardId = "BG33_Reward_011";
        private const string PerpetualIncantationRewardId = "BG33_Reward_020";
        private const string RallyingCryRewardId = "BG33_Reward_021";
        private const string EnhanceAMaticTauntSpellCardId = "BG24_Reward_715t";
        private const string EnhanceAMaticWindfurySpellCardId = "BG24_Reward_715t2";
        private const string EnhanceAMaticDivineShieldSpellCardId = "BG24_Reward_715t3";
        private const string EnhanceAMaticRebornSpellCardId = "BG24_Reward_715t4";
        private const string RushingWindsSpellCardId = "BG33_Reward_006t";
        private const string TimelineAcceleratorSpellCardId = "BG27_Reward_504t";
        private const string KidnapSackSpellCardId = "BG24_Reward_718t";
        private const string GoldenHammerSpellCardId = "BG24_Reward_719t";
        private const string ShifterZerusProxyCardId = "BG24_Reward_362t";
        private const string MagicfinTokenCardId = "BG33_Reward_011t";
        private const string BloodGemCardId = "BLOOD_GEM";
        private const string TitusRivendareCardId = "BG25_354";
        private const string BassgillCardId = "BG26_350";
        private const string AureateLaureateCardId = "BG32_236";
        private const string FlightyScoutCardId = "BG32_330";
        private const string GluttonousTroggCardId = "BG35_801";
        private const string GluttonousTroggBuyCounter = "gluttonous-trogg-buys";
        private const string GluttonousTroggClaimedCounter = "gluttonous-trogg-claimed";
        private const string OminousSeerCardId = "BG31_330";
        private const string PickyEaterCardId = "BG24_009";
        private const string RazorfenGeomancerCardId = "BG20_100";
        private const string RiverSkipperCardId = "BG33_140";
        private const string ScarletSurvivorCardId = "BG35_814";
        private const string SouthseaBuskerCardId = "BG26_135";
        private const string SunBaconRelaxerCardId = "BG20_301";
        private const string UpbeatFrontdrakeCardId = "BG26_529";
        private const string WrathWeaverCardId = "BGS_004";
        private const string AlertAlarmistCardId = "BG35_340";
        private const string BristlebackBullyCardId = "BG35_432";
        private const string FireBallerCardId = "BG31_816";
        private const string ForestRoverCardId = "BG31_801";
        private const string FreedealingGamblerCardId = "BGS_049";
        private const string HummingBirdCardId = "BG26_805";
        private const string IntrepidBotanistCardId = "BG32_237";
        private const string NerubianDeathswarmerCardId = "BG25_011";
        private const string OozelingGladiatorCardId = "BG27_002";
        private const string PatientScoutCardId = "BG24_715";
        private const string ProphetOfTheBoarCardId = "BG20_203";
        private const string SellementalCardId = "BGS_115";
        private const string ShellCollectorCardId = "BG23_002";
        private const string SnowBallerCardId = "BG31_818";
        private const string SoulRewinderCardId = "BG26_174";
        private const string TadCardId = "BG22_202";
        private const string MetallicHunterCardId = "BG32_170";
        private const string DrBoomsMonsterCardId = "BG32_172";
        private const string SlimyShieldCardId = "SLIMY_SHIELD";
        private const string BristlebackBloodGemCardId = "BRISTLEBACK_BLOOD_GEM";
        private const string RebornBloodGemCardId = "REBORN_BLOOD_GEM";
        private const string SnarlingConductorCardId = "BG28_585";
        private const string PointyArrowCardId = "100596";
        private const string LabAssistantCardId = "BG35_150";
        private const string DemonFodderCardId = "DEMON_FODDER";
        private const string HastyExcavationCardId = "104559";
        private const string TarecgosaCardId = "BG21_015";
        private const string EternalKnightCardId = "BG25_008";
        private const string AncestralAutomatonCardId = "BG_TTN_401";
        private const string OldSoulCardId = "BG34_231";
        private const string ReefRifferCardId = "BG26_501";
        private const string SurfNSurfCardId = "BG27_004";
        private const string LavaLurkerCardId = "BG23_009";
        private const string ReefRifferSpellCardId = "REEF_RIFFER_SPELL";
        private const string SurfNSurfSpellCardId = "SURF_N_SURF_SPELL";
        private const string DeepSeaAnglerSpellCardId = "DEEP_SEA_ANGLER_SPELL";
        private const string DeepBlueSpellCardId = "DEEP_BLUE_SPELL";
        private const string VolcanicVisitorAttackSpellCardId = "VOLCANIC_VISITOR_ATTACK_SPELL";
        private const string VolcanicVisitorHealthSpellCardId = "VOLCANIC_VISITOR_HEALTH_SPELL";
        private const string FrostlingPriestessSpellCardId = "FROSTLING_PRIESTESS_SPELL";
        private const string PreciousPearlSpellCardId = "TRINKET_PRECIOUS_PEARL_SPELL";
        private const string OphidianStaffSpellCardId = "TRINKET_OPHIDIAN_STAFF_SPELL";
        private const string VibrantBubbleSpellCardId = "TRINKET_VIBRANT_BUBBLE_SPELL";
        private const string DoubleStitchNeedleSpellCardId = "TRINKET_DOUBLE_STITCH_NEEDLE_SPELL";
        private const string TokenOfTheOldGodsSpellCardId = "TRINKET_TOKEN_OF_THE_OLD_GODS_SPELL";
        private const string ChillmereMosaicSpellCardId = "TRINKET_CHILLMERE_MOSAIC_SPELL";
        private const string TimewarpedGlowscaleSpellCardId = "TIMEWARPED_GLOWSCALE_SPELL";
        private const string WearyMageSpellCardId = "WEARY_MAGE_SPELL";
        private const string ThaumaturgistSpellCardId = "THAUMATURGIST_SPELL";
        private const string TemporarySpellcraftSourceId = "Temporary Spellcraft";
        private const string TemporaryVenomousSourceId = "Temporary Venomous";
        private const string PermanentSpellcraftCounter = "permanent_spellcraft_left";
        private const string AllSpellsCastThisGameCounter = "all_spells_cast_this_game";
        private const string ArchaicScrollSpellCounter = "archaic_scroll_spells";
        private const string BallerPortraitElementalCounter = "baller_portrait_elementals";
        private const string BloodboundEarringsSpellCounter = "bloodbound_earrings_spells";
        private const string ChromaticTearBattlecryCounter = "chromatic_tear_battlecries";
        private const string Batch2ScheduleCounterPrefix = "trinket:schedule:";
        private const string CliffdiverBattlecryThisTurnCounter = "trinket:cliffdiver:battlecries_this_turn";
        private const string MurkyBattlecryThisGameCounter = "trinket:murky:battlecries_this_game";
        private const string MarineSignetMinionCounter = "trinket:marine_signet:minions";
        private const string MarineSignetTierCounter = "trinket:marine_signet:tier";
        private const string WindfallSoldThisTurnCounter = "trinket:windfall:sold_this_turn";
        private const string CopperCoilCounterPrefix = "trinket:copper_coil:";
        private const string SpitescaleSushiRollExtraCastsLeftCounter = "spitescale_sushi_roll_extra_casts_left";
        private const string DazzlingDaggerAuraSourceId = "Trinket:Dazzling Dagger";
        private const int AutomaticTavernSpellCastMaxDepth = 4;
        private const string GlobalEternalKnightSourceId = "Eternal Knight";
        private const string GlobalAutomatonSourceId = "Ancestral Automaton";
        private const string PatientScoutTierCounter = "patient-scout-tier";
        private const string UpbeatFrontdrakeTurnCounter = "upbeat-frontdrake-turns";
        private const string LockedTurnsCounter = "locked-turns";
        private const string AnnoyOModuleCardId = "BG_BOT_911";
        private const string DeepSeaAnglerCardId = "BG23_004";
        private const string BristlebackScrapSmithCardId = "BG24_707";
        private const string PeggyCardId = "BG25_032";
        private const string PufferquilCardId = "BG25_039";
        private const string FelElementalCardId = "BG25_041";
        private const string AccordOTronCardId = "BG26_147";
        private const string JazzerCardId = "BG26_159";
        private const string DeepBlueCroonerCardId = "BG26_502";
        private const string MalchezaarCardId = "BG26_524";
        private const string GunpowderCourierCardId = "BG26_810";
        private const string ChronoCaptainHooktailCardId = "BG27_005";
        private const string MutableBeetleCardId = "BG27_084";
        private const string SprightlyScarabRebornOptionCardId = "SPRIGHTLY_SCARAB_REBORN_OPTION";
        private const string SprightlyScarabWindfuryOptionCardId = "SPRIGHTLY_SCARAB_WINDFURY_OPTION";
        private const string DisguisedGraverobberCardId = "BG28_303";
        private const string UtilityDroneCardId = "BG31_859";
        private const string LostCityLooterCardId = "BG33_820";
        private const string HealthyBountyCardId = "BG33_811";
        private const string HostileBountyCardId = "BG33_812";
        private const string SelfishBountyCardId = "BG33_813";
        private const string FriendlyBountyCardId = "BG33_814";
        private const string WealthyBountyCardId = "BG33_815";
        private const string OfficialHealthyBountyCardId = "122182";
        private const string OfficialHostileBountyCardId = "122183";
        private const string OfficialSelfishBountyCardId = "122184";
        private const string OfficialFriendlyBountyCardId = "122185";
        private const string OfficialWealthyBountyCardId = "122186";
        private const string DreamersEmbraceCardNumber = "105266";
        private const string NaturalBlessingCardNumber = "104472";
        private const string CloningConchCardNumber = "110400";
        private const string DuplicatingLensCardNumber = "130853";
        private const string GoldenizerCardNumber = "98914";
        private const string MaraudersContractCardNumber = "BG31_891";
        private const string JewelryBoxTauntGemCardId = "TRINKET_JEWELRY_BOX_TAUNT_GEM";
        private const string JewelryBoxDivineShieldGemCardId = "TRINKET_JEWELRY_BOX_DIVINE_SHIELD_GEM";
        private const string JewelryBoxRebornGemCardId = "TRINKET_JEWELRY_BOX_REBORN_GEM";
        private const string ColdlightDiverCardId = "BG33_894";
        private const string BlueChromawhelpCardId = "BG34_634t";
        private const string BlackChromawhelpCardId = "BG34_635t";
        private const string GreenChromawhelpCardId = "BG34_636t";
        private const string BronzeChromawhelpCardId = "BG34_637t";
        private const string RedChromawhelpCardId = "BG34_638t";
        private const string BristlingDrummerCardId = "BG34_683";
        private const string JuvenileWaveCardId = "BG34_856";
        private const string MurgletonAuntieCardId = "BG35_140";
        private const string MurgletonDaddyCardId = "BG35_141";
        private const string BloodGemBarrageCardNumber = "126676";
        private const string RefreshingAnomalyCardId = "BGS_116";
        private const string TavernTempestCardId = "BGS_123";
        private const string BlueshellTurtleCardId = "BG24_018";
        private const string KingBagurgleCardId = "BGS_030";
        private const string ZestyShakerCardId = "BG26_505";
        private const string GemSmugglerRuggugCardId = "BG28_583";
        private const string TrigoreTheLasherCardId = "BG29_807";
        private const string DevoutSatyressCardId = "BG33_155";
        private const string PersistentPoetCardId = "BG29_813";
        private const string PricklyPiperCardId = "BG26_525";
        private const string VolcanicVisitorCardId = "BG30_117";
        private const string FearlessFoodieCardId = "BG30_123";
        private const string FearlessFoodieGrowthOptionCardId = "FEARLESS_FOODIE_GROWTH_OPTION";
        private const string FearlessFoodieGemsOptionCardId = "FEARLESS_FOODIE_GEMS_OPTION";
        private const string FrostlingPriestessCardId = "BG33_319";
        private const string DoomsdayDragonEggCardId = "BG34_639";
        private const string ProudPrivateerCardId = "BG33_825";
        private const string GrittyHeadhunterCardId = "BG31_822";
        private const string HackerfinCardId = "BG31_148";
        private const string BalladistCardId = "BG26_814";
        private const string FilletfighterCardId = "BG26_137";
        private const string FeedingTigerSharkCardId = "BG34_523";
        private const string SignatureTimerCardId = "BG31_178";
        private const string ScrapperCardId = "BG29_503";
        private const string BrannosaurCardId = "BG34_865";
        private const string DustyCycloneCardId = "BG32_841";
        private const string DeepwaterChieftainCardId = "BG35_143";
        private const string ManasparkCardId = "BG35_881";
        private const string HumongousCardId = "BG32_341";
        private const string EnchantedDrudgeCardId = "BG35_341";
        private const string FriendlyFelboarCardId = "BG32_880";
        private const string AbyssalBrawlerCardId = "BG35_921";
        private const string SaloonDancerCardId = "BG35_702";
        private const string BristlingGemcultivatorCardId = "BG35_433";
        private const string WoodlandDefilerCardId = "BG35_151";
        private const string WildfireExecutionerCardId = "BG34_500";
        private const string PlaguedGhoulCardId = "BG34_690";
        private const string DualWieldPirateCardId = "BG31_824";
        private const string RylakMetalheadCardId = "BG26_801";
        private const string RylakSpellBonusSourceId = "Heavy Metal Wyrm";
        private const string DeepwaterSchoolCardId = "131218";
        private const string ArcaneConsumptionCardId = "130311";
        private const string DisturbedGraveCounter = "disturbed-grave-round";
        private const string NomiCardId = "BGS_104";
        private const string MoonEaterChampionCardId = "BG29_840";
        private const string BrannBronzebeardCardId = "BG_LOE_077";
        private const string BoarHerderCardId = "BG33_888";
        private const string NalaaCardId = "BG28_551";
        private const string CataclysmicChampionCardId = "BG35_123";
        private const string ArenaShowmanCardId = "BG28_550";
        private const string DynamicDuoCardId = "BG26_199";
        private const string DrakkariEnchanterCardId = "BG26_ICC_901";
        private const string LightfangEnforcerCardId = "BGS_009";
        private const string FarmhandWhirlOMatronCardId = "BG26_162";
        private const string FirelandsFlameCardId = "BG35_882";
        private const string NightmareParlorGuestCardId = "BG32_111";
        private const string VoidpupTrainerCardId = "BG35_152";
        private const string FamishedFelbatCardId = "BG21_005";
        private const string FelboarCardId = "BG28_633";
        private const string FelFlameDrakeCardId = "BG32_821";
        private const string AshenCorruptorCardId = "BG32_873";
        private const string ChargingCzarinaCardId = "BG28_741";
        private const string DarnassusPieEffectId = "darnassus_pie";
        private const string DarnassusPieDoubleEffectId = "darnassus_pie_double";
        private const string DesignerEyepatchEffectId = "designer_eyepatch";
        private const string DefilerPortraitEffectId = "defiler_portrait";
        private const string DefilerPortraitGreaterEffectId = "defiler_portrait_greater";
        private const string DragonwingGliderEffectId = "dragonwing_glider";
        private const string DragonwingGliderGreaterEffectId = "dragonwing_glider_greater";
        private const string FeralTalismanEffectId = "feral_talisman";
        private const string ArtisanalUrnEffectId = "artisanal_urn";
        private const string GildedAnchorEffectId = "gilded_anchor";
        private const string LorewalkerScrollEffectId = "lorewalker_scroll";
        private const string NerglishPhrasebookEffectId = "nerglish_phrasebook";
        private const string NomiStickerEffectId = "nomi_sticker";
        private const string FountainPenEffectId = "fountain_pen";
        private const string FountainPenSourceId = "Fountain Pen";
        private const string GreatBoarStickerEffectId = "great_boar_sticker";
        private const string BluegillFlippersEffectId = "bluegill_flippers";
        private const string SpellPoweredWrenchEffectId = "spell_powered_wrench";
        private const string RecyclingStickerEffectId = "recycling_sticker";
        private const string AuricOfferingEffectId = "auric_offering";
        private const string ToxicStingerEffectId = "toxic_stinger";
        private const string EnigmaticHeadstoneEffectId = "enigmatic_headstone";
        private const string ToughTuskStickerEffectId = "tough_tusk_sticker";
        private const string TemporaryToughTuskDivineShieldTag = "temporary_tough_tusk_divine_shield";
        private const string EggOfEndtimesPortraitEffectId = "egg_of_the_endtimes_portrait";
        private const string EssenceOfDreamsEffectId = "essence_of_dreams";
        private const string ChromaticTearLesserEffectId = "chromatic_tear_lesser";
        private const string MechaJaraxxusStickerEffectId = "mecha_jaraxxus_sticker";
        private const string PrivateerPortraitEffectId = "privateer_portrait";
        private const string SunkenAnchorEffectId = "sunken_anchor";
        private const string ErrglStickerEffectId = "errgl_sticker";
        private const string GrittyPortraitEffectId = "gritty_portrait";
        private const string JewelryBoxEffectId = "jewelry_box";
        private const string ConchPortraitEffectId = "conch_portrait";
        private const string LensCaseEffectId = "lens_case";
        private const string AzerothModelGlobeEffectId = "azeroth_model_globe";
        private const string GoldPendantEffectId = "gold_pendant";
        private const string GoldenizerSupplyEffectId = "goldenizer_supply";
        private const string RendleStickerEffectId = "rendle_sticker";
        private const string ExquisiteDishwareEffectId = "exquisite_dishware";
        private const string HackerfinPortraitEffectId = "hackerfin_portrait";
        private const string WindfallPortraitEffectId = "windfall_portrait";
        private const string CliffdiverStickerEffectId = "cliffdiver_sticker";
        private const string MurkyStickerEffectId = "murky_sticker";
        private const string BlessingPortraitEffectId = "blessing_portrait";
        private const string MarineSignetEffectId = "marine_signet";
        private const string ElectrodeAttractorEffectId = "electrode_attractor";
        private const string GuidingCandleEffectId = "guiding_candle";
        private const string UpstartEmbersEffectId = "upstart_embers";
        private const string WarbandWhistleEffectId = "warband_whistle";
        private const string BattlecruiserPortraitEffectId = "battlecruiser_portrait";
        private const string DemonicTapestryEffectId = "demonic_tapestry";
        private const string FinleysHelmetEffectId = "finleys_helmet";
        private const string InnkeepersSteinEffectId = "innkeepers_stein";
        private const string FelbatPortraitEffectId = "felbat_portrait";
        private const string NetherPendantEffectId = "nether_pendant";
        private const string GlowingGauntletEffectId = "glowing_gauntlet";
        private const string PilgrimpStickerEffectId = "pilgrimp_sticker";
        private const string BazaarStickerEffectId = "bazaar_sticker";
        private const string MagicfinStickerEffectId = "magicfin_sticker";
        private const string EyeOfSargerasEffectId = "eye_of_sargeras";
        private const string GrifterPortraitEffectId = "grifter_portrait";
        private const string ExtravagantScaleEffectId = "extravagant_scale";
        private const string FancySpellbookEffectId = "fancy_spellbook";
        private const string SharkCannonEffectId = "shark_cannon";
        private const string MawCasterPortraitEffectId = "maw_caster_portrait";
        private const string SafetyPatchEffectId = "safety_patch";
        private const string ElectromagneticDeviceEffectId = "electromagnetic_device";
        private const string InnkeepersHearthEffectId = "innkeepers_hearth";
        private const string KaleidoscopeEffectId = "kaleidoscope";
        private const string JailerStickerEffectId = "jailer_sticker";
        private const string DemonbloodGourdEffectId = "demonblood_gourd";
        private const string StatueOfHireekEffectId = "statue_of_hireek";
        private const string ShakerPortraitEffectId = "shaker_portrait";
        private const string TranscribingTypewriterEffectId = "transcribing_typewriter";
        private const string CuratorStickerEffectId = "curator_sticker";
        private const string SplinterOfAurumEffectId = "splinter_of_aurum";
        private const string HornOfSummoningEffectId = "horn_of_summoning";
        private const string MagiciansTopHatEffectId = "magicians_top_hat";
        private const string ShrineOfEvolutionEffectId = "shrine_of_evolution";
        private const string TideRaiserPortraitEffectId = "tide_raiser_portrait";
        private const string PortableFactoryEffectId = "portable_factory";
        private const string ReplicaCathedralEffectId = "replica_cathedral";
        private const string JarredFrostlingEffectId = "jarred_frostling";
        private const string PowderKegEffectId = "powder_keg";
        private const string PromoPortraitEffectId = "promo_portrait";
        private const string SkyGolemPortraitEffectId = "sky_golem_portrait";
        private const string ValdrakkenWindChimesEffectId = "valdrakken_wind_chimes";
        private const string HoggyBankEffectId = "hoggy_bank";
        private const string ShipInABottleEffectId = "ship_in_a_bottle";
        private const string GilneanThornedRoseEffectId = "gilnean_thorned_rose";
        private const string JarOGemsEffectId = "jar_o_gems";
        private const string MugOfTheSireEffectId = "mug_of_the_sire";
        private const string ThornspikePauldronEffectId = "thornspike_pauldron";
        private const string TigerCarvingEffectId = "tiger_carving";
        private const string BlingtronsSunglassesEffectId = "blingtrons_sunglasses";
        private const string ScrapsmithPortraitEffectId = "scrapsmith_portrait";
        private const string RustyTridentEffectId = "rusty_trident";
        private const string EyeOfDalaranEffectId = "eye_of_dalaran";
        private const string ElementiumChestEffectId = "elementium_chest";
        private const string AccordOTronPortraitEffectId = "accord_o_tron_portrait";
        private const string GuidingCandleRoundCounter = "trinket:guiding_candle:round";
        private const string GuidingCandleRefreshesCounter = "trinket:guiding_candle:refreshes";
        private const string WarbandWhistlePendingCounter = "trinket:warband_whistle:pending";
        private const string DemonicTapestryRefreshCounter = "trinket:demonic_tapestry:refreshes";
        private const string PilgrimpRoundCounter = "trinket:pilgrimp:round";
        private const string PilgrimpUsedCounter = "trinket:pilgrimp:used";
        private const string BazaarRoundCounter = "trinket:bazaar:round";
        private const string BazaarUsedCounter = "trinket:bazaar:used";
        private const string MagicfinRoundCounter = "trinket:magicfin:round";
        private const string MagicfinUsesCounter = "trinket:magicfin:uses";
        private const string EyeOfSargerasBuyCounter = "trinket:eye_of_sargeras:buys";
        private const string GrifterRoundCounter = "trinket:grifter:round";
        private const string GrifterUsedCounter = "trinket:grifter:used";
        private const string ExtravagantScaleProgressCounter = "trinket:extravagant_scale:progress";
        private const string ExtravagantScaleTriggersCounter = "trinket:extravagant_scale:triggers";
        private const string FancySpellbookProgressCounter = "trinket:fancy_spellbook:progress";
        private const string SharkCannonProgressCounter = "trinket:shark_cannon:progress";
        private const string SharkCannonBonusCounter = "trinket:shark_cannon:bonus";
        private const string NetherPendantDamageCounter = "trinket:nether_pendant:damage";
        private const string NetherPendantBonusCounter = "trinket:nether_pendant:bonus";
        private const string TypewriterRemainingCounterPrefix = "trinket:typewriter:";
        private const string StatueOfHireekProgressCounter = "trinket:statue_of_hireek:progress";
        private const string SplinterOfAurumClaimedCounter = "trinket:splinter_of_aurum:claimed";
        private const string PortableFactoryCatalogIndexCounterPrefix = "trinket:portable_factory:";
        private const string ReplicaCathedralRoundCounter = "trinket:replica_cathedral:round";
        private const string ReplicaCathedralUsedCounter = "trinket:replica_cathedral:used";
        private const string DemonicTapestryHealthCostTag = "trinket_demonic_tapestry_health_cost";
        private const string TaughtTavernSpellTagPrefix = "taught_tavern_spell:";
        private const string Batch3HealthCostPilgrimp = "pilgrimp";
        private const string Batch3HealthCostBazaar = "bazaar";
        private const string Batch3HealthCostEye = "eye_of_sargeras";
        private const string Batch3HealthCostDemonicTapestry = "demonic_tapestry";
        private const string Batch3HealthCostHastyExcavation = "hasty_excavation";
        private const string Batch3FreeCostGrifter = "grifter";
        private const string BaseBuyCostCounter = "base_buy_cost";
        private const string PrizedPromoDrakeCardId = "BG21_014";
        private const string DalaranCheeseWheelAuraSourceId = "Trinket:Dalaran Cheese Wheel";
        private const string DarnassusPieAuraSourceId = "Trinket:Darnassus Pie";
        private const string DefilerPortraitAuraSourceId = "Trinket:Defiler Portrait";
        private const string NetherPendantAuraSourceId = "Trinket:Nether Pendant";
        private const string GlowingGauntletAuraSourceId = "Trinket:Glowing Gauntlet";
        private const string FeralTalismanAuraSourceId = "Trinket:Feral Talisman";
        private const string ArtisanalUrnAuraSourceId = "Trinket:Artisanal Urn";
        private const string BrashPirateCardId = "BG35_701";
        private const string ShipwreckedCaptainCardId = "BG33_821";
        private const string ObsidianRavagerCardId = "BG33_825";
        private const string MaelstromNagaCardId = "BG34_922";
        private const string SereneMeditatorCardId = "BG32_835";
        private const string DarkcrestStrategistCardId = "BG31_920";
        private const string GlowscaleCardId = "BG23_008";
        private const string LivingAzeriteCardId = "BG28_707";
        private const string WearyMageCardId = "BG31_830";
        private const string TimewarpedGlowscaleCardId = "BG34_Giant_035";
        private const string ThaumaturgistCardId = "BG31_924";
        private const string ArcaneBehemothCardId = "BG31_360";
        private const string FacelessManipulatorCardId = "BG_EX1_564";
        private const string TimewarpedPoetCardId = "BG34_Giant_314";
        private const string TimewarpedRadioStarCardId = "BG34_Giant_330";
        private const string TimewarpedLeapfroggerCardId = "BG34_Giant_031";
        private const string TimewarpedSkipperCardId = "BG34_Giant_072";
        private const string FishOfNzothCardId = "TB_BaconShop_HP_105t";
        private const string MoonRiderCardId = "BG35_602";
        private const string ThreeLilQuilboarCardId = "BG26_867";
        private const string DarkgazeElderCardId = "BG23_018";
        private const string HotAirSurveyorCardId = "BG30_121";
        private const string MurculesCardId = "BG35_142";
        private const string OperaticBelcherCardId = "BG33_318";
        private const string TideOracleMorglCardId = "BG35_895";
        private const string PrimalfinLookoutCardId = "BGS_020";
        private const string MurlocBurglarCardId = "BG30_122";
        private const string KalecgosCardId = "BGS_041";
        private const string DragonCaretakerCardId = "BG34_633";
        private const string WindfallTornadoCardId = "BG34_858";
        private const string ScreamingBansheeCardId = "BG35_334";
        private const string KelThuzadCardId = "BG28_308";
        private const string EternalSummonerCardId = "BG25_009";
        private const string CharlgaCardId = "BG26_157";
        private const string BristlebachPortraitMinionCardId = "BG26_157";
        private const string SurpriseElementalCardId = "BG26_175";
        private const string GreymanesChampionCardId = "BG29_841";
        private const string GroundbreakerCardId = "BG31_035";
        private const string MoonsteelJuggernautCardId = "BG31_171";
        private const string SpacefarerCardId = "BG31_820";
        private const string DrustfallenButcherHighCardId = "BG32_234";
        private const string FireforgedEvokerCardId = "BG32_822";
        private const string WildfireManasurgeCardId = "BG32_846";
        private const string AirAdmiralRogersCardId = "BG33_823";
        private const string FelfinFungalmancerCardId = "BG33_891";
        private const string PrimalfinPortraitistCardId = "BG33_893";
        private const string SlitherspearCardId = "BG33_920";
        private const string ShatteredMatriarchCardId = "BG33_923";
        private const string ScrapbookingStudentCardId = "BG34_175";
        private const string RabidSauroliskCardId = "BG34_321";
        private const string ForsakenThalnosCardId = "BG34_692";
        private const string QueenGuardCardId = "BG34_926";
        private const string StrengthIngestorCardId = "BG35_153";
        private const string TwistedWrathguardCardId = "BG35_155";
        private const string FallenSkyGolemCardId = "BG35_342";
        private const string EarthsongShamanCardId = "BG35_431";
        private const string ThornedTrailblazerCardId = "BG35_437";
        private const string BelindaStonehearthCardId = "BG35_883";
        private const string BloodChampionCardId = "BG23_017";
        private const string CaptainSandersCardId = "BG25_034";
        private const string PolarizingBeatboxerCardId = "BG26_149";
        private const string SargerasChampionCardId = "BG27_016";
        private const string SeaWitchZarJiraCardId = "BG27_514";
        private const string MurozondThiefCardId = "BG34_145";
        private const string RheaSupremeWardenCardId = "BG34_319";
        private const string StoneAgeRockRockCardId = "BG34_950";
        private const string SacredGiftCardNumber = "122899";
        private const string ConflagrationCardNumber = "130310";
        private const string QueensCommandCardNumber = "130713";
        private const string MenagerieTablewareCardNumber = "105271";
        private const string StaffOfEnrichmentCardNumber = "105276";
        private const string DisturbedGraveCardNumber = "126957";
        private const string ButcheringCardNumber = "110412";
        private const string ChannelTheDevourerCardNumber = "100899";
        private const string AzeriteEmpowermentCardNumber = "109232";
        private const string KnockoffWisdomballCardNumber = "113902";
        private const string BorrowingEastWindCardNumber = "126909";
        private const string SpitescaleSpecialCardNumber = "110406";
        private const string MountingAvalancheCardNumber = "122862";
        private const string MightOfStormwindCardNumber = "131152";
        private const string TemperatureShiftCardNumber = "117670";
        private const string ShinyRingCardNumber = "109230";
        private const string BattlecruiserProxyCardId = "TRINKET_BATTLECRUISER";
        private const string BattlecruiserUpgradeProxyCardId = "TRINKET_BATTLECRUISER_UPGRADE";
        private const string DoubloonGrifterProxyCardId = "TRINKET_DOUBLOON_GRIFTER";
        private const string MawCasterProxyCardId = "TRINKET_MAW_CASTER";
        private const string MagicfinMurlocProxyCardId = "TRINKET_MAGICFIN_MURLOC";
        private const string CoinPouch3GoldProxyCardId = "TRINKET_COIN_POUCH_3";
        private const string JailerStickerSpellCardId = "TRINKET_JAILER_STICKER_SPELL";
        private const string DemonbloodGourdSpellCardId = "TRINKET_DEMONBLOOD_GOURD_SPELL";
        private const string ShiftingTideSpellCardId = "TRINKET_SHIFTING_TIDE_SPELL";
        private const string MishmashBuddyCardId = "TB_BaconShop_HERO_33_Buddy";
        private const string CuratorAmalgamProxyCardId = "TRINKET_CURATOR_AMALGAM";
        private const string TideRaiserCardId = "BG34_920";

        private static readonly QuestDifficultyProfile QuestDifficultyBalance =
            QuestDifficultyProfile.CreateDefault(PatchwerkHeroCardId);

        private static readonly string[] RandomSpellcraftSpellCardIds =
        {
            DeepSeaAnglerSpellCardId,
            DeepBlueSpellCardId,
            ReefRifferSpellCardId,
            SurfNSurfSpellCardId,
            VolcanicVisitorAttackSpellCardId,
            VolcanicVisitorHealthSpellCardId,
            FrostlingPriestessSpellCardId
        };

        private struct BuyCostEvaluation
        {
            public int Cost;
            public bool CostsHealth;
            public string HealthCostSource;
            public string FreeCostSource;
        }

        private readonly MinionCatalog catalog;
        private readonly SpellCatalog spellCatalog;
        private readonly HeroCatalog heroCatalog;
        private readonly TrinketCatalog trinketCatalog;
        private readonly QuestCatalog questCatalog;
        private readonly MinionEffectCatalog effectCatalog;
        private readonly ITestScenarioRepository scenarioRepository;
        private readonly List<Tribe> activeTribes;
        private readonly string selectedHeroCardId;
        private readonly AdvancedMechanicMode advancedMechanicMode;
        private readonly CardPoolVersionSelection cardPoolVersionSelection;
        private readonly CardPoolAvailability cardPoolAvailability;
        private CombatTestSnapshot combatTestSnapshot;
        private int automaticTavernSpellCastDepth;

        private MatchService(MinionCatalog catalog, SpellCatalog spellCatalog, HeroCatalog heroCatalog, TrinketCatalog trinketCatalog, QuestCatalog questCatalog, int seed, ITestScenarioRepository scenarioRepository, MatchSetupOptions setup)
        {
            this.catalog = catalog;
            this.spellCatalog = spellCatalog;
            this.heroCatalog = heroCatalog;
            this.trinketCatalog = trinketCatalog;
            this.questCatalog = questCatalog;
            this.scenarioRepository = scenarioRepository ?? new FileTestScenarioRepository();
            activeTribes = TribeAvailabilityRules.Normalize(setup?.ActiveTribes);
            selectedHeroCardId = setup?.SelectedHeroCardId;
            advancedMechanicMode = setup?.AdvancedMechanicMode ?? AdvancedMechanicMode.None;
            cardPoolVersionSelection = CreateCardPoolVersionSelection(setup);
            cardPoolAvailability = new CardPoolAvailability(cardPoolVersionSelection);
            effectCatalog = MinionEffectCatalog.CreateDefault();
            State = CreateMatch(seed);
        }

        public MatchState State { get; private set; }

        public HeroCatalog HeroCatalog => heroCatalog;

        public TrinketCatalog TrinketCatalog => trinketCatalog;

        public QuestCatalog QuestCatalog => questCatalog;

        public bool IsMinionAllowedByCardPool(MinionDefinition minion)
        {
            return cardPoolAvailability.AllowsMinion(minion);
        }

        public bool IsTavernSpellAllowedByCardPool(TavernSpellDefinition spell)
        {
            return cardPoolAvailability.AllowsTavernSpell(spell);
        }

        public CombatTestSnapshot LastCombatTestSnapshot => combatTestSnapshot;

        public bool HasCombatTestSnapshot => combatTestSnapshot?.BeforeCombat != null;

        public IReadOnlyList<string> TestScenarioNames => scenarioRepository.ListScenarioNames();

        public static MatchService CreateWithDefaultCatalog(int seed = 12345, ITestScenarioRepository scenarios = null, MatchSetupOptions setup = null)
        {
            return new MatchService(
                MinionCatalogLoader.LoadFromResources(),
                SpellCatalogLoader.LoadFromResources(),
                HeroCatalogLoader.LoadFromResources(),
                TrinketCatalogLoader.LoadFromResources(),
                QuestCatalogLoader.LoadFromResources(),
                seed,
                scenarios,
                setup);
        }

        private CardPoolVersionSelection CreateCardPoolVersionSelection(MatchSetupOptions setup)
        {
            if (setup == null || setup.IsDefaultCardPoolVersion)
            {
                return CardPoolVersionFactory.CreateDefaultSelection(catalog, spellCatalog);
            }

            return new CardPoolVersionSelection
            {
                VersionId = string.IsNullOrEmpty(setup.CardPoolVersionId) ? CardPoolVersionFactory.DefaultVersionId : setup.CardPoolVersionId,
                VersionName = string.IsNullOrEmpty(setup.CardPoolVersionName) ? "自定义版本" : setup.CardPoolVersionName,
                IsDefault = false,
                EnabledMinionCardIds = new HashSet<string>(
                    (setup.EnabledMinionCardIds ?? new List<string>()).Where(cardId => !string.IsNullOrEmpty(cardId)),
                    StringComparer.OrdinalIgnoreCase),
                EnabledTavernSpellCardNumbers = new HashSet<string>(
                    (setup.EnabledTavernSpellCardNumbers ?? new List<string>()).Where(cardNumber => !string.IsNullOrEmpty(cardNumber)),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        public MatchState Apply(GameCommand command)
        {
            switch (command.Type)
            {
                case GameCommandType.BuyMinion:
                    BuyMinion(command.Index);
                    break;
                case GameCommandType.PlayMinion:
                    PlayMinion(
                        command.Index,
                        command.TargetIndex,
                        command.TargetZone,
                        command.SecondaryTargetIndex,
                        command.SecondaryTargetZone,
                        command.TargetInstanceId,
                        command.SecondaryTargetInstanceId,
                        command.ChoiceId);
                    break;
                case GameCommandType.DiscardCardFromHand:
                    DiscardCardFromHand(command.Index);
                    break;
                case GameCommandType.UseHeroPower:
                    UseHeroPower(
                        command.TargetIndex,
                        command.TargetZone,
                        command.SecondaryTargetIndex,
                        command.SecondaryTargetZone,
                        command.TargetInstanceId,
                        command.SecondaryTargetInstanceId,
                        command.ChoiceId);
                    break;
                case GameCommandType.SellMinion:
                    SellMinion(command.InstanceId);
                    break;
                case GameCommandType.RerollShop:
                    RerollShop();
                    break;
                case GameCommandType.FreezeShop:
                    FreezeShop(command.Flag);
                    break;
                case GameCommandType.UpgradeTavern:
                    UpgradeTavern();
                    break;
                case GameCommandType.NextTurn:
                    NextTurn();
                    break;
                case GameCommandType.SimulateCombat:
                    SimulateCombat();
                    break;
                case GameCommandType.ChooseDiscover:
                    ChooseDiscover(command.Index);
                    break;
                case GameCommandType.ChooseMechanicOption:
                    ChooseMechanicOption(command.Index);
                    break;
                case GameCommandType.DebugOfferLesserTrinkets:
                    OfferTrinketChoice(TrinketSlotKind.Lesser, "debug");
                    break;
                case GameCommandType.DebugOfferGreaterTrinkets:
                    OfferTrinketChoice(TrinketSlotKind.Greater, "debug");
                    break;
                case GameCommandType.DebugOfferQuests:
                    OfferQuestChoice(3, "debug", "Main", null);
                    break;
                case GameCommandType.MoveMinion:
                    MoveMinionToHand(command.InstanceId);
                    break;
                case GameCommandType.MoveBoardMinion:
                    MoveBoardMinion(command.InstanceId, command.TargetIndex);
                    break;
                case GameCommandType.UpdateMinion:
                    UpdateMinion(command.InstanceId, command.MinionPatch);
                    break;
                case GameCommandType.AddCardToHand:
                    AddCardToHand(command.CardId, command.CardKind);
                    break;
                case GameCommandType.DebugCastCard:
                    CastDebugCard(command.CardId, command.CardKind, command.TargetIndex);
                    break;
                case GameCommandType.AddOpponentMinion:
                    AddOpponentMinion(command.InstanceId, command.Flag);
                    break;
                case GameCommandType.RemoveOpponentMinion:
                    RemoveOpponentMinion(command.InstanceId);
                    break;
                case GameCommandType.MoveOpponentMinion:
                    MoveOpponentMinion(command.InstanceId, command.TargetIndex);
                    break;
                case GameCommandType.UpdateOpponentMinion:
                    UpdateOpponentMinion(command.InstanceId, command.MinionPatch);
                    break;
                case GameCommandType.ClearOpponentBoard:
                    ClearOpponentBoard();
                    break;
                case GameCommandType.CopyPlayerBoardToOpponent:
                    CopyPlayerBoardToOpponent(false);
                    break;
                case GameCommandType.MirrorPlayerBoardToOpponent:
                    CopyPlayerBoardToOpponent(true);
                    break;
                case GameCommandType.SaveTestScenario:
                    SaveTestScenario(command.ScenarioName);
                    break;
                case GameCommandType.LoadTestScenario:
                    LoadTestScenario(command.ScenarioName);
                    break;
                case GameCommandType.RunCombatTest:
                    RunCombatTest(command.CombatTestOptions);
                    break;
                case GameCommandType.ResetCombatTestSnapshot:
                    ResetCombatTestSnapshot();
                    break;
                case GameCommandType.DebugAddGold:
                    State.Player.Tavern.Gold = Math.Max(0, State.Player.Tavern.Gold + command.Index);
                    State.Player.Tavern.MaxGold = Math.Max(State.Player.Tavern.MaxGold, State.Player.Tavern.Gold);
                    break;
            }

            RefreshPlayerBoardTribeDistribution();
            return State;
        }

        private MatchState CreateMatch(int seed)
        {
            var initialHero = ResolveInitialHero();
            var initialShopSize = HeroEffectEngine.ModifyShopSize(
                initialHero?.HeroPower?.CardId,
                TavernRules.GetShopSize(1));
            var initial = CreateShopFromPool(null, 1, initialShopSize, seed, "shop-1");
            var initialHealth = initialHero?.Health > 0 ? initialHero.Health : 30;
            var state = new MatchState
            {
                Mode = MatchMode.TavernPractice,
                Phase = MatchPhase.Tavern,
                Round = 1,
                Seed = seed,
                ActiveTribes = new List<Tribe>(activeTribes),
                CardPoolVersionId = cardPoolVersionSelection.VersionId,
                CardPoolVersionName = cardPoolVersionSelection.VersionName,
                IsDefaultCardPoolVersion = cardPoolVersionSelection.IsDefault,
                EnabledMinionCardIds = cardPoolVersionSelection.EnabledMinionCardIds.ToList(),
                EnabledTavernSpellCardNumbers = cardPoolVersionSelection.EnabledTavernSpellCardNumbers.ToList(),
                Player = new LocalPlayerState
                {
                    HeroId = initialHero?.HeroCardId,
                    HeroPowerCardId = initialHero?.HeroPower?.CardId,
                    Health = initialHealth,
                    MaxHealth = initialHealth,
                    Armor = initialHero?.Armor ?? 0,
                    Tavern = new TavernState
                    {
                        Tier = 1,
                        Gold = TavernRules.GetMaxGoldForRound(1),
                        MaxGold = TavernRules.GetMaxGoldForRound(1),
                        UpgradeCost = TavernRules.GetUpgradeCost(1),
                        Frozen = false,
                        Shop = initial.Shop,
                        Hand = new List<MinionInstance>(),
                        Pool = initial.Pool,
                        AdvancedMechanics = new AdvancedMechanicState(),
                        SearchPlan = new SearchPlanState(),
                        RecruitLog = new List<RecruitLogEntry>()
                    },
                    Board = new List<MinionInstance>()
                },
                Opponent = new LocalOpponentState
                {
                    Name = "训练对手",
                    Health = 30,
                    Armor = 0,
                    TavernTier = 1,
                    Editable = true,
                    Board = new List<MinionInstance>()
                },
                RecruitHints = new List<SearchHint>
                {
                    new SearchHint { Type = SearchHintType.CanHit, Message = "当前商店有可购买随从，可先补齐战场。", Severity = SearchHintSeverity.Info }
                },
                CombatLog = new List<CombatLogEntry>()
            };
            BoardTribeAnalyzer.Refresh(state.Player);
            var startResult = HeroEffectEngine.Dispatch(new HeroEffectContext
            {
                EventType = HeroEffectEventType.MatchStarted,
                State = state,
                Heroes = heroCatalog,
                Minions = catalog,
                Spells = spellCatalog,
                Rng = new SeededRng(seed + 37)
            });
            foreach (var message in startResult.Messages)
            {
                AddRecruitLog(state, RecruitLogType.Play, message, state.Player.Tavern.Gold, state.Player.Tavern.Gold);
            }

            State = state;
            LogHeroEffectImplementationStatus(state);
            MaybeOfferOpeningQuestMode(state);
            MaybeOfferStartingQuest(state);
            return state;
        }

        private HeroDefinition ResolveInitialHero()
        {
            if (heroCatalog == null || heroCatalog.AllHeroes.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(selectedHeroCardId))
            {
                var selected = heroCatalog.AllHeroes.FirstOrDefault(hero =>
                    string.Equals(hero.HeroCardId, selectedHeroCardId, StringComparison.OrdinalIgnoreCase));
                if (selected != null)
                {
                    return selected;
                }
            }

            return heroCatalog.AllHeroes.FirstOrDefault(hero => hero.Name == "Patchwerk")
                ?? heroCatalog.GetInitialSelectableHeroes().FirstOrDefault();
        }

        private void RefreshPlayerBoardTribeDistribution()
        {
            BoardTribeAnalyzer.Refresh(State.Player);
        }

        private void LogHeroEffectImplementationStatus(MatchState state)
        {
            if (state?.Player == null)
            {
                return;
            }

            var implementation = HeroEffectImplementationRegistry.FindByHeroCardId(state.Player.HeroId);
            if (implementation.Status == HeroEffectImplementationStatus.Implemented)
            {
                return;
            }

            var heroName = string.IsNullOrWhiteSpace(implementation.HeroName) ? state.Player.HeroId : implementation.HeroName;
            var heroPowerName = GetHeroPowerName(state.Player.HeroPowerCardId);
            var buddyName = GetBuddyName(implementation.BuddyCardId);
            var note = implementation.Note ?? "No status note.";
            AddRecruitLog(
                state,
                RecruitLogType.Play,
                $"英雄效果状态: {heroName} / {heroPowerName}{FormatBuddySuffix(buddyName)} - {implementation.Status} ({implementation.Phase}) {note}",
                state.Player.Tavern.Gold,
                state.Player.Tavern.Gold);
        }

        private string GetHeroPowerName(string heroPowerCardId)
        {
            if (string.IsNullOrWhiteSpace(heroPowerCardId) || heroCatalog == null)
            {
                return heroPowerCardId ?? "unknown hero power";
            }

            try
            {
                return heroCatalog.GetHeroPowerByCardId(heroPowerCardId).Name;
            }
            catch (InvalidOperationException)
            {
                return heroPowerCardId;
            }
        }

        private string GetBuddyName(string buddyCardId)
        {
            if (string.IsNullOrWhiteSpace(buddyCardId) || heroCatalog == null)
            {
                return string.Empty;
            }

            try
            {
                return heroCatalog.GetBuddyByCardId(buddyCardId).Name;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        private static string FormatBuddySuffix(string buddyName)
        {
            return string.IsNullOrWhiteSpace(buddyName) ? string.Empty : " / " + buddyName;
        }

        private IReadOnlyCollection<Tribe> CurrentActiveTribes()
        {
            if (State == null)
            {
                return activeTribes;
            }

            State.ActiveTribes = TribeAvailabilityRules.Normalize(State.ActiveTribes == null || State.ActiveTribes.Count == 0
                ? activeTribes
                : State.ActiveTribes);
            return State.ActiveTribes;
        }

        private IEnumerable<MinionDefinition> AvailableMinions()
        {
            var active = CurrentActiveTribes();
            return catalog.All.Where(minion =>
                cardPoolAvailability.AllowsMinion(minion) &&
                TribeAvailabilityRules.IsMinionAvailable(minion, active));
        }

        private IEnumerable<TavernSpellDefinition> AvailableTavernSpells()
        {
            var active = CurrentActiveTribes();
            return spellCatalog.All.Where(spell =>
                cardPoolAvailability.AllowsTavernSpell(spell) &&
                TribeAvailabilityRules.IsTavernSpellAvailable(spell, active));
        }

        private void OfferTrinketChoice(TrinketSlotKind slotKind, string source)
        {
            OfferTrinketChoice(slotKind, source, slotKind);
        }

        private void OfferTrinketChoice(TrinketSlotKind poolSlotKind, string source, TrinketSlotKind targetSlotKind)
        {
            if (trinketCatalog == null)
            {
                throw new InvalidOperationException("Trinket catalog is not loaded.");
            }

            var tavern = State.Player.Tavern;
            var advanced = EnsureAdvancedMechanicState(tavern);
            if (advanced.PendingChoice != null)
            {
                throw new InvalidOperationException("An advanced-mechanic choice is already pending.");
            }

            var trinkets = EnsureTrinketState(tavern);
            if (targetSlotKind == TrinketSlotKind.Lesser && !string.IsNullOrEmpty(trinkets.LesserTrinketId))
            {
                throw new InvalidOperationException("A Lesser Trinket is already equipped.");
            }

            if (targetSlotKind == TrinketSlotKind.Greater && !string.IsNullOrEmpty(trinkets.GreaterTrinketId))
            {
                throw new InvalidOperationException("A Greater Trinket is already equipped.");
            }

            var equippedIds = CurrentEquippedTrinketIds(trinkets);
            var pool = trinketCatalog.GetOfferableBySlot(poolSlotKind)
                .Where(definition => !equippedIds.Contains(definition.CardId))
                .ToList();
            if (pool.Count == 0)
            {
                throw new InvalidOperationException("No offerable Trinkets exist for slot: " + poolSlotKind);
            }

            var options = PickTrinketOptions(
                pool,
                4,
                State.Seed + State.Round * 4099 + (poolSlotKind == TrinketSlotKind.Lesser ? 17 : 53) + (targetSlotKind == poolSlotKind ? 0 : 211));
            var request = new MechanicChoiceRequest
            {
                RequestId = "trinket-" + targetSlotKind.ToString().ToLowerInvariant() + "-" + State.Round + "-" + tavern.RecruitLog.Count,
                Kind = AdvancedMechanicKind.Trinket,
                Source = source,
                Slot = targetSlotKind.ToString(),
                Round = State.Round,
                RemainingPicks = 1
            };

            foreach (var option in options)
            {
                request.Options.Add(CreateTrinketChoiceOption(option, targetSlotKind));
            }

            advanced.PendingChoice = request;
            var poolLabel = poolSlotKind == targetSlotKind ? string.Empty : " from " + poolSlotKind + " pool";
            AddRecruitLog(
                RecruitLogType.Discover,
                targetSlotKind + " Trinket choices offered" + poolLabel + " (" + request.Options.Count + ").",
                tavern.Gold,
                tavern.Gold);
        }

        private static HashSet<string> CurrentEquippedTrinketIds(PlayerTrinketState trinkets)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (trinkets == null)
            {
                return ids;
            }

            if (!string.IsNullOrWhiteSpace(trinkets.LesserTrinketId))
            {
                ids.Add(trinkets.LesserTrinketId);
            }

            if (!string.IsNullOrWhiteSpace(trinkets.GreaterTrinketId))
            {
                ids.Add(trinkets.GreaterTrinketId);
            }

            if (trinkets.Equipped != null)
            {
                foreach (var equipped in trinkets.Equipped)
                {
                    if (!string.IsNullOrWhiteSpace(equipped?.TrinketId))
                    {
                        ids.Add(equipped.TrinketId);
                    }
                }
            }

            return ids;
        }

        private void ChooseMechanicOption(int optionIndex)
        {
            var tavern = State.Player.Tavern;
            var advanced = EnsureAdvancedMechanicState(tavern);
            var request = advanced.PendingChoice;
            if (request == null)
            {
                throw new InvalidOperationException("No advanced-mechanic choice is pending.");
            }

            if (optionIndex < 0 || optionIndex >= request.Options.Count)
            {
                throw new InvalidOperationException("Advanced-mechanic option does not exist.");
            }

            var option = request.Options[optionIndex];
            if (request.Kind == AdvancedMechanicKind.Trinket)
            {
                EquipTrinketFromOption(option);
            }
            else if (request.Kind == AdvancedMechanicKind.Quest)
            {
                ActivateQuestFromOption(request, option);
            }
            else
            {
                throw new InvalidOperationException("Unsupported advanced-mechanic choice kind: " + request.Kind);
            }

            request.RemainingPicks = Math.Max(0, request.RemainingPicks - 1);
            if (request.RemainingPicks <= 0)
            {
                advanced.PendingChoice = null;
            }
        }

        private List<TrinketDefinition> PickTrinketOptions(List<TrinketDefinition> pool, int count, int seed)
        {
            var remaining = new List<TrinketDefinition>(pool);
            var picked = new List<TrinketDefinition>();
            var rng = new SeededRng(seed);
            while (picked.Count < count && remaining.Count > 0)
            {
                var index = rng.NextInt(remaining.Count);
                picked.Add(remaining[index]);
                remaining.RemoveAt(index);
            }

            return picked;
        }

        private void MaybeOfferStartingQuest(MatchState state)
        {
            if (state?.Player == null ||
                !string.Equals(state.Player.HeroId, SireDenathriusHeroCardId, StringComparison.OrdinalIgnoreCase) ||
                questCatalog == null ||
                questCatalog.ImplementedQuests.Count == 0)
            {
                return;
            }

            var advanced = EnsureAdvancedMechanicState(state.Player.Tavern);
            if (advanced.PendingChoice != null)
            {
                AddRecruitLog(state, RecruitLogType.Discover, "Sire Denathrius could not offer Quests because another advanced choice is pending.", state.Player.Tavern.Gold, state.Player.Tavern.Gold);
                return;
            }

            advanced.PendingChoice = CreateQuestChoiceRequest(
                PickQuestOptions(2, state.Seed + 240100),
                "sire-denathrius",
                "Main",
                null,
                state.Round,
                state.Player.Tavern.RecruitLog.Count);
            AddRecruitLog(state, RecruitLogType.Discover, "Sire Denathrius offered 2 Quest choices.", state.Player.Tavern.Gold, state.Player.Tavern.Gold);
        }

        private void MaybeOfferOpeningQuestMode(MatchState state)
        {
            if (!ModeIncludesQuests() ||
                state?.Player == null ||
                questCatalog == null ||
                questCatalog.ImplementedQuests.Count == 0)
            {
                return;
            }

            var advanced = EnsureAdvancedMechanicState(state.Player.Tavern);
            if (advanced.PendingChoice != null)
            {
                AddRecruitLog(state, RecruitLogType.Discover, "Quest mode could not offer Quests because another advanced choice is pending.", state.Player.Tavern.Gold, state.Player.Tavern.Gold);
                return;
            }

            advanced.PendingChoice = CreateQuestChoiceRequest(
                PickQuestOptions(3, state.Seed + 330006),
                "quest-mode-opening",
                "Main",
                null,
                state.Round,
                state.Player.Tavern.RecruitLog.Count);
            AddRecruitLog(state, RecruitLogType.Discover, "Quest mode offered 3 Quest choices.", state.Player.Tavern.Gold, state.Player.Tavern.Gold);
        }

        private bool ModeIncludesQuests()
        {
            return advancedMechanicMode == AdvancedMechanicMode.Quests ||
                   advancedMechanicMode == AdvancedMechanicMode.Mixed;
        }

        private void OfferQuestChoice(int count, string source, string slot, string rewardOverrideId)
        {
            if (questCatalog == null)
            {
                throw new InvalidOperationException("Quest catalog is not loaded.");
            }

            var tavern = State.Player.Tavern;
            var advanced = EnsureAdvancedMechanicState(tavern);
            if (advanced.PendingChoice != null)
            {
                throw new InvalidOperationException("An advanced-mechanic choice is already pending.");
            }

            var quests = PickQuestOptions(count, State.Seed + State.Round * 6151 + tavern.RecruitLog.Count);
            advanced.PendingChoice = CreateQuestChoiceRequest(quests, source, slot, rewardOverrideId, State.Round, tavern.RecruitLog.Count);
            AddRecruitLog(RecruitLogType.Discover, "Quest choices offered (" + advanced.PendingChoice.Options.Count + ").", tavern.Gold, tavern.Gold);
        }

        private MechanicChoiceRequest CreateQuestChoiceRequest(
            List<QuestDefinition> quests,
            string source,
            string slot,
            string rewardOverrideId,
            int round,
            int logCount)
        {
            var request = new MechanicChoiceRequest
            {
                RequestId = "quest-" + slot.ToLowerInvariant() + "-" + round + "-" + logCount,
                Kind = AdvancedMechanicKind.Quest,
                Source = source,
                Slot = slot,
                Round = round,
                RemainingPicks = 1
            };

            for (var index = 0; index < quests.Count; index += 1)
            {
                var reward = ResolveQuestReward(quests[index], rewardOverrideId, index);
                request.Options.Add(CreateQuestChoiceOption(quests[index], reward, slot));
            }

            return request;
        }

        private List<QuestDefinition> PickQuestOptions(int count, int seed)
        {
            var pool = questCatalog == null ? new List<QuestDefinition>() : questCatalog.ImplementedQuests;
            if (pool.Count == 0)
            {
                throw new InvalidOperationException("No implemented Quests exist.");
            }

            var remaining = new List<QuestDefinition>(pool);
            var picked = new List<QuestDefinition>();
            var rng = new SeededRng(seed);
            while (picked.Count < count && remaining.Count > 0)
            {
                var index = rng.NextInt(remaining.Count);
                picked.Add(remaining[index]);
                remaining.RemoveAt(index);
            }

            return picked;
        }

        private QuestRewardDefinition ResolveQuestReward(QuestDefinition quest, string rewardOverrideId, int optionIndex)
        {
            if (!string.IsNullOrEmpty(rewardOverrideId) && questCatalog.TryGetRewardById(rewardOverrideId, out var overrideReward))
            {
                return overrideReward;
            }

            if (!string.IsNullOrEmpty(quest?.DefaultRewardId) &&
                questCatalog.TryGetRewardById(quest.DefaultRewardId, out var defaultReward) &&
                defaultReward.OfferPoolStatus == QuestOfferPoolStatus.Offerable)
            {
                return defaultReward;
            }

            var rewards = questCatalog.OfferableRewards;
            if (rewards.Count == 0)
            {
                throw new InvalidOperationException("No offerable Quest Rewards exist.");
            }

            return rewards[Math.Abs(optionIndex) % rewards.Count];
        }

        private MechanicChoiceOption CreateQuestChoiceOption(QuestDefinition quest, QuestRewardDefinition reward, string slot)
        {
            var difficulty = ResolveQuestDifficulty(reward);
            var requiredAmount = ResolveQuestRequiredAmount(quest, difficulty.Tier);
            return new MechanicChoiceOption
            {
                OptionId = quest.CardId + ":" + reward.Id,
                Kind = AdvancedMechanicKind.Quest,
                SourceId = quest.CardId,
                DisplayName = quest.Name,
                Text = FormatQuestText(quest.Text, requiredAmount),
                ImagePath = quest.ImagePath,
                RewardId = reward.Id,
                RewardName = reward.Name,
                RewardText = reward.Text,
                RewardImagePath = reward.ImagePath,
                RequiredAmount = requiredAmount,
                DifficultyTier = difficulty.Tier,
                RewardPowerLevel = reward.PowerLevel.ToString(),
                Slot = slot,
                ImplementationStatus = quest.ImplementationStatus.ToString(),
                Tags = quest.Tags == null ? new List<string>() : new List<string>(quest.Tags)
            };
        }

        private void ActivateQuestFromOption(MechanicChoiceRequest request, MechanicChoiceOption option)
        {
            if (option == null || string.IsNullOrEmpty(option.SourceId))
            {
                throw new InvalidOperationException("Quest option is invalid.");
            }

            if (!questCatalog.TryGetQuestByCardId(option.SourceId, out var quest))
            {
                throw new InvalidOperationException("Quest definition does not exist: " + option.SourceId);
            }

            if (!questCatalog.TryGetRewardById(option.RewardId, out var reward))
            {
                throw new InvalidOperationException("Quest reward definition does not exist: " + option.RewardId);
            }

            if (string.Equals(request.Source, "quest-ethereal-evidence", StringComparison.OrdinalIgnoreCase))
            {
                ActivateImmediateQuestReward(request, quest, reward, option);
                return;
            }

            var tavern = State.Player.Tavern;
            var advanced = EnsureAdvancedMechanicState(tavern);
            var quests = EnsureQuestState(tavern);
            var slot = string.IsNullOrWhiteSpace(request.Slot) ? "Main" : request.Slot;
            if (string.Equals(slot, "Bonus", StringComparison.OrdinalIgnoreCase))
            {
                if (quests.BonusQuest != null && !quests.BonusQuest.Completed)
                {
                    throw new InvalidOperationException("A Bonus Quest is already active.");
                }

                quests.BonusQuest = CreateActiveQuest(quest, reward, request.Source, option.RequiredAmount, option.DifficultyTier);
            }
            else
            {
                if (quests.MainQuest != null && !quests.MainQuest.Completed)
                {
                    throw new InvalidOperationException("A Main Quest is already active.");
                }

                quests.MainQuest = CreateActiveQuest(quest, reward, request.Source, option.RequiredAmount, option.DifficultyTier);
            }

            advanced.Equipped.Add(new EquippedAdvancedMechanic
            {
                Kind = AdvancedMechanicKind.Quest,
                SourceId = quest.CardId,
                DisplayName = quest.Name,
                Slot = slot,
                EquippedRound = State.Round,
                CostPaid = 0,
                ImplementationStatus = quest.ImplementationStatus.ToString()
            });
            AddRecruitLog(RecruitLogType.Discover, "Quest chosen: " + quest.Name + " -> " + reward.Name + ".", tavern.Gold, tavern.Gold);
        }

        private void ActivateImmediateQuestReward(MechanicChoiceRequest request, QuestDefinition quest, QuestRewardDefinition reward, MechanicChoiceOption option)
        {
            var tavern = State.Player.Tavern;
            var advanced = EnsureAdvancedMechanicState(tavern);
            var quests = EnsureQuestState(tavern);
            var active = CreateActiveQuest(quest, reward, request.Source, 1, option.DifficultyTier);
            active.Progress = active.RequiredAmount;
            active.Completed = true;
            active.RewardActive = true;
            active.Source = request.Source;
            quests.BonusQuest = active;
            if (quests.Completed == null)
            {
                quests.Completed = new List<ActiveQuestState>();
            }

            quests.Completed.Add(active);
            advanced.Equipped.Add(new EquippedAdvancedMechanic
            {
                Kind = AdvancedMechanicKind.Quest,
                SourceId = quest.CardId,
                DisplayName = reward.Name,
                Slot = "Bonus",
                EquippedRound = State.Round,
                CostPaid = 0,
                ImplementationStatus = reward.ImplementationStatus.ToString()
            });
            DispatchQuestReward(active, QuestRewardTrigger.OnComplete, null, null);
            AddRecruitLog(RecruitLogType.Discover, "Ethereal Evidence reward chosen: " + reward.Name + ".", tavern.Gold, tavern.Gold);
        }

        private ActiveQuestState CreateActiveQuest(QuestDefinition quest, QuestRewardDefinition reward, string source, int requiredAmount, int difficultyTier)
        {
            var difficulty = ResolveQuestDifficulty(reward);
            var resolvedRequiredAmount = requiredAmount > 0
                ? requiredAmount
                : ResolveQuestRequiredAmount(quest, difficulty.Tier);
            var resolvedDifficultyTier = difficultyTier > 0 ? difficultyTier : difficulty.Tier;
            return new ActiveQuestState
            {
                QuestId = quest.Id,
                QuestCardId = quest.CardId,
                QuestName = quest.Name,
                QuestText = FormatQuestText(quest.Text, resolvedRequiredAmount),
                QuestImagePath = quest.ImagePath,
                RewardId = reward.Id,
                RewardCardId = reward.CardId,
                RewardName = reward.Name,
                RewardText = reward.Text,
                RewardImagePath = reward.ImagePath,
                Source = source,
                Progress = 0,
                BaseRequiredAmount = Math.Max(1, quest.Objective.RequiredAmount),
                RequiredAmount = resolvedRequiredAmount,
                DifficultyTier = resolvedDifficultyTier,
                DifficultyModifier = difficulty.Modifier,
                RewardPowerLevel = reward.PowerLevel,
                Completed = false,
                RewardActive = false,
                ImplementationStatus = quest.ImplementationStatus
            };
        }

        private (int Tier, int Modifier) ResolveQuestDifficulty(QuestRewardDefinition reward)
        {
            var modifier = ResolveQuestDifficultyModifier();
            var tier = QuestDifficultyBalance.ResolveTier(
                reward?.PowerLevel ?? QuestRewardPowerLevel.Medium,
                State.Player.Armor,
                State.Player.MaxHealth,
                State.Player.HeroId);
            return (tier, modifier);
        }

        private int ResolveQuestDifficultyModifier()
        {
            return QuestDifficultyBalance.ResolveModifier(
                State.Player.Armor,
                State.Player.MaxHealth,
                State.Player.HeroId);
        }

        private static int ResolveQuestRequiredAmount(QuestDefinition quest, int difficultyTier)
        {
            var baseAmount = Math.Max(1, quest?.Objective?.RequiredAmount ?? 1);
            return QuestDifficultyBalance.ResolveRequiredAmount(baseAmount, difficultyTier);
        }

        private static string FormatQuestText(string text, int requiredAmount)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var match = Regex.Match(text, "\\d+");
            return match.Success
                ? text.Substring(0, match.Index) + requiredAmount + text.Substring(match.Index + match.Length)
                : text;
        }

        private void RecordQuestProgress(QuestObjectiveKind kind, int amount)
        {
            if (amount <= 0 || questCatalog == null)
            {
                return;
            }

            var quests = EnsureQuestState(State.Player.Tavern);
            RecordQuestProgress(quests.MainQuest, kind, amount);
            RecordQuestProgress(quests.BonusQuest, kind, amount);
        }

        private void RecordQuestProgress(ActiveQuestState active, QuestObjectiveKind kind, int amount)
        {
            if (active == null || active.Completed || !questCatalog.TryGetQuestById(active.QuestId, out var definition))
            {
                return;
            }

            if (definition.Objective.Kind != kind)
            {
                return;
            }

            var before = active.Progress;
            active.Progress = Math.Min(active.RequiredAmount, active.Progress + amount);
            if (active.Progress != before)
            {
                AddRecruitLog(RecruitLogType.Play, "Quest progress: " + active.QuestName + " " + active.Progress + "/" + active.RequiredAmount + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }

            if (active.Progress >= active.RequiredAmount)
            {
                CompleteQuest(active);
            }
        }

        private void CompleteQuest(ActiveQuestState active)
        {
            if (active == null || active.Completed)
            {
                return;
            }

            active.Completed = true;
            active.RewardActive = true;
            var quests = EnsureQuestState(State.Player.Tavern);
            if (quests.Completed == null)
            {
                quests.Completed = new List<ActiveQuestState>();
            }

            quests.Completed.Add(active);
            AddRecruitLog(RecruitLogType.Play, "Quest complete: " + active.QuestName + ". Reward active: " + active.RewardName + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            DispatchQuestReward(active, QuestRewardTrigger.OnComplete, null, null);
            if (string.Equals(active.RewardId, AlterEgoRewardId, StringComparison.OrdinalIgnoreCase) &&
                questCatalog.TryGetRewardById(active.RewardId, out var alterEgoReward))
            {
                EnsureQuestState(State.Player.Tavern).RewardCounters[QuestRewardCounterKey(AlterEgoRewardId, "parity")] = 0;
                ApplyAlterEgoBuff(alterEgoReward, false);
            }
        }

        private void DispatchQuestRewardTurnStarted()
        {
            ClearQuestTemporaryStealth();
            ClearQuestTemporaryGoldenHammer();
            TransformQuestZerusCards();
            ApplyNorgannonAutoUpgrade();
            DispatchQuestRewards(QuestRewardTrigger.TurnStarted, null, null);
            DispatchContinuousQuestRewardTurnStarted();
        }

        private void DispatchQuestRewardTurnEnded()
        {
            DispatchQuestRewards(QuestRewardTrigger.TurnEnded, null, null);
        }

        private void DispatchQuestRewardCardBought(MinionInstance bought)
        {
            DispatchQuestRewards(QuestRewardTrigger.CardBought, bought, null);
        }

        private void DispatchQuestRewardMinionSold(MinionInstance sold)
        {
            DispatchQuestRewards(QuestRewardTrigger.MinionSold, sold, null);
        }

        private void DispatchQuestRewardMinionPlayed(MinionInstance played)
        {
            DispatchQuestRewards(QuestRewardTrigger.MinionPlayed, played, null);
            if (played != null && HasActiveQuestReward(TheSmokingGunRewardId))
            {
                BuffMinion(played, 4, 0, "The Smoking Gun");
            }
        }

        private void DispatchQuestRewardShopRefreshed(List<MinionInstance> shop)
        {
            DispatchQuestRewards(QuestRewardTrigger.ShopRefreshed, null, shop);
            if (HasActiveQuestReward(AlterEgoRewardId) &&
                questCatalog.TryGetRewardById(AlterEgoRewardId, out var alterEgoReward))
            {
                ApplyAlterEgoBuff(alterEgoReward, false);
            }
        }

        private void DispatchQuestRewardAfterCombat(MinionInstance lastDeadFriendly)
        {
            DispatchQuestRewards(QuestRewardTrigger.AfterCombat, lastDeadFriendly, null);
        }

        private void DispatchQuestRewardDiscoverChosen(MinionInstance picked)
        {
            DispatchQuestRewards(QuestRewardTrigger.DiscoverChosen, picked, null);
        }

        private void DispatchContinuousQuestRewardTurnStarted()
        {
            foreach (var active in ActiveQuestRewards())
            {
                if (!questCatalog.TryGetRewardById(active.RewardId, out var reward))
                {
                    continue;
                }

                if (string.Equals(reward.Id, EndlessBloodMoonRewardId, StringComparison.OrdinalIgnoreCase))
                {
                    AddBloodGemsToHand(2, reward.Name);
                }
            }
        }

        private void DispatchQuestRewards(QuestRewardTrigger trigger, MinionInstance bought, List<MinionInstance> shop)
        {
            var quests = EnsureQuestState(State.Player.Tavern);
            DispatchQuestReward(quests.MainQuest, trigger, bought, shop);
            DispatchQuestReward(quests.BonusQuest, trigger, bought, shop);
        }

        private void DispatchQuestReward(ActiveQuestState active, QuestRewardTrigger trigger, MinionInstance bought, List<MinionInstance> shop)
        {
            if (active == null || !active.RewardActive || !questCatalog.TryGetRewardById(active.RewardId, out var reward) || reward.Trigger != trigger)
            {
                return;
            }

            ApplyQuestReward(active, reward, bought, shop);
        }

        private List<ActiveQuestState> ActiveQuestRewards()
        {
            var quests = EnsureQuestState(State.Player.Tavern);
            var active = new List<ActiveQuestState>();
            if (quests.MainQuest?.RewardActive == true)
            {
                active.Add(quests.MainQuest);
            }

            if (quests.BonusQuest?.RewardActive == true)
            {
                active.Add(quests.BonusQuest);
            }

            return active;
        }

        private bool HasActiveQuestReward(string rewardId)
        {
            return ActiveQuestRewards().Any(active =>
                string.Equals(active.RewardId, rewardId, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyQuestReward(ActiveQuestState active, QuestRewardDefinition reward, MinionInstance bought, List<MinionInstance> shop)
        {
            var tavern = State.Player.Tavern;
            switch (reward.EffectKind)
            {
                case QuestRewardEffectKind.GrantGold:
                    GrantQuestGold(reward.GoldAmount, reward.Name);
                    break;
                case QuestRewardEffectKind.GrantGoldAndMaxGold:
                    tavern.MaxGold = Math.Min(StatMath.MaxStat, tavern.MaxGold + reward.MaxGoldAmount);
                    GrantQuestGold(reward.GoldAmount, reward.Name);
                    break;
                case QuestRewardEffectKind.GainGoldEachTurn:
                    var quests = EnsureQuestState(tavern);
                    var gold = Math.Max(1, quests.HiddenTreasureVaultGold);
                    GrantQuestGold(gold, reward.Name);
                    if (reward.Improves)
                    {
                        quests.HiddenTreasureVaultGold = Math.Min(StatMath.MaxStat, gold + Math.Max(1, reward.GoldAmount));
                    }
                    break;
                case QuestRewardEffectKind.BuffBoughtMinionAndImprove:
                    if (bought != null && bought.CardKind == CardKind.Minion)
                    {
                        var questState = EnsureQuestState(tavern);
                        BuffMinion(bought, questState.CookedBookAttack, questState.CookedBookHealth, reward.Name);
                        AddRecruitLog(RecruitLogType.Play, reward.Name + ": buffed " + bought.Name + " +" + questState.CookedBookAttack + "/+" + questState.CookedBookHealth + ".", tavern.Gold, tavern.Gold);
                        if (reward.Improves)
                        {
                            questState.CookedBookAttack = Math.Min(StatMath.MaxStat, questState.CookedBookAttack + 1);
                            questState.CookedBookHealth = Math.Min(StatMath.MaxStat, questState.CookedBookHealth + 1);
                        }
                    }
                    break;
                case QuestRewardEffectKind.BuffRandomShopMinion:
                    var shopTargets = (shop ?? tavern.Shop).Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
                    if (shopTargets.Count > 0)
                    {
                        var picked = new SeededRng(State.Seed + State.Round * 811 + tavern.RecruitLog.Count).Pick(shopTargets);
                        BuffMinion(picked, reward.AttackBonus, reward.HealthBonus, reward.Name);
                        if (string.Equals(reward.Id, "BG24_Reward_128", StringComparison.OrdinalIgnoreCase) &&
                            !picked.Keywords.Contains(Keyword.DivineShield))
                        {
                            picked.Keywords.Add(Keyword.DivineShield);
                        }

                        AddRecruitLog(RecruitLogType.Play, reward.Name + ": buffed Tavern minion " + picked.Name + " +" + reward.AttackBonus + "/+" + reward.HealthBonus + ".", tavern.Gold, tavern.Gold);
                    }
                    break;
                case QuestRewardEffectKind.BuffTierThreeOrLowerMinions:
                    var rng = new SeededRng(State.Seed + State.Round * 947 + tavern.RecruitLog.Count);
                    var candidates = State.Player.Board
                        .Where(minion => minion != null && minion.TavernTier <= 3)
                        .OrderBy(_ => rng.NextInt(StatMath.MaxStat))
                        .Take(Math.Max(1, reward.TargetCount))
                        .ToList();
                    foreach (var minion in candidates)
                    {
                        BuffMinion(minion, reward.AttackBonus, reward.HealthBonus, reward.Name);
                    }

                    if (candidates.Count > 0)
                    {
                        AddRecruitLog(RecruitLogType.Play, reward.Name + ": buffed " + candidates.Count + " friendly minion(s).", tavern.Gold, tavern.Gold);
                    }
                    break;
                case QuestRewardEffectKind.RightmostStealthAndHealth:
                    ApplyTheotarsParasol(reward);
                    break;
                case QuestRewardEffectKind.FriendlyMinionsAttackAura:
                    tavern.QuestFriendlyAttackAura = Math.Max(tavern.QuestFriendlyAttackAura, reward.AttackBonus);
                    BuffAllMinions(State.Player.Board, reward.AttackBonus, 0, reward.Name);
                    break;
                case QuestRewardEffectKind.BuffRandomHandMinion:
                    BuffRandomHandMinion(reward.AttackBonus, reward.HealthBonus, reward.Name);
                    break;
                case QuestRewardEffectKind.StartCombatBoardBuff:
                    tavern.NextCombatBoardAttack += reward.AttackBonus;
                    tavern.NextCombatBoardHealth += reward.HealthBonus;
                    break;
                case QuestRewardEffectKind.AlternatingShopTierBuff:
                    ApplyAlterEgoBuff(reward, true);
                    break;
                case QuestRewardEffectKind.EndTurnMenagerieBoardBuff:
                    var typeCount = CountFriendlyMinionTypes(State.Player.Board);
                    if (typeCount > 0)
                    {
                        BuffAllMinions(State.Player.Board, typeCount, typeCount, reward.Name);
                    }
                    break;
                case QuestRewardEffectKind.VolatileVenomAura:
                    tavern.QuestVolatileVenomActive = true;
                    break;
                case QuestRewardEffectKind.RightmostMissingHealthAttack:
                    ApplyBloodGoblet(reward);
                    break;
                case QuestRewardEffectKind.BuffOtherSameTierMinions:
                    ApplySinfallMedallion(bought, reward);
                    break;
                case QuestRewardEffectKind.AddEnhancedPartToHand:
                    AddEnhancedPartToHand(reward.Name);
                    break;
                case QuestRewardEffectKind.AvengeDamageHighestHealthEnemy:
                    tavern.QuestBoomSquadActive = true;
                    break;
                case QuestRewardEffectKind.BuffNonTauntByTauntCount:
                    ApplySturdyShard(reward);
                    break;
                case QuestRewardEffectKind.PlayMissingTypeMenagerieBuff:
                    ApplyMapOfTheUnknown(bought, reward);
                    break;
                case QuestRewardEffectKind.ImproveBloodGemsAndAddGems:
                    tavern.BloodGemBonusAttack += Math.Max(0, reward.AttackBonus);
                    tavern.BloodGemBonusHealth += Math.Max(0, reward.HealthBonus);
                    if (reward.Trigger == QuestRewardTrigger.TurnStarted)
                    {
                        AddBloodGemsToHand(Math.Max(1, reward.TargetCount), reward.Name);
                    }
                    break;
                case QuestRewardEffectKind.CombatSummonBuffAndImprove:
                    tavern.QuestTumblingAttack = GetQuestRewardCounter(reward.Id, "attack", reward.AttackBonus);
                    tavern.QuestTumblingHealth = GetQuestRewardCounter(reward.Id, "health", reward.HealthBonus);
                    break;
                case QuestRewardEffectKind.LeftmostDivineShieldImmediateAttack:
                    break;
                case QuestRewardEffectKind.AvengeGainFreeRefresh:
                    tavern.QuestGrimFreshenerActive = true;
                    break;
                case QuestRewardEffectKind.AddRushingWindsSpellcraft:
                    AddGeneratedSpellsToHand(RushingWindsSpellCardId, 1, reward.Name);
                    MarkTemporarySpellcraftCard(RushingWindsSpellCardId);
                    break;
                case QuestRewardEffectKind.SellMinionStatsToShop:
                    ApplyAnimaBribe(bought, reward);
                    break;
                case QuestRewardEffectKind.BuyMinionStatsToFriendly:
                    ApplyInvigoratingConch(bought, reward);
                    break;
                case QuestRewardEffectKind.FirstBuyEachTurnCopy:
                    ApplyDoubleHeadedReward(active, bought, reward);
                    break;
                case QuestRewardEffectKind.GainRandomTavernSpells:
                    AddRandomTavernSpellToHand(State.Player.Tavern.Tier, Math.Max(1, reward.TargetCount), reward.Name);
                    break;
                case QuestRewardEffectKind.TavernSpellCostDiscount:
                case QuestRewardEffectKind.SetTavernMinionCost:
                    break;
                case QuestRewardEffectKind.CopyExpensiveBoughtTavernSpell:
                    ApplySplittingScroll(bought, reward);
                    break;
                case QuestRewardEffectKind.GoldenHighestTierShopMinion:
                    ApplyGoldenForge(reward);
                    break;
                case QuestRewardEffectKind.TriggerBattlecriesAtEndOfTurn:
                    ApplySnickerSnacks(reward);
                    break;
                case QuestRewardEffectKind.RefreshCountShopBuffAura:
                    ApplyTealTigerSapphire(shop ?? State.Player.Tavern.Shop, reward);
                    break;
                case QuestRewardEffectKind.EdgeMinionsConsumeShop:
                    ApplyDevilsInTheDetails(reward);
                    break;
                case QuestRewardEffectKind.GoldenHighestTierShopAfterRefreshes:
                    ApplyGiftOfTheGoldenKobold(reward);
                    break;
                case QuestRewardEffectKind.GainLastDeadFriendlyPlainCopyAfterCombat:
                    ApplyVictimsSpecter(bought, reward);
                    break;
                case QuestRewardEffectKind.MakeEdgeMinionsGoldenForCombat:
                case QuestRewardEffectKind.SummonHighestHealthCopyAtCombatStart:
                case QuestRewardEffectKind.PermanentBuffDeathrattleMinionAfterDeath:
                case QuestRewardEffectKind.AvengeGainRandomTavernSpell:
                case QuestRewardEffectKind.AvengeSummonAmalgam:
                case QuestRewardEffectKind.ExtraDeathrattleTriggers:
                    break;
                case QuestRewardEffectKind.ExtraDiscoverCopy:
                    CopyDiscoveredCardToHand(bought, reward);
                    break;
                case QuestRewardEffectKind.GainGoldenBuddy:
                    GainGoldenBuddy(reward);
                    break;
                case QuestRewardEffectKind.DiscoverOpponentWarbandMinionAfterCombat:
                    StartDoppelgangersLocketDiscover(reward);
                    break;
                case QuestRewardEffectKind.DiscoverBuddyEachTurn:
                    StartBuddyDiscover(reward);
                    break;
                case QuestRewardEffectKind.DelayedLesserTrinketChoice:
                    ScheduleQuestTrinketChoice(reward, TrinketSlotKind.Lesser);
                    break;
                case QuestRewardEffectKind.DelayedGreaterTrinketChoice:
                    ScheduleQuestTrinketChoice(reward, TrinketSlotKind.Greater);
                    break;
                case QuestRewardEffectKind.DiscoverSecondHeroPower:
                    StartSecondHeroPowerDiscover(reward);
                    break;
                case QuestRewardEffectKind.ExtraEndOfTurnTriggers:
                    ApplyGhastlyMask(reward);
                    break;
                case QuestRewardEffectKind.GainRandomPlaceholder92:
                    ApplyFriendsAlongTheWay(reward);
                    break;
                case QuestRewardEffectKind.SpinYoggWheel:
                    ApplyYoggTasticTasties(reward);
                    break;
                case QuestRewardEffectKind.DiscoverCurrentTierMinion:
                    StartTierDiscover(State.Player.Tavern.Tier, "quest:" + reward.Id);
                    break;
                case QuestRewardEffectKind.WisdomballHelpfulRefreshes:
                    ApplyWondrousWisdomball(shop ?? State.Player.Tavern.Shop, reward);
                    break;
                case QuestRewardEffectKind.GainTransformingZerus:
                    AddTransformingZerusToHand(reward);
                    break;
                case QuestRewardEffectKind.ChooseNewRewardsEachTurn:
                    OfferEtherealEvidenceRewards(reward);
                    break;
                case QuestRewardEffectKind.AddKidnapSackSpellcraft:
                    AddGeneratedSpellsToHand(KidnapSackSpellCardId, 1, reward.Name);
                    MarkTemporarySpellcraftCard(KidnapSackSpellCardId);
                    break;
                case QuestRewardEffectKind.AddGoldenHammerSpellcraft:
                    AddGeneratedSpellsToHand(GoldenHammerSpellCardId, 1, reward.Name);
                    MarkTemporarySpellcraftCard(GoldenHammerSpellCardId);
                    break;
                case QuestRewardEffectKind.GainTierUpTransformSpells:
                    AddGeneratedSpellsToHand(TimelineAcceleratorSpellCardId, Math.Max(1, reward.TargetCount), reward.Name);
                    break;
                case QuestRewardEffectKind.GuidancePlaceholder92ShopSlots:
                    ApplyScepterOfGuidance(shop ?? State.Player.Tavern.Shop, reward);
                    break;
                case QuestRewardEffectKind.GoldenFriendlyTierAndImprove:
                    ApplySmeltingChamber(reward);
                    break;
                case QuestRewardEffectKind.GainTierSevenCopy:
                    GainTierSevenCopy(reward);
                    break;
                case QuestRewardEffectKind.CastRandomTavernSpells:
                    CastRandomTavernSpells(Math.Max(1, reward.TargetCount), reward.Name);
                    break;
                case QuestRewardEffectKind.UnlockTierSevenAndAutoUpgrade:
                    ScheduleNorgannonAutoUpgrade(reward);
                    break;
                case QuestRewardEffectKind.MagicfinRelic:
                    ApplyMagicfinRelic(reward);
                    break;
                case QuestRewardEffectKind.TwoCopiesTripleRule:
                case QuestRewardEffectKind.ExtraBattlecryTriggers:
                case QuestRewardEffectKind.ExtraTavernSpellCast:
                case QuestRewardEffectKind.ScalingTavernSpellBonus:
                case QuestRewardEffectKind.ExtraRallyTriggers:
                    break;
            }
        }

        private void GrantQuestGold(int amount, string source)
        {
            if (amount <= 0)
            {
                return;
            }

            var tavern = State.Player.Tavern;
            var before = tavern.Gold;
            tavern.Gold = Math.Min(StatMath.MaxStat, tavern.Gold + amount);
            AddRecruitLog(RecruitLogType.Play, source + ": gained " + amount + " Gold.", before, tavern.Gold);
            TryTriggerSplinterOfAurum();
        }

        private void ApplyAnimaBribe(MinionInstance sold, QuestRewardDefinition reward)
        {
            if (sold == null || sold.CardKind != CardKind.Minion)
            {
                return;
            }

            var candidates = State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var picked = new SeededRng(State.Seed + State.Round * 2441 + State.Player.Tavern.RecruitLog.Count).Pick(candidates);
            BuffMinion(picked, Math.Max(0, sold.Attack), Math.Max(0, sold.MaxHealth), reward.Name);
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": gave " + sold.Name + "'s stats to " + picked.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyInvigoratingConch(MinionInstance bought, QuestRewardDefinition reward)
        {
            if (bought == null || bought.CardKind != CardKind.Minion)
            {
                return;
            }

            var candidates = State.Player.Board.Where(minion => minion != null).ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var picked = new SeededRng(State.Seed + State.Round * 2459 + State.Player.Tavern.RecruitLog.Count).Pick(candidates);
            BuffMinion(picked, Math.Max(0, bought.Attack), Math.Max(0, bought.MaxHealth), reward.Name);
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": gave bought minion stats to " + picked.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyDoubleHeadedReward(ActiveQuestState active, MinionInstance bought, QuestRewardDefinition reward)
        {
            if (active == null || bought == null)
            {
                return;
            }

            var key = QuestRewardCounterKey(reward.Id, "firstBuyRound");
            var quests = EnsureQuestState(State.Player.Tavern);
            if (quests.RewardCounters.TryGetValue(key, out var round) && round == State.Round)
            {
                return;
            }

            quests.RewardCounters[key] = State.Round;
            CopyCardToHand(bought, reward.Name);
        }

        private void ApplySplittingScroll(MinionInstance bought, QuestRewardDefinition reward)
        {
            if (bought == null || bought.CardKind != CardKind.TavernSpell)
            {
                return;
            }

            var cost = bought.Counters.TryGetValue("last_purchase_cost", out var storedCost)
                ? storedCost
                : Math.Max(0, bought.Cost);
            if (cost < 3)
            {
                return;
            }

            CopyCardToHand(bought, reward.Name);
        }

        private void ApplyGoldenForge(QuestRewardDefinition reward)
        {
            TryMakeHighestTierShopMinionGolden(reward.Name);
        }

        private bool TryMakeHighestTierShopMinionGolden(string sourceName)
        {
            var target = State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion && !card.Golden)
                .OrderByDescending(card => card.TavernTier)
                .FirstOrDefault();
            if (target == null)
            {
                return false;
            }

            MakeGoldenInPlace(target);
            AddRecruitLog(RecruitLogType.Play, sourceName + ": made " + target.Name + " Golden.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            return true;
        }

        private void ApplySnickerSnacks(QuestRewardDefinition reward)
        {
            var candidates = State.Player.Board
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion && minion.Keywords.Contains(Keyword.Battlecry))
                .ToList();
            var targetCount = Math.Max(1, reward.TargetCount);
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 2467 + State.Player.Tavern.RecruitLog.Count);
            var triggered = 0;
            while (triggered < targetCount && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var target = candidates[index];
                candidates.RemoveAt(index);
                ResolveMinionBattlecry(target);
                triggered += 1;
            }

            AddRecruitLog(RecruitLogType.Play, reward.Name + ": triggered " + triggered + " friendly Battlecry minion(s).", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyTealTigerSapphire(List<MinionInstance> shop, QuestRewardDefinition reward)
        {
            var quests = EnsureQuestState(State.Player.Tavern);
            var countKey = QuestRewardCounterKey(reward.Id, "refreshesThisTurn");
            quests.RewardCounters.TryGetValue(countKey, out var refreshes);
            refreshes += 1;
            quests.RewardCounters[countKey] = refreshes;

            var attackPerRefresh = Math.Max(1, reward.AttackBonus);
            var healthPerRefresh = Math.Max(1, reward.HealthBonus);
            foreach (var card in (shop ?? State.Player.Tavern.Shop).Where(card => card != null && card.CardKind == CardKind.Minion))
            {
                ApplyTealTigerSapphireDelta(card, refreshes, attackPerRefresh, healthPerRefresh, reward.Id);
            }
        }

        private void ResetTealTigerSapphireTurnState()
        {
            var tavern = State.Player.Tavern;
            var quests = EnsureQuestState(tavern);
            quests.RewardCounters.Remove(QuestRewardCounterKey(TealTigerSapphireRewardId, "refreshesThisTurn"));
            foreach (var card in tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion))
            {
                RemoveTealTigerSapphireBuff(card, TealTigerSapphireRewardId);
            }
        }

        private static void ApplyTealTigerSapphireDelta(MinionInstance target, int refreshes, int attackPerRefresh, int healthPerRefresh, string sourceId)
        {
            if (target == null)
            {
                return;
            }

            var counterKey = sourceId + ":appliedRefreshes";
            target.Counters.TryGetValue(counterKey, out var appliedRefreshes);
            var deltaRefreshes = Math.Max(0, refreshes - appliedRefreshes);
            if (deltaRefreshes <= 0)
            {
                return;
            }

            var attack = StatMath.SaturatingMultiply(deltaRefreshes, attackPerRefresh, 0, StatMath.MaxStat);
            var health = StatMath.SaturatingMultiply(deltaRefreshes, healthPerRefresh, 0, StatMath.MaxStat);
            StatMath.ApplyStatDelta(target, attack, health);
            target.Enchantments.Add(new Enchantment
            {
                Id = sourceId,
                SourceId = sourceId,
                AttackBonus = attack,
                HealthBonus = health
            });
            target.Counters[counterKey] = refreshes;
            RefreshScarletSurvivor(target);
        }

        private static void RemoveTealTigerSapphireBuff(MinionInstance target, string sourceId)
        {
            if (target == null)
            {
                return;
            }

            var enchantments = target.Enchantments
                .Where(enchantment => enchantment != null && string.Equals(enchantment.SourceId, sourceId, StringComparison.Ordinal))
                .ToList();
            foreach (var enchantment in enchantments)
            {
                StatMath.ApplyStatDeltaPreservingDamage(
                    target,
                    StatMath.SaturatingSubtract(0, enchantment.AttackBonus),
                    StatMath.SaturatingSubtract(0, enchantment.HealthBonus));
                target.Enchantments.Remove(enchantment);
            }

            target.Counters.Remove(sourceId + ":appliedRefreshes");
        }

        private void ApplyDevilsInTheDetails(QuestRewardDefinition reward)
        {
            var edges = new List<MinionInstance>();
            var left = State.Player.Board.FirstOrDefault();
            var right = State.Player.Board.LastOrDefault();
            if (left != null)
            {
                edges.Add(left);
            }

            if (right != null && (left == null || !string.Equals(left.InstanceId, right.InstanceId, StringComparison.Ordinal)))
            {
                edges.Add(right);
            }

            foreach (var eater in edges)
            {
                ConsumeRandomShopMinionForQuest(eater, reward.Name);
            }
        }

        private void ConsumeRandomShopMinionForQuest(MinionInstance eater, string sourceName)
        {
            if (eater == null)
            {
                return;
            }

            var candidates = State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var eaterIndex = Math.Max(0, State.Player.Board.IndexOf(eater));
            var rng = new SeededRng(State.Seed + State.Round * 2473 + State.Player.Tavern.RecruitLog.Count + eaterIndex * 31);
            var picked = rng.Pick(candidates);
            State.Player.Tavern.Shop[picked.Index] = null;
            TavernShopSlots.ClearSlot(State.Player.Tavern, picked.Index);
            BuffMinion(eater, Math.Max(0, picked.Card.Attack), Math.Max(0, picked.Card.MaxHealth), sourceName);
            HandleDevourForTierSixSevenMinions();
            ReleaseMinionToPool(picked.Card);
            AddRecruitLog(RecruitLogType.Play, sourceName + ": " + eater.Name + " consumed " + picked.Card.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyGiftOfTheGoldenKobold(QuestRewardDefinition reward)
        {
            var threshold = Math.Max(1, reward.TargetCount);
            var quests = EnsureQuestState(State.Player.Tavern);
            var key = QuestRewardCounterKey(reward.Id, "refreshes");
            quests.RewardCounters.TryGetValue(key, out var refreshes);
            refreshes += 1;
            if (refreshes >= threshold)
            {
                refreshes -= threshold;
                TryMakeHighestTierShopMinionGolden(reward.Name);
            }

            quests.RewardCounters[key] = refreshes;
        }

        private void ApplyVictimsSpecter(MinionInstance lastDeadFriendly, QuestRewardDefinition reward)
        {
            if (lastDeadFriendly == null || State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var definition = catalog.All.FirstOrDefault(card =>
                string.Equals(card.CardId, lastDeadFriendly.CardId, StringComparison.OrdinalIgnoreCase));
            var copy = definition != null
                ? MinionFactory.Create(definition, BoardSide.Player, "victims-specter-" + State.Round + "-" + State.Player.Tavern.Hand.Count, lastDeadFriendly.Golden, PoolSource.Copy, 0)
                : CreatePlainCopy(lastDeadFriendly, "victims-specter-" + State.Round + "-" + State.Player.Tavern.Hand.Count);
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Copy;
            copy.OriginPoolSource = PoolSource.Copy;
            copy.PoolCopiesHeld = 0;
            copy.CanReturnToPoolAfterAttach = false;
            State.Player.Tavern.Hand.Add(copy);
            HandleCardsAddedToHand(1, reward.Name);
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": copied " + copy.Name + " to hand.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyStolenGoldCombatStart(List<MinionInstance> combatBoard)
        {
            var left = combatBoard?.FirstOrDefault();
            var right = combatBoard?.LastOrDefault();
            if (left != null)
            {
                MakeGoldenInPlace(left);
            }

            if (right != null && (left == null || !string.Equals(left.InstanceId, right.InstanceId, StringComparison.Ordinal)))
            {
                MakeGoldenInPlace(right);
            }

            AddRecruitLog(RecruitLogType.Play, "Stolen Gold: edge minions are Golden for combat.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyEvilTwinCombatStart(List<MinionInstance> combatBoard)
        {
            if (combatBoard == null || combatBoard.Count >= BoardLimit)
            {
                return;
            }

            var target = combatBoard
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion)
                .OrderByDescending(minion => minion.Health)
                .ThenBy(minion => combatBoard.IndexOf(minion))
                .FirstOrDefault();
            if (target == null)
            {
                return;
            }

            var copy = target.Clone();
            copy.InstanceId = "quest-evil-twin-" + target.InstanceId + "-" + State.Round;
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Summon;
            copy.OriginPoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            combatBoard.Add(copy);
            AddRecruitLog(RecruitLogType.Play, "Evil Twin: summoned a copy of " + target.Name + " for combat.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyRitualDaggerCombatReward(CombatReward reward)
        {
            if (!HasActiveQuestReward(RitualDaggerRewardId))
            {
                return;
            }

            var target = State.Player.Board.FirstOrDefault(minion =>
                minion != null &&
                string.Equals(minion.InstanceId, reward.SourceInstanceId, StringComparison.Ordinal));
            if (target == null)
            {
                return;
            }

            var attack = 5;
            var health = 5;
            var sourceName = "Ritual Dagger";
            if (questCatalog != null && questCatalog.TryGetRewardById(RitualDaggerRewardId, out var definition))
            {
                attack = Math.Max(1, definition.AttackBonus);
                health = Math.Max(1, definition.HealthBonus);
                sourceName = definition.Name;
            }

            BuffMinion(target, attack, health, sourceName);
            AddRecruitLog(RecruitLogType.Play, sourceName + ": permanently buffed " + target.Name + " +" + attack + "/+" + health + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private bool CopyCardToHand(MinionInstance source, string sourceName)
        {
            var tavern = State.Player.Tavern;
            if (source == null || tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var copy = source.Clone();
            copy.InstanceId = "player-" + copy.DefinitionId + "-quest-copy-" + State.Round + "-" + tavern.Hand.Count + "-" + tavern.RecruitLog.Count;
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Copy;
            copy.OriginPoolSource = PoolSource.Copy;
            copy.PoolCopiesHeld = 0;
            copy.CanReturnToPoolAfterAttach = false;
            tavern.Hand.Add(copy);
            HandleCardsAddedToHand(1, sourceName);
            AddRecruitLog(RecruitLogType.Play, sourceName + ": copied " + source.Name + " to hand.", tavern.Gold, tavern.Gold);
            return true;
        }

        private void CopyDiscoveredCardToHand(MinionInstance picked, QuestRewardDefinition reward)
        {
            if (picked == null || reward == null || !IsHandCopyEligible(picked))
            {
                return;
            }

            CopyCardToHand(picked, reward.Name);
        }

        private static bool IsHandCopyEligible(MinionInstance card)
        {
            return card != null &&
                   (card.CardKind == CardKind.Minion ||
                    card.CardKind == CardKind.HeroBuddy ||
                    card.CardKind == CardKind.TavernSpell ||
                    card.CardKind == CardKind.Spell);
        }

        private void GainGoldenBuddy(QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            if (heroCatalog == null || tavern.Hand.Count >= HandLimit || string.IsNullOrEmpty(State.Player.HeroId))
            {
                return;
            }

            HeroBuddyDefinition buddy;
            try
            {
                buddy = heroCatalog.GetHeroByCardId(State.Player.HeroId).Buddy;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (buddy == null || string.IsNullOrEmpty(buddy.CardId))
            {
                AddRecruitLog(RecruitLogType.Play, reward.Name + ": no Buddy is available for this hero.", tavern.Gold, tavern.Gold);
                return;
            }

            var card = MinionFactory.Create(
                buddy,
                BoardSide.Player,
                "quest-partner-" + State.Round + "-" + tavern.Hand.Count,
                PoolSource.Copy);
            MakeGoldenInPlace(card);
            tavern.Hand.Add(card);
            HandleCardsAddedToHand(1, reward.Name);
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": gained Golden Buddy " + card.Name + ".", tavern.Gold, tavern.Gold);
        }

        private void StartDoppelgangersLocketDiscover(QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Discover != null)
            {
                AddRecruitLog(RecruitLogType.Discover, reward.Name + ": Discover is already pending.", tavern.Gold, tavern.Gold);
                return;
            }

            var candidates = EnsureOpponentHistory().LastOpponentWarband
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion && !minion.Golden)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3181 + tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var copy = candidates[index].Clone();
                candidates.RemoveAt(index);
                copy.InstanceId = "quest-doppelganger-" + copy.DefinitionId + "-" + State.Round + "-" + options.Count;
                copy.Owner = BoardSide.Player;
                copy.PoolSource = PoolSource.Copy;
                copy.OriginPoolSource = PoolSource.Copy;
                copy.PoolCopiesHeld = 0;
                copy.CanReturnToPoolAfterAttach = false;
                options.Add(copy);
            }

            tavern.Discover = new DiscoverState
            {
                Source = "quest:" + reward.Id,
                RewardTier = State.Player.Tavern.Tier,
                Options = options
            };
            AddRecruitLog(RecruitLogType.Discover, reward.Name + ": Discover an opponent warband minion.", tavern.Gold, tavern.Gold);
        }

        private void StartBuddyDiscover(QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            if (heroCatalog == null || tavern.Discover != null)
            {
                if (tavern.Discover != null)
                {
                    AddRecruitLog(RecruitLogType.Discover, reward.Name + ": Discover is already pending.", tavern.Gold, tavern.Gold);
                }

                return;
            }

            var candidates = heroCatalog.AllBuddies
                .Where(buddy => buddy != null && !string.IsNullOrEmpty(buddy.CardId) && !buddy.ExcludedFromBuddyDiscover)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3203 + tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var buddy = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(
                    buddy,
                    BoardSide.Player,
                    "quest-buddy-" + State.Round + "-" + options.Count,
                    PoolSource.Discover));
            }

            tavern.Discover = new DiscoverState
            {
                Source = "quest-buddy:" + reward.Id,
                RewardTier = 0,
                Options = options
            };
            AddRecruitLog(RecruitLogType.Discover, reward.Name + ": Discover a Buddy.", tavern.Gold, tavern.Gold);
        }

        private void StartSecondHeroPowerDiscover(QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            if (heroCatalog == null || tavern.Discover != null)
            {
                if (tavern.Discover != null)
                {
                    AddRecruitLog(RecruitLogType.Discover, reward.Name + ": Discover is already pending.", tavern.Gold, tavern.Gold);
                }

                return;
            }

            if (State.Player.ExtraHeroPowerCardIds == null)
            {
                State.Player.ExtraHeroPowerCardIds = new List<string>();
            }

            var owned = new HashSet<string>(State.Player.ExtraHeroPowerCardIds, StringComparer.OrdinalIgnoreCase);
            owned.Add(State.Player.HeroPowerCardId ?? string.Empty);
            var candidates = heroCatalog.GetDiscoverableHeroPowers(State.Player.HeroPowerCardId)
                .Where(power => power != null && !owned.Contains(power.CardId))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3251 + tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var power = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(power, BoardSide.Player, "quest-cosmic-" + State.Round + "-" + options.Count));
            }

            tavern.Discover = new DiscoverState
            {
                Source = "quest-second-hero-power",
                RewardTier = 0,
                Options = options
            };
            AddRecruitLog(RecruitLogType.Discover, reward.Name + ": Discover a second Hero Power.", tavern.Gold, tavern.Gold);
        }

        private void AddSecondHeroPower(MinionInstance picked)
        {
            if (picked == null || picked.CardKind != CardKind.HeroPower)
            {
                return;
            }

            if (State.Player.ExtraHeroPowerCardIds == null)
            {
                State.Player.ExtraHeroPowerCardIds = new List<string>();
            }

            if (!State.Player.ExtraHeroPowerCardIds.Any(cardId => string.Equals(cardId, picked.CardId, StringComparison.OrdinalIgnoreCase)))
            {
                State.Player.ExtraHeroPowerCardIds.Add(picked.CardId);
            }

            AddRecruitLog(RecruitLogType.Discover, "Second Hero Power gained: " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ScheduleQuestTrinketChoice(QuestRewardDefinition reward, TrinketSlotKind slotKind)
        {
            if (reward == null)
            {
                return;
            }

            var quests = EnsureQuestState(State.Player.Tavern);
            quests.RewardCounters[QuestRewardCounterKey(reward.Id, "trinketRound")] = State.Round + 1;
            AddRecruitLog(RecruitLogType.Discover, reward.Name + ": " + slotKind + " Trinket choice scheduled for next turn.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyGhastlyMask(QuestRewardDefinition reward)
        {
            HandleTurnEndedForTierOneMinions();
            HandleTurnEndedForTierThreeMinions();
            HandleTurnEndedForTierFourMinions();
            HandleTurnEndedForTierFiveMinions();
            HandleTurnEndedForTierSixSevenMinions();
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": friendly end-of-turn minion effects repeated once.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyFriendsAlongTheWay(QuestRewardDefinition reward)
        {
            AddRandomTierMinionsToHand(Math.Max(1, State.Player.Tavern.Tier), Math.Max(1, reward.TargetCount), reward.Name);
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": used placeholder-92 proxy minions.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyYoggTasticTasties(QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            var rng = new SeededRng(State.Seed + State.Round * 3257 + tavern.RecruitLog.Count);
            switch (rng.NextInt(6))
            {
                case 0:
                    GrantQuestGold(3, reward.Name);
                    break;
                case 1:
                    BuffAllMinions(State.Player.Board, 3, 3, reward.Name);
                    break;
                case 2:
                    AddRandomTavernSpellToHand(State.Player.Tavern.Tier, 1, reward.Name);
                    break;
                case 3:
                    tavern.FreeRefreshes = StatMath.SaturatingAdd(tavern.FreeRefreshes, 2, 0, StatMath.MaxStat);
                    break;
                case 4:
                    BuffAllMinions(tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 4, 4, reward.Name);
                    break;
                default:
                    AddRandomTierMinionsToHand(Math.Max(1, tavern.Tier), 1, reward.Name);
                    break;
            }
        }

        private void ApplyWondrousWisdomball(List<MinionInstance> shop, QuestRewardDefinition reward)
        {
            var interval = Math.Max(2, reward.TargetCount);
            var quests = EnsureQuestState(State.Player.Tavern);
            var key = QuestRewardCounterKey(reward.Id, "refreshes");
            quests.RewardCounters.TryGetValue(key, out var refreshes);
            refreshes += 1;
            if (refreshes >= interval)
            {
                refreshes = 0;
                State.Player.Tavern.HelpfulRefreshes = StatMath.SaturatingAdd(State.Player.Tavern.HelpfulRefreshes, 1, 0, StatMath.MaxStat);
                ApplyHelpfulRefresh(shop);
                AddRecruitLog(RecruitLogType.Play, reward.Name + ": provided a helpful refresh.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }

            quests.RewardCounters[key] = refreshes;
        }

        private void AddTransformingZerusToHand(QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            tavern.Hand.Add(CreateShifterZerusProxyCard(reward.Name + "-" + State.Round + "-" + tavern.Hand.Count));
            HandleCardsAddedToHand(1, reward.Name);
        }

        private void TransformQuestZerusCards()
        {
            var tavern = State.Player.Tavern;
            var rng = new SeededRng(State.Seed + State.Round * 3263 + tavern.RecruitLog.Count);
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier <= Math.Max(1, tavern.Tier))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            for (var index = 0; index < tavern.Hand.Count; index += 1)
            {
                var card = tavern.Hand[index];
                if (card?.Tags == null || !card.Tags.Contains("quest_transforming_zerus"))
                {
                    continue;
                }

                var transformed = MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "quest-zerus-" + State.Round + "-" + index, false, PoolSource.Copy, 0);
                transformed.Tags.Add("quest_transforming_zerus");
                transformed.Tags.Add("generated_by_shifter_zerus_proxy");
                tavern.Hand[index] = transformed;
            }
        }

        private void OfferEtherealEvidenceRewards(QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            var advanced = EnsureAdvancedMechanicState(tavern);
            if (advanced.PendingChoice != null || questCatalog == null)
            {
                return;
            }

            var quest = questCatalog.ImplementedQuests.FirstOrDefault();
            var rewards = questCatalog.OfferableRewards
                .Where(candidate => !string.Equals(candidate.Id, reward.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (quest == null || rewards.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3271 + tavern.RecruitLog.Count);
            var request = new MechanicChoiceRequest
            {
                RequestId = "quest-ethereal-evidence-" + State.Round + "-" + tavern.RecruitLog.Count,
                Kind = AdvancedMechanicKind.Quest,
                Source = "quest-ethereal-evidence",
                Slot = "Bonus",
                Round = State.Round,
                RemainingPicks = 1
            };

            while (request.Options.Count < Math.Max(1, reward.TargetCount) && rewards.Count > 0)
            {
                var index = rng.NextInt(rewards.Count);
                var picked = rewards[index];
                rewards.RemoveAt(index);
                request.Options.Add(CreateQuestChoiceOption(quest, picked, "Bonus"));
            }

            advanced.PendingChoice = request;
            AddRecruitLog(RecruitLogType.Discover, reward.Name + ": choose a new Quest Reward.", tavern.Gold, tavern.Gold);
        }

        private void ApplyScepterOfGuidance(List<MinionInstance> shop, QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            var targets = shop ?? tavern.Shop;
            if (targets == null)
            {
                return;
            }

            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier <= Math.Max(1, tavern.Tier))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3277 + tavern.RecruitLog.Count);
            var count = Math.Max(1, reward.TargetCount);
            for (var index = 0; index < count; index += 1)
            {
                var card = MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "guidance-" + State.Round + "-" + index, false, PoolSource.Pool, 1);
                if (index < targets.Count)
                {
                    if (targets[index] != null)
                    {
                        ReleaseMinionToPool(targets[index]);
                    }

                    targets[index] = card;
                    TavernShopSlots.ClearSlot(tavern, index);
                }
                else
                {
                    targets.Add(card);
                }
            }

            TavernShopSlots.Ensure(tavern);
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": filled placeholder-92 Tavern slots.", tavern.Gold, tavern.Gold);
        }

        private void ApplySmeltingChamber(QuestRewardDefinition reward)
        {
            var quests = EnsureQuestState(State.Player.Tavern);
            var key = QuestRewardCounterKey(reward.Id, "tier");
            var tier = GetQuestRewardCounter(reward.Id, "tier", Math.Max(1, reward.TargetCount));
            var target = State.Player.Board
                .FirstOrDefault(minion => minion != null && minion.CardKind == CardKind.Minion && minion.TavernTier == tier && !minion.Golden);
            if (target != null)
            {
                MakeGoldenInPlace(target);
                AddRecruitLog(RecruitLogType.Play, reward.Name + ": made a Tier " + tier + " friendly minion Golden.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }

            quests.RewardCounters[key] = Math.Min(TavernRules.MaxTavernTier, tier + 1);
        }

        private void GainTierSevenCopy(QuestRewardDefinition reward)
        {
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier == TavernRules.MaxTavernTier)
                .ToList();
            if (candidates.Count == 0)
            {
                var highest = AvailableMinions().Where(minion => minion.InPool).Select(minion => minion.TavernTier).DefaultIfEmpty(1).Max();
                candidates = AvailableMinions().Where(minion => minion.InPool && minion.TavernTier == highest).ToList();
            }

            if (candidates.Count == 0 || State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3299 + State.Player.Tavern.RecruitLog.Count);
            State.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "secret-culprit-" + State.Round, false, PoolSource.Copy, 0));
            HandleCardsAddedToHand(1, reward.Name);
        }

        private void CastRandomTavernSpells(int count, string source)
        {
            CastRandomTavernSpells(count, State.Player.Tavern.Tier, source, "untamed");
        }

        private int CastRandomTavernSpells(int count, int maxTier, string source, string instancePrefix)
        {
            var tavern = State.Player.Tavern;
            if (count <= 0)
            {
                return 0;
            }

            if (!TryEnterAutomaticTavernSpellCast(source))
            {
                return 0;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3301 + tavern.RecruitLog.Count);
            var candidates = AvailableTavernSpells()
                .Where(spell => spell.TavernTier <= Math.Max(1, maxTier))
                .ToList();
            var cast = 0;
            try
            {
                while (cast < count && candidates.Count > 0)
                {
                    var definition = rng.Pick(candidates);
                    var spell = MinionFactory.Create(definition, BoardSide.Player, instancePrefix + "-" + State.Round + "-" + cast);
                    if (CastAutomaticTavernSpell(spell, source, -1, State.Seed + State.Round * 3307 + tavern.RecruitLog.Count + cast))
                    {
                        cast += 1;
                    }
                    else
                    {
                        candidates.Remove(definition);
                    }
                }
            }
            finally
            {
                ExitAutomaticTavernSpellCast();
            }

            if (cast > 0)
            {
                AddRecruitLog(RecruitLogType.Play, source + ": randomly cast " + cast + " Tavern spell(s).", tavern.Gold, tavern.Gold);
            }

            return cast;
        }

        private bool TryEnterAutomaticTavernSpellCast(string source)
        {
            if (automaticTavernSpellCastDepth >= AutomaticTavernSpellCastMaxDepth)
            {
                AddRecruitLog(
                    RecruitLogType.Play,
                    source + ": automatic Tavern spell cast chain stopped at depth " + AutomaticTavernSpellCastMaxDepth + ".",
                    State.Player.Tavern.Gold,
                    State.Player.Tavern.Gold);
                return false;
            }

            automaticTavernSpellCastDepth += 1;
            return true;
        }

        private void ExitAutomaticTavernSpellCast()
        {
            automaticTavernSpellCastDepth = Math.Max(0, automaticTavernSpellCastDepth - 1);
        }

        private bool CastAutomaticTavernSpell(MinionInstance spell, string source, int targetIndex, int rngSeed)
        {
            if (spell == null || (spell.CardKind != CardKind.TavernSpell && spell.CardKind != CardKind.Spell))
            {
                return false;
            }

            var tavern = State.Player.Tavern;
            var resolvedTargetIndex = ResolveDebugSpellTargetIndex(spell, targetIndex);
            try
            {
                ValidateExplicitPlayTarget(spell, resolvedTargetIndex);
            }
            catch (InvalidOperationException ex)
            {
                AddRecruitLog(RecruitLogType.Play, source + ": skipped " + spell.Name + " - " + ex.Message, tavern.Gold, tavern.Gold);
                return false;
            }

            var spellTargetId = ResolveFriendlyBoardTargetId(resolvedTargetIndex);
            string spellResult;
            var spellcraftCastCount = 1;
            if (TryCastQuestRewardSpell(spell, resolvedTargetIndex, out spellResult))
            {
                HandleAutomaticSpellCastSideEffects(spell, spellTargetId, spellcraftCastCount);
                return true;
            }

            var dynamicBonus = GetBoardTavernSpellBonus();
            var perpetualBonus = spell.CardKind == CardKind.TavernSpell ? GetPerpetualIncantationBonus() : (Attack: 0, Health: 0);
            var trinketBonus = spell.CardKind == CardKind.TavernSpell ? GetTrinketTavernSpellBonus() : (Attack: 0, Health: 0);
            tavern.TavernSpellBonusAttack += dynamicBonus.Attack + perpetualBonus.Attack + trinketBonus.Attack;
            tavern.TavernSpellBonusHealth += dynamicBonus.Health + perpetualBonus.Health + trinketBonus.Health;
            try
            {
                spellResult = TavernSpellEngine.Cast(spell, State, catalog, spellCatalog, new SeededRng(rngSeed), resolvedTargetIndex, heroCatalog);
                var extraCasts = GetTavernSpellExtraCasts(spell);
                spellcraftCastCount += extraCasts;
                for (var extraCast = 0; extraCast < extraCasts; extraCast += 1)
                {
                    spellResult += " + " + TavernSpellEngine.Cast(spell, State, catalog, spellCatalog, new SeededRng(rngSeed + extraCast + 1), resolvedTargetIndex, heroCatalog);
                }
            }
            finally
            {
                tavern.TavernSpellBonusAttack -= dynamicBonus.Attack;
                tavern.TavernSpellBonusHealth -= dynamicBonus.Health;
                tavern.TavernSpellBonusAttack -= perpetualBonus.Attack;
                tavern.TavernSpellBonusHealth -= perpetualBonus.Health;
                tavern.TavernSpellBonusAttack -= trinketBonus.Attack;
                tavern.TavernSpellBonusHealth -= trinketBonus.Health;
            }

            HandleAutomaticSpellCastSideEffects(spell, spellTargetId, spellcraftCastCount);
            return true;
        }

        private void HandleAutomaticSpellCastSideEffects(MinionInstance spell, string spellTargetId, int spellcraftCastCount)
        {
            var tavern = State.Player.Tavern;
            HandleSpellCastOnTarget(spell, spellTargetId);
            DispatchTrinketSpellcraftCast(spell, spellcraftCastCount);
            DispatchTrinketSpellCast(spell);
            RecordQuestProgress(QuestObjectiveKind.CastSpells, 1);
            if (spell.CardKind == CardKind.TavernSpell)
            {
                tavern.TavernSpellsCastThisTurn = StatMath.SaturatingAdd(tavern.TavernSpellsCastThisTurn, 1, 0, StatMath.MaxStat);
                tavern.TavernSpellsCastThisGame = StatMath.SaturatingAdd(tavern.TavernSpellsCastThisGame, 1, 0, StatMath.MaxStat);
                tavern.CardsPlayedThisTurn = StatMath.SaturatingAdd(tavern.CardsPlayedThisTurn, 1, 0, StatMath.MaxStat);
                tavern.LastTavernSpellCardId = spell.CardId;
                RecordQuestProgress(QuestObjectiveKind.CastTavernSpells, 1);
                DispatchBoardEvent(MechanicEventType.TavernSpellCast);
                HandleTavernSpellCastForTierThreeMinions(spell);
                HandleTavernSpellCastForTierFourMinions(spell);
                HandleTavernSpellCastForTierFiveMinions(spell);
                HandleTavernSpellCastForTierSixSevenMinions(spell);
                DispatchHeroEffect(HeroEffectEventType.TavernSpellCast, spell);
                DispatchTrinketTavernSpellCast(spell, false);
                ImprovePerpetualIncantation();
            }

            HandleCardPlayedForTierFiveMinions(spell);
            HandleCardPlayedForTierSixSevenMinions(spell);
        }

        private void ScheduleNorgannonAutoUpgrade(QuestRewardDefinition reward)
        {
            var quests = EnsureQuestState(State.Player.Tavern);
            if (GetQuestRewardCounter(reward.Id, "used", 0) > 0)
            {
                return;
            }

            quests.RewardCounters[QuestRewardCounterKey(reward.Id, "pendingRound")] = State.Round + 1;
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": Tavern Tier 7 unlocked; one automatic upgrade scheduled.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyNorgannonAutoUpgrade()
        {
            if (!HasActiveQuestReward(NorgannonsRewardId))
            {
                return;
            }

            var quests = EnsureQuestState(State.Player.Tavern);
            var pendingKey = QuestRewardCounterKey(NorgannonsRewardId, "pendingRound");
            if (!quests.RewardCounters.TryGetValue(pendingKey, out var pendingRound) || pendingRound > State.Round)
            {
                return;
            }

            quests.RewardCounters.Remove(pendingKey);
            quests.RewardCounters[QuestRewardCounterKey(NorgannonsRewardId, "used")] = 1;
            var tavern = State.Player.Tavern;
            if (tavern.Tier < TavernRules.MaxTavernTier)
            {
                tavern.Tier += 1;
                tavern.UpgradeCost = tavern.Tier >= TavernRules.MaxTavernTier ? 0 : TavernRules.GetUpgradeCost(tavern.Tier);
                AddRecruitLog(RecruitLogType.LevelUp, "Norgannon's Reward: upgraded Tavern to Tier " + tavern.Tier + ".", tavern.Gold, tavern.Gold);
                DispatchTrinketTavernTierReached();
            }
        }

        private void ApplyMagicfinRelic(QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            var token = CreateMagicfinToken(reward.Name + "-" + State.Round + "-" + tavern.RecruitLog.Count);
            if (State.Player.Board.Count < BoardLimit)
            {
                State.Player.Board.Add(token);
                StartMagicfinRelicDiscover(token, reward);
            }
            else if (tavern.Hand.Count < HandLimit)
            {
                tavern.Hand.Add(token);
                HandleCardsAddedToHand(1, reward.Name);
            }
        }

        private void StartMagicfinRelicDiscover(MinionInstance token, QuestRewardDefinition reward)
        {
            var tavern = State.Player.Tavern;
            if (token == null || tavern.Discover != null)
            {
                return;
            }

            var candidates = AvailableTavernSpells()
                .Where(spell => spell.TavernTier <= Math.Max(1, tavern.Tier))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3313 + tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "magicfin-" + State.Round + "-" + options.Count));
            }

            tavern.Discover = new DiscoverState
            {
                Source = "quest-magicfin:" + token.InstanceId,
                TargetInstanceId = token.InstanceId,
                RewardTier = tavern.Tier,
                Options = options
            };
            AddRecruitLog(RecruitLogType.Discover, reward.Name + ": Discover a Tavern spell for " + token.Name + ".", tavern.Gold, tavern.Gold);
        }

        private void ResolveMagicfinRelicDiscover(DiscoverState discover, MinionInstance picked)
        {
            if (discover == null || picked == null)
            {
                return;
            }

            var targetIndex = State.Player.Board.FindIndex(minion => string.Equals(minion.InstanceId, discover.TargetInstanceId, StringComparison.Ordinal));
            if (targetIndex < 0)
            {
                return;
            }

            var result = TavernSpellEngine.Cast(picked, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 3319 + State.Player.Tavern.RecruitLog.Count), targetIndex, heroCatalog);
            AddRecruitLog(RecruitLogType.Discover, "Magicfin Relic taught " + picked.Name + ": " + result, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private bool TryCastQuestRewardSpell(MinionInstance spell, int targetIndex, out string result)
        {
            result = null;
            if (spell == null)
            {
                return false;
            }

            switch (spell.CardId)
            {
                case KidnapSackSpellCardId:
                    result = CastKidnapSack();
                    return true;
                case GoldenHammerSpellCardId:
                    result = CastGoldenHammer(targetIndex);
                    return true;
                case TimelineAcceleratorSpellCardId:
                    result = CastTimelineAccelerator(targetIndex);
                    return true;
                default:
                    return false;
            }
        }

        private string CastKidnapSack()
        {
            var tavern = State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return "hand is full";
            }

            var picked = tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .FirstOrDefault(item => item.Card != null && !item.Card.Golden);
            if (picked == null)
            {
                return "no non-Golden Tavern card";
            }

            tavern.Shop[picked.Index] = null;
            TavernShopSlots.ClearSlot(tavern, picked.Index);
            picked.Card.Owner = BoardSide.Player;
            picked.Card.PoolSource = PoolSource.Copy;
            picked.Card.PoolCopiesHeld = 0;
            tavern.Hand.Add(picked.Card);
            HandleCardsAddedToHand(1, "Kidnap Sack");
            return "moved " + picked.Card.Name + " to hand";
        }

        private string CastGoldenHammer(int targetIndex)
        {
            var target = ResolveQuestSpellFriendlyTarget(targetIndex);
            if (target == null)
            {
                return "no friendly minion";
            }

            if (!target.Golden)
            {
                var attackBonus = Math.Max(0, target.Attack);
                var healthBonus = Math.Max(0, target.MaxHealth);
                MakeGoldenInPlace(target);
                target.Enchantments.Add(new Enchantment
                {
                    Id = GoldenHammerRewardId,
                    SourceId = GoldenHammerRewardId,
                    AttackBonus = attackBonus,
                    HealthBonus = healthBonus,
                    Duration = "TEMPORARY"
                });
                target.Tags.Add("quest_temporary_golden_hammer");
            }

            return target.Name + " is Golden until next turn";
        }

        private string CastTimelineAccelerator(int targetIndex)
        {
            var target = ResolveQuestSpellFriendlyTarget(targetIndex);
            if (target == null)
            {
                return "no friendly minion";
            }

            var currentIndex = State.Player.Board.IndexOf(target);
            var tier = Math.Min(TavernRules.MaxTavernTier, Math.Max(1, target.TavernTier) + 1);
            var candidates = AvailableMinions().Where(minion => minion.InPool && minion.TavernTier == tier).ToList();
            if (candidates.Count == 0)
            {
                return "no Tier " + tier + " transform target";
            }

            var rng = new SeededRng(State.Seed + State.Round * 3323 + State.Player.Tavern.RecruitLog.Count + currentIndex);
            var transformed = MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "timeline-" + State.Round + "-" + currentIndex, target.Golden, PoolSource.Copy, 0);
            State.Player.Board[currentIndex] = transformed;
            return target.Name + " transformed into " + transformed.Name;
        }

        private MinionInstance ResolveQuestSpellFriendlyTarget(int targetIndex)
        {
            if (targetIndex >= 0 && targetIndex < State.Player.Board.Count)
            {
                return State.Player.Board[targetIndex];
            }

            return State.Player.Board.FirstOrDefault();
        }

        private void ClearQuestTemporaryGoldenHammer()
        {
            foreach (var minion in State.Player.Board.Where(minion => minion?.Tags != null && minion.Tags.Contains("quest_temporary_golden_hammer")).ToList())
            {
                var enchantments = minion.Enchantments
                    .Where(enchantment => enchantment != null && string.Equals(enchantment.SourceId, GoldenHammerRewardId, StringComparison.Ordinal))
                    .ToList();
                foreach (var enchantment in enchantments)
                {
                    StatMath.ApplyStatDeltaPreservingDamage(
                        minion,
                        StatMath.SaturatingSubtract(0, enchantment.AttackBonus),
                        StatMath.SaturatingSubtract(0, enchantment.HealthBonus));
                    minion.Enchantments.Remove(enchantment);
                }

                minion.Golden = false;
                minion.Tags.Remove("quest_temporary_golden_hammer");
            }
        }

        private (int Attack, int Health) GetPerpetualIncantationBonus()
        {
            if (!HasActiveQuestReward(PerpetualIncantationRewardId))
            {
                return (0, 0);
            }

            if (!questCatalog.TryGetRewardById(PerpetualIncantationRewardId, out var reward))
            {
                return (2, 1);
            }

            return (
                GetQuestRewardCounter(reward.Id, "attack", Math.Max(1, reward.AttackBonus)),
                GetQuestRewardCounter(reward.Id, "health", Math.Max(1, reward.HealthBonus)));
        }

        private void ImprovePerpetualIncantation()
        {
            if (!HasActiveQuestReward(PerpetualIncantationRewardId) ||
                !questCatalog.TryGetRewardById(PerpetualIncantationRewardId, out var reward))
            {
                return;
            }

            var quests = EnsureQuestState(State.Player.Tavern);
            var current = GetPerpetualIncantationBonus();
            quests.RewardCounters[QuestRewardCounterKey(reward.Id, "attack")] =
                StatMath.SaturatingAdd(current.Attack, Math.Max(1, reward.AttackBonus), 0, StatMath.MaxStat);
            quests.RewardCounters[QuestRewardCounterKey(reward.Id, "health")] =
                StatMath.SaturatingAdd(current.Health, Math.Max(1, reward.HealthBonus), 0, StatMath.MaxStat);
        }

        private void ApplyTheotarsParasol(QuestRewardDefinition reward)
        {
            var target = State.Player.Board.LastOrDefault();
            if (target == null)
            {
                return;
            }

            BuffMinion(target, 0, reward.HealthBonus, reward.Name);
            if (!target.Keywords.Contains(Keyword.Stealth))
            {
                target.Keywords.Add(Keyword.Stealth);
            }

            target.Tags.Add("quest_temporary_stealth:" + reward.Id);
            AddRecruitLog(RecruitLogType.Play, reward.Name + ": protected " + target.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ClearQuestTemporaryStealth()
        {
            foreach (var minion in State.Player.Board)
            {
                if (minion.Tags == null || !minion.Tags.Any(tag => tag.StartsWith("quest_temporary_stealth:", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                minion.Tags.RemoveAll(tag => tag.StartsWith("quest_temporary_stealth:", StringComparison.OrdinalIgnoreCase));
                minion.Keywords.Remove(Keyword.Stealth);
            }
        }

        private void BuffRandomHandMinion(int attack, int health, string source)
        {
            var candidates = State.Player.Tavern.Hand.Where(card => card.CardKind == CardKind.Minion).ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var picked = new SeededRng(State.Seed + State.Round * 1543 + State.Player.Tavern.RecruitLog.Count).Pick(candidates);
            BuffMinion(picked, attack, health, source);
            AddRecruitLog(RecruitLogType.Play, source + ": buffed hand minion " + picked.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyAlterEgoBuff(QuestRewardDefinition reward, bool switchParityBeforeApplying)
        {
            var quests = EnsureQuestState(State.Player.Tavern);
            var key = QuestRewardCounterKey(reward.Id, "parity");
            var parity = GetQuestRewardCounter(reward.Id, "parity", 0);
            if (switchParityBeforeApplying)
            {
                parity = parity == 0 ? 1 : 0;
                quests.RewardCounters[key] = parity;
            }
            else if (!quests.RewardCounters.ContainsKey(key))
            {
                quests.RewardCounters[key] = parity;
            }

            var wantEven = parity == 0;
            foreach (var card in State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion))
            {
                RemoveEnchantmentsFromSource(card, reward.Name);
                if (card.TavernTier <= 0)
                {
                    continue;
                }

                var isEven = card.TavernTier % 2 == 0;
                if (isEven == wantEven)
                {
                    BuffMinion(card, reward.AttackBonus, reward.HealthBonus, reward.Name);
                }
            }
        }

        private static bool HasEnchantmentFrom(MinionInstance minion, string source)
        {
            return minion?.Enchantments != null &&
                   minion.Enchantments.Any(enchantment => string.Equals(enchantment.SourceId, source, StringComparison.OrdinalIgnoreCase));
        }

        private static void RemoveEnchantmentsFromSource(MinionInstance minion, string source)
        {
            if (minion?.Enchantments == null)
            {
                return;
            }

            var existing = minion.Enchantments
                .Where(enchantment => string.Equals(enchantment.SourceId, source, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var enchantment in existing)
            {
                StatMath.ApplyStatDeltaPreservingDamage(
                    minion,
                    StatMath.SaturatingSubtract(0, enchantment.AttackBonus),
                    StatMath.SaturatingSubtract(0, enchantment.HealthBonus));
                minion.Enchantments.Remove(enchantment);
            }
        }

        private void ApplyBloodGoblet(QuestRewardDefinition reward)
        {
            var target = State.Player.Board.LastOrDefault();
            if (target == null)
            {
                return;
            }

            var missingHealth = Math.Max(0, State.Player.MaxHealth - State.Player.Health);
            if (missingHealth > 0)
            {
                BuffMinion(target, missingHealth, 0, reward.Name);
            }
        }

        private void ApplySinfallMedallion(MinionInstance played, QuestRewardDefinition reward)
        {
            if (played == null || played.CardKind != CardKind.Minion || played.TavernTier <= 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 1877 + State.Player.Tavern.RecruitLog.Count);
            var targets = State.Player.Board
                .Where(minion => minion.InstanceId != played.InstanceId && minion.TavernTier == played.TavernTier)
                .OrderBy(_ => rng.NextInt(StatMath.MaxStat))
                .Take(Math.Max(1, reward.TargetCount))
                .ToList();
            foreach (var target in targets)
            {
                BuffMinion(target, reward.AttackBonus, reward.HealthBonus, reward.Name);
            }
        }

        private void ApplySturdyShard(QuestRewardDefinition reward)
        {
            var taunts = State.Player.Board.Count(minion => minion.Keywords.Contains(Keyword.Taunt));
            if (taunts <= 0)
            {
                return;
            }

            BuffAllMinions(
                State.Player.Board.Where(minion => !minion.Keywords.Contains(Keyword.Taunt)),
                taunts * Math.Max(1, reward.AttackBonus),
                taunts * Math.Max(1, reward.HealthBonus),
                reward.Name);
        }

        private void ApplyMapOfTheUnknown(MinionInstance played, QuestRewardDefinition reward)
        {
            if (played == null || played.CardKind != CardKind.Minion)
            {
                return;
            }

            var playedTypes = CountedTribes(played);
            if (playedTypes.Count == 0)
            {
                return;
            }

            var playedCreatedNewType = playedTypes.Any(type => State.Player.Board.Count(minion => CountedTribes(minion).Contains(type)) == 1);
            if (!playedCreatedNewType)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 2017 + State.Player.Tavern.RecruitLog.Count);
            foreach (var type in State.Player.Board.SelectMany(CountedTribes).Distinct())
            {
                var candidates = State.Player.Board.Where(minion => CountedTribes(minion).Contains(type)).ToList();
                if (candidates.Count > 0)
                {
                    BuffMinion(rng.Pick(candidates), reward.AttackBonus, reward.HealthBonus, reward.Name);
                }
            }
        }

        private static int CountFriendlyMinionTypes(IEnumerable<MinionInstance> minions)
        {
            return minions == null ? 0 : minions.SelectMany(CountedTribes).Distinct().Count();
        }

        private static List<Tribe> CountedTribes(MinionInstance minion)
        {
            if (minion?.Tribes == null)
            {
                return new List<Tribe>();
            }

            return minion.Tribes.Where(tribe => tribe != Tribe.None).Distinct().ToList();
        }

        private static bool HasTribe(MinionInstance minion, Tribe tribe)
        {
            return minion?.Tribes != null &&
                (tribe == Tribe.All || minion.Tribes.Contains(tribe) || minion.Tribes.Contains(Tribe.All));
        }

        private void ApplyBronzebeardPortrait(TrinketDefinition definition)
        {
            AddMinionByCardIdToHand(BrannBronzebeardCardId, definition.Name);
            AddRandomBattlecryMinionToHand(1, definition.Name);
            ApplyBronzebeardPortraitTribes();
        }

        private void ApplyBronzebeardPortraitTribes()
        {
            AddTribesToMatchingPlayerMinions(BrannBronzebeardCardId, Tribe.Murloc, Tribe.Dragon);
        }

        private void ApplyDrakkariPortrait(TrinketDefinition definition)
        {
            AddMinionByCardIdToHand(DrakkariEnchanterCardId, definition.Name);
            ApplyDrakkariPortraitTribes();
        }

        private void ApplyDrakkariPortraitTribes()
        {
            AddTribesToMatchingPlayerMinions(DrakkariEnchanterCardId, Tribe.Mech, Tribe.Elemental);
        }

        private void ApplyEnforcerPortrait(TrinketDefinition definition)
        {
            AddMinionByCardIdToHand(LightfangEnforcerCardId, definition.Name);
            ApplyEnforcerPortraitTypes();
        }

        private void ApplyEnforcerPortraitTypes()
        {
            SetMatchingPlayerMinionsToAllTypes(LightfangEnforcerCardId);
        }

        private void ApplyBristlebachPortrait(TrinketDefinition definition)
        {
            AddMinionByCardIdToHand(BristlebachPortraitMinionCardId, definition.Name);
        }

        private void ApplyCzarinaPortrait(TrinketDefinition definition)
        {
            AddMinionByCardIdToHand(ChargingCzarinaCardId, definition.Name);
        }

        private void ApplyDefilerPortrait(TrinketDefinition definition)
        {
            AddMinionByCardIdToHand(WoodlandDefilerCardId, definition.Name);
            ApplyTrinketShopAuras(State.Player.Tavern.Shop);
        }

        private void AddTribesToMatchingPlayerMinions(string cardId, params Tribe[] tribes)
        {
            AddTribesToMatchingMinions(State.Player.Tavern.Hand, cardId, tribes);
            if (AddTribesToMatchingMinions(State.Player.Board, cardId, tribes))
            {
                RefreshPlayerBoardTribeDistribution();
            }
        }

        private void SetMatchingPlayerMinionsToAllTypes(string cardId)
        {
            SetMatchingMinionsToAllTypes(State.Player.Tavern.Hand, cardId);
            if (SetMatchingMinionsToAllTypes(State.Player.Board, cardId))
            {
                RefreshPlayerBoardTribeDistribution();
            }
        }

        private static bool SetMatchingMinionsToAllTypes(IEnumerable<MinionInstance> minions, string cardId)
        {
            if (minions == null || string.IsNullOrWhiteSpace(cardId))
            {
                return false;
            }

            var changed = false;
            foreach (var minion in minions.Where(candidate =>
                string.Equals(candidate?.CardId, cardId, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                changed |= SetMinionToAllTypes(minion);
            }

            return changed;
        }

        private static bool SetMinionToAllTypes(MinionInstance minion)
        {
            if (minion == null)
            {
                return false;
            }

            if (minion.Tribes == null ||
                minion.Tribes.Count != 1 ||
                minion.Tribes[0] != Tribe.All)
            {
                minion.Tribes = new List<Tribe> { Tribe.All };
                return true;
            }

            return false;
        }

        private static bool AddTribesToMatchingMinions(IEnumerable<MinionInstance> minions, string cardId, params Tribe[] tribes)
        {
            if (minions == null || string.IsNullOrWhiteSpace(cardId))
            {
                return false;
            }

            var changed = false;
            foreach (var minion in minions.Where(candidate =>
                string.Equals(candidate?.CardId, cardId, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                changed |= AddTribesToMinion(minion, tribes);
            }

            return changed;
        }

        private static bool AddTribesToMinion(MinionInstance minion, IEnumerable<Tribe> tribes)
        {
            if (minion == null || tribes == null)
            {
                return false;
            }

            if (minion.Tribes == null)
            {
                minion.Tribes = new List<Tribe>();
            }

            var changed = false;
            foreach (var tribe in tribes.Where(tribe => tribe != Tribe.None).Distinct())
            {
                if (!minion.Tribes.Contains(tribe))
                {
                    minion.Tribes.Add(tribe);
                    changed = true;
                }
            }

            if (minion.Tribes.Any(tribe => tribe != Tribe.None))
            {
                changed |= minion.Tribes.RemoveAll(tribe => tribe == Tribe.None) > 0;
            }

            return changed;
        }

        private void AddEnhancedPartToHand(string source)
        {
            var cards = new[]
            {
                EnhanceAMaticTauntSpellCardId,
                EnhanceAMaticWindfurySpellCardId,
                EnhanceAMaticDivineShieldSpellCardId,
                EnhanceAMaticRebornSpellCardId
            };
            var picked = cards[new SeededRng(State.Seed + State.Round * 2221 + State.Player.Tavern.RecruitLog.Count).NextInt(cards.Length)];
            AddGeneratedSpellsToHand(picked, 1, source);
        }

        private void MarkTemporarySpellcraftCard(string cardId)
        {
            var card = State.Player.Tavern.Hand.LastOrDefault(item => item.CardId == cardId);
            if (card != null && !card.Tags.Contains("temporary_spellcraft_card"))
            {
                card.Tags.Add("temporary_spellcraft_card");
            }
        }

        private int GetQuestRewardCounter(string rewardId, string name, int fallback = 0)
        {
            var counters = EnsureQuestState(State.Player.Tavern).RewardCounters;
            return counters.TryGetValue(QuestRewardCounterKey(rewardId, name), out var value) ? value : fallback;
        }

        private static string QuestRewardCounterKey(string rewardId, string name)
        {
            return rewardId + ":" + name;
        }

        private static MechanicChoiceOption CreateTrinketChoiceOption(TrinketDefinition definition, TrinketSlotKind? targetSlotKind = null)
        {
            return new MechanicChoiceOption
            {
                OptionId = definition.CardId,
                Kind = AdvancedMechanicKind.Trinket,
                SourceId = definition.CardId,
                DisplayName = definition.Name,
                Text = definition.Text,
                ImagePath = definition.ImagePath,
                Cost = definition.Cost,
                Slot = (targetSlotKind ?? definition.SlotKind).ToString(),
                ImplementationStatus = definition.ImplementationStatus.ToString(),
                Tags = definition.Tags == null ? new List<string>() : new List<string>(definition.Tags)
            };
        }

        private void EquipTrinketFromOption(MechanicChoiceOption option)
        {
            if (option == null || string.IsNullOrEmpty(option.SourceId))
            {
                throw new InvalidOperationException("Trinket option is invalid.");
            }

            if (!trinketCatalog.TryGetByCardId(option.SourceId, out var definition))
            {
                throw new InvalidOperationException("Trinket definition does not exist: " + option.SourceId);
            }

            var targetSlotKind = ParseTrinketSlotKind(option.Slot, definition.SlotKind);
            EquipTrinket(definition, targetSlotKind);
        }

        private static TrinketSlotKind ParseTrinketSlotKind(string value, TrinketSlotKind fallback)
        {
            return Enum.TryParse(value, true, out TrinketSlotKind parsed) ? parsed : fallback;
        }

        private void EquipTrinket(TrinketDefinition definition, TrinketSlotKind? targetSlotKind = null)
        {
            var tavern = State.Player.Tavern;
            var advanced = EnsureAdvancedMechanicState(tavern);
            var trinkets = EnsureTrinketState(tavern);
            var slotKind = targetSlotKind ?? definition.SlotKind;
            if (definition.OfferPoolStatus == TrinketOfferPoolStatus.Disabled)
            {
                throw new InvalidOperationException("Disabled Trinkets cannot be equipped: " + definition.CardId);
            }

            if (slotKind != definition.SlotKind &&
                !(definition.SlotKind == TrinketSlotKind.Lesser && slotKind == TrinketSlotKind.Greater))
            {
                throw new InvalidOperationException("Unsupported Trinket slot override: " + definition.SlotKind + " into " + slotKind);
            }

            if (slotKind == TrinketSlotKind.Lesser && !string.IsNullOrEmpty(trinkets.LesserTrinketId))
            {
                throw new InvalidOperationException("A Lesser Trinket is already equipped.");
            }

            if (slotKind == TrinketSlotKind.Greater && !string.IsNullOrEmpty(trinkets.GreaterTrinketId))
            {
                throw new InvalidOperationException("A Greater Trinket is already equipped.");
            }

            if (tavern.Gold < definition.Cost)
            {
                throw new InvalidOperationException("Not enough Gold to equip Trinket.");
            }

            var before = tavern.Gold;
            SpendGold(definition.Cost);

            if (slotKind == TrinketSlotKind.Lesser)
            {
                trinkets.LesserTrinketId = definition.CardId;
            }
            else
            {
                trinkets.GreaterTrinketId = definition.CardId;
            }

            trinkets.Equipped.Add(new EquippedTrinketState
            {
                TrinketId = definition.CardId,
                Name = definition.Name,
                SlotKind = slotKind,
                EquippedRound = State.Round,
                CostPaid = definition.Cost,
                ImplementationStatus = definition.ImplementationStatus
            });
            advanced.Equipped.Add(new EquippedAdvancedMechanic
            {
                Kind = AdvancedMechanicKind.Trinket,
                SourceId = definition.CardId,
                DisplayName = definition.Name,
                Slot = slotKind.ToString(),
                EquippedRound = State.Round,
                CostPaid = definition.Cost,
                ImplementationStatus = definition.ImplementationStatus.ToString()
            });

            ApplyTrinketEquippedEffects(definition);
            AddRecruitLog(
                RecruitLogType.Play,
                "Equipped " + slotKind + " Trinket: " + definition.Name + " - " + definition.ImplementationStatus,
                before,
                tavern.Gold);

            if (definition.ImplementationStatus != TrinketImplementationStatus.Implemented)
            {
                AddRecruitLog(
                    RecruitLogType.Play,
                    "Trinket status: " + definition.Name + " - " + definition.Notes,
                    tavern.Gold,
                    tavern.Gold);
            }
        }

        private void ApplyTrinketEquippedEffects(TrinketDefinition definition)
        {
            if (definition.EffectIds == null)
            {
                return;
            }

            foreach (var effectId in definition.EffectIds)
            {
                switch (effectId)
                {
                    case "bobs_tip_jar":
                        ApplyBobsTipJar();
                        break;
                    case "ornate_clock":
                        ApplyOrnateClock(definition);
                        break;
                    case "worn_treasure_map":
                        ScheduleWornTreasureMap(definition);
                        break;
                    case "sacrificial_altar":
                        ApplySacrificialAltar(definition);
                        break;
                    case "bartend_o_trons_oilcan":
                        ReduceTavernUpgradeCost(definition.Name, 3);
                        break;
                    case "wax_imprinter":
                        TriggerWaxImprinter(definition);
                        break;
                    case "dalaran_cheese_wheel":
                        RecalculateDalaranCheeseWheelBonus();
                        ApplyTrinketShopAuras(State.Player.Tavern.Shop);
                        break;
                    case GreatBoarStickerEffectId:
                        ApplyGreatBoarSticker(definition);
                        break;
                    case DarnassusPieEffectId:
                    case DarnassusPieDoubleEffectId:
                        ApplyTrinketShopAuras(State.Player.Tavern.Shop);
                        break;
                    case "rockin_music_box":
                        AddRandomBattlecryMinionToHand(1, definition.Name);
                        break;
                    case "chromatic_tear":
                        AddRandomChromadrakesToHand(2, definition.Name);
                        break;
                    case EggOfEndtimesPortraitEffectId:
                        GrantEggOfTheEndtimes(definition);
                        break;
                    case EssenceOfDreamsEffectId:
                        AddGeneratedOrCatalogTavernSpellToHand(DreamersEmbraceCardNumber, 2, definition.Name);
                        break;
                    case ChromaticTearLesserEffectId:
                        AddRandomChromadrakesToHand(1, definition.Name);
                        break;
                    case MechaJaraxxusStickerEffectId:
                        AddRandomMagneticMechaDemonsToHand(2, definition.Name);
                        break;
                    case PrivateerPortraitEffectId:
                        AddMinionByCardIdToHand(ProudPrivateerCardId, definition.Name);
                        AddBountiesToHand(2, definition.Name);
                        break;
                    case SunkenAnchorEffectId:
                        AddBountiesToHand(2, definition.Name);
                        break;
                    case ErrglStickerEffectId:
                        AddRandomMurgletonToHand(definition.Name);
                        break;
                    case GrittyPortraitEffectId:
                        AddMinionByCardIdToHand(GrittyHeadhunterCardId, definition.Name);
                        break;
                    case JewelryBoxEffectId:
                        AddRandomJewelryBoxBloodGemToHand(definition.Name);
                        break;
                    case ConchPortraitEffectId:
                        AddGeneratedOrCatalogTavernSpellToHand(CloningConchCardNumber, 1, definition.Name);
                        break;
                    case LensCaseEffectId:
                        AddGeneratedOrCatalogTavernSpellToHand(DuplicatingLensCardNumber, 1, definition.Name);
                        break;
                    case GoldPendantEffectId:
                        ApplyGoldPendant(definition);
                        break;
                    case RendleStickerEffectId:
                        StealHighestTierTavernCard(definition);
                        break;
                    case HackerfinPortraitEffectId:
                        AddMinionByCardIdToHand(HackerfinCardId, definition.Name);
                        break;
                    case BlessingPortraitEffectId:
                        AddGeneratedOrCatalogTavernSpellToHand(NaturalBlessingCardNumber, 1, definition.Name);
                        break;
                    case WarbandWhistleEffectId:
                        ApplyWarbandWhistle(definition);
                        break;
                    case BattlecruiserPortraitEffectId:
                        AddBattlecruiserProxyToHand(definition.Name);
                        break;
                    case FelbatPortraitEffectId:
                        AddMinionByCardIdToHand(FamishedFelbatCardId, definition.Name);
                        EnsureTrinketShopMinimumCards(7, definition.Name);
                        break;
                    case NetherPendantEffectId:
                    case GlowingGauntletEffectId:
                        EnsureTrinketShopMinimumCards(7, definition.Name);
                        ApplyTrinketShopAuras(State.Player.Tavern.Shop);
                        break;
                    case GrifterPortraitEffectId:
                        AddDoubloonGrifterProxyToHand(definition.Name);
                        break;
                    case MawCasterPortraitEffectId:
                        AddMawCasterProxyToHand(definition.Name);
                        break;
                    case SafetyPatchEffectId:
                        GrantTrinketGold(5, definition.Name);
                        AddRecruitLog(RecruitLogType.Play, definition.Name + ": Ice Block is tracked as a proxy until Secrets are implemented.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                        break;
                    case ElectromagneticDeviceEffectId:
                        StartElectromagneticDeviceDiscover(definition);
                        break;
                    case InnkeepersHearthEffectId:
                        StartInnkeepersHearthDiscover(definition);
                        break;
                    case KaleidoscopeEffectId:
                        StartKaleidoscopeDiscover(definition);
                        break;
                    case JailerStickerEffectId:
                        AddTrinketSpellcraftCardToHand(JailerStickerSpellCardId, definition.Name);
                        break;
                    case DemonbloodGourdEffectId:
                        AddTrinketSpellcraftCardToHand(DemonbloodGourdSpellCardId, definition.Name);
                        break;
                    case ShakerPortraitEffectId:
                        AddMinionByCardIdToHand(ZestyShakerCardId, definition.Name);
                        break;
                    case TranscribingTypewriterEffectId:
                        SetAdvancedMechanicCounter(TypewriterCounterKey(definition), definition.SlotKind == TrinketSlotKind.Greater ? 4 : 2);
                        break;
                    case CuratorStickerEffectId:
                        ApplyCuratorSticker(definition);
                        break;
                    case SplinterOfAurumEffectId:
                        TryTriggerSplinterOfAurum(definition);
                        break;
                    case HornOfSummoningEffectId:
                        AddRandomDistinctTierMinionsToHand(1, 6, definition.Name);
                        break;
                    case MagiciansTopHatEffectId:
                        ApplyMagiciansTopHat(definition);
                        break;
                    case ShrineOfEvolutionEffectId:
                        TransformBoardIntoRandomTierMinions(4, definition.Name);
                        break;
                    case TideRaiserPortraitEffectId:
                        AddMinionByCardIdToHand(TideRaiserCardId, definition.Name);
                        break;
                    case PortableFactoryEffectId:
                        StartPortableFactoryDiscover(definition);
                        break;
                    case PromoPortraitEffectId:
                        AddMinionByCardIdToHand(PrizedPromoDrakeCardId, definition.Name);
                        break;
                    case SkyGolemPortraitEffectId:
                        AddMinionByCardIdToHand(FallenSkyGolemCardId, definition.Name);
                        break;
                    case ScrapsmithPortraitEffectId:
                        AddMinionByCardIdToHand(BristlebackScrapSmithCardId, definition.Name);
                        break;
                    case "bronzebeard_portrait":
                        ApplyBronzebeardPortrait(definition);
                        break;
                    case "drakkari_portrait":
                        ApplyDrakkariPortrait(definition);
                        break;
                    case "enforcer_portrait":
                        ApplyEnforcerPortrait(definition);
                        break;
                    case "bristlebach_portrait":
                        ApplyBristlebachPortrait(definition);
                        break;
                    case "czarina_portrait":
                        ApplyCzarinaPortrait(definition);
                        break;
                    case DefilerPortraitEffectId:
                    case DefilerPortraitGreaterEffectId:
                        ApplyDefilerPortrait(definition);
                        break;
                    case "conductor_portrait":
                        AddMinionByCardIdToHand(SnarlingConductorCardId, definition.Name);
                        break;
                    case "balladist_portrait":
                        AddMinionByCardIdToHand(BalladistCardId, definition.Name);
                        break;
                    case "baller_portrait":
                        AddTavernSpellToHand(TemperatureShiftCardNumber, definition.Name);
                        break;
                    case "explorers_binoculars":
                        ApplyExplorersBinoculars(definition);
                        break;
                    case "scraper_sticker":
                        AddRandomMagneticMechToHand(1, definition.Name);
                        break;
                    case "avalanche_sticker":
                        AddTavernSpellToHand(MountingAvalancheCardNumber, definition.Name);
                        break;
                    case "butchers_sickle":
                        AddTavernSpellToHand(ButcheringCardNumber, definition.Name);
                        break;
                    case "devourer_sticker":
                        AddTavernSpellToHand(ChannelTheDevourerCardNumber, definition.Name);
                        break;
                    case "empowerment_portrait":
                        AddTavernSpellToHand(AzeriteEmpowermentCardNumber, definition.Name);
                        break;
                    case "wisdomball_supply":
                        AddTavernSpellToHand(KnockoffWisdomballCardNumber, definition.Name);
                        break;
                    case "reflective_pendant":
                        AddPlainCopyOfRandomFriendlyMinionToHand(definition.Name);
                        break;
                    case "sellemental_portrait":
                        AddMinionByCardIdToHand(SellementalCardId, definition.Name);
                        break;
                    case "booms_monster_portrait":
                        AddMinionByCardIdToHand(DrBoomsMonsterCardId, definition.Name);
                        break;
                    case "beatboxer_portrait":
                        AddMinionByCardIdToHand(PolarizingBeatboxerCardId, definition.Name);
                        break;
                    case "morgl_portrait":
                        AddMinionByCardIdToHand(TideOracleMorglCardId, definition.Name);
                        break;
                    case "surprise_portrait":
                        AddMinionByCardIdToHand(SurpriseElementalCardId, definition.Name);
                        break;
                    case "behemoth_portrait":
                        AddMinionByCardIdToHand(ArcaneBehemothCardId, definition.Name);
                        break;
                    case "manipulator_portrait":
                        AddMinionByCardIdToHand(FacelessManipulatorCardId, definition.Name);
                        break;
                    case "poet_portrait":
                        AddMinionByCardIdToHand(TimewarpedPoetCardId, definition.Name);
                        break;
                    case "radio_star_portrait":
                        AddMinionByCardIdToHandWithKeyword(TimewarpedRadioStarCardId, definition.Name, Keyword.Reborn);
                        break;
                    case "fish_portrait":
                        AddMinionByCardIdToHand(FishOfNzothCardId, definition.Name);
                        break;
                    case "leapfrogger_portrait":
                        AddMinionByCardIdToHand(TimewarpedLeapfroggerCardId, definition.Name);
                        break;
                    case "skipper_portrait":
                        AddMinionByCardIdToHand(TimewarpedSkipperCardId, definition.Name);
                        break;
                    case "stuffed_coin_purse":
                        TryTriggerStuffedCoinPurse(definition);
                        break;
                    case "bob_blehead":
                        GrantTrinketGold(2, definition.Name);
                        break;
                    case "mysterious_orb":
                        ApplyMysteriousOrb(definition);
                        break;
                    case "book_of_medivh":
                        StartBookOfMedivhDiscover(definition);
                        break;
                    case "lavish_cape":
                        ApplyLavishCape(definition);
                        break;
                    case "pocket_cyclone":
                        ApplyPocketCyclone(definition, true);
                        break;
                    case "pagles_fishing_rod":
                        ApplyPaglesFishingRod(definition);
                        break;
                    case "heart_of_the_forest":
                        EnsureHeartOfForestBonus();
                        break;
                    case "marvelous_mushroom":
                        EnsureMarvelousMushroomBonus();
                        break;
                    case "charming_panpipes":
                        EnsureCharmingPanpipesBonus();
                        break;
                    case "dazzling_dagger":
                        ApplyDazzlingDaggerAuraToBoard();
                        break;
                    case FeralTalismanEffectId:
                    case ArtisanalUrnEffectId:
                        ApplyBoardTrinketAuras();
                        break;
                    case "primalfin_portrait":
                        AddMinionByCardIdToHand(PrimalfinLookoutCardId, definition.Name);
                        break;
                    case "azsharan_statuette":
                        AddRandomSpellcraftSpellsToHand(3, definition.Name);
                        break;
                    case "spitescale_sushi_roll":
                        AddTavernSpellToHand(SpitescaleSpecialCardNumber, definition.Name);
                        ResetSpitescaleSushiRollExtraCasts();
                        break;
                    case "precious_pearl":
                        AddTrinketSpellcraftCardToHand(PreciousPearlSpellCardId, definition.Name);
                        break;
                    case "ophidian_staff":
                        AddTrinketSpellcraftCardToHand(OphidianStaffSpellCardId, definition.Name);
                        break;
                    case "vibrant_bubble":
                        AddTrinketSpellcraftCardToHand(VibrantBubbleSpellCardId, definition.Name);
                        break;
                    case "double_stitch_needle":
                        AddTrinketSpellcraftCardToHand(DoubleStitchNeedleSpellCardId, definition.Name);
                        break;
                    case "token_of_the_old_gods":
                        AddTrinketSpellcraftCardToHand(TokenOfTheOldGodsSpellCardId, definition.Name);
                        break;
                    case "chillmere_mosaic":
                        AddTrinketSpellcraftCardToHand(ChillmereMosaicSpellCardId, definition.Name);
                        break;
                    case "eternal_portrait":
                        AddMinionByCardIdToHand(EternalKnightCardId, definition.Name);
                        break;
                    case "rivendare_portrait":
                        AddMinionByCardIdToHand(TitusRivendareCardId, definition.Name);
                        break;
                    case "bassgill_portrait":
                        AddMinionByCardIdToHand(BassgillCardId, definition.Name);
                        break;
                    case "battle_horn":
                        StartBattlecryMinionDiscover(definition);
                        break;
                    case "deathly_phylactery":
                        StartDeathrattleMinionDiscover(definition);
                        break;
                    case "rylak_portrait":
                        AddMinionByCardIdToHand(RylakMetalheadCardId, definition.Name);
                        break;
                    case "belcher_portrait":
                        AddMinionByCardIdToHand(OperaticBelcherCardId, definition.Name);
                        break;
                    case "redeemer_portrait":
                        AddMinionByCardIdToHand(NalaaCardId, definition.Name);
                        break;
                    case "azerite_portrait":
                        AddMinionByCardIdToHand(LivingAzeriteCardId, definition.Name);
                        break;
                    case "glowscale_portrait":
                        AddMinionByCardIdToHand(TimewarpedGlowscaleCardId, definition.Name);
                        break;
                    case "groundbreaker_portrait":
                        AddMinionByCardIdToHand(GroundbreakerCardId, definition.Name);
                        break;
                    case "weary_portrait":
                        AddMinionByCardIdToHand(WearyMageCardId, definition.Name);
                        break;
                    case "thaumaturgist_portrait":
                        AddMinionByCardIdToHand(ThaumaturgistCardId, definition.Name);
                        break;
                }
            }
        }

        private void ApplyBobsTipJar()
        {
            var tavern = State.Player.Tavern;
            var trinkets = EnsureTrinketState(tavern);
            var before = tavern.Gold;
            trinkets.ExtraMaxGold += 4;
            tavern.MaxGold += 4;
            tavern.Gold += 4;
            AddRecruitLog(RecruitLogType.Play, "Bob's Tip Jar: gained 4 Gold and +4 maximum Gold.", before, tavern.Gold);
        }

        private void ApplyOrnateClock(TrinketDefinition definition)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            GrantTrinketGold(2, definition.Name);
            trinkets.OrnateClockGreaterOfferRound = State.Round + 1;
            AddRecruitLog(
                RecruitLogType.Discover,
                definition.Name + ": Greater Trinket choice scheduled for round " + trinkets.OrnateClockGreaterOfferRound + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void ScheduleWornTreasureMap(TrinketDefinition definition)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.WornTreasureMapClaimed || trinkets.WornTreasureMapDueRound > 0)
            {
                return;
            }

            trinkets.WornTreasureMapDueRound = State.Round + 2;
            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": 10 Gold will be gained on round " + trinkets.WornTreasureMapDueRound + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void ApplySacrificialAltar(TrinketDefinition definition)
        {
            var removed = State.Player.Board.ToList();
            if (removed.Count == 0)
            {
                AddRecruitLog(RecruitLogType.Play, definition.Name + ": no minions to remove.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                return;
            }

            foreach (var minion in removed)
            {
                State.Player.Board.Remove(minion);
                ReleaseMinionToPool(minion);
                RecordOutsideCombatMinionDestroyed(definition.Name);
            }

            GrantTrinketGold(removed.Count * 3, definition.Name);
        }

        private void GrantTrinketGold(int amount, string source)
        {
            if (amount <= 0)
            {
                return;
            }

            var tavern = State.Player.Tavern;
            var before = tavern.Gold;
            tavern.Gold = Math.Min(StatMath.MaxStat, tavern.Gold + amount);
            AddRecruitLog(RecruitLogType.Play, source + ": gained " + amount + " Gold.", before, tavern.Gold);
            TryTriggerSplinterOfAurum();
        }

        private void ApplyMysteriousOrb(TrinketDefinition definition)
        {
            GrantTrinketGold(8, definition.Name);
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            trinkets.MysteriousOrbNextTrinketIsLesser = true;
            AddRecruitLog(
                RecruitLogType.Discover,
                definition.Name + ": your next Trinket choice will use the Lesser Trinket pool.",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void StartBookOfMedivhDiscover(TrinketDefinition definition)
        {
            var pickCount = definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
            StartTavernSpellDiscover(pickCount, "trinket:book_of_medivh:" + definition.CardId, definition.Name);
        }

        private void ApplyLavishCape(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            var typeCount = BoardTribeAnalyzer.CountDistinctTribes(State.Player.Board);
            if (typeCount <= 0)
            {
                return;
            }

            var cast = CastRandomTavernSpells(typeCount, tavern.Tier, definition.Name, "lavish-cape");
            if (cast == 0)
            {
                AddRecruitLog(
                    RecruitLogType.Play,
                    definition.Name + ": no current-tier Tavern spells are available to cast.",
                    tavern.Gold,
                    tavern.Gold);
            }
        }

        private void ApplyPocketCyclone(TrinketDefinition definition, bool onEquip)
        {
            var castCount = definition.SlotKind == TrinketSlotKind.Greater && onEquip
                ? 4
                : definition.SlotKind == TrinketSlotKind.Greater
                    ? 2
                    : 1;
            CastTavernSpellImmediate(BorrowingEastWindCardNumber, castCount, definition.Name, "pocket-cyclone");
        }

        private void ApplyExplorersBinoculars(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            var added = AddRandomTierMinionsToHand(4, 3, definition.Name);
            if (added > 0)
            {
                return;
            }

            var reason = tavern.Hand.Count >= HandLimit
                ? "hand is full"
                : "no current-pool Tier 4 minions are available";
            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": " + reason + ".",
                tavern.Gold,
                tavern.Gold);
        }

        private void ApplyPaglesFishingRod(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            var added = AddRandomTierMinionsToHand(TavernRules.MaxTavernTier, 1, definition.Name);
            if (added > 0)
            {
                return;
            }

            var reason = tavern.Hand.Count >= HandLimit
                ? "hand is full"
                : "no current-pool Tier 7 minions are available";
            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": " + reason + ".",
                tavern.Gold,
                tavern.Gold);
        }

        private bool StartTavernSpellDiscover(int pickCount, string source, string sourceName)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Discover != null)
            {
                AddRecruitLog(RecruitLogType.Discover, sourceName + ": Tavern spell Discover delayed because another Discover is pending.", tavern.Gold, tavern.Gold);
                return false;
            }

            var candidates = AvailableTavernSpells()
                .Where(spell => spell.TavernTier <= Math.Max(1, tavern.Tier))
                .ToList();
            if (candidates.Count == 0)
            {
                AddRecruitLog(RecruitLogType.Discover, sourceName + ": no Tavern spells are available to Discover.", tavern.Gold, tavern.Gold);
                return false;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3181 + tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, source + "-" + options.Count));
            }

            tavern.Discover = new DiscoverState
            {
                Source = source,
                RewardTier = Math.Max(1, tavern.Tier),
                RemainingPicks = Math.Max(1, pickCount),
                Options = options
            };
            AddRecruitLog(RecruitLogType.Discover, sourceName + ": Discover a Tavern spell.", tavern.Gold, tavern.Gold);
            return true;
        }

        private static string TypewriterCounterKey(TrinketDefinition definition)
        {
            return TypewriterRemainingCounterPrefix + (definition?.CardId ?? string.Empty);
        }

        private static string PortableFactoryCounterKey(TrinketDefinition definition)
        {
            return PortableFactoryCatalogIndexCounterPrefix + (definition?.CardId ?? string.Empty);
        }

        private void StartElectromagneticDeviceDiscover(TrinketDefinition definition, int pickCountOverride = 0)
        {
            var pickCount = pickCountOverride > 0
                ? pickCountOverride
                : definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
            var candidates = AvailableMinions()
                .Where(minion =>
                    minion.InPool &&
                    minion.Tribes.Contains(Tribe.Mech) &&
                    minion.Keywords.Contains(Keyword.Magnetic))
                .ToList();
            StartBatch4MinionDiscover(
                definition,
                "trinket:electromagnetic_device:" + definition.CardId,
                0,
                pickCount,
                candidates);
        }

        private void StartInnkeepersHearthDiscover(TrinketDefinition definition, int pickCountOverride = 0)
        {
            var isGreater = definition.SlotKind == TrinketSlotKind.Greater;
            var tier = isGreater ? 6 : Math.Max(1, State.Player.Tavern.Tier);
            var pickCount = pickCountOverride > 0 ? pickCountOverride : isGreater ? 2 : 1;
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier == tier)
                .ToList();
            StartBatch4MinionDiscover(
                definition,
                "trinket:innkeepers_hearth:" + definition.CardId,
                tier,
                pickCount,
                candidates);
        }

        private void StartKaleidoscopeDiscover(TrinketDefinition definition, int pickCountOverride = 0)
        {
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier == TavernRules.MaxTavernTier)
                .ToList();
            var tier = TavernRules.MaxTavernTier;
            if (candidates.Count == 0)
            {
                tier = AvailableMinions()
                    .Where(minion => minion.InPool)
                    .Select(minion => minion.TavernTier)
                    .DefaultIfEmpty(Math.Max(1, State.Player.Tavern.Tier))
                    .Max();
                candidates = AvailableMinions()
                    .Where(minion => minion.InPool && minion.TavernTier == tier)
                    .ToList();
            }

            StartBatch4MinionDiscover(
                definition,
                "trinket:kaleidoscope:" + definition.CardId,
                tier,
                Math.Max(1, pickCountOverride > 0 ? pickCountOverride : 1),
                candidates);
        }

        private void StartPortableFactoryDiscover(TrinketDefinition definition)
        {
            var tier = definition.SlotKind == TrinketSlotKind.Greater ? 5 : 4;
            var candidates = AvailableMinions()
                .Where(minion =>
                    minion.InPool &&
                    minion.TavernTier == tier &&
                    minion.Tribes != null &&
                    minion.Tribes.Any(tribe => tribe != Tribe.None))
                .ToList();
            StartBatch4MinionDiscover(
                definition,
                "trinket:portable_factory:" + definition.CardId,
                tier,
                1,
                candidates);
        }

        private bool StartBatch4MinionDiscover(
            TrinketDefinition definition,
            string source,
            int rewardTier,
            int pickCount,
            IEnumerable<MinionDefinition> candidates)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Discover != null)
            {
                AddRecruitLog(RecruitLogType.Discover, definition.Name + ": Discover delayed because another Discover is pending.", tavern.Gold, tavern.Gold);
                return false;
            }

            var remaining = (candidates ?? Enumerable.Empty<MinionDefinition>())
                .Where(minion => minion != null && minion.InPool)
                .ToList();
            if (remaining.Count == 0)
            {
                AddRecruitLog(RecruitLogType.Discover, definition.Name + ": no eligible minions are available to Discover.", tavern.Gold, tavern.Gold);
                return false;
            }

            var rng = new SeededRng(State.Seed + State.Round * 4211 + tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && remaining.Count > 0)
            {
                var index = rng.NextInt(remaining.Count);
                var picked = remaining[index];
                remaining.RemoveAt(index);
                options.Add(MinionFactory.Create(picked, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Discover, 0));
            }

            tavern.Discover = new DiscoverState
            {
                Source = source,
                RewardTier = rewardTier,
                RemainingPicks = Math.Max(1, pickCount),
                Options = options
            };
            AddRecruitLog(RecruitLogType.Discover, definition.Name + ": Discover a minion.", tavern.Gold, tavern.Gold);
            return true;
        }

        private bool ResolveBatch4Discover(DiscoverState discover, MinionInstance picked)
        {
            if (discover == null || picked == null || string.IsNullOrEmpty(discover.Source))
            {
                return false;
            }

            var definition = ResolveBatch4DiscoverDefinition(discover.Source);
            if (definition == null)
            {
                return false;
            }

            var card = picked;
            card.Owner = BoardSide.Player;
            card.InstanceId = "batch4-discover-" + State.Round + "-" + State.Player.Tavern.Hand.Count + "-" + State.Player.Tavern.RecruitLog.Count;
            card.PoolSource = PoolSource.Discover;
            card.OriginPoolSource = PoolSource.Discover;
            card.PoolCopiesHeld = 0;
            card.CanReturnToPoolAfterAttach = false;
            ApplyBatch4DiscoverModifiers(discover.Source, definition, card);
            DispatchDiscoverChosenEffect(discover, card);

            var remaining = Math.Max(0, discover.RemainingPicks - 1);
            State.Player.Tavern.Discover = null;
            if (State.Player.Tavern.Hand.Count < HandLimit)
            {
                State.Player.Tavern.Hand.Add(card);
                HandleCardsAddedToHand(1, definition.Name);
                DispatchQuestRewardDiscoverChosen(card);
                DispatchTrinketDiscoverChosen(card);
                AddRecruitLog(RecruitLogType.Discover, definition.Name + ": chose " + card.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }
            else
            {
                AddRecruitLog(RecruitLogType.Discover, definition.Name + ": hand is full.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }

            if (remaining > 0)
            {
                RestartBatch4Discover(discover.Source, definition, remaining);
            }

            return true;
        }

        private TrinketDefinition ResolveBatch4DiscoverDefinition(string source)
        {
            var cardId = Batch4DiscoverCardId(source, "trinket:electromagnetic_device:")
                ?? Batch4DiscoverCardId(source, "trinket:innkeepers_hearth:")
                ?? Batch4DiscoverCardId(source, "trinket:kaleidoscope:")
                ?? Batch4DiscoverCardId(source, "trinket:portable_factory:");
            if (string.IsNullOrEmpty(cardId))
            {
                return null;
            }

            return trinketCatalog != null && trinketCatalog.TryGetByCardId(cardId, out var definition)
                ? definition
                : EquippedTrinketDefinitions().FirstOrDefault(trinket => string.Equals(trinket.CardId, cardId, StringComparison.OrdinalIgnoreCase));
        }

        private static string Batch4DiscoverCardId(string source, string prefix)
        {
            return !string.IsNullOrEmpty(source) && source.StartsWith(prefix, StringComparison.Ordinal)
                ? source.Substring(prefix.Length)
                : null;
        }

        private void ApplyBatch4DiscoverModifiers(string source, TrinketDefinition definition, MinionInstance card)
        {
            if (source.StartsWith("trinket:innkeepers_hearth:", StringComparison.Ordinal))
            {
                SetMinionStats(card, definition.SlotKind == TrinketSlotKind.Greater ? 20 : 12, definition.SlotKind == TrinketSlotKind.Greater ? 20 : 12);
            }
            else if (source.StartsWith("trinket:kaleidoscope:", StringComparison.Ordinal))
            {
                if (definition.SlotKind == TrinketSlotKind.Greater)
                {
                    MakeGoldenInPlace(card);
                }

                LockCardInHand(card, 2);
            }
            else if (source.StartsWith("trinket:portable_factory:", StringComparison.Ordinal))
            {
                var catalogIndex = catalog.All.FindIndex(minion => string.Equals(minion.CardId, card.CardId, StringComparison.OrdinalIgnoreCase));
                if (catalogIndex >= 0)
                {
                    SetAdvancedMechanicCounter(PortableFactoryCounterKey(definition), catalogIndex);
                }
            }
        }

        private void RestartBatch4Discover(string source, TrinketDefinition definition, int remaining)
        {
            if (source.StartsWith("trinket:electromagnetic_device:", StringComparison.Ordinal))
            {
                StartElectromagneticDeviceDiscover(definition, remaining);
            }
            else if (source.StartsWith("trinket:innkeepers_hearth:", StringComparison.Ordinal))
            {
                StartInnkeepersHearthDiscover(definition, remaining);
            }
            else if (source.StartsWith("trinket:kaleidoscope:", StringComparison.Ordinal))
            {
                StartKaleidoscopeDiscover(definition, remaining);
            }
        }

        private static void SetMinionStats(MinionInstance card, int attack, int health)
        {
            if (card == null)
            {
                return;
            }

            card.BaseAttack = Math.Max(0, attack);
            card.BaseHealth = Math.Max(1, health);
            card.Attack = card.BaseAttack;
            card.Health = card.BaseHealth;
            card.MaxHealth = card.BaseHealth;
        }

        private static void LockCardInHand(MinionInstance card, int turns)
        {
            if (card == null || turns <= 0)
            {
                return;
            }

            card.Counters[LockedTurnsCounter] = Math.Max(turns, card.Counters.TryGetValue(LockedTurnsCounter, out var current) ? current : 0);
            if (!card.Tags.Contains("locked_in_hand"))
            {
                card.Tags.Add("locked_in_hand");
            }
        }

        private bool StartDeathrattleMinionDiscover(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Discover != null)
            {
                AddRecruitLog(RecruitLogType.Discover, definition.Name + ": Deathrattle Discover delayed because another Discover is pending.", tavern.Gold, tavern.Gold);
                return false;
            }

            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.Keywords.Contains(Keyword.Deathrattle))
                .ToList();
            if (candidates.Count == 0)
            {
                AddRecruitLog(RecruitLogType.Discover, definition.Name + ": no Deathrattle minions are available to Discover.", tavern.Gold, tavern.Gold);
                return false;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3209 + tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var minion = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(minion, BoardSide.Player, "discover:deathly_phylactery:" + definition.CardId + "-" + options.Count, false, PoolSource.Discover, 0));
            }

            tavern.Discover = new DiscoverState
            {
                Source = "trinket:deathly_phylactery:" + definition.CardId,
                RewardTier = Math.Max(1, tavern.Tier),
                RemainingPicks = 1,
                Options = options
            };
            AddRecruitLog(RecruitLogType.Discover, definition.Name + ": Discover a Deathrattle minion.", tavern.Gold, tavern.Gold);
            return true;
        }

        private bool StartBattlecryMinionDiscover(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Discover != null)
            {
                AddRecruitLog(RecruitLogType.Discover, definition.Name + ": Battlecry Discover delayed because another Discover is pending.", tavern.Gold, tavern.Gold);
                return false;
            }

            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.Keywords.Contains(Keyword.Battlecry))
                .ToList();
            if (candidates.Count == 0)
            {
                AddRecruitLog(RecruitLogType.Discover, definition.Name + ": no Battlecry minions are available to Discover.", tavern.Gold, tavern.Gold);
                return false;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3221 + tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var minion = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(minion, BoardSide.Player, "discover:battle_horn:" + definition.CardId + "-" + options.Count, false, PoolSource.Discover, 0));
            }

            tavern.Discover = new DiscoverState
            {
                Source = "trinket:battle_horn:" + definition.CardId,
                RewardTier = Math.Max(1, tavern.Tier),
                RemainingPicks = 1,
                Options = options
            };
            AddRecruitLog(RecruitLogType.Discover, definition.Name + ": Discover a Battlecry minion.", tavern.Gold, tavern.Gold);
            return true;
        }

        private void EnsureHeartOfForestBonus()
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.HeartOfForestBonusAttack <= 0)
            {
                trinkets.HeartOfForestBonusAttack = 1;
            }

            if (trinkets.HeartOfForestBonusHealth <= 0)
            {
                trinkets.HeartOfForestBonusHealth = 1;
            }
        }

        private void EnsureMarvelousMushroomBonus()
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.MarvelousMushroomBonusAttack <= 0)
            {
                trinkets.MarvelousMushroomBonusAttack = 1;
            }

            if (trinkets.MarvelousMushroomBonusHealth <= 0)
            {
                trinkets.MarvelousMushroomBonusHealth = 1;
            }
        }

        private void ImproveMarvelousMushroom(string sourceName)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            EnsureMarvelousMushroomBonus();
            trinkets.MarvelousMushroomBonusAttack = StatMath.SaturatingAdd(trinkets.MarvelousMushroomBonusAttack, 1, 0, StatMath.MaxStat);
            trinkets.MarvelousMushroomBonusHealth = StatMath.SaturatingAdd(trinkets.MarvelousMushroomBonusHealth, 1, 0, StatMath.MaxStat);
            AddRecruitLog(
                RecruitLogType.Play,
                sourceName + ": Tavern spell bonus improved to +" + trinkets.MarvelousMushroomBonusAttack + "/+" + trinkets.MarvelousMushroomBonusHealth + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void EnsureCharmingPanpipesBonus()
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.CharmingPanpipesAttack <= 0)
            {
                trinkets.CharmingPanpipesAttack = 3;
            }

            if (trinkets.CharmingPanpipesHealth <= 0)
            {
                trinkets.CharmingPanpipesHealth = 3;
            }
        }

        private void ImproveCharmingPanpipes(TrinketDefinition definition)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            EnsureCharmingPanpipesBonus();
            trinkets.CharmingPanpipesAttack = StatMath.SaturatingAdd(trinkets.CharmingPanpipesAttack, 1, 0, StatMath.MaxStat);
            trinkets.CharmingPanpipesHealth = StatMath.SaturatingAdd(trinkets.CharmingPanpipesHealth, 1, 0, StatMath.MaxStat);
            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": end-turn buff improved to +" + trinkets.CharmingPanpipesAttack + "/+" + trinkets.CharmingPanpipesHealth + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void ApplyCharmingPanpipesTurnEnd(TrinketDefinition definition)
        {
            var target = State.Player.Board.FirstOrDefault();
            if (target == null)
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            EnsureCharmingPanpipesBonus();
            BuffMinion(target, trinkets.CharmingPanpipesAttack, trinkets.CharmingPanpipesHealth, definition.Name);
        }

        private void ApplyChargingStaff(TrinketDefinition definition)
        {
            var attack = definition.SlotKind == TrinketSlotKind.Greater ? 7 : 3;
            BuffAllMinions(
                State.Player.Board.Where(minion => minion != null && minion.Keywords.Contains(Keyword.DivineShield)),
                attack,
                0,
                definition.Name);
        }

        private void ApplyGildedAnchor(TrinketDefinition definition)
        {
            var amount = definition.SlotKind == TrinketSlotKind.Greater ? 10 : 3;
            BuffAllMinions(
                State.Player.Board.Where(minion => minion != null && minion.Golden),
                amount,
                amount,
                definition.Name);
        }

        private void ApplyLorewalkerScroll(TrinketDefinition definition, MinionInstance target)
        {
            var amount = definition.SlotKind == TrinketSlotKind.Greater ? 10 : 4;
            BuffMinion(target, amount, amount, definition.Name);
        }

        private void ApplyNerglishPhrasebook(TrinketDefinition definition)
        {
            var target = State.Player.Tavern.Hand.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion);
            if (target == null)
            {
                return;
            }

            var amount = definition.SlotKind == TrinketSlotKind.Greater ? 6 : 3;
            BuffMinion(target, amount, amount, definition.Name);
        }

        private void ApplyNomiSticker(TrinketDefinition definition)
        {
            var amount = definition.SlotKind == TrinketSlotKind.Greater ? 5 : 2;
            GrowElementalsInTavernAndFuture(amount, amount, definition.Name);
        }

        private void ApplyGreatBoarSticker(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            var attack = definition.SlotKind == TrinketSlotKind.Greater ? 3 : 2;
            var health = definition.SlotKind == TrinketSlotKind.Greater ? 3 : 1;
            var count = definition.SlotKind == TrinketSlotKind.Greater ? 5 : 3;
            AddBloodGemsToHand(count, definition.Name);
            tavern.BloodGemBonusAttack = StatMath.SaturatingAdd(tavern.BloodGemBonusAttack, attack, 0, StatMath.MaxStat);
            tavern.BloodGemBonusHealth = StatMath.SaturatingAdd(tavern.BloodGemBonusHealth, health, 0, StatMath.MaxStat);
        }

        private void ApplyBluegillFlippers(TrinketDefinition definition)
        {
            var boardTarget = State.Player.Board.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion);
            BuffMinion(boardTarget, 3, 3, definition.Name);

            var handTarget = State.Player.Tavern.Hand.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion);
            BuffMinion(handTarget, 3, 3, definition.Name);
        }

        private void ApplyAuricOffering(TrinketDefinition definition)
        {
            var target = State.Player.Board.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion);
            if (target == null)
            {
                return;
            }

            var repeats = 1 + State.Player.Board.Count(card => card != null && card.CardKind == CardKind.Minion && card.Golden);
            BuffMinion(target, 4 * repeats, 3 * repeats, definition.Name);
        }

        private void ApplyToxicStinger(TrinketDefinition definition)
        {
            var candidates = State.Player.Board
                .Where(card => card != null && card.CardKind == CardKind.Minion && HasTribe(card, Tribe.Murloc))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 811 + State.Player.Tavern.RecruitLog.Count + definition.DbfId);
            var target = rng.Pick(candidates);
            BuffMinion(target, 8, 8, definition.Name);
            AddKeyword(target, Keyword.Venomous);
        }

        private void ApplyAccordOTronPortrait(TrinketDefinition definition)
        {
            var mechs = State.Player.Board
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion && HasTribe(minion, Tribe.Mech))
                .ToList();
            var targets = new List<MinionInstance>();
            var left = mechs.FirstOrDefault();
            var right = mechs.LastOrDefault();
            if (left != null)
            {
                targets.Add(left);
            }

            if (right != null && !targets.Any(target => string.Equals(target.InstanceId, right.InstanceId, StringComparison.OrdinalIgnoreCase)))
            {
                targets.Add(right);
            }

            var magnetized = 0;
            foreach (var target in targets)
            {
                var accord = CreateAccordOTronMagnetic(definition.Name + "-" + State.Round + "-" + magnetized);
                if (accord == null)
                {
                    continue;
                }

                AttachMagneticToTarget(accord, target, definition.Name);
                magnetized += 1;
            }

            if (magnetized > 0)
            {
                AddRecruitLog(
                    RecruitLogType.Play,
                    definition.Name + ": magnetized Accord-o-Tron to " + magnetized + " edge Mech(s).",
                    State.Player.Tavern.Gold,
                    State.Player.Tavern.Gold);
            }
        }

        private MinionInstance CreateAccordOTronMagnetic(string suffix)
        {
            var definition = catalog.All.FirstOrDefault(minion => string.Equals(minion.CardId, AccordOTronCardId, StringComparison.OrdinalIgnoreCase));
            var minion = definition != null
                ? MinionFactory.Create(definition, BoardSide.Player, suffix, false, PoolSource.Copy, 0)
                : CreateTrinketProxyMinion(
                    AccordOTronCardId,
                    "Accord-o-Tron",
                    "accord-o-tron",
                    suffix,
                    3,
                    3,
                    4,
                    new List<Tribe> { Tribe.Mech },
                    new List<Keyword> { Keyword.Magnetic },
                    "After you buy this, get 1 extra Gold next turn.");
            if (minion == null)
            {
                return null;
            }

            AddKeyword(minion, Keyword.Magnetic);
            return minion;
        }

        private void ApplyEnigmaticHeadstone(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            tavern.UndeadAttackBonus = StatMath.SaturatingAdd(tavern.UndeadAttackBonus, 2, 0, StatMath.MaxStat);
            AddShopGrowth(Tribe.Undead, 2, 0, definition.Name);
            BuffAllMinions(
                State.Player.Board
                    .Concat(tavern.Hand)
                    .Concat(tavern.Shop.Where(card => card != null))
                    .Where(card => HasTribe(card, Tribe.Undead)),
                2,
                0,
                definition.Name);
        }

        private void ApplyToughTuskSticker(MinionInstance target)
        {
            if (target == null || target.Keywords.Contains(Keyword.DivineShield))
            {
                return;
            }

            target.Keywords.Add(Keyword.DivineShield);
            if (!target.Tags.Contains(TemporaryToughTuskDivineShieldTag))
            {
                target.Tags.Add(TemporaryToughTuskDivineShieldTag);
            }
        }

        private int CurrentDazzlingDaggerAttack()
        {
            return 1 + (Math.Max(0, GetAdvancedMechanicCounter(AllSpellsCastThisGameCounter)) >> 2);
        }

        private void ApplyDazzlingDaggerAura(MinionInstance target)
        {
            if (target == null || target.CardKind != CardKind.Minion)
            {
                return;
            }

            SetTrackedBuff(target, DazzlingDaggerAuraSourceId, CurrentDazzlingDaggerAttack(), 0);
        }

        private void ApplyDazzlingDaggerAuraToBoard()
        {
            foreach (var minion in State.Player.Board.Where(card => card != null && card.CardKind == CardKind.Minion))
            {
                ApplyDazzlingDaggerAura(minion);
            }
        }

        private void ApplyDazzlingDaggerAuraToCombatBoard(List<MinionInstance> combatBoard)
        {
            foreach (var minion in CombatBoardMinions(combatBoard))
            {
                SetTrackedBuff(minion, DazzlingDaggerAuraSourceId, CurrentDazzlingDaggerAttack(), 0);
            }
        }

        private void ApplyBoardTrinketAuras()
        {
            ApplyBoardTrinketAuras(State.Player.Board);
        }

        private void ApplyBoardTrinketAuras(List<MinionInstance> board)
        {
            if (board == null)
            {
                return;
            }

            var feralBonus = GetFeralTalismanBonus();
            var artisanalUrnAttack = GetArtisanalUrnAttackBonus();

            foreach (var minion in board.Where(card => card != null && card.CardKind == CardKind.Minion))
            {
                ApplyOrRemoveTrackedBuff(
                    minion,
                    FeralTalismanAuraSourceId,
                    feralBonus.Attack,
                    feralBonus.Health,
                    feralBonus.Attack > 0 || feralBonus.Health > 0);
                ApplyOrRemoveTrackedBuff(
                    minion,
                    ArtisanalUrnAuraSourceId,
                    artisanalUrnAttack,
                    0,
                    artisanalUrnAttack > 0 && HasTribe(minion, Tribe.Undead));
            }
        }

        private (int Attack, int Health) GetFeralTalismanBonus()
        {
            var attack = 0;
            var health = 0;
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null || !definition.EffectIds.Contains(FeralTalismanEffectId))
                {
                    continue;
                }

                attack = StatMath.SaturatingAdd(attack, definition.SlotKind == TrinketSlotKind.Greater ? 8 : 2, 0, StatMath.MaxStat);
                health = StatMath.SaturatingAdd(health, definition.SlotKind == TrinketSlotKind.Greater ? 5 : 1, 0, StatMath.MaxStat);
            }

            return (attack, health);
        }

        private int GetArtisanalUrnAttackBonus()
        {
            var attack = 0;
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null || !definition.EffectIds.Contains(ArtisanalUrnEffectId))
                {
                    continue;
                }

                attack = StatMath.SaturatingAdd(attack, definition.SlotKind == TrinketSlotKind.Greater ? 15 : 3, 0, StatMath.MaxStat);
            }

            return attack;
        }

        private static void ApplyOrRemoveTrackedBuff(MinionInstance target, string sourceId, int attack, int health, bool shouldApply)
        {
            if (shouldApply)
            {
                SetTrackedBuff(target, sourceId, attack, health);
                return;
            }

            RemoveTrackedBuff(target, sourceId);
        }

        private void ApplyBewitchedRibbonSpellCast(TrinketDefinition definition, bool inCombat)
        {
            var amount = inCombat ? 2 : 1;
            BuffAllMinions(State.Player.Board, amount, amount, definition.Name);
        }

        private void ApplyBewitchedRibbonCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            BuffAllMinions(CombatBoardMinions(combatBoard), 2, 2, definition.Name + " combat");
        }

        private void ApplyComfyCoffin(TrinketDefinition definition)
        {
            var amount = definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
            var tavern = State.Player.Tavern;
            tavern.UndeadAttackBonus = StatMath.SaturatingAdd(tavern.UndeadAttackBonus, amount, 0, StatMath.MaxStat);
            AddShopGrowth(Tribe.Undead, amount, 0, definition.Name);
            BuffAllMinions(
                State.Player.Board
                    .Concat(tavern.Hand)
                    .Concat(tavern.Shop.Where(card => card != null))
                    .Where(card => HasTribe(card, Tribe.Undead)),
                amount,
                0,
                definition.Name);
        }

        private void ApplyMiniatureShip(TrinketDefinition definition)
        {
            BuffAllMinions(
                State.Player.Board.Where(minion => HasTribe(minion, Tribe.Pirate)),
                2,
                2,
                definition.Name);
        }

        private void ApplyBootyBayBrew(TrinketDefinition definition, int amountSpent)
        {
            var candidates = State.Player.Board
                .Where(minion => HasTribe(minion, Tribe.Pirate))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 4099 + State.Player.Tavern.RecruitLog.Count + definition.DbfId + amountSpent);
            var target = rng.Pick(candidates);
            var attack = definition.SlotKind == TrinketSlotKind.Greater ? 5 : 4;
            var health = definition.SlotKind == TrinketSlotKind.Greater ? 6 : 3;
            BuffMinion(target, attack, health, definition.Name);
        }

        private void RecordFelburnedLedgerHeroDamage()
        {
            if (!HasEquippedTrinketEffect("felburned_ledger"))
            {
                return;
            }

            var tavern = State.Player.Tavern;
            var trinkets = EnsureTrinketState(tavern);
            trinkets.FelburnedLedgerBonusThisTurn = StatMath.SaturatingAdd(
                trinkets.FelburnedLedgerBonusThisTurn,
                1,
                0,
                StatMath.MaxStat);
            AddRecruitLog(
                RecruitLogType.Play,
                "Felburned Ledger: Tavern spells give an extra +" + trinkets.FelburnedLedgerBonusThisTurn + "/+" + trinkets.FelburnedLedgerBonusThisTurn + " this turn.",
                tavern.Gold,
                tavern.Gold);
        }

        private bool TryTriggerStuffedCoinPurse(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            var trinkets = EnsureTrinketState(tavern);
            if (tavern.Tier < 6 || trinkets.StuffedCoinPurseClaimed)
            {
                return false;
            }

            if (definition == null ||
                definition.EffectIds == null ||
                !definition.EffectIds.Contains("stuffed_coin_purse"))
            {
                return false;
            }

            trinkets.StuffedCoinPurseClaimed = true;
            GrantTrinketGold(12, definition.Name);
            return true;
        }

        private void ReduceTavernUpgradeCost(string source, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var tavern = State.Player.Tavern;
            if (tavern.Tier >= TavernRules.MaxTavernTier)
            {
                tavern.UpgradeCost = 0;
                return;
            }

            var before = tavern.UpgradeCost;
            tavern.UpgradeCost = Math.Max(0, tavern.UpgradeCost - amount);
            if (tavern.UpgradeCost != before)
            {
                AddRecruitLog(RecruitLogType.Play, source + ": Tavern upgrade cost reduced by " + amount + ".", tavern.Gold, tavern.Gold);
            }
        }

        private void TriggerWaxImprinter(TrinketDefinition definition)
        {
            if (State.Player.Health <= 2)
            {
                AddRecruitLog(RecruitLogType.Play, definition.Name + ": health is too low to take 2 damage.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                return;
            }

            GrantTrinketGold(2, definition.Name);
            DamagePlayerHero(2);
        }

        private void MaybeOfferScheduledTrinketChoice()
        {
            var tavern = State.Player.Tavern;
            var advanced = EnsureAdvancedMechanicState(tavern);
            var trinkets = EnsureTrinketState(tavern);
            if (advanced.PendingChoice != null)
            {
                return;
            }

            if (MaybeOfferQuestDelayedTrinketChoice(QuaintBoutiqueRewardId, TrinketSlotKind.Lesser) ||
                MaybeOfferQuestDelayedTrinketChoice(JumboWarehouseRewardId, TrinketSlotKind.Greater))
            {
                return;
            }

            if (trinkets.OrnateClockGreaterOfferRound > 0 && trinkets.OrnateClockGreaterOfferRound <= State.Round)
            {
                trinkets.OrnateClockGreaterOfferRound = 0;
                if (string.IsNullOrEmpty(trinkets.GreaterTrinketId))
                {
                    OfferNextTrinketChoice(TrinketSlotKind.Greater, "trinket:ornate_clock");
                    return;
                }
            }

            if (State.Round == 6 && string.IsNullOrEmpty(trinkets.LesserTrinketId))
            {
                OfferTrinketChoice(TrinketSlotKind.Lesser, "turn-schedule");
                return;
            }

            if (State.Round == 9 && string.IsNullOrEmpty(trinkets.GreaterTrinketId))
            {
                OfferNextTrinketChoice(TrinketSlotKind.Greater, "turn-schedule");
            }
        }

        private void OfferNextTrinketChoice(TrinketSlotKind slotKind, string source)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (slotKind == TrinketSlotKind.Greater && trinkets.MysteriousOrbNextTrinketIsLesser)
            {
                trinkets.MysteriousOrbNextTrinketIsLesser = false;
                OfferTrinketChoice(TrinketSlotKind.Lesser, source + ":mysterious_orb", TrinketSlotKind.Greater);
                return;
            }

            OfferTrinketChoice(slotKind, source);
        }

        private bool MaybeOfferQuestDelayedTrinketChoice(string rewardId, TrinketSlotKind slotKind)
        {
            var quests = EnsureQuestState(State.Player.Tavern);
            var key = QuestRewardCounterKey(rewardId, "trinketRound");
            if (!quests.RewardCounters.TryGetValue(key, out var dueRound) || dueRound > State.Round)
            {
                return false;
            }

            quests.RewardCounters.Remove(key);
            var rewardName = rewardId;
            if (questCatalog != null && questCatalog.TryGetRewardById(rewardId, out var reward))
            {
                rewardName = reward.Name;
            }

            GrantQuestGold(4, rewardName);

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if ((slotKind == TrinketSlotKind.Lesser && !string.IsNullOrEmpty(trinkets.LesserTrinketId)) ||
                (slotKind == TrinketSlotKind.Greater && !string.IsNullOrEmpty(trinkets.GreaterTrinketId)))
            {
                AddRecruitLog(RecruitLogType.Discover, rewardName + ": " + slotKind + " Trinket slot is already filled.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                return false;
            }

            OfferNextTrinketChoice(slotKind, "quest:" + rewardId);
            return true;
        }

        private void DispatchTrinketTavernTierReached()
        {
            if (State.Player.Tavern.Tier < 6)
            {
                return;
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds != null &&
                    definition.EffectIds.Contains("stuffed_coin_purse") &&
                    TryTriggerStuffedCoinPurse(definition))
                {
                    return;
                }
            }
        }

        private void DispatchTrinketTurnEnded()
        {
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("goblin_wallet"))
                {
                    var tavern = State.Player.Tavern;
                    var trinkets = EnsureTrinketState(tavern);
                    trinkets.ExtraMaxGold += 1;
                    tavern.MaxGold += 1;
                    AddRecruitLog(RecruitLogType.Play, definition.Name + ": maximum Gold increased by 1.", tavern.Gold, tavern.Gold);
                }

                if (definition.EffectIds.Contains("aggem_sticker"))
                {
                    ApplyAggemSticker(definition);
                }

                if (definition.EffectIds.Contains("charming_panpipes"))
                {
                    ApplyCharmingPanpipesTurnEnd(definition);
                }

                if (definition.EffectIds.Contains("charging_staff"))
                {
                    ApplyChargingStaff(definition);
                }

                if (definition.EffectIds.Contains(GildedAnchorEffectId))
                {
                    ApplyGildedAnchor(definition);
                }

                if (definition.EffectIds.Contains(AuricOfferingEffectId))
                {
                    ApplyAuricOffering(definition);
                }

                if (definition.EffectIds.Contains(ToxicStingerEffectId))
                {
                    ApplyToxicStinger(definition);
                }

                if (definition.EffectIds.Contains(EnigmaticHeadstoneEffectId))
                {
                    ApplyEnigmaticHeadstone(definition);
                }

                if (definition.EffectIds.Contains(GoldenizerSupplyEffectId) &&
                    ShouldRunScheduledTrinketGrant(definition, "turn_end", 3))
                {
                    AddGeneratedOrCatalogTavernSpellToHand(GoldenizerCardNumber, 1, definition.Name);
                }

                if (definition.EffectIds.Contains(RendleStickerEffectId))
                {
                    StealHighestTierTavernCard(definition);
                }

                if (definition.EffectIds.Contains(ExquisiteDishwareEffectId))
                {
                    ApplyExquisiteDishware(definition);
                }

                if (definition.EffectIds.Contains(HackerfinPortraitEffectId))
                {
                    ApplyHackerfinPortrait(definition);
                }

                if (definition.EffectIds.Contains(WindfallPortraitEffectId))
                {
                    ApplyWindfallPortrait(definition);
                }

                if (definition.EffectIds.Contains(CliffdiverStickerEffectId))
                {
                    ApplyCliffdiverSticker(definition);
                }

                if (definition.EffectIds.Contains(MurkyStickerEffectId))
                {
                    ApplyMurkySticker(definition);
                }

                if (definition.EffectIds.Contains(AccordOTronPortraitEffectId))
                {
                    ApplyAccordOTronPortrait(definition);
                }
            }
        }

        private void ApplyAggemSticker(TrinketDefinition definition)
        {
            var targets = BoardTribeAnalyzer.SelectOneOfEachTribe(State.Player.Board);
            if (targets.Count == 0)
            {
                return;
            }

            var tavern = State.Player.Tavern;
            var attack = StatMath.SaturatingMultiply(7, 1 + tavern.BloodGemBonusAttack, 0, StatMath.MaxStat);
            var health = StatMath.SaturatingMultiply(7, 1 + tavern.BloodGemBonusHealth, 0, StatMath.MaxStat);
            foreach (var target in targets)
            {
                BuffMinion(target, attack, health, definition.Name + " Blood Gem");
            }

            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": played 7 Blood Gems on " + targets.Count + " friendly type minion(s).",
                tavern.Gold,
                tavern.Gold);
        }

        private bool ShouldRunScheduledTrinketGrant(TrinketDefinition definition, string eventName, int period)
        {
            if (definition == null || string.IsNullOrEmpty(definition.CardId) || string.IsNullOrEmpty(eventName) || period <= 1)
            {
                return true;
            }

            var key = Batch2ScheduleCounterPrefix + definition.CardId + ":" + eventName;
            var progress = IncrementAdvancedMechanicCounter(key);
            if (progress < period)
            {
                return false;
            }

            SetAdvancedMechanicCounter(key, progress % period);
            return true;
        }

        private void GrantEggOfTheEndtimes(TrinketDefinition definition)
        {
            var before = State.Player.Tavern.Hand.Count;
            AddMinionByCardIdToHand(DoomsdayDragonEggCardId, definition.Name);
            if (State.Player.Tavern.Hand.Count <= before)
            {
                return;
            }

            var egg = State.Player.Tavern.Hand.LastOrDefault();
            if (egg == null || egg.CardId != DoomsdayDragonEggCardId)
            {
                return;
            }

            if (definition.SlotKind == TrinketSlotKind.Greater)
            {
                MakeGoldenInPlace(egg);
                egg.Counters[LockedTurnsCounter] = 1;
                if (!egg.Tags.Contains("locked_in_hand"))
                {
                    egg.Tags.Add("locked_in_hand");
                }
            }
        }

        private int AddGeneratedOrCatalogTavernSpellToHand(string cardNumber, int count, string source)
        {
            if (count <= 0 || State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return 0;
            }

            var added = AddTavernSpellToHand(cardNumber, count, source);
            var remaining = count - added;
            if (remaining <= 0)
            {
                return added;
            }

            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < remaining && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(CreateGeneratedTavernSpellCard(
                    cardNumber,
                    source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
            }

            var generated = State.Player.Tavern.Hand.Count - before;
            HandleCardsAddedToHand(generated, source);
            return added + generated;
        }

        private void AddRandomMagneticMechaDemonsToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 4093 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool &&
                    minion.Keywords.Contains(Keyword.Magnetic) &&
                    minion.Tribes.Contains(Tribe.Mech) &&
                    minion.Tribes.Contains(Tribe.Demon))
                .ToList();
            AddRandomMinionsFromCandidates(candidates, count, source, rng);
        }

        private void AddRandomMurgletonToHand(string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 4099 + State.Player.Tavern.RecruitLog.Count);
            AddMinionByCardIdToHand(rng.NextInt(2) == 0 ? MurgletonAuntieCardId : MurgletonDaddyCardId, source);
        }

        private void AddRandomJewelryBoxBloodGemToHand(string source)
        {
            var gems = new[] { JewelryBoxTauntGemCardId, JewelryBoxDivineShieldGemCardId, JewelryBoxRebornGemCardId };
            var rng = new SeededRng(State.Seed + State.Round * 4111 + State.Player.Tavern.RecruitLog.Count);
            AddGeneratedOrCatalogTavernSpellToHand(gems[rng.NextInt(gems.Length)], 1, source);
        }

        private void ApplyGoldPendant(TrinketDefinition definition)
        {
            var candidates = State.Player.Board
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion && !minion.Golden && minion.TavernTier <= 4)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 4127 + State.Player.Tavern.RecruitLog.Count + definition.DbfId);
            var target = rng.Pick(candidates);
            MakeGoldenInPlace(target);
            AddRecruitLog(RecruitLogType.Triple, definition.Name + ": made " + target.Name + " Golden.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void StealHighestTierTavernCard(TrinketDefinition definition)
        {
            var candidates = State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null)
                .OrderByDescending(item => item.Card.TavernTier)
                .ThenBy(item => item.Index)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            MoveTavernCardToHand(candidates[0].Index, definition.Name);
        }

        private void StealRandomPirateTavernCard(string source)
        {
            var candidates = State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion && HasTribe(item.Card, Tribe.Pirate))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 4133 + State.Player.Tavern.RecruitLog.Count);
            MoveTavernCardToHand(rng.Pick(candidates).Index, source);
        }

        private bool MoveTavernCardToHand(int shopIndex, string source)
        {
            var tavern = State.Player.Tavern;
            if (shopIndex < 0 || shopIndex >= tavern.Shop.Count || tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var card = tavern.Shop[shopIndex];
            if (card == null)
            {
                return false;
            }

            tavern.Shop.RemoveAt(shopIndex);
            card.Owner = BoardSide.Player;
            card.InstanceId = "player-stolen-" + card.DefinitionId + "-" + State.Round + "-" + tavern.Hand.Count + "-" + tavern.RecruitLog.Count;
            card.PoolSource = PoolSource.Copy;
            card.OriginPoolSource = PoolSource.Copy;
            card.PoolCopiesHeld = 0;
            card.CanReturnToPoolAfterAttach = false;
            tavern.Hand.Add(card);
            HandleCardsAddedToHand(1, source);
            AddRecruitLog(RecruitLogType.Play, source + ": stole " + card.Name + " from the Tavern.", tavern.Gold, tavern.Gold);
            return true;
        }

        private void ApplyExquisiteDishware(TrinketDefinition definition)
        {
            var tribes = State.Player.Board
                .SelectMany(BoardTribeAnalyzer.GetCountedTribes)
                .Where(tribe => tribe != Tribe.None && tribe != Tribe.All)
                .Distinct()
                .ToList();
            if (tribes.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 4139 + State.Player.Tavern.RecruitLog.Count);
            var added = 0;
            foreach (var tribe in tribes)
            {
                var candidates = AvailableMinions()
                    .Where(minion => minion.InPool && MatchesTribe(minion, tribe))
                    .ToList();
                added += AddRandomMinionsFromCandidates(candidates, 1, definition.Name, rng);
            }

            if (added > 0)
            {
                AddRecruitLog(RecruitLogType.Play, definition.Name + ": added " + added + " typed minion(s).", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }
        }

        private void ApplyHackerfinPortrait(TrinketDefinition definition)
        {
            foreach (var hackerfin in State.Player.Board.Where(minion => minion.CardId == HackerfinCardId).ToList())
            {
                ApplyHackerfinBattlecry(hackerfin, hackerfin.Golden ? 2 : 1);
            }
        }

        private void ApplyHackerfinBattlecry(MinionInstance source, int multiplier)
        {
            var bonusKeywords = CountDifferentBonusKeywords(State.Player.Board);
            var attack = StatMath.SaturatingMultiply(1 + bonusKeywords, multiplier, 0, StatMath.MaxStat);
            var health = StatMath.SaturatingMultiply(2 + bonusKeywords, multiplier, 0, StatMath.MaxStat);
            BuffAllMinions(
                State.Player.Board.Where(minion => source == null || minion.InstanceId != source.InstanceId),
                attack,
                health,
                "Hackerfin");
        }

        private static int CountDifferentBonusKeywords(IEnumerable<MinionInstance> minions)
        {
            var bonusKeywords = new[]
            {
                Keyword.Taunt,
                Keyword.DivineShield,
                Keyword.Venomous,
                Keyword.Reborn,
                Keyword.Windfury,
                Keyword.Stealth
            };
            return minions
                .Where(minion => minion != null && minion.Keywords != null)
                .SelectMany(minion => minion.Keywords)
                .Distinct()
                .Count(keyword => bonusKeywords.Contains(keyword));
        }

        private void ApplyWindfallPortrait(TrinketDefinition definition)
        {
            var before = State.Player.Tavern.Hand.Count;
            AddMinionByCardIdToHand(WindfallTornadoCardId, definition.Name);
            if (State.Player.Tavern.Hand.Count <= before)
            {
                return;
            }

            var tornado = State.Player.Tavern.Hand.LastOrDefault();
            if (tornado == null || tornado.CardId != WindfallTornadoCardId)
            {
                return;
            }

            var baseAmount = definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
            var sold = GetAdvancedMechanicCounter(WindfallSoldThisTurnCounter);
            var amount = StatMath.SaturatingAdd(baseAmount, sold, 0, StatMath.MaxStat);
            BuffMinion(tornado, amount, amount, definition.Name);
        }

        private void ApplyCliffdiverSticker(TrinketDefinition definition)
        {
            var target = State.Player.Board.FirstOrDefault();
            if (target == null)
            {
                return;
            }

            var battlecries = GetAdvancedMechanicCounter(CliffdiverBattlecryThisTurnCounter);
            BuffMinion(
                target,
                StatMath.SaturatingAdd(3, battlecries, 0, StatMath.MaxStat),
                StatMath.SaturatingAdd(2, battlecries, 0, StatMath.MaxStat),
                definition.Name);
        }

        private void ApplyMurkySticker(TrinketDefinition definition)
        {
            var battlecries = GetAdvancedMechanicCounter(MurkyBattlecryThisGameCounter);
            var amount = StatMath.SaturatingAdd(1, battlecries, 0, StatMath.MaxStat);
            BuffAllMinions(State.Player.Board.Take(2), amount, amount, definition.Name);
        }

        private void AdvanceMarineSignet(TrinketDefinition definition)
        {
            var progress = IncrementAdvancedMechanicCounter(MarineSignetMinionCounter);
            if (progress < 4)
            {
                return;
            }

            var rewards = progress / 4;
            SetAdvancedMechanicCounter(MarineSignetMinionCounter, progress % 4);
            for (var reward = 0; reward < rewards; reward += 1)
            {
                var tier = Math.Max(1, GetAdvancedMechanicCounter(MarineSignetTierCounter, 1));
                AddRandomTavernSpellToHandExactTier(tier, 1, definition.Name);
                SetAdvancedMechanicCounter(MarineSignetTierCounter, Math.Min(TavernRules.MaxTavernTier, tier + 1));
            }
        }

        private int AddRandomTavernSpellToHandExactTier(int tier, int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 4153 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableTavernSpells()
                .Where(spell => spell.TavernTier == Math.Max(1, tier))
                .ToList();
            var before = State.Player.Tavern.Hand.Count;
            AddRandomTavernSpellsFromCandidates(candidates, count, source, rng);
            return State.Player.Tavern.Hand.Count - before;
        }

        private void ApplyJewelryBoxBloodGem(MinionInstance spell, MinionInstance target)
        {
            if (target == null || !HasTribe(target, Tribe.Quilboar))
            {
                return;
            }

            BuffMinion(target, 1 + State.Player.Tavern.BloodGemBonusAttack, 1 + State.Player.Tavern.BloodGemBonusHealth, "Jewelry Box Blood Gem");
            switch (spell.CardId)
            {
                case JewelryBoxTauntGemCardId:
                    AddKeyword(target, Keyword.Taunt);
                    break;
                case JewelryBoxDivineShieldGemCardId:
                    AddKeyword(target, Keyword.DivineShield);
                    break;
                case JewelryBoxRebornGemCardId:
                    AddKeyword(target, Keyword.Reborn);
                    break;
            }
        }

        private static bool IsJewelryBoxBloodGemSpell(MinionInstance spell)
        {
            return spell != null &&
                (spell.CardId == JewelryBoxTauntGemCardId ||
                    spell.CardId == JewelryBoxDivineShieldGemCardId ||
                    spell.CardId == JewelryBoxRebornGemCardId);
        }

        private void ApplyBlessingPortraitToHand(MinionInstance target)
        {
            var tribes = BoardTribeAnalyzer.GetCountedTribes(target);
            if (tribes.Count == 0)
            {
                return;
            }

            BuffAllMinions(
                State.Player.Tavern.Hand.Where(card => card.CardKind == CardKind.Minion && BoardTribeAnalyzer.GetCountedTribes(card).Any(tribes.Contains)),
                3,
                3,
                "Blessing Portrait");
        }

        private void ApplyWarbandWhistle(TrinketDefinition definition)
        {
            State.Player.Tavern.FreeRefreshes = StatMath.SaturatingAdd(State.Player.Tavern.FreeRefreshes, 1, 0, StatMath.MaxStat);
            SetAdvancedMechanicCounter(WarbandWhistlePendingCounter, 1);
            AddRecruitLog(RecruitLogType.Play, definition.Name + ": gained a free warband Refresh.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void AddBattlecruiserProxyToHand(string source)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            tavern.Hand.Add(CreateProxyMinion(
                BattlecruiserProxyCardId,
                "Battlecruiser",
                "Proxy Battlecruiser from Battlecruiser Portrait.",
                4,
                12,
                12,
                "battlecruiser-" + State.Round + "-" + tavern.Hand.Count,
                new[] { Tribe.None }));
            HandleCardsAddedToHand(1, source);
        }

        private void AddDoubloonGrifterProxyToHand(string source)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            tavern.Hand.Add(CreateProxyMinion(
                DoubloonGrifterProxyCardId,
                "Doubloon Grifter",
                "Proxy Doubloon Grifter from Grifter Portrait.",
                3,
                3,
                3,
                "doubloon-grifter-" + State.Round + "-" + tavern.Hand.Count,
                new[] { Tribe.Pirate }));
            HandleCardsAddedToHand(1, source);
        }

        private void AddMawCasterProxyToHand(string source)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            tavern.Hand.Add(CreateProxyMinion(
                MawCasterProxyCardId,
                "Maw Caster",
                "Whenever you destroy a minion outside combat, get a 3-Gold Coin Pouch.",
                4,
                4,
                4,
                "maw-caster-" + State.Round + "-" + tavern.Hand.Count,
                new[] { Tribe.None }));
            HandleCardsAddedToHand(1, source);
        }

        private void RecordOutsideCombatMinionDestroyed(string source)
        {
            var definition = EquippedTrinketDefinitions().FirstOrDefault(trinket =>
                trinket.EffectIds != null && trinket.EffectIds.Contains(MawCasterPortraitEffectId));
            if (definition == null || State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            State.Player.Tavern.Hand.Add(CreateGeneratedTavernSpellCard(
                CoinPouch3GoldProxyCardId,
                "3-Gold Coin Pouch",
                "Gain 3 Gold.",
                0,
                0,
                "maw-caster-coin-pouch-" + State.Round + "-" + State.Player.Tavern.Hand.Count,
                "coin_pouch",
                "trinket_proxy"));
            HandleCardsAddedToHand(1, source ?? definition.Name);
        }

        private static MinionInstance CreateProxyMinion(
            string cardId,
            string name,
            string text,
            int tier,
            int attack,
            int health,
            string suffix,
            IEnumerable<Tribe> tribes,
            params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "proxy-" + cardId + "-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = BuyCost,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = tier,
                Tribes = tribes == null ? new List<Tribe> { Tribe.None } : new List<Tribe>(tribes),
                Keywords = keywords == null ? new List<Keyword>() : new List<Keyword>(keywords),
                OfficialKeywords = keywords == null ? new List<Keyword>() : new List<Keyword>(keywords),
                Text = text,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                OriginPoolSource = PoolSource.Copy,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "trinket_proxy", "generated_minion" }
            };
        }

        private void EnsureTrinketShopMinimumCards(int desiredCards, string source)
        {
            if (desiredCards <= 0)
            {
                return;
            }

            var tavern = State.Player.Tavern;
            TavernShopSlots.Ensure(tavern);
            var currentCards = tavern.Shop.Count(card => card != null);
            var missing = Math.Max(0, desiredCards - currentCards);
            if (missing <= 0)
            {
                return;
            }

            var drawn = CreateShopFromPool(
                tavern.Pool,
                tavern.Tier,
                missing,
                State.Seed + State.Round * 6211 + tavern.RecruitLog.Count + currentCards,
                "trinket-min-shop-" + State.Round + "-" + currentCards,
                includeTavernSpell: false,
                minimumTier: GetCurrentShopMinimumTier());
            tavern.Pool = drawn.Pool;
            tavern.Shop.AddRange(drawn.Shop);
            TavernShopSlots.Ensure(tavern);
            AddRecruitLog(RecruitLogType.Reroll, source + ": filled the Tavern to " + desiredCards + " cards.", tavern.Gold, tavern.Gold);
        }

        private int GetTrinketMinimumShopCards()
        {
            return HasEquippedTrinketEffect(FelbatPortraitEffectId) || HasEquippedTrinketEffect(GlowingGauntletEffectId)
                ? 7
                : 0;
        }

        private void ApplyTrinketRefreshResultModifiers(List<MinionInstance> shop)
        {
            if (shop == null)
            {
                return;
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains(WarbandWhistleEffectId))
                {
                    ApplyWarbandWhistleRefresh(shop, definition);
                }

                if (definition.EffectIds.Contains(ElectrodeAttractorEffectId))
                {
                    AddRandomMagneticMechToShop(shop, definition);
                }

                if (definition.EffectIds.Contains(InnkeepersSteinEffectId))
                {
                    AddRandomHigherTierMinionToShop(shop, definition);
                }

                if (definition.EffectIds.Contains(BattlecruiserPortraitEffectId))
                {
                    AddBattlecruiserUpgradeToShop(shop, definition);
                }
            }

            EnsureTrinketShopMinimumCards(GetTrinketMinimumShopCards(), "Trinkets");

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds != null && definition.EffectIds.Contains(GuidingCandleEffectId))
                {
                    ApplyGuidingCandleRefresh(shop, definition);
                }
            }

            ApplyTrinketShopCostDisplays(shop);
        }

        private void ApplyWarbandWhistleRefresh(List<MinionInstance> shop, TrinketDefinition definition)
        {
            if (GetAdvancedMechanicCounter(WarbandWhistlePendingCounter) <= 0)
            {
                return;
            }

            SetAdvancedMechanicCounter(WarbandWhistlePendingCounter, 0);
            var tavern = State.Player.Tavern;
            TavernShopSlots.Ensure(tavern);
            var copies = State.Player.Board
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion)
                .Select((minion, index) => CreatePlainCopy(minion, "warband-whistle-" + State.Round + "-" + index))
                .ToList();
            if (copies.Count == 0)
            {
                AddRecruitLog(RecruitLogType.Reroll, definition.Name + ": no warband minions to copy.", tavern.Gold, tavern.Gold);
                return;
            }

            var copyIndex = 0;
            for (var index = 0; index < shop.Count && copyIndex < copies.Count; index += 1)
            {
                if (TavernShopSlots.IsSlotFrozen(tavern, index))
                {
                    continue;
                }

                shop[index] = copies[copyIndex];
                copyIndex += 1;
            }

            while (copyIndex < copies.Count && shop.Count < 7)
            {
                shop.Add(copies[copyIndex]);
                copyIndex += 1;
            }

            TavernShopSlots.Ensure(tavern);
            AddRecruitLog(RecruitLogType.Reroll, definition.Name + ": refreshed with " + copyIndex + " plain warband copie(s).", tavern.Gold, tavern.Gold);
        }

        private void AddRandomMagneticMechToShop(List<MinionInstance> shop, TrinketDefinition definition)
        {
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool &&
                    minion.Keywords.Contains(Keyword.Magnetic) &&
                    minion.Tribes.Contains(Tribe.Mech))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 6221 + State.Player.Tavern.RecruitLog.Count + definition.DbfId);
            var minion = MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "electrode-" + State.Round + "-" + shop.Count, false, PoolSource.Pool, 1);
            minion.Cost = 2;
            shop.Add(minion);
            TavernShopSlots.Ensure(State.Player.Tavern);
        }

        private void AddRandomHigherTierMinionToShop(List<MinionInstance> shop, TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier > tavern.Tier && minion.TavernTier <= TavernRules.MaxTavernTier)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 6229 + tavern.RecruitLog.Count + definition.DbfId);
            shop.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "innkeepers-stein-" + State.Round + "-" + shop.Count, false, PoolSource.Pool, 1));
            TavernShopSlots.Ensure(tavern);
        }

        private void AddBattlecruiserUpgradeToShop(List<MinionInstance> shop, TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            shop.Add(CreateGeneratedTavernSpellCard(
                BattlecruiserUpgradeProxyCardId,
                "Battlecruiser Upgrade",
                "Proxy upgrade for Battlecruiser Portrait.",
                0,
                Math.Max(1, tavern.Tier),
                "battlecruiser-upgrade-" + State.Round + "-" + shop.Count,
                "battlecruiser_upgrade",
                "trinket_proxy"));
            TavernShopSlots.Ensure(tavern);
        }

        private void ApplyGuidingCandleRefresh(List<MinionInstance> shop, TrinketDefinition definition)
        {
            ResetRoundCounter(GuidingCandleRoundCounter, GuidingCandleRefreshesCounter);
            var used = GetAdvancedMechanicCounter(GuidingCandleRefreshesCounter);
            if (used >= 2)
            {
                return;
            }

            SetAdvancedMechanicCounter(GuidingCandleRefreshesCounter, used + 1);
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier == 6)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var tavern = State.Player.Tavern;
            var rng = new SeededRng(State.Seed + State.Round * 6239 + tavern.RecruitLog.Count + definition.DbfId + used);
            TavernShopSlots.Ensure(tavern);
            for (var index = 0; index < shop.Count; index += 1)
            {
                if (TavernShopSlots.IsSlotFrozen(tavern, index) || shop[index]?.CardKind != CardKind.Minion)
                {
                    continue;
                }

                shop[index] = MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, "guiding-candle-" + State.Round + "-" + index, false, PoolSource.Pool, 1);
            }

            TavernShopSlots.Ensure(tavern);
        }

        private void ApplyUpstartEmbersRefresh(List<MinionInstance> shop, TrinketDefinition definition)
        {
            var target = shop?
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.MaxHealth)
                .ThenByDescending(card => card.Attack)
                .ThenBy(card => card.InstanceId)
                .FirstOrDefault();
            if (target == null)
            {
                return;
            }

            BuffMinion(target, Math.Max(0, target.Attack), Math.Max(0, target.MaxHealth), definition.Name);
        }

        private void ApplyDemonicTapestryRefresh(List<MinionInstance> shop, TrinketDefinition definition)
        {
            if (shop == null)
            {
                return;
            }

            foreach (var card in shop.Where(card => card?.Tags != null))
            {
                card.Tags.RemoveAll(tag => string.Equals(tag, DemonicTapestryHealthCostTag, StringComparison.OrdinalIgnoreCase));
            }

            var progress = IncrementAdvancedMechanicCounter(DemonicTapestryRefreshCounter);
            if (progress < 4)
            {
                return;
            }

            SetAdvancedMechanicCounter(DemonicTapestryRefreshCounter, progress % 4);
            var target = shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.TavernTier)
                .ThenByDescending(card => card.Attack + card.MaxHealth)
                .ThenBy(card => card.InstanceId)
                .FirstOrDefault();
            if (target == null)
            {
                return;
            }

            if (target.Tags == null)
            {
                target.Tags = new List<string>();
            }

            if (!target.Tags.Contains(DemonicTapestryHealthCostTag))
            {
                target.Tags.Add(DemonicTapestryHealthCostTag);
            }

            AddRecruitLog(RecruitLogType.Reroll, definition.Name + ": " + target.Name + " costs Health this refresh.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void ApplyFinleysHelmetRefresh(List<MinionInstance> shop, TrinketDefinition definition)
        {
            var targets = shop?
                .Where(card => card != null && card.CardKind == CardKind.Minion && HasTribe(card, Tribe.Murloc))
                .ToList();
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 6247 + State.Player.Tavern.RecruitLog.Count + definition.DbfId);
            foreach (var target in targets)
            {
                BuffMinion(target, 5, 5, definition.Name);
                AddKeyword(target, PickFinleyBonusKeyword(rng));
            }
        }

        private static Keyword PickFinleyBonusKeyword(SeededRng rng)
        {
            var keywords = new[]
            {
                Keyword.Taunt,
                Keyword.DivineShield,
                Keyword.Windfury,
                Keyword.Reborn,
                Keyword.Venomous
            };
            return rng.Pick(keywords);
        }

        private void ResetRoundCounter(string roundKey, string valueKey)
        {
            if (GetAdvancedMechanicCounter(roundKey) == State.Round)
            {
                return;
            }

            SetAdvancedMechanicCounter(roundKey, State.Round);
            SetAdvancedMechanicCounter(valueKey, 0);
        }

        private void DispatchTrinketTurnStarted()
        {
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains(SplinterOfAurumEffectId))
                {
                    TryTriggerSplinterOfAurum(definition);
                }

                if (definition.EffectIds.Contains(PortableFactoryEffectId))
                {
                    AddPortableFactoryCopy(definition);
                }

                if (definition.EffectIds.Contains("worn_treasure_map"))
                {
                    TryClaimWornTreasureMap(definition);
                }

                if (definition.EffectIds.Contains("bartend_o_trons_oilcan"))
                {
                    ReduceTavernUpgradeCost(definition.Name, 3);
                }

                if (definition.EffectIds.Contains("wax_imprinter"))
                {
                    TriggerWaxImprinter(definition);
                }

                if (definition.EffectIds.Contains("rockin_music_box"))
                {
                    AddRandomBattlecryMinionToHand(1, definition.Name);
                }

                if (definition.EffectIds.Contains("balladist_portrait"))
                {
                    AddMinionByCardIdToHand(BalladistCardId, definition.Name);
                }

                if (definition.EffectIds.Contains("butchers_sickle"))
                {
                    AddTavernSpellToHand(ButcheringCardNumber, definition.Name);
                }

                if (definition.EffectIds.Contains("devourer_sticker"))
                {
                    AddTavernSpellToHand(ChannelTheDevourerCardNumber, definition.Name);
                }

                if (definition.EffectIds.Contains("empowerment_portrait"))
                {
                    AddTavernSpellToHand(AzeriteEmpowermentCardNumber, definition.Name);
                }

                if (definition.EffectIds.Contains("wisdomball_supply"))
                {
                    AddTavernSpellToHand(KnockoffWisdomballCardNumber, definition.Name);
                }

                if (definition.EffectIds.Contains("scraper_sticker"))
                {
                    AddRandomMagneticMechToHand(1, definition.Name);
                }

                if (definition.EffectIds.Contains("reflective_pendant"))
                {
                    AddPlainCopyOfRandomFriendlyMinionToHand(definition.Name);
                }

                if (definition.EffectIds.Contains("sellemental_portrait"))
                {
                    AddMinionByCardIdToHand(SellementalCardId, definition.Name);
                }

                if (definition.EffectIds.Contains("booms_monster_portrait"))
                {
                    AddMinionByCardIdToHand(DrBoomsMonsterCardId, definition.Name);
                }

                if (definition.EffectIds.Contains("book_of_medivh"))
                {
                    StartBookOfMedivhDiscover(definition);
                }

                if (definition.EffectIds.Contains("lavish_cape"))
                {
                    ApplyLavishCape(definition);
                }

                if (definition.EffectIds.Contains("pocket_cyclone"))
                {
                    ApplyPocketCyclone(definition, false);
                }

                if (definition.EffectIds.Contains("pagles_fishing_rod"))
                {
                    ApplyPaglesFishingRod(definition);
                }

                if (definition.EffectIds.Contains(EggOfEndtimesPortraitEffectId) &&
                    definition.SlotKind == TrinketSlotKind.Lesser &&
                    ShouldRunScheduledTrinketGrant(definition, "turn_start", 2))
                {
                    GrantEggOfTheEndtimes(definition);
                }

                if (definition.EffectIds.Contains(EssenceOfDreamsEffectId))
                {
                    AddGeneratedOrCatalogTavernSpellToHand(DreamersEmbraceCardNumber, 1, definition.Name);
                }

                if (definition.EffectIds.Contains(ChromaticTearLesserEffectId))
                {
                    AddRandomChromadrakesToHand(1, definition.Name);
                }

                if (definition.EffectIds.Contains(MechaJaraxxusStickerEffectId))
                {
                    AddRandomMagneticMechaDemonsToHand(2, definition.Name);
                }

                if (definition.EffectIds.Contains(PrivateerPortraitEffectId) ||
                    definition.EffectIds.Contains(SunkenAnchorEffectId))
                {
                    AddBountiesToHand(2, definition.Name);
                }

                if (definition.EffectIds.Contains(ErrglStickerEffectId))
                {
                    AddRandomMurgletonToHand(definition.Name);
                }

                if (definition.EffectIds.Contains(GrittyPortraitEffectId))
                {
                    AddMinionByCardIdToHand(GrittyHeadhunterCardId, definition.Name);
                }

                if (definition.EffectIds.Contains(JewelryBoxEffectId))
                {
                    AddRandomJewelryBoxBloodGemToHand(definition.Name);
                }

                if (definition.EffectIds.Contains(ConchPortraitEffectId) &&
                    ShouldRunScheduledTrinketGrant(definition, "turn_start", 2))
                {
                    AddGeneratedOrCatalogTavernSpellToHand(CloningConchCardNumber, 1, definition.Name);
                }

                if (definition.EffectIds.Contains(LensCaseEffectId) &&
                    ShouldRunScheduledTrinketGrant(definition, "turn_start", 2))
                {
                    AddGeneratedOrCatalogTavernSpellToHand(DuplicatingLensCardNumber, 1, definition.Name);
                }

                if (definition.EffectIds.Contains(AzerothModelGlobeEffectId) &&
                    ShouldRunScheduledTrinketGrant(definition, "turn_start", 2))
                {
                    GrantTrinketGold(2, definition.Name);
                    StartTierDiscover(6, definition.Name);
                }

                if (definition.EffectIds.Contains(GoldPendantEffectId))
                {
                    ApplyGoldPendant(definition);
                }

                if (definition.EffectIds.Contains(BlessingPortraitEffectId))
                {
                    AddGeneratedOrCatalogTavernSpellToHand(NaturalBlessingCardNumber, 1, definition.Name);
                }

                if (definition.EffectIds.Contains("marvelous_mushroom"))
                {
                    ImproveMarvelousMushroom(definition.Name);
                }

                if (definition.EffectIds.Contains("azsharan_statuette"))
                {
                    AddRandomSpellcraftSpellsToHand(3, definition.Name);
                }

                if (definition.EffectIds.Contains("spitescale_sushi_roll"))
                {
                    ResetSpitescaleSushiRollExtraCasts();
                }

                if (definition.EffectIds.Contains("precious_pearl"))
                {
                    AddTrinketSpellcraftCardToHand(PreciousPearlSpellCardId, definition.Name);
                }

                if (definition.EffectIds.Contains("ophidian_staff"))
                {
                    AddTrinketSpellcraftCardToHand(OphidianStaffSpellCardId, definition.Name);
                }

                if (definition.EffectIds.Contains("vibrant_bubble"))
                {
                    AddTrinketSpellcraftCardToHand(VibrantBubbleSpellCardId, definition.Name);
                }

                if (definition.EffectIds.Contains("double_stitch_needle"))
                {
                    AddTrinketSpellcraftCardToHand(DoubleStitchNeedleSpellCardId, definition.Name);
                }

                if (definition.EffectIds.Contains("token_of_the_old_gods"))
                {
                    AddTrinketSpellcraftCardToHand(TokenOfTheOldGodsSpellCardId, definition.Name);
                }

                if (definition.EffectIds.Contains("chillmere_mosaic"))
                {
                    AddTrinketSpellcraftCardToHand(ChillmereMosaicSpellCardId, definition.Name);
                }
            }
        }

        private void TryClaimWornTreasureMap(TrinketDefinition definition)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.WornTreasureMapClaimed ||
                trinkets.WornTreasureMapDueRound <= 0 ||
                trinkets.WornTreasureMapDueRound > State.Round)
            {
                return;
            }

            trinkets.WornTreasureMapClaimed = true;
            trinkets.WornTreasureMapDueRound = 0;
            GrantTrinketGold(10, definition.Name);
        }

        private void DispatchTrinketCardBought(MinionInstance bought)
        {
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("kodo_leather_pouch"))
                {
                    var attack = definition.SlotKind == TrinketSlotKind.Greater ? 4 : 2;
                    var health = definition.SlotKind == TrinketSlotKind.Greater ? 4 : 1;
                    BuffRandomFriendlyMinionsFromTrinket(definition, 2, attack, health);
                }

                if (definition.EffectIds.Contains("shaman_prayer_beads"))
                {
                    ApplyShamanPrayerBeads(bought, definition);
                }

                if (definition.EffectIds.Contains("reusable_batteries"))
                {
                    ApplyReusableBatteries(bought, definition);
                }

                if (definition.EffectIds.Contains("peacebloom_candle") && bought != null && bought.CardKind == CardKind.TavernSpell)
                {
                    RecordPeacebloomCandleBuy(definition.Name);
                }

                if (definition.EffectIds.Contains("lubber_sticker") && bought != null && bought.CardKind == CardKind.TavernSpell)
                {
                    RecordLubberStickerTavernSpellBuy(definition.Name);
                }

                if (definition.EffectIds.Contains(MagicfinStickerEffectId) && bought != null && bought.CardKind == CardKind.TavernSpell)
                {
                    AddMagicfinTaughtMurloc(bought, definition);
                }

                if (definition.EffectIds.Contains(TranscribingTypewriterEffectId))
                {
                    CopyBoughtMinionForTypewriter(bought, definition);
                }
            }
        }

        private void ApplyShamanPrayerBeads(MinionInstance bought, TrinketDefinition definition)
        {
            if (bought == null || bought.CardKind != CardKind.Minion || !bought.Keywords.Contains(Keyword.Battlecry))
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            trinkets.ShamanPrayerBeadsBattlecryBuys += 1;
            if (trinkets.ShamanPrayerBeadsBattlecryBuys < 2)
            {
                AddRecruitLog(RecruitLogType.Play, definition.Name + ": 1 Battlecry minion left.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                return;
            }

            trinkets.ShamanPrayerBeadsBattlecryBuys = 0;
            AddRandomBattlecryMinionToHand(1, definition.Name);
        }

        private void ApplyReusableBatteries(MinionInstance bought, TrinketDefinition definition)
        {
            if (bought == null || bought.CardKind != CardKind.Minion)
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.ReusableBatteriesLastTriggerRound == State.Round)
            {
                return;
            }

            trinkets.ReusableBatteriesLastTriggerRound = State.Round;
            AddMagneticSatellitesToHand(1, Math.Max(0, bought.Attack), Math.Max(1, bought.MaxHealth), definition.Name);
        }

        private void AddMagicfinTaughtMurloc(MinionInstance bought, TrinketDefinition definition)
        {
            ResetRoundCounter(MagicfinRoundCounter, MagicfinUsesCounter);
            var used = GetAdvancedMechanicCounter(MagicfinUsesCounter);
            if (used >= 2 || State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            SetAdvancedMechanicCounter(MagicfinUsesCounter, used + 1);
            var suffix = "magicfin-" + State.Round + "-" + used + "-" + State.Player.Tavern.Hand.Count;
            var murloc = CreateProxyMinion(
                MagicfinMurlocProxyCardId,
                "Taught Murloc",
                "1/1 Murloc taught " + bought.Name + ".",
                1,
                1,
                1,
                suffix,
                new[] { Tribe.Murloc });
            murloc.Tags.Add(TaughtTavernSpellTagPrefix + bought.CardId);
            murloc.Tags.Add("magicfin_taught_murloc");
            State.Player.Tavern.Hand.Add(murloc);
            HandleCardsAddedToHand(1, definition.Name);
        }

        private void CopyBoughtMinionForTypewriter(MinionInstance bought, TrinketDefinition definition)
        {
            if (bought == null || bought.CardKind != CardKind.Minion || State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var key = TypewriterCounterKey(definition);
            var remaining = GetAdvancedMechanicCounter(key);
            if (remaining <= 0)
            {
                return;
            }

            var copy = bought.Clone();
            copy.InstanceId = "typewriter-copy-" + definition.CardId + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count;
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Copy;
            copy.OriginPoolSource = PoolSource.Copy;
            copy.PoolCopiesHeld = 0;
            copy.CanReturnToPoolAfterAttach = false;
            State.Player.Tavern.Hand.Add(copy);
            SetAdvancedMechanicCounter(key, Math.Max(0, remaining - 1));
            HandleCardsAddedToHand(1, definition.Name);
            AddRecruitLog(RecruitLogType.Play, definition.Name + ": copied " + bought.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void AddPortableFactoryCopy(TrinketDefinition definition)
        {
            if (State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var catalogIndex = GetAdvancedMechanicCounter(PortableFactoryCounterKey(definition), -1);
            if (catalogIndex < 0 || catalogIndex >= catalog.All.Count)
            {
                return;
            }

            var stored = catalog.All[catalogIndex];
            if (stored == null)
            {
                return;
            }

            State.Player.Tavern.Hand.Add(MinionFactory.Create(
                stored,
                BoardSide.Player,
                "portable-factory-" + definition.CardId + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count,
                false,
                PoolSource.Copy,
                0));
            HandleCardsAddedToHand(1, definition.Name);
            AddRecruitLog(RecruitLogType.Play, definition.Name + ": added a copy of " + stored.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void RecordPeacebloomCandleBuy(string sourceName)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.PeacebloomCandleRound != State.Round)
            {
                trinkets.PeacebloomCandleRound = State.Round;
                trinkets.PeacebloomCandleBuysThisRound = 0;
            }

            if (trinkets.PeacebloomCandleBuysThisRound >= 3)
            {
                return;
            }

            trinkets.PeacebloomCandleBuysThisRound += 1;
            AddRecruitLog(
                RecruitLogType.Play,
                sourceName + ": " + Math.Max(0, 3 - trinkets.PeacebloomCandleBuysThisRound) + " free Tavern spell buy(s) left this turn.",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void RecordLubberStickerTavernSpellBuy(string sourceName)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.LubberStickerRound != State.Round)
            {
                trinkets.LubberStickerRound = State.Round;
                trinkets.LubberStickerTavernSpellBuysThisRound = 0;
            }

            trinkets.LubberStickerTavernSpellBuysThisRound = StatMath.SaturatingAdd(
                trinkets.LubberStickerTavernSpellBuysThisRound,
                1,
                0,
                StatMath.MaxStat);
            AddRecruitLog(
                RecruitLogType.Play,
                sourceName + ": first Tavern spell discount used for this turn.",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void DispatchTrinketCardDiscarded(MinionInstance discarded)
        {
            if (discarded == null)
            {
                return;
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("conductor_portrait"))
                {
                    ApplyConductorPortraitDiscard(definition);
                }
            }
        }

        private void ApplyConductorPortraitDiscard(TrinketDefinition definition)
        {
            var targetCount = State.Player.Board.Count;
            ApplyBloodGemToAllFriendlyMinions(definition.Name);
            if (targetCount > 0)
            {
                AddRecruitLog(
                    RecruitLogType.Play,
                    definition.Name + ": played a Blood Gem on all friendly minions after a discard.",
                    State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
            }
        }

        private void DispatchTrinketMagnetized(MinionInstance magnetic, MinionInstance target)
        {
            if (magnetic == null || target == null)
            {
                return;
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("copper_coil"))
                {
                    ApplyCopperCoil(definition, target);
                }

                if (definition.EffectIds.Contains(ElectromagneticDeviceEffectId))
                {
                    var bonus = definition.SlotKind == TrinketSlotKind.Greater ? 4 : 3;
                    BuffMinion(target, bonus, bonus, definition.Name);
                    AddRecruitLog(
                        RecruitLogType.Play,
                        definition.Name + ": magnetized target gained +" + bonus + "/+" + bonus + ".",
                        State.Player.Tavern.Gold,
                        State.Player.Tavern.Gold);
                }
            }
        }

        private void DispatchTrinketMagneticMinionPlayed(MinionInstance magnetic)
        {
            if (magnetic == null || !magnetic.Keywords.Contains(Keyword.Magnetic))
            {
                return;
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds != null && definition.EffectIds.Contains(SpellPoweredWrenchEffectId))
                {
                    AddRandomTavernSpellToHand(State.Player.Tavern.Tier, 1, definition.Name);
                }
            }
        }

        private void ApplyCopperCoil(TrinketDefinition definition, MinionInstance target)
        {
            var baseAttack = definition.SlotKind == TrinketSlotKind.Greater ? 3 : 1;
            var baseHealth = definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
            var attackKey = CopperCoilCounterKey(definition, "attack");
            var healthKey = CopperCoilCounterKey(definition, "health");
            var attack = Math.Max(baseAttack, GetAdvancedMechanicCounter(attackKey, baseAttack));
            var health = Math.Max(baseHealth, GetAdvancedMechanicCounter(healthKey, baseHealth));

            BuffMinion(target, attack, health, definition.Name);
            SetAdvancedMechanicCounter(attackKey, StatMath.SaturatingAdd(attack, baseAttack, 0, StatMath.MaxStat));
            SetAdvancedMechanicCounter(healthKey, StatMath.SaturatingAdd(health, baseHealth, 0, StatMath.MaxStat));
            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": magnetized minion gained +" + attack + "/+" + health + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private static string CopperCoilCounterKey(TrinketDefinition definition, string stat)
        {
            return CopperCoilCounterPrefix + (definition?.CardId ?? "unknown") + ":" + stat;
        }

        private void DispatchTrinketMinionPlayed(MinionInstance played)
        {
            if (played == null || played.CardKind != CardKind.Minion)
            {
                return;
            }

            DispatchTrinketCardPlayed(played);

            if (played.Keywords.Contains(Keyword.Battlecry))
            {
                var battlecryTriggers = Math.Max(1, GetBattlecryRepeats(played));
                if (HasEquippedTrinketEffect(CliffdiverStickerEffectId))
                {
                    IncrementAdvancedMechanicCounter(CliffdiverBattlecryThisTurnCounter, battlecryTriggers);
                }

                if (HasEquippedTrinketEffect(MurkyStickerEffectId))
                {
                    IncrementAdvancedMechanicCounter(MurkyBattlecryThisGameCounter, battlecryTriggers);
                }
            }

            var refreshBoardAuras = false;
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("water_wheel"))
                {
                    ApplyWaterWheel(played, definition);
                }

                if (definition.EffectIds.Contains("primordial_terrarium"))
                {
                    ApplyPrimordialTerrarium(played, definition);
                }

                if (definition.EffectIds.Contains("nazjatar_postcard") && HasTribe(played, Tribe.Naga))
                {
                    AddRandomSpellcraftSpellsToHand(1, definition.Name);
                }

                if (definition.EffectIds.Contains("dazzling_dagger"))
                {
                    ApplyDazzlingDaggerAura(played);
                }

                if (definition.EffectIds.Contains(FeralTalismanEffectId) ||
                    definition.EffectIds.Contains(ArtisanalUrnEffectId))
                {
                    refreshBoardAuras = true;
                }

                if (definition.EffectIds.Contains("baller_portrait") && HasTribe(played, Tribe.Elemental))
                {
                    AdvanceBallerPortrait(definition);
                }

                if (definition.EffectIds.Contains("chromatic_tear") && played.Keywords.Contains(Keyword.Battlecry))
                {
                    AdvanceChromaticTear(definition);
                }

                if (definition.EffectIds.Contains(NerglishPhrasebookEffectId))
                {
                    ApplyNerglishPhrasebook(definition);
                }

                if (definition.EffectIds.Contains(NomiStickerEffectId) && HasTribe(played, Tribe.Elemental))
                {
                    ApplyNomiSticker(definition);
                }

                if (definition.EffectIds.Contains(RecyclingStickerEffectId) && HasTribe(played, Tribe.Elemental))
                {
                    State.Player.Tavern.FreeRefreshes = StatMath.SaturatingAdd(State.Player.Tavern.FreeRefreshes, 1, 0, StatMath.MaxStat);
                }

                if (definition.EffectIds.Contains(MarineSignetEffectId))
                {
                    AdvanceMarineSignet(definition);
                }
            }

            if (refreshBoardAuras)
            {
                ApplyBoardTrinketAuras();
            }
        }

        private void DispatchTrinketCardPlayed(MinionInstance played)
        {
            if (played == null ||
                (played.CardKind != CardKind.Minion &&
                    played.CardKind != CardKind.TavernSpell &&
                    played.CardKind != CardKind.Spell))
            {
                return;
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains(DragonwingGliderEffectId))
                {
                    ApplyDragonwingGlider(definition, 4, 4);
                }

                if (definition.EffectIds.Contains(DragonwingGliderGreaterEffectId))
                {
                    ApplyDragonwingGlider(definition, 6, 4);
                }
            }
        }

        private void ApplyDragonwingGlider(TrinketDefinition definition, int attack, int health)
        {
            var candidates = State.Player.Board
                .Where(minion => minion != null && BoardTribeAnalyzer.GetCountedTribes(minion).Contains(Tribe.Dragon))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(
                State.Seed +
                State.Round * 8053 +
                State.Player.Tavern.RecruitLog.Count +
                State.Player.Tavern.CardsPlayedThisTurn +
                definition.DbfId);
            var target = rng.Pick(candidates);
            BuffMinion(target, attack, health, definition.Name);
            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": gave " + target.Name + " +" + attack + "/+" + health + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void AdvanceChromaticTear(TrinketDefinition definition)
        {
            var progress = IncrementAdvancedMechanicCounter(ChromaticTearBattlecryCounter);
            if (progress < 7)
            {
                return;
            }

            var rewards = progress / 7;
            SetAdvancedMechanicCounter(ChromaticTearBattlecryCounter, progress % 7);
            AddRandomChromadrakesToHand(2 * rewards, definition.Name);
        }

        private void AdvanceBallerPortrait(TrinketDefinition definition)
        {
            var progress = IncrementAdvancedMechanicCounter(BallerPortraitElementalCounter);
            if (progress < 9)
            {
                return;
            }

            var rewards = progress / 9;
            SetAdvancedMechanicCounter(BallerPortraitElementalCounter, progress % 9);
            AddTavernSpellToHand(TemperatureShiftCardNumber, rewards, definition.Name);
        }

        private void ApplyWaterWheel(MinionInstance played, TrinketDefinition definition)
        {
            if (!HasTribe(played, Tribe.Elemental))
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.WaterWheelRound != State.Round)
            {
                trinkets.WaterWheelRound = State.Round;
                trinkets.WaterWheelTriggersThisRound = 0;
            }

            if (trinkets.WaterWheelTriggersThisRound >= 2)
            {
                return;
            }

            var before = State.Player.Tavern.Hand.Count;
            AddRandomTavernSpellToHand(State.Player.Tavern.Tier, 1, definition.Name);
            if (State.Player.Tavern.Hand.Count > before)
            {
                trinkets.WaterWheelTriggersThisRound += 1;
            }
        }

        private void ApplyPrimordialTerrarium(MinionInstance played, TrinketDefinition definition)
        {
            if (!HasTribe(played, Tribe.Elemental))
            {
                return;
            }

            var tavern = State.Player.Tavern;
            tavern.NextTavernSpellCostReduction = StatMath.SaturatingAdd(
                tavern.NextTavernSpellCostReduction,
                1,
                0,
                StatMath.MaxStat);
            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": next Tavern spell costs (1) less.",
                tavern.Gold,
                tavern.Gold);
        }

        private (int Attack, int Health) GetTrinketTavernSpellBonus()
        {
            var attack = 0;
            var health = 0;
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (HasEquippedTrinketEffect("heart_of_the_forest"))
            {
                EnsureHeartOfForestBonus();
                attack += trinkets.HeartOfForestBonusAttack;
                health += trinkets.HeartOfForestBonusHealth;
            }

            if (HasEquippedTrinketEffect("marvelous_mushroom"))
            {
                EnsureMarvelousMushroomBonus();
                attack += trinkets.MarvelousMushroomBonusAttack;
                health += trinkets.MarvelousMushroomBonusHealth;
            }

            if (HasEquippedTrinketEffect("bubble_crown") &&
                GetAdvancedMechanicCounter(AllSpellsCastThisGameCounter) >= 10)
            {
                attack += 4;
                health += 4;
            }

            if (HasEquippedTrinketEffect("felburned_ledger") && trinkets.FelburnedLedgerBonusThisTurn > 0)
            {
                attack += trinkets.FelburnedLedgerBonusThisTurn;
                health += trinkets.FelburnedLedgerBonusThisTurn;
            }

            return (attack, health);
        }

        private void DispatchTrinketTavernSpellCast(MinionInstance spell, bool fromHand)
        {
            if (spell == null || spell.CardKind != CardKind.TavernSpell)
            {
                return;
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (fromHand && definition.EffectIds.Contains("heart_of_the_forest"))
                {
                    ImproveHeartOfForest(definition);
                }

                if (definition.EffectIds.Contains("wizards_pipe"))
                {
                    ApplyWizardsPipe(definition);
                }

                if (definition.EffectIds.Contains("comfy_coffin"))
                {
                    ApplyComfyCoffin(definition);
                }

                if (definition.EffectIds.Contains("miniature_ship"))
                {
                    ApplyMiniatureShip(definition);
                }

                if (definition.EffectIds.Contains(BluegillFlippersEffectId))
                {
                    ApplyBluegillFlippers(definition);
                }
            }
        }

        private void DispatchTrinketSpellCast(MinionInstance spell, bool fromHand = false)
        {
            if (spell == null || (spell.CardKind != CardKind.TavernSpell && spell.CardKind != CardKind.Spell))
            {
                return;
            }

            if (fromHand)
            {
                DispatchTrinketCardPlayed(spell);
            }

            IncrementAdvancedMechanicCounter(AllSpellsCastThisGameCounter);
            if (spell.CardId == MaraudersContractCardNumber)
            {
                StealRandomPirateTavernCard("Marauder's Contract");
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("archaic_scroll"))
                {
                    AdvanceArchaicScroll(definition);
                }

                if (definition.EffectIds.Contains("charming_panpipes"))
                {
                    ImproveCharmingPanpipes(definition);
                }

                if (definition.EffectIds.Contains("bewitched_ribbon"))
                {
                    ApplyBewitchedRibbonSpellCast(definition, false);
                }

                if (definition.EffectIds.Contains("dazzling_dagger"))
                {
                    ApplyDazzlingDaggerAuraToBoard();
                }

                if (definition.EffectIds.Contains("bloodbound_earrings"))
                {
                    AdvanceBloodboundEarrings(definition);
                }

                if (fromHand && definition.EffectIds.Contains("bloodbound_ring") && IsBloodGemSpell(spell))
                {
                    ApplyBloodboundRing(definition);
                }
            }
        }

        private void AdvanceBloodboundEarrings(TrinketDefinition definition)
        {
            var threshold = definition.SlotKind == TrinketSlotKind.Greater ? 5 : 4;
            var gemsPerTrigger = definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
            var counterKey = BloodboundEarringsCounterKey(definition);
            var progress = IncrementAdvancedMechanicCounter(counterKey);
            if (progress < threshold)
            {
                return;
            }

            var triggers = progress / threshold;
            SetAdvancedMechanicCounter(counterKey, progress % threshold);
            for (var trigger = 0; trigger < triggers; trigger += 1)
            {
                for (var gem = 0; gem < gemsPerTrigger; gem += 1)
                {
                    ApplyBloodGemToAllFriendlyMinions(definition.Name);
                }
            }

            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": played " + gemsPerTrigger + " Blood Gem(s) on all friendly minions " + triggers + " time(s).",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private static string BloodboundEarringsCounterKey(TrinketDefinition definition)
        {
            return BloodboundEarringsSpellCounter + ":" + (definition?.CardId ?? "unknown");
        }

        private void ApplyBloodboundRing(TrinketDefinition definition)
        {
            BuffAllMinions(
                State.Player.Board.Where(minion => minion.Keywords.Contains(Keyword.DivineShield)),
                1 + State.Player.Tavern.BloodGemBonusAttack,
                1 + State.Player.Tavern.BloodGemBonusHealth,
                definition.Name);
        }

        private static bool IsBloodGemSpell(MinionInstance spell)
        {
            return spell != null &&
                (spell.CardId == BloodGemCardId ||
                    spell.CardId == BristlebackBloodGemCardId ||
                    spell.CardId == RebornBloodGemCardId);
        }

        private void DispatchTrinketSpellcraftCast(MinionInstance spell, int castCount = 1)
        {
            if (spell?.Tags == null || !spell.Tags.Contains("spellcraft"))
            {
                return;
            }

            var repeats = Math.Max(1, castCount);
            for (var repeat = 0; repeat < repeats; repeat += 1)
            {
                foreach (var definition in EquippedTrinketDefinitions())
                {
                    if (definition.EffectIds == null)
                    {
                        continue;
                    }

                    if (definition.EffectIds.Contains("glowscale_portrait"))
                    {
                        BuffAllMinions(
                            State.Player.Board.Where(minion => minion.Keywords.Contains(Keyword.DivineShield)),
                            3,
                            3,
                            definition.Name);
                    }

                    if (definition.EffectIds.Contains("coral_spear"))
                    {
                        CastTavernSpellImmediate(MightOfStormwindCardNumber, definition.Name);
                    }

                    if (definition.EffectIds.Contains("chillmere_mosaic") &&
                        spell.CardId == ChillmereMosaicSpellCardId)
                    {
                        RefreshShopWithBattlecryMinionsForOneCost(definition.Name);
                    }
                }
            }
        }

        private void AdvanceArchaicScroll(TrinketDefinition definition)
        {
            var progress = IncrementAdvancedMechanicCounter(ArchaicScrollSpellCounter);
            if (progress < 6)
            {
                return;
            }

            SetAdvancedMechanicCounter(ArchaicScrollSpellCounter, 0);
            AddRandomTribeMinionToHand(Tribe.Naga, 1, definition.Name);
        }

        private int GetAdvancedMechanicCounter(string key, int fallback = 0)
        {
            var counters = EnsureAdvancedMechanicState(State.Player.Tavern).Counters;
            return counters.TryGetValue(key, out var value) ? value : fallback;
        }

        private void SetAdvancedMechanicCounter(string key, int value)
        {
            EnsureAdvancedMechanicState(State.Player.Tavern).Counters[key] = value;
        }

        private int IncrementAdvancedMechanicCounter(string key, int amount = 1)
        {
            var next = StatMath.SaturatingAdd(GetAdvancedMechanicCounter(key), amount, 0, StatMath.MaxStat);
            SetAdvancedMechanicCounter(key, next);
            return next;
        }

        private void ImproveHeartOfForest(TrinketDefinition definition)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            EnsureHeartOfForestBonus();
            var threshold = definition.SlotKind == TrinketSlotKind.Greater ? 6 : 5;
            trinkets.HeartOfForestCastProgress += 1;
            if (trinkets.HeartOfForestCastProgress < threshold)
            {
                return;
            }

            trinkets.HeartOfForestCastProgress = 0;
            trinkets.HeartOfForestBonusAttack = StatMath.SaturatingAdd(trinkets.HeartOfForestBonusAttack, 1, 0, StatMath.MaxStat);
            trinkets.HeartOfForestBonusHealth = StatMath.SaturatingAdd(trinkets.HeartOfForestBonusHealth, 1, 0, StatMath.MaxStat);
            AddRecruitLog(
                RecruitLogType.Play,
                definition.Name + ": Tavern spell bonus improved to +" + trinkets.HeartOfForestBonusAttack + "/+" + trinkets.HeartOfForestBonusHealth + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void ApplyWizardsPipe(TrinketDefinition definition)
        {
            var targets = State.Player.Board
                .Where(minion => minion != null && !BoardTribeAnalyzer.GetCountedTribes(minion).Any(tribe => tribe != Tribe.None))
                .ToList();
            foreach (var target in targets)
            {
                BuffMinion(target, 4, 4, definition.Name);
            }
        }

        private void DispatchTrinketDiscoverChosen(MinionInstance picked)
        {
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("sinstone_sticker"))
                {
                    ApplySinstoneSticker(picked, definition);
                }

                if (definition.EffectIds.Contains("primalfin_portrait") && picked != null && picked.CardKind == CardKind.Minion)
                {
                    AddRandomTavernSpellToHand(State.Player.Tavern.Tier, 1, definition.Name);
                }
            }
        }

        private void ApplySinstoneSticker(MinionInstance picked, TrinketDefinition definition)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (trinkets.SinstoneStickerRound != State.Round)
            {
                trinkets.SinstoneStickerRound = State.Round;
                trinkets.SinstoneStickerCopiesThisRound = 0;
            }

            if (trinkets.SinstoneStickerCopiesThisRound >= 2)
            {
                return;
            }

            if (CopyCardToHand(picked, definition.Name))
            {
                trinkets.SinstoneStickerCopiesThisRound += 1;
            }
        }

        private void PrepareTrinketCombatStartEffects(List<MinionInstance> playerCombatBoard)
        {
            ClearTemporaryTrinketBloodGemBonus();
            PrepareTrinketAvengeEffects();
            var tavern = State.Player.Tavern;
            var valdrakkenExtraTriggers = HasEquippedTrinketEffect(ValdrakkenWindChimesEffectId) ? 1 : 0;
            var promoExtraTriggers = HasEquippedTrinketEffect(PromoPortraitEffectId) ? 1 : 0;

            if (HasEquippedTrinketEffect(FeralTalismanEffectId) ||
                HasEquippedTrinketEffect(ArtisanalUrnEffectId))
            {
                ApplyBoardTrinketAuras(playerCombatBoard);
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                var batch5StartRepeats = GetBatch5StartOfCombatRepeats(definition, valdrakkenExtraTriggers, ref promoExtraTriggers);
                if (definition.EffectIds.Contains(JarredFrostlingEffectId))
                {
                    tavern.TrinketJarredFrostlingTargets += 2 * batch5StartRepeats;
                }

                if (definition.EffectIds.Contains(PowderKegEffectId))
                {
                    tavern.TrinketPowderKegTargets += 3 * batch5StartRepeats;
                }

                if (definition.EffectIds.Contains(HoggyBankEffectId))
                {
                    tavern.TrinketHoggyBankActive = true;
                }

                if (definition.EffectIds.Contains(RustyTridentEffectId))
                {
                    tavern.TrinketRustyTridentTriggers += batch5StartRepeats;
                }

                if (definition.EffectIds.Contains(SkyGolemPortraitEffectId))
                {
                    tavern.TrinketSkyGolemDeathrattleTriggers += batch5StartRepeats;
                }

                if (definition.EffectIds.Contains(ShipInABottleEffectId))
                {
                    ApplyShipInABottleCombatStart(playerCombatBoard, definition, batch5StartRepeats);
                }

                if (definition.EffectIds.Contains("valorous_medallion"))
                {
                    var amount = definition.SlotKind == TrinketSlotKind.Greater ? 6 : 2;
                    State.Player.Tavern.NextCombatBoardAttack += amount;
                    State.Player.Tavern.NextCombatBoardHealth += amount;
                    AddRecruitLog(RecruitLogType.Play, definition.Name + ": next combat board +" + amount + "/+" + amount + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                }

                if (definition.EffectIds.Contains("dazzling_dagger"))
                {
                    ApplyDazzlingDaggerAuraToCombatBoard(playerCombatBoard);
                }

                if (definition.EffectIds.Contains("bewitched_ribbon"))
                {
                    ApplyBewitchedRibbonCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("bronze_timepiece"))
                {
                    ApplyBronzeTimepieceCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("dramaloc_sticker"))
                {
                    ApplyDramalocStickerCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("emerald_dreamcatcher"))
                {
                    ApplyEmeraldDreamcatcherCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("ironforge_anvil"))
                {
                    ApplyIronforgeAnvilCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("karazhan_chess_set"))
                {
                    ApplyKarazhanChessSetCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("protective_ring"))
                {
                    ApplyProtectiveRingCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("tinyfin_onesie"))
                {
                    ApplyTinyfinOnesieCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("automaton_portrait"))
                {
                    ApplyAutomatonPortraitCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("baleful_incense"))
                {
                    ApplyBalefulIncenseCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("eternal_portrait"))
                {
                    ApplyEternalPortraitCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("holy_mallet"))
                {
                    ApplyHolyMalletCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("training_certificate"))
                {
                    ApplyTrainingCertificateCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("hogwash_basin"))
                {
                    ApplyHogwashBasinCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("rivendare_portrait"))
                {
                    ApplyRivendarePortraitCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("crocheted_sungill"))
                {
                    ApplyCrochetedSungillCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("eclectic_shrine"))
                {
                    ApplyEclecticShrineCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("vashjir_anemone"))
                {
                    ApplyVashjirAnemoneCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("yulon_sticker"))
                {
                    ApplyYulonStickerCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("stegodon_portrait"))
                {
                    ApplyStegodonPortraitCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("fang_anklet"))
                {
                    ApplyFangAnkletCombatStart(playerCombatBoard, definition);
                }

                if (definition.EffectIds.Contains("mama_bear_sticker"))
                {
                    tavern.TrinketCombatBeastSummonBonusAttack += 5;
                    tavern.TrinketCombatBeastSummonBonusHealth += 5;
                }

                if (definition.EffectIds.Contains("slamma_sticker"))
                {
                    tavern.TrinketSlammaStickerActive = true;
                }

                if (definition.EffectIds.Contains("bassgill_portrait"))
                {
                    tavern.TrinketBassgillPortraitActive = true;
                }

                if (definition.EffectIds.Contains("reinforced_shield"))
                {
                    tavern.TrinketReinforcedShieldUses += 5;
                }

                if (definition.EffectIds.Contains("twin_sky_lanterns"))
                {
                    tavern.TrinketTwinSkyLanternCopies = Math.Max(tavern.TrinketTwinSkyLanternCopies, 1);
                }

                if (definition.EffectIds.Contains("twin_sky_lanterns_two"))
                {
                    tavern.TrinketTwinSkyLanternCopies = Math.Max(tavern.TrinketTwinSkyLanternCopies, 2);
                }

                if (definition.EffectIds.Contains("ceremonial_sword"))
                {
                    tavern.TrinketCeremonialSwordAttack += 4;
                }

                if (definition.EffectIds.Contains("faerie_dragon_scale"))
                {
                    tavern.TrinketFaerieDragonScaleUses += 3;
                }

                if (definition.EffectIds.Contains("alliance_keychain"))
                {
                    tavern.TrinketAllianceKeychainTargets += definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
                }

                if (definition.EffectIds.Contains("deathly_phylactery"))
                {
                    tavern.TrinketDeathlyPhylacteryExtraDeathrattles += 1;
                }

                if (definition.EffectIds.Contains("herald_sticker"))
                {
                    tavern.TrinketHeraldStickerActive = true;
                }

                if (definition.EffectIds.Contains("rylak_portrait"))
                {
                    tavern.TrinketRylakPortraitActive = true;
                }

                if (definition.EffectIds.Contains("divine_signet"))
                {
                    tavern.TrinketDivineSignetUses += 4;
                }

                if (definition.EffectIds.Contains("mechagon_adapter"))
                {
                    tavern.TrinketMechagonAdapterUses += 3;
                }

                if (definition.EffectIds.Contains("deathtouch_apple"))
                {
                    tavern.TrinketDeathtouchAppleUses += 3;
                }

                if (definition.EffectIds.Contains("tarecgosa_sticker"))
                {
                    tavern.TrinketTarecgosaStickerActive = true;
                }

                if (definition.EffectIds.Contains("unholy_sanctum"))
                {
                    tavern.TrinketUnholySanctumAttack += definition.SlotKind == TrinketSlotKind.Greater ? 6 : 2;
                    tavern.TrinketUnholySanctumHealth += definition.SlotKind == TrinketSlotKind.Greater ? 4 : 2;
                    tavern.TrinketUnholySanctumSourceCardId = definition.CardId;
                }

                if (definition.EffectIds.Contains("fishy_sticker"))
                {
                    tavern.TrinketFishyStickerActive = true;
                }

                if (definition.EffectIds.Contains("soul_fermenter"))
                {
                    tavern.TrinketSoulFermenterActive = true;
                }

                if (definition.EffectIds.Contains("belcher_portrait"))
                {
                    var amount = definition.SlotKind == TrinketSlotKind.Greater ? 14 : 4;
                    tavern.TrinketBelcherPortraitAttack += amount;
                    tavern.TrinketBelcherPortraitHealth += amount;
                    tavern.TrinketBelcherPortraitSourceCardId = definition.CardId;
                }

                if (definition.EffectIds.Contains("boom_controller"))
                {
                    tavern.TrinketBoomControllerActive = true;
                }

                if (definition.EffectIds.Contains("blood_golem_sticker"))
                {
                    tavern.TrinketBloodGolemStickerActive = true;
                }

                if (definition.EffectIds.Contains("blood_amulet"))
                {
                    tavern.TrinketBloodAmuletActive = true;
                }

                if (definition.EffectIds.Contains("all_purpose_kibble"))
                {
                    var trinkets = EnsureTrinketState(tavern);
                    EnsureAllPurposeKibbleAttack(trinkets);
                    tavern.TrinketAllPurposeKibbleAttack = Math.Max(tavern.TrinketAllPurposeKibbleAttack, trinkets.AllPurposeKibbleAttack);
                }

                if (definition.EffectIds.Contains("sthara_sticker"))
                {
                    tavern.TrinketSTharaStickerActive = true;
                }

                if (definition.EffectIds.Contains(JarOGemsEffectId))
                {
                    tavern.TrinketJarOGemsAttackThreshold = 2;
                }

                if (definition.EffectIds.Contains(ElementiumChestEffectId))
                {
                    tavern.TrinketElementiumChestAttackThreshold = 2;
                }

                if (definition.EffectIds.Contains(GilneanThornedRoseEffectId))
                {
                    tavern.TrinketGilneanRoseAvengeThreshold = 3;
                    tavern.TrinketGilneanRoseAttack += 4;
                    tavern.TrinketGilneanRoseHealth += 5;
                }

                if (definition.EffectIds.Contains(TigerCarvingEffectId))
                {
                    tavern.TrinketTigerCarvingAttack += definition.SlotKind == TrinketSlotKind.Greater ? 6 : 3;
                    tavern.TrinketTigerCarvingHealth += definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
                }

                if (definition.EffectIds.Contains(ThornspikePauldronEffectId))
                {
                    tavern.TrinketThornspikePauldronAttack += 2;
                    tavern.TrinketThornspikePauldronHealth += 1;
                }

                if (definition.EffectIds.Contains(MugOfTheSireEffectId))
                {
                    tavern.TrinketMugOfTheSireActive = true;
                }

                if (definition.EffectIds.Contains(BlingtronsSunglassesEffectId))
                {
                    tavern.TrinketBlingtronSunglassesActive = true;
                }

                if (definition.EffectIds.Contains(ScrapsmithPortraitEffectId))
                {
                    tavern.TrinketScrapsmithPortraitActive = true;
                }

                if (definition.EffectIds.Contains(EyeOfDalaranEffectId))
                {
                    tavern.TrinketEyeOfDalaranActive = true;
                }
            }
        }

        private void ClearTemporaryTrinketBloodGemBonus()
        {
            var tavern = State.Player.Tavern;
            if (tavern.TrinketTemporaryBloodGemAttack != 0)
            {
                tavern.BloodGemBonusAttack = Math.Max(0, tavern.BloodGemBonusAttack - tavern.TrinketTemporaryBloodGemAttack);
                tavern.TrinketTemporaryBloodGemAttack = 0;
            }

            if (tavern.TrinketTemporaryBloodGemHealth != 0)
            {
                tavern.BloodGemBonusHealth = Math.Max(0, tavern.BloodGemBonusHealth - tavern.TrinketTemporaryBloodGemHealth);
                tavern.TrinketTemporaryBloodGemHealth = 0;
            }
        }

        private static int GetBatch5StartOfCombatRepeats(TrinketDefinition definition, int sharedExtraTriggers, ref int promoExtraTriggers)
        {
            if (definition?.EffectIds == null ||
                !definition.EffectIds.Any(IsBatch5StartOfCombatEffect))
            {
                return 1;
            }

            var repeats = 1 + Math.Max(0, sharedExtraTriggers);
            if (promoExtraTriggers > 0)
            {
                repeats += 1;
                promoExtraTriggers -= 1;
            }

            return Math.Max(1, repeats);
        }

        private static bool IsBatch5StartOfCombatEffect(string effectId)
        {
            return effectId == JarredFrostlingEffectId ||
                effectId == PowderKegEffectId ||
                effectId == HoggyBankEffectId ||
                effectId == RustyTridentEffectId ||
                effectId == SkyGolemPortraitEffectId ||
                effectId == ShipInABottleEffectId;
        }

        private void ApplyShipInABottleCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition, int repeats)
        {
            var rng = new SeededRng(State.Seed + State.Round * 911 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && MatchesTribe(minion, Tribe.Pirate))
                .ToList();
            for (var index = 0; index < repeats && candidates.Count > 0; index += 1)
            {
                var picked = rng.Pick(candidates);
                if (State.Player.Tavern.Hand.Count < HandLimit)
                {
                    State.Player.Tavern.Hand.Add(MinionFactory.Create(
                        picked,
                        BoardSide.Player,
                        definition.Name + "-hand-" + State.Round + "-" + index,
                        false,
                        PoolSource.Copy,
                        0));
                    HandleCardsAddedToHand(1, definition.Name);
                }

                if (combatBoard == null || combatBoard.Count >= BoardLimit)
                {
                    continue;
                }

                var summoned = MinionFactory.Create(
                    picked,
                    BoardSide.Player,
                    definition.Name + "-combat-" + State.Round + "-" + index,
                    false,
                    PoolSource.Summon,
                    0);
                summoned.CanAttack = true;
                if (!summoned.Tags.Contains("wingmen_immediate_attack_pending"))
                {
                    summoned.Tags.Add("wingmen_immediate_attack_pending");
                }

                combatBoard.Add(summoned);
                AddRecruitLog(RecruitLogType.Play, definition.Name + ": summoned and got " + summoned.Name + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }
        }

        private void PrepareTrinketAvengeEffects()
        {
            ResetTrinketCombatState();
            var tavern = State.Player.Tavern;
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("bird_feeder"))
                {
                    tavern.TrinketBirdFeederAvengeThreshold = 2;
                    tavern.TrinketBirdFeederAttack += definition.SlotKind == TrinketSlotKind.Greater ? 4 : 1;
                    tavern.TrinketBirdFeederHealth += definition.SlotKind == TrinketSlotKind.Greater ? 4 : 1;
                }

                if (definition.EffectIds.Contains("beetle_band"))
                {
                    tavern.TrinketBeetleBandAvengeThreshold = definition.SlotKind == TrinketSlotKind.Greater ? 6 : 5;
                    tavern.TrinketBeetleBandSummonCount += definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
                }

                if (definition.EffectIds.Contains("quilligraphy_set"))
                {
                    tavern.TrinketQuilligraphyAvengeThreshold = 4;
                    tavern.TrinketQuilligraphyAttack += definition.SlotKind == TrinketSlotKind.Greater ? 1 : 0;
                    tavern.TrinketQuilligraphyHealth += 1;
                }

                if (definition.EffectIds.Contains("wicked_tome"))
                {
                    tavern.TrinketWickedTomeAvengeThreshold = definition.SlotKind == TrinketSlotKind.Greater ? 4 : 3;
                    tavern.TrinketWickedTomeAttack += 1;
                    tavern.TrinketWickedTomeHealth += definition.SlotKind == TrinketSlotKind.Greater ? 1 : 0;
                }

                if (definition.EffectIds.Contains("staff_of_the_scourge"))
                {
                    tavern.TrinketStaffOfTheScourgeAvengeThreshold = 5;
                }

                if (definition.EffectIds.Contains("cloud_serpent_horn"))
                {
                    tavern.TrinketCloudSerpentHornAvengeThreshold = 3;
                }

                if (definition.EffectIds.Contains("fridge_magnet"))
                {
                    tavern.TrinketFridgeMagnetAvengeThreshold = 3;
                }

                if (definition.EffectIds.Contains("battle_horn"))
                {
                    tavern.TrinketBattleHornAvengeThreshold = 2;
                }

                if (definition.EffectIds.Contains("bristlebach_portrait"))
                {
                    tavern.TrinketBristlebachPortraitActive = true;
                }
            }
        }

        private void ApplyBronzeTimepieceCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var targets = CombatBoardMinions(combatBoard).ToList();
            foreach (var minion in targets)
            {
                BuffMinion(minion, 0, Math.Max(0, minion.Attack / 2), definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyDramalocStickerCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var highestHandAttack = State.Player.Tavern.Hand
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .Select(card => Math.Max(0, card.Attack))
                .DefaultIfEmpty(0)
                .Max();
            if (highestHandAttack <= 0)
            {
                return;
            }

            var targets = CombatBoardMinions(combatBoard)
                .Where(minion => HasTribe(minion, Tribe.Murloc))
                .ToList();
            foreach (var murloc in targets)
            {
                BuffMinion(murloc, highestHandAttack, 0, definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyEmeraldDreamcatcherCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var minions = CombatBoardMinions(combatBoard).ToList();
            var highestAttack = minions.Select(minion => Math.Max(0, minion.Attack)).DefaultIfEmpty(0).Max();
            if (highestAttack <= 0)
            {
                return;
            }

            var targets = minions.Where(minion => HasTribe(minion, Tribe.Dragon)).ToList();
            foreach (var dragon in targets)
            {
                BuffMinion(dragon, Math.Max(0, highestAttack - dragon.Attack), 0, definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyIronforgeAnvilCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var targets = CombatBoardMinions(combatBoard)
                .Where(minion => minion.Tribes == null || minion.Tribes.Count == 0 || minion.Tribes.All(tribe => tribe == Tribe.None))
                .ToList();
            foreach (var minion in targets)
            {
                BuffMinion(minion, StatMath.SaturatingMultiply(minion.Attack, 2, 0, StatMath.MaxStat), StatMath.SaturatingMultiply(minion.MaxHealth, 2, 0, StatMath.MaxStat), definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyKarazhanChessSetCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            if (combatBoard == null || combatBoard.Count >= BoardLimit)
            {
                return;
            }

            var leftmost = CombatBoardMinions(combatBoard).FirstOrDefault();
            if (leftmost == null)
            {
                return;
            }

            var copy = leftmost.Clone();
            copy.InstanceId = "trinket-karazhan-chess-set-" + leftmost.InstanceId + "-" + State.Round;
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Summon;
            copy.OriginPoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            combatBoard.Add(copy);
            LogTrinketCombatStart(definition, 1);
        }

        private void ApplyProtectiveRingCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var candidates = CombatBoardMinions(combatBoard)
                .Where(minion => HasTribe(minion, Tribe.Pirate))
                .ToList();
            var rng = new SeededRng(State.Seed + State.Round * 3571 + State.Player.Tavern.RecruitLog.Count);
            var applied = 0;
            while (applied < 4 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var target = candidates[index];
                candidates.RemoveAt(index);
                AddKeyword(target, Keyword.DivineShield);
                applied += 1;
            }

            LogTrinketCombatStart(definition, applied);
        }

        private void ApplyTinyfinOnesieCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var target = CombatBoardMinions(combatBoard).FirstOrDefault();
            var handMinion = State.Player.Tavern.Hand
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.MaxHealth)
                .ThenByDescending(card => card.Health)
                .ThenByDescending(card => card.Attack)
                .FirstOrDefault();
            if (target == null || handMinion == null)
            {
                return;
            }

            BuffMinion(target, Math.Max(0, handMinion.Attack), Math.Max(0, handMinion.MaxHealth), definition.Name);
            LogTrinketCombatStart(definition, 1);
        }

        private void ApplyAutomatonPortraitCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            if (combatBoard == null || combatBoard.Count >= BoardLimit)
            {
                return;
            }

            var automaton = CreateCombatMinionByCardId(AncestralAutomatonCardId, "trinket-automaton-portrait-" + State.Round);
            if (automaton == null)
            {
                return;
            }

            combatBoard.Add(automaton);
            var summonCount = State.Player.Tavern.AncestralAutomatonSummons + 1;
            ApplyAutomatonCombatStats(combatBoard, summonCount);
            LogTrinketCombatStart(definition, 1);
        }

        private void ApplyBalefulIncenseCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var targets = new List<MinionInstance>();
            var left = CombatBoardMinions(combatBoard).FirstOrDefault(minion => HasTribe(minion, Tribe.Undead));
            var right = CombatBoardMinions(combatBoard).LastOrDefault(minion => HasTribe(minion, Tribe.Undead));
            if (left != null)
            {
                targets.Add(left);
            }

            if (right != null && !targets.Any(minion => string.Equals(minion.InstanceId, right.InstanceId, StringComparison.Ordinal)))
            {
                targets.Add(right);
            }

            foreach (var target in targets)
            {
                AddKeyword(target, Keyword.Reborn);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyEternalPortraitCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var targets = CombatBoardMinions(combatBoard)
                .Where(minion => string.Equals(minion.CardId, EternalKnightCardId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var target in targets)
            {
                AddKeyword(target, Keyword.Taunt);
                AddKeyword(target, Keyword.Reborn);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyHolyMalletCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var targets = new List<MinionInstance>();
            var left = CombatBoardMinions(combatBoard).FirstOrDefault();
            var right = CombatBoardMinions(combatBoard).LastOrDefault();
            if (left != null)
            {
                targets.Add(left);
            }

            if (right != null && !targets.Any(minion => string.Equals(minion.InstanceId, right.InstanceId, StringComparison.Ordinal)))
            {
                targets.Add(right);
            }

            foreach (var target in targets)
            {
                AddKeyword(target, Keyword.DivineShield);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyTrainingCertificateCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var targets = CombatBoardMinions(combatBoard)
                .Select((minion, index) => new { Minion = minion, Index = index })
                .OrderBy(item => item.Minion.Attack)
                .ThenBy(item => item.Index)
                .Take(2)
                .Select(item => item.Minion)
                .ToList();
            foreach (var target in targets)
            {
                BuffMinion(target, Math.Max(0, target.Attack), Math.Max(0, target.MaxHealth), definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyHogwashBasinCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var attack = StatMath.SaturatingMultiply(3, 1 + State.Player.Tavern.BloodGemBonusAttack, 0, StatMath.MaxStat);
            var health = StatMath.SaturatingMultiply(3, 1 + State.Player.Tavern.BloodGemBonusHealth, 0, StatMath.MaxStat);
            var targets = CombatBoardMinions(combatBoard).ToList();
            foreach (var target in targets)
            {
                BuffMinion(target, attack, health, definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyRivendarePortraitCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var targets = CombatBoardMinions(combatBoard)
                .Where(minion => string.Equals(minion.CardId, TitusRivendareCardId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var target in targets)
            {
                BuffMinion(target, 0, Math.Max(0, target.MaxHealth), definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyCrochetedSungillCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var handMinion = State.Player.Tavern.Hand
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.MaxHealth)
                .ThenByDescending(card => card.Health)
                .ThenByDescending(card => card.Attack)
                .FirstOrDefault();
            if (handMinion == null)
            {
                return;
            }

            BuffMinion(handMinion, 4, 4, definition.Name);
            var affected = 1;
            if (combatBoard != null && combatBoard.Count < BoardLimit)
            {
                var copy = handMinion.Clone();
                copy.InstanceId = "trinket-crocheted-sungill-" + handMinion.InstanceId + "-" + State.Round;
                copy.Owner = BoardSide.Player;
                copy.PoolSource = PoolSource.Summon;
                copy.OriginPoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanReturnToPoolAfterAttach = false;
                copy.CanAttack = true;
                combatBoard.Add(copy);
                affected += 1;
            }

            LogTrinketCombatStart(definition, affected);
        }

        private void ApplyEclecticShrineCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var mechanics = EnsureAdvancedMechanicState(State.Player.Tavern);
            var attackKey = "trinket:eclectic_shrine:attack";
            var healthKey = "trinket:eclectic_shrine:health";
            var attack = mechanics.Counters.TryGetValue(attackKey, out var storedAttack) ? Math.Max(3, storedAttack) : 3;
            var health = mechanics.Counters.TryGetValue(healthKey, out var storedHealth) ? Math.Max(2, storedHealth) : 2;
            var targets = new List<MinionInstance>();
            var usedInstances = new HashSet<string>();
            var tribes = new[]
            {
                Tribe.Beast,
                Tribe.Murloc,
                Tribe.Mech,
                Tribe.Demon,
                Tribe.Dragon,
                Tribe.Pirate,
                Tribe.Elemental,
                Tribe.Quilboar,
                Tribe.Undead,
                Tribe.Naga
            };

            foreach (var tribe in tribes)
            {
                var target = CombatBoardMinions(combatBoard)
                    .FirstOrDefault(minion => HasTribe(minion, tribe) && !usedInstances.Contains(minion.InstanceId));
                if (target == null)
                {
                    continue;
                }

                targets.Add(target);
                usedInstances.Add(target.InstanceId);
            }

            foreach (var target in targets)
            {
                BuffMinion(target, attack, health, definition.Name);
            }

            mechanics.Counters[attackKey] = StatMath.SaturatingAdd(attack, 3, 0, StatMath.MaxStat);
            mechanics.Counters[healthKey] = StatMath.SaturatingAdd(health, 2, 0, StatMath.MaxStat);
            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyVashjirAnemoneCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var amount = 1 + (Math.Max(0, State.Player.Tavern.TavernSpellsCastThisGame) >> 2);
            var targets = CombatBoardMinions(combatBoard)
                .Where(minion => HasTribe(minion, Tribe.Naga))
                .ToList();
            foreach (var target in targets)
            {
                BuffMinion(target, 0, amount, definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyYulonStickerCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var target = CombatBoardMinions(combatBoard)
                .Where(minion => HasTribe(minion, Tribe.Dragon))
                .OrderByDescending(minion => minion.TavernTier)
                .ThenBy(minion => combatBoard.IndexOf(minion))
                .FirstOrDefault();
            if (target == null)
            {
                return;
            }

            MakeGoldenForCombat(target);
            LogTrinketCombatStart(definition, 1);
        }

        private void ApplyStegodonPortraitCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var targets = CombatBoardMinions(combatBoard)
                .Where(minion => HasTribe(minion, Tribe.Beast))
                .Take(2)
                .ToList();
            foreach (var target in targets)
            {
                AddKeyword(target, Keyword.DivineShield);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private void ApplyFangAnkletCombatStart(List<MinionInstance> combatBoard, TrinketDefinition definition)
        {
            var trinkets = EnsureTrinketState(State.Player.Tavern);
            EnsureFangAnkletBonus(trinkets);
            var targets = CombatBoardMinions(combatBoard)
                .Where(minion => HasTribe(minion, Tribe.Beast))
                .ToList();
            foreach (var target in targets)
            {
                BuffMinion(target, trinkets.FangAnkletBonusAttack, trinkets.FangAnkletBonusHealth, definition.Name);
            }

            LogTrinketCombatStart(definition, targets.Count);
        }

        private IEnumerable<MinionInstance> CombatBoardMinions(List<MinionInstance> combatBoard)
        {
            return combatBoard == null
                ? Enumerable.Empty<MinionInstance>()
                : combatBoard.Where(minion => minion != null && minion.CardKind == CardKind.Minion);
        }

        private MinionInstance CreateCombatMinionByCardId(string cardId, string instanceId)
        {
            var definition = catalog.All.FirstOrDefault(minion => string.Equals(minion.CardId, cardId, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                return null;
            }

            var minion = MinionFactory.Create(definition, BoardSide.Player, instanceId, false, PoolSource.Summon, 0);
            minion.Owner = BoardSide.Player;
            minion.CanAttack = true;
            return minion;
        }

        private void ApplyAutomatonCombatStats(List<MinionInstance> combatBoard, int summonCount)
        {
            var otherSummons = Math.Max(0, summonCount - 1);
            foreach (var automaton in CombatBoardMinions(combatBoard).Where(minion => string.Equals(minion.CardId, AncestralAutomatonCardId, StringComparison.OrdinalIgnoreCase)))
            {
                var attack = StatMath.SaturatingMultiply(otherSummons, automaton.Golden ? 6 : 3, 0, StatMath.MaxStat);
                var health = StatMath.SaturatingMultiply(otherSummons, automaton.Golden ? 4 : 2, 0, StatMath.MaxStat);
                BuffMinion(automaton, attack, health, GlobalAutomatonSourceId);
            }
        }

        private void AddKeyword(MinionInstance target, Keyword keyword)
        {
            if (target != null && !target.Keywords.Contains(keyword))
            {
                target.Keywords.Add(keyword);
            }
        }

        private static void MakeGoldenForCombat(MinionInstance target)
        {
            if (target == null || target.Golden)
            {
                return;
            }

            target.Golden = true;
            StatMath.DoubleCurrentStats(target, false);
            RefreshScarletSurvivor(target);
        }

        private void LogTrinketCombatStart(TrinketDefinition definition, int affected)
        {
            if (affected > 0)
            {
                AddRecruitLog(RecruitLogType.Play, definition.Name + ": applied start of combat effect to " + affected + " minion(s).", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }
        }

        private void PrepareQuestCombatStartEffects(List<MinionInstance> playerCombatBoard)
        {
            var tavern = State.Player.Tavern;
            tavern.QuestFriendlyAttackAura = HasActiveQuestReward(TheSmokingGunRewardId) ? 4 : 0;
            tavern.QuestVolatileVenomActive = HasActiveQuestReward(VolatileVenomRewardId);
            tavern.QuestBoomSquadActive = HasActiveQuestReward(BoomSquadRewardId);
            tavern.QuestGrimFreshenerActive = HasActiveQuestReward(GrimFreshenerRewardId);
            tavern.QuestCycleOfEnergyActive = HasActiveQuestReward(CycleOfEnergyRewardId);
            tavern.QuestStableAmalgamationActive = HasActiveQuestReward(StableAmalgamationRewardId);
            tavern.QuestDeathrattleExtraTriggers = HasActiveQuestReward(TurbulentTombsRewardId) ? 1 : 0;

            if (HasActiveQuestReward(StolenGoldRewardId))
            {
                ApplyStolenGoldCombatStart(playerCombatBoard);
            }

            if (HasActiveQuestReward(EvilTwinRewardId))
            {
                ApplyEvilTwinCombatStart(playerCombatBoard);
            }

            if (HasActiveQuestReward(StaffOfOriginationRewardId))
            {
                tavern.NextCombatBoardAttack += 12;
                tavern.NextCombatBoardHealth += 12;
                AddRecruitLog(RecruitLogType.Play, "Staff of Origination: next combat board +12/+12.", tavern.Gold, tavern.Gold);
            }

            if (HasActiveQuestReward(TumblingDisasterRewardId))
            {
                tavern.QuestTumblingAttack = GetQuestRewardCounter(TumblingDisasterRewardId, "attack", 4);
                tavern.QuestTumblingHealth = GetQuestRewardCounter(TumblingDisasterRewardId, "health", 4);
            }
            else
            {
                tavern.QuestTumblingAttack = 0;
                tavern.QuestTumblingHealth = 0;
            }

            if (HasActiveQuestReward(RighteousChargeRewardId))
            {
                var leftmost = playerCombatBoard?.FirstOrDefault();
                if (leftmost != null)
                {
                    if (!leftmost.Keywords.Contains(Keyword.DivineShield))
                    {
                        leftmost.Keywords.Add(Keyword.DivineShield);
                    }

                    if (!leftmost.Tags.Contains("wingmen_immediate_attack_pending"))
                    {
                        leftmost.Tags.Add("wingmen_immediate_attack_pending");
                    }
                }
            }
        }

        private void RecordTrinketShopRefresh()
        {
            if (!HasEquippedTrinketEffect("dalaran_cheese_wheel"))
            {
                return;
            }

            var tavern = State.Player.Tavern;
            var trinkets = EnsureTrinketState(tavern);
            var beforeImprovement = trinkets.DalaranCheeseWheelRefreshes / 4;
            trinkets.DalaranCheeseWheelRefreshes += 1;
            var afterImprovement = trinkets.DalaranCheeseWheelRefreshes / 4;
            RecalculateDalaranCheeseWheelBonus();
            if (afterImprovement > beforeImprovement)
            {
                AddRecruitLog(
                    RecruitLogType.Play,
                    "Dalaran Cheese Wheel improved to +" + trinkets.DalaranCheeseWheelBonusAttack + "/+" + trinkets.DalaranCheeseWheelBonusHealth + " in the Tavern.",
                    tavern.Gold,
                    tavern.Gold);
            }
        }

        private void DispatchTrinketShopRefreshed(List<MinionInstance> shop)
        {
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("cursed_crystal"))
                {
                    ApplyCursedCrystal(shop, definition);
                }

                if (definition.EffectIds.Contains("lightning_in_a_bottle"))
                {
                    ApplyLightningInABottle(shop, definition);
                }

                if (definition.EffectIds.Contains("lubber_sticker"))
                {
                    AddLubberStickerExtraTavernSpell(shop, definition);
                }

                if (definition.EffectIds.Contains(UpstartEmbersEffectId))
                {
                    ApplyUpstartEmbersRefresh(shop, definition);
                }

                if (definition.EffectIds.Contains(DemonicTapestryEffectId))
                {
                    ApplyDemonicTapestryRefresh(shop, definition);
                }

                if (definition.EffectIds.Contains(FinleysHelmetEffectId))
                {
                    ApplyFinleysHelmetRefresh(shop, definition);
                }
            }

            ApplyTrinketShopCostDisplays(shop);
        }

        private void AddLubberStickerExtraTavernSpell(List<MinionInstance> shop, TrinketDefinition definition)
        {
            if (shop == null)
            {
                return;
            }

            var tavern = State.Player.Tavern;
            var rng = new SeededRng(State.Seed + State.Round * 3203 + tavern.RecruitLog.Count + shop.Count);
            var spell = DrawTavernSpell(Math.Max(1, tavern.Tier), rng);
            if (spell == null)
            {
                AddRecruitLog(RecruitLogType.Reroll, definition.Name + ": no Tavern spell is available for the extra offer.", tavern.Gold, tavern.Gold);
                return;
            }

            shop.Add(MinionFactory.Create(spell, BoardSide.Player, "trinket-lubber-sticker-" + State.Round + "-" + shop.Count));
            TavernShopSlots.Ensure(tavern);
            AddRecruitLog(RecruitLogType.Reroll, definition.Name + ": added an extra Tavern spell to the Tavern.", tavern.Gold, tavern.Gold);
        }

        private void ApplyLubberStickerRefreshOffers(List<MinionInstance> shop)
        {
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds != null && definition.EffectIds.Contains("lubber_sticker"))
                {
                    AddLubberStickerExtraTavernSpell(shop, definition);
                }
            }
        }

        private void ApplyCursedCrystal(List<MinionInstance> shop, TrinketDefinition definition)
        {
            var targets = shop?.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            BuffAllMinions(targets, 3, 3, definition.Name);
        }

        private void ApplyLightningInABottle(List<MinionInstance> shop, TrinketDefinition definition)
        {
            var targets = shop?.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            var highest = targets
                .OrderByDescending(card => card.Attack)
                .ThenByDescending(card => card.MaxHealth)
                .ThenBy(card => card.InstanceId)
                .First();
            var lowest = targets
                .OrderBy(card => card.Attack)
                .ThenBy(card => card.MaxHealth)
                .ThenBy(card => card.InstanceId)
                .First();

            BuffMinion(lowest, Math.Max(0, highest.Attack), Math.Max(0, highest.MaxHealth), definition.Name);
        }

        private void RecalculateDalaranCheeseWheelBonus()
        {
            var tavern = State.Player.Tavern;
            var trinkets = EnsureTrinketState(tavern);
            var baseBonus = 0;
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds != null && definition.EffectIds.Contains("dalaran_cheese_wheel"))
                {
                    baseBonus += definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1;
                }
            }

            if (baseBonus <= 0)
            {
                trinkets.DalaranCheeseWheelBonusAttack = 0;
                trinkets.DalaranCheeseWheelBonusHealth = 0;
                return;
            }

            var bonus = baseBonus + trinkets.DalaranCheeseWheelRefreshes / 4;
            trinkets.DalaranCheeseWheelBonusAttack = bonus;
            trinkets.DalaranCheeseWheelBonusHealth = bonus;
        }

        private void ApplyTrinketShopAuras(List<MinionInstance> shop)
        {
            if (shop == null)
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            var dalaranAttack = trinkets.DalaranCheeseWheelBonusAttack;
            var dalaranHealth = trinkets.DalaranCheeseWheelBonusHealth;
            var darnassusBonus = Math.Max(0, trinkets.DarnassusPieSoldMinionsThisTurn) * GetDarnassusPieBonusPerSold();
            var defilerBonus = GetDefilerPortraitFodderBonus();
            var netherBonus = GetNetherPendantShopBonus();
            var glowingBonus = HasEquippedTrinketEffect(GlowingGauntletEffectId) ? 3 : 0;

            foreach (var minion in shop.Where(card => card != null && card.CardKind == CardKind.Minion))
            {
                RemoveTrinketShopAura(minion, DalaranCheeseWheelAuraSourceId);
                RemoveTrinketShopAura(minion, DarnassusPieAuraSourceId);
                RemoveTrinketShopAura(minion, DefilerPortraitAuraSourceId);
                RemoveTrinketShopAura(minion, NetherPendantAuraSourceId);
                RemoveTrinketShopAura(minion, GlowingGauntletAuraSourceId);
                ApplyTrinketShopAura(minion, "trinket-dalaran-cheese-wheel-", DalaranCheeseWheelAuraSourceId, dalaranAttack, dalaranHealth);
                ApplyTrinketShopAura(minion, "trinket-darnassus-pie-", DarnassusPieAuraSourceId, darnassusBonus, darnassusBonus);
                ApplyTrinketShopAura(minion, "trinket-nether-pendant-", NetherPendantAuraSourceId, netherBonus, netherBonus);
                ApplyTrinketShopAura(minion, "trinket-glowing-gauntlet-", GlowingGauntletAuraSourceId, glowingBonus, glowingBonus);
                if (IsDemonFodder(minion))
                {
                    ApplyTrinketShopAura(minion, "trinket-defiler-portrait-", DefilerPortraitAuraSourceId, defilerBonus, defilerBonus);
                }
            }

            ApplyTrinketShopCostDisplays(shop);
        }

        private int GetDefilerPortraitFodderBonus()
        {
            var bonus = 0;
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains(DefilerPortraitGreaterEffectId))
                {
                    bonus += 10;
                    continue;
                }

                if (definition.EffectIds.Contains(DefilerPortraitEffectId))
                {
                    bonus += definition.SlotKind == TrinketSlotKind.Greater ? 10 : 2;
                }
            }

            return bonus;
        }

        private int GetDarnassusPieBonusPerSold()
        {
            var bonus = 0;
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains(DarnassusPieEffectId))
                {
                    bonus += 1;
                }

                if (definition.EffectIds.Contains(DarnassusPieDoubleEffectId))
                {
                    bonus += 2;
                }
            }

            return bonus;
        }

        private int GetNetherPendantShopBonus()
        {
            if (!HasEquippedTrinketEffect(NetherPendantEffectId))
            {
                return 0;
            }

            return 2 + Math.Max(0, GetAdvancedMechanicCounter(NetherPendantBonusCounter));
        }

        private void ApplyTrinketShopCostDisplays(List<MinionInstance> shop)
        {
            if (shop == null)
            {
                return;
            }

            foreach (var card in shop)
            {
                if (card == null || (card.CardKind != CardKind.Minion && card.CardKind != CardKind.TavernSpell))
                {
                    continue;
                }

                card.Cost = EvaluateBuyCost(card).Cost;
            }
        }

        private bool IsGrifterFreePirate(MinionInstance card)
        {
            if (!HasEquippedTrinketEffect(GrifterPortraitEffectId) ||
                card == null ||
                card.CardKind != CardKind.Minion ||
                !HasTribe(card, Tribe.Pirate))
            {
                return false;
            }

            ResetRoundCounter(GrifterRoundCounter, GrifterUsedCounter);
            return GetAdvancedMechanicCounter(GrifterUsedCounter) <= 0;
        }

        private static bool IsMagneticMech(MinionInstance card)
        {
            return card != null &&
                card.CardKind == CardKind.Minion &&
                card.Keywords != null &&
                card.Keywords.Contains(Keyword.Magnetic) &&
                card.Tribes != null &&
                card.Tribes.Contains(Tribe.Mech);
        }

        private static bool IsDemonFodder(MinionInstance minion)
        {
            if (minion == null || minion.CardKind != CardKind.Minion)
            {
                return false;
            }

            if (string.Equals(minion.CardId, DemonFodderCardId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return minion.Tags != null && minion.Tags.Any(tag =>
                string.Equals(tag, "demon_fodder", StringComparison.OrdinalIgnoreCase));
        }

        private static void ApplyTrinketShopAura(MinionInstance minion, string idPrefix, string sourceId, int attack, int health)
        {
            if (minion == null || (attack <= 0 && health <= 0))
            {
                return;
            }

            StatMath.ApplyStatDelta(minion, attack, health);
            minion.Enchantments.Add(new Enchantment
            {
                Id = idPrefix + minion.InstanceId,
                SourceId = sourceId,
                AttackBonus = attack,
                HealthBonus = health,
                Duration = "PERMANENT"
            });
        }

        private static void RemoveTrinketShopAura(MinionInstance minion, string sourceId)
        {
            if (minion?.Enchantments == null)
            {
                return;
            }

            var existing = minion.Enchantments
                .Where(enchantment => enchantment.SourceId == sourceId)
                .ToList();
            foreach (var enchantment in existing)
            {
                StatMath.ApplyStatDeltaPreservingDamage(
                    minion,
                    StatMath.SaturatingSubtract(0, enchantment.AttackBonus),
                    StatMath.SaturatingSubtract(0, enchantment.HealthBonus));
                minion.Enchantments.Remove(enchantment);
            }
        }

        private void BuffRandomFriendlyMinionsFromTrinket(TrinketDefinition definition, int count, int attack, int health)
        {
            var candidates = State.Player.Board.Where(minion => minion != null).ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 7919 + State.Player.Tavern.RecruitLog.Count + definition.DbfId);
            var buffed = 0;
            while (buffed < count && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var target = candidates[index];
                candidates.RemoveAt(index);
                StatMath.ApplyStatDelta(target, attack, health);
                target.Enchantments.Add(new Enchantment
                {
                    Id = "trinket-" + definition.CardId + "-" + State.Round + "-" + buffed,
                    SourceId = definition.Name,
                    AttackBonus = attack,
                    HealthBonus = health,
                    Duration = "PERMANENT"
                });
                buffed += 1;
            }

            AddRecruitLog(RecruitLogType.Play, definition.Name + ": buffed " + buffed + " friendly minion(s) +" + attack + "/+" + health + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private bool HasEquippedTrinketEffect(string effectId)
        {
            return EquippedTrinketDefinitions().Any(definition =>
                definition.EffectIds != null && definition.EffectIds.Contains(effectId));
        }

        private List<TrinketDefinition> EquippedTrinketDefinitions()
        {
            var result = new List<TrinketDefinition>();
            if (trinketCatalog == null)
            {
                return result;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddEquippedTrinketDefinition(result, seen, trinkets.LesserTrinketId);
            AddEquippedTrinketDefinition(result, seen, trinkets.GreaterTrinketId);
            if (trinkets.Equipped != null)
            {
                foreach (var equipped in trinkets.Equipped)
                {
                    AddEquippedTrinketDefinition(result, seen, equipped?.TrinketId);
                }
            }

            return result;
        }

        private void AddEquippedTrinketDefinition(List<TrinketDefinition> result, HashSet<string> seen, string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId) || seen.Contains(cardId))
            {
                return;
            }

            if (trinketCatalog.TryGetByCardId(cardId, out var definition))
            {
                result.Add(definition);
                seen.Add(cardId);
            }
        }

        private static AdvancedMechanicState EnsureAdvancedMechanicState(TavernState tavern)
        {
            if (tavern.AdvancedMechanics == null)
            {
                tavern.AdvancedMechanics = new AdvancedMechanicState();
            }

            if (tavern.AdvancedMechanics.Equipped == null)
            {
                tavern.AdvancedMechanics.Equipped = new List<EquippedAdvancedMechanic>();
            }

            if (tavern.AdvancedMechanics.Counters == null)
            {
                tavern.AdvancedMechanics.Counters = new Dictionary<string, int>();
            }

            if (tavern.AdvancedMechanics.Trinkets == null)
            {
                tavern.AdvancedMechanics.Trinkets = new PlayerTrinketState();
            }

            if (tavern.AdvancedMechanics.Trinkets.Equipped == null)
            {
                tavern.AdvancedMechanics.Trinkets.Equipped = new List<EquippedTrinketState>();
            }

            if (tavern.AdvancedMechanics.Quests == null)
            {
                tavern.AdvancedMechanics.Quests = new PlayerQuestState();
            }

            if (tavern.AdvancedMechanics.Quests.Completed == null)
            {
                tavern.AdvancedMechanics.Quests.Completed = new List<ActiveQuestState>();
            }

            if (tavern.AdvancedMechanics.Quests.RewardCounters == null)
            {
                tavern.AdvancedMechanics.Quests.RewardCounters = new Dictionary<string, int>();
            }

            if (tavern.AdvancedMechanics.Quests.RewardFlags == null)
            {
                tavern.AdvancedMechanics.Quests.RewardFlags = new Dictionary<string, bool>();
            }

            if (tavern.AdvancedMechanics.Quests.HiddenTreasureVaultGold <= 0)
            {
                tavern.AdvancedMechanics.Quests.HiddenTreasureVaultGold = 1;
            }

            if (tavern.AdvancedMechanics.Quests.CookedBookAttack <= 0)
            {
                tavern.AdvancedMechanics.Quests.CookedBookAttack = 2;
            }

            if (tavern.AdvancedMechanics.Quests.CookedBookHealth <= 0)
            {
                tavern.AdvancedMechanics.Quests.CookedBookHealth = 2;
            }

            return tavern.AdvancedMechanics;
        }

        private static PlayerTrinketState EnsureTrinketState(TavernState tavern)
        {
            return EnsureAdvancedMechanicState(tavern).Trinkets;
        }

        private static PlayerQuestState EnsureQuestState(TavernState tavern)
        {
            return EnsureAdvancedMechanicState(tavern).Quests;
        }

        private int ApplyTrinketTavernSpellCostModifiers(MinionInstance target, int cost)
        {
            if (target == null || target.CardKind != CardKind.TavernSpell)
            {
                return cost;
            }

            if (HasEquippedTrinketEffect("cowrie_necklace") && IsStatTavernSpell(target))
            {
                cost = Math.Max(0, cost - 2);
            }

            if (HasEquippedTrinketEffect("lubber_sticker"))
            {
                var trinkets = EnsureTrinketState(State.Player.Tavern);
                if (trinkets.LubberStickerRound != State.Round)
                {
                    trinkets.LubberStickerRound = State.Round;
                    trinkets.LubberStickerTavernSpellBuysThisRound = 0;
                }

                if (trinkets.LubberStickerTavernSpellBuysThisRound <= 0)
                {
                    cost = Math.Max(0, cost - 1);
                }
            }

            if (HasEquippedTrinketEffect("peacebloom_candle"))
            {
                var trinkets = EnsureTrinketState(State.Player.Tavern);
                if (trinkets.PeacebloomCandleRound != State.Round)
                {
                    trinkets.PeacebloomCandleRound = State.Round;
                    trinkets.PeacebloomCandleBuysThisRound = 0;
                }

                if (trinkets.PeacebloomCandleBuysThisRound < 3)
                {
                    cost = 0;
                }
            }

            return cost;
        }

        private static bool IsStatTavernSpell(MinionInstance card)
        {
            if (card == null || card.CardKind != CardKind.TavernSpell)
            {
                return false;
            }

            if (card.Tags != null && card.Tags.Any(tag =>
                string.Equals(tag, "buff_spell", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, "stat_tavern_spell", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tag, "targeted_spell", StringComparison.OrdinalIgnoreCase) ||
                tag.IndexOf("buff", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(card.Text) &&
                (card.Text.IndexOf("+", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    card.Text.IndexOf("stats", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private int GetBuyCost(MinionInstance target)
        {
            return EvaluateBuyCost(target).Cost;
        }

        private BuyCostEvaluation EvaluateBuyCost(MinionInstance target)
        {
            var evaluation = new BuyCostEvaluation();
            if (target == null)
            {
                return evaluation;
            }

            var tavern = State.Player.Tavern;
            var cost = GetBaseBuyCost(target);

            if (target.CardKind == CardKind.Minion && HasActiveQuestReward(BloodsoakedTomeRewardId))
            {
                cost = 2;
            }

            if (target.Tags != null && target.Tags.Contains("chillmere_mosaic_cost_1"))
            {
                cost = 1;
            }

            if (target.CardKind == CardKind.TavernSpell)
            {
                if (tavern.NextTavernSpellCostReduction > 0)
                {
                    cost = Math.Max(0, cost - tavern.NextTavernSpellCostReduction);
                }

                if (HasActiveQuestReward(BeyondTheMirageRewardId))
                {
                    cost = Math.Max(0, cost - 1);
                }

                cost = ApplyTrinketTavernSpellCostModifiers(target, cost);
            }

            if (HasEquippedTrinketEffect(ElectrodeAttractorEffectId) && IsMagneticMech(target))
            {
                cost = 2;
            }

            cost = Math.Max(0, HeroEffectEngine.ModifyBuyCost(State, State.Player.HeroPowerCardId, target, cost));
            evaluation.Cost = cost;

            if (target.CardKind == CardKind.TavernSpell && target.CardId == HastyExcavationCardId)
            {
                evaluation.CostsHealth = true;
                evaluation.HealthCostSource = Batch3HealthCostHastyExcavation;
            }
            else if (HasEquippedTrinketEffect(PilgrimpStickerEffectId) && target.CardKind == CardKind.Minion && HasTribe(target, Tribe.Demon))
            {
                ResetRoundCounter(PilgrimpRoundCounter, PilgrimpUsedCounter);
                if (GetAdvancedMechanicCounter(PilgrimpUsedCounter) <= 0)
                {
                    evaluation.CostsHealth = true;
                    evaluation.HealthCostSource = Batch3HealthCostPilgrimp;
                }
            }
            else if (HasEquippedTrinketEffect(BazaarStickerEffectId) && target.CardKind == CardKind.TavernSpell)
            {
                ResetRoundCounter(BazaarRoundCounter, BazaarUsedCounter);
                if (GetAdvancedMechanicCounter(BazaarUsedCounter) <= 0)
                {
                    evaluation.CostsHealth = true;
                    evaluation.HealthCostSource = Batch3HealthCostBazaar;
                }
            }
            else if (target.Tags != null && target.Tags.Contains(DemonicTapestryHealthCostTag))
            {
                evaluation.CostsHealth = true;
                evaluation.HealthCostSource = Batch3HealthCostDemonicTapestry;
            }

            if (HasEquippedTrinketEffect(EyeOfSargerasEffectId) &&
                GetAdvancedMechanicCounter(EyeOfSargerasBuyCounter) % 4 == 3)
            {
                evaluation.CostsHealth = true;
                evaluation.HealthCostSource = Batch3HealthCostEye;
            }

            if (IsGrifterFreePirate(target))
            {
                evaluation.Cost = 0;
                evaluation.CostsHealth = false;
                evaluation.HealthCostSource = null;
                evaluation.FreeCostSource = Batch3FreeCostGrifter;
            }

            return evaluation;
        }

        private int GetBaseBuyCost(MinionInstance target)
        {
            if (target == null)
            {
                return 0;
            }

            if (target.CardKind == CardKind.TavernSpell)
            {
                if (target.Counters != null && target.Counters.TryGetValue(BaseBuyCostCounter, out var storedCost))
                {
                    return Math.Max(0, storedCost);
                }

                if (target.Tags != null && target.Tags.Contains("generated_tavern_spell"))
                {
                    var instanceCost = Math.Max(0, target.Cost);
                    if (target.Counters == null)
                    {
                        target.Counters = new Dictionary<string, int>();
                    }

                    target.Counters[BaseBuyCostCounter] = instanceCost;
                    return instanceCost;
                }

                var definition = spellCatalog.All.FirstOrDefault(spell =>
                    string.Equals(spell.CardNumber, target.CardId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(spell.Id, target.CardId, StringComparison.OrdinalIgnoreCase));
                if (definition != null)
                {
                    return Math.Max(0, definition.Cost);
                }

                var generatedCost = Math.Max(0, target.Cost);
                if (target.Counters != null)
                {
                    target.Counters[BaseBuyCostCounter] = generatedCost;
                }

                return generatedCost;
            }

            return target.Cost > 0 ? target.Cost : BuyCost;
        }

        private void RecordBatch3PurchaseCostUsage(MinionInstance target, BuyCostEvaluation evaluation)
        {
            if (target == null)
            {
                return;
            }

            if (string.Equals(evaluation.HealthCostSource, Batch3HealthCostPilgrimp, StringComparison.OrdinalIgnoreCase))
            {
                SetAdvancedMechanicCounter(PilgrimpUsedCounter, 1);
            }

            if (string.Equals(evaluation.HealthCostSource, Batch3HealthCostBazaar, StringComparison.OrdinalIgnoreCase))
            {
                SetAdvancedMechanicCounter(BazaarUsedCounter, 1);
            }

            if (string.Equals(evaluation.FreeCostSource, Batch3FreeCostGrifter, StringComparison.OrdinalIgnoreCase))
            {
                SetAdvancedMechanicCounter(GrifterUsedCounter, 1);
            }

            if (HasEquippedTrinketEffect(EyeOfSargerasEffectId))
            {
                IncrementAdvancedMechanicCounter(EyeOfSargerasBuyCounter);
            }
        }

        private void BuyMinion(int shopIndex)
        {
            var tavern = State.Player.Tavern;
            TavernShopSlots.Ensure(tavern);
            if (shopIndex < 0 || shopIndex >= tavern.Shop.Count || tavern.Shop[shopIndex] == null)
            {
                throw new InvalidOperationException("目标商店槽位不存在。");
            }

            if (tavern.Hand.Count >= HandLimit)
            {
                throw new InvalidOperationException("手牌已满。");
            }

            var target = tavern.Shop[shopIndex];
            var evaluation = EvaluateBuyCost(target);
            var cost = evaluation.Cost;

            if (evaluation.CostsHealth)
            {
                if (State.Player.Health <= cost)
                {
                    throw new InvalidOperationException("生命值不足，无法购买。");
                }
            }
            else if (tavern.Gold < cost)
            {
                throw new InvalidOperationException("金币不足。");
            }

            var before = tavern.Gold;
            if (evaluation.CostsHealth)
            {
                DamagePlayerHero(cost);
            }
            else
            {
                SpendGold(cost);
            }

            target.Counters["last_purchase_cost"] = cost;
            tavern.Hand.Add(target);
            tavern.Shop[shopIndex] = null;
            TavernShopSlots.ClearSlot(tavern, shopIndex);
            RecordBatch3PurchaseCostUsage(target, evaluation);
            ApplyTrinketShopCostDisplays(tavern.Shop);
            HandleCardsAddedToHand(1, "buy");
            if (target.CardKind == CardKind.TavernSpell)
            {
                tavern.NextTavernSpellCostReduction = 0;
            }

            AddRecruitLog(RecruitLogType.Buy, "购买 " + target.Name, before, tavern.Gold);
            RecordQuestProgress(QuestObjectiveKind.BuyCards, 1);
            if (target.CardKind == CardKind.Minion)
            {
                RecordQuestProgress(QuestObjectiveKind.BuyMinions, 1);
            }

            if (target.CardKind == CardKind.TavernSpell)
            {
                RecordQuestProgress(QuestObjectiveKind.BuyTavernSpells, 1);
            }

            DispatchBoardEvent(MechanicEventType.CardBought);
            DispatchHeroEffect(HeroEffectEventType.CardBought, target, evaluation.CostsHealth ? 0 : cost);
            DispatchTrinketCardBought(target);
            DispatchQuestRewardCardBought(target);
            HandleCardBoughtForTierOneMinions();
            HandleCardBoughtForTierSixSevenMinions(target);
            ResolvePlayerTriples();
        }

        private void DiscardCardFromHand(int handIndex)
        {
            var tavern = State.Player.Tavern;
            if (handIndex < 0 || handIndex >= tavern.Hand.Count)
            {
                throw new InvalidOperationException("Target hand card does not exist.");
            }

            var discarded = tavern.Hand[handIndex];
            tavern.Hand.RemoveAt(handIndex);
            DispatchTrinketCardDiscarded(discarded);
            AddRecruitLog(
                RecruitLogType.Play,
                "Discarded " + (discarded?.Name ?? "a card") + ".",
                tavern.Gold,
                tavern.Gold);
        }

        private void PlayMinion(
            int handIndex,
            int targetIndex,
            TargetZone targetZone = TargetZone.Unspecified,
            int secondaryTargetIndex = -1,
            TargetZone secondaryTargetZone = TargetZone.Unspecified,
            string targetInstanceId = null,
            string secondaryTargetInstanceId = null,
            string choiceId = null)
        {
            var tavern = State.Player.Tavern;
            if (handIndex < 0 || handIndex >= tavern.Hand.Count)
            {
                throw new InvalidOperationException("目标手牌不存在。");
            }

            var target = tavern.Hand[handIndex];
            if (IsCardLocked(target))
            {
                throw new InvalidOperationException("这张牌被锁定，暂时不能打出。");
            }

            if (IsTripleRewardCard(target))
            {
                tavern.Hand.RemoveAt(handIndex);
                State.Player.Tavern.Discover = CreateTripleDiscover();
                AddRecruitLog(RecruitLogType.Discover, "Triple reward discover", tavern.Gold, tavern.Gold);
                return;
            }

            ValidateExplicitPlayTarget(target, targetIndex);

            if (target.CardKind == CardKind.TavernSpell || target.CardKind == CardKind.Spell)
            {
                var spellTargetId = ResolveFriendlyBoardTargetId(targetIndex);
                var spellTargetName = ResolveFriendlyBoardTargetName(targetIndex);
                tavern.Hand.RemoveAt(handIndex);
                string spellResult;
                if (TryCastQuestRewardSpell(target, targetIndex, out spellResult))
                {
                    HandleSpellCastOnTarget(target, spellTargetId, true);
                    DispatchTrinketSpellcraftCast(target);
                    DispatchTrinketSpellCast(target, true);
                    RecordQuestProgress(QuestObjectiveKind.CastSpells, 1);
                    HandleCardPlayedForTierFiveMinions(target);
                    HandleCardPlayedForTierSixSevenMinions(target);
                    AddRecruitLog(RecruitLogType.Play, "施放 " + target.Name + FormatTargetSuffix(spellTargetName) + " - " + spellResult, tavern.Gold, tavern.Gold);
                    return;
                }

                var dynamicBonus = GetBoardTavernSpellBonus();
                var perpetualBonus = target.CardKind == CardKind.TavernSpell ? GetPerpetualIncantationBonus() : (Attack: 0, Health: 0);
                var trinketBonus = target.CardKind == CardKind.TavernSpell ? GetTrinketTavernSpellBonus() : (Attack: 0, Health: 0);
                tavern.TavernSpellBonusAttack += dynamicBonus.Attack + perpetualBonus.Attack + trinketBonus.Attack;
                tavern.TavernSpellBonusHealth += dynamicBonus.Health + perpetualBonus.Health + trinketBonus.Health;
                var spellcraftCastCount = 1;
                try
                {
                    spellResult = TavernSpellEngine.Cast(target, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count), targetIndex, heroCatalog);
                    var extraCasts = GetTavernSpellExtraCasts(target);
                    spellcraftCastCount += extraCasts;
                    for (var extraCast = 0; extraCast < extraCasts; extraCast += 1)
                    {
                        spellResult += " + " + TavernSpellEngine.Cast(target, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count + extraCast + 1), targetIndex, heroCatalog);
                    }
                }
                finally
                {
                    tavern.TavernSpellBonusAttack -= dynamicBonus.Attack;
                    tavern.TavernSpellBonusHealth -= dynamicBonus.Health;
                    tavern.TavernSpellBonusAttack -= perpetualBonus.Attack;
                    tavern.TavernSpellBonusHealth -= perpetualBonus.Health;
                    tavern.TavernSpellBonusAttack -= trinketBonus.Attack;
                    tavern.TavernSpellBonusHealth -= trinketBonus.Health;
                }

                HandleSpellCastOnTarget(target, spellTargetId, true);
                DispatchTrinketSpellcraftCast(target, spellcraftCastCount);
                DispatchTrinketSpellCast(target, true);
                RecordQuestProgress(QuestObjectiveKind.CastSpells, 1);
                if (target.CardKind == CardKind.TavernSpell)
                {
                    tavern.TavernSpellsCastThisTurn = StatMath.SaturatingAdd(tavern.TavernSpellsCastThisTurn, 1, 0, StatMath.MaxStat);
                    tavern.TavernSpellsCastThisGame = StatMath.SaturatingAdd(tavern.TavernSpellsCastThisGame, 1, 0, StatMath.MaxStat);
                    tavern.CardsPlayedThisTurn = StatMath.SaturatingAdd(tavern.CardsPlayedThisTurn, 1, 0, StatMath.MaxStat);
                    tavern.LastTavernSpellCardId = target.CardId;
                    RecordQuestProgress(QuestObjectiveKind.CastTavernSpells, 1);
                    DispatchBoardEvent(MechanicEventType.TavernSpellCast);
                    HandleTavernSpellCastForTierThreeMinions(target);
                    HandleTavernSpellCastForTierFourMinions(target);
                    HandleTavernSpellCastForTierFiveMinions(target);
                    HandleTavernSpellCastForTierSixSevenMinions(target);
                    DispatchHeroEffect(HeroEffectEventType.TavernSpellCast, target);
                    DispatchTrinketTavernSpellCast(target, true);
                    ImprovePerpetualIncantation();
                }

                HandleCardPlayedForTierFiveMinions(target);
                HandleCardPlayedForTierSixSevenMinions(target);
                AddRecruitLog(RecruitLogType.Play, "施放 " + target.Name + FormatTargetSuffix(spellTargetName) + " - " + spellResult, tavern.Gold, tavern.Gold);
                return;
            }

            if (target.CardId != ScrapperCardId && TryPlayMagneticMinion(handIndex, target, targetIndex))
            {
                return;
            }

            if (State.Player.Board.Count >= BoardLimit)
            {
                throw new InvalidOperationException("战场已满。");
            }

            var battlecryTargetId = ResolveBattlecryTargetId(target, targetIndex);
            var battlecryTargetName = string.IsNullOrEmpty(battlecryTargetId) ? null : ResolveFriendlyBoardTargetName(targetIndex);

            tavern.Hand.RemoveAt(handIndex);
            target.Owner = BoardSide.Player;
            target.InstanceId = "player-" + target.DefinitionId + "-play-" + State.Round + "-" + handIndex;
            State.Player.Board.Insert(NormalizeBoardInsertIndex(targetIndex, State.Player.Board.Count), target);
            if (target.CardId == AncestralAutomatonCardId)
            {
                State.Player.Tavern.AncestralAutomatonSummons += 1;
                ApplyAncestralAutomatonBonuses();
            }

            if (target.CardId == EternalKnightCardId)
            {
                ApplyEternalKnightBonuses();
            }

            if (target.CardId == LavaLurkerCardId)
            {
                ResetPermanentSpellcraftCounter(target);
            }

            ResolveMinionBattlecry(target, battlecryTargetId);
            if (target.Keywords.Contains(Keyword.Battlecry))
            {
                RecordQuestProgress(QuestObjectiveKind.PlayBattlecryMinions, 1);
            }

            tavern.CardsPlayedThisTurn += 1;
            if (target.Tribes.Contains(Tribe.Elemental))
            {
                tavern.ElementalsPlayedThisTurn += 1;
            }

            HandleCardPlayedForTierFiveMinions(target);
            HandleCardPlayedForTierSixSevenMinions(target);
            DispatchSourceEvent(MechanicEventType.CardPlayed, target);
            DispatchHeroEffect(
                HeroEffectEventType.CardPlayed,
                target,
                targetIndex: targetIndex,
                targetZone: targetZone,
                secondaryTargetIndex: secondaryTargetIndex,
                secondaryTargetZone: secondaryTargetZone,
                targetInstanceId: targetInstanceId,
                secondaryTargetInstanceId: secondaryTargetInstanceId,
                choiceId: choiceId);
            DispatchTrinketMinionPlayed(target);
            AddRecruitLog(RecruitLogType.Play, "打出 " + target.Name + FormatTargetSuffix(battlecryTargetName), tavern.Gold, tavern.Gold);
            DispatchQuestRewardMinionPlayed(target);
            HandleDemonPlayedForWrathWeavers(target);
            HandleQuilboarPlayedForProphets(target);
            HandleMurlocPlayedForTierFourMinions(target);
            HandleMinionPlayedForTierFiveMinions(target);
            HandleMinionPlayedForTierSixSevenMinions(target);
            ResolveDiscoverThenDeath(target);
            if (target.Golden && !HasGrantedTripleReward(target))
            {
                MarkTripleRewardGranted(target);
                GrantTripleRewardCard();
            }

            ResolvePlayerTriples();
        }

        private void UseHeroPower(
            int targetIndex,
            TargetZone targetZone,
            int secondaryTargetIndex,
            TargetZone secondaryTargetZone,
            string targetInstanceId,
            string secondaryTargetInstanceId,
            string choiceId)
        {
            DispatchHeroEffect(
                HeroEffectEventType.HeroPowerUsed,
                targetIndex: targetIndex,
                targetZone: targetZone,
                secondaryTargetIndex: secondaryTargetIndex,
                secondaryTargetZone: secondaryTargetZone,
                targetInstanceId: targetInstanceId,
                secondaryTargetInstanceId: secondaryTargetInstanceId,
                choiceId: choiceId);
        }

        private string ResolveFriendlyBoardTargetId(int targetIndex)
        {
            return TryResolveFriendlyBoardTarget(targetIndex, out var target) ? target.InstanceId : null;
        }

        private string ResolveFriendlyBoardTargetName(int targetIndex)
        {
            return TryResolveFriendlyBoardTarget(targetIndex, out var target) ? target.Name : null;
        }

        private bool TryResolveFriendlyBoardTarget(int targetIndex, out MinionInstance target)
        {
            target = targetIndex >= 0 && targetIndex < State.Player.Board.Count
                ? State.Player.Board[targetIndex]
                : null;
            return target != null;
        }

        private string ResolveBattlecryTargetId(
            MinionInstance card,
            int targetIndex,
            TargetZone targetZone = TargetZone.Unspecified,
            string targetInstanceId = null)
        {
            if (card == null)
            {
                return null;
            }

            if (card.CardId != ScrapperCardId &&
                card.CardId != MutableBeetleCardId &&
                card.CardId != DisguisedGraverobberCardId)
            {
                return null;
            }

            if (targetZone != TargetZone.Unspecified && targetZone != TargetZone.FriendlyBoard)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(targetInstanceId))
            {
                var target = State.Player.Board.FirstOrDefault(minion => string.Equals(minion.InstanceId, targetInstanceId, StringComparison.OrdinalIgnoreCase));
                return target?.InstanceId;
            }

            return ResolveFriendlyBoardTargetId(targetIndex);
        }

        private void ValidateExplicitPlayTarget(MinionInstance card, int targetIndex)
        {
            if (card == null || targetIndex < 0)
            {
                return;
            }

            if (card.CardKind == CardKind.TavernSpell || card.CardKind == CardKind.Spell)
            {
                if (!TryResolveFriendlyBoardTarget(targetIndex, out var target))
                {
                    if (IsFriendlyBoardTargetedSpell(card.CardId))
                    {
                        throw new InvalidOperationException(card.Name + " needs a friendly board target.");
                    }

                    return;
                }

                ValidateTargetedSpellTarget(card, target);
                return;
            }

            if (card.CardId == DisguisedGraverobberCardId &&
                TryResolveFriendlyBoardTarget(targetIndex, out var battlecryTarget) &&
                !battlecryTarget.Tribes.Contains(Tribe.Undead))
            {
                throw new InvalidOperationException("Disguised Graverobber needs a friendly Undead target.");
            }
        }

        private static void ValidateTargetedSpellTarget(MinionInstance spell, MinionInstance target)
        {
            switch (spell.CardId)
            {
                case "100601":
                    if (target.TavernTier > 4)
                    {
                        throw new InvalidOperationException("Eyes of the Earth Mother needs a friendly Tier 4 or lower target.");
                    }

                    break;
                case ArcaneConsumptionCardId:
                    if (!target.Tribes.Contains(Tribe.Elemental))
                    {
                        throw new InvalidOperationException("Arcane Absorption needs a friendly Elemental target.");
                    }

                    break;
                case ButcheringCardNumber:
                    if (!target.Tribes.Contains(Tribe.Undead))
                    {
                        throw new InvalidOperationException("Butchering needs a friendly Undead target.");
                    }

                    break;
                case JailerStickerSpellCardId:
                    if (!target.Tribes.Contains(Tribe.Undead) && !target.Tribes.Contains(Tribe.All))
                    {
                        throw new InvalidOperationException("Jailer Sticker needs a friendly Undead target.");
                    }

                    break;
            }
        }

        private static bool IsFriendlyBoardTargetedSpell(string cardId)
        {
            switch (cardId)
            {
                case BloodGemCardId:
                case BristlebackBloodGemCardId:
                case RebornBloodGemCardId:
                case SlimyShieldCardId:
                case ReefRifferSpellCardId:
                case SurfNSurfSpellCardId:
                case DeepSeaAnglerSpellCardId:
                case DeepBlueSpellCardId:
                case VolcanicVisitorAttackSpellCardId:
                case VolcanicVisitorHealthSpellCardId:
                case TimewarpedGlowscaleSpellCardId:
                case WearyMageSpellCardId:
                case DoubleStitchNeedleSpellCardId:
                case TokenOfTheOldGodsSpellCardId:
                case JailerStickerSpellCardId:
                case DemonbloodGourdSpellCardId:
                case ShiftingTideSpellCardId:
                case DeepwaterSchoolCardId:
                case ArcaneConsumptionCardId:
                case EnhanceAMaticTauntSpellCardId:
                case EnhanceAMaticWindfurySpellCardId:
                case EnhanceAMaticDivineShieldSpellCardId:
                case EnhanceAMaticRebornSpellCardId:
                case RushingWindsSpellCardId:
                case TimelineAcceleratorSpellCardId:
                case GoldenHammerSpellCardId:
                case ButcheringCardNumber:
                case "100596":
                case "100601":
                case "100899":
                case "103791":
                case "103796":
                case "104445":
                case "104472":
                case "104601":
                case "105664":
                case "105667":
                case "105752":
                case "110642":
                case "113901":
                case "117573":
                case "119603":
                case "120900":
                case "130310":
                case "130312":
                case "131153":
                    return true;
                default:
                    return false;
            }
        }

        private static string FormatTargetSuffix(string targetName)
        {
            return string.IsNullOrEmpty(targetName) ? string.Empty : " -> " + targetName;
        }

        private void AddCardToHand(string cardId, CardKind cardKind)
        {
            var tavern = State.Player.Tavern;
            if (cardKind == CardKind.Hero)
            {
                var hero = heroCatalog.GetHeroByCardId(cardId);
                State.Player.HeroId = hero.HeroCardId;
                State.Player.HeroPowerCardId = hero.HeroPower?.CardId;
                State.Player.Health = hero.Health > 0 ? hero.Health : 30;
                State.Player.MaxHealth = State.Player.Health;
                State.Player.Armor = Math.Max(0, hero.Armor);
                AddRecruitLog(RecruitLogType.Discover, "Hero set: " + hero.Name, tavern.Gold, tavern.Gold);
                return;
            }

            if (cardKind == CardKind.HeroPower)
            {
                var power = heroCatalog.GetHeroPowerByCardId(cardId);
                State.Player.HeroPowerCardId = power.CardId;
                AddRecruitLog(RecruitLogType.Discover, "Hero Power set: " + power.Name, tavern.Gold, tavern.Gold);
                return;
            }

            if (tavern.Hand.Count >= HandLimit)
            {
                throw new InvalidOperationException("Hand is full.");
            }

            var card = CreateDebugCard(cardId, cardKind, "debug-hand-" + State.Round + "-" + tavern.Hand.Count);

            tavern.Hand.Add(card);
            HandleCardsAddedToHand(1, "debug");
            ApplyEternalKnightBonuses();
            ApplyAncestralAutomatonBonuses();
            ApplyFallenSkyGolemBonuses();
            AddRecruitLog(RecruitLogType.Buy, "Debug add " + card.Name, tavern.Gold, tavern.Gold);
        }

        private void CastDebugCard(string cardId, CardKind cardKind, int targetIndex)
        {
            if (cardKind == CardKind.Minion)
            {
                AddOpponentMinion(cardId);
                return;
            }

            if (cardKind != CardKind.TavernSpell && cardKind != CardKind.Spell)
            {
                throw new InvalidOperationException("Unsupported card kind: " + cardKind);
            }

            var tavern = State.Player.Tavern;
            var spell = CreateDebugCard(cardId, cardKind, "debug-cast-" + State.Round + "-" + tavern.RecruitLog.Count);
            var resolvedTargetIndex = ResolveDebugSpellTargetIndex(spell, targetIndex);
            ValidateExplicitPlayTarget(spell, resolvedTargetIndex);

            var spellTargetId = ResolveFriendlyBoardTargetId(resolvedTargetIndex);
            var spellTargetName = ResolveFriendlyBoardTargetName(resolvedTargetIndex);
            string spellResult;
            if (TryCastQuestRewardSpell(spell, resolvedTargetIndex, out spellResult))
            {
                HandleSpellCastOnTarget(spell, spellTargetId);
                DispatchTrinketSpellcraftCast(spell);
                DispatchTrinketSpellCast(spell);
                HandleCardPlayedForTierFiveMinions(spell);
                HandleCardPlayedForTierSixSevenMinions(spell);
                AddRecruitLog(RecruitLogType.Play, "Debug cast " + spell.Name + FormatTargetSuffix(spellTargetName) + " - " + spellResult, tavern.Gold, tavern.Gold);
                return;
            }

            var dynamicBonus = GetBoardTavernSpellBonus();
            var perpetualBonus = spell.CardKind == CardKind.TavernSpell ? GetPerpetualIncantationBonus() : (Attack: 0, Health: 0);
            var trinketBonus = spell.CardKind == CardKind.TavernSpell ? GetTrinketTavernSpellBonus() : (Attack: 0, Health: 0);
            tavern.TavernSpellBonusAttack += dynamicBonus.Attack + perpetualBonus.Attack + trinketBonus.Attack;
            tavern.TavernSpellBonusHealth += dynamicBonus.Health + perpetualBonus.Health + trinketBonus.Health;
            var spellcraftCastCount = 1;
            try
            {
                spellResult = TavernSpellEngine.Cast(spell, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count), resolvedTargetIndex, heroCatalog);
                var extraCasts = GetTavernSpellExtraCasts(spell);
                spellcraftCastCount += extraCasts;
                for (var extraCast = 0; extraCast < extraCasts; extraCast += 1)
                {
                    spellResult += " + " + TavernSpellEngine.Cast(spell, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count + extraCast + 1), resolvedTargetIndex, heroCatalog);
                }
            }
            finally
            {
                tavern.TavernSpellBonusAttack -= dynamicBonus.Attack;
                tavern.TavernSpellBonusHealth -= dynamicBonus.Health;
                tavern.TavernSpellBonusAttack -= perpetualBonus.Attack;
                tavern.TavernSpellBonusHealth -= perpetualBonus.Health;
                tavern.TavernSpellBonusAttack -= trinketBonus.Attack;
                tavern.TavernSpellBonusHealth -= trinketBonus.Health;
            }

            HandleSpellCastOnTarget(spell, spellTargetId);
            DispatchTrinketSpellcraftCast(spell, spellcraftCastCount);
            DispatchTrinketSpellCast(spell);
            if (spell.CardKind == CardKind.TavernSpell)
            {
                tavern.TavernSpellsCastThisTurn = StatMath.SaturatingAdd(tavern.TavernSpellsCastThisTurn, 1, 0, StatMath.MaxStat);
                tavern.TavernSpellsCastThisGame = StatMath.SaturatingAdd(tavern.TavernSpellsCastThisGame, 1, 0, StatMath.MaxStat);
                tavern.CardsPlayedThisTurn = StatMath.SaturatingAdd(tavern.CardsPlayedThisTurn, 1, 0, StatMath.MaxStat);
                tavern.LastTavernSpellCardId = spell.CardId;
                DispatchBoardEvent(MechanicEventType.TavernSpellCast);
                HandleTavernSpellCastForTierThreeMinions(spell);
                HandleTavernSpellCastForTierFourMinions(spell);
                HandleTavernSpellCastForTierFiveMinions(spell);
                HandleTavernSpellCastForTierSixSevenMinions(spell);
                DispatchHeroEffect(HeroEffectEventType.TavernSpellCast, spell);
                DispatchTrinketTavernSpellCast(spell, false);
                ImprovePerpetualIncantation();
            }

            HandleCardPlayedForTierFiveMinions(spell);
            HandleCardPlayedForTierSixSevenMinions(spell);
            AddRecruitLog(RecruitLogType.Play, "Debug cast " + spell.Name + FormatTargetSuffix(spellTargetName) + " - " + spellResult, tavern.Gold, tavern.Gold);
        }

        private int ResolveDebugSpellTargetIndex(MinionInstance spell, int targetIndex)
        {
            if (targetIndex >= 0 || spell == null || !IsFriendlyBoardTargetedSpell(spell.CardId) || State.Player.Board.Count == 0)
            {
                return targetIndex;
            }

            var candidates = new List<int>();
            for (var index = 0; index < State.Player.Board.Count; index += 1)
            {
                if (IsValidDebugSpellTarget(spell, State.Player.Board[index]))
                {
                    candidates.Add(index);
                }
            }

            if (candidates.Count == 0)
            {
                return targetIndex;
            }

            var rng = new SeededRng(State.Seed + State.Round * 1879 + State.Player.Tavern.RecruitLog.Count);
            return candidates[rng.NextInt(candidates.Count)];
        }

        private static bool IsValidDebugSpellTarget(MinionInstance spell, MinionInstance target)
        {
            try
            {
                ValidateTargetedSpellTarget(spell, target);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private MinionInstance CreateDebugCard(string cardId, CardKind cardKind, string instanceId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                throw new InvalidOperationException("Card id is required.");
            }

            MinionInstance card;
            if (cardKind == CardKind.Minion)
            {
                var definition = catalog.GetByCardId(cardId);
                card = MinionFactory.Create(definition, BoardSide.Player, instanceId, false, PoolSource.Debug, 0);
            }
            else if (cardKind == CardKind.TavernSpell)
            {
                var definition = spellCatalog.All.FirstOrDefault(spell =>
                    spell.CardNumber == cardId ||
                    spell.Id == cardId ||
                    (!string.IsNullOrEmpty(spell.ImagePath) && spell.ImagePath.EndsWith("/" + cardId, StringComparison.OrdinalIgnoreCase)));
                if (definition == null && IsBountyCardId(cardId))
                {
                    card = CreateGeneratedSpellCard(cardId, instanceId);
                    card.PoolSource = PoolSource.Debug;
                    card.OriginPoolSource = PoolSource.Debug;
                }
                else if (definition == null)
                {
                    throw new InvalidOperationException("Spell card id does not exist: " + cardId);
                }
                else
                {
                    card = MinionFactory.Create(definition, BoardSide.Player, instanceId);
                    card.PoolSource = PoolSource.Debug;
                    card.OriginPoolSource = PoolSource.Debug;
                }
            }
            else if (cardKind == CardKind.Spell)
            {
                card = CreateGeneratedSpellCard(cardId, instanceId);
                card.PoolSource = PoolSource.Debug;
                card.OriginPoolSource = PoolSource.Debug;
            }
            else if (cardKind == CardKind.HeroBuddy)
            {
                var definition = heroCatalog.GetBuddyByCardId(cardId);
                card = MinionFactory.Create(definition, BoardSide.Player, instanceId, PoolSource.Debug);
            }
            else if (cardKind == CardKind.HeroPower)
            {
                var definition = heroCatalog.GetHeroPowerByCardId(cardId);
                card = MinionFactory.Create(definition, BoardSide.Player, instanceId);
            }
            else
            {
                throw new InvalidOperationException("Unsupported card kind: " + cardKind);
            }

            return card;
        }

        private void DispatchSourceEvent(MechanicEventType eventType, MinionInstance source)
        {
            var dispatcher = new EffectDispatcher(effectCatalog, new SeededRng(State.Seed + State.Round * 1009 + State.Player.Tavern.RecruitLog.Count));
            dispatcher.Dispatch(new EffectDispatchContext
            {
                EventType = eventType,
                Source = source,
                Tavern = State.Player.Tavern,
                FriendlyBoard = State.Player.Board,
                FriendlyHand = State.Player.Tavern.Hand,
                FriendlyShop = State.Player.Tavern.Shop
            });
        }

        private void DispatchBoardEvent(MechanicEventType eventType)
        {
            var snapshot = State.Player.Board.ToList();
            foreach (var minion in snapshot)
            {
                DispatchSourceEvent(eventType, minion);
            }
        }

        private void DispatchHeroEffect(
            HeroEffectEventType eventType,
            MinionInstance card = null,
            int goldCost = 0,
            int targetIndex = -1,
            MinionInstance targetCard = null,
            TargetZone targetZone = TargetZone.Unspecified,
            int secondaryTargetIndex = -1,
            TargetZone secondaryTargetZone = TargetZone.Unspecified,
            string targetInstanceId = null,
            string secondaryTargetInstanceId = null,
            string choiceId = null)
        {
            var result = HeroEffectEngine.Dispatch(new HeroEffectContext
            {
                EventType = eventType,
                State = State,
                Heroes = heroCatalog,
                Minions = catalog,
                Spells = spellCatalog,
                Rng = new SeededRng(State.Seed + State.Round * 2017 + State.Player.Tavern.RecruitLog.Count),
                Card = card,
                GoldCost = goldCost,
                TargetIndex = targetIndex,
                TargetZone = targetZone,
                SecondaryTargetIndex = secondaryTargetIndex,
                SecondaryTargetZone = secondaryTargetZone,
                TargetInstanceId = targetInstanceId,
                SecondaryTargetInstanceId = secondaryTargetInstanceId,
                ChoiceId = choiceId,
                TargetCard = targetCard,
                BattlecryResolver = ReplayBattlecryForHeroEffect
            });

            foreach (var message in result.Messages)
            {
                AddRecruitLog(RecruitLogType.Play, message, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }
        }

        private void DispatchCombatRewardHeroEffect(HeroEffectEventType eventType, CombatReward reward)
        {
            if (reward == null)
            {
                return;
            }

            var source = ResolveCombatRewardSource(reward);
            var target = ResolveCombatRewardTarget(reward);
            var repeats = Math.Max(1, reward.Amount);
            for (var i = 0; i < repeats; i += 1)
            {
                DispatchHeroEffect(eventType, source, targetCard: target);
            }
        }

        private MinionInstance ResolveCombatRewardSource(CombatReward reward)
        {
            if (reward == null || string.IsNullOrEmpty(reward.SourceCardId))
            {
                return null;
            }

            var source = string.IsNullOrEmpty(reward.SourceInstanceId)
                ? null
                : State.Player.Board.FirstOrDefault(card => string.Equals(card.InstanceId, reward.SourceInstanceId, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                source = State.Player.Board.FirstOrDefault(card => string.Equals(card.CardId, reward.SourceCardId, StringComparison.OrdinalIgnoreCase));
            }

            if (source != null)
            {
                return source;
            }

            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = string.IsNullOrEmpty(reward.SourceInstanceId) ? "combat-" + reward.SourceCardId : reward.SourceInstanceId,
                DefinitionId = reward.SourceCardId,
                CardId = reward.SourceCardId,
                Name = reward.SourceCardId,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0
            };
        }

        private MinionInstance ResolveCombatRewardTarget(CombatReward reward)
        {
            if (reward == null || string.IsNullOrEmpty(reward.CardId))
            {
                return null;
            }

            if (reward.Type == CombatRewardType.FriendlyMinionSummoned)
            {
                var summonAttack = Math.Max(0, reward.Attack);
                var summonHealth = Math.Max(1, reward.Health);
                return new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = string.IsNullOrEmpty(reward.TargetInstanceId) ? "combat-summon-" + reward.CardId : reward.TargetInstanceId,
                    DefinitionId = reward.CardId,
                    CardId = reward.CardId,
                    Name = reward.CardId,
                    BaseAttack = summonAttack,
                    BaseHealth = summonHealth,
                    Attack = summonAttack,
                    Health = summonHealth,
                    MaxHealth = summonHealth,
                    TavernTier = Math.Max(0, reward.TavernTier),
                    Tribes = new List<Tribe> { Tribe.None },
                    Keywords = new List<Keyword>(),
                    Owner = BoardSide.Player,
                    PoolSource = PoolSource.Summon,
                    PoolCopiesHeld = 0
                };
            }

            var target = string.IsNullOrEmpty(reward.TargetInstanceId)
                ? null
                : State.Opponent.Board.FirstOrDefault(card => string.Equals(card.InstanceId, reward.TargetInstanceId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                target = State.Opponent.Board.FirstOrDefault(card => string.Equals(card.CardId, reward.CardId, StringComparison.OrdinalIgnoreCase));
            }

            if (target != null)
            {
                return target;
            }

            var definition = catalog.All.FirstOrDefault(card => string.Equals(card.CardId, reward.CardId, StringComparison.OrdinalIgnoreCase));
            if (definition != null)
            {
                var generated = MinionFactory.Create(
                    definition,
                    BoardSide.Player,
                    "combat-kill-target-" + State.Round + "-" + State.Player.Tavern.Hand.Count,
                    source: PoolSource.Copy,
                    poolCopiesHeld: 0);
                if (!string.IsNullOrEmpty(reward.TargetInstanceId))
                {
                    generated.InstanceId = reward.TargetInstanceId;
                }

                return generated;
            }

            var attack = Math.Max(0, reward.Attack);
            var health = Math.Max(1, reward.Health);
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = string.IsNullOrEmpty(reward.TargetInstanceId) ? "combat-target-" + reward.CardId : reward.TargetInstanceId,
                DefinitionId = reward.CardId,
                CardId = reward.CardId,
                Name = reward.CardId,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Owner = BoardSide.Opponent,
                PoolSource = PoolSource.Debug,
                PoolCopiesHeld = 0
            };
        }

        private void SellMinion(string instanceId)
        {
            var target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId == instanceId);
            if (target == null)
            {
                throw new InvalidOperationException("要出售的随从不在玩家战场。");
            }

            var tavern = State.Player.Tavern;
            var before = tavern.Gold;
            var sellValue = target.CardId == FreedealingGamblerCardId ? 3 : SellValue;
            if (target.CardId == BlueshellTurtleCardId && tavern.LostLastCombat)
            {
                sellValue = 5;
            }

            tavern.Gold = Math.Min(tavern.MaxGold, tavern.Gold + sellValue);
            DispatchSourceEvent(MechanicEventType.MinionSold, target);
            ResolveTierOneSellEffect(target);
            ResolveTierFourSellEffect(target);
            ResolveTierSixSevenSellEffect(target);
            DispatchHeroEffect(HeroEffectEventType.MinionSold, target);
            DispatchQuestRewardMinionSold(target);
            DispatchTrinketMinionSold(target);
            State.Player.Board.Remove(target);
            ReleaseMinionToPool(target);
            MaybeOfferShadyAristocratQuest(target);
            AddRecruitLog(RecruitLogType.Sell, "出售 " + target.Name, before, tavern.Gold);
        }

        private void DispatchTrinketMinionSold(MinionInstance sold)
        {
            if (sold == null || sold.CardKind != CardKind.Minion)
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            AdvanceTrinketSoldMinionCounterReward("lava_lamp", ref trinkets.LavaLampSoldMinions, 5, Tribe.Elemental, "Lava Lamp");
            AdvanceTrinketSoldMinionCounterReward("fungalmancer_sticker", ref trinkets.FungalmancerStickerSoldMinions, 5, Tribe.Murloc, "Fungalmancer Sticker");
            AdvanceTrinketSoldMinionTavernSpellReward("avalanche_sticker", ref trinkets.AvalancheStickerSoldMinions, 4, MountingAvalancheCardNumber, "Avalanche Sticker");
            if (TryConsumeTrinketFirstSoldMinionThisRound("gem_donation", ref trinkets.GemDonationSoldRound))
            {
                ApplyGemDonation();
            }

            if (HasEquippedTrinketEffect(DarnassusPieEffectId) || HasEquippedTrinketEffect(DarnassusPieDoubleEffectId))
            {
                trinkets.DarnassusPieSoldMinionsThisTurn = Math.Max(0, trinkets.DarnassusPieSoldMinionsThisTurn) + 1;
                ApplyTrinketShopAuras(State.Player.Tavern.Shop);
            }

            if (HasEquippedTrinketEffect(WindfallPortraitEffectId))
            {
                IncrementAdvancedMechanicCounter(WindfallSoldThisTurnCounter);
            }
        }

        private bool TryConsumeTrinketFirstSoldMinionThisRound(string effectId, ref int soldRound)
        {
            if (!HasEquippedTrinketEffect(effectId) || soldRound == State.Round)
            {
                return false;
            }

            soldRound = State.Round;
            return true;
        }

        private void ApplyGemDonation()
        {
            var targets = State.Player.Tavern.Shop
                .Where(card => card != null && card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.TavernTier)
                .Take(3)
                .ToList();
            if (targets.Count <= 0)
            {
                return;
            }

            var attack = 1 + State.Player.Tavern.BloodGemBonusAttack;
            var health = 1 + State.Player.Tavern.BloodGemBonusHealth;
            foreach (var target in targets)
            {
                BuffMinion(target, attack, health, "Gem Donation Blood Gem");
            }

            AddRecruitLog(
                RecruitLogType.Play,
                "Gem Donation: played Blood Gems on " + targets.Count + " highest-Tier Tavern minion(s).",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void AdvanceTrinketSoldMinionCounterReward(string effectId, ref int counter, int threshold, Tribe rewardTribe, string source)
        {
            if (!HasEquippedTrinketEffect(effectId))
            {
                return;
            }

            AdvanceTrinketCounterReward(ref counter, 1, threshold, rewardTribe, source);
        }

        private void AdvanceTrinketSoldMinionTavernSpellReward(string effectId, ref int counter, int threshold, string cardNumber, string source)
        {
            if (!HasEquippedTrinketEffect(effectId))
            {
                return;
            }

            counter = Math.Max(0, counter) + 1;
            var rewards = counter / threshold;
            counter %= threshold;
            if (rewards <= 0)
            {
                return;
            }

            var added = AddTavernSpellToHand(cardNumber, rewards, source);
            AddRecruitLog(
                RecruitLogType.Play,
                source + ": counter completed " + rewards + " time(s), added " + added + " Tavern spell(s).",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void MaybeOfferShadyAristocratQuest(MinionInstance target)
        {
            if (target == null ||
                questCatalog == null ||
                !string.Equals(target.CardId, ShadyAristocratCardId, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(target.CardId, GoldenShadyAristocratCardId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var advanced = EnsureAdvancedMechanicState(State.Player.Tavern);
            if (advanced.PendingChoice != null)
            {
                AddRecruitLog(RecruitLogType.Discover, "Shady Aristocrat could not offer a Quest because another advanced choice is pending.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                return;
            }

            var rewardId = target.Golden || string.Equals(target.CardId, GoldenShadyAristocratCardId, StringComparison.OrdinalIgnoreCase)
                ? GoldenShadyCoinPouchRewardId
                : ShadyCoinPouchRewardId;
            OfferQuestChoice(1, "shady-aristocrat", "Bonus", rewardId);
        }

        private void MoveMinionToHand(string instanceId)
        {
            var target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId == instanceId);
            if (target == null)
            {
                throw new InvalidOperationException("Target minion is not on the player board.");
            }

            var tavern = State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                throw new InvalidOperationException("Hand is full.");
            }

            State.Player.Board.Remove(target);
            target.Owner = BoardSide.Player;
            target.InstanceId = "player-" + target.DefinitionId + "-return-" + State.Round + "-" + tavern.Hand.Count;
            tavern.Hand.Add(target);
            AddRecruitLog(RecruitLogType.Play, "Return " + target.Name, tavern.Gold, tavern.Gold);
        }

        private void MoveBoardMinion(string instanceId, int targetIndex)
        {
            var target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId == instanceId);
            if (target == null)
            {
                throw new InvalidOperationException("Target minion is not on the player board.");
            }

            State.Player.Board.Remove(target);
            State.Player.Board.Insert(NormalizeBoardInsertIndex(targetIndex, State.Player.Board.Count), target);
            AddRecruitLog(RecruitLogType.Play, "Reorder " + target.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void AddOpponentMinion(string cardId, bool golden = false)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                throw new InvalidOperationException("Opponent minion card id is required.");
            }

            if (State.Opponent.Board.Count >= BoardLimit)
            {
                throw new InvalidOperationException("Opponent board is full.");
            }

            var definition = catalog.GetByCardId(cardId);
            var minion = MinionFactory.Create(definition, BoardSide.Opponent, "debug-board-" + State.Round + "-" + State.Opponent.Board.Count, golden, PoolSource.Debug, 0);
            State.Opponent.Board.Add(minion);
            AddRecruitLog(RecruitLogType.Play, "Debug opponent add " + minion.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void RemoveOpponentMinion(string instanceId)
        {
            var target = State.Opponent.Board.FirstOrDefault(minion => minion.InstanceId == instanceId);
            if (target == null)
            {
                throw new InvalidOperationException("Target minion is not on the opponent board.");
            }

            State.Opponent.Board.Remove(target);
            AddRecruitLog(RecruitLogType.Play, "Debug opponent remove " + target.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void MoveOpponentMinion(string instanceId, int targetIndex)
        {
            var target = State.Opponent.Board.FirstOrDefault(minion => minion.InstanceId == instanceId);
            if (target == null)
            {
                throw new InvalidOperationException("Target minion is not on the opponent board.");
            }

            State.Opponent.Board.Remove(target);
            State.Opponent.Board.Insert(NormalizeBoardInsertIndex(targetIndex, State.Opponent.Board.Count), target);
            AddRecruitLog(RecruitLogType.Play, "Debug opponent reorder " + target.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void UpdateOpponentMinion(string instanceId, MinionPatch patch)
        {
            if (string.IsNullOrEmpty(instanceId) || patch == null)
            {
                throw new InvalidOperationException("Target minion is not on the opponent board.");
            }

            if (!UpdateMinionInList(State.Opponent.Board, instanceId, patch))
            {
                throw new InvalidOperationException("Target minion is not on the opponent board.");
            }
        }

        private void ClearOpponentBoard()
        {
            State.Opponent.Board.Clear();
            AddRecruitLog(RecruitLogType.Play, "Debug opponent board cleared", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void CopyPlayerBoardToOpponent(bool mirrored)
        {
            State.Opponent.Board.Clear();
            var source = mirrored
                ? State.Player.Board.AsEnumerable().Reverse()
                : State.Player.Board.AsEnumerable();
            var index = 0;
            foreach (var minion in source.Take(BoardLimit))
            {
                var copy = minion.Clone();
                copy.Owner = BoardSide.Opponent;
                copy.InstanceId = "opponent-copy-" + State.Round + "-" + index + "-" + copy.DefinitionId;
                copy.PoolSource = PoolSource.Debug;
                copy.PoolCopiesHeld = 0;
                State.Opponent.Board.Add(copy);
                index += 1;
            }

            AddRecruitLog(
                RecruitLogType.Play,
                mirrored ? "Debug opponent mirrored from player board" : "Debug opponent copied from player board",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private static int NormalizeBoardInsertIndex(int targetIndex, int currentCount)
        {
            if (targetIndex < 0)
            {
                return currentCount;
            }

            return Math.Min(Math.Max(0, targetIndex), currentCount);
        }

        private void FreezeShop(bool frozen)
        {
            TavernShopSlots.SetAllFrozen(State.Player.Tavern, frozen);
        }

        private int GetCurrentShopMinionSize()
        {
            return HeroEffectEngine.ModifyShopSize(
                State.Player.HeroPowerCardId,
                TavernRules.GetShopSize(State.Player.Tavern.Tier));
        }

        private int GetCurrentShopMinimumTier()
        {
            return HasEquippedTrinketEffect("bob_blehead") ? 3 : TavernRules.MinTavernTier;
        }

        private bool RefreshShopFromPoolPreservingFrozen(int seed, string suffix)
        {
            var tavern = State.Player.Tavern;
            TavernShopSlots.Ensure(tavern);
            var shopSize = GetCurrentShopMinionSize();
            var minimumTier = GetCurrentShopMinimumTier();
            if (!TavernShopSlots.HasAnyFrozenSlot(tavern))
            {
                var drawn = CreateShopFromPool(ReleaseShopToPool(), tavern.Tier, shopSize, seed, suffix, minimumTier: minimumTier);
                TavernShopSlots.ReplaceShop(tavern, drawn.Shop);
                tavern.Pool = drawn.Pool;
                return true;
            }

            var targetSlotCount = Math.Max(tavern.Shop.Count, shopSize);
            while (tavern.Shop.Count < targetSlotCount)
            {
                tavern.Shop.Add(null);
            }

            TavernShopSlots.Ensure(tavern);
            var replacementsNeeded = 0;
            for (var index = 0; index < tavern.Shop.Count; index += 1)
            {
                if (!TavernShopSlots.IsSlotFrozen(tavern, index))
                {
                    replacementsNeeded += 1;
                }
            }

            if (replacementsNeeded <= 0)
            {
                return false;
            }

            var released = ReleaseShopToPool(releaseFrozenSlots: false);
            var drawnReplacements = CreateShopFromPool(released, tavern.Tier, replacementsNeeded, seed, suffix, includeTavernSpell: false, minimumTier: minimumTier);
            var replacementIndex = 0;
            for (var index = 0; index < tavern.Shop.Count; index += 1)
            {
                if (TavernShopSlots.IsSlotFrozen(tavern, index))
                {
                    continue;
                }

                tavern.Shop[index] = replacementIndex < drawnReplacements.Shop.Count
                    ? drawnReplacements.Shop[replacementIndex]
                    : null;
                replacementIndex += 1;
            }

            tavern.Pool = drawnReplacements.Pool;
            TavernShopSlots.Ensure(tavern);
            return true;
        }

        private bool RefreshShopWithBattlecryMinionsForOneCost(string source)
        {
            var tavern = State.Player.Tavern;
            TavernShopSlots.Ensure(tavern);
            var shopSize = GetCurrentShopMinionSize();
            var minimumTier = GetCurrentShopMinimumTier();
            var pool = new MinionPool(catalog.All, ReleaseShopToPool(), CurrentActiveTribes(), cardPoolAvailability.AllowsMinion);
            var rng = new SeededRng(State.Seed + State.Round * 6151 + tavern.RecruitLog.Count);
            var shop = new List<MinionInstance>();
            for (var index = 0; index < shopSize; index += 1)
            {
                var candidates = AvailableMinions()
                    .Where(definition =>
                        definition.InPool &&
                        definition.TavernTier >= minimumTier &&
                        definition.TavernTier <= tavern.Tier &&
                        definition.Keywords.Contains(Keyword.Battlecry) &&
                        pool.Remaining(definition.Id) > 0)
                    .ToList();
                if (candidates.Count == 0)
                {
                    break;
                }

                var picked = rng.Pick(candidates);
                pool.Occupy(picked.Id);
                var minion = MinionFactory.Create(picked, BoardSide.Player, "chillmere-" + State.Round + "-" + index, false, PoolSource.Pool, 1);
                minion.Cost = 1;
                if (!minion.Tags.Contains("chillmere_mosaic_cost_1"))
                {
                    minion.Tags.Add("chillmere_mosaic_cost_1");
                }

                shop.Add(minion);
            }

            TavernShopSlots.ReplaceShop(tavern, shop);
            tavern.Pool = pool.Snapshot();
            if (shop.Count <= 0)
            {
                TavernShopSlots.Ensure(tavern);
                return false;
            }

            ApplyShopGrowth(tavern.Shop, tavern.Growth.ShopModifiers);
            InjectRefreshCards(tavern.Shop);
            ApplyRefreshBuffToShop(tavern.Shop);
            ApplyRefreshRightmostBuffToShop(tavern.Shop);
            ApplyHelpfulRefresh(tavern.Shop);
            RecordTrinketShopRefresh();
            ApplyTrinketRefreshResultModifiers(tavern.Shop);
            ApplyTrinketShopAuras(tavern.Shop);
            DispatchTrinketShopRefreshed(tavern.Shop);
            RecordQuestProgress(QuestObjectiveKind.RefreshShop, 1);
            DispatchQuestRewardShopRefreshed(tavern.Shop);
            DispatchBoardEvent(MechanicEventType.ShopRefreshed);
            HandleShopRefreshedForTierThreeMinions();
            DispatchHeroEffect(HeroEffectEventType.ShopRefreshed);
            TavernShopSlots.Ensure(tavern);
            AddRecruitLog(RecruitLogType.Reroll, source + ": refreshed the Tavern with Battlecry minions costing 1.", tavern.Gold, tavern.Gold);
            return true;
        }

        private void RerollShop()
        {
            var tavern = State.Player.Tavern;
            var costsHealth = tavern.FreeRefreshes <= 0 && tavern.HealthCostRefreshes > 0;
            var cost = tavern.FreeRefreshes > 0
                ? 0
                : costsHealth
                    ? RerollCost
                    : HeroEffectEngine.ModifyRefreshCost(State, State.Player.HeroPowerCardId, RerollCost);
            if (!costsHealth && tavern.Gold < cost)
            {
                throw new InvalidOperationException("金币不足，无法刷新。");
            }

            if (costsHealth && State.Player.Health <= cost)
            {
                throw new InvalidOperationException("Health is too low to refresh.");
            }

            var before = tavern.Gold;
            if (tavern.FreeRefreshes > 0)
            {
                tavern.FreeRefreshes -= 1;
            }

            if (costsHealth)
            {
                DamagePlayerHero(cost);
                tavern.HealthCostRefreshes -= 1;
            }
            else
            {
                SpendGold(cost);
            }
            HeroEffectEngine.RecordRefreshCostPaid(State, State.Player.HeroPowerCardId, cost);
            var refreshed = RefreshShopFromPoolPreservingFrozen(State.Seed + State.Round * 101 + before, "reroll-" + State.Round + "-" + before);
            if (refreshed)
            {
                ApplyShopGrowth(tavern.Shop, tavern.Growth.ShopModifiers);
                InjectRefreshCards(tavern.Shop);
                ApplyRefreshBuffToShop(tavern.Shop);
                ApplyRefreshRightmostBuffToShop(tavern.Shop);
                ApplyHelpfulRefresh(tavern.Shop);
                RecordTrinketShopRefresh();
                ApplyTrinketRefreshResultModifiers(tavern.Shop);
                ApplyTrinketShopAuras(tavern.Shop);
                DispatchTrinketShopRefreshed(tavern.Shop);
                RecordQuestProgress(QuestObjectiveKind.RefreshShop, 1);
                DispatchQuestRewardShopRefreshed(tavern.Shop);
                DispatchBoardEvent(MechanicEventType.ShopRefreshed);
                HandleShopRefreshedForTierThreeMinions();
                DispatchHeroEffect(HeroEffectEventType.ShopRefreshed);
                TavernShopSlots.Ensure(tavern);
            }
            tavern.SearchPlan.GoldSpentOnRerollThisTurn += cost;
            AddRecruitLog(RecruitLogType.Reroll, "刷新酒馆", before, tavern.Gold);
        }

        private void UpgradeTavern()
        {
            var tavern = State.Player.Tavern;
            if (tavern.Tier >= TavernRules.MaxTavernTier)
            {
                throw new InvalidOperationException("酒馆等级已满。");
            }

            var upgradeCost = HeroEffectEngine.ModifyUpgradeCost(State, State.Player.HeroPowerCardId, tavern.UpgradeCost);
            if (tavern.Gold < upgradeCost)
            {
                throw new InvalidOperationException("金币不足，无法升级。");
            }

            var before = tavern.Gold;
            var spent = upgradeCost;
            SpendGold(spent);
            tavern.Tier += 1;
            tavern.UpgradeCost = tavern.Tier >= TavernRules.MaxTavernTier ? 0 : TavernRules.GetUpgradeCost(tavern.Tier);
            var refund = HeroEffectEngine.HandleOmuUpgradeRefund(State, spent);
            if (refund > 0)
            {
                AddRecruitLog(RecruitLogType.Play, "Forest Warden Omu: refunded " + refund + " Gold.", before, tavern.Gold);
            }
            AddRecruitLog(RecruitLogType.LevelUp, "升级到 " + tavern.Tier + " 本", before, tavern.Gold);
            DispatchTrinketTavernTierReached();
        }

        private void NextTurn()
        {
            DispatchBoardEvent(MechanicEventType.TurnEnded);
            HandleTurnEndedForTierOneMinions();
            HandleTurnEndedForTierThreeMinions();
            HandleTurnEndedForTierFourMinions();
            HandleTurnEndedForTierFiveMinions();
            HandleTurnEndedForTierSixSevenMinions();
            DispatchHeroEffect(HeroEffectEventType.TurnEnded);
            DispatchTrinketTurnEnded();
            SetAdvancedMechanicCounter(CliffdiverBattlecryThisTurnCounter, 0);
            SetAdvancedMechanicCounter(WindfallSoldThisTurnCounter, 0);
            DispatchQuestRewardTurnEnded();
            var tavern = State.Player.Tavern;
            var nextRound = State.Round + 1;
            var trinkets = EnsureTrinketState(tavern);
            var maxGold = HeroEffectEngine.ModifyTurnMaxGold(State, TavernRules.GetMaxGoldForRound(nextRound)) + Math.Max(0, trinkets.ExtraMaxGold);
            var bonusGold = tavern.NextTurnBonusGold;
            ResetTealTigerSapphireTurnState();
            trinkets.DarnassusPieSoldMinionsThisTurn = 0;
            var refreshed = RefreshShopFromPoolPreservingFrozen(State.Seed + nextRound * 997, "turn-" + nextRound);

            State.Round = nextRound;
            State.Phase = MatchPhase.Tavern;
            tavern.Gold = maxGold + bonusGold;
            tavern.MaxGold = maxGold;
            tavern.NextTurnBonusGold = 0;
            tavern.UpgradeCost = TavernRules.DecrementUpgradeCost(tavern.UpgradeCost);
            if (refreshed)
            {
                ApplyShopGrowth(tavern.Shop, tavern.Growth.ShopModifiers);
                InjectRefreshCards(tavern.Shop);
                ApplyRefreshBuffToShop(tavern.Shop);
                ApplyRefreshRightmostBuffToShop(tavern.Shop);
                ApplyHelpfulRefresh(tavern.Shop);
                EnsureTrinketShopMinimumCards(GetTrinketMinimumShopCards(), "Trinkets");
                ApplyTrinketShopAuras(tavern.Shop);
                ApplyLubberStickerRefreshOffers(tavern.Shop);
                DispatchHeroEffect(HeroEffectEventType.ShopRefreshed);
                TavernShopSlots.Ensure(tavern);
            }
            else
            {
                EnsureTrinketShopMinimumCards(GetTrinketMinimumShopCards(), "Trinkets");
                ApplyTrinketShopAuras(tavern.Shop);
            }

            tavern.TavernSpellsCastThisTurn = 0;
            tavern.CardsPlayedThisTurn = 0;
            tavern.ElementalsPlayedThisTurn = 0;
            tavern.GoldSpentThisTurn = 0;
            trinkets.FelburnedLedgerBonusThisTurn = 0;
            tavern.SearchPlan.GoldSpentOnRerollThisTurn = 0;
            tavern.SearchPlan.HitsThisTurn.Clear();
            TickHandLocks();
            TickPatientScouts();
            ClearTemporarySpellcraftEffects();
            ResetPermanentSpellcraftCounters();
            State.CombatLog.Clear();
            State.LastResult = null;
            State.LastReplay = null;
            AddRecruitLog(RecruitLogType.TurnStart, "第 " + nextRound + " 回合开始", 0, tavern.Gold);
            DispatchTrinketTurnStarted();
            MaybeOfferScheduledTrinketChoice();
            AddSpellcraftFromBoard();
            DispatchBoardEvent(MechanicEventType.TurnStarted);
            DispatchHeroEffect(HeroEffectEventType.TurnStarted);
            DispatchQuestRewardTurnStarted();
            HandleTurnStartedForTierThreeMinions();
        }

        private void ClearTemporarySpellcraftEffects()
        {
            foreach (var card in State.Player.Board.Concat(State.Player.Tavern.Hand).Concat(State.Player.Tavern.Shop.Where(card => card != null)))
            {
                ClearTemporarySpellcraftEffects(card);
            }

            State.Player.Tavern.Hand.RemoveAll(card => card.Tags != null && card.Tags.Contains("temporary_spellcraft_card"));
        }

        private static void ClearTemporarySpellcraftEffects(MinionInstance card)
        {
            if (card == null)
            {
                return;
            }

            var temporaryEnchantments = card.Enchantments.Where(enchantment => enchantment.SourceId == TemporarySpellcraftSourceId).ToList();
            foreach (var enchantment in temporaryEnchantments)
            {
                StatMath.ApplyStatDeltaPreservingDamage(
                    card,
                    StatMath.SaturatingSubtract(0, enchantment.AttackBonus),
                    StatMath.SaturatingSubtract(0, enchantment.HealthBonus));
                card.Enchantments.Remove(enchantment);
            }

            RemoveTemporarySpellcraftKeyword(card, TemporaryToughTuskDivineShieldTag, Keyword.DivineShield);
            if (!card.Tags.Contains("temporary_spellcraft"))
            {
                if (card.Tags.Remove("temporary_venomous"))
                {
                    card.Keywords.Remove(Keyword.Venomous);
                }

                return;
            }

            card.Tags.Remove("temporary_spellcraft");
            if (card.Tags.Remove("temporary_venomous"))
            {
                card.Keywords.Remove(Keyword.Venomous);
            }

            RemoveTemporarySpellcraftKeyword(card, "temporary_spellcraft_added_reborn", Keyword.Reborn);
            RemoveTemporarySpellcraftKeyword(card, "temporary_spellcraft_added_taunt", Keyword.Taunt);
            RemoveTemporarySpellcraftKeyword(card, "temporary_spellcraft_added_divine_shield", Keyword.DivineShield);
            RemoveTemporarySpellcraftKeyword(card, "temporary_spellcraft_added_windfury", Keyword.Windfury);
            RemoveTemporarySpellcraftKeyword(card, TemporaryToughTuskDivineShieldTag, Keyword.DivineShield);
            card.Tags.Remove("surf_n_surf_crab");
            card.Counters.Remove("surf_crab_attack");
            card.Counters.Remove("surf_crab_health");
            if (card.Tags.Remove("temporary_spellcraft_added_deathrattle"))
            {
                card.Keywords.Remove(Keyword.Deathrattle);
            }
        }

        private static void RemoveTemporarySpellcraftKeyword(MinionInstance card, string tag, Keyword keyword)
        {
            if (card.Tags.Remove(tag))
            {
                card.Keywords.Remove(keyword);
            }
        }

        private void ResetPermanentSpellcraftCounters()
        {
            foreach (var lavaLurker in State.Player.Board.Where(card => card.CardId == LavaLurkerCardId))
            {
                ResetPermanentSpellcraftCounter(lavaLurker);
            }
        }

        private static void ResetPermanentSpellcraftCounter(MinionInstance lavaLurker)
        {
            lavaLurker.Counters[PermanentSpellcraftCounter] = lavaLurker.Golden ? 2 : 1;
        }

        private void AddSpellcraftFromBoard()
        {
            foreach (var source in State.Player.Board.ToList())
            {
                if (State.Player.Tavern.Hand.Count >= HandLimit)
                {
                    return;
                }

                switch (source.CardId)
                {
                    case DeepSeaAnglerCardId:
                        AddGeneratedSpellsToHand(DeepSeaAnglerSpellCardId, 1, "spellcraft-" + source.InstanceId);
                        State.Player.Tavern.Hand.Last().Counters["angler_attack"] = source.Golden ? 4 : 2;
                        State.Player.Tavern.Hand.Last().Counters["angler_health"] = source.Golden ? 12 : 6;
                        break;
                    case DeepBlueCroonerCardId:
                        AddGeneratedSpellsToHand(DeepBlueSpellCardId, 1, "spellcraft-" + source.InstanceId);
                        State.Player.Tavern.Hand.Last().Counters["deep_blue_attack"] = (source.Golden ? 4 : 2) + State.Player.Tavern.DeepBlueBonusAttack;
                        State.Player.Tavern.Hand.Last().Counters["deep_blue_health"] = (source.Golden ? 4 : 2) + State.Player.Tavern.DeepBlueBonusHealth;
                        State.Player.Tavern.Hand.Last().Counters["deep_blue_growth"] = source.Golden ? 2 : 1;
                        break;
                    case ReefRifferCardId:
                        AddGeneratedSpellsToHand(ReefRifferSpellCardId, 1, "spellcraft-" + source.InstanceId);
                        State.Player.Tavern.Hand.Last().Counters["spellcraft_multiplier"] = source.Golden ? 2 : 1;
                        break;
                    case SurfNSurfCardId:
                        AddGeneratedSpellsToHand(SurfNSurfSpellCardId, 1, "spellcraft-" + source.InstanceId);
                        State.Player.Tavern.Hand.Last().Counters["crab_attack"] = source.Golden ? 6 : 3;
                        State.Player.Tavern.Hand.Last().Counters["crab_health"] = source.Golden ? 4 : 2;
                        break;
                    case VolcanicVisitorCardId:
                        AddGeneratedSpellsToHand(VolcanicVisitorAttackSpellCardId, 1, "spellcraft-" + source.InstanceId);
                        State.Player.Tavern.Hand.Last().Counters["spellcraft_amount"] = source.Golden ? 8 : 4;
                        if (State.Player.Tavern.Hand.Count < HandLimit)
                        {
                            AddGeneratedSpellsToHand(VolcanicVisitorHealthSpellCardId, 1, "spellcraft-" + source.InstanceId);
                            State.Player.Tavern.Hand.Last().Counters["spellcraft_amount"] = source.Golden ? 8 : 4;
                        }

                        break;
                    case FrostlingPriestessCardId:
                        AddGeneratedSpellsToHand(FrostlingPriestessSpellCardId, 1, "spellcraft-" + source.InstanceId);
                        State.Player.Tavern.Hand.Last().Counters["spellcraft_multiplier"] = source.Golden ? 2 : 1;
                        break;
                    case SereneMeditatorCardId:
                        AddTavernSpellToHand("131153", "spellcraft-" + source.InstanceId);
                        break;
                    case DarkcrestStrategistCardId:
                        AddRandomTierOneNagaToHand(source.Golden ? 2 : 1, "spellcraft-" + source.InstanceId);
                        source.Counters.TryGetValue("darkcrest_bonus", out var darkcrestBonus);
                        source.Counters["darkcrest_bonus"] = darkcrestBonus + (source.Golden ? 2 : 1);
                        break;
                    case GlowscaleCardId:
                        var handBefore = State.Player.Tavern.Hand.Count;
                        AddGeneratedSpellsToHand(SlimyShieldCardId, source.Golden ? 2 : 1, "spellcraft-" + source.InstanceId);
                        foreach (var added in State.Player.Tavern.Hand.Skip(handBefore))
                        {
                            added.Tags.Add("divine_shield_spell");
                            added.Text = "Spellcraft: Give a minion Divine Shield until next turn.";
                        }

                        break;
                    case TimewarpedGlowscaleCardId:
                        AddGeneratedSpellsToHand(TimewarpedGlowscaleSpellCardId, source.Golden ? 2 : 1, "spellcraft-" + source.InstanceId);
                        break;
                    case WearyMageCardId:
                        var wearyHandBefore = State.Player.Tavern.Hand.Count;
                        AddGeneratedSpellsToHand(WearyMageSpellCardId, source.Golden ? 2 : 1, "spellcraft-" + source.InstanceId);
                        if (HasEquippedTrinketEffect("weary_portrait"))
                        {
                            foreach (var added in State.Player.Tavern.Hand.Skip(wearyHandBefore))
                            {
                                if (!added.Tags.Contains("permanent_weary_spellcraft"))
                                {
                                    added.Tags.Add("permanent_weary_spellcraft");
                                }
                            }
                        }

                        break;
                    case ThaumaturgistCardId:
                        var thaumaturgistHandBefore = State.Player.Tavern.Hand.Count;
                        AddGeneratedSpellsToHand(ThaumaturgistSpellCardId, 1, "spellcraft-" + source.InstanceId);
                        var spellAmount = (source.Golden ? 2 : 1) * (1 + (Math.Max(0, GetAdvancedMechanicCounter(AllSpellsCastThisGameCounter)) >> 2));
                        foreach (var added in State.Player.Tavern.Hand.Skip(thaumaturgistHandBefore))
                        {
                            added.Counters["spellcraft_amount"] = spellAmount;
                            if (HasEquippedTrinketEffect("thaumaturgist_portrait") && !added.Tags.Contains("permanent_thaumaturgist_spellcraft"))
                            {
                                added.Tags.Add("permanent_thaumaturgist_spellcraft");
                            }
                        }

                        break;
                    case SeaWitchZarJiraCardId:
                        AddCopiesOfShopMinionsToHand(source.Golden ? 2 : 1, "zarjira");
                        break;
                }
            }
        }

        private void ChooseDiscover(int optionIndex)
        {
            var discover = State.Player.Tavern.Discover;
            if (discover == null || optionIndex < 0 || optionIndex >= discover.Options.Count)
            {
                throw new InvalidOperationException("发现奖励不存在。");
            }

            var picked = discover.Options[optionIndex];
            if (ResolveBatch4Discover(discover, picked))
            {
                return;
            }

            if (discover.Source == "quest-second-hero-power" && picked.CardKind == CardKind.HeroPower)
            {
                AddSecondHeroPower(picked);
                State.Player.Tavern.Discover = null;
                return;
            }

            if (!string.IsNullOrEmpty(discover.Source) && discover.Source.StartsWith("quest-magicfin:", StringComparison.Ordinal))
            {
                ResolveMagicfinRelicDiscover(discover, picked);
                State.Player.Tavern.Discover = null;
                return;
            }

            if (discover.Source == "hero-power:unmasked-identity" || picked.CardKind == CardKind.HeroPower)
            {
                State.Player.HeroPowerCardId = picked.CardId;
                AddRecruitLog(RecruitLogType.Discover, "Hero Power changed: " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                State.Player.Tavern.Discover = null;
                return;
            }

            if (!string.IsNullOrEmpty(discover.Source) && discover.Source.StartsWith("sprightly-scarab:", StringComparison.Ordinal))
            {
                ResolveSprightlyScarabChoice(discover, picked);
                AddRecruitLog(RecruitLogType.Discover, "Sprightly Scarab choice " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                State.Player.Tavern.Discover = null;
                return;
            }

            if (discover.Source == "fearless-foodie")
            {
                ResolveFearlessFoodieChoice(picked);
                AddRecruitLog(RecruitLogType.Discover, "Fearless Foodie choice " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                State.Player.Tavern.Discover = null;
                return;
            }

            if (!string.IsNullOrEmpty(discover.Source) && discover.Source.StartsWith("doomsday-dragon-egg:", StringComparison.Ordinal))
            {
                HatchDoomsdayDragonEgg(discover.Source.Substring("doomsday-dragon-egg:".Length), picked);
                AddRecruitLog(RecruitLogType.Discover, "Doomsday Dragon Egg hatched into " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                State.Player.Tavern.Discover = null;
                return;
            }

            if (discover.Source == "scrapper-magnetic")
            {
                ResolveScrapperMagneticChoice(discover, picked);
                var remaining = Math.Max(0, discover.RemainingPicks - 1);
                var targetInstanceId = discover.TargetInstanceId;
                AddRecruitLog(RecruitLogType.Discover, "Scrapper magnetized " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                State.Player.Tavern.Discover = null;
                if (remaining > 0)
                {
                    StartScrapperMagneticDiscover(null, targetInstanceId, remaining);
                }

                return;
            }

            if (discover.Source == "hero-power:galakronds-greed")
            {
                ResolveGalakrondChoice(discover, picked);
                DispatchDiscoverChosenEffect(discover, picked);
                AddRecruitLog(RecruitLogType.Discover, "Galakrond replacement " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                State.Player.Tavern.Discover = null;
                return;
            }

            DispatchDiscoverChosenEffect(discover, picked);
            var remainingBookPicks = !string.IsNullOrEmpty(discover.Source) &&
                discover.Source.StartsWith("trinket:book_of_medivh:", StringComparison.Ordinal)
                    ? Math.Max(0, discover.RemainingPicks - 1)
                    : 0;
            State.Player.Tavern.Hand.Add(picked);
            HandleCardsAddedToHand(1, "discover");
            DispatchQuestRewardDiscoverChosen(picked);
            DispatchTrinketDiscoverChosen(picked);
            if (discover.Source == "prickly-piper")
            {
                DamagePlayerHero(Math.Max(1, picked.TavernTier));
            }

            AddRecruitLog(RecruitLogType.Discover, "发现 " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            State.Player.Tavern.Discover = null;
            if (remainingBookPicks > 0)
            {
                StartTavernSpellDiscover(remainingBookPicks, discover.Source, "Book of Medivh");
            }
        }

        private void DispatchDiscoverChosenEffect(DiscoverState discover, MinionInstance picked)
        {
            var result = HeroEffectEngine.Dispatch(new HeroEffectContext
            {
                EventType = HeroEffectEventType.DiscoverChosen,
                State = State,
                Heroes = heroCatalog,
                Minions = catalog,
                Spells = spellCatalog,
                Rng = new SeededRng(State.Seed + State.Round * 2027 + State.Player.Tavern.RecruitLog.Count),
                Card = picked,
                DiscoverSource = discover?.Source
            });
            foreach (var message in result.Messages)
            {
                AddRecruitLog(RecruitLogType.Discover, message, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }
        }

        private void ResolveGalakrondChoice(DiscoverState discover, MinionInstance picked)
        {
            var tavern = State.Player.Tavern;
            var slot = discover.RemainingPicks;
            if (slot < 0 ||
                slot >= tavern.Shop.Count ||
                tavern.Shop[slot] == null ||
                !string.Equals(tavern.Shop[slot].InstanceId, discover.TargetInstanceId, StringComparison.OrdinalIgnoreCase))
            {
                slot = tavern.Shop.FindIndex(card =>
                    card != null &&
                    string.Equals(card.InstanceId, discover.TargetInstanceId, StringComparison.OrdinalIgnoreCase));
            }

            if (slot < 0)
            {
                throw new InvalidOperationException("Galakrond's Greed target is no longer in the Tavern.");
            }

            picked.Owner = BoardSide.Player;
            picked.PoolSource = PoolSource.Copy;
            picked.OriginPoolSource = PoolSource.Copy;
            picked.PoolCopiesHeld = 0;
            tavern.Shop[slot] = picked;
        }

        private void UpdateMinion(string instanceId, MinionPatch patch)
        {
            if (string.IsNullOrEmpty(instanceId) || patch == null)
            {
                throw new InvalidOperationException("目标随从不存在。");
            }

            var updated = false;
            updated |= UpdateMinionInList(State.Player.Board, instanceId, patch);
            updated |= UpdateMinionInList(State.Opponent.Board, instanceId, patch);
            updated |= UpdateMinionInList(State.Player.Tavern.Hand, instanceId, patch);
            updated |= UpdateMinionInList(State.Player.Tavern.Shop, instanceId, patch);

            if (State.Player.Tavern.Discover != null)
            {
                updated |= UpdateMinionInList(State.Player.Tavern.Discover.Options, instanceId, patch);
            }

            if (!updated)
            {
                throw new InvalidOperationException("目标随从不存在。");
            }
        }

        private static bool UpdateMinionInList(List<MinionInstance> minions, string instanceId, MinionPatch patch)
        {
            var updated = false;
            foreach (var minion in minions)
            {
                if (minion == null || minion.InstanceId != instanceId)
                {
                    continue;
                }

                if (patch.Attack.HasValue)
                {
                    minion.Attack = Math.Max(0, patch.Attack.Value);
                }

                if (patch.MaxHealth.HasValue)
                {
                    minion.MaxHealth = Math.Max(1, patch.MaxHealth.Value);
                }

                if (patch.Health.HasValue)
                {
                    minion.Health = Math.Max(1, patch.Health.Value);
                }

                minion.Health = Math.Min(minion.Health, minion.MaxHealth);

                if (patch.Golden.HasValue)
                {
                    minion.Golden = patch.Golden.Value;
                }

                if (patch.Keywords != null)
                {
                    minion.Keywords = new List<Keyword>(patch.Keywords);
                }

                if (patch.Tribes != null)
                {
                    minion.Tribes = new List<Tribe>(patch.Tribes);
                }

                updated = true;
            }

            return updated;
        }

        private void SimulateCombat()
        {
            RunCombatTest(new CombatTestOptions
            {
                Seed = State.Seed + State.Round,
                SafetyLimit = 200
            });
        }

        private void SaveTestScenario(string scenarioName)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                throw new InvalidOperationException("Scenario name is required.");
            }

            var scenario = TestScenarioMapper.Capture(State, scenarioName.Trim());
            scenarioRepository.Save(scenario);
            AddRecruitLog(RecruitLogType.Play, "保存测试场景 " + scenario.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void LoadTestScenario(string scenarioName)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                throw new InvalidOperationException("Scenario name is required.");
            }

            var scenario = scenarioRepository.Load(scenarioName.Trim());
            TestScenarioMapper.ApplyTo(State, scenario);
            CurrentActiveTribes();
            combatTestSnapshot = null;
            State.LastReplay = null;
            AddRecruitLog(RecruitLogType.Play, "加载测试场景 " + scenario.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void RunCombatTest(CombatTestOptions options)
        {
            var nextOptions = options ?? new CombatTestOptions();
            if (nextOptions.SafetyLimit <= 0)
            {
                nextOptions.SafetyLimit = 200;
            }

            if (nextOptions.Seed == 0)
            {
                nextOptions.Seed = State.Seed + State.Round;
            }

            if (nextOptions.ResetBeforeRun && combatTestSnapshot?.BeforeCombat != null)
            {
                TestScenarioMapper.ApplyTo(State, combatTestSnapshot.BeforeCombat);
            }

            combatTestSnapshot = new CombatTestSnapshot
            {
                BeforeCombat = TestScenarioMapper.Capture(State, "__before_combat__"),
                Options = new CombatTestOptions
                {
                    Seed = nextOptions.Seed,
                    ResetBeforeRun = nextOptions.ResetBeforeRun,
                    SafetyLimit = nextOptions.SafetyLimit
                }
            };

            var playerCombatBoard = CreateCombatStartPlayerBoard();
            var opponentCombatBoard = State.Opponent.Board.Select(minion => minion.Clone()).ToList();
            var heroCombatResult = HeroEffectEngine.ApplyCombatStartEffects(new HeroCombatEffectContext
            {
                State = State,
                Minions = catalog,
                Rng = new SeededRng(State.Seed + State.Round * 3037 + State.Player.Tavern.RecruitLog.Count),
                PlayerBoard = playerCombatBoard,
                OpponentBoard = opponentCombatBoard
            });
            foreach (var message in heroCombatResult.Messages)
            {
                AddRecruitLog(RecruitLogType.Play, message, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            }
            PrepareTrinketCombatStartEffects(playerCombatBoard);
            PrepareQuestCombatStartEffects(playerCombatBoard);

            CaptureLastOpponentWarband(opponentCombatBoard);

            var result = CombatEngine.SimulateBasicCombat(
                playerCombatBoard,
                opponentCombatBoard,
                nextOptions.Seed,
                nextOptions.SafetyLimit,
                State.Player.Tavern,
                null,
                State.Player.Tavern.Hand);
            State.Phase = MatchPhase.Result;
            State.CombatLog = result.Log;
            State.LastResult = result;
            State.LastReplay = result.Replay;
            combatTestSnapshot.Result = result;
            ApplyPermanentCombatBuffs(result);
            ApplyCombatOutcomeRewards(result);
            ApplyCombatRewards(result.PlayerRewards);
            ApplyTideRaiserPortraitCombatRewards(result);
            PersistQuestCombatRewards();
            CaptureRecentCombatDeaths(result);
            DispatchQuestRewardAfterCombat(LastDeadFriendlyMinion());
            State.Player.Tavern.LostLastCombat = result.Winner == CombatWinner.Opponent;
            State.Player.Tavern.TemporaryAvengeBeastRewards = 0;
            State.Player.Tavern.NextCombatBoardAttack = 0;
            State.Player.Tavern.NextCombatBoardHealth = 0;
            State.Player.Tavern.NextCombatBeetles = 0;
            State.Player.Tavern.NextCombatEnemyHealthToOne = 0;
            State.Player.Tavern.NextCombatLeftmostCopiesNearestEnemyStats = false;
            State.Player.Tavern.NextCombatLeftmostDoubleAttack = false;
            State.Player.Tavern.NextCombatTriggerMixedMechanics = false;
            State.Player.Tavern.CombatSummonBonusAttack = 0;
            State.Player.Tavern.CombatSummonBonusHealth = 0;
            State.Player.Tavern.CombatSummonTaunt = false;
            State.Player.Tavern.CombatSummonDoubleStats = false;
            State.Player.Tavern.CombatSameTierSummonBuffTier = 0;
            State.Player.Tavern.CombatSameTierSummonBuffAttack = 0;
            State.Player.Tavern.CombatSameTierSummonBuffHealth = 0;
            ResetTrinketCombatState();
            ResetQuestCombatState();
        }

        private void ResetTrinketCombatState()
        {
            var tavern = State.Player.Tavern;
            tavern.TrinketBirdFeederAvengeThreshold = 0;
            tavern.TrinketBirdFeederAttack = 0;
            tavern.TrinketBirdFeederHealth = 0;
            tavern.TrinketBeetleBandAvengeThreshold = 0;
            tavern.TrinketBeetleBandSummonCount = 0;
            tavern.TrinketQuilligraphyAvengeThreshold = 0;
            tavern.TrinketQuilligraphyAttack = 0;
            tavern.TrinketQuilligraphyHealth = 0;
            tavern.TrinketWickedTomeAvengeThreshold = 0;
            tavern.TrinketWickedTomeAttack = 0;
            tavern.TrinketWickedTomeHealth = 0;
            tavern.TrinketStaffOfTheScourgeAvengeThreshold = 0;
            tavern.TrinketCloudSerpentHornAvengeThreshold = 0;
            tavern.TrinketFridgeMagnetAvengeThreshold = 0;
            tavern.TrinketBattleHornAvengeThreshold = 0;
            tavern.TrinketBristlebachPortraitActive = false;
            tavern.TrinketCombatBeastSummonBonusAttack = 0;
            tavern.TrinketCombatBeastSummonBonusHealth = 0;
            tavern.TrinketSlammaStickerActive = false;
            tavern.TrinketBassgillPortraitActive = false;
            tavern.TrinketReinforcedShieldUses = 0;
            tavern.TrinketTwinSkyLanternCopies = 0;
            tavern.TrinketCeremonialSwordAttack = 0;
            tavern.TrinketFaerieDragonScaleUses = 0;
            tavern.TrinketAllianceKeychainTargets = 0;
            tavern.TrinketDeathlyPhylacteryExtraDeathrattles = 0;
            tavern.TrinketHeraldStickerActive = false;
            tavern.TrinketRylakPortraitActive = false;
            tavern.TrinketDivineSignetUses = 0;
            tavern.TrinketMechagonAdapterUses = 0;
            tavern.TrinketDeathtouchAppleUses = 0;
            tavern.TrinketTarecgosaStickerActive = false;
            tavern.TrinketUnholySanctumAttack = 0;
            tavern.TrinketUnholySanctumHealth = 0;
            tavern.TrinketUnholySanctumSourceCardId = null;
            tavern.TrinketFishyStickerActive = false;
            tavern.TrinketSoulFermenterActive = false;
            tavern.TrinketBelcherPortraitAttack = 0;
            tavern.TrinketBelcherPortraitHealth = 0;
            tavern.TrinketBelcherPortraitSourceCardId = null;
            tavern.TrinketBoomControllerActive = false;
            tavern.TrinketBloodGolemStickerActive = false;
            tavern.TrinketBloodAmuletActive = false;
            tavern.TrinketAllPurposeKibbleAttack = 0;
            tavern.TrinketSTharaStickerActive = false;
            tavern.TrinketStartOfCombatExtraTriggers = 0;
            tavern.TrinketPromoPortraitExtraTriggers = 0;
            tavern.TrinketJarredFrostlingTargets = 0;
            tavern.TrinketPowderKegTargets = 0;
            tavern.TrinketHoggyBankActive = false;
            tavern.TrinketRustyTridentTriggers = 0;
            tavern.TrinketSkyGolemDeathrattleTriggers = 0;
            tavern.TrinketJarOGemsAttackThreshold = 0;
            tavern.TrinketJarOGemsAttackCounter = 0;
            tavern.TrinketElementiumChestAttackThreshold = 0;
            tavern.TrinketElementiumChestAttackCounter = 0;
            tavern.TrinketGilneanRoseAvengeThreshold = 0;
            tavern.TrinketGilneanRoseAttack = 0;
            tavern.TrinketGilneanRoseHealth = 0;
            tavern.TrinketTigerCarvingAttack = 0;
            tavern.TrinketTigerCarvingHealth = 0;
            tavern.TrinketThornspikePauldronAttack = 0;
            tavern.TrinketThornspikePauldronHealth = 0;
            tavern.TrinketMugOfTheSireActive = false;
            tavern.TrinketBlingtronSunglassesActive = false;
            tavern.TrinketScrapsmithPortraitActive = false;
            tavern.TrinketEyeOfDalaranActive = false;
        }

        private void ResetQuestCombatState()
        {
            var tavern = State.Player.Tavern;
            tavern.QuestFriendlyAttackAura = 0;
            tavern.QuestVolatileVenomActive = false;
            tavern.QuestBoomSquadActive = false;
            tavern.QuestGrimFreshenerActive = false;
            tavern.QuestCycleOfEnergyActive = false;
            tavern.QuestStableAmalgamationActive = false;
            tavern.QuestDeathrattleExtraTriggers = 0;
            tavern.QuestTumblingAttack = 0;
            tavern.QuestTumblingHealth = 0;
            tavern.QuestTumblingAvengeAttack = 0;
            tavern.QuestTumblingAvengeHealth = 0;
        }

        private void CaptureLastOpponentWarband(IReadOnlyList<MinionInstance> opponentCombatBoard)
        {
            var history = EnsureOpponentHistory();
            history.LastOpponentHeroId = State.Opponent.HeroId;
            history.LastOpponentRound = State.Round;
            history.LastOpponentTavernTier = State.Opponent.TavernTier;
            history.LastOpponentWarband = CloneHistoryBoard(opponentCombatBoard, BoardSide.Opponent);
            if (State.Opponent.Health <= 0 && history.LastOpponentWarband.Count > 0)
            {
                AddEliminatedOpponentWarbandSnapshot(history);
            }
        }

        private void CaptureRecentCombatDeaths(CombatOutput result)
        {
            EnsureOpponentHistory().RecentCombatDeaths = CreateCombatDeathHistory(result);
        }

        private MinionInstance LastDeadFriendlyMinion()
        {
            return EnsureOpponentHistory().RecentCombatDeaths
                .LastOrDefault(minion => minion != null && minion.Owner == BoardSide.Player);
        }

        private OpponentHistoryState EnsureOpponentHistory()
        {
            if (State.OpponentHistory == null)
            {
                State.OpponentHistory = new OpponentHistoryState();
            }

            return State.OpponentHistory;
        }

        private static List<MinionInstance> CloneHistoryBoard(IEnumerable<MinionInstance> board, BoardSide owner)
        {
            return (board ?? Enumerable.Empty<MinionInstance>())
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion)
                .Take(BoardLimit)
                .Select(minion =>
                {
                    var copy = minion.Clone();
                    copy.Owner = owner;
                    copy.PoolSource = PoolSource.Copy;
                    copy.OriginPoolSource = PoolSource.Copy;
                    copy.PoolCopiesHeld = 0;
                    return copy;
                })
                .ToList();
        }

        private void AddEliminatedOpponentWarbandSnapshot(OpponentHistoryState history)
        {
            if (history.EliminatedPlayerWarbands.Any(snapshot =>
                    snapshot != null &&
                    snapshot.Round == State.Round &&
                    string.Equals(snapshot.HeroId, history.LastOpponentHeroId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            history.EliminatedPlayerWarbands.Add(new OpponentWarbandSnapshot
            {
                HeroId = history.LastOpponentHeroId,
                Round = State.Round,
                TavernTier = history.LastOpponentTavernTier,
                Eliminated = true,
                Warband = CloneHistoryBoard(history.LastOpponentWarband, BoardSide.Opponent)
            });

            while (history.EliminatedPlayerWarbands.Count > BoardLimit)
            {
                history.EliminatedPlayerWarbands.RemoveAt(0);
            }
        }

        private static List<MinionInstance> CreateCombatDeathHistory(CombatOutput result)
        {
            var deaths = new List<MinionInstance>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var frame in result?.Replay?.Frames ?? Enumerable.Empty<CombatFrame>())
            {
                if (frame?.DeadEntityIds == null)
                {
                    continue;
                }

                foreach (var entityId in frame.DeadEntityIds)
                {
                    if (string.IsNullOrEmpty(entityId) || !seen.Add(entityId))
                    {
                        continue;
                    }

                    if (TryCreateDeathHistoryMinion(frame.PlayerBoardSnapshot, entityId, out var playerDeath) ||
                        TryCreateDeathHistoryMinion(frame.OpponentBoardSnapshot, entityId, out playerDeath))
                    {
                        deaths.Add(playerDeath);
                    }
                }
            }

            return deaths;
        }

        private static bool TryCreateDeathHistoryMinion(CombatBoardSnapshot snapshot, string instanceId, out MinionInstance minion)
        {
            minion = null;
            var source = snapshot?.Minions?.FirstOrDefault(card =>
                string.Equals(card.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            if (source == null)
            {
                return false;
            }

            minion = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = source.InstanceId,
                DefinitionId = string.IsNullOrEmpty(source.CardId) ? source.InstanceId : source.CardId,
                CardId = source.CardId,
                Name = source.Name,
                BaseAttack = source.BaseAttack,
                BaseHealth = source.BaseHealth,
                Attack = source.Attack,
                Health = Math.Max(0, source.Health),
                MaxHealth = source.MaxHealth,
                TavernTier = source.TavernTier,
                Golden = source.Golden,
                CanAttack = source.CanAttack,
                AttacksThisCombat = source.AttacksThisCombat,
                Owner = snapshot.Side,
                Tribes = new List<Tribe>(source.Tribes ?? new List<Tribe>()),
                Keywords = new List<Keyword>(source.Keywords ?? new List<Keyword>()),
                OfficialKeywords = new List<Keyword>(source.Keywords ?? new List<Keyword>()),
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string>(source.Tags ?? new List<string>())
            };
            return true;
        }

        private void ApplyCombatOutcomeRewards(CombatOutput result)
        {
            if (result == null)
            {
                return;
            }

            if (result.Winner == CombatWinner.Player && State.Player.Tavern.PendingCombatWinGold > 0)
            {
                State.Player.Tavern.NextTurnBonusGold += State.Player.Tavern.PendingCombatWinGold;
            }
            else if (result.Winner == CombatWinner.Draw && State.Player.Tavern.PendingCombatDrawGold > 0)
            {
                State.Player.Tavern.NextTurnBonusGold += State.Player.Tavern.PendingCombatDrawGold;
            }

            State.Player.Tavern.PendingCombatWinGold = 0;
            State.Player.Tavern.PendingCombatDrawGold = 0;
        }

        private void ApplyPermanentCombatBuffs(CombatOutput result)
        {
            if (result == null)
            {
                return;
            }

            foreach (var original in State.Player.Board.Where(minion => minion.CardId == TarecgosaCardId))
            {
                var final = result.FinalPlayerBoard.FirstOrDefault(minion => minion.InstanceId == original.InstanceId);
                if (final == null)
                {
                    continue;
                }

                var multiplier = original.Golden ? 2 : 1;
                var attackDelta = StatMath.SaturatingMultiply(Math.Max(0, StatMath.SaturatingDelta(final.Attack, original.Attack)), multiplier, 0, StatMath.MaxStat);
                var healthDelta = StatMath.SaturatingMultiply(Math.Max(0, StatMath.SaturatingDelta(final.MaxHealth, original.MaxHealth)), multiplier, 0, StatMath.MaxStat);
                if (attackDelta > 0 || healthDelta > 0)
                {
                    BuffMinion(original, attackDelta, healthDelta, "Tarecgosa");
                }

                foreach (var keyword in final.Keywords.Where(keyword => !original.Keywords.Contains(keyword)))
                {
                    original.Keywords.Add(keyword);
                }
            }

            foreach (var original in State.Player.Board.Where(minion => minion.CardId == TrigoreTheLasherCardId || minion.CardId == DevoutSatyressCardId))
            {
                PersistPositiveCombatDelta(original, result.FinalPlayerBoard.FirstOrDefault(minion => minion.InstanceId == original.InstanceId), original.Name);
            }

            if (State.Player.Tavern.TrinketTarecgosaStickerActive)
            {
                var edgeDragons = State.Player.Board
                    .Where(minion => minion.Tribes.Contains(Tribe.Dragon))
                    .ToList();
                var persisted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var original in new[] { edgeDragons.FirstOrDefault(), edgeDragons.LastOrDefault() })
                {
                    if (original == null || !persisted.Add(original.InstanceId))
                    {
                        continue;
                    }

                    PersistPositiveCombatDelta(
                        original,
                        result.FinalPlayerBoard.FirstOrDefault(minion => minion.InstanceId == original.InstanceId),
                        "Tarecgosa Sticker");
                }
            }

            var poetPositions = State.Player.Board
                .Select((minion, index) => new { Minion = minion, Index = index })
                .Where(item => item.Minion.CardId == PersistentPoetCardId)
                .ToList();
            foreach (var poet in poetPositions)
            {
                foreach (var index in new[] { poet.Index - 1, poet.Index + 1 })
                {
                    if (index < 0 || index >= State.Player.Board.Count)
                    {
                        continue;
                    }

                    var original = State.Player.Board[index];
                    if (!original.Tribes.Contains(Tribe.Dragon))
                    {
                        continue;
                    }

                    PersistPositiveCombatDelta(original, result.FinalPlayerBoard.FirstOrDefault(minion => minion.InstanceId == original.InstanceId), "Persistent Poet");
                }
            }
        }

        private void PersistPositiveCombatDelta(MinionInstance original, MinionInstance final, string sourceId)
        {
            if (original == null || final == null)
            {
                return;
            }

            var attackDelta = Math.Max(0, StatMath.SaturatingDelta(final.Attack, original.Attack));
            var healthDelta = Math.Max(0, StatMath.SaturatingDelta(final.MaxHealth, original.MaxHealth));
            if (attackDelta > 0 || healthDelta > 0)
            {
                BuffMinion(original, attackDelta, healthDelta, sourceId);
            }

            foreach (var keyword in final.Keywords.Where(keyword => !original.Keywords.Contains(keyword)))
            {
                original.Keywords.Add(keyword);
            }
        }

        private void ApplyCombatRewards(IEnumerable<CombatReward> rewards)
        {
            foreach (var reward in rewards ?? Enumerable.Empty<CombatReward>())
            {
                switch (reward.Type)
                {
                    case CombatRewardType.TavernSpellCostReduction:
                        State.Player.Tavern.NextTavernSpellCostReduction += reward.Amount;
                        AddRecruitLog(RecruitLogType.Play, "Combat reward: next Tavern spell costs " + reward.Amount + " less", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                        break;
                    case CombatRewardType.AddGeneratedSpellToHand:
                        AddGeneratedSpellsToHand(reward.CardId, reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.EternalKnightDied:
                        State.Player.Tavern.EternalKnightDeaths += reward.Amount;
                        ApplyEternalKnightBonuses();
                        break;
                    case CombatRewardType.FriendlyMinionDied:
                        State.Player.Tavern.FriendlyMinionDeathsThisGame += reward.Amount;
                        AdvanceOldSouls(reward.Amount);
                        DispatchCombatRewardHeroEffect(HeroEffectEventType.FriendlyMinionDiedInCombat, reward);
                        ApplyTrinketFriendlyDeathRewards(reward.Amount);
                        break;
                    case CombatRewardType.FriendlyDeathrattleMinionDied:
                        ApplyRitualDaggerCombatReward(reward);
                        break;
                    case CombatRewardType.FriendlyDeathrattleTriggered:
                        State.Player.Tavern.DeathrattlesTriggeredThisGame += reward.Amount;
                        ApplyFallenSkyGolemBonuses();
                        DispatchCombatRewardHeroEffect(HeroEffectEventType.FriendlyDeathrattleTriggeredInCombat, reward);
                        break;
                    case CombatRewardType.FriendlyMinionKilledEnemy:
                        DispatchCombatRewardHeroEffect(HeroEffectEventType.FriendlyMinionKilledEnemyInCombat, reward);
                        break;
                    case CombatRewardType.FriendlyMinionSummoned:
                        DispatchCombatRewardHeroEffect(HeroEffectEventType.FriendlyMinionSummonedInCombat, reward);
                        ApplyTrinketFriendlySummonRewards(reward);
                        break;
                    case CombatRewardType.BuffHandMinion:
                        BuffFirstHandMinion(reward.Attack, reward.Health, reward.SourceCardId);
                        break;
                    case CombatRewardType.BuffOriginalFriendlyMinion:
                        ApplyOriginalFriendlyMinionCombatBuff(reward);
                        break;
                    case CombatRewardType.ImproveAllPurposeKibble:
                        ApplyAllPurposeKibbleCombatReward(reward);
                        break;
                    case CombatRewardType.TriggerFriendlyBattlecry:
                        TriggerFriendlyBattlecryFromCombatReward(reward);
                        break;
                    case CombatRewardType.GainNextTurnGold:
                        State.Player.Tavern.NextTurnBonusGold += reward.Amount;
                        break;
                    case CombatRewardType.ImproveBloodGemsUntilNextCombat:
                        ApplyTemporaryBloodGemCombatReward(reward);
                        break;
                    case CombatRewardType.AddRandomSpellcraftSpellToHand:
                        AddRandomSpellcraftSpellToHand(reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.ImproveBloodGemAttack:
                        State.Player.Tavern.BloodGemBonusAttack += reward.Amount;
                        break;
                    case CombatRewardType.ImproveBloodGemHealth:
                        State.Player.Tavern.BloodGemBonusHealth += reward.Amount;
                        break;
                    case CombatRewardType.ImproveElementalHealth:
                        State.Player.Tavern.ElementalHealthBonus += reward.Amount;
                        AddShopGrowth(Tribe.Elemental, 0, reward.Amount, reward.SourceCardId);
                        break;
                    case CombatRewardType.ImproveRefreshBuff:
                        State.Player.Tavern.RefreshBuffAttack += reward.Attack;
                        State.Player.Tavern.RefreshBuffHealth += reward.Health;
                        break;
                    case CombatRewardType.AddTavernSpellToHand:
                        AddTavernSpellToHand(reward.CardId, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.AddRandomTavernSpellToHand:
                        AddRandomTavernSpellToHand(State.Player.Tavern.Tier, reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.AddRandomBeastToHand:
                        AddRandomTribeMinionToHand(Tribe.Beast, reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.AddRandomMagneticMechToHand:
                        AddRandomMagneticMechToHand(reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.AddRandomChromawhelpToHand:
                        AddRandomChromawhelpToHand(reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.ImproveUndeadAttack:
                        State.Player.Tavern.UndeadAttackBonus += reward.Amount;
                        AddShopGrowth(Tribe.Undead, reward.Amount, 0, reward.SourceCardId);
                        break;
                    case CombatRewardType.ImproveTavernSpellAttack:
                        State.Player.Tavern.TavernSpellBonusAttack += reward.Amount;
                        break;
                    case CombatRewardType.GainFreeRefresh:
                        State.Player.Tavern.FreeRefreshes += reward.Amount;
                        break;
                    case CombatRewardType.AddRandomSameTribeMinionToHand:
                        if (Enum.TryParse(reward.CardId, out Tribe tribe) && tribe != Tribe.None)
                        {
                            AddRandomTribeMinionToHand(tribe, reward.Amount, "combat-" + reward.SourceCardId);
                        }

                        break;
                    case CombatRewardType.AddRandomElementalToHand:
                        AddRandomTribeMinionToHand(Tribe.Elemental, reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.AddRandomDemonToHand:
                        AddRandomTribeMinionToHand(Tribe.Demon, reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.AddRandomBattlecryMinionToHand:
                        AddRandomBattlecryMinionToHand(reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.AddBountyToHand:
                        AddBountiesToHand(reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.ImproveElementalShopStats:
                        GrowElementalsInTavernAndFuture(reward.Amount, reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.ImproveTavernMinionStats:
                        AddShopGrowth(Tribe.All, reward.Amount, reward.Amount, "combat-" + reward.SourceCardId);
                        BuffAllMinions(State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), reward.Amount, reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                    case CombatRewardType.AddRandomTierSixMinionToHand:
                        AddRandomTierMinionsToHand(6, reward.Amount, "combat-" + reward.SourceCardId);
                        break;
                }
            }
        }

        private void ApplyTideRaiserPortraitCombatRewards(CombatOutput result)
        {
            if (!HasEquippedTrinketEffect(TideRaiserPortraitEffectId) || result?.Replay?.Frames == null)
            {
                return;
            }

            var casts = result.Replay.Frames.Count(frame =>
                frame.EventType == CombatEventType.CombatSpellCast &&
                frame.ActorSide == BoardSide.Player &&
                string.Equals(frame.ActorId, TideRaiserCardId, StringComparison.OrdinalIgnoreCase));
            casts = Math.Min(3, Math.Max(0, casts));
            if (casts <= 0)
            {
                return;
            }

            AddGeneratedSpellsToHand(ShiftingTideSpellCardId, casts, "Tide Raiser Portrait");
            AddRecruitLog(RecruitLogType.Play, "Tide Raiser Portrait: copied " + casts + " combat spell(s).", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void TriggerFriendlyBattlecryFromCombatReward(CombatReward reward)
        {
            if (reward == null || reward.Amount <= 0)
            {
                return;
            }

            var candidates = State.Player.Board
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion && minion.Keywords.Contains(Keyword.Battlecry))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 3347 + State.Player.Tavern.RecruitLog.Count);
            var triggered = 0;
            while (triggered < reward.Amount && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var target = candidates[index];
                candidates.RemoveAt(index);
                ResolveMinionBattlecry(target);
                triggered += 1;
            }

            var sourceName = reward.SourceCardId;
            if (!string.IsNullOrEmpty(reward.SourceCardId) &&
                trinketCatalog != null &&
                trinketCatalog.TryGetByCardId(reward.SourceCardId, out var definition))
            {
                sourceName = definition.Name;
            }

            AddRecruitLog(
                RecruitLogType.Play,
                sourceName + ": triggered " + triggered + " friendly Battlecry minion(s).",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void ApplyTrinketFriendlyDeathRewards(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (HasEquippedTrinketEffect("lucky_tabby"))
            {
                AdvanceTrinketCounterReward(ref trinkets.LuckyTabbyDeaths, amount, 7, Tribe.Beast, "Lucky Tabby");
            }

            if (HasEquippedTrinketEffect("bleeding_heart"))
            {
                AdvanceTrinketCounterReward(ref trinkets.BleedingHeartDeaths, amount, 8, Tribe.Undead, "Bleeding Heart");
            }

            if (HasEquippedTrinketEffect("stormcoil_sticker"))
            {
                AdvanceTrinketCounterReward(ref trinkets.StormcoilStickerDeaths, amount, 8, Tribe.Mech, "Stormcoil Sticker");
            }
        }

        private void ApplyTrinketFriendlySummonRewards(CombatReward reward)
        {
            if (reward == null || reward.Amount <= 0 || !CombatRewardHasTribe(reward, Tribe.Beast))
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            if (HasEquippedTrinketEffect("wildfeather_duster"))
            {
                AdvanceTrinketCounterReward(ref trinkets.WildfeatherDusterBeastSummons, reward.Amount, 6, Tribe.Beast, "Wildfeather Duster");
            }

            if (HasEquippedTrinketEffect("fang_anklet"))
            {
                EnsureFangAnkletBonus(trinkets);
                trinkets.FangAnkletBonusAttack = StatMath.SaturatingAdd(trinkets.FangAnkletBonusAttack, reward.Amount, 0, StatMath.MaxStat);
                trinkets.FangAnkletBonusHealth = StatMath.SaturatingAdd(trinkets.FangAnkletBonusHealth, reward.Amount, 0, StatMath.MaxStat);
                AddRecruitLog(
                    RecruitLogType.Play,
                    "Fang Anklet: improved Beast combat bonus to +" + trinkets.FangAnkletBonusAttack + "/+" + trinkets.FangAnkletBonusHealth + ".",
                    State.Player.Tavern.Gold,
                    State.Player.Tavern.Gold);
            }
        }

        private void AdvanceTrinketCounterReward(ref int counter, int amount, int threshold, Tribe rewardTribe, string source)
        {
            counter = Math.Max(0, counter) + amount;
            var rewards = counter / threshold;
            counter %= threshold;
            if (rewards <= 0)
            {
                return;
            }

            var before = State.Player.Tavern.Hand.Count;
            AddRandomTribeMinionToHand(rewardTribe, rewards, source);
            var added = State.Player.Tavern.Hand.Count - before;
            AddRecruitLog(
                RecruitLogType.Play,
                source + ": counter completed " + rewards + " time(s), added " + added + " random " + rewardTribe + " minion(s).",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private static bool CombatRewardHasTribe(CombatReward reward, Tribe tribe)
        {
            if (reward?.Tribes == null || tribe == Tribe.None)
            {
                return false;
            }

            return tribe == Tribe.All
                ? reward.Tribes.Any(candidate => candidate != Tribe.None)
                : reward.Tribes.Contains(tribe) || reward.Tribes.Contains(Tribe.All);
        }

        private static void EnsureFangAnkletBonus(PlayerTrinketState trinkets)
        {
            if (trinkets == null)
            {
                return;
            }

            if (trinkets.FangAnkletBonusAttack <= 0)
            {
                trinkets.FangAnkletBonusAttack = 1;
            }

            if (trinkets.FangAnkletBonusHealth <= 0)
            {
                trinkets.FangAnkletBonusHealth = 1;
            }
        }

        private static void EnsureAllPurposeKibbleAttack(PlayerTrinketState trinkets)
        {
            if (trinkets == null)
            {
                return;
            }

            if (trinkets.AllPurposeKibbleAttack <= 0)
            {
                trinkets.AllPurposeKibbleAttack = 2;
            }
        }

        private void ApplyAllPurposeKibbleCombatReward(CombatReward reward)
        {
            if (reward == null || reward.Amount <= 0)
            {
                return;
            }

            var trinkets = EnsureTrinketState(State.Player.Tavern);
            EnsureAllPurposeKibbleAttack(trinkets);
            trinkets.AllPurposeKibbleAttack = StatMath.SaturatingAdd(trinkets.AllPurposeKibbleAttack, reward.Amount, 0, StatMath.MaxStat);
            AddRecruitLog(
                RecruitLogType.Play,
                "All-Purpose Kibble: improved Beast attack bonus to +" + trinkets.AllPurposeKibbleAttack + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void ApplyTemporaryBloodGemCombatReward(CombatReward reward)
        {
            if (reward == null || reward.Amount <= 0)
            {
                return;
            }

            var attack = StatMath.SaturatingMultiply(Math.Max(0, reward.Attack), reward.Amount, 0, StatMath.MaxStat);
            var health = StatMath.SaturatingMultiply(Math.Max(0, reward.Health), reward.Amount, 0, StatMath.MaxStat);
            State.Player.Tavern.BloodGemBonusAttack = StatMath.SaturatingAdd(State.Player.Tavern.BloodGemBonusAttack, attack, 0, StatMath.MaxStat);
            State.Player.Tavern.BloodGemBonusHealth = StatMath.SaturatingAdd(State.Player.Tavern.BloodGemBonusHealth, health, 0, StatMath.MaxStat);
            State.Player.Tavern.TrinketTemporaryBloodGemAttack = StatMath.SaturatingAdd(State.Player.Tavern.TrinketTemporaryBloodGemAttack, attack, 0, StatMath.MaxStat);
            State.Player.Tavern.TrinketTemporaryBloodGemHealth = StatMath.SaturatingAdd(State.Player.Tavern.TrinketTemporaryBloodGemHealth, health, 0, StatMath.MaxStat);
            AddRecruitLog(
                RecruitLogType.Play,
                reward.SourceCardId + ": Blood Gems give +" + attack + "/+" + health + " until next combat.",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void AddRandomSpellcraftSpellToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 919 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableTavernSpells()
                .Where(spell => spell.Tags != null && spell.Tags.Any(tag => tag.IndexOf("spellcraft", StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            if (candidates.Count > 0)
            {
                AddRandomTavernSpellsFromCandidates(candidates, count, source, rng);
                return;
            }

            AddGeneratedSpellsToHand(ShiftingTideSpellCardId, count, source);
        }

        private void ApplyOriginalFriendlyMinionCombatBuff(CombatReward reward)
        {
            var source = string.IsNullOrWhiteSpace(reward.SourceCardId) ? "Combat reward" : reward.SourceCardId;
            if (string.IsNullOrWhiteSpace(reward.TargetInstanceId))
            {
                AddRecruitLog(RecruitLogType.Play, "Combat reward skipped: no original target for " + source + ".", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
                return;
            }

            var target = State.Player.Board.FirstOrDefault(minion =>
                string.Equals(minion.InstanceId, reward.TargetInstanceId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                AddRecruitLog(
                    RecruitLogType.Play,
                    "Combat reward skipped: original minion not found for " + source + " target " + reward.TargetInstanceId + ".",
                    State.Player.Tavern.Gold,
                    State.Player.Tavern.Gold);
                return;
            }

            var amount = Math.Max(1, reward.Amount);
            var attack = StatMath.SaturatingMultiply(Math.Max(0, reward.Attack), amount, 0, StatMath.MaxStat);
            var health = StatMath.SaturatingMultiply(Math.Max(0, reward.Health), amount, 0, StatMath.MaxStat);
            if (attack <= 0 && health <= 0)
            {
                return;
            }

            BuffMinion(target, attack, health, source);
            AddRecruitLog(
                RecruitLogType.Play,
                source + ": permanently buffed " + target.Name + " +" + attack + "/+" + health + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private void PersistQuestCombatRewards()
        {
            if (!HasActiveQuestReward(TumblingDisasterRewardId))
            {
                return;
            }

            var tavern = State.Player.Tavern;
            var quests = EnsureQuestState(tavern);
            if (tavern.QuestTumblingAttack > 0)
            {
                quests.RewardCounters[QuestRewardCounterKey(TumblingDisasterRewardId, "attack")] = tavern.QuestTumblingAttack;
            }

            if (tavern.QuestTumblingHealth > 0)
            {
                quests.RewardCounters[QuestRewardCounterKey(TumblingDisasterRewardId, "health")] = tavern.QuestTumblingHealth;
            }
        }

        private void ApplyFallenSkyGolemBonuses()
        {
            var triggers = State.Player.Tavern.DeathrattlesTriggeredThisGame;
            if (triggers <= 0)
            {
                return;
            }

            foreach (var card in State.Player.Board.Concat(State.Player.Tavern.Hand).Concat(State.Player.Tavern.Shop.Where(card => card != null)))
            {
                if (card.CardId != FallenSkyGolemCardId)
                {
                    continue;
                }

                var multiplier = card.Golden ? 2 : 1;
                SetTrackedBuff(
                    card,
                    "Fallen Sky Golem",
                    StatMath.SaturatingMultiply(StatMath.SaturatingMultiply(triggers, 4, 0, StatMath.MaxStat), multiplier, 0, StatMath.MaxStat),
                    StatMath.SaturatingMultiply(StatMath.SaturatingMultiply(triggers, 2, 0, StatMath.MaxStat), multiplier, 0, StatMath.MaxStat));
            }
        }

        private void ApplyEternalKnightBonuses()
        {
            foreach (var card in State.Player.Board.Concat(State.Player.Tavern.Hand).Concat(State.Player.Tavern.Shop.Where(card => card != null)))
            {
                if (card.CardId != EternalKnightCardId)
                {
                    continue;
                }

                var attack = StatMath.SaturatingMultiply(State.Player.Tavern.EternalKnightDeaths, card.Golden ? 8 : 4, 0, StatMath.MaxStat);
                var health = StatMath.SaturatingMultiply(State.Player.Tavern.EternalKnightDeaths, card.Golden ? 4 : 2, 0, StatMath.MaxStat);
                SetTrackedBuff(card, GlobalEternalKnightSourceId, attack, health);
            }
        }

        private void ApplyAncestralAutomatonBonuses()
        {
            var otherSummons = Math.Max(0, State.Player.Tavern.AncestralAutomatonSummons - 1);
            foreach (var card in State.Player.Board.Concat(State.Player.Tavern.Hand).Concat(State.Player.Tavern.Shop.Where(card => card != null)))
            {
                if (card.CardId != AncestralAutomatonCardId)
                {
                    continue;
                }

                var attack = StatMath.SaturatingMultiply(otherSummons, card.Golden ? 6 : 3, 0, StatMath.MaxStat);
                var health = StatMath.SaturatingMultiply(otherSummons, card.Golden ? 4 : 2, 0, StatMath.MaxStat);
                SetTrackedBuff(card, GlobalAutomatonSourceId, attack, health);
            }
        }

        private void AdvanceOldSouls(int deaths)
        {
            foreach (var oldSoul in State.Player.Tavern.Hand.Where(card => card.CardId == OldSoulCardId && !card.Golden))
            {
                oldSoul.Counters.TryGetValue("old-soul-deaths", out var current);
                current += deaths;
                oldSoul.Counters["old-soul-deaths"] = current;
                if (current >= 15)
                {
                    MakeGoldenInPlace(oldSoul);
                    oldSoul.Counters["old-soul-deaths"] = 15;
                }
            }
        }

        private void BuffFirstHandMinion(int attack, int health, string sourceId)
        {
            var target = State.Player.Tavern.Hand.FirstOrDefault(card => card.CardKind == CardKind.Minion);
            if (target != null)
            {
                BuffMinion(target, attack, health, sourceId);
            }
        }

        private void StartFearlessFoodieChoice(MinionInstance source)
        {
            var multiplier = source?.Golden == true ? 2 : 1;
            State.Player.Tavern.Discover = new DiscoverState
            {
                Source = "fearless-foodie",
                RewardTier = 0,
                Options = new List<MinionInstance>
                {
                    CreateFearlessFoodieChoice(FearlessFoodieGrowthOptionCardId, "Improve Blood Gems", "Your Blood Gems give extra stats this game.", multiplier),
                    CreateFearlessFoodieChoice(FearlessFoodieGemsOptionCardId, "Get Blood Gems", "Get Blood Gems.", 4 * multiplier)
                }
            };
        }

        private void ResolveFearlessFoodieChoice(MinionInstance picked)
        {
            if (picked == null)
            {
                return;
            }

            if (picked.CardId == FearlessFoodieGrowthOptionCardId)
            {
                var amount = picked.Counters.TryGetValue("foodie_multiplier", out var stored) ? Math.Max(1, stored) : 1;
                State.Player.Tavern.BloodGemBonusAttack += amount;
                State.Player.Tavern.BloodGemBonusHealth += amount;
                return;
            }

            if (picked.CardId == FearlessFoodieGemsOptionCardId)
            {
                var count = picked.Counters.TryGetValue("foodie_gems", out var stored) ? Math.Max(1, stored) : 4;
                AddBloodGemsToHand(count, "fearless-foodie");
            }
        }

        private void StartSprightlyScarabChoice(MinionInstance source, string battlecryTargetId, int multiplier)
        {
            if (source == null)
            {
                return;
            }

            var beastTarget = ResolveSprightlyScarabBeastTarget(source, battlecryTargetId);
            State.Player.Tavern.Discover = new DiscoverState
            {
                Source = "sprightly-scarab:" + source.InstanceId,
                TargetInstanceId = beastTarget?.InstanceId,
                RemainingPicks = Math.Max(1, multiplier),
                Options = new List<MinionInstance>
                {
                    CreateSprightlyScarabChoice(SprightlyScarabRebornOptionCardId, "Beast Reborn", "Give the chosen Beast stats and Reborn.", multiplier),
                    CreateSprightlyScarabChoice(SprightlyScarabWindfuryOptionCardId, "Windfury", "This gains Attack and Windfury.", multiplier)
                }
            };
        }

        private void ResolveSprightlyScarabChoice(DiscoverState discover, MinionInstance picked)
        {
            if (discover == null || picked == null)
            {
                return;
            }

            var multiplier = Math.Max(1, discover.RemainingPicks);
            var sourceId = discover.Source.Substring("sprightly-scarab:".Length);
            var source = State.Player.Board.FirstOrDefault(minion => minion.InstanceId == sourceId);
            if (picked.CardId == SprightlyScarabRebornOptionCardId)
            {
                var target = ResolveSprightlyScarabBeastTarget(source, discover.TargetInstanceId);
                if (target == null)
                {
                    return;
                }

                BuffMinion(target, multiplier, multiplier, "Sprightly Scarab");
                if (!target.Keywords.Contains(Keyword.Reborn))
                {
                    target.Keywords.Add(Keyword.Reborn);
                }

                return;
            }

            if (picked.CardId == SprightlyScarabWindfuryOptionCardId && source != null)
            {
                BuffMinion(source, 4 * multiplier, 0, "Sprightly Scarab");
                if (!source.Keywords.Contains(Keyword.Windfury))
                {
                    source.Keywords.Add(Keyword.Windfury);
                }
            }
        }

        private MinionInstance ResolveSprightlyScarabBeastTarget(MinionInstance source, string targetInstanceId)
        {
            var selected = State.Player.Board.FirstOrDefault(minion => minion.InstanceId == targetInstanceId);
            if (selected != null && selected.Tribes.Contains(Tribe.Beast))
            {
                return selected;
            }

            return State.Player.Board.FirstOrDefault(minion => (source == null || minion.InstanceId != source.InstanceId) && minion.Tribes.Contains(Tribe.Beast))
                ?? (source != null && source.Tribes.Contains(Tribe.Beast) ? source : null)
                ?? State.Player.Board.FirstOrDefault(minion => minion.Tribes.Contains(Tribe.Beast));
        }

        private void BuffBalladistPirate(MinionInstance source, int multiplier)
        {
            var target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId != source.InstanceId && minion.Tribes.Contains(Tribe.Pirate))
                ?? State.Player.Board.FirstOrDefault(minion => minion.Tribes.Contains(Tribe.Pirate));
            if (target == null)
            {
                return;
            }

            var amount = StatMath.SaturatingMultiply(StatMath.SaturatingAdd(Math.Max(0, State.Player.Tavern.GoldSpentThisTurn), 1, 0, StatMath.MaxStat), multiplier, 0, StatMath.MaxStat);
            BuffMinion(
                target,
                HasEquippedTrinketEffect("balladist_portrait") ? amount : 0,
                amount,
                "Balladist");
        }

        private void SetupDoomsdayDragonEgg(MinionInstance card)
        {
            if (card == null || card.CardId != DoomsdayDragonEggCardId || card.Counters.ContainsKey(LockedTurnsCounter))
            {
                return;
            }

            card.Counters[LockedTurnsCounter] = 2;
            if (!card.Tags.Contains("locked_in_hand"))
            {
                card.Tags.Add("locked_in_hand");
            }
        }

        private void StartReadyDoomsdayDragonEggDiscover()
        {
            if (State.Player.Tavern.Discover != null)
            {
                return;
            }

            var egg = State.Player.Tavern.Hand.FirstOrDefault(card => card.CardId == DoomsdayDragonEggCardId && card.Tags.Contains("doomsday_hatch_ready"));
            if (egg == null)
            {
                return;
            }

            egg.Tags.Remove("doomsday_hatch_ready");
            var rng = new SeededRng(State.Seed + State.Round * 811 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier == 6 && minion.Tribes.Contains(Tribe.Dragon))
                .ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "doomsday-dragon-" + egg.InstanceId + "-" + options.Count, egg.Golden, PoolSource.Discover, 0));
            }

            State.Player.Tavern.Discover = new DiscoverState
            {
                Source = "doomsday-dragon-egg:" + egg.InstanceId,
                RewardTier = 6,
                Options = options
            };
        }

        private void HatchDoomsdayDragonEgg(string eggInstanceId, MinionInstance picked)
        {
            var index = State.Player.Tavern.Hand.FindIndex(card => card.InstanceId == eggInstanceId);
            if (index < 0 || picked == null)
            {
                return;
            }

            picked.Owner = BoardSide.Player;
            picked.PoolSource = PoolSource.Copy;
            picked.PoolCopiesHeld = 0;
            State.Player.Tavern.Hand[index] = picked;
        }

        private void ResetCombatTestSnapshot()
        {
            if (combatTestSnapshot?.BeforeCombat == null)
            {
                return;
            }

            TestScenarioMapper.ApplyTo(State, combatTestSnapshot.BeforeCombat);
            State.CombatLog.Clear();
            State.LastResult = null;
            State.LastReplay = null;
        }

        private void ResolveMinionBattlecry(MinionInstance target, string battlecryTargetId = null)
        {
            var repeats = GetBattlecryRepeats(target);
            for (var index = 0; index < repeats; index += 1)
            {
                ResolveSingleMinionBattlecry(target, battlecryTargetId);
            }
        }

        private HeroBattlecryReplayResult ReplayBattlecryForHeroEffect(HeroBattlecryReplayRequest request)
        {
            if (request == null || request.Source == null)
            {
                throw new InvalidOperationException("Battlecry replay needs a source minion.");
            }

            var result = new HeroBattlecryReplayResult();
            var source = request.Source;
            var requestRepeats = Math.Max(1, request.RepeatCount);
            var boardRepeats = GetBattlecryRepeats(source);
            var totalRepeats = requestRepeats * boardRepeats;
            var battlecryTargetId = ResolveBattlecryTargetId(source, request.TargetIndex, request.TargetZone, request.TargetInstanceId);
            for (var index = 0; index < totalRepeats; index += 1)
            {
                ResolveSingleMinionBattlecry(source, battlecryTargetId);
                DispatchSourceEvent(MechanicEventType.CardPlayed, source);
                DispatchHeroEffect(
                    HeroEffectEventType.BattlecryTriggered,
                    source,
                    targetIndex: request.TargetIndex,
                    targetZone: request.TargetZone,
                    targetInstanceId: request.TargetInstanceId);
                result.ResolvedRepeats += 1;
            }

            result.Messages.Add("Battlecry replay: resolved " + result.ResolvedRepeats + " trigger(s) for " + source.Name + ".");
            return result;
        }

        private void ResolveSingleMinionBattlecry(MinionInstance target, string battlecryTargetId = null)
        {
            ResolveTierOneBattlecry(target);
            ResolveTierThreeBattlecry(target, battlecryTargetId);
            ResolveTierFourBattlecry(target, battlecryTargetId);
            ResolveTierFiveBattlecry(target);
            ResolveTierSixSevenBattlecry(target);
            ResolveKalecgosBattlecryTrigger(target);
        }

        private int GetBattlecryRepeats(MinionInstance target)
        {
            if (target == null || !target.Keywords.Contains(Keyword.Battlecry))
            {
                return 1;
            }

            var brann = State.Player.Board
                .Where(minion => minion.CardId == BrannBronzebeardCardId && minion.InstanceId != target.InstanceId)
                .OrderByDescending(minion => minion.Golden ? 3 : 2)
                .FirstOrDefault();
            var repeats = brann == null ? 1 : brann.Golden ? 3 : 2;
            if (HasActiveQuestReward(ExquisiteConchRewardId))
            {
                var quests = EnsureQuestState(State.Player.Tavern);
                var key = QuestRewardCounterKey(ExquisiteConchRewardId, "usedRound");
                var usedRound = quests.RewardCounters.TryGetValue(key, out var stored) ? stored : -1;
                if (usedRound != State.Round)
                {
                    repeats += 2;
                    quests.RewardCounters[key] = State.Round;
                }
            }

            if (HasActiveQuestReward(GilneanWarHornRewardId))
            {
                repeats += 1;
            }

            return repeats;
        }

        private void ResolveKalecgosBattlecryTrigger(MinionInstance target)
        {
            if (target == null || !target.Keywords.Contains(Keyword.Battlecry))
            {
                return;
            }

            foreach (var kalecgos in State.Player.Board.Where(minion => minion.CardId == KalecgosCardId).ToList())
            {
                BuffAllMinions(State.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Dragon)), kalecgos.Golden ? 4 : 2, kalecgos.Golden ? 4 : 2, "Kalecgos");
            }
        }

        private (int Attack, int Health) GetBoardTavernSpellBonus()
        {
            var attack = 0;
            var health = 0;
            foreach (var minion in State.Player.Board)
            {
                if (minion.CardId == HumongousCardId)
                {
                    attack += minion.Golden ? 2 : 1;
                    health += minion.Golden ? 4 : 2;
                }

                if (minion.CardId == EnchantedDrudgeCardId)
                {
                    attack += minion.Golden ? 2 : 1;
                    health += minion.Golden ? 2 : 1;
                }
            }

            return (attack, health);
        }

        private int GetTavernSpellExtraCasts(MinionInstance spell)
        {
            if (spell == null)
            {
                return 0;
            }

            var extra = ConsumeSpitescaleSushiRollExtraCast(spell);
            if (spell.CardKind != CardKind.TavernSpell && spell.CardKind != CardKind.Spell)
            {
                return extra;
            }

            if (TryConsumeReplicaCathedralExtraCast())
            {
                extra += 1;
            }

            if (spell.CardKind != CardKind.TavernSpell)
            {
                return extra;
            }

            if (State.Player.Board.Any(minion => minion.CardId == MaelstromNagaCardId))
            {
                extra += 1;
            }

            if (State.Player.Board.Any(minion => minion.CardId == BelindaStonehearthCardId))
            {
                extra += 1;
            }

            if (IsBountyCardId(spell.CardId) && State.Player.Board.Any(minion => minion.CardId == ObsidianRavagerCardId))
            {
                extra += 1;
            }

            if (HasActiveQuestReward(TemporalTamperingRewardId))
            {
                extra += 1;
            }

            return extra;
        }

        private bool TryConsumeReplicaCathedralExtraCast()
        {
            if (!HasEquippedTrinketEffect(ReplicaCathedralEffectId))
            {
                return false;
            }

            if (GetAdvancedMechanicCounter(ReplicaCathedralRoundCounter) != State.Round)
            {
                SetAdvancedMechanicCounter(ReplicaCathedralRoundCounter, State.Round);
                SetAdvancedMechanicCounter(ReplicaCathedralUsedCounter, 0);
            }

            if (GetAdvancedMechanicCounter(ReplicaCathedralUsedCounter) > 0)
            {
                return false;
            }

            SetAdvancedMechanicCounter(ReplicaCathedralUsedCounter, 1);
            return true;
        }

        private int ConsumeSpitescaleSushiRollExtraCast(MinionInstance spell)
        {
            if (spell?.Tags == null || !spell.Tags.Contains("spellcraft") || !HasEquippedTrinketEffect("spitescale_sushi_roll"))
            {
                return 0;
            }

            var left = GetAdvancedMechanicCounter(SpitescaleSushiRollExtraCastsLeftCounter, 2);
            if (left <= 0)
            {
                return 0;
            }

            SetAdvancedMechanicCounter(SpitescaleSushiRollExtraCastsLeftCounter, left - 1);
            return 1;
        }

        private void ResetSpitescaleSushiRollExtraCasts()
        {
            SetAdvancedMechanicCounter(SpitescaleSushiRollExtraCastsLeftCounter, 2);
        }

        private MinionInstance FirstOtherFriendlyMinion(MinionInstance source)
        {
            return State.Player.Board.FirstOrDefault(minion => source == null || minion.InstanceId != source.InstanceId);
        }

        private void ResolveTierFourBattlecry(MinionInstance target, string battlecryTargetId)
        {
            if (target == null)
            {
                return;
            }

            var multiplier = target.Golden ? 2 : 1;
            switch (target.CardId)
            {
                case RefreshingAnomalyCardId:
                    State.Player.Tavern.FreeRefreshes = StatMath.SaturatingAdd(State.Player.Tavern.FreeRefreshes, 2 * multiplier, 0, StatMath.MaxStat);
                    break;
                case TavernTempestCardId:
                    AddRandomTribeMinionToHand(Tribe.Elemental, multiplier, "tavern-tempest");
                    break;
                case FeedingTigerSharkCardId:
                    StartTribeDiscover(Tribe.Beast, "feeding-tiger-shark");
                    break;
                case PricklyPiperCardId:
                    StartTribeDiscover(Tribe.Demon, "prickly-piper");
                    break;
                case FearlessFoodieCardId:
                    StartFearlessFoodieChoice(target);
                    break;
                case BalladistCardId:
                    BuffBalladistPirate(target, multiplier);
                    break;
                case KingBagurgleCardId:
                    BuffAllMinions(State.Player.Board.Where(minion => minion.InstanceId != target.InstanceId && minion.Tribes.Contains(Tribe.Murloc)), 4 * multiplier, 4 * multiplier, "King Bagurgle");
                    BuffAllMinions(State.Player.Tavern.Hand.Where(minion => minion.Tribes.Contains(Tribe.Murloc)), 4 * multiplier, 4 * multiplier, "King Bagurgle");
                    break;
                case ScrapperCardId:
                    StartScrapperMagneticDiscover(target, battlecryTargetId, multiplier);
                    break;
                case BrannosaurCardId:
                    State.Player.Tavern.RefreshBuffAttack = StatMath.SaturatingAdd(State.Player.Tavern.RefreshBuffAttack, 7 * multiplier, 0, StatMath.MaxStat);
                    State.Player.Tavern.RefreshBuffHealth = StatMath.SaturatingAdd(State.Player.Tavern.RefreshBuffHealth, 7 * multiplier, 0, StatMath.MaxStat);
                    break;
                case DustyCycloneCardId:
                    AddShopGrowth(Tribe.Elemental, multiplier, 0, "Dusty Cyclone");
                    BuffAllMinions(State.Player.Board.Concat(State.Player.Tavern.Hand).Concat(State.Player.Tavern.Shop.Where(card => card != null)).Where(minion => minion.Tribes.Contains(Tribe.Elemental)), multiplier, 0, "Dusty Cyclone");
                    break;
                case DeepwaterChieftainCardId:
                    AddGeneratedSpellsToHand(DeepwaterSchoolCardId, multiplier, "deepwater-chieftain");
                    break;
                case ManasparkCardId:
                    AddGeneratedSpellsToHand(ArcaneConsumptionCardId, multiplier, "manaspark");
                    break;
                case SaloonDancerCardId:
                    var saloonBuff = StatMath.SaturatingAdd(2 * multiplier, State.Player.Tavern.TavernSpellsCastThisTurn, 0, StatMath.MaxStat);
                    BuffMinion(FirstOtherFriendlyMinion(target) ?? target, saloonBuff, saloonBuff, "Saloon Dancer");
                    break;
            }
        }

        private void ResolveTierFiveBattlecry(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            var multiplier = target.Golden ? 2 : 1;
            switch (target.CardId)
            {
                case BoarHerderCardId:
                    AddGeneratedSpellsToHand(BristlebackBloodGemCardId, multiplier, "boar-herder");
                    break;
                case ArenaShowmanCardId:
                    StartTavernSpellDiscover("arena-showman");
                    break;
                case FarmhandWhirlOMatronCardId:
                    GrowElementalsInTavernAndFuture(8 * multiplier, 8 * multiplier, "Farmhand Whirl-O-Tron");
                    break;
                case FirelandsFlameCardId:
                    AddTavernSpellToHand(ConflagrationCardNumber, "firelands-flame");
                    break;
                case NightmareParlorGuestCardId:
                    AddTavernSpellToHand(MenagerieTablewareCardNumber, "nightmare-parlor-guest");
                    break;
                case GrittyHeadhunterCardId:
                    AddGeneratedOrCatalogTavernSpellToHand(MaraudersContractCardNumber, multiplier, "gritty-headhunter");
                    break;
                case HackerfinCardId:
                    ApplyHackerfinBattlecry(target, multiplier);
                    break;
                case VoidpupTrainerCardId:
                    AddLowTierShopGrowth(3 * multiplier, 3 * multiplier, "Voidpup Trainer");
                    break;
                case ShipwreckedCaptainCardId:
                    AddBountiesToHand(multiplier, "shipwrecked-captain");
                    break;
                case PrimalfinLookoutCardId:
                    if (State.Player.Board.Any(minion => minion.InstanceId != target.InstanceId && minion.Tribes.Contains(Tribe.Murloc)))
                    {
                        StartTribeDiscover(Tribe.Murloc, "primalfin-lookout");
                    }

                    break;
                case DragonCaretakerCardId:
                    AddRandomChromawhelpToHand(multiplier, "dragon-caretaker");
                    break;
            }
        }

        private void ResolveTierSixSevenBattlecry(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            var multiplier = target.Golden ? 2 : 1;
            switch (target.CardId)
            {
                case QueenGuardCardId:
                    CastTavernSpellImmediate(QueensCommandCardNumber, "queen-guard");
                    break;
                case BloodChampionCardId:
                    State.Player.Tavern.BloodGemBonusAttack += multiplier;
                    State.Player.Tavern.BloodGemBonusHealth += multiplier;
                    break;
                case CaptainSandersCardId:
                    MakeGoldenFriendlyTierSixOrLower(multiplier);
                    break;
                case SargerasChampionCardId:
                    AddShopGrowth(Tribe.All, 5 * multiplier, 5 * multiplier, "Sargeras' Champion");
                    BuffAllMinions(State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 5 * multiplier, 5 * multiplier, "Sargeras' Champion");
                    break;
                case RheaSupremeWardenCardId:
                    AddRandomTierMinionsToHand(6, multiplier, "rhea");
                    break;
            }
        }

        private void ResolveTierThreeBattlecry(MinionInstance target, string battlecryTargetId)
        {
            if (target == null)
            {
                return;
            }

            var multiplier = target.Golden ? 2 : 1;
            switch (target.CardId)
            {
                case FelElementalCardId:
                    AddShopGrowth(Tribe.All, 2 * multiplier, 1 * multiplier, "Fel Elemental");
                    BuffAllMinions(State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), 2 * multiplier, 1 * multiplier, "Fel Elemental");
                    break;
                case JazzerCardId:
                    State.Player.Tavern.BloodGemBonusHealth += multiplier;
                    break;
                case MalchezaarCardId:
                    State.Player.Tavern.HealthCostRefreshes += 2 * multiplier;
                    target.Counters["health_refreshes_ready"] = 2 * multiplier;
                    break;
                case MutableBeetleCardId:
                    StartSprightlyScarabChoice(target, battlecryTargetId, multiplier);
                    break;
                case DisguisedGraverobberCardId:
                    DestroyUndeadAndAddPlainCopies(target, battlecryTargetId, multiplier);
                    break;
                case ColdlightDiverCardId:
                    AddRandomTavernSpellToHand(1, target.Golden ? 2 : 1, "coldlight-diver");
                    break;
                case BlueChromawhelpCardId:
                    AddRandomTavernSpellToHandByCost(2, target.Golden ? 2 : 1, "blue-chromawhelp");
                    break;
                case BlackChromawhelpCardId:
                    State.Player.Tavern.TavernSpellBonusHealth += multiplier;
                    break;
                case GreenChromawhelpCardId:
                    BuffAllMinions(OtherFriendlyDragons(target), 2 * multiplier, 4 * multiplier, "Green Chromawhelp");
                    break;
                case BronzeChromawhelpCardId:
                    BuffAllMinions(OtherFriendlyDragons(target), 4 * multiplier, 2 * multiplier, "Bronze Chromawhelp");
                    break;
                case RedChromawhelpCardId:
                    State.Player.Tavern.TavernSpellBonusAttack += multiplier;
                    break;
                case BristlingDrummerCardId:
                    AddTavernSpellToHand(BloodGemBarrageCardNumber, "bristling-drummer");
                    break;
                case MurgletonAuntieCardId:
                    State.Player.Tavern.MurgleAttackBattlecries += multiplier;
                    BuffAllMinions(State.Player.Board.Where(minion => minion.CardId != target.CardId && minion.Tribes.Contains(Tribe.Murloc)), 2 * State.Player.Tavern.MurgleAttackBattlecries, 0, "Murgleton Auntie");
                    break;
                case MurgletonDaddyCardId:
                    State.Player.Tavern.MurgleHealthBattlecries += multiplier;
                    BuffAllMinions(State.Player.Board.Where(minion => minion.CardId != target.CardId && minion.Tribes.Contains(Tribe.Murloc)), 0, 2 * State.Player.Tavern.MurgleHealthBattlecries, "Murgleton Daddy");
                    break;
            }
        }

        private void ResolveTierOneBattlecry(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            switch (target.CardId)
            {
                case AureateLaureateCardId:
                    MakeGoldenInPlace(target);
                    break;
                case OminousSeerCardId:
                    State.Player.Tavern.NextTavernSpellCostReduction += target.Golden ? 2 : 1;
                    break;
                case PickyEaterCardId:
                    DevourRandomShopMinion(target, target.Golden ? 2 : 1);
                    break;
                case RazorfenGeomancerCardId:
                    AddBloodGemsToHand(target.Golden ? 4 : 2, "razorfen");
                    break;
                case SouthseaBuskerCardId:
                    State.Player.Tavern.NextTurnBonusGold += target.Golden ? 2 : 1;
                    break;
                case ShellCollectorCardId:
                    AddTavernSpellToHand("104436", "shell-collector");
                    break;
                case IntrepidBotanistCardId:
                    State.Player.Tavern.TavernSpellBonusAttack += target.Golden ? 2 : 1;
                    break;
                case OozelingGladiatorCardId:
                    AddSlimyShieldsToHand(target.Golden ? 4 : 2, "oozeling");
                    break;
                case ForestRoverCardId:
                    State.Player.Tavern.BeetleAttackBonus += target.Golden ? 4 : 2;
                    State.Player.Tavern.BeetleHealthBonus += target.Golden ? 2 : 1;
                    break;
                case NerubianDeathswarmerCardId:
                    State.Player.Tavern.UndeadAttackBonus += target.Golden ? 2 : 1;
                    State.Player.Tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
                    {
                        Scope = BuffScope.ShopGlobal,
                        Tribe = Tribe.Undead,
                        Attack = target.Golden ? 2 : 1,
                        Health = 0,
                        SourceId = "死亡群居蛛魔"
                    });
                    BuffAllMinions(State.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Undead)), target.Golden ? 2 : 1, 0, "死亡群居蛛魔");
                    BuffAllMinions(State.Player.Tavern.Hand.Where(minion => minion.Tribes.Contains(Tribe.Undead)), target.Golden ? 2 : 1, 0, "死亡群居蛛魔");
                    BuffAllMinions(State.Player.Tavern.Shop.Where(minion => minion != null && minion.Tribes.Contains(Tribe.Undead)), target.Golden ? 2 : 1, 0, "死亡群居蛛魔");
                    break;
                case LabAssistantCardId:
                    State.Player.Tavern.DemonFodderRefreshes += target.Golden ? 6 : 3;
                    break;
            }
        }

        private void ResolveTierOneSellEffect(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            if (target.CardId == SunBaconRelaxerCardId)
            {
                AddBloodGemsToHand(target.Golden ? 4 : 2, "sun-bacon");
                return;
            }

            if (target.CardId == RiverSkipperCardId)
            {
                AddRandomTierOneMinionsToHand(target.Golden ? 2 : 1, "river-skipper");
                return;
            }

            if (target.CardId == PatientScoutCardId)
            {
                var tier = target.Counters.TryGetValue(PatientScoutTierCounter, out var scoutTier) ? scoutTier : 1;
                StartTierDiscover(Math.Min(TavernRules.MaxTavernTier, Math.Max(1, tier)), "耐心的侦查员");
                return;
            }

            if (target.CardId == TadCardId)
            {
                AddRandomTribeMinionToHand(Tribe.Murloc, target.Golden ? 2 : 1, "tad");
                return;
            }

            if (target.CardId == SellementalCardId)
            {
                AddGeneratedElementalsToHand(target.Golden ? 2 : 1, "sellemental");
                return;
            }

            if (target.CardId == FireBallerCardId)
            {
                var amount = target.Golden ? 2 : 1;
                var attack = StatMath.SaturatingAdd(amount, State.Player.Tavern.FutureBallerAttackBonus, 0, StatMath.MaxStat);
                BuffAllMinions(State.Player.Board, attack, 0, "火焰投球手");
                State.Player.Tavern.FutureBallerAttackBonus = StatMath.SaturatingAdd(State.Player.Tavern.FutureBallerAttackBonus, amount, 0, StatMath.MaxStat);
                return;
            }

            if (target.CardId == SnowBallerCardId)
            {
                var amount = target.Golden ? 2 : 1;
                var health = StatMath.SaturatingAdd(amount, State.Player.Tavern.FutureBallerHealthBonus, 0, StatMath.MaxStat);
                BuffAllMinions(State.Player.Board, 0, health, "冰雪投球手");
                State.Player.Tavern.FutureBallerHealthBonus = StatMath.SaturatingAdd(State.Player.Tavern.FutureBallerHealthBonus, amount, 0, StatMath.MaxStat);
            }
        }

        private void ResolveTierFourSellEffect(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            if (target.CardId == PlaguedGhoulCardId)
            {
                var amount = target.Golden ? 8 : 4;
                State.Player.Tavern.UndeadAttackBonus += amount;
                AddShopGrowth(Tribe.Undead, amount, 0, "Plagued Ghoul out of combat");
                BuffAllMinions(State.Player.Board.Concat(State.Player.Tavern.Hand).Concat(State.Player.Tavern.Shop.Where(card => card != null)).Where(minion => minion.Tribes.Contains(Tribe.Undead)), amount, 0, "Plagued Ghoul out of combat");
            }
        }

        private void ResolveTierSixSevenSellEffect(MinionInstance target)
        {
            if (target == null)
            {
                return;
            }

            foreach (var wrathguard in State.Player.Board.Where(minion => minion.CardId == TwistedWrathguardCardId).ToList())
            {
                State.Player.Tavern.DemonFodderRefreshes += wrathguard.Golden ? 2 : 1;
            }
        }

        private void ResolveDiscoverThenDeath(MinionInstance target)
        {
            if (target == null ||
                !target.Tags.Contains("discover_then_death") ||
                !target.Counters.TryGetValue(DisturbedGraveCounter, out var round) ||
                round != State.Round)
            {
                return;
            }

            State.Player.Board.Remove(target);
            ResolveTierFourSellEffect(target);
            ReleaseMinionToPool(target);
            RecordOutsideCombatMinionDestroyed("Disturbed Grave");
            AddRecruitLog(RecruitLogType.Play, "Disturbed Grave destroyed " + target.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void HandleCardBoughtForTierOneMinions()
        {
            foreach (var trogg in State.Player.Board.Where(minion => minion.CardId == GluttonousTroggCardId))
            {
                if (trogg.Counters.TryGetValue(GluttonousTroggClaimedCounter, out var claimed) && claimed > 0)
                {
                    continue;
                }

                trogg.Counters.TryGetValue(GluttonousTroggBuyCounter, out var bought);
                bought += 1;
                trogg.Counters[GluttonousTroggBuyCounter] = bought;
                if (bought < 4)
                {
                    continue;
                }

                BuffMinion(trogg, trogg.Golden ? 8 : 4, trogg.Golden ? 8 : 4, "贪吃的穴居人");
                trogg.Counters[GluttonousTroggClaimedCounter] = 1;
            }
        }

        private void HandleTurnEndedForTierOneMinions()
        {
            foreach (var minion in State.Player.Board.Where(minion => minion.CardId == UpbeatFrontdrakeCardId).ToList())
            {
                minion.Counters.TryGetValue(UpbeatFrontdrakeTurnCounter, out var turns);
                turns += 1;
                if (turns < 3)
                {
                    minion.Counters[UpbeatFrontdrakeTurnCounter] = turns;
                    continue;
                }

                minion.Counters[UpbeatFrontdrakeTurnCounter] = 0;
                AddRandomTribeMinionToHand(Tribe.Dragon, minion.Golden ? 2 : 1, "upbeat-frontdrake");
            }
        }

        private void HandleTurnStartedForTierThreeMinions()
        {
            var extraGold = 0;
            State.Player.Tavern.HealthCostRefreshes = 0;
            foreach (var minion in State.Player.Board)
            {
                if (minion.CardId == AccordOTronCardId)
                {
                    extraGold += minion.Golden ? 2 : 1;
                }

                if (minion.CardId == MalchezaarCardId)
                {
                    State.Player.Tavern.HealthCostRefreshes += minion.Golden ? 4 : 2;
                }
            }

            if (extraGold > 0)
            {
                State.Player.Tavern.Gold += extraGold;
                State.Player.Tavern.MaxGold = Math.Max(State.Player.Tavern.MaxGold, State.Player.Tavern.Gold);
            }
        }

        private void HandleTurnEndedForTierThreeMinions()
        {
            foreach (var minion in State.Player.Board.ToList())
            {
                switch (minion.CardId)
                {
                    case LostCityLooterCardId:
                        AddBountiesToHand(minion.Golden ? 2 : 1, "lost-city-looter");
                        break;
                }
            }
        }

        private void HandleTurnEndedForTierFourMinions()
        {
            foreach (var minion in State.Player.Board.ToList())
            {
                var multiplier = minion.Golden ? 2 : 1;
                switch (minion.CardId)
                {
                    case SignatureTimerCardId:
                        AddRandomTavernSpellToHand(TavernRules.MaxTavernTier, multiplier, "signature-timer");
                        break;
                    case WoodlandDefilerCardId:
                        State.Player.Tavern.DemonFodderRefreshes += 3 * multiplier;
                        break;
                    case BristlingGemcultivatorCardId:
                        AddGeneratedSpellsToHand(RebornBloodGemCardId, multiplier, "bristling-gemcultivator");
                        break;
                    case WildfireExecutionerCardId:
                        DevourHighestHealthShopMinion(minion, multiplier);
                        break;
                }
            }
        }

        private void HandleShopRefreshedForTierThreeMinions()
        {
            foreach (var minion in State.Player.Board.Where(minion => minion.CardId == JuvenileWaveCardId))
            {
                ApplyRefreshBuffToShop(State.Player.Tavern.Shop, minion.Golden ? 6 : 3, minion.Golden ? 6 : 3, "Juvenile Wave");
            }
        }

        private void HandleTavernSpellCastForTierThreeMinions(MinionInstance spell)
        {
            foreach (var minion in State.Player.Board.ToList())
            {
                if (minion.CardId == ChronoCaptainHooktailCardId)
                {
                    BuffAllMinions(State.Player.Board, minion.Golden ? 2 : 1, 0, "Chrono Captain Hooktail");
                }
            }
        }

        private void HandleTavernSpellCastForTierFourMinions(MinionInstance spell)
        {
            foreach (var minion in State.Player.Board.ToList())
            {
                if (minion.CardId == AbyssalBrawlerCardId)
                {
                    BuffMinion(minion, minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, "Abyssal Brawler");
                }
            }
        }

        private void HandleTavernSpellCastForTierFiveMinions(MinionInstance spell)
        {
            foreach (var minion in State.Player.Board.ToList())
            {
                var multiplier = minion.Golden ? 2 : 1;
                switch (minion.CardId)
                {
                    case NalaaCardId:
                        var extraNalaaAttack = HasEquippedTrinketEffect("redeemer_portrait") ? 4 : 0;
                        var extraNalaaHealth = HasEquippedTrinketEffect("redeemer_portrait") ? 4 : 0;
                        BuffOneOfEachFriendlyType((4 + extraNalaaAttack) * multiplier, (3 + extraNalaaHealth) * multiplier, "Nalaa the Redeemer");
                        break;
                    case LivingAzeriteCardId:
                        var azeriteAttack = 3 * multiplier;
                        var azeriteHealth = 2 * multiplier;
                        GrowElementalsInTavernAndFuture(azeriteAttack, azeriteHealth, "Living Azerite");
                        if (HasEquippedTrinketEffect("azerite_portrait"))
                        {
                            BuffAllMinions(
                                State.Player.Board.Where(card => card.Tribes.Contains(Tribe.Elemental)),
                                azeriteAttack,
                                azeriteHealth,
                                "Azerite Portrait");
                        }

                        break;
                    case FelboarCardId:
                        minion.Counters.TryGetValue("felboar_spells", out var spells);
                        spells += 1;
                        if (spells >= 3)
                        {
                            spells = 0;
                            DevourHighestHealthShopMinion(minion, multiplier);
                        }

                        minion.Counters["felboar_spells"] = spells;
                        break;
                    case ChargingCzarinaCardId:
                        var czarinaAttack = 4 * multiplier;
                        var czarinaHealth = HasEquippedTrinketEffect("czarina_portrait") ? czarinaAttack : 0;
                        BuffAllMinions(
                            State.Player.Board.Where(card => card.Keywords.Contains(Keyword.DivineShield)),
                            czarinaAttack,
                            czarinaHealth,
                            "Charging Czarina");
                        break;
                }
            }
        }

        private void HandleTavernSpellCastForTierSixSevenMinions(MinionInstance spell)
        {
            foreach (var minion in State.Player.Board.ToList())
            {
                var multiplier = minion.Golden ? 2 : 1;
                switch (minion.CardId)
                {
                    case GroundbreakerCardId:
                        minion.Counters.TryGetValue("groundbreaker_spells", out var spells);
                        spells += 1;
                        if (spells >= 4)
                        {
                            spells = 0;
                            minion.Counters.TryGetValue("groundbreaker_bonus", out var bonus);
                            minion.Counters["groundbreaker_bonus"] = bonus + multiplier;
                        }

                        minion.Counters["groundbreaker_spells"] = spells;
                        break;
                    case FireforgedEvokerCardId:
                        minion.Counters.TryGetValue("dragon_spell_attack", out var attack);
                        minion.Counters.TryGetValue("dragon_spell_health", out var health);
                        minion.Counters["dragon_spell_attack"] = StatMath.SaturatingAdd(attack, 2 * multiplier, 0, StatMath.MaxStat);
                        minion.Counters["dragon_spell_health"] = StatMath.SaturatingAdd(health, multiplier, 0, StatMath.MaxStat);
                        break;
                    case ShatteredMatriarchCardId:
                        BuffAllMinions(State.Player.Board, 0, 3 * multiplier, "Shattered Matriarch");
                        break;
                    case ForsakenThalnosCardId:
                        var amount = 2 * multiplier;
                        State.Player.Tavern.UndeadAttackBonus += amount;
                        AddShopGrowth(Tribe.Undead, amount, 0, "Forsaken Thalnos");
                        BuffAllMinions(State.Player.Board.Concat(State.Player.Tavern.Hand).Concat(State.Player.Tavern.Shop.Where(card => card != null)).Where(card => card.Tribes.Contains(Tribe.Undead)), amount, 0, "Forsaken Thalnos");
                        break;
                }
            }
        }

        private void HandleCardPlayedForTierFiveMinions(MinionInstance played)
        {
            if (played == null)
            {
                return;
            }

            foreach (var champion in State.Player.Board.Where(minion => minion.CardId == MoonEaterChampionCardId).ToList())
            {
                if (played.TavernTier % 2 == 1)
                {
                    BuffAllMinions(State.Player.Board.Where(minion => minion.TavernTier % 2 == 1), champion.Golden ? 2 : 1, champion.Golden ? 2 : 1, "Moon-Eater's Champion");
                }
            }
        }

        private void HandleCardPlayedForTierSixSevenMinions(MinionInstance played)
        {
            if (played == null)
            {
                return;
            }

            foreach (var champion in State.Player.Board.Where(minion => minion.CardId == GreymanesChampionCardId).ToList())
            {
                if (played.TavernTier > 0 && played.TavernTier % 2 == 0)
                {
                    BuffAllMinions(State.Player.Board.Where(minion => minion.TavernTier > 0 && minion.TavernTier % 2 == 0), champion.Golden ? 4 : 2, champion.Golden ? 4 : 2, "Greymane's Champion");
                }
            }

            foreach (var portraitist in State.Player.Board.Where(minion => minion.CardId == PrimalfinPortraitistCardId).ToList())
            {
                if (played.TavernTier > 0 && played.TavernTier <= 3)
                {
                    BuffAllMinions(State.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Murloc)), portraitist.Golden ? 2 : 1, portraitist.Golden ? 4 : 2, "Primalfin Portraitist");
                }
            }
        }

        private void HandleMinionPlayedForTierFiveMinions(MinionInstance played)
        {
            if (played == null)
            {
                return;
            }

            if (played.Tribes.Contains(Tribe.Elemental))
            {
                foreach (var nomi in State.Player.Board.Where(minion => minion.CardId == NomiCardId).ToList())
                {
                    GrowElementalsInTavernAndFuture(nomi.Golden ? 8 : 4, nomi.Golden ? 8 : 4, "Nomi");
                }
            }

            if (played.Tribes.Contains(Tribe.Murloc))
            {
                foreach (var burglar in State.Player.Board.Where(minion => minion.CardId == MurlocBurglarCardId).ToList())
                {
                    BuffMinion(State.Player.Board.FirstOrDefault(minion => minion.InstanceId != burglar.InstanceId), burglar.Golden ? 10 : 5, burglar.Golden ? 10 : 5, "Murloc Burglar");
                    BuffFirstHandMinion(burglar.Golden ? 10 : 5, burglar.Golden ? 10 : 5, "Murloc Burglar");
                }

                foreach (var oracle in State.Player.Board.Where(minion => minion.CardId == TideOracleMorglCardId).ToList())
                {
                    oracle.Counters.TryGetValue("murlocs_played", out var count);
                    count += 1;
                    if (count >= 2)
                    {
                        count = 0;
                        State.Player.Tavern.TavernSpellBonusAttack += oracle.Golden ? 2 : 1;
                        State.Player.Tavern.TavernSpellBonusHealth += oracle.Golden ? 2 : 1;
                    }

                    oracle.Counters["murlocs_played"] = count;
                }
            }
        }

        private void HandleMinionPlayedForTierSixSevenMinions(MinionInstance played)
        {
            if (played == null)
            {
                return;
            }

            if (played.Tribes.Contains(Tribe.Naga))
            {
                foreach (var groundbreaker in State.Player.Board.Where(minion => minion.CardId == GroundbreakerCardId && !ReferenceEquals(minion, played)).ToList())
                {
                    groundbreaker.Counters.TryGetValue("groundbreaker_bonus", out var bonus);
                    var amount = (groundbreaker.Golden ? 2 : 1) + bonus;
                    BuffMinion(groundbreaker, amount, amount, "Groundbreaker");
                    if (HasEquippedTrinketEffect("groundbreaker_portrait"))
                    {
                        var index = State.Player.Board.IndexOf(groundbreaker);
                        if (index > 0)
                        {
                            BuffMinion(State.Player.Board[index - 1], amount, amount, "Groundbreaker Portrait");
                        }
                    }
                }
            }

            if (played.Tribes.Contains(Tribe.Elemental))
            {
                foreach (var manasurge in State.Player.Board.Where(minion => minion.CardId == WildfireManasurgeCardId).ToList())
                {
                    BuffAllMinions(State.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Elemental)), manasurge.Golden ? 8 : 4, manasurge.Golden ? 8 : 4, "Wildfire Manasurge");
                }
            }

            if (played.Tribes.Contains(Tribe.Beast))
            {
                foreach (var saurolisk in State.Player.Board.Where(minion => minion.CardId == RabidSauroliskCardId).ToList())
                {
                    BuffAllMinions(State.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Beast)), saurolisk.Golden ? 6 : 3, saurolisk.Golden ? 6 : 3, "Rabid Saurolisk");
                    foreach (var beast in State.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Beast)).ToList())
                    {
                        beast.Health = StatMath.DamageHealth(beast.Health, 1);
                    }
                }
            }
        }

        private void HandleTurnEndedForTierFiveMinions()
        {
            var drakkari = State.Player.Board
                .Where(minion => minion.CardId == DrakkariEnchanterCardId)
                .OrderByDescending(minion => minion.Golden ? 3 : 2)
                .FirstOrDefault();
            var repeats = drakkari == null ? 1 : drakkari.Golden ? 3 : 2;
            for (var repeat = 0; repeat < repeats; repeat += 1)
            {
                foreach (var minion in State.Player.Board.ToList())
                {
                    var multiplier = minion.Golden ? 2 : 1;
                    switch (minion.CardId)
                    {
                        case CataclysmicChampionCardId:
                            if (!string.IsNullOrEmpty(State.Player.Tavern.LastTavernSpellCardId))
                            {
                                AddTavernSpellToHand(State.Player.Tavern.LastTavernSpellCardId, "cataclysmic-champion");
                            }

                            break;
                        case DynamicDuoCardId:
                            minion.Counters.TryGetValue("duo_turns", out var turns);
                            turns += 1;
                            if (turns >= 2)
                            {
                                turns = 0;
                                AddCopyOfLeftNeighborToHand(minion, "dynamic-duo");
                            }

                            minion.Counters["duo_turns"] = turns;
                            break;
                        case KelThuzadCardId:
                            ResummonLeftUndead(minion, "kel-thuzad");
                            break;
                        case FamishedFelbatCardId:
                            foreach (var demon in State.Player.Board.Where(card => card.Tribes.Contains(Tribe.Demon)).ToList())
                            {
                                DevourHighestHealthShopMinion(demon, multiplier);
                            }

                            break;
                        case FelFlameDrakeCardId:
                            State.Player.Tavern.TavernSpellBonusAttack += multiplier;
                            State.Player.Tavern.TavernSpellBonusHealth += multiplier;
                            break;
                        case ScreamingBansheeCardId:
                            minion.Counters.TryGetValue("banshee_bonus", out var bonus);
                            BuffAllMinions(State.Player.Board, multiplier + bonus, multiplier + bonus, "Screaming Banshee");
                            break;
                        case BrashPirateCardId:
                            var leftmostPirate = State.Player.Board.FirstOrDefault(card => card.Tribes.Contains(Tribe.Pirate));
                            for (var index = 0; index < 1 + State.Player.Tavern.CardsPlayedThisTurn; index += 1)
                            {
                                BuffMinion(leftmostPirate, 3 * multiplier, 3 * multiplier, "Brash Pirate");
                            }

                            break;
                        case MurculesCardId:
                            AddMinionByCardIdToHand((State.Round + repeat) % 2 == 0 ? MurgletonAuntieCardId : MurgletonDaddyCardId, "murcules");
                            break;
                    }
                }
            }
        }

        private void HandleTurnEndedForTierSixSevenMinions()
        {
            foreach (var minion in State.Player.Board.ToList())
            {
                var multiplier = minion.Golden ? 2 : 1;
                switch (minion.CardId)
                {
                    case "BG28_595":
                        AddRandomTavernSpellToHand(TavernRules.MaxTavernTier, 2 * multiplier, "firestarter");
                        break;
                    case MoonsteelJuggernautCardId:
                        minion.Counters.TryGetValue("moonsteel_bonus", out var bonus);
                        AddMagneticSatellitesToHand(2 * multiplier, 6 + bonus, 6 + bonus, "moonsteel");
                        minion.Counters["moonsteel_bonus"] = bonus + multiplier;
                        break;
                    case EarthsongShamanCardId:
                        var repeats = 1 + minion.Keywords.Count(keyword => keyword != Keyword.Windfury);
                        for (var repeat = 0; repeat < repeats * multiplier; repeat += 1)
                        {
                            ApplyBloodGemToAllFriendlyMinions("Earthsong Shaman");
                        }

                        break;
                    case MurozondThiefCardId:
                        var handTarget = State.Player.Tavern.Hand.FirstOrDefault(card => card.CardKind == CardKind.Minion);
                        BuffMinion(
                            handTarget,
                            StatMath.SaturatingMultiply(minion.Attack, multiplier, 0, StatMath.MaxStat),
                            StatMath.SaturatingMultiply(minion.MaxHealth, multiplier, 0, StatMath.MaxStat),
                            "Future Murloc");
                        break;
                }
            }
        }

        private void HandleCardBoughtForTierSixSevenMinions(MinionInstance bought)
        {
            if (bought == null)
            {
                return;
            }

            if (bought.CardKind == CardKind.TavernSpell)
            {
                foreach (var fungalmancer in State.Player.Board.Where(minion => minion.CardId == FelfinFungalmancerCardId).ToList())
                {
                    fungalmancer.Counters.TryGetValue("felfin_used_round", out var usedRound);
                    var maxUses = fungalmancer.Golden ? 2 : 1;
                    fungalmancer.Counters.TryGetValue("felfin_uses", out var uses);
                    if (usedRound != State.Round)
                    {
                        uses = 0;
                        fungalmancer.Counters["felfin_used_round"] = State.Round;
                    }

                    if (uses < maxUses)
                    {
                        AddTaughtMurlocToHand(bought, "felfin");
                        fungalmancer.Counters["felfin_uses"] = uses + 1;
                    }
                }

                return;
            }

            if (bought.CardKind != CardKind.Minion)
            {
                return;
            }

            foreach (var rock in State.Player.Board.Where(minion => minion.CardId == StoneAgeRockRockCardId).ToList())
            {
                rock.Counters.TryGetValue("rock_used_round", out var usedRound);
                if (usedRound == State.Round)
                {
                    continue;
                }

                BuffMinion(bought, 10, 10, "Stone Age Rock Rock");
                var multiplier = rock.Golden ? 2 : 1;
                BuffMinion(
                    bought,
                    StatMath.SaturatingMultiply(bought.Attack, multiplier, 0, StatMath.MaxStat),
                    StatMath.SaturatingMultiply(bought.MaxHealth, multiplier, 0, StatMath.MaxStat),
                    "Stone Age Rock Rock multiplier");
                rock.Counters["rock_used_round"] = State.Round;
            }
        }

        private void HandleGoldSpentForTierFiveMinions(int amount)
        {
            foreach (var tornado in State.Player.Board.Where(minion => minion.CardId == WindfallTornadoCardId).ToList())
            {
                tornado.Counters.TryGetValue("gold_spent", out var spent);
                spent += amount;
                while (spent >= 7)
                {
                    spent -= 7;
                    TavernSpellEngine.Cast(MinionFactory.Create(spellCatalog.All.First(spell => spell.CardNumber == BorrowingEastWindCardNumber), BoardSide.Player, "windfall-tornado"), State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 619 + spent));
                }

                tornado.Counters["gold_spent"] = spent;
            }

            foreach (var elder in State.Player.Board.Where(minion => minion.CardId == DarkgazeElderCardId).ToList())
            {
                elder.Counters.TryGetValue("gold_spent", out var spent);
                spent += amount;
                while (spent >= 8)
                {
                    spent -= 8;
                    foreach (var quilboar in State.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Quilboar)).ToList())
                    {
                        BuffMinion(quilboar, 2 + State.Player.Tavern.BloodGemBonusAttack, 2 + State.Player.Tavern.BloodGemBonusHealth, "Darkgaze Elder");
                    }
                }

                elder.Counters["gold_spent"] = spent;
            }
        }

        private void HandleGoldSpentForTierSixSevenMinions(int amount)
        {
            foreach (var rogers in State.Player.Board.Where(minion => minion.CardId == AirAdmiralRogersCardId).ToList())
            {
                rogers.Counters.TryGetValue("gold_spent", out var spent);
                spent += amount;
                while (spent >= 9)
                {
                    spent -= 9;
                    AddBountiesToHand(rogers.Golden ? 2 : 1, "air-admiral-rogers");
                }

                rogers.Counters["gold_spent"] = spent;
            }
        }

        private void HandleMurlocPlayedForTierFourMinions(MinionInstance played)
        {
            if (played == null || !played.Tribes.Contains(Tribe.Murloc))
            {
                return;
            }

            foreach (var fillet in State.Player.Tavern.Hand.Where(card => card.CardId == FilletfighterCardId))
            {
                BuffMinion(fillet, fillet.Golden ? 10 : 5, fillet.Golden ? 10 : 5, "Filletfighter");
            }
        }

        private void HandleSpellCastOnTarget(MinionInstance spell, string targetInstanceId, bool fromHand = false)
        {
            var target = string.IsNullOrEmpty(targetInstanceId)
                ? State.Player.Board.FirstOrDefault() ?? State.Player.Tavern.Shop.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion)
                : State.Player.Board.FirstOrDefault(minion => minion.InstanceId == targetInstanceId);
            if (target == null)
            {
                return;
            }

            if ((spell.CardId == BloodGemCardId || spell.CardId == BristlebackBloodGemCardId) &&
                (State.Player.Tavern.BloodGemBonusAttack != 0 || State.Player.Tavern.BloodGemBonusHealth != 0))
            {
                BuffMinion(target, State.Player.Tavern.BloodGemBonusAttack, State.Player.Tavern.BloodGemBonusHealth, "Blood Gem Growth");
            }

            if (fromHand && IsBloodGemSpell(spell) && HasEquippedTrinketEffect(ToughTuskStickerEffectId))
            {
                ApplyToughTuskSticker(target);
            }

            if (spell.CardId == RebornBloodGemCardId &&
                (State.Player.Tavern.BloodGemBonusAttack != 0 || State.Player.Tavern.BloodGemBonusHealth != 0))
            {
                BuffMinion(target, State.Player.Tavern.BloodGemBonusAttack, State.Player.Tavern.BloodGemBonusHealth, "Blood Gem Growth");
            }

            if ((spell.CardId == BloodGemCardId || spell.CardId == BristlebackBloodGemCardId || spell.CardId == RebornBloodGemCardId) &&
                target.CardId == GemSmugglerRuggugCardId)
            {
                var other = State.Player.Board.FirstOrDefault(minion => minion.InstanceId != target.InstanceId);
                if (other != null)
                {
                    BuffMinion(other, 1 + State.Player.Tavern.BloodGemBonusAttack, 1 + State.Player.Tavern.BloodGemBonusHealth, "Ruggug Blood Gem");
                }
            }

            if ((spell.CardId == BloodGemCardId || spell.CardId == BristlebackBloodGemCardId || spell.CardId == RebornBloodGemCardId) &&
                State.Player.Board.Any(minion => minion.CardId == HotAirSurveyorCardId))
            {
                BuffMinion(target, 1 + State.Player.Tavern.BloodGemBonusAttack, 1 + State.Player.Tavern.BloodGemBonusHealth, "Hot-Air Surveyor");
            }

            if (spell.Tags.Contains("divine_shield_spell"))
            {
                if (target != null && !target.Keywords.Contains(Keyword.DivineShield))
                {
                    target.Keywords.Add(Keyword.DivineShield);
                }
            }

            if (spell.CardId == JailerStickerSpellCardId)
            {
                ResolveJailerStickerSpell(target);
                return;
            }

            if (spell.CardId == DemonbloodGourdSpellCardId)
            {
                DevourRandomShopMinion(target, 1);
            }

            if (spell.CardId == ShiftingTideSpellCardId)
            {
                var amount = target.Tribes.Contains(Tribe.Naga) || target.Tribes.Contains(Tribe.All) ? 4 : 2;
                BuffMinion(target, amount, amount, "Shifting Tide");
            }

            if (spell.Tags.Contains("spellcraft") && target.CardId == ZestyShakerCardId)
            {
                target.Counters.TryGetValue("zesty-copy-round", out var copiedRound);
                if (copiedRound != State.Round && State.Player.Tavern.Hand.Count < HandLimit)
                {
                    var copies = HasEquippedTrinketEffect(ShakerPortraitEffectId) ? 2 : 1;
                    var added = 0;
                    for (var index = 0; index < copies && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
                    {
                        var copy = spell.Clone();
                        copy.InstanceId = "zesty-copy-" + State.Round + "-" + State.Player.Tavern.Hand.Count;
                        copy.PoolSource = PoolSource.Copy;
                        copy.OriginPoolSource = PoolSource.Copy;
                        copy.PoolCopiesHeld = 0;
                        State.Player.Tavern.Hand.Add(copy);
                        added += 1;
                    }

                    target.Counters["zesty-copy-round"] = State.Round;
                    HandleCardsAddedToHand(added, "zesty-shaker");
                }
            }

            if (target.CardId == PufferquilCardId && !target.Keywords.Contains(Keyword.Venomous))
            {
                target.Keywords.Add(Keyword.Venomous);
                if (!target.Tags.Contains("temporary_venomous"))
                {
                    target.Tags.Add("temporary_venomous");
                }
            }

            if (IsJewelryBoxBloodGemSpell(spell))
            {
                ApplyJewelryBoxBloodGem(spell, target);
            }

            if (spell.CardId == NaturalBlessingCardNumber && HasEquippedTrinketEffect(BlessingPortraitEffectId))
            {
                ApplyBlessingPortraitToHand(target);
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds != null && definition.EffectIds.Contains(LorewalkerScrollEffectId))
                {
                    ApplyLorewalkerScroll(definition, target);
                }
            }
        }

        private void ResolveJailerStickerSpell(MinionInstance target)
        {
            if (target == null || !State.Player.Board.Remove(target))
            {
                return;
            }

            ReleaseMinionToPool(target);
            RecordOutsideCombatMinionDestroyed("Jailer Sticker");
            var count = EquippedTrinketDefinitions()
                .Where(definition => definition.EffectIds != null && definition.EffectIds.Contains(JailerStickerEffectId))
                .Select(definition => definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1)
                .DefaultIfEmpty(1)
                .Max();
            AddRandomTribeMinionToHand(Tribe.Undead, count, "Jailer Sticker");
            AddRecruitLog(RecruitLogType.Play, "Jailer Sticker destroyed " + target.Name + " and added " + count + " Undead.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void HandleCardsAddedToHand(int count, string source)
        {
            if (count <= 0)
            {
                return;
            }

            var addedCards = State.Player.Tavern.Hand
                .Skip(Math.Max(0, State.Player.Tavern.Hand.Count - count))
                .ToList();
            foreach (var card in addedCards)
            {
                SetupDoomsdayDragonEgg(card);
            }

            if (HasEquippedTrinketEffect("bronzebeard_portrait"))
            {
                ApplyBronzebeardPortraitTribes();
            }

            if (HasEquippedTrinketEffect("drakkari_portrait"))
            {
                ApplyDrakkariPortraitTribes();
            }

            if (HasEquippedTrinketEffect("enforcer_portrait"))
            {
                ApplyEnforcerPortraitTypes();
            }

            RecordQuestProgress(QuestObjectiveKind.AddCardsToHand, count);

            var pirateCardsAdded = addedCards.Count(card => card.Tribes.Contains(Tribe.Pirate));
            if (pirateCardsAdded > 0)
            {
                for (var added = 0; added < pirateCardsAdded; added += 1)
                {
                    foreach (var drust in State.Player.Board.Where(minion => minion.CardId == DrustfallenButcherHighCardId).ToList())
                    {
                        BuffAllMinions(State.Player.Board, drust.Golden ? 6 : 2, drust.Golden ? 6 : 2, "Drustfallen Butcher");
                        BuffAllMinions(State.Player.Board.Where(minion => minion.Golden), drust.Golden ? 12 : 4, drust.Golden ? 12 : 4, "Drustfallen Butcher golden");
                    }
                }
            }

            for (var added = 0; added < count; added += 1)
            {
                foreach (var peggy in State.Player.Board.Where(minion => minion.CardId == PeggyCardId).ToList())
                {
                    var target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId != peggy.InstanceId && minion.Tribes.Contains(Tribe.Pirate));
                    if (target != null)
                    {
                        BuffMinion(target, peggy.Golden ? 4 : 2, peggy.Golden ? 2 : 1, "Peggy Sturdybone");
                    }
                }
            }
        }

        private void HandleGoldSpent(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            State.Player.Tavern.GoldSpentThisTurn += amount;
            State.Player.Tavern.GoldSpentThisGame += amount;
            RecordQuestProgress(QuestObjectiveKind.SpendGold, amount);
            DispatchTrinketGoldSpent(amount);

            HandleGoldSpentForTierFiveMinions(amount);
            HandleGoldSpentForTierSixSevenMinions(amount);

            foreach (var courier in State.Player.Board.Where(minion => minion.CardId == GunpowderCourierCardId))
            {
                courier.Counters.TryGetValue("gold_spent", out var spent);
                spent += amount;
                while (spent >= 6)
                {
                    spent -= 6;
                    BuffAllMinions(State.Player.Board.Where(minion => minion.Tribes.Contains(Tribe.Pirate)), courier.Golden ? 4 : 2, 0, "Gunpowder Courier");
                }

                courier.Counters["gold_spent"] = spent;
            }

            foreach (var pirate in State.Player.Board.Where(minion => minion.CardId == DualWieldPirateCardId))
            {
                pirate.Counters.TryGetValue("gold_spent", out var spent);
                spent += amount;
                while (spent >= 5)
                {
                    spent -= 5;
                    var targets = State.Player.Board
                        .Where(minion => minion.Tribes.Contains(Tribe.Pirate))
                        .Take(2)
                        .ToList();
                    BuffAllMinions(targets, pirate.Golden ? 6 : 3, pirate.Golden ? 8 : 4, "Dual Wield Pirate");
                }

                pirate.Counters["gold_spent"] = spent;
            }
        }

        private void DispatchTrinketGoldSpent(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null)
                {
                    continue;
                }

                if (definition.EffectIds.Contains("booty_bay_brew"))
                {
                    ApplyBootyBayBrew(definition, amount);
                }

                if (definition.EffectIds.Contains(ExtravagantScaleEffectId))
                {
                    RecordExtravagantScaleGoldSpent(definition, amount);
                }

                if (definition.EffectIds.Contains(FancySpellbookEffectId))
                {
                    RecordFancySpellbookGoldSpent(definition, amount);
                }

                if (definition.EffectIds.Contains(SharkCannonEffectId))
                {
                    RecordSharkCannonGoldSpent(definition, amount);
                }
            }
        }

        private void RecordExtravagantScaleGoldSpent(TrinketDefinition definition, int amount)
        {
            var triggers = GetAdvancedMechanicCounter(ExtravagantScaleTriggersCounter);
            if (triggers >= 2)
            {
                return;
            }

            var progress = GetAdvancedMechanicCounter(ExtravagantScaleProgressCounter) + amount;
            while (progress >= 20 && triggers < 2)
            {
                progress -= 20;
                triggers += 1;
                foreach (var minion in State.Player.Board.Where(minion => minion != null && minion.CardKind == CardKind.Minion).ToList())
                {
                    BuffMinion(minion, Math.Max(0, minion.Attack), 0, definition.Name);
                }
            }

            SetAdvancedMechanicCounter(ExtravagantScaleProgressCounter, progress);
            SetAdvancedMechanicCounter(ExtravagantScaleTriggersCounter, triggers);
        }

        private void RecordFancySpellbookGoldSpent(TrinketDefinition definition, int amount)
        {
            var progress = GetAdvancedMechanicCounter(FancySpellbookProgressCounter) + amount;
            while (progress >= 7)
            {
                progress -= 7;
                CastTavernSpellImmediate(ShinyRingCardNumber, definition.Name);
            }

            SetAdvancedMechanicCounter(FancySpellbookProgressCounter, progress);
        }

        private void RecordSharkCannonGoldSpent(TrinketDefinition definition, int amount)
        {
            var progress = GetAdvancedMechanicCounter(SharkCannonProgressCounter) + amount;
            while (progress >= 10)
            {
                progress -= 10;
                var bonusIndex = GetAdvancedMechanicCounter(SharkCannonBonusCounter);
                var bonus = 1 + Math.Max(0, bonusIndex);
                BuffAllMinions(
                    State.Player.Board.Where(minion => minion != null && minion.CardKind == CardKind.Minion && HasTribe(minion, Tribe.Pirate)),
                    bonus,
                    bonus,
                    definition.Name);
                SetAdvancedMechanicCounter(SharkCannonBonusCounter, bonusIndex + 1);
            }

            SetAdvancedMechanicCounter(SharkCannonProgressCounter, progress);
        }

        private bool TryPlayMagneticMinion(int handIndex, MinionInstance source, int targetIndex)
        {
            if (source == null || !source.Keywords.Contains(Keyword.Magnetic) || targetIndex < 0 || targetIndex >= State.Player.Board.Count)
            {
                return false;
            }

            var target = State.Player.Board[targetIndex];
            if (!CanMagnetizeTo(source, target))
            {
                return false;
            }

            State.Player.Tavern.Hand.RemoveAt(handIndex);
            BuffMinion(target, source.Attack, source.MaxHealth, source.Name);
            foreach (var keyword in source.Keywords.Where(keyword => keyword != Keyword.Magnetic && !target.Keywords.Contains(keyword)))
            {
                target.Keywords.Add(keyword);
            }

            HandleMagnetizedForTierSixSevenMinions(source, target);
            DispatchHeroEffect(HeroEffectEventType.Magnetized, source, targetIndex: targetIndex);
            DispatchTrinketMagnetized(source, target);
            DispatchTrinketCardPlayed(source);
            DispatchTrinketMagneticMinionPlayed(source);
            AddRecruitLog(RecruitLogType.Play, "Magnetize " + source.Name + " to " + target.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            ResolvePlayerTriples();
            return true;
        }

        private static bool CanMagnetizeTo(MinionInstance source, MinionInstance target)
        {
            if (source.CardId == UtilityDroneCardId)
            {
                return target.Tribes.Contains(Tribe.Mech) || target.Tribes.Contains(Tribe.Elemental) || target.Tribes.Contains(Tribe.All);
            }

            return target.Tribes.Contains(Tribe.Mech) || target.Tribes.Contains(Tribe.All);
        }

        private void HandleMagnetizedForTierSixSevenMinions(MinionInstance magnetic, MinionInstance target)
        {
            if (magnetic == null || target == null)
            {
                return;
            }

            foreach (var student in State.Player.Board.Where(minion => minion.CardId == ScrapbookingStudentCardId).ToList())
            {
                BuffAllMinions(State.Player.Board, student.Golden ? 10 : 5, student.Golden ? 10 : 5, "Scrapbooking Student");
            }

            foreach (var beatboxer in State.Player.Board.Where(minion => minion.CardId == PolarizingBeatboxerCardId && minion.InstanceId != target.InstanceId).ToList())
            {
                var repeats = beatboxer.Golden ? 2 : 1;
                for (var index = 0; index < repeats; index += 1)
                {
                    BuffMinion(beatboxer, magnetic.Attack, magnetic.MaxHealth, "Polarizing Beatboxer");
                    foreach (var keyword in magnetic.Keywords.Where(keyword => keyword != Keyword.Magnetic && !beatboxer.Keywords.Contains(keyword)))
                    {
                        beatboxer.Keywords.Add(keyword);
                    }
                }
            }
        }

        private void AddShopGrowth(Tribe tribe, int attack, int health, string sourceId)
        {
            var fountainPenBonus = GetFountainPenExtraStats(sourceId, attack, health);
            State.Player.Tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
            {
                Scope = BuffScope.ShopGlobal,
                Tribe = tribe,
                Attack = StatMath.SaturatingAdd(attack, fountainPenBonus.Attack, 0, StatMath.MaxStat),
                Health = StatMath.SaturatingAdd(health, fountainPenBonus.Health, 0, StatMath.MaxStat),
                SourceId = sourceId
            });
        }

        private void GrowElementalsInTavernAndFuture(int attack, int health, string sourceId)
        {
            AddShopGrowth(Tribe.Elemental, attack, health, sourceId);
            BuffAllMinions(
                State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion && MatchesTribe(card, Tribe.Elemental)),
                attack,
                health,
                sourceId);
        }

        private void AddLowTierShopGrowth(int attack, int health, string sourceId)
        {
            State.Player.Tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
            {
                Scope = BuffScope.ShopGlobal,
                Tribe = Tribe.All,
                TierCap = 3,
                Attack = attack,
                Health = health,
                SourceId = sourceId
            });
            BuffAllMinions(
                State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion && card.TavernTier <= 3),
                attack,
                health,
                sourceId);
        }

        private void BuffOneOfEachFriendlyType(int attack, int health, string sourceId)
        {
            var seen = new HashSet<Tribe>();
            foreach (var minion in State.Player.Board)
            {
                var tribe = BoardTribeAnalyzer.GetCountedTribes(minion).FirstOrDefault(candidate => candidate != Tribe.None && !seen.Contains(candidate));
                if (tribe == Tribe.None)
                {
                    continue;
                }

                seen.Add(tribe);
                BuffMinion(minion, attack, health, sourceId);
            }
        }

        private void AddRandomTavernSpellToHand(int maxTier, int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 541 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableTavernSpells()
                .Where(spell => spell.TavernTier <= Math.Max(1, maxTier))
                .ToList();
            AddRandomTavernSpellsFromCandidates(candidates, count, source, rng);
        }

        private void AddRandomTavernSpellToHandByCost(int cost, int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 547 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableTavernSpells()
                .Where(spell => spell.Cost == cost)
                .ToList();
            AddRandomTavernSpellsFromCandidates(candidates, count, source, rng);
        }

        private void AddRandomTavernSpellsFromCandidates(List<TavernSpellDefinition> candidates, int count, string source, SeededRng rng)
        {
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; index += 1)
            {
                State.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, source + "-" + State.Round + "-" + index));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private void ApplyRefreshBuffToShop(List<MinionInstance> shop)
        {
            ApplyRefreshBuffToShop(shop, State.Player.Tavern.RefreshBuffAttack, State.Player.Tavern.RefreshBuffHealth, "Refresh Growth");
        }

        private void ApplyRefreshBuffToShop(List<MinionInstance> shop, int attack, int health, string sourceId)
        {
            if (attack == 0 && health == 0)
            {
                return;
            }

            var target = shop?.FirstOrDefault(card => card != null && card.CardKind == CardKind.Minion);
            if (target != null)
            {
                BuffMinion(target, attack, health, sourceId);
            }
        }

        private void ApplyRefreshRightmostBuffToShop(List<MinionInstance> shop)
        {
            var attack = State.Player.Tavern.RefreshRightmostBuffAttack;
            var health = State.Player.Tavern.RefreshRightmostBuffHealth;
            if (attack == 0 && health == 0)
            {
                return;
            }

            var target = shop?.LastOrDefault(card => card != null && card.CardKind == CardKind.Minion);
            if (target != null)
            {
                BuffMinion(target, attack, health, "Borrowing East Wind");
            }
        }

        private void ApplyHelpfulRefresh(List<MinionInstance> shop)
        {
            if (State.Player.Tavern.HelpfulRefreshes <= 0)
            {
                return;
            }

            var targets = shop?.Where(card => card != null && card.CardKind == CardKind.Minion).ToList();
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            BuffAllMinions(targets, 2, 2, "Knockoff Wisdomball");
            State.Player.Tavern.HelpfulRefreshes -= 1;
        }

        private IEnumerable<MinionInstance> OtherFriendlyDragons(MinionInstance source)
        {
            return State.Player.Board.Where(minion => minion.InstanceId != source.InstanceId && minion.Tribes.Contains(Tribe.Dragon));
        }

        private void BuffFirstBeastForMutableBeetle(MinionInstance source, int multiplier)
        {
            var target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId != source.InstanceId && minion.Tribes.Contains(Tribe.Beast))
                ?? State.Player.Board.FirstOrDefault(minion => minion.Tribes.Contains(Tribe.Beast));
            if (target == null)
            {
                return;
            }

            BuffMinion(target, multiplier, multiplier, "Mutable Beetle");
            if (!target.Keywords.Contains(Keyword.Reborn))
            {
                target.Keywords.Add(Keyword.Reborn);
            }
        }

        private void DestroyUndeadAndAddPlainCopies(MinionInstance source, string battlecryTargetId, int count)
        {
            var target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId == battlecryTargetId);
            if (target == null || target.InstanceId == source.InstanceId || !target.Tribes.Contains(Tribe.Undead))
            {
                target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId != source.InstanceId && minion.Tribes.Contains(Tribe.Undead));
            }

            if (target == null)
            {
                return;
            }

            State.Player.Board.Remove(target);
            RecordOutsideCombatMinionDestroyed("Disguised Graverobber");
            var added = 0;
            var copies = Math.Max(1, count);
            for (var index = 0; index < copies && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                var definition = catalog.All.FirstOrDefault(minion => minion.CardId == target.CardId);
                var copy = definition != null
                    ? MinionFactory.Create(definition, BoardSide.Player, "graverobber-copy-" + State.Round + "-" + index, target.Golden, PoolSource.Copy, 0)
                    : CreatePlainCopy(target, "copy-" + target.InstanceId + "-" + State.Round + "-" + index);
                copy.Owner = BoardSide.Player;
                copy.PoolSource = PoolSource.Copy;
                copy.PoolCopiesHeld = 0;
                State.Player.Tavern.Hand.Add(copy);
                added += 1;
            }

            HandleCardsAddedToHand(added, "disguised-graverobber");
        }

        private void HandleDemonPlayedForWrathWeavers(MinionInstance played)
        {
            if (played == null || played.CardKind != CardKind.Minion || !played.Tribes.Contains(Tribe.Demon))
            {
                return;
            }

            foreach (var weaver in State.Player.Board.Where(minion => minion.CardId == WrathWeaverCardId))
            {
                var repeat = weaver.Golden ? 2 : 1;
                for (var index = 0; index < repeat; index += 1)
                {
                    DamagePlayerHero(1);
                    BuffMinion(weaver, 2, 1, "愤怒编织者");
                }
            }
        }

        private void DamagePlayerHero(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var healthBefore = State.Player.Health;
            var rewinders = State.Player.Board.Where(minion => minion.CardId == SoulRewinderCardId).ToList();
            if (rewinders.Count == 0)
            {
                State.Player.Health = Math.Max(0, State.Player.Health - amount);
                var tookDamage = State.Player.Health < healthBefore;
                TriggerAshenCorruptors(amount);
                if (tookDamage)
                {
                    RecordFelburnedLedgerHeroDamage();
                    RecordNetherPendantHeroDamage();
                }

                if (HasAshenCorruptor())
                {
                    State.Player.Health = healthBefore;
                }

                return;
            }

            foreach (var rewinder in rewinders)
            {
                BuffMinion(rewinder, 0, rewinder.Golden ? 2 : 1, "灵魂回溯者");
            }
            TriggerAshenCorruptors(amount);
        }

        private void RecordNetherPendantHeroDamage()
        {
            if (!HasEquippedTrinketEffect(NetherPendantEffectId))
            {
                return;
            }

            var progress = IncrementAdvancedMechanicCounter(NetherPendantDamageCounter);
            if (progress < 3)
            {
                return;
            }

            SetAdvancedMechanicCounter(NetherPendantDamageCounter, progress % 3);
            var bonus = IncrementAdvancedMechanicCounter(NetherPendantBonusCounter);
            ApplyTrinketShopAuras(State.Player.Tavern.Shop);
            AddRecruitLog(
                RecruitLogType.Play,
                "Nether Pendant: Tavern minions have +" + (2 + bonus) + "/+" + (2 + bonus) + ".",
                State.Player.Tavern.Gold,
                State.Player.Tavern.Gold);
        }

        private bool HasAshenCorruptor()
        {
            return State.Player.Board.Any(minion => minion.CardId == AshenCorruptorCardId);
        }

        private void TriggerAshenCorruptors(int amount)
        {
            foreach (var corruptor in State.Player.Board.Where(minion => minion.CardId == AshenCorruptorCardId).ToList())
            {
                var multiplier = corruptor.Golden ? 2 : 1;
                for (var index = 0; index < Math.Max(1, amount); index += 1)
                {
                    BuffAllMinions(State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), multiplier, multiplier, "Ashen Corruptor");
                }
            }
        }

        private void HandleQuilboarPlayedForProphets(MinionInstance played)
        {
            if (played == null || played.CardKind != CardKind.Minion || !played.Tribes.Contains(Tribe.Quilboar))
            {
                return;
            }

            foreach (var prophet in State.Player.Board.Where(minion => minion.CardId == ProphetOfTheBoarCardId))
            {
                AddBloodGemsToHand(prophet.Golden ? 2 : 1, "prophet");
            }
        }

        private List<MinionInstance> CreateCombatStartPlayerBoard()
        {
            var board = State.Player.Board.Select(minion => minion.Clone()).ToList();
            var scouts = State.Player.Tavern.Hand.Where(card => card.CardId == FlightyScoutCardId).ToList();
            foreach (var scout in scouts)
            {
                if (board.Count >= BoardLimit)
                {
                    break;
                }

                var copy = scout.Clone();
                copy.InstanceId = "combat-scout-copy-" + board.Count + "-" + copy.InstanceId;
                copy.Owner = BoardSide.Player;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                if (copy.Golden)
                {
                    StatMath.DoubleCurrentStats(copy, true);
                }

                board.Add(copy);
            }

            return board;
        }

        private bool DevourRandomShopMinion(MinionInstance eater, int multiplier)
        {
            var candidates = State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                return false;
            }

            var rng = new SeededRng(State.Seed + State.Round * 313 + State.Player.Tavern.RecruitLog.Count);
            var picked = rng.Pick(candidates);
            State.Player.Tavern.Shop[picked.Index] = null;
            BuffMinion(
                eater,
                StatMath.SaturatingMultiply(picked.Card.Attack, multiplier, 0, StatMath.MaxStat),
                StatMath.SaturatingMultiply(picked.Card.Health, multiplier, 0, StatMath.MaxStat),
                "挑食魔犬");
            HandleDevourForTierSixSevenMinions();
            ReleaseMinionToPool(picked.Card);
            RecordStatueOfHireekConsume(1);
            return true;
        }

        private void DevourHighestHealthShopMinion(MinionInstance eater, int multiplier)
        {
            var picked = State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .OrderByDescending(item => item.Card.MaxHealth)
                .FirstOrDefault();
            if (picked == null)
            {
                return;
            }

            State.Player.Tavern.Shop[picked.Index] = null;
            BuffMinion(
                eater,
                StatMath.SaturatingMultiply(picked.Card.Attack, multiplier, 0, StatMath.MaxStat),
                StatMath.SaturatingMultiply(picked.Card.MaxHealth, multiplier, 0, StatMath.MaxStat),
                "Wildfire Executioner");
            HandleDevourForTierSixSevenMinions();
            ReleaseMinionToPool(picked.Card);
            RecordStatueOfHireekConsume(1);
        }

        private void RecordStatueOfHireekConsume(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var definition = EquippedTrinketDefinitions().FirstOrDefault(trinket =>
                trinket.EffectIds != null && trinket.EffectIds.Contains(StatueOfHireekEffectId));
            if (definition == null)
            {
                return;
            }

            var progress = GetAdvancedMechanicCounter(StatueOfHireekProgressCounter) + amount;
            var rewards = progress / 2;
            SetAdvancedMechanicCounter(StatueOfHireekProgressCounter, progress % 2);
            if (rewards <= 0)
            {
                return;
            }

            AddRandomTavernSpellToHand(State.Player.Tavern.Tier, rewards, definition.Name);
            AddRecruitLog(RecruitLogType.Play, definition.Name + ": consumed Tavern minions added " + rewards + " Tavern spell(s).", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void HandleDevourForTierSixSevenMinions()
        {
            foreach (var ingestor in State.Player.Board.Where(minion => minion.CardId == StrengthIngestorCardId).ToList())
            {
                BuffAllMinions(State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion), ingestor.Golden ? 2 : 1, ingestor.Golden ? 2 : 1, "Strength Ingestor");
            }
        }

        private void AddBloodGemsToHand(int count, string source)
        {
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(CreateBloodGemCard(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private void AddSlimyShieldsToHand(int count, string source)
        {
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(CreateSlimyShieldCard(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private void AddGeneratedSpellsToHand(string cardId, int count, string source)
        {
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(CreateGeneratedSpellCard(cardId, source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private bool AddTrinketSpellcraftCardToHand(string cardId, string source)
        {
            var before = State.Player.Tavern.Hand.Count;
            AddGeneratedSpellsToHand(cardId, 1, source);
            if (State.Player.Tavern.Hand.Count <= before)
            {
                return false;
            }

            var added = State.Player.Tavern.Hand.Last();
            if (!added.Tags.Contains("temporary_spellcraft_card"))
            {
                added.Tags.Add("temporary_spellcraft_card");
            }

            return true;
        }

        private void AddRandomSpellcraftSpellsToHand(int count, string source)
        {
            var tavern = State.Player.Tavern;
            var rng = new SeededRng(State.Seed + State.Round * 3251 + tavern.RecruitLog.Count);
            var before = tavern.Hand.Count;
            for (var index = 0; index < count && tavern.Hand.Count < HandLimit; index += 1)
            {
                var cardId = RandomSpellcraftSpellCardIds[rng.NextInt(RandomSpellcraftSpellCardIds.Length)];
                var card = CreateGeneratedSpellCard(cardId, source + "-" + State.Round + "-" + tavern.Hand.Count);
                if (!card.Tags.Contains("temporary_spellcraft_card"))
                {
                    card.Tags.Add("temporary_spellcraft_card");
                }

                tavern.Hand.Add(card);
            }

            HandleCardsAddedToHand(tavern.Hand.Count - before, source);
        }

        private void AddBountiesToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 563 + State.Player.Tavern.RecruitLog.Count);
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(CreateGeneratedSpellCard(BountyCardIds[rng.NextInt(BountyCardIds.Length)], source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private int AddTavernSpellToHand(string cardNumber, string source)
        {
            return AddTavernSpellToHand(cardNumber, 1, source);
        }

        private int AddTavernSpellToHand(string cardNumber, int count, string source)
        {
            if (State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return 0;
            }

            var definition = spellCatalog.All.FirstOrDefault(spell => spell.CardNumber == cardNumber);
            if (definition == null)
            {
                return 0;
            }

            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(MinionFactory.Create(definition, BoardSide.Player, source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
            }

            var added = State.Player.Tavern.Hand.Count - before;
            HandleCardsAddedToHand(added, source);
            return added;
        }

        private void CastTavernSpellImmediate(string cardNumber, string source)
        {
            var definition = spellCatalog.All.FirstOrDefault(spell => spell.CardNumber == cardNumber || spell.Id == cardNumber);
            if (definition == null)
            {
                return;
            }

            var spell = MinionFactory.Create(definition, BoardSide.Player, source + "-" + State.Round + "-" + State.Player.Tavern.RecruitLog.Count);
            if (!TryEnterAutomaticTavernSpellCast(source))
            {
                return;
            }

            try
            {
                CastAutomaticTavernSpell(spell, source, -1, State.Seed + State.Round * 701 + State.Player.Tavern.RecruitLog.Count);
            }
            finally
            {
                ExitAutomaticTavernSpellCast();
            }
        }

        private void SpendGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            State.Player.Tavern.Gold -= amount;
            HandleGoldSpent(amount);
        }

        private int CastTavernSpellImmediate(string cardNumber, int count, string source, string instancePrefix)
        {
            var definition = spellCatalog.All.FirstOrDefault(spell => spell.CardNumber == cardNumber || spell.Id == cardNumber);
            if (definition == null || count <= 0)
            {
                return 0;
            }

            if (!TryEnterAutomaticTavernSpellCast(source))
            {
                return 0;
            }

            var cast = 0;
            try
            {
                while (cast < count)
                {
                    var spell = MinionFactory.Create(
                        definition,
                        BoardSide.Player,
                        instancePrefix + "-" + State.Round + "-" + State.Player.Tavern.RecruitLog.Count + "-" + cast);
                    if (!CastAutomaticTavernSpell(
                        spell,
                        source,
                        -1,
                        State.Seed + State.Round * 701 + State.Player.Tavern.RecruitLog.Count + cast))
                    {
                        break;
                    }

                    cast += 1;
                }
            }
            finally
            {
                ExitAutomaticTavernSpellCast();
            }

            return cast;
        }

        private void MakeGoldenFriendlyTierSixOrLower(int count)
        {
            foreach (var target in State.Player.Board.Where(minion => minion.TavernTier <= 6 && minion.CardId != CaptainSandersCardId).Take(count).ToList())
            {
                MakeGoldenInPlace(target);
            }
        }

        private void ApplyBloodGemToAllFriendlyMinions(string source)
        {
            foreach (var target in State.Player.Board.ToList())
            {
                BuffMinion(target, 1 + State.Player.Tavern.BloodGemBonusAttack, 1 + State.Player.Tavern.BloodGemBonusHealth, source);
            }
        }

        private static MinionInstance CreateGeneratedTavernSpellCard(string cardId, string suffix)
        {
            switch (cardId)
            {
                case DreamersEmbraceCardNumber:
                    return CreateGeneratedTavernSpellCard(cardId, "Dreamer's Embrace", "Give a minion +3/+3. If it's a Dragon or Murloc, give it +6/+6 instead.", 3, 3, suffix, "buff_spell", "targeted_spell");
                case NaturalBlessingCardNumber:
                    return CreateGeneratedTavernSpellCard(cardId, "Natural Blessing", "Give minions of the target's type in your warband and Tavern +3/+3.", 4, 4, suffix, "buff_spell", "targeted_spell");
                case CloningConchCardNumber:
                    return CreateGeneratedTavernSpellCard(cardId, "Cloning Conch", "Get a random Murloc and a copy of it.", 4, 4, suffix, "murloc_spell");
                case DuplicatingLensCardNumber:
                    return CreateGeneratedTavernSpellCard(cardId, "Duplicating Lens", "Choose a minion. Get a plain copy of it.", 4, 4, suffix, "copy_spell", "targeted_spell");
                case GoldenizerCardNumber:
                    return CreateGeneratedTavernSpellCard(cardId, "Goldenizer", "Make a friendly minion Golden.", 0, 5, suffix, "golden_spell", "targeted_spell");
                case MaraudersContractCardNumber:
                    return CreateGeneratedTavernSpellCard(cardId, "Marauder's Contract", "Steal a random Pirate from the Tavern.", 3, 5, suffix, "pirate_spell", "steal_spell");
                case JewelryBoxTauntGemCardId:
                    return CreateGeneratedTavernSpellCard(cardId, "Taunting Blood Gem", "Give a Quilboar +1/+1 and Taunt.", 0, 0, suffix, "blood_gem", "targeted_spell", "quilboar_spell");
                case JewelryBoxDivineShieldGemCardId:
                    return CreateGeneratedTavernSpellCard(cardId, "Gleaming Blood Gem", "Give a Quilboar +1/+1 and Divine Shield.", 0, 0, suffix, "blood_gem", "targeted_spell", "quilboar_spell");
                case JewelryBoxRebornGemCardId:
                    return CreateGeneratedTavernSpellCard(cardId, "Reborn Blood Gem", "Give a Quilboar +1/+1 and Reborn.", 0, 0, suffix, "blood_gem", "targeted_spell", "quilboar_spell");
                case CoinPouch3GoldProxyCardId:
                    return CreateGeneratedTavernSpellCard(cardId, "3-Gold Coin Pouch", "Gain 3 Gold.", 0, 0, suffix, "coin_pouch", "trinket_proxy");
                default:
                    throw new InvalidOperationException("Generated Tavern spell card id does not exist: " + cardId);
            }
        }

        private static MinionInstance CreateGeneratedTavernSpellCard(string cardId, string name, string text, int cost, int tier, string suffix, params string[] tags)
        {
            var allTags = new List<string> { "generated_spell", "generated_tavern_spell", "tavern_spell" };
            if (tags != null)
            {
                allTags.AddRange(tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
            }

            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "generated-tavern-spell-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = cost,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 0,
                MaxHealth = 0,
                TavernTier = tier,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                OfficialKeywords = new List<Keyword> { Keyword.TavernSpell },
                Text = text,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                AttacksThisCombat = 0,
                OriginPoolSource = PoolSource.Copy,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                EffectIds = new List<string>(),
                Tags = allTags
            };
        }

        private static MinionInstance CreateGeneratedSpellCard(string cardId, string suffix)
        {
            switch (cardId)
            {
                case BloodGemCardId:
                    return CreateBloodGemCard(suffix);
                case SlimyShieldCardId:
                    return CreateSlimyShieldCard(suffix);
                case BristlebackBloodGemCardId:
                    return CreateBristlebackBloodGemCard(suffix);
                case RebornBloodGemCardId:
                    return CreateRebornBloodGemCard(suffix);
                case PointyArrowCardId:
                    return CreatePointyArrowCard(suffix);
                case ReefRifferSpellCardId:
                    return CreateReefRifferSpellCard(suffix);
                case SurfNSurfSpellCardId:
                    return CreateSurfNSurfSpellCard(suffix);
                case DeepSeaAnglerSpellCardId:
                    return CreateDeepSeaAnglerSpellCard(suffix);
                case DeepBlueSpellCardId:
                    return CreateDeepBlueSpellCard(suffix);
                case VolcanicVisitorAttackSpellCardId:
                    return CreateVolcanicVisitorSpellCard(suffix, true);
                case VolcanicVisitorHealthSpellCardId:
                    return CreateVolcanicVisitorSpellCard(suffix, false);
                case TimewarpedGlowscaleSpellCardId:
                    return CreateTimewarpedGlowscaleSpellCard(suffix);
                case WearyMageSpellCardId:
                    return CreateWearyMageSpellCard(suffix);
                case ThaumaturgistSpellCardId:
                    return CreateThaumaturgistSpellCard(suffix);
                case FrostlingPriestessSpellCardId:
                    return CreateFrostlingPriestessSpellCard(suffix);
                case PreciousPearlSpellCardId:
                    return CreateTrinketSpellcraftCard(cardId, "Precious Pearl", "Spellcraft: Give a minion +30/+30 until next turn.", suffix, "buff_spell");
                case OphidianStaffSpellCardId:
                    return CreateTrinketSpellcraftCard(cardId, "Ophidian Staff", "Spellcraft: Give a Beast +2/+2 and Reborn until next turn.", suffix, "buff_spell", "beast_target_spell", "reborn_grant");
                case VibrantBubbleSpellCardId:
                    return CreateTrinketSpellcraftCard(cardId, "Vibrant Bubble", "Spellcraft: Give a Murloc a random Bonus Keyword until next turn.", suffix, "murloc_target_spell", "keyword_grant");
                case DoubleStitchNeedleSpellCardId:
                    return CreateTrinketSpellcraftCard(cardId, "Double Stitch Needle", "Spellcraft: Choose a friendly minion. Double its stats and lock it in your hand for 1 turn.", suffix, "buff_spell", "lock_spell");
                case TokenOfTheOldGodsSpellCardId:
                    return CreateTrinketSpellcraftCard(cardId, "Token of the Old Gods", "Spellcraft: Transform a friendly minion into a random minion from a Tavern Tier higher.", suffix, "transform_spell");
                case ChillmereMosaicSpellCardId:
                    return CreateNonTargetedTrinketSpellcraftCard(cardId, "Chillmere Mosaic", "Spellcraft: Refresh the Tavern with Battlecry minions. They cost (1).", suffix, "shop_refresh_spell", "battlecry_shop_refresh");
                case JailerStickerSpellCardId:
                    return CreateTrinketSpellcraftCard(cardId, "Jailer Sticker", "Spellcraft: Destroy a friendly Undead to get random Undead.", suffix, "destroy_spell", "undead_target_spell");
                case DemonbloodGourdSpellCardId:
                    return CreateTrinketSpellcraftCard(cardId, "Demonblood Gourd", "Spellcraft: A friendly minion consumes a random minion in the Tavern.", suffix, "devour_spell");
                case ShiftingTideSpellCardId:
                    return CreateTrinketSpellcraftCard(cardId, "Shifting Tide", "Spellcraft: Give a minion +2/+2. If it is a Naga, give it +4/+4 instead.", suffix, "buff_spell", "naga_bonus_spell");
                case DeepwaterSchoolCardId:
                    return CreateDeepwaterSchoolCard(suffix);
                case ArcaneConsumptionCardId:
                    return CreateArcaneConsumptionCard(suffix);
                case EnhanceAMaticTauntSpellCardId:
                    return CreateQuestKeywordSpellCard(cardId, "Mega Horn", "Give a minion +5/+5 and Taunt.", suffix, Keyword.Taunt);
                case EnhanceAMaticWindfurySpellCardId:
                    return CreateQuestKeywordSpellCard(cardId, "Blazing Blades", "Give a minion +5/+5 and Windfury.", suffix, Keyword.Windfury);
                case EnhanceAMaticDivineShieldSpellCardId:
                    return CreateQuestKeywordSpellCard(cardId, "Bunker Plating", "Give a minion +5/+5 and Divine Shield.", suffix, Keyword.DivineShield);
                case EnhanceAMaticRebornSpellCardId:
                    return CreateQuestKeywordSpellCard(cardId, "Death Rewinder", "Give a minion +5/+5 and Reborn.", suffix, Keyword.Reborn);
                case RushingWindsSpellCardId:
                    return CreateQuestKeywordSpellCard(cardId, "Rushing Winds", "Give a minion Windfury and Divine Shield.", suffix, Keyword.Windfury, Keyword.DivineShield);
                case TimelineAcceleratorSpellCardId:
                    return CreateQuestUtilitySpellCard(cardId, "Timeline Accelerator", "Transform a friendly minion into one from a Tavern Tier higher.", suffix, true);
                case KidnapSackSpellCardId:
                    return CreateQuestUtilitySpellCard(cardId, "Kidnap Sack", "Move a non-Golden Tavern card to your hand.", suffix, false);
                case GoldenHammerSpellCardId:
                    return CreateQuestUtilitySpellCard(cardId, "The Golden Hammer", "Make a friendly minion Golden until next turn.", suffix, true);
                case HealthyBountyCardId:
                case HostileBountyCardId:
                case SelfishBountyCardId:
                case FriendlyBountyCardId:
                case WealthyBountyCardId:
                case OfficialHealthyBountyCardId:
                case OfficialHostileBountyCardId:
                case OfficialSelfishBountyCardId:
                case OfficialFriendlyBountyCardId:
                case OfficialWealthyBountyCardId:
                    return CreateBountyCard(cardId, suffix);
                default:
                    throw new InvalidOperationException("Generated spell card id does not exist: " + cardId);
            }
        }

        private static MinionInstance CreateTrinketSpellcraftCard(string cardId, string name, string text, string suffix, params string[] tags)
        {
            var allTags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell" };
            if (tags != null)
            {
                allTags.AddRange(tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
            }

            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "trinket-spellcraft-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = 0,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = text,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = allTags
            };
        }

        private static MinionInstance CreateNonTargetedTrinketSpellcraftCard(string cardId, string name, string text, string suffix, params string[] tags)
        {
            var allTags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card" };
            if (tags != null)
            {
                allTags.AddRange(tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
            }

            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "trinket-spellcraft-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = 0,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = text,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = allTags
            };
        }

        private static MinionInstance CreateQuestKeywordSpellCard(string cardId, string name, string text, string suffix, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "quest-spell-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = 0,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = keywords == null ? new List<Keyword>() : keywords.ToList(),
                Text = text,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_spell", "quest_reward_spell", "targeted_spell" }
            };
        }

        private static MinionInstance CreateQuestUtilitySpellCard(string cardId, string name, string text, string suffix, bool targeted)
        {
            var tags = new List<string> { "generated_spell", "quest_reward_spell" };
            if (targeted)
            {
                tags.Add("targeted_spell");
            }

            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "quest-spell-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = 0,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = text,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = tags
            };
        }

        private static MinionInstance CreateShifterZerusProxyCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "quest-zerus-" + suffix,
                DefinitionId = "quest-shifter-zerus",
                CardId = ShifterZerusProxyCardId,
                Name = "Shifter Zerus",
                Cost = 3,
                BaseAttack = 1,
                BaseHealth = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 3,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = "Each turn this is in your hand, transform it into a random minion.",
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_minion", "quest_transforming_zerus" }
            };
        }

        private static MinionInstance CreateMagicfinToken(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "quest-magicfin-" + suffix,
                DefinitionId = "quest-magicfin-token",
                CardId = MagicfinTokenCardId,
                Name = "Magicfin",
                Cost = 3,
                BaseAttack = 1,
                BaseHealth = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.Murloc },
                Keywords = new List<Keyword>(),
                Text = "Generated by Magicfin Relic.",
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_minion", "magicfin_relic" }
            };
        }

        private void AddRandomTierOneMinionsToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 431 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions().Where(minion => minion.InPool && minion.TavernTier == 1).ToList();
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; index += 1)
            {
                State.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, source + "-" + State.Round + "-" + index, false, PoolSource.Copy, 0));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private int AddRandomTierMinionsToHand(int tier, int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 673 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions().Where(minion => minion.InPool && minion.TavernTier == tier && !minion.CardId.StartsWith("BGDUO", StringComparison.Ordinal)).ToList();
            return AddRandomMinionsFromCandidates(candidates, count, source, rng);
        }

        private void AddRandomTierOneNagaToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 653 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions().Where(minion => minion.InPool && minion.TavernTier == 1 && MatchesTribe(minion, Tribe.Naga)).ToList();
            AddRandomMinionsFromCandidates(candidates, count, source, rng);
        }

        private void AddRandomBattlecryMinionToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 659 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions().Where(minion => minion.InPool && minion.Keywords.Contains(Keyword.Battlecry)).ToList();
            AddRandomMinionsFromCandidates(candidates, count, source, rng);
        }

        private void AddMagneticSatellitesToHand(int count, int attack, int health, string source)
        {
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(new MinionInstance
                {
                    CardKind = CardKind.Minion,
                    InstanceId = source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count,
                    DefinitionId = "moonsteel-satellite",
                    CardId = "MOONSTEEL_SATELLITE",
                    Name = "Moonsteel Satellite",
                    BaseAttack = attack,
                    BaseHealth = health,
                    Attack = attack,
                    Health = health,
                    MaxHealth = health,
                    TavernTier = 6,
                    Owner = BoardSide.Player,
                    Tribes = new List<Tribe> { Tribe.Mech },
                    Keywords = new List<Keyword> { Keyword.Magnetic },
                    Enchantments = new List<Enchantment>(),
                    Counters = new Dictionary<string, int>(),
                    Tags = new List<string> { "generated_minion", "magnetic" },
                    PoolSource = PoolSource.Copy,
                    PoolCopiesHeld = 0
                });
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private void AddTaughtMurlocToHand(MinionInstance spell, string source)
        {
            if (State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            State.Player.Tavern.Hand.Add(new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count,
                DefinitionId = "taught-murloc",
                CardId = "TAUGHT_MURLOC",
                Name = "Taught Murloc",
                BaseAttack = 1,
                BaseHealth = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 1,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.Murloc },
                Keywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int> { { "taught_spell", spell?.CardId?.GetHashCode() ?? 0 } },
                Tags = new List<string> { "generated_minion", "taught_spell:" + (spell?.CardId ?? string.Empty) },
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            });
            HandleCardsAddedToHand(1, source);
        }

        private void AddMinionByCardIdToHand(string cardId, string source)
        {
            if (State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var definition = catalog.All.FirstOrDefault(minion => minion.CardId == cardId);
            if (definition == null)
            {
                if (string.Equals(cardId, TitusRivendareCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateTitusRivendareProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, BassgillCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateBassgillProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, LivingAzeriteCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateLivingAzeriteProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, TimewarpedGlowscaleCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateTimewarpedGlowscaleProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, WearyMageCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateWearyMageProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, ThaumaturgistCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateThaumaturgistProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, ArcaneBehemothCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateArcaneBehemothProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, FacelessManipulatorCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateFacelessManipulatorProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, TimewarpedPoetCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateTimewarpedPoetProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, TimewarpedRadioStarCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateTimewarpedRadioStarProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, FishOfNzothCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateFishOfNzothProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, TimewarpedLeapfroggerCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateTimewarpedLeapfroggerProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, TimewarpedSkipperCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateTimewarpedSkipperProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, LightfangEnforcerCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateLightfangEnforcerProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, SnarlingConductorCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateSnarlingConductorProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, GrittyHeadhunterCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateGrittyHeadhunterProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }
                else if (string.Equals(cardId, HackerfinCardId, StringComparison.OrdinalIgnoreCase))
                {
                    State.Player.Tavern.Hand.Add(CreateHackerfinProxy(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                    HandleCardsAddedToHand(1, source);
                }

                return;
            }

            State.Player.Tavern.Hand.Add(MinionFactory.Create(definition, BoardSide.Player, source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count, false, PoolSource.Copy, 0));
            HandleCardsAddedToHand(1, source);
        }

        private void AddMinionByCardIdToHandWithKeyword(string cardId, string source, Keyword keyword)
        {
            var before = State.Player.Tavern.Hand.Count;
            AddMinionByCardIdToHand(cardId, source);
            if (State.Player.Tavern.Hand.Count <= before)
            {
                return;
            }

            var minion = State.Player.Tavern.Hand[State.Player.Tavern.Hand.Count - 1];
            if (!minion.Keywords.Contains(keyword))
            {
                minion.Keywords.Add(keyword);
            }
        }

        private static MinionInstance CreateTitusRivendareProxy(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-titus-rivendare-" + suffix,
                DefinitionId = TitusRivendareCardId,
                CardId = TitusRivendareCardId,
                Name = "Titus Rivendare",
                Cost = 3,
                BaseAttack = 1,
                BaseHealth = 7,
                Attack = 1,
                Health = 7,
                MaxHealth = 7,
                TavernTier = 5,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.Undead },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Text = "Your Deathrattles trigger an extra time.",
                Golden = false,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                OriginPoolSource = PoolSource.Copy,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                EffectIds = new List<string> { "deathrattle_extra_trigger" },
                Tags = new List<string> { "generated_proxy", "deathrattle_support" }
            };
        }

        private static MinionInstance CreateLightfangEnforcerProxy(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-lightfang-enforcer-" + suffix,
                DefinitionId = LightfangEnforcerCardId,
                CardId = LightfangEnforcerCardId,
                Name = "Lightfang Enforcer",
                Cost = 3,
                BaseAttack = 2,
                BaseHealth = 2,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 5,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Text = "At the end of your turn, give a friendly minion of each type +2/+2.",
                Golden = false,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                OriginPoolSource = PoolSource.Copy,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                EffectIds = new List<string>(),
                Tags = new List<string> { "generated_proxy", "portrait_minion" }
            };
        }

        private static MinionInstance CreateBassgillProxy(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-bassgill-" + suffix,
                DefinitionId = BassgillCardId,
                CardId = BassgillCardId,
                Name = "Bassgill",
                Cost = 3,
                BaseAttack = 5,
                BaseHealth = 2,
                Attack = 5,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 3,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.Murloc },
                Keywords = new List<Keyword> { Keyword.Deathrattle },
                OfficialKeywords = new List<Keyword> { Keyword.Deathrattle },
                Text = "Deathrattle: Summon the highest-Health Murloc from your hand for this combat only.",
                Golden = false,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                OriginPoolSource = PoolSource.Copy,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                EffectIds = new List<string>(),
                Tags = new List<string> { "generated_proxy", "deathrattle_support" }
            };
        }

        private static MinionInstance CreateLivingAzeriteProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                LivingAzeriteCardId,
                "Living Azerite",
                "living-azerite",
                suffix,
                6,
                5,
                4,
                new List<Tribe> { Tribe.Elemental },
                new List<Keyword> { Keyword.Trigger },
                "Whenever you cast a Tavern spell, give Elementals in the Tavern +3/+2 this game.");
        }

        private static MinionInstance CreateTimewarpedGlowscaleProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                TimewarpedGlowscaleCardId,
                "Timewarped Glowscale",
                "timewarped-glowscale",
                suffix,
                6,
                12,
                5,
                new List<Tribe> { Tribe.Naga },
                new List<Keyword> { Keyword.Spellcraft, Keyword.Taunt },
                "Taunt. Spellcraft: Give a minion Divine Shield.");
        }

        private static MinionInstance CreateWearyMageProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                WearyMageCardId,
                "Weary Mage",
                "weary-mage",
                suffix,
                5,
                1,
                4,
                new List<Tribe> { Tribe.Naga },
                new List<Keyword> { Keyword.Spellcraft },
                "Spellcraft: Give a minion +2/+2. If it is a Naga, also give it Reborn until next turn.");
        }

        private static MinionInstance CreateThaumaturgistProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                ThaumaturgistCardId,
                "Thaumaturgist",
                "thaumaturgist",
                suffix,
                2,
                2,
                3,
                new List<Tribe> { Tribe.Naga },
                new List<Keyword> { Keyword.Spellcraft },
                "Spellcraft: Give a minion +1/+1 until next turn. Improved by every 4 spells you've cast this game.");
        }

        private static MinionInstance CreateArcaneBehemothProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                ArcaneBehemothCardId,
                "Arcane Behemoth",
                "arcane-behemoth",
                suffix,
                4,
                8,
                7,
                new List<Tribe> { Tribe.Elemental },
                new List<Keyword> { Keyword.Taunt },
                "Taunt. After you sell an Elemental, gain its stats.");
        }

        private static MinionInstance CreateFacelessManipulatorProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                FacelessManipulatorCardId,
                "Faceless Manipulator",
                "faceless-manipulator",
                suffix,
                3,
                3,
                1,
                new List<Tribe> { Tribe.None },
                new List<Keyword> { Keyword.Battlecry },
                "Battlecry: Choose a minion and become a copy of it.");
        }

        private static MinionInstance CreateTimewarpedPoetProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                TimewarpedPoetCardId,
                "Timewarped Poet",
                "timewarped-poet",
                suffix,
                6,
                7,
                5,
                new List<Tribe> { Tribe.Dragon },
                new List<Keyword> { Keyword.DivineShield },
                "Divine Shield. All your Dragons keep Bonus Keywords and stats gained in combat.");
        }

        private static MinionInstance CreateTimewarpedRadioStarProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                TimewarpedRadioStarCardId,
                "Timewarped Radio Star",
                "timewarped-radio-star",
                suffix,
                1,
                1,
                5,
                new List<Tribe> { Tribe.Undead },
                new List<Keyword> { Keyword.Deathrattle },
                "Deathrattle: Get a copy of the enemy minion that killed this with full Health and enchantments.");
        }

        private static MinionInstance CreateFishOfNzothProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                FishOfNzothCardId,
                "Fish of N'Zoth",
                "fish-of-nzoth",
                suffix,
                2,
                2,
                1,
                new List<Tribe> { Tribe.Beast },
                new List<Keyword> { Keyword.Trigger },
                "After a different friendly Deathrattle minion dies in combat, gain its Deathrattle.");
        }

        private static MinionInstance CreateTimewarpedLeapfroggerProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                TimewarpedLeapfroggerCardId,
                "Timewarped Leapfrogger",
                "timewarped-leapfrogger",
                suffix,
                3,
                3,
                3,
                new List<Tribe> { Tribe.Beast },
                new List<Keyword> { Keyword.Taunt, Keyword.Reborn, Keyword.Deathrattle },
                "Taunt, Reborn. Deathrattle: Give a friendly Beast +1/+1 and this Deathrattle.");
        }

        private static MinionInstance CreateTimewarpedSkipperProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                TimewarpedSkipperCardId,
                "Timewarped Skipper",
                "timewarped-skipper",
                suffix,
                5,
                6,
                3,
                new List<Tribe> { Tribe.Murloc },
                new List<Keyword> { Keyword.Trigger },
                "After you sell a Tier 2 minion, get a random Tier 1 minion.");
        }

        private static MinionInstance CreateSnarlingConductorProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                SnarlingConductorCardId,
                "Snarling Conductor",
                "snarling-conductor",
                suffix,
                4,
                5,
                4,
                new List<Tribe> { Tribe.Quilboar },
                new List<Keyword> { Keyword.Trigger },
                "At the start of your turn, discard a spell to gain 4 Gold.");
        }

        private static MinionInstance CreateGrittyHeadhunterProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                GrittyHeadhunterCardId,
                "Gritty Headhunter",
                "gritty-headhunter",
                suffix,
                5,
                4,
                5,
                new List<Tribe> { Tribe.Pirate },
                new List<Keyword> { Keyword.Battlecry },
                "Battlecry: Get a Marauder's Contract.");
        }

        private static MinionInstance CreateHackerfinProxy(string suffix)
        {
            return CreateTrinketProxyMinion(
                HackerfinCardId,
                "Hackerfin",
                "hackerfin",
                suffix,
                5,
                3,
                5,
                new List<Tribe> { Tribe.Murloc },
                new List<Keyword> { Keyword.Battlecry },
                "Battlecry: Give your other minions +1/+2. Improved by each different Bonus Keyword in your warband.");
        }

        private static MinionInstance CreateTrinketProxyMinion(
            string cardId,
            string name,
            string instancePrefix,
            string suffix,
            int attack,
            int health,
            int tavernTier,
            List<Tribe> tribes,
            List<Keyword> keywords,
            string text)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-" + instancePrefix + "-" + suffix,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = tavernTier,
                Owner = BoardSide.Player,
                Tribes = tribes,
                Keywords = new List<Keyword>(keywords),
                OfficialKeywords = new List<Keyword>(keywords),
                Text = text,
                Golden = false,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = true,
                AttacksThisCombat = 0,
                OriginPoolSource = PoolSource.Copy,
                CanReturnToPoolAfterAttach = false,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                EffectIds = new List<string>(),
                Tags = new List<string> { "generated_proxy", "portrait_minion" }
            };
        }

        private bool AddPlainCopyOfRandomFriendlyMinionToHand(string source)
        {
            var tavern = State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return false;
            }

            var candidates = State.Player.Board
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                AddRecruitLog(RecruitLogType.Play, source + ": no friendly minion to copy.", tavern.Gold, tavern.Gold);
                return false;
            }

            var rng = new SeededRng(State.Seed + State.Round * 677 + tavern.RecruitLog.Count);
            var target = rng.Pick(candidates);
            var copy = CreatePlainCopy(target, source + "-" + State.Round + "-" + tavern.Hand.Count);
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Copy;
            copy.OriginPoolSource = PoolSource.Copy;
            copy.PoolCopiesHeld = 0;
            copy.CanReturnToPoolAfterAttach = false;
            tavern.Hand.Add(copy);
            HandleCardsAddedToHand(1, source);
            AddRecruitLog(RecruitLogType.Play, source + ": copied " + target.Name + " to hand.", tavern.Gold, tavern.Gold);
            return true;
        }

        private void AddCopyOfLeftNeighborToHand(MinionInstance source, string sourceName)
        {
            if (source == null || State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var index = State.Player.Board.FindIndex(minion => minion.InstanceId == source.InstanceId);
            if (index <= 0)
            {
                return;
            }

            var copy = State.Player.Board[index - 1].Clone();
            copy.InstanceId = sourceName + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count;
            copy.Owner = BoardSide.Player;
            copy.PoolSource = PoolSource.Copy;
            copy.PoolCopiesHeld = 0;
            State.Player.Tavern.Hand.Add(copy);
            HandleCardsAddedToHand(1, sourceName);
        }

        private void AddCopiesOfShopMinionsToHand(int count, string sourceName)
        {
            var before = State.Player.Tavern.Hand.Count;
            foreach (var shopMinion in State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).Take(count).ToList())
            {
                if (State.Player.Tavern.Hand.Count >= HandLimit)
                {
                    break;
                }

                var copy = shopMinion.Clone();
                copy.InstanceId = sourceName + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count;
                copy.Owner = BoardSide.Player;
                copy.PoolSource = PoolSource.Copy;
                copy.PoolCopiesHeld = 0;
                State.Player.Tavern.Hand.Add(copy);
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, sourceName);
        }

        private void ResummonLeftUndead(MinionInstance source, string sourceName)
        {
            if (source == null)
            {
                return;
            }

            var index = State.Player.Board.FindIndex(minion => minion.InstanceId == source.InstanceId);
            if (index <= 0)
            {
                return;
            }

            var target = State.Player.Board[index - 1];
            if (!target.Tribes.Contains(Tribe.Undead))
            {
                return;
            }

            var definition = catalog.All.FirstOrDefault(minion => minion.CardId == target.CardId);
            var copy = definition != null
                ? MinionFactory.Create(definition, BoardSide.Player, sourceName + "-" + State.Round + "-" + index, false, PoolSource.Summon, 0)
                : CreatePlainCopy(target, sourceName + "-" + State.Round + "-" + index);
            State.Player.Board.RemoveAt(index - 1);
            State.Player.Board.Insert(index - 1, copy);
        }

        private static MinionInstance CreatePlainCopy(MinionInstance source, string instanceId)
        {
            var attack = source.BaseAttack > 0 ? source.BaseAttack : source.Attack;
            var health = source.BaseHealth > 0 ? source.BaseHealth : source.MaxHealth;
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = source.DefinitionId,
                CardId = source.CardId,
                Name = source.Name,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = source.TavernTier,
                Golden = source.Golden,
                Tribes = source.Tribes.ToList(),
                Keywords = source.Keywords.ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                Owner = source.Owner,
                CanAttack = source.CanAttack,
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0
            };
        }

        private void StartTavernSpellDiscover(string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 661 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableTavernSpells()
                .Where(spell => spell.TavernTier <= Math.Max(1, State.Player.Tavern.Tier))
                .ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count));
            }

            State.Player.Tavern.Discover = new DiscoverState { Source = source, RewardTier = State.Player.Tavern.Tier, Options = options };
        }

        private void AddRandomTribeMinionToHand(Tribe tribe, int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 457 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions().Where(minion => minion.InPool && MatchesTribe(minion, tribe)).ToList();
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; index += 1)
            {
                State.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, source + "-" + State.Round + "-" + index, false, PoolSource.Copy, 0));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private void AddRandomMagneticMechToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 601 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.Tribes.Contains(Tribe.Mech) && minion.Keywords.Contains(Keyword.Magnetic))
                .ToList();
            AddRandomMinionsFromCandidates(candidates, count, source, rng);
        }

        private void AddRandomChromawhelpToHand(int count, string source)
        {
            AddRandomChromadrakesToHand(count, source);
        }

        private int AddRandomChromadrakesToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 607 + State.Player.Tavern.RecruitLog.Count);
            var ids = new[] { BlueChromawhelpCardId, BlackChromawhelpCardId, GreenChromawhelpCardId, BronzeChromawhelpCardId, RedChromawhelpCardId };
            var candidates = AvailableMinions().Where(minion => ids.Contains(minion.CardId)).ToList();
            return AddRandomMinionsFromCandidates(candidates, count, source, rng);
        }

        private int AddRandomMinionsFromCandidates(List<MinionDefinition> candidates, int count, string source, SeededRng rng)
        {
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; index += 1)
            {
                State.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, source + "-" + State.Round + "-" + index, false, PoolSource.Copy, 0));
            }

            var added = State.Player.Tavern.Hand.Count - before;
            HandleCardsAddedToHand(added, source);
            return added;
        }

        private int AddRandomDistinctTierMinionsToHand(int tier, int count, string source)
        {
            var tavern = State.Player.Tavern;
            var before = tavern.Hand.Count;
            var rng = new SeededRng(State.Seed + State.Round * 4231 + tavern.RecruitLog.Count);
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier == tier && !minion.CardId.StartsWith("BGDUO", StringComparison.Ordinal))
                .GroupBy(minion => minion.CardId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            while (tavern.Hand.Count < HandLimit && candidates.Count > 0 && tavern.Hand.Count - before < count)
            {
                var index = rng.NextInt(candidates.Count);
                var picked = candidates[index];
                candidates.RemoveAt(index);
                tavern.Hand.Add(MinionFactory.Create(
                    picked,
                    BoardSide.Player,
                    source + "-" + State.Round + "-" + tavern.Hand.Count,
                    false,
                    PoolSource.Copy,
                    0));
            }

            var added = tavern.Hand.Count - before;
            HandleCardsAddedToHand(added, source);
            return added;
        }

        private void ApplyMagiciansTopHat(TrinketDefinition definition)
        {
            AddRandomDistinctTierMinionsToHand(1, 2, definition.Name);
            AddRandomDistinctTierMinionsToHand(2, 2, definition.Name);
            AddRandomDistinctTierMinionsToHand(3, 2, definition.Name);
        }

        private void ApplyCuratorSticker(TrinketDefinition definition)
        {
            var tavern = State.Player.Tavern;
            var added = 0;
            if (tavern.Hand.Count < HandLimit)
            {
                var mishmash = CreateProxyMinion(
                    MishmashBuddyCardId,
                    "Mishmash",
                    "Whenever your Amalgam gains stats, this gains them too.",
                    3,
                    4,
                    4,
                    definition.Name + "-" + State.Round + "-" + tavern.Hand.Count,
                    new[] { Tribe.All });
                MakeGoldenInPlace(mishmash);
                mishmash.Tags.Add("hero_buddy");
                tavern.Hand.Add(mishmash);
                added += 1;
            }

            if (tavern.Hand.Count < HandLimit)
            {
                tavern.Hand.Add(CreateProxyMinion(
                    CuratorAmalgamProxyCardId,
                    "Amalgam",
                    "Proxy 10/10 all-type Amalgam with Venomous.",
                    1,
                    10,
                    10,
                    definition.Name + "-" + State.Round + "-" + tavern.Hand.Count,
                    new[] { Tribe.All },
                    new[] { Keyword.Venomous }));
                added += 1;
            }

            HandleCardsAddedToHand(added, definition.Name);
        }

        private void TransformBoardIntoRandomTierMinions(int tier, string source)
        {
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier == tier && !minion.CardId.StartsWith("BGDUO", StringComparison.Ordinal))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 4241 + State.Player.Tavern.RecruitLog.Count);
            for (var index = 0; index < State.Player.Board.Count; index += 1)
            {
                var old = State.Player.Board[index];
                var picked = rng.Pick(candidates);
                var transformed = MinionFactory.Create(
                    picked,
                    BoardSide.Player,
                    source + "-" + State.Round + "-" + index,
                    old != null && old.Golden,
                    PoolSource.Copy,
                    0);
                State.Player.Board[index] = transformed;
            }

            AddRecruitLog(RecruitLogType.Play, source + ": transformed your warband into Tier " + tier + " minions.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            ResolvePlayerTriples();
        }

        private void TryTriggerSplinterOfAurum()
        {
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds != null && definition.EffectIds.Contains(SplinterOfAurumEffectId))
                {
                    TryTriggerSplinterOfAurum(definition);
                }
            }
        }

        private void TryTriggerSplinterOfAurum(TrinketDefinition definition)
        {
            if (definition == null ||
                GetAdvancedMechanicCounter(SplinterOfAurumClaimedCounter) > 0 ||
                State.Player.Tavern.Gold < 15 ||
                State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.TavernTier == 5 && !minion.CardId.StartsWith("BGDUO", StringComparison.Ordinal))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 4253 + State.Player.Tavern.RecruitLog.Count);
            var card = MinionFactory.Create(
                rng.Pick(candidates),
                BoardSide.Player,
                "splinter-of-aurum-" + State.Round + "-" + State.Player.Tavern.Hand.Count,
                true,
                PoolSource.Copy,
                0);
            if (!card.Golden)
            {
                MakeGoldenInPlace(card);
            }

            State.Player.Tavern.Hand.Add(card);
            SetAdvancedMechanicCounter(SplinterOfAurumClaimedCounter, 1);
            HandleCardsAddedToHand(1, definition.Name);
            AddRecruitLog(RecruitLogType.Play, definition.Name + ": added a Golden Tier 5 minion.", State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private void AddGeneratedElementalsToHand(int count, string source)
        {
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(CreateGeneratedElementalCard(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
        }

        private void StartTierOneDiscover(string source)
        {
            StartTierDiscover(1, source);
        }

        private void StartTierDiscover(int tier, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 467 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions().Where(minion => minion.InPool && minion.TavernTier == tier).ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Copy, 0));
            }

            State.Player.Tavern.Discover = new DiscoverState { Source = source, RewardTier = tier, Options = options };
        }

        private void StartTribeDiscover(Tribe tribe, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 587 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions().Where(minion => minion.InPool && MatchesTribe(minion, tribe)).ToList();
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + source + "-" + options.Count, false, PoolSource.Discover, 0));
            }

            State.Player.Tavern.Discover = new DiscoverState { Source = source, RewardTier = State.Player.Tavern.Tier, Options = options };
        }

        private void StartScrapperMagneticDiscover(MinionInstance source, string targetInstanceId, int picks)
        {
            var target = string.IsNullOrEmpty(targetInstanceId)
                ? null
                : State.Player.Board.FirstOrDefault(minion => minion.InstanceId == targetInstanceId && CanReceiveMagneticMech(minion));
            target = target
                ?? State.Player.Board.FirstOrDefault(minion => (source == null || minion.InstanceId != source.InstanceId) && CanReceiveMagneticMech(minion))
                ?? State.Player.Board.FirstOrDefault(CanReceiveMagneticMech);
            if (target == null)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 593 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions()
                .Where(minion => minion.InPool && minion.Tribes.Contains(Tribe.Mech))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "scrapper-magnetic-" + State.Round + "-" + options.Count, false, PoolSource.Discover, 0));
            }

            State.Player.Tavern.Discover = new DiscoverState
            {
                Source = "scrapper-magnetic",
                RewardTier = 0,
                TargetInstanceId = target.InstanceId,
                RemainingPicks = Math.Max(1, picks),
                Options = options
            };
        }

        private void ResolveScrapperMagneticChoice(DiscoverState discover, MinionInstance picked)
        {
            if (discover == null || picked == null)
            {
                return;
            }

            var target = State.Player.Board.FirstOrDefault(minion => minion.InstanceId == discover.TargetInstanceId && CanMagnetizeTo(picked, minion));
            if (target == null)
            {
                return;
            }

            AttachMagneticToTarget(picked, target, "Scrapper Magnetic");
        }

        private static bool CanReceiveMagneticMech(MinionInstance target)
        {
            return target != null && (target.Tribes.Contains(Tribe.Mech) || target.Tribes.Contains(Tribe.All));
        }

        private void AttachMagneticToTarget(MinionInstance source, MinionInstance target, string enchantmentSource)
        {
            BuffMinion(target, source.Attack, source.MaxHealth, enchantmentSource);
            foreach (var keyword in source.Keywords.Where(keyword => keyword != Keyword.Magnetic && !target.Keywords.Contains(keyword)))
            {
                target.Keywords.Add(keyword);
            }

            foreach (var tag in source.Tags.Where(tag => !target.Tags.Contains(tag)))
            {
                target.Tags.Add(tag);
            }

            DispatchTrinketMagnetized(source, target);
        }

        private void TickHandLocks()
        {
            foreach (var card in State.Player.Tavern.Hand.Where(card => card?.Counters != null && card.Counters.ContainsKey(LockedTurnsCounter)))
            {
                card.Counters[LockedTurnsCounter] = Math.Max(0, card.Counters[LockedTurnsCounter] - 1);
                if (card.Counters[LockedTurnsCounter] == 0)
                {
                    card.Counters.Remove(LockedTurnsCounter);
                    card.Tags.Remove("locked_in_hand");
                    if (card.CardId == DoomsdayDragonEggCardId && !card.Tags.Contains("doomsday_hatch_ready"))
                    {
                        card.Tags.Add("doomsday_hatch_ready");
                    }
                }
            }

            StartReadyDoomsdayDragonEggDiscover();
        }

        private void TickPatientScouts()
        {
            foreach (var scout in State.Player.Board.Concat(State.Player.Tavern.Hand).Where(card => card.CardId == PatientScoutCardId))
            {
                scout.Counters.TryGetValue(PatientScoutTierCounter, out var tier);
                scout.Counters[PatientScoutTierCounter] = Math.Min(TavernRules.MaxTavernTier, Math.Max(1, tier) + 1);
            }
        }

        private static bool IsCardLocked(MinionInstance card)
        {
            return card?.Counters != null &&
                card.Counters.TryGetValue(LockedTurnsCounter, out var turns) &&
                turns > 0;
        }

        private void InjectRefreshCards(List<MinionInstance> shop)
        {
            if (shop == null || State.Player.Tavern.DemonFodderRefreshes <= 0)
            {
                return;
            }

            shop.Add(CreateDemonFodderCard("refresh-" + State.Round + "-" + State.Player.Tavern.DemonFodderRefreshes));
            State.Player.Tavern.DemonFodderRefreshes -= 1;
        }

        private void BuffAllMinions(IEnumerable<MinionInstance> minions, int attack, int health, string sourceId)
        {
            foreach (var minion in minions.Where(minion => minion != null && minion.CardKind == CardKind.Minion))
            {
                BuffMinion(minion, attack, health, sourceId);
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
                Name = "鲜血宝石",
                Cost = 0,
                BaseAttack = 0,
                BaseHealth = 0,
                Attack = 0,
                Health = 0,
                MaxHealth = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.BloodGem },
                Text = "使一个友方随从获得+1/+1。",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Tags = new List<string> { "generated_spell", "blood_gem", "targeted_spell", "buff_spell" }
            };
        }

        private static MinionInstance CreateFearlessFoodieChoice(string cardId, string name, string text, int amount)
        {
            var counters = new Dictionary<string, int>();
            if (cardId == FearlessFoodieGrowthOptionCardId)
            {
                counters["foodie_multiplier"] = amount;
            }
            else
            {
                counters["foodie_gems"] = amount;
            }

            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-" + cardId.ToLowerInvariant(),
                DefinitionId = cardId.ToLowerInvariant(),
                CardId = cardId,
                Name = name,
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = text,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = counters,
                Tags = new List<string> { "choose_one", "fearless_foodie" }
            };
        }

        private static MinionInstance CreateSprightlyScarabChoice(string cardId, string name, string text, int multiplier)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-" + cardId.ToLowerInvariant(),
                DefinitionId = cardId.ToLowerInvariant(),
                CardId = cardId,
                Name = name,
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = text,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = new Dictionary<string, int> { { "sprightly_scarab_multiplier", Math.Max(1, multiplier) } },
                Tags = new List<string> { "choose_one", "sprightly_scarab" }
            };
        }

        private static MinionInstance CreateSlimyShieldCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-slimy-shield-" + suffix,
                DefinitionId = "slimy-shield",
                CardId = SlimyShieldCardId,
                Name = "黏黏盾",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = "使一个随从获得+1/+1和嘲讽。",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Tags = new List<string> { "generated_spell", "targeted_spell", "buff_spell", "taunt_grant" }
            };
        }

        private static MinionInstance CreateBristlebackBloodGemCard(string suffix)
        {
            var card = CreateBloodGemCard("bristleback-" + suffix);
            card.CardId = BristlebackBloodGemCardId;
            card.DefinitionId = "bristleback-blood-gem";
            card.InstanceId = "player-bristleback-blood-gem-" + suffix;
            card.Tags.Add("quilboar_taunt_grant");
            card.Text = "Give a friendly minion +1/+1. If it is a Quilboar, also give it Taunt.";
            return card;
        }

        private static MinionInstance CreateRebornBloodGemCard(string suffix)
        {
            var card = CreateBloodGemCard("reborn-" + suffix);
            card.CardId = RebornBloodGemCardId;
            card.DefinitionId = "reborn-blood-gem";
            card.InstanceId = "player-reborn-blood-gem-" + suffix;
            card.Tags.Add("quilboar_reborn_grant");
            card.Text = "Give a friendly minion Blood Gem stats. If it is a Quilboar, also give it Reborn.";
            return card;
        }

        private static MinionInstance CreatePointyArrowCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-pointy-arrow-" + suffix,
                DefinitionId = "pointy-arrow",
                CardId = PointyArrowCardId,
                Name = "Pointy Arrow",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = "Give a minion +4 Attack.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Tags = new List<string> { "generated_spell", "targeted_spell", "buff_spell", "attack_buff_spell" }
            };
        }

        private static MinionInstance CreateReefRifferSpellCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-reef-riffer-spell-" + suffix,
                DefinitionId = "reef-riffer-spell",
                CardId = ReefRifferSpellCardId,
                Name = "Reef Riff",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft: Give a minion stats equal to your Tier until next turn.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = new Dictionary<string, int> { { "spellcraft_multiplier", 1 } },
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "buff_spell" }
            };
        }

        private static MinionInstance CreateSurfNSurfSpellCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-surf-n-surf-spell-" + suffix,
                DefinitionId = "surf-n-surf-spell",
                CardId = SurfNSurfSpellCardId,
                Name = "Surf n' Surf",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft: Give a minion Deathrattle: Summon a 3/2 Crab until next turn.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = new Dictionary<string, int> { { "crab_attack", 3 }, { "crab_health", 2 } },
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "deathrattle_grant" }
            };
        }

        private static MinionInstance CreateDeepSeaAnglerSpellCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-deep-sea-angler-spell-" + suffix,
                DefinitionId = "deep-sea-angler-spell",
                CardId = DeepSeaAnglerSpellCardId,
                Name = "Deep Sea Angling",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft: Give a minion +2/+6 and Taunt until next turn.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = new Dictionary<string, int> { { "angler_attack", 2 }, { "angler_health", 6 } },
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "buff_spell", "taunt_grant" }
            };
        }

        private static MinionInstance CreateDeepBlueSpellCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-deep-blue-spell-" + suffix,
                DefinitionId = "deep-blue-spell",
                CardId = DeepBlueSpellCardId,
                Name = "Deep Blue",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft: Give a minion scaling stats until next turn.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = new Dictionary<string, int> { { "deep_blue_attack", 2 }, { "deep_blue_health", 2 }, { "deep_blue_growth", 1 } },
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "buff_spell" }
            };
        }

        private static MinionInstance CreateTimewarpedGlowscaleSpellCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-timewarped-glowscale-spell-" + suffix,
                DefinitionId = "timewarped-glowscale-spell",
                CardId = TimewarpedGlowscaleSpellCardId,
                Name = "Timewarped Glowscale",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft: Give a minion Divine Shield.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "divine_shield_spell" }
            };
        }

        private static MinionInstance CreateWearyMageSpellCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-weary-mage-spell-" + suffix,
                DefinitionId = "weary-mage-spell",
                CardId = WearyMageSpellCardId,
                Name = "Weary Mage",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft: Give a minion +2/+2. If it is a Naga, also give it Reborn until next turn.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "buff_spell", "weary_mage_spell" }
            };
        }

        private static MinionInstance CreateThaumaturgistSpellCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-thaumaturgist-spell-" + suffix,
                DefinitionId = "thaumaturgist-spell",
                CardId = ThaumaturgistSpellCardId,
                Name = "Thaumaturgist",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft: Give a minion scaling stats until next turn.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = new Dictionary<string, int> { { "spellcraft_amount", 1 } },
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "buff_spell", "thaumaturgist_spell" }
            };
        }

        private static MinionInstance CreateVolcanicVisitorSpellCard(string suffix, bool attackChoice)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-volcanic-visitor-" + (attackChoice ? "attack-" : "health-") + suffix,
                DefinitionId = "volcanic-visitor-" + (attackChoice ? "attack" : "health") + "-spell",
                CardId = attackChoice ? VolcanicVisitorAttackSpellCardId : VolcanicVisitorHealthSpellCardId,
                Name = attackChoice ? "Volcanic Visitor Attack" : "Volcanic Visitor Health",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = attackChoice ? "Spellcraft: Give a minion +4 Attack until next turn." : "Spellcraft: Give a minion +4 Health until next turn.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = new Dictionary<string, int> { { "spellcraft_amount", 4 } },
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "targeted_spell", "buff_spell", attackChoice ? "attack_buff_spell" : "health_buff_spell" }
            };
        }

        private static MinionInstance CreateFrostlingPriestessSpellCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-frostling-priestess-spell-" + suffix,
                DefinitionId = "frostling-priestess-spell",
                CardId = FrostlingPriestessSpellCardId,
                Name = "Frostling Priestess",
                Cost = 0,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Spellcraft },
                Text = "Spellcraft: Get a random Tavern spell that gives stats.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Counters = new Dictionary<string, int> { { "spellcraft_multiplier", 1 } },
                Tags = new List<string> { "generated_spell", "spellcraft", "temporary_spellcraft_card", "generated_tavern_spell", "stat_tavern_spell" }
            };
        }

        private static MinionInstance CreateDeepwaterSchoolCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-deepwater-school-" + suffix,
                DefinitionId = "deepwater-school",
                CardId = DeepwaterSchoolCardId,
                Name = "Deepwater Clan",
                Cost = 2,
                TavernTier = 4,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = "Give a minion +2/+2. Give your Murlocs +2/+2.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Tags = new List<string> { "generated_spell", "generated_tavern_spell", "deepwater_clan", "murloc_buff", "buff_spell" }
            };
        }

        private static MinionInstance CreateArcaneConsumptionCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-arcane-consumption-" + suffix,
                DefinitionId = "arcane-consumption",
                CardId = ArcaneConsumptionCardId,
                Name = "Arcane Absorption",
                Cost = 1,
                TavernTier = 4,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = "Give a friendly Elemental half the stats of the highest-Health minion in the Tavern.",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Tags = new List<string> { "generated_spell", "generated_tavern_spell", "arcane_absorption", "elemental_buff", "buff_spell" }
            };
        }

        private static readonly string[] BountyCardIds =
        {
            OfficialHealthyBountyCardId,
            OfficialHostileBountyCardId,
            OfficialSelfishBountyCardId,
            OfficialFriendlyBountyCardId,
            OfficialWealthyBountyCardId
        };

        private static readonly string[] LegacyBountyCardIds =
        {
            HealthyBountyCardId,
            HostileBountyCardId,
            SelfishBountyCardId,
            FriendlyBountyCardId,
            WealthyBountyCardId
        };

        private static bool IsBountyCardId(string cardId)
        {
            return BountyCardIds.Contains(cardId) || LegacyBountyCardIds.Contains(cardId);
        }

        private static MinionInstance CreateBountyCard(string cardId, string suffix)
        {
            var name = "Bounty";
            var text = "Generated Tier 3 Tavern Spell.";
            var tags = new List<string> { "generated_spell", "generated_tavern_spell", "bounty" };
            switch (cardId)
            {
                case HealthyBountyCardId:
                case OfficialHealthyBountyCardId:
                    name = "Healthy Bounty";
                    text = "Give four friendly minions +4 Health.";
                    tags.Add("board_health_buff");
                    tags.Add("buff_spell");
                    break;
                case HostileBountyCardId:
                case OfficialHostileBountyCardId:
                    name = "Hostile Bounty";
                    text = "Give four friendly minions +4 Attack.";
                    tags.Add("board_attack_buff");
                    tags.Add("buff_spell");
                    break;
                case SelfishBountyCardId:
                case OfficialSelfishBountyCardId:
                    name = "Selfish Bounty";
                    text = "Give a friendly minion +6/+6.";
                    tags.Add("targeted_spell");
                    tags.Add("buff_spell");
                    break;
                case FriendlyBountyCardId:
                case OfficialFriendlyBountyCardId:
                    name = "Friendly Bounty";
                    text = "Get a random minion of your most common type.";
                    tags.Add("tribe_discover");
                    break;
                case WealthyBountyCardId:
                case OfficialWealthyBountyCardId:
                    name = "Wealthy Bounty";
                    text = "Gain 2 Gold.";
                    tags.Add("economy_spell");
                    break;
            }

            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-bounty-" + cardId.ToLowerInvariant() + "-" + suffix,
                DefinitionId = "bounty-" + cardId.ToLowerInvariant(),
                CardId = cardId,
                Name = name,
                Cost = 2,
                TavernTier = 3,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.TavernSpell },
                Text = text,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = tags
            };
        }

        private static MinionInstance CreateGeneratedElementalCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-generated-elemental-" + suffix,
                DefinitionId = "generated-elemental",
                CardId = "GENERATED_ELEMENTAL",
                Name = "商贩元素",
                Cost = 3,
                BaseAttack = 3,
                BaseHealth = 3,
                Attack = 3,
                Health = 3,
                MaxHealth = 3,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.Elemental },
                Keywords = new List<Keyword>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_minion", "elemental" }
            };
        }

        private static MinionInstance CreateDemonFodderCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "player-demon-fodder-" + suffix,
                DefinitionId = "demon-fodder",
                CardId = DemonFodderCardId,
                Name = "恶魔饲料",
                Cost = 3,
                BaseAttack = 1,
                BaseHealth = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 1,
                Tribes = new List<Tribe> { Tribe.Demon },
                Keywords = new List<Keyword>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0,
                Tags = new List<string> { "generated_minion", "demon_fodder", "demon" }
            };
        }

        private void BuffMinion(MinionInstance target, int attack, int health, string sourceId)
        {
            if (target == null)
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
            RefreshScarletSurvivor(target);
            ResolveHighTierBuffTriggers(target, attack, health);
            ApplyFountainPenExtraStats(target, attack, health, sourceId);
        }

        private void ApplyFountainPenExtraStats(MinionInstance target, int attack, int health, string sourceId)
        {
            var bonus = GetFountainPenExtraStats(sourceId, attack, health);
            if (bonus.Attack == 0 && bonus.Health == 0)
            {
                return;
            }

            BuffMinion(target, bonus.Attack, bonus.Health, FountainPenSourceId);
        }

        private (int Attack, int Health) GetFountainPenExtraStats(string sourceId, int attack, int health)
        {
            if ((attack == 0 && health == 0) ||
                string.IsNullOrWhiteSpace(sourceId) ||
                string.Equals(sourceId, FountainPenSourceId, StringComparison.Ordinal))
            {
                return (0, 0);
            }

            if (!IsFriendlyElementalStatGrantSource(sourceId))
            {
                return (0, 0);
            }

            var extraAttack = 0;
            var extraHealth = 0;
            foreach (var definition in EquippedTrinketDefinitions())
            {
                if (definition.EffectIds == null || !definition.EffectIds.Contains(FountainPenEffectId))
                {
                    continue;
                }

                extraAttack = StatMath.SaturatingAdd(extraAttack, definition.SlotKind == TrinketSlotKind.Greater ? 4 : 2, 0, StatMath.MaxStat);
                extraHealth = StatMath.SaturatingAdd(extraHealth, definition.SlotKind == TrinketSlotKind.Greater ? 2 : 1, 0, StatMath.MaxStat);
            }

            return (extraAttack, extraHealth);
        }

        private bool IsFriendlyElementalStatGrantSource(string sourceId)
        {
            return State.Player.Board.Any(minion =>
                minion != null &&
                HasTribe(minion, Tribe.Elemental) &&
                IsElementalStatGrantSourceMatch(minion, sourceId));
        }

        private static bool IsElementalStatGrantSourceMatch(MinionInstance source, string sourceId)
        {
            if (source == null || string.IsNullOrWhiteSpace(sourceId))
            {
                return false;
            }

            if (string.Equals(source.CardId, sourceId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.Name, sourceId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            switch (sourceId)
            {
                case "Nomi":
                    return string.Equals(source.CardId, NomiCardId, StringComparison.OrdinalIgnoreCase);
                case "Living Azerite":
                    return string.Equals(source.CardId, LivingAzeriteCardId, StringComparison.OrdinalIgnoreCase);
                case "Fel Elemental":
                    return string.Equals(source.CardId, FelElementalCardId, StringComparison.OrdinalIgnoreCase);
                case "Dusty Cyclone":
                    return string.Equals(source.CardId, DustyCycloneCardId, StringComparison.OrdinalIgnoreCase);
                case "Wildfire Manasurge":
                    return string.Equals(source.CardId, WildfireManasurgeCardId, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        private void ResolveHighTierBuffTriggers(MinionInstance target, int attack, int health)
        {
            if (target == null || !State.Player.Board.Any(minion => minion.InstanceId == target.InstanceId))
            {
                return;
            }

            if (attack > 0 && target.Tribes.Contains(Tribe.Pirate))
            {
                foreach (var spacefarer in State.Player.Board.Where(minion => minion.CardId == SpacefarerCardId && minion.InstanceId != target.InstanceId).ToList())
                {
                    BuffMinion(spacefarer, 0, spacefarer.Golden ? 4 : 2, "Spacefarer");
                }
            }

            if (health <= 0 || !target.Tribes.Contains(Tribe.Naga))
            {
                return;
            }

            foreach (var slitherspear in State.Player.Board.Where(minion => minion.CardId == SlitherspearCardId && minion.InstanceId != target.InstanceId).ToList())
            {
                BuffMinion(target, StatMath.SaturatingMultiply(health, slitherspear.Golden ? 2 : 1, 0, StatMath.MaxStat), 0, "Slitherspear");
            }
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
            var attackDelta = StatMath.SaturatingDelta(attack, currentAttack);
            var healthDelta = StatMath.SaturatingDelta(health, currentHealth);
            StatMath.ApplyStatDelta(target, attackDelta, healthDelta);

            if (existing == null)
            {
                target.Enchantments.Add(new Enchantment
                {
                    Id = sourceId,
                    SourceId = sourceId,
                    AttackBonus = attack,
                    HealthBonus = health
                });
                RefreshScarletSurvivor(target);
                return;
            }

            existing.AttackBonus = attack;
            existing.HealthBonus = health;
            RefreshScarletSurvivor(target);
        }

        private static void RemoveTrackedBuff(MinionInstance target, string sourceId)
        {
            if (target?.Enchantments == null)
            {
                return;
            }

            var existing = target.Enchantments
                .Where(enchantment => enchantment.SourceId == sourceId)
                .ToList();
            foreach (var enchantment in existing)
            {
                StatMath.ApplyStatDeltaPreservingDamage(
                    target,
                    StatMath.SaturatingSubtract(0, enchantment.AttackBonus),
                    StatMath.SaturatingSubtract(0, enchantment.HealthBonus));
                target.Enchantments.Remove(enchantment);
            }

            RefreshScarletSurvivor(target);
        }

        private static void MakeGoldenInPlace(MinionInstance target)
        {
            if (target == null || target.Golden)
            {
                return;
            }

            target.Golden = true;
            StatMath.DoubleCurrentStats(target, false);
            MarkTripleRewardGranted(target);
            RefreshScarletSurvivor(target);
        }

        private static void RefreshScarletSurvivor(MinionInstance target)
        {
            if (target != null && target.CardId == ScarletSurvivorCardId && target.Attack >= 6 && !target.Keywords.Contains(Keyword.DivineShield))
            {
                target.Keywords.Add(Keyword.DivineShield);
            }
        }

        private void ResolvePlayerTriples()
        {
            var all = State.Player.Tavern.Hand.Concat(State.Player.Board).ToList();
            var candidate = FindPlayerTripleCandidate(all, out var requiredCopies);
            if (string.IsNullOrEmpty(candidate) && TryResolveSurpriseElementalTriple(all))
            {
                return;
            }

            if (string.IsNullOrEmpty(candidate))
            {
                return;
            }

            var result = TripleEngine.ResolveTriple(all, candidate, BoardSide.Player, State.Round + "-" + State.Player.Tavern.RecruitLog.Count, requiredCopies);
            State.Player.Tavern.Hand = result.Remaining.Where(minion => State.Player.Tavern.Hand.Any(hand => hand.InstanceId == minion.InstanceId)).ToList();
            State.Player.Board = result.Remaining.Where(minion => State.Player.Board.Any(board => board.InstanceId == minion.InstanceId)).ToList();

            if (State.Player.Tavern.Hand.Count < HandLimit)
            {
                State.Player.Tavern.Hand.Add(result.Golden);
            }
            else if (State.Player.Board.Count < BoardLimit)
            {
                State.Player.Board.Add(result.Golden);
            }

            AddRecruitLog(RecruitLogType.Triple, "三连合成 " + result.Golden.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
        }

        private string FindPlayerTripleCandidate(List<MinionInstance> all, out int requiredCopies)
        {
            requiredCopies = 3;
            if (all == null || all.Count == 0)
            {
                return null;
            }

            var pilferedLampsActive = HasActiveQuestReward(PilferedLampsRewardId);
            var designerEyepatchActive = HasEquippedTrinketEffect(DesignerEyepatchEffectId);
            foreach (var group in all
                         .Where(item => item != null && !item.Golden && !string.IsNullOrEmpty(item.DefinitionId))
                         .GroupBy(item => item.DefinitionId))
            {
                var groupRequiredCopies = GetPlayerTripleRequiredCopies(
                    group,
                    pilferedLampsActive,
                    designerEyepatchActive);
                if (group.Count() < groupRequiredCopies)
                {
                    continue;
                }

                requiredCopies = groupRequiredCopies;
                return group.Key;
            }

            return null;
        }

        private static int GetPlayerTripleRequiredCopies(
            IEnumerable<MinionInstance> group,
            bool pilferedLampsActive,
            bool designerEyepatchActive)
        {
            if (pilferedLampsActive)
            {
                return 2;
            }

            if (designerEyepatchActive && group.Any(IsPirateTripleCandidate))
            {
                return 2;
            }

            return 3;
        }

        private static bool IsPirateTripleCandidate(MinionInstance minion)
        {
            return BoardTribeAnalyzer.GetCountedTribes(minion).Contains(Tribe.Pirate);
        }

        private bool TryResolveSurpriseElementalTriple(List<MinionInstance> all)
        {
            var surprise = all.Where(card => !card.Golden && card.CardId == SurpriseElementalCardId).ToList();
            if (surprise.Count == 0)
            {
                return false;
            }

            var elementalGroup = all
                .Where(card => !card.Golden && card.CardId != SurpriseElementalCardId && card.Tribes.Contains(Tribe.Elemental))
                .GroupBy(card => card.DefinitionId)
                .FirstOrDefault(group => group.Count() + surprise.Count >= 3);
            if (elementalGroup == null)
            {
                return false;
            }

            var materials = elementalGroup.Take(3).Concat(surprise).Take(3).ToList();
            if (materials.Count < 3)
            {
                return false;
            }

            var baseItem = elementalGroup.First();
            var remaining = all.Where(card => !materials.Any(material => material.InstanceId == card.InstanceId)).ToList();
            var golden = baseItem.Clone();
            golden.InstanceId = "player-" + baseItem.DefinitionId + "-golden-surprise-" + State.Round + "-" + State.Player.Tavern.RecruitLog.Count;
            golden.Owner = BoardSide.Player;
            golden.Golden = true;
            golden.Attack = StatMath.SaturatingMultiply(baseItem.Attack, 2, 0, StatMath.MaxStat);
            golden.Health = StatMath.SaturatingMultiply(baseItem.Health, 2, int.MinValue, StatMath.MaxStat);
            golden.MaxHealth = StatMath.SaturatingMultiply(baseItem.MaxHealth, 2, 1, StatMath.MaxStat);
            StatMath.ClampCurrentHealthToMax(golden);
            golden.PoolSource = materials.Sum(item => item.PoolCopiesHeld) > 0 ? PoolSource.Pool : PoolSource.Copy;
            golden.PoolCopiesHeld = materials.Sum(item => item.PoolCopiesHeld);

            State.Player.Tavern.Hand = remaining.Where(minion => State.Player.Tavern.Hand.Any(hand => hand.InstanceId == minion.InstanceId)).ToList();
            State.Player.Board = remaining.Where(minion => State.Player.Board.Any(board => board.InstanceId == minion.InstanceId)).ToList();
            if (State.Player.Tavern.Hand.Count < HandLimit)
            {
                State.Player.Tavern.Hand.Add(golden);
            }
            else if (State.Player.Board.Count < BoardLimit)
            {
                State.Player.Board.Add(golden);
            }

            AddRecruitLog(RecruitLogType.Triple, "Surprise Elemental triple " + golden.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            return true;
        }

        private void GrantTripleRewardCard()
        {
            var tavern = State.Player.Tavern;
            if (tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            tavern.Hand.Add(CreateTripleRewardCard(State.Round + "-" + tavern.RecruitLog.Count));
            AddRecruitLog(RecruitLogType.Triple, "Triple reward card", tavern.Gold, tavern.Gold);
        }

        private static bool IsTripleRewardCard(MinionInstance minion)
        {
            return minion != null && minion.DefinitionId == TripleRewardDefinitionId;
        }

        private static bool HasGrantedTripleReward(MinionInstance minion)
        {
            return minion.Counters != null &&
                minion.Counters.TryGetValue(TripleRewardGrantedCounter, out var granted) &&
                granted > 0;
        }

        private static void MarkTripleRewardGranted(MinionInstance minion)
        {
            if (minion.Counters == null)
            {
                minion.Counters = new Dictionary<string, int>();
            }

            minion.Counters[TripleRewardGrantedCounter] = 1;
        }

        private static MinionInstance CreateTripleRewardCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = "player-" + TripleRewardDefinitionId + "-" + suffix,
                DefinitionId = TripleRewardDefinitionId,
                CardId = TripleRewardCardId,
                Name = "Triple Reward",
                Cost = 0,
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword> { Keyword.Discover, Keyword.TavernSpell },
                Text = "Play: Discover a minion from one tavern tier higher, up to tier 7.",
                Golden = false,
                Owner = BoardSide.Player,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                CanAttack = false,
                AttacksThisCombat = 0,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0
            };
        }

        private DiscoverState CreateTripleDiscover()
        {
            var rewardTier = Math.Min(TavernRules.MaxTavernTier, State.Player.Tavern.Tier + 1);
            var candidates = AvailableMinions().Where(definition => definition.InPool && definition.TavernTier == rewardTier).ToList();
            if (candidates.Count < 3)
            {
                candidates = AvailableMinions().Where(definition => definition.InPool && definition.TavernTier <= rewardTier).ToList();
            }

            var rng = new SeededRng(State.Seed + State.Round * 7919 + State.Player.Tavern.RecruitLog.Count);
            var options = new List<MinionInstance>();
            while (options.Count < 3 && candidates.Count > 0)
            {
                var index = rng.NextInt(candidates.Count);
                var definition = candidates[index];
                candidates.RemoveAt(index);
                options.Add(MinionFactory.Create(definition, BoardSide.Player, "discover-" + options.Count, false, PoolSource.Discover, 0));
            }

            return new DiscoverState { RewardTier = rewardTier, Options = options };
        }

        private ShopState CreateShopFromPool(
            IDictionary<string, int> snapshot,
            int tier,
            int size,
            int seed,
            string suffix,
            bool includeTavernSpell = true,
            int minimumTier = TavernRules.MinTavernTier)
        {
            var pool = new MinionPool(catalog.All, snapshot, CurrentActiveTribes(), cardPoolAvailability.AllowsMinion);
            var rng = new SeededRng(seed);
            var spell = includeTavernSpell ? DrawTavernSpell(tier, rng, minimumTier) : null;
            var definitions = pool.DrawShop(tier, size, rng, minimumTier);
            var shop = definitions
                .Select((definition, index) => MinionFactory.Create(definition, BoardSide.Player, suffix + "-" + index, false, PoolSource.Pool, 1))
                .ToList();

            if (spell != null)
            {
                shop.Add(MinionFactory.Create(spell, BoardSide.Player, suffix + "-spell"));
            }

            return new ShopState
            {
                Shop = shop,
                Pool = pool.Snapshot()
            };
        }

        private TavernSpellDefinition DrawTavernSpell(int tier, SeededRng rng, int minimumTier = TavernRules.MinTavernTier)
        {
            var minTier = Math.Max(TavernRules.MinTavernTier, minimumTier);
            var candidates = AvailableTavernSpells()
                .Where(spell => spell.TavernTier >= minTier && spell.TavernTier <= tier)
                .ToList();
            return candidates.Count == 0 ? null : rng.Pick(candidates);
        }

        private static void ApplyShopGrowth(List<MinionInstance> shop, List<TavernGrowthModifier> modifiers)
        {
            if (shop == null || modifiers == null || modifiers.Count == 0)
            {
                return;
            }

            foreach (var minion in shop)
            {
                if (minion == null || minion.CardKind != CardKind.Minion)
                {
                    continue;
                }

                foreach (var modifier in modifiers)
                {
                    if (modifier.Scope != BuffScope.ShopGlobal ||
                        !MatchesTribe(minion, modifier.Tribe) ||
                        (modifier.TierCap > 0 && minion.TavernTier > modifier.TierCap))
                    {
                        continue;
                    }

                    MechanicEngine.ApplyToMinion(minion, new MechanicAction
                    {
                        Type = MechanicActionType.BuffStats,
                        Attack = modifier.Attack,
                        Health = modifier.Health,
                        SourceId = modifier.SourceId
                    });
                }
            }
        }

        private static bool MatchesTribe(MinionInstance minion, Tribe tribe)
        {
            return tribe == Tribe.All ||
                minion.Tribes.Contains(tribe) ||
                minion.Tribes.Contains(Tribe.All);
        }

        private static bool MatchesTribe(MinionDefinition minion, Tribe tribe)
        {
            return tribe == Tribe.All ||
                minion.Tribes.Contains(tribe) ||
                minion.Tribes.Contains(Tribe.All);
        }

        private Dictionary<string, int> ReleaseShopToPool(bool releaseFrozenSlots = true)
        {
            var tavern = State.Player.Tavern;
            TavernShopSlots.Ensure(tavern);
            var pool = new MinionPool(catalog.All, State.Player.Tavern.Pool, CurrentActiveTribes(), cardPoolAvailability.AllowsMinion);
            for (var index = 0; index < tavern.Shop.Count; index += 1)
            {
                if (!releaseFrozenSlots && TavernShopSlots.IsSlotFrozen(tavern, index))
                {
                    continue;
                }

                var minion = tavern.Shop[index];
                if (minion != null && minion.PoolCopiesHeld > 0)
                {
                    pool.Release(minion.DefinitionId, minion.PoolCopiesHeld);
                }
            }

            return pool.Snapshot();
        }

        private void ReleaseMinionToPool(MinionInstance minion)
        {
            var pool = new MinionPool(catalog.All, State.Player.Tavern.Pool, CurrentActiveTribes(), cardPoolAvailability.AllowsMinion);
            if (minion.PoolCopiesHeld > 0)
            {
                pool.Release(minion.DefinitionId, minion.PoolCopiesHeld);
            }

            State.Player.Tavern.Pool = pool.Snapshot();
        }

        private void AddRecruitLog(RecruitLogType type, string message, int goldBefore, int goldAfter)
        {
            AddRecruitLog(State, type, message, goldBefore, goldAfter);
        }

        private static void AddRecruitLog(MatchState state, RecruitLogType type, string message, int goldBefore, int goldAfter)
        {
            state.Player.Tavern.RecruitLog.Add(new RecruitLogEntry
            {
                Seq = state.Player.Tavern.RecruitLog.Count + 1,
                Round = state.Round,
                Type = type,
                Message = message,
                GoldBefore = goldBefore,
                GoldAfter = goldAfter
            });
        }

        private sealed class ShopState
        {
            public List<MinionInstance> Shop;
            public Dictionary<string, int> Pool;
        }
    }
}
