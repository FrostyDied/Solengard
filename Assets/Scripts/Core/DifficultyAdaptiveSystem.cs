using UnityEngine;

// Reduz silenciosamente a dificuldade quando o jogador morre 3+ vezes na mesma wave.
// Nunca comunica os ajustes ao jogador.
public class DifficultyAdaptiveSystem : MonoBehaviour
{
    public static DifficultyAdaptiveSystem Instance { get; private set; }

    const string PREF_PREFIX = "adaptive_deaths_wave_";
    const int    MAX_WAVES   = 30;

    int  currentWave     = 1;
    int  deathsThisWave  = 0;
    bool modifierApplied = false;

    public float EnemyHealthModifier { get; private set; } = 1f;
    public float EnemySpeedModifier  { get; private set; } = 1f;
    public float EnemyDamageModifier { get; private set; } = 1f;
    public int   EnemyCountReduction { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        PlayerHealth.OnPlayerDied      += HandlePlayerDied;
        ZoneManager.OnZoneCompleted    += HandleWaveCompleted;
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    void OnDisable()
    {
        PlayerHealth.OnPlayerDied      -= HandlePlayerDied;
        ZoneManager.OnZoneCompleted    -= HandleWaveCompleted;
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            // Always start fresh — stale prefs from a crash would incorrectly apply the reducer
            ClearSavedDeaths();
            currentWave     = 1;
            deathsThisWave  = 0;
            modifierApplied = false;

            float maldicao = PermanentUpgradeSystem.Instance?.MaldicaoDifficultyBonus ?? 0f;
            EnemyHealthModifier  = 1f + maldicao;
            EnemySpeedModifier   = 1f + maldicao * 0.5f;
            EnemyDamageModifier  = 1f + maldicao * 0.7f;
        }
        else if (state == GameState.GameOver || state == GameState.Victory)
        {
            ClearSavedDeaths();
            if (state == GameState.Victory)
                ResetModifiers();
        }
    }

    void HandlePlayerDied()
    {
        deathsThisWave++;
        PlayerPrefs.SetInt(PREF_PREFIX + currentWave, deathsThisWave);
        PlayerPrefs.Save();

        if (deathsThisWave >= 3 && !modifierApplied)
        {
            modifierApplied      = true;
            EnemyHealthModifier  = Mathf.Max(0.1f, EnemyHealthModifier - 0.15f);
            EnemyCountReduction += 1;
        }
    }

    void HandleWaveCompleted(int wave)
    {
        currentWave     = wave + 1;
        deathsThisWave  = PlayerPrefs.GetInt(PREF_PREFIX + currentWave, 0);
        modifierApplied = deathsThisWave >= 3;
    }

    void ClearSavedDeaths()
    {
        for (int i = 1; i <= MAX_WAVES; i++)
            PlayerPrefs.DeleteKey(PREF_PREFIX + i);
        PlayerPrefs.Save();
    }

    void ResetModifiers()
    {
        EnemyHealthModifier = 1f;
        EnemySpeedModifier  = 1f;
        EnemyDamageModifier = 1f;
        EnemyCountReduction = 0;
    }

    public void AjustarPorBoostPlayer(float hpBonus = 0f, float speedBonus = 0f, float damageBonus = 0f)
    {
        EnemyHealthModifier += hpBonus;
        EnemySpeedModifier  += speedBonus;
        EnemyDamageModifier += damageBonus;
        var enemies = UnityEngine.Object.FindObjectsByType<EnemyBase>(UnityEngine.FindObjectsSortMode.None);
        foreach (var e in enemies)
            if (!e.IsDead)
            {
                e.maxHealth += e.maxHealth * hpBonus;
                e.moveSpeed += e.moveSpeed * speedBonus;
                e.damage    += e.damage    * damageBonus;
            }
    }
}
