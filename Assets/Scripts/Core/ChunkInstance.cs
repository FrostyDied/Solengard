using UnityEngine;
using System.Collections.Generic;

public class ChunkInstance : MonoBehaviour
{
    List<GameObject> _props = new();

    public void Populate(Vector2Int gridPos, int biome,
        List<GameObject> prefabs, int count, float size)
    {
        Debug.Log($"[Chunk] Populate: biome={biome}, prefabs={prefabs?.Count ?? 0}, count={count}");
        Clear();
        if (prefabs == null || prefabs.Count == 0) return;

        int seed = gridPos.x * 73856093 ^ gridPos.y * 19349663 ^ biome * 83492791;
        var rng  = new System.Random(seed);
        float half   = size * 0.5f;
        float margin = size * 0.1f;

        for (int i = 0; i < count; i++)
        {
            int pi = rng.Next(prefabs.Count);
            if (prefabs[pi] == null) continue;

            float x = (float)(rng.NextDouble() * (size - margin * 2) - (half - margin));
            float y = (float)(rng.NextDouble() * (size - margin * 2) - (half - margin));
            Vector3 pos = transform.position + new Vector3(x, y, 0);

            var go = Instantiate(prefabs[pi], pos, Quaternion.identity, transform);

            var ep = go.GetComponent<EnvironmentProp>();
            if (ep != null) ep.Initialize(seed + i * 1000);

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

            _props.Add(go);
        }
    }

    public void Repopulate(int biome, List<GameObject> prefabs, int count, float size)
    {
        var gridPos = new Vector2Int(
            Mathf.FloorToInt(transform.position.x / size),
            Mathf.FloorToInt(transform.position.y / size));
        Populate(gridPos, biome, prefabs, count, size);
    }

    public void Clear()
    {
        foreach (var p in _props) if (p != null) Destroy(p);
        _props.Clear();
    }
}
