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

        string[] spritesMenu = {
            "hud_container.png", "menu_button.png", "hud_separator.png",
            "action_button_base.png", "complete_container.png"
        };
        foreach(var s in spritesMenu) LoadUI(s); // força reimport via TextureImporter

        // Canvas — destroy-and-recreate para forçar reaplicação de sprites
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null)
        {
            Undo.DestroyObjectImmediate(canvasGO);
            log.AppendLine("  Canvas antigo destruído");
        }
        canvasGO = new GameObject("Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Layout MainMenu");
        {
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
            // Fundo dark fantasy — força reimport como Sprite se necessário
            const string BG_PATH = "Assets/Art/UI/Backgournds/menu_background.png";
            var bgImporter = AssetImporter.GetAtPath(BG_PATH) as TextureImporter;
            if (bgImporter != null && bgImporter.textureType != TextureImporterType.Sprite)
            {
                bgImporter.textureType = TextureImporterType.Sprite;
                bgImporter.SaveAndReimport();
            }
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BG_PATH);
            if (bgSprite != null)
            {
                var bgImg = go.GetComponent<Image>() ?? go.AddComponent<Image>();
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Simple;
                bgImg.color = Color.white;
                bgImg.preserveAspect = false;
            }
            else Debug.LogWarning("[Layout] menu_background.png não encontrado em Assets/Art/UI/Backgrounds/");
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

            var topBarImg = topBarGO.GetComponent<Image>() ?? topBarGO.AddComponent<Image>();
            var topBarSprite = LoadUI("hud_container.png");
            if(topBarSprite != null){ topBarImg.sprite = topBarSprite; topBarImg.type = Image.Type.Simple; topBarImg.color = Color.white; }

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
            botaoOfertasGO.SetActive(false);
            TryWire(mmmSO,"botaoBencaos",botaoBencaosGO.GetComponent<Button>(),log);
            botaoBencaosGO.SetActive(false);
            TryWire(mmmSO,"botaoBaus",   botaoBausGO.GetComponent<Button>(),   log);
            botaoBausGO.SetActive(false);
        }

        // RightPanel — remove if empty (BotaoOferta is now at canvas level)
        { var rpTr=canvasTr.Find("RightPanel");
          if(rpTr!=null && rpTr.childCount==0) total+=DestroyLegacyGO(canvasTr,"RightPanel",log); }

        // BotaoOferta — flush to right edge, symmetric to LeftPanel
        { var (bo,boN)=FindReparentOrCreateUI(canvasTr,canvasTr,"BotaoOferta");
          SetRect(RT(bo),new(1,.5f),new(1,.5f),new(1,.5f),new(-5,0),new(110,110));
          if(boN){ EnsureImage(bo,Hex("#3A1A0A")); EnsureButton(bo); var lbl=AddLabel(bo,"OFERTA\nQUENTE!",22f,Hex("#FF6600")); lbl.textWrappingMode=TMPro.TextWrappingModes.NoWrap; log.AppendLine("  BotaoOferta (borda direita)"); total++; }
          var ofertaImg = bo.GetComponent<Image>() ?? bo.AddComponent<Image>();
          var ofertaSprite = LoadUI("action_button_base.png");
          if(ofertaSprite != null){ ofertaImg.sprite = ofertaSprite; ofertaImg.type = Image.Type.Simple; ofertaImg.color = new Color(1f,0.4f,0f,1f); }
          bo.SetActive(false); }

        // PlayButton
        GameObject playButtonGO;
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"PlayButton");
            playButtonGO=go;
            // Always apply position — 180px above bottom, clearing BottomTabs (140px) with 40px gap
            SetRect(RT(go),new(.5f,0),new(.5f,0),new(.5f,0),new(0,180),new(500,100));
            if(isNew)
            {
                EnsureButton(go);
                var lbl=AddLabel(go,"> JOGAR",56f,Color.white); lbl.fontStyle=FontStyles.Bold;
                var sh=lbl.gameObject.AddComponent<Shadow>(); sh.effectDistance=new(0,-4); sh.effectColor=new Color(0,0,0,.5f);
                log.AppendLine("  PlayButton"); total++;
            }
            TryWire(mmmSO,"botaoJogar",playButtonGO.GetComponent<Button>(),log);
            ApplyGUISprite(playButtonGO, "Button/Button_Rectangle_01_Convex_White_Bg.png", Hex("#8B2535"));
        }

        // BottomTabs
        GameObject tabLojaGO, tabMissoesGO, tabPasseGO;
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"BottomTabs");
            if(isNew){ AnchorBottomBar(RT(go),140f); EnsureImage(go,Hex("#0D0D1F")); log.AppendLine("  BottomTabs"); total++; }
            var tabsBgImg = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            var tabsSprite = LoadUI("hud_separator.png");
            if(tabsSprite != null){ tabsBgImg.sprite = tabsSprite; tabsBgImg.type = Image.Type.Simple; tabsBgImg.color = new Color(0.05f,0.05f,0.15f,1f); }
            var hlg=go.GetComponent<HorizontalLayoutGroup>()??go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment=TextAnchor.MiddleCenter; hlg.childControlWidth=true; hlg.childControlHeight=true;
            hlg.childForceExpandWidth=true; hlg.childForceExpandHeight=true; hlg.spacing=0;
            var tr=go.transform;

            var tabMenuSprite = LoadUI("menu_button.png");
            Color tabNormal = new Color(0.08f,0.05f,0.15f,1f);
            Color tabActive = new Color(0.35f,0.05f,0.6f,1f);

            var (lGO,lN)=FindOrCreateUI(tr,"TabLoja");    tabLojaGO=lGO;
            if(lN){ EnsureImage(lGO,Hex("#0D0D1F")); EnsureButton(lGO); AddLabel(lGO,"LOJA",24f,Color.white);    log.AppendLine("  BottomTabs/TabLoja");    total++; }
            { var img=lGO.GetComponent<Image>()??lGO.AddComponent<Image>(); if(tabMenuSprite!=null){ img.sprite=tabMenuSprite; img.type=Image.Type.Simple; img.color=tabNormal; } }

            var (mGO,mN)=FindOrCreateUI(tr,"TabMissoes"); tabMissoesGO=mGO;
            if(mN){ EnsureImage(mGO,Hex("#0D0D1F")); EnsureButton(mGO); AddLabel(mGO,"MISSÕES",24f,Color.white); log.AppendLine("  BottomTabs/TabMissoes"); total++; }
            { var img=mGO.GetComponent<Image>()??mGO.AddComponent<Image>(); if(tabMenuSprite!=null){ img.sprite=tabMenuSprite; img.type=Image.Type.Simple; img.color=tabNormal; } }

            var (tjGO,tjN)=FindOrCreateUI(tr,"TabJogar");
            if(tjN){ EnsureImage(tjGO,Hex("#5A1090")); EnsureButton(tjGO); AddLabel(tjGO,"> JOGAR",24f,Color.white); log.AppendLine("  BottomTabs/TabJogar"); total++; }
            { var img=tjGO.GetComponent<Image>()??tjGO.AddComponent<Image>(); if(tabMenuSprite!=null){ img.sprite=tabMenuSprite; img.type=Image.Type.Simple; img.color=tabActive; } }
            tjGO.SetActive(false);

            var (pGO,pN)=FindOrCreateUI(tr,"TabPasse");   tabPasseGO=pGO;
            if(pN){ EnsureImage(pGO,Hex("#0D0D1F")); EnsureButton(pGO); AddLabel(pGO,"UPGRADES",24f,Color.white); log.AppendLine("  BottomTabs/TabPasse");   total++; }
            { var img=pGO.GetComponent<Image>()??pGO.AddComponent<Image>(); if(tabMenuSprite!=null){ img.sprite=tabMenuSprite; img.type=Image.Type.Simple; img.color=tabNormal; } }

            { var (c,n)=FindOrCreateUI(tr,"TabConfigs"); if(n){ EnsureImage(c,Hex("#0D0D1F")); EnsureButton(c); AddLabel(c,"CONFIG",24f,Color.white); log.AppendLine("  BottomTabs/TabConfigs"); total++; }
              var img=c.GetComponent<Image>()??c.AddComponent<Image>(); if(tabMenuSprite!=null){ img.sprite=tabMenuSprite; img.type=Image.Type.Simple; img.color=tabNormal; } }

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

        // ── PainelLoja ────────────────────────────────────────────────────────────
        {
            var (lojaGO,_) = FindOrCreateUI(canvasTr,"PainelLoja");
            var lojaTr     = lojaGO.transform;
            if (lojaGO.GetComponent<LojaController>() == null) lojaGO.AddComponent<LojaController>();
            var lojaSO = new UnityEditor.SerializedObject(lojaGO.GetComponent<LojaController>());

            // Header
            { var (h,hn)=FindOrCreateUI(lojaTr,"HeaderLoja");
              if(hn){ SetRect(RT(h),new(0,1),new(1,1),new(.5f,1),new(0,-50),new(0,100));
              EnsureImage(h,Hex("#1A0A2E")); total++; }
              { var (t,_)=FindOrCreateUI(h.transform,"TituloLoja");
                SetRect(RT(t),new(0,0),new(.65f,1),new(0,.5f),new(20,0),Vector2.zero);
                var tmp=EnsureTMP(t,"LOJA",42f,Color.white); tmp.fontStyle=FontStyles.Bold; }
              var (sGO,_)=FindOrCreateUI(h.transform,"TextoSaldo");
              SetRect(RT(sGO),new(.65f,0),new(1,1),new(1,.5f),new(-16,0),Vector2.zero);
              var sTMP=EnsureTMP(sGO,"💎 0",32f,Hex("#FFD700")); sTMP.alignment=TextAlignmentOptions.Right;
              TryWire(lojaSO,"textoSaldo",sGO.GetComponent<TextMeshProUGUI>(),log); }

            // Abas
            { var (ab,abn)=FindOrCreateUI(lojaTr,"AbasLoja");
              if(abn){ SetRect(RT(ab),new(0,1),new(1,1),new(.5f,1),new(0,-150),new(0,55));
              EnsureImage(ab,Hex("#110820")); total++; }
              var abTr=ab.transform;
              string[] abaNomes={"Personagens","Upgrades","Diamantes"};
              string[] abaIds={"BtnPersonagens","BtnUpgrades","BtnDiamantes"};
              string[] abaWires={"btnAbaPersonagens","btnAbaUpgrades","btnAbaDiamantes"};
              Color[] abaCores={Hex("#3A1080"),Hex("#1A1060"),Hex("#0A1060")};
              for(int i=0;i<3;i++){
                var (b,bn)=FindOrCreateUI(abTr,abaIds[i]);
                if(bn){ SetRect(RT(b),new(i/3f,0),new((i+1)/3f,1),new(.5f,.5f),Vector2.zero,Vector2.zero);
                EnsureImage(b,abaCores[i]); EnsureButton(b); AddLabel(b,abaNomes[i],22f,Color.white); total++; }
                TryWire(lojaSO,abaWires[i],ab.transform.Find(abaIds[i])?.GetComponent<UnityEngine.UI.Button>(),log);
              }}

            // Aba Personagens — grid 2 colunas
            var (apGO,apn)=FindOrCreateUI(lojaTr,"AbaPersonagens");
            if(apn){ SetRect(RT(apGO),new(0,0),new(1,1),new(.5f,.5f),new(0,-205),new(0,-205));
            EnsureImage(apGO,Hex("#0D0D1F")); total++; }
            TryWire(lojaSO,"abaPersonagens",apGO,log);
            {
                var classesData=LojaController.GetClasses();
                float cW=240f, cH=180f, padX=20f, padY=20f;
                for(int i=0;i<classesData.Length;i++){
                    var (id,nome,preco)=classesData[i];
                    int col=i%2, row=i/2;
                    float x = col==0 ? -(cW/2+padX/2) : (cW/2+padX/2);
                    float y = 80f - row*(cH+padY);
                    var (card,cn)=FindOrCreateUI(apGO.transform,$"CardClasse_{id}");
                    if(cn){
                        SetRect(RT(card),new(.5f,1),new(.5f,1),new(.5f,1),new(x,y),new(cW,cH));
                        EnsureImage(card,Hex("#1E1040"));
                        var (nm,_)=FindOrCreateUI(card.transform,"Nome");
                        SetRect(RT(nm),new(0,.5f),new(1,1),new(.5f,1),new(0,-8),new(0,-16));
                        var ntmp=EnsureTMP(nm,nome,28f,Color.white); ntmp.alignment=TextAlignmentOptions.Center; ntmp.fontStyle=FontStyles.Bold;
                        var (btn,bn)=FindOrCreateUI(card.transform,"BtnComprar");
                        if(bn){ SetRect(RT(btn),new(.1f,0),new(.9f,0),new(.5f,0),new(0,16),new(0,48));
                        EnsureImage(btn,Hex("#5A1090")); EnsureButton(btn);
                        AddLabel(btn,$"💎 {preco}",22f,Color.white); total++; }
                        var lojaCtrl=lojaGO.GetComponent<LojaController>();
                        var btnComp=btn.GetComponent<UnityEngine.UI.Button>();
                        if(btnComp!=null && lojaCtrl!=null){
                            string cid=id; int cp=preco;
                            btnComp.onClick.RemoveAllListeners();
                            btnComp.onClick.AddListener(()=>lojaCtrl.ComprarClasse(cid,cp));
                        }
                        log.AppendLine($"  Loja/Card_{id}"); total++;
                    }
                }
            }

            // Aba Upgrades — lista com categorias
            var (auGO,aun)=FindOrCreateUI(lojaTr,"AbaUpgrades");
            if(aun){ SetRect(RT(auGO),new(0,0),new(1,1),new(.5f,.5f),new(0,-205),new(0,-205));
            EnsureImage(auGO,Hex("#0D0D1F")); auGO.SetActive(false); total++; }
            TryWire(lojaSO,"abaUpgrades",auGO,log);
            {
                var cats = new (string nome, PermanentUpgradeId[] ids)[] {
                    ("Ofensa",     new[]{PermanentUpgradeId.Poder, PermanentUpgradeId.Recarga}),
                    ("Defesa",     new[]{PermanentUpgradeId.Armadura, PermanentUpgradeId.VidaMaxima, PermanentUpgradeId.Recuperacao}),
                    ("Ataque",     new[]{PermanentUpgradeId.Area, PermanentUpgradeId.Velocidade, PermanentUpgradeId.Duracao, PermanentUpgradeId.Quantidade}),
                    ("Mobilidade", new[]{PermanentUpgradeId.Movimento, PermanentUpgradeId.Magnetismo}),
                    ("Progressao", new[]{PermanentUpgradeId.Sorte, PermanentUpgradeId.Crescimento, PermanentUpgradeId.Riqueza}),
                    ("Especiais",  new[]{PermanentUpgradeId.Maldicao, PermanentUpgradeId.Ressurreicao, PermanentUpgradeId.PoderEspecial}),
                };
                float yPos = -20f;
                foreach(var (catNome, catIds) in cats){
                    var (catLbl,_)=FindOrCreateUI(auGO.transform,$"Cat_{catNome}");
                    SetRect(RT(catLbl),new(0,1),new(1,1),new(.5f,1),new(0,yPos),new(0,36));
                    EnsureTMP(catLbl,catNome,22f,Hex("#C8A0FF")).fontStyle=FontStyles.Bold;
                    yPos -= 40f;
                    foreach(var uid in catIds){
                        var data=PermanentUpgradeSystem.GetData(uid);
                        if(data==null) continue;
                        var (row,_)=FindOrCreateUI(auGO.transform,$"UpRow_{uid}");
                        SetRect(RT(row),new(0,1),new(1,1),new(.5f,1),new(0,yPos),new(0,52));
                        EnsureImage(row,Hex("#151530"));
                        var (rnm,_)=FindOrCreateUI(row.transform,"Nome");
                        SetRect(RT(rnm),new(0,0),new(.6f,1),new(0,.5f),new(12,0),Vector2.zero);
                        EnsureTMP(rnm,$"{data.nome}\n<size=16><color=#888>{data.descricao}</color></size>",20f,Color.white);
                        var (rbtn,rbn)=FindOrCreateUI(row.transform,"BtnUpgrade");
                        if(rbn){ SetRect(RT(rbtn),new(.6f,.1f),new(1,.9f),new(1,.5f),new(-12,0),Vector2.zero);
                        EnsureImage(rbtn,Hex("#2A1060")); EnsureButton(rbtn);
                        AddLabel(rbtn,$"💎 {data.diamondCostPerLevel}",18f,Color.white); total++; }
                        var lojaCtrl=lojaGO.GetComponent<LojaController>();
                        var rbtnComp=rbtn.GetComponent<UnityEngine.UI.Button>();
                        if(rbtnComp!=null && lojaCtrl!=null){
                            PermanentUpgradeId capturedId=uid;
                            rbtnComp.onClick.RemoveAllListeners();
                            rbtnComp.onClick.AddListener(()=>lojaCtrl.ComprarUpgrade(capturedId));
                        }
                        yPos -= 56f; total++;
                    }
                    yPos -= 10f;
                }
            }

            // Aba Diamantes
            var (adGO,adn)=FindOrCreateUI(lojaTr,"AbaDiamantes");
            if(adn){ SetRect(RT(adGO),new(0,0),new(1,1),new(.5f,.5f),new(0,-205),new(0,-205));
            EnsureImage(adGO,Hex("#0D0D1F")); adGO.SetActive(false); total++; }
            TryWire(lojaSO,"abaDiamantes",adGO,log);
            {
                var pacotes=LojaController.GetPacotes();
                float py=80f;
                for(int i=0;i<pacotes.Length;i++){
                    var (pid,pnome,pdias,ppreco)=pacotes[i];
                    var (card,cn)=FindOrCreateUI(adGO.transform,$"CardPacote_{i}");
                    if(cn){
                        SetRect(RT(card),new(.5f,1),new(.5f,1),new(.5f,1),new(0,py-i*180f),new(460,160));
                        EnsureImage(card,Hex("#0A1E40"));
                        var (nm,_)=FindOrCreateUI(card.transform,"Info");
                        SetRect(RT(nm),new(0,0),new(.6f,1),new(0,.5f),new(16,0),Vector2.zero);
                        EnsureTMP(nm,$"{pnome}\n💎 {pdias}",26f,Color.white);
                        var (btn,bn)=FindOrCreateUI(card.transform,"BtnPacote");
                        if(bn){ SetRect(RT(btn),new(.6f,.1f),new(1,.9f),new(1,.5f),new(-12,0),Vector2.zero);
                        EnsureImage(btn,Hex("#1A4A90")); EnsureButton(btn);
                        AddLabel(btn,ppreco,22f,Color.white); total++;
                        var lojaCtrl=lojaGO.GetComponent<LojaController>();
                        var pbtComp=btn.GetComponent<UnityEngine.UI.Button>();
                        if(pbtComp!=null && lojaCtrl!=null){
                            string cpid=pid;
                            pbtComp.onClick.RemoveAllListeners();
                            pbtComp.onClick.AddListener(()=>lojaCtrl.ComprarDiamantes(cpid));
                        }}
                        log.AppendLine($"  Loja/Pacote_{i}"); total++;
                    }
                }
                var (vbtn,vbn)=FindOrCreateUI(adGO.transform,"BtnVideo");
                if(vbn){ SetRect(RT(vbtn),new(.5f,1),new(.5f,1),new(.5f,1),new(0,py-pacotes.Length*180f-20f),new(460,70));
                EnsureImage(vbtn,Hex("#1A5020")); EnsureButton(vbtn);
                AddLabel(vbtn,"Assistir Video  +50 Diamantes",24f,Color.white); total++;
                var lojaCtrl=lojaGO.GetComponent<LojaController>();
                var vComp=vbtn.GetComponent<UnityEngine.UI.Button>();
                if(vComp!=null && lojaCtrl!=null){
                    vComp.onClick.RemoveAllListeners();
                    vComp.onClick.AddListener(()=>lojaCtrl.AssistirVideo());
                }}
            }

            // Feedback
            var (fbGO,fbn)=FindOrCreateUI(lojaTr,"TextoFeedback");
            if(fbn){ SetRect(RT(fbGO),new(0,0),new(1,0),new(.5f,0),new(0,30),new(0,55));
            var ftmp=EnsureTMP(fbGO,"",28f,Hex("#FFD700")); ftmp.alignment=TextAlignmentOptions.Center;
            fbGO.SetActive(false); total++; }
            TryWire(lojaSO,"textoFeedback",fbGO.GetComponent<TextMeshProUGUI>(),log);

            lojaSO.ApplyModifiedProperties();
        }

        // PopupRecompensa
        {
            var (go,isNew)=FindOrCreateUI(canvasTr,"PopupRecompensa");
            if(isNew){ SetRect(RT(go),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(700,500)); EnsureImage(go,Hex("#1E0A3C")); go.SetActive(false); log.AppendLine("  PopupRecompensa"); total++; }
            var popupImg = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            var popupSprite = LoadUI("complete_container.png");
            if(popupSprite != null){ popupImg.sprite = popupSprite; popupImg.type = Image.Type.Simple; popupImg.color = Color.white; }
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
            s.referenceResolution = new Vector2(1080f, 1920f);
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
            hudBgRT.sizeDelta       = new Vector2(0f, 120f);
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

        // TopBar — layout RPG portrait
        {
        var (go,isNew)=FindOrCreateUI(hudTr,"TopBar");
        if(isNew){ AnchorTopBar(RT(go),120f); EnsureImage(go,new Color(0,0,0,0)); log.AppendLine("  TopBar"); total++; }
        var tr=go.transform;

        // Avatar 96x96
        var (avGO,avN)=FindOrCreateUI(tr,"Avatar");
        if(avN){
            var avRT=RT(avGO);
            avRT.anchorMin=new Vector2(0f,1f); avRT.anchorMax=new Vector2(0f,1f);
            avRT.pivot=new Vector2(0f,1f);
            avRT.anchoredPosition=new Vector2(8f,-30f);
            avRT.sizeDelta=new Vector2(96f,96f);
            var bgImg=avGO.GetComponent<Image>()??avGO.AddComponent<Image>();
            bgImg.color=Hex("#8B6914");
            var (innerGO,_)=FindOrCreateUI(avGO.transform,"Inner");
            var iRT=RT(innerGO); iRT.anchorMin=Vector2.zero; iRT.anchorMax=Vector2.one;
            iRT.offsetMin=new Vector2(3f,3f); iRT.offsetMax=new Vector2(-3f,-3f);
            var iImg=innerGO.GetComponent<Image>()??innerGO.AddComponent<Image>(); iImg.color=Hex("#1A0E04");
            var (imgGO,_)=FindOrCreateUI(avGO.transform,"AvatarImg");
            var aRT=RT(imgGO); aRT.anchorMin=Vector2.zero; aRT.anchorMax=Vector2.one;
            aRT.offsetMin=new Vector2(6f,6f); aRT.offsetMax=new Vector2(-6f,-6f);
            var avImg=imgGO.GetComponent<Image>()??imgGO.AddComponent<Image>();
            avImg.preserveAspect=true; avImg.color=Color.white;
            TryWire(hudSO,"avatarImagem",avImg,log);
            log.AppendLine("  Avatar"); total++;
        }

        // 5 Boost slots 28x28 horizontais acima das barras
        for(int i=0;i<5;i++){
            var (bsGO,bsN)=FindOrCreateUI(tr,$"BoostSlot{i}");
            if(bsN){
                var bRT=RT(bsGO);
                bRT.anchorMin=new Vector2(0f,1f); bRT.anchorMax=new Vector2(0f,1f);
                bRT.pivot=new Vector2(0f,1f);
                bRT.anchoredPosition=new Vector2(148f+(i*32f),-30f);
                bRT.sizeDelta=new Vector2(28f,28f);
                var bImg=bsGO.GetComponent<Image>()??bsGO.AddComponent<Image>(); bImg.color=Hex("#5A4010");
                var (bInner,_)=FindOrCreateUI(bsGO.transform,"Inner");
                var biRT=RT(bInner); biRT.anchorMin=Vector2.zero; biRT.anchorMax=Vector2.one;
                biRT.offsetMin=new Vector2(2f,2f); biRT.offsetMax=new Vector2(-2f,-2f);
                var biImg=bInner.GetComponent<Image>()??bInner.AddComponent<Image>(); biImg.color=Hex("#1A0E04");
                var (bIcon,_)=FindOrCreateUI(bsGO.transform,"Icon");
                var bcRT=RT(bIcon); bcRT.anchorMin=Vector2.zero; bcRT.anchorMax=Vector2.one;
                bcRT.offsetMin=new Vector2(4f,4f); bcRT.offsetMax=new Vector2(-4f,-4f);
                var bcImg=bIcon.GetComponent<Image>()??bIcon.AddComponent<Image>(); bcImg.color=new Color(1f,1f,1f,0f);
                log.AppendLine($"  BoostSlot{i}"); total++;
            }
        }

        // 3 Barras fixas
        float[] bWidths ={300f,340f,260f};
        float[] bHeights={12f,12f,12f};
        float[] bYpos   ={-64f,-80f,-96f};
        Color[] bBorders={Hex("#8B6914"),Hex("#3A1A6A"),Hex("#1A5A1A")};
        Color[] bBGs    ={new Color(.12f,.04f,.04f,1f),new Color(.06f,.03f,.12f,1f),new Color(.03f,.12f,.03f,1f)};
        Color[] bFills  ={new Color(.85f,.15f,.1f,1f),new Color(.2f,.35f,.95f,1f),new Color(.1f,.8f,.2f,1f)};
        RectTransform fillVidaRT=null,fillXPRT=null,fillPoderRT=null;
        string[] bNames={"HealthBar","XPBar","PoderBar"};
        for(int i=0;i<3;i++){
            var (barGO,barN)=FindOrCreateUI(tr,bNames[i]);
            if(barN){
                var bRT=RT(barGO);
                bRT.anchorMin=new Vector2(0f,1f); bRT.anchorMax=new Vector2(0f,1f);
                bRT.pivot=new Vector2(0f,1f);
                bRT.anchoredPosition=new Vector2(148f,bYpos[i]);
                bRT.sizeDelta=new Vector2(bWidths[i],bHeights[i]);
                EnsureImage(barGO,bBorders[i]);
                var fRT=BuildBar(barGO,bBGs[i],bFills[i]);
                if(i==0)fillVidaRT=fRT; else if(i==1)fillXPRT=fRT; else fillPoderRT=fRT;
                log.AppendLine($"  {bNames[i]}"); total++;
            } else {
                var fRT=barGO.transform.Find("Fill")?.GetComponent<RectTransform>();
                if(i==0)fillVidaRT=fRT; else if(i==1)fillXPRT=fRT; else fillPoderRT=fRT;
            }
        }

        // Timer top-right
        var (tiGO,tiN)=FindOrCreateUI(tr,"TimerText");
        if(tiN){
            var tRT=RT(tiGO);
            tRT.anchorMin=new Vector2(1f,0f); tRT.anchorMax=new Vector2(1f,1f);
            tRT.pivot=new Vector2(1f,0.5f);
            tRT.anchoredPosition=new Vector2(-12f,0f);
            tRT.sizeDelta=new Vector2(130f,0f);
            var t=EnsureTMP(tiGO,"10:00",30f,Color.white);
            t.fontStyle=FontStyles.Bold; t.alignment=TextAlignmentOptions.Right;
            log.AppendLine("  TimerText"); total++;
        }

        if(fillVidaRT!=null)  TryWire(hudSO,"fillVida",  fillVidaRT,  log);
        if(fillXPRT!=null)    TryWire(hudSO,"fillXP",    fillXPRT,    log);
        if(fillPoderRT!=null) TryWire(hudSO,"fillPoder",  fillPoderRT, log);
        TryWire(hudSO,"textoTimer", tiGO.GetComponent<TextMeshProUGUI>(),  log);

        var hudComp=hudGO.GetComponent<HUDComplete>();
        if(hudComp!=null){
            var boostImgs=new UnityEngine.UI.Image[5];
            for(int i=0;i<5;i++){
                var slot=tr.Find($"BoostSlot{i}/Icon");
                if(slot!=null) boostImgs[i]=slot.GetComponent<UnityEngine.UI.Image>();
            }
            hudComp.boostSlots=boostImgs;
            UnityEditor.EditorUtility.SetDirty(hudComp);
        }
        }

        // PauseButton — fora da TopBar, canto direito abaixo dela
        {
            var (pbGO,pbN)=FindOrCreateUI(hudTr,"PauseButton");
            if(pbN){
                var pRT=RT(pbGO);
                pRT.anchorMin=new Vector2(1f,1f); pRT.anchorMax=new Vector2(1f,1f);
                pRT.pivot=new Vector2(1f,1f);
                pRT.anchoredPosition=new Vector2(-8f,-128f);
                pRT.sizeDelta=new Vector2(48f,48f);
                EnsureImage(pbGO,Hex("#00000080")); EnsureButton(pbGO);
                AddLabel(pbGO,"II",22f,Color.white);
                log.AppendLine("  PauseButton"); total++;
            }
            TryWire(hudSO,"botaoPause",hudTr.Find("PauseButton")?.GetComponent<Button>(),log);
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
                SetRect(RT(icoGO),Vector2.zero,Vector2.one,new Vector2(.5f,.5f),new Vector2(8f,8f),new Vector2(-16f,-16f));
                var icoImg=icoGO.GetComponent<Image>()??icoGO.AddComponent<Image>();
                var swordSprite=LoadUI("action_button_sword.png");
                if(swordSprite!=null){ icoImg.sprite=swordSprite; icoImg.preserveAspect=true; }
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

        // CoverPanel — painel preto que cobre tudo até a Lore terminar
        {
            var coverGO = GameObject.Find("CoverPanel");
            if (coverGO != null) Undo.DestroyObjectImmediate(coverGO);
            coverGO = new GameObject("CoverPanel");
            Undo.RegisterCreatedObjectUndo(coverGO, "Layout GameScene");
            var c = coverGO.AddComponent<Canvas>();
            c.renderMode   = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 998;
            coverGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var coverRT = coverGO.GetComponent<RectTransform>();
            coverRT.anchorMin = Vector2.zero;
            coverRT.anchorMax = Vector2.one;
            var img = coverGO.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            coverGO.AddComponent<CoverPanel>();
            log.AppendLine("  CoverPanel criado"); total++;
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
                s.referenceResolution = new Vector2(1080f, 1920f);
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
                s.referenceResolution = new Vector2(1080f, 1920f);
                s.matchWidthOrHeight  = 0.5f;
            }
            jcGO.AddComponent<GraphicRaycaster>();
            log.AppendLine("  JoystickCanvas criado"); total++;
            var jcTr = jcGO.transform;

            var (bgGO, bgNew) = FindOrCreateUI(jcTr, "JoystickBackground");
            if (bgNew)
            {
                SetRect(RT(bgGO), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(80f, 80f), new Vector2(180f, 180f));
                var bgImg = bgGO.GetComponent<Image>() ?? bgGO.AddComponent<Image>();
                var joySprite = LoadUI("joystick_complete.png");
                if (joySprite != null) { bgImg.sprite = joySprite; bgImg.color = new Color(1f, 1f, 1f, 0.7f); }
                else bgImg.color = new Color(1f, 1f, 1f, 0.15f);
                log.AppendLine("  JoystickBackground"); total++;
            }

            var (knobGO, knobNew) = FindOrCreateUI(bgGO.transform, "JoystickKnob");
            if (knobNew)
            {
                SetRect(RT(knobGO), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(70f, 70f));
                var knobImg = knobGO.GetComponent<Image>() ?? knobGO.AddComponent<Image>();
                var frameSprite = LoadUI("joystick_frame.png");
                if (frameSprite != null) { knobImg.sprite = frameSprite; knobImg.color = new Color(1f, 1f, 1f, 0.9f); }
                else knobImg.color = new Color(1f, 1f, 1f, 0.3f);
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

    const string GUIPRO = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/";

    static void ApplyGUISprite(GameObject go, string spritePath, Color tint)
    {
        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img == null) img = go.AddComponent<UnityEngine.UI.Image>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GUIPRO + spritePath);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = tint;
            img.pixelsPerUnitMultiplier = 1f;
        }
        else
        {
            Debug.LogWarning($"[GUIPro] Sprite não encontrado: {spritePath}");
        }
    }

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

    static RectTransform BuildBar(GameObject go, Color bgColor, Color fillColor)
    {
        var bg = new GameObject("BG");
        Undo.RegisterCreatedObjectUndo(bg, "Solengard Layout");
        bg.transform.SetParent(go.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(2f, 2f); bgRT.offsetMax = new Vector2(-2f, -2f);

        var fill = new GameObject("Fill");
        Undo.RegisterCreatedObjectUndo(fill, "Solengard Layout");
        fill.transform.SetParent(go.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = new Vector2(3f, 3f);
        fillRT.offsetMax = new Vector2(-3f, -3f);
        fillRT.pivot     = new Vector2(0f, 0.5f);

        // Left cap shadow
        var capL = new GameObject("CapL");
        Undo.RegisterCreatedObjectUndo(capL, "Solengard Layout");
        capL.transform.SetParent(go.transform, false);
        var capLImg = capL.AddComponent<Image>();
        capLImg.color = new Color(0f,0f,0f,0.4f);
        var capLRT = capL.GetComponent<RectTransform>();
        capLRT.anchorMin = new Vector2(0f,0f); capLRT.anchorMax = new Vector2(0f,1f);
        capLRT.offsetMin = new Vector2(2f,2f); capLRT.offsetMax = new Vector2(8f,-2f);

        // Right cap shadow
        var capR = new GameObject("CapR");
        Undo.RegisterCreatedObjectUndo(capR, "Solengard Layout");
        capR.transform.SetParent(go.transform, false);
        var capRImg = capR.AddComponent<Image>();
        capRImg.color = new Color(0f,0f,0f,0.4f);
        var capRRT = capR.GetComponent<RectTransform>();
        capRRT.anchorMin = new Vector2(1f,0f); capRRT.anchorMax = new Vector2(1f,1f);
        capRRT.offsetMin = new Vector2(-8f,2f); capRRT.offsetMax = new Vector2(-2f,-2f);

        return fillRT;
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
