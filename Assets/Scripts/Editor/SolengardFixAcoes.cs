using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Solengard.UI;

// Fix cirurgico do enum-shift (MenuAction): re-seta a acao dos botoes afetados POR NOME do enum
// (imune ao int serializado deslocado). Alvos:
//   - 5 botoes de pacote de diamantes (parametro comeca com "pacote_diamantes_") -> ComprarPacote
//   - BtnVideo (Assistir Video)                                                  -> AssistirVideo
// Toca SO esses 6; idempotente; reversivel por git. NAO mexe no enum nem em outros botoes
// (cards/AbrirDetalhe, aba Legado/AbrirLegado e abas da BottomTabs ficam intactos).
public static class SolengardFixAcoes
{
    [MenuItem("Solengard/Fix/Re-wirar ações (pacotes + vídeo)")]
    static void Rewire()
    {
        var todos = Object.FindObjectsByType<MenuButtonAction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (todos == null || todos.Length == 0)
        {
            EditorUtility.DisplayDialog("Solengard", "Nenhum MenuButtonAction na cena ativa. Abra a MainMenu.", "OK");
            return;
        }

        var log = new StringBuilder();
        int corrigidos = 0;
        foreach (var mba in todos)
        {
            // So os 6 alvos: identificacao inequivoca por parametro / nome do GO.
            MenuAction? alvo = null;
            if (!string.IsNullOrEmpty(mba.parametro) && mba.parametro.StartsWith("pacote_diamantes_"))
                alvo = MenuAction.ComprarPacote;
            else if (mba.gameObject.name == "BtnVideo")
                alvo = MenuAction.AssistirVideo;

            if (alvo == null) continue; // nao e alvo -> nao toca

            int antes = (int)mba.acao;
            if (mba.acao != alvo.Value)
            {
                mba.acao = alvo.Value;            // set por NOME do enum -> serializa o int atual correto
                EditorUtility.SetDirty(mba);
                corrigidos++;
            }
            log.AppendLine($"  {mba.gameObject.name} (param='{mba.parametro}'): acao {antes} -> {(int)mba.acao} ({mba.acao})");
        }

        if (corrigidos > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[FixAcoes] {corrigidos} corrigido(s) de {log.ToString().Split('\n').Length - 1} alvo(s):\n{log}");
        EditorUtility.DisplayDialog("Solengard — Re-wirar ações",
            (corrigidos > 0 ? $"{corrigidos} botão(ões) corrigido(s). Salve a cena (Ctrl+S).\n\n"
                            : "Nada a corrigir (já estavam certos).\n\n") +
            "Estado dos alvos (esperado: ComprarPacote=15, AssistirVideo=16):\n" + log, "OK");
    }

    [MenuItem("Solengard/Fix/Re-wirar ações (pacotes + vídeo)", validate = true)]
    static bool RewireValidate() => !string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
}
