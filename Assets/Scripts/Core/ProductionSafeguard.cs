using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(-1000)]
public class ProductionSafeguard : MonoBehaviour
{
    public static ProductionSafeguard Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        RunStartupChecks();
    }

    void RunStartupChecks()
    {
        Debug.Log("[Safeguard] Iniciando verificações de produção...");

        if (Time.timeScale != 1f)
        {
            Debug.LogWarning($"[Safeguard] timeScale estava {Time.timeScale} — corrigido para 1");
            Time.timeScale = 1f;
        }

        ValidateSession();
        ValidatePlayerPrefs();
        StartCoroutine(VerifySpawnStarted());
    }

    IEnumerator VerifySpawnStarted()
    {
        yield return new WaitForSecondsRealtime(5f);
        var zm = ZoneManager.Instance;
        if (zm == null || GameManager.Instance == null) yield break;
        if (GameManager.Instance.IsPlaying && !zm.IsRunning)
        {
            Debug.LogError("[Safeguard] ZoneManager não está rodando após 5s — forçando StartZones");
            zm.StartZones();
        }
    }

    void ValidateSession()
    {
        var sm = RunSessionManager.Instance;
        if (sm == null) return;

        var session = sm.GetSession();
        if (!session.HasValue) return;

        var s      = session.Value;
        bool invalid = false;
        string reason = "";

        if (s.currentWave < 1 || s.currentWave > 5)
        {
            invalid = true;
            reason  = $"wave inválida ({s.currentWave})";
        }
        else if (s.currentHealth <= 0 || s.currentHealth > 10000)
        {
            invalid = true;
            reason  = $"HP inválido ({s.currentHealth})";
        }
        else if (s.killCount < 0)
        {
            invalid = true;
            reason  = $"kills inválido ({s.killCount})";
        }

        if (invalid)
        {
            Debug.LogWarning($"[Safeguard] Sessão corrompida ({reason}) — descartada");
            sm.ClearSession();
        }
    }

    void ValidatePlayerPrefs()
    {
        int diamonds = PlayerPrefs.GetInt("diamonds", 0);
        if (diamonds < 0 || diamonds > 999999999)
        {
            Debug.LogWarning($"[Safeguard] Diamantes inválido ({diamonds}) — corrigido");
            PlayerPrefs.SetInt("diamonds", Mathf.Clamp(diamonds, 0, 999999999));
        }

        int gold = PlayerPrefs.GetInt("gold", 0);
        if (gold < 0 || gold > 999999999)
            PlayerPrefs.SetInt("gold", Mathf.Clamp(gold, 0, 999999999));

        PlayerPrefs.Save();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            RunSessionManager.Instance?.SaveSession();
            Debug.Log("[Safeguard] App pausado — sessão salva");
        }
    }

    void OnApplicationFocus(bool focused)
    {
        if (focused && GameManager.Instance != null &&
            GameManager.Instance.IsPlaying && Time.timeScale == 0f)
        {
            StartCoroutine(RecoverTimeScale());
        }
    }

    IEnumerator RecoverTimeScale()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (Time.timeScale == 0f && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            Debug.LogWarning("[Safeguard] timeScale travado em 0 após retomar — recuperado");
            Time.timeScale = 1f;
        }
    }
}
