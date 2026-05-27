using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Menu: Solengard ▸ Layout MainMenu  — monta hierarquia UI estilo Squad Busters na cena MainMenu
//       Solengard ▸ Layout GameScene — monta HUD na cena GameScene
// Idempotente: só cria elementos ausentes; só faz wire de campos null.
public static class SolengardLayoutSetup
{
    const string MAIN_MENU_SCENE = "MainMenu";
    const string GAME_SCENE      = "GameScene";

    // ── Menu items ──────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Layout MainMenu")]
    static void LayoutMainMenu()
    {
        if (!ValidateScene(MAIN_MENU_SCENE)) return;
        var scene = EditorSceneManager.GetActiveScene();
        var log   = new StringBuilder();
        int total = RunLayoutMainMenu(log);
        if (total > 0) EditorSceneManager.MarkSceneDirty(scene);

        var sb = new StringBuilder();
        sb.AppendLine(total > 0
            ? $"✓ {total} elemento(s) criado(s)/configurado(s):\n{log}"
            : "Layout já estava montado — nenhuma alteração necessária.");
        sb.AppendLine("\n─────────────────────────────────────");
        sb.AppendLine("Pendências manuais:");
        sb.AppendLine("• Ajustar posição/tamanho visual dos botões no Canvas");
        sb.AppendLine("• Adicionar sprites/ícones nas tabs e panels");
        sb.AppendLine("• NomeJogador: alimentar via script com nome do perfil");
        EditorUtility.DisplayDialog("Solengard Layout MainMenu — Concluído", sb.ToString(), "OK");
    }

    [MenuItem("Solengard/Layout MainMenu", validate = true)]
    static bool ValidateLayoutMainMenu() =>
        EditorSceneManager.GetActiveScene().name == MAIN_MENU_SCENE;

    [MenuItem("Solengard/Layout GameScene")]
    static void LayoutGameScene()
    {
        if (!ValidateScene(GAME_SCENE)) return;
        var scene = EditorSceneManager.GetActiveScene();
        var log   = new StringBuilder();
        int total = RunLayoutGameScene(log);
        if (total > 0) EditorSceneManager.MarkSceneDirty(scene);

        var sb = new StringBuilder();
        sb.AppendLine(total > 0
            ? $"✓ {total} elemento(s) criado(s)/configurado(s):\n{log}"
            : "Layout já estava montado — nenhuma alteração necessária.");
        sb.AppendLine("\n─────────────────────────────────────");
        sb.AppendLine("Pendências manuais:");
        sb.AppendLine("• TimerText: adicionar campo 'timerText' em HUDComplete e assinar WaveTimerSystem.OnTimerTick");
        sb.AppendLine("• bannerWave / textoBannerWave: arrastar WaveWarningUI banner para HUDComplete.bannerWave");
        EditorUtility.DisplayDialog("Solengard Layout GameScene — Concluído", sb.ToString(), "OK");
    }

    [MenuItem("Solengard/Layout GameScene", validate = true)]
    static bool ValidateLayoutGameScene() =>
        EditorSceneManager.GetActiveScene().name == GAME_SCENE;

    // ── MainMenu Layout ─────────────────────────────────────────────────────────

    static int RunLayoutMainMenu(StringBuilder log)
    {
        int total = 0;

        // Canvas
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Layout MainMenu");
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = canvasGO.AddComponent<CanvasScaler>();
            s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1080f, 1920f);
            s.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            log.AppendLine("  Canvas criado"); total++;
        }
        var canvasTr = canvasGO.transform;
        var mmm      = canvasGO.GetComponent<MainMenuManager>() ?? canvasGO.AddComponent<MainMenuManager>();
        var mmmSO    = new SerializedObject(mmm);

        // BG
        {
            var (go, isNew) = FindOrCreateUI(canvasTr, "BG");
            if (isNew) { StretchFull(RT(go)); EnsureImage(go, Hex("#0A0A1A")); log.AppendLine("  BG"); total++; }
        }

        // TopBar
        GameObject topBarGO;
        GameObject textoDiamantesGO;
        GameObject botaoConfigGO;
        {
            var (go, isNew) = FindOrCreateUI(canvasTr, "TopBar");
            topBarGO = go;
            if (isNew)
            {
                AnchorTopBar(RT(go), 140f);
                EnsureImage(go, Hex("#00000080"));
                log.AppendLine("  TopBar"); total++;
            }
            // Enforce sibling order: TopBar right after BG (index 0), before all content
            topBarGO.transform.SetSiblingIndex(1);

            var tr = go.transform;

            // Warn about legacy elements from CreateMainMenuScene that won't be auto-removed
            if (canvasTr.Find("PlayerInfoPanel") != null)
                log.AppendLine("  ⚠ PlayerInfoPanel (legado) detectado — filhos migrados para TopBar");
            if (canvasTr.Find("MainButtons") != null)
                log.AppendLine("  ⚠ MainButtons (legado) detectado — botões migrados para novo layout");

            // Use deep search so elements that exist under PlayerInfoPanel/MainButtons get reparented here
            { var (c,n)=FindReparentOrCreateUI(tr,canvasTr,"AvatarPlaceholder"); if(n){ SetRect(RT(c),new(0,.5f),new(0,.5f),new(0,.5f),new(20,0),new(90,90)); EnsureImage(c,Hex("#2A2A4A")); log.AppendLine("  TopBar/AvatarPlaceholder"); total++; } }
            { var (c,n)=FindReparentOrCreateUI(tr,canvasTr,"NomeJogador");        if(n){ SetRect(RT(c),new(0,.5f),new(0,.5f),new(0,.5f),new(125,0),new(280,55)); EnsureTMP(c,"Jogador",32f,Color.white); log.AppendLine("  TopBar/NomeJogador"); total++; } }

            var (dGO,dN)=FindReparentOrCreateUI(tr,canvasTr,"TextoDiamantes");
            textoDiamantesGO=dGO;
            if(dN){ SetRect(RT(dGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(80,0),new(220,56)); EnsureTMP(dGO,"0 💎",36f,Hex("#FFD700")); log.AppendLine("  TopBar/TextoDiamantes"); total++; }

            { var (c,n)=FindReparentOrCreateUI(tr,canvasTr,"TextoMoedas"); if(n){ SetRect(RT(c),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(310,0),new(220,56)); EnsureTMP(c,"0 🪙",36f,Hex("#C0C0C0")); log.AppendLine("  TopBar/TextoMoedas"); total++; } }

            var (cfgGO,cfgN)=FindReparentOrCreateUI(tr,canvasTr,"BotaoConfiguracoes");
            botaoConfigGO=cfgGO;
            if(cfgN){ SetRect(RT(cfgGO),new(1,.5f),new(1,.5f),new(1,.5f),new(-60,0),new(70,70)); EnsureImage(cfgGO,Hex("#1A1A2A")); EnsureButton(cfgGO); AddLabel(cfgGO,"⚙",32f,Color.white); log.AppendLine("  TopBar/BotaoConfiguracoes"); total++; }

            TryWire(mmmSO,"textoDiamantes",    textoDiamantesGO.GetComponent<TextMeshProUGUI>(),log);
            TryWire(mmmSO,"botaoConfiguracoes",botaoConfigGO.GetComponent<Button>(),log);
        }

        // CenterArea
        GameObject textoTemporadaGO, textoStreakGO;
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"CenterArea");
            if(isNew){ var rt=RT(go); rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=new(0,140); rt.offsetMax=new(0,-140); log.AppendLine("  CenterArea"); total++; }
            var tr=go.transform;

            { var (c,n)=FindOrCreateUI(tr,"TextoTitulo"); if(n){ SetRect(RT(c),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,200),new(800,100)); var t=EnsureTMP(c,"SOLENGARD",72f,Hex("#C8A0FF")); t.fontStyle=FontStyles.Bold; log.AppendLine("  CenterArea/TextoTitulo"); total++; } }

            var (tGO,tN)=FindOrCreateUI(tr,"TextoTemporada");
            textoTemporadaGO=tGO;
            if(tN){ SetRect(RT(tGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,120),new(600,60)); EnsureTMP(tGO,"Temporada 1",28f,Hex("#8080AA")); log.AppendLine("  CenterArea/TextoTemporada"); total++; }

            var (sGO,sN)=FindOrCreateUI(tr,"TextoStreak");
            textoStreakGO=sGO;
            if(sN){ SetRect(RT(sGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,20),new(400,60)); EnsureTMP(sGO,"🔥 Dia 1",28f,Hex("#FFD700")); log.AppendLine("  CenterArea/TextoStreak"); total++; }

            TryWire(mmmSO,"textoNivelPasse", textoTemporadaGO.GetComponent<TextMeshProUGUI>(),log);
            TryWire(mmmSO,"textoStreakLogin",textoStreakGO.GetComponent<TextMeshProUGUI>(),log);
        }

        // LeftPanel
        GameObject botaoRankingGO;
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"LeftPanel");
            if(isNew){ SetRect(RT(go),new(0,.35f),new(0,.65f),new(0,.5f),Vector2.zero,new(120,0)); log.AppendLine("  LeftPanel"); total++; }
            var vlg=go.GetComponent<VerticalLayoutGroup>()??go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing=20f; vlg.childAlignment=TextAnchor.MiddleCenter;
            vlg.childControlWidth=false; vlg.childControlHeight=false;
            vlg.childForceExpandWidth=false; vlg.childForceExpandHeight=false;
            vlg.padding=new RectOffset(10,10,30,30);
            var tr=go.transform;

            { var (c,n)=FindReparentOrCreateUI(tr,canvasTr,"BotaoMissoes"); if(n){ RT(c).sizeDelta=new(100,100); EnsureImage(c,Hex("#1A1A3A")); EnsureButton(c); AddLabel(c,"MISSÕES",22f,Color.white); log.AppendLine("  LeftPanel/BotaoMissoes"); total++; } }
            { var (c,n)=FindReparentOrCreateUI(tr,canvasTr,"BotaoPasse");   if(n){ RT(c).sizeDelta=new(100,100); EnsureImage(c,Hex("#1A1A3A")); EnsureButton(c); AddLabel(c,"PASSE",22f,Color.white);   log.AppendLine("  LeftPanel/BotaoPasse");   total++; } }

            var (rkGO,rkN)=FindReparentOrCreateUI(tr,canvasTr,"BotaoRanking");
            botaoRankingGO=rkGO;
            if(rkN){ RT(rkGO).sizeDelta=new(100,100); EnsureImage(rkGO,Hex("#1A1A3A")); EnsureButton(rkGO); AddLabel(rkGO,"RANKING",22f,Color.white); log.AppendLine("  LeftPanel/BotaoRanking"); total++; }

            TryWire(mmmSO,"botaoRanking",botaoRankingGO.GetComponent<Button>(),log);
        }

        // RightPanel
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"RightPanel");
            if(isNew){ SetRect(RT(go),new(1,.5f),new(1,.5f),new(1,.5f),Vector2.zero,new(120,200)); log.AppendLine("  RightPanel"); total++; }
            var (c,n)=FindOrCreateUI(go.transform,"BotaoOferta");
            if(n){ SetRect(RT(c),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(110,110)); EnsureImage(c,Hex("#3A1A0A")); EnsureButton(c); var lbl=AddLabel(c,"OFERTA\nQUENTE!",22f,Hex("#FF6600")); lbl.enableWordWrapping=false; log.AppendLine("  RightPanel/BotaoOferta"); total++; }
        }

        // PlayButton
        GameObject playButtonGO;
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"PlayButton");
            playButtonGO=go;
            if(isNew)
            {
                SetRect(RT(go),new(.5f,0),new(.5f,0),new(.5f,0),new(0,180),new(700,130));
                EnsureImage(go,Hex("#5A1090")); EnsureButton(go);
                var lbl=AddLabel(go,"⚔ JOGAR",56f,Color.white); lbl.fontStyle=FontStyles.Bold;
                var sh=lbl.gameObject.AddComponent<Shadow>(); sh.effectDistance=new(0,-4); sh.effectColor=new Color(0,0,0,.5f);
                log.AppendLine("  PlayButton"); total++;
            }
            TryWire(mmmSO,"botaoJogar",playButtonGO.GetComponent<Button>(),log);
        }

        // BottomTabs
        GameObject tabLojaGO, tabMissoesGO, tabPasseGO;
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"BottomTabs");
            if(isNew){ AnchorBottomBar(RT(go),140f); EnsureImage(go,Hex("#0D0D1F")); log.AppendLine("  BottomTabs"); total++; }
            var hlg=go.GetComponent<HorizontalLayoutGroup>()??go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment=TextAnchor.MiddleCenter; hlg.childControlWidth=true; hlg.childControlHeight=true;
            hlg.childForceExpandWidth=true; hlg.childForceExpandHeight=true; hlg.spacing=0;
            var tr=go.transform;

            var (lGO,lN)=FindOrCreateUI(tr,"TabLoja");    tabLojaGO=lGO;
            if(lN){ EnsureImage(lGO,Hex("#0D0D1F")); EnsureButton(lGO); AddLabel(lGO,"LOJA",24f,Color.white);    log.AppendLine("  BottomTabs/TabLoja");    total++; }

            var (mGO,mN)=FindOrCreateUI(tr,"TabMissoes"); tabMissoesGO=mGO;
            if(mN){ EnsureImage(mGO,Hex("#0D0D1F")); EnsureButton(mGO); AddLabel(mGO,"MISSÕES",24f,Color.white); log.AppendLine("  BottomTabs/TabMissoes"); total++; }

            { var (c,n)=FindOrCreateUI(tr,"TabJogar"); if(n){ EnsureImage(c,Hex("#5A1090")); EnsureButton(c); AddLabel(c,"⚔ JOGAR",24f,Color.white); log.AppendLine("  BottomTabs/TabJogar"); total++; } }

            var (pGO,pN)=FindOrCreateUI(tr,"TabPasse");   tabPasseGO=pGO;
            if(pN){ EnsureImage(pGO,Hex("#0D0D1F")); EnsureButton(pGO); AddLabel(pGO,"PASSE",24f,Color.white);   log.AppendLine("  BottomTabs/TabPasse");   total++; }

            { var (c,n)=FindOrCreateUI(tr,"TabConfigs"); if(n){ EnsureImage(c,Hex("#0D0D1F")); EnsureButton(c); AddLabel(c,"CONFIG",24f,Color.white); log.AppendLine("  BottomTabs/TabConfigs"); total++; } }

            TryWire(mmmSO,"botaoLoja",    tabLojaGO.GetComponent<Button>(),    log);
            TryWire(mmmSO,"botaoMissoes", tabMissoesGO.GetComponent<Button>(), log);
            TryWire(mmmSO,"botaoPasse",   tabPasseGO.GetComponent<Button>(),   log);
        }

        // Panels (full-screen, inactive)
        var panels = new (string name, string field)[]
        {
            ("PainelLoja","painelLoja"), ("PainelPasse","painelPasse"), ("PainelMissoes","painelMissoes"),
            ("PainelRanking","painelRanking"), ("PainelConfiguracoes","painelConfiguracoes"),
        };
        foreach (var (name,field) in panels)
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,name);
            if(isNew){ StretchFull(RT(go)); EnsureImage(go,Hex("#0D0D1F")); go.SetActive(false); log.AppendLine($"  {name}"); total++; }
            TryWire(mmmSO,field,go,log);
        }

        // PopupRecompensa
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"PopupRecompensa");
            if(isNew){ SetRect(RT(go),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(700,500)); EnsureImage(go,Hex("#1E0A3C")); go.SetActive(false); log.AppendLine("  PopupRecompensa"); total++; }
            var tr=go.transform;

            { var (c,n)=FindOrCreateUI(tr,"TituloPopup"); if(n){ SetRect(RT(c),new(0,1),new(1,1),new(.5f,1),Vector2.zero,new(0,80)); var t=EnsureTMP(c,"RECOMPENSA DIÁRIA",42f,Color.white); t.fontStyle=FontStyles.Bold; log.AppendLine("  Popup/TituloPopup"); total++; } }

            var (rdGO,rdN)=FindOrCreateUI(tr,"TextoRecompensaDia");
            if(rdN){ SetRect(RT(rdGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,60),new(600,60)); EnsureTMP(rdGO,"Dia 1 de 7",32f,Color.white); log.AppendLine("  Popup/TextoRecompensaDia"); total++; }

            var (rdmGO,rdmN)=FindOrCreateUI(tr,"TextoRecompensaDiamantes");
            if(rdmN){ SetRect(RT(rdmGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,-20),new(600,60)); EnsureTMP(rdmGO,"+10 diamantes",28f,Hex("#FFD700")); log.AppendLine("  Popup/TextoRecompensaDiamantes"); total++; }

            var (btnGO,btnN)=FindOrCreateUI(tr,"BotaoColetarRecompensa");
            if(btnN){ SetRect(RT(btnGO),new(.5f,0),new(.5f,0),new(.5f,0),new(0,20),new(500,100)); EnsureImage(btnGO,Hex("#5A1090")); EnsureButton(btnGO); AddLabel(btnGO,"COLETAR",40f,Color.white); log.AppendLine("  Popup/BotaoColetarRecompensa"); total++; }

            TryWire(mmmSO,"popupRecompensa",          go,                                         log);
            TryWire(mmmSO,"textoRecompensaDia",       rdGO.GetComponent<TextMeshProUGUI>(),       log);
            TryWire(mmmSO,"textoRecompensaDiamantes", rdmGO.GetComponent<TextMeshProUGUI>(),      log);
            TryWire(mmmSO,"botaoColetarRecompensa",   btnGO.GetComponent<Button>(),               log);
        }

        mmmSO.ApplyModifiedProperties();
        return total;
    }

    // ── GameScene HUD Layout ────────────────────────────────────────────────────

    static int RunLayoutGameScene(StringBuilder log)
    {
        int total = 0;

        // HUD Canvas
        var hudGO = GameObject.Find("HUD Canvas");
        if (hudGO == null)
        {
            hudGO = new GameObject("HUD Canvas");
            Undo.RegisterCreatedObjectUndo(hudGO, "Layout GameScene");
            var c = hudGO.AddComponent<Canvas>();
            c.renderMode   = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;
            var s = hudGO.AddComponent<CanvasScaler>();
            s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1080f, 1920f);
            s.matchWidthOrHeight  = 0.5f;
            hudGO.AddComponent<GraphicRaycaster>();
            log.AppendLine("  HUD Canvas criado"); total++;
        }
        var hudTr = hudGO.transform;

        var hud   = Object.FindFirstObjectByType<HUDComplete>(FindObjectsInactive.Include);
        if (hud == null)
        {
            hud = hudGO.AddComponent<HUDComplete>();
            log.AppendLine("  HUDComplete adicionado"); total++;
        }
        var hudSO = new SerializedObject(hud);

        // TopHudBar
        {
            var (go,isNew)=FindOrCreateUI(hudTr,"TopHudBar");
            if(isNew){ AnchorTopBar(RT(go),100f); EnsureImage(go,Hex("#00000060")); log.AppendLine("  TopHudBar"); total++; }
            var tr=go.transform;

            var (slGO,slN)=FindOrCreateUI(tr,"HealthSlider");
            if(slN){ SetRect(RT(slGO),new(0,.5f),new(0,.5f),new(0,.5f),new(20,-50),new(350,35)); BuildSlider(slGO); log.AppendLine("  HealthSlider"); total++; }

            var (wtGO,wtN)=FindOrCreateUI(tr,"WaveText");
            if(wtN){ SetRect(RT(wtGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,-50),new(400,60)); var t=EnsureTMP(wtGO,"Wave 1/5",40f,Color.white); t.fontStyle=FontStyles.Bold; log.AppendLine("  WaveText"); total++; }

            var (dtGO,dtN)=FindOrCreateUI(tr,"DiamondText");
            if(dtN){ SetRect(RT(dtGO),new(1,.5f),new(1,.5f),new(1,.5f),new(-20,-50),new(250,55)); EnsureTMP(dtGO,"0 💎",34f,Hex("#FFD700")); log.AppendLine("  DiamondText"); total++; }

            TryWire(hudSO,"barraVida",     slGO.GetComponent<Slider>(),                log);
            TryWire(hudSO,"textoWave",     wtGO.GetComponent<TextMeshProUGUI>(),       log);
            TryWire(hudSO,"textoDiamantes",dtGO.GetComponent<TextMeshProUGUI>(),       log);
        }

        // SecondRow
        {
            var (go,isNew)=FindOrCreateUI(hudTr,"SecondRow");
            if(isNew){ SetRect(RT(go),new(0,1),new(1,1),new(.5f,1),new(0,-100),new(0,60)); log.AppendLine("  SecondRow"); total++; }
            var tr=go.transform;

            var (scGO,scN)=FindOrCreateUI(tr,"ScoreText");
            if(scN){ SetRect(RT(scGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(300,50)); EnsureTMP(scGO,"0",28f,Hex("#AAAAAA")); log.AppendLine("  ScoreText"); total++; }

            { var (c,n)=FindOrCreateUI(tr,"TimerText"); if(n){ SetRect(RT(c),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(200,0),new(200,55)); var t=EnsureTMP(c,"60",36f,Color.white); t.fontStyle=FontStyles.Bold; log.AppendLine("  TimerText (wire manual)"); total++; } }

            var (pbGO,pbN)=FindOrCreateUI(tr,"PauseButton");
            if(pbN){ SetRect(RT(pbGO),new(1,.5f),new(1,.5f),new(1,.5f),new(-20,0),new(70,70)); EnsureImage(pbGO,Hex("#00000060")); EnsureButton(pbGO); AddLabel(pbGO,"II",28f,Color.white); log.AppendLine("  PauseButton"); total++; }

            TryWire(hudSO,"textoScore",scGO.GetComponent<TextMeshProUGUI>(),log);
            TryWire(hudSO,"botaoPause",pbGO.GetComponent<Button>(),log);
        }

        // BottomHud
        {
            var (go,isNew)=FindOrCreateUI(hudTr,"BottomHud");
            if(isNew){ AnchorBottomBar(RT(go),120f); EnsureImage(go,Hex("#00000060")); log.AppendLine("  BottomHud"); total++; }
            var tr=go.transform;

            var (mpGO,mpN)=FindOrCreateUI(tr,"MissionPanel");
            if(mpN){ SetRect(RT(mpGO),new(0,.5f),new(0,.5f),new(0,.5f),new(20,0),new(400,80)); EnsureImage(mpGO,Hex("#00000080")); log.AppendLine("  MissionPanel"); total++; }
            var mptr=mpGO.transform;

            var (tmGO,tmN)=FindOrCreateUI(mptr,"TextoMissao");
            if(tmN){ SetRect(RT(tmGO),new(0,1),new(1,1),new(.5f,1),Vector2.zero,new(0,42)); EnsureTMP(tmGO,"Missão ativa",24f,Color.white); log.AppendLine("  TextoMissao"); total++; }

            var (tpGO,tpN)=FindOrCreateUI(mptr,"TextoProgressoMissao");
            if(tpN){ SetRect(RT(tpGO),new(0,0),new(1,0),new(.5f,0),Vector2.zero,new(0,36)); EnsureTMP(tpGO,"0/10",22f,Hex("#FFD700")); log.AppendLine("  TextoProgressoMissao"); total++; }

            TryWire(hudSO,"painelMissao",        mpGO,                                     log);
            TryWire(hudSO,"textoMissao",         tmGO.GetComponent<TextMeshProUGUI>(),     log);
            TryWire(hudSO,"textoProgressoMissao",tpGO.GetComponent<TextMeshProUGUI>(),     log);
        }

        hudSO.ApplyModifiedProperties();
        return total;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    static Color Hex(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }

    static (GameObject go, bool isNew) FindOrCreateUI(Transform parent, string name)
    {
        var found = parent.Find(name);
        if (found != null) return (found.gameObject, false);
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Solengard Layout");
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return (go, true);
    }

    static Transform FindInHierarchy(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            var found = FindInHierarchy(child, name);
            if (found != null) return found;
        }
        return null;
    }

    static (GameObject go, bool isNew) FindReparentOrCreateUI(Transform targetParent, Transform searchRoot, string name)
    {
        var direct = targetParent.Find(name);
        if (direct != null) return (direct.gameObject, false);

        var found = FindInHierarchy(searchRoot, name);
        if (found != null)
        {
            Undo.SetTransformParent(found, targetParent, "Solengard Layout");
            return (found.gameObject, true);
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Solengard Layout");
        go.AddComponent<RectTransform>();
        go.transform.SetParent(targetParent, false);
        return (go, true);
    }

    static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();

    static void StretchFull(RectTransform rt)
    { rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=Vector2.zero; rt.offsetMax=Vector2.zero; }

    static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    { rt.anchorMin=aMin; rt.anchorMax=aMax; rt.pivot=pivot; rt.anchoredPosition=pos; rt.sizeDelta=size; }

    static void AnchorTopBar(RectTransform rt, float height)
    { rt.anchorMin=new(0,1); rt.anchorMax=new(1,1); rt.pivot=new(.5f,1); rt.anchoredPosition=Vector2.zero; rt.sizeDelta=new(0,height); }

    static void AnchorBottomBar(RectTransform rt, float height)
    { rt.anchorMin=new(0,0); rt.anchorMax=new(1,0); rt.pivot=new(.5f,0); rt.anchoredPosition=Vector2.zero; rt.sizeDelta=new(0,height); }

    static Image EnsureImage(GameObject go, Color color)
    { var i=go.GetComponent<Image>()??go.AddComponent<Image>(); i.color=color; return i; }

    static TextMeshProUGUI EnsureTMP(GameObject go, string text, float size, Color color)
    {
        var t=go.GetComponent<TextMeshProUGUI>()??go.AddComponent<TextMeshProUGUI>();
        t.text=text; t.fontSize=size; t.color=color; t.alignment=TextAlignmentOptions.Center;
        return t;
    }

    static Button EnsureButton(GameObject go)
    {
        var img=go.GetComponent<Image>()??go.AddComponent<Image>();
        var btn=go.GetComponent<Button>()??go.AddComponent<Button>();
        btn.targetGraphic=img; return btn;
    }

    static TextMeshProUGUI AddLabel(GameObject parent, string text, float size, Color color)
    {
        var found=parent.transform.Find("Label");
        GameObject lGO = found!=null ? found.gameObject : new GameObject("Label");
        if (found==null)
        {
            Undo.RegisterCreatedObjectUndo(lGO,"Solengard Layout");
            lGO.AddComponent<RectTransform>();
            lGO.transform.SetParent(parent.transform,false);
            StretchFull(lGO.GetComponent<RectTransform>());
        }
        return EnsureTMP(lGO,text,size,color);
    }

    static void BuildSlider(GameObject go)
    {
        var bg=new GameObject("Background"); Undo.RegisterCreatedObjectUndo(bg,"Solengard Layout");
        bg.transform.SetParent(go.transform,false);
        var bgImg=bg.AddComponent<Image>(); bgImg.color=new Color(.15f,.0f,.0f,1f);
        StretchFull(bg.GetComponent<RectTransform>());

        var fa=new GameObject("Fill Area"); Undo.RegisterCreatedObjectUndo(fa,"Solengard Layout");
        fa.transform.SetParent(go.transform,false);
        StretchFull(fa.AddComponent<RectTransform>());

        var fill=new GameObject("Fill"); Undo.RegisterCreatedObjectUndo(fill,"Solengard Layout");
        fill.transform.SetParent(fa.transform,false);
        var fillImg=fill.AddComponent<Image>(); fillImg.color=new Color(.8f,.1f,.1f,1f);
        var fillRT=fill.GetComponent<RectTransform>();
        fillRT.anchorMin=Vector2.zero; fillRT.anchorMax=Vector2.one; fillRT.offsetMin=Vector2.zero; fillRT.offsetMax=Vector2.zero;

        var slider=go.AddComponent<Slider>();
        slider.fillRect=fillRT; slider.value=1f; slider.maxValue=1f; slider.direction=Slider.Direction.LeftToRight;
    }

    static void TryWire(SerializedObject so, string prop, Object val, StringBuilder log)
    {
        if (val==null) return;
        var p=so.FindProperty(prop);
        if (p==null||p.objectReferenceValue!=null) return;
        p.objectReferenceValue=val;
        log.AppendLine($"  Wire {so.targetObject.GetType().Name}.{prop}");
    }

    static bool ValidateScene(string expected)
    {
        if (EditorSceneManager.GetActiveScene().name==expected) return true;
        EditorUtility.DisplayDialog("Solengard Layout",
            $"Abra a cena '{expected}' antes de executar este layout.\n\nCena atual: '{EditorSceneManager.GetActiveScene().name}'","OK");
        return false;
    }
}
