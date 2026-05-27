using UnityEngine;

// Inimigo cannon fodder. Fraco e rápido — base do sistema de waves.
public class EnemyZumbi : EnemyBase
{
    protected override void Awake()
    {
        maxHealth = 15f;
        moveSpeed = 2.5f;
        damage    = 5f;
        base.Awake();
    }
}
