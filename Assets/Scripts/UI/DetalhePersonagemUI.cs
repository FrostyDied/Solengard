using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Binder runtime do PainelDetalhePersonagem (overlay sobre a Loja). Acha os filhos por nome
// (padrao ConfigUIBinder), popula no Mostrar(classId) lendo o ClassDefinition em runtime via
// Resources.Load("Classes/{id}"). Stats/preco vem das fontes autoritativas:
//   - stats: ClassDefinition (Resources)
//   - preco: LojaController.GetClasses()
//   - posse: PlayerClassManager.IsClassUnlocked / PlayerPrefs "class_unlocked_{id}"
//   - selecionada: PlayerPrefs "selected_class"
// Compra: LojaController.ComprarClasse(id, preco). Selecao: PlayerClassManager.SelectClass(id).
// O builder (SolengardDetalhePersonagemSetup) pre-preenche o mapa splashes (classId->Sprite).
public class DetalhePersonagemUI : MonoBehaviour
{
    [System.Serializable] public struct ClasseSplash { public string classId; public Sprite splash; }
    [Tooltip("Mapa classId -> splash (pre-preenchido pelo builder).")]
    [SerializeField] ClasseSplash[] splashes;

    Image _splash;
    TextMeshProUGUI _nome, _classe, _especial, _saldo;
    TextMeshProUGUI _vVida, _vDano, _vDefesa, _vVel, _vAlcance, _vCadencia, _vAlvo;
    Button _btnAcao, _btnFechar;
    TextMeshProUGUI _lblAcao;
    string _classId;
    bool _bound;

    void OnEnable()
    {
        if (!_bound) Bind();
        DiamondSystem.OnDiamondsChanged += AoMudarSaldo;
    }

    void OnDisable() => DiamondSystem.OnDiamondsChanged -= AoMudarSaldo;

    void Bind()
    {
        _splash    = Achar("Splash")?.GetComponent<Image>();
        _nome      = Achar("Nome")?.GetComponent<TextMeshProUGUI>();
        _classe    = Achar("Classe")?.GetComponent<TextMeshProUGUI>();
        _especial  = Achar("Especial")?.GetComponent<TextMeshProUGUI>();
        _saldo     = Achar("TextoSaldo")?.GetComponent<TextMeshProUGUI>(); // saldo do header padrao
        _vVida     = Achar("Val_Vida")?.GetComponent<TextMeshProUGUI>();
        _vDano     = Achar("Val_Dano")?.GetComponent<TextMeshProUGUI>();
        _vDefesa   = Achar("Val_Defesa")?.GetComponent<TextMeshProUGUI>();
        _vVel      = Achar("Val_Velocidade")?.GetComponent<TextMeshProUGUI>();
        _vAlcance  = Achar("Val_Alcance")?.GetComponent<TextMeshProUGUI>();
        _vCadencia = Achar("Val_Cadencia")?.GetComponent<TextMeshProUGUI>();
        _vAlvo     = Achar("Val_Alvo")?.GetComponent<TextMeshProUGUI>();

        _btnAcao = Achar("BtnAcao")?.GetComponent<Button>();
        if (_btnAcao != null)
        {
            _lblAcao = _btnAcao.GetComponentInChildren<TextMeshProUGUI>(true);
            _btnAcao.onClick.RemoveListener(OnAcao);
            _btnAcao.onClick.AddListener(OnAcao);
        }
        _btnFechar = Achar("BtnFecharDetalhe")?.GetComponent<Button>();
        if (_btnFechar != null)
        {
            _btnFechar.onClick.RemoveListener(Fechar);
            _btnFechar.onClick.AddListener(Fechar);
        }
        _bound = true;
    }

    public void Mostrar(string classId)
    {
        if (!_bound) Bind();
        _classId = classId;

        var def = Resources.Load<ClassDefinition>($"Classes/{classId}");
        if (def == null) { Debug.LogWarning($"[DetalhePersonagem] ClassDefinition 'Classes/{classId}' nao encontrado."); return; }

        if (_nome   != null) _nome.text   = def.displayName;
        if (_classe != null) _classe.text = def.className;

        if (_splash != null)
        {
            var sp = SplashDe(classId);
            if (sp != null) { _splash.sprite = sp; _splash.color = Color.white; _splash.preserveAspect = true; }
        }

        SetNum(_vVida,    def.maxHP);
        SetNum(_vDano,    def.attackDamage);
        SetNum(_vDefesa,  def.defense);
        SetNum(_vVel,     def.moveSpeed,    1);
        SetNum(_vAlcance, def.attackRange,  1);
        if (_vCadencia != null) _vCadencia.text = $"{def.attackInterval:0.#}s";
        if (_vAlvo     != null) _vAlvo.text     = AlvoDe(def.attackType);

        if (_especial != null)
            _especial.text = $"{def.specialName}\n<size=70%>Recarga {def.specialCooldown:0.#}s · Dura {def.specialDuration:0.#}s</size>";

        AtualizarSaldo(DiamondSystem.Instance?.GetBalance() ?? 0);
        AtualizarBotao();

        // Pronto pra controle: foco inicial no botao de acao.
        if (_btnAcao != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(_btnAcao.gameObject);
    }

    void AtualizarBotao()
    {
        bool possui   = Possui(_classId);
        string atual  = PlayerPrefs.GetString("selected_class", "warrior");
        if (!possui)               SetBotao($"Comprar <sprite name=\"diamante\"> {Preco(_classId):N0}", true);
        else if (atual != _classId) SetBotao("Selecionar", true);
        else                        SetBotao("Selecionado", false);
    }

    void SetBotao(string txt, bool interativo)
    {
        if (_lblAcao != null) _lblAcao.text = txt;
        if (_btnAcao != null) _btnAcao.interactable = interativo;
    }

    void OnAcao()
    {
        if (string.IsNullOrEmpty(_classId)) return;
        if (!Possui(_classId))
        {
            var loja = LojaController.Instance != null
                ? LojaController.Instance
                : FindAnyObjectByType<LojaController>(FindObjectsInactive.Include);
            loja?.ComprarClasse(_classId, Preco(_classId));   // mesma logica/feedback do grid
        }
        else
        {
            PlayerClassManager.Instance?.SelectClass(_classId);
        }
        AtualizarBotao(); // reflete Comprar -> Selecionar -> Selecionado
    }

    // Overlay: so desativa o detalhe; a Loja continua ativa atras.
    void Fechar() => gameObject.SetActive(false);

    void AoMudarSaldo(int s) { AtualizarSaldo(s); AtualizarBotao(); }
    void AtualizarSaldo(int s) { if (_saldo != null) _saldo.text = s.ToString("N0"); }

    // ── Helpers ──────────────────────────────────────────────────────────────────
    static bool Possui(string id)
    {
        var pcm = PlayerClassManager.Instance;
        return pcm != null ? pcm.IsClassUnlocked(id)
                           : PlayerPrefs.GetInt($"class_unlocked_{id}", 0) == 1;
    }

    static int Preco(string id)
    {
        foreach (var (cid, _, preco) in LojaController.GetClasses())
            if (cid == id) return preco;
        return 0;
    }

    static string AlvoDe(AttackType t) => t == AttackType.RangedSingle ? "Único" : "Área";

    Sprite SplashDe(string id)
    {
        if (splashes != null)
            foreach (var e in splashes) if (e.classId == id) return e.splash;
        return null;
    }

    static void SetNum(TextMeshProUGUI t, float v, int dec = 0)
    {
        if (t != null) t.text = v.ToString(dec == 0 ? "0" : "0.#");
    }

    Transform Achar(string nome)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == nome) return t;
        return null;
    }
}
