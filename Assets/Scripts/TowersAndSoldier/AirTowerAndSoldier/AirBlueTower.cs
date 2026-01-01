using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AirBlueTower : AirTowerHealth
{
    // --- İÇ SINIF: Her Bir Saldırı Hattını Temsil Eder ---
    private class AttackLine
    {
        public LineRenderer lineRenderer;
        public Coroutine spawnCoroutine;
        // 🔑 DÜZELTİLDİ: TowerHealth yerine AirTowerHealth kullan
        public AirTowerHealth currentTargetTower;
        public GameObject lineObject;
        public List<GameObject> spawnedSoldiers = new List<GameObject>();
        public int lineID;

        public AttackLine(int id) { lineID = id; }
        public bool IsActive => spawnCoroutine != null;
    }
    // -----------------------------------------------------------

    public GameObject blueSoldierPrefab;
    public float attackInterval = 10f;
    public float initialAttackDelay = 3f;

    [Header("Line Visuals")]
    public float lineYOffset = -0.5f;
    public float lineWidth = 0.3f;
    [ColorUsage(true, true)]
    public Color lineColor = new Color(0f, 0f, 1f, 0.5f);
    public GameObject linePrefab;

    [Header("Yol Kontrolü")]
    public LayerMask towerLayerMask;
    public float raycastOffset = 0.5f;

    // --- DEĞİŞKENLER ---
    private const int MAX_LINES = 3;
    private List<AttackLine> attackLines = new List<AttackLine>();
    private Coroutine mainAttackLoopCoroutine;
    private float soldierSpeed = 15f;

    private int allowedLines = 1;

    // Sabitler
    private const int TARGET_HEALTH_FOR_HEAL = 10;
    private const float HEAL_INTERVAL = 3f;

    // 🔑 DÜZELTİLDİ: TowerHealth yerine AirTowerHealth kullan
    private AirTowerHealth prioritizedHealTarget = null;


    protected override void Awake()
    {
        base.Awake();
        var singleLineRenderer = GetComponent<LineRenderer>();
        if (singleLineRenderer != null) singleLineRenderer.enabled = false;

        InitializeAttackLines();
    }

    void Start()
    {
        mainAttackLoopCoroutine = StartCoroutine(StartAttackLoopWithDelay());
    }

    public void ApplyLevelConfig(float speedBonus)
    {
        soldierSpeed = speedBonus;
    }

    private void InitializeAttackLines()
    {
        if (linePrefab == null)
        {
            Debug.LogError("HATA: AirBlueTower için LineRenderer içeren prefab atanmadı!");
            return;
        }

        for (int i = 0; i < MAX_LINES; i++)
        {
            AttackLine line = new AttackLine(i);
            line.lineObject = Instantiate(linePrefab, transform.position, Quaternion.identity, transform);
            line.lineObject.name = $"AirBlueTower_Line_{i}";
            line.lineRenderer = line.lineObject.GetComponent<LineRenderer>();

            if (line.lineRenderer != null)
                InitializeLineDrawer(line.lineRenderer, lineColor, lineColor);
            else
                Debug.LogError($"HATA: '{linePrefab.name}' prefabında LineRenderer bulunamadı!");

            attackLines.Add(line);
        }
    }


    protected override void ConvertTower(string attackerTeamTag)
    {
        if (mainAttackLoopCoroutine != null) StopCoroutine(mainAttackLoopCoroutine);
        prioritizedHealTarget = null;

        foreach (var line in attackLines)
        {
            if (line.spawnCoroutine != null) StopCoroutine(line.spawnCoroutine);
            // 🔑 DÜZELTİLDİ: AirSoldier destroy edilmeli
            foreach (GameObject soldier in line.spawnedSoldiers)
                if (soldier != null) Destroy(soldier);

            line.spawnedSoldiers.Clear();
            line.currentTargetTower = null;
            if (line.lineRenderer != null) line.lineRenderer.positionCount = 0;
        }

        base.ConvertTower(attackerTeamTag);
    }

    private IEnumerator StartAttackLoopWithDelay()
    {
        yield return new WaitForSeconds(initialAttackDelay);
        mainAttackLoopCoroutine = StartCoroutine(AutoAttackLoop());
    }

    private IEnumerator AutoAttackLoop()
    {
        while (this != null)
        {
            // 🔑 OYUN DURUMU KONTROLÜ
            if (AirGameManager.Instance != null && AirGameManager.currentState != AirGameManager.GameState.Playing)
            {
                yield return null;
                continue;
            }

            float attackSpeed = GetAttackSpeedBasedOnHealth();
            UpdateLineLimitBasedOnHealth();

            // 🔑 DÜZELTİLDİ: AirTowerHealth listesi kullan
            List<AirTowerHealth> currentTargets = new List<AirTowerHealth>();

            // --- 1. İyileştirme Kilidi Kontrolü ---
            if (prioritizedHealTarget != null)
            {
                if (prioritizedHealTarget.currentHealth >= TARGET_HEALTH_FOR_HEAL || prioritizedHealTarget.IsDead() || prioritizedHealTarget.teamID != this.teamID)
                {
                    prioritizedHealTarget = null;
                }
                else
                {
                    for (int i = 0; i < allowedLines; i++)
                    {
                        currentTargets.Add(prioritizedHealTarget);
                    }
                }
            }


            // --- 2. Hedef Arama (Eğer kilit yoksa) ---
            if (prioritizedHealTarget == null)
            {
                var newTargets = FindClearPathTargetTowers(allowedLines, currentTargets);
                currentTargets.AddRange(newTargets);
            }


            // --- HEDEFLERİ SALDIRI HATLARI İLE EŞLEŞTİR VE BAŞLAT/DURDUR ---
            for (int i = 0; i < MAX_LINES; i++)
            {
                AttackLine line = attackLines[i];

                if (i < allowedLines && i < currentTargets.Count)
                {
                    AirTowerHealth target = currentTargets[i];

                    if (line.currentTargetTower != target)
                    {
                        if (line.spawnCoroutine != null) StopCoroutine(line.spawnCoroutine);
                        line.currentTargetTower = target;
                        line.spawnCoroutine = StartCoroutine(SpawnSoldiersOrHealLoop(line));
                    }

                    UpdateLineRenderer(line.lineRenderer, transform.position, target.transform.position, lineYOffset, lineColor);
                }
                else
                {
                    if (line.IsActive)
                        CancelAttackLine(line);
                    else if (line.lineRenderer != null)
                        line.lineRenderer.positionCount = 0;
                }
            }
            yield return new WaitForSeconds(attackInterval / attackSpeed);
        }
    }


    private IEnumerator SpawnSoldiersOrHealLoop(AttackLine line)
    {
        float currentLineInterval = attackInterval;
        AirTowerHealth target = line.currentTargetTower;

        // --- İyileştirme Mantığı ---
        if (target == prioritizedHealTarget)
        {
            // (İyileştirme kodun olduğu gibi kalabilir...)
            while (target != null && !target.Equals(null) && !target.IsDead() && target.currentHealth < TARGET_HEALTH_FOR_HEAL && target.teamID == this.teamID)
            {
                target.AddSoldiers(1);
                yield return new WaitForSeconds(HEAL_INTERVAL);
            }
            if (prioritizedHealTarget == target) prioritizedHealTarget = null;
            yield break;
        }

        // --- Saldırı Mantığı (DÜZELTİLEN KISIM BURASI) ---
        while (line.currentTargetTower != null && line.currentTargetTower.teamID != this.teamID)
        {
            if (line.currentTargetTower.IsDead() || line.currentTargetTower.teamID == this.teamID)
                break;

            // 🚀 1. Yön ve Rotasyon Hesaplama
            Vector3 direction = (line.currentTargetTower.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion spawnRotation = Quaternion.Euler(0, 0, angle);

            // 🚀 2. Uçağı Doğru Rotasyonla Oluştur
            GameObject soldier = Instantiate(blueSoldierPrefab, transform.position, spawnRotation);

            // 🚀 3. Görsel Düzeltme (Ters Dönmeyi Engelleme)
            // Eğer uçak sola gidiyorsa (x < 0), uçağı Y ekseninde aynalıyoruz
            if (direction.x < 0)
            {
                soldier.transform.localScale = new Vector3(1, -1, 1);
            }

            // 🚀 4. AirSoldier Başlatma
            AirSoldier airSoldierScript = soldier.GetComponent<AirSoldier>();
            if (airSoldierScript != null)
            {
                airSoldierScript.Initialize(0, line.currentTargetTower);
            }
            else
            {
                Debug.LogError("HATA: AirSoldier scripti bulunamadı!");
                Destroy(soldier);
                break;
            }

            line.spawnedSoldiers.RemoveAll(item => item == null);
            line.spawnedSoldiers.Add(soldier);

            float speedMultiplier = GetAttackSpeedBasedOnHealth();
            yield return new WaitForSeconds(currentLineInterval / speedMultiplier);
        }

        CancelAttackLine(line);
    }

    // 🔑 DÜZELTİLDİ: Sadece AirTowerHealth hedeflerini döndürür
    private List<AirTowerHealth> FindClearPathTargetTowers(int targetsNeeded, List<AirTowerHealth> excludeList)
    {
        List<AirTowerHealth> targets = new List<AirTowerHealth>();
        var allAirTowers = FindObjectsByType<AirTowerHealth>(FindObjectsSortMode.None);

        // 1. ÖNCELİK: Düşük canlı, yeni ele geçirilmiş kuleleri iyileştirme.
        if (prioritizedHealTarget == null && targetsNeeded > 0)
        {
            var newCaptures = allAirTowers
                .Where(t => t != this && t.teamID == this.teamID && t.currentHealth < 3 && !excludeList.Contains(t))
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position));

            foreach (var tower in newCaptures)
            {
                // 🔑 DÜZELTİLDİ: IsPathBlocked kontrolü Air modunda true döndürmemelidir.
                if (!IsPathBlocked(tower.transform))
                {
                    prioritizedHealTarget = tower;
                    targets.Add(tower);
                    targetsNeeded--;
                    break;
                }
            }
        }

        // 2. ÖNCELİK: Düşman Kulelerine Saldırı
        if (targetsNeeded > 0)
        {
            var enemyTowers = allAirTowers
                .Where(t => t != this && t.teamID != this.teamID && !excludeList.Contains(t))
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position));

            foreach (var tower in enemyTowers)
            {
                if (targetsNeeded <= 0) break;
                // 🔑 DÜZELTİLDİ: IsPathBlocked kontrolü Air modunda true döndürmemelidir.
                if (!IsPathBlocked(tower.transform))
                {
                    targets.Add(tower);
                    targetsNeeded--;
                }
            }
        }

        return targets;
    }

    private void UpdateLineLimitBasedOnHealth()
    {
        int ch = Mathf.RoundToInt(currentHealth);
        int newAllowedLines = 1;

        if (ch >= 25) newAllowedLines = 3;
        else if (ch >= 10) newAllowedLines = 2;
        else newAllowedLines = 1;

        if (newAllowedLines != allowedLines)
            allowedLines = newAllowedLines;
    }


    private bool IsPathBlocked(Transform target)
    {
        // 🔑 KRİTİK DÜZELTME: HAVA KULELERİ İÇİN ENGEL YOKTUR.
        return false;
    }


    private void InitializeLineDrawer(LineRenderer lr, Color startColor, Color endColor)
    {
        if (lr == null) return;
        lr.positionCount = 0;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = startColor;
        lr.endColor = endColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
    }

    private void UpdateLineRenderer(LineRenderer lr, Vector3 start, Vector3 end, float yOffset, Color color)
    {
        if (lr == null) return;

        Vector3 startPos = start;
        startPos.y += yOffset;
        Vector3 targetPos = end;
        targetPos.y += yOffset;

        lr.positionCount = 2;
        lr.SetPosition(0, startPos);
        lr.SetPosition(1, targetPos);
        lr.startColor = color;
        lr.endColor = color;
    }

    void CancelAttackLine(AttackLine line)
    {
        if (line == null) return;
        if (line.spawnCoroutine != null) { StopCoroutine(line.spawnCoroutine); line.spawnCoroutine = null; }
        foreach (GameObject soldier in line.spawnedSoldiers) if (soldier != null) Destroy(soldier);
        line.spawnedSoldiers.Clear();

        if (prioritizedHealTarget == line.currentTargetTower)
        {
        }

        line.currentTargetTower = null;
        if (line.lineRenderer != null) line.lineRenderer.positionCount = 0;
    }


    private float GetAttackSpeedBasedOnHealth()
    {
        float healthPercent = (float)currentHealth / maxHealth * 100f;

        if (healthPercent < 10f) return 1f;
        else if (healthPercent < 20f) return 2.5f;
        else if (healthPercent < 30f) return 2.75f;
        else if (healthPercent < 100f) return 3f;
        else return 5f;
    }

    private void OnDestroy()
    {
        if (mainAttackLoopCoroutine != null) StopCoroutine(mainAttackLoopCoroutine);

        foreach (var line in attackLines)
            if (line.spawnCoroutine != null) StopCoroutine(line.spawnCoroutine);
    }
}