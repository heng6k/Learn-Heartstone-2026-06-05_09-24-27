using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer;
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

        private void Awake()
        {
            EnsureEventSystem();
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            matchService = MatchService.CreateWithDefaultCatalog();
            advisor = new LocalAdvisorService();
            ShowHub();
        }

        private void ShowHub()
        {
            ClearCanvas();
            new MainHubView(canvas.transform, ShowTrainer).Build();
        }

        private void ShowTrainer()
        {
            ClearCanvas();
            new TavernTrainerView(canvas.transform, matchService, advisor, ShowHub).Build();
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("LearnHearthstoneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var created = canvasObject.GetComponent<Canvas>();
            created.renderMode = RenderMode.ScreenSpaceOverlay;
            created.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            return created;
        }

        private void ClearCanvas()
        {
            for (var index = canvas.transform.childCount - 1; index >= 0; index -= 1)
            {
                Destroy(canvas.transform.GetChild(index).gameObject);
            }
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
