using UnityEngine;

// Gerencia o auto-ataque do player: a cada cooldown, detecta o inimigo mais
// próximo dentro do range e aplica dano diretamente via EnemyBase.TakeDamage.
public class PlayerAttack : MonoBehaviour
{
    [Header("Atributos de Ataque")]
    public float attackDamage   = 25f;
    public float attackRange    = 5f;
    public float attackCooldown = 0.4f;

    [Header("Detecção")]
    public LayerMask enemyLayerMask;

    [Header("Efeito Visual")]
    [SerializeField] GameObject attackEffectPrefab;

    PlayerWeapon weapon;
    float        _cooldownTimer;

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Awake()
    {
        weapon = GetComponent<PlayerWeapon>();
        SyncFromWeapon();
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    void OnEnable()  => PlayerWeapon.OnWeaponUpgraded += AoUpgradeArma;
    void OnDisable() => PlayerWeapon.OnWeaponUpgraded -= AoUpgradeArma;

    void Update()
    {
        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            TryAttack();
            _cooldownTimer = attackCooldown;
        }
    }

    // ── Lógica de ataque ────────────────────────────────────────────────────────

    void TryAttack()
    {
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Enemy");

        var hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);
        if (hits.Length == 0) return;

        Collider2D nearest = null;
        float      minDist = float.MaxValue;
        foreach (var h in hits)
        {
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < minDist) { minDist = d; nearest = h; }
        }
        if (nearest == null) return;

        var enemy = nearest.GetComponent<EnemyBase>();
        if (enemy != null) enemy.TakeDamage(attackDamage);

        Vector2 dir = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;

        if (PlayerController.Instance != null)
            PlayerController.Instance.LastAttackTime = Time.time;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && Mathf.Abs(dir.x) > 0.1f) sr.flipX = dir.x < 0f;

        SpawnAttackEffect(dir);
    }

    void SpawnAttackEffect(Vector2 dir)
    {
        if (attackEffectPrefab == null) return;
        var effectPos = transform.position + (Vector3)(dir * attackRange * 0.6f);
        var effect    = Instantiate(attackEffectPrefab, effectPos, Quaternion.identity);
        float angle   = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        effect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Destroy(effect, 0.2f);
    }

    // ── Weapon sync ─────────────────────────────────────────────────────────────

    public void SyncFromWeapon()
    {
        if (weapon == null) return;
        attackDamage   = weapon.damage;
        attackRange    = weapon.attackRange;
        attackCooldown = 1f / Mathf.Max(weapon.attackSpeed, 0.01f);
    }

    void AoUpgradeArma(PlayerWeapon pw) => SyncFromWeapon();

    // ── Gizmos ──────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
