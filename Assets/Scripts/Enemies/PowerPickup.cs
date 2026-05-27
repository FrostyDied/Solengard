using UnityEngine;

// Pickup gerado por EnemyOrc (30% de chance). Concede poder temporário ao tocar o player.
[RequireComponent(typeof(CircleCollider2D))]
public class PowerPickup : MonoBehaviour
{
    public float lifetime = 10f;

    float timer;

    void OnEnable()
    {
        timer = lifetime;
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.4f;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TemporaryPowerSystem.Instance?.GrantRandomPower();
        Destroy(gameObject);
    }
}
