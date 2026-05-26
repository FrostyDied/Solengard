using UnityEngine;

// ScriptableObject de configuração global do Solengard.
// Crie via Assets → Solengard → GameConfig e atribua ao GameManager.
// Permite ajuste fino de balanceamento sem recompilar o projeto.
[CreateAssetMenu(fileName = "GameConfig", menuName = "Solengard/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Dificuldade")]
    public float dificuldadeBase          = 1f;
    // Multiplicador aplicado à dificuldade a cada wave concluída
    public float escalamentoPorWave       = 0.15f;
    // Multiplicadores máximos para não tornar o jogo injogável
    public float multiplicadorDificMaximo = 5f;

    [Header("Diamantes por ação")]
    public int diamantesPorKill           = 0;  // drop aleatório
    public int diamantesPorWaveConcluida  = 5;
    public int diamantesPorMissaoDiaria   = 15;

    [Header("Anúncios (rewarded ads)")]
    // Intervalo mínimo em segundos entre exibições de rewarded ads
    public float intervaloMinimoAds       = 300f;
    // Diamantes concedidos por rewarded ad assistido
    public int   recompensaAdDiamantes    = 10;

    [Header("Temporada")]
    // Duração da temporada em dias corridos
    public int   duracaoTemporadaDias     = 30;

    // XP concedido por cada tipo de ação
    public int   xpPorKill                = 1;
    public int   xpPorWaveConcluida       = 25;
    public int   xpPorMissaoDiaria        = 50;

    [Header("Wave System")]
    public float intervaloEntreWaves      = 5f;
    public int   inimigosBaseWave1        = 5;
    public int   incrementoInimigosPorWave = 3;

    // Retorna o multiplicador de dificuldade para a wave informada
    public float GetDificuldadeParaWave(int wave)
    {
        float mult = dificuldadeBase + escalamentoPorWave * (wave - 1);
        return Mathf.Min(mult, multiplicadorDificMaximo);
    }
}
