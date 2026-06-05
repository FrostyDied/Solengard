using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    float      _damage;
    float      _speed;
    Vector2    _dir;
    GameObject _impactVFX;

    public void Init(float damage, Vector2 dir, float speed, GameObject impactVFX)
    {
        _damage    = damage;
        _dir       = dir.normalized;
        _speed     = speed;
        _impactVFX = impactVFX;
        Destroy(gameObject, 2.5f);
    }

    void Update()
    {
        transform.Translate(_dir * _speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col == null) return;
        var enemy = col.GetComponent<EnemyBase>() ?? col.GetComponentInParent<EnemyBase>();
        if (enemy == null) return;

        enemy.TakeDamage(_damage);

        if (_impactVFX != null)
        {
            var fx = Instantiate(_impactVFX, transform.position, Quaternion.identity);
            Destroy(fx, 0.5f);
        }

        Destroy(gameObject);
    }
}
