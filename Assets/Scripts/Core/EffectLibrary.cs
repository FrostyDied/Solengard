using System.Collections.Generic;
using UnityEngine;

public static class EffectLibrary
{
    static readonly Dictionary<string, Sprite[]> _cache = new();

    public static Sprite[] GetFrames(string effectName)
    {
        if (_cache.TryGetValue(effectName, out var cached)) return cached;

        var sprites = Resources.LoadAll<Sprite>($"Effects/{effectName}");
        System.Array.Sort(sprites, (a, b) => NaturalCompare(a.name, b.name));
        _cache[effectName] = sprites;

        if (sprites.Length == 0)
            Debug.LogWarning($"[EffectLibrary] Nenhum sprite encontrado em Effects/{effectName}");

        return sprites;
    }

    public static Sprite[] GetFramesRange(string effectName, int start, int count)
    {
        var all = GetFrames(effectName);
        if (all.Length == 0) return all;
        start = Mathf.Clamp(start, 0, all.Length - 1);
        count = Mathf.Clamp(count, 1, all.Length - start);
        var result = new Sprite[count];
        System.Array.Copy(all, start, result, 0, count);
        return result;
    }

    static int NaturalCompare(string a, string b)
    {
        int numA = ExtractTrailingInt(a);
        int numB = ExtractTrailingInt(b);

        if (numA >= 0 && numB >= 0)
        {
            string prefA = StripTrailingDigits(a);
            string prefB = StripTrailingDigits(b);
            int cmp = string.Compare(prefA, prefB, System.StringComparison.OrdinalIgnoreCase);
            return cmp != 0 ? cmp : numA.CompareTo(numB);
        }
        return string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase);
    }

    static int ExtractTrailingInt(string s)
    {
        int i = s.Length - 1;
        while (i >= 0 && char.IsDigit(s[i])) i--;
        return i < s.Length - 1 && int.TryParse(s.Substring(i + 1), out int n) ? n : -1;
    }

    static string StripTrailingDigits(string s)
    {
        int i = s.Length - 1;
        while (i >= 0 && char.IsDigit(s[i])) i--;
        return s.Substring(0, i + 1);
    }
}
