using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Solengard.UI;

// Atualiza o parametro (= productId que chega no ComprarPacote) dos 5 botoes de pacote na cena,
// de "pacote_diamantes_1..5" para "diamonds_200..6000" (padrao EN por valor), alinhado com
// LojaController.Pacotes e IAPSystem. Tambem garante acao=ComprarPacote (defensivo).
// Mapeia por VALOR/ordem da Loja. Idempotente (re-rodar nao muda se ja novos); reversivel por git.
public static class SolengardFixPacoteIds
{
    // antigo -> novo (Iniciante/Aventureiro/Heroi/Lenda/Mitico = 200/450/1000/2800/6000)
    static readonly (string velho, string novo)[] MAP =
    {
        ("pacote_diamantes_1", "diamonds_200"),
        ("pacote_diamantes_2", "diamonds_450"),
        ("pacote_diamantes_3", "diamonds_1000"),
        ("pacote_diamantes_4", "diamonds_2800"),
        ("pacote_diamantes_5", "diamonds_6000"),
    };
    static readonly HashSet<string> NOVOS = new() { "diamonds_200", "diamonds_450", "diamonds_1000", "diamonds_2800", "diamonds_6000" };

    [MenuItem("Solengard/Fix/Atualizar IDs de pacote (botões)")]
    static void Fix()
    {
        var todos = Object.FindObjectsByType<MenuButtonAction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (todos == null || todos.Length == 0)
        {
            EditorUtility.DisplayDialog("Solengard", "Nenhum MenuButtonAction na cena ativa. Abra a MainMenu.", "OK");
            return;
        }

        var log = new StringBuilder();
        int n = 0;
        foreach (var mba in todos)
        {
            // E um botao de pacote? (parametro antigo OU ja novo)
            string novo = null;
            foreach (var (velho, nv) in MAP) if (mba.parametro == velho) novo = nv;
            if (novo == null && NOVOS.Contains(mba.parametro)) novo = mba.parametro; // ja migrado
            if (novo == null) continue; // nao e pacote -> nao toca

            bool mudou = false;
            if (mba.parametro != novo) { mba.parametro = novo; mudou = true; }
            if (mba.acao != MenuAction.ComprarPacote) { mba.acao = MenuAction.ComprarPacote; mudou = true; }
            if (mudou) { EditorUtility.SetDirty(mba); n++; }
            log.AppendLine($"  {mba.gameObject.name}: parametro='{mba.parametro}', acao={mba.acao}");
        }

        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[FixPacoteIds] {n} botao(oes) atualizado(s):\n{log}");
        EditorUtility.DisplayDialog("Solengard — IDs de pacote",
            (n > 0 ? $"{n} botão(ões) atualizado(s). Salve a cena (Ctrl+S).\n\n"
                   : "Nada a mudar (já estavam corretos).\n\n") +
            "Esperado: 5 botões com diamonds_200/450/1000/2800/6000 + acao=ComprarPacote.\n" + log, "OK");
    }

    [MenuItem("Solengard/Fix/Atualizar IDs de pacote (botões)", validate = true)]
    static bool FixValidate() => GameObject.Find("Canvas") != null;
}
