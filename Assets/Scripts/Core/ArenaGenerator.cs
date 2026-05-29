using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// TODO: re-enable Start() and GenerateArena() after resolving the Unity freeze.
// Root cause suspected: SetTilesBlock / SetTiles on a Tilemap with CompositeCollider2D
// (GenerationType = Synchronous) triggers a full physics geometry rebuild synchronously,
// which blocks the main thread even inside a coroutine. Fix options:
//   1. Switch CompositeCollider2D to GenerationType.Manual and call GenerateGeometry()
//      once after all tiles are placed.
//   2. Replace runtime tile generation with a pre-built tilemap saved in the scene.
[DefaultExecutionOrder(100)]
public class ArenaGenerator : MonoBehaviour
{
    [SerializeField] Tilemap groundTilemap;
    [SerializeField] Tilemap obstacleTilemap;
    [SerializeField] Sprite floorSprite;
    [SerializeField] Sprite wallSprite;

    void Start()
    {
        // DISABLED — causes Unity freeze due to CompositeCollider2D synchronous rebuild.
        // StartCoroutine(GenerateArenaCoroutine(30, 30));
    }

    public void GenerateArena(int width, int height)
    {
        // DISABLED — see Start() comment above.
        // StartCoroutine(GenerateArenaCoroutine(width, height));
    }

    public IEnumerator GenerateArenaCoroutine(int width, int height)
    {
        width  = Mathf.Clamp(width,  1, 50);
        height = Mathf.Clamp(height, 1, 50);

        if (groundTilemap == null || obstacleTilemap == null)
        {
            Debug.LogWarning("[ArenaGenerator] Tilemaps não atribuídos — arena não gerada.");
            yield break;
        }

        if (floorSprite == null || wallSprite == null)
        {
            Debug.LogWarning("[ArenaGenerator] Sprites não atribuídos — arena não gerada.");
            yield break;
        }

        groundTilemap.ClearAllTiles();
        obstacleTilemap.ClearAllTiles();

        var floorTile = MakeTile(floorSprite);
        var wallTile  = MakeTile(wallSprite);

        int ox = -width  / 2;
        int oy = -height / 2;

        // Ground fill — build array with frame yields, then one batch call
        var groundTiles = new TileBase[width * height];
        for (int i = 0; i < groundTiles.Length; i++)
        {
            groundTiles[i] = floorTile;
            if (i > 0 && i % 100 == 0) yield return null;
        }
        groundTilemap.SetTilesBlock(new BoundsInt(ox, oy, 0, width, height, 1), groundTiles);

        yield return null;

        // Wall border — collect positions with frame yields, then one batch call
        var wallPos = new List<Vector3Int>();
        int col = 0;
        for (int x = ox - 2; x < ox + width + 2; x++)
        {
            for (int t = 1; t <= 2; t++)
            {
                wallPos.Add(new Vector3Int(x, oy - t,              0));
                wallPos.Add(new Vector3Int(x, oy + height - 1 + t, 0));
            }
            if (++col % 100 == 0) yield return null;
        }
        for (int y = oy; y < oy + height; y++)
        {
            for (int t = 1; t <= 2; t++)
            {
                wallPos.Add(new Vector3Int(ox - t,             y, 0));
                wallPos.Add(new Vector3Int(ox + width - 1 + t, y, 0));
            }
        }

        var wallTilesArr = new TileBase[wallPos.Count];
        System.Array.Fill(wallTilesArr, wallTile);
        obstacleTilemap.SetTiles(wallPos.ToArray(), wallTilesArr);
    }

    private Tile MakeTile(Sprite sprite)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        return tile;
    }
}
