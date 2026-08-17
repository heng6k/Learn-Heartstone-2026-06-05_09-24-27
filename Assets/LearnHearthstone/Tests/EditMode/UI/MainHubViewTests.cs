using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed class MainHubViewTests
    {
        private const string CaptureDirectory = ".planning/tavern-ui-screenshot-requirements/captures";

        [Test]
        public void Build_CreatesExactlyTwoSimpleGameEntries()
        {
            var rootObject = Root(1366, 768);
            try
            {
                var trainerOpened = false;
                var guidesOpened = false;

                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => trainerOpened = true,
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    openStrategyGuides: () => guidesOpened = true).Build();

                Assert.AreEqual(1, FindChildren(rootObject.transform, "MainHubEntryDeck").Count);
                var training = FindChildren(rootObject.transform, "MainHubPrimaryStartButton").Single().GetComponent<Button>();
                var guides = FindChildren(rootObject.transform, "MainHubStrategyGuideButton").Single().GetComponent<Button>();
                Assert.IsTrue(training.interactable);
                Assert.IsTrue(guides.interactable);
                StringAssert.Contains(
                    "模拟对局",
                    FindChildren(training.transform, "MainHubPrimaryStartButtonTitle").Single().GetComponent<Text>().text);
                StringAssert.Contains(
                    "一图流训练",
                    FindChildren(guides.transform, "MainHubStrategyGuideButtonTitle").Single().GetComponent<Text>().text);

                training.onClick.Invoke();
                guides.onClick.Invoke();

                Assert.IsTrue(trainerOpened);
                Assert.IsTrue(guidesOpened);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_DisablesGuideEntryWhenRouteIsUnavailable()
        {
            var rootObject = Root(1366, 768);
            try
            {
                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                var entry = FindChildren(rootObject.transform, "MainHubStrategyGuideButton").Single().GetComponent<Button>();
                Assert.IsFalse(entry.interactable);
                StringAssert.Contains(
                    "暂不可用",
                    FindChildren(entry.transform, "MainHubStrategyGuideButtonActionLabel").Single().GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_KeepsCompactVersionEntryWithoutRestoringLongConfigurationPanels()
        {
            var rootObject = Root(1366, 768);
            try
            {
                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    currentGameVersion: CurrentVersion(),
                    openVersionCenter: () => { },
                    openStrategyGuides: () => { }).Build();

                StringAssert.Contains(
                    "36.2",
                    FindChildren(rootObject.transform, "MainHubVersionContext").Single().GetComponent<Text>().text);
                Assert.AreEqual(1, FindChildren(rootObject.transform, "MainHubPrimaryStartButton").Count);
                Assert.AreEqual(1, FindChildren(rootObject.transform, "MainHubStrategyGuideButton").Count);
                Assert.AreEqual(1, FindChildren(rootObject.transform, "MainHubVersionCenterButton").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "MainHubGameVersionStrip").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "MainHubRecommendedSetup").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "MainHubPrimaryPath").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "MainHubSecondaryRoutes").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "ModuleGrid").Count);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_AppliesStarLanternThemeAndReadableControls()
        {
            var rootObject = Root(1366, 768);
            try
            {
                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                Assert.AreEqual(
                    UnityTavernUiStyle.BackWall,
                    FindChildren(rootObject.transform, "MainHub").Single().GetComponent<Image>().color);
                var header = FindChildren(rootObject.transform, "MainHubHeader").Single();
                var rail = FindChildren(header, "MainHubStarLanternRail").Single();
                var facet = FindChildren(header, "MainHubStarLanternFacet").Single();
                Assert.IsFalse(rail.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(facet.GetComponent<Image>().raycastTarget);
                Assert.AreEqual(0f, Mathf.DeltaAngle(45f, facet.localEulerAngles.z), 0.001f);
                Assert.AreEqual(1, FindChildren(rootObject.transform, "MainHubPrimaryStartButtonStarLanternRail").Count);
                Assert.AreEqual(1, FindChildren(rootObject.transform, "MainHubStrategyGuideButtonStarLanternRail").Count);

                var language = FindChildren(rootObject.transform, "MainHubLanguageChineseButton").Single();
                Assert.GreaterOrEqual(language.GetComponent<LayoutElement>().minHeight, UnityTavernUiStyle.TouchHeight);
                StringAssert.Contains("当前", language.GetComponentInChildren<Text>().text);
                Assert.IsTrue(rootObject.GetComponentsInChildren<Text>(true).All(text => text.fontSize >= 14));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_UsesLocalCardArtworkAndAReadableActionPlateForBothModes()
        {
            var rootObject = Root(1280, 720);
            try
            {
                var layout = UnityTavernLayoutContext.ForSize(1280f, 720f);
                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    layout,
                    openStrategyGuides: () => { }).Build();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    FindChildren(rootObject.transform, "MainHub").Single().GetComponent<RectTransform>());

                var atmosphere = FindChildren(rootObject.transform, "MainHubAtmosphere").Single();
                Assert.IsNotNull(atmosphere.GetComponent<Image>().sprite);
                Assert.IsFalse(atmosphere.GetComponent<Image>().raycastTarget);
                Assert.IsTrue(atmosphere.GetComponent<LayoutElement>().ignoreLayout);

                foreach (var prefix in new[] { "MainHubPrimaryStartButton", "MainHubStrategyGuideButton" })
                {
                    var art = FindChildren(rootObject.transform, prefix + "Artwork").Single();
                    var cardImages = art.GetComponentsInChildren<Image>(true)
                        .Where(image => image.name.StartsWith(prefix + "CardArt-", StringComparison.Ordinal))
                        .ToList();
                    Assert.AreEqual(2, cardImages.Count, prefix);
                    Assert.IsTrue(cardImages.All(image => image.sprite != null), prefix);
                    Assert.IsTrue(cardImages.All(image => !image.raycastTarget), prefix);

                    var badge = FindChildren(rootObject.transform, prefix + "ModeBadge").Single().GetComponent<Text>();
                    Assert.IsFalse(string.IsNullOrWhiteSpace(badge.text), prefix);
                    var actionPlate = FindChildren(rootObject.transform, prefix + "ActionPlate").Single();
                    Assert.GreaterOrEqual(
                        actionPlate.GetComponent<LayoutElement>().minHeight,
                        UiFactory.MinimumButtonHeight,
                        prefix);
                    var actionPlatePhysicalHeight =
                        actionPlate.GetComponent<RectTransform>().rect.height * layout.CanvasScaleFactor;
                    Assert.That(
                        actionPlatePhysicalHeight,
                        Is.InRange(UiFactory.MinimumButtonHeight - 0.5f, UiFactory.MinimumButtonHeight + 1f),
                        prefix + " physical action height");
                    var actionLabel = FindChildren(rootObject.transform, prefix + "ActionLabel").Single().GetComponent<Text>();
                    Assert.IsFalse(string.IsNullOrWhiteSpace(actionLabel.text), prefix);
                    Assert.Greater(actionLabel.rectTransform.rect.width, 80f, prefix);
                    Assert.GreaterOrEqual(actionLabel.rectTransform.rect.height, UiFactory.MinimumButtonHeight, prefix);
                    Assert.Greater(actionLabel.color.a, 0.9f, prefix);
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_ShowsCompactVersionContextAsAWorkingFooterEntryWithoutAddingAThirdCard()
        {
            var rootObject = Root(1366, 768);
            try
            {
                var versionCenterOpened = false;

                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    currentGameVersion: CurrentVersion(),
                    openVersionCenter: () => versionCenterOpened = true).Build();

                var context = FindChildren(rootObject.transform, "MainHubVersionContext").Single().GetComponent<Text>().text;
                StringAssert.Contains("36.2", context);
                StringAssert.Contains("查看版本与机制", context);
                Assert.AreEqual(1, FindChildren(rootObject.transform, "MainHubVersionCenterButton").Count);
                FindChildren(rootObject.transform, "MainHubVersionCenterButton").Single().GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(versionCenterOpened);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [TestCase(740, 360)]
        [TestCase(844, 390)]
        [TestCase(932, 430)]
        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(2560, 1080)]
        public void Build_ReferenceResolutionsKeepLandscapeHierarchyAndTouchTargets(int width, int height)
        {
            var rootObject = Root(width, height);
            try
            {
                var layout = UnityTavernLayoutContext.ForSize(width, height);
                rootObject.GetComponent<RectTransform>().sizeDelta = new Vector2(
                    width / layout.CanvasScaleFactor,
                    height / layout.CanvasScaleFactor);
                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    layout,
                    currentGameVersion: CurrentVersion(),
                    openVersionCenter: () => { },
                    openStrategyGuides: () => { }).Build();

                var hub = FindChildren(rootObject.transform, "MainHub").Single().GetComponent<RectTransform>();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(hub);

                var entryDeck = FindChildren(rootObject.transform, "MainHubEntryDeck").Single();
                Assert.IsNotNull(entryDeck.GetComponent<HorizontalLayoutGroup>());
                var header = FindChildren(rootObject.transform, "MainHubHeader").Single().GetComponent<RectTransform>();
                var expectedHeaderPhysicalHeight = layout.IsCompact ? 64f : 76f;
                if (layout.IsCompact)
                {
                    Assert.LessOrEqual(
                        header.rect.height * layout.CanvasScaleFactor,
                        66f,
                        "The compact header must not absorb flexible height.");
                    Assert.GreaterOrEqual(
                        header.rect.height * layout.CanvasScaleFactor,
                        UiFactory.MinimumButtonHeight - 0.5f,
                        "The compact header must remain physically readable after layout compression.");
                }
                else
                {
                    Assert.AreEqual(
                        expectedHeaderPhysicalHeight,
                        header.rect.height * layout.CanvasScaleFactor,
                        0.5f,
                        "The fixed header must not absorb flexible desktop height.");
                }

                var training = FindChildren(rootObject.transform, "MainHubPrimaryStartButton").Single().GetComponent<RectTransform>();
                var guides = FindChildren(rootObject.transform, "MainHubStrategyGuideButton").Single().GetComponent<RectTransform>();
                var minimumEntryWidth = layout.IsCompact ? 160f : 220f;
                Assert.GreaterOrEqual(training.rect.width * layout.CanvasScaleFactor, minimumEntryWidth);
                Assert.GreaterOrEqual(guides.rect.width * layout.CanvasScaleFactor, minimumEntryWidth);
                var minimumEntryHeight = layout.IsCompact ? UiFactory.MinimumButtonHeight : 120f;
                Assert.GreaterOrEqual(training.rect.height * layout.CanvasScaleFactor, minimumEntryHeight);
                Assert.GreaterOrEqual(guides.rect.height * layout.CanvasScaleFactor, minimumEntryHeight);
                Assert.AreEqual(1, FindChildren(rootObject.transform, "MainHubVersionCenterButton").Count);
                Assert.AreEqual(0, FindChildren(rootObject.transform, "MainHubGameVersionStrip").Count);

                var footerObject = FindChildren(rootObject.transform, "MainHubVersionCenterButton").Single();
                var footer = footerObject.GetComponent<LayoutElement>();
                Assert.AreEqual(0f, footer.flexibleHeight);
                Assert.GreaterOrEqual(
                    footerObject.GetComponent<RectTransform>().rect.height * layout.CanvasScaleFactor,
                    38f,
                    "Version context must retain visible height at every target resolution.");
                Assert.GreaterOrEqual(
                    FindChildren(rootObject.transform, "MainHubVersionContext").Single().GetComponent<RectTransform>().rect.height * layout.CanvasScaleFactor,
                    16f,
                    "Version text must retain a readable line box at every target resolution.");
                StringAssert.Contains(
                    "36.2",
                    FindChildren(rootObject.transform, "MainHubVersionContext").Single().GetComponent<Text>().text);

                foreach (var button in rootObject.GetComponentsInChildren<Button>(true))
                {
                    Assert.IsNotNull(button.GetComponent<UnitySelectableFocusRing>(), button.name);
                    var rect = button.GetComponent<RectTransform>().rect;
                    Assert.GreaterOrEqual(
                        rect.width * layout.CanvasScaleFactor,
                        UiFactory.MinimumButtonHeight - 0.5f,
                        button.name + " physical width");
                    Assert.GreaterOrEqual(
                        rect.height * layout.CanvasScaleFactor,
                        UiFactory.MinimumButtonHeight - 0.5f,
                        button.name + " physical height");
                }
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_CapturesInitialHierarchyAtTargetSizes()
        {
            Capture(1280, 720, "phase20-main-hub-1280x720.png");
            Capture(1920, 1080, "phase20-main-hub-1920x1080.png");
            Capture(2560, 1080, "phase20-main-hub-2560x1080.png");
            Capture(844, 390, "phase20-main-hub-844x390.png");
        }

        [Test]
        public void PackagedUiFont_CoversChineseAndLatinGlyphs()
        {
            var font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");

            Assert.IsNotNull(font);
            Assert.IsTrue(font.HasCharacter('中'));
            Assert.IsTrue(font.HasCharacter('A'));
        }

        [Test]
        public void UiFactory_DefaultFontSupportsChineseAndLatin()
        {
            UiFactory.SetFontOverride(null);
            var rootObject = Root(320, 180);
            try
            {
                var label = UiFactory.Label("DefaultFontLabel", rootObject.transform, "酒馆 Tavern");

                Assert.IsNotNull(label.font);
                Assert.IsTrue(label.font.dynamic);
                Assert.IsTrue(label.font.HasCharacter('中'));
                Assert.IsTrue(label.font.HasCharacter('A'));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void UiFactory_PartitionsFontAtlasesBySizeAndStyle()
        {
            UiFactory.SetFontOverride(null);
            var rootObject = Root(320, 180);
            try
            {
                var desktop = UnityTavernLayoutContext.ForSize(1920f, 1080f);
                var normal14 = UiFactory.Label("Normal14", rootObject.transform, "酒馆", 14, FontStyle.Normal, desktop);
                var normal14Again = UiFactory.Label("Normal14Again", rootObject.transform, "随从", 14, FontStyle.Normal, desktop);
                var bold14 = UiFactory.Label("Bold14", rootObject.transform, "战斗", 14, FontStyle.Bold, desktop);
                var normal16 = UiFactory.Label("Normal16", rootObject.transform, "回合", 16, FontStyle.Normal, desktop);
                var normal18 = UiFactory.Label("Normal18", rootObject.transform, "准备", 18, FontStyle.Normal, desktop);

                Assert.AreSame(normal14.font, normal14Again.font);
                Assert.AreNotSame(normal14.font, bold14.font);
                Assert.AreNotSame(normal14.font, normal16.font);
                Assert.AreNotSame(normal14.font, normal18.font);
                normal14.font.RequestCharactersInTexture(normal14.text, normal14.fontSize, normal14.fontStyle);
                normal18.font.RequestCharactersInTexture(normal18.text, normal18.fontSize, normal18.fontStyle);
                Assert.AreNotSame(normal14.font.material.mainTexture, normal18.font.material.mainTexture);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void UiFactory_RecreatesCachedFontAfterAtlasTextureIsUnloaded()
        {
            UiFactory.SetFontOverride(null);
            var rootObject = Root(320, 180);
            try
            {
                var first = UiFactory.Label("AtlasBeforeUnload", rootObject.transform, "酒馆", 23, FontStyle.Italic);
                var firstFont = first.font;
                var firstAtlas = firstFont.material.mainTexture;
                Assert.IsNotNull(firstAtlas);

                Object.DestroyImmediate(firstAtlas);
                var rebuilt = UiFactory.Label("AtlasAfterUnload", rootObject.transform, "随从", 23, FontStyle.Italic);

                Assert.AreNotSame(firstFont, rebuilt.font);
                Assert.IsNotNull(rebuilt.font.material);
                Assert.IsNotNull(rebuilt.font.material.mainTexture);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static GameVersionSummaryViewModel CurrentVersion()
        {
            return new GameVersionSummaryViewModel(new GameVersionDefinition(
                GameVersionIds.Season14Preview,
                "36.2 预览",
                new DateTime(2026, 8, 4, 17, 0, 0, DateTimeKind.Utc),
                GameVersionOfficialStatus.Announced,
                GameVersionImplementationStatus.Partial,
                RulesetIds.Season14Preview,
                ContentSetIds.Season14Preview,
                "第 14 赛季：新英雄、新卡和黑暗之赐。"));
        }

        private static GameObject Root(int width, int height)
        {
            var root = new GameObject("Root", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            return root;
        }

        private static void Capture(int width, int height, string fileName)
        {
            Directory.CreateDirectory(CaptureDirectory);
            var path = Path.Combine(CaptureDirectory, fileName);
            var cameraObject = new GameObject("MainHubCaptureCamera", typeof(Camera));
            var canvasObject = new GameObject(
                "MainHubCaptureCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousActive = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;

                var layout = UnityTavernLayoutContext.ForSize(width, height);
                var canvas = canvasObject.GetComponent<Canvas>();
                LearnHearthstone.Presentation.LearnHearthstoneBootstrap.ConfigureCanvas(canvas, layout);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;

                new MainHubView(
                    canvasObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    layout,
                    currentGameVersion: CurrentVersion(),
                    openVersionCenter: () => { },
                    openStrategyGuides: () => { }).Build();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvasObject.GetComponent<RectTransform>());
                camera.Render();

                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Assert.Greater(new FileInfo(path).Length, 0, path);
            }
            finally
            {
                RenderTexture.active = previousActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }

                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static List<Transform> FindChildren(Transform root, string name)
        {
            var results = new List<Transform>();
            Collect(root, name, results);
            return results;
        }

        private static void Collect(Transform root, string name, List<Transform> results)
        {
            if (root.name == name)
            {
                results.Add(root);
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                Collect(root.GetChild(index), name, results);
            }
        }
    }
}
