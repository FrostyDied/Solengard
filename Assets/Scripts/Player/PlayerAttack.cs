using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Stats")]
    public float attackDamage   = 30f;
    public float attackRange    = 6.5f;
    public float attackCooldown = 0.3f;

    [Header("Detecção")]
    public LayerMask enemyLayerMask;

    AttackType _attackType      = AttackType.Melee360;
    float      _attackArc       = 270f;
    int        _projectileCount = 1;
    bool       _meleeAlt;

    PlayerWeapon      weapon;
    CharacterAnimator _anim;
    float             _cooldownTimer;

    void Awake()
    {
        weapon = GetComponent<PlayerWeapon>();
        _anim  = GetComponent<CharacterAnimator>();
        SyncFromWeapon();
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Enemy");
        _cooldownTimer = attackCooldown;
    }

    void Start() => StartCoroutine(WaitAndApplyClass());

    IEnumerator WaitAndApplyClass()
    {
        yield return null;
        var cls = PlayerClassManager.Instance?.CurrentClass;
        if (cls != null)
            SetClassConfig(cls.attackDamage, cls.attackRange, cls.attackInterval,
                cls.attackType, cls.attackArc, cls.projectileCount);
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
        if (PlayerClassManager.Instance?.CurrentClass == null) return;
        if (PlayerController.Instance != null)
            PlayerController.Instance.LastAttackTime = Time.time;

        switch (_attackType)
        {
            case AttackType.MeleeDirectional: AttackGuerreiro();  break;
            case AttackType.Melee180:         AttackPaladino();   break;
            case AttackType.MeleeCone:        AttackAssassino();  break;
            case AttackType.RangedSingle:     AttackMago();       break;
            case AttackType.RangedMulti:      AttackCacador();    break;
            case AttackType.RangedSummon:     AttackNecromante(); break;
            default:                          AttackMelee360();   break;
        }
    }

    // ── Guerreiro (MeleeDirectional) ─────────────────────────────────────────────

    void AttackGuerreiro()
    {
        var facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection : Vector2.right;
        Vector2 attackDir = _meleeAlt ? -facing : facing;
        _meleeAlt = !_meleeAlt;

        StartCoroutine(ProceduralVFX.Whip(
            transform.position, attackDir,
            length: attackRange, duration: 0.4f,
            color: new Color(1f, 0.5f, 0.1f),
            width: 0.18f, amplitude: 0.5f
        ));

        // Cone de 90° (±45° da direção do ataque)
        ApplyDamageCone(attackDir, 45f);
    }

    // ── Paladino (Melee180) ───────────────────────────────────────────────────────

    void AttackPaladino()
    {
        Vector2 attackDir = GetAttackDirection();

        StartCoroutine(ProceduralVFX.SlashArc(
            transform.position, attackDir,
            arcDegrees: 180f, radius: attackRange,
            duration: 0.35f,
            color: new Color(1f, 0.9f, 0.3f),
            width: 0.25f
        ));

        ApplyDamageArc(attackDir, 180f);
    }

    // ── Assassino (MeleeCone) ─────────────────────────────────────────────────────

    void AttackAssassino()
    {
        Vector2 attackDir = GetAttackDirection();

        StartCoroutine(ProceduralVFX.DaggerFlash(
            transform.position + (Vector3)(attackDir * 0.5f),
            attackDir,
            length: attackRange,
            color: new Color(0.9f, 0.1f, 0.1f)
        ));
        StartCoroutine(ProceduralVFX.SlashArc(
            transform.position, attackDir,
            arcDegrees: 60f, radius: attackRange * 0.8f,
            duration: 0.15f,
            color: new Color(0.8f, 0.0f, 0.0f)
        ));

        ApplyDamageArc(attackDir, _attackArc);
    }

    // ── Mago (RangedSingle) ───────────────────────────────────────────────────────

    void AttackMago()
    {
        var targets = GetNearestEnemies(1);
        if (targets.Count == 0) return;

        var target = targets[0];
        Vector2 dir   = ((Vector2)(target.transform.position - transform.position)).normalized;
        float   range = Mathf.Min(Vector2.Distance(transform.position, target.transform.position), attackRange);

        StartCoroutine(ProceduralVFX.EnergyBolt(
            transform.position, dir,
            speed: 18f, range: range,
            coreColor:  new Color(0.3f, 0.5f, 1f),
            trailColor: new Color(0.6f, 0.3f, 1f),
            size: 0.2f,
            onHit: hitPos =>
            {
                StartCoroutine(ProceduralVFX.ExplosionRing(hitPos, new Color(0.4f, 0.6f, 1f), 1.5f, 0.3f));
                ApplyDamageAtPoint(hitPos, 0.5f);
            }
        ));
    }

    // ── Necromante (RangedSummon) ─────────────────────────────────────────────────

    void AttackNecromante()
    {
        var targets = GetNearestEnemies(1);
        if (targets.Count == 0) return;

        var target = targets[0];
        Vector2 dir   = ((Vector2)(target.transform.position - transform.position)).normalized;
        float   range = Mathf.Min(Vector2.Distance(transform.position, target.transform.position), attackRange);

        StartCoroutine(ProceduralVFX.EnergyBolt(
            transform.position, dir,
            speed: 14f, range: range,
            coreColor:  new Color(0.2f, 0.8f, 0.3f),
            trailColor: new Color(0.1f, 0.4f, 0.1f),
            size: 0.18f,
            onHit: hitPos =>
            {
                StartCoroutine(ProceduralVFX.ExplosionRing(hitPos, new Color(0.2f, 0.6f, 0.2f), 1.2f, 0.25f));
                ApplyDamageAtPoint(hitPos, 0.5f);
            }
        ));
    }

    // ── Caçador (RangedMulti) ─────────────────────────────────────────────────────

    void AttackCacador()
    {
        var targets = GetNearestEnemies(_projectileCount);
        foreach (var target in targets)
        {
            Vector2 dirToTarget = ((Vector2)(target.transform.position - transform.position)).normalized;

            StartCoroutine(ProceduralVFX.ArrowStreak(
                transform.position, dirToTarget,
                speed: 22f, range: attackRange,
                color: new Color(0.4f, 0.9f, 1f)
            ));

            if (target.isBoss) Debug.Log($"[PlayerAttack] Acertou boss {target.name} — {attackDamage:F0} dmg");
            target.TakeDamage(attackDamage);
        }
    }

    // ── Fallback (Melee360) ───────────────────────────────────────────────────────

    void AttackMelee360() => ApplyDamageArc(Vector2.right, 360f);

    // ── Helpers de dano ───────────────────────────────────────────────────────────

    void ApplyDamageCone(Vector2 dir, float halfAngle)
    {
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var results = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, attackRange, filter, results);
        foreach (var col in results)
        {
            if (col == null) continue;
            var toEnemy = ((Vector2)(col.transform.position - transform.position)).normalized;
            if (Vector2.Angle(dir, toEnemy) > halfAngle) continue;
            DamageCollider(col);
        }
    }

    void ApplyDamageArc(Vector2 dir, float arc)
    {
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var results = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, attackRange, filter, results);
        foreach (var col in results)
        {
            if (col == null) continue;
            if (arc < 360f && !InArc(col.transform.position, arc, dir)) continue;
            DamageCollider(col);
        }
    }

    void ApplyDamageAtPoint(Vector3 hitPos, float radius)
    {
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var results = new List<Collider2D>();
        Physics2D.OverlapCircle(hitPos, radius, filter, results);
        foreach (var col in results)
        {
            if (col == null) continue;
            DamageCollider(col);
        }
    }

    void DamageCollider(Collider2D col)
    {
        var enemy = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
        if (enemy == null) return;
        if (enemy.isBoss) Debug.Log($"[PlayerAttack] Acertou boss {enemy.name} — {attackDamage:F0} dmg");
        enemy.TakeDamage(attackDamage);
    }

    // ── Helpers de detecção ───────────────────────────────────────────────────────

    List<EnemyBase> GetNearestEnemies(int count)
    {
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var cols = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, attackRange, filter, cols);

        var seen    = new HashSet<EnemyBase>();
        var enemies = new List<EnemyBase>();
        foreach (var col in cols)
        {
            if (col == null) continue;
            var e = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
            if (e != null && !e.IsDead && seen.Add(e)) enemies.Add(e);
        }

        enemies.Sort((a, b) =>
            Vector2.Distance(transform.position, a.transform.position)
                .CompareTo(Vector2.Distance(transform.position, b.transform.position)));

        return enemies.Count > count ? enemies.GetRange(0, count) : enemies;
    }

    EnemyBase GetNearestEnemyInCone(Vector2 dir, float arc)
    {
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var cols = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, attackRange, filter, cols);

        EnemyBase nearest = null;
        float     minDist = float.MaxValue;
        foreach (var col in cols)
        {
            if (col == null) continue;
            var toEnemy = ((Vector2)(col.transform.position - transform.position)).normalized;
            if (Vector2.Angle(dir, toEnemy) > arc * 0.5f) continue;
            var e = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
            if (e == null || e.IsDead) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e; }
        }
        return nearest;
    }

    bool InArc(Vector3 targetPos, float arc, Vector2 dir)
    {
        if (arc >= 360f) return true;
        var toTarget = ((Vector2)(targetPos - transform.position)).normalized;
        return Vector2.Angle(dir, toTarget) <= arc * 0.5f;
    }

    Vector2 GetAttackDirection()
    {
        var facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection : Vector2.right;

        var inArc = GetNearestEnemyInCone(facing, _attackArc);
        if (inArc != null)
            return ((Vector2)(inArc.transform.position - transform.position)).normalized;

        var nearest = GetNearestEnemy(attackRange * 1.5f);
        if (nearest != null)
            return ((Vector2)(nearest.transform.position - transform.position)).normalized;

        return facing;
    }

    EnemyBase GetNearestEnemy(float range)
    {
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var cols = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, range, filter, cols);

        EnemyBase nearest = null;
        float     minDist = float.MaxValue;
        foreach (var col in cols)
        {
            if (col == null) continue;
            var e = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
            if (e == null || e.IsDead) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e; }
        }
        return nearest;
    }

    // ── Weapon sync ───────────────────────────────────────────────────────────────

    public void SyncFromWeapon()
    {
        if (weapon == null) return;
        attackDamage   = weapon.damage;
        attackRange    = weapon.attackRange;
        attackCooldown = 1f / Mathf.Max(weapon.attackSpeed, 0.01f);
    }

    void AoUpgradeArma(PlayerWeapon pw) => SyncFromWeapon();

    public void SetClassConfig(float dmg, float range, float interval,
        AttackType type, float arc, int projCount)
    {
        attackDamage     = dmg;
        attackRange      = range;
        attackCooldown   = interval;
        _attackType      = type;
        _attackArc       = arc;
        _projectileCount = projCount;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
