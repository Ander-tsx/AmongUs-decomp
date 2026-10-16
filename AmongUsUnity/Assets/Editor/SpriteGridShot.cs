using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Contact sheet of sprites by name (from Assets/Original/Sprite) for inspecting migrated art.
/// Args: -names a,b,c -out file.png [-cell 200]</summary>
public static class SpriteGridShot
{
    static string Arg(string name, string def)
    {
        var a = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : def;
    }

    public static void Run()
    {
        var names = Arg("-names", "").Split(',').Where(s => s.Length > 0).ToArray();
        int cell = int.Parse(Arg("-cell", "200"));
        int cols = Mathf.Min(names.Length, 6), rows = (names.Length + cols - 1) / cols;
        var tex = new Texture2D(cols * cell, rows * cell, TextureFormat.RGBA32, false);
        var bg = new Color32[tex.width * tex.height];
        for (int i = 0; i < bg.Length; i++) bg[i] = new Color32(70, 70, 90, 255);
        tex.SetPixels32(bg);
        for (int n = 0; n < names.Length; n++)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Original/Sprite/" + names[n] + ".asset");
            if (sp == null) { Debug.LogWarning("SHOT missing " + names[n]); continue; }
            var src = sp.texture;
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var full = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            full.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); full.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var r = sp.textureRect;
            var px = full.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
            float k = Mathf.Min((cell - 8) / r.width, (cell - 8) / r.height, 1f);
            int tw = Mathf.Max(1, (int)(r.width * k)), th = Mathf.Max(1, (int)(r.height * k));
            int ox = (n % cols) * cell + (cell - tw) / 2, oy = (rows - 1 - n / cols) * cell + (cell - th) / 2;
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    var c = px[(int)(y / k) * (int)r.width + (int)(x / k)];
                    var d = tex.GetPixel(ox + x, oy + y);
                    tex.SetPixel(ox + x, oy + y, Color.Lerp(d, new Color(c.r, c.g, c.b, 1), c.a));
                }
            Debug.Log("SHOT " + names[n] + " " + r.width + "x" + r.height);
        }
        File.WriteAllBytes(Arg("-out", "grid.png"), tex.EncodeToPNG());
        EditorApplication.Exit(0);
    }
}
