using System;
using TMPro;
using UnityEngine;

public class AirTowerHealth : MonoBehaviour
{
    public int maxHealth = 31;
    public int currentHealth;
    public TextMeshPro healthText;

    [Header("Takım Ayarı")]
    public string teamTag;

    public event Action OnHealthChanged;
    public int teamID;

    protected virtual void Awake()
    {
        Application.targetFrameRate = 60;
        teamTag = gameObject.tag;
        // 🔑 Can değeri AirGameManager tarafından InitializeTower ile atanacağı için, 
        // burada güvenli bir başlangıç değeri veriyoruz.
        currentHealth = 0;
        RefreshHealthUI();
    }

    public virtual void TakeDamage(int amount, string attackerTeamTag)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        RefreshHealthUI();
        StartCoroutine(PunchScaleEffect());
        // AirTowerHealth.cs içinde TakeDamage kısmı
        if (AirCameraShake.Instance != null)
        {
            AirCameraShake.Instance.Shake(0.1f, 0.05f);
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetTrigger("Attack");

        if (currentHealth <= 0)
            ConvertTower(attackerTeamTag);
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public virtual void InitializeTower(int health, int team)
    {
        maxHealth = 31;
        currentHealth = Mathf.Clamp(health, 0, 30);
        this.teamID = team;

        this.teamTag = (teamID == 1) ? "RedTower" : "BlueTower";
        gameObject.tag = this.teamTag;

        RefreshHealthUI();
    }

    public void AddSoldiers(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, 30);
        RefreshHealthUI();
    }

    private void RefreshHealthUI()
    {
        UpdateHealthUI();
        OnHealthChanged?.Invoke();
    }

    public void UpdateHealthUI()
    {
        if (healthText == null) return;
        currentHealth = Mathf.Clamp(currentHealth, 0, 30);
        healthText.text = currentHealth >= 30 ? "MAX" : currentHealth.ToString();
    }

    protected virtual void ConvertTower(string attackerTeamTag)
    {
        if (attackerTeamTag == this.teamTag) return;

        AirGameManager.Instance?.OnTowerConverted(this.teamTag, attackerTeamTag);

        GameObject newTowerPrefab = null;
        int newTeamID = -1;

        // 🔑 DÜZELTİLDİ: AirGameManager'daki prefabları kullan
        if (attackerTeamTag == "RedTower")
        {
            newTowerPrefab = AirGameManager.Instance.redTowerPrefab;
            newTeamID = 1;
        }
        else if (attackerTeamTag == "BlueTower")
        {
            newTowerPrefab = AirGameManager.Instance.blueTowerPrefab;
            newTeamID = 0;
        }

        if (newTowerPrefab != null && newTeamID != -1)
        {
            VibrationManager.Vibrate(100);
            GameObject newTower = Instantiate(newTowerPrefab, transform.position, transform.rotation, AirGameManager.Instance.levelContainer);

            // 🔑 DÜZELTİLDİ: AirTowerHealth bileşenini ara
            AirTowerHealth newTowerHealth = newTower.GetComponent<AirTowerHealth>();

            if (newTowerHealth != null)
            {
                newTowerHealth.InitializeTower(UnityEngine.Random.Range(1, 2), newTeamID);
            }
        }
        Destroy(gameObject);
    }
    // Scriptin içine şu Coroutine'i ekle
    private System.Collections.IEnumerator PunchScaleEffect()
    {
        Vector3 originalScale = Vector3.one; // Eğer kulelerin ana ölçeği farklıysa onu buraya yazabilirsin
        Vector3 punchScale = originalScale * 1.2f; // %20 büyüme

        // Hızlıca büyü
        float elapsed = 0f;
        float duration = 0.05f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Yavaşça eski haline dön
        elapsed = 0f;
        duration = 0.1f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
    }
}