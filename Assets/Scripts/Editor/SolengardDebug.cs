using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

// Temporário — diagnóstico e correção de sistemas fora do lugar.
// Remover após estabilizar a GameScene.
public static class SolengardDebug
{
    // ── Debug Scene ──────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Debug Scene")]
    static void DebugScene()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══ SOLENGARD SCENE DEBUG ═══\n");

        // 1. Cena ativa
        var activeScene = SceneManager.GetActiveScene();
        sb.AppendLine($"[1] Active scene name: '{activeScene.name}'");
        Debug.Log($"[SolengardDebug] Active scene name: '{activeScene.name}'");

        // 2. Quantidade de cenas carregadas
        sb.AppendLine($"[2] loadedSceneCount: {SceneManager.loadedSceneCount}");
        Debug.Log($"[SolengardDebug] loadedSceneCount: {SceneManager.loadedSceneCount}");

        // 3. Para cada cena carregada, nome + root GameObjects
        sb.AppendLine("\n[3] Todas as cenas carregadas:");
        for (int i = 0; i < SceneManager.loadedSceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            var roots = scene.GetRootGameObjects();
            sb.AppendLine($"  Cena [{i}]: '{scene.name}' — {roots.Length} root GOs");
            Debug.Log($"[SolengardDebug] Cena [{i}]: '{scene.name}' — {roots.Length} root GOs");
            foreach (var go in roots)
            {
                sb.AppendLine($"    • {go.name} (active={go.activeSelf})");
                Debug.Log($"[SolengardDebug]     • {go.name} (active={go.activeSelf})");
            }
        }

        // 4. FindFirstObjectByType<GameManager>
        sb.AppendLine("\n[4] FindFirstObjectByType<GameManager>(Include):");
        var found = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (found != null)
        {
            sb.AppendLine($"  → ENCONTRADO: '{found.gameObject.name}' (scene='{found.gameObject.scene.name}')");
            Debug.Log($"[SolengardDebug] FindFirstObjectByType<GameManager> → ENCONTRADO em '{found.gameObject.scene.name}'/{found.gameObject.name}");
        }
        else
        {
            sb.AppendLine("  → NULL (não encontrado em nenhuma cena)");
            Debug.LogWarning("[SolengardDebug] FindFirstObjectByType<GameManager> → NULL");
        }

        // 5. activeScene root GOs — GameManager presente e no lugar certo?
        sb.AppendLine($"\n[5] activeScene ('{activeScene.name}') root GOs com GameManager:");
        var activeRoots = activeScene.GetRootGameObjects();
        bool foundInActive = false;
        foreach (var go in activeRoots)
        {
            var gm = go.GetComponentInChildren<GameManager>(true);
            if (gm != null)
            {
                bool correct = go.name == "GameManager";
                sb.AppendLine($"  → ENCONTRADO: '{go.name}' tem GameManager (active={go.activeSelf}) {(correct ? "✓ nome correto" : "✗ NOME ERRADO — execute Fix GameManager")}");
                Debug.Log($"[SolengardDebug] GameManager em '{go.name}' — nome correto: {correct}");
                foundInActive = true;
            }
        }
        if (!foundInActive)
        {
            sb.AppendLine("  → NÃO encontrado na cena ativa");
            Debug.LogWarning("[SolengardDebug] GameManager NÃO encontrado na cena ativa");
        }

        sb.AppendLine("\n═══ FIM DO DEBUG ═══");
        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Solengard Debug Scene", sb.ToString(), "OK");
    }

    // ── Fix GameManager (e outros sistemas no lugar errado) ─────────────────────

    [MenuItem("Solengard/Fix GameManager")]
    static void FixGameManager()
    {
        Undo.SetCurrentGroupName("Fix Solengard Systems");
        int undoGroup = Undo.GetCurrentGroup();

        var log   = new System.Text.StringBuilder();
        int fixed_ = 0;

        fixed_ += FixSystem<GameManager>          ("GameManager",           log);
        fixed_ += FixSystem<ZoneManager>           ("ZoneManager",           log);
        fixed_ += FixSystem<ObjectPoolManager>    ("ObjectPoolManager",     log);
        fixed_ += FixSystem<UpgradeSystem>        ("UpgradeSystem",         log);
        fixed_ += FixSystem<DiamondSystem>        ("DiamondSystem",         log);
        fixed_ += FixSystem<ScoreSystem>          ("ScoreSystem",           log);
        fixed_ += FixSystem<SeasonPassSystem>     ("SeasonPassSystem",      log);
        fixed_ += FixSystem<DailyRewardSystem>    ("DailyRewardSystem",     log);
        fixed_ += FixSystem<DailyMissionSystem>   ("DailyMissionSystem",    log);
        fixed_ += FixSystem<AuthSystem>           ("AuthSystem",            log);
        fixed_ += FixSystem<IAPSystem>            ("IAPSystem",             log);
        fixed_ += FixSystem<LocalizationManager>  ("LocalizationManager",   log);
        fixed_ += FixSystem<ProceduralArenaSystem>("ProceduralArenaSystem", log);
        fixed_ += FixSystem<GameSceneBootstrap>   ("GameSceneBootstrap",    log);
        fixed_ += FixSystem<WaveTimerSystem>         ("WaveTimerSystem",          log);
        fixed_ += FixSystem<DifficultyAdaptiveSystem>("DifficultyAdaptiveSystem", log);
        fixed_ += FixSystem<RunRewardSystem>         ("RunRewardSystem",           log);
        fixed_ += FixSystem<DynamicDifficultySystem> ("DynamicDifficultySystem",  log);
        fixed_ += FixSystem<TemporaryPowerSystem>    ("TemporaryPowerSystem",      log);

        Undo.CollapseUndoOperations(undoGroup);

        if (fixed_ > 0)
            EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);

        string msg = fixed_ > 0
            ? $"✓ {fixed_} sistema(s) movido(s) para GameObjects corretos:\n\n{log}"
            : "Nenhum sistema fora do lugar. Tudo já estava correto.";

        Debug.Log($"[SolengardDebug] Fix GameManager — {fixed_} correção(ões).\n{log}");
        EditorUtility.DisplayDialog("Solengard Fix GameManager", msg, "OK");
    }

    [MenuItem("Solengard/Fix GameManager", validate = true)]
    static bool ValidateFixGameManager() =>
        !string.IsNullOrEmpty(SceneManager.GetActiveScene().name);

    // ── Forçar próxima zona (Play Mode) ─────────────────────────────────────────

    [MenuItem("Solengard/Debug/Forcar Proxima Zona")]
    static void ForceNextZone()
    {
        var zm = Object.FindFirstObjectByType<ZoneManager>();
        if (zm == null) { Debug.LogError("[Debug] ZoneManager não encontrado"); return; }

        var field = typeof(ZoneManager).GetField("_allBossesDefeated",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(zm, true);
            Debug.Log($"[Debug] Forçando avanço para próxima zona (zona atual: {zm.CurrentZone + 1})");
        }
        else
        {
            Debug.LogError("[Debug] Campo _allBossesDefeated não encontrado em ZoneManager");
        }

        var ph = PlayerController.Instance?.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.RestoreHealth(ph.MaxHealth, ph.MaxHealth);
            Debug.Log("[Debug] Player restaurado para próxima zona");
        }
    }

    // ── Diagnóstico de posições ──────────────────────────────────────────────────

    [MenuItem("Solengard/Debug/Log Posicoes")]
    static void LogPosicoes()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Debug] Log Posições só funciona durante o Play Mode. Dê Play primeiro.");
            return;
        }

        var player = Object.FindFirstObjectByType<PlayerController>();
        var camera = Camera.main;
        var wcm    = Object.FindFirstObjectByType<WorldChunkManager>();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DIAGNÓSTICO DE POSIÇÕES ===");
        sb.AppendLine($"Timestamp: {System.DateTime.Now:HH:mm:ss}");
        sb.AppendLine();

        if (player != null)
        {
            sb.AppendLine("PLAYER");
            sb.AppendLine($"  Posição: {player.transform.position}");
            sb.AppendLine($"  Chunk atual: {WorldToChunk(player.transform.position, 20f)}");
        }
        else sb.AppendLine("PLAYER: NULL");

        if (camera != null)
        {
            sb.AppendLine("CÂMERA");
            sb.AppendLine($"  Posição: {camera.transform.position}");
            sb.AppendLine($"  OrthoSize: {camera.orthographicSize}");
            sb.AppendLine($"  Visível: X[{camera.transform.position.x - camera.orthographicSize * camera.aspect:F1} a {camera.transform.position.x + camera.orthographicSize * camera.aspect:F1}]");
            sb.AppendLine($"           Y[{camera.transform.position.y - camera.orthographicSize:F1} a {camera.transform.position.y + camera.orthographicSize:F1}]");
        }
        else sb.AppendLine("CÂMERA: NULL");

        if (wcm != null)
        {
            var activeField = typeof(WorldChunkManager).GetField("_active",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var active = activeField?.GetValue(wcm) as Dictionary<Vector2Int, ChunkInstance>;

            sb.AppendLine($"CHUNKS ATIVOS: {active?.Count ?? 0}");
            if (active != null)
            {
                foreach (var kv in active)
                {
                    var chunk = kv.Value;
                    if (chunk == null) continue;

                    var propsField = typeof(ChunkInstance).GetField("_props",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var props     = propsField?.GetValue(chunk) as List<GameObject>;
                    int propCount = props?.Count ?? 0;
                    int nullProps = 0;
                    if (props != null) foreach (var p in props) if (p == null) nullProps++;

                    bool visivel = false;
                    if (camera != null)
                    {
                        float hw        = camera.orthographicSize * camera.aspect;
                        float hh        = camera.orthographicSize;
                        float chunkHalf = 10f; // CHUNK_SIZE / 2
                        float camX      = camera.transform.position.x;
                        float camY      = camera.transform.position.y;
                        float cx        = chunk.transform.position.x;
                        float cy        = chunk.transform.position.y;
                        visivel = cx + chunkHalf > camX - hw && cx - chunkHalf < camX + hw &&
                                  cy + chunkHalf > camY - hh && cy - chunkHalf < camY + hh;
                    }

                    sb.AppendLine($"  Chunk {kv.Key}: pos={chunk.transform.position:F0} props={propCount} nulos={nullProps} visivel={visivel}");
                }
            }
        }
        else sb.AppendLine("WORLDCHUNKMANAGER: NULL");

        var arena = SimpleArena.Instance;
        if (arena != null)
        {
            var floorField = typeof(SimpleArena).GetField("_floorRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var floor = floorField?.GetValue(arena) as SpriteRenderer;
            sb.AppendLine("CHÃO (SimpleArena)");
            sb.AppendLine($"  Posição: {floor?.transform.position:F0}");
            sb.AppendLine($"  Escala: {floor?.transform.localScale:F1}");
        }

        var allProps = Object.FindObjectsByType<EnvironmentProp>(FindObjectsSortMode.None);
        sb.AppendLine($"ENVIRONMENT PROPS NA CENA: {allProps.Length}");
        int shown = 0;
        foreach (var ep in allProps)
        {
            if (shown >= 5) break;
            var sr = ep.GetComponent<SpriteRenderer>();
            sb.AppendLine($"  {ep.gameObject.name} pos={ep.transform.position:F0} sprite={sr?.sprite?.name ?? "NULL"} enabled={sr?.enabled} sortOrder={sr?.sortingOrder}");
            shown++;
        }

        string report = sb.ToString();
        Debug.Log(report);

        string path = $"Assets/Logs/PosicoesDiagnostico_{System.DateTime.Now:HHmmss}.txt";
        System.IO.Directory.CreateDirectory("Assets/Logs");
        System.IO.File.WriteAllText(path, report);
        AssetDatabase.Refresh();
        Debug.Log($"[Debug] Log salvo em {path}");
    }

    static Vector2Int WorldToChunk(Vector3 pos, float chunkSize) =>
        new Vector2Int(
            Mathf.FloorToInt(pos.x / chunkSize),
            Mathf.FloorToInt(pos.y / chunkSize));

    // ── Seletor de zona inicial ──────────────────────────────────────────────────

    [MenuItem("Solengard/Debug/Ir para Zona 1")] static void GoToZone1() => SetTestZone(1);
    [MenuItem("Solengard/Debug/Ir para Zona 2")] static void GoToZone2() => SetTestZone(2);
    [MenuItem("Solengard/Debug/Ir para Zona 3")] static void GoToZone3() => SetTestZone(3);
    [MenuItem("Solengard/Debug/Ir para Zona 4")] static void GoToZone4() => SetTestZone(4);
    [MenuItem("Solengard/Debug/Ir para Zona 5")] static void GoToZone5() => SetTestZone(5);

    static void SetTestZone(int zone)
    {
        var zm = Object.FindFirstObjectByType<ZoneManager>();
        if (zm == null) { Debug.LogError("[Debug] ZoneManager não encontrado"); return; }
        zm.testStartZone = zone;

        if (Application.isPlaying)
        {
            // Em Play: aplica o VISUAL do bioma imediatamente (calibração).
            // Gameplay (inimigos/boss) continua na zona atual — pare e dê Play
            // para iniciar a gameplay na zona escolhida.
            BiomeSystem.Instance?.SetBiome((BiomeSystem.Biome)(zone - 1));
            WorldChunkManager.Instance?.SetBiome(zone - 1);
            Debug.Log($"[Debug] Visual do bioma {zone} aplicado AO VIVO. Para gameplay da zona, reinicie o Play.");
        }
        else
        {
            EditorUtility.SetDirty(zm); // sem isto o valor não persiste na cena
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(zm.gameObject.scene);
            Debug.Log($"[Debug] testStartZone = {zone} — dê Play para iniciar nessa zona");
        }
    }

    // Encontra o componente T na cena ativa; se estiver num GO com nome errado, move.
    static int FixSystem<T>(string expectedName, System.Text.StringBuilder log) where T : Component
    {
        var scene = SceneManager.GetActiveScene();
        T existing = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            existing = root.GetComponentInChildren<T>(true);
            if (existing != null) break;
        }

        if (existing == null)  return 0; // não existe na cena — nenhuma correção
        if (existing.gameObject.name == expectedName) return 0; // já no lugar certo

        // Está no lugar errado — criar novo GO e mover
        string oldName = existing.gameObject.name;
        var newGO = new GameObject(expectedName);
        Undo.RegisterCreatedObjectUndo(newGO, "Fix Solengard Systems");
        var newComp = newGO.AddComponent<T>();
        CopySerializedProperties(existing, newComp);
        Undo.DestroyObjectImmediate(existing); // remove só o componente, preserva o GO original

        string entry = $"  {typeof(T).Name}: '{oldName}' → '{expectedName}'";
        log.AppendLine(entry);
        Debug.Log($"[SolengardDebug] {entry}");
        return 1;
    }

    // Copia todos os campos serializados visíveis de src para dst (ignora m_Script).
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
}
