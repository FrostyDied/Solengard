using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;

// Menu: Solengard ▸ Setup Scene / Setup Systems / Setup Pools and Upgrades / Setup All
// Atribui automaticamente referências e dados de gameplay a componentes da cena.
// Nunca sobrescreve valores existentes; exige GameScene aberta.
public static class SolengardSetup
{
    const string GAME_CONFIG_PATH  = "Assets/Data/GameConfig.asset";
    const string PLAYER_DATA_PATH  = "Assets/Data/PlayerData.asset";
    const string SLIME_PREFAB_PATH = "Assets/Prefabs/Enemies/Slime.prefab";
    const string EXPECTED_SCENE    = "GameScene";

    // ── Setup Scene ──────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Setup Scene")]
    static void SetupScene()
    {
        if (!ValidateScene(out var scene)) return;
        if (!LoadAssets(out var gameConfig, out var playerData)) return;

        var log   = new StringBuilder();
        int total = RunSetupScene(gameConfig, playerData, log);

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

        log.AppendLine("── Setup Scene ──────────────────────");
        int t1 = RunSetupScene(gameConfig, playerData, log);

        log.AppendLine("\n── Setup Systems ────────────────────");
        int t2 = RunSetupSystems(log, warns);

        log.AppendLine("\n── Setup Pools & Upgrades ───────────");
        int t3 = RunSetupPoolsAndUpgrades(log);

        int total = t1 + t2 + t3;
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

    // ── Core logic ───────────────────────────────────────────────────────────────

    static int RunSetupScene(GameConfig gameConfig, PlayerData playerData, StringBuilder log)
    {
        int total = 0;
        total += TryAssign<WaveManager>("gameConfig",   gameConfig,  log);
        total += TryAssign<DiamondSystem>("playerData",  playerData,  log);
        total += TryAssign<ScoreSystem>("playerData",    playerData,  log);
        total += TryAssign<SeasonPassSystem>("playerData", playerData, log);
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

        return total;
    }

    static int RunSetupPoolsAndUpgrades(StringBuilder log)
    {
        int total = 0;

        var slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLIME_PREFAB_PATH);
        if (slimePrefab == null)
            Debug.LogWarning($"[SolengardSetup] Prefab não encontrado: {SLIME_PREFAB_PATH} — pools e enemyPrefabs ignorados.");

        total += TryAddSlimePool(slimePrefab, log);
        total += TryAddEnemyPrefab(slimePrefab, log);
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

    static int TryAddEnemyPrefab(GameObject prefab, StringBuilder log)
    {
        if (prefab == null) return 0;

        var wm = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        if (wm == null) { Debug.LogWarning("[SolengardSetup] WaveManager não encontrado."); return 0; }

        var so  = new SerializedObject(wm);
        var arr = so.FindProperty("enemyPrefabs");
        if (arr == null || arr.arraySize > 0) return 0;

        arr.InsertArrayElementAtIndex(0);
        arr.GetArrayElementAtIndex(0).objectReferenceValue = prefab;
        so.ApplyModifiedProperties();

        log.AppendLine("  WaveManager.enemyPrefabs → Slime.prefab adicionado");
        Debug.Log("[SolengardSetup] Slime.prefab adicionado ao WaveManager.enemyPrefabs.");
        return 1;
    }

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

    // ── Helpers ───────────────────────────────────────────────────────────────────

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
        sb.AppendLine("• GameManager → waveManager");
        sb.AppendLine("  (arrastar o GameObject WaveManager da hierarquia)");
        sb.AppendLine();
        sb.AppendLine("• GameManager → proceduralArena");
        sb.AppendLine("  (arrastar o GameObject ProceduralArenaSystem da hierarquia)");
        sb.AppendLine();
        sb.AppendLine("• WaveManager → spawnPoints");
        sb.AppendLine("  (arrastar Transforms de spawn ao redor da arena)");
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
    }
}
