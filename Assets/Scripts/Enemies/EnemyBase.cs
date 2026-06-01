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

    [Header("Tipo")]
    [SerializeField] protected bool isHeavy = false;
    [SerializeField] protected bool isBoss  = false;

    [Header("Dano de Contato")]
    [SerializeField] float contactDamageInterval = 0.5f;

    [Header("XP Drop")]
    [SerializeField] int xpValue = 3;

    [Header("Referências")]
    // Deixe vazio para usar busca automática em Awake
    public Transform playerTransform;

    // Atribuído pelo WaveManager para ser notificado quando este inimigo morrer
    public System.Action OnDeathCallback;

    public static event System.Action OnEnemyDied;
    [HideInInspector] public string poolTag;

    public const float CHARACTER_WORLD_SCALE = 1.0f;

    protected float currentHealth;
    protected Rigidbody2D rb;
    protected SpriteRenderer _sr;

    bool  isDead;
    float _contactTimer;
    int   _facingSign       = 1;
    float _lastFlipTime     = -99f;
    float _findPlayerTimer  = 0f;
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

    protected virtual void Start()
    {
        moveSpeed *= 1.2f; // horda 20% mais rápida globalmente
    }

    protected virtual void Update()
    {
        if (playerTransform == null)
        {
            _findPlayerTimer -= Time.deltaTime;
            if (_findPlayerTimer <= 0f) { FindPlayer(); _findPlayerTimer = 0.5f; }
        }
    }

    protected virtual void FixedUpdate()
    {
        MoveTowardsPlayer();
    }

    void FindPlayer()
    {
        if (PlayerController.Instance != null)
            playerTransform = PlayerController.Instance.transform;
        else
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void MoveTowardsPlayer()
    {
        if (playerTransform == null) { FindPlayer(); return; }

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
        if (isDead || !CanTakeDamage()) return;
        currentHealth -= amount;
        var hitType = isBoss  ? VFXManager.EnemyType.Boss  :
                      isHeavy ? VFXManager.EnemyType.Heavy : VFXManager.EnemyType.Normal;
        VFXManager.Instance?.SpawnHit(transform.position, hitType);
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

    protected virtual Color GetDeathColor()
    {
        string n = gameObject.name;
        if (n.Contains("Slime"))              return new Color(0.3f, 0.8f, 0.2f);
        if (n.Contains("Zumbi") || n.Contains("Zombie")) return new Color(0.5f, 0.1f, 0.5f);
        if (n.Contains("Orc")  || n.Contains("Gnoll"))   return new Color(0.7f, 0.1f, 0.1f);
        if (n.Contains("Golem"))              return new Color(0.5f, 0.6f, 0.7f);
        if (n.Contains("Boss") || n.Contains("Lich"))    return new Color(1f,   0.8f, 0.1f);
        return new Color(1f, 0.4f, 0.1f);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        var anim = GetComponent<CharacterAnimator>();
        if (anim != null) anim.SetState(CharacterAnimator.State.Death);

        var deathType = isBoss  ? VFXManager.EnemyType.Boss  :
                        isHeavy ? VFXManager.EnemyType.Heavy : VFXManager.EnemyType.Normal;
        VFXManager.Instance?.SpawnDeath(transform.position, deathType);
        GameManager.Instance?.IncrementKill();
        XPDrop.SpawnAt(transform.position, xpValue);
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
