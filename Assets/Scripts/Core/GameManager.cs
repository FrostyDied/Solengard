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

[System.Serializable]
public struct RunData
{
    public int    wavesCompleted;
    public int    waveReached;
    public int    totalKills;
    public float  timeSurvived;
    public string causeOfDeath;
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

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        WaveManager.OnAllWavesCompleted    += HandleAllWavesCompleted;
        WaveManager.OnWaveCompleted        += HandleWaveCompleted;
        EnemyBase.OnEnemyDied              += HandleEnemyDied;
        WaveTimerSystem.OnWaveTimerExpired += HandleTimerExpired;
    }

    void OnDisable()
    {
        WaveManager.OnAllWavesCompleted    -= HandleAllWavesCompleted;
        WaveManager.OnWaveCompleted        -= HandleWaveCompleted;
        EnemyBase.OnEnemyDied              -= HandleEnemyDied;
        WaveTimerSystem.OnWaveTimerExpired -= HandleTimerExpired;
    }

    void Start()
    {
        SetState(GameState.MainMenu);
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    public void StartGame()
    {
        if (currentState != GameState.MainMenu) return;

        currentRunData = new RunData { causeOfDeath = "inimigo" };
        runStartTime   = Time.time;

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
        if (currentState != GameState.Playing) return;

        currentRunData.waveReached  = waveManager != null ? waveManager.CurrentWave : currentRunData.wavesCompleted;
        currentRunData.timeSurvived = Time.time - runStartTime;

        runRewardSystem?.CalculateAndDeliverReward(currentRunData);

        SetState(GameState.GameOver);
        OnGameOver?.Invoke();
    }

    // Recarrega a cena ativa para iniciar uma nova run
    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ── Handlers privados ───────────────────────────────────────────────────────

    void HandleAllWavesCompleted()
    {
        if (currentState != GameState.Playing) return;

        currentRunData.wavesCompleted = waveManager != null ? waveManager.TotalWaves : currentRunData.wavesCompleted;
        currentRunData.waveReached    = currentRunData.wavesCompleted;
        currentRunData.timeSurvived   = Time.time - runStartTime;
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
        currentRunData.causeOfDeath = "tempo esgotado";
    }

    void SetState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Debug.Log($"[GameManager] Estado: {newState}");
        OnGameStateChanged?.Invoke(newState);
    }
}
