using UnityEngine;
using UnityEngine.UI;

namespace Solengard.UI
{
    // Liga o botão X ao FecharTodos do MainMenuManager em runtime.
    // Anexado pelo Editor via CriarBotaoFechar — zero wire manual.
    [RequireComponent(typeof(Button))]
    public class BotaoFecharPainel : MonoBehaviour
    {
        void Start()
        {
            var btn = GetComponent<Button>();
            var mmm = FindAnyObjectByType<MainMenuManager>();
            if (btn != null && mmm != null)
                btn.onClick.AddListener(mmm.FecharTodos);
        }
    }
}
