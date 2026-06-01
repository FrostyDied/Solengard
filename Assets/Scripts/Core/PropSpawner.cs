using System.Collections.Generic;
using UnityEngine;

public class PropSpawner : MonoBehaviour
{
    public static PropSpawner Instance { get; private set; }

    [Header("Densidade")]
    [SerializeField] int   maxProps           = 12;
    [SerializeField] float spawnRadiusMin     = 18f;
    [SerializeField] float spawnRadiusMax     = 26f;
    [SerializeField] float despawnRadius      = 32f;
    [SerializeField] float minSpacing         = 8f;
    [SerializeField] float spawnCheckInterval = 1f;

    // Tint escuro azulado — dá tom sombrio dark fantasy sem perder legibilidade
    static readonly Color DARK_TINT = new Color(0.55f, 0.58f, 0.70f);

    Transform        _player;
    List<GameObject> _activeProps  = new();
    List<Sprite>     _propSprites  = new(); // com colisão
    List<Sprite>     _decorSprites = new(); // decorativos sem colisão
    float            _timer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadRealProps();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        PrePopulate();
    }

    void Update()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _timer = spawnCheckInterval;
        CleanupDistantProps();
        TrySpawnProp();
    }

    void LoadRealProps()
    {
#if UNITY_EDITOR
        // ── Bloqueantes (colisão) ─────────────────────────────────────────────────
        // Rochas Season3 — excluir variante _no_shadow (duplicada, sem sombra)
        LoadFolder("Assets/Art/Environment/Season3_Grassland/Rocks/PNG/Objects_separately",
                   _propSprites, exclude: "_no_shadow");

        // Ruínas Season3
        LoadFolder("Assets/Art/Environment/Season3_Grassland/Ruins/PNG/Assets", _propSprites);

        // Árvores Season2
        LoadFolder("Assets/Art/Environment/Season2_Forest/Trees/PNG/Assets_separately/Trees",
                   _propSprites);

        // Ossos e criaturas Season6 (Undead)
        LoadFolder("Assets/Art/Environment/Season6_Undead/Objects/PNG/Objects_separately",
                   _propSprites);

        // Objetos amaldiçoados Season7 (Cursed)
        LoadFolder("Assets/Art/Environment/Season7_Cursed/Objects/PNG/Objects_separately",
                   _propSprites);

        // ── Decorativos (sem colisão) ─────────────────────────────────────────────
        // Cogumelos e objetos pequenos Season2
        LoadFolder("Assets/Art/Environment/Season2_Forest/Objects/PNG/Assets_no_shadow",
                   _decorSprites);

        // ── Pastas mistas: filtrar por nome de arquivo ────────────────────────────
        string[] mixedFolders = {
            "Assets/Art/Environment/Season3_Grassland/Tileset/PNG/Objects_separated",
            "Assets/Art/Environment/Season2_Forest/Bushes/PNG/Assets",
        };
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", mixedFolders);
        foreach (var g in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            var spr  = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr == null) continue;
            string fname  = System.IO.Path.GetFileName(path).ToLower();
            bool isDecor  = fname.StartsWith("bush") || fname.StartsWith("autumn_bush");
            (isDecor ? _decorSprites : _propSprites).Add(spr);
        }

        Debug.Log($"[PropSpawner] {_propSprites.Count} props bloqueantes, {_decorSprites.Count} decorativos carregados.");
#endif
    }

#if UNITY_EDITOR
    void LoadFolder(string folder, List<Sprite> target, string exclude = null)
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        foreach (var g in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            if (exclude != null && path.Contains(exclude)) continue;
            var spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null) target.Add(spr);
        }
    }
#endif

    // Distribui props sombrios em anel ao redor do player antes do primeiro frame.
    void PrePopulate()
    {
        Vector3 center = _player != null ? _player.position : Vector3.zero;
        for (int i = 0; i < maxProps; i++)
        {
            float angle = (Mathf.PI * 2f / maxProps) * i + Random.Range(-0.2f, 0.2f);
            float dist  = Random.Range(spawnRadiusMin, spawnRadiusMax);
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
            SpawnPropAt(pos);
        }
    }

    void TrySpawnProp()
    {
        if (_activeProps.Count >= maxProps) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = Random.Range(spawnRadiusMin, spawnRadiusMax);
        Vector3 pos = _player.position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
        SpawnPropAt(pos);
    }

    void SpawnPropAt(Vector3 pos)
    {
        foreach (var prop in _activeProps)
        {
            if (prop == null) continue;
            if (Vector3.Distance(prop.transform.position, pos) < minSpacing) return;
        }

        // 10% chance: cristal sombrio pulsante (puramente procedural)
        if (Random.value < 0.10f)
        {
            var crystal = SpawnCrystal(pos);
            crystal.transform.SetParent(transform);
            _activeProps.Add(crystal);
            return;
        }

        var go = CreateProp(pos);
        if (go == null) return;
        go.transform.SetParent(transform);
        _activeProps.Add(go);
    }

    GameObject CreateProp(Vector3 pos)
    {
        bool   isDecor = _decorSprites.Count > 0 && Random.value < 0.35f;
        Sprite sprite  = isDecor
            ? _decorSprites[Random.Range(0, _decorSprites.Count)]
            : (_propSprites.Count > 0 ? _propSprites[Random.Range(0, _propSprites.Count)] : null);
        if (sprite == null) return null;

        var go    = new GameObject(isDecor ? "Decor" : "Obstacle");
        go.transform.position = pos;

        var sr       = go.AddComponent<SpriteRenderer>();
        sr.sprite    = sprite;
        sr.color     = DARK_TINT;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        if (!isDecor)
        {
            float spriteH  = sprite.bounds.size.y;
            float spriteW  = sprite.bounds.size.x;
            var col        = go.AddComponent<CircleCollider2D>();
            col.radius     = Mathf.Max(0.3f, spriteW * 0.25f);
            col.offset     = new Vector2(0f, -spriteH * 0.35f);
            var rb         = go.AddComponent<Rigidbody2D>();
            rb.bodyType    = RigidbodyType2D.Static;
            int layer      = LayerMask.NameToLayer("Obstacle");
            if (layer >= 0) go.layer = layer;

            // Olhos vermelhos pulsantes em objetos altos (~20%)
            if (spriteH > 1.5f && Random.value < 0.20f)
                AddRedEyes(go, spriteH);
        }

        return go;
    }

    // Cristal sombrio — losango brilhante azul, procedural simples
    GameObject SpawnCrystal(Vector3 pos)
    {
        var go    = new GameObject("Crystal");
        go.transform.position = pos;
        var sr    = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCrystalSprite();
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);
        go.AddComponent<PulsingGlow>();
        return go;
    }

    static Sprite MakeCrystalSprite()
    {
        const int size = 16;
        var tex   = new Texture2D(size, size);
        var clear = new Color(0, 0, 0, 0);
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                tex.SetPixel(i, j, clear);

        var glow = new Color(0.4f,  0.7f,  1f,   0.3f);
        var mid  = new Color(0.5f,  0.8f,  1f,   0.9f);
        var core = new Color(0.85f, 0.95f, 1f,   1f  );

        int cx = size / 2, cy = size / 2;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                int d = Mathf.Abs(x - cx) + Mathf.Abs(y - cy);
                if      (d <= 2) tex.SetPixel(x, y, core);
                else if (d <= 4) tex.SetPixel(x, y, mid);
                else if (d <= 6) tex.SetPixel(x, y, glow);
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        var spr  = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        spr.name = "Crystal";
        return spr;
    }

    void AddRedEyes(GameObject parent, float spriteH)
    {
        var eyes = new GameObject("RedEyes");
        eyes.transform.SetParent(parent.transform);
        eyes.transform.localPosition = new Vector3(0f, spriteH * 0.3f, 0f);
        var sr       = eyes.AddComponent<SpriteRenderer>();
        sr.sprite    = MakeRedEyesSprite();
        sr.sortingOrder = 60;
        eyes.AddComponent<PulsingGlow>();
    }

    static Sprite MakeRedEyesSprite()
    {
        var tex = new Texture2D(8, 4);
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 4; j++)
                tex.SetPixel(i, j, new Color(0, 0, 0, 0));

        var red = new Color(1f, 0.15f, 0.1f, 1f);
        tex.SetPixel(1, 2, red); tex.SetPixel(2, 2, red);
        tex.SetPixel(5, 2, red); tex.SetPixel(6, 2, red);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 8, 4), new Vector2(0.5f, 0.5f), 16f);
    }

    void CleanupDistantProps()
    {
        for (int i = _activeProps.Count - 1; i >= 0; i--)
        {
            if (_activeProps[i] == null) { _activeProps.RemoveAt(i); continue; }
            if (Vector3.Distance(_activeProps[i].transform.position, _player.position) > despawnRadius)
            {
                Destroy(_activeProps[i]);
                _activeProps.RemoveAt(i);
            }
        }
    }
}
