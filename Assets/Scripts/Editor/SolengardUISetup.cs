using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class SolengardUISetup
{
    static string EXPORTED = "Assets/Art/UI/MobileFantasyUI/Exported/";

    static Sprite Load(string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(EXPORTED + name);

    [MenuItem("Solengard/UI/Setup HUD")]
    static void SetupHUD()
    {
        var hud = Object.FindFirstObjectByType<HUDComplete>();
        if (hud == null) { Debug.LogError("HUDComplete não encontrado"); return; }

        // Container principal do HUD
        var hudGO = hud.gameObject;
        var hudImg = hudGO.GetComponent<Image>() ?? hudGO.AddComponent<Image>();
        hudImg.sprite = Load("hud_container.png");
        hudImg.type = Image.Type.Sliced;

        // Barra de Vida — frame + fill
        if (hud.barraVida != null)
        {
            // Background da barra (frame)
            var bg = hud.barraVida.GetComponentInChildren<Image>();
            if (bg != null) bg.sprite = Load("bar_frame_1.png");

            // Fill da barra
            var fill = hud.barraVida.fillRect?.GetComponent<Image>();
            if (fill != null)
            {
                fill.sprite = Load("bar_fill_1.png");
                fill.type = Image.Type.Filled;
                fill.color = new Color(0.2f, 0.8f, 0.3f); // verde para vida
            }
        }

        EditorUtility.SetDirty(hud);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[UISetup] HUD configurado com Mobile Fantasy UI");
    }

    [MenuItem("Solengard/UI/Setup LevelUp")]
    static void SetupLevelUp()
    {
        var levelUpUI = Object.FindFirstObjectByType<LevelUpUI>();
        if (levelUpUI == null) { Debug.LogError("LevelUpUI não encontrado"); return; }

        // Painel principal
        var panel = levelUpUI.transform.Find("panel") ??
                    levelUpUI.transform.GetChild(0);
        if (panel != null)
        {
            var panelImg = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            panelImg.sprite = Load("complete_container.png");
            panelImg.type = Image.Type.Sliced;

            // Frame sobre o container
            var frameGO = new GameObject("Frame");
            frameGO.transform.SetParent(panel, false);
            var frameRT = frameGO.AddComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = frameRT.offsetMax = Vector2.zero;
            var frameImg = frameGO.AddComponent<Image>();
            frameImg.sprite = Load("complete_frame.png");
            frameImg.type = Image.Type.Sliced;
        }

        // Botões dos cards de upgrade
        var buttons = levelUpUI.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            var btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.sprite = Load("menu_button.png");
                btnImg.type = Image.Type.Sliced;
            }

            // Estado pressionado
            var spriteState = btn.spriteState;
            spriteState.pressedSprite = Load("menu_button_pressed.png");
            btn.spriteState = spriteState;
            btn.transition = Selectable.Transition.SpriteSwap;
        }

        EditorUtility.SetDirty(levelUpUI);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[UISetup] LevelUp configurado com Mobile Fantasy UI");
    }
}
