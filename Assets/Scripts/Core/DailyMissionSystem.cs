using System.Collections.Generic;
using UnityEngine;

public enum MissionType { MatarInimigos, SobreviverWaves, ColetarDiamantes, VencerSemTomarDano }

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

// 3 missões diárias renovadas à meia-noite (UTC). Progresso salvo por data em PlayerPrefs.
public class DailyMissionSystem : MonoBehaviour
{
    public static event System.Action<DailyMission> OnMissionCompleted;

    const string PREF_DATA_MISSOES = "sol_mission_date";
    const string PREF_MISSOES_JSON = "sol_missions";

    List<DailyMission> missoesHoje = new();

    // Definições do pool de missões com variantes de dificuldade
    static readonly (MissionType tipo, string desc, int meta, int recompensa)[] poolMissoes =
    {
        ( MissionType.MatarInimigos,       "Matar {0} inimigos",          50,  10 ),
        ( MissionType.MatarInimigos,       "Matar {0} inimigos",         100,  20 ),
        ( MissionType.MatarInimigos,       "Matar {0} inimigos",         200,  30 ),
        ( MissionType.SobreviverWaves,     "Sobreviver {0} waves",          3,  10 ),
        ( MissionType.SobreviverWaves,     "Sobreviver {0} waves",          5,  20 ),
        ( MissionType.SobreviverWaves,     "Sobreviver {0} waves",         10,  30 ),
        ( MissionType.ColetarDiamantes,    "Coletar {0} diamantes",        10,  15 ),
        ( MissionType.ColetarDiamantes,    "Coletar {0} diamantes",        25,  20 ),
        ( MissionType.ColetarDiamantes,    "Coletar {0} diamantes",        50,  25 ),
        ( MissionType.VencerSemTomarDano,  "Vencer {0} wave sem tomar dano", 1, 30 ),
    };

    void OnEnable()
    {
        EnemyBase.OnEnemyDied       += AoInimigoMorrer;
        WaveManager.OnWaveCompleted += AoWaveConcluida;
    }

    void OnDisable()
    {
        EnemyBase.OnEnemyDied       -= AoInimigoMorrer;
        WaveManager.OnWaveCompleted -= AoWaveConcluida;
    }

    void AoInimigoMorrer()      => UpdateMissionProgress(nameof(MissionType.MatarInimigos),   1);
    void AoWaveConcluida(int w) => UpdateMissionProgress(nameof(MissionType.SobreviverWaves), 1);

    void Start() => CarregarOuGerarMissoes();

    // Retorna as missões do dia atual
    public List<DailyMission> GetTodayMissions() => missoesHoje;

    // Atualiza o progresso de todas as missões do tipo informado
    public void UpdateMissionProgress(string missionType, int quantidade)
    {
        if (!System.Enum.TryParse(missionType, out MissionType tipo)) return;

        foreach (DailyMission missao in missoesHoje)
        {
            if (missao.concluida || missao.tipo != tipo) continue;

            missao.progresso = Mathf.Min(missao.progresso + quantidade, missao.meta);

            if (missao.ProgessoCompleto && !missao.concluida)
            {
                missao.concluida = true;
                Debug.Log($"[DailyMissionSystem] Missão concluída: {missao.descricao}");
                OnMissionCompleted?.Invoke(missao);
            }
        }
        SalvarProgresso();
    }

    // Reivindica recompensa da missão pelo índice (0-2)
    public bool ClaimMissionReward(int indice)
    {
        if (indice < 0 || indice >= missoesHoje.Count) return false;

        DailyMission missao = missoesHoje[indice];
        if (!missao.concluida || missao.recompensaResgatada) return false;

        missao.recompensaResgatada = true;
        DiamondSystem.Instance?.AddDiamonds(missao.recompensaDiamantes);
        SalvarProgresso();

        Debug.Log($"[DailyMissionSystem] Recompensa resgatada: +{missao.recompensaDiamantes} diamantes");
        return true;
    }

    // ── Persistência ────────────────────────────────────────────────────────────

    void CarregarOuGerarMissoes()
    {
        string hoje        = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        string datasSalvas = PlayerPrefs.GetString(PREF_DATA_MISSOES, "");

        if (datasSalvas == hoje)
        {
            CarregarProgresso();
        }
        else
        {
            GerarMissoesDodia(hoje);
        }
    }

    void GerarMissoesDodia(string hoje)
    {
        // Usa data como semente para gerar as mesmas missões em dispositivos diferentes
        int semente = hoje.GetHashCode();
        Random.State estadoOriginal = Random.state;
        Random.InitState(semente);

        List<int> indices = new();
        while (indices.Count < 3)
        {
            int idx = Random.Range(0, poolMissoes.Length);
            if (!indices.Contains(idx)) indices.Add(idx);
        }

        Random.state = estadoOriginal;

        missoesHoje.Clear();
        foreach (int idx in indices)
        {
            var (tipo, desc, meta, recompensa) = poolMissoes[idx];
            missoesHoje.Add(new DailyMission
            {
                id                  = $"{hoje}_{tipo}_{meta}",
                tipo                = tipo,
                descricao           = string.Format(desc, meta),
                meta                = meta,
                recompensaDiamantes = recompensa,
            });
        }

        PlayerPrefs.SetString(PREF_DATA_MISSOES, hoje);
        SalvarProgresso();
        Debug.Log($"[DailyMissionSystem] 3 novas missões geradas para {hoje}.");
    }

    void SalvarProgresso()
    {
        // Serializa a lista manualmente porque JsonUtility não serializa List<T> diretamente
        MissaoListWrapper wrapper = new() { missoes = missoesHoje };
        PlayerPrefs.SetString(PREF_MISSOES_JSON, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    void CarregarProgresso()
    {
        string json = PlayerPrefs.GetString(PREF_MISSOES_JSON, "");
        if (string.IsNullOrEmpty(json)) return;

        MissaoListWrapper wrapper = JsonUtility.FromJson<MissaoListWrapper>(json);
        if (wrapper?.missoes != null) missoesHoje = wrapper.missoes;
    }

    [System.Serializable]
    class MissaoListWrapper { public List<DailyMission> missoes = new(); }
}
