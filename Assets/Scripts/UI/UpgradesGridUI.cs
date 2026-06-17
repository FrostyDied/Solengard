using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Solengard.UI
{
    // Apresentacao em GRID da aba Upgrades da Loja (substitui a lista antiga).
    // Padrao binder (igual MissoesUIBinder/ConfigUIBinder): anexado a AbaUpgrades, encontra
    // por nome os containers criados pelo editor builder (GridContainer + DetailPanel) e
    // monta os cards em runtime. NAO reimplementa compra/saldo — delega a
    // LojaController.ComprarUpgrade + PermanentUpgradeSystem (mesma logica da lista).
    //
    // ─────────────────────────────────────────────────────────────────────────────
    //  PONTO UNICO DE ICONES  ->  campo serializado 'icones' (abaixo).
    //  O editor builder pre-preenche um slot por upgrade (sprite vazio = placeholder).
    //  Para encaixar a arte depois: Inspector do AbaUpgrades -> UpgradesGridUI -> Icones
    //  -> arraste o Sprite no slot do upgrade correspondente. Zero refatoracao.
    // ─────────────────────────────────────────────────────────────────────────────
    public class UpgradesGridUI : MonoBehaviour
    {
        [System.Serializable]
        public struct IconeUpgrade
        {
            public PermanentUpgradeId id;
            public Sprite             icone; // vazio => placeholder (quadrado solido)
        }

        [Header("Icones por upgrade (placeholder enquanto vazio)")]
        [Tooltip("Mapa id->sprite. Pre-preenchido pelo builder; arraste os sprites depois.")]
        [SerializeField] IconeUpgrade[] icones;

        // ── Paleta dark fantasy ──────────────────────────────────────────────────
        static readonly Color OURO          = new Color(0.78f, 0.65f, 0.20f);
        static readonly Color CARD_NORMAL   = new Color(0.12f, 0.07f, 0.22f, 0.95f);
        static readonly Color CARD_SEL      = new Color(0.28f, 0.10f, 0.50f, 0.97f);
        static readonly Color BORDA_NORMAL  = new Color(0.45f, 0.30f, 0.12f, 0.9f); // dourado apagado
        static readonly Color BORDA_SEL     = OURO;
        static readonly Color QUAD_CHEIO    = OURO;
        static readonly Color QUAD_VAZIO    = new Color(0.18f, 0.16f, 0.26f, 1f);
        static readonly Color ICONE_PLACE   = new Color(0.16f, 0.13f, 0.28f, 1f);
        static readonly Color BTN_OK        = new Color(0.35f, 0.06f, 0.60f);
        static readonly Color BTN_OFF       = new Color(0.25f, 0.25f, 0.28f);

        Transform        _grid;
        TextMeshProUGUI  _detNome, _detDesc, _detCusto;
        Button           _btnComprar;
        TextMeshProUGUI  _btnComprarLabel;

        readonly List<(PermanentUpgradeId id, Image bg, Outline borda, Transform progresso)> _cards = new();
        PermanentUpgradeId _selecionado;
        bool _temSelecao;
        bool _bound;
        Vector2 _cellSize = new Vector2(128f, 146f); // lido do GridLayoutGroup em Bind() (fallback)

        // Ordem de exibicao = ordem do enum (todos os upgrades atuais).
        static readonly PermanentUpgradeId[] Ordem = (PermanentUpgradeId[])System.Enum.GetValues(typeof(PermanentUpgradeId));

        void OnEnable()
        {
            if (!_bound) Bind();
            DiamondSystem.OnDiamondsChanged += AoMudarDiamantes;
            ConstruirCards();
            RefreshTudo();
        }

        void OnDisable()
        {
            DiamondSystem.OnDiamondsChanged -= AoMudarDiamantes;
        }

        void Bind()
        {
            _grid            = FindDeep("GridContainer");
            var glg = _grid != null ? _grid.GetComponent<GridLayoutGroup>() : null;
            if (glg != null) _cellSize = glg.cellSize; // dimensiona os internos do card a partir daqui
            _detNome         = FindDeep("Det_Nome")?.GetComponent<TextMeshProUGUI>();
            _detDesc         = FindDeep("Det_Descricao")?.GetComponent<TextMeshProUGUI>();
            _detCusto        = FindDeep("Det_Custo")?.GetComponent<TextMeshProUGUI>();
            var btnTr        = FindDeep("BtnComprar");
            _btnComprar      = btnTr?.GetComponent<Button>();
            _btnComprarLabel = btnTr?.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_btnComprar != null)
            {
                _btnComprar.onClick.RemoveListener(Comprar);
                _btnComprar.onClick.AddListener(Comprar);
            }
            _bound = true;
            if (_grid == null)
                Debug.LogWarning("[UpgradesGrid] GridContainer nao encontrado — rode 'Solengard/Upgrades: Construir Grid (Loja)'.");
        }

        // ── Construcao do grid ───────────────────────────────────────────────────
        void ConstruirCards()
        {
            if (_grid == null) return;
            for (int i = _grid.childCount - 1; i >= 0; i--) Destroy(_grid.GetChild(i).gameObject);
            _cards.Clear();

            foreach (var id in Ordem)
            {
                var data = PermanentUpgradeSystem.GetData(id);
                if (data == null) continue;
                CriarCard(id, data);
            }

            if (!_temSelecao && _cards.Count > 0) { _selecionado = _cards[0].id; _temSelecao = true; }
        }

        void CriarCard(PermanentUpgradeId id, PermanentUpgradeData data)
        {
            var card = NovoGO($"Card_{id}", _grid);
            var bg   = card.AddComponent<Image>(); bg.color = CARD_NORMAL;
            var borda = card.AddComponent<Outline>();
            borda.effectColor = BORDA_NORMAL; borda.effectDistance = new Vector2(3f, 3f);
            var btn  = card.AddComponent<Button>();
            // (GridLayoutGroup controla tamanho/posicao do card.)

            // Nome (topo) — autosize p/ caber na largura do card; nomes longos quebram em 2 linhas
            var nome = NovoTexto("Nome", card.transform, data.nome, 15f, Color.white, TextAlignmentOptions.Center);
            Anchor(nome.transform, 0.06f, 0.70f, 0.94f, 0.97f);
            nome.enableAutoSizing = true; nome.fontSizeMin = 8f; nome.fontSizeMax = 0.12f * _cellSize.x; // escala c/ o card

            // Icone (centro, quadrado) — sprite real se mapeado, senao placeholder solido
            var ico = NovoGO("Icone", card.transform);
            var icoImg = ico.AddComponent<Image>();
            var sprite = GetIcone(id);
            if (sprite != null) { icoImg.sprite = sprite; icoImg.color = Color.white; icoImg.preserveAspect = true; }
            else                { icoImg.color = ICONE_PLACE; }
            icoImg.raycastTarget = false;
            Anchor(ico.transform, 0.30f, 0.30f, 0.70f, 0.66f);

            // Progresso: fileira de quadradinhos PEQUENOS, dimensionados em px p/ CABER sempre na
            // largura interna do card. Posicionamento manual (sem LayoutGroup) -> deterministico,
            // nunca vaza (era esse o bug do childControlWidth=false). cheios = niveis adquiridos.
            var prog = NovoGO("Progresso", card.transform);
            Anchor(prog.transform, 0.08f, 0.06f, 0.92f, 0.24f);
            int n = Mathf.Max(1, data.maxLevel);
            float containerW = (0.92f - 0.08f) * _cellSize.x; // largura interna real da fileira
            float containerH = (0.24f - 0.06f) * _cellSize.y;
            const float gap = 3f;
            float sqMax = 0.11f * _cellSize.x; // teto escala com o card (a formula ainda impede vazar)
            float sq = Mathf.Floor(Mathf.Min((containerW - (n - 1) * gap) / n, containerH, sqMax));
            if (sq < 1f) sq = 1f;
            float totalW = n * sq + (n - 1) * gap;
            float startX = (containerW - totalW) * 0.5f; // centraliza a fileira no container
            for (int i = 0; i < n; i++)
            {
                var q = NovoGO($"Q{i}", prog.transform);
                var qImg = q.AddComponent<Image>(); qImg.color = QUAD_VAZIO; qImg.raycastTarget = false;
                var qrt = (RectTransform)q.transform;
                qrt.anchorMin = new Vector2(0f, 0.5f); qrt.anchorMax = new Vector2(0f, 0.5f);
                qrt.pivot     = new Vector2(0f, 0.5f);
                qrt.sizeDelta = new Vector2(sq, sq);
                qrt.anchoredPosition = new Vector2(startX + i * (sq + gap), 0f);
            }

            var idLocal = id;
            btn.onClick.AddListener(() => Selecionar(idLocal));
            _cards.Add((id, bg, borda, prog.transform));
        }

        // ── Selecao + detalhe ────────────────────────────────────────────────────
        void Selecionar(PermanentUpgradeId id)
        {
            _selecionado = id; _temSelecao = true;
            RefreshSelecaoVisual();
            RefreshDetalhe();
        }

        void RefreshSelecaoVisual()
        {
            foreach (var (id, bg, borda, _) in _cards)
            {
                bool sel = _temSelecao && id.Equals(_selecionado);
                bg.color = sel ? CARD_SEL : CARD_NORMAL;
                borda.effectColor = sel ? BORDA_SEL : BORDA_NORMAL;
                borda.effectDistance = sel ? new Vector2(4f, 4f) : new Vector2(3f, 3f);
            }
        }

        void RefreshDetalhe()
        {
            if (!_temSelecao) return;
            var data = PermanentUpgradeSystem.GetData(_selecionado);
            if (data == null) return;

            int nivel = NivelAtual(_selecionado);
            bool max  = nivel >= data.maxLevel;

            if (_detNome != null) _detNome.text = data.nome;
            if (_detDesc != null) _detDesc.text = $"{data.descricao}\n<color=#B0A0D0>Nível {nivel}/{data.maxLevel}  ·  {ResumoMaximo(data)}</color>";

            int custo = PermanentUpgradeSystem.GetCusto(_selecionado, nivel);
            int saldo = DiamondSystem.Instance?.GetBalance() ?? 0;

            if (_detCusto != null)
                _detCusto.text = max ? "<color=#FFD700>NÍVEL MÁXIMO</color>"
                                     : $"Custo: <sprite name=\"diamante\"> {custo:N0}";

            if (_btnComprar != null)
            {
                bool pode = !max && saldo >= custo;
                _btnComprar.interactable = pode;
                var img = _btnComprar.GetComponent<Image>();
                if (img != null) img.color = pode ? BTN_OK : BTN_OFF;
                if (_btnComprarLabel != null)
                    _btnComprarLabel.text = max ? "MÁXIMO" : "COMPRAR";
            }
        }

        // Resumo do teto do upgrade (ex.: "máx. +40%"). Tipos especiais tratados a parte.
        static string ResumoMaximo(PermanentUpgradeData d)
        {
            float total = d.incrementoPerLevel * d.maxLevel;
            switch (d.id)
            {
                case PermanentUpgradeId.Armadura:     return $"máx. -{total:0} de dano";
                case PermanentUpgradeId.Quantidade:   return $"máx. +{d.maxLevel} projéteis";
                case PermanentUpgradeId.Ressurreicao: return "revive 1× por run";
                case PermanentUpgradeId.Recuperacao:  return $"máx. +{total:0.0} HP/s";
                case PermanentUpgradeId.PoderEspecial:return $"máx. -{total:0}s recarga";
                default:                              return $"máx. +{total * 100f:0}%";
            }
        }

        // ── Compra (delega a logica existente) ───────────────────────────────────
        void Comprar()
        {
            if (!_temSelecao) return;
            var loja = LojaController.Instance != null
                ? LojaController.Instance
                : FindAnyObjectByType<LojaController>(FindObjectsInactive.Include);
            if (loja == null) { Debug.LogWarning("[UpgradesGrid] LojaController ausente."); return; }
            loja.ComprarUpgrade(_selecionado); // mesma logica/feedback da lista antiga
            RefreshTudo();                     // reflete novo nivel/custo/saldo
        }

        void AoMudarDiamantes(int _) => RefreshDetalhe();

        void RefreshTudo()
        {
            foreach (var (id, _, _, progresso) in _cards) RefreshProgresso(id, progresso);
            RefreshSelecaoVisual();
            RefreshDetalhe();
        }

        void RefreshProgresso(PermanentUpgradeId id, Transform progresso)
        {
            int nivel = NivelAtual(id);
            for (int i = 0; i < progresso.childCount; i++)
            {
                var img = progresso.GetChild(i).GetComponent<Image>();
                if (img != null) img.color = i < nivel ? QUAD_CHEIO : QUAD_VAZIO;
            }
        }

        // ── Dados ─────────────────────────────────────────────────────────────────
        // Nivel via singleton; fallback PlayerPrefs (mesma chave do sistema) se ausente.
        static int NivelAtual(PermanentUpgradeId id) =>
            PermanentUpgradeSystem.Instance != null
                ? PermanentUpgradeSystem.Instance.GetLevel(id)
                : PlayerPrefs.GetInt($"perm_upgrade_{id}", 0);

        Sprite GetIcone(PermanentUpgradeId id)
        {
            if (icones != null)
                foreach (var e in icones) if (e.id == id) return e.icone;
            return null;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────
        Transform FindDeep(string nome)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == nome) return t;
            return null;
        }

        static void Anchor(Transform t, float minX, float minY, float maxX, float maxY)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = new Vector2(minX, minY); rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static GameObject NovoGO(string nome, Transform parent)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static TextMeshProUGUI NovoTexto(string nome, Transform parent, string texto, float size, Color cor, TextAlignmentOptions align)
        {
            var go = NovoGO(nome, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = texto; tmp.fontSize = size; tmp.color = cor; tmp.alignment = align;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal; tmp.raycastTarget = false;
            return tmp;
        }
    }
}
