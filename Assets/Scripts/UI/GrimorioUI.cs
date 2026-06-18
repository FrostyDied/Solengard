using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Solengard.UI
{
    // Apresentacao GRIMORIO (livro) da tela de Upgrades — EXPERIMENTO alternativo ao grid.
    // Painel proprio (PainelUpgradesGrimorio); o grid permanece intacto. Folheia 1 categoria
    // por pagina (swipe horizontal + setas, clamp estilo livro). Reusa 100% os dados de
    // PermanentUpgradeSystem e a compra via LojaController.ComprarUpgrade. So apresentacao.
    //
    // ── PONTOS UNICOS DE ENCAIXE (Inspector deste componente) ──────────────────────
    //  (a) spriteFundoGrimorio  : fundo UNICO (mesma arte nas 6 paginas; placeholder por cor)
    //  (b) runaAcesa           : runa de nivel adquirido
    //  (c) runaApagada         : runa de nivel restante
    //  (d) icones[]            : id -> sprite do upgrade (pre-preenchido com os 16 ids)
    // Tudo funciona sem as imagens (placeholders).
    public class GrimorioUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [System.Serializable]
        public struct IconeUpgrade { public PermanentUpgradeId id; public Sprite icone; }

        [Header("Encaixe de PNGs (placeholder enquanto vazio)")]
        [Tooltip("(a) fundo UNICO do grimorio (mesma arte nas 6 paginas).")]
        [SerializeField] Sprite spriteFundoGrimorio;
        [Tooltip("(b) runa de nivel adquirido.")]
        [SerializeField] Sprite runaAcesa;
        [Tooltip("(c) runa de nivel restante.")]
        [SerializeField] Sprite runaApagada;
        [Tooltip("(d) mapa id->sprite (pre-preenchido pelo builder).")]
        [SerializeField] IconeUpgrade[] icones;

        // Categorias na ordem das paginas (mesma do grid/legado).
        static readonly (string nome, PermanentUpgradeId[] ids)[] Categorias =
        {
            ("Ofensa",     new[]{ PermanentUpgradeId.Poder, PermanentUpgradeId.Recarga }),
            ("Defesa",     new[]{ PermanentUpgradeId.Armadura, PermanentUpgradeId.VidaMaxima, PermanentUpgradeId.Recuperacao }),
            ("Ataque",     new[]{ PermanentUpgradeId.Area, PermanentUpgradeId.Velocidade, PermanentUpgradeId.Duracao, PermanentUpgradeId.Quantidade }),
            ("Mobilidade", new[]{ PermanentUpgradeId.Movimento, PermanentUpgradeId.Magnetismo }),
            ("Progressão", new[]{ PermanentUpgradeId.Crescimento, PermanentUpgradeId.Riqueza }),
            ("Especiais",  new[]{ PermanentUpgradeId.Maldicao, PermanentUpgradeId.Ressurreicao, PermanentUpgradeId.PoderEspecial }),
        };

        static readonly Color OURO        = new Color(0.78f, 0.65f, 0.20f);
        static readonly Color QUAD_ACESO  = OURO;
        static readonly Color QUAD_APAGADO= new Color(0.18f, 0.16f, 0.26f, 1f);
        static readonly Color ICONE_PLACE = new Color(0.16f, 0.13f, 0.28f, 1f);
        static readonly Color BTN_OK      = new Color(0.35f, 0.06f, 0.60f);
        static readonly Color BTN_OFF     = new Color(0.25f, 0.25f, 0.28f);
        // Cor placeholder (pergaminho escuro) ate o PNG de fundo unico entrar.
        static readonly Color FUNDO_PLACE = new Color(0.10f, 0.07f, 0.05f, 0.92f);
        // Moldura "selo" do icone: aro dourado -> aro escuro (emoldura o quadrado colorido do PNG).
        static readonly Color MOLDURA_OURO   = new Color(0.62f, 0.48f, 0.16f, 1f);
        static readonly Color MOLDURA_ESCURA = new Color(0.09f, 0.07f, 0.05f, 1f);
        // Fundo do card (preto translucido) -> contraste do texto sobre o pergaminho.
        static readonly Color CARD_BG        = new Color(0f, 0f, 0f, 0.34f);
        static readonly Color TEXTO_DESC     = new Color(0.90f, 0.87f, 0.96f);

        Image _fundo;
        TextMeshProUGUI _titulo, _saldo;
        Transform _entradas;
        Button _btnPrev, _btnNext;
        int  _pagina;
        bool _bound;
        Vector2 _dragInicio;
        const float SWIPE_MIN = 90f; // threshold px p/ confirmar a virada (anti-toque acidental)

        void OnEnable()
        {
            if (!_bound) Bind();
            DiamondSystem.OnDiamondsChanged += AoMudarDiamantes;
            MostrarPagina(_pagina);
            AtualizarSaldo(DiamondSystem.Instance?.GetBalance() ?? 0);
        }

        void OnDisable() => DiamondSystem.OnDiamondsChanged -= AoMudarDiamantes;

        void Bind()
        {
            _fundo    = FindDeep("PageBackground")?.GetComponent<Image>();
            _titulo   = FindDeep("CategoriaTitulo")?.GetComponent<TextMeshProUGUI>();
            _saldo    = FindDeep("TextoSaldo")?.GetComponent<TextMeshProUGUI>();
            _entradas = FindDeep("Entradas");
            _btnPrev  = FindDeep("BtnPrev")?.GetComponent<Button>();
            _btnNext  = FindDeep("BtnNext")?.GetComponent<Button>();
            if (_btnPrev != null) { _btnPrev.onClick.RemoveListener(PaginaAnterior); _btnPrev.onClick.AddListener(PaginaAnterior); }
            if (_btnNext != null) { _btnNext.onClick.RemoveListener(ProximaPagina);  _btnNext.onClick.AddListener(ProximaPagina); }
            _bound = true;
            if (_entradas == null)
                Debug.LogWarning("[Grimorio] 'Entradas' nao encontrado — rode 'Solengard/Grimorio: Construir Painel'.");
        }

        // ── Folhear (clamp estilo livro) ─────────────────────────────────────────────
        public void ProximaPagina()  { if (_pagina < Categorias.Length - 1) MostrarPagina(_pagina + 1); }
        public void PaginaAnterior() { if (_pagina > 0)                      MostrarPagina(_pagina - 1); }

        // Swipe: detecta o gesto na raiz do painel (filhos nao sao IDragHandler -> evento sobe).
        public void OnBeginDrag(PointerEventData e) => _dragInicio = e.position;
        public void OnDrag(PointerEventData e) { /* sem feedback visual por ora */ }
        public void OnEndDrag(PointerEventData e)
        {
            Vector2 d = e.position - _dragInicio;
            if (Mathf.Abs(d.x) < SWIPE_MIN || Mathf.Abs(d.x) <= Mathf.Abs(d.y)) return; // horizontal dominante + threshold
            if (d.x < 0) ProximaPagina(); else PaginaAnterior();
        }

        void MostrarPagina(int p)
        {
            _pagina = Mathf.Clamp(p, 0, Categorias.Length - 1);
            var (nome, ids) = Categorias[_pagina];

            if (_titulo != null) _titulo.text = $"{nome} · {_pagina + 1}/{Categorias.Length}";

            // Fundo UNICO do grimorio (mesma arte nas 6 paginas); cor placeholder ate o PNG.
            if (_fundo != null)
            {
                if (spriteFundoGrimorio != null) { _fundo.sprite = spriteFundoGrimorio; _fundo.color = Color.white; _fundo.type = Image.Type.Simple; }
                else                             { _fundo.sprite = null; _fundo.color = FUNDO_PLACE; }
            }

            if (_btnPrev != null) _btnPrev.interactable = _pagina > 0;
            if (_btnNext != null) _btnNext.interactable = _pagina < Categorias.Length - 1;

            ConstruirEntradas(ids);
        }

        void ConstruirEntradas(PermanentUpgradeId[] ids)
        {
            if (_entradas == null) return;
            for (int i = _entradas.childCount - 1; i >= 0; i--) Destroy(_entradas.GetChild(i).gameObject);

            // So entradas com dados validos contam pro empilhamento.
            var validos = new System.Collections.Generic.List<(PermanentUpgradeId id, PermanentUpgradeData data)>();
            foreach (var id in ids)
            {
                var data = PermanentUpgradeSystem.GetData(id);
                if (data != null) validos.Add((id, data));
            }
            int total = validos.Count;
            if (total == 0) return;

            // Altura por entrada com CAP -> 2 entradas nao "esticam"; bloco centralizado no
            // container (que ja respeita a moldura do pergaminho). Anti-vazamento: slotH nunca
            // excede o espaco disponivel, e o bloco fica dentro de [0,1].
            const float gapY = 0.03f, maxSlotH = 0.30f;
            float slotH  = Mathf.Min(maxSlotH, (1f - gapY * (total - 1)) / total);
            float blockH = slotH * total + gapY * (total - 1);
            float startTop = 1f - (1f - blockH) * 0.5f;

            for (int i = 0; i < total; i++)
            {
                float top = startTop - i * (slotH + gapY);
                float bot = top - slotH;
                CriarEntrada(validos[i].id, validos[i].data, top, bot);
            }
        }

        void CriarEntrada(PermanentUpgradeId id, PermanentUpgradeData data, float top, float bot)
        {
            var card = NovoGO($"Entrada_{id}", _entradas);
            Anchor(card.transform, 0f, bot, 1f, top);
            var cbg = card.AddComponent<Image>();
            cbg.sprite = UIRound(); cbg.type = Image.Type.Sliced;
            cbg.color = CARD_BG; cbg.raycastTarget = false;

            // ── Moldura "selo" do icone: aro dourado -> aro escuro -> icone (PNG com fundo colorido) ──
            var sp = GetIcone(id);
            var moldura = NovoGO("Moldura", card.transform);
            Anchor(moldura.transform, 0.03f, 0.18f, 0.26f, 0.86f);
            var mImg = moldura.AddComponent<Image>();
            mImg.sprite = UIRound(); mImg.type = Image.Type.Sliced; mImg.color = MOLDURA_OURO; mImg.raycastTarget = false;

            var aro = NovoGO("Aro", moldura.transform);
            Anchor(aro.transform, 0.10f, 0.10f, 0.90f, 0.90f);
            var aImg = aro.AddComponent<Image>();
            aImg.sprite = UIRound(); aImg.type = Image.Type.Sliced; aImg.color = MOLDURA_ESCURA; aImg.raycastTarget = false;

            var ico = NovoGO("Icone", aro.transform);
            Anchor(ico.transform, 0.09f, 0.09f, 0.91f, 0.91f);
            var icoImg = ico.AddComponent<Image>(); icoImg.raycastTarget = false;
            if (sp != null) { icoImg.sprite = sp; icoImg.color = Color.white; icoImg.preserveAspect = true; }
            else            { icoImg.color = ICONE_PLACE; }

            // ── Coluna direita: nome / descricao (wrap) / runas / acao ──
            var nome = NovoTexto("Nome", card.transform, data.nome, 30f, OURO, TextAlignmentOptions.Left);
            nome.fontStyle = FontStyles.Bold; Anchor(nome.transform, 0.31f, 0.78f, 0.98f, 1.00f);

            var desc = NovoTexto("Desc", card.transform, data.descricao, 20f, TEXTO_DESC, TextAlignmentOptions.TopLeft);
            Anchor(desc.transform, 0.31f, 0.50f, 0.98f, 0.74f);

            // Runas (linha dedicada -> maiores): N = maxLevel; acesas = nivel adquirido. Anchors
            // fracionarios particionando [0,1] -> sempre cabem, nunca vazam (anti-vazamento).
            var runas = NovoGO("Runas", card.transform);
            Anchor(runas.transform, 0.31f, 0.27f, 0.80f, 0.47f);
            ConstruirRunas(runas.transform, Mathf.Max(1, data.maxLevel), NivelAtual(id));

            int  nivel = NivelAtual(id);
            bool max   = nivel >= data.maxLevel;
            int  custo = PermanentUpgradeSystem.GetCusto(id, nivel);
            int  saldo = DiamondSystem.Instance?.GetBalance() ?? 0;

            var custoT = NovoTexto("Custo", card.transform,
                max ? "MÁX" : $"<sprite name=\"diamante\"> {custo:N0}", 26f, OURO, TextAlignmentOptions.Left);
            custoT.fontStyle = FontStyles.Bold;
            Anchor(custoT.transform, 0.31f, 0.03f, 0.55f, 0.24f);

            var btn  = NovoGO("BtnDesbloquear", card.transform);
            Anchor(btn.transform, 0.58f, 0.02f, 0.98f, 0.25f);
            var bimg = btn.AddComponent<Image>();
            bimg.sprite = UIRound(); bimg.type = Image.Type.Sliced;
            var bbtn = btn.AddComponent<Button>();
            bool pode = !max && saldo >= custo;
            bimg.color = pode ? BTN_OK : BTN_OFF;
            bbtn.interactable = pode;
            var blbl = NovoTexto("Label", btn.transform, max ? "MÁXIMO" : "Desbloquear", 20f, Color.white, TextAlignmentOptions.Center);
            blbl.fontStyle = FontStyles.Bold; Anchor(blbl.transform, 0.04f, 0f, 0.96f, 1f);

            var idLocal = id;
            bbtn.onClick.AddListener(() => Comprar(idLocal));
        }

        void ConstruirRunas(Transform parent, int n, int nivel)
        {
            const float gap = 0.06f;
            float w = (1f - gap * (n - 1)) / n; // particiona a largura -> nunca vaza
            for (int i = 0; i < n; i++)
            {
                var r = NovoGO($"Runa{i}", parent);
                float x0 = i * (w + gap);
                Anchor(r.transform, x0, 0f, x0 + w, 1f);
                var rimg = r.AddComponent<Image>(); rimg.raycastTarget = false; rimg.preserveAspect = true;
                bool acesa = i < nivel;
                var sp = acesa ? runaAcesa : runaApagada;
                if (sp != null) { rimg.sprite = sp; rimg.color = Color.white; }
                else            { rimg.color = acesa ? QUAD_ACESO : QUAD_APAGADO; }
            }
        }

        // ── Compra (delega a logica existente) ───────────────────────────────────────
        void Comprar(PermanentUpgradeId id)
        {
            var loja = LojaController.Instance != null
                ? LojaController.Instance
                : FindAnyObjectByType<LojaController>(FindObjectsInactive.Include);
            if (loja == null) { Debug.LogWarning("[Grimorio] LojaController ausente."); return; }
            loja.ComprarUpgrade(id);                 // mesma logica/feedback do grid
            MostrarPagina(_pagina);                   // reflete novo nivel/custo/runas
            AtualizarSaldo(DiamondSystem.Instance?.GetBalance() ?? 0);
        }

        void AoMudarDiamantes(int s) { AtualizarSaldo(s); MostrarPagina(_pagina); }
        void AtualizarSaldo(int s) { if (_saldo != null) _saldo.text = s.ToString("N0"); }

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

        // ── Helpers ──────────────────────────────────────────────────────────────────
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

        // Sprite de canto arredondado GERADO por codigo (uma vez, em cache). Nao depende de
        // recurso embutido (UI/Skin/UISprite.psd nao carrega no Unity 6 -> spam de erros).
        // Textura branca com cantos transparentes + border p/ Image.Type.Sliced escalar limpo.
        static Sprite _uiRound;
        static Sprite UIRound()
        {
            if (_uiRound != null) return _uiRound;
            const int S = 32, R = 10;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            var on = new Color32(255, 255, 255, 255);
            var off = new Color32(255, 255, 255, 0);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int cx = Mathf.Clamp(x, R, S - 1 - R);
                    int cy = Mathf.Clamp(y, R, S - 1 - R);
                    float dx = x - cx, dy = y - cy;
                    px[y * S + x] = (dx * dx + dy * dy <= R * R) ? on : off;
                }
            tex.SetPixels32(px); tex.Apply();
            _uiRound = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f),
                                     100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
            return _uiRound;
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
            tmp.textWrappingMode = TextWrappingModes.Normal; tmp.raycastTarget = false;
            return tmp;
        }
    }
}
