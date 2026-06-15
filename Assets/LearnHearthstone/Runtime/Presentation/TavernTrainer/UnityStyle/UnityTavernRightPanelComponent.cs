using System;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public enum UnityTavernInspectorTab
    {
        Actions,
        Details,
        Advice,
        Logs
    }

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
            ConfigurePanelChrome();

            if (HasPrefabReferences())
            {
                RemoveTabRow();
                SetText(titleText, title);
                ConfigureHeader(ResolveHeaderTransform());
                ConfigureFloatingToggle(floating, toggleFloating);
                BuildSection(actionParent, buildActions);
                BuildSection(detailParent, buildDetail);
                BuildSection(advisorParent, buildAdvisor);
                BuildSection(logParent, buildLog);
                return;
            }

            BuildGenerated(title, floating, toggleFloating, buildActions, buildDetail, buildAdvisor, buildLog);
        }

        public void BuildTabbed(
            string title,
            bool floating,
            Action toggleFloating,
            UnityTavernInspectorTab activeTab,
            Action<UnityTavernInspectorTab> changeTab,
            Action<Transform> buildActions,
            Action<Transform> buildDetail,
            Action<Transform> buildAdvisor,
            Action<Transform> buildLog)
        {
            ConfigurePanelChrome();

            if (HasPrefabReferences())
            {
                SetText(titleText, title);
                ConfigureHeader(ResolveHeaderTransform());
                ConfigureFloatingToggle(floating, toggleFloating);
                BuildTabRow(activeTab, changeTab);
                BuildTabbedSection(actionParent, activeTab == UnityTavernInspectorTab.Actions, buildActions);
                BuildTabbedSection(detailParent, activeTab == UnityTavernInspectorTab.Details, buildDetail);
                BuildTabbedSection(advisorParent, activeTab == UnityTavernInspectorTab.Advice, buildAdvisor);
                BuildTabbedSection(logParent, activeTab == UnityTavernInspectorTab.Logs, buildLog);
                return;
            }

            BuildGeneratedTabbed(title, floating, toggleFloating, activeTab, changeTab, buildActions, buildDetail, buildAdvisor, buildLog);
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
            ConfigureHeader(header.transform);

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

        private void BuildGeneratedTabbed(
            string title,
            bool floating,
            Action toggleFloating,
            UnityTavernInspectorTab activeTab,
            Action<UnityTavernInspectorTab> changeTab,
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
            ConfigureHeader(header.transform);

            var titleLabel = UiFactory.Label("UnityRightPanelTitle", header.transform, title, 18, FontStyle.Bold);
            titleLabel.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(titleLabel.gameObject, 1f, 0f);

            floatingToggleButton = CreateFloatingToggle(header.transform, out floatingToggleText);
            ConfigureFloatingToggle(floating, toggleFloating);

            BuildTabRow(activeTab, changeTab);

            var activeHost = new GameObject("UnityRightPanelActiveHost", typeof(RectTransform));
            activeHost.transform.SetParent(transform, false);
            UnityTavernUiStyle.SetFlexible(activeHost, 1f, 1f);
            ConfigureSectionHost(activeHost.transform, true);

            switch (activeTab)
            {
                case UnityTavernInspectorTab.Details:
                    buildDetail?.Invoke(activeHost.transform);
                    break;
                case UnityTavernInspectorTab.Advice:
                    buildAdvisor?.Invoke(activeHost.transform);
                    break;
                case UnityTavernInspectorTab.Logs:
                    buildLog?.Invoke(activeHost.transform);
                    break;
                default:
                    buildActions?.Invoke(activeHost.transform);
                    break;
            }
        }

        private void ConfigureFloatingToggle(bool floating, Action toggleFloating)
        {
            if (floatingToggleButton == null)
            {
                return;
            }

            var toggleImage = ConfigureFloatingToggleChrome(floatingToggleButton.gameObject);
            floatingToggleButton.targetGraphic = toggleImage;
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

            parent.gameObject.SetActive(true);
            ClearChildren(parent);
            ConfigureSectionHost(parent, true);
            build?.Invoke(parent);
        }

        private static void BuildTabbedSection(Transform parent, bool active, Action<Transform> build)
        {
            if (parent == null)
            {
                return;
            }

            parent.gameObject.SetActive(active);
            ClearChildren(parent);
            ConfigureSectionHost(parent, active);
            if (active)
            {
                build?.Invoke(parent);
            }
        }

        private void BuildTabRow(UnityTavernInspectorTab activeTab, Action<UnityTavernInspectorTab> changeTab)
        {
            RemoveTabRow();

            var tabRow = new GameObject("UnityRightPanelTabs", typeof(RectTransform));
            tabRow.transform.SetParent(transform, false);
            var header = titleText != null ? titleText.transform.parent : null;
            if (header != null && header.parent == transform)
            {
                tabRow.transform.SetSiblingIndex(header.GetSiblingIndex() + 1);
            }

            UnityTavernUiStyle.SetPreferredHeight(tabRow, 42f);
            ConfigureTabRow(tabRow);
            var layout = tabRow.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddTabButton(tabRow.transform, "UnityRightPanelTab-Actions", "操作", UnityTavernInspectorTab.Actions, activeTab, changeTab);
            AddTabButton(tabRow.transform, "UnityRightPanelTab-Details", "详情", UnityTavernInspectorTab.Details, activeTab, changeTab);
            AddTabButton(tabRow.transform, "UnityRightPanelTab-Advice", "建议", UnityTavernInspectorTab.Advice, activeTab, changeTab);
            AddTabButton(tabRow.transform, "UnityRightPanelTab-Logs", "日志", UnityTavernInspectorTab.Logs, activeTab, changeTab);
        }

        private void RemoveTabRow()
        {
            for (var index = transform.childCount - 1; index >= 0; index -= 1)
            {
                var child = transform.GetChild(index);
                if (child.name == "UnityRightPanelTabs")
                {
                    if (UnityEngine.Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        private static Button AddTabButton(
            Transform parent,
            string name,
            string text,
            UnityTavernInspectorTab tab,
            UnityTavernInspectorTab activeTab,
            Action<UnityTavernInspectorTab> changeTab)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFlexible(buttonObject, 1f, 0f);

            var selected = tab == activeTab;
            var image = buttonObject.GetComponent<Image>();
            image.color = selected ? UnityTavernUiStyle.TableLit : UnityTavernUiStyle.PanelRaised;
            image.raycastTarget = true;

            var outline = UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                selected ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.72f) : new Color(0f, 0f, 0f, 0.24f),
                new Vector2(1.2f, -1.2f));
            outline.enabled = selected;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => changeTab?.Invoke(tab));
            UnityTavernUiStyle.TintSelectable(
                button,
                selected ? new Color(1f, 0.91f, 0.62f, 1f) : Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 12, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = selected ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private void ConfigurePanelChrome()
        {
            UnityTavernUiStyle.ConfigureSurface(gameObject, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.ConfigureOutline(
                gameObject,
                new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.28f),
                new Vector2(1.5f, -1.5f));
        }

        private Transform ResolveHeaderTransform()
        {
            if (titleText != null)
            {
                return titleText.transform.parent;
            }

            return floatingToggleButton != null ? floatingToggleButton.transform.parent : null;
        }

        private static void ConfigureHeader(Transform header)
        {
            if (header == null)
            {
                return;
            }

            var image = UnityTavernUiStyle.ConfigureSurface(header.gameObject, UnityTavernUiStyle.PanelRaised);
            image.enabled = true;
            UnityTavernUiStyle.ConfigureOutline(
                header.gameObject,
                new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.18f),
                new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(header.gameObject, 40f);

            var layout = UnityTavernUiStyle.EnsureComponent<HorizontalLayoutGroup>(header.gameObject);
            layout.padding = new RectOffset(8, 6, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var accent = header.Find("UnityRightPanelHeaderAccent");
            if (accent == null)
            {
                var accentObject = new GameObject("UnityRightPanelHeaderAccent", typeof(RectTransform), typeof(Image));
                accentObject.transform.SetParent(header, false);
                accent = accentObject.transform;
            }

            accent.SetAsFirstSibling();
            UnityTavernUiStyle.SetFixedSize(accent.gameObject, 4f, 28f);
            UnityTavernUiStyle.ConfigureSurface(accent.gameObject, UnityTavernUiStyle.Gold);
        }

        private static void ConfigureTabRow(GameObject tabRow)
        {
            var image = UnityTavernUiStyle.ConfigureSurface(tabRow, UnityTavernUiStyle.Panel);
            image.enabled = true;
            UnityTavernUiStyle.ConfigureOutline(
                tabRow,
                new Color(0f, 0f, 0f, 0.36f),
                new Vector2(1f, -1f));
        }

        private static void ConfigureSectionHost(Transform parent, bool active)
        {
            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(parent.gameObject);
            element.flexibleHeight = active ? 1f : 0f;

            var image = UnityTavernUiStyle.EnsureComponent<Image>(parent.gameObject);
            image.enabled = active;
            image.color = new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.54f);
            image.raycastTarget = false;

            var outline = UnityTavernUiStyle.EnsureComponent<Outline>(parent.gameObject);
            outline.enabled = active;
            outline.effectColor = new Color(0f, 0f, 0f, 0.28f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;

            if (!active)
            {
                return;
            }

            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(parent.gameObject);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
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
            UnityTavernUiStyle.SetFixedSize(buttonObject, 78f, 32f);

            var image = ConfigureFloatingToggleChrome(buttonObject);

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

        private static Image ConfigureFloatingToggleChrome(GameObject buttonObject)
        {
            var image = UnityTavernUiStyle.ConfigureSurface(buttonObject, UnityTavernUiStyle.PanelRaised, true);
            UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.26f),
                new Vector2(1f, -1f));
            return image;
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
