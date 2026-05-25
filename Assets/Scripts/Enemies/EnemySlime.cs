using UnityEngine;

// Inimigo básico do Solengard: persegue o player e causa dano por contato.
// Herda toda a lógica de movimento e vida de EnemyBase.
public class EnemySlime : EnemyBase
{
    // Timer que controla o intervalo entre cada aplicação de dano por contato
    float contactDamageTimer;

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Start()
    {
        // Valores padrão do Slime — sobrescrevem os campos públicos de EnemyBase
        maxHealth  = 50f;
        moveSpeed  = 1.5f;
        damage     = 10f;

        // Ressincroniza a vida atual, pois Awake() (base) rodou antes deste Start()
        currentHealth = maxHealth;
    }

    // ── Colisão com o player ────────────────────────────────────────────────────

    // Chamado todo FixedUpdate enquanto o Slime permanecer em contato com outro collider.
    // O timer garante que o dano seja aplicado no máximo uma vez por segundo.
    void OnCollisionStay2D(Collision2D collision)
    {
        contactDamageTimer -= Time.fixedDeltaTime;

        if (contactDamageTimer > 0f) return;

        PlayerHealth playerHealth = collision.collider.GetComponent<PlayerHealth>();

        if (playerHealth == null) return;

        playerHealth.TakeDamage(damage);

        // Reinicia o intervalo após cada golpe
        contactDamageTimer = 1f;
    }

    // Zera o timer ao sair do contato para que o próximo toque cause dano imediatamente
    void OnCollisionExit2D(Collision2D collision)
    {
        contactDamageTimer = 0f;
    }

    // ── Morte ───────────────────────────────────────────────────────────────────

    // Sobrescreve EnemyBase.OnDie() — futuramente spawna partículas/loot
    protected override void OnDie()
    {
        Debug.Log("Slime morreu!");
    }
}
