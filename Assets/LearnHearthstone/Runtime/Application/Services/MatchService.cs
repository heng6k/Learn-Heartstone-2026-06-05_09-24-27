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
        private const string WrathWeaverCardId = "BGS_004";

        private readonly MinionCatalog catalog;
        private readonly SpellCatalog spellCatalog;
        private readonly MinionEffectCatalog effectCatalog;
        private readonly ITestScenarioRepository scenarioRepository;
        private CombatTestSnapshot combatTestSnapshot;

        private MatchService(MinionCatalog catalog, SpellCatalog spellCatalog, int seed, ITestScenarioRepository scenarioRepository)
        {
            this.catalog = catalog;
            this.spellCatalog = spellCatalog;
            this.scenarioRepository = scenarioRepository ?? new FileTestScenarioRepository();
            effectCatalog = MinionEffectCatalog.CreateDefault();
            State = CreateMatch(seed);
        }

        public MatchState State { get; private set; }

        public CombatTestSnapshot LastCombatTestSnapshot => combatTestSnapshot;

        public bool HasCombatTestSnapshot => combatTestSnapshot?.BeforeCombat != null;

        public IReadOnlyList<string> TestScenarioNames => scenarioRepository.ListScenarioNames();

        public static MatchService CreateWithDefaultCatalog(int seed = 12345, ITestScenarioRepository scenarios = null)
        {
            return new MatchService(MinionCatalogLoader.LoadFromResources(), SpellCatalogLoader.LoadFromResources(), seed, scenarios);
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
                case GameCommandType.AddOpponentMinion:
                    AddOpponentMinion(command.InstanceId);
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

            return State;
        }

        private MatchState CreateMatch(int seed)
        {
            var initial = CreateShopFromPool(null, 1, TavernRules.GetShopSize(1), seed, "shop-1");
            return new MatchState
            {
                Mode = MatchMode.TavernPractice,
                Phase = MatchPhase.Tavern,
                Round = 1,
                Seed = seed,
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

            if (tavern.Gold < cost)
            {
                throw new InvalidOperationException("金币不足。");
            }

            var before = tavern.Gold;
            tavern.Gold -= cost;
            tavern.Hand.Add(target);
            tavern.Shop[shopIndex] = null;
            if (target.CardKind == CardKind.TavernSpell)
            {
                tavern.NextTavernSpellCostReduction = 0;
            }

            AddRecruitLog(RecruitLogType.Buy, "购买 " + target.Name, before, tavern.Gold);
            DispatchBoardEvent(MechanicEventType.CardBought);
            HandleCardBoughtForTierOneMinions();
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
            if (IsTripleRewardCard(target))
            {
                tavern.Hand.RemoveAt(handIndex);
                State.Player.Tavern.Discover = CreateTripleDiscover();
                AddRecruitLog(RecruitLogType.Discover, "Triple reward discover", tavern.Gold, tavern.Gold);
                return;
            }

            if (target.CardKind == CardKind.TavernSpell)
            {
                tavern.Hand.RemoveAt(handIndex);
                var spellResult = TavernSpellEngine.Cast(target, State, catalog, spellCatalog, new SeededRng(State.Seed + State.Round * 1777 + tavern.RecruitLog.Count));
                DispatchBoardEvent(MechanicEventType.TavernSpellCast);
                AddRecruitLog(RecruitLogType.Play, "施放 " + target.Name + " - " + spellResult, tavern.Gold, tavern.Gold);
                return;
            }

            if (State.Player.Board.Count >= BoardLimit)
            {
                throw new InvalidOperationException("战场已满。");
            }

            tavern.Hand.RemoveAt(handIndex);
            target.Owner = BoardSide.Player;
            target.InstanceId = "player-" + target.DefinitionId + "-play-" + State.Round + "-" + handIndex;
            State.Player.Board.Insert(NormalizeBoardInsertIndex(targetIndex, State.Player.Board.Count), target);
            ResolveTierOneBattlecry(target);
            DispatchSourceEvent(MechanicEventType.CardPlayed, target);
            AddRecruitLog(RecruitLogType.Play, "打出 " + target.Name, tavern.Gold, tavern.Gold);
            HandleDemonPlayedForWrathWeavers(target);
            if (target.Golden && !HasGrantedTripleReward(target))
            {
                MarkTripleRewardGranted(target);
                GrantTripleRewardCard();
            }

            ResolvePlayerTriples();
        }

        private void AddCardToHand(string cardId, CardKind cardKind)
        {
            var tavern = State.Player.Tavern;
            if (string.IsNullOrEmpty(cardId))
            {
                throw new InvalidOperationException("Card id is required.");
            }

            if (tavern.Hand.Count >= HandLimit)
            {
                throw new InvalidOperationException("Hand is full.");
            }

            MinionInstance card;
            if (cardKind == CardKind.Minion)
            {
                var definition = catalog.GetByCardId(cardId);
                card = MinionFactory.Create(definition, BoardSide.Player, "debug-hand-" + State.Round + "-" + tavern.Hand.Count, false, PoolSource.Debug, 0);
            }
            else if (cardKind == CardKind.TavernSpell)
            {
                var definition = spellCatalog.All.FirstOrDefault(spell => spell.CardNumber == cardId || spell.Id == cardId);
                if (definition == null)
                {
                    throw new InvalidOperationException("Spell card id does not exist: " + cardId);
                }

                card = MinionFactory.Create(definition, BoardSide.Player, "debug-hand-" + State.Round + "-" + tavern.Hand.Count);
                card.PoolSource = PoolSource.Debug;
                card.OriginPoolSource = PoolSource.Debug;
            }
            else
            {
                throw new InvalidOperationException("Unsupported card kind: " + cardKind);
            }

            tavern.Hand.Add(card);
            AddRecruitLog(RecruitLogType.Buy, "Debug add " + card.Name, tavern.Gold, tavern.Gold);
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
            tavern.Gold = Math.Min(tavern.MaxGold, tavern.Gold + SellValue);
            DispatchSourceEvent(MechanicEventType.MinionSold, target);
            ResolveTierOneSellEffect(target);
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

        private void AddOpponentMinion(string cardId)
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
            var minion = MinionFactory.Create(definition, BoardSide.Opponent, "debug-board-" + State.Round + "-" + State.Opponent.Board.Count, false, PoolSource.Debug, 0);
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
            if (tavern.Gold < RerollCost)
            {
                throw new InvalidOperationException("金币不足，无法刷新。");
            }

            var before = tavern.Gold;
            var released = ReleaseShopToPool();
            var drawn = CreateShopFromPool(released, tavern.Tier, TavernRules.GetShopSize(tavern.Tier), State.Seed + State.Round * 101 + before, "reroll-" + State.Round + "-" + before);
            tavern.Gold -= RerollCost;
            tavern.Shop = drawn.Shop;
            ApplyShopGrowth(tavern.Shop, tavern.Growth.ShopModifiers);
            DispatchBoardEvent(MechanicEventType.ShopRefreshed);
            tavern.Pool = drawn.Pool;
            tavern.Frozen = false;
            tavern.SearchPlan.GoldSpentOnRerollThisTurn += RerollCost;
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
            tavern.Gold -= tavern.UpgradeCost;
            tavern.Tier += 1;
            tavern.UpgradeCost = tavern.Tier >= TavernRules.MaxTavernTier ? 0 : TavernRules.GetUpgradeCost(tavern.Tier);
            AddRecruitLog(RecruitLogType.LevelUp, "升级到 " + tavern.Tier + " 本", before, tavern.Gold);
        }

        private void NextTurn()
        {
            DispatchBoardEvent(MechanicEventType.TurnEnded);
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
            }

            tavern.Pool = shopState.Pool;
            tavern.SearchPlan.GoldSpentOnRerollThisTurn = 0;
            tavern.SearchPlan.HitsThisTurn.Clear();
            State.CombatLog.Clear();
            State.LastResult = null;
            AddRecruitLog(RecruitLogType.TurnStart, "第 " + nextRound + " 回合开始", 0, tavern.Gold);
            DispatchBoardEvent(MechanicEventType.TurnStarted);
        }

        private void ChooseDiscover(int optionIndex)
        {
            var discover = State.Player.Tavern.Discover;
            if (discover == null || optionIndex < 0 || optionIndex >= discover.Options.Count)
            {
                throw new InvalidOperationException("发现奖励不存在。");
            }

            State.Player.Tavern.Hand.Add(discover.Options[optionIndex]);
            AddRecruitLog(RecruitLogType.Discover, "发现 " + discover.Options[optionIndex].Name, State.Player.Tavern.Gold, State.Player.Tavern.Gold);
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
            combatTestSnapshot = null;
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
            var result = CombatEngine.SimulateBasicCombat(playerCombatBoard, State.Opponent.Board, nextOptions.Seed, nextOptions.SafetyLimit);
            State.Phase = MatchPhase.Result;
            State.CombatLog = result.Log;
            State.LastResult = result;
            combatTestSnapshot.Result = result;
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
            }
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
                    State.Player.Health = Math.Max(0, State.Player.Health - 1);
                    BuffMinion(weaver, 2, 1, "愤怒编织者");
                }
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
            ReleaseMinionToPool(picked.Card);
        }

        private void AddBloodGemsToHand(int count, string source)
        {
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit; index += 1)
            {
                State.Player.Tavern.Hand.Add(CreateBloodGemCard(source + "-" + State.Round + "-" + State.Player.Tavern.Hand.Count));
            }
        }

        private void AddRandomTierOneMinionsToHand(int count, string source)
        {
            var rng = new SeededRng(State.Seed + State.Round * 431 + State.Player.Tavern.RecruitLog.Count);
            var candidates = catalog.All.Where(minion => minion.InPool && minion.TavernTier == 1).ToList();
            for (var index = 0; index < count && State.Player.Tavern.Hand.Count < HandLimit && candidates.Count > 0; index += 1)
            {
                State.Player.Tavern.Hand.Add(MinionFactory.Create(rng.Pick(candidates), BoardSide.Player, source + "-" + State.Round + "-" + index, false, PoolSource.Copy, 0));
            }
        }

        private static MinionInstance CreateBloodGemCard(string suffix)
        {
            return new MinionInstance
            {
                CardKind = CardKind.TavernSpell,
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
                Keywords = new List<Keyword> { Keyword.TavernSpell, Keyword.BloodGem },
                Text = "使一个友方随从获得+1/+1。",
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                Tags = new List<string> { "generated_spell", "blood_gem", "targeted_spell", "buff_spell" }
            };
        }

        private static void BuffMinion(MinionInstance target, int attack, int health, string sourceId)
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
            var candidates = catalog.All.Where(definition => definition.InPool && definition.TavernTier == rewardTier).ToList();
            if (candidates.Count < 3)
            {
                candidates = catalog.All.Where(definition => definition.InPool && definition.TavernTier <= rewardTier).ToList();
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
            var pool = new MinionPool(catalog.All, snapshot);
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
            var candidates = spellCatalog.GetTavernSpellsForTier(tier);
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
                    if (modifier.Scope != BuffScope.ShopGlobal || !MatchesTribe(minion, modifier.Tribe))
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

        private Dictionary<string, int> ReleaseShopToPool()
        {
            var pool = new MinionPool(catalog.All, State.Player.Tavern.Pool);
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
            var pool = new MinionPool(catalog.All, State.Player.Tavern.Pool);
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
