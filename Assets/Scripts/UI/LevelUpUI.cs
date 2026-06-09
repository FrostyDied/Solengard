using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance { get; private set; }

    [System.Serializable]
    public class UpgradeOption
    {
        public string        id;
        public string        nome;
        public string        descricao;
        public System.Action onChoose;
    }

    [SerializeField] GameObject          panel;
    [SerializeField] Button[]            optionButtons; // 3 botões
    [SerializeField] TextMeshProUGUI[]   optionNames;
    [SerializeField] TextMeshProUGUI[]   optionDescs;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable()  => XPSystem.OnLevelUp += Show;
    void OnDisable() => XPSystem.OnLevelUp -= Show;

    public void Show(int level)
    {
        var pc = PlayerController.Instance;
        VFXManager.Instance?.SpawnLevelUp(pc != null ? pc.transform.position : Vector3.zero);
        Time.timeScale = 0f;
        var options = GetRandomOptions(3);
        if (panel != null) panel.SetActive(true);

        for (int i = 0; i < optionButtons.Length && i < options.Count; i++)
        {
            var opt = options[i];
            if (optionNames != null && i < optionNames.Length && optionNames[i] != null)
                optionNames[i].text = opt.nome;
            if (optionDescs != null && i < optionDescs.Length && optionDescs[i] != null)
                optionDescs[i].text = opt.descricao;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => Choose(opt));
            optionButtons[i].gameObject.SetActive(true);
        }
    }

    void Choose(UpgradeOption opt)
    {
        opt.onChoose?.Invoke();
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
        Debug.Log($"[LevelUp] Escolheu: {opt.nome}");
    }

    List<UpgradeOption> GetRandomOptions(int count)
    {
        var all    = BuildAllOptions();
        var result = new List<UpgradeOption>();
        var used   = new HashSet<int>();
        int safe   = 0;
        while (result.Count < count && safe++ < 100)
        {
            int i = Random.Range(0, all.Count);
            if (!used.Contains(i)) { used.Add(i); result.Add(all[i]); }
        }
        return result;
    }

    List<UpgradeOption> BuildAllOptions()
    {
        var pc = PlayerController.Instance;
        var pa = pc != null ? pc.GetComponent<PlayerAttack>()  : null;
        var ph = pc != null ? pc.GetComponent<PlayerHealth>()  : null;

        var options = new List<UpgradeOption>
        {
            new UpgradeOption { id = "dano",     nome = "Lamina Afiada",     descricao = "+10% dano de ataque",
                onChoose = () => { if (pa) pa.attackDamage = Mathf.Min(pa.attackDamage * 1.10f, 150f); } },
            new UpgradeOption { id = "vel",      nome = "Pes Ageis",         descricao = "+2% velocidade de movimento",
                onChoose = () => { if (pc) pc.moveSpeed = Mathf.Min(pc.moveSpeed * 1.02f, PlayerController.MAX_MOVE_SPEED); } },
            new UpgradeOption { id = "vida",     nome = "Coracao Forte",     descricao = "+5% vida maxima",
                onChoose = () => { if (ph) ph.AumentarVidaMax(ph.MaxHealth * 0.05f); } },
            new UpgradeOption { id = "range",    nome = "Alcance Mistico",   descricao = "+8% alcance de ataque",
                onChoose = () => { if (pa) pa.attackRange = Mathf.Min(pa.attackRange * 1.08f, 12f); } },
            new UpgradeOption { id = "cooldown", nome = "Furia de Combate",  descricao = "Ataca 3% mais rapido",
                onChoose = () => { if (pa) pa.attackCooldown = Mathf.Max(pa.attackCooldown * 0.97f, 0.12f); } },
            new UpgradeOption { id = "xpmagnet", nome = "Cristal Magnetico", descricao = "Cristais de XP se atraem de longe",
                onChoose = () => { XPDrop.GlobalMagnetRadius += 3f; } },
        };

        // Adiciona boosts exclusivos da classe atual
        var classId = PlayerClassManager.Instance?.CurrentClass?.classId ?? "";
        var classBoosts = GetClassBoosts(classId);
        foreach (var b in classBoosts)
            options.Add(b);

        return options;
    }

    System.Collections.Generic.List<UpgradeOption> GetClassBoosts(string classId)
    {
        var list = new System.Collections.Generic.List<UpgradeOption>();
        switch (classId)
        {
            case "warrior":
                list.Add(new UpgradeOption { id="corrente_perfurante", nome="Corrente Perfurante", descricao="WhipChain atravessa todos os inimigos no caminho", onChoose=()=>PlayerClassManager.Instance?.AddBoost("corrente_perfurante") });
                list.Add(new UpgradeOption { id="sede_sangue",         nome="Sede de Sangue",      descricao="Cada kill durante Fúria aumenta dano +5%",          onChoose=()=>PlayerClassManager.Instance?.AddBoost("sede_sangue") });
                list.Add(new UpgradeOption { id="pele_ferro",          nome="Pele de Ferro",        descricao="Reduz dano recebido 20% quando HP < 30%",           onChoose=()=>PlayerClassManager.Instance?.AddBoost("pele_ferro") });
                break;
            case "mage":
                list.Add(new UpgradeOption { id="sobrecarga_arcana",   nome="Sobrecarga Arcana",    descricao="Cada 5º projétil causa 3x dano",                    onChoose=()=>PlayerClassManager.Instance?.AddBoost("sobrecarga_arcana") });
                list.Add(new UpgradeOption { id="fragmentacao",        nome="Fragmentação",         descricao="Projéteis explodem em 4 direções ao acertar",       onChoose=()=>PlayerClassManager.Instance?.AddBoost("fragmentacao") });
                list.Add(new UpgradeOption { id="fluxo_magico",        nome="Fluxo Mágico",         descricao="Nova Arcana recarrega 40% mais rápido",             onChoose=()=>PlayerClassManager.Instance?.AddBoost("fluxo_magico") });
                break;
            case "assassin":
                list.Add(new UpgradeOption { id="golpe_letal",         nome="Golpe Letal",          descricao="Primeiro ataque após Fase Sombria causa 5x dano",   onChoose=()=>PlayerClassManager.Instance?.AddBoost("golpe_letal") });
                list.Add(new UpgradeOption { id="rastro_venenoso",     nome="Rastro Venenoso",      descricao="Deixa rastro que envenena inimigos por 3s",         onChoose=()=>PlayerClassManager.Instance?.AddBoost("rastro_venenoso") });
                list.Add(new UpgradeOption { id="adrenalina",          nome="Adrenalina",           descricao="Ao receber dano, velocidade +60% e dano +40% por 3s", onChoose=()=>PlayerClassManager.Instance?.AddBoost("adrenalina") });
                break;
            case "necromancer":
                list.Add(new UpgradeOption { id="alma_drenada",        nome="Alma Drenada",         descricao="Cada kill recupera 3 HP",                           onChoose=()=>PlayerClassManager.Instance?.AddBoost("alma_drenada") });
                list.Add(new UpgradeOption { id="caveira_explosiva",   nome="Caveira Explosiva",    descricao="Projéteis explodem em área maior ao acertar",       onChoose=()=>PlayerClassManager.Instance?.AddBoost("caveira_explosiva") });
                list.Add(new UpgradeOption { id="maldicao_ampliada",   nome="Maldição Ampliada",    descricao="Maldição em Área dura 3s a mais",                   onChoose=()=>PlayerClassManager.Instance?.AddBoost("maldicao_ampliada") });
                break;
            case "paladin":
                list.Add(new UpgradeOption { id="consagracao",         nome="Consagração",          descricao="Julgamento cria zona que causa dano e reduz velocidade por 8s", onChoose=()=>PlayerClassManager.Instance?.AddBoost("consagracao") });
                list.Add(new UpgradeOption { id="luz_cegante",         nome="Luz Cegante",          descricao="Julgamento Divino atordoa por 4s em vez de 2s",    onChoose=()=>PlayerClassManager.Instance?.AddBoost("luz_cegante") });
                list.Add(new UpgradeOption { id="aura_curadora",       nome="Aura Curadora",        descricao="Regenera 2 HP/s constantemente",                   onChoose=()=>PlayerClassManager.Instance?.AddBoost("aura_curadora") });
                break;
            case "hunter":
                list.Add(new UpgradeOption { id="flechas_perfurantes", nome="Flechas Perfurantes",  descricao="Flechas atravessam inimigos",                       onChoose=()=>PlayerClassManager.Instance?.AddBoost("flechas_perfurantes") });
                list.Add(new UpgradeOption { id="olho_aguia",          nome="Olho de Águia",        descricao="Alcance de ataque +40%",                           onChoose=()=>PlayerClassManager.Instance?.AddBoost("olho_aguia") });
                list.Add(new UpgradeOption { id="rajada_dupla",        nome="Rajada Dupla",         descricao="Chuva de Flechas dispara em 2 cones simultâneos",   onChoose=()=>PlayerClassManager.Instance?.AddBoost("rajada_dupla") });
                break;
        }
        return list;
    }
}
