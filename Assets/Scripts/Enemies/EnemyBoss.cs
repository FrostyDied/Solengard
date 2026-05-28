using System.Collections;
using UnityEngine;

// Boss final. Duas fases: Fase 2 (<50% HP) aumenta velocidade e spawna Zumbis.
// Ao morrer concede 50 diamantes ao player.
public class EnemyBoss : EnemyBase
{
    public static event System.Action OnBossDied;

    [Header("Boss")]
    public float phase2SpeedMultiplier = 1.8f;
    public float zumbiSpawnInterval    = 3f;

    const string ZumbiTag = "EnemyZumbi";

    bool phase2Active;
    bool phase2Triggered;

    protected override void Awake()
    {
        maxHealth = 1000f;
        moveSpeed = 0.6f;
        damage    = 50f;
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        phase2Active    = false;
        phase2Triggered = false;
    }

    protected override void FixedUpdate()
    {
        CheckPhaseTransition();
        base.FixedUpdate();
    }

    void CheckPhaseTransition()
    {
        if (phase2Triggered) return;
        if (currentHealth <= maxHealth * 0.5f)
        {
            phase2Triggered = true;
            phase2Active    = true;
            moveSpeed      *= phase2SpeedMultiplier;
            StartCoroutine(Phase2ZumbiRoutine());
        }
    }

    IEnumerator Phase2ZumbiRoutine()
    {
        while (phase2Active)
        {
            yield return new WaitForSeconds(zumbiSpawnInterval);
            if (phase2Active) SpawnZumbi();
        }
    }

    void SpawnZumbi()
    {
        Vector3 offset = (Vector3)(Random.insideUnitCircle * 2f);
        GameObject zumbi = ObjectPoolManager.Instance?.GetFromPool(ZumbiTag);
        if (zumbi == null) return;

        zumbi.transform.position = transform.position + offset;
        var eb = zumbi.GetComponent<EnemyBase>();
        if (eb != null) { eb.poolTag = ZumbiTag; eb.OnDeathCallback = null; }
    }

    protected override void NotifyDeathCause()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.currentRunData.causeOfDeath   = "Boss";
        GameManager.Instance.currentRunData.lastDeathCause = DeathCause.Boss;
    }

    protected override void OnDie()
    {
        phase2Active = false;
        OnBossDied?.Invoke();
        DiamondSystem.Instance?.AddDiamonds(50);
    }
}
