using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SolengardDebugTools
{
    [MenuItem("Solengard/Debug/Preview Sheet Rows")]
    static void PreviewSheetRows()
    {
        string sheet = "Assets/Art/Characters/Hero/Level1-3/PNG/Swordsman_lvl1/Without_shadow/Swordsman_lvl1_Walk_without_shadow.png";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().ToList();
        if (sprites.Count == 0)
        {
            Debug.LogError("[PreviewSheetRows] Nenhum sub-sprite. O sheet está importado como Multiple? Rode 'Solengard → Setup Sprites' primeiro.");
            return;
        }

        var rows = new List<List<Sprite>>();
        foreach (var s in sprites.OrderByDescending(s => s.rect.y))
        {
            var row = rows.FirstOrDefault(r => Mathf.Abs(r[0].rect.y - s.rect.y) < 6f);
            if (row == null) { row = new List<Sprite>(); rows.Add(row); }
            row.Add(s);
        }

        var old = GameObject.Find("SheetRowPreview");
        if (old != null) Object.DestroyImmediate(old);
        var parent = new GameObject("SheetRowPreview");

        Debug.Log($"[PreviewSheetRows] === {rows.Count} linhas encontradas no sheet de Walk ===");
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i].OrderBy(s => s.rect.x).ToList();
            Debug.Log($"  Linha {i} (Y={row[0].rect.y:F0}): {row.Count} frames — primeiro frame: {row[0].name}");

            var go = new GameObject($"Linha_{i}_Y{row[0].rect.y:F0}");
            go.transform.SetParent(parent.transform);
            go.transform.position = new Vector3(0, -i * 2.5f, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = row[0];
            sr.transform.localScale = Vector3.one * 3f;
        }

        Selection.activeGameObject = parent;
        Debug.Log("[PreviewSheetRows] === Veja na Scene view: qual linha mostra o personagem de PERFIL (lateral)? Informe o índice 0-N de cima para baixo ===");
    }
}
