# Briefing — Design System Solengard (para Claude Design)

> Cole este briefing no Claude Design para gerar o style guide / design system completo.
> Depois, cada tela é gerada pedindo "use o design system Solengard".
> O output (HTML/CSS) será traduzido para UXML/USS do Unity UI Toolkit.

---

## CONTEXTO

Crie um DESIGN SYSTEM completo e coeso para a UI de um jogo **RPG dark fantasy gótico** chamado **Solengard** — um roguelite de sobrevivência em ondas, sombrio e atmosférico, para **PC/Steam**. 

Objetivo de qualidade: **indie profissional caprichado** — limpo, coeso, consistente, com bom acabamento. Não precisa ser AAA, mas não pode parecer amador. A referência de polish/HUD é **Lies of P** (acabamento premium, mas priorizando CLAREZA acima de estilo).

Gere tudo em **HTML/CSS limpo, semântico e com flexbox** (será traduzido para UXML/USS do Unity UI Toolkit — organização clara e variáveis CSS ajudam muito).

---

## 1. PALETA (tokens / variáveis CSS)

Filosofia: **base escura, fria e neutra** (descansa a vista em sessões longas) + **um accent quente âmbar/brasa** (chama o olho pro que importa, remete a tocha/fogo de masmorra). Cor forte só nos detalhes, nunca em áreas grandes. Roxo apenas como toque ocasional (magia/raridade), nunca protagonista.

```
/* Base (áreas grandes, fundos) */
--bg-deep:    #14131A;  /* carvão quase preto, levemente frio - fundo principal */
--bg-panel:   #1F1D29;  /* pedra escura - painéis */
--bg-elevated:#2A2833;  /* pedra média - cards/elementos elevados */

/* Accent (destaques, pouco e marcante) */
--accent:        #D4A03C;  /* âmbar/brasa - botões primários, títulos, seleção */
--accent-hover:  #E8B95A;  /* âmbar mais claro - hover */
--accent-cold:   #5A7A95;  /* azul-aço gelado - ações secundárias, ícones */

/* Texto */
--text-primary:  #E8E0CC;  /* osso - texto principal, quente e legível no escuro */
--text-secondary:#9A95A8;  /* cinza pedra - texto secundário */
--text-on-accent:#14131A;  /* texto escuro sobre botão âmbar */

/* Estados (sóbrios, não berrantes) */
--success: #6B8E5A;  /* verde-musgo */
--danger:  #A83838;  /* sangue seco */
--disabled:#4A4754;  /* cinza dessaturado */

/* Toque ocasional (magia/raridade) */
--arcane:  #6B4E8E;  /* roxo místico - usar com parcimônia */
```

---

## 2. TIPOGRAFIA

- **Títulos / display:** fonte serifada com cara de fantasia medieval (ex: Cinzel, Cormorant, ou similar), peso bold, cor `--accent`. Para títulos de tela e seções.
- **Corpo:** fonte limpa e muito legível (serifada sutil ou sans humanista), cor `--text-primary`.
- **Hierarquia:** H1 (título de tela, grande), H2 (seção), corpo, legenda (menor, `--text-secondary`).
- Boa altura de linha e contraste. Nada de texto sumindo no fundo.

---

## 3. ESPAÇAMENTO E FORMA

- Escala de espaçamento consistente: 4 / 8 / 16 / 24 / 32 / 48px. Respiro generoso.
- Cantos: painéis com moldura ornamentada; controles com cantos levemente arredondados (4-6px).
- Bordas de painel: moldura gótica sutil (cantoneiras, linha dourada fina), **9-slice friendly** (estica sem distorcer — importante pra responsividade).
- Layout sempre em **flexbox**, proporções relativas (responsivo).

---

## 4. COMPONENTES (gerar cada um com seus estados)

1. **Painel / Frame:** moldura ornamentada estilo grimório/pedra (cantoneiras góticas discretas, borda `--accent` fina), fundo `--bg-panel`. Variações: grande, médio, pequeno.

2. **Header de tela:** título à esquerda (`--accent`, display) + área à direita (ex: saldo/ícone) + botão X (fechar) no canto superior direito. Sobre faixa escura translúcida.

3. **Menu lateral (PC):** lista vertical de itens à esquerda da tela (NÃO barra inferior — isso é mobile). Item normal / hover / selecionado (destaque `--accent`). Para o menu principal.

4. **Botão:** 
   - Primário: fundo `--accent`, texto `--text-on-accent`.
   - Secundário: contorno `--accent`, fundo transparente.
   - Perigo: `--danger`.
   - Estados: normal / hover (`--accent-hover`) / pressed / disabled (`--disabled`).

5. **Card / Slot:** para item (upgrade, personagem), com área de ícone + nome + descrição + ação. Moldura "selo" sutil. Estados: normal / hover / selecionado / bloqueado (cadeado).

6. **Slider:** trilho de pedra escura, preenchimento `--accent`, handle visível. Para volumes.

7. **Toggle / Switch:** liga/desliga temático (off escuro, on `--accent`).

8. **Dropdown / Seletor:** campo coeso com a estética, para resolução, idioma, qualidade.

9. **Indicador de progresso (runas):** série de runas/pips — acesa (`--accent`, brilho) / apagada (escura). Como no grimório, para níveis de upgrade.

10. **Barra de progresso:** para XP, missões. Trilho escuro + preenchimento `--accent`.

11. **HUD de gameplay (barra inferior):** referência Lies of P, priorizando CLAREZA. Elementos: barra de VIDA (proeminente, pisca `--danger` quando crítica), barra de XP/nível, indicador de ONDA atual, ícone de ESPECIAL com cooldown (preenche quando pronto). Tudo numa faixa inferior coesa, legível num relance, sem poluir a tela. Não-diegético, limpo.

12. **Modal / Popup:** confirmações, com painel central + overlay escurecido.

---

## 5. TELAS A COBRIR (gerar depois, usando o sistema)

Versão **Steam** (sem compra de diamantes nem anúncios — diamantes são moeda de progressão ganha jogando):

- **Menu principal:** lista vertical à esquerda — Continuar (só se houver run salva) / Novo Jogo / Santuário / Configurações. Arte de fundo dark fantasy à direita.
- **Configurações:** Áudio (sliders música/SFX), Vídeo (tela cheia, resolução, qualidade), Jogo (idioma), Conta (nome + editar perfil), botões Restaurar/Salvar. (PRIMEIRO PILOTO)
- **Santuário:** hub de fortalecimento com Grimório (upgrades) + Personagens (heróis). Nome temático, sem conotação de compra.
- **Grimório:** páginas por categoria de upgrade, folhear, entradas com runas de progresso.
- **Missões:** cards de missão (descrição + progresso + recompensa).
- **Legado:** painéis de estatística (recordes, acumulados).
- **Perfil:** nome, avatar, dentro de Configurações.
- **HUD de gameplay:** barra inferior (item 11).
- **Game Over / fim de run:** resultado, recompensa, botões.
- **Seleção de classe / pré-run.**
- **Lore:** imagem + texto entre zonas.

---

## 6. PRINCÍPIOS (aplicar em tudo)

- **Coeso:** todos os componentes parecem do mesmo mundo (mesma paleta, mesmo nível de detalhe, mesma linguagem de borda).
- **Limpo:** respiro, hierarquia clara (primário se destaca, secundário recua), não poluído.
- **Legível:** contraste alto, baixa saturação nas áreas grandes, informação essencial salta.
- **Responsivo:** flexbox, proporções relativas, painel central com largura máxima centralizado, funciona em 1080p / 1440p / 4K / ultrawide (21:9).
- **Clareza > estilo** (especialmente no HUD): bonito nunca às custas de entender num relance.

---

## ENTREGA

Apresente um **style guide** mostrando: a paleta, a tipografia, e cada componente nos seus estados. Estruture o CSS com as variáveis no topo (`:root`) para fácil ajuste de cores depois. Quero usar este sistema como base para gerar todas as telas listadas, de forma consistente.
