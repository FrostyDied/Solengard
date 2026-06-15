using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore;

// Cria um TMP Sprite Asset a partir do icon_diamante.png com um sprite nomeado "diamante"
// e o registra como fallback do sprite asset default (EmojiOne), de modo que a tag
// <sprite name="diamante"> funcione em QUALQUER TextMeshPro sem wiring por componente.
public static class SolengardDiamanteSprite
{
    const string ICON_PATH   = "Assets/Art/UI/Icons/icon_diamante.png";
    const string ASSET_PATH  = "Assets/Art/UI/Icons/Diamante SDF.asset";
    const string SPRITE_NAME = "diamante";

    [MenuItem("Solengard/Setup/Criar Sprite Asset do Diamante")]
    static void CreateDiamanteSpriteAsset()
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
        var sprite  = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (texture == null || sprite == null)
        {
            Debug.LogError($"[DiamanteSprite] Não achei textura/sprite em: {ICON_PATH}");
            return;
        }

        int w = texture.width, h = texture.height;
        Debug.Log($"[DiamanteSprite] Textura {w}x{h} carregada.");

        // Sprite asset
        var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        spriteAsset.name = "Diamante";
        spriteAsset.spriteSheet = texture;

        // Carimba a versão ANTES de UpdateLookupTables. Sem isso, o TMP vê "material != null +
        // versão vazia", trata como asset legado e chama UpgradeSpriteAsset() — que itera
        // spriteInfoList (null aqui → NRE) e zera as tabelas. O campo m_Version é 'internal',
        // então gravamos via SerializedObject (campo serializado → persiste no asset salvo, logo
        // o mesmo check no Awake() em runtime também passa e o ícone não some ao recarregar).
        var verSO = new SerializedObject(spriteAsset);
        verSO.FindProperty("m_Version").stringValue = "1.1.0";
        verSO.ApplyModifiedProperties();

        // Glyph: ocupa a textura inteira (sprite único). BearingY ~0.9*altura para sentar na baseline.
        var glyph = new TMP_SpriteGlyph
        {
            index     = 0,
            metrics   = new GlyphMetrics(w, h, 0f, h * 0.9f, w),
            glyphRect = new GlyphRect(0, 0, w, h),
            scale     = 1f,
            atlasIndex = 0,
            sprite    = sprite,
        };
        spriteAsset.spriteGlyphTable.Add(glyph);

        // Character "diamante" (unicode 0x1F48E = 💎, bônus: também mapeia o emoji literal).
        var character = new TMP_SpriteCharacter(0x1F48E, glyph) { name = SPRITE_NAME, scale = 1f };
        spriteAsset.spriteCharacterTable.Add(character);

        // Material com o shader de sprite do TMP.
        var mat = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "Diamante Material" };
        mat.SetTexture(ShaderUtilities.ID_MainTex, texture);
        spriteAsset.material = mat;

        spriteAsset.UpdateLookupTables();

        // Grava o asset + material como sub-objeto.
        var existing = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(ASSET_PATH);
        if (existing != null) AssetDatabase.DeleteAsset(ASSET_PATH);
        AssetDatabase.CreateAsset(spriteAsset, ASSET_PATH);
        AssetDatabase.AddObjectToAsset(mat, spriteAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ASSET_PATH);

        // Confirma que a versão ficou PERSISTIDA no disco (recarrega do zero). Se vier vazia,
        // o Awake() em runtime dispararia o upgrade e zeraria as tabelas → ícone sumiria.
        var reloaded = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(ASSET_PATH);
        if (reloaded == null || string.IsNullOrEmpty(reloaded.version))
            Debug.LogError("[DiamanteSprite] FALHA: m_Version não persistiu no asset salvo " +
                           "(Awake() faria upgrade e zeraria as tabelas em runtime).");
        else
            Debug.Log($"[DiamanteSprite] Versão persistida no asset: {reloaded.version} " +
                      $"({reloaded.spriteCharacterTable.Count} sprite(s) na tabela).");

        // Registra como fallback do sprite asset default (pra <sprite name="diamante"> ser global).
        var def = TMP_Settings.defaultSpriteAsset;
        if (def == null)
        {
            Debug.LogWarning("[DiamanteSprite] TMP_Settings.defaultSpriteAsset é null — " +
                             "asset criado, mas a tag só funcionará com spriteAsset atribuído manualmente.");
        }
        else
        {
            if (def.fallbackSpriteAssets == null)
                def.fallbackSpriteAssets = new System.Collections.Generic.List<TMP_SpriteAsset>();
            if (!def.fallbackSpriteAssets.Contains(spriteAsset))
            {
                def.fallbackSpriteAssets.Add(spriteAsset);
                EditorUtility.SetDirty(def);
                AssetDatabase.SaveAssets();
                Debug.Log($"[DiamanteSprite] Registrado como fallback de '{def.name}'.");
            }
            else
            {
                Debug.Log($"[DiamanteSprite] Já estava no fallback de '{def.name}'.");
            }
        }

        Debug.Log($"[DiamanteSprite] OK → {ASSET_PATH}. Use <sprite name=\"{SPRITE_NAME}\"> no texto.");
        EditorGUIUtility.PingObject(spriteAsset);
    }
}
