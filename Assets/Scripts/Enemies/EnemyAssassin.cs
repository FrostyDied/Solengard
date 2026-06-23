using UnityEngine;

public class EnemyAssassin : EnemyBase
{
    [Header("Dash")]
    [SerializeField] float dashRange    = 4f;
    [SerializeField] float dashSpeed    = 12f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 2.5f;

    [Header("Histerese")]
    [SerializeField] float holdBand = 0.4f;

    float   _dashTimer    = 0f;
    float   _dashDuration = 0f;
    bool    _dashing      = false;
    Vector2 _dashDir;

    protected override void Awake()
    {
        maxHealth          = 20f;
        moveSpeed          = 2.2f;
        contactDamage      = 12f;
        stoppingDistance   = 1.2f;
        dashSpeed          = 8f;
        separationRadius   = 1.0f;
        separationStrength = 0.8f;
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

        // FASE 0: afastamento de emergência — nunca dentro de 1u do player
        if (dist < 1.0f)
        {
            rb.linearVelocity = -dir * moveSpeed * 2f;
            _dashing          = false;
            _dashTimer        = dashCooldown;
            return;
        }

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

        // ZONA MORTA: histerese — previne flip-flop entre recuo e perseguição
        if (dist > stoppingDistance && dist <= stoppingDistance + holdBand)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // FASE 3: muito próximo → recuar com separação (blend 70/30 garante que recuo nunca é cancelado)
        if (dist <= stoppingDistance)
        {
            Vector2 sep3       = ComputeSeparation();
            Vector2 retreatDir = -dir;
            if (sep3.sqrMagnitude > 0.001f)
            {
                Vector2 candidate = (-dir + sep3.normalized * separationStrength).normalized;
                retreatDir = Vector2.Dot(candidate, -dir) >= 0.1f
                    ? candidate
                    : (-dir * 0.7f + sep3.normalized * 0.3f).normalized;
            }
            rb.linearVelocity = retreatDir * moveSpeed;
            return;
        }

        // FASE 4: perseguição normal com separação (mesmo blend conservador)
        Vector2 sep4    = ComputeSeparation();
        Vector2 desired = dir;
        if (sep4.sqrMagnitude > 0.001f)
        {
            Vector2 candidate4 = (dir + sep4.normalized * separationStrength).normalized;
            desired = Vector2.Dot(candidate4, dir) >= 0.1f
                ? candidate4
                : (dir * 0.7f + sep4.normalized * 0.3f).normalized;
        }
        rb.linearVelocity = desired * moveSpeed;
    }
}
