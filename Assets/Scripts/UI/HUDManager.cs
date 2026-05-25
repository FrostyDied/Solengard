using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Gerencia toda a interface do gameplay do Solengard.
// Attach em um GameObject "HUD" na cena de jogo e configure as referências no Inspector.
public class HUDManager : MonoBehaviour
{
    // ── Referências de UI (configurar no Inspector) ─────────────────────────────

    [Header("Vida")]
    public Slider healthSlider;

    [Header("Wave")]
    public TextMeshProUGUI waveText;

    [Header("Painéis")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    // ── Estado interno ──────────────────────────────────────────────────────────

    // Número da última wave exibida; -1 força atualização imediata ao entrar em Playing
    int ultimaWaveExibida = -1;

    // ── Unity ───────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        PlayerHealth.OnHealthChanged   += AtualizarBarraDeVida;
        GameManager.OnGameStateChanged += AoMudarEstado;
        GameManager.OnGameOver         += AoGameOver;
    }

    void OnDisable()
    {
        // Remove assinaturas para evitar referências mortas ao descarregar a cena
        PlayerHealth.OnHealthChanged   -= AtualizarBarraDeVida;
        GameManager.OnGameStateChanged -= AoMudarEstado;
        GameManager.OnGameOver         -= AoGameOver;
    }

    void Start()
    {
        // Garante estado visual limpo ao entrar na cena
        gameOverPanel?.SetActive(false);
        victoryPanel?.SetActive(false);

        // Inicializa a barra de vida cheia
        if (healthSlider != null)
            healthSlider.value = 1f;
    }

    void Update()
    {
        // WaveManager não expõe evento de mudança de wave; polling leve com cache
        AtualizarTextoWaveSeMudou();
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    // Atualiza o texto de wave; pode ser chamado por sistemas externos no futuro
    public void UpdateWaveDisplay(int atual, int total)
    {
        if (waveText == null) return;

        waveText.text = $"Wave {atual}/{total}";
    }

    // ── Handlers de eventos ─────────────────────────────────────────────────────

    // Recebe vida atual e máxima de PlayerHealth.OnHealthChanged
    void AtualizarBarraDeVida(float vidaAtual, float vidaMaxima)
    {
        if (healthSlider == null) return;

        healthSlider.value = vidaAtual / vidaMaxima;
    }

    void AoMudarEstado(GameState novoEstado)
    {
        switch (novoEstado)
        {
            case GameState.Playing:
                // Reseta cache para forçar exibição imediata da wave atual
                ultimaWaveExibida = -1;

                gameOverPanel?.SetActive(false);
                victoryPanel?.SetActive(false);
                break;

            case GameState.Victory:
                victoryPanel?.SetActive(true);
                break;

            case GameState.Paused:
                // Painel de pausa pode ser integrado aqui no futuro
                break;

            case GameState.GameOver:
                // Tratado por AoGameOver (evento dedicado do GameManager)
                break;
        }
    }

    void AoGameOver()
    {
        gameOverPanel?.SetActive(true);
    }

    // ── Atualização de wave via polling ─────────────────────────────────────────

    // Consulta o WaveManager a cada frame, mas só redesenha o texto quando a wave muda.
    // Evita criar dependência de evento no WaveManager sem precisar modificá-lo.
    void AtualizarTextoWaveSeMudou()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        WaveManager wm = GameManager.Instance.waveManager;
        if (wm == null) return;

        if (wm.CurrentWave == ultimaWaveExibida) return;

        ultimaWaveExibida = wm.CurrentWave;
        UpdateWaveDisplay(wm.CurrentWave, wm.TotalWaves);
    }
}
