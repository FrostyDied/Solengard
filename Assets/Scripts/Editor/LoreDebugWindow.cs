using UnityEngine;
using UnityEditor;

/// Ferramenta de QA editor-only: dispara a tela de lore de qualquer zona (1-15)
/// sem precisar jogar até ela. Exige Play Mode (o ShowLore congela timeScale e
/// espera input — o comportamento real que se quer conferir). Não vai pro build.
public class LoreDebugWindow : EditorWindow
{
    int _zona = 1;

    [MenuItem("Solengard/Debug/Testar Lore de Zona")]
    static void Open()
    {
        var win = GetWindow<LoreDebugWindow>("Testar Lore");
        win.minSize = new Vector2(260f, 110f);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Teste de Lore por Zona", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _zona = EditorGUILayout.IntSlider("Zona (1-15)", _zona, 1, 15);

        EditorGUILayout.Space();

        if (GUILayout.Button("Mostrar Lore", GUILayout.Height(28f)))
            MostrarLore();

        if (!EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Entre em Play Mode para testar.", MessageType.Info);
    }

    void MostrarLore()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Lore Debug", "Entre em Play Mode primeiro.", "OK");
            return;
        }

        if (ZoneManager.Instance == null || BiomeSystem.Instance == null || LoreScreenUI.Instance == null)
        {
            EditorUtility.DisplayDialog(
                "Lore Debug",
                "Instâncias não encontradas. Entre em Play Mode na GameScene primeiro.\n\n" +
                $"ZoneManager={(ZoneManager.Instance != null)}  " +
                $"BiomeSystem={(BiomeSystem.Instance != null)}  " +
                $"LoreScreenUI={(LoreScreenUI.Instance != null)}",
                "OK");
            return;
        }

        var zoneConfig = ZoneManager.Instance.GetZone(_zona - 1);
        if (zoneConfig == null)
        {
            EditorUtility.DisplayDialog("Lore Debug", $"Zona {_zona} não existe no ZoneManager.", "OK");
            return;
        }

        string nome = zoneConfig.nome;
        string lore = BiomeSystem.Instance.GetLoreByZone(_zona);

        LoreScreenUI.Instance.StartCoroutine(
            LoreScreenUI.Instance.ShowLore(nome, lore, null));
    }
}
