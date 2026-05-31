using UnityEngine;

// Gerencia o auto-ataque do player: a cada cooldown, detecta o inimigo mais
// próximo dentro do range e aplica dano diretamente via EnemyBase.TakeDamage.
// Attach este componente no mesmo GameObject que PlayerController.
public class PlayerAttack : MonoBehaviour
{
    [Header("Atributos de Ataque")]
    public float attackDamage   = 25f;
    public float attackRange    = 4.5f;
    public float attackCooldown = 0.5f;

    [Header("Detecção")]
    public LayerMask enemyLayerMask;

    [Header("Efeito Visual")]
    [SerializeField] GameObject attackEffectPrefab;

    PlayerWeapon weapon;
    float cooldownTimer;

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Awake()
    {
        weapon = GetComponent<PlayerWeapon>();
        SyncFromWeapon();

        // Fallbacks para quando o campo não foi configurado no Inspector
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Enemy");
        if (attackRange    <= 0f) attackRange   = 3f;

        Debug.Log($"[PlayerAttack] Awake — damage={attackDamage} range={attackRange} cooldown={attackCooldown} layerMask={enemyLayerMask.value}");
    }

    void OnEnable()  => PlayerWeapon.OnWeaponUpgraded += AoUpgradeArma;
    void OnDisable() => PlayerWeapon.OnWeaponUpgraded -= AoUpgradeArma;

    void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            TryAttack();
            cooldownTimer = attackCooldown;
        }
    }

    // ── Lógica de ataque ────────────────────────────────────────────────────────

    void TryAttack()
    {
        EnemyBase alvo = EncontrarInimigoMaisProximo();
        if (alvo == null) return;

        if (PlayerController.Instance != null)
            PlayerController.Instance.LastAttackTime = Time.time;

        // Flip sprite toward enemy; PlayerController respects LastAttackTime for 0.3s
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float dir = alvo.transform.position.x - transform.position.x;
            if (Mathf.Abs(dir) > 0.01f) sr.flipX = dir < 0f;
        }

        alvo.TakeDamage(attackDamage);

        if (attackEffectPrefab != null)
        {
            Vector2 attackDir = ((Vector2)(alvo.transform.position - transform.position)).normalized;
            var effectPos = transform.position + (Vector3)(attackDir * attackRange * 0.6f);
            var effect    = Instantiate(attackEffectPrefab, effectPos, Quaternion.identity);
            float angle   = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
            effect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            Destroy(effect, 0.2f);
        }
    }

    public void SyncFromWeapon()
    {
        if (weapon == null) return;
        attackDamage   = weapon.damage;
        attackRange    = weapon.attackRange;
        attackCooldown = 1f / Mathf.Max(weapon.attackSpeed, 0.01f);
    }

    void AoUpgradeArma(PlayerWeapon pw) => SyncFromWeapon();

    // Retorna o EnemyBase mais próximo no semicírculo frontal dentro do attackRange
    EnemyBase EncontrarInimigoMaisProximo()
    {
        Collider2D[] colisores = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);

        EnemyBase maisProximo = null;
        float menorDistancia  = float.MaxValue;

        foreach (Collider2D col in colisores)
        {
            EnemyBase inimigo = col.GetComponent<EnemyBase>();
            if (inimigo == null) continue;

            // Only hit enemies in the facing semicircle
            var toEnemy   = ((Vector2)(inimigo.transform.position - transform.position)).normalized;
            var facing    = PlayerController.Instance != null
                            ? PlayerController.Instance.FacingDirection : Vector2.right;
            if (Vector2.Dot(facing, toEnemy) < 0f) continue;

            float distancia = Vector2.Distance(transform.position, col.transform.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                maisProximo    = inimigo;
            }
        }

        return maisProximo;
    }

    // ── Gizmos ──────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
