using System;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernSelectedCardPanelComponent : MonoBehaviour
    {
        public const string SelectedCardPanelPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Panels/SelectedCardDetailPanel.prefab";
        public const string SelectedCardPanelPrefabResourcePath = "TavernTrainer/UnityStyle/Panels/SelectedCardDetailPanel";

        [SerializeField] private Transform contentParent;

        public static GameObject CreatePanelHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var panelObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernSelectedCardPanelComponent));

            panelObject.name = fallbackName;
            panelObject.transform.SetParent(parent, false);
            if (panelObject.GetComponent<Image>() == null)
            {
                panelObject.AddComponent<Image>();
            }

            if (panelObject.GetComponent<UnityTavernSelectedCardPanelComponent>() == null)
            {
                panelObject.AddComponent<UnityTavernSelectedCardPanelComponent>();
            }

            return panelObject;
        }

        public void ConfigureReferences(Transform content = null)
        {
            contentParent = content;
        }

        public void Build(Action<Transform> buildContent)
        {
            ConfigurePanelSurface(gameObject);

            var target = contentParent ?? transform;
            ConfigureLayout(target.gameObject);
            ClearChildren(target);
            buildContent?.Invoke(target);
        }

        public static void ConfigureLayout(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target.gameObject);
            layout.padding = new RectOffset(12, 12, 14, 14);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigurePanelSurface(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.16f),
                new Vector2(1f, -1f));
        }

        private static GameObject ResolvePrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SelectedCardPanelPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(SelectedCardPanelPrefabResourcePath);
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
