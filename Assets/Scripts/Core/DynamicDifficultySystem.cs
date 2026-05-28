using UnityEngine;

// Sistema de dificuldade dinâmica baseado em tempo de sessão.
// 5 tiers progressivos que aumentam HP/dano inimigo e desbloqueiam novos tipos.
// Singleton de cena (não DontDestroyOnLoad — reseta a cada run).
public class DynamicDifficultySystem : MonoBehaviour
{
    public static DynamicDifficultySystem Instance { get; private set; }
    public static event System.Action<int> OnDifficultyTierChanged;

    struct Tier
    {
        public float    timeThreshold;
        public float    healthMultiplier;
        public float    damageMultiplier;
        public float    spawnRateMultiplier;
        public int      maxEnemiesOnScreen;
        public string[] availableEnemyTypes;
    }

    static readonly Tier[] Tiers =
    {
        new Tier { timeThreshold=0,   healthMultiplier=1.0f, damageMultiplier=1.0f,  spawnRateMultiplier=1.0f, maxEnemiesOnScreen=20, availableEnemyTypes=new[]{"EnemyZumbi"} },
        new Tier { timeThreshold=60,  healthMultiplier=1.2f, damageMultiplier=1.1f,  spawnRateMultiplier=1.1f, maxEnemiesOnScreen=25, availableEnemyTypes=new[]{"EnemyZumbi","EnemyOrc"} },
        new Tier { timeThreshold=120, healthMultiplier=1.4f, damageMultiplier=1.25f, spawnRateMultiplier=1.2f, maxEnemiesOnScreen=30, availableEnemyTypes=new[]{"EnemyZumbi","EnemyOrc","EnemyArcher"} },
        new Tier { timeThreshold=180, healthMultiplier=1.6f, damageMultiplier=1.4f,  spawnRateMultiplier=1.3f, maxEnemiesOnScreen=35, availableEnemyTypes=new[]{"EnemyZumbi","EnemyOrc","EnemyArcher","EnemyMage","EnemyAssassin"} },
        new Tier { timeThreshold=240, healthMultiplier=2.0f, damageMultiplier=1.6f,  spawnRateMultiplier=1.5f, maxEnemiesOnScreen=50, availableEnemyTypes=new[]{"EnemyZumbi","EnemyOrc","EnemyArcher","EnemyMage","EnemyAssassin","EnemyGolem","EnemyBoss"} },
    };

    int currentTierIndex;

    // Tempo lido do GameManager.RunTimeSeconds — só avança quando Playing, reseta no StartGame
    float ElapsedTime => GameManager.Instance != null ? GameManager.Instance.RunTimeSeconds : 0f;

    public int   CurrentTierIndex    => currentTierIndex;
    public float HealthMultiplier    => Tiers[currentTierIndex].healthMultiplier;
    public float DamageMultiplier    => Tiers[currentTierIndex].damageMultiplier;
    public float SpawnRateMultiplier => Tiers[currentTierIndex].spawnRateMultiplier;
    public int   MaxEnemiesOnScreen  => Tiers[currentTierIndex].maxEnemiesOnScreen;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        TryAdvanceTier();
    }

    void TryAdvanceTier()
    {
        // Iterative loop handles multiple simultaneous tier jumps (e.g., editor timeskips) without stack overflow
        while (true)
        {
            int next = currentTierIndex + 1;
            if (next >= Tiers.Length) return;
            if (ElapsedTime < Tiers[next].timeThreshold) return;

            currentTierIndex = next;
            Debug.Log($"[DynamicDifficultySystem] Tier {next} ({ElapsedTime:F0}s) — HP×{HealthMultiplier} DMG×{DamageMultiplier}");
            OnDifficultyTierChanged?.Invoke(next);
        }
    }

    public string[] GetAvailableEnemyTypes() => Tiers[currentTierIndex].availableEnemyTypes;

    public void ApplyToEnemy(EnemyBase enemy)
    {
        var t = Tiers[currentTierIndex];
        enemy.maxHealth *= t.healthMultiplier;
        enemy.damage    *= t.damageMultiplier;
        enemy.InitializeHealth();
    }

    public void ResetTier()
    {
        currentTierIndex = 0;
    }
}
