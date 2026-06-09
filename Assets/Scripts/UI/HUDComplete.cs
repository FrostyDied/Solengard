using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDComplete : MonoBehaviour
{
    [Header("Avatar")]
    public Image           avatarImagem;

    [Header("Boosts Ativos")]
    public Image[]         boostSlots = new Image[5];

    [Header("Poder Especial Bar")]
    public RectTransform   fillPoder;

    [Header("Vida")]
    public RectTransform   fillVida;
    public TextMeshProUGUI textoVida;

    [Header("XP")]
    public RectTransform   fillXP;
    public TextMeshProUGUI textoNivel;

    [Header("Timer")]
    public TextMeshProUGUI textoTimer;

    [Header("Poder Especial")]
    public Button          botaoPoderEspecial;
    public TextMeshProUGUI textoCooldown;
    public Image           imagemPoderEspecial;

    [Header("Pause")]
    public Button          botaoPause;
    [SerializeField] GameObject pausePanel;
    [SerializeField] Button     botaoRetomar;
    [SerializeField] Button     botaoMenuPrincipalPause;

    void OnEnable()
    {
        PlayerHealth.OnHealthChanged += AtualizarVida;
        XPSystem.OnLevelUp           += AtualizarNivel;
        WaveTimerSystem.OnTimerTick  += AtualizarTimer;
    }

    void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= AtualizarVida;
        XPSystem.OnLevelUp           -= AtualizarNivel;
        WaveTimerSystem.OnTimerTick  -= AtualizarTimer;
    }

    void Start()
    {
        botaoPause?.onClick.AddListener(TogglePause);
        botaoRetomar?.onClick.AddListener(TogglePause);
        botaoMenuPrincipalPause?.onClick.AddListener(IrParaMenu);
        pausePanel?.SetActive(false);
        botaoPoderEspecial?.onClick.AddListener(UsarPoderEspecial);

        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) AtualizarVida(ph.CurrentHealth, ph.MaxHealth);

        var xp = XPSystem.Instance;
        if (xp != null) { AtualizarNivel(xp.CurrentLevel); SetFill(fillXP, xp.XPProgress); }

        var wt = WaveTimerSystem.Instance;
        AtualizarTimer(wt != null && wt.IsRunning ? wt.TimeRemaining : 600f);

        SetFill(fillPoder, 1f);
        AtualizarAvatar();
    }

    void Update()
    {
        var xp = XPSystem.Instance;
        if (xp != null) SetFill(fillXP, xp.XPProgress);
    }

    void AtualizarVida(float atual, float max)
    {
        SetFill(fillVida, max > 0f ? atual / max : 0f);
        if (textoVida != null) textoVida.text = $"{Mathf.CeilToInt(atual)}/{Mathf.CeilToInt(max)}";
    }

    void AtualizarNivel(int nivel)
    {
        if (textoNivel != null) textoNivel.text = $"Nv.{nivel}";
    }

    void AtualizarTimer(float segundos)
    {
        if (textoTimer == null) return;
        textoTimer.text  = $"{Mathf.FloorToInt(segundos/60f):00}:{Mathf.FloorToInt(segundos%60f):00}";
        textoTimer.color = segundos <= 30f ? Color.red : Color.white;
    }

    void AtualizarAvatar()
    {
        if (avatarImagem == null) return;
        var cls = PlayerClassManager.Instance?.CurrentClass;
        if (cls == null) return;

        // Tenta carregar portrait da pasta Portroit por classId
        // Mapeamento classId → nome do arquivo
        string nomeArquivo = cls.classId switch
        {
            "warrior"     => "Guerreiro_Portroit",
            "mage"        => "Mago_Portroit",
            "assassin"    => "Assassino_Portroit",
            "necromancer" => "Necromante_Portroit",
            "paladin"     => "Paladino_Portroit",
            "hunter"      => "Caçador_Portroit",
            _             => null
        };

        if (nomeArquivo != null)
        {
            var portrait = Resources.Load<Sprite>($"Characters/Hero/Portroit/{nomeArquivo}");
            if (portrait != null) { avatarImagem.sprite = portrait; return; }
        }

        // Fallback: classIcon ou idleFrames[0]
        if (cls.classIcon != null)
            avatarImagem.sprite = cls.classIcon;
        else if (cls.idleFrames != null && cls.idleFrames.Length > 0)
            avatarImagem.sprite = cls.idleFrames[0];
    }

    static void SetFill(RectTransform rt, float t)
    {
        if (rt == null) return;
        t = Mathf.Clamp01(t);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(t,  1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void TogglePause()
    {
        if (pausePanel == null) return;
        bool p = !pausePanel.activeSelf;
        pausePanel.SetActive(p);
        Time.timeScale = p ? 0f : 1f;
    }

    void IrParaMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    bool _poderEmCooldown = false;
    void UsarPoderEspecial()
    {
        if (_poderEmCooldown) return;
        PlayerClassManager.Instance?.ActivateSpecialPower();
        var cooldown = PlayerClassManager.Instance?.CurrentClass?.specialCooldown ?? 45f;
        // Upgrade permanente: -5s por nível (máx -15s)
        float reducao = (PermanentUpgradeSystem.Instance?.GetLevel(PermanentUpgradeId.PoderEspecial) ?? 0) * 5f;
        cooldown = Mathf.Max(cooldown - reducao, 30f);
        // Fluxo Mágico (Mago): -40% adicional
        if (PlayerClassManager.Instance?.HasBoost("fluxo_magico") == true &&
            PlayerClassManager.Instance?.CurrentClass?.classId == "mage")
            cooldown *= 0.6f;
        StartCoroutine(CooldownPoder(cooldown));
    }

    IEnumerator CooldownPoder(float duracao)
    {
        _poderEmCooldown = true;
        if (botaoPoderEspecial != null) botaoPoderEspecial.interactable = false;
        SetFill(fillPoder, 0f);
        float restante = duracao;
        while (restante > 0f)
        {
            if (textoCooldown != null) textoCooldown.text = Mathf.CeilToInt(restante) + "s";
            SetFill(fillPoder, 1f - (restante / duracao));
            restante -= Time.deltaTime;
            yield return null;
        }
        _poderEmCooldown = false;
        if (botaoPoderEspecial != null) botaoPoderEspecial.interactable = true;
        if (textoCooldown != null) textoCooldown.text = "";
        SetFill(fillPoder, 1f);
    }
}
