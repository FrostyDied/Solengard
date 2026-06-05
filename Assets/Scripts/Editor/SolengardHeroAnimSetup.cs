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
        public bool   multiRowSheet;
        public float  worldScale;
        // Projétil
        public string projectileFile;
        public int    projectileMaxFrame;   // -1 = todos os frames; >=0 = frames 0..N
        public bool   projectileIsStatic;   // true → salvar em projectileSprite (frame único)
        public float  projectileScale;
        public string projectileNameFilter; // opcional: só aceita sprites cujo nome contém este texto
    }

    static readonly HeroEntry[] _entries =
    {
        new HeroEntry {
            classId           = "warrior",
            prefabName        = "Hero_Warrior",
            folder            = HERO_ROOT + "/Cavaleiro/Level1-3/PNG/Swordsman_lvl1/Without_shadow",
            idleFile          = "Swordsman_lvl1_Idle_without_shadow.png",
            walkFile          = "Swordsman_lvl1_Walk_without_shadow.png",
            attackFile        = "Swordsman_lvl1_attack_without_shadow.png",
            hurtFile          = "Swordsman_lvl1_Hurt_without_shadow.png",
            deathFile         = "Swordsman_lvl1_Death_without_shadow.png",
            multiRowSheet     = true,
            worldScale        = 1.0f,
            projectileScale   = 1f,
        },
        new HeroEntry {
            classId           = "mage",
            prefabName        = "Hero_Mage",
            folder            = HERO_ROOT + "/Mago/Lightning Mage",
            idleFile          = "Idle.png",
            walkFile          = "Walk.png",
            attackFile        = "Attack_1.png",
            hurtFile          = "Hurt.png",
            deathFile         = "Dead.png",
            multiRowSheet     = false,
            worldScale        = 0.32f,
            projectileFile       = "Light_ball.png",
            projectileMaxFrame  = -1,
            projectileIsStatic  = false,
            projectileScale     = 2.0f,
            projectileNameFilter = "Light_ball",
        },
        new HeroEntry {
            classId           = "assassin",
            prefabName        = "Hero_Assassin",
            folder            = HERO_ROOT + "/Assassino/Assassino",
            idleFile          = "Idle.png",
            walkFile          = "Walk.png",
            attackFile        = "Attack.png",
            hurtFile          = "Hurt.png",
            deathFile         = "Dead.png",
            multiRowSheet     = false,
            worldScale        = 1.0f,
            projectileScale   = 1f,
        },
        new HeroEntry {
            classId           = "necromancer",
            prefabName        = "Hero_Necromancer",
            folder            = HERO_ROOT + "/Necromante/Necromante",
            idleFile          = "Idle.png",
            walkFile          = "Walk.png",
            attackFile        = "Attack_1.png",
            hurtFile          = "Hurt.png",
            deathFile         = "Dead.png",
            multiRowSheet     = false,
            worldScale        = 0.32f,
            projectileFile    = "Spear.png",
            projectileMaxFrame = 4,
            projectileIsStatic = false,
            projectileScale   = 2.5f,
        },
        new HeroEntry {
            classId           = "paladin",
            prefabName        = "Hero_Paladin",
            folder            = HERO_ROOT + "/Paladino/Paladino",
            idleFile          = "Idle.png",
            walkFile          = "Walk.png",
            attackFile        = "Attack 1.png",
            hurtFile          = "Hurt.png",
            deathFile         = "Dead.png",
            multiRowSheet     = false,
            worldScale        = 0.32f,
            projectileScale   = 1f,
        },
        new HeroEntry {
            classId            = "hunter",
            prefabName         = "Hero_Hunter",
            folder             = HERO_ROOT + "/Caçador/Caçador",
            idleFile           = "Idle.png",
            walkFile           = "Walk.png",
            attackFile         = "Attack.png",
            hurtFile           = "Hurt.png",
            deathFile          = "Dead.png",
            multiRowSheet      = false,
            worldScale         = 1.0f,
            projectileFile     = "Charge.png",
            projectileMaxFrame = 0,
            projectileIsStatic = true,
            projectileScale    = 3.0f,
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

            string defPath = $"Assets/Resources/Classes/{e.classId}.asset";
            var classDef = AssetDatabase.LoadAssetAtPath<ClassDefinition>(defPath);
            if (classDef != null)
            {
                var cso = new SerializedObject(classDef);
                SetSpriteArray(cso, "idleFrames",   idle);
                SetSpriteArray(cso, "walkFrames",   walk);
                SetSpriteArray(cso, "attackFrames", attack);
                SetSpriteArray(cso, "hurtFrames",   hurt);
                SetSpriteArray(cso, "deathFrames",  death);

                // Projétil
                var projFrames = LoadProjectileFrames(e);
                if (e.projectileIsStatic && projFrames.Length > 0)
                {
                    cso.FindProperty("projectileSprite").objectReferenceValue = projFrames[0];
                    SetSpriteArray(cso, "projectileFrames", new Sprite[0]);
                }
                else if (projFrames.Length > 0)
                {
                    SetSpriteArray(cso, "projectileFrames", projFrames);
                    cso.FindProperty("projectileSprite").objectReferenceValue = null;
                }
                cso.FindProperty("projectileScale").floatValue = e.projectileScale;

                cso.ApplyModifiedProperties();
                EditorUtility.SetDirty(classDef);
                Debug.Log($"[HeroAnim] ClassDefinition {e.classId} frames atualizados (projFrames={projFrames.Length})");
            }
            else
            {
                Debug.LogWarning($"[HeroAnim] ClassDefinition não encontrado: {defPath}");
            }
        }

        AssetDatabase.SaveAssets();
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

        return all.OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();
    }

    static Sprite[] LoadProjectileFrames(HeroEntry e)
    {
        if (string.IsNullOrEmpty(e.projectileFile)) return new Sprite[0];

        string path = e.folder + "/" + e.projectileFile;
        var all = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(s => string.IsNullOrEmpty(e.projectileNameFilter) || s.name.Contains(e.projectileNameFilter))
            .OrderBy(s => s.name, System.StringComparer.Ordinal)
            .ToList();

        if (all.Count == 0)
        {
            Debug.LogWarning($"[HeroAnim] {e.classId}: nenhum sprite de projétil em '{path}' (filtro='{e.projectileNameFilter}')");
            return new Sprite[0];
        }

        if (e.projectileMaxFrame >= 0 && e.projectileMaxFrame < all.Count - 1)
            all = all.Take(e.projectileMaxFrame + 1).ToList();

        return all.ToArray();
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
