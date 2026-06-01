using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CharacterAnimator : MonoBehaviour
{
    public enum State { Idle, Walk, Attack, Hurt, Death }

    [Header("Frames por estado")]
    [SerializeField] Sprite[] idleFrames;
    [SerializeField] Sprite[] walkFrames;
    [SerializeField] Sprite[] attackFrames;
    [SerializeField] Sprite[] hurtFrames;
    [SerializeField] Sprite[] deathFrames;

    [Header("Velocidade (frames por segundo)")]
    [SerializeField] float fps = 8f;

    SpriteRenderer _sr;
    State _state;
    int   _frame;
    float _timer;
    bool  _locked;

    void Awake()
    {
        _sr     = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (_sr == null) Debug.LogError($"[CharacterAnimator] SpriteRenderer não encontrado em '{gameObject.name}' nem em filhos.");
        _state  = State.Idle;
        _frame  = 0;
        _timer  = 0f;
        _locked = false;
    }

    void Start()
    {
        WarnFrameCount("idle", idleFrames);
        WarnFrameCount("walk", walkFrames);
    }

    void WarnFrameCount(string label, Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return;
        if (frames.Length < 4)
            Debug.LogWarning($"[CharacterAnimator] {gameObject.name} '{label}': {frames.Length} frame(s) — ideal 4-6 para animação suave.");
    }

    void Update()
    {
        var frames = GetFrames(_state);
        if (frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer < 1f / fps) return;
        _timer = 0f;
        _frame++;

        if (_frame >= frames.Length)
        {
            if (_locked) { _frame = frames.Length - 1; return; }
            _frame = 0;
        }

        if (frames[_frame] != null)
            _sr.sprite = frames[_frame];
    }

    Sprite[] GetFrames(State s) => s switch
    {
        State.Walk   => walkFrames?.Length   > 0 ? walkFrames   : idleFrames,
        State.Attack => attackFrames?.Length > 0 ? attackFrames : idleFrames,
        State.Hurt   => hurtFrames?.Length   > 0 ? hurtFrames   : idleFrames,
        State.Death  => deathFrames?.Length  > 0 ? deathFrames  : idleFrames,
        _            => idleFrames
    };

    public void SetState(State newState)
    {
        if (_locked || newState == _state) return;
        _state = newState;
        _frame = 0;
        _timer = 0f;
        _locked = newState == State.Death;
    }

    public void ForceState(State newState)
    {
        _locked = false;
        _state  = newState;
        _frame  = 0;
        _timer  = 0f;
    }

    public State CurrentState => _state;
}
