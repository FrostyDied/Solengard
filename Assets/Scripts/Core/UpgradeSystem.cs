using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    NovaArma,
    UpgradeArma,
    ItemPassivo,
    CuraInstantanea,
    AumentarVidaMaxima
}

[System.Serializable]
public class UpgradeOption
{
    public string      nome;
    public string      descricao;
    public UpgradeType tipo;
    public PassiveItemType itemPassivo; // relevante quando tipo == ItemPassivo
    public Sprite      icone;
}

// Sistema roguelite de upgrades entre waves.
// Gera 3 opções ao fim de cada wave e aguarda o jogador escolher.
public class UpgradeSystem : MonoBehaviour
{
    // Passa as 3 opções para a UI exibir
    public static event System.Action<List<UpgradeOption>> OnUpgradeOptionsReady;

    [Header("Referências")]
    public PlayerHealth     playerHealth;
    public PassiveItemSystem passiveItemSystem;

    [Header("Pool de Upgrades disponíveis")]
    public List<UpgradeOption> poolDeUpgrades = new();

    void OnEnable()  => WaveManager.OnWaveCompleted += AoFimDeWave;
    void OnDisable() => WaveManager.OnWaveCompleted -= AoFimDeWave;

    void Start()
    {
        if (poolDeUpgrades.Count != 0) return;
        poolDeUpgrades.AddRange(new[]
        {
            new UpgradeOption { nome = "Cura Instantânea", descricao = "Restaura 50 HP",       tipo = UpgradeType.CuraInstantanea    },
            new UpgradeOption { nome = "Vida Máxima",      descricao = "+25 HP máximos",        tipo = UpgradeType.AumentarVidaMaxima },
            new UpgradeOption { nome = "Nova Arma",        descricao = "Equipa uma nova arma",  tipo = UpgradeType.NovaArma           },
            new UpgradeOption { nome = "Upgrade de Arma",  descricao = "Melhora a arma atual",  tipo = UpgradeType.UpgradeArma        },
            new UpgradeOption { nome = "Item Passivo",     descricao = "Adiciona item passivo", tipo = UpgradeType.ItemPassivo         },
        });
        Debug.Log("[UpgradeSystem] Pool populada com 5 opções padrão.");
    }

    void AoFimDeWave(int waveNumber) => GerarENotificar();

    // Sorteia 3 upgrades distintos da pool configurada
    public List<UpgradeOption> GenerateUpgradeOptions()
    {
        if (poolDeUpgrades.Count == 0)
        {
            Debug.LogWarning("[UpgradeSystem] Pool de upgrades vazia. Configure no Inspector.");
            return new List<UpgradeOption>();
        }

        List<UpgradeOption> disponiveis = new(poolDeUpgrades);
        List<UpgradeOption> opcoes      = new();
        int quantidade = Mathf.Min(3, disponiveis.Count);

        for (int i = 0; i < quantidade; i++)
        {
            int idx = Random.Range(0, disponiveis.Count);
            opcoes.Add(disponiveis[idx]);
            disponiveis.RemoveAt(idx);
        }
        return opcoes;
    }

    // Aplica o upgrade escolhido pelo jogador
    public void ApplyUpgrade(UpgradeOption opcao)
    {
        if (opcao == null) return;

        switch (opcao.tipo)
        {
            case UpgradeType.CuraInstantanea:
                playerHealth?.Heal(50f);
                break;

            case UpgradeType.AumentarVidaMaxima:
                if (playerHealth != null)
                {
                    playerHealth.maxHealth += 25f;
                    playerHealth.Heal(25f);
                }
                break;

            case UpgradeType.ItemPassivo:
                passiveItemSystem?.AddPassiveItem(opcao.itemPassivo);
                break;

            case UpgradeType.NovaArma:
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO == null) { Debug.LogWarning("[UpgradeSystem] Player não encontrado para NovaArma."); break; }
                var weapon = playerGO.GetComponent<PlayerWeapon>() ?? playerGO.AddComponent<PlayerWeapon>();
                if (!weapon.IsMaxLevel) weapon.Upgrade();
                break;
            }

            case UpgradeType.UpgradeArma:
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO == null) { Debug.LogWarning("[UpgradeSystem] Player não encontrado para UpgradeArma."); break; }
                var weapon = playerGO.GetComponent<PlayerWeapon>();
                if (weapon != null && !weapon.IsMaxLevel) weapon.Upgrade();
                else if (weapon == null) Debug.LogWarning("[UpgradeSystem] PlayerWeapon não encontrado para UpgradeArma.");
                break;
            }
        }

        Debug.Log($"[UpgradeSystem] Upgrade aplicado: {opcao.nome}");
    }

    void GerarENotificar()
    {
        List<UpgradeOption> opcoes = GenerateUpgradeOptions();
        OnUpgradeOptionsReady?.Invoke(opcoes);
    }
}
