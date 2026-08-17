using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideShareCardUiTests
    {
        private const string Phase21AcceptanceDirectory = ".planning/phase21-profile-one-sheet-acceptance";

        [Test]
        public void SelectionOpensIndependentBeginnerAndHardOneSheetsPerGuide()
        {
            var root = new GameObject("StrategyGuideSharePreviewRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.Guides[0];
                var beginner = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.GuidedDiscover);
                var hard = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.OpenBuild);

                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: version).Build();

                var previewButtons = root.GetComponentsInChildren<Button>(true)
                    .Where(item => item.name.StartsWith("StrategyGuideSharePreviewButton-", StringComparison.Ordinal))
                    .ToList();
                Assert.AreEqual(catalog.Guides.Count * 2, previewButtons.Count);
                foreach (var item in catalog.Guides)
                {
                    var beginnerProfile = item.EntryProfiles.Single(profile =>
                        profile.Difficulty == StrategyGuideDifficulties.GuidedDiscover);
                    var hardProfile = item.EntryProfiles.Single(profile =>
                        profile.Difficulty == StrategyGuideDifficulties.OpenBuild);
                    Assert.AreEqual(1, Find(root.transform,
                        "StrategyGuideSharePreviewButton-" + item.GuideId + "-" + beginnerProfile.ProfileId).Count);
                    Assert.AreEqual(1, Find(root.transform,
                        "StrategyGuideSharePreviewButton-" + item.GuideId + "-" + hardProfile.ProfileId).Count);
                    Assert.AreEqual(0, Find(root.transform,
                        "StrategyGuideSharePreviewButton-" + item.GuideId + "-showcase").Count);
                }

                Find(root.transform,
                        "StrategyGuideSharePreviewButton-" + guide.GuideId + "-" + beginner.ProfileId)
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();

                var overlay = Find(root.transform, "StrategyGuideShareOverlay").Single();
                Assert.AreEqual(
                    guide.Title + " · " + beginner.Title,
                    Find(overlay, "StrategyGuideShareTitle").Single().GetComponent<Text>().text);
                StringAssert.Contains(" · " + beginner.ProfileId + " · ",
                    Find(overlay, "StrategyGuideShareHash").Single().GetComponent<Text>().text);
                Assert.AreEqual(1, Find(overlay, "StrategyGuideShareStartingState").Count);
                Assert.AreEqual(3, FindPrefix(overlay, "StrategyGuideShareZone-").Count);
                Assert.AreEqual(1, Find(overlay, "StrategyGuideShareLearningGoal").Count);
                Assert.AreEqual(1, Find(overlay, "StrategyGuideShareKeyDecisions").Count);
                Assert.AreEqual(beginner.KeyDecisions.Count, FindPrefix(overlay, "StrategyGuideShareDecision-").Count);
                Assert.AreEqual(1, Find(overlay, "StrategyGuideShareShapingTimeline").Count);
                Assert.AreEqual(beginner.ShapingSpellCardIds.Count,
                    FindPrefix(overlay, "StrategyGuideShareShapingTurn-").Count);
                Assert.AreEqual(beginner.ShapingSpellCardIds.Count,
                    FindPrefix(overlay, "StrategyGuideShareShapingImage-")
                        .Count(item => item.GetComponent<Image>() != null));
                Assert.AreEqual(1, Find(overlay, "StrategyGuideShareCompletion").Count);
                Assert.AreEqual(0, FindPrefix(overlay, "StrategyGuideShareFinalCard-").Count);
                Assert.AreEqual(0, FindPrefix(overlay, "StrategyGuideShareEntry-").Count);
                Assert.AreEqual(0, FindPrefix(overlay, "StrategyGuideShareEnemy").Count);

                Find(overlay, "StrategyGuideShareCloseButton").Single().GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(0, Find(root.transform, "StrategyGuideShareOverlay").Count);

                Find(root.transform,
                        "StrategyGuideSharePreviewButton-" + guide.GuideId + "-" + hard.ProfileId)
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();
                overlay = Find(root.transform, "StrategyGuideShareOverlay").Single();
                Assert.AreEqual(
                    guide.Title + " · " + hard.Title,
                    Find(overlay, "StrategyGuideShareTitle").Single().GetComponent<Text>().text);
                StringAssert.Contains(" · " + hard.ProfileId + " · ",
                    Find(overlay, "StrategyGuideShareHash").Single().GetComponent<Text>().text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SharePreviewKeepsReadableTextTouchTargetsAndProgressiveDisclosure()
        {
            var root = new GameObject("StrategyGuideShareAccessibilityRoot", typeof(RectTransform));
            try
            {
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.Guides[0];
                var beginner = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.GuidedDiscover);

                new StrategyGuideSelectionView(
                    root.transform,
                    catalog,
                    snapshot.ForLanguage(false),
                    GameVersionIds.Season14Preview,
                    (_, __) => { },
                    () => { },
                    layoutContext: UnityTavernLayoutContext.ForSize(1280f, 720f),
                    resolvedVersion: version).Build();
                Find(root.transform,
                        "StrategyGuideSharePreviewButton-" + guide.GuideId + "-" + beginner.ProfileId)
                    .Single()
                    .GetComponent<Button>()
                    .onClick.Invoke();

                var overlay = Find(root.transform, "StrategyGuideShareOverlay").Single();
                Assert.IsTrue(overlay.GetComponentsInChildren<Text>(true).All(item => item.fontSize >= 14));
                Assert.IsTrue(overlay.GetComponentsInChildren<Button>(true).All(item =>
                    item.GetComponent<LayoutElement>() != null &&
                    item.GetComponent<LayoutElement>().minHeight >= UnityTavernUiStyle.TouchHeight));
                Assert.AreEqual(1, Find(overlay, "StrategyGuideShareLearningGoalValue").Count);
                Assert.AreEqual(1, Find(overlay, "StrategyGuideShareGrowthTargets").Count);
                Assert.AreEqual(1, Find(overlay, "StrategyGuideShareCompletionCondition").Count);
                StringAssert.Contains(
                    "实际游戏以正常概率为准",
                    Find(overlay, "StrategyGuideShareProbability").Single().GetComponent<Text>().text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShortLandscapeSelectionKeepsEveryOneSheetAndStartActionReachable()
        {
            foreach (var size in new[]
                     {
                         new Vector2(740f, 360f),
                         new Vector2(844f, 390f),
                         new Vector2(932f, 430f)
                     })
            {
                var root = new GameObject("StrategyGuideShortShareActionsRoot", typeof(RectTransform));
                try
                {
                    var layout = UnityTavernLayoutContext.ForSize(size.x, size.y);
                    var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                    var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                    var catalog = StrategyGuideCatalogLoader.LoadFromResources();

                    new StrategyGuideSelectionView(
                        root.transform,
                        catalog,
                        snapshot.ForLanguage(false),
                        GameVersionIds.Season14Preview,
                        (_, __) => { },
                        () => { },
                        layoutContext: layout,
                        resolvedVersion: version).Build();

                    var host = Find(root.transform, "StrategyGuideMobileActionHost").Single();
                    Assert.AreEqual(58f,
                        host.GetComponent<LayoutElement>().preferredHeight * layout.CanvasScaleFactor,
                        0.5f);
                    foreach (var guide in catalog.Guides)
                    {
                        var actions = Find(host, "StrategyGuideProfiles-" + guide.GuideId).Single();
                        Assert.IsInstanceOf<HorizontalLayoutGroup>(
                            actions.GetComponent<HorizontalOrVerticalLayoutGroup>());
                        var buttons = actions.GetComponentsInChildren<Button>(true);
                        var profiles = guide.EntryProfiles.Where(item => item != null).ToList();
                        Assert.AreEqual(profiles.Count + 3, buttons.Length);
                        Assert.IsTrue(buttons.All(item =>
                            item.GetComponent<LayoutElement>() != null &&
                            item.GetComponent<LayoutElement>().minHeight * layout.CanvasScaleFactor >=
                            UnityTavernUiStyle.TouchHeight - 0.01f));
                        Assert.AreEqual(1, buttons.Count(item => item.GetComponentInChildren<Text>().text == "初级图"));
                        Assert.AreEqual(1, buttons.Count(item => item.GetComponentInChildren<Text>().text == "困难图"));
                        Assert.AreEqual(1, buttons.Count(item => item.GetComponentInChildren<Text>().text == "复制码"));
                        Assert.AreEqual(profiles.Count,
                            buttons.Count(item => item.name.StartsWith("StrategyGuideStartButton-", StringComparison.Ordinal)));
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void CompactSharePreviewUsesViewportAndScrollsWithoutShrinkingAccessibility()
        {
            var root = new GameObject("StrategyGuideCompactSharePreviewRoot", typeof(RectTransform));
            try
            {
                var layout = UnityTavernLayoutContext.ForSize(844f, 390f);
                var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
                var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
                var catalog = StrategyGuideCatalogLoader.LoadFromResources();
                var guide = catalog.Guides[0];
                var profile = guide.EntryProfiles.Single(item =>
                    item.Difficulty == StrategyGuideDifficulties.GuidedDiscover);
                var model = StrategyGuideShareCardService.Create(
                    catalog,
                    guide.GuideId,
                    profile.ProfileId,
                    version,
                    snapshot.ForLanguage(false),
                    false);

                new StrategyGuideShareCardView(
                    root.transform,
                    model,
                    layout,
                    false,
                    () => { }).Build();

                var shell = Find(root.transform, "StrategyGuideShareShell").Single().GetComponent<RectTransform>();
                var physicalShellSize = shell.sizeDelta * layout.CanvasScaleFactor;
                Assert.AreEqual(820f, physicalShellSize.x, 0.5f);
                Assert.AreEqual(366f, physicalShellSize.y, 0.5f);
                Assert.LessOrEqual(physicalShellSize.x, layout.Width);
                Assert.LessOrEqual(physicalShellSize.y, layout.Height);

                var scroll = Find(root.transform, "StrategyGuideShareCardScroll").Single().GetComponent<ScrollRect>();
                Assert.IsTrue(scroll.vertical);
                Assert.IsFalse(scroll.horizontal);
                Assert.GreaterOrEqual(
                    Find(root.transform, "StrategyGuideShareCard").Single().GetComponent<LayoutElement>().preferredHeight *
                    layout.CanvasScaleFactor,
                    600f - 0.5f);
                Assert.IsTrue(root.GetComponentsInChildren<Text>(true).All(item =>
                    item.fontSize * layout.CanvasScaleFactor >= 14f - 0.01f));
                Assert.IsTrue(root.GetComponentsInChildren<Button>(true).All(item =>
                    item.GetComponent<LayoutElement>() != null &&
                    item.GetComponent<LayoutElement>().minHeight * layout.CanvasScaleFactor >=
                    UnityTavernUiStyle.TouchHeight - 0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CaptureBeginnerAndHardProfileOneSheetAcceptanceImages()
        {
            Directory.CreateDirectory(Phase21AcceptanceDirectory);
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.GetGuide("GUIDE-S14-DEMON-TAVERN-CONSUME");

            CaptureProfile(
                catalog,
                guide,
                guide.EntryProfiles.Single(profile => profile.ProfileId == "guided"),
                version,
                snapshot,
                "guided");
            CaptureProfile(
                catalog,
                guide,
                guide.EntryProfiles.Single(profile => profile.ProfileId == "difficult"),
                version,
                snapshot,
                "difficult");
        }

        private static void CaptureProfile(
            StrategyGuideCatalog catalog,
            StrategyGuideDefinition guide,
            StrategyGuideEntryProfileDefinition profile,
            ResolvedGameVersion version,
            GameCatalogSnapshot snapshot,
            string filePrefix)
        {
            var model = StrategyGuideShareCardService.Create(
                catalog,
                guide.GuideId,
                profile.ProfileId,
                version,
                snapshot.ForLanguage(false),
                false);
            var export = StrategyGuideShareCardPngExporter.Export(model, false, Phase21AcceptanceDirectory);
            var exportPath = Path.Combine(Phase21AcceptanceDirectory, filePrefix + "-export-1600x900.png");
            File.Copy(export.Path, exportPath, true);
            if (!string.Equals(export.Path, exportPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(export.Path);
            }
            Assert.Greater(new FileInfo(exportPath).Length, 4096, exportPath);

            CapturePreview(model, 1280, 720, filePrefix + "-preview-1280x720.png");
            CapturePreview(model, 1920, 1080, filePrefix + "-preview-1920x1080.png");
            CapturePreview(model, 844, 390, filePrefix + "-preview-844x390.png");
        }

        private static void CapturePreview(
            StrategyGuideShareCardModel model,
            int width,
            int height,
            string fileName)
        {
            var path = Path.Combine(Phase21AcceptanceDirectory, fileName);
            var cameraObject = new GameObject("StrategyGuideShareCaptureCamera", typeof(Camera));
            var canvasObject = new GameObject(
                "StrategyGuideShareCaptureCanvas",
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
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(width, height);
                var canvas = canvasObject.GetComponent<Canvas>();
                LearnHearthstone.Presentation.LearnHearthstoneBootstrap.ConfigureCanvas(canvas, layout);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;

                new StrategyGuideShareCardView(
                    canvasObject.transform,
                    model,
                    layout,
                    false,
                    () => { }).Build();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
                var scroll = canvasObject.GetComponentInChildren<ScrollRect>(true);
                if (scroll != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                    scroll.verticalNormalizedPosition = 1f;
                }
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
                camera.Render();
                AssertPreviewGeometry(canvasObject.transform, camera, width, height);

                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Assert.Greater(new FileInfo(path).Length, 4096, path);
            }
            finally
            {
                RenderTexture.active = previousActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                UnityEngine.Object.DestroyImmediate(canvasObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void AssertPreviewGeometry(Transform root, Camera camera, int width, int height)
        {
            if (width >= 1280)
            {
                AssertRenderedText(root, camera, "StrategyGuideShareTitle");
                AssertRenderedText(root, camera, "StrategyGuideShareStartMeta");
                AssertRenderedText(root, camera, "StrategyGuideShareVersion");
                AssertRenderedText(root, camera, "StrategyGuideShareHash");
                AssertRenderedText(root, camera, "StrategyGuideShareStartingStateHeading");
                AssertRenderedText(root, camera, "StrategyGuideShareProbability");
                AssertRenderedText(root, camera, "StrategyGuideShareDisclaimer");
                foreach (var item in FindPrefix(root, "StrategyGuideShareStartName-")
                    .Concat(FindPrefix(root, "StrategyGuideShareStartStats-")))
                {
                    AssertRenderedText(item.GetComponent<Text>(), camera);
                }
            }

            foreach (var item in FindPrefix(root, "StrategyGuideShareShapingImage-")
                .Where(item => item.GetComponent<Image>() != null))
            {
                var image = item.GetComponent<Image>();
                if (image.sprite != null)
                {
                    continue;
                }

                Assert.Less(image.color.maxColorComponent, 0.95f, item.name + " must not render a white missing-art block.");
                var fallback = item.GetComponentsInChildren<Text>(true).SingleOrDefault();
                Assert.IsNotNull(fallback, item.name + " needs a readable missing-art fallback.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(fallback.text), item.name + " needs readable fallback copy.");
                var fallbackRect = PhysicalRect(fallback.rectTransform, camera);
                Assert.GreaterOrEqual(fallbackRect.height, 13.5f, item.name + " fallback must keep a readable line box.");
                Assert.Greater(fallbackRect.width, 1f, item.name + " fallback must keep a visible width.");
            }

            if (width != 844 || height != 390)
            {
                return;
            }

            var actions = Find(root, "StrategyGuideShareActions").Single().GetComponent<RectTransform>();
            var scroll = Find(root, "StrategyGuideShareCardScroll").Single().GetComponent<ScrollRect>();
            var card = Find(root, "StrategyGuideShareCard").Single().GetComponent<RectTransform>();
            var actionsRect = PhysicalRect(actions, camera);
            var scrollRect = PhysicalRect(scroll.GetComponent<RectTransform>(), camera);
            var viewportRect = PhysicalRect(scroll.viewport, camera);
            var contentRect = PhysicalRect(scroll.content, camera);
            var cardRect = PhysicalRect(card, camera);

            Assert.AreEqual(48f, actionsRect.height, 0.75f, "Compact actions must remain a fixed physical-height bar.");
            foreach (var button in actions.GetComponentsInChildren<Button>(true))
            {
                AssertContained(actionsRect, PhysicalRect(button.GetComponent<RectTransform>(), camera), 0.75f, button.name);
            }
            Assert.LessOrEqual(Intersection(actionsRect, scrollRect).height, 0.75f, "Actions must not overlap the ScrollView.");
            Assert.GreaterOrEqual(scrollRect.height, 200f, "Compact ScrollView must own the remaining modal height.");
            Assert.GreaterOrEqual(viewportRect.height, 200f, "Compact viewport must remain usable.");
            AssertContained(scrollRect, viewportRect, 0.75f, "viewport");
            Assert.Greater(scroll.viewport.GetComponent<Image>().color.a, 0f, "The hidden stencil-mask graphic needs non-zero alpha.");
            Assert.GreaterOrEqual(contentRect.height, 599f, "Compact content must preserve the full flow card for scrolling.");
            Assert.GreaterOrEqual(cardRect.height, 599f, "Compact flow card must not shrink to the viewport.");
            AssertContained(contentRect, cardRect, 0.75f, "card");

            var visibleCard = Intersection(viewportRect, cardRect);
            Assert.GreaterOrEqual(visibleCard.width, viewportRect.width * 0.95f, "The flow card must intersect the viewport width.");
            Assert.GreaterOrEqual(visibleCard.height, viewportRect.height * 0.95f, "The top of the flow card must be visible after opening.");
        }

        private static void AssertRenderedText(Transform root, Camera camera, string name)
        {
            AssertRenderedText(Find(root, name).Single().GetComponent<Text>(), camera);
        }

        private static void AssertRenderedText(Text text, Camera camera)
        {
            var rect = PhysicalRect(text.rectTransform, camera);
            Assert.GreaterOrEqual(rect.height, 13.5f, text.name + " must keep a readable physical line box. Actual: " + rect);
            Assert.Greater(rect.width, 1f, text.name + " must keep a visible width. Actual: " + rect);
            Assert.Greater(text.cachedTextGenerator.characterCountVisible, 0, text.name + " must render visible characters.");
        }

        private static Rect PhysicalRect(RectTransform transform, Camera camera)
        {
            var corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            var bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(
                Mathf.Min(bottomLeft.x, topRight.x),
                Mathf.Min(bottomLeft.y, topRight.y),
                Mathf.Max(bottomLeft.x, topRight.x),
                Mathf.Max(bottomLeft.y, topRight.y));
        }

        private static Rect Intersection(Rect left, Rect right)
        {
            var xMin = Mathf.Max(left.xMin, right.xMin);
            var yMin = Mathf.Max(left.yMin, right.yMin);
            var xMax = Mathf.Min(left.xMax, right.xMax);
            var yMax = Mathf.Min(left.yMax, right.yMax);
            return xMax <= xMin || yMax <= yMin
                ? Rect.zero
                : Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static void AssertContained(Rect parent, Rect child, float tolerance, string label)
        {
            Assert.GreaterOrEqual(child.xMin, parent.xMin - tolerance, label + " extends left of its parent.");
            Assert.GreaterOrEqual(child.yMin, parent.yMin - tolerance, label + " extends below its parent.");
            Assert.LessOrEqual(child.xMax, parent.xMax + tolerance, label + " extends right of its parent.");
            Assert.LessOrEqual(child.yMax, parent.yMax + tolerance, label + " extends above its parent.");
        }

        private static List<Transform> FindPrefix(Transform root, string prefix)
        {
            var result = new List<Transform>();
            Collect(root, item => item.name.StartsWith(prefix, StringComparison.Ordinal), result);
            return result;
        }

        private static List<Transform> Find(Transform root, string name)
        {
            var result = new List<Transform>();
            Collect(root, item => string.Equals(item.name, name, StringComparison.Ordinal), result);
            return result;
        }

        private static void Collect(Transform root, Func<Transform, bool> predicate, ICollection<Transform> result)
        {
            if (predicate(root))
            {
                result.Add(root);
            }
            for (var index = 0; index < root.childCount; index += 1)
            {
                Collect(root.GetChild(index), predicate, result);
            }
        }
    }
}
