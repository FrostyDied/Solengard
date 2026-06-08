using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    static readonly string EXPORTED_UI = "Assets/Art/UI/MobileFantasyUI/Exported/";
    static Sprite LoadUI(string name)
    {
        var path     = EXPORTED_UI + name;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType     = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

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
            if(dN){ SetRect(RT(dGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(80,0),new(220,56)); EnsureTMP(dGO,"0 DIA",36f,Hex("#FFD700")); log.AppendLine("  TopBar/TextoDiamantes"); total++; }

            { var (c,n)=FindReparentOrCreateUI(tr,canvasTr,"TextoMoedas"); if(n){ SetRect(RT(c),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(310,0),new(220,56)); EnsureTMP(c,"0 G",36f,Hex("#C0C0C0")); log.AppendLine("  TopBar/TextoMoedas"); total++; } }

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

            // Always apply all positions — re-running layout corrects existing scenes
            { var (c,n)=FindOrCreateUI(tr,"TextoTitulo"); SetRect(RT(c),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,350),new(800,100)); var t=EnsureTMP(c,"SOLENGARD",72f,Hex("#C8A0FF")); t.fontStyle=FontStyles.Bold; if(n){ log.AppendLine("  CenterArea/TextoTitulo"); total++; } }

            var (tGO,tN)=FindOrCreateUI(tr,"TextoTemporada");
            textoTemporadaGO=tGO;
            SetRect(RT(tGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,280),new(600,60)); EnsureTMP(tGO,"Temporada 1",28f,Hex("#8080AA"));
            if(tN){ log.AppendLine("  CenterArea/TextoTemporada"); total++; }

            var (sGO,sN)=FindOrCreateUI(tr,"TextoStreak");
            textoStreakGO=sGO;
            SetRect(RT(sGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,230),new(400,60)); EnsureTMP(sGO,"* Dia 1",28f,Hex("#FFD700"));
            if(sN){ log.AppendLine("  CenterArea/TextoStreak"); total++; }

            // SeasonBanner — always apply position
            { var (bn,bnN)=FindOrCreateUI(tr,"SeasonBanner");
              SetRect(RT(bn),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,120),new(700,180));
              if(bnN){ EnsureImage(bn,Hex("#1E0A3C")); log.AppendLine("  CenterArea/SeasonBanner"); total++; }
              var (bnt,bntN)=FindOrCreateUI(bn.transform,"TextoSeasonBanner");
              if(bntN){ StretchFull(RT(bnt)); var tmp=EnsureTMP(bnt,"> Temporada das Sombras\nComplete 50 waves para ganhar a skin lendaria",28f,Color.white); tmp.textWrappingMode=TMPro.TextWrappingModes.Normal; log.AppendLine("  SeasonBanner/TextoSeasonBanner"); total++; } }

            // FeaturedContent — always apply position
            { var (fc,fcN)=FindOrCreateUI(tr,"FeaturedContent");
              SetRect(RT(fc),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,-60),new(700,160));
              if(fcN){ EnsureImage(fc,Hex("#0D0D2A")); log.AppendLine("  CenterArea/FeaturedContent"); total++; }
              var (mp,mpN)=FindOrCreateUI(fc.transform,"TextoMelhorPontuacao");
              if(mpN){ SetRect(RT(mp),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,30),new(680,60)); EnsureTMP(mp,"TOP Melhor Pontuacao: 0",32f,Hex("#FFD700")); log.AppendLine("  FeaturedContent/TextoMelhorPontuacao"); total++; }
              var (ur,urN)=FindOrCreateUI(fc.transform,"TextoUltimaRun");
              if(urN){ SetRect(RT(ur),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,-30),new(680,50)); EnsureTMP(ur,"> Ultima Run: --",26f,Hex("#AAAAAA")); log.AppendLine("  FeaturedContent/TextoUltimaRun"); total++; }
              TryWire(mmmSO,"textoMelhorPontuacao",mp.GetComponent<TextMeshProUGUI>(),log);
              TryWire(mmmSO,"textoUltimaRun",      ur.GetComponent<TextMeshProUGUI>(),log); }

            TryWire(mmmSO,"textoNivelPasse", textoTemporadaGO.GetComponent<TextMeshProUGUI>(),log);
            TryWire(mmmSO,"textoStreakLogin",textoStreakGO.GetComponent<TextMeshProUGUI>(),log);
        }

        // LeftPanel
        GameObject botaoOfertasGO, botaoBencaosGO, botaoBausGO;
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"LeftPanel");
            // Always apply: stretched anchor flush to left edge, pivot left-center (Squad Busters style)
            SetRect(RT(go),new(0,.35f),new(0,.65f),new(0,.5f),Vector2.zero,new(100,0));
            if(isNew){ log.AppendLine("  LeftPanel"); total++; }
            var vlg=go.GetComponent<VerticalLayoutGroup>()??go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing=20f; vlg.childAlignment=TextAnchor.MiddleCenter;
            vlg.childControlWidth=false; vlg.childControlHeight=false;
            vlg.childForceExpandWidth=false; vlg.childForceExpandHeight=false;
            vlg.padding=new RectOffset(5,5,20,20);
            var tr=go.transform;

            var (ofGO,ofN)=FindReparentOrCreateUI(tr,canvasTr,"BotaoOfertas"); botaoOfertasGO=ofGO;
            if(ofN){ RT(ofGO).sizeDelta=new(90,90); EnsureImage(ofGO,Hex("#2A1A0A")); EnsureButton(ofGO); var lbl=AddLabel(ofGO,"OFERTAS",16f,Color.white);  lbl.textWrappingMode=TMPro.TextWrappingModes.NoWrap; log.AppendLine("  LeftPanel/BotaoOfertas");  total++; }

            var (beGO,beN)=FindReparentOrCreateUI(tr,canvasTr,"BotaoBencaos"); botaoBencaosGO=beGO;
            if(beN){ RT(beGO).sizeDelta=new(90,90); EnsureImage(beGO,Hex("#0A1A2A")); EnsureButton(beGO); var lbl=AddLabel(beGO,"BÊNÇÃOS",16f,Color.white);  lbl.textWrappingMode=TMPro.TextWrappingModes.NoWrap; log.AppendLine("  LeftPanel/BotaoBencaos");  total++; }

            var (baGO,baN)=FindReparentOrCreateUI(tr,canvasTr,"BotaoBaus");    botaoBausGO=baGO;
            if(baN){ RT(baGO).sizeDelta=new(90,90); EnsureImage(baGO,Hex("#1A0A2A")); EnsureButton(baGO); var lbl=AddLabel(baGO,"BAÚS",16f,Color.white);      lbl.textWrappingMode=TMPro.TextWrappingModes.NoWrap; log.AppendLine("  LeftPanel/BotaoBaus");     total++; }

            TryWire(mmmSO,"botaoOfertas",botaoOfertasGO.GetComponent<Button>(),log);
            TryWire(mmmSO,"botaoBencaos",botaoBencaosGO.GetComponent<Button>(),log);
            TryWire(mmmSO,"botaoBaus",   botaoBausGO.GetComponent<Button>(),   log);
        }

        // RightPanel — remove if empty (BotaoOferta is now at canvas level)
        { var rpTr=canvasTr.Find("RightPanel");
          if(rpTr!=null && rpTr.childCount==0) total+=DestroyLegacyGO(canvasTr,"RightPanel",log); }

        // BotaoOferta — flush to right edge, symmetric to LeftPanel
        { var (bo,boN)=FindReparentOrCreateUI(canvasTr,canvasTr,"BotaoOferta");
          SetRect(RT(bo),new(1,.5f),new(1,.5f),new(1,.5f),new(-5,0),new(110,110));
          if(boN){ EnsureImage(bo,Hex("#3A1A0A")); EnsureButton(bo); var lbl=AddLabel(bo,"OFERTA\nQUENTE!",22f,Hex("#FF6600")); lbl.textWrappingMode=TMPro.TextWrappingModes.NoWrap; log.AppendLine("  BotaoOferta (borda direita)"); total++; } }

        // PlayButton
        GameObject playButtonGO;
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"PlayButton");
            playButtonGO=go;
            // Always apply position — 180px above bottom, clearing BottomTabs (140px) with 40px gap
            SetRect(RT(go),new(.5f,0),new(.5f,0),new(.5f,0),new(0,180),new(700,130));
            if(isNew)
            {
                EnsureImage(go,Hex("#5A1090")); EnsureButton(go);
                var lbl=AddLabel(go,"> JOGAR",56f,Color.white); lbl.fontStyle=FontStyles.Bold;
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

            var (tjGO,tjN)=FindOrCreateUI(tr,"TabJogar");
            if(tjN){ EnsureImage(tjGO,Hex("#5A1090")); EnsureButton(tjGO); AddLabel(tjGO,"> JOGAR",24f,Color.white); log.AppendLine("  BottomTabs/TabJogar"); total++; }

            var (pGO,pN)=FindOrCreateUI(tr,"TabPasse");   tabPasseGO=pGO;
            if(pN){ EnsureImage(pGO,Hex("#0D0D1F")); EnsureButton(pGO); AddLabel(pGO,"PASSE",24f,Color.white);   log.AppendLine("  BottomTabs/TabPasse");   total++; }

            { var (c,n)=FindOrCreateUI(tr,"TabConfigs"); if(n){ EnsureImage(c,Hex("#0D0D1F")); EnsureButton(c); AddLabel(c,"CONFIG",24f,Color.white); log.AppendLine("  BottomTabs/TabConfigs"); total++; } }

            TryWire(mmmSO,"botaoLoja",    tabLojaGO.GetComponent<Button>(),    log);
            TryWire(mmmSO,"botaoMissoes", tabMissoesGO.GetComponent<Button>(), log);
            TryWire(mmmSO,"botaoPasse",   tabPasseGO.GetComponent<Button>(),   log);
            TryWire(mmmSO,"botaoTabJogar",tjGO.GetComponent<Button>(),         log);
        }

        // Panels (full-screen, inactive)
        var panels = new (string name, string field, string color)[]
        {
            ("PainelLoja",          "painelLoja",          "#0D0D1F"),
            ("PainelPasse",         "painelPasse",          "#0D0D1F"),
            ("PainelMissoes",       "painelMissoes",        "#0D0D1F"),
            ("PainelRanking",       "painelRanking",        "#0D0D1F"),
            ("PainelConfiguracoes", "painelConfiguracoes",  "#0D0D1F"),
            ("PainelOfertas",       "painelOfertas",        "#1A0A00"),
            ("PainelBencaos",       "painelBencaos",        "#000A1A"),
            ("PainelBaus",          "painelBaus",           "#0A001A"),
        };
        foreach (var (name,field,color) in panels)
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,name);
            if(isNew){ StretchFull(RT(go)); EnsureImage(go,Hex(color)); go.SetActive(false); log.AppendLine($"  {name}"); total++; }
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

        // Destroy legacy containers — children have been migrated or are no longer needed
        total += DestroyLegacyGO(canvasTr, "PlayerInfoPanel", log);
        total += DestroyLegacyGO(canvasTr, "MainButtons",     log);

        mmmSO.ApplyModifiedProperties();

        // SessionRestoreCanvas — sortingOrder 50, acima de tudo no MainMenu
        {
            var srcGO = GameObject.Find("SessionRestoreCanvas");
            if (srcGO == null)
            {
                srcGO = new GameObject("SessionRestoreCanvas");
                Undo.RegisterCreatedObjectUndo(srcGO, "Layout MainMenu");
                var c = srcGO.AddComponent<Canvas>();
                c.renderMode   = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 50;
                var s = srcGO.AddComponent<CanvasScaler>();
                s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1080f, 1920f);
                s.matchWidthOrHeight  = 0.5f;
                log.AppendLine("  SessionRestoreCanvas criado"); total++;
            }
            if (srcGO.GetComponent<GraphicRaycaster>() == null) { srcGO.AddComponent<GraphicRaycaster>(); log.AppendLine("  SessionRestoreCanvas: GraphicRaycaster adicionado"); total++; }
            var srcTr = srcGO.transform;

            var srui   = srcGO.GetComponent<SessionRestoreUI>() ?? srcGO.AddComponent<SessionRestoreUI>();
            var sruiSO = new SerializedObject(srui);

            var (panelGO, panelNew) = FindOrCreateUI(srcTr, "SessionRestorePanel");
            if (panelNew)
            {
                SetRect(RT(panelGO), new(.5f,.5f), new(.5f,.5f), new(.5f,.5f), Vector2.zero, new(800f,500f));
                EnsureImage(panelGO, Hex("#0D0020"));
                panelGO.SetActive(false);
                log.AppendLine("  SessionRestorePanel"); total++;
            }
            var spTr = panelGO.transform;

            { var (go,n)=FindOrCreateUI(spTr,"TituloSessao");
              if(n){ SetRect(RT(go),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,180f),new(740f,70f));
                     var t=EnsureTMP(go,"SESSAO ANTERIOR",48f,Hex("#C8A0FF")); t.fontStyle=FontStyles.Bold;
                     log.AppendLine("  SessionRestorePanel/TituloSessao"); total++; } }

            var (detGO,detN)=FindOrCreateUI(spTr,"TextoDetalhes");
            if(detN){ SetRect(RT(detGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,80f),new(720f,60f)); EnsureTMP(detGO,"Wave 1  0 kills  00:00",32f,Color.white); log.AppendLine("  SessionRestorePanel/TextoDetalhes"); total++; }

            var (btnCGO,btnCN)=FindOrCreateUI(spTr,"BotaoContinuar");
            if(btnCN){ SetRect(RT(btnCGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,-30f),new(500f,90f)); EnsureImage(btnCGO,Hex("#5A1090")); EnsureButton(btnCGO); AddLabel(btnCGO,"CONTINUAR",36f,Color.white); log.AppendLine("  SessionRestorePanel/BotaoContinuar"); total++; }

            var (btnNGO,btnNN)=FindOrCreateUI(spTr,"BotaoNovaRun");
            if(btnNN){ SetRect(RT(btnNGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,-150f),new(500f,90f)); EnsureImage(btnNGO,Hex("#2A2A4A")); EnsureButton(btnNGO); AddLabel(btnNGO,"NOVA RUN",36f,Color.white); log.AppendLine("  SessionRestorePanel/BotaoNovaRun"); total++; }

            TryWire(sruiSO,"panel",         panelGO,                                    log);
            TryWire(sruiSO,"textoDetalhes", detGO.GetComponent<TextMeshProUGUI>(),      log);
            TryWire(sruiSO,"botaoContinuar",btnCGO.GetComponent<Button>(),              log);
            TryWire(sruiSO,"botaoNovaRun",  btnNGO.GetComponent<Button>(),              log);
            sruiSO.ApplyModifiedProperties();
        }

        return total;
    }

    // ── GameScene HUD Layout ────────────────────────────────────────────────────

    static int RunLayoutGameScene(StringBuilder log)
    {
        int total = 0;

        // EventSystem — obrigatório para clicks de UI funcionarem
        {
            var scene = EditorSceneManager.GetActiveScene();
            bool hasES = false;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<EventSystem>(true) != null) { hasES = true; break; }
            if (!hasES)
            {
                var esGO = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(esGO, "Layout GameScene");
                esGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esGO.AddComponent<StandaloneInputModule>();
#endif
                log.AppendLine("  EventSystem criado"); total++;
            }
        }

        // HUD Canvas
        var hudGO = GameObject.Find("HUD Canvas");
        if (hudGO != null)
        {
            Undo.DestroyObjectImmediate(hudGO);
            log.AppendLine("  HUD Canvas antigo destruído");
        }
        hudGO = new GameObject("HUD Canvas");
        Undo.RegisterCreatedObjectUndo(hudGO, "Layout GameScene");
        {
            var c = hudGO.AddComponent<Canvas>();
            c.renderMode   = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;
            var s = hudGO.AddComponent<CanvasScaler>();
            s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.matchWidthOrHeight  = 0.5f;
        }
        hudGO.AddComponent<GraphicRaycaster>();
        log.AppendLine("  HUD Canvas criado"); total++;
        var hudTr = hudGO.transform;

        // Limpar filhos legados de versões anteriores do Layout Setup
        string[] legadoNomes = { "SecondRow", "BottomHud", "BannerWave", "TopHudBar" };
        foreach (var nome in legadoNomes)
        {
            var legado = hudTr.Find(nome);
            if (legado != null)
            {
                Undo.DestroyObjectImmediate(legado.gameObject);
                log.AppendLine($"  Removido legado: {nome}");
            }
        }

        var hud   = Object.FindFirstObjectByType<HUDComplete>(FindObjectsInactive.Include);
        if (hud == null)
        {
            hud = hudGO.AddComponent<HUDComplete>();
            log.AppendLine("  HUDComplete adicionado"); total++;
        }
        var hudSO = new SerializedObject(hud);

        // Fundo visual do HUD
        if (hudGO.transform.Find("HUDBackground") == null)
        {
            var hudBg    = new GameObject("HUDBackground");
            Undo.RegisterCreatedObjectUndo(hudBg, "Layout GameScene");
            hudBg.transform.SetParent(hudGO.transform, false);
            hudBg.transform.SetAsFirstSibling();
            var hudBgRT  = hudBg.AddComponent<RectTransform>();
            hudBgRT.anchorMin       = new Vector2(0f, 1f);
            hudBgRT.anchorMax       = new Vector2(1f, 1f);
            hudBgRT.pivot           = new Vector2(0.5f, 1f);
            hudBgRT.sizeDelta       = new Vector2(0f, 110f);
            hudBgRT.anchoredPosition = Vector2.zero;
            var hudBgImg = hudBg.AddComponent<Image>();
            var containerSprite = LoadUI("hud_container.png");
            if (containerSprite != null)
            {
                hudBgImg.sprite = containerSprite;
                hudBgImg.type   = Image.Type.Sliced;
                hudBgImg.color  = new Color(1f, 1f, 1f, 0.9f);
            }
            else hudBgImg.color = new Color(0f, 0f, 0f, 0.5f);
            log.AppendLine("  HUDBackground criado"); total++;
        }

        // TopBar (âncora topo, h=110)
        {
            var (go,isNew)=FindOrCreateUI(hudTr,"TopBar");
            if(isNew){ AnchorTopBar(RT(go),110f); EnsureImage(go,Hex("#00000060")); log.AppendLine("  TopBar"); total++; }
            var tr=go.transform;

            var (slGO,slN)=FindOrCreateUI(tr,"HealthSlider");
            if(slN){ SetRect(RT(slGO),new(0,.5f),new(0,.5f),new(0,.5f),new(20,0),new(340,50)); BuildSlider(slGO); log.AppendLine("  HealthSlider"); total++; }

            var (tvGO,tvN)=FindOrCreateUI(tr,"VidaText");
            if(tvN){ SetRect(RT(tvGO),new(0,.5f),new(0,.5f),new(0,.5f),new(310,0),new(110,38)); EnsureTMP(tvGO,"100/100",20f,Color.white); log.AppendLine("  VidaText"); total++; }

            var (tiGO,tiN)=FindOrCreateUI(tr,"TimerText");
            if(tiN){ SetRect(RT(tiGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(150,50)); var t=EnsureTMP(tiGO,"00:00",36f,Color.white); t.fontStyle=FontStyles.Bold; t.alignment=TextAlignmentOptions.Center; log.AppendLine("  TimerText"); total++; }

            TryWire(hudSO,"barraVida",  slGO.GetComponent<Slider>(),           log);
            TryWire(hudSO,"textoVida",  tvGO.GetComponent<TextMeshProUGUI>(),  log);
            TryWire(hudSO,"textoTimer", tiGO.GetComponent<TextMeshProUGUI>(),  log);
        }

        // XPBar (faixa fina logo abaixo do TopBar)
        {
            var (go,isNew)=FindOrCreateUI(hudTr,"XPBar");
            if(isNew){ SetRect(RT(go),new(0,1),new(1,1),new(.5f,1),new(0,-110),new(0,14)); log.AppendLine("  XPBar"); total++; }
            var tr=go.transform;

            var (xpGO,xpN)=FindOrCreateUI(tr,"XPSlider");
            if(xpN){ SetRect(RT(xpGO),new(0,.5f),new(1,.5f),new(.5f,.5f),new(-45,0),new(-90,14)); BuildSliderXP(xpGO); log.AppendLine("  XPSlider"); total++; }

            var (nvGO,nvN)=FindOrCreateUI(tr,"NivelText");
            if(nvN){ SetRect(RT(nvGO),new(1,.5f),new(1,.5f),new(1,.5f),new(-5,0),new(80,14)); EnsureTMP(nvGO,"Nv.1",15f,Hex("#AAAAFF")); log.AppendLine("  NivelText"); total++; }

            var (pbGO,pbN)=FindOrCreateUI(tr,"PauseButton");
            if(pbN){ SetRect(RT(pbGO),new(1,.5f),new(1,.5f),new(1,.5f),new(-5,0),new(50,14)); EnsureImage(pbGO,Hex("#00000060")); EnsureButton(pbGO); AddLabel(pbGO,"II",16f,Color.white); log.AppendLine("  PauseButton"); total++; }

            TryWire(hudSO,"barraXP",    xpGO.GetComponent<Slider>(),           log);
            TryWire(hudSO,"textoNivel", nvGO.GetComponent<TextMeshProUGUI>(),  log);
            TryWire(hudSO,"botaoPause", pbGO.GetComponent<Button>(),           log);
        }

        // PoderEspecial (bottom-right, 100×100)
        {
            var (go,isNew)=FindOrCreateUI(hudTr,"PoderEspecial");
            if(isNew)
            {
                var rt=RT(go);
                rt.anchorMin=new Vector2(1,0); rt.anchorMax=new Vector2(1,0); rt.pivot=new Vector2(1,0);
                rt.sizeDelta=new Vector2(100,100); rt.anchoredPosition=new Vector2(-20,20);

                var bgImg=go.GetComponent<Image>()??go.AddComponent<Image>();
                var slotSprite=LoadUI("slot_base.png");
                if(slotSprite!=null){ bgImg.sprite=slotSprite; bgImg.type=Image.Type.Sliced; }
                else bgImg.color=Hex("#00000080");

                var (icoGO,_)=FindOrCreateUI(go.transform,"Icone");
                SetRect(RT(icoGO),Vector2.zero,Vector2.one,new Vector2(.5f,.5f),Vector2.zero,new Vector2(-10,-10));
                var icoImg=icoGO.GetComponent<Image>()??icoGO.AddComponent<Image>();
                var swordSprite=LoadUI("action_button_sword.png");
                if(swordSprite!=null) icoImg.sprite=swordSprite;
                else icoImg.color=Hex("#FFFFFF80");

                EnsureButton(go);

                var (cdGO,_)=FindOrCreateUI(go.transform,"Cooldown");
                SetRect(RT(cdGO),Vector2.zero,Vector2.one,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
                var cdTMP=EnsureTMP(cdGO,"",24f,Color.white);
                cdTMP.alignment=TextAlignmentOptions.Center;
                cdTMP.fontStyle=FontStyles.Bold;

                log.AppendLine("  PoderEspecial"); total++;
            }
            TryWire(hudSO,"botaoPoderEspecial",go.GetComponent<Button>(),log);
            var cdT=go.transform.Find("Cooldown");
            if(cdT!=null) TryWire(hudSO,"textoCooldown",cdT.GetComponent<TextMeshProUGUI>(),log);
            var icoT=go.transform.Find("Icone");
            if(icoT!=null) TryWire(hudSO,"imagemPoderEspecial",icoT.GetComponent<Image>(),log);
        }

        // PauseCanvas (sortingOrder 25 — acima do HUD e Joystick, abaixo do GameOver)
        {
            var pcGO = GameObject.Find("PauseCanvas");
            if (pcGO != null)
            {
                Undo.DestroyObjectImmediate(pcGO);
                log.AppendLine("  PauseCanvas antigo destruído");
            }
            pcGO = new GameObject("PauseCanvas");
            Undo.RegisterCreatedObjectUndo(pcGO, "Layout GameScene");
            {
                var c = pcGO.AddComponent<Canvas>();
                c.renderMode   = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 25;
                var s = pcGO.AddComponent<CanvasScaler>();
                s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1920f, 1080f);
                s.matchWidthOrHeight  = 0.5f;
            }
            pcGO.AddComponent<GraphicRaycaster>();
            log.AppendLine("  PauseCanvas criado"); total++;
            var pcTr = pcGO.transform;

            var (pausePanelGO,ppNew)=FindOrCreateUI(pcTr,"PausePanel");
            if(ppNew)
            {
                SetRect(RT(pausePanelGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(800f,900f));
                EnsureImage(pausePanelGO,Hex("#00000095"));
                pausePanelGO.SetActive(false);
                log.AppendLine("  PausePanel"); total++;
            }
            var ppTr = pausePanelGO.transform;

            { var (go,n)=FindOrCreateUI(ppTr,"TextoPausado");
              if(n){ SetRect(RT(go),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,300),new(600,80));
                     var t=EnsureTMP(go,"|| PAUSADO",72f,Hex("#C8A0FF")); t.fontStyle=FontStyles.Bold;
                     log.AppendLine("  PausePanel/TextoPausado"); total++; } }

            var (retomarGO,retomarN)=FindOrCreateUI(ppTr,"BotaoRetomar");
            if(retomarN){ SetRect(RT(retomarGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,50),new(500,100)); EnsureImage(retomarGO,Hex("#5A1090")); EnsureButton(retomarGO); AddLabel(retomarGO,"> RETOMAR",36f,Color.white); log.AppendLine("  PausePanel/BotaoRetomar"); total++; }

            var (menuPauseGO,menuPauseN)=FindOrCreateUI(ppTr,"BotaoMenuPrincipalPause");
            if(menuPauseN){ SetRect(RT(menuPauseGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,-80),new(500,100)); EnsureImage(menuPauseGO,Hex("#2A2A4A")); EnsureButton(menuPauseGO); AddLabel(menuPauseGO,"MENU PRINCIPAL",36f,Color.white); log.AppendLine("  PausePanel/BotaoMenuPrincipalPause"); total++; }

            TryWire(hudSO,"pausePanel",             pausePanelGO,                            log);
            TryWire(hudSO,"botaoRetomar",           retomarGO.GetComponent<Button>(),        log);
            TryWire(hudSO,"botaoMenuPrincipalPause",menuPauseGO.GetComponent<Button>(),      log);
        }

        hudSO.ApplyModifiedProperties();

        // Joystick Canvas (sortingOrder 20, acima do HUD)
        {
            var jcGO = GameObject.Find("JoystickCanvas");
            if (jcGO != null)
            {
                Undo.DestroyObjectImmediate(jcGO);
                log.AppendLine("  JoystickCanvas antigo destruído");
            }
            jcGO = new GameObject("JoystickCanvas");
            Undo.RegisterCreatedObjectUndo(jcGO, "Layout GameScene");
            {
                var c = jcGO.AddComponent<Canvas>();
                c.renderMode   = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 20;
                var s = jcGO.AddComponent<CanvasScaler>();
                s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1920f, 1080f);
                s.matchWidthOrHeight  = 0.5f;
            }
            jcGO.AddComponent<GraphicRaycaster>();
            log.AppendLine("  JoystickCanvas criado"); total++;
            var jcTr = jcGO.transform;

            var (bgGO, bgNew) = FindOrCreateUI(jcTr, "JoystickBackground");
            if (bgNew)
            {
                // Canto inferior esquerdo, pivot (0,0), offset (80,80)
                SetRect(RT(bgGO), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(80f, 80f), new Vector2(200f, 200f));
                EnsureImage(bgGO, Hex("#80000000"));
                log.AppendLine("  JoystickBackground"); total++;
            }

            var (knobGO, knobNew) = FindOrCreateUI(bgGO.transform, "JoystickKnob");
            if (knobNew)
            {
                SetRect(RT(knobGO), new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(80f, 80f));
                EnsureImage(knobGO, Hex("#AAFFFFFF"));
                log.AppendLine("  JoystickKnob"); total++;
            }

            var joystick   = bgGO.GetComponent<MobileJoystick>() ?? bgGO.AddComponent<MobileJoystick>();
            var joystickSO = new SerializedObject(joystick);
            TryWire(joystickSO, "knobTransform", knobGO.GetComponent<RectTransform>(), log);
            joystickSO.ApplyModifiedProperties();
        }

        // Game Over Canvas (sortingOrder 30, acima de tudo)
        // IMPORTANTE: GameOverScreen vai no Canvas (sempre ativo), não no painel (inativo).
        {
            var gcGO = GameObject.Find("GameOverCanvas");
            if (gcGO == null)
            {
                gcGO = new GameObject("GameOverCanvas");
                Undo.RegisterCreatedObjectUndo(gcGO, "Layout GameScene");
                var c = gcGO.AddComponent<Canvas>();
                c.renderMode   = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 30;
                var s = gcGO.AddComponent<CanvasScaler>();
                s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1080f, 1920f);
                s.matchWidthOrHeight  = 0.5f;
                log.AppendLine("  GameOverCanvas criado"); total++;
            }
            if (gcGO.GetComponent<GraphicRaycaster>() == null) { gcGO.AddComponent<GraphicRaycaster>(); log.AppendLine("  GameOverCanvas: GraphicRaycaster adicionado"); total++; }
            var gcTr = gcGO.transform;

            // Painel central (inativo por padrão)
            var (panelGO, panelNew) = FindOrCreateUI(gcTr, "GameOverPanel");
            if (panelNew)
            {
                SetRect(RT(panelGO), new(.5f,.5f), new(.5f,.5f), new(.5f,.5f), Vector2.zero, new(800f,1000f));
                EnsureImage(panelGO, Hex("#0D0020"));
                panelGO.SetActive(false);
                log.AppendLine("  GameOverPanel"); total++;
            }
            var pTr = panelGO.transform;

            // Título
            { var (go,n)=FindOrCreateUI(pTr,"TituloGameOver");
              if(n){ SetRect(RT(go),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,390f),new(740f,80f));
                     var t=EnsureTMP(go,"A ESCURIDÃO PREVALECEU",48f,Hex("#C8A0FF")); t.fontStyle=FontStyles.Bold;
                     log.AppendLine("  TituloGameOver"); total++; } }

            // 6 stats empilhados verticalmente
            var (waveGO,   wN) = FindOrCreateUI(pTr, "textoWaveAtingida");
            var (killsGO,  kN) = FindOrCreateUI(pTr, "textoKills");
            var (tempoGO,  tN) = FindOrCreateUI(pTr, "textoTempo");
            var (causaGO,  cN) = FindOrCreateUI(pTr, "textoCausa");
            var (scoreGO,  sN) = FindOrCreateUI(pTr, "textoScore");
            var (diamanGO, dN) = FindOrCreateUI(pTr, "textoDiamantes");

            if(wN){ SetRect(RT(waveGO),  new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,260f),new(700f,50f)); EnsureTMP(waveGO,  "Wave —",        32f,Color.white); log.AppendLine("  textoWaveAtingida"); total++; }
            if(kN){ SetRect(RT(killsGO), new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,195f),new(700f,50f)); EnsureTMP(killsGO, "Kills: —",       32f,Color.white); log.AppendLine("  textoKills");        total++; }
            if(tN){ SetRect(RT(tempoGO), new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,130f),new(700f,50f)); EnsureTMP(tempoGO, "Tempo: 00:00",   32f,Color.white); log.AppendLine("  textoTempo");        total++; }
            if(cN){ SetRect(RT(causaGO), new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f, 65f),new(700f,50f)); EnsureTMP(causaGO, "Causa: —",       32f,Color.white); log.AppendLine("  textoCausa");        total++; }
            if(sN){ SetRect(RT(scoreGO), new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,  0f),new(700f,50f)); EnsureTMP(scoreGO, "Score: —",        32f,Color.white); log.AppendLine("  textoScore");        total++; }
            if(dN){ SetRect(RT(diamanGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0f,-65f),new(700f,50f)); EnsureTMP(diamanGO,"+0 diamantes",    32f,Color.white); log.AppendLine("  textoDiamantes");    total++; }

            // Botões
            var (ressGO, ressNew) = FindOrCreateUI(pTr, "BotaoRessuscitar");
            if (ressNew)
            {
                SetRect(RT(ressGO), new(.5f,.5f), new(.5f,.5f), new(.5f,.5f), new(0f,-130f), new(500f,90f));
                EnsureImage(ressGO, Hex("#8A6000")); EnsureButton(ressGO);
                AddLabel(ressGO, "> RESSUSCITAR", 36f, Color.white);

                // Hint de anúncio abaixo do botão (filho, fora do rect do botão)
                var (subGO, _) = FindOrCreateUI(ressGO.transform, "SubtituloRessuscitar");
                SetRect(RT(subGO), new(.5f,.5f), new(.5f,.5f), new(.5f,.5f), new(0f,-35f), new(500f,30f));
                EnsureTMP(subGO, "( assista um anúncio )", 20f, Hex("#888888"));
                log.AppendLine("  BotaoRessuscitar + subtítulo"); total++;
            }

            var (restartGO, restartNew) = FindOrCreateUI(pTr, "BotaoJogarNovamente");
            if (restartNew)
            {
                SetRect(RT(restartGO), new(.5f,.5f), new(.5f,.5f), new(.5f,.5f), new(0f,-280f), new(600f,100f));
                EnsureImage(restartGO, Hex("#5A1090")); EnsureButton(restartGO);
                AddLabel(restartGO, "> JOGAR NOVAMENTE", 36f, Color.white);
                log.AppendLine("  BotaoJogarNovamente"); total++;
            }

            var (menuGO, menuNew) = FindOrCreateUI(pTr, "BotaoMenuPrincipal");
            if (menuNew)
            {
                SetRect(RT(menuGO), new(.5f,.5f), new(.5f,.5f), new(.5f,.5f), new(0f,-410f), new(600f,100f));
                EnsureImage(menuGO, Hex("#2A2A4A")); EnsureButton(menuGO);
                AddLabel(menuGO, "MENU PRINCIPAL", 36f, Color.white);
                log.AppendLine("  BotaoMenuPrincipal"); total++;
            }

            // GameOverScreen no Canvas (sempre ativo) para que OnEnable dispare corretamente
            var gos   = gcGO.GetComponent<GameOverScreen>() ?? gcGO.AddComponent<GameOverScreen>();
            var gosSO = new SerializedObject(gos);
            TryWire(gosSO, "panel",              panelGO,                                    log);
            TryWire(gosSO, "waveText",           waveGO.GetComponent<TextMeshProUGUI>(),      log);
            TryWire(gosSO, "killsText",          killsGO.GetComponent<TextMeshProUGUI>(),     log);
            TryWire(gosSO, "timeText",           tempoGO.GetComponent<TextMeshProUGUI>(),     log);
            TryWire(gosSO, "causeText",          causaGO.GetComponent<TextMeshProUGUI>(),     log);
            TryWire(gosSO, "scoreText",          scoreGO.GetComponent<TextMeshProUGUI>(),     log);
            TryWire(gosSO, "diamondsText",       diamanGO.GetComponent<TextMeshProUGUI>(),    log);
            TryWire(gosSO, "ressuscitarButton",  ressGO.GetComponent<Button>(),               log);
            TryWire(gosSO, "restartButton",      restartGO.GetComponent<Button>(),            log);
            TryWire(gosSO, "menuButton",         menuGO.GetComponent<Button>(),               log);
            var panelProp = gosSO.FindProperty("panel");
            if (panelProp != null && panelProp.objectReferenceValue == null)
                Debug.LogError("[Layout GameScene] GameOverScreen.panel ficou null após wiring — verifique GameOverPanel na hierarquia.");
            gosSO.ApplyModifiedProperties();
        }

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
        var bg = new GameObject("Background"); Undo.RegisterCreatedObjectUndo(bg, "Solengard Layout");
        bg.transform.SetParent(go.transform, false);
        var bgImg      = bg.AddComponent<Image>();
        var frameSprite = LoadUI("bar_frame_1.png");
        if (frameSprite != null) { bgImg.sprite = frameSprite; bgImg.type = Image.Type.Sliced; bgImg.color = Color.white; }
        else bgImg.color = new Color(.15f, .0f, .0f, 1f);
        StretchFull(bg.GetComponent<RectTransform>());

        var fa = new GameObject("Fill Area"); Undo.RegisterCreatedObjectUndo(fa, "Solengard Layout");
        fa.transform.SetParent(go.transform, false);
        StretchFull(fa.AddComponent<RectTransform>());

        var fill = new GameObject("Fill"); Undo.RegisterCreatedObjectUndo(fill, "Solengard Layout");
        fill.transform.SetParent(fa.transform, false);
        var fillImg    = fill.AddComponent<Image>();
        var fillSprite = LoadUI("bar_fill_1.png");
        if (fillSprite != null) { fillImg.sprite = fillSprite; fillImg.type = Image.Type.Filled; fillImg.color = new Color(0.2f, 0.85f, 0.3f); }
        else fillImg.color = new Color(.8f, .1f, .1f, 1f);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRT; slider.value = 1f; slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;
    }

    static void BuildSliderXP(GameObject go)
    {
        var bg = new GameObject("Background"); Undo.RegisterCreatedObjectUndo(bg, "Solengard Layout");
        bg.transform.SetParent(go.transform, false);
        var bgImg       = bg.AddComponent<Image>();
        var frameSprite = LoadUI("bar_frame_2.png");
        if (frameSprite != null) { bgImg.sprite = frameSprite; bgImg.type = Image.Type.Sliced; bgImg.color = Color.white; }
        else bgImg.color = new Color(.05f, .05f, .2f, 1f);
        StretchFull(bg.GetComponent<RectTransform>());

        var fa = new GameObject("Fill Area"); Undo.RegisterCreatedObjectUndo(fa, "Solengard Layout");
        fa.transform.SetParent(go.transform, false);
        StretchFull(fa.AddComponent<RectTransform>());

        var fill = new GameObject("Fill"); Undo.RegisterCreatedObjectUndo(fill, "Solengard Layout");
        fill.transform.SetParent(fa.transform, false);
        var fillImg    = fill.AddComponent<Image>();
        var fillSprite = LoadUI("bar_fill_2.png");
        if (fillSprite != null) { fillImg.sprite = fillSprite; fillImg.type = Image.Type.Filled; fillImg.color = new Color(0.3f, 0.5f, 1.0f); }
        else fillImg.color = new Color(.2f, .4f, .9f, 1f);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRT; slider.value = 0f; slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
    }

    static void TryWire(SerializedObject so, string prop, Object val, StringBuilder log)
    {
        if (val==null) return;
        var p=so.FindProperty(prop);
        if (p==null||p.objectReferenceValue!=null) return;
        p.objectReferenceValue=val;
        log.AppendLine($"  Wire {so.targetObject.GetType().Name}.{prop}");
    }

    static int DestroyLegacyGO(Transform parent, string name, StringBuilder log)
    {
        var t = parent.Find(name);
        if (t == null) return 0;
        Undo.DestroyObjectImmediate(t.gameObject);
        log.AppendLine($"  ✕ Removido legado: {name}");
        return 1;
    }

    static bool ValidateScene(string expected)
    {
        if (EditorSceneManager.GetActiveScene().name==expected) return true;
        EditorUtility.DisplayDialog("Solengard Layout",
            $"Abra a cena '{expected}' antes de executar este layout.\n\nCena atual: '{EditorSceneManager.GetActiveScene().name}'","OK");
        return false;
    }
}
