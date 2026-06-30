# UI Toolkit — Pendências (versão final)

Pendências levantadas no piloto da tela de **Configurações** (UI Toolkit, Steam-first).
O piloto está **funcional e validado** (sliders, abas, dropdown, restaurar, persistência).
Estes itens são refinos para a versão final / próximas telas.

## Refinos visuais (versão final)

- [ ] **Proporção/tamanho**: a Config ficou pequena na tela. Calibrar `max-width` do painel
      (`.config-panel` em `Config.uss`) e/ou a **Reference Resolution** do
      `SolengardPanelSettings.asset` (hoje 1920×1080, Scale With Screen Size, Match 0.5).
- [ ] **Arte de fundo**: hoje é cor sólida `#16151C` (`.config-screen`). Criar arte temática
      dark fantasy de fundo (textura/imagem) no lugar do sólido.
- [ ] **Blur dinâmico**: quando a Config abre **durante o gameplay**, aplicar blur no fundo do
      jogo congelado. Envolve: detecção automática menu vs gameplay + captura de tela (render
      texture) + shader de blur por trás do painel. (No menu, mantém a arte de fundo.)

## Refinos técnicos (anotados durante o piloto)

- [ ] **Glow do título** (`text-shadow`): removido porque o UI Builder desta versão rejeita a
      propriedade (quebra abrir o uxml). Reintroduzir por um caminho aceito — ex.: `-unity-text-outline`
      sutil, ou textura/elemento de glow atrás do título.
- [ ] **Knob deslizante do toggle**: hoje é uma pílula que fica âmbar quando ON (sem knob).
      Implementar knob que desliza (provável VisualElement custom em vez do `<Toggle>` nativo).
- [ ] **Fill do slider com gradiente**: hoje é cor sólida âmbar (`.sol-slider-fill`). Aplicar
      gradiente âmbar (`linear-gradient(90deg,#8a6520,#D4A03C)` do design) via textura/background-image.
- [ ] **Estender `SettingsManager`** para religar as abas hoje só-visuais:
      - Aba **Vídeo**: resolução (`Screen.SetResolution`), tela cheia (`Screen.fullScreen`),
        qualidade (`QualitySettings.SetQualityLevel`), V-Sync (`QualitySettings.vSyncCount`),
        números de dano (flag de gameplay).
      - Aba **Conta**: Conta/Login (fluxo de `AuthSystem`), editar perfil.
- [ ] **Integrar MMSoundManager (Feel)**: hoje o `ApplyAudioToEngine()` é fallback e mapeia só
      Música → `AudioListener.volume`. O slider **SFX** persiste mas não tem efeito audível.
      Conectar música e SFX por track real no MMSoundManager.
- [ ] **Pacotes de idioma**: o dropdown de Idioma persiste `PREF_LANGUAGE`, mas a UI não troca de
      idioma de verdade. Integrar com `LocalizationManager` + tabelas de tradução para o texto
      mudar ao selecionar.

## Referências

- Padrão de tela: ver memória `uitoolkit-screen-pattern`.
- Fontes: FontAsset TextCore (não TMP) — `Assets/UI/Editor/CreateTextCoreFontAssets.cs`.
- Arquivos do piloto: `Assets/UI/Screens/Config/` (`Config.uxml`, `Config.uss`),
  `Assets/UI/Scripts/ConfigPanelController.cs`, `Assets/UI/Styles/` (`tokens.uss`, `common.uss`).
