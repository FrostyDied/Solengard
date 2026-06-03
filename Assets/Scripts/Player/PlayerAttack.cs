using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Stats")]
    public float attackDamage   = 30f;
    public float attackRange    = 6.5f;
    public float attackCooldown = 0.3f;

    [Header("Detecção")]
    public LayerMask enemyLayerMask;

    PlayerWeapon weapon;
    float _cooldownTimer;

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
            Attack();
            _cooldownTimer = attackCooldown;
        }
    }

    void Attack()
    {
        SpawnSpinSlash();

        if (PlayerController.Instance != null)
            PlayerController.Instance.LastAttackTime = Time.time;

        var anim = GetComponent<CharacterAnimator>();
        if (anim != null)
        {
            anim.SetState(CharacterAnimator.State.Attack);
            StartCoroutine(ResetToWalk(anim));
        }

        var hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);
        Debug.Log($"[PlayerAttack] {hits.Length} inimigos no range (mask={enemyLayerMask.value})");
        foreach (var h in hits)
        {
            if (h == null) continue;
            var enemy = h.GetComponent<EnemyBase>() ?? h.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                if (enemy.isBoss)
                    Debug.Log($"[PlayerAttack] Acertou boss {enemy.name} com {attackDamage:F0} dano");
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    IEnumerator ResetToWalk(CharacterAnimator anim)
    {
        yield return new WaitForSeconds(attackCooldown * 0.8f);
        if (anim != null) anim.SetState(CharacterAnimator.State.Walk);
    }

    void SpawnSpinSlash()
    {
        VFXManager.Instance?.SpawnAttackAoE(transform.position);
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
