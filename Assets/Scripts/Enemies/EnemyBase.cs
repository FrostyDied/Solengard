using System.Collections;
using UnityEngine;

// Classe base para todos os inimigos do Solengard.
// Inimigos específicos devem herdar desta classe e sobrescrever OnDie().
public class EnemyBase : MonoBehaviour
{
    [Header("Atributos")]
    public float maxHealth = 30f;
    public float moveSpeed = 1.2f;
    public float damage = 10f;

    [Header("Movimento")]
    public float stoppingDistance   = 0.8f;
    public float separationRadius   = 2.0f;
    public float separationStrength = 1.5f;

    [Header("Dano de Contato")]
    [SerializeField] float contactDamageInterval = 0.5f;

    [Header("Referências")]
    // Deixe vazio para usar busca automática em Awake
    public Transform playerTransform;

    // Atribuído pelo WaveManager para ser notificado quando este inimigo morrer
    public System.Action OnDeathCallback;

    public static event System.Action OnEnemyDied;
    [HideInInspector] public string poolTag;

    protected float currentHealth;
    protected Rigidbody2D rb;

    bool  isDead;
    float _contactTimer;

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
        currentHealth  = maxHealth;
        _contactTimer  = contactDamageInterval; // ready to deal damage immediately on first contact
        isDead         = false;

        // Reset animator and sprite color so pool-reused enemies start clean
        var anim = GetComponent<CharacterAnimator>();
        if (anim != null) anim.ForceState(CharacterAnimator.State.Idle);
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

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
        float   speed      = dist < stoppingDistance * 3f
                             ? moveSpeed * (dist / (stoppingDistance * 3f))
                             : moveSpeed;
        rb.linearVelocity  = (toPlayer + separation * separationStrength).normalized * speed;

        var anim = GetComponent<CharacterAnimator>();
        if (anim != null) anim.SetState(CharacterAnimator.State.Walk);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && playerTransform != null)
        {
            float dir = playerTransform.position.x - transform.position.x;
            const float FLIP_DEADZONE = 0.25f;
            if (Mathf.Abs(dir) > FLIP_DEADZONE) sr.flipX = dir < 0f;
        }
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

    void OnCollisionStay2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        _contactTimer += Time.fixedDeltaTime;
        if (_contactTimer < contactDamageInterval) return;
        _contactTimer = 0f;
        NotifyDeathCause();
        var ph = col.gameObject.GetComponent<PlayerHealth>();
        ph?.TakeDamage(damage);
    }

    protected virtual void NotifyDeathCause() { }

    // Recebe dano e dispara Die() quando a vida chega a zero
    public void TakeDamage(float amount)
    {
        Debug.Log($"[Enemy] TakeDamage chamado em {gameObject.name} amount={amount} hp={currentHealth:F1}\n{System.Environment.StackTrace}");
        if (isDead || !CanTakeDamage()) return;
        currentHealth -= amount;
        StartCoroutine(FlashRed());
        if (currentHealth <= 0.01f) Die();
    }

    IEnumerator FlashRed()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    protected virtual bool CanTakeDamage() => true;

    void Die()
    {
        Debug.Log($"[Enemy] Die() chamado em {gameObject.name}\n{System.Environment.StackTrace}");
        if (isDead) return;
        isDead = true;

        var anim = GetComponent<CharacterAnimator>();
        if (anim != null) anim.SetState(CharacterAnimator.State.Death);

        GameManager.Instance?.IncrementKill();
        OnDie();
        OnDeathCallback?.Invoke();
        OnEnemyDied?.Invoke();

        StartCoroutine(DieAfterAnimation());
    }

    IEnumerator DieAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f);
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
