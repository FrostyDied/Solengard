using UnityEngine;

public class EnemyVampire : EnemyBase
{
    protected override void Awake()
    {
        maxHealth     = 28f;
        moveSpeed     = 2.2f;
        contactDamage = 16f;
        base.Awake();
    }
}
