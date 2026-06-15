using System;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernLogPanelComponent : MonoBehaviour
    {
        public const string RecruitLogPanelPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Panels/RecruitLogPanel.prefab";
        public const string CombatLogPanelPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Panels/CombatLogPanel.prefab";
        public const string RecruitLogPanelPrefabResourcePath = "TavernTrainer/UnityStyle/Panels/RecruitLogPanel";
        public const string CombatLogPanelPrefabResourcePath = "TavernTrainer/UnityStyle/Panels/CombatLogPanel";

        [SerializeField] private Text titleText;
        [SerializeField] private Transform contentParent;
        [SerializeField] private ScrollRect scrollRect;

        public static GameObject CreatePanelHost(Transform parent, string fallbackName, bool combatLog)
        {
            var prefab = ResolvePrefab(combatLog);
            var panelObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernLogPanelComponent));

            panelObject.name = fallbackName;
            panelObject.transform.SetParent(parent, false);
            if (panelObject.GetComponent<Image>() == null)
            {
                panelObject.AddComponent<Image>();
            }

            if (panelObject.GetComponent<UnityTavernLogPanelComponent>() == null)
            {
                panelObject.AddComponent<UnityTavernLogPanelComponent>();
            }

            return panelObject;
        }

        public void ConfigureReferences(Text title = null, Transform content = null, ScrollRect scroll = null)
        {
            titleText = title;
            contentParent = content;
            scrollRect = scroll;
        }

        public void Build(string title, Action<Transform> buildLines)
        {
            ConfigurePanelSurface(gameObject);

            ConfigureRootLayout(gameObject);
            if (HasPrefabReferences())
            {
                SetText(titleText, title);
                BuildSection(contentParent, buildLines);
                return;
            }

            BuildGenerated(title, buildLines);
        }

        public static void ConfigureRootLayout(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target.gameObject);
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static void ConfigureContentLayout(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target.gameObject);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigurePanelSurface(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.34f),
                new Vector2(1f, -1f));
        }

        private void BuildGenerated(string title, Action<Transform> buildLines)
        {
            ClearChildren(transform);

            var heading = UiFactory.Label("UnityLogTitle", transform, title, 13, FontStyle.Bold);
            heading.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(heading.gameObject, 22f);

            var content = UiFactory.ScrollView("UnityLogScrollView", transform, UnityTavernUiStyle.Panel, out scrollRect);
            ConfigureContentLayout(content.gameObject);
            buildLines?.Invoke(content);
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
            return titleText != null || contentParent != null || scrollRect != null;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static GameObject ResolvePrefab(bool combatLog)
        {
#if UNITY_EDITOR
            var assetPath = combatLog ? CombatLogPanelPrefabAssetPath : RecruitLogPanelPrefabAssetPath;
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(combatLog ? CombatLogPanelPrefabResourcePath : RecruitLogPanelPrefabResourcePath);
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
