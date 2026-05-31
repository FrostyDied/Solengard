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

        // Derive snap size from the chosen tile so the grid aligns exactly to tile boundaries.
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
        string[] folders = { "Assets/Art/Environment/Season1_Dungeon/Tileset/PNG" };

        // Collect every individual sub-sprite from every texture in the folder.
        var allSprites = new System.Collections.Generic.List<Sprite>();
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", folders);
        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var assets  = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
                if (a is Sprite s) allSprites.Add(s);
        }

        // Words that identify non-floor tiles — walls, corners, ornaments, etc.
        string[] exclude = { "wall", "corner", "edge", "top", "bottom", "side",
                             "ornament", "claw", "torch", "decor", "transition",
                             "arch", "pillar", "door", "window", "border" };

        // Preference order: names that suggest a plain repeatable floor tile.
        string[] prefer  = { "floor", "ground", "center", "tile", "stone", "dirt" };

        // Pass 1: preferred name, square, no excluded word.
        foreach (string pref in prefer)
        {
            foreach (Sprite s in allSprites)
            {
                string n = s.name.ToLower();
                if (exclude.Any(e => n.Contains(e))) continue;
                if (!n.Contains(pref)) continue;
                if (Mathf.Abs(s.rect.width - s.rect.height) < 2f)
                {
                    Debug.Log($"[SimpleArena] Tile de chão escolhido: {s.name} ({s.rect.width}x{s.rect.height}px, PPU={s.pixelsPerUnit})");
                    return s;
                }
            }
        }

        // Pass 2: any small square tile without excluded words.
        foreach (Sprite s in allSprites)
        {
            string n = s.name.ToLower();
            if (exclude.Any(e => n.Contains(e))) continue;
            if (Mathf.Abs(s.rect.width - s.rect.height) < 2f && s.rect.width <= 48f)
            {
                Debug.Log($"[SimpleArena] Tile fallback: {s.name} ({s.rect.width}x{s.rect.height}px, PPU={s.pixelsPerUnit})");
                return s;
            }
        }

        Debug.LogWarning("[SimpleArena] Nenhum tile de chão adequado encontrado — use 'Solengard/Debug/List Tileset Sprites' para inspecionar.");
#endif
        return null;
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
        sb.AppendLine($"=== Tileset sprites em {folders[0]} ===\n");

        int total = 0;
        foreach (var guid in guids)
        {
            string path   = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var    assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);

            var sprites = assets.OfType<Sprite>().ToList();
            if (sprites.Count == 0) continue;

            sb.AppendLine($"── {System.IO.Path.GetFileName(path)} ({sprites.Count} sprites) ──");
            foreach (var s in sprites)
                sb.AppendLine($"  {s.name,-40} {s.rect.width,4}x{s.rect.height,-4} PPU={s.pixelsPerUnit}");

            total += sprites.Count;
        }

        sb.AppendLine($"\nTotal: {total} sub-sprites encontrados.");
        Debug.Log(sb.ToString());
        UnityEditor.EditorUtility.DisplayDialog("Tileset Sprites", $"{total} sub-sprites logados no Console.", "OK");
    }
}
#endif
