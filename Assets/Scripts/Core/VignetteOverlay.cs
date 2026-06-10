using UnityEngine;

// Vinheta de câmera: 4 gradientes escuros nas bordas da tela.
// Texturas geradas uma vez; sprites parented à Main Camera, sortingOrder 200.
public class VignetteOverlay : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity = 0.4f;

    static Texture2D _tex;
    static Sprite _grad;
    const int GRAD_W = 4, GRAD_H = 64;

    readonly SpriteRenderer[] _edges = new SpriteRenderer[4];
    Camera _cam;

    static Sprite GetGradient()
    {
        if (_grad == null)
        {
            _tex = new Texture2D(GRAD_W, GRAD_H, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Bilinear;
            _tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[GRAD_W * GRAD_H];
            for (int y = 0; y < GRAD_H; y++)
            {
                float a = 1f - (float)y / (GRAD_H - 1); // opaco na base → transparente no topo
                byte ab = (byte)(a * a * 255f);
                for (int x = 0; x < GRAD_W; x++) px[y * GRAD_W + x] = new Color32(0, 0, 0, ab);
            }
            _tex.SetPixels32(px);
            _tex.Apply(false, false);
            // pivot na base: rotacionar posiciona a faixa "crescendo" da borda p/ dentro
            _grad = Sprite.Create(_tex, new Rect(0, 0, GRAD_W, GRAD_H),
                new Vector2(0.5f, 0f), GRAD_H);
        }
        return _grad;
    }

    void Start() => TryBuild();

    void TryBuild()
    {
        if (_cam != null || Camera.main == null) return;
        _cam = Camera.main;
        string[] names = { "Bottom", "Top", "Left", "Right" };
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject($"Vignette{names[i]}");
            go.transform.SetParent(_cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetGradient();
            sr.sortingOrder = 200;
            sr.color = new Color(1f, 1f, 1f, intensity);
            _edges[i] = sr;
        }
    }

    void LateUpdate()
    {
        if (_cam == null) { TryBuild(); if (_cam == null) return; }

        float h = _cam.orthographicSize;
        float w = h * _cam.aspect;
        float band = h * 0.35f; // profundidade da faixa
        const float z = 10f;    // câmera fica em z=-10 → faixa no plano z=0

        Place(0, new Vector3(0, -h, z), 0f,    w * 2f, band);
        Place(1, new Vector3(0,  h, z), 180f,  w * 2f, band);
        Place(2, new Vector3(-w, 0, z), -90f,  h * 2f, band);
        Place(3, new Vector3( w, 0, z),  90f,  h * 2f, band);
    }

    void Place(int i, Vector3 localPos, float rotZ, float width, float depth)
    {
        if (_edges[i] == null) return;
        var tr = _edges[i].transform;
        tr.localPosition = localPos;
        tr.localRotation = Quaternion.Euler(0, 0, rotZ);
        // sprite: (GRAD_W/GRAD_H)u de largura × 1u de altura, pivot na base
        tr.localScale = new Vector3(width / ((float)GRAD_W / GRAD_H), depth, 1f);
    }
}
