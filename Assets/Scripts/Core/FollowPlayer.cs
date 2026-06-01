using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    Transform _target;

    public void Init(Transform t, int order) { _target = t; }

    void LateUpdate()
    {
        if (_target == null) return;
        var p = _target.position; p.z = 0;
        transform.position = p;
    }
}
