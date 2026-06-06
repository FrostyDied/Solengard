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

    AttackType _attackType       = AttackType.Melee360;
    float      _attackArc        = 270f;
    int        _projectileCount  = 1;
    bool       _meleeAlt;

    PlayerWeapon      weapon;
    CharacterAnimator _anim;
    float             _cooldownTimer;

    static Sprite _dotSprite;

    const float PROJ_SPEED = 18f;

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
        {
            SetClassConfig(cls.attackDamage, cls.attackRange, cls.attackInterval,
                cls.attackType, cls.attackArc, cls.projectileCount);
        }
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
            case AttackType.Melee360:         AttackMelee(360f);             break;
            case AttackType.Melee180:         AttackMelee(180f);             break;
            case AttackType.MeleeCone:        AttackMelee(_attackArc);       break;
            case AttackType.MeleeDirectional: AttackMeleeDirectional();      break;
            case AttackType.RangedSingle:     AttackRanged(1);               break;
            case AttackType.RangedMulti:      AttackRanged(_projectileCount); break;
            case AttackType.RangedSummon:     AttackRangedSummon();          break;
            default:                          AttackMelee(360f);             break;
        }
    }

    // ── Melee ────────────────────────────────────────────────────────────────────

    void AttackMeleeDirectional()
    {
        var facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection
            : Vector2.right;

        Vector2 attackDir = _meleeAlt ? -facing : facing;
        _meleeAlt = !_meleeAlt;

        var   meleeFrames = EffectLibrary.GetFrames(GetMeleeEffect());
        float angle       = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
        for (int i = -1; i <= 1; i++)
        {
            var rotDir = Quaternion.Euler(0f, 0f, i * 25f) * new Vector3(attackDir.x, attackDir.y, 0f);
            SpriteVFX.Spawn(meleeFrames, transform.position + rotDir * 1.5f, angle + i * 25f, 0.8f, 0.25f, 30f);
        }

        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var results = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, attackRange, filter, results);

        foreach (var col in results)
        {
            if (col == null) continue;
            var dir = ((Vector2)(col.transform.position - transform.position)).normalized;
            if (Vector2.Angle(attackDir, dir) > 45f) continue;
            var enemy = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                if (enemy.isBoss) Debug.Log($"[PlayerAttack] Acertou boss {enemy.name} — {attackDamage:F0} dmg");
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    void AttackMelee(float arc)
    {
        var facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection
            : Vector2.right;

        // Melee180 e MeleeCone miram na direção do inimigo mais próximo (fallback: qualquer direção)
        Vector2   attackDir    = facing;
        EnemyBase nearestEnemy = null;
        if (_attackType == AttackType.Melee180 || _attackType == AttackType.MeleeCone)
        {
            attackDir    = GetAttackDirection();
            nearestEnemy = GetNearestEnemyInCone(attackDir, arc)
                        ?? GetNearestEnemy(attackRange * 1.5f);
        }

        switch (_attackType)
        {
            case AttackType.Melee360:
                SpriteVFX.Spawn(EffectLibrary.GetFrames(GetMeleeEffect()), transform.position, 0f, 0.5f, 0.35f);
                break;

            case AttackType.Melee180:
            {
                Vector3 pos    = transform.position + (Vector3)(attackDir * 1.5f);
                float angle180 = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
                SpriteVFX.Spawn(EffectLibrary.GetFrames(GetMeleeEffect()), pos, angle180, 0.35f, 0.35f);
                break;
            }

            case AttackType.MeleeCone:
            {
                Vector3 conePos = nearestEnemy != null
                    ? nearestEnemy.transform.position
                    : transform.position + (Vector3)(attackDir * 1.5f);
                float angleCone = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
                SpriteVFX.Spawn(EffectLibrary.GetFrames(GetMeleeEffect()), conePos, angleCone, 0.25f, 0.35f);
                break;
            }

            default:
                SpriteVFX.Spawn(EffectLibrary.GetFrames(GetMeleeEffect()), transform.position, 0f, 0.5f, 0.35f);
                break;
        }

        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var results = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, attackRange, filter, results);

        foreach (var col in results)
        {
            if (col == null) continue;
            if (arc < 360f && !InArc(col.transform.position, arc, attackDir)) continue;
            var enemy = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                if (enemy.isBoss) Debug.Log($"[PlayerAttack] Acertou boss {enemy.name} — {attackDamage:F0} dmg");
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    // ── Ranged ───────────────────────────────────────────────────────────────────

    void AttackRanged(int count)
    {
        var targets = GetNearestEnemies(count);
        foreach (var e in targets)
            FireProjectile((Vector2)(e.transform.position - transform.position));
    }

    void AttackRangedSummon()
    {
        var targets = GetNearestEnemies(1);
        foreach (var e in targets)
            FireProjectile((Vector2)(e.transform.position - transform.position));
    }

    void FireProjectile(Vector2 dir)
    {
        var classDef         = PlayerClassManager.Instance?.CurrentClass;
        bool hasAnimFrames   = classDef != null && classDef.projectileFrames != null && classDef.projectileFrames.Length > 0;
        bool hasStaticSprite = !hasAnimFrames && classDef != null && classDef.projectileSprite != null;

        var go = new GameObject("PlayerProjectile");
        go.transform.position = transform.position;

        int playerLayer = LayerMask.NameToLayer("Player");
        go.layer = playerLayer >= 0 ? playerLayer : 0;

        if (hasStaticSprite)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        float baseScale = hasAnimFrames || hasStaticSprite
            ? (classDef != null ? classDef.projectileScale : 1f) * 0.5f
            : 1f;
        float scale = _attackType == AttackType.RangedMulti ? baseScale * 1.5f : baseScale;
        go.transform.localScale = Vector3.one * scale;

        Debug.Log($"[Proj] {classDef?.classId} scale={scale} projectileScale={classDef?.projectileScale} hasFrames={hasAnimFrames}");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Characters";
        sr.sortingOrder     = 2;

        if (hasAnimFrames)
        {
            sr.sprite = classDef.projectileFrames[0];
            sr.color  = Color.white;
        }
        else if (hasStaticSprite)
        {
            sr.sprite = classDef.projectileSprite;
            sr.color  = Color.white;
        }
        else
        {
            sr.sprite = GetDotSprite();
            sr.color  = _attackType == AttackType.RangedMulti
                ? new Color(0.6f, 0.9f, 1.0f)
                : _attackType == AttackType.RangedSummon
                    ? new Color(0.4f, 1.0f, 0.5f)
                    : new Color(0.5f, 0.3f, 1.0f);
        }

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.18f;

        float lifetime = attackRange / PROJ_SPEED + 0.2f;

        var proj = go.AddComponent<PlayerProjectile>();
        proj.Init(attackDamage, dir, PROJ_SPEED, lifetime);

        if (hasAnimFrames)
            proj.SetFrames(classDef.projectileFrames);

        // VFX de impacto por classe
        string impactEffect = GetImpactEffect();
        if (!string.IsNullOrEmpty(impactEffect))
        {
            Sprite[] impactFrames = EffectLibrary.GetFrames(impactEffect);
            float impactScale = _attackType == AttackType.RangedSingle ? 0.5f : 0.4f;
            if (impactFrames.Length > 0)
                proj.SetImpactVFX(impactFrames, impactScale);
        }

        // Caçador: flash de disparo na posição do player
        if (_attackType == AttackType.RangedMulti)
        {
            var muzzleFrames = EffectLibrary.GetFrames("Slash/10");
            if (muzzleFrames.Length > 0)
            {
                Vector2 dirN  = dir.normalized;
                float   mAngle = Mathf.Atan2(dirN.y, dirN.x) * Mathf.Rad2Deg;
                SpriteVFX.Spawn(muzzleFrames, transform.position + (Vector3)(dirN * 0.5f), mAngle, 0.2f, 0.15f, 30f);
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

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
            if (e != null && seen.Add(e)) enemies.Add(e);
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
            if (e == null) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e; }
        }
        return nearest;
    }

    bool InArc(Vector3 targetPos, float arc)
    {
        var facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection
            : Vector2.right;
        return InArc(targetPos, arc, facing);
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
            ? PlayerController.Instance.FacingDirection
            : Vector2.right;

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
            if (e == null) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e; }
        }
        return nearest;
    }

    string GetMeleeEffect() => _attackType switch
    {
        AttackType.MeleeDirectional => "Guerreiro/Sword",
        AttackType.Melee180         => "Paladino",
        AttackType.MeleeCone        => "Assassino",
        _                           => "Slash/5",
    };

    string GetImpactEffect() => _attackType switch
    {
        AttackType.RangedSingle => "Mago/Attack",
        AttackType.RangedMulti  => "Cacador/Arrow",
        AttackType.RangedSummon => "Slash/2",
        _                       => "",
    };

    static Sprite GetDotSprite()
    {
        if (_dotSprite != null) return _dotSprite;
        const int   size = 12;
        const float ppu  = 32f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float alpha = Mathf.Clamp01((r - Mathf.Sqrt(dx * dx + dy * dy)) * 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        tex.Apply();
        _dotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
        return _dotSprite;
    }

    // ── Weapon sync ──────────────────────────────────────────────────────────────

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

    // ── Gizmos ──────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
