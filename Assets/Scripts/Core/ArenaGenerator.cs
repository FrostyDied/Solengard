using UnityEngine;
using UnityEngine.Tilemaps;

public class ArenaGenerator : MonoBehaviour
{
    [SerializeField] Tilemap groundTilemap;
    [SerializeField] Tilemap obstacleTilemap;
    [SerializeField] Sprite floorSprite;
    [SerializeField] Sprite wallSprite;

    void Start() => GenerateArena(30, 30);

    public void GenerateArena(int width, int height)
    {
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

        // Ground fill — batch to avoid per-tile overhead on large maps
        var groundBounds = new BoundsInt(ox, oy, 0, width, height, 1);
        var groundTiles  = new TileBase[width * height];
        System.Array.Fill(groundTiles, floorTile);
        groundTilemap.SetTilesBlock(groundBounds, groundTiles);

        // Border walls — 2 tiles thick on each side, batched to avoid per-tile collider rebuilds
        var wallPos = new System.Collections.Generic.List<Vector3Int>();
        for (int x = ox - 2; x < ox + width + 2; x++)
        {
            for (int t = 1; t <= 2; t++)
            {
                wallPos.Add(new Vector3Int(x, oy - t,              0));
                wallPos.Add(new Vector3Int(x, oy + height - 1 + t, 0));
            }
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
