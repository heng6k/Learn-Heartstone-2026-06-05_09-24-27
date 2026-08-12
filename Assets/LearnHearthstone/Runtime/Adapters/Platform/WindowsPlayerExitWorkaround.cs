#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine;

namespace LearnHearthstone.Adapters.Platform
{
    /// <summary>
    /// Installs a narrowly scoped native guard for Unity 6000.4.10f1's Windows
    /// PlatformAccessibilityManager shutdown access violation.
    /// </summary>
    internal static class WindowsPlayerExitWorkaround
    {
        private const string AffectedUnityVersion = "6000.4.10f1";

        [DllImport(
            "LearnHearthstone.WindowsExitGuard",
            ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int InstallExitGuard();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (UnityEngine.Application.unityVersion != AffectedUnityVersion)
            {
                return;
            }

            int result = InstallExitGuard();
            if (result < 0)
            {
                Debug.LogError($"Windows exit guard failed to install: {result}");
            }
        }
    }
}
#endif
