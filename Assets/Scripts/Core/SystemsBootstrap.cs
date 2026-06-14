using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Passo 2 da refatoracao arquitetural do MainMenu (Pilar A).
//
// Garante que TODOS os singletons de sistema existam em runtime, em QUALQUER cena,
// sem depender de rodar nenhum gerador de Editor nem de a cena conter os GameObjects.
// Resolve de forma definitiva o bug "FindObjectOfType retorna null -> acao morre em silencio".
//
// Estrategia anti-duplicacao:
//   1. BeforeSceneLoad: para cada sistema, so cria se NENHUMA instancia existir
//      (FindAnyObjectByType). Nunca cria um segundo de nada por conta propria.
//   2. Cada singleton ja tem guard no proprio Awake ("primeiro vence, duplicata se
//      autodestroi"), entao mesmo que a cena tambem contenha o GameObject, sobra
//      exatamente 1 instancia viva.
//   3. AfterSceneLoad agenda uma verificacao (1 frame depois, ja com os Destroy()
//      de eventuais duplicatas aplicados) que loga a contagem real de cada sistema.
public static class SystemsBootstrap
{
    // GameObject name + tipo do componente. Nomes seguem a convencao "[S] ..." ja usada na cena.
    static readonly (string name, Type type)[] Systems =
    {
        ("[S] DiamondSystem",       typeof(DiamondSystem)),
        ("[S] PermanentUpgrades",   typeof(PermanentUpgradeSystem)),
        ("[S] SeasonPassSystem",    typeof(SeasonPassSystem)),
        ("[S] DailyRewardSystem",   typeof(DailyRewardSystem)),
        ("[S] IAPSystem",           typeof(IAPSystem)),
        ("[S] AuthSystem",          typeof(AuthSystem)),
        ("[S] LocalizationManager", typeof(LocalizationManager)),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureSystems()
    {
        var criados   = new List<string>();
        var jaExistia = new List<string>();

        foreach (var (name, type) in Systems)
        {
            // Se ja existe uma instancia (replay sem domain reload, ou criada por outro
            // caminho), NAO cria -> nunca gera duplicata pela mao do bootstrap.
            if (UnityEngine.Object.FindAnyObjectByType(type, FindObjectsInactive.Include) != null)
            {
                jaExistia.Add(type.Name);
                continue;
            }

            var go = new GameObject(name);
            go.AddComponent(type); // o Awake do proprio singleton faz Instance + DontDestroyOnLoad
            criados.Add(type.Name);
        }

        Debug.Log($"[SystemsBootstrap] BeforeSceneLoad -> criados: " +
                  $"[{string.Join(", ", criados)}] | ja existiam: [{string.Join(", ", jaExistia)}]");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ScheduleVerification()
    {
        var hostGO = new GameObject("[S] BootstrapVerifier") { hideFlags = HideFlags.HideInHierarchy };
        UnityEngine.Object.DontDestroyOnLoad(hostGO);
        hostGO.AddComponent<SystemsBootstrapVerifier>().Configure(Systems);
    }

    // Componente auxiliar: espera 1 frame (para o Destroy() de eventuais duplicatas ja
    // ter sido processado pela engine) e entao conta quantas instancias de cada sistema
    // existem. Loga "OK" se houver exatamente 1; "!!" + warning caso contrario. Autodestroi.
    class SystemsBootstrapVerifier : MonoBehaviour
    {
        (string name, Type type)[] systems;

        public void Configure((string name, Type type)[] s) => systems = s;

        IEnumerator Start()
        {
            yield return null; // 1 frame: garante que Destroy() de duplicatas ja foi aplicado

            var sb = new StringBuilder("[SystemsBootstrap] Verificacao de singletons em runtime:\n");
            bool ok = true;
            foreach (var (_, type) in systems)
            {
                int count = UnityEngine.Object
                    .FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Length;
                sb.AppendLine($"  {(count == 1 ? "OK " : "!! ")}{type.Name}: {count}");
                if (count != 1) ok = false;
            }

            if (ok) Debug.Log(sb.ToString());
            else    Debug.LogWarning(sb.ToString() + "  -> esperado exatamente 1 de cada!");

            Destroy(gameObject);
        }
    }
}
