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

    GameObject _vfxMelee;
    GameObject _vfxMagicImpact;
    GameObject _vfxArrowImpact;
    GameObject _vfxSummonImpact;

    Sprite[] _slashFrames;

    void Awake()
    {
        weapon = GetComponent<PlayerWeapon>();
        _anim  = GetComponent<CharacterAnimator>();
        SyncFromWeapon();
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Enemy");
        LoadVFX();
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

    void LoadVFX()
    {
        _vfxMelee        = Resources.Load<GameObject>("VFX/AoE slash orange");
        _vfxMagicImpact  = Resources.Load<GameObject>("VFX/Crystal effect blue");
        _vfxArrowImpact  = Resources.Load<GameObject>("VFX/Sparks explode blue");
        _vfxSummonImpact = Resources.Load<GameObject>("VFX/Stones hit");
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

        Vector3 vfxPos = transform.position + (Vector3)(attackDir * 1.5f);
        float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg - 90f;
        SpriteVFX.Spawn(_slashFrames, vfxPos, angle, 1.5f, 0.3f, 16f);

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

        switch (_attackType)
        {
            case AttackType.Melee360:
                SpawnVFX(_vfxMelee, transform.position, 0.5f, 1.5f);
                break;

            case AttackType.Melee180:
            {
                Vector3 pos   = transform.position + (Vector3)((Vector2)facing * 1.5f);
                float   angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f;
                SpriteVFX.Spawn(_slashFrames, pos, angle, 1.2f, 0.3f, 16f);
                break;
            }

            case AttackType.MeleeCone:
            {
                var nearestInCone = GetNearestEnemyInCone(facing, _attackArc);
                Vector3 pos = nearestInCone != null
                    ? nearestInCone.transform.position
                    : transform.position + (Vector3)((Vector2)facing * 1.5f);
                float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f;
                SpriteVFX.Spawn(_slashFrames, pos, angle, 0.6f, 0.3f, 16f);
                break;
            }

            default:
                SpawnVFX(_vfxMelee, transform.position, 0.5f, 1f);
                break;
        }

        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(enemyLayerMask);
        var results = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, attackRange, filter, results);

        foreach (var col in results)
        {
            if (col == null) continue;
            if (arc < 360f && !InArc(col.transform.position, arc)) continue;
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

        float scale = hasAnimFrames || hasStaticSprite
            ? (classDef != null ? classDef.projectileScale : 1f) * 0.5f
            : 1f;
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
        if (arc >= 360f) return true;
        var dir    = ((Vector2)(targetPos - transform.position)).normalized;
        var facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection
            : Vector2.right;
        return Vector2.Angle(facing, dir) <= arc * 0.5f;
    }

    static GameObject SpawnVFX(GameObject prefab, Vector3 pos, float lifetime, float scale = 1f)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, pos, Quaternion.identity);
        if (scale != 1f) go.transform.localScale = Vector3.one * scale;
        Destroy(go, lifetime);
        return go;
    }

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

        var cls = PlayerClassManager.Instance?.CurrentClass;
        if (cls != null)
            _slashFrames = cls.attackFrames;
    }

    // ── Gizmos ──────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
