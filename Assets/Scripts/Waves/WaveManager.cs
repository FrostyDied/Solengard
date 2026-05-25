using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Gerencia todas as ondas de inimigos de uma fase do Solengard.
// Attach este componente em um GameObject persistente na cena de jogo.
public class WaveManager : MonoBehaviour
{
    // Disparado quando todas as waves da fase forem concluídas
    public static event System.Action OnAllWavesCompleted;

    [Header("Configuração de Waves")]
    public int totalWaves = 5;
    public float timeBetweenWaves = 5f;

    [Header("Inimigos")]
    // Prefabs possíveis de inimigos — o spawn escolhe aleatoriamente desta lista
    public List<GameObject> enemyPrefabs = new();

    // Quantidade de inimigos na primeira wave; cresce a cada wave
    public int baseEnemyCount = 5;

    // Quantos inimigos a mais por wave (ex.: wave 2 = base + increment * 1)
    public int enemyCountIncrement = 3;

    [Header("Pontos de Spawn")]
    // Transforms posicionados ao redor da arena que definem onde os inimigos aparecem
    public List<Transform> spawnPoints = new();

    // ── Estado interno ──────────────────────────────────────────────────────────

    int currentWave = 0;
    int enemiesAlive = 0;
    bool isSpawning = false;

    // Posições fixas usadas como fallback quando spawnPoints não está configurado (modo de teste)
    static readonly Vector2[] fallbackSpawnPositions =
    {
        new Vector2(-5f,  5f),
        new Vector2( 5f,  5f),
        new Vector2( 5f, -5f),
        new Vector2(-5f, -5f),
    };

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Start()
    {
        StartWave();
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    // Inicia a próxima wave; chamado automaticamente no Start e após cada EndWave
    public void StartWave()
    {
        if (isSpawning) return;

        currentWave++;
        int count = EnemyCountForWave(currentWave);
        enemiesAlive = count;

        Debug.Log($"[WaveManager] Wave {currentWave}/{totalWaves} iniciada — {count} inimigos");

        StartCoroutine(SpawnWave(count));
    }

    // Chamado pelo inimigo (ou pelo EnemyBase) ao morrer
    public void OnEnemyDied()
    {
        enemiesAlive--;

        if (enemiesAlive <= 0)
            EndWave();
    }

    // ── Lógica interna ──────────────────────────────────────────────────────────

    // Spawna os inimigos da wave com um pequeno intervalo entre cada um
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

    // Instancia um inimigo aleatório em uma posição de spawn
    void SpawnEnemy()
    {
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogError("[WaveManager] Nenhum prefab de inimigo configurado. Adicione prefabs à lista 'Enemy Prefabs' no Inspector.");
            return;
        }

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Vector3 posicao    = ObterPosicaoDeSpawn();

        GameObject enemy = Instantiate(prefab, posicao, Quaternion.identity);

        // Registra o callback de morte no inimigo recém-criado
        EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
            enemyBase.OnDeathCallback = OnEnemyDied;
        else
            Debug.LogWarning($"[WaveManager] Prefab '{prefab.name}' não possui EnemyBase.");
    }

    // Retorna posição de um spawnPoint configurado ou, em fallback, um dos 4 cantos fixos da arena
    Vector3 ObterPosicaoDeSpawn()
    {
        if (spawnPoints.Count > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Count)].position;

        // Avisa uma única vez por spawn — sem spam, pois o log aparece a cada inimigo
        Debug.LogWarning("[WaveManager] SpawnPoints não configurados — usando posições de fallback para testes.");
        return fallbackSpawnPositions[Random.Range(0, fallbackSpawnPositions.Length)];
    }

    // Encerra a wave atual e decide o que vem a seguir
    void EndWave()
    {
        Debug.Log($"[WaveManager] Wave {currentWave} concluída.");

        if (currentWave >= totalWaves)
        {
            Debug.Log("[WaveManager] Todas as waves concluídas!");
            OnAllWavesCompleted?.Invoke();
            return;
        }

        // Aguarda o intervalo configurado antes de iniciar a próxima wave
        StartCoroutine(NextWaveCountdown());
    }

    IEnumerator NextWaveCountdown()
    {
        Debug.Log($"[WaveManager] Próxima wave em {timeBetweenWaves}s...");
        yield return new WaitForSeconds(timeBetweenWaves);
        StartWave();
    }

    // Fórmula de escalonamento: cada wave adiciona enemyCountIncrement inimigos
    int EnemyCountForWave(int wave)
    {
        return baseEnemyCount + enemyCountIncrement * (wave - 1);
    }

    // ── Propriedades de leitura (úteis para a UI) ───────────────────────────────

    public int CurrentWave => currentWave;
    public int TotalWaves => totalWaves;
    public int EnemiesAlive => enemiesAlive;
}
