using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SolengardHeroAnimSetup
{
    const string HERO_ROOT  = "Assets/Art/Characters/Hero";
    const string PREFAB_DIR = "Assets/Prefabs/Heroes";
    const float  ROW_Y_TOL  = 6f;
    const float  MIN_SIZE   = 8f;

    struct HeroEntry
    {
        public string classId;
        public string prefabName;
        public string folder;
        public string idleFile;
        public string walkFile;
        public string attackFile;
        public string hurtFile;
        public string deathFile;
        public bool   multiRowSheet;  // true = Guerreiro: sheets têm várias linhas (direções)
        public float  worldScale;
    }

    static readonly HeroEntry[] _entries =
    {
        new HeroEntry {
            classId      = "warrior",
            prefabName   = "Hero_Warrior",
            folder       = HERO_ROOT + "/Cavaleiro/Level1-3/PNG/Swordsman_lvl1/Without_shadow",
            idleFile     = "Swordsman_lvl1_Idle_without_shadow.png",
            walkFile     = "Swordsman_lvl1_Walk_without_shadow.png",
            attackFile   = "Swordsman_lvl1_attack_without_shadow.png",   // 'a' minúsculo
            hurtFile     = "Swordsman_lvl1_Hurt_without_shadow.png",
            deathFile    = "Swordsman_lvl1_Death_without_shadow.png",
            multiRowSheet = true,
            worldScale   = 1.0f,
        },
        new HeroEntry {
            classId      = "mage",
            prefabName   = "Hero_Mage",
            folder       = HERO_ROOT + "/Mago/Lightning Mage",           // espaço no nome
            idleFile     = "Idle.png",
            walkFile     = "Walk.png",
            attackFile   = "Attack_1.png",
            hurtFile     = "Hurt.png",
            deathFile    = "Dead.png",
            multiRowSheet = false,
            worldScale   = 0.32f,
        },
        new HeroEntry {
            classId      = "assassin",
            prefabName   = "Hero_Assassin",
            folder       = HERO_ROOT + "/Assassino/Assassino",
            idleFile     = "Idle.png",
            walkFile     = "Walk.png",
            attackFile   = "Attack.png",
            hurtFile     = "Hurt.png",
            deathFile    = "Dead.png",
            multiRowSheet = false,
            worldScale   = 1.0f,
        },
        new HeroEntry {
            classId      = "necromancer",
            prefabName   = "Hero_Necromancer",
            folder       = HERO_ROOT + "/Necromante/Necromante",
            idleFile     = "Idle.png",
            walkFile     = "Walk.png",
            attackFile   = "Attack_1.png",
            hurtFile     = "Hurt.png",
            deathFile    = "Dead.png",
            multiRowSheet = false,
            worldScale   = 0.32f,
        },
        new HeroEntry {
            classId      = "paladin",
            prefabName   = "Hero_Paladin",
            folder       = HERO_ROOT + "/Paladino/Paladino",
            idleFile     = "Idle.png",
            walkFile     = "Walk.png",
            attackFile   = "Attack 1.png",   // espaço no nome do arquivo
            hurtFile     = "Hurt.png",
            deathFile    = "Dead.png",
            multiRowSheet = false,
            worldScale   = 0.32f,
        },
        new HeroEntry {
            classId      = "hunter",
            prefabName   = "Hero_Hunter",
            folder       = HERO_ROOT + "/Caçador/Caçador",    // ç = ç
            idleFile     = "Idle.png",
            walkFile     = "Walk.png",
            attackFile   = "Attack.png",
            hurtFile     = "Hurt.png",
            deathFile    = "Dead.png",
            multiRowSheet = false,
            worldScale   = 1.0f,
        },
    };

    [MenuItem("Solengard/Classes/Setup Hero Animations")]
    static void SetupHeroAnimations()
    {
        if (!AssetDatabase.IsValidFolder(PREFAB_DIR))
        {
            Directory.CreateDirectory(PREFAB_DIR);
            AssetDatabase.Refresh();
        }

        int done = 0;
        foreach (var e in _entries)
        {
            var idle   = LoadFrames(e, e.idleFile);
            var walk   = LoadFrames(e, e.walkFile);
            var attack = LoadFrames(e, e.attackFile);
            var hurt   = LoadFrames(e, e.hurtFile);
            var death  = LoadFrames(e, e.deathFile);

            Debug.Log($"[HeroAnim] {e.classId}: idle={idle.Length} walk={walk.Length} attack={attack.Length} hurt={hurt.Length} death={death.Length}");

            string prefabPath = $"{PREFAB_DIR}/{e.prefabName}.prefab";
            EnsurePrefabExists(prefabPath, e.prefabName, e.worldScale);

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var anim = contents.GetComponent<CharacterAnimator>()
                        ?? contents.AddComponent<CharacterAnimator>();
                var so = new SerializedObject(anim);
                SetSpriteArray(so, "idleFrames",   idle);
                SetSpriteArray(so, "walkFrames",   walk);
                SetSpriteArray(so, "attackFrames", attack);
                SetSpriteArray(so, "hurtFrames",   hurt);
                SetSpriteArray(so, "deathFrames",  death);
                so.ApplyModifiedProperties();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                done++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[HeroAnim] Concluído — {done}/{_entries.Length} prefabs configurados em {PREFAB_DIR}");
        EditorUtility.DisplayDialog("Hero Animations", $"{done} heroes configurados.", "OK");
    }

    // ── Criação de prefab novo ────────────────────────────────────────────────

    static void EnsurePrefabExists(string prefabPath, string goName, float worldScale)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        var go = new GameObject(goName);
        go.tag = "Player";
        go.transform.localScale = Vector3.one * worldScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Characters";

        go.AddComponent<CharacterAnimator>();

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType    = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius    = 0.35f;
        col.isTrigger = false;

        go.AddComponent<PlayerController>();
        go.AddComponent<PlayerHealth>();
        go.AddComponent<PlayerAttack>();

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"[HeroAnim] Prefab criado: {prefabPath}");
    }

    // ── Carregamento de frames ────────────────────────────────────────────────

    static Sprite[] LoadFrames(HeroEntry e, string filename)
    {
        if (string.IsNullOrEmpty(filename)) return new Sprite[0];

        string path = e.folder + "/" + filename;
        var all = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(s => s.rect.width >= MIN_SIZE && s.rect.height >= MIN_SIZE)
            .ToList();

        if (all.Count == 0)
        {
            Debug.LogWarning($"[HeroAnim] {e.classId}: nenhum sprite em '{path}'");
            return new Sprite[0];
        }

        if (e.multiRowSheet)
            return ExtractTopRow(all);

        // Animação de uma linha: ordenar por nome (Idle_0, Idle_1, ...)
        return all.OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();
    }

    // Extrai apenas a primeira linha (Y mais alto) de um sheet multi-direcional.
    static Sprite[] ExtractTopRow(List<Sprite> sprites)
    {
        var rows = new List<List<Sprite>>();
        foreach (var s in sprites.OrderByDescending(s => s.rect.y))
        {
            var row = rows.FirstOrDefault(r => Mathf.Abs(r[0].rect.y - s.rect.y) < ROW_Y_TOL);
            if (row == null) { row = new List<Sprite>(); rows.Add(row); }
            row.Add(s);
        }
        return rows[0].OrderBy(s => s.rect.x).ToArray();
    }

    // ── Utilitário ────────────────────────────────────────────────────────────

    static void SetSpriteArray(SerializedObject so, string propName, Sprite[] sprites)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) return;
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
