using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Solengard.UI;

// Builder NAO-DESTRUTIVO do PainelUpgradesGrimorio (experimento alternativo ao grid).
// Cria o painel + setas + fundo + container de entradas, anexa GrimorioUI (que monta as
// paginas em runtime), pre-preenche os pontos de encaixe, religa MainMenuManager.painelGrimorio
// e reassegura a barra de navegacao por cima. O grid atual NAO e tocado.
// Ativar p/ testar: MainMenuManager -> usarGrimorioUpgrades = true.
public static class SolengardGrimorioSetup
{
    static readonly Color OURO     = new Color(0.78f, 0.65f, 0.20f);
    static readonly Color BG_PANEL = new Color(0.05f, 0.05f, 0.12f, 1f);

    [MenuItem("Solengard/Grimorio: Construir Painel")]
    static void Construir()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado. Abra a MainMenu.", "OK"); return; }
        var ct  = canvas.transform;
        var log = new StringBuilder();

        // Painel raiz (full-screen, inativo). Image raycastTarget=true -> captura o swipe.
        var painel = FindOrCreate(ct, "PainelUpgradesGrimorio");
        StretchFull(painel);
        EnsureImage(painel, BG_PANEL, true);
        painel.SetActive(false);

        // Header GRIMORIO (titulo esq + saldo dir) — reusa o mesmo da Loja/Missoes/Legado.
        SolengardMissoesLegadoSetup.HeaderEstiloLoja(painel.transform, "GRIMÓRIO");

        // X canonico (mesmo de Loja/Config) -> BotaoFecharPainel -> FecharTodos.
        SolengardLayoutSetup.CriarBotaoFechar(painel.transform, "BtnFechar");

        // Titulo da categoria + indicador (ex: "Ofensa · 1/6").
        var cat = AddText(painel, "CategoriaTitulo", "Ofensa · 1/6", 30f, OURO, TextAlignmentOptions.Center);
        cat.fontStyle = FontStyles.Bold; Anchor(cat.gameObject, 0.15f, 0.80f, 0.85f, 0.87f);

        // Fundo da pagina (placeholder; GrimorioUI aplica o sprite UNICO spriteFundoGrimorio).
        var fundo = FindOrCreate(painel.transform, "PageBackground");
        Anchor(fundo, 0.06f, 0.13f, 0.94f, 0.79f);
        EnsureImage(fundo, new Color(0.12f, 0.05f, 0.18f, 0.92f), false);

        // Container das entradas (sobre o fundo; clearance da barra de nav na base).
        var entradas = FindOrCreate(painel.transform, "Entradas");
        Anchor(entradas, 0.15f, 0.19f, 0.85f, 0.72f);
        { var i = entradas.GetComponent<Image>(); if (i != null) Object.DestroyImmediate(i); }

        // Setas laterais (fallback do swipe). Glifos "<"/">" p/ garantir render (a fonte do
        // projeto nao tem todos os glifos especiais — mesma questao do diamante/trofeu).
        Seta(painel.transform, "BtnPrev", "<", esquerda: true);
        Seta(painel.transform, "BtnNext", ">", esquerda: false);

        // Controller + pre-preenche os pontos de encaixe (icones 16 ids + 6 fundos).
        var grim = painel.GetComponent<GrimorioUI>() ?? painel.AddComponent<GrimorioUI>();
        PrePreencher(grim, log);

        // Religa MainMenuManager.painelGrimorio.
        var mmm = Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (mmm != null)
        {
            var so = new SerializedObject(mmm);
            var p = so.FindProperty("painelGrimorio");
            if (p != null) { p.objectReferenceValue = painel; so.ApplyModifiedProperties(); EditorUtility.SetDirty(mmm); log.AppendLine("  MainMenuManager.painelGrimorio religado"); }
            else log.AppendLine("  [!] campo 'painelGrimorio' ausente no MainMenuManager");
        }
        else log.AppendLine("  [!] MainMenuManager nao encontrado");

        // Barra de navegacao persistente DEVE ficar por cima do painel novo.
        var bottom = ct.Find("BottomTabs");
        if (bottom != null) bottom.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Grimorio] Construido:\n{log}");
        EditorUtility.DisplayDialog("Solengard — Grimorio",
            "PainelUpgradesGrimorio construido (grid intacto). Salve a cena.\n\n" +
            "Para testar: MainMenuManager -> usarGrimorioUpgrades = TRUE, e abra a aba UPGRADES.\n\n" + log, "OK");
    }

    [MenuItem("Solengard/Grimorio: Construir Painel", validate = true)]
    static bool ConstruirValidate() => GameObject.Find("Canvas") != null;

    // Pre-preenche os pontos de encaixe (so reconstroi se o tamanho divergir -> preserva PNGs ja postos).
    static void PrePreencher(GrimorioUI grim, StringBuilder log)
    {
        var so  = new SerializedObject(grim);
        var ids = (PermanentUpgradeId[])System.Enum.GetValues(typeof(PermanentUpgradeId));

        var ic = so.FindProperty("icones");
        if (ic != null && ic.arraySize != ids.Length)
        {
            ic.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var e = ic.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)ids[i];
                e.FindPropertyRelative("icone").objectReferenceValue = null;
            }
            log.AppendLine($"  icones: {ids.Length} slots (placeholder)");
        }

        // Fundo agora e sprite UNICO (spriteFundoGrimorio) — sem array p/ pre-preencher.
        so.ApplyModifiedProperties();
    }

    static void Seta(Transform painel, string nome, string glifo, bool esquerda)
    {
        var go = FindOrCreate(painel, nome);
        float xMin = esquerda ? 0.02f : 0.88f;
        float xMax = esquerda ? 0.12f : 0.98f;
        Anchor(go, xMin, 0.42f, xMax, 0.56f);
        EnsureImage(go, new Color(0.10f, 0.06f, 0.20f, 0.85f), true);
        if (go.GetComponent<Button>() == null) go.AddComponent<Button>();
        var lbl = AddText(go, "Label", glifo, 48f, Color.white, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold; StretchFull(lbl.gameObject);
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
    static GameObject FindOrCreate(GameObject parent, string nome) => FindOrCreate(parent.transform, nome);

    static void Anchor(GameObject go, float minX, float minY, float maxX, float maxY)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(minX, minY); rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    static void StretchFull(GameObject go) => Anchor(go, 0f, 0f, 1f, 1f);

    static Image EnsureImage(GameObject go, Color cor, bool raycast)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.sprite = null; img.color = cor; img.raycastTarget = raycast;
        return img;
    }

    static TextMeshProUGUI AddText(GameObject parent, string nome, string texto, float size, Color cor, TextAlignmentOptions align)
    {
        var go = FindOrCreate(parent, nome);
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto; tmp.fontSize = size; tmp.color = cor; tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal; tmp.raycastTarget = false;
        return tmp;
    }
}
