using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Builds Resources/UiSprites.asset from Assets/Editor/ui_sprites.txt (sprites migrated into Assets/Original/Sprite).</summary>
public static class UiCatalogBuilder
{
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Resources");
        var names = new List<string>();
        var sprites = new List<Sprite>();
        foreach (var raw in File.ReadAllLines("Assets/Editor/ui_sprites.txt"))
        {
            var n = raw.Trim();
            if (n.Length == 0) continue;
            var s = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Original/Sprite/" + n + ".asset");
            if (s == null) { Debug.LogWarning("UiCatalog: sprite not found " + n); continue; }
            names.Add(n);
            sprites.Add(s);
        }
        const string path = "Assets/Resources/UiSprites.asset";
        var cat = AssetDatabase.LoadAssetAtPath<UiSprites>(path);
        if (cat == null) { cat = ScriptableObject.CreateInstance<UiSprites>(); AssetDatabase.CreateAsset(cat, path); }
        cat.Names = names.ToArray();
        cat.Sprites = sprites.ToArray();
        cat.Font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Original/Font/Barlow-Bold.ttf");
        cat.FontBlack = AssetDatabase.LoadAssetAtPath<Font>("Assets/Original/Font/Barlow-Black.ttf");
        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
    }
}
