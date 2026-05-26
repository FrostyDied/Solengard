using System.Collections.Generic;
using UnityEngine;

// ScriptableObject com todos os dados persistentes do jogador.
// Crie via Assets → Solengard → PlayerData e atribua ao GameManager.
[CreateAssetMenu(fileName = "PlayerData", menuName = "Solengard/PlayerData")]
public class PlayerData : ScriptableObject
{
    const string PREF_KEY = "sol_player_data";

    [Header("Perfil")]
    public string playerName = "Herói";

    [Header("Moeda e Progressão")]
    public int   totalDiamonds;
    public int   currentSeasonLevel;
    public int   seasonXP;
    public int   bestScore;

    [Header("Estatísticas")]
    public int   totalKills;
    public int   totalWaves;

    [Header("Desbloqueáveis")]
    public List<string> unlockedWeapons = new();
    public List<string> unlockedSkins   = new();

    [Header("Configurações")]
    [Range(0f, 1f)] public float volumeMusica  = 0.8f;
    [Range(0f, 1f)] public float volumeSFX     = 1f;
    public Language              language       = Language.Portuguese;
    public bool                  notificacoes   = true;

    // ── Persistência ─────────────────────────────────────────────────────────────

    public void Save()
    {
        string json = JsonUtility.ToJson(new SaveData(this));
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] Dados salvos.");
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json)) return;

        SaveData dados = JsonUtility.FromJson<SaveData>(json);
        if (dados == null) return;

        playerName          = dados.playerName;
        totalDiamonds       = dados.totalDiamonds;
        currentSeasonLevel  = dados.currentSeasonLevel;
        seasonXP            = dados.seasonXP;
        bestScore           = dados.bestScore;
        totalKills          = dados.totalKills;
        totalWaves          = dados.totalWaves;
        unlockedWeapons     = new List<string>(dados.unlockedWeapons ?? new List<string>());
        unlockedSkins       = new List<string>(dados.unlockedSkins   ?? new List<string>());
        volumeMusica        = dados.volumeMusica;
        volumeSFX           = dados.volumeSFX;
        language            = (Language)dados.languageIndex;
        notificacoes        = dados.notificacoes;

        Debug.Log("[PlayerData] Dados carregados.");
    }

    public void Reset()
    {
        playerName         = "Herói";
        totalDiamonds      = 0;
        currentSeasonLevel = 0;
        seasonXP           = 0;
        bestScore          = 0;
        totalKills         = 0;
        totalWaves         = 0;
        unlockedWeapons    = new List<string>();
        unlockedSkins      = new List<string>();
        volumeMusica       = 0.8f;
        volumeSFX          = 1f;
        language           = Language.Portuguese;
        notificacoes       = true;

        PlayerPrefs.DeleteKey(PREF_KEY);
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] Dados resetados.");
    }

    // ── DTO intermediário para serialização ──────────────────────────────────────

    [System.Serializable]
    class SaveData
    {
        public string       playerName;
        public int          totalDiamonds;
        public int          currentSeasonLevel;
        public int          seasonXP;
        public int          bestScore;
        public int          totalKills;
        public int          totalWaves;
        public List<string> unlockedWeapons;
        public List<string> unlockedSkins;
        public float        volumeMusica;
        public float        volumeSFX;
        public int          languageIndex;
        public bool         notificacoes;

        public SaveData(PlayerData d)
        {
            playerName         = d.playerName;
            totalDiamonds      = d.totalDiamonds;
            currentSeasonLevel = d.currentSeasonLevel;
            seasonXP           = d.seasonXP;
            bestScore          = d.bestScore;
            totalKills         = d.totalKills;
            totalWaves         = d.totalWaves;
            unlockedWeapons    = new List<string>(d.unlockedWeapons);
            unlockedSkins      = new List<string>(d.unlockedSkins);
            volumeMusica       = d.volumeMusica;
            volumeSFX          = d.volumeSFX;
            languageIndex      = (int)d.language;
            notificacoes       = d.notificacoes;
        }
    }
}
