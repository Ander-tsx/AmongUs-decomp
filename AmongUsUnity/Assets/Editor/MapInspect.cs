using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MapInspect
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Original/Ship/SkeldShip.prefab");
        if (prefab == null) { Debug.LogError("INSPECT prefab null"); EditorApplication.Exit(1); return; }
        var srs = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        int nullSprites = srs.Count(r => r.sprite == null);
        int missing = prefab.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        Debug.Log($"INSPECT renderers={srs.Length} nullSprites={nullSprites} missingScripts={missing} " +
                  $"edge={prefab.GetComponentsInChildren<EdgeCollider2D>(true).Length} poly={prefab.GetComponentsInChildren<PolygonCollider2D>(true).Length} " +
                  $"box={prefab.GetComponentsInChildren<BoxCollider2D>(true).Length}");
        var s = srs.FirstOrDefault(r => r.sprite != null);
        if (s != null) Debug.Log($"INSPECT sample sprite {s.sprite.name} tex={s.sprite.texture?.name} rect={s.sprite.rect} ppu={s.sprite.pixelsPerUnit}");
        EditorApplication.Exit(0);
    }
}
