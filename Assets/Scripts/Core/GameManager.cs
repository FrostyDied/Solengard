using UnityEngine;

// Estados possíveis do jogo em qualquer momento
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Victory
}

// Controlador central do Solengard — deve existir exatamente uma instância por sessão.
// Attach este componente em um GameObject "GameManager" na cena principal.
public class GameManager : MonoBehaviour
{
    // ── Singleton ───────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ── Eventos estáticos (a UI e outros sistemas assinam aqui) ─────────────────

    // Disparado sempre que o estado muda; passa o novo estado como argumento
    public static event System.Action<GameState> OnGameStateChanged;

    // Disparado especificamente no Game Over (atalho para a tela de derrota)
    public static event System.Action OnGameOver;

    // ── Referências ─────────────────────────────────────────────────────────────

    [Header("Referências")]
    public WaveManager waveManager;
    [SerializeField] ProceduralArenaSystem proceduralArena;

    // ── Estado interno ──────────────────────────────────────────────────────────

    GameState currentState;

    public GameState CurrentState => currentState;

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Garante que só existe uma instância; destrói duplicatas ao trocar de cena
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        WaveManager.OnAllWavesCompleted += HandleAllWavesCompleted;
    }

    void OnDisable()
    {
        // Remove a assinatura para evitar referências mortas ao descarregar a cena
        WaveManager.OnAllWavesCompleted -= HandleAllWavesCompleted;
    }

    void Start()
    {
        // Começa no menu principal; StartGame() deve ser chamado pela UI
        SetState(GameState.MainMenu);
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    // Chamado pelo botão "Jogar" na UI do menu principal
    public void StartGame()
    {
        if (currentState != GameState.MainMenu) return;

        SetState(GameState.Playing);
        proceduralArena?.InitializeRun();

        if (waveManager != null)
            waveManager.StartWave();
        else
            Debug.LogWarning("[GameManager] WaveManager não atribuído no Inspector.");
    }

    // Pausa o jogo congelando o tempo físico
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    // Retoma o jogo restaurando o tempo físico
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    // Chamado quando o player morre
    public void TriggerGameOver()
    {
        if (currentState != GameState.Playing) return;

        SetState(GameState.GameOver);
        OnGameOver?.Invoke();
    }

    // ── Handlers privados ───────────────────────────────────────────────────────

    // Chamado pelo evento do WaveManager quando todas as waves forem concluídas
    void HandleAllWavesCompleted()
    {
        if (currentState != GameState.Playing) return;

        SetState(GameState.Victory);
    }

    // Centraliza a mudança de estado e notifica os ouvintes
    void SetState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"[GameManager] Estado: {newState}");
        OnGameStateChanged?.Invoke(newState);
    }
}
