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
        private static readonly Dictionary<string, string[]> SupplementalZhCn = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "BG34_BlackMarket_Skip", new[] { "退出时空酒馆", "退出时空酒馆。剩余时空资源会保留到下一次时空穿梭。" } },
            { "BG34_Treasure_900", new[] { "时空扭曲的进化酒馆", "获得一张“进化酒馆”。在每个回合开始时重复此效果。" } },
            { "BG34_Treasure_902", new[] { "时空扭曲的大盗", "发现你上一局战队中的一个随从。它保留属性值和额外关键词。" } },
            { "BG34_Treasure_903", new[] { "时空扭曲的免费招待", "发现3个你当前酒馆等级的随从。" } },
            { "BG34_Treasure_905", new[] { "时空扭曲的笼中鼠", "使一个随从获得+2/+2，然后使其属性值翻倍。" } },
            { "BG34_Treasure_912", new[] { "时空扭曲的香蕉盛宴", "用酒馆餐点香蕉填满你的手牌。本局对战中，你的酒馆法术额外使目标获得+3/+3。" } },
            { "BG34_HeroPowerSpell_003", new[] { "奥拉基尔之力", "购买时施放。将“虫群拍打”作为你的第二个英雄技能。" } },
            { "BG34_Treasure_917", new[] { "时空扭曲的新兵", "本局对战中，使酒馆中的随从获得+2/+2，且酒馆始终拥有7张牌。" } },
            { "BG34_HeroPowerSpell_005", new[] { "伊莉斯之力", "购买时施放。将“领路探险者”作为你的第二个英雄技能。" } },
            { "BG34_HeroPowerSpell_006", new[] { "乔治之力", "购买时施放。将“圣光恩泽”作为你的第二个英雄技能。" } },
            { "BG34_HeroPowerSpell_008", new[] { "沙德沃克之力", "购买时施放。将“碎碎念”作为你的第二个英雄技能。" } },
            { "BG34_HeroPowerSpell_009", new[] { "特隆之力", "购买时施放。将“快速复生”作为你的第二个英雄技能。" } },
            { "BG34_HeroPowerSpell_010", new[] { "巫妖王之力", "购买时施放。将“复生仪式”作为你的第二个英雄技能。" } },
            { "BG34_HeroPowerSpell_012", new[] { "泽瑞斯之力", "购买时施放。将“三个愿望”作为你的第二个英雄技能。" } },
            { "BG34_HeroPowerSpell_015", new[] { "古夫之力", "购买时施放。将“自然平衡”作为你的第二个英雄技能。" } },
            { "BG34_Treasure_932", new[] { "时空扭曲的攻击油", "你的铸币上限提高4点，并获得4枚铸币。" } },
            { "BG34_Treasure_933", new[] { "时空扭曲的进化", "选择一个随从，发现一个六级随从并将其变形为该随从，然后将其属性值设为30/30。" } },
            { "BG34_Treasure_934", new[] { "时空扭曲的护甲储藏", "购买时施放。获得10点护甲。" } },
            { "BG34_Treasure_937", new[] { "时空扭曲的尸骸", "发现一个五级或更高等级的亡语随从，并使其获得复生。" } },
            { "BG34_Treasure_940", new[] { "时空扭曲的厨师之选", "选择一个随从，分别获得一个与其类型相同的四级、五级和六级随机随从。" } },
            { "BG34_Treasure_300", new[] { "时空扭曲的投资", "购买时施放。你下一次时空穿梭会额外获得1点时空资源。" } },
            { "BG34_Treasure_301", new[] { "时空扭曲的豆茎", "从大型时空酒馆中发现一张消耗为1的牌，并将其在手牌中锁定1回合。" } },
            { "BG34_HeroPowerSpell_016", new[] { "弗勒格尔之力", "购买时施放。将“出海捕鱼”作为你的第二个英雄技能。" } },
            { "BG34_HeroPowerSpell_017", new[] { "希尔瓦娜斯之力", "购买时施放。将“回收灵魂”作为你的第二个英雄技能。" } },
            { "BG34_HeroPowerSpell_018", new[] { "塔维什之力", "购买时施放。将“锁定目标”作为你的第二个英雄技能。" } },
            { "BG34_Treasure_302", new[] { "时空扭曲的克隆装置", "选择一个友方随从，召唤一个完全相同的复制。" } },
            { "BG34_Treasure_950", new[] { "时空扭曲的特别惊喜", "用随机塑造法术填满你的手牌。" } },
            { "BG34_Treasure_951", new[] { "时空扭曲的海螺", "选择一个友方鱼人，召唤一个完全相同的复制。" } },
            { "BG34_Treasure_953", new[] { "时空扭曲的启示", "从小型时空酒馆中发现一个消耗为1的随从和一个消耗为2的随从。" } },
            { "BG34_HeroPowerSpell_022", new[] { "拉卡尼休之力", "购买时施放。将“酒馆照明”作为你的第二个英雄技能。" } },
            { "BG34_Treasure_919", new[] { "时空扭曲的仪式", "发现两个七级随从。" } },
            { "BG34_Treasure_955", new[] { "时空扭曲的镀金器", "选择一个随从，将其变为金色。" } },
            { "BG34_Treasure_606", new[] { "时空扭曲的大赢家！", "发现一张三级暗月奖品。以后每过三个回合，在回合开始时重复此效果。" } },
            { "BG34_Treasure_607", new[] { "时空扭曲的见习生", "分别获得一个一级、二级和三级随机随从。" } },
            { "BG34_Treasure_608", new[] { "时空扭曲的戒指", "获得一张“闪亮戒指”。在每个回合开始时重复此效果。" } },
            { "BG34_Treasure_609", new[] { "时空扭曲的套索", "获得一张“附魔套索”。在每个回合开始时重复此效果。" } },
            { "BG34_Treasure_966", new[] { "时空扭曲的小偷", "发现你上一局战队中的一个随从，并将其属性值设为20/20。" } },
            { "BG34_Treasure_620", new[] { "时空扭曲的苹果", "本局对战中，每当酒馆刷新后，施放“这些苹果”。" } },
            { "BG34_Treasure_625", new[] { "时空扭曲的秘密", "发现一个金色七级随从。" } }
        };

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

                ApplySupplementalLocalization(definition);
                CompleteMechanicTemplates(definition);
                definitions.Add(definition);
                existing.Add(definition.CardId);
            }
        }

        private static void ApplySupplementalLocalization(TimewarpedTavernCardDefinition definition)
        {
            if (definition != null && SupplementalZhCn.TryGetValue(definition.CardId ?? string.Empty, out var localized))
            {
                definition.ZhName = localized[0];
                definition.ZhText = localized[1];
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
