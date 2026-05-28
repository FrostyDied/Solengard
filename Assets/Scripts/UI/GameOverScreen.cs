using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Attach no GameOverCanvas (sempre ativo) — NÃO no painel (inativo).
// O painel inativo não executa Awake/OnEnable, impedindo a assinatura de eventos.
public class GameOverScreen : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] TextMeshProUGUI waveText;
    [SerializeField] TextMeshProUGUI killsText;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI causeText;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI diamondsText;

    [Header("Painel")]
    [SerializeField] GameObject panel;

    [Header("Botões")]
    [SerializeField] Button ressuscitarButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button menuButton;

    RunSummary cachedSummary;
    bool       summaryReceived;
    bool       reviverUsado;

    void Awake()
    {
        panel?.SetActive(false);
        if (ressuscitarButton == null) Debug.LogError("[GameOverScreen] ressuscitarButton é null!");
        if (restartButton     == null) Debug.LogError("[GameOverScreen] restartButton é null!");
        if (menuButton        == null) Debug.LogError("[GameOverScreen] menuButton é null!");
        ressuscitarButton?.onClick.AddListener(OnRessuscitarButton);
        restartButton?.onClick.AddListener(OnRestartButton);
        menuButton?.onClick.AddListener(OnMainMenuButton);
    }

    void OnEnable()
    {
        reviverUsado = false;
        ressuscitarButton?.gameObject.SetActive(true);
        GameManager.OnGameOver                += OnGameOver;
        RunRewardSystem.OnRunRewardCalculated += CacheSummary;
    }

    void OnDisable()
    {
        GameManager.OnGameOver                -= OnGameOver;
        RunRewardSystem.OnRunRewardCalculated -= CacheSummary;
    }

    void CacheSummary(RunSummary summary)
    {
        cachedSummary   = summary;
        summaryReceived = true;
    }

    void OnGameOver()
    {
        StopAllCoroutines(); // prevent multiple AnimateStats from a previous incomplete run
        summaryReceived = false;
        panel?.SetActive(true);
        StartCoroutine(AnimateStats());
    }

    IEnumerator AnimateStats()
    {
        // Aguarda até 1s para o resumo chegar via RunRewardSystem.OnRunRewardCalculated
        float waited = 0f;
        while (!summaryReceived && waited < 1f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        TextMeshProUGUI[] stats = { waveText, killsText, timeText, causeText, scoreText, diamondsText };
        foreach (var t in stats)
            if (t != null) t.enabled = false;

        if (summaryReceived)
        {
            RunSummary s = cachedSummary;
            if (waveText     != null) waveText.text     = $"Wave {s.waveReached}";
            if (killsText    != null) killsText.text    = $"Kills: {s.totalKills}";
            if (timeText     != null) timeText.text     = $"Tempo: {FormatTime(s.timeSurvived)}";
            if (causeText    != null) causeText.text    = GetCauseFlavorText(s.lastDeathCause);
            if (scoreText    != null) scoreText.text    = $"Score: {s.score}";
            if (diamondsText != null) diamondsText.text = $"+{s.diamondsEarned} diamantes";
        }
        else if (GameManager.Instance != null)
        {
            // Fallback: RunRewardSystem não entregou summary — usa dados brutos do GameManager
            var rd = GameManager.Instance.currentRunData;
            if (waveText     != null) waveText.text     = $"Wave {rd.waveReached}";
            if (killsText    != null) killsText.text    = $"Kills: {rd.totalKills}";
            if (timeText     != null) timeText.text     = $"Tempo: {FormatTime(rd.timeSurvived)}";
            if (causeText    != null) causeText.text    = $"Causa: {rd.causeOfDeath}";
            if (scoreText    != null) scoreText.text    = "Score: —";
            if (diamondsText != null) diamondsText.text = "+0 diamantes";
        }
        else yield break;

        var delay = new WaitForSecondsRealtime(0.3f);
        foreach (var t in stats)
        {
            if (t != null) t.enabled = true;
            yield return delay;
        }
    }

    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    public void OnRessuscitarButton()
    {
        Debug.Log("[GameOverScreen] OnRessuscitarButton chamado");
        if (reviverUsado)
        {
            ressuscitarButton?.gameObject.SetActive(false);
            return;
        }
        if (AdSystem.Instance == null || !AdSystem.Instance.IsAdAvailable())
        {
            Debug.LogWarning("[GameOverScreen] AdSystem não disponível — revive bloqueado.");
            return;
        }
        AdSystem.Instance.ShowRewardedAd(RevivePlayer);
    }

    void RevivePlayer()
    {
        var ph = Object.FindFirstObjectByType<PlayerHealth>();
        if (ph == null)
        {
            Debug.LogError("[GameOverScreen] PlayerHealth não encontrado para reviver.");
            return;
        }
        ph.Revive(0.5f);
        panel?.SetActive(false);
        Time.timeScale = 1f;
        GameManager.Instance?.SetStatePlaying();
        reviverUsado = true;
        ressuscitarButton?.gameObject.SetActive(false);
        Debug.Log("[GameOverScreen] Player revivido com sucesso.");
    }

    public void OnRestartButton()
    {
        Debug.Log("[GameOverScreen] OnRestartButton chamado");
        Time.timeScale = 1f;
        GameManager.Instance?.RestartRun();
    }

    public void OnMainMenuButton()
    {
        Debug.Log("[GameOverScreen] OnMainMenuButton chamado");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    string GetCauseFlavorText(DeathCause cause) => cause switch
    {
        DeathCause.TempoEsgotado => "Consumido pela fúria sombria",
        DeathCause.Boss          => "Derrotado pelo Boss das Sombras",
        DeathCause.Veneno        => "Sucumbiu ao veneno",
        DeathCause.Sangramento   => "Esvaiu-se em sangue",
        _                        => "Abatido pelos inimigos",
    };
}
