using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Soldier : MonoBehaviour
{
    public int teamID; // 0 for Blue, 1 for Red
    public float speed = 20f;
    public int damage = 1;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    private Transform targetTower;
    private TowerHealth towerHealth;
    private Rigidbody2D rb;
    private float lastAttackTime = -999f;
    private bool isFightingSoldier = false;
    private bool isDead = false; // ✅ yeni eklendi
    public float deathForce = 4f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

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
            towerHealth = target.GetComponent<TowerHealth>();
    }

    void Update()
    {
        if (isDead) return; // ✅ ölü asker artık işlem yapmasın

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
                        spriteRenderer.flipX = directionX < 0;
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
                        Debug.Log($"{gameObject.name} kuleye vurdu! Hasar: {damage}");
                    }

                    Die(); // ✅ direkt yok etmek yerine animasyonlu ölüm
                }
            }

            if (animator != null)
                animator.SetBool("IsMoving", currentlyMoving);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return; // ✅ ölü asker artık çarpışmaz

        Soldier otherSoldier = other.GetComponent<Soldier>();
        if (otherSoldier != null && otherSoldier.teamID != teamID)
        {
            isFightingSoldier = true;
            otherSoldier.Die();
            Die();
        }
    }

    // ✅ ÖLÜM METODU
    // 🚨 NOT: Bu kodun çalışması için, sınıfınızda bu değişkenlerin tanımlı olduğunu varsayıyorum:
    // public float deathForce = 400f; // Ne kadar güçlü fırlatılacağını ayarlar
    // private Rigidbody2D rb;
    // private SpriteRenderer spriteRenderer;

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
            // 1. Fırlatma için Rigidbody'yi Dinamik yap ve yer çekimini aç
            // Eski: rb.simulated = false; yerine daha modern ve fırlatma için uygun olan:
            rb.bodyType = RigidbodyType2D.Dynamic; // Artık fizik kuvvetlerinden etkilensin
            rb.gravityScale = 1; // Yer çekimi uygulansın

            // 2. Fırlatma Yönünü Belirle
            // Askerin baktığı yönün tersine (geri) ve hafifçe yukarı doğru kuvvet uygulayacağız.
            // Asker sağa bakıyorsa sola fırlamalı, sola bakıyorsa sağa fırlamalı.
            float horizontalDirection = spriteRenderer.flipX ? 1f : -1f;
            Vector2 kickDirection = new Vector2(horizontalDirection, 1f).normalized; // Yön vektörü (yatay + biraz yukarı)

            // 3. Fırlatma Kuvvetini Uygula
            rb.AddForce(kickDirection * deathForce, ForceMode2D.Impulse);
        }

        // Çarpışmayı kapat
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Fırlatma ve animasyon süresi için yeterli bekleme süresi
        // Bu süreyi, fırlatmanın havada kalma ve animasyon süresine göre ayarlayın.
        Destroy(gameObject, 0.3f); // 0.5 saniye yerine 1.0 saniye yaptık ki fırlatma görülsün
    }

}
