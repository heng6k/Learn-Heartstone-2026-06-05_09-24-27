using System;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Content
{
    public static class BattlegroundsLocalizedText
    {
        public static string TrinketSlot(TrinketSlotKind value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            return value == TrinketSlotKind.Greater ? "大饰品" : "小饰品";
        }

        public static string TrinketImplementation(TrinketImplementationStatus value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case TrinketImplementationStatus.Implemented: return "已实现";
                case TrinketImplementationStatus.FrameworkFirst: return "框架支持";
                case TrinketImplementationStatus.Planned: return "计划中";
                case TrinketImplementationStatus.Deferred: return "已延期";
                case TrinketImplementationStatus.Unsupported: return "暂不支持";
                default: return "未登记";
            }
        }

        public static string OfferPool(TrinketOfferPoolStatus value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case TrinketOfferPoolStatus.Offerable: return "可正常提供";
                case TrinketOfferPoolStatus.HiddenEffectOnly: return "仅隐藏效果";
                case TrinketOfferPoolStatus.DebugOnly: return "仅调试";
                default: return "已禁用";
            }
        }

        public static string OfferPool(QuestOfferPoolStatus value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case QuestOfferPoolStatus.Offerable: return "可正常提供";
                case QuestOfferPoolStatus.HiddenEffectOnly: return "仅隐藏效果";
                case QuestOfferPoolStatus.DebugOnly: return "仅调试";
                default: return "已禁用";
            }
        }

        public static string Power(TrinketPowerLevel value, bool useEnglish)
        {
            return Power((int)value, value.ToString(), useEnglish);
        }

        public static string Power(QuestRewardPowerLevel value, bool useEnglish)
        {
            return Power((int)value, value.ToString(), useEnglish);
        }

        private static string Power(int value, string english, bool useEnglish)
        {
            if (useEnglish) return english;
            switch (value)
            {
                case 1: return "较弱";
                case 2: return "中等";
                case 3: return "较强";
                case 4: return "强力";
                default: return "待评估";
            }
        }

        public static string TrinketTrigger(TrinketTriggerTemplate value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case TrinketTriggerTemplate.OnEquip: return "装备时";
                case TrinketTriggerTemplate.Passive: return "持续生效";
                case TrinketTriggerTemplate.TurnStart: return "回合开始时";
                case TrinketTriggerTemplate.TurnEnd: return "回合结束时";
                case TrinketTriggerTemplate.ShopRefresh: return "刷新酒馆时";
                case TrinketTriggerTemplate.CardBought: return "购买卡牌时";
                case TrinketTriggerTemplate.CardSold: return "出售卡牌时";
                case TrinketTriggerTemplate.MinionPlayed: return "使用随从时";
                case TrinketTriggerTemplate.SpellCast: return "施放法术时";
                case TrinketTriggerTemplate.SpellcraftCast: return "施放塑造法术时";
                case TrinketTriggerTemplate.StartOfCombat: return "战斗开始时";
                case TrinketTriggerTemplate.Avenge: return "复仇触发";
                case TrinketTriggerTemplate.Combat: return "战斗中";
                case TrinketTriggerTemplate.Auto: return "自动判定";
                default: return "其他时机";
            }
        }

        public static string QuestTrigger(QuestRewardTrigger value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case QuestRewardTrigger.OnComplete: return "完成任务时";
                case QuestRewardTrigger.TurnStarted: return "回合开始时";
                case QuestRewardTrigger.TurnEnded: return "回合结束时";
                case QuestRewardTrigger.CardBought: return "购买卡牌时";
                case QuestRewardTrigger.CardPlayed: return "使用卡牌时";
                case QuestRewardTrigger.MinionPlayed: return "使用随从时";
                case QuestRewardTrigger.MinionSold: return "出售随从时";
                case QuestRewardTrigger.ShopRefreshed: return "刷新酒馆时";
                case QuestRewardTrigger.StartOfCombat: return "战斗开始时";
                case QuestRewardTrigger.AfterCombat: return "战斗结束后";
                case QuestRewardTrigger.CombatMinionSummoned: return "战斗中召唤随从时";
                case QuestRewardTrigger.CombatFriendlyMinionDied: return "战斗中友方随从死亡时";
                case QuestRewardTrigger.CombatAfterAttack: return "战斗中攻击后";
                case QuestRewardTrigger.SpellcraftGenerated: return "生成塑造法术时";
                case QuestRewardTrigger.DiscoverChosen: return "完成发现时";
                default: return "满足条件时";
            }
        }

        public static string QuestEffect(QuestRewardEffectKind value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            var name = value.ToString();
            if (value == QuestRewardEffectKind.None) return "无额外效果";
            if (name.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Cost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Refresh", StringComparison.OrdinalIgnoreCase) >= 0) return "经济与酒馆效果";
            if (name.IndexOf("Buff", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Stats", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Gem", StringComparison.OrdinalIgnoreCase) >= 0) return "随从强化效果";
            if (name.IndexOf("Discover", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Gain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Copy", StringComparison.OrdinalIgnoreCase) >= 0) return "获取卡牌效果";
            if (name.IndexOf("Combat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Summon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Deathrattle", StringComparison.OrdinalIgnoreCase) >= 0) return "战斗效果";
            if (name.IndexOf("Battlecry", StringComparison.OrdinalIgnoreCase) >= 0) return "战吼效果";
            if (name.IndexOf("EndOfTurn", StringComparison.OrdinalIgnoreCase) >= 0) return "回合结束效果";
            if (name.IndexOf("Spell", StringComparison.OrdinalIgnoreCase) >= 0) return "法术效果";
            return "特殊规则效果";
        }

        public static string QuestObjective(QuestObjectiveKind value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case QuestObjectiveKind.BuyCards: return "购买卡牌";
                case QuestObjectiveKind.BuyMinions: return "购买随从";
                case QuestObjectiveKind.BuyTavernSpells: return "购买酒馆法术";
                case QuestObjectiveKind.AddCardsToHand: return "获取手牌";
                case QuestObjectiveKind.SellMinions: return "出售随从";
                case QuestObjectiveKind.SpendGold: return "花费铸币";
                case QuestObjectiveKind.RefreshShop: return "刷新酒馆";
                case QuestObjectiveKind.CastSpells: return "施放法术";
                case QuestObjectiveKind.CastTavernSpells: return "施放酒馆法术";
                default: return "使用战吼随从";
            }
        }

        public static string QuestImplementation(QuestImplementationStatus value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case QuestImplementationStatus.Implemented: return "已实现";
                case QuestImplementationStatus.FrameworkFirst: return "框架支持";
                case QuestImplementationStatus.Planned: return "计划中";
                case QuestImplementationStatus.Deferred: return "已延期";
                case QuestImplementationStatus.Unsupported: return "暂不支持";
                default: return "未登记";
            }
        }

        public static string AnomalyFamily(AnomalyEffectFamily value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case AnomalyEffectFamily.Economy: return "经济";
                case AnomalyEffectFamily.TavernRefresh: return "酒馆刷新";
                case AnomalyEffectFamily.MinionPool: return "随从池";
                case AnomalyEffectFamily.Buddy: return "伙伴";
                case AnomalyEffectFamily.DarkmoonPrize: return "暗月奖品";
                case AnomalyEffectFamily.SecondHeroPower: return "第二英雄技能";
                case AnomalyEffectFamily.Timewarp: return "时空酒馆";
                case AnomalyEffectFamily.GeneratedSpell: return "衍生法术";
                case AnomalyEffectFamily.GeneratedMinion: return "衍生随从";
                case AnomalyEffectFamily.DelayedReward: return "延迟奖励";
                case AnomalyEffectFamily.TripleRule: return "三连规则";
                case AnomalyEffectFamily.CombatRule: return "战斗规则";
                case AnomalyEffectFamily.SharedLobbyChoice: return "全局抉择";
                case AnomalyEffectFamily.SinglePlayerChoice: return "玩家抉择";
                case AnomalyEffectFamily.HeroReplacement: return "英雄替换";
                default: return "其他规则";
            }
        }

        public static string AnomalyImplementation(AnomalyImplementationStatus value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case AnomalyImplementationStatus.Implemented: return "已实现";
                case AnomalyImplementationStatus.OfferableWithExactProxy: return "可用等效实现";
                case AnomalyImplementationStatus.FrameworkOnly: return "仅框架支持";
                case AnomalyImplementationStatus.Planned: return "计划中";
                case AnomalyImplementationStatus.BlockedByDependency: return "等待依赖";
                case AnomalyImplementationStatus.DebugOnly: return "仅调试";
                default: return "暂不支持";
            }
        }

        public static string HeroImplementation(HeroEffectImplementationStatus value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case HeroEffectImplementationStatus.Implemented: return "已实现";
                case HeroEffectImplementationStatus.Next: return "即将实现";
                case HeroEffectImplementationStatus.Planned: return "计划中";
                case HeroEffectImplementationStatus.FrameworkFirst: return "框架支持";
                case HeroEffectImplementationStatus.Deferred: return "已延期";
                default: return "未登记";
            }
        }

        public static string HeroImplementation(string value, bool useEnglish)
        {
            if (useEnglish || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            return Enum.TryParse(value, true, out HeroEffectImplementationStatus status)
                ? HeroImplementation(status, false)
                : "状态未知";
        }

        public static string HeroPowerCategory(HeroPowerCategory value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case LearnHearthstone.Domain.Models.HeroPowerCategory.Economy: return "经济";
                case LearnHearthstone.Domain.Models.HeroPowerCategory.Buff: return "强化";
                case LearnHearthstone.Domain.Models.HeroPowerCategory.Combat: return "战斗";
                case LearnHearthstone.Domain.Models.HeroPowerCategory.Minion: return "随从";
                case LearnHearthstone.Domain.Models.HeroPowerCategory.Discover: return "发现";
                case LearnHearthstone.Domain.Models.HeroPowerCategory.Health: return "生命值";
                case LearnHearthstone.Domain.Models.HeroPowerCategory.Passive: return "被动";
                case LearnHearthstone.Domain.Models.HeroPowerCategory.HeroSwap: return "更换英雄";
                default: return "其他";
            }
        }

        public static string HeroPowerEligibility(HeroPowerReplacementEligibility value, bool useEnglish)
        {
            if (useEnglish) return value.ToString();
            switch (value)
            {
                case HeroPowerReplacementEligibility.DiscoverableAfterStart: return "开局后可发现";
                case HeroPowerReplacementEligibility.InitialOnly: return "仅初始技能";
                case HeroPowerReplacementEligibility.NonSelectable: return "不可选择";
                default: return "已禁用";
            }
        }

        public static string Slot(string value, bool useEnglish)
        {
            if (useEnglish || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            switch (value.Trim().ToLowerInvariant())
            {
                case "lesser": return "小饰品";
                case "greater": return "大饰品";
                case "main": return "主要任务";
                case "bonus": return "额外任务";
                case "endofturn": return "回合结束";
                default: return "通用";
            }
        }

        public static string Tribe(string value, bool useEnglish)
        {
            if (useEnglish || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            switch (value.Trim().ToLowerInvariant())
            {
                case "beast": return "野兽";
                case "demon": return "恶魔";
                case "dragon": return "龙";
                case "elemental": return "元素";
                case "mech":
                case "mechanical": return "机械";
                case "murloc": return "鱼人";
                case "naga": return "纳迦";
                case "pirate": return "海盗";
                case "quilboar": return "野猪人";
                case "undead": return "亡灵";
                default: return "中立";
            }
        }

        public static string MechanicTag(string value, bool useEnglish)
        {
            if (useEnglish || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            var normalized = value.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "battlecry": return "战吼";
                case "deathrattle": return "亡语";
                case "discover": return "发现";
                case "economy": return "经济";
                case "choose_one": return "抉择";
                case "combat_start": return "战斗开始";
                case "combat_event": return "战斗事件";
                case "shop_refresh": return "刷新酒馆";
                case "spell_cast": return "施放法术";
                case "spellcraft": return "塑造法术";
                case "turn_start": return "回合开始";
                case "turn_end": return "回合结束";
                case "on_equip": return "装备时";
                case "stats": return "属性强化";
                case "board_aura": return "战场光环";
                case "shop_aura": return "酒馆光环";
                case "trinket_choice": return "饰品选择";
                case "hero_power": return "英雄技能";
                case "buddy": return "伙伴";
                case "blood_gem": return "鲜血宝石";
                case "magnetic": return "磁力";
                case "buffstats": return "属性强化";
                case "grantkeyword": return "赋予关键词";
                case "generatecard": return "生成卡牌";
                case "summon": return "召唤";
                case "shopmodifier": return "酒馆调整";
                case "combatmodifier": return "战斗调整";
                case "tribesynergy": return "种族联动";
                case "spellsynergy": return "法术联动";
                case "poolmodifier": return "卡池调整";
                case "utility": return "功能效果";
                default: return "其他机制";
            }
        }

        public static string ProxyLevel(string value, bool useEnglish)
        {
            if (useEnglish || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            switch (value.Trim().ToLowerInvariant())
            {
                case "exact": return "精确实现";
                case "proxysafe": return "等效实现";
                default: return "标准实现";
            }
        }

        public static string FilterTag(string tag, bool useEnglish)
        {
            if (useEnglish || string.IsNullOrWhiteSpace(tag)) return tag ?? string.Empty;
            var separator = tag.IndexOf(':');
            var prefix = separator > 0 ? tag.Substring(0, separator).ToLowerInvariant() : string.Empty;
            var value = separator >= 0 && separator + 1 < tag.Length ? tag.Substring(separator + 1) : tag;
            if (prefix == "race" || Enum.TryParse(value, true, out LearnHearthstone.Domain.Models.Tribe _)) return Tribe(value, false);
            if (prefix == "power")
            {
                switch (value.ToLowerInvariant())
                {
                    case "weak": return "较弱";
                    case "medium": return "中等";
                    case "strong": return "较强";
                    case "premium": return "强力";
                }
            }
            if (prefix == "timing" && Enum.TryParse(value, true, out TrinketTriggerTemplate trigger))
            {
                return TrinketTrigger(trigger, false);
            }
            if (prefix == "slot") return Slot(value, false);
            if (prefix == "category")
            {
                if (Enum.TryParse(value, true, out LearnHearthstone.Domain.Models.HeroPowerCategory category))
                {
                    return HeroPowerCategory(category, false);
                }
            }
            if (prefix == "status")
            {
                if (Enum.TryParse(value, true, out HeroEffectImplementationStatus heroStatus)) return HeroImplementation(heroStatus, false);
                if (Enum.TryParse(value, true, out TrinketImplementationStatus trinketStatus)) return TrinketImplementation(trinketStatus, false);
                if (Enum.TryParse(value, true, out QuestImplementationStatus questStatus)) return QuestImplementation(questStatus, false);
            }
            if (prefix == "pool")
            {
                if (Enum.TryParse(value, true, out TrinketOfferPoolStatus trinketPool)) return OfferPool(trinketPool, false);
                if (Enum.TryParse(value, true, out QuestOfferPoolStatus questPool)) return OfferPool(questPool, false);
            }
            return MechanicTag(value, false);
        }
    }
}
