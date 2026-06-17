using System.Collections.Generic;
using UnityEngine;

// Estatísticas LIFETIME do jogador (tela Legado). Singleton criado pelo SystemsBootstrap,
// persiste entre cenas. Acumula contadores ao fim de cada run assinando
// RunRewardSystem.OnRunRewardCalculated — NÃO altera o fluxo de run.
public class LegadoStatsSystem : MonoBehaviour
{
    public static LegadoStatsSystem Instance { get; private set; }

    const string K_RUNS     = "sol_lt_runs";
    const string K_TIME     = "sol_lt_time";      // segundos acumulados
    const string K_DIAMONDS = "sol_lt_diamonds";  // diamantes ganhos lifetime
    const string K_KILLS    = "sol_lt_kills";
    const string K_MAXZONE  = "sol_lt_maxzone";
    const string K_CLASS    = "sol_lt_class_";     // + classId

    // classId -> nome de exibição (para "personagem mais usado")
    static readonly (string id, string nome)[] Classes =
    {
        ("warrior",     "Guerreiro"),
        ("mage",        "Mago"),
        ("assassin",    "Assassino"),
        ("necromancer", "Necromante"),
        ("paladin",     "Paladino"),
        ("hunter",      "Caçador"),
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (Instance != this) return;
        RunRewardSystem.OnRunRewardCalculated += AoRunFinalizada;
    }

    void OnDisable()
    {
        if (Instance != this) return;
        RunRewardSystem.OnRunRewardCalculated -= AoRunFinalizada;
    }

    void AoRunFinalizada(RunSummary s)
    {
        PlayerPrefs.SetInt   (K_RUNS,     TotalRuns + 1);
        PlayerPrefs.SetFloat (K_TIME,     TempoTotalSegundos + Mathf.Max(0f, s.timeSurvived));
        PlayerPrefs.SetInt   (K_DIAMONDS, DiamantesLifetime + Mathf.Max(0, s.diamondsEarned));
        PlayerPrefs.SetInt   (K_KILLS,    KillsLifetime + Mathf.Max(0, s.totalKills));
        PlayerPrefs.SetInt   (K_MAXZONE,  Mathf.Max(ZonaMaxima, s.waveReached));

        string classId = PlayerClassManager.Instance?.CurrentClass?.classId;
        if (!string.IsNullOrEmpty(classId))
        {
            string chave = K_CLASS + classId;
            PlayerPrefs.SetInt(chave, PlayerPrefs.GetInt(chave, 0) + 1);
        }

        PlayerPrefs.Save();
        Debug.Log($"[Legado] Run registrada — runs={TotalRuns} kills={KillsLifetime} maxZone={ZonaMaxima}");
    }

    // ── Getters lifetime ────────────────────────────────────────────────────────

    public int   TotalRuns          => PlayerPrefs.GetInt(K_RUNS, 0);
    public float TempoTotalSegundos => PlayerPrefs.GetFloat(K_TIME, 0f);
    public int   DiamantesLifetime  => PlayerPrefs.GetInt(K_DIAMONDS, 0);
    public int   KillsLifetime      => PlayerPrefs.GetInt(K_KILLS, 0);
    public int   ZonaMaxima         => PlayerPrefs.GetInt(K_MAXZONE, 0);

    // ── Getters derivados (dados já existentes) ──────────────────────────────────

    public int MelhorPontuacao => PlayerPrefs.GetInt("sol_best_score", 0);

    public LastRunData UltimaRun()
    {
        string json = PlayerPrefs.GetString("sol_last_run", "");
        if (string.IsNullOrEmpty(json)) return null;
        try   { return JsonUtility.FromJson<LastRunData>(json); }
        catch { return null; }
    }

    // Retorna (nome, partidas) do personagem mais usado; ("—", 0) se nenhum.
    public (string nome, int partidas) PersonagemMaisUsado()
    {
        string melhorNome = "—";
        int    melhor     = 0;
        foreach (var (id, nome) in Classes)
        {
            int n = PlayerPrefs.GetInt(K_CLASS + id, 0);
            if (n > melhor) { melhor = n; melhorNome = nome; }
        }
        return (melhorNome, melhor);
    }

    // Formata segundos como "HHh MMm" (ou "MMm" se < 1h).
    public static string FormatarTempo(float segundos)
    {
        int total = Mathf.FloorToInt(segundos);
        int h = total / 3600;
        int m = (total % 3600) / 60;
        return h > 0 ? $"{h}h {m:00}m" : $"{m}m";
    }
}
