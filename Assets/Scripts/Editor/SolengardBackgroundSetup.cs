using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Troca o fundo de TODOS os paineis de menu (Painel*) por menu_background_Dark.png.
// NAO-destrutivo: so altera o Image raiz de cada painel; nao mexe em filhos nem na TopBar.
// A cena e a fonte da verdade — rode uma vez e salve. Os geradores (SolengardLayoutSetup
// e SolengardMissoesLegadoSetup) ja foram sincronizados para usar a mesma imagem.
public static class SolengardBackgroundSetup
{
    const string DARK_PATH = "Assets/Art/UI/Backgrounds/menu_background_Dark.png";

    [MenuItem("Solengard/UI: Trocar Fundo dos Paineis (Escuro)")]
    static void TrocarFundos()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado na cena ativa. Abra a MainMenu.", "OK");
            return;
        }

        var sprite = CarregarFundoEscuro();
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Solengard", $"Sprite nao carregou: {DARK_PATH}", "OK");
            return;
        }

        var log = new StringBuilder();
        int n = 0;
        // Todos os filhos diretos do Canvas cujo nome comeca com "Painel".
        // (TopBar NAO comeca com "Painel" -> nunca e tocada, preserva a sombra #0A0A1A.)
        foreach (Transform filho in canvas.transform)
        {
            if (!filho.name.StartsWith("Painel")) continue;
            var img = filho.GetComponent<Image>();
            if (img == null) img = filho.gameObject.AddComponent<Image>();
            img.sprite         = sprite;
            img.color          = Color.white;
            img.type           = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget  = true; // mantem o painel bloqueando cliques no fundo
            RemoverEscurecedor(filho); // limpa a camada antiga (fundo ja vem escuro)
            EditorUtility.SetDirty(filho.gameObject);
            log.AppendLine($"  {filho.name}");
            n++;
        }

        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Fundos] {n} painel(eis) com fundo escuro:\n{log}");
        EditorUtility.DisplayDialog("Solengard — Fundo Escuro",
            n > 0 ? $"{n} painel(eis) atualizado(s) (TopBar preservada). Salve a cena.\n\n{log}"
                  : "Nenhum painel 'Painel*' encontrado no Canvas.", "OK");
    }

    [MenuItem("Solengard/UI: Trocar Fundo dos Paineis (Escuro)", validate = true)]
    static bool TrocarFundosValidate() => GameObject.Find("Canvas") != null;

    // Remove a antiga camada 'Escurecedor'. Nao e mais necessaria: o fundo ja vem escuro
    // por imagem (menu_background_Dark.png). Idempotente — re-rodar os builders limpa os paineis.
    internal static void RemoverEscurecedor(Transform painel)
    {
        var t = painel.Find("Escurecedor");
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    // Forca import como Sprite Single (o asset vem como Multiple) e carrega.
    static Sprite CarregarFundoEscuro()
    {
        var imp = AssetImporter.GetAtPath(DARK_PATH) as TextureImporter;
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
        {
            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(DARK_PATH);
    }
}
