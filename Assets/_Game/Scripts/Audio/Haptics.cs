using OneBlock.Save;
using UnityEngine;

namespace OneBlock.Audio
{
    /// <summary>Short, amplitude-controlled vibrations on Android (API 26+), respecting the Vibration setting.</summary>
    public static class Haptics
    {
        public enum Strength { Light, Medium, Heavy }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static int _sdk = -1;

        static AndroidJavaObject Vibrator
        {
            get
            {
                if (_vibrator != null) return _vibrator;
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _sdk = version.GetStatic<int>("SDK_INT");
                return _vibrator;
            }
        }
#endif

        public static void Play(Strength strength)
        {
            if (!SaveSystem.Data.hapticsOn) return;
            long ms = strength switch { Strength.Light => 12, Strength.Medium => 28, _ => 60 };
            int amplitude = strength switch { Strength.Light => 70, Strength.Medium => 150, _ => 255 };
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var v = Vibrator;
                if (v == null) return;
                if (_sdk >= 26)
                {
                    using var effect = new AndroidJavaClass("android.os.VibrationEffect");
                    using var oneShot = effect.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                    v.Call("vibrate", oneShot);
                }
                else v.Call("vibrate", ms);
            }
            catch (System.Exception)
            {
                // Some devices have no vibrator service; haptics are a nicety, never an error
            }
#endif
        }

        // Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the Android manifest.
        // This method is never called; the amplitude-controlled path above is used instead.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051")]
        static void RequestPermissionForBuild()
        {
#if UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
