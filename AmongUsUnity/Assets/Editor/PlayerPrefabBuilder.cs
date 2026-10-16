using System.IO;
using PowerTools;
using UnityEditor;
using UnityEngine;

/// <summary>Builds Resources/Player.prefab and Resources/DeadBody.prefab from the migrated original assets.</summary>
public static class PlayerPrefabBuilder
{
    const string Orig = "Assets/Original/";

    static AnimationClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Orig + "AnimationClip/" + n + ".anim");

    public static Material PlayerMaterial()
    {
        const string path = "Assets/Resources/PlayerMaterial.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        Directory.CreateDirectory("Assets/Resources");
        m = new Material(Shader.Find("AmongUsLan/PlayerShader"));
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    public static void Build()
    {
        Directory.CreateDirectory("Assets/Resources");
        var mat = PlayerMaterial();

        // ---- Player: root scale 0.7, body child scale 0.5, as in the original prefab ----
        var root = new GameObject("Player");
        root.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        var bodyGo = new GameObject("Normal");
        bodyGo.transform.SetParent(root.transform, false);
        bodyGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        var sr = bodyGo.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = mat;
        var idle = Clip("Idle_0");
        bodyGo.AddComponent<Animator>();
        var anim = bodyGo.AddComponent<SpriteAnim>();
        var vis = root.AddComponent<PlayerVisual>();
        vis.Body = sr; vis.Anim = anim;
        vis.IdleClip = idle; vis.WalkClip = Clip("Walk"); vis.GhostClip = Clip("Ghost");
        vis.SpawnClip = Clip("SpawnIn"); vis.EnterVentClip = Clip("EnterVent"); vis.ExitVentClip = Clip("ExitVent");
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Player.prefab");
        Object.DestroyImmediate(root);

        // ---- Dead body: the original prefab minus its (missing) scripts, recoloured through our shader ----
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Orig + "GameObject/DeadBodyA.prefab");
        if (src != null)
        {
            var body = (GameObject)Object.Instantiate(src);
            foreach (var t in body.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                var s = r.sharedMaterial != null ? r.sharedMaterial.shader.name : "";
                if (s.Contains("PlayerShader") || s.Contains("SimpleMaskedPlayer") || r.sharedMaterial == null) r.sharedMaterial = mat;
            }
            foreach (var c in body.GetComponentsInChildren<Collider2D>(true)) c.isTrigger = true;
            PrefabUtility.SaveAsPrefabAsset(body, "Assets/Resources/DeadBody.prefab");
            Object.DestroyImmediate(body);
        }
        AssetDatabase.SaveAssets();
    }
}
