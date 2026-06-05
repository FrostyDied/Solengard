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
    float _attackLockTimer;

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
        if (_attackLockTimer > 0f) _attackLockTimer -= Time.deltaTime;

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
        // Durante lock de ataque, só Hurt e Death podem sobrescrever
        if (_attackLockTimer > 0f && newState != State.Hurt && newState != State.Death) return;
        if (_locked || newState == _state) return;
        _state  = newState;
        _frame  = 0;
        _timer  = 0f;
        _locked = newState == State.Death;
    }

    // Inicia animação de ataque e bloqueia sobrescrita por duration segundos
    public void LockAttack(float duration)
    {
        _attackLockTimer = duration;
        _state  = State.Attack;
        _frame  = 0;
        _timer  = 0f;
        _locked = false;
    }

    public void ForceState(State newState)
    {
        _locked = false;
        _state  = newState;
        _frame  = 0;
        _timer  = 0f;
    }

    public State CurrentState => _state;

    public void OverrideFrames(Sprite[] idle, Sprite[] walk, Sprite[] attack, Sprite[] hurt, Sprite[] death)
    {
        if (idle   != null && idle.Length   > 0) idleFrames   = idle;
        if (walk   != null && walk.Length   > 0) walkFrames   = walk;
        if (attack != null && attack.Length > 0) attackFrames = attack;
        if (hurt   != null && hurt.Length   > 0) hurtFrames   = hurt;
        if (death  != null && death.Length  > 0) deathFrames  = death;
        _frame = 0;
        _timer = 0f;
    }
}
