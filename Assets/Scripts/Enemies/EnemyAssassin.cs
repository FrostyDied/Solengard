using UnityEngine;

public class EnemyAssassin : EnemyBase
{
    [Header("Dash")]
    [SerializeField] float dashRange    = 4f;
    [SerializeField] float dashSpeed    = 12f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 2.5f;

    float   _dashTimer    = 0f;
    float   _dashDuration = 0f;
    bool    _dashing      = false;
    Vector2 _dashDir;

    protected override void Awake()
    {
        maxHealth        = 20f;
        moveSpeed        = 3.5f;
        contactDamage    = 12f;
        stoppingDistance = 1.2f;
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _dashTimer    = 0f;
        _dashDuration = 0f;
        _dashing      = false;
    }

    protected override void MoveTowardsPlayer()
    {
        if (playerTransform == null) { FindPlayer(); return; }

        float   dist = Vector2.Distance(rb.position, (Vector2)playerTransform.position);
        Vector2 dir  = ((Vector2)playerTransform.position - rb.position).normalized;

        // FASE 1: em dash — direção FIXA, não guiada
        if (_dashing)
        {
            _dashDuration -= Time.fixedDeltaTime;
            rb.linearVelocity = _dashDir * dashSpeed;
            if (_dashDuration <= 0f)
            {
                _dashing = false;
                rb.linearVelocity = -_dashDir * dashSpeed * 0.5f;
            }
            return;
        }

        _dashTimer -= Time.fixedDeltaTime;

        // FASE 2: no range e cooldown zerado → iniciar dash com direção fixada
        if (dist <= dashRange && dist > stoppingDistance && _dashTimer <= 0f)
        {
            _dashing      = true;
            _dashDuration = dashDuration;
            _dashTimer    = dashCooldown;
            _dashDir      = dir;
            rb.linearVelocity = _dashDir * dashSpeed;
            return;
        }

        // FASE 3: muito próximo → recuar
        if (dist <= stoppingDistance)
        {
            rb.linearVelocity = -dir * moveSpeed;
            return;
        }

        // FASE 4: perseguição normal
        rb.linearVelocity = dir * moveSpeed;
    }
}
