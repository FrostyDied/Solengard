using UnityEngine;

public class EnemyAssassin : EnemyBase
{
    [Header("Dash")]
    [SerializeField] float dashRange    = 4f;
    [SerializeField] float dashSpeed    = 14f;
    [SerializeField] float dashDuration = 0.2f;
    [SerializeField] float dashCooldown = 2f;

    float _dashTimer    = 0f;
    float _dashDuration = 0f;
    bool  _dashing      = false;

    protected override void Awake()
    {
        maxHealth        = 20f;
        moveSpeed        = 3.5f;
        contactDamage    = 12f;
        stoppingDistance = 0.2f;
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

        if (_dashing)
        {
            _dashDuration -= Time.fixedDeltaTime;
            rb.linearVelocity = dir * dashSpeed;
            if (_dashDuration <= 0f) _dashing = false;
            return;
        }

        _dashTimer -= Time.fixedDeltaTime;

        if (dist <= dashRange && _dashTimer <= 0f)
        {
            _dashing          = true;
            _dashDuration     = dashDuration;
            _dashTimer        = dashCooldown;
            rb.linearVelocity = dir * dashSpeed;
            return;
        }

        if (dist <= stoppingDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = dir * moveSpeed;
    }
}
