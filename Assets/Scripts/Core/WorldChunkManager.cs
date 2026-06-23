using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(100)]
public class WorldChunkManager : MonoBehaviour
{
    public static WorldChunkManager Instance { get; private set; }

    [System.Serializable]
    public class BiomePropList
    {
        public List<GameObject> prefabs = new();
    }

    const float CHUNK_SIZE      = 20f;
    const int   GRID_RADIUS     = 3;    // 7×7 = 49 chunks ativos (140×140u)
    const int   PROPS_PER_CHUNK = 12;

    static readonly string[][] BIOME_RESOURCE_NAMES = new string[][]
    {
        new[]{ "Veremoth_Tree","Veremoth_Bush","Veremoth_Mushroom","Veremoth_Rock","Veremoth_Ruin" },
        new[]{ "Khorduum_Crystal","Khorduum_Stone","Khorduum_Mushroom","Khorduum_Object" },
        new[]{ "Valdross_Grave","Valdross_Bones","Valdross_Tree","Valdross_Object" },
        new[]{ "Gorveth_Tree","Gorveth_Plant","Gorveth_Object","Gorveth_Mushroom" },
        new[]{ "Arkenfall_Rock","Arkenfall_Ruin","Arkenfall_Bones","Arkenfall_Tree" },
    };

    [HideInInspector] public BiomePropList[] biomeProps = new BiomePropList[11];
    [HideInInspector] public BiomePropList sharedNeutralProps = new();

    int        _currentBiome = 0;
    Transform  _player;
    Vector2Int _lastChunk = new Vector2Int(int.MaxValue, int.MaxValue);
    Dictionary<Vector2Int, ChunkInstance> _active = new();
    Queue<ChunkInstance>                  _pool   = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        for (int i = 0; i < biomeProps.Length; i++)
            if (biomeProps[i] == null) biomeProps[i] = new BiomePropList();
        if (sharedNeutralProps == null) sharedNeutralProps = new BiomePropList();
    }

    void Start()
    {
        // Diagnóstico: verificar se biomeProps foi serializado corretamente
        Debug.Log($"[Chunks] Start: biomeProps.Length={biomeProps?.Length ?? 0}");
        int totalPrefabs = 0;
        for (int i = 0; i < biomeProps?.Length; i++)
        {
            int count = biomeProps[i]?.prefabs?.Count ?? 0;
            Debug.Log($"[Chunks] Bioma {i}: {count} prefabs serializados");
            totalPrefabs += count;
        }

        // Se biomeProps está vazio, carregar via Resources.Load como fallback
        if (totalPrefabs == 0)
        {
            Debug.LogWarning("[Chunks] biomeProps vazio em runtime — usando fallback Resources.Load");
            LoadBiomePropsFromResources();
        }

        if (PlayerController.Instance != null)
            _player = PlayerController.Instance.transform;
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
        // Pré-geração síncrona do anel 3×3 central: chão textured antes do frame 1.
        // O anel externo (raio 2-3) continua assíncrono, ordenado por distância.
        var gen = ProceduralSceneGenerator.Instance;
        if (gen != null && _player != null)
        {
            var c0      = ToChunk(_player.position);
            var props0   = biomeProps[_currentBiome]?.prefabs;
            var neutral0 = sharedNeutralProps?.prefabs;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var pos   = c0 + new Vector2Int(dx, dy);
                var chunk = _pool.Count > 0 ? _pool.Dequeue() : MakeChunk();
                chunk.transform.position = ToWorld(pos);
                gen.GenerateChunkSync(chunk.gameObject, pos.x, pos.y,
                    _currentBiome, CHUNK_SIZE, props0, PROPS_PER_CHUNK, neutral0);
                _active[pos] = chunk;
            }
        }

        UpdateChunks(); // anel externo — async, ordenado por distância
    }

    void LoadBiomePropsFromResources()
    {
        biomeProps = new BiomePropList[5];
        for (int b = 0; b < 5; b++)
        {
            biomeProps[b] = new BiomePropList();
            foreach (var name in BIOME_RESOURCE_NAMES[b])
            {
                var go = Resources.Load<GameObject>($"Environment/Rich/{name}");
                if (go != null)
                    biomeProps[b].prefabs.Add(go);
                else
                    Debug.LogWarning($"[Chunks] Resources.Load falhou: Environment/Rich/{name}");
            }
            Debug.Log($"[Chunks] Bioma {b} carregado via Resources: {biomeProps[b].prefabs.Count} prefabs");
        }
    }

    void Update()
    {
        // Detecta player destruído ou inativo (recriado pelo RebuildGameScene)
        if (_player == null || !_player.gameObject.activeInHierarchy)
        {
            _player = null;
            if (PlayerController.Instance != null)
                _player = PlayerController.Instance.transform;
            else
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
            }
            if (_player != null)
            {
                _lastChunk = new Vector2Int(int.MaxValue, int.MaxValue);
                UpdateChunks();
            }
            return;
        }

        var c = ToChunk(_player.position);
        if (c != _lastChunk) { _lastChunk = c; UpdateChunks(); }
    }

    ProceduralFog       _fog;
    ProceduralParticles _particles;
    bool                _atmoSearched;

    public void SetBiome(int b)
    {
        _currentBiome = Mathf.Clamp(b, 0, biomeProps.Length - 1);

        // Propaga o bioma para névoa e partículas (Find só uma vez — refs cacheadas)
        if (!_atmoSearched)
        {
            _fog          = FindFirstObjectByType<ProceduralFog>();
            _particles    = FindFirstObjectByType<ProceduralParticles>();
            _atmoSearched = true;
        }
        _fog?.SetBiome(_currentBiome);
        _particles?.SetBiome(_currentBiome);

        var props   = biomeProps[_currentBiome]?.prefabs;
        var neutral = sharedNeutralProps?.prefabs;
        int count = props?.Count ?? 0;
        Debug.Log($"[Chunks] SetBiome({b}): {count} prefabs específicos + {neutral?.Count ?? 0} neutros");
        foreach (var kv in _active)
            kv.Value.Repopulate(_currentBiome, props, PROPS_PER_CHUNK, CHUNK_SIZE, neutral);
    }

    void UpdateChunks()
    {
        if (_player == null) return;
        var center = ToChunk(_player.position);
        var needed = new HashSet<Vector2Int>();
        for (int x = -GRID_RADIUS; x <= GRID_RADIUS; x++)
            for (int y = -GRID_RADIUS; y <= GRID_RADIUS; y++)
                needed.Add(center + new Vector2Int(x, y));
        var remove = new List<Vector2Int>();
        foreach (var kv in _active)
            if (!needed.Contains(kv.Key)) remove.Add(kv.Key);
        foreach (var k in remove)
        {
            _pool.Enqueue(_active[k]);
            _active[k].Clear();
            _active.Remove(k);
        }

        var props   = biomeProps[_currentBiome]?.prefabs;
        var neutral = sharedNeutralProps?.prefabs;

        // Ordenar por distância: o chunk mais próximo inicia a build de textura
        // primeiro — com o lock serial do ProceduralSceneGenerator, a ordem de
        // início das coroutines é a ordem de conclusão das texturas.
        var toPopulate = new List<Vector2Int>();
        foreach (var pos in needed)
            if (!_active.ContainsKey(pos)) toPopulate.Add(pos);
        toPopulate.Sort((a, b) =>
            (a - center).sqrMagnitude.CompareTo((b - center).sqrMagnitude));

        foreach (var pos in toPopulate)
        {
            var chunk = _pool.Count > 0 ? _pool.Dequeue() : MakeChunk();
            chunk.transform.position = ToWorld(pos);
            chunk.Populate(pos, _currentBiome, props, PROPS_PER_CHUNK, CHUNK_SIZE, neutral);
            _active[pos] = chunk;
        }
    }

    ChunkInstance MakeChunk()
    {
        var go = new GameObject("Chunk");
        go.transform.SetParent(transform);
        return go.AddComponent<ChunkInstance>();
    }

    void OnDestroy()
    {
        foreach (var kv in _active)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        _active.Clear();
        while (_pool.Count > 0)
        {
            var chunk = _pool.Dequeue();
            if (chunk != null) Destroy(chunk.gameObject);
        }
        _pool.Clear();
    }

    static Vector2Int ToChunk(Vector3 p) =>
        new Vector2Int(Mathf.FloorToInt(p.x / CHUNK_SIZE),
                       Mathf.FloorToInt(p.y / CHUNK_SIZE));

    static Vector3 ToWorld(Vector2Int c) =>
        new Vector3(c.x * CHUNK_SIZE + CHUNK_SIZE * 0.5f,
                    c.y * CHUNK_SIZE + CHUNK_SIZE * 0.5f, 0);
}
