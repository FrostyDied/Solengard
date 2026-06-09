using UnityEngine;
using System.Collections.Generic;

public enum PermanentUpgradeId
{
    Poder, Armadura, VidaMaxima, Recuperacao, Recarga,
    Area, Velocidade, Duracao, Quantidade, Movimento,
    Magnetismo, Sorte, Crescimento, Riqueza, Maldicao, Ressurreicao,
    PoderEspecial
}

[System.Serializable]
public class PermanentUpgradeData
{
    public PermanentUpgradeId id;
    public string nome;
    public string descricao;
    public int maxLevel;
    public int diamondCostPerLevel;
    public float incrementoPerLevel; // % ou flat dependendo do upgrade
}

public class PermanentUpgradeSystem : MonoBehaviour
{
    public static PermanentUpgradeSystem Instance { get; private set; }

    // Definições dos 16 upgrades
    static readonly PermanentUpgradeData[] Upgrades = new PermanentUpgradeData[]
    {
        new() { id=PermanentUpgradeId.Poder,        nome="Poder",        descricao="Aumenta dano de ataque +5% por nível",          maxLevel=4, diamondCostPerLevel=600,   incrementoPerLevel=0.05f  },
        new() { id=PermanentUpgradeId.Armadura,      nome="Armadura",     descricao="Reduz dano recebido em 1 por nível",            maxLevel=3, diamondCostPerLevel=600,   incrementoPerLevel=1f     },
        new() { id=PermanentUpgradeId.VidaMaxima,    nome="Vida Máxima",  descricao="Aumenta HP máximo +10% por nível",              maxLevel=3, diamondCostPerLevel=400,   incrementoPerLevel=0.10f  },
        new() { id=PermanentUpgradeId.Recuperacao,   nome="Recuperação",  descricao="Regenera +0.1 HP/s por nível",                  maxLevel=4, diamondCostPerLevel=200,   incrementoPerLevel=0.1f   },
        new() { id=PermanentUpgradeId.Recarga,       nome="Recarga",      descricao="Velocidade de ataque +2.5% por nível",          maxLevel=2, diamondCostPerLevel=900,   incrementoPerLevel=0.025f },
        new() { id=PermanentUpgradeId.Area,          nome="Área",         descricao="Área de impacto +5% por nível",                 maxLevel=2, diamondCostPerLevel=300,   incrementoPerLevel=0.05f  },
        new() { id=PermanentUpgradeId.Velocidade,    nome="Velocidade",   descricao="Velocidade dos projéteis +10% por nível",       maxLevel=2, diamondCostPerLevel=300,   incrementoPerLevel=0.10f  },
        new() { id=PermanentUpgradeId.Duracao,       nome="Duração",      descricao="Duração de projéteis +15% por nível",           maxLevel=2, diamondCostPerLevel=300,   incrementoPerLevel=0.15f  },
        new() { id=PermanentUpgradeId.Quantidade,    nome="Quantidade",   descricao="+1 projétil simultâneo",                        maxLevel=1, diamondCostPerLevel=5000,  incrementoPerLevel=1f     },
        new() { id=PermanentUpgradeId.Movimento,     nome="Movimento",    descricao="Velocidade de movimento +5% por nível",         maxLevel=2, diamondCostPerLevel=300,   incrementoPerLevel=0.05f  },
        new() { id=PermanentUpgradeId.Magnetismo,    nome="Magnetismo",   descricao="Raio de coleta de XP +25% por nível",           maxLevel=2, diamondCostPerLevel=300,   incrementoPerLevel=0.25f  },
        new() { id=PermanentUpgradeId.Sorte,         nome="Sorte",        descricao="Chance de drops raros +10% por nível",          maxLevel=3, diamondCostPerLevel=600,   incrementoPerLevel=0.10f  },
        new() { id=PermanentUpgradeId.Crescimento,   nome="Crescimento",  descricao="XP ganho por kill +3% por nível",               maxLevel=4, diamondCostPerLevel=900,   incrementoPerLevel=0.03f  },
        new() { id=PermanentUpgradeId.Riqueza,       nome="Riqueza",      descricao="Diamantes ganhos +10% por nível",               maxLevel=4, diamondCostPerLevel=200,   incrementoPerLevel=0.10f  },
        new() { id=PermanentUpgradeId.Maldicao,      nome="Maldição",     descricao="+10% dificuldade e +10% recompensas por nível", maxLevel=4, diamondCostPerLevel=1700,  incrementoPerLevel=0.10f  },
        new() { id=PermanentUpgradeId.Ressurreicao,  nome="Ressurreição", descricao="Revive uma vez por run com 50% HP",             maxLevel=1, diamondCostPerLevel=10000, incrementoPerLevel=1f     },
        new() { id=PermanentUpgradeId.PoderEspecial, nome="Poder Especial", descricao="Reduz cooldown do especial em 5s por nível", maxLevel=3, diamondCostPerLevel=800, incrementoPerLevel=5f },
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    // Retorna o nível atual de um upgrade (salvo em PlayerPrefs)
    public int GetLevel(PermanentUpgradeId id) =>
        PlayerPrefs.GetInt($"perm_upgrade_{id}", 0);

    // Retorna o multiplicador total de um upgrade
    // Para upgrades percentuais: retorna 1.0 + (level * incremento)
    // Para Armadura (flat): retorna level * incremento (valor a subtrair)
    // Para Quantidade e Ressurreição (fixo): retorna level
    public float GetBonus(PermanentUpgradeId id)
    {
        int level = GetLevel(id);
        if (level == 0) return id == PermanentUpgradeId.Armadura ? 0f : 1f;
        var data = System.Array.Find(Upgrades, u => u.id == id);
        if (data == null) return 1f;

        return id switch
        {
            PermanentUpgradeId.Armadura     => level * data.incrementoPerLevel, // flat
            PermanentUpgradeId.Quantidade   => level,                           // count
            PermanentUpgradeId.Ressurreicao => level,                           // flag
            _                               => 1f + level * data.incrementoPerLevel // multiplier
        };
    }

    // Tenta comprar um nível do upgrade — retorna true se sucesso
    public bool TryPurchase(PermanentUpgradeId id)
    {
        var data = System.Array.Find(Upgrades, u => u.id == id);
        if (data == null) return false;
        int currentLevel = GetLevel(id);
        if (currentLevel >= data.maxLevel) return false;
        int cost = data.diamondCostPerLevel;
        if (!DiamondSystem.Instance.SpendDiamonds(cost)) return false;
        PlayerPrefs.SetInt($"perm_upgrade_{id}", currentLevel + 1);
        PlayerPrefs.Save();
        return true;
    }

    public bool IsMaxLevel(PermanentUpgradeId id) =>
        GetLevel(id) >= System.Array.Find(Upgrades, u => u.id == id)?.maxLevel;

    public static PermanentUpgradeData GetData(PermanentUpgradeId id) =>
        System.Array.Find(Upgrades, u => u.id == id);

    // Retorna total de dificuldade extra da Maldição (0.0 a 0.5)
    public float MaldicaoDifficultyBonus =>
        GetLevel(PermanentUpgradeId.Maldicao) * 0.10f;

    // Retorna bônus de diamantes da Maldição e Riqueza combinados
    public float DiamondBonus =>
        GetBonus(PermanentUpgradeId.Riqueza) * (1f + MaldicaoDifficultyBonus);

    // Regeneração passiva total (HP/s)
    public float RecuperacaoHPS =>
        GetLevel(PermanentUpgradeId.Recuperacao) * 0.1f;

    // Ressurreição disponível para esta run
    bool _ressurreicaoUsadaNaRun = false;
    public bool PodeRessuscitar => GetLevel(PermanentUpgradeId.Ressurreicao) > 0 && !_ressurreicaoUsadaNaRun;
    public void UsarRessurreicao() => _ressurreicaoUsadaNaRun = true;
    public void ResetarRessurreicao() => _ressurreicaoUsadaNaRun = false;
}
