using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernToastComponent : MonoBehaviour
    {
        public const string ErrorToastPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Modals/ErrorToast.prefab";
        public const string ErrorToastPrefabResourcePath = "TavernTrainer/UnityStyle/Modals/ErrorToast";

        [SerializeField] private Text messageText;

        public static GameObject CreateToastHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var toastObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernToastComponent));

            toastObject.name = fallbackName;
            toastObject.transform.SetParent(parent, false);
            if (toastObject.GetComponent<Image>() == null)
            {
                toastObject.AddComponent<Image>();
            }

            if (toastObject.GetComponent<UnityTavernToastComponent>() == null)
            {
                toastObject.AddComponent<UnityTavernToastComponent>();
            }

            return toastObject;
        }

        public void ConfigureReferences(Text message = null)
        {
            messageText = message;
        }

        public void Build(string message)
        {
            Build(message, new Color(0.42f, 0.10f, 0.09f, 0.94f));
        }

        public void Build(string message, Color backgroundColor)
        {
            var image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            image.color = backgroundColor;
            image.raycastTarget = false;
            ConfigureRect(gameObject.GetComponent<RectTransform>());

            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
                return;
            }

            ClearChildren(transform);
            var label = UiFactory.Label("UnityErrorToastText", transform, message, 13, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        public static void ConfigureRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(520f, 42f);
            rect.anchoredPosition = new Vector2(0f, 24f);
        }

        private static GameObject ResolvePrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ErrorToastPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(ErrorToastPrefabResourcePath);
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
