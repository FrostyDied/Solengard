using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Fix de alinhamento do saldo na TopBar (home): o numero era CENTER-aligned e crescia p/ a
// esquerda, sobrepondo o ICONE de diamante (num de 6 digitos como "89.024"). Alinha igual a
// Loja: numero LEFT-aligned, pivot a esquerda, comecando logo apos a borda direita do icone
// -> cresce so p/ a direita, sem sobreposicao. Toca SO o TextoDiamantes (o icone fica onde esta).
// Idempotente (recalcula a partir da posicao atual do icone); reversivel por git.
public static class SolengardFixSaldoTopBar
{
    const float GAP     = 12f;   // respiro entre icone e numero
    const float LARGURA = 240f;  // largura do campo do numero (cresce p/ direita)

    [MenuItem("Solengard/Fix/Alinhar saldo (TopBar)")]
    static void Alinhar()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado. Abra a MainMenu.", "OK"); return; }

        var ico = AcharProfundo(canvas.transform, "IcoDiamante");
        var txt = AcharProfundo(canvas.transform, "TextoDiamantes");
        if (ico == null || txt == null)
        {
            EditorUtility.DisplayDialog("Solengard", $"Nao encontrado: IcoDiamante={ico != null}, TextoDiamantes={txt != null}", "OK");
            return;
        }

        var irt = (RectTransform)ico;
        var trt = (RectTransform)txt;

        // Borda direita do icone no espaco do anchor (icone e center-anchored).
        float iconeDir = irt.anchoredPosition.x + irt.sizeDelta.x * (1f - irt.pivot.x);

        // Numero: left-aligned, pivot a esquerda -> a borda esquerda = anchoredPosition.x.
        var tmp = txt.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.alignment = TextAlignmentOptions.MidlineLeft; // horizontal Left + vertical Middle
        trt.pivot           = new Vector2(0f, 0.5f);
        trt.sizeDelta       = new Vector2(LARGURA, trt.sizeDelta.y);
        trt.anchoredPosition = new Vector2(iconeDir + GAP, 0f);

        EditorUtility.SetDirty(txt.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        string msg = $"TextoDiamantes alinhado:\n  alignment=Left, pivot=(0,0.5)\n  borda esq do numero = {iconeDir + GAP:0} (icone dir {iconeDir:0} + gap {GAP:0})";
        Debug.Log($"[FixSaldoTopBar] {msg}");
        EditorUtility.DisplayDialog("Solengard — Alinhar saldo (TopBar)", msg + "\n\nSalve a cena (Ctrl+S).", "OK");
    }

    [MenuItem("Solengard/Fix/Alinhar saldo (TopBar)", validate = true)]
    static bool AlinharValidate() => GameObject.Find("Canvas") != null;

    static Transform AcharProfundo(Transform raiz, string nome)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nome) return t;
        return null;
    }
}
