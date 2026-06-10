using UnityEngine;

// Partículas ambientais por bioma — pool fixo de 40 SpriteRenderers,
// zero alocação em Update. Comportamento por bioma:
//   0 Veremoth:  esporos cyan, drift sinusoidal lento
//   1 Khorduum:  poeira âmbar, sobe lentamente
//   2 Valdross:  vaga-lumes azuis, movimento errático (Perlin)
//   3 Gorveth:   pirilampos amarelos, lentos com brilho pulsante
//   4 Arkenfall: brasas laranja, sobem com aceleração e fade
public class ProceduralParticles : MonoBehaviour
{
    const int POOL = 40;

    static Texture2D _tex;
    static Sprite _dot;

    readonly SpriteRenderer[] _srs = new SpriteRenderer[POOL];
    readonly Vector3[] _pos = new Vector3[POOL];
    readonly Vector3[] _vel = new Vector3[POOL];
    readonly float[] _life = new float[POOL];
    readonly float[] _maxLife = new float[POOL];
    readonly float[] _phase = new float[POOL];

    int _biome;
    Color _color = new Color(0.48f, 1f, 0.93f);
    Transform _cam;

    static Sprite GetDot()
    {
        if (_dot == null)
        {
            const int S = 8;
            _tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Bilinear;
            _tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[S * S];
            float c = (S - 1) * 0.5f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    px[y * S + x] = new Color32(255, 255, 255,
                        (byte)(Mathf.Clamp01(1f - d) * 255f));
                }
            _tex.SetPixels32(px);
            _tex.Apply(false, false);
            _dot = Sprite.Create(_tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        }
        return _dot;
    }

    void Start()
    {
        for (int i = 0; i < POOL; i++)
        {
            var go = new GameObject("P");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDot();
            sr.sortingOrder = 96;
            _srs[i] = sr;
            Respawn(i, scatter: true);
        }
    }

    void Respawn(int i, bool scatter = false)
    {
        if (_srs[i] == null) return;
        Vector3 center = _cam != null ? _cam.position : Vector3.zero;
        _pos[i] = new Vector3(center.x + Random.Range(-14f, 14f),
                              center.y + Random.Range(-9f, 9f), 0);
        _phase[i] = Random.Range(0f, Mathf.PI * 2f);
        _maxLife[i] = Random.Range(3f, 7f);
        _life[i] = scatter ? Random.Range(0f, _maxLife[i]) : 0f;

        switch (_biome)
        {
            case 0:  _vel[i] = new Vector3(Random.Range(-0.2f, 0.2f),  Random.Range(-0.15f, 0.05f), 0); break;
            case 1:  _vel[i] = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(0.15f, 0.35f), 0); break;
            case 2:  _vel[i] = Vector3.zero; break; // errático via Perlin no Update
            case 3:  _vel[i] = new Vector3(Random.Range(-0.1f, 0.1f),  Random.Range(-0.05f, 0.1f), 0); break;
            default: _vel[i] = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.4f, 0.9f), 0); break;
        }

        _srs[i].transform.position = _pos[i];
        _srs[i].transform.localScale = Vector3.one * Random.Range(0.05f, 0.14f);
    }

    void Update()
    {
        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
        float dt = Time.deltaTime;
        float t = Time.time;
        Vector3 center = _cam != null ? _cam.position : Vector3.zero;

        for (int i = 0; i < POOL; i++)
        {
            if (_srs[i] == null) continue;
            _life[i] += dt;

            Vector3 d = _pos[i] - center;
            if (_life[i] >= _maxLife[i] || Mathf.Abs(d.x) > 16f || Mathf.Abs(d.y) > 11f)
            {
                Respawn(i);
                continue;
            }

            switch (_biome)
            {
                case 0: // esporos: drift senoidal
                    _pos[i] += (_vel[i] + new Vector3(Mathf.Sin(t * 0.7f + _phase[i]) * 0.25f, 0, 0)) * dt;
                    break;
                case 2: // vaga-lumes: errático
                    _pos[i] += new Vector3(
                        (Mathf.PerlinNoise(t * 0.5f, _phase[i]) - 0.5f) * 1.6f,
                        (Mathf.PerlinNoise(_phase[i], t * 0.5f) - 0.5f) * 1.6f, 0) * dt;
                    break;
                case 4: // brasas: aceleram subindo
                    _vel[i] += Vector3.up * (0.35f * dt);
                    _pos[i] += _vel[i] * dt;
                    break;
                default: // poeira (1) e pirilampos (3): velocidade constante
                    _pos[i] += _vel[i] * dt;
                    break;
            }

            float lifeT = _life[i] / _maxLife[i];
            float fade = lifeT < 0.15f ? lifeT / 0.15f
                       : lifeT > 0.7f ? (1f - lifeT) / 0.3f : 1f;
            float glow = _biome == 3
                ? 0.55f + 0.45f * Mathf.Sin(t * 2.5f + _phase[i]) // pulso Gorveth
                : 1f;
            var c = _color;
            c.a = Mathf.Clamp01(fade * glow) * 0.85f;
            _srs[i].color = c;
            _srs[i].transform.position = _pos[i];
        }
    }

    public void SetBiome(int id)
    {
        _biome = Mathf.Clamp(id, 0, 4);
        var gen = ProceduralSceneGenerator.Instance;
        if (gen != null && gen.biomes != null && gen.biomes.Length > _biome && gen.biomes[_biome] != null)
            _color = gen.biomes[_biome].particleColor;
        for (int i = 0; i < POOL; i++)
            Respawn(i, scatter: true);
    }
}
