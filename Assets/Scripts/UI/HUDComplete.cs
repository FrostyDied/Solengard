using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDComplete : MonoBehaviour
{
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
        Debug.Log("[HUD] Poder especial ativado");
        StartCoroutine(CooldownPoder(30f));
    }

    IEnumerator CooldownPoder(float duracao)
    {
        _poderEmCooldown = true;
        if (botaoPoderEspecial != null) botaoPoderEspecial.interactable = false;
        float restante = duracao;
        while (restante > 0f)
        {
            if (textoCooldown != null) textoCooldown.text = Mathf.CeilToInt(restante) + "s";
            restante -= Time.deltaTime;
            yield return null;
        }
        _poderEmCooldown = false;
        if (botaoPoderEspecial != null) botaoPoderEspecial.interactable = true;
        if (textoCooldown != null) textoCooldown.text = "";
    }
}
