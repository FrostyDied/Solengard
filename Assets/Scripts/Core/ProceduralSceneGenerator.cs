using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BiomeVisualConfig
{
    public string biomeName;
    public Color groundBase, groundAccent, groundHighlight, groundShadow;
    public Color rockColor, rockDark;
    public Color treeBase, treeMid, treeTopLight, treeTopDark;
    public Color bushColor, bushLight;
    public Color fogColor, particleColor;
    public float fogDensity = 0.4f;
    public int treeCount = 4, rockCount = 5, bushCount = 8;
}

public class ProceduralSceneGenerator : MonoBehaviour
{
    public static ProceduralSceneGenerator Instance { get; private set; }

    [Header("Seed")]
    public int globalSeed = 0; // 0 = aleatório no Awake

    [Header("Config por bioma")]
    public BiomeVisualConfig[] biomes = new BiomeVisualConfig[5];

    // ---- CACHE DE TEXTURAS DE CHÃO (grid 7x7 = 49 chunks, evita stutter) ----
    static readonly Dictionary<long, Texture2D> _groundTexCache = new();
    static readonly Queue<long> _cacheOrder = new();
    const int MAX_CACHED_TEXTURES = 60;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (globalSeed == 0) globalSeed = Random.Range(1, 99999);
        InitDefaultBiomes();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        // NÃO destruir texturas do cache aqui — sobrevivem entre rebuilds de chunk
    }

    public int GetChunkSeed(int chunkX, int chunkY)
        => globalSeed ^ (chunkX * 7919) ^ (chunkY * 6271);

    // Ponto de entrada — chamado pelo ChunkInstance (Bloco F)
    public void GenerateChunk(GameObject chunkRoot, int chunkX, int chunkY,
        int biomeId, float chunkSize)
    {
        int seed = GetChunkSeed(chunkX, chunkY);
        var biome = biomes[Mathf.Clamp(biomeId, 0, biomes.Length - 1)];

        GenerateGround(chunkRoot, seed, biomeId, biome, chunkSize);
        GenerateRocks(chunkRoot, seed, biome, chunkSize);
        GenerateTrees(chunkRoot, seed, biome, chunkSize);
        GenerateBushes(chunkRoot, seed, biome, chunkSize);
    }

    public void ClearChunk(GameObject chunkRoot)
    {
        for (int i = chunkRoot.transform.childCount - 1; i >= 0; i--)
            Destroy(chunkRoot.transform.GetChild(i).gameObject);
        // Texturas cacheadas NÃO são destruídas — o sprite some, a textura fica
    }

    // ---- CHÃO ----
    void GenerateGround(GameObject root, int seed, int biomeId,
        BiomeVisualConfig biome, float size)
    {
        var go = new GameObject("Ground");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -100; // convenção do projeto (layer Default)

        Texture2D tex = GetOrCreateGroundTexture(seed, biomeId, biome);

        const int texSize = 256;
        float ppu = texSize / size;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
    }

    Texture2D GetOrCreateGroundTexture(int seed, int biomeId, BiomeVisualConfig biome)
    {
        long key = ((long)seed << 8) | (uint)biomeId;
        if (_groundTexCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var tex = BuildGroundTexture(seed, biome);

        // LRU simples por ordem de inserção
        if (_groundTexCache.Count >= MAX_CACHED_TEXTURES && _cacheOrder.Count > 0)
        {
            long oldest = _cacheOrder.Dequeue();
            if (_groundTexCache.TryGetValue(oldest, out var old) && old != null)
                Destroy(old);
            _groundTexCache.Remove(oldest);
        }
        _groundTexCache[key] = tex;
        _cacheOrder.Enqueue(key);
        return tex;
    }

    Texture2D BuildGroundTexture(int seed, BiomeVisualConfig biome)
    {
        const int texSize = 256;
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;

        var pixels = new Color[texSize * texSize];
        var noise = new SeededNoise(seed);

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float fx = (float)x / texSize;
                float fy = (float)y / texSize;

                float macro = noise.FBM(fx, fy, 3f, 4);
                float mid   = noise.Smooth(fx * 8f + 5f, fy * 8f + 5f) * 0.5f;
                float micro = noise.Smooth(fx * 20f + 10f, fy * 20f + 10f) * 0.25f;
                float h = macro * 0.5f + mid * 0.35f + micro * 0.15f;

                Color c;
                if (h > 0.65f)
                    c = Color.Lerp(biome.groundAccent, biome.groundHighlight, (h - 0.65f) / 0.35f);
                else if (h > 0.35f)
                    c = Color.Lerp(biome.groundBase, biome.groundAccent, (h - 0.35f) / 0.30f);
                else
                    c = Color.Lerp(biome.groundShadow, biome.groundBase, h / 0.35f);

                // Sombra direcional (luz topo-esquerda)
                float sh = noise.FBM(fx + 0.02f, fy - 0.02f, 3f, 2);
                c = Color.Lerp(c, biome.groundShadow, Mathf.Clamp01(macro - sh) * 0.3f);

                // Rachaduras
                if (noise.Get(fx * 30f, fy * 30f) > 0.92f)
                    c = Color.Lerp(c, biome.groundShadow, 0.5f);

                pixels[y * texSize + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    // ---- PROPS (implementados nos Blocos C e D) ----
    void GenerateRocks(GameObject root, int seed, BiomeVisualConfig biome, float size)
    {
        // Bloco C
    }

    void GenerateTrees(GameObject root, int seed, BiomeVisualConfig biome, float size)
    {
        // Bloco D
    }

    void GenerateBushes(GameObject root, int seed, BiomeVisualConfig biome, float size)
    {
        // Bloco D
    }

    // ---- PALETAS DOS 5 BIOMAS ----
    void InitDefaultBiomes()
    {
        biomes = new BiomeVisualConfig[5];

        biomes[0] = new BiomeVisualConfig { biomeName = "Veremoth",
            groundBase = new Color(0.22f,0.35f,0.18f), groundAccent = new Color(0.28f,0.42f,0.22f),
            groundHighlight = new Color(0.35f,0.52f,0.28f), groundShadow = new Color(0.12f,0.20f,0.10f),
            treeBase = new Color(0.10f,0.06f,0.02f), treeMid = new Color(0.18f,0.10f,0.04f),
            treeTopLight = new Color(0.25f,0.45f,0.12f), treeTopDark = new Color(0.10f,0.20f,0.05f),
            rockColor = new Color(0.23f,0.23f,0.16f), rockDark = new Color(0.10f,0.10f,0.06f),
            bushColor = new Color(0.12f,0.24f,0.06f), bushLight = new Color(0.22f,0.40f,0.12f),
            fogColor = new Color(0.16f,0.36f,0.12f), particleColor = new Color(0.48f,1.00f,0.93f),
            fogDensity = 0.35f, treeCount = 4, rockCount = 5, bushCount = 8 };

        biomes[1] = new BiomeVisualConfig { biomeName = "Khorduum",
            groundBase = new Color(0.18f,0.20f,0.25f), groundAccent = new Color(0.25f,0.28f,0.35f),
            groundHighlight = new Color(0.35f,0.38f,0.48f), groundShadow = new Color(0.08f,0.09f,0.12f),
            treeBase = new Color(0.10f,0.10f,0.16f), treeMid = new Color(0.15f,0.16f,0.22f),
            treeTopLight = new Color(0.79f,0.50f,0.10f), treeTopDark = new Color(0.54f,0.31f,0.06f),
            rockColor = new Color(0.16f,0.16f,0.23f), rockDark = new Color(0.06f,0.06f,0.12f),
            bushColor = new Color(0.10f,0.10f,0.16f), bushLight = new Color(0.79f,0.50f,0.10f),
            fogColor = new Color(0.31f,0.20f,0.04f), particleColor = new Color(1.00f,0.63f,0.10f),
            fogDensity = 0.25f, treeCount = 3, rockCount = 7, bushCount = 5 };

        biomes[2] = new BiomeVisualConfig { biomeName = "Valdross",
            groundBase = new Color(0.20f,0.22f,0.18f), groundAccent = new Color(0.28f,0.30f,0.24f),
            groundHighlight = new Color(0.38f,0.40f,0.32f), groundShadow = new Color(0.10f,0.11f,0.08f),
            treeBase = new Color(0.06f,0.06f,0.08f), treeMid = new Color(0.10f,0.10f,0.12f),
            treeTopLight = new Color(0.18f,0.18f,0.22f), treeTopDark = new Color(0.08f,0.08f,0.10f),
            rockColor = new Color(0.83f,0.79f,0.66f), rockDark = new Color(0.54f,0.50f,0.44f),
            bushColor = new Color(0.10f,0.10f,0.12f), bushLight = new Color(0.69f,0.83f,1.00f),
            fogColor = new Color(0.59f,0.59f,0.78f), particleColor = new Color(0.69f,0.83f,1.00f),
            fogDensity = 0.50f, treeCount = 5, rockCount = 4, bushCount = 6 };

        biomes[3] = new BiomeVisualConfig { biomeName = "Gorveth",
            groundBase = new Color(0.15f,0.22f,0.14f), groundAccent = new Color(0.20f,0.30f,0.18f),
            groundHighlight = new Color(0.28f,0.40f,0.24f), groundShadow = new Color(0.06f,0.10f,0.05f),
            treeBase = new Color(0.04f,0.08f,0.04f), treeMid = new Color(0.08f,0.12f,0.06f),
            treeTopLight = new Color(0.42f,0.18f,0.63f), treeTopDark = new Color(0.23f,0.06f,0.38f),
            rockColor = new Color(0.10f,0.16f,0.10f), rockDark = new Color(0.04f,0.08f,0.04f),
            bushColor = new Color(0.08f,0.12f,0.06f), bushLight = new Color(0.29f,1.00f,0.29f),
            fogColor = new Color(0.12f,0.24f,0.08f), particleColor = new Color(0.91f,0.85f,0.48f),
            fogDensity = 0.65f, treeCount = 5, rockCount = 3, bushCount = 10 };

        biomes[4] = new BiomeVisualConfig { biomeName = "Arkenfall",
            groundBase = new Color(0.30f,0.20f,0.12f), groundAccent = new Color(0.40f,0.28f,0.16f),
            groundHighlight = new Color(0.52f,0.38f,0.22f), groundShadow = new Color(0.14f,0.08f,0.04f),
            treeBase = new Color(0.16f,0.12f,0.06f), treeMid = new Color(0.23f,0.17f,0.08f),
            treeTopLight = new Color(0.83f,0.79f,0.66f), treeTopDark = new Color(0.54f,0.50f,0.44f),
            rockColor = new Color(0.29f,0.22f,0.16f), rockDark = new Color(0.16f,0.11f,0.06f),
            bushColor = new Color(0.12f,0.08f,0.04f), bushLight = new Color(1.00f,0.42f,0.10f),
            fogColor = new Color(0.23f,0.08f,0.04f), particleColor = new Color(1.00f,0.42f,0.10f),
            fogDensity = 0.30f, treeCount = 2, rockCount = 8, bushCount = 4 };
    }
}
