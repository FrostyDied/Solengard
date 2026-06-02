using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [SerializeField] CanvasGroup     canvasGroup;
    [SerializeField] TextMeshProUGUI titulotexto;
    [SerializeField] TextMeshProUGUI subtitulo;
    [SerializeField] TextMeshProUGUI statsTexto;
    [SerializeField] Button          restartButton;
    [SerializeField] Button          mainMenuButton;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    void OnEnable()  { ZoneManager.OnGameOver += ShowGameOver; }
    void OnDisable() { ZoneManager.OnGameOver -= ShowGameOver; }

    void ShowGameOver(string motivo)
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        if (titulotexto != null) titulotexto.text = "VOCÊ CAIU";
        if (subtitulo   != null) subtitulo.text   = motivo;
        if (statsTexto  != null)
        {
            int zona  = ZoneManager.Instance != null ? ZoneManager.Instance.CurrentZone + 1 : 1;
            int kills = ZoneManager.Instance != null ? ZoneManager.Instance.KillCount        : 0;
            var sc    = Object.FindFirstObjectByType<ScoreSystem>();
            int score = sc != null ? sc.ScoreAtual : 0;
            statsTexto.text = $"Zona {zona} — {kills} eliminados — {score} pts";
        }

        canvasGroup.DOFade(1f, 0.5f).SetUpdate(true);

        restartButton?.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });

        mainMenuButton?.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        });
    }
}
