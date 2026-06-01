using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    public enum EnemyType { Small, Normal, Heavy, Boss }

    [Header("Ataque do Herói")]
    [SerializeField] GameObject attackAoEPrefab;

    [Header("Hits nos Inimigos")]
    [SerializeField] GameObject hitLightPrefab;
    [SerializeField] GameObject hitHeavyPrefab;
    [SerializeField] GameObject hitBossPrefab;

    [Header("Morte de Inimigos")]
    [SerializeField] GameObject deathSmallPrefab;
    [SerializeField] GameObject deathNormalPrefab;
    [SerializeField] GameObject deathHeavyPrefab;
    [SerializeField] GameObject deathBossPrefab;

    [Header("Coleta de XP")]
    [SerializeField] GameObject xpCollectPrefab;
    [SerializeField] GameObject crystalEnvPrefab;

    [Header("Level Up")]
    [SerializeField] GameObject levelUpCirclePrefab;
    [SerializeField] GameObject levelUpAuraPrefab;

    [Header("Aura do Herói")]
    [SerializeField] GameObject playerAuraPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        AutoLoadPrefabs();
    }

    void AutoLoadPrefabs()
    {
        Load(ref attackAoEPrefab,     "VFX/AoE slash orange");
        Load(ref hitLightPrefab,      "VFX/Electro hit");
        Load(ref hitHeavyPrefab,      "VFX/Stones hit");
        Load(ref hitBossPrefab,       "VFX/Explosion");
        Load(ref deathSmallPrefab,    "VFX/Sparks explode blue");
        Load(ref deathNormalPrefab,   "VFX/Explosion");
        Load(ref deathHeavyPrefab,    "VFX/Smoke AOE explosion");
        Load(ref deathBossPrefab,     "VFX/Red energy explosion");
        Load(ref xpCollectPrefab,     "VFX/Sparks explode blue");
        Load(ref crystalEnvPrefab,    "VFX/Crystal effect blue");
        Load(ref levelUpCirclePrefab, "VFX/Healing circle");
        Load(ref levelUpAuraPrefab,   "VFX/Star aura");
        Load(ref playerAuraPrefab,    "VFX/Buff");
    }

    static void Load(ref GameObject field, string path)
    {
        if (field != null) return;
        field = Resources.Load<GameObject>(path);
        if (field == null) Debug.LogWarning($"[VFX] Prefab não encontrado em Resources/{path}");
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    public void SpawnAttackAoE(Vector3 pos)
        => Spawn(attackAoEPrefab, pos, 0.5f);

    public void SpawnHit(Vector3 pos, EnemyType type = EnemyType.Normal)
    {
        var prefab = type == EnemyType.Boss  ? hitBossPrefab  :
                     type == EnemyType.Heavy ? hitHeavyPrefab : hitLightPrefab;
        Spawn(prefab, pos, 0.4f);
    }

    public void SpawnDeath(Vector3 pos, EnemyType type = EnemyType.Normal)
    {
        var prefab = type == EnemyType.Boss  ? deathBossPrefab  :
                     type == EnemyType.Heavy ? deathHeavyPrefab :
                     type == EnemyType.Small ? deathSmallPrefab : deathNormalPrefab;
        Spawn(prefab, pos, 1.5f);
    }

    public void SpawnXPCollect(Vector3 pos)
        => Spawn(xpCollectPrefab, pos, 0.5f);

    public void SpawnLevelUp(Vector3 pos)
    {
        Spawn(levelUpCirclePrefab, pos, 2f);
        Spawn(levelUpAuraPrefab,   pos, 2f);
    }

    void Spawn(GameObject prefab, Vector3 pos, float lifetime)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(go, lifetime);
    }
}
