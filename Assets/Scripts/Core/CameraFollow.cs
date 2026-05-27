using UnityEngine;

// Faz a câmera seguir o player suavemente via Lerp.
// Attach na Main Camera. target é atribuído automaticamente pelo Rebuild GameScene.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float     smoothSpeed = 5f;
    [SerializeField] Vector3   offset      = new Vector3(0f, 0f, -10f);

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
        transform.position = Vector3.Lerp(
            transform.position,
            target.position + offset,
            smoothSpeed * Time.deltaTime);
    }
}
