using System;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernActionPanelComponent : MonoBehaviour
    {
        public const string ActionPanelPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Panels/ActionPanel.prefab";
        public const string ActionPanelPrefabResourcePath = "TavernTrainer/UnityStyle/Panels/ActionPanel";

        [SerializeField] private Transform actionParent;

        public static GameObject CreatePanelHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var panelObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernActionPanelComponent));

            panelObject.name = fallbackName;
            panelObject.transform.SetParent(parent, false);
            if (panelObject.GetComponent<Image>() == null)
            {
                panelObject.AddComponent<Image>();
            }

            if (panelObject.GetComponent<UnityTavernActionPanelComponent>() == null)
            {
                panelObject.AddComponent<UnityTavernActionPanelComponent>();
            }

            return panelObject;
        }

        public void ConfigureReferences(Transform actions = null)
        {
            actionParent = actions;
        }

        public void Build(Action<Transform> buildActions)
        {
            ConfigurePanelSurface(gameObject);

            var target = actionParent ?? transform;
            ConfigureGrid(target.gameObject);
            ClearChildren(target);
            buildActions?.Invoke(target);
        }

        public static void ConfigureGrid(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<GridLayoutGroup>(target.gameObject);
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = new Vector2(8, 8);
            layout.cellSize = new Vector2(142f, 40f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
        }

        private static void ConfigurePanelSurface(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.18f),
                new Vector2(1f, -1f));
        }

        private static GameObject ResolvePrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ActionPanelPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(ActionPanelPrefabResourcePath);
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
