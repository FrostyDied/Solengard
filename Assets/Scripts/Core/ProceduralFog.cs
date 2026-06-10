using UnityEngine;

// Névoa procedural em 3 camadas (Built-in Pipeline, sem URP/Light2D).
// Segue a câmera; cor e densidade transicionam suavemente por bioma.
public class ProceduralFog : MonoBehaviour
{
    static Texture2D _fogTex;
    static Sprite _fogSprite;

    readonly SpriteRenderer[] _layers = new SpriteRenderer[3];
    // Velocidades de drift por camada (parallax sutil entre as 3)
    readonly Vector2[] _drift = { new Vector2(0.12f, 0.04f), new Vector2(-0.08f, 0.06f), new Vector2(0.05f, -0.03f) };
    // Pesos altos: com cor escura sobre chão escuro, alpha baixo fica invisível
    readonly float[] _layerWeight = { 0.8f, 0.6f, 0.45f };

    Color _targetColor = new Color(0.16f, 0.36f, 0.12f);
    float _targetDensity = 0.35f;
    Color _currentColor;
    float _currentDensity;
    Transform _cam;

    void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject($"FogLayer{i}");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetFogSprite();
            sr.sortingOrder = 90 + i * 2; // 90, 92, 94 — faixa de atmosfera
            go.transform.localScale = Vector3.one * (45f + i * 10f); // cobre a tela com folga
            _layers[i] = sr;
        }
        SetBiome(0);
        _currentColor = _targetColor;
        _currentDensity = _targetDensity;
    }

    static Sprite GetFogSprite()
    {
        if (_fogSprite == null)
        {
            const int S = 128;
            _fogTex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            _fogTex.filterMode = FilterMode.Bilinear;
            _fogTex.wrapMode = TextureWrapMode.Clamp;
            var noise = new SeededNoise(1337);
            var px = new Color[S * S];
            float c = (S - 1) * 0.5f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float fx = (float)x / S, fy = (float)y / S;
                    float n = noise.FBM(fx, fy, 4f, 3);
                    // borda radial suave: o sprite não mostra quina
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float edge = Mathf.Clamp01(1f - d);
                    px[y * S + x] = new Color(1f, 1f, 1f, n * edge * edge);
                }
            _fogTex.SetPixels(px);
            _fogTex.Apply(false, false);
            _fogSprite = Sprite.Create(_fogTex, new Rect(0, 0, S, S),
                new Vector2(0.5f, 0.5f), S);
        }
        return _fogSprite;
    }

    void LateUpdate()
    {
        if (_cam == null)
        {
            if (Camera.main != null) _cam = Camera.main.transform;
            else return;
        }
        // Segue a câmera mantendo o plano do jogo (z = 0)
        transform.position = new Vector3(_cam.position.x, _cam.position.y, 0f);

        _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * 0.8f);
        _currentDensity = Mathf.Lerp(_currentDensity, _targetDensity, Time.deltaTime * 0.8f);

        float t = Time.time;
        for (int i = 0; i < 3; i++)
        {
            if (_layers[i] == null) continue;
            // drift lento independente por camada
            _layers[i].transform.localPosition = new Vector3(
                Mathf.Sin(t * _drift[i].x + i * 2.1f) * 4f,
                Mathf.Cos(t * _drift[i].y + i * 1.3f) * 3f, 0);
            float pulse = 0.9f + 0.1f * Mathf.Sin(t * 0.25f + i * 1.7f);
            var c = _currentColor;
            c.a = _currentDensity * _layerWeight[i] * pulse;
            // SpriteRenderer.color é cor por renderer (vertex color) —
            // não instancia material, mesmo efeito do MaterialPropertyBlock
            _layers[i].color = c;
        }
    }

    public void SetBiome(int id)
    {
        var gen = ProceduralSceneGenerator.Instance;
        if (gen == null || gen.biomes == null || gen.biomes.Length == 0) return;
        var b = gen.biomes[Mathf.Clamp(id, 0, gen.biomes.Length - 1)];
        if (b == null) return;
        // clarear 25%: fogColor pura é quase a cor do chão → névoa camuflada
        _targetColor = Color.Lerp(b.fogColor, Color.white, 0.25f);
        _targetDensity = b.fogDensity;
    }
}
