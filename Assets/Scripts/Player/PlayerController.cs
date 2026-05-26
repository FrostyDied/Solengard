using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Em mobile usa o joystick virtual; fallback para Input caso o componente não exista
        moveInput = MobileJoystick.Instance != null
            ? MobileJoystick.Instance.Direction
            : Vector2.zero;
#else
        // Lê input do teclado (WASD / setas) no editor e builds desktop
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();
#endif
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}