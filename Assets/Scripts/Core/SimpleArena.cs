using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SimpleArena : MonoBehaviour
{
    public static SimpleArena Instance { get; private set; }

    [Header("Tamanho do chão (cobre a câmera com folga)")]
    [SerializeField] float tileAreaSize = 60f;

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
            Debug.Log($"[SimpleArena] Chão com tileset: {floorSprite.name}");
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

        // Snap ao grid do tile para a textura não deslizar com o player
        Vector3 pos = _player.position;
        pos.x = Mathf.Round(pos.x);
        pos.y = Mathf.Round(pos.y);
        pos.z = 0f;
        _floor.position = pos;
    }

    Sprite LoadFloorSprite()
    {
#if UNITY_EDITOR
        string[] folders  = { "Assets/Art/Environment/Season1_Dungeon/Tileset/PNG" };
        string[] keywords = { "floor", "ground", "tile", "stone", "dirt" };
        foreach (var kw in keywords)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:Sprite {kw}", folders);
            if (guids.Length > 0)
            {
                var spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                if (spr != null) return spr;
            }
        }
        var any = UnityEditor.AssetDatabase.FindAssets("t:Sprite", folders);
        if (any.Length > 0)
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(any[0]));
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
