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
    [SerializeField] Button botaoTabJogar;
    public Button botaoLoja;
    public Button botaoPasse;
    public Button botaoMissoes;
    public Button botaoRanking;
    public Button botaoConfiguracoes;
    [SerializeField] Button botaoFecharLoja;

    [Header("Atalhos laterais (LeftPanel)")]
    [SerializeField] Button botaoOfertas;
    [SerializeField] Button botaoBencaos;
    [SerializeField] Button botaoBaus;

    [Header("Informações do jogador")]
    public TextMeshProUGUI textoDiamantes;
    public TextMeshProUGUI textoNivelPasse;
    public TextMeshProUGUI textoStreakLogin;
    [SerializeField] TextMeshProUGUI textoMelhorPontuacao;
    [SerializeField] TextMeshProUGUI textoUltimaRun;

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
    public GameObject painelLegado;

    [Header("Experimento Grimório (alternativo ao grid de Upgrades)")]
    public GameObject painelGrimorio;
    [Tooltip("ON = aba UPGRADES abre o Grimório; OFF = abre o grid atual.")]
    public bool usarGrimorioUpgrades = false;

    [Header("Detalhe de personagem (overlay sobre a Loja)")]
    public GameObject painelDetalhe;

    [Header("Barra de navegação persistente (BottomTabs)")]
    [SerializeField] Image tabLojaImg;
    [SerializeField] Image tabMissoesImg;
    [SerializeField] Image tabUpgradesImg;
    [SerializeField] Image tabLegadoImg;

    public enum NavSection { Nenhuma, Loja, Missoes, Upgrades, Legado }
    static readonly Color NAV_ATIVO  = new Color(0.35f, 0.05f, 0.60f); // #5A1090 destaque (tabActive)
    static readonly Color NAV_NORMAL = new Color(0.08f, 0.05f, 0.15f); // base (tabNormal)

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
        if (botaoJogar           == null) botaoJogar           = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (botaoTabJogar        == null) botaoTabJogar        = GameObject.Find("TabJogar")?.GetComponent<Button>();
        if (textoMelhorPontuacao == null) textoMelhorPontuacao = GameObject.Find("TextoMelhorPontuacao")?.GetComponent<TextMeshProUGUI>();
        if (textoUltimaRun       == null) textoUltimaRun       = GameObject.Find("TextoUltimaRun")?.GetComponent<TextMeshProUGUI>();

        ConfigurarBotoes();
        AtualizarInfosJogador();
        popupRecompensa?.SetActive(false);

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

    // Wrappers públicos para o MenuButtonAction chamar (Passo 3) — delegam à lógica
    // existente; ConfigurarBotoes() permanece inalterado.
    public void AbrirLoja()          { AbrirPainel(painelLoja); HighlightNav(NavSection.Loja); }
    public void AbrirUpgrades()
    {
        // Experimento A/B: bool decide grimorio vs grid (grid = comportamento original).
        if (usarGrimorioUpgrades && painelGrimorio != null)
            AbrirPainel(painelGrimorio);
        else
        {
            AbrirPainel(painelLoja);
            LojaController.Instance?.AbrirAbaUpgradesDireto();
        }
        HighlightNav(NavSection.Upgrades);
    }
    public void AbrirMissoes()       { AbrirPainel(painelMissoes); HighlightNav(NavSection.Missoes); }
    public void AbrirRanking()       => AbrirPainel(painelRanking);
    public void AbrirConfiguracoes() => AbrirPainel(painelConfiguracoes);
    public void AbrirLegado()        { AbrirPainel(painelLegado); HighlightNav(NavSection.Legado); }
    public void ColetarRecompensa()  => ColetarRecompensaDiaria();

    // Overlay sobre a Loja: NAO usa AbrirPainel (exclusivo). Ativa por cima e a Loja
    // permanece ativa atras; o X do detalhe (DetalhePersonagemUI.Fechar) so desativa o
    // detalhe -> volta pra Loja.
    public void AbrirDetalhePersonagem(string classId)
    {
        if (painelDetalhe == null) return;
        painelDetalhe.transform.SetAsLastSibling();            // acima da Loja (overlay)
        painelDetalhe.SetActive(true);
        // BottomTabs persistente DEVE ficar por cima do detalhe (tappavel p/ navegar).
        var bottom = transform.Find("BottomTabs");
        if (bottom != null) bottom.SetAsLastSibling();
        painelDetalhe.GetComponent<DetalhePersonagemUI>()?.Mostrar(classId);
    }

    void AbrirPainel(GameObject painel)
    {
        foreach (GameObject p in new[] { painelLoja, painelPasse, painelMissoes, painelRanking, painelConfiguracoes, painelLegado, painelGrimorio, painelOfertas, painelBencaos, painelBaus, painelDetalhe })
            p?.SetActive(false);
        painel?.SetActive(true);
    }

    public void FecharTodos()
    {
        foreach (var p in new[] { painelLoja, painelPasse, painelMissoes, painelRanking,
                                   painelConfiguracoes, painelLegado, painelGrimorio, painelOfertas, painelBencaos, painelBaus, painelDetalhe })
            if (p != null) p.SetActive(false);
        HighlightNav(NavSection.Nenhuma);
    }

    // Destaca a aba da seção ativa na barra inferior persistente (refs ligadas pelo editor).
    void HighlightNav(NavSection s)
    {
        if (tabLojaImg     != null) tabLojaImg.color     = s == NavSection.Loja     ? NAV_ATIVO : NAV_NORMAL;
        if (tabMissoesImg  != null) tabMissoesImg.color  = s == NavSection.Missoes  ? NAV_ATIVO : NAV_NORMAL;
        if (tabUpgradesImg != null) tabUpgradesImg.color = s == NavSection.Upgrades ? NAV_ATIVO : NAV_NORMAL;
        if (tabLegadoImg   != null) tabLegadoImg.color   = s == NavSection.Legado   ? NAV_ATIVO : NAV_NORMAL;
    }

    // ── UI ───────────────────────────────────────────────────────────────────────

    void ConfigurarBotoes()
    {
        botaoJogar?.onClick.AddListener(LoadGameScene);
        botaoTabJogar?.onClick.AddListener(LoadGameScene);
        botaoLoja?.onClick.AddListener(AbrirLoja);
        botaoPasse?.onClick.AddListener(AbrirUpgrades);
        botaoMissoes?.onClick.AddListener(AbrirMissoes);
        botaoRanking?.onClick.AddListener(() => AbrirPainel(painelRanking));
        botaoConfiguracoes?.onClick.AddListener(() => AbrirPainel(painelConfiguracoes));
        botaoOfertas?.onClick.AddListener(AbrirOfertas);
        botaoBencaos?.onClick.AddListener(AbrirBencaos);
        botaoBaus?.onClick.AddListener(AbrirBaus);
        botaoColetarRecompensa?.onClick.AddListener(ColetarRecompensaDiaria);
        botaoFecharLoja?.onClick.AddListener(FecharTodos);
    }

    void AtualizarInfosJogador()
    {
        Debug.Log($"[MainMenu] sol_best_score={PlayerPrefs.GetInt("sol_best_score", 0)}");
        Debug.Log($"[MainMenu] sol_last_run={PlayerPrefs.GetString("sol_last_run", "vazio")}");
        Debug.Log($"[MainMenu] textoMelhorPontuacao null={textoMelhorPontuacao == null}");
        Debug.Log($"[MainMenu] textoUltimaRun null={textoUltimaRun == null}");

        int saldo = DiamondSystem.Instance?.GetBalance() ?? 0;
        AtualizarDiamantes(saldo);

        int nivelPasse = SeasonPassSystem.Instance?.NivelAtual ?? 0;
        if (textoNivelPasse != null) textoNivelPasse.text = $"Nível {nivelPasse}";

        int streak = PlayerPrefs.GetInt(DailyRewardSystem.PREF_DIA_STREAK, 0);
        if (textoStreakLogin != null) textoStreakLogin.text = $"Streak: {streak} dia(s)";

        int melhorScore = PlayerPrefs.GetInt("sol_best_score", 0);
        if (textoMelhorPontuacao != null)
            textoMelhorPontuacao.text = $"TOP Melhor Pontuacao: {melhorScore:N0}";

        if (textoUltimaRun != null)
        {
            string lastRunJson = PlayerPrefs.GetString("sol_last_run", "");
            if (!string.IsNullOrEmpty(lastRunJson))
            {
                try
                {
                    var lr = JsonUtility.FromJson<LastRunData>(lastRunJson);
                    int mm = Mathf.FloorToInt(lr.time / 60f);
                    int ss = Mathf.FloorToInt(lr.time % 60f);
                    textoUltimaRun.text = $"> Ultima Run: Wave {lr.wave} - {lr.kills} kills - {mm:00}:{ss:00}";
                }
                catch { textoUltimaRun.text = "> Ultima Run: --"; }
            }
            else
                textoUltimaRun.text = "> Ultima Run: --";
        }
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
            textoRecompensaDiamantes.text = $"+{diamantes} diamantes" + (dia == 7 ? "\n+ Item especial!" : "");

        popupRecompensa?.SetActive(true);
    }

    void ColetarRecompensaDiaria()
    {
        FindFirstObjectByType<DailyRewardSystem>()?.ClaimReward(diaRecompensaAtual);
        popupRecompensa?.SetActive(false);
        AtualizarInfosJogador();
    }
}
