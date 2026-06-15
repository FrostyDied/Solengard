using UnityEngine;
using UnityEngine.UI;

namespace Solengard.UI
{
    [RequireComponent(typeof(Button))]
    public class BotaoComprarUpgrade : MonoBehaviour
    {
        public string upgradeId;

        void Start()
        {
            var btn = GetComponent<Button>();
            if (btn != null) {
                btn.onClick.AddListener(OnClick);
                Debug.Log($"[CompraUpgrade] Listener registrado em {gameObject.name}, upgradeId='{upgradeId}'");
            } else {
                Debug.LogWarning($"[CompraUpgrade] SEM Button em {gameObject.name}");
            }
        }

        void OnClick()
        {
            Debug.Log($"[CompraUpgrade] CLIQUE em {gameObject.name}, upgradeId='{upgradeId}'");
            var loja = FindAnyObjectByType<LojaController>();
            if (loja == null) { Debug.LogWarning("[CompraUpgrade] LojaController NULL"); return; }
            if (System.Enum.TryParse<PermanentUpgradeId>(upgradeId, out var id)) {
                Debug.Log($"[CompraUpgrade] Chamando ComprarUpgrade({id})");
                loja.ComprarUpgrade(id);
            } else {
                Debug.LogWarning($"[CompraUpgrade] upgradeId '{upgradeId}' não é um PermanentUpgradeId válido");
            }
        }
    }
}
