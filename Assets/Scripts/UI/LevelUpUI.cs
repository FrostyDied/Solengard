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

    readonly Dictionary<string, int> _boostLevels = new();

    public void ResetBoostLevels() => _boostLevels.Clear();

    int GetBoostLevel(string id) => _boostLevels.TryGetValue(id, out var v) ? v : 0;

    void RegisterBoost(string id)
    {
        _boostLevels[id] = GetBoostLevel(id) + 1;
    }

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
        var options = new List<UpgradeOption>();
        var classId = PlayerClassManager.Instance?.CurrentClass?.classId ?? "";

        AddProgressiveBoost(options, "dano", GetDanoLabel(), GetDanoDesc(),
            maxLevel: 3, onChoose: () => {
                RegisterBoost("dano");
                var pa = PlayerController.Instance?.GetComponent<PlayerAttack>();
                if (pa != null)
                {
                    float[] mults = { 1.05f, 1.10f, 1.15f };
                    pa.attackDamage *= mults[Mathf.Min(GetBoostLevel("dano") - 1, 2)];
                }
                NotifyDifficultyBoost("dano");
            });

        AddProgressiveBoost(options, "vel", GetVelLabel(), GetVelDesc(),
            maxLevel: 3, onChoose: () => {
                RegisterBoost("vel");
                var pc = PlayerController.Instance;
                if (pc != null)
                {
                    float[] mults = { 1.02f, 1.04f, 1.06f };
                    pc.SetMoveSpeed(pc.moveSpeed * mults[Mathf.Min(GetBoostLevel("vel") - 1, 2)]);
                }
                NotifyDifficultyBoost("vel");
            });

        AddProgressiveBoost(options, "vida", GetVidaLabel(), GetVidaDesc(),
            maxLevel: 3, onChoose: () => {
                RegisterBoost("vida");
                var ph = PlayerController.Instance?.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    float[] mults = { 0.03f, 0.06f, 0.09f };
                    float bonus = ph.MaxHealth * mults[Mathf.Min(GetBoostLevel("vida") - 1, 2)];
                    ph.AumentarVidaMax(bonus);
                }
                NotifyDifficultyBoost("vida");
            });

        AddProgressiveBoost(options, "cooldown", GetCooldownLabel(), "Ataque mais rápido",
            maxLevel: 3, onChoose: () => {
                RegisterBoost("cooldown");
                var pa = PlayerController.Instance?.GetComponent<PlayerAttack>();
                if (pa != null)
                {
                    float[] reductions = { 0.97f, 0.95f, 0.93f };
                    pa.attackCooldown *= reductions[Mathf.Min(GetBoostLevel("cooldown") - 1, 2)];
                    pa.attackCooldown = Mathf.Max(pa.attackCooldown, 0.12f);
                }
                NotifyDifficultyBoost("cooldown");
            });

        AddProgressiveBoost(options, "range", GetRangeLabel(), GetRangeDesc(),
            maxLevel: 3, onChoose: () => {
                RegisterBoost("range");
                var pa = PlayerController.Instance?.GetComponent<PlayerAttack>();
                if (pa != null)
                {
                    float[] mults = { 1.08f, 1.12f, 1.15f };
                    pa.attackRange *= mults[Mathf.Min(GetBoostLevel("range") - 1, 2)];
                    pa.attackRange = Mathf.Min(pa.attackRange, 12f);
                }
                NotifyDifficultyBoost("range");
            });

        AddProgressiveBoost(options, "xpmagnet", GetMagnetLabel(), "Cristais de XP voam mais longe até você",
            maxLevel: 3, onChoose: () => {
                RegisterBoost("xpmagnet");
                float[] bonuses = { 2f, 3f, 4f };
                XPDrop.GlobalMagnetRadius += bonuses[Mathf.Min(GetBoostLevel("xpmagnet") - 1, 2)];
            });

        // Recuperação de Emergência — cura % do HP máximo (aparece só se HP < 60%)
        {
            var ph = PlayerController.Instance?.GetComponent<PlayerHealth>();
            if (ph != null && ph.CurrentHealth / ph.MaxHealth < 0.60f && UnityEngine.Random.value < 0.25f)
            {
                int nivel = GetBoostLevel("recuperacao");
                if (nivel < 3)
                {
                    string[] nomes = { "Poção de Vida I", "Poção de Vida II", "Poção de Vida III" };
                    string[] descs = { "Recupera 25% do HP máximo", "Recupera 35% do HP máximo", "Recupera 50% do HP máximo" };
                    float[]  curas = { 0.25f, 0.35f, 0.50f };
                    options.Add(new UpgradeOption {
                        id        = "recuperacao",
                        nome      = nomes[nivel],
                        descricao = descs[nivel],
                        onChoose  = () => {
                            RegisterBoost("recuperacao");
                            var ph2 = PlayerController.Instance?.GetComponent<PlayerHealth>();
                            if (ph2 != null)
                            {
                                float cura = ph2.MaxHealth * curas[Mathf.Min(GetBoostLevel("recuperacao") - 1, 2)];
                                ph2.Heal(cura);
                            }
                        }
                    });
                }
            }
        }

        if (UnityEngine.Random.value < 0.30f)
        {
            var classBoosts = GetClassBoosts(classId);
            foreach (var b in classBoosts)
                if (!PlayerClassManager.Instance.HasBoost(b.id))
                    options.Add(b);
        }

        return options;
    }

    void AddProgressiveBoost(List<UpgradeOption> list, string id, string nome, string desc, int maxLevel, System.Action onChoose)
    {
        if (GetBoostLevel(id) >= maxLevel) return;
        list.Add(new UpgradeOption { id = id, nome = nome, descricao = desc, onChoose = onChoose });
    }

    string GetDanoLabel()     { int l = GetBoostLevel("dano");     string[] n = { "Lâmina Afiada I",       "Lâmina Afiada II",       "Lâmina Afiada III"       }; return n[Mathf.Min(l, 2)]; }
    string GetDanoDesc()      { int l = GetBoostLevel("dano");     string[] d = { "+5% dano",               "+10% dano",              "+15% dano"               }; return d[Mathf.Min(l, 2)]; }
    string GetVelLabel()      { int l = GetBoostLevel("vel");      string[] n = { "Passos Ágeis I",         "Passos Ágeis II",        "Passos Ágeis III"        }; return n[Mathf.Min(l, 2)]; }
    string GetVelDesc()       { int l = GetBoostLevel("vel");      string[] d = { "+2% velocidade",         "+4% velocidade",         "+6% velocidade"          }; return d[Mathf.Min(l, 2)]; }
    string GetVidaLabel()     { int l = GetBoostLevel("vida");     string[] n = { "Coração Forte I",        "Coração Forte II",       "Coração Forte III"       }; return n[Mathf.Min(l, 2)]; }
    string GetVidaDesc()      { int l = GetBoostLevel("vida");     string[] d = { "+3% HP máximo",          "+6% HP máximo",          "+9% HP máximo"           }; return d[Mathf.Min(l, 2)]; }
    string GetCooldownLabel() { int l = GetBoostLevel("cooldown"); string[] n = { "Fúria de Combate I",     "Fúria de Combate II",    "Fúria de Combate III"    }; return n[Mathf.Min(l, 2)]; }
    string GetRangeLabel()    { int l = GetBoostLevel("range");    string[] n = { "Alcance Místico I",      "Alcance Místico II",     "Alcance Místico III"     }; return n[Mathf.Min(l, 2)]; }
    string GetRangeDesc()     { int l = GetBoostLevel("range");    string[] d = { "+8% alcance",            "+12% alcance",           "+15% alcance"            }; return d[Mathf.Min(l, 2)]; }
    string GetMagnetLabel()   { int l = GetBoostLevel("xpmagnet"); string[] n = { "Cristal Magnético I",   "Cristal Magnético II",   "Cristal Magnético III"   }; return n[Mathf.Min(l, 2)]; }

    void NotifyDifficultyBoost(string boostId)
    {
        var diff = DifficultyAdaptiveSystem.Instance;
        if (diff == null) return;
        switch (boostId)
        {
            case "vida":     diff.AjustarPorBoostPlayer(hpBonus: 0.04f);    break;
            case "dano":     diff.AjustarPorBoostPlayer(hpBonus: 0.06f);    break;
            case "vel":      diff.AjustarPorBoostPlayer(speedBonus: 0.03f); break;
            case "cooldown": diff.AjustarPorBoostPlayer(speedBonus: 0.04f); break;
            case "range":    diff.AjustarPorBoostPlayer(hpBonus: 0.06f);    break;
        }
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
