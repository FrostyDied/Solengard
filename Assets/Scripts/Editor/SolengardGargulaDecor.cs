using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Gargula decorativa no canto inferior direito de 4 telas de menu (Loja/Missoes/Legado/Config).
// NAO no Grimorio (fica preto limpo). Sobreposicao puramente decorativa.
//
// ── CALIBRAGEM (troque os valores e re-rode o menu) ──────────────────────────────────
//   LARGURA / ALTURA : caixa do sprite (preserveAspect=true encaixa sem distorcer)
//   OFFSET           : respiro do canto inf-dir (X negativo = p/ esquerda, Y positivo = p/ cima)
//   HEX_COR          : tint p/ escurecer levemente o sprite (#FFFFFF = original)
//
// ORDEM DE RENDER: GargulaDecor entra como PRIMEIRO filho (SetAsFirstSibling) -> fica
//   a FRENTE do fundo solido (que e o Image-componente do painel, renderiza antes dos
//   filhos) e ATRAS de todo conteudo (header, cards). BottomTabs/header continuam por cima.
//
// NAO-DESTRUTIVO / ADITIVO / IDEMPOTENTE / REVERSIVEL:
//   - Se "GargulaDecor" ja existe no painel, ATUALIZA (nao duplica).
//   - raycastTarget = false -> nao bloqueia toque.
//   - Reverter: 'git checkout -- Assets/Scenes/MainMenu.unity', ou apagar este arquivo.
//     A cena e a fonte da verdade — rode uma vez e salve.
public static class SolengardGargulaDecor
{
    // >>> Ajuste fino aqui <<<
    const float   LARGURA = 380f;
    const float   ALTURA  = 380f;
    static readonly Vector2 OFFSET = new Vector2(10f, 80f); // canto inf-dir (Pos X, Pos Y) — calibrado na cena
    const string  HEX_COR = "#B0B0B0";                       // cinza p/ escurecer um pouco

    const string GARGULA_PATH = "Assets/Art/UI/Backgrounds/gargula.png";
    static readonly string[] PAINEIS =
    {
        "PainelLoja",
        "PainelMissoes",
        "PainelLegado",
        "PainelConfiguracoes",
    };

    [MenuItem("Solengard/UI: Gargula Decorativa (4 telas)")]
    static void Aplicar()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado na cena ativa. Abra a MainMenu.", "OK");
            return;
        }

        var sprite = CarregarGargula();
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Solengard", $"Sprite nao carregou: {GARGULA_PATH}", "OK");
            return;
        }

        var cor = ColorUtility.TryParseHtmlString(HEX_COR, out var c) ? c : Color.white;

        var log = new StringBuilder();
        int n = 0;
        foreach (var nome in PAINEIS)
        {
            var painel = canvas.transform.Find(nome);
            if (painel == null) { log.AppendLine($"  [!] {nome} nao encontrado"); continue; }

            // Idempotente: reusa o GargulaDecor existente; so cria se faltar.
            var existente = painel.Find("GargulaDecor");
            bool novo = existente == null;
            var go = novo ? new GameObject("GargulaDecor", typeof(RectTransform)) : existente.gameObject;
            if (novo) go.transform.SetParent(painel, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);   // canto inferior direito
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(LARGURA, ALTURA);
            rt.anchoredPosition = OFFSET;

            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite         = sprite;
            img.color          = cor;
            img.preserveAspect = true;   // nao distorce a gargula
            img.raycastTarget  = false;  // nao bloqueia toque

            go.transform.SetAsFirstSibling(); // atras do conteudo, a frente do fundo solido
            EditorUtility.SetDirty(go);
            log.AppendLine($"  {nome}/GargulaDecor {(novo ? "(criado)" : "(atualizado)")}");
            n++;
        }

        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Gargula] {n} painel(eis):\n{log}");
        EditorUtility.DisplayDialog("Solengard — Gargula Decorativa",
            n > 0 ? $"{n} painel(eis) com GargulaDecor. Salve a cena (Ctrl+S).\n\n" +
                    $"Tam: {LARGURA}x{ALTURA}  Offset: {OFFSET.x},{OFFSET.y}  Cor: {HEX_COR}\n\n{log}"
                  : $"Nenhum alvo encontrado no Canvas.\n\n{log}", "OK");
    }

    [MenuItem("Solengard/UI: Gargula Decorativa (4 telas)", validate = true)]
    static bool AplicarValidate() => GameObject.Find("Canvas") != null;

    // Forca import como Sprite/Single (igual aos demais loaders do projeto).
    static Sprite CarregarGargula()
    {
        var imp = AssetImporter.GetAtPath(GARGULA_PATH) as TextureImporter;
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
        {
            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(GARGULA_PATH);
    }
}
