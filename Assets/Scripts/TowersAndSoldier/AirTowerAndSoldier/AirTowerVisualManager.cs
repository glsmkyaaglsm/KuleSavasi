using UnityEngine;

public class AirTowerVisualManager : MonoBehaviour
{
    public GameObject singleCubePrefab;
    public GameObject doubleCubePrefab;
    public GameObject tripleCubePrefab;
    private GameObject currentVisual;
    // 🔑 DÜZELTİLDİ: AirTowerHealth kullan
    private AirTowerHealth towerHealth;

    private void Awake()
    {
        // 🔑 DÜZELTİLDİ: AirTowerHealth al
        towerHealth = GetComponent<AirTowerHealth>();
        if (towerHealth == null)
        {
            Debug.LogError("AirTowerHealth component bulunamadı!");
            return;
        }
        // 🔑 DÜZELTİLDİ: AirTowerHealth olayına abone ol
        towerHealth.OnHealthChanged += UpdateVisual;
    }

    private void Start()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (towerHealth == null) return;

        GameObject newPrefab = null;

        if (towerHealth.currentHealth <= 9)
            newPrefab = singleCubePrefab;
        else if (towerHealth.currentHealth <= 24)
            newPrefab = doubleCubePrefab;
        else
            newPrefab = tripleCubePrefab;

        if (currentVisual != null && currentVisual.name.Replace("(Clone)", "") == newPrefab.name)
            return;

        if (currentVisual != null)
            Destroy(currentVisual);

        currentVisual = Instantiate(newPrefab, transform.position, transform.rotation, transform);
    }
}