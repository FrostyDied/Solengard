using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Torna a BottomTabs uma barra de navegacao PERSISTENTE dentro dos paineis:
//  - SetAsLastSibling -> renderiza POR CIMA dos paineis full-screen e recebe cliques.
//  - sempre ativa -> 1 toque troca de secao de qualquer tela (a navegacao ja existia).
//  - liga as 4 Images das abas no MainMenuManager (highlight da secao ativa via HighlightNav).
//  - sobe o TextoFeedback da Loja p/ nao ficar atras da barra.
// NAO-destrutivo / idempotente. Rode DEPOIS do builder de Missoes+Legado (p/ a barra
// terminar por cima do PainelLegado recem-criado).
public static class SolengardNavSetup
{
    [MenuItem("Solengard/Nav: Barra Inferior Persistente")]
    static void Construir()
    {
        var canvas = GameObject.Find("Canvas");
        var bottom = canvas != null ? canvas.transform.Find("BottomTabs") : null;
        if (bottom == null) { EditorUtility.DisplayDialog("Solengard", "Canvas/BottomTabs nao encontrado. Abra a MainMenu.", "OK"); return; }

        var log = new StringBuilder();

        // 1) Barra por cima de todos os paineis + sempre ativa/clicavel.
        bottom.SetAsLastSibling();
        bottom.gameObject.SetActive(true);
        log.AppendLine("  BottomTabs -> ultimo sibling (por cima) + ativo");

        // 2) Liga as 4 Images das abas no MainMenuManager (highlight).
        var imgLoja = ImgDe(bottom, "TabLoja");
        var imgMiss = ImgDe(bottom, "TabMissoes");
        var imgUpg  = ImgDe(bottom, "TabPasse");                         // label "UPGRADES"
        var imgLeg  = ImgDe(bottom, "TabLegado") ?? ImgDe(bottom, "TabConfigs"); // GameObject ainda chama TabConfigs

        var mmm = Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (mmm != null)
        {
            var so = new SerializedObject(mmm);
            Ligar(so, "tabLojaImg",     imgLoja, "TabLoja",          log);
            Ligar(so, "tabMissoesImg",  imgMiss, "TabMissoes",       log);
            Ligar(so, "tabUpgradesImg", imgUpg,  "TabPasse",         log);
            Ligar(so, "tabLegadoImg",   imgLeg,  "TabLegado/Configs", log);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(mmm);
        }
        else log.AppendLine("  [!] MainMenuManager nao encontrado");

        // 3) Sobe o TextoFeedback da Loja (estava em y30, atras da barra) p/ y170.
        var fb = canvas.transform.Find("PainelLoja/TextoFeedback");
        if (fb != null)
        {
            var rt = fb.GetComponent<RectTransform>();
            if (rt != null) { rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 170f); log.AppendLine("  PainelLoja/TextoFeedback -> y170"); }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Nav] Barra persistente:\n{log}");
        EditorUtility.DisplayDialog("Solengard — Nav Persistente",
            $"BottomTabs persistente (por cima dos paineis) + Images ligadas. Salve a cena.\n\n{log}", "OK");
    }

    [MenuItem("Solengard/Nav: Barra Inferior Persistente", validate = true)]
    static bool ConstruirValidate() => GameObject.Find("Canvas") != null;

    static Image ImgDe(Transform bottom, string nome)
    {
        var t = bottom.Find(nome);
        return t != null ? t.GetComponent<Image>() : null;
    }

    static void Ligar(SerializedObject so, string campo, Image img, string desc, StringBuilder log)
    {
        var p = so.FindProperty(campo);
        if (p == null) { log.AppendLine($"  [!] campo '{campo}' ausente no MainMenuManager"); return; }
        p.objectReferenceValue = img;
        log.AppendLine(img != null ? $"  {campo} -> {desc}" : $"  [!] Image '{desc}' nao encontrada p/ {campo}");
    }
}
