using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class HeroCombatDamageService
    {
        public static HeroDamageResolution ResolveAndApply(
            CombatOutput combat,
            MatchState state,
            int round,
            HeroDamageCapPolicy capPolicy,
            bool isTopFour)
        {
            var result = new HeroDamageResolution
            {
                Winner = combat?.Winner ?? CombatWinner.Draw,
                CapPolicy = capPolicy,
                Round = Math.Max(1, round)
            };
            if (combat == null || state == null || combat.SafetyStopped || combat.Winner == CombatWinner.Draw)
            {
                return result;
            }

            var playerWon = combat.Winner == CombatWinner.Player;
            var survivors = playerWon ? combat.FinalPlayerBoard : combat.FinalOpponentBoard;
            result.DamagedSide = playerWon ? BoardSide.Opponent : BoardSide.Player;
            result.TavernTierDamage = Math.Max(1, playerWon
                ? combat.FinalPlayerTavern?.Tier ?? state.Player?.Tavern?.Tier ?? 1
                : combat.FinalOpponentTavern?.Tier ?? state.Opponent?.TavernTier ?? 1);
            result.SurvivingMinionTierDamage = StatMath.SaturatingSum(
                (survivors ?? new List<MinionInstance>())
                    .Where(minion => minion != null && minion.Health > 0)
                    .Select(minion => Math.Max(1, minion.TavernTier)),
                0,
                int.MaxValue);
            result.RawDamage = StatMath.SaturatingAdd(
                result.TavernTierDamage,
                result.SurvivingMinionTierDamage,
                0,
                int.MaxValue);
            result.CapRemoved = isTopFour ||
                capPolicy == HeroDamageCapPolicy.TrainingRound12Approximation && result.Round >= 12;
            result.UsesApproximation = !isTopFour &&
                capPolicy == HeroDamageCapPolicy.TrainingRound12Approximation &&
                result.Round >= 12;
            result.DamageCap = result.CapRemoved ? 0 : DamageCapForRound(result.Round);
            result.AppliedDamage = result.CapRemoved
                ? result.RawDamage
                : Math.Min(result.RawDamage, result.DamageCap);

            if (playerWon)
            {
                ApplyToOpponent(state.Opponent, result);
            }
            else
            {
                ApplyToPlayer(state.Player, result);
            }

            result.Applied = result.AppliedDamage > 0;
            return result;
        }

        private static int DamageCapForRound(int round)
        {
            if (round <= 3)
            {
                return 5;
            }

            return round <= 7 ? 10 : 15;
        }

        private static void ApplyToPlayer(LocalPlayerState player, HeroDamageResolution result)
        {
            if (player == null)
            {
                return;
            }

            result.ArmorBefore = Math.Max(0, player.Armor);
            result.HealthBefore = Math.Max(0, player.Health);
            Apply(result);
            player.Armor = result.ArmorAfter;
            player.Health = result.HealthAfter;
        }

        private static void ApplyToOpponent(LocalOpponentState opponent, HeroDamageResolution result)
        {
            if (opponent == null)
            {
                return;
            }

            result.ArmorBefore = Math.Max(0, opponent.Armor);
            result.HealthBefore = Math.Max(0, opponent.Health);
            Apply(result);
            opponent.Armor = result.ArmorAfter;
            opponent.Health = result.HealthAfter;
        }

        private static void Apply(HeroDamageResolution result)
        {
            result.ArmorAbsorbed = Math.Min(result.ArmorBefore, result.AppliedDamage);
            result.ArmorAfter = result.ArmorBefore - result.ArmorAbsorbed;
            result.HealthDamage = Math.Min(
                result.HealthBefore,
                Math.Max(0, result.AppliedDamage - result.ArmorAbsorbed));
            result.HealthAfter = result.HealthBefore - result.HealthDamage;
        }
    }
}
