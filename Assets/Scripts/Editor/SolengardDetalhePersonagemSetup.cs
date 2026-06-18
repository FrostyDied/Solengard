using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Solengard.UI;

// Constroi o PainelDetalhePersonagem (overlay sobre a Loja) + transforma os 5 cards em botoes
// AbrirDetalhe + religa MainMenuManager.painelDetalhe. NAO-DESTRUTIVO / IDEMPOTENTE / REVERSIVEL.
//
// Estrutura: painel (backdrop preto p/ letterbox) -> Moldura (AspectRatioFitter na proporcao
// da arte; detalhe_bg) que contem TODO o conteudo alinhado as 2 areas da arte:
//   area SUPERIOR: Nome/Classe (topo) + Splash;  area INFERIOR: Stats + Especial + BtnAcao.
// X = skin canonico SEM BotaoFecharPainel (o binder fia p/ fechar SO o detalhe via
// DetalhePersonagemUI.Fechar). O binder popula tudo no Mostrar(classId).
//
// Pronto pra controle: BtnAcao e X sao Selectable com Navigation Explicit entre si; o binder
// define o foco inicial (SetSelectedGameObject = BtnAcao) ao abrir.
public static class SolengardDetalhePersonagemSetup
{
    const string CHARS_DIR = "Assets/Art/UI/Characters/";
    const string BG_PATH   = "Assets/Art/UI/Detalhe/detalhe_bg.png";
    const float  ART_W = 1024f, ART_H = 1536f; // proporcao da moldura (AspectRatioFitter)
    const float  MOLDURA_TOP = 0.92f;  // clearance do header (topo)
    const float  MOLDURA_BOT = 0.075f; // clearance da BottomTabs (base)

    const string BTN_PATH  = "Assets/Art/UI/Buttons/btn_compra.png";
    const bool   BTN_ACAO_SLICED = false; // true depois de marcar borders na placa (9-slice)
    const float  BTN_LARGURA     = 180f;  // largura alvo (altura = largura / proporcao REAL da placa)
    const float  BTN_LARGURA_MAX = 880f;  // teto (caso aumente BTN_LARGURA)
    const float  BTN_ANCHOR_Y    = 0.088f;// centro do botao na fracao vertical da Moldura (area inferior)

    static readonly Color BACKDROP   = new Color(0.039f, 0.039f, 0.051f, 1f); // #0A0A0D
    static readonly Color SHEET      = new Color(0f, 0f, 0f, 0.62f);          // bottom-sheet translucida
    static readonly Color OURO       = new Color(0.78f, 0.65f, 0.20f);
    static readonly Color COR_NOME   = new Color(0.96f, 0.92f, 1f);
    static readonly Color COR_CLASSE = new Color(0.72f, 0.68f, 0.82f);
    static readonly Color COR_LABEL  = new Color(0.62f, 0.60f, 0.70f);
    static readonly Color BTN_OK     = new Color(0.35f, 0.06f, 0.60f);

    // classId -> arquivo do splash (mesmos PNGs dos cards).
    static readonly (string id, string file)[] SPLASHES =
    {
        ("mage", "seraphine.png"), ("assassin", "vael.png"), ("necromancer", "marveth.png"),
        ("paladin", "aldric.png"), ("hunter", "rynn.png"),
    };

    // Stats exibidos (ordem + label). O VALOR (Val_*) e populado pelo binder em runtime.
    static readonly (string val, string label)[] STATS =
    {
        ("Val_Vida", "Vida"), ("Val_Dano", "Dano"), ("Val_Defesa", "Defesa"),
        ("Val_Velocidade", "Velocidade"), ("Val_Alcance", "Alcance"),
        ("Val_Cadencia", "Cadência"), ("Val_Alvo", "Alvo"),
    };

    [MenuItem("Solengard/UI: Detalhe Personagem (painel + cards)")]
    static void Construir()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado. Abra a MainMenu.", "OK"); return; }
        var ct = canvas.transform;
        var log = new StringBuilder();

        // ── Painel raiz (full-screen, inativo) = backdrop preto (letterbox da moldura) ──
        var painel = FindOrCreate(ct, "PainelDetalhePersonagem");
        Stretch(painel);
        var bg = Img(painel, BACKDROP, true);
        bg.sprite = null;

        // ── Moldura: AspectRatioFitter na proporcao da arte -> ocupa exatamente a area
        //    renderizada da arte (com letterbox) e o conteudo (filhos) alinha as 2 areas
        //    em qualquer aparelho. detalhe_bg preenche a Moldura sem distorcao. ──
        var moldura = FindOrCreate(painel.transform, "Moldura");
        // Faixa entre header (topo) e BottomTabs (base): vertical-stretch + largura derivada da
        // altura pela proporcao da arte (HeightControlsWidth) -> arte nao fica sob header/nav.
        var mrt = (RectTransform)moldura.transform;
        mrt.anchorMin = new Vector2(0.5f, MOLDURA_BOT); mrt.anchorMax = new Vector2(0.5f, MOLDURA_TOP);
        mrt.pivot = new Vector2(0.5f, 0.5f); mrt.sizeDelta = Vector2.zero; mrt.anchoredPosition = Vector2.zero;
        var molImg = moldura.GetComponent<Image>() ?? moldura.AddComponent<Image>();
        var bgArt = CarregarSprite(BG_PATH);
        if (bgArt != null) { molImg.sprite = bgArt; molImg.color = Color.white; molImg.type = Image.Type.Simple; molImg.preserveAspect = false; }
        else molImg.color = BACKDROP;
        molImg.raycastTarget = true;
        var arf = moldura.GetComponent<AspectRatioFitter>() ?? moldura.AddComponent<AspectRatioFitter>();
        arf.aspectMode  = AspectRatioFitter.AspectMode.HeightControlsWidth;
        arf.aspectRatio = ART_W / ART_H;

        // Reparent do conteudo existente p/ dentro da Moldura (idempotente: 1a vez move de
        // painel/BottomSheet; nas seguintes ja esta na Moldura).
        foreach (var n in new[] { "Nome", "Classe", "Splash", "Stats", "Especial", "BtnAcao" })
        {
            var t = AcharProfundo(painel.transform, n);
            if (t != null && t.parent != moldura.transform) t.SetParent(moldura.transform, false);
        }
        var oldSheet = painel.transform.Find("BottomSheet"); // nao usado mais (a arte e o painel)
        if (oldSheet != null) Object.DestroyImmediate(oldSheet.gameObject);

        // ── Nome + Classe (topo, area SUPERIOR) ──
        var nome = Txt(moldura.transform, "Nome", "Nome", 48f, COR_NOME, TextAlignmentOptions.Center);
        nome.fontStyle = FontStyles.Bold; Anchor(nome.gameObject, 0.10f, 0.865f, 0.90f, 0.93f);
        var classe = Txt(moldura.transform, "Classe", "Classe", 28f, COR_CLASSE, TextAlignmentOptions.Center);
        Anchor(classe.gameObject, 0.10f, 0.815f, 0.90f, 0.865f);

        // ── Splash (area SUPERIOR, acima da divisoria; preserveAspect) ──
        var splash = FindOrCreate(moldura.transform, "Splash");
        Anchor(splash, 0.10f, 0.39f, 0.90f, 0.815f);
        var splImg = splash.GetComponent<Image>() ?? splash.AddComponent<Image>();
        splImg.preserveAspect = true; splImg.raycastTarget = false; splImg.color = Color.white;

        // ── Stats (area INFERIOR, abaixo da divisoria) — grid 2 col x 4 linhas ──
        var stats = FindOrCreate(moldura.transform, "Stats");
        Anchor(stats, 0.10f, 0.17f, 0.90f, 0.305f);
        { var i = stats.GetComponent<Image>(); if (i != null) Object.DestroyImmediate(i); }
        for (int k = 0; k < STATS.Length; k++)
            CelulaStat(stats.transform, STATS[k].val, STATS[k].label, k % 2, k / 2);

        // ── Especial (inferior, sob os stats) ──
        var esp = Txt(moldura.transform, "Especial", "Especial", 22f, OURO, TextAlignmentOptions.Center);
        Anchor(esp.gameObject, 0.10f, 0.125f, 0.90f, 0.165f);

        // ── BtnAcao: PROPORCAO TRAVADA (sizeDelta fixo na proporcao real da placa), reposicionado
        //    na area inferior por anchor fracionario (NAO usa anchor esticado -> nao espreme). ──
        var btnAcao = FindOrCreate(moldura.transform, "BtnAcao");
        var btnImg = btnAcao.GetComponent<Image>() ?? btnAcao.AddComponent<Image>();
        var btnSp = CarregarSprite(BTN_PATH);
        if (btnSp != null)
        {
            btnImg.sprite = btnSp; btnImg.color = Color.white;
            btnImg.type = BTN_ACAO_SLICED ? Image.Type.Sliced : Image.Type.Simple;
            btnImg.preserveAspect = false; // rect ja tem a proporcao da placa -> sem distorcao
        }
        else { btnImg.sprite = null; btnImg.color = BTN_OK; }
        btnImg.raycastTarget = true;

        float btnAspect = (btnSp != null && btnSp.rect.height > 0f) ? btnSp.rect.width / btnSp.rect.height : 1.5f;
        float btnW = BTN_LARGURA, btnH = btnW / btnAspect;
        if (btnW > BTN_LARGURA_MAX) { btnW = BTN_LARGURA_MAX; btnH = btnW / btnAspect; }
        var brt = (RectTransform)btnAcao.transform;
        brt.anchorMin = new Vector2(0.5f, BTN_ANCHOR_Y); brt.anchorMax = new Vector2(0.5f, BTN_ANCHOR_Y); brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(btnW, btnH);
        brt.anchoredPosition = Vector2.zero;

        var btnAcaoBtn = btnAcao.GetComponent<Button>() ?? btnAcao.AddComponent<Button>();
        btnAcaoBtn.targetGraphic = btnImg;
        var lbl = Txt(btnAcao.transform, "Label", "Comprar", 28f, Color.white, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold; Anchor(lbl.gameObject, 0.04f, 0f, 0.96f, 1f);

        // ── Header padrao (#1A0A2E) reusando o mesmo das outras telas (titulo + saldo) ──
        SolengardMissoesLegadoSetup.HeaderEstiloLoja(painel.transform, "PERSONAGENS");

        // ── X canonico no canto sup-dir (sobre o header), fora da Moldura, SEM BotaoFecharPainel ──
        // Reparent se veio da Moldura (versao anterior); o binder fia o clique -> Fechar().
        var xExist = AcharProfundo(painel.transform, "BtnFecharDetalhe");
        if (xExist != null && xExist.parent != painel.transform) xExist.SetParent(painel.transform, false);
        var x = SolengardLayoutSetup.CriarBotaoFechar(painel.transform, "BtnFecharDetalhe");
        var bfp = x.GetComponent<BotaoFecharPainel>();
        if (bfp != null) Object.DestroyImmediate(bfp);
        var xBtn = x.GetComponent<Button>();

        // ── Navegacao (controle): BtnAcao <-> X, modo Explicit ──
        if (xBtn != null)
        {
            var n1 = btnAcaoBtn.navigation; n1.mode = Navigation.Mode.Explicit; n1.selectOnUp = xBtn; n1.selectOnDown = xBtn; btnAcaoBtn.navigation = n1;
            var n2 = xBtn.navigation;       n2.mode = Navigation.Mode.Explicit; n2.selectOnUp = btnAcaoBtn; n2.selectOnDown = btnAcaoBtn; xBtn.navigation = n2;
        }

        // Ordem de render dentro da Moldura: bg(Image da Moldura) -> Splash -> resto -> X.
        splash.transform.SetAsFirstSibling();
        x.transform.SetAsLastSibling();

        // ── Binder + pre-preenche o mapa de splashes ──
        var binder = painel.GetComponent<DetalhePersonagemUI>() ?? painel.AddComponent<DetalhePersonagemUI>();
        PrePreencherSplashes(binder, log);

        painel.SetActive(false);

        // ── Transforma os 5 cards em botoes AbrirDetalhe (remove BtnComprar) ──
        foreach (var (id, _) in SPLASHES)
        {
            var card = AcharProfundo(ct, $"CardClasse_{id}");
            if (card == null) { log.AppendLine($"  [!] CardClasse_{id} nao encontrado"); continue; }
            var cimg = card.GetComponent<Image>() ?? card.gameObject.AddComponent<Image>();
            var cbtn = card.GetComponent<Button>() ?? card.gameObject.AddComponent<Button>();
            cbtn.targetGraphic = cimg;
            var mba = card.GetComponent<MenuButtonAction>() ?? card.gameObject.AddComponent<MenuButtonAction>();
            mba.acao = MenuAction.AbrirDetalhe; mba.parametro = id;
            var bc = card.Find("BtnComprar");
            if (bc != null) Object.DestroyImmediate(bc.gameObject); // compra migra p/ o detalhe
            EditorUtility.SetDirty(card.gameObject);
            log.AppendLine($"  CardClasse_{id} -> AbrirDetalhe (BtnComprar removido)");
        }

        // ── Religa MainMenuManager.painelDetalhe ──
        var mmm = Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (mmm != null)
        {
            var so = new SerializedObject(mmm);
            var p = so.FindProperty("painelDetalhe");
            if (p != null) { p.objectReferenceValue = painel; so.ApplyModifiedProperties(); EditorUtility.SetDirty(mmm); log.AppendLine("  MainMenuManager.painelDetalhe religado"); }
            else log.AppendLine("  [!] campo 'painelDetalhe' ausente no MainMenuManager");
        }
        else log.AppendLine("  [!] MainMenuManager nao encontrado");

        // Detalhe e overlay: deve ficar por cima de tudo (inclusive BottomTabs).
        painel.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[DetalhePersonagem] construido:\n{log}");
        EditorUtility.DisplayDialog("Solengard — Detalhe Personagem",
            $"PainelDetalhePersonagem construido + cards religados. Salve a cena (Ctrl+S).\n\n{log}", "OK");
    }

    [MenuItem("Solengard/UI: Detalhe Personagem (painel + cards)", validate = true)]
    static bool ConstruirValidate() => GameObject.Find("Canvas") != null;

    static void PrePreencherSplashes(DetalhePersonagemUI binder, StringBuilder log)
    {
        var so  = new SerializedObject(binder);
        var arr = so.FindProperty("splashes");
        if (arr == null) { log.AppendLine("  [!] campo 'splashes' ausente"); return; }
        arr.arraySize = SPLASHES.Length;
        for (int i = 0; i < SPLASHES.Length; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("classId").stringValue = SPLASHES[i].id;
            e.FindPropertyRelative("splash").objectReferenceValue = CarregarSprite(CHARS_DIR + SPLASHES[i].file);
        }
        so.ApplyModifiedProperties();
        log.AppendLine($"  splashes: {SPLASHES.Length} classes pre-preenchidas");
    }

    // Uma celula de stat: label (esq) + valor (dir, nome Val_*) numa grade 2x4.
    static void CelulaStat(Transform parent, string valName, string label, int col, int row)
    {
        const int COLS = 2, ROWS = 4;
        float cw = 1f / COLS, ch = 1f / ROWS;
        float x0 = col * cw, x1 = (col + 1) * cw;
        float yTop = 1f - row * ch, yBot = yTop - ch;

        var cell = FindOrCreate(parent, "Cell_" + valName);
        Anchor(cell, x0, yBot, x1, yTop);
        { var i = cell.GetComponent<Image>(); if (i != null) Object.DestroyImmediate(i); }

        var l = Txt(cell.transform, "Lbl", label, 22f, COR_LABEL, TextAlignmentOptions.Left);
        Anchor(l.gameObject, 0.06f, 0f, 0.55f, 1f);
        var v = Txt(cell.transform, valName, "—", 24f, Color.white, TextAlignmentOptions.Right);
        v.fontStyle = FontStyles.Bold; Anchor(v.gameObject, 0.45f, 0f, 0.94f, 1f);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────
    static GameObject FindOrCreate(Transform parent, string nome)
    {
        var e = parent.Find(nome);
        if (e != null) return e.gameObject;
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Anchor(GameObject go, float minX, float minY, float maxX, float maxY)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(minX, minY); rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    static void Stretch(GameObject go) => Anchor(go, 0f, 0f, 1f, 1f);

    static Image Img(GameObject go, Color cor, bool raycast)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = cor; img.raycastTarget = raycast;
        return img;
    }

    static TextMeshProUGUI Txt(Transform parent, string nome, string texto, float size, Color cor, TextAlignmentOptions align)
    {
        var go = FindOrCreate(parent, nome);
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto; tmp.fontSize = size; tmp.color = cor; tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal; tmp.raycastTarget = false;
        return tmp;
    }

    static Transform AcharProfundo(Transform raiz, string nome)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nome) return t;
        return null;
    }

    static Sprite CarregarSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogWarning($"[DetalhePersonagem] asset ausente: {path}"); return null; }
        bool mudou = false;
        if (imp.textureType != TextureImporterType.Sprite)   { imp.textureType = TextureImporterType.Sprite; mudou = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; mudou = true; }
        if (!imp.alphaIsTransparency)                        { imp.alphaIsTransparency = true; mudou = true; }
        if (mudou) imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
