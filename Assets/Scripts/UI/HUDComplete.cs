using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD completa do gameplay. Substitui ou complementa o HUDManager básico.
// Attach em um GameObject "HUDComplete" na cena de jogo.
public class HUDComplete : MonoBehaviour
{
    [Header("Vida")]
    public Slider              barraVida;
    public TextMeshProUGUI     textoVida;

    [Header("Wave")]
    public TextMeshProUGUI     textoWave;
    public GameObject          bannerWave;
    public TextMeshProUGUI     textoBannerWave;

    [Header("Diamantes e Score")]
    public TextMeshProUGUI     textoDiamantes;
    public TextMeshProUGUI     textoScore;

    [Header("Missão ativa")]
    public GameObject          painelMissao;
    public TextMeshProUGUI     textoMissao;
    public TextMeshProUGUI     textoProgressoMissao;

    [Header("Timer")]
    public TextMeshProUGUI     textoTimer;

    [Header("Botão Pause")]
    public Button              botaoPause;

    [Header("Painel de Pausa")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] Button     botaoRetomar;
    [SerializeField] Button     botaoMenuPrincipalPause;

    int ultimaWaveExibida = -1;
    ScoreSystem scoreSystem;

    void OnEnable()
    {
        PlayerHealth.OnHealthChanged          += AtualizarVida;
        DiamondSystem.OnDiamondsChanged       += AtualizarDiamantes;
        GameManager.OnGameStateChanged        += AoMudarEstado;
        DailyMissionSystem.OnMissionCompleted += AoCompletarMissao;
        WaveTimerSystem.OnTimerTick           += AtualizarTimer;
    }

    void OnDisable()
    {
        PlayerHealth.OnHealthChanged          -= AtualizarVida;
        DiamondSystem.OnDiamondsChanged       -= AtualizarDiamantes;
        GameManager.OnGameStateChanged        -= AoMudarEstado;
        DailyMissionSystem.OnMissionCompleted -= AoCompletarMissao;
        WaveTimerSystem.OnTimerTick           -= AtualizarTimer;
    }

    void Start()
    {
        botaoPause?.onClick.AddListener(PausarJogo);
        botaoRetomar?.onClick.AddListener(RetormarJogo);
        botaoMenuPrincipalPause?.onClick.AddListener(IrParaMenuPrincipal);
        bannerWave?.SetActive(false);
        painelMissao?.SetActive(false);
        pausePanel?.SetActive(false);

        scoreSystem = FindFirstObjectByType<ScoreSystem>();
        AtualizarDiamantes(DiamondSystem.Instance?.GetBalance() ?? 0);
    }

    void Update()
    {
        AtualizarWaveSeMudou();
        AtualizarScore();
    }

    // ── Handlers de eventos ──────────────────────────────────────────────────────

    void AtualizarVida(float atual, float maxima)
    {
        if (barraVida != null) barraVida.value = atual / maxima;
        if (textoVida != null) textoVida.text  = $"{Mathf.CeilToInt(atual)}/{Mathf.CeilToInt(maxima)}";
    }

    void AtualizarDiamantes(int saldo)
    {
        if (textoDiamantes != null) textoDiamantes.text = saldo.ToString("N0");
    }

    void AtualizarTimer(float t)
    {
        if (textoTimer == null) return;
        textoTimer.text  = string.Format("{0}:{1:00}", (int)t / 60, (int)t % 60);
        textoTimer.color = t <= 10f ? Color.red : Color.white;
    }

    void AoMudarEstado(GameState estado)
    {
        if (estado == GameState.Playing)  { ultimaWaveExibida = -1; pausePanel?.SetActive(false); }
        if (estado == GameState.GameOver) pausePanel?.SetActive(false);
    }

    void AoCompletarMissao(DailyMission missao)
    {
        if (textoMissao != null)    textoMissao.text = $"Missão completa: {missao.descricao}";
        painelMissao?.SetActive(true);
        StartCoroutine(OcultarMissaoApos(3f));
    }

    // ── Atualização por polling ──────────────────────────────────────────────────

    void AtualizarWaveSeMudou()
    {
        WaveManager wm = GameManager.Instance?.waveManager;
        if (wm == null || wm.CurrentWave == ultimaWaveExibida) return;

        ultimaWaveExibida = wm.CurrentWave;
        if (textoWave != null) textoWave.text = $"Wave {wm.CurrentWave}/{wm.TotalWaves}";
        ShowWaveStartBanner(wm.CurrentWave);
    }

    void AtualizarScore()
    {
        if (scoreSystem != null && textoScore != null)
            textoScore.text = scoreSystem.ScoreAtual.ToString("N0");
    }

    // ── Banner de início de wave ─────────────────────────────────────────────────

    public void ShowWaveStartBanner(int numeroDaWave)
    {
        if (bannerWave == null) return;
        if (textoBannerWave != null) textoBannerWave.text = $"WAVE {numeroDaWave}";
        StartCoroutine(AnimarBanner());
    }

    IEnumerator AnimarBanner()
    {
        bannerWave.SetActive(true);
        yield return new WaitForSeconds(2f);
        bannerWave.SetActive(false);
    }

    IEnumerator OcultarMissaoApos(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        painelMissao?.SetActive(false);
    }

    void PausarJogo()
    {
        GameManager.Instance?.PauseGame();
        pausePanel?.SetActive(true);
    }

    void RetormarJogo()
    {
        GameManager.Instance?.ResumeGame();
        pausePanel?.SetActive(false);
    }

    void IrParaMenuPrincipal()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
