using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType { Normal, Elite, Boss }

public class WaveManager : MonoBehaviour
{
    public static event System.Action<int> OnWaveCompleted;
    public static event System.Action      OnAllWavesCompleted;

    [Header("Configuração de Waves")]
    public int totalWaves = 10;

    [Header("GameConfig (opcional — sobrescreve os valores acima se atribuído)")]
    [SerializeField] GameConfig gameConfig;

    [Header("Inimigos")]
    public List<GameObject> enemyPrefabs = new();

    [Header("Pontos de Spawn")]
    public List<Transform> spawnPoints = new();

    [Header("Sistemas")]
    [SerializeField] WaveTimerSystem        waveTimerSystem;
    [SerializeField] DynamicDifficultySystem dynamicDifficulty;

    // ── Valores lidos do GameConfig (com fallback nos padrões) ──────────────────

    int   TotalWavesConfig    => gameConfig != null ? gameConfig.totalWaves               : totalWaves;
    float RawTimeBetweenWaves => gameConfig != null ? gameConfig.intervaloEntreWaves       : 8f;
    int   BaseEnemyCount      => gameConfig != null ? gameConfig.inimigosBaseWave1         : 20;
    int   EnemyCountIncrement => gameConfig != null ? gameConfig.incrementoInimigosPorWave : 10;

    public float TimeBetweenWaves => RawTimeBetweenWaves;

    // ── Estado interno ──────────────────────────────────────────────────────────

    int  currentWave  = 0;
    int  enemiesAlive = 0;
    bool isSpawning   = false;

    static readonly Vector2[] fallbackSpawnPositions =
    {
        new Vector2(-5f,  5f),
        new Vector2( 5f,  5f),
        new Vector2( 5f, -5f),
        new Vector2(-5f, -5f),
    };

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Start() { }

    // ── API pública ─────────────────────────────────────────────────────────────

    public void StartWave()
    {
        if (isSpawning) return;

        currentWave++;
        int count = EnemyCountForWave(currentWave);

        // Limita pelo maxEnemiesOnScreen do tier atual
        if (dynamicDifficulty != null)
            count = Mathf.Min(count, dynamicDifficulty.MaxEnemiesOnScreen);

        enemiesAlive = count;

        Debug.Log($"[WaveManager] Wave {currentWave}/{TotalWavesConfig} iniciada — {count} inimigos");

        waveTimerSystem?.StartTimer();
        StartCoroutine(SpawnWave(count));
    }

    public void OnEnemyDied()
    {
        enemiesAlive--;
        if (enemiesAlive <= 0)
            EndWave();
    }

    // ── Lógica interna ──────────────────────────────────────────────────────────

    IEnumerator SpawnWave(int count)
    {
        isSpawning = true;

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(0.4f);
        }

        isSpawning = false;
    }

    void SpawnEnemy()
    {
        var available = GetFilteredPrefabs();
        if (available.Count == 0)
        {
            Debug.LogError("[WaveManager] Nenhum prefab de inimigo configurado.");
            return;
        }

        GameObject prefab  = available[Random.Range(0, available.Count)];
        Vector3    posicao = ObterPosicaoDeSpawn();

        GameObject enemy = ObjectPoolManager.Instance?.GetFromPool(prefab.name);
        if (enemy != null)
            enemy.transform.position = posicao;
        else
            enemy = Instantiate(prefab, posicao, Quaternion.identity);

        EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            enemyBase.OnDeathCallback = OnEnemyDied;
            enemyBase.poolTag         = prefab.name;
            ApplyAdaptiveHealthModifier(enemyBase);
            dynamicDifficulty?.ApplyToEnemy(enemyBase);
        }
        else
            Debug.LogWarning($"[WaveManager] Prefab '{prefab.name}' não possui EnemyBase.");
    }

    // Filtra enemyPrefabs pelos tipos disponíveis no tier atual do DynamicDifficultySystem
    List<GameObject> GetFilteredPrefabs()
    {
        if (dynamicDifficulty == null) return enemyPrefabs;

        string[] available = dynamicDifficulty.GetAvailableEnemyTypes();
        if (available == null || available.Length == 0) return enemyPrefabs;

        var filtered = new List<GameObject>();
        foreach (var prefab in enemyPrefabs)
            if (System.Array.IndexOf(available, prefab.name) >= 0)
                filtered.Add(prefab);

        return filtered.Count > 0 ? filtered : enemyPrefabs;
    }

    void ApplyAdaptiveHealthModifier(EnemyBase enemy)
    {
        if (DifficultyAdaptiveSystem.Instance == null) return;
        float mod = DifficultyAdaptiveSystem.Instance.EnemyHealthModifier;
        if (Mathf.Approximately(mod, 1f)) return;

        enemy.maxHealth = enemy.maxHealth * mod;
        enemy.InitializeHealth();
    }

    Vector3 ObterPosicaoDeSpawn()
    {
        if (spawnPoints.Count > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Count)].position;

        Debug.LogWarning("[WaveManager] SpawnPoints não configurados — usando posições de fallback para testes.");
        return fallbackSpawnPositions[Random.Range(0, fallbackSpawnPositions.Length)];
    }

    void EndWave()
    {
        Debug.Log($"[WaveManager] Wave {currentWave} concluída.");
        waveTimerSystem?.StopTimer();
        OnWaveCompleted?.Invoke(currentWave);

        if (currentWave >= TotalWavesConfig)
        {
            Debug.Log("[WaveManager] Todas as waves concluídas!");
            OnAllWavesCompleted?.Invoke();
            return;
        }

        StartCoroutine(NextWaveCountdown());
    }

    IEnumerator NextWaveCountdown()
    {
        Debug.Log($"[WaveManager] Próxima wave em {RawTimeBetweenWaves}s...");
        yield return new WaitForSeconds(RawTimeBetweenWaves);
        StartWave();
    }

    int EnemyCountForWave(int wave)
    {
        int count     = BaseEnemyCount + EnemyCountIncrement * (wave - 1);
        int reduction = DifficultyAdaptiveSystem.Instance?.EnemyCountReduction ?? 0;
        return Mathf.Max(1, count - reduction);
    }

    // ── Propriedades de leitura ─────────────────────────────────────────────────

    public WaveType CurrentWaveType
    {
        get
        {
            if (currentWave >= TotalWavesConfig)        return WaveType.Boss;
            if (currentWave == 5 || currentWave == 8)   return WaveType.Elite;
            return WaveType.Normal;
        }
    }

    public int CurrentWave  => currentWave;
    public int TotalWaves   => TotalWavesConfig;
    public int EnemiesAlive => enemiesAlive;
}
