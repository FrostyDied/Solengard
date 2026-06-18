using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Reconstroi os 5 cards de personagem da Loja com arte real e monta o grid 2+2+1 SEM scroll.
// NAO-DESTRUTIVO / IDEMPOTENTE / REVERSIVEL (git checkout da cena).
//
// Por card (mesma estrutura validada no mage de teste):
//   geometria 470x470 -> moldura card_frame (Image raiz) -> Personagem -> Nome (TMP) -> BtnComprar.
//   O Button e o wiring (MenuButtonAction=ComprarClasse) NAO sao tocados; so a aparencia/label.
//
// IMPORT: forca Texture Type=Sprite, Sprite Mode=Single, Alpha Is Transparency=true em TODOS os
//   sprites usados (5 personagens + card_frame + btn_compra) ANTES de aplicar — evita sprite
//   fatiado/nulo (varios vem como Multiple). Altera os .meta (reverter via git checkout de Art/).
//   9-slice: sprites ainda sem borders -> Simple; marque borders e vire as consts *_SLICED.
public static class SolengardCardsPersonagemSetup
{
    const string FRAME_PATH = "Assets/Art/UI/Cards/card_frame.png";
    const string BTN_PATH   = "Assets/Art/UI/Buttons/btn_compra.png";
    const string CHARS_DIR  = "Assets/Art/UI/Characters/";

    // ── Geometria do grid (referencia 1080x1920; area util 205–1780) ──
    const float CARD_LADO      = 470f; // 1:1
    const float AREA_TOP       = 205f; // fim das abas (topo da area util)
    const float RESPIRO_TOPO   = 40f;
    const float MARGEM_LATERAL = 50f;
    const float GAP_COL        = 40f;
    const float GAP_LINHA      = 45f;
    const float TELA_W         = 1080f;

    // ── Calibragem compartilhada (ajuste fino no Inspector) ──
    const bool  MOLDURA_SLICED = false; // true depois de marcar borders na moldura
    const bool  BOTAO_SLICED   = false; // true depois de marcar borders na placa
    const float PERS_INSET_X   = 26f;   // respiro lateral da personagem
    const float PERS_TOP       = 80f;   // Pos Y do Personagem = -PERS_TOP (calibrado na cena: -80)
    const float PERS_ALTURA    = 360f;  // > 470? nao — define o tamanho; passa do card => mascara corta
    const bool  USAR_MASCARA   = true;  // RectMask2D no card -> recorta a personagem nos limites do card
    const bool  BOTAO_PRESERVE_ASPECT = true;
    const float NOME_FONT = 42f;   // tamanho do Nome (topo do card)
    static readonly Color COR_NOME = new Color(0.96f, 0.92f, 1f);
    // Classe (2a linha, abaixo do nome): menor e mais discreta.
    const float CLASSE_FONT = 28f;
    static readonly Color COR_CLASSE = new Color(0.72f, 0.68f, 0.82f);

    // col: 0=esquerda, 1=direita, 2=centralizado.  row: 0=topo .. 2=base.
    struct Def { public string id, sprite, nome, classe; public int col, row;
        public Def(string id, string sprite, string nome, string classe, int col, int row)
        { this.id = id; this.sprite = sprite; this.nome = nome; this.classe = classe; this.col = col; this.row = row; } }

    static readonly Def[] CARDS =
    {
        new Def("mage",        "seraphine.png", "Seraphine", "Maga",       0, 0),
        new Def("assassin",    "vael.png",      "Vael",      "Assassino",  1, 0),
        new Def("necromancer", "marveth.png",   "Marveth",   "Necromante", 0, 1),
        new Def("paladin",     "aldric.png",    "Aldric",    "Paladino",   1, 1),
        new Def("hunter",      "rynn.png",      "Rynn",      "Caçador",    2, 2), // linha 3, centralizado
    };

    [MenuItem("Solengard/UI: Cards Personagem (5 + grid)")]
    static void Construir()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado. Abra a MainMenu.", "OK"); return; }

        // Sprites compartilhados (forca Single/alpha antes de usar).
        var frameSp = CarregarSprite(FRAME_PATH);
        var btnSp   = CarregarSprite(BTN_PATH);

        var log = new StringBuilder();
        int n = 0;
        foreach (var d in CARDS)
        {
            var card = AcharProfundo(canvas.transform, $"CardClasse_{d.id}");
            if (card == null) { log.AppendLine($"  [!] CardClasse_{d.id} nao encontrado"); continue; }
            var charSp = CarregarSprite(CHARS_DIR + d.sprite);
            ProcessarCard(card, d, frameSp, charSp, btnSp, log);
            n++;
        }

        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Cards] {n} card(s) montado(s):\n{log}");
        EditorUtility.DisplayDialog("Solengard — Cards Personagem",
            $"{n} card(s) reconstruido(s) + grid. Salve a cena (Ctrl+S).\n\n{log}", "OK");
    }

    [MenuItem("Solengard/UI: Cards Personagem (5 + grid)", validate = true)]
    static bool ConstruirValidate() => GameObject.Find("Canvas") != null;

    static void ProcessarCard(Transform card, Def d, Sprite frameSp, Sprite charSp, Sprite btnSp, StringBuilder log)
    {
        // ── 0) GEOMETRIA: 470x470, anchor/pivot top-center, posicao no grid ──
        var rt = (RectTransform)card;
        rt.anchorMin = new Vector2(.5f, 1f); rt.anchorMax = new Vector2(.5f, 1f); rt.pivot = new Vector2(.5f, 1f);
        rt.sizeDelta = new Vector2(CARD_LADO, CARD_LADO);
        rt.anchoredPosition = PosFor(card.parent as RectTransform, d.col, d.row);

        // ── 1) FUNDO: moldura ──
        var cardImg = card.GetComponent<Image>() ?? card.gameObject.AddComponent<Image>();
        if (frameSp != null)
        {
            cardImg.sprite = frameSp; cardImg.color = Color.white;
            cardImg.type = MOLDURA_SLICED ? Image.Type.Sliced : Image.Type.Simple;
            cardImg.preserveAspect = false;
        }
        cardImg.raycastTarget = true;

        var mask = card.GetComponent<RectMask2D>();
        if (USAR_MASCARA && mask == null) card.gameObject.AddComponent<RectMask2D>();
        else if (!USAR_MASCARA && mask != null) Object.DestroyImmediate(mask);

        // ── 2) PERSONAGEM ──
        var persGO = FilhoOuCria(card, "Personagem");
        var prt = persGO.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 1f); prt.anchorMax = new Vector2(1f, 1f); prt.pivot = new Vector2(.5f, 1f);
        prt.offsetMin = new Vector2(PERS_INSET_X, -(PERS_TOP + PERS_ALTURA));
        prt.offsetMax = new Vector2(-PERS_INSET_X, -PERS_TOP);
        var persImg = persGO.GetComponent<Image>() ?? persGO.AddComponent<Image>();
        if (charSp != null) { persImg.sprite = charSp; persImg.color = Color.white; }
        persImg.preserveAspect = true; persImg.raycastTarget = false;

        // ── 3) NOME (grande, topo) + CLASSE (menor, discreta, logo abaixo) ──
        var nomeGO = FilhoOuCria(card, "Nome");
        var nrt = nomeGO.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, .85f); nrt.anchorMax = new Vector2(1f, 1f); nrt.pivot = new Vector2(.5f, 1f);
        nrt.offsetMin = new Vector2(10f, 0f); nrt.offsetMax = new Vector2(-10f, -8f);
        var nomeTmp = nomeGO.GetComponent<TextMeshProUGUI>() ?? nomeGO.AddComponent<TextMeshProUGUI>();
        nomeTmp.text = d.nome; nomeTmp.color = COR_NOME; nomeTmp.fontSize = NOME_FONT;
        nomeTmp.fontStyle = FontStyles.Bold; nomeTmp.alignment = TextAlignmentOptions.Center;
        nomeTmp.textWrappingMode = TextWrappingModes.NoWrap; nomeTmp.raycastTarget = false;

        var classeGO = FilhoOuCria(card, "Classe");
        var crt = classeGO.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, .75f); crt.anchorMax = new Vector2(1f, .85f); crt.pivot = new Vector2(.5f, 1f);
        crt.offsetMin = new Vector2(10f, 0f); crt.offsetMax = new Vector2(-10f, 0f);
        var classeTmp = classeGO.GetComponent<TextMeshProUGUI>() ?? classeGO.AddComponent<TextMeshProUGUI>();
        classeTmp.text = d.classe; classeTmp.color = COR_CLASSE; classeTmp.fontSize = CLASSE_FONT;
        classeTmp.alignment = TextAlignmentOptions.Center;
        classeTmp.textWrappingMode = TextWrappingModes.NoWrap; classeTmp.raycastTarget = false;

        // ── 4) BTN COMPRA: so aparencia + label; Button/wiring INTACTOS ──
        var btnT = card.Find("BtnComprar");
        if (btnT == null) log.AppendLine($"  [!] {card.name}/BtnComprar ausente");
        else
        {
            var btnImg = btnT.GetComponent<Image>() ?? btnT.gameObject.AddComponent<Image>();
            if (btnSp != null)
            {
                btnImg.sprite = btnSp; btnImg.color = Color.white;
                btnImg.type = BOTAO_SLICED ? Image.Type.Sliced : Image.Type.Simple;
                btnImg.preserveAspect = !BOTAO_SLICED && BOTAO_PRESERVE_ASPECT;
            }
            btnImg.raycastTarget = true;
            var lbl = btnT.GetComponentInChildren<TextMeshProUGUI>(true);
            if (lbl != null) { lbl.text = $"<sprite name=\"diamante\"> {Preco(d.id)}"; lbl.color = Color.white; lbl.raycastTarget = false; }
        }

        // ── 5) Ordem de render: Personagem -> Nome -> BtnComprar (moldura = raiz, atras) ──
        persGO.transform.SetSiblingIndex(0);
        nomeGO.transform.SetSiblingIndex(1);
        classeGO.transform.SetSiblingIndex(2);
        if (btnT != null) btnT.SetAsLastSibling();

        EditorUtility.SetDirty(card.gameObject);
        log.AppendLine($"  {card.name}: {d.nome}  @({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})  preco {Preco(d.id)}");
    }

    // anchoredPosition do card dentro da AbaPersonagens (filhos top-center).
    // Topo da AbaPersonagens (px abaixo do topo da tela) computado do RectTransform do pai
    // (assume ancoras full-stretch, como o builder de layout cria): -offsetMax.y.
    static Vector2 PosFor(RectTransform parent, int col, int row)
    {
        float containerTop = 307.5f; // fallback se o pai nao resolver
        if (parent != null)
            containerTop = -(parent.anchoredPosition.y + parent.sizeDelta.y * (1f - parent.pivot.y));

        float apX = col == 0 ? (MARGEM_LATERAL + CARD_LADO / 2f - TELA_W / 2f)
                  : col == 1 ? (TELA_W - MARGEM_LATERAL - CARD_LADO / 2f - TELA_W / 2f)
                  : 0f; // centralizado
        float cardTop = AREA_TOP + RESPIRO_TOPO + row * (CARD_LADO + GAP_LINHA);
        float apY = containerTop - cardTop;
        return new Vector2(apX, apY);
    }

    static int Preco(string id)
    {
        foreach (var (cid, _, preco) in LojaController.GetClasses())
            if (cid == id) return preco;
        return 0;
    }

    static GameObject FilhoOuCria(Transform parent, string nome)
    {
        var t = parent.Find(nome);
        if (t != null) return t.gameObject;
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static Transform AcharProfundo(Transform raiz, string nome)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nome) return t;
        return null;
    }

    // Forca Sprite + Single + Alpha Is Transparency (sprites vem como Multiple).
    static Sprite CarregarSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogWarning($"[Cards] asset ausente: {path}"); return null; }
        bool mudou = false;
        if (imp.textureType != TextureImporterType.Sprite)    { imp.textureType = TextureImporterType.Sprite; mudou = true; }
        if (imp.spriteImportMode != SpriteImportMode.Single)  { imp.spriteImportMode = SpriteImportMode.Single; mudou = true; }
        if (!imp.alphaIsTransparency)                         { imp.alphaIsTransparency = true; mudou = true; }
        if (mudou) imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
