using UnityEngine;

public static class VibrationManager
{
    // Android nesnelerini sadece Android platformunda tanýmlýyoruz
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject _vibrator = null;

    private static AndroidJavaObject GetVibrator()
    {
        if (_vibrator == null)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
        }
        return _vibrator;
    }
#endif

    public static void Vibrate(long milliseconds)
    {
        // 1. Kullanýcý ayarý kapalýysa hiç çalýþma
        if (PlayerPrefs.GetInt("VibrationToggleState", 1) == 0) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        try 
        {
            AndroidJavaObject v = GetVibrator();
            if (v != null)
            {
                v.Call("vibrate", milliseconds);
            }
            else
            {
                Debug.LogWarning("Vibrator servisi alýnamadý.");
            }
        } 
        catch (System.Exception e) 
        {
            Debug.LogError("Android Titreþim Hatasý: " + e.Message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#else
        // Editörde sadece konsola yazdýr
        Debug.Log("Titreþim simülasyonu: " + milliseconds + "ms");
#endif
    }
}