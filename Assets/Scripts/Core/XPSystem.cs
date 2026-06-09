using UnityEngine;

public class XPSystem : MonoBehaviour
{
    public static XPSystem Instance { get; private set; }

    [Header("XP necessário por nível")]
    [SerializeField] int[] xpTable = { 50, 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600 };

    public int CurrentLevel      { get; private set; } = 1;
    public int CurrentXP         { get; private set; } = 0;
    public int LevelUpsThisZone  { get; private set; } = 0;
    public const int MaxLevelUpsPerZone = 3;
    public int XPToNextLevel => CurrentLevel <= xpTable.Length
        ? xpTable[CurrentLevel - 1]
        : xpTable[xpTable.Length - 1] + CurrentLevel * 50;

    public float XPProgress => (float)CurrentXP / XPToNextLevel;

    public static event System.Action<int> OnLevelUp;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ResetLevel()
    {
        CurrentXP        = 0;
        CurrentLevel     = 1;
        LevelUpsThisZone = 0;
        Debug.Log("[XP] Level resetado para nova zona");
    }

    public void AddXP(int amount)
    {
        CurrentXP += amount;
        while (CurrentXP >= XPToNextLevel)
        {
            CurrentXP -= XPToNextLevel;
            CurrentLevel++;
            Debug.Log($"[XP] Level UP! Nível {CurrentLevel}");
            if (LevelUpsThisZone >= MaxLevelUpsPerZone) continue;
            LevelUpsThisZone++;
            OnLevelUp?.Invoke(CurrentLevel);
        }
    }
}
