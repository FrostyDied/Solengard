using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Victory
}

public enum DeathCause
{
    Inimigo,
    TempoEsgotado,
    Boss,
    Veneno,
    Sangramento
}

[System.Serializable]
public struct RunData
{
    public int        wavesCompleted;
    public int        waveReached;
    public int        totalKills;
    public float      timeSurvived;
    public string     causeOfDeath;
    public DeathCause lastDeathCause;
}

public class GameManager : MonoBehaviour
{
    // ── Singleton ───────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ── Eventos estáticos ────────────────────────────────────────────────────────

    public static event System.Action<GameState>      OnGameStateChanged;
    public static event System.Action                 OnGameOver;
    public static event System.Action<RunSessionData> OnSessionFound;

    // ── Referências ─────────────────────────────────────────────────────────────

    [Header("Referências")]
    [SerializeField] ZoneManager           zoneManager;
    [SerializeField] ProceduralArenaSystem proceduralArena;
    [SerializeField] RunRewardSystem       runRewardSystem;

    // ── Estado interno ──────────────────────────────────────────────────────────

    GameState currentState;
    public GameState CurrentState => currentState;

    bool _gameStarted;

    public RunData currentRunData;
    float          runStartTime;

    float runTimer;
    // Segundos decorridos desde StartGame(); incrementa apenas enquanto Playing
    public float RunTimeSeconds => runTimer;

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Awake()
    {
        Debug.Log($"[GameManager] Awake — Instance={(Instance == null ? "null" : Instance.GetInstanceID().ToString())} ThisID={GetInstanceID()}");
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded         += OnSceneLoaded;
        WaveManager.OnAllWavesCompleted  += HandleAllWavesCompleted;
        WaveManager.OnWaveCompleted      += HandleWaveCompleted;
        ZoneManager.OnGameOver           += HandleZoneGameOver;
        ZoneManager.OnAllZonesCompleted  += HandleZoneVictory;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded         -= OnSceneLoaded;
        WaveManager.OnAllWavesCompleted  -= HandleAllWavesCompleted;
        WaveManager.OnWaveCompleted      -= HandleWaveCompleted;
        ZoneManager.OnGameOver           -= HandleZoneGameOver;
        ZoneManager.OnAllZonesCompleted  -= HandleZoneVictory;
    }

    // Refreshes scene-bound references after reload and triggers AutoStart on restarts.
    // Initial load is handled by Start(); reloads (RestartRun) are handled here.
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (runRewardSystem == null)
            runRewardSystem = Object.FindFirstObjectByType<RunRewardSystem>();

        // Restart path: Start() won't run again because GameManager is DontDestroyOnLoad
        if (scene.name == "GameScene" && !_gameStarted && currentState == GameState.MainMenu)
            AutoStart();
    }

    void Start()
    {
        SetState(GameState.MainMenu);

        if (RunSessionManager.Instance != null && RunSessionManager.Instance.HasActiveSession())
        {
            var session = RunSessionManager.Instance.LoadSession();
            Debug.Log($"[GameManager] Sessao ativa encontrada — wave={session.currentWave} kills={session.killCount}");
            OnSessionFound?.Invoke(session);
        }

        // Initial load path
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene")
            AutoStart();
    }

    void AutoStart()
    {
        if (Camera.main != null) Camera.main.backgroundColor = Color.black;
        StartCoroutine(SafetyTimeScale());
        StartGame();
    }

    // Fade de preto para transparente — chamado APÓS lore para revelar o jogo (sortingOrder 997, abaixo da lore 999)
    IEnumerator FadeFromBlack(System.Action onComplete)
    {
        var overlay = new GameObject("FadeOverlay");
        var canvas  = overlay.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 997;
        var img  = overlay.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        var rect = img.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        float t = 0f;
        while (t < 1f)
        {
            t        += Time.unscaledDeltaTime * 1.2f;
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
        Destroy(overlay);
        onComplete?.Invoke();
    }

    // Timeout longo: lore pode durar ~10s + tempo do usuário pressionar.
    // 15s a partir do início da lore é seguro sem prejudicar a UX normal.
    IEnumerator SafetyTimeScale()
    {
        yield return new WaitForSecondsRealtime(15f);
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("[GameManager] timeScale estava 0 após 15s — forçando para 1 (lore travada?)");
            Time.timeScale = 1f;
            ZoneManager.Instance?.StartZones();
        }
    }

    void Update()
    {
        if (currentState == GameState.Playing)
            runTimer += Time.deltaTime;
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    public void StartGame()
    {
        if (_gameStarted) { Debug.LogWarning("[GameManager] StartGame duplicata ignorada"); return; }
        _gameStarted = true;
        if (currentState != GameState.MainMenu && currentState != GameState.GameOver) return;

        Application.targetFrameRate  = 60;
        QualitySettings.vSyncCount   = 0;
        Screen.sleepTimeout          = SleepTimeout.NeverSleep;
        Physics2D.velocityIterations = 4;
        Physics2D.positionIterations = 2;
        Time.fixedDeltaTime          = 0.02f;

        bool sessaoAtiva = RunSessionManager.Instance != null && RunSessionManager.Instance.HasActiveSession();
        RunSessionData sessaoData = sessaoAtiva ? RunSessionManager.Instance.LoadSession() : default;
        if (sessaoAtiva)
            Debug.Log($"[GameManager] Sessao ativa — wave={sessaoData.currentWave}, restaurada apos lore");

        currentRunData = new RunData { causeOfDeath = "inimigo" };
        runStartTime   = Time.time;
        runTimer       = 0f;

        SetState(GameState.Playing);

        // Canvas do LoreScreenUI agora sempre ativo — não precisa de FindObjectsInactive
        var loreUI = Object.FindFirstObjectByType<LoreScreenUI>();
        var config  = BiomeSystem.Instance?.GetConfig(1);
        Debug.Log($"[GameManager] StartGame loreUI={loreUI != null} config={config != null} sessao={sessaoAtiva}");

        System.Action afterLore = sessaoAtiva
            ? (System.Action)(() => StartCoroutine(FadeFromBlack(() => RestoreSession(sessaoData))))
            : ()          => StartCoroutine(FadeFromBlack(() =>
              {
                  proceduralArena?.InitializeRun();
                  ZoneManager.Instance?.StartZones();
              }));

        if (loreUI != null && config != null)
            StartCoroutine(loreUI.ShowLore(config, afterLore));
        else
        {
            Debug.LogWarning($"[GameManager] Sem lore — fallback direto. loreUI={loreUI != null} config={config != null}");
            afterLore();
        }
    }

    // Restaura o estado de jogo a partir de uma sessão salva
    public void RestoreSession(RunSessionData session)
    {
        currentRunData = new RunData
        {
            waveReached    = session.currentWave,
            wavesCompleted = Mathf.Max(0, session.currentWave - 1),
            totalKills     = session.killCount,
            timeSurvived   = session.timeElapsed,
            causeOfDeath   = string.IsNullOrEmpty(session.causeOfDeath) ? "inimigo" : session.causeOfDeath,
        };
        runTimer     = session.timeElapsed;
        runStartTime = Time.time;

        SetState(GameState.Playing);
        proceduralArena?.InitializeRun();

        var ph = Object.FindFirstObjectByType<PlayerHealth>();
        if (ph != null) ph.RestoreHealth(session.currentHealth, session.maxHealth);

        ZoneManager.Instance?.RestoreToZone(session.currentWave - 1);

        RunSessionManager.Instance?.ClearSession();
        Debug.Log($"[GameManager] Sessao restaurada — wave={session.currentWave} kills={session.killCount} hp={session.currentHealth:F0}");
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void TriggerGameOver()
    {
        Debug.Log($"[TriggerGameOver] estado={currentState} runReward={runRewardSystem != null}");
        if (currentState != GameState.Playing && currentState != GameState.Paused)
        {
            Debug.LogWarning($"[GameManager] TriggerGameOver ignorado — estado: {currentState}");
            return;
        }

        Time.timeScale = 0f; // garante pausa mesmo se já estava Paused

        currentRunData.waveReached  = ZoneManager.Instance != null
            ? ZoneManager.Instance.CurrentZone + 1
            : currentRunData.wavesCompleted;
        currentRunData.timeSurvived = runTimer;

        Debug.Log($"[GameOver] kills={currentRunData.totalKills} wave={currentRunData.waveReached} time={RunTimeSeconds:F1} runRewardSystem={runRewardSystem != null}");

        RunSessionManager.Instance?.ClearSession();

        try { runRewardSystem?.CalculateAndDeliverReward(currentRunData); }
        catch (System.Exception e) { Debug.LogError($"[GameManager] RunRewardSystem exception: {e.Message}"); }

        SetState(GameState.GameOver);
        OnGameOver?.Invoke();
    }

    // Retorna ao estado Playing após revive — só aplicável quando em GameOver
    public void SetStatePlaying()
    {
        if (currentState == GameState.GameOver)
            SetState(GameState.Playing);
    }

    // Recarrega a cena ativa para iniciar uma nova run
    public void RestartRun()
    {
        _gameStarted = false;
        SetState(GameState.MainMenu);
        RunSessionManager.Instance?.ClearSession();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ── Handlers privados ───────────────────────────────────────────────────────

    void HandleZoneGameOver(string motivo)
    {
        if (currentState != GameState.Playing && currentState != GameState.Paused) return;
        currentRunData.causeOfDeath = motivo;
        TriggerGameOver();
    }

    void HandleZoneVictory()
    {
        if (currentState != GameState.Playing) return;
        currentRunData.causeOfDeath = "vitória";
        currentRunData.timeSurvived = runTimer;
        RunSessionManager.Instance?.ClearSession();
        runRewardSystem?.CalculateAndDeliverReward(currentRunData);
        SetState(GameState.Victory);
    }

    void HandleAllWavesCompleted()
    {
        if (currentState != GameState.Playing) return;

        currentRunData.waveReached    = currentRunData.wavesCompleted;
        currentRunData.timeSurvived   = runTimer;
        currentRunData.causeOfDeath   = "vitória";

        RunSessionManager.Instance?.ClearSession();
        runRewardSystem?.CalculateAndDeliverReward(currentRunData);

        SetState(GameState.Victory);
    }

    void HandleWaveCompleted(int wave)
    {
        currentRunData.wavesCompleted = wave;
    }

    // ── API pública de tracking (chamada diretamente por EnemyBase e WaveManager) ─

    public void IncrementKill()
    {
        currentRunData.totalKills++;
        if (currentRunData.totalKills % 5 == 0)
            Debug.Log($"[GameManager] totalKills={currentRunData.totalKills}");
    }

    public void IncrementWave(int wave)
    {
        currentRunData.waveReached = wave;
    }

    void SetState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"[GameManager] Estado: {newState}");
        OnGameStateChanged?.Invoke(newState);
    }
}
