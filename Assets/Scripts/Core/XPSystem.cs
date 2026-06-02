using UnityEngine;

public class XPSystem : MonoBehaviour
{
    public static XPSystem Instance { get; private set; }

    [Header("XP necessário por nível")]
    [SerializeField] int[] xpTable = { 30, 65, 110, 170, 250, 350, 475, 625, 800, 1000 };

    public int CurrentLevel  { get; private set; } = 1;
    public int CurrentXP     { get; private set; } = 0;
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
        CurrentXP    = 0;
        CurrentLevel = 1;
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
            OnLevelUp?.Invoke(CurrentLevel);
        }
    }
}
