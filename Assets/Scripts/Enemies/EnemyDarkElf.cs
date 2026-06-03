using UnityEngine;

public class EnemyDarkElf : EnemyAssassin
{
    public bool isBoss = false;

    protected override void Awake()
    {
        base.Awake();
        maxHealth        = 35f;
        moveSpeed        = 2.5f;
        contactDamage    = 15f;
        stoppingDistance = 1.2f;
    }

    protected override void MoveTowardsPlayer()
    {
        if (!isBoss) { base.MoveTowardsPlayer(); return; }

        // Boss mode: movimento direto sem dash
        if (playerTransform == null) { FindPlayer(); return; }

        float   dist = Vector2.Distance(rb.position, (Vector2)playerTransform.position);
        Vector2 dir  = ((Vector2)playerTransform.position - rb.position).normalized;

        if (dist < 1.0f)
        {
            rb.linearVelocity = -dir * moveSpeed * 2f;
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
