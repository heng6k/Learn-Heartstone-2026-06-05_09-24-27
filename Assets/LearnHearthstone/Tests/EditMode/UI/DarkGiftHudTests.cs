using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DarkGiftHudTests
    {
        [Test]
        public void DesktopShowsThreeGiftIconsAndOverflowWhileCompactAggregates()
        {
            AssertGiftRack(1920, 1080, 3, true, false);
            AssertGiftRack(844, 390, 0, false, true);
        }

        [Test]
        public void GiftDetailsShowCurrentStateAndTriggerTimeline()
        {
            var root = Root(1280, 720);
            try
            {
                var service = CreateGiftService(2, out _);
                Build(root, service);

                Find(root.transform, "UnityDarkGiftEffect-0").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(Find(root.transform, "UnityDarkGiftDetailsOverlay"));
                var current = AllText(root.transform, "UnityDarkGiftCurrentEffects");
                StringAssert.Contains("来源", current);
                StringAssert.Contains("第 3 回合", current);
                StringAssert.Contains("剩余次数", current);
                StringAssert.Contains("冷却", current);
                StringAssert.Contains("叠加", current);
                var history = AllText(root.transform, "UnityDarkGiftTriggerTimeline");
                StringAssert.Contains("第 4 回合", history);
                StringAssert.Contains("刷新后触发", history);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Season14NormalGiftButton_IsVisibleFocusedAndOpensPaidChoice()
        {
            var root = Root(1280, 720);
            try
            {
                var service = CreateSeason14Service();
                service.State.Round = 3;
                service.State.Phase = MatchPhase.Tavern;
                service.State.ChoiceQueue = new ChoiceQueueState();
                service.State.Player.Tavern.Gold = 10;
                Build(root, service);

                var entry = Find(root.transform, "UnityDarkGiftButton");
                Assert.IsNotNull(entry);
                Assert.IsTrue(entry.GetComponent<Button>().interactable);
                Assert.IsNotNull(entry.GetComponent<UnitySelectableFocusRing>());
                StringAssert.Contains("3", Text(root.transform, "UnityHeroEffectBadge-DarkGift-NormalEntry"));

                entry.GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(7, service.State.Player.Tavern.Gold);
                Assert.AreEqual(Season14DarkGiftSourceService.NormalEntrySourceId, service.State.ChoiceQueue.ActiveChoice.Source);
                Assert.IsNotNull(Find(root.transform, "UnityDarkGiftChoicePanel"));
                StringAssert.Contains("3 金币", Text(root.transform, "UnityDarkGiftChoiceMetadata"));
                StringAssert.Contains("黑暗之赐按钮", Text(root.transform, "UnityDarkGiftChoiceMetadata"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Season14NormalGiftButton_ShowsTierUsesAndUnlockCountdown()
        {
            var root = Root(1280, 720);
            try
            {
                var service = CreateSeason14Service();
                service.State.Round = 1;
                service.State.Phase = MatchPhase.Tavern;
                service.State.ChoiceQueue = new ChoiceQueueState();
                Build(root, service);

                var status = Text(root.transform, "UnityHeroEffectType-DarkGift-NormalEntry");
                StringAssert.Contains("2", status, "Round one should show two rounds until unlock.");
                StringAssert.Contains("3/3", status, "The button must show remaining match uses.");
                StringAssert.Contains("2", status, "The button must show the round-three tier-two offer.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LockboxEffectShowsRemainingTurnsAndLatestAcceleration()
        {
            var root = Root(1280, 720);
            try
            {
                var service = CreateGiftService(0, out _);
                service.State.DelayedObjectStates.Add(new DelayedObjectState
                {
                    InstanceId = "lockbox-ui4",
                    DefinitionRevisionId = "NEUTRAL_ROGUE_BG36_520t@36.2-preview-v1",
                    CreatedRound = 2,
                    RemainingTurns = 3,
                    Source = "越狱行动"
                });
                service.State.MechanicEvents.Add(new MechanicEventRecord
                {
                    Sequence = 8,
                    Round = 4,
                    Phase = MatchPhase.Tavern,
                    Type = "lockbox.accelerated",
                    Source = "野心勃勃的逃兵",
                    Targets = new List<string> { "lockbox-ui4" },
                    Result = "remaining=3"
                });
                Build(root, service);

                var lockbox = Find(root.transform, "UnityLockboxEffect");
                Assert.IsNotNull(lockbox);
                StringAssert.Contains("3", Text(root.transform, "UnityHeroEffectBadge-Lockbox-lockbox-ui4"));
                lockbox.GetComponent<Button>().onClick.Invoke();
                StringAssert.Contains("第 4 回合", Text(root.transform, "UnityHeroEffectTooltipStatus"));
                StringAssert.Contains("加速", Text(root.transform, "UnityHeroEffectTooltipStatus"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FishbaitActivateUsesUnifiedTargetingAndResolvesImmediatelyAfterSelection()
        {
            var root = Root(1280, 720);
            try
            {
                var service = CreateRecruitActionService(out var source, out var target);
                Build(root, service);
                Find(root.transform, "UnityCard-" + source.InstanceId).GetComponent<Button>().onClick.Invoke();

                var action = FindPrefix(root.transform, "UnityRecruitActionButton-");
                Assert.IsNotNull(action, "Selecting an Activate carrier must expose its action in the always-visible tavern action bar.");
                var label = action.GetComponentInChildren<Text>().text;
                StringAssert.Contains("发动", label);
                StringAssert.Contains("2 金币", label);
                StringAssert.Contains("1/1", label);
                StringAssert.Contains("酒馆随从", label);
                action.GetComponent<Button>().onClick.Invoke();

                StringAssert.Contains("酒馆", Text(root.transform, "UnityRecruitActionTargetHint"));
                StringAssert.Contains("不是己方战队", Text(root.transform, "UnityRecruitActionTargetHint"));
                Assert.IsNotNull(Find(root.transform, "UnityTargetingCancelButton"));
                Find(root.transform, "UnityTargetingCancelButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsNull(Find(root.transform, "UnityRecruitActionTargetPanel"));
                Assert.AreEqual(10, service.State.Player.Tavern.Gold);
                Assert.IsFalse((service.State.RecruitActionStates ?? new List<RecruitActionState>())
                    .Any(state => state != null && state.UsesThisTurn > 0));

                FindPrefix(root.transform, "UnityRecruitActionButton-").GetComponent<Button>().onClick.Invoke();
                Find(root.transform, "UnityCard-" + target.InstanceId).GetComponent<Button>().onClick.Invoke();

                Assert.IsNull(Find(root.transform, "UnityRecruitActionConfirmOverlay"));
                Assert.IsTrue(service.LastRecruitActionResult.Succeeded, service.LastRecruitActionResult.Message);
                Assert.AreEqual(8, service.State.Player.Tavern.Gold);
                Assert.AreEqual(1, service.State.RecruitActionStates.Single().UsesThisTurn);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TavernActionBar_UsesCompactBoundedButtonsWithPhysicalTouchHeight()
        {
            var root = Root(1920, 1080);
            try
            {
                var service = CreateGiftService(0, out _);
                Build(root, service);

                var bar = Find(root.transform, "UnityTavernActionBar");
                var group = bar.GetComponent<HorizontalLayoutGroup>();
                Assert.IsFalse(group.childForceExpandWidth);
                Assert.GreaterOrEqual(group.spacing, 8f);
                foreach (var button in bar.GetComponentsInChildren<Button>(true))
                {
                    var element = button.GetComponent<LayoutElement>();
                    Assert.IsNotNull(element, button.name + " must declare bounded layout dimensions.");
                    Assert.LessOrEqual(element.preferredWidth, 106f, button.name + " is visually oversized.");
                    Assert.GreaterOrEqual(element.minHeight, UnityTavernUiStyle.TouchHeight);
                    Assert.AreEqual(0f, element.flexibleWidth);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LockedActivateUsesDisabledStateAndTextReason()
        {
            var root = Root(1280, 720);
            try
            {
                var service = CreateRecruitActionService(out var source, out _);
                service.State.RecruitActionStates.Add(new RecruitActionState
                {
                    SourceInstanceId = source.InstanceId,
                    LockedReason = "需要解除封印"
                });
                Build(root, service);
                Find(root.transform, "UnityCard-" + source.InstanceId).GetComponent<Button>().onClick.Invoke();

                var actionObject = FindPrefix(root.transform, "UnityRecruitActionButton-");
                Assert.IsNotNull(actionObject, "Selected recruit-action minions must expose an Activate button.");
                var action = actionObject.GetComponent<Button>();
                Assert.IsFalse(action.interactable);
                StringAssert.Contains("不可用", action.GetComponentInChildren<Text>().text);
                StringAssert.Contains("需要解除封印", action.GetComponentInChildren<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertGiftRack(int width, int height, int expectedIndividual, bool expectsOverflow, bool expectsAggregate)
        {
            var root = Root(width, height);
            try
            {
                var service = CreateGiftService(5, out _);
                Build(root, service);

                Assert.AreEqual(expectedIndividual, CountPrefix(root.transform, "UnityDarkGiftEffect-"));
                Assert.AreEqual(expectsOverflow, Find(root.transform, "UnityDarkGiftEffectOverflow") != null);
                Assert.AreEqual(expectsAggregate, Find(root.transform, "UnityDarkGiftEffectAggregate") != null);
                if (expectsAggregate)
                {
                    StringAssert.Contains("×5", Text(root.transform, "UnityHeroEffectType-DarkGift-Aggregate"));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static MatchService CreateGiftService(int giftCount, out List<DarkGiftDefinition> gifts)
        {
            var baseline = MatchService.CreateWithDefaultCatalog(51, new InMemoryTestScenarioRepository());
            gifts = Enumerable.Range(0, Math.Max(1, giftCount)).Select(index => new DarkGiftDefinition
            {
                Id = "ui4-hud-gift-" + index,
                ResearchKey = "UI4-HUD-GIFT-R" + index,
                RevisionId = "ui4-hud-gift-" + index + "@1",
                EffectRevision = "ui4-hud-gift-" + index + ".effect@1",
                DisplayName = "暗影赐礼 " + (index + 1),
                Text = "刷新后触发。",
                StackPolicy = DarkGiftStackPolicies.Stack,
                MaxStacks = 3,
                DurationPolicy = DarkGiftDurationPolicies.Persistent,
                InitialUses = 2,
                CooldownRounds = 1,
                ImplementationStatus = DarkGiftImplementationStatus.Implemented
            }).ToList();
            var catalogs = Catalogs(baseline, baseline.Catalogs.Minions, new DarkGiftCatalog(gifts));
            var service = MatchService.CreateWithCatalogs(catalogs, 51, new InMemoryTestScenarioRepository(), darkGiftDefinitions: gifts);
            service.State.Round = 4;
            for (var index = 0; index < giftCount; index += 1)
            {
                service.State.PlayerDarkGifts.AcquiredGiftInstances.Add(new PlayerDarkGiftInstance
                {
                    InstanceId = "ui4-gift-target-" + index,
                    DefinitionRevisionId = gifts[index].RevisionId,
                    AcquiredRound = 3,
                    Source = "英雄技能",
                    StackCount = 2,
                    RemainingUses = 1,
                    Cooldown = 1,
                    Active = true
                });
            }

            if (giftCount > 0)
            {
                service.State.PlayerDarkGifts.TriggerHistory.Events.Add(new MechanicEventRecord
                {
                    Sequence = 1,
                    Round = 4,
                    Phase = MatchPhase.Tavern,
                    Type = "dark-gift.trigger.resolved",
                    Source = "英雄技能",
                    Targets = new List<string> { "ui4-gift-target-0", gifts[0].RevisionId },
                    Result = "刷新后触发"
                });
            }

            return service;
        }

        private static MatchService CreateSeason14Service()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("ui4-normal-dark-gift-entry");
            var resolved = snapshot.VersionedContent.CreateResolver()
                .Resolve(GameVersionIds.Season14Preview, snapshot);
            return MatchService.CreateWithResolvedVersion(
                resolved,
                73,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableQuests = false,
                    EnableTrinkets = true,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                });
        }

        private static MatchService CreateRecruitActionService(out MinionInstance source, out MinionInstance target)
        {
            var baseline = MatchService.CreateWithDefaultCatalog(61, new InMemoryTestScenarioRepository());
            var sourceDefinition = Definition("UI4_SOURCE", "诱饵猎手");
            sourceDefinition.RecruitActions = new List<RecruitActionDefinition>
            {
                new RecruitActionDefinition
                {
                    ActionId = "activate:ui4-fishbait",
                    ResolverId = "ui4.fishbait@1",
                    CostSpec = new RecruitActionCostSpec { Gold = 2 },
                    TargetSpec = RecruitActionTargetSpec.TavernMinion,
                    UsesPerTurn = 1,
                    AllowedPhase = MatchPhase.Tavern
                }
            };
            var targetDefinition = Definition("UI4_TARGET", "鱼饵");
            var minionCatalog = new MinionCatalog(baseline.Catalogs.Minions.All.Concat(new[] { sourceDefinition, targetDefinition }));
            var catalogs = Catalogs(baseline, minionCatalog, baseline.Catalogs.DarkGifts);
            var resolvers = new RecruitActionResolverRegistry();
            resolvers.Register("ui4.fishbait@1", _ => RecruitActionResolution.Success());
            var service = MatchService.CreateWithCatalogs(
                catalogs,
                61,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { EnableQuests = false, EnableTrinkets = false },
                recruitActionResolvers: resolvers);
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Shop.Clear();
            service.State.Player.Tavern.Gold = 10;
            source = MinionFactory.Create(sourceDefinition, BoardSide.Player, "ui4-source", false, PoolSource.Copy, 0);
            target = MinionFactory.Create(targetDefinition, BoardSide.Player, "ui4-target", false, PoolSource.Copy, 0);
            service.State.Player.Board.Add(source);
            service.State.Player.Tavern.Shop.Add(target);
            return service;
        }

        private static MinionDefinition Definition(string id, string name)
        {
            return new MinionDefinition
            {
                Id = id,
                CardId = id,
                RevisionId = id + "@1",
                EffectRevision = id + ".effect@1",
                Name = name,
                Text = name,
                TavernTier = 2,
                BaseAttack = 2,
                BaseHealth = 2,
                InPool = false,
                Tribes = new List<Tribe> { Tribe.None }
            };
        }

        private static GameCatalogSet Catalogs(MatchService baseline, MinionCatalog minions, DarkGiftCatalog gifts)
        {
            return new GameCatalogSet(
                minions,
                baseline.Catalogs.Spells,
                baseline.Catalogs.Heroes,
                baseline.Catalogs.Trinkets,
                baseline.Catalogs.Quests,
                baseline.Catalogs.TimewarpedTavern,
                baseline.Catalogs.Anomalies,
                baseline.Catalogs.DarkmoonPrizes,
                gifts);
        }

        private static GameObject Root(int width, int height)
        {
            var root = new GameObject("Root", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            return root;
        }

        private static void Build(GameObject root, MatchService service)
        {
            new UnityTavernTrainerView(root.transform, service, new LocalAdvisorService(), () => { }).Build();
        }

        private static string AllText(Transform root, string sectionName)
        {
            var section = Find(root, sectionName);
            Assert.IsNotNull(section, "Missing section: " + sectionName);
            return string.Join("\n", section.GetComponentsInChildren<Text>(true).Select(item => item.text));
        }

        private static string Text(Transform root, string name)
        {
            var target = Find(root, name);
            Assert.IsNotNull(target, "Missing text object: " + name);
            return target.GetComponent<Text>().text;
        }

        private static int CountPrefix(Transform root, string prefix)
        {
            var count = root.name.StartsWith(prefix, StringComparison.Ordinal) ? 1 : 0;
            for (var index = 0; index < root.childCount; index += 1)
            {
                count += CountPrefix(root.GetChild(index), prefix);
            }

            return count;
        }

        private static Transform FindPrefix(Transform root, string prefix)
        {
            if (root.name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var found = FindPrefix(root.GetChild(index), prefix);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var found = Find(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
