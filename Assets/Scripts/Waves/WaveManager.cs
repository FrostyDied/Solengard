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

    [Header("Spawn contínuo")]
    [SerializeField] float spawnInterval        = 0.5f;
    [SerializeField] float minimumWaveDuration  = 60f;

    [Header("Sistemas")]
    [SerializeField] WaveTimerSystem         waveTimerSystem;
    [SerializeField] DynamicDifficultySystem dynamicDifficulty;

    // ── Valores lidos do GameConfig (com fallback nos padrões) ──────────────────

    int   TotalWavesConfig    => gameConfig != null ? gameConfig.totalWaves               : totalWaves;
    float RawTimeBetweenWaves => gameConfig != null ? gameConfig.intervaloEntreWaves       : 8f;
    int   BaseEnemyCount      => gameConfig != null ? gameConfig.inimigosBaseWave1         : 50;
    int   EnemyCountIncrement => gameConfig != null ? gameConfig.incrementoInimigosPorWave : 10;

    public float TimeBetweenWaves => RawTimeBetweenWaves;

    // ── Estado interno ──────────────────────────────────────────────────────────

    int   currentWave  = 0;
    int   enemiesAlive = 0;  // inimigos atualmente ativos na tela
    int   killCount    = 0;  // kills realizados nesta wave
    int   killQuota    = 0;  // kills necessários para concluir a wave
    bool  isSpawning   = false;
    float waveStartTime;

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
        killQuota     = EnemyCountForWave(currentWave);
        killCount     = 0;
        enemiesAlive  = 0;
        waveStartTime = Time.time;

        Debug.Log($"[WaveManager] Wave {currentWave}/{TotalWavesConfig} — quota: {killQuota} kills");

        waveTimerSystem?.StartTimer();
        StartCoroutine(SpawnLoop());
    }

    // Chamado pelo OnDeathCallback de cada inimigo
    public void OnEnemyDied()
    {
        enemiesAlive--;
        killCount++;

        if (killCount >= killQuota && Time.time - waveStartTime >= minimumWaveDuration)
            EndWave();
    }

    // ── Lógica interna ──────────────────────────────────────────────────────────

    // Spawna continuamente enquanto a wave está ativa,
    // respeitando o limite de inimigos na tela.
    IEnumerator SpawnLoop()
    {
        isSpawning = true;

        while (isSpawning)
        {
            int maxOnScreen = dynamicDifficulty != null ? dynamicDifficulty.MaxEnemiesOnScreen : 20;

            if (enemiesAlive < maxOnScreen)
            {
                SpawnEnemy();
                enemiesAlive++;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
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

        enemy.maxHealth *= mod;
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
        isSpawning = false; // para o SpawnLoop

        Debug.Log($"[WaveManager] Wave {currentWave} concluída ({killCount} kills). Inimigos restantes na tela NÃO são destruídos.");
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
            if (currentWave >= TotalWavesConfig)       return WaveType.Boss;
            if (currentWave == 5 || currentWave == 8)  return WaveType.Elite;
            return WaveType.Normal;
        }
    }

    public int CurrentWave  => currentWave;
    public int TotalWaves   => TotalWavesConfig;
    public int EnemiesAlive => enemiesAlive;
    public int KillCount    => killCount;
    public int KillQuota    => killQuota;
}
