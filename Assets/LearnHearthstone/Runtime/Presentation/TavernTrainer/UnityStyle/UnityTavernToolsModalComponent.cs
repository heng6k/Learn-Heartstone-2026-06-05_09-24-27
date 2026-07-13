using System;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernToolsModalComponent : MonoBehaviour
    {
        public const string ToolsModalPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Modals/TrainerToolsModal.prefab";
        public const string ToolsModalPrefabResourcePath = "TavernTrainer/UnityStyle/Modals/TrainerToolsModal";

        [SerializeField] private Text titleText;
        [SerializeField] private Transform contentParent;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text closeButtonText;

        public static GameObject CreateModalHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var modalObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernToolsModalComponent));

            modalObject.name = fallbackName;
            modalObject.transform.SetParent(parent, false);
            if (modalObject.GetComponent<Image>() == null)
            {
                modalObject.AddComponent<Image>();
            }

            if (modalObject.GetComponent<UnityTavernToolsModalComponent>() == null)
            {
                modalObject.AddComponent<UnityTavernToolsModalComponent>();
            }

            return modalObject;
        }

        public void ConfigureReferences(Text title = null, Transform content = null, Button close = null, Text closeLabel = null)
        {
            titleText = title;
            contentParent = content;
            closeButton = close;
            closeButtonText = closeLabel;
        }

        public void Build(string title, Action<Transform> buildContent, Action close)
        {
            ConfigureOverlay(gameObject);
            if (HasPrefabReferences())
            {
                SetText(titleText, title);
                ConfigureChromeFromReferences();
                ConfigureClose(close);
                BuildSection(contentParent, buildContent);
                return;
            }

            BuildGenerated(title, buildContent, close);
        }

        public static void ConfigureOverlay(GameObject target)
        {
            UnityTavernUiStyle.Stretch(target.GetComponent<RectTransform>());
            var image = UnityTavernUiStyle.EnsureComponent<Image>(target);
            image.color = new Color(0f, 0f, 0f, 0.50f);
            image.raycastTarget = true;
        }

        public static void ConfigurePanel(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.22f, 0.16f);
            rect.anchorMax = new Vector2(0.78f, 0.84f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void ConfigurePanelLayout(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target);
            layout.padding = new RectOffset(18, 18, 16, 18);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static void ConfigureContentLayout(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target);
            layout.padding = new RectOffset(10, 10, 10, 12);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static void ConfigurePanelChrome(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.26f),
                new Vector2(1.5f, -1.5f));
        }

        public static void ConfigureHeader(Transform header)
        {
            if (header == null)
            {
                return;
            }

            UnityTavernUiStyle.ConfigureSurface(header.gameObject, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.ConfigureOutline(
                header.gameObject,
                new Color(0f, 0f, 0f, 0.32f),
                new Vector2(1f, -1f));
            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(header.gameObject);
            element.minHeight = 54f;
            element.preferredHeight = 54f;
            element.flexibleHeight = 0f;

            var layout = UnityTavernUiStyle.EnsureComponent<HorizontalLayoutGroup>(header.gameObject);
            layout.padding = new RectOffset(8, 6, 5, 5);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var accent = header.Find("UnityTrainerToolsHeaderAccent");
            if (accent == null)
            {
                var accentObject = new GameObject("UnityTrainerToolsHeaderAccent", typeof(RectTransform), typeof(Image));
                accentObject.transform.SetParent(header, false);
                accent = accentObject.transform;
            }

            accent.SetAsFirstSibling();
            UnityTavernUiStyle.SetFixedSize(accent.gameObject, 4f, 28f);
            UnityTavernUiStyle.ConfigureSurface(accent.gameObject, UnityTavernUiStyle.Blue);
        }

        public static Image ConfigureCloseButtonChrome(GameObject buttonObject)
        {
            UnityTavernUiStyle.SetFixedSize(buttonObject, 92f, 44f);
            var image = UnityTavernUiStyle.ConfigureSurface(buttonObject, UnityTavernUiStyle.PanelRaised, true);
            UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.22f),
                new Vector2(1f, -1f));
            return image;
        }

        private void BuildGenerated(string title, Action<Transform> buildContent, Action close)
        {
            ClearChildren(transform);

            var panel = new GameObject("UnityTrainerToolsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            ConfigurePanel(panel.GetComponent<RectTransform>());
            ConfigurePanelChrome(panel);
            ConfigurePanelLayout(panel);

            var header = new GameObject("UnityTrainerToolsHeader", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            ConfigureHeader(header.transform);

            titleText = UiFactory.Label("UnityTrainerToolsTitle", header.transform, title, 20, FontStyle.Bold);
            titleText.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(titleText.gameObject, 1f, 0f);
            closeButton = CreateCloseButton(header.transform, out closeButtonText);
            ConfigureClose(close);

            contentParent = UiFactory.ScrollView("UnityTrainerToolsScroll", panel.transform, UnityTavernUiStyle.Panel, out _);
            ConfigureContentLayout(contentParent.gameObject);
            BuildSection(contentParent, buildContent);
        }

        private void ConfigureClose(Action close)
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.targetGraphic = ConfigureCloseButtonChrome(closeButton.gameObject);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => close?.Invoke());
            SetText(closeButtonText, "关闭");
            if (closeButtonText != null)
            {
                closeButtonText.fontSize = Mathf.Max(14, closeButtonText.fontSize);
            }
        }

        private void ConfigureChromeFromReferences()
        {
            var header = titleText != null ? titleText.transform.parent : closeButton != null ? closeButton.transform.parent : null;
            if (header != null && header.parent != null)
            {
                ConfigurePanel(header.parent.GetComponent<RectTransform>());
                ConfigurePanelChrome(header.parent.gameObject);
                ConfigurePanelLayout(header.parent.gameObject);
            }

            ConfigureHeader(header);
            if (closeButton != null)
            {
                closeButton.targetGraphic = ConfigureCloseButtonChrome(closeButton.gameObject);
            }
        }

        private static void BuildSection(Transform parent, Action<Transform> build)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            ConfigureContentLayout(parent.gameObject);
            build?.Invoke(parent);
        }

        private bool HasPrefabReferences()
        {
            return titleText != null || contentParent != null || closeButton != null;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static Button CreateCloseButton(Transform parent, out Text label)
        {
            var buttonObject = new GameObject("UnityTrainerToolsCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(buttonObject, 92f, 44f);
            var image = ConfigureCloseButtonChrome(buttonObject);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            UnityTavernUiStyle.TintSelectable(button, Color.white, new Color(1f, 0.91f, 0.62f, 1f), new Color(0.72f, 0.62f, 0.42f, 1f));

            label = UiFactory.Label("UnityTrainerToolsCloseText", buttonObject.transform, "关闭", 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private static GameObject ResolvePrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ToolsModalPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(ToolsModalPrefabResourcePath);
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index -= 1)
            {
                var child = parent.GetChild(index).gameObject;
                if (UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
        }
    }
}
