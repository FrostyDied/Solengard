using UnityEngine;

public class EnemyRat : EnemyBase
{
    protected override void Awake()
    {
        maxHealth     = 12f;
        moveSpeed     = 3.0f;
        contactDamage = 8f;
        base.Awake();
    }
}
