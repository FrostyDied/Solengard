using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SimpleArena : MonoBehaviour
{
    public static SimpleArena Instance { get; private set; }

    [Header("Tamanho do chão (cobre a câmera com folga)")]
    [SerializeField] float tileAreaSize  = 60f;

    [Header("Snap do tile (calculado automaticamente a partir do sprite)")]
    [SerializeField] float tileWorldSize = 4f;

    [Header("Sprite do tileset (deixe vazio para grama procedural)")]
    [SerializeField] Sprite floorSprite;

    [Header("Cor de fallback (último recurso)")]
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
        if (floorSprite == null)
        {
#if UNITY_EDITOR
            floorSprite = LoadGrassTile();
#else
            floorSprite = GenerateGrassFloor();
#endif
        }

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

    // Grama verde procedural e tileável — 64×64px a 16 PPU = tile de 4 unidades de mundo.
    Sprite GenerateGrassFloor()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode   = TextureWrapMode.Repeat;

        var baseGreen  = new Color(0.30f, 0.55f, 0.25f);
        var darkGreen  = new Color(0.24f, 0.46f, 0.20f);
        var lightGreen = new Color(0.36f, 0.62f, 0.30f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float n  = Mathf.PerlinNoise(x * 0.15f,        y * 0.15f);
                float n2 = Mathf.PerlinNoise(x * 0.5f + 100f,  y * 0.5f + 100f);
                Color c  = Color.Lerp(darkGreen, lightGreen, n);
                // Pontilhado de tufos de grama
                if (n2 > 0.75f) c = Color.Lerp(c, lightGreen, 0.6f);
                if (n2 < 0.15f) c = Color.Lerp(c, darkGreen,  0.5f);
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
        spr.name = "GrassFloor";
        Debug.Log("[SimpleArena] Chão de grama procedural gerado (64x64px, PPU=16, tileWorld=4).");
        return spr;
    }

#if UNITY_EDITOR
    Sprite LoadGrassTile()
    {
        const string path = "Assets/Art/Environment/Season3_Grassland/Tileset/PNG/ground_grasss.png";

        var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (!importer.isReadable)                         { importer.isReadable = true;                   changed = true; }
            if (importer.wrapMode != TextureWrapMode.Repeat)  { importer.wrapMode   = TextureWrapMode.Repeat; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning("[SimpleArena] ground_grasss.png não encontrado — usando grama procedural.");
            return GenerateGrassFloor();
        }

        // ground_grasss.png é 336×256 = grade 21×16 tiles de 16px.
        // y=128 em Unity coords (y cresce para cima) = row 8 do fundo = row 7 do topo visual.
        // Região 32×32 (2×2 tiles) dá mais variação natural ao tile repetido.
        const int cellX = 160, cellY = 128, region = 32;

        var spr = Sprite.Create(tex,
            new Rect(cellX, cellY, region, region),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit: 16f,
            extrude: 0,
            meshType: SpriteMeshType.FullRect);
        spr.name = "GrassTile_Season3";
        Debug.Log($"[SimpleArena] Tile de grama real extraído de ground_grasss.png em ({cellX},{cellY}) {region}×{region}px");
        return spr;
    }
#endif

    // Mantido comentado — extrai uma célula 16×16 de walls_floor.png quando desejado.
    /*
    Sprite LoadFloorSprite()
    {
#if UNITY_EDITOR
        const string WF_PATH = "Assets/Art/Environment/Season1_Dungeon/Tileset/PNG/walls_floor.png";
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(WF_PATH);
        if (tex != null)
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(WF_PATH) as UnityEditor.TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(WF_PATH);
            }
            int cellX = 96, cellY = 16, cell = 16;
            var spr = Sprite.Create(tex,
                new Rect(cellX, cellY, cell, cell),
                new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
            spr.name = "FloorTile_Extracted";
            return spr;
        }
#endif
        return null;
    }
    */

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
        sb.AppendLine("Tile configurado para extração: x=96, y=16, 16x16");
        sb.AppendLine("Para usar o tile real, descomente LoadFloorSprite() em BuildFloor().");

        var importer = UnityEditor.AssetImporter.GetAtPath(WF_PATH) as UnityEditor.TextureImporter;
        sb.AppendLine($"\nIsReadable: {(importer != null ? importer.isReadable.ToString() : "n/a")}");

        UnityEngine.Debug.Log($"[SimpleArena] Preview:\n{sb}");
        UnityEditor.EditorUtility.DisplayDialog("Preview Floor Tile", sb.ToString(), "OK");
    }

    [UnityEditor.MenuItem("Solengard/Debug/Preview Grass Tile")]
    static void PreviewGrassTile()
    {
        const string path = "Assets/Art/Environment/Season3_Grassland/Tileset/PNG/ground_grasss.png";
        const int cellX = 160, cellY = 128, region = 32;

        var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (importer != null && (!importer.isReadable || importer.wrapMode != TextureWrapMode.Repeat))
        {
            importer.isReadable = true;
            importer.wrapMode   = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
        if (tex == null)
        {
            UnityEditor.EditorUtility.DisplayDialog("Preview Grass Tile",
                $"Arquivo não encontrado:\n{path}", "OK");
            return;
        }

        UnityEngine.Debug.Log(
            $"[SimpleArena] Preview Grass Tile\n" +
            $"Arquivo: {System.IO.Path.GetFileName(path)}  {tex.width}×{tex.height}px\n" +
            $"Grade: {tex.width / 16}×{tex.height / 16} tiles de 16px\n" +
            $"Região extraída: ({cellX},{cellY}) {region}×{region}px\n" +
            $"Linha visual (de cima): {(tex.height - cellY - region) / 16}   Coluna: {cellX / 16}");

        var spr = UnityEngine.Sprite.Create(tex,
            new UnityEngine.Rect(cellX, cellY, region, region),
            new UnityEngine.Vector2(0.5f, 0.5f),
            16f, 0, SpriteMeshType.FullRect);
        spr.name = "GrassTile_Preview";

        var previewGO = new UnityEngine.GameObject("__GrassTilePreview__");
        var sr = previewGO.AddComponent<UnityEngine.SpriteRenderer>();
        sr.sprite       = spr;
        sr.drawMode     = SpriteDrawMode.Tiled;
        sr.size         = new UnityEngine.Vector2(10f, 10f);
        sr.tileMode     = SpriteTileMode.Continuous;
        sr.sortingOrder = -100;
        previewGO.transform.position = UnityEngine.Vector3.zero;

        UnityEditor.Selection.activeGameObject = previewGO;
        UnityEditor.EditorUtility.DisplayDialog("Preview Grass Tile",
            $"GameObject '__GrassTilePreview__' criado na cena.\n\n" +
            $"Região: ({cellX},{cellY}) {region}×{region}px\n" +
            $"Linha visual ≈ {(tex.height - cellY - region) / 16} do topo, coluna {cellX / 16}\n\n" +
            $"Se não parecer grama pura, ajuste cellX/cellY em LoadGrassTile().\n" +
            $"Delete o objeto de preview quando terminar.", "OK");
    }
}
#endif
