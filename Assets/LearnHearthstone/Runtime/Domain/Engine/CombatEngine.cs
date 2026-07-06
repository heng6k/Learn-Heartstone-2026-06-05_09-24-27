using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class CombatEngine
    {
        private const int BoardLimit = 7;
        private const string CordPullerCardId = "BG29_611";
        private const string HarmlessBoneheadCardId = "BG28_300";
        private const string RotHideGnollCardId = "BG25_013";
        private const string ManasaberCardId = "BG26_800";
        private const string BuzzingVerminCardId = "BG31_803";
        private const string TwilightHatchlingCardId = "BG34_630";
        private const string ForestRoverCardId = "BG31_801";
        private const string GlowgulletWarlordCardId = "BG32_430";
        private const string ScarletSkullCardId = "BG25_022";
        private const string HummingBirdCardId = "BG26_805";
        private const string AlertAlarmistCardId = "BG35_340";
        private const string BristlebackBullyCardId = "BG35_432";
        private const string MetallicHunterCardId = "BG32_170";
        private const string TideRaiserCardId = "BG34_920";
        private const string HandlessForsakenCardId = "BG25_010";
        private const string BoneWatcherCardId = "BG30_125";
        private const string SlyRaptorCardId = "BG25_806";
        private const string AmberGuardianCardId = "BG24_500";
        private const string BristlebackScrapSmithCardId = "BG24_707";
        private const string GemDayPiperCardId = "BG26_160";
        private const string TreasureSeekerCardId = "BG26_360";
        private const string MummifierCardId = "BG28_309";
        private const string UtilityDroneCardId = "BG31_859";
        private const string BuriedBristlebackCardId = "BG32_434";
        private const string RadiantEmberCardId = "BG32_842";
        private const string DustboneDestroyerCardId = "BG33_323";
        private const string ColdlightDiverCardId = "BG33_894";
        private const string ToughOrcaCardId = "BG34_312";
        private const string JuvenileWaveCardId = "BG34_856";
        private const string RoaringRecruiterCardId = "BG29_816";
        private const string DeflectOBotCardId = "BGS_071";
        private const string WildfireElementalCardId = "BGS_126";
        private const string SleepySupporterCardId = "BG33_241";
        private const string ExpertAviatorCardId = "BG34_140";
        private const string EternalKnightCardId = "BG25_008";
        private const string AncestralAutomatonCardId = "BG_TTN_401";
        private const string VeryHungryWinterfinnerCardId = "BG29_300";
        private const string BloodGemCardId = "BLOOD_GEM";
        private const string BristlebackBloodGemCardId = "BRISTLEBACK_BLOOD_GEM";
        private const string PointyArrowCardId = "100596";
        private const string BloodGemBarrageCardNumber = "126676";
        private const string PersistentPoetCardId = "BG29_813";
        private const string PersistentPoetSourceId = "Persistent Poet";
        private const string PrizedPromoDrakeCardId = "BG21_014";
        private const string HungrySnapjawCardId = "BG27_556";
        private const string SindoreiStraightShotCardId = "BG25_016";
        private const string BladeCollectorCardId = "BG26_817";
        private const string MonstrousMacawCardId = "BGS_078";
        private const string TopperTheThiefCardId = "BG33_822";
        private const string ValiantRebelCardId = "BG34_604";
        private const string RecruiterOfTheDeepCardId = "BG34_925";
        private const string BananaSlammaCardId = "BG26_802";
        private const string BassgillCardId = "BG26_350";
        private const string TrigoreTheLasherCardId = "BG29_807";
        private const string DevoutSatyressCardId = "BG33_155";
        private const string SilentSwarmguardCardId = "BG33_156";
        private const string TunnelBlasterCardId = "BG_DAL_775";
        private const string HeavyMetalWyrmCardId = "BG26_801";
        private const string TwilightNestmatronCardId = "BG34_731";
        private const string AutoAssemblerCardId = "BG32_172";
        private const string PlaguedGhoulCardId = "BG34_690";
        private const string DeepwaterChieftainCardId = "BG35_143";
        private const string ManasparkCardId = "BG35_881";
        private const string RefreshingAnomalyCardId = "BGS_116";
        private const string TavernTempestCardId = "BGS_123";
        private const string KingBagurgleCardId = "BGS_030";
        private const string PricklyPiperCardId = "BG26_525";
        private const string BalladistCardId = "BG26_814";
        private const string FeedingTigerSharkCardId = "BG34_523";
        private const string ScrapperCardId = "BG29_503";
        private const string BrannosaurCardId = "BG34_865";
        private const string SaloonDancerCardId = "BG35_702";
        private const string DustyCycloneCardId = "BG32_841";
        private const string FriendlyFelboarCardId = "BG32_880";
        private const string MobileProjectionistCardId = "BG31_175";
        private const string HatchingResearcherCardId = "BG34_632";
        private const string DreamingThornweaverCardId = "BG32_433";
        private const string RedChromawhelpCardId = "BG34_638t";
        private const string WyvernFrontmanCardId = "BG35_601";
        private const string KangorsApprenticeCardId = "BGS_012";
        private const string TitusRivendareCardId = "BG25_354";
        private const string LeeroyTheRecklessCardId = "BG23_318";
        private const string BarrensConjurerCardId = "BG29_862";
        private const string WintergraspGhoulCardId = "BG34_694";
        private const string CatacombCrasherCardId = "BG30_129";
        private const string DrustfallenButcherCardId = "BG32_324";
        private const string EternalSummonerCardId = "BG34_403";
        private const string BrannsEpicEggCardId = "TB_BaconShop_HERO_43_Buddy";
        private const string ScreamingBansheeCardId = "BG35_334";
        private const string FarmhandWhirlOMatronCardId = "BG26_162";
        private const string NightmareParlorGuestCardId = "BG32_111";
        private const string ShadowdancerCardId = "BG32_891";
        private const string ScrapScraperCardId = "BG26_148";
        private const string ClunkerJunkerCardId = "BG35_890";
        private const string HolyMecherelCardId = "BG33_809";
        private const string ShipwreckedCaptainCardId = "BG33_821";
        private const string SewerRatPackCardId = "BG35_604";
        private const string BristlebachCardId = "BG29_808";
        private const string MoonRiderCardId = "BG35_602";
        private const string SkyfinRaptorCardId = "BG29_806";
        private const string TurquoiseSkittererCardId = "BG31_809";
        private const string ThreeLilQuilboarCardId = "BG26_867";
        private const string OperaticBelcherCardId = "BG33_318";
        private const string DragonCaretakerCardId = "BG34_633";
        private const string EternalSummonerHighCardId = "BG25_009";
        private const string BristlebachPortraitMinionCardId = "BG26_157";
        private const string ChoralMrrrglrCardId = "BG26_354";
        private const string DeadlySporebatCardId = "BG31_835";
        private const string SilkyShimmermothCardId = "BG32_204";
        private const string FireforgedEvokerCardId = "BG32_822";
        private const string RuinsLordCardId = "BG33_154";
        private const string CharmingWingCardId = "BG33_240";
        private const string DeadseaSmasherCardId = "BG34_765";
        private const string RingingNagaCardId = "BG34_921";
        private const string QueenGuardCardId = "BG34_926";
        private const string FallenSkyGolemCardId = "BG35_342";
        private const string ThornedTrailblazerCardId = "BG35_437";
        private const string SkyPirateCardId = "SKY_PIRATE";
        private const string ImpulsiveTricksterCardId = "BG21_006";
        private const string KaboomBotCardId = "BG_BOT_606";
        private const string BloodChampionCardId = "BG23_017";
        private const string SargerasChampionCardId = "BG27_016";
        private const string ObsidianRavagerDragonCardId = "BG27_017";
        private const string StitchedReclaimerCardId = "BG31_999";
        private const string RheaSupremeWardenCardId = "BG34_319";
        private const string LastOfItsKindCardId = "BG34_320";
        private const string TenaciousKodoCardId = "BG34_322";
        private const string GoldrinnCardId = "BGS_018";
        private const string TimewarpedAlleycatCardId = "BG34_Giant_009";
        private const string TimewarpedBuskerCardId = "BG34_Giant_001";
        private const string TimewarpedBoarCardId = "BG34_Giant_201";
        private const string TimewarpedScourfinCardId = "BG34_Giant_017";
        private const string TimewarpedFestergutCardId = "BG34_Giant_590";
        private const string TimewarpedNelliesShipCardId = "BG34_Giant_074t";
        private const string TimewarpedLeapfroggerCardId = "BG34_Giant_031";
        private const string TimewarpedGeistCardId = "BG34_Giant_034";
        private const string TimewarpedGreenskeeperCardId = "BG34_Giant_041";
        private const string TimewarpedChameleonCardId = "BG34_Giant_042";
        private const string TimewarpedDeiosCardId = "BG34_Giant_376";
        private const string TimewarpedHawkstriderCardId = "BG34_Giant_370";
        private const string TimewarpedPiperCardId = "BG34_Giant_069";
        private const string TimewarpedLilQuilboarCardId = "BG34_Giant_608";
        private const string TimewarpedBassgillCardId = "BG34_Giant_071";
        private const string TimewarpedDeathswarmerCardId = "BG34_Giant_081";
        private const string TimewarpedRedWhelpCardId = "BG34_Giant_091";
        private const string TimewarpedSeaGlassCardId = "BG34_Giant_110";
        private const string TimewarpedSauroliskCardId = "BG34_Giant_202";
        private const string TimewarpedMurculesCardId = "BG34_Giant_207";
        private const string TimewarpedPagleCardId = "BG34_Giant_208";
        private const string TimewarpedMothershipCardId = "BG34_Giant_598";
        private const string TimewarpedJazzerCardId = "BG34_Giant_306";
        private const string TimewarpedWarghoulCardId = "BG34_Giant_331";
        private const string TimewarpedRadioStarCardId = "BG34_Giant_330";
        private const string TimewarpedHyenaCardId = "BG34_Giant_581";
        private const string TimewarpedRagnarosCardId = "BG34_Giant_580";
        private const string TimewarpedHenchmanCardId = "BG34_Giant_593";
        private const string TimewarpedCaretakerCardId = "BG34_Giant_618";
        private const string TimewarpedGemsplitterCardId = "BG34_Giant_644";
        private const string TimewarpedIckyImpCardId = "BG34_Giant_674";
        private const string TimewarpedChimeraCardId = "BG34_Giant_679";
        private const string TimewarpedCollectorCardId = "BG34_Giant_680";
        private const string TimewarpedNestSwarmerCardId = "BG34_Giant_687";
        private const string TimewarpedTideRazorCardId = "BG34_Giant_328";
        private const string TimewarpedWhirlOTronCardId = "BG34_Giant_599";
        private const string TimewarpedPashmarCardId = "BG34_Giant_211";
        private const string TimewarpedCopterCardId = "BG34_Giant_302";
        private const string TimewarpedCalligrapherCardId = "BG34_PreMadeChamp_091";
        private const string TimewarpedDuskmawCardId = "BG34_PreMadeChamp_020";
        private const string TimewarpedGeomancerCardId = "BG34_Giant_305";
        private const string TimewarpedGhoulAcabraCardId = "BG34_Giant_609";
        private const string TimewarpedHunterCardId = "BG34_Giant_588";
        private const string TimewarpedPrismscaleCardId = "BG34_PreMadeChamp_022";
        private const string TimewarpedRecyclerCardId = "BG34_Giant_082";
        private const string TimewarpedStormcloudCardId = "BG34_PreMadeChamp_031";
        private const string TimewarpedThorncallerCardId = "BG34_Giant_078";
        private const string TimewarpedTravelerCardId = "BG34_Giant_605";
        private const string DisturbedGraveCardNumber = "126957";
        private const string ButcheringCardNumber = "110412";
        private const string MenagerieTablewareCardNumber = "105271";
        private const string StaffOfEnrichmentCardNumber = "105276";
        private const string SacredGiftCardNumber = "122899";
        private const string DeepwaterSchoolCardId = "131218";
        private const string ArcaneConsumptionCardId = "130311";
        private const string HealthyBountyCardId = "BG33_811";
        private const string DeathlyPhylacteryCardId = "BG30_MagicItem_700";
        private const string HeraldStickerCardId = "BG32_MagicItem_306";
        private const string DivineSignetCardId = "BG32_MagicItem_171";
        private const string MechagonAdapterCardId = "BG30_MagicItem_910";
        private const string DeathtouchAppleCardId = "BG35_MagicItem_731";
        private const string JarredFrostlingCardId = "BG30_MagicItem_952";
        private const string PowderKegCardId = "BG35_MagicItem_714";
        private const string SkyGolemPortraitCardId = "BG35_MagicItem_740";
        private const string HoggyBankCardId = "BG30_MagicItem_411";
        private const string RustyTridentCardId = "BG30_MagicItem_917";
        private const string JarOGemsCardId = "BG30_MagicItem_546";
        private const string ElementiumChestCardId = "BG30_MagicItem_923";
        private const string GilneanThornedRoseCardId = "BG30_MagicItem_864";
        private const string TigerCarvingCardId = "BG30_MagicItem_427";
        private const string TigerCarvingGreaterCardId = "BG30_MagicItem_427t";
        private const string ThornspikePauldronCardId = "BG35_MagicItem_431t";
        private const string MugOfTheSireCardId = "BG30_MagicItem_438t";
        private const string BlingtronsSunglassesCardId = "BG30_MagicItem_978";
        private const string ScrapsmithPortraitCardId = "BG35_MagicItem_430";
        private const string EyeOfDalaranCardId = "BG30_MagicItem_981";
        private const string UnholySanctumCardId = "BG32_MagicItem_862";
        private const string FishyStickerCardId = "BG30_MagicItem_821t2";
        private const string SoulFermenterCardId = "BG35_MagicItem_732";
        private const string BelcherPortraitCardId = "BG30_MagicItem_432";
        private const string BoomControllerCardId = "BG30_MagicItem_440";
        private const string BloodGolemStickerCardId = "BG30_MagicItem_442";
        private const string BloodAmuletCardId = "BG35_MagicItem_432";
        private const string AllPurposeKibbleCardId = "BG32_MagicItem_200";
        private const string STharaStickerCardId = "BG32_MagicItem_907";
        private const string BloodGolemTokenId = "blood-golem";
        private const string BloodGemSourceId = "Blood Gem";
        private const string FishOfNzothCardId = "FISH_OF_NZOTH";
        private const string HeroFishOfNzothCardId = "NZOTH_FISH";
        private const string SneedDeathrattleTierTagPrefix = "sneed_deathrattle_tier:";
        private const string PilotedWhirlOTronCardId = "BG21_HERO_030_Buddy";
        private const string WanderingTreantCardId = "TB_BaconShop_HERO_95_Buddy";
        private const string VenomstrikeTrapSecretId = "TB_Bacon_Secrets_01";
        private const string BetterVenomstrikeTrapSecretId = "TB_Bacon_Secrets_01b";
        private const string SnakeTrapSecretId = "TB_Bacon_Secrets_02";
        private const string SplittingImageSecretId = "TB_Bacon_Secrets_04";
        private const string EffigySecretId = "TB_Bacon_Secrets_05";
        private const string AutodefenseMatrixSecretId = "TB_Bacon_Secrets_07";
        private const string BetterAutodefenseMatrixSecretId = "TB_Bacon_Secrets_07b";
        private const string AvengeSecretId = "TB_Bacon_Secrets_08";
        private const string RedemptionSecretId = "TB_Bacon_Secrets_10";
        private const string BetterRedemptionSecretId = "TB_Bacon_Secrets_10b";
        private const string HandOfSalvationSecretId = "TB_Bacon_Secrets_11";
        private const string IceBlockSecretId = "TB_Bacon_Secrets_12";
        private const string ReckoningSecretId = "TB_Bacon_Secrets_14";
        private const string PackTacticsSecretId = "TB_Bacon_Secrets_15";
        private const string BetterPackTacticsSecretId = "TB_Bacon_Secrets_15b";
        private const string HandOfSalvationDeathsCounter = "secret:hand_of_salvation_deaths";
        private const string TavishPowerId = "BG22_HERO_000p";
        private const string CrabbyCardId = "BG22_HERO_000_Buddy";
        private const string TamsinPhylacteryTag = "hero_tamsin_phylactery";
        private const string MonstrosityCardId = "BG20_HERO_282_Buddy";
        private const string OnyxiaPowerId = "BG22_HERO_305p";
        private const string OnyxiaWhelpTokenId = "BG22_HERO_305t";
        private const string OnyxiaWhelpStatsCounter = "hero:onyxia:whelp_stats";
        private const string OnyxiaAvengeCounter = "hero:onyxia:avenge";
        private const string ManyWhelpsCardId = "BG22_HERO_305_Buddy";
        private const string BrukanPowerId = "BG22_HERO_001p";
        private const string SpiritRaptorCardId = "BG22_HERO_001_Buddy";
        private const string BrukanElementFire = "fire";
        private const string BrukanElementEarth = "earth";
        private const string BrukanElementWater = "water";
        private const string BrukanElementLightning = "lightning";
        private const string BrukanEarthDeathrattleTag = "hero_brukan_earth_deathrattle";
        private const string BrukanSpiritElementTagPrefix = "hero_brukan_spirit:";
        private const string BrukanElementalTokenId = "BG22_HERO_001t";
        private const string BattlecruiserCardId = "BG31_HERO_801pt";
        private const string ZerglingCardId = "BG31_HERO_811t2";
        private const string BanelingCardId = "BG31_HERO_811t5";
        private const string MutaliskCardId = "BG31_HERO_811t6";
        private const string LurkerCardId = "BG31_HERO_811t7";
        private const string UltraliskCardId = "BG31_HERO_811t10";
        private const string ProtossColossusCardId = "BG31_HERO_802pt";
        private const string ProtossCarrierCardId = "BG31_HERO_802pt1";
        private const string ProtossImmortalCardId = "BG31_HERO_802pt4";
        private const string ProtossVoidRayCardId = "BG31_HERO_802pt5";
        private const string ProtossMothershipCardId = "BG31_HERO_802pt7";
        private const string ProtossInterceptorTokenId = "PROTOSS_INTERCEPTOR";
        private const string FishyStickerFishTag = "trinket_fishy_sticker_fish";
        private const string WingmenImmediateAttackPendingTag = "wingmen_immediate_attack_pending";
        private const string EclipsionFirstAttackImmunePendingTag = "eclipsion_first_attack_immune_pending";
        private const string TwinSkyLanternCopyTag = "trinket_twin_sky_lantern_copy";
        private const string JarredFrostlingCounter = "trinket_jarred_frostling";
        private const string PowderKegCounter = "trinket_powder_keg";
        private const string HoggyBankCounter = "trinket_hoggy_bank";
        private const string RustyTridentCounter = "trinket_rusty_trident";
        private const string SkyGolemCounter = "trinket_sky_golem";
        private const string FlourishingFrostlingTokenId = "TRINKET_FLOURISHING_FROSTLING";
        private const int DeathtouchAppleUsesPerCombat = 3;

        public static CombatOutput SimulateBasicCombat(
            IEnumerable<MinionInstance> playerBoard,
            IEnumerable<MinionInstance> opponentBoard,
            int seed,
            int safetyLimit = 200,
            TavernState playerTavern = null,
            TavernState opponentTavern = null,
            IEnumerable<MinionInstance> playerHand = null,
            IEnumerable<MinionInstance> opponentHand = null,
            IEnumerable<MinionInstance> playerCombatSummonPool = null,
            IEnumerable<MinionInstance> opponentCombatSummonPool = null)
        {
            var context = new CombatContext(
                playerBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList(),
                opponentBoard.Select(minion => minion.Clone()).Where(IsAlive).ToList(),
                playerTavern,
                opponentTavern,
                playerHand?.Select(card => card.Clone()).ToList() ?? new List<MinionInstance>(),
                opponentHand?.Select(card => card.Clone()).ToList() ?? new List<MinionInstance>(),
                playerCombatSummonPool?.Select(card => card.Clone()).ToList() ?? new List<MinionInstance>(),
                opponentCombatSummonPool?.Select(card => card.Clone()).ToList() ?? new List<MinionInstance>(),
                seed);
            var attackerSide = context.Player.Board.Count >= context.Opponent.Board.Count ? BoardSide.Player : BoardSide.Opponent;
            var steps = 0;
            context.Replay.InitialSnapshot = CreateBoardPairSnapshot(context);
            AddLog(context.Log, "CombatStarted", "seed " + seed + " player " + context.Player.Board.Count + " opponent " + context.Opponent.Board.Count, null, null, LogSeverity.Normal);
            RecordFrame(context, CombatEventType.CombatStarted, "seed " + seed + " player " + context.Player.Board.Count + " opponent " + context.Opponent.Board.Count);

            ResolveHeroStartOfCombatEffects(context, ref steps, safetyLimit);
            var preCombatImmediateSide = QueueTaggedImmediateAttacks(context);
            ResolveImmediateAttacks(context, ref steps, safetyLimit);
            ResolveTrinketStartOfCombatEffects(context, context.Player);
            ResolveTrinketStartOfCombatEffects(context, context.Opponent);
            ResolveTrinketStartOfCombatDeathrattles(context, context.Player);
            ResolveTrinketStartOfCombatDeathrattles(context, context.Opponent);
            ApplyStartOfCombatAuras(context, context.Player);
            ApplyStartOfCombatAuras(context, context.Opponent);
            if (preCombatImmediateSide.HasValue)
            {
                attackerSide = preCombatImmediateSide.Value == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            }

            while (context.Player.Board.Any(IsAlive) && context.Opponent.Board.Any(IsAlive) && steps < safetyLimit)
            {
                var attackers = context.Get(attackerSide);
                var attackerIndex = FindNextAttackerIndex(attackers.Board, attackers.AttackIndex);
                if (attackerIndex < 0)
                {
                    break;
                }

                steps += 1;
                var attackResult = PerformAttack(context, attackerSide, attackerIndex, steps, false);
                ResolveExtraAttacks(context, attackResult, ref steps, safetyLimit);
                ResolveImmediateAttacks(context, ref steps, safetyLimit);

                AdvanceNaturalAttackPointers(context, attackerSide, attackerIndex);

                attackerSide = attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
            }

            var winner = context.Player.Board.Count == context.Opponent.Board.Count
                ? CombatWinner.Draw
                : context.Player.Board.Count > context.Opponent.Board.Count ? CombatWinner.Player : CombatWinner.Opponent;
            if (winner == CombatWinner.Opponent && TryTriggerSecret(context.Player.Tavern, IceBlockSecretId))
            {
                winner = CombatWinner.Draw;
                AddLog(context.Log, "SecretTriggered", "Ice Block prevented lethal hero damage", IceBlockSecretId, null, LogSeverity.Good);
            }

            AddLog(context.Log, "CombatEnded", "winner " + winner + " steps " + steps + " safety " + (steps >= safetyLimit), null, null, LogSeverity.Normal);
            context.Replay.Result = winner;
            context.Replay.Steps = steps;
            context.Replay.SafetyStopped = steps >= safetyLimit;
            context.Replay.PlayerRewards = CloneRewards(context.Player.Rewards);
            context.Replay.OpponentRewards = CloneRewards(context.Opponent.Rewards);
            RecordFrame(context, CombatEventType.CombatEnded, "winner " + winner + " steps " + steps + " safety " + (steps >= safetyLimit));
            return new CombatOutput
            {
                Winner = winner,
                FinalPlayerBoard = context.Player.Board,
                FinalOpponentBoard = context.Opponent.Board,
                Log = context.Log,
                Replay = context.Replay,
                PlayerRewards = context.Player.Rewards,
                OpponentRewards = context.Opponent.Rewards,
                Steps = steps,
                SafetyStopped = steps >= safetyLimit
            };
        }

        private static void ResolveHeroStartOfCombatEffects(CombatContext context, ref int steps, int safetyLimit)
        {
            ResolveTavishDeadeye(context);
            ResolveBrukanElement(context, context.Player, context.Player.Tavern?.HeroBrukanElement);
            ResolveBrukanElement(context, context.Opponent, context.Opponent.Tavern?.HeroBrukanElement);
            ResolveBattlecruiserStartOfCombat(context, context.Player);
            ResolveBattlecruiserStartOfCombat(context, context.Opponent);
            ResolveZergStartOfCombat(context, context.Player);
            ResolveZergStartOfCombat(context, context.Opponent);
            ResolveProtossStartOfCombat(context, context.Player);
            ResolveProtossStartOfCombat(context, context.Opponent);
            ResolveDeaths(context, BoardSide.Opponent);
            ResolveDeaths(context, BoardSide.Player);
            ResolveImmediateAttacks(context, ref steps, safetyLimit);
        }

        private static void ResolveTavishDeadeye(CombatContext context)
        {
            var tavern = context.Player.Tavern;
            if (tavern == null || !tavern.HeroTavishDeadeyeActive)
            {
                return;
            }

            var opponent = context.Opponent;
            var target = !string.IsNullOrEmpty(tavern.HeroTavishTargetInstanceId)
                ? opponent.Board.FirstOrDefault(minion => minion.InstanceId == tavern.HeroTavishTargetInstanceId && IsAlive(minion))
                : null;
            if (target == null && tavern.HeroTavishTargetIndex >= 0 && tavern.HeroTavishTargetIndex < opponent.Board.Count)
            {
                target = opponent.Board[tavern.HeroTavishTargetIndex];
            }

            target = target ?? opponent.Board.FirstOrDefault(IsAlive);
            if (target == null)
            {
                return;
            }

            var damage = GetCombatSpellDamage(context.Player, 1);
            var result = DealDamage(target, damage, false);
            if (result.Minion.Health <= 0)
            {
                MarkKilledBy(result.Minion, TavishPowerId, BoardSide.Player, TavishPowerId);
            }

            ReplaceByInstanceId(opponent.Board, result.Minion);
            AddLog(context.Log, "HeroStartOfCombat", "Deadeye dealt " + damage + " damage to " + target.InstanceId, TavishPowerId, target.InstanceId, LogSeverity.Good);
        }

        private static void ResolveBrukanElement(CombatContext context, CombatSideState owner, string element)
        {
            var tavern = owner?.Tavern;
            if (context == null || owner == null || tavern == null || !tavern.HeroBrukanElementActive || string.IsNullOrEmpty(element))
            {
                return;
            }

            CallBrukanElement(context, owner, element, BrukanPowerId, true);
        }

        private static void CallBrukanElement(CombatContext context, CombatSideState owner, string element, string sourceId, bool remember)
        {
            if (string.Equals(element, BrukanElementFire, StringComparison.OrdinalIgnoreCase))
            {
                var target = owner.Board.FirstOrDefault(IsAlive);
                if (target != null)
                {
                    BuffMinion(target, target.Attack, 0, "Fire Invocation");
                }
            }
            else if (string.Equals(element, BrukanElementEarth, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var target in owner.Board.Where(IsAlive).Take(4).ToList())
                {
                    AddKeyword(target, Keyword.Deathrattle);
                    AddTag(target, BrukanEarthDeathrattleTag);
                }
            }
            else if (string.Equals(element, BrukanElementWater, StringComparison.OrdinalIgnoreCase))
            {
                var target = owner.Board.LastOrDefault(IsAlive);
                if (target != null)
                {
                    BuffMinion(target, 0, 3, "Water Invocation");
                    AddKeyword(target, Keyword.Taunt);
                }
            }
            else if (string.Equals(element, BrukanElementLightning, StringComparison.OrdinalIgnoreCase))
            {
                var enemy = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
                for (var index = 0; index < 5; index += 1)
                {
                    var targets = enemy.Board.Where(IsAlive).ToList();
                    if (targets.Count == 0)
                    {
                        break;
                    }

                    var target = new SeededRng(context.Seed + context.AttackSequence * 607 + index * 31 + targets.Count).Pick(targets);
                    var result = DealDamage(target, GetCombatSpellDamage(owner, 1), false);
                    if (result.Minion.Health <= 0)
                    {
                        MarkKilledBy(result.Minion, sourceId, owner.Side, BrukanPowerId);
                    }

                    ReplaceByInstanceId(enemy.Board, result.Minion);
                }
            }

            if (remember)
            {
                RememberBrukanElement(owner, element);
            }

            AddLog(context.Log, "HeroStartOfCombat", "Embrace the Elements called " + element, sourceId, null, LogSeverity.Good);
        }

        private static void RememberBrukanElement(CombatSideState owner, string element)
        {
            var tag = BrukanSpiritElementTagPrefix + element;
            foreach (var raptor in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == SpiritRaptorCardId))
            {
                AddTag(raptor, tag);
            }
        }

        private static void ResolveBattlecruiserStartOfCombat(CombatContext context, CombatSideState owner)
        {
            var opponent = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            foreach (var battlecruiser in owner.Board
                .Where(minion => IsAlive(minion) && IsBattlecruiser(minion) && minion.Tags.Contains("battlecruiser_yamato"))
                .ToList())
            {
                battlecruiser.Counters.TryGetValue("battlecruiser_yamato", out var yamato);
                battlecruiser.Counters.TryGetValue("battlecruiser_missile_pod", out var missilePod);
                var shots = Math.Max(1, yamato) * (missilePod > 0 ? 2 : 1);
                for (var shot = 0; shot < shots; shot += 1)
                {
                    var target = opponent.Board
                        .Where(IsAlive)
                        .OrderByDescending(minion => minion.Health)
                        .ThenBy(minion => minion.InstanceId)
                        .FirstOrDefault();
                    if (target == null)
                    {
                        break;
                    }

                    var damage = Math.Max(1, battlecruiser.Attack);
                    var result = DealDamage(target, damage, false);
                    if (!IsAlive(result.Minion))
                    {
                        MarkKilledBy(result.Minion, battlecruiser.InstanceId, owner.Side, battlecruiser.CardId);
                    }

                    ReplaceByInstanceId(opponent.Board, result.Minion);
                    AddLog(context.Log, "HeroStartOfCombat", "Yamato Cannon dealt " + damage + " to " + target.InstanceId, battlecruiser.InstanceId, target.InstanceId, LogSeverity.Good);
                }
            }
        }

        private static void ResolveZergStartOfCombat(CombatContext context, CombatSideState owner)
        {
            foreach (var zergling in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == ZerglingCardId).ToList())
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    break;
                }

                var copy = zergling.Clone();
                copy.InstanceId = zergling.InstanceId + "-zergling-copy-" + context.Replay.Frames.Count;
                copy.Owner = owner.Side;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanAttack = true;
                ApplySummonAuras(owner, copy);
                var insertIndex = owner.Board.FindIndex(minion => minion.InstanceId == zergling.InstanceId);
                owner.Board.Insert(Math.Min(insertIndex + 1, owner.Board.Count), copy);
                ResolveFriendlySummonTriggers(context, owner, copy, zergling);
                AddLog(context.Log, "HeroStartOfCombat", "Zergling summoned a copy", zergling.InstanceId, copy.InstanceId, LogSeverity.Good);
            }

            foreach (var ultralisk in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == UltraliskCardId).ToList())
            {
                var multiplier = ultralisk.Golden ? 3 : 2;
                BuffMinion(ultralisk, ultralisk.Attack * (multiplier - 1), ultralisk.MaxHealth * (multiplier - 1), "Ultralisk");
                AddLog(context.Log, "HeroStartOfCombat", "Ultralisk multiplied its stats", ultralisk.InstanceId, null, LogSeverity.Good);
            }
        }

        private static void ResolveProtossStartOfCombat(CombatContext context, CombatSideState owner)
        {
            foreach (var immortal in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == ProtossImmortalCardId).ToList())
            {
                var index = owner.Board.FindIndex(minion => minion.InstanceId == immortal.InstanceId);
                if (index < 0)
                {
                    continue;
                }

                var adjacent = new List<MinionInstance>();
                if (index > 0 && IsAlive(owner.Board[index - 1]))
                {
                    adjacent.Add(owner.Board[index - 1]);
                }

                if (index + 1 < owner.Board.Count && IsAlive(owner.Board[index + 1]))
                {
                    adjacent.Add(owner.Board[index + 1]);
                }

                var attack = adjacent.Sum(minion => Math.Max(0, minion.Attack));
                var health = adjacent.Sum(minion => Math.Max(0, minion.MaxHealth));
                if (attack == 0 && health == 0)
                {
                    continue;
                }

                BuffMinion(immortal, attack, health, "Immortal");
                AddLog(context.Log, "HeroStartOfCombat", "Immortal gained adjacent stats", immortal.InstanceId, null, LogSeverity.Good);
            }
        }

        private static bool IsBattlecruiser(MinionInstance minion)
        {
            return minion != null &&
                (string.Equals(minion.CardId, BattlecruiserCardId, StringComparison.OrdinalIgnoreCase) ||
                 (minion.Tags != null && minion.Tags.Contains("battlecruiser")));
        }

        private static void ApplyStartOfCombatAuras(CombatContext context, CombatSideState side)
        {
            ApplySideCombatHistoryBonuses(context, side);

            if (side.Tavern != null && (side.Tavern.NextCombatBoardAttack != 0 || side.Tavern.NextCombatBoardHealth != 0))
            {
                foreach (var minion in side.Board.Where(IsAlive))
                {
                    BuffMinion(minion, side.Tavern.NextCombatBoardAttack, side.Tavern.NextCombatBoardHealth, "Next Combat Tavern Spell");
                }
            }

            if (side.Tavern != null && (side.Tavern.QuestFriendlyAttackAura != 0 || side.Tavern.QuestVolatileVenomActive))
            {
                var attack = side.Tavern.QuestFriendlyAttackAura + (side.Tavern.QuestVolatileVenomActive ? 7 : 0);
                var health = side.Tavern.QuestVolatileVenomActive ? 7 : 0;
                foreach (var minion in side.Board.Where(IsAlive))
                {
                    BuffMinion(minion, attack, health, "Quest combat aura");
                }
            }

            if (side.Tavern != null && side.Tavern.NextCombatBeetles > 0)
            {
                SummonStartOfCombatBeetles(context, side, side.Tavern.NextCombatBeetles);
            }

            if (side.Tavern != null)
            {
                ApplyDelayedTavernSpellCombatAuras(context, side);
            }

            ResolveTimewarpedChameleonStartOfCombat(context, side);
            ResolveTimewarpedRagnarosStartOfCombat(context, side);
            ResolveTimewarpedRedWhelpStartOfCombat(context, side);
            ResolveTimewarpedHawkstriderStartOfCombat(context, side);
            ResolveTimewarpedWhirlOTronStartOfCombat(context, side);

            foreach (var mrrrglr in side.Board.Where(minion => IsAlive(minion) && minion.CardId == ChoralMrrrglrCardId).ToList())
            {
                var multiplier = mrrrglr.Golden ? 2 : 1;
                var attack = StatMath.SaturatingMultiply(StatMath.SaturatingSum(side.Hand.Where(card => card.CardKind == CardKind.Minion).Select(card => card.Attack), 0, StatMath.MaxStat), multiplier, 0, StatMath.MaxStat);
                var health = StatMath.SaturatingMultiply(StatMath.SaturatingSum(side.Hand.Where(card => card.CardKind == CardKind.Minion).Select(card => card.MaxHealth), 0, StatMath.MaxStat), multiplier, 0, StatMath.MaxStat);
                BuffMinion(mrrrglr, attack, health, "Choral Mrrrglr");
            }

            foreach (var evoker in side.Board.Where(minion => IsAlive(minion) && minion.CardId == FireforgedEvokerCardId).ToList())
            {
                var attack = evoker.Golden ? 4 : 2;
                var health = evoker.Golden ? 2 : 1;
                evoker.Counters.TryGetValue("dragon_spell_attack", out var attackBonus);
                evoker.Counters.TryGetValue("dragon_spell_health", out var healthBonus);
                BuffAll(
                    side.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Dragon)),
                    StatMath.SaturatingAdd(attack, attackBonus, 0, StatMath.MaxStat),
                    StatMath.SaturatingAdd(health, healthBonus, 0, StatMath.MaxStat),
                    "Fireforged Evoker");
            }

            ResolveStitchedReclaimersAtCombatStart(context, side);

            foreach (var promo in side.Board.Where(minion => IsAlive(minion) && minion.CardId == PrizedPromoDrakeCardId).ToList())
            {
                var amount = promo.Golden ? 8 : 4;
                foreach (var dragon in side.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Dragon)))
                {
                    BuffMinion(dragon, amount, amount, "Prized Promo-Drake");
                }
            }

            foreach (var snapjaw in side.Board.Where(minion => IsAlive(minion) && minion.CardId == HungrySnapjawCardId).ToList())
            {
                SummonHighestAttackMurlocFromHand(context, side, snapjaw);
            }

            if (side.Tavern != null && side.Tavern.TemporaryAvengeBeastRewards > 0)
            {
                side.TemporaryAvengeBeastRewards = side.Tavern.TemporaryAvengeBeastRewards;
            }

            side.BeastAttackAura = side.Board
                .Where(minion => IsAlive(minion) && minion.CardId == HummingBirdCardId)
                .Sum(minion => minion.Golden ? 2 : 1);
            foreach (var guardian in side.Board.Where(minion => IsAlive(minion) && minion.CardId == AmberGuardianCardId).ToList())
            {
                var target = side.Board.FirstOrDefault(minion => minion.InstanceId != guardian.InstanceId && IsAlive(minion) && minion.Tribes.Contains(Tribe.Dragon));
                if (target == null)
                {
                    continue;
                }

                BuffMinion(target, guardian.Golden ? 4 : 2, guardian.Golden ? 4 : 2, "Amber Guardian");
                if (!target.Keywords.Contains(Keyword.DivineShield))
                {
                    target.Keywords.Add(Keyword.DivineShield);
                }
            }

            if (side.BeastAttackAura <= 0)
            {
                return;
            }

            foreach (var beast in side.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Beast)))
            {
                BuffMinion(beast, side.BeastAttackAura, 0, "Humming Bird");
            }
        }

        private static void ApplySideCombatHistoryBonuses(CombatContext context, CombatSideState side)
        {
            var tavern = side?.Tavern;
            if (context == null || side == null || side.Side != BoardSide.Opponent || tavern == null)
            {
                return;
            }

            var buffed = 0;
            var undeadAttack = Math.Max(0, tavern.UndeadAttackBonus);
            if (undeadAttack > 0)
            {
                foreach (var undead in side.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Undead)).ToList())
                {
                    if (HasTrackedCombatHistoryBuff(undead, "Undead Attack Bonus"))
                    {
                        continue;
                    }

                    BuffMinion(undead, undeadAttack, 0, "Undead Attack Bonus");
                    buffed += 1;
                }
            }

            var eternalDeaths = Math.Max(0, tavern.EternalKnightDeaths);
            if (eternalDeaths > 0)
            {
                foreach (var knight in side.Board.Where(minion => IsAlive(minion) && minion.CardId == EternalKnightCardId).ToList())
                {
                    if (HasTrackedCombatHistoryBuff(knight, "Eternal Knight"))
                    {
                        continue;
                    }

                    BuffMinion(
                        knight,
                        StatMath.SaturatingMultiply(eternalDeaths, knight.Golden ? 8 : 4, 0, StatMath.MaxStat),
                        StatMath.SaturatingMultiply(eternalDeaths, knight.Golden ? 4 : 2, 0, StatMath.MaxStat),
                        "Eternal Knight");
                    buffed += 1;
                }
            }

            var otherAutomatonSummons = Math.Max(0, tavern.AncestralAutomatonSummons - 1);
            if (otherAutomatonSummons > 0)
            {
                foreach (var automaton in side.Board.Where(minion => IsAlive(minion) && minion.CardId == AncestralAutomatonCardId).ToList())
                {
                    if (HasTrackedCombatHistoryBuff(automaton, "Ancestral Automaton"))
                    {
                        continue;
                    }

                    BuffMinion(
                        automaton,
                        StatMath.SaturatingMultiply(otherAutomatonSummons, automaton.Golden ? 6 : 3, 0, StatMath.MaxStat),
                        StatMath.SaturatingMultiply(otherAutomatonSummons, automaton.Golden ? 4 : 2, 0, StatMath.MaxStat),
                        "Ancestral Automaton");
                    buffed += 1;
                }
            }

            if (buffed > 0)
            {
                AddLog(context.Log, "StartOfCombat", "Applied " + buffed + " side history bonus(es)", null, null, LogSeverity.Good);
            }
        }

        private static bool HasTrackedCombatHistoryBuff(MinionInstance minion, string sourceId)
        {
            return minion?.Enchantments != null &&
                minion.Enchantments.Any(enchantment => string.Equals(enchantment?.SourceId, sourceId, StringComparison.Ordinal));
        }

        private static void ResolveTimewarpedChameleonStartOfCombat(CombatContext context, CombatSideState side)
        {
            for (var index = 1; index < side.Board.Count; index += 1)
            {
                var source = side.Board[index];
                if (!IsAlive(source) || source.CardId != TimewarpedChameleonCardId)
                {
                    continue;
                }

                var left = side.Board[index - 1];
                if (left == null || !IsAlive(left))
                {
                    continue;
                }

                var copy = left.Clone();
                copy.InstanceId = source.InstanceId;
                copy.Owner = side.Side;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanAttack = true;
                side.Board[index] = copy;
                AddLog(context.Log, "StartOfCombat", source.InstanceId + " transformed into a copy of " + left.InstanceId, source.InstanceId, left.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.TrinketTriggered,
                    source.InstanceId + " copied left minion " + left.InstanceId,
                    side.Side,
                    source.InstanceId,
                    side.Side,
                    left.InstanceId,
                    new[] { source.InstanceId, left.InstanceId },
                    null,
                    null,
                    null,
                    new[] { source.InstanceId });
            }
        }

        private static void ResolveTimewarpedHawkstriderStartOfCombat(CombatContext context, CombatSideState side)
        {
            foreach (var hawkstrider in side.Board.Where(minion => IsAlive(minion) && minion.CardId == TimewarpedHawkstriderCardId).ToList())
            {
                var multiplier = hawkstrider.Golden ? 2 : 1;
                var targetIds = side.Board
                    .Where(minion => IsAlive(minion) && minion.Keywords.Contains(Keyword.Deathrattle))
                    .Select(minion => minion.InstanceId)
                    .ToList();
                for (var repeat = 0; repeat < multiplier; repeat += 1)
                {
                    foreach (var targetId in targetIds)
                    {
                        TriggerStartOfCombatDeathrattle(context, side, targetId, hawkstrider.InstanceId);
                    }
                }
            }
        }

        private static void ResolveTimewarpedWhirlOTronStartOfCombat(CombatContext context, CombatSideState side)
        {
            foreach (var whirl in side.Board.Where(minion => IsAlive(minion) && minion.CardId == TimewarpedWhirlOTronCardId).ToList())
            {
                var copies = side.Board
                    .Where(minion =>
                        IsAlive(minion) &&
                        minion.InstanceId != whirl.InstanceId &&
                        minion.CardId != TimewarpedWhirlOTronCardId &&
                        minion.Keywords.Contains(Keyword.Deathrattle))
                    .Take(2)
                    .Select((minion, index) =>
                    {
                        var copy = minion.Clone();
                        copy.InstanceId = "timewarped-whirl-o-tron-copy-" + whirl.InstanceId + "-" + index + "-" + minion.InstanceId;
                        copy.Owner = side.Side;
                        copy.PoolSource = PoolSource.Summon;
                        copy.PoolCopiesHeld = 0;
                        return copy;
                    })
                    .ToList();
                if (copies.Count == 0)
                {
                    continue;
                }

                AddKeyword(whirl, Keyword.Deathrattle);
                side.CopiedDeathrattlesBySource[whirl.InstanceId] = copies;
                AddLog(context.Log, "StartOfCombat", "Timewarped Whirl-O-Tron copied " + copies.Count + " Deathrattle(s)", whirl.InstanceId, copies.First().InstanceId, LogSeverity.Good);
            }
        }

        private static void ResolveTimewarpedRagnarosStartOfCombat(CombatContext context, CombatSideState side)
        {
            var opponent = context.Get(side.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            foreach (var source in side.Board.Where(minion => IsAlive(minion) && minion.CardId == TimewarpedRagnarosCardId).ToList())
            {
                var target = opponent.Board
                    .Where(IsAlive)
                    .OrderByDescending(minion => minion.Health)
                    .ThenByDescending(minion => minion.MaxHealth)
                    .ThenBy(minion => minion.InstanceId)
                    .FirstOrDefault();
                if (target == null)
                {
                    continue;
                }

                var result = DealDamage(target, Math.Max(0, source.Attack), false);
                ReplaceByInstanceId(opponent.Board, result.Minion);
                ResolveDamageTriggers(
                    context,
                    side,
                    source.InstanceId,
                    false,
                    false,
                    opponent,
                    target.InstanceId,
                    result.CombatDamageDealt,
                    result.DivineShieldBroken);
                AddLog(context.Log, "StartOfCombat", source.InstanceId + " dealt " + source.Attack + " to " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
                ResolveDeaths(context, opponent.Side);
            }
        }

        private static void ResolveTimewarpedRedWhelpStartOfCombat(CombatContext context, CombatSideState side)
        {
            var opponent = context.Get(side.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            foreach (var source in side.Board.Where(minion => IsAlive(minion) && minion.CardId == TimewarpedRedWhelpCardId).ToList())
            {
                var candidates = opponent.Board.Where(IsAlive).ToList();
                var targets = new List<MinionInstance>();
                var rng = new SeededRng(context.Seed + context.AttackSequence * 101 + context.Log.Count + source.InstanceId.Sum(ch => ch));
                while (targets.Count < 2 && candidates.Count > 0)
                {
                    var index = rng.NextInt(candidates.Count);
                    targets.Add(candidates[index]);
                    candidates.RemoveAt(index);
                }

                if (targets.Count == 0)
                {
                    continue;
                }

                source.Counters.TryGetValue("timewarped_red_whelp_bonus", out var bonus);
                var amount = StatMath.SaturatingMultiply(3 + Math.Max(0, bonus), source.Golden ? 2 : 1, 0, StatMath.MaxStat);
                var damagedIds = new List<string>();
                foreach (var target in targets)
                {
                    var result = DealDamage(target, amount, false);
                    ReplaceByInstanceId(opponent.Board, result.Minion);
                    if (result.CombatDamageDealt || result.DivineShieldBroken)
                    {
                        damagedIds.Add(target.InstanceId);
                    }

                    ResolveDamageTriggers(
                        context,
                        side,
                        source.InstanceId,
                        false,
                        false,
                        opponent,
                        target.InstanceId,
                        result.CombatDamageDealt,
                        result.DivineShieldBroken);
                }

                if (damagedIds.Count > 0)
                {
                    AddLog(context.Log, "StartOfCombat", source.InstanceId + " dealt " + amount + " to " + string.Join(",", damagedIds.ToArray()), source.InstanceId, null, LogSeverity.Good);
                }

                ResolveDeaths(context, opponent.Side);
            }
        }

        private static void ApplyDelayedTavernSpellCombatAuras(CombatContext context, CombatSideState side)
        {
            var tavern = side.Tavern;
            var opponent = context.Get(side.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            for (var index = 0; index < tavern.NextCombatEnemyHealthToOne; index += 1)
            {
                var target = opponent.Board.Where(IsAlive).OrderByDescending(minion => minion.Health).ThenBy(minion => minion.InstanceId).FirstOrDefault();
                if (target != null)
                {
                    target.Health = 1;
                    AddLog(context.Log, "CombatSpellCast", "Tavern spell set " + target.InstanceId + " Health to 1", null, target.InstanceId, LogSeverity.Good);
                }
            }

            if (tavern.NextCombatLeftmostCopiesNearestEnemyStats)
            {
                var target = side.Board.FirstOrDefault(IsAlive);
                var enemy = opponent.Board.FirstOrDefault(IsAlive);
                if (target != null && enemy != null)
                {
                    BuffMinion(target, enemy.Attack, enemy.MaxHealth, "Share the Love");
                }
            }

            if (tavern.NextCombatLeftmostDoubleAttack)
            {
                var target = side.Board.FirstOrDefault(IsAlive);
                if (target != null)
                {
                    BuffMinion(target, target.Attack, 0, "Nozdormu's Offspring");
                }
            }

            if (tavern.NextCombatTriggerMixedMechanics)
            {
                AddReward(context.Log, side, CombatRewardType.AddGeneratedSpellToHand, "127642", ArcaneConsumptionCardId, 1);
                AddReward(context.Log, side, CombatRewardType.ImproveUndeadAttack, "127642", null, 2);
                AddReward(context.Log, side, CombatRewardType.AddRandomMagneticMechToHand, "127642", null, 1);
            }
        }

        private static void ResolveStitchedReclaimersAtCombatStart(CombatContext context, CombatSideState side)
        {
            foreach (var reclaimer in side.Board.Where(minion => IsAlive(minion) && minion.CardId == StitchedReclaimerCardId).ToList())
            {
                var index = side.Board.FindIndex(minion => minion.InstanceId == reclaimer.InstanceId);
                if (index < 0)
                {
                    continue;
                }

                var targetIndexes = reclaimer.Golden ? new[] { index - 1, index + 1 } : new[] { index - 1 };
                foreach (var targetIndex in targetIndexes.OrderByDescending(value => value))
                {
                    if (targetIndex < 0 || targetIndex >= side.Board.Count)
                    {
                        continue;
                    }

                    var target = side.Board[targetIndex];
                    if (target.CardId == StitchedReclaimerCardId)
                    {
                        continue;
                    }

                    var copy = target.Clone();
                    copy.InstanceId = "stitched-copy-" + reclaimer.InstanceId + "-" + target.InstanceId;
                    copy.Owner = side.Side;
                    copy.PoolSource = PoolSource.Summon;
                    copy.PoolCopiesHeld = 0;
                    side.StitchedCopies[reclaimer.InstanceId + ":" + target.InstanceId] = copy;
                    side.Board.RemoveAt(targetIndex);
                    AddLog(context.Log, "CombatStarted", reclaimer.InstanceId + " stitched " + target.InstanceId, reclaimer.InstanceId, target.InstanceId, LogSeverity.Good);
                }
            }
        }

        private static void SummonStartOfCombatBeetles(CombatContext context, CombatSideState side, int count)
        {
            var source = new MinionInstance
            {
                InstanceId = "start-of-combat-beetles-" + side.Side,
                DefinitionId = "boon-of-beetles",
                CardId = "110401",
                Name = "Boon of Beetles",
                Owner = side.Side,
                CardKind = CardKind.TavernSpell,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Tags = new List<string>()
            };

            for (var index = 0; index < count; index += 1)
            {
                AddToken(context, side, source, side.Board.Count, "boon-beetle", "Beetle", 1, 1, Tribe.Beast, Keyword.Taunt);
            }
        }

        private static void ApplySummonAuras(CombatSideState side, MinionInstance minion)
        {
            if (minion == null)
            {
                return;
            }

            if (side.BeastAttackAura > 0 && HasCountedTribe(minion, Tribe.Beast))
            {
                BuffMinion(minion, side.BeastAttackAura, 0, "Humming Bird");
            }

            foreach (var kodo in side.Board.Where(source => IsAlive(source) && source.CardId == TenaciousKodoCardId).ToList())
            {
                side.SummonAuraUses.TryGetValue(kodo.InstanceId, out var uses);
                if (uses >= 3 || minion.InstanceId == kodo.InstanceId)
                {
                    continue;
                }

                var amount = StatMath.SaturatingMultiply(Math.Max(kodo.Attack, kodo.MaxHealth), kodo.Golden ? 2 : 1, 0, StatMath.MaxStat);
                BuffMinion(minion, amount, amount, "Tenacious Kodo");
                side.SummonAuraUses[kodo.InstanceId] = uses + 1;
            }
        }

        private static void ResolveExtraAttacks(CombatContext context, AttackResult attackResult, ref int steps, int safetyLimit)
        {
            if (!attackResult.AttackerSurvived || !attackResult.AttackerHadWindfury)
            {
                return;
            }

            if (steps >= safetyLimit || !context.Get(attackResult.DefenderSide).Board.Any(IsAlive))
            {
                return;
            }

            var attackers = context.Get(attackResult.AttackerSide);
            var attackerIndex = attackers.Board.FindIndex(minion => minion.InstanceId == attackResult.AttackerId);
            if (attackerIndex < 0)
            {
                return;
            }

            steps += 1;
            var windfuryResult = PerformAttack(context, attackResult.AttackerSide, attackerIndex, steps, true);
            ResolveImmediateAttacks(context, ref steps, safetyLimit);
            AddLog(context.Log, "WindfuryResolved", windfuryResult.AttackerId + " extra attack", windfuryResult.AttackerId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.WindfuryResolved,
                windfuryResult.AttackerId + " extra attack",
                windfuryResult.AttackerSide,
                windfuryResult.AttackerId,
                windfuryResult.DefenderSide,
                null,
                new[] { windfuryResult.AttackerId });
        }

        private static void ResolveImmediateAttacks(CombatContext context, ref int steps, int safetyLimit)
        {
            while (context.ImmediateAttacks.Count > 0 && steps < safetyLimit && context.Player.Board.Any(IsAlive) && context.Opponent.Board.Any(IsAlive))
            {
                var request = context.ImmediateAttacks.Dequeue();
                var attackers = context.Get(request.Side);
                var attackerIndex = attackers.Board.FindIndex(minion => minion.InstanceId == request.InstanceId && IsAlive(minion));
                if (attackerIndex < 0)
                {
                    continue;
                }

                steps += 1;
                PerformAttack(context, request.Side, attackerIndex, steps, true);
            }
        }

        private static BoardSide? QueueTaggedImmediateAttacks(CombatContext context)
        {
            BoardSide? lastQueuedSide = null;
            QueueTaggedImmediateAttacks(context, context.Player, ref lastQueuedSide);
            QueueTaggedImmediateAttacks(context, context.Opponent, ref lastQueuedSide);
            return lastQueuedSide;
        }

        private static void QueueTaggedImmediateAttacks(CombatContext context, CombatSideState owner, ref BoardSide? lastQueuedSide)
        {
            foreach (var minion in owner.Board.Where(minion => IsAlive(minion) && minion.Tags.Contains(WingmenImmediateAttackPendingTag)).ToList())
            {
                minion.Tags.Remove(WingmenImmediateAttackPendingTag);
                context.ImmediateAttacks.Enqueue(new ImmediateAttackRequest(owner.Side, minion.InstanceId));
                lastQueuedSide = owner.Side;
                AddLog(context.Log, "ImmediateAttackQueued", minion.InstanceId + " queued by Wingmen", minion.InstanceId, null, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.ImmediateAttackQueued,
                    minion.InstanceId + " queued by Wingmen",
                    owner.Side,
                    minion.InstanceId,
                    owner.Side,
                    null,
                    new[] { minion.InstanceId });
            }
        }

        private static AttackResult PerformAttack(CombatContext context, BoardSide attackerSide, int attackerIndex, int step, bool triggeredAttack)
        {
            var attackers = context.Get(attackerSide);
            var defenders = context.Get(attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            if (attackerIndex < 0 || attackerIndex >= attackers.Board.Count || !defenders.Board.Any(IsAlive))
            {
                return AttackResult.Empty(attackerSide);
            }

            var attacker = attackers.Board[attackerIndex];
            var defender = ChooseDefender(defenders.Board.Where(IsAlive).ToList(), context.Seed + step + context.AttackSequence);
            var defenderIndex = defenders.Board.FindIndex(minion => minion.InstanceId == defender.InstanceId);
            var defenderHealthBeforeDamage = defender.Health;
            RecordFrame(
                context,
                CombatEventType.AttackDeclared,
                attacker.InstanceId + " attacks " + defender.InstanceId,
                attackers.Side,
                attacker.InstanceId,
                defenders.Side,
                defender.InstanceId,
                new[] { attacker.InstanceId, defender.InstanceId },
                null,
                null,
                null,
                null,
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                triggeredAttack);
            ResolveAttackDeclarationTriggers(context, attackers, attacker, defenders, defender, triggeredAttack);
            var attackerVenomous = attacker.Keywords.Contains(Keyword.Venomous);
            var defenderVenomous = defender.Keywords.Contains(Keyword.Venomous);
            var attackerPoison = attacker.Keywords.Contains(Keyword.Poisonous) || attackerVenomous;
            var defenderPoison = defender.Keywords.Contains(Keyword.Poisonous) || defenderVenomous;
            var attackerImmuneWhileAttacking = SideHasTag(attackers, EclipsionFirstAttackImmunePendingTag);
            var defenderDamage = DealDamage(defender, attacker.Attack, attackerPoison);
            var attackerDamage = attackerImmuneWhileAttacking
                ? new DamageResult(attacker.Clone(), false)
                : DealDamage(attacker, defender.Attack, defenderPoison);
            var damagedDefender = defenderDamage.Minion;
            var damagedAttacker = attackerDamage.Minion;
            if (attackerImmuneWhileAttacking)
            {
                RemoveTagFromSide(attackers, EclipsionFirstAttackImmunePendingTag);
                damagedAttacker.Tags.Remove(EclipsionFirstAttackImmunePendingTag);
                AddLog(context.Log, "ImmuneWhileAttackingResolved", attacker.InstanceId + " ignored defender damage while attacking", attacker.InstanceId, defender.InstanceId, LogSeverity.Good);
            }

            ResolveCleaveDamage(context, attackers, attacker, defenders, defenderIndex);
            damagedAttacker.Keywords.Remove(Keyword.Stealth);
            damagedAttacker.AttacksThisCombat += 1;
            var damagedIds = new List<string>();
            if (attackerDamage.CombatDamageDealt || attackerDamage.DivineShieldBroken)
            {
                damagedIds.Add(attacker.InstanceId);
            }

            if (defenderDamage.CombatDamageDealt || defenderDamage.DivineShieldBroken)
            {
                damagedIds.Add(defender.InstanceId);
            }

            if (attackerVenomous && defenderDamage.CombatDamageDealt)
            {
                damagedAttacker.Keywords.Remove(Keyword.Venomous);
                ResolveTrinketVenomousLost(context, attackers, damagedAttacker);
            }

            if (defenderVenomous && attackerDamage.CombatDamageDealt)
            {
                damagedDefender.Keywords.Remove(Keyword.Venomous);
                ResolveTrinketVenomousLost(context, defenders, damagedDefender);
            }

            if (damagedDefender.Health <= 0)
            {
                MarkKilledBy(damagedDefender, damagedAttacker.InstanceId, attackers.Side, damagedAttacker.CardId);
            }

            if (damagedAttacker.Health <= 0)
            {
                MarkKilledBy(damagedAttacker, damagedDefender.InstanceId, defenders.Side, damagedDefender.CardId);
            }

            ReplaceByInstanceId(attackers.Board, damagedAttacker);
            ReplaceByInstanceId(defenders.Board, damagedDefender);
            ResolveReckoningSecret(context, defenders, damagedAttacker, defenderDamage.CombatDamageDealt, attacker.Attack);
            ResolveReckoningSecret(context, attackers, damagedDefender, attackerDamage.CombatDamageDealt, defender.Attack);
            ResolveOverkillTriggers(context, attackers, damagedAttacker, defenders, defenderIndex, defenderHealthBeforeDamage, defenderDamage);
            QueueFriendlyAttackReward(context, attackers, damagedAttacker, damagedDefender, triggeredAttack);
            QueueDamagedMinionRewards(context.Log, attackers, attacker, attackerDamage.CombatDamageDealt);
            QueueDamagedMinionRewards(context.Log, defenders, defender, defenderDamage.CombatDamageDealt);
            context.AttackSequence += 1;
            AddLog(
                context.Log,
                triggeredAttack ? "TriggeredAttackResolved" : "AttackResolved",
                attacker.InstanceId + " attacked " + defender.InstanceId,
                attacker.InstanceId,
                defender.InstanceId,
                LogSeverity.Normal);
            if (attackerDamage.DivineShieldBroken || defenderDamage.DivineShieldBroken)
            {
                RecordFrame(
                    context,
                    CombatEventType.DivineShieldBroken,
                    attacker.InstanceId + " / " + defender.InstanceId + " shield check",
                    attackers.Side,
                    attacker.InstanceId,
                    defenders.Side,
                    defender.InstanceId,
                    new[] { attacker.InstanceId, defender.InstanceId },
                    damagedIds);
            }

            if ((attackerVenomous && defenderDamage.CombatDamageDealt) || (defenderVenomous && attackerDamage.CombatDamageDealt))
            {
                RecordFrame(
                    context,
                    CombatEventType.VenomousResolved,
                    attacker.InstanceId + " / " + defender.InstanceId + " venomous resolved",
                    attackers.Side,
                    attacker.InstanceId,
                    defenders.Side,
                    defender.InstanceId,
                    new[] { attacker.InstanceId, defender.InstanceId },
                    damagedIds);
            }

            RecordFrame(
                context,
                CombatEventType.DamageResolved,
                attacker.InstanceId + " attacked " + defender.InstanceId,
                attackers.Side,
                attacker.InstanceId,
                defenders.Side,
                defender.InstanceId,
                new[] { attacker.InstanceId, defender.InstanceId },
                damagedIds,
                null,
                null,
                null,
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                (attackerDamage.CombatDamageDealt ? 1 : 0) + (defenderDamage.CombatDamageDealt ? 1 : 0),
                (attackerDamage.DivineShieldBroken ? 1 : 0) + (defenderDamage.DivineShieldBroken ? 1 : 0),
                triggeredAttack);
            ResolveDamageTriggers(
                context,
                attackers,
                attacker.InstanceId,
                attackerDamage.CombatDamageDealt,
                attackerDamage.DivineShieldBroken,
                defenders,
                defender.InstanceId,
                defenderDamage.CombatDamageDealt,
                defenderDamage.DivineShieldBroken);

            if (attackers.Tavern != null && attackers.Tavern.QuestVolatileVenomActive)
            {
                var volatileAttacker = attackers.Board.FirstOrDefault(minion => minion.InstanceId == attacker.InstanceId && IsAlive(minion));
                if (volatileAttacker != null)
                {
                    volatileAttacker.Health = 0;
                    MarkKilledBy(volatileAttacker, volatileAttacker.InstanceId, attackers.Side, "BG24_Reward_364");
                    AddLog(context.Log, "VolatileVenom", volatileAttacker.InstanceId + " died after attacking", volatileAttacker.InstanceId, null, LogSeverity.Warning);
                }
            }

            var deadIds = attackers.Board.Concat(defenders.Board).Where(minion => minion.Health <= 0).Select(minion => minion.InstanceId).ToList();
            if (deadIds.Count > 0)
            {
                RecordFrame(
                    context,
                    CombatEventType.DeathQueued,
                    "death queue " + string.Join(",", deadIds.ToArray()),
                    attackers.Side,
                    attacker.InstanceId,
                    defenders.Side,
                    defender.InstanceId,
                    deadIds,
                    null,
                    deadIds);
            }

            ResolveDeaths(context, attackers.Side);
            ResolveDeaths(context, defenders.Side);
            ResolveRally(context, attackers.Side, attacker.InstanceId, triggeredAttack);

            var attackerSurvived = attackers.Board.Any(minion => minion.InstanceId == attacker.InstanceId && IsAlive(minion));
            return new AttackResult(
                attacker.InstanceId,
                attackers.Side,
                defenders.Side,
                attackerSurvived,
                attacker.Keywords.Contains(Keyword.Windfury) && !triggeredAttack);
        }

        private static bool IsAlive(MinionInstance minion)
        {
            return minion.Health > 0;
        }

        private static void ResolveDeaths(CombatContext context, BoardSide side)
        {
            var owner = context.Get(side);
            var index = 0;
            var newEntityIds = new List<string>();
            var retargetSourceIds = new List<string>();
            while (index < owner.Board.Count)
            {
                var minion = owner.Board[index];
                if (minion.Health > 0)
                {
                    index += 1;
                    continue;
                }

                QueueFriendlyKillReward(context, owner, minion);
                TrackDeadMech(owner, minion);
                TrackSTharaDemonDeath(owner, minion);
                owner.Board.RemoveAt(index);
                var inserted = 0;
                var newEntityCountBeforeDeathEffects = newEntityIds.Count;
                inserted += ResolveRedemptionSecret(context, owner, minion, index + inserted, newEntityIds);
                inserted += ResolveDeathSecretTriggers(context, owner, minion, index + inserted, newEntityIds);
                AddReward(context.Log, owner, CombatRewardType.FriendlyMinionDied, minion.CardId, null, 1, minion.InstanceId);
                ResolveTavishCrabbyReward(context, owner, minion);
            ResolveTamsinMonstrosityDeath(context, owner, minion);
            ResolveTimewarpedFriendlyDeathTriggers(context, owner, minion);
            ResolveTimewarpedBoarDeath(context, owner, minion);
            ResolveEyeOfDalaranDeath(context, owner, minion);
                if (minion.Keywords.Contains(Keyword.Deathrattle))
                {
                    AddReward(context.Log, owner, CombatRewardType.FriendlyDeathrattleMinionDied, minion.CardId, null, 1, minion.InstanceId);
                    CopyDeathrattleToFishOfNzoth(context, owner, minion);
                }

                inserted += ResolveBoomControllerDeath(context, owner, minion, index + inserted, newEntityIds);
                inserted += ResolveBloodGolemStickerDeath(context, owner, minion, index + inserted, newEntityIds);
                ResolveAllianceKeychain(context, owner, minion);
                ResolveRotHideGnollDeathAura(owner);
                if (minion.Keywords.Contains(Keyword.Taunt))
                {
                    QueueTauntDeathRewards(context, owner, minion);
                    ResolveScrapsmithPortraitTauntDeath(context, owner, minion);
                }

                if (minion.CardId == EternalKnightCardId)
                {
                    AddReward(context.Log, owner, CombatRewardType.EternalKnightDied, minion.CardId, null, 1);
                }

                ResolveAvenge(context, owner, minion.InstanceId);
                if (minion.Keywords.Contains(Keyword.Deathrattle))
                {
                    inserted += ResolveDeathrattleEffect(context, owner, minion, index + inserted, newEntityIds, true, minion.InstanceId);
                }

                if (minion.Keywords.Contains(Keyword.Reborn))
                {
                    var reborn = minion.Clone();
                    reborn.Health = minion.Tags.Contains("battlecruiser_full_health_reborn")
                        ? Math.Max(1, reborn.MaxHealth)
                        : 1;
                    reborn.MaxHealth = Math.Max(1, reborn.MaxHealth);
                    reborn.Keywords.Remove(Keyword.Reborn);
                    if (owner.Board.Count >= BoardLimit)
                    {
                        RecordRebornOverflow(context, owner, minion);
                    }
                    else
                    {
                        ApplySummonAuras(owner, reborn);
                        owner.Board.Insert(Math.Min(index + inserted, owner.Board.Count), reborn);
                        ResolveFriendlySummonTriggers(context, owner, reborn, minion);
                        inserted += 1;
                        newEntityIds.Add(reborn.InstanceId);
                        AddLog(context.Log, "RebornResolved", minion.InstanceId + " reborn", minion.InstanceId, null, LogSeverity.Good);
                        RecordFrame(
                            context,
                            CombatEventType.RebornResolved,
                            minion.InstanceId + " reborn",
                            owner.Side,
                            minion.InstanceId,
                            owner.Side,
                            reborn.InstanceId,
                            new[] { minion.InstanceId, reborn.InstanceId },
                            null,
                            null,
                            new[] { reborn.InstanceId },
                            new[] { minion.InstanceId });
                        ResolveDeathtouchAppleReborn(context, owner, reborn, minion.InstanceId);
                    }
                }

                if (newEntityIds.Count > newEntityCountBeforeDeathEffects)
                {
                    retargetSourceIds.Add(minion.InstanceId);
                }

                index += inserted;
            }

            ResolveSoulFermenterResummon(context, owner, newEntityIds);
            ResolveSTharaStickerResummon(context, owner, newEntityIds);
            RetargetAttackPointerToNewUnits(context, owner, newEntityIds, retargetSourceIds);
        }

        private static void ResolveDeathtouchAppleReborn(CombatContext context, CombatSideState owner, MinionInstance reborn, string sourceId)
        {
            var tavern = owner?.Tavern;
            if (context == null ||
                tavern == null ||
                reborn == null ||
                tavern.TrinketDeathtouchAppleUses <= 0 ||
                reborn.CardKind != CardKind.Minion ||
                !IsAlive(reborn) ||
                !HasCountedTribe(reborn, Tribe.Undead) ||
                (reborn.Keywords != null && reborn.Keywords.Contains(Keyword.Reborn)))
            {
                return;
            }

            AddKeyword(reborn, Keyword.Reborn);
            tavern.TrinketDeathtouchAppleUses = Math.Max(0, tavern.TrinketDeathtouchAppleUses - 1);
            var usedThisCombat = Math.Min(
                DeathtouchAppleUsesPerCombat,
                Math.Max(1, DeathtouchAppleUsesPerCombat - tavern.TrinketDeathtouchAppleUses));
            var message = "Deathtouch Apple gave Reborn to " + reborn.InstanceId;
            AddLog(context.Log, "TrinketRebornTriggered", message, DeathtouchAppleCardId, reborn.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.TrinketTriggered,
                message,
                owner.Side,
                DeathtouchAppleCardId,
                owner.Side,
                reborn.InstanceId,
                new[] { DeathtouchAppleCardId, sourceId, reborn.InstanceId },
                null,
                null,
                null,
                new[] { DeathtouchAppleCardId, sourceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                false,
                usedThisCombat,
                DeathtouchAppleUsesPerCombat);
        }

        private static void ResolveTavishCrabbyReward(CombatContext context, CombatSideState defeatedOwner, MinionInstance defeated)
        {
            if (defeatedOwner?.Side != BoardSide.Opponent ||
                defeated == null ||
                !string.Equals(GetKillerCardId(defeated), TavishPowerId, StringComparison.OrdinalIgnoreCase) ||
                !context.Player.Board.Any(minion => IsAlive(minion) && minion.CardId == CrabbyCardId))
            {
                return;
            }

            AddReward(context.Log, context.Player, CombatRewardType.AddPlainCopyOfKilledEnemyToHand, CrabbyCardId, defeated.CardId, 1, defeated.InstanceId);
        }

        private static void ResolveTamsinMonstrosityDeath(CombatContext context, CombatSideState owner, MinionInstance deadMinion)
        {
            if (owner == null || deadMinion == null)
            {
                return;
            }

            var attack = Math.Max(0, deadMinion.Attack);
            if (attack <= 0)
            {
                return;
            }

            foreach (var monstrosity in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == MonstrosityCardId).ToList())
            {
                BuffMinion(monstrosity, attack, 0, "Monstrosity");
                AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, MonstrosityCardId, monstrosity.InstanceId, 1, attack, 0, deadMinion.InstanceId);
            }
        }

        private static void ResolveTrinketStartOfCombatEffects(CombatContext context, CombatSideState owner)
        {
            var tavern = owner.Tavern;
            if (tavern == null)
            {
                return;
            }

            if (tavern.TrinketFishyStickerActive)
            {
                SummonFishOfNzoth(context, owner);
            }

            if (tavern.TrinketSoulFermenterActive)
            {
                DestroySoulFermenterLeftmostMinions(context, owner);
            }

            ApplyTrinketDeathrattleGrant(
                context,
                owner,
                owner.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Elemental)),
                tavern.TrinketJarredFrostlingTargets,
                JarredFrostlingCounter,
                JarredFrostlingCardId,
                "Jarred Frostling");
            ApplyTrinketDeathrattleGrant(
                context,
                owner,
                owner.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Pirate)),
                tavern.TrinketPowderKegTargets,
                PowderKegCounter,
                PowderKegCardId,
                "Powder Keg");
            if (tavern.TrinketHoggyBankActive)
            {
                ApplyTrinketDeathrattleGrant(
                    context,
                    owner,
                    owner.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Quilboar)),
                    int.MaxValue,
                    HoggyBankCounter,
                    HoggyBankCardId,
                    "Hoggy Bank");
            }

            if (tavern.TrinketRustyTridentTriggers > 0)
            {
                ApplyTrinketDeathrattleGrant(
                    context,
                    owner,
                    owner.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Naga)),
                    int.MaxValue,
                    RustyTridentCounter,
                    RustyTridentCardId,
                    "Rusty Trident",
                    tavern.TrinketRustyTridentTriggers);
            }

            if (tavern.TrinketSkyGolemDeathrattleTriggers > 0)
            {
                ApplyTrinketDeathrattleGrant(
                    context,
                    owner,
                    owner.Board.Where(IsAlive),
                    int.MaxValue,
                    SkyGolemCounter,
                    SkyGolemPortraitCardId,
                    "Sky Golem Portrait",
                    tavern.TrinketSkyGolemDeathrattleTriggers);
            }
        }

        private static void ResolveTimewarpedFriendlyDeathTriggers(CombatContext context, CombatSideState owner, MinionInstance deadMinion)
        {
            if (deadMinion == null || !HasCountedTribe(deadMinion, Tribe.Beast))
            {
                return;
            }

            foreach (var hyena in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == TimewarpedHyenaCardId).ToList())
            {
                var multiplier = hyena.Golden ? 2 : 1;
                BuffMinion(hyena, 2 * multiplier, 2 * multiplier, "Timewarped Hyena");
                AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, hyena.CardId, hyena.InstanceId, 1, 2 * multiplier, 2 * multiplier, deadMinion.InstanceId);
            }
        }

        private static void ResolveTimewarpedBoarDeath(CombatContext context, CombatSideState owner, MinionInstance deadMinion)
        {
            if (deadMinion == null || deadMinion.CardId != TimewarpedBoarCardId)
            {
                return;
            }

            owner.AvengeCounters.TryGetValue("timewarped-boar-deaths", out var count);
            count += 1;
            while (count >= 3)
            {
                count -= 3;
                AddReward(context.Log, owner, CombatRewardType.AddRandomGoldenBeastToHand, deadMinion.CardId, null, 1, deadMinion.InstanceId);
            }

            owner.AvengeCounters["timewarped-boar-deaths"] = count;
            AddLog(context.Log, "TimewarpedBoar", "Timewarped Boar death count " + count + "/3", deadMinion.InstanceId, null, LogSeverity.Good);
        }

        private static void ApplyTrinketDeathrattleGrant(
            CombatContext context,
            CombatSideState owner,
            IEnumerable<MinionInstance> candidates,
            int targetCount,
            string counterKey,
            string sourceCardId,
            string sourceName,
            int stacksPerTarget = 1)
        {
            if (targetCount <= 0 || stacksPerTarget <= 0)
            {
                return;
            }

            var targets = candidates
                .Where(minion => minion != null && minion.CardKind == CardKind.Minion)
                .Take(targetCount)
                .ToList();
            if (targets.Count == 0)
            {
                return;
            }

            foreach (var target in targets)
            {
                AddKeyword(target, Keyword.Deathrattle);
                target.Counters.TryGetValue(counterKey, out var current);
                target.Counters[counterKey] = StatMath.SaturatingAdd(current, stacksPerTarget, 0, StatMath.MaxStat);
            }

            AddLog(context.Log, "TrinketTriggered", sourceName + " granted Deathrattle to " + targets.Count + " minion(s)", sourceCardId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.TrinketTriggered,
                sourceName + " granted Deathrattle to " + targets.Count + " minion(s)",
                owner.Side,
                sourceCardId,
                owner.Side,
                targets.FirstOrDefault()?.InstanceId,
                targets.Select(target => target.InstanceId).Concat(new[] { sourceCardId }),
                null,
                null,
                null,
                targets.Select(target => target.InstanceId),
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                false,
                targets.Count,
                targetCount == int.MaxValue ? targets.Count : targetCount);
        }

        private static void SummonFishOfNzoth(CombatContext context, CombatSideState owner)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                RecordSummonOverflow(context, owner, null, FishOfNzothCardId, "Fish of N'Zoth");
                return;
            }

            var source = new MinionInstance
            {
                InstanceId = "trinket-fishy-sticker-" + owner.Side,
                DefinitionId = FishyStickerCardId,
                CardId = FishyStickerCardId,
                Name = "Fishy Sticker",
                Owner = owner.Side,
                CardKind = CardKind.TavernSpell,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Tags = new List<string>()
            };
            var fish = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "trinket-fish-of-nzoth-" + owner.Side,
                DefinitionId = FishOfNzothCardId,
                CardId = FishOfNzothCardId,
                Name = "Fish of N'Zoth",
                BaseAttack = 4,
                BaseHealth = 4,
                Attack = 4,
                Health = 4,
                MaxHealth = 4,
                TavernTier = 1,
                Golden = true,
                Owner = owner.Side,
                Tribes = new List<Tribe> { Tribe.Beast },
                Keywords = new List<Keyword> { Keyword.Deathrattle },
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string> { FishyStickerFishTag },
                CanAttack = true,
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0
            };
            ApplySummonAuras(owner, fish);
            owner.Board.Add(fish);
            ResolveFriendlySummonTriggers(context, owner, fish, source);
            AddLog(context.Log, "MinionSummoned", "Fishy Sticker summoned Golden Fish of N'Zoth", source.InstanceId, fish.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                "Fishy Sticker summoned Golden Fish of N'Zoth",
                owner.Side,
                source.InstanceId,
                owner.Side,
                fish.InstanceId,
                new[] { source.InstanceId, fish.InstanceId },
                null,
                null,
                new[] { fish.InstanceId },
                new[] { source.InstanceId });
        }

        private static void DestroySoulFermenterLeftmostMinions(CombatContext context, CombatSideState owner)
        {
            var targets = owner.Board
                .Where(IsAlive)
                .Take(3)
                .ToList();
            if (targets.Count == 0)
            {
                return;
            }

            owner.SoulFermenterStoredMinions.Clear();
            foreach (var target in targets)
            {
                var stored = target.Clone();
                stored.InstanceId = "soul-fermenter-stored-" + owner.SoulFermenterStoredMinions.Count + "-" + target.InstanceId;
                stored.Owner = owner.Side;
                stored.PoolSource = PoolSource.Summon;
                stored.PoolCopiesHeld = 0;
                stored.CanAttack = true;
                owner.SoulFermenterStoredMinions.Add(stored);

                target.Health = 0;
                MarkKilledBy(target, SoulFermenterCardId, owner.Side, SoulFermenterCardId);
            }

            AddLog(context.Log, "DeathQueued", "Soul Fermenter destroyed " + targets.Count + " left-most minion(s)", SoulFermenterCardId, null, LogSeverity.Warning);
            RecordFrame(
                context,
                CombatEventType.DeathQueued,
                "Soul Fermenter destroyed " + targets.Count + " left-most minion(s)",
                owner.Side,
                SoulFermenterCardId,
                owner.Side,
                null,
                targets.Select(minion => minion.InstanceId),
                null,
                targets.Select(minion => minion.InstanceId),
                null,
                new[] { SoulFermenterCardId });
            ResolveDeaths(context, owner.Side);
        }

        private static void ResolveTrinketStartOfCombatDeathrattles(CombatContext context, CombatSideState owner)
        {
            var tavern = owner.Tavern;
            if (tavern == null)
            {
                return;
            }

            if (tavern.TrinketHeraldStickerActive)
            {
                var sources = owner.Board
                    .Where(minion => IsAlive(minion) && minion.Keywords.Contains(Keyword.Deathrattle))
                    .Select(minion => minion.InstanceId)
                    .ToList();
                foreach (var sourceId in sources)
                {
                    TriggerStartOfCombatDeathrattle(context, owner, sourceId, "Herald Sticker");
                }
            }

            if (tavern.TrinketRylakPortraitActive)
            {
                var sources = owner.Board
                    .Where(minion => IsAlive(minion) && minion.CardId == HeavyMetalWyrmCardId && minion.Keywords.Contains(Keyword.Deathrattle))
                    .Select(minion => minion.InstanceId)
                    .ToList();
                foreach (var sourceId in sources)
                {
                    TriggerStartOfCombatDeathrattle(context, owner, sourceId, "Rylak Portrait");
                }
            }
        }

        private static void TriggerStartOfCombatDeathrattle(CombatContext context, CombatSideState owner, string sourceId, string sourceName)
        {
            var sourceIndex = owner.Board.FindIndex(minion => minion.InstanceId == sourceId && IsAlive(minion) && minion.Keywords.Contains(Keyword.Deathrattle));
            if (sourceIndex < 0)
            {
                return;
            }

            var source = owner.Board[sourceIndex];
            var newEntityIds = new List<string>();
            ResolveDeathrattleEffect(context, owner, source, Math.Min(sourceIndex + 1, owner.Board.Count), newEntityIds, false, sourceName);
            ResolveDeaths(context, owner.Side);
            ResolveDeaths(context, owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
        }

        private static int ResolveDeathrattleEffect(
            CombatContext context,
            CombatSideState owner,
            MinionInstance minion,
            int insertIndex,
            List<string> newEntityIds,
            bool sourceRemoved,
            string sourceName)
        {
            var detail = sourceName == minion.InstanceId
                ? minion.InstanceId + " deathrattle"
                : sourceName + " triggered " + minion.InstanceId + " deathrattle";
            AddLog(context.Log, "DeathrattleResolved", detail, minion.InstanceId, null, LogSeverity.Normal);
            RecordFrame(
                context,
                CombatEventType.DeathrattleResolved,
                detail,
                owner.Side,
                minion.InstanceId,
                owner.Side,
                null,
                new[] { minion.InstanceId },
                null,
                sourceRemoved ? new[] { minion.InstanceId } : null,
                null,
                new[] { minion.InstanceId });
            var deathrattleRepeats = GetDeathrattleRepeats(owner);
            AddReward(context.Log, owner, CombatRewardType.FriendlyDeathrattleTriggered, minion.CardId, null, deathrattleRepeats, minion.InstanceId);
            var thornedTrailblazerBonus = owner.Board
                .Where(candidate => IsAlive(candidate) && candidate.CardId == ThornedTrailblazerCardId)
                .Sum(candidate => candidate.Golden ? 2 : 1);
            if (thornedTrailblazerBonus > 0)
            {
                AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemAttack, ThornedTrailblazerCardId, null, thornedTrailblazerBonus);
                if (owner.Tavern != null && owner.Tavern.TrinketVinespeakerPortraitHealthActive)
                {
                    AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemHealth, ThornedTrailblazerCardId, null, thornedTrailblazerBonus);
                }
            }

            var inserted = 0;
            for (var repeat = 0; repeat < deathrattleRepeats; repeat += 1)
            {
                inserted += ResolveDeathrattleSummons(context, owner, minion, insertIndex + inserted, newEntityIds, sourceRemoved);
                ResolveTamsinPhylacteryDeathrattle(context, owner, minion);
                inserted += ResolveBrukanEarthDeathrattle(context, owner, minion, insertIndex + inserted, newEntityIds);
                inserted += ResolveBrannsEpicEggDeathrattle(context, owner, minion, insertIndex + inserted, newEntityIds);
                ResolveBattlecruiserCaduceusDeathrattle(context, owner, minion);
                ResolveBanelingDeathrattle(context, owner, minion);
                ResolveSpiritRaptorDeathrattle(context, owner, minion);
            }

            ResolveTrinketDeathrattleTriggered(context, owner, minion, deathrattleRepeats);
            ResolveTimewarpedDeathrattleTriggered(context, owner, minion, deathrattleRepeats);
            return inserted;
        }

        private static void ResolveTamsinPhylacteryDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            if (source?.Tags == null || !source.Tags.Contains(TamsinPhylacteryTag))
            {
                return;
            }

            var attack = Math.Max(0, source.Attack);
            var health = Math.Max(0, source.MaxHealth);
            foreach (var target in owner.Board.Where(minion => IsAlive(minion) && minion.InstanceId != source.InstanceId).ToList())
            {
                BuffMinion(target, attack, health, "Fragrant Phylactery");
            }

            AddLog(context.Log, "DeathrattleResolved", "Fragrant Phylactery shared " + attack + "/" + health + " stats", source.InstanceId, null, LogSeverity.Good);
        }

        private static void ResolveBattlecruiserCaduceusDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            if (source?.Tags == null || !source.Tags.Contains("battlecruiser_caduceus"))
            {
                return;
            }

            var target = owner.Board.FirstOrDefault(IsAlive);
            if (target == null)
            {
                return;
            }

            source.Counters.TryGetValue("battlecruiser_caduceus_attack", out var attack);
            source.Counters.TryGetValue("battlecruiser_caduceus_health", out var health);
            attack = Math.Max(1, attack);
            health = Math.Max(1, health);
            BuffMinion(target, attack, health, "Caduceus Reactor");
            AddLog(context.Log, "DeathrattleResolved", "Caduceus Reactor buffed " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static void ResolveBanelingDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            if (source == null || source.CardId != BanelingCardId)
            {
                return;
            }

            var enemy = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            var targets = enemy.Board.Where(IsAlive).ToList();
            if (targets.Count == 0)
            {
                return;
            }

            var target = new SeededRng(context.Seed + context.AttackSequence * 811 + targets.Count).Pick(targets);
            var damage = Math.Max(1, source.Attack);
            var result = DealDamage(target, damage, false);
            if (!IsAlive(result.Minion))
            {
                MarkKilledBy(result.Minion, source.InstanceId, owner.Side, source.CardId);
            }

            ReplaceByInstanceId(enemy.Board, result.Minion);
            AddLog(context.Log, "DeathrattleResolved", "Baneling dealt " + damage + " to " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static int ResolveBrukanEarthDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            if (source?.Tags == null || !source.Tags.Contains(BrukanEarthDeathrattleTag))
            {
                return 0;
            }

            var token = AddToken(context, owner, source, insertIndex, BrukanElementalTokenId, "Elemental", 1, 1, Tribe.Elemental);
            if (token == null)
            {
                return 0;
            }

            newEntityIds?.Add(token.InstanceId);
            return 1;
        }

        private static int ResolveBrannsEpicEggDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            if (source == null || !string.Equals(source.CardId, BrannsEpicEggCardId, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var count = source.Golden ? 2 : 1;
            owner.Rewards.Add(new CombatReward
            {
                Type = CombatRewardType.AddRandomBattlecryMinionToHand,
                Side = owner.Side,
                SourceCardId = source.CardId,
                SourceInstanceId = source.InstanceId,
                Amount = count,
                TavernTier = owner.Tavern == null ? 0 : owner.Tavern.Tier
            });
            AddLog(context.Log, "CombatRewardQueued", "Brann's Epic Egg queued " + count + " Battlecry minion reward(s)", source.CardId, null, LogSeverity.Good);

            var inserted = 0;
            for (var index = 0; index < count; index += 1)
            {
                var candidates = owner.CombatSummonPool
                    .Where(card => card.Keywords.Contains(Keyword.Battlecry) ||
                                   (card.Text != null && card.Text.IndexOf("Battlecry", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                   (card.Tags != null && card.Tags.Any(tag => tag.IndexOf("battlecry", StringComparison.OrdinalIgnoreCase) >= 0)))
                    .ToList();
                if (owner.Board.Count >= BoardLimit || candidates.Count == 0)
                {
                    break;
                }

                var rng = new SeededRng(context.Seed + context.Replay.Frames.Count * 791 + index);
                var summoned = rng.Pick(candidates).Clone();
                summoned.InstanceId = "branns-epic-egg-" + source.InstanceId + "-" + index;
                summoned.Owner = owner.Side;
                summoned.PoolSource = PoolSource.Summon;
                summoned.OriginPoolSource = PoolSource.Summon;
                summoned.PoolCopiesHeld = 0;
                summoned.CanAttack = true;
                ApplySummonAuras(owner, summoned);
                owner.Board.Insert(Math.Min(insertIndex + inserted, owner.Board.Count), summoned);
                ResolveFriendlySummonTriggers(context, owner, summoned, source);
                newEntityIds?.Add(summoned.InstanceId);
                inserted += 1;
                AddLog(context.Log, "MinionSummoned", "Brann's Epic Egg summoned " + summoned.Name, source.InstanceId, summoned.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.MinionSummoned,
                    "Brann's Epic Egg summoned " + summoned.Name,
                    owner.Side,
                    source.InstanceId,
                    owner.Side,
                    summoned.InstanceId,
                    new[] { source.InstanceId, summoned.InstanceId },
                    null,
                    null,
                    new[] { summoned.InstanceId },
                    new[] { source.InstanceId });
            }

            return inserted;
        }

        private static void ResolveSpiritRaptorDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            if (source?.CardId != SpiritRaptorCardId || source.Tags == null)
            {
                return;
            }

            var elements = source.Tags
                .Where(tag => tag.StartsWith(BrukanSpiritElementTagPrefix, StringComparison.Ordinal))
                .Select(tag => tag.Substring(BrukanSpiritElementTagPrefix.Length))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var element in elements)
            {
                CallBrukanElement(context, owner, element, SpiritRaptorCardId, false);
            }
        }

        private static void ResolveTimewarpedDeathrattleTriggered(CombatContext context, CombatSideState owner, MinionInstance source, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            foreach (var saurolisk in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == TimewarpedSauroliskCardId).ToList())
            {
                var multiplier = saurolisk.Golden ? 2 : 1;
                var attack = 3 * amount * multiplier;
                var health = 2 * amount * multiplier;
                BuffMinion(saurolisk, attack, health, "Timewarped Saurolisk");
                AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, saurolisk.CardId, saurolisk.InstanceId, 1, attack, health, source?.InstanceId);
            }

            foreach (var ghoul in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == TimewarpedGhoulAcabraCardId).ToList())
            {
                var multiplier = ghoul.Golden ? 2 : 1;
                var attack = 3 * amount * multiplier;
                var health = 2 * amount * multiplier;
                foreach (var target in owner.Board.Where(IsAlive).ToList())
                {
                    BuffMinion(target, attack, health, "Timewarped Ghoul-acabra");
                    AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, ghoul.CardId, target.InstanceId, 1, attack, health, source?.InstanceId);
                }
            }
        }

        private static void ResolveTrinketDeathrattleTriggered(CombatContext context, CombatSideState owner, MinionInstance source, int amount)
        {
            var tavern = owner.Tavern;
            if (tavern == null || amount <= 0)
            {
                return;
            }

            ResolveBloodAmuletDeathrattleTriggered(context, owner, source, amount);
            if (tavern.TrinketThornspikePauldronAttack > 0 || tavern.TrinketThornspikePauldronHealth > 0)
            {
                AddReward(
                    context.Log,
                    owner,
                    CombatRewardType.ImproveBloodGemsUntilNextCombat,
                    ThornspikePauldronCardId,
                    null,
                    amount,
                    tavern.TrinketThornspikePauldronAttack,
                    tavern.TrinketThornspikePauldronHealth,
                    source?.InstanceId);
            }

            var attack = tavern.TrinketUnholySanctumAttack;
            var health = tavern.TrinketUnholySanctumHealth;
            var sourceCardId = string.IsNullOrWhiteSpace(tavern.TrinketUnholySanctumSourceCardId)
                ? UnholySanctumCardId
                : tavern.TrinketUnholySanctumSourceCardId;
            if (attack <= 0 && health <= 0)
            {
                return;
            }

            var target = owner.Board.LastOrDefault(IsAlive);
            if (target == null)
            {
                AddLog(context.Log, "TrinketDeathrattleTriggered", "Unholy Sanctum found no right-most minion", sourceCardId, null, LogSeverity.Warning);
                return;
            }

            var totalAttack = StatMath.SaturatingMultiply(Math.Max(0, attack), amount, 0, StatMath.MaxStat);
            var totalHealth = StatMath.SaturatingMultiply(Math.Max(0, health), amount, 0, StatMath.MaxStat);
            BuffMinion(target, totalAttack, totalHealth, "Unholy Sanctum");
            AddTargetedReward(
                context.Log,
                owner,
                CombatRewardType.BuffOriginalFriendlyMinion,
                sourceCardId,
                target.InstanceId,
                amount,
                attack,
                health,
                source?.InstanceId);
            AddLog(context.Log, "TrinketDeathrattleTriggered", "Unholy Sanctum buffed " + target.InstanceId + " +" + totalAttack + "/+" + totalHealth, sourceCardId, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DeathrattleResolved,
                "Unholy Sanctum buffed " + target.InstanceId,
                owner.Side,
                sourceCardId,
                owner.Side,
                target.InstanceId,
                new[] { source?.InstanceId, target.InstanceId, sourceCardId },
                null,
                null,
                null,
                new[] { sourceCardId });
        }

        private static void ResolveBloodAmuletDeathrattleTriggered(CombatContext context, CombatSideState owner, MinionInstance source, int amount)
        {
            var tavern = owner.Tavern;
            if (tavern == null || !tavern.TrinketBloodAmuletActive || amount <= 0)
            {
                return;
            }

            var gemAttack = 1 + Math.Max(0, tavern.BloodGemBonusAttack);
            var gemHealth = 1 + Math.Max(0, tavern.BloodGemBonusHealth);
            var targetIds = new List<string>();
            for (var repeat = 0; repeat < amount; repeat += 1)
            {
                var candidates = owner.Board
                    .Where(IsAlive)
                    .ToList();
                var targetCount = Math.Min(3, candidates.Count);
                if (targetCount <= 0)
                {
                    break;
                }

                var rng = new SeededRng(
                    context.Seed +
                    context.AttackSequence * 997 +
                    context.Replay.Frames.Count * 53 +
                    repeat * 31 +
                    candidates.Count * 17 +
                    gemAttack * 5 +
                    gemHealth * 7);
                for (var index = 0; index < targetCount; index += 1)
                {
                    var target = rng.Pick(candidates);
                    candidates.Remove(target);
                    ApplyBloodGem(target, tavern);
                    AddTargetedReward(
                        context.Log,
                        owner,
                        CombatRewardType.BuffOriginalFriendlyMinion,
                        BloodAmuletCardId,
                        target.InstanceId,
                        1,
                        gemAttack,
                        gemHealth,
                        source?.InstanceId);
                    targetIds.Add(target.InstanceId);
                }
            }

            if (targetIds.Count == 0)
            {
                AddLog(context.Log, "TrinketDeathrattleTriggered", "Blood Amulet found no friendly minions for Blood Gems", BloodAmuletCardId, source?.InstanceId, LogSeverity.Warning);
                return;
            }

            AddLog(context.Log, "TrinketDeathrattleTriggered", "Blood Amulet played " + targetIds.Count + " permanent Blood Gem(s)", BloodAmuletCardId, targetIds.FirstOrDefault(), LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DeathrattleResolved,
                "Blood Amulet played " + targetIds.Count + " permanent Blood Gem(s)",
                owner.Side,
                BloodAmuletCardId,
                owner.Side,
                targetIds.FirstOrDefault(),
                targetIds.Concat(new[] { source?.InstanceId, BloodAmuletCardId }),
                null,
                null,
                null,
                targetIds,
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                false,
                targetIds.Count,
                Math.Max(1, amount * 3));
        }

        private static int ResolveDeathrattleSummons(CombatContext context, CombatSideState owner, MinionInstance minion, int insertIndex, List<string> newEntityIds, bool sourceRemoved = true)
        {
            var inserted = 0;
            inserted += ResolveTrinketCounterDeathrattles(context, owner, minion, insertIndex, newEntityIds);
            inserted += ResolveSneedSummonDeathrattle(context, owner, minion, insertIndex + inserted, newEntityIds);
            switch (minion.CardId)
            {
                case ImpulsiveTricksterCardId:
                    ResolveImpulsivePortraitDeathrattle(context, owner, minion, insertIndex);
                    break;
                case KaboomBotCardId:
                    ResolveKaboomBotPortraitDeathrattle(context, owner, minion);
                    break;
                case FishOfNzothCardId:
                case HeroFishOfNzothCardId:
                    inserted += ResolveFishOfNzothCopiedDeathrattles(context, owner, minion, insertIndex + inserted, newEntityIds);
                    break;
                case PilotedWhirlOTronCardId:
                    TriggerLeftmostOtherDeathrattleFromDeathrattle(context, owner, minion, insertIndex + inserted, newEntityIds);
                    break;
                case TimewarpedWhirlOTronCardId:
                    inserted += ResolveTimewarpedWhirlOTronCopiedDeathrattles(context, owner, minion, insertIndex + inserted, newEntityIds);
                    break;
                case CordPullerCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "microbot", "Microbot", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Mech);
                    break;
                case HarmlessBoneheadCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "skeleton", "Skeleton", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead);
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "skeleton", "Skeleton", minion.Golden ? 2 : 1, minion.Golden ? 2 : 1, Tribe.Undead);
                    break;
                case ManasaberCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "cubling", "Cubling", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt);
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "cubling", "Cubling", 0, minion.Golden ? 2 : 1, Tribe.Beast, Keyword.Taunt);
                    break;
                case BuzzingVerminCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "beetle", "Beetle", 2 + (owner.Tavern?.BeetleAttackBonus ?? 0), 2 + (owner.Tavern?.BeetleHealthBonus ?? 0), Tribe.Beast);
                    if (minion.Golden)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "beetle", "Beetle", 2 + (owner.Tavern?.BeetleAttackBonus ?? 0), 2 + (owner.Tavern?.BeetleHealthBonus ?? 0), Tribe.Beast);
                    }

                    break;
                case TwilightHatchlingCardId:
                    inserted += AddImmediateAttackHatchling(context, owner, minion, insertIndex + inserted, newEntityIds) == null ? 0 : 1;
                    if (minion.Golden)
                    {
                        inserted += AddImmediateAttackHatchling(context, owner, minion, insertIndex + inserted, newEntityIds) == null ? 0 : 1;
                    }

                    break;
                case ForestRoverCardId:
                    inserted += AddTokenAndTrack(
                        context,
                        owner,
                        minion,
                        insertIndex + inserted,
                        newEntityIds,
                        "beetle",
                        "Beetle",
                        (minion.Golden ? 4 : 2) + (owner.Tavern?.BeetleAttackBonus ?? 0),
                        (minion.Golden ? 4 : 2) + (owner.Tavern?.BeetleHealthBonus ?? 0),
                        Tribe.Beast);
                    break;
                case GlowgulletWarlordCardId:
                    inserted += AddBloodGemToken(context, owner, minion, insertIndex + inserted, newEntityIds);
                    inserted += AddBloodGemToken(context, owner, minion, insertIndex + inserted, newEntityIds);
                    if (minion.Golden)
                    {
                        inserted += AddBloodGemToken(context, owner, minion, insertIndex + inserted, newEntityIds);
                        inserted += AddBloodGemToken(context, owner, minion, insertIndex + inserted, newEntityIds);
                    }

                    break;
                case ScarletSkullCardId:
                    BuffFirstFriendly(owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Undead)), minion.Golden ? 2 : 1, minion.Golden ? 4 : 2, "Scarlet Skull");
                    break;
                case AlertAlarmistCardId:
                    AddReward(context.Log, owner, CombatRewardType.TavernSpellCostReduction, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case BristlebackBullyCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, minion.CardId, BristlebackBloodGemCardId, minion.Golden ? 2 : 1);
                    break;
                case MetallicHunterCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, minion.CardId, PointyArrowCardId, minion.Golden ? 2 : 1);
                    break;
                case TideRaiserCardId:
                    ResolveTideRaiser(context, owner, insertIndex, minion.Golden);
                    break;
                case HandlessForsakenCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "reborn-hand", "Reborn Hand", 2, 1, Tribe.Undead, Keyword.Reborn);
                    if (minion.Golden)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "reborn-hand", "Reborn Hand", 2, 1, Tribe.Undead, Keyword.Reborn);
                    }

                    break;
                case BoneWatcherCardId:
                    var skeletonCount = minion.Golden ? 6 : 3;
                    for (var summonIndex = 0; summonIndex < skeletonCount; summonIndex += 1)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "skeleton", "Skeleton", 1, 1, Tribe.Undead);
                    }

                    break;
                case SlyRaptorCardId:
                    inserted += AddTokenAndTrack(
                        context,
                        owner,
                        minion,
                        insertIndex + inserted,
                        newEntityIds,
                        "summoned-beast",
                        "Summoned Beast",
                        minion.Golden ? 12 : 6,
                        minion.Golden ? 12 : 6,
                        Tribe.Beast);
                    break;
                case GemDayPiperCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemAttack, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case TreasureSeekerCardId:
                    AddReward(context.Log, owner, CombatRewardType.BuffHandMinion, minion.CardId, null, 1, minion.Golden ? 14 : 7, minion.Golden ? 14 : 7);
                    break;
                case MummifierCardId:
                    GiveDifferentUndeadReborn(context, owner, minion);
                    break;
                case BuriedBristlebackCardId:
                    ApplyBloodGemsToAdjacent(owner, insertIndex, minion.Golden ? 2 : 1);
                    break;
                case RadiantEmberCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveElementalHealth, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case ColdlightDiverCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, "104436", minion.Golden ? 2 : 1);
                    break;
                case JuvenileWaveCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveRefreshBuff, minion.CardId, null, 1, minion.Golden ? 6 : 3, minion.Golden ? 6 : 3);
                    break;
                case HeavyMetalWyrmCardId:
                    TriggerAdjacentBattlecryResources(context, owner, minion, sourceRemoved ? insertIndex : Math.Max(0, insertIndex - 1), sourceRemoved);
                    break;
                case SilentSwarmguardCardId:
                    DealAreaDamage(context, owner, minion, minion.Golden ? 4 : 2, candidate => !(candidate.Owner == owner.Side && candidate.Tribes.Contains(Tribe.Demon)));
                    break;
                case TunnelBlasterCardId:
                    DealAreaDamage(context, owner, minion, minion.Golden ? 6 : 3, candidate => true);
                    break;
                case TwilightNestmatronCardId:
                    var twilightCount = minion.Golden ? 4 : 2;
                    for (var summonIndex = 0; summonIndex < twilightCount; summonIndex += 1)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "twilight-hatchling", "Twilight Hatchling", 1, 1, Tribe.Dragon, Keyword.Taunt);
                    }

                    break;
                case AutoAssemblerCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "ancestral-automaton", "Ancestral Automaton", minion.Golden ? 6 : 3, minion.Golden ? 8 : 4, Tribe.Mech);
                    break;
                case PlaguedGhoulCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveUndeadAttack, minion.CardId, null, minion.Golden ? 4 : 2);
                    break;
                case FriendlyFelboarCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveTavernSpellAttack, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case DeepwaterChieftainCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, minion.CardId, DeepwaterSchoolCardId, minion.Golden ? 2 : 1);
                    break;
                case ManasparkCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, minion.CardId, ArcaneConsumptionCardId, minion.Golden ? 2 : 1);
                    break;
                case KangorsApprenticeCardId:
                    inserted += SummonDeadMechPlainCopies(context, owner, minion, insertIndex + inserted, newEntityIds, minion.Golden ? 4 : 2);
                    break;
                case LeeroyTheRecklessCardId:
                    DestroyKiller(context, owner, minion);
                    break;
                case BarrensConjurerCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomBattlecryMinionToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case WintergraspGhoulCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, DisturbedGraveCardNumber, minion.Golden ? 2 : 1);
                    break;
                case FarmhandWhirlOMatronCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveElementalShopStats, minion.CardId, null, minion.Golden ? 16 : 8);
                    break;
                case NightmareParlorGuestCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, MenagerieTablewareCardNumber, minion.Golden ? 2 : 1);
                    break;
                case ShadowdancerCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, StaffOfEnrichmentCardNumber, minion.Golden ? 2 : 1);
                    break;
                case ScrapScraperCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomMagneticMechToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case ClunkerJunkerCardId:
                    BuffAll(owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Mech)), minion.Golden ? 4 : 2, 0, "Clunker Junker");
                    break;
                case HolyMecherelCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, minion.CardId, SacredGiftCardNumber, minion.Golden ? 2 : 1);
                    break;
                case ShipwreckedCaptainCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddBountyToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case SewerRatPackCardId:
                    var ratCount = minion.Golden ? 4 : 2;
                    for (var summonIndex = 0; summonIndex < ratCount; summonIndex += 1)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "sewer-rat", "Sewer Rat", 2, 3, Tribe.Beast, Keyword.Taunt);
                    }

                    break;
                case BristlebachCardId:
                    BuffAll(owner.Board, 0, minion.Golden ? 2 : 1, "Bristlebach");
                    DealAreaDamage(context, owner, minion, 1, candidate => candidate.Owner == owner.Side);
                    break;
                case TurquoiseSkittererCardId:
                    if (owner.Tavern != null)
                    {
                        owner.Tavern.BeetleAttackBonus += minion.Golden ? 10 : 5;
                        owner.Tavern.BeetleHealthBonus += minion.Golden ? 10 : 5;
                    }

                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "beetle", "Beetle", 2 + (owner.Tavern?.BeetleAttackBonus ?? 0), 2 + (owner.Tavern?.BeetleHealthBonus ?? 0), Tribe.Beast);
                    break;
                case ThreeLilQuilboarCardId:
                    foreach (var quilboar in owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Quilboar)).ToList())
                    {
                        BuffMinion(quilboar, minion.Golden ? 6 : 3, minion.Golden ? 6 : 3, "Three Lil' Quilboar");
                    }

                    break;
                case DragonCaretakerCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomChromawhelpToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case EternalSummonerHighCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "eternal-knight", "Eternal Knight", minion.Golden ? 8 : 4, minion.Golden ? 2 : 1, Tribe.Undead, Keyword.Taunt);
                    break;
                case SilkyShimmermothCardId:
                    if (owner.Tavern != null)
                    {
                        owner.Tavern.BeetleAttackBonus += minion.Golden ? 4 : 2;
                        owner.Tavern.BeetleHealthBonus += minion.Golden ? 2 : 1;
                    }

                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "beetle", "Beetle", 2 + (owner.Tavern?.BeetleAttackBonus ?? 0), 2 + (owner.Tavern?.BeetleHealthBonus ?? 0), Tribe.Beast);
                    break;
                case DeadlySporebatCardId:
                    inserted += SummonLeftmostHandMinionForCombat(context, owner, minion, insertIndex + inserted, newEntityIds);
                    break;
                case BassgillCardId:
                    inserted += SummonHighestHealthMurlocsFromHand(context, owner, minion, insertIndex + inserted, newEntityIds, minion.Golden ? 2 : 1);
                    break;
                case TimewarpedBassgillCardId:
                    inserted += SummonHighestHealthMinionsFromHand(context, owner, minion, insertIndex + inserted, newEntityIds, minion.Golden ? 2 : 1, true);
                    break;
                case TimewarpedScourfinCardId:
                    inserted += ResolveTimewarpedScourfinDeathrattle(context, owner, minion, insertIndex + inserted, newEntityIds);
                    break;
                case TimewarpedFestergutCardId:
                    inserted += ResolveTimewarpedSummonAndGetTribe(
                        context,
                        owner,
                        minion,
                        insertIndex + inserted,
                        newEntityIds,
                        Tribe.Undead,
                        minion.Golden ? 2 : 1,
                        "undead-creation",
                        "Undead Creation",
                        3,
                        3);
                    break;
                case TimewarpedNelliesShipCardId:
                    inserted += ResolveTimewarpedSummonAndGetTribe(
                        context,
                        owner,
                        minion,
                        insertIndex + inserted,
                        newEntityIds,
                        Tribe.Pirate,
                        minion.Golden ? 2 : 1,
                        "nellie-pirate",
                        "Nellie's Pirate",
                        3,
                        3);
                    break;
                case TimewarpedJazzerCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemHealth, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case TimewarpedLilQuilboarCardId:
                    PlayBloodGemsOnFriendlyQuilboar(owner, minion.Golden ? 6 : 3);
                    break;
                case TimewarpedGeistCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveTavernSpellStats, minion.CardId, null, minion.Golden ? 2 : 1, 2, 2);
                    break;
                case TimewarpedCaretakerCardId:
                    inserted += ResolveTimewarpedCaretakerDeathrattle(context, owner, minion, insertIndex + inserted, newEntityIds);
                    break;
                case TimewarpedIckyImpCardId:
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "timewarped-imp", "Imp", Math.Max(0, minion.Attack), Math.Max(1, minion.MaxHealth), Tribe.Demon);
                    inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "timewarped-imp", "Imp", Math.Max(0, minion.Attack), Math.Max(1, minion.MaxHealth), Tribe.Demon);
                    if (minion.Golden)
                    {
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "timewarped-imp", "Imp", Math.Max(0, minion.Attack), Math.Max(1, minion.MaxHealth), Tribe.Demon);
                        inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "timewarped-imp", "Imp", Math.Max(0, minion.Attack), Math.Max(1, minion.MaxHealth), Tribe.Demon);
                    }

                    break;
                case TimewarpedNestSwarmerCardId:
                    inserted += ResolveTimewarpedNestSwarmer(context, owner, minion, insertIndex + inserted, newEntityIds);
                    break;
                case TimewarpedTideRazorCardId:
                    inserted += ResolveTimewarpedSummonAndGetTribe(
                        context,
                        owner,
                        minion,
                        insertIndex + inserted,
                        newEntityIds,
                        Tribe.Pirate,
                        4 * (minion.Golden ? 2 : 1),
                        "tide-razor-pirate",
                        "Tide Razor Pirate",
                        3,
                        3);
                    break;
                case TimewarpedWarghoulCardId:
                    inserted += TriggerAdjacentDeathrattleExceptWarghoul(context, owner, minion, insertIndex + inserted, newEntityIds, sourceRemoved);
                    break;
                case TimewarpedLeapfroggerCardId:
                    ResolveTimewarpedLeapfroggerDeathrattle(context, owner, minion);
                    break;
                case TimewarpedRadioStarCardId:
                    QueueTimewarpedRadioStarReward(context, owner, minion);
                    break;
                case QueenGuardCardId:
                    BuffAll(owner.Board, minion.Golden ? 4 : 2, minion.Golden ? 4 : 2, "Queen's Command");
                    BuffAll(owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Naga)), minion.Golden ? 4 : 2, minion.Golden ? 4 : 2, "Queen's Command Naga");
                    break;
                case BloodChampionCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemAttack, minion.CardId, null, minion.Golden ? 2 : 1);
                    AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemHealth, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case SargerasChampionCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveTavernMinionStats, minion.CardId, null, minion.Golden ? 10 : 5);
                    break;
                case RheaSupremeWardenCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomTierSixMinionToHand, minion.CardId, null, minion.Golden ? 2 : 1);
                    break;
                case GoldrinnCardId:
                    BuffAll(owner.Board.Where(candidate => candidate.Tribes.Contains(Tribe.Beast)), minion.Golden ? 16 : 8, minion.Golden ? 16 : 8, "Goldrinn");
                    break;
                case StitchedReclaimerCardId:
                    foreach (var stored in owner.StitchedCopies.Where(pair => pair.Key.StartsWith(minion.InstanceId + ":", StringComparison.Ordinal)).Select(pair => pair.Value).ToList())
                    {
                        if (owner.Board.Count >= BoardLimit)
                        {
                            break;
                        }

                        var copy = stored.Clone();
                        copy.InstanceId = "stitched-summon-" + minion.InstanceId + "-" + newEntityIds.Count;
                        ApplySummonAuras(owner, copy);
                        owner.Board.Insert(Math.Min(insertIndex + inserted, owner.Board.Count), copy);
                        ResolveFriendlySummonTriggers(context, owner, copy, minion);
                        newEntityIds.Add(copy.InstanceId);
                        inserted += 1;
                    }

                    break;
            }

            if (minion.Tags.Contains("surf_n_surf_crab"))
            {
                var attack = minion.Counters.TryGetValue("surf_crab_attack", out var storedAttack) ? storedAttack : 3;
                var health = minion.Counters.TryGetValue("surf_crab_health", out var storedHealth) ? storedHealth : 2;
                inserted += AddTokenAndTrack(context, owner, minion, insertIndex + inserted, newEntityIds, "crab", "Crab", attack, health, Tribe.Beast);
            }

            return inserted;
        }

        private static void ResolveImpulsivePortraitDeathrattle(CombatContext context, CombatSideState owner, MinionInstance minion, int insertIndex)
        {
            if (owner.Tavern == null || !owner.Tavern.TrinketImpulsivePortraitActive)
            {
                return;
            }

            var health = Math.Max(0, minion.MaxHealth);
            if (health <= 0)
            {
                return;
            }

            var targets = new List<MinionInstance>();
            var leftIndex = insertIndex - 1;
            if (leftIndex >= 0 && leftIndex < owner.Board.Count && IsAlive(owner.Board[leftIndex]))
            {
                targets.Add(owner.Board[leftIndex]);
            }

            if (insertIndex >= 0 && insertIndex < owner.Board.Count && IsAlive(owner.Board[insertIndex]))
            {
                targets.Add(owner.Board[insertIndex]);
            }

            foreach (var target in targets)
            {
                BuffMinion(target, 0, health, "Impulsive Portrait");
            }

            if (targets.Count > 0)
            {
                AddLog(context.Log, "DeathrattleResolved", minion.InstanceId + " gave Health to adjacent minions", minion.InstanceId, targets.First().InstanceId, LogSeverity.Good);
            }
        }

        private static void ResolveKaboomBotPortraitDeathrattle(CombatContext context, CombatSideState owner, MinionInstance minion)
        {
            if (owner.Tavern == null || !owner.Tavern.TrinketKaboomBotPortraitActive)
            {
                return;
            }

            var enemy = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            var candidates = enemy.Board.Where(IsAlive).ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var amount = minion.Golden ? 16 : 8;
            var rng = new SeededRng(context.Seed + context.AttackSequence * 997 + context.Replay.Frames.Count * 37 + candidates.Count);
            var target = rng.Pick(candidates);
            var result = DealDamage(target, amount, false);
            ReplaceByInstanceId(enemy.Board, result.Minion);
            if (result.Minion.Health <= 0)
            {
                MarkKilledBy(result.Minion, minion.InstanceId, owner.Side, minion.CardId);
            }

            AddLog(context.Log, "DeathrattleResolved", minion.InstanceId + " dealt " + amount + " Kaboom Bot Portrait damage to " + target.InstanceId, minion.InstanceId, target.InstanceId, LogSeverity.Good);
            ResolveDeaths(context, enemy.Side);
        }

        private static int ResolveTrinketCounterDeathrattles(CombatContext context, CombatSideState owner, MinionInstance minion, int insertIndex, List<string> newEntityIds)
        {
            if (minion?.Counters == null)
            {
                return 0;
            }

            var inserted = 0;
            inserted += ResolveJarredFrostlingDeathrattle(context, owner, minion, insertIndex + inserted, newEntityIds, GetCounter(minion, JarredFrostlingCounter));
            inserted += ResolvePowderKegDeathrattle(context, owner, minion, insertIndex + inserted, newEntityIds, GetCounter(minion, PowderKegCounter));
            ResolveHoggyBankDeathrattle(context, owner, minion, GetCounter(minion, HoggyBankCounter));
            ResolveRustyTridentDeathrattle(context, owner, minion, GetCounter(minion, RustyTridentCounter));
            ResolveSkyGolemPortraitDeathrattle(context, owner, minion, GetCounter(minion, SkyGolemCounter));
            return inserted;
        }

        private static int ResolveJarredFrostlingDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds, int count)
        {
            var inserted = 0;
            for (var index = 0; index < count; index += 1)
            {
                var token = AddToken(context, owner, source, insertIndex + inserted, "flourishing-frostling", "Flourishing Frostling", 2, 2, Tribe.Elemental);
                if (token == null)
                {
                    break;
                }

                token.CardId = FlourishingFrostlingTokenId;
                newEntityIds?.Add(token.InstanceId);
                inserted += 1;
            }

            return inserted;
        }

        private static int ResolvePowderKegDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds, int count)
        {
            var inserted = 0;
            for (var index = 0; index < count; index += 1)
            {
                var token = AddToken(
                    context,
                    owner,
                    source,
                    insertIndex + inserted,
                    "powder-keg-sky-pirate",
                    "Sky Pirate",
                    StatMath.SaturatingAdd(
                        StatMath.SaturatingAdd(1, Math.Max(0, source.Attack), 0, StatMath.MaxStat),
                        owner.Tavern?.TrinketSkyPirateAttackBonus ?? 0,
                        0,
                        StatMath.MaxStat),
                    1,
                    Tribe.Pirate);
                if (token == null)
                {
                    break;
                }

                token.CardId = SkyPirateCardId;
                newEntityIds?.Add(token.InstanceId);
                context.ImmediateAttacks.Enqueue(new ImmediateAttackRequest(owner.Side, token.InstanceId));
                AddLog(context.Log, "ImmediateAttackQueued", token.InstanceId + " queued by Powder Keg", token.InstanceId, null, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.ImmediateAttackQueued,
                    token.InstanceId + " queued by Powder Keg",
                    owner.Side,
                    token.InstanceId,
                    owner.Side,
                    null,
                    new[] { source.InstanceId, token.InstanceId, PowderKegCardId },
                    null,
                    null,
                    null,
                    new[] { PowderKegCardId });
                inserted += 1;
            }

            return inserted;
        }

        private static void ResolveHoggyBankDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int count)
        {
            if (count <= 0)
            {
                return;
            }

            AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, HoggyBankCardId, BristlebackBloodGemCardId, count * 2, source?.InstanceId);
        }

        private static void ResolveRustyTridentDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int count)
        {
            if (count <= 0)
            {
                return;
            }

            AddReward(context.Log, owner, CombatRewardType.AddRandomSpellcraftSpellToHand, RustyTridentCardId, null, count, source?.InstanceId);
        }

        private static void ResolveSkyGolemPortraitDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int count)
        {
            if (count <= 0)
            {
                return;
            }

            var targets = owner.Board.Where(IsAlive).ToList();
            foreach (var target in targets)
            {
                var attack = StatMath.SaturatingMultiply(2, count, 0, StatMath.MaxStat);
                var health = StatMath.SaturatingMultiply(2, count, 0, StatMath.MaxStat);
                BuffMinion(target, attack, health, "Sky Golem Portrait");
                AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, SkyGolemPortraitCardId, target.InstanceId, 1, attack, health, source?.InstanceId);
            }

            if (targets.Count > 0)
            {
                AddLog(context.Log, "TrinketDeathrattleTriggered", "Sky Golem Portrait permanently buffed " + targets.Count + " minion(s)", SkyGolemPortraitCardId, source?.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.TrinketTriggered,
                    "Sky Golem Portrait permanently buffed " + targets.Count + " minion(s)",
                    owner.Side,
                    SkyGolemPortraitCardId,
                    owner.Side,
                    targets.FirstOrDefault()?.InstanceId,
                    targets.Select(target => target.InstanceId).Concat(new[] { SkyGolemPortraitCardId }),
                    null,
                    null,
                    null,
                    targets.Select(target => target.InstanceId),
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    false,
                    count,
                    1);
            }
        }

        private static int GetCounter(MinionInstance minion, string key)
        {
            return minion?.Counters != null && minion.Counters.TryGetValue(key, out var value)
                ? Math.Max(0, value)
                : 0;
        }

        private static void CopyDeathrattleToFishOfNzoth(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            if (source == null || IsFishOfNzoth(source))
            {
                return;
            }

            var fish = owner.Board.FirstOrDefault(minion =>
                IsAlive(minion) &&
                IsFishOfNzoth(minion));
            if (fish == null)
            {
                return;
            }

            var copy = source.Clone();
            copy.InstanceId = "fish-copy-" + owner.FishyStickerCopiedDeathrattles.Count + "-" + source.InstanceId;
            copy.Owner = owner.Side;
            copy.PoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            owner.FishyStickerCopiedDeathrattles.Add(copy);
            AddLog(context.Log, "TrinketDeathrattleTriggered", "Fish of N'Zoth copied " + source.InstanceId + " deathrattle", fish.InstanceId, source.InstanceId, LogSeverity.Good);
        }

        private static int ResolveRedemptionSecret(CombatContext context, CombatSideState owner, MinionInstance dead, int insertIndex, List<string> newEntityIds)
        {
            if (dead == null || owner.Board.Count >= BoardLimit)
            {
                return 0;
            }

            var better = TryTriggerSecret(owner.Tavern, BetterRedemptionSecretId);
            var normal = better ? false : TryTriggerSecret(owner.Tavern, RedemptionSecretId);
            if (!better && !normal)
            {
                return 0;
            }

            var copy = dead.Clone();
            copy.InstanceId = (better ? "better-redemption-" : "redemption-") + dead.InstanceId + "-" + context.Replay.Frames.Count;
            copy.Owner = owner.Side;
            copy.Health = better ? Math.Max(1, copy.MaxHealth) : 1;
            copy.PoolSource = PoolSource.Summon;
            copy.OriginPoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            ApplySummonAuras(owner, copy);
            owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), copy);
            ResolveFriendlySummonTriggers(context, owner, copy, dead);
            newEntityIds?.Add(copy.InstanceId);
            AddLog(
                context.Log,
                "SecretTriggered",
                (better ? "Better Redemption" : "Redemption") + " revived " + dead.InstanceId,
                better ? BetterRedemptionSecretId : RedemptionSecretId,
                copy.InstanceId,
                LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                (better ? "Better Redemption" : "Redemption") + " revived " + dead.InstanceId,
                owner.Side,
                better ? BetterRedemptionSecretId : RedemptionSecretId,
                owner.Side,
                copy.InstanceId,
                new[] { dead.InstanceId, copy.InstanceId },
                null,
                null,
                new[] { copy.InstanceId },
                new[] { better ? BetterRedemptionSecretId : RedemptionSecretId });
            return 1;
        }

        private static int ResolveDeathSecretTriggers(CombatContext context, CombatSideState owner, MinionInstance dead, int insertIndex, List<string> newEntityIds)
        {
            if (owner == null || dead == null)
            {
                return 0;
            }

            var inserted = 0;
            if (TryTriggerSecret(owner.Tavern, EffigySecretId))
            {
                inserted += ResolveEffigySecret(context, owner, dead, insertIndex + inserted, newEntityIds);
            }

            if (TryTriggerSecret(owner.Tavern, AvengeSecretId))
            {
                var targets = owner.Board.Where(IsAlive).ToList();
                if (targets.Count > 0)
                {
                    var target = new SeededRng(context.Seed + context.Replay.Frames.Count * 313 + targets.Count).Pick(targets);
                    BuffMinion(target, 3, 2, "Avenge Secret");
                    AddLog(context.Log, "SecretTriggered", "Avenge gave +3/+2 to " + target.InstanceId, AvengeSecretId, target.InstanceId, LogSeverity.Good);
                }
                else
                {
                    AddLog(context.Log, "SecretTriggered", "Avenge had no friendly minion to buff", AvengeSecretId, dead.InstanceId, LogSeverity.Warning);
                }
            }

            owner.AvengeCounters.TryGetValue(HandOfSalvationDeathsCounter, out var deaths);
            deaths += 1;
            owner.AvengeCounters[HandOfSalvationDeathsCounter] = deaths;
            if (deaths >= 2 && TryTriggerSecret(owner.Tavern, HandOfSalvationSecretId))
            {
                inserted += ReviveSecretMinion(context, owner, dead, insertIndex + inserted, newEntityIds, HandOfSalvationSecretId, "Hand of Salvation", false);
            }

            return inserted;
        }

        private static int ResolveEffigySecret(CombatContext context, CombatSideState owner, MinionInstance dead, int insertIndex, List<string> newEntityIds)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                RecordSummonOverflow(context, owner, dead, dead.CardId, dead.Name);
                AddLog(context.Log, "SecretTriggered", "Effigy triggered but the board was full", EffigySecretId, dead.InstanceId, LogSeverity.Warning);
                return 0;
            }

            var candidates = owner.CombatSummonPool
                .Where(card => card != null && card.CardKind == CardKind.Minion && card.TavernTier == dead.TavernTier)
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = owner.CombatSummonPool
                    .Where(card => card != null && card.CardKind == CardKind.Minion)
                    .ToList();
            }

            if (candidates.Count == 0)
            {
                AddLog(context.Log, "SecretTriggered", "Effigy found no summon candidate", EffigySecretId, dead.InstanceId, LogSeverity.Warning);
                return 0;
            }

            var summoned = new SeededRng(context.Seed + context.Replay.Frames.Count * 719 + dead.InstanceId.Length)
                .Pick(candidates)
                .Clone();
            summoned.InstanceId = "effigy-" + dead.InstanceId + "-" + context.Replay.Frames.Count;
            summoned.Owner = owner.Side;
            summoned.PoolSource = PoolSource.Summon;
            summoned.OriginPoolSource = PoolSource.Summon;
            summoned.PoolCopiesHeld = 0;
            summoned.CanAttack = true;
            summoned.AttacksThisCombat = 0;
            RemoveKillTags(summoned);
            summoned.Health = Math.Max(1, summoned.MaxHealth);
            ApplySummonAuras(owner, summoned);
            owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), summoned);
            ResolveFriendlySummonTriggers(context, owner, summoned, dead);
            newEntityIds?.Add(summoned.InstanceId);
            AddLog(context.Log, "SecretTriggered", "Effigy summoned " + summoned.InstanceId, EffigySecretId, summoned.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                "Effigy summoned " + summoned.InstanceId,
                owner.Side,
                EffigySecretId,
                owner.Side,
                summoned.InstanceId,
                new[] { EffigySecretId, dead.InstanceId, summoned.InstanceId },
                null,
                null,
                new[] { summoned.InstanceId },
                new[] { EffigySecretId, dead.InstanceId });
            return 1;
        }

        private static int ReviveSecretMinion(
            CombatContext context,
            CombatSideState owner,
            MinionInstance dead,
            int insertIndex,
            List<string> newEntityIds,
            string secretId,
            string secretName,
            bool fullHealth)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                RecordSummonOverflow(context, owner, dead, dead.CardId, dead.Name);
                AddLog(context.Log, "SecretTriggered", secretName + " triggered but the board was full", secretId, dead.InstanceId, LogSeverity.Warning);
                return 0;
            }

            var copy = dead.Clone();
            copy.InstanceId = secretName.ToLowerInvariant().Replace(" ", "-") + "-" + dead.InstanceId + "-" + context.Replay.Frames.Count;
            copy.Owner = owner.Side;
            copy.Health = fullHealth ? Math.Max(1, copy.MaxHealth) : 1;
            copy.PoolSource = PoolSource.Summon;
            copy.OriginPoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            copy.AttacksThisCombat = 0;
            RemoveKillTags(copy);
            ApplySummonAuras(owner, copy);
            owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), copy);
            ResolveFriendlySummonTriggers(context, owner, copy, dead);
            newEntityIds?.Add(copy.InstanceId);
            AddLog(context.Log, "SecretTriggered", secretName + " revived " + dead.InstanceId, secretId, copy.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                secretName + " revived " + dead.InstanceId,
                owner.Side,
                secretId,
                owner.Side,
                copy.InstanceId,
                new[] { dead.InstanceId, copy.InstanceId },
                null,
                null,
                new[] { copy.InstanceId },
                new[] { secretId });
            return 1;
        }

        private static bool IsFishOfNzoth(MinionInstance minion)
        {
            return minion != null &&
                   (string.Equals(minion.CardId, FishOfNzothCardId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(minion.CardId, HeroFishOfNzothCardId, StringComparison.OrdinalIgnoreCase) ||
                    (minion.Tags != null && minion.Tags.Contains(FishyStickerFishTag)));
        }

        private static bool TryTriggerSecret(TavernState tavern, string secretCardId)
        {
            if (tavern?.Secrets == null || string.IsNullOrEmpty(secretCardId))
            {
                return false;
            }

            var index = tavern.Secrets.FindIndex(secret =>
                secret != null &&
                !secret.Triggered &&
                string.Equals(secret.SecretCardId, secretCardId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return false;
            }

            tavern.Secrets[index].Triggered = true;
            tavern.Secrets.RemoveAt(index);
            return true;
        }

        private static int ResolveFishOfNzothCopiedDeathrattles(CombatContext context, CombatSideState owner, MinionInstance fish, int insertIndex, List<string> newEntityIds)
        {
            var inserted = 0;
            var copies = owner.FishyStickerCopiedDeathrattles.ToList();
            if (copies.Count == 0)
            {
                return 0;
            }

            var repeats = fish.Golden ? 2 : 1;
            for (var repeat = 0; repeat < repeats; repeat += 1)
            {
                for (var index = 0; index < copies.Count; index += 1)
                {
                    var source = copies[index].Clone();
                    source.InstanceId = fish.InstanceId + "-copied-" + repeat + "-" + index + "-" + source.InstanceId;
                    source.Owner = owner.Side;
                    AddLog(context.Log, "DeathrattleResolved", "Fish of N'Zoth triggered copied " + source.CardId + " deathrattle", fish.InstanceId, source.InstanceId, LogSeverity.Good);
                    inserted += ResolveDeathrattleSummons(context, owner, source, insertIndex + inserted, newEntityIds, false);
                }
            }

            return inserted;
        }

        private static int ResolveTimewarpedWhirlOTronCopiedDeathrattles(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            if (source == null ||
                !owner.CopiedDeathrattlesBySource.TryGetValue(source.InstanceId, out var copies) ||
                copies == null ||
                copies.Count == 0)
            {
                return 0;
            }

            var inserted = 0;
            var repeats = source.Golden ? 2 : 1;
            for (var repeat = 0; repeat < repeats; repeat += 1)
            {
                for (var index = 0; index < copies.Count; index += 1)
                {
                    var copy = copies[index].Clone();
                    copy.InstanceId = source.InstanceId + "-whirl-copy-" + repeat + "-" + index + "-" + copy.InstanceId;
                    copy.Owner = owner.Side;
                    AddLog(context.Log, "DeathrattleResolved", "Timewarped Whirl-O-Tron triggered copied " + copy.CardId + " deathrattle", source.InstanceId, copy.InstanceId, LogSeverity.Good);
                    inserted += ResolveDeathrattleEffect(context, owner, copy, insertIndex + inserted, newEntityIds, false, source.InstanceId);
                }
            }

            owner.CopiedDeathrattlesBySource.Remove(source.InstanceId);
            return inserted;
        }

        private static int ResolveSneedSummonDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            if (source?.Tags == null)
            {
                return 0;
            }

            var tierTag = source.Tags.FirstOrDefault(tag => tag.StartsWith(SneedDeathrattleTierTagPrefix, StringComparison.Ordinal));
            if (string.IsNullOrEmpty(tierTag) ||
                owner.Board.Count >= BoardLimit ||
                owner.CombatSummonPool.Count == 0)
            {
                return 0;
            }

            var tierText = tierTag.Substring(SneedDeathrattleTierTagPrefix.Length);
            var tier = int.TryParse(tierText, out var parsed) ? Math.Max(1, parsed) : Math.Max(1, owner.Tavern?.Tier ?? 1);
            var candidates = owner.CombatSummonPool
                .Where(card => card.CardKind == CardKind.Minion && card.TavernTier <= tier)
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = owner.CombatSummonPool.Where(card => card.CardKind == CardKind.Minion).ToList();
            }

            if (candidates.Count == 0)
            {
                return 0;
            }

            var summoned = new SeededRng(context.Seed + context.Replay.Frames.Count * 997 + owner.Board.Count)
                .Pick(candidates)
                .Clone();
            summoned.InstanceId = "sneed-deathrattle-" + source.InstanceId + "-" + context.Replay.Frames.Count;
            summoned.Owner = owner.Side;
            summoned.PoolSource = PoolSource.Summon;
            summoned.OriginPoolSource = PoolSource.Summon;
            summoned.PoolCopiesHeld = 0;
            summoned.CanAttack = true;
            ApplySummonAuras(owner, summoned);
            owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), summoned);
            ResolveFriendlySummonTriggers(context, owner, summoned, source);
            newEntityIds?.Add(summoned.InstanceId);
            AddLog(context.Log, "MinionSummoned", "Sneed's Replicator summoned " + summoned.Name, source.InstanceId, summoned.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                "Sneed's Replicator summoned " + summoned.Name,
                owner.Side,
                source.InstanceId,
                owner.Side,
                summoned.InstanceId,
                new[] { source.InstanceId, summoned.InstanceId },
                null,
                null,
                new[] { summoned.InstanceId },
                new[] { source.InstanceId });
            return 1;
        }

        private static void TriggerLeftmostOtherDeathrattleFromDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            var target = owner.Board.FirstOrDefault(minion => IsAlive(minion) && minion.Keywords.Contains(Keyword.Deathrattle));
            if (target == null)
            {
                return;
            }

            ResolveDeathrattleSummons(context, owner, target, insertIndex, newEntityIds, false);
            AddLog(context.Log, "DeathrattleResolved", source.InstanceId + " copied " + target.InstanceId + " deathrattle", source.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static void ResolveSoulFermenterResummon(CombatContext context, CombatSideState owner, List<string> newEntityIds)
        {
            if (owner.SoulFermenterTriggered ||
                owner.SoulFermenterStoredMinions.Count == 0 ||
                owner.Board.Any(IsAlive))
            {
                return;
            }

            owner.SoulFermenterTriggered = true;
            var summoned = new List<string>();
            foreach (var stored in owner.SoulFermenterStoredMinions)
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    RecordSummonOverflow(context, owner, null, stored.CardId, stored.Name);
                    break;
                }

                var copy = stored.Clone();
                copy.InstanceId = "soul-fermenter-resummon-" + summoned.Count + "-" + stored.InstanceId;
                copy.Owner = owner.Side;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanAttack = true;
                ApplySummonAuras(owner, copy);
                owner.Board.Add(copy);
                ResolveFriendlySummonTriggers(context, owner, copy, null);
                newEntityIds?.Add(copy.InstanceId);
                summoned.Add(copy.InstanceId);
            }

            if (summoned.Count <= 0)
            {
                return;
            }

            AddLog(context.Log, "MinionSummoned", "Soul Fermenter resummoned " + summoned.Count + " minion(s)", SoulFermenterCardId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                "Soul Fermenter resummoned " + summoned.Count + " minion(s)",
                owner.Side,
                SoulFermenterCardId,
                owner.Side,
                null,
                summoned.Concat(new[] { SoulFermenterCardId }),
                null,
                null,
                summoned,
                new[] { SoulFermenterCardId });
        }

        private static void ResolveSTharaStickerResummon(CombatContext context, CombatSideState owner, List<string> newEntityIds)
        {
            if (owner.Tavern == null ||
                !owner.Tavern.TrinketSTharaStickerActive ||
                owner.STharaTriggered ||
                owner.STharaStoredDemon == null ||
                owner.Board.Any(IsAlive))
            {
                return;
            }

            owner.STharaTriggered = true;
            if (owner.Board.Count >= BoardLimit)
            {
                RecordSummonOverflow(context, owner, owner.STharaStoredDemon, owner.STharaStoredDemon.CardId, owner.STharaStoredDemon.Name);
                return;
            }

            var copy = owner.STharaStoredDemon.Clone();
            copy.InstanceId = "sthara-resummon-" + owner.Side + "-" + owner.STharaStoredDemon.InstanceId;
            copy.Owner = owner.Side;
            copy.Health = Math.Max(1, copy.MaxHealth);
            copy.PoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            copy.AttacksThisCombat = 0;
            RemoveKillTags(copy);
            ApplySummonAuras(owner, copy);
            owner.Board.Add(copy);
            ResolveFriendlySummonTriggers(context, owner, copy, owner.STharaStoredDemon);
            newEntityIds?.Add(copy.InstanceId);

            AddLog(context.Log, "MinionSummoned", "S'Thara Sticker resummoned first dead Demon " + copy.InstanceId, STharaStickerCardId, copy.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                "S'Thara Sticker resummoned first dead Demon " + copy.InstanceId,
                owner.Side,
                STharaStickerCardId,
                owner.Side,
                copy.InstanceId,
                new[] { STharaStickerCardId, owner.STharaStoredDemon.InstanceId, copy.InstanceId },
                null,
                null,
                new[] { copy.InstanceId },
                new[] { STharaStickerCardId, owner.STharaStoredDemon.InstanceId });
        }

        private static void ResolveRotHideGnollDeathAura(CombatSideState owner)
        {
            foreach (var gnoll in owner.Board.Where(candidate => candidate.CardId == RotHideGnollCardId && IsAlive(candidate)).ToList())
            {
                BuffMinion(gnoll, gnoll.Golden ? 2 : 1, 0, "Rot Hide Gnoll");
            }
        }

        private static void ResolveTideRaiser(CombatContext context, CombatSideState owner, int deadIndex, bool golden)
        {
            var candidates = new List<MinionInstance>();
            if (deadIndex - 1 >= 0 && deadIndex - 1 < owner.Board.Count)
            {
                candidates.Add(owner.Board[deadIndex - 1]);
            }

            if (deadIndex >= 0 && deadIndex < owner.Board.Count)
            {
                candidates.Add(owner.Board[deadIndex]);
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var targets = golden ? candidates : new List<MinionInstance> { new SeededRng(context.Seed + context.AttackSequence + deadIndex).Pick(candidates) };
            foreach (var target in targets)
            {
                var amount = target.Tribes.Contains(Tribe.Naga) ? 4 : 2;
                BuffMinion(target, amount, amount, "Shifting Tide");
                AddLog(context.Log, "CombatSpellCast", "Shifting Tide on " + target.InstanceId, TideRaiserCardId, target.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.CombatSpellCast,
                    "Shifting Tide on " + target.InstanceId,
                    owner.Side,
                    TideRaiserCardId,
                    owner.Side,
                    target.InstanceId,
                    new[] { TideRaiserCardId, target.InstanceId },
                    new[] { target.InstanceId },
                    null,
                    null,
                    new[] { TideRaiserCardId });
            }
        }

        private static void QueueTauntDeathRewards(CombatContext context, CombatSideState owner, MinionInstance deadMinion)
        {
            foreach (var source in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == BristlebackScrapSmithCardId))
            {
                AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, source.CardId, BristlebackBloodGemCardId, source.Golden ? 2 : 1);
            }
        }

        private static void ResolveEyeOfDalaranDeath(CombatContext context, CombatSideState owner, MinionInstance deadMinion)
        {
            if (owner?.Tavern == null ||
                !owner.Tavern.TrinketEyeOfDalaranActive ||
                deadMinion == null ||
                BoardTribeAnalyzer.GetCountedTribes(deadMinion).Any(tribe => tribe != Tribe.None))
            {
                return;
            }

            AddReward(context.Log, owner, CombatRewardType.AddRandomTavernSpellToHand, EyeOfDalaranCardId, null, 1, deadMinion.InstanceId);
            AddLog(context.Log, "TrinketDeathTriggered", "The Eye of Dalaran queued a random Tavern spell", EyeOfDalaranCardId, deadMinion.InstanceId, LogSeverity.Good);
        }

        private static void ResolveScrapsmithPortraitTauntDeath(CombatContext context, CombatSideState owner, MinionInstance deadMinion)
        {
            if (owner?.Tavern == null || !owner.Tavern.TrinketScrapsmithPortraitActive)
            {
                return;
            }

            var targets = owner.Board
                .Where(minion => IsAlive(minion) && minion.CardId == BristlebackScrapSmithCardId)
                .ToList();
            foreach (var target in targets)
            {
                ApplyBloodGem(target, owner.Tavern);
                AddTargetedReward(
                    context.Log,
                    owner,
                    CombatRewardType.BuffOriginalFriendlyMinion,
                    ScrapsmithPortraitCardId,
                    target.InstanceId,
                    1,
                    1 + Math.Max(0, owner.Tavern.BloodGemBonusAttack),
                    1 + Math.Max(0, owner.Tavern.BloodGemBonusHealth),
                    deadMinion?.InstanceId);
            }

            if (targets.Count > 0)
            {
                AddLog(context.Log, "TrinketDeathTriggered", "Scrapsmith Portrait played permanent Blood Gems on " + targets.Count + " Scrapsmith(s)", ScrapsmithPortraitCardId, deadMinion?.InstanceId, LogSeverity.Good);
            }
        }

        private static void DealAreaDamage(CombatContext context, CombatSideState owner, MinionInstance source, int amount, Func<MinionInstance, bool> canDamage)
        {
            var damagedIds = new List<string>();
            foreach (var side in new[] { context.Player, context.Opponent })
            {
                foreach (var target in side.Board.Where(minion => IsAlive(minion) && canDamage(minion)).ToList())
                {
                    var result = DealDamage(target, amount, false);
                    ReplaceByInstanceId(side.Board, result.Minion);
                    if (result.CombatDamageDealt || result.DivineShieldBroken)
                    {
                        damagedIds.Add(target.InstanceId);
                    }
                }
            }

            if (damagedIds.Count == 0)
            {
                return;
            }

            AddLog(context.Log, "DamageTriggered", source.InstanceId + " dealt area damage", source.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                source.InstanceId + " dealt area damage",
                owner.Side,
                source.InstanceId,
                owner.Side,
                null,
                damagedIds.Concat(new[] { source.InstanceId }),
                damagedIds,
                null,
                null,
                new[] { source.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                damagedIds.Count);
        }

        private static void TriggerAdjacentBattlecryResources(CombatContext context, CombatSideState owner, MinionInstance source, int sourceIndex, bool sourceRemoved = true)
        {
            var candidates = new List<MinionInstance>();
            var leftIndex = sourceIndex - 1;
            var rightIndex = sourceRemoved ? sourceIndex : sourceIndex + 1;
            if (leftIndex >= 0 && leftIndex < owner.Board.Count)
            {
                candidates.Add(owner.Board[leftIndex]);
            }

            if (rightIndex >= 0 && rightIndex < owner.Board.Count)
            {
                candidates.Add(owner.Board[rightIndex]);
            }

            var targets = candidates.Where(IsAlive).ToList();
            if (targets.Count == 0)
            {
                return;
            }

            if (!source.Golden && targets.Count > 1)
            {
                targets = new List<MinionInstance> { new SeededRng(context.Seed + context.AttackSequence + sourceIndex).Pick(targets) };
            }

            foreach (var target in targets)
            {
                TriggerBattlecryResource(context, owner, source, target);
            }
        }

        private static void ResolveTimewarpedGreenskeeperRally(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var multiplier = source.Golden ? 2 : 1;
            for (var repeat = 0; repeat < multiplier; repeat += 1)
            {
                var battlecryTarget = owner.Board
                    .LastOrDefault(minion => minion.InstanceId != source.InstanceId && IsAlive(minion) && minion.Keywords.Contains(Keyword.Battlecry));
                if (battlecryTarget != null)
                {
                    TriggerBattlecryResource(context, owner, source, battlecryTarget);
                    AddLog(context.Log, "RallyResolved", source.InstanceId + " triggered " + battlecryTarget.InstanceId + " battlecry", source.InstanceId, battlecryTarget.InstanceId, LogSeverity.Good);
                }

                var deathrattleTarget = owner.Board
                    .LastOrDefault(minion => minion.InstanceId != source.InstanceId && IsAlive(minion) && minion.Keywords.Contains(Keyword.Deathrattle));
                if (deathrattleTarget == null)
                {
                    continue;
                }

                var targetIndex = owner.Board.FindIndex(minion => minion.InstanceId == deathrattleTarget.InstanceId);
                if (targetIndex < 0)
                {
                    continue;
                }

                var newEntityIds = new List<string>();
                ResolveDeathrattleEffect(context, owner, deathrattleTarget, Math.Min(targetIndex + 1, owner.Board.Count), newEntityIds, false, source.InstanceId);
                ResolveDeaths(context, owner.Side);
                ResolveDeaths(context, owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
                AddLog(context.Log, "RallyResolved", source.InstanceId + " triggered " + deathrattleTarget.InstanceId + " deathrattle", source.InstanceId, deathrattleTarget.InstanceId, LogSeverity.Good);
            }
        }

        private static void TriggerBattlecryResource(CombatContext context, CombatSideState owner, MinionInstance source, MinionInstance target)
        {
            var multiplier = target.Golden ? 2 : 1;
            switch (target.CardId)
            {
                case FeedingTigerSharkCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomBeastToHand, source.CardId, null, multiplier);
                    break;
                case PricklyPiperCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomDemonToHand, source.CardId, null, multiplier);
                    break;
                case BalladistCardId:
                    BuffFirstFriendly(owner.Board.Where(minion => minion.InstanceId != target.InstanceId && minion.Tribes.Contains(Tribe.Pirate)), 0, multiplier, "Balladist");
                    break;
                case KingBagurgleCardId:
                    foreach (var murloc in owner.Board.Where(minion => minion.InstanceId != target.InstanceId && minion.Tribes.Contains(Tribe.Murloc)))
                    {
                        BuffMinion(murloc, 4 * multiplier, 4 * multiplier, "King Bagurgle");
                    }

                    break;
                case ScrapperCardId:
                    MagnetizeRandomMechOntoFriendlyMech(context, owner, target, multiplier);
                    break;
                case DeepwaterChieftainCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, source.CardId, DeepwaterSchoolCardId, multiplier);
                    break;
                case ManasparkCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, source.CardId, ArcaneConsumptionCardId, multiplier);
                    break;
                case RefreshingAnomalyCardId:
                    AddReward(context.Log, owner, CombatRewardType.GainFreeRefresh, source.CardId, null, 2 * multiplier);
                    break;
                case TavernTempestCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomElementalToHand, source.CardId, null, multiplier);
                    break;
                case BrannosaurCardId:
                    AddReward(context.Log, owner, CombatRewardType.ImproveRefreshBuff, source.CardId, null, 1, 7 * multiplier, 7 * multiplier);
                    break;
                case SaloonDancerCardId:
                    BuffFirstFriendly(owner.Board.Where(minion => minion.InstanceId != target.InstanceId), 2 * multiplier, 2 * multiplier, "Saloon Dancer");
                    break;
                case DustyCycloneCardId:
                    foreach (var elemental in owner.Board.Where(minion => minion.Tribes.Contains(Tribe.Elemental)))
                    {
                        BuffMinion(elemental, multiplier, 0, "Dusty Cyclone");
                    }

                    break;
                case TimewarpedBuskerCardId:
                    AddReward(context.Log, owner, CombatRewardType.GainNextTurnGold, target.CardId, null, multiplier, target.InstanceId);
                    break;
                case TimewarpedNestSwarmerCardId:
                    ResolveTimewarpedNestSwarmer(context, owner, target, owner.Board.Count, new List<string>());
                    break;
                case TimewarpedCalligrapherCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomTavernSpellToHand, target.CardId, null, multiplier, target.InstanceId);
                    break;
                case TimewarpedHunterCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, target.CardId, PointyArrowCardId, multiplier, target.InstanceId);
                    break;
                case TimewarpedThorncallerCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, target.CardId, BloodGemBarrageCardNumber, multiplier, target.InstanceId);
                    break;
            }
        }

        private static int TriggerAdjacentDeathrattleExceptWarghoul(
            CombatContext context,
            CombatSideState owner,
            MinionInstance source,
            int sourceIndex,
            List<string> newEntityIds,
            bool sourceRemoved)
        {
            var candidates = new List<MinionInstance>();
            if (sourceRemoved)
            {
                var leftIndex = sourceIndex - 1;
                if (leftIndex >= 0 && leftIndex < owner.Board.Count)
                {
                    candidates.Add(owner.Board[leftIndex]);
                }

                if (sourceIndex >= 0 && sourceIndex < owner.Board.Count)
                {
                    candidates.Add(owner.Board[sourceIndex]);
                }
            }
            else
            {
                var currentIndex = owner.Board.FindIndex(minion => minion.InstanceId == source.InstanceId);
                if (currentIndex >= 0)
                {
                    var leftIndex = currentIndex - 1;
                    var rightIndex = currentIndex + 1;
                    if (leftIndex >= 0 && leftIndex < owner.Board.Count)
                    {
                        candidates.Add(owner.Board[leftIndex]);
                    }

                    if (rightIndex >= 0 && rightIndex < owner.Board.Count)
                    {
                        candidates.Add(owner.Board[rightIndex]);
                    }
                }
            }

            var targets = candidates
                .Where(minion => IsAlive(minion) && minion.CardId != TimewarpedWarghoulCardId && minion.Keywords.Contains(Keyword.Deathrattle))
                .ToList();
            if (targets.Count == 0)
            {
                return 0;
            }

            var target = targets.Count == 1
                ? targets[0]
                : new SeededRng(context.Seed + context.AttackSequence + sourceIndex + context.Log.Count).Pick(targets);
            var targetIndex = owner.Board.FindIndex(minion => minion.InstanceId == target.InstanceId);
            if (targetIndex < 0)
            {
                return 0;
            }

            AddLog(context.Log, "DeathrattleResolved", source.InstanceId + " triggered adjacent " + target.InstanceId + " deathrattle", source.InstanceId, target.InstanceId, LogSeverity.Good);
            return ResolveDeathrattleEffect(context, owner, target, Math.Min(targetIndex + 1, owner.Board.Count), newEntityIds, false, source.InstanceId);
        }

        private static void MagnetizeRandomMechOntoFriendlyMech(CombatContext context, CombatSideState owner, MinionInstance source, int count)
        {
            var target = owner.Board.FirstOrDefault(minion => minion.InstanceId != source.InstanceId && IsAlive(minion) && (minion.Tribes.Contains(Tribe.Mech) || minion.Tribes.Contains(Tribe.All)))
                ?? owner.Board.FirstOrDefault(minion => IsAlive(minion) && (minion.Tribes.Contains(Tribe.Mech) || minion.Tribes.Contains(Tribe.All)));
            if (target == null)
            {
                return;
            }

            var amount = Math.Max(1, count);
            for (var index = 0; index < amount; index += 1)
            {
                BuffMinion(target, 2, 2, "Scrapper Magnetic");
            }
        }

        private static void GiveDifferentUndeadReborn(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var target = owner.Board.FirstOrDefault(minion => minion.InstanceId != source.InstanceId && IsAlive(minion) && minion.Tribes.Contains(Tribe.Undead) && !minion.Keywords.Contains(Keyword.Reborn));
            if (target == null)
            {
                return;
            }

            target.Keywords.Add(Keyword.Reborn);
            AddLog(context.Log, "DeathrattleResolved", source.InstanceId + " gave Reborn to " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DeathrattleResolved,
                source.InstanceId + " gave Reborn to " + target.InstanceId,
                owner.Side,
                source.InstanceId,
                owner.Side,
                target.InstanceId,
                new[] { source.InstanceId, target.InstanceId },
                null,
                null,
                null,
                new[] { source.InstanceId });
        }

        private static void ApplyBloodGemsToAdjacent(CombatSideState owner, int deadIndex, int count)
        {
            var targets = new List<MinionInstance>();
            if (deadIndex - 1 >= 0 && deadIndex - 1 < owner.Board.Count)
            {
                targets.Add(owner.Board[deadIndex - 1]);
            }

            if (deadIndex >= 0 && deadIndex < owner.Board.Count)
            {
                targets.Add(owner.Board[deadIndex]);
            }

            foreach (var target in targets.Where(IsAlive))
            {
                for (var index = 0; index < count; index += 1)
                {
                    ApplyBloodGem(target, owner.Tavern);
                }
            }
        }

        private static void ResolveRally(CombatContext context, BoardSide side, string attackerId, bool triggeredAttack)
        {
            var owner = context.Get(side);
            var attackerIndex = owner.Board.FindIndex(minion => minion.InstanceId == attackerId && IsAlive(minion));
            if (attackerIndex < 0)
            {
                return;
            }

            var attacker = owner.Board[attackerIndex];
            var hasRally = attacker.Keywords.Contains(Keyword.Rally);
            var repeats = hasRally ? GetRallyRepeats(owner) : 1;
            if (hasRally)
            {
                AddReward(context.Log, owner, CombatRewardType.FriendlyRallyTriggered, attacker.CardId, null, repeats, attacker.InstanceId);
            }

            for (var repeat = 0; repeat < repeats; repeat += 1)
            {
                if (attacker.CardId == DustboneDestroyerCardId)
                {
                    ResolveDustboneDestroyerRally(context, owner, attacker, triggeredAttack);
                }

                if (attacker.CardId == TimewarpedSeaGlassCardId)
                {
                    ResolveTimewarpedSeaGlassRally(context, owner, attacker, triggeredAttack);
                }

                if (attacker.CardId == TimewarpedNestSwarmerCardId)
                {
                    ResolveTimewarpedNestSwarmer(context, owner, attacker, attackerIndex + 1, new List<string>());
                }

                if (attacker.CardId == TimewarpedCollectorCardId)
                {
                    ResolveTimewarpedCollectorRally(context, owner, attacker, triggeredAttack);
                }

                if (attacker.CardId == TimewarpedGreenskeeperCardId)
                {
                    ResolveTimewarpedGreenskeeperRally(context, owner, attacker);
                }

                ResolveHighTierRally(context, owner, attacker, attackerIndex);
                ResolveBattlecruiserBallisticsRally(context, owner, attacker);
                ResolveZergRally(context, owner, attacker);
                ResolveProtossRally(context, owner, attacker, attackerIndex);

                if (attacker.CardId == ExpertAviatorCardId)
                {
                    ResolveExpertAviatorRally(context, owner, attacker, attackerIndex);
                }

                if (attacker.CardId != SleepySupporterCardId || attackerIndex + 1 >= owner.Board.Count)
                {
                    continue;
                }

                var target = owner.Board[attackerIndex + 1];
                var amount = attacker.Golden ? 4 : 2;
                BuffMinion(target, amount, amount, "Sleepy Supporter");
                AddLog(context.Log, "RallyResolved", attacker.InstanceId + " rallied " + target.InstanceId, attacker.InstanceId, target.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.RallyResolved,
                    attacker.InstanceId + " rallied " + target.InstanceId,
                    owner.Side,
                    attacker.InstanceId,
                    owner.Side,
                    target.InstanceId,
                    new[] { attacker.InstanceId, target.InstanceId },
                    new[] { target.InstanceId },
                    null,
                    null,
                    new[] { attacker.InstanceId });
            }
        }

        private static void ResolveBattlecruiserBallisticsRally(CombatContext context, CombatSideState owner, MinionInstance attacker)
        {
            if (!IsBattlecruiser(attacker) || attacker.Tags == null || !attacker.Tags.Contains("battlecruiser_ballistics"))
            {
                return;
            }

            attacker.Counters.TryGetValue("battlecruiser_ballistics_attack", out var amount);
            amount = Math.Max(1, amount);
            foreach (var target in owner.Board.Where(minion => IsAlive(minion) && minion.InstanceId != attacker.InstanceId))
            {
                BuffMinion(target, amount, 0, "Advanced Ballistics");
            }

            AddLog(context.Log, "RallyResolved", "Advanced Ballistics gave +" + amount + " Attack to other friendly minions", attacker.InstanceId, null, LogSeverity.Good);
        }

        private static void ResolveZergRally(CombatContext context, CombatSideState owner, MinionInstance attacker)
        {
            if (attacker.CardId != "BG31_HERO_811t4")
            {
                return;
            }

            var amount = attacker.Golden ? 2 : 1;
            BuffMinion(attacker, amount, 0, "Hydralisk");
            AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, attacker.CardId, attacker.InstanceId, 1, amount, 0, attacker.InstanceId);
            AddLog(context.Log, "RallyResolved", "Hydralisk permanently gained Attack", attacker.InstanceId, null, LogSeverity.Good);
        }

        private static void ResolveProtossRally(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            if (attacker.CardId != ProtossColossusCardId)
            {
                return;
            }

            var enemy = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            var targets = enemy.Board.Where(IsAlive).ToList();
            if (targets.Count == 0)
            {
                return;
            }

            var target = new SeededRng(context.Seed + context.AttackSequence * 919 + attackerIndex).Pick(targets);
            var damage = Math.Max(1, attacker.Cost + 1);
            var result = DealDamage(target, damage, false);
            if (!IsAlive(result.Minion))
            {
                MarkKilledBy(result.Minion, attacker.InstanceId, owner.Side, attacker.CardId);
            }

            ReplaceByInstanceId(enemy.Board, result.Minion);
            AddLog(context.Log, "RallyResolved", "Colossus dealt " + damage + " splash damage to " + target.InstanceId, attacker.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static int GetRallyRepeats(CombatSideState owner)
        {
            return 1 + GetTimewarpedDeiosExtraTriggers(owner);
        }

        private static void ResolveTimewarpedCollectorRally(CombatContext context, CombatSideState owner, MinionInstance attacker, bool triggeredAttack)
        {
            if (owner.Board.Count(minion => IsAlive(minion) && minion.Golden) < 4)
            {
                return;
            }

            AddKeyword(attacker, Keyword.DivineShield);
            AddLog(context.Log, "RallyResolved", attacker.InstanceId + " gained Divine Shield", attacker.InstanceId, attacker.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.RallyResolved,
                attacker.InstanceId + " gained Divine Shield",
                owner.Side,
                attacker.InstanceId,
                owner.Side,
                attacker.InstanceId,
                new[] { attacker.InstanceId },
                new[] { attacker.InstanceId },
                null,
                null,
                new[] { attacker.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                triggeredAttack);
        }

        private static void ResolveExpertAviatorRally(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            var count = attacker.Golden ? 2 : 1;
            var candidates = owner.Hand
                .Where(card => card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.Attack)
                .ThenBy(card => card.InstanceId)
                .Take(count)
                .ToList();
            var insertIndex = attackerIndex + 1;
            foreach (var candidate in candidates)
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    return;
                }

                var copy = candidate.Clone();
                copy.InstanceId = "combat-aviator-" + attacker.InstanceId + "-" + copy.InstanceId;
                copy.Owner = owner.Side;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanAttack = true;
                ApplySummonAuras(owner, copy);
                owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), copy);
                ResolveFriendlySummonTriggers(context, owner, copy, attacker);
                insertIndex += 1;
                AddLog(context.Log, "RallyResolved", attacker.InstanceId + " summoned " + copy.InstanceId, attacker.InstanceId, copy.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.RallyResolved,
                    attacker.InstanceId + " summoned " + copy.InstanceId,
                    owner.Side,
                    attacker.InstanceId,
                    owner.Side,
                    copy.InstanceId,
                    new[] { attacker.InstanceId, copy.InstanceId },
                    null,
                    null,
                    new[] { copy.InstanceId },
                    new[] { attacker.InstanceId });
            }
        }

        private static void ResolveDustboneDestroyerRally(CombatContext context, CombatSideState owner, MinionInstance attacker, bool triggeredAttack)
        {
            var amount = attacker.Golden ? 2 : 1;
            var targets = owner.Board
                .Where(minion => minion.InstanceId != attacker.InstanceId && IsAlive(minion) && minion.Tribes.Contains(Tribe.Undead))
                .ToList();
            if (targets.Count == 0)
            {
                return;
            }

            foreach (var target in targets)
            {
                BuffMinion(target, amount, 0, "Dustbone Destroyer");
            }

            AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " rallied undead attack +" + amount, attacker.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AttackTriggered,
                attacker.InstanceId + " rallied undead attack +" + amount,
                owner.Side,
                attacker.InstanceId,
                owner.Side,
                null,
                targets.Select(target => target.InstanceId).Concat(new[] { attacker.InstanceId }),
                null,
                null,
                null,
                new[] { attacker.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                triggeredAttack);
        }

        private static void ResolveTimewarpedSeaGlassRally(CombatContext context, CombatSideState owner, MinionInstance attacker, bool triggeredAttack)
        {
            attacker.Counters.TryGetValue("timewarped_sea_glass_rallies", out var rallies);
            if (rallies >= 2)
            {
                return;
            }

            attacker.Counters["timewarped_sea_glass_rallies"] = rallies + 1;
            BuffMinion(attacker, Math.Max(0, attacker.Attack), Math.Max(0, attacker.MaxHealth), "Timewarped Sea Glass");
            AddLog(context.Log, "RallyResolved", attacker.InstanceId + " doubled stats", attacker.InstanceId, attacker.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.RallyResolved,
                attacker.InstanceId + " doubled stats",
                owner.Side,
                attacker.InstanceId,
                owner.Side,
                attacker.InstanceId,
                new[] { attacker.InstanceId },
                null,
                null,
                null,
                new[] { attacker.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                triggeredAttack,
                rallies + 1,
                2);
        }

        private static void ResolveTrinketAttackDeclarationTriggers(CombatContext context, CombatSideState owner, MinionInstance attacker, bool triggeredAttack)
        {
            var tavern = owner.Tavern;
            if (tavern == null || attacker == null)
            {
                return;
            }

            if (tavern.TrinketCeremonialSwordAttack != 0)
            {
                BuffMinion(attacker, tavern.TrinketCeremonialSwordAttack, 0, "Ceremonial Sword");
                AddLog(context.Log, "TrinketAttackTriggered", "Ceremonial Sword gave +" + tavern.TrinketCeremonialSwordAttack + " Attack to " + attacker.InstanceId, null, attacker.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.AttackTriggered,
                    "Ceremonial Sword gave +" + tavern.TrinketCeremonialSwordAttack + " Attack to " + attacker.InstanceId,
                    owner.Side,
                    null,
                    owner.Side,
                    attacker.InstanceId,
                    new[] { attacker.InstanceId },
                    null,
                    null,
                    null,
                    new[] { attacker.InstanceId },
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    triggeredAttack);
            }

            if (tavern.TrinketFaerieDragonScaleUses > 0 && attacker.Tribes.Contains(Tribe.Dragon))
            {
                AddKeyword(attacker, Keyword.DivineShield);
                tavern.TrinketFaerieDragonScaleUses -= 1;
                AddLog(context.Log, "TrinketAttackTriggered", "Faerie Dragon Scale gave Divine Shield to " + attacker.InstanceId, null, attacker.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.AttackTriggered,
                    "Faerie Dragon Scale gave Divine Shield to " + attacker.InstanceId,
                    owner.Side,
                    null,
                    owner.Side,
                    attacker.InstanceId,
                    new[] { attacker.InstanceId },
                    null,
                    null,
                    null,
                    new[] { attacker.InstanceId },
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    triggeredAttack,
                    3 - tavern.TrinketFaerieDragonScaleUses,
                    3);
            }

            if (tavern.TrinketAllPurposeKibbleAttack > 0 && HasCountedTribe(attacker, Tribe.Beast))
            {
                var attack = tavern.TrinketAllPurposeKibbleAttack;
                BuffMinion(attacker, attack, 0, "All-Purpose Kibble");
                tavern.TrinketAllPurposeKibbleAttack = StatMath.SaturatingAdd(tavern.TrinketAllPurposeKibbleAttack, 1, 0, StatMath.MaxStat);
                AddReward(context.Log, owner, CombatRewardType.ImproveAllPurposeKibble, AllPurposeKibbleCardId, null, 1);
                AddLog(context.Log, "TrinketAttackTriggered", "All-Purpose Kibble gave +" + attack + " Attack to " + attacker.InstanceId + " and improved to +" + tavern.TrinketAllPurposeKibbleAttack, AllPurposeKibbleCardId, attacker.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.AttackTriggered,
                    "All-Purpose Kibble gave +" + attack + " Attack to " + attacker.InstanceId,
                    owner.Side,
                    AllPurposeKibbleCardId,
                    owner.Side,
                    attacker.InstanceId,
                    new[] { attacker.InstanceId, AllPurposeKibbleCardId },
                    null,
                    null,
                    null,
                    new[] { attacker.InstanceId },
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    triggeredAttack,
                    tavern.TrinketAllPurposeKibbleAttack,
                    0);
            }

            if (tavern.TrinketJarOGemsAttackThreshold > 0)
            {
                tavern.TrinketJarOGemsAttackCounter += 1;
                if (tavern.TrinketJarOGemsAttackCounter >= tavern.TrinketJarOGemsAttackThreshold)
                {
                    tavern.TrinketJarOGemsAttackCounter = 0;
                    var targets = owner.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Quilboar)).ToList();
                    foreach (var target in targets)
                    {
                        ApplyBloodGem(target, tavern);
                    }

                    AddLog(context.Log, "TrinketAttackTriggered", "Jar o' Gems played Blood Gems on " + targets.Count + " Quilboar", JarOGemsCardId, attacker.InstanceId, LogSeverity.Good);
                    RecordFrame(
                        context,
                        CombatEventType.AttackTriggered,
                        "Jar o' Gems played Blood Gems on " + targets.Count + " Quilboar",
                        owner.Side,
                        JarOGemsCardId,
                        owner.Side,
                        targets.FirstOrDefault()?.InstanceId,
                        targets.Select(target => target.InstanceId).Concat(new[] { attacker.InstanceId, JarOGemsCardId }),
                        targets.Select(target => target.InstanceId),
                        null,
                        null,
                        targets.Select(target => target.InstanceId),
                        null,
                        BoardSide.Player,
                        -1,
                        0,
                        0,
                        0,
                        0,
                        triggeredAttack,
                        tavern.TrinketJarOGemsAttackThreshold,
                        tavern.TrinketJarOGemsAttackThreshold);
                }
            }

            if (tavern.TrinketElementiumChestAttackThreshold > 0 && HasCountedTribe(attacker, Tribe.Pirate))
            {
                tavern.TrinketElementiumChestAttackCounter += 1;
                if (tavern.TrinketElementiumChestAttackCounter >= tavern.TrinketElementiumChestAttackThreshold)
                {
                    tavern.TrinketElementiumChestAttackCounter = 0;
                    AddReward(context.Log, owner, CombatRewardType.GainNextTurnGold, ElementiumChestCardId, null, 1, attacker.InstanceId);
                    AddLog(context.Log, "TrinketAttackTriggered", "Elementium Chest queued 1 Gold next turn", ElementiumChestCardId, attacker.InstanceId, LogSeverity.Good);
                    RecordFrame(
                        context,
                        CombatEventType.AttackTriggered,
                        "Elementium Chest queued 1 Gold next turn",
                        owner.Side,
                        ElementiumChestCardId,
                        owner.Side,
                        attacker.InstanceId,
                        new[] { attacker.InstanceId, ElementiumChestCardId },
                        null,
                        null,
                        null,
                        new[] { ElementiumChestCardId },
                        null,
                        BoardSide.Player,
                        -1,
                        0,
                        0,
                        0,
                        0,
                        triggeredAttack,
                        tavern.TrinketElementiumChestAttackThreshold,
                        tavern.TrinketElementiumChestAttackThreshold);
                }
            }
        }

        private static void ResolveAttackDeclarationTriggers(CombatContext context, CombatSideState owner, MinionInstance attacker, CombatSideState defenderOwner, MinionInstance defender, bool triggeredAttack)
        {
            if (!IsAlive(attacker))
            {
                return;
            }

            ResolveTrinketAttackDeclarationTriggers(context, owner, attacker, triggeredAttack);
            ResolveSecretAttackDeclarationTriggers(context, defenderOwner, defender);
            ResolveWanderingTreantTauntAttacked(context, defenderOwner, defender);

            if (attacker.CardId == SindoreiStraightShotCardId && defender != null)
            {
                defender.Keywords.Remove(Keyword.Reborn);
                defender.Keywords.Remove(Keyword.Taunt);
                AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " purged Reborn/Taunt from " + defender.InstanceId, attacker.InstanceId, defender.InstanceId, LogSeverity.Good);
            }

            if (attacker.CardId == ValiantRebelCardId && defender != null)
            {
                BuffMinion(attacker, defender.Attack, 0, "Valiant Rebel");
                AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " gained target attack", attacker.InstanceId, defender.InstanceId, LogSeverity.Good);
            }

            if (attacker.CardId == OperaticBelcherCardId)
            {
                var target = owner.Board.FirstOrDefault(minion => minion.InstanceId != attacker.InstanceId && minion.Tribes.Contains(Tribe.Murloc));
                if (target != null && !target.Keywords.Contains(Keyword.Venomous))
                {
                    target.Keywords.Add(Keyword.Venomous);
                    AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " gave Venomous to " + target.InstanceId, attacker.InstanceId, target.InstanceId, LogSeverity.Good);
                }
            }

            if (attacker.CardId == MonstrousMacawCardId)
            {
                TriggerLeftmostOtherDeathrattle(context, owner, attacker);
            }

            if (attacker.CardId == TopperTheThiefCardId)
            {
                AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, attacker.CardId, HealthyBountyCardId, attacker.Golden ? 2 : 1);
            }

            if (attacker.CardId == MobileProjectionistCardId)
            {
                AddReward(context.Log, owner, CombatRewardType.AddRandomMagneticMechToHand, attacker.CardId, null, attacker.Golden ? 2 : 1);
            }

            if (attacker.CardId == RecruiterOfTheDeepCardId)
            {
                CastChefChoiceOnRightNeighbor(context, owner, attacker);
                if (attacker.Golden)
                {
                    CastChefChoiceOnRightNeighbor(context, owner, attacker);
                }
            }

            if (!attacker.Tribes.Contains(Tribe.Dragon))
            {
                return;
            }

            // The attacking Roaring Recruiter is not "another" friendly Dragon for its own trigger.
            var sources = owner.Board
                .Where(minion => minion.CardId == RoaringRecruiterCardId && minion.InstanceId != attacker.InstanceId && IsAlive(minion))
                .ToList();
            foreach (var source in sources)
            {
                var attack = source.Golden ? 6 : 3;
                var health = source.Golden ? 2 : 1;
                BuffMinion(attacker, attack, health, "Roaring Recruiter");
                AddLog(context.Log, "AttackTriggered", source.InstanceId + " buffed attacking dragon " + attacker.InstanceId, source.InstanceId, attacker.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.AttackTriggered,
                    source.InstanceId + " buffed attacking dragon " + attacker.InstanceId,
                    owner.Side,
                    source.InstanceId,
                    owner.Side,
                    attacker.InstanceId,
                    new[] { source.InstanceId, attacker.InstanceId },
                    null,
                    null,
                    null,
                    new[] { source.InstanceId },
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    triggeredAttack);
            }
        }

        private static void ResolveSecretAttackDeclarationTriggers(CombatContext context, CombatSideState defenderOwner, MinionInstance defender)
        {
            if (defenderOwner == null || defender == null)
            {
                return;
            }

            if (TryTriggerSecret(defenderOwner.Tavern, SnakeTrapSecretId))
            {
                for (var index = 0; index < 3; index += 1)
                {
                    AddToken(context, defenderOwner, defender, defenderOwner.Board.Count, "bacon-secret-snake", "Snake", 1, 1, Tribe.Beast);
                }

                AddLog(context.Log, "SecretTriggered", "Snake Trap summoned three Snakes", SnakeTrapSecretId, defender.InstanceId, LogSeverity.Good);
            }

            var betterVenomstrike = TryTriggerSecret(defenderOwner.Tavern, BetterVenomstrikeTrapSecretId);
            var normalVenomstrike = betterVenomstrike ? false : TryTriggerSecret(defenderOwner.Tavern, VenomstrikeTrapSecretId);
            if (betterVenomstrike || normalVenomstrike)
            {
                var cobra = AddToken(context, defenderOwner, defender, defenderOwner.Board.Count, "bacon-secret-cobra", "Cobra", 2, 3, Tribe.Beast, Keyword.Poisonous);
                if (cobra != null && betterVenomstrike)
                {
                    AddKeyword(cobra, Keyword.Reborn);
                }

                AddLog(
                    context.Log,
                    "SecretTriggered",
                    (betterVenomstrike ? "Better Venomstrike Trap" : "Venomstrike Trap") + " summoned a Cobra",
                    betterVenomstrike ? BetterVenomstrikeTrapSecretId : VenomstrikeTrapSecretId,
                    cobra?.InstanceId ?? defender.InstanceId,
                    LogSeverity.Good);
            }

            if (TryTriggerSecret(defenderOwner.Tavern, SplittingImageSecretId))
            {
                var copy = SummonCombatCopy(context, defenderOwner, defender, defenderOwner.Board.Count, SplittingImageSecretId, "splitting-image", null, null);
                AddLog(context.Log, "SecretTriggered", "Splitting Image copied " + defender.InstanceId, SplittingImageSecretId, copy?.InstanceId ?? defender.InstanceId, LogSeverity.Good);
            }

            var betterPackTactics = TryTriggerSecret(defenderOwner.Tavern, BetterPackTacticsSecretId);
            var normalPackTactics = betterPackTactics ? false : TryTriggerSecret(defenderOwner.Tavern, PackTacticsSecretId);
            if (betterPackTactics || normalPackTactics)
            {
                var copy = SummonCombatCopy(
                    context,
                    defenderOwner,
                    defender,
                    defenderOwner.Board.Count,
                    betterPackTactics ? BetterPackTacticsSecretId : PackTacticsSecretId,
                    betterPackTactics ? "better-pack-tactics" : "pack-tactics",
                    normalPackTactics ? 3 : (int?)null,
                    normalPackTactics ? 3 : (int?)null);
                AddLog(
                    context.Log,
                    "SecretTriggered",
                    (betterPackTactics ? "Better Pack Tactics" : "Pack Tactics") + " copied " + defender.InstanceId,
                    betterPackTactics ? BetterPackTacticsSecretId : PackTacticsSecretId,
                    copy?.InstanceId ?? defender.InstanceId,
                    LogSeverity.Good);
            }

            var betterAutodefense = TryTriggerSecret(defenderOwner.Tavern, BetterAutodefenseMatrixSecretId);
            var normalAutodefense = betterAutodefense ? false : TryTriggerSecret(defenderOwner.Tavern, AutodefenseMatrixSecretId);
            if (betterAutodefense || normalAutodefense)
            {
                AddKeyword(defender, Keyword.DivineShield);
                if (betterAutodefense)
                {
                    AddKeyword(defender, Keyword.Reborn);
                }

                AddLog(
                    context.Log,
                    "SecretTriggered",
                    (betterAutodefense ? "Better Autodefense Matrix" : "Autodefense Matrix") + " protected " + defender.InstanceId,
                    betterAutodefense ? BetterAutodefenseMatrixSecretId : AutodefenseMatrixSecretId,
                    defender.InstanceId,
                    LogSeverity.Good);
            }
        }

        private static MinionInstance SummonCombatCopy(
            CombatContext context,
            CombatSideState owner,
            MinionInstance source,
            int insertIndex,
            string sourceId,
            string instancePrefix,
            int? setAttack,
            int? setHealth)
        {
            if (owner == null || source == null)
            {
                return null;
            }

            if (owner.Board.Count >= BoardLimit)
            {
                RecordSummonOverflow(context, owner, source, source.CardId, source.Name);
                return null;
            }

            var copy = source.Clone();
            copy.InstanceId = instancePrefix + "-" + source.InstanceId + "-" + context.Replay.Frames.Count;
            copy.Owner = owner.Side;
            copy.PoolSource = PoolSource.Summon;
            copy.OriginPoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            copy.AttacksThisCombat = 0;
            RemoveKillTags(copy);
            if (setAttack.HasValue)
            {
                copy.BaseAttack = setAttack.Value;
                copy.Attack = setAttack.Value;
            }

            if (setHealth.HasValue)
            {
                copy.BaseHealth = setHealth.Value;
                copy.MaxHealth = setHealth.Value;
                copy.Health = setHealth.Value;
            }
            else
            {
                copy.Health = Math.Max(1, copy.Health);
            }

            ApplySummonAuras(owner, copy);
            owner.Board.Insert(Math.Min(Math.Max(0, insertIndex), owner.Board.Count), copy);
            ResolveFriendlySummonTriggers(context, owner, copy, source);
            AddLog(context.Log, "MinionSummoned", sourceId + " summoned copy of " + source.InstanceId, sourceId, copy.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                sourceId + " summoned copy of " + source.InstanceId,
                owner.Side,
                sourceId,
                owner.Side,
                copy.InstanceId,
                new[] { sourceId, source.InstanceId, copy.InstanceId },
                null,
                null,
                new[] { copy.InstanceId },
                new[] { sourceId, source.InstanceId });
            return copy;
        }

        private static void ResolveWanderingTreantTauntAttacked(CombatContext context, CombatSideState defenderOwner, MinionInstance defender)
        {
            if (defenderOwner == null ||
                defender == null ||
                !defender.Keywords.Contains(Keyword.Taunt))
            {
                return;
            }

            var treants = defenderOwner.Board
                .Where(minion => IsAlive(minion) && minion.CardId == WanderingTreantCardId)
                .ToList();
            foreach (var treant in treants)
            {
                var amount = treant.Golden ? 2 : 1;
                BuffAll(defenderOwner.Board.Where(IsAlive), amount, amount, "Wandering Treant");
                AddReward(context.Log, defenderOwner, CombatRewardType.BuffFriendlyBoard, treant.CardId, null, 1, amount, amount, treant.InstanceId);
                AddLog(context.Log, "AttackTriggered", "Wandering Treant buffed the warband after a Taunt was attacked", treant.InstanceId, defender.InstanceId, LogSeverity.Good);
            }
        }

        private static void ResolveDamageTriggers(
            CombatContext context,
            CombatSideState attackerOwner,
            string attackerId,
            bool attackerTookDamage,
            bool attackerShieldBroken,
            CombatSideState defenderOwner,
            string defenderId,
            bool defenderTookDamage,
            bool defenderShieldBroken)
        {
            ResolveTrinketDivineShieldLostTriggers(context, attackerOwner, attackerId, attackerShieldBroken);
            ResolveTrinketDivineShieldLostTriggers(context, defenderOwner, defenderId, defenderShieldBroken);
            ResolveToughOrcaDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage, attackerShieldBroken);
            ResolveToughOrcaDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage, defenderShieldBroken);
            ResolveTrigoreDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveTrigoreDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage);
            ResolveSkyfinDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveSkyfinDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage);
            ResolveDevoutSatyressDamageTrigger(context, attackerOwner, attackerId, defenderTookDamage || defenderShieldBroken);
            ResolveDevoutSatyressDamageTrigger(context, defenderOwner, defenderId, attackerTookDamage || attackerShieldBroken);
            ResolveRuinsLordDamageTrigger(context, attackerOwner, attackerId, defenderTookDamage || defenderShieldBroken);
            ResolveRuinsLordDamageTrigger(context, defenderOwner, defenderId, attackerTookDamage || attackerShieldBroken);
            ResolveTigerCarvingDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveTigerCarvingDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage);
            ResolveWyvernDamageRefreshTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveWyvernDamageRefreshTrigger(context, defenderOwner, defenderId, defenderTookDamage);
            ResolveSilkyShimmermothDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveSilkyShimmermothDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage);
            ResolveTimewarpedDamageTrigger(context, attackerOwner, attackerId, attackerTookDamage);
            ResolveTimewarpedDamageTrigger(context, defenderOwner, defenderId, defenderTookDamage);
        }

        private static void ResolveReckoningSecret(CombatContext context, CombatSideState secretOwner, MinionInstance enemySource, bool dealtDamage, int damageAmount)
        {
            if (context == null ||
                secretOwner == null ||
                enemySource == null ||
                !dealtDamage ||
                damageAmount < 3 ||
                !IsAlive(enemySource) ||
                !TryTriggerSecret(secretOwner.Tavern, ReckoningSecretId))
            {
                return;
            }

            var enemyOwner = context.Get(secretOwner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            enemySource.Health = 0;
            MarkKilledBy(enemySource, ReckoningSecretId, secretOwner.Side, ReckoningSecretId);
            ReplaceByInstanceId(enemyOwner.Board, enemySource);
            AddLog(context.Log, "SecretTriggered", "Reckoning destroyed " + enemySource.InstanceId, ReckoningSecretId, enemySource.InstanceId, LogSeverity.Good);
        }

        private static void ResolveTimewarpedDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage || string.IsNullOrEmpty(damagedId))
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && IsAlive(minion));
            if (source == null)
            {
                return;
            }

            if (source.CardId == TimewarpedDeathswarmerCardId)
            {
                AddReward(context.Log, owner, CombatRewardType.ImproveUndeadAttack, source.CardId, null, source.Golden ? 2 : 1, source.InstanceId);
            }
            else if (source.CardId == TimewarpedPiperCardId)
            {
                source.Counters.TryGetValue("timewarped_piper_triggers", out var triggers);
                if (triggers >= (source.Golden ? 4 : 2))
                {
                    return;
                }

                source.Counters["timewarped_piper_triggers"] = triggers + 1;
                AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemAttack, source.CardId, null, source.Golden ? 2 : 1, source.InstanceId);
            }
            else if (source.CardId == TimewarpedChimeraCardId)
            {
                var multiplier = source.Golden ? 2 : 1;
                var targets = SelectOneOfEachFriendlyCombatType(owner);
                foreach (var target in targets)
                {
                    BuffMinion(target, 2 * multiplier, 1 * multiplier, "Timewarped Chimera");
                    AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, source.CardId, target.InstanceId, 1, 2 * multiplier, 1 * multiplier, source.InstanceId);
                }
            }
        }

        private static void ResolveTigerCarvingDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            var tavern = owner?.Tavern;
            if (!tookDamage ||
                tavern == null ||
                (tavern.TrinketTigerCarvingAttack <= 0 && tavern.TrinketTigerCarvingHealth <= 0) ||
                string.IsNullOrEmpty(damagedId))
            {
                return;
            }

            var candidates = owner.Board
                .Where(minion => IsAlive(minion) && !string.Equals(minion.InstanceId, damagedId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var target = new SeededRng(context.Seed + context.AttackSequence * 613 + candidates.Count).Pick(candidates);
            BuffMinion(target, tavern.TrinketTigerCarvingAttack, tavern.TrinketTigerCarvingHealth, "Tiger Carving");
            AddTargetedReward(
                context.Log,
                owner,
                CombatRewardType.BuffOriginalFriendlyMinion,
                tavern.TrinketTigerCarvingAttack >= 6 ? TigerCarvingGreaterCardId : TigerCarvingCardId,
                target.InstanceId,
                1,
                tavern.TrinketTigerCarvingAttack,
                tavern.TrinketTigerCarvingHealth,
                damagedId);
            AddLog(context.Log, "TrinketDamageTriggered", "Tiger Carving buffed " + target.InstanceId, damagedId, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                "Tiger Carving buffed " + target.InstanceId,
                owner.Side,
                damagedId,
                owner.Side,
                target.InstanceId,
                new[] { damagedId, target.InstanceId },
                new[] { damagedId },
                null,
                null,
                new[] { target.InstanceId });
        }

        private static void ResolveTrinketDivineShieldLostTriggers(CombatContext context, CombatSideState owner, string minionId, bool shieldBroken)
        {
            var tavern = owner.Tavern;
            if (!shieldBroken || tavern == null || string.IsNullOrEmpty(minionId))
            {
                return;
            }

            var target = owner.Board.FirstOrDefault(minion => minion.InstanceId == minionId && IsAlive(minion));
            if (tavern.TrinketDivineSignetUses > 0)
            {
                tavern.TrinketDivineSignetUses -= 1;
                AddReward(context.Log, owner, CombatRewardType.AddRandomTavernSpellToHand, DivineSignetCardId, null, 1, minionId);
                AddLog(context.Log, "TrinketDivineShieldLost", "Divine Signet queued a random Tavern spell", DivineSignetCardId, minionId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.DamageTriggered,
                    "Divine Signet queued a random Tavern spell",
                    owner.Side,
                    minionId,
                    owner.Side,
                    minionId,
                    new[] { minionId, DivineSignetCardId },
                    null,
                    null,
                    null,
                    new[] { DivineSignetCardId },
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    1,
                    false,
                    4 - tavern.TrinketDivineSignetUses,
                        4);
            }

            foreach (var gemsplitter in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == TimewarpedGemsplitterCardId).ToList())
            {
                AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemAttack, gemsplitter.CardId, null, gemsplitter.Golden ? 2 : 1, minionId);
            }

            if (target == null ||
                tavern.TrinketMechagonAdapterUses <= 0 ||
                !(target.Tribes.Contains(Tribe.Mech) || target.Tribes.Contains(Tribe.All)))
            {
                return;
            }

            tavern.TrinketMechagonAdapterUses -= 1;
            AddKeyword(target, Keyword.DivineShield);
            AddLog(context.Log, "TrinketDivineShieldLost", "Mechagon Adapter restored Divine Shield to " + minionId, MechagonAdapterCardId, minionId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                "Mechagon Adapter restored Divine Shield to " + minionId,
                owner.Side,
                minionId,
                owner.Side,
                minionId,
                new[] { minionId, MechagonAdapterCardId },
                null,
                null,
                null,
                new[] { MechagonAdapterCardId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                1,
                false,
                3 - tavern.TrinketMechagonAdapterUses,
                3);
        }

        private static void ResolveTrinketVenomousLost(CombatContext context, CombatSideState owner, MinionInstance target)
        {
            var tavern = owner.Tavern;
            if (target == null ||
                tavern == null ||
                (tavern.TrinketBelcherPortraitAttack <= 0 && tavern.TrinketBelcherPortraitHealth <= 0))
            {
                return;
            }

            var sourceCardId = string.IsNullOrWhiteSpace(tavern.TrinketBelcherPortraitSourceCardId)
                ? BelcherPortraitCardId
                : tavern.TrinketBelcherPortraitSourceCardId;
            if (target.Health > 0)
            {
                BuffMinion(target, tavern.TrinketBelcherPortraitAttack, tavern.TrinketBelcherPortraitHealth, "Belcher Portrait");
            }

            AddTargetedReward(
                context.Log,
                owner,
                CombatRewardType.BuffOriginalFriendlyMinion,
                sourceCardId,
                target.InstanceId,
                1,
                tavern.TrinketBelcherPortraitAttack,
                tavern.TrinketBelcherPortraitHealth,
                target.InstanceId);
            AddLog(context.Log, "TrinketVenomousLost", "Belcher Portrait buffed " + target.InstanceId + " after Venomous was lost", sourceCardId, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AttackTriggered,
                "Belcher Portrait buffed " + target.InstanceId + " after Venomous was lost",
                owner.Side,
                sourceCardId,
                owner.Side,
                target.InstanceId,
                new[] { sourceCardId, target.InstanceId },
                new[] { target.InstanceId },
                null,
                null,
                new[] { sourceCardId });
        }

        private static void ResolveWyvernDamageRefreshTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && minion.CardId == WyvernFrontmanCardId);
            if (source == null)
            {
                return;
            }

            source.Counters.TryGetValue("wyvern_refreshes", out var triggers);
            if (triggers >= 3)
            {
                return;
            }

            source.Counters["wyvern_refreshes"] = triggers + 1;
            AddReward(context.Log, owner, CombatRewardType.GainFreeRefresh, source.CardId, null, source.Golden ? 2 : 1);
            AddLog(context.Log, "DamageTriggered", source.InstanceId + " gained free refresh", source.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                source.InstanceId + " gained free refresh",
                owner.Side,
                source.InstanceId,
                owner.Side,
                source.InstanceId,
                new[] { source.InstanceId },
                new[] { source.InstanceId },
                null,
                null,
                new[] { source.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                1,
                0,
                false,
                triggers + 1,
                3);
        }

        private static void ResolveSilkyShimmermothDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage || owner.Tavern == null)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && minion.CardId == SilkyShimmermothCardId);
            if (source == null)
            {
                return;
            }

            owner.Tavern.BeetleAttackBonus += source.Golden ? 4 : 2;
            owner.Tavern.BeetleHealthBonus += source.Golden ? 2 : 1;
            AddLog(context.Log, "DamageTriggered", source.InstanceId + " improved Beetles", source.InstanceId, null, LogSeverity.Good);
        }

        private static void ResolveOverkillTriggers(
            CombatContext context,
            CombatSideState attackerOwner,
            MinionInstance attacker,
            CombatSideState defenderOwner,
            int defenderIndex,
            int defenderHealthBeforeDamage,
            DamageResult defenderDamage)
        {
            if (attacker.CardId != WildfireElementalCardId || !defenderDamage.CombatDamageDealt || defenderDamage.Minion.Health > 0)
            {
                return;
            }

            var excess = Math.Max(0, attacker.Attack - defenderHealthBeforeDamage);
            if (excess <= 0)
            {
                return;
            }

            var targets = new List<MinionInstance>();
            if (defenderIndex - 1 >= 0 && defenderIndex - 1 < defenderOwner.Board.Count)
            {
                targets.Add(defenderOwner.Board[defenderIndex - 1]);
            }

            if (defenderIndex + 1 >= 0 && defenderIndex + 1 < defenderOwner.Board.Count)
            {
                targets.Add(defenderOwner.Board[defenderIndex + 1]);
            }

            var damagedIds = new List<string>();
            foreach (var target in targets.Where(IsAlive))
            {
                var result = DealDamage(target, StatMath.SaturatingMultiply(excess, attacker.Golden ? 2 : 1, 0, StatMath.MaxStat), false);
                ReplaceByInstanceId(defenderOwner.Board, result.Minion);
                if (result.CombatDamageDealt || result.DivineShieldBroken)
                {
                    damagedIds.Add(target.InstanceId);
                }

                if (attacker.CardId == TimewarpedCollectorCardId)
                {
                    ResolveDamageTriggers(
                        context,
                        attackerOwner,
                        attacker.InstanceId,
                        false,
                        false,
                        defenderOwner,
                        target.InstanceId,
                        result.CombatDamageDealt,
                        result.DivineShieldBroken);
                }
                else
                {
                    ResolveWyvernDamageRefreshTrigger(context, defenderOwner, target.InstanceId, result.CombatDamageDealt);
                }
            }

            if (damagedIds.Count == 0)
            {
                return;
            }

            AddLog(context.Log, "DamageTriggered", attacker.InstanceId + " dealt excess damage", attacker.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                attacker.InstanceId + " dealt excess damage",
                attackerOwner.Side,
                attacker.InstanceId,
                defenderOwner.Side,
                null,
                damagedIds.Concat(new[] { attacker.InstanceId }),
                damagedIds,
                null,
                null,
                new[] { attacker.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                damagedIds.Count);
        }

        private static void ResolveCleaveDamage(CombatContext context, CombatSideState attackerOwner, MinionInstance attacker, CombatSideState defenderOwner, int defenderIndex)
        {
            if ((attacker.CardId != BladeCollectorCardId && attacker.CardId != TimewarpedCollectorCardId) || attacker.Attack <= 0)
            {
                return;
            }

            var damagedIds = new List<string>();
            foreach (var index in new[] { defenderIndex - 1, defenderIndex + 1 })
            {
                if (index < 0 || index >= defenderOwner.Board.Count)
                {
                    continue;
                }

                var target = defenderOwner.Board[index];
                if (!IsAlive(target))
                {
                    continue;
                }

                var result = DealDamage(target, attacker.Attack, attacker.Keywords.Contains(Keyword.Poisonous) || attacker.Keywords.Contains(Keyword.Venomous));
                ReplaceByInstanceId(defenderOwner.Board, result.Minion);
                if (result.CombatDamageDealt || result.DivineShieldBroken)
                {
                    damagedIds.Add(target.InstanceId);
                }

                ResolveWyvernDamageRefreshTrigger(context, defenderOwner, target.InstanceId, result.CombatDamageDealt);
            }

            if (damagedIds.Count == 0)
            {
                return;
            }

            AddLog(context.Log, "DamageTriggered", attacker.InstanceId + " cleaved adjacent targets", attacker.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                attacker.InstanceId + " cleaved adjacent targets",
                attackerOwner.Side,
                attacker.InstanceId,
                defenderOwner.Side,
                null,
                damagedIds.Concat(new[] { attacker.InstanceId }),
                damagedIds,
                null,
                null,
                new[] { attacker.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                damagedIds.Count);
        }

        private static void TriggerLeftmostOtherDeathrattle(CombatContext context, CombatSideState owner, MinionInstance attacker)
        {
            var target = owner.Board.FirstOrDefault(minion => minion.InstanceId != attacker.InstanceId && IsAlive(minion) && minion.Keywords.Contains(Keyword.Deathrattle));
            if (target == null)
            {
                return;
            }

            var index = owner.Board.FindIndex(minion => minion.InstanceId == target.InstanceId);
            var newEntityIds = new List<string>();
            ResolveDeathrattleSummons(context, owner, target, Math.Max(0, index + 1), newEntityIds);
            AddLog(context.Log, "AttackTriggered", attacker.InstanceId + " triggered " + target.InstanceId + " deathrattle", attacker.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static void CastChefChoiceOnRightNeighbor(CombatContext context, CombatSideState owner, MinionInstance attacker)
        {
            var attackerIndex = owner.Board.FindIndex(minion => minion.InstanceId == attacker.InstanceId);
            if (attackerIndex < 0 || attackerIndex + 1 >= owner.Board.Count)
            {
                return;
            }

            var target = owner.Board[attackerIndex + 1];
            var tribes = BoardTribeAnalyzer.GetCountedTribes(target);
            if (tribes.Count == 0)
            {
                return;
            }

            var tribe = tribes.Count == 1 ? tribes[0] : new SeededRng(context.Seed + context.AttackSequence + attackerIndex).Pick(tribes);
            AddReward(context.Log, owner, CombatRewardType.AddRandomSameTribeMinionToHand, attacker.CardId, tribe.ToString(), 1);
            AddLog(context.Log, "CombatSpellCast", attacker.InstanceId + " cast Chef's Choice on " + target.InstanceId + " (" + tribe + ")", attacker.InstanceId, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.CombatSpellCast,
                attacker.InstanceId + " cast Chef's Choice on " + target.InstanceId,
                owner.Side,
                attacker.InstanceId,
                owner.Side,
                target.InstanceId,
                new[] { attacker.InstanceId, target.InstanceId },
                new[] { target.InstanceId },
                null,
                null,
                new[] { attacker.InstanceId });
        }

        private static void ResolveHighTierRally(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            switch (attacker.CardId)
            {
                case CharmingWingCardId:
                    BuffAll(
                        owner.Board.Where(minion => minion.InstanceId != attacker.InstanceId && minion.Tribes.Contains(Tribe.Dragon)).Take(2),
                        0,
                        StatMath.SaturatingMultiply(attacker.MaxHealth, attacker.Golden ? 2 : 1, 0, StatMath.MaxStat),
                        "Charming Wing");
                    break;
                case DeadseaSmasherCardId:
                    BuffAll(owner.Board.Where(minion => minion.InstanceId != attacker.InstanceId).Take(attacker.Golden ? 6 : 3), attacker.Attack, 0, "Deadsea Smasher");
                    break;
                case QueenGuardCardId:
                    BuffAll(owner.Board, attacker.Golden ? 4 : 2, attacker.Golden ? 4 : 2, "Queen's Command");
                    BuffAll(owner.Board.Where(minion => minion.Tribes.Contains(Tribe.Naga)), attacker.Golden ? 4 : 2, attacker.Golden ? 4 : 2, "Queen's Command Naga");
                    break;
                case RheaSupremeWardenCardId:
                    AddReward(context.Log, owner, CombatRewardType.AddRandomTierSixMinionToHand, attacker.CardId, null, attacker.Golden ? 2 : 1);
                    break;
                case LastOfItsKindCardId:
                    var repeats = attacker.Golden ? 2 : 1;
                    for (var repeat = 0; repeat < repeats; repeat += 1)
                    {
                        BuffOneOfEachTribe(owner.Board, 12, 12, "The Last of Its Kind");
                    }

                    break;
                case ObsidianRavagerDragonCardId:
                    DamageRallyTargetAndNeighbor(context, owner, attacker, attackerIndex);
                    break;
                case RingingNagaCardId:
                    BuffMinion(attacker, attacker.Golden ? 4 : 2, attacker.Golden ? 4 : 2, "Shiny Ring");
                    break;
                case "BG35_700":
                    SummonSkyPirate(context, owner, attacker, attackerIndex);
                    break;
            }
        }

        private static void BuffOneOfEachTribe(IEnumerable<MinionInstance> board, int attack, int health, string sourceId)
        {
            var seen = new HashSet<Tribe>();
            foreach (var minion in board.Where(IsAlive))
            {
                var tribe = minion.Tribes.FirstOrDefault(candidate => candidate != Tribe.None && candidate != Tribe.All && !seen.Contains(candidate));
                if (tribe == Tribe.None)
                {
                    continue;
                }

                seen.Add(tribe);
                BuffMinion(minion, attack, health, sourceId);
            }
        }

        private static void DamageRallyTargetAndNeighbor(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            var defenderOwner = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            var targetIndex = defenderOwner.Board.FindIndex(IsAlive);
            if (targetIndex < 0)
            {
                return;
            }

            var indexes = attacker.Golden
                ? new[] { targetIndex - 1, targetIndex, targetIndex + 1 }
                : new[] { targetIndex, targetIndex + 1 };
            var damagedIds = new List<string>();
            foreach (var index in indexes.Distinct())
            {
                if (index < 0 || index >= defenderOwner.Board.Count)
                {
                    continue;
                }

                var target = defenderOwner.Board[index];
                if (!IsAlive(target))
                {
                    continue;
                }

                var result = DealDamage(target, attacker.Attack, false);
                ReplaceByInstanceId(defenderOwner.Board, result.Minion);
                if (result.CombatDamageDealt || result.DivineShieldBroken)
                {
                    damagedIds.Add(target.InstanceId);
                }
            }

            if (damagedIds.Count > 0)
            {
                AddLog(context.Log, "RallyResolved", attacker.InstanceId + " ravaged adjacent targets", attacker.InstanceId, null, LogSeverity.Good);
            }
        }

        private static void SummonSkyPirate(CombatContext context, CombatSideState owner, MinionInstance attacker, int attackerIndex)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                return;
            }

            var token = AddToken(
                context,
                owner,
                attacker,
                Math.Min(attackerIndex + 1, owner.Board.Count),
                "sky-pirate",
                "Sky Pirate",
                StatMath.SaturatingAdd(attacker.Attack, owner.Tavern?.TrinketSkyPirateAttackBonus ?? 0, 0, StatMath.MaxStat),
                1,
                Tribe.Pirate);
            if (token == null)
            {
                return;
            }

            token.CardId = SkyPirateCardId;
            context.ImmediateAttacks.Enqueue(new ImmediateAttackRequest(owner.Side, token.InstanceId));
        }

        private static void ResolveToughOrcaDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage, bool shieldBroken)
        {
            if (!tookDamage)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && minion.CardId == ToughOrcaCardId && IsAlive(minion));
            if (source == null)
            {
                return;
            }

            var amount = source.Golden ? 2 : 1;
            var targets = owner.Board
                .Where(minion => minion.InstanceId != source.InstanceId && IsAlive(minion))
                .ToList();
            if (targets.Count == 0)
            {
                return;
            }

            foreach (var target in targets)
            {
                BuffMinion(target, amount, amount, "Tough Orca");
            }

            AddLog(context.Log, "DamageTriggered", source.InstanceId + " buffed other friendly minions", source.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.DamageTriggered,
                source.InstanceId + " buffed other friendly minions",
                owner.Side,
                source.InstanceId,
                owner.Side,
                null,
                targets.Select(target => target.InstanceId).Concat(new[] { source.InstanceId }),
                new[] { source.InstanceId },
                null,
                null,
                new[] { source.InstanceId },
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                1,
                shieldBroken ? 1 : 0,
                false);
        }

        private static void ResolveTrigoreDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage)
            {
                return;
            }

            var damaged = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && HasCountedTribe(minion, Tribe.Beast));
            if (damaged == null)
            {
                return;
            }

            foreach (var trigore in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == TrigoreTheLasherCardId && minion.InstanceId != damagedId))
            {
                BuffMinion(trigore, 0, trigore.Golden ? 4 : 2, "Trigore the Lasher");
                AddLog(context.Log, "DamageTriggered", trigore.InstanceId + " gained Health from damaged Beast", trigore.InstanceId, damagedId, LogSeverity.Good);
            }
        }

        private static void ResolveSkyfinDamageTrigger(CombatContext context, CombatSideState owner, string damagedId, bool tookDamage)
        {
            if (!tookDamage)
            {
                return;
            }

            var damaged = owner.Board.FirstOrDefault(minion => minion.InstanceId == damagedId && HasCountedTribe(minion, Tribe.Beast));
            if (damaged == null)
            {
                return;
            }

            foreach (var skyfin in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == SkyfinRaptorCardId))
            {
                var target = owner.Board.FirstOrDefault(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Beast) && minion.InstanceId != damagedId);
                if (target == null)
                {
                    continue;
                }

                BuffMinion(target, skyfin.Golden ? 6 : 3, skyfin.Golden ? 4 : 2, "Skyfin Raptor");
                AddLog(context.Log, "DamageTriggered", skyfin.InstanceId + " buffed " + target.InstanceId, skyfin.InstanceId, target.InstanceId, LogSeverity.Good);
            }
        }

        private static void ResolveDevoutSatyressDamageTrigger(CombatContext context, CombatSideState owner, string sourceId, bool dealtDamage)
        {
            if (!dealtDamage)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == sourceId && HasCountedTribe(minion, Tribe.Demon));
            if (source == null)
            {
                return;
            }

            foreach (var satyress in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == DevoutSatyressCardId && minion.InstanceId != source.InstanceId))
            {
                BuffMinion(satyress, satyress.Golden ? 2 : 1, satyress.Golden ? 4 : 2, "Devout Satyress");
                AddLog(context.Log, "DamageTriggered", satyress.InstanceId + " grew from Demon damage", satyress.InstanceId, sourceId, LogSeverity.Good);
            }
        }

        private static void ResolveRuinsLordDamageTrigger(CombatContext context, CombatSideState owner, string sourceId, bool dealtDamage)
        {
            if (!dealtDamage)
            {
                return;
            }

            var source = owner.Board.FirstOrDefault(minion => minion.InstanceId == sourceId && HasCountedTribe(minion, Tribe.Demon));
            if (source == null)
            {
                return;
            }

            foreach (var ruinsLord in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == RuinsLordCardId).ToList())
            {
                var attack = ruinsLord.Golden ? 4 : 2;
                var health = ruinsLord.Golden ? 2 : 1;
                BuffAll(owner.Board.Where(minion => minion.InstanceId != source.InstanceId), attack, health, "Ruins Lord");
                AddLog(context.Log, "DamageTriggered", ruinsLord.InstanceId + " rewarded Demon damage", ruinsLord.InstanceId, source.InstanceId, LogSeverity.Good);
            }
        }

        private static void ResolveAvenge(CombatContext context, CombatSideState owner, string deadId)
        {
            if (owner.Tavern != null)
            {
                ResolveQuestAvenge(context, owner, deadId);
                ResolveTrinketAvenge(context, owner, deadId);
            }

            if (owner.TemporaryAvengeBeastRewards > 0)
            {
                owner.AvengeCounters.TryGetValue("temporary-beast-revenge", out var temporaryCount);
                temporaryCount += 1;
                if (temporaryCount >= 4)
                {
                    temporaryCount = 0;
                    AddReward(context.Log, owner, CombatRewardType.AddRandomBeastToHand, "123553", null, owner.TemporaryAvengeBeastRewards);
                }

                owner.AvengeCounters["temporary-beast-revenge"] = temporaryCount;
            }

            ResolveOnyxiaAvenge(context, owner, deadId);

            var sources = owner.Board
                .Where(minion => IsAlive(minion) && (minion.Keywords.Contains(Keyword.Avenge) || minion.EffectIds.Contains("avenge_2_buff_self_2_2")))
                .ToList();
            foreach (var source in sources)
            {
                var threshold = source.Counters.TryGetValue("avenge_threshold", out var storedThreshold) ? Math.Max(1, storedThreshold) : GetDefaultAvengeThreshold(source);
                var count = source.Counters.TryGetValue("avenge_count", out var storedCount) ? storedCount + 1 : 1;
                var triggered = count >= threshold;
                if (triggered)
                {
                    source.Counters["avenge_count"] = 0;
                    AddReward(context.Log, owner, CombatRewardType.FriendlyAvengeTriggered, source.CardId, null, 1, source.InstanceId);
                    if (source.CardId == HatchingResearcherCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.AddRandomChromawhelpToHand, source.CardId, null, source.Golden ? 2 : 1);
                    }
                    else if (source.CardId == DreamingThornweaverCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemHealth, source.CardId, null, source.Golden ? 2 : 1);
                    }
                    else if (source.CardId == DrustfallenButcherCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.AddTavernSpellToHand, source.CardId, ButcheringCardNumber, source.Golden ? 2 : 1);
                    }
                    else if (source.CardId == EternalSummonerCardId)
                    {
                        var token = AddToken(context, owner, source, owner.Board.Count, "eternal-knight", "Eternal Knight", 4, 1, Tribe.Undead);
                        if (token != null)
                        {
                            token.CardId = EternalKnightCardId;
                            token.Keywords.Add(Keyword.Windfury);
                            AddLog(context.Log, "Avenge", source.InstanceId + " summoned Eternal Knight", source.InstanceId, token.InstanceId, LogSeverity.Good);
                        }
                    }
                    else if (source.CardId == BristlebachPortraitMinionCardId)
                    {
                        var gems = source.Golden ? 4 : 2;
                        var targets = owner.Tavern != null && owner.Tavern.TrinketBristlebachPortraitActive
                            ? owner.Board.Where(IsAlive).ToList()
                            : owner.Board.Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Quilboar)).ToList();
                        foreach (var target in targets)
                        {
                            for (var gem = 0; gem < gems; gem += 1)
                            {
                                ApplyBloodGem(target, owner.Tavern);
                            }
                        }
                    }
                    else if (source.CardId == DeadlySporebatCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.AddRandomSameTribeMinionToHand, source.CardId, Tribe.Undead.ToString(), source.Golden ? 2 : 1);
                    }
                    else if (source.CardId == ScreamingBansheeCardId)
                    {
                        source.Counters.TryGetValue("banshee_bonus", out var bonus);
                        bonus += source.Golden ? 2 : 1;
                        source.Counters["banshee_bonus"] = bonus;
                    }
                    else if (source.CardId == TimewarpedGeomancerCardId)
                    {
                        var multiplier = source.Golden ? 2 : 1;
                        AddReward(context.Log, owner, CombatRewardType.AddGeneratedSpellToHand, source.CardId, BloodGemCardId, multiplier);
                        AddReward(context.Log, owner, CombatRewardType.ImproveBloodGemStats, source.CardId, null, multiplier, 1, 1);
                    }
                    else if (source.CardId == TimewarpedDuskmawCardId)
                    {
                        var multiplier = source.Golden ? 2 : 1;
                        foreach (var dragon in owner.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Dragon)).ToList())
                        {
                            BuffMinion(dragon, 6 * multiplier, 2 * multiplier, "Timewarped Duskmaw");
                            AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, source.CardId, dragon.InstanceId, 1, 6 * multiplier, 2 * multiplier, source.InstanceId);
                        }
                    }
                    else if (source.CardId == TimewarpedNestSwarmerCardId)
                    {
                        ResolveTimewarpedNestSwarmer(context, owner, source, owner.Board.Count, new List<string>());
                    }
                    else if (source.CardId == TimewarpedMothershipCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.AddRandomProtossToHand, source.CardId, null, source.Golden ? 2 : 1, source.InstanceId);
                    }
                    else if (source.CardId == LurkerCardId)
                    {
                        var amount = source.Golden ? 2 : 1;
                        BuffMinion(source, amount, amount, "Lurker");
                        AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, source.CardId, source.InstanceId, 1, amount, amount, source.InstanceId);
                    }
                    else if (source.CardId == ProtossCarrierCardId)
                    {
                        var stats = source.Counters.TryGetValue("interceptor_stats", out var storedStats) ? Math.Max(7, storedStats) : 7;
                        var token = AddToken(context, owner, source, owner.Board.Count, ProtossInterceptorTokenId, "Interceptor", stats, stats, Tribe.None);
                        source.Counters["interceptor_stats"] = stats + (source.Golden ? 4 : 2);
                        if (token != null)
                        {
                            AddLog(context.Log, "Avenge", "Carrier summoned an Interceptor", source.InstanceId, token.InstanceId, LogSeverity.Good);
                        }
                    }
                    else if (source.CardId == ProtossMothershipCardId)
                    {
                        AddReward(context.Log, owner, CombatRewardType.AddRandomProtossToHand, source.CardId, null, source.Golden ? 2 : 1, source.InstanceId);
                    }
                    else
                    {
                        BuffMinion(source, source.Golden ? 4 : 2, source.Golden ? 4 : 2, "Avenge");
                    }
                }
                else
                {
                    source.Counters["avenge_count"] = count;
                }

                AddLog(context.Log, "AvengeCounterUpdated", source.InstanceId + " avenge " + count + "/" + threshold, source.InstanceId, deadId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.AvengeCounterUpdated,
                    source.InstanceId + " avenge " + count + "/" + threshold,
                    owner.Side,
                    source.InstanceId,
                    owner.Side,
                    source.InstanceId,
                    new[] { source.InstanceId, deadId },
                    null,
                    new[] { deadId },
                    null,
                    new[] { source.InstanceId },
                    null,
                    BoardSide.Player,
                    -1,
                    0,
                    0,
                    0,
                    0,
                    false,
                    count,
                    threshold);
            }
        }

        private static void ResolveOnyxiaAvenge(CombatContext context, CombatSideState owner, string deadId)
        {
            var tavern = owner?.Tavern;
            if (tavern == null || !tavern.HeroOnyxiaBroodmotherActive)
            {
                return;
            }

            owner.AvengeCounters.TryGetValue(OnyxiaAvengeCounter, out var count);
            count += 1;
            if (count < 4)
            {
                owner.AvengeCounters[OnyxiaAvengeCounter] = count;
                return;
            }

            owner.AvengeCounters[OnyxiaAvengeCounter] = 0;
            if (owner.Board.Count >= BoardLimit)
            {
                return;
            }

            tavern.HeroEffectCounters.TryGetValue(OnyxiaWhelpStatsCounter, out var stats);
            stats = Math.Max(1, stats);
            var token = AddToken(context, owner, null, owner.Board.Count, OnyxiaWhelpTokenId, "Whelp", stats, stats, Tribe.Dragon);
            tavern.HeroEffectCounters[OnyxiaWhelpStatsCounter] = StatMath.SaturatingAdd(stats, 1, 1, StatMath.MaxStat);
            if (token == null)
            {
                return;
            }

            context.ImmediateAttacks.Enqueue(new ImmediateAttackRequest(owner.Side, token.InstanceId));
            AddLog(context.Log, "ImmediateAttackQueued", token.InstanceId + " queued by Broodmother after " + deadId, OnyxiaPowerId, token.InstanceId, LogSeverity.Good);

            foreach (var buddy in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == ManyWhelpsCardId).ToList())
            {
                BuffMinion(buddy, 2, 2, "Many Whelps");
                AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, ManyWhelpsCardId, buddy.InstanceId, 1, 2, 2, token.InstanceId);
            }
        }

        private static void ResolveTrinketAvenge(CombatContext context, CombatSideState owner, string deadId)
        {
            var tavern = owner.Tavern;
            if (tavern == null)
            {
                return;
            }

            if (tavern.TrinketBirdFeederAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-bird-feeder", tavern.TrinketBirdFeederAvengeThreshold, "Bird Feeder", deadId))
            {
                BuffAll(owner.Board, tavern.TrinketBirdFeederAttack, tavern.TrinketBirdFeederHealth, "Bird Feeder");
                AddLog(context.Log, "TrinketAvenge", "Bird Feeder gave your minions +" + tavern.TrinketBirdFeederAttack + "/+" + tavern.TrinketBirdFeederHealth, null, deadId, LogSeverity.Good);
            }

            if (tavern.TrinketBeetleBandAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-beetle-band", tavern.TrinketBeetleBandAvengeThreshold, "Beetle Band", deadId))
            {
                for (var index = 0; index < tavern.TrinketBeetleBandSummonCount; index += 1)
                {
                    var token = AddToken(context, owner, null, owner.Board.Count, "trinket-beetle", "Beetle", 2, 2, Tribe.Beast, Keyword.Taunt);
                    if (token != null)
                    {
                        AddLog(context.Log, "TrinketAvenge", "Beetle Band summoned " + token.InstanceId, null, token.InstanceId, LogSeverity.Good);
                    }
                }
            }

            if (tavern.TrinketQuilligraphyAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-quilligraphy-set", tavern.TrinketQuilligraphyAvengeThreshold, "Quilligraphy Set", deadId))
            {
                tavern.BloodGemBonusAttack = StatMath.SaturatingAdd(tavern.BloodGemBonusAttack, tavern.TrinketQuilligraphyAttack, 0, StatMath.MaxStat);
                tavern.BloodGemBonusHealth = StatMath.SaturatingAdd(tavern.BloodGemBonusHealth, tavern.TrinketQuilligraphyHealth, 0, StatMath.MaxStat);
                AddLog(context.Log, "TrinketAvenge", "Quilligraphy Set improved Blood Gems +" + tavern.TrinketQuilligraphyAttack + "/+" + tavern.TrinketQuilligraphyHealth, null, deadId, LogSeverity.Good);
            }

            if (tavern.TrinketWickedTomeAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-wicked-tome", tavern.TrinketWickedTomeAvengeThreshold, "Wicked Tome", deadId))
            {
                tavern.TavernSpellBonusAttack = StatMath.SaturatingAdd(tavern.TavernSpellBonusAttack, tavern.TrinketWickedTomeAttack, 0, StatMath.MaxStat);
                tavern.TavernSpellBonusHealth = StatMath.SaturatingAdd(tavern.TavernSpellBonusHealth, tavern.TrinketWickedTomeHealth, 0, StatMath.MaxStat);
                AddLog(context.Log, "TrinketAvenge", "Wicked Tome improved Tavern spells +" + tavern.TrinketWickedTomeAttack + "/+" + tavern.TrinketWickedTomeHealth, null, deadId, LogSeverity.Good);
            }

            if (tavern.TrinketStaffOfTheScourgeAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-staff-of-the-scourge", tavern.TrinketStaffOfTheScourgeAvengeThreshold, "Staff of the Scourge", deadId))
            {
                GiveRandomFriendlyUndeadReborn(context, owner, deadId);
            }

            if (tavern.TrinketCloudSerpentHornAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-cloud-serpent-horn", tavern.TrinketCloudSerpentHornAvengeThreshold, "Cloud Serpent Horn", deadId))
            {
                GiveRightmostAttackToDragon(context, owner, deadId);
            }

            if (tavern.TrinketFridgeMagnetAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-fridge-magnet", tavern.TrinketFridgeMagnetAvengeThreshold, "Fridge Magnet", deadId))
            {
                AddReward(context.Log, owner, CombatRewardType.AddRandomMagneticMechToHand, "BG30_MagicItem_545", null, 1);
                AddLog(context.Log, "TrinketAvenge", "Fridge Magnet queued a random Magnetic minion", null, deadId, LogSeverity.Good);
            }

            if (tavern.TrinketBattleHornAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-battle-horn", tavern.TrinketBattleHornAvengeThreshold, "Battle Horn", deadId))
            {
                AddReward(context.Log, owner, CombatRewardType.TriggerFriendlyBattlecry, "BG32_MagicItem_415", null, 1);
                AddLog(context.Log, "TrinketAvenge", "Battle Horn queued a friendly Battlecry trigger", null, deadId, LogSeverity.Good);
            }

            if (tavern.TrinketGilneanRoseAvengeThreshold > 0 &&
                AdvanceTrinketAvengeCounter(context, owner, "trinket-gilnean-thorned-rose", tavern.TrinketGilneanRoseAvengeThreshold, "Gilnean Thorned Rose", deadId))
            {
                ResolveGilneanThornedRose(context, owner, deadId);
            }
        }

        private static void ResolveGilneanThornedRose(CombatContext context, CombatSideState owner, string deadId)
        {
            var tavern = owner.Tavern;
            if (tavern == null)
            {
                return;
            }

            var targets = owner.Board.Where(IsAlive).ToList();
            foreach (var target in targets)
            {
                BuffMinion(target, tavern.TrinketGilneanRoseAttack, tavern.TrinketGilneanRoseHealth, "Gilnean Thorned Rose");
                AddTargetedReward(
                    context.Log,
                    owner,
                    CombatRewardType.BuffOriginalFriendlyMinion,
                    GilneanThornedRoseCardId,
                    target.InstanceId,
                    1,
                    tavern.TrinketGilneanRoseAttack,
                    tavern.TrinketGilneanRoseHealth,
                    deadId);

                var result = DealDamage(target, 1, false);
                if (result.CombatDamageDealt || result.DivineShieldBroken)
                {
                    ReplaceByInstanceId(owner.Board, result.Minion);
                    if (result.Minion.Health <= 0)
                    {
                        MarkKilledBy(result.Minion, GilneanThornedRoseCardId, owner.Side, GilneanThornedRoseCardId);
                    }
                }
            }

            AddLog(context.Log, "TrinketAvenge", "Gilnean Thorned Rose buffed and damaged " + targets.Count + " minion(s)", GilneanThornedRoseCardId, deadId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AvengeProgressed,
                "Gilnean Thorned Rose buffed and damaged " + targets.Count + " minion(s)",
                owner.Side,
                GilneanThornedRoseCardId,
                owner.Side,
                targets.FirstOrDefault()?.InstanceId,
                targets.Select(target => target.InstanceId).Concat(new[] { deadId, GilneanThornedRoseCardId }),
                targets.Select(target => target.InstanceId),
                new[] { deadId },
                null,
                targets.Select(target => target.InstanceId),
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                targets.Count,
                0,
                false,
                targets.Count,
                tavern.TrinketGilneanRoseAvengeThreshold);
            ResolveDeaths(context, owner.Side);
        }

        private static void ResolveAllianceKeychain(CombatContext context, CombatSideState owner, MinionInstance dead)
        {
            var tavern = owner.Tavern;
            if (tavern == null || dead == null || tavern.TrinketAllianceKeychainTargets <= 0)
            {
                return;
            }

            var requestedTargets = tavern.TrinketAllianceKeychainTargets;
            tavern.TrinketAllianceKeychainTargets = 0;
            var candidates = owner.Board
                .Where(minion => IsAlive(minion))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var attack = Math.Max(0, dead.Attack);
            var health = Math.Max(0, dead.MaxHealth);
            var targetCount = Math.Min(requestedTargets, candidates.Count);
            var rng = new SeededRng(context.Seed + context.AttackSequence * 997 + attack * 31 + health * 17 + candidates.Count);
            var targets = new List<MinionInstance>();
            for (var index = 0; index < targetCount; index += 1)
            {
                var target = rng.Pick(candidates);
                candidates.Remove(target);
                BuffMinion(target, attack, health, "Alliance Keychain");
                targets.Add(target);
            }

            var targetIds = targets.Select(target => target.InstanceId).ToList();
            AddLog(context.Log, "TrinketDeathTriggered", "Alliance Keychain gave +" + attack + "/+" + health + " to " + targetCount + " friendly minion(s)", dead.InstanceId, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AvengeProgressed,
                "Alliance Keychain gave +" + attack + "/+" + health + " to " + targetCount + " friendly minion(s)",
                owner.Side,
                dead.InstanceId,
                owner.Side,
                targetIds.FirstOrDefault(),
                targetIds.Concat(new[] { dead.InstanceId }),
                null,
                new[] { dead.InstanceId },
                null,
                targetIds);
        }

        private static void GiveRandomFriendlyUndeadReborn(CombatContext context, CombatSideState owner, string deadId)
        {
            var candidates = owner.Board
                .Where(minion => IsAlive(minion) && minion.Tribes.Contains(Tribe.Undead) && !minion.Keywords.Contains(Keyword.Reborn))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var target = new SeededRng(context.Seed + context.AttackSequence + candidates.Count).Pick(candidates);
            target.Keywords.Add(Keyword.Reborn);
            AddLog(context.Log, "TrinketAvenge", "Staff of the Scourge gave Reborn to " + target.InstanceId, null, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AvengeProgressed,
                "Staff of the Scourge gave Reborn to " + target.InstanceId,
                owner.Side,
                null,
                owner.Side,
                target.InstanceId,
                new[] { target.InstanceId, deadId },
                null,
                new[] { deadId },
                null,
                new[] { target.InstanceId });
        }

        private static void GiveRightmostAttackToDragon(CombatContext context, CombatSideState owner, string deadId)
        {
            var source = owner.Board.LastOrDefault(IsAlive);
            if (source == null)
            {
                return;
            }

            var candidates = owner.Board
                .Where(minion => minion.InstanceId != source.InstanceId && IsAlive(minion) && minion.Tribes.Contains(Tribe.Dragon))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var target = new SeededRng(context.Seed + context.AttackSequence + source.Attack + candidates.Count).Pick(candidates);
            BuffMinion(target, source.Attack, 0, "Cloud Serpent Horn");
            AddLog(context.Log, "TrinketAvenge", "Cloud Serpent Horn gave " + source.Attack + " Attack to " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AvengeProgressed,
                "Cloud Serpent Horn gave " + source.Attack + " Attack to " + target.InstanceId,
                owner.Side,
                source.InstanceId,
                owner.Side,
                target.InstanceId,
                new[] { source.InstanceId, target.InstanceId, deadId },
                null,
                new[] { deadId },
                null,
                new[] { target.InstanceId });
        }

        private static bool AdvanceTrinketAvengeCounter(CombatContext context, CombatSideState owner, string counterKey, int threshold, string sourceName, string deadId)
        {
            if (threshold <= 0)
            {
                return false;
            }

            owner.AvengeCounters.TryGetValue(counterKey, out var count);
            count += 1;
            var displayCount = count;
            var triggered = count >= threshold;
            if (triggered)
            {
                count = 0;
                displayCount = threshold;
            }

            owner.AvengeCounters[counterKey] = count;
            AddLog(context.Log, "TrinketAvengeCounterUpdated", sourceName + " avenge " + displayCount + "/" + threshold, null, deadId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AvengeCounterUpdated,
                sourceName + " avenge " + displayCount + "/" + threshold,
                owner.Side,
                null,
                owner.Side,
                null,
                new[] { deadId },
                null,
                new[] { deadId },
                null,
                null,
                null,
                BoardSide.Player,
                -1,
                0,
                0,
                0,
                0,
                false,
                displayCount,
                threshold);
            return triggered;
        }

        private static void ResolveQuestAvenge(CombatContext context, CombatSideState owner, string deadId)
        {
            if (owner.Tavern == null)
            {
                return;
            }

            if (owner.Tavern.QuestBoomSquadActive)
            {
                owner.AvengeCounters.TryGetValue("quest-boom-squad", out var boomCount);
                boomCount += 1;
                if (boomCount >= 3)
                {
                    boomCount = 0;
                    var enemy = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
                    var target = enemy.Board.Where(IsAlive).OrderByDescending(minion => minion.Health).FirstOrDefault();
                    if (target != null)
                    {
                        var result = DealDamage(target, 10, false);
                        ReplaceByInstanceId(enemy.Board, result.Minion);
                        AddLog(context.Log, "QuestAvenge", "Boom Squad dealt 10 damage to " + target.InstanceId, null, target.InstanceId, LogSeverity.Good);
                        if (!IsAlive(result.Minion))
                        {
                            ResolveDeaths(context, enemy.Side);
                        }
                    }
                }

                owner.AvengeCounters["quest-boom-squad"] = boomCount;
            }

            if (owner.Tavern.QuestGrimFreshenerActive)
            {
                owner.AvengeCounters.TryGetValue("quest-grim-freshener", out var freshenerCount);
                freshenerCount += 1;
                if (freshenerCount >= 2)
                {
                    freshenerCount = 0;
                    AddReward(context.Log, owner, CombatRewardType.GainFreeRefresh, "BG33_Reward_004", null, 1);
                }

                owner.AvengeCounters["quest-grim-freshener"] = freshenerCount;
            }

            if (owner.Tavern.QuestCycleOfEnergyActive)
            {
                owner.AvengeCounters.TryGetValue("quest-cycle-of-energy", out var cycleCount);
                cycleCount += 1;
                if (cycleCount >= 3)
                {
                    cycleCount = 0;
                    AddReward(context.Log, owner, CombatRewardType.AddRandomTavernSpellToHand, "BG28_Reward_504", null, 1);
                }

                owner.AvengeCounters["quest-cycle-of-energy"] = cycleCount;
            }

            if (owner.Tavern.QuestTumblingAttack > 0 || owner.Tavern.QuestTumblingHealth > 0)
            {
                owner.AvengeCounters.TryGetValue("quest-tumbling-disaster", out var tumblingCount);
                tumblingCount += 1;
                if (tumblingCount >= 4)
                {
                    tumblingCount = 0;
                    owner.Tavern.QuestTumblingAvengeAttack += 1;
                    owner.Tavern.QuestTumblingAvengeHealth += 1;
                    owner.Tavern.QuestTumblingAttack += 1;
                    owner.Tavern.QuestTumblingHealth += 1;
                    AddLog(context.Log, "QuestAvenge", "Tumbling Disaster improved permanently", null, deadId, LogSeverity.Good);
                }

                owner.AvengeCounters["quest-tumbling-disaster"] = tumblingCount;
            }

            if (owner.Tavern.QuestStableAmalgamationActive)
            {
                owner.AvengeCounters.TryGetValue("quest-stable-amalgamation", out var amalgamCount);
                amalgamCount += 1;
                if (amalgamCount >= 7)
                {
                    amalgamCount = 0;
                    var token = AddToken(context, owner, null, owner.Board.Count, "stable-amalgam", "Stable Amalgam", 50, 50, Tribe.All);
                    if (token != null)
                    {
                        AddLog(context.Log, "QuestAvenge", "Stable Amalgamation summoned " + token.InstanceId, null, token.InstanceId, LogSeverity.Good);
                    }
                }

                owner.AvengeCounters["quest-stable-amalgamation"] = amalgamCount;
            }
        }

        private static int GetDefaultAvengeThreshold(MinionInstance source)
        {
            if (source.CardId == DrustfallenButcherCardId)
            {
                return 3;
            }

            if (source.CardId == EternalSummonerCardId)
            {
                return 5;
            }

            if (source.CardId == ScreamingBansheeCardId)
            {
                return 2;
            }

            if (source.CardId == HatchingResearcherCardId || source.CardId == DreamingThornweaverCardId)
            {
                return 3;
            }

            if (source.CardId == DeadlySporebatCardId)
            {
                return 4;
            }

            if (source.CardId == TimewarpedGeomancerCardId)
            {
                return 5;
            }

            if (source.CardId == TimewarpedPashmarCardId)
            {
                return 3;
            }

            if (source.CardId == TimewarpedCopterCardId ||
                source.CardId == TimewarpedStormcloudCardId)
            {
                return 3;
            }

            if (source.CardId == TimewarpedRecyclerCardId ||
                source.CardId == TimewarpedTravelerCardId)
            {
                return 4;
            }

            if (source.CardId == TimewarpedMothershipCardId)
            {
                return 4;
            }

            if (source.CardId == TimewarpedPrismscaleCardId)
            {
                return 2;
            }

            if (source.CardId == TimewarpedDuskmawCardId || source.CardId == TimewarpedNestSwarmerCardId)
            {
                return 1;
            }

            return 2;
        }

        private static int GetDeathrattleRepeats(CombatSideState owner)
        {
            var extra = owner.Board
                .Where(minion => IsAlive(minion) && minion.CardId == TitusRivendareCardId)
                .Sum(minion => minion.Golden ? 2 : 1);
            extra += GetTimewarpedDeiosExtraTriggers(owner);
            extra += Math.Max(0, owner.Tavern?.QuestDeathrattleExtraTriggers ?? 0);
            if ((owner.Tavern?.TrinketDeathlyPhylacteryExtraDeathrattles ?? 0) > 0)
            {
                extra += owner.Tavern.TrinketDeathlyPhylacteryExtraDeathrattles;
                owner.Tavern.TrinketDeathlyPhylacteryExtraDeathrattles = 0;
            }

            return 1 + extra;
        }

        private static int GetTimewarpedDeiosExtraTriggers(CombatSideState owner)
        {
            return owner.Board
                .Where(minion => IsAlive(minion) && minion.CardId == TimewarpedDeiosCardId)
                .Sum(minion => minion.Golden ? 2 : 1);
        }

        private static void AddReward(List<CombatLogEntry> log, CombatSideState owner, CombatRewardType type, string sourceCardId, string cardId, int amount, string sourceInstanceId = null)
        {
            AddReward(log, owner, type, sourceCardId, cardId, amount, 0, 0, sourceInstanceId);
        }

        private static void AddReward(List<CombatLogEntry> log, CombatSideState owner, CombatRewardType type, string sourceCardId, string cardId, int amount, int attack, int health, string sourceInstanceId = null)
        {
            if (amount <= 0)
            {
                return;
            }

            owner.Rewards.Add(new CombatReward
            {
                Type = type,
                Side = owner.Side,
                SourceCardId = sourceCardId,
                SourceInstanceId = sourceInstanceId,
                CardId = cardId,
                Amount = amount,
                Attack = attack,
                Health = health
            });
            AddLog(log, "CombatRewardQueued", type + " x" + amount + " from " + sourceCardId, sourceCardId, cardId, LogSeverity.Good);
        }

        private static void AddTargetedReward(List<CombatLogEntry> log, CombatSideState owner, CombatRewardType type, string sourceCardId, string targetInstanceId, int amount, int attack, int health, string sourceInstanceId = null)
        {
            if (amount <= 0)
            {
                return;
            }

            owner.Rewards.Add(new CombatReward
            {
                Type = type,
                Side = owner.Side,
                SourceCardId = sourceCardId,
                SourceInstanceId = sourceInstanceId,
                TargetInstanceId = targetInstanceId,
                Amount = amount,
                Attack = attack,
                Health = health
            });
            AddLog(log, "CombatRewardQueued", type + " x" + amount + " from " + sourceCardId + " to " + targetInstanceId, sourceCardId, targetInstanceId, LogSeverity.Good);
        }

        private static void QueueDamagedMinionRewards(List<CombatLogEntry> log, CombatSideState owner, MinionInstance damaged, bool tookDamage)
        {
            if (!tookDamage || damaged == null)
            {
                return;
            }

            if (damaged.CardId == VeryHungryWinterfinnerCardId)
            {
                AddReward(
                    log,
                    owner,
                    CombatRewardType.BuffHandMinion,
                    damaged.CardId,
                    null,
                    1,
                    damaged.Golden ? 4 : 2,
                    damaged.Golden ? 2 : 1);
            }

        }

        private static void PlayBloodGemsOnFriendlyQuilboar(CombatSideState owner, int count)
        {
            if (owner == null || count <= 0)
            {
                return;
            }

            foreach (var target in owner.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Quilboar)).ToList())
            {
                for (var gem = 0; gem < count; gem += 1)
                {
                    ApplyBloodGem(target, owner.Tavern);
                }
            }
        }

        private static MinionInstance AddImmediateAttackHatchling(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds = null)
        {
            var token = AddToken(context, owner, source, insertIndex, "hatchling", "Hatchling", 3, 3, Tribe.Dragon);
            if (token != null)
            {
                newEntityIds?.Add(token.InstanceId);
                context.ImmediateAttacks.Enqueue(new ImmediateAttackRequest(owner.Side, token.InstanceId));
                AddLog(context.Log, "ImmediateAttackQueued", token.InstanceId + " queued", token.InstanceId, null, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.ImmediateAttackQueued,
                    token.InstanceId + " queued",
                    owner.Side,
                    token.InstanceId,
                    owner.Side,
                    null,
                    new[] { source.InstanceId, token.InstanceId },
                    null,
                    null,
                    null,
                    new[] { source.InstanceId });
            }

            return token;
        }

        private static void BuffFirstFriendly(IEnumerable<MinionInstance> candidates, int attack, int health, string sourceId)
        {
            var target = candidates.FirstOrDefault();
            if (target == null)
            {
                return;
            }

            BuffMinion(target, attack, health, sourceId);
        }

        private static void BuffAll(IEnumerable<MinionInstance> candidates, int attack, int health, string sourceId)
        {
            foreach (var target in candidates.Where(IsAlive).ToList())
            {
                BuffMinion(target, attack, health, sourceId);
            }
        }

        private static void DestroyLargestEnemy(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var opponent = context.Get(owner.Side == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);
            var target = opponent.Board
                .Where(IsAlive)
                .OrderByDescending(minion => minion.Attack + minion.MaxHealth)
                .FirstOrDefault();
            if (target == null)
            {
                return;
            }

            target.Health = 0;
            AddLog(context.Log, "DeathrattleResolved", source.InstanceId + " destroyed " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
        }

        private static void DestroyKiller(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var killerId = GetKillerId(source);
            if (string.IsNullOrEmpty(killerId))
            {
                return;
            }

            var killerOwner = context.Player.Board.Any(minion => minion.InstanceId == killerId) ? context.Player : context.Opponent;
            var target = killerOwner.Board.FirstOrDefault(minion => minion.InstanceId == killerId && IsAlive(minion));
            if (target == null)
            {
                return;
            }

            target.Health = 0;
            AddLog(context.Log, "DeathrattleResolved", source.InstanceId + " destroyed killer " + target.InstanceId, source.InstanceId, target.InstanceId, LogSeverity.Good);
            if (killerOwner.Side != owner.Side)
            {
                ResolveDeaths(context, killerOwner.Side);
            }
        }

        private static int ResolveBoomControllerDeath(CombatContext context, CombatSideState owner, MinionInstance deadMinion, int insertIndex, List<string> newEntityIds)
        {
            if (owner.Tavern == null ||
                !owner.Tavern.TrinketBoomControllerActive ||
                owner.BoomControllerTriggered ||
                deadMinion == null ||
                !HasCountedTribe(deadMinion, Tribe.Mech))
            {
                return 0;
            }

            owner.BoomControllerTriggered = true;
            if (owner.Board.Count >= BoardLimit)
            {
                RecordSummonOverflow(context, owner, deadMinion, deadMinion.CardId, deadMinion.Name);
                return 0;
            }

            var copy = deadMinion.Clone();
            copy.InstanceId = "boom-controller-" + owner.Side + "-" + deadMinion.InstanceId;
            copy.Owner = owner.Side;
            copy.Health = Math.Max(1, copy.MaxHealth);
            copy.PoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            copy.AttacksThisCombat = 0;
            RemoveKillTags(copy);
            ApplySummonAuras(owner, copy);
            owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), copy);
            newEntityIds?.Add(copy.InstanceId);
            ResolveFriendlySummonTriggers(context, owner, copy, deadMinion);
            AddLog(context.Log, "MinionSummoned", "Boom Controller summoned exact copy of " + deadMinion.InstanceId, BoomControllerCardId, copy.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                "Boom Controller summoned exact copy of " + deadMinion.InstanceId,
                owner.Side,
                BoomControllerCardId,
                owner.Side,
                copy.InstanceId,
                new[] { BoomControllerCardId, deadMinion.InstanceId, copy.InstanceId },
                null,
                null,
                new[] { copy.InstanceId },
                new[] { BoomControllerCardId, deadMinion.InstanceId });
            return 1;
        }

        private static int ResolveBloodGolemStickerDeath(CombatContext context, CombatSideState owner, MinionInstance deadMinion, int insertIndex, List<string> newEntityIds)
        {
            if (owner.Tavern == null ||
                !owner.Tavern.TrinketBloodGolemStickerActive ||
                deadMinion == null ||
                !HasCountedTribe(deadMinion, Tribe.Quilboar))
            {
                return 0;
            }

            var bloodGemStats = GetBloodGemStats(deadMinion);
            if (bloodGemStats.Attack <= 0 && bloodGemStats.Health <= 0)
            {
                AddLog(context.Log, "TrinketDeathTriggered", "Blood Golem Sticker found no Blood Gem stats on " + deadMinion.InstanceId, BloodGolemStickerCardId, deadMinion.InstanceId, LogSeverity.Warning);
                return 0;
            }

            return AddTokenAndTrack(
                context,
                owner,
                deadMinion,
                insertIndex,
                newEntityIds,
                BloodGolemTokenId,
                "Blood Golem",
                Math.Max(0, bloodGemStats.Attack),
                Math.Max(1, bloodGemStats.Health),
                Tribe.None);
        }

        private static bool HasCountedTribe(MinionInstance minion, Tribe tribe)
        {
            return BoardTribeAnalyzer.HasTribe(minion, tribe);
        }

        private static List<MinionInstance> SelectOneOfEachFriendlyCombatType(CombatSideState owner)
        {
            return BoardTribeAnalyzer.SelectOneOfEachTribe(owner?.Board?.Where(IsAlive));
        }

        private static (int Attack, int Health) GetBloodGemStats(MinionInstance minion)
        {
            var attack = 0;
            var health = 0;
            foreach (var enchantment in minion?.Enchantments ?? Enumerable.Empty<Enchantment>())
            {
                if (!IsBloodGemEnchantment(enchantment))
                {
                    continue;
                }

                attack = StatMath.SaturatingAdd(attack, Math.Max(0, enchantment.AttackBonus), 0, StatMath.MaxStat);
                health = StatMath.SaturatingAdd(health, Math.Max(0, enchantment.HealthBonus), 0, StatMath.MaxStat);
            }

            return (attack, health);
        }

        private static bool IsBloodGemEnchantment(Enchantment enchantment)
        {
            return ContainsBloodGemText(enchantment?.SourceId) || ContainsBloodGemText(enchantment?.Id);
        }

        private static bool ContainsBloodGemText(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(BloodGemSourceId, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void RemoveKillTags(MinionInstance minion)
        {
            minion?.Tags?.RemoveAll(tag =>
                tag.StartsWith("killed_by:", StringComparison.Ordinal) ||
                tag.StartsWith("killed_by_side:", StringComparison.Ordinal) ||
                tag.StartsWith("killed_by_card:", StringComparison.Ordinal));
        }

        private static void TrackSTharaDemonDeath(CombatSideState owner, MinionInstance minion)
        {
            if (owner.Tavern == null ||
                !owner.Tavern.TrinketSTharaStickerActive ||
                owner.STharaStoredDemon != null ||
                minion == null ||
                !HasCountedTribe(minion, Tribe.Demon))
            {
                return;
            }

            var stored = minion.Clone();
            stored.InstanceId = "sthara-stored-" + owner.Side + "-" + minion.InstanceId;
            stored.Owner = owner.Side;
            stored.Health = Math.Max(1, stored.MaxHealth);
            stored.PoolSource = PoolSource.Summon;
            stored.PoolCopiesHeld = 0;
            stored.CanAttack = true;
            stored.AttacksThisCombat = 0;
            RemoveKillTags(stored);
            owner.STharaStoredDemon = stored;
        }

        private static void TrackDeadMech(CombatSideState owner, MinionInstance minion)
        {
            if (minion == null || !minion.Tribes.Contains(Tribe.Mech))
            {
                return;
            }

            owner.DeadMechPlainCopies.Add(CreatePlainCombatCopy(minion, "dead-mech-" + owner.DeadMechPlainCopies.Count));
        }

        private static int SummonDeadMechPlainCopies(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds, int count)
        {
            var inserted = 0;
            foreach (var deadMech in owner.DeadMechPlainCopies.Take(count).ToList())
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    RecordSummonOverflow(context, owner, source, deadMech.CardId, deadMech.Name);
                    continue;
                }

                var copy = CreatePlainCombatCopy(deadMech, "kangor-" + source.InstanceId + "-" + inserted);
                copy.Owner = owner.Side;
                ApplySummonAuras(owner, copy);
                owner.Board.Insert(Math.Min(insertIndex + inserted, owner.Board.Count), copy);
                ResolveFriendlySummonTriggers(context, owner, copy, source);
                newEntityIds?.Add(copy.InstanceId);
                inserted += 1;
                AddLog(context.Log, "MinionSummoned", source.InstanceId + " rebuilt " + copy.InstanceId, source.InstanceId, copy.InstanceId, LogSeverity.Good);
            }

            return inserted;
        }

        private static MinionInstance CreatePlainCombatCopy(MinionInstance source, string instanceId)
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
                CanAttack = true,
                Owner = source.Owner,
                Tribes = source.Tribes.ToList(),
                Keywords = source.Keywords.Where(keyword => keyword != Keyword.Reborn).ToList(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0
            };
        }

        private static void QueueFriendlyKillReward(CombatContext context, CombatSideState defeatedOwner, MinionInstance defeated)
        {
            var killerId = GetKillerId(defeated);
            if (string.IsNullOrEmpty(killerId))
            {
                return;
            }

            var killerSide = GetKillerSide(defeated);
            var killer = killerSide.HasValue
                ? FindMinionByInstanceId(context, killerSide.Value, killerId)
                : FindMinionByInstanceId(context, killerId);
            if (!killerSide.HasValue && killer != null)
            {
                killerSide = killer.Owner;
            }

            if (!killerSide.HasValue || killerSide.Value == defeatedOwner.Side)
            {
                return;
            }

            var killerCardId = GetKillerCardId(defeated);
            if (string.IsNullOrEmpty(killerCardId) && killer != null)
            {
                killerCardId = killer.CardId;
            }

            if (string.IsNullOrEmpty(killerCardId))
            {
                return;
            }

            var rewardOwner = context.Get(killerSide.Value);
            rewardOwner.Rewards.Add(new CombatReward
            {
                Type = CombatRewardType.FriendlyMinionKilledEnemy,
                Side = rewardOwner.Side,
                SourceCardId = killerCardId,
                SourceInstanceId = killerId,
                TargetInstanceId = defeated.InstanceId,
                CardId = defeated.CardId,
                Amount = 1,
                Attack = defeated.BaseAttack > 0 ? defeated.BaseAttack : Math.Max(0, defeated.Attack),
                Health = defeated.BaseHealth > 0 ? defeated.BaseHealth : Math.Max(1, defeated.MaxHealth)
            });
            AddLog(context.Log, "CombatRewardQueued", "FriendlyMinionKilledEnemy from " + killerCardId + " killed " + defeated.CardId, killerId, defeated.InstanceId, LogSeverity.Good);
            ResolveMutaliskFriendlyKill(context, rewardOwner, defeated);
            QueueTimewarpedHenchmanReward(context, rewardOwner, killer, defeated);
        }

        private static void ResolveMutaliskFriendlyKill(CombatContext context, CombatSideState rewardOwner, MinionInstance defeated)
        {
            foreach (var mutalisk in rewardOwner.Board.Where(minion => IsAlive(minion) && minion.CardId == MutaliskCardId).ToList())
            {
                var amount = mutalisk.Golden ? 2 : 1;
                BuffMinion(mutalisk, amount, 0, "Mutalisk");
                AddTargetedReward(context.Log, rewardOwner, CombatRewardType.BuffOriginalFriendlyMinion, mutalisk.CardId, mutalisk.InstanceId, 1, amount, 0, defeated?.InstanceId);
                AddLog(context.Log, "CombatRewardQueued", "Mutalisk permanently gained Attack after a friendly kill", mutalisk.InstanceId, defeated?.InstanceId, LogSeverity.Good);
            }
        }

        private static void QueueFriendlyAttackReward(
            CombatContext context,
            CombatSideState attackers,
            MinionInstance attacker,
            MinionInstance defender,
            bool triggeredAttack)
        {
            if (context == null || attackers == null || attacker == null)
            {
                return;
            }

            attackers.Rewards.Add(new CombatReward
            {
                Type = CombatRewardType.FriendlyMinionAttacked,
                Side = attackers.Side,
                SourceCardId = attacker.CardId,
                SourceInstanceId = attacker.InstanceId,
                TargetInstanceId = defender?.InstanceId,
                CardId = defender?.CardId,
                Amount = 1,
                Attack = attacker.Attack,
                Health = attacker.MaxHealth
            });
            AddLog(
                context.Log,
                "CombatRewardQueued",
                "FriendlyMinionAttacked from " + attacker.CardId + (triggeredAttack ? " (triggered)" : string.Empty),
                attacker.InstanceId,
                defender?.InstanceId,
                LogSeverity.Good);
            ResolveVoidRayFriendlyAttack(context, attackers, attacker);
        }

        private static void ResolveVoidRayFriendlyAttack(CombatContext context, CombatSideState owner, MinionInstance attacker)
        {
            var voidRays = owner.Board
                .Where(minion => IsAlive(minion) && minion.CardId == ProtossVoidRayCardId)
                .ToList();
            if (voidRays.Count == 0)
            {
                return;
            }

            foreach (var voidRay in voidRays)
            {
                var amount = voidRay.Golden ? 10 : 5;
                BuffMinion(voidRay, amount, 0, "Void Ray");
                AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, voidRay.CardId, voidRay.InstanceId, 1, amount, 0, attacker.InstanceId);
                if (attacker.InstanceId != voidRay.InstanceId)
                {
                    var liveAttacker = owner.Board.FirstOrDefault(minion => IsAlive(minion) && minion.InstanceId == attacker.InstanceId);
                    if (liveAttacker != null)
                    {
                        BuffMinion(liveAttacker, amount, 0, "Void Ray");
                        AddTargetedReward(context.Log, owner, CombatRewardType.BuffOriginalFriendlyMinion, voidRay.CardId, liveAttacker.InstanceId, 1, amount, 0, attacker.InstanceId);
                    }
                }
            }
        }

        private static void QueueTimewarpedHenchmanReward(CombatContext context, CombatSideState rewardOwner, MinionInstance killer, MinionInstance defeated)
        {
            if (killer == null || defeated == null || killer.CardId != TimewarpedHenchmanCardId)
            {
                return;
            }

            var key = string.IsNullOrEmpty(killer.InstanceId) ? killer.CardId : killer.InstanceId;
            rewardOwner.FriendlyKillCounters.TryGetValue(key, out var kills);
            kills += 1;
            rewardOwner.FriendlyKillCounters[key] = kills;
            if (kills != 2)
            {
                return;
            }

            AddReward(
                context.Log,
                rewardOwner,
                CombatRewardType.AddPlainCopyOfKilledEnemyToHand,
                killer.CardId,
                defeated.CardId,
                killer.Golden ? 2 : 1,
                defeated.BaseAttack > 0 ? defeated.BaseAttack : Math.Max(0, defeated.Attack),
                defeated.BaseHealth > 0 ? defeated.BaseHealth : Math.Max(1, defeated.MaxHealth),
                killer.InstanceId);
            AddLog(context.Log, "CombatRewardQueued", "Timewarped Henchman copied second kill " + defeated.CardId, killer.InstanceId, defeated.InstanceId, LogSeverity.Good);
        }

        private static MinionInstance FindMinionByInstanceId(CombatContext context, string instanceId)
        {
            return string.IsNullOrEmpty(instanceId)
                ? null
                : context.Player.Board.Concat(context.Opponent.Board).FirstOrDefault(minion => minion.InstanceId == instanceId);
        }

        private static MinionInstance FindMinionByInstanceId(CombatContext context, BoardSide side, string instanceId)
        {
            return string.IsNullOrEmpty(instanceId)
                ? null
                : context.Get(side).Board.FirstOrDefault(minion => minion.InstanceId == instanceId);
        }

        private static void MarkKilledBy(MinionInstance target, string killerId, BoardSide killerSide, string killerCardId)
        {
            if (target == null || string.IsNullOrEmpty(killerId))
            {
                return;
            }

            if (target.Tags == null)
            {
                target.Tags = new List<string>();
            }

            target.Tags.RemoveAll(tag => tag.StartsWith("killed_by:", StringComparison.Ordinal));
            target.Tags.RemoveAll(tag => tag.StartsWith("killed_by_side:", StringComparison.Ordinal));
            target.Tags.RemoveAll(tag => tag.StartsWith("killed_by_card:", StringComparison.Ordinal));
            target.Tags.Add("killed_by:" + killerId);
            target.Tags.Add("killed_by_side:" + killerSide);
            if (!string.IsNullOrEmpty(killerCardId))
            {
                target.Tags.Add("killed_by_card:" + killerCardId);
            }
        }

        private static string GetKillerId(MinionInstance target)
        {
            var tag = target?.Tags?.FirstOrDefault(value => value.StartsWith("killed_by:", StringComparison.Ordinal));
            return string.IsNullOrEmpty(tag) ? null : tag.Substring("killed_by:".Length);
        }

        private static BoardSide? GetKillerSide(MinionInstance target)
        {
            var tag = target?.Tags?.FirstOrDefault(value => value.StartsWith("killed_by_side:", StringComparison.Ordinal));
            if (string.IsNullOrEmpty(tag))
            {
                return null;
            }

            return Enum.TryParse(tag.Substring("killed_by_side:".Length), out BoardSide side) ? side : (BoardSide?)null;
        }

        private static string GetKillerCardId(MinionInstance target)
        {
            var tag = target?.Tags?.FirstOrDefault(value => value.StartsWith("killed_by_card:", StringComparison.Ordinal));
            return string.IsNullOrEmpty(tag) ? null : tag.Substring("killed_by_card:".Length);
        }

        private static int AddBloodGemToken(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds = null)
        {
            var token = AddToken(context, owner, source, insertIndex, "quilboar", "Quilboar", 1, 1, Tribe.Quilboar, Keyword.Taunt);
            ApplyBloodGem(token, owner.Tavern);
            if (token != null)
            {
                newEntityIds?.Add(token.InstanceId);
            }

            return token == null ? 0 : 1;
        }

        private static int AddTokenAndTrack(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds, string tokenId, string name, int attack, int health, Tribe tribe, Keyword? keyword = null)
        {
            var token = AddToken(context, owner, source, insertIndex, tokenId, name, attack, health, tribe, keyword);
            if (token == null)
            {
                return 0;
            }

            newEntityIds?.Add(token.InstanceId);
            return 1;
        }

        private static void SummonHighestAttackMurlocFromHand(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                return;
            }

            var candidate = owner.Hand
                .Where(card => card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Murloc))
                .OrderByDescending(card => card.Attack)
                .ThenBy(card => card.InstanceId)
                .FirstOrDefault();
            if (candidate == null)
            {
                return;
            }

            var copy = candidate.Clone();
            copy.InstanceId = "combat-snapjaw-" + source.InstanceId + "-" + copy.InstanceId;
            copy.Owner = owner.Side;
            copy.PoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            ApplySummonAuras(owner, copy);
            owner.Board.Add(copy);
            ResolveFriendlySummonTriggers(context, owner, copy, source);
            AddLog(context.Log, "MinionSummoned", source.InstanceId + " summoned hand Murloc " + copy.InstanceId, source.InstanceId, copy.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                source.InstanceId + " summoned hand Murloc " + copy.InstanceId,
                owner.Side,
                source.InstanceId,
                owner.Side,
                copy.InstanceId,
                new[] { source.InstanceId, copy.InstanceId },
                null,
                null,
                new[] { copy.InstanceId },
                new[] { source.InstanceId });
        }

        private static int SummonHighestHealthMurlocsFromHand(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds, int count)
        {
            var inserted = 0;
            var candidates = owner.Hand
                .Where(card => card.CardKind == CardKind.Minion && card.Tribes.Contains(Tribe.Murloc))
                .OrderByDescending(card => card.MaxHealth)
                .ThenBy(card => card.InstanceId)
                .Take(Math.Max(0, count))
                .ToList();
            foreach (var candidate in candidates)
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    break;
                }

                var copy = candidate.Clone();
                copy.InstanceId = "combat-bassgill-" + source.InstanceId + "-" + candidate.InstanceId;
                copy.Owner = owner.Side;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanAttack = true;
                ApplySummonAuras(owner, copy);
                owner.Board.Insert(Math.Min(insertIndex + inserted, owner.Board.Count), copy);
                newEntityIds.Add(copy.InstanceId);
                ResolveFriendlySummonTriggers(context, owner, copy, source);
                inserted += 1;
                AddLog(context.Log, "MinionSummoned", source.InstanceId + " summoned hand Murloc " + copy.InstanceId, source.InstanceId, copy.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.MinionSummoned,
                    source.InstanceId + " summoned hand Murloc " + copy.InstanceId,
                    owner.Side,
                    source.InstanceId,
                    owner.Side,
                    copy.InstanceId,
                    new[] { source.InstanceId, copy.InstanceId },
                    null,
                    null,
                    new[] { copy.InstanceId },
                    new[] { source.InstanceId });
            }

            return inserted;
        }

        private static int SummonHighestHealthMinionsFromHand(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds, int count, bool divineShield)
        {
            var inserted = 0;
            var candidates = owner.Hand
                .Where(card => card.CardKind == CardKind.Minion)
                .OrderByDescending(card => card.MaxHealth)
                .ThenBy(card => card.InstanceId)
                .Take(Math.Max(0, count))
                .ToList();
            foreach (var candidate in candidates)
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    break;
                }

                var copy = candidate.Clone();
                copy.InstanceId = "combat-timewarped-hand-" + source.InstanceId + "-" + candidate.InstanceId;
                copy.Owner = owner.Side;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanAttack = true;
                if (divineShield)
                {
                    AddKeyword(copy, Keyword.DivineShield);
                }

                ApplySummonAuras(owner, copy);
                owner.Board.Insert(Math.Min(insertIndex + inserted, owner.Board.Count), copy);
                newEntityIds.Add(copy.InstanceId);
                ResolveFriendlySummonTriggers(context, owner, copy, source);
                inserted += 1;
                AddLog(context.Log, "MinionSummoned", source.InstanceId + " summoned hand minion " + copy.InstanceId, source.InstanceId, copy.InstanceId, LogSeverity.Good);
            }

            return inserted;
        }

        private static int ResolveTimewarpedScourfinDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            var candidates = owner.Hand.Where(card => card.CardKind == CardKind.Minion).ToList();
            if (candidates.Count == 0)
            {
                return 0;
            }

            var target = new SeededRng(context.Seed + context.AttackSequence * 719 + candidates.Count).Pick(candidates);
            var multiplier = source.Golden ? 2 : 1;
            var attack = 7 * multiplier;
            var health = 7 * multiplier;
            AddTargetedReward(context.Log, owner, CombatRewardType.BuffTargetHandMinion, source.CardId, target.InstanceId, 1, attack, health, source.InstanceId);
            if (owner.Board.Count >= BoardLimit)
            {
                return 0;
            }

            var copy = target.Clone();
            copy.InstanceId = "combat-scourfin-" + source.InstanceId + "-" + target.InstanceId;
            copy.Owner = owner.Side;
            copy.PoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            BuffMinion(copy, attack, health, "Timewarped Scourfin");
            ApplySummonAuras(owner, copy);
            owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), copy);
            newEntityIds.Add(copy.InstanceId);
            ResolveFriendlySummonTriggers(context, owner, copy, source);
            return 1;
        }

        private static int ResolveTimewarpedCaretakerDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            var skeletons = 5 * (source.Golden ? 2 : 1);
            var inserted = 0;
            var overflowed = 0;
            for (var index = 0; index < skeletons; index += 1)
            {
                if (owner.Board.Count >= BoardLimit)
                {
                    overflowed += 1;
                    RecordSummonOverflow(context, owner, source, "skeleton", "Skeleton");
                    continue;
                }

                inserted += AddTokenAndTrack(context, owner, source, insertIndex + inserted, newEntityIds, "skeleton", "Skeleton", 1, 1, Tribe.Undead);
            }

            if (overflowed > 0)
            {
                AddReward(context.Log, owner, CombatRewardType.ImproveUndeadAttack, source.CardId, null, overflowed);
            }

            return inserted;
        }

        private static int ResolveTimewarpedNestSwarmer(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            var multiplier = source.Golden ? 2 : 1;
            AddReward(context.Log, owner, CombatRewardType.ImproveBeetleStats, source.CardId, null, multiplier, 2, 3, source.InstanceId);
            if (owner.Tavern != null)
            {
                owner.Tavern.BeetleAttackBonus += 2 * multiplier;
                owner.Tavern.BeetleHealthBonus += 3 * multiplier;
            }

            return AddTokenAndTrack(
                context,
                owner,
                source,
                insertIndex,
                newEntityIds,
                "beetle",
                "Beetle",
                2 + (owner.Tavern?.BeetleAttackBonus ?? 0),
                2 + (owner.Tavern?.BeetleHealthBonus ?? 0),
                Tribe.Beast);
        }

        private static int ResolveTimewarpedSummonAndGetTribe(
            CombatContext context,
            CombatSideState owner,
            MinionInstance source,
            int insertIndex,
            List<string> newEntityIds,
            Tribe tribe,
            int count,
            string tokenId,
            string tokenName,
            int attack,
            int health)
        {
            var amount = Math.Max(0, count);
            var inserted = 0;
            for (var index = 0; index < amount; index += 1)
            {
                inserted += AddTokenAndTrack(
                    context,
                    owner,
                    source,
                    insertIndex + inserted,
                    newEntityIds,
                    tokenId,
                    tokenName,
                    attack,
                    health,
                    tribe);
            }

            if (amount > 0)
            {
                AddReward(context.Log, owner, CombatRewardType.AddRandomSameTribeMinionToHand, source.CardId, tribe.ToString(), amount, source.InstanceId);
            }

            return inserted;
        }

        private static void ResolveTimewarpedLeapfroggerDeathrattle(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var candidates = owner.Board.Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Beast)).ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var target = new SeededRng(context.Seed + context.AttackSequence * 811 + candidates.Count).Pick(candidates);
            var multiplier = source.Golden ? 2 : 1;
            BuffMinion(target, multiplier, multiplier, "Timewarped Leapfrogger");
            AddKeyword(target, Keyword.Deathrattle);
            AddKeyword(target, Keyword.Taunt);
            AddKeyword(target, Keyword.Reborn);
        }

        private static void QueueTimewarpedRadioStarReward(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var killerId = GetKillerId(source);
            var killerSide = GetKillerSide(source);
            if (!killerSide.HasValue || string.IsNullOrEmpty(killerId))
            {
                return;
            }

            var killer = FindMinionByInstanceId(context, killerSide.Value, killerId);
            if (killer == null)
            {
                return;
            }

            AddReward(context.Log, owner, CombatRewardType.AddCopyOfKillerToHand, source.CardId, killer.CardId, 1, Math.Max(0, killer.Attack), Math.Max(1, killer.MaxHealth), source.InstanceId);
        }

        private static int SummonLeftmostHandMinionForCombat(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, List<string> newEntityIds)
        {
            if (owner.Board.Count >= BoardLimit)
            {
                return 0;
            }

            var candidate = owner.Hand.FirstOrDefault(card => card.CardKind == CardKind.Minion);
            if (candidate == null)
            {
                return 0;
            }

            var copy = candidate.Clone();
            copy.InstanceId = "combat-hand-" + source.InstanceId + "-" + candidate.InstanceId;
            copy.Owner = owner.Side;
            copy.PoolSource = PoolSource.Summon;
            copy.PoolCopiesHeld = 0;
            copy.CanAttack = true;
            ApplySummonAuras(owner, copy);
            owner.Board.Insert(Math.Min(insertIndex, owner.Board.Count), copy);
            newEntityIds.Add(copy.InstanceId);
            ResolveFriendlySummonTriggers(context, owner, copy, source);
            AddLog(context.Log, "MinionSummoned", source.InstanceId + " summoned hand minion " + copy.InstanceId, source.InstanceId, copy.InstanceId, LogSeverity.Good);
            return 1;
        }

        private static MinionInstance AddToken(CombatContext context, CombatSideState owner, MinionInstance source, int insertIndex, string tokenId, string name, int attack, int health, Tribe tribe, Keyword? keyword = null)
        {
            var sourceInstanceId = source?.InstanceId ?? "quest";
            if (owner.Board.Count >= BoardLimit)
            {
                foreach (var crasher in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == CatacombCrasherCardId))
                {
                    BuffAll(owner.Board, crasher.Golden ? 4 : 2, crasher.Golden ? 4 : 2, "Catacomb Crasher");
                }

                RecordSummonOverflow(context, owner, source, tokenId, name);
                return null;
            }

            var keywords = new List<Keyword>();
            if (keyword.HasValue)
            {
                keywords.Add(keyword.Value);
            }

            var token = new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = "token-" + sourceInstanceId + "-" + tokenId + "-" + owner.Board.Count,
                DefinitionId = tokenId,
                CardId = tokenId.ToUpperInvariant(),
                Name = name,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords,
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                Owner = owner.Side,
                CanAttack = true,
                PoolSource = PoolSource.Summon,
                PoolCopiesHeld = 0
            };
            ApplySummonAuras(owner, token);
            owner.Board.Insert(Math.Min(Math.Max(0, insertIndex), owner.Board.Count), token);
            ResolveFriendlySummonTriggers(context, owner, token, source);
            AddLog(context.Log, "MinionSummoned", sourceInstanceId + " summoned " + name, sourceInstanceId, token.InstanceId, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.MinionSummoned,
                sourceInstanceId + " summoned " + name,
                owner.Side,
                sourceInstanceId,
                owner.Side,
                token.InstanceId,
                new[] { sourceInstanceId, token.InstanceId },
                null,
                null,
                new[] { token.InstanceId },
                new[] { sourceInstanceId });
            return token;
        }

        private static void RecordSummonOverflow(CombatContext context, CombatSideState owner, MinionInstance source, string tokenId, string name)
        {
            var sourceInstanceId = source?.InstanceId ?? "quest";
            var overflowId = "overflow-" + sourceInstanceId + "-" + tokenId + "-" + context.Replay.Frames.Count;
            if (owner?.Tavern != null && owner.Tavern.TrinketMugOfTheSireActive)
            {
                var targets = owner.Board.Where(IsAlive).ToList();
                foreach (var target in targets)
                {
                    BuffMinion(target, 5, 0, "Mug of the Sire");
                }

                AddLog(context.Log, "TrinketSummonOverflowed", "Mug of the Sire gave +5 Attack to " + targets.Count + " minion(s)", MugOfTheSireCardId, sourceInstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.TrinketTriggered,
                    "Mug of the Sire gave +5 Attack to " + targets.Count + " minion(s)",
                    owner.Side,
                    MugOfTheSireCardId,
                    owner.Side,
                    targets.FirstOrDefault()?.InstanceId,
                    targets.Select(target => target.InstanceId).Concat(new[] { sourceInstanceId, MugOfTheSireCardId }),
                    null,
                    null,
                    null,
                    targets.Select(target => target.InstanceId),
                    new[] { overflowId },
                    owner.Side,
                    -1,
                    1,
                    0);
            }

            AddLog(context.Log, "SummonOverflowed", sourceInstanceId + " overflowed " + name, sourceInstanceId, tokenId, LogSeverity.Warning);
            RecordFrame(
                context,
                CombatEventType.SummonOverflowed,
                sourceInstanceId + " overflowed " + name,
                owner.Side,
                sourceInstanceId,
                owner.Side,
                tokenId,
                new[] { sourceInstanceId },
                null,
                null,
                null,
                new[] { sourceInstanceId },
                new[] { overflowId },
                owner.Side,
                -1,
                1,
                0);
        }

        private static void ResolveFriendlySummonTriggers(CombatContext context, CombatSideState owner, MinionInstance summoned, MinionInstance source)
        {
            if (summoned == null)
            {
                return;
            }

            ApplyHeroCombatSummonModifiers(owner, summoned);
            ApplyTrinketCombatSummonModifiers(context, owner, summoned, source);

            if (HasCountedTribe(summoned, Tribe.Beast))
            {
                foreach (var slamma in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == BananaSlammaCardId))
                {
                    BuffMinion(summoned, StatMath.SaturatingMultiply(summoned.Attack, slamma.Golden ? 2 : 1, 0, StatMath.MaxStat), 0, "Banana Slamma");
                }

                foreach (var rider in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == MoonRiderCardId))
                {
                    rider.Counters.TryGetValue("beast_summon_attack", out var bonus);
                    bonus += rider.Golden ? 4 : 2;
                    rider.Counters["beast_summon_attack"] = bonus;
                    BuffMinion(summoned, bonus, 0, "Moon-Rider");
                }
            }

            if (HasCountedTribe(summoned, Tribe.Mech))
            {
                foreach (var deflecto in owner.Board.Where(minion => IsAlive(minion) && minion.CardId == DeflectOBotCardId))
                {
                    BuffMinion(deflecto, deflecto.Golden ? 4 : 2, 0, "Deflect-o-Bot");
                    if (!deflecto.Keywords.Contains(Keyword.DivineShield))
                    {
                        deflecto.Keywords.Add(Keyword.DivineShield);
                    }

                    RecordFrame(
                        context,
                        CombatEventType.AttackTriggered,
                        deflecto.InstanceId + " reacted to Mech summon",
                        owner.Side,
                        deflecto.InstanceId,
                        owner.Side,
                        summoned.InstanceId,
                        new[] { deflecto.InstanceId, summoned.InstanceId, source?.InstanceId },
                        null,
                        null,
                        null,
                        new[] { deflecto.InstanceId });
                }
            }

            QueueFriendlySummonReward(context, owner, source, summoned);
        }

        private static void ApplyTrinketCombatSummonModifiers(CombatContext context, CombatSideState owner, MinionInstance summoned, MinionInstance source)
        {
            var tavern = owner.Tavern;
            if (tavern == null || summoned == null)
            {
                return;
            }

            if (HasCountedTribe(summoned, Tribe.Beast) &&
                (tavern.TrinketCombatBeastSummonBonusAttack != 0 || tavern.TrinketCombatBeastSummonBonusHealth != 0))
            {
                BuffMinion(summoned, tavern.TrinketCombatBeastSummonBonusAttack, tavern.TrinketCombatBeastSummonBonusHealth, "Mama Bear Sticker");
            }

            if (HasCountedTribe(summoned, Tribe.Beast) && tavern.TrinketSlammaStickerActive)
            {
                BuffMinion(summoned, summoned.Attack, 0, "Slamma Sticker");
            }

            if (HasCountedTribe(summoned, Tribe.Murloc) && tavern.TrinketBassgillPortraitActive)
            {
                AddKeyword(summoned, Keyword.DivineShield);
            }

            if (tavern.TrinketReinforcedShieldUses > 0)
            {
                AddKeyword(summoned, Keyword.DivineShield);
                tavern.TrinketReinforcedShieldUses -= 1;
            }

            if (tavern.TrinketBlingtronSunglassesActive && HasCountedTribe(summoned, Tribe.Mech))
            {
                var candidates = owner.Board
                    .Where(minion => IsAlive(minion) && HasCountedTribe(minion, Tribe.Mech) && !minion.Keywords.Contains(Keyword.DivineShield))
                    .ToList();
                if (candidates.Count > 0)
                {
                    var target = new SeededRng(context.Seed + context.AttackSequence * 811 + candidates.Count).Pick(candidates);
                    AddKeyword(target, Keyword.DivineShield);
                    AddLog(context.Log, "TrinketSummonTriggered", "Blingtron's Sunglasses gave Divine Shield to " + target.InstanceId, BlingtronsSunglassesCardId, target.InstanceId, LogSeverity.Good);
                    RecordFrame(
                        context,
                        CombatEventType.TrinketTriggered,
                        "Blingtron's Sunglasses gave Divine Shield to " + target.InstanceId,
                        owner.Side,
                        BlingtronsSunglassesCardId,
                        owner.Side,
                        target.InstanceId,
                        new[] { summoned.InstanceId, target.InstanceId, BlingtronsSunglassesCardId },
                        null,
                        null,
                        null,
                        new[] { target.InstanceId });
                }
            }

            ResolveTwinSkyLanterns(context, owner, summoned, source);
        }

        private static void ResolveTwinSkyLanterns(CombatContext context, CombatSideState owner, MinionInstance summoned, MinionInstance source)
        {
            var tavern = owner.Tavern;
            if (tavern == null ||
                tavern.TrinketTwinSkyLanternCopies <= 0 ||
                owner.TwinSkyLanternTriggered ||
                (summoned.Tags != null && summoned.Tags.Contains(TwinSkyLanternCopyTag)) ||
                owner.Board.Count >= BoardLimit)
            {
                return;
            }

            owner.TwinSkyLanternTriggered = true;
            var sourceIndex = owner.Board.FindIndex(minion => minion.InstanceId == summoned.InstanceId);
            var insertIndex = sourceIndex < 0 ? owner.Board.Count : sourceIndex + 1;
            var copyCount = Math.Min(tavern.TrinketTwinSkyLanternCopies, BoardLimit - owner.Board.Count);
            for (var index = 0; index < copyCount; index += 1)
            {
                var copy = summoned.Clone();
                copy.InstanceId = "twin-sky-lantern-" + summoned.InstanceId + "-" + index;
                copy.Owner = owner.Side;
                copy.PoolSource = PoolSource.Summon;
                copy.PoolCopiesHeld = 0;
                copy.CanAttack = true;
                if (copy.Tags == null)
                {
                    copy.Tags = new List<string>();
                }

                if (!copy.Tags.Contains(TwinSkyLanternCopyTag))
                {
                    copy.Tags.Add(TwinSkyLanternCopyTag);
                }

                owner.Board.Insert(Math.Min(insertIndex + index, owner.Board.Count), copy);
                ResolveFriendlySummonTriggers(context, owner, copy, summoned);
                AddLog(context.Log, "MinionSummoned", "Twin Sky Lanterns copied " + summoned.InstanceId + " as " + copy.InstanceId, summoned.InstanceId, copy.InstanceId, LogSeverity.Good);
                RecordFrame(
                    context,
                    CombatEventType.MinionSummoned,
                    "Twin Sky Lanterns copied " + summoned.InstanceId + " as " + copy.InstanceId,
                    owner.Side,
                    summoned.InstanceId,
                    owner.Side,
                    copy.InstanceId,
                    new[] { summoned.InstanceId, copy.InstanceId, source?.InstanceId },
                    null,
                    null,
                    new[] { copy.InstanceId },
                    new[] { summoned.InstanceId });
            }
        }

        private static void ApplyHeroCombatSummonModifiers(CombatSideState owner, MinionInstance summoned)
        {
            var tavern = owner.Tavern;
            if (tavern == null || summoned == null)
            {
                return;
            }

            if (tavern.CombatSummonBonusAttack != 0 || tavern.CombatSummonBonusHealth != 0 || tavern.CombatSummonTaunt)
            {
                BuffMinion(summoned, tavern.CombatSummonBonusAttack, tavern.CombatSummonBonusHealth, "Sprout It Out!");
                if (tavern.CombatSummonTaunt && !summoned.Keywords.Contains(Keyword.Taunt))
                {
                    summoned.Keywords.Add(Keyword.Taunt);
                }
            }

            if (tavern.QuestFriendlyAttackAura != 0 ||
                tavern.QuestVolatileVenomActive ||
                tavern.QuestTumblingAttack != 0 ||
                tavern.QuestTumblingHealth != 0)
            {
                var attack = tavern.QuestFriendlyAttackAura + (tavern.QuestVolatileVenomActive ? 7 : 0) + tavern.QuestTumblingAttack;
                var health = (tavern.QuestVolatileVenomActive ? 7 : 0) + tavern.QuestTumblingHealth;
                BuffMinion(summoned, attack, health, "Quest combat summon");
            }

            if (tavern.CombatSummonDoubleStats)
            {
                BuffMinion(summoned, summoned.Attack, summoned.MaxHealth, "Tamuzo");
            }

            if (tavern.CombatSameTierSummonBuffTier > 0 && summoned.TavernTier == tavern.CombatSameTierSummonBuffTier)
            {
                foreach (var minion in owner.Board.Where(IsAlive))
                {
                    BuffMinion(minion, tavern.CombatSameTierSummonBuffAttack, tavern.CombatSameTierSummonBuffHealth, "Baby Y'Shaarj");
                }
            }
        }

        private static void QueueFriendlySummonReward(CombatContext context, CombatSideState owner, MinionInstance source, MinionInstance summoned)
        {
            if (owner == null || summoned == null)
            {
                return;
            }

            owner.Rewards.Add(new CombatReward
            {
                Type = CombatRewardType.FriendlyMinionSummoned,
                Side = owner.Side,
                SourceCardId = source?.CardId,
                SourceInstanceId = source?.InstanceId,
                TargetInstanceId = summoned.InstanceId,
                CardId = summoned.CardId,
                Amount = 1,
                Attack = summoned.Attack,
                Health = summoned.MaxHealth,
                TavernTier = summoned.TavernTier,
                Tribes = summoned.Tribes == null ? new List<Tribe>() : new List<Tribe>(summoned.Tribes)
            });
            AddLog(context.Log, "CombatRewardQueued", "FriendlyMinionSummoned from " + (source?.CardId ?? "combat"), source?.InstanceId, summoned.InstanceId, LogSeverity.Good);
        }

        private static void RecordRebornOverflow(CombatContext context, CombatSideState owner, MinionInstance source)
        {
            var overflowId = "reborn-overflow-" + source.InstanceId + "-" + context.Replay.Frames.Count;
            AddLog(context.Log, "RebornOverflowed", source.InstanceId + " reborn overflowed", source.InstanceId, null, LogSeverity.Warning);
            RecordFrame(
                context,
                CombatEventType.RebornOverflowed,
                source.InstanceId + " reborn overflowed",
                owner.Side,
                source.InstanceId,
                owner.Side,
                source.InstanceId,
                new[] { source.InstanceId },
                null,
                null,
                null,
                new[] { source.InstanceId },
                new[] { overflowId },
                owner.Side,
                -1,
                0,
                1);
        }

        private static void RetargetAttackPointerToNewUnits(CombatContext context, CombatSideState owner, List<string> newEntityIds, List<string> sourceIds)
        {
            if (newEntityIds == null || newEntityIds.Count == 0)
            {
                return;
            }

            var candidates = newEntityIds
                .Select(id => new { Id = id, Index = owner.Board.FindIndex(minion => minion.InstanceId == id && IsAlive(minion) && minion.CanAttack) })
                .Where(candidate => candidate.Index >= 0)
                .OrderBy(candidate => candidate.Index)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var target = candidates[0];
            owner.PendingAttackIndexOverride = target.Index;
            AddLog(context.Log, "AttackPointerRetargeted", owner.Side + " attack pointer -> " + target.Id + " @" + target.Index, target.Id, null, LogSeverity.Good);
            RecordFrame(
                context,
                CombatEventType.AttackPointerRetargeted,
                owner.Side + " attack pointer -> " + target.Id + " @" + target.Index,
                owner.Side,
                target.Id,
                owner.Side,
                target.Id,
                candidates.Select(candidate => candidate.Id).Concat(sourceIds ?? Enumerable.Empty<string>()),
                null,
                null,
                null,
                candidates.Select(candidate => candidate.Id),
                null,
                owner.Side,
                target.Index,
                0,
                0);
        }

        private static void ApplyBloodGem(MinionInstance target, TavernState tavern = null)
        {
            if (target == null)
            {
                return;
            }

            BuffMinion(target, 1 + (tavern?.BloodGemBonusAttack ?? 0), 1 + (tavern?.BloodGemBonusHealth ?? 0), "Blood Gem");
        }

        private static void AddKeyword(MinionInstance target, Keyword keyword)
        {
            if (target == null)
            {
                return;
            }

            if (target.Keywords == null)
            {
                target.Keywords = new List<Keyword>();
            }

            if (!target.Keywords.Contains(keyword))
            {
                target.Keywords.Add(keyword);
            }
        }

        private static void AddTag(MinionInstance target, string tag)
        {
            if (target == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            if (target.Tags == null)
            {
                target.Tags = new List<string>();
            }

            if (!target.Tags.Contains(tag))
            {
                target.Tags.Add(tag);
            }
        }

        private static void BuffMinion(MinionInstance target, int attack, int health, string sourceId)
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
        }

        private static void AddLog(List<CombatLogEntry> log, string title, string detail, string actorId, string targetId, LogSeverity severity)
        {
            log.Add(new CombatLogEntry
            {
                Seq = log.Count + 1,
                Title = title,
                Detail = detail,
                ActorId = actorId,
                TargetId = targetId,
                Severity = severity
            });
        }

        private static void RecordFrame(
            CombatContext context,
            CombatEventType eventType,
            string logText,
            BoardSide actorSide = BoardSide.Player,
            string actorId = null,
            BoardSide targetSide = BoardSide.Opponent,
            string targetId = null,
            IEnumerable<string> relatedEntityIds = null,
            IEnumerable<string> damagedEntityIds = null,
            IEnumerable<string> deadEntityIds = null,
            IEnumerable<string> summonedEntityIds = null,
            IEnumerable<string> triggerSourceIds = null,
            IEnumerable<string> overflowedEntityIds = null,
            BoardSide attackPointerSide = BoardSide.Player,
            int attackPointerIndex = -1,
            int summonOverflowCount = 0,
            int rebornOverflowCount = 0,
            int actualDamageCount = 0,
            int divineShieldBreakCount = 0,
            bool triggeredAttack = false,
            int mechanicCounter = 0,
            int mechanicThreshold = 0)
        {
            context.Replay.Frames.Add(new CombatFrame
            {
                Index = context.Replay.Frames.Count,
                EventType = eventType,
                ActorSide = actorSide,
                ActorId = actorId,
                TargetSide = targetSide,
                TargetId = targetId,
                PlayerBoardSnapshot = CreateBoardSnapshot(BoardSide.Player, context.Player.Board),
                OpponentBoardSnapshot = CreateBoardSnapshot(BoardSide.Opponent, context.Opponent.Board),
                LogText = logText,
                RelatedEntityIds = DistinctIds(relatedEntityIds),
                DamagedEntityIds = DistinctIds(damagedEntityIds),
                DeadEntityIds = DistinctIds(deadEntityIds),
                SummonedEntityIds = DistinctIds(summonedEntityIds),
                TriggerSourceIds = DistinctIds(triggerSourceIds),
                OverflowedEntityIds = DistinctIds(overflowedEntityIds),
                AttackPointerSide = attackPointerSide,
                AttackPointerIndex = attackPointerIndex,
                SummonOverflowCount = summonOverflowCount,
                RebornOverflowCount = rebornOverflowCount,
                ActualDamageCount = actualDamageCount,
                DivineShieldBreakCount = divineShieldBreakCount,
                TriggeredAttack = triggeredAttack,
                MechanicCounter = mechanicCounter,
                MechanicThreshold = mechanicThreshold
            });
        }

        private static CombatBoardPairSnapshot CreateBoardPairSnapshot(CombatContext context)
        {
            return new CombatBoardPairSnapshot
            {
                Player = CreateBoardSnapshot(BoardSide.Player, context.Player.Board),
                Opponent = CreateBoardSnapshot(BoardSide.Opponent, context.Opponent.Board)
            };
        }

        private static CombatBoardSnapshot CreateBoardSnapshot(BoardSide side, IList<MinionInstance> board)
        {
            var snapshot = new CombatBoardSnapshot { Side = side };
            for (var index = 0; index < board.Count; index += 1)
            {
                var minion = board[index];
                if (minion == null)
                {
                    continue;
                }

                snapshot.Minions.Add(new CombatMinionSnapshot
                {
                    Position = index,
                    InstanceId = minion.InstanceId,
                    CardId = minion.CardId,
                    Name = minion.Name,
                    Attack = minion.Attack,
                    Health = minion.Health,
                    MaxHealth = minion.MaxHealth,
                    BaseAttack = minion.BaseAttack,
                    BaseHealth = minion.BaseHealth,
                    TavernTier = minion.TavernTier,
                    Golden = minion.Golden,
                    CanAttack = minion.CanAttack,
                    AttacksThisCombat = minion.AttacksThisCombat,
                    Keywords = minion.Keywords == null ? new List<Keyword>() : new List<Keyword>(minion.Keywords),
                    Tribes = minion.Tribes == null ? new List<Tribe>() : new List<Tribe>(minion.Tribes),
                    EnchantmentSourceIds = minion.Enchantments == null
                        ? new List<string>()
                        : minion.Enchantments.Where(enchantment => enchantment != null && !string.IsNullOrEmpty(enchantment.SourceId)).Select(enchantment => enchantment.SourceId).Distinct().ToList(),
                    Tags = minion.Tags == null ? new List<string>() : new List<string>(minion.Tags)
                });
            }

            return snapshot;
        }

        private static List<string> DistinctIds(IEnumerable<string> ids)
        {
            return ids == null
                ? new List<string>()
                : ids.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        }

        private static List<CombatReward> CloneRewards(IEnumerable<CombatReward> rewards)
        {
            return rewards == null
                ? new List<CombatReward>()
                : rewards.Select(reward => new CombatReward
                {
                    Type = reward.Type,
                    Side = reward.Side,
                    SourceCardId = reward.SourceCardId,
                    SourceInstanceId = reward.SourceInstanceId,
                    TargetInstanceId = reward.TargetInstanceId,
                    CardId = reward.CardId,
                    Amount = reward.Amount,
                    Attack = reward.Attack,
                    Health = reward.Health,
                    TavernTier = reward.TavernTier,
                    Tribes = reward.Tribes == null ? new List<Tribe>() : new List<Tribe>(reward.Tribes)
                }).ToList();
        }

        private static void AdvanceNaturalAttackPointers(CombatContext context, BoardSide attackerSide, int attackerIndex)
        {
            var attackers = context.Get(attackerSide);
            var defenders = context.Get(attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player);

            if (!ApplyPendingAttackPointer(attackers))
            {
                attackers.AttackIndex = NormalizeAttackIndex(attackers.Board, attackerIndex + 1);
            }

            if (!ApplyPendingAttackPointer(defenders))
            {
                defenders.AttackIndex = NormalizeAttackIndex(defenders.Board, defenders.AttackIndex);
            }
        }

        private static bool ApplyPendingAttackPointer(CombatSideState side)
        {
            if (!side.PendingAttackIndexOverride.HasValue)
            {
                return false;
            }

            side.AttackIndex = NormalizeAttackIndex(side.Board, side.PendingAttackIndexOverride.Value);
            side.PendingAttackIndexOverride = null;
            return true;
        }

        private static int FindNextAttackerIndex(IList<MinionInstance> board, int startIndex)
        {
            if (board.Count == 0)
            {
                return -1;
            }

            var normalized = NormalizeAttackIndex(board, startIndex);
            for (var offset = 0; offset < board.Count; offset += 1)
            {
                var index = (normalized + offset) % board.Count;
                var candidate = board[index];
                if (candidate != null && IsAlive(candidate) && candidate.CanAttack)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int NormalizeAttackIndex(IList<MinionInstance> board, int index)
        {
            if (board.Count == 0)
            {
                return 0;
            }

            var normalized = index % board.Count;
            return normalized < 0 ? normalized + board.Count : normalized;
        }

        private static MinionInstance ChooseDefender(IList<MinionInstance> defenders, int seed)
        {
            var visible = defenders.Where(minion => !minion.Keywords.Contains(Keyword.Stealth)).ToList();
            var targetPool = visible.Count > 0 ? visible : defenders;
            var taunts = targetPool.Where(minion => minion.Keywords.Contains(Keyword.Taunt)).ToList();
            var candidates = taunts.Count > 0 ? taunts : targetPool;
            return new SeededRng(seed).Pick(candidates);
        }

        private static int GetCombatSpellDamage(CombatSideState owner, int baseAmount)
        {
            return StatMath.SaturatingAdd(Math.Max(0, baseAmount), Math.Max(0, owner?.Tavern?.SpellPower ?? 0), 0, StatMath.MaxStat);
        }

        private static DamageResult DealDamage(MinionInstance target, int amount, bool poison)
        {
            var next = target.Clone();
            if (amount <= 0)
            {
                return new DamageResult(next, false);
            }

            if (next.Keywords.Contains(Keyword.DivineShield))
            {
                next.Keywords.Remove(Keyword.DivineShield);
                return new DamageResult(next, false, true);
            }

            next.Health = poison ? 0 : StatMath.DamageHealth(next.Health, amount);
            return new DamageResult(next, true, false);
        }

        private static bool SideHasTag(CombatSideState side, string tag)
        {
            return side.Board.Any(minion => minion != null && minion.Tags.Contains(tag));
        }

        private static void RemoveTagFromSide(CombatSideState side, string tag)
        {
            foreach (var minion in side.Board.Where(minion => minion != null))
            {
                minion.Tags.Remove(tag);
            }
        }

        private static void ReplaceByInstanceId(IList<MinionInstance> items, MinionInstance next)
        {
            for (var index = 0; index < items.Count; index += 1)
            {
                if (items[index].InstanceId == next.InstanceId)
                {
                    items[index] = next;
                    return;
                }
            }
        }

        private sealed class CombatContext
        {
            public CombatContext(List<MinionInstance> player, List<MinionInstance> opponent, TavernState playerTavern, TavernState opponentTavern, List<MinionInstance> playerHand, List<MinionInstance> opponentHand, List<MinionInstance> playerCombatSummonPool, List<MinionInstance> opponentCombatSummonPool, int seed)
            {
                Player = new CombatSideState(BoardSide.Player, player, playerTavern, playerHand, playerCombatSummonPool);
                Opponent = new CombatSideState(BoardSide.Opponent, opponent, opponentTavern, opponentHand, opponentCombatSummonPool);
                Seed = seed;
                Replay = new CombatReplay { Seed = seed };
            }

            public CombatSideState Player { get; }
            public CombatSideState Opponent { get; }
            public int Seed { get; }
            public int AttackSequence { get; set; }
            public List<CombatLogEntry> Log { get; } = new List<CombatLogEntry>();
            public CombatReplay Replay { get; }
            public Queue<ImmediateAttackRequest> ImmediateAttacks { get; } = new Queue<ImmediateAttackRequest>();

            public CombatSideState Get(BoardSide side)
            {
                return side == BoardSide.Player ? Player : Opponent;
            }
        }

        private sealed class CombatSideState
        {
            public CombatSideState(BoardSide side, List<MinionInstance> board, TavernState tavern, List<MinionInstance> hand, List<MinionInstance> combatSummonPool)
            {
                Side = side;
                Board = board;
                Tavern = tavern;
                Hand = hand;
                CombatSummonPool = combatSummonPool ?? new List<MinionInstance>();
            }

            public BoardSide Side { get; }
            public List<MinionInstance> Board { get; }
            public List<MinionInstance> Hand { get; }
            public List<MinionInstance> CombatSummonPool { get; }
            public TavernState Tavern { get; }
            public List<CombatReward> Rewards { get; } = new List<CombatReward>();
            public int AttackIndex { get; set; }
            public int? PendingAttackIndexOverride { get; set; }
            public int BeastAttackAura { get; set; }
            public int TemporaryAvengeBeastRewards { get; set; }
            public bool TwinSkyLanternTriggered { get; set; }
            public Dictionary<string, int> AvengeCounters { get; } = new Dictionary<string, int>();
            public Dictionary<string, int> FriendlyKillCounters { get; } = new Dictionary<string, int>();
            public List<MinionInstance> DeadMechPlainCopies { get; } = new List<MinionInstance>();
            public Dictionary<string, MinionInstance> StitchedCopies { get; } = new Dictionary<string, MinionInstance>();
            public Dictionary<string, List<MinionInstance>> CopiedDeathrattlesBySource { get; } = new Dictionary<string, List<MinionInstance>>();
            public Dictionary<string, int> SummonAuraUses { get; } = new Dictionary<string, int>();
            public List<MinionInstance> FishyStickerCopiedDeathrattles { get; } = new List<MinionInstance>();
            public List<MinionInstance> SoulFermenterStoredMinions { get; } = new List<MinionInstance>();
            public bool SoulFermenterTriggered { get; set; }
            public bool BoomControllerTriggered { get; set; }
            public MinionInstance STharaStoredDemon { get; set; }
            public bool STharaTriggered { get; set; }
        }

        private readonly struct ImmediateAttackRequest
        {
            public ImmediateAttackRequest(BoardSide side, string instanceId)
            {
                Side = side;
                InstanceId = instanceId;
            }

            public BoardSide Side { get; }
            public string InstanceId { get; }
        }

        private readonly struct AttackResult
        {
            public AttackResult(string attackerId, BoardSide attackerSide, BoardSide defenderSide, bool attackerSurvived, bool attackerHadWindfury)
            {
                AttackerId = attackerId;
                AttackerSide = attackerSide;
                DefenderSide = defenderSide;
                AttackerSurvived = attackerSurvived;
                AttackerHadWindfury = attackerHadWindfury;
            }

            public string AttackerId { get; }
            public BoardSide AttackerSide { get; }
            public BoardSide DefenderSide { get; }
            public bool AttackerSurvived { get; }
            public bool AttackerHadWindfury { get; }

            public static AttackResult Empty(BoardSide attackerSide)
            {
                var defenderSide = attackerSide == BoardSide.Player ? BoardSide.Opponent : BoardSide.Player;
                return new AttackResult(null, attackerSide, defenderSide, false, false);
            }
        }

        private readonly struct DamageResult
        {
            public DamageResult(MinionInstance minion, bool combatDamageDealt, bool divineShieldBroken = false)
            {
                Minion = minion;
                CombatDamageDealt = combatDamageDealt;
                DivineShieldBroken = divineShieldBroken;
            }

            public MinionInstance Minion { get; }
            public bool CombatDamageDealt { get; }
            public bool DivineShieldBroken { get; }
        }
    }
}
