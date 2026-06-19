using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Redesign das 3 abas do topo da Loja (Personagens/Upgrades/Diamantes) com a placa tab_loja.png
// (arte ja na proporcao da aba -> Simple, sem 9-slice). NAO-DESTRUTIVO / IDEMPOTENTE / REVERSIVEL.
//  - Cada botao (BtnPersonagens/BtnUpgrades/BtnDiamantes): Image sprite=tab_loja, Type=Simple,
//    preserveAspect=false, color=white, e Button.transition=None (ColorTint nao briga c/ o highlight).
//  - Container AbasLoja: bg roxo -> transparente (nao competir com a placa).
//  - Wiring/labels intactos. O highlight de aba ativa vive no LojaController (alpha).
public static class SolengardTabsLojaSetup
{
    const string TAB_PATH = "Assets/Art/UI/Tabs/tab_loja.png";
    static readonly string[] BOTOES = { "BtnPersonagens", "BtnUpgrades", "BtnDiamantes" };

    [MenuItem("Solengard/UI: Tabs da Loja (placa)")]
    static void Construir()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado. Abra a MainMenu.", "OK"); return; }

        var sprite = CarregarSprite(TAB_PATH);
        if (sprite == null) { EditorUtility.DisplayDialog("Solengard", $"Sprite nao carregou: {TAB_PATH}", "OK"); return; }

        var log = new StringBuilder();
        int n = 0;
        foreach (var nome in BOTOES)
        {
            var t = AcharProfundo(canvas.transform, nome);
            if (t == null) { log.AppendLine($"  [!] {nome} nao encontrado"); continue; }

            var img = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
            img.sprite         = sprite;
            img.type           = Image.Type.Simple;  // placa ja esta na proporcao da aba (sem 9-slice)
            img.preserveAspect = false;               // estica p/ preencher a aba
            img.color          = Color.white;          // alpha controlado pelo highlight em runtime
            img.raycastTarget  = true;

            var btn = t.GetComponent<Button>();
            if (btn != null) btn.transition = Selectable.Transition.None; // highlight por alpha (sem ColorTint)

            EditorUtility.SetDirty(t.gameObject);
            log.AppendLine($"  {nome}: tab_loja (Simple), transition=None");
            n++;
        }

        // Container AbasLoja -> transparente (nao compete com a placa).
        var abas = AcharProfundo(canvas.transform, "AbasLoja");
        if (abas != null)
        {
            var bi = abas.GetComponent<Image>();
            if (bi != null) { bi.color = new Color(0f, 0f, 0f, 0f); bi.raycastTarget = false; EditorUtility.SetDirty(abas.gameObject); log.AppendLine("  AbasLoja: bg transparente"); }
        }

        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[TabsLoja] {n} aba(s) com placa:\n{log}");
        EditorUtility.DisplayDialog("Solengard — Tabs da Loja",
            $"{n} aba(s) atualizada(s). Salve a cena (Ctrl+S).\n\n{log}", "OK");
    }

    [MenuItem("Solengard/UI: Tabs da Loja (placa)", validate = true)]
    static bool ConstruirValidate() => GameObject.Find("Canvas") != null;

    static Transform AcharProfundo(Transform raiz, string nome)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nome) return t;
        return null;
    }

    // Carrega o sprite. So forca Sprite/Single se ainda nao estiver (preserva os borders 9-slice
    // que voce ja marcou no Sprite Editor).
    static Sprite CarregarSprite(string path)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogWarning($"[TabsLoja] asset ausente: {path}"); return null; }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
