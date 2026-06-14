using UnityEngine;
using UnityEngine.UI;

namespace Solengard.UI
{
    // Acoes que um botao do menu pode disparar. Cobre TUDO que hoje e ligado por lambda:
    // navegacao (MainMenuManager) + compras (LojaController).
    public enum MenuAction
    {
        Nenhuma = 0,

        // ── Navegacao (MainMenuManager) ──
        Jogar,
        AbrirLoja,
        AbrirUpgrades,
        AbrirMissoes,
        AbrirRanking,
        AbrirConfiguracoes,
        AbrirOfertas,
        AbrirBencaos,
        AbrirBaus,
        Fechar,
        ColetarRecompensa,

        // ── Loja (LojaController) — usam o campo 'parametro' ──
        ComprarClasse,   // parametro = classId            (ex: "mage")
        ComprarUpgrade,  // parametro = PermanentUpgradeId  (ex: "Poder")
        ComprarPacote,   // parametro = productId           (ex: "pacote_diamantes_3")
        AssistirVideo,
    }

    // Passo 3 da refatoracao do MainMenu (Pilar B).
    //
    // Componente robusto de bind de botao: religa-se sozinho no Start() e despacha a acao
    // chamando os METODOS PUBLICOS de MainMenuManager/LojaController — nunca reimplementa
    // a logica. Generaliza o padrao ja validado de BotaoFecharPainel/BotaoComprarUpgrade.
    // Nao depende de lambda serializada: a acao e um enum serializado no Inspector/Editor.
    [RequireComponent(typeof(Button))]
    public class MenuButtonAction : MonoBehaviour
    {
        [Tooltip("Acao disparada ao clicar neste botao.")]
        public MenuAction acao = MenuAction.Nenhuma;

        [Tooltip("Parametro para acoes de compra: classId / PermanentUpgradeId / productId.")]
        public string parametro;

        MainMenuManager menuCache;
        LojaController  lojaCache;

        // Resolucao robusta no momento do clique (nunca lambda serializada).
        // MainMenuManager vive no Canvas (sempre ativo). LojaController expoe Instance,
        // mas so apos o PainelLoja ativar a 1a vez — por isso ha fallback por busca.
        MainMenuManager Menu => menuCache != null
            ? menuCache
            : (menuCache = FindAnyObjectByType<MainMenuManager>(FindObjectsInactive.Include));

        LojaController Loja
        {
            get
            {
                if (LojaController.Instance != null) return LojaController.Instance;
                if (lojaCache != null) return lojaCache;
                return lojaCache = FindAnyObjectByType<LojaController>(FindObjectsInactive.Include);
            }
        }

        void Start()
        {
            var btn = GetComponent<Button>();
            if (btn == null) { Debug.LogWarning($"[MenuButtonAction] SEM Button em {name}"); return; }
            btn.onClick.AddListener(Executar);
        }

        public void Executar()
        {
            switch (acao)
            {
                case MenuAction.Jogar:              Menu?.LoadGameScene();             break;
                case MenuAction.AbrirLoja:          Menu?.AbrirLoja();                 break;
                case MenuAction.AbrirUpgrades:      Menu?.AbrirUpgrades();             break;
                case MenuAction.AbrirMissoes:       Menu?.AbrirMissoes();              break;
                case MenuAction.AbrirRanking:       Menu?.AbrirRanking();              break;
                case MenuAction.AbrirConfiguracoes: Menu?.AbrirConfiguracoes();        break;
                case MenuAction.AbrirOfertas:       Menu?.AbrirOfertas();              break;
                case MenuAction.AbrirBencaos:       Menu?.AbrirBencaos();              break;
                case MenuAction.AbrirBaus:          Menu?.AbrirBaus();                 break;
                case MenuAction.Fechar:             Menu?.FecharTodos();               break;
                case MenuAction.ColetarRecompensa:  Menu?.ColetarRecompensa();         break;

                case MenuAction.ComprarClasse:      ComprarClasse();                   break;
                case MenuAction.ComprarUpgrade:     ComprarUpgrade();                  break;
                case MenuAction.ComprarPacote:      Loja?.ComprarDiamantes(parametro); break;
                case MenuAction.AssistirVideo:      Loja?.AssistirVideo();             break;

                case MenuAction.Nenhuma:
                default:
                    Debug.LogWarning($"[MenuButtonAction] Acao nao configurada em '{name}'");
                    break;
            }
        }

        // ComprarClasse exige (classId, preco). O preco e buscado na fonte autoritativa
        // (LojaController.GetClasses) pelo classId — sem duplicar precos no botao.
        void ComprarClasse()
        {
            var loja = Loja;
            if (loja == null) { Debug.LogWarning("[MenuButtonAction] LojaController ausente p/ ComprarClasse"); return; }
            foreach (var (id, _, preco) in LojaController.GetClasses())
                if (id == parametro) { loja.ComprarClasse(id, preco); return; }
            Debug.LogWarning($"[MenuButtonAction] classId '{parametro}' nao encontrado em GetClasses()");
        }

        void ComprarUpgrade()
        {
            var loja = Loja;
            if (loja == null) { Debug.LogWarning("[MenuButtonAction] LojaController ausente p/ ComprarUpgrade"); return; }
            if (System.Enum.TryParse<PermanentUpgradeId>(parametro, out var id))
                loja.ComprarUpgrade(id);
            else
                Debug.LogWarning($"[MenuButtonAction] '{parametro}' nao e um PermanentUpgradeId valido");
        }
    }
}
