using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LearnHearthstone.Editor
{
    public static class GameViewAspectPresetTool
    {
        private static readonly GameViewPreset[] Presets =
        {
            new GameViewPreset("恢复大屏 1920x1080", 1920, 1080),
            new GameViewPreset("小窗 994x384", 994, 384),
            new GameViewPreset("16:9 1280x720", 1280, 720),
            new GameViewPreset("4:3 1024x768", 1024, 768),
            new GameViewPreset("9:16 540x960", 540, 960)
        };

        [MenuItem("Learn Heartstone/Debug/Add Game View Size Presets")]
        public static void AddDebugPresets()
        {
            for (var index = 0; index < Presets.Length; index += 1)
            {
                AddOrFindPreset(Presets[index].Width, Presets[index].Height, Presets[index].Label);
            }
        }

        public static bool ApplyDebugPreset(int width, int height, string label)
        {
            try
            {
                var presetIndex = AddOrFindPreset(width, height, label);
                SelectGameViewSize(presetIndex);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to apply Game View preset " + width + "x" + height + ": " + exception.Message);
                return false;
            }
        }

        private static int AddOrFindPreset(int width, int height, string label)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var gameViewSizeType = RequiredType(assembly, "UnityEditor.GameViewSize");
            var gameViewSizeTypeEnum = RequiredType(assembly, "UnityEditor.GameViewSizeType");
            var group = GetStandaloneGroup(assembly);

            var existingIndex = FindPresetIndex(group, width, height);
            if (existingIndex >= 0)
            {
                return existingIndex;
            }

            var fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
            var constructor = gameViewSizeType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) },
                null);
            if (constructor == null)
            {
                throw new MissingMethodException("UnityEditor.GameViewSize constructor not found.");
            }

            var displayLabel = "LH " + label;
            var size = constructor.Invoke(new[] { fixedResolution, width, height, displayLabel });
            var addCustomSize = RequiredMethod(group.GetType(), "AddCustomSize");
            addCustomSize.Invoke(group, new[] { size });

            return FindPresetIndex(group, width, height);
        }

        private static object GetStandaloneGroup(Assembly assembly)
        {
            var sizesType = RequiredType(assembly, "UnityEditor.GameViewSizes");
            var groupType = RequiredType(assembly, "UnityEditor.GameViewSizeGroupType");
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = RequiredProperty(singletonType, "instance").GetValue(null, null);
            var standalone = Enum.Parse(groupType, "Standalone");
            return RequiredMethod(sizesType, "GetGroup").Invoke(instance, new[] { standalone });
        }

        private static int FindPresetIndex(object group, int width, int height)
        {
            var groupType = group.GetType();
            var getTotalCount = RequiredMethod(groupType, "GetTotalCount");
            var getGameViewSize = RequiredMethod(groupType, "GetGameViewSize");
            var total = (int)getTotalCount.Invoke(group, null);
            for (var index = 0; index < total; index += 1)
            {
                var size = getGameViewSize.Invoke(group, new object[] { index });
                if (ReadInt(size, "width") == width && ReadInt(size, "height") == height)
                {
                    return index;
                }
            }

            return -1;
        }

        private static void SelectGameViewSize(int index)
        {
            if (index < 0)
            {
                throw new InvalidOperationException("Game View preset was not created.");
            }

            var assembly = typeof(UnityEditor.Editor).Assembly;
            var gameViewType = RequiredType(assembly, "UnityEditor.GameView");
            var gameView = EditorWindow.GetWindow(gameViewType);
            var selectedSizeIndex = gameViewType.GetProperty(
                "selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (selectedSizeIndex == null || !selectedSizeIndex.CanWrite)
            {
                throw new MissingMemberException("UnityEditor.GameView.selectedSizeIndex setter not found.");
            }

            selectedSizeIndex.SetValue(gameView, index, null);
            gameView.Repaint();
        }

        private static int ReadInt(object target, string name)
        {
            var type = target.GetType();
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return Convert.ToInt32(property.GetValue(target, null));
            }

            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return Convert.ToInt32(field.GetValue(target));
            }

            throw new MissingMemberException(type.FullName, name);
        }

        private static Type RequiredType(Assembly assembly, string fullName)
        {
            var type = assembly.GetType(fullName);
            if (type == null)
            {
                throw new TypeLoadException(fullName);
            }

            return type;
        }

        private static MethodInfo RequiredMethod(Type type, string name)
        {
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, name);
            }

            return method;
        }

        private static PropertyInfo RequiredProperty(Type type, string name)
        {
            var property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                throw new MissingMemberException(type.FullName, name);
            }

            return property;
        }

        private readonly struct GameViewPreset
        {
            public GameViewPreset(string label, int width, int height)
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
