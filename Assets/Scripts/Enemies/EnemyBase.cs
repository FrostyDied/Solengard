using UnityEngine;

// Classe base para todos os inimigos do Solengard.
// Inimigos específicos devem herdar desta classe e sobrescrever OnDie().
public class EnemyBase : MonoBehaviour
{
    [Header("Atributos")]
    public float maxHealth = 30f;
    public float moveSpeed = 2f;
    public float damage = 10f;

    [Header("Movimento")]
    public float stoppingDistance   = 0.3f;
    public float separationRadius   = 1.5f;
    public float separationStrength = 0.6f;

    [Header("Referências")]
    // Deixe vazio para usar busca automática em Awake
    public Transform playerTransform;

    // Atribuído pelo WaveManager para ser notificado quando este inimigo morrer
    public System.Action OnDeathCallback;

    public static event System.Action OnEnemyDied;
    [HideInInspector] public string poolTag;

    protected float currentHealth;
    protected Rigidbody2D rb;

    float contactDamageTimer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        currentHealth = maxHealth;

        if (transform.localScale == Vector3.one)
            transform.localScale = new Vector3(2f, 2f, 1f);

        // Busca o player automaticamente caso não tenha sido atribuído no Inspector
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    protected virtual void OnEnable()
    {
        currentHealth      = maxHealth;
        contactDamageTimer = 0f;
        // Re-find player in case the scene was reloaded and the reference became stale
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    protected virtual void OnDisable() { }

    protected virtual void FixedUpdate()
    {
        MoveTowardsPlayer();
    }

    void MoveTowardsPlayer()
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(rb.position, (Vector2)playerTransform.position);
        if (dist <= stoppingDistance) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 toPlayer   = ((Vector2)playerTransform.position - rb.position).normalized;
        Vector2 separation = ComputeSeparation();
        rb.linearVelocity  = (toPlayer + separation * separationStrength).normalized * moveSpeed;
    }

    Vector2 ComputeSeparation()
    {
        Vector2 sep    = Vector2.zero;
        var     nearby = Physics2D.OverlapCircleAll(rb.position, separationRadius);
        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            if (col.GetComponent<EnemyBase>() == null) continue;
            Vector2 away = rb.position - (Vector2)col.transform.position;
            float   d    = away.magnitude;
            if (d > 0.01f) sep += away / d;
        }
        return sep;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        contactDamageTimer -= Time.fixedDeltaTime;
        if (contactDamageTimer > 0f) return;

        PlayerHealth ph = collision.collider.GetComponent<PlayerHealth>();
        if (ph == null) return;

        NotifyDeathCause();
        ph.TakeDamage(damage);
        contactDamageTimer = 1f;
    }

    protected virtual void NotifyDeathCause() { }

    void OnCollisionExit2D(Collision2D collision)
    {
        contactDamageTimer = 0f;
    }

    // Recebe dano e dispara Die() quando a vida chega a zero
    public void TakeDamage(float amount)
    {
        if (!CanTakeDamage()) return;
        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
    }

    protected virtual bool CanTakeDamage() => true;

    void Die()
    {
        Debug.Log($"[EnemyBase] Die() — {gameObject.name}. GM={GameManager.Instance != null}");
        GameManager.Instance?.IncrementKill();
        OnDie();
        OnDeathCallback?.Invoke();
        OnEnemyDied?.Invoke();

        if (ObjectPoolManager.Instance != null && !string.IsNullOrEmpty(poolTag))
            ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
        else
            Destroy(gameObject);
    }

    // Sobrescreva em subclasses para efeitos de morte específicos (drops, animações, etc.)
    protected virtual void OnDie() { }

    // Sincroniza currentHealth com maxHealth; use após modificar maxHealth externamente
    public void InitializeHealth() => currentHealth = maxHealth;
}
