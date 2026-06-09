using UnityEngine;

// Inimigo básico do Solengard: persegue o player e causa dano por contato.
// Herda toda a lógica de movimento, vida e dano por contato de EnemyBase.
public class EnemySlime : EnemyBase
{
    protected override void Awake()
    {
        maxHealth = 20f;
        moveSpeed = 1.0f;
        damage    = 4f;
        base.Awake(); // sets currentHealth = maxHealth with correct values
        transform.localScale = new Vector3(1.6f, 1.6f, 1f);
    }

    protected override void OnDie()
    {
        Debug.Log("Slime morreu!");
    }
}
