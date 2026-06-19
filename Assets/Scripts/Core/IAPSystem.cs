using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

// Sistema de compras in-app com Unity IAP.
// STUB — produtos definidos, processamento pendente de configuração nas stores.
// TODO: instalar pacote com.unity.purchasing via Package Manager para ativar UNITY_PURCHASING.
#if UNITY_PURCHASING
public class IAPSystem : MonoBehaviour, IStoreListener
#else
public class IAPSystem : MonoBehaviour
#endif
{
    public static IAPSystem Instance { get; private set; }

    public static event System.Action<string> OnPurchaseSuccess;
    public static event System.Action<string> OnProductPurchaseFailed;

    // IDs dos produtos — TODO: registrar IDs reais no Google Play Console e App Store Connect
    // IDs alinhados com LojaController.Pacotes (padrao EN por valor). Estes sao os IDs a
    // cadastrar no Google Play Console / App Store Connect.
    public const string PROD_DIA_200   = "diamonds_200";
    public const string PROD_DIA_450   = "diamonds_450";
    public const string PROD_DIA_1000  = "diamonds_1000";
    public const string PROD_DIA_2800  = "diamonds_2800";
    public const string PROD_DIA_6000  = "diamonds_6000";
    public const string PROD_PASSE_PREMIUM = "passe_temporada_premium";
    public const string PROD_DLC_1     = "dlc_temporada_1";

#if UNITY_PURCHASING
    IStoreController    storeController;
    IExtensionProvider  extensionProvider;
    bool                inicializado;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InicializarIAP();
    }

    void InicializarIAP()
    {
#if UNITY_PURCHASING
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(PROD_DIA_200,       ProductType.Consumable);
        builder.AddProduct(PROD_DIA_450,       ProductType.Consumable);
        builder.AddProduct(PROD_DIA_1000,      ProductType.Consumable);
        builder.AddProduct(PROD_DIA_2800,      ProductType.Consumable);
        builder.AddProduct(PROD_DIA_6000,      ProductType.Consumable);
        builder.AddProduct(PROD_PASSE_PREMIUM, ProductType.NonConsumable);
        builder.AddProduct(PROD_DLC_1,         ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
        Debug.Log("[IAPSystem] Inicializando Unity IAP...");
#else
        Debug.Log("[IAPSystem] Unity IAP não instalado. Compras desabilitadas.");
#endif
    }

    // Inicia o fluxo de compra para o produto informado
    public void BuyProduct(string productId)
    {
#if UNITY_PURCHASING
        if (!inicializado || storeController == null)
        {
            Debug.LogWarning("[IAPSystem] IAP não inicializado.");
            OnProductPurchaseFailed?.Invoke("Loja não disponível");
            return;
        }
        storeController.InitiatePurchase(productId);
#else
        Debug.Log($"[IAPSystem] BuyProduct stub: {productId}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AplicarCompra(productId);
#else
        Debug.LogWarning("[IAPSystem] Compra ignorada: Unity IAP não instalado em build de produção.");
#endif
#endif
    }

    // Obrigatório para Apple — restaura compras não-consumíveis
    public void RestorePurchases()
    {
#if UNITY_PURCHASING && (UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX)
        if (!inicializado) return;
        var apple = extensionProvider.GetExtension<IAppleExtensions>();
        apple.RestoreTransactions(resultado =>
        {
            Debug.Log($"[IAPSystem] RestorePurchases: {resultado}");
        });
#else
        Debug.Log("[IAPSystem] RestorePurchases: apenas iOS/macOS.");
#endif
    }

    // ── IStoreListener ───────────────────────────────────────────────────────────

#if UNITY_PURCHASING
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController   = controller;
        extensionProvider = extensions;
        inicializado      = true;
        Debug.Log("[IAPSystem] IAP inicializado com sucesso.");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogWarning($"[IAPSystem] Falha na inicialização: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string mensagem)
    {
        Debug.LogWarning($"[IAPSystem] Falha na inicialização: {error} — {mensagem}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string id = args.purchasedProduct.definition.id;
        Debug.Log($"[IAPSystem] Compra processada: {id}");
        AplicarCompra(id);
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        string motivo = reason.ToString();
        Debug.LogWarning($"[IAPSystem] Falha na compra de {product.definition.id}: {motivo}");
        OnProductPurchaseFailed?.Invoke(motivo);
    }
#endif

    // ── Aplicação de recompensas ──────────────────────────────────────────────────

    void AplicarCompra(string productId)
    {
        switch (productId)
        {
            case PROD_DIA_200:
                DiamondSystem.Instance?.AddDiamonds(200);
                break;
            case PROD_DIA_450:
                DiamondSystem.Instance?.AddDiamonds(450);
                break;
            case PROD_DIA_1000:
                DiamondSystem.Instance?.AddDiamonds(1000);
                break;
            case PROD_DIA_2800:
                DiamondSystem.Instance?.AddDiamonds(2800);
                break;
            case PROD_DIA_6000:
                DiamondSystem.Instance?.AddDiamonds(6000);
                break;
            case PROD_PASSE_PREMIUM:
                SeasonPassSystem.Instance?.AtivarPremium();
                break;
            case PROD_DLC_1:
                // TODO: desbloquear conteúdo da DLC temporada 1
                Debug.Log("[IAPSystem] DLC Temporada 1 desbloqueada. TODO: implementar unlock.");
                break;
        }
        OnPurchaseSuccess?.Invoke(productId);
    }
}
