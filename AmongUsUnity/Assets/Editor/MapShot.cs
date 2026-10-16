using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Headless visual check: renders the migrated Skeld prefab to a PNG.
/// Args: -shotOut path -shotX x -shotY y -shotSize orthoSize [-shotW 1600 -shotH 900]</summary>
public static class MapShot
{
    static string Arg(string name, string def)
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : def;
    }

    public static void Run()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Arg("-prefab", "Assets/Original/Ship/SkeldShip.prefab"));
        var ship = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (Arg("-prefab", "").Length > 0) ship.transform.position = Vector3.zero;
        foreach (var d in ship.GetComponentsInChildren<DateHide>(true)) d.Apply();
        var camGo = new GameObject("cam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = float.Parse(Arg("-shotSize", "12"));
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(float.Parse(Arg("-shotX", "-8")), float.Parse(Arg("-shotY", "-6")), -200);
        cam.farClipPlane = 1000;
        int w = int.Parse(Arg("-shotW", "1600")), h = int.Parse(Arg("-shotH", "900"));
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        var path = Arg("-shotOut", "shot.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log("SHOT saved " + path + " renderers=" + ship.GetComponentsInChildren<SpriteRenderer>().Length);
        EditorApplication.Exit(0);
    }
}
