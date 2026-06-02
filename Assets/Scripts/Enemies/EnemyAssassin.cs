using UnityEngine;

// Assassino. Alta velocidade, dash veloz ao entrar no range de ataque.
public class EnemyAssassin : EnemyBase
{
    [Header("Assassin")]
    [SerializeField] float dashRange    = 3.0f;
    [SerializeField] float dashSpeed    = 12f;
    [SerializeField] float dashCooldown = 2.5f;

    float _dashTimer = 0f;

    protected override void Awake()
    {
        maxHealth          = 20f;
        moveSpeed          = 4f;
        damage             = 12f;
        separationStrength = 0.3f;  // deriva lateral suficiente para cruzar a fronteira de dano
        separationRadius   = 1.0f;  // afasta de outros Assassins próximos
        stoppingDistance   = 0.3f;  // penetra mais fundo antes de parar
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _dashTimer = 0f;
    }

    protected override void MoveTowardsPlayer()
    {
        if (playerTransform == null) { FindPlayer(); return; }

        float   dist     = Vector2.Distance(rb.position, (Vector2)playerTransform.position);
        Vector2 toPlayer = ((Vector2)playerTransform.position - rb.position).normalized;

        _dashTimer -= Time.fixedDeltaTime;

        // Dash: dispara quando dentro do range e cooldown zerado — atravessa a fronteira de dano
        if (dist <= dashRange && _dashTimer <= 0f)
        {
            rb.linearVelocity = toPlayer * dashSpeed;
            _dashTimer = dashCooldown;
            return;
        }

        if (dist <= stoppingDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 separation = ComputeSeparation();
        float   speed      = dist < stoppingDistance * 3f
                             ? moveSpeed * (dist / (stoppingDistance * 3f))
                             : moveSpeed;

        rb.linearVelocity = (toPlayer + separation * separationStrength).normalized * speed;
    }
}
