using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

// Temporário — diagnóstico de cena para investigar GameManager missing.
// Remover após resolver o problema.
public static class SolengardDebug
{
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

        // 5. activeScene.GetRootGameObjects() — GameManager presente?
        sb.AppendLine($"\n[5] activeScene ('{activeScene.name}') root GOs com GameManager:");
        var activeRoots = activeScene.GetRootGameObjects();
        bool foundInActive = false;
        foreach (var go in activeRoots)
        {
            var gm = go.GetComponentInChildren<GameManager>(true);
            if (gm != null)
            {
                sb.AppendLine($"  → ENCONTRADO: '{go.name}' tem GameManager (active={go.activeSelf})");
                Debug.Log($"[SolengardDebug] GameManager encontrado na cena ativa: '{go.name}'");
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
}
