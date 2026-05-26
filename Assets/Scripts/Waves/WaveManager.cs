using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Gerencia todas as ondas de inimigos de uma fase do Solengard.
// Attach este componente em um GameObject persistente na cena de jogo.
public class WaveManager : MonoBehaviour
{
    // Disparado ao fim de cada wave individual (passa o número da wave concluída)
    public static event System.Action<int> OnWaveCompleted;

    // Disparado quando todas as waves da fase forem concluídas
    public static event System.Action OnAllWavesCompleted;

    [Header("Configuração de Waves")]
    public int totalWaves = 5;  // fallback quando GameConfig não está atribuído

    [Header("GameConfig (opcional — sobrescreve os valores acima se atribuído)")]
    [SerializeField] GameConfig gameConfig;

    [Header("Inimigos")]
    // Prefabs possíveis de inimigos — o spawn escolhe aleatoriamente desta lista
    public List<GameObject> enemyPrefabs = new();

    [Header("Pontos de Spawn")]
    // Transforms posicionados ao redor da arena que definem onde os inimigos aparecem
    public List<Transform> spawnPoints = new();

    // ── Valores lidos do GameConfig (com fallback nos padrões) ──────────────────

    int   TotalWavesConfig    => gameConfig != null ? gameConfig.totalWaves               : totalWaves;
    float TimeBetweenWaves    => gameConfig != null ? gameConfig.intervaloEntreWaves       : 5f;
    int   BaseEnemyCount      => gameConfig != null ? gameConfig.inimigosBaseWave1         : 5;
    int   EnemyCountIncrement => gameConfig != null ? gameConfig.incrementoInimigosPorWave : 3;

    // ── Estado interno ──────────────────────────────────────────────────────────

    int  currentWave  = 0;
    int  enemiesAlive = 0;
    bool isSpawning   = false;

    // Posições fixas usadas como fallback quando spawnPoints não está configurado (modo de teste)
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

    // Inicia a próxima wave; chamado pelo GameManager após o jogador iniciar a partida
    public void StartWave()
    {
        if (isSpawning) return;

        currentWave++;
        int count = EnemyCountForWave(currentWave);
        enemiesAlive = count;

        Debug.Log($"[WaveManager] Wave {currentWave}/{TotalWavesConfig} iniciada — {count} inimigos");

        StartCoroutine(SpawnWave(count));
    }

    // Chamado pelo EnemyBase ao morrer (via OnDeathCallback)
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
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogError("[WaveManager] Nenhum prefab de inimigo configurado. Adicione prefabs à lista 'Enemy Prefabs' no Inspector.");
            return;
        }

        GameObject prefab  = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Vector3    posicao = ObterPosicaoDeSpawn();

        // Tenta reutilizar da pool; instancia normalmente se a pool não estiver configurada
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
        }
        else
            Debug.LogWarning($"[WaveManager] Prefab '{prefab.name}' não possui EnemyBase.");
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
        Debug.Log($"[WaveManager] Próxima wave em {TimeBetweenWaves}s...");
        yield return new WaitForSeconds(TimeBetweenWaves);
        StartWave();
    }

    int EnemyCountForWave(int wave)
    {
        return BaseEnemyCount + EnemyCountIncrement * (wave - 1);
    }

    // ── Propriedades de leitura (úteis para a UI) ───────────────────────────────

    public int CurrentWave  => currentWave;
    public int TotalWaves   => TotalWavesConfig;
    public int EnemiesAlive => enemiesAlive;
}
