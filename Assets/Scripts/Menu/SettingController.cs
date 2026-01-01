using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingController : MonoBehaviour
{
    // --- INSPECTOR ATAMALARI ---
    

    [Header("Mixer")]
    // Inspector'da bağlayacağımız Audio Mixer
    public AudioMixer mainMixer;


    [Header("Titreşim Ayarları")]
    public Toggle vibrationToggle;
    public Image vibrationBackgroundImage;
    private const string VIBRATION_PREF_KEY = "VibrationToggleState";

    [Header("Toggle Sprite Görselleri")]
    // Açık durumdaki (Mavi) Toggle görseli
    public Sprite onSprite;
    // Kapalı durumdaki (Kırmızı) Toggle görseli
    public Sprite offSprite;

    // --- MÜZİK AYARLARI ---
    [Header("Müzik Ayarları")]
    public Toggle musicToggle;
    // Toggle'ın arka plan görseli (değiştireceğimiz Image bileşeni)
    public Image musicBackgroundImage;
    // Audio Mixer'daki parametre adı
    public string musicParamName = "Music";
    private const string MUSIC_PREF_KEY = "MusicToggleState";

    // --- SES AYARLARI ---
    [Header("Ses Ayarları")]
    public Toggle soundToggle;
    // Toggle'ın arka plan görseli (değiştireceğimiz Image bileşeni)
    public Image soundBackgroundImage;
    // Audio Mixer'daki parametre adı
    public string soundParamName = "Sound";
    private const string SOUND_PREF_KEY = "SoundToggleState";

    // Audio Mixer'da ses açma/kapama için kullanılacak logaritmik değerler
    private const float MAX_VOLUME = 0f;    // Ses Açık (Tam Ses)
    private const float MIN_VOLUME = -80f;  // Ses Kapalı (Mute)

    // ---------------------------------------------------------------------

    void Start()
    {
        // 1. Toggle'lara dinleyici (listener) ekle
        // Değer değiştiğinde OnToggleChanged metodu çalışacak.
        musicToggle.onValueChanged.AddListener(delegate { OnToggleChanged(musicToggle, musicBackgroundImage, musicParamName, MUSIC_PREF_KEY); });
        soundToggle.onValueChanged.AddListener(delegate { OnToggleChanged(soundToggle, soundBackgroundImage, soundParamName, SOUND_PREF_KEY); });
        vibrationToggle.onValueChanged.AddListener(delegate { OnVibrationToggleChanged(); });
        InitializeVibrationToggle();

        // 2. Kayıtlı durumları yükle ve ilk ayarı yap.
        // InitializeToggle, toggle durumunu PlayerPrefs'ten yükler ve hemen ardından 
        // OnToggleChanged'i çağırarak ses ve sprite ayarlarını garanti eder.
        InitializeToggle(musicToggle, musicBackgroundImage, musicParamName, MUSIC_PREF_KEY);
        InitializeToggle(soundToggle, soundBackgroundImage, soundParamName, SOUND_PREF_KEY);
    }

    /// <summary>
    /// Toggle'ın başlangıç durumunu PlayerPrefs'ten yükler ve ardından OnToggleChanged'i manuel tetikler.
    /// Bu, oyun açılışında kayıtlı ayarın uygulanmasını garanti eder.
    /// </summary>
    private void InitializeToggle(Toggle toggle, Image targetImage, string paramName, string prefKey)
    {
        // PlayerPrefs'ten kaydedilmiş değeri yükle. (Varsayılan: 1 yani Açık)
        // 1 = True (Açık), 0 = False (Kapalı)
        int savedState = PlayerPrefs.GetInt(prefKey, 1);
        bool isOn = savedState == 1;

        // Toggle görsel bileşeninin (handle) durumunu PlayerPrefs'ten gelen değere ayarla
        toggle.isOn = isOn;

        // Toggle'ın ilk ayarlarını (Ses, Sprite, Kayıt) yapmak için OnToggleChanged'i manuel olarak çağır.
        // Bu, kayıtlı durumun **kesinlikle** sesi ve görseli ayarlamasını sağlar.
        OnToggleChanged(toggle, targetImage, paramName, prefKey);
    }

    /// <summary>
    /// Toggle değeri değiştiğinde veya oyun açıldığında InitializeToggle tarafından manuel çağrıldığında çalışır.
    /// Sesi ve sprite'ı günceller ve ayarı PlayerPrefs'e kaydeder.
    /// </summary>
    public void OnToggleChanged(Toggle toggle, Image targetImage, string paramName, string prefKey)
    {
        bool isOn = toggle.isOn;
        float volume = isOn ? MAX_VOLUME : MIN_VOLUME;
        int stateToSave = isOn ? 1 : 0;

        // Sesi Audio Mixer'da ayarla
        SetVolume(paramName, volume);
        VibrationManager.Vibrate(50);

        // Sprite'ı değiştir (Açık/Kapalı görseli)
        SetToggleSprite(targetImage, isOn);

        // Ayarı kalıcı olarak kaydet
        PlayerPrefs.SetInt(prefKey, stateToSave);
        PlayerPrefs.Save(); // Değişikliği diske kaydet
    }


    // Ses seviyesini Audio Mixer'da ayarlayan fonksiyon
    private void SetVolume(string parameterName, float volume)
    {
        if (mainMixer != null)
        {
            // Belirtilen parametreye ses seviyesini ayarla (logaritmik)
            mainMixer.SetFloat(parameterName, volume);
        }
        else
        {
            Debug.LogError("Audio Mixer, SettingController'a atanmamış! Lütfen Inspector'dan atayın.");
        }
    }

    /// <summary>
    /// Toggle'ın görsel Sprite'ını değiştirir (onSprite/offSprite).
    /// </summary>
    private void SetToggleSprite(Image targetImage, bool isOn)
    {
        if (targetImage != null)
        {
            targetImage.sprite = isOn ? onSprite : offSprite;
        }
    }
    private void InitializeVibrationToggle()
    {
        int savedState = PlayerPrefs.GetInt(VIBRATION_PREF_KEY, 1);
        vibrationToggle.isOn = savedState == 1;
        OnVibrationToggleChanged();
    }

    public void OnVibrationToggleChanged()
    {
        bool isOn = vibrationToggle.isOn;
        SetToggleSprite(vibrationBackgroundImage, isOn);
        PlayerPrefs.SetInt(VIBRATION_PREF_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
        VibrationManager.Vibrate(50);

    }
}