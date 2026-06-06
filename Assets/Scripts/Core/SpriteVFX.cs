using UnityEngine;
using System.Collections;

public class SpriteVFX : MonoBehaviour
{
    SpriteRenderer _sr;
    Sprite[]       _frames;
    float          _fps = 24f;
    float          _lifetime;

    public static GameObject Spawn(Sprite[] frames, Vector3 pos,
        float rotation, float scale, float lifetime, float fps = 24f)
    {
        if (frames == null || frames.Length == 0) return null;

        var go = new GameObject("SpriteVFX");
        go.transform.position   = pos;
        go.transform.rotation   = Quaternion.Euler(0f, 0f, rotation);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Characters";
        sr.sortingOrder     = 200;
        sr.sprite           = frames[0];

        var vfx      = go.AddComponent<SpriteVFX>();
        vfx._sr      = sr;
        vfx._frames  = frames;
        vfx._fps     = fps;
        vfx._lifetime = lifetime;

        Destroy(go, lifetime);
        return go;
    }

    void Start() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        float interval = 1f / _fps;
        int   idx      = 0;
        float elapsed  = 0f;
        while (elapsed < _lifetime)
        {
            _sr.sprite = _frames[idx % _frames.Length];
            idx++;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }
}
