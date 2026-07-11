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
        private Canvas canvas;
        private MatchService matchService;
        private IAdvisorService advisor;
        private bool useEnglish;

        private void Awake()
        {
            EnsureEventSystem();
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            ConfigureCanvas(canvas);

            advisor = new LocalAdvisorService();
            ShowHub();
        }

        private void ShowHub()
        {
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
            ClearCanvas();
            new UnityTavernTribeSelectionView(canvas.transform, StartUnityTrainer, ShowHub, useEnglish: useEnglish).Build();
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
            matchService = MatchService.CreateWithDefaultCatalog(
                setup: setup ?? new MatchSetupOptions());
            ClearCanvas();
            new UnityTavernTrainerView(canvas.transform, matchService, advisor, ShowHub, ShowLegacyTrainer).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowRealisticTrainer()
        {
            matchService = MatchService.CreateWithDefaultCatalog();
            ClearCanvas();
            new RealisticTavernTrainerView(canvas.transform, matchService, advisor, ShowHub, ShowLegacyTrainer).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowLegacyTrainer()
        {
            matchService = MatchService.CreateWithDefaultCatalog();
            ClearCanvas();
            new TavernTrainerView(canvas.transform, matchService, advisor, ShowHub).Build();
            AddDebugAspectRatioOverlay();
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
            DebugAspectRatioOverlay.Build(canvas.transform);
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
