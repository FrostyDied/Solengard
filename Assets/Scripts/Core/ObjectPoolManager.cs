using System.Collections.Generic;
using UnityEngine;

// Gerencia pools de objetos reutilizáveis para performance em mobile.
// Configure as pools no Inspector e chame GetFromPool / ReturnToPool nos sistemas de spawn.
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        [Min(1)] public int tamanhoInicial = 10;
    }

    [Header("Pools (inimigos, projéteis, efeitos)")]
    public List<Pool> pools = new();

    Dictionary<string, Queue<GameObject>> poolDic = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InicializarPools();
    }

    void InicializarPools()
    {
        // Snapshot do count antes de qualquer Instantiate — Awakes dos prefabs podem chamar
        // RegisterPool() que adiciona a pools[], corrompendo um foreach em andamento.
        int count = pools.Count;
        for (int p = 0; p < count; p++)
        {
            Pool pool = pools[p];
            Queue<GameObject> fila = new();
            for (int i = 0; i < pool.tamanhoInicial; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                fila.Enqueue(obj);
            }
            poolDic[pool.tag] = fila;
            Debug.Log($"[ObjectPoolManager] Pool '{pool.tag}' criada com {pool.tamanhoInicial} objetos.");
        }
    }

    // Retira um objeto da pool; expande automaticamente se estiver vazia
    public GameObject GetFromPool(string tag)
    {
        if (!poolDic.TryGetValue(tag, out Queue<GameObject> fila))
        {
            Debug.LogWarning($"[ObjectPoolManager] Pool '{tag}' não encontrada.");
            return null;
        }

        if (fila.Count == 0)
        {
            Pool pool = pools.Find(p => p.tag == tag);
            if (pool == null) return null;
            GameObject extra = Instantiate(pool.prefab);
            extra.SetActive(false);
            fila.Enqueue(extra);
        }

        GameObject obj = fila.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public bool HasPool(string tag) => poolDic.ContainsKey(tag);

    // Registra uma pool em runtime (usada por sistemas que criam prefabs proceduralmente)
    public void RegisterPool(string tag, GameObject prefab, int initialSize)
    {
        if (poolDic.ContainsKey(tag)) return;

        pools.Add(new Pool { tag = tag, prefab = prefab, tamanhoInicial = initialSize });
        Queue<GameObject> fila = new();
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            fila.Enqueue(obj);
        }
        poolDic[tag] = fila;
        Debug.Log($"[ObjectPoolManager] Pool '{tag}' registrada em runtime ({initialSize} objetos).");
    }

    // Desativa o objeto e o devolve à pool correspondente
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDic.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPoolManager] Pool '{tag}' não existe. Objeto destruído.");
            Destroy(obj);
            return;
        }
        obj.SetActive(false);
        poolDic[tag].Enqueue(obj);
    }
}
