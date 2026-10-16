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
        public const float WorldHeight = 1.0f;

        static Texture2D _base;
        static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        // Idle front-facing crewmate in ClassicPlayerAnimations (top-left cell, top-down pixel rows).
        const int CropX = 8, CropTop = 8, CropW = 140, CropH = 172;

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
            var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), rect.height / WorldHeight);
            Cache[colorIndex] = sprite;
            return sprite;
        }
    }

    /// <summary>Procedural "Skeld-like" ship: rooms joined by corridors, walls generated around the floor.</summary>
    public static class ShipMap
    {
        public struct Room { public string Name; public RectInt Rect; public Color Floor; public bool Label; }

        public static readonly Room[] Rooms =
        {
            new Room { Name = "Cafeteria",  Rect = new RectInt(0, 0, 12, 8),   Floor = new Color(0.36f, 0.42f, 0.5f),  Label = true },
            new Room { Name = "Navigation", Rect = new RectInt(16, 0, 10, 8),  Floor = new Color(0.3f, 0.45f, 0.4f),   Label = true },
            new Room { Name = "Reactor",    Rect = new RectInt(0, 14, 12, 8),  Floor = new Color(0.5f, 0.38f, 0.34f),  Label = true },
            new Room { Name = "Electrical", Rect = new RectInt(16, 14, 10, 8), Floor = new Color(0.46f, 0.44f, 0.3f),  Label = true },
            new Room { Name = "MedBay",     Rect = new RectInt(-10, 0, 8, 8),  Floor = new Color(0.34f, 0.5f, 0.52f),  Label = true },
            new Room { Name = "Storage",    Rect = new RectInt(0, -8, 12, 6),  Floor = new Color(0.4f, 0.36f, 0.45f),  Label = true },
            new Room { Name = "",           Rect = new RectInt(12, 3, 4, 2),   Floor = new Color(0.27f, 0.31f, 0.37f) },
            new Room { Name = "",           Rect = new RectInt(5, 8, 2, 6),    Floor = new Color(0.27f, 0.31f, 0.37f) },
            new Room { Name = "",           Rect = new RectInt(12, 17, 4, 2),  Floor = new Color(0.27f, 0.31f, 0.37f) },
            new Room { Name = "",           Rect = new RectInt(20, 8, 2, 6),   Floor = new Color(0.27f, 0.31f, 0.37f) },
            new Room { Name = "",           Rect = new RectInt(-2, 3, 2, 2),   Floor = new Color(0.27f, 0.31f, 0.37f) },
            new Room { Name = "",           Rect = new RectInt(5, -2, 2, 2),   Floor = new Color(0.27f, 0.31f, 0.37f) },
        };

        public struct TaskSpot { public string Name; public Vector2 Pos; }

        public static readonly TaskSpot[] Tasks =
        {
            new TaskSpot { Name = "Submit Scan",     Pos = new Vector2(-6f, 4f) },
            new TaskSpot { Name = "Fuel Engines",    Pos = new Vector2(3f, -5f) },
            new TaskSpot { Name = "Empty Garbage",   Pos = new Vector2(9f, -5f) },
            new TaskSpot { Name = "Start Reactor",   Pos = new Vector2(3f, 18f) },
            new TaskSpot { Name = "Fix Wiring",      Pos = new Vector2(20f, 18f) },
            new TaskSpot { Name = "Calibrate Distributor", Pos = new Vector2(24f, 16f) },
            new TaskSpot { Name = "Chart Course",    Pos = new Vector2(21f, 4f) },
            new TaskSpot { Name = "Download Data",   Pos = new Vector2(10f, 6f) },
        };

        public static readonly Vector2 EmergencyButton = new Vector2(6f, 4f);
        public static readonly Vector2 SpawnCenter = new Vector2(6f, 4f);

        static Sprite _white;
        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var t = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                    t.SetPixel(0, 0, Color.white);
                    t.Apply();
                    _white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }
                return _white;
            }
        }

        static SpriteRenderer Quad(Transform parent, Vector2 center, Vector2 size, Color color, int order, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = White;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        public static GameObject Build()
        {
            var root = new GameObject("Ship");
            var walk = new HashSet<Vector2Int>();
            foreach (var r in Rooms)
            {
                for (int x = r.Rect.xMin; x < r.Rect.xMax; x++)
                    for (int y = r.Rect.yMin; y < r.Rect.yMax; y++)
                        walk.Add(new Vector2Int(x, y));
                Quad(root.transform, r.Rect.center, r.Rect.size, r.Floor, -100, "Floor " + r.Name);
            }

            var walls = new HashSet<Vector2Int>();
            foreach (var c in walk)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        var n = new Vector2Int(c.x + dx, c.y + dy);
                        if (!walk.Contains(n)) walls.Add(n);
                    }
            foreach (var w in walls)
            {
                var sr = Quad(root.transform, new Vector2(w.x + 0.5f, w.y + 0.5f), Vector2.one, new Color(0.09f, 0.1f, 0.14f), -90, "Wall");
                sr.gameObject.AddComponent<BoxCollider2D>();
            }

            // Decorations: task consoles and the emergency button table.
            foreach (var t in Tasks)
                Quad(root.transform, t.Pos, new Vector2(0.8f, 0.8f), new Color(0.95f, 0.8f, 0.2f), -50, "Task " + t.Name);
            Quad(root.transform, EmergencyButton, new Vector2(1.4f, 1.4f), new Color(0.75f, 0.12f, 0.12f), -50, "Emergency Button");
            return root;
        }
    }
}
