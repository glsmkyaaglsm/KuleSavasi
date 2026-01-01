using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AirLevelConfig
{
    [Header("Kule Sayýlarý")]
    public int redCount;
    public int blueCount;

    [Header("Kule Saðlýklarý")]
    public List<int> redTowerHealths = new List<int>();
    public List<int> blueTowerHealths = new List<int>();

    [Header("Spawn & Engel Ayarlarý")]
    public List<Vector2> spawnPositions = new List<Vector2>();
    public bool useObstacle1;
    public bool useObstacle2;

    // Opsiyonel: Level hýzý veya bonuslarý ekleyebilirsin
    public float soldierSpeedBonus;
}
