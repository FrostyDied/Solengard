using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Stats")]
    public float attackDamage   = 30f;
    public float attackRange    = 6.5f;
    public float attackCooldown = 0.3f;

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
        SpawnSpinSlash();

        if (PlayerController.Instance != null)
            PlayerController.Instance.LastAttackTime = Time.time;

        var hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);
        foreach (var h in hits)
        {
            if (h == null) continue;
            var enemy = h.GetComponent<EnemyBase>();
            if (enemy != null) enemy.TakeDamage(attackDamage);
        }
    }

    void SpawnSpinSlash()
    {
        if (slashEffectPrefab != null)
        {
            for (int i = 0; i < 4; i++)
            {
                float   angle = i * 90f;
                float   rad   = angle * Mathf.Deg2Rad;
                Vector3 pos   = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * attackRange * 0.45f;
                var fx = Instantiate(slashEffectPrefab, pos, Quaternion.Euler(0, 0, angle));
                Destroy(fx, slashDuration);
            }
        }
        else
        {
            StartCoroutine(ProceduralSpinSlash());
        }
    }

    IEnumerator ProceduralSpinSlash()
    {
        const int count = 4;
        var fxObjs = new GameObject[count];
        var fxSrs  = new SpriteRenderer[count];
        var fxDirs = new Vector3[count];
        var playerSr = GetComponent<SpriteRenderer>();

        for (int i = 0; i < count; i++)
        {
            float angle = i * 90f;
            float rad   = angle * Mathf.Deg2Rad;
            fxDirs[i]   = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            var fx = new GameObject("SpinFX");
            fx.transform.position = transform.position;
            fx.transform.rotation = Quaternion.Euler(0, 0, angle);

            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite       = playerSr?.sprite;
            sr.color        = new Color(1f, 0.9f, 0.3f, 0.85f);
            sr.sortingOrder = 40;
            fx.transform.localScale = Vector3.one * 0.5f;

            fxObjs[i] = fx;
            fxSrs[i]  = sr;
        }

        float t = 0f;
        while (t < slashDuration)
        {
            t += Time.deltaTime;
            float ratio = t / slashDuration;
            for (int i = 0; i < count; i++)
            {
                if (fxObjs[i] == null) continue;
                fxObjs[i].transform.position  = transform.position + fxDirs[i] * Mathf.Lerp(0f, attackRange * 0.6f, ratio);
                fxObjs[i].transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.1f, ratio);
                fxSrs[i].color = new Color(1f, 0.9f, 0.3f, Mathf.Lerp(0.85f, 0f, ratio));
            }
            yield return null;
        }

        for (int i = 0; i < count; i++)
            if (fxObjs[i] != null) Destroy(fxObjs[i]);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
