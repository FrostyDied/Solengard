using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType { Normal, Elite, Boss }

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Prefabs de inimigos")]
    public List<GameObject> enemyPrefabs = new();

    [Header("Configuração base")]
    [SerializeField] float spawnInterval    = 0.5f;
    [SerializeField] int   maxOnScreen      = 40;
    [SerializeField] int   totalWaves       = 5;
    [SerializeField] float waveDuration     = 60f;
    [SerializeField] float timeBetweenWaves = 3f;

    public int   CurrentWave       { get; private set; } = 1;
    public int   TotalWaves        => totalWaves;
    public float WaveTimeRemaining { get; private set; }
    public int   KillCount         { get; private set; }
    public bool  IsRunning         { get; private set; }

    // Compatibilidade com sistemas que leem CurrentWaveType
    public WaveType CurrentWaveType
    {
        get
        {
            if (CurrentWave >= totalWaves) return WaveType.Boss;
            if (CurrentWave == 4 || CurrentWave == 5) return WaveType.Elite;
            return WaveType.Normal;
        }
    }

    public static event System.Action<int> OnWaveStarted;
    public static event System.Action<int> OnWaveCompleted;
    public static event System.Action      OnAllWavesCompleted;

    // Índices em enemyPrefabs por wave
    // [0] Slime  [1] Zumbi  [2] Archer  [3] Orc  [4] Mage  [5] Assassin  [6] Golem
    readonly int[][] _waveUnlocks = new int[][]
    {
        new int[]{ 0, 1 },             // Wave 1: Slime + Zumbi
        new int[]{ 0, 1, 2 },          // Wave 2: + Archer
        new int[]{ 0, 1, 2, 3 },       // Wave 3: + Orc
        new int[]{ 1, 2, 3, 4, 5 },    // Wave 4: + Mage + Assassin
        new int[]{ 2, 3, 4, 5, 6 },    // Wave 5: + Golem
    };

    Transform           _player;
    List<GameObject>    _activeEnemies = new();
    Coroutine           _spawnCoroutine;
    Coroutine           _waveCoroutine;

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

    // ── API pública ─────────────────────────────────────────────────────────────

    public void StartWaves()
    {
        if (IsRunning) return;
        IsRunning   = true;
        KillCount   = 0;
        CurrentWave = 1;
        _waveCoroutine = StartCoroutine(WaveLoop());
    }

    // Alias para compatibilidade com GameManager existente
    public void StartWave() => StartWaves();

    public void RestoreToWave(int wave)
    {
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        if (_waveCoroutine  != null) StopCoroutine(_waveCoroutine);
        IsRunning   = false;
        CurrentWave = Mathf.Clamp(wave, 1, totalWaves);
        IsRunning   = true;
        _waveCoroutine = StartCoroutine(WaveLoop());
        Debug.Log($"[WaveManager] RestoreToWave({wave})");
    }

    // ── Loop principal ──────────────────────────────────────────────────────────

    IEnumerator WaveLoop()
    {
        while (CurrentWave <= totalWaves)
        {
            WaveTimeRemaining = waveDuration;
            OnWaveStarted?.Invoke(CurrentWave);
            GameManager.Instance?.IncrementWave(CurrentWave);
            Debug.Log($"[Wave] Wave {CurrentWave} iniciada");

            _spawnCoroutine = StartCoroutine(SpawnLoop());

            while (WaveTimeRemaining > 0f)
            {
                WaveTimeRemaining -= Time.deltaTime;
                yield return null;
            }

            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            OnWaveCompleted?.Invoke(CurrentWave);
            Debug.Log($"[Wave] Wave {CurrentWave} completa. Kills: {KillCount}");

            ClearEnemies();
            yield return new WaitForSeconds(timeBetweenWaves);
            CurrentWave++;
        }

        IsRunning = false;
        OnAllWavesCompleted?.Invoke();
        Debug.Log("[Wave] Todas as waves concluídas!");
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (_player == null) FindPlayer();

            float interval = Mathf.Max(0.15f, spawnInterval - (CurrentWave - 1) * 0.07f);
            int   max      = maxOnScreen + (CurrentWave - 1) * 12;

            if (_player != null && CountActive() < max && enemyPrefabs.Count > 0)
                SpawnEnemy();

            yield return new WaitForSeconds(interval);
        }
    }

    // ── Spawn ───────────────────────────────────────────────────────────────────

    void SpawnEnemy()
    {
        Camera  cam    = Camera.main;
        Vector3 camPos = cam != null ? cam.transform.position : _player.position;

        float camH = cam != null ? cam.orthographicSize + 3f : 14f;
        float camW = cam != null ? camH * cam.aspect    + 3f : 20f;

        Vector3 spawnPos;
        switch (Random.Range(0, 4))
        {
            case 0:  spawnPos = new Vector3(camPos.x + Random.Range(-camW, camW), camPos.y + camH, 0f); break;
            case 1:  spawnPos = new Vector3(camPos.x + Random.Range(-camW, camW), camPos.y - camH, 0f); break;
            case 2:  spawnPos = new Vector3(camPos.x - camW, camPos.y + Random.Range(-camH, camH), 0f); break;
            default: spawnPos = new Vector3(camPos.x + camW,  camPos.y + Random.Range(-camH, camH), 0f); break;
        }

        var allowedIdx = GetAllowedIndices();
        int idx        = allowedIdx[Random.Range(0, allowedIdx.Length)];
        var prefab     = enemyPrefabs[idx];
        if (prefab == null) return;

        var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        var eb = enemy.GetComponent<EnemyBase>();
        if (eb != null)
        {
            var captured = enemy;
            eb.OnDeathCallback = () =>
            {
                KillCount++;
                _activeEnemies.Remove(captured);
            };
        }

        _activeEnemies.Add(enemy);
    }

    int[] GetAllowedIndices()
    {
        int waveIdx = Mathf.Clamp(CurrentWave - 1, 0, _waveUnlocks.Length - 1);
        var allowed = _waveUnlocks[waveIdx];
        var valid   = new List<int>();
        foreach (var i in allowed)
            if (i < enemyPrefabs.Count) valid.Add(i);
        return valid.Count > 0 ? valid.ToArray() : new int[] { 0 };
    }

    int CountActive()
    {
        _activeEnemies.RemoveAll(e => e == null);
        return _activeEnemies.Count;
    }

    void ClearEnemies()
    {
        foreach (var e in _activeEnemies)
            if (e != null) Destroy(e);
        _activeEnemies.Clear();
    }

    // OnEnemyDied mantido para compatibilidade com sistemas legados
    public void OnEnemyDied()
    {
        KillCount++;
    }
}
