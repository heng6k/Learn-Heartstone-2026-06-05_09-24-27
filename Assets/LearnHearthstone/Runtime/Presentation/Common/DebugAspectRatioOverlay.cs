using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.Common
{
    public static class DebugAspectRatioOverlay
    {
        public const string OverlayName = "DebugAspectRatioOverlay";
        public const string ButtonName = "DebugAspectRatioButton";
        public const string ModalOverlayName = "DebugAspectRatioModalOverlay";
        public const string ModalPanelName = "DebugAspectRatioModalPanel";
        public const string CurrentLabelName = "DebugAspectRatioCurrentLabel";
        public const string PresetButtonPrefix = "DebugAspectRatioPreset-";

        private static readonly AspectRatioPreset[] Presets =
        {
            new AspectRatioPreset("恢复大屏 1920x1080", 1920, 1080),
            new AspectRatioPreset("小窗 994x384", 994, 384),
            new AspectRatioPreset("16:9 1280x720", 1280, 720),
            new AspectRatioPreset("4:3 1024x768", 1024, 768),
            new AspectRatioPreset("9:16 540x960", 540, 960)
        };

        public static void Build(Transform root, Action<int, int> applyResolution = null)
        {
            if (root == null)
            {
                return;
            }

            var existing = FindChild(root, OverlayName);
            if (existing != null)
            {
                DestroyUi(existing.gameObject);
            }

            var overlay = new GameObject(OverlayName, typeof(RectTransform));
            overlay.transform.SetParent(root, false);
            UiFactory.Stretch(overlay.GetComponent<RectTransform>());
            overlay.transform.SetAsLastSibling();

            CreateButton(overlay.transform, applyResolution ?? ApplyResolution);
        }

        private static void CreateButton(Transform parent, Action<int, int> applyResolution)
        {
            var button = UiFactory.Button(ButtonName, parent, "窗口比例", () => ToggleModal(parent, applyResolution));
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(132f, 44f);
            rect.anchoredPosition = new Vector2(-16f, 16f);

            var image = button.GetComponent<Image>();
            image.color = new Color(0.18f, 0.25f, 0.29f, 0.94f);
            button.targetGraphic = image;
        }

        private static void ToggleModal(Transform overlay, Action<int, int> applyResolution)
        {
            var existing = FindChild(overlay, ModalOverlayName);
            if (existing != null)
            {
                DestroyUi(existing.gameObject);
                return;
            }

            ShowModal(overlay, applyResolution);
        }

        private static void ShowModal(Transform overlay, Action<int, int> applyResolution)
        {
            var modal = new GameObject(ModalOverlayName, typeof(RectTransform), typeof(Image), typeof(Button));
            modal.transform.SetParent(overlay, false);
            UiFactory.Stretch(modal.GetComponent<RectTransform>());
            modal.transform.SetAsLastSibling();

            var backdrop = modal.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.34f);
            backdrop.raycastTarget = true;

            var backdropButton = modal.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = backdrop;
            backdropButton.onClick.AddListener(() => DestroyUi(modal));

            var panel = UiFactory.Panel(ModalPanelName, modal.transform, new Color(0.11f, 0.15f, 0.17f, 0.98f));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.sizeDelta = new Vector2(300f, 292f);
            panelRect.anchoredPosition = new Vector2(-16f, 68f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.raycastTarget = true;

            var layout = UiFactory.Vertical(panel, 10, 4);
            layout.childForceExpandHeight = false;

            var title = UiFactory.Label("DebugAspectRatioTitle", panel.transform, "窗口比例调试", 18, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetHeight(title.gameObject, 24f);

            var current = UiFactory.Label(CurrentLabelName, panel.transform, "当前 " + Screen.width + "x" + Screen.height, 14);
            current.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetHeight(current.gameObject, 20f);

            foreach (var preset in Presets)
            {
                var captured = preset;
                var presetButton = UiFactory.Button(
                    PresetButtonPrefix + captured.Width + "x" + captured.Height,
                    panel.transform,
                    captured.Label,
                    () =>
                    {
                        applyResolution(captured.Width, captured.Height);
                        DestroyUi(modal);
                    });
                UiFactory.SetHeight(presetButton.gameObject, 30f);
            }

            var close = UiFactory.Button("DebugAspectRatioCloseButton", panel.transform, "关闭", () => DestroyUi(modal));
            UiFactory.SetHeight(close.gameObject, 30f);
        }

        private static void ApplyResolution(int width, int height)
        {
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            TryApplyEditorGameViewPreset(width, height);
        }

        private static void TryApplyEditorGameViewPreset(int width, int height)
        {
            if (!UnityEngine.Application.isEditor)
            {
                return;
            }

            try
            {
                var toolType = FindType("LearnHearthstone.Editor.GameViewAspectPresetTool");
                var method = toolType?.GetMethod(
                    "ApplyDebugPreset",
                    BindingFlags.Public | BindingFlags.Static);
                method?.Invoke(null, new object[] { width, height, PresetLabelFor(width, height) });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to switch Unity Game View preset: " + exception.Message);
            }
        }

        private static Type FindType(string fullName)
        {
            var direct = Type.GetType(fullName + ", LearnHearthstone.Editor");
            if (direct != null)
            {
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var index = 0; index < assemblies.Length; index += 1)
            {
                var type = assemblies[index].GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string PresetLabelFor(int width, int height)
        {
            for (var index = 0; index < Presets.Length; index += 1)
            {
                var preset = Presets[index];
                if (preset.Width == width && preset.Height == height)
                {
                    return preset.Label;
                }
            }

            return width + "x" + height;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var found = FindChild(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void DestroyUi(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private readonly struct AspectRatioPreset
        {
            public AspectRatioPreset(string label, int width, int height)
            {
                Label = label;
                Width = width;
                Height = height;
            }

            public string Label { get; }

            public int Width { get; }

            public int Height { get; }
        }
    }
}
