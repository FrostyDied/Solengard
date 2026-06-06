using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    float      _damage;
    float      _speed;
    Vector2    _dir;

    Sprite[]       _frames;
    SpriteRenderer _sr;
    float          _frameTimer;
    int            _frameIndex;
    const float    FRAME_INTERVAL = 1f / 12f;

    Sprite[] _impactFrames;
    float    _impactScale;

    public void Init(float damage, Vector2 dir, float speed, float lifetime = 2.5f)
    {
        _damage = damage;
        _dir    = dir.normalized;
        _speed  = speed;
        _sr     = GetComponent<SpriteRenderer>();
        Destroy(gameObject, lifetime);
    }

    public void SetFrames(Sprite[] frames)
    {
        _frames = frames;
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_frames != null && _frames.Length > 0)
            _sr.sprite = _frames[0];
    }

    public void SetImpactVFX(Sprite[] frames, float scale)
    {
        _impactFrames = frames;
        _impactScale  = scale;
    }

    void Update()
    {
        transform.Translate(_dir * _speed * Time.deltaTime, Space.World);

        if (_frames != null && _frames.Length > 1)
        {
            _frameTimer += Time.deltaTime;
            if (_frameTimer >= FRAME_INTERVAL)
            {
                _frameTimer -= FRAME_INTERVAL;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                if (_sr != null) _sr.sprite = _frames[_frameIndex];
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col == null) return;
        var enemy = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
        if (enemy == null) return;

        enemy.TakeDamage(_damage);

        bool spawnImpact = !enemy.IsDead && _impactFrames != null && _impactFrames.Length > 0;
        Debug.Log($"[Hit] {enemy.name} isDead={enemy.IsDead} → spawning={( spawnImpact ? "impactVFX" : "nenhum")}");
        if (spawnImpact)
            SpriteVFX.Spawn(_impactFrames, transform.position, 0f, _impactScale, 0.4f);

        Destroy(gameObject);
    }
}
