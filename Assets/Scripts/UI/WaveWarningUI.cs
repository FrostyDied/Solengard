using System.Collections;
using UnityEngine;
using TMPro;

public class WaveWarningUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] GameObject      banner;
    [SerializeField] TextMeshProUGUI bannerText;
    [SerializeField] CanvasGroup     canvasGroup;

    static readonly Color ColorNormal = Color.white;
    static readonly Color ColorElite  = new Color(1f, 0.5f, 0f);
    static readonly Color ColorBoss   = Color.red;

    bool _bossWarningShown = false;

    void Awake()
    {
        banner?.SetActive(false);
    }

    void OnEnable()
    {
        ZoneManager.OnZoneCompleted += HandleWaveCompleted;
        WaveTimerSystem.OnTimerTick += CheckBossWarning;
        ZoneManager.OnZoneStarted   += ResetBossWarning;
    }

    void OnDisable()
    {
        ZoneManager.OnZoneCompleted -= HandleWaveCompleted;
        WaveTimerSystem.OnTimerTick -= CheckBossWarning;
        ZoneManager.OnZoneStarted   -= ResetBossWarning;
    }

    void HandleWaveCompleted(int completedWave)
    {
        int nextWave = completedWave + 1;
        StartCoroutine(ShowAfterDelay(0f, nextWave));
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
        if (wave == 5 || wave == 8) return WaveType.Elite;
        return WaveType.Normal;
    }

    void ResetBossWarning(int _) => _bossWarningShown = false;

    void CheckBossWarning(float timeRemaining)
    {
        if (_bossWarningShown) return;
        if (timeRemaining <= 123f && timeRemaining > 120f)
        {
            _bossWarningShown = true;
            StartCoroutine(ShowBossBanner());
        }
    }

    IEnumerator ShowBossBanner()
    {
        if (banner == null) yield break;

        if (bannerText != null)
        {
            bannerText.text      = "⚠ BOSS A CAMINHO ⚠";
            bannerText.color     = new Color(1f, 0.2f, 0.1f);
            bannerText.fontStyle = TMPro.FontStyles.Bold;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        var rt = banner.GetComponent<RectTransform>();
        banner.SetActive(true);

        float   screenW   = Screen.width;
        Vector2 centerPos = rt != null ? new Vector2(0f, rt.anchoredPosition.y) : Vector2.zero;
        Vector2 startPos  = new Vector2(-screenW, centerPos.y);
        Vector2 endPos    = new Vector2( screenW, centerPos.y);
        if (rt != null) rt.anchoredPosition = startPos;

        float t = 0f, slideTime = 0.4f;

        // Slide in
        while (t < slideTime)
        {
            t += Time.deltaTime;
            if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, centerPos, t / slideTime);
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = centerPos;

        // Pulso vermelho por 2s
        t = 0f;
        while (t < 2f)
        {
            t += Time.deltaTime;
            if (bannerText != null)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(t * 8f);
                bannerText.color = new Color(1f, 0.2f * pulse, 0.1f * pulse, 1f);
            }
            yield return null;
        }

        // Slide out para a direita
        t = 0f;
        while (t < slideTime)
        {
            t += Time.deltaTime;
            if (rt != null) rt.anchoredPosition = Vector2.Lerp(centerPos, endPos, t / slideTime);
            yield return null;
        }

        banner.SetActive(false);
        if (rt != null) rt.anchoredPosition = centerPos; // reset para o banner normal
    }
}
