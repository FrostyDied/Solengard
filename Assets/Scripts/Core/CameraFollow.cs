using UnityEngine;

// Faz a câmera seguir o player suavemente via Lerp.
// Attach na Main Camera. target é atribuído automaticamente pelo Rebuild GameScene.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float     smoothSpeed = 5f;
    [SerializeField] Vector3   offset      = new Vector3(0f, 0f, -10f);

    [Header("Bounds")]
    [SerializeField] bool  useBounds;
    [SerializeField] float minX, maxX, minY, maxY;

    static CameraFollow _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        this.minX = minX; this.maxX = maxX;
        this.minY = minY; this.maxY = maxY;
        useBounds = true;
        Debug.Log($"[Camera] Bounds recebidos: {minX},{maxX},{minY},{maxY}");
    }

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = Vector3.Lerp(
            transform.position,
            target.position + offset,
            smoothSpeed * Time.deltaTime);

        if (useBounds)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
        }

        transform.position = pos;
    }
}
