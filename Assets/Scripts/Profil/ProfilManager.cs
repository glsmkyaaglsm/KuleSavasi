using UnityEngine;
using TMPro; // TextMeshPro kullanýyorsan
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    [Header("UI Elemanlarý")]
    public TMP_InputField nameInput;
    public TMP_InputField emailInput;
    public TextMeshProUGUI levelText;

    void Start()
    {
        // Sayfa açýldýðýnda eski verileri yükle
        LoadProfile();
    }

    public void SaveProfile()
    {
        // Ýsim ve Mail'i kaydet
        PlayerPrefs.SetString("PlayerName", nameInput.text);
        PlayerPrefs.SetString("PlayerEmail", emailInput.text);
        PlayerPrefs.Save();

        Debug.Log("Profil Kaydedildi!");
        VibrationManager.Vibrate(50); // Küçük bir "kaydedildi" titreþimi ;)
    }

    public void LoadProfile()
    {
        nameInput.text = PlayerPrefs.GetString("PlayerName", "Misafir");
        emailInput.text = PlayerPrefs.GetString("PlayerEmail", "Ornek@mail.com");

        // BURAYI DÜZELTTÝK: AirGameManager'ýn kullandýðý anahtarýn aynýsýný yazdýk
        int ulasilanLevel = PlayerPrefs.GetInt("Air_CurrentLevel", 1);

        levelText.text = ulasilanLevel.ToString();
    }
}