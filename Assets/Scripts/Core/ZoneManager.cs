using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [System.Serializable]
    public class ZoneConfig
    {
        public string              nome;
        public BiomeSystem.Biome   biome;
        public float durationSeconds  = 600f;
        public float bossSpawnAt      = 480f;
        public float bossTimeLimit    = 120f;
        public float hpMultiplier     = 1f;
        public float speedMultiplier  = 1f;
        public float damageMultiplier = 1f;
        public int   spawnMax         = 40;
        public float spawnInterval    = 0.5f;
        public int[] enemyIndices;
    }

    [Header("Configuração das 5 zonas")]
    [SerializeField] ZoneConfig[] zones = new ZoneConfig[]
    {
        new ZoneConfig {
            nome="Floresta de Veremoth",   biome=BiomeSystem.Biome.Veremoth,
            hpMultiplier=1f,  speedMultiplier=1f,  damageMultiplier=1f,
            spawnMax=40,  spawnInterval=0.50f, enemyIndices=new int[]{0,1} },
        new ZoneConfig {
            nome="Cavernas de Khorduum",   biome=BiomeSystem.Biome.Khorduum,
            hpMultiplier=1.8f, speedMultiplier=1.2f, damageMultiplier=1.5f,
            spawnMax=55,  spawnInterval=0.40f, enemyIndices=new int[]{0,1,2} },
        new ZoneConfig {
            nome="Cemitério de Valdross",  biome=BiomeSystem.Biome.Valdross,
            hpMultiplier=2.8f, speedMultiplier=1.4f, damageMultiplier=2f,
            spawnMax=65,  spawnInterval=0.35f, enemyIndices=new int[]{1,2,3} },
        new ZoneConfig {
            nome="Pântano de Gorveth",     biome=BiomeSystem.Biome.Gorveth,
            hpMultiplier=4f,  speedMultiplier=1.6f, damageMultiplier=2.8f,
            spawnMax=75,  spawnInterval=0.30f, enemyIndices=new int[]{2,3,4,5} },
        new ZoneConfig {
            nome="Campo de Arkenfall",     biome=BiomeSystem.Biome.Arkenfall,
            hpMultiplier=6f,  speedMultiplier=1.8f, damageMultiplier=3.5f,
            spawnMax=85,  spawnInterval=0.25f, enemyIndices=new int[]{3,4,5,6} },
    };

    public int   CurrentZone       { get; private set; } = 0;
    public float ZoneTimeRemaining { get; private set; }
    public float BossTimeRemaining { get; private set; }
    public bool  BossActive        { get; private set; }
    public bool  IsRunning         { get; private set; }
    public int   KillCount         { get; private set; }

    public static event System.Action<int>    OnZoneStarted;
    public static event System.Action<int>    OnZoneCompleted;
    public static event System.Action         OnBossSpawned;
    public static event System.Action         OnBossDefeated;
    public static event System.Action<string> OnGameOver;
    public static event System.Action         OnAllZonesCompleted;

    [Header("Prefabs de inimigos (mesma ordem do WaveManager)")]
    public List<GameObject> enemyPrefabs = new();

    [Header("Prefab do boss (EnemyGolem amplificado)")]
    [SerializeField] GameObject bossPrefab;

    Transform        _player;
    List<GameObject> _activeEnemies = new();
    GameObject       _bossInstance;
    Coroutine        _spawnCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => FindPlayer();

    void FindPlayer()
    {
        if (PlayerController.Instance != null)
            _player = PlayerController.Instance.transform;
        else
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
    }

    public void StartZones()
    {
        if (IsRunning) return;
        IsRunning   = true;
        CurrentZone = 0;
        StartCoroutine(ZoneLoop());
    }

    IEnumerator ZoneLoop()
    {
        while (CurrentZone < zones.Length)
        {
            var zone = zones[CurrentZone];
            ZoneTimeRemaining = zone.durationSeconds;
            BossActive        = false;

            BiomeSystem.Instance?.SetBiome(zone.biome);
            OnZoneStarted?.Invoke(CurrentZone);
            Debug.Log($"[Zone] Iniciando zona {CurrentZone + 1}: {zone.nome}");

            EnemyBase.GlobalHPMult     = zone.hpMultiplier;
            EnemyBase.GlobalSpeedMult  = zone.speedMultiplier;
            EnemyBase.GlobalDamageMult = zone.damageMultiplier;

            _spawnCoroutine = StartCoroutine(SpawnLoop(zone));

            bool zoneCleared = false;

            while (ZoneTimeRemaining > 0f && !zoneCleared)
            {
                ZoneTimeRemaining -= Time.deltaTime;

                if (!BossActive && ZoneTimeRemaining <= zone.durationSeconds - zone.bossSpawnAt)
                    StartCoroutine(SpawnBoss(zone));

                if (BossActive)
                {
                    BossTimeRemaining -= Time.deltaTime;

                    if (BossTimeRemaining <= 0f)
                    {
                        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
                        OnGameOver?.Invoke($"Tempo esgotado na {zone.nome}");
                        IsRunning = false;
                        yield break;
                    }

                    if (_bossInstance == null)
                    {
                        BossActive   = false;
                        zoneCleared  = true;
                        OnBossDefeated?.Invoke();
                        Debug.Log($"[Zone] Boss derrotado! Zona {CurrentZone + 1} completa.");
                    }
                }

                yield return null;
            }

            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            ClearEnemies();
            OnZoneCompleted?.Invoke(CurrentZone);

            var ph = PlayerController.Instance?.GetComponent<PlayerHealth>();
            if (ph != null) ph.Curar(ph.MaxHealth);
            XPSystem.Instance?.ResetLevel();

            yield return new WaitForSeconds(3f);
            CurrentZone++;

            if (CurrentZone < zones.Length)
            {
                var nextConfig = BiomeSystem.Instance?.GetConfig(CurrentZone + 1);
                var loreUI     = Object.FindFirstObjectByType<LoreScreenUI>();
                if (loreUI != null && nextConfig != null)
                {
                    bool loreDone = false;
                    StartCoroutine(loreUI.ShowLore(nextConfig, () => loreDone = true));
                    yield return new WaitUntil(() => loreDone);
                }
            }
        }

        IsRunning = false;
        OnAllZonesCompleted?.Invoke();
        Debug.Log("[Zone] Todas as zonas concluídas! Vitória!");
    }

    IEnumerator SpawnLoop(ZoneConfig zone)
    {
        while (true)
        {
            if (_player == null) FindPlayer();

            float progress = 1f - (ZoneTimeRemaining / zone.durationSeconds);
            int   max      = Mathf.RoundToInt(zone.spawnMax * (1f + progress * 0.5f));
            float interval = Mathf.Max(0.15f, zone.spawnInterval * (1f - progress * 0.3f));

            if (_player != null && CountActive() < max && zone.enemyIndices.Length > 0)
                SpawnEnemy(zone);

            yield return new WaitForSeconds(interval);
        }
    }

    void SpawnEnemy(ZoneConfig zone)
    {
        int idx = zone.enemyIndices[Random.Range(0, zone.enemyIndices.Length)];
        if (idx >= enemyPrefabs.Count) idx = 0;
        var prefab = enemyPrefabs[idx];
        if (prefab == null) return;

        var enemy = Instantiate(prefab, GetSpawnPosition(), Quaternion.identity);
        var eb    = enemy.GetComponent<EnemyBase>();
        if (eb != null)
        {
            eb.OnDeathCallback = () =>
            {
                KillCount++;
                _activeEnemies.Remove(enemy);
            };
        }
        _activeEnemies.Add(enemy);
    }

    IEnumerator SpawnBoss(ZoneConfig zone)
    {
        BossActive        = true;
        BossTimeRemaining = zone.bossTimeLimit;

        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        ClearEnemies();

        yield return new WaitForSeconds(1f);

        var bossToUse = bossPrefab;
        if (bossToUse == null && enemyPrefabs.Count > 6) bossToUse = enemyPrefabs[6];
        if (bossToUse == null) yield break;

        _bossInstance = Instantiate(bossToUse, GetSpawnPosition(), Quaternion.identity);

        var eb = _bossInstance.GetComponent<EnemyBase>();
        if (eb != null)
        {
            eb.maxHealth *= 5f;
            eb.moveSpeed *= 1.5f;
            eb.InitializeHealth();
            _bossInstance.transform.localScale *= 3f;

            eb.OnDeathCallback = () => { _bossInstance = null; KillCount++; };
        }

        OnBossSpawned?.Invoke();
        Debug.Log($"[Zone] BOSS spawnado! {zone.bossTimeLimit}s para derrotá-lo!");

        yield return new WaitForSeconds(2f);
        _spawnCoroutine = StartCoroutine(SpawnLoop(zone));
    }

    Vector3 GetSpawnPosition()
    {
        if (_player == null) return Vector3.zero;
        var   cam    = Camera.main;
        Vector3 pos  = cam != null ? cam.transform.position : _player.position;
        float camH   = cam != null ? cam.orthographicSize + 3f : 14f;
        float camW   = cam != null ? camH * cam.aspect    + 3f : 20f;
        switch (Random.Range(0, 4))
        {
            case 0:  return new Vector3(pos.x + Random.Range(-camW, camW), pos.y + camH, 0f);
            case 1:  return new Vector3(pos.x + Random.Range(-camW, camW), pos.y - camH, 0f);
            case 2:  return new Vector3(pos.x - camW, pos.y + Random.Range(-camH, camH), 0f);
            default: return new Vector3(pos.x + camW,  pos.y + Random.Range(-camH, camH), 0f);
        }
    }

    int CountActive()
    {
        _activeEnemies.RemoveAll(e => e == null);
        return _activeEnemies.Count;
    }

    void ClearEnemies()
    {
        foreach (var e in _activeEnemies) if (e != null) Destroy(e);
        _activeEnemies.Clear();
    }

    public void RestoreToZone(int zone)
    {
        CurrentZone = Mathf.Clamp(zone, 0, zones.Length - 1);
    }
}
