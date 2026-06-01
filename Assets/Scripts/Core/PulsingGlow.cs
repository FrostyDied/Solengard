using UnityEngine;

public class PulsingGlow : MonoBehaviour
{
    SpriteRenderer _sr;
    float          _t;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _t  = Random.value * 6f;
    }

    void Update()
    {
        if (_sr == null) return;
        _t += Time.deltaTime * 2f;
        float a = 0.5f + Mathf.Sin(_t) * 0.5f;
        var c = _sr.color; c.a = a; _sr.color = c;
    }
}
