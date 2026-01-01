using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectPanel : MonoBehaviour
{
    [Header("Level Button Prefab")]
    public GameObject levelButtonPrefab; // Buton prefab'ý (Text ve Button component içermeli)
    public Transform buttonContainer;    // Butonlarýn konulacaðý panel alt nesnesi
    public int totalLevels = 100;         // Toplam level sayýsý

    private void OnEnable()
    {
        PopulateLevelButtons();
    }

    private void PopulateLevelButtons()
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        // Kilit kontrolü için en yüksek açýlmýþ level kullanýlýyor
        int highestUnlockedLevel = PlayerPrefs.GetInt("HighestLevelReached", 1);

        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject buttonObj = Instantiate(levelButtonPrefab, buttonContainer);
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Button button = buttonObj.GetComponent<Button>();

            if (buttonText != null) buttonText.text = $"{i}";

            if (i <= highestUnlockedLevel)
            {
                button.interactable = true;
                int levelToLoad = i;
                button.onClick.AddListener(() =>
                {
                    LoadSelectedLevel(levelToLoad);
                });
            }
            else
            {
                button.interactable = false;
            }
        }
    }

    private void LoadSelectedLevel(int level)
    {
        // Sadece oynanacak level’i kaydediyoruz, kilitleme için HighestLevelReached kullanýlacak
        PlayerPrefs.SetInt("CurrentLevel", level);
        PlayerPrefs.Save();

        gameObject.SetActive(false);
       
    }

}
