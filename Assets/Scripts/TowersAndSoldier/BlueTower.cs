using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BlueTower : TowerHealth
{
    // --- İÇ SINIF: Her Bir Saldırı Hattını Temsil Eder ---
    private class AttackLine
    {
        public LineRenderer lineRenderer;
        public Coroutine spawnCoroutine;
        public TowerHealth currentTargetTower;
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

    // ⬅️ YENİ: Kule canına göre izin verilen aktif çizgi sayısı
    private int allowedLines = 1;

    // Sabitler
    private const int TARGET_HEALTH_FOR_HEAL = 10;
    private const float HEAL_INTERVAL = 3f;

    // YALNIZCA YENİ ELE GEÇİRİLEN KULEYİ TUTAR.
    private TowerHealth prioritizedHealTarget = null;


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
            Debug.LogError("HATA: BlueTower için LineRenderer içeren prefab atanmadı!");
            return;
        }

        for (int i = 0; i < MAX_LINES; i++)
        {
            AttackLine line = new AttackLine(i);
            line.lineObject = Instantiate(linePrefab, transform.position, Quaternion.identity, transform);
            line.lineObject.name = $"BlueTower_Line_{i}";
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

    // --- TEMEL SALDIRI/İYİLEŞTİRME DÖNGÜSÜ (Hedefleri Yönetir) ---
    private IEnumerator AutoAttackLoop()
    {
        while (this != null)
        {
            float attackSpeed = GetAttackSpeedBasedOnHealth();

            // ⬅️ YENİ: Can bazlı çizgi limitini güncelle
            UpdateLineLimitBasedOnHealth();

            List<TowerHealth> currentTargets = new List<TowerHealth>();

            // --- 1. İyileştirme Kilidi Kontrolü ---
            // Eğer öncelikli bir iyileştirme hedefi varsa, 10 cana ulaşana kadar SADECE onu hedefle.
            if (prioritizedHealTarget != null)
            {
                if (prioritizedHealTarget.currentHealth >= TARGET_HEALTH_FOR_HEAL || prioritizedHealTarget.IsDead() || prioritizedHealTarget.teamID != this.teamID)
                {
                    prioritizedHealTarget = null;
                }
                else
                {
                    // ⬅️ ÖNEMLİ KİLİT: Tüm izin verilen hatları iyileştirme hedefine yönlendir.
                    for (int i = 0; i < allowedLines; i++)
                    {
                        currentTargets.Add(prioritizedHealTarget);
                    }
                }
            }


            // --- 2. Hedef Arama (Eğer kilit yoksa) ---
            if (prioritizedHealTarget == null)
            {
                // Mevcut hedefleri listeye ekle (bu aşamada 0 olmalı)
                // İyileştirme hedefini bulmaya çalış.
                var newTargets = FindClearPathTargetTowers(allowedLines, currentTargets);
                currentTargets.AddRange(newTargets);
            }


            // --- HEDEFLERİ SALDIRI HATLARI İLE EŞLEŞTİR VE BAŞLAT/DURDUR ---
            for (int i = 0; i < MAX_LINES; i++)
            {
                AttackLine line = attackLines[i];

                if (i < allowedLines && i < currentTargets.Count) // ⬅️ allowedLines kontrolü eklendi
                {
                    TowerHealth target = currentTargets[i];

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
                    // Artık hedef yok veya çizgi sınırının dışındayız, hattı iptal et
                    if (line.IsActive)
                        CancelAttackLine(line);
                    else if (line.lineRenderer != null) // Sınıra takılan çizgileri temizle
                        line.lineRenderer.positionCount = 0;
                }
            }
            yield return new WaitForSeconds(attackInterval / attackSpeed);
        }
    }
    // ----------------------------------------------------------------------


    // --- Hata Düzeltmeli Asker Gönderme/İyileştirme Döngüsü ---
    private IEnumerator SpawnSoldiersOrHealLoop(AttackLine line)
    {
        float currentLineInterval = attackInterval;

        TowerHealth target = line.currentTargetTower;

        // --- İyileştirme Mantığı ---
        if (target == prioritizedHealTarget)
        {
            Debug.Log($"Blue Tower, ele geçirdiği {target.gameObject.name} kulesini iyileştiriyor...");

            while (target != null && !target.Equals(null) && !target.IsDead() && target.currentHealth < TARGET_HEALTH_FOR_HEAL && target.teamID == this.teamID)
            {
                target.AddSoldiers(1); // 1 can ekle
                yield return new WaitForSeconds(HEAL_INTERVAL);
            }

            // İyileştirme tamamlandıysa hedefi sıfırla
            if (target != null && target.teamID == this.teamID && target.currentHealth >= TARGET_HEALTH_FOR_HEAL)
            {
                if (prioritizedHealTarget == target) prioritizedHealTarget = null;
                Debug.Log($"İyileştirme Bitti: {target.gameObject.name} 10 cana ulaştı.");
            }
            else // Hedef öldü veya takım değiştirdi
            {
                if (prioritizedHealTarget == target) prioritizedHealTarget = null;
            }

            yield break; // İyileştirme bitti.
        }

        // --- Saldırı Mantığı ---
        while (line.currentTargetTower != null && line.currentTargetTower.teamID != this.teamID)
        {
            if (line.currentTargetTower.IsDead() || line.currentTargetTower.teamID == this.teamID)
                break;

            GameObject soldier = Instantiate(blueSoldierPrefab, transform.position, Quaternion.identity);
            Soldier soldierScript = soldier.GetComponent<Soldier>();

            if (soldierScript != null)
            {
                soldierScript.teamID = 0;
                soldierScript.speed = soldierSpeed;
                soldierScript.SetTarget(line.currentTargetTower.transform);
            }

            line.spawnedSoldiers.RemoveAll(item => item == null);
            line.spawnedSoldiers.Add(soldier);

            float speedMultiplier = GetAttackSpeedBasedOnHealth();
            yield return new WaitForSeconds(currentLineInterval / speedMultiplier);
        }

        CancelAttackLine(line);
    }
    // ----------------------------------------------------------------------


    private List<TowerHealth> FindClearPathTargetTowers(int targetsNeeded, List<TowerHealth> excludeList)
    {
        List<TowerHealth> targets = new List<TowerHealth>();

        // 1. ÖNCELİK: Düşük canlı, yeni ele geçirilmiş kuleleri iyileştirme. (Sadece prioritizedHealTarget null ise çalışır)
        if (prioritizedHealTarget == null && targetsNeeded > 0)
        {
            // Yeni yakalanan ve canı düşük olan kuleleri bul
            var newCaptures = FindObjectsByType<TowerHealth>(FindObjectsSortMode.None)
                .Where(t => t != this && t.teamID == this.teamID && t.currentHealth < 3 && !excludeList.Contains(t))
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position));

            foreach (var tower in newCaptures)
            {
                if (!IsPathBlocked(tower.transform))
                {
                    prioritizedHealTarget = tower;
                    Debug.Log($"Yeni ele geçirilen kule bulundu ve takviye için önceliklendirildi: {tower.gameObject.name}");
                    targets.Add(tower);
                    targetsNeeded--;
                    break;
                }
            }
        }

        // 2. ÖNCELİK: Düşman Kulelerine Saldırı
        if (targetsNeeded > 0)
        {
            var enemyTowers = FindObjectsByType<TowerHealth>(FindObjectsSortMode.None)
                .Where(t => t != this && t.teamID != this.teamID && !excludeList.Contains(t))
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position));

            foreach (var tower in enemyTowers)
            {
                if (targetsNeeded <= 0) break;
                if (!IsPathBlocked(tower.transform))
                {
                    targets.Add(tower);
                    targetsNeeded--;
                }
            }
        }

        return targets;
    }

    // ⬅️ YENİ METOT: Can yüzdesine göre izin verilen hat sayısını ayarlar
    private void UpdateLineLimitBasedOnHealth()
    {
        int ch = Mathf.RoundToInt(currentHealth);
        int newAllowedLines = 1;

        // 25 Can ve üstü -> 3 Hat
        if (ch >= 25) newAllowedLines = 3;
        // 10 Can ve üstü -> 2 Hat
        else if (ch >= 10) newAllowedLines = 2;
        // 10 Can altı -> 1 Hat
        else newAllowedLines = 1;

        if (newAllowedLines != allowedLines)
        {
            allowedLines = newAllowedLines;
            Debug.Log($"Kule Canı: {ch}. Yeni İzin Verilen Çizgi Sayısı: {allowedLines}");
        }
    }


    private bool IsPathBlocked(Transform target)
    {
        Vector2 startPos = transform.position;
        Vector2 endPos = target.position;
        Vector2 direction = (endPos - startPos).normalized;
        float distance = Vector2.Distance(startPos, endPos);
        Vector2 rayStart = startPos + direction * raycastOffset;
        float rayLength = distance - (2 * raycastOffset);

        if (rayLength <= 0) return false;

        RaycastHit2D hit = Physics2D.BoxCast(rayStart, new Vector2(0.2f, 0.2f), 0f, direction, rayLength, towerLayerMask);
        return hit.collider != null && hit.collider.transform != this.transform && hit.collider.transform != target;
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

        // Bu hattın hedefi iyileştirme hedefiyse, iyileştirme hedefini sıfırla
        if (prioritizedHealTarget == line.currentTargetTower)
        {
            // DİKKAT: Diğer hatlar da iyileştirme hedefindeyse, AutoAttackLoop onu tekrar seçecektir.
            // Sadece bu hattın hedefini sıfırlıyoruz, kilit AutoAttackLoop'ta kırılacak.
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