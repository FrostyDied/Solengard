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
    public const float MAX_MOVE_SPEED = 9f;

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

    // Histerese de flip: evita oscilação em movimentos diagonais/verticais
    int _facingSign = 1; // 1 = direita, -1 = esquerda
    const float FLIP_THRESHOLD = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _rb   = GetComponent<Rigidbody2D>();
        _sr   = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        _anim = GetComponent<CharacterAnimator>() ?? GetComponentInChildren<CharacterAnimator>();
        if (_sr   == null) Debug.LogError($"[PlayerController] SpriteRenderer não encontrado em '{gameObject.name}' nem em filhos.");
        if (_anim == null) Debug.LogError($"[PlayerController] CharacterAnimator não encontrado em '{gameObject.name}' nem em filhos.");

        _rb.gravityScale  = 0f;
        _rb.isKinematic   = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Debug.LogError("[PlayerController] Legacy Input desabilitado! Mude Active Input Handling para 'Both' em Project Settings > Player.");
#endif
    }

    void Update()      => InputManagement();
    void FixedUpdate()
    {
        Vector2 next = (Vector2)transform.position + MoveDir * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(next);
    }

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
        if (joystick.magnitude < 0.1f) joystick = Vector2.zero;
        if (MobileJoystick.Instance != null && MobileJoystick.Instance.IsActive)
            joystick = MobileJoystick.Instance.InputDirection;

        MoveDir = joystick.magnitude > 0.1f ? joystick.normalized : keyboardDir;
        if (MoveDir.magnitude < 0.05f) MoveDir = Vector2.zero;

        // Histerese: só troca de direção quando claramente comprometido com o novo lado.
        // Entre -0.5 e 0.5 (movimentos verticais ou diagonal suave) mantém o último flip.
        if (MoveDir.x > FLIP_THRESHOLD && _facingSign != 1)
        {
            _facingSign = 1;
            if (_sr != null) _sr.flipX = false;
        }
        else if (MoveDir.x < -FLIP_THRESHOLD && _facingSign != -1)
        {
            _facingSign = -1;
            if (_sr != null) _sr.flipX = true;
        }

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

}
