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

        // Chicote em C com ponta FIXA no player
        StartCoroutine(ProceduralVFX.WhipChain(
            transform,          // ponta fixa no player
            attackDir,
            length: attackRange * 0.8f,
            duration: 0.35f,
            color: new Color(0.6f, 0.85f, 1f)
        ));

        ApplyDamageCone(attackDir, 60f);
    }

    // ── Paladino (Melee180) ───────────────────────────────────────────────────────

    void AttackPaladino()
    {
        // Só ataca se há inimigo no range
        if (GetNearestEnemy(attackRange) == null) return;

        Vector2 attackDir = GetAttackDirection();
        Vector3 swordTip  = transform.position + (Vector3)(attackDir * 0.8f);

        StartCoroutine(ProceduralVFX.DaggerFlash(
            transform.position, attackDir,
            length: 1.2f,
            color: new Color(1f, 0.95f, 0.4f),
            width: 0.12f
        ));
        StartCoroutine(ProceduralVFX.SlashArc(
            swordTip, attackDir,
            arcDegrees: 100f, radius: attackRange * 0.5f,
            duration: 0.3f,
            color: new Color(1f, 0.9f, 0.3f),
            width: 0.15f
        ));

        StartCoroutine(PaladinoDamageArc(attackDir, 100f, attackRange * 0.5f, 0.3f));
    }

    // ── Assassino (MeleeCone) ─────────────────────────────────────────────────────

    void AttackAssassino()
    {
        // Só ataca se há inimigo DENTRO do attackRange
        EnemyBase nearestEnemy = GetNearestEnemyInCone(GetAttackDirection(), _attackArc)
                              ?? GetNearestEnemy(attackRange); // sem multiplicador

        if (nearestEnemy == null) return; // sem alvo no range = não ataca

        float   dmg       = attackDamage;
        Vector2 dirToEnemy = ((Vector2)(nearestEnemy.transform.position
                               - transform.position)).normalized;

        StartCoroutine(ProceduralVFX.StarProjectile(
            transform.position,
            dirToEnemy,
            speed: 14f,
            range: attackRange,
            color: new Color(0.9f, 0.1f, 0.1f),
            onHit: enemy =>
            {
                if (enemy == null || enemy.IsDead) return;
                enemy.TakeDamage(dmg);
                StartCoroutine(ProceduralVFX.CrossSlash(this,
                    enemy.transform.position, new Color(0.9f, 0f, 0.8f)));
            }
        ));
    }

    // ── Mago (RangedSingle) ───────────────────────────────────────────────────────

    void AttackMago()
    {
        var targets = GetNearestEnemies(1);
        if (targets.Count == 0) return;

        var target = targets[0];
        Vector2 dir = ((Vector2)(target.transform.position - transform.position)).normalized;
        // Usar attackRange fixo — garante que projétil alcança inimigo dentro do range
        float range = attackRange;

        StartCoroutine(ProceduralVFX.EnergyBolt(
            transform.position, dir,
            speed: 18f, range: range,
            coreColor:  new Color(0.2f, 0.6f, 1f),
            trailColor: new Color(1f, 0.6f, 0.1f),
            size: 0.2f,
            onHit: hitPos =>
            {
                StartCoroutine(ProceduralVFX.ExplosionRing(hitPos, new Color(0.4f, 0.6f, 1f), 1.5f, 0.3f));
                ApplyDamageAtPoint(hitPos, 0.5f); // era 0.1f — raio maior para garantir hit
            }
        ));
    }

    // ── Necromante (RangedSummon) ─────────────────────────────────────────────────

    void AttackNecromante()
    {
        var targets = GetNearestEnemies(1);
        if (targets.Count == 0) return;

        var target = targets[0];
        Vector2 dir = ((Vector2)(target.transform.position - transform.position)).normalized;

        StartCoroutine(ProceduralVFX.SkullProjectile(
            transform.position, dir, 8f, attackRange,
            onHit: hitPos =>
            {
                ApplyDamageAtPoint(hitPos, 0.5f);
                StartCoroutine(ProceduralVFX.ExplosionRing(
                    hitPos, new Color(0.3f, 0.9f, 0.3f), 0.8f, 0.25f));
            }
        ));
    }

    // ── Caçador (RangedMulti) ─────────────────────────────────────────────────────

    void AttackCacador()
    {
        var targets = GetNearestEnemies(_projectileCount);
        if (targets.Count == 0)
        {
            var facing = PlayerController.Instance != null
                ? PlayerController.Instance.FacingDirection : Vector2.right;
            StartCoroutine(ProceduralVFX.ArrowStreak(
                transform, facing,
                speed: 12f, range: attackRange,
                color: new Color(0.4f, 0.9f, 1f)
            ));
            StartCoroutine(ProceduralVFX.CrescentSlash(
                transform.position, facing,
                attackRange * 0.5f, 0.3f, new Color(0.4f, 0.9f, 1f)
            ));
            return;
        }

        foreach (var target in targets)
        {
            Vector2 dirToTarget = ((Vector2)(target.transform.position - transform.position)).normalized;
            float   dmg         = attackDamage;

            StartCoroutine(ProceduralVFX.ArrowStreak(
                transform, dirToTarget,
                speed: 12f, range: attackRange,
                color: new Color(0.4f, 0.9f, 1f),
                onHit: enemy =>
                {
                    if (enemy == null) return;
                    if (enemy.isBoss) Debug.Log($"[PlayerAttack] Acertou boss {enemy.name} — {dmg:F0} dmg");
                    enemy.TakeDamage(dmg);
                }
            ));
            StartCoroutine(ProceduralVFX.CrescentSlash(
                transform.position, dirToTarget,
                attackRange * 0.5f, 0.3f, new Color(0.4f, 0.9f, 1f)
            ));
        }
    }

    // ── Fallback (Melee360) ───────────────────────────────────────────────────────

    void AttackMelee360() => ApplyDamageArc(Vector2.right, 360f);

    IEnumerator PaladinoDamageArc(Vector2 dir, float arc, float maxRadius, float duration)
    {
        var hit    = new HashSet<EnemyBase>();
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var results = new List<Collider2D>();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float r = maxRadius * (t < 0.4f ? t / 0.4f : 1f);
            Physics2D.OverlapCircle(transform.position, r * 0.9f, filter, results);
            foreach (var col in results)
            {
                if (col == null) continue;
                if (!InArc(col.transform.position, arc, dir)) continue;
                var e = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
                if (e == null || e.IsDead || !hit.Add(e)) continue;
                if (e.isBoss) Debug.Log($"[PlayerAttack] Acertou boss {e.name} — {attackDamage:F0} dmg");
                e.TakeDamage(attackDamage);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator DelayedSlashArc(float delay, Vector3 originOffset, Vector2 dir,
        float arc, float radius, float duration, Color color, float width)
    {
        yield return new WaitForSeconds(delay);
        // Usar posição ATUAL do player + offset relativo
        Vector3 origin = transform.position + originOffset;
        yield return ProceduralVFX.SlashArc(origin, dir, arc, radius, duration, color, width);
    }

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

    void ApplyDamageArc(Vector2 dir, float arc, float searchRange = -1f)
    {
        float r = searchRange > 0f ? searchRange : attackRange;
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var results = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, r, filter, results);
        var hit = new HashSet<EnemyBase>(); // evita hits duplos
        foreach (var col in results)
        {
            if (col == null) continue;
            if (arc < 360f && !InArc(col.transform.position, arc, dir)) continue;
            var enemy = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.IsDead || !hit.Add(enemy)) continue;
            if (enemy.isBoss) Debug.Log($"[PlayerAttack] Acertou boss {enemy.name} — {attackDamage:F0} dmg");
            enemy.TakeDamage(attackDamage);
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
