using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class GameVersionCenterViewTests
    {
        [Test]
        public void Build_DesktopShowsVersionListStatusTabsAndDifferenceLegend()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                new GameVersionCenterView(
                    rootObject.transform,
                    CreateContent(),
                    GameVersionIds.Season14Preview,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1280f, 720f)).Build();

                Assert.AreEqual(1, FindChildren(rootObject.transform, "GameVersionCenterDesktopBody").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "GameVersionCenterCompactListPage").Count);
                Assert.AreEqual(
                    2,
                    rootObject.GetComponentsInChildren<Button>(true).Count(button => button.name.StartsWith("GameVersionCenterVersionButton-", StringComparison.Ordinal)));
                CollectionAssert.IsSubsetOf(
                    new[]
                    {
                        "GameVersionCenterTabOverview",
                        "GameVersionCenterTabHeroes",
                        "GameVersionCenterTabCards",
                        "GameVersionCenterTabMechanics",
                        "GameVersionCenterTabCompare"
                    },
                    rootObject.GetComponentsInChildren<Button>(true).Select(button => button.name).ToArray());

                StringAssert.Contains("已公布", TextOf(rootObject.transform, "GameVersionCenterOfficialStatus"));
                StringAssert.Contains("部分支持", TextOf(rootObject.transform, "GameVersionCenterImplementationStatus"));
                var legend = TextOf(rootObject.transform, "GameVersionCenterDifferenceLegend");
                StringAssert.Contains("+ 新增", legend);
                StringAssert.Contains("~ 调整", legend);
                StringAssert.Contains("- 移除", legend);
                StringAssert.Contains("↩ 回归", legend);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CompactUsesFullScreenTwoLevelNavigation()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var view = new GameVersionCenterView(
                    rootObject.transform,
                    CreateContent(),
                    GameVersionIds.Season14Preview,
                    () => { },
                    UnityTavernLayoutContext.ForSize(844f, 390f));
                view.Build();

                Assert.AreEqual(1, FindChildren(rootObject.transform, "GameVersionCenterCompactDetailPage").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "GameVersionCenterDesktopBody").Count);

                FindChildren(rootObject.transform, "GameVersionCenterCompactListButton").Single().GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(1, FindChildren(rootObject.transform, "GameVersionCenterCompactListPage").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "GameVersionCenterCompactDetailPage").Count);

                FindChildren(rootObject.transform, "GameVersionCenterVersionButton-" + GameVersionIds.LegacyCompositeSandbox).Single().GetComponent<Button>().onClick.Invoke();

                Assert.AreEqual(1, FindChildren(rootObject.transform, "GameVersionCenterCompactDetailPage").Count);
                StringAssert.Contains("综合沙盒", TextOf(rootObject.transform, "GameVersionCenterVersionName"));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Compare_UsesTextAndSymbolsForEveryDifferenceKind()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                new GameVersionCenterView(
                    rootObject.transform,
                    CreateContent(),
                    GameVersionIds.Season14Preview,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1280f, 720f)).Build();

                FindChildren(rootObject.transform, "GameVersionCenterTabCompare").Single().GetComponent<Button>().onClick.Invoke();

                var text = string.Join("\n", rootObject.GetComponentsInChildren<Text>(true).Select(item => item.text));
                StringAssert.Contains("+ 新增 · HERO_NEW", text);
                StringAssert.Contains("~ 调整 · MINION_CHANGE", text);
                StringAssert.Contains("- 移除 · SPELL_REMOVE", text);
                StringAssert.Contains("↩ 回归 · MINION_RETURN", text);
                StringAssert.Contains("黑暗之赐", text);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_ButtonsAndTextRespectPhysicalMinimums()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var layout = UnityTavernLayoutContext.ForSize(844f, 390f);
                new GameVersionCenterView(
                    rootObject.transform,
                    CreateContent(),
                    GameVersionIds.Season14Preview,
                    () => { },
                    layout).Build();

                var minimumTouch = layout.CanvasUnitsForPhysicalPixels(48f);
                foreach (var button in rootObject.GetComponentsInChildren<Button>(true))
                {
                    Assert.GreaterOrEqual(button.GetComponent<LayoutElement>().minHeight, minimumTouch - 0.01f, button.name);
                    Assert.IsNotNull(button.GetComponent<UnitySelectableFocusRing>(), button.name);
                }

                var minimumText = Mathf.CeilToInt(layout.CanvasUnitsForPhysicalPixels(14f));
                Assert.IsTrue(rootObject.GetComponentsInChildren<Text>(true).All(text => text.fontSize >= minimumText));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BackButton_InvokesHubNavigation()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var returned = false;
                new GameVersionCenterView(
                    rootObject.transform,
                    CreateContent(),
                    GameVersionIds.Season14Preview,
                    () => returned = true,
                    UnityTavernLayoutContext.ForSize(1280f, 720f)).Build();

                FindChildren(rootObject.transform, "GameVersionCenterBackButton").Single().GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(returned);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_EmbeddedCatalogProjectsActualPreviewCountsAndDifferenceKinds()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("ui2-test");
                new GameVersionCenterView(
                    rootObject.transform,
                    snapshot.VersionedContent,
                    GameVersionIds.Season14Preview,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1280f, 720f)).Build();

                StringAssert.Contains("英雄 8 项", TextOf(rootObject.transform, "GameVersionCenterOverviewContentSummary"));
                StringAssert.Contains("卡牌 167 项", TextOf(rootObject.transform, "GameVersionCenterOverviewContentSummary"));
                StringAssert.Contains("机制 4 项", TextOf(rootObject.transform, "GameVersionCenterOverviewContentSummary"));
                StringAssert.Contains("原子卡池成员：875", TextOf(rootObject.transform, "GameVersionCenterOverviewPoolSummary"));
                StringAssert.Contains("第 14 赛季：黑暗之赐", TextOf(rootObject.transform, "GameVersionCenterOverviewSummary"));

                FindChildren(rootObject.transform, "GameVersionCenterTabCompare").Single().GetComponent<Button>().onClick.Invoke();

                var text = string.Join("\n", rootObject.GetComponentsInChildren<Text>(true).Select(item => item.text));
                StringAssert.Contains("+ 新增", text);
                StringAssert.Contains("~ 调整", text);
                StringAssert.Contains("- 移除", text);
                StringAssert.Contains("↩ 回归", text);
                StringAssert.Contains("黑暗之赐", text);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static VersionedContentCatalog CreateContent()
        {
            var legacy = new GameVersionDefinition(
                GameVersionIds.LegacyCompositeSandbox,
                "综合沙盒（旧行为）",
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                GameVersionOfficialStatus.Unofficial,
                GameVersionImplementationStatus.Verified,
                RulesetIds.LegacyCompositeSandbox,
                ContentSetIds.LegacyCompositeSandbox,
                "保留当前综合训练器行为。");
            var preview = new GameVersionDefinition(
                GameVersionIds.Season14Preview,
                "36.2 预览",
                new DateTime(2026, 8, 4, 17, 0, 0, DateTimeKind.Utc),
                GameVersionOfficialStatus.Announced,
                GameVersionImplementationStatus.Partial,
                RulesetIds.Season14Preview,
                ContentSetIds.Season14Preview,
                "第 14 赛季预览：新英雄、新卡和黑暗之赐。");
            var revisions = new[]
            {
                Revision(EntityKind.Hero, "HERO_NEW", "hero-new", preview.Id),
                Revision(EntityKind.Minion, "MINION_CHANGE", "minion-change", preview.Id),
                Revision(EntityKind.Minion, "MINION_RETURN", "minion-return", preview.Id, "pool-action:return"),
                Revision(EntityKind.TavernSpell, "SPELL_REMOVE", "spell-remove", preview.Id, "pool-action:remove")
            };

            return new VersionedContentCatalog(
                new GameVersionCatalog(new[] { legacy, preview }),
                new[]
                {
                    new RulesetDefinition(RulesetIds.LegacyCompositeSandbox, 1),
                    new RulesetDefinition(RulesetIds.Season14Preview, 1, mechanicProfiles: new[] { DarkGiftProfiles.Season14PreviewId })
                },
                new[]
                {
                    new ContentSetDefinition(ContentSetIds.LegacyCompositeSandbox),
                    new ContentSetDefinition(
                        ContentSetIds.Season14Preview,
                        heroRevisionIds: new[] { revisions[0].RevisionId },
                        minionRevisionIds: new[] { revisions[1].RevisionId, revisions[2].RevisionId },
                        tavernSpellRevisionIds: new[] { revisions[3].RevisionId })
                },
                revisions);
        }

        private static EntityRevisionDefinition Revision(EntityKind kind, string stableId, string revisionId, string versionId, params string[] tags)
        {
            return new EntityRevisionDefinition(kind, stableId, revisionId, revisionId + "-effect", versionId, tags: tags);
        }

        private static string TextOf(Transform root, string name)
        {
            return FindChildren(root, name).Single().GetComponent<Text>().text;
        }

        private static List<Transform> FindChildren(Transform root, string name)
        {
            var results = new List<Transform>();
            Collect(root, item => item.name == name, results);
            return results;
        }

        private static void Collect(Transform root, Func<Transform, bool> predicate, List<Transform> results)
        {
            if (predicate(root))
            {
                results.Add(root);
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                Collect(root.GetChild(index), predicate, results);
            }
        }
    }
}
