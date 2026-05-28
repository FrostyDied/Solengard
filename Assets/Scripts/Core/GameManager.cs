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

    public static event System.Action<GameState> OnGameStateChanged;
    public static event System.Action            OnGameOver;

    // ── Referências ─────────────────────────────────────────────────────────────

    [Header("Referências")]
    public WaveManager waveManager;
    [SerializeField] ProceduralArenaSystem proceduralArena;
    [SerializeField] RunRewardSystem       runRewardSystem;

    // ── Estado interno ──────────────────────────────────────────────────────────

    GameState currentState;
    public GameState CurrentState => currentState;

    public RunData currentRunData;
    float          runStartTime;

    float runTimer;
    // Segundos decorridos desde StartGame(); incrementa apenas enquanto Playing
    public float RunTimeSeconds => runTimer;

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded           += OnSceneLoaded;
        WaveManager.OnAllWavesCompleted    += HandleAllWavesCompleted;
        WaveManager.OnWaveCompleted        += HandleWaveCompleted;
        EnemyBase.OnEnemyDied              += HandleEnemyDied;
        WaveTimerSystem.OnWaveTimerExpired += HandleTimerExpired;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded           -= OnSceneLoaded;
        WaveManager.OnAllWavesCompleted    -= HandleAllWavesCompleted;
        WaveManager.OnWaveCompleted        -= HandleWaveCompleted;
        EnemyBase.OnEnemyDied              -= HandleEnemyDied;
        WaveTimerSystem.OnWaveTimerExpired -= HandleTimerExpired;
    }

    // Refreshes waveManager after a scene reload — the serialized field points to the old scene's destroyed instance
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (waveManager == null)
            waveManager = Object.FindFirstObjectByType<WaveManager>();
    }

    void Start()
    {
        SetState(GameState.MainMenu);
    }

    void Update()
    {
        if (currentState == GameState.Playing)
            runTimer += Time.deltaTime;
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    public void StartGame()
    {
        if (currentState != GameState.MainMenu && currentState != GameState.GameOver) return;

        currentRunData = new RunData { causeOfDeath = "inimigo" };
        runStartTime   = Time.time;
        runTimer       = 0f;

        SetState(GameState.Playing);
        proceduralArena?.InitializeRun();

        if (waveManager != null)
            waveManager.StartWave();
        else
            Debug.LogWarning("[GameManager] WaveManager não atribuído no Inspector.");
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
        if (currentState != GameState.Playing)
        {
            Debug.LogWarning($"[GameManager] TriggerGameOver ignorado — estado atual: {currentState}");
            return;
        }

        Time.timeScale = 0f; // pausa o jogo imediatamente, antes de qualquer outra operação

        currentRunData.waveReached  = waveManager != null ? waveManager.CurrentWave : currentRunData.wavesCompleted;
        currentRunData.timeSurvived = runTimer;

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
        SetState(GameState.MainMenu); // reseta estado antes de recarregar para GameSceneBootstrap encontrar MainMenu
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ── Handlers privados ───────────────────────────────────────────────────────

    void HandleAllWavesCompleted()
    {
        if (currentState != GameState.Playing) return;

        currentRunData.wavesCompleted = waveManager != null ? waveManager.TotalWaves : currentRunData.wavesCompleted;
        currentRunData.waveReached    = currentRunData.wavesCompleted;
        currentRunData.timeSurvived   = runTimer;
        currentRunData.causeOfDeath   = "vitória";

        runRewardSystem?.CalculateAndDeliverReward(currentRunData);

        SetState(GameState.Victory);
    }

    void HandleWaveCompleted(int wave)
    {
        currentRunData.wavesCompleted = wave;
    }

    void HandleEnemyDied()
    {
        currentRunData.totalKills++;
    }

    void HandleTimerExpired()
    {
        currentRunData.causeOfDeath   = "tempo esgotado";
        currentRunData.lastDeathCause = DeathCause.TempoEsgotado;
    }

    void SetState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"[GameManager] Estado: {newState}");
        OnGameStateChanged?.Invoke(newState);
    }
}
