using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] Vector3 offset    = new Vector3(0f, 0f, -10f);
    [SerializeField] float orthoSize   = 14f;

    Transform _target;
    Vector3   _velocity; // SmoothDamp state

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        FindPlayer();
        if (Camera.main != null)
            Camera.main.orthographicSize = orthoSize;
    }

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _target = p.transform;
    }

    void LateUpdate()
    {
        if (_target == null) { FindPlayer(); return; }

        Vector3 desired = _target.position + offset;
        desired.z = offset.z;
        transform.position = Vector3.SmoothDamp(
            transform.position, desired, ref _velocity, 1f / smoothSpeed);
    }

    public void SetTarget(Transform t) => _target = t;
}
