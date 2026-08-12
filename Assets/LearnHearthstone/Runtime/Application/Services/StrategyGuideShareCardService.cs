using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public static class StrategyGuideShareCardService
    {
        private static readonly IReadOnlyDictionary<string, string> ChineseTribes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Beast"] = "野兽",
                ["Murloc"] = "鱼人",
                ["Mech"] = "机械",
                ["Demon"] = "恶魔",
                ["Dragon"] = "龙",
                ["Pirate"] = "海盗",
                ["Elemental"] = "元素",
                ["Naga"] = "纳迦",
                ["Quilboar"] = "野猪人",
                ["Undead"] = "亡灵"
            };

        public static StrategyGuideShareCardModel Create(
            StrategyGuideCatalog catalog,
            string guideId,
            ResolvedGameVersion version,
            GameCatalogSet catalogs,
            bool useEnglish)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            return Create(
                catalog,
                guideId,
                catalog.GetDefaultProfile(guideId).ProfileId,
                version,
                catalogs,
                useEnglish);
        }

        public static StrategyGuideShareCardModel Create(
            StrategyGuideCatalog catalog,
            string guideId,
            string profileId,
            ResolvedGameVersion version,
            GameCatalogSet catalogs,
            bool useEnglish)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }
            if (catalogs == null)
            {
                throw new ArgumentNullException(nameof(catalogs));
            }

            var guide = catalog.GetGuide(guideId);
            var profile = catalog.GetProfile(guideId, profileId);
            var compiled = StrategyGuideScenarioCompiler.Compile(
                catalog,
                guide,
                version,
                useEnglish,
                profile.ProfileId);
            var publicCode = StrategyGuidePortableCodeService.Export(
                catalog,
                guideId,
                profile.ProfileId,
                version);
            var contentHash = publicCode.Split('.')[2];
            var scenario = compiled.Scenario;
            var model = new StrategyGuideShareCardModel
            {
                GuideId = guide.GuideId,
                ProfileId = profile.ProfileId,
                RevisionId = guide.RevisionId,
                GameVersionId = guide.GameVersionId,
                ContentSnapshotId = version.ContentSnapshotId,
                Title = Localized(guide.Title, guide.EnglishTitle, useEnglish),
                Summary = Localized(guide.Summary, guide.EnglishSummary, useEnglish),
                Archetype = guide.Archetype,
                Difficulty = profile.Difficulty,
                DifficultyTitle = Localized(profile.Title, profile.EnglishTitle, useEnglish),
                LearningGoal = Localized(profile.LearningGoal, profile.EnglishLearningGoal, useEnglish),
                StartRound = scenario.SavedAtRound,
                TavernTier = scenario.Tavern.Tier,
                Gold = scenario.Tavern.Gold,
                MaxGold = scenario.Tavern.MaxGold,
                AllowsUndo = profile.Undo != null && profile.Undo.UsesPerRun > 0,
                PublicCode = publicCode,
                ContentHash = contentHash,
                ContentHashShort = contentHash.Substring(0, Math.Min(12, contentHash.Length)),
                Hero = HeroAsset(catalogs, guide.HeroCardId, useEnglish),
                LesserTrinket = TrinketAsset(catalogs, guide.LesserTrinketCardId),
                GreaterTrinket = TrinketAsset(catalogs, guide.GreaterTrinketCardId),
                HasControlledOffers = HasControlledOffer(profile),
                CompletionCondition = CompletionCondition(profile, useEnglish)
            };
            model.RecommendedLesserTrinkets.AddRange(RecommendedTrinketIds(
                    guide.RecommendedLesserTrinketCardIds,
                    guide.LesserTrinketCardId)
                .Select(cardId => TrinketAsset(catalogs, cardId)));
            model.RecommendedGreaterTrinkets.AddRange(RecommendedTrinketIds(
                    guide.RecommendedGreaterTrinketCardIds,
                    guide.GreaterTrinketCardId)
                .Select(cardId => TrinketAsset(catalogs, cardId)));

            model.ProbabilityNotice = model.HasControlledOffers
                ? (useEnglish
                    ? "Teaching offers are controlled. Actual matches use normal probabilities."
                    : "教学候选已受控，实际游戏以正常概率为准。")
                : (useEnglish
                    ? "The fixed seed is only used to reproduce this training scenario."
                    : "固定种子仅用于复现本训练场景。");
            model.Disclaimer = useEnglish
                ? "Unofficial training tool · image and LHSG1 code share one revision"
                : "非官方训练工具 · 图片与 LHSG1 代码同源";

            model.ActiveTribes.AddRange((guide.ActiveTribes ?? new List<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => LocalizedTribe(item, useEnglish)));
            model.FinalComposition.AddRange((guide.FinalComposition ?? new List<StrategyGuideCardDefinition>())
                .Where(item => item != null)
                .Select(item => MinionAsset(catalogs, item.CardId, item.Golden, useEnglish)));
            model.CoreCards.AddRange((guide.CoreMinionCardIds ?? new List<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(item => MinionAsset(catalogs, item, false, useEnglish)));
            model.CoreCards.AddRange((guide.CoreSpellCardNumbers ?? new List<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(item => SpellAsset(catalogs, item, useEnglish)));

            model.DarkGifts.AddRange((profile.DarkGiftAttachments ?? new List<StrategyGuideDarkGiftAttachment>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.GiftResearchKey))
                .Select(item => item.GiftResearchKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(item => DarkGiftAsset(catalogs, item)));
            model.Entries.Add(new StrategyGuideShareCardEntry
            {
                ProfileId = profile.ProfileId,
                Difficulty = profile.Difficulty,
                Title = model.DifficultyTitle,
                StrategyLabel = AcquisitionLabel(profile.AcquisitionPlan, useEnglish),
                AllowsUndo = model.AllowsUndo
            });

            model.StartingShop.AddRange((scenario.Shop ?? new List<ScenarioCardState>())
                .Where(item => item != null)
                .Select(item => ScenarioAsset(item, useEnglish)));
            model.StartingBoard.AddRange((scenario.PlayerBoard ?? new List<ScenarioCardState>())
                .Where(item => item != null)
                .Select(item => ScenarioAsset(item, useEnglish)));
            model.StartingHand.AddRange((scenario.Hand ?? new List<ScenarioCardState>())
                .Where(item => item != null)
                .Select(item => ScenarioAsset(item, useEnglish)));

            var decisions = useEnglish && profile.EnglishKeyDecisions != null && profile.EnglishKeyDecisions.Count > 0
                ? profile.EnglishKeyDecisions
                : profile.KeyDecisions ?? new List<string>();
            model.KeyDecisions.AddRange(decisions
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Take(3));

            model.ShapingTurns.AddRange((profile.ShapingSpellCardIds ?? new List<string>())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select((item, index) => new StrategyGuideShareCardShapingTurn
                {
                    LocalTurn = index + 1,
                    Spell = SpellAsset(catalogs, item, useEnglish)
                }));
            model.GrowthTargets.AddRange((profile.GrowthQuality ?? new List<StrategyGuideGrowthValue>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Key))
                .Select(item => new StrategyGuideShareCardGrowthTarget
                {
                    Key = item.Key,
                    Label = GrowthLabel(item.Key, useEnglish),
                    MinimumValue = item.Value
                }));
            return model;
        }

        private static StrategyGuideShareCardAsset ScenarioAsset(ScenarioCardState card, bool useEnglish)
        {
            return new StrategyGuideShareCardAsset
            {
                StableId = FirstNonEmpty(card.InstanceId, card.CardId),
                CardKind = card.CardKind,
                Name = card.Name,
                ImagePath = card.ImagePath,
                Golden = card.Golden,
                Badge = card.Golden ? (useEnglish ? "Golden" : "金色") : string.Empty,
                Attack = card.Attack,
                Health = card.Health,
                TavernTier = card.TavernTier,
                Cost = card.Cost
            };
        }

        private static StrategyGuideShareCardAsset HeroAsset(
            GameCatalogSet catalogs,
            string cardId,
            bool useEnglish)
        {
            var definition = catalogs.Heroes.GetHeroByCardId(cardId);
            return new StrategyGuideShareCardAsset
            {
                StableId = definition.HeroCardId,
                CardKind = CardKind.Hero,
                Name = useEnglish
                    ? FirstNonEmpty(definition.Name, definition.ZhName)
                    : FirstNonEmpty(definition.ZhName, definition.Name),
                ImagePath = definition.ImagePath
            };
        }

        private static StrategyGuideShareCardAsset TrinketAsset(GameCatalogSet catalogs, string cardId)
        {
            var definition = catalogs.Trinkets.GetByCardId(cardId);
            return new StrategyGuideShareCardAsset
            {
                StableId = definition.CardId,
                CardKind = CardKind.Trinket,
                Name = definition.Name,
                ImagePath = definition.ImagePath,
                Badge = definition.SlotKind == TrinketSlotKind.Lesser ? "小型" : "大型"
            };
        }

        private static StrategyGuideShareCardAsset MinionAsset(
            GameCatalogSet catalogs,
            string cardId,
            bool golden,
            bool useEnglish)
        {
            var definition = catalogs.Minions.GetByCardId(cardId);
            return new StrategyGuideShareCardAsset
            {
                StableId = definition.CardId,
                CardKind = CardKind.Minion,
                Name = definition.Name,
                ImagePath = definition.ImagePath,
                Golden = golden,
                Badge = golden ? (useEnglish ? "Golden" : "金色") : string.Empty,
                Attack = definition.BaseAttack,
                Health = definition.BaseHealth,
                TavernTier = definition.TavernTier
            };
        }

        private static StrategyGuideShareCardAsset SpellAsset(
            GameCatalogSet catalogs,
            string cardNumber,
            bool useEnglish)
        {
            var definition = catalogs.Spells.GetByCardNumber(cardNumber);
            return new StrategyGuideShareCardAsset
            {
                StableId = definition.CardNumber,
                CardKind = CardKind.TavernSpell,
                Name = useEnglish
                    ? FirstNonEmpty(definition.EnglishName, definition.Name)
                    : definition.Name,
                ImagePath = definition.ImagePath,
                Cost = definition.Cost
            };
        }

        private static StrategyGuideShareCardAsset DarkGiftAsset(GameCatalogSet catalogs, string researchKey)
        {
            var definition = catalogs.DarkGifts.GetByResearchKey(researchKey);
            return new StrategyGuideShareCardAsset
            {
                StableId = definition.ResearchKey,
                CardKind = CardKind.Spell,
                Name = definition.DisplayName,
                ImagePath = definition.ImagePath,
                Badge = "黑暗之赐"
            };
        }

        private static List<string> RecommendedTrinketIds(IEnumerable<string> recommendations, string fallbackCardId)
        {
            var values = (recommendations ?? Enumerable.Empty<string>())
                .Where(cardId => !string.IsNullOrWhiteSpace(cardId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (values.Count == 0 && !string.IsNullOrWhiteSpace(fallbackCardId))
            {
                values.Add(fallbackCardId);
            }
            return values;
        }

        private static bool HasControlledOffer(StrategyGuideEntryProfileDefinition profile)
        {
            var plan = profile?.AcquisitionPlan;
            if (plan == null)
            {
                return false;
            }
            return plan.DiscloseControlledOffers ||
                (plan.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                    .Any(item => item != null &&
                        !string.Equals(item.Policy, StrategyGuideOfferPolicies.NaturalSeeded, StringComparison.Ordinal));
        }

        private static string AcquisitionLabel(StrategyGuideAcquisitionPlanDefinition plan, bool useEnglish)
        {
            var policies = (plan?.OfferSchedules ?? new List<StrategyGuideOfferScheduleDefinition>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Policy))
                .Select(item => item.Policy)
                .Distinct(StringComparer.Ordinal)
                .Select(item => LocalizedPolicy(item, useEnglish))
                .ToList();
            return policies.Count == 0
                ? (useEnglish ? "Fixed lineup" : "固定阵容")
                : string.Join(" / ", policies);
        }

        private static string LocalizedPolicy(string policy, bool useEnglish)
        {
            if (policy == StrategyGuideOfferPolicies.MustIncludeAny)
            {
                return useEnglish ? "One recommended option" : "推荐中保证一个";
            }
            if (useEnglish)
            {
                return policy == StrategyGuideOfferPolicies.NaturalSeeded ? "Seeded odds" :
                    policy == StrategyGuideOfferPolicies.MustInclude ? "Guaranteed option" :
                    policy == StrategyGuideOfferPolicies.Pinned ? "Pinned offer" : policy;
            }
            return policy == StrategyGuideOfferPolicies.NaturalSeeded ? "固定种子" :
                policy == StrategyGuideOfferPolicies.MustInclude ? "保证出现" :
                policy == StrategyGuideOfferPolicies.Pinned ? "固定候选" : policy;
        }

        private static string GrowthLabel(string key, bool useEnglish)
        {
            if (string.Equals(key, "beast.lobsterGrowth", StringComparison.Ordinal))
            {
                return useEnglish ? "Tasty Lobster growth" : "美味龙虾成长";
            }
            if (string.Equals(key, "tavern.spellsCastThisGame", StringComparison.Ordinal))
            {
                return useEnglish ? "Tavern spells cast" : "本局施放酒馆法术";
            }
            if (string.Equals(key, "demon.tavernBonusAttack", StringComparison.Ordinal))
            {
                return useEnglish ? "Permanent Tavern Attack" : "酒馆永久攻击成长";
            }
            if (string.Equals(key, "demon.tavernBonusHealth", StringComparison.Ordinal))
            {
                return useEnglish ? "Permanent Tavern Health" : "酒馆永久生命成长";
            }
            return key;
        }

        private static string CompletionCondition(StrategyGuideEntryProfileDefinition profile, bool useEnglish)
        {
            var victory = profile?.Victory ?? new StrategyGuideVictoryCondition();
            var parts = new List<string>();
            if (victory.RequireFinalComposition)
            {
                parts.Add(useEnglish ? "complete the target lineup" : "完成目标阵容");
            }
            if (victory.RequireCombatWin)
            {
                parts.Add(useEnglish ? "win the next combat" : "赢下下一场战斗");
            }
            if ((profile?.GrowthQuality?.Count ?? 0) > 0)
            {
                parts.Add(useEnglish ? "meet every growth target" : "达到全部成长目标");
            }
            if (parts.Count == 0)
            {
                return useEnglish ? "Complete the scenario objective." : "完成本关目标。";
            }
            return (useEnglish ? "Finish when you " : "完成条件：") + string.Join(useEnglish ? ", " : "、", parts) + ".";
        }

        private static string LocalizedTribe(string tribe, bool useEnglish)
        {
            if (useEnglish || !ChineseTribes.TryGetValue(tribe, out var localized))
            {
                return tribe;
            }
            return localized;
        }

        private static string Localized(string chinese, string english, bool useEnglish)
        {
            return useEnglish ? FirstNonEmpty(english, chinese) : FirstNonEmpty(chinese, english);
        }

        private static string FirstNonEmpty(string primary, string fallback)
        {
            return !string.IsNullOrWhiteSpace(primary) ? primary : fallback;
        }
    }
}
