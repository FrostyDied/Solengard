using UnityEngine;

// Oscilação senoidal vertical no sprite — dá vida sem troca de frames.
// Ideal para inimigos "flutuantes" (fantasmas, almas). Adicionar manualmente no prefab.
// NÃO aplica ao Rigidbody — só ao Transform do sprite filho (ou do próprio GO).
public class EnemyBob : MonoBehaviour
{
    [SerializeField] float amplitude = 0.06f;
    [SerializeField] float speed     = 4f;

    Vector3   _basePos;
    Transform _sprite;
    float     _phase;

    void Start()
    {
        _sprite  = transform;
        _basePos = _sprite.localPosition;
        _phase   = Random.value * 6.28f;
    }

    void Update()
    {
        var p = _basePos;
        p.y += Mathf.Sin(Time.time * speed + _phase) * amplitude;
        _sprite.localPosition = p;
    }
}
