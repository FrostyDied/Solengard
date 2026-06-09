using UnityEngine;
using UnityEditor;

public class SolengardBoostDebug : EditorWindow
{
    [MenuItem("Solengard/Debug/Boost e Especiais")]
    static void Open() => GetWindow<SolengardBoostDebug>("Boost Debug");

    void OnGUI()
    {
        GUILayout.Label("── Poderes Especiais ──", EditorStyles.boldLabel);
        if (GUILayout.Button("Ativar Especial da Classe Atual"))
            PlayerClassManager.Instance?.ActivateSpecialPower();

        GUILayout.Space(3);
        GUILayout.Label("── Trocar Classe ──", EditorStyles.boldLabel);
        string[] classes = { "warrior", "mage", "assassin", "necromancer", "paladin", "hunter" };
        string[] classNames = { "Guerreiro", "Mago", "Assassino", "Necromante", "Paladino", "Caçador" };
        for (int i = 0; i < classes.Length; i++)
            if (GUILayout.Button($"Selecionar: {classNames[i]}"))
            {
                PlayerClassManager.Instance?.SelectClass(classes[i]);
                PlayerClassManager.Instance?.ActivateSpecialPower();
            }

        GUILayout.Space(5);
        GUILayout.Label("── Boosts Base ──", EditorStyles.boldLabel);
        string[] baseBoosts = { "dano", "vel", "vida", "cooldown", "range", "xpmagnet", "recuperacao" };
        foreach (var b in baseBoosts)
            if (GUILayout.Button($"Aplicar: {b}"))
                PlayerClassManager.Instance?.AddBoost(b);

        GUILayout.Space(5);
        var currentClass = PlayerClassManager.Instance?.CurrentClass?.classId ?? "";
        GUILayout.Label($"── Boosts de Classe ({currentClass}) ──", EditorStyles.boldLabel);

        var boostsByClass = new System.Collections.Generic.Dictionary<string, string[]>
        {
            { "warrior",     new[]{ "corrente_perfurante", "sede_sangue", "pele_ferro" } },
            { "mage",        new[]{ "sobrecarga_arcana", "fragmentacao", "fluxo_magico" } },
            { "assassin",    new[]{ "golpe_letal", "rastro_venenoso", "adrenalina" } },
            { "necromancer", new[]{ "alma_drenada", "caveira_explosiva", "maldicao_ampliada" } },
            { "paladin",     new[]{ "consagracao", "luz_cegante", "aura_curadora" } },
            { "hunter",      new[]{ "flechas_perfurantes", "olho_aguia", "rajada_dupla" } },
        };

        if (boostsByClass.TryGetValue(currentClass, out var boosts))
            foreach (var b in boosts)
                if (GUILayout.Button($"Aplicar: {b}"))
                    PlayerClassManager.Instance?.AddBoost(b);
        else
            GUILayout.Label("Nenhuma classe selecionada");

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
