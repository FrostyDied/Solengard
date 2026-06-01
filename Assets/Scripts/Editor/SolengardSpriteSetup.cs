using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SolengardSpriteSetup
{
    // FASE 6 — PPU 32 para todos os personagens garante movimento subpixel suave.
    // CHARACTER_SCALE serve de referência: com PPU 32 e scale 1.8 o herói ocupa ~10% da tela (orthoSize 14).
    private const float CHARACTER_PPU   = 32f;
    private const float CHARACTER_SCALE = 1.8f;

    private struct CategoryCount
    {
        public int hero, enemies, environment, effects, ui, other;
        public int Total => hero + enemies + environment + effects + ui + other;
    }

    [MenuItem("Solengard/Setup Sprites")]
    public static void SetupAllSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Solengard Sprite Setup", "No textures found in Assets/Art.", "OK");
            return;
        }

        var counts = new CategoryCount();
        var loggedCategories = new HashSet<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            float progress = (float)i / guids.Length;
            bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                "Solengard Sprite Setup",
                $"Configurando: {System.IO.Path.GetFileName(path)}",
                progress);

            if (cancelled)
                break;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            ConfigureImporter(importer, path, ref counts, loggedCategories);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Solengard Sprite Setup — Concluído",
            $"Herói: {counts.hero}  |  Inimigos: {counts.enemies}  |  " +
            $"Ambiente: {counts.environment}  |  Efeitos: {counts.effects}  |  " +
            $"UI: {counts.ui}  |  Outros: {counts.other}\n\nTotal: {counts.Total} sprites configurados.",
            "OK");
    }

    private static void ConfigureImporter(
        TextureImporter importer,
        string path,
        ref CategoryCount counts,
        HashSet<string> loggedCategories)
    {
        // Part 1 — base settings
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        // Character sheets can be 768 px+ wide; 512 would downsample them. UI/environment stay smaller.
        bool isCharacter = ContainsAny(path, "Characters", "Enemies", "Hero", "Boss");
        importer.maxTextureSize = isCharacter ? 2048 : 512;
        importer.alphaIsTransparency = true;
        importer.wrapMode            = TextureWrapMode.Clamp;

        // Tileset sprites need Repeat wrap + FullRect mesh for SpriteDrawMode.Tiled
        if (Contains(path, "Tileset"))
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);
            ts.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(ts);
        }

        // "Spritesheets" = boss packed sheets; "With_shadow"/"Without_shadow" = per-animation
        // composite sheets; "/Parts/" = individual body-part animation sheets.
        // Animation-state-named files (Idle.png, Walk.png, etc.) in /Characters/ are also sheets
        // (e.g. DarkElf uses simple state filenames instead of the Without_shadow pattern).
        string filenameLower = path.Substring(path.LastIndexOf('/') + 1);
        int dotPos = filenameLower.LastIndexOf('.');
        if (dotPos > 0) filenameLower = filenameLower.Substring(0, dotPos);
        filenameLower = filenameLower.ToLowerInvariant();
        bool isAnimStateFile = ContainsAny(filenameLower, "idle", "walk", "run", "attack", "hurt", "death", "dead", "dying");

        bool isSpritesheet = path.IndexOf("Spritesheets",   System.StringComparison.Ordinal) >= 0
                          || path.IndexOf("With_shadow",    System.StringComparison.Ordinal) >= 0
                          || path.IndexOf("Without_shadow", System.StringComparison.Ordinal) >= 0
                          || path.IndexOf("/Parts/",        System.StringComparison.Ordinal) >= 0
                          || (isAnimStateFile && Contains(path, "/Characters/"));
        importer.spriteImportMode = isSpritesheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;

        // Part 2 — pixelsPerUnit by path
        (string category, int ppu) = ResolveRule(path);

        importer.spritePixelsPerUnit = ppu;

        if (!loggedCategories.Contains(category))
        {
            Debug.Log($"[SolengardSpriteSetup] Regra aplicada — {category}: {ppu} PPU");
            loggedCategories.Add(category);
        }

        importer.SaveAndReimport();

        // Tally
        switch (category)
        {
            case string c when c.StartsWith("Herói"):       counts.hero++;        break;
            case string c when c.StartsWith("Inimigo"):     counts.enemies++;     break;
            case string c when c.StartsWith("Boss"):        counts.enemies++;     break;
            case string c when c.StartsWith("Ambiente"):    counts.environment++; break;
            case string c when c.StartsWith("Efeitos"):     counts.effects++;     break;
            case string c when c.StartsWith("UI"):          counts.ui++;          break;
            default:                                         counts.other++;       break;
        }
    }

    private static (string category, int ppu) ResolveRule(string path)
    {
        // Hero
        if (ContainsAny(path, "Swordsman_lvl1", "Swordsman_lvl2", "Swordsman_lvl3"))
            return ("Herói Lv1-3", 32);
        if (ContainsAny(path, "Swordsman_lvl4", "Swordsman_lvl5", "Swordsman_lvl6"))
            return ("Herói Lv4-6", 32);
        if (ContainsAny(path, "Swordsman_lvl7", "Swordsman_lvl8", "Swordsman_lvl9"))
            return ("Herói Lv7-9", 28);

        // Small enemies
        if (Contains(path, "Slime"))     return ("Inimigo Slime",    48);
        if (Contains(path, "Zombie"))    return ("Inimigo Zombie",   32);
        if (Contains(path, "Goblin"))    return ("Inimigo Goblin",   40);
        if (Contains(path, "Skeleton"))  return ("Inimigo Skeleton", 32);
        if (Contains(path, "Ghost"))     return ("Inimigo Ghost",    36);
        if (Contains(path, "DarkElf"))   return ("Inimigo DarkElf",  32);

        // Medium enemies — raised to CHARACTER_PPU for smooth subpixel movement
        if (Contains(path, "Gnoll")) return ("Inimigo Gnoll", (int)CHARACTER_PPU);
        if (Contains(path, "Orc"))   return ("Inimigo Orc",   (int)CHARACTER_PPU);
        if (Contains(path, "Demon")) return ("Inimigo Demon",  (int)CHARACTER_PPU);

        // Large enemies — raised to CHARACTER_PPU (scale compensates for size)
        if (Contains(path, "Golem")) return ("Inimigo Golem", (int)CHARACTER_PPU);
        if (Contains(path, "Lich"))  return ("Inimigo Lich",  (int)CHARACTER_PPU);

        // Boss — raised to CHARACTER_PPU; scale in prefab adjusts visual size
        if (ContainsAny(path, "Caveman", "Giant", "Viking", "Boss"))
            return ("Boss", (int)CHARACTER_PPU);

        // Environment
        if (Contains(path, "Tileset"))                                         return ("Ambiente Tileset",  16);
        if (ContainsAny(path, "Objects", "Props"))                             return ("Ambiente Objects",  32);
        if (ContainsAny(path, "Trees", "Bushes", "Rocks", "Ruins", "Crystals")) return ("Ambiente Nature", 32);

        // Effects
        if (ContainsAny(path, "Effects", "Explosion", "Fire", "Magic"))
            return ("Efeitos", 32);

        // UI
        if (ContainsAny(path, "UI", "Icons", "Weapons"))
            return ("UI", 1);

        return ("Default", 32);
    }

    private static bool Contains(string path, string keyword) =>
        path.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool ContainsAny(string path, params string[] keywords)
    {
        foreach (string kw in keywords)
            if (Contains(path, kw)) return true;
        return false;
    }
}
