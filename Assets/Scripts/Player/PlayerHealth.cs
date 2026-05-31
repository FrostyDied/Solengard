using System.Collections;
using UnityEngine;

// Gerencia a vida do player e integra com o GameManager ao morrer.
// Attach junto ao PlayerController no GameObject do player.
public class PlayerHealth : MonoBehaviour
{
    // ── Eventos (a HUD e outros sistemas assinam aqui) ──────────────────────────

    // Disparado sempre que a vida muda; passa vida atual e máxima para a barra de HUD
    public static event System.Action<float, float> OnHealthChanged;

    // Disparado uma única vez quando o player morre
    public static event System.Action OnPlayerDied;

    // ── Atributos configuráveis ─────────────────────────────────────────────────

    [Header("Vida")]
    public float maxHealth = 150f;

    [Header("Invencibilidade (iframes)")]
    // Duração em segundos de invencibilidade após receber dano
    public float iframeDuration = 0.5f;

    // ── Estado interno ──────────────────────────────────────────────────────────

    float     currentHealth;
    bool      isInvincible = false;
    bool      isDead = false;
    Coroutine iFrameCoroutine;

    // ── Propriedades de leitura ─────────────────────────────────────────────────

    public float CurrentHealth => currentHealth;
    public float MaxHealth     => maxHealth;

    // Retorna valor entre 0 e 1; use direto no slider da barra de vida
    public float HealthPercentage => currentHealth / maxHealth;

    public bool IsInvincible { get => isInvincible; set => isInvincible = value; }

    // ── Unity ───────────────────────────────────────────────────────────────────

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Notifica a HUD com os valores iniciais assim que a cena carrega
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ── API pública ─────────────────────────────────────────────────────────────

    // Aplica dano ao player; ignorado durante iframes ou após a morte
    public void TakeDamage(float amount)
    {
        if (Time.timeScale == 0f || isInvincible || isDead) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        StartCoroutine(FlashWhite());

        var anim = GetComponent<CharacterAnimator>();
        if (anim != null)
        {
            anim.SetState(CharacterAnimator.State.Hurt);
            Invoke(nameof(ResetAnim), 0.3f);
        }

        if (currentHealth <= 0f)
            Die();
        else
        {
            if (iFrameCoroutine != null) StopCoroutine(iFrameCoroutine);
            iFrameCoroutine = StartCoroutine(IFrameRoutine());
        }
    }

    IEnumerator FlashWhite()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        sr.color = Color.white * 2f;
        yield return new WaitForSeconds(0.08f);
        sr.color = Color.white;
    }

    // Restaura vida sem ultrapassar o máximo
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Restaura vida e maxHealth ao retomar uma sessão salva
    public void RestoreHealth(float current, float max)
    {
        maxHealth     = max;
        currentHealth = Mathf.Clamp(current, 0f, max);
        isDead        = currentHealth <= 0f;
        isInvincible  = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[PlayerHealth] RestoreHealth: {currentHealth:F0}/{maxHealth:F0}");
    }

    // Revive o player após game over (usado pelo sistema de ressuscitar via anúncio)
    public void Revive(float healthFraction = 0.5f)
    {
        isDead        = false;
        iFrameCoroutine = null;
        isInvincible  = false;
        currentHealth = maxHealth * Mathf.Clamp01(healthFraction);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[PlayerHealth] Player revivido com {currentHealth:F0}/{maxHealth:F0} HP.");
    }

    // ── Lógica interna ──────────────────────────────────────────────────────────

    void Die()
    {
        isDead = true;
        Debug.Log("[PlayerHealth] Player morreu — chamando GameOver");
        OnPlayerDied?.Invoke();

        if (GameManager.Instance == null)
        {
            Debug.LogError("[PlayerHealth] GameManager.Instance é null — TriggerGameOver não será chamado!");
            return;
        }

        GameManager.Instance.TriggerGameOver();
    }

    // Ativa invencibilidade temporária e a desativa após iframeDuration segundos
    IEnumerator IFrameRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(iframeDuration);
        isInvincible = false;
    }

    void ResetAnim()
    {
        var anim = GetComponent<CharacterAnimator>();
        if (anim != null) anim.ForceState(CharacterAnimator.State.Idle);
    }
}
