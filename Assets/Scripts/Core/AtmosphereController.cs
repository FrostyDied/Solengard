using UnityEngine;

public class AtmosphereController : MonoBehaviour
{
    [SerializeField] Color ambientTint = new Color(0.45f, 0.45f, 0.6f);
    [SerializeField] Color fogColor    = new Color(0.3f,  0.5f,  0.35f, 0.12f);

    Transform _player;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        CreateDarkOverlay();
        CreateFogLayers();
    }

    void CreateDarkOverlay()
    {
        var go = new GameObject("DarkOverlay");
        go.transform.SetParent(transform);

        const int size = 64;
        var tex = new Texture2D(size, size);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = (x - size / 2f) / (size / 2f);
                float dy = (y - size / 2f) / (size / 2f);
                float d  = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float darkness = Mathf.Lerp(0.25f, 0.65f, d);
                tex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.1f, darkness));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        var sr       = go.AddComponent<SpriteRenderer>();
        sr.sprite    = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
        sr.sortingOrder = 100;
        go.transform.localScale = new Vector3(60, 60, 1);

        if (_player != null) go.AddComponent<FollowPlayer>().Init(_player, 100);
    }

    void CreateFogLayers()
    {
        for (int i = 0; i < 2; i++)
        {
            var go   = new GameObject($"FogLayer{i}");
            go.transform.SetParent(transform);
            var sr   = go.AddComponent<SpriteRenderer>();
            sr.sprite       = MakeFogSprite();
            sr.color        = fogColor;
            sr.sortingOrder = 90 + i;
            go.transform.localScale = new Vector3(50, 50, 1);
            go.AddComponent<FogDrift>().Init(_player, i % 2 == 0 ? 0.3f : -0.2f, 90 + i);
        }
    }

    Sprite MakeFogSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                float a = Mathf.Clamp01(n - 0.4f) * 0.8f;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Repeat;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}
