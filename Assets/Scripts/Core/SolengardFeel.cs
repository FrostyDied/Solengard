using System.Collections;
using UnityEngine;
using TMPro;
using MoreMountains.Feedbacks;

/// Fachada de juice. Singleton de cena — não sobrevive entre cenas.
/// Fase 1: Hit (squash no inimigo). Fase 2B: PlayerHit (som). Fase 2C: SpecialPower (som + flash no botão).
/// Fase 3: BossWarning (banner com nome do boss).
public class SolengardFeel : MonoBehaviour
{
    public static SolengardFeel Instance { get; private set; }

    [Header("Fase 2B")]
    [SerializeField] MMF_Player _playerHitFeedback;

    [Header("Fase 2C")]
    [SerializeField] MMF_Player _specialPowerFeedback;

    [Header("Fase 3 — Boss")]
    [SerializeField] MMF_Player _bossBannerFeedback;
    [SerializeField] TMP_Text   _bossBannerText;
    [SerializeField] MMF_Player _bossDeathFeedback;
    [SerializeField] MMF_Player _bossVictoryFeedback;
    [SerializeField] TMP_Text   _bossVictoryNameText;

    MMF_Sound _specialSound;
    MMF_Image _specialFlash;

    // Nome do boss da zona atual — capturado no BossWarning para o texto de vitória.
    string _lastBossTitle = "BOSS";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _specialSound = _specialPowerFeedback?.FeedbacksList?.Find(f => f is MMF_Sound) as MMF_Sound;
        _specialFlash = _specialPowerFeedback?.FeedbacksList?.Find(f => f is MMF_Image) as MMF_Image;

        // Texto de vitória começa invisível — o fade-in do MMF_Player o revela ao tocar.
        var victoryCG = _bossVictoryFeedback?.GetComponent<CanvasGroup>();
        if (victoryCG != null) victoryCG.alpha = 0f;
    }

    void OnEnable()
    {
        ZoneManager.OnBossWarning  += BossWarning;
        ZoneManager.OnBossDefeated += BossDefeated;
    }

    void OnDisable()
    {
        ZoneManager.OnBossWarning  -= BossWarning;
        ZoneManager.OnBossDefeated -= BossDefeated;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Toca o MMF_Player que o EnemyBase montou no próprio GO do inimigo.
    public void Hit(Transform enemy)
    {
        if (enemy == null) return;
        enemy.GetComponent<MMF_Player>()?.PlayFeedbacks();
    }

    // Fase 2B: flash de tela + som ao player tomar dano.
    // Sem lockout — iframes do PlayerHealth garantem no máximo 1 chamada por intervalo.
    public void PlayerHit()
    {
        _playerHitFeedback?.PlayFeedbacks();
    }

    // Fase 2C: som + flash da cor da classe no botão de special.
    public void SpecialPower()
    {
        var (color, clip) = PlayerClassManager.Instance?.GetClassConfig() ?? (Color.white, null);
        if (_specialSound != null) _specialSound.Sfx = clip;
        if (_specialFlash != null)
        {
            _specialFlash.ToDestinationColor = new Color(color.r, color.g, color.b, 0.7f);
            _specialFlash.Duration = 0.6f;
        }
        _specialPowerFeedback?.PlayFeedbacks();
    }

    // Fase 3: banner com nome do boss quando o spawn inicia (gatilho: ZoneManager.OnBossWarning).
    public void BossWarning(string bossTitle)
    {
        _lastBossTitle = string.IsNullOrEmpty(bossTitle) ? "BOSS" : bossTitle;
        if (_bossBannerText != null)
            _bossBannerText.text = _lastBossTitle.ToUpper();
        _bossBannerFeedback?.PlayFeedbacks();
    }

    // Fase 3: hit stop + flash dourado + som + texto de vitória quando todos os bosses morrem.
    // O texto de vitória é atrasado 0.4s para começar APÓS o flash dourado (0.3s) terminar —
    // senão o flash interfere no CanvasGroup do texto e causa aparição dupla.
    public void BossDefeated()
    {
        if (_bossVictoryNameText != null)
            _bossVictoryNameText.text = _lastBossTitle.ToUpper();
        _bossDeathFeedback?.PlayFeedbacks();   // hit stop + flash + som
        StartCoroutine(DelayedVictoryFeedback());
    }

    IEnumerator DelayedVictoryFeedback()
    {
        yield return new WaitForSeconds(0.4f);
        _bossVictoryFeedback?.PlayFeedbacks(); // texto de vitória (fade-in + slam + hold + fade)
    }

    // Stubs para fases futuras — sem implementação agora.
    public void BossHit(Transform boss) { }
    public void PlayerDied() { }
}
