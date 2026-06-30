using System;
using System.Collections.Generic;

namespace LearnHearthstone.Domain.Models
{
    public static class SpellBehaviorTemplate
    {
        public static SpellCardTemplate ResolveCardTemplate(TavernSpellDefinition definition)
        {
            if (definition == null)
            {
                return SpellCardTemplate.Unknown;
            }

            if (definition.CardTemplate != SpellCardTemplate.Auto)
            {
                return definition.CardTemplate;
            }

            if (HasToken(definition.SpecialType, "spellcraft") ||
                HasToken(definition.Keywords, "spellcraft") ||
                HasToken(definition.Tags, "spellcraft"))
            {
                return SpellCardTemplate.Spellcraft;
            }

            if (HasToken(definition.Tags, "blood_gem") || HasToken(definition.Keywords, "blood gem"))
            {
                return SpellCardTemplate.BloodGem;
            }

            if (HasToken(definition.Tags, "darkmoon_prize") || HasToken(definition.Category, "darkmoon"))
            {
                return SpellCardTemplate.DarkmoonPrize;
            }

            if (HasToken(definition.Tags, "generated_spell"))
            {
                return SpellCardTemplate.Generated;
            }

            if (HasToken(definition.Category, "tavernspell") ||
                HasToken(definition.Type, "tavernspell") ||
                HasToken(definition.Tags, "tavern_spell"))
            {
                return SpellCardTemplate.TavernSpell;
            }

            if (HasToken(definition.Type, "spell") || HasToken(definition.Category, "spell"))
            {
                return SpellCardTemplate.Spell;
            }

            return SpellCardTemplate.Unknown;
        }

        public static SpellTargetTemplate ResolveTargetTemplate(TavernSpellDefinition definition)
        {
            if (definition == null)
            {
                return SpellTargetTemplate.Unknown;
            }

            if (definition.TargetTemplate != SpellTargetTemplate.Auto)
            {
                return definition.TargetTemplate;
            }

            if (HasToken(definition.Tags, "targeted_spell") ||
                HasToken(definition.Tags, "targeted_attack_buff") ||
                HasToken(definition.Tags, "targeted_health_buff") ||
                HasToken(definition.Tags, "targeted_stat_buff"))
            {
                return SpellTargetTemplate.FriendlyMinion;
            }

            if (HasToken(definition.Tags, "shop_spell") ||
                HasToken(definition.Tags, "shop_buff") ||
                HasToken(definition.Tags, "shop_steal") ||
                HasToken(definition.Tags, "steal_spell"))
            {
                return SpellTargetTemplate.TavernShop;
            }

            if (HasToken(definition.Tags, "discover_spell") || HasToken(definition.EffectIds, "discover"))
            {
                return SpellTargetTemplate.Discover;
            }

            if (HasToken(definition.Tags, "card_generator") || HasToken(definition.EffectIds, "generate"))
            {
                return SpellTargetTemplate.Hand;
            }

            return SpellTargetTemplate.None;
        }

        public static SpellEffectTemplate ResolveEffectTemplate(TavernSpellDefinition definition)
        {
            if (definition == null)
            {
                return SpellEffectTemplate.Unknown;
            }

            if (definition.EffectTemplate != SpellEffectTemplate.Auto)
            {
                return definition.EffectTemplate;
            }

            if (HasToken(definition.Tags, "buff_spell") ||
                HasToken(definition.Tags, "attack_buff") ||
                HasToken(definition.Tags, "health_buff") ||
                HasToken(definition.Tags, "targeted_stat_buff") ||
                HasToken(definition.EffectIds, "buff"))
            {
                return SpellEffectTemplate.BuffStats;
            }

            if (HasToken(definition.Tags, "keyword_grant") ||
                HasToken(definition.Tags, "taunt_grant") ||
                HasToken(definition.EffectIds, "keyword"))
            {
                return SpellEffectTemplate.GrantKeyword;
            }

            if (HasToken(definition.Tags, "economy_spell") ||
                HasToken(definition.Tags, "gain_gold") ||
                HasToken(definition.Tags, "max_gold_growth") ||
                HasToken(definition.Tags, "health_cost"))
            {
                return SpellEffectTemplate.Economy;
            }

            if (HasToken(definition.Tags, "discover_spell") || HasToken(definition.EffectIds, "discover"))
            {
                return SpellEffectTemplate.Discover;
            }

            if (HasToken(definition.Tags, "card_generator") || HasToken(definition.EffectIds, "generate"))
            {
                return SpellEffectTemplate.GenerateCard;
            }

            if (HasToken(definition.Tags, "refresh_spell") || HasToken(definition.Tags, "free_refresh"))
            {
                return SpellEffectTemplate.Refresh;
            }

            if (HasToken(definition.Tags, "shop_spell") ||
                HasToken(definition.Tags, "shop_buff") ||
                HasToken(definition.Tags, "shop_steal") ||
                HasToken(definition.Tags, "steal_spell"))
            {
                return SpellEffectTemplate.ShopInteraction;
            }

            if (HasToken(definition.EffectIds, "transform"))
            {
                return SpellEffectTemplate.Transform;
            }

            if (HasToken(definition.EffectIds, "summon"))
            {
                return SpellEffectTemplate.Summon;
            }

            if (HasToken(definition.EffectIds, "copy") || HasToken(definition.EffectIds, "clone"))
            {
                return SpellEffectTemplate.Copy;
            }

            return SpellEffectTemplate.Unknown;
        }

        private static bool HasToken(IEnumerable<string> values, string token)
        {
            if (values == null || string.IsNullOrEmpty(token))
            {
                return false;
            }

            foreach (var value in values)
            {
                if (HasToken(value, token))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasToken(string value, string token)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public static class TrinketBehaviorTemplate
    {
        public static TrinketTriggerTemplate ResolveTriggerTemplate(TrinketDefinition definition)
        {
            if (definition == null)
            {
                return TrinketTriggerTemplate.Unknown;
            }

            if (definition.TriggerTemplate != TrinketTriggerTemplate.Auto)
            {
                return definition.TriggerTemplate;
            }

            if (HasToken(definition.EffectFamily, "turn_start") || HasToken(definition.Tags, "turn_start"))
            {
                return TrinketTriggerTemplate.TurnStart;
            }

            if (HasToken(definition.EffectFamily, "turn_end") || HasToken(definition.Tags, "turn_end"))
            {
                return TrinketTriggerTemplate.TurnEnd;
            }

            if (HasToken(definition.EffectFamily, "combat_start") ||
                HasToken(definition.EffectFamily, "start_of_combat") ||
                HasToken(definition.Mechanics, "startofcombat"))
            {
                return TrinketTriggerTemplate.StartOfCombat;
            }

            if (HasToken(definition.EffectFamily, "avenge") || HasToken(definition.Mechanics, "avenge"))
            {
                return TrinketTriggerTemplate.Avenge;
            }

            if (HasToken(definition.EffectFamily, "refresh") || HasToken(definition.EffectFamily, "shop_refresh"))
            {
                return TrinketTriggerTemplate.ShopRefresh;
            }

            if (HasToken(definition.EffectFamily, "buy_trigger") || HasToken(definition.Tags, "buy_trigger"))
            {
                return TrinketTriggerTemplate.CardBought;
            }

            if (HasToken(definition.EffectFamily, "sell") || HasToken(definition.Tags, "sell_trigger"))
            {
                return TrinketTriggerTemplate.CardSold;
            }

            if (HasToken(definition.EffectFamily, "tavern_spell") ||
                HasToken(definition.EffectFamily, "spell_cast") ||
                HasToken(definition.Tags, "tavern_spell"))
            {
                return TrinketTriggerTemplate.SpellCast;
            }

            if (HasToken(definition.EffectFamily, "spellcraft") || HasToken(definition.Tags, "spellcraft"))
            {
                return TrinketTriggerTemplate.SpellcraftCast;
            }

            if (HasToken(definition.EffectFamily, "on_equip") || HasToken(definition.Tags, "on_equip"))
            {
                return TrinketTriggerTemplate.OnEquip;
            }

            if (HasToken(definition.EffectFamily, "passive") || HasToken(definition.Tags, "passive"))
            {
                return TrinketTriggerTemplate.Passive;
            }

            return TrinketTriggerTemplate.Unknown;
        }

        public static TrinketEffectTemplate ResolveEffectTemplate(TrinketDefinition definition)
        {
            if (definition == null)
            {
                return TrinketEffectTemplate.Unknown;
            }

            if (definition.EffectTemplate != TrinketEffectTemplate.Auto)
            {
                return definition.EffectTemplate;
            }

            if (HasToken(definition.EffectFamily, "economy") ||
                HasToken(definition.EffectFamily, "gold") ||
                HasToken(definition.Tags, "economy"))
            {
                return TrinketEffectTemplate.Economy;
            }

            if (HasToken(definition.EffectFamily, "buff") ||
                HasToken(definition.EffectFamily, "stats") ||
                HasToken(definition.Tags, "buff"))
            {
                return TrinketEffectTemplate.BuffStats;
            }

            if (HasToken(definition.EffectFamily, "keyword") || HasToken(definition.Tags, "keyword"))
            {
                return TrinketEffectTemplate.GrantKeyword;
            }

            if (HasToken(definition.EffectFamily, "discover") || HasToken(definition.Tags, "discover"))
            {
                return TrinketEffectTemplate.Discover;
            }

            if (HasToken(definition.EffectFamily, "generate") ||
                HasToken(definition.EffectFamily, "card") ||
                HasToken(definition.Tags, "generator"))
            {
                return TrinketEffectTemplate.GenerateCard;
            }

            if (HasToken(definition.EffectFamily, "summon") || HasToken(definition.Tags, "summon"))
            {
                return TrinketEffectTemplate.Summon;
            }

            if (HasToken(definition.EffectFamily, "shop") || HasToken(definition.EffectFamily, "refresh"))
            {
                return TrinketEffectTemplate.ShopModifier;
            }

            if (HasToken(definition.EffectFamily, "combat") ||
                HasToken(definition.EffectFamily, "avenge") ||
                HasToken(definition.EffectFamily, "start_of_combat"))
            {
                return TrinketEffectTemplate.CombatModifier;
            }

            if (HasToken(definition.EffectFamily, "deathrattle") || HasToken(definition.Mechanics, "deathrattle"))
            {
                return TrinketEffectTemplate.Deathrattle;
            }

            if (HasToken(definition.EffectFamily, "tavern_spell") || HasToken(definition.EffectFamily, "spellcraft"))
            {
                return TrinketEffectTemplate.SpellSynergy;
            }

            if (HasToken(definition.EffectFamily, "tribe") || HasToken(definition.AssociatedRaces, ""))
            {
                return TrinketEffectTemplate.TribeSynergy;
            }

            if (HasToken(definition.EffectFamily, "pool"))
            {
                return TrinketEffectTemplate.PoolModifier;
            }

            return TrinketEffectTemplate.Unknown;
        }

        private static bool HasToken(IEnumerable<string> values, string token)
        {
            if (values == null)
            {
                return false;
            }

            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(token))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return true;
                    }
                }
                else if (HasToken(value, token))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasToken(string value, string token)
        {
            return !string.IsNullOrEmpty(value) &&
                !string.IsNullOrEmpty(token) &&
                value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
