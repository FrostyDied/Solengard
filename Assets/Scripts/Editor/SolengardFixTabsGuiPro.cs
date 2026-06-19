using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Solengard.UI;

// Fix CIRURGICO: desativa a skin do prefab GUI Pro (Button_Rectangle_01_Convex_White) que esta
// aninhada como FILHO de cada um dos 3 botoes de aba da Loja, e que renderiza o roxo por cima
// do tab_loja. Toca SO os filhos-prefab dentro de BtnPersonagens/BtnUpgrades/BtnDiamantes —
// NAO mexe nas outras ~25 instancias do mesmo prefab na cena (X de fechar, etc.).
//
// DESATIVA (SetActive false), nao remove: reversivel (re-ativar / git checkout), idempotente.
// O Button + MenuButtonAction ficam no proprio BtnXxx (nao no prefab) -> clique nao quebra.
// Apos desativar, o Image do BtnXxx (tab_loja) aparece e o highlight por alpha volta a funcionar.
public static class SolengardFixTabsGuiPro
{
    static readonly string[] ABAS = { "BtnPersonagens", "BtnUpgrades", "BtnDiamantes" };
    const string PREFAB_MARCA = "Button_Rectangle_01_Convex_White";

    [MenuItem("Solengard/Fix/Desativar skin GUI Pro das abas")]
    static void Fix()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { EditorUtility.DisplayDialog("Solengard", "Canvas nao encontrado. Abra a MainMenu.", "OK"); return; }

        var log = new StringBuilder();
        int desativados = 0;
        foreach (var nome in ABAS)
        {
            var aba = AcharProfundo(canvas.transform, nome);
            if (aba == null) { log.AppendLine($"  [!] {nome} nao encontrado"); continue; }

            // Confirma que o clique mora no proprio botao de aba (nao no prefab que vamos desativar).
            bool temBtn = aba.GetComponent<Button>() != null;
            bool temMba = aba.GetComponent<MenuButtonAction>() != null;

            int achados = 0;
            foreach (Transform child in aba) // SO filhos diretos do botao de aba
            {
                string p = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
                if (string.IsNullOrEmpty(p) || !p.Contains(PREFAB_MARCA)) continue; // nao e a skin GUI Pro

                achados++;
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    EditorUtility.SetDirty(child.gameObject);
                    desativados++;
                    log.AppendLine($"  {nome}/{child.name}: skin GUI Pro DESATIVADA");
                }
                else log.AppendLine($"  {nome}/{child.name}: skin GUI Pro ja estava inativa");
            }

            log.AppendLine($"  {nome}: Button={temBtn}, MenuButtonAction={temMba}, skins encontradas={achados}");
        }

        if (desativados > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[FixTabsGuiPro] {desativados} skin(s) desativada(s):\n{log}");
        EditorUtility.DisplayDialog("Solengard — Desativar skin GUI Pro das abas",
            (desativados > 0 ? $"{desativados} skin(s) desativada(s). Salve a cena (Ctrl+S).\n\n"
                             : "Nada a desativar (ja estavam inativas).\n\n") + log, "OK");
    }

    [MenuItem("Solengard/Fix/Desativar skin GUI Pro das abas", validate = true)]
    static bool FixValidate() => GameObject.Find("Canvas") != null;

    static Transform AcharProfundo(Transform raiz, string nome)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nome) return t;
        return null;
    }
}
