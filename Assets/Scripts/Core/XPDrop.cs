using UnityEngine;

public class XPDrop : MonoBehaviour
{
    [SerializeField] int   xpValue       = 3;
    [SerializeField] float collectRadius = 0.8f;
    [SerializeField] float moveSpeed     = 8f;
    [SerializeField] float lifetime      = 12f;

    // 0 = sem atração automática; upgrade "Cristal Magnético" incrementa este valor
    public static float GlobalMagnetRadius = 0f;

    Transform      _player;
    bool           _attracted;
    float          _timer;
    SpriteRenderer _sr;
    Rigidbody2D    _rb;

    static Sprite _cachedSprite;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        _sr    = GetComponent<SpriteRenderer>();
        _rb    = GetComponent<Rigidbody2D>();
        _timer = lifetime;

        if (_rb != null)
        {
            _rb.linearVelocity = Random.insideUnitCircle * 2.5f;
            _rb.gravityScale   = 0f;
        }
    }

    void Update()
    {
        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist < collectRadius)
        {
            Collect();
            return;
        }

        if (dist < GlobalMagnetRadius || _attracted)
        {
            _attracted = true;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            transform.position = Vector2.MoveTowards(
                transform.position, _player.position, moveSpeed * Time.deltaTime);
        }

        if (_sr != null)
        {
            float pulse = 0.7f + Mathf.Sin(Time.time * 6f) * 0.3f;
            _sr.color = new Color(0.2f, 0.5f, 1f, pulse);
        }

        _timer -= Time.deltaTime;
        if (_timer <= 0f) Destroy(gameObject);
    }

    void Collect()
    {
        VFXFactory.SpawnXPCollect(transform.position);
        XPSystem.Instance?.AddXP(xpValue);
        Destroy(gameObject);
    }

    public static XPDrop SpawnAt(Vector3 pos, int xp = 3)
    {
        var go = new GameObject("XPCrystal");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        if (_cachedSprite == null) _cachedSprite = MakeCrystalSprite();
        sr.sprite       = _cachedSprite;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var drop = go.AddComponent<XPDrop>();
        drop.xpValue = xp;
        return drop;
    }

    static Sprite MakeCrystalSprite()
    {
        int w = 8, h = 10;
        var tex   = new Texture2D(w, h);
        var clear = new Color(0, 0, 0, 0);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, clear);

        var blue     = new Color(0.3f,  0.55f, 1f);
        var bright   = new Color(0.7f,  0.85f, 1f);
        var darkBlue = new Color(0.15f, 0.3f,  0.8f);

        for (int y = 0; y < h; y++)
        {
            float t  = Mathf.Abs(y - h * 0.5f) / (h * 0.5f);
            int   hw = Mathf.RoundToInt((1f - t) * w * 0.5f);
            int   cx = w / 2;
            for (int x = cx - hw; x <= cx + hw; x++)
            {
                Color c = y > h * 0.5f
                    ? Color.Lerp(blue, bright,   (y - h * 0.5f) / (h * 0.5f))
                    : Color.Lerp(darkBlue, blue,  y / (h * 0.5f));
                tex.SetPixel(x, y, c);
            }
        }
        if (w / 2 + 1 < w)
            tex.SetPixel(w / 2 + 1, h * 2 / 3, new Color(1f, 1f, 1f, 0.8f));
        tex.SetPixel(w / 2, h * 2 / 3, bright);

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}
