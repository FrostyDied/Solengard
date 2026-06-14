using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu: Solengard ▸ Setup Scene / Setup Systems / Setup Pools and Upgrades / Setup All
//                 ▸ Create MainMenu Scene
// Atribui automaticamente referências e dados de gameplay a componentes da cena.
// "Create MainMenu Scene" gera Assets/Scenes/MainMenu.unity com hierarquia UI completa.
public static class SolengardSetup
{
    const string GAME_CONFIG_PATH        = "Assets/Data/GameConfig.asset";
    const string PLAYER_DATA_PATH        = "Assets/Data/PlayerData.asset";
    const string SLIME_PREFAB_PATH       = "Assets/Prefabs/Enemies/EnemySlime.prefab";
    const string HIT_EFFECT_PATH         = "Assets/Prefabs/Effects/HitEffect.prefab";
    const string EXPECTED_SCENE          = "GameScene";
    const string MAIN_MENU_SCENE_PATH    = "Assets/Scenes/MainMenu.unity";
    const string GAME_SCENE_PATH         = "Assets/Scenes/GameScene.unity";
    const string PLAYER_LEVEL1_PREFAB    = "Assets/Prefabs/Characters/Player_Level1.prefab";
    static readonly string[] ENEMY_PREFAB_PATHS =
    {
        "Assets/Prefabs/Enemies/EnemySlime.prefab",     // [0]
        "Assets/Prefabs/Enemies/EnemyZumbi.prefab",     // [1]
        "Assets/Prefabs/Enemies/EnemyArcher.prefab",    // [2]
        "Assets/Prefabs/Enemies/EnemyOrc.prefab",       // [3]
        "Assets/Prefabs/Enemies/EnemyMage.prefab",      // [4]
        "Assets/Prefabs/Enemies/EnemyAssassin.prefab",  // [5]
        "Assets/Prefabs/Enemies/EnemyGolem.prefab",     // [6]
        "Assets/Prefabs/Enemies/EnemyBoss.prefab",      // [7] reservado
    };

    // Inimigos e bosses por zona — usados por TryAddEnemyPrefabsToZoneManager
    static readonly string[][] ZONE_ENEMY_PATHS =
    {
        // Zona 0 — Veremoth (Floresta)
        new[] {
            "Assets/Prefabs/Enemies/EnemySlime.prefab",
            "Assets/Prefabs/Enemies/EnemySlime2.prefab",
            "Assets/Prefabs/Enemies/EnemyZumbi.prefab",
            "Assets/Prefabs/Enemies/EnemyZumbi2.prefab",
            "Assets/Prefabs/Enemies/EnemyGoblin.prefab",
        },
        // Zona 1 — Khorduum (Caverna)
        new[] {
            "Assets/Prefabs/Enemies/EnemySlime3.prefab",
            "Assets/Prefabs/Enemies/EnemyZumbi3.prefab",
            "Assets/Prefabs/Enemies/EnemyOrcHeavy.prefab",
            "Assets/Prefabs/Enemies/EnemyOrcHeavy2.prefab",
            "Assets/Prefabs/Enemies/EnemyGoblin2.prefab",
        },
        // Zona 2 — Valdross (Cemitério)
        new[] {
            "Assets/Prefabs/Enemies/EnemyArcher.prefab",
            "Assets/Prefabs/Enemies/EnemyArcher2.prefab",
            "Assets/Prefabs/Enemies/EnemyMage.prefab",
            "Assets/Prefabs/Enemies/EnemyMage2.prefab",
            "Assets/Prefabs/Enemies/EnemyGoblin3.prefab",
        },
        // Zona 3 — Gorveth (Pântano)
        new[] {
            "Assets/Prefabs/Enemies/EnemyArcher3.prefab",
            "Assets/Prefabs/Enemies/EnemyOrc.prefab",
            "Assets/Prefabs/Enemies/EnemyOrc2.prefab",
            "Assets/Prefabs/Enemies/EnemyOrcHeavy3.prefab",
            "Assets/Prefabs/Enemies/EnemyMage3.prefab",
        },
        // Zona 4 — Arkenfall (Campo de Batalha)
        new[] {
            "Assets/Prefabs/Enemies/EnemyOrc3.prefab",
            "Assets/Prefabs/Enemies/EnemyAssassin.prefab",
            "Assets/Prefabs/Enemies/EnemyAssassin2.prefab",
            "Assets/Prefabs/Enemies/EnemyAssassin3.prefab",
            "Assets/Prefabs/Enemies/EnemyDarkElf2.prefab",
        },
    };

    static readonly string[][] ZONE_BOSS_PATHS =
    {
        // Zona 0 — Veremoth
        new[] { "Assets/Prefabs/Enemies/EnemyGolem.prefab" },
        // Zona 1 — Khorduum
        new[] { "Assets/Prefabs/Enemies/BossCaveman.prefab" },
        // Zona 2 — Valdross
        new[] { "Assets/Prefabs/Enemies/BossGiantGoblin.prefab" },
        // Zona 3 — Gorveth (DarkElf amplificado ×8 HP, ×2.5 scale)
        new[] { "Assets/Prefabs/Enemies/EnemyDarkElf.prefab" },
        // Zona 4 — Arkenfall (3 bosses simultâneos)
        new[] {
            "Assets/Prefabs/Enemies/BossCaveman.prefab",
            "Assets/Prefabs/Enemies/BossGiantGoblin.prefab",
            "Assets/Prefabs/Enemies/BossVikingLeader.prefab",
        },
    };

    // ── Setup Scene ──────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Setup Scene")]
    static void SetupScene()
    {
        if (!ValidateScene(out var scene)) return;
        if (!LoadAssets(out var gameConfig, out var playerData)) return;

        var log   = new StringBuilder();
        int total = RunSetupScene(gameConfig, playerData, log);

        ConfigurePhysicsMatrix();
        Physics2D.gravity         = Vector2.zero;
        QualitySettings.antiAliasing = 0;

        if (total > 0) EditorSceneManager.MarkSceneDirty(scene);

        var sb = new StringBuilder();
        AppendResult(sb, total, log);
        AppendManualPendencies(sb);
        EditorUtility.DisplayDialog("Solengard Setup Scene — Concluído", sb.ToString(), "OK");
    }

    [MenuItem("Solengard/Setup Scene", validate = true)]
    static bool ValidateSetupScene() =>
        !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().name);

    // ── Setup Systems ────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Setup Systems")]
    static void SetupSystems()
    {
        if (!ValidateScene(out var scene)) return;

        var log   = new StringBuilder();
        var warns = new StringBuilder();
        int total = RunSetupSystems(log, warns);

        if (total > 0) EditorSceneManager.MarkSceneDirty(scene);

        var sb = new StringBuilder();
        AppendResult(sb, total, log);
        if (warns.Length > 0) sb.AppendLine($"⚠ Avisos:\n{warns}");
        EditorUtility.DisplayDialog("Solengard Setup Systems — Concluído", sb.ToString(), "OK");
    }

    [MenuItem("Solengard/Setup Systems", validate = true)]
    static bool ValidateSetupSystems() =>
        !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().name);

    // ── Setup Pools and Upgrades ─────────────────────────────────────────────────

    [MenuItem("Solengard/Setup Pools and Upgrades")]
    static void SetupPoolsAndUpgrades()
    {
        if (!ValidateScene(out var scene)) return;

        var log   = new StringBuilder();
        int total = RunSetupPoolsAndUpgrades(log);

        if (total > 0) EditorSceneManager.MarkSceneDirty(scene);

        var sb = new StringBuilder();
        if (total > 0)
            sb.AppendLine($"✓ {total} configuração(ões) realizadas:\n{log}");
        else
            sb.AppendLine("Nenhuma configuração necessária.\nTodos os dados já estavam preenchidos.\n");
        EditorUtility.DisplayDialog("Solengard Setup Pools & Upgrades — Concluído", sb.ToString(), "OK");
    }

    [MenuItem("Solengard/Setup Pools and Upgrades", validate = true)]
    static bool ValidateSetupPoolsAndUpgrades() =>
        !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().name);

    // ── Setup All ────────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Setup All")]
    static void SetupAll()
    {
        if (!ValidateScene(out var scene)) return;
        if (!LoadAssets(out var gameConfig, out var playerData)) return;

        var log   = new StringBuilder();
        var warns = new StringBuilder();

        log.AppendLine("── Criar Sistemas Novos ─────────────");
        int t0 = RunCreateNewSystemObjects(log);

        log.AppendLine("\n── Setup Scene ──────────────────────");
        int t1 = RunSetupScene(gameConfig, playerData, log);

        log.AppendLine("\n── Setup Systems ────────────────────");
        int t2 = RunSetupSystems(log, warns);

        log.AppendLine("\n── Setup Pools & Upgrades ───────────");
        int t3 = RunSetupPoolsAndUpgrades(log);

        int total = t0 + t1 + t2 + t3;
        if (total > 0) EditorSceneManager.MarkSceneDirty(scene);

        var sb = new StringBuilder();
        if (total > 0)
            sb.AppendLine($"✓ {total} atribuição(ões) no total:\n{log}");
        else
            sb.AppendLine("Nenhuma atribuição necessária.\nTudo já estava configurado.\n");
        if (warns.Length > 0)
            sb.AppendLine($"⚠ Avisos:\n{warns}");
        AppendManualPendencies(sb);
        EditorUtility.DisplayDialog("Solengard Setup All — Concluído", sb.ToString(), "OK");
    }

    [MenuItem("Solengard/Setup All", validate = true)]
    static bool ValidateSetupAll() =>
        !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().name);

    // ── Rebuild GameScene ────────────────────────────────────────────────────────

    [MenuItem("Solengard/Rebuild GameScene")]
    static void RebuildGameScene()
    {
        // 1. Validate — must be GameScene
        if (!ValidateScene(out var scene)) return;
        if (!LoadAssets(out var gameConfig, out var playerData)) return;

        // 2. Confirm
        if (!EditorUtility.DisplayDialog("Rebuild GameScene",
            "Isso vai recriar todos os GameObjects da GameScene do zero.\n\n" +
            "A Main Camera será preservada. Continuar?",
            "Reconstruir", "Cancelar"))
            return;

        Physics2D.gravity = Vector2.zero;

        Undo.SetCurrentGroupName("Rebuild GameScene");
        int undoGroup = Undo.GetCurrentGroup();

        // 3. Collect all root GOs first, then destroy cleanly
        var rootSnapshot = scene.GetRootGameObjects(); // snapshot before any destruction

        GameObject mainCamGO = null;
        foreach (var go in rootSnapshot)
        {
            if (go.GetComponentInChildren<Camera>(true) != null)
            {
                mainCamGO = go;
                continue;
            }
            Object.DestroyImmediate(go);
        }

        // Destroy all children of Main Camera (e.g. misplaced systems parented to it)
        if (mainCamGO != null)
        {
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in mainCamGO.transform)
                children.Add(child.gameObject);
            foreach (var child in children)
                Object.DestroyImmediate(child);

            mainCamGO.name = "Main Camera";
            try { mainCamGO.tag = "MainCamera"; } catch { }
            if (mainCamGO.GetComponent<AudioListener>() == null)
                mainCamGO.AddComponent<AudioListener>();
            var cam = mainCamGO.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags      = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black; // preto para não ter flash antes do fade
            }
        }

        // 4. Create every system as a root GO (same order as RunCreateNewSystemObjects)
        CreateSceneSystem<ProductionSafeguard>   ("ProductionSafeguard");
        CreateSceneSystem<PlayerClassManager>    ("PlayerClassManager");
        CreateSceneSystem<GameManager>           ("GameManager");
        CreateSceneSystem<ZoneManager>           ("ZoneManager");
        CreateSceneSystem<ObjectPoolManager>     ("ObjectPoolManager");
        CreateSceneSystem<UpgradeSystem>         ("UpgradeSystem");
        CreateSceneSystem<DiamondSystem>         ("DiamondSystem");
        CreateSceneSystem<PermanentUpgradeSystem>("PermanentUpgradeSystem");
        CreateSceneSystem<DailyMissionSystem>    ("DailyMissionSystem");
        CreateSceneSystem<DailyRewardSystem>     ("DailyRewardSystem");
        CreateSceneSystem<ScoreSystem>           ("ScoreSystem");
        CreateSceneSystem<SeasonPassSystem>      ("SeasonPassSystem");
        CreateSceneSystem<AuthSystem>            ("AuthSystem");
        CreateSceneSystem<IAPSystem>             ("IAPSystem");
        CreateSceneSystem<LocalizationManager>   ("LocalizationManager");
        CreateSceneSystem<ProceduralArenaSystem> ("ProceduralArenaSystem");
        CreateSceneSystem<GameSceneBootstrap>    ("GameSceneBootstrap");
        CreateSceneSystem<WaveTimerSystem>          ("WaveTimerSystem");
        CreateSceneSystem<DifficultyAdaptiveSystem> ("DifficultyAdaptiveSystem");
        CreateSceneSystem<RunRewardSystem>          ("RunRewardSystem");
        CreateSceneSystem<DynamicDifficultySystem>  ("DynamicDifficultySystem");
        CreateSceneSystem<TemporaryPowerSystem>     ("TemporaryPowerSystem");
        CreateSceneSystem<SimpleArena>              ("SimpleArena");
        CreateSceneSystem<PropSpawner>              ("PropSpawner");
        CreateSceneSystem<WorldChunkManager>        ("WorldChunkManager");
        CreateSceneSystem<WaveBoostSystem>          ("WaveBoostSystem");

        // WorldChunkManager substitui PropSpawner — desativar para evitar duplicação
        { var old = Object.FindFirstObjectByType<PropSpawner>(); if (old != null) old.gameObject.SetActive(false); }
        CreateSceneSystem<AtmosphereController>     ("AtmosphereController");
        CreateSceneSystem<XPSystem>                 ("XPSystem");
        CreateSceneSystem<BiomeSystem>              ("BiomeSystem");
        CreateSceneSystem<VFXManager>               ("VFXManager");
        CreateSceneSystem<ProceduralSceneGenerator> ("ProceduralSceneGenerator");
        CreateSceneSystem<ProceduralFog>            ("ProceduralFog");
        CreateSceneSystem<ProceduralParticles>      ("ProceduralParticles");
        CreateSceneSystem<VignetteOverlay>          ("VignetteOverlay");

        // EventSystem — required for UI clicks; module depends on Input System setting
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Rebuild GameScene");
            SceneManager.MoveGameObjectToScene(esGO, scene);
            esGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esGO.AddComponent<StandaloneInputModule>();
#endif
        }

        CreateLevelUpUIInScene(scene);
        CreateLoreScreenUI(scene);
        CreateGameOverUI(scene);

        // 5. Player — destroy any lingering Player-tagged objects before creating a fresh one
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.tag == "Player")
                Object.DestroyImmediate(go);
        }

        AssetDatabase.ImportAsset(PLAYER_LEVEL1_PREFAB, ImportAssetOptions.ForceUpdate);
        var playerPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_LEVEL1_PREFAB);
        GameObject playerGO;

        if (playerPrefabAsset != null)
        {
            playerGO = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefabAsset, scene);
            playerGO.name = "Player";
            playerGO.transform.position   = Vector3.zero;
            playerGO.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            Undo.RegisterCreatedObjectUndo(playerGO, "Rebuild GameScene");
            Debug.Log("[SolengardSetup] Player instanciado de Player_Level1.prefab.");
        }
        else
        {
            Debug.LogWarning("[SolengardSetup] Player_Level1.prefab não encontrado — usando placeholder.");
            playerGO = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(playerGO, "Rebuild GameScene");
            SceneManager.MoveGameObjectToScene(playerGO, scene);
            playerGO.transform.parent   = null;
            playerGO.transform.position = Vector3.zero;

            var sr  = playerGO.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(32, 32);
            var px  = new Color[32 * 32];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0.2f, 0.55f, 1f);
            tex.SetPixels(px);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f, 32f);

            var col = playerGO.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            var rb   = playerGO.AddComponent<Rigidbody2D>();
            rb.gravityScale   = 0f;
            rb.freezeRotation = true;

            playerGO.AddComponent<PlayerController>();
            playerGO.AddComponent<PlayerHealth>();
            playerGO.AddComponent<PlayerAttack>();
            playerGO.AddComponent<PlayerWeapon>();
            playerGO.AddComponent<PassiveItemSystem>();
            playerGO.AddComponent<WeaponEvolutionSystem>();
        }

        try { playerGO.tag = "Player"; }
        catch { Debug.LogWarning("[SolengardSetup] Tag 'Player' não existe — adicione em Project Settings → Tags."); }

        // Destroy ALL CameraFollow instances in the scene before adding a single clean one.
        // Duplicate instances cause jitter as they fight over the singleton field.
        foreach (var existingCf in Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(existingCf);

        // Add exactly one CameraFollow to Main Camera
        var mainCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCam != null)
            mainCam.AddComponent<CameraFollow>();

        // Wire camera target to player and fix orthoSize
        var cf = mainCam != null ? mainCam.GetComponent<CameraFollow>() : null;
        if (cf != null)
        {
            if (playerGO != null) cf.SetTarget(playerGO.transform);
            var so = new SerializedObject(cf);
            so.FindProperty("orthoSize").floatValue = 12f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 6. Wire references — Setup Scene + Systems + Pools
        var log   = new StringBuilder();
        var warns = new StringBuilder();
        log.AppendLine("── Setup Scene ──────────────────────");
        RunSetupScene(gameConfig, playerData, log);
        log.AppendLine("\n── Setup Systems ────────────────────");
        RunSetupSystems(log, warns);
        log.AppendLine("\n── Setup Pools & Upgrades ───────────");
        RunSetupPoolsAndUpgrades(log);

        // Log de confirmação do ZoneManager após setup completo
        var zmCheck = Object.FindFirstObjectByType<ZoneManager>(FindObjectsInactive.Include);
        if (zmCheck != null)
        {
            var zmSO      = new SerializedObject(zmCheck);
            var zonesProp = zmSO.FindProperty("zones");
            if (zonesProp != null)
            {
                for (int z = 0; z < zonesProp.arraySize; z++)
                {
                    var zp = zonesProp.GetArrayElementAtIndex(z);
                    var ep = zp.FindPropertyRelative("enemyPrefabs");
                    var bp = zp.FindPropertyRelative("bossPrefabs");
                    Debug.Log($"[Setup] Zona {z + 1}: {ep?.arraySize ?? 0} inimigos, {bp?.arraySize ?? 0} boss(es)");
                }
            }
        }

        // 7. Collapse Undo + save scene
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();

        // 8. Summary dialog
        int rootCount = scene.GetRootGameObjects().Length;
        var sb = new StringBuilder();
        sb.AppendLine($"✓ GameScene recriada com {rootCount} GameObjects na raiz da cena.\n");
        if (warns.Length > 0) sb.AppendLine($"⚠ Avisos:\n{warns}");
        AppendManualPendencies(sb);
        EditorUtility.DisplayDialog("Solengard — GameScene Recriada", sb.ToString(), "OK");
    }

    [MenuItem("Solengard/Rebuild GameScene", validate = true)]
    static bool ValidateRebuildGameScene() =>
        EditorSceneManager.GetActiveScene().name == EXPECTED_SCENE;

    // ── Debug ────────────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Debug/Limpar Sessao Salva")]
    static void LimparSessao()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Debug] Sessao e PlayerPrefs limpos.");
        EditorUtility.DisplayDialog("Solengard Debug", "PlayerPrefs limpos.\nPróxima run começará do zero.", "OK");
    }

    // Reseta SO o limite diario de video (preserva diamantes/classes/upgrades).
    [MenuItem("Solengard/Debug/Resetar Limite de Video")]
    static void ResetarLimiteVideo()
    {
        PlayerPrefs.DeleteKey("ad_video_count");
        PlayerPrefs.DeleteKey("ad_video_last_utc");
        PlayerPrefs.Save();
        Debug.Log("[Debug] Limite de video resetado - botao volta a (0/3)");
    }

    // ── Setup VFX Resources ──────────────────────────────────────────────────────

    [MenuItem("Solengard/Setup VFX Resources")]
    static void SetupVFXResources()
    {
        const string src = "Assets/Hovl Studio/Magic effects pack/Prefabs/";
        const string dst = "Assets/Resources/VFX/";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/VFX"))
            AssetDatabase.CreateFolder("Assets/Resources", "VFX");

        var copies = new System.Collections.Generic.Dictionary<string, string>
        {
            { src + "AoE effects/AoE slash orange.prefab",       dst + "AoE slash orange.prefab" },
            { src + "Hits and explosions/Electro hit.prefab",     dst + "Electro hit.prefab" },
            { src + "Hits and explosions/Stones hit.prefab",      dst + "Stones hit.prefab" },
            { src + "Hits and explosions/Explosion.prefab",       dst + "Explosion.prefab" },
            { src + "Sparks/Sparks explode blue.prefab",          dst + "Sparks explode blue.prefab" },
            { src + "AoE effects/Smoke AOE explosion.prefab",     dst + "Smoke AOE explosion.prefab" },
            { src + "AoE effects/Red energy explosion.prefab",    dst + "Red energy explosion.prefab" },
            { src + "Magic circles/Healing circle.prefab",        dst + "Healing circle.prefab" },
            { src + "Character auras/Star aura.prefab",           dst + "Star aura.prefab" },
            { src + "Character auras/Buff.prefab",                dst + "Buff.prefab" },
        };

        int copied = 0;
        var missing = new System.Text.StringBuilder();
        foreach (var kv in copies)
        {
            if (!System.IO.File.Exists(kv.Key.Replace("/", "\\")))
            {
                missing.AppendLine($"  Não encontrado: {kv.Key}");
                continue;
            }
            if (!System.IO.File.Exists(kv.Value.Replace("/", "\\")))
            {
                AssetDatabase.CopyAsset(kv.Key, kv.Value);
                copied++;
            }
        }

        // Crystal effect blue lives under Environment/
        string crystalSrc = src + "Environment/Crystal effect blue.prefab";
        string crystalDst = dst + "Crystal effect blue.prefab";
        if (System.IO.File.Exists(crystalSrc.Replace("/", "\\")))
        {
            if (!System.IO.File.Exists(crystalDst.Replace("/", "\\")))
            { AssetDatabase.CopyAsset(crystalSrc, crystalDst); copied++; }
        }
        else missing.AppendLine($"  Não encontrado: {crystalSrc}");

        AssetDatabase.Refresh();

        var msg = $"✓ {copied} prefab(s) copiado(s) para Resources/VFX/.";
        if (missing.Length > 0) msg += $"\n\n⚠ Prefabs ausentes (verifique os nomes):\n{missing}";
        EditorUtility.DisplayDialog("Solengard — Setup VFX Resources", msg, "OK");
        Debug.Log($"[VFX] {copied} prefab(s) copiados para Resources/VFX/.");
    }

    // ── Create MainMenu Scene ────────────────────────────────────────────────────

    [MenuItem("Solengard/Create MainMenu Scene")]
    static void CreateMainMenuScene()
    {
        Debug.Log("[SolengardSetup] CreateMainMenuScene iniciado");
        try
        {
            CreateMainMenuSceneImpl();
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Erro", ex.Message + "\n\n" + ex.StackTrace, "OK");
            Debug.LogException(ex);
        }
    }

    static void CreateMainMenuSceneImpl()
    {
        // 1. Handle unsaved changes in the active scene before touching scene management
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.isDirty)
        {
            bool save = EditorUtility.DisplayDialog(
                "Cenas não salvas",
                $"A cena '{activeScene.name}' tem alterações não salvas.\n\nDeseja salvar antes de continuar?",
                "Salvar", "Continuar sem salvar");

            if (save)
            {
                // Untitled scenes have no path — ask the user to save manually
                if (string.IsNullOrEmpty(activeScene.path))
                {
                    EditorUtility.DisplayDialog("Solengard",
                        "A cena ativa não tem um caminho no disco (\"Untitled\").\n\n" +
                        "Salve-a manualmente via File → Save Scene antes de continuar.",
                        "OK");
                    return;
                }

                EditorSceneManager.SaveOpenScenes();
            }
        }

        // 2. Abort if scene file already exists — never overwrite
        string fullPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "..", MAIN_MENU_SCENE_PATH));
        if (System.IO.File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Solengard",
                $"Cena já existe:\n{MAIN_MENU_SCENE_PATH}\n\n" +
                "Exclua o arquivo para criar novamente.",
                "OK");
            return;
        }

        // Group everything into one Ctrl+Z step
        Undo.SetCurrentGroupName("Create MainMenu Scene");
        int undoGroup = Undo.GetCurrentGroup();

        // 3. New scene additive — preserva a cena atual no editor
        var mainMenuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(mainMenuScene);

        // 3. Main Camera — search only within mainMenuScene (scene is additive; FindFirstObjectByType
        //    would find cameras in other open scenes and incorrectly skip creation here)
        Camera existingCam = null;
        foreach (var root in mainMenuScene.GetRootGameObjects())
        {
            existingCam = root.GetComponentInChildren<Camera>(true);
            if (existingCam != null) break;
        }

        if (existingCam == null)
        {
            var cameraGO = NewGO("Main Camera");
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.04f, 0.12f);
            cameraGO.AddComponent<AudioListener>();
        }
        else
        {
            existingCam.gameObject.tag = "MainCamera";
            if (existingCam.GetComponent<AudioListener>() == null)
                existingCam.gameObject.AddComponent<AudioListener>();
        }

        // 4. Singletons — root-level GOs; DontDestroyOnLoad requires scene root (no parent)
        NewSingleton(null, "[S] DiamondSystem",        typeof(DiamondSystem));
        NewSingleton(null, "[S] PermanentUpgrades",   typeof(PermanentUpgradeSystem));
        NewSingleton(null, "[S] SeasonPassSystem",    typeof(SeasonPassSystem));
        NewSingleton(null, "[S] DailyRewardSystem",   typeof(DailyRewardSystem));
        NewSingleton(null, "[S] IAPSystem",           typeof(IAPSystem));
        NewSingleton(null, "[S] AuthSystem",          typeof(AuthSystem));
        NewSingleton(null, "[S] LocalizationManager", typeof(LocalizationManager));

        // 5. EventSystem — same scope issue: search only within mainMenuScene
        bool hasEventSystem = false;
        foreach (var root in mainMenuScene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<EventSystem>(true) != null) { hasEventSystem = true; break; }
        }
        if (!hasEventSystem)
        {
            var eventSystemGO = NewGO("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }

        // 6. Canvas — Screen Space Overlay, Scale With Screen Size, 1080×1920
        var canvasGO = NewGO("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1080f, 1920f);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 7. MainMenuManager lives on the Canvas root
        var mmm = canvasGO.AddComponent<MainMenuManager>();

        // 8. BG — full-screen colour placeholder
        var bgGO = NewUIChild(canvasGO.transform, "BG");
        bgGO.AddComponent<Image>().color = new Color(0.08f, 0.04f, 0.12f);
        StretchFull(RT(bgGO));

        // 9. PlayerInfoPanel — top bar with player stats
        var playerInfoGO = NewUIChild(canvasGO.transform, "PlayerInfoPanel");
        var pirt = RT(playerInfoGO);
        pirt.anchorMin       = new Vector2(0f, 1f);
        pirt.anchorMax       = new Vector2(1f, 1f);
        pirt.pivot           = new Vector2(0.5f, 1f);
        pirt.sizeDelta       = new Vector2(0f, 180f);
        pirt.anchoredPosition = Vector2.zero;

        var textoDiamantes  = NewTMP(playerInfoGO.transform, "TextoDiamantes",  "0 DIA",  26);
        var textoNivelPasse  = NewTMP(playerInfoGO.transform, "TextoNivelPasse", "Nível 1",      22);
        var textoStreakLogin = NewTMP(playerInfoGO.transform, "TextoStreakLogin", "Dia 1",        20);

        // 10. MainButtons — 6 navigation buttons (designer positions/sizes them)
        var mainButtonsGO = NewUIChild(canvasGO.transform, "MainButtons");
        var mbrt = RT(mainButtonsGO);
        mbrt.anchorMin = new Vector2(0.1f, 0.20f);
        mbrt.anchorMax = new Vector2(0.9f, 0.85f);
        mbrt.offsetMin = Vector2.zero;
        mbrt.offsetMax = Vector2.zero;

        var (_, botaoJogar)         = NewButton(mainButtonsGO.transform, "BotaoJogar",         "JOGAR");
        var (_, botaoLoja)          = NewButton(mainButtonsGO.transform, "BotaoLoja",          "LOJA");
        var (_, botaoPasse)         = NewButton(mainButtonsGO.transform, "BotaoPasse",         "PASSE");
        var (_, botaoMissoes)       = NewButton(mainButtonsGO.transform, "BotaoMissoes",       "MISSÕES");
        var (_, botaoRanking)       = NewButton(mainButtonsGO.transform, "BotaoRanking",       "RANKING");
        var (_, botaoConfiguracoes) = NewButton(mainButtonsGO.transform, "BotaoConfiguracoes", "CONFIGURAÇÕES");
        var (_, botaoOfertas)       = NewButton(mainButtonsGO.transform, "BotaoOfertas",       "OFERTAS");
        var (_, botaoBencaos)       = NewButton(mainButtonsGO.transform, "BotaoBencaos",       "BÊNÇÃOS");
        var (_, botaoBaus)          = NewButton(mainButtonsGO.transform, "BotaoBaus",          "BAÚS");

        // 11. Panels — full-screen, inactive by default
        var painelLoja          = NewPanel(canvasGO.transform, "PainelLoja");
        var painelPasse         = NewPanel(canvasGO.transform, "PainelPasse");
        var painelMissoes       = NewPanel(canvasGO.transform, "PainelMissoes");
        var painelRanking       = NewPanel(canvasGO.transform, "PainelRanking");
        var painelConfiguracoes = NewPanel(canvasGO.transform, "PainelConfiguracoes");
        var painelOfertas       = NewPanel(canvasGO.transform, "PainelOfertas");
        var painelBencaos       = NewPanel(canvasGO.transform, "PainelBencaos");
        var painelBaus          = NewPanel(canvasGO.transform, "PainelBaus");

        // 12. PopupRecompensa — centred popup, inactive
        var popupGO = NewUIChild(canvasGO.transform, "PopupRecompensa");
        var popupRT = RT(popupGO);
        popupRT.anchorMin        = new Vector2(0.1f, 0.32f);
        popupRT.anchorMax        = new Vector2(0.9f, 0.68f);
        popupRT.offsetMin        = Vector2.zero;
        popupRT.offsetMax        = Vector2.zero;
        popupGO.AddComponent<Image>().color = new Color(0.10f, 0.05f, 0.18f, 0.97f);

        var textoRecompensaDia       = NewTMP(popupGO.transform, "TextoRecompensaDia",       "Dia 1 de 7",    22);
        var textoRecompensaDiamantes = NewTMP(popupGO.transform, "TextoRecompensaDiamantes", "+10 diamantes", 20);
        var (_, botaoColetarRecompensa) = NewButton(popupGO.transform, "BotaoColetarRecompensa", "COLETAR");
        popupGO.SetActive(false);

        // 13. Wire all 25 MainMenuManager references via SerializedObject
        var so = new SerializedObject(mmm);
        WireRef(so, "botaoJogar",               botaoJogar);
        WireRef(so, "botaoLoja",                botaoLoja);
        WireRef(so, "botaoPasse",               botaoPasse);
        WireRef(so, "botaoMissoes",             botaoMissoes);
        WireRef(so, "botaoRanking",             botaoRanking);
        WireRef(so, "botaoConfiguracoes",       botaoConfiguracoes);
        WireRef(so, "botaoOfertas",             botaoOfertas);
        WireRef(so, "botaoBencaos",             botaoBencaos);
        WireRef(so, "botaoBaus",                botaoBaus);
        WireRef(so, "textoDiamantes",           textoDiamantes);
        WireRef(so, "textoNivelPasse",          textoNivelPasse);
        WireRef(so, "textoStreakLogin",         textoStreakLogin);
        WireRef(so, "popupRecompensa",          popupGO);
        WireRef(so, "textoRecompensaDia",       textoRecompensaDia);
        WireRef(so, "textoRecompensaDiamantes", textoRecompensaDiamantes);
        WireRef(so, "botaoColetarRecompensa",   botaoColetarRecompensa);
        WireRef(so, "painelLoja",               painelLoja);
        WireRef(so, "painelPasse",              painelPasse);
        WireRef(so, "painelMissoes",            painelMissoes);
        WireRef(so, "painelRanking",            painelRanking);
        WireRef(so, "painelConfiguracoes",      painelConfiguracoes);
        WireRef(so, "painelOfertas",            painelOfertas);
        WireRef(so, "painelBencaos",            painelBencaos);
        WireRef(so, "painelBaus",               painelBaus);
        so.ApplyModifiedProperties();

        // 14. Build Settings: MainMenu[0], GameScene[1]
        UpdateBuildSettings();

        // 15. Collapse undo, save scene, refresh database
        Undo.CollapseUndoOperations(undoGroup);

        // Close any other loaded instance of the same path before saving — Unity forbids
        // saving to a path that is already open in another scene slot.
        var existingSlot = EditorSceneManager.GetSceneByPath(MAIN_MENU_SCENE_PATH);
        if (existingSlot.isLoaded && existingSlot != mainMenuScene)
            EditorSceneManager.CloseScene(existingSlot, true);

        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(mainMenuScene, MAIN_MENU_SCENE_PATH);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Solengard — MainMenu Criada",
            "✓ Cena criada com sucesso!\n\n" +
            "Abra Assets/Scenes/MainMenu.unity para posicionamento visual e arte.\n\n" +
            "Build Settings atualizado:\n  [0] MainMenu\n  [1] GameScene",
            "OK");
    }

    // ── Core logic ───────────────────────────────────────────────────────────────

    static int RunSetupScene(GameConfig gameConfig, PlayerData playerData, StringBuilder log)
    {
        int total = 0;
        total += TryAssign<DiamondSystem>("playerData",   playerData,  log);
        total += TryAssign<ScoreSystem>("playerData",     playerData,  log);
        total += TryAssign<SeasonPassSystem>("playerData", playerData, log);

        total += TryAssignPrefabByPath<PlayerHealth>("hitEffectPrefab",    HIT_EFFECT_PATH, log);
        return total;
    }

    static int RunSetupSystems(StringBuilder log, StringBuilder warns)
    {
        int total = 0;

        // PassiveItemSystem ← player components
        total += TryAssignComponent<PassiveItemSystem, PlayerHealth>("playerHealth",         log);
        total += TryAssignComponent<PassiveItemSystem, PlayerAttack>("playerAttack",         log);
        total += TryAssignComponent<PassiveItemSystem, PlayerController>("playerController", log);

        // WeaponEvolutionSystem ← PassiveItemSystem
        total += TryAssignComponent<WeaponEvolutionSystem, PassiveItemSystem>("passiveItemSystem", log);

        // UpgradeSystem ← PlayerHealth + PassiveItemSystem
        total += TryAssignComponent<UpgradeSystem, PlayerHealth>("playerHealth",           log);
        total += TryAssignComponent<UpgradeSystem, PassiveItemSystem>("passiveItemSystem", log);

        // UpgradeUIManager ← UpgradeSystem (opcional — pode não existir na cena ainda)
        if (Object.FindFirstObjectByType<UpgradeUIManager>(FindObjectsInactive.Include) != null)
            total += TryAssignComponent<UpgradeUIManager, UpgradeSystem>("upgradeSystem", log);

        // PlayerAttack.enemyLayerMask
        total += TryAssignLayerMask<PlayerAttack>("enemyLayerMask", "Enemy", log, warns);

        // New systems — cross-component wiring
        total += TryAssignComponent<GameManager,           RunRewardSystem>          ("runRewardSystem",    log);
        total += TryAssignComponent<RunRewardSystem,       WaveTimerSystem>          ("waveTimerSystem",    log);

        // DifficultyAdaptiveSystem — presence check only (no reference to wire)
        if (Object.FindFirstObjectByType<DifficultyAdaptiveSystem>(FindObjectsInactive.Include) == null)
            warns.AppendLine("  DifficultyAdaptiveSystem não encontrado na cena. Execute 'Setup All' para criá-lo automaticamente.");

        // GameOverScreen — criado e conectado via Layout GameScene; apenas verificação de presença
        if (Object.FindFirstObjectByType<GameOverScreen>(FindObjectsInactive.Include) == null)
            warns.AppendLine("  GameOverScreen não encontrado na cena. Execute 'Layout GameScene' para criá-lo automaticamente.");

        // CameraFollow — garantir que só existe uma instância e que está na Main Camera
        {
            var cameras = Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cameras.Length > 1)
            {
                foreach (var cf in cameras)
                {
                    if (cf.gameObject.CompareTag("MainCamera")) continue;
                    Object.DestroyImmediate(cf);
                    log.AppendLine("  CameraFollow duplicado removido.");
                    total++;
                }
            }
            else if (cameras.Length == 0)
            {
                var mainCam = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCam != null)
                {
                    mainCam.AddComponent<CameraFollow>();
                    log.AppendLine("  CameraFollow adicionado à Main Camera.");
                    total++;
                }
                else
                {
                    warns.AppendLine("  CameraFollow ausente e Main Camera não encontrada.");
                }
            }
        }

        return total;
    }

    // Creates LoreScreen UI hierarchy:
    //   LoreScreenCanvas (Canvas + LoreScreenUI) — ALWAYS ACTIVE
    //     └── LorePanel (CanvasGroup) — inactive by default; activated by ShowLore
    //           ├── Background
    //           ├── Separador
    //           ├── NomeBioma
    //           ├── TextoLore
    //           └── Instrucao
    static void CreateLoreScreenUI(Scene scene)
    {
        // ── Canvas root — stays active so FindFirstObjectByType finds it ──────────
        var canvasGO = new GameObject("LoreScreenCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Rebuild GameScene");
        SceneManager.MoveGameObjectToScene(canvasGO, scene);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var loreUI = canvasGO.AddComponent<LoreScreenUI>();

        // ── LorePanel — 90% da tela, ativa/desativa o conteúdo ──────────────────
        var panelGO = new GameObject("LorePanel");
        Undo.RegisterCreatedObjectUndo(panelGO, "Rebuild GameScene");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.05f, 0.05f);
        panelRT.anchorMax = new Vector2(0.95f, 0.95f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        var cg = panelGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // ── Content — all parented to LorePanel ──────────────────────────────────

        // Background full-screen
        var bgGO = new GameObject("Background");
        Undo.RegisterCreatedObjectUndo(bgGO, "Rebuild GameScene");
        bgGO.transform.SetParent(panelGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.97f);

        // Separator — gold horizontal line
        var sepGO = new GameObject("Separador");
        Undo.RegisterCreatedObjectUndo(sepGO, "Rebuild GameScene");
        sepGO.transform.SetParent(panelGO.transform, false);
        var sepRT = sepGO.AddComponent<RectTransform>();
        sepRT.anchorMin        = new Vector2(0.5f, 0.5f);
        sepRT.anchorMax        = new Vector2(0.5f, 0.5f);
        sepRT.pivot            = new Vector2(0.5f, 0.5f);
        sepRT.sizeDelta        = new Vector2(600f, 3f);
        sepRT.anchoredPosition = new Vector2(0f, 130f);
        var sepImg = sepGO.AddComponent<Image>();
        sepImg.color = new Color(0.78f, 0.65f, 0.20f, 0f);

        // NomeBioma
        var nomeGO = new GameObject("NomeBioma");
        Undo.RegisterCreatedObjectUndo(nomeGO, "Rebuild GameScene");
        nomeGO.transform.SetParent(panelGO.transform, false);
        var nomeRT = nomeGO.AddComponent<RectTransform>();
        nomeRT.anchorMin        = new Vector2(0.5f, 0.5f);
        nomeRT.anchorMax        = new Vector2(0.5f, 0.5f);
        nomeRT.pivot            = new Vector2(0.5f, 0.5f);
        nomeRT.sizeDelta        = new Vector2(700f, 80f);
        nomeRT.anchoredPosition = new Vector2(0f, 180f);
        var nomeTMP = nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTMP.text             = "";
        nomeTMP.alignment        = TextAlignmentOptions.Center;
        nomeTMP.fontSize         = 48f;
        nomeTMP.fontStyle        = FontStyles.Bold;
        nomeTMP.color            = new Color(0.78f, 0.65f, 0.20f, 1f);
        nomeTMP.characterSpacing = 12f;

        // TextoLore
        var loreGO = new GameObject("TextoLore");
        Undo.RegisterCreatedObjectUndo(loreGO, "Rebuild GameScene");
        loreGO.transform.SetParent(panelGO.transform, false);
        var loreRT = loreGO.AddComponent<RectTransform>();
        loreRT.anchorMin        = new Vector2(0.5f, 0.5f);
        loreRT.anchorMax        = new Vector2(0.5f, 0.5f);
        loreRT.pivot            = new Vector2(0.5f, 0.5f);
        loreRT.sizeDelta        = new Vector2(680f, 320f);
        loreRT.anchoredPosition = new Vector2(0f, -80f);
        var loreTMP = loreGO.AddComponent<TextMeshProUGUI>();
        loreTMP.text        = "";
        loreTMP.alignment   = TextAlignmentOptions.Center;
        loreTMP.fontSize      = 32f;
        loreTMP.fontStyle     = FontStyles.Normal;
        loreTMP.color         = new Color(0.85f, 0.83f, 0.88f, 1f);
        loreTMP.lineSpacing   = 12f;
        loreTMP.overflowMode  = TMPro.TextOverflowModes.Truncate;

        // Instrucao
        var instrGO = new GameObject("Instrucao");
        Undo.RegisterCreatedObjectUndo(instrGO, "Rebuild GameScene");
        instrGO.transform.SetParent(panelGO.transform, false);
        var instrRT = instrGO.AddComponent<RectTransform>();
        instrRT.anchorMin        = new Vector2(0.5f, 0.5f);
        instrRT.anchorMax        = new Vector2(0.5f, 0.5f);
        instrRT.pivot            = new Vector2(0.5f, 0.5f);
        instrRT.sizeDelta        = new Vector2(400f, 50f);
        instrRT.anchoredPosition = new Vector2(0f, -240f);
        var instrTMP = instrGO.AddComponent<TextMeshProUGUI>();
        instrTMP.text      = "";
        instrTMP.alignment = TextAlignmentOptions.Center;
        instrTMP.fontSize  = 20f;
        instrTMP.color     = new Color(0.60f, 0.58f, 0.65f, 1f);

        // ── Wire fields via SerializedObject ──────────────────────────────────────
        var so = new UnityEditor.SerializedObject(loreUI);
        so.FindProperty("lorePanel").objectReferenceValue   = panelGO;
        so.FindProperty("canvasGroup").objectReferenceValue = cg;
        so.FindProperty("background").objectReferenceValue  = bgImg;
        so.FindProperty("nomeBioma").objectReferenceValue   = nomeTMP;
        so.FindProperty("textoLore").objectReferenceValue   = loreTMP;
        so.FindProperty("instrucao").objectReferenceValue   = instrTMP;
        so.FindProperty("separador").objectReferenceValue   = sepImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Ordem de sibling garante nomeBioma na frente do textoLore
        bgGO.transform.SetSiblingIndex(0);
        sepGO.transform.SetSiblingIndex(1);
        loreGO.transform.SetSiblingIndex(2);
        nomeGO.transform.SetSiblingIndex(3);
        instrGO.transform.SetSiblingIndex(4);

        // Canvas root stays active; only the panel is hidden by default
        panelGO.SetActive(false);
        Debug.Log($"[SolengardSetup] LoreScreenUI criada — Canvas sempre ativo, LorePanel inativo por padrão (sortingOrder={canvas.sortingOrder})");
    }

    // Creates required system GameObjects if absent; called by SetupAll before wiring.
    static int RunCreateNewSystemObjects(StringBuilder log)
    {
        int total = 0;

        // ── Core singletons ───────────────────────────────────────────────────────
        total += EnsureSystemObject<GameManager>          ("GameManager",           log);
        total += EnsureSystemObject<DiamondSystem>        ("DiamondSystem",         log);
        total += EnsureSystemObject<PermanentUpgradeSystem>("PermanentUpgradeSystem", log);
        total += EnsureSystemObject<ScoreSystem>          ("ScoreSystem",           log);
        total += EnsureSystemObject<SeasonPassSystem>     ("SeasonPassSystem",      log);
        total += EnsureSystemObject<DailyRewardSystem>    ("DailyRewardSystem",     log);
        total += EnsureSystemObject<DailyMissionSystem>   ("DailyMissionSystem",    log);
        total += EnsureSystemObject<AuthSystem>           ("AuthSystem",            log);
        total += EnsureSystemObject<IAPSystem>            ("IAPSystem",             log);
        total += EnsureSystemObject<LocalizationManager>  ("LocalizationManager",   log);

        // ── Gameplay systems ──────────────────────────────────────────────────────
        total += EnsureSystemObject<ZoneManager>           ("ZoneManager",           log);
        total += EnsureSystemObject<ObjectPoolManager>    ("ObjectPoolManager",     log);
        total += EnsureSystemObject<UpgradeSystem>        ("UpgradeSystem",         log);
        total += EnsureSystemObject<ProceduralArenaSystem>("ProceduralArenaSystem", log);
        total += EnsureSystemObject<SimpleArena>          ("SimpleArena",           log);
        total += EnsureSystemObject<RunSessionManager>    ("RunSessionManager",     log);
        total += EnsureSystemObject<GameSceneBootstrap>   ("GameSceneBootstrap",    log);

        // ── Difficulty & wave sub-systems ─────────────────────────────────────────
        total += EnsureSystemObject<WaveTimerSystem>         ("WaveTimerSystem",          log);
        total += EnsureSystemObject<DifficultyAdaptiveSystem>("DifficultyAdaptiveSystem", log);
        total += EnsureSystemObject<RunRewardSystem>         ("RunRewardSystem",           log);
        total += EnsureSystemObject<DynamicDifficultySystem> ("DynamicDifficultySystem",  log);
        total += EnsureSystemObject<TemporaryPowerSystem>    ("TemporaryPowerSystem",      log);
        total += EnsureSystemObject<PropSpawner>             ("PropSpawner",               log);
        total += EnsureSystemObject<WorldChunkManager>      ("WorldChunkManager",         log);
        total += EnsureSystemObject<WaveBoostSystem>        ("WaveBoostSystem",           log);
        total += EnsureSystemObject<AtmosphereController>    ("AtmosphereController",      log);
        total += EnsureSystemObject<XPSystem>                ("XPSystem",                  log);
        total += EnsureSystemObject<BiomeSystem>             ("BiomeSystem",               log);
        total += EnsureSystemObject<VFXManager>              ("VFXManager",                log);
        total += EnsureSystemObject<ProceduralSceneGenerator>("ProceduralSceneGenerator",  log);
        total += EnsureSystemObject<ProceduralFog>           ("ProceduralFog",             log);
        total += EnsureSystemObject<ProceduralParticles>     ("ProceduralParticles",       log);
        total += EnsureSystemObject<VignetteOverlay>         ("VignetteOverlay",           log);
        total += EnsureSystemObject<ZoneManager>             ("ZoneManager",               log);

        return total;
    }

    // Creates the full LevelUp UI hierarchy (Canvas → Panel → 3 dark-fantasy cards horizontal) and wires LevelUpUI.
    static void CreateLevelUpUIInScene(Scene scene)
    {
        var canvasGO = new GameObject("LevelUpCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Rebuild GameScene");
        SceneManager.MoveGameObjectToScene(canvasGO, scene);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var levelUpUI = canvasGO.AddComponent<LevelUpUI>();

        // ── Full-screen dark overlay panel (inactive by default) ─────────────────
        var panelGO = new GameObject("LevelUpPanel");
        Undo.RegisterCreatedObjectUndo(panelGO, "Rebuild GameScene");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        panelGO.SetActive(false);

        // ── Title "ESCOLHA SEU PODER" ─────────────────────────────────────────────
        var titleGO = new GameObject("Title");
        Undo.RegisterCreatedObjectUndo(titleGO, "Rebuild GameScene");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax        = new Vector2(0.5f, 0.5f);
        titleRT.pivot            = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta        = new Vector2(800f, 70f);
        titleRT.anchoredPosition = new Vector2(0f, 160f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "ESCOLHA SEU PODER";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize  = 28f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = new Color(1f, 0.85f, 0.3f);

        // ── Container for 3 cards side-by-side: 180×220 each, 20px gap → 580×220 ─
        var containerGO = new GameObject("CardsContainer");
        Undo.RegisterCreatedObjectUndo(containerGO, "Rebuild GameScene");
        containerGO.transform.SetParent(panelGO.transform, false);
        var containerRT = containerGO.AddComponent<RectTransform>();
        containerRT.anchorMin        = new Vector2(0.5f, 0.5f);
        containerRT.anchorMax        = new Vector2(0.5f, 0.5f);
        containerRT.pivot            = new Vector2(0.5f, 0.5f);
        containerRT.sizeDelta        = new Vector2(580f, 220f);
        containerRT.anchoredPosition = new Vector2(0f, -10f);

        var buttons     = new Button[3];
        var optionNames = new TextMeshProUGUI[3];
        var optionDescs = new TextMeshProUGUI[3];

        float[] cardCenterX = { -200f, 0f, 200f }; // 180px wide + 20px gap

        for (int i = 0; i < 3; i++)
        {
            // ── Card root (Button + EventTrigger for hover scale) ─────────────────
            var cardGO = new GameObject($"Card_{i}");
            Undo.RegisterCreatedObjectUndo(cardGO, "Rebuild GameScene");
            cardGO.transform.SetParent(containerGO.transform, false);
            var cardRT = cardGO.AddComponent<RectTransform>();
            cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
            cardRT.pivot            = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta        = new Vector2(180f, 220f);
            cardRT.anchoredPosition = new Vector2(cardCenterX[i], 0f);

            // Gold border background (fills card completely)
            var borderGO = new GameObject("Border");
            Undo.RegisterCreatedObjectUndo(borderGO, "Rebuild GameScene");
            borderGO.transform.SetParent(cardGO.transform, false);
            var borderRT = borderGO.AddComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = Vector2.zero;
            borderRT.offsetMax = Vector2.zero;
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(1f, 0.75f, 0.2f);

            // Dark purple fill (3px inset from border on each side)
            var fillGO = new GameObject("Fill");
            Undo.RegisterCreatedObjectUndo(fillGO, "Rebuild GameScene");
            fillGO.transform.SetParent(cardGO.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2( 3f,  3f);
            fillRT.offsetMax = new Vector2(-3f, -3f);
            fillGO.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.12f, 0.97f);

            // Button on card root — targetGraphic = border for color transitions on click
            var btn = cardGO.AddComponent<Button>();
            btn.targetGraphic = borderImg;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.9f, 0.5f);
            colors.pressedColor     = new Color(0.8f, 0.6f, 0.1f);
            btn.colors = colors;

            // Hover scale via EventTrigger
            var trigger     = cardGO.AddComponent<EventTrigger>();
            var capturedCard = cardGO;
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => capturedCard.transform.localScale = Vector3.one * 1.05f);
            trigger.triggers.Add(enter);
            var exitEv = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEv.callback.AddListener(_ => capturedCard.transform.localScale = Vector3.one);
            trigger.triggers.Add(exitEv);

            // ── Icon TMP (top 30%) ────────────────────────────────────────────────
            var iconGO = new GameObject("Icon");
            Undo.RegisterCreatedObjectUndo(iconGO, "Rebuild GameScene");
            iconGO.transform.SetParent(cardGO.transform, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.05f, 0.68f);
            iconRT.anchorMax = new Vector2(0.95f, 0.96f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
            iconTMP.text      = "?";
            iconTMP.alignment = TextAlignmentOptions.Center;
            iconTMP.fontSize  = 36f;
            iconTMP.color     = Color.white;

            // ── Name TMP (middle 25%) — gold bold ────────────────────────────────
            var nameGO = new GameObject("Name");
            Undo.RegisterCreatedObjectUndo(nameGO, "Rebuild GameScene");
            nameGO.transform.SetParent(cardGO.transform, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.04f, 0.42f);
            nameRT.anchorMax = new Vector2(0.96f, 0.70f);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text      = "—";
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.fontSize  = 18f;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color     = new Color(1f, 0.85f, 0.3f);

            // ── Desc TMP (bottom 40%) — light gray ───────────────────────────────
            var descGO = new GameObject("Desc");
            Undo.RegisterCreatedObjectUndo(descGO, "Rebuild GameScene");
            descGO.transform.SetParent(cardGO.transform, false);
            var descRT = descGO.AddComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.04f, 0.06f);
            descRT.anchorMax = new Vector2(0.96f, 0.43f);
            descRT.offsetMin = Vector2.zero;
            descRT.offsetMax = Vector2.zero;
            var descTMP = descGO.AddComponent<TextMeshProUGUI>();
            descTMP.text      = "—";
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.fontSize  = 13f;
            descTMP.color     = new Color(0.8f, 0.8f, 0.8f);

            buttons[i]     = btn;
            optionNames[i] = nameTMP;
            optionDescs[i] = descTMP;
        }

        // ── Wire all references on LevelUpUI ─────────────────────────────────────
        var so = new SerializedObject(levelUpUI);
        so.FindProperty("panel").objectReferenceValue = panelGO;

        var btnProp = so.FindProperty("optionButtons");
        btnProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            btnProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];

        var namesProp = so.FindProperty("optionNames");
        namesProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            namesProp.GetArrayElementAtIndex(i).objectReferenceValue = optionNames[i];

        var descsProp = so.FindProperty("optionDescs");
        descsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            descsProp.GetArrayElementAtIndex(i).objectReferenceValue = optionDescs[i];

        so.ApplyModifiedProperties();

        Debug.Log("[SolengardSetup] LevelUpUI dark-fantasy criado e referências conectadas.");
    }

    static int EnsureSystemObject<T>(string goName, StringBuilder log) where T : Component
    {
        var activeScene = SceneManager.GetActiveScene();
        var rootObjects = activeScene.GetRootGameObjects();

        // Find existing instance within the active scene only
        T existing = null;
        foreach (var root in rootObjects)
        {
            existing = root.GetComponentInChildren<T>(true);
            if (existing != null) break;
        }

        if (existing != null)
        {
            // Already on the correct GO — nothing to do
            if (existing.gameObject.name == goName) return 0;

            // On the wrong GO (e.g. GameManager on "Main Camera") — move it
            return MoveSystemComponent(existing, goName, log);
        }

        // Not in scene at all — create
        var newGO = new GameObject(goName);
        Undo.RegisterCreatedObjectUndo(newGO, "Solengard Setup All");
        newGO.AddComponent<T>();

        string msg = $"  Criado GameObject '{goName}' com componente {typeof(T).Name}";
        log.AppendLine(msg);
        Debug.Log($"[SolengardSetup] {msg}");
        return 1;
    }

    // Moves component T from its current (wrong) GO to a new correctly-named GO,
    // copying all serialized references in the process.
    static int MoveSystemComponent<T>(T oldComponent, string newGoName, StringBuilder log) where T : Component
    {
        string oldGoName = oldComponent.gameObject.name;

        var newGO = new GameObject(newGoName);
        Undo.RegisterCreatedObjectUndo(newGO, "Solengard Setup All");
        var newComp = newGO.AddComponent<T>();
        CopySerializedProperties(oldComponent, newComp);
        Undo.DestroyObjectImmediate(oldComponent); // removes component only, not the GO

        string msg = $"  Movido {typeof(T).Name}: '{oldGoName}' → '{newGoName}'";
        log.AppendLine(msg);
        Debug.Log($"[SolengardSetup] {msg}");
        return 1;
    }

    // Copies all visible serialized fields from src to dst (skips m_Script).
    static void CopySerializedProperties(Component src, Component dst)
    {
        var srcSO = new SerializedObject(src);
        var dstSO = new SerializedObject(dst);

        SerializedProperty prop = srcSO.GetIterator();
        if (!prop.NextVisible(true)) return;
        do
        {
            if (prop.name == "m_Script") continue;
            SerializedProperty dstProp = dstSO.FindProperty(prop.propertyPath);
            if (dstProp != null) dstSO.CopyFromSerializedProperty(prop);
        }
        while (prop.NextVisible(false));

        dstSO.ApplyModifiedProperties();
    }

    // Creates a named GameObject at the root of the active scene with component T attached.
    static GameObject CreateSceneSystem<T>(string goName) where T : Component
    {
        var go = new GameObject(goName);
        Undo.RegisterCreatedObjectUndo(go, "Rebuild GameScene");
        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
        go.transform.parent = null;
        go.AddComponent<T>();
        return go;
    }

    static void ConfigurePhysicsMatrix()
    {
        int player   = LayerMask.NameToLayer("Player");
        int enemy    = LayerMask.NameToLayer("Enemy");
        int obstacle = LayerMask.NameToLayer("Obstacle");
        int ground   = LayerMask.NameToLayer("Ground");
        int effect   = LayerMask.NameToLayer("Effect");

        if (player < 0 || enemy < 0) return;

        // Player is kinematic; enemy colliders are triggers — no physical response regardless.
        // Ignoring this pair removes any residual stacking/repulsion near the player.
        Physics2D.IgnoreLayerCollision(player, enemy, true);

        // Enemy-enemy: ignore physical collision — separation is handled via ComputeSeparation() in code.
        // Prevents enemies from forming a ring around the player due to mutual physics repulsion.
        Physics2D.IgnoreLayerCollision(enemy, enemy, true);

        // Obstacles: player collides (uses cover), enemies pass through freely.
        // NOTE: create layer "Obstacle" in Project Settings → Tags and Layers if absent.
        if (obstacle >= 0)
        {
            Physics2D.IgnoreLayerCollision(obstacle, player, false); // herói COLIDE
            Physics2D.IgnoreLayerCollision(obstacle, enemy,  true);  // inimigo ATRAVESSA
        }
        else
        {
            Debug.LogWarning("[SolengardSetup] Layer 'Obstacle' não existe — crie em Project Settings → Tags and Layers.");
        }

        if (ground >= 0)
        {
            Physics2D.IgnoreLayerCollision(ground, ground,   true);
            Physics2D.IgnoreLayerCollision(ground, player,   true);
            Physics2D.IgnoreLayerCollision(ground, enemy,    true);
            if (obstacle >= 0) Physics2D.IgnoreLayerCollision(ground, obstacle, true);
        }
        if (effect >= 0)
        {
            Physics2D.IgnoreLayerCollision(effect, effect, true);
            if (ground >= 0) Physics2D.IgnoreLayerCollision(effect, ground, true);
        }

        Debug.Log("[SolengardSetup] Matriz de colisão configurada.");
    }

    static int RunSetupPoolsAndUpgrades(StringBuilder log)
    {
        int total = 0;

        total += TrySetEnemyCollidersToSolid(log);

        var slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLIME_PREFAB_PATH);
        if (slimePrefab == null)
            Debug.LogWarning($"[SolengardSetup] {SLIME_PREFAB_PATH} não encontrado — pool ignorada.");

        total += TryAddSlimePool(slimePrefab, log);
        total += TryAddEnemyPools(log);
        total += TryAddEnemyPrefabsToZoneManager(log);
        total += TryPopulateUpgrades(log);
        total += TryPopulateSeasonRewards(log);
        total += TryPopulateObstaclePositions(log);

        return total;
    }

    // ── Data populators ───────────────────────────────────────────────────────────

    static int TryAddSlimePool(GameObject prefab, StringBuilder log)
    {
        if (prefab == null) return 0;

        var mgr = Object.FindFirstObjectByType<ObjectPoolManager>(FindObjectsInactive.Include);
        if (mgr == null) { Debug.LogWarning("[SolengardSetup] ObjectPoolManager não encontrado."); return 0; }

        var so  = new SerializedObject(mgr);
        var arr = so.FindProperty("pools");
        if (arr == null || arr.arraySize > 0) return 0;

        arr.InsertArrayElementAtIndex(0);
        var elem = arr.GetArrayElementAtIndex(0);
        elem.FindPropertyRelative("tag").stringValue             = "Slime";
        elem.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        elem.FindPropertyRelative("tamanhoInicial").intValue     = 10;
        so.ApplyModifiedProperties();

        log.AppendLine("  ObjectPoolManager.pools → Pool 'Slime' adicionada (tamanho 10)");
        Debug.Log("[SolengardSetup] Pool 'Slime' criada no ObjectPoolManager.");
        return 1;
    }

    static int TryAddEnemyPools(StringBuilder log)
    {
        var mgr = Object.FindFirstObjectByType<ObjectPoolManager>(FindObjectsInactive.Include);
        if (mgr == null) { Debug.LogWarning("[SolengardSetup] ObjectPoolManager não encontrado."); return 0; }

        var so  = new SerializedObject(mgr);
        var arr = so.FindProperty("pools");
        if (arr == null) return 0;

        var existingTags = new HashSet<string>();
        for (int i = 0; i < arr.arraySize; i++)
            existingTags.Add(arr.GetArrayElementAtIndex(i).FindPropertyRelative("tag").stringValue);

        int added = 0;
        foreach (string path in ENEMY_PREFAB_PATHS)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"[SolengardSetup] Pool prefab não encontrado: {path}"); continue; }

            string tag = System.IO.Path.GetFileNameWithoutExtension(path);
            if (existingTags.Contains(tag)) continue;

            int idx = arr.arraySize;
            arr.InsertArrayElementAtIndex(idx);
            var elem = arr.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("tag").stringValue             = tag;
            elem.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            elem.FindPropertyRelative("tamanhoInicial").intValue     = 10;
            existingTags.Add(tag);
            added++;
            log.AppendLine($"  ObjectPoolManager.pools → Pool '{tag}' adicionada (tamanho 10)");
        }

        if (added > 0)
        {
            so.ApplyModifiedProperties();
            Debug.Log($"[SolengardSetup] {added} enemy pools adicionadas ao ObjectPoolManager.");
        }
        return added;
    }

    static int TryAddEnemyPrefabs(StringBuilder log) =>
        TryAddEnemyPrefabsToZoneManager(log);

    static int TryPopulateUpgrades(StringBuilder log)
    {
        var us = Object.FindFirstObjectByType<UpgradeSystem>(FindObjectsInactive.Include);
        if (us == null) { Debug.LogWarning("[SolengardSetup] UpgradeSystem não encontrado."); return 0; }

        var so  = new SerializedObject(us);
        var arr = so.FindProperty("poolDeUpgrades");
        if (arr == null || arr.arraySize > 0) return 0;

        // (nome, descricao, UpgradeType int, PassiveItemType int)
        var dados = new[]
        {
            ("Mais Dano",        "Aumenta dano em 20%",             (int)UpgradeType.UpgradeArma,        (int)PassiveItemType.DamageGem    ),
            ("Mais Velocidade",  "Aumenta velocidade de movimento", (int)UpgradeType.ItemPassivo,        (int)PassiveItemType.SpeedRune    ),
            ("Cura Instantânea", "Restaura 30 de vida",             (int)UpgradeType.CuraInstantanea,    (int)PassiveItemType.HealthOrb    ),
            ("Mais Vida",        "Aumenta vida máxima em 20",       (int)UpgradeType.AumentarVidaMaxima, (int)PassiveItemType.ShieldAmulet ),
            ("Nova Arma",        "Adiciona uma arma aleatória",     (int)UpgradeType.NovaArma,           (int)PassiveItemType.DamageGem    ),
        };

        for (int i = 0; i < dados.Length; i++)
        {
            arr.InsertArrayElementAtIndex(i);
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("nome").stringValue      = dados[i].Item1;
            e.FindPropertyRelative("descricao").stringValue = dados[i].Item2;
            e.FindPropertyRelative("tipo").intValue         = dados[i].Item3;
            e.FindPropertyRelative("itemPassivo").intValue  = dados[i].Item4;
        }
        so.ApplyModifiedProperties();

        log.AppendLine($"  UpgradeSystem.poolDeUpgrades → {dados.Length} upgrades adicionados (ícones pendentes)");
        return 1;
    }

    static int TryPopulateSeasonRewards(StringBuilder log)
    {
        var sp = Object.FindFirstObjectByType<SeasonPassSystem>(FindObjectsInactive.Include);
        if (sp == null) { Debug.LogWarning("[SolengardSetup] SeasonPassSystem não encontrado."); return 0; }

        var so  = new SerializedObject(sp);
        var arr = so.FindProperty("recompensas");
        if (arr == null || arr.arraySize > 0) return 0;

        // (nivel, nome, diamantes, apenasPremiun)
        // free: 1-5, 11-15, 21-25 | premium: 6-10, 16-20, 26-30
        var r = new (int n, string nome, int d, bool p)[]
        {
            ( 1, "Fragmento das Sombras",  10, false),
            ( 2, "Runa do Iniciante",      15, false),
            ( 3, "Cristal Sombrio",        20, false),
            ( 4, "Essência Corrompida",    25, false),
            ( 5, "Selo das Trevas",        30, false),
            ( 6, "Garra do Demônio",       35, true ),
            ( 7, "Olho do Abismo",         40, true ),
            ( 8, "Osso Maldito",           50, true ),
            ( 9, "Chama Negra",            60, true ),
            (10, "Coração das Sombras",   100, true ),
            (11, "Véu da Escuridão",       15, false),
            (12, "Dente do Leviatã",       20, false),
            (13, "Cinza Eterna",           25, false),
            (14, "Lágrima do Inferno",     30, false),
            (15, "Esporo das Profundezas", 40, false),
            (16, "Escama do Dragão",       50, true ),
            (17, "Veneno do Basilisco",    60, true ),
            (18, "Alma Aprisionada",       75, true ),
            (19, "Sangue do Lich",         90, true ),
            (20, "Fragmento do Vazio",    150, true ),
            (21, "Luz Proibida",           20, false),
            (22, "Grimório Sombrio",       30, false),
            (23, "Relíquia Amaldiçoada",   40, false),
            (24, "Tomo das Trevas",        50, false),
            (25, "Sigilo do Eterno",       60, false),
            (26, "Artefato do Caos",       75, true ),
            (27, "Pedra do Julgamento",   100, true ),
            (28, "Cetro Espectral",       125, true ),
            (29, "Coroa das Sombras",     150, true ),
            (30, "Lenda das Trevas",      300, true ),
        };

        for (int i = 0; i < r.Length; i++)
        {
            arr.InsertArrayElementAtIndex(i);
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("nivel").intValue          = r[i].n;
            e.FindPropertyRelative("nome").stringValue        = r[i].nome;
            e.FindPropertyRelative("diamantes").intValue      = r[i].d;
            e.FindPropertyRelative("apenasPremiun").boolValue = r[i].p;
        }
        so.ApplyModifiedProperties();

        log.AppendLine($"  SeasonPassSystem.recompensas → {r.Length} recompensas adicionadas (níveis 1-30)");
        return 1;
    }

    static int TryPopulateObstaclePositions(StringBuilder log)
    {
        var arena = Object.FindFirstObjectByType<ProceduralArenaSystem>(FindObjectsInactive.Include);
        if (arena == null) { Debug.LogWarning("[SolengardSetup] ProceduralArenaSystem não encontrado."); return 0; }

        var so  = new SerializedObject(arena);
        var arr = so.FindProperty("posicoesObstaculoCandidatas");
        if (arr == null || arr.arraySize > 0) return 0;

        Vector2[] pos =
        {
            new(-4,  3), new(-2,  3), new(2,  3), new(4,  3),
            new(-4,  0),                           new(4,  0),
            new(-4, -3), new(-2, -3), new(2, -3), new(4, -3),
            new( 0,  3),                           new(0, -3),
        };

        for (int i = 0; i < pos.Length; i++)
        {
            arr.InsertArrayElementAtIndex(i);
            arr.GetArrayElementAtIndex(i).vector2Value = pos[i];
        }
        so.ApplyModifiedProperties();

        log.AppendLine($"  ProceduralArenaSystem.posicoesObstaculoCandidatas → {pos.Length} posições adicionadas");
        return 1;
    }

    static int TryAddEnemyPrefabsToZoneManager(StringBuilder log)
    {
        var zm = Object.FindFirstObjectByType<ZoneManager>(FindObjectsInactive.Include);
        if (zm == null) return 0;

        var so         = new SerializedObject(zm);
        var zonesProp  = so.FindProperty("zones");
        if (zonesProp == null || zonesProp.arraySize == 0) return 0;

        int totalAssigned = 0;
        int zoneCount = Mathf.Min(zonesProp.arraySize, ZONE_ENEMY_PATHS.Length);

        for (int z = 0; z < zoneCount; z++)
        {
            var zoneProp = zonesProp.GetArrayElementAtIndex(z);

            // Preencher enemyPrefabs da zona
            var epProp = zoneProp.FindPropertyRelative("enemyPrefabs");
            if (epProp != null)
            {
                epProp.ClearArray();
                foreach (string path in ZONE_ENEMY_PATHS[z])
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[SolengardSetup] Prefab não encontrado: {path}");
                        continue;
                    }
                    epProp.InsertArrayElementAtIndex(epProp.arraySize);
                    epProp.GetArrayElementAtIndex(epProp.arraySize - 1).objectReferenceValue = prefab;
                }
            }

            // Preencher bossPrefabs da zona
            var bpProp = zoneProp.FindPropertyRelative("bossPrefabs");
            if (bpProp != null)
            {
                bpProp.ClearArray();
                foreach (string path in ZONE_BOSS_PATHS[z])
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[SolengardSetup] Boss prefab não encontrado: {path}");
                        continue;
                    }
                    bpProp.InsertArrayElementAtIndex(bpProp.arraySize);
                    bpProp.GetArrayElementAtIndex(bpProp.arraySize - 1).objectReferenceValue = prefab;
                }
            }

            int ep = epProp?.arraySize ?? 0;
            int bp = bpProp?.arraySize ?? 0;
            log.AppendLine($"  Zona {z + 1}: {ep} inimigos, {bp} boss(es)");
            totalAssigned += ep + bp;
        }

        so.ApplyModifiedProperties();
        return totalAssigned > 0 ? 1 : 0;
    }

    static void CreateGameOverUI(Scene scene)
    {
        var canvasGO = new GameObject("GameOverCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Rebuild GameScene");
        SceneManager.MoveGameObjectToScene(canvasGO, scene);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var goUI = canvasGO.AddComponent<GameOverUI>();
        var cg   = canvasGO.AddComponent<CanvasGroup>();

        // Fundo escuro semi-transparente
        var bgGO = new GameObject("Background");
        Undo.RegisterCreatedObjectUndo(bgGO, "Rebuild GameScene");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = new Color(0.05f, 0.01f, 0.01f, 0.93f);

        // Título vermelho-escarlate
        var titleGO = new GameObject("Titulo");
        Undo.RegisterCreatedObjectUndo(titleGO, "Rebuild GameScene");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.5f); titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta = new Vector2(700f, 100f);
        titleRT.anchoredPosition = new Vector2(0f, 250f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "VOCÊ CAIU";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize  = 64f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = new Color(0.85f, 0.10f, 0.10f);

        // Subtítulo (motivo do game over)
        var subGO = new GameObject("Subtitulo");
        Undo.RegisterCreatedObjectUndo(subGO, "Rebuild GameScene");
        subGO.transform.SetParent(canvasGO.transform, false);
        var subRT = subGO.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.5f); subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.sizeDelta = new Vector2(700f, 60f);
        subRT.anchoredPosition = new Vector2(0f, 160f);
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text      = "";
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.fontSize  = 28f;
        subTMP.color     = new Color(0.75f, 0.65f, 0.65f);

        // Stats — zona, kills, score (dourado)
        var statsGO = new GameObject("Stats");
        Undo.RegisterCreatedObjectUndo(statsGO, "Rebuild GameScene");
        statsGO.transform.SetParent(canvasGO.transform, false);
        var statsRT = statsGO.AddComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0.5f, 0.5f); statsRT.anchorMax = new Vector2(0.5f, 0.5f);
        statsRT.pivot = new Vector2(0.5f, 0.5f);
        statsRT.sizeDelta = new Vector2(700f, 50f);
        statsRT.anchoredPosition = new Vector2(0f, 50f);
        var statsTMP = statsGO.AddComponent<TextMeshProUGUI>();
        statsTMP.text      = "";
        statsTMP.alignment = TextAlignmentOptions.Center;
        statsTMP.fontSize  = 26f;
        statsTMP.color     = new Color(0.85f, 0.75f, 0.40f);

        // Botão Tentar Novamente
        var (restartGO, restartBtn) = NewButton(canvasGO.transform, "BotaoReiniciar", "TENTAR NOVAMENTE");
        var restartRT = RT(restartGO);
        restartRT.anchorMin = new Vector2(0.5f, 0.5f); restartRT.anchorMax = new Vector2(0.5f, 0.5f);
        restartRT.pivot     = new Vector2(0.5f, 0.5f);
        restartRT.sizeDelta = new Vector2(480f, 90f);
        restartRT.anchoredPosition = new Vector2(0f, -100f);

        // Botão Menu Principal
        var (menuGO, menuBtn) = NewButton(canvasGO.transform, "BotaoMenu", "MENU PRINCIPAL");
        var menuRT = RT(menuGO);
        menuRT.anchorMin = new Vector2(0.5f, 0.5f); menuRT.anchorMax = new Vector2(0.5f, 0.5f);
        menuRT.pivot     = new Vector2(0.5f, 0.5f);
        menuRT.sizeDelta = new Vector2(480f, 90f);
        menuRT.anchoredPosition = new Vector2(0f, -220f);

        var so = new SerializedObject(goUI);
        so.FindProperty("canvasGroup").objectReferenceValue   = cg;
        so.FindProperty("titulotexto").objectReferenceValue   = titleTMP;
        so.FindProperty("subtitulo").objectReferenceValue     = subTMP;
        so.FindProperty("statsTexto").objectReferenceValue    = statsTMP;
        so.FindProperty("restartButton").objectReferenceValue = restartBtn;
        so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        canvasGO.SetActive(false);
        Debug.Log("[SolengardSetup] GameOverUI criada (sortingOrder=300).");
    }

    // ── Helpers — Setup menus ─────────────────────────────────────────────────────

    static bool ValidateScene(out Scene scene)
    {
        scene = EditorSceneManager.GetActiveScene();
        if (scene.name == EXPECTED_SCENE) return true;

        EditorUtility.DisplayDialog("Solengard Setup",
            $"Abra a cena '{EXPECTED_SCENE}' antes de executar o Setup.\n\nCena atual: '{scene.name}'",
            "OK");
        return false;
    }

    static bool LoadAssets(out GameConfig gameConfig, out PlayerData playerData)
    {
        gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GAME_CONFIG_PATH);
        if (gameConfig == null)
        {
            EditorUtility.DisplayDialog("Solengard Setup",
                $"Asset não encontrado:\n{GAME_CONFIG_PATH}\n\n" +
                "Crie via Assets → Create → Solengard → GameConfig.", "OK");
            playerData = null;
            return false;
        }

        playerData = AssetDatabase.LoadAssetAtPath<PlayerData>(PLAYER_DATA_PATH);
        if (playerData == null)
        {
            EditorUtility.DisplayDialog("Solengard Setup",
                $"Asset não encontrado:\n{PLAYER_DATA_PATH}\n\n" +
                "Crie via Assets → Create → Solengard → PlayerData.", "OK");
            return false;
        }
        return true;
    }

    // Atribui um ScriptableObject/asset a um campo de referência de um componente.
    static int TryAssign<T>(string propertyName, Object value, StringBuilder log)
        where T : Component
    {
        var component = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (component == null)
        {
            Debug.LogWarning($"[SolengardSetup] {typeof(T).Name} não encontrado na cena.");
            return 0;
        }

        var so   = new SerializedObject(component);
        var prop = so.FindProperty(propertyName);

        if (prop == null)
        {
            Debug.LogWarning($"[SolengardSetup] Propriedade '{propertyName}' não encontrada em {typeof(T).Name}. " +
                             "Verifique se o campo está declarado como [SerializeField] ou public.");
            return 0;
        }

        if (prop.objectReferenceValue != null) return 0;

        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();

        string msg = $"  {typeof(T).Name}.{propertyName} → {value.name}";
        log.AppendLine(msg);
        Debug.Log($"[SolengardSetup] {msg}");
        return 1;
    }

    // Atribui um componente de cena encontrado via FindFirstObjectByType<TSource>.
    static int TryAssignComponent<TTarget, TSource>(string propertyName, StringBuilder log)
        where TTarget : Component
        where TSource : Component
    {
        var target = Object.FindFirstObjectByType<TTarget>(FindObjectsInactive.Include);
        if (target == null)
        {
            Debug.LogWarning($"[SolengardSetup] {typeof(TTarget).Name} não encontrado na cena.");
            return 0;
        }

        var source = Object.FindFirstObjectByType<TSource>(FindObjectsInactive.Include);
        if (source == null)
        {
            Debug.LogWarning($"[SolengardSetup] {typeof(TSource).Name} não encontrado — '{propertyName}' não atribuído.");
            return 0;
        }

        var so   = new SerializedObject(target);
        var prop = so.FindProperty(propertyName);

        if (prop == null)
        {
            Debug.LogWarning($"[SolengardSetup] Propriedade '{propertyName}' não encontrada em {typeof(TTarget).Name}.");
            return 0;
        }

        if (prop.objectReferenceValue != null) return 0;

        prop.objectReferenceValue = source;
        so.ApplyModifiedProperties();

        string msg = $"  {typeof(TTarget).Name}.{propertyName} → {source.gameObject.name}";
        log.AppendLine(msg);
        Debug.Log($"[SolengardSetup] {msg}");
        return 1;
    }

    // Atribui LayerMask pelo nome da layer; avisa no warns se a layer não existir.
    static int TryAssignLayerMask<T>(string propertyName, string layerName,
                                     StringBuilder log, StringBuilder warns)
        where T : Component
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex == -1)
        {
            warns.AppendLine($"  Layer '{layerName}' não existe. Crie em Project Settings → Tags and Layers e rode Setup Systems novamente.");
            return 0;
        }

        var component = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (component == null)
        {
            Debug.LogWarning($"[SolengardSetup] {typeof(T).Name} não encontrado na cena.");
            return 0;
        }

        var so   = new SerializedObject(component);
        var prop = so.FindProperty(propertyName);

        if (prop == null)
        {
            Debug.LogWarning($"[SolengardSetup] Propriedade '{propertyName}' não encontrada em {typeof(T).Name}.");
            return 0;
        }

        if (prop.intValue != 0) return 0;

        prop.intValue = 1 << layerIndex;
        so.ApplyModifiedProperties();

        string msg = $"  {typeof(T).Name}.{propertyName} → layer '{layerName}' (1 << {layerIndex})";
        log.AppendLine(msg);
        Debug.Log($"[SolengardSetup] {msg}");
        return 1;
    }

    // Loads a prefab by path and assigns it to a serialized field (skips if already set)
    static int TryAssignPrefabByPath<T>(string propertyName, string assetPath, StringBuilder log)
        where T : Component
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[SolengardSetup] Prefab não encontrado: {assetPath}");
            return 0;
        }
        return TryAssign<T>(propertyName, prefab, log);
    }

    // Enemy colliders are set to isTrigger=true at runtime by EnemyBase.Awake().
    // This function is kept as a no-op to avoid breaking the call site.
    static int TrySetEnemyCollidersToSolid(StringBuilder log) => 0;

    static void AppendResult(StringBuilder sb, int total, StringBuilder log)
    {
        if (total > 0)
            sb.AppendLine($"✓ {total} atribuição(ões) realizadas:\n{log}");
        else
            sb.AppendLine("Nenhuma atribuição necessária.\nTodos os campos já estavam preenchidos.\n");
    }

    static void AppendManualPendencies(StringBuilder sb)
    {
        sb.AppendLine("─────────────────────────────────────");
        sb.AppendLine("Campos que requerem atribuição MANUAL no Inspector:");
        sb.AppendLine("• GameManager → proceduralArena");
        sb.AppendLine("  (arrastar o GameObject ProceduralArenaSystem da hierarquia)");
        sb.AppendLine();
        sb.AppendLine("• ZoneManager → zones[].enemyPrefabs e bossPrefabs");
        sb.AppendLine("  (preenchidos automaticamente pelo Setup — 5 zonas com inimigos e bosses distintos)");
        sb.AppendLine();
        sb.AppendLine("• ObjectPoolManager.pools[0].prefab");
        sb.AppendLine("  (atribuir Slime.prefab ao criar Assets/Prefabs/Enemies/Slime.prefab)");
        sb.AppendLine();
        sb.AppendLine("• UpgradeSystem.poolDeUpgrades[].icone");
        sb.AppendLine("  (Sprites de ícone para cada upgrade — arte pendente)");
        sb.AppendLine();
        sb.AppendLine("• ProceduralArenaSystem → prefabObstaculo + modificadoresDisponiveis");
        sb.AppendLine();
        sb.AppendLine("• HUDComplete, UpgradeUIManager, MainMenuManager, MobileJoystick");
        sb.AppendLine("  (todas as referências de UI — configurar após montar as cenas)");
        sb.AppendLine();
        sb.AppendLine("• GameOverScreen — execute 'Layout GameScene' para criar e conectar automaticamente");
        sb.AppendLine();
        sb.AppendLine("• WaveWarningUI → banner + CanvasGroup + TextMeshProUGUI");
        sb.AppendLine("  (banner de aviso de zone — conectar na hierarquia da GameScene)");
    }

    // ── Helpers — Create MainMenu Scene ──────────────────────────────────────────

    // Root-level GameObject (no UI, no parent)
    static GameObject NewGO(string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create MainMenu Scene");
        return go;
    }

    // UI child with RectTransform; mirrors DefaultControls.CreateUIObject pattern
    static GameObject NewUIChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create MainMenu Scene");
        go.AddComponent<RectTransform>(); // replaces Transform with RectTransform
        go.transform.SetParent(parent, false);
        return go;
    }

    // Singleton root GO; parent must be null — DontDestroyOnLoad requires scene root
    static void NewSingleton(Transform parent, string name, System.Type type)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create MainMenu Scene");
        if (parent != null) go.transform.SetParent(parent, false);
        try { go.AddComponent(type); }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SolengardSetup] Não foi possível adicionar {type.Name}: {e.Message}");
        }
    }

    // Button: RectTransform + Image + Button + child TMP label
    static (GameObject go, Button btn) NewButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create MainMenu Scene");
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.10f, 0.38f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(labelGO, "Create MainMenu Scene");
        labelGO.AddComponent<RectTransform>();
        labelGO.transform.SetParent(go.transform, false);
        StretchFull(RT(labelGO));
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = 28f;
        tmp.fontStyle = FontStyles.Bold;

        return (go, btn);
    }

    // TextMeshProUGUI element with placeholder text
    static TextMeshProUGUI NewTMP(Transform parent, string name, string placeholder, float fontSize = 24f)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create MainMenu Scene");
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = placeholder;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = fontSize;
        return tmp;
    }

    // Full-screen panel with semi-transparent background; inactive by default
    static GameObject NewPanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create MainMenu Scene");
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.05f, 0.02f, 0.08f, 0.97f);
        StretchFull(RT(go));
        go.SetActive(false);
        return go;
    }

    // Stretch RectTransform to fill its parent completely
    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    // Shorthand to get RectTransform (valid after any UI component has been added)
    static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();

    // Set a SerializedObject reference field by name; logs warning if property not found
    static void WireRef(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogWarning($"[SolengardSetup] Propriedade '{propName}' não encontrada em MainMenuManager.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    // Overwrites Build Settings with MainMenu[0] + GameScene[1], no duplicates
    static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
            new EditorBuildSettingsScene(GAME_SCENE_PATH,      true),
        };
        Debug.Log("[SolengardSetup] Build Settings → MainMenu[0], GameScene[1]");
    }

    // ── Rich Environment ─────────────────────────────────────────────────────────

    [MenuItem("Solengard/Setup Rich Environment")]
    static void SetupRichEnvironment()
    {
        if (!ValidateScene(out var scene)) return;
        SetupRichPrefabs();
        SetupWorldChunkManager();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Solengard — Rich Environment",
            "Prefabs ricos criados e WorldChunkManager configurado.", "OK");
    }

    [MenuItem("Solengard/Setup Rich Environment", validate = true)]
    static bool ValidateSetupRichEnvironment() =>
        !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().name);

    [MenuItem("Solengard/Setup/Copiar Prefabs para Resources")]
    static void CopyPrefabsToResources()
    {
        const string SRC = "Assets/Prefabs/Environment/Rich";
        const string DST = "Assets/Resources/Environment/Rich";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Environment"))
            AssetDatabase.CreateFolder("Assets/Resources", "Environment");
        if (!AssetDatabase.IsValidFolder(DST))
            AssetDatabase.CreateFolder("Assets/Resources/Environment", "Rich");

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { SRC });
        int copied = 0, skipped = 0;
        foreach (var guid in guids)
        {
            var srcPath = AssetDatabase.GUIDToAssetPath(guid);
            var name    = Path.GetFileName(srcPath);
            var dstPath = DST + "/" + name;
            if (!File.Exists(Path.GetFullPath(dstPath)))
            {
                AssetDatabase.CopyAsset(srcPath, dstPath);
                copied++;
            }
            else skipped++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Setup] {copied} prefabs copiados para Resources/Environment/Rich/ ({skipped} já existiam)");
    }

    static void SetupRichPrefabs()
    {
        string richDir = "Assets/Prefabs/Environment/Rich";
        if (!AssetDatabase.IsValidFolder(richDir))
            AssetDatabase.CreateFolder("Assets/Prefabs/Environment", "Rich");

        // VEREMOTH — Season2_Forest
        CreateRichPrefab(richDir + "/Veremoth_Tree.prefab",
            new[]{ "Assets/Art/Environment/Season2_Forest/Trees/PNG/Assets_separately" },
            new[]{ "Broken_tree","Willow","Burned_tree","Curved_tree",
                   "Tree1","Tree2","Tree3","Tree4","Tree5",
                   "Tree6","Tree7","Tree8","Moss_tree" },
            new[]{ "_shadow","_no_shadow","Snow","Christmas","Palm","Light_balls",
                   "Living","Autumn","Blue","Flower","Fruit","Luminous","Mega",
                   "Swirl","White","Ent","Idol" },
            true, 0.35f);

        CreateRichPrefab(richDir + "/Veremoth_Bush.prefab",
            new[]{ "Assets/Art/Environment/Season2_Forest/Bushes/PNG/Assets" },
            new[]{ "Bush1","Bush2","Bush3","Bush4","Bush5","Bush6",
                   "Bush7","Bush8","Bush_simple" },
            new[]{ "_shadow","Snow","Cactus","Autumn" },
            false, 0f);

        CreateRichPrefab(richDir + "/Veremoth_Mushroom.prefab",
            new[]{ "Assets/Art/Environment/Season2_Forest/Objects/PNG/Assets" },
            new[]{ "mushroom","Mushroom","Chanterelle","Fern" },
            new[]{ "_shadow","Snow" },
            false, 0f);

        CreateRichPrefab(richDir + "/Veremoth_Rock.prefab",
            new[]{ "Assets/Art/Environment/Season2_Forest/Tileset/PNG/Objects_separated" },
            new[]{ "Beige_stone","Brown_stone","Light_stone" },
            new[]{ "_shadow","Snow","Water" },
            true, 0.3f);

        CreateRichPrefab(richDir + "/Veremoth_Ruin.prefab",
            new[]{ "Assets/Art/Environment/Season2_Forest/Tileset/PNG/Objects_separated" },
            new[]{ "Ruin","ruin" },
            new[]{ "_shadow","_grass","_ground","Snow","Water" },
            true, 0.4f);

        // KHORDUUM — Season5_Cave
        CreateRichPrefab(richDir + "/Khorduum_Crystal.prefab",
            new[]{ "Assets/Art/Environment/Season5_Cave/Crystals/PNG/Assets" },
            new[]{ "crystal","Crystal" },
            new[]{ "_shadow","_grass","_ground","_dark","_light" },
            false, 0f);

        CreateRichPrefab(richDir + "/Khorduum_Stone.prefab",
            new[]{ "Assets/Art/Environment/Season5_Cave/Objects/PNG/Objects_separately",
                   "Assets/Art/Environment/Season5_Cave/Tileset/PNG/Objects_separately" },
            new[]{ "stone","Stone","stalagmite" },
            new[]{ "_shadow","_dark","_light","_grass","_ground" },
            true, 0.35f);

        CreateRichPrefab(richDir + "/Khorduum_Mushroom.prefab",
            new[]{ "Assets/Art/Environment/Season5_Cave/Objects/PNG/Objects_separately" },
            new[]{ "mushroom","Mushroom" },
            new[]{ "_dark_shadow" },
            false, 0f);

        CreateRichPrefab(richDir + "/Khorduum_Object.prefab",
            new[]{ "Assets/Art/Environment/Season5_Cave/Objects/PNG/Objects_separately" },
            new[]{ "web","cocoon","magic","spider","bone","Bonefire","caveman","centipede" },
            new[]{ "_dark_shadow" },
            false, 0f);

        // VALDROSS — Season6_Undead
        CreateRichPrefab(richDir + "/Valdross_Grave.prefab",
            new[]{ "Assets/Art/Environment/Season6_Undead/Objects/PNG/Objects_separately" },
            new[]{ "Grave","grave","coffin","Scull_door","excavated" },
            new[]{ "_shadow2","_shadow3" },
            true, 0.3f);

        CreateRichPrefab(richDir + "/Valdross_Bones.prefab",
            new[]{ "Assets/Art/Environment/Season6_Undead/Objects/PNG/Objects_separately" },
            new[]{ "Bones","bone_","skull","Pile_sculls","Dead_arm" },
            new[]{ "_shadow2","_shadow3" },
            false, 0f);

        CreateRichPrefab(richDir + "/Valdross_Tree.prefab",
            new[]{ "Assets/Art/Environment/Season6_Undead/Objects/PNG/Objects_separately" },
            new[]{ "Dead_tree","Broken_tree","monster_tree","Thorn_plant",
                   "undead_plant","Plant_shadow1" },
            new[]{ "_shadow2","_shadow3" },
            true, 0.3f);

        CreateRichPrefab(richDir + "/Valdross_Object.prefab",
            new[]{ "Assets/Art/Environment/Season6_Undead/Objects/PNG/Objects_separately" },
            new[]{ "web_rock","web_tree","Crystal_shadow1","mushroom1_1",
                   "mushroom2_1","Lich" },
            new[]{ "_shadow2","_shadow3" },
            false, 0f);

        // GORVETH — Season4_Swamp
        CreateRichPrefab(richDir + "/Gorveth_Tree.prefab",
            new[]{ "Assets/Art/Environment/Season4_Swamp/Objects/PNG/Assets" },
            new[]{ "Sculls_tree","Curved_tree","Tree1","Tree2",
                   "Tree3","Tree4","Broken_tree","Tree-fern" },
            new[]{ "_shadow","_grass","_ground","Water" },
            true, 0.35f);

        CreateRichPrefab(richDir + "/Gorveth_Plant.prefab",
            new[]{ "Assets/Art/Environment/Season4_Swamp/Objects/PNG/Assets" },
            new[]{ "Predator_plant","Fren_flower","Fern","Grass_pink",
                   "Grass_white","Reeds","Moss" },
            new[]{ "_shadow","_grass","_ground","Water" },
            false, 0f);

        CreateRichPrefab(richDir + "/Gorveth_Object.prefab",
            new[]{ "Assets/Art/Environment/Season4_Swamp/Objects/PNG/Assets" },
            new[]{ "Bones","Cauldron","Stick","Totem","totem","Witch","House","Statue" },
            new[]{ "_shadow","_grass","_ground","Water" },
            true, 0.25f);

        CreateRichPrefab(richDir + "/Gorveth_Mushroom.prefab",
            new[]{ "Assets/Art/Environment/Season4_Swamp/Objects/PNG/Assets" },
            new[]{ "Mushroom_black","Mushroom_gray","Mushroom_red",
                   "Musgroom1","Musgroom2","Musgroom3","Musgroom4" },
            new[]{ "_shadow","_grass","_ground","Water" },
            false, 0f);

        // ARKENFALL — Season3_Grassland + Season6_Undead
        CreateRichPrefab(richDir + "/Arkenfall_Rock.prefab",
            new[]{ "Assets/Art/Environment/Season3_Grassland/Rocks/PNG/Objects_separately" },
            new[]{ "Rock1_","Rock2_","Rock3_","Rock4_",
                   "Rock5_","Rock6_","Rock7_","Rock8_" },
            new[]{ "_no_shadow","Snow","Water","_grass","_ground" },
            true, 0.35f);

        CreateRichPrefab(richDir + "/Arkenfall_Ruin.prefab",
            new[]{ "Assets/Art/Environment/Season3_Grassland/Ruins/PNG/Assets" },
            new[]{ "Brown_ruins","Brown-gray_ruins","Blue-gray_ruins",
                   "Sand_ruins","Yellow_ruins" },
            new[]{ "Snow","Water" },
            true, 0.4f);

        CreateRichPrefab(richDir + "/Arkenfall_Bones.prefab",
            new[]{ "Assets/Art/Environment/Season6_Undead/Objects/PNG/Objects_separately" },
            new[]{ "Bones_shadow2","bone_monster","Dead_arm_shadow2",
                   "skull_chasm","Pile_sculls" },
            new[]{ "_shadow3" },
            false, 0f);

        CreateRichPrefab(richDir + "/Arkenfall_Tree.prefab",
            new[]{ "Assets/Art/Environment/Season6_Undead/Objects/PNG/Objects_separately" },
            new[]{ "Tree_shadow2","Broken_tree_shadow2","Dead_tree_shadow2",
                   "monster_tree","web_tree" },
            new[]{ "_shadow3" },
            false, 0f);

        AssetDatabase.Refresh();
        Debug.Log("[RichEnv] Todos os prefabs ricos criados!");
    }

    static void CreateRichPrefab(string prefabPath, string[] searchFolders,
        string[] includeKeywords, string[] excludeKeywords,
        bool hasCollider, float colRadius)
    {
        var sprites = new List<Sprite>();
        var guids   = AssetDatabase.FindAssets("t:Sprite", searchFolders);

        foreach (var g in guids)
        {
            var    path      = AssetDatabase.GUIDToAssetPath(g);
            string nameLower = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();

            bool excluded = false;
            foreach (var ex in excludeKeywords)
                if (nameLower.Contains(ex.ToLower())) { excluded = true; break; }
            if (excluded) continue;

            bool included = false;
            foreach (var inc in includeKeywords)
                if (nameLower.Contains(inc.ToLower())) { included = true; break; }
            if (!included) continue;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) sprites.Add(sprite);
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[RichEnv] Nenhum sprite encontrado para {prefabPath}");
            return;
        }

        var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        var ep = go.AddComponent<EnvironmentProp>();
        ep.sprites        = sprites;
        ep.hasCollider    = hasCollider;
        ep.colliderRadius = colRadius;

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"[RichEnv] {System.IO.Path.GetFileNameWithoutExtension(prefabPath)}: {sprites.Count} sprites");
    }

    static void SetupWorldChunkManager()
    {
        var wcm = Object.FindFirstObjectByType<WorldChunkManager>(FindObjectsInactive.Include);
        if (wcm == null) { Debug.LogError("[Setup] WorldChunkManager não encontrado na cena"); return; }

        string[][] biomePaths = new string[][]
        {
            new[]{ "Assets/Prefabs/Environment/Rich/Veremoth_Tree.prefab",
                   "Assets/Prefabs/Environment/Rich/Veremoth_Bush.prefab",
                   "Assets/Prefabs/Environment/Rich/Veremoth_Mushroom.prefab",
                   "Assets/Prefabs/Environment/Rich/Veremoth_Rock.prefab",
                   "Assets/Prefabs/Environment/Rich/Veremoth_Ruin.prefab" },
            new[]{ "Assets/Prefabs/Environment/Rich/Khorduum_Crystal.prefab",
                   "Assets/Prefabs/Environment/Rich/Khorduum_Stone.prefab",
                   "Assets/Prefabs/Environment/Rich/Khorduum_Mushroom.prefab",
                   "Assets/Prefabs/Environment/Rich/Khorduum_Object.prefab" },
            new[]{ "Assets/Prefabs/Environment/Rich/Valdross_Grave.prefab",
                   "Assets/Prefabs/Environment/Rich/Valdross_Bones.prefab",
                   "Assets/Prefabs/Environment/Rich/Valdross_Tree.prefab",
                   "Assets/Prefabs/Environment/Rich/Valdross_Object.prefab" },
            new[]{ "Assets/Prefabs/Environment/Rich/Gorveth_Tree.prefab",
                   "Assets/Prefabs/Environment/Rich/Gorveth_Plant.prefab",
                   "Assets/Prefabs/Environment/Rich/Gorveth_Object.prefab",
                   "Assets/Prefabs/Environment/Rich/Gorveth_Mushroom.prefab" },
            new[]{ "Assets/Prefabs/Environment/Rich/Arkenfall_Rock.prefab",
                   "Assets/Prefabs/Environment/Rich/Arkenfall_Ruin.prefab",
                   "Assets/Prefabs/Environment/Rich/Arkenfall_Bones.prefab",
                   "Assets/Prefabs/Environment/Rich/Arkenfall_Tree.prefab" },
        };

        var so = new SerializedObject(wcm);
        so.Update();
        var bioArr = so.FindProperty("biomeProps");
        bioArr.arraySize = 5;

        for (int b = 0; b < 5; b++)
        {
            var listProp = bioArr.GetArrayElementAtIndex(b).FindPropertyRelative("prefabs");
            var paths    = biomePaths[b];
            listProp.arraySize = paths.Length;
            int found = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = go;
                if (go != null) found++;
            }
            Debug.Log($"[Setup] Bioma {b}: {found}/{paths.Length} prefabs");
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Setup] Cena marcada como suja — salve com Ctrl+S antes de Play");
        Debug.Log("[Setup] WorldChunkManager configurado para todos os 5 biomas");
    }
}
