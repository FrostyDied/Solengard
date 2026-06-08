using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// HUD de gameplay. Attach no "HUD Canvas" gerado pelo SolengardLayoutSetup.
public class HUDComplete : MonoBehaviour
{
    [Header("Vida")]
    public Slider          barraVida;
    public TextMeshProUGUI textoVida;

    [Header("XP / Nível")]
    public Slider          barraXP;
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

        // Seed inicial — lê estado atual caso eventos já tenham disparado antes do HUD existir
        var xp = XPSystem.Instance;
        if (xp != null) AtualizarNivel(xp.CurrentLevel);

        var ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) AtualizarVida(ph.CurrentHealth, ph.MaxHealth);

        var wt = WaveTimerSystem.Instance;
        if (wt != null && wt.IsRunning) AtualizarTimer(wt.TimeRemaining);
        else AtualizarTimer(600f);
    }

    void Update()
    {
        if (barraXP == null) return;
        var xp = XPSystem.Instance;
        if (xp != null) barraXP.value = xp.XPProgress;
    }

    void AtualizarVida(float atual, float max)
    {
        if (barraVida != null) barraVida.value = max > 0f ? atual / max : 0f;
        if (textoVida != null) textoVida.text  = $"{Mathf.CeilToInt(atual)}/{Mathf.CeilToInt(max)}";
    }

    void AtualizarNivel(int nivel)
    {
        if (textoNivel != null) textoNivel.text = $"Nv.{nivel}";
    }

    void AtualizarTimer(float segundos)
    {
        if (textoTimer == null) return;
        int min = Mathf.FloorToInt(segundos / 60f);
        int seg = Mathf.FloorToInt(segundos % 60f);
        textoTimer.text  = $"{min:00}:{seg:00}";
        textoTimer.color = segundos <= 10f ? Color.red : Color.white;
    }

    void TogglePause()
    {
        if (pausePanel == null) return;
        bool pausado = !pausePanel.activeSelf;
        pausePanel.SetActive(pausado);
        Time.timeScale = pausado ? 0f : 1f;
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
            if (textoCooldown != null)
                textoCooldown.text = Mathf.CeilToInt(restante) + "s";
            restante -= Time.deltaTime;
            yield return null;
        }
        _poderEmCooldown = false;
        if (botaoPoderEspecial != null) botaoPoderEspecial.interactable = true;
        if (textoCooldown != null) textoCooldown.text = "";
    }
}
