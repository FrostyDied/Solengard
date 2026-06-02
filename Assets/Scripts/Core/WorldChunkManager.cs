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
    const int   GRID_RADIUS     = 2;    // 5×5 = 25 chunks ativos
    const int   PROPS_PER_CHUNK = 12;

    [HideInInspector] public BiomePropList[] biomeProps = new BiomePropList[5];

    int        _currentBiome = 0;
    Transform  _player;
    Vector2Int _lastChunk = new Vector2Int(int.MaxValue, int.MaxValue);
    Dictionary<Vector2Int, ChunkInstance> _active = new();
    Queue<ChunkInstance>                  _pool   = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        for (int i = 0; i < 5; i++)
            if (biomeProps[i] == null) biomeProps[i] = new BiomePropList();
    }

    void Start()
    {
        if (PlayerController.Instance != null)
            _player = PlayerController.Instance.transform;
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
        UpdateChunks();
    }

    void Update()
    {
        if (_player == null)
        {
            if (PlayerController.Instance != null)
                _player = PlayerController.Instance.transform;
            else
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
            }
            if (_player != null) UpdateChunks();
            return;
        }

        var c = ToChunk(_player.position);
        if (c != _lastChunk) { _lastChunk = c; UpdateChunks(); }
    }

    public void SetBiome(int b)
    {
        _currentBiome = Mathf.Clamp(b, 0, 4);
        var props = biomeProps[_currentBiome]?.prefabs;
        foreach (var kv in _active)
            kv.Value.Repopulate(_currentBiome, props, PROPS_PER_CHUNK, CHUNK_SIZE);
        Debug.Log($"[Chunks] Bioma trocado para {_currentBiome}");
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

        var props = biomeProps[_currentBiome]?.prefabs;
        foreach (var pos in needed)
        {
            if (_active.ContainsKey(pos)) continue;
            var chunk = _pool.Count > 0 ? _pool.Dequeue() : MakeChunk();
            chunk.transform.position = ToWorld(pos);
            chunk.Populate(pos, _currentBiome, props, PROPS_PER_CHUNK, CHUNK_SIZE);
            _active[pos] = chunk;
        }
    }

    ChunkInstance MakeChunk()
    {
        var go = new GameObject("Chunk");
        go.transform.SetParent(transform);
        return go.AddComponent<ChunkInstance>();
    }

    static Vector2Int ToChunk(Vector3 p) =>
        new Vector2Int(Mathf.FloorToInt(p.x / CHUNK_SIZE),
                       Mathf.FloorToInt(p.y / CHUNK_SIZE));

    static Vector3 ToWorld(Vector2Int c) =>
        new Vector3(c.x * CHUNK_SIZE + CHUNK_SIZE * 0.5f,
                    c.y * CHUNK_SIZE + CHUNK_SIZE * 0.5f, 0);
}
