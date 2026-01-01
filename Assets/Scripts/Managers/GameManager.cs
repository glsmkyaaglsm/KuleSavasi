using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameState currentState = GameState.Menu;
    public enum GameState { Menu, Playing, Paused, GameOver }

    public static GameManager Instance;

    [Header("Kule Prefabları")]
    public GameObject redTowerPrefab;
    public GameObject blueTowerPrefab;

    [Header("Engel Prefabları")]
    public GameObject obstaclePrefab1;
    public GameObject obstaclePrefab2;

    [Header("Spawn Konumları")]
    public Transform[] towerSpawnPositions;
    public Transform[] obstacleSpawnPositions;

    [Header("Hierarchy Düzeni")]
    public Transform levelContainer; 

    [Header("Level Ayarları")]
    public int maxRedTowers = 4;
    public int maxBlueTowers = 3;
    public int maxInitialHealth = 10;
    public int maxObstaclesPerLevel = 2;

    [Header("Level Effects")]
    public GameObject confettiPrefab;

    [Header("Spawn Ayarları")]
    public float spawnClearRadiusFallback = 0.6f;
    public float spawnJitter = 0.08f;

    public LayerMask towerLayerMask;

    private int currentLevel;
    private int redTowerCount;
    private int blueTowerCount;
    private bool isGameEnded;

    private float spawnClearRadius;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Time.timeScale = 0f;
        currentState = GameState.Menu;
    }

    private void Start()
    {
        spawnClearRadius = CalculateSpawnClearance();
        StartCoroutine(GenerateLevel());
    }

    public void OnTowerConverted(string oldTeamTag, string newTeamTag)
    {
        if (isGameEnded) return;

        if (oldTeamTag == "RedTower") redTowerCount--;
        else if (oldTeamTag == "BlueTower") blueTowerCount--;

        if (newTeamTag == "RedTower") redTowerCount++;
        else if (newTeamTag == "BlueTower") blueTowerCount++;

        CheckLevelState();
    }

    private void CheckLevelState()
    {
        if (blueTowerCount <= 0)
        {
            EndLevel(true, "Level Tamamlandı! Tüm kuleler ele geçirildi!");
        }
        else if (redTowerCount <= 0)
        {
            EndLevel(false, "Oyun Bitti! Tüm kuleler kaybedildi!");
        }
    }

    private void EndLevel(bool won, string message)
    {
        isGameEnded = true;
        ButtonManager.Instance.ShowLevelEndMessage(message, won);
    }

    public void LoadNextLevelInternal()
    {
        int savedLevel = PlayerPrefs.GetInt("CurrentLevel", 1);

        int highest = PlayerPrefs.GetInt("HighestLevelReached", 1);
        if (savedLevel + 1 > highest)
            PlayerPrefs.SetInt("HighestLevelReached", savedLevel + 1);

        PlayerPrefs.SetInt("CurrentLevel", savedLevel + 1);
        PlayerPrefs.Save();

        currentLevel = savedLevel + 1;
        Time.timeScale = 1f;
        currentState = GameState.Playing;

        StartCoroutine(GenerateLevel());
    }

    public void RestartGameInternal()
    {
        Time.timeScale = 1f;
        currentState = GameState.Playing;
        StartCoroutine(GenerateLevel());
    }

    private IEnumerator GenerateLevel()
    {
        isGameEnded = false;
        DestroyAllTowersAndObstaclesAndSoldiers();

        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        Random.InitState(currentLevel * 1000);

        yield return null;

        spawnClearRadius = CalculateSpawnClearance();

        LevelConfig loaded = LevelSaver.LoadLevel(currentLevel);

        if (loaded != null && loaded.spawnPositions.Count > 0)
        {
            SpawnLevelFromSave(loaded);
        }
        else
        {
            SpawnLevelRandomly();
        }
    }

    private void SpawnLevelFromSave(LevelConfig loaded)
    {
        redTowerCount = loaded.redCount;
        blueTowerCount = loaded.blueCount;

        int idx = 0;

        // 🔹 Red towers
        for (int i = 0; i < loaded.redTowerHealths.Count; i++)
        {
            Vector3 pos = loaded.spawnPositions[idx];
            // Instantiate'e levelContainer parametresi eklendi
            GameObject tower = Instantiate(redTowerPrefab, pos, Quaternion.identity, levelContainer);
            tower.transform.position = new Vector3(pos.x, pos.y, 0f);
            tower.GetComponent<TowerHealth>()?.InitializeTower(loaded.redTowerHealths[i], 1);
            idx++;
        }

        // 🔹 Blue towers
        for (int i = 0; i < loaded.blueTowerHealths.Count; i++)
        {
            Vector3 pos = loaded.spawnPositions[idx];
            // Instantiate'e levelContainer parametresi eklendi
            GameObject tower = Instantiate(blueTowerPrefab, pos, Quaternion.identity, levelContainer);
            tower.transform.position = new Vector3(pos.x, pos.y, 0f);
            tower.GetComponent<TowerHealth>()?.InitializeTower(loaded.blueTowerHealths[i], 0);
            idx++;
        }

        // 🔹 Engeller
        List<GameObject> availableObstacles = new List<GameObject>();
        if (loaded.useObstacle1 && obstaclePrefab1 != null) availableObstacles.Add(obstaclePrefab1);
        if (loaded.useObstacle2 && obstaclePrefab2 != null) availableObstacles.Add(obstaclePrefab2);

        SpawnObstacles(availableObstacles);
    }

    private void SpawnLevelRandomly()
    {
        List<int> redHealthValues = GenerateTowerHealthValues(maxRedTowers);
        List<int> blueHealthValues = GenerateTowerHealthValues(maxBlueTowers);

        redTowerCount = redHealthValues.Count;
        blueTowerCount = blueHealthValues.Count;

        List<Vector2> recordedSpawnPositions = new List<Vector2>();
        List<int> usedIndexes = new List<int>();

        SpawnTowers_RecordPositions(redTowerPrefab, redHealthValues, true, usedIndexes, recordedSpawnPositions);
        SpawnTowers_RecordPositions(blueTowerPrefab, blueHealthValues, false, usedIndexes, recordedSpawnPositions);

        List<GameObject> availableObstacles = new List<GameObject>();
        if (obstaclePrefab1 != null) availableObstacles.Add(obstaclePrefab1);
        if (obstaclePrefab2 != null) availableObstacles.Add(obstaclePrefab2);

        SpawnObstacles(availableObstacles);

        LevelConfig newLevel = new LevelConfig
        {
            redCount = redHealthValues.Count,
            blueCount = blueHealthValues.Count,
            redTowerHealths = new List<int>(redHealthValues),
            blueTowerHealths = new List<int>(blueHealthValues),
            spawnPositions = new List<Vector2>(recordedSpawnPositions),
            useObstacle1 = obstaclePrefab1 != null,
            useObstacle2 = obstaclePrefab2 != null
        };

        LevelSaver.SaveLevel(newLevel, currentLevel);
    }

    private void SpawnTowers_RecordPositions(GameObject towerPrefab, List<int> healthValues, bool isRedTeam, List<int> usedIndexes, List<Vector2> outSpawnPositions)
    {
        foreach (int health in healthValues)
        {
            bool spawned = false;
            int attempts = 0;

            while (!spawned && attempts < 100)
            {
                attempts++;
                int idx = Random.Range(0, towerSpawnPositions.Length);
                if (usedIndexes.Contains(idx)) continue;

                Vector3 pos = towerSpawnPositions[idx].position;
                if (IsPositionBlocked(pos)) continue;

                Vector3 jitter = new Vector3(Random.Range(-spawnJitter, spawnJitter), Random.Range(-spawnJitter, spawnJitter), 0f);
                Vector3 spawnPos = pos + jitter;
                spawnPos.z = 0f;

                // Instantiate'e levelContainer parametresi eklendi
                GameObject tower = Instantiate(towerPrefab, spawnPos, Quaternion.identity, levelContainer);
                tower.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);
                tower.GetComponent<TowerHealth>()?.InitializeTower(health, isRedTeam ? 1 : 0);

                usedIndexes.Add(idx);
                outSpawnPositions.Add(spawnPos);
                spawned = true;
            }

            if (!spawned) Debug.LogWarning("Tower spawn edilemedi, boş pozisyon bulunamadı.");
        }
    }

    private void SpawnObstacles(List<GameObject> availableObstacles)
    {
        if (availableObstacles.Count == 0) return;

        List<int> usedIndexes = new List<int>();
        int obstaclesToSpawn = Random.Range(1, maxObstaclesPerLevel + 1);

        for (int i = 0; i < obstaclesToSpawn; i++)
        {
            bool spawned = false;
            int attempts = 0;

            while (!spawned && attempts < 100)
            {
                attempts++;
                int idx = Random.Range(0, obstacleSpawnPositions.Length);
                if (usedIndexes.Contains(idx)) continue;

                Vector3 pos = obstacleSpawnPositions[idx].position;
                if (IsPositionBlocked(pos)) continue;

                Vector3 jitter = new Vector3(Random.Range(-spawnJitter, spawnJitter), Random.Range(-spawnJitter, spawnJitter), 0f);
                Vector3 spawnPos = pos + jitter;
                spawnPos.z = 0f;

                // Instantiate'e levelContainer parametresi eklendi
                GameObject obstacle = Instantiate(availableObstacles[Random.Range(0, availableObstacles.Count)], spawnPos, Quaternion.identity, levelContainer);
                obstacle.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);

                usedIndexes.Add(idx);
                spawned = true;
            }

            if (!spawned) Debug.LogWarning("Obstacle spawn edilemedi, boş pozisyon bulunamadı.");
        }
    }

    private void DestroyAllTowersAndObstaclesAndSoldiers()
    {
        foreach (var t in GameObject.FindObjectsByType<TowerHealth>(FindObjectsSortMode.None))
        {
            if (t != null)
                Destroy(t.gameObject);
        }

        foreach (var s in GameObject.FindObjectsByType<Soldier>(FindObjectsSortMode.None))
        {
            if (s != null)
                Destroy(s.gameObject);
        }

        DestroyAllObstacleClones(obstaclePrefab1);
        DestroyAllObstacleClones(obstaclePrefab2);
    }

    private void DestroyAllObstacleClones(GameObject prefab)
    {
        if (prefab == null) return;

        foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            if (go.name.Contains(prefab.name) && go.name.Contains("(Clone)"))
            {
                Destroy(go);
            }
        }
    }

    private List<int> GenerateTowerHealthValues(int maxTowers)
    {
        List<int> values = new List<int>();
        int towerCount = Random.Range(2, maxTowers + 1);
        for (int i = 0; i < towerCount; i++)
        {
            values.Add(Random.Range(5, maxInitialHealth + 1));
        }
        return values;
    }

    private bool IsPositionBlocked(Vector3 pos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, spawnClearRadius, towerLayerMask);
        return hits != null && hits.Length > 0;
    }

    private float CalculateSpawnClearance()
    {
        float maxRadius = Mathf.Max(
            GetPrefabApproxRadius(redTowerPrefab),
            GetPrefabApproxRadius(blueTowerPrefab),
            GetPrefabApproxRadius(obstaclePrefab1),
            GetPrefabApproxRadius(obstaclePrefab2)
        );

        return Mathf.Max(maxRadius + 0.05f, spawnClearRadiusFallback);
    }

    private float GetPrefabApproxRadius(GameObject prefab)
    {
        if (prefab == null) return 0f;

        CircleCollider2D cc = prefab.GetComponent<CircleCollider2D>();
        if (cc != null) return Mathf.Max(cc.radius * Mathf.Max(prefab.transform.localScale.x, prefab.transform.localScale.y), 0.02f);

        BoxCollider2D bc = prefab.GetComponent<BoxCollider2D>();
        if (bc != null)
        {
            Vector2 size = bc.size;
            float approx = Mathf.Max(size.x * prefab.transform.localScale.x, size.y * prefab.transform.localScale.y) * 0.5f;
            return Mathf.Max(approx, 0.02f);
        }

        return 0.0f;
    }
}