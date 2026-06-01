using System.Collections.Generic;
using UnityEngine;

public class PropSpawner : MonoBehaviour
{
    public static PropSpawner Instance { get; private set; }

    [Header("Densidade")]
    [SerializeField] int   maxProps           = 12;
    [SerializeField] float spawnRadiusMin     = 18f;
    [SerializeField] float spawnRadiusMax     = 26f;
    [SerializeField] float despawnRadius      = 32f;
    [SerializeField] float minSpacing         = 8f;
    [SerializeField] float spawnCheckInterval = 1f;

    Transform        _player;
    List<GameObject> _activeProps = new();
    float            _timer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        PrePopulate();
    }

    void Update()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _timer = spawnCheckInterval;
        CleanupDistantProps();
        TrySpawnProp();
    }

    // Distribui props sombrios em anel ao redor do player antes do primeiro frame.
    void PrePopulate()
    {
        Vector3 center = _player != null ? _player.position : Vector3.zero;
        for (int i = 0; i < maxProps; i++)
        {
            float angle = (Mathf.PI * 2f / maxProps) * i + Random.Range(-0.2f, 0.2f);
            float dist  = Random.Range(spawnRadiusMin, spawnRadiusMax);
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
            SpawnPropAt(pos);
        }
    }

    void TrySpawnProp()
    {
        if (_activeProps.Count >= maxProps) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = Random.Range(spawnRadiusMin, spawnRadiusMax);
        Vector3 pos = _player.position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
        SpawnPropAt(pos);
    }

    void SpawnPropAt(Vector3 pos)
    {
        foreach (var prop in _activeProps)
        {
            if (prop == null) continue;
            if (Vector3.Distance(prop.transform.position, pos) < minSpacing) return;
        }

        var go = CreateProceduralProp(pos);
        go.transform.SetParent(transform);
        _activeProps.Add(go);
    }

    GameObject CreateProceduralProp(Vector3 pos)
    {
        var types = System.Enum.GetValues(typeof(ProceduralProps.PropType));
        var type  = (ProceduralProps.PropType)types.GetValue(Random.Range(0, types.Length));
        int seed  = Random.Range(0, 99999);

        var go = new GameObject($"Prop_{type}");
        go.transform.position = pos;

        var sr       = go.AddComponent<SpriteRenderer>();
        sr.sprite    = ProceduralProps.Generate(type, seed);
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        bool blocks = type != ProceduralProps.PropType.Bones
                   && type != ProceduralProps.PropType.Crystal;
        if (blocks)
        {
            var col    = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.offset = new Vector2(0f, -0.3f);
            var rb         = go.AddComponent<Rigidbody2D>();
            rb.bodyType    = RigidbodyType2D.Static;
        }

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer >= 0) go.layer = obstacleLayer;

        if ((type == ProceduralProps.PropType.DeadTree || type == ProceduralProps.PropType.Tombstone)
            && Random.value < 0.25f)
            AddRedEyes(go);

        return go;
    }

    void AddRedEyes(GameObject parent)
    {
        var eyes = new GameObject("RedEyes");
        eyes.transform.SetParent(parent.transform);
        eyes.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        var sr       = eyes.AddComponent<SpriteRenderer>();
        sr.sprite    = MakeRedEyesSprite();
        sr.sortingOrder = 60;
        eyes.AddComponent<PulsingGlow>();
    }

    Sprite MakeRedEyesSprite()
    {
        var tex = new Texture2D(8, 4);
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 4; j++)
                tex.SetPixel(i, j, new Color(0, 0, 0, 0));

        var red = new Color(1f, 0.15f, 0.1f, 1f);
        tex.SetPixel(1, 2, red); tex.SetPixel(2, 2, red);
        tex.SetPixel(5, 2, red); tex.SetPixel(6, 2, red);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 8, 4), new Vector2(0.5f, 0.5f), 16f);
    }

    void CleanupDistantProps()
    {
        for (int i = _activeProps.Count - 1; i >= 0; i--)
        {
            if (_activeProps[i] == null) { _activeProps.RemoveAt(i); continue; }
            if (Vector3.Distance(_activeProps[i].transform.position, _player.position) > despawnRadius)
            {
                Destroy(_activeProps[i]);
                _activeProps.RemoveAt(i);
            }
        }
    }
}
