using UnityEngine;

public class HeartDrop : MonoBehaviour
{
    [SerializeField] float healPercent   = 0.25f;
    [SerializeField] float collectRadius = 1.0f;
    [SerializeField] float lifetime      = 15f;

    float          _timer;
    SpriteRenderer _sr;

    void Start()
    {
        _sr    = GetComponent<SpriteRenderer>();
        _timer = lifetime;
    }

    void Update()
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        if (dist < collectRadius)
        {
            Collect(player.GetComponent<PlayerHealth>());
            return;
        }

        if (_sr != null)
        {
            float pulse = 0.7f + Mathf.Sin(Time.time * 4f) * 0.3f;
            _sr.color = new Color(1f, 0.2f + pulse * 0.3f, 0.2f, pulse);
        }

        _timer -= Time.deltaTime;
        if (_timer <= 0f) Destroy(gameObject);
    }

    void Collect(PlayerHealth ph)
    {
        if (ph == null) { Destroy(gameObject); return; }
        float heal = ph.MaxHealth * healPercent;
        ph.Curar(heal);
        VFXManager.Instance?.SpawnLevelUp(transform.position);
        Debug.Log($"[Heart] Curou {heal:F0} HP");
        Destroy(gameObject);
    }

    public static HeartDrop SpawnAt(Vector3 pos)
    {
        var go = new GameObject("HeartDrop");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 1.2f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeHeartSprite();
        sr.sortingOrder = 6;

        return go.AddComponent<HeartDrop>();
    }

    static Sprite MakeHeartSprite()
    {
        int size  = 16;
        var tex   = new Texture2D(size, size);
        var clear = new Color(0, 0, 0, 0);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, clear);

        var red    = new Color(0.90f, 0.15f, 0.15f);
        var bright = new Color(1.00f, 0.40f, 0.40f);

        int[,] heart =
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,1,1,0,0,0,0,0,0,1,1,0,0,0,0},
            {0,1,1,1,1,0,0,0,0,1,1,1,1,0,0,0},
            {1,1,1,1,1,1,0,0,1,1,1,1,1,1,0,0},
            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0},
            {1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0},
            {0,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0},
            {0,0,1,1,1,1,1,1,1,1,1,1,0,0,0,0},
            {0,0,0,1,1,1,1,1,1,1,1,0,0,0,0,0},
            {0,0,0,0,1,1,1,1,1,1,0,0,0,0,0,0},
            {0,0,0,0,0,1,1,1,1,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
        };

        for (int row = 0; row < heart.GetLength(0); row++)
            for (int col = 0; col < heart.GetLength(1); col++)
                if (heart[row, col] == 1)
                {
                    Color c = (col == 1 || col == 2) && row < 4 ? bright : red;
                    tex.SetPixel(col, size - 1 - row, c);
                }

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}
