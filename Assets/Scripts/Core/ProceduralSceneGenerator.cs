using UnityEngine;
using System.Collections;
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

// Segura a textura de chão viva enquanto o chunk a usa (FIX 3 — refcount)
public class GroundTextureRef : MonoBehaviour
{
    public long key;
    void OnDestroy() => ProceduralSceneGenerator.ReleaseGroundTexture(key);
}

// Pulso suave de alpha para as fake lights (sin lento, fase aleatória)
public class FakeLightPulse : MonoBehaviour
{
    public float baseAlpha = 0.25f;
    public float speed = 1f;
    public float phase;

    SpriteRenderer _sr;

    void Awake() { _sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        if (_sr == null) return;
        var c = _sr.color;
        c.a = baseAlpha * (0.75f + 0.25f * Mathf.Sin(Time.time * speed + phase));
        _sr.color = c;
    }
}

public class ProceduralSceneGenerator : MonoBehaviour
{
    public static ProceduralSceneGenerator Instance { get; private set; }

    [Header("Seed")]
    public int globalSeed = 0; // 0 = aleatório no Awake

    [Header("Config por bioma")]
    public BiomeVisualConfig[] biomes = new BiomeVisualConfig[5];

    [Header("Chão (FIX 1/2)")]
    [Tooltip("128 = mobile seguro; 256 = mais detalhe se não houver stutter")]
    public int groundTexSize = 128;
    [Tooltip("Linhas de textura geradas por frame (amortização)")]
    public int rowsPerFrame = 32;

    [Header("Híbrido (FIX 4)")]
    [Tooltip("false = props Craftpix posicionados pelo motor; true = props 100% procedurais")]
    public bool useProceduralProps = false;

    // ---- CACHE DE TEXTURAS DE CHÃO (grid 7x7 = 49 chunks, evita stutter) ----
    // Key por (chunkX, chunkY, biomeId); refcount impede o LRU de destruir
    // textura que um chunk ativo ainda usa (FIX 3)
    static readonly Dictionary<long, Texture2D> _groundTexCache = new();
    static readonly Dictionary<long, int> _groundTexRefs = new();
    static readonly Queue<long> _cacheOrder = new();
    static readonly HashSet<long> _building = new();
    const int MAX_CACHED_TEXTURES = 60;

    static long GroundKey(int cx, int cy, int biomeId)
        => ((long)(cx & 0xFFFFF) << 28) | ((long)(cy & 0xFFFFF) << 8) | (long)(biomeId & 0xFF);

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
        _building.Clear(); // coroutines morrem com o GO — não deixar keys travadas
        // NÃO destruir texturas do cache aqui — sobrevivem entre rebuilds de chunk
    }

    public int GetChunkSeed(int chunkX, int chunkY)
        => globalSeed ^ (chunkX * 7919) ^ (chunkY * 6271);

    // Ponto de entrada — chamado pelo ChunkInstance (Bloco F)
    // Híbrido (FIX 4): chão sempre procedural; props Craftpix posicionados
    // pelo motor (seed + densidade), a menos que useProceduralProps = true.
    public void GenerateChunk(GameObject chunkRoot, int chunkX, int chunkY,
        int biomeId, float chunkSize,
        List<GameObject> craftpixPrefabs = null, int craftpixCount = 12)
    {
        int seed = GetChunkSeed(chunkX, chunkY);
        var biome = biomes[Mathf.Clamp(biomeId, 0, biomes.Length - 1)];

        // Noise de densidade espacial (Adendo 2): campo coerente no MUNDO
        // (usa globalSeed, não o seed do chunk, para variar suavemente entre chunks)
        Vector3 wp = chunkRoot.transform.position;
        float density = new SeededNoise(globalSeed).FBM(wp.x * 0.01f, wp.y * 0.01f, 2f, 2);
        float densityMult = density < 0.35f ? 0.3f : density > 0.65f ? 1.5f : 1f;
        bool allowTrees = density >= 0.35f; // área aberta: sem árvores (combate)

        _emissiveSpots.Clear(); // posições candidatas para fake lights (Bloco E)

        GenerateGround(chunkRoot, chunkX, chunkY, biomeId, biome, chunkSize);

        if (useProceduralProps)
        {
            // Props 100% procedurais (preservados — reativar pela flag)
            GenerateRocks(chunkRoot, seed, biomeId, biome, chunkSize);
            if (allowTrees)
                GenerateTrees(chunkRoot, seed, biomeId, biome, chunkSize, densityMult);
            GenerateBushes(chunkRoot, seed, biomeId, biome, chunkSize, densityMult);
            GenerateGrass(chunkRoot, seed, biomeId, biome, chunkSize, densityMult);
            GenerateCliffs(chunkRoot, seed, biomeId, biome, chunkSize);
        }
        else
        {
            PlaceCraftpixProps(chunkRoot, seed, biomeId, craftpixPrefabs,
                craftpixCount, chunkSize, densityMult, allowTrees);
        }

        GenerateFakeLights(chunkRoot, seed, biomeId, biome, chunkSize);
    }

    // FIX 4 — props Craftpix com POSICIONAMENTO procedural:
    // seed do chunk decide posição/quantidade; densidade espacial modula;
    // tint de bioma e Y-sorting idênticos ao ChunkInstance original.
    void PlaceCraftpixProps(GameObject root, int seed, int biomeId,
        List<GameObject> prefabs, int count, float size,
        float densityMult, bool allowTrees)
    {
        if (prefabs == null || prefabs.Count == 0) return;

        var rng = new System.Random(seed);
        int total = Mathf.RoundToInt(count * densityMult);
        float half = size * 0.5f;
        float margin = size * 0.1f;

        for (int i = 0; i < total; i++)
        {
            int pi = rng.Next(prefabs.Count);
            if (prefabs[pi] == null) continue;

            float x = (float)(rng.NextDouble() * (size - margin * 2) - (half - margin));
            float y = (float)(rng.NextDouble() * (size - margin * 2) - (half - margin));

            // centro do chunk protegido (área de combate)
            if (Mathf.Abs(x) < size * 0.15f && Mathf.Abs(y) < size * 0.15f) continue;
            // área aberta: sem árvores
            if (!allowTrees && prefabs[pi].name.Contains("Tree")) continue;

            Vector3 pos = root.transform.position + new Vector3(x, y, 0);
            var go = Instantiate(prefabs[pi], pos, Quaternion.identity, root.transform);

            var ep = go.GetComponent<EnvironmentProp>();
            if (ep != null) ep.Initialize(seed + i * 1000);

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = biomeId < ChunkInstance.BIOME_TINTS.Length
                    ? ChunkInstance.BIOME_TINTS[biomeId] : Color.white;
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 0.1f) + 50;
            }

            _emissiveSpots.Add(new Vector3(x, y, 0)); // fake lights junto aos props
        }
    }

    public void ClearChunk(GameObject chunkRoot)
    {
        for (int i = chunkRoot.transform.childCount - 1; i >= 0; i--)
            Destroy(chunkRoot.transform.GetChild(i).gameObject);
        // Texturas cacheadas NÃO são destruídas — o sprite some, a textura fica
    }

    // ---- CHÃO (FIX 1: noise em WORLD-SPACE → zero emenda entre chunks) ----
    void GenerateGround(GameObject root, int chunkX, int chunkY, int biomeId,
        BiomeVisualConfig biome, float size)
    {
        var go = new GameObject("Ground");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -100; // convenção do projeto (layer Default)

        long key = GroundKey(chunkX, chunkY, biomeId);

        if (_groundTexCache.TryGetValue(key, out var cached) && cached != null)
        {
            AssignGroundSprite(sr, go, cached, size, key);
            return;
        }

        // FIX 2: placeholder sólido enquanto a textura é gerada (sem buraco)
        sr.sprite = GetWhitePixelSprite();
        sr.color = biome.groundBase;
        go.transform.localScale = new Vector3(size, size, 1f);

        StartCoroutine(BuildGroundAsync(sr, go, key, chunkX, chunkY, biome, size));
    }

    void AssignGroundSprite(SpriteRenderer sr, GameObject go, Texture2D tex,
        float size, long key)
    {
        int ts = tex.width;
        sr.color = Color.white;
        go.transform.localScale = Vector3.one;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, ts, ts),
            new Vector2(0.5f, 0.5f), ts / size, 0, SpriteMeshType.FullRect);

        // FIX 3: o GO segura a textura viva via refcount enquanto existir
        var rc = go.AddComponent<GroundTextureRef>();
        rc.key = key;
        _groundTexRefs.TryGetValue(key, out int n);
        _groundTexRefs[key] = n + 1;
    }

    public static void ReleaseGroundTexture(long key)
    {
        if (!_groundTexRefs.TryGetValue(key, out int n)) return;
        if (n <= 1) _groundTexRefs.Remove(key);
        else _groundTexRefs[key] = n - 1;
    }

    // FIX 2: textura gerada em fatias de rowsPerFrame linhas por frame.
    // Como o grid 7×7 cria chunks bem fora da câmera, a geração amortizada
    // termina antes do player alcançar o anel externo (pré-geração).
    IEnumerator BuildGroundAsync(SpriteRenderer sr, GameObject go, long key,
        int chunkX, int chunkY, BiomeVisualConfig biome, float size)
    {
        // outra coroutine já constrói esta key? aguardar e reaproveitar
        while (_building.Contains(key))
            yield return null;

        if (!_groundTexCache.TryGetValue(key, out var tex) || tex == null)
        {
            _building.Add(key);

            int ts = Mathf.Max(32, groundTexSize);
            tex = new Texture2D(ts, ts, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color[ts * ts];
            // FIX 1: UMA seed global p/ todo o chão — campo contínuo no mundo
            var noise = new SeededNoise(globalSeed);

            // FIX 4: paleta dessaturada 15% (não competir com pixel art)
            Color gShadow    = Desaturate(biome.groundShadow, 0.85f);
            Color gBase      = Desaturate(biome.groundBase, 0.85f);
            Color gAccent    = Desaturate(biome.groundAccent, 0.85f);
            Color gHighlight = Desaturate(biome.groundHighlight, 0.85f);

            float worldOX = chunkX * size;
            float worldOY = chunkY * size;
            int rows = Mathf.Max(8, rowsPerFrame);

            for (int y = 0; y < ts; y++)
            {
                // centro do pixel → bordas simétricas entre chunks vizinhos
                float v = (worldOY + ((y + 0.5f) / ts) * size) * 0.05f;
                for (int x = 0; x < ts; x++)
                {
                    float u = (worldOX + ((x + 0.5f) / ts) * size) * 0.05f;

                    float macro = noise.FBM(u, v, 3f, 4);
                    float mid   = noise.Smooth(u * 8f + 5f, v * 8f + 5f) * 0.5f;
                    float micro = noise.Smooth(u * 20f + 10f, v * 20f + 10f) * 0.25f;
                    float h = macro * 0.5f + mid * 0.35f + micro * 0.15f;

                    Color c;
                    if (h > 0.65f)
                        c = Color.Lerp(gAccent, gHighlight, (h - 0.65f) / 0.35f);
                    else if (h > 0.35f)
                        c = Color.Lerp(gBase, gAccent, (h - 0.35f) / 0.30f);
                    else
                        c = Color.Lerp(gShadow, gBase, h / 0.35f);

                    // Sombra direcional (luz topo-esquerda)
                    float sh = noise.FBM(u + 0.02f, v - 0.02f, 3f, 2);
                    c = Color.Lerp(c, gShadow, Mathf.Clamp01(macro - sh) * 0.3f);

                    // Rachaduras
                    if (noise.Get(u * 30f, v * 30f) > 0.92f)
                        c = Color.Lerp(c, gShadow, 0.5f);

                    // FIX 4: ruído fino (escala 40, ±8/255) — textura menos chapada
                    float fine = (noise.Get(u * 40f, v * 40f) - 0.5f) * (16f / 255f);
                    c.r = Mathf.Clamp01(c.r + fine);
                    c.g = Mathf.Clamp01(c.g + fine);
                    c.b = Mathf.Clamp01(c.b + fine);

                    pixels[y * ts + x] = c;
                }
                if ((y + 1) % rows == 0) yield return null; // amortização
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            CacheGroundTexture(key, tex);
            _building.Remove(key);
        }

        // o chunk pode ter sido reciclado durante a geração — textura fica no cache
        if (sr != null && go != null)
            AssignGroundSprite(sr, go, tex, size, key);
    }

    static void CacheGroundTexture(long key, Texture2D tex)
    {
        // FIX 3: eviction só destrói textura que NENHUM chunk ativo referencia
        int guard = _cacheOrder.Count;
        while (_groundTexCache.Count >= MAX_CACHED_TEXTURES && guard-- > 0)
        {
            long oldest = _cacheOrder.Dequeue();
            if (_groundTexRefs.TryGetValue(oldest, out int refs) && refs > 0)
            {
                _cacheOrder.Enqueue(oldest); // em uso — volta para o fim da fila
                continue;
            }
            if (_groundTexCache.TryGetValue(oldest, out var old) && old != null)
                Object.Destroy(old);
            _groundTexCache.Remove(oldest);
        }
        _groundTexCache[key] = tex;
        _cacheOrder.Enqueue(key);
    }

    static Sprite _whitePixelSprite;
    static Sprite GetWhitePixelSprite()
    {
        if (_whitePixelSprite == null)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGB24, false);
            t.SetPixel(0, 0, Color.white);
            t.Apply(false, false);
            _whitePixelSprite = Sprite.Create(t, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f), 1f);
        }
        return _whitePixelSprite;
    }

    static Color Desaturate(Color c, float satMult)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s * satMult), v);
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
            var pos = new Vector3(x, y, 0);
            CreateRock(root, pos, r, seed + i * 100, biomeId, biome);
            if (biomeId == 1 || biomeId == 4) // Khorduum/Arkenfall: rochas emissivas
                _emissiveSpots.Add(pos);
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

    // ---- ÁRVORES (Bloco D) ----
    void GenerateTrees(GameObject root, int seed, int biomeId, BiomeVisualConfig biome,
        float size, float densityMult)
    {
        var rng = new System.Random(seed + 2000);
        int count = Mathf.RoundToInt((biome.treeCount + rng.Next(2)) * densityMult);

        for (int i = 0; i < count; i++)
        {
            // Evitar o centro do chunk (área de combate)
            float x = 0f, y = 0f;
            bool ok = false;
            for (int tries = 0; tries < 8 && !ok; tries++)
            {
                x = (float)(rng.NextDouble() * size * 0.9f - size * 0.45f);
                y = (float)(rng.NextDouble() * size * 0.9f - size * 0.45f);
                ok = Mathf.Abs(x) >= size * 0.15f || Mathf.Abs(y) >= size * 0.15f;
            }
            if (!ok) continue;

            int treeSeed = seed + 7000 + i * 211;
            var pos = new Vector3(x, y, 0);

            if (biomeId == 1)
                CreateStalagmite(root, pos, treeSeed, biome);          // Khorduum: caverna
            else if (biomeId == 4 && rng.NextDouble() < 0.5)
                CreateBrokenPillar(root, pos, treeSeed, biome);        // Arkenfall: ruínas
            else
                CreateTree(root, pos, treeSeed, biomeId, biome, rng);
        }
    }

    void CreateTree(GameObject root, Vector3 localPos, int treeSeed, int biomeId,
        BiomeVisualConfig biome, System.Random rng)
    {
        var go = new GameObject("Tree");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;

        float worldY = go.transform.position.y;
        int sortOrder = Mathf.RoundToInt(-worldY * 0.1f + 50f);

        var noise = new SeededNoise(treeSeed);
        float trunkH = 1.2f + (float)rng.NextDouble() * 1.0f;
        float trunkW = 0.08f + noise.Get(treeSeed, 1) * 0.06f;
        float topR   = 0.5f + noise.Get(treeSeed, 2) * 0.5f;

        // Sombra no chão
        CreateEllipse(go, new Vector3(topR * 0.15f, -0.05f, 0),
            topR * 0.85f, topR * 0.28f, new Color(0, 0, 0, 0.28f), 40);

        // Tronco
        CreateQuad(go, new Vector3(0, trunkH * 0.5f, 0), trunkW, trunkH,
            biome.treeBase, biome.treeMid, sortOrder);

        // Galhos 2-3 (Valdross: caídos a -45°)
        float branchAngle = biomeId == 2 ? -45f : 30f;
        int branches = 2 + (int)(noise.Get(treeSeed, 3) * 2);
        for (int b = 0; b < branches; b++)
        {
            float bh = trunkH * (0.35f + b * 0.2f);
            float dir = noise.Get(treeSeed, 4 + b) > 0.5f ? 1f : -1f;
            float len = 0.3f + noise.Get(treeSeed, 5 + b) * 0.4f;
            CreateBranch(go, new Vector3(0, bh, 0), dir, len, trunkW * 0.5f,
                biome.treeMid, sortOrder, branchAngle);
        }

        // Copa — círculos irregulares sobrepostos (Valdross: esparsa, 2 camadas)
        int layers = biomeId == 2 ? 2 : 3 + (int)(noise.Get(treeSeed, 6) * 2);
        for (int l = layers - 1; l >= 0; l--)
        {
            float lr  = topR * (0.65f + l * 0.12f);
            float lox = (noise.Get(treeSeed, 7 + l) - 0.5f) * topR * 0.35f;
            float loy = trunkH + topR * 0.35f + l * topR * 0.18f;
            Color col = l == 0 ? biome.treeTopDark :
                        l == layers - 1 ? biome.treeTopLight : biome.treeMid;
            CreateIrregularCircle(go, new Vector3(lox, loy, 0), lr, 0.85f,
                treeSeed + l * 50, col, sortOrder + 1 + l);
        }
    }

    // Galho: quad fino saindo do tronco em ângulo
    void CreateBranch(GameObject parent, Vector3 origin, float dirSign, float len,
        float width, Color color, int sortOrder, float angleDeg = 30f)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 end = origin + new Vector3(Mathf.Cos(rad) * len * dirSign, Mathf.Sin(rad) * len, 0);
        CreateQuadLine(parent, origin, end, width, color, sortOrder);
    }

    // Círculo irregular de 8 pontos (camadas de copa, arbustos)
    void CreateIrregularCircle(GameObject parent, Vector3 localPos, float radius,
        float roundness, int seed, Color color, int sortOrder)
    {
        const int pts = 8;
        var noise = new SeededNoise(seed);
        var verts = new Vector3[pts + 1];
        verts[0] = localPos;
        for (int i = 0; i < pts; i++)
        {
            float a = (float)i / pts * Mathf.PI * 2f;
            float r = radius * (roundness + noise.Get(seed + i * 0.7f, 0) * (1f - roundness) * 2f);
            verts[i + 1] = localPos + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0);
        }
        CreateMeshGO(parent, verts, pts, color, color, sortOrder);
    }

    // Khorduum: estalagmite no lugar de árvore
    void CreateStalagmite(GameObject root, Vector3 localPos, int seed, BiomeVisualConfig biome)
    {
        var go = new GameObject("Stalagmite");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;

        float worldY = go.transform.position.y;
        int sortOrder = Mathf.RoundToInt(-worldY * 0.1f + 50f);

        var noise = new SeededNoise(seed);
        float h = 1.5f + noise.Get(seed, 0) * 1.5f;
        float w = 0.3f + noise.Get(seed, 1) * 0.3f;

        CreateEllipse(go, new Vector3(0, -0.03f, 0), w * 1.1f, w * 0.32f,
            new Color(0, 0, 0, 0.3f), 40);

        // Corpo afinando até a ponta (fan a partir do centro)
        var verts = new Vector3[6];
        verts[0] = new Vector3(0, h * 0.4f, 0);
        verts[1] = new Vector3(-w, 0, 0);
        verts[2] = new Vector3(w, 0, 0);
        verts[3] = new Vector3(w * 0.45f, h * 0.55f, 0);
        verts[4] = new Vector3((noise.Get(seed, 2) - 0.5f) * w * 0.3f, h, 0);
        verts[5] = new Vector3(-w * 0.45f, h * 0.55f, 0);
        CreateMeshGO(go, verts, 5, biome.rockColor, biome.rockDark, sortOrder);

        // Ponta de cristal emissivo
        CreateTriangle(go,
            new Vector3(-w * 0.18f, h * 0.75f, 0),
            new Vector3( w * 0.18f, h * 0.75f, 0),
            new Vector3(0, h * 1.15f, 0),
            biome.treeTopDark, biome.treeTopLight, sortOrder + 1);
    }

    // Arkenfall: pilar quebrado no lugar de árvore (50%)
    void CreateBrokenPillar(GameObject root, Vector3 localPos, int seed, BiomeVisualConfig biome)
    {
        var go = new GameObject("Pillar");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;

        float worldY = go.transform.position.y;
        int sortOrder = Mathf.RoundToInt(-worldY * 0.1f + 50f);

        var noise = new SeededNoise(seed);
        float h = 1.0f + noise.Get(seed, 0) * 1.2f;
        float w = 0.25f + noise.Get(seed, 1) * 0.15f;

        CreateEllipse(go, new Vector3(0.05f, -0.04f, 0), w * 1.4f, w * 0.4f,
            new Color(0, 0, 0, 0.3f), 40);

        // Corpo com topo quebrado (inclinado)
        var v = new Vector3[]
        {
            new Vector3(-w, 0, 0),
            new Vector3( w, 0, 0),
            new Vector3( w, h * (0.75f + noise.Get(seed, 2) * 0.25f), 0),
            new Vector3(-w, h, 0),
        };
        CreateQuadMesh(go, v, biome.treeBase, biome.treeTopLight, sortOrder);

        // Bloco caído ao lado
        CreateSimplePolygon(go,
            new Vector3(w * (1.5f + noise.Get(seed, 3)), w * 0.2f, 0),
            w * 0.5f, 5, seed + 9, biome.treeTopLight, biome.treeBase, sortOrder);
    }

    // ---- ARBUSTOS (Bloco D) ----
    void GenerateBushes(GameObject root, int seed, int biomeId, BiomeVisualConfig biome,
        float size, float densityMult)
    {
        var rng = new System.Random(seed + 3000);
        int count = Mathf.RoundToInt((biome.bushCount + rng.Next(3)) * densityMult);
        for (int i = 0; i < count; i++)
        {
            float x = (float)(rng.NextDouble() * size * 0.94f - size * 0.47f);
            float y = (float)(rng.NextDouble() * size * 0.94f - size * 0.47f);
            var pos = new Vector3(x, y, 0);
            CreateBush(root, pos, seed + 5000 + i * 131, biome);
            if (biomeId == 0 || biomeId == 2 || biomeId == 3) // Veremoth/Valdross/Gorveth
                _emissiveSpots.Add(pos);
        }
    }

    void CreateBush(GameObject root, Vector3 localPos, int seed, BiomeVisualConfig biome)
    {
        var go = new GameObject("Bush");
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = localPos;

        float worldY = go.transform.position.y;
        int sortOrder = Mathf.RoundToInt(-worldY * 0.1f + 50f);

        var noise = new SeededNoise(seed);
        float baseR = 0.25f + noise.Get(seed, 0) * 0.25f;

        CreateEllipse(go, new Vector3(0.05f, -baseR * 0.3f, 0),
            baseR * 1.1f, baseR * 0.35f, new Color(0, 0, 0, 0.25f), 40);

        // 3-5 círculos irregulares agrupados
        int blobs = 3 + (int)(noise.Get(seed, 1) * 3);
        for (int i = 0; i < blobs; i++)
        {
            float ox = (noise.Get(seed, 2 + i) - 0.5f) * baseR * 1.2f;
            float oy = noise.Get(seed, 6 + i) * baseR * 0.5f;
            float r = baseR * (0.5f + noise.Get(seed, 10 + i) * 0.4f);
            Color col = Color.Lerp(biome.bushColor, biome.bushLight, noise.Get(seed, 14 + i) * 0.6f);
            CreateIrregularCircle(go, new Vector3(ox, oy, 0), r, 0.8f,
                seed + i * 37, col, sortOrder + (i % 2));
        }
    }

    // ---- GRAMA E FLORES (Bloco D + Adendo 1) ----
    // Tudo numa mesh combinada por chunk: até ~100 lâminas viram 1 draw call
    void GenerateGrass(GameObject root, int seed, int biomeId, BiomeVisualConfig biome,
        float size, float densityMult)
    {
        if (biomeId == 1) return; // Khorduum: caverna, sem grama

        var rng = new System.Random(seed + 4000);
        int tufts = Mathf.RoundToInt((12 + rng.Next(9)) * densityMult);
        if (tufts <= 0) return;

        var verts = new List<Vector3>(tufts * 20);
        var cols  = new List<Color>(tufts * 20);
        var tris  = new List<int>(tufts * 30);
        var fverts = new List<Vector3>();
        var fcols  = new List<Color>();
        var ftris  = new List<int>();
        bool allowFlowers = biomeId != 4; // Adendo 1: sem flores em Khorduum (já saiu) e Arkenfall

        for (int i = 0; i < tufts; i++)
        {
            float x = (float)(rng.NextDouble() * size * 0.94f - size * 0.47f);
            float y = (float)(rng.NextDouble() * size * 0.94f - size * 0.47f);
            var basePos = new Vector3(x, y, 0);

            int blades = 3 + rng.Next(3); // 3-5 lâminas
            for (int b = 0; b < blades; b++)
            {
                float ang = ((float)rng.NextDouble() - 0.5f) * 40f * Mathf.Deg2Rad; // ±20°
                float h = 0.16f + (float)rng.NextDouble() * 0.08f;
                float vary = 0.85f + (float)rng.NextDouble() * 0.3f; // ±15%
                var col = new Color(
                    Mathf.Clamp01(biome.bushColor.r * vary),
                    Mathf.Clamp01(biome.bushColor.g * vary),
                    Mathf.Clamp01(biome.bushColor.b * vary), 1f);
                var p0 = basePos + new Vector3((b - blades * 0.5f) * 0.03f, 0, 0);
                var p1 = p0 + new Vector3(Mathf.Sin(ang) * h, Mathf.Cos(ang) * h, 0);
                AddQuadLine(verts, cols, tris, p0, p1, 0.03f, col);
            }

            // Flor: prob. 15% por tufo, polígono de 5 pontas na cor particleColor
            if (allowFlowers && rng.NextDouble() < 0.15)
            {
                float fr = 0.05f + (float)rng.NextDouble() * 0.03f;
                AddPolygonFan(fverts, fcols, ftris,
                    basePos + new Vector3(0, 0.12f, 0), fr, 5,
                    Color.Lerp(biome.particleColor, Color.white, 0.3f),
                    biome.particleColor, rng);
            }
        }

        BuildCombinedMesh(root, "Grass", verts, cols, tris, 41);
        if (fverts.Count > 0)
            BuildCombinedMesh(root, "Flowers", fverts, fcols, ftris, 42);
    }

    static void AddQuadLine(List<Vector3> v, List<Color> c, List<int> t,
        Vector3 a, Vector3 b, float width, Color color)
    {
        Vector3 d = b - a;
        Vector3 perp = new Vector3(-d.y, d.x, 0).normalized * (width * 0.5f);
        int i0 = v.Count;
        v.Add(a - perp); v.Add(a + perp); v.Add(b + perp); v.Add(b - perp);
        c.Add(color); c.Add(color); c.Add(color); c.Add(color);
        t.Add(i0); t.Add(i0 + 1); t.Add(i0 + 2);
        t.Add(i0); t.Add(i0 + 2); t.Add(i0 + 3);
    }

    static void AddPolygonFan(List<Vector3> v, List<Color> c, List<int> t,
        Vector3 center, float radius, int sides, Color colCenter, Color colEdge,
        System.Random rng)
    {
        int ci = v.Count;
        v.Add(center); c.Add(colCenter);
        for (int i = 0; i < sides; i++)
        {
            float a = (float)i / sides * Mathf.PI * 2f;
            float r = radius * (0.8f + (float)rng.NextDouble() * 0.4f);
            v.Add(center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0));
            c.Add(colEdge);
        }
        for (int i = 0; i < sides; i++)
        {
            t.Add(ci); t.Add(ci + 1 + i); t.Add(ci + 1 + (i + 1) % sides);
        }
    }

    void BuildCombinedMesh(GameObject parent, string name,
        List<Vector3> v, List<Color> c, List<int> t, int sortOrder)
    {
        if (v.Count == 0) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        go.AddComponent<RuntimeMeshAutoDestroy>();
        mr.sortingOrder = sortOrder;
        mr.sharedMaterial = GetSharedVertexColorMaterial();

        var mesh = new Mesh();
        mesh.SetVertices(v);
        mesh.SetColors(c);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
    }

    // ---- PENHASCOS (Adendo 3) ----
    void GenerateCliffs(GameObject root, int seed, int biomeId, BiomeVisualConfig biome, float size)
    {
        var noise = new SeededNoise(seed + 9000);
        if (noise.Get(seed, 0) > 0.25f) return; // 25% dos chunks

        var rng = new System.Random(seed + 9001);
        var go = new GameObject("Cliff");
        go.transform.SetParent(root.transform, false);

        // SEMPRE na borda do chunk (nunca no centro)
        int edge = rng.Next(4);
        float half = size * 0.5f;
        float along = (float)(rng.NextDouble() * size * 0.5f - size * 0.25f);
        Vector3 basePos = edge == 0 ? new Vector3(along,  half * 0.85f, 0)
                        : edge == 1 ? new Vector3(along, -half * 0.85f, 0)
                        : edge == 2 ? new Vector3(-half * 0.85f, along, 0)
                        :             new Vector3( half * 0.85f, along, 0);
        go.transform.localPosition = basePos;

        float worldY = go.transform.position.y;
        int sortOrder = Mathf.RoundToInt(-worldY * 0.1f + 50f);

        Color topColor = new Color(
            Mathf.Clamp01(biome.rockColor.r * 1.2f),
            Mathf.Clamp01(biome.rockColor.g * 1.2f),
            Mathf.Clamp01(biome.rockColor.b * 1.2f));

        int formations = 2 + rng.Next(3); // 2-4 polígonos agrupados
        float xCursor = -(formations - 1) * 0.9f;
        for (int f = 0; f < formations; f++)
        {
            float w = 1.5f + (float)rng.NextDouble() * 1.5f; // 1.5-3u
            float h = 2.5f + (float)rng.NextDouble() * 1.5f; // 2.5-4u
            var off = new Vector3(xCursor, (float)(rng.NextDouble() * 0.4 - 0.2), 0);
            xCursor += w * 0.6f;

            // Parede com gradiente vertical (base rockDark → topo +20% brilho)
            var v = new Vector3[]
            {
                off + new Vector3(-w * 0.5f, 0, 0),
                off + new Vector3( w * 0.5f, 0, 0),
                off + new Vector3( w * 0.42f, h, 0),
                off + new Vector3(-w * 0.45f, h * (0.92f + (float)rng.NextDouble() * 0.08f), 0),
            };
            CreateQuadMesh(go, v, biome.rockDark, topColor, sortOrder);

            // Faixa escura na base (ilusão de queda)
            CreateQuad(go, off + new Vector3(0, 0.075f, 0), w, 0.15f,
                new Color(0f, 0f, 0f, 0.55f), new Color(0f, 0f, 0f, 0.25f), sortOrder + 1);

            // Linha separando o topo da parede
            CreateQuad(go, off + new Vector3(0, h * 0.72f, 0), w * 0.92f, 0.05f,
                biome.rockDark, biome.rockDark, sortOrder + 1);
        }
        // Sem collider — apenas visual
    }

    // ---- FAKE LIGHTS (Bloco E — substituem Light2D, inexistente sem URP) ----
    readonly List<Vector3> _emissiveSpots = new();

    static Texture2D _radialTex;
    static Sprite _radialSprite;
    static Sprite GetRadialSprite()
    {
        if (_radialSprite == null)
        {
            const int S = 64;
            _radialTex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            _radialTex.filterMode = FilterMode.Bilinear;
            _radialTex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[S * S];
            float c = (S - 1) * 0.5f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * a * 255f)); // falloff quadrático
                }
            _radialTex.SetPixels32(px);
            _radialTex.Apply(false, false);
            _radialSprite = Sprite.Create(_radialTex, new Rect(0, 0, S, S),
                new Vector2(0.5f, 0.5f), S);
        }
        return _radialSprite;
    }

    void GenerateFakeLights(GameObject root, int seed, int biomeId,
        BiomeVisualConfig biome, float size)
    {
        var rng = new System.Random(seed + 6000);
        int count = 2 + rng.Next(3); // 2-4 por chunk

        for (int i = 0; i < count; i++)
        {
            // Junto a props emissivos do bioma; fallback: posição aleatória
            Vector3 pos = _emissiveSpots.Count > 0
                ? _emissiveSpots[rng.Next(_emissiveSpots.Count)]
                    + new Vector3((float)(rng.NextDouble() * 0.6 - 0.3),
                                  (float)(rng.NextDouble() * 0.6 - 0.3), 0)
                : new Vector3((float)(rng.NextDouble() * size * 0.8f - size * 0.4f),
                              (float)(rng.NextDouble() * size * 0.8f - size * 0.4f), 0);

            var go = new GameObject("FakeLight");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * (2f + (float)rng.NextDouble() * 2f); // 2-4u

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetRadialSprite();
            sr.sortingOrder = 85;
            var col = biome.particleColor;
            col.a = 0.25f;
            sr.color = col;

            var pulse = go.AddComponent<FakeLightPulse>();
            pulse.baseAlpha = 0.25f;
            pulse.speed = 0.8f + (float)rng.NextDouble();
            pulse.phase = (float)(rng.NextDouble() * Mathf.PI * 2f);
        }
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
