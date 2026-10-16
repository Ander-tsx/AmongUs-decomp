using System.Collections.Generic;
using UnityEngine;

namespace AmongUsLan
{
    public static class Palette
    {
        public static readonly string[] Names =
            { "Red", "Blue", "Green", "Pink", "Orange", "Yellow", "Black", "White", "Purple", "Brown", "Cyan", "Lime" };

        public static readonly Color[] Colors =
        {
            new Color32(197, 17, 17, 255), new Color32(19, 46, 210, 255), new Color32(17, 127, 45, 255),
            new Color32(237, 84, 186, 255), new Color32(239, 125, 13, 255), new Color32(246, 246, 87, 255),
            new Color32(63, 71, 78, 255), new Color32(214, 224, 240, 255), new Color32(107, 49, 188, 255),
            new Color32(113, 73, 30, 255), new Color32(57, 254, 221, 255), new Color32(80, 240, 57, 255),
        };
    }

    /// <summary>
    /// Reuses the original crewmate sprite sheet. The sheet is authored with a red body / blue shade mask,
    /// so each player colour is produced by remapping those two channels at load time.
    /// </summary>
    public static class PlayerArt
    {
        // Original Player prefab: root scale 0.7 * Normal body scale 0.5 = 0.35 at 100 pixels per unit.
        public const float BodyScale = 0.35f;

        static Texture2D _base;
        static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        // Idle front-facing crewmate in ClassicPlayerAnimations (top-left cell, top-down pixel rows).
        const int CropX = 4, CropTop = 6, CropW = 150, CropH = 200;

        static void LoadBase()
        {
            if (_base != null) return;
            var asset = Resources.Load<TextAsset>("Art/ClassicPlayerAnimations");
            var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sheet.LoadImage(asset.bytes, false);
            int y = sheet.height - CropTop - CropH;
            _base = new Texture2D(CropW, CropH, TextureFormat.RGBA32, false);
            _base.SetPixels(sheet.GetPixels(CropX, y, CropW, CropH));
            _base.Apply();
            Object.Destroy(sheet);
        }

        public static Sprite Get(int colorIndex)
        {
            colorIndex = Mathf.Abs(colorIndex) % Palette.Colors.Length;
            if (Cache.TryGetValue(colorIndex, out var cached)) return cached;
            LoadBase();
            var c = Palette.Colors[colorIndex];
            var src = _base.GetPixels32();
            var dst = new Color32[src.Length];
            int minX = CropW, minY = CropH, maxX = 0, maxY = 0;
            for (int i = 0; i < src.Length; i++)
            {
                var p = src[i];
                if (p.a < 8) { dst[i] = default; continue; }
                bool grayShadow = Mathf.Abs(p.r - p.g) < 24 && Mathf.Abs(p.g - p.b) < 24 && p.r > 20 && p.r < 140;
                if (grayShadow) { dst[i] = default; continue; }
                if (p.r > p.g + 40 && p.r > p.b + 40)
                {
                    float k = Mathf.Clamp01(p.r / 215f);
                    dst[i] = new Color(c.r * k, c.g * k, c.b * k, 1f);
                }
                else if (p.b > p.r + 40 && p.b > p.g + 40)
                {
                    float k = Mathf.Clamp01(p.b / 215f) * 0.62f;
                    dst[i] = new Color(c.r * k, c.g * k, c.b * k, 1f);
                }
                else dst[i] = p;
                int x = i % CropW, y = i / CropW;
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
            var tex = new Texture2D(CropW, CropH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            tex.SetPixels32(dst);
            tex.Apply();
            var rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f / BodyScale);
            Cache[colorIndex] = sprite;
            return sprite;
        }
    }
}
