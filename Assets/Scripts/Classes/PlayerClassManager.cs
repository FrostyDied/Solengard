using UnityEngine;

public class PlayerClassManager : MonoBehaviour
{
    public static PlayerClassManager Instance { get; private set; }
    public ClassDefinition CurrentClass { get; private set; }

    const string SELECTED_CLASS_KEY = "selected_class";
    const string DEFAULT_CLASS      = "warrior";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadSelectedClass();
    }

    void LoadSelectedClass()
    {
        string classId = PlayerPrefs.GetString(SELECTED_CLASS_KEY, DEFAULT_CLASS);
        CurrentClass = Resources.Load<ClassDefinition>($"Classes/{classId}");

        if (CurrentClass == null)
        {
            Debug.LogWarning($"[ClassManager] Classe '{classId}' não encontrada — usando {DEFAULT_CLASS}");
            CurrentClass = Resources.Load<ClassDefinition>($"Classes/{DEFAULT_CLASS}");
        }

        Debug.Log($"[ClassManager] Classe carregada: {CurrentClass?.className ?? "NENHUMA"}");
    }

    public void SelectClass(string classId)
    {
        var def = Resources.Load<ClassDefinition>($"Classes/{classId}");
        if (def == null)
        {
            Debug.LogError($"[ClassManager] Classe '{classId}' não existe");
            return;
        }
        if (!IsClassUnlocked(classId))
        {
            Debug.LogWarning($"[ClassManager] Classe '{classId}' bloqueada");
            return;
        }
        PlayerPrefs.SetString(SELECTED_CLASS_KEY, classId);
        PlayerPrefs.Save();
        CurrentClass = def;
    }

    public bool IsClassUnlocked(string classId)
    {
        var def = Resources.Load<ClassDefinition>($"Classes/{classId}");
        if (def == null) return false;
        if (def.unlockedByDefault) return true;
        return PlayerPrefs.GetInt($"class_unlocked_{classId}", 0) == 1;
    }

    public void UnlockClass(string classId)
    {
        PlayerPrefs.SetInt($"class_unlocked_{classId}", 1);
        PlayerPrefs.Save();
        Debug.Log($"[ClassManager] Classe '{classId}' desbloqueada");
    }

    public void ApplyClassToPlayer(GameObject playerGO)
    {
        Debug.Log($"[ClassManager] ApplyClassToPlayer chamado. Classe={CurrentClass?.className ?? "NULL"}, selected_class PlayerPrefs={PlayerPrefs.GetString(SELECTED_CLASS_KEY, "vazio")}");

        if (CurrentClass == null) { Debug.LogError("[ClassManager] CurrentClass é null — LoadSelectedClass falhou?"); return; }

        var health = playerGO.GetComponent<PlayerHealth>();
        if (health != null)
            health.SetMaxHealth(CurrentClass.maxHP);

        var attack = playerGO.GetComponent<PlayerAttack>();
        if (attack != null)
            attack.SetClassConfig(
                CurrentClass.attackDamage,
                CurrentClass.attackRange,
                CurrentClass.attackInterval,
                CurrentClass.attackType,
                CurrentClass.attackArc,
                CurrentClass.projectileCount);

        var controller = playerGO.GetComponent<PlayerController>();
        if (controller != null)
            controller.SetMoveSpeed(CurrentClass.moveSpeed);

        playerGO.transform.localScale = Vector3.one * CurrentClass.worldScale;

        var anim = playerGO.GetComponent<CharacterAnimator>();
        int idleCount = CurrentClass.idleFrames?.Length ?? 0;
        Debug.Log($"[ClassManager] CharacterAnimator={anim != null}, idleFrames={idleCount}, walkFrames={CurrentClass.walkFrames?.Length ?? 0}");
        if (anim != null && idleCount > 0)
        {
            anim.OverrideFrames(
                CurrentClass.idleFrames,
                CurrentClass.walkFrames,
                CurrentClass.attackFrames,
                CurrentClass.hurtFrames,
                CurrentClass.deathFrames);
            Debug.Log($"[ClassManager] OverrideFrames aplicado — {idleCount} frames idle");
        }
        else if (idleCount == 0)
        {
            Debug.LogWarning($"[ClassManager] idleFrames vazio para '{CurrentClass.classId}' — rode Solengard > Classes > Setup Hero Animations no Editor");
        }

        Debug.Log($"[ClassManager] Player configurado como {CurrentClass.className}: sprite trocado, stats aplicados");
    }
}
