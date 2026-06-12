using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LastRunData
{
    public int   wave;
    public int   kills;
    public float time;
    public int   score;
}

[System.Serializable]
public class RunSummary
{
    public int        waveReached;
    public int        totalKills;
    public float      timeSurvived;
    public string     causeOfDeath;
    public DeathCause lastDeathCause;
    public int        score;
    public int        diamondsEarned;
}

public class RunRewardSystem : MonoBehaviour
{
    public static event System.Action<RunSummary> OnRunRewardCalculated;

    const string PREF_KEY = "sol_run_history";

    [SerializeField] WaveTimerSystem waveTimerSystem;

    void OnEnable()
    {
        if (waveTimerSystem == null)
            waveTimerSystem = Object.FindFirstObjectByType<WaveTimerSystem>();
    }

    public RunSummary CalculateAndDeliverReward(RunData runData)
    {
        Debug.Log($"[RunReward] INICIO kills={runData.totalKills} wave={runData.waveReached}");

        if (waveTimerSystem == null)
            waveTimerSystem = Object.FindFirstObjectByType<WaveTimerSystem>();
        var scoreSystem = Object.FindFirstObjectByType<ScoreSystem>();
        Debug.Log($"[RunReward] ScoreSystem found={scoreSystem != null} score={scoreSystem?.ScoreAtual}");

        int score;
        if (scoreSystem != null)
        {
            score = scoreSystem.ScoreAtual;
            Debug.Log($"[RunReward] Calculando: kills={runData.totalKills} wave={runData.waveReached} score={score} (via ScoreSystem)");
        }
        else
        {
            float timeBonus = waveTimerSystem != null ? waveTimerSystem.TimeRemaining / 10f : 0f;
            score = (runData.waveReached * 10) + (runData.totalKills * 2) + Mathf.FloorToInt(timeBonus);
            Debug.Log($"[RunReward] Calculando: kills={runData.totalKills} wave={runData.waveReached} score={score} (ScoreSystem nao encontrado, calculo proprio)");
        }

        var summary = new RunSummary
        {
            waveReached    = runData.waveReached,
            totalKills     = runData.totalKills,
            timeSurvived   = runData.timeSurvived,
            causeOfDeath   = runData.causeOfDeath,
            lastDeathCause = runData.lastDeathCause,
            score          = score,
            diamondsEarned = Mathf.Max(1, Mathf.RoundToInt((score / 10f) * (PermanentUpgradeSystem.Instance?.DiamondBonus ?? 1f)))
        };

        Debug.Log($"[RunReward] Diamonds a entregar={summary.diamondsEarned}");
        DiamondSystem.Instance?.AddDiamonds(summary.diamondsEarned);
        Debug.Log("[RunReward] AddDiamonds chamado");

        Debug.Log($"[RunReward] Salvando score={summary.score}");
        SaveRunHistory(summary);
        Debug.Log($"[RunReward] Invocando OnRunRewardCalculated subscribers={OnRunRewardCalculated?.GetInvocationList()?.Length}");
        OnRunRewardCalculated?.Invoke(summary);

        Debug.Log($"[RunRewardSystem] Run finalizada — score: {score}, diamantes: {summary.diamondsEarned}");
        return summary;
    }

    void SaveRunHistory(RunSummary summary)
    {
        string json    = PlayerPrefs.GetString(PREF_KEY, "{\"runs\":[]}");
        RunHistoryWrapper history;

        try   { history = JsonUtility.FromJson<RunHistoryWrapper>(json); }
        catch { history = new RunHistoryWrapper(); }

        if (history.runs == null)
            history.runs = new List<RunSummary>();

        history.runs.Add(summary);

        if (history.runs.Count > 20)
            history.runs.RemoveAt(0);

        PlayerPrefs.SetString(PREF_KEY, JsonUtility.ToJson(history));

        int bestScore = PlayerPrefs.GetInt("sol_best_score", 0);
        if (summary.score > bestScore)
            PlayerPrefs.SetInt("sol_best_score", summary.score);

        var lastRun = new LastRunData
        {
            wave  = summary.waveReached,
            kills = summary.totalKills,
            time  = summary.timeSurvived,
            score = summary.score,
        };
        PlayerPrefs.SetString("sol_last_run", JsonUtility.ToJson(lastRun));

        PlayerPrefs.Save();
    }

    [System.Serializable]
    class RunHistoryWrapper
    {
        public List<RunSummary> runs = new List<RunSummary>();
    }
}
