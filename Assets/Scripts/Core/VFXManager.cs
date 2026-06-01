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
#if UNITY_EDITOR
        const string base_ = "Assets/Hovl Studio/Magic effects pack/Prefabs/";
        Load(ref attackAoEPrefab,     base_ + "AoE effects/AoE slash orange.prefab");
        Load(ref hitLightPrefab,      base_ + "Hits and explosions/Electro hit.prefab");
        Load(ref hitHeavyPrefab,      base_ + "Hits and explosions/Stones hit.prefab");
        Load(ref hitBossPrefab,       base_ + "Hits and explosions/Explosion.prefab");
        Load(ref deathSmallPrefab,    base_ + "Sparks/Sparks explode blue.prefab");
        Load(ref deathNormalPrefab,   base_ + "Hits and explosions/Explosion.prefab");
        Load(ref deathHeavyPrefab,    base_ + "AoE effects/Smoke AOE explosion.prefab");
        Load(ref deathBossPrefab,     base_ + "AoE effects/Red energy explosion.prefab");
        Load(ref xpCollectPrefab,     base_ + "Sparks/Sparks explode blue.prefab");
        Load(ref crystalEnvPrefab,    base_ + "Environment/Crystal effect blue.prefab");
        Load(ref levelUpCirclePrefab, base_ + "Magic circles/Healing circle.prefab");
        Load(ref levelUpAuraPrefab,   base_ + "Character auras/Star aura.prefab");
        Load(ref playerAuraPrefab,    base_ + "Character auras/Buff.prefab");
#endif
    }

    static void Load(ref GameObject field, string path)
    {
#if UNITY_EDITOR
        if (field != null) return;
        field = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (field == null) Debug.LogWarning($"[VFX] Prefab não encontrado: {path}");
#endif
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
