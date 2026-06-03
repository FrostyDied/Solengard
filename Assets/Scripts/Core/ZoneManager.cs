using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [System.Serializable]
    public class ZoneConfig
    {
        public string              nome;
        public BiomeSystem.Biome   biome;
        public float durationSeconds  = 600f;
        public float bossSpawnAt      = 480f;
        public float bossTimeLimit    = 120f;
        public float hpMultiplier     = 1f;
        public float speedMultiplier  = 1f;
        public float damageMultiplier = 1f;
        public int   spawnMax         = 40;
        public float spawnInterval    = 0.5f;

        [HideInInspector] public List<GameObject> enemyPrefabs = new();
        [HideInInspector] public List<GameObject> bossPrefabs  = new();
    }

    [Header("Configuração das 5 zonas")]
    [SerializeField] ZoneConfig[] zones = new ZoneConfig[]
    {
        new ZoneConfig {
            nome="Floresta de Veremoth",   biome=BiomeSystem.Biome.Veremoth,
            hpMultiplier=1f,  speedMultiplier=1f,  damageMultiplier=1f,
            spawnMax=40,  spawnInterval=0.50f },
        new ZoneConfig {
            nome="Cavernas de Khorduum",   biome=BiomeSystem.Biome.Khorduum,
            hpMultiplier=1.8f, speedMultiplier=1.2f, damageMultiplier=1.5f,
            spawnMax=55,  spawnInterval=0.40f },
        new ZoneConfig {
            nome="Cemitério de Valdross",  biome=BiomeSystem.Biome.Valdross,
            hpMultiplier=2.8f, speedMultiplier=1.4f, damageMultiplier=2f,
            spawnMax=65,  spawnInterval=0.35f },
        new ZoneConfig {
            nome="Pântano de Gorveth",     biome=BiomeSystem.Biome.Gorveth,
            hpMultiplier=4f,  speedMultiplier=1.6f, damageMultiplier=2.8f,
            spawnMax=75,  spawnInterval=0.30f },
        new ZoneConfig {
            nome="Campo de Arkenfall",     biome=BiomeSystem.Biome.Arkenfall,
            hpMultiplier=6f,  speedMultiplier=1.8f, damageMultiplier=3.5f,
            spawnMax=85,  spawnInterval=0.25f },
    };

    public int   CurrentZone       { get; private set; } = 0;
    public float ZoneTimeRemaining { get; private set; }
    public float BossTimeRemaining { get; private set; }
    public bool  BossActive        { get; private set; }
    public bool  IsRunning         { get; private set; }
    public int   KillCount         { get; private set; }

    public static event System.Action<int>    OnZoneStarted;
    public static event System.Action<int>    OnZoneCompleted;
    public static event System.Action         OnBossSpawned;
    public static event System.Action         OnBossDefeated;
    public static event System.Action<string> OnGameOver;
    public static event System.Action         OnAllZonesCompleted;

    [Header("Modo de teste — 0 = desativado")]
    [SerializeField] float testBossSpawnAt = 0f;

    readonly int[] _quotaPerMinute = { 30, 50, 80, 110, 140, 170, 200, 240 };
    int   _currentMinute    = 0;
    int   _spawnBudget      = 0;
    bool  _heartDropped     = false;

    Transform        _player;
    List<GameObject> _activeEnemies     = new();
    List<GameObject> _bossInstances     = new();
    bool             _allBossesDefeated = false;
    bool             _bossSpawnStarted  = false;
    Coroutine        _spawnCoroutine;
    Coroutine        _quotaTimerCoroutine;
    GameObject       _fadeOverlay;
    GameObject       _victoryTextGO;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        FindPlayer();

        if (testBossSpawnAt > 0f)
            foreach (var z in zones) z.bossSpawnAt = testBossSpawnAt;

        if (Camera.main == null)
            Debug.LogWarning("[ZoneManager] Camera.main é null — verificar tag 'MainCamera' na câmera");
    }

    void FindPlayer()
    {
        if (PlayerController.Instance != null)
            _player = PlayerController.Instance.transform;
        else
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
    }

    public void StartZones()
    {
        if (IsRunning) return;
        IsRunning   = true;
        CurrentZone = 0;
        StartCoroutine(ZoneLoop());
    }

    IEnumerator ZoneLoop()
    {
        while (CurrentZone < zones.Length)
        {
            var zone = zones[CurrentZone];
            ZoneTimeRemaining  = zone.durationSeconds;
            BossActive         = false;
            _bossSpawnStarted  = false;
            _heartDropped      = false;
            _allBossesDefeated = false;
            _spawnBudget       = _quotaPerMinute[0];
            _currentMinute     = 1;

            BiomeSystem.Instance?.SetBiome(zone.biome);
            WorldChunkManager.Instance?.SetBiome(CurrentZone);
            OnZoneStarted?.Invoke(CurrentZone);
            Debug.Log($"[Zone] Iniciando zona {CurrentZone + 1}: {zone.nome}");

            EnemyBase.GlobalHPMult     = zone.hpMultiplier;
            EnemyBase.GlobalSpeedMult  = zone.speedMultiplier;
            EnemyBase.GlobalDamageMult = zone.damageMultiplier;

            _quotaTimerCoroutine = StartCoroutine(QuotaTimer());
            _spawnCoroutine      = StartCoroutine(SpawnLoop(zone));

            bool zoneCleared = false;

            while (ZoneTimeRemaining > 0f && !zoneCleared)
            {
                ZoneTimeRemaining -= Time.deltaTime;

                if (!BossActive && !_bossSpawnStarted &&
                    ZoneTimeRemaining <= zone.durationSeconds - zone.bossSpawnAt)
                {
                    _bossSpawnStarted = true;
                    Debug.Log($"[Zone] SpawnBoss iniciado UMA VEZ. _bossSpawnStarted=true");
                    StartCoroutine(SpawnBoss(zone));
                }

                if (BossActive)
                {
                    BossTimeRemaining -= Time.deltaTime;

                    if (BossTimeRemaining <= 0f)
                    {
                        ClearEnemies();
                        OnGameOver?.Invoke($"Tempo esgotado na {zone.nome}");
                        IsRunning = false;
                        yield break;
                    }
                }

                if (_allBossesDefeated)
                {
                    Debug.Log("[Zone] _allBossesDefeated=true detectado — avançando para próxima zona");
                    BossActive         = false;
                    _allBossesDefeated = false;
                    zoneCleared        = true;
                }

                yield return null;
            }

            ClearEnemies();
            OnZoneCompleted?.Invoke(CurrentZone);

            var ph = PlayerController.Instance?.GetComponent<PlayerHealth>();
            if (ph != null) ph.Curar(ph.MaxHealth);
            XPSystem.Instance?.ResetLevel();

            yield return StartCoroutine(ZoneTransition(CurrentZone));

            CurrentZone++;

            if (CurrentZone < zones.Length)
            {
                var nextConfig = BiomeSystem.Instance?.GetConfig(CurrentZone + 1);
                var loreUI     = Object.FindFirstObjectByType<LoreScreenUI>();
                if (loreUI != null && nextConfig != null)
                {
                    bool loreDone = false;
                    StartCoroutine(loreUI.ShowLore(nextConfig, () => loreDone = true));
                    yield return new WaitUntil(() => loreDone);
                }
                yield return StartCoroutine(FadeIn());
            }
        }

        IsRunning = false;
        OnAllZonesCompleted?.Invoke();
        Debug.Log("[Zone] Todas as zonas concluídas! Vitória!");
    }

    IEnumerator SpawnLoop(ZoneConfig zone)
    {
        while (true)
        {
            if (BossActive)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            if (_player == null) FindPlayer();

            if (!_heartDropped && ZoneTimeRemaining <= 300f && _player != null)
            {
                HeartDrop.SpawnAt(_player.position + (Vector3)(Random.insideUnitCircle * 5f));
                _heartDropped = true;
            }

            _activeEnemies.RemoveAll(e => e == null);
            int activeCount = _activeEnemies.Count;
            int safetyLimit = zone.spawnMax * 3;

            if (_spawnBudget > 0 && activeCount < safetyLimit && _player != null && zone.enemyPrefabs.Count > 0)
            {
                SpawnEnemy(zone);
                _spawnBudget--;
            }

            yield return new WaitForSeconds(zone.spawnInterval);
        }
    }

    IEnumerator QuotaTimer()
    {
        while (_currentMinute < _quotaPerMinute.Length)
        {
            yield return new WaitForSeconds(30f);
            if (_currentMinute < _quotaPerMinute.Length)
            {
                int quota = _quotaPerMinute[_currentMinute];
                _spawnBudget += quota;
                Debug.Log($"[Zone] Intervalo {_currentMinute + 1}: +{quota} liberados. Budget: {_spawnBudget}");
                _currentMinute++;
            }
        }
    }

    void SpawnEnemy(ZoneConfig zone)
    {
        if (zone.enemyPrefabs == null || zone.enemyPrefabs.Count == 0) return;

        var prefab = zone.enemyPrefabs[Random.Range(0, zone.enemyPrefabs.Count)];
        if (prefab == null) return;

        var enemy = Instantiate(prefab, GetSpawnPosition(), Quaternion.identity);
        var eb    = enemy.GetComponent<EnemyBase>();
        if (eb != null)
        {
            eb.OnDeathCallback = () =>
            {
                KillCount++;
                _activeEnemies.Remove(enemy);
            };
        }
        _activeEnemies.Add(enemy);
    }

    IEnumerator SpawnBoss(ZoneConfig zone)
    {
        var validBosses = zone.bossPrefabs?.Where(b => b != null).ToList() ?? new List<GameObject>();
        if (validBosses.Count == 0)
        {
            Debug.LogError($"[Zone] {zone.nome}: nenhum boss prefab configurado");
            yield break;
        }

        BossActive        = true;
        BossTimeRemaining = zone.bossTimeLimit;

        yield return new WaitForSeconds(2f);

        float angleStep = validBosses.Count > 1 ? 360f / validBosses.Count : 0f;
        for (int i = 0; i < validBosses.Count; i++)
        {
            Vector3 pos;
            if (validBosses.Count > 1)
            {
                float   angle  = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 8f;
                pos = (_player != null ? _player.position : Vector3.zero) + offset;
            }
            else
            {
                pos = GetSpawnPosition();
            }

            var bossGO = Instantiate(validBosses[i], pos, Quaternion.identity);
            var eb     = bossGO.GetComponent<EnemyBase>();
            if (eb != null)
            {
                string bossName = validBosses[i].name.ToLower();
                float scaleMultiplier, hpMultiplier;

                if (bossName.Contains("caveman") || bossName.Contains("goblin") || bossName.Contains("viking"))
                {
                    scaleMultiplier = 0.25f;
                    hpMultiplier    = 6f;
                    eb.moveSpeed   *= 1.8f;
                }
                else if (bossName.Contains("darkelf") || bossName.Contains("elf"))
                {
                    scaleMultiplier     = 1.5f;
                    hpMultiplier        = 8f;
                    eb.moveSpeed       *= 0.7f;
                    eb.stoppingDistance = 2.0f;
                    var darkElf = bossGO.GetComponent<EnemyDarkElf>();
                    if (darkElf != null) darkElf.isBoss = true;
                }
                else // EnemyGolem e outros
                {
                    scaleMultiplier = 3f;
                    hpMultiplier    = 5f;
                    eb.moveSpeed   *= 1.5f;
                }

                bossGO.transform.localScale *= scaleMultiplier;
                eb.maxHealth               *= hpMultiplier;
                eb.InitializeHealth();

                var bossRef = bossGO;
                eb.OnDeathCallback = () =>
                {
                    _bossInstances.Remove(bossRef);
                    KillCount++;
                    Debug.Log($"[Boss] Morreu. Restantes: {_bossInstances.Count}");
                    if (_bossInstances.Count == 0)
                    {
                        Debug.Log("[Boss] Todos derrotados — _allBossesDefeated = true");
                        _allBossesDefeated = true;
                        OnBossDefeated?.Invoke();
                        Debug.Log("[Zone] Todos os bosses derrotados!");
                    }
                };
            }

            var anim = bossGO.GetComponent<CharacterAnimator>();
            if (anim != null) anim.ForceState(CharacterAnimator.State.Walk);

#if UNITY_EDITOR
            string spawnedName = validBosses[i].name.ToLower();
            if ((spawnedName.Contains("darkelf") || spawnedName.Contains("elf")) && anim != null)
                LoadAndApplyElf2Frames(anim);
#endif

            if (bossGO.GetComponent<BossAttack>() == null)
                bossGO.AddComponent<BossAttack>();

            _bossInstances.Add(bossGO);
        }

        OnBossSpawned?.Invoke();
        Debug.Log($"[Zone] {validBosses.Count} boss(es) spawnados em {zone.nome}! {zone.bossTimeLimit}s para derrotá-los!");

        yield return new WaitForSeconds(1f);
        _spawnCoroutine      = StartCoroutine(SpawnLoop(zone));
        _quotaTimerCoroutine = StartCoroutine(QuotaTimer());
    }

    Vector3 GetSpawnPosition()
    {
        if (_player == null) FindPlayer();
        if (_player == null) return Vector3.zero;

        Camera cam = Camera.main;
        if (cam == null)
        {
            var cf = CameraFollow.Instance;
            if (cf != null) cam = cf.GetComponent<Camera>();
        }

        Vector3 center = cam != null ? cam.transform.position : _player.position;
        float camH     = cam != null ? cam.orthographicSize + 3f : 15f;
        float camW     = cam != null ? camH * cam.aspect    + 3f : 22f;

        float rx = Random.Range(-camW, camW);
        float ry = Random.Range(-camH, camH);
        switch (Random.Range(0, 4))
        {
            case 0:  return new Vector3(center.x + rx,    center.y + camH, 0f);
            case 1:  return new Vector3(center.x + rx,    center.y - camH, 0f);
            case 2:  return new Vector3(center.x - camW,  center.y + ry,   0f);
            default: return new Vector3(center.x + camW,  center.y + ry,   0f);
        }
    }

    void ClearEnemies()
    {
        if (_spawnCoroutine      != null) { StopCoroutine(_spawnCoroutine);      _spawnCoroutine      = null; }
        if (_quotaTimerCoroutine != null) { StopCoroutine(_quotaTimerCoroutine); _quotaTimerCoroutine = null; }

        foreach (var e in _activeEnemies) if (e != null) Destroy(e);
        _activeEnemies.Clear();

        foreach (var b in _bossInstances) if (b != null) Destroy(b);
        _bossInstances.Clear();

        foreach (var p in GameObject.FindGameObjectsWithTag("EnemyProjectile")) Destroy(p);

        foreach (var x in FindObjectsByType<XPDrop>(FindObjectsSortMode.None))   Destroy(x.gameObject);
        foreach (var h in FindObjectsByType<HeartDrop>(FindObjectsSortMode.None)) Destroy(h.gameObject);

        BossActive         = false;
        _allBossesDefeated = false;
        _spawnBudget       = 0;

        Debug.Log("[Zone] Limpeza completa — inimigos, bosses, projéteis, XP e corações destruídos");
    }

    // ── Transição cinematográfica entre zonas ────────────────────────────────────

    IEnumerator ZoneTransition(int completedZone)
    {
        yield return StartCoroutine(FadeOut());
        ShowZoneVictoryText(completedZone);
        yield return new WaitForSecondsRealtime(1.5f);
        HideZoneVictoryText();
        yield return new WaitForSecondsRealtime(0.5f);
    }

    IEnumerator FadeOut()
    {
        _fadeOverlay = CreateFadeOverlay();
        var img = _fadeOverlay.GetComponent<Image>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.5f;
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, 1f);
    }

    IEnumerator FadeIn()
    {
        if (_fadeOverlay == null) yield break;
        var img = _fadeOverlay.GetComponent<Image>();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 1.2f;
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
        Destroy(_fadeOverlay);
        _fadeOverlay = null;
    }

    GameObject CreateFadeOverlay()
    {
        var go     = new GameObject("ZoneFadeOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998;
        var img  = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        var rect = img.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go;
    }

    void ShowZoneVictoryText(int zoneIndex)
    {
        var go     = new GameObject("ZoneVictoryText");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var txt = go.AddComponent<TMPro.TextMeshProUGUI>();
        string[] zoneNames =
        {
            "FLORESTA DE VEREMOTH — CONQUISTADA",
            "CAVERNAS DE KHORDUUM — CONQUISTADAS",
            "CEMITÉRIO DE VALDROSS — CONQUISTADO",
            "PÂNTANO DE GORVETH — CONQUISTADO",
            "CAMPO DE ARKENFALL — CONQUISTADO",
        };
        txt.text      = zoneIndex < zoneNames.Length ? zoneNames[zoneIndex] : "ZONA CONQUISTADA";
        txt.fontSize  = 28f;
        txt.color     = new Color(0.78f, 0.65f, 0.20f);
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.fontStyle = TMPro.FontStyles.Bold;

        var rect = txt.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.45f);
        rect.anchorMax = new Vector2(0.9f, 0.55f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        txt.DOFade(0f, 0f).SetUpdate(true);
        txt.DOFade(1f, 0.4f).SetUpdate(true);

        _victoryTextGO = go;
    }

    void HideZoneVictoryText()
    {
        if (_victoryTextGO == null) return;
        var txt = _victoryTextGO.GetComponent<TMPro.TextMeshProUGUI>();
        if (txt != null)
            txt.DOFade(0f, 0.3f).SetUpdate(true)
               .OnComplete(() => { if (_victoryTextGO != null) Destroy(_victoryTextGO); });
        else
            Destroy(_victoryTextGO);
        _victoryTextGO = null;
    }

    // ── Utilitários ──────────────────────────────────────────────────────────────

    public void RestoreToZone(int zone)
    {
        CurrentZone = Mathf.Clamp(zone, 0, zones.Length - 1);
    }

#if UNITY_EDITOR
    static void LoadAndApplyElf2Frames(CharacterAnimator anim)
    {
        const string ELF2 = "Assets/Art/Characters/Enemies/DarkElf/Elf_2";
        if (!UnityEditor.AssetDatabase.IsValidFolder(ELF2)) return;

        string[] paths = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { ELF2 })
            .Select(UnityEditor.AssetDatabase.GUIDToAssetPath).ToArray();

        var idle   = ExtractRow(FindFile(paths, new[]{"idle"},                        new[]{"shadow_"}));
        var walk   = ExtractRow(FindFile(paths, new[]{"walk"},                        new[]{"run","attack","shadow_"}));
        var attack = ExtractRow(FindFile(paths, new[]{"attack"},                      new[]{"walk","run","shadow_"}));
        var hurt   = ExtractRow(FindFile(paths, new[]{"hurt","hit"},                  new[]{"shadow_"}));
        var death  = ExtractRow(FindFile(paths, new[]{"death","dead","dying","die"},   new[]{"shadow_"}));

        anim.OverrideFrames(idle, walk, attack, hurt, death);
    }

    static string FindFile(string[] paths, string[] include, string[] exclude)
    {
        foreach (var p in paths)
        {
            var n = System.IO.Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            if (!include.Any(k => n.Contains(k))) continue;
            if (exclude.Any(k => n.Contains(k))) continue;
            return p;
        }
        return null;
    }

    static Sprite[] ExtractRow(string sheetPath)
    {
        if (string.IsNullOrEmpty(sheetPath)) return null;
        var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>().Where(s => s.rect.width >= 8f && s.rect.height >= 8f).ToList();
        if (sprites.Count == 0) return null;
        var rows = new System.Collections.Generic.List<System.Collections.Generic.List<Sprite>>();
        foreach (var s in sprites.OrderByDescending(s => s.rect.y))
        {
            var row = rows.FirstOrDefault(r => Mathf.Abs(r[0].rect.y - s.rect.y) < 6f);
            if (row == null) { row = new System.Collections.Generic.List<Sprite>(); rows.Add(row); }
            row.Add(s);
        }
        return rows[0].OrderBy(s => s.rect.x).ToArray();
    }
#endif
}
