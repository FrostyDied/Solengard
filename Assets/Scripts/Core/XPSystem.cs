using UnityEngine;

public class XPSystem : MonoBehaviour
{
    public static XPSystem Instance { get; private set; }

    [Header("XP necessário por nível")]
    [SerializeField] int[] xpTable = { 10, 20, 35, 55, 80, 110, 145, 185, 230, 280 };

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
