using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SimpleArena : MonoBehaviour
{
    public static SimpleArena Instance { get; private set; }

    [Header("Tamanho do chão (cobre a câmera com folga)")]
    [SerializeField] float tileAreaSize  = 60f;

    [Header("Snap do tile (calculado automaticamente a partir do sprite)")]
    [SerializeField] float tileWorldSize = 2f;

    [Header("Sprite do tileset (auto se vazio)")]
    [SerializeField] Sprite floorSprite;

    [Header("Cor de fallback")]
    [SerializeField] Color fallbackColor = new Color(0.15f, 0.13f, 0.17f);

    Transform      _player;
    Transform      _floor;
    SpriteRenderer _floorRenderer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildFloor();
    }

    void Start() => FindPlayer();

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void BuildFloor()
    {
        if (floorSprite == null) floorSprite = LoadFloorSprite();
        if (floorSprite == null) floorSprite = GenerateProceduralFloor();

        // Derive the snap grid size from the chosen tile so the grid aligns exactly.
        if (floorSprite != null)
            tileWorldSize = floorSprite.rect.width / floorSprite.pixelsPerUnit;

        var go = new GameObject("InfiniteFloor");
        go.transform.SetParent(transform);
        _floor         = go.transform;
        _floorRenderer = go.AddComponent<SpriteRenderer>();
        _floorRenderer.sortingOrder = -100;

        if (floorSprite != null)
        {
            _floorRenderer.sprite   = floorSprite;
            _floorRenderer.drawMode = SpriteDrawMode.Tiled;
            _floorRenderer.size     = new Vector2(tileAreaSize, tileAreaSize);
            _floorRenderer.tileMode = SpriteTileMode.Continuous;
            Debug.Log($"[SimpleArena] Chão: {floorSprite.name} ({floorSprite.rect.width}x{floorSprite.rect.height}px) tileWorldSize={tileWorldSize:F2}");
        }
        else
        {
            _floorRenderer.sprite   = MakePixel(Color.white);
            _floorRenderer.color    = fallbackColor;
            go.transform.localScale = new Vector3(tileAreaSize, tileAreaSize, 1f);
            Debug.LogWarning("[SimpleArena] Sem tileset — cor sólida.");
        }
    }

    void LateUpdate()
    {
        if (_player == null) { FindPlayer(); return; }
        if (_floor == null) return;

        // Snap ao grid do tile: Floor em vez de Round para o chão "pular" em fronteiras fixas,
        // evitando o efeito de textura "grudada" que acompanha o player continuamente.
        Vector3 p = _player.position;
        float snappedX = Mathf.Floor(p.x / tileWorldSize) * tileWorldSize;
        float snappedY = Mathf.Floor(p.y / tileWorldSize) * tileWorldSize;
        _floor.position = new Vector3(snappedX, snappedY, 0f);
    }

    Sprite LoadFloorSprite()
    {
#if UNITY_EDITOR
        // Strategy 1: extract a floor cell directly from walls_floor.png by pixel region.
        // walls_floor is 224x352 with 16px cells. The central stone floor tile sits at
        // approximately (x=96, y=16) — adjust via "Solengard/Debug/Preview Floor Tile".
        const string WF_PATH = "Assets/Art/Environment/Season1_Dungeon/Tileset/PNG/walls_floor.png";

        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(WF_PATH);
        if (tex != null)
        {
            // Ensure the texture is CPU-readable so Sprite.Create can reference a sub-rect.
            var importer = UnityEditor.AssetImporter.GetAtPath(WF_PATH) as UnityEditor.TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(WF_PATH);
            }

            // Cell (96, 16) size 16x16 — central column, first floor row from bottom.
            // Unity's Rect origin is bottom-left, matching texture pixel coordinates.
            int cellX = 96, cellY = 16, cell = 16;
            var spr = Sprite.Create(tex,
                new Rect(cellX, cellY, cell, cell),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 16f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect);
            spr.name = "FloorTile_Extracted";
            Debug.Log($"[SimpleArena] Tile extraído de walls_floor em ({cellX},{cellY}) {cell}x{cell}px");
            return spr;
        }

        // Strategy 2: search for individual sub-sprites in the tileset folder.
        string[] folders = { "Assets/Art/Environment/Season1_Dungeon/Tileset/PNG" };
        string[] exclude = { "wall", "corner", "edge", "top", "bottom", "side",
                             "ornament", "claw", "torch", "decor", "transition",
                             "arch", "pillar", "door", "window", "border" };
        string[] prefer  = { "floor", "ground", "center", "tile", "stone", "dirt" };

        var allSprites = new System.Collections.Generic.List<Sprite>();
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", folders))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                if (a is Sprite s) allSprites.Add(s);
        }

        foreach (string pref in prefer)
        {
            foreach (Sprite s in allSprites)
            {
                string n = s.name.ToLower();
                if (exclude.Any(e => n.Contains(e))) continue;
                if (!n.Contains(pref)) continue;
                if (Mathf.Abs(s.rect.width - s.rect.height) < 2f)
                {
                    Debug.Log($"[SimpleArena] Sub-sprite de chão: {s.name} ({s.rect.width}x{s.rect.height}px)");
                    return s;
                }
            }
        }

        foreach (Sprite s in allSprites)
        {
            string n = s.name.ToLower();
            if (exclude.Any(e => n.Contains(e))) continue;
            if (Mathf.Abs(s.rect.width - s.rect.height) < 2f && s.rect.width <= 48f)
            {
                Debug.Log($"[SimpleArena] Tile fallback de sub-sprite: {s.name} ({s.rect.width}x{s.rect.height}px)");
                return s;
            }
        }

        Debug.LogWarning("[SimpleArena] Nenhum tile encontrado — usando procedural. Rode 'Solengard/Debug/Preview Floor Tile'.");
#endif
        return null;
    }

    // Generates a seamlessly-tileable stone-floor texture at runtime (no assets required).
    Sprite GenerateProceduralFloor()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode   = TextureWrapMode.Repeat;

        var baseCol = new Color(0.16f, 0.14f, 0.18f);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float noise = Mathf.PerlinNoise(x * 0.3f + 7f, y * 0.3f + 13f) * 0.06f;
                Color c     = baseCol + new Color(noise, noise, noise);
                // Mortar lines between 16px blocks
                if (x % 16 == 0 || y % 16 == 0) c *= 0.7f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        var spr = Sprite.Create(tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit: 16f,
            extrude: 0,
            meshType: SpriteMeshType.FullRect);
        spr.name = "ProceduralFloor";
        Debug.Log("[SimpleArena] Chão procedural gerado (32x32 pedra escura).");
        return spr;
    }

    static Sprite MakePixel(Color c)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, c);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    public Bounds PlayArea => new Bounds(
        _player != null ? _player.position : Vector3.zero,
        new Vector3(tileAreaSize, tileAreaSize, 0f));
}

#if UNITY_EDITOR
public static class SimpleArenaTilesetDebug
{
    [UnityEditor.MenuItem("Solengard/Debug/List Tileset Sprites")]
    static void ListTilesetSprites()
    {
        string[] folders = { "Assets/Art/Environment/Season1_Dungeon/Tileset/PNG" };
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", folders);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Sub-sprites em {folders[0]} ===\n");

        int total = 0;
        foreach (var guid in guids)
        {
            string path    = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var    assets  = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            var    sprites = assets.OfType<Sprite>().ToList();
            if (sprites.Count == 0) continue;

            sb.AppendLine($"── {System.IO.Path.GetFileName(path)} ({sprites.Count} sprites) ──");
            foreach (var s in sprites)
                sb.AppendLine($"  {s.name,-40} {s.rect.width,4}x{s.rect.height,-4} rect=({s.rect.x},{s.rect.y}) PPU={s.pixelsPerUnit}");
            total += sprites.Count;
        }

        sb.AppendLine($"\nTotal: {total} sub-sprites.");
        UnityEngine.Debug.Log(sb.ToString());
        UnityEditor.EditorUtility.DisplayDialog("Tileset Sprites", $"{total} sprites logados no Console.", "OK");
    }

    [UnityEditor.MenuItem("Solengard/Debug/Preview Floor Tile")]
    static void PreviewFloorTile()
    {
        const string WF_PATH = "Assets/Art/Environment/Season1_Dungeon/Tileset/PNG/walls_floor.png";
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(WF_PATH);

        if (tex == null)
        {
            UnityEditor.EditorUtility.DisplayDialog("Preview Floor Tile",
                $"Arquivo não encontrado:\n{WF_PATH}", "OK");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Textura: {System.IO.Path.GetFileName(WF_PATH)}");
        sb.AppendLine($"Dimensões: {tex.width}x{tex.height}px");
        sb.AppendLine($"Células de 16px: {tex.width / 16} colunas × {tex.height / 16} linhas");
        sb.AppendLine();
        sb.AppendLine("Tile de chão atual extraído em:");
        sb.AppendLine("  x=96, y=16, tamanho=16x16");
        sb.AppendLine();
        sb.AppendLine("Para ajustar, edite cellX/cellY em SimpleArena.LoadFloorSprite().");

        var importer = UnityEditor.AssetImporter.GetAtPath(WF_PATH) as UnityEditor.TextureImporter;
        sb.AppendLine($"\nIsReadable: {(importer != null ? importer.isReadable.ToString() : "n/a")}");

        UnityEngine.Debug.Log($"[SimpleArena] Preview Floor Tile:\n{sb}");
        UnityEditor.EditorUtility.DisplayDialog("Preview Floor Tile", sb.ToString(), "OK");
    }
}
#endif
