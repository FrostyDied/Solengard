using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Stats")]
    public float attackDamage   = 25f;
    public float attackRange    = 5f;
    public float attackCooldown = 0.5f;

    [Header("Cone de ataque (graus) — amplitude do golpe na direção encarada")]
    [SerializeField] float attackConeAngle = 120f;

    [Header("Efeito visual da espada")]
    [SerializeField] GameObject slashEffectPrefab;
    [SerializeField] float slashDuration = 0.18f;

    [Header("Detecção")]
    public LayerMask enemyLayerMask;

    PlayerWeapon weapon;
    float _cooldownTimer;

    void Awake()
    {
        weapon = GetComponent<PlayerWeapon>();
        SyncFromWeapon();
        if (enemyLayerMask == 0) enemyLayerMask = LayerMask.GetMask("Enemy");
        if (slashEffectPrefab == null) TryLoadSlashPrefab();
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
        // Direção que o herói encara — nunca zero, mantém a última mesmo parado
        Vector2 facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection
            : Vector2.right;
        if (facing == Vector2.zero) facing = Vector2.right;

        // Efeito de espada SEMPRE aparece, mesmo sem inimigos na frente
        SpawnSlash(facing);

        if (PlayerController.Instance != null)
            PlayerController.Instance.LastAttackTime = Time.time;

        var hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);
        if (hits.Length == 0) return;

        float halfCone = attackConeAngle * 0.5f;

        foreach (var h in hits)
        {
            if (h == null) continue;

            Vector2 toEnemy = ((Vector2)h.transform.position - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(facing, toEnemy);
            if (angle > halfCone) continue; // fora do cone — não é atingido

            var enemy = h.GetComponent<EnemyBase>();
            if (enemy != null) enemy.TakeDamage(attackDamage);
        }
    }

    void SpawnSlash(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * attackRange * 0.5f);

        if (slashEffectPrefab != null)
        {
            var fx  = Instantiate(slashEffectPrefab, spawnPos, Quaternion.identity);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            fx.transform.rotation = Quaternion.Euler(0, 0, ang);
            if (dir.x < 0)
                fx.transform.localScale = new Vector3(-fx.transform.localScale.x,
                                                       fx.transform.localScale.y,
                                                       fx.transform.localScale.z);
            Destroy(fx, slashDuration);
        }
        else
        {
            StartCoroutine(ProceduralSlash(spawnPos, dir));
        }
    }

    IEnumerator ProceduralSlash(Vector3 pos, Vector2 dir)
    {
        var fx = new GameObject("SlashFX");
        fx.transform.position = pos;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        fx.transform.rotation = Quaternion.Euler(0, 0, ang);

        var sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite       = GetComponent<SpriteRenderer>()?.sprite;
        sr.color        = new Color(1f, 1f, 1f, 0.85f);
        sr.sortingOrder = 40;
        fx.transform.localScale = Vector3.one * 0.6f;

        float t = 0f;
        while (t < slashDuration)
        {
            t += Time.deltaTime;
            float ratio = t / slashDuration;
            sr.color              = new Color(1f, 1f, 1f, Mathf.Lerp(0.85f, 0f, ratio));
            fx.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.1f, ratio);
            yield return null;
        }
        Destroy(fx);
    }

    void TryLoadSlashPrefab()
    {
#if UNITY_EDITOR
        string[] paths = {
            "Assets/Prefabs/Effects/SwordSlash.prefab",
            "Assets/Prefabs/Effects/Slash.prefab",
            "Assets/Prefabs/Effects/AttackEffect.prefab",
            "Assets/Prefabs/Effects/HitEffect.prefab",
        };
        foreach (var p in paths)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) { slashEffectPrefab = go; return; }
        }
#endif
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
        Vector2 facing = PlayerController.Instance != null
            ? PlayerController.Instance.FacingDirection
            : Vector2.right;

        // Range circle
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Attack cone edges
        float half     = attackConeAngle * 0.5f;
        Vector3 leftE  = Quaternion.Euler(0, 0,  half) * (Vector3)(facing * attackRange);
        Vector3 rightE = Quaternion.Euler(0, 0, -half) * (Vector3)(facing * attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftE);
        Gizmos.DrawLine(transform.position, transform.position + rightE);
    }
}
