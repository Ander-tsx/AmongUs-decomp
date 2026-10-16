using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Renders several migrated prefabs to PNGs in one editor session. Args: -names a,b,c -dir outdir [-size 3]</summary>
public static class PrefabShots
{
    static string Arg(string name, string def)
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : def;
    }

    public static void Run()
    {
        var dir = Arg("-dir", "shots");
        Directory.CreateDirectory(dir);
        float size = float.Parse(Arg("-size", "3"));
        foreach (var n in Arg("-names", "").Split(','))
        {
            if (n.Length == 0) continue;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Original/GameObject/" + n + ".prefab");
            if (prefab == null) { Debug.LogWarning("SHOTS missing " + n); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = Vector3.zero;
            var camGo = new GameObject("cam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = size; cam.farClipPlane = 1000;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            camGo.transform.position = new Vector3(0, 0, -200);
            int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, n + ".png"), tex.EncodeToPNG());
            Debug.Log("SHOTS " + n);
        }
        EditorApplication.Exit(0);
    }
}
