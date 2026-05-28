using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RunSessionData
{
    public bool         isActive;
    public int          currentWave;
    public int          killCount;
    public float        timeElapsed;
    public float        currentHealth;
    public float        maxHealth;
    public int          weaponLevel;
    public string       weaponType;
    public List<string> activePassiveItems;
    public List<string> activeUpgrades;
    public int          diamondsThisRun;
    public string       causeOfDeath;
}

// Persiste o estado de uma run em andamento para que possa ser restaurada após
// o app ser pausado ou fechado.
public class RunSessionManager : MonoBehaviour
{
    public static RunSessionManager Instance { get; private set; }

    const string PREF_KEY = "sol_run_session";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    public void SaveSession()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        var rd     = GameManager.Instance.currentRunData;
        var player = Object.FindFirstObjectByType<PlayerHealth>();
        var weapon = Object.FindFirstObjectByType<PlayerWeapon>();

        var session = new RunSessionData
        {
            isActive           = true,
            currentWave        = GameManager.Instance.waveManager != null
                                    ? GameManager.Instance.waveManager.CurrentWave
                                    : rd.waveReached,
            killCount          = rd.totalKills,
            timeElapsed        = GameManager.Instance.RunTimeSeconds,
            currentHealth      = player != null ? player.CurrentHealth : 0f,
            maxHealth          = player != null ? player.maxHealth     : 100f,
            weaponLevel        = weapon != null ? weapon.level         : 1,
            weaponType         = weapon != null ? weapon.GetType().Name : "",
            activePassiveItems = new List<string>(),
            activeUpgrades     = new List<string>(),
            diamondsThisRun    = 0,
            causeOfDeath       = rd.causeOfDeath ?? "inimigo",
        };

        PlayerPrefs.SetString(PREF_KEY, JsonUtility.ToJson(session));
        PlayerPrefs.Save();
        Debug.Log($"[RunSessionManager] Sessao salva — wave={session.currentWave} kills={session.killCount} hp={session.currentHealth:F0}");
    }

    public RunSessionData LoadSession()
    {
        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json)) return default;
        try   { return JsonUtility.FromJson<RunSessionData>(json); }
        catch { return default; }
    }

    public void ClearSession()
    {
        PlayerPrefs.DeleteKey(PREF_KEY);
        PlayerPrefs.Save();
        Debug.Log("[RunSessionManager] Sessao limpa.");
    }

    public bool HasActiveSession()
    {
        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json)) return false;
        try   { return JsonUtility.FromJson<RunSessionData>(json).isActive; }
        catch { return false; }
    }

    // ── Ciclo de vida do app ─────────────────────────────────────────────────────

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveSession();
    }

    void OnApplicationQuit()
    {
        SaveSession();
    }
}
