using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public static class DesignValidationScenarioCatalog
    {
        public const string UndeadRevengeGrowth = "DVT - Undead Revenge Growth";
        public const string SpellFlow = "DVT - Spell Flow";
        public const string HistoricalStats = "DVT - Historical Stats";
        public const string EconomyToPower = "DVT - Economy To Power";
        public const string FullNextTurnFlow = "DVT - Full Next Turn Flow";

        public static IReadOnlyList<TestScenarioDefinition> ListScenarios()
        {
            return new List<TestScenarioDefinition>
            {
                Clone(CreateUndeadRevengeGrowth()),
                Clone(CreateSpellFlow()),
                Clone(CreateHistoricalStats()),
                Clone(CreateEconomyToPower()),
                Clone(CreateFullNextTurnFlow())
            };
        }

        public static bool TryGetScenario(string name, out TestScenarioDefinition scenario)
        {
            foreach (var candidate in ListScenarios())
            {
                if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    scenario = candidate;
                    return true;
                }
            }

            scenario = null;
            return false;
        }

        private static TestScenarioDefinition CreateUndeadRevengeGrowth()
        {
            var scenario = BaseScenario(UndeadRevengeGrowth, 8, 91001);
            scenario.PlayerBoard.Add(Minion("player-anchor", "Bumper", "DVT_BUMPER", 7, 7, Tribe.None, BoardSide.Player, 3, Keyword.Taunt));
            scenario.PlayerBoard.Add(Minion("player-cleanup", "Cleanup Attacker", "DVT_CLEANUP", 5, 4, Tribe.None, BoardSide.Player, 3));
            scenario.OpponentBoard.Add(Minion("opponent-choral", "Choral Mrrrglr", "BG26_354", 6, 6, Tribe.Murloc, BoardSide.Opponent, 6));
            scenario.OpponentBoard.Add(Minion("opponent-bassgill", "Bassgill", "BG26_350", 8, 3, Tribe.Murloc, BoardSide.Opponent, 4, Keyword.Deathrattle));
            scenario.OpponentBoard.Add(Minion("opponent-eternal", "Eternal Knight", "BG25_008", 4, 1, Tribe.Undead, BoardSide.Opponent, 2));
            scenario.OpponentHand.Add(Minion("opponent-hand-big-murloc", "Hand Murloc Threat", "DVT_HAND_MURLOC", 12, 14, Tribe.Murloc, BoardSide.Opponent, 5));
            scenario.OpponentHand.Add(Minion("opponent-hand-undead", "Hand Undead Followup", "DVT_HAND_UNDEAD", 7, 6, Tribe.Undead, BoardSide.Opponent, 4));
            scenario.OpponentCombatModifiers.UndeadAttackBonus = 6;
            scenario.OpponentCombatModifiers.FriendlyMinionDeathsThisGame = 4;
            return scenario;
        }

        private static TestScenarioDefinition CreateSpellFlow()
        {
            var scenario = BaseScenario(SpellFlow, 7, 91002);
            scenario.PlayerBoard.Add(Minion("player-spell-target", "Spell Target", "DVT_SPELL_TARGET", 5, 18, Tribe.None, BoardSide.Player, 4, Keyword.Taunt));
            scenario.OpponentBoard.Add(Minion("opponent-spell-caster", "Combat Spell Proxy", "DVT_COMBAT_SPELL_PROXY", 7, 7, Tribe.Naga, BoardSide.Opponent, 4));
            scenario.OpponentBoard.Add(Minion("opponent-spell-payoff", "Spell Payoff Body", "DVT_SPELL_PAYOFF", 9, 9, Tribe.Elemental, BoardSide.Opponent, 5));
            scenario.OpponentCombatModifiers.SpellPower = 4;
            scenario.OpponentCombatModifiers.SpellsCastThisGame = 9;
            scenario.OpponentCombatModifiers.TavernSpellBonusAttack = 3;
            scenario.OpponentCombatModifiers.TavernSpellBonusHealth = 2;
            scenario.PlayerCombatModifiers.SpellPower = 2;
            scenario.PlayerCombatModifiers.SpellsCastThisGame = 5;
            scenario.Tavern.TavernSpellBonusAttack = 3;
            scenario.Tavern.TavernSpellBonusHealth = 2;
            return scenario;
        }

        private static TestScenarioDefinition CreateHistoricalStats()
        {
            var scenario = BaseScenario(HistoricalStats, 9, 91003);
            scenario.PlayerBoard.Add(Minion("player-eternal", "Eternal Knight", "BG25_008", 16, 8, Tribe.Undead, BoardSide.Player, 2));
            scenario.PlayerBoard.Add(Minion("player-automaton", "Ancestral Automaton", "BG_TTN_401", 12, 10, Tribe.Mech, BoardSide.Player, 2));
            scenario.OpponentBoard.Add(Minion("opponent-eternal-history", "Eternal Knight", "BG25_008", 4, 1, Tribe.Undead, BoardSide.Opponent, 2));
            scenario.OpponentBoard.Add(Minion("opponent-automaton-history", "Ancestral Automaton", "BG_TTN_401", 3, 4, Tribe.Mech, BoardSide.Opponent, 2));
            scenario.OpponentBoard.Add(Minion("opponent-timewarped-mrrrglr", "Timewarped Mrrrglr", "BG34_Giant_321", 5, 5, Tribe.Murloc, BoardSide.Opponent, 5));
            scenario.OpponentHand.Add(Minion("opponent-history-hand-a", "History Hand Body", "DVT_HISTORY_HAND_A", 8, 8, Tribe.Murloc, BoardSide.Opponent, 4));
            scenario.PlayerCombatModifiers.EternalKnightDeaths = 3;
            scenario.PlayerCombatModifiers.AstralAutomatonSummons = 4;
            scenario.OpponentCombatModifiers.EternalKnightDeaths = 4;
            scenario.OpponentCombatModifiers.AstralAutomatonSummons = 5;
            return scenario;
        }

        private static TestScenarioDefinition CreateEconomyToPower()
        {
            var scenario = BaseScenario(EconomyToPower, 10, 91004);
            scenario.Tavern.Gold = 10;
            scenario.Tavern.MaxGold = 10;
            scenario.Tavern.NextTurnBonusGold = 3;
            scenario.Tavern.FreeRefreshes = 2;
            scenario.Tavern.NextTavernSpellCostReduction = 1;
            scenario.PlayerBoard.Add(Minion("player-resource-board", "Resource Board Payoff", "DVT_RESOURCE_BOARD", 10, 10, Tribe.Pirate, BoardSide.Player, 5));
            scenario.PlayerBoard.Add(Minion("player-quest-payoff", "Quest Payoff Body", "DVT_QUEST_PAYOFF", 8, 12, Tribe.Dragon, BoardSide.Player, 5));
            scenario.OpponentBoard.Add(Minion("opponent-pressure", "Tempo Check", "DVT_TEMPO_CHECK", 13, 13, Tribe.Demon, BoardSide.Opponent, 5, Keyword.Taunt));
            scenario.Hand.Add(Minion("player-hand-resource", "Banked Resource Minion", "DVT_BANKED_RESOURCE", 6, 6, Tribe.Elemental, BoardSide.Player, 4));
            scenario.Shop.Add(TavernSpell("shop-tavern-spell", "Stat Tavern Spell", "DVT_TAVERN_SPELL", 2));
            scenario.PlayerCombatModifiers.TavernSpellBonusAttack = 4;
            scenario.PlayerCombatModifiers.TavernSpellBonusHealth = 4;
            scenario.PlayerCombatModifiers.BloodGemAttackBonus = 2;
            scenario.PlayerCombatModifiers.BloodGemHealthBonus = 2;
            return scenario;
        }

        private static TestScenarioDefinition CreateFullNextTurnFlow()
        {
            var scenario = BaseScenario(FullNextTurnFlow, 6, 91005);
            scenario.Phase = MatchPhase.Tavern;
            scenario.Tavern.Gold = 6;
            scenario.Tavern.MaxGold = 6;
            scenario.Tavern.NextTurnBonusGold = 1;
            scenario.PlayerBoard.Add(Minion("player-flow-attacker", "Flow Attacker", "DVT_FLOW_ATTACKER", 6, 5, Tribe.None, BoardSide.Player, 3));
            scenario.OpponentBoard.Add(Minion("opponent-flow-defender", "Flow Defender", "DVT_FLOW_DEFENDER", 4, 6, Tribe.None, BoardSide.Opponent, 3));
            scenario.OpponentHand.Add(Minion("opponent-flow-hand", "Flow Hand Backup", "DVT_FLOW_HAND", 3, 3, Tribe.Murloc, BoardSide.Opponent, 2));
            return scenario;
        }

        private static TestScenarioDefinition BaseScenario(string name, int round, int seed)
        {
            return new TestScenarioDefinition
            {
                Name = name,
                SavedAtRound = round,
                Seed = seed,
                Phase = MatchPhase.Tavern,
                Player = new PlayerScenarioState
                {
                    HeroId = "DVT_PLAYER",
                    Health = 30,
                    Armor = 0
                },
                Opponent = new OpponentScenarioState
                {
                    Name = "Design Validation Opponent",
                    HeroId = "DVT_OPPONENT",
                    Health = 30,
                    Armor = 0,
                    TavernTier = 6,
                    Editable = true
                },
                Tavern = new ScenarioTavernState
                {
                    Tier = 6,
                    Gold = 8,
                    MaxGold = 8,
                    UpgradeCost = 0
                }
            };
        }

        private static ScenarioCardState Minion(string instanceId, string name, string cardId, int attack, int health, Tribe tribe, BoardSide owner, int tier, params Keyword[] keywords)
        {
            return new ScenarioCardState
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = tier,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(keywords ?? new Keyword[0]),
                OfficialKeywords = new List<Keyword>(keywords ?? new Keyword[0]),
                Owner = owner,
                CanAttack = true,
                PoolSource = PoolSource.Debug,
                OriginPoolSource = PoolSource.Debug
            };
        }

        private static ScenarioCardState TavernSpell(string instanceId, string name, string cardId, int cost)
        {
            return new ScenarioCardState
            {
                CardKind = CardKind.TavernSpell,
                InstanceId = instanceId,
                DefinitionId = cardId,
                CardId = cardId,
                Name = name,
                Cost = cost,
                TavernTier = 3,
                Tribes = new List<Tribe> { Tribe.None },
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Debug,
                OriginPoolSource = PoolSource.Debug
            };
        }

        private static TestScenarioDefinition Clone(TestScenarioDefinition scenario)
        {
            return TestScenarioMapper.Clone(scenario);
        }
    }
}
