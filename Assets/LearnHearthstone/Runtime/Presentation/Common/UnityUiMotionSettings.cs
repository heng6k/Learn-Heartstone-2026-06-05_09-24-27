using System;
using UnityEngine;

namespace LearnHearthstone.Presentation.Common
{
    public static class UnityUiMotionSettings
    {
        private static bool reduceMotion;

        public static event Action<bool> Changed;

        public static bool ReduceMotion
        {
            get => reduceMotion;
            set
            {
                if (reduceMotion == value)
                {
                    return;
                }

                reduceMotion = value;
                Changed?.Invoke(value);
            }
        }

        public static float Duration(float regularDuration)
        {
            return reduceMotion ? 0f : Mathf.Max(0f, regularDuration);
        }
    }
}
