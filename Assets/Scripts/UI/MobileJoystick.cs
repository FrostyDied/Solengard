using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Joystick virtual fixo no canto inferior esquerdo para builds mobile.
// Attach no JoystickBackground. Conecte knobTransform no Inspector ou via Layout GameScene.
// No editor só ativa se forceEnableInEditor = true; caso contrário o GO é desativado.
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static MobileJoystick Instance { get; private set; }

    [SerializeField] RectTransform knobTransform;
    [SerializeField] float         maxRadius           = 80f;
    [SerializeField] bool          forceEnableInEditor = false;

    public bool    IsActive       { get; private set; }
    public Vector2 InputDirection { get; private set; }
    public Vector2 Direction      => InputDirection; // backward compat

    RectTransform rectTransform;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance      = this;
        rectTransform = GetComponent<RectTransform>();

#if !UNITY_ANDROID && !UNITY_IOS
        if (!forceEnableInEditor)
            gameObject.SetActive(false);
#endif
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsActive = true;
        UpdateKnob(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateKnob(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsActive       = false;
        InputDirection = Vector2.zero;
        if (knobTransform != null)
            knobTransform.anchoredPosition = Vector2.zero;
    }

    void UpdateKnob(PointerEventData eventData)
    {
        if (knobTransform == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 local);

        Vector2 clamped            = Vector2.ClampMagnitude(local, maxRadius);
        knobTransform.anchoredPosition = clamped;
        InputDirection             = clamped / maxRadius;
    }
}
