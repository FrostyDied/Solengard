using UnityEngine;

// [DefaultExecutionOrder(50)]: garante que Start() roda APÓS GameManager (ordem 0),
// assegurando que o estado MainMenu já foi setado antes de chamarmos StartGame().
[DefaultExecutionOrder(50)]
public class GameSceneBootstrap : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameSceneBootstrap] GameManager não encontrado — verifique se existe um GameManager persistente.");
            return;
        }

        var state = GameManager.Instance.CurrentState;

        // Aceita MainMenu (carga inicial) ou GameOver (GameManager persistindo entre runs
        // sem ter passado por RestartRun — edge case de testes no editor)
        if (state == GameState.MainMenu || state == GameState.GameOver)
        {
            Debug.Log($"[GameSceneBootstrap] Estado {state} — iniciando StartGame()");
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.Log($"[GameSceneBootstrap] Estado {state} — StartGame não invocado (já em execução).");
        }
    }
}
