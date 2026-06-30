using System;
using System.Collections.Generic;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Data
{
    public static class TimewarpedTavernCatalogLoader
    {
        private const string ResourcePath = "Data/timewarpedTavernCards";

        public static TimewarpedTavernCatalog LoadFromResources()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing Resources/" + ResourcePath + ".json");
            }

            return LoadFromJson(asset.text);
        }

        public static TimewarpedTavernCatalog LoadFromJson(string json)
        {
            var payload = JsonUtility.FromJson<RawPayload>(json);
            if (payload == null || payload.cards == null)
            {
                throw new InvalidOperationException("Invalid Timewarped Tavern card payload.");
            }

            var definitions = new List<TimewarpedTavernCardDefinition>();
            foreach (var raw in payload.cards)
            {
                definitions.Add(ToDefinition(raw));
            }

            AddSupplementalNonMinionDefinitions(definitions);
            return new TimewarpedTavernCatalog(definitions);
        }

        private static TimewarpedTavernCardDefinition ToDefinition(RawCard raw)
        {
            var definition = new TimewarpedTavernCardDefinition
            {
                CardId = raw.cardId,
                DbfId = raw.dbfId,
                Name = raw.name,
                ZhName = raw.zhName,
                CardKind = MapCardKind(raw.cardKind),
                TimewarpKind = MapTimewarpKind(raw.timewarpKind),
                Cost = raw.cost,
                TechLevel = raw.techLevel,
                Attack = raw.attack,
                Health = raw.health,
                Tribes = MapTribes(raw.tribes),
                Keywords = MapKeywords(raw.keywords),
                Text = raw.text,
                ZhText = raw.zhText,
                ImagePath = raw.imagePath,
                EffectIds = raw.effectIds == null ? new List<string>() : new List<string>(raw.effectIds),
                Tags = raw.tags == null ? new List<string>() : new List<string>(raw.tags),
                PoolStatus = raw.poolStatus,
                PurchaseBehavior = MapPurchaseBehavior(raw.purchaseBehavior),
                PrimaryMechanicTemplate = MapMechanicTemplate(raw.mechanicTemplate),
                MechanicTemplates = MapMechanicTemplates(raw.mechanicTemplates),
                GoldenCardId = raw.goldenCardId,
                GoldenDbfId = raw.goldenDbfId
            };
            AddKeywordBodiesFromText(definition);
            CompleteMechanicTemplates(definition);
            return definition;
        }

        private static void AddKeywordBodiesFromText(TimewarpedTavernCardDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            var text = ((definition.Text ?? string.Empty) + " " + (definition.ZhText ?? string.Empty)).Trim();
            if ((text.IndexOf("Also damages adjacent minions", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 string.Equals(definition.CardId, "BG34_Giant_680", StringComparison.OrdinalIgnoreCase)) &&
                !definition.Keywords.Contains(Keyword.Cleave))
            {
                definition.Keywords.Add(Keyword.Cleave);
            }
        }

        private static void AddSupplementalNonMinionDefinitions(List<TimewarpedTavernCardDefinition> definitions)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
            {
                if (!string.IsNullOrEmpty(definition.CardId))
                {
                    existing.Add(definition.CardId);
                }
            }

            foreach (var definition in SupplementalNonMinionDefinitions())
            {
                if (string.IsNullOrEmpty(definition.CardId) || existing.Contains(definition.CardId))
                {
                    continue;
                }

                CompleteMechanicTemplates(definition);
                definitions.Add(definition);
                existing.Add(definition.CardId);
            }
        }

        private static void CompleteMechanicTemplates(TimewarpedTavernCardDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            var templates = TimewarpedCardBehavior.ResolveMechanicTemplates(definition);
            definition.MechanicTemplates = templates;
            definition.PrimaryMechanicTemplate = templates.Count == 0
                ? TimewarpedMechanicTemplate.Unknown
                : templates[0];
        }

        private static IEnumerable<TimewarpedTavernCardDefinition> SupplementalNonMinionDefinitions()
        {
            yield return ExitCard();

            yield return ImplementedTavernSpell("BG34_Treasure_900", "Timewarped Evolving Tavern", 1, 3, TimewarpKind.Minor, "Get an Evolving Tavern. Repeat at the start of each turn.", "timewarp_repeat_evolving_tavern");
            yield return ImplementedTavernSpell("BG34_Treasure_902", "Timewarped Master Thief", 3, 5, TimewarpKind.Major, "Discover a minion from your warband last game. It keeps its stats and Bonus Keywords.", "timewarp_previous_warband_keep_stats");
            yield return ImplementedTavernSpell("BG34_Treasure_903", "Timewarped On the House", 2, 5, TimewarpKind.Major, "Discover 3 minions from your Tier.", "timewarp_discover_current_tier_3");
            yield return ImplementedTavernSpell("BG34_Treasure_905", "Timewarped Rat in a Cage", 1, 5, TimewarpKind.Major, "Give a minion +2/+2, then double its stats.", "timewarp_rat_in_a_cage");
            yield return ImplementedTavernSpell("BG34_Treasure_912", "Timewarped B.A.N.A.N.A.S.", 2, 5, TimewarpKind.Major, "Fill your hand with Tavern Dish Bananas. Your Tavern spells give an extra +3/+3 this game.", "timewarp_bananas");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_003", "Power of Al'Akir", 1, 3, TimewarpKind.Minor, "Casts When Bought. Make Swatting Insects your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedTavernSpell("BG34_Treasure_917", "Timewarped New Recruit", 1, 3, TimewarpKind.Minor, "Give minions in the Tavern +2/+2 this game. The Tavern always has 7 cards this game.", "timewarp_new_recruit");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_005", "Power of Elise", 1, 3, TimewarpKind.Minor, "Casts When Bought. Make Lead Explorer your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_006", "Power of George", 2, 3, TimewarpKind.Minor, "Casts When Bought. Make Boon of Light your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_008", "Power of Shudderwock", 1, 3, TimewarpKind.Minor, "Casts When Bought. Make Snicker-Snack your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_009", "Power of Teron", 1, 3, TimewarpKind.Minor, "Casts When Bought. Make Rapid Reanimation your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_010", "Power of the Lich King", 1, 3, TimewarpKind.Minor, "Casts When Bought. Make Reborn Rites your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_012", "Power of Zephrys", 1, 3, TimewarpKind.Minor, "Casts When Bought. Make Three Wishes your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_015", "Power of Guff", 2, 3, TimewarpKind.Minor, "Casts When Bought. Make Natural Balance your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedTavernSpell("BG34_Treasure_932", "Timewarped Strike Oil", 2, 5, TimewarpKind.Major, "Increase your maximum Gold by 4. Gain Gold.", "timewarp_strike_oil");
            yield return ImplementedTavernSpell("BG34_Treasure_933", "Timewarped Evolution", 1, 5, TimewarpKind.Major, "Choose a minion. Discover a Tier 6 minion to transform it into. Set its stats to 30/30.", "timewarp_evolution");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_Treasure_934", "Timewarped Armor Stash", 1, 5, TimewarpKind.Major, "Casts When Bought. Gain 10 Armor.", "timewarp_gain_armor");
            yield return ImplementedTavernSpell("BG34_Treasure_937", "Timewarped Corpse", 1, 5, TimewarpKind.Major, "Discover a Deathrattle minion from Tier 5 or higher. Give it Reborn.", "timewarp_discover_deathrattle_tier5_reborn");
            yield return ImplementedTavernSpell("BG34_Treasure_940", "Timewarped Chef's Choice", 1, 5, TimewarpKind.Major, "Choose a minion. Get a random minion of the same type from Tiers 4, 5, and 6.", "timewarp_chefs_choice");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_Treasure_300", "Timewarped Investment", 1, 0, TimewarpKind.None, "Casts When Bought. Gain 1 extra Chronum at your next Timewarp.", "timewarp_next_chronum");
            yield return ImplementedTavernSpell("BG34_Treasure_301", "Timewarped Beanstalk", 2, 3, TimewarpKind.Minor, "Discover a 1-Cost card from the Major Timewarp. Lock it in your hand for 1 turn.", "timewarp_beanstalk");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_016", "Power of Flurgl", 2, 3, TimewarpKind.Minor, "Casts When Bought. Make Gone Fishing your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_017", "Power of Sylvanas", 1, 3, TimewarpKind.Minor, "Casts When Bought. Make Reclaimed Souls your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_018", "Power of Tavish", 1, 0, TimewarpKind.None, "Casts When Bought. Make Lock and Load your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedTavernSpell("BG34_Treasure_302", "Timewarped Cloning Device", 2, 5, TimewarpKind.Major, "Choose a friendly minion. Summon an exact copy of it.", "timewarp_cloning_device");
            yield return ImplementedTavernSpell("BG34_Treasure_950", "Timewarped Special", 1, 5, TimewarpKind.Major, "Fill your hand with random Spellcraft spells.", "timewarp_special");
            yield return ImplementedTavernSpell("BG34_Treasure_951", "Timewarped Conch", 1, 5, TimewarpKind.Major, "Choose a friendly Murloc. Summon an exact copy of it.", "timewarp_conch");
            yield return ImplementedTavernSpell("BG34_Treasure_953", "Timewarped Revelation", 1, 5, TimewarpKind.Major, "Discover a 1-Cost and 2-Cost minion from the Minor Timewarp.", "timewarp_revelation");
            yield return ImplementedCastsWhenBoughtTavernSpell("BG34_HeroPowerSpell_022", "Power of Rakanishu", 1, 0, TimewarpKind.None, "Casts When Bought. Make Tavern Lighting your second Hero Power.", "timewarp_second_hero_power");
            yield return ImplementedTavernSpell("BG34_Treasure_919", "Timewarped Ritual", 2, 5, TimewarpKind.Major, "Discover two Tier 7 minions.", "timewarp_discover_tier7_2");
            yield return ImplementedTavernSpell("BG34_Treasure_955", "Timewarped Goldenizer", 2, 5, TimewarpKind.Major, "Choose a minion. Make it Golden.", "timewarp_goldenizer");
            yield return ImplementedTavernSpell("BG34_Treasure_606", "Timewarped Big Winner!", 2, 3, TimewarpKind.Minor, "Discover a Tier 3 Darkmoon Prize. Repeat at the start of every three turns.", "timewarp_big_winner");
            yield return ImplementedTavernSpell("BG34_Treasure_607", "Timewarped Trainee", 1, 3, TimewarpKind.Minor, "Get a random minion each from Tiers 1, 2, and 3.", "timewarp_trainee");
            yield return ImplementedTavernSpell("BG34_Treasure_608", "Timewarped Ring", 1, 3, TimewarpKind.Minor, "Get a Shiny Ring. Repeat at the start of each turn.", "timewarp_repeat_shiny_ring");
            yield return ImplementedTavernSpell("BG34_Treasure_609", "Timewarped Lasso", 1, 3, TimewarpKind.Minor, "Get an Enchanted Lasso. Repeat at the start of each turn.", "timewarp_repeat_enchanted_lasso");
            yield return ImplementedTavernSpell("BG34_Treasure_966", "Timewarped Thief", 1, 5, TimewarpKind.Major, "Discover a minion from your warband last game. Set its stats to 20/20.", "timewarp_previous_warband_20_20");
            yield return ImplementedTavernSpell("BG34_Treasure_620", "Timewarped Apples", 1, 3, TimewarpKind.Minor, "After the Tavern is Refreshed this game, cast Them Apples.", "timewarp_refresh_cast_them_apples");
            yield return ImplementedTavernSpell("BG34_Treasure_625", "Timewarped Secrecy", 3, 5, TimewarpKind.Major, "Discover a Golden Tier 7 minion.", "timewarp_secrecy");
        }

        private static TimewarpedTavernCardDefinition ExitCard()
        {
            return new TimewarpedTavernCardDefinition
            {
                CardId = "BG34_BlackMarket_Skip",
                Name = "Exit the Timewarped Tavern",
                CardKind = CardKind.Spell,
                TimewarpKind = TimewarpKind.None,
                Cost = 0,
                TechLevel = 0,
                Attack = 0,
                Health = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = "Exit the Timewarped Tavern. Save your remaining Chronum for your next Timewarp.",
                ImagePath = "CardImages/BG34_BlackMarket_Skip",
                EffectIds = new List<string> { "timewarp_exit" },
                Tags = new List<string> { "timewarped", "timewarp:non_minion", TimewarpedCardBehavior.ExitTag },
                PoolStatus = "utility",
                PurchaseBehavior = TimewarpedPurchaseBehavior.Exit
            };
        }

        private static TimewarpedTavernCardDefinition ImplementedCastsWhenBoughtTavernSpell(
            string cardId,
            string name,
            int cost,
            int techLevel,
            TimewarpKind kind,
            string text,
            string effectId)
        {
            var definition = BlockedTavernSpell(cardId, name, cost, techLevel, kind, text, true);
            definition.Tags.RemoveAll(tag => string.Equals(tag, TimewarpedCardBehavior.BlockedNonMinionSupportTag, StringComparison.OrdinalIgnoreCase));
            if (!definition.Tags.Exists(tag => string.Equals(tag, "timewarp:implemented", StringComparison.OrdinalIgnoreCase)))
            {
                definition.Tags.Add("timewarp:implemented");
            }

            if (!string.IsNullOrEmpty(effectId))
            {
                definition.EffectIds.Add(effectId);
            }

            definition.PoolStatus = "implemented_non_minion";
            definition.PurchaseBehavior = TimewarpedPurchaseBehavior.CastsWhenBought;
            return definition;
        }

        private static TimewarpedTavernCardDefinition ImplementedTavernSpell(
            string cardId,
            string name,
            int cost,
            int techLevel,
            TimewarpKind kind,
            string text,
            string effectId)
        {
            var definition = BlockedTavernSpell(cardId, name, cost, techLevel, kind, text);
            definition.Tags.RemoveAll(tag => string.Equals(tag, TimewarpedCardBehavior.BlockedNonMinionSupportTag, StringComparison.OrdinalIgnoreCase));
            if (!definition.Tags.Exists(tag => string.Equals(tag, "timewarp:implemented", StringComparison.OrdinalIgnoreCase)))
            {
                definition.Tags.Add("timewarp:implemented");
            }

            if (!string.IsNullOrEmpty(effectId))
            {
                definition.EffectIds.Add(effectId);
            }

            definition.PoolStatus = "implemented_non_minion";
            definition.PurchaseBehavior = TimewarpedPurchaseBehavior.EntersHand;
            return definition;
        }

        private static TimewarpedTavernCardDefinition BlockedTavernSpell(
            string cardId,
            string name,
            int cost,
            int techLevel,
            TimewarpKind kind,
            string text,
            bool castsWhenBought = false)
        {
            var tags = new List<string>
            {
                "timewarped",
                "timewarp:non_minion",
                TimewarpedCardBehavior.BlockedNonMinionSupportTag
            };

            if (kind == TimewarpKind.Minor)
            {
                tags.Add("timewarp:minor");
            }
            else if (kind == TimewarpKind.Major)
            {
                tags.Add("timewarp:major");
            }
            else
            {
                tags.Add("timewarp:unknown");
            }

            if (castsWhenBought)
            {
                tags.Add(TimewarpedCardBehavior.CastsWhenBoughtTag);
            }

            return new TimewarpedTavernCardDefinition
            {
                CardId = cardId,
                Name = name,
                CardKind = CardKind.TavernSpell,
                TimewarpKind = kind,
                Cost = cost,
                TechLevel = techLevel,
                Attack = 0,
                Health = 0,
                Tribes = new List<Tribe> { Tribe.None },
                Keywords = new List<Keyword>(),
                Text = text,
                ImagePath = "CardImages/" + cardId,
                EffectIds = new List<string>(),
                Tags = tags,
                PoolStatus = "blocked_by_non_minion_support",
                PurchaseBehavior = TimewarpedPurchaseBehavior.Unsupported
            };
        }

        private static TimewarpedPurchaseBehavior MapPurchaseBehavior(string value)
        {
            switch (value)
            {
                case "EntersHand":
                case "enters_hand":
                    return TimewarpedPurchaseBehavior.EntersHand;
                case "CastsWhenBought":
                case "casts_when_bought":
                    return TimewarpedPurchaseBehavior.CastsWhenBought;
                case "Exit":
                case "exit":
                    return TimewarpedPurchaseBehavior.Exit;
                case "Unsupported":
                case "unsupported":
                    return TimewarpedPurchaseBehavior.Unsupported;
                default:
                    return TimewarpedPurchaseBehavior.Auto;
            }
        }

        private static List<TimewarpedMechanicTemplate> MapMechanicTemplates(List<string> values)
        {
            var templates = new List<TimewarpedMechanicTemplate>();
            if (values == null)
            {
                return templates;
            }

            foreach (var value in values)
            {
                var template = MapMechanicTemplate(value);
                if (template != TimewarpedMechanicTemplate.Auto && !templates.Contains(template))
                {
                    templates.Add(template);
                }
            }

            return templates;
        }

        private static TimewarpedMechanicTemplate MapMechanicTemplate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return TimewarpedMechanicTemplate.Auto;
            }

            switch (value)
            {
                case "start_of_combat": return TimewarpedMechanicTemplate.StartOfCombat;
                case "end_of_turn": return TimewarpedMechanicTemplate.EndOfTurn;
                case "token_summon": return TimewarpedMechanicTemplate.TokenSummon;
                case "generate_card": return TimewarpedMechanicTemplate.GenerateCard;
                case "shop_interaction": return TimewarpedMechanicTemplate.ShopInteraction;
                case "hero_power": return TimewarpedMechanicTemplate.HeroPower;
                default:
                    return Enum.TryParse(value, true, out TimewarpedMechanicTemplate parsed)
                        ? parsed
                        : TimewarpedMechanicTemplate.Auto;
            }
        }

        private static CardKind MapCardKind(string value)
        {
            switch (value)
            {
                case "TavernSpell": return CardKind.TavernSpell;
                case "Spell": return CardKind.Spell;
                case "Trinket": return CardKind.Trinket;
                default: return CardKind.Minion;
            }
        }

        private static TimewarpKind MapTimewarpKind(string value)
        {
            switch (value)
            {
                case "minor":
                case "Minor":
                    return TimewarpKind.Minor;
                case "major":
                case "Major":
                    return TimewarpKind.Major;
                case "historical":
                case "Historical":
                    return TimewarpKind.Historical;
                default:
                    return TimewarpKind.None;
            }
        }

        private static List<Tribe> MapTribes(List<string> raw)
        {
            var tribes = new List<Tribe>();
            if (raw != null)
            {
                foreach (var tribe in raw)
                {
                    var mapped = MapTribe(tribe);
                    if (!tribes.Contains(mapped))
                    {
                        tribes.Add(mapped);
                    }
                }
            }

            if (tribes.Count == 0)
            {
                tribes.Add(Tribe.None);
            }

            return tribes;
        }

        private static Tribe MapTribe(string value)
        {
            switch (value)
            {
                case "BEAST":
                case "Beast":
                    return Tribe.Beast;
                case "MURLOC":
                case "Murloc":
                    return Tribe.Murloc;
                case "MECH":
                case "Mech":
                    return Tribe.Mech;
                case "DEMON":
                case "Demon":
                    return Tribe.Demon;
                case "DRAGON":
                case "Dragon":
                    return Tribe.Dragon;
                case "PIRATE":
                case "Pirate":
                    return Tribe.Pirate;
                case "ELEMENTAL":
                case "Elemental":
                    return Tribe.Elemental;
                case "QUILBOAR":
                case "Quilboar":
                    return Tribe.Quilboar;
                case "UNDEAD":
                case "Undead":
                    return Tribe.Undead;
                case "NAGA":
                case "Naga":
                    return Tribe.Naga;
                case "ALL":
                case "All":
                    return Tribe.All;
                default:
                    return Tribe.None;
            }
        }

        private static List<Keyword> MapKeywords(List<string> raw)
        {
            var keywords = new List<Keyword>();
            if (raw == null)
            {
                return keywords;
            }

            foreach (var keyword in raw)
            {
                if (!TryMapKeywordTag(keyword, out var mapped))
                {
                    continue;
                }

                if (!keywords.Contains(mapped))
                {
                    keywords.Add(mapped);
                }
            }

            return keywords;
        }

        private static bool TryMapKeywordTag(string value, out Keyword keyword)
        {
            switch (value)
            {
                case "TAUNT":
                case "Taunt":
                    keyword = Keyword.Taunt;
                    return true;
                case "DIVINE_SHIELD":
                case "DivineShield":
                    keyword = Keyword.DivineShield;
                    return true;
                case "POISONOUS":
                case "Poisonous":
                    keyword = Keyword.Poisonous;
                    return true;
                case "VENOMOUS":
                case "Venomous":
                    keyword = Keyword.Venomous;
                    return true;
                case "REBORN":
                case "Reborn":
                    keyword = Keyword.Reborn;
                    return true;
                case "DEATHRATTLE":
                case "Deathrattle":
                    keyword = Keyword.Deathrattle;
                    return true;
                case "BATTLECRY":
                case "Battlecry":
                    keyword = Keyword.Battlecry;
                    return true;
                case "WINDFURY":
                case "Windfury":
                    keyword = Keyword.Windfury;
                    return true;
                case "CLEAVE":
                case "Cleave":
                    keyword = Keyword.Cleave;
                    return true;
                case "MAGNETIC":
                case "MODULAR":
                case "Magnetic":
                    keyword = Keyword.Magnetic;
                    return true;
                case "AVENGE":
                case "Avenge":
                    keyword = Keyword.Avenge;
                    return true;
                case "BACON_RALLY":
                case "RALLY":
                case "Rally":
                    keyword = Keyword.Rally;
                    return true;
                case "SPELLCRAFT":
                case "BACON_SPELLCRAFT_ID":
                case "Spellcraft":
                    keyword = Keyword.Spellcraft;
                    return true;
                case "DISCOVER":
                case "Discover":
                    keyword = Keyword.Discover;
                    return true;
                case "STEALTH":
                case "Stealth":
                    keyword = Keyword.Stealth;
                    return true;
                case "AURA":
                case "Aura":
                    keyword = Keyword.Aura;
                    return true;
                case "END_OF_TURN":
                case "END_OF_TURN_TRIGGER":
                case "EndOfTurn":
                    keyword = Keyword.EndOfTurn;
                    return true;
                case "TRIGGER_VISUAL":
                case "Trigger":
                    keyword = Keyword.Trigger;
                    return true;
                default:
                    keyword = Keyword.Trigger;
                    return false;
            }
        }

        [Serializable]
        private sealed class RawPayload
        {
            public int count;
            public List<RawCard> cards;
        }

        [Serializable]
        private sealed class RawCard
        {
            public string cardId;
            public int dbfId;
            public string name;
            public string zhName;
            public string cardKind;
            public string timewarpKind;
            public int cost;
            public int techLevel;
            public int attack;
            public int health;
            public List<string> tribes;
            public List<string> keywords;
            public string text;
            public string zhText;
            public string imagePath;
            public List<string> effectIds;
            public List<string> tags;
            public string poolStatus;
            public string purchaseBehavior;
            public string mechanicTemplate;
            public List<string> mechanicTemplates;
            public string goldenCardId;
            public int goldenDbfId;
        }
    }
}
