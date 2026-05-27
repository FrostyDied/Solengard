using UnityEngine;

// Aliado temporário do InvocarAliado. Persegue o inimigo mais próximo e aplica dano.
// Criado via código por TemporaryPowerSystem — adicione SpriteRenderer ao prefab para visual.
public class TemporaryAlly : MonoBehaviour
{
    public float lifetime       = 15f;
    public float attackDamage   = 15f;
    public float attackRange    = 3f;
    public float attackCooldown = 1.2f;
    public float moveSpeed      = 3.5f;

    float       timer;
    float       cooldown;
    Rigidbody2D rb;

    void Start()
    {
        rb                = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        timer = lifetime;
    }

    void Update()
    {
        timer    -= Time.deltaTime;
        cooldown -= Time.deltaTime;
        if (timer <= 0f) { Destroy(gameObject); return; }

        EnemyBase target = FindNearest();
        if (target == null) { if (rb != null) rb.linearVelocity = Vector2.zero; return; }

        Vector2 dir  = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        float   dist = Vector2.Distance(transform.position, target.transform.position);

        rb.linearVelocity = dist > attackRange ? dir * moveSpeed : Vector2.zero;

        if (cooldown <= 0f && dist <= attackRange)
        {
            target.TakeDamage(attackDamage);
            cooldown = attackCooldown;
        }
    }

    EnemyBase FindNearest()
    {
        var enemies  = Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        EnemyBase nearest = null;
        float     best    = float.MaxValue;

        foreach (var e in enemies)
        {
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < best) { best = d; nearest = e; }
        }
        return nearest;
    }
}
