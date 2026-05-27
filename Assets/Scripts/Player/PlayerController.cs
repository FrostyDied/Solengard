using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Vector2     moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        moveInput.x = Keyboard.current != null ?
            (Keyboard.current.dKey.isPressed ? 1 : Keyboard.current.aKey.isPressed ? -1 : 0) : 0;
        moveInput.y = Keyboard.current != null ?
            (Keyboard.current.wKey.isPressed ? 1 : Keyboard.current.sKey.isPressed ? -1 : 0) : 0;
#else
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
#endif

        if (MobileJoystick.Instance != null && MobileJoystick.Instance.IsActive)
            moveInput = MobileJoystick.Instance.InputDirection;

        moveInput = moveInput.normalized;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
