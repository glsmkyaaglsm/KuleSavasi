using UnityEngine;

public class TowerVisualManager : MonoBehaviour
{
    public GameObject singleCubePrefab;
    public GameObject doubleCubePrefab;
    public GameObject tripleCubePrefab;
    private GameObject currentVisual;
    private TowerHealth towerHealth;

    private void Awake()
    {
        towerHealth = GetComponent<TowerHealth>();
        if (towerHealth == null)
        {
            Debug.LogError("TowerHealth component bulunamadý!");
            return;
        }
        towerHealth.OnHealthChanged += UpdateVisual;
    }

    private void Start()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
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
