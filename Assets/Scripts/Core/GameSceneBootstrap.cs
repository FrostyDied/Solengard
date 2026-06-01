using UnityEngine;

// GameManager.AutoStart (via Invoke 1.5s) é responsável por iniciar o jogo na GameScene.
// Este Bootstrap existe para futuras inicializações de cena que não sejam StartGame().
[DefaultExecutionOrder(50)]
public class GameSceneBootstrap : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameSceneBootstrap] GameManager não encontrado.");
            return;
        }

        Debug.Log($"[GameSceneBootstrap] Cena pronta — estado={GameManager.Instance.CurrentState}. StartGame será chamado pelo AutoStart.");
    }
}
