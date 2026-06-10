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

    // Dados dos pacotes IAP
    static readonly (string productId, string nome, int diamantes, string preco)[] Pacotes = {
        ("pacote_100_diamantes", "Iniciante",    500,   "R$4,99"),
        ("pacote_500_diamantes", "Aventureiro",  1500,  "R$12,99"),
        ("pacote_1000_diamantes","Herói",         4000,  "R$24,99"),
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        DiamondSystem.OnDiamondsChanged += AtualizarSaldo;
        AtualizarSaldo(DiamondSystem.Instance?.GetBalance() ?? 0);
        AbrirAba(abaPersonagens);
    }

    void OnDisable() => DiamondSystem.OnDiamondsChanged -= AtualizarSaldo;

    void AtualizarSaldo(int saldo)
    {
        if (textoSaldo != null) textoSaldo.text = $"💎 {saldo:N0}";
    }

    public void AbrirAba(GameObject aba)
    {
        abaPersonagens?.SetActive(aba == abaPersonagens);
        abaUpgrades?.SetActive(aba == abaUpgrades);
        abaDiamantes?.SetActive(aba == abaDiamantes);
    }

    public void AbrirAbaUpgradesDireto()
    {
        gameObject.SetActive(true);
        AbrirAba(abaUpgrades);
    }

    // Compra de personagem
    public void ComprarClasse(string classId, int preco)
    {
        if (PlayerClassManager.Instance?.IsClassUnlocked(classId) == true)
        { Feedback("Já desbloqueado!"); return; }
        if (DiamondSystem.Instance?.SpendDiamonds(preco) == false)
        { Feedback("Diamantes insuficientes!"); return; }
        PlayerPrefs.SetInt($"class_unlocked_{classId}", 1);
        PlayerPrefs.Save();
        Feedback($"{classId} desbloqueado!");
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

    // Vídeo rewarded
    public void AssistirVideo()
    {
        if (AdSystem.Instance?.IsAdAvailable() == false)
        { Feedback("Nenhum vídeo disponível!"); return; }
        AdSystem.Instance.ShowRewardedAd(() => {
            DiamondSystem.Instance?.AddDiamonds(50);
            Feedback("+50 💎 por assistir!");
        });
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
        yield return new UnityEngine.WaitForSeconds(2f);
        textoFeedback.gameObject.SetActive(false);
    }

    // Chamados pelo Layout Setup para popular cards
    public void RefreshPersonagens() { /* populado pelo SolengardLayoutSetup */ }
    public void RefreshUpgrades()    { /* populado pelo SolengardLayoutSetup */ }

    // Acesso estático aos dados para o Layout Setup
    public static (string id, string nome, int preco)[] GetClasses() => Classes;
    public static (string productId, string nome, int diamantes, string preco)[] GetPacotes() => Pacotes;
}
