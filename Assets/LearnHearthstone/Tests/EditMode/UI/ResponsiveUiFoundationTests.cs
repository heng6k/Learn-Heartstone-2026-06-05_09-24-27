using System;
using System.Linq;
using System.Reflection;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class ResponsiveUiFoundationTests
    {
        [Test]
        public void LayoutContext_ConvertsPhysicalPixelsUsingConfiguredCanvasScaleRule()
        {
            var scaleProperty = typeof(UnityTavernLayoutContext).GetProperty("CanvasScaleFactor");
            var convertMethod = typeof(UnityTavernLayoutContext).GetMethod(
                "CanvasUnitsForPhysicalPixels",
                new[] { typeof(float) });

            Assert.IsNotNull(scaleProperty, "UI1 requires an explicit Canvas scale-factor contract.");
            Assert.IsNotNull(convertMethod, "UI1 requires physical-pixel to Canvas-unit conversion.");

            AssertPhysicalConversion(
                UnityTavernLayoutContext.ForSize(844f, 390f),
                844f / 1920f,
                scaleProperty,
                convertMethod);
            AssertPhysicalConversion(
                UnityTavernLayoutContext.ForSize(1280f, 720f),
                2f / 3f,
                scaleProperty,
                convertMethod);
        }

        [Test]
        public void LayoutContext_HighDpiHandheldUsesLogicalSafeAreaAndDensityIndependentTouchTargets()
        {
            var mobile = UnityTavernLayoutContext.ForScreen(
                2400f,
                1080f,
                new Rect(80f, 0f, 2240f, 1080f),
                420f,
                true);
            var desktop = UnityTavernLayoutContext.ForScreen(
                2400f,
                1080f,
                new Rect(0f, 0f, 2400f, 1080f),
                0f,
                false);

            Assert.IsTrue(mobile.IsCompact, "High-DPI handhelds must not inherit the desktop Wide layout.");
            Assert.IsTrue(desktop.IsWide);
            Assert.AreEqual(420f / UnityTavernLayoutContext.MobileReferenceDpi, mobile.DensityScale, 0.001f);
            Assert.AreEqual(2400f, mobile.PixelWidth, 0.001f);
            Assert.GreaterOrEqual(
                mobile.CanvasUnitsForTouchTarget() * mobile.CanvasScaleFactor,
                48f * mobile.DensityScale - 0.01f);
        }

        [Test]
        public void LayoutContext_ResolvesShortLandscapeBeforeCompactWithoutMisclassifying720p()
        {
            Assert.AreEqual(
                UnityTavernLayoutMode.ShortLandscape,
                UnityTavernLayoutContext.ForSize(844f, 390f).Mode);
            Assert.AreEqual(
                UnityTavernLayoutMode.ShortLandscape,
                UnityTavernLayoutContext.ForSize(994f, 384f).Mode);
            Assert.AreEqual(
                UnityTavernLayoutMode.Compact,
                UnityTavernLayoutContext.ForSize(1000f, 600f).Mode);
            Assert.AreEqual(
                UnityTavernLayoutMode.Standard,
                UnityTavernLayoutContext.ForSize(1280f, 720f).Mode);

            var shortLayout = UnityTavernLayoutContext.ForSize(844f, 390f);
            Assert.IsTrue(shortLayout.IsShortLandscape);
            Assert.IsTrue(shortLayout.IsCompact, "ShortLandscape must retain compact component styling during migration.");
        }

        [Test]
        public void LayoutContext_ShortLandscapeMetricsMatchPhysicalShellBudget()
        {
            var layout = UnityTavernLayoutContext.ForSize(844f, 390f);

            Assert.AreEqual(
                UnityTavernUiStyle.ShortLandscapeShopHeight,
                layout.ZoneMetrics(UnityTavernZoneKind.Shop, UnityTavernCardMode.Shop).Height * layout.CanvasScaleFactor,
                0.01f);
            Assert.AreEqual(
                UnityTavernUiStyle.ShortLandscapeBoardHeight,
                layout.ZoneMetrics(UnityTavernZoneKind.PlayerBoard, UnityTavernCardMode.Board).Height * layout.CanvasScaleFactor,
                0.01f);
            Assert.AreEqual(
                UnityTavernUiStyle.ShortLandscapeHandPeekHeight,
                layout.HandZoneHeight(10) * layout.CanvasScaleFactor,
                0.01f);
        }

        [Test]
        public void MobileKeyboardAvoider_UsesKeyboardTopAndAllStyledInputsReceiveTheComponent()
        {
            Assert.AreEqual(
                0.45f,
                UnityMobileKeyboardAvoider.CalculateKeyboardTopAnchor(1000, new Rect(0f, 0f, 1000f, 450f), 0.05f),
                0.001f);

            var inputObject = new GameObject("KeyboardAwareInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            try
            {
                var input = inputObject.GetComponent<InputField>();
                UnityTavernUiStyle.ConfigureInputField(input, UnityTavernUiStyle.Blue);
                Assert.IsNotNull(inputObject.GetComponent<UnityMobileKeyboardAvoider>());
            }
            finally
            {
                Object.DestroyImmediate(inputObject);
            }
        }

        [Test]
        public void Bootstrap_RebuildsRoutesOnlyWhenResponsiveModeChanges()
        {
            Assert.IsFalse(LearnHearthstone.Presentation.LearnHearthstoneBootstrap.RequiresRouteRebuild(
                UnityTavernLayoutMode.Compact,
                UnityTavernLayoutMode.Compact));
            Assert.IsTrue(LearnHearthstone.Presentation.LearnHearthstoneBootstrap.RequiresRouteRebuild(
                UnityTavernLayoutMode.Compact,
                UnityTavernLayoutMode.Standard));
        }

        [Test]
        public void MobileRuntimePolicy_UsesLowQualityAndSixtyFpsTargets()
        {
            Assert.AreEqual(1, LearnHearthstone.Presentation.LearnHearthstoneDistributionChannel.MobileQualityLevel);
            Assert.AreEqual(60, LearnHearthstone.Presentation.LearnHearthstoneDistributionChannel.MobileTargetFrameRate);
        }

        [Test]
        public void CardImageProvider_BoundedCacheEvictsLeastRecentlyUsedEntry()
        {
            var cacheType = typeof(CardImageProvider).GetNestedType("BoundedSpriteCache", BindingFlags.NonPublic);
            Assert.IsNotNull(cacheType);
            var cache = Activator.CreateInstance(
                cacheType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { 2, false },
                null);
            var set = cacheType.GetMethod("Set", BindingFlags.Instance | BindingFlags.Public);
            var tryGet = cacheType.GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public);
            var count = cacheType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);

            set.Invoke(cache, new object[] { "a", null });
            set.Invoke(cache, new object[] { "b", null });
            var touchArguments = new object[] { "a", null };
            Assert.IsTrue((bool)tryGet.Invoke(cache, touchArguments));
            set.Invoke(cache, new object[] { "c", null });

            Assert.AreEqual(2, count.GetValue(cache));
            Assert.IsFalse((bool)tryGet.Invoke(cache, new object[] { "b", null }));
            Assert.IsTrue((bool)tryGet.Invoke(cache, new object[] { "a", null }));
            Assert.IsTrue((bool)tryGet.Invoke(cache, new object[] { "c", null }));
            Assert.AreEqual(640, CardImageProvider.MaximumCachedSpriteCount);
        }

        [Test]
        public void SafeAreaPanel_UsesActualInsetsMinimumMarginAndTitleSafeBounds()
        {
            var safeAreaType = typeof(UiFactory).Assembly.GetType(
                "LearnHearthstone.Presentation.Common.UnitySafeAreaPanel");
            Assert.IsNotNull(safeAreaType, "UI1 requires a reusable Screen.safeArea component.");

            var calculate = safeAreaType.GetMethod(
                "CalculateSafeRect",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(calculate, "Safe-area math must be deterministic and EditMode-testable.");

            var actualSafeArea = new Rect(24f, 0f, 796f, 390f);
            var safeRect = (Rect)calculate.Invoke(null, new object[] { 844, 390, actualSafeArea, false });
            AssertRect(safeRect, 24f, 16f, 820f, 374f);

            var titleSafeRect = (Rect)calculate.Invoke(null, new object[] { 844, 390, actualSafeArea, true });
            AssertRect(titleSafeRect, 42.2f, 19.5f, 801.8f, 370.5f);
        }

        [Test]
        public void UiFactory_CompactControlsMeetFinalPhysicalPixelMinimums()
        {
            var root = new GameObject("ResponsiveUiRoot", typeof(RectTransform));
            try
            {
                var compact = UnityTavernLayoutContext.ForSize(844f, 390f);
                var buttonMethod = typeof(UiFactory).GetMethods()
                    .SingleOrDefault(method => method.Name == "Button" && method.GetParameters().Length == 5);
                Assert.IsNotNull(buttonMethod, "UiFactory.Button must accept a layout context for deterministic sizing.");

                var button = (Button)buttonMethod.Invoke(
                    null,
                    new object[] { "CompactButton", root.transform, "确认", new UnityAction(() => { }), compact });
                var buttonHeight = button.GetComponent<LayoutElement>().minHeight;
                var buttonWidth = button.GetComponent<LayoutElement>().minWidth;
                var buttonLabel = button.GetComponentInChildren<Text>();
                Assert.GreaterOrEqual(buttonWidth * ReadScale(compact), 48f - 0.01f);
                Assert.GreaterOrEqual(buttonHeight * ReadScale(compact), 48f - 0.01f);
                Assert.GreaterOrEqual(buttonLabel.resizeTextMinSize * ReadScale(compact), 14f - 0.01f);

                UnityTavernUiStyle.SetFixedSize(button.gameObject, 24f, 24f);
                Assert.GreaterOrEqual(button.GetComponent<LayoutElement>().minWidth * ReadScale(compact), 48f - 0.01f);
                Assert.GreaterOrEqual(button.GetComponent<LayoutElement>().minHeight * ReadScale(compact), 48f - 0.01f);

                var scrollMethod = typeof(UiFactory).GetMethods()
                    .SingleOrDefault(method => method.Name == "ScrollView" && method.GetParameters().Length == 6);
                Assert.IsNotNull(scrollMethod, "UiFactory.ScrollView must accept a layout context for deterministic sizing.");
                var arguments = new object[] { "CompactScroll", root.transform, Color.clear, null, compact, false };
                scrollMethod.Invoke(null, arguments);
                var scrollRect = (ScrollRect)arguments[3];
                var scrollbarWidth = scrollRect.verticalScrollbar.GetComponent<RectTransform>().sizeDelta.x;
                Assert.GreaterOrEqual(scrollbarWidth * ReadScale(compact), 20f - 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FocusTrap_ContainsSelectionAndRestoresPreviousControl()
        {
            var root = new GameObject("FocusTrapRoot", typeof(RectTransform));
            GameObject eventSystemObject = null;
            try
            {
                var eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    eventSystemObject = new GameObject("FocusTrapEventSystem", typeof(EventSystem));
                    eventSystem = eventSystemObject.GetComponent<EventSystem>();
                }

                var outside = UiFactory.Button("OutsideButton", root.transform, "Outside", () => { });
                var modal = new GameObject("BlockingModal", typeof(RectTransform));
                modal.transform.SetParent(root.transform, false);
                var inside = UiFactory.Button("InsideButton", modal.transform, "Inside", () => { });
                eventSystem.SetSelectedGameObject(outside.gameObject);

                var trapType = typeof(UiFactory).Assembly.GetType(
                    "LearnHearthstone.Presentation.Common.UnityFocusTrap");
                Assert.IsNotNull(trapType, "UI1 requires a reusable blocking-modal focus trap.");
                var trap = modal.AddComponent(trapType);
                trapType.GetMethod("Activate").Invoke(trap, new object[] { inside.gameObject });
                Assert.AreSame(inside.gameObject, eventSystem.currentSelectedGameObject);

                eventSystem.SetSelectedGameObject(outside.gameObject);
                trapType.GetMethod("EnforceFocus").Invoke(trap, null);
                Assert.AreSame(inside.gameObject, eventSystem.currentSelectedGameObject);

                trapType.GetMethod("Release").Invoke(trap, null);
                Assert.AreSame(outside.gameObject, eventSystem.currentSelectedGameObject);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (eventSystemObject != null)
                {
                    Object.DestroyImmediate(eventSystemObject);
                }
            }
        }

        [Test]
        public void UiFactoryButton_UsesIndependentVisibleFocusRing()
        {
            var root = new GameObject("FocusRingRoot", typeof(RectTransform));
            GameObject eventSystemObject = null;
            try
            {
                var eventSystem = EventSystem.current;
                if (eventSystem == null)
                {
                    eventSystemObject = new GameObject("FocusRingEventSystem", typeof(EventSystem));
                    eventSystem = eventSystemObject.GetComponent<EventSystem>();
                }

                var button = UiFactory.Button("FocusRingButton", root.transform, "Focus", () => { });
                var focusRingType = typeof(UiFactory).Assembly.GetType(
                    "LearnHearthstone.Presentation.Common.UnitySelectableFocusRing");
                Assert.IsNotNull(focusRingType, "UiFactory buttons require a non-color-only focus indicator.");
                var focusRing = button.GetComponent(focusRingType);
                Assert.IsNotNull(focusRing);

                var focusOutline = (Outline)focusRingType.GetProperty("FocusOutline").GetValue(focusRing);
                Assert.IsNotNull(focusOutline);
                Assert.AreNotSame(button.gameObject, focusOutline.gameObject);
                Assert.IsFalse(focusOutline.enabled);
                Assert.IsFalse(focusOutline.GetComponent<Image>().raycastTarget);
                var ring = focusOutline.transform.parent;
                Assert.IsNull(ring.GetComponent<Image>(), "Focus must be drawn as borders, never as a full-size color block over input text.");
                Assert.AreEqual(4, ring.GetComponentsInChildren<Image>(true).Length);
                Assert.IsTrue(ring.GetComponentsInChildren<Image>(true).All(image =>
                    image.rectTransform.sizeDelta.x == 3f || image.rectTransform.sizeDelta.y == 3f));

                var eventData = new BaseEventData(eventSystem);
                ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.selectHandler);
                Assert.IsTrue(focusOutline.enabled);
                Assert.GreaterOrEqual(focusOutline.effectDistance.magnitude, 2f);

                ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.deselectHandler);
                Assert.IsFalse(focusOutline.enabled);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (eventSystemObject != null)
                {
                    Object.DestroyImmediate(eventSystemObject);
                }
            }
        }

        [Test]
        public void InputPromptService_SwitchesHumanReadableBindingByDeviceFamily()
        {
            var assembly = typeof(UiFactory).Assembly;
            var serviceType = assembly.GetType("LearnHearthstone.Presentation.Common.UnityInputPromptService");
            var familyType = assembly.GetType("LearnHearthstone.Presentation.Common.UnityInputDeviceFamily");
            Assert.IsNotNull(serviceType, "UI1 requires dynamic input prompt resolution.");
            Assert.IsNotNull(familyType);

            var setFamily = serviceType.GetMethod("SetCurrentDeviceFamily", BindingFlags.Public | BindingFlags.Static);
            var display = serviceType.GetMethod("DisplayNameForBindings", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(setFamily);
            Assert.IsNotNull(display);
            var keyboard = Enum.Parse(familyType, "KeyboardMouse");
            var gamepad = Enum.Parse(familyType, "Gamepad");
            var bindings = new[] { "<Keyboard>/enter", "<Gamepad>/buttonSouth" };

            setFamily.Invoke(null, new[] { keyboard });
            var keyboardDisplay = (string)display.Invoke(null, new object[] { bindings });
            setFamily.Invoke(null, new[] { gamepad });
            var gamepadDisplay = (string)display.Invoke(null, new object[] { bindings });

            Assert.IsNotEmpty(keyboardDisplay);
            Assert.IsNotEmpty(gamepadDisplay);
            Assert.AreNotEqual(keyboardDisplay, gamepadDisplay);
            StringAssert.DoesNotContain("<Keyboard>", keyboardDisplay);
            StringAssert.DoesNotContain("<Gamepad>", gamepadDisplay);
        }

        [Test]
        public void UiMotionSettings_DisablesButtonFadeCardPulseAndReplayMotionTogether()
        {
            var settingsType = typeof(UiFactory).Assembly.GetType(
                "LearnHearthstone.Presentation.Common.UnityUiMotionSettings");
            Assert.IsNotNull(settingsType, "UI1 requires one shared reduced-motion setting.");
            var reduceMotion = settingsType.GetProperty("ReduceMotion", BindingFlags.Public | BindingFlags.Static);
            var duration = settingsType.GetMethod("Duration", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(reduceMotion);
            Assert.IsNotNull(duration);

            var root = new GameObject("ReducedMotionRoot", typeof(RectTransform));
            try
            {
                reduceMotion.SetValue(null, true);
                Assert.AreEqual(0f, (float)duration.Invoke(null, new object[] { 0.58f }), 0.001f);
                Assert.IsTrue(UnityTavernCardComponent.ReduceTargetingMotion);

                var button = UiFactory.Button("ReducedMotionButton", root.transform, "Confirm", () => { });
                Assert.AreEqual(0f, button.colors.fadeDuration, 0.001f);

                var tileObject = new GameObject(
                    "ReducedMotionReplayTile",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(UnityTavernReplayTileAnimator));
                tileObject.transform.SetParent(root.transform, false);
                var animator = tileObject.GetComponent<UnityTavernReplayTileAnimator>();
                animator.Configure(UnityTavernReplayTileMotion.Strike, Color.white, 1f);
                Assert.IsFalse(animator.HasMotion);
            }
            finally
            {
                if (reduceMotion != null)
                {
                    reduceMotion.SetValue(null, false);
                }

                Object.DestroyImmediate(root);
            }
        }

        [TestCase(844, 390)]
        [TestCase(994, 384)]
        [TestCase(1000, 600)]
        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void Ui1Metrics_HoldAtRequiredAcceptanceResolution(int width, int height)
        {
            var root = new GameObject("Ui1ResolutionRoot", typeof(RectTransform));
            try
            {
                var layout = UnityTavernLayoutContext.ForSize(width, height);
                var scale = ReadScale(layout);
                var button = UiFactory.Button("ResolutionButton", root.transform, "Confirm", () => { }, layout);
                Assert.GreaterOrEqual(button.GetComponent<LayoutElement>().minHeight * scale, 48f - 0.01f);
                Assert.GreaterOrEqual(button.GetComponentInChildren<Text>().resizeTextMinSize * scale, 14f - 0.01f);

                UiFactory.ScrollView("ResolutionScroll", root.transform, Color.clear, out var scrollRect, layout);
                var scrollbarWidth = scrollRect.verticalScrollbar.GetComponent<RectTransform>().sizeDelta.x;
                Assert.GreaterOrEqual(scrollbarWidth * scale, 20f - 0.01f);

                var titleSafe = (Rect)typeof(UiFactory).Assembly
                    .GetType("LearnHearthstone.Presentation.Common.UnitySafeAreaPanel")
                    .GetMethod("CalculateSafeRect", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { width, height, new Rect(0f, 0f, width, height), true });
                Assert.GreaterOrEqual(titleSafe.xMin, width * 0.05f - 0.01f);
                Assert.GreaterOrEqual(titleSafe.yMin, height * 0.05f - 0.01f);
                Assert.LessOrEqual(titleSafe.xMax, width * 0.95f + 0.01f);
                Assert.LessOrEqual(titleSafe.yMax, height * 0.95f + 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Bootstrap_CreatesTitleSafeRouteRootWithoutRaycastBlocker()
        {
            var canvasObject = new GameObject("SafeAreaCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var routeRoot = LearnHearthstone.Presentation.LearnHearthstoneBootstrap.CreateSafeAreaRoot(
                    canvasObject.transform);
                Assert.AreEqual("LearnHearthstoneSafeArea", routeRoot.name);
                Assert.IsNotNull(routeRoot.GetComponent(
                    typeof(UiFactory).Assembly.GetType("LearnHearthstone.Presentation.Common.UnitySafeAreaPanel")));
                Assert.IsNull(routeRoot.GetComponent<Graphic>());
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void SafeAreaPanel_RefreshesAnchorsWhenInsetsChangeAtSameResolution()
        {
            var root = new GameObject("SafeAreaRefreshRoot", typeof(RectTransform));
            var panelObject = new GameObject(
                "SafeAreaPanel",
                typeof(RectTransform),
                typeof(UnitySafeAreaPanel));
            panelObject.transform.SetParent(root.transform, false);
            try
            {
                var panel = panelObject.GetComponent<UnitySafeAreaPanel>();
                panel.Refresh(844, 390, new Rect(0f, 0f, 844f, 390f));
                var rect = panelObject.GetComponent<RectTransform>();
                var initialAnchorMin = rect.anchorMin;

                panel.Refresh(844, 390, new Rect(100f, 0f, 644f, 390f));

                Assert.AreNotEqual(initialAnchorMin, rect.anchorMin);
                Assert.AreEqual(100f / 844f, rect.anchorMin.x, 0.0001f);
                Assert.AreEqual(744f / 844f, rect.anchorMax.x, 0.0001f);
                Assert.AreEqual(0.05f, rect.anchorMin.y, 0.0001f);
                Assert.AreEqual(0.95f, rect.anchorMax.y, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertPhysicalConversion(
            UnityTavernLayoutContext layout,
            float expectedScale,
            PropertyInfo scaleProperty,
            MethodInfo convertMethod)
        {
            var scale = (float)scaleProperty.GetValue(layout);
            var canvasUnits = (float)convertMethod.Invoke(layout, new object[] { 48f });
            Assert.AreEqual(expectedScale, scale, 0.0001f);
            Assert.AreEqual(48f, canvasUnits * scale, 0.01f);
        }

        private static float ReadScale(UnityTavernLayoutContext layout)
        {
            return (float)typeof(UnityTavernLayoutContext)
                .GetProperty("CanvasScaleFactor")
                .GetValue(layout);
        }

        private static void AssertRect(Rect rect, float xMin, float yMin, float xMax, float yMax)
        {
            Assert.AreEqual(xMin, rect.xMin, 0.01f);
            Assert.AreEqual(yMin, rect.yMin, 0.01f);
            Assert.AreEqual(xMax, rect.xMax, 0.01f);
            Assert.AreEqual(yMax, rect.yMax, 0.01f);
        }
    }
}
