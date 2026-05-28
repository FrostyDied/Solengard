using UnityEngine;

// Gerencia o auto-ataque do player: a cada cooldown, detecta o inimigo mais
// próximo dentro do range e aplica dano diretamente via EnemyBase.TakeDamage.
// Attach este componente no mesmo GameObject que PlayerController.
public class PlayerAttack : MonoBehaviour
{
    [Header("Atributos de Ataque")]
    public float attackDamage   = 35f;
    public float attackRange    = 3f;
    public float attackCooldown = 0.6f;

    [Header("Detecção")]
    // Layer "Enemy" deve estar configurada no projeto para que a detecção funcione
    public LayerMask enemyLayerMask;

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

        Debug.Log($"[PlayerAttack] Atacando {alvo.name} — damage={attackDamage}");
        alvo.TakeDamage(attackDamage);
    }

    public void SyncFromWeapon()
    {
        if (weapon == null) return;
        attackDamage   = weapon.damage;
        attackRange    = weapon.attackRange;
        attackCooldown = 1f / Mathf.Max(weapon.attackSpeed, 0.01f);
    }

    void AoUpgradeArma(PlayerWeapon pw) => SyncFromWeapon();

    // Retorna o EnemyBase mais próximo dentro do attackRange, ou null se não houver nenhum
    EnemyBase EncontrarInimigoMaisProximo()
    {
        Collider2D[] colisores = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);

        EnemyBase maisProximo  = null;
        float menorDistancia   = float.MaxValue;

        foreach (Collider2D col in colisores)
        {
            EnemyBase inimigo = col.GetComponent<EnemyBase>();

            if (inimigo == null) continue;

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

    // Exibe o range de ataque apenas quando o objeto está selecionado no editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
