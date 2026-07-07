using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class CombatResultExplainer
    {
        public static CombatExplanation Analyze(CombatOutput output, TestScenarioDefinition beforeCombat = null)
        {
            var explanation = new CombatExplanation();
            if (output == null)
            {
                explanation.Summary = "No combat result is available.";
                return explanation;
            }

            explanation.Winner = output.Winner;
            explanation.Summary = BuildSummary(output);
            AddMainFactors(explanation, output);
            AddVariableSignals(explanation, beforeCombat);
            AddTriggerSignals(explanation, output);
            AddContributors(explanation, output);
            AddSwingCandidates(explanation, output);
            return explanation;
        }

        private static string BuildSummary(CombatOutput output)
        {
            var winner = output.Winner == CombatWinner.Draw ? "draw" : output.Winner.ToString().ToLowerInvariant() + " win";
            return winner + " after " + Math.Max(0, output.Steps) + " combat step(s).";
        }

        private static void AddMainFactors(CombatExplanation explanation, CombatOutput output)
        {
            var damageEvents = CountFrames(output, CombatEventType.DamageResolved);
            var deathEvents = CountFrames(output, CombatEventType.DeathQueued);
            var summonEvents = CountFrames(output, CombatEventType.MinionSummoned);
            AddItem(explanation.MainFactors, "Damage pressure", damageEvents + " damage resolution(s) shaped the fight.", damageEvents);
            AddItem(explanation.MainFactors, "Deaths", deathEvents + " minion death event(s) were queued.", deathEvents);
            AddItem(explanation.MainFactors, "Summons", summonEvents + " summon event(s) changed board size.", summonEvents);

            if (output.SafetyStopped)
            {
                AddItem(explanation.MainFactors, "Safety stop", "Combat stopped at the configured safety limit.", 1, LogSeverity.Warning);
            }
        }

        private static void AddVariableSignals(CombatExplanation explanation, TestScenarioDefinition scenario)
        {
            if (scenario == null)
            {
                return;
            }

            AddSideVariables(explanation, BoardSide.Player, scenario.PlayerCombatModifiers);
            AddSideVariables(explanation, BoardSide.Opponent, scenario.OpponentCombatModifiers);
        }

        private static void AddSideVariables(CombatExplanation explanation, BoardSide side, SideCombatModifierState modifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            AddVariable(explanation, side, "Spell power", modifiers.SpellPower, "combat spell-like damage reads this side value.");
            AddVariable(explanation, side, "Spells cast", modifiers.SpellsCastThisGame, "spell-count effects can read this side history.");
            AddVariable(explanation, side, "Tavern spell stats", modifiers.TavernSpellBonusAttack + modifiers.TavernSpellBonusHealth, "configured tavern spell stat growth is present.");
            AddVariable(explanation, side, "Blood gem quality", modifiers.BloodGemAttackBonus + modifiers.BloodGemHealthBonus, "blood gem stat quality is configured.");
            AddVariable(explanation, side, "Undead attack", modifiers.UndeadAttackBonus, "undead minions can receive the side attack history.");
            AddVariable(explanation, side, "Eternal Knight deaths", modifiers.EternalKnightDeaths, "Eternal Knights can receive history stats.");
            AddVariable(explanation, side, "Astral Automaton summons", modifiers.AstralAutomatonSummons, "Astral Automatons can receive history stats.");
            AddVariable(explanation, side, "Friendly deaths", modifiers.FriendlyMinionDeathsThisGame, "death-count effects can read this side history.");
        }

        private static void AddVariable(CombatExplanation explanation, BoardSide side, string title, int value, string detail)
        {
            if (value <= 0)
            {
                return;
            }

            explanation.VariableSignals.Add(new CombatExplanationItem
            {
                Title = title,
                Detail = side + " value " + value + ": " + detail,
                Count = value,
                Side = side,
                Severity = LogSeverity.Good
            });
        }

        private static void AddTriggerSignals(CombatExplanation explanation, CombatOutput output)
        {
            AddTrigger(explanation, output, CombatEventType.DeathrattleResolved, "Deathrattle");
            AddTrigger(explanation, output, CombatEventType.AvengeProgressed, "Avenge");
            AddTrigger(explanation, output, CombatEventType.AvengeCounterUpdated, "Avenge counter");
            AddTrigger(explanation, output, CombatEventType.RallyResolved, "Rally");
            AddTrigger(explanation, output, CombatEventType.RebornResolved, "Reborn");
            AddTrigger(explanation, output, CombatEventType.CombatSpellCast, "Combat spell");
            AddRewardTrigger(explanation, output.PlayerRewards, BoardSide.Player);
            AddRewardTrigger(explanation, output.OpponentRewards, BoardSide.Opponent);
        }

        private static void AddTrigger(CombatExplanation explanation, CombatOutput output, CombatEventType type, string title)
        {
            var count = CountFrames(output, type);
            if (count <= 0)
            {
                return;
            }

            explanation.TriggerSignals.Add(new CombatExplanationItem
            {
                Title = title,
                Detail = title + " appeared " + count + " time(s) in the replay.",
                Count = count,
                Severity = LogSeverity.Good
            });
        }

        private static void AddRewardTrigger(CombatExplanation explanation, IEnumerable<CombatReward> rewards, BoardSide side)
        {
            var grouped = (rewards ?? Enumerable.Empty<CombatReward>())
                .GroupBy(reward => reward.Type)
                .OrderByDescending(group => group.Count())
                .Take(3);
            foreach (var group in grouped)
            {
                explanation.TriggerSignals.Add(new CombatExplanationItem
                {
                    Title = group.Key.ToString(),
                    Detail = side + " queued " + group.Count() + " combat reward(s).",
                    Count = group.Count(),
                    Side = side,
                    Severity = LogSeverity.Good
                });
            }
        }

        private static void AddContributors(CombatExplanation explanation, CombatOutput output)
        {
            var frames = output.Replay?.Frames ?? new List<CombatFrame>();
            var contributors = frames
                .Where(frame => !string.IsNullOrEmpty(frame.ActorId))
                .GroupBy(frame => frame.ActorId, StringComparer.OrdinalIgnoreCase)
                .Select(group => new CombatContribution
                {
                    EntityId = group.Key,
                    Side = group.Select(frame => frame.ActorSide).FirstOrDefault(),
                    DamageEvents = group.Count(frame => frame.EventType == CombatEventType.DamageResolved || frame.ActualDamageCount > 0),
                    TriggerEvents = group.Count(frame => IsTriggerFrame(frame.EventType)),
                    Summons = group.Sum(frame => frame.SummonedEntityIds?.Count ?? 0)
                })
                .OrderByDescending(contribution => contribution.DamageEvents + contribution.TriggerEvents + contribution.Summons)
                .ThenBy(contribution => contribution.EntityId, StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            foreach (var contribution in contributors)
            {
                contribution.Note = contribution.DamageEvents + " damage, " + contribution.TriggerEvents + " trigger, " + contribution.Summons + " summon signal(s).";
                explanation.TopContributors.Add(contribution);
            }
        }

        private static void AddSwingCandidates(CombatExplanation explanation, CombatOutput output)
        {
            foreach (var contributor in explanation.TopContributors.Take(3))
            {
                explanation.KeySwingCandidates.Add(contributor.EntityId + " is a likely swing candidate: " + contributor.Note);
            }

            if (explanation.KeySwingCandidates.Count == 0)
            {
                var survivor = (output.Winner == CombatWinner.Opponent ? output.FinalOpponentBoard : output.FinalPlayerBoard)
                    .Where(minion => minion != null)
                    .OrderByDescending(minion => minion.Attack + minion.Health)
                    .FirstOrDefault();
                if (survivor != null)
                {
                    explanation.KeySwingCandidates.Add(survivor.InstanceId + " survived with the largest visible stats.");
                }
            }
        }

        private static bool IsTriggerFrame(CombatEventType type)
        {
            switch (type)
            {
                case CombatEventType.DeathrattleResolved:
                case CombatEventType.AvengeProgressed:
                case CombatEventType.AvengeCounterUpdated:
                case CombatEventType.RallyResolved:
                case CombatEventType.RebornResolved:
                case CombatEventType.CombatSpellCast:
                case CombatEventType.TrinketTriggered:
                    return true;
                default:
                    return false;
            }
        }

        private static int CountFrames(CombatOutput output, CombatEventType type)
        {
            return output?.Replay?.Frames?.Count(frame => frame.EventType == type) ?? 0;
        }

        private static void AddItem(List<CombatExplanationItem> items, string title, string detail, int count, LogSeverity severity = LogSeverity.Normal)
        {
            if (count <= 0 && severity != LogSeverity.Warning)
            {
                return;
            }

            items.Add(new CombatExplanationItem
            {
                Title = title,
                Detail = detail,
                Count = count,
                Severity = severity
            });
        }
    }
}
