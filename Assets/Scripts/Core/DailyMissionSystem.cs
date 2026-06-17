using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionType
{
    MatarInimigos,
    SobreviverWaves,
    ColetarDiamantes,
    VencerSemTomarDano, // mantido p/ compat; fora do pool atual
    JogarPartida,
    AlcancarZona,
    SobreviverTempo
}

[System.Serializable]
public class DailyMission
{
    public string      id;
    public MissionType tipo;
    public string      descricao;
    public int         meta;
    public int         progresso;
    public int         recompensaDiamantes;
    public bool        concluida;
    public bool        recompensaResgatada;

    public bool ProgessoCompleto => progresso >= meta;
}

// Sistema de missões — DIÁRIAS (reset 24h UTC) e SEMANAIS (reset semana ISO).
// NOTA: a classe mantém o nome DailyMissionSystem por compat de GUID com a cena;
// hoje gerencia diárias E semanais. É singleton (criado pelo SystemsBootstrap) e
// persiste entre cenas. Progresso salvo em PlayerPrefs (seeded por data/semana).
public class DailyMissionSystem : MonoBehaviour
{
    public static DailyMissionSystem Instance { get; private set; }

    public static event System.Action<DailyMission> OnMissionCompleted;
    public static event System.Action               OnMissionsChanged;

    const string PREF_DATA_DIARIAS  = "sol_mission_date";
    const string PREF_DIARIAS_JSON  = "sol_missions";
    const string PREF_ID_SEMANAIS   = "sol_weekmission_id";
    const string PREF_SEMANAIS_JSON = "sol_weekmissions";

    List<DailyMission> diarias  = new();
    List<DailyMission> semanais = new();
    int  ultimoSaldoDiamantes = -1;

    // (tipo, descrição com {0}=meta, meta, recompensa em diamantes)
    static readonly (MissionType tipo, string desc, int meta, int recompensa)[] poolDiarias =
    {
        ( MissionType.MatarInimigos,    "Matar {0} inimigos",                50,  10 ),
        ( MissionType.MatarInimigos,    "Matar {0} inimigos",               100,  20 ),
        ( MissionType.SobreviverTempo,  "Sobreviver {0}s numa partida",     300,  15 ),
        ( MissionType.JogarPartida,     "Jogar {0} partida",                  1,  10 ),
        ( MissionType.AlcancarZona,     "Alcançar a Zona {0}",                2,  20 ),
        ( MissionType.SobreviverWaves,  "Concluir {0} zonas",                 3,  15 ),
        ( MissionType.ColetarDiamantes, "Coletar {0} diamantes",             25,  20 ),
    };

    static readonly (MissionType tipo, string desc, int meta, int recompensa)[] poolSemanais =
    {
        ( MissionType.MatarInimigos,    "Matar {0} inimigos na semana",     500,  60 ),
        ( MissionType.JogarPartida,     "Jogar {0} partidas",                10,  50 ),
        ( MissionType.AlcancarZona,     "Alcançar a Zona {0}",                5, 100 ),
        ( MissionType.ColetarDiamantes, "Coletar {0} diamantes",            200,  70 ),
        ( MissionType.SobreviverTempo,  "Sobreviver {0}s numa partida",     600,  60 ),
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
        EnemyBase.OnEnemyDied              += AoInimigoMorrer;
        ZoneManager.OnZoneCompleted        += AoZonaConcluida;
        DiamondSystem.OnDiamondsChanged    += AoColetarDiamantes;
        RunRewardSystem.OnRunRewardCalculated += AoRunFinalizada;
    }

    void OnDisable()
    {
        if (Instance != this) return;
        EnemyBase.OnEnemyDied              -= AoInimigoMorrer;
        ZoneManager.OnZoneCompleted        -= AoZonaConcluida;
        DiamondSystem.OnDiamondsChanged    -= AoColetarDiamantes;
        RunRewardSystem.OnRunRewardCalculated -= AoRunFinalizada;
    }

    void Start()
    {
        CarregarOuGerar();
        ultimoSaldoDiamantes = DiamondSystem.Instance?.GetBalance() ?? -1;
    }

    // ── Fontes de progresso ───────────────────────────────────────────────────────

    void AoInimigoMorrer() => Progress(MissionType.MatarInimigos, 1);

    void AoZonaConcluida(int zona) => Progress(MissionType.SobreviverWaves, 1);

    void AoColetarDiamantes(int novoSaldo)
    {
        if (ultimoSaldoDiamantes < 0) { ultimoSaldoDiamantes = novoSaldo; return; }
        int delta = novoSaldo - ultimoSaldoDiamantes;
        if (delta > 0) Progress(MissionType.ColetarDiamantes, delta);
        ultimoSaldoDiamantes = novoSaldo;
    }

    void AoRunFinalizada(RunSummary summary)
    {
        Progress(MissionType.JogarPartida, 1);
        ProgressMax(MissionType.AlcancarZona,    summary.waveReached);
        ProgressMax(MissionType.SobreviverTempo, Mathf.FloorToInt(summary.timeSurvived));
    }

    // ── API pública ────────────────────────────────────────────────────────────────

    public List<DailyMission> GetDailyMissions()  => diarias;
    public List<DailyMission> GetWeeklyMissions() => semanais;

    public bool ClaimDaily(int i)  => Claim(diarias, i);
    public bool ClaimWeekly(int i) => Claim(semanais, i);

    // Tempo restante até o próximo reset (UTC).
    public TimeSpan TimeUntilDailyReset()
    {
        var now  = DateTime.UtcNow;
        var next = now.Date.AddDays(1);
        return next - now;
    }

    public TimeSpan TimeUntilWeeklyReset()
    {
        var now = DateTime.UtcNow;
        // próxima segunda-feira 00:00 UTC
        int diasParaSegunda = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (diasParaSegunda == 0) diasParaSegunda = 7;
        var next = now.Date.AddDays(diasParaSegunda);
        return next - now;
    }

    // ── Núcleo de progresso ──────────────────────────────────────────────────────

    void Progress(MissionType tipo, int delta)
    {
        bool mudou = false;
        foreach (var lista in new[] { diarias, semanais })
            foreach (var m in lista)
            {
                if (m.recompensaResgatada || m.tipo != tipo) continue;
                int antes = m.progresso;
                m.progresso = Mathf.Min(m.progresso + delta, m.meta);
                if (m.progresso != antes) mudou = true;
                MarcarConclusao(m);
            }
        if (mudou) { Salvar(); OnMissionsChanged?.Invoke(); }
    }

    void ProgressMax(MissionType tipo, int valor)
    {
        bool mudou = false;
        foreach (var lista in new[] { diarias, semanais })
            foreach (var m in lista)
            {
                if (m.recompensaResgatada || m.tipo != tipo) continue;
                int novo = Mathf.Min(Mathf.Max(m.progresso, valor), m.meta);
                if (novo != m.progresso) { m.progresso = novo; mudou = true; }
                MarcarConclusao(m);
            }
        if (mudou) { Salvar(); OnMissionsChanged?.Invoke(); }
    }

    void MarcarConclusao(DailyMission m)
    {
        if (m.ProgessoCompleto && !m.concluida)
        {
            m.concluida = true;
            Debug.Log($"[Missoes] Concluída: {m.descricao}");
            OnMissionCompleted?.Invoke(m);
        }
    }

    bool Claim(List<DailyMission> lista, int i)
    {
        if (i < 0 || i >= lista.Count) return false;
        var m = lista[i];
        if (!m.concluida || m.recompensaResgatada) return false;

        m.recompensaResgatada = true;
        DiamondSystem.Instance?.AddDiamonds(m.recompensaDiamantes);
        Salvar();
        OnMissionsChanged?.Invoke();
        Debug.Log($"[Missoes] Recompensa resgatada: +{m.recompensaDiamantes} 💎 ({m.descricao})");
        return true;
    }

    // ── Persistência / reset ────────────────────────────────────────────────────

    void CarregarOuGerar()
    {
        string hoje = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (PlayerPrefs.GetString(PREF_DATA_DIARIAS, "") == hoje)
            diarias = CarregarLista(PREF_DIARIAS_JSON);
        else
            diarias = GerarMissoes(poolDiarias, 3, hoje, PREF_DATA_DIARIAS, PREF_DIARIAS_JSON, "diária");

        string semana = ChaveSemana(DateTime.UtcNow);
        if (PlayerPrefs.GetString(PREF_ID_SEMANAIS, "") == semana)
            semanais = CarregarLista(PREF_SEMANAIS_JSON);
        else
            semanais = GerarMissoes(poolSemanais, 3, semana, PREF_ID_SEMANAIS, PREF_SEMANAIS_JSON, "semanal");

        OnMissionsChanged?.Invoke();
    }

    static string ChaveSemana(DateTime dt)
    {
        var cal  = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        int week = cal.GetWeekOfYear(dt, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{dt:yyyy}-W{week:00}";
    }

    List<DailyMission> GerarMissoes(
        (MissionType tipo, string desc, int meta, int recompensa)[] pool,
        int quantidade, string chave, string prefChave, string prefJson, string rotulo)
    {
        int semente = chave.GetHashCode();
        UnityEngine.Random.State estado = UnityEngine.Random.state;
        UnityEngine.Random.InitState(semente);

        var indices = new List<int>();
        int limite = Mathf.Min(quantidade, pool.Length);
        while (indices.Count < limite)
        {
            int idx = UnityEngine.Random.Range(0, pool.Length);
            if (!indices.Contains(idx)) indices.Add(idx);
        }
        UnityEngine.Random.state = estado;

        var nova = new List<DailyMission>();
        foreach (int idx in indices)
        {
            var (tipo, desc, meta, recompensa) = pool[idx];
            nova.Add(new DailyMission
            {
                id                  = $"{chave}_{tipo}_{meta}",
                tipo                = tipo,
                descricao           = string.Format(desc, meta),
                meta                = meta,
                recompensaDiamantes = recompensa,
            });
        }

        PlayerPrefs.SetString(prefChave, chave);
        SalvarLista(prefJson, nova);
        Debug.Log($"[Missoes] {nova.Count} missões {rotulo}(s) geradas para {chave}.");
        return nova;
    }

    void Salvar()
    {
        SalvarLista(PREF_DIARIAS_JSON,  diarias);
        SalvarLista(PREF_SEMANAIS_JSON, semanais);
    }

    static void SalvarLista(string chave, List<DailyMission> lista)
    {
        var wrapper = new MissaoListWrapper { missoes = lista };
        PlayerPrefs.SetString(chave, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    static List<DailyMission> CarregarLista(string chave)
    {
        string json = PlayerPrefs.GetString(chave, "");
        if (string.IsNullOrEmpty(json)) return new List<DailyMission>();
        var wrapper = JsonUtility.FromJson<MissaoListWrapper>(json);
        return wrapper?.missoes ?? new List<DailyMission>();
    }

    [System.Serializable]
    class MissaoListWrapper { public List<DailyMission> missoes = new(); }
}
