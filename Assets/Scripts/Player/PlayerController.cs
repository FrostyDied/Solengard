using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

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
            (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1 :
             Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed  ? -1 : 0) : 0;
        moveInput.y = Keyboard.current != null ?
            (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed   ? 1 :
             Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? -1 : 0) : 0;
#else
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
#endif

        if (MobileJoystick.Instance != null && MobileJoystick.Instance.IsActive)
            moveInput = MobileJoystick.Instance.InputDirection;

        moveInput = moveInput.normalized;

        var anim = GetComponent<CharacterAnimator>();
        if (anim != null)
            anim.SetState(moveInput.magnitude > 0.1f
                ? CharacterAnimator.State.Walk
                : CharacterAnimator.State.Idle);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && Mathf.Abs(moveInput.x) > 0.01f
            && Time.time - PlayerAttack.LastAttackTime > 0.3f)
            sr.flipX = moveInput.x < 0f;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
