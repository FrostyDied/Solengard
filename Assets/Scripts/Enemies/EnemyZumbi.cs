using UnityEngine;

// Inimigo cannon fodder. Fraco e rápido — base do sistema de waves.
public class EnemyZumbi : EnemyBase
{
    protected override void Awake()
    {
        maxHealth = 35f;
        moveSpeed = 1.2f;
        damage    = 5f;
        base.Awake();
    }
}
