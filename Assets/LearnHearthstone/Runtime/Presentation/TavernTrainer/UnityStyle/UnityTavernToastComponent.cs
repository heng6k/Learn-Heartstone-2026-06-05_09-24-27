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
            Build(message, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.DangerRed, 0.96f));
        }

        public void Build(string message, Color backgroundColor)
        {
            var image = UnityTavernUiStyle.ConfigureSurface(gameObject, backgroundColor);
            UnityTavernUiStyle.ConfigureOutline(
                gameObject,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.72f),
                new Vector2(1.5f, -1.5f));
            ConfigureRect(gameObject.GetComponent<RectTransform>());

            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
                UnityTavernUiStyle.ConfigureLabel(messageText, UnityTavernUiStyle.TextLight, 14);
                return;
            }

            ClearChildren(transform);
            var label = UiFactory.Label("UnityErrorToastText", transform, message, 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.ConfigureLabel(label, UnityTavernUiStyle.TextLight, 14);
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
            rect.sizeDelta = new Vector2(520f, UnityTavernUiStyle.TouchHeight);
            rect.anchoredPosition = new Vector2(0f, 32f);
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
