using System.Collections.Generic;
using UnityEngine;

// Calcula e persiste o score de cada run.
// Fórmula: (kills × 10) + (waves × 100) + (segundos × 2) + bonus de dificuldade.
public class ScoreSystem : MonoBehaviour
{
    public static event System.Action<int> OnNewHighScore;

    const string PREF_SCORES = "sol_scores";
    const int    MAX_SCORES  = 10;

    [Header("Dados da run atual (atualizados em tempo real)")]
    public int   inimigosMortos;
    public int   wavesCompletadas;
    public float tempoSobrevivido;
    public int   bonusDificuldade;

    int scoreAtual;
    public int ScoreAtual => scoreAtual;

    // ── Contadores em tempo real ─────────────────────────────────────────────────

    void OnEnable()
    {
        GameManager.OnGameStateChanged += AoMudarEstado;
        EnemyBase.OnEnemyDied          += RegistrarKill;
        WaveManager.OnWaveCompleted    += AoWaveConcluida;
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= AoMudarEstado;
        EnemyBase.OnEnemyDied          -= RegistrarKill;
        WaveManager.OnWaveCompleted    -= AoWaveConcluida;
    }

    void AoWaveConcluida(int wave) => RegistrarWaveConcluida();

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            tempoSobrevivido += Time.deltaTime;
            scoreAtual = CalcularScore();
        }
    }

    public void RegistrarKill() => inimigosMortos++;
    public void RegistrarWaveConcluida() => wavesCompletadas++;

    // ── Cálculo e persistência ──────────────────────────────────────────────────

    public int CalculateRunScore()
    {
        scoreAtual = CalcularScore();
        Debug.Log($"[ScoreSystem] Score final: {scoreAtual}");

        List<int> ranking = CarregarScores();
        bool novoRecord = ranking.Count == 0 || scoreAtual > ranking[0];

        ranking.Add(scoreAtual);
        ranking.Sort((a, b) => b.CompareTo(a));
        if (ranking.Count > MAX_SCORES) ranking.RemoveRange(MAX_SCORES, ranking.Count - MAX_SCORES);
        SalvarScores(ranking);

        if (novoRecord)
        {
            Debug.Log($"[ScoreSystem] Novo recorde: {scoreAtual}!");
            OnNewHighScore?.Invoke(scoreAtual);
            // TODO: sincronizar melhor score com Supabase leaderboard
        }

        return scoreAtual;
    }

    public List<int> GetLeaderboard() => CarregarScores();

    int CalcularScore()
    {
        return (inimigosMortos * 10)
             + (wavesCompletadas * 100)
             + (Mathf.FloorToInt(tempoSobrevivido) * 2)
             + bonusDificuldade;
    }

    void ResetarRun()
    {
        inimigosMortos   = 0;
        wavesCompletadas = 0;
        tempoSobrevivido = 0f;
        bonusDificuldade = 0;
        scoreAtual       = 0;
    }

    void AoMudarEstado(GameState estado)
    {
        if (estado == GameState.GameOver || estado == GameState.Victory)
            CalculateRunScore();

        if (estado == GameState.Playing)
            ResetarRun();
    }

    // ── JSON em PlayerPrefs ─────────────────────────────────────────────────────

    List<int> CarregarScores()
    {
        string json = PlayerPrefs.GetString(PREF_SCORES, "");
        if (string.IsNullOrEmpty(json)) return new List<int>();
        ScoreWrapper w = JsonUtility.FromJson<ScoreWrapper>(json);
        return w?.scores ?? new List<int>();
    }

    void SalvarScores(List<int> scores)
    {
        string json = JsonUtility.ToJson(new ScoreWrapper { scores = scores });
        PlayerPrefs.SetString(PREF_SCORES, json);
        PlayerPrefs.Save();
    }

    [System.Serializable]
    class ScoreWrapper { public List<int> scores = new(); }
}
