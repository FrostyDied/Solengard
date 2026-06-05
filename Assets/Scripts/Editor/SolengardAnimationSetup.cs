using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SolengardAnimationSetup
{
    struct Entry
    {
        public string prefabPath;
        public string spriteFolder;
    }

    struct BossEntry
    {
        public string prefabPath;
        public string idleFolder;
        public string walkFolder;
        public string attackFolder;
        public string hurtFolder;
        public string deathFolder;
    }

    static readonly Entry[] _entries =
    {
        // ── Players ─────────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level1.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level1-3/PNG/Swordsman_lvl1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level2.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level1-3/PNG/Swordsman_lvl2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level3.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level1-3/PNG/Swordsman_lvl3/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level4.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level4-6/PNG/Swordsman_lvl4/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level5.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level4-6/PNG/Swordsman_lvl5/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level6.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level4-6/PNG/Swordsman_lvl6/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level7.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level7-9/PNG/Swordsman_lvl7/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level8.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level7-9/PNG/Swordsman_lvl8/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Characters/Player_Level9.prefab", spriteFolder = "Assets/Art/Characters/Hero/Cavaleiro/Level7-9/PNG/Swordsman_lvl9/Without_shadow" },

        // ── Slimes ──────────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemySlime.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Slime/PNG/Slime1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemySlime2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Slime/PNG/Slime2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemySlime3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Slime/PNG/Slime3/Without_shadow" },

        // ── Zombies ─────────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyZumbi.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Zombie/Premium/PNG/Zombie1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyZumbi2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Zombie/Premium/PNG/Zombie2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyZumbi3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Zombie/Premium/PNG/Zombie3/Without_shadow" },

        // ── Orcs (Gnoll) ────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyOrc.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Gnoll/PNG/Gnoll1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyOrc2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Gnoll/PNG/Gnoll2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyOrc3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Gnoll/PNG/Gnoll3/Without_shadow" },

        // ── Archers (Skeleton) ───────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyArcher.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Skeleton/Premium/PNG/Skeleton1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyArcher2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Skeleton/Premium/PNG/Skeleton2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyArcher3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Skeleton/Premium/PNG/Skeleton3/Without_shadow" },

        // ── Assassins (Ghost) ────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyAssassin.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Ghost/PNG/Ghost1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyAssassin2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Ghost/PNG/Ghost2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyAssassin3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Ghost/PNG/Ghost3/Without_shadow" },

        // ── Mages (Demon) ────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyMage.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Demon/PNG/Demon1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyMage2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Demon/PNG/Demon2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyMage3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Demon/PNG/Demon3/Without_shadow" },

        // ── Golems ──────────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyGolem.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Golem/PNG/Golem1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyGolem2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Golem/PNG/Golem2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyGolem3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Golem/PNG/Golem3/Without_shadow" },

        // ── Bosses (Lich) ────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyBoss.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Lich/PNG/Lich1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyBoss2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Lich/PNG/Lich2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyBoss3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Lich/PNG/Lich3/Without_shadow" },

        // ── Goblins ─────────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyGoblin.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Goblin/PNG/Goblin1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyGoblin2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Goblin/PNG/Goblin2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyGoblin3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Goblin/PNG/Goblin3/Without_shadow" },

        // ── Dark Elves ───────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyDarkElf.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/DarkElf/Elf_1" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyDarkElf2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/DarkElf/Elf_2" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyDarkElf3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/DarkElf/Elf_3" },

        // ── Orcs Heavy ───────────────────────────────────────────────────────────────
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyOrcHeavy.prefab",  spriteFolder = "Assets/Art/Characters/Enemies/Orc/PNG/Orc1/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyOrcHeavy2.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Orc/PNG/Orc2/Without_shadow" },
        new Entry { prefabPath = "Assets/Prefabs/Enemies/EnemyOrcHeavy3.prefab", spriteFolder = "Assets/Art/Characters/Enemies/Orc/PNG/Orc3/Without_shadow" },
    };

    const string BOSS_ROOT = "Assets/Art/Characters/Enemies/Boss";

    static readonly BossEntry[] _bossEntries =
    {
        new BossEntry {
            prefabPath   = "Assets/Prefabs/Enemies/BossCaveman.prefab",
            idleFolder   = BOSS_ROOT + "/Caveman Boss/PNG/PNG Sequences/Front - Idle",
            walkFolder   = BOSS_ROOT + "/Caveman Boss/PNG/PNG Sequences/Front - Walking",
            attackFolder = BOSS_ROOT + "/Caveman Boss/PNG/PNG Sequences/Front - Attacking",
            hurtFolder   = BOSS_ROOT + "/Caveman Boss/PNG/PNG Sequences/Front - Hurt",
            deathFolder  = BOSS_ROOT + "/Caveman Boss/PNG/PNG Sequences/Dying",
        },
        new BossEntry {
            prefabPath   = "Assets/Prefabs/Enemies/BossGiantGoblin.prefab",
            idleFolder   = BOSS_ROOT + "/Giant Goblin/PNG/PNG Sequences/Front - Idle",
            walkFolder   = BOSS_ROOT + "/Giant Goblin/PNG/PNG Sequences/Front - Walking",
            attackFolder = BOSS_ROOT + "/Giant Goblin/PNG/PNG Sequences/Front - Attacking",
            hurtFolder   = BOSS_ROOT + "/Giant Goblin/PNG/PNG Sequences/Front - Hurt",
            deathFolder  = BOSS_ROOT + "/Giant Goblin/PNG/PNG Sequences/Dying",
        },
        new BossEntry {
            prefabPath   = "Assets/Prefabs/Enemies/BossVikingLeader.prefab",
            idleFolder   = BOSS_ROOT + "/Viking Leader/PNG/PNG Sequences/Front - Idle",
            walkFolder   = BOSS_ROOT + "/Viking Leader/PNG/PNG Sequences/Front - Walking",
            attackFolder = BOSS_ROOT + "/Viking Leader/PNG/PNG Sequences/Front - Attacking",
            hurtFolder   = BOSS_ROOT + "/Viking Leader/PNG/PNG Sequences/Front - Hurt",
            deathFolder  = BOSS_ROOT + "/Viking Leader/PNG/PNG Sequences/Dying",
        },
    };

    // ── Configuração de linha lateral ────────────────────────────────────────
    // Índice da linha do sprite sheet que contém a animação side-facing.
    // 0 = topo (maior Y no espaço Unity), contando para baixo.
    // Rode "Solengard → Debug → Preview Sheet Rows" para identificar o índice correto.
    const int   SIDE_ROW_INDEX  = 0; // Craftpix: linha 0 (Y mais alto) = Down (de frente para câmera)
    const float ROW_Y_TOLERANCE = 6f;
    const float MIN_SPRITE_SIZE = 8f; // descarta artefatos de auto-slice com menos de 8px

    [MenuItem("Solengard/Setup Animations")]
    static void SetupAnimations()
    {
        int configured = 0;
        var noIdle  = new List<string>();
        var noWalk  = new List<string>();
        var log     = new System.Text.StringBuilder();

        foreach (var entry in _entries)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[AnimSetup] Prefab não encontrado: {entry.prefabPath}");
                continue;
            }

            if (!AssetDatabase.IsValidFolder(entry.spriteFolder))
            {
                Debug.LogWarning($"[AnimSetup] Pasta não encontrada: {entry.spriteFolder}");
            }

            string[] pngPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { entry.spriteFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            // "shadow_" (not "shadow") so "without_shadow" filenames are NOT excluded — only standalone shadow variants are.
            string idlePath   = FindStateFile(pngPaths, new[] { "idle" },                           new[] { "shadow_" });
            string walkPath   = FindStateFile(pngPaths, new[] { "walk" },                           new[] { "run", "attack", "shadow_" });
            string attackPath = FindStateFile(pngPaths, new[] { "attack" },                         new[] { "walk", "run", "shadow_" });
            string hurtPath   = FindStateFile(pngPaths, new[] { "hurt", "hit" },                   new[] { "shadow_" });
            string deathPath  = FindStateFile(pngPaths, new[] { "death", "dead", "dying", "die" },  new[] { "shadow_" });

            var idle   = ExtractSideRow(idlePath);
            var walk   = ExtractSideRow(walkPath);
            var attack = ExtractSideRow(attackPath);
            var hurt   = ExtractSideRow(hurtPath);
            var death  = ExtractSideRow(deathPath);

            string prefabName = Path.GetFileNameWithoutExtension(entry.prefabPath);
            if (idle.Length == 0) noIdle.Add(prefabName);
            if (walk.Length == 0) noWalk.Add(prefabName);

            log.AppendLine($"  {prefabName}:");
            log.AppendLine($"    idle={idle.Length} ({Path.GetFileNameWithoutExtension(idlePath ?? "?")}), walk={walk.Length} ({Path.GetFileNameWithoutExtension(walkPath ?? "?")}), attack={attack.Length}, hurt={hurt.Length}, death={death.Length}");

            var contents = PrefabUtility.LoadPrefabContents(entry.prefabPath);
            try
            {
                var anim = contents.GetComponent<CharacterAnimator>()
                        ?? contents.AddComponent<CharacterAnimator>();

                var so = new SerializedObject(anim);
                SetSpriteArray(so, "idleFrames",   idle);
                SetSpriteArray(so, "walkFrames",   walk);
                SetSpriteArray(so, "attackFrames", attack);
                SetSpriteArray(so, "hurtFrames",   hurt);
                SetSpriteArray(so, "deathFrames",  death);
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(contents, entry.prefabPath);
                configured++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ── Boss prefabs com PNGs individuais por pasta ──────────────────────────
        foreach (var entry in _bossEntries)
        {
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[AnimSetup] Boss prefab não encontrado: {entry.prefabPath}");
                continue;
            }

            var idle   = LoadFramesFromFolder(entry.idleFolder);
            var walk   = LoadFramesFromFolder(entry.walkFolder);
            var attack = LoadFramesFromFolder(entry.attackFolder);
            var hurt   = LoadFramesFromFolder(entry.hurtFolder);
            var death  = LoadFramesFromFolder(entry.deathFolder);

            string prefabName = Path.GetFileNameWithoutExtension(entry.prefabPath);
            log.AppendLine($"  {prefabName} [boss]: idle={idle.Length} walk={walk.Length} attack={attack.Length} hurt={hurt.Length} death={death.Length}");
            if (idle.Length == 0) noIdle.Add(prefabName);
            if (walk.Length == 0) noWalk.Add(prefabName);

            var contents = PrefabUtility.LoadPrefabContents(entry.prefabPath);
            try
            {
                var anim = contents.GetComponent<CharacterAnimator>()
                        ?? contents.AddComponent<CharacterAnimator>();
                var so = new SerializedObject(anim);
                SetSpriteArray(so, "idleFrames",   idle);
                SetSpriteArray(so, "walkFrames",   walk);
                SetSpriteArray(so, "attackFrames", attack);
                SetSpriteArray(so, "hurtFrames",   hurt);
                SetSpriteArray(so, "deathFrames",  death);
                so.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(contents, entry.prefabPath);
                configured++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        Debug.Log($"[AnimSetup] Concluído.\n{log}");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Animações configuradas: {configured} prefabs");
        if (noIdle.Count > 0)
            sb.AppendLine($"\nSem idle frames: {noIdle.Count}\n  {string.Join(", ", noIdle)}");
        if (noWalk.Count > 0)
            sb.AppendLine($"\nSem walk frames: {noWalk.Count}\n  {string.Join(", ", noWalk)}");

        EditorUtility.DisplayDialog("Solengard — Setup Animations", sb.ToString(), "OK");
    }

    [MenuItem("Solengard/Setup Animations", validate = true)]
    static bool Validate() => true;

    // Returns the path of the FIRST png that matches keywords and doesn't match excludeKeywords.
    static string FindStateFile(string[] allPaths, string[] keywords, string[] excludeKeywords = null)
    {
        foreach (var path in allPaths)
        {
            string filename = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (!keywords.Any(k => filename.Contains(k))) continue;
            if (excludeKeywords != null && excludeKeywords.Any(k => filename.Contains(k))) continue;
            return path;
        }
        return null;
    }

    // Extracts only the SIDE_ROW_INDEX row from a directional sprite sheet.
    // Rows are sorted top→bottom (descending Y). Single-row sheets always return row 0.
    // Sprites smaller than MIN_SPRITE_SIZE in either dimension are discarded (auto-slice noise).
    static Sprite[] ExtractSideRow(string sheetPath)
    {
        if (string.IsNullOrEmpty(sheetPath)) return new Sprite[0];

        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToList();
        var valid   = sprites.Where(s => s.rect.width >= MIN_SPRITE_SIZE && s.rect.height >= MIN_SPRITE_SIZE).ToList();
        if (valid.Count == 0) return new Sprite[0];

        // Group into rows by Y coordinate with tolerance for slightly misaligned slices
        var rows = new List<List<Sprite>>();
        foreach (var s in valid.OrderByDescending(s => s.rect.y))
        {
            var row = rows.FirstOrDefault(r => Mathf.Abs(r[0].rect.y - s.rect.y) < ROW_Y_TOLERANCE);
            if (row == null) { row = new List<Sprite>(); rows.Add(row); }
            row.Add(s);
        }

        int idx = Mathf.Clamp(SIDE_ROW_INDEX, 0, rows.Count - 1);
        return rows[idx].OrderBy(s => s.rect.x).ToArray();
    }

    static Sprite[] LoadFramesFromFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) return new Sprite[0];
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        var sprites = new List<Sprite>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sp = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sp != null) sprites.Add(sp);
        }
        return sprites.OrderBy(s => s.texture.name).ToArray();
    }

    static void SetSpriteArray(SerializedObject so, string propName, Sprite[] sprites)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) return;
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
