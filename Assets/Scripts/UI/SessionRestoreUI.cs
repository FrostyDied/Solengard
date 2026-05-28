using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach no SessionRestoreCanvas da cena MainMenu.
// Exibe popup quando GameManager.OnSessionFound e dispara as ações do jogador.
public class SessionRestoreUI : MonoBehaviour
{
    [SerializeField] GameObject      panel;
    [SerializeField] TextMeshProUGUI textoDetalhes;
    [SerializeField] Button          botaoContinuar;
    [SerializeField] Button          botaoNovaRun;

    RunSessionData sessionData;

    void OnEnable()
    {
        GameManager.OnSessionFound += OnSessionFound;
    }

    void OnDisable()
    {
        GameManager.OnSessionFound -= OnSessionFound;
    }

    void Start()
    {
        panel?.SetActive(false);
        botaoContinuar?.onClick.AddListener(OnContinuar);
        botaoNovaRun?.onClick.AddListener(OnNovaRun);
    }

    void OnSessionFound(RunSessionData session)
    {
        sessionData = session;

        int mm = Mathf.FloorToInt(session.timeElapsed / 60f);
        int ss = Mathf.FloorToInt(session.timeElapsed % 60f);

        if (textoDetalhes != null)
            textoDetalhes.text = $"Wave {session.currentWave}  {session.killCount} kills  {mm:00}:{ss:00}";

        panel?.SetActive(true);
        Debug.Log($"[SessionRestoreUI] Popup exibido — wave={session.currentWave} kills={session.killCount}");
    }

    void OnContinuar()
    {
        Debug.Log("[SessionRestoreUI] Jogador escolheu CONTINUAR sessao");
        panel?.SetActive(false);
        // Session still active — GameManager.StartGame() will auto-restore it
        Object.FindFirstObjectByType<MainMenuManager>()?.LoadGameScene();
    }

    void OnNovaRun()
    {
        Debug.Log("[SessionRestoreUI] Jogador escolheu NOVA RUN");
        panel?.SetActive(false);
        RunSessionManager.Instance?.ClearSession();
        Object.FindFirstObjectByType<MainMenuManager>()?.LoadGameScene();
    }
}
