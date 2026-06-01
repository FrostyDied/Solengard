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

    public const float CHARACTER_WORLD_SCALE = 2f;

    protected float currentHealth;
    protected Rigidbody2D rb;
    protected SpriteRenderer _sr;

    bool  isDead;
    float _contactTimer;
    int   _facingSign    = 1;   // histerese de flip: 1=direita -1=esquerda
    float _lastFlipTime  = -99f;
    const float FLIP_COOLDOWN  = 0.5f;
    const float FLIP_THRESHOLD = 0.8f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation  = true;
        rb.interpolation   = RigidbodyInterpolation2D.Interpolate;
        currentHealth      = maxHealth;

        _sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (_sr == null) Debug.LogError($"[EnemyBase] SpriteRenderer não encontrado em '{gameObject.name}' nem em filhos.");

        // Trigger-only: enemies detect contact via OnTriggerStay2D but don't push the player physically
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (transform.localScale == Vector3.one)
            transform.localScale = new Vector3(CHARACTER_WORLD_SCALE, CHARACTER_WORLD_SCALE, 1f);

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
        if (_sr != null) _sr.color = Color.white;

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

        if (_sr != null)
        {
            float vx = rb.linearVelocity.x;
            if (Time.time - _lastFlipTime > FLIP_COOLDOWN)
            {
                if (vx > FLIP_THRESHOLD && _facingSign != 1)
                {
                    _facingSign   = 1;
                    _sr.flipX     = false;
                    _lastFlipTime = Time.time;
                }
                else if (vx < -FLIP_THRESHOLD && _facingSign != -1)
                {
                    _facingSign   = -1;
                    _sr.flipX     = true;
                    _lastFlipTime = Time.time;
                }
            }
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

    void OnTriggerStay2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;
        _contactTimer += Time.fixedDeltaTime;
        if (_contactTimer < contactDamageInterval) return;
        _contactTimer = 0f;
        NotifyDeathCause();
        col.GetComponent<PlayerHealth>()?.TakeDamage(damage);
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
        if (_sr == null) yield break;
        _sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        _sr.color = Color.white;
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
