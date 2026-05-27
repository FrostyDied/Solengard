using System.Collections;
using UnityEngine;

// Invocador. Convoca Zumbis periodicamente e ao morrer.
// Zumbis invocados não contam para o progresso da wave.
public class EnemyMage : EnemyBase
{
    [Header("Mage")]
    public float summonInterval = 8f;

    const string ZumbiTag = "EnemyZumbi";

    protected override void Awake()
    {
        maxHealth = 60f;
        moveSpeed = 1.2f;
        damage    = 8f;
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(SummonRoutine());
    }

    IEnumerator SummonRoutine()
    {
        yield return new WaitForSeconds(summonInterval * 0.5f);
        while (true)
        {
            SummonZumbis(2);
            yield return new WaitForSeconds(summonInterval);
        }
    }

    protected override void OnDie()
    {
        SummonZumbis(1);
    }

    void SummonZumbis(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = (Vector3)(Random.insideUnitCircle * 1.5f);
            GameObject zumbi = ObjectPoolManager.Instance?.GetFromPool(ZumbiTag);
            if (zumbi == null) continue;

            zumbi.transform.position = transform.position + offset;
            var eb = zumbi.GetComponent<EnemyBase>();
            if (eb != null) { eb.poolTag = ZumbiTag; eb.OnDeathCallback = null; }
        }
    }
}
