using UnityEngine;
using UnityEngine.UI;

// Joystick virtual fixo para builds mobile.
// No editor usa WASD; em Android/iOS usa touch.
// PlayerController deve verificar MobileJoystick.Instance?.Direction para obter o input.
public class MobileJoystick : MonoBehaviour
{
    public static MobileJoystick Instance { get; private set; }

    [Header("Joystick")]
    public RectTransform areaJoystick;  // círculo externo (fundo)
    public RectTransform knob;          // círculo interno (alça)
    [Min(10f)] public float raio = 80f;

    [Header("Botão de habilidade (canto direito)")]
    public Button botaoHabilidade;

    // Direção normalizada do joystick; lida pelo PlayerController
    public Vector2 Direction { get; private set; }

    int touchIdJoystick = -1;
    Vector2 origemJoystick;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        ProcessarTouch();
#else
        // Editor: simula joystick com WASD
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Direction = new Vector2(h, v).normalized;
        AtualizarKnob(Direction * raio);
#endif
    }

#if UNITY_ANDROID || UNITY_IOS
    void ProcessarTouch()
    {
        foreach (Touch touch in Input.touches)
        {
            // Apenas touches no lado esquerdo da tela controlam o joystick
            bool ladoEsquerdo = touch.position.x < Screen.width * 0.5f;

            switch (touch.phase)
            {
                case TouchPhase.Began when ladoEsquerdo && touchIdJoystick == -1:
                    touchIdJoystick = touch.fingerId;
                    origemJoystick  = touch.position;
                    break;

                case TouchPhase.Moved when touch.fingerId == touchIdJoystick:
                case TouchPhase.Stationary when touch.fingerId == touchIdJoystick:
                    Vector2 delta    = touch.position - origemJoystick;
                    Vector2 clamped  = Vector2.ClampMagnitude(delta, raio);
                    Direction        = clamped / raio;
                    AtualizarKnob(clamped);
                    break;

                case TouchPhase.Ended when touch.fingerId == touchIdJoystick:
                case TouchPhase.Canceled when touch.fingerId == touchIdJoystick:
                    ResetarJoystick();
                    break;
            }
        }
    }
#endif

    void AtualizarKnob(Vector2 offset)
    {
        if (knob != null)
            knob.anchoredPosition = offset;
    }

    void ResetarJoystick()
    {
        touchIdJoystick = -1;
        Direction       = Vector2.zero;
        AtualizarKnob(Vector2.zero);
    }
}
