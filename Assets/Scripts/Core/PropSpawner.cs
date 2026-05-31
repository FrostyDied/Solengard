using System.Collections.Generic;
using UnityEngine;

public class PropSpawner : MonoBehaviour
{
    public static PropSpawner Instance { get; private set; }

    [Header("Prefabs de obstáculos")]
    [SerializeField] List<GameObject> obstaclePrefabs = new();

    [Header("Densidade — ESPARSO")]
    [SerializeField] int   maxProps           = 12;
    [SerializeField] float spawnRadius        = 25f;
    [SerializeField] float despawnRadius      = 35f;
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

    void TrySpawnProp()
    {
        if (_activeProps.Count >= maxProps || obstaclePrefabs.Count == 0) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = Random.Range(spawnRadius * 0.6f, spawnRadius);
        Vector3 pos = _player.position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);

        foreach (var prop in _activeProps)
        {
            if (prop == null) continue;
            if (Vector3.Distance(prop.transform.position, pos) < minSpacing) return;
        }

        var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
        if (prefab == null) return;

        var instance = Instantiate(prefab, pos, Quaternion.identity);
        instance.transform.SetParent(transform);
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

    void LoadDefaultObstacles()
    {
#if UNITY_EDITOR
        string[] paths =
        {
            "Assets/Prefabs/Environment/Season1/DungeonObject.prefab",
            "Assets/Prefabs/Environment/Season2/ForestTree.prefab",
            "Assets/Prefabs/Environment/Season3/GrasslandRock.prefab",
            "Assets/Prefabs/Environment/Season3/GrasslandRuin.prefab",
        };
        foreach (var p in paths)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) obstaclePrefabs.Add(go);
        }
#endif
    }
}
