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

        return new List<UpgradeOption>
        {
            new UpgradeOption { id = "dano",     nome = "Lamina Afiada",     descricao = "+25% dano de ataque",
                onChoose = () => { if (pa) pa.attackDamage = Mathf.Min(pa.attackDamage * 1.25f, 150f); } },
            new UpgradeOption { id = "vel",      nome = "Pes Ageis",         descricao = "+20% velocidade de movimento",
                onChoose = () => { if (pc) pc.moveSpeed = Mathf.Min(pc.moveSpeed * 1.2f, PlayerController.MAX_MOVE_SPEED); } },
            new UpgradeOption { id = "vida",     nome = "Coracao Forte",     descricao = "+30 vida maxima e cura 20",
                onChoose = () => { if (ph) { ph.AumentarVidaMax(30f); ph.Curar(20f); } } },
            new UpgradeOption { id = "range",    nome = "Alcance Mistico",   descricao = "+30% alcance de ataque",
                onChoose = () => { if (pa) pa.attackRange = Mathf.Min(pa.attackRange * 1.3f, 12f); } },
            new UpgradeOption { id = "cooldown", nome = "Furia de Combate",  descricao = "Ataca 25% mais rapido",
                onChoose = () => { if (pa) pa.attackCooldown = Mathf.Max(pa.attackCooldown * 0.75f, 0.12f); } },
            new UpgradeOption { id = "xpmagnet", nome = "Cristal Magnetico", descricao = "Cristais de XP se atraem de longe",
                onChoose = () => { XPDrop.GlobalMagnetRadius += 3f; } },
        };
    }
}
