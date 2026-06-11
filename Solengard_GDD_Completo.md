# SOLENGARD — Game Design Document Completo
> Grimhold Games · Versão consolidada de todas as sessões
> Engine: Unity 6 (6000.0.75f1 LTS) · Repositório: github.com/FrostyDied/Solengard
> Local: D:\Projetos\Solengard · Package ID: com.grimholdgames.solengard

---

## 1. VISÃO GERAL DO PROJETO

**Gênero:** Wave survival roguelite top-down 2D dark fantasy
**Plataformas:** Android + iOS (free-to-play) + Steam (pago ~R$10,90)
**Studio:** Grimhold Games (solo dev — Paegle)
**Referência principal:** Vampire Survivors

### Loop central
Player sobrevive a hordas de inimigos → coleta XP → sobe de nível → escolhe boost → chega no boss → passa de zona → repete por 5 zonas.

---

## 2. ESTADO ATUAL DO DESENVOLVIMENTO

### ✅ IMPLEMENTADO E FUNCIONANDO
- Core loop: PlayerAttack → WaveManager → ScoreSystem → RunRewardSystem → DiamondSystem → GameOver → MainMenu → Restart
- 6 classes jogáveis com ClassDefinition ScriptableObjects
- Motor ProceduralVFX (VFX gerado por código: WhipChain, EnergyBolt, SlashArc, DaggerFlash, StarProjectile, ArrowStreak, SkullProjectile, CrossSlash, CrescentSlash, ExplosionRing)
- Sistema de chunks (WorldChunkManager, 5 biomas)
- ProductionSafeguard (auto-recovery)
- ZoneManager (5 zonas com boss)
- Lore entre zonas (LoreScreenUI com fonte gótica)
- Sistema de XP e level-up (LevelUpUI com 6 boosts)
- Mobile Fantasy Game Interface (29 PNGs exportados)
- Fonte gótica (StraightPixelGothic_TMP_v2.asset)

### ⚠️ PARCIALMENTE IMPLEMENTADO
- HUD da partida (em reestruturação — ver Seção 8)
- Mobile Fantasy UI sprites (exportados, aguardando integração no HUD)

### ❌ NÃO IMPLEMENTADO (roadmap)
- Boosts durante a run (LevelUpUI existe, precisa de visual correto)
- Upgrades permanentes (sistema documentado, não implementado)
- Joystick mobile (MobileJoystick.cs existe, não validado)
- Som (trilha + SFX)
- Build Android APK
- Tela de seleção de personagem
- Loja de upgrades permanentes
- Telemetria (Unity Analytics)

---

## 3. SISTEMA DE CLASSES

### 6 Classes implementadas (ClassDefinition ScriptableObjects em Assets/Resources/Classes/)

| Classe | Nome | HP | ATK | Vel | AttackType | Range | Interval |
|--------|------|----|-----|-----|-----------|-------|---------|
| Guerreiro | Kael | 180 | 40 | 2.66 | MeleeDirectional | 3.5u | 1.4s |
| Mago | Seraphine | 90 | 50 | 2.6 | RangedSingle | 5u | 2.0s |
| Assassino | Vael | 110 | 45 | 2.2 | MeleeCone 60° | 2.5u | 1.0s |
| Necromante | Marveth | 100 | 35 | 2.5 | RangedSummon | 7u | 2.5s |
| Paladino | Aldric | 200 | 25 | 2.4 | Melee180° | 3u | 1.8s |
| Caçador | Rynn | 120 | 38 | 2.7 | RangedMulti | 6u | 2.0s |

### VFX por classe (ProceduralVFX.cs)
- **Guerreiro:** WhipChain (azul elétrico, ponta fixa no player)
- **Mago:** EnergyBolt (azul-chama + laranja, cresce durante o voo)
- **Assassino:** StarProjectile (estrela geométrica vermelha rotacionando, colisão real)
- **Necromante:** SkullProjectile (caveirinha verde geométrica com bounce, colisão real)
- **Paladino:** SlashArc + DaggerFlash (arco dourado, 100°)
- **Caçador:** ArrowStreak (flecha azul claro com ponta triangular) + CrescentSlash

### Poderes Especiais (cooldown 30s, botão no HUD — pendente de implementação)
- Guerreiro: Fúria Sanguínea — ATK +80%, velocidade +50% por 5s
- Mago: Nova Arcana — 8 orbes em burst 360°
- Assassino: Evasão Sombria — invulnerável 3s + dash + trail
- Necromante: Invocação em Massa — 3 minions nos inimigos mais próximos
- Paladino: Aura Sagrada — campo de dano 360° por 6s
- Caçador: Chuva de Flechas — 12 flechas em área por 3s

### Passivas por Classe (não implementadas)
- Guerreiro: Pele de Ferro — kill reduz dano recebido 10% (máx 50%, reseta ao tomar dano)
- Mago: Sobrecarga Caótica — 5 kills sem dano = próximo projétil 300% dano
- Assassino: Golpe Fatal — primeiro hit = 2.5x dano crítico
- Necromante: Dreno de Alma — kill restaura 5 HP
- Paladino: Escudo Divino — a cada 15s sem morrer, absorve 1 hit
- Caçador: Marcação de Presa — primeiro hit aplica marca +30% dano por 5s

### Desbloqueio por plataforma
- Steam: todos liberados (`#if UNITY_STANDALONE`)
- Mobile: Guerreiro grátis, demais via diamantes ou IAP

---

## 4. SISTEMA DE ZONAS E BIOMAS

### 5 Zonas
| Zona | Bioma | Cor camera | Inimigos | Boss |
|------|-------|-----------|---------|------|
| 1 | Veremoth (Floresta) | Verde escuro | Goblin, Slime, Zumbi | Caveman |
| 2 | Khorduum (Caverna) | Azul pedra | Orc, Golem, DarkElf | GiantGoblin |
| 3 | Valdross (Cemitério) | Cinza roxo | Skeleton, Ghost, Lich | VikingLeader |
| 4 | Gorveth (Pântano) | Verde pântano | Demon, Gnoll, Archer | (definir) |
| 5 | Arkenfall (Campo) | Marrom | DarkElf2, Mage, OrcHeavy | 3 bosses juntos |

### Progressão entre zonas
- Player mantém upgrades e vida ao trocar de zona
- Boss aparece após timer (testBossSpawnAt no ZoneManager)
- ZoneLoop: spawn → boss → transição → lore → próxima zona

---

## 5. ECONOMIA E MONETIZAÇÃO

### Moeda única: Diamante 💎
- Coletado durante a run (drop dos inimigos)
- Fontes mobile: gameplay + rewarded ads + bônus diário + IAP
- Fontes Steam: apenas gameplay

### Para que serve por plataforma
| Plataforma | Diamante serve para |
|-----------|---------------------|
| Mobile | Heróis + upgrades permanentes + IAP |
| Steam | Apenas upgrades permanentes |

### Upgrades Permanentes (16 upgrades documentados)
Implementar na fase de meta-progressão. Ver Seção 9 para lista completa.

---

## 6. BOOSTS DURANTE A RUN (LevelUpUI — precisa de visual)

### Boosts padrão (todas as classes)
| # | Nome | Efeito |
|---|------|--------|
| 1 | Lâmina Afiada | +10% attackDamage (máx 150) |
| 2 | Pés Ágeis | +2% moveSpeed |
| 3 | Coração Forte | +5% vida máxima |
| 4 | Alcance Místico | +8% attackRange (máx 12) |
| 5 | Fúria de Combate | -3% attackCooldown (mín 0.12) |
| 6 | Cristal Magnético | +3 XPDrop.GlobalMagnetRadius |

### Boosts específicos por classe (pendente de implementação)
- Caçador: +1 flecha (1→2→3)
- Mago: +1 orbe (1→2→3)
- Necromante: minions duram mais (+5s/nível)
- Assassino: cone mais largo (+15°/nível)
- Paladino: knockback mais forte
- Guerreiro: mais ataques direcionais

---

## 7. BUGS ATIVOS (registrados)

### 🔴 CRÍTICOS

**BUG-CLASS-001 — Guerreiro: VFX 360° persistente**
- Descrição: WhipChain aparece como efeito circular em vez de direcional
- Causa provável: timing de inicialização — _cooldownTimer começa em 0
- Status: Em investigação

**BUG-CLASS-002 — Necromante: explosão dupla** 
- Descrição: Duplo VFX ao matar inimigo
- Causa confirmada: SpawnHit + SpawnDeath simultâneos quando projétil mata com 1 hit
- Status: Em tratamento

**BUG-CLASS-003 — Colisão de projéteis fora do range**
- Descrição: Classes atacam inimigos fora do attackRange sem causar dano
- Solução implementada: guard "só atacar quando inimigo dentro do attackRange"
- Status: Parcialmente resolvido

### 🟡 MÉDIOS

**BUG-001 — Movimento involuntário do herói**
- Descrição: Herói se move sozinho sem input
- Tentativas: mass=1000f, isTrigger, kinematic. Melhorou mas pode persistir
- Status: Monitorar

**BUG-002 — Animação girando (flip oscillation)**
- Sprites podem virar esquerda/direita perto de velocidade zero
- Status: Melhorou com histerese, pode ter casos residuais

**BUG-003 — EnemyDarkElf3: idle=0, walk=0**
- Inimigo da Zona 5 sem animação
- Status: Pendente

**BUG-004 — Props aparecendo do nada**
- Objetos do cenário aparecem subitamente ao mover rápido
- Status: Parcialmente corrigido (PrePopulate)

**BUG-005 — WaveBoostSystem sem UI**
- Lógica funciona, UI de escolha não conectada
- Status: Aguardando implementação da tela de boost

**BUG-006 — GameOverScreen não encontrado**
- Warning no Setup
- Status: Pendente

**BUG-007 — Goblin/OrcHeavy attack=1 frame**
- Status: Baixa prioridade

### 🟢 BAIXOS / POLISH

**TMP enableWordWrapping obsoleto** — SolengardLayoutSetup.cs (trocar por textWrappingMode)
**DestroyImmediate fora de contexto Editor** — SolengardSetup.cs
**Emoji em LiberationSans SDF** — Caracteres □

---

## 8. HUD DA PARTIDA (EM REESTRUTURAÇÃO)

### Definição aprovada do HUD
**Manter:**
- Barra de HP (Slider) com sprites Mobile Fantasy UI (bar_frame_1 + bar_fill_1)
- Barra de XP (Slider) com sprites (bar_frame_2 + bar_fill_2)
- Timer formato MM:SS (10 minutos)
- Botão de Pause

**Remover:**
- Wave (sistema mudou)
- "0 Dia" (não usado)
- Missão Ativa (não implementado)
- Score (secundário)

**Adicionar:**
- Botão de Poder Especial (slot_base + action_button_sword, cooldown 30s)

### Assets disponíveis (Assets/Art/UI/MobileFantasyUI/Exported/)
```
bar_frame_1/2.png, bar_fill_1/2.png
hud_container/frame/header/separator.png
slot_base.png
action_button_base/sword/pressed/frame_outer.png
joystick_complete/frame/buttons/pressed.png
menu_complete/button/button_pressed/ornaments/separators.png
complete_screen/container/frame/header/stars/buttons.png
```

### Status atual do HUD
- HUDComplete.cs está sendo reconstruído via SolengardLayoutSetup
- SolengardLayoutSetup cria o HUD Canvas em runtime (não estático)
- HUDBackground com hud_container.png — problema de escala/posição em investigação
- **Problema crítico:** LoreScreenCanvas tinha LocalScale {0,0,0} — corrigido (commit bb57413)
- **Problema crítico:** Canvas ReferenceResolution 1080×1920 (portrait) → corrigido para 1920×1080

### Arquivos de UI relevantes
- `Assets/Scripts/UI/HUDComplete.cs` — HUD principal
- `Assets/Scripts/UI/LevelUpUI.cs` — cards de boost
- `Assets/Scripts/UI/LoreScreenUI.cs` — lore ✅ concluído
- `Assets/Scripts/Editor/SolengardLayoutSetup.cs` — cria o HUD em runtime
- `Assets/Scripts/Editor/SolengardUISetup.cs` — aplica sprites (menus Editor)

---

## 9. UPGRADES PERMANENTES (documentados — implementar na meta-progressão)

| # | Nome | Efeito | Incremento | Cap | Slots | Preço |
|---|------|--------|-----------|-----|-------|-------|
| 1 | Poder | +% dano | +5%/nível | +25% | 4 | 600💎 |
| 2 | Armadura | -dano flat | -1/nível | -3 | 3 | 600💎 |
| 3 | Vida Máxima | +% HP | +10%/nível | +30% | 3 | 400💎 |
| 4 | Recuperação | +HP/s | +0.1/nível | +0.5/s | 4 | 200💎 |
| 5 | Recarga | +% vel ataque | +2.5%/nível | +5% | 2 | 900💎 |
| 6 | Área | +% área | +5%/nível | +10% | 2 | 300💎 |
| 7 | Velocidade | +% vel projétil | +10%/nível | +20% | 2 | 300💎 |
| 8 | Duração | +% duração | +15%/nível | +30% | 2 | 300💎 |
| 9 | Quantidade | +1 projétil | fixo | — | 1 | 5.000💎 |
| 10 | Movimento | +% vel player | +5%/nível | +10% | 2 | 300💎 |
| 11 | Magnetismo | +% coleta XP | +25%/nível | +50% | 2 | 300💎 |
| 12 | Sorte | +% chance raro | +10%/nível | +30% | 3 | 600💎 |
| 13 | Crescimento | +% XP | +3%/nível | +15% | 4 | 900💎 |
| 14 | Riqueza | +% diamantes | +10%/nível | +50% | 4 | 200💎 |
| 15 | Maldição | inimigos mais difíceis + +% recompensas | +10%/nível | +50% | 4 | 1.700💎 |
| 16 | Ressurreição | revive com 50% HP | fixo | — | 1 | 10.000💎 |

### Arquitetura agnóstica por classe
```
ClassDefinition (stats base) + PermanentUpgrades (multiplicadores) = Stats finais
```
GetBonus() retorna o multiplicador. PlayerClassManager aplica sobre stats base da classe selecionada.

### Maldição
Aumenta dificuldade (inimigos mais fortes) E aumenta recompensas (+10% diamantes e XP por nível).

---

## 10. TELEMETRIA (implementar antes do lançamento)

**Ferramenta:** Unity Analytics (gratuito)

**Eventos planejados:**
- `zona_iniciada`, `zona_completada` (zona, tempo, kills, classe)
- `game_over` (zona, causa, classe)
- `classe_selecionada`, `classe_comprada`
- `boss_derrotado`, `boss_matou_player`
- `level_up_escolha` (boost escolhido, opções recusadas)
- `poder_especial_usado`
- `diamante_coletado` (fonte, quantidade)
- `upgrade_permanente_comprado`

**Por que é estratégico:** permite identificar onde jogadores param de jogar, qual classe é mais popular, qual boost ninguém escolhe.

**LGPD:** informar na política de privacidade que coleta dados anônimos de gameplay.

---

## 11. ASSETS E FERRAMENTAS

### Unity e Plugins
- Unity 6 (6000.0.75f1 LTS)
- DOTween Pro (instalado)
- Magic Effects (instalado)

### Assets comprados
- Mobile Fantasy Game Interface — Craftpix (UI oficial)
- Fonte: Straight Pixel Gothic (Assets/Art/Fonts/)

### Assets planejados (Unity Asset Store Summer Sale 10/06/2026)
- **Feel (MoreMountains)** — screen shake, hit-stop — **PRIORIDADE ALTA**
- **The Ultimate Magic Spells Sound Effects Pack – Cyberwave Orchestra** — SFX

### Sprites das classes (em Assets/Art/Characters/Hero/)
- Guerreiro: Swordsman_lvl1/Without_shadow/ (PPU=32)
- Mago: Lightning Mage/ (PPU=100, worldScale=2.0)
- Assassino: Assassino/ (PPU=32)
- Necromante: Necromante/ (PPU=100, worldScale=2.0)
- Paladino: Paladino/ (PPU=100, worldScale=1.8)
- Caçador: Caçador/ (PPU=32, worldScale=1.4)

### Effects disponíveis (Assets/Art/Effects/)
- Slash_1/PNG/1-10 — ✅ Single, 496×496, prontos
- Explosions/PNG/ — ✅ Single, 256×256 e 128×128, prontos
- TopDown/PNG/Explosion1-6 — ✅ Single, 64-256px, prontos
- RPG_Icons — 320 ícones PNG prontos

---

## 12. TIPOGRAFIA

- **Fonte gótica (Straight Pixel Gothic):** EXCLUSIVA da tela de Lore
  - TMP Asset: Assets/Art/Fonts/StraightPixelGothic_TMP_v2.asset
  - NomeBioma: fontSize=90, Pos Y=250
  - TextoLore: fontSize=52
  - Instrucao: fontSize=24
- **Fonte padrão TMP:** TODOS os menus, HUD, boosts, upgrades
  - Razão: acessibilidade e legibilidade durante gameplay

---

## 13. LORE DAS ZONAS

### Zona 1 — Floresta de Veremoth ✅ implementada
Floresta corrompida. Props: árvores mortas, fungos, raízes, névoa baixa.

### Zona 2 — Cavernas de Khorduum (pendente)
Cavernas de cristal escuro com veias âmbar.

### Zona 3 — Cemitério de Valdross (pendente)
Cemitério antigo. Props: tumbas, cruzes, árvores-chorão, lanternas.

### Zona 4 — Pântano de Gorveth (pendente)
Pântano denso. Props: troncos, água negra, plantas carnívoras, névoa alta.

### Zona 5 — Campo de Arkenfall (pendente)
Campo de batalha abandonado. Props: crateras, máquinas de guerra, bandeiras.

---

## 14. SEQUÊNCIA DE SETUP NO UNITY (ordem obrigatória)

1. Ctrl+R
2. Solengard → Setup Animations
3. Solengard → Classes → Setup Hero Animations
4. Solengard → Effects → Fix Slash SpriteMode
5. Solengard → Effects → Copiar Efeitos para Resources
6. Solengard → Rebuild GameScene
7. Solengard → Setup All
8. Arrastar ZoneManager + ProceduralArenaSystem no GameManager
9. Inspector ZoneManager: testBossSpawnAt=30 (teste)
10. Ctrl+S e Play

---

## 15. CHECKLIST DE REGRESSÃO (rodar após qualquer mudança)

1. [ ] Player se move e PARA ao soltar controles
2. [ ] Player ataca e mata inimigos normais
3. [ ] Player toma dano e morre
4. [ ] Game Over aparece ao morrer
5. [ ] Boss aparece na zona
6. [ ] Boss TOMA DANO ao ser atacado
7. [ ] Boss morre e zona avança
8. [ ] Cenário aparece em todas as direções
9. [ ] Cristais de XP ficam parados e podem ser coletados
10. [ ] Level-up aparece e pausa o jogo
11. [ ] Lore aparece ao iniciar e ao trocar de zona
12. [ ] Transição entre zonas limpa
13. [ ] Sessão salva restaura E inicia spawn
14. [ ] Classes: cada uma com VFX distinto, ataque só no range

---

## 16. ROADMAP

### BLOCO A — Jogabilidade core (fazer primeiro)
1. ✅ Loop principal funcionando
2. ✅ 6 classes com VFX procedural distinto
3. ⬜ HUD reestruturado com Mobile Fantasy UI
4. ⬜ Boosts durante a run (LevelUpUI com visual correto)
5. ⬜ Poder especial por classe (botão no HUD)

### BLOCO B — Experiência completa
6. ⬜ Tela de seleção de personagem
7. ⬜ Joystick mobile validado
8. ⬜ Build Android APK (beta tester)

### BLOCO C — Meta-progressão e monetização
9. ⬜ PermanentUpgradeManager.cs
10. ⬜ Loja de upgrades (tela com Mobile Fantasy UI)
11. ⬜ Rewarded ads + bônus diário + IAP

### BLOCO D — Polish e lançamento
12. ⬜ Feel (MoreMountains) — screen shake, hit-stop
13. ⬜ Som (trilha dark fantasy + SFX)
14. ⬜ Telemetria (Unity Analytics)
15. ⬜ Build iOS + submissão às lojas
16. ⬜ Steam (Fase 2, pago)

---

## 17. PRÓXIMO PROJETO (registrado)

Após o Solengard: Metroidvania RPG platformer inspirado em Castlevania: Symphony of the Night.
**Decisão de processo:** fazer GDD antes de qualquer código.

---

*Última atualização: sessão 2026-06-08*
*Commits recentes: 1eeaa59 (mago range), ad19c1b (gitignore), 668a523 (fonte gótica)*
