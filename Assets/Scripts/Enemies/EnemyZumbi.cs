using UnityEngine;

// Inimigo cannon fodder. Fraco e rápido — base do sistema de waves.
public class EnemyZumbi : EnemyBase
{
    protected override void Awake()
    {
        maxHealth = 20f;
        moveSpeed = 1.2f;
        damage    = 6f;
        base.Awake();
    }
}
