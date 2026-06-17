using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Popula o PainelMissoes em runtime a partir do DailyMissionSystem (diárias + semanais).
// Segue o padrão ConfigUIBinder: anexar ao PainelMissoes, encontra containers por nome
// (criados pelo editor builder) e constrói as linhas. Botão Coletar ligado em runtime.
public class MissoesUIBinder : MonoBehaviour
{
    Transform _dailyContainer;
    Transform _weeklyContainer;
    TextMeshProUGUI _resetDiarias;
    TextMeshProUGUI _resetSemanais;
    bool _bound;

    void OnEnable()
    {
        if (!_bound) Bind();
        DailyMissionSystem.OnMissionsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        DailyMissionSystem.OnMissionsChanged -= Refresh;
    }

    void Update()
    {
        var ms = DailyMissionSystem.Instance;
        if (ms == null) return;
        if (_resetDiarias  != null) _resetDiarias.text  = $"Renova em {FmtHM(ms.TimeUntilDailyReset())}";
        if (_resetSemanais != null) _resetSemanais.text = $"Renova em {FmtDHM(ms.TimeUntilWeeklyReset())}";
    }

    void Bind()
    {
        _dailyContainer  = FindDeep("DailyContainer");
        _weeklyContainer = FindDeep("WeeklyContainer");
        _resetDiarias    = FindDeep("ResetDiarias")?.GetComponent<TextMeshProUGUI>();
        _resetSemanais   = FindDeep("ResetSemanais")?.GetComponent<TextMeshProUGUI>();
        _bound = true;
        if (_dailyContainer == null || _weeklyContainer == null)
            Debug.LogWarning("[MissoesBinder] Containers não encontrados — rode o editor builder de Missões/Legado.");
    }

    void Refresh()
    {
        var ms = DailyMissionSystem.Instance;
        if (ms == null) return;
        BuildLista(_dailyContainer,  ms.GetDailyMissions(),  weekly: false);
        BuildLista(_weeklyContainer, ms.GetWeeklyMissions(), weekly: true);
    }

    void BuildLista(Transform container, List<DailyMission> missoes, bool weekly)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--) Destroy(container.GetChild(i).gameObject);

        for (int i = 0; i < missoes.Count; i++)
            CriarLinha(container, missoes[i], i, weekly);
    }

    void CriarLinha(Transform parent, DailyMission m, int indice, bool weekly)
    {
        var row = NovoGO("Mission", parent);
        var rle = row.AddComponent<LayoutElement>(); rle.minHeight = 90f; rle.preferredHeight = 90f;
        var img = row.AddComponent<Image>(); img.color = new Color(0.08f, 0.08f, 0.16f, 0.85f);

        // Descrição
        var desc = NovoTexto("Desc", row.transform, m.descricao, 26f, Color.white, TextAlignmentOptions.TopLeft);
        var dRT = (RectTransform)desc.transform;
        dRT.anchorMin = new Vector2(0.02f, 0.45f); dRT.anchorMax = new Vector2(0.70f, 0.95f);
        dRT.offsetMin = Vector2.zero; dRT.offsetMax = Vector2.zero;

        // Barra de progresso (fundo + fill)
        var barBg = NovoGO("BarBg", row.transform);
        var barBgImg = barBg.AddComponent<Image>(); barBgImg.color = new Color(0f, 0f, 0f, 0.5f);
        var bRT = (RectTransform)barBg.transform;
        bRT.anchorMin = new Vector2(0.02f, 0.12f); bRT.anchorMax = new Vector2(0.70f, 0.38f);
        bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;

        var fill = NovoGO("Fill", barBg.transform);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = m.concluida ? new Color(0.30f, 0.78f, 0.35f) : new Color(0.55f, 0.35f, 0.85f);
        float t = m.meta > 0 ? Mathf.Clamp01((float)m.progresso / m.meta) : 0f;
        var fRT = (RectTransform)fill.transform;
        fRT.anchorMin = new Vector2(0f, 0f); fRT.anchorMax = new Vector2(t, 1f);
        fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;

        var prog = NovoTexto("Prog", barBg.transform, $"{m.progresso}/{m.meta}", 20f, Color.white, TextAlignmentOptions.Center);
        var pRT = (RectTransform)prog.transform;
        pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
        pRT.offsetMin = Vector2.zero; pRT.offsetMax = Vector2.zero;

        // Botão coletar
        var btnGO = NovoGO("BtnColetar", row.transform);
        var btnImg = btnGO.AddComponent<Image>();
        var btn = btnGO.AddComponent<Button>();
        var btnRT = (RectTransform)btnGO.transform;
        btnRT.anchorMin = new Vector2(0.72f, 0.18f); btnRT.anchorMax = new Vector2(0.98f, 0.82f);
        btnRT.offsetMin = Vector2.zero; btnRT.offsetMax = Vector2.zero;

        bool podeColetar = m.concluida && !m.recompensaResgatada;
        btnImg.color = m.recompensaResgatada ? new Color(0.25f, 0.25f, 0.28f)
                     : podeColetar           ? new Color(0.78f, 0.65f, 0.20f)
                                             : new Color(0.30f, 0.30f, 0.34f);
        btn.interactable = podeColetar;

        string label = m.recompensaResgatada ? "Resgatado" : $"+{m.recompensaDiamantes} 💎";
        NovoTexto("Label", btnGO.transform, label, 22f, Color.white, TextAlignmentOptions.Center);

        int idx = indice;
        bool sem = weekly;
        btn.onClick.AddListener(() =>
        {
            var sys = DailyMissionSystem.Instance;
            if (sys == null) return;
            if (sem) sys.ClaimWeekly(idx); else sys.ClaimDaily(idx);
            // Refresh é disparado via OnMissionsChanged
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    Transform FindDeep(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
        return null;
    }

    static GameObject NovoGO(string nome, Transform parent)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static TextMeshProUGUI NovoTexto(string nome, Transform parent, string texto, float size, Color cor, TextAlignmentOptions align)
    {
        var go = NovoGO(nome, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = texto; tmp.fontSize = size; tmp.color = cor; tmp.alignment = align;
        tmp.enableWordWrapping = true; tmp.raycastTarget = false;
        return tmp;
    }

    static string FmtHM(System.TimeSpan ts) => $"{(int)ts.TotalHours:00}:{ts.Minutes:00}";
    static string FmtDHM(System.TimeSpan ts) => ts.Days > 0 ? $"{ts.Days}d {ts.Hours:00}h" : $"{ts.Hours:00}h {ts.Minutes:00}m";
}
