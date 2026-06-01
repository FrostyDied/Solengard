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

    static Sprite _slashSprite;

    IEnumerator ProceduralSpinSlash()
    {
        const int rays = 8;
        var fxList = new System.Collections.Generic.List<GameObject>();

        if (_slashSprite == null) _slashSprite = MakeSlashSprite();

        for (int i = 0; i < rays; i++)
        {
            float angle = (360f / rays) * i;
            float rad   = angle * Mathf.Deg2Rad;
            var   dir   = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var fx = new GameObject("SlashRay");
            fx.transform.SetParent(transform);
            fx.transform.localPosition = dir * attackRange * 0.4f;
            fx.transform.rotation      = Quaternion.Euler(0, 0, angle);

            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            sr.color        = new Color(1f, 0.9f, 0.2f, 0.9f);
            sr.sprite       = _slashSprite;
            fx.transform.localScale = new Vector3(attackRange * 0.4f, 0.15f, 1f);

            fxList.Add(fx);
        }

        float t   = 0f;
        float dur = slashDuration;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            foreach (var fx in fxList)
            {
                if (fx == null) continue;
                fx.transform.localScale = new Vector3(
                    attackRange * Mathf.Lerp(0.3f, 0.7f, p),
                    Mathf.Lerp(0.2f, 0.05f, p), 1f);
                var sr = fx.GetComponent<SpriteRenderer>();
                if (sr) sr.color = new Color(1f, 0.9f, 0.2f, Mathf.Lerp(0.9f, 0f, p));
            }
            yield return null;
        }

        foreach (var fx in fxList)
            if (fx != null) Destroy(fx);
    }

    static Sprite MakeSlashSprite()
    {
        var tex = new Texture2D(16, 4);
        for (int x = 0; x < 16; x++)
        {
            float t = (float)x / 16f;
            float a = Mathf.Lerp(1f, 0f, t);
            tex.SetPixel(x, 0, new Color(1f, 1f,  0.5f, a * 0.5f));
            tex.SetPixel(x, 1, new Color(1f, 0.9f, 0.2f, a));
            tex.SetPixel(x, 2, new Color(1f, 0.9f, 0.2f, a));
            tex.SetPixel(x, 3, new Color(1f, 1f,  0.5f, a * 0.5f));
        }
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 16, 4), new Vector2(0f, 0.5f), 16f);
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
