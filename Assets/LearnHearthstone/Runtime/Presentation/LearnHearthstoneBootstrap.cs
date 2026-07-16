using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer;
using LearnHearthstone.Presentation.TavernTrainer.Realistic;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine.InputSystem.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation
{
    public sealed class LearnHearthstoneBootstrap : MonoBehaviour
    {
        private enum ViewRoute
        {
            Hub,
            Setup,
            UnityTrainer,
            RealisticTrainer,
            LegacyTrainer
        }

        private const string UiFontResourcePath = "Fonts/NotoSansSC-Regular";

        private Canvas canvas;
        private MatchService matchService;
        private IAdvisorService advisor;
        private bool useEnglish;
        private ViewRoute currentRoute;
        private UnityTavernTribeSelectionView tribeSelectionView;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private UnityTavernLayoutMode lastLayoutMode;

        private void Awake()
        {
            ConfigureUiFont();
            EnsureEventSystem();
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            ConfigureCanvas(canvas);
            RememberScreenLayout();

            advisor = new LocalAdvisorService();
            ShowHub();
        }

        private void Update()
        {
            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            {
                return;
            }

            var layout = UnityTavernLayoutContext.Current();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            if (layout.Mode == lastLayoutMode)
            {
                return;
            }

            lastLayoutMode = layout.Mode;
            ConfigureCanvas(canvas, layout);
            RebuildCurrentRoute(layout);
        }

        private void ShowHub()
        {
            currentRoute = ViewRoute.Hub;
            tribeSelectionView = null;
            ClearCanvas();
            new MainHubView(
                canvas.transform,
                ShowLegacyTrainer,
                ShowRealisticTrainer,
                ShowUnityTrainer,
                useEnglish: useEnglish,
                languageChanged: SetLanguage).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowUnityTrainer()
        {
            currentRoute = ViewRoute.Setup;
            ClearCanvas();
            tribeSelectionView = new UnityTavernTribeSelectionView(canvas.transform, StartUnityTrainer, ShowHub, useEnglish: useEnglish);
            tribeSelectionView.Build();
            AddDebugAspectRatioOverlay();
        }

        private void SetLanguage(bool nextUseEnglish)
        {
            if (useEnglish == nextUseEnglish)
            {
                return;
            }

            useEnglish = nextUseEnglish;
            ShowHub();
        }

        private void StartUnityTrainer(MatchSetupOptions setup)
        {
            currentRoute = ViewRoute.UnityTrainer;
            tribeSelectionView = null;
            matchService = MatchService.CreateWithDefaultCatalog(
                CreateMatchSeed(),
                setup: setup ?? new MatchSetupOptions());
            ClearCanvas();
            new UnityTavernTrainerView(canvas.transform, matchService, advisor, ShowHub, ShowLegacyTrainer).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowRealisticTrainer()
        {
            currentRoute = ViewRoute.RealisticTrainer;
            tribeSelectionView = null;
            matchService = MatchService.CreateWithDefaultCatalog(CreateMatchSeed());
            ClearCanvas();
            new RealisticTavernTrainerView(canvas.transform, matchService, advisor, ShowHub, ShowLegacyTrainer).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowLegacyTrainer()
        {
            currentRoute = ViewRoute.LegacyTrainer;
            tribeSelectionView = null;
            matchService = MatchService.CreateWithDefaultCatalog(CreateMatchSeed());
            ClearCanvas();
            new TavernTrainerView(canvas.transform, matchService, advisor, ShowHub).Build();
            AddDebugAspectRatioOverlay();
        }

        private void RebuildCurrentRoute(UnityTavernLayoutContext layout)
        {
            switch (currentRoute)
            {
                case ViewRoute.Hub:
                    ShowHub();
                    break;
                case ViewRoute.Setup:
                    tribeSelectionView?.RebuildForLayout(layout);
                    break;
                case ViewRoute.UnityTrainer:
                    canvas.GetComponentInChildren<UnityTavernTrainerController>(true)?.Rebuild();
                    break;
            }
        }

        private void RememberScreenLayout()
        {
            var layout = UnityTavernLayoutContext.Current();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastLayoutMode = layout.Mode;
        }

        private static void ConfigureUiFont()
        {
            var font = Resources.Load<Font>(UiFontResourcePath);
            if (font == null)
            {
                Debug.LogError("Bundled UI font is missing at Resources/" + UiFontResourcePath + ".");
                return;
            }

            UiFactory.SetFontOverride(font);
        }

        private static int CreateMatchSeed()
        {
            return System.Guid.NewGuid().GetHashCode();
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("LearnHearthstoneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            return canvasObject.GetComponent<Canvas>();
        }

        public static void ConfigureCanvas(Canvas target)
        {
            ConfigureCanvas(target, UnityTavernLayoutContext.Current());
        }

        public static void ConfigureCanvas(Canvas target, UnityTavernLayoutContext layout)
        {
            if (target == null)
            {
                return;
            }

            target.renderMode = RenderMode.ScreenSpaceOverlay;
            target.pixelPerfect = false;

            var scaler = UnityTavernUiStyle.EnsureComponent<CanvasScaler>(target.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = layout.IsCompact ? 0f : 0.5f;

            UnityTavernUiStyle.EnsureComponent<GraphicRaycaster>(target.gameObject);
        }

        private void ClearCanvas()
        {
            for (var index = canvas.transform.childCount - 1; index >= 0; index -= 1)
            {
                Destroy(canvas.transform.GetChild(index).gameObject);
            }
        }

        private void AddDebugAspectRatioOverlay()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugAspectRatioOverlay.Build(canvas.transform);
#endif
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            var eventSystemObject = eventSystem != null
                ? eventSystem.gameObject
                : new GameObject("EventSystem", typeof(EventSystem));

            var legacyModule = eventSystemObject.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
                Destroy(legacyModule);
            }

            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
