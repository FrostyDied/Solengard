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
    // Alias para subclasses que preferem o nome específico de contato
    protected float contactDamage { get => damage; set => damage = value; }

    [Header("Movimento")]
    public float stoppingDistance   = 0.3f;
    public float separationRadius   = 0.5f;
    public float separationStrength = 0.6f;

    [Header("Tipo")]
    [SerializeField] protected bool isHeavy = false;
    public bool isBoss = false;

    [Header("Dano de Contato")]
    [SerializeField] float contactDamageInterval = 0.5f;

    [Header("XP Drop")]
    [SerializeField] int xpValue = 3;

    [Header("Referências")]
    // Deixe vazio para usar busca automática em Awake
    public Transform playerTransform;

    // Atribuído pelo ZoneManager para ser notificado quando este inimigo morrer
    public System.Action OnDeathCallback;

    public static event System.Action OnEnemyDied;
    [HideInInspector] public string poolTag;

    public static float GlobalHPMult     = 1f;
    public static float GlobalSpeedMult  = 1f;
    public static float GlobalDamageMult = 1f;

    public const float CHARACTER_WORLD_SCALE = 1.0f;

    protected float currentHealth;
    public    bool  IsDead => isDead;
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
        rb.bodyType               = RigidbodyType2D.Dynamic;
        rb.gravityScale           = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        currentHealth             = maxHealth;

        _sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (_sr == null) Debug.LogError($"[EnemyBase] SpriteRenderer não encontrado em '{gameObject.name}' nem em filhos.");

        var col = GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();
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
        _contactTimer  = 0f; // começa em zero → primeiro dano é imediato ao entrar no range
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
        moveSpeed    *= 1.2f;
        maxHealth    *= GlobalHPMult;
        currentHealth = maxHealth;
        moveSpeed    *= GlobalSpeedMult;
        damage       *= GlobalDamageMult;
    }

    protected virtual void Update()
    {
        if (playerTransform == null)
        {
            _findPlayerTimer -= Time.deltaTime;
            if (_findPlayerTimer <= 0f) { FindPlayer(); _findPlayerTimer = 0.5f; }
        }
        CheckContactDamage();
    }

    protected virtual void FixedUpdate()
    {
        MoveTowardsPlayer();
    }

    protected void FindPlayer()
    {
        if (PlayerController.Instance != null)
            playerTransform = PlayerController.Instance.transform;
        else
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (playerTransform == null) { FindPlayer(); return; }

        float dist = Vector2.Distance(rb.position, (Vector2)playerTransform.position);
        if (dist <= stoppingDistance) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 toPlayer   = ((Vector2)playerTransform.position - rb.position).normalized;
        Vector2 separation = ComputeSeparation();
        Vector2 desired = toPlayer + separation * separationStrength;
        // Garante que separação nunca inverta a direção de aproximação
        if (Vector2.Dot(desired.normalized, toPlayer) < 0.1f)
            desired = toPlayer;
        rb.linearVelocity = desired.normalized * moveSpeed;

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

    protected Vector2 ComputeSeparation()
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

    void CheckContactDamage()
    {
        if (playerTransform == null) return;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist >= stoppingDistance) return;
        _contactTimer -= Time.deltaTime;
        if (_contactTimer > 0f) return;
        _contactTimer = contactDamageInterval;
        NotifyDeathCause();
        Debug.Log($"[Enemy] {name} causou {damage} dano por distância");
        playerTransform.GetComponent<PlayerHealth>()?.TakeDamage(damage);
    }

    protected virtual void NotifyDeathCause() { }

    // Recebe dano e dispara Die() quando a vida chega a zero
    public void TakeDamage(float amount)
    {
        if (isDead || !CanTakeDamage()) return;
        currentHealth -= amount;
        if (isBoss)
            Debug.Log($"[Boss] {name} recebeu {amount:F0} dano. HP: {currentHealth:F0}/{maxHealth:F0}");

        var hitType = isBoss  ? VFXManager.EnemyType.Boss  :
                      isHeavy ? VFXManager.EnemyType.Heavy : VFXManager.EnemyType.Normal;

        var anim = GetComponent<CharacterAnimator>();
        if (anim != null) anim.SetState(CharacterAnimator.State.Hurt);

        if (currentHealth <= 0.01f)
        {
            Die(); // Die() spawna SpawnDeath — sem SpawnHit duplicado
        }
        else
        {
            VFXManager.Instance?.SpawnHit(transform.position, hitType);
            StartCoroutine(FlashRed());
            if (anim != null) StartCoroutine(ResetAnimAfterHurt(anim));
        }
    }

    IEnumerator ResetAnimAfterHurt(CharacterAnimator anim)
    {
        yield return new WaitForSeconds(0.3f);
        if (!isDead && anim != null) anim.SetState(CharacterAnimator.State.Walk);
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
        Debug.Log($"[Die] {name} spawning SpawnDeath");
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
