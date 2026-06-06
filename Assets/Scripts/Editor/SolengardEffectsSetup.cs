using UnityEditor;
using UnityEngine;
using System.IO;

public static class SolengardEffectsSetup
{
    const string SLASH_SRC   = "Assets/Art/Effects/Slash_1/PNG";
    const string SLASH2_SRC  = "Assets/Art/Effects/Slash_2/slash5/png";
    const string EFFECTS_RES = "Assets/Resources/Effects";

    [MenuItem("Solengard/Effects/Fix Slash SpriteMode")]
    static void FixSlashSpriteMode()
    {
        int fixed_ = 0;

        // Slash_1 completo
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SLASH_SRC }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                fixed_ += FixToSingle(path);
        }

        // Arquivos individuais — cada PNG já é 1 frame
        string[] singlePaths =
        {
            "Assets/Art/Effects/Magic_1/1 Magic/10/10.png",
            "Assets/Art/Effects/Fire/1 Magic/3/Cycle.png",
            "Assets/Art/Effects/Fire/1 Magic/3/Start.png",
            "Assets/Art/Effects/Fire/1 Magic/3/Finish.png",
            "Assets/Art/Effects/Arrow/PNG/without background/15.png",
            "Assets/Art/Effects/Sword/PNG/Transperent/Icon39.png",
            "Assets/Art/Effects/Sword/PNG/Transperent/Icon14.png",
        };
        foreach (string path in singlePaths)
            fixed_ += FixToSingle(path);

        AssetDatabase.Refresh();
        Debug.Log($"[Effects] Fix Slash SpriteMode: {fixed_} texturas corrigidas");
    }

    [MenuItem("Solengard/Effects/Fix Slash2 SpriteMode")]
    static void FixSlash2SpriteMode()
    {
        int fixados = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SLASH2_SRC }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            fixados++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Effects] {fixados} sprites do Slash_2/slash5 corrigidos para Single");
    }

    [MenuItem("Solengard/Effects/Copiar Efeitos para Resources")]
    static void CopyEffectsToResources()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(EFFECTS_RES);
        EnsureFolder($"{EFFECTS_RES}/Slash");

        int copied = 0;

        // ── Slash_1/PNG/1..10 ────────────────────────────────────────────────────
        for (int i = 1; i <= 10; i++)
        {
            EnsureFolder($"{EFFECTS_RES}/Slash/{i}");
            copied += CopyPngsFromFolder($"{SLASH_SRC}/{i}", $"{EFFECTS_RES}/Slash/{i}", fixSingle: true);
        }

        // ── Slash_2/slash5 → Slash/Paladino ─────────────────────────────────────
        EnsureFolder($"{EFFECTS_RES}/Slash/Paladino");
        copied += CopyPngsFromFolder(SLASH2_SRC, $"{EFFECTS_RES}/Slash/Paladino", fixSingle: true);

        // ── Explosions ───────────────────────────────────────────────────────────
        var explosionMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Assets/Art/Effects/Explosions/PNG/Explosion",            "Explosion"       },
            { "Assets/Art/Effects/Explosions/PNG/Explosion_blue_circle", "ExplosionBlue"  },
            { "Assets/Art/Effects/Explosions/PNG/Explosion_gas",        "ExplosionGas"    },
            { "Assets/Art/Effects/Explosions/PNG/Circle_explosion",     "CircleExplosion" },
            { "Assets/Art/Effects/Explosions/PNG/Smoke",                "Smoke"           },
        };
        foreach (var kv in explosionMap)
        {
            EnsureFolder($"{EFFECTS_RES}/{kv.Value}");
            copied += CopyPngsFromFolder(kv.Key, $"{EFFECTS_RES}/{kv.Value}", fixSingle: true);
        }

        // ── Mago Light_charge + Necromante Special (preservar Multiple+slices) ──
        EnsureFolder($"{EFFECTS_RES}/Magic");
        EnsureFolder($"{EFFECTS_RES}/Magic/MagoImpact");
        copied += CopySingleAsset(
            "Assets/Art/Characters/Hero/Mago/Lightning Mage/Light_charge.png",
            $"{EFFECTS_RES}/Magic/MagoImpact/Light_charge.png", preserveImport: true);

        EnsureFolder($"{EFFECTS_RES}/Magic/NecromanteImpact");
        copied += CopySingleAsset(
            "Assets/Art/Characters/Hero/Necromante/Necromante/Special.png",
            $"{EFFECTS_RES}/Magic/NecromanteImpact/Special.png", preserveImport: true);

        // ── Assassino: Magic_2/2.png (Multiple + 12 slices) ─────────────────────
        EnsureFolder($"{EFFECTS_RES}/Assassino");
        copied += CopySingleAsset(
            "Assets/Art/Effects/Magic_2/1 Magic/2.png",
            $"{EFFECTS_RES}/Assassino/2.png", preserveImport: true);

        // ── Caçador Arrow ────────────────────────────────────────────────────────
        EnsureFolder($"{EFFECTS_RES}/Cacador");
        EnsureFolder($"{EFFECTS_RES}/Cacador/Arrow");
        copied += CopySingleAsset(
            "Assets/Art/Effects/Arrow/PNG/without background/15.png",
            $"{EFFECTS_RES}/Cacador/Arrow/15.png", preserveImport: false);

        // ── Guerreiro Sword ──────────────────────────────────────────────────────
        EnsureFolder($"{EFFECTS_RES}/Guerreiro");
        EnsureFolder($"{EFFECTS_RES}/Guerreiro/Sword");
        copied += CopySingleAsset(
            "Assets/Art/Effects/Sword/PNG/Transperent/Icon39.png",
            $"{EFFECTS_RES}/Guerreiro/Sword/Icon39.png", preserveImport: false);
        copied += CopySingleAsset(
            "Assets/Art/Effects/Sword/PNG/Transperent/Icon14.png",
            $"{EFFECTS_RES}/Guerreiro/Sword/Icon14.png", preserveImport: false);

        // ── Mago Attack (Magic_1/10/10.png — 1 frame, 208×72) ───────────────────
        EnsureFolder($"{EFFECTS_RES}/Mago");
        EnsureFolder($"{EFFECTS_RES}/Mago/Attack");
        copied += CopySingleAsset(
            "Assets/Art/Effects/Magic_1/1 Magic/10/10.png",
            $"{EFFECTS_RES}/Mago/Attack/10.png", preserveImport: false);

        // ── Mago Area: Fire/1 Magic/3 — renomear para garantir ordem de animação ─
        EnsureFolder($"{EFFECTS_RES}/Mago/Area");
        copied += CopySingleAsset(
            "Assets/Art/Effects/Fire/1 Magic/3/Start.png",
            $"{EFFECTS_RES}/Mago/Area/1_Start.png", preserveImport: false);
        copied += CopySingleAsset(
            "Assets/Art/Effects/Fire/1 Magic/3/Cycle.png",
            $"{EFFECTS_RES}/Mago/Area/2_Cycle.png", preserveImport: false);
        copied += CopySingleAsset(
            "Assets/Art/Effects/Fire/1 Magic/3/Finish.png",
            $"{EFFECTS_RES}/Mago/Area/3_Finish.png", preserveImport: false);

        // ── Necromante: Slash_1/PNG/2 (5 frames, 240×240) ───────────────────────
        EnsureFolder($"{EFFECTS_RES}/Necromante");
        copied += CopyPngsFromFolder("Assets/Art/Effects/Slash_1/PNG/2", $"{EFFECTS_RES}/Necromante", fixSingle: true);

        // ── Paladino: Slash_1/PNG/4 (8 frames, 240×240) ─────────────────────────
        EnsureFolder($"{EFFECTS_RES}/Paladino");
        copied += CopyPngsFromFolder("Assets/Art/Effects/Slash_1/PNG/4", $"{EFFECTS_RES}/Paladino", fixSingle: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Effects] Copiar Efeitos: {copied} arquivos copiados para {EFFECTS_RES}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    static int FixToSingle(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return 0;
        if (importer.spriteImportMode == SpriteImportMode.Single &&
            importer.textureType      == TextureImporterType.Sprite) return 0;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.textureType      = TextureImporterType.Sprite;
        EditorUtility.SetDirty(importer);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return 1;
    }

    static int CopyPngsFromFolder(string srcFolder, string destFolder, bool fixSingle)
    {
        if (!AssetDatabase.IsValidFolder(srcFolder)) return 0;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { srcFolder });
        int count = 0;
        foreach (string guid in guids)
        {
            string srcPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!srcPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
            string rel = srcPath.Substring(srcFolder.Length + 1);
            if (rel.Contains("/") || rel.Contains("\\")) continue;

            string destPath = $"{destFolder}/{Path.GetFileName(srcPath)}";
            if (!AssetDatabase.IsValidFolder(destPath) && AssetDatabase.LoadAssetAtPath<Object>(destPath) == null)
            {
                AssetDatabase.CopyAsset(srcPath, destPath);
                count++;
            }

            if (fixSingle)
            {
                var imp = AssetImporter.GetAtPath(destPath) as TextureImporter;
                if (imp != null && imp.spriteImportMode != SpriteImportMode.Single)
                {
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.textureType      = TextureImporterType.Sprite;
                    EditorUtility.SetDirty(imp);
                    AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }
        return count;
    }

    static int CopySingleAsset(string srcPath, string destPath, bool preserveImport)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(destPath) != null) return 0;
        if (!System.IO.File.Exists(Path.GetFullPath(srcPath))) return 0;

        AssetDatabase.CopyAsset(srcPath, destPath);

        if (!preserveImport)
        {
            var imp = AssetImporter.GetAtPath(destPath) as TextureImporter;
            if (imp != null)
            {
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.textureType      = TextureImporterType.Sprite;
                EditorUtility.SetDirty(imp);
                AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceUpdate);
            }
        }
        return 1;
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string name   = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
