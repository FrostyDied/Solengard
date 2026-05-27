using UnityEngine;

// Projétil disparado por EnemyArcher. Gerenciado via ObjectPoolManager (tag "EnemyProjectile").
[RequireComponent(typeof(CircleCollider2D))]
public class EnemyProjectile : MonoBehaviour
{
    public float speed    = 8f;
    public float lifetime = 3f;

    [HideInInspector] public float  damage;
    [HideInInspector] public string poolTag;

    Vector2 direction;
    float   timer;

    public void Launch(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage    = dmg;
        timer     = lifetime;
    }

    void OnEnable()
    {
        timer = lifetime;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
        timer -= Time.deltaTime;
        if (timer <= 0f) ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;
        ph.TakeDamage(damage);
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null && !string.IsNullOrEmpty(poolTag))
            ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
        else
            Destroy(gameObject);
    }
}
