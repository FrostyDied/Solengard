using System.Collections;
using UnityEngine;
using TMPro;

public class WaveWarningUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] GameObject      banner;
    [SerializeField] TextMeshProUGUI bannerText;
    [SerializeField] CanvasGroup     canvasGroup;
    [SerializeField] WaveManager     waveManager;

    static readonly Color ColorNormal = Color.white;
    static readonly Color ColorElite  = new Color(1f, 0.5f, 0f);
    static readonly Color ColorBoss   = Color.red;

    void Awake()
    {
        banner?.SetActive(false);

        if (waveManager == null)
            waveManager = Object.FindFirstObjectByType<WaveManager>();
    }

    void OnEnable()  => WaveManager.OnWaveCompleted += HandleWaveCompleted;
    void OnDisable() => WaveManager.OnWaveCompleted -= HandleWaveCompleted;

    void HandleWaveCompleted(int completedWave)
    {
        if (waveManager == null) return;

        int nextWave = completedWave + 1;
        if (nextWave > waveManager.TotalWaves) return;

        float delay = Mathf.Max(0f, waveManager.TimeBetweenWaves - 3f);
        StartCoroutine(ShowAfterDelay(delay, nextWave));
    }

    IEnumerator ShowAfterDelay(float delay, int waveNumber)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        WaveType type  = GetWaveType(waveNumber);
        string   label = type switch
        {
            WaveType.Elite => "Elite",
            WaveType.Boss  => "Boss",
            _              => "Normal"
        };
        Color color = type switch
        {
            WaveType.Elite => ColorElite,
            WaveType.Boss  => ColorBoss,
            _              => ColorNormal
        };

        if (bannerText != null)
        {
            bannerText.text  = $"WAVE {waveNumber} — {label}";
            bannerText.color = color;
        }

        yield return StartCoroutine(AnimateBanner());
    }

    IEnumerator AnimateBanner()
    {
        banner?.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        yield return StartCoroutine(Fade(0f, 1f, 0.3f));
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(Fade(1f, 0f, 0.3f));

        banner?.SetActive(false);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed          += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    WaveType GetWaveType(int wave)
    {
        if (waveManager == null)                return WaveType.Normal;
        if (wave >= waveManager.TotalWaves)     return WaveType.Boss;
        if (wave == 5 || wave == 8)             return WaveType.Elite;
        return WaveType.Normal;
    }
}
