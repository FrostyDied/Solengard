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

    [Header("Botão Pause")]
    public Button              botaoPause;

    int ultimaWaveExibida = -1;

    void OnEnable()
    {
        PlayerHealth.OnHealthChanged     += AtualizarVida;
        DiamondSystem.OnDiamondsChanged  += AtualizarDiamantes;
        GameManager.OnGameStateChanged   += AoMudarEstado;
        DailyMissionSystem.OnMissionCompleted += AoCompletarMissao;
    }

    void OnDisable()
    {
        PlayerHealth.OnHealthChanged     -= AtualizarVida;
        DiamondSystem.OnDiamondsChanged  -= AtualizarDiamantes;
        GameManager.OnGameStateChanged   -= AoMudarEstado;
        DailyMissionSystem.OnMissionCompleted -= AoCompletarMissao;
    }

    void Start()
    {
        botaoPause?.onClick.AddListener(PausarJogo);
        bannerWave?.SetActive(false);
        painelMissao?.SetActive(false);

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

    void AoMudarEstado(GameState estado)
    {
        if (estado == GameState.Playing) ultimaWaveExibida = -1;
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
        ScoreSystem sc = FindFirstObjectByType<ScoreSystem>();
        if (sc != null && textoScore != null)
            textoScore.text = sc.ScoreAtual.ToString("N0");
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

    void PausarJogo() => GameManager.Instance?.PauseGame();
}
