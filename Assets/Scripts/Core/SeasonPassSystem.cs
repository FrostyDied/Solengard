using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SeasonReward
{
    public int    nivel;
    public string nome;
    public int    diamantes;
    public bool   apenasPremiun;
    // TODO: adicionar campos para cosméticos/skins quando sistema de itens estiver pronto
}

// Sistema de passe de temporada com 30 níveis, tiers gratuito e premium.
// XP ganho por kills, waves e missões diárias.
public class SeasonPassSystem : MonoBehaviour
{
    public static SeasonPassSystem Instance { get; private set; }

    public static event System.Action<int> OnSeasonLevelUp;

    const string PREF_NIVEL        = "sol_sp_level";
    const string PREF_XP           = "sol_sp_xp";
    const string PREF_PREMIUM       = "sol_sp_premium";
    const string PREF_REWARDS_JSON  = "sol_sp_claimed";
    const int    NIVEIS_TOTAIS      = 30;

    [Header("XP necessário por nível")]
    public int xpPorNivel = 500;

    [Header("Recompensas por nível")]
    public List<SeasonReward> recompensas = new();

    int nivelAtual;
    int xpAtual;
    bool isPremium;

    public int  NivelAtual => nivelAtual;
    public int  XPAtual    => xpAtual;
    public bool IsPremium  => isPremium;
    public int  XPParaProximoNivel => xpPorNivel - (xpAtual % xpPorNivel);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Carregar();
    }

    // Adiciona XP e verifica se houve level up
    public void AddSeasonXP(int quantidade)
    {
        if (nivelAtual >= NIVEIS_TOTAIS) return;

        xpAtual += quantidade;

        while (xpAtual >= xpPorNivel && nivelAtual < NIVEIS_TOTAIS)
        {
            xpAtual   -= xpPorNivel;
            nivelAtual++;
            Debug.Log($"[SeasonPassSystem] Nível {nivelAtual} atingido!");
            OnSeasonLevelUp?.Invoke(nivelAtual);
        }

        Salvar();
    }

    // Reivindica recompensa de um nível específico; isPremiumTier exige passe premium
    public bool ClaimReward(int nivel, bool isPremiumTier)
    {
        if (nivel < 1 || nivel > NIVEIS_TOTAIS) return false;
        if (nivel > nivelAtual) return false;
        if (isPremiumTier && !isPremium)
        {
            Debug.Log("[SeasonPassSystem] Passe premium necessário para esta recompensa.");
            return false;
        }

        string chave = $"nivel_{nivel}_premium_{isPremiumTier}";
        if (PlayerPrefs.GetInt(chave, 0) == 1) return false; // já resgatado

        SeasonReward reward = recompensas.Find(r => r.nivel == nivel && r.apenasPremiun == isPremiumTier);
        if (reward == null) return false;

        PlayerPrefs.SetInt(chave, 1);
        PlayerPrefs.Save();

        if (reward.diamantes > 0)
            DiamondSystem.Instance?.AddDiamonds(reward.diamantes);

        Debug.Log($"[SeasonPassSystem] Recompensa resgatada: {reward.nome}");
        return true;
    }

    // Ativa o tier premium (chamado pelo IAPSystem após compra confirmada)
    public void AtivarPremium()
    {
        isPremium = true;
        PlayerPrefs.SetInt(PREF_PREMIUM, 1);
        PlayerPrefs.Save();
        Debug.Log("[SeasonPassSystem] Passe premium ativado.");
        // TODO: sincronizar status premium com Supabase
    }

    void Salvar()
    {
        PlayerPrefs.SetInt(PREF_NIVEL, nivelAtual);
        PlayerPrefs.SetInt(PREF_XP, xpAtual);
        PlayerPrefs.Save();
    }

    void Carregar()
    {
        nivelAtual = PlayerPrefs.GetInt(PREF_NIVEL, 0);
        xpAtual    = PlayerPrefs.GetInt(PREF_XP, 0);
        isPremium  = PlayerPrefs.GetInt(PREF_PREMIUM, 0) == 1;
    }
}
