using System.Collections.Generic;
using UnityEngine;

public enum RandomEventType { MiniBossEvent, PowerUpSpawnEvent, SpeedBoostEvent, EnemyRageEvent }

[System.Serializable]
public class RandomEvent
{
    public RandomEventType tipo;
    [Range(0f, 1f)] public float peso = 0.25f;
}

[System.Serializable]
public class WaveModifier
{
    public string nome;
    public float  multiplicadorInimigos = 1f;
    public float  multiplicadorVelocidade = 1f;
    public float  multiplicadorVida = 1f;
}

// Gera variação procedural na arena a cada run: obstáculos, eventos e modificadores de wave.
// Chamado pelo GameManager no início de cada run via InitializeRun().
public class ProceduralArenaSystem : MonoBehaviour
{
    public static event System.Action<string> OnRandomEventTriggered;

    [Header("Posições candidatas de obstáculos")]
    public List<Vector2> posicoesObstaculoCandidatas = new();

    [Header("Obstáculos")]
    public GameObject prefabObstaculo;
    [Min(0)] public int minObstaculos = 3;
    [Min(0)] public int maxObstaculos = 6;

    [Header("Eventos aleatórios")]
    public List<RandomEvent> eventosDisponiveis = new()
    {
        new RandomEvent { tipo = RandomEventType.MiniBossEvent,    peso = 0.15f },
        new RandomEvent { tipo = RandomEventType.PowerUpSpawnEvent, peso = 0.35f },
        new RandomEvent { tipo = RandomEventType.SpeedBoostEvent,  peso = 0.30f },
        new RandomEvent { tipo = RandomEventType.EnemyRageEvent,   peso = 0.20f },
    };

    [Header("Modificadores de wave disponíveis")]
    public List<WaveModifier> modificadoresDisponiveis = new();

    // Modificador ativo para a run atual — lido pelo ZoneManager e sistemas de inimigos
    public WaveModifier ModificadorAtivo { get; private set; }

    List<GameObject> obstaculosSpawnados = new();

    // Chamado pelo GameManager no início de cada run
    public void InitializeRun()
    {
        LimparArena();
        SpawnarObstaculos();
        SortearModificadorDeWave();

        RandomEventType eventoSorteado = SortearEvento();
        Debug.Log($"[ProceduralArenaSystem] Evento da run: {eventoSorteado}");
        OnRandomEventTriggered?.Invoke(eventoSorteado.ToString());
    }

    void SpawnarObstaculos()
    {
        if (prefabObstaculo == null || posicoesObstaculoCandidatas.Count == 0) return;

        List<Vector2> candidatos = new(posicoesObstaculoCandidatas);
        int quantidade = Random.Range(minObstaculos, Mathf.Min(maxObstaculos, candidatos.Count) + 1);

        for (int i = 0; i < quantidade; i++)
        {
            int idx = Random.Range(0, candidatos.Count);
            Vector3 pos = candidatos[idx];
            candidatos.RemoveAt(idx);

            GameObject obs = Instantiate(prefabObstaculo, pos, Quaternion.identity);
            obstaculosSpawnados.Add(obs);
        }

        Debug.Log($"[ProceduralArenaSystem] {quantidade} obstáculos gerados.");
    }

    // Usa sorteio ponderado pelos pesos configurados
    RandomEventType SortearEvento()
    {
        if (eventosDisponiveis.Count == 0) return RandomEventType.PowerUpSpawnEvent;

        float total = 0f;
        foreach (RandomEvent e in eventosDisponiveis) total += e.peso;

        float roll = Random.Range(0f, total);
        float acumulado = 0f;

        foreach (RandomEvent e in eventosDisponiveis)
        {
            acumulado += e.peso;
            if (roll <= acumulado) return e.tipo;
        }

        return eventosDisponiveis[0].tipo;
    }

    void SortearModificadorDeWave()
    {
        if (modificadoresDisponiveis.Count == 0)
        {
            ModificadorAtivo = new WaveModifier { nome = "Normal" };
            return;
        }

        ModificadorAtivo = modificadoresDisponiveis[Random.Range(0, modificadoresDisponiveis.Count)];
        Debug.Log($"[ProceduralArenaSystem] Modificador de wave: {ModificadorAtivo.nome}");
    }

    void LimparArena()
    {
        foreach (GameObject obs in obstaculosSpawnados)
            if (obs != null) Destroy(obs);
        obstaculosSpawnados.Clear();
    }
}
