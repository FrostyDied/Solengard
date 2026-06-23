using UnityEngine;

public class EnemyMushroom : EnemyBase
{
    protected override void Awake()
    {
        maxHealth     = 40f;
        moveSpeed     = 1.4f;
        contactDamage = 15f;
        base.Awake();
    }
}
