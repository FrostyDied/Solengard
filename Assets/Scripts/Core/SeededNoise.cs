using UnityEngine;

// Noise determinístico por seed — base de todo o motor procedural de cenário.
// Sem estado mutável: a mesma seed gera sempre o mesmo chunk.
public class SeededNoise
{
    readonly int seed;
    public SeededNoise(int seed) { this.seed = seed; }

    public float Get(float x, float y)
    {
        float n = Mathf.Sin(x * 127.1f + y * 311.7f + seed) * 43758.5453f;
        return n - Mathf.Floor(n);
    }

    public float Smooth(float x, float y)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float fx = x - ix, fy = y - iy;
        float ux = fx * fx * (3 - 2 * fx);
        float uy = fy * fy * (3 - 2 * fy);
        float a = Get(ix, iy), b = Get(ix + 1, iy);
        float c = Get(ix, iy + 1), d = Get(ix + 1, iy + 1);
        return a + (b - a) * ux + (c - a) * uy + (a - b - c + d) * ux * uy;
    }

    public float FBM(float x, float y, float freq, int oct)
    {
        float v = 0, amp = 0.5f, max = 0;
        for (int i = 0; i < oct; i++)
        {
            v += Smooth(x * freq, y * freq) * amp;
            max += amp; amp *= 0.5f; freq *= 2f;
        }
        return v / max;
    }
}
