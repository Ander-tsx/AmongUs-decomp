using System;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Procedurally drawn pixel-art props for the Halloween theme (no external assets needed).</summary>
    public static class HalloweenArt
    {
        static Sprite _pumpkin, _glow, _web, _ghost, _tomb, _candle, _skull, _spider, _cat;
        static Sprite[] _bat;

        static Sprite Make(int w, int h, Func<float, float, Color> f, float ppu = 32f)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h);
            t.SetPixels(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
        }

        static readonly Color Clear = new Color(0, 0, 0, 0);
        static float Ell(float x, float y, float cx, float cy, float rx, float ry) { float a = (x - cx) / rx, b = (y - cy) / ry; return a * a + b * b; }
        static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay, t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float qx = ax + dx * t - px, qy = ay + dy * t - py;
            return Mathf.Sqrt(qx * qx + qy * qy);
        }

        public static Sprite Pumpkin => _pumpkin != null ? _pumpkin : _pumpkin = Make(48, 44, (u, v) =>
        {
            float x = (u - 0.5f) * 2f, y = (v - 0.45f) * 2f;
            // stem
            if (Mathf.Abs(x - (y - 0.7f) * 0.25f) < 0.11f && y > 0.62f && y < 0.98f) return new Color(0.28f, 0.42f, 0.1f);
            float best = 9f; float[] cxs = { -0.42f, 0f, 0.42f }; float[] rxs = { 0.46f, 0.52f, 0.46f }; int lobe = 0;
            for (int i = 0; i < 3; i++) { float d = Ell(x, y, cxs[i], 0f, rxs[i], i == 1 ? 0.74f : 0.68f); if (d < best) { best = d; lobe = i; } }
            if (best > 1f) return Clear;
            float shade = 1f - 0.38f * best * best;
            var body = new Color(1f * shade, 0.47f * shade, 0.04f * shade);
            if (best > 0.86f) body *= 0.62f;
            if (lobe != 1 && best > 0.55f) body *= 0.88f;
            // carved face (glowing from inside)
            var glow = new Color(1f, 0.9f, 0.35f);
            for (int e = -1; e <= 1; e += 2)
            {
                float ex = e * 0.3f, w = 0.17f * (1f - (y - 0.08f) / 0.3f);
                if (y > 0.08f && y < 0.38f && Mathf.Abs(x - ex) < w) return glow;
            }
            float lo = -0.58f + 0.5f * (x / 0.62f) * (x / 0.62f), hi = lo + 0.26f;
            if (Mathf.Abs(x) < 0.62f && y > lo && y < hi)
            {
                float f = (x * 4.2f) - Mathf.Floor(x * 4.2f);
                if (f < 0.24f && y > hi - 0.11f) return body;
                if (f > 0.76f && y < lo + 0.11f) return body;
                return glow;
            }
            return body;
        });

        public static Sprite Glow => _glow != null ? _glow : _glow = Make(32, 32, (u, v) =>
        {
            float r = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
            float a = Mathf.Clamp01(1f - r); a *= a;
            return new Color(1f, 0.55f, 0.1f, a * 0.55f);
        }, 16f);

        public static Sprite Bat(int frame)
        {
            if (_bat == null) _bat = new Sprite[2];
            if (_bat[frame] != null) return _bat[frame];
            return _bat[frame] = Make(32, 16, (u, v) =>
            {
                float x = (u - 0.5f) * 2f, y = (v - 0.5f) * 2f, ax = Mathf.Abs(x);
                var dark = new Color(0.1f, 0.04f, 0.16f);
                if (Ell(x, y, 0, 0, 0.12f, 0.45f) < 1f)
                {
                    if (Ell(x, y, 0.05f, 0.22f, 0.03f, 0.06f) < 1f || Ell(x, y, -0.05f, 0.22f, 0.03f, 0.06f) < 1f) return new Color(1f, 0.2f, 0.2f);
                    return dark;
                }
                if (Ell(x, y, 0.06f, 0.5f, 0.04f, 0.14f) < 1f || Ell(x, y, -0.06f, 0.5f, 0.04f, 0.14f) < 1f) return dark; // ears
                if (ax > 0.1f && ax < 0.98f)
                {
                    float k = (ax - 0.1f) / 0.88f;
                    float top = frame == 0 ? 0.15f + 0.65f * Mathf.Sin(k * 1.4f) : 0.15f - 0.55f * k * 0.9f + 0.25f * Mathf.Sin(k * 2.6f);
                    float bot = -0.45f + 0.28f * Mathf.Abs(Mathf.Sin(k * 9f)) * (1f - k * 0.6f) - (frame == 0 ? 0f : 0.15f * k);
                    if (frame == 1) bot = Mathf.Min(bot, top - 0.1f) ;
                    if (y < top && y > Mathf.Min(bot, top - 0.12f) && k < 1f) return dark;
                }
                return Clear;
            }, 16f);
        }

        public static Sprite Ghost => _ghost != null ? _ghost : _ghost = Make(32, 40, (u, v) =>
        {
            float x = (u - 0.5f) * 2f, y = (v - 0.5f) * 2f;
            bool head = Ell(x, y, 0, 0.3f, 0.78f, 0.62f) < 1f;
            float bottom = -0.8f + 0.14f * Mathf.Sin(u * Mathf.PI * 2f * 3f);
            bool body = Mathf.Abs(x) < 0.78f && y <= 0.3f && y > bottom;
            if (!head && !body) return Clear;
            if (Ell(x, y, -0.28f, 0.35f, 0.13f, 0.2f) < 1f || Ell(x, y, 0.28f, 0.35f, 0.13f, 0.2f) < 1f) return new Color(0.05f, 0.02f, 0.1f, 0.9f);
            if (Ell(x, y, 0, -0.05f, 0.12f, 0.17f) < 1f) return new Color(0.05f, 0.02f, 0.1f, 0.85f);
            return new Color(0.88f, 0.95f, 1f, 0.85f);
        }, 24f);

        public static Sprite Web => _web != null ? _web : _web = Make(48, 48, (u, v) =>
        {
            // anchored in the top-left corner; flip with scale.x = -1 for the other side
            float x = u, y = 1f - v;
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 1f || r < 0.02f) return Clear;
            float ang = Mathf.Atan2(y, x);
            float spokes = ang / (Mathf.PI / 2f) * 6f; float sp = Mathf.Abs(spokes - Mathf.Round(spokes)) * r * 0.26f;
            bool spoke = sp < 0.012f;
            float ringR = r * 5f; float rf = ringR - Mathf.Floor(ringR);
            float sag = 0.07f * Mathf.Abs(Mathf.Sin(spokes * Mathf.PI));
            bool ring = Mathf.Abs(rf - (0.2f + sag * 2f)) < 0.07f && r > 0.18f;
            if (!spoke && !ring) return Clear;
            return new Color(0.92f, 0.92f, 1f, 0.5f * (1f - r * 0.5f));
        }, 24f);

        public static Sprite Tomb => _tomb != null ? _tomb : _tomb = Make(24, 32, (u, v) =>
        {
            float x = (u - 0.5f) * 2f, y = v * 2f - 1f;
            bool inside = Mathf.Abs(x) < 0.82f && (y < 0.35f || Ell(x, y, 0, 0.35f, 0.82f, 0.62f) < 1f) && y > -0.96f;
            if (!inside) return Clear;
            if ((Mathf.Abs(x) < 0.09f && y > 0f && y < 0.7f) || (Mathf.Abs(y - 0.4f) < 0.08f && Mathf.Abs(x) < 0.32f)) return new Color(0.22f, 0.22f, 0.28f);
            float shade = 0.78f + 0.2f * (1f - Mathf.Abs(x)); if (y < -0.8f) shade *= 0.7f;
            return new Color(0.5f * shade, 0.52f * shade, 0.58f * shade);
        }, 24f);

        public static Sprite Candle => _candle != null ? _candle : _candle = Make(10, 26, (u, v) =>
        {
            float x = (u - 0.5f) * 2f;
            if (v < 0.62f && Mathf.Abs(x) < 0.62f) return new Color(0.95f, 0.9f, 0.75f) * (0.85f + 0.15f * x);
            if (v >= 0.62f && v < 0.68f && Mathf.Abs(x) < 0.08f) return new Color(0.15f, 0.1f, 0.1f);
            if (v >= 0.68f && Ell(x, v, 0, 0.82f, 0.45f, 0.17f) < 1f) return Ell(x, v, 0, 0.78f, 0.22f, 0.08f) < 1f ? new Color(1f, 1f, 0.7f) : new Color(1f, 0.6f, 0.1f);
            return new Color(0, 0, 0, 0);
        }, 24f);

        public static Sprite Skull => _skull != null ? _skull : _skull = Make(20, 20, (u, v) =>
        {
            float x = (u - 0.5f) * 2f, y = (v - 0.5f) * 2f;
            bool cranium = Ell(x, y, 0, 0.2f, 0.82f, 0.74f) < 1f, jaw = Mathf.Abs(x) < 0.5f && y < 0f && y > -0.8f;
            if (!cranium && !jaw) return Clear;
            if (Ell(x, y, -0.32f, 0.12f, 0.2f, 0.22f) < 1f || Ell(x, y, 0.32f, 0.12f, 0.2f, 0.22f) < 1f) return new Color(0.06f, 0.03f, 0.08f);
            if (y < -0.1f && y > -0.3f && Mathf.Abs(x) < 0.08f) return new Color(0.2f, 0.15f, 0.15f);
            if (y < -0.5f && Mathf.Abs(Mathf.Sin(x * 11f)) < 0.2f) return new Color(0.3f, 0.28f, 0.25f);
            return new Color(0.93f, 0.9f, 0.8f);
        }, 24f);

        public static Sprite Spider => _spider != null ? _spider : _spider = Make(24, 24, (u, v) =>
        {
            float x = (u - 0.5f) * 2f, y = (v - 0.5f) * 2f;
            var blk = new Color(0.08f, 0.04f, 0.1f);
            if (Ell(x, y, 0, -0.1f, 0.38f, 0.42f) < 1f || Ell(x, y, 0, 0.42f, 0.22f, 0.2f) < 1f)
            {
                if (Ell(x, y, 0.08f, 0.47f, 0.05f, 0.05f) < 1f || Ell(x, y, -0.08f, 0.47f, 0.05f, 0.05f) < 1f) return new Color(1f, 0.1f, 0.1f);
                return blk;
            }
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 4; i++)
                {
                    float ya = 0.35f - i * 0.28f;
                    float kx = s * 0.8f, ky = ya + 0.3f;
                    if (SegDist(x, y, s * 0.25f, ya * 0.6f, kx, ky) < 0.07f || SegDist(x, y, kx, ky, s * 0.95f, ya - 0.5f) < 0.07f) return blk;
                }
            return Clear;
        }, 24f);

        public static Sprite Cat => _cat != null ? _cat : _cat = Make(24, 28, (u, v) =>
        {
            float x = (u - 0.5f) * 2f, y = (v - 0.5f) * 2f;
            var blk = new Color(0.07f, 0.04f, 0.1f);
            bool body = Ell(x, y, 0, -0.35f, 0.62f, 0.55f) < 1f, head = Ell(x, y, 0, 0.42f, 0.5f, 0.42f) < 1f;
            bool ears = (Mathf.Abs(x) > 0.18f && Mathf.Abs(x) < 0.52f && y > 0.55f && y < 0.98f && y < 0.5f + (0.52f - Mathf.Abs(x)) * 2.4f && y > 0.5f);
            bool tail = Ell(x, y, 0.62f, -0.2f, 0.12f, 0.6f) < 1f && x > 0.5f;
            if (!body && !head && !ears && !tail) return Clear;
            if (Ell(x, y, -0.2f, 0.45f, 0.1f, 0.1f) < 1f || Ell(x, y, 0.2f, 0.45f, 0.1f, 0.1f) < 1f) return new Color(0.9f, 0.9f, 0.2f);
            return blk;
        }, 24f);
    }
}
