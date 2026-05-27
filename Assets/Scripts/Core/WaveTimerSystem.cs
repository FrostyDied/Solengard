using System.Collections;
using UnityEngine;

public class WaveTimerSystem : MonoBehaviour
{
    public static event System.Action           OnWaveTimerExpired;
    public static event System.Action<float>    OnTimerTick;

    [Header("Configuração")]
    [SerializeField] float waveDuration = 60f;

    float    timeRemaining;
    bool     isRunning;
    Coroutine timerCoroutine;

    void OnEnable()  => WaveManager.OnWaveCompleted += OnWaveCompleted;
    void OnDisable() => WaveManager.OnWaveCompleted -= OnWaveCompleted;

    void OnWaveCompleted(int _) => StopTimer();

    public void StartTimer()
    {
        StopTimer();
        timeRemaining  = waveDuration;
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        isRunning = false;
    }

    IEnumerator TimerRoutine()
    {
        isRunning = true;

        while (timeRemaining > 0f)
        {
            yield return null;
            timeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Max(0f, timeRemaining));
        }

        timeRemaining = 0f;
        OnTimerTick?.Invoke(0f);
        isRunning = false;

        ApplyFury();
        OnWaveTimerExpired?.Invoke();
    }

    void ApplyFury()
    {
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase e in enemies)
        {
            e.moveSpeed *= 1.5f;
            e.damage    *= 1.25f;
        }
        Debug.Log($"[WaveTimerSystem] Fúria aplicada — {enemies.Length} inimigos afetados.");
    }

    public float TimeRemaining => Mathf.Max(0f, timeRemaining);
    public bool  IsRunning     => isRunning;
    public float WaveDuration  => waveDuration;
}
