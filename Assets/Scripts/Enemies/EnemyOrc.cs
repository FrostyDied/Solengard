using UnityEngine;

// Inimigo tanque. Lento e resistente. Tem 30% de chance de dropar um PowerPickup ao morrer.
public class EnemyOrc : EnemyBase
{
    protected override void Awake()
    {
        maxHealth = 80f;
        moveSpeed = 0.9f;
        damage    = 20f;
        base.Awake();
    }

    protected override void OnDie()
    {
        if (Random.value < 0.3f)
            SpawnPowerPickup();
    }

    void SpawnPowerPickup()
    {
        var go = new GameObject("PowerPickup");
        go.transform.position = transform.position;
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<PowerPickup>();
    }
}
