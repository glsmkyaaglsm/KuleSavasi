using System;
using TMPro;
using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    public int maxHealth = 71;
    public int currentHealth;
    public TextMeshPro healthText;

    [Header("Takým Ayarý")]
    public string teamTag;

    public event Action OnHealthChanged;
    public int teamID;

    protected virtual void Awake()
    {
        Application.targetFrameRate = 60;
        teamTag = gameObject.tag;
        currentHealth = Mathf.Min(maxHealth, 30);
        RefreshHealthUI();
    }

    public virtual void TakeDamage(int amount, string attackerTeamTag)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        RefreshHealthUI();

        // --- EKLENDÝ: vurulunca animasyon oynat ---
        Animator anim = GetComponent<Animator>();
        StartCoroutine(PunchScaleEffect());

        // AirTowerHealth.cs içinde TakeDamage kýsmý
        if (AirCameraShake.Instance != null)
        {
            AirCameraShake.Instance.Shake(0.1f, 0.05f);
        }
        if (anim != null)
            anim.SetTrigger("Attack");
        // --------------------------------------------

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
        healthText.text = currentHealth >= 30 ? "MAX" : currentHealth.ToString();
    }

    protected virtual void ConvertTower(string attackerTeamTag)
    {
        if (attackerTeamTag == this.teamTag) return;

        Debug.Log($"{gameObject.name}, {attackerTeamTag} tarafýndan ele geçirildi!");

        GameManager.Instance?.OnTowerConverted(this.teamTag, attackerTeamTag);

        GameObject newTowerPrefab = null;

        int newTeamID = -1;

        if (attackerTeamTag == "RedTower")
        {
            newTowerPrefab = GameManager.Instance.redTowerPrefab;
            newTeamID = 1;
        }
        else if (attackerTeamTag == "BlueTower")
        {
            newTowerPrefab = GameManager.Instance.blueTowerPrefab;
            newTeamID = 0;
        }

        if (newTowerPrefab != null && newTeamID != -1)
        {
            VibrationManager.Vibrate(100);
            GameObject newTower = Instantiate(newTowerPrefab, transform.position, transform.rotation);
            TowerHealth newTowerHealth = newTower.GetComponent<TowerHealth>();

            if (newTowerHealth != null)
            {
                newTowerHealth.InitializeTower(UnityEngine.Random.Range(1, 2), newTeamID);
            }
        }
        Destroy(gameObject);
    }
    private System.Collections.IEnumerator PunchScaleEffect()
    {
        Vector3 originalScale = Vector3.one; // Eðer kulelerin ana ölçeði farklýysa onu buraya yazabilirsin
        Vector3 punchScale = originalScale * 1.2f; // %20 büyüme

        // Hýzlýca büyü
        float elapsed = 0f;
        float duration = 0.05f;
        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Yavaþça eski haline dön
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