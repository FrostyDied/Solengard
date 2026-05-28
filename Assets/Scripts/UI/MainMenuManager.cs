using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Gerencia o menu principal do Solengard.
// Attach em um GameObject "MainMenuManager" na cena MainMenu.
public class MainMenuManager : MonoBehaviour
{
    [Header("Botões principais")]
    public Button botaoJogar;
    public Button botaoLoja;
    public Button botaoPasse;
    public Button botaoMissoes;
    public Button botaoRanking;
    public Button botaoConfiguracoes;

    [Header("Atalhos laterais (LeftPanel)")]
    [SerializeField] Button botaoOfertas;
    [SerializeField] Button botaoBencaos;
    [SerializeField] Button botaoBaus;

    [Header("Informações do jogador")]
    public TextMeshProUGUI textoDiamantes;
    public TextMeshProUGUI textoNivelPasse;
    public TextMeshProUGUI textoStreakLogin;

    [Header("Popup de recompensa diária")]
    public GameObject      popupRecompensa;
    public TextMeshProUGUI textoRecompensaDia;
    public TextMeshProUGUI textoRecompensaDiamantes;
    public Button          botaoColetarRecompensa;

    [Header("Painéis (abertos pelos botões)")]
    public GameObject painelLoja;
    public GameObject painelPasse;
    public GameObject painelMissoes;
    public GameObject painelRanking;
    public GameObject painelConfiguracoes;

    [Header("Painéis laterais")]
    [SerializeField] GameObject painelOfertas;
    [SerializeField] GameObject painelBencaos;
    [SerializeField] GameObject painelBaus;

    [Header("Nome da cena de jogo")]
    public string nomeGameScene = "GameScene";

    // Armazena o dia/diamantes para uso no botão coletar
    int diaRecompensaAtual;
    int diamantesRecompensaAtual;

    void OnEnable()
    {
        DailyRewardSystem.OnDailyRewardAvailable += ExibirPopupRecompensa;
        DiamondSystem.OnDiamondsChanged          += AtualizarDiamantes;
    }

    void OnDisable()
    {
        DailyRewardSystem.OnDailyRewardAvailable -= ExibirPopupRecompensa;
        DiamondSystem.OnDiamondsChanged          -= AtualizarDiamantes;
    }

    void Start()
    {
        ConfigurarBotoes();
        AtualizarInfosJogador();
        popupRecompensa?.SetActive(false);

        // Verifica recompensa diária logo ao abrir o menu
        FindFirstObjectByType<DailyRewardSystem>()?.CheckDailyReward();
    }

    // ── Navegação ────────────────────────────────────────────────────────────────

    public void LoadGameScene()
    {
        Debug.Log($"[MainMenuManager] Carregando {nomeGameScene}...");
        SceneManager.LoadScene(nomeGameScene);
    }

    public void AbrirOfertas() => AbrirPainel(painelOfertas);
    public void AbrirBencaos() => AbrirPainel(painelBencaos);
    public void AbrirBaus()    => AbrirPainel(painelBaus);

    void AbrirPainel(GameObject painel)
    {
        foreach (GameObject p in new[] { painelLoja, painelPasse, painelMissoes, painelRanking, painelConfiguracoes, painelOfertas, painelBencaos, painelBaus })
            p?.SetActive(false);
        painel?.SetActive(true);
    }

    // ── UI ───────────────────────────────────────────────────────────────────────

    void ConfigurarBotoes()
    {
        botaoJogar?.onClick.AddListener(LoadGameScene);
        botaoLoja?.onClick.AddListener(() => AbrirPainel(painelLoja));
        botaoPasse?.onClick.AddListener(() => AbrirPainel(painelPasse));
        botaoMissoes?.onClick.AddListener(() => AbrirPainel(painelMissoes));
        botaoRanking?.onClick.AddListener(() => AbrirPainel(painelRanking));
        botaoConfiguracoes?.onClick.AddListener(() => AbrirPainel(painelConfiguracoes));
        botaoOfertas?.onClick.AddListener(AbrirOfertas);
        botaoBencaos?.onClick.AddListener(AbrirBencaos);
        botaoBaus?.onClick.AddListener(AbrirBaus);
        botaoColetarRecompensa?.onClick.AddListener(ColetarRecompensaDiaria);
    }

    void AtualizarInfosJogador()
    {
        int saldo = DiamondSystem.Instance?.GetBalance() ?? 0;
        AtualizarDiamantes(saldo);

        int nivelPasse = SeasonPassSystem.Instance?.NivelAtual ?? 0;
        if (textoNivelPasse != null) textoNivelPasse.text = $"Nível {nivelPasse}";

        int streak = PlayerPrefs.GetInt(DailyRewardSystem.PREF_DIA_STREAK, 0);
        if (textoStreakLogin != null) textoStreakLogin.text = $"Streak: {streak} dia(s)";
    }

    void AtualizarDiamantes(int saldo)
    {
        if (textoDiamantes != null) textoDiamantes.text = saldo.ToString("N0");
    }

    void ExibirPopupRecompensa(int dia, int diamantes)
    {
        diaRecompensaAtual        = dia;
        diamantesRecompensaAtual  = diamantes;

        if (textoRecompensaDia != null)
            textoRecompensaDia.text = $"Dia {dia} de 7";
        if (textoRecompensaDiamantes != null)
            textoRecompensaDiamantes.text = $"+{diamantes} diamantes" + (dia == 7 ? "\n🎁 Item especial!" : "");

        popupRecompensa?.SetActive(true);
    }

    void ColetarRecompensaDiaria()
    {
        FindFirstObjectByType<DailyRewardSystem>()?.ClaimReward(diaRecompensaAtual);
        popupRecompensa?.SetActive(false);
        AtualizarInfosJogador();
    }
}
