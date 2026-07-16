using System;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernDiscoverModalComponent : MonoBehaviour
    {
        public const string DiscoverModalPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Modals/DiscoverModal.prefab";
        public const string DiscoverModalPrefabResourcePath = "TavernTrainer/UnityStyle/Modals/DiscoverModal";

        [SerializeField] private Text titleText;
        [SerializeField] private Transform optionParent;

        public static GameObject CreateModalHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var modalObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernDiscoverModalComponent));

            modalObject.name = fallbackName;
            modalObject.transform.SetParent(parent, false);
            if (modalObject.GetComponent<Image>() == null)
            {
                modalObject.AddComponent<Image>();
            }

            if (modalObject.GetComponent<UnityTavernDiscoverModalComponent>() == null)
            {
                modalObject.AddComponent<UnityTavernDiscoverModalComponent>();
            }

            return modalObject;
        }

        public void ConfigureReferences(Text title = null, Transform options = null)
        {
            titleText = title;
            optionParent = options;
        }

        public void SetBackdropRaycastBlocking(bool blocksRaycasts)
        {
            var image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            image.raycastTarget = blocksRaycasts;
        }

        public void Build(string title, Action<Transform> buildOptions)
        {
            var image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            image.color = new Color(0f, 0f, 0f, 0.68f);
            image.raycastTarget = true;
            UnityTavernUiStyle.Stretch(gameObject.GetComponent<RectTransform>());

            if (HasPrefabReferences())
            {
                SetText(titleText, title);
                UnityTavernUiStyle.ConfigureLabel(titleText, UnityTavernUiStyle.TextLight, 14);
                var panel = titleText != null ? titleText.transform.parent : optionParent != null ? optionParent.parent : null;
                ConfigurePanelChrome(panel == null ? null : panel.gameObject);
                if (optionParent != null)
                {
                    ClearChildren(optionParent);
                    buildOptions?.Invoke(optionParent);
                }

                return;
            }

            BuildGenerated(title, buildOptions);
        }

        private void BuildGenerated(string title, Action<Transform> buildOptions)
        {
            ClearChildren(transform);

            var panel = new GameObject("UnityDiscoverPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            ConfigurePanelChrome(panel);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560f, 310f);
            rect.anchoredPosition = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 18);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var titleLabel = UiFactory.Label("UnityDiscoverTitle", panel.transform, title, 20, FontStyle.Bold);
            UnityTavernUiStyle.ConfigureLabel(titleLabel, UnityTavernUiStyle.TextLight, 14);
            titleLabel.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetPreferredHeight(titleLabel.gameObject, 34f);

            var options = new GameObject("UnityDiscoverOptions", typeof(RectTransform));
            options.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetFlexible(options, 1f, 1f);
            var rowLayout = options.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;

            buildOptions?.Invoke(options.transform);
        }

        private static void ConfigurePanelChrome(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            UnityTavernUiStyle.ConfigureSurface(panel, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.62f),
                new Vector2(1.5f, -1.5f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityDiscoverStarLantern", UnityTavernUiStyle.ArcaneBlue);
        }

        private bool HasPrefabReferences()
        {
            return titleText != null || optionParent != null;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static GameObject ResolvePrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DiscoverModalPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(DiscoverModalPrefabResourcePath);
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
