using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movimento")]
    public float moveSpeed = 5f;

    // Direção atual do movimento (fonte da verdade para flip e ataque)
    public Vector2 MoveDir { get; private set; }

    // Direção para onde o player está virado (para ataque direcional)
    public Vector2 FacingDirection { get; private set; } = Vector2.right;

    // Input do joystick mobile (pode ser setado pelo MobileJoystick)
    public Vector2 JoystickInput { get; set; }

    // Tempo do último ataque — lido pelo PlayerAttack; não bloqueia mais o flip
    public float LastAttackTime { get; set; } = -999f;

    Rigidbody2D       _rb;
    SpriteRenderer    _sr;
    CharacterAnimator _anim;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _rb   = GetComponent<Rigidbody2D>();
        _sr   = GetComponent<SpriteRenderer>();
        _anim = GetComponent<CharacterAnimator>();

        _rb.gravityScale           = 0f;
        _rb.freezeRotation         = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        _rb.mass                   = 1000f; // prevents enemies from pushing the player on physical contact

        // Zero-friction material so the player slides along obstacle edges cleanly
        var slideMat = new PhysicsMaterial2D("Slide") { friction = 0f, bounciness = 0f };
        var col = GetComponent<Collider2D>();
        if (col != null) col.sharedMaterial = slideMat;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Debug.LogError("[PlayerController] Legacy Input desabilitado! Mude Active Input Handling para 'Both' em Project Settings > Player.");
#endif
    }

    void Update()    => InputManagement();
    void FixedUpdate() => Move();

    void InputManagement()
    {
        Vector2 keyboardDir = Vector2.zero;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current != null)
        {
            float x = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ?  1f :
                      Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed  ? -1f : 0f;
            float y = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed   ?  1f :
                      Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed  ? -1f : 0f;
            keyboardDir = new Vector2(x, y).normalized;
        }
#else
        keyboardDir = new Vector2(Input.GetAxisRaw("Horizontal"),
                                  Input.GetAxisRaw("Vertical")).normalized;
#endif

        // MobileJoystick sobrescreve teclado quando ativo
        Vector2 joystick = JoystickInput;
        if (MobileJoystick.Instance != null && MobileJoystick.Instance.IsActive)
            joystick = MobileJoystick.Instance.InputDirection;

        MoveDir = joystick.magnitude > 0.1f ? joystick.normalized : keyboardDir;

        // Flip do sprite SEMPRE segue o movimento imediatamente (virar rápido = sobrevivência)
        if (_sr != null && Mathf.Abs(MoveDir.x) > 0.01f)
            _sr.flipX = MoveDir.x < 0f;

        // FacingDirection suporta 4 direções — eixo dominante define a frente do ataque
        if (MoveDir.magnitude > 0.1f)
        {
            if (Mathf.Abs(MoveDir.x) >= Mathf.Abs(MoveDir.y))
                FacingDirection = MoveDir.x > 0f ? Vector2.right : Vector2.left;
            else
                FacingDirection = MoveDir.y > 0f ? Vector2.up : Vector2.down;
        }

        if (_anim != null)
            _anim.SetState(MoveDir.magnitude > 0.1f
                ? CharacterAnimator.State.Walk
                : CharacterAnimator.State.Idle);
    }

    void Move()
    {
        _rb.linearVelocity = MoveDir * moveSpeed;
    }
}
