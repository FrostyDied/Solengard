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

// Meshes criadas em runtime NÃO são destruídas junto com o GameObject —
// sem isto, cada rebuild de chunk vazaria memória de mesh no mobile.
public class RuntimeMeshAutoDestroy : MonoBehaviour
{
    void OnDestroy()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
    }
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
        GenerateRocks(chunkRoot, seed, biomeId, biome, chunkSize);
        GenerateTrees(chunkRoot, seed, biomeId, biome, chunkSize);
        GenerateBushes(chunkRoot, seed, biomeId, biome, chunkSize);
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

    // ---- ROCHAS (Bloco C) ----
    void GenerateRocks(GameObject root, int seed, int biomeId, BiomeVisualConfig biome, float size)
    {
        var rng = new System.Random(seed + 1000);
        int count = biome.rockCount + rng.Next(3);

        for (int i = 0; i < count; i++)
        {
            float x = (float)(rng.NextDouble() * size * 0.9f - size * 0.45f);
            float y = (float)(rng.NextDouble() * size * 0.9f - size * 0.45f);
            float r = 0.3f + (float)rng.NextDouble() * 0.8f;
            CreateRock(root, new Vector3(x, y, 0), r, seed + i * 100, biomeId, biome);
        }
    }

    void CreateRock(GameObject root, Vector3 localPos, float radius,
        int rockSeed, int biomeId, BiomeVisualConfig biome)
    {
        var go = new GameObject("Rock");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;

        // Sorting pela convenção do projeto: worldY
        float worldY = go.transform.position.y;
        int sortOrder = Mathf.RoundToInt(-worldY * 0.1f + 50f);

        // Sombra projetada
        CreateEllipse(go, new Vector3(radius * 0.15f, -radius * 0.2f, 0),
            radius * 0.9f, radius * 0.35f, new Color(0, 0, 0, 0.35f), 40);

        // Valdross: rochas alongadas verticalmente, como lápides
        float yFlatten = biomeId == 2 ? 0.9f : 0.7f;

        // Corpo — polígono noise radial, fan triangulation com vertex colors
        int pts = 8 + (Mathf.Abs(rockSeed) % 5);
        var noise = new SeededNoise(rockSeed);
        var verts = new Vector3[pts + 1];
        verts[0] = Vector3.zero;
        for (int i = 0; i < pts; i++)
        {
            float angle = (float)i / pts * Mathf.PI * 2f;
            float r = radius * (0.7f + noise.Get(rockSeed + i * 0.3f, 0) * 0.5f);
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r * yFlatten, 0);
        }
        CreateMeshGO(go, verts, pts, biome.rockColor, biome.rockDark, sortOrder);

        // Rachadura ocasional
        if (noise.Get(rockSeed, 1) > 0.6f)
            CreateCrack(go, radius, rockSeed, biome.rockDark, sortOrder + 1);

        // Khorduum: 1-2 cristais saindo do topo da rocha
        if (biomeId == 1)
        {
            int crystals = 1 + (Mathf.Abs(rockSeed / 7) % 2);
            for (int ci = 0; ci < crystals; ci++)
            {
                float cx = (noise.Get(rockSeed, 5 + ci) - 0.5f) * radius * 0.6f;
                float ch = radius * (0.7f + noise.Get(rockSeed, 8 + ci) * 0.6f);
                float cw = radius * 0.16f;
                CreateTriangle(go,
                    new Vector3(cx - cw, radius * 0.2f, 0),
                    new Vector3(cx + cw, radius * 0.2f, 0),
                    new Vector3(cx + (noise.Get(rockSeed, 11 + ci) - 0.5f) * cw, radius * 0.2f + ch, 0),
                    biome.rockColor, biome.bushLight, sortOrder + 2);
            }
        }

        // 2-3 pedras satélite pequenas
        var prng = new System.Random(rockSeed);
        int pebbles = 2 + prng.Next(2);
        for (int p = 0; p < pebbles; p++)
        {
            float pr = radius * (0.15f + (float)prng.NextDouble() * 0.15f);
            float ang = (float)(prng.NextDouble() * Mathf.PI * 2);
            float dist = radius * (1.1f + (float)prng.NextDouble() * 0.5f);
            var ppos = new Vector3(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist * 0.7f, 0);
            CreateSimplePolygon(go, ppos, pr, 6, rockSeed + p * 31,
                biome.rockColor, biome.rockDark, sortOrder);
        }
    }

    // ---- HELPERS DE MESH (compartilhados por rochas, árvores, arbustos) ----

    // Material COMPARTILHADO (não criar new Material por mesh — leak de memória)
    static Material _vertexColorMat;
    static Material GetSharedVertexColorMaterial()
    {
        if (_vertexColorMat == null)
            _vertexColorMat = new Material(Shader.Find("Sprites/Default"));
        return _vertexColorMat;
    }

    // Mesh procedural fan com vertex colors (centro claro, borda escura)
    void CreateMeshGO(GameObject parent, Vector3[] verts, int ptCount,
        Color colorCenter, Color colorEdge, int sortOrder, bool brightenCenter = true)
    {
        var go = new GameObject("Mesh");
        go.transform.SetParent(parent.transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        go.AddComponent<RuntimeMeshAutoDestroy>(); // mesh de runtime não morre com o GO sozinha
        mr.sortingOrder = sortOrder;
        mr.sharedMaterial = GetSharedVertexColorMaterial();

        var mesh = new Mesh();
        var triangles = new int[ptCount * 3];
        for (int i = 0; i < ptCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % ptCount + 1;
        }
        var colors = new Color[verts.Length];
        colors[0] = brightenCenter ? Color.Lerp(colorCenter, Color.white, 0.2f) : colorCenter;
        for (int i = 1; i < verts.Length; i++) colors[i] = colorEdge;

        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
    }

    // Elipse de cor chapada (sombras projetadas)
    void CreateEllipse(GameObject parent, Vector3 localPos, float rx, float ry,
        Color color, int sortOrder, int segments = 16)
    {
        var verts = new Vector3[segments + 1];
        verts[0] = localPos;
        for (int i = 0; i < segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            verts[i + 1] = localPos + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0);
        }
        CreateMeshGO(parent, verts, segments, color, color, sortOrder, brightenCenter: false);
    }

    // Quad com gradiente vertical (troncos, pilares). v4: bl, br, tr, tl
    void CreateQuadMesh(GameObject parent, Vector3[] v4, Color cBottom, Color cTop, int sortOrder)
    {
        var go = new GameObject("Quad");
        go.transform.SetParent(parent.transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        go.AddComponent<RuntimeMeshAutoDestroy>();
        mr.sortingOrder = sortOrder;
        mr.sharedMaterial = GetSharedVertexColorMaterial();

        var mesh = new Mesh();
        mesh.vertices = v4;
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.colors = new[] { cBottom, cBottom, cTop, cTop };
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
    }

    void CreateQuad(GameObject parent, Vector3 localCenter, float w, float h,
        Color bottom, Color top, int sortOrder)
    {
        var v = new Vector3[]
        {
            localCenter + new Vector3(-w * 0.5f, -h * 0.5f, 0),
            localCenter + new Vector3( w * 0.5f, -h * 0.5f, 0),
            localCenter + new Vector3( w * 0.5f,  h * 0.5f, 0),
            localCenter + new Vector3(-w * 0.5f,  h * 0.5f, 0),
        };
        CreateQuadMesh(parent, v, bottom, top, sortOrder);
    }

    // Quad fino entre dois pontos (galhos, rachaduras, lâminas de grama)
    void CreateQuadLine(GameObject parent, Vector3 a, Vector3 b, float width,
        Color color, int sortOrder)
    {
        Vector3 d = b - a;
        Vector3 perp = new Vector3(-d.y, d.x, 0).normalized * (width * 0.5f);
        var v = new Vector3[] { a - perp, a + perp, b + perp, b - perp };
        CreateQuadMesh(parent, v, color, color, sortOrder);
    }

    void CreateTriangle(GameObject parent, Vector3 a, Vector3 b, Vector3 c,
        Color colBase, Color colTip, int sortOrder)
    {
        var go = new GameObject("Tri");
        go.transform.SetParent(parent.transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        go.AddComponent<RuntimeMeshAutoDestroy>();
        mr.sortingOrder = sortOrder;
        mr.sharedMaterial = GetSharedVertexColorMaterial();

        var mesh = new Mesh();
        mesh.vertices = new[] { a, b, c };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.colors = new[] { colBase, colBase, colTip };
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
    }

    // Rachadura: linha quebrada descendo do topo da rocha
    void CreateCrack(GameObject parent, float radius, int seed, Color color, int sortOrder)
    {
        var noise = new SeededNoise(seed);
        Vector3 p = new Vector3((noise.Get(seed, 20) - 0.5f) * radius * 0.6f, radius * 0.45f, 0);
        int segs = 2 + (Mathf.Abs(seed) % 2);
        float w = Mathf.Max(0.02f, radius * 0.05f);
        for (int i = 0; i < segs; i++)
        {
            Vector3 q = p + new Vector3((noise.Get(seed, 21 + i) - 0.5f) * radius * 0.5f,
                                        -radius * (0.25f + noise.Get(seed, 24 + i) * 0.2f), 0);
            CreateQuadLine(parent, p, q, w, color, sortOrder);
            p = q;
        }
    }

    // Polígono irregular achatado (pedras satélite, blocos caídos)
    void CreateSimplePolygon(GameObject parent, Vector3 localPos, float radius,
        int sides, int seed, Color color, Color colorDark, int sortOrder)
    {
        var noise = new SeededNoise(seed);
        var verts = new Vector3[sides + 1];
        verts[0] = localPos;
        for (int i = 0; i < sides; i++)
        {
            float a = (float)i / sides * Mathf.PI * 2f;
            float r = radius * (0.8f + noise.Get(seed + i, 0) * 0.4f);
            verts[i + 1] = localPos + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.7f, 0);
        }
        CreateMeshGO(parent, verts, sides, color, colorDark, sortOrder);
    }

    // ---- ÁRVORES E ARBUSTOS (Bloco D) ----
    void GenerateTrees(GameObject root, int seed, int biomeId, BiomeVisualConfig biome, float size)
    {
        // Bloco D
    }

    void GenerateBushes(GameObject root, int seed, int biomeId, BiomeVisualConfig biome, float size)
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
