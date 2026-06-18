using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Fundo dos paineis de menu:
//   - Loja / Missoes / Legado / Config -> SPRITE menu_background_slate.png (slate blue leve c/ borda).
//   - Grimorio (atras do livro)         -> COR SOLIDA preta (destaca o grimorio; sem sprite).
//
// ── CALIBRAGEM (troque e re-rode o menu) ─────────────────────────────────────────────
//   SLATE_PATH    : arte de fundo dos 4 paineis
//   HEX_GRIMORIO  : preto/quase-preto atras do livro
//   HEX_FALLBACK  : cor solida usada nos 4 paineis SE o sprite nao carregar
//
// NAO-DESTRUTIVO / ADITIVO / IDEMPOTENTE / REVERSIVEL:
//   - So altera o Image RAIZ de cada painel (GetComponent, nao recursa em filhos):
//     o GargulaDecor (filho, indice 0), o pergaminho PageBackground e os Headers ficam intactos.
//   - NAO reordena siblings -> ordem de render preservada:
//       fundo (raiz) -> GargulaDecor -> conteudo/cards -> header/nav.
//   - Reverter: 'git checkout -- Assets/Scenes/MainMenu.unity', ou apagar este arquivo.
//     A cena e a fonte da verdade — rode uma vez e salve.
public static class SolengardFundoLiso
{
    // >>> Ajuste fino aqui <<<
    const string SLATE_PATH   = "Assets/Art/UI/Backgrounds/menu_background_slate.png";
    const string HEX_GRIMORIO = "#0A0A0D"; // atras do livro (Grimorio)
    const string HEX_FALLBACK = "#14171F"; // cor solida nos 4 paineis se o sprite faltar

    // Escopo fechado. Cada nome e um filho direto do Canvas.
    static readonly string[] PAINEIS =
    {
        "PainelLoja",
        "PainelMissoes",
        "PainelLegado",
        "PainelConfiguracoes",
    };
    const string PAINEL_GRIMORIO = "PainelUpgradesGrimorio"; // raiz = "atras do livro"

    [MenuItem("Solengard/UI: Fundo dos Paineis (Slate + Grimorio preto)")]
    static void AplicarFundo()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado na cena ativa. Abra a MainMenu.", "OK");
            return;
        }

        var slate    = CarregarSlate();
        var fallback  = ParseHex(HEX_FALLBACK, new Color(0.078f, 0.090f, 0.122f, 1f));
        var corGrim   = ParseHex(HEX_GRIMORIO, new Color(0.039f, 0.039f, 0.051f, 1f));

        var log = new StringBuilder();
        int n = 0;

        // 4 paineis -> sprite slate (ou cor de fallback se o asset sumir).
        foreach (var nome in PAINEIS)
            if (AplicarRaiz(canvas.transform, nome, slate, fallback, log)) n++;

        // Grimorio -> preto solido (sem sprite).
        if (AplicarRaiz(canvas.transform, PAINEL_GRIMORIO, null, corGrim, log)) n++;

        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Fundo] {n} painel(eis):\n{log}");
        EditorUtility.DisplayDialog("Solengard — Fundo dos Paineis",
            n > 0 ? $"{n} painel(eis) atualizado(s). Salve a cena (Ctrl+S).\n\n" +
                    $"4 paineis: {(slate != null ? "sprite slate" : "FALLBACK " + HEX_FALLBACK)}\n" +
                    $"Grimorio:  {HEX_GRIMORIO} (preto)\n\n{log}"
                  : $"Nenhum alvo encontrado no Canvas.\n\n{log}", "OK");
    }

    [MenuItem("Solengard/UI: Fundo dos Paineis (Slate + Grimorio preto)", validate = true)]
    static bool AplicarFundoValidate() => GameObject.Find("Canvas") != null;

    // Aplica no Image RAIZ do painel (nao recursa em filhos -> GargulaDecor/Header intactos).
    // sprite != null -> usa o sprite (color=white); sprite == null -> cor solida pura.
    static bool AplicarRaiz(Transform canvas, string nome, Sprite sprite, Color cor, StringBuilder log)
    {
        var alvo = canvas.Find(nome);
        if (alvo == null) { log.AppendLine($"  [!] {nome} nao encontrado"); return false; }

        var img = alvo.GetComponent<Image>() ?? alvo.gameObject.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite         = sprite;
            img.color          = Color.white;
            img.type           = Image.Type.Simple;
            img.preserveAspect = false; // estica p/ preencher
            log.AppendLine($"  {nome}  (sprite slate)");
        }
        else
        {
            img.sprite = null;          // cor solida pura
            img.color  = cor;
            img.type   = Image.Type.Simple;
            log.AppendLine($"  {nome}  ({ColorUtility.ToHtmlStringRGB(cor)})");
        }
        img.raycastTarget = true;       // painel continua bloqueando cliques no fundo
        EditorUtility.SetDirty(alvo.gameObject);
        return true;
    }

    // Forca import como Sprite/Single (igual aos demais loaders do projeto).
    static Sprite CarregarSlate()
    {
        var imp = AssetImporter.GetAtPath(SLATE_PATH) as TextureImporter;
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
        {
            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SLATE_PATH);
    }

    static Color ParseHex(string hex, Color fallback)
        => ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
}
