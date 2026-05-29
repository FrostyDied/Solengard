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

        groundTilemap.ClearAllTiles();
        obstacleTilemap.ClearAllTiles();

        var floorTile = MakeTile(floorSprite);
        var wallTile  = MakeTile(wallSprite);

        int ox = -width  / 2;
        int oy = -height / 2;

        // Ground fill
        for (int x = ox; x < ox + width; x++)
            for (int y = oy; y < oy + height; y++)
                groundTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);

        // Border walls — 2 tiles thick on each side
        for (int x = ox - 2; x < ox + width + 2; x++)
        {
            for (int t = 1; t <= 2; t++)
            {
                obstacleTilemap.SetTile(new Vector3Int(x, oy - t,              0), wallTile);
                obstacleTilemap.SetTile(new Vector3Int(x, oy + height - 1 + t, 0), wallTile);
            }
        }
        for (int y = oy; y < oy + height; y++)
        {
            for (int t = 1; t <= 2; t++)
            {
                obstacleTilemap.SetTile(new Vector3Int(ox - t,             y, 0), wallTile);
                obstacleTilemap.SetTile(new Vector3Int(ox + width - 1 + t, y, 0), wallTile);
            }
        }
    }

    private Tile MakeTile(Sprite sprite)
    {
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        return tile;
    }
}
