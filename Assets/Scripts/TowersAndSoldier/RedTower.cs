using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // ✅ Yeni Input System

public class RedTower : TowerHealth
{
    private class AttackLine
    {
        public LineRenderer lineRenderer;
        public Coroutine spawnCoroutine;
        public Transform currentTargetTower;
        public List<GameObject> spawnedSoldiers = new List<GameObject>();
        public GameObject lineObject;
        public bool isDrawing = false;
        public bool canDrawNewLine = true;
        public int lineID;
        public AttackLine(int id) { lineID = id; }
        public bool IsActive => spawnCoroutine != null;
    }

    private List<AttackLine> attackLines = new List<AttackLine>();
    private const int MAX_LINES = 3;
    private int currentDrawingLineID = -1;
    private int allowedLines = 1;

    [Header("Line Visuals")]
    public float lineYOffset = -2f;
    public float lineWidth = 0.3f;
    [ColorUsage(true, true)]
    public Color lineColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("3 Line Support")]
    public GameObject linePrefab;

    [Header("Tower Settings")]
    public float attackSpeedMultiplier = 10f;
    public float baseSpawnInterval = 10f;
    public GameObject redPrefab;
    public LayerMask towerLayerMask;
    public float raycastOffset = 3f;

    [Header("Line Cut Interaction")]
    public float maxCutTime = 0.5f;
    public float cutProximityTolerance = 0.8f;
    public float minCutDistance = 0.5f;

    private Vector2 initialMousePos;
    private float cutStartTime;
    private bool isListeningForCut = false;

    protected override void Awake()
    {
        base.Awake();
        InitializeAttackLines();
    }

    private void InitializeAttackLines()
    {
        if (linePrefab == null)
        {
            Debug.LogError("HATA: LineRenderer prefabı atanmadı!");
            return;
        }

        for (int i = 0; i < MAX_LINES; i++)
        {
            AttackLine line = new AttackLine(i);
            line.lineObject = Instantiate(linePrefab, transform.position, Quaternion.identity, transform);
            line.lineObject.name = $"RedTower_Line_{i}";
            line.lineRenderer = line.lineObject.GetComponent<LineRenderer>();

            if (line.lineRenderer == null)
            {
                Debug.LogError($"HATA: '{linePrefab.name}' prefabında LineRenderer bulunamadı!");
                Destroy(line.lineObject);
                continue;
            }

            InitializeLineDrawer(line.lineRenderer, lineColor, lineColor);
            attackLines.Add(line);
        }
    }

    protected override void ConvertTower(string attackerTeamTag)
    {
        CancelAllAttacks();
        base.ConvertTower(attackerTeamTag);
    }

    void Update()
    {
        HandlePlayerInput();
        UpdateLineLimitBasedOnHealth();
    }

    private AttackLine GetAvailableLine()
    {
        int activeCount = attackLines.FindAll(l => l.IsActive || l.isDrawing).Count;
        if (activeCount >= allowedLines) return null;
        return attackLines.Find(l => !l.IsActive && !l.isDrawing);
    }

    private AttackLine GetDrawingLine() => attackLines.Find(l => l.isDrawing);
    private bool IsAnyLineActive() => attackLines.Exists(l => l.IsActive);

    // --- INPUT HELPERS (Mouse + Touch) ---
    private bool PointerWasPressedThisFrame()
    {
        var mouse = Mouse.current;
        var touch = Touchscreen.current;
        bool mp = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool tp = touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        return mp || tp;
    }

    private bool PointerWasReleasedThisFrame()
    {
        var mouse = Mouse.current;
        var touch = Touchscreen.current;
        bool mr = mouse != null && mouse.leftButton.wasReleasedThisFrame;
        bool tr = touch != null && touch.primaryTouch.press.wasReleasedThisFrame;
        return mr || tr;
    }

    private bool PointerIsPressed()
    {
        var mouse = Mouse.current;
        var touch = Touchscreen.current;
        bool mp = mouse != null && mouse.leftButton.isPressed;
        bool tp = touch != null && touch.primaryTouch.press.isPressed;
        return mp || tp;
    }

    // Returns screen position; if no input returns Vector2.zero
    private Vector2 GetPointerScreenPosition()
    {
        var mouse = Mouse.current;
        var touch = Touchscreen.current;
        if (mouse != null)
            return mouse.position.ReadValue();
        if (touch != null)
            return touch.primaryTouch.position.ReadValue();
        return Vector2.zero;
    }

    // Returns world position or Vector2.zero if camera missing
    private Vector2 GetPointerWorldPosition()
    {
        if (Camera.main == null) return Vector2.zero;
        Vector2 screenPos = GetPointerScreenPosition();
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    // ✅ Yeni Input System ile hem mouse hem dokunmatik için
    void HandlePlayerInput()
    {
        if (Camera.main == null) return;

        bool pressed = PointerWasPressedThisFrame();
        bool released = PointerWasReleasedThisFrame();
        Vector2 worldPos = GetPointerWorldPosition();

        // Basıldıysa
        if (pressed)
        {
            Collider2D hitCollider = Physics2D.OverlapPoint(worldPos);

            if (hitCollider != null && hitCollider.transform == transform)
            {
                AttackLine availableLine = GetAvailableLine();
                if (availableLine == null)
                {
                    Debug.Log($"Maksimum {allowedLines} saldırı hattına ulaşıldı.");
                    return;
                }

                if (availableLine.canDrawNewLine)
                {
                    StartLine(availableLine);
                    availableLine.canDrawNewLine = false;
                }
                else
                {
                    Debug.Log($"Yeni çizgi oluşturmak için önce mevcut hattı kesmelisin! (Line ID: {availableLine.lineID})");
                }
            }
            else if (hitCollider == null || hitCollider.GetComponent<TowerHealth>() == null)
            {
                if (IsAnyLineActive() && GetDrawingLine() == null)
                {
                    initialMousePos = worldPos;
                    cutStartTime = Time.time;
                    isListeningForCut = true;
                }
            }
        }

        AttackLine drawingLine = GetDrawingLine();
        if (drawingLine != null)
        {
            UpdateLine(drawingLine, worldPos);
            if (released) EndLine(drawingLine, worldPos);
        }

        if (released && isListeningForCut && drawingLine == null)
            CheckForLineCut(worldPos);

        if (released)
            isListeningForCut = false;
    }

    void StartLine(AttackLine line)
    {
        if (line.lineRenderer == null) return;

        line.isDrawing = true;
        currentDrawingLineID = line.lineID;
        LineRenderer lr = line.lineRenderer;
        lr.positionCount = 2;

        Vector3 startPos = transform.position;
        startPos.y += lineYOffset;
        lr.SetPosition(0, startPos);
        lr.startColor = lineColor;
        lr.endColor = lineColor;
    }

    // Updated to accept pointer world position
    void UpdateLine(AttackLine line, Vector2 pointerWorldPos)
    {
        LineRenderer lr = line.lineRenderer;
        if (lr == null || lr.positionCount < 2) return;

        lr.SetPosition(1, pointerWorldPos);
    }

    // Updated to accept pointer world position
    void EndLine(AttackLine line, Vector2 pointerWorldPos)
    {
        line.isDrawing = false;
        currentDrawingLineID = -1;
        if (line.lineRenderer == null) { CancelAttack(line); return; }

        Vector2 mousePos = pointerWorldPos;
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePos);

        if (hitCollider != null && (hitCollider.CompareTag("BlueTower") || hitCollider.CompareTag("RedTower")))
        {
            Transform targetTower = hitCollider.transform;
            if (targetTower == transform) { CancelAttack(line); return; }

            if (IsPathBlocked(targetTower))
            {
                StartCoroutine(ShowInvalidPathFeedback(line));
            }
            else
            {
                Vector3 targetPos = targetTower.position;
                targetPos.y += lineYOffset;
                line.lineRenderer.SetPosition(1, targetPos);
                line.currentTargetTower = targetTower;

                if (line.spawnCoroutine == null)
                    line.spawnCoroutine = StartCoroutine(SpawnSoldiersLoop(line));
            }
        }
        else CancelAttack(line);
    }

    // Updated to accept final pointer world pos for cut computation
    void CheckForLineCut(Vector2 finalPointerWorldPos)
    {
        float timeElapsed = Time.time - cutStartTime;
        float distanceTravelled = Vector2.Distance(initialMousePos, finalPointerWorldPos);

        Debug.DrawLine(initialMousePos, finalPointerWorldPos, Color.yellow, 1f);
        if (timeElapsed > maxCutTime || distanceTravelled < minCutDistance) return;

        bool lineWasCut = false;

        foreach (var line in attackLines.FindAll(l => l.IsActive))
        {
            if (line.lineRenderer == null || line.lineRenderer.positionCount < 2) continue;

            if (IsCloseToLineSegment(line.lineRenderer.GetPosition(0), line.lineRenderer.GetPosition(1),
                initialMousePos, finalPointerWorldPos, cutProximityTolerance))
            {
                Debug.Log($"!!! KESME BAŞARILI !!! Saldırı hattı iptal edildi. (Line ID: {line.lineID})");
                CancelAttack(line);
                lineWasCut = true;
            }
        }

        if (lineWasCut)
            isListeningForCut = false;
    }

    private bool IsCloseToLineSegment(Vector2 lineA, Vector2 lineB, Vector2 cutA, Vector2 cutB, float tolerance)
    {
        Vector2 cutMid = (cutA + cutB) / 2f;
        if (DistanceToLineSegment(lineA, lineB, cutMid) <= tolerance) return true;
        if (DistanceToLineSegment(lineA, lineB, cutA) <= tolerance) return true;
        if (DistanceToLineSegment(lineA, lineB, cutB) <= tolerance) return true;
        return false;
    }

    private float DistanceToLineSegment(Vector2 lineA, Vector2 lineB, Vector2 point)
    {
        Vector2 AP = point - lineA;
        Vector2 AB = lineB - lineA;
        float t = Vector2.Dot(AP, AB) / AB.sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector2 closestPoint = lineA + t * AB;
        Debug.DrawLine(point, closestPoint, Color.green, 0.1f);
        return Vector2.Distance(point, closestPoint);
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
        Debug.DrawRay(rayStart, direction * rayLength, Color.magenta, 2f);
        return hit.collider != null && hit.collider.transform != transform && hit.collider.transform != target;
    }

    private IEnumerator ShowInvalidPathFeedback(AttackLine line)
    {
        if (line.lineRenderer != null)
        {
            line.lineRenderer.startColor = Color.black;
            line.lineRenderer.endColor = Color.black;
        }
        yield return new WaitForSeconds(0.3f);
        CancelAttack(line);
    }

    void CancelAttack(AttackLine line)
    {
        if (line == null) return;
        if (line.spawnCoroutine != null) { StopCoroutine(line.spawnCoroutine); line.spawnCoroutine = null; }
        foreach (GameObject soldier in line.spawnedSoldiers) if (soldier != null) Destroy(soldier);
        line.spawnedSoldiers.Clear();
        if (line.lineRenderer != null) line.lineRenderer.positionCount = 0;
        line.currentTargetTower = null;
        line.isDrawing = false;
        line.canDrawNewLine = true;
        Debug.Log($"ATTACK İPTAL EDİLDİ: Çizgi temizlendi. (Line ID: {line.lineID})");
    }

    void CancelAllAttacks()
    {
        foreach (var line in attackLines) CancelAttack(line);
    }

    IEnumerator SpawnSoldiersLoop(AttackLine line)
    {
        line.spawnedSoldiers.Clear();
        Vector3 lockedTargetPosition = line.currentTargetTower.position;

        while (true)
        {
            if (line.currentTargetTower == null || line.currentTargetTower.GetComponent<TowerHealth>()?.IsDead() == true)
            {
                line.currentTargetTower = FindTowerNearPosition(lockedTargetPosition, 1.5f);
                if (line.currentTargetTower == null) { yield return new WaitForSeconds(0.5f); continue; }

                if (line.lineRenderer != null)
                {
                    Vector3 targetPosWithOffset = line.currentTargetTower.position;
                    targetPosWithOffset.y += lineYOffset;
                    line.lineRenderer.SetPosition(1, targetPosWithOffset);
                }
            }

            if (line.currentTargetTower.CompareTag("RedTower"))
                yield return StartCoroutine(SlowHeal(line.currentTargetTower, 1, 0.2f));
            else if (line.currentTargetTower.CompareTag("BlueTower"))
            {
                GameObject soldier = Instantiate(redPrefab, transform.position, Quaternion.identity);
                Soldier soldierScript = soldier.GetComponent<Soldier>();
                if (soldierScript != null)
                {
                    soldierScript.teamID = 1;
                    soldierScript.SetTarget(line.currentTargetTower);
                }
                line.spawnedSoldiers.Add(soldier);
            }

            float attackSpeed = GetAttackSpeedBasedOnHealth();
            yield return new WaitForSeconds(baseSpawnInterval / attackSpeed);
        }
    }

    private IEnumerator SlowHeal(Transform tower, int totalHeal, float interval)
    {
        int healed = 0;
        TowerHealth targetHealth = tower.GetComponent<TowerHealth>();
        while (healed < totalHeal && targetHealth != null && !targetHealth.IsDead())
        {
            targetHealth.AddSoldiers(1);
            healed++;
            yield return new WaitForSeconds(interval);
        }
    }

    private Transform FindTowerNearPosition(Vector3 position, float tolerance = 1.5f)
    {
        TowerHealth[] allTowers = GameObject.FindObjectsByType<TowerHealth>(FindObjectsSortMode.None);
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (TowerHealth t in allTowers)
        {
            if (!t.IsDead())
            {
                float dist = Vector3.Distance(t.transform.position, position);
                if (dist < tolerance && dist < minDistance)
                {
                    minDistance = dist;
                    closest = t.transform;
                }
            }
        }
        return closest;
    }

    private void InitializeLineDrawer(LineRenderer lr, Color startColor, Color color)
    {
        if (lr == null) return;
        lr.positionCount = 0;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = startColor;
        lr.endColor = color;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
    }

    private float GetAttackSpeedBasedOnHealth()
    {
        float healthPercent = (float)currentHealth / maxHealth * 100f;
        if (healthPercent < 10f) return 7f;
        else if (healthPercent < 20f) return 7.5f;
        else if (healthPercent < 25f) return 9f;
        else if (healthPercent < 100f) return 10f;
        else return 5f;
    }

    private IEnumerator CancelAttackNextFrame(AttackLine line)
    {
        yield return null;
        if (line != null && line.IsActive)
            CancelAttack(line);
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

        for (int i = allowedLines; i < attackLines.Count; i++)
        {
            AttackLine line = attackLines[i];
            if (line == null) continue;
            if (line.isDrawing) continue;
            if (line.IsActive)
                StartCoroutine(CancelAttackNextFrame(line));
        }
    }
}
