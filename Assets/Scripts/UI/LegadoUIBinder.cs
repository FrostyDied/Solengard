using UnityEngine;
using TMPro;

// Popula o PainelLegado em runtime a partir do LegadoStatsSystem + dados existentes.
// Padrão ConfigUIBinder: anexar ao PainelLegado, encontra os textos por nome (criados
// pelo editor builder) e preenche os valores no OnEnable.
public class LegadoUIBinder : MonoBehaviour
{
    void OnEnable()
    {
        DiamondSystem.OnDiamondsChanged += AtualizarSaldo;
        Refresh();
        AtualizarSaldo(DiamondSystem.Instance?.GetBalance() ?? 0);
    }

    void OnDisable() => DiamondSystem.OnDiamondsChanged -= AtualizarSaldo;

    // Saldo de diamantes no header (igual Loja).
    void AtualizarSaldo(int saldo) => Set("TextoSaldo", saldo.ToString("N0"));

    void Refresh()
    {
        var st = LegadoStatsSystem.Instance;

        // ── Recordes ──
        Set("Val_MelhorScore", (st?.MelhorPontuacao ?? PlayerPrefs.GetInt("sol_best_score", 0)).ToString("N0"));
        Set("Val_ZonaMax",     $"Zona {Mathf.Max(1, st?.ZonaMaxima ?? PlayerPrefs.GetInt("sol_lt_maxzone", 0))}");

        var lr = st?.UltimaRun();
        if (lr != null)
        {
            int mm = Mathf.FloorToInt(lr.time / 60f), ss = Mathf.FloorToInt(lr.time % 60f);
            Set("Val_UltimaRun", $"Wave {lr.wave} · {lr.kills} kills · {mm:00}:{ss:00}");
        }
        else Set("Val_UltimaRun", "—");

        // ── Acumulados ──
        Set("Val_Partidas",  (st?.TotalRuns ?? 0).ToString("N0"));
        Set("Val_Tempo",     LegadoStatsSystem.FormatarTempo(st?.TempoTotalSegundos ?? 0f));
        Set("Val_Diamantes", $"{(st?.DiamantesLifetime ?? 0):N0} 💎");
        Set("Val_Kills",     (st?.KillsLifetime ?? 0).ToString("N0"));

        // ── Preferências ──
        var (nome, partidas) = st?.PersonagemMaisUsado() ?? ("—", 0);
        Set("Val_Personagem", partidas > 0 ? $"{nome} ({partidas})" : "—");
    }

    void Set(string nome, string valor)
    {
        var t = FindDeep(nome);
        if (t == null) return;
        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = valor;
    }

    Transform FindDeep(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
        return null;
    }
}
