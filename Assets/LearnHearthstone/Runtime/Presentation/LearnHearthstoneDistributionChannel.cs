using UnityEngine;

namespace LearnHearthstone.Presentation
{
    public static class LearnHearthstoneDistributionChannel
    {
        public const int MobileTargetFrameRate = 60;
        public const int MobileQualityLevel = 1;

#if LEARN_HEARTHSTONE_WECHAT_MINIGAME
        public const bool IsWeChatMiniGame = true;
#else
        public const bool IsWeChatMiniGame = false;
#endif

        public static void ConfigureRuntime()
        {
            if (!UnityEngine.Application.isMobilePlatform && !IsWeChatMiniGame)
            {
                return;
            }

            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            if (QualitySettings.names.Length > MobileQualityLevel &&
                QualitySettings.GetQualityLevel() > MobileQualityLevel)
            {
                QualitySettings.SetQualityLevel(MobileQualityLevel, true);
            }

            QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = MobileTargetFrameRate;

#if LEARN_HEARTHSTONE_WECHAT_MINIGAME && UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var wxType = System.Type.GetType("WeChatWASM.WX, Wx", throwOnError: true);
                var getSystemInfo = wxType.GetMethod(
                    "GetSystemInfoSync",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var systemInfo = getSystemInfo?.Invoke(null, null);
                Debug.Log("WeChat mini-game runtime ready: " + systemInfo + ".");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("WeChat runtime information is unavailable: " + exception.Message);
            }
#endif
        }
    }
}
