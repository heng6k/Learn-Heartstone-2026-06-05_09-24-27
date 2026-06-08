using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class TestScenarioMapper
    {
        public static TestScenarioDefinition Capture(MatchState state, string name)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var tavern = state.Player.Tavern;
            return new TestScenarioDefinition
            {
                Name = name,
                SavedAtRound = state.Round,
                Seed = state.Seed,
                Phase = state.Phase,
                Player = new PlayerScenarioState
                {
                    HeroId = state.Player.HeroId,
                    Health = state.Player.Health,
                    Armor = state.Player.Armor
                },
                Opponent = new OpponentScenarioState
                {
                    Name = state.Opponent.Name,
                    HeroId = state.Opponent.HeroId,
                    Health = state.Opponent.Health,
                    Armor = state.Opponent.Armor,
                    TavernTier = state.Opponent.TavernTier,
                    Editable = state.Opponent.Editable
                },
                Tavern = new ScenarioTavernState
                {
                    Tier = tavern.Tier,
                    Gold = tavern.Gold,
                    MaxGold = tavern.MaxGold,
                    UpgradeCost = tavern.UpgradeCost,
                    Frozen = tavern.Frozen,
                    NextTurnBonusGold = tavern.NextTurnBonusGold,
                    NextTavernSpellCostReduction = tavern.NextTavernSpellCostReduction,
                    FreeRefreshes = tavern.FreeRefreshes,
                    DemonFodderRefreshes = tavern.DemonFodderRefreshes,
                    TavernSpellBonusAttack = tavern.TavernSpellBonusAttack,
                    TavernSpellBonusHealth = tavern.TavernSpellBonusHealth,
                    BeetleAttackBonus = tavern.BeetleAttackBonus,
                    BeetleHealthBonus = tavern.BeetleHealthBonus,
                    FutureBallerAttackBonus = tavern.FutureBallerAttackBonus,
                    FutureBallerHealthBonus = tavern.FutureBallerHealthBonus,
                    UndeadAttackBonus = tavern.UndeadAttackBonus,
                    EternalKnightDeaths = tavern.EternalKnightDeaths,
                    AncestralAutomatonSummons = tavern.AncestralAutomatonSummons,
                    FriendlyMinionDeathsThisGame = tavern.FriendlyMinionDeathsThisGame
                },
                Shop = CaptureCards(tavern.Shop),
                Hand = CaptureCards(tavern.Hand),
                PlayerBoard = CaptureCards(state.Player.Board),
                OpponentBoard = CaptureCards(state.Opponent.Board)
            };
        }

        public static TestScenarioDefinition Clone(TestScenarioDefinition scenario)
        {
            var cloneState = new MatchState();
            ApplyTo(cloneState, scenario);
            return Capture(cloneState, scenario.Name);
        }

        public static void ApplyTo(MatchState target, TestScenarioDefinition scenario)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            target.Phase = scenario.Phase;
            target.Round = Math.Max(1, scenario.SavedAtRound);
            target.Seed = scenario.Seed;
            target.Player.HeroId = scenario.Player?.HeroId;
            target.Player.Health = scenario.Player?.Health ?? target.Player.Health;
            target.Player.Armor = scenario.Player?.Armor ?? target.Player.Armor;
            target.Opponent.Name = string.IsNullOrEmpty(scenario.Opponent?.Name) ? target.Opponent.Name : scenario.Opponent.Name;
            target.Opponent.HeroId = scenario.Opponent?.HeroId;
            target.Opponent.Health = scenario.Opponent?.Health ?? target.Opponent.Health;
            target.Opponent.Armor = scenario.Opponent?.Armor ?? target.Opponent.Armor;
            target.Opponent.TavernTier = Math.Max(1, scenario.Opponent?.TavernTier ?? target.Opponent.TavernTier);
            target.Opponent.Editable = scenario.Opponent?.Editable ?? target.Opponent.Editable;

            var tavern = target.Player.Tavern;
            tavern.Tier = Math.Max(1, scenario.Tavern?.Tier ?? tavern.Tier);
            tavern.Gold = Math.Max(0, scenario.Tavern?.Gold ?? tavern.Gold);
            tavern.MaxGold = Math.Max(tavern.Gold, scenario.Tavern?.MaxGold ?? tavern.MaxGold);
            tavern.UpgradeCost = Math.Max(0, scenario.Tavern?.UpgradeCost ?? tavern.UpgradeCost);
            tavern.Frozen = scenario.Tavern?.Frozen ?? tavern.Frozen;
            tavern.NextTurnBonusGold = Math.Max(0, scenario.Tavern?.NextTurnBonusGold ?? tavern.NextTurnBonusGold);
            tavern.NextTavernSpellCostReduction = Math.Max(0, scenario.Tavern?.NextTavernSpellCostReduction ?? tavern.NextTavernSpellCostReduction);
            tavern.FreeRefreshes = Math.Max(0, scenario.Tavern?.FreeRefreshes ?? tavern.FreeRefreshes);
            tavern.DemonFodderRefreshes = Math.Max(0, scenario.Tavern?.DemonFodderRefreshes ?? tavern.DemonFodderRefreshes);
            tavern.TavernSpellBonusAttack = Math.Max(0, scenario.Tavern?.TavernSpellBonusAttack ?? tavern.TavernSpellBonusAttack);
            tavern.TavernSpellBonusHealth = Math.Max(0, scenario.Tavern?.TavernSpellBonusHealth ?? tavern.TavernSpellBonusHealth);
            tavern.BeetleAttackBonus = Math.Max(0, scenario.Tavern?.BeetleAttackBonus ?? tavern.BeetleAttackBonus);
            tavern.BeetleHealthBonus = Math.Max(0, scenario.Tavern?.BeetleHealthBonus ?? tavern.BeetleHealthBonus);
            tavern.FutureBallerAttackBonus = Math.Max(0, scenario.Tavern?.FutureBallerAttackBonus ?? tavern.FutureBallerAttackBonus);
            tavern.FutureBallerHealthBonus = Math.Max(0, scenario.Tavern?.FutureBallerHealthBonus ?? tavern.FutureBallerHealthBonus);
            tavern.UndeadAttackBonus = Math.Max(0, scenario.Tavern?.UndeadAttackBonus ?? tavern.UndeadAttackBonus);
            tavern.EternalKnightDeaths = Math.Max(0, scenario.Tavern?.EternalKnightDeaths ?? tavern.EternalKnightDeaths);
            tavern.AncestralAutomatonSummons = Math.Max(0, scenario.Tavern?.AncestralAutomatonSummons ?? tavern.AncestralAutomatonSummons);
            tavern.FriendlyMinionDeathsThisGame = Math.Max(0, scenario.Tavern?.FriendlyMinionDeathsThisGame ?? tavern.FriendlyMinionDeathsThisGame);

            tavern.Shop = RestoreCards(scenario.Shop, BoardSide.Player);
            tavern.Hand = RestoreCards(scenario.Hand, BoardSide.Player);
            target.Player.Board = RestoreCards(scenario.PlayerBoard, BoardSide.Player);
            target.Opponent.Board = RestoreCards(scenario.OpponentBoard, BoardSide.Opponent);
            target.CombatLog.Clear();
            target.LastResult = null;
        }

        private static List<ScenarioCardState> CaptureCards(IEnumerable<MinionInstance> cards)
        {
            return cards == null
                ? new List<ScenarioCardState>()
                : cards.Where(card => card != null).Select(CaptureCard).ToList();
        }

        private static ScenarioCardState CaptureCard(MinionInstance card)
        {
            return new ScenarioCardState
            {
                CardKind = card.CardKind,
                InstanceId = card.InstanceId,
                DefinitionId = card.DefinitionId,
                CardId = card.CardId,
                Name = card.Name,
                Cost = card.Cost,
                BaseAttack = card.BaseAttack,
                BaseHealth = card.BaseHealth,
                Attack = card.Attack,
                Health = card.Health,
                MaxHealth = card.MaxHealth,
                TavernTier = card.TavernTier,
                Tribes = new List<Tribe>(card.Tribes),
                Keywords = new List<Keyword>(card.Keywords),
                Text = card.Text,
                Golden = card.Golden,
                Owner = card.Owner,
                Enchantments = card.Enchantments.Select(enchantment => new ScenarioEnchantmentState
                {
                    Id = enchantment.Id,
                    SourceId = enchantment.SourceId,
                    AttackBonus = enchantment.AttackBonus,
                    HealthBonus = enchantment.HealthBonus,
                    AddedKeywords = new List<Keyword>(enchantment.AddedKeywords),
                    Duration = enchantment.Duration
                }).ToList(),
                Counters = card.Counters.Select(counter => new ScenarioCounterState { Key = counter.Key, Value = counter.Value }).ToList(),
                CanAttack = card.CanAttack,
                AttacksThisCombat = card.AttacksThisCombat,
                OriginPoolSource = card.OriginPoolSource,
                CanReturnToPoolAfterAttach = card.CanReturnToPoolAfterAttach,
                PoolSource = card.PoolSource,
                PoolCopiesHeld = card.PoolCopiesHeld,
                ImagePath = card.ImagePath,
                EffectIds = new List<string>(card.EffectIds),
                Tags = card.Tags == null ? new List<string>() : new List<string>(card.Tags)
            };
        }

        private static List<MinionInstance> RestoreCards(IEnumerable<ScenarioCardState> cards, BoardSide owner)
        {
            return cards == null
                ? new List<MinionInstance>()
                : cards.Where(card => card != null).Select(card => RestoreCard(card, owner)).ToList();
        }

        private static MinionInstance RestoreCard(ScenarioCardState card, BoardSide owner)
        {
            var maxHealth = Math.Max(1, card.MaxHealth);
            var health = Math.Max(1, Math.Min(card.Health, maxHealth));
            if (card.CardKind == CardKind.TavernSpell || card.CardKind == CardKind.Spell)
            {
                maxHealth = 0;
                health = 0;
            }

            return new MinionInstance
            {
                CardKind = card.CardKind,
                InstanceId = string.IsNullOrEmpty(card.InstanceId) ? owner.ToString().ToLowerInvariant() + "-" + card.DefinitionId + "-scenario" : card.InstanceId,
                DefinitionId = card.DefinitionId,
                CardId = card.CardId,
                Name = card.Name,
                Cost = card.Cost,
                BaseAttack = Math.Max(0, card.BaseAttack),
                BaseHealth = Math.Max(0, card.BaseHealth),
                Attack = Math.Max(0, card.Attack),
                Health = health,
                MaxHealth = maxHealth,
                TavernTier = Math.Max(0, card.TavernTier),
                Tribes = card.Tribes == null ? new List<Tribe> { Tribe.None } : new List<Tribe>(card.Tribes),
                Keywords = card.Keywords == null ? new List<Keyword>() : new List<Keyword>(card.Keywords),
                Text = card.Text,
                Golden = card.Golden,
                Owner = owner,
                Enchantments = card.Enchantments == null
                    ? new List<Enchantment>()
                    : card.Enchantments.Select(enchantment => new Enchantment
                    {
                        Id = enchantment.Id,
                        SourceId = enchantment.SourceId,
                        AttackBonus = enchantment.AttackBonus,
                        HealthBonus = enchantment.HealthBonus,
                        AddedKeywords = enchantment.AddedKeywords == null ? new List<Keyword>() : new List<Keyword>(enchantment.AddedKeywords),
                        Duration = enchantment.Duration
                    }).ToList(),
                Counters = card.Counters == null ? new Dictionary<string, int>() : card.Counters.ToDictionary(counter => counter.Key, counter => counter.Value),
                CanAttack = card.CanAttack,
                AttacksThisCombat = card.AttacksThisCombat,
                OriginPoolSource = card.OriginPoolSource,
                CanReturnToPoolAfterAttach = card.CanReturnToPoolAfterAttach,
                PoolSource = card.PoolSource,
                PoolCopiesHeld = card.PoolCopiesHeld,
                ImagePath = card.ImagePath,
                EffectIds = card.EffectIds == null ? new List<string>() : new List<string>(card.EffectIds),
                Tags = card.Tags == null ? new List<string>() : new List<string>(card.Tags)
            };
        }
    }
}
