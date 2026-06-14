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

    static readonly string ICONS_UI   = "Assets/Art/UI/Icons/";
    static readonly string BG_UI      = "Assets/Art/UI/Backgrounds/";
    static readonly string BTN_GUIPRO = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_Component_Buttons/Button_Rectangle_01_Convex_White.prefab";
    static Sprite LoadBG(string name)
    {
        string path = BG_UI + name;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
        {
            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite LoadIcon(string name)
    {
        string path = ICONS_UI + name;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        // Must check spriteImportMode too: textureType=8 already but spriteMode=2 (Multiple)
        // makes LoadAssetAtPath<Sprite> return null — force Single on every call that needs fixing.
        if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
        {
            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ── Menu items ──────────────────────────────────────────────────────────────

    [MenuItem("Solengard/Legacy (NAO USAR)/Layout MainMenu (destrutivo)")]
    static void LayoutMainMenu()
    {
        if (!EditorUtility.DisplayDialog(
            "Gerador destrutivo (legado)",
            "Este comando DESTRÓI e recria o Canvas do MainMenu do zero, REGREDINDO a cena viva " +
            "(que agora é a fonte da verdade).\n\nUse apenas como referência histórica.\n\nContinuar mesmo assim?",
            "Sim, rodar mesmo assim", "Cancelar")) return;
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

    [MenuItem("Solengard/Legacy (NAO USAR)/Layout MainMenu (destrutivo)", validate = true)]
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
            StretchFull(RT(go)); // sempre reposiciona (não só na criação)
            if (isNew) { EnsureImage(go, Hex("#0A0A1A")); log.AppendLine("  BG"); total++; }
            // Fundo dark fantasy — força reimport como Sprite se necessário
            const string BG_PATH = "Assets/Art/UI/Backgrounds/menu_background.png";
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
            var topBarBG = LoadBG("topbar_background.png");
            if (topBarBG != null) { topBarImg.sprite = topBarBG; topBarImg.color = Color.white; topBarImg.type = Image.Type.Simple; topBarImg.preserveAspect = false; }
            topBarImg.raycastTarget = false;

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
            if(dN){ SetRect(RT(dGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(110,0),new(160,56)); EnsureTMP(dGO,"0",36f,Hex("#FFD700")); log.AppendLine("  TopBar/TextoDiamantes"); total++; }
            // Ícone de diamante à esquerda do número
            var (icoDia,icoDiaN)=FindReparentOrCreateUI(tr,canvasTr,"IcoDiamante");
            SetRect(RT(icoDia),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(50,0),new(44,44));
            var icoDiaImg = icoDia.GetComponent<UnityEngine.UI.Image>() ?? icoDia.AddComponent<UnityEngine.UI.Image>();
            var gemSp = LoadIcon("icon_diamante.png");
            if(gemSp!=null){ icoDiaImg.sprite=gemSp; icoDiaImg.color=Color.white; icoDiaImg.preserveAspect=true; }
            icoDiaImg.raycastTarget=false;

            { var moedasT = tr.Find("TextoMoedas"); if(moedasT!=null) Object.DestroyImmediate(moedasT.gameObject); }

            var (cfgGO,cfgN)=FindReparentOrCreateUI(tr,canvasTr,"BotaoConfiguracoes");
            botaoConfigGO=cfgGO;
            if(cfgN){ SetRect(RT(cfgGO),new(1,.5f),new(1,.5f),new(1,.5f),new(-60,0),new(70,70)); EnsureImage(cfgGO,Hex("#1A1A2A")); EnsureButton(cfgGO); log.AppendLine("  TopBar/BotaoConfiguracoes"); total++; }
            // Sprite de engrenagem em vez do emoji ⚙
            var cfgLabel = cfgGO.transform.Find("Label");
            if(cfgLabel!=null) Object.DestroyImmediate(cfgLabel.gameObject);
            var cfgImg = cfgGO.GetComponent<UnityEngine.UI.Image>() ?? cfgGO.AddComponent<UnityEngine.UI.Image>();
            var gearSp = LoadIcon("icon_config.png");
            if(gearSp!=null){ cfgImg.sprite=gearSp; cfgImg.color=Color.white; cfgImg.preserveAspect=true; }

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
            { var (c,n)=FindOrCreateUI(tr,"TextoTitulo"); SetRect(RT(c),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,350),new(800,100)); var t=EnsureTMP(c,"SOLENGARD",72f,Hex("#C8A0FF")); t.fontStyle=FontStyles.Bold; t.gameObject.SetActive(false); /* arte de fundo já contém o título SOLENGARD */ if(n){ log.AppendLine("  CenterArea/TextoTitulo"); total++; } }

            var (tGO,tN)=FindOrCreateUI(tr,"TextoTemporada");
            textoTemporadaGO=tGO;
            SetRect(RT(tGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,280),new(600,60)); EnsureTMP(tGO,"Temporada 1",28f,Hex("#8080AA"));
            tGO.SetActive(false); // ocultar até temporadas serem implementadas
            if(tN){ log.AppendLine("  CenterArea/TextoTemporada"); total++; }

            var (sGO,sN)=FindOrCreateUI(tr,"TextoStreak");
            textoStreakGO=sGO;
            SetRect(RT(sGO),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,230),new(400,60)); EnsureTMP(sGO,"* Dia 1",28f,Hex("#FFD700"));
            sGO.SetActive(false); // ocultar até streak/login serem implementados
            if(sN){ log.AppendLine("  CenterArea/TextoStreak"); total++; }

            // SeasonBanner — always apply position
            { var (bn,bnN)=FindOrCreateUI(tr,"SeasonBanner");
              SetRect(RT(bn),new(.5f,.5f),new(.5f,.5f),new(.5f,.5f),new(0,120),new(700,180));
              if(bnN){ EnsureImage(bn,Hex("#1E0A3C")); log.AppendLine("  CenterArea/SeasonBanner"); total++; }
              var (bnt,bntN)=FindOrCreateUI(bn.transform,"TextoSeasonBanner");
              if(bntN){ StretchFull(RT(bnt)); var tmp=EnsureTMP(bnt,"> Temporada das Sombras\nComplete 50 waves para ganhar a skin lendaria",28f,Color.white); tmp.textWrappingMode=TMPro.TextWrappingModes.Normal; log.AppendLine("  SeasonBanner/TextoSeasonBanner"); total++; }
              bn.SetActive(false); } // ocultar até sistema de temporadas ser implementado

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
            StretchFull(RT(go)); // sempre — painel existente pode ter âncoras antigas
            if(isNew){ EnsureImage(go,Hex(color)); go.SetActive(false); log.AppendLine($"  {name}"); total++; }
            var imgFundo = go.GetComponent<UnityEngine.UI.Image>();
            if (imgFundo == null) imgFundo = go.AddComponent<UnityEngine.UI.Image>();
            var panelBG = LoadBG("panel_background.png");
            if (panelBG != null) { imgFundo.sprite = panelBG; imgFundo.color = Color.white; imgFundo.type = UnityEngine.UI.Image.Type.Simple; imgFundo.preserveAspect = false; }
            else if (imgFundo.color.a < 0.99f || imgFundo.color == Color.white) imgFundo.color = Hex(color);
            imgFundo.raycastTarget = true;
            TryWire(mmmSO,field,go,log);
            // Botão X de fechar nos painéis que precisam (auto-liga via BotaoFecharPainel)
            if (name == "PainelMissoes" || name == "PainelPasse" || name == "PainelRanking")
                CriarBotaoFechar(go.transform, "BtnFechar");
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
                var tmp=EnsureTMP(t,"LOJA",42f,Color.white); tmp.fontStyle=FontStyles.Bold; tmp.alignment=TMPro.TextAlignmentOptions.Left; }
              var (icH,_)=FindOrCreateUI(h.transform,"IcoDiamanteHeader");
              SetRect(RT(icH),new(.65f,0),new(.65f,1),new(0,.5f),new(8,0),new(36,36));
              var icHImg=icH.GetComponent<UnityEngine.UI.Image>()??icH.AddComponent<UnityEngine.UI.Image>();
              var icHSp=LoadIcon("icon_diamante.png");
              if(icHSp!=null){ icHImg.sprite=icHSp; icHImg.color=Color.white; icHImg.preserveAspect=true; }
              icHImg.raycastTarget=false;
              var (sGO,_)=FindOrCreateUI(h.transform,"TextoSaldo");
              SetRect(RT(sGO),new(.65f,0),new(.88f,1),new(0,.5f),new(50,0),Vector2.zero);
              var sTMP=EnsureTMP(sGO,"0",32f,Hex("#FFD700")); sTMP.alignment=TMPro.TextAlignmentOptions.Left;
              TryWire(lojaSO,"textoSaldo",sGO.GetComponent<TextMeshProUGUI>(),log); }

            // Botão X de fechar
            var btnFecharLoja = CriarBotaoFechar(lojaTr, "BtnFecharLoja");
            TryWire(mmmSO, "botaoFecharLoja", btnFecharLoja.GetComponent<UnityEngine.UI.Button>(), log);

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
            { var i=apGO.GetComponent<UnityEngine.UI.Image>(); if(i!=null){ i.sprite=null; i.color=new Color(0,0,0,0); } }
            {
                var classesData=LojaController.GetClasses();
                float cW=320f, cH=270f, padX=80f, padY=50f;
                for(int i=0;i<classesData.Length;i++){
                    var (id,nome,preco)=classesData[i];
                    int col=i%2, row=i/2;
                    float x = col==0 ? -(cW/2+padX/2) : (cW/2+padX/2);
                    float y = 0f - row*(cH+padY);
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
                        WireMenuButton(btn, Solengard.UI.MenuAction.ComprarClasse, id); // Passo 4: substitui lambda nao-serializada
                        log.AppendLine($"  Loja/Card_{id}"); total++;
                    }
                }
            }

            // Aba Upgrades — lista com categorias
            var (auGO,aun)=FindOrCreateUI(lojaTr,"AbaUpgrades");
            if(aun){ SetRect(RT(auGO),new(0,0),new(1,1),new(.5f,.5f),new(0,-205),new(0,-205));
            EnsureImage(auGO,Hex("#0D0D1F")); auGO.SetActive(false); total++; }
            TryWire(lojaSO,"abaUpgrades",auGO,log);
            { var i=auGO.GetComponent<UnityEngine.UI.Image>(); if(i!=null){ i.sprite=null; i.color=new Color(0,0,0,0); } }
            {
                var cats = new (string nome, PermanentUpgradeId[] ids)[] {
                    ("Ofensa",     new[]{PermanentUpgradeId.Poder, PermanentUpgradeId.Recarga}),
                    ("Defesa",     new[]{PermanentUpgradeId.Armadura, PermanentUpgradeId.VidaMaxima, PermanentUpgradeId.Recuperacao}),
                    ("Ataque",     new[]{PermanentUpgradeId.Area, PermanentUpgradeId.Velocidade, PermanentUpgradeId.Duracao, PermanentUpgradeId.Quantidade}),
                    ("Mobilidade", new[]{PermanentUpgradeId.Movimento, PermanentUpgradeId.Magnetismo}),
                    ("Progressao", new[]{PermanentUpgradeId.Crescimento, PermanentUpgradeId.Riqueza}),
                    ("Especiais",  new[]{PermanentUpgradeId.Maldicao, PermanentUpgradeId.Ressurreicao, PermanentUpgradeId.PoderEspecial}),
                };
                float yPos = -20f;
                foreach(var (catNome, catIds) in cats){
                    var (catLbl,_)=FindOrCreateUI(auGO.transform,$"Cat_{catNome}");
                    SetRect(RT(catLbl),new(0,1),new(1,1),new(.5f,1),new(0,yPos),new(-60,36));
                    EnsureTMP(catLbl,catNome,22f,Hex("#C8A0FF")).fontStyle=FontStyles.Bold;
                    yPos -= 40f;
                    foreach(var uid in catIds){
                        var data=PermanentUpgradeSystem.GetData(uid);
                        if(data==null) continue;
                        var (row,_)=FindOrCreateUI(auGO.transform,$"UpRow_{uid}");
                        SetRect(RT(row),new(0,1),new(1,1),new(.5f,1),new(0,yPos),new(-60,52));
                        EnsureImage(row,Hex("#151530"));
                        var (rnm,_)=FindOrCreateUI(row.transform,"Nome");
                        SetRect(RT(rnm),new(0,0),new(.6f,1),new(0,.5f),new(12,0),Vector2.zero);
                        EnsureTMP(rnm,$"{data.nome}\n<size=16><color=#888>{data.descricao}</color></size>",20f,Color.white);
                        var (rbtn,rbn)=FindOrCreateUI(row.transform,"BtnUpgrade");
                        if(rbn){ SetRect(RT(rbtn),new(.6f,.1f),new(1,.9f),new(1,.5f),new(-12,0),Vector2.zero);
                        EnsureImage(rbtn,Hex("#2A1060")); EnsureButton(rbtn);
                        AddLabel(rbtn,$"💎 {PermanentUpgradeSystem.GetCusto(data.id,0)}",18f,Color.white); total++; }
                        var wire=rbtn.GetComponent<Solengard.UI.BotaoComprarUpgrade>()??rbtn.AddComponent<Solengard.UI.BotaoComprarUpgrade>();
                        wire.upgradeId=uid.ToString();
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
            { var i=adGO.GetComponent<UnityEngine.UI.Image>(); if(i!=null){ i.sprite=null; i.color=new Color(0,0,0,0); } }
            {
                var pacotes=LojaController.GetPacotes();
                float py=80f;
                for(int i=0;i<pacotes.Length;i++){
                    var (pid,pnome,pdias,ppreco,pbonus,pbadge)=pacotes[i];
                    var (card,cn)=FindOrCreateUI(adGO.transform,$"CardPacote_{i}");
                    SetRect(RT(card),new(.5f,1),new(.5f,1),new(.5f,1),new(0,py-i*240f),new(500,200));
                    if(cn){ EnsureImage(card,Hex("#0A1E40")); total++; }

                    // Badge (faixa no topo do card)
                    if(!string.IsNullOrEmpty(pbadge)){
                        var (bdg,_)=FindOrCreateUI(card.transform,"Badge");
                        SetRect(RT(bdg),new(0,1),new(1,1),new(.5f,1),new(0,0),new(0,30));
                        EnsureImage(bdg,Hex(pbadge=="MELHOR VALOR"?"#8B2535":"#FFD700"));
                        var (btxt,_)=FindOrCreateUI(bdg.transform,"BadgeText");
                        StretchFull(RT(btxt));
                        var btmp=EnsureTMP(btxt,pbadge,18f,pbadge=="MELHOR VALOR"?Color.white:Color.black);
                        btmp.alignment=TMPro.TextAlignmentOptions.Center; btmp.fontStyle=TMPro.FontStyles.Bold;
                    }

                    // Info (esquerda 60%): nome + diamantes + bônus
                    var (info,_)=FindOrCreateUI(card.transform,"Info");
                    SetRect(RT(info),new(0,0),new(.6f,1),new(0,.5f),new(20,0),Vector2.zero);
                    string infoTxt=$"{pnome}\n<size=130%>💎 {pdias}</size>";
                    if(pbonus>0) infoTxt+=$"\n<color=#FFD700>+{pbonus}% BÔNUS</color>";
                    var itmp=EnsureTMP(info,infoTxt,24f,Color.white);
                    itmp.alignment=TMPro.TextAlignmentOptions.Left;

                    // Botão de compra (direita)
                    var (btnP,btnPn)=FindOrCreateUI(card.transform,"BtnPacote");
                    SetRect(RT(btnP),new(.62f,.5f),new(.62f,.5f),new(.5f,.5f),new(80f,0),new(150f,90f));
                    if(btnPn){ EnsureImage(btnP,Hex("#5A1090")); EnsureButton(btnP); }
                    var (precoTxt,_)=FindOrCreateUI(btnP.transform,"Preco");
                    StretchFull(RT(precoTxt));
                    var ptmp=EnsureTMP(precoTxt,ppreco,22f,Color.white);
                    ptmp.alignment=TMPro.TextAlignmentOptions.Center;
                    WireMenuButton(btnP, Solengard.UI.MenuAction.ComprarPacote, pid); // Passo 4: substitui lambda nao-serializada

                    log.AppendLine($"  Loja/Pacote_{i}"); total++;
                }
                var (vbtn,vbn)=FindOrCreateUI(adGO.transform,"BtnVideo");
                if(vbn){ SetRect(RT(vbtn),new(.5f,1),new(.5f,1),new(.5f,1),new(0,py-pacotes.Length*240f),new(500,100));
                EnsureImage(vbtn,Hex("#1A5020")); EnsureButton(vbtn);
                AddLabel(vbtn,"Assistir Video  +50 Diamantes",24f,Color.white); total++;
                WireMenuButton(vbtn, Solengard.UI.MenuAction.AssistirVideo); // Passo 4: substitui lambda nao-serializada
                }
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

    // ── Skin Element: instancia prefab GUI Pro como filho visual (não destrói lógica) ──
    static void SkinElement(GameObject host, string prefabPath, Color corBg, string labelTexto = null)
    {
        if (host == null) { Debug.LogWarning("[Skin] host nulo."); return; }

        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning($"[Skin] Prefab não encontrado: {prefabPath}"); return; }

        // 1. Remove skin anterior se existir (idempotente — pode rodar várias vezes)
        var skinAntigo = host.transform.Find("[Skin]");
        if (skinAntigo != null) Object.DestroyImmediate(skinAntigo.gameObject);

        // 2. Host Image vira hitbox invisível (NUNCA enabled=false) — a skin assume o visual,
        //    mas o host mantém um alvo de raycast determinístico no retângulo cheio do botão.
        var hostImg = host.GetComponent<UnityEngine.UI.Image>();
        if (hostImg != null) { hostImg.sprite = null; hostImg.color = new Color(0,0,0,0); hostImg.enabled = true; hostImg.raycastTarget = true; }

        // 3. Instancia o prefab visual como filho
        var skin = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, host.transform);
        skin.name = "[Skin]";

        // 4. Stretch full dentro do host
        var srt = skin.GetComponent<RectTransform>();
        if (srt == null) srt = skin.AddComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        srt.localScale = Vector3.one;
        srt.SetAsFirstSibling(); // skin atrás de tudo (Label fica na frente)

        // 5. Desliga raycast de TODAS as Images da skin (cliques passam pro Button do host)
        foreach (var img in skin.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            img.raycastTarget = false;

        // 6. Recolore o filho "Bg" da skin com a cor do Solengard
        var bgTr = skin.transform.Find("Bg");
        UnityEngine.UI.Image bgImg = null;
        if (bgTr != null)
        {
            bgImg = bgTr.GetComponent<UnityEngine.UI.Image>();
            if (bgImg != null) bgImg.color = corBg;
        }
        else Debug.LogWarning($"[Skin] Filho 'Bg' não encontrado no prefab {prefab.name}");

        // 7. Reconecta Button.targetGraphic para o Bg da skin (estados visuais funcionam)
        var btn = host.GetComponent<UnityEngine.UI.Button>();
        if (btn != null && bgImg != null) btn.targetGraphic = bgImg;

        // 8. Remove o TMP de demonstração que vem dentro do prefab (Text / Text_Title etc)
        foreach (var tmp in skin.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            Object.DestroyImmediate(tmp.gameObject);

        // 9. Garante que o Label do HOST (nosso texto real) fica na frente da skin
        var labelTr = host.transform.Find("Label");
        if (labelTr != null)
        {
            labelTr.SetAsLastSibling();
            if (labelTexto != null)
            {
                var tmp = labelTr.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.text = labelTexto;
            }
        }

        UnityEditor.EditorUtility.SetDirty(host);
    }

    static void ApplyButtonLayers(GameObject go, Color corBg, Color corBorder, string labelTexto = null)
    {
        const string BASE = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/";
        var spriteBg       = AssetDatabase.LoadAssetAtPath<Sprite>(BASE + "Button/Button_Rectangle_01_Convex_White_Bg.png");
        var spriteBorder   = AssetDatabase.LoadAssetAtPath<Sprite>(BASE + "Button/Button_Rectangle_01_Convex_White_Border.png");
        var spriteLight    = AssetDatabase.LoadAssetAtPath<Sprite>(BASE + "Button/Button_Rectangle_01_Convex_White_Light.png");
        var spriteGradient = AssetDatabase.LoadAssetAtPath<Sprite>(BASE + "Button/Button_Rectangle_01_Convex_White_Gradient.png");
        if (spriteBg == null) { Debug.LogWarning($"[ApplyButtonLayers] Sprite Bg não encontrado em {BASE}Button/"); return; }
        // Desativa Image do root em vez de destruir (evita MissingReferenceException na closure)
        var rootImg = go.GetComponent<UnityEngine.UI.Image>();
        if (rootImg != null)
        {
            rootImg.sprite = null;
            rootImg.color = new Color(0, 0, 0, 0);
            rootImg.enabled = false;
        }

        // Captura o transform antes da closure para evitar acesso a componente destruído
        var goTransform = go.transform;

        // Helper interno para criar/atualizar filho de layer
        UnityEngine.UI.Image EnsureLayer(string childName, UnityEngine.Sprite sprite, UnityEngine.Color cor, int siblingIndex)
        {
            var child = goTransform.Find(childName);
            if (child == null)
            {
                var newGO = new GameObject(childName);
                newGO.transform.SetParent(goTransform, false);
                child = newGO.transform;
            }
            child.SetSiblingIndex(siblingIndex);
            var img = child.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = child.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = cor;
            img.pixelsPerUnitMultiplier = 1f;
            img.raycastTarget = false;
            var rt = child.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return img;
        }
        EnsureLayer("Border",   spriteBorder   ?? spriteBg, corBorder,              0);
        var bgImg = EnsureLayer("Bg",           spriteBg,   corBg,                  1);
        EnsureLayer("Light",    spriteLight    ?? spriteBg, new Color(1,1,1,0.25f), 2);
        EnsureLayer("Gradient", spriteGradient ?? spriteBg, new Color(0,0,0,0.20f), 3);
        var btn = go.GetComponent<UnityEngine.UI.Button>();
        if (btn != null) btn.targetGraphic = bgImg;
        if (labelTexto != null)
        {
            var labelTr = go.transform.Find("Label");
            if (labelTr != null)
            {
                var tmp = labelTr.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null) { tmp.text = labelTexto; labelTr.SetAsLastSibling(); }
            }
        }
        EditorUtility.SetDirty(go);
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

    [MenuItem("Solengard/Aplicar Layers Botão JOGAR")]
    static void AplicarLayersPlayButton()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogWarning("[Layers] Canvas não encontrado."); return; }
        var playBtn = canvas.transform.Find("PlayButton");
        if (playBtn == null) { Debug.LogWarning("[Layers] PlayButton não encontrado."); return; }
        ApplyButtonLayers(playBtn.gameObject, Hex("#8B2535"), Hex("#3A0A15"), "> JOGAR");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Solengard", "✓ Layers aplicadas no PlayButton.", "OK");
    }

    [MenuItem("Solengard/Skin MainMenu (Fase 1)")]
    static void SkinMainMenuFase1()
    {
        string BTN = BTN_GUIPRO;
        int total = 0;

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogWarning("[Skin] Canvas não encontrado — abra a cena MainMenu."); return; }
        var canvasTr = canvas.transform;

        void Skin(Transform parent, string child, Color cor)
        {
            if (parent == null) return;
            var t = parent.Find(child);
            if (t != null) { SkinElement(t.gameObject, BTN, cor); total++; }
            else Debug.LogWarning($"[Skin] Não encontrado: {parent.name}/{child}");
        }

        // ── PlayButton (vinho, já validado) ──
        Skin(canvasTr, "PlayButton", Hex("#8B2535"));

        // ── BottomTabs — cor neutra escura bonita nos 4 visíveis ──
        var tabs = canvasTr.Find("BottomTabs");
        if (tabs != null)
            foreach (var tab in new[] { "TabLoja", "TabMissoes", "TabPasse", "TabConfigs" })
                Skin(tabs, tab, Hex("#1A1A2E"));

        // ── PainelLoja ──
        var loja = canvasTr.Find("PainelLoja");
        if (loja != null)
        {
            // Abas da loja — roxo escuro neutro
            var abas = loja.Find("AbasLoja");
            if (abas != null)
                foreach (var b in new[] { "BtnPersonagens", "BtnUpgrades", "BtnDiamantes" })
                    Skin(abas, b, Hex("#2A1060"));

            // Botões comprar classe — roxo primário
            var abaPers = loja.Find("AbaPersonagens");
            if (abaPers != null)
                foreach (Transform card in abaPers)
                    Skin(card, "BtnComprar", Hex("#5A1090"));

            // Botões comprar upgrade — roxo secundário
            var abaUp = loja.Find("AbaUpgrades");
            if (abaUp != null)
                foreach (Transform child in abaUp)
                    if (child.name.StartsWith("UpRow_"))
                        Skin(child, "BtnUpgrade", Hex("#2A1060"));

            // Botões comprar diamante — dourado
            var abaDia = loja.Find("AbaDiamantes");
            if (abaDia != null)
            {
                foreach (Transform card in abaDia)
                    if (card.name.StartsWith("CardPacote_"))
                        Skin(card, "BtnPacote", Hex("#B8860B"));
                // BtnVideo — verde discreto
                Skin(abaDia, "BtnVideo", Hex("#1A5020"));
            }
        }

        // ── PopupRecompensa / Coletar — dourado ──
        var popup = canvasTr.Find("PopupRecompensa");
        if (popup != null) Skin(popup, "BotaoColetarRecompensa", Hex("#B8860B"));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Skin] MainMenu Fase 1: {total} botões skinados.");
        EditorUtility.DisplayDialog("Solengard — Skin MainMenu", $"✓ {total} botões skinados com GUI Pro.", "OK");
    }

    static void SkinPanel(GameObject host, Color corBg)
    {
        if (host == null) { Debug.LogWarning("[SkinPanel] host nulo."); return; }
        const string POPUP = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_Component_Popups/Popup_02_White.prefab";
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(POPUP);
        if (prefab == null) { Debug.LogWarning($"[SkinPanel] Prefab não encontrado: {POPUP}"); return; }

        // Remove skin anterior (idempotente)
        var antigo = host.transform.Find("[PanelSkin]");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

        // Desativa Image do root do painel (a skin assume o fundo)
        var hostImg = host.GetComponent<UnityEngine.UI.Image>();
        if (hostImg != null) { hostImg.sprite = null; hostImg.color = new Color(0,0,0,0); hostImg.enabled = false; }

        // Instancia o popup como filho de FUNDO
        var skin = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, host.transform);
        skin.name = "[PanelSkin]";

        var srt = skin.GetComponent<RectTransform>();
        if (srt == null) srt = skin.AddComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        srt.localScale = Vector3.one;
        srt.SetAsFirstSibling(); // fundo atrás de todo o conteúdo

        // Raycast off em tudo da skin (não bloqueia interação do conteúdo)
        foreach (var img in skin.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            img.raycastTarget = false;

        // Recolore o Bg
        var bgTr = skin.transform.Find("Bg");
        if (bgTr != null)
        {
            var bgImg = bgTr.GetComponent<UnityEngine.UI.Image>();
            if (bgImg != null) bgImg.color = corBg;
        }

        // Remove textos demo do prefab (Text_Title, Text_Description)
        foreach (var tmp in skin.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            Object.DestroyImmediate(tmp.gameObject);

        // Remove Content_Demo se existir
        var demoTr = skin.transform.Find("Content_Demo");
        if (demoTr != null) Object.DestroyImmediate(demoTr.gameObject);

        UnityEditor.EditorUtility.SetDirty(host);
    }

    [MenuItem("Solengard/Legacy (NAO USAR)/Construir Config (destrutivo)")]
    static void PopularPainelConfiguracoes()
    {
        if (!EditorUtility.DisplayDialog(
            "Gerador destrutivo (legado)",
            "Apaga e recria TODO o conteúdo do PainelConfiguracoes, sobrescrevendo o que já está " +
            "assado na cena (que agora é a fonte da verdade).\n\nUse apenas como referência histórica " +
            "ou para regenerar do zero.\n\nContinuar mesmo assim?",
            "Sim, rodar mesmo assim", "Cancelar")) return;
        const string SLIDER_PREFAB = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_Component_Slider/Slider_Basic_Rectangle_White.prefab";
        const string FLAG_BASE = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Icon_Flag/";
        string BTN = BTN_GUIPRO;

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogWarning("[Config] Canvas não encontrado."); return; }
        var painelTr = canvas.transform.Find("PainelConfiguracoes");
        if (painelTr == null) { Debug.LogWarning("[Config] PainelConfiguracoes não encontrado."); return; }
        var painel = painelTr.gameObject;

        for (int i = painel.transform.childCount - 1; i >= 0; i--)
        {
            var c = painel.transform.GetChild(i);
            if (c.name != "[PanelSkin]") Object.DestroyImmediate(c.gameObject);
        }

        var content = new GameObject("ConfigContent", typeof(RectTransform));
        content.transform.SetParent(painel.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.07f, 0.05f);
        crt.anchorMax = new Vector2(0.93f, 0.93f);
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

        float y = 1f;

        RectTransform NovaLinha(string nome, float alturaFrac, float gap, ref float cursorY)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(content.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, cursorY - alturaFrac);
            rt.anchorMax = new Vector2(1, cursorY);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            cursorY -= (alturaFrac + gap);
            return rt;
        }

        void RecolorSlider(GameObject sliderGO)
        {
            var slider = sliderGO.GetComponent<UnityEngine.UI.Slider>();
            if (slider == null) return;
            slider.interactable = true;

            // Garante que o root do slider tem Image com raycast (área clicável)
            var rootImg = sliderGO.GetComponent<UnityEngine.UI.Image>();
            if (rootImg == null) rootImg = sliderGO.AddComponent<UnityEngine.UI.Image>();
            rootImg.color = Hex("#1A1A3A");
            rootImg.raycastTarget = true; // CRÍTICO: captura o clique/drag

            // Fill roxo
            if (slider.fillRect != null)
            {
                var fillImg = slider.fillRect.GetComponent<UnityEngine.UI.Image>();
                if (fillImg != null) { fillImg.color = Hex("#C8A0FF"); fillImg.raycastTarget = false; }
            }

            // Background filho (se houver)
            var bgTr = sliderGO.transform.Find("Background");
            if (bgTr != null)
            {
                var bi = bgTr.GetComponent<UnityEngine.UI.Image>();
                if (bi != null) { bi.color = Hex("#1A1A3A"); bi.raycastTarget = true; }
            }

            // Cria handle visual se não existir (thumb dourado)
            if (slider.handleRect == null)
            {
                var handle = new GameObject("Handle", typeof(RectTransform));
                handle.transform.SetParent(sliderGO.transform, false);
                var hrt = handle.GetComponent<RectTransform>();
                hrt.sizeDelta = new Vector2(28, 36);
                hrt.anchorMin = new Vector2(0, 0.5f);
                hrt.anchorMax = new Vector2(0, 0.5f);
                var himg = handle.AddComponent<UnityEngine.UI.Image>();
                himg.color = Hex("#FFD700");
                himg.raycastTarget = false;
                slider.handleRect = hrt;
                slider.targetGraphic = himg;
            }
        }

        void LinhaAudio(string nome, string label, float alturaFrac, ref float cursorY)
        {
            var linha = NovaLinha(nome, alturaFrac, 0.015f, ref cursorY);

            var lbl = AddLabel(linha.gameObject, label, 28f, Color.white);
            var lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0); lblRt.anchorMax = new Vector2(0.28f, 1);
            lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            lbl.alignment = TMPro.TextAlignmentOptions.Left;

            // Switch construído do zero (Toggle puro funciona; prefab Switch_White bloqueia)
            // Usa sprites do GUI Pro como decoração visual
            const string SWITCH_SPRITE_BASE = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/UI_Etc/";
            {
                var swGO = new GameObject(nome + "_Switch", typeof(RectTransform));
                swGO.transform.SetParent(linha, false);
                var swrt = swGO.GetComponent<RectTransform>();
                swrt.anchorMin = new Vector2(0.30f, 0.15f); swrt.anchorMax = new Vector2(0.46f, 0.85f);
                swrt.offsetMin = Vector2.zero; swrt.offsetMax = Vector2.zero;

                // Background do switch (Image + raycast pra capturar clique)
                var bgImg = swGO.AddComponent<UnityEngine.UI.Image>();
                var bgSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SWITCH_SPRITE_BASE + "Switch_Bg_White_Bg.png");
                if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.type = UnityEngine.UI.Image.Type.Sliced; }
                bgImg.color = Hex("#2A1060");
                bgImg.raycastTarget = true;

                // Toggle no root (como o teste que funcionou)
                var toggle = swGO.AddComponent<UnityEngine.UI.Toggle>();
                toggle.interactable = true;
                toggle.isOn = true;
                toggle.targetGraphic = bgImg;

                // Handle (thumb dourado que desliza)
                var handle = new GameObject("Handle", typeof(RectTransform));
                handle.transform.SetParent(swGO.transform, false);
                var hrt = handle.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(0.55f, 0.1f); hrt.anchorMax = new Vector2(0.95f, 0.9f);
                hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
                var handleImg = handle.AddComponent<UnityEngine.UI.Image>();
                var handleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SWITCH_SPRITE_BASE + "Switch_Handle_On.png");
                if (handleSprite != null) handleImg.sprite = handleSprite;
                handleImg.color = Color.white;
                handleImg.raycastTarget = false;
                // NÃO setar toggle.graphic — deixa o UpdateSwitchVisual controlar a visibilidade manualmente
                toggle.graphic = null;
                // Toggle Transition None pra Unity não interferir no visual
                toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.None;
            }

            var sliderPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SLIDER_PREFAB);
            if (sliderPrefab != null)
            {
                var sl = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(sliderPrefab, linha);
                sl.name = nome + "_Slider";
                var slrt = sl.GetComponent<RectTransform>();
                slrt.anchorMin = new Vector2(0.50f, 0.3f); slrt.anchorMax = new Vector2(1f, 0.7f);
                slrt.offsetMin = Vector2.zero; slrt.offsetMax = Vector2.zero;
                slrt.localScale = Vector3.one;
                var slider = sl.GetComponent<UnityEngine.UI.Slider>();
                if (slider != null) { slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.8f; }
                RecolorSlider(sl);
            }
        }

        // ── TÍTULO ──
        {
            var t = NovaLinha("Titulo", 0.09f, 0.03f, ref y);
            var tmp = AddLabel(t.gameObject, "CONFIGURAÇÕES", 40f, Hex("#FFD700"));
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontStyle = TMPro.FontStyles.Bold;
        }

        // ── ÁUDIO ──
        LinhaAudio("Musica", "Música", 0.10f, ref y);
        LinhaAudio("SFX", "Efeitos", 0.10f, ref y);

        // ── IDIOMA ──
        {
            var linha = NovaLinha("LinhaIdioma", 0.11f, 0.025f, ref y);
            var go = new GameObject("BtnIdioma", typeof(RectTransform));
            go.transform.SetParent(linha, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.12f); rt.anchorMax = new Vector2(0.95f, 0.88f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            EnsureButton(go);
            AddLabel(go, "Idioma: Português", 26f, Color.white);
            var flag = new GameObject("Flag", typeof(RectTransform));
            flag.transform.SetParent(go.transform, false);
            var frt = flag.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.04f, 0.2f); frt.anchorMax = new Vector2(0.15f, 0.8f);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            var fimg = flag.AddComponent<UnityEngine.UI.Image>();
            var flagSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(FLAG_BASE + "icon_language_flag_prt.png");
            if (flagSprite != null) fimg.sprite = flagSprite;
            fimg.raycastTarget = false;
            SkinElement(go, BTN, Hex("#2A1060"));
        }

        // ── CONTA / LOGIN ──
        {
            var linha = NovaLinha("LinhaConta", 0.11f, 0.025f, ref y);
            var go = new GameObject("BtnConta", typeof(RectTransform));
            go.transform.SetParent(linha, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.12f); rt.anchorMax = new Vector2(0.95f, 0.88f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            EnsureButton(go);
            AddLabel(go, "Conta / Login", 26f, Color.white);
            SkinElement(go, BTN, Hex("#1A4A90"));
        }

        // ── RESTAURAR COMPRAS ──
        {
            var linha = NovaLinha("LinhaRestaurar", 0.11f, 0.025f, ref y);
            var go = new GameObject("BtnRestaurar", typeof(RectTransform));
            go.transform.SetParent(linha, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.12f); rt.anchorMax = new Vector2(0.95f, 0.88f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            EnsureButton(go);
            AddLabel(go, "Restaurar Compras", 26f, Color.white);
            SkinElement(go, BTN, Hex("#2A1060"));
        }

        // ── PRIVACIDADE + CRÉDITOS (lado a lado) ──
        {
            var linha = NovaLinha("LinhaInfo", 0.10f, 0.025f, ref y);
            void MiniBotao(string nome, string texto, float xMin, float xMax)
            {
                var go = new GameObject(nome, typeof(RectTransform));
                go.transform.SetParent(linha, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(xMin, 0.12f); rt.anchorMax = new Vector2(xMax, 0.88f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                EnsureButton(go);
                AddLabel(go, texto, 22f, Color.white);
                SkinElement(go, BTN, Hex("#2A1060"));
            }
            MiniBotao("BtnPrivacidade", "Privacidade", 0.05f, 0.48f);
            MiniBotao("BtnCreditos", "Créditos", 0.52f, 0.95f);
        }

        // ── BOTÃO FECHAR (X) ──
        {
            var go = new GameObject("BtnFecharConfig", typeof(RectTransform));
            go.transform.SetParent(content.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.88f, 0.93f); rt.anchorMax = new Vector2(0.99f, 1.0f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            EnsureButton(go);
            AddLabel(go, "X", 28f, Color.white);
            SkinElement(go, BTN, Hex("#8B2020"));
            var bfImg = go.GetComponent<UnityEngine.UI.Image>();
            if (bfImg != null) { bfImg.color = new Color(0,0,0,0); bfImg.enabled = true; }
        }

        // ── Garante SettingsManager na cena ──
        var smGO = GameObject.Find("SettingsManager");
        if (smGO == null)
        {
            smGO = new GameObject("SettingsManager");
            smGO.AddComponent(System.Type.GetType("Solengard.Core.SettingsManager, Assembly-CSharp"));
            Debug.Log("[Config] GameObject SettingsManager criado na cena.");
        }

        // ── Anexa ConfigUIBinder ao painel ──
        var binderType = System.Type.GetType("Solengard.UI.ConfigUIBinder, Assembly-CSharp");
        if (binderType != null)
        {
            if (painel.GetComponent(binderType) == null)
            {
                painel.AddComponent(binderType);
                Debug.Log("[Config] ConfigUIBinder anexado ao PainelConfiguracoes.");
            }
        }
        else Debug.LogWarning("[Config] Tipo ConfigUIBinder não encontrado.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Config] PainelConfiguracoes populado (refinado).");
        EditorUtility.DisplayDialog("Solengard", "✓ Config refinada construída.", "OK");
    }

    static GameObject CriarBotaoFechar(Transform painelPai, string nomeBotao = "BtnFechar")
    {
        var existente = painelPai.Find(nomeBotao);
        if (existente != null) return existente.gameObject;

        var go = new GameObject(nomeBotao, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        go.transform.SetParent(painelPai, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-35f, -45f);
        rt.sizeDelta        = new Vector2(75f, 75f);

        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.color = Hex("#8B2535");
        img.raycastTarget = true;
        SkinElement(go, BTN_GUIPRO, Hex("#8B2020"));
        // SkinElement desativa a Image do host — reabilita transparente para o Button receber raycasts
        img.color = new Color(0, 0, 0, 0);
        img.enabled = true;

        var txtGO = new GameObject("X", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "X"; tmp.fontSize = 40;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.raycastTarget = false;

        var tipoFechar = System.Type.GetType("Solengard.UI.BotaoFecharPainel, Assembly-CSharp");
        if (tipoFechar != null && go.GetComponent(tipoFechar) == null)
            go.AddComponent(tipoFechar);

        return go;
    }

    // ── MenuButtonAction (Passo 4) ───────────────────────────────────────────────
    // Anexa MenuButtonAction a um botao existente (idempotente) e define acao+parametro.
    // Substitui as lambdas-no-Editor que nao serializavam (bug #1). NAO recria nem
    // reposiciona o botao — apenas adiciona/atualiza o componente.
    static void WireMenuButton(GameObject btnGO, Solengard.UI.MenuAction acao, string parametro = "")
    {
        if (btnGO == null) return;
        var btn = btnGO.GetComponent<UnityEngine.UI.Button>();
        if (btn != null) btn.onClick.RemoveAllListeners(); // limpa listener antigo (lambda nao serializava de qualquer forma)
        var mba = btnGO.GetComponent<Solengard.UI.MenuButtonAction>()
                  ?? btnGO.AddComponent<Solengard.UI.MenuButtonAction>();
        mba.acao      = acao;
        mba.parametro = parametro;
        EditorUtility.SetDirty(mba);
    }

    // Comando NAO-destrutivo: religa os botoes da Loja (cards de classe, pacotes, video)
    // com MenuButtonAction na cena ATUAL, SEM recriar/reposicionar nada. Preserva os
    // ajustes manuais de tamanho/posicao. Rodar uma vez e salvar a cena.
    [MenuItem("Solengard/Loja: Religar Botoes (MenuButtonAction)")]
    static void ReligarBotoesLoja()
    {
        if (!ValidateScene(MAIN_MENU_SCENE)) return;
        var canvas     = GameObject.Find("Canvas");
        var painelLoja = canvas != null ? canvas.transform.Find("PainelLoja") : null;
        if (painelLoja == null) { EditorUtility.DisplayDialog("Solengard", "Canvas/PainelLoja nao encontrado na cena.", "OK"); return; }

        var log = new StringBuilder();
        int n = 0;

        var abaPers = painelLoja.Find("AbaPersonagens");
        if (abaPers != null)
        {
            foreach (var (id, _, _) in LojaController.GetClasses())
            {
                var btn = abaPers.Find($"CardClasse_{id}/BtnComprar");
                if (btn != null) { WireMenuButton(btn.gameObject, Solengard.UI.MenuAction.ComprarClasse, id); log.AppendLine($"  ComprarClasse -> {id}"); n++; }
                else log.AppendLine($"  AUSENTE: CardClasse_{id}/BtnComprar");
            }
        }
        else log.AppendLine("  AUSENTE: AbaPersonagens");

        var abaDia = painelLoja.Find("AbaDiamantes");
        if (abaDia != null)
        {
            var pacotes = LojaController.GetPacotes();
            for (int i = 0; i < pacotes.Length; i++)
            {
                var btn = abaDia.Find($"CardPacote_{i}/BtnPacote");
                if (btn != null) { WireMenuButton(btn.gameObject, Solengard.UI.MenuAction.ComprarPacote, pacotes[i].productId); log.AppendLine($"  ComprarPacote -> {pacotes[i].productId}"); n++; }
                else log.AppendLine($"  AUSENTE: CardPacote_{i}/BtnPacote");
            }
            var vbtn = abaDia.Find("BtnVideo");
            if (vbtn != null) { WireMenuButton(vbtn.gameObject, Solengard.UI.MenuAction.AssistirVideo); log.AppendLine("  AssistirVideo -> BtnVideo"); n++; }
            else log.AppendLine("  AUSENTE: BtnVideo");
        }
        else log.AppendLine("  AUSENTE: AbaDiamantes");

        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Solengard — Religar Botoes Loja",
            $"{n} botao(oes) religado(s) com MenuButtonAction (sem recriar/reposicionar):\n\n{log}", "OK");
    }

    [MenuItem("Solengard/Loja: Religar Botoes (MenuButtonAction)", validate = true)]
    static bool ValidateReligarBotoesLoja() =>
        EditorSceneManager.GetActiveScene().name == MAIN_MENU_SCENE;

    [MenuItem("Solengard/DEBUG Botao Config")]
    static void DebugBotaoConfig()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[DEBUG] Canvas não encontrado na cena."); return; }

        // ── BotaoConfiguracoes ──────────────────────────────────────────────
        var topBar = canvas.transform.Find("TopBar");
        var cfgTr  = topBar?.Find("BotaoConfiguracoes");
        if (cfgTr == null) { Debug.LogError("[DEBUG] BotaoConfiguracoes não encontrado em Canvas/TopBar."); return; }

        var btn = cfgTr.GetComponent<Button>();
        var img = cfgTr.GetComponent<Image>();
        Debug.Log($"[DEBUG] BotaoConfiguracoes encontrado.\n" +
                  $"  Button: {(btn != null ? "OK" : "AUSENTE")}\n" +
                  $"  Button.interactable: {btn?.interactable}\n" +
                  $"  Image: {(img != null ? "OK" : "AUSENTE")}\n" +
                  $"  Image.sprite: {(img?.sprite != null ? img.sprite.name : "NULL ← PROBLEMA")}\n" +
                  $"  Image.raycastTarget: {img?.raycastTarget}\n" +
                  $"  sizeDelta: {cfgTr.GetComponent<RectTransform>()?.sizeDelta}");

        // ── MainMenuManager ─────────────────────────────────────────────────
        var mmm = UnityEngine.Object.FindFirstObjectByType<MainMenuManager>();
        if (mmm == null) { Debug.LogError("[DEBUG] MainMenuManager não encontrado na cena."); return; }

        var so = new UnityEditor.SerializedObject(mmm);
        var fBtn   = so.FindProperty("botaoConfiguracoes");
        var fPanel = so.FindProperty("painelConfiguracoes");
        Debug.Log($"[DEBUG] MainMenuManager campos:\n" +
                  $"  botaoConfiguracoes → {(fBtn?.objectReferenceValue != null ? fBtn.objectReferenceValue.name : "NULL ← PROBLEMA")}\n" +
                  $"  painelConfiguracoes → {(fPanel?.objectReferenceValue != null ? fPanel.objectReferenceValue.name : "NULL ← PROBLEMA")}");

        // ── Ícones ──────────────────────────────────────────────────────────
        var gearSp = LoadIcon("icon_config.png");
        var gemSp  = LoadIcon("icon_diamante.png");
        Debug.Log($"[DEBUG] Sprites carregados:\n" +
                  $"  icon_config.png  → {(gearSp != null ? gearSp.name : "NULL ← spriteMode errado")}\n" +
                  $"  icon_diamante.png → {(gemSp != null ? gemSp.name : "NULL ← spriteMode errado")}");

        Debug.Log("[DEBUG] Concluído. Verifique o Console acima.");
    }

    // ── Passo 6: Validador NAO-destrutivo do MainMenu ────────────────────────────
    // Percorre a cena e REPORTA problemas. 100% read-only: nenhum SetRect, AddComponent,
    // Destroy ou SetDirty. So le e loga. Secoes:
    //   A) refs nulas no MainMenuManager
    //   B) botoes da Loja sem MenuButtonAction / acao invalida
    //   C) invariante de raycast (host Image dos botoes + camadas de skin)
    //   D) singletons de sistema presentes na cena (ausencia = info, bootstrap cria em runtime)
    //   E) layout da Loja: valores que o codigo geraria vs a cena viva (PASS/DIFF aritmetico)
    [MenuItem("Solengard/Validar MainMenu (read-only)")]
    static void ValidarMainMenu()
    {
        if (!ValidateScene(MAIN_MENU_SCENE)) return;
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { EditorUtility.DisplayDialog("Solengard — Validar MainMenu", "Canvas nao encontrado na cena.", "OK"); return; }
        var canvasTr = canvas.transform;

        var sb = new StringBuilder("══════ VALIDAR MAINMENU (read-only) ══════\n");
        int issues = 0;
        void Fail(string msg) { issues++; sb.AppendLine("  DIFF " + msg); }
        void Ok(string msg)   { sb.AppendLine("  OK   " + msg); }
        void Info(string msg) { sb.AppendLine("  --   " + msg); }

        bool Approx(Vector2 a, Vector2 b) => Mathf.Abs(a.x - b.x) < 0.5f && Mathf.Abs(a.y - b.y) < 0.5f;
        string V(Vector2 v) => $"({v.x},{v.y})";

        // ── A) Refs nulas no MainMenuManager ──────────────────────────────────
        // Refs conhecidas como pendentes de feature futura: campo NULL aqui e
        // intencional (a feature ainda nao existe), entao reportamos como INFO (--)
        // e NAO contamos como problema. Qualquer OUTRO campo null continua DIFF.
        var refsPendentes = new System.Collections.Generic.Dictionary<string, string>
        {
            { "botaoRanking", "feature futura: Ranking depende de backend online" },
        };
        sb.AppendLine("\n[A] MainMenuManager — refs serializadas:");
        var mmm = UnityEngine.Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (mmm == null) { Fail("MainMenuManager AUSENTE na cena."); }
        else
        {
            var so = new SerializedObject(mmm);
            var it = so.GetIterator();
            int nulls = 0;
            for (bool enter = true; it.NextVisible(enter); enter = false)
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.name == "m_Script") continue;
                if (it.objectReferenceValue != null) continue;
                if (refsPendentes.TryGetValue(it.name, out var motivo))
                    Info($"{it.name}: pendente ({motivo})");
                else { Fail($"campo NULL: {it.name}"); nulls++; }
            }
            if (nulls == 0) Ok("todas as refs preenchidas (ignorando pendencias conhecidas).");
        }

        // ── B) Botoes da Loja: MenuButtonAction + acao valida ──────────────────
        sb.AppendLine("\n[B] Botoes da Loja — MenuButtonAction:");
        var painelLoja = canvasTr.Find("PainelLoja");
        if (painelLoja == null) Fail("PainelLoja AUSENTE.");
        else
        {
            void CheckMBA(Transform btn, string path, Solengard.UI.MenuAction esperada, bool precisaParam)
            {
                if (btn == null) { Fail($"botao AUSENTE: {path}"); return; }
                var mba = btn.GetComponent<Solengard.UI.MenuButtonAction>();
                if (mba == null) { Fail($"sem MenuButtonAction: {path}"); return; }
                if (mba.acao != esperada) { Fail($"acao errada em {path}: {mba.acao} (esperado {esperada})"); return; }
                if (precisaParam && string.IsNullOrEmpty(mba.parametro)) { Fail($"parametro vazio em {path} ({esperada})"); return; }
                Ok($"{path} -> {mba.acao}{(precisaParam ? " ('" + mba.parametro + "')" : "")}");
            }
            var abaPers = painelLoja.Find("AbaPersonagens");
            foreach (var (id, _, _) in LojaController.GetClasses())
                CheckMBA(abaPers?.Find($"CardClasse_{id}/BtnComprar"), $"CardClasse_{id}/BtnComprar", Solengard.UI.MenuAction.ComprarClasse, true);
            var abaDia = painelLoja.Find("AbaDiamantes");
            var pacotes = LojaController.GetPacotes();
            for (int i = 0; i < pacotes.Length; i++)
                CheckMBA(abaDia?.Find($"CardPacote_{i}/BtnPacote"), $"CardPacote_{i}/BtnPacote", Solengard.UI.MenuAction.ComprarPacote, true);
            CheckMBA(abaDia?.Find("BtnVideo"), "BtnVideo", Solengard.UI.MenuAction.AssistirVideo, false);
        }

        // Outros MenuButtonAction na cena com acao invalida (varre tudo)
        foreach (var mba in canvas.GetComponentsInChildren<Solengard.UI.MenuButtonAction>(true))
        {
            if (mba.acao == Solengard.UI.MenuAction.Nenhuma) Fail($"MenuButtonAction com acao=Nenhuma em '{mba.name}'");
            bool compra = mba.acao == Solengard.UI.MenuAction.ComprarClasse
                       || mba.acao == Solengard.UI.MenuAction.ComprarUpgrade
                       || mba.acao == Solengard.UI.MenuAction.ComprarPacote;
            if (compra && string.IsNullOrEmpty(mba.parametro)) Fail($"compra sem parametro em '{mba.name}' ({mba.acao})");
        }

        // ── C) Invariante de raycast ───────────────────────────────────────────
        sb.AppendLine("\n[C] Invariante de raycast:");
        int raycastBad = 0;
        // C1: host Image de cada Button = enabled + raycastTarget (hitbox do Passo 5)
        foreach (var btn in canvas.GetComponentsInChildren<Button>(true))
        {
            var img = btn.GetComponent<Image>();
            if (img == null) { Info($"Button sem Image no host: '{btn.name}' (alvo de raycast e filho?)"); continue; }
            if (!img.enabled)        { Fail($"host Image DISABLED (regressao Passo 5): '{btn.name}'"); raycastBad++; }
            if (!img.raycastTarget)  { Fail($"host Image raycastTarget=false: '{btn.name}'"); raycastBad++; }
        }
        // C2: camadas de skin nunca capturam raycast
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "[Skin]" && t.name != "[PanelSkin]") continue;
            foreach (var img in t.GetComponentsInChildren<Image>(true))
                if (img.raycastTarget) { Fail($"skin capturando raycast: '{t.parent?.name}/[Skin]' filho '{img.name}'"); raycastBad++; }
        }
        if (raycastBad == 0) Ok("hosts de botao com hitbox correta + skins sem raycast.");

        // ── D) Singletons de sistema (edit mode) ───────────────────────────────
        sb.AppendLine("\n[D] Singletons na cena (ausencia = OK, SystemsBootstrap cria em runtime):");
        var sysTypes = new System.Type[]
        {
            typeof(DiamondSystem), typeof(PermanentUpgradeSystem), typeof(SeasonPassSystem),
            typeof(DailyRewardSystem), typeof(IAPSystem), typeof(AdSystem),
            typeof(AuthSystem), typeof(LocalizationManager),
        };
        foreach (var ty in sysTypes)
        {
            var inst = UnityEngine.Object.FindFirstObjectByType(ty, FindObjectsInactive.Include);
            if (inst != null) Ok($"{ty.Name}: presente na cena");
            else              Info($"{ty.Name}: ausente na cena -> criado em runtime pelo bootstrap");
        }

        // ── E) Layout da Loja: codigo (formulas) vs cena viva ──────────────────
        sb.AppendLine("\n[E] Layout da Loja — codigo (gerador) vs cena viva:");
        int layoutBad = 0;
        void CheckRect(Transform t, string name, Vector2 posCode, Vector2 sizeCode)
        {
            if (t == null) { Fail($"{name}: AUSENTE na cena"); layoutBad++; return; }
            var rt = t.GetComponent<RectTransform>();
            if (rt == null) { Fail($"{name}: sem RectTransform"); layoutBad++; return; }
            bool okP = Approx(rt.anchoredPosition, posCode);
            bool okS = Approx(rt.sizeDelta, sizeCode);
            if (okP && okS) Ok($"{name}: pos {V(posCode)} size {V(sizeCode)}");
            else { Fail($"{name}: codigo pos {V(posCode)} size {V(sizeCode)} | cena pos {V(rt.anchoredPosition)} size {V(rt.sizeDelta)}"); layoutBad++; }
        }
        if (painelLoja != null)
        {
            // Personagens — mesmas formulas do gerador (linhas 464/469/472)
            float cW = 320f, cH = 270f, padX = 80f, padY = 50f;
            var abaPers = painelLoja.Find("AbaPersonagens");
            var classes = LojaController.GetClasses();
            for (int i = 0; i < classes.Length; i++)
            {
                int col = i % 2, row = i / 2;
                float x = col == 0 ? -(cW / 2 + padX / 2) : (cW / 2 + padX / 2);
                float y = 0f - row * (cH + padY);
                CheckRect(abaPers?.Find($"CardClasse_{classes[i].id}"), $"CardClasse_{classes[i].id}", new Vector2(x, y), new Vector2(cW, cH));
            }
            // Diamantes — formulas do gerador (linhas 541/576)
            var abaDia = painelLoja.Find("AbaDiamantes");
            float py = 80f;
            var pacotes = LojaController.GetPacotes();
            for (int i = 0; i < pacotes.Length; i++)
                CheckRect(abaDia?.Find($"CardPacote_{i}"), $"CardPacote_{i}", new Vector2(0, py - i * 240f), new Vector2(500, 200));
            CheckRect(abaDia?.Find("BtnVideo"), "BtnVideo", new Vector2(0, py - pacotes.Length * 240f), new Vector2(500, 100));
        }
        if (layoutBad == 0 && painelLoja != null) sb.AppendLine("  => layout Loja SINCRONIZADO (codigo == cena)");

        // ── Resumo ─────────────────────────────────────────────────────────────
        sb.AppendLine($"\n══════ RESULTADO: {(issues == 0 ? "PASS — nenhum problema" : issues + " problema(s) encontrado(s)")} ══════");
        if (issues == 0) Debug.Log(sb.ToString());
        else             Debug.LogWarning(sb.ToString());
        EditorUtility.DisplayDialog("Solengard — Validar MainMenu",
            issues == 0 ? "PASS — nenhum problema encontrado.\nVeja o relatorio completo no Console."
                        : $"{issues} problema(s) encontrado(s).\nVeja os DIFF no Console (warning).", "OK");
    }

    [MenuItem("Solengard/Validar MainMenu (read-only)", validate = true)]
    static bool ValidarMainMenu_Validate() => EditorSceneManager.GetActiveScene().name == MAIN_MENU_SCENE;
}
