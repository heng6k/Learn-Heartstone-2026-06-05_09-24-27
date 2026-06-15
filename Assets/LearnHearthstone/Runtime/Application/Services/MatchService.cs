using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string BloodGemCardId = "BLOOD_GEM";
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
        private const string SlimyShieldCardId = "SLIMY_SHIELD";
        private const string BristlebackBloodGemCardId = "BRISTLEBACK_BLOOD_GEM";
        private const string RebornBloodGemCardId = "REBORN_BLOOD_GEM";
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
        private const string TemporarySpellcraftSourceId = "Temporary Spellcraft";
        private const string TemporaryVenomousSourceId = "Temporary Venomous";
        private const string PermanentSpellcraftCounter = "permanent_spellcraft_left";
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
        private const string WoodlandWardenCardId = "BG35_151";
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
        private const string FarmhandWhirlOMatronCardId = "BG26_162";
        private const string FirelandsFlameCardId = "BG35_882";
        private const string NightmareParlorGuestCardId = "BG32_111";
        private const string VoidpupTrainerCardId = "BG35_152";
        private const string FamishedFelbatCardId = "BG21_005";
        private const string FelboarCardId = "BG28_633";
        private const string FelFlameDrakeCardId = "BG32_821";
        private const string AshenCorruptorCardId = "BG32_873";
        private const string ChargingCzarinaCardId = "BG28_741";
        private const string BrashPirateCardId = "BG35_701";
        private const string ShipwreckedCaptainCardId = "BG33_821";
        private const string ObsidianRavagerCardId = "BG33_825";
        private const string MaelstromNagaCardId = "BG34_922";
        private const string SereneMeditatorCardId = "BG32_835";
        private const string DarkcrestStrategistCardId = "BG31_920";
        private const string GlowscaleCardId = "BG23_008";
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
        private const string BorrowingEastWindCardNumber = "126909";

        private readonly MinionCatalog catalog;
        private readonly SpellCatalog spellCatalog;
        private readonly MinionEffectCatalog effectCatalog;
        private readonly ITestScenarioRepository scenarioRepository;
        private readonly List<Tribe> activeTribes;
        private CombatTestSnapshot combatTestSnapshot;

        private MatchService(MinionCatalog catalog, SpellCatalog spellCatalog, int seed, ITestScenarioRepository scenarioRepository, MatchSetupOptions setup)
        {
            this.catalog = catalog;
            this.spellCatalog = spellCatalog;
            this.scenarioRepository = scenarioRepository ?? new FileTestScenarioRepository();
            activeTribes = TribeAvailabilityRules.Normalize(setup?.ActiveTribes);
            effectCatalog = MinionEffectCatalog.CreateDefault();
            State = CreateMatch(seed);
        }

        public MatchState State { get; private set; }

        public CombatTestSnapshot LastCombatTestSnapshot => combatTestSnapshot;

        public bool HasCombatTestSnapshot => combatTestSnapshot?.BeforeCombat != null;

        public IReadOnlyList<string> TestScenarioNames => scenarioRepository.ListScenarioNames();

        public static MatchService CreateWithDefaultCatalog(int seed = 12345, ITestScenarioRepository scenarios = null, MatchSetupOptions setup = null)
        {
            return new MatchService(MinionCatalogLoader.LoadFromResources(), SpellCatalogLoader.LoadFromResources(), seed, scenarios, setup);
        }

        public MatchState Apply(GameCommand command)
        {
            switch (command.Type)
            {
                case GameCommandType.BuyMinion:
                    BuyMinion(command.Index);
                    break;
                case GameCommandType.PlayMinion:
                    PlayMinion(command.Index, command.TargetIndex);
                    break;
                case GameCommandType.SellMinion:
                    SellMinion(command.InstanceId);
                    break;
                case GameCommandType.RerollShop:
                    RerollShop();
                    break;
                case GameCommandType.FreezeShop:
                    State.Player.Tavern.Frozen = command.Flag;
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
            var initial = CreateShopFromPool(null, 1, TavernRules.GetShopSize(1), seed, "shop-1");
            var state = new MatchState
            {
                Mode = MatchMode.TavernPractice,
                Phase = MatchPhase.Tavern,
                Round = 1,
                Seed = seed,
                ActiveTribes = new List<Tribe>(activeTribes),
                Player = new LocalPlayerState
                {
                    Health = 30,
                    Armor = 0,
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
            return state;
        }

        private void RefreshPlayerBoardTribeDistribution()
        {
            BoardTribeAnalyzer.Refresh(State.Player);
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
            return catalog.All.Where(minion => TribeAvailabilityRules.IsMinionAvailable(minion, active));
        }

        private IEnumerable<TavernSpellDefinition> AvailableTavernSpells()
        {
            var active = CurrentActiveTribes();
            return spellCatalog.All.Where(spell =>
                spell.InPool &&
                spell.Category == "TavernSpell" &&
                TribeAvailabilityRules.IsTavernSpellAvailable(spell, active));
        }

        private void BuyMinion(int shopIndex)
        {
            var tavern = State.Player.Tavern;
            if (shopIndex < 0 || shopIndex >= tavern.Shop.Count || tavern.Shop[shopIndex] == null)
            {
                throw new InvalidOperationException("目标商店槽位不存在。");
            }

            if (tavern.Hand.Count >= HandLimit)
            {
                throw new InvalidOperationException("手牌已满。");
            }

            var target = tavern.Shop[shopIndex];
            var cost = target.Cost > 0 ? target.Cost : BuyCost;
            if (target.CardKind == CardKind.TavernSpell && tavern.NextTavernSpellCostReduction > 0)
            {
                cost = Math.Max(0, cost - tavern.NextTavernSpellCostReduction);
            }

            var costsHealth = target.CardKind == CardKind.TavernSpell && target.CardId == HastyExcavationCardId;
            if (costsHealth)
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
            if (costsHealth)
            {
                DamagePlayerHero(cost);
            }
            else
            {
                tavern.Gold -= cost;
            }

            tavern.Hand.Add(target);
            tavern.Shop[shopIndex] = null;
            HandleCardsAddedToHand(1, "buy");
            if (target.CardKind == CardKind.TavernSpell)
            {
                tavern.NextTavernSpellCostReduction = 0;
            }

            AddRecruitLog(RecruitLogType.Buy, "购买 " + target.Name, before, tavern.Gold);
            HandleGoldSpent(costsHealth ? 0 : cost);
            DispatchBoardEvent(MechanicEventType.CardBought);
            HandleCardBoughtForTierOneMinions();
            HandleCardBoughtForTierSixSevenMinions(target);
            ResolvePlayerTriples();
        }

        private void PlayMinion(int handIndex, int targetIndex)
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
                var dynamicBonus = GetBoardTavernSpellBonus();
                tavern.TavernSpellBonusAttack += dynamicBonus.Attack;
                tavern.TavernSpellBonusHealth += dynamicBonus.Health;
                string spellResult;
                try
                {
                    spellResult = TavernSpellEngine.Cast(target, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count), targetIndex);
                    for (var extraCast = 0; extraCast < GetTavernSpellExtraCasts(target); extraCast += 1)
                    {
                        spellResult += " + " + TavernSpellEngine.Cast(target, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count + extraCast + 1), targetIndex);
                    }
                }
                finally
                {
                    tavern.TavernSpellBonusAttack -= dynamicBonus.Attack;
                    tavern.TavernSpellBonusHealth -= dynamicBonus.Health;
                }

                HandleSpellCastOnTarget(target, spellTargetId);
                if (target.CardKind == CardKind.TavernSpell)
                {
                    tavern.TavernSpellsCastThisTurn += 1;
                    tavern.CardsPlayedThisTurn += 1;
                    tavern.LastTavernSpellCardId = target.CardId;
                    DispatchBoardEvent(MechanicEventType.TavernSpellCast);
                    HandleTavernSpellCastForTierThreeMinions(target);
                    HandleTavernSpellCastForTierFourMinions(target);
                    HandleTavernSpellCastForTierFiveMinions(target);
                    HandleTavernSpellCastForTierSixSevenMinions(target);
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
            tavern.CardsPlayedThisTurn += 1;
            if (target.Tribes.Contains(Tribe.Elemental))
            {
                tavern.ElementalsPlayedThisTurn += 1;
            }

            HandleCardPlayedForTierFiveMinions(target);
            HandleCardPlayedForTierSixSevenMinions(target);
            DispatchSourceEvent(MechanicEventType.CardPlayed, target);
            AddRecruitLog(RecruitLogType.Play, "打出 " + target.Name + FormatTargetSuffix(battlecryTargetName), tavern.Gold, tavern.Gold);
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

        private string ResolveBattlecryTargetId(MinionInstance card, int targetIndex)
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
                case DeepwaterSchoolCardId:
                case ArcaneConsumptionCardId:
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
            var dynamicBonus = GetBoardTavernSpellBonus();
            tavern.TavernSpellBonusAttack += dynamicBonus.Attack;
            tavern.TavernSpellBonusHealth += dynamicBonus.Health;
            string spellResult;
            try
            {
                spellResult = TavernSpellEngine.Cast(spell, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count), resolvedTargetIndex);
                for (var extraCast = 0; extraCast < GetTavernSpellExtraCasts(spell); extraCast += 1)
                {
                    spellResult += " + " + TavernSpellEngine.Cast(spell, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count + extraCast + 1), resolvedTargetIndex);
                }
            }
            finally
            {
                tavern.TavernSpellBonusAttack -= dynamicBonus.Attack;
                tavern.TavernSpellBonusHealth -= dynamicBonus.Health;
            }

            HandleSpellCastOnTarget(spell, spellTargetId);
            if (spell.CardKind == CardKind.TavernSpell)
            {
                tavern.TavernSpellsCastThisTurn += 1;
                tavern.CardsPlayedThisTurn += 1;
                tavern.LastTavernSpellCardId = spell.CardId;
                DispatchBoardEvent(MechanicEventType.TavernSpellCast);
                HandleTavernSpellCastForTierThreeMinions(spell);
                HandleTavernSpellCastForTierFourMinions(spell);
                HandleTavernSpellCastForTierFiveMinions(spell);
                HandleTavernSpellCastForTierSixSevenMinions(spell);
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
                var definition = spellCatalog.All.FirstOrDefault(spell => spell.CardNumber == cardId || spell.Id == cardId);
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
            State.Player.Board.Remove(target);
            ReleaseMinionToPool(target);
            AddRecruitLog(RecruitLogType.Sell, "出售 " + target.Name, before, tavern.Gold);
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

        private void RerollShop()
        {
            var tavern = State.Player.Tavern;
            var costsHealth = tavern.FreeRefreshes <= 0 && tavern.HealthCostRefreshes > 0;
            var cost = tavern.FreeRefreshes > 0 ? 0 : RerollCost;
            if (!costsHealth && tavern.Gold < cost)
            {
                throw new InvalidOperationException("金币不足，无法刷新。");
            }

            if (costsHealth && State.Player.Health <= cost)
            {
                throw new InvalidOperationException("Health is too low to refresh.");
            }

            var before = tavern.Gold;
            var released = ReleaseShopToPool();
            var drawn = CreateShopFromPool(released, tavern.Tier, TavernRules.GetShopSize(tavern.Tier), State.Seed + State.Round * 101 + before, "reroll-" + State.Round + "-" + before);
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
                tavern.Gold -= cost;
                HandleGoldSpent(cost);
            }
            tavern.Shop = drawn.Shop;
            ApplyShopGrowth(tavern.Shop, tavern.Growth.ShopModifiers);
            InjectRefreshCards(tavern.Shop);
            ApplyRefreshBuffToShop(tavern.Shop);
            ApplyRefreshRightmostBuffToShop(tavern.Shop);
            ApplyHelpfulRefresh(tavern.Shop);
            DispatchBoardEvent(MechanicEventType.ShopRefreshed);
            HandleShopRefreshedForTierThreeMinions();
            tavern.Pool = drawn.Pool;
            tavern.Frozen = false;
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

            if (tavern.Gold < tavern.UpgradeCost)
            {
                throw new InvalidOperationException("金币不足，无法升级。");
            }

            var before = tavern.Gold;
            var spent = tavern.UpgradeCost;
            tavern.Gold -= tavern.UpgradeCost;
            HandleGoldSpent(spent);
            tavern.Tier += 1;
            tavern.UpgradeCost = tavern.Tier >= TavernRules.MaxTavernTier ? 0 : TavernRules.GetUpgradeCost(tavern.Tier);
            AddRecruitLog(RecruitLogType.LevelUp, "升级到 " + tavern.Tier + " 本", before, tavern.Gold);
        }

        private void NextTurn()
        {
            DispatchBoardEvent(MechanicEventType.TurnEnded);
            HandleTurnEndedForTierOneMinions();
            HandleTurnEndedForTierThreeMinions();
            HandleTurnEndedForTierFourMinions();
            HandleTurnEndedForTierFiveMinions();
            HandleTurnEndedForTierSixSevenMinions();
            var tavern = State.Player.Tavern;
            var nextRound = State.Round + 1;
            var maxGold = TavernRules.GetMaxGoldForRound(nextRound);
            var bonusGold = tavern.NextTurnBonusGold;
            var wasFrozen = tavern.Frozen;
            var shopState = wasFrozen
                ? new ShopState { Shop = tavern.Shop, Pool = tavern.Pool }
                : CreateShopFromPool(ReleaseShopToPool(), tavern.Tier, TavernRules.GetShopSize(tavern.Tier), State.Seed + nextRound * 997, "turn-" + nextRound);

            State.Round = nextRound;
            State.Phase = MatchPhase.Tavern;
            tavern.Gold = maxGold + bonusGold;
            tavern.MaxGold = maxGold;
            tavern.NextTurnBonusGold = 0;
            tavern.UpgradeCost = TavernRules.DecrementUpgradeCost(tavern.UpgradeCost);
            tavern.Frozen = false;
            tavern.Shop = shopState.Shop;
            if (!wasFrozen)
            {
                ApplyShopGrowth(tavern.Shop, tavern.Growth.ShopModifiers);
                InjectRefreshCards(tavern.Shop);
                ApplyRefreshBuffToShop(tavern.Shop);
                ApplyRefreshRightmostBuffToShop(tavern.Shop);
                ApplyHelpfulRefresh(tavern.Shop);
            }

            tavern.Pool = shopState.Pool;
            tavern.TavernSpellsCastThisTurn = 0;
            tavern.CardsPlayedThisTurn = 0;
            tavern.ElementalsPlayedThisTurn = 0;
            tavern.GoldSpentThisTurn = 0;
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
            AddSpellcraftFromBoard();
            DispatchBoardEvent(MechanicEventType.TurnStarted);
            HandleTurnStartedForTierThreeMinions();
        }

        private void ClearTemporarySpellcraftEffects()
        {
            foreach (var card in State.Player.Board.Concat(State.Player.Tavern.Hand).Concat(State.Player.Tavern.Shop.Where(card => card != null)))
            {
                ClearTemporarySpellcraftEffects(card);
            }
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
                card.Attack -= enchantment.AttackBonus;
                card.MaxHealth = Math.Max(1, card.MaxHealth - enchantment.HealthBonus);
                card.Health = Math.Min(card.Health, card.MaxHealth);
                card.Enchantments.Remove(enchantment);
            }

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
            card.Tags.Remove("surf_n_surf_crab");
            card.Counters.Remove("surf_crab_attack");
            card.Counters.Remove("surf_crab_health");
            if (card.Tags.Remove("temporary_spellcraft_added_deathrattle"))
            {
                card.Keywords.Remove(Keyword.Deathrattle);
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

            State.Player.Tavern.Hand.Add(picked);
            if (discover.Source == "prickly-piper")
            {
                DamagePlayerHero(Math.Max(1, picked.TavernTier));
            }

            AddRecruitLog(RecruitLogType.Discover, "发现 " + picked.Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
            State.Player.Tavern.Discover = null;
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
            var result = CombatEngine.SimulateBasicCombat(
                playerCombatBoard,
                State.Opponent.Board,
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
            State.Player.Tavern.LostLastCombat = result.Winner == CombatWinner.Opponent;
            State.Player.Tavern.TemporaryAvengeBeastRewards = 0;
            State.Player.Tavern.NextCombatBoardAttack = 0;
            State.Player.Tavern.NextCombatBoardHealth = 0;
            State.Player.Tavern.NextCombatBeetles = 0;
            State.Player.Tavern.NextCombatEnemyHealthToOne = 0;
            State.Player.Tavern.NextCombatLeftmostCopiesNearestEnemyStats = false;
            State.Player.Tavern.NextCombatLeftmostDoubleAttack = false;
            State.Player.Tavern.NextCombatTriggerMixedMechanics = false;
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
                var attackDelta = Math.Max(0, final.Attack - original.Attack) * multiplier;
                var healthDelta = Math.Max(0, final.MaxHealth - original.MaxHealth) * multiplier;
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

            var attackDelta = Math.Max(0, final.Attack - original.Attack);
            var healthDelta = Math.Max(0, final.MaxHealth - original.MaxHealth);
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
                        break;
                    case CombatRewardType.FriendlyDeathrattleTriggered:
                        State.Player.Tavern.DeathrattlesTriggeredThisGame += reward.Amount;
                        ApplyFallenSkyGolemBonuses();
                        break;
                    case CombatRewardType.BuffHandMinion:
                        BuffFirstHandMinion(reward.Attack, reward.Health, reward.SourceCardId);
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
                SetTrackedBuff(card, "Fallen Sky Golem", triggers * 4 * multiplier, triggers * 2 * multiplier);
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

                var attack = State.Player.Tavern.EternalKnightDeaths * (card.Golden ? 8 : 4);
                var health = State.Player.Tavern.EternalKnightDeaths * (card.Golden ? 4 : 2);
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

                var attack = otherSummons * (card.Golden ? 6 : 3);
                var health = otherSummons * (card.Golden ? 4 : 2);
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

            BuffMinion(target, 0, (1 + Math.Max(0, State.Player.Tavern.GoldSpentThisTurn)) * multiplier, "Balladist");
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
                ResolveTierOneBattlecry(target);
                ResolveTierThreeBattlecry(target, battlecryTargetId);
                ResolveTierFourBattlecry(target, battlecryTargetId);
                ResolveTierFiveBattlecry(target);
                ResolveTierSixSevenBattlecry(target);
                ResolveKalecgosBattlecryTrigger(target);
            }
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
            return brann == null ? 1 : brann.Golden ? 3 : 2;
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
            if (spell == null || spell.CardKind != CardKind.TavernSpell)
            {
                return 0;
            }

            var extra = 0;
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

            return extra;
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
                    State.Player.Tavern.FreeRefreshes += 2 * multiplier;
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
                    State.Player.Tavern.RefreshBuffAttack += 7 * multiplier;
                    State.Player.Tavern.RefreshBuffHealth += 7 * multiplier;
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
                    BuffMinion(FirstOtherFriendlyMinion(target) ?? target, 2 * multiplier + State.Player.Tavern.TavernSpellsCastThisTurn, 2 * multiplier + State.Player.Tavern.TavernSpellsCastThisTurn, "Saloon Dancer");
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
                var attack = (target.Golden ? 2 : 1) + State.Player.Tavern.FutureBallerAttackBonus;
                BuffAllMinions(State.Player.Board, attack, 0, "火焰投球手");
                State.Player.Tavern.FutureBallerAttackBonus += target.Golden ? 2 : 1;
                return;
            }

            if (target.CardId == SnowBallerCardId)
            {
                var health = (target.Golden ? 2 : 1) + State.Player.Tavern.FutureBallerHealthBonus;
                BuffAllMinions(State.Player.Board, 0, health, "冰雪投球手");
                State.Player.Tavern.FutureBallerHealthBonus += target.Golden ? 2 : 1;
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
                    case WoodlandWardenCardId:
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
                        BuffOneOfEachFriendlyType(4 * multiplier, 3 * multiplier, "Nalaa the Redeemer");
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
                        BuffAllMinions(State.Player.Board.Where(card => card.Keywords.Contains(Keyword.DivineShield)), 4 * multiplier, 0, "Charging Czarina");
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
                        minion.Counters["dragon_spell_attack"] = attack + 2 * multiplier;
                        minion.Counters["dragon_spell_health"] = health + multiplier;
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
                foreach (var groundbreaker in State.Player.Board.Where(minion => minion.CardId == GroundbreakerCardId).ToList())
                {
                    groundbreaker.Counters.TryGetValue("groundbreaker_bonus", out var bonus);
                    var amount = (groundbreaker.Golden ? 2 : 1) + bonus;
                    BuffMinion(groundbreaker, amount, amount, "Groundbreaker");
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
                        beast.Health -= 1;
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
                        BuffMinion(handTarget, minion.Attack * multiplier, minion.MaxHealth * multiplier, "Future Murloc");
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
                BuffMinion(bought, bought.Attack * multiplier, bought.MaxHealth * multiplier, "Stone Age Rock Rock multiplier");
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

        private void HandleSpellCastOnTarget(MinionInstance spell, string targetInstanceId)
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

            if (spell.Tags.Contains("spellcraft") && target.CardId == ZestyShakerCardId)
            {
                target.Counters.TryGetValue("zesty-copy-round", out var copiedRound);
                if (copiedRound != State.Round && State.Player.Tavern.Hand.Count < HandLimit)
                {
                    var copy = spell.Clone();
                    copy.InstanceId = "zesty-copy-" + State.Round + "-" + State.Player.Tavern.Hand.Count;
                    copy.PoolSource = PoolSource.Copy;
                    copy.PoolCopiesHeld = 0;
                    State.Player.Tavern.Hand.Add(copy);
                    target.Counters["zesty-copy-round"] = State.Round;
                    HandleCardsAddedToHand(1, "zesty-shaker");
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
            State.Player.Tavern.Growth.ShopModifiers.Add(new TavernGrowthModifier
            {
                Scope = BuffScope.ShopGlobal,
                Tribe = tribe,
                Attack = attack,
                Health = health,
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
                TriggerAshenCorruptors(amount);
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
                    copy.Attack *= 2;
                    copy.MaxHealth *= 2;
                    copy.Health = copy.MaxHealth;
                }

                board.Add(copy);
            }

            return board;
        }

        private void DevourRandomShopMinion(MinionInstance eater, int multiplier)
        {
            var candidates = State.Player.Tavern.Shop
                .Select((card, index) => new { Card = card, Index = index })
                .Where(item => item.Card != null && item.Card.CardKind == CardKind.Minion)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            var rng = new SeededRng(State.Seed + State.Round * 313 + State.Player.Tavern.RecruitLog.Count);
            var picked = rng.Pick(candidates);
            State.Player.Tavern.Shop[picked.Index] = null;
            BuffMinion(eater, picked.Card.Attack * multiplier, picked.Card.Health * multiplier, "挑食魔犬");
            HandleDevourForTierSixSevenMinions();
            ReleaseMinionToPool(picked.Card);
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
            BuffMinion(eater, picked.Card.Attack * multiplier, picked.Card.MaxHealth * multiplier, "Wildfire Executioner");
            HandleDevourForTierSixSevenMinions();
            ReleaseMinionToPool(picked.Card);
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

        private void AddTavernSpellToHand(string cardNumber, string source)
        {
            if (State.Player.Tavern.Hand.Count >= HandLimit)
            {
                return;
            }

            var definition = spellCatalog.All.FirstOrDefault(spell => spell.CardNumber == cardNumber);
            if (definition != null)
            {
                State.Player.Tavern.Hand.Add(MinionFactory.Create(definition, BoardSide.Player, source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
                HandleCardsAddedToHand(1, source);
            }
        }

        private void CastTavernSpellImmediate(string cardNumber, string source)
        {
            var definition = spellCatalog.All.FirstOrDefault(spell => spell.CardNumber == cardNumber || spell.Id == cardNumber);
            if (definition == null)
            {
                return;
            }

            var spell = MinionFactory.Create(definition, BoardSide.Player, source + "-" + State.Round + "-" + State.Player.Tavern.RecruitLog.Count);
            TavernSpellEngine.Cast(spell, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 701 + State.Player.Tavern.RecruitLog.Count));
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
                case FrostlingPriestessSpellCardId:
                    return CreateFrostlingPriestessSpellCard(suffix);
                case DeepwaterSchoolCardId:
                    return CreateDeepwaterSchoolCard(suffix);
                case ArcaneConsumptionCardId:
                    return CreateArcaneConsumptionCard(suffix);
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

        private void AddRandomTierMinionsToHand(int tier, int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 673 + State.Player.Tavern.RecruitLog.Count);
            var candidates = AvailableMinions().Where(minion => minion.InPool && minion.TavernTier == tier && !minion.CardId.StartsWith("BGDUO", StringComparison.Ordinal)).ToList();
            AddRandomMinionsFromCandidates(candidates, count, source, rng);
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
                return;
            }

            State.Player.Tavern.Hand.Add(MinionFactory.Create(definition, BoardSide.Player, source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count, false, PoolSource.Copy, 0));
            HandleCardsAddedToHand(1, source);
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
            var rng = new SeededRng(State.Seed + State.Round * 607 + State.Player.Tavern.RecruitLog.Count);
            var ids = new[] { BlueChromawhelpCardId, BlackChromawhelpCardId, GreenChromawhelpCardId, BronzeChromawhelpCardId, RedChromawhelpCardId };
            var candidates = catalog.All.Where(minion => ids.Contains(minion.CardId)).ToList();
            AddRandomMinionsFromCandidates(candidates, count, source, rng);
        }

        private void AddRandomMinionsFromCandidates(List<MinionDefinition> candidates, int count, string source, SeededRng rng)
        {
            var before = State.Player.Tavern.Hand.Count;
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; index += 1)
            {
                State.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, source + "-" + State.Round + "-" + index, false, PoolSource.Copy, 0));
            }

            HandleCardsAddedToHand(State.Player.Tavern.Hand.Count - before, source);
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
                Tags = new List<string> { "generated_spell", "spellcraft", "targeted_spell", "buff_spell" }
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
                Tags = new List<string> { "generated_spell", "spellcraft", "targeted_spell", "deathrattle_grant" }
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
                Tags = new List<string> { "generated_spell", "spellcraft", "targeted_spell", "buff_spell", "taunt_grant" }
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
                Tags = new List<string> { "generated_spell", "spellcraft", "targeted_spell", "buff_spell" }
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
                Tags = new List<string> { "generated_spell", "spellcraft", "targeted_spell", "buff_spell", attackChoice ? "attack_buff_spell" : "health_buff_spell" }
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
                Tags = new List<string> { "generated_spell", "spellcraft", "generated_tavern_spell", "stat_tavern_spell" }
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

            target.Attack += attack;
            target.MaxHealth += health;
            target.Health += health;
            target.Enchantments.Add(new Enchantment
            {
                Id = sourceId,
                SourceId = sourceId,
                AttackBonus = attack,
                HealthBonus = health
            });
            RefreshScarletSurvivor(target);
            ResolveHighTierBuffTriggers(target, attack, health);
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
                BuffMinion(target, health * (slitherspear.Golden ? 2 : 1), 0, "Slitherspear");
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
            var attackDelta = attack - currentAttack;
            var healthDelta = health - currentHealth;
            target.Attack += attackDelta;
            target.MaxHealth += healthDelta;
            target.Health += healthDelta;

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

        private static void MakeGoldenInPlace(MinionInstance target)
        {
            if (target == null || target.Golden)
            {
                return;
            }

            target.Golden = true;
            target.Attack *= 2;
            target.MaxHealth *= 2;
            target.Health *= 2;
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
            var candidate = TripleEngine.FindTripleCandidate(all);
            if (string.IsNullOrEmpty(candidate) && TryResolveSurpriseElementalTriple(all))
            {
                return;
            }

            if (string.IsNullOrEmpty(candidate))
            {
                return;
            }

            var result = TripleEngine.ResolveTriple(all, candidate, BoardSide.Player, State.Round + "-" + State.Player.Tavern.RecruitLog.Count);
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
            golden.Attack = baseItem.Attack * 2;
            golden.Health = baseItem.Health * 2;
            golden.MaxHealth = baseItem.MaxHealth * 2;
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

        private ShopState CreateShopFromPool(IDictionary<string, int> snapshot, int tier, int size, int seed, string suffix)
        {
            var pool = new MinionPool(catalog.All, snapshot, CurrentActiveTribes());
            var rng = new SeededRng(seed);
            var spell = DrawTavernSpell(tier, rng);
            var definitions = pool.DrawShop(tier, size, rng);
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

        private TavernSpellDefinition DrawTavernSpell(int tier, SeededRng rng)
        {
            var candidates = AvailableTavernSpells()
                .Where(spell => spell.TavernTier <= tier)
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

        private Dictionary<string, int> ReleaseShopToPool()
        {
            var pool = new MinionPool(catalog.All, State.Player.Tavern.Pool, CurrentActiveTribes());
            foreach (var minion in State.Player.Tavern.Shop)
            {
                if (minion != null && minion.PoolCopiesHeld > 0)
                {
                    pool.Release(minion.DefinitionId, minion.PoolCopiesHeld);
                }
            }

            return pool.Snapshot();
        }

        private void ReleaseMinionToPool(MinionInstance minion)
        {
            var pool = new MinionPool(catalog.All, State.Player.Tavern.Pool, CurrentActiveTribes());
            if (minion.PoolCopiesHeld > 0)
            {
                pool.Release(minion.DefinitionId, minion.PoolCopiesHeld);
            }

            State.Player.Tavern.Pool = pool.Snapshot();
        }

        private void AddRecruitLog(RecruitLogType type, string message, int goldBefore, int goldAfter)
        {
            State.Player.Tavern.RecruitLog.Add(new RecruitLogEntry
            {
                Seq = State.Player.Tavern.RecruitLog.Count + 1,
                Round = State.Round,
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
