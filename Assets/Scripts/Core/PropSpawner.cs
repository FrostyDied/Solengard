using System.Collections.Generic;
using UnityEngine;

public class PropSpawner : MonoBehaviour
{
    public static PropSpawner Instance { get; private set; }

    [Header("Prefabs de obstáculos")]
    [SerializeField] List<GameObject> obstaclePrefabs = new();

    [Header("Densidade")]
    [SerializeField] int   maxProps           = 12;
    [SerializeField] float spawnRadiusMin     = 18f;  // sempre fora da tela (orthoSize=14)
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
        if (obstaclePrefabs.Count == 0) LoadDefaultObstacles();
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

    // Distribui os props iniciais em anel ao redor do player antes do jogo começar.
    // Props aparecem fora do campo de visão desde o primeiro frame.
    void PrePopulate()
    {
        if (obstaclePrefabs.Count == 0) return;
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
        if (_activeProps.Count >= maxProps || obstaclePrefabs.Count == 0) return;

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

        if (obstaclePrefabs.Count == 0) return;
        var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
        if (prefab == null) return;

        var instance = Instantiate(prefab, pos, Quaternion.identity);
        instance.SetActive(true);
        instance.transform.SetParent(transform);

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer >= 0)
            SetLayerRecursive(instance, obstacleLayer);

        _activeProps.Add(instance);
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

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    void LoadDefaultObstacles()
    {
#if UNITY_EDITOR
        // Season3 prefabs prioritários — rochas e ruínas de grama
        string[] prefabPaths = {
            "Assets/Prefabs/Environment/Season3/GrasslandRock.prefab",
            "Assets/Prefabs/Environment/Season3/GrasslandRuin.prefab",
        };
        foreach (var p in prefabPaths)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) obstaclePrefabs.Add(go);
        }

        if (obstaclePrefabs.Count > 0) return;

        // Fallback: criar templates simples a partir dos PNGs individuais da Season3
        string[] spritePaths = {
            "Assets/Art/Environment/Season3_Grassland/Tileset/PNG/Objects_separated/Tree1.png",
            "Assets/Art/Environment/Season3_Grassland/Tileset/PNG/Objects_separated/Tree2.png",
            "Assets/Art/Environment/Season3_Grassland/Tileset/PNG/Objects_separated/Rpck_grass1.png",
            "Assets/Art/Environment/Season3_Grassland/Rocks/PNG/Objects_separately/Rock1_1.png",
        };
        foreach (var p in spritePaths)
        {
            var spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (spr == null) continue;
            var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(p));
            go.transform.SetParent(transform);
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = spr;
            sr.sortingOrder = 1;
            go.AddComponent<BoxCollider2D>();
            obstaclePrefabs.Add(go);
        }

        if (obstaclePrefabs.Count == 0)
            Debug.LogWarning("[PropSpawner] Nenhum prefab encontrado. Atribua no Inspector.");
#endif
    }
}
