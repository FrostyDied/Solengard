using System.Collections;
using UnityEngine;

// Inimigo de longa distância. Mantém distância mínima do player e atira projéteis.
public class EnemyArcher : EnemyBase
{
    [Header("Archer")]
    public float minRange      = 6f;
    public float shootInterval = 2.5f;

    const string ProjectileTag = "EnemyProjectile";

    protected override void Awake()
    {
        maxHealth = 25f;
        moveSpeed = 1.0f;
        damage    = 6f;
        minRange  = Random.Range(5f, 8f);
        base.Awake();
        EnsureProjectilePool();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(ShootRoutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllCoroutines();
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
        proj.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);
        var ep = proj.GetComponent<EnemyProjectile>();
        if (ep != null) { ep.poolTag = ProjectileTag; ep.Launch(dir, damage); }
    }

    // Registra a pool de projéteis se ainda não existe no ObjectPoolManager
    static void EnsureProjectilePool()
    {
        if (ObjectPoolManager.Instance == null) return;
        if (ObjectPoolManager.Instance.HasPool(ProjectileTag)) return;

        var template = new GameObject("EnemyProjectile_Template");
        template.AddComponent<EnemyProjectile>();
        var col = template.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.15f;

        var sr = template.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeArrowSprite();
        sr.sortingOrder = 10;

        template.SetActive(false);
        try { template.tag = ProjectileTag; } catch { }
        DontDestroyOnLoad(template);

        ObjectPoolManager.Instance.RegisterPool(ProjectileTag, template, 30);
    }

    static Sprite MakeArrowSprite()
    {
        int w = 11, h = 4;
        var tex   = new Texture2D(w, h);
        var clear = new Color(0f, 0f, 0f, 0f);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, clear);

        var c = new Color(1f, 0.95f, 0.7f);
        for (int x = 0; x < 9; x++) { tex.SetPixel(x, 1, c); tex.SetPixel(x, 2, c); }
        tex.SetPixel(9,  0, c); tex.SetPixel(9,  1, c); tex.SetPixel(9,  2, c); tex.SetPixel(9,  3, c);
        tex.SetPixel(10, 1, c); tex.SetPixel(10, 2, c);

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 16f);
    }
}
