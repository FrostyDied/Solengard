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
        if (hud == null)
        {
            Debug.LogError("[UISetup] HUDComplete não encontrado. Execute Solengard → Rebuild GameScene primeiro.");
            return;
        }

        var hudCanvas = hud.gameObject;

        // Criar ou encontrar HUDBackground como filho do Canvas
        var bgTransform = hudCanvas.transform.Find("HUDBackground");
        if (bgTransform == null)
        {
            var bgGO = new GameObject("HUDBackground");
            bgGO.transform.SetParent(hudCanvas.transform, false);
            bgGO.transform.SetAsFirstSibling(); // atrás de tudo
            var rt = bgGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            bgTransform = bgGO.transform;
        }
        var bgImg = bgTransform.GetComponent<Image>()
                 ?? bgTransform.gameObject.AddComponent<Image>();
        bgImg.sprite = Load("hud_container.png");
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(1f, 1f, 1f, 0.85f); // leve transparência

        // Aplicar sprite na barra de vida se existir
        if (hud.barraVida != null)
        {
            // Background da barra (frame vazio)
            var bgBar = hud.barraVida.transform.Find("Background");
            if (bgBar != null)
            {
                var bgBarImg = bgBar.GetComponent<Image>();
                if (bgBarImg != null)
                {
                    bgBarImg.sprite = Load("bar_frame_1.png");
                    bgBarImg.type = Image.Type.Sliced;
                    bgBarImg.color = Color.white;
                }
            }

            // Fill da barra
            if (hud.barraVida.fillRect != null)
            {
                var fillImg = hud.barraVida.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.sprite = Load("bar_fill_1.png");
                    fillImg.type = Image.Type.Filled;
                    fillImg.color = new Color(0.2f, 0.85f, 0.3f); // verde vida
                }
            }
        }

        EditorUtility.SetDirty(hud);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[UISetup] HUD configurado — HUDBackground criado com Mobile Fantasy UI");
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
