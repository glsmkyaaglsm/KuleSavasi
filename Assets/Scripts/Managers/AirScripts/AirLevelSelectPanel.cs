using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AirLevelSelectPanel : MonoBehaviour
{
    private const string LEVEL_PREFIX = "Air";

    [Header("Level Button Prefab")]
    public GameObject levelButtonPrefab;
    public Transform buttonContainer;
    public int totalLevels = 100;
    public GameObject airPlaneTowersPanel;


    private void OnEnable()
    {
        PopulateLevelButtons();
    }

    private void PopulateLevelButtons()
    {
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        int highestUnlockedLevel = PlayerPrefs.GetInt(LEVEL_PREFIX + "_HighestLevelReached", 1);

        for (int i = 1; i <= totalLevels; i++)
        {
            GameObject buttonObj = Instantiate(levelButtonPrefab, buttonContainer);
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Button button = buttonObj.GetComponent<Button>();

            if (buttonText != null) buttonText.text = i.ToString();

            if (i <= highestUnlockedLevel)
            {
                button.interactable = true;
                int levelToLoad = i;
                button.onClick.AddListener(() => LoadSelectedLevel(levelToLoad));
            }
            else
            {
                button.interactable = false;
            }
        }
    }

    private void LoadSelectedLevel(int level)
    {
        PlayerPrefs.SetInt(LEVEL_PREFIX + "_CurrentLevel", level);

        // Sahneyi YÜKLEMİYORUZ (SceneManager.LoadScene SİLİNDİ)

        // AirGameManager'a eriş ve her şeyi baştan kurmasını söyle
        AirGameManager manager = Object.FindFirstObjectByType<AirGameManager>();
        if (manager != null)
        {
            manager.StopAllCoroutines(); // Varsa eski işlemleri durdur
            StartCoroutine(manager.GenerateLevel());
        }

        OpenSpecificMenu(); // Artık çalışacaktır çünkü sahne hala aynı sahne!
    }


    void OpenSpecificMenu()
    {
        // Örneğin başka bir sayfadaki butonun yaptığı işi burada çağırın
        Debug.Log("Sahne yüklendi ve diğer sayfadaki işlem başlatıldı!");

    }
}