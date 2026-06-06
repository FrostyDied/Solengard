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
        Destroy(gameObject);
    }
}
