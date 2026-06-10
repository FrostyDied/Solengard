using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SolengardGUIProSetup : EditorWindow
{
    static readonly string PREFABS_PATH = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_DemoScene_Panels/";

    // Paleta sombria Solengard
    static readonly Color COL_BG         = HexCol("#0A0A1A");
    static readonly Color COL_PANEL      = HexCol("#1A0A2E");
    static readonly Color COL_BTN_PRI    = HexCol("#5A1090");
    static readonly Color COL_BTN_SEC    = HexCol("#2A1060");
    static readonly Color COL_ACCENT     = HexCol("#FFD700");
    static readonly Color COL_TEXT       = Color.white;
    static readonly Color COL_TEXT_SEC   = HexCol("#C8A0FF");
    static readonly Color COL_DARK       = HexCol("#0D0D1F");

    [MenuItem("Solengard/GUI Pro/Setup Menu Principal")]
    static void SetupMenuPrincipal()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.name.Contains("MainMenu"))
        {
            EditorUtility.DisplayDialog("Erro", "Abra a MainMenu scene primeiro!", "OK");
            return;
        }

        // Carregar e instanciar prefab Home
        var homePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_PATH + "Home.prefab");
        if (homePrefab == null) { Debug.LogError("Home.prefab não encontrado!"); return; }

        // Destruir Canvas existente se houver
        var oldCanvas = GameObject.Find("Canvas");
        if (oldCanvas != null) Undo.DestroyObjectImmediate(oldCanvas);

        var homeGO = (GameObject)PrefabUtility.InstantiatePrefab(homePrefab);
        Undo.RegisterCreatedObjectUndo(homeGO, "Setup GUI Pro Home");
        homeGO.name = "Canvas";

        // Aplicar paleta sombria
        ApplyDarkPalette(homeGO);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[SolengardGUIProSetup] Home instanciado e recolorido!");
    }

    [MenuItem("Solengard/GUI Pro/Setup Game Over")]
    static void SetupGameOver()
    {
        InstanciarPrefab("PlayContinue", "GameOverCanvas");
    }

    [MenuItem("Solengard/GUI Pro/Setup Pause")]
    static void SetupPause()
    {
        InstanciarPrefab("PlayPause", "PauseCanvas");
    }

    [MenuItem("Solengard/GUI Pro/Setup LevelUp")]
    static void SetupLevelUp()
    {
        InstanciarPrefab("LevelUp", "LevelUpCanvas");
    }

    [MenuItem("Solengard/GUI Pro/Setup Boss Warning")]
    static void SetupBossWarning()
    {
        InstanciarPrefab("PlayBoss", "BossWarningCanvas");
    }

    [MenuItem("Solengard/GUI Pro/Setup Character Select")]
    static void SetupCharacterSelect()
    {
        InstanciarPrefab("CharacterSelect", "CharacterSelectCanvas");
    }

    [MenuItem("Solengard/GUI Pro/Recolorir Selecionado")]
    static void RecolorirSelecionado()
    {
        if (Selection.activeGameObject == null) return;
        ApplyDarkPalette(Selection.activeGameObject);
        Debug.Log($"[SolengardGUIProSetup] Paleta aplicada em {Selection.activeGameObject.name}");
    }

    static void InstanciarPrefab(string prefabName, string goName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_PATH + prefabName + ".prefab");
        if (prefab == null) { Debug.LogError($"{prefabName}.prefab não encontrado!"); return; }

        var old = GameObject.Find(goName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(go, $"Setup {goName}");
        go.name = goName;
        ApplyDarkPalette(go);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[SolengardGUIProSetup] {goName} instanciado!");
    }

    static void ApplyDarkPalette(GameObject root)
    {
        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            var name = img.gameObject.name.ToLower();

            // Preserva imagens com sprite artístico (backgrounds, ícones, ilustrações)
            if (img.sprite != null && !name.Contains("fill") && !name.Contains("bg_dark") && !name.Contains("overlay"))
                continue;

            // Só recolore imagens sem sprite (cores sólidas de UI)
            if (name.Contains("background") || name.Contains("back") || name.Contains("light"))
                continue; // preserva backgrounds artísticos
            else if (name.Contains("panel") || name.Contains("frame") || name.Contains("popup") || name.Contains("window"))
                img.color = COL_PANEL;
            else if (name.Contains("btn_") || name.Contains("button_"))
            {
                if (name.Contains("play") || name.Contains("start") || name.Contains("primary") || name.Contains("confirm"))
                    img.color = COL_BTN_PRI;
                else
                    img.color = COL_BTN_SEC;
            }
            else if (name.Contains("fill"))
                img.color = COL_BTN_PRI;
            else if (name.Contains("homemenu") || name.Contains("menubar") || name.Contains("navbar") || name.Contains("tabbar"))
                img.color = COL_DARK;
            else if (name.Contains("resourcebar") || name.Contains("topbar"))
                img.color = new Color(0f, 0f, 0f, 0.7f);
        }

        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            var name = tmp.gameObject.name.ToLower();
            if (name.Contains("coin") || name.Contains("gem") || name.Contains("gold") || name.Contains("price"))
                tmp.color = COL_ACCENT;
            else if (name.Contains("sub") || name.Contains("desc") || name.Contains("info") || name.Contains("label"))
                tmp.color = COL_TEXT_SEC;
            else
                tmp.color = COL_TEXT;
        }
    }

    static Color HexCol(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
