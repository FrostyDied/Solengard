using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Solengard.UI;

// Converte a aba Upgrades da Loja de LISTA para GRID de cards.
// Idempotente: remove os elementos da lista antiga (Cat_*/UpRow_*), cria GridContainer
// (GridLayoutGroup 4 colunas) + DetailPanel (nome/descricao/custo/COMPRAR) e anexa o
// runtime UpgradesGridUI, que monta os cards e religa o botao COMPRAR sozinho.
// NAO altera a logica de compra (PermanentUpgradeSystem/LojaController.ComprarUpgrade).
public static class SolengardUpgradesGridSetup
{
    static readonly Color BG_DETAIL = new Color(0.10f, 0.06f, 0.20f, 0.96f);
    static readonly Color OURO      = new Color(0.78f, 0.65f, 0.20f);
    static readonly Color BTN_OK    = new Color(0.35f, 0.06f, 0.60f);

    [MenuItem("Solengard/Upgrades: Construir Grid (Loja)")]
    static void Construir()
    {
        var canvas = GameObject.Find("Canvas");
        var aba = canvas != null ? Achar(canvas.transform, "AbaUpgrades") : null;
        if (aba == null)
        {
            EditorUtility.DisplayDialog("Solengard", "Canvas/PainelLoja/AbaUpgrades nao encontrado na cena ativa. Abra a MainMenu.", "OK");
            return;
        }

        var log = new StringBuilder();

        // Fundo da aba transparente (o painel ja tem o fundo blur).
        var abaImg = aba.GetComponent<Image>();
        if (abaImg != null) { abaImg.sprite = null; abaImg.color = new Color(0, 0, 0, 0); }

        // 1) Remove a lista antiga (categorias + linhas).
        int removidos = 0;
        for (int i = aba.childCount - 1; i >= 0; i--)
        {
            var c = aba.GetChild(i);
            if (c.name.StartsWith("Cat_") || c.name.StartsWith("UpRow_"))
            {
                Object.DestroyImmediate(c.gameObject);
                removidos++;
            }
        }
        log.AppendLine($"  Lista antiga removida: {removidos} elemento(s)");

        // 2) GridContainer (top) + GridLayoutGroup 4 colunas.
        var grid = FindOrCreate(aba, "GridContainer");
        // Grid maior: ocupa ~88% da largura e mais area vertical (DetailPanel virou barra fina).
        Anchor(grid, 0.04f, 0.20f, 0.96f, 0.99f);
        var glg = grid.GetComponent<GridLayoutGroup>() ?? grid.AddComponent<GridLayoutGroup>();
        // 4 col centralizadas, 4 linhas (16 upgrades). Internos do card escalam com o cellSize.
        glg.cellSize        = new Vector2(220f, 248f);
        glg.spacing         = new Vector2(22f, 22f);
        glg.padding         = new RectOffset(16, 16, 16, 16);
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.MiddleCenter; // centraliza o bloco na faixa
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;
        var gridImg = grid.GetComponent<Image>(); if (gridImg != null) Object.DestroyImmediate(gridImg);
        log.AppendLine("  GridContainer (GridLayoutGroup 4 col)");

        // 3) DetailPanel: BARRA FINA na base (~15% da altura). Conteudo horizontal:
        //    esquerda = Nome + Descricao; direita = Custo + botao COMPRAR.
        var det = FindOrCreate(aba, "DetailPanel");
        // Barra inteira subida (descola da borda inferior; botao COMPRAR nao corta).
        Anchor(det, 0.02f, 0.05f, 0.98f, 0.20f);
        EnsureImage(det, BG_DETAIL, true);

        // Nome: esquerda-topo da barra.
        var nome = AddText(det, "Det_Nome", "Selecione um upgrade", 28f, OURO, TextAlignmentOptions.Left);
        nome.fontStyle = FontStyles.Bold; Anchor(nome.gameObject, 0.02f, 0.52f, 0.64f, 0.95f);

        // Descricao: esquerda-baixo, abaixo do nome (1-2 linhas compactas).
        var desc = AddText(det, "Det_Descricao", "", 19f, Color.white, TextAlignmentOptions.TopLeft);
        Anchor(desc.gameObject, 0.02f, 0.06f, 0.64f, 0.50f);

        // Custo: direita-topo.
        var custo = AddText(det, "Det_Custo", "", 22f, OURO, TextAlignmentOptions.Left);
        Anchor(custo.gameObject, 0.66f, 0.55f, 0.98f, 0.95f);

        // Botao COMPRAR: direita-baixo, com margem da borda inferior da barra (~269x92 px).
        var btn = FindOrCreate(det, "BtnComprar");
        Anchor(btn, 0.70f, 0.16f, 0.96f, 0.52f);
        EnsureImage(btn, BTN_OK, true);
        if (btn.GetComponent<Button>() == null) btn.AddComponent<Button>();
        var lbl = AddText(btn, "Label", "COMPRAR", 22f, Color.white, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold; StretchFull(lbl.gameObject);
        log.AppendLine("  DetailPanel (barra fina: nome/descricao | custo/COMPRAR)");

        // 4) Runtime binder + pre-preenche o PONTO UNICO de icones (1 slot por upgrade).
        var grade = aba.GetComponent<UpgradesGridUI>() ?? aba.gameObject.AddComponent<UpgradesGridUI>();
        PrePreencherIcones(grade, log);

        EditorUtility.SetDirty(aba.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[UpgradesGrid] Construido:\n{log}");
        EditorUtility.DisplayDialog("Solengard — Upgrades Grid",
            $"Aba Upgrades convertida para GRID. Salve a cena.\n\n{log}\n" +
            "Icones: Inspector do AbaUpgrades -> UpgradesGridUI -> Icones (arraste os sprites).", "OK");
    }

    [MenuItem("Solengard/Upgrades: Construir Grid (Loja)", validate = true)]
    static bool ConstruirValidate() => GameObject.Find("Canvas") != null;

    // Cria 1 entrada por PermanentUpgradeId (sprite vazio). So reconstroi se o tamanho
    // divergir -> re-rodar NAO apaga sprites ja atribuidos pelo usuario.
    static void PrePreencherIcones(UpgradesGridUI grade, StringBuilder log)
    {
        var so   = new SerializedObject(grade);
        var prop = so.FindProperty("icones");
        if (prop == null) { log.AppendLine("  [!] campo 'icones' nao encontrado"); return; }

        var ids = (PermanentUpgradeId[])System.Enum.GetValues(typeof(PermanentUpgradeId));
        if (prop.arraySize == ids.Length)
        {
            log.AppendLine($"  Icones: {ids.Length} slots preservados (sprites mantidos)");
            return;
        }

        prop.arraySize = ids.Length;
        for (int i = 0; i < ids.Length; i++)
        {
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("id").enumValueIndex = (int)ids[i];
            el.FindPropertyRelative("icone").objectReferenceValue = null;
        }
        so.ApplyModifiedProperties();
        log.AppendLine($"  Icones: {ids.Length} slots criados (placeholder)");
    }

    // ── Helpers (locais, sem dependencia do gerador legado) ──────────────────────
    static Transform Achar(Transform raiz, string nome)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) if (t.name == nome) return t;
        return null;
    }

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
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal; tmp.raycastTarget = false;
        return tmp;
    }
}
