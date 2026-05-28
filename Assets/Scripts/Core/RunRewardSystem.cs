using System.Collections.Generic;
using UnityEngine;

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

    public RunSummary CalculateAndDeliverReward(RunData runData)
    {
        float timeBonus    = waveTimerSystem != null ? waveTimerSystem.TimeRemaining / 10f : 0f;
        int   score        = (runData.wavesCompleted * 10) + (runData.totalKills * 2) + Mathf.FloorToInt(timeBonus);

        var summary = new RunSummary
        {
            waveReached    = runData.waveReached,
            totalKills     = runData.totalKills,
            timeSurvived   = runData.timeSurvived,
            causeOfDeath   = runData.causeOfDeath,
            lastDeathCause = runData.lastDeathCause,
            score          = score,
            diamondsEarned = score
        };

        DiamondSystem.Instance?.AddDiamonds(summary.diamondsEarned);
        SaveRunHistory(summary);
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
        PlayerPrefs.Save();
    }

    [System.Serializable]
    class RunHistoryWrapper
    {
        public List<RunSummary> runs = new List<RunSummary>();
    }
}
