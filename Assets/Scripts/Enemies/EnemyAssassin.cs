using System.Collections;
using UnityEngine;

// Assassino. Alta velocidade, teleporta perto do player e fica invulnerável brevemente.
public class EnemyAssassin : EnemyBase
{
    [Header("Assassin")]
    public float teleportInterval    = 5f;
    public float postTeleportIFrames = 0.5f;

    bool isPostTeleportInvincible;

    protected override void Awake()
    {
        maxHealth         = 20f;
        moveSpeed         = 4f;
        damage            = 12f;
        separationStrength = 0f;   // ignora multidão — sempre carrega direto no player
        separationRadius  = 0f;
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isPostTeleportInvincible = false;
        StartCoroutine(TeleportRoutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        isPostTeleportInvincible = false;
        StopAllCoroutines();
    }

    protected override bool CanTakeDamage() => !isPostTeleportInvincible;

    IEnumerator TeleportRoutine()
    {
        yield return new WaitForSeconds(teleportInterval * 0.5f);
        while (true)
        {
            yield return new WaitForSeconds(teleportInterval);
            if (playerTransform != null)
                yield return StartCoroutine(Teleport());
        }
    }

    IEnumerator Teleport()
    {
        Vector2 offset = Random.insideUnitCircle.normalized * 1.5f;
        transform.position = (Vector2)playerTransform.position + offset;

        isPostTeleportInvincible = true;
        yield return new WaitForSeconds(postTeleportIFrames);
        isPostTeleportInvincible = false;
    }
}
