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

        var player = Object.FindFirstObjectByType<PlayerHealth>();
        // Nunca persiste um "cadáver": se o player ainda não spawnou (null) ou está
        // morto/zerado, pula o save em vez de gravar HP 0 (evita sessão corrompida).
        if (player == null || player.CurrentHealth <= 0f)
        {
            Debug.LogWarning("[RunSessionManager] SaveSession ignorado — player nulo ou HP<=0");
            return;
        }

        var rd     = GameManager.Instance.currentRunData;
        var weapon = Object.FindFirstObjectByType<PlayerWeapon>();

        var session = new RunSessionData
        {
            isActive           = true,
            currentWave        = ZoneManager.Instance != null
                                    ? ZoneManager.Instance.CurrentZone + 1
                                    : rd.waveReached,
            killCount          = rd.totalKills,
            timeElapsed        = GameManager.Instance.RunTimeSeconds,
            currentHealth      = player.CurrentHealth,   // garantido != null e > 0
            maxHealth          = player.maxHealth,
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

    public RunSessionData? GetSession()
    {
        if (!HasActiveSession()) return null;
        var session = LoadSession();
        if (session.currentWave < 0 || session.currentWave > 5)
        {
            Debug.LogWarning($"[Session] Sessão com wave inválida ({session.currentWave}) — descartada");
            ClearSession();
            return null;
        }
        return session;
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
