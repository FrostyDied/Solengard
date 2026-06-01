using UnityEngine;

public class FogDrift : MonoBehaviour
{
    Transform _player;
    float     _speed;
    Vector2   _offset;

    public void Init(Transform p, float speed, int order) { _player = p; _speed = speed; }

    void Update()
    {
        _offset.x += _speed * Time.deltaTime;
        _offset.y += _speed * 0.5f * Time.deltaTime;
        Vector3 basePos    = _player != null ? _player.position : Vector3.zero;
        transform.position = new Vector3(
            basePos.x + Mathf.Sin(_offset.x) * 3f,
            basePos.y + Mathf.Cos(_offset.y) * 3f,
            0f);
    }
}
