using UnityEngine;

[CreateAssetMenu(fileName = "ClassDefinition", menuName = "Solengard/Class Definition")]
public class ClassDefinition : ScriptableObject
{
    [Header("Identidade")]
    public string classId;
    public string displayName;
    public string className;
    [TextArea(3, 6)]
    public string lore;

    [Header("Stats Base")]
    public float maxHP         = 120f;
    public float defense       = 12f;
    public float attackDamage  = 38f;
    public float moveSpeed     = 4f;

    [Header("Ataque Auto")]
    public float      attackRange     = 6.5f;
    public float      attackInterval  = 1f;
    public AttackType attackType      = AttackType.Melee360;
    public float      attackArc       = 270f;
    public int        projectileCount = 1;

    [Header("Poder Especial")]
    public string      specialName;
    public float       specialCooldown = 10f;
    public float       specialDuration = 5f;
    public SpecialType specialType;

    [Header("Sprites — pasta base")]
    public string spriteFolder;

    [Header("PPU dos sprites")]
    public float pixelsPerUnit = 32f;

    [Header("Desbloqueio")]
    public bool  unlockedByDefault  = false;
    public int   unlockCostDiamonds = 0;
    public float worldScale         = 1f;
}

public enum AttackType
{
    Melee360,     // Guerreiro — arco 270°
    Melee180,     // Paladino — arco 180° frontal
    MeleeCone,    // Assassino — cone 60° rápido
    RangedSingle, // Mago — projétil no mais próximo
    RangedMulti,  // Caçador — 2 alvos
    RangedSummon  // Necromante — bone shard + minion
}

public enum SpecialType
{
    BuffSelf,        // Guerreiro — Fúria Sanguínea
    BurstProjectile, // Mago — Nova Arcana
    Invulnerability, // Assassino — Evasão Sombria
    SummonMinions,   // Necromante — Invocação em Massa
    AuraDamage,      // Paladino — Aura Sagrada
    ArrowRain        // Caçador — Chuva de Flechas
}
