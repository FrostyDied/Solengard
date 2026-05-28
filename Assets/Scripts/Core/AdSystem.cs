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

    public void ShowRewardedAd(System.Action onComplete)
    {
        Debug.Log("[AdSystem] ShowRewardedAd stub — chamando onComplete() diretamente.");
        onComplete?.Invoke();
    }

    public bool IsAdAvailable() => true;
}
