using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class ChoiceQueueModalTests
    {
        private const string CaptureDirectory = ".planning/tavern-ui-screenshot-requirements/captures";

        [Test]
        public void DarkGift_SelectDoesNotResolveUntilSeparateConfirm()
        {
            var root = Root(1280, 720);
            try
            {
                var service = CreateService(out var gift, out var minion);
                OfferDarkGifts(service, gift, minion, 3);
                Build(root, service);

                Assert.IsFalse(Find(root.transform, "UnityDarkGiftChoiceConfirmButton").GetComponent<Button>().interactable);
                Find(root.transform, "UnityDarkGiftChoiceSelectButton-1").GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(service.State.ChoiceQueue.ActiveChoice, "Selecting a card must not resolve the queue.");
                Assert.IsNotNull(Find(root.transform, "UnityDarkGiftChoiceSelectedMarker-1"));
                StringAssert.Contains("已选择", Text(root.transform, "UnityDarkGiftChoiceSelectedMarker-1"));
                var confirm = Find(root.transform, "UnityDarkGiftChoiceConfirmButton").GetComponent<Button>();
                Assert.IsTrue(confirm.interactable);

                confirm.onClick.Invoke();

                Assert.IsNull(service.State.ChoiceQueue.ActiveChoice);
                Assert.AreEqual(1, service.State.PlayerDarkGifts.AcquiredGiftInstances.Count);
                Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DarkGift_BlockingHeaderShowsSourceRoundCostCountAndExplanationWithoutClose()
        {
            var root = Root(1280, 720);
            try
            {
                var service = CreateService(out var gift, out var minion);
                OfferDarkGifts(service, gift, minion, 3);
                Build(root, service);

                var metadata = Text(root.transform, "UnityDarkGiftChoiceMetadata");
                StringAssert.Contains("英雄技能", metadata);
                StringAssert.Contains("第 3 回合", metadata);
                StringAssert.Contains("3 金币", metadata);
                StringAssert.Contains("0/1", metadata);
                StringAssert.Contains("阻塞", metadata);
                Assert.IsNull(Find(root.transform, "UnityDarkGiftChoiceCloseButton"));

                Find(root.transform, "UnityDarkGiftChoiceWhyBlockedButton").GetComponent<Button>().onClick.Invoke();

                StringAssert.Contains("必须完成", Text(root.transform, "UnityDarkGiftChoiceBlockingExplanation"));
                Assert.IsNotNull(Find(root.transform, "UnityDarkGiftChoicePanel").GetComponent<UnityFocusTrap>());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DarkGift_CompactUsesSingleCardPagingWhileDesktopShowsGrid()
        {
            AssertChoiceCountAtResolution(844, 390, 1, true);
            AssertChoiceCountAtResolution(1920, 1080, 3, false);
        }

        [Test]
        public void DarkGift_CardShowsMinionAndGiftRulesAsSeparateReadableSections()
        {
            var root = Root(1920, 1080);
            try
            {
                var service = CreateService(out var gift, out var minion);
                minion.Text = "MINION-RULES-SENTINEL";
                gift.Text = "DARK-GIFT-RULES-SENTINEL";
                OfferDarkGifts(service, gift, minion, 3);
                Build(root, service);

                Assert.AreEqual("MINION-RULES-SENTINEL", Text(root.transform, "UnityDarkGiftChoiceMinionText-0"));
                Assert.AreEqual("DARK-GIFT-RULES-SENTINEL", Text(root.transform, "UnityDarkGiftChoiceGiftText-0"));
                Assert.GreaterOrEqual(
                    Find(root.transform, "UnityDarkGiftChoiceSelectButton-0").GetComponent<LayoutElement>().minHeight,
                    48f);
                Assert.LessOrEqual(
                    Find(root.transform, "UnityDarkGiftChoiceConfirmButton").GetComponent<LayoutElement>().preferredWidth,
                    280f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DarkGift_CardUsesPortraitMainArtAttachedGiftAndGlanceStats()
        {
            var root = Root(1920, 1080);
            try
            {
                var service = CreateService(out var gift, out var minion);
                OfferDarkGifts(service, gift, minion, 3);
                Build(root, service);

                var minionArt = Find(root.transform, "UnityDarkGiftChoiceMinionArt").GetComponent<LayoutElement>();
                var giftArt = Find(root.transform, "UnityDarkGiftChoiceGiftArt").GetComponent<LayoutElement>();
                Assert.Greater(minionArt.preferredHeight, minionArt.preferredWidth, "The full minion card must use a portrait slot.");
                Assert.Greater(giftArt.preferredHeight, giftArt.preferredWidth, "The attached Dark Gift must use a portrait slot.");
                Assert.Less(giftArt.preferredHeight, minionArt.preferredHeight, "The Dark Gift is a secondary attachment, not a competing full-size card.");
                Assert.IsTrue(Find(root.transform, "UnityDarkGiftChoiceGiftArt").IsChildOf(
                    Find(root.transform, "UnityDarkGiftChoiceGiftAttachment")));

                var summary = Text(root.transform, "UnityDarkGiftChoiceMinionSummary-0");
                StringAssert.Contains(minion.TavernTier + " 星", summary);
                StringAssert.Contains(minion.BaseAttack + "/" + minion.BaseHealth, summary);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DarkGift_MissingArtShowsNamedFallbackInsteadOfAnonymousSquare()
        {
            var root = Root(1920, 1080);
            try
            {
                var service = CreateService(out var gift, out var minion);
                OfferDarkGifts(service, gift, minion, 1);
                var option = service.State.ChoiceQueue.ActiveChoice.Options[0];
                option.SourceId = "MISSING-MINION-ART";
                option.ImagePath = null;
                option.RewardId = "MISSING-GIFT-ART";
                option.RewardImagePath = null;
                Build(root, service);

                StringAssert.Contains(minion.Name, Text(root.transform, "UnityDarkGiftChoiceMinionArtFallback"));
                StringAssert.Contains(gift.DisplayName, Text(root.transform, "UnityDarkGiftChoiceGiftArtFallback"));
                Assert.Less(
                    Find(root.transform, "UnityDarkGiftChoiceMinionArt").GetComponent<Image>().color.a,
                    1f);
                Assert.Less(
                    Find(root.transform, "UnityDarkGiftChoiceGiftArt").GetComponent<Image>().color.a,
                    1f);
                var giftFallbackColor = Find(root.transform, "UnityDarkGiftChoiceGiftArt").GetComponent<Image>().color;
                Assert.Less(Mathf.Max(giftFallbackColor.r, giftFallbackColor.g, giftFallbackColor.b), 0.35f);
                Assert.Greater(giftFallbackColor.a, 0.9f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(844, 390)]
        [TestCase(994, 384)]
        [TestCase(1000, 600)]
        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void DarkGift_ReferenceResolutionsStayInsideViewportAndKeepFocusAffordances(int width, int height)
        {
            var root = Root(width, height);
            try
            {
                var service = CreateService(out var gift, out var minion);
                OfferDarkGifts(service, gift, minion, 3);
                Build(root, service);

                var panel = Find(root.transform, "UnityDarkGiftChoicePanel").GetComponent<RectTransform>();
                var layout = UnityTavernLayoutContext.ForSize(width, height);
                Assert.LessOrEqual(panel.rect.width * layout.CanvasScaleFactor, width + 0.01f);
                Assert.LessOrEqual(panel.rect.height * layout.CanvasScaleFactor, height + 0.01f);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
                if (layout.IsCompact)
                {
                    var card = Find(root.transform, "UnityDarkGiftChoiceCard-0").GetComponent<RectTransform>();
                    AssertContainedBy(panel, Find(root.transform, "UnityDarkGiftChoiceHeader").GetComponent<RectTransform>());
                    AssertContainedBy(panel, card);
                    AssertContainedBy(card, Find(root.transform, "UnityDarkGiftChoiceMinionArt").GetComponent<RectTransform>());
                    AssertContainedBy(card, Find(root.transform, "UnityDarkGiftChoiceGiftArt").GetComponent<RectTransform>());
                    AssertContainedBy(card, Find(root.transform, "UnityDarkGiftChoiceMinionSection-0").GetComponent<RectTransform>());
                    AssertContainedBy(card, Find(root.transform, "UnityDarkGiftChoiceGiftSection-0").GetComponent<RectTransform>());
                    AssertContainedBy(card, Find(root.transform, "UnityDarkGiftChoiceSelectButton-0").GetComponent<RectTransform>());
                    Assert.IsNotNull(card.GetComponent<HorizontalLayoutGroup>());
                    Assert.IsNotNull(Find(root.transform, "UnityDarkGiftChoiceCompactDetails-0"));
                    AssertContainedBy(panel, Find(root.transform, "UnityDarkGiftChoicePreviousButton").GetComponent<RectTransform>());
                    AssertContainedBy(panel, Find(root.transform, "UnityDarkGiftChoicePage").GetComponent<RectTransform>());
                    AssertContainedBy(panel, Find(root.transform, "UnityDarkGiftChoiceNextButton").GetComponent<RectTransform>());
                    AssertContainedBy(panel, Find(root.transform, "UnityDarkGiftChoiceActions").GetComponent<RectTransform>());
                    AssertContainedBy(panel, Find(root.transform, "UnityDarkGiftChoiceConfirmButton").GetComponent<RectTransform>());
                }

                foreach (var button in panel.GetComponentsInChildren<Button>(true))
                {
                    Assert.IsNotNull(button.GetComponent<UnitySelectableFocusRing>(), button.name);
                    var buttonRect = button.GetComponent<RectTransform>().rect;
                    Assert.GreaterOrEqual(
                        buttonRect.width * layout.CanvasScaleFactor,
                        UiFactory.MinimumButtonHeight - 0.5f,
                        button.name + " physical width");
                    Assert.GreaterOrEqual(
                        buttonRect.height * layout.CanvasScaleFactor,
                        UiFactory.MinimumButtonHeight - 0.5f,
                        button.name + " physical height");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DarkGift_CapturesPortraitCompositionAtDesktopAndCompactSizes()
        {
            Capture(1920, 1080, "phase10-dark-gift-choice-1920x1080.png");
            Capture(844, 390, "phase10-dark-gift-choice-844x390.png");
            Capture(994, 384, "phase10-dark-gift-choice-994x384.png");
        }

        private static void AssertChoiceCountAtResolution(int width, int height, int expectedCards, bool expectsPager)
        {
            var root = Root(width, height);
            try
            {
                var service = CreateService(out var gift, out var minion);
                OfferDarkGifts(service, gift, minion, 3);
                Build(root, service);

                Assert.AreEqual(expectedCards, CountPrefix(root.transform, "UnityDarkGiftChoiceCard-"));
                Assert.AreEqual(expectsPager, Find(root.transform, "UnityDarkGiftChoiceNextButton") != null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static MatchService CreateService(out DarkGiftDefinition gift, out MinionDefinition minion)
        {
            var baseline = MatchService.CreateWithDefaultCatalog(41, new InMemoryTestScenarioRepository());
            minion = baseline.Catalogs.Minions.All.First(item => item.InPool && item.TavernTier == 2);
            gift = new DarkGiftDefinition
            {
                Id = "ui4-test-gift",
                ResearchKey = "UI4-GIFT-R01",
                RevisionId = "ui4-test-gift@1",
                EffectRevision = "ui4-test-gift.effect@1",
                DisplayName = "星灯赐礼",
                Text = "每回合首次刷新后获得强化。",
                StackPolicy = DarkGiftStackPolicies.Stack,
                MaxStacks = 3,
                DurationPolicy = DarkGiftDurationPolicies.Persistent,
                InitialUses = 2,
                ImplementationStatus = DarkGiftImplementationStatus.Implemented
            };
            var catalogs = new GameCatalogSet(
                baseline.Catalogs.Minions,
                baseline.Catalogs.Spells,
                baseline.Catalogs.Heroes,
                baseline.Catalogs.Trinkets,
                baseline.Catalogs.Quests,
                baseline.Catalogs.TimewarpedTavern,
                baseline.Catalogs.Anomalies,
                baseline.Catalogs.DarkmoonPrizes,
                new DarkGiftCatalog(new[] { gift }));
            var resolvers = new DarkGiftResolverRegistry();
            resolvers.Register(gift.EffectRevision, _ => DarkGiftResolution.Success("ui4-applied"));
            return MatchService.CreateWithCatalogs(
                catalogs,
                41,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { EnableQuests = false, EnableTrinkets = false },
                darkGiftDefinitions: new[] { gift },
                darkGiftResolvers: resolvers);
        }

        private static void OfferDarkGifts(MatchService service, DarkGiftDefinition gift, MinionDefinition minion, int count)
        {
            service.State.Round = 3;
            service.State.ChoiceQueue = new ChoiceQueueState
            {
                ActiveChoice = new ChoiceQueueItem
                {
                    RequestId = "ui4-dark-gift-choice",
                    Kind = ChoiceRequestKind.DarkGift,
                    Source = "英雄技能：暗月邀约",
                    CreatedRound = 3,
                    Priority = 0,
                    Blocking = true,
                    RemainingPicks = 1,
                    ResolutionMetadata = new List<ChoiceResolutionMetadataEntry>
                    {
                        new ChoiceResolutionMetadataEntry { Key = "gold-cost", Value = "3" },
                        new ChoiceResolutionMetadataEntry { Key = "stack-policy", Value = gift.StackPolicy }
                    },
                    Options = Enumerable.Range(0, count).Select(index => new MechanicChoiceOption
                    {
                        OptionId = "ui4-option-" + index,
                        Kind = AdvancedMechanicKind.DarkGift,
                        SourceId = minion.CardId,
                        DisplayName = minion.Name,
                        Text = minion.Text,
                        ImagePath = minion.ImagePath,
                        RewardId = gift.RevisionId,
                        RewardName = gift.DisplayName,
                        RewardText = gift.Text,
                        RewardImagePath = gift.ImagePath,
                        DifficultyTier = minion.TavernTier,
                        Attack = minion.BaseAttack,
                        Health = minion.BaseHealth,
                        Tribes = new List<Tribe>(minion.Tribes),
                        Cost = 3,
                        Tags = new List<string> { "stack:" + gift.StackPolicy }
                    }).ToList()
                }
            };
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
            var cameraObject = new GameObject("DarkGiftCaptureCamera", typeof(Camera));
            var canvasObject = new GameObject(
                "DarkGiftCaptureCanvas",
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

                var canvas = canvasObject.GetComponent<Canvas>();
                LearnHearthstoneBootstrap.ConfigureCanvas(canvas, UnityTavernLayoutContext.ForSize(width, height));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;

                var service = CreateService(out var gift, out var minion);
                OfferDarkGifts(service, gift, minion, 3);
                Build(canvasObject, service);
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

        private static void Build(GameObject root, MatchService service)
        {
            new UnityTavernTrainerView(root.transform, service, new LocalAdvisorService(), () => { }).Build();
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

        private static void AssertContainedBy(RectTransform container, RectTransform child)
        {
            var containerCorners = new Vector3[4];
            var childCorners = new Vector3[4];
            container.GetWorldCorners(containerCorners);
            child.GetWorldCorners(childCorners);
            const float tolerance = 0.5f;
            Assert.GreaterOrEqual(childCorners[0].x, containerCorners[0].x - tolerance, child.name + " left");
            Assert.GreaterOrEqual(childCorners[0].y, containerCorners[0].y - tolerance, child.name + " bottom");
            Assert.LessOrEqual(childCorners[2].x, containerCorners[2].x + tolerance, child.name + " right");
            Assert.LessOrEqual(childCorners[2].y, containerCorners[2].y + tolerance, child.name + " top");
        }
    }
}
