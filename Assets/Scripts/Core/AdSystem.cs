using UnityEngine;

// TODO: integrar Unity Ads SDK real (com.unity.ads) antes do lançamento.
// Substitua ShowRewardedAd por Advertisement.Show e IsAdAvailable por Advertisement.IsReady.
public class AdSystem : MonoBehaviour
{
    public static AdSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // onRewarded: chamado SO quando o video foi concluido (credita a recompensa).
    // onSkipped:  chamado quando o usuario fecha antes (sem recompensa).
    // Compativel com chamadas de 1 arg. SDK real: ligar onRewarded ao evento de
    // conclusao e onSkipped ao fechar antecipado.
    public void ShowRewardedAd(System.Action onRewarded, System.Action onSkipped = null)
    {
        Debug.Log("[AdSystem] ShowRewardedAd stub — simula conclusao, chamando onRewarded().");
        onRewarded?.Invoke();
    }

    public bool IsAdAvailable() => true;
}
