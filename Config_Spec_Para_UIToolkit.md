# Solengard — Tela de Configurações: Spec para tradução UXML/USS

Este documento reúne TUDO que você precisa para traduzir a tela de Configurações
(desenhada no Claude Design) para Unity UI Toolkit (UXML + USS), na cena
`MainMenu_UIToolkit_Teste.unity`.

**Fontes de verdade, em ordem de prioridade:**
1. As **3 imagens** anexadas (abas Áudio, Vídeo, Jogo renderizadas) — referência visual definitiva.
2. Os **tokens de design** (seção 1) — cores, espaçamentos, fontes.
3. O **CSS de referência** (seção 3) — implementação fiel de cada componente.
4. O **markup estrutural** (seção 4) — hierarquia exata dos elementos.

**Escopo do piloto (já decidido):** religar funcionalmente só ÁUDIO (Volume Geral,
Música, SFX) e JOGO (Idioma). A aba VÍDEO é traduzida visualmente mas os controles
ficam desabilitados/sem religar (o SettingsManager não suporta resolução/tela
cheia/qualidade ainda). A aba CONTA tem botões visuais (sem ação funcional no piloto).

---

## 1. TOKENS DE DESIGN (→ vira `tokens.uss` com variáveis `--sol-*`)

```css
/* Base — áreas grandes, fundos */
--bg-deep:     #14131A;
--bg-panel:    #1F1D29;
--bg-elevated: #2A2833;

/* Accent — destaques */
--accent:       #D4A03C;
--accent-hover: #E8B95A;
--accent-cold:  #5A7A95;

/* Texto */
--text-primary:   #E8E0CC;
--text-secondary: #9A95A8;
--text-on-accent: #14131A;

/* Estados */
--success:  #6B8E5A;
--danger:   #A83838;
--disabled: #4A4754;
--arcane:   #6B4E8E;

/* Bordas do mundo */
--line:        rgba(212,160,60,.22);
--line-strong: rgba(212,160,60,.45);

/* Espaçamento */
--s1:4px; --s2:8px; --s3:16px; --s4:24px; --s5:32px; --s6:48px;

/* Tipografia */
--font-display: Cinzel (SDF)         /* títulos, abas, botões — peso 700/800 */
--font-body:    Alegreya Sans (SDF)  /* corpo, labels — peso 400/500/700 */
```

**Nota USS:** UI Toolkit não suporta `var()` da mesma forma que CSS. Use variáveis
USS nativas (`--sol-accent: #D4A03C;` em `:root` e `var(--sol-accent)` nas regras),
que o UI Toolkit suporta. Prefixe tudo com `--sol-` para evitar colisão.

---

## 2. TIPOGRAFIA & FONTES (SDF)

- **Cinzel** (Google Fonts) → títulos, abas, botões. Pesos: Bold (700), Black (800).
- **Alegreya Sans** (Google Fonts) → corpo, labels, campos. Pesos: Regular (400),
  Medium (500), Bold (700).
- Gerar Font Asset SDF de cada via Font Asset Creator (Window > TextMeshPro >
  Font Asset Creator), atlas SDFAA, e referenciar no USS com
  `-unity-font-definition: url("project://.../Cinzel SDF.asset")`.
- O título usa glow âmbar: `text-shadow:0 2px 24px rgba(212,160,60,.2)` →
  no UI Toolkit, usar `-unity-text-outline` sutil ou `text-shadow` (suportado no
  UI Toolkit do Unity 6).

---

## 3. CSS DE REFERÊNCIA (cada bloco → regra USS equivalente)

```css
/* ---------- Layout ---------- */
.config-screen{min-height:100vh;display:flex;justify-content:center;align-items:flex-start;
  padding:48px 24px;
  background:radial-gradient(120% 80% at 50% -10%, #1b1924 0%, #14131A 60%)}
  /* UI Toolkit: gradiente radial não é nativo — usar cor sólida #16151c
     ou textura de fundo. Confirmar abordagem. */
.config-panel{position:relative;width:100%;max-width:860px;background:#1F1D29;
  border:1px solid rgba(212,160,60,.45);border-radius:10px;
  box-shadow:inset 0 0 0 1px rgba(0,0,0,.4), 0 20px 60px rgba(0,0,0,.55);
  display:flex;flex-direction:column}

/* cantoneiras douradas — 4 cantos em L */
.corner{position:absolute;width:16px;height:16px}
.corner-tl{top:9px;left:9px;border-top:2px solid #D4A03C;border-left:2px solid #D4A03C}
.corner-tr{top:9px;right:9px;border-top:2px solid #D4A03C;border-right:2px solid #D4A03C}
.corner-bl{bottom:9px;left:9px;border-bottom:2px solid #D4A03C;border-left:2px solid #D4A03C}
.corner-br{bottom:9px;right:9px;border-bottom:2px solid #D4A03C;border-right:2px solid #D4A03C}
  /* UI Toolkit: cada corner é um VisualElement com bordas parciais.
     Bordas parciais (só top+left) são suportadas via border-top-width +
     border-left-width individuais. */

/* ---------- Header ---------- */
.config-header{display:flex;align-items:center;justify-content:space-between;
  padding:24px 32px;border-bottom:1px solid rgba(212,160,60,.22);background:rgba(0,0,0,.25)}
.config-title{font-family:Cinzel;font-weight:800;font-size:28px;letter-spacing:.06em;
  color:#D4A03C;text-shadow:0 2px 24px rgba(212,160,60,.2)}
.config-close{width:36px;height:36px;border-radius:6px;border:1px solid rgba(212,160,60,.22);
  background:#2A2833;color:#9A95A8;font-size:18px}
.config-close:hover{border-color:rgba(212,160,60,.45);color:#D4A03C;background:rgba(212,160,60,.08)}

/* ---------- Abas ---------- */
.config-tabs{display:flex;gap:4px;padding:0 32px;border-bottom:1px solid rgba(212,160,60,.22);
  background:rgba(0,0,0,.15)}
.config-tab{border:none;background:transparent;font-family:Cinzel;font-weight:700;font-size:15px;
  letter-spacing:.03em;color:#9A95A8;padding:14px 22px;position:relative}
.config-tab:hover{color:#E8E0CC}
.config-tab.is-active{color:#D4A03C}
.config-tab.is-active::after{content:"";position:absolute;left:14px;right:14px;bottom:-1px;height:3px;
  border-radius:2px;background:#D4A03C;box-shadow:0 0 10px rgba(212,160,60,.6)}
  /* UI Toolkit: o ::after vira um VisualElement filho (underline) visível só na aba ativa,
     OU border-bottom na aba ativa. */

/* ---------- Corpo / painéis ---------- */
.config-body{padding:32px}
.config-tabpanel{display:none;flex-direction:column;gap:8px}
.config-tabpanel.is-active{display:flex}
  /* UI Toolkit: alternar display via classe is-active (style.display=Flex/None no C#) */
.config-section-hint{font-size:12px;letter-spacing:.2em;text-transform:uppercase;
  color:#9A95A8;font-weight:700;margin-bottom:8px}

/* linha de controle genérica */
.config-row{display:flex;align-items:center;justify-content:space-between;gap:24px;
  padding:16px 0;border-bottom:1px solid rgba(212,160,60,.1)}
.config-row:last-child{border-bottom:none}
.config-row-label{display:flex;flex-direction:column;gap:2px}
.config-row-name{font-size:15px;color:#E8E0CC;font-weight:500}
.config-row-desc{font-size:12px;color:#9A95A8}
.config-row-control{flex:0 0 auto;display:flex;align-items:center;gap:16px}

/* ---------- Slider ---------- */
/* IMPORTANTE: no UI Toolkit, use o controle nativo <Slider> e ESTILIZE via USS,
   OU recrie com VisualElements. O nativo já tem drag/valor — recomendado religar
   no SettingsManager. Visual abaixo é a referência de aparência. */
.config-slider-row .config-row-control{flex:1 1 auto;max-width:340px}
.config-slider-value{font-size:13px;color:#D4A03C;width:42px;text-align:right} /* monospace */
.config-slider{height:14px;border-radius:999px;background:#15141b;
  border:1px solid rgba(212,160,60,.22);box-shadow:inset 0 1px 3px rgba(0,0,0,.6)}
.config-slider-fill{border-radius:999px;
  background:linear-gradient(90deg,#8a6520,#D4A03C);box-shadow:0 0 10px rgba(212,160,60,.4)}
.config-slider-handle{width:20px;height:20px;border-radius:50%;border:1px solid #5a420f;
  background:radial-gradient(circle at 35% 30%,#E8B95A,#8a6520);box-shadow:0 2px 6px rgba(0,0,0,.6)}

/* ---------- Toggle ---------- */
/* UI Toolkit: usar <Toggle> nativo estilizado, OU VisualElement custom. */
.config-toggle{width:50px;height:26px;border-radius:999px;background:#15141b;
  border:1px solid rgba(212,160,60,.22)}
.config-toggle-knob{width:20px;height:20px;border-radius:50%;background:#9A95A8;top:2px;left:3px}
.config-toggle.is-on{background:#D4A03C;border-color:#D4A03C}
.config-toggle.is-on .config-toggle-knob{left:27px;background:#14131A}

/* ---------- Dropdown ---------- */
/* UI Toolkit: usar <DropdownField> nativo estilizado. */
.config-dropdown{display:flex;align-items:center;justify-content:space-between;gap:16px;
  min-width:190px;padding:10px 14px;border:1px solid rgba(212,160,60,.22);border-radius:6px;
  background:#2A2833;color:#E8E0CC;font-size:14px}
.config-dropdown:hover{border-color:rgba(212,160,60,.45)}
.config-dropdown-caret{color:#D4A03C;font-size:11px}

/* ---------- Campo de texto ---------- */
.config-field{min-width:220px;padding:10px 14px;border:1px solid rgba(212,160,60,.22);border-radius:6px;
  background:#2A2833;color:#E8E0CC;font-size:14px}
.config-field:focus{border-color:#D4A03C}

/* ---------- Botões ---------- */
.config-btn{font-family:Cinzel;font-weight:700;font-size:14px;border-radius:5px;
  padding:11px 22px;border:1.5px solid transparent;white-space:nowrap}
.config-btn--primary{background:#D4A03C;color:#14131A;border-color:#D4A03C}
.config-btn--primary:hover{background:#E8B95A;box-shadow:0 0 16px rgba(212,160,60,.4)}
.config-btn--secondary{background:transparent;color:#D4A03C;border-color:#D4A03C}
.config-btn--secondary:hover{background:rgba(212,160,60,.12);color:#E8B95A;border-color:#E8B95A}

/* ---------- Rodapé ---------- */
.config-footer{display:flex;align-items:center;justify-content:space-between;gap:24px;
  padding:24px 32px;border-top:1px solid rgba(212,160,60,.22);background:rgba(0,0,0,.25)}
.config-footer-links{display:flex;gap:24px}
.config-link{color:#9A95A8;font-size:13px}
.config-link:hover{color:#D4A03C}
.config-footer-actions{display:flex;gap:16px}
```

---

## 4. MARKUP ESTRUTURAL (hierarquia → árvore UXML)

```
config-screen
└── config-panel
    ├── corner-tl, corner-tr, corner-bl, corner-br   (4 cantoneiras)
    ├── HEADER (config-header)
    │   ├── config-title: "CONFIGURAÇÕES"
    │   └── config-close: "✕"
    ├── ABAS (config-tabs)
    │   ├── config-tab is-active [data-tab=audio]: "Áudio"
    │   ├── config-tab [data-tab=video]: "Vídeo"
    │   ├── config-tab [data-tab=jogo]: "Jogo"
    │   └── config-tab [data-tab=conta]: "Conta"
    ├── CORPO (config-body)
    │   ├── PAINEL ÁUDIO (config-tabpanel is-active [data-tab=audio])
    │   │   ├── section-hint: "Áudio"
    │   │   ├── row slider "Música" — 75%          [RELIGAR: SetMusicVolume]
    │   │   └── row slider "Efeitos (SFX)" — 85%   [RELIGAR: SetSfxVolume]
    │   │   /* NOTA: o slider "Volume Geral" do design foi REMOVIDO do piloto
    │   │      (não há Master no SettingsManager). Só Música + SFX. */
    │   ├── PAINEL VÍDEO (config-tabpanel [data-tab=video])  [VISUAL APENAS — sem religar]
    │   │   ├── section-hint: "Vídeo"
    │   │   ├── row dropdown "Resolução" — "1920 × 1080"
    │   │   ├── row toggle "Tela cheia" — ON
    │   │   ├── row dropdown "Qualidade gráfica" — "Alta"
    │   │   ├── row toggle "V-Sync" — OFF
    │   │   └── row toggle "Números de dano" — ON
    │   ├── PAINEL JOGO (config-tabpanel [data-tab=jogo])
    │   │   ├── section-hint: "Jogo"
    │   │   └── row dropdown "Idioma" — "Português"   [RELIGAR: SetLanguage/GetLanguage]
    │   └── PAINEL CONTA (config-tabpanel [data-tab=conta])  [BOTÕES VISUAIS — sem ação no piloto]
    │       ├── section-hint: "Conta"
    │       ├── row: campo "Nome do jogador" = "Errante das Brasas" + btn "Editar perfil"
    │       ├── row: "Conta" + desc + btn-primary "Conta / Login"
    │       └── row: "Compras" + desc + btn-secondary "Restaurar Compras"  [#if !STEAM no futuro]
    └── RODAPÉ (config-footer)
        ├── links: "Privacidade" | "Créditos"
        └── actions: btn-secondary "Restaurar Padrões" + btn-primary "Salvar"
```

---

## 5. RELIGAMENTO DE BACKEND (escopo do piloto)

API do `Solengard.Core.SettingsManager` (singleton) a religar:

| Controle UI            | Método/prop do SettingsManager        | Faixa      |
|------------------------|---------------------------------------|------------|
| Slider "Música"        | `SetMusicVolume(float)` / `MusicVolume`| 0..1       |
| Slider "Efeitos (SFX)" | `SetSfxVolume(float)` / `SfxVolume`   | 0..1       |
| Dropdown "Idioma"      | `SetLanguage(int)` / `GetLanguage()`  | 0=PT 1=EN 2=ES |
| Botão "Restaurar Padrões"| (ler defaults e reaplicar)          | —          |
| Botão "Salvar"         | (persiste — `LoadSettings`/PlayerPrefs já auto-salva no set) | — |

**Slider "Volume Geral" REMOVIDO do piloto** (não há Master no SettingsManager).
A aba Áudio tem apenas Música e SFX. Quando o Master for criado no backend
(passo futuro), o slider é adicionado.

**Padrão de religamento** (espelhar o `ConfigUIBinder.cs` atual):
- `RegisterValueChangedCallback` nos sliders/dropdown → chamar a API.
- Ler estado inicial das props e usar `SetValueWithoutNotify` ao sincronizar.
- Assinar `event Action OnSettingsChanged` para refletir mudanças externas.

---

## 6. NOTAS DE TRADUÇÃO UI TOOLKIT (pontos de atenção)

1. **Controles nativos vs custom:** prefira `<Slider>`, `<Toggle>`, `<DropdownField>`
   nativos do UI Toolkit, estilizados via USS para igualar o visual. Eles já têm
   interação (drag, click) e facilitam o religamento. O CSS de `.config-slider` etc.
   é a referência de APARÊNCIA, não obrigatoriamente da estrutura.
2. **Gradientes radiais** (fundo da tela, handle do slider): UI Toolkit não suporta
   `radial-gradient` nativo. Usar cor sólida aproximada ou textura. Para o fundo,
   `#16151c` sólido é aceitável. Confirmar se quer textura depois.
3. **Gradiente linear** no slider-fill (`linear-gradient(90deg,#8a6520,#D4A03C)`):
   UI Toolkit suporta gradiente via `background-image` com textura, ou cor sólida
   `#D4A03C`. Para o piloto, cor sólida âmbar é aceitável.
4. **Cantoneiras:** cada uma é um VisualElement 16×16 com 2 bordas (ex: top+left),
   posicionado absoluto nos cantos. Bordas parciais via `border-top-width` +
   `border-left-width` individuais funcionam no UI Toolkit.
5. **Troca de abas:** lógica em C# no ConfigPanelController — clicar numa aba
   adiciona `is-active` nela e no painel correspondente, remove dos outros
   (`element.AddToClassList`/`RemoveFromClassList`).
6. **`box-shadow`:** UI Toolkit tem suporte limitado. Sombras grandes (drop do painel)
   podem ser omitidas ou aproximadas. Não é crítico para fidelidade.
7. **`text-shadow` (glow do título):** suportado no Unity 6 UI Toolkit. Aplicar no título.
