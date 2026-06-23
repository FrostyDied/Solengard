using UnityEngine;

public class EnemyLizardman : EnemyBase
{
    protected override void Awake()
    {
        maxHealth     = 30f;
        moveSpeed     = 2.0f;
        contactDamage = 12f;
        base.Awake();
    }
}
