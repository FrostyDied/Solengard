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
    [SerializeField] Button restartButton;
    [SerializeField] Button menuButton;

    RunSummary cachedSummary;
    bool       summaryReceived;

    void Awake()
    {
        panel?.SetActive(false);
        restartButton?.onClick.AddListener(OnRestartButton);
        menuButton?.onClick.AddListener(OnMainMenuButton);
    }

    void OnEnable()
    {
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
            if (causeText    != null) causeText.text    = $"Causa: {s.causeOfDeath}";
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

    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.RestartRun();
    }

    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
