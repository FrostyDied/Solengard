using UnityEngine;

public class EnemyDarkElf : EnemyAssassin
{
    protected override void Awake()
    {
        base.Awake();
        maxHealth        = 35f;
        moveSpeed        = 2.5f;
        contactDamage    = 15f;
        stoppingDistance = 1.2f;
    }
}
