using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SimpleArena : MonoBehaviour
{
    public static SimpleArena Instance { get; private set; }

    [Header("Dimensões")]
    [SerializeField] float width  = 40f;
    [SerializeField] float height = 40f;

    [Header("Cor do chão (protótipo)")]
    [SerializeField] Color floorColor = new Color(0.2f, 0.18f, 0.22f);

    [Header("Sprite real do tileset (opcional)")]
    [SerializeField] Sprite floorSprite;

    public float Width    => width;
    public float Height   => height;
    public Bounds PlayArea => new Bounds(Vector3.zero,
        new Vector3(width - 4f, height - 4f, 0));

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Build();
    }

    void Build()
    {
        BuildFloor();
        BuildWalls();
        ConfigureCamera();
        Debug.Log($"[SimpleArena] Arena {width}x{height} criada.");
    }

    void BuildFloor()
    {
        var go = new GameObject("ArenaFloor");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -10;

        if (floorSprite != null)
        {
            sr.sprite   = floorSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size     = new Vector2(width, height);
            sr.tileMode = SpriteTileMode.Continuous;
        }
        else
        {
            sr.sprite = MakePixel(Color.white);
            sr.color  = floorColor;
            go.transform.localScale = new Vector3(width, height, 1f);
        }
    }

    void BuildWalls()
    {
        var root = new GameObject("ArenaWalls");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;

        float hw = width  / 2f;
        float hh = height / 2f;
        float t  = 2f;

        CreateWall(root, "WallN", new Vector2(0,  hh + t / 2f), new Vector2(width + t * 2, t));
        CreateWall(root, "WallS", new Vector2(0, -hh - t / 2f), new Vector2(width + t * 2, t));
        CreateWall(root, "WallE", new Vector2( hw + t / 2f, 0), new Vector2(t, height));
        CreateWall(root, "WallW", new Vector2(-hw - t / 2f, 0), new Vector2(t, height));
    }

    void CreateWall(GameObject parent, string wallName, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(wallName);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer >= 0) go.layer = obstacleLayer;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    void ConfigureCamera()
    {
        if (Camera.main == null) return;
        Camera.main.orthographicSize = height / 2f * 0.85f;

        var cf = Camera.main.GetComponent<CameraFollow>();
        if (cf == null) return;

        float bx = width  / 2f - 6f;
        float by = height / 2f - 6f;
        cf.SetBounds(-bx, bx, -by, by);
        Debug.Log($"[SimpleArena] CameraFollow bounds: ±{bx:F1} ±{by:F1}");
    }

    static Sprite MakePixel(Color c)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, c);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
