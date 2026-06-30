using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace Solengard.EditorTools
{
    /// <summary>
    /// Gera FontAssets no formato TextCore (UnityEngine.TextCore.Text.FontAsset),
    /// que o UI Toolkit aceita via -unity-font-definition no USS.
    /// Os assets do TMP Font Asset Creator (TMPro.TMP_FontAsset) NÃO servem pro UITK
    /// (warning "Unsupported type TMP_FontAsset ... only Font, FontAsset").
    ///
    /// Modo Dynamic: glifos renderizados sob demanda a partir do .ttf de origem,
    /// então acentos do português (Ç, ã, õ, …) aparecem sem precisar pré-bakear.
    /// </summary>
    public static class CreateTextCoreFontAssets
    {
        struct FontJob
        {
            public string ttfPath;
            public string outPath;
            public string assetName;
        }

        static readonly FontJob[] Jobs =
        {
            new FontJob {
                ttfPath   = "Assets/Art/Fonts/Cinzel/Cinzel-Bold.ttf",
                outPath   = "Assets/Art/Fonts/Cinzel/Cinzel-Bold UITK.asset",
                assetName = "Cinzel-Bold UITK",
            },
            new FontJob {
                ttfPath   = "Assets/Art/Fonts/AlegreyaSans/AlegreyaSans-Regular.ttf",
                outPath   = "Assets/Art/Fonts/AlegreyaSans/AlegreyaSans-Regular UITK.asset",
                assetName = "AlegreyaSans-Regular UITK",
            },
            new FontJob {
                ttfPath   = "Assets/Art/Fonts/AlegreyaSans/AlegreyaSans-Bold.ttf",
                outPath   = "Assets/Art/Fonts/AlegreyaSans/AlegreyaSans-Bold UITK.asset",
                assetName = "AlegreyaSans-Bold UITK",
            },
        };

        // Parâmetros do atlas SDF
        const int SamplingPointSize = 90;
        const int AtlasPadding      = 9;   // margem p/ outline/glow do título
        const int AtlasWidth        = 1024;
        const int AtlasHeight       = 1024;

        [MenuItem("Solengard/Fontes/Gerar TextCore SDF (UI Toolkit)")]
        public static void Generate()
        {
            int ok = 0;
            var report = new List<string>();

            foreach (var job in Jobs)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(job.ttfPath);
                if (font == null)
                {
                    Debug.LogError($"[Fontes] .ttf não encontrado: {job.ttfPath}");
                    continue;
                }

                // Regenera limpo se o asset já existir (idempotente)
                if (AssetDatabase.LoadAssetAtPath<FontAsset>(job.outPath) != null)
                    AssetDatabase.DeleteAsset(job.outPath);

                FontAsset fontAsset = FontAsset.CreateFontAsset(
                    font,
                    SamplingPointSize,
                    AtlasPadding,
                    GlyphRenderMode.SDFAA,
                    AtlasWidth,
                    AtlasHeight,
                    AtlasPopulationMode.Dynamic,
                    enableMultiAtlasSupport: true);

                if (fontAsset == null)
                {
                    Debug.LogError($"[Fontes] Falha ao criar FontAsset de {job.ttfPath}");
                    continue;
                }

                fontAsset.name = job.assetName;

                AssetDatabase.CreateAsset(fontAsset, job.outPath);

                // Persistir os sub-assets (atlas texture(s) + material) dentro do .asset
                if (fontAsset.atlasTextures != null)
                {
                    for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                    {
                        var tex = fontAsset.atlasTextures[i];
                        if (tex == null) continue;
                        tex.name = $"{job.assetName} Atlas {i}";
                        tex.hideFlags = HideFlags.HideInHierarchy;
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }

                if (fontAsset.material != null)
                {
                    fontAsset.material.name = $"{job.assetName} Material";
                    fontAsset.material.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }

                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(job.outPath, ImportAssetOptions.ForceUpdate);

                string guid = AssetDatabase.AssetPathToGUID(job.outPath);
                report.Add($"  {job.assetName,-26}  guid: {guid}");
                ok++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Fontes] TextCore FontAssets gerados: {ok}/{Jobs.Length}\n" +
                      string.Join("\n", report) +
                      "\n→ Próximo passo: trocar os url()/GUIDs em common.uss para esses assets.");
        }
    }
}
