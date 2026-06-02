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
