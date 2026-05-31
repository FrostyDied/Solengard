using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] Vector3 offset    = new Vector3(0f, 0f, -10f);
    [SerializeField] float orthoSize   = 7f;

    Transform _target;

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

        Vector3 desired  = _target.position + offset;
        Vector3 smoothed = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        smoothed.z       = offset.z;
        transform.position = smoothed;
    }

    public void SetTarget(Transform t) => _target = t;

    // Kept for SimpleArena compatibility — no-ops since bounds were removed
    public void SetBounds(float minX, float maxX, float minY, float maxY) { }
    public void ClearBounds() { }
}
