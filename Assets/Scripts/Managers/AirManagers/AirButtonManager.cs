using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AirButtonManager : MonoBehaviour
{
    public static AirButtonManager Instance;

    [Header("UI")]
    public GameObject startMenuPanel;       // Başlangıç menüsü paneli
    public GameObject levelEndPanel;        // Seviye bitiş paneli
    public TextMeshProUGUI levelEndText;    // Seviye bitiş mesajı
    public GameObject nextLevelButton;      // Sonraki level butonu
    public GameObject mainMenuButton;       // Ana menü butonu
    public GameObject rStart;               // Hızlı yeniden başlat butonu (rStart)
    public GameObject restartButton;        // Yeniden başlat butonu (Level Bitişi)
    public GameObject pauseButton;          // Pause butonu
    public GameObject pausePanel;           // Pause paneli
    public GameObject seviye;               // Seviye Seçim Paneli (seviye)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Başlangıçta menü ve panel ayarları (Aynı Kalır)
        startMenuPanel?.SetActive(true);
        levelEndPanel?.SetActive(false);
        nextLevelButton?.SetActive(false);
        mainMenuButton?.SetActive(false);
        rStart?.SetActive(false);
        restartButton?.SetActive(false);
        pauseButton?.SetActive(false);
        pausePanel?.SetActive(false);
    }


    public void StartGame()
    {
        // 🔑 AirGameManager ile durumu kontrol et
        if (AirGameManager.currentState != AirGameManager.GameState.Menu) return;

        Time.timeScale = 1f;
        // 🔑 AirGameManager ile durumu güncelle
        AirGameManager.currentState = AirGameManager.GameState.Playing;

        startMenuPanel?.SetActive(false);

        pauseButton?.SetActive(true);
        mainMenuButton?.SetActive(true);
        rStart?.SetActive(true);
        VibrationManager.Vibrate(50);
    }

    public void Seviye()
    {
        seviye?.SetActive(true);
        VibrationManager.Vibrate(50);

    }

    public void SeviyeExit()
    {
        seviye?.SetActive(false);
        VibrationManager.Vibrate(50);

    }

    public void RestartGame()
    {
        restartButton?.SetActive(false);
        levelEndPanel?.SetActive(false);
        VibrationManager.Vibrate(50);

        pauseButton?.SetActive(true);

        // 🔑 AirGameManager'ın Restart metodunu çağır
        AirGameManager.Instance.RestartGameInternal();
    }

    public void LoadNextLevel()
    {
        nextLevelButton?.SetActive(false);
        levelEndPanel?.SetActive(false);
        VibrationManager.Vibrate(50);

        pauseButton?.SetActive(true);

        // 🔑 AirGameManager'ın LoadNextLevel metodunu çağır
        AirGameManager.Instance.LoadNextLevelInternal();
    }

    // Bu metot, AirGameManager tarafından çağrılacaktır.
    public void ShowLevelEndMessage(string message, bool isWin)
    {
        StartCoroutine(LevelEndRoutine(message, isWin));
        VibrationManager.Vibrate(80);

    }

    private IEnumerator LevelEndRoutine(string message, bool isWin)
    {
        Time.timeScale = 0f;
        // 🔑 AirGameManager ile durumu güncelle
        AirGameManager.currentState = AirGameManager.GameState.GameOver;

        // --- UI Görünürlük Kontrolü (Aynı Kalır) ---
        nextLevelButton?.SetActive(false);
        restartButton?.SetActive(false);
        pauseButton?.SetActive(false);

        levelEndPanel?.SetActive(true);
        if (levelEndText != null) levelEndText.text = message;
        levelEndText.transform.localScale = Vector3.zero;

        // ... (Animasyon kodları aynı kalır) ...
        CanvasGroup cg = levelEndPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = levelEndPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
            yield return null;
        }
        cg.alpha = 1f;

        timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            levelEndText.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, Mathf.SmoothStep(0f, 1f, timer / 0.5f));
            yield return null;
        }
        levelEndText.transform.localScale = Vector3.one;

        // 🔑 Konfeti kontrolünü AirGameManager.Instance üzerinden yap
        if (isWin && AirGameManager.Instance.confettiPrefab != null)
        {
            Vector3 spawnPos = levelEndPanel.transform.position;
            spawnPos.y = -5f;
            spawnPos.z = 5f;
            spawnPos.x = 0f;
            // 🔑 Konfeti prefabını AirGameManager'dan al
            Instantiate(AirGameManager.Instance.confettiPrefab, spawnPos, Quaternion.identity);
        }


        if (isWin) { nextLevelButton?.SetActive(true); StartCoroutine(WinVibrationRoutine()); PlayerPrefs.SetInt("LastCompletedLevel", PlayerPrefs.GetInt("LastCompletedLevel", 1) + 1); }

        else { restartButton?.SetActive(true); VibrationManager.Vibrate(200); }
    }
    private IEnumerator WinVibrationRoutine()
    {
        VibrationManager.Vibrate(50);  // tık
        yield return new WaitForSecondsRealtime(0.1f);
        VibrationManager.Vibrate(50);  // tık
        yield return new WaitForSecondsRealtime(0.1f);
        VibrationManager.Vibrate(150); // tııık!
    }


    // --- PAUSE & RESUME ---
    public void PauseGame()
    {
        // 🔑 AirGameManager ile durumu kontrol et
        if (AirGameManager.currentState != AirGameManager.GameState.Playing) return;
        VibrationManager.Vibrate(50);

        Time.timeScale = 0f;
        // 🔑 AirGameManager ile durumu güncelle
        AirGameManager.currentState = AirGameManager.GameState.Paused;

        pausePanel?.SetActive(true);
    }

    public void ResumeGame()
    {
        // 🔑 AirGameManager ile durumu kontrol et
        if (AirGameManager.currentState != AirGameManager.GameState.Paused) return;
        VibrationManager.Vibrate(50);

        Time.timeScale = 1f;
        // 🔑 AirGameManager ile durumu güncelle
        AirGameManager.currentState = AirGameManager.GameState.Playing;

        pausePanel?.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        // 🔑 AirGameManager ile durumu güncelle
        AirGameManager.currentState = AirGameManager.GameState.Menu;

        // Mevcut sahneyi yeniden yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        // 🔑 AirGameManager ile durumu güncelle
        AirGameManager.currentState = AirGameManager.GameState.Playing;
        VibrationManager.Vibrate(50);

        // Bu kısım, Air modundan çıktığınızda hangi ana sahneye döneceğinizi belirler.
        // Eğer Buz Kulesi (MainScene) ana menüsüne dönecekseniz:
        SceneManager.LoadScene("MainScene");

        // Eğer Air modunun kendine ait bir ana menüsü varsa (örneğin "AirMainMenu"):
        // SceneManager.LoadScene("AirMainMenu"); 
    }
}