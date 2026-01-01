using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    public static ButtonManager Instance;

    [Header("UI")]
    public GameObject startMenuPanel;       // Başlangıç menüsü paneli
    public GameObject levelEndPanel;        // Seviye bitiş paneli
    public TextMeshProUGUI levelEndText;    // Seviye bitiş mesajı
    public GameObject nextLevelButton;      // Sonraki level butonu
    public GameObject mainMenuButton;      // Sonraki level butonu
    public GameObject rStart;      // Sonraki level butonu
    public GameObject restartButton;        // Yeniden başlat butonu
    public GameObject pauseButton;          // Pause butonu
    public GameObject pausePanel;           // Pause paneli (devam etme butonu + UI elemanları)
    public GameObject seviye;           // Pause paneli (devam etme butonu + UI elemanları)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Başlangıçta menü ve panel ayarları
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
        if (GameManager.currentState != GameManager.GameState.Menu) return;

        Time.timeScale = 1f;
        GameManager.currentState = GameManager.GameState.Playing;

        startMenuPanel?.SetActive(false);

        // Oyun başladığında pause butonu görünür
        pauseButton?.SetActive(true);
        mainMenuButton?.SetActive(true);
        rStart?.SetActive(true);
        VibrationManager.Vibrate(50);
        // Başlangıç bulanık paneli gizle
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
        // --- Butonları geri getir ---
        pauseButton?.SetActive(true);

        GameManager.Instance.RestartGameInternal();
    }
    


    public void LoadNextLevel()
    {
        nextLevelButton?.SetActive(false);
        levelEndPanel?.SetActive(false);
        VibrationManager.Vibrate(50);
        // --- Butonları geri getir ---
        pauseButton?.SetActive(true);

        GameManager.Instance.LoadNextLevelInternal();
    }

    public void ShowLevelEndMessage(string message, bool isWin)
    {
        StartCoroutine(LevelEndRoutine(message, isWin));
        VibrationManager.Vibrate(80);
    }

    private IEnumerator LevelEndRoutine(string message, bool isWin)
    {
        Time.timeScale = 0f;
        GameManager.currentState = GameManager.GameState.GameOver;

        // --- Tüm butonları önce gizle ---
        nextLevelButton?.SetActive(false);
        restartButton?.SetActive(false);
        pauseButton?.SetActive(false);

        levelEndPanel?.SetActive(true);
        if (levelEndText != null) levelEndText.text = message;
        levelEndText.transform.localScale = Vector3.zero;

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

        if (isWin && GameManager.Instance.confettiPrefab != null)
        {
            Vector3 spawnPos = levelEndPanel.transform.position;
            spawnPos.y = -5f;
            spawnPos.z = 5f;
            spawnPos.x = 0f;
            Instantiate(GameManager.Instance.confettiPrefab, spawnPos, Quaternion.identity);
        }

        // --- Sadece ilgili buton aktif edilsin ---
        if (isWin) { nextLevelButton?.SetActive(true); StartCoroutine(WinVibrationRoutine()); }
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
        if (GameManager.currentState != GameManager.GameState.Playing) return;

        Time.timeScale = 0f;
        GameManager.currentState = GameManager.GameState.Paused;
        VibrationManager.Vibrate(50);
        pausePanel?.SetActive(true);
    }

    public void ResumeGame()
    {
        if (GameManager.currentState != GameManager.GameState.Paused) return;
        VibrationManager.Vibrate(50);
        Time.timeScale = 1f;
        GameManager.currentState = GameManager.GameState.Playing;

        pausePanel?.SetActive(false);
    }
    public void ReturnToMainMenu()
    {
        // 1. Zamanı normal akışına döndür (Pause modunda kalmasın)
        Time.timeScale = 1f;
        VibrationManager.Vibrate(50);
        // 2. Oyun durumunu Menü olarak ayarla (gerçi sahne yenilenince Awake bunu yapacak ama garanti olsun)
        GameManager.currentState = GameManager.GameState.Menu;

        // 3. Mevcut sahneyi yeniden yükle
        // Bu işlem tüm kuleleri siler, puanları sıfırlar ve Awake fonksiyonunu tetikleyerek menüyü açar.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void MainMenu()
    {
        VibrationManager.Vibrate(50);
        // Oyun durumunu güncelleyip zamanı başlatır
        Time.timeScale = 1f;
        GameManager.currentState = GameManager.GameState.Playing;

        // Menü UI'larını gizle (Buton Manager'ınızdaki diğer UI kodları buraya gelir)
        // startMenuPanel?.SetActive(false); 

        // ✅ "defence" sahnesini yükle
        // Mevcut sahneyi boşaltmadan yeni sahneyi yükle
        SceneManager.LoadScene("MainScene");
    }
}