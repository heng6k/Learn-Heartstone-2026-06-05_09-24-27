using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Domain.Engine
{
    public static class DarkmoonPrizeEngine
    {
        public static int PrizeTierForDarkmoonFaireRound(int round)
        {
            return ClampPrizeTier(Math.Max(1, round) / 4);
        }

        public static int PrizeTierForUpPrizingRound(int round)
        {
            return ClampPrizeTier(1 + ((Math.Max(1, round) - 1) / 3));
        }

        public static IReadOnlyList<DarkmoonPrizeDefinition> SelectOfferableDefinitions(DarkmoonPrizeCatalog catalog, int tier)
        {
            return catalog == null
                ? new List<DarkmoonPrizeDefinition>()
                : catalog.GetByTier(ClampPrizeTier(tier));
        }

        public static MinionInstance CreatePrizeCard(DarkmoonPrizeDefinition definition, string suffix)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var tags = new List<string>
            {
                "generated_spell",
                "darkmoon_prize",
                "darkmoon_prize_tier_" + definition.Tier
            };

            foreach (var tag in definition.Tags ?? new List<string>())
            {
                if (!tags.Contains(tag))
                {
                    tags.Add(tag);
                }
            }

            if (definition.ImplementationStatus == DarkmoonPrizeImplementationStatus.Implemented)
            {
                tags.Add("implemented_darkmoon_prize");
            }
            else
            {
                tags.Add("darkmoon_prize_proxy");
            }

            return new MinionInstance
            {
                CardKind = CardKind.Spell,
                InstanceId = "player-darkmoon-prize-" + definition.CardId.ToLowerInvariant() + "-" + suffix,
                DefinitionId = "darkmoon-prize-" + definition.CardId.ToLowerInvariant(),
                CardId = definition.CardId,
                Name = definition.Name,
                Cost = 0,
                TavernTier = definition.Tier,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = definition.Keywords == null ? new List<Keyword>() : new List<Keyword>(definition.Keywords),
                Text = definition.Text,
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                PoolCopiesHeld = 0,
                Tags = tags
            };
        }

        private static int ClampPrizeTier(int tier)
        {
            return Math.Max(1, Math.Min(4, tier));
        }
    }
}
