using System.Collections.Generic;
using UnityEngine;

public enum PassiveItemType
{
    HealthOrb,       // Cura instantânea ao coletar
    SpeedRune,       // Aumenta velocidade de movimento
    DamageGem,       // Aumenta dano de ataque
    CriticalStone,   // Bônus de dano crítico (simulado como dano extra)
    ShieldAmulet,    // Aumenta vida máxima
    CooldownCrystal  // Reduz cooldown de ataque
}

[System.Serializable]
public class PassiveItem
{
    public PassiveItemType itemType;
    public string displayName;
    public string descricao;
}

// Gerencia até 6 itens passivos simultâneos e aplica seus efeitos nos componentes do player.
public class PassiveItemSystem : MonoBehaviour
{
    [Header("Referências do Player")]
    public PlayerHealth     playerHealth;
    public PlayerAttack     playerAttack;
    public PlayerController playerController;

    List<PassiveItem> itensAtivos = new();

    public IReadOnlyList<PassiveItem> ItensAtivos => itensAtivos;
    public bool EstaCheia => itensAtivos.Count >= 6;

    // Adiciona um item passivo e aplica seu efeito imediatamente; retorna false se cheio
    public bool AddPassiveItem(PassiveItemType tipo)
    {
        if (EstaCheia)
        {
            Debug.Log("[PassiveItemSystem] Limite de 6 itens passivos atingido.");
            return false;
        }

        PassiveItem item = new() { itemType = tipo, displayName = tipo.ToString() };
        itensAtivos.Add(item);
        AplicarEfeito(tipo);

        Debug.Log($"[PassiveItemSystem] Item adicionado: {tipo}");
        return true;
    }

    void AplicarEfeito(PassiveItemType tipo)
    {
        switch (tipo)
        {
            case PassiveItemType.HealthOrb:
                playerHealth?.Heal(30f);
                break;

            case PassiveItemType.SpeedRune:
                if (playerController != null) playerController.moveSpeed += 1f;
                break;

            case PassiveItemType.DamageGem:
                if (playerAttack != null) playerAttack.attackDamage += 10f;
                break;

            case PassiveItemType.CriticalStone:
                if (playerAttack != null) playerAttack.attackDamage += 5f;
                break;

            case PassiveItemType.ShieldAmulet:
                if (playerHealth != null)
                {
                    playerHealth.maxHealth += 25f;
                    playerHealth.Heal(25f);
                }
                break;

            case PassiveItemType.CooldownCrystal:
                if (playerAttack != null)
                    playerAttack.attackCooldown = Mathf.Max(0.1f, playerAttack.attackCooldown - 0.15f);
                break;
        }
    }
}
