using System;
using System.Collections.Generic;
using System.Linq;

namespace LearnHearthstone.Domain.Engine
{
    public sealed class ChooseOneOptionDefinition
    {
        public ChooseOneOptionDefinition(
            string choiceId,
            string name,
            string englishName,
            string text,
            string englishText,
            bool requiresPlayerTarget)
        {
            ChoiceId = choiceId;
            Name = name;
            EnglishName = englishName;
            Text = text;
            EnglishText = englishText;
            RequiresPlayerTarget = requiresPlayerTarget;
        }

        public string ChoiceId { get; }
        public string Name { get; }
        public string EnglishName { get; }
        public string Text { get; }
        public string EnglishText { get; }
        public bool RequiresPlayerTarget { get; }
    }

    public static class ChooseOneOptionRegistry
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyList<ChooseOneOptionDefinition>> OptionsByCardId =
            new Dictionary<string, IReadOnlyList<ChooseOneOptionDefinition>>(StringComparer.OrdinalIgnoreCase)
            {
                ["117573"] = new[]
                {
                    new ChooseOneOptionDefinition(
                        "immediate",
                        "立即增益",
                        "Right Now",
                        "使你的随从获得+2/+2。",
                        "Give your minions +2/+2.",
                        false),
                    new ChooseOneOptionDefinition(
                        "delayed",
                        "双倍规划",
                        "Double Later",
                        "在你的下个回合开始时，使你的随从获得+2/+2，触发两次。",
                        "At the start of your next turn, give your minions +2/+2 twice.",
                        false)
                },
                ["117567"] = new[]
                {
                    new ChooseOneOptionDefinition(
                        "attack",
                        "进攻旗帜",
                        "Banner of Might",
                        "使一个随从获得+3/+1。",
                        "Give a minion +3/+1.",
                        true),
                    new ChooseOneOptionDefinition(
                        "health",
                        "守护旗帜",
                        "Banner of Fortitude",
                        "使一个随从获得+1/+3。",
                        "Give a minion +1/+3.",
                        true)
                },
                ["117584"] = new[]
                {
                    new ChooseOneOptionDefinition(
                        "target",
                        "森林馈赠",
                        "Focused Bounty",
                        "使一个随从获得+6/+6，触发两次。",
                        "Give a minion +6/+6 twice.",
                        true),
                    new ChooseOneOptionDefinition(
                        "board",
                        "森林共荣",
                        "Shared Bounty",
                        "使你的随从获得+2/+2。",
                        "Give your minions +2/+2.",
                        false)
                },
                ["115910"] = new[]
                {
                    new ChooseOneOptionDefinition(
                        "minion",
                        "随从潜力",
                        "Minion Potential",
                        "发现一张你当前等级的随从牌。",
                        "Discover a minion of your Tavern Tier.",
                        false),
                    new ChooseOneOptionDefinition(
                        "spell",
                        "法术潜力",
                        "Spell Potential",
                        "发现一张你当前等级的酒馆法术牌。",
                        "Discover a Tavern spell of your Tavern Tier.",
                        false)
                },
                ["VOLCANIC_VISITOR_CHOICE_SPELL"] = new[]
                {
                    new ChooseOneOptionDefinition(
                        "attack",
                        "熔岩之力",
                        "Molten Might",
                        "使你的随从获得+4攻击力。",
                        "Give your minions +4 Attack.",
                        false),
                    new ChooseOneOptionDefinition(
                        "health",
                        "火山之韧",
                        "Volcanic Fortitude",
                        "使你的随从获得+4生命值。",
                        "Give your minions +4 Health.",
                        false)
                }
            };

        public static IReadOnlyCollection<string> RegisteredCardIds => OptionsByCardId.Keys.ToList();

        public static bool TryGetOptions(string cardId, out IReadOnlyList<ChooseOneOptionDefinition> options)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                options = Array.Empty<ChooseOneOptionDefinition>();
                return false;
            }

            return OptionsByCardId.TryGetValue(cardId, out options);
        }

        public static bool TryGetOption(
            string cardId,
            string choiceId,
            out ChooseOneOptionDefinition option)
        {
            option = null;
            if (!TryGetOptions(cardId, out var options) || string.IsNullOrWhiteSpace(choiceId))
            {
                return false;
            }

            option = options.FirstOrDefault(candidate =>
                string.Equals(candidate.ChoiceId, choiceId, StringComparison.OrdinalIgnoreCase));
            return option != null;
        }
    }
}
