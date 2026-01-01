using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Sýnýf adý AirLevelSaver olmalý
public static class AirLevelSaver
{
    // Level kayýtlarýnda "Air" önekini kullanýr
    private const string PREFIX = "Air";

    public static void SaveLevel(LevelConfig level, int levelNumber)
    {
        if (level == null) return;

        // Anahtar: Level_Air_[LevelNumber]
        string key = $"Level_{PREFIX}_{levelNumber}";

        string json = JsonUtility.ToJson(level);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        Debug.Log($"Air Level {levelNumber} kaydedildi.");
    }

    public static LevelConfig LoadLevel(int levelNumber)
    {
        // Anahtar: Level_Air_[LevelNumber]
        string key = $"Level_{PREFIX}_{levelNumber}";

        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            LevelConfig level = JsonUtility.FromJson<LevelConfig>(json);
            Debug.Log($"Air Level {levelNumber} yüklendi.");
            return level;
        }
        return null;
    }
}