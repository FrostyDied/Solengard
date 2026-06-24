using UnityEngine;
using MoreMountains.Feedbacks;

/// Fachada de juice. Singleton de cena — não sobrevive entre cenas.
/// Fase 1: Hit (squash no inimigo). Fase 2B: PlayerHit (som). Fase 2C: SpecialPower (som + flash no botão).
public class SolengardFeel : MonoBehaviour
{
    public static SolengardFeel Instance { get; private set; }

    [Header("Fase 2B")]
    [SerializeField] MMF_Player _playerHitFeedback;

    [Header("Fase 2C")]
    [SerializeField] MMF_Player _specialPowerFeedback;

    MMF_Sound _specialSound;
    MMF_Image _specialFlash;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _specialSound = _specialPowerFeedback?.FeedbacksList?.Find(f => f is MMF_Sound) as MMF_Sound;
        _specialFlash = _specialPowerFeedback?.FeedbacksList?.Find(f => f is MMF_Image) as MMF_Image;
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

    // Stubs para fases futuras — sem implementação agora.
    public void BossHit(Transform boss) { }
    public void PlayerDied() { }
}
