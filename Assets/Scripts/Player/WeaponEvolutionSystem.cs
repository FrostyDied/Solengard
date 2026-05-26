using System.Collections.Generic;
using UnityEngine;

// Gerencia evolução de armas no estilo Vampire Survivors.
// Combine arma no nível 5 + item passivo específico para obter a arma evoluída.
public class WeaponEvolutionSystem : MonoBehaviour
{
    // Passa: nome da arma original, nome da arma evoluída
    public static event System.Action<string, string> OnWeaponEvolved;

    [Header("Referências")]
    public PassiveItemSystem passiveItemSystem;

    List<PlayerWeapon> armasEquipadas = new();

    // Tabela de combinações: (nomeArma, tipoPassivo) → nomeArmaEvoluída
    static readonly Dictionary<(string, PassiveItemType), string> tabelaEvolucoes = new()
    {
        { ("Espada",  PassiveItemType.DamageGem),      "Espada Sombria"   },
        { ("Arco",    PassiveItemType.SpeedRune),       "Arco das Sombras" },
        { ("Cajado",  PassiveItemType.CooldownCrystal), "Cajado do Caos"   },
        { ("Machado", PassiveItemType.CriticalStone),   "Machado Voraz"    },
        { ("Lança",   PassiveItemType.ShieldAmulet),    "Lança Sagrada"    },
    };

    public IReadOnlyList<PlayerWeapon> ArmasEquipadas => armasEquipadas;

    // Registra uma nova arma no sistema; máximo de 3 simultâneas
    public bool AdicionarArma(PlayerWeapon arma)
    {
        if (armasEquipadas.Count >= 3)
        {
            Debug.Log("[WeaponEvolutionSystem] Máximo de 3 armas equipadas.");
            return false;
        }
        armasEquipadas.Add(arma);
        return true;
    }

    // Verifica se a combinação arma + passivo resulta em evolução e aplica caso positivo
    public bool TryEvolve(PlayerWeapon weapon, PassiveItem passive)
    {
        if (weapon == null || passive == null) return false;

        // Evolução só disponível ao atingir nível máximo
        if (!weapon.IsMaxLevel)
        {
            Debug.Log($"[WeaponEvolutionSystem] {weapon.weaponName} precisa estar no nível 5 para evoluir.");
            return false;
        }

        var chave = (weapon.weaponName, passive.itemType);
        if (!tabelaEvolucoes.TryGetValue(chave, out string nomeEvoluido))
        {
            Debug.Log($"[WeaponEvolutionSystem] Nenhuma evolução para {weapon.weaponName} + {passive.itemType}.");
            return false;
        }

        Debug.Log($"[WeaponEvolutionSystem] {weapon.weaponName} → {nomeEvoluido}!");
        OnWeaponEvolved?.Invoke(weapon.weaponName, nomeEvoluido);

        weapon.weaponName = nomeEvoluido;
        weapon.Upgrade();
        return true;
    }

    // Verifica todas as armas equipadas contra todos os itens passivos ativos
    public void VerificarEvolucoesPossiveis()
    {
        if (passiveItemSystem == null) return;

        foreach (PlayerWeapon arma in armasEquipadas)
        {
            foreach (PassiveItem item in passiveItemSystem.ItensAtivos)
            {
                if (TryEvolve(arma, item)) return; // uma evolução por verificação
            }
        }
    }
}
