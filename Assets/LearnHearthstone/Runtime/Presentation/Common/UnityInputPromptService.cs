using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LearnHearthstone.Presentation.Common
{
    public enum UnityInputDeviceFamily
    {
        KeyboardMouse,
        Gamepad,
        Touch
    }

    public static class UnityInputPromptService
    {
        public static event Action<UnityInputDeviceFamily> DeviceFamilyChanged;

        public static UnityInputDeviceFamily CurrentDeviceFamily { get; private set; } = UnityInputDeviceFamily.KeyboardMouse;

        public static void SetCurrentDeviceFamily(UnityInputDeviceFamily family)
        {
            if (CurrentDeviceFamily == family)
            {
                return;
            }

            CurrentDeviceFamily = family;
            DeviceFamilyChanged?.Invoke(family);
        }

        public static string DisplayNameForBindings(IEnumerable<string> bindingPaths)
        {
            var paths = bindingPaths?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList() ?? new List<string>();
            var selectedPath = paths.FirstOrDefault(path => MatchesFamily(path, CurrentDeviceFamily))
                ?? paths.FirstOrDefault();
            return string.IsNullOrWhiteSpace(selectedPath)
                ? string.Empty
                : InputControlPath.ToHumanReadableString(
                    selectedPath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public static UnityInputDeviceFamily FamilyFor(InputDevice device)
        {
            if (device is Gamepad)
            {
                return UnityInputDeviceFamily.Gamepad;
            }

            return device is Touchscreen
                ? UnityInputDeviceFamily.Touch
                : UnityInputDeviceFamily.KeyboardMouse;
        }

        private static bool MatchesFamily(string path, UnityInputDeviceFamily family)
        {
            switch (family)
            {
                case UnityInputDeviceFamily.Gamepad:
                    return path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase);
                case UnityInputDeviceFamily.Touch:
                    return path.StartsWith("<Touchscreen>", StringComparison.OrdinalIgnoreCase);
                default:
                    return path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase) ||
                           path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase) ||
                           path.StartsWith("<Pen>", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class UnityInputDeviceTracker : MonoBehaviour
    {
        private void OnEnable()
        {
            InputSystem.onEvent += OnInputEvent;
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= OnInputEvent;
        }

        private static void OnInputEvent(InputEventPtr eventPointer, InputDevice device)
        {
            if (device != null)
            {
                UnityInputPromptService.SetCurrentDeviceFamily(UnityInputPromptService.FamilyFor(device));
            }
        }
    }
}
