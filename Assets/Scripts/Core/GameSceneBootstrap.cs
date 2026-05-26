using UnityEngine;

// Attach em um GameObject na cena de jogo.
// Garante que StartGame() seja chamado ao carregar a cena via SceneManager.
public class GameSceneBootstrap : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameSceneBootstrap] GameManager não encontrado — verifique se existe um GameManager persistente.");
            return;
        }

        if (GameManager.Instance.CurrentState == GameState.MainMenu)
            GameManager.Instance.StartGame();
    }
}
