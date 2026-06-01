using UnityEngine;
using DG.Tweening;

public class BiomeSystem : MonoBehaviour
{
    public static BiomeSystem Instance { get; private set; }

    public enum Biome
    {
        Veremoth,    // Wave 1 — Floresta
        Khorduum,    // Wave 2 — Cavernas
        Valdross,    // Wave 3 — Cemitério
        Gorveth,     // Wave 4 — Pântano
        Arkenfall    // Wave 5 — Campo de Batalha
    }

    [System.Serializable]
    public class BiomeConfig
    {
        public Biome               biome;
        public string              nome;
        public Color               corChao;
        public Color               corChaoAccent;
        public SimpleArena.BiomePattern floorPattern;
        public Color               corNevoa;
        public float               densidadeNevoa;
        public Color               corAmbiente;
        public string              loreTexto;
    }

    readonly BiomeConfig[] _biomes = new BiomeConfig[]
    {
        new BiomeConfig
        {
            biome          = Biome.Veremoth,
            nome           = "Floresta de Veremoth",
            corChao        = new Color(0.10f, 0.12f, 0.10f),
            corChaoAccent  = new Color(0.05f, 0.07f, 0.05f),
            floorPattern   = SimpleArena.BiomePattern.Forest,
            corNevoa       = new Color(0.29f, 0.42f, 0.54f, 0.40f),
            densidadeNevoa = 0.35f,
            corAmbiente    = new Color(0.40f, 0.38f, 0.50f),
            loreTexto      = "A floresta não tem nome nos mapas antigos.\nNinguém que entrou fundo o suficiente voltou para nomeá-la.\n\nOs locais a chamam de Veremoth.\nO lugar onde a luz pede licença."
        },
        new BiomeConfig
        {
            biome          = Biome.Khorduum,
            nome           = "Cavernas de Khorduum",
            corChao        = new Color(0.05f, 0.07f, 0.09f),
            corChaoAccent  = new Color(0.10f, 0.12f, 0.15f),
            floorPattern   = SimpleArena.BiomePattern.Cave,
            corNevoa       = new Color(0.80f, 0.40f, 0.00f, 0.25f),
            densidadeNevoa = 0.20f,
            corAmbiente    = new Color(0.25f, 0.22f, 0.30f),
            loreTexto      = "As árvores não desaparecem.\nElas se tornam pedra.\n\nVocê percebe que não está mais entre árvores\nmas entre estalactites que crescem como dentes\nde uma criatura enterrada há eras.\n\nE de baixo, muito abaixo:\num batimento. Lento. Regular.\nComo se a montanha tivesse um coração."
        },
        new BiomeConfig
        {
            biome          = Biome.Valdross,
            nome           = "Cemitério de Valdross",
            corChao        = new Color(0.16f, 0.18f, 0.10f),
            corChaoAccent  = new Color(0.10f, 0.12f, 0.05f),
            floorPattern   = SimpleArena.BiomePattern.Cemetery,
            corNevoa       = new Color(0.29f, 0.42f, 0.54f, 0.55f),
            densidadeNevoa = 0.55f,
            corAmbiente    = new Color(0.30f, 0.32f, 0.35f),
            loreTexto      = "O cemitério de Valdross não tem muros.\nSe estende até onde a vista alcança em todas as direções.\n\nUm campo de lápides irregulares\nque cobrem o solo como dentes numa mandíbula quebrada.\n\nE então você ouve: passos que param quando você para."
        },
        new BiomeConfig
        {
            biome          = Biome.Gorveth,
            nome           = "Pântano de Gorveth",
            corChao        = new Color(0.05f, 0.10f, 0.05f),
            corChaoAccent  = new Color(0.04f, 0.08f, 0.04f),
            floorPattern   = SimpleArena.BiomePattern.Swamp,
            corNevoa       = new Color(0.20f, 0.45f, 0.25f, 0.60f),
            densidadeNevoa = 0.65f,
            corAmbiente    = new Color(0.20f, 0.28f, 0.22f),
            loreTexto      = "A terra começa a ceder.\nNão colapsa — apenas cede, gradualmente,\ncomo um convite.\n\nHá bolhas que sobem à superfície\ne estouram com um som\nque é quase, mas não exatamente, uma palavra."
        },
        new BiomeConfig
        {
            biome          = Biome.Arkenfall,
            nome           = "Campo de Batalha de Arkenfall",
            corChao        = new Color(0.24f, 0.10f, 0.00f),
            corChaoAccent  = new Color(0.10f, 0.04f, 0.00f),
            floorPattern   = SimpleArena.BiomePattern.Battlefield,
            corNevoa       = new Color(0.29f, 0.42f, 0.54f, 0.45f),
            densidadeNevoa = 0.50f,
            corAmbiente    = new Color(0.20f, 0.18f, 0.25f),
            loreTexto      = "A última batalha foi travada há dois séculos.\nMas para os que ficaram,\nela nunca terminou.\n\nE todos eles — mortos e máquinas e espectros —\nmovem-se com a lentidão inexorável\nde algo que não tem pressa.\n\nVocê chegou até aqui.\nIsso significa algo.\nAinda não sabemos o quê."
        }
    };

    public BiomeConfig CurrentBiome { get; private set; }

    SpriteRenderer       _floorRenderer;
    AtmosphereController _atmosphere;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _atmosphere = Object.FindFirstObjectByType<AtmosphereController>();
        if (SimpleArena.Instance != null) _floorRenderer = SimpleArena.Instance.GetFloorRenderer();
        SetBiome(Biome.Veremoth, instant: true);
    }

    public void SetBiome(Biome biome, bool instant = false)
    {
        var config = System.Array.Find(_biomes, b => b.biome == biome);
        if (config == null) return;
        CurrentBiome = config;

        float duration = instant ? 0f : 3f;

        if (_floorRenderer != null)
        {
            if (instant) _floorRenderer.color = Color.white;
            else         _floorRenderer.DOColor(Color.white, duration);
        }

        _atmosphere?.TransitionTo(config.corNevoa, config.densidadeNevoa, duration);

        SimpleArena.Instance?.SetBiomePalette(config.corChao, config.corChaoAccent, config.floorPattern, instant);

        Debug.Log($"[Biome] Transição para: {config.nome}");
    }

    public BiomeConfig GetConfig(int waveIndex)
    {
        int idx = Mathf.Clamp(waveIndex - 1, 0, _biomes.Length - 1);
        return _biomes[idx];
    }
}
