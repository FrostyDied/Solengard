using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateClassDefinitions
{
    const string OUTPUT_DIR = "Assets/Resources/Classes";

    [MenuItem("Solengard/Classes/Criar 6 ClassDefinitions")]
    static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(OUTPUT_DIR))
        {
            Directory.CreateDirectory(OUTPUT_DIR);
            AssetDatabase.Refresh();
        }

        Create(new ClassDefinition
        {
            classId             = "warrior",
            displayName         = "Kael, o Cinzeiro",
            className           = "Guerreiro",
            maxHP               = 180f,
            defense             = 20f,
            attackDamage        = 40f,
            moveSpeed           = 3.5f,
            attackRange         = 6.5f,
            attackInterval      = 0.8f,
            attackType          = AttackType.Melee360,
            attackArc           = 270f,
            projectileCount     = 1,
            specialName         = "Fúria Sanguínea",
            specialType         = SpecialType.BuffSelf,
            specialCooldown     = 8f,
            specialDuration     = 5f,
            spriteFolder        = "Assets/Art/Characters/Hero/Cavaleiro/Level1-3/PNG/Swordsman_lvl1/Without_shadow",
            pixelsPerUnit       = 32f,
            unlockedByDefault   = true,
            unlockCostDiamonds  = 0,
            worldScale          = 1.0f,
        });

        Create(new ClassDefinition
        {
            classId             = "mage",
            displayName         = "Seraphine, a Esgotada",
            className           = "Mago",
            maxHP               = 90f,
            defense             = 8f,
            attackDamage        = 50f,
            moveSpeed           = 4.5f,
            attackRange         = 6.5f,
            attackInterval      = 1.2f,
            attackType          = AttackType.RangedSingle,
            attackArc           = 270f,
            projectileCount     = 1,
            specialName         = "Nova Arcana",
            specialType         = SpecialType.BurstProjectile,
            specialCooldown     = 8f,
            specialDuration     = 5f,
            spriteFolder        = "Assets/Art/Characters/Hero/Mago/Lightning Mage",
            pixelsPerUnit       = 100f,
            unlockedByDefault   = false,
            unlockCostDiamonds  = 500,
            worldScale          = 0.32f,
        });

        Create(new ClassDefinition
        {
            classId             = "assassin",
            displayName         = "Vael, a Sombra Fraturada",
            className           = "Assassino",
            maxHP               = 110f,
            defense             = 10f,
            attackDamage        = 45f,
            moveSpeed           = 6.0f,
            attackRange         = 6.5f,
            attackInterval      = 0.35f,
            attackType          = AttackType.MeleeCone,
            attackArc           = 60f,
            projectileCount     = 1,
            specialName         = "Evasão Sombria",
            specialType         = SpecialType.Invulnerability,
            specialCooldown     = 12f,
            specialDuration     = 3f,
            spriteFolder        = "Assets/Art/Characters/Hero/Assassino/Assassino",
            pixelsPerUnit       = 32f,
            unlockedByDefault   = false,
            unlockCostDiamonds  = 800,
            worldScale          = 1.0f,
        });

        Create(new ClassDefinition
        {
            classId             = "necromancer",
            displayName         = "Marveth, o Incompleto",
            className           = "Necromante",
            maxHP               = 100f,
            defense             = 12f,
            attackDamage        = 35f,
            moveSpeed           = 4.0f,
            attackRange         = 6.5f,
            attackInterval      = 1.0f,
            attackType          = AttackType.RangedSummon,
            attackArc           = 270f,
            projectileCount     = 1,
            specialName         = "Invocação em Massa",
            specialType         = SpecialType.SummonMinions,
            specialCooldown     = 15f,
            specialDuration     = 10f,
            spriteFolder        = "Assets/Art/Characters/Hero/Necromante/Necromante",
            pixelsPerUnit       = 100f,
            unlockedByDefault   = false,
            unlockCostDiamonds  = 1000,
            worldScale          = 0.32f,
        });

        Create(new ClassDefinition
        {
            classId             = "paladin",
            displayName         = "Aldric, o Último Juramento",
            className           = "Paladino",
            maxHP               = 200f,
            defense             = 30f,
            attackDamage        = 25f,
            moveSpeed           = 3.0f,
            attackRange         = 6.5f,
            attackInterval      = 1.2f,
            attackType          = AttackType.Melee180,
            attackArc           = 180f,
            projectileCount     = 1,
            specialName         = "Aura Sagrada",
            specialType         = SpecialType.AuraDamage,
            specialCooldown     = 10f,
            specialDuration     = 6f,
            spriteFolder        = "Assets/Art/Characters/Hero/Paladino/Paladino",
            pixelsPerUnit       = 100f,
            unlockedByDefault   = false,
            unlockCostDiamonds  = 500,
            worldScale          = 0.32f,
        });

        Create(new ClassDefinition
        {
            classId             = "hunter",
            displayName         = "Rynn, a Sem-Descanso",
            className           = "Caçador",   // ç
            maxHP               = 120f,
            defense             = 12f,
            attackDamage        = 38f,
            moveSpeed           = 5.0f,
            attackRange         = 6.5f,
            attackInterval      = 1.0f,
            attackType          = AttackType.RangedMulti,
            attackArc           = 270f,
            projectileCount     = 2,
            specialName         = "Chuva de Flechas",
            specialType         = SpecialType.ArrowRain,
            specialCooldown     = 12f,
            specialDuration     = 3f,
            spriteFolder        = "Assets/Art/Characters/Hero/Caçador/Caçador",   // ç
            pixelsPerUnit       = 32f,
            unlockedByDefault   = false,
            unlockCostDiamonds  = 800,
            worldScale          = 1.0f,
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Classes] 6 ClassDefinitions criadas em " + OUTPUT_DIR);
    }

    [MenuItem("Solengard/Classes/Testar Carregamento")]
    static void TestLoad()
    {
        var classes = Resources.LoadAll<ClassDefinition>("Classes");
        Debug.Log($"[Teste] {classes.Length} classes carregadas:");
        foreach (var c in classes)
        {
            bool pathOk = System.IO.Directory.Exists(c.spriteFolder);
            Debug.Log($"  {c.className} ({c.classId}): HP={c.maxHP} ATK={c.attackDamage} PPU={c.pixelsPerUnit} worldScale={c.worldScale} pathOk={pathOk}");
        }
    }

    static void Create(ClassDefinition data)
    {
        string path = $"{OUTPUT_DIR}/{data.classId}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<ClassDefinition>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(data, existing);
            EditorUtility.SetDirty(existing);
            Debug.Log($"[Classes] Atualizado: {path}");
            return;
        }

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[Classes] Criado: {path}");
    }
}
