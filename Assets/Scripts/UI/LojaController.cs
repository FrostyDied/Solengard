using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LojaController : MonoBehaviour
{
    public static LojaController Instance { get; private set; }

    [Header("Abas")]
    [SerializeField] GameObject abaPersonagens;
    [SerializeField] GameObject abaUpgrades;
    [SerializeField] GameObject abaDiamantes;
    [SerializeField] Button btnAbaPersonagens;
    [SerializeField] Button btnAbaUpgrades;
    [SerializeField] Button btnAbaDiamantes;

    [Header("Saldo")]
    [SerializeField] TextMeshProUGUI textoSaldo;

    [Header("Feedback")]
    [SerializeField] TextMeshProUGUI textoFeedback;

    // Dados dos personagens
    static readonly (string id, string nome, int preco)[] Classes = {
        ("mage",        "Mago",       500),
        ("assassin",    "Assassino",  800),
        ("necromancer", "Necromante", 1200),
        ("paladin",     "Paladino",   1500),
        ("hunter",      "Caçador",    2000),
    };

    // Dados dos pacotes IAP. "bonus" = percentual exibido (+X%), "diamantes" já é o total final.
    static readonly (string productId, string nome, int diamantes, string preco, int bonus, string badge)[] Pacotes = {
        ("diamonds_200",  "Iniciante",   200,  "R$4,99",   0,  ""),
        ("diamonds_450",  "Aventureiro", 450,  "R$9,99",   12, ""),
        ("diamonds_1000", "Herói",       1000, "R$19,99",  25, "MAIS POPULAR"),
        ("diamonds_2800", "Lenda",       2800, "R$49,99",  40, "MELHOR VALOR"),
        ("diamonds_6000", "Mítico",      6000, "R$99,99",  50, ""),
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        btnAbaPersonagens?.onClick.AddListener(() => AbrirAba(abaPersonagens));
        btnAbaUpgrades?.onClick.AddListener(AbrirUpgradesUnificado);
        btnAbaDiamantes?.onClick.AddListener(() => AbrirAba(abaDiamantes));
    }

    void OnEnable()
    {
        DiamondSystem.OnDiamondsChanged += AtualizarSaldo;
        AtualizarSaldo(DiamondSystem.Instance?.GetBalance() ?? 0);
        AbrirAba(abaPersonagens);
        ResolverBotaoVideo();
        AtualizarEstadoBotaoVideo();
    }

    void OnDisable()
    {
        DiamondSystem.OnDiamondsChanged -= AtualizarSaldo;
        // Nunca deixa o feedback orfao ativo: se a Loja fechar antes da coroutine esconder
        // (troca de aba / navegacao), a coroutine morre sem o SetActive(false). Garante aqui.
        if (textoFeedback != null) textoFeedback.gameObject.SetActive(false);
    }

    // Tick de 1s — so processa com o PainelLoja ativo (Update nao roda com a loja fechada).
    void Update()
    {
        if (btnVideo == null) return;
        tickVideoTimer += Time.unscaledDeltaTime;
        if (tickVideoTimer < 1f) return;
        tickVideoTimer = 0f;
        AtualizarEstadoBotaoVideo();
    }

    // Auto-resolve o botao de video pela hierarquia (sem campo serializado / wiring).
    void ResolverBotaoVideo()
    {
        if (btnVideo != null) return;
        var t = transform.Find("AbaDiamantes/BtnVideo");
        if (t == null) return;
        btnVideo = t.GetComponent<Button>();
        var lbl = t.Find("Label");
        if (lbl != null) textoBotaoVideo = lbl.GetComponent<TextMeshProUGUI>();
    }

    // Atualiza estado/texto do botao de video conforme o limite diario (3 / janela 24h).
    void AtualizarEstadoBotaoVideo()
    {
        if (btnVideo == null) return;

        int  count = PlayerPrefs.GetInt(AD_COUNT_KEY, 0);
        long last  = System.Convert.ToInt64(PlayerPrefs.GetString(AD_LAST_KEY, "0"));
        double segRestante = last > 0
            ? (new System.DateTime(last, System.DateTimeKind.Utc).AddHours(24) - System.DateTime.UtcNow).TotalSeconds
            : 0;

        // Janela expirou -> zera a contagem (persistente, p/ AssistirVideo enxergar count=0).
        if (count >= AD_MAX && segRestante <= 0)
        {
            count = 0;
            PlayerPrefs.SetInt(AD_COUNT_KEY, 0);
            PlayerPrefs.Save();
        }

        if (count >= AD_MAX)
        {
            btnVideo.interactable = false;
            int total = Mathf.Max(0, (int)segRestante);
            int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
            SetTextoVideo($"Disponível em {h:00}:{m:00}:{s:00}");
        }
        else
        {
            btnVideo.interactable = true;
            SetTextoVideo($"Assistir Vídeo  +50 <sprite name=\"diamante\">  ({count}/{AD_MAX})");
        }
    }

    void SetTextoVideo(string txt)
    {
        if (textoBotaoVideo != null) textoBotaoVideo.text = txt;
    }

    void AtualizarSaldo(int saldo)
    {
        if (textoSaldo != null) textoSaldo.text = $"{saldo:N0}";
    }

    // Alpha da placa esmaecida (aba inativa). Ativa = 1.0. Ajustavel.
    const float ALPHA_INATIVO = 0.5f;

    public void AbrirAba(GameObject aba)
    {
        abaPersonagens?.SetActive(aba == abaPersonagens);
        abaUpgrades?.SetActive(aba == abaUpgrades);
        abaDiamantes?.SetActive(aba == abaDiamantes);
        AtualizarHighlightAbas(aba);
    }

    // Acende a placa da aba de conteudo ativa; esmaece as outras (via alpha da Image).
    // Generico: cobre Personagens/Diamantes (trocam conteudo na Loja) e, se algum dia o toggle
    // voltar pro grid interno, tambem a Upgrades. No modo Grimorio, Upgrades sai pra tela cheia
    // (nao passa por AbrirAba) -> nunca acende persistente; reabrir a Loja reseta p/ Personagens.
    void AtualizarHighlightAbas(GameObject abaAtiva)
    {
        SetAlphaAba(btnAbaPersonagens, abaAtiva == abaPersonagens);
        SetAlphaAba(btnAbaUpgrades,    abaAtiva == abaUpgrades);
        SetAlphaAba(btnAbaDiamantes,   abaAtiva == abaDiamantes);
    }

    static void SetAlphaAba(Button b, bool ativo)
    {
        if (b == null) return;
        var img = b.GetComponent<Image>();
        if (img == null) return;
        var c = img.color;
        c.a = ativo ? 1f : ALPHA_INATIVO;
        img.color = c;
    }

    public void AbrirAbaUpgradesDireto()
    {
        gameObject.SetActive(true);
        AbrirAba(abaUpgrades);
    }

    // FIX 3: a aba "Upgrades" interna passa pelo MESMO caminho da BottomTabs
    // (MainMenuManager.AbrirUpgrades), respeitando usarGrimorioUpgrades -> abre o Grimorio
    // (ou o grid, conforme o toggle). Substitui o lambda divergente AbrirAba(abaUpgrades).
    // Delegacao por codigo (sem MenuButtonAction serializado) -> nao depende de int de enum
    // (a cena tem acao shiftada por reordenacao do enum — ver diagnostico do BtnVideo).
    void AbrirUpgradesUnificado()
    {
        var mmm = FindAnyObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (mmm != null) mmm.AbrirUpgrades();
        else AbrirAba(abaUpgrades); // fallback defensivo
    }

    // Compra de personagem
    public void ComprarClasse(string classId, int preco)
    {
        var classes = PlayerClassManager.Instance;
        // Posse: usa o manager se existir; senao fallback DIRETO no PlayerPrefs (mesma chave)
        // -> nunca recobra mesmo com PlayerClassManager.Instance null.
        bool jaPossui = classes != null
            ? classes.IsClassUnlocked(classId)
            : PlayerPrefs.GetInt($"class_unlocked_{classId}", 0) == 1;
        if (jaPossui) { Feedback("Já desbloqueado!"); return; }

        // Saldo: '!= true' trata null como falha -> nunca desbloqueia sem cobrar.
        if (DiamondSystem.Instance?.SpendDiamonds(preco) != true)
        { Feedback("Diamantes insuficientes!"); return; }

        if (classes != null) classes.UnlockClass(classId);
        else { PlayerPrefs.SetInt($"class_unlocked_{classId}", 1); PlayerPrefs.Save(); }

        string nome = System.Array.Find(Classes, c => c.id == classId).nome ?? classId;
        Feedback($"{nome} desbloqueado!");
        RefreshPersonagens();
    }

    // Compra de upgrade permanente
    public void ComprarUpgrade(PermanentUpgradeId id)
    {
        var sys = PermanentUpgradeSystem.Instance;
        if (sys == null) return;
        if (sys.IsMaxLevel(id)) { Feedback("Nível máximo!"); return; }
        var data = PermanentUpgradeSystem.GetData(id);
        if (data == null) return;
        if (sys.TryPurchase(id))
            Feedback($"{data.nome} melhorado!");
        else
            Feedback("Diamantes insuficientes!");
        RefreshUpgrades();
    }

    // Compra de diamantes via IAP
    public void ComprarDiamantes(string productId)
    {
        IAPSystem.Instance?.BuyProduct(productId);
        Feedback("Processando compra...");
    }

    // Vídeo rewarded — limite de 3 por janela de 24h (contada do último vídeo).
    const int    AD_MAX        = 3;
    const string AD_COUNT_KEY  = "ad_video_count";
    const string AD_LAST_KEY   = "ad_video_last_utc";

    // Botao de video: auto-resolvido em OnEnable (AbaDiamantes/BtnVideo) + throttle do tick.
    Button          btnVideo;
    TextMeshProUGUI textoBotaoVideo;
    float           tickVideoTimer;

    public void AssistirVideo()
    {
        var ads = AdSystem.Instance;
        if (ads == null)          { Feedback("Anúncios indisponíveis!"); return; } // nunca deref null
        if (!ads.IsAdAvailable()) { Feedback("Nenhum vídeo disponível!"); return; }

        int  count = PlayerPrefs.GetInt(AD_COUNT_KEY, 0);
        long last  = System.Convert.ToInt64(PlayerPrefs.GetString(AD_LAST_KEY, "0"));
        if (last > 0 &&
            (System.DateTime.UtcNow - new System.DateTime(last, System.DateTimeKind.Utc)).TotalHours >= 24)
            count = 0; // janela de 24h desde o último vídeo expirou -> zera

        if (count >= AD_MAX)
        { Feedback("Limite de vídeos atingido. Volte mais tarde."); return; }

        ads.ShowRewardedAd(
            onRewarded: () => {                          // só dispara se o vídeo CONCLUIU
                int novo = count + 1;
                PlayerPrefs.SetInt(AD_COUNT_KEY, novo);
                PlayerPrefs.SetString(AD_LAST_KEY, System.DateTime.UtcNow.Ticks.ToString());
                PlayerPrefs.Save();
                DiamondSystem.Instance?.AddDiamonds(50);
                Feedback($"+50 diamantes! ({novo}/{AD_MAX})");
                AtualizarEstadoBotaoVideo(); // reflete (n/3) ou trava no 3º imediatamente
            },
            onSkipped: () => Feedback("Vídeo não concluído — sem recompensa.")
        );
    }

    void Feedback(string msg)
    {
        if (textoFeedback == null) return;
        StopAllCoroutines();
        StartCoroutine(MostrarFeedback(msg));
    }

    IEnumerator MostrarFeedback(string msg)
    {
        textoFeedback.text = msg;
        textoFeedback.gameObject.SetActive(true);
        yield return new UnityEngine.WaitForSecondsRealtime(2f); // imune a timeScale==0
        textoFeedback.gameObject.SetActive(false);
    }

    // Chamados pelo Layout Setup para popular cards
    public void RefreshPersonagens() { /* populado pelo SolengardLayoutSetup */ }
    public void RefreshUpgrades()    { /* populado pelo SolengardLayoutSetup */ }

    // Acesso estático aos dados para o Layout Setup
    public static (string id, string nome, int preco)[] GetClasses() => Classes;
    public static (string productId, string nome, int diamantes, string preco, int bonus, string badge)[] GetPacotes() => Pacotes;
}
