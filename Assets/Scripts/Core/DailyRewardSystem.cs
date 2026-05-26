using UnityEngine;

// Controla recompensas de login diário com sequência de 7 dias.
// A sequência reseta se o jogador faltar um dia.
public class DailyRewardSystem : MonoBehaviour
{
    // Passa: dia atual da sequência (1-7), quantidade de diamantes
    public static event System.Action<int, int> OnDailyRewardAvailable;

    const string PREF_ULTIMO_LOGIN = "sol_last_login";
    const string PREF_DIA_STREAK   = "sol_streak_day";

    // Recompensas indexadas por dia (índice 0 = dia 1)
    static readonly int[] recompensasDiamantes = { 10, 15, 20, 25, 30, 50, 100 };

    // Chamado pelo GameManager no Start após inicialização
    public void CheckDailyReward()
    {
        string hoje      = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
        string ultimoLog = PlayerPrefs.GetString(PREF_ULTIMO_LOGIN, "");
        int    diaAtual  = PlayerPrefs.GetInt(PREF_DIA_STREAK, 0);

        // Sem recompensa se já resgatou hoje
        if (ultimoLog == hoje) return;

        bool faltouUmDia = VerificarSeFaltouDia(ultimoLog);

        if (faltouUmDia)
        {
            // Reseta sequência ao faltar um dia
            diaAtual = 0;
            Debug.Log("[DailyRewardSystem] Sequência resetada por ausência.");
        }

        diaAtual = (diaAtual % 7) + 1;

        PlayerPrefs.SetString(PREF_ULTIMO_LOGIN, hoje);
        PlayerPrefs.SetInt(PREF_DIA_STREAK, diaAtual);
        PlayerPrefs.Save();

        int diamantes = recompensasDiamantes[diaAtual - 1];
        Debug.Log($"[DailyRewardSystem] Recompensa disponível — Dia {diaAtual}: {diamantes} diamantes");
        OnDailyRewardAvailable?.Invoke(diaAtual, diamantes);
    }

    // Reivindica a recompensa e deposita os diamantes na conta do jogador
    public void ClaimReward(int diaAtual)
    {
        if (diaAtual < 1 || diaAtual > 7) return;

        int diamantes = recompensasDiamantes[diaAtual - 1];
        DiamondSystem.Instance?.AddDiamonds(diamantes);

        if (diaAtual == 7)
            Debug.Log("[DailyRewardSystem] Bônus de dia 7: item especial desbloqueado! TODO: implementar entrega do item");
    }

    bool VerificarSeFaltouDia(string ultimoLogin)
    {
        if (string.IsNullOrEmpty(ultimoLogin)) return false;

        if (System.DateTime.TryParse(ultimoLogin, out System.DateTime dataAnterior))
        {
            System.TimeSpan diferenca = System.DateTime.UtcNow.Date - dataAnterior.Date;
            return diferenca.TotalDays > 1;
        }
        return false;
    }
}
