using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType { Normal, Elite, Boss }

public class WaveManager : MonoBehaviour
{
    public static event System.Action<int> OnWaveCompleted;
    public static event System.Action      OnAllWavesCompleted;

    [System.Serializable]
    public class SpawnPhase
    {
        public float  startTime;
        public string enemyType;
        public float  spawnInterval;
        public int    maxOnScreen;
        public float  healthMult;
        public float  speedMult;
    }

    [Header("Inimigos")]
    public List<GameObject> enemyPrefabs = new();

    [Header("Sistemas")]
    [SerializeField] WaveTimerSystem         waveTimerSystem;
    [SerializeField] DynamicDifficultySystem dynamicDifficulty;

    // Mantido para compatibilidade com SolengardSetup (não usado no spawn contínuo)
    [Header("GameConfig (legado)")]
    [SerializeField] GameConfig gameConfig;

    static readonly SpawnPhase[] Phases = new SpawnPhase[]
    {
        new SpawnPhase { startTime=0,   enemyType="Zombie",   spawnInterval=0.6f, maxOnScreen=15, healthMult=1.0f, speedMult=1.0f },
        new SpawnPhase { startTime=30,  enemyType="Slime",    spawnInterval=0.5f, maxOnScreen=25, healthMult=1.0f, speedMult=1.0f },
        new SpawnPhase { startTime=60,  enemyType="Archer",   spawnInterval=0.5f, maxOnScreen=30, healthMult=1.2f, speedMult=1.1f },
        new SpawnPhase { startTime=90,  enemyType="Orc",      spawnInterval=0.8f, maxOnScreen=35, healthMult=1.3f, speedMult=1.1f },
        new SpawnPhase { startTime=120, enemyType="Mage",     spawnInterval=0.7f, maxOnScreen=40, healthMult=1.5f, speedMult=1.2f },
        new SpawnPhase { startTime=180, enemyType="Assassin", spawnInterval=0.4f, maxOnScreen=50, healthMult=1.5f, speedMult=1.3f },
        new SpawnPhase { startTime=240, enemyType="All",      spawnInterval=0.3f, maxOnScreen=60, healthMult=2.0f, speedMult=1.4f },
    };

    // ── Estado interno ──────────────────────────────────────────────────────────

    int   currentWave  = 0;
    int   enemiesAlive = 0;
    int   killCount    = 0;
    bool  isSpawning   = false;
    float _gameTime    = 0f;

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Start() { }

    // ── API pública ─────────────────────────────────────────────────────────────

    public void StartWave()
    {
        StopAllCoroutines();
        StartWavesAt(0f);
    }

    public void RestoreToWave(int wave)
    {
        isSpawning = false;
        StopAllCoroutines();
        int   phaseIdx   = Mathf.Clamp(wave - 1, 0, Phases.Length - 1);
        float restoreAt  = Phases[phaseIdx].startTime;
        Debug.Log($"[WaveManager] RestoreToWave({wave}) → gameTime ≈ {restoreAt}s");
        StartWavesAt(restoreAt);
    }

    public void OnEnemyDied()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        killCount++;
    }

    // ── Núcleo do spawn contínuo ────────────────────────────────────────────────

    void StartWavesAt(float startGameTime)
    {
        if (isSpawning) return;
        isSpawning   = true;
        _gameTime    = startGameTime;
        killCount    = 0;
        enemiesAlive = 0;
        currentWave  = 0;
        waveTimerSystem?.StartTimer();
        StartCoroutine(SpawnContinuous());
    }

    IEnumerator SpawnContinuous()
    {
        var activated = new HashSet<int>();

        while (isSpawning)
        {
            _gameTime += Time.deltaTime;

            for (int i = 0; i < Phases.Length; i++)
            {
                if (Phases[i].startTime <= _gameTime && !activated.Contains(i))
                {
                    activated.Add(i);
                    currentWave++;
                    Debug.Log($"[Spawn] Fase {currentWave}: {Phases[i].enemyType} aos {_gameTime:F0}s");
                    waveTimerSystem?.StartTimer();
                    OnWaveCompleted?.Invoke(currentWave);
                    GameManager.Instance?.IncrementWave(currentWave);
                    int capturedI    = i;
                    int capturedWave = currentWave;
                    StartCoroutine(ActivatePhase(Phases[capturedI], capturedWave));
                }
            }

            yield return null;
        }
    }

    IEnumerator ActivatePhase(SpawnPhase phase, int waveNum)
    {
        if (waveNum <= 5 && BiomeSystem.Instance != null)
        {
            var loreUI = Object.FindFirstObjectByType<LoreScreenUI>(FindObjectsInactive.Include);
            var config  = BiomeSystem.Instance.GetConfig(waveNum);
            int capturedWave = waveNum;

            if (loreUI != null && config != null)
            {
                yield return loreUI.StartCoroutine(
                    loreUI.ShowLore(config, () =>
                        BiomeSystem.Instance.SetBiome((BiomeSystem.Biome)(capturedWave - 1))
                    )
                );
            }
            else
            {
                BiomeSystem.Instance.SetBiome((BiomeSystem.Biome)(capturedWave - 1));
            }
        }
        BeginSpawning(phase);
    }

    void BeginSpawning(SpawnPhase phase) => StartCoroutine(SpawnPhaseLoop(phase));

    IEnumerator SpawnPhaseLoop(SpawnPhase phase)
    {
        while (isSpawning)
        {
            if (enemiesAlive < phase.maxOnScreen)
            {
                SpawnEnemyOfType(phase);
                enemiesAlive++;
            }
            yield return new WaitForSeconds(phase.spawnInterval);
        }
    }

    // ── Spawn de inimigo ────────────────────────────────────────────────────────

    void SpawnEnemyOfType(SpawnPhase phase)
    {
        var prefab = SelectPrefab(phase.enemyType);
        if (prefab == null)
        {
            Debug.LogError($"[WaveManager] Nenhum prefab para '{phase.enemyType}'.");
            return;
        }

        Vector3    pos   = GetSpawnPosition();
        GameObject enemy = ObjectPoolManager.Instance?.GetFromPool(prefab.name);
        if (enemy != null)
            enemy.transform.position = pos;
        else
            enemy = Instantiate(prefab, pos, Quaternion.identity);

        var eb = enemy.GetComponent<EnemyBase>();
        if (eb != null)
        {
            eb.OnDeathCallback = OnEnemyDied;
            eb.poolTag         = prefab.name;

            float hpMod = phase.healthMult;
            if (DifficultyAdaptiveSystem.Instance != null)
                hpMod *= DifficultyAdaptiveSystem.Instance.EnemyHealthModifier;
            if (!Mathf.Approximately(hpMod, 1f))
            {
                eb.maxHealth *= hpMod;
                eb.InitializeHealth();
            }
            if (!Mathf.Approximately(phase.speedMult, 1f))
                eb.moveSpeed *= phase.speedMult;

            dynamicDifficulty?.ApplyToEnemy(eb);
        }
        else
            Debug.LogWarning($"[WaveManager] '{prefab.name}' sem EnemyBase.");
    }

    GameObject SelectPrefab(string enemyType)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return null;
        if (enemyType == "All") return enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        string[] kws     = GetKeywords(enemyType);
        var      matches = new List<GameObject>();
        foreach (var p in enemyPrefabs)
        {
            if (p == null) continue;
            foreach (var kw in kws)
                if (p.name.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    { matches.Add(p); break; }
        }
        return matches.Count > 0
            ? matches[Random.Range(0, matches.Count)]
            : enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
    }

    static string[] GetKeywords(string enemyType)
    {
        switch (enemyType)
        {
            case "Zombie":   return new[] { "Zumbi", "Zombie" };
            case "Slime":    return new[] { "Slime" };
            case "Archer":   return new[] { "Archer" };
            case "Orc":      return new[] { "Orc" };
            case "Mage":     return new[] { "Mage" };
            case "Assassin": return new[] { "Assassin" };
            default:         return new[] { enemyType };
        }
    }

    // ── Posição de spawn — bordas da câmera ─────────────────────────────────────

    Vector3 GetSpawnPosition()
    {
        Camera  cam    = Camera.main;
        Vector3 camPos = cam != null
            ? cam.transform.position
            : (PlayerController.Instance != null
                ? (Vector3)PlayerController.Instance.transform.position
                : Vector3.zero);

        float camH = cam != null ? cam.orthographicSize + 2f : 12f;
        float camW = cam != null ? camH * cam.aspect + 2f    : 20f;

        switch (Random.Range(0, 4))
        {
            case 0:  return new Vector3(camPos.x + Random.Range(-camW, camW), camPos.y + camH, 0f); // topo
            case 1:  return new Vector3(camPos.x + Random.Range(-camW, camW), camPos.y - camH, 0f); // base
            case 2:  return new Vector3(camPos.x - camW, camPos.y + Random.Range(-camH, camH), 0f); // esquerda
            default: return new Vector3(camPos.x + camW,  camPos.y + Random.Range(-camH, camH), 0f); // direita
        }
    }

    // ── Propriedades de leitura ─────────────────────────────────────────────────

    public WaveType CurrentWaveType
    {
        get
        {
            if (currentWave >= Phases.Length) return WaveType.Boss;
            if (currentWave == 4 || currentWave == 5) return WaveType.Elite;
            return WaveType.Normal;
        }
    }

    public float GameTime     => _gameTime;
    public int   CurrentWave  => currentWave;
    public int   TotalWaves   => Phases.Length;
    public int   EnemiesAlive => enemiesAlive;
    public int   KillCount    => killCount;
    public int   KillQuota    => 0;
}
