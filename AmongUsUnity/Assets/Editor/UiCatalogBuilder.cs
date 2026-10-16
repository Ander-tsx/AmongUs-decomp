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

/// <summary>Builds Resources/PrefabCatalog.asset from Assets/Editor/prefabs.txt (prefabs in Assets/Original/GameObject).</summary>
public static class PrefabCatalogBuilder
{
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Resources");
        var names = new List<string>();
        var prefabs = new List<GameObject>();
        foreach (var raw in File.ReadAllLines("Assets/Editor/prefabs.txt"))
        {
            var n = raw.Trim();
            if (n.Length == 0) continue;
            var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Original/GameObject/" + n + ".prefab");
            if (src == null) { Debug.LogWarning("PrefabCatalog: prefab not found " + n); continue; }
            // Strip the behaviours whose scripts were not recovered, keep the bound ones, save into Resources.
            var inst = (GameObject)Object.Instantiate(src);
            foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            Directory.CreateDirectory("Assets/Resources/Minigames");
            var saved = PrefabUtility.SaveAsPrefabAsset(inst, "Assets/Resources/Minigames/" + n + ".prefab");
            Object.DestroyImmediate(inst);
            names.Add(n);
            prefabs.Add(saved);
        }
        const string path = "Assets/Resources/PrefabCatalog.asset";
        var cat = AssetDatabase.LoadAssetAtPath<PrefabCatalog>(path);
        if (cat == null) { cat = ScriptableObject.CreateInstance<PrefabCatalog>(); AssetDatabase.CreateAsset(cat, path); }
        cat.Names = names.ToArray();
        cat.Prefabs = prefabs.ToArray();
        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
    }
}
