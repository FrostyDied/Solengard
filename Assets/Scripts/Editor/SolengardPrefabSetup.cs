using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class SolengardPrefabSetup
{
    private const string Art    = "Assets/Art";
    private const string Prefabs = "Assets/Prefabs";

    private static int heroCount, enemyCount, effectCount, envCount, skippedCount, missingFolderCount;
    private static bool cancelled;
    private static int currentOp;
    private const  int TotalOps = 72; // 9 heroes + 36 enemies + 7 effects + 20 env

    [MenuItem("Solengard/Setup All Prefabs")]
    public static void SetupAllPrefabs()
    {
        heroCount = enemyCount = effectCount = envCount = skippedCount = missingFolderCount = 0;
        cancelled = false;
        currentOp = 0;

        EnsureTag("Player");
        EnsureTag("Effect");
        EnsureTag("EnemyProjectile");
        EnsureLayer("Player");
        EnsureLayer("Enemy");
        EnsureLayer("Ground");
        EnsureLayer("Obstacle");
        EnsureLayer("Effect");

        EnsureFolder($"{Prefabs}/Characters");
        EnsureFolder($"{Prefabs}/Enemies");
        EnsureFolder($"{Prefabs}/Effects");
        for (int s = 1; s <= 7; s++)
            EnsureFolder($"{Prefabs}/Environment/Season{s}");

        BuildHeroes();
        if (!cancelled) BuildEnemies();
        if (!cancelled) BuildEffects();
        if (!cancelled) BuildEnvironment();

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int total = heroCount + enemyCount + effectCount + envCount;
        EditorUtility.DisplayDialog(
            "Solengard — Setup All Prefabs",
            $"Herói: {heroCount}  |  Inimigos: {enemyCount}  |  Efeitos: {effectCount}  |  Ambiente: {envCount}  |  Total: {total} prefabs criados\n" +
            $"Prefabs já existentes pulados: {skippedCount}\n" +
            $"Pastas não encontradas: {missingFolderCount}",
            "OK");
    }

    // ── SECTION 1: HEROES ────────────────────────────────────────────────────

    private static void BuildHeroes()
    {
        for (int lvl = 1; lvl <= 9 && !cancelled; lvl++)
        {
            string group  = lvl <= 3 ? "Level1-3" : lvl <= 6 ? "Level4-6" : "Level7-9";
            string folder = $"{Art}/Characters/Hero/{group}/PNG/Swordsman_lvl{lvl}/Without_shadow";

            var go = new GameObject($"Player_Level{lvl}");
            go.AddComponent<SpriteRenderer>().sprite = FindSprite(folder);
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerHealth>();
            go.AddComponent<PlayerAttack>();
            go.AddComponent<PlayerWeapon>();
            go.AddComponent<PassiveItemSystem>();
            go.AddComponent<WeaponEvolutionSystem>();

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale  = 0;
            rb.constraints   = RigidbodyConstraints2D.FreezeRotation;

            go.AddComponent<BoxCollider2D>();
            go.tag = "Player";
            SetLayer(go, "Player");

            if (Save(go, $"{Prefabs}/Characters/Player_Level{lvl}.prefab")) heroCount++;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ── SECTION 2: ENEMIES ───────────────────────────────────────────────────

    private static void BuildEnemies()
    {
        Enemy3("EnemySlime",      $"{Art}/Characters/Enemies/Slime/PNG/Slime{{n}}/Without_shadow",                  typeof(EnemySlime));
        Enemy3("EnemyZumbi",      $"{Art}/Characters/Enemies/Zombie/Premium/PNG/Zombie{{n}}/Without_shadow",        typeof(EnemyZumbi));
        Enemy3("EnemyOrc",        $"{Art}/Characters/Enemies/Gnoll/PNG/Gnoll{{n}}/Without_shadow",                  typeof(EnemyOrc));
        Enemy3("EnemyArcher",     $"{Art}/Characters/Enemies/Skeleton/Premium/PNG/Skeleton{{n}}/Without_shadow",    typeof(EnemyArcher));
        Enemy3("EnemyAssassin",   $"{Art}/Characters/Enemies/Ghost/PNG/Ghost{{n}}/Without_shadow",                  typeof(EnemyAssassin));
        Enemy3("EnemyMage",       $"{Art}/Characters/Enemies/Demon/PNG/Demon{{n}}/Without_shadow",                  typeof(EnemyMage));
        Enemy3("EnemyGolem",      $"{Art}/Characters/Enemies/Golem/PNG/Golem{{n}}/Without_shadow",                  typeof(EnemyGolem));
        Enemy3("EnemyBoss",       $"{Art}/Characters/Enemies/Lich/PNG/Lich{{n}}/Without_shadow",                    typeof(EnemyBoss));
        Enemy3("EnemyGoblin",     $"{Art}/Characters/Enemies/Goblin/PNG/Goblin{{n}}/Without_shadow",                typeof(EnemyZumbi));
        Enemy3("EnemyDarkElf",    $"{Art}/Characters/Enemies/DarkElf/Elf_{{n}}",                                    typeof(EnemyAssassin));
        Enemy3("EnemyOrcHeavy",   $"{Art}/Characters/Enemies/Orc/PNG/Orc{{n}}/Without_shadow",                     typeof(EnemyOrc));

        Enemy("BossCaveman",      $"{Art}/Characters/Enemies/Boss/Caveman Boss/PNG/PNG Sequences/Front - Idle",     typeof(EnemyBoss));
        Enemy("BossGiantGoblin",  $"{Art}/Characters/Enemies/Boss/Giant Goblin/PNG/PNG Sequences/Front - Idle",     typeof(EnemyBoss));
        Enemy("BossVikingLeader", $"{Art}/Characters/Enemies/Boss/Viking Leader/PNG/PNG Sequences/Front - Idle",    typeof(EnemyBoss));
    }

    private static void Enemy3(string baseName, string folderTemplate, Type component)
    {
        for (int i = 1; i <= 3 && !cancelled; i++)
            Enemy(i == 1 ? baseName : $"{baseName}{i}", folderTemplate.Replace("{n}", i.ToString()), component);
    }

    private static void Enemy(string prefabName, string folder, Type component)
    {
        if (cancelled) return;

        var go = new GameObject(prefabName);
        go.AddComponent<SpriteRenderer>().sprite = FindSprite(folder);
        go.AddComponent(component);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale          = 0;
        rb.constraints           = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<CircleCollider2D>();
        SetLayer(go, "Enemy");

        if (Save(go, $"{Prefabs}/Enemies/{prefabName}.prefab")) enemyCount++;
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ── SECTION 3: EFFECTS ───────────────────────────────────────────────────

    private static void BuildEffects()
    {
        Effect("ExplosionEffect", $"{Art}/Effects/Explosions/PNG/Explosion");
        Effect("LightningEffect", $"{Art}/Effects/Explosions/PNG/Lightning");
        Effect("MagicEffect",     $"{Art}/Effects/Magic/1 Magic/1");
        Effect("HitEffect",       $"{Art}/Effects/TopDown/PNG/Explosion1");
        Effect("FireEffect",      $"{Art}/Effects/TopDown/PNG/Fire_small");
        Effect("SmokeEffect",     $"{Art}/Effects/Explosions/PNG/Smoke");

        if (cancelled) return;

        var go = new GameObject("EnemyProjectile");
        go.AddComponent<SpriteRenderer>().sprite = FindSprite($"{Art}/Effects/Magic/1 Magic/1");

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        go.AddComponent<EnemyProjectile>();
        go.tag = "EnemyProjectile";

        if (Save(go, $"{Prefabs}/Effects/EnemyProjectile.prefab")) effectCount++;
        UnityEngine.Object.DestroyImmediate(go);
    }

    private static void Effect(string prefabName, string folder)
    {
        if (cancelled) return;
        var go = new GameObject(prefabName);
        go.AddComponent<SpriteRenderer>().sprite = FindSprite(folder);
        go.tag = "Effect";
        if (Save(go, $"{Prefabs}/Effects/{prefabName}.prefab")) effectCount++;
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ── SECTION 4: ENVIRONMENT ───────────────────────────────────────────────

    private static void BuildEnvironment()
    {
        // Season 1 - Dungeon
        Env("Season1", "DungeonFloor",     $"{Art}/Environment/Season1_Dungeon/Tileset/PNG",                       "Ground",   false, false);
        Env("Season1", "DungeonFloorFree", $"{Art}/Environment/Season1_Dungeon/Tileset_Free/PNG",                  "Ground",   false, false);
        Env("Season1", "DungeonObject",    $"{Art}/Environment/Season1_Dungeon/Objects/DungeonObjects/PNG",         "Obstacle", true,  false);
        Env("Season1", "DungeonProp",      $"{Art}/Environment/Season1_Dungeon/Objects/DungeonProps/PNG",           "Default",  false, false);

        // Season 2 - Forest
        Env("Season2", "ForestFloor",      $"{Art}/Environment/Season2_Forest/Tileset/PNG",                        "Ground",   false, false);
        Env("Season2", "ForestTree",       $"{Art}/Environment/Season2_Forest/Trees/PNG/Assets_separately",        "Obstacle", true,  false);
        Env("Season2", "ForestBush",       $"{Art}/Environment/Season2_Forest/Bushes/PNG/Assets",                  "Obstacle", false, true);
        Env("Season2", "ForestObject",     $"{Art}/Environment/Season2_Forest/Objects/PNG/Assets",                 "Default",  false, false);

        // Season 3 - Grassland
        Env("Season3", "GrasslandFloor",   $"{Art}/Environment/Season3_Grassland/Tileset/PNG",                     "Ground",   false, false);
        Env("Season3", "GrasslandRock",    $"{Art}/Environment/Season3_Grassland/Rocks/PNG/Objects_separately",    "Obstacle", false, true);
        Env("Season3", "GrasslandRuin",    $"{Art}/Environment/Season3_Grassland/Ruins/PNG/Assets",                "Obstacle", true,  false);

        // Season 4 - Swamp
        Env("Season4", "SwampFloor",       $"{Art}/Environment/Season4_Swamp/Tileset/PNG",                         "Ground",   false, false);
        Env("Season4", "SwampObject",      $"{Art}/Environment/Season4_Swamp/Objects/PNG/Assets",                  "Default",  false, false);

        // Season 5 - Cave
        Env("Season5", "CaveFloor",        $"{Art}/Environment/Season5_Cave/Tileset/PNG",                          "Ground",   false, false);
        Env("Season5", "CaveCrystal",      $"{Art}/Environment/Season5_Cave/Crystals/PNG/Assets",                  "Obstacle", false, true);
        Env("Season5", "CaveObject",       $"{Art}/Environment/Season5_Cave/Objects/PNG/Objects_separately",        "Default",  false, false);

        // Season 6 - Undead
        Env("Season6", "UndeadFloor",      $"{Art}/Environment/Season6_Undead/Tileset/PNG",                        "Ground",   false, false);
        Env("Season6", "UndeadObject",     $"{Art}/Environment/Season6_Undead/Objects/PNG/Objects_separately",     "Default",  false, false);

        // Season 7 - Cursed
        Env("Season7", "CursedFloor",      $"{Art}/Environment/Season7_Cursed/Tileset/PNG",                        "Ground",   false, false);
        Env("Season7", "CursedObject",     $"{Art}/Environment/Season7_Cursed/Objects/PNG/Objects_separately",     "Default",  false, false);
    }

    private static void Env(string season, string prefabName, string folder, string layer, bool box, bool circle)
    {
        if (cancelled) return;
        var go = new GameObject(prefabName);
        go.AddComponent<SpriteRenderer>().sprite = FindSprite(folder);
        if (box)    go.AddComponent<BoxCollider2D>();
        if (circle) go.AddComponent<CircleCollider2D>();
        SetLayer(go, layer);
        if (Save(go, $"{Prefabs}/Environment/{season}/{prefabName}.prefab")) envCount++;
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ── UTILITIES ────────────────────────────────────────────────────────────

    private static Sprite FindSprite(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[SolengardPrefabSetup] Pasta não encontrada: {folder}");
            missingFolderCount++;
            return null;
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        if (guids.Length == 0) return null;

        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0)
                return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static bool Save(GameObject go, string path)
    {
        currentOp++;
        string name = Path.GetFileNameWithoutExtension(path);
        cancelled = EditorUtility.DisplayCancelableProgressBar(
            "Solengard — Setup All Prefabs",
            $"Criando: {name}",
            (float)currentOp / TotalOps);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            skippedCount++;
            return false;
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        return true;
    }

    private static void SetLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0) go.layer = layer;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent    = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
        string newFolder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, newFolder);
    }

    private static void EnsureLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) >= 0) return;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");

        for (int i = 6; i < layers.arraySize; i++)
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(el.stringValue)) continue;
            el.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[SolengardPrefabSetup] Layer criada: '{layerName}' (slot {i})");
            return;
        }

        Debug.LogWarning($"[SolengardPrefabSetup] Layer '{layerName}' não pôde ser criada — sem slots livres.");
    }

    private static void EnsureTag(string tagName)
    {
        foreach (string existing in InternalEditorUtility.tags)
            if (existing == tagName) return;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        int idx = tags.arraySize;
        tags.InsertArrayElementAtIndex(idx);
        tags.GetArrayElementAtIndex(idx).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[SolengardPrefabSetup] Tag criada: '{tagName}'");
    }
}
