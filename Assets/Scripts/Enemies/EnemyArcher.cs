using System.Collections;
using UnityEngine;

// Inimigo de longa distância. Mantém distância mínima do player e atira projéteis.
public class EnemyArcher : EnemyBase
{
    [Header("Archer")]
    public float minRange      = 6f;
    public float shootInterval = 2.5f;

    const string ProjectileTag = "EnemyProjectile";

    static bool poolRegistered;

    protected override void Awake()
    {
        maxHealth = 40f;
        moveSpeed = 1.5f;
        damage    = 10f;
        base.Awake();
        EnsureProjectilePool();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(ShootRoutine());
    }

    // Sobrescreve o movimento padrão: recua se muito perto, avança se muito longe
    protected override void FixedUpdate()
    {
        if (playerTransform == null) return;

        float   dist = Vector2.Distance(rb.position, (Vector2)playerTransform.position);
        Vector2 dir  = ((Vector2)playerTransform.position - rb.position).normalized;

        if (dist < minRange)
            rb.linearVelocity = -dir * moveSpeed;
        else if (dist > minRange + 1f)
            rb.linearVelocity = dir * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;
    }

    IEnumerator ShootRoutine()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            if (playerTransform != null) Shoot();
            yield return new WaitForSeconds(shootInterval);
        }
    }

    void Shoot()
    {
        Vector2 dir  = ((Vector2)playerTransform.position - rb.position).normalized;
        GameObject proj = ObjectPoolManager.Instance?.GetFromPool(ProjectileTag);
        if (proj == null) return;

        proj.transform.position = transform.position;
        var ep = proj.GetComponent<EnemyProjectile>();
        if (ep != null) { ep.poolTag = ProjectileTag; ep.Launch(dir, damage); }
    }

    // Registra a pool de projéteis na primeira vez que um Archer acorda
    static void EnsureProjectilePool()
    {
        if (poolRegistered || ObjectPoolManager.Instance == null) return;

        var template = new GameObject("EnemyProjectile_Template");
        template.AddComponent<EnemyProjectile>();
        var col = template.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.15f;
        template.SetActive(false);
        DontDestroyOnLoad(template);

        ObjectPoolManager.Instance.RegisterPool(ProjectileTag, template, 30);
        poolRegistered = true;
    }
}
