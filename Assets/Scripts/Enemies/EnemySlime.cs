using UnityEngine;

// Inimigo básico do Solengard: persegue o player e causa dano por contato.
// Herda toda a lógica de movimento, vida e dano por contato de EnemyBase.
public class EnemySlime : EnemyBase
{
    protected override void Awake()
    {
        maxHealth = 60f;
        moveSpeed = 1.5f;
        damage    = 10f;
        base.Awake(); // sets currentHealth = maxHealth with correct values
    }

    protected override void OnDie()
    {
        Debug.Log("Slime morreu!");
    }
}
