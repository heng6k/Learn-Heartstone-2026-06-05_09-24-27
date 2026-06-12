using System;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernRightPanelComponent : MonoBehaviour
    {
        public const string RightPanelPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Panels/RightInspectorPanel.prefab";
        public const string RightPanelPrefabResourcePath = "TavernTrainer/UnityStyle/Panels/RightInspectorPanel";

        [SerializeField] private Text titleText;
        [SerializeField] private Transform actionParent;
        [SerializeField] private Transform detailParent;
        [SerializeField] private Transform advisorParent;
        [SerializeField] private Transform logParent;
        [SerializeField] private Button floatingToggleButton;
        [SerializeField] private Text floatingToggleText;

        public static GameObject CreatePanelHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var panelObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernRightPanelComponent));

            panelObject.name = fallbackName;
            panelObject.transform.SetParent(parent, false);
            if (panelObject.GetComponent<Image>() == null)
            {
                panelObject.AddComponent<Image>();
            }

            if (panelObject.GetComponent<UnityTavernRightPanelComponent>() == null)
            {
                panelObject.AddComponent<UnityTavernRightPanelComponent>();
            }

            return panelObject;
        }

        public void ConfigureReferences(
            Text title = null,
            Transform actions = null,
            Transform detail = null,
            Transform advisor = null,
            Transform log = null,
            Button floatingToggle = null,
            Text floatingToggleLabel = null)
        {
            titleText = title;
            actionParent = actions;
            detailParent = detail;
            advisorParent = advisor;
            logParent = log;
            floatingToggleButton = floatingToggle;
            floatingToggleText = floatingToggleLabel;
        }

        public void Build(
            string title,
            Action<Transform> buildActions,
            Action<Transform> buildDetail,
            Action<Transform> buildAdvisor,
            Action<Transform> buildLog)
        {
            Build(title, false, null, buildActions, buildDetail, buildAdvisor, buildLog);
        }

        public void Build(
            string title,
            bool floating,
            Action toggleFloating,
            Action<Transform> buildActions,
            Action<Transform> buildDetail,
            Action<Transform> buildAdvisor,
            Action<Transform> buildLog)
        {
            var image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            image.color = UnityTavernUiStyle.PanelQuiet;
            image.raycastTarget = false;

            if (HasPrefabReferences())
            {
                SetText(titleText, title);
                ConfigureFloatingToggle(floating, toggleFloating);
                BuildSection(actionParent, buildActions);
                BuildSection(detailParent, buildDetail);
                BuildSection(advisorParent, buildAdvisor);
                BuildSection(logParent, buildLog);
                return;
            }

            BuildGenerated(title, floating, toggleFloating, buildActions, buildDetail, buildAdvisor, buildLog);
        }

        private void BuildGenerated(
            string title,
            bool floating,
            Action toggleFloating,
            Action<Transform> buildActions,
            Action<Transform> buildDetail,
            Action<Transform> buildAdvisor,
            Action<Transform> buildLog)
        {
            ClearChildren(transform);
            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(gameObject);
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var header = new GameObject("UnityRightPanelHeader", typeof(RectTransform));
            header.transform.SetParent(transform, false);
            UnityTavernUiStyle.SetPreferredHeight(header, 30f);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = true;

            var titleLabel = UiFactory.Label("UnityRightPanelTitle", header.transform, title, 18, FontStyle.Bold);
            titleLabel.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(titleLabel.gameObject, 1f, 0f);

            floatingToggleButton = CreateFloatingToggle(header.transform, out floatingToggleText);
            ConfigureFloatingToggle(floating, toggleFloating);

            buildActions?.Invoke(transform);
            buildDetail?.Invoke(transform);
            buildAdvisor?.Invoke(transform);
            buildLog?.Invoke(transform);
        }

        private void ConfigureFloatingToggle(bool floating, Action toggleFloating)
        {
            if (floatingToggleButton == null)
            {
                return;
            }

            floatingToggleButton.onClick.RemoveAllListeners();
            if (toggleFloating != null)
            {
                floatingToggleButton.onClick.AddListener(() => toggleFloating());
            }

            floatingToggleButton.interactable = toggleFloating != null;
            SetText(floatingToggleText, floating ? "收起" : "展开");
        }

        private static void BuildSection(Transform parent, Action<Transform> build)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            build?.Invoke(parent);
        }

        private bool HasPrefabReferences()
        {
            return titleText != null
                || actionParent != null
                || detailParent != null
                || advisorParent != null
                || logParent != null
                || floatingToggleButton != null
                || floatingToggleText != null;
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
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RightPanelPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(RightPanelPrefabResourcePath);
        }

        private static Button CreateFloatingToggle(Transform parent, out Text label)
        {
            var buttonObject = new GameObject("UnityRightPanelFloatToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(buttonObject, 76f, 30f);

            var image = buttonObject.GetComponent<Image>();
            image.color = UnityTavernUiStyle.PanelRaised;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            label = UiFactory.Label("UnityRightPanelFloatToggleText", buttonObject.transform, "展开", 12, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
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
