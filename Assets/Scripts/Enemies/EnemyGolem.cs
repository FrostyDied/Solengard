using UnityEngine;

// Mini-boss. Lento e blindado. Ao morrer, spawna 2 Orcs.
// Orcs invocados não contam para o progresso da wave.
public class EnemyGolem : EnemyBase
{
    const string OrcTag = "EnemyOrc";

    protected override void Awake()
    {
        maxHealth = 200f;
        moveSpeed = 0.6f;
        damage    = 25f;
        base.Awake();
    }

    protected override void OnDie()
    {
        SpawnOrcs(2);
    }

    void SpawnOrcs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = (Vector3)(Random.insideUnitCircle * 1.5f);
            GameObject orc = ObjectPoolManager.Instance?.GetFromPool(OrcTag);
            if (orc == null) continue;

            orc.transform.position = transform.position + offset;
            var eb = orc.GetComponent<EnemyBase>();
            if (eb != null) { eb.poolTag = OrcTag; eb.OnDeathCallback = null; }
        }
    }
}
