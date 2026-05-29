using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(100)]
public class ArenaGenerator : MonoBehaviour
{
    [SerializeField] Tilemap groundTilemap;
    [SerializeField] Tilemap obstacleTilemap;
    [SerializeField] Sprite floorSprite;
    [SerializeField] Sprite wallSprite;

    void Start() => GenerateArena(30, 30);

    public void GenerateArena(int width, int height)
    {
        width  = Mathf.Clamp(width,  1, 50);
        height = Mathf.Clamp(height, 1, 50);

        if (groundTilemap == null || obstacleTilemap == null)
        {
            Debug.LogWarning("[ArenaGenerator] Tilemaps não atribuídos — arena não gerada.");
            return;
        }

        if (floorSprite == null || wallSprite == null)
        {
            Debug.LogWarning("[ArenaGenerator] Sprites não atribuídos — arena não gerada.");
            return;
        }

        groundTilemap.ClearAllTiles();
        obstacleTilemap.ClearAllTiles();

        var floorTile = MakeTile(floorSprite);
        var wallTile  = MakeTile(wallSprite);

        int ox = -width  / 2;
        int oy = -height / 2;

        // Ground — single batch call
        var groundTiles = new TileBase[width * height];
        System.Array.Fill(groundTiles, floorTile);
        groundTilemap.SetTilesBlock(new BoundsInt(ox, oy, 0, width, height, 1), groundTiles);

        // Walls — collect all positions then single batch call
        var wallPos = new List<Vector3Int>();
        for (int x = ox - 2; x < ox + width + 2; x++)
            for (int t = 1; t <= 2; t++)
            {
                wallPos.Add(new Vector3Int(x, oy - t,              0));
                wallPos.Add(new Vector3Int(x, oy + height - 1 + t, 0));
            }
        for (int y = oy; y < oy + height; y++)
            for (int t = 1; t <= 2; t++)
            {
                wallPos.Add(new Vector3Int(ox - t,             y, 0));
                wallPos.Add(new Vector3Int(ox + width - 1 + t, y, 0));
            }

        var wallTiles = new TileBase[wallPos.Count];
        System.Array.Fill(wallTiles, wallTile);
        obstacleTilemap.SetTiles(wallPos.ToArray(), wallTiles);

        // Fit camera to arena
        if (Camera.main != null)
            Camera.main.orthographicSize = Mathf.Max(width, height) * 0.6f;
    }

    private Tile MakeTile(Sprite sprite)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        return tile;
    }
}
