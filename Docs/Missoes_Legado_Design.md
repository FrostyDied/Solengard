# Design — Missões (Diárias/Semanais) + Legado (Estatísticas)

Documento de decisões de design para revisão. Implementação acompanha este doc.

## Princípios respeitados
- **Singletons + SystemsBootstrap**: os sistemas novos existem em runtime via `SystemsBootstrap`
  (criados em `BeforeSceneLoad`, `DontDestroyOnLoad`, guard de duplicata no `Awake`).
- **Wiring robusto**: botões usam `MenuButtonAction` (enum serializado + `FindAnyObjectByType`),
  binders de painel seguem o padrão `ConfigUIBinder` (busca por nome no `OnEnable`). Sem lambda em editor.
- **Cena = fonte da verdade**: a UI é construída por um **editor builder não-destrutivo**
  (`FindOrCreate`, idempotente) e populada por binders em runtime. Nenhum gerador destrutivo novo.
- **Aditivo / não quebrar**: reusa o evento `RunRewardSystem.OnRunRewardCalculated` (fim de run já
  consertado) em vez de alterar o fluxo de run. Reusa `DiamondSystem.AddDiamonds`, dados
  `sol_best_score`/`sol_last_run`/`sol_run_history`.

## Decisão sobre o sistema de missões
O `DailyMissionSystem` já existe (GameObject só na GameScene, rastreia kills/waves/diamantes).
Para evitar quebrar a referência de cena (renomear classe = missing script) e duplo-tracking:
- **Atualizo o `DailyMissionSystem` no lugar** → vira **singleton** (`Instance` + `DontDestroyOnLoad`),
  passa a gerenciar **diárias E semanais**, ganha novos tipos de missão, e entra no `SystemsBootstrap`.
- O GameObject que sobra na GameScene se autodestrói pelo guard de singleton (o do Bootstrap vence).
- Nome da classe mantido (`DailyMissionSystem`) por compatibilidade de GUID; comentário documenta que
  agora cobre diárias+semanais. (Renomear arquivo quebraria a referência na cena.)

## ESCOPO 1 — Missões

### Tipos de missão (enum `MissionType`)
Existentes: `MatarInimigos`, `SobreviverWaves`, `ColetarDiamantes` (mantidos).
Novos: `JogarPartida`, `AlcancarZona`, `SobreviverTempo`.
Removido do uso: `VencerSemTomarDano` (mantido no enum p/ compat, fora do pool).

### Fontes de progresso (eventos)
| Tipo | Fonte | Lógica |
|---|---|---|
| MatarInimigos | `EnemyBase.OnEnemyDied` | +1 por kill |
| SobreviverWaves | `ZoneManager.OnZoneCompleted` | +1 por zona concluída |
| ColetarDiamantes | `DiamondSystem.OnDiamondsChanged` | += delta positivo |
| JogarPartida | `RunRewardSystem.OnRunRewardCalculated` | +1 por run finalizada |
| AlcancarZona | `RunRewardSystem.OnRunRewardCalculated` | progresso = max(prog, summary.waveReached) |
| SobreviverTempo | `RunRewardSystem.OnRunRewardCalculated` | progresso = max(prog, summary.timeSurvived) |

### Diárias (3/dia, reset 24h UTC, seeded por data) — recompensas modestas 10–30 💎
Pool (escolhe 3/dia, seeded p/ ser igual entre dispositivos):
- Matar 50 inimigos → **10** 💎
- Matar 100 inimigos → **20** 💎
- Sobreviver 5 min (300s) numa partida → **15** 💎
- Jogar 1 partida → **10** 💎
- Alcançar a Zona 2 → **20** 💎
- Concluir 3 zonas (waves) → **15** 💎
- Coletar 25 diamantes → **20** 💎

### Semanais (3/semana, reset 7 dias por ano-semana ISO, seeded) — 50–100 💎
Pool (escolhe 3/semana):
- Matar 500 inimigos na semana → **60** 💎
- Jogar 10 partidas → **50** 💎
- Alcançar a Zona 5 → **100** 💎
- Coletar 200 diamantes → **70** 💎
- Sobreviver 10 min numa partida → **60** 💎

### Persistência (PlayerPrefs)
- Diárias: `sol_mission_date` (yyyy-MM-dd) + `sol_missions` (JSON). (mantém chaves atuais)
- Semanais: `sol_weekmission_id` (yyyy-Www) + `sol_weekmissions` (JSON).
- Reset detectado no `Start`/`OnEnable` comparando a chave de data/semana.

### UI — PainelMissoes (já existe, hoje vazio)
- `MissoesUIBinder` (padrão ConfigUIBinder) anexado ao painel.
- Constrói as linhas em runtime sob `DailyContainer` e `WeeklyContainer` (VerticalLayoutGroup):
  cada linha = descrição + barra de progresso (fill anchorMax.x) + texto `prog/meta` + botão **Coletar**
  (desabilitado se incompleta ou já resgatada; mostra `<sprite name="diamante">N`).
- Textos de reset: "Diárias renovam em HH:MM" e "Semanais em Nd HH:MM".
- Coletar → `DailyMissionSystem.Instance.ClaimDaily(i)` / `ClaimWeekly(i)` → `DiamondSystem.AddDiamonds`.

## ESCOPO 2 — Legado (substitui Config no bottom bar)

### Sistema `LegadoStatsSystem` (novo, singleton, bootstrap)
Assina `RunRewardSystem.OnRunRewardCalculated` e acumula contadores lifetime (PlayerPrefs):
| Contador | Chave | Atualização |
|---|---|---|
| Total de partidas | `sol_lt_runs` | +1 |
| Tempo total jogado (s) | `sol_lt_time` | += summary.timeSurvived |
| Diamantes ganhos lifetime | `sol_lt_diamonds` | += summary.diamondsEarned |
| Kills lifetime | `sol_lt_kills` | += summary.totalKills |
| Zona máxima | `sol_lt_maxzone` | max(atual, summary.waveReached) |
| Uso por classe | `sol_lt_class_<id>` | +1 na classe da run (PlayerClassManager.CurrentClass) |

Getter `PersonagemMaisUsado()` varre `sol_lt_class_*` e retorna o de maior contagem.

### Tela LEGADO — PainelLegado (novo)
Seções (cards no padrão dark fantasy, `panel_background.png`, tipografia consistente):
- **Recordes**: Melhor pontuação (`sol_best_score`), Última run (`sol_last_run`), Zona máxima (`sol_lt_maxzone`).
- **Acumulados**: Partidas, Tempo total (HH:MM), Diamantes ganhos (lifetime), Kills lifetime.
- **Preferências**: Personagem mais usado.
- **Ranking**: placeholder "🏆 Ranking Global — Em breve" (reservado p/ futuro).
- `LegadoUIBinder` popula os textos por nome no `OnEnable`.

### Bottom bar
- `TabConfigs` → renomeado para **`TabLegado`** (label "LEGADO"), wire para `MainMenuManager.AbrirLegado`
  via `MenuButtonAction(AbrirLegado)`. Config segue só na engrenagem da TopBar.

## Arquivos
**Novos**: `MissionSystem`? não — atualiza `DailyMissionSystem.cs`; `LegadoStatsSystem.cs`,
`MissoesUIBinder.cs`, `LegadoUIBinder.cs`, `Editor/SolengardMissoesLegadoSetup.cs`.
**Modificados**: `SystemsBootstrap.cs` (+2 sistemas), `MenuButtonAction.cs` (+AbrirLegado, +ColetarMissao),
`MainMenuManager.cs` (+painelLegado, +AbrirLegado).

## Como construir a UI (passo manual no Editor, não-destrutivo)
Menu `Solengard/Missoes+Legado: Construir UI` → cria conteúdo do PainelMissoes, cria PainelLegado,
troca a aba Config→Legado, anexa binders + MenuButtonAction, religa MainMenuManager. Idempotente.
