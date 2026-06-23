using UnityEngine;
using MoreMountains.Feedbacks;

/// Fachada de juice. Singleton de cena — não sobrevive entre cenas.
/// Fase 1: apenas Hit (squash + flash branco no inimigo).
public class SolengardFeel : MonoBehaviour
{
    public static SolengardFeel Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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

    // Stubs para fases futuras — sem implementação agora.
    public void PlayerHit() { }
    public void BossHit(Transform boss) { }
    public void PlayerDied() { }
}
