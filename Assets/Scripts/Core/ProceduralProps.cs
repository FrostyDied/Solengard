using UnityEngine;

public static class ProceduralProps
{
    static readonly Color Stone       = new Color(0.32f, 0.30f, 0.34f);
    static readonly Color StoneDark   = new Color(0.20f, 0.19f, 0.23f);
    static readonly Color DeadWood    = new Color(0.28f, 0.22f, 0.18f);
    static readonly Color DeadWoodD   = new Color(0.16f, 0.12f, 0.10f);
    static readonly Color Bone        = new Color(0.78f, 0.76f, 0.68f);
    static readonly Color BoneDark    = new Color(0.55f, 0.53f, 0.46f);
    static readonly Color CrystalGlow = new Color(0.4f,  0.7f,  1f);

    public enum PropType { Tombstone, Cross, DeadTree, Stump, Bones, BrokenColumn, FallenPillar, Crystal }

    public static Sprite Generate(PropType type, int seed)
    {
        const int size = 32;
        var tex   = new Texture2D(size, size);
        var clear = new Color(0, 0, 0, 0);
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                tex.SetPixel(i, j, clear);

        var rng = new System.Random(seed);

        switch (type)
        {
            case PropType.Tombstone:    DrawTombstone(tex, rng);    break;
            case PropType.Cross:        DrawCross(tex, rng);        break;
            case PropType.DeadTree:     DrawDeadTree(tex, rng);     break;
            case PropType.Stump:        DrawStump(tex, rng);        break;
            case PropType.Bones:        DrawBones(tex, rng);        break;
            case PropType.BrokenColumn: DrawBrokenColumn(tex, rng); break;
            case PropType.FallenPillar: DrawFallenPillar(tex, rng); break;
            case PropType.Crystal:      DrawCrystal(tex, rng);      break;
        }

        tex.Apply();
        tex.filterMode = FilterMode.Point;

        var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.2f), 16f);
        s.name = $"Prop_{type}_{seed}";
        return s;
    }

    static void Px(Texture2D t, int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= t.width || y >= t.height) return;
        if (c.a >= 1f) { t.SetPixel(x, y, c); return; }
        Color bg = t.GetPixel(x, y);
        t.SetPixel(x, y, Color.Lerp(bg, c, c.a));
    }

    static void Rect(Texture2D t, int x0, int y0, int w, int h, Color c)
    {
        for (int x = x0; x < x0 + w; x++)
            for (int y = y0; y < y0 + h; y++)
                Px(t, x, y, c);
    }

    static void GroundShadow(Texture2D t, int cx, int w)
    {
        var shadow = new Color(0f, 0f, 0f, 0.35f);
        for (int x = cx - w; x <= cx + w; x++)
        {
            float d = Mathf.Abs(x - cx) / (float)w;
            if (d > 1f) continue;
            int hh = Mathf.RoundToInt(3 * (1 - d));
            for (int y = 1; y <= hh; y++)
                Px(t, x, y, shadow);
        }
    }

    static void DrawTombstone(Texture2D t, System.Random rng)
    {
        GroundShadow(t, 16, 8);
        Rect(t, 10, 4, 12, 18, Stone);
        Rect(t, 11, 21, 10, 3, Stone);
        Rect(t, 12, 23, 8,  2, Stone);
        Rect(t, 10, 4,  2, 18, StoneDark);
        Rect(t, 20, 4,  2, 18, StoneDark);
        for (int y = 6; y < 18; y++) Px(t, 15 + (y % 2), y, StoneDark);
        Rect(t, 15, 14, 2, 6, StoneDark);
        Rect(t, 13, 16, 6, 2, StoneDark);
    }

    static void DrawCross(Texture2D t, System.Random rng)
    {
        GroundShadow(t, 16, 6);
        Rect(t, 14, 4, 4, 22, Stone);
        Rect(t, 9, 18, 14, 4, Stone);
        Rect(t, 14, 4,  1, 22, StoneDark);
        Rect(t, 9, 18, 14,  1, StoneDark);
        if (rng.NextDouble() > 0.5) Rect(t, 17, 24, 2, 2, StoneDark);
    }

    static void DrawDeadTree(Texture2D t, System.Random rng)
    {
        GroundShadow(t, 16, 9);
        int x = 15;
        for (int y = 2; y < 22; y++)
        {
            Rect(t, x, y, 3, 1, DeadWood);
            Px(t, x, y, DeadWoodD);
            if (y % 4 == 0) x += (rng.Next(3) - 1);
        }
        DrawBranch(t, x,     20, 1,  rng);
        DrawBranch(t, x + 2, 16, -1, rng);
        DrawBranch(t, x,     12, 1,  rng);
        DrawBranch(t, x + 1, 9,  -1, rng);
    }

    static void DrawBranch(Texture2D t, int sx, int sy, int dir, System.Random rng)
    {
        int x = sx, y = sy;
        int len = 4 + rng.Next(4);
        for (int i = 0; i < len; i++)
        {
            x += dir; y += (i % 2);
            Px(t, x, y,     DeadWood);
            Px(t, x, y + 1, DeadWoodD);
        }
    }

    static void DrawStump(Texture2D t, System.Random rng)
    {
        GroundShadow(t, 16, 7);
        Rect(t, 11, 3, 10, 8, DeadWood);
        Rect(t, 11, 3, 10, 2, DeadWoodD);
        Rect(t, 12, 10, 8, 2, DeadWoodD);
        Px(t, 16, 11, DeadWood);
        Rect(t, 14, 10, 4, 1, DeadWood);
    }

    static void DrawBones(Texture2D t, System.Random rng)
    {
        GroundShadow(t, 16, 8);
        Rect(t, 8, 3, 8, 2, Bone);
        Rect(t, 7, 3, 1, 2, BoneDark);
        Rect(t, 16, 3, 1, 2, BoneDark);
        Rect(t, 14, 6, 7, 2, Bone);
        Rect(t, 18, 8, 6, 5, Bone);
        Px(t, 19, 10, StoneDark);
        Px(t, 22, 10, StoneDark);
        Rect(t, 19, 8, 4, 1, BoneDark);
    }

    static void DrawBrokenColumn(Texture2D t, System.Random rng)
    {
        GroundShadow(t, 16, 7);
        Rect(t, 12, 3, 8, 14, Stone);
        Rect(t, 12, 3, 2, 14, StoneDark);
        for (int x = 12; x < 20; x++)
            Rect(t, x, 17 + rng.Next(3), 1, 1, StoneDark);
        Px(t, 15, 8, StoneDark);
        Px(t, 17, 12, StoneDark);
    }

    static void DrawFallenPillar(Texture2D t, System.Random rng)
    {
        GroundShadow(t, 16, 10);
        Rect(t, 4, 4, 24, 6, Stone);
        Rect(t, 4, 4, 24, 2, StoneDark);
        Rect(t, 12, 4, 1, 6, StoneDark);
        Rect(t, 20, 4, 1, 6, StoneDark);
    }

    static void DrawCrystal(Texture2D t, System.Random rng)
    {
        GroundShadow(t, 16, 6);
        for (int y = 3; y < 20; y++)
        {
            int w = Mathf.RoundToInt(5 * (1 - Mathf.Abs(y - 11) / 9f));
            Rect(t, 16 - w, y, w * 2, 1, CrystalGlow);
        }
        Rect(t, 14, 9, 4, 5, Color.Lerp(CrystalGlow, Color.white, 0.5f));
        var glow = new Color(CrystalGlow.r, CrystalGlow.g, CrystalGlow.b, 0.25f);
        for (int x = 10; x < 22; x++)
            for (int y = 5; y < 18; y++)
                if (t.GetPixel(x, y).a < 0.1f) Px(t, x, y, glow);
    }
}

#if UNITY_EDITOR
static class ProceduralPropsDebug
{
    [UnityEditor.MenuItem("Solengard/Debug/Preview Procedural Props")]
    static void PreviewProceduralProps()
    {
        var types   = System.Enum.GetValues(typeof(ProceduralProps.PropType));
        int count   = types.Length;
        float spacing = 3f;
        float startX  = -(count - 1) * spacing * 0.5f;

        var created = new System.Collections.Generic.List<UnityEngine.Object>();
        for (int i = 0; i < count; i++)
        {
            var type = (ProceduralProps.PropType)types.GetValue(i);
            var go   = new UnityEngine.GameObject($"Preview_{type}");
            go.transform.position = new UnityEngine.Vector3(startX + i * spacing, 0f, 0f);
            var sr       = go.AddComponent<UnityEngine.SpriteRenderer>();
            sr.sprite    = ProceduralProps.Generate(type, 42);
            sr.sortingOrder = 10;
            created.Add(go);
        }

        UnityEditor.Selection.objects = created.ToArray();
        UnityEngine.Debug.Log($"[ProceduralProps] {count} props de preview criados. Delete quando terminar.");
        UnityEditor.EditorUtility.DisplayDialog("Preview Procedural Props",
            $"{count} objetos criados na cena, lado a lado.\n\nDelete quando terminar a inspeção visual.", "OK");
    }
}
#endif
