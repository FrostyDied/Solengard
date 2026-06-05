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

    PlayerWeapon weapon;
    float _cooldownTimer;

    static Sprite _dotSprite;

    GameObject _vfxMelee;
    GameObject _vfxMagicImpact;
    GameObject _vfxArrowImpact;
    GameObject _vfxSummonImpact;

    void Awake()
    {
        weapon = GetComponent<PlayerWeapon>();
        SyncFromWeapon();
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Enemy");
        LoadVFX();
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
        if (PlayerController.Instance != null)
            PlayerController.Instance.LastAttackTime = Time.time;

        switch (_attackType)
        {
            case AttackType.Melee360:     AttackMelee(360f);            break;
            case AttackType.Melee180:     AttackMelee(180f);            break;
            case AttackType.MeleeCone:    AttackMelee(_attackArc);      break;
            case AttackType.RangedSingle: AttackRanged(1);              break;
            case AttackType.RangedMulti:  AttackRanged(_projectileCount); break;
            case AttackType.RangedSummon: AttackRangedSummon();         break;
            default:                      AttackMelee(360f);            break;
        }
    }

    // ── Melee ────────────────────────────────────────────────────────────────────

    void AttackMelee(float arc)
    {
        SpawnVFX(_vfxMelee, transform.position, 0.5f);

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
        var targets   = GetNearestEnemies(count);
        var impactVFX = _attackType == AttackType.RangedMulti ? _vfxArrowImpact : _vfxMagicImpact;
        var color     = _attackType == AttackType.RangedMulti
            ? new Color(0.6f, 0.9f, 1.0f)   // caçador — azul claro (flecha)
            : new Color(0.5f, 0.3f, 1.0f);   // mago    — roxo (orbe)
        foreach (var e in targets)
            FireProjectile((Vector2)(e.transform.position - transform.position), color, impactVFX);
    }

    void AttackRangedSummon()
    {
        var targets = GetNearestEnemies(1);
        foreach (var e in targets)
            FireProjectile((Vector2)(e.transform.position - transform.position),
                new Color(0.4f, 1.0f, 0.5f), _vfxSummonImpact);   // necromante — verde osso
    }

    void FireProjectile(Vector2 dir, Color color, GameObject impactVFX)
    {
        var go = new GameObject("PlayerProjectile");
        go.transform.position = transform.position;
        go.layer = 0; // Default — colide com Enemy independente da layer matrix do player

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = GetDotSprite();
        sr.color            = color;
        sr.sortingLayerName = "Characters";
        sr.sortingOrder     = 2;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.18f;

        var proj = go.AddComponent<PlayerProjectile>();
        proj.Init(attackDamage, dir, 12f, impactVFX);
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

    bool InArc(Vector3 targetPos, float arc)
    {
        if (arc >= 360f) return true;
        var dir    = ((Vector2)(targetPos - transform.position)).normalized;
        var facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection
            : Vector2.right;
        return Vector2.Angle(facing, dir) <= arc * 0.5f;
    }

    static void SpawnVFX(GameObject prefab, Vector3 pos, float lifetime)
    {
        if (prefab == null) return;
        Destroy(Instantiate(prefab, pos, Quaternion.identity), lifetime);
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
    }

    // ── Gizmos ──────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
