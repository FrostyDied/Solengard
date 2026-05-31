using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        // Ensure AssetDatabase index is up-to-date before sprite queries
        AssetDatabase.Refresh();

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
            string group    = lvl <= 3 ? "Level1-3" : lvl <= 6 ? "Level4-6" : "Level7-9";
            string charRoot = $"{Art}/Characters/Hero/{group}/PNG/Swordsman_lvl{lvl}";

            var go = new GameObject($"Player_Level{lvl}");
            go.AddComponent<SpriteRenderer>().sprite = FindCharacterSprite(charRoot);
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerHealth>();
            go.AddComponent<PlayerAttack>();
            go.AddComponent<PlayerWeapon>();
            go.AddComponent<PassiveItemSystem>();
            go.AddComponent<WeaponEvolutionSystem>();

            // PlayerController has [RequireComponent(Rigidbody2D)] — Unity adds one automatically.
            // AddComponent would return null for a duplicate, so always get-or-add.
            var rb = go.GetComponent<Rigidbody2D>() ?? go.AddComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError($"[SolengardPrefabSetup] Rigidbody2D null on Player_Level{lvl} — skipping rb config");
            }
            else
            {
                rb.gravityScale           = 0f;
                rb.freezeRotation         = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
            }

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
        CharEnemy3("EnemySlime",    $"{Art}/Characters/Enemies/Slime/PNG/Slime{{n}}",                     typeof(EnemySlime));
        CharEnemy3("EnemyZumbi",    $"{Art}/Characters/Enemies/Zombie/Premium/PNG/Zombie{{n}}",           typeof(EnemyZumbi));
        CharEnemy3("EnemyOrc",      $"{Art}/Characters/Enemies/Gnoll/PNG/Gnoll{{n}}",                     typeof(EnemyOrc));
        CharEnemy3("EnemyArcher",   $"{Art}/Characters/Enemies/Skeleton/Premium/PNG/Skeleton{{n}}",       typeof(EnemyArcher));
        CharEnemy3("EnemyAssassin", $"{Art}/Characters/Enemies/Ghost/PNG/Ghost{{n}}",                     typeof(EnemyAssassin));
        CharEnemy3("EnemyMage",     $"{Art}/Characters/Enemies/Demon/PNG/Demon{{n}}",                     typeof(EnemyMage));
        CharEnemy3("EnemyGolem",    $"{Art}/Characters/Enemies/Golem/PNG/Golem{{n}}",                     typeof(EnemyGolem));
        CharEnemy3("EnemyBoss",     $"{Art}/Characters/Enemies/Lich/PNG/Lich{{n}}",                       typeof(EnemyBoss));
        CharEnemy3("EnemyGoblin",   $"{Art}/Characters/Enemies/Goblin/PNG/Goblin{{n}}",                   typeof(EnemyZumbi));
        CharEnemy3("EnemyDarkElf",  $"{Art}/Characters/Enemies/DarkElf/Elf_{{n}}",                        typeof(EnemyAssassin));
        CharEnemy3("EnemyOrcHeavy", $"{Art}/Characters/Enemies/Orc/PNG/Orc{{n}}",                         typeof(EnemyOrc));

        Enemy("BossCaveman",      $"{Art}/Characters/Enemies/Boss/Caveman Boss/PNG/PNG Sequences/Front - Idle",  typeof(EnemyBoss));
        Enemy("BossGiantGoblin",  $"{Art}/Characters/Enemies/Boss/Giant Goblin/PNG/PNG Sequences/Front - Idle",  typeof(EnemyBoss));
        Enemy("BossVikingLeader", $"{Art}/Characters/Enemies/Boss/Viking Leader/PNG/PNG Sequences/Front - Idle", typeof(EnemyBoss));
    }

    private static void Enemy3(string baseName, string folderTemplate, Type component)
    {
        for (int i = 1; i <= 3 && !cancelled; i++)
            Enemy(i == 1 ? baseName : $"{baseName}{i}", folderTemplate.Replace("{n}", i.ToString()), component);
    }

    private static void CharEnemy3(string baseName, string charRootTemplate, Type component)
    {
        for (int i = 1; i <= 3 && !cancelled; i++)
        {
            string name     = i == 1 ? baseName : $"{baseName}{i}";
            string charRoot = charRootTemplate.Replace("{n}", i.ToString());
            CharEnemy(name, charRoot, component);
        }
    }

    private static void CharEnemy(string prefabName, string charRoot, Type component)
    {
        if (cancelled) return;

        var go = new GameObject(prefabName);
        go.AddComponent<SpriteRenderer>().sprite = FindCharacterSprite(charRoot);
        go.AddComponent(component);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 0;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        go.AddComponent<CircleCollider2D>();
        SetLayer(go, "Enemy");

        if (Save(go, $"{Prefabs}/Enemies/{prefabName}.prefab")) enemyCount++;
        UnityEngine.Object.DestroyImmediate(go);
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
        EffectKw("ExplosionEffect", $"{Art}/Effects/Explosions/PNG/Explosion",  new[]{"explosion"});
        EffectKw("LightningEffect", $"{Art}/Effects/Explosions/PNG/Lightning",  new[]{"lightning"});
        EffectKw("MagicEffect",
            FindSpriteByKeywords($"{Art}/Effects/Magic/1 Magic",                      new[]{"magic","effect"},             skipDigits: true)
         ?? FindSpriteByKeywords($"{Art}/Effects/Explosions/PNG/Explosion_blue_circle", new[]{"explosion","blue","circle"}));
        EffectKw("HitEffect",       $"{Art}/Effects/TopDown/PNG/Explosion1",    new[]{"explosion","hit","impact"});
        EffectKw("FireEffect",      $"{Art}/Effects/TopDown/PNG/Fire_small",    new[]{"fire"});
        EffectKw("SmokeEffect",     $"{Art}/Effects/Explosions/PNG/Smoke",      new[]{"smoke"});

        if (cancelled) return;

        var go = new GameObject("EnemyProjectile");
        go.AddComponent<SpriteRenderer>().sprite =
            FindSpriteByKeywords($"{Art}/Effects/Magic/1 Magic",                      new[]{"magic","orb","ball","projectile"}, skipDigits: true)
         ?? FindSpriteByKeywords($"{Art}/Effects/Explosions/PNG/Explosion_blue_circle", new[]{"explosion","blue","circle"});

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        go.AddComponent<EnemyProjectile>();
        go.tag = "EnemyProjectile";

        if (Save(go, $"{Prefabs}/Effects/EnemyProjectile.prefab")) effectCount++;
        UnityEngine.Object.DestroyImmediate(go);
    }

    private static void EffectKw(string prefabName, string folder, string[] keywords, bool skipDigits = false) =>
        EffectKw(prefabName, FindSpriteByKeywords(folder, keywords, skipDigits: skipDigits));

    private static void EffectKw(string prefabName, Sprite sprite)
    {
        if (cancelled) return;
        var go = new GameObject(prefabName);
        go.AddComponent<SpriteRenderer>().sprite = sprite;
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
        bool isFloor = prefabName.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0;
        go.AddComponent<SpriteRenderer>().sprite = isFloor ? FilteredSprite(folder) : FindSprite(folder);
        if (box)    go.AddComponent<BoxCollider2D>();
        if (circle) go.AddComponent<CircleCollider2D>();
        SetLayer(go, layer);
        if (Save(go, $"{Prefabs}/Environment/{season}/{prefabName}.prefab")) envCount++;
        UnityEngine.Object.DestroyImmediate(go);
    }

    // ── UTILITIES ────────────────────────────────────────────────────────────

    private static readonly string[] FloorPreferred = {"floor","ground","tile","stone","dirt","grass","path","base"};
    private static readonly string[] FloorExcluded  = {"animation","animated","crack","arch","column","bridge","bubble","source","detail","object","prop","trap","decor"};

    private static Sprite FilteredSprite(string folder) =>
        FindSpriteByKeywords(folder, FloorPreferred, FloorExcluded);

    private static Sprite FindSprite(string folder)
    {
        string[] guids = GetGuids(folder);
        if (guids == null) return null;

        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Sprite s = LoadSprite(p);
                if (s != null) { Debug.Log($"[SolengardPrefabSetup] Sprite atribuído: {p}"); return s; }
            }
        }

        string first = AssetDatabase.GUIDToAssetPath(guids[0]);
        Sprite fallback = LoadSprite(first);
        if (fallback != null) Debug.Log($"[SolengardPrefabSetup] Sprite atribuído (fallback): {first}");
        return fallback;
    }

    private static Sprite FindSpriteByKeywords(string folder, string[] preferred, string[] excluded = null, bool skipDigits = false)
    {
        string[] guids = GetGuids(folder);
        if (guids == null) return null;

        // Pass 1: preferred keywords in order
        foreach (string kw in preferred)
        {
            foreach (string guid in guids)
            {
                string p     = AssetDatabase.GUIDToAssetPath(guid);
                string fname = Path.GetFileNameWithoutExtension(p);
                if (fname.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Sprite s = LoadSprite(p);
                if (s != null) { Debug.Log($"[SolengardPrefabSetup] Sprite atribuído [{kw}]: {p}"); return s; }
            }
        }

        // Pass 2: first that passes exclusion + digit filter
        foreach (string guid in guids)
        {
            string p     = AssetDatabase.GUIDToAssetPath(guid);
            string fname = Path.GetFileNameWithoutExtension(p);
            if (skipDigits && IsOnlyDigits(fname)) continue;
            if (excluded != null && ContainsAnyKeyword(fname, excluded)) continue;
            Sprite s = LoadSprite(p);
            if (s != null) { Debug.Log($"[SolengardPrefabSetup] Sprite atribuído (filtered): {p}"); return s; }
        }

        // Pass 3: absolute fallback — skip digits if requested, return null to allow caller to try another folder
        foreach (string guid in guids)
        {
            string p     = AssetDatabase.GUIDToAssetPath(guid);
            string fname = Path.GetFileNameWithoutExtension(p);
            if (skipDigits && IsOnlyDigits(fname)) continue;
            Sprite s = LoadSprite(p);
            if (s != null) { Debug.Log($"[SolengardPrefabSetup] Sprite atribuído (fallback): {p}"); return s; }
        }

        if (skipDigits) return null; // all files were digits — let caller try fallback folder

        string firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        Sprite first = LoadSprite(firstPath);
        if (first != null) Debug.Log($"[SolengardPrefabSetup] Sprite atribuído (first): {firstPath}");
        return first;
    }

    private static Sprite FindCharacterSprite(string charRoot)
    {
        string partsFolder  = $"{charRoot}/Parts";
        string shadowFolder = $"{charRoot}/Without_shadow";

        if (AssetDatabase.IsValidFolder(partsFolder))
        {
            Sprite s = FindIdleBodySprite(partsFolder);
            if (s != null) return s;
        }

        if (AssetDatabase.IsValidFolder(shadowFolder))
        {
            Sprite s = FindIdleBodySprite(shadowFolder);
            if (s != null) return s;
        }

        // Last resort: anything found recursively under charRoot
        string[] fallback = AssetDatabase.FindAssets("t:Texture2D", new[] { charRoot });
        if (fallback.Length == 0) fallback = AssetDatabase.FindAssets("t:Sprite", new[] { charRoot });
        if (fallback.Length > 0)
        {
            Sprite s = LoadSprite(AssetDatabase.GUIDToAssetPath(fallback[0]));
            if (s != null) return s;
        }

        Debug.LogWarning($"[SolengardPrefabSetup] Nenhum sprite encontrado: {charRoot}");
        return null;
    }

    private static Sprite FindIdleBodySprite(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        if (guids.Length == 0) return null;

        // Pass 1: file with "idle" + "body"
        foreach (string guid in guids)
        {
            string p  = AssetDatabase.GUIDToAssetPath(guid);
            string fn = Path.GetFileNameWithoutExtension(p);
            if (fn.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0 &&
                fn.IndexOf("body", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Sprite s = LoadSprite(p);
                if (s != null) { Debug.Log($"[SolengardPrefabSetup] Sprite: {p}"); return s; }
            }
        }

        // Pass 2: any "idle" file
        foreach (string guid in guids)
        {
            string p  = AssetDatabase.GUIDToAssetPath(guid);
            string fn = Path.GetFileNameWithoutExtension(p);
            if (fn.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Sprite s = LoadSprite(p);
                if (s != null) { Debug.Log($"[SolengardPrefabSetup] Sprite: {p}"); return s; }
            }
        }

        // Pass 3: first available
        Sprite first = LoadSprite(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (first != null) Debug.Log($"[SolengardPrefabSetup] Sprite (fallback): {AssetDatabase.GUIDToAssetPath(guids[0])}");
        return first;
    }

    private static string[] GetGuids(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogWarning($"[SolengardPrefabSetup] Pasta não encontrada: {folder}");
            missingFolderCount++;
            return null;
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        if (guids.Length == 0)
            guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });

        return guids.Length > 0 ? guids : null;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;

        foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is Sprite sprite) return sprite;

        return null;
    }

    private static bool IsOnlyDigits(string name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        foreach (char c in name)
            if (!char.IsDigit(c)) return false;
        return true;
    }

    private static bool ContainsAnyKeyword(string name, string[] keywords)
    {
        foreach (string kw in keywords)
            if (name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
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

        EditorUtility.SetDirty(go);
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

    // ── AUDIT ─────────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Audit Prefabs")]
    public static void AuditPrefabs()
    {
        const string outputPath = "Assets/PrefabAudit.txt";

        var sections = new (string label, string folder)[]
        {
            ("PERSONAGENS", $"{Prefabs}/Characters"),
            ("INIMIGOS",    $"{Prefabs}/Enemies"),
            ("EFEITOS",     $"{Prefabs}/Effects"),
            ("AMBIENTE",    $"{Prefabs}/Environment"),
        };

        var sb          = new StringBuilder();
        var missing     = new List<string>();
        int total       = 0;
        int withSprite  = 0;

        sb.AppendLine("=== AUDIT DE PREFABS - Solengard ===");
        sb.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        foreach (var (label, folder) in sections)
        {
            sb.AppendLine();
            sb.AppendLine($"{label} ({folder}/):");

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            if (guids.Length == 0)
            {
                sb.AppendLine("  (nenhum prefab encontrado)");
                continue;
            }

            // Sort by path so output is deterministic
            var paths = new List<string>(guids.Length);
            foreach (string guid in guids)
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (string path in paths)
            {
                string prefabName = Path.GetFileName(path);
                var    prefab     = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string spriteName = "[VAZIO - sem sprite!]";

                if (prefab != null)
                {
                    var sr = prefab.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                    {
                        spriteName = sr.sprite.name;
                        withSprite++;
                    }
                    else
                    {
                        missing.Add(prefabName);
                    }
                }
                else
                {
                    missing.Add(prefabName);
                }

                // Indent Environment entries one extra level (sub-seasons)
                string indent = folder.EndsWith("Environment") ? "  " : "";
                sb.AppendLine($"{indent}- {prefabName} → {spriteName}");
                total++;
            }
        }

        int withoutSprite = total - withSprite;

        sb.AppendLine();
        sb.AppendLine("RESUMO:");
        sb.AppendLine($"- Total prefabs: {total}");
        sb.AppendLine($"- Com sprite: {withSprite}");
        sb.AppendLine($"- SEM sprite: {withoutSprite}");

        if (missing.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Prefabs SEM sprite:");
            foreach (string m in missing)
                sb.AppendLine($"  * {m}");
        }

        string report = sb.ToString();

        // Write file
        string fullPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), outputPath);
        File.WriteAllText(fullPath, report, Encoding.UTF8);

        // Log to Console
        Debug.Log($"[SolengardPrefabSetup] Audit concluído — {total} prefabs | {withSprite} com sprite | {withoutSprite} sem sprite\n\n{report}");

        // Refresh and open in editor
        AssetDatabase.Refresh();
        var txt = AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath);
        if (txt != null) AssetDatabase.OpenAsset(txt);
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
