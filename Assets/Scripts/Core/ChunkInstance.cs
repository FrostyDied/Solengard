using UnityEngine;
using System.Collections.Generic;

public class ChunkInstance : MonoBehaviour
{
    public static readonly Color[] BIOME_TINTS =
    {
        new Color(0.55f, 0.62f, 0.50f), // Veremoth — verde escuro sombrio
        new Color(0.45f, 0.50f, 0.65f), // Khorduum — azul pedra escuro
        new Color(0.55f, 0.52f, 0.58f), // Valdross — cinza roxo
        new Color(0.48f, 0.58f, 0.45f), // Gorveth — verde pântano
        new Color(0.62f, 0.52f, 0.45f), // Arkenfall — marrom ferrugem
    };

    List<GameObject> _props = new();

    public void Populate(Vector2Int gridPos, int biome,
        List<GameObject> prefabs, int count, float size)
    {
        Clear();

        // Motor procedural (Fase 2): chão procedural + props Craftpix
        // posicionados pelo motor (híbrido). O motor aplica tint e Y-sort.
        if (ProceduralSceneGenerator.Instance != null)
        {
            ProceduralSceneGenerator.Instance.GenerateChunk(
                gameObject, gridPos.x, gridPos.y, biome, size, prefabs, count);
            return;
        }

        // FALLBACK Craftpix — código original intacto
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning($"[Chunk] Populate abortado: prefabs null ou vazio para bioma {biome} em {gridPos}");
            return;
        }

        int nullCount = 0;
        foreach (var p in prefabs) if (p == null) nullCount++;
        if (nullCount > 0)
            Debug.LogWarning($"[Chunk] {nullCount}/{prefabs.Count} prefabs são null para bioma {biome}");

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
            if (sr != null)
            {
                sr.color        = biome < BIOME_TINTS.Length ? BIOME_TINTS[biome] : Color.white;
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 0.1f) + 50;
            }

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
        if (ProceduralSceneGenerator.Instance != null)
        {
            ProceduralSceneGenerator.Instance.ClearChunk(gameObject);
            _props.Clear();
            return;
        }

        // FALLBACK original
        foreach (var p in _props) if (p != null) Destroy(p);
        _props.Clear();
    }
}
