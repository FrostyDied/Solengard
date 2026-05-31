using System.Collections.Generic;
using UnityEngine;

public class WaveBoostSystem : MonoBehaviour
{
    public static WaveBoostSystem Instance { get; private set; }

    [System.Serializable]
    public class Boost
    {
        public string id;
        public string nome;
        public string descricao;
        public int    custoDiamantes;
        public float  valor;
    }

    [Header("Boosts disponíveis")]
    [SerializeField] List<Boost> boostsDisponiveis = new();

    [Header("Config de vídeo")]
    [SerializeField] int videoACada = 3;

    int _ultimaWaveComVideo = -99;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (boostsDisponiveis.Count == 0) CriarBoostsPadrao();
    }

    void OnEnable()  => WaveManager.OnWaveCompleted += AoCompletarWave;
    void OnDisable() => WaveManager.OnWaveCompleted -= AoCompletarWave;

    void AoCompletarWave(int wave)
    {
        Debug.Log($"[WaveBoostSystem] Wave {wave} concluída — boost disponível. Vídeo: {PodeAssistirVideo(wave)}");
        // UI de boost será implementada em fase posterior
    }

    void CriarBoostsPadrao()
    {
        boostsDisponiveis.Add(new Boost { id="dano",     nome="Dano +20%",       descricao="Aumenta dano de ataque",  custoDiamantes=50, valor=0.20f });
        boostsDisponiveis.Add(new Boost { id="vel",      nome="Velocidade +15%", descricao="Move mais rápido",        custoDiamantes=40, valor=0.15f });
        boostsDisponiveis.Add(new Boost { id="vida",     nome="Vida Máx +25",    descricao="Mais vida máxima",        custoDiamantes=60, valor=25f   });
        boostsDisponiveis.Add(new Boost { id="range",    nome="Alcance +20%",    descricao="Ataca de mais longe",     custoDiamantes=45, valor=0.20f });
        boostsDisponiveis.Add(new Boost { id="cooldown", nome="Ataque +15%",     descricao="Ataca mais rápido",       custoDiamantes=55, valor=0.15f });
    }

    public bool PodeAssistirVideo(int waveAtual) =>
        (waveAtual - _ultimaWaveComVideo) >= videoACada;

    public bool ComprarBoost(string boostId)
    {
        var boost = boostsDisponiveis.Find(b => b.id == boostId);
        if (boost == null || DiamondSystem.Instance == null) return false;
        if (DiamondSystem.Instance.Saldo < boost.custoDiamantes) return false;

        DiamondSystem.Instance.GastarDiamantes(boost.custoDiamantes);
        AplicarBoost(boost);
        return true;
    }

    public void PegarTodosViaVideo(int waveAtual)
    {
        if (!PodeAssistirVideo(waveAtual)) return;
        // AdSystem stub — substitua pela chamada real ao rewarded ad
        _ultimaWaveComVideo = waveAtual;
        foreach (var b in boostsDisponiveis) AplicarBoost(b);
        Debug.Log("[WaveBoostSystem] Todos os boosts aplicados via vídeo!");
    }

    void AplicarBoost(Boost b)
    {
        var pc = PlayerController.Instance;
        var pa = pc != null ? pc.GetComponent<PlayerAttack>()  : null;
        var ph = pc != null ? pc.GetComponent<PlayerHealth>() : null;

        switch (b.id)
        {
            case "dano":     if (pa != null) pa.attackDamage  *= (1f + b.valor); break;
            case "vel":      if (pc != null) pc.moveSpeed     *= (1f + b.valor); break;
            case "vida":     if (ph != null) ph.AumentarVidaMax(b.valor);        break;
            case "range":    if (pa != null) pa.attackRange   *= (1f + b.valor); break;
            case "cooldown": if (pa != null) pa.attackCooldown *= (1f - b.valor); break;
        }

        Debug.Log($"[WaveBoostSystem] Boost '{b.nome}' aplicado.");
    }
}
