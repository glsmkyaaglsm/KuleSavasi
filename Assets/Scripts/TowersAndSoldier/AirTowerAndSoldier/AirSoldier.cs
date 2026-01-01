using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class AirSoldier : MonoBehaviour
{
    public int teamID; // 0 for Blue, 1 for Red
    public float speed = 20f;
    public int damage = 1;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    private Transform targetTower;
    // 🔑 DÜZELTİLDİ: TowerHealth yerine AirTowerHealth kullan
    private AirTowerHealth towerHealth;
    private Rigidbody2D rb;
    private float lastAttackTime = -999f;
    private bool isFightingSoldier = false;
    private bool isDead = false;
    public float deathForce = 4f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // 🔑 Yeni: AirBlueTower/AirRedTower'dan çağrılır
    public void Initialize(int team, AirTowerHealth target)
    {
        teamID = team;
        SetTarget(target.transform);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetTarget(Transform target)
    {
        targetTower = target;
        if (target != null)
            // 🔑 DÜZELTİLDİ: AirTowerHealth ara
            towerHealth = target.GetComponent<AirTowerHealth>();
    }

    void Update()
    {
        if (isDead) return;

        // 🔑 Oyun durumunu kontrol et
        if (AirGameManager.Instance != null && AirGameManager.currentState != AirGameManager.GameState.Playing) return;

        if (targetTower == null)
        {
            Die();
            return;
        }

        if (!isFightingSoldier)
        {
            float distance = Vector2.Distance(rb.position, targetTower.position);
            bool currentlyMoving = false;

            if (distance > attackRange)
            {
                Vector2 currentPosition = rb.position;
                Vector2 targetPosition = Vector2.MoveTowards(currentPosition, targetTower.position, speed * Time.deltaTime);

                if (targetPosition.x != currentPosition.x)
                {
                    float directionX = targetPosition.x - currentPosition.x;
                    if (spriteRenderer != null)
                        //spriteRenderer.flipX = directionX < 0;
                    currentlyMoving = true;
                }
                else if (targetPosition.y != currentPosition.y)
                {
                    currentlyMoving = true;
                }

                rb.MovePosition(targetPosition);
            }
            else
            {
                currentlyMoving = false;
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    lastAttackTime = Time.time;

                    if (towerHealth != null)
                    {
                        string attackerTag = (teamID == 1) ? "RedTower" : "BlueTower";
                        towerHealth.TakeDamage(damage, attackerTag);
                    }

                    Die();
                }
            }

            if (animator != null)
                animator.SetBool("IsMoving", currentlyMoving);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        // 🔑 DÜZELTİLDİ: AirSoldier ile çarpışma kontrolü
        AirSoldier otherSoldier = other.GetComponent<AirSoldier>();
        if (otherSoldier != null && otherSoldier.teamID != teamID)
        {
            isFightingSoldier = true;
            otherSoldier.Die();
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        VibrationManager.Vibrate(10);

        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsDead", true);
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1.5f; // Biraz daha hızlı düşmesi için artırılabilir

            // GÜNCELLENEN KISIM: 
            // Uçağın baktığı yönün tersine ve biraz yukarı fırlat (Geriye doğru savrulma efekti)
            Vector2 backwardDirection = -transform.right;
            Vector2 kickDirection = (backwardDirection + Vector2.up).normalized;
            rb.linearVelocity = Vector2.zero; // velocity yerine linearVelocity yazıyoruz
            rb.AddForce(kickDirection * deathForce, ForceMode2D.Impulse);

            // Uçağın düşerken dönmesi için rastgele bir tork (dönüş) ekleyelim
            rb.AddTorque(Random.Range(-50f, 50f));
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 0.5f); // 0.3f bazen çok kısa kalabilir, 0.5f daha iyidir
    }
}