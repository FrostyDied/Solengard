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
            _rb.linearVelocity = Random.insideUnitCircle * 1.0f;
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
            _sr.color = new Color(0.9f, 0.97f, 1f, pulse);
        }

    }

    void Collect()
    {
        VFXManager.Instance?.SpawnXPCollect(transform.position);
        XPSystem.Instance?.AddXP(xpValue);
        Destroy(gameObject);
    }

    public static XPDrop SpawnAt(Vector3 pos, int xp = 3)
    {
        var go = new GameObject("XPCrystal");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        if (_cachedSprite == null)
        {
            _cachedSprite = Resources.Load<Sprite>("Icons/moeda");
            if (_cachedSprite == null) _cachedSprite = MakeCrystalSprite();
        }
        sr.sprite = _cachedSprite;
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 0f;
        rb.linearDamping          = 10f;
        rb.angularDamping         = 10f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        go.transform.localScale   = new Vector3(0.08f, 0.08f, 1f);

        var drop = go.AddComponent<XPDrop>();
        drop.xpValue = xp;
        return drop;
    }

    static Sprite MakeCrystalSprite()
    {
        int w = 7, h = 9;
        var tex   = new Texture2D(w, h);
        var clear = new Color(0, 0, 0, 0);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, clear);

        var diamondBase    = new Color(0.75f, 0.90f, 1.00f);
        var diamondDark    = new Color(0.40f, 0.65f, 0.90f);
        var diamondBright  = new Color(0.95f, 0.98f, 1.00f);
        var diamondReflect = new Color(1.00f, 1.00f, 1.00f);

        for (int y = 0; y < h; y++)
        {
            float t  = Mathf.Abs(y - h * 0.5f) / (h * 0.5f);
            int   hw = Mathf.RoundToInt((1f - t) * w * 0.5f);
            int   cx = w / 2;
            for (int x = cx - hw; x <= cx + hw; x++)
            {
                Color c = y > h * 0.5f
                    ? Color.Lerp(diamondBase,  diamondBright, (y - h * 0.5f) / (h * 0.5f))
                    : Color.Lerp(diamondDark,  diamondBase,    y / (h * 0.5f));
                tex.SetPixel(x, y, c);
            }
        }
        if (w / 2 + 1 < w)
            tex.SetPixel(w / 2 + 1, h * 2 / 3, diamondReflect);
        tex.SetPixel(w / 2, h * 2 / 3, diamondBright);

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }
}
