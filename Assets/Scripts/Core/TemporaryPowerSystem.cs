using System.Collections;
using UnityEngine;

public enum PowerType { FuriaSombria, EscudoAncestral, ChuvaDeProjeteis, InvocarAliado }

// Gerencia poderes temporários obtidos via PowerPickup.
// Apenas 1 poder ativo por vez. Singleton de cena (reseta a cada run).
public class TemporaryPowerSystem : MonoBehaviour
{
    public static TemporaryPowerSystem Instance { get; private set; }
    public static event System.Action<PowerType, float> OnPowerActivated;
    public static event System.Action                   OnPowerExpired;

    [Header("Durações (segundos)")]
    public float duracaoFuriaSombria    = 10f;
    public float duracaoEscudoAncestral = 5f;
    public float duracaoChuvaProjeteis  = 8f;
    public float duracaoInvocarAliado   = 15f;

    bool isPowerActive;

    // Snapshots captured before modifying player stats — restored on revert to avoid float drift
    float snapDamage;
    float snapSpeed;
    float snapCooldown;

    PlayerAttack     playerAttack;
    PlayerHealth     playerHealth;
    PlayerController playerController;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => CachePlayerRefs();

    void CachePlayerRefs()
    {
        playerAttack     = Object.FindFirstObjectByType<PlayerAttack>(FindObjectsInactive.Include);
        playerHealth     = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        playerController = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
    }

    public void GrantRandomPower()
    {
        if (isPowerActive) return;
        var values = System.Enum.GetValues(typeof(PowerType));
        ActivatePower((PowerType)values.GetValue(Random.Range(0, values.Length)));
    }

    public void ActivatePower(PowerType type)
    {
        if (isPowerActive) return;
        if (playerAttack == null) CachePlayerRefs();
        StartCoroutine(PowerRoutine(type));
    }

    IEnumerator PowerRoutine(PowerType type)
    {
        isPowerActive = true;
        float duration = ApplyPower(type);
        OnPowerActivated?.Invoke(type, duration);
        yield return new WaitForSeconds(duration);
        RevertPower(type);
        isPowerActive = false;
        OnPowerExpired?.Invoke();
    }

    float ApplyPower(PowerType type)
    {
        switch (type)
        {
            case PowerType.FuriaSombria:
                if (playerAttack     != null) { snapDamage = playerAttack.attackDamage;     playerAttack.attackDamage  = snapDamage * 1.5f; }
                if (playerController != null) { snapSpeed  = playerController.moveSpeed;    playerController.moveSpeed = snapSpeed  * 1.3f; }
                return duracaoFuriaSombria;

            case PowerType.EscudoAncestral:
                if (playerHealth != null) playerHealth.IsInvincible = true;
                return duracaoEscudoAncestral;

            case PowerType.ChuvaDeProjeteis:
                if (playerAttack != null) { snapCooldown = playerAttack.attackCooldown; playerAttack.attackCooldown = snapCooldown / 3f; }
                return duracaoChuvaProjeteis;

            case PowerType.InvocarAliado:
                SpawnTemporaryAlly();
                return duracaoInvocarAliado;

            default: return 5f;
        }
    }

    void RevertPower(PowerType type)
    {
        switch (type)
        {
            case PowerType.FuriaSombria:
                if (playerAttack     != null) playerAttack.attackDamage  = snapDamage;
                if (playerController != null) playerController.moveSpeed = snapSpeed;
                break;

            case PowerType.EscudoAncestral:
                if (playerHealth != null) playerHealth.IsInvincible = false;
                break;

            case PowerType.ChuvaDeProjeteis:
                if (playerAttack != null) playerAttack.attackCooldown = snapCooldown;
                break;

            case PowerType.InvocarAliado:
                break; // aliado se auto-destrói via TemporaryAlly.lifetime
        }
    }

    void SpawnTemporaryAlly()
    {
        if (playerController == null) return;

        Vector3 pos = playerController.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 1.5f);
        var go = new GameObject("TemporaryAlly");
        go.transform.position = pos;
        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        go.AddComponent<TemporaryAlly>().lifetime = duracaoInvocarAliado;
    }
}
