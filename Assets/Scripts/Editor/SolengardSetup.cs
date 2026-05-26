using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;

// Menu: Solengard ▸ Setup Scene
// Atribui automaticamente os ScriptableObject assets (GameConfig, PlayerData) a todos os
// componentes da cena que ainda tenham os campos nulos. Nunca sobrescreve valores existentes.
public static class SolengardSetup
{
    const string GAME_CONFIG_PATH = "Assets/Data/GameConfig.asset";
    const string PLAYER_DATA_PATH = "Assets/Data/PlayerData.asset";
    const string EXPECTED_SCENE   = "GameScene";

    [MenuItem("Solengard/Setup Scene")]
    static void SetupScene()
    {
        // ── 1. Verificar cena ───────────────────────────────────────────────────

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != EXPECTED_SCENE)
        {
            EditorUtility.DisplayDialog(
                "Solengard Setup",
                $"Abra a cena '{EXPECTED_SCENE}' antes de executar o Setup.\n\n" +
                $"Cena atual: '{activeScene.name}'",
                "OK");
            return;
        }

        // ── 2. Carregar assets ──────────────────────────────────────────────────

        var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GAME_CONFIG_PATH);
        if (gameConfig == null)
        {
            EditorUtility.DisplayDialog("Solengard Setup",
                $"Asset não encontrado:\n{GAME_CONFIG_PATH}\n\n" +
                "Crie o asset via menu Assets → Create → Solengard → GameConfig.",
                "OK");
            return;
        }

        var playerData = AssetDatabase.LoadAssetAtPath<PlayerData>(PLAYER_DATA_PATH);
        if (playerData == null)
        {
            EditorUtility.DisplayDialog("Solengard Setup",
                $"Asset não encontrado:\n{PLAYER_DATA_PATH}\n\n" +
                "Crie o asset via menu Assets → Create → Solengard → PlayerData.",
                "OK");
            return;
        }

        // ── 3. Atribuir campos (somente se null) ────────────────────────────────

        int total = 0;
        var log   = new StringBuilder();

        // WaveManager.gameConfig
        total += TryAssign<WaveManager>("gameConfig", gameConfig, log);

        // DiamondSystem.playerData
        total += TryAssign<DiamondSystem>("playerData", playerData, log);

        // ScoreSystem.playerData
        total += TryAssign<ScoreSystem>("playerData", playerData, log);

        // SeasonPassSystem.playerData
        total += TryAssign<SeasonPassSystem>("playerData", playerData, log);

        // ── 4. Marcar cena como modificada ─────────────────────────────────────

        if (total > 0)
            EditorSceneManager.MarkSceneDirty(activeScene);

        // ── 5. Relatório ────────────────────────────────────────────────────────

        var sb = new StringBuilder();

        if (total > 0)
            sb.AppendLine($"✓ {total} atribuição(ões) realizada(s):\n{log}");
        else
            sb.AppendLine("Nenhuma atribuição necessária.\nTodos os campos já estavam preenchidos.\n");

        sb.AppendLine("─────────────────────────────────────");
        sb.AppendLine("Campos que requerem atribuição MANUAL no Inspector:");
        sb.AppendLine("• GameManager → waveManager");
        sb.AppendLine("  (arraste o GameObject WaveManager da hierarquia)");
        sb.AppendLine();
        sb.AppendLine("• GameManager → proceduralArena");
        sb.AppendLine("  (arraste o GameObject ProceduralArenaSystem da hierarquia)");

        EditorUtility.DisplayDialog("Solengard Setup — Concluído", sb.ToString(), "OK");
    }

    // Valida se o item de menu deve estar habilitado (habilita apenas com uma cena aberta)
    [MenuItem("Solengard/Setup Scene", validate = true)]
    static bool ValidateSetupScene()
    {
        return !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().name);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    // Encontra o primeiro componente do tipo T na cena (incluindo inativos),
    // lê a propriedade indicada e atribui o valor somente se ela estiver null.
    // Retorna 1 se atribuiu, 0 caso contrário.
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

        if (prop.objectReferenceValue != null)
            return 0; // já preenchido — não sobrescrever

        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();

        string msg = $"  {typeof(T).Name}.{propertyName} → {value.name}";
        log.AppendLine(msg);
        Debug.Log($"[SolengardSetup] {msg}");
        return 1;
    }
}
