using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Solengard.UI;

// Editor builder NÃO-DESTRUTIVO (idempotente) para Missões + Legado.
// - Constrói o conteúdo do PainelMissoes (containers que o MissoesUIBinder popula).
// - Cria o PainelLegado com seções/placeholders que o LegadoUIBinder popula.
// - Troca a aba Config->Legado no bottom bar (MenuButtonAction = AbrirLegado).
// - Religa MainMenuManager.painelLegado.
// Rode na cena MainMenu: Solengard/Missoes+Legado: Construir UI.
public static class SolengardMissoesLegadoSetup
{
    static readonly Color BG_CARD   = new Color(0.08f, 0.08f, 0.16f, 0.92f);
    static readonly Color BG_PANEL  = new Color(0.05f, 0.05f, 0.12f, 1f);
    static readonly Color OURO       = new Color(0.78f, 0.65f, 0.20f);

    [MenuItem("Solengard/Missoes+Legado: Construir UI")]
    static void Construir()
    {
        var canvas = AcharCanvas();
        if (canvas == null) { EditorUtility.DisplayDialog("Missoes+Legado", "Canvas nao encontrado na cena ativa. Abra a MainMenu.", "OK"); return; }

        var log = new System.Text.StringBuilder();
        ConstruirMissoes(canvas, log);
        var painelLegado = ConstruirLegado(canvas, log);
        TrocarAbaConfigPorLegado(canvas, log);
        ReligarMainMenu(painelLegado, log);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log($"[Missoes+Legado] Construido:\n{log}");
        EditorUtility.DisplayDialog("Missoes+Legado", $"UI construida (idempotente).\n\n{log}", "OK");
    }

    [MenuItem("Solengard/Missoes+Legado: Construir UI", validate = true)]
    static bool ConstruirValidate() => !string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

    // ── PainelMissoes ──────────────────────────────────────────────────────────────

    static void ConstruirMissoes(Transform canvas, System.Text.StringBuilder log)
    {
        var painel = FindOrCreate(canvas, "PainelMissoes");
        StretchFull(RT(painel));
        AplicarFundoBlur(painel);
        painel.SetActive(false);

        HeaderEstiloLoja(painel.transform, "MISSÕES");

        // Seção Diárias
        SecaoHeader(painel.transform, "HeaderDiarias", "DIÁRIAS", "ResetDiarias",
            new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.80f));
        Container(painel.transform, "DailyContainer",
            new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.73f));

        // Seção Semanais
        SecaoHeader(painel.transform, "HeaderSemanais", "SEMANAIS", "ResetSemanais",
            new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.40f));
        Container(painel.transform, "WeeklyContainer",
            new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.33f)); // bottom 0.12: clearance da barra de nav

        CriarBotaoFechar(painel.transform);

        if (painel.GetComponent<MissoesUIBinder>() == null) painel.AddComponent<MissoesUIBinder>();
        log.AppendLine("  PainelMissoes (conteudo + MissoesUIBinder)");
    }

    static void SecaoHeader(Transform parent, string nomeHeader, string titulo, string nomeReset, Vector2 min, Vector2 max)
    {
        var h = FindOrCreate(parent, nomeHeader);
        var hRT = RT(h); hRT.anchorMin = min; hRT.anchorMax = max; hRT.offsetMin = Vector2.zero; hRT.offsetMax = Vector2.zero;
        var t = AddTextChild(h, "Label", titulo, 28f, OURO, TextAlignmentOptions.Left);
        StretchPad(RT(t.gameObject), 0.02f);

        var reset = AddTextChild(h, nomeReset, "Renova em --:--", 20f, new Color(0.7f, 0.7f, 0.75f), TextAlignmentOptions.Right);
        var rRT = RT(reset.gameObject); rRT.anchorMin = new Vector2(0.5f, 0f); rRT.anchorMax = new Vector2(0.98f, 1f);
        rRT.offsetMin = Vector2.zero; rRT.offsetMax = Vector2.zero;
    }

    static void Container(Transform parent, string nome, Vector2 min, Vector2 max)
    {
        var c = FindOrCreate(parent, nome);
        var rt = RT(c); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var vlg = c.GetComponent<VerticalLayoutGroup>() ?? c.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f; vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true; vlg.childControlWidth = true;
        vlg.childAlignment = TextAnchor.UpperCenter;
    }

    // ── PainelLegado ───────────────────────────────────────────────────────────────

    static GameObject ConstruirLegado(Transform canvas, System.Text.StringBuilder log)
    {
        var painel = FindOrCreate(canvas, "PainelLegado");
        StretchFull(RT(painel));
        AplicarFundoBlur(painel);
        painel.SetActive(false);

        HeaderEstiloLoja(painel.transform, "LEGADO");

        // Recordes
        var rec = Card(painel.transform, "CardRecordes", "RECORDES",
            new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.86f));
        LinhaStat(rec, "Melhor Pontuação", "Val_MelhorScore", 0);
        LinhaStat(rec, "Última Run",        "Val_UltimaRun",   1);
        LinhaStat(rec, "Zona Máxima",       "Val_ZonaMax",     2);

        // Acumulados
        var acc = Card(painel.transform, "CardAcumulados", "ACUMULADOS",
            new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.64f));
        LinhaStat(acc, "Partidas",           "Val_Partidas",  0);
        LinhaStat(acc, "Tempo Total",        "Val_Tempo",     1);
        LinhaStat(acc, "Diamantes (total)",  "Val_Diamantes", 2);
        LinhaStat(acc, "Abates (total)",     "Val_Kills",     3);

        // Preferências
        var pref = Card(painel.transform, "CardPreferencias", "PREFERÊNCIAS",
            new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.38f));
        LinhaStat(pref, "Personagem Favorito", "Val_Personagem", 0);

        // Ranking (placeholder)
        var rank = Card(painel.transform, "CardRanking", "RANKING",
            new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.26f));
        // Sem emoji 🏆 (TMP renderiza como □ sem sprite asset) — texto puro.
        var soon = AddTextChild(rank, "RankingSoon", "Ranking Global — Em breve", 24f,
            new Color(0.7f, 0.7f, 0.75f), TextAlignmentOptions.Center);
        StretchPad(RT(soon.gameObject), 0.04f);

        CriarBotaoFechar(painel.transform);

        if (painel.GetComponent<LegadoUIBinder>() == null) painel.AddComponent<LegadoUIBinder>();
        log.AppendLine("  PainelLegado (conteudo + LegadoUIBinder)");
        return painel;
    }

    static GameObject Card(Transform parent, string nome, string titulo, Vector2 min, Vector2 max)
    {
        var c = FindOrCreate(parent, nome);
        var rt = RT(c); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        EnsureImage(c, BG_CARD);

        var head = AddTextChild(c, "CardHeader", titulo, 24f, OURO, TextAlignmentOptions.TopLeft);
        var hRT = RT(head.gameObject); hRT.anchorMin = new Vector2(0.03f, 0.78f); hRT.anchorMax = new Vector2(0.97f, 0.98f);
        hRT.offsetMin = Vector2.zero; hRT.offsetMax = Vector2.zero;
        return c;
    }

    // Linha label-esquerda / valor-direita dentro do card. 'slot' empilha de cima p/ baixo.
    static void LinhaStat(GameObject card, string label, string nomeValor, int slot)
    {
        float top = 0.74f - slot * 0.20f;
        float bot = top - 0.18f;

        var lbl = AddTextChild(card, $"Lbl_{nomeValor}", label, 22f, Color.white, TextAlignmentOptions.Left);
        var lRT = RT(lbl.gameObject); lRT.anchorMin = new Vector2(0.04f, bot); lRT.anchorMax = new Vector2(0.55f, top);
        lRT.offsetMin = Vector2.zero; lRT.offsetMax = Vector2.zero;

        var val = AddTextChild(card, nomeValor, "—", 22f, OURO, TextAlignmentOptions.Right);
        var vRT = RT(val.gameObject); vRT.anchorMin = new Vector2(0.55f, bot); vRT.anchorMax = new Vector2(0.96f, top);
        vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;
    }

    // ── Bottom bar: Config -> Legado ─────────────────────────────────────────────

    static void TrocarAbaConfigPorLegado(Transform canvas, System.Text.StringBuilder log)
    {
        var tab = AcharFilho(canvas, "TabConfigs") ?? AcharFilho(canvas, "TabLegado");
        if (tab == null) { log.AppendLine("  [!] Aba Config/Legado nao encontrada no bottom bar"); return; }

        // Relabel
        var lbl = tab.GetComponentInChildren<TextMeshProUGUI>(true);
        if (lbl != null) lbl.text = "LEGADO";

        // MenuButtonAction = AbrirLegado (sobrescreve acao anterior)
        var mba = tab.GetComponent<MenuButtonAction>() ?? tab.gameObject.AddComponent<MenuButtonAction>();
        mba.acao = MenuAction.AbrirLegado;
        mba.parametro = "";
        EditorUtility.SetDirty(tab.gameObject);
        log.AppendLine("  Aba Config -> LEGADO (MenuButtonAction=AbrirLegado)");
    }

    static void ReligarMainMenu(GameObject painelLegado, System.Text.StringBuilder log)
    {
        var mmm = Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (mmm == null) { log.AppendLine("  [!] MainMenuManager nao encontrado"); return; }
        var so = new SerializedObject(mmm);
        var p = so.FindProperty("painelLegado");
        if (p != null) { p.objectReferenceValue = painelLegado; so.ApplyModifiedProperties(); EditorUtility.SetDirty(mmm); log.AppendLine("  MainMenuManager.painelLegado religado"); }
        else log.AppendLine("  [!] campo painelLegado nao encontrado em MainMenuManager");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    static Transform AcharCanvas()
    {
        var bottom = AcharNaCena("BottomTabs");
        if (bottom != null) { var c = bottom.GetComponentInParent<Canvas>(); if (c != null) return c.transform; }
        var any = Object.FindFirstObjectByType<Canvas>();
        return any != null ? any.transform : null;
    }

    static Transform AcharNaCena(string nome)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == nome) return t;
        return null;
    }

    static Transform AcharFilho(Transform raiz, string nome)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) if (t.name == nome) return t;
        return null;
    }

    // Header espelhando o HeaderLoja: barra topo #1A0A2E, titulo esquerda branco f42 bold,
    // diamante + saldo a direita (saldo dinamico via binder). Remove o Titulo centralizado antigo.
    // internal: reutilizado pelo builder do Grimorio (mesmo header).
    internal static void HeaderEstiloLoja(Transform painel, string titulo)
    {
        var velho = painel.Find("Titulo");
        if (velho != null) Object.DestroyImmediate(velho.gameObject);

        var h = FindOrCreate(painel, "Header");
        var hrt = RT(h);
        hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f); hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = new Vector2(0f, -50f); hrt.sizeDelta = new Vector2(0f, 100f);
        EnsureImage(h, new Color(0.102f, 0.039f, 0.180f, 1f)); // #1A0A2E (igual HeaderLoja)

        var t = AddTextChild(h, "Titulo", titulo, 42f, Color.white, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold;
        var trt = RT(t.gameObject);
        trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(0.65f, 1f); trt.pivot = new Vector2(0f, 0.5f);
        trt.anchoredPosition = new Vector2(20f, 0f); trt.sizeDelta = Vector2.zero;

        var ico = FindOrCreate(h.transform, "IcoDiamanteHeader");
        var irt = RT(ico);
        irt.anchorMin = new Vector2(0.65f, 0f); irt.anchorMax = new Vector2(0.65f, 1f); irt.pivot = new Vector2(0f, 0.5f);
        irt.anchoredPosition = new Vector2(8f, 0f); irt.sizeDelta = new Vector2(36f, 36f);
        var icoImg = ico.GetComponent<Image>() ?? ico.AddComponent<Image>();
        var gem = CarregarIconeDiamante();
        if (gem != null) { icoImg.sprite = gem; icoImg.color = Color.white; icoImg.preserveAspect = true; }
        icoImg.raycastTarget = false;

        var saldo = AddTextChild(h, "TextoSaldo", "0", 32f, new Color(1f, 0.843f, 0f), TextAlignmentOptions.Left); // #FFD700
        var srt = RT(saldo.gameObject);
        srt.anchorMin = new Vector2(0.65f, 0f); srt.anchorMax = new Vector2(0.88f, 1f); srt.pivot = new Vector2(0f, 0.5f);
        srt.anchoredPosition = new Vector2(50f, 0f); srt.sizeDelta = Vector2.zero;
    }

    static Sprite CarregarIconeDiamante()
    {
        const string p = "Assets/Art/UI/Icons/icon_diamante.png";
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }

    // Botao X padronizado: delega ao MESMO criador canonico usado por Loja/Config
    // (SolengardLayoutSetup.CriarBotaoFechar — skin GUI Pro, host Image alpha0+raycast,
    // glifo X 40px, BotaoFecharPainel -> FecharTodos). Remove qualquer X fora do padrao
    // antes (idempotente; repara paineis ja construidos com o X antigo vermelho).
    static void CriarBotaoFechar(Transform painel)
    {
        var antigo = painel.Find("BtnFechar");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);
        SolengardLayoutSetup.CriarBotaoFechar(painel, "BtnFechar");
    }

    // Fundo unico dos paineis de menu (menu_background_Dark.png). Forca import como Sprite
    // Single (o asset vem como Multiple) e aplica esticado. Mesma imagem de Loja/Config.
    static void AplicarFundoBlur(GameObject painel)
    {
        var img = painel.GetComponent<Image>() ?? painel.AddComponent<Image>();
        var sprite = CarregarFundoBlur();
        if (sprite != null)
        {
            img.sprite = sprite; img.color = Color.white;
            img.type = Image.Type.Simple; img.preserveAspect = false;
        }
        else img.color = BG_PANEL; // fallback solido se o asset sumir
        img.raycastTarget = true;
        SolengardBackgroundSetup.RemoverEscurecedor(painel.transform); // limpa camada antiga (fundo ja escuro)
    }

    const string BLUR_PATH = "Assets/Art/UI/Backgrounds/menu_background_Dark.png";
    static Sprite CarregarFundoBlur()
    {
        var imp = AssetImporter.GetAtPath(BLUR_PATH) as TextureImporter;
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(BLUR_PATH);
    }

    static GameObject FindOrCreate(Transform parent, string nome)
    {
        var existente = parent.Find(nome);
        if (existente != null) return existente.gameObject;
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void StretchPad(RectTransform rt, float pad)
    {
        rt.anchorMin = new Vector2(pad, 0f); rt.anchorMax = new Vector2(1f - pad, 1f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Image EnsureImage(GameObject go, Color cor)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = cor; img.raycastTarget = true;
        return img;
    }

    static TextMeshProUGUI AddTextChild(GameObject parent, string nome, string texto, float size, Color cor, TextAlignmentOptions align)
    {
        var existente = parent.transform.Find(nome);
        GameObject go = existente != null ? existente.gameObject : new GameObject(nome, typeof(RectTransform));
        if (existente == null) go.transform.SetParent(parent.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto; tmp.fontSize = size; tmp.color = cor; tmp.alignment = align;
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal; tmp.raycastTarget = false;
        return tmp;
    }
}
