using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

public static class SolengardFontSetup
{
    const string FONT_PATH = "Assets/Art/Fonts/Straight pixel gothic.otf";
    const string TMP_FONT_PATH = "Assets/Art/Fonts/StraightPixelGothic_TMP.asset";

    [MenuItem("Solengard/Setup/Criar TMP Font Asset (Fonte Gótica)")]
    static void CreateTMPFontAsset()
    {
        // Verificar se a fonte OTF existe
        var font = AssetDatabase.LoadAssetAtPath<Font>(FONT_PATH);
        if (font == null)
        {
            Debug.LogError($"[FontSetup] Fonte não encontrada em: {FONT_PATH}");
            return;
        }

        Debug.Log($"[FontSetup] Fonte carregada: {font.name}");

        // Criar o TMP Font Asset via TMP_FontAsset
        var tmpFont = TMP_FontAsset.CreateFontAsset(font);
        if (tmpFont == null)
        {
            Debug.LogError("[FontSetup] Falha ao criar TMP Font Asset");
            return;
        }

        // Salvar o asset
        AssetDatabase.CreateAsset(tmpFont, TMP_FONT_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FontSetup] TMP Font Asset criado em: {TMP_FONT_PATH}");
        Debug.Log("[FontSetup] Próximo passo: Solengard → Setup → Aplicar Fonte Gótica na Lore");
    }

    [MenuItem("Solengard/Setup/Aplicar Fonte Gótica na Lore")]
    static void ApplyGothicFontToLore()
    {
        // Carregar o TMP font asset
        var tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TMP_FONT_PATH);
        if (tmpFont == null)
        {
            Debug.LogError($"[FontSetup] TMP Font não encontrado. Execute 'Criar TMP Font Asset' primeiro.");
            return;
        }

        // Encontrar LoreScreenUI na cena
        var loreUI = Object.FindFirstObjectByType<LoreScreenUI>();
        if (loreUI == null)
        {
            Debug.LogError("[FontSetup] LoreScreenUI não encontrado na cena. Execute Rebuild GameScene primeiro.");
            return;
        }

        // Aplicar fonte em todos os TextMeshProUGUI da LoreScreenUI
        var texts = loreUI.GetComponentsInChildren<TextMeshProUGUI>(true);
        int applied = 0;
        foreach (var text in texts)
        {
            text.font = tmpFont;

            // Ajustes visuais por tipo de texto
            if (text.gameObject.name.ToLower().Contains("bioma") ||
                text.gameObject.name.ToLower().Contains("nome"))
            {
                text.fontSize = 48f;
                text.color    = new Color(0.98f, 0.85f, 0.20f); // dourado
                text.fontStyle = FontStyles.Bold;
            }
            else if (text.gameObject.name.ToLower().Contains("instrucao") ||
                     text.gameObject.name.ToLower().Contains("instrução"))
            {
                text.fontSize = 18f;
                text.color    = new Color(0.75f, 0.75f, 0.75f); // cinza claro
            }
            else // textoLore
            {
                text.fontSize = 24f;
                text.color    = new Color(0.92f, 0.90f, 0.85f); // branco creme
            }

            applied++;
        }

        // Marcar como dirty para salvar
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[FontSetup] Fonte gótica aplicada em {applied} textos da LoreScreenUI");
        Debug.Log("[FontSetup] Salve a cena com Ctrl+S para preservar as mudanças");
    }

    [MenuItem("Solengard/Setup/Aplicar Fonte Gótica em Todos os Menus")]
    static void ApplyGothicFontEverywhere()
    {
        var tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TMP_FONT_PATH);
        if (tmpFont == null)
        {
            Debug.LogError("[FontSetup] TMP Font não encontrado. Execute 'Criar TMP Font Asset' primeiro.");
            return;
        }

        // Aplicar em todos os TextMeshProUGUI da cena
        var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        int applied = 0;
        foreach (var text in allTexts)
        {
            text.font = tmpFont;
            applied++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[FontSetup] Fonte gótica aplicada em {applied} textos na cena");
    }
}
