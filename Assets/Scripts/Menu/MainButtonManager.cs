using UnityEngine;
using UnityEngine.SceneManagement;

public class MainButtonManager : MonoBehaviour
{
    // Inspector'da bağlayacağımız UI Paneli GameObject'leri
    [Header("UI Panelleri")]
    public GameObject settingsPanel;
    public GameObject profilePanel; // Yeni eklenen Profil Paneli


    [Header("Level Select Panel")]
    public GameObject levelSelectPanel;   // Level seçme paneli
    public GameObject levelSelectButton;  // Paneli açacak buton


    private void Awake()
    {

        // Level select paneli başlangıçta kapalı
        levelSelectPanel?.SetActive(false);

        if (levelSelectButton != null)
            levelSelectButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(ToggleLevelSelectPanel);
        if (levelSelectButton != null)
        {
            levelSelectButton.GetComponent<UnityEngine.UI.Button>()
                             .onClick.AddListener(() => {
                                 Debug.Log("Level Select Button Tıklandı");
                                 ToggleLevelSelectPanel();
                             });
        }
    }
    public void ToggleLevelSelectPanel()
    {
        if (levelSelectPanel == null) return;

        bool isActive = levelSelectPanel.activeSelf;
        levelSelectPanel.SetActive(!isActive);
        Debug.Log("Panel Aktif mi: " + levelSelectPanel.activeSelf);

        // Opsiyonel: arka plan bulanık paneli aç/kapa
    }
    void Start()
    {
        // Oyun başladığında tüm panelleri gizle
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (profilePanel != null)
        {
            profilePanel.SetActive(false); // Profil paneli de gizlenir
        }
    }
    public void StartGame()
    {
        // Oyun durumunu güncelleyip zamanı başlatır
        Time.timeScale = 1f;
        GameManager.currentState = GameManager.GameState.Playing;
        VibrationManager.Vibrate(50);

        // UI gizleme/gösterme işlemleri...
        // startMenuPanel?.SetActive(false); 

        // ✅ TEKİL YÜKLEME MODUNU KULLANIN
        // Bu, mevcut SAHNEYİ TAMAMEN BOŞALTIP ("destroy") yenisini yükler.
        SceneManager.LoadScene("Defence", LoadSceneMode.Single);

        // NOT: Bu satırdan sonraki kodlar yeni sahne yüklendikten sonra çalışmayacaktır, 
        // GameManager objesi yeni sahnede tekrar kurulmalıdır.
    }
    // --- Ayarlar Paneli Yönetim Fonksiyonları ---

    public void OpenSettingsPanel()
    {
        if (profilePanel != null) profilePanel.SetActive(false); // Profil açıksa kapat
        if (settingsPanel != null) settingsPanel.SetActive(true);
        VibrationManager.Vibrate(50);

    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        VibrationManager.Vibrate(50);

    }

    // --- Profil Paneli Yönetim Fonksiyonları (YENİ) ---

    /// <summary>
    /// Profil Paneli'ni görünür yapar. (Profil Butonu için)
    /// </summary>
    public void OpenProfilePanel()
    {
        VibrationManager.Vibrate(50);

        if (settingsPanel != null) settingsPanel.SetActive(false); // Ayarlar açıksa kapat
        if (profilePanel != null) profilePanel.SetActive(true);
    }

    /// <summary>
    /// Profil Paneli'ni gizler. (X Kapatma Butonu için)
    /// </summary>
    public void CloseProfilePanel()
    {
        VibrationManager.Vibrate(50);

        if (profilePanel != null) profilePanel.SetActive(false);
    }
}