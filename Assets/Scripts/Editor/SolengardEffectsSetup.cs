using UnityEditor;
using UnityEngine;
using System.IO;

public static class SolengardEffectsSetup
{
    const string SLASH_SRC  = "Assets/Art/Effects/Slash_1/PNG";
    const string EFFECTS_RES = "Assets/Resources/Effects";

    [MenuItem("Solengard/Effects/Fix Slash SpriteMode")]
    static void FixSlashSpriteMode()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SLASH_SRC });
        int fixed_ = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.textureType      = TextureImporterType.Sprite;
                EditorUtility.SetDirty(importer);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                fixed_++;
            }
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Effects] Fix Slash SpriteMode: {fixed_} texturas corrigidas");
    }

    [MenuItem("Solengard/Effects/Copiar Efeitos para Resources")]
    static void CopyEffectsToResources()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(EFFECTS_RES);
        EnsureFolder($"{EFFECTS_RES}/Slash");

        int copied = 0;

        // Slash_1/PNG/1..10 → Resources/Effects/Slash/1..10
        for (int i = 1; i <= 10; i++)
        {
            string src  = $"{SLASH_SRC}/{i}";
            string dest = $"{EFFECTS_RES}/Slash/{i}";
            EnsureFolder(dest);
            copied += CopyPngsFromFolder(src, dest, fixSingle: true);
        }

        // Explosions
        var explosionMap = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Assets/Art/Effects/Explosions/PNG/Explosion",           "Explosion"       },
            { "Assets/Art/Effects/Explosions/PNG/Explosion_blue_circle","ExplosionBlue"  },
            { "Assets/Art/Effects/Explosions/PNG/Explosion_gas",       "ExplosionGas"    },
            { "Assets/Art/Effects/Explosions/PNG/Circle_explosion",    "CircleExplosion" },
            { "Assets/Art/Effects/Explosions/PNG/Smoke",               "Smoke"           },
        };

        foreach (var kv in explosionMap)
        {
            string dest = $"{EFFECTS_RES}/{kv.Value}";
            EnsureFolder(dest);
            copied += CopyPngsFromFolder(kv.Key, dest, fixSingle: true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Effects] Copiar Efeitos: {copied} arquivos copiados para {EFFECTS_RES}");
    }

    static int CopyPngsFromFolder(string srcFolder, string destFolder, bool fixSingle)
    {
        if (!AssetDatabase.IsValidFolder(srcFolder)) return 0;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { srcFolder });
        int count = 0;
        foreach (string guid in guids)
        {
            string srcPath  = AssetDatabase.GUIDToAssetPath(guid);
            if (!srcPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
            // Only direct children of srcFolder (not sub-subfolders)
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

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string name   = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
