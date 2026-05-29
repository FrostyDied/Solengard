using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-100)]
public class ArenaGenerator : MonoBehaviour
{
    [SerializeField] Tilemap     groundTilemap;
    [SerializeField] Tilemap     obstacleTilemap;
    [SerializeField] CameraFollow cameraFollow;

    void Start() => GenerateArena(50, 50);

    public void GenerateArena(int width, int height)
    {
        width  = Mathf.Clamp(width,  1, 50);
        height = Mathf.Clamp(height, 1, 50);

        if (groundTilemap == null)
            groundTilemap = GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();
        if (obstacleTilemap == null)
            obstacleTilemap = GameObject.Find("ObstacleTilemap")?.GetComponent<Tilemap>();

        var floorTile = MakeSolidTile(new Color(0.267f, 0.267f, 0.267f));
        var wallTile  = MakeSolidTile(new Color(0.133f, 0.133f, 0.133f));
        Debug.Log($"[Arena] Gerando {width}x{height} tiles, PPU={floorTile.sprite?.pixelsPerUnit}");
        Debug.Log($"[Arena] ground={groundTilemap != null} obstacle={obstacleTilemap != null} floorTile={floorTile != null} wallTile={wallTile != null}");

        if (groundTilemap == null || obstacleTilemap == null)
        {
            Debug.LogWarning("[ArenaGenerator] Tilemaps não encontrados — arena não gerada.");
            return;
        }

        groundTilemap.ClearAllTiles();
        obstacleTilemap.ClearAllTiles();

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

        // Camera
        if (Camera.main != null)
            Camera.main.orthographicSize = 10f;

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            float boundsX = 18f, boundsY = 18f;
            cameraFollow.SetBounds(-boundsX, boundsX, -boundsY, boundsY);
            Debug.Log($"[Arena] Bounds setados: ±{boundsX} ±{boundsY}");
        }
        else
        {
            Debug.LogWarning("[Arena] CameraFollow não encontrado — bounds não aplicados.");
        }
    }

    private Tile MakeSolidTile(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color  = color;
        return tile;
    }
}
