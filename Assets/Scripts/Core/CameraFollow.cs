using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] Vector3 offset = new Vector3(0f, 0f, -10f);

    Transform _target;
    bool  _useBounds;
    float _minX, _maxX, _minY, _maxY;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => FindPlayer();

    void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _target = p.transform;
    }

    void LateUpdate()
    {
        if (_target == null) { FindPlayer(); return; }

        var desired  = _target.position + offset;
        var smoothed = Vector3.Lerp(transform.position, desired,
                           smoothSpeed * Time.deltaTime);

        if (_useBounds)
        {
            smoothed.x = Mathf.Clamp(smoothed.x, _minX, _maxX);
            smoothed.y = Mathf.Clamp(smoothed.y, _minY, _maxY);
        }

        smoothed.z = offset.z;
        transform.position = smoothed;
    }

    public void SetTarget(Transform t) => _target = t;

    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        _minX = minX; _maxX = maxX;
        _minY = minY; _maxY = maxY;
        _useBounds = true;
        Debug.Log($"[CameraFollow] Bounds ativos: X[{minX},{maxX}] Y[{minY},{maxY}]");
    }

    public void ClearBounds() => _useBounds = false;
}
