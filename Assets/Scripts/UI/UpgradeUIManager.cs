using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Exibe o painel de 3 opções de upgrade entre waves.
// Pausa o jogo ao abrir (timeScale=0) e retoma ao escolher.
public class UpgradeUIManager : MonoBehaviour
{
    [Header("Painel principal")]
    public GameObject painelUpgrade;

    [Header("Cards (3 elementos, mesma ordem)")]
    public Image[]   iconesCards    = new Image[3];
    public TextMeshProUGUI[] nomesCards  = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] descsCards  = new TextMeshProUGUI[3];
    public Button[]  botoesCards   = new Button[3];

    [Header("Referências")]
    public UpgradeSystem upgradeSystem;

    List<UpgradeOption> opcoesAtuais = new();

    void OnEnable()  => UpgradeSystem.OnUpgradeOptionsReady += ExibirOpcoes;
    void OnDisable() => UpgradeSystem.OnUpgradeOptionsReady -= ExibirOpcoes;

    void Start()
    {
        painelUpgrade?.SetActive(false);

        // Conecta cada botão ao seu índice
        for (int i = 0; i < botoesCards.Length; i++)
        {
            int idx = i;
            botoesCards[i]?.onClick.AddListener(() => EscolherUpgrade(idx));
        }
    }

    void ExibirOpcoes(List<UpgradeOption> opcoes)
    {
        opcoesAtuais = opcoes;

        for (int i = 0; i < 3; i++)
        {
            bool temOpcao = i < opcoes.Count;

            if (botoesCards.Length > i)
                botoesCards[i]?.gameObject.SetActive(temOpcao);

            if (!temOpcao) continue;

            UpgradeOption op = opcoes[i];

            if (iconesCards.Length > i && iconesCards[i] != null)
            {
                iconesCards[i].sprite  = op.icone;
                iconesCards[i].enabled = op.icone != null;
            }
            if (nomesCards.Length > i && nomesCards[i] != null)
                nomesCards[i].text = op.nome;
            if (descsCards.Length > i && descsCards[i] != null)
                descsCards[i].text = op.descricao;
        }

        painelUpgrade?.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("[UpgradeUIManager] Painel de upgrade aberto.");
    }

    void EscolherUpgrade(int indice)
    {
        if (indice < 0 || indice >= opcoesAtuais.Count) return;

        upgradeSystem?.ApplyUpgrade(opcoesAtuais[indice]);
        FecharPainel();
    }

    void FecharPainel()
    {
        painelUpgrade?.SetActive(false);
        Time.timeScale = 1f;
        opcoesAtuais.Clear();
        Debug.Log("[UpgradeUIManager] Painel fechado. Jogo retomado.");
    }
}
