using UnityEngine;
using UnityEditor;

public class SolengardBoostDebug : EditorWindow
{
    [MenuItem("Solengard/Debug/Boost e Especiais")]
    static void Open() => GetWindow<SolengardBoostDebug>("Boost Debug");

    void OnGUI()
    {
        GUILayout.Label("── Poderes Especiais ──", EditorStyles.boldLabel);
        if (GUILayout.Button("Guerreiro — Fúria Sanguinária"))
            PlayerClassManager.Instance?.ActivateSpecialPower();

        GUILayout.Space(5);
        GUILayout.Label("── Boosts Base ──", EditorStyles.boldLabel);
        string[] baseBoosts = { "dano", "vel", "vida", "cooldown", "range", "xpmagnet", "recuperacao" };
        foreach (var b in baseBoosts)
            if (GUILayout.Button($"Aplicar: {b}"))
                PlayerClassManager.Instance?.AddBoost(b);

        GUILayout.Space(5);
        GUILayout.Label("── Boosts de Classe ──", EditorStyles.boldLabel);
        string[] classBoosts = {
            "corrente_perfurante","sede_sangue","pele_ferro",
            "sobrecarga_arcana","fragmentacao","fluxo_magico",
            "golpe_letal","rastro_venenoso","adrenalina",
            "alma_drenada","caveira_explosiva","maldicao_ampliada",
            "consagracao","luz_cegante","aura_curadora",
            "flechas_perfurantes","olho_aguia","rajada_dupla"
        };
        foreach (var b in classBoosts)
            if (GUILayout.Button($"Aplicar: {b}"))
                PlayerClassManager.Instance?.AddBoost(b);

        GUILayout.Space(5);
        GUILayout.Label("── Especiais por Classe ──", EditorStyles.boldLabel);
        if (GUILayout.Button("Ativar Especial da Classe Atual"))
            PlayerClassManager.Instance?.ActivateSpecialPower();

        GUILayout.Space(5);
        if (GUILayout.Button("Limpar Todos os Boosts"))
            PlayerClassManager.Instance?.ClearBoosts();

        GUILayout.Space(5);
        GUILayout.Label("── XP ──", EditorStyles.boldLabel);
        if (GUILayout.Button("+50 XP"))
            XPSystem.Instance?.AddXP(50);
        if (GUILayout.Button("+500 XP"))
            XPSystem.Instance?.AddXP(500);
    }
}
