using System;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernAdvisorPanelComponent : MonoBehaviour
    {
        public const string AdvisorPanelPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Panels/AdvisorPanel.prefab";
        public const string AdvisorPanelPrefabResourcePath = "TavernTrainer/UnityStyle/Panels/AdvisorPanel";

        [SerializeField] private Text titleText;
        [SerializeField] private Transform contentParent;

        public static GameObject CreatePanelHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var panelObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernAdvisorPanelComponent));

            panelObject.name = fallbackName;
            panelObject.transform.SetParent(parent, false);
            if (panelObject.GetComponent<Image>() == null)
            {
                panelObject.AddComponent<Image>();
            }

            if (panelObject.GetComponent<UnityTavernAdvisorPanelComponent>() == null)
            {
                panelObject.AddComponent<UnityTavernAdvisorPanelComponent>();
            }

            return panelObject;
        }

        public void ConfigureReferences(Text title = null, Transform content = null)
        {
            titleText = title;
            contentParent = content;
        }

        public void Build(string title, Action<Transform> buildLines)
        {
            var image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            image.color = UnityTavernUiStyle.Panel;
            image.raycastTarget = false;

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
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static void ConfigureContentLayout(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target.gameObject);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void BuildGenerated(string title, Action<Transform> buildLines)
        {
            ClearChildren(transform);

            var heading = UiFactory.Label("UnityAdvisorTitle", transform, title, 14, FontStyle.Bold);
            heading.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(heading.gameObject, 22f);

            buildLines?.Invoke(transform);
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
            return titleText != null || contentParent != null;
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
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(AdvisorPanelPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(AdvisorPanelPrefabResourcePath);
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
